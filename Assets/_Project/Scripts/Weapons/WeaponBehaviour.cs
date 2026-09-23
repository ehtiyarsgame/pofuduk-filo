using UnityEngine;

namespace PofudukFilo.Weapons
{
    /// <summary>
    /// Base for every weapon. Owns cooldown timing; subclasses only decide what a shot looks like.
    /// Lives as a child of the player so it follows the ship.
    /// </summary>
    public abstract class WeaponBehaviour : MonoBehaviour
    {
        private float _cooldownTimer;

        public WeaponDefinition Definition { get; private set; }
        public int Level { get; private set; }
        protected PlayerStats Stats { get; private set; }
        protected int ShotCounter { get; private set; }

        public bool IsMaxLevel => Level >= Definition.MaxLevel;

        public void Initialize(WeaponDefinition definition, PlayerStats stats, int level = 1)
        {
            Definition = definition;
            Stats = stats;
            Level = level;
            ShotCounter = 0;
            _cooldownTimer = 0f;
            OnLevelChanged();
        }

        public void LevelUp()
        {
            if (IsMaxLevel) return;
            Level++;
            OnLevelChanged();
        }

        protected WeaponLevelStats CurrentStats => Definition.GetStats(Level);

        protected virtual void Update()
        {
            if (Definition == null) return;

            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f) return;

            WeaponLevelStats s = CurrentStats;
            _cooldownTimer = Stats.FinalCooldown(s.cooldown);
            ShotCounter++;
            Fire(s);
        }

        protected float RollDamage(float baseDamage)
        {
            float damage = baseDamage * Stats.DamageMultiplier;
            return Random.value < Stats.CritChance ? damage * 2f : damage;
        }

        protected abstract void Fire(in WeaponLevelStats stats);

        /// <summary>Hook for weapons whose visuals change with level (extra orbiters, cats…).</summary>
        protected virtual void OnLevelChanged() { }
    }
}
