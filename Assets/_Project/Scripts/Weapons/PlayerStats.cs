using System;
using System.Collections.Generic;
using PofudukFilo.Core;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Run-time global stat modifiers: meta upgrades (set once at run start) plus passives.
    /// Weapons read their final values through here (weapon-system.md §4).
    /// </summary>
    public sealed class PlayerStats
    {
        private readonly Dictionary<StatType, float> _meta = new();
        private readonly Dictionary<StatType, float> _passive = new();
        private readonly Dictionary<StatType, float> _run = new();

        public void ClearMetaBonuses() => _meta.Clear();

        public void AddMetaBonus(StatType stat, float value)
        {
            _meta.TryGetValue(stat, out float current);
            _meta[stat] = current + value;
        }

        /// <summary>Character perks, constellation nodes and per-run gains (evolution damage).</summary>
        public void ClearRunBonuses() => _run.Clear();

        public void AddRunBonus(StatType stat, float value)
        {
            _run.TryGetValue(stat, out float current);
            _run[stat] = current + value;
        }

        public void ClearPassiveBonuses() => _passive.Clear();

        public void AddPassiveBonus(StatType stat, float value)
        {
            _passive.TryGetValue(stat, out float current);
            _passive[stat] = current + value;
        }

        public float GetBonus(StatType stat)
        {
            _meta.TryGetValue(stat, out float m);
            _passive.TryGetValue(stat, out float p);
            _run.TryGetValue(stat, out float r);
            return m + p + r;
        }

        public float DamageMultiplier => 1f + GetBonus(StatType.Damage);
        public float AreaMultiplier => 1f + GetBonus(StatType.Area);
        public float DurationMultiplier => 1f + GetBonus(StatType.Duration);
        public float SpeedMultiplier => 1f + GetBonus(StatType.ProjectileSpeed);
        public float CritChance => GetBonus(StatType.CritChance);

        /// <summary>
        /// Cooldown reductions stack multiplicatively with a 35 % floor, so meta and passive
        /// sources are kept as separate factors rather than summed.
        /// </summary>
        public float FinalCooldown(float baseCooldown)
        {
            Span<float> reductions = stackalloc float[2];
            int count = 0;
            if (_meta.TryGetValue(StatType.CooldownReduction, out float m) && m > 0f) reductions[count++] = m;
            if (_passive.TryGetValue(StatType.CooldownReduction, out float p) && p > 0f) reductions[count++] = p;
            return Formulas.FinalCooldown(baseCooldown, reductions.Slice(0, count));
        }
    }
}
