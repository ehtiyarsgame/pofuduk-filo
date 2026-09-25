using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Mırnav's Kıvılcım (hero-guns.md §3.3): no bullet at all. An instant arc leaps from the ship to the nearest
    /// enemy within range, then jumps on to nearby enemies. It needs no aim but has a short range, so the cat
    /// must fly up to the swarm. Grows by chain length.
    /// Stats: <c>projectileCount</c> = jumps after the first target, <c>area</c> = range from the ship,
    /// <c>lifetime</c> = stun seconds (0 = none), <c>effects</c> Slow = zapped enemies slow down.
    /// </summary>
    public sealed class ChainArcGun : WeaponBehaviour
    {
        [SerializeField] private float jumpRange = 3.5f;
        [SerializeField] private float falloffPerJump = 0.85f;
        [SerializeField] private Color boltColor = new(1f, 0.93f, 0.5f, 1f);
        [SerializeField] private Vector2 muzzleOffset = new(0f, 0.5f);

        private readonly List<Enemy> _chain = new(16);

        protected override void Fire(in WeaponLevelStats s)
        {
            if (Enemies == null) return;
            Vector2 from = Origin + muzzleOffset;
            _chain.Clear();
            Enemy target = Enemies.FindNearest(from, AreaOf(s), _chain);
            if (target == null) return;

            float damage = s.damage;
            int jumps = Mathf.Max(0, s.projectileCount);
            bool slow = (s.effects & BulletEffect.Slow) != 0;
            for (int k = 0; k <= jumps && target != null; k++)
            {
                Vector2 to = target.transform.position;
                Bolt(from, to, k == 0 ? 0.14f : 0.1f);
                _chain.Add(target);
                if (s.lifetime > 0f) target.Stun(s.lifetime);
                if (slow) target.Slow(0.55f, 1.2f);
                Enemies.DamageEnemy(target, RollDamage(damage));
                damage *= falloffPerJump;
                from = to;
                target = Enemies.FindNearest(from, jumpRange, _chain);
            }
        }

        /// <summary>A jagged three-part bolt: the midpoints are knocked sideways so it reads as electricity.</summary>
        private void Bolt(Vector2 from, Vector2 to, float width)
        {
            if (Vfx == null) return;
            Vector2 d = to - from;
            Vector2 n = new Vector2(-d.y, d.x).normalized * Mathf.Min(0.35f, d.magnitude * 0.15f);
            Vector2 a = from + d * 0.33f + n * Random.Range(-1f, 1f);
            Vector2 b = from + d * 0.66f + n * Random.Range(-1f, 1f);
            Vfx.Segment(from, a, boltColor, width, 0.09f);
            Vfx.Segment(a, b, boltColor, width, 0.09f);
            Vfx.Segment(b, to, boltColor, width, 0.09f);
            Vfx.Pop(to, 0.35f, new Color(boltColor.r, boltColor.g, boltColor.b, 0.6f), 0.1f);
        }
    }
}
