using PofudukFilo.Core;
using PofudukFilo.Weapons;
using UnityEngine;

namespace PofudukFilo.Meta
{
    /// <summary>
    /// One Workshop (Atölye) upgrade. Values from design/gdd/meta-economy.md §3.3 A,
    /// e.g. Damage: base 100, growth 1.35, max 10, +5 %/level.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Meta Upgrade", fileName = "MetaUpgrade")]
    public sealed class MetaUpgradeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public StatType stat;
        public float effectPerLevel = 0.05f;
        public int maxLevel = 10;
        public int baseCost = 100;
        public float costGrowth = 1.35f;

        /// <summary>Gold needed to go from <paramref name="currentLevel"/> to the next level.</summary>
        public int CostForNext(int currentLevel) => Formulas.MetaUpgradeCost(baseCost, costGrowth, currentLevel + 1);
    }
}
