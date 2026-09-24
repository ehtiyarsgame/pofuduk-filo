using System.Collections.Generic;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 6) Fish Missile — little fish launch in a fan and home on the nearest enemy (weapon-system.md §3.2).
    /// Lv3+: a small splash on impact; pierce lets a fish swim on to the next target.
    /// Evolved (Shark Swarm): big sharks whose every hit releases pups that hunt on their own.
    /// Uses <c>projectileCount</c> fish per volley and <c>area</c> as the splash radius.
    /// </summary>
    public sealed class FishMissile : WeaponBehaviour
    {
        [SerializeField] private Sprite fishSprite;
        [SerializeField] private Color fishColor = Color.white;
        [SerializeField] private float fishScale = 0.55f;
        [SerializeField] private float hitRadius = 0.3f;
        [SerializeField] private float turnRateDeg = 320f;
        [SerializeField] private float seekRange = 16f;
        [SerializeField] private float launchSpread = 80f;
        [SerializeField] private float launchSeconds = 0.18f;
        [SerializeField] private Color splashColor = new(0.6f, 0.9f, 1f, 0.6f);

        [Header("Evolution: Shark Swarm")]
        [SerializeField] private bool isEvolved;
        [SerializeField] private int pupsPerHit = 2;
        [SerializeField] private float pupScale = 0.4f;
        [SerializeField] private float pupDamage = 0.45f;
        [SerializeField] private int maxFish = 40;

        private struct Fish
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Life;
            public float Damage;
            public Enemy Target;
            public int LastHitId;
            public int PierceLeft;
            public bool Pup;
        }

        private readonly List<Fish> _fish = new(24);

        protected override void Fire(in WeaponLevelStats s)
        {
            int count = Mathf.Max(1, s.projectileCount);
            float speed = s.projectileSpeed * Stats.SpeedMultiplier;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                float angle = (90f + t * launchSpread) * Mathf.Deg2Rad;
                Launch(Origin + Vector2.up * 0.4f, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    s.damage, s.pierce, DurationOf(s), false);
            }
        }

        private void Launch(Vector2 position, Vector2 velocity, float damage, int pierce, float life, bool pup)
        {
            if (_fish.Count >= maxFish) return;
            float scale = (pup ? pupScale : fishScale) * (isEvolved && !pup ? 1.5f : 1f);
            SpriteRenderer r = Vfx != null && fishSprite != null ? Vfx.Sprites.Get(fishSprite, position, scale, fishColor) : null;
            _fish.Add(new Fish
            {
                Renderer = r, Position = position, Velocity = velocity, Life = life,
                Damage = damage, PierceLeft = pierce, Pup = pup, LastHitId = -1
            });
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime;
            if (dt <= 0f || Definition == null) return;

            WeaponLevelStats s = CurrentStats;
            float turn = turnRateDeg * Mathf.Deg2Rad * dt;
            for (int i = _fish.Count - 1; i >= 0; i--)
            {
                Fish f = _fish[i];
                f.Age += dt;
                if (f.Age >= f.Life)
                {
                    Remove(i, f);
                    continue;
                }

                // Straight out of the tube for a moment, then hunt.
                if (f.Age > launchSeconds)
                {
                    if (f.Target == null || f.Target.IsDead || !Enemies.IsOnScreen(f.Target))
                        f.Target = Enemies.FindNearest(f.Position, seekRange);
                    if (f.Target != null)
                    {
                        Vector2 desired = ((Vector2)f.Target.transform.position - f.Position).normalized * f.Velocity.magnitude;
                        f.Velocity = Vector3.RotateTowards(f.Velocity, desired, turn, 0f);
                    }
                }
                f.Position += f.Velocity * dt;

                int n = Enemies.QueryCircle(f.Position, hitRadius, Scratch);
                Enemy hit = null;
                for (int k = 0; k < n; k++)
                    if (Scratch[k].Id != f.LastHitId) { hit = Scratch[k]; break; }
                if (hit != null && Impact(ref f, hit, s))
                {
                    Remove(i, f);
                    continue;
                }

                if (f.Renderer != null)
                {
                    f.Renderer.transform.position = f.Position;
                    f.Renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(f.Velocity.y, f.Velocity.x) * Mathf.Rad2Deg - 90f);
                }
                _fish[i] = f;
            }
        }

        /// <summary>Damages the target (and splash); true when the fish is spent.</summary>
        private bool Impact(ref Fish f, Enemy target, in WeaponLevelStats s)
        {
            Vector2 at = f.Position;
            Enemies.DamageEnemy(target, RollDamage(f.Damage));
            float splash = AreaOf(s);
            if (splash > 0f && !f.Pup)
            {
                DamageArea(at, splash, f.Damage * 0.5f);
                if (Vfx != null) Vfx.Pop(at, splash, splashColor, 0.2f);
            }

            if (isEvolved && !f.Pup)
            {
                float speed = f.Velocity.magnitude;
                for (int p = 0; p < pupsPerHit; p++)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Launch(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * 0.8f, f.Damage * pupDamage, 0, 2f, true);
                }
            }

            f.LastHitId = target.Id;
            f.Target = null;
            f.PierceLeft--;
            return f.PierceLeft < 0;
        }

        private void Remove(int index, in Fish f)
        {
            if (f.Renderer != null && Vfx != null) Vfx.Sprites.Release(f.Renderer);
            _fish.RemoveAt(index);
        }

        private void OnDestroy()
        {
            if (Vfx != null)
                foreach (Fish f in _fish)
                    if (f.Renderer != null) Vfx.Sprites.Release(f.Renderer);
            _fish.Clear();
        }
    }
}
