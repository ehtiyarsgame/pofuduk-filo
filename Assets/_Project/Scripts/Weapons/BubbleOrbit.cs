using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 4) Bubble Orbit — gum bubbles circle the ship, hurting what they touch. Lv3+: each bubble
    /// eats one enemy bullet then recharges; Lv5: a full bubble pops for area damage.
    /// Evolved (Gum Rings): three rings — inner eats bullets without recharge, middle sticks and
    /// slows enemies, outer counter-rotates and cuts; every 20 bullets eaten fires gum meteors
    /// (weapon-system.md §3.2). Orbiters are continuous, so Fire() only drives the contact tick.
    /// </summary>
    public sealed class BubbleOrbit : WeaponBehaviour, IBulletAbsorber
    {
        [SerializeField] private Sprite bubbleSprite;
        [SerializeField] private Color bubbleColor = new(1f, 0.62f, 0.86f, 0.85f);
        [SerializeField] private Color recharging = new(1f, 0.62f, 0.86f, 0.35f);
        [SerializeField] private float bubbleRadius = 0.35f;
        [SerializeField] private float angularSpeedDeg = 160f;
        [SerializeField] private int absorbFromLevel = 3;
        [SerializeField] private float rechargeSeconds = 5f;
        [SerializeField] private int popFromLevel = 5;

        [Header("Evolution: Gum Rings")]
        [SerializeField] private bool isEvolved;
        [SerializeField] private int meteorEvery = 20;
        [SerializeField] private int meteorBulletType = -1;
        [SerializeField] private float slowFactor = 0.4f;
        [SerializeField] private Color[] ringColors =
        {
            new(1f, 0.62f, 0.86f, 0.9f),   // pembe
            new(0.5f, 0.88f, 0.77f, 0.9f), // mint
            new(0.78f, 0.7f, 1f, 0.9f)     // lila
        };

        private struct Bubble
        {
            public SpriteRenderer Renderer;
            public Vector2 Position;
            public float Radius;
            public int Ring;
            public float RechargeUntil;
        }

        private readonly List<Bubble> _bubbles = new(16);
        private float _angle;
        private int _absorbed;

        // ---------------------------------------------------------------- Layout

        protected override void OnLevelChanged() => Rebuild();

        private void OnEnable()
        {
            if (BulletSystem.Instance != null) BulletSystem.Instance.RegisterAbsorber(this);
        }

        private void OnDisable()
        {
            if (BulletSystem.Instance != null) BulletSystem.Instance.UnregisterAbsorber(this);
        }

        private void Rebuild()
        {
            ReleaseAll();
            WeaponLevelStats s = CurrentStats;
            int perRing = Mathf.Max(1, s.projectileCount);
            int rings = isEvolved ? 3 : 1;

            for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < perRing; i++)
            {
                float scale = bubbleRadius * 2f * (isEvolved ? 1.2f : 1f);
                Color color = isEvolved ? ringColors[ring % ringColors.Length] : bubbleColor;
                SpriteRenderer r = Vfx != null && bubbleSprite != null ? Vfx.Sprites.Get(bubbleSprite, Origin, scale, color) : null;
                _bubbles.Add(new Bubble { Renderer = r, Radius = bubbleRadius, Ring = ring });
            }
        }

        private float RingRadius(int ring)
        {
            float baseRadius = CurrentStats.area * Stats.AreaMultiplier;
            return baseRadius * (1f + ring * 0.55f);
        }

        // ---------------------------------------------------------------- Frame

        protected override void Update()
        {
            base.Update();
            if (Definition == null) return;

            float dt = Time.deltaTime;
            _angle += angularSpeedDeg * Stats.SpeedMultiplier * dt;

            int perRing = Mathf.Max(1, CurrentStats.projectileCount);
            for (int i = 0; i < _bubbles.Count; i++)
            {
                Bubble b = _bubbles[i];
                int slot = i % perRing;
                // Outer ring counter-rotates (Gum Rings).
                float dir = isEvolved && b.Ring == 2 ? -1f : 1f;
                float a = (dir * _angle + slot * 360f / perRing + b.Ring * 20f) * Mathf.Deg2Rad;
                b.Position = Origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * RingRadius(b.Ring);

                if (b.Renderer != null)
                {
                    b.Renderer.transform.position = b.Position;
                    if (!isEvolved)
                        b.Renderer.color = Time.time < b.RechargeUntil ? recharging : bubbleColor;
                }
                _bubbles[i] = b;
            }
        }

        /// <summary>The cooldown is the contact tick: each bubble damages what it touches.</summary>
        protected override void Fire(in WeaponLevelStats s)
        {
            for (int i = 0; i < _bubbles.Count; i++)
            {
                Bubble b = _bubbles[i];
                int n = Enemies.QueryCircle(b.Position, b.Radius, Scratch);
                for (int k = 0; k < n; k++)
                {
                    Enemy e = Scratch[k];
                    if (isEvolved && b.Ring == 1)
                    {
                        e.Slow(slowFactor, 1f); // middle ring: sticky
                        continue;
                    }
                    Enemies.DamageEnemy(e, RollDamage(s.damage));
                }
            }
        }

        // ---------------------------------------------------------------- IBulletAbsorber

        public int AbsorberCount => Level >= absorbFromLevel || isEvolved ? _bubbles.Count : 0;

        public Vector2 GetAbsorberCenter(int index) => _bubbles[index].Position;

        public float GetAbsorberRadius(int index) => _bubbles[index].Radius;

        public bool CanAbsorb(int index)
        {
            if (isEvolved) return _bubbles[index].Ring == 0;
            return Time.time >= _bubbles[index].RechargeUntil;
        }

        public void OnAbsorbed(int index, Vector2 bulletPosition)
        {
            Bubble b = _bubbles[index];
            _absorbed++;

            if (isEvolved)
            {
                if (meteorEvery > 0 && _absorbed % meteorEvery == 0) FireMeteors(b.Position);
                return;
            }

            b.RechargeUntil = Time.time + rechargeSeconds;
            _bubbles[index] = b;

            if (Level >= popFromLevel)
            {
                // A swollen bubble pops: area damage.
                DamageArea(b.Position, CurrentStats.area * 0.8f, CurrentStats.damage * 2f);
                if (Vfx != null) Vfx.Pop(b.Position, 1.2f, bubbleColor);
            }
        }

        private void FireMeteors(Vector2 from)
        {
            if (meteorBulletType < 0) return;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                BulletSystem.Instance.SpawnPlayerBullet(meteorBulletType, from, dir * 8f,
                    RollDamage(CurrentStats.damage * 3f), 2, 2f);
            }
            if (Vfx != null) Vfx.Confetti(from, ringColors[0], 12);
        }

        private void ReleaseAll()
        {
            if (Vfx != null)
                for (int i = 0; i < _bubbles.Count; i++)
                    if (_bubbles[i].Renderer != null) Vfx.Sprites.Release(_bubbles[i].Renderer);
            _bubbles.Clear();
        }

        private void OnDestroy() => ReleaseAll();
    }
}
