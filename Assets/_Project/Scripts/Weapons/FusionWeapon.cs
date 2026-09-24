using System;
using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>Two evolved weapons that fuse into one legendary (weapon-system.md §3.4).</summary>
    [CreateAssetMenu(menuName = "Pofuduk Filo/Fusion Recipe", fileName = "Fusion")]
    public sealed class FusionRecipe : ScriptableObject
    {
        public WeaponDefinition a;
        public WeaponDefinition b;
        [Tooltip("Its behaviour prefab should be a FusionWeapon listing a and b as parts.")]
        public WeaponDefinition result;
        [Tooltip("Run-long damage bonus granted on fusion.")]
        public float damageBonus = 0.25f;
    }

    /// <summary>
    /// A fused weapon runs both evolved behaviours as children and occupies a single slot — the
    /// freed slot is the reward. The combined look comes from both effects overlapping.
    /// </summary>
    public sealed class FusionWeapon : WeaponBehaviour
    {
        [SerializeField] private WeaponDefinition[] parts = Array.Empty<WeaponDefinition>();

        private readonly List<WeaponBehaviour> _children = new(2);

        protected override void OnLevelChanged()
        {
            if (_children.Count > 0) return;
            foreach (WeaponDefinition part in parts)
            {
                if (part == null || part.behaviourPrefab == null) continue;
                WeaponBehaviour child = Instantiate(part.behaviourPrefab, transform);
                child.Initialize(part, Stats);
                _children.Add(child);
            }
        }

        /// <summary>The parts fire on their own cooldowns.</summary>
        protected override void Fire(in WeaponLevelStats stats) { }
    }
}
