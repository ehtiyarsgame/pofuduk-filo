using System.Collections.Generic;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// 5) Spark Cat — a floating kitten throws chain lightning at the nearest enemy.
    /// Lv3: hit enemies are stunned (cannot shoot); Lv4: a second cat; Lv5: no damage falloff.
    /// Evolved (Storm Cat Goddess): lightning strikes random enemies and leaves electric daisies;
    /// every Nth strike the cat yawns and a 20-bounce chain crosses the screen
    /// (weapon-system.md §3.2). Uses <c>projectileCount</c> as bounces and <c>area</c> as bounce range.
    /// </summary>
    public sealed class SparkCat : WeaponBehaviour
    {
        [SerializeField] private Sprite catSprite;
        [SerializeField] private Color boltColor = new(1f, 0.93f, 0.55f);
        [SerializeField] private float firstTargetRange = 7f;
        [SerializeField] private float falloffPerBounce = 0.8f;
        [SerializeField] private int stunFromLevel = 3;
        [SerializeField] private float stunSeconds = 1f;
        [SerializeField] private int secondCatFromLevel = 4;
        [SerializeField] private int noFalloffFromLevel = 5;
        [SerializeField] private Vector2[] catOffsets = { new(-0.9f, 0.3f), new(0.9f, 0.3f) };

        [Header("Evolution: Storm Cat Goddess")]
        [SerializeField] private bool isEvolved;
        [SerializeField] private int strikesPerTick = 3;
        [SerializeField] private float daisyRadius = 0.8f;
        [SerializeField] private float daisySeconds = 1.5f;
        [SerializeField] private float daisyTick = 0.3f;
        [SerializeField] private int yawnBounces = 20;
        [SerializeField] private Color daisyColor = new(1f, 0.95f, 0.6f, 0.45f);

        private struct Daisy
        {
            public Vector2 Center;
            public float Age;
            public float NextTick;
            public float Damage;
            public SpriteRenderer Renderer;
        }

        private readonly List<SpriteRenderer> _cats = new(2);
        private readonly List<Enemy> _chain = new(24);
        private readonly List<Daisy> _daisies = new(16);

        protected override void OnLevelChanged()
        {
            int wanted = isEvolved ? 1 : Level >= secondCatFromLevel ? 2 : 1;
            while (_cats.Count < wanted && Vfx != null && catSprite != null)
                _cats.Add(Vfx.Sprites.Get(catSprite, Origin, isEvolved ? 1.6f : 0.6f, Color.white));
        }

        protected override void Update()
        {
            base.Update();
            if (Definition == null) return;

            for (int i = 0; i < _cats.Count; i++)
            {
                Vector2 target = CatPosition(i);
                Transform t = _cats[i].transform;
                t.position = Vector2.Lerp(t.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            TickDaisies(Time.deltaTime);
        }

        private Vector2 CatPosition(int index)
        {
            if (isEvolved)
            {
                // The goddess dozes at the top of the screen.
                Camera cam = Camera.main;
                return cam != null ? (Vector2)cam.transform.position + Vector2.up * (cam.orthographicSize - 1.5f) : Origin + Vector2.up * 8f;
            }
            return Origin + catOffsets[index % catOffsets.Length] + Vector2.up * (Mathf.Sin(Time.time * 3f + index) * 0.1f);
        }

        protected override void Fire(in WeaponLevelStats s)
        {
            if (isEvolved)
            {
                Storm(s);
                return;
            }

            int cats = Level >= secondCatFromLevel ? 2 : 1;
            for (int c = 0; c < cats; c++)
            {
                Vector2 from = c < _cats.Count ? (Vector2)_cats[c].transform.position : Origin;
                Chain(from, firstTargetRange, s.projectileCount, s.damage, AreaOf(s));
            }
        }

        /// <summary>Chain lightning from <paramref name="from"/>; each bounce jumps to the nearest unhit enemy.</summary>
        private void Chain(Vector2 from, float firstRange, int bounces, float damage, float bounceRange)
        {
            _chain.Clear();
            Vector2 point = from;
            float dmg = damage;
            float range = firstRange;

            for (int i = 0; i <= bounces; i++)
            {
                Enemy target = Enemies.FindNearest(point, range, _chain);
                if (target == null) break;

                Vector2 hit = target.transform.position;
                if (Vfx != null) Vfx.Segment(point, hit, boltColor, 0.1f, 0.12f);

                _chain.Add(target);
                if (Level >= stunFromLevel || isEvolved) target.Stun(stunSeconds);
                Enemies.DamageEnemy(target, RollDamage(dmg));

                if (Level < noFalloffFromLevel && !isEvolved) dmg *= falloffPerBounce;
                point = hit;
                range = bounceRange;
            }
        }

        // ---------------------------------------------------------------- Storm Cat Goddess

        private void Storm(in WeaponLevelStats s)
        {
            Camera cam = Camera.main;
            float radius = cam != null ? cam.orthographicSize * 2f : 12f;
            Vector2 center = cam != null ? (Vector2)cam.transform.position : Origin;

            int n = Enemies.QueryCircle(center, radius, Scratch);
            for (int i = 0; i < strikesPerTick && n > 0; i++)
            {
                Enemy target = Scratch[Random.Range(0, n)];
                Vector2 hit = target.transform.position;
                if (Vfx != null) Vfx.Segment(hit + Vector2.up * 6f, hit, boltColor, 0.16f, 0.15f);
                Enemies.DamageEnemy(target, RollDamage(s.damage));
                SpawnDaisy(hit, s.damage * 0.3f);
            }

            if (s.specialEveryN > 0 && ShotCounter % s.specialEveryN == 0)
            {
                Vector2 from = _cats.Count > 0 ? (Vector2)_cats[0].transform.position : Origin;
                Chain(from, radius, yawnBounces, s.damage * s.specialDamageMultiplier, radius);
            }
        }

        private void SpawnDaisy(Vector2 at, float damage)
        {
            SpriteRenderer r = Vfx != null && Vfx.CircleSprite != null
                ? Vfx.Sprites.Get(Vfx.CircleSprite, at, daisyRadius * 2f, daisyColor)
                : null;
            _daisies.Add(new Daisy { Center = at, Damage = damage, Renderer = r });
        }

        private void TickDaisies(float dt)
        {
            for (int i = _daisies.Count - 1; i >= 0; i--)
            {
                Daisy d = _daisies[i];
                d.Age += dt;
                if (d.Age >= d.NextTick)
                {
                    d.NextTick = d.Age + daisyTick;
                    DamageArea(d.Center, daisyRadius, d.Damage);
                }

                if (d.Age >= daisySeconds)
                {
                    if (d.Renderer != null) Vfx.Sprites.Release(d.Renderer);
                    _daisies.RemoveAt(i);
                    continue;
                }
                _daisies[i] = d;
            }
        }

        private void OnDestroy()
        {
            if (Vfx == null) return;
            foreach (SpriteRenderer cat in _cats) Vfx.Sprites.Release(cat);
            foreach (Daisy d in _daisies) if (d.Renderer != null) Vfx.Sprites.Release(d.Renderer);
        }
    }
}
