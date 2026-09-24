using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 7) Yarn Ball — Ball Blast's bouncing ball: yarn balls ricochet around the playfield, hurting every enemy
    /// they roll through (weapon-system.md §3.2). The weapon keeps <c>projectileCount</c> balls alive and
    /// relaunches any that expired; <c>area</c> is the ball radius. Lv5: each wall bounce adds damage.
    /// Evolved (Cosmic Yarn): one giant ball whose wall bounces unwind short-lived mini balls.
    /// </summary>
    public sealed class YarnBall : WeaponBehaviour
    {
        [SerializeField] private Sprite yarnSprite;
        [SerializeField] private Color yarnColor = Color.white;
        [SerializeField] private float perEnemyHitCooldown = 0.25f;
        [SerializeField] private float hudFraction = 0.24f;
        [SerializeField] private int bounceBonusFromLevel = 5;
        [SerializeField] private float bounceBonus = 0.1f;
        [SerializeField] private int maxBounceBonuses = 5;

        [Header("Evolution: Cosmic Yarn")]
        [SerializeField] private bool isEvolved;
        [SerializeField] private int maxMinis = 6;
        [SerializeField] private float miniScale = 0.45f;
        [SerializeField] private float miniSeconds = 3f;

        private struct Ball
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Radius;
            public float Age;
            public float Life;
            public float Damage;
            public int Bounces;
            public bool Mini;
        }

        private readonly List<Ball> _balls = new(12);
        private readonly Dictionary<int, float> _nextHitTime = new(64);

        protected override void Fire(in WeaponLevelStats s)
        {
            int wanted = isEvolved ? 1 : Mathf.Max(1, s.projectileCount);
            int main = 0;
            foreach (Ball b in _balls) if (!b.Mini) main++;
            if (main >= wanted) return;

            float speed = s.projectileSpeed * Stats.SpeedMultiplier;
            float angle = (90f + Random.Range(-35f, 35f)) * Mathf.Deg2Rad;
            Spawn(Origin + Vector2.up * 0.5f, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                AreaOf(s), DurationOf(s), s.damage, false);
        }

        private void Spawn(Vector2 position, Vector2 velocity, float radius, float life, float damage, bool mini)
        {
            SpriteRenderer r = Vfx != null && yarnSprite != null ? Vfx.Sprites.Get(yarnSprite, position, radius * 2f, yarnColor) : null;
            _balls.Add(new Ball { Renderer = r, Position = position, Velocity = velocity, Radius = radius, Life = life, Damage = damage, Mini = mini });
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime;
            if (dt <= 0f || Definition == null) return;

            Camera cam = Camera.main;
            Vector2 c = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
            float h = cam != null ? cam.orthographicSize : 10f;
            float w = cam != null ? h * cam.aspect : 5f;
            float top = c.y + h - h * hudFraction;
            if (_nextHitTime.Count > 512) _nextHitTime.Clear();

            int minis = 0;
            foreach (Ball b in _balls) if (b.Mini) minis++;

            for (int i = _balls.Count - 1; i >= 0; i--)
            {
                Ball b = _balls[i];
                b.Age += dt;
                if (b.Age >= b.Life)
                {
                    Remove(i, b);
                    continue;
                }

                b.Position += b.Velocity * dt;
                bool bounced = false;
                if (b.Position.x - b.Radius < c.x - w && b.Velocity.x < 0f) { b.Velocity.x = -b.Velocity.x; bounced = true; }
                if (b.Position.x + b.Radius > c.x + w && b.Velocity.x > 0f) { b.Velocity.x = -b.Velocity.x; bounced = true; }
                if (b.Position.y + b.Radius > top && b.Velocity.y > 0f) { b.Velocity.y = -b.Velocity.y; bounced = true; }
                if (b.Position.y - b.Radius < c.y - h && b.Velocity.y < 0f) { b.Velocity.y = -b.Velocity.y; bounced = true; }

                if (bounced && !b.Mini)
                {
                    b.Bounces++;
                    if (isEvolved && minis < maxMinis)
                    {
                        Vector2 v = new Vector2(-b.Velocity.y, b.Velocity.x) * 0.8f; // spin off sideways
                        Spawn(b.Position, v, b.Radius * miniScale, miniSeconds, b.Damage * 0.5f, true);
                        minis++;
                    }
                }

                float mult = 1f;
                if (!b.Mini && (Level >= bounceBonusFromLevel || isEvolved))
                    mult += bounceBonus * Mathf.Min(b.Bounces, maxBounceBonuses);
                HitAround(b.Position, b.Radius, b.Damage * mult);

                if (b.Renderer != null)
                {
                    b.Renderer.transform.position = b.Position;
                    b.Renderer.transform.Rotate(0f, 0f, -b.Velocity.x * 60f * dt);
                }
                _balls[i] = b;
            }
        }

        private void HitAround(Vector2 center, float radius, float damage)
        {
            float now = Time.time;
            int n = Enemies.QueryCircle(center, radius, Scratch);
            for (int k = 0; k < n; k++)
            {
                var e = Scratch[k];
                if (_nextHitTime.TryGetValue(e.Id, out float next) && now < next) continue;
                _nextHitTime[e.Id] = now + perEnemyHitCooldown;
                Enemies.DamageEnemy(e, RollDamage(damage));
            }
        }

        private void Remove(int index, in Ball b)
        {
            if (b.Renderer != null && Vfx != null) Vfx.Sprites.Release(b.Renderer);
            _balls.RemoveAt(index);
        }

        private void OnDestroy()
        {
            if (Vfx != null)
                foreach (Ball b in _balls)
                    if (b.Renderer != null) Vfx.Sprites.Release(b.Renderer);
            _balls.Clear();
        }
    }
}
