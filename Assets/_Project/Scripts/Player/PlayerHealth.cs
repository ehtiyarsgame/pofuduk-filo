using System;
using UnityEngine;

namespace PofudukFilo.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        [SerializeField] private float baseMaxHp = 100f;
        [Tooltip("Real hitbox — much smaller than the sprite, shown as the glowing heart.")]
        [SerializeField] private float hitRadius = 0.18f;
        [SerializeField] private float invulnerabilitySeconds = 1.2f;

        public event Action<float, float> HealthChanged; // current, max
        public event Action Died;

        private float _invulnerableUntil;

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public float HitRadius => hitRadius;
        public bool IsAlive => CurrentHp > 0f;
        /// <summary>Flat damage reduction per hit from the meta "Armour" upgrade.</summary>
        public float Armor { get; set; }

        private void Awake()
        {
            Instance = this;
            SetMaxHp(baseMaxHp);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Applies meta/passive HP bonuses at run start.</summary>
        public void SetMaxHp(float maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            HealthChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || Time.time < _invulnerableUntil) return;

            CurrentHp = Mathf.Max(0f, CurrentHp - Mathf.Max(1f, amount - Armor));
            _invulnerableUntil = Time.time + invulnerabilitySeconds;
            HealthChanged?.Invoke(CurrentHp, MaxHp);

            if (!IsAlive) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            HealthChanged?.Invoke(CurrentHp, MaxHp);
        }

        /// <summary>Revive from the continue screen.</summary>
        public void Revive(float hpFraction = 0.5f)
        {
            CurrentHp = MaxHp * hpFraction;
            _invulnerableUntil = Time.time + 3f;
            HealthChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
