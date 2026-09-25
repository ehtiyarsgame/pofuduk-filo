using UnityEngine;

namespace PofudukFilo.Weapons
{
    public enum StatType
    {
        Damage,
        CooldownReduction,
        Area,
        Duration,
        ProjectileSpeed,
        CritChance,
        MaxHp,
        MagnetRadius,
        Luck,
        // Meta-only stats (Workshop). Armor, Rerolls, Banishes, Revives are flat counts; Experience is a fraction.
        Armor,
        Experience,
        Rerolls,
        Banishes,
        Revives,
        // Hangar / Constellation rule modifiers (counts or fractions, see each consumer).
        StunDuration,           // +1 = stun lasts twice as long (Mırnav)
        AbsorbHeal,             // HP healed per enemy bullet absorbed (Balonbaş)
        EvolutionDamage,        // damage bonus gained per evolution this run (Yıldızpati)
        DraftChoices,           // extra level-up cards
        EliteGold,              // +% gold from elites
        GoldGain,               // +% gold from every pickup
        EvolutionChestLevels,   // extra passive levels granted by an evolution chest
        GrazeXp,                // +1 = graze XP doubled
        // In-run build cards (passives.md §3.2). Appended so serialized values of the stats above keep their meaning.
        ExtraProjectiles,       // +N shots / balls / bounces on every weapon that fires more than zero
        Pierce,                 // +N enemies each piercing shot passes through
        CritDamage,             // crit multiplier = 2 + value
        LowHpDamage,            // +% damage while below 40 % HP
        RushGain                // +% Sugar meter gain
    }

    /// <summary>A flat stat change from a character or constellation node.</summary>
    [System.Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public float value;

        public StatModifier(StatType stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }

    [CreateAssetMenu(menuName = "Pofuduk Filo/Passive", fileName = "Passive")]
    public sealed class PassiveDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public Rarity rarity = Rarity.Common;
        public StatType stat;
        [Tooltip("Bonus per level as a fraction (0.1 = +10 %).")]
        public float valuePerLevel = 0.1f;
        public int maxLevel = 5;
        [Tooltip("Optional cost of the card (Cam Top: damage for max HP). Applied per level like the bonus; 0 value = none.")]
        public StatType drawbackStat;
        public float drawbackPerLevel;
        [Tooltip("Weapon Lab price in gold; 0 = in the card pool from the start (meta-economy.md §3.3 C).")]
        public int labCost;
    }
}
