using System;
using System.Collections.Generic;
using PofudukFilo.Weapons;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace PofudukFilo.Progression
{
    public enum UpgradeKind
    {
        Weapon,
        Passive,
        Fallback
    }

    /// <summary>One card offered on level-up.</summary>
    public readonly struct UpgradeOption
    {
        public readonly UpgradeKind Kind;
        public readonly WeaponDefinition Weapon;
        public readonly PassiveDefinition Passive;
        public readonly Rarity Rarity;

        public UpgradeOption(WeaponDefinition weapon)
        {
            Kind = UpgradeKind.Weapon;
            Weapon = weapon;
            Passive = null;
            Rarity = weapon.rarity;
        }

        public UpgradeOption(PassiveDefinition passive)
        {
            Kind = UpgradeKind.Passive;
            Weapon = null;
            Passive = passive;
            Rarity = passive.rarity;
        }

        public UpgradeOption(UpgradeKind kind)
        {
            Kind = kind;
            Weapon = null;
            Passive = null;
            Rarity = Rarity.Common;
        }
    }

    /// <summary>
    /// Builds the 3-card level-up offer. Weight per card (game-concept.md §5):
    /// w = rarityWeight × (owned ? 1.6 : 1) × (on an evolution path ? 1.4 : 1).
    /// </summary>
    public sealed class UpgradeDraft : MonoBehaviour
    {
        private static readonly float[] RarityWeights = { 60f, 28f, 10f, 2f };

        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private List<WeaponDefinition> weaponPool = new();
        [SerializeField] private List<PassiveDefinition> passivePool = new();
        [SerializeField] private int choices = 3;
        [SerializeField] private float ownedMultiplier = 1.6f;
        [SerializeField] private float evolutionPathMultiplier = 1.4f;
        [SerializeField] private uint seed;

        private readonly List<UpgradeOption> _candidates = new(32);
        private readonly List<float> _weights = new(32);
        private readonly HashSet<ScriptableObject> _banished = new();
        private Random _random;

        /// <summary>Luck bonus shifts weight from Common to rarer cards (meta "Luck" upgrade).</summary>
        public float Luck { get; set; }

        private void Awake()
        {
            _random = new Random(seed != 0 ? seed : (uint)Environment.TickCount | 1u);
        }

        public void Banish(ScriptableObject card) => _banished.Add(card);

        public void ClearBanished() => _banished.Clear();

        public int Roll(List<UpgradeOption> results)
        {
            results.Clear();
            _candidates.Clear();
            _weights.Clear();

            foreach (WeaponDefinition w in weaponPool)
            {
                if (_banished.Contains(w) || !inventory.CanTake(w)) continue;
                WeaponBehaviour owned = inventory.Find(w);
                bool onPath = w.evolutionPassive != null && inventory.GetPassiveLevel(w.evolutionPassive) > 0;
                AddCandidate(new UpgradeOption(w), owned != null, onPath);
            }

            foreach (PassiveDefinition p in passivePool)
            {
                if (_banished.Contains(p) || !inventory.CanTake(p)) continue;
                bool owned = inventory.GetPassiveLevel(p) > 0;
                bool onPath = IsEvolutionKeyForOwnedWeapon(p);
                AddCandidate(new UpgradeOption(p), owned, onPath);
            }

            Span<float> weights = _weights.Count <= 64 ? stackalloc float[_weights.Count] : new float[_weights.Count];
            for (int i = 0; i < _weights.Count; i++) weights[i] = _weights[i];

            Span<int> picked = stackalloc int[choices];
            int count = WeightedPicker.PickDistinct(weights, picked, ref _random);
            for (int i = 0; i < count; i++) results.Add(_candidates[picked[i]]);

            // Everything maxed / slots full → gold or heal cards (game-concept.md §6).
            while (results.Count < choices) results.Add(new UpgradeOption(UpgradeKind.Fallback));
            return results.Count;
        }

        public void Apply(in UpgradeOption option)
        {
            switch (option.Kind)
            {
                case UpgradeKind.Weapon: inventory.AddOrLevelWeapon(option.Weapon); break;
                case UpgradeKind.Passive: inventory.AddOrLevelPassive(option.Passive); break;
                case UpgradeKind.Fallback: break; // Run director grants gold / heal.
            }
        }

        private void AddCandidate(in UpgradeOption option, bool owned, bool onEvolutionPath)
        {
            float w = RarityWeights[(int)option.Rarity];
            if (option.Rarity != Rarity.Common) w *= 1f + Luck;
            if (owned) w *= ownedMultiplier;
            if (onEvolutionPath) w *= evolutionPathMultiplier;
            _candidates.Add(option);
            _weights.Add(w);
        }

        private bool IsEvolutionKeyForOwnedWeapon(PassiveDefinition passive)
        {
            IReadOnlyList<WeaponBehaviour> weapons = inventory.Weapons;
            for (int i = 0; i < weapons.Count; i++)
                if (weapons[i].Definition.evolutionPassive == passive) return true;
            return false;
        }
    }
}
