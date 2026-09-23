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
        Revives
    }

    [CreateAssetMenu(menuName = "Pofuduk Filo/Passive", fileName = "Passive")]
    public sealed class PassiveDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public Rarity rarity = Rarity.Common;
        public StatType stat;
        [Tooltip("Bonus per level as a fraction (0.1 = +10 %).")]
        public float valuePerLevel = 0.1f;
        public int maxLevel = 5;
    }
}
