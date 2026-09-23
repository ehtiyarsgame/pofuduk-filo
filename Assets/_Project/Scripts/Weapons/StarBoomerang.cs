using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 3) Star Boomerang — stars fly out, slow down and return, hitting on both legs (×1.5 on the
    /// way back from Lv3). Lv5: returning stars circle the ship once before being thrown again.
    /// Evolved (Galaxy Vortex): a spiral in the upper screen pulls enemies in, then collapses into
    /// a firework of small stars (weapon-system.md §3.2).
    /// </summary>
    public sealed class StarBoomerang : WeaponBehaviour
    {
        private enum Leg { Out, Back, Orbit }

        [SerializeField] private Sprite starSprite;
        [SerializeField] private Color starColor = new(1f, 0.91f, 0.64f);
        [SerializeField] private float hitRadius = 0.35f;
        [SerializeField] private float perEnemyHitCooldown = 0.3f;
        [SerializeField] private int returnBonusFromLevel = 3;
        [SerializeField] private int orbitFromLevel = 5;
        [SerializeField] private float orbitRadius = 1.3f;

        [Header("Evolution: Galaxy Vortex")]
        [SerializeField] private bool isEvolved;
        [SerializeField] private float vortexPull = 3f;
        [SerializeField] private float vortexTick = 0.25f;
        [SerializeField] private int fireworkBulletType = -1;
        [SerializeField] private int fireworkCount = 36;
        [SerializeField] private Color vortexColor = new(0.72f, 0.62f, 1f, 0.5f);

        private struct Star
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public Leg Leg;
            public float OrbitAngle;
            public float OrbitTravelled;
            public float Damage;
        }

        private struct Vortex
        {
            public Vector2 Center;
            public float Age;
            public float Duration;
            public float Radius;
            public float Damage;
            public float NextTick;
            public SpriteRenderer Renderer;
        }

        private readonly List<Star> _stars = new(8);
        private readonly List<Vortex> _vortices = new(2);
        // Hit cooldown per (enemy id); shared by all stars — cheap and good enough at this scale.
        private readonly Dictionary<int, float> _nextHitTime = new(64);

        protected override void Fire(in WeaponLevelStats s)
        {
            if (isEvolved)
            {
                SpawnVortex(s);
                return;
            }

            int count = Mathf.Max(1, s.projectileCount);
            float speed = s.projectileSpeed * Stats.SpeedMultiplier;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                float angle = (90f + t * s.spreadDegrees) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                SpriteRenderer r = Vfx != null && starSprite != null ? Vfx.Sprites.Get(starSprite, Origin, 0.45f, starColor) : null;
                _stars.Add(new Star { Renderer = r, Position = Origin, Velocity = dir * speed, Damage = s.damage });
            }
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            TickStars(dt);
            TickVortices(dt);
        }

        private void TickStars(float dt)
        {
            WeaponLevelStats s = CurrentStats;
            float speed = s.projectileSpeed * Stats.SpeedMultiplier;
            // Deceleration chosen so the star turns around after `lifetime/2` seconds.
            float decel = speed / Mathf.Max(0.1f, DurationOf(s) * 0.5f);

            for (int i = _stars.Count - 1; i >= 0; i--)
            {
                Star st = _stars[i];
                switch (st.Leg)
                {
                    case Leg.Out:
                        Vector2 slowed = st.Velocity - st.Velocity.normalized * decel * dt;
                        if (Vector2.Dot(slowed, st.Velocity) <= 0f) st.Leg = Leg.Back;
                        else st.Velocity = slowed;
                        st.Position += st.Velocity * dt;
                        break;

                    case Leg.Back:
                        Vector2 toShip = Origin - st.Position;
                        float dist = toShip.magnitude;
                        st.Position += toShip.normalized * Mathf.Min(dist, speed * 1.2f * dt);
                        if (dist < 0.3f)
                        {
                            if (Level >= orbitFromLevel)
                            {
                                st.Leg = Leg.Orbit;
                                st.OrbitAngle = Mathf.Atan2(-toShip.y, -toShip.x);
                            }
                            else
                            {
                                Remove(i, st);
                                continue;
                            }
                        }
                        break;

                    case Leg.Orbit:
                        float step = speed / orbitRadius * dt;
                        st.OrbitAngle += step;
                        st.OrbitTravelled += step;
                        st.Position = Origin + new Vector2(Mathf.Cos(st.OrbitAngle), Mathf.Sin(st.OrbitAngle)) * orbitRadius;
                        if (st.OrbitTravelled >= Mathf.PI * 2f)
                        {
                            Remove(i, st);
                            continue;
                        }
                        break;
                }

                float mult = st.Leg != Leg.Out && Level >= returnBonusFromLevel ? 1.5f : 1f;
                HitAround(st.Position, hitRadius, st.Damage * mult);

                if (st.Renderer != null)
                {
                    st.Renderer.transform.position = st.Position;
                    st.Renderer.transform.Rotate(0f, 0f, 720f * dt);
                }
                _stars[i] = st;
            }
        }

        private void HitAround(Vector2 center, float radius, float damage)
        {
            float now = Time.time;
            if (_nextHitTime.Count > 512) _nextHitTime.Clear(); // ids only grow; old entries are stale
            int n = Enemies.QueryCircle(center, radius, Scratch);
            for (int k = 0; k < n; k++)
            {
                Enemy e = Scratch[k];
                if (_nextHitTime.TryGetValue(e.Id, out float next) && now < next) continue;
                _nextHitTime[e.Id] = now + perEnemyHitCooldown;
                Enemies.DamageEnemy(e, RollDamage(damage));
            }
        }

        private void Remove(int index, in Star st)
        {
            if (st.Renderer != null) Vfx.Sprites.Release(st.Renderer);
            _stars.RemoveAt(index);
        }

        // ---------------------------------------------------------------- Galaxy Vortex

        private void SpawnVortex(in WeaponLevelStats s)
        {
            Camera cam = Camera.main;
            Vector2 center = cam != null
                ? (Vector2)cam.transform.position + new Vector2(Random.Range(-2f, 2f), cam.orthographicSize * 0.45f)
                : Origin + Vector2.up * 6f;

            float radius = AreaOf(s);
            SpriteRenderer r = Vfx != null && Vfx.CircleSprite != null
                ? Vfx.Sprites.Get(Vfx.CircleSprite, center, radius * 2f, vortexColor)
                : null;

            _vortices.Add(new Vortex
            {
                Center = center,
                Duration = DurationOf(s),
                Radius = radius,
                Damage = s.damage,
                Renderer = r
            });
        }

        private void TickVortices(float dt)
        {
            for (int i = _vortices.Count - 1; i >= 0; i--)
            {
                Vortex v = _vortices[i];
                v.Age += dt;

                int n = Enemies.QueryCircle(v.Center, v.Radius, Scratch);
                for (int k = 0; k < n; k++)
                {
                    Vector2 to = v.Center - (Vector2)Scratch[k].transform.position;
                    Scratch[k].Nudge(Vector2.ClampMagnitude(to, vortexPull * dt));
                }

                if (v.Age >= v.NextTick)
                {
                    v.NextTick = v.Age + vortexTick;
                    DamageArea(v.Center, v.Radius, v.Damage);
                }

                if (v.Renderer != null)
                    v.Renderer.transform.Rotate(0f, 0f, -360f * dt);

                if (v.Age >= v.Duration)
                {
                    Collapse(v);
                    _vortices.RemoveAt(i);
                    continue;
                }
                _vortices[i] = v;
            }
        }

        private void Collapse(in Vortex v)
        {
            if (v.Renderer != null) Vfx.Sprites.Release(v.Renderer);
            DamageArea(v.Center, v.Radius, v.Damage * 4f);

            if (fireworkBulletType >= 0)
            {
                for (int i = 0; i < fireworkCount; i++)
                {
                    float a = i * Mathf.PI * 2f / fireworkCount;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    BulletSystem.Instance.SpawnPlayerBullet(fireworkBulletType, v.Center, dir * 7f, RollDamage(v.Damage), 1, 1.5f);
                }
            }

            if (Vfx == null) return;
            Vfx.Pop(v.Center, v.Radius * 1.2f, vortexColor, 0.4f);
            Vfx.Confetti(v.Center, starColor, 30);
        }

        private void OnDestroy()
        {
            if (Vfx == null) return;
            foreach (Star st in _stars) if (st.Renderer != null) Vfx.Sprites.Release(st.Renderer);
            foreach (Vortex v in _vortices) if (v.Renderer != null) Vfx.Sprites.Release(v.Renderer);
        }
    }
}
