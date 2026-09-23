using System.Collections.Generic;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Feather Blaster evolution — Rainbow Prism Beam. A continuous beam to the top of the screen;
    /// the first enemy it touches refracts it into three side beams, which can refract again
    /// (a small fractal tree). Hue drifts along the beam (weapon-system.md §3.2).
    /// Cooldown is the damage tick; <c>projectileCount</c> is the refraction depth,
    /// <c>area</c> the beam half-width.
    /// </summary>
    public sealed class PrismBeam : WeaponBehaviour
    {
        [SerializeField] private float beamLength = 22f;
        [SerializeField] private float branchLength = 5f;
        [SerializeField] private float branchAngle = 35f;
        [SerializeField] private float branchDamageScale = 0.6f;
        [SerializeField] private Vector2 muzzleOffset = new(0f, 0.5f);

        private readonly List<Enemy> _hitThisTick = new(32);

        protected override void Fire(in WeaponLevelStats s)
        {
            _hitThisTick.Clear();
            float halfWidth = AreaOf(s);
            int depth = Mathf.Max(0, s.projectileCount);
            Cast(Origin + muzzleOffset, Vector2.up, beamLength, halfWidth, s.damage, depth, s.cooldown);
        }

        private void Cast(Vector2 from, Vector2 dir, float length, float halfWidth, float damage, int depth, float tick)
        {
            Enemy first = FirstAlong(from, dir, length, halfWidth, out float firstDistance);
            float visibleLength = first != null && depth > 0 ? firstDistance : length;

            // Everything along the (possibly shortened) beam takes damage.
            DamageAlong(from, dir, visibleLength, halfWidth, damage);

            if (Vfx != null)
            {
                float hue = Mathf.Repeat(Time.time * 0.5f + depth * 0.15f, 1f);
                Color c = Color.HSVToRGB(hue, 0.45f, 1f); // pastel
                c.a = 0.85f;
                Vfx.Segment(from, from + dir * visibleLength, c, halfWidth * 2f, tick * 1.5f);
            }

            if (first == null || depth <= 0) return;

            Vector2 hit = from + dir * firstDistance;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 branch = Quaternion.Euler(0f, 0f, i * branchAngle) * dir;
                Cast(hit + branch * 0.3f, branch, branchLength, halfWidth * 0.7f, damage * branchDamageScale, depth - 1, tick);
            }
        }

        private static Enemy FirstAlong(Vector2 from, Vector2 dir, float length, float halfWidth, out float distance)
        {
            Enemy best = null;
            distance = length;
            Vector2 mid = from + dir * (length * 0.5f);
            int n = Enemies.QueryCircle(mid, length * 0.5f + halfWidth, Scratch);
            for (int i = 0; i < n; i++)
            {
                if (!OnBeam(Scratch[i], from, dir, length, halfWidth, out float along)) continue;
                if (along < distance)
                {
                    distance = along;
                    best = Scratch[i];
                }
            }
            return best;
        }

        private void DamageAlong(Vector2 from, Vector2 dir, float length, float halfWidth, float damage)
        {
            Vector2 mid = from + dir * (length * 0.5f);
            int n = Enemies.QueryCircle(mid, length * 0.5f + halfWidth, Scratch);
            for (int i = 0; i < n; i++)
            {
                Enemy e = Scratch[i];
                // Each enemy takes beam damage once per tick, however many branches cross it.
                if (_hitThisTick.Contains(e) || !OnBeam(e, from, dir, length, halfWidth, out _)) continue;
                _hitThisTick.Add(e);
                Enemies.DamageEnemy(e, RollDamage(damage));
            }
        }

        private static bool OnBeam(Enemy e, Vector2 from, Vector2 dir, float length, float halfWidth, out float along)
        {
            Vector2 rel = (Vector2)e.transform.position - from;
            along = Vector2.Dot(rel, dir);
            if (along < 0f || along > length) return false;
            float perp = Mathf.Abs(rel.x * dir.y - rel.y * dir.x);
            return perp <= halfWidth + e.HitRadius;
        }
    }
}
