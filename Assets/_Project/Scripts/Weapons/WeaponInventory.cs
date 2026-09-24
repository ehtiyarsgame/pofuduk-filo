using System;
using System.Collections.Generic;
using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// The player's in-run loadout: max 4 weapons + 4 passives (game-concept.md §3.4),
    /// passive stat aggregation and evolution checks.
    /// </summary>
    public sealed class WeaponInventory : MonoBehaviour
    {
        public const int MaxWeapons = 4;
        public const int MaxPassives = 4;

        [SerializeField] private Transform weaponMount;
        [SerializeField] private WeaponDefinition startingWeapon;

        private readonly List<WeaponBehaviour> _weapons = new(MaxWeapons);
        private readonly Dictionary<PassiveDefinition, int> _passives = new(MaxPassives);

        public event Action<WeaponDefinition, WeaponDefinition> WeaponEvolved; // from, to
        public event Action PassivesChanged;
        public event Action<FusionRecipe> WeaponFused;

        public PlayerStats Stats { get; } = new();
        public IReadOnlyList<WeaponBehaviour> Weapons => _weapons;
        public IReadOnlyDictionary<PassiveDefinition, int> Passives => _passives;
        public bool HasFreeWeaponSlot => _weapons.Count < MaxWeapons;
        public bool HasFreePassiveSlot => _passives.Count < MaxPassives;

        public WeaponDefinition StartingWeapon
        {
            get => startingWeapon;
            set => startingWeapon = value;
        }

        /// <summary>Destroys the loadout and re-equips the starting weapon (new run).</summary>
        public void ResetLoadout()
        {
            for (int i = 0; i < _weapons.Count; i++) Destroy(_weapons[i].gameObject);
            _weapons.Clear();
            _passives.Clear();
            Stats.ClearPassiveBonuses();
            if (startingWeapon != null) AddOrLevelWeapon(startingWeapon);
        }

        /// <summary>Resume a saved run: exactly these weapons and passives at these levels (run-resume.md).</summary>
        public void RestoreLoadout(IReadOnlyList<(WeaponDefinition def, int level)> weapons,
            IReadOnlyList<(PassiveDefinition def, int level)> passives)
        {
            for (int i = 0; i < _weapons.Count; i++) Destroy(_weapons[i].gameObject);
            _weapons.Clear();
            _passives.Clear();
            Stats.ClearPassiveBonuses();
            foreach ((WeaponDefinition def, int level) in weapons)
            {
                if (def == null || _weapons.Count >= MaxWeapons) continue;
                WeaponBehaviour weapon = Instantiate(def.behaviourPrefab, weaponMount != null ? weaponMount : transform);
                weapon.Initialize(def, Stats, Mathf.Clamp(level, 1, def.MaxLevel));
                _weapons.Add(weapon);
            }
            foreach ((PassiveDefinition def, int level) in passives)
                if (def != null && _passives.Count < MaxPassives) _passives[def] = Mathf.Clamp(level, 1, def.maxLevel);
            RecalculatePassiveStats();
            PassivesChanged?.Invoke();
        }

        public WeaponBehaviour Find(WeaponDefinition definition)
        {
            for (int i = 0; i < _weapons.Count; i++)
                if (_weapons[i].Definition == definition) return _weapons[i];
            return null;
        }

        public int GetPassiveLevel(PassiveDefinition passive) =>
            _passives.TryGetValue(passive, out int level) ? level : 0;

        public bool CanTake(WeaponDefinition definition)
        {
            WeaponBehaviour owned = Find(definition);
            return owned != null ? !owned.IsMaxLevel : HasFreeWeaponSlot;
        }

        public bool CanTake(PassiveDefinition passive)
        {
            int level = GetPassiveLevel(passive);
            return level > 0 ? level < passive.maxLevel : HasFreePassiveSlot;
        }

        public void AddOrLevelWeapon(WeaponDefinition definition)
        {
            WeaponBehaviour owned = Find(definition);
            if (owned != null)
            {
                owned.LevelUp();
                return;
            }

            if (!HasFreeWeaponSlot) return;
            WeaponBehaviour weapon = Instantiate(definition.behaviourPrefab, weaponMount != null ? weaponMount : transform);
            weapon.Initialize(definition, Stats);
            _weapons.Add(weapon);
        }

        public void AddOrLevelPassive(PassiveDefinition passive)
        {
            int level = GetPassiveLevel(passive);
            if (level == 0 && !HasFreePassiveSlot) return;
            _passives[passive] = Mathf.Min(level + 1, passive.maxLevel);
            RecalculatePassiveStats();
            PassivesChanged?.Invoke();
        }

        /// <summary>Weapon at max level whose evolution passive is owned (weapon-system.md §3.2).</summary>
        public bool CanEvolve(WeaponBehaviour weapon) =>
            weapon.IsMaxLevel &&
            weapon.Definition.evolvesInto != null &&
            weapon.Definition.evolutionPassive != null &&
            GetPassiveLevel(weapon.Definition.evolutionPassive) > 0;

        /// <summary>Called when a boss/elite chest opens. Evolves every eligible weapon (the pity rule).</summary>
        public int EvolveAllEligible()
        {
            int evolved = 0;
            for (int i = 0; i < _weapons.Count; i++)
            {
                WeaponBehaviour old = _weapons[i];
                if (!CanEvolve(old)) continue;

                WeaponDefinition from = old.Definition;
                WeaponDefinition to = from.evolvesInto;
                WeaponBehaviour evolvedWeapon = Instantiate(to.behaviourPrefab, old.transform.parent);
                evolvedWeapon.Initialize(to, Stats);
                _weapons[i] = evolvedWeapon;
                Destroy(old.gameObject);

                evolved++;
                WeaponEvolved?.Invoke(from, to);
            }
            return evolved;
        }

        /// <summary>Fuses every recipe whose two evolved weapons are both owned. Returns the count.</summary>
        public int FuseAllEligible(IReadOnlyList<FusionRecipe> recipes)
        {
            int fused = 0;
            for (int r = 0; r < recipes.Count; r++)
            {
                FusionRecipe recipe = recipes[r];
                WeaponBehaviour a = Find(recipe.a);
                WeaponBehaviour b = Find(recipe.b);
                if (a == null || b == null) continue;

                _weapons.Remove(a);
                _weapons.Remove(b);
                Destroy(a.gameObject);
                Destroy(b.gameObject);

                WeaponBehaviour fusion = Instantiate(recipe.result.behaviourPrefab, weaponMount != null ? weaponMount : transform);
                fusion.Initialize(recipe.result, Stats);
                _weapons.Add(fusion);
                fused++;
                WeaponFused?.Invoke(recipe);
            }
            return fused;
        }

        private void RecalculatePassiveStats()
        {
            Stats.ClearPassiveBonuses();
            foreach (KeyValuePair<PassiveDefinition, int> pair in _passives)
                Stats.AddPassiveBonus(pair.Key.stat, pair.Key.valuePerLevel * pair.Value);
        }
    }
}
