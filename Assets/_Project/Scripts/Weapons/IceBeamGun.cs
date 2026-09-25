using System.Collections.Generic;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Pengu's Buz Işını (hero-guns.md §3.3): a continuous ice beam straight up. It slows everything it touches, and
    /// an enemy that stays in the beam long enough freezes solid (stunned, cannot shoot). The beam hits the first
    /// 1 + pierce enemies from the bottom up. Grows by control: width, freeze time and more beams.
    /// Stats: cooldown = damage tick, <c>projectileCount</c> = beams, <c>area</c> = beam half-width,
    /// <c>pierce</c> = extra enemies per beam, <c>lifetime</c> = freeze seconds (0 = slow only).
    /// </summary>
    public sealed class IceBeamGun : WeaponBehaviour
    {
        [SerializeField] private float beamLength = 22f;
        [SerializeField] private float beamSpacing = 0.55f;
        [SerializeField] private float freezeAfterSeconds = 1.2f;
        [SerializeField] private Color beamColor = new(0.62f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color frozenColor = new(0.7f, 0.95f, 1f, 0.55f);
        [SerializeField] private Vector2 muzzleOffset = new(0f, 0.5f);

        /// <summary>Seconds each enemy (by id) has spent in the beam; decays when it leaves.</summary>
        private readonly Dictionary<int, float> _exposure = new(64);
        private readonly List<int> _decay = new(64);
        private readonly List<Enemy> _column = new(32);
        private readonly HashSet<int> _hitThisTick = new();

        protected override void Fire(in WeaponLevelStats s)
        {
            if (Enemies == null) return;
            int beams = Mathf.Max(1, s.projectileCount);
            float halfWidth = AreaOf(s);
            float tick = Stats.FinalCooldown(s.cooldown);
            _hitThisTick.Clear();

            for (int i = 0; i < beams; i++)
            {
                float offset = beams == 1 ? 0f : (i / (beams - 1f) - 0.5f) * beamSpacing * (beams - 1);
                Vector2 from = Origin + muzzleOffset + new Vector2(offset, 0f);
                Cast(from, halfWidth, s, tick);
            }

            // Enemies out of every beam thaw: exposure falls back so freezing needs a steady hold.
            _decay.Clear();
            foreach (KeyValuePair<int, float> e in _exposure)
                if (!_hitThisTick.Contains(e.Key)) _decay.Add(e.Key);
            foreach (int id in _decay)
            {
                float left = _exposure[id] - tick * 2f;
                if (left <= 0f) _exposure.Remove(id);
                else _exposure[id] = left;
            }
        }

        private void Cast(Vector2 from, float halfWidth, in WeaponLevelStats s, float tick)
        {
            // Everything in the column above the muzzle, nearest first.
            _column.Clear();
            Vector2 mid = from + Vector2.up * (beamLength * 0.5f);
            int n = Enemies.QueryCircle(mid, beamLength * 0.5f + halfWidth, Scratch);
            for (int i = 0; i < n; i++)
            {
                Enemy e = Scratch[i];
                Vector2 p = e.transform.position;
                if (p.y < from.y || Mathf.Abs(p.x - from.x) > halfWidth + e.HitRadius) continue;
                _column.Add(e);
            }
            _column.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            int reach = Mathf.Min(_column.Count, 1 + Mathf.Max(0, s.pierce));
            float top = from.y + beamLength;
            // A beam that ran out of pierce stops at the last enemy it could reach.
            if (reach > 0 && reach == 1 + Mathf.Max(0, s.pierce) && reach < _column.Count)
                top = _column[reach - 1].transform.position.y;

            for (int i = 0; i < reach; i++)
            {
                Enemy e = _column[i];
                _hitThisTick.Add(e.Id);
                e.Slow(0.5f, tick * 3f);
                _exposure.TryGetValue(e.Id, out float held);
                held += tick;
                _exposure[e.Id] = held;
                if (s.lifetime > 0f && held >= freezeAfterSeconds)
                {
                    e.Stun(s.lifetime);
                    _exposure[e.Id] = 0f; // frozen: the next freeze needs a fresh hold
                    if (Vfx != null) Vfx.Pop(e.transform.position, e.HitRadius * 1.8f, frozenColor, s.lifetime);
                }
                Enemies.DamageEnemy(e, RollDamage(s.damage));
            }

            if (Vfx != null)
            {
                Vfx.Segment(from, new Vector2(from.x, top), beamColor, halfWidth * 2f, tick * 1.6f);
                Vfx.Segment(from, new Vector2(from.x, top), new Color(1f, 1f, 1f, 0.85f), halfWidth * 0.6f, tick * 1.6f);
            }
        }

        protected override void OnLevelChanged() => _exposure.Clear();
    }
}
