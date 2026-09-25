using System.Collections.Generic;
using PofudukFilo.Enemies;
using PofudukFilo.Feel;
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

        /// <summary>Any weapon fired this frame (ShipVisual: gun recoil and muzzle flash).</summary>
        public static event System.Action<WeaponBehaviour> AnyFired;

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

            _cooldownTimer -= Time.deltaTime * Core.SugarRush.FireRate * Forge.FireRate; // Şeker Hücumu and the Forge speed every weapon
            if (_cooldownTimer > 0f) return;

            WeaponLevelStats s = CurrentStats;
            // Hero gun mods (hero-guns.md §4): each hero gun's own permanent tracks.
            Meta.GunMods.Bonus mod = GunModState.For(Definition);
            _cooldownTimer = Stats.FinalCooldown(s.cooldown) / (1f + mod.FireRate);
            ShotCounter++;
            s.damage *= 1f + mod.Damage;
            s.area *= 1f + mod.Area;
            s.lifetime *= 1f + mod.Duration;
            if (s.projectileCount > 0) s.projectileCount += mod.Count;
            s.pierce += mod.Pierce;
            // Build cards (passives.md §3.2): Çift Namlu adds a shot/ball/bounce, Delici Pençe adds pierce.
            if (s.projectileCount > 0) s.projectileCount += Mathf.RoundToInt(Stats.GetBonus(StatType.ExtraProjectiles));
            s.pierce += Mathf.RoundToInt(Stats.GetBonus(StatType.Pierce));
            Fire(s);
            AnyFired?.Invoke(this);
        }

        /// <summary>Scratch list for enemy queries; weapons run on the main thread one at a time.</summary>
        protected static readonly List<Enemy> Scratch = new(64);

        protected static EnemyManager Enemies => EnemyManager.Instance;
        protected static VfxSystem Vfx => VfxSystem.Instance;
        protected Vector2 Origin => transform.position;

        protected float AreaOf(in WeaponLevelStats s) => s.area * Stats.AreaMultiplier;
        protected float DurationOf(in WeaponLevelStats s) => s.lifetime * Stats.DurationMultiplier;

        /// <summary>Damages every enemy in the circle. Returns the number hit.</summary>
        protected int DamageArea(Vector2 center, float radius, float damage)
        {
            int n = Enemies.QueryCircle(center, radius, Scratch);
            for (int i = 0; i < n; i++) Enemies.DamageEnemy(Scratch[i], RollDamage(damage));
            return n;
        }

        protected float RollDamage(float baseDamage)
        {
            float damage = baseDamage * Stats.DamageMultiplier * WeaponMastery.Multiplier(Definition) * Forge.DamageMultiplier;
            // Son Direniş: hits harder while the ship is below the low-HP line.
            Player.PlayerHealth hp = Player.PlayerHealth.Instance;
            if (hp != null && hp.MaxHp > 0f && hp.CurrentHp < hp.MaxHp * LowHpLine)
                damage *= 1f + Stats.GetBonus(StatType.LowHpDamage);
            return Random.value < Stats.CritChance ? damage * (2f + Stats.GetBonus(StatType.CritDamage)) : damage;
        }

        /// <summary>HP fraction under which Son Direniş (LowHpDamage) applies.</summary>
        public const float LowHpLine = 0.4f;

        protected abstract void Fire(in WeaponLevelStats stats);

        /// <summary>Hook for weapons whose visuals change with level (extra orbiters, cats…).</summary>
        protected virtual void OnLevelChanged() { }
    }
}
