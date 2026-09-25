using System;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>Stats for one weapon level. Values mirror the tables in design/gdd/weapon-system.md §3.2.</summary>
    [Serializable]
    public struct WeaponLevelStats
    {
        public float damage;
        public float cooldown;
        public int projectileCount;
        public float spreadDegrees;
        public float projectileSpeed;
        public int pierce;
        public float area;
        public float lifetime;
        [Tooltip("Every Nth shot is a special shot (e.g. Feather Blaster Lv5 giant feather). 0 = off.")]
        public int specialEveryN;
        public float specialDamageMultiplier;
        [Tooltip("On-hit traits of this level's shots (straight shooters only): explode, split, chain, slow.")]
        public Bullets.BulletEffect effects;
        [Tooltip("Extra wing shots fanning out beside the lanes, in pairs (Tüy Blaster, hero-guns.md §3.3).")]
        public int sideShots;
        [Tooltip("Times each shot may bounce off walls or enemies, growing ×1.15 per bounce (Balonbaş, hero-guns.md §3.4).")]
        public int bounces;
        [Tooltip("Turn rate toward the nearest enemy in °/s; 0 = straight (Yıldızpati, hero-guns.md §3.4).")]
        public float homing;
        [TextArea] public string upgradeText;
    }

    /// <summary>
    /// Data for one weapon. Tuned in the inspector, never written at runtime
    /// (architecture.md §8 rule 4) — runtime level lives on the WeaponBehaviour.
    /// </summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Weapon", fileName = "Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("What the weapon does, shown on level-up cards and in the Armory (Turkish source; Loc translates).")]
        [TextArea] public string description;
        public Sprite icon;
        public Rarity rarity = Rarity.Common;

        [Tooltip("Index into BulletSystem's bullet type list.")]
        public int bulletTypeIndex;

        [Tooltip("Level 1..5 stats, index 0 = level 1.")]
        public WeaponLevelStats[] levels = new WeaponLevelStats[5];

        [Tooltip("Weapon Lab price in gold; 0 = in the card pool from the start (meta-economy.md §3.3 C).")]
        public int labCost;

        [Tooltip("A hero's own main gun: only in the card pool for the pilot who flies with it (hero-guns.md).")]
        public bool heroOnly;

        [Tooltip("Rewarded ads that also unlock it (ad-rewards.md); 0 = gold only.")]
        public int adsToUnlock;

        [Tooltip("Prefab carrying the WeaponBehaviour that fires this weapon.")]
        public WeaponBehaviour behaviourPrefab;

        [Header("Evolution")]
        [Tooltip("Passive that must be owned (any level) for this weapon to evolve at max level.")]
        public PassiveDefinition evolutionPassive;
        [Tooltip("Evolved weapon that replaces this one. Null for already-evolved weapons.")]
        public WeaponDefinition evolvesInto;

        public int MaxLevel => levels.Length;
        public bool IsEvolved => evolvesInto == null && evolutionPassive == null;

        public WeaponLevelStats GetStats(int level) => levels[Mathf.Clamp(level, 1, MaxLevel) - 1];
    }
}
