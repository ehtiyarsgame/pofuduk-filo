using System;
using PofudukFilo.Enemies;
using PofudukFilo.Player;
using UnityEngine;

namespace PofudukFilo.Core
{
    /// <summary>
    /// Şeker Hücumu — the signature risk/reward loop (design/gdd/sugar-rush.md). Kills chained
    /// within <see cref="comboWindow"/> build a combo; every kill fills the rush meter, scaled by
    /// the combo. Getting hit breaks the combo and spills half the meter, so dodging (the one
    /// skill an auto-shooter asks for) is what pays. A full meter triggers a timed rush: faster
    /// weapons and wingmen, doubled XP. Static multipliers are read by weapons and XP.
    /// </summary>
    public sealed class SugarRush : MonoBehaviour
    {
        public static SugarRush Instance { get; private set; }

        /// <summary>Weapon cooldowns tick this much faster during a rush (1 otherwise).</summary>
        public static float FireRate => Instance != null && Instance.Active ? Instance.rushFireRate : 1f;
        /// <summary>XP gained is multiplied by this during a rush (1 otherwise).</summary>
        public static float XpMultiplier => Instance != null && Instance.Active ? Instance.rushXpMultiplier : 1f;

        [SerializeField] private float comboWindow = 1.6f;
        [SerializeField] private float meterMax = 120f; // QA run 13: a rush every ~20 s at 70 — too routine
        [Tooltip("Meter per kill = killValue × (1 + combo × comboBonus).")]
        [SerializeField] private float comboBonus = 0.03f;
        [SerializeField] private float eliteKillValue = 10f;
        [SerializeField] private float bossKillValue = 35f;
        [SerializeField, Range(0f, 1f)] private float meterKeptOnHit = 0.5f;
        [SerializeField] private float rushSeconds = 6f;
        [SerializeField] private float rushFireRate = 1.7f;
        [SerializeField] private float rushXpMultiplier = 2f;

        public event Action<int> ComboChanged;
        public event Action<float> MeterChanged; // 0..1
        public event Action RushStarted;
        public event Action RushEnded;

        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public bool Active { get; private set; }
        public float Meter01 => Active ? _rushLeft / rushSeconds : _meter / meterMax;
        public float ComboTimeLeft01 => Combo > 0 ? _comboTimer / comboWindow : 0f;

        private float _meter;
        private float _comboTimer;
        private float _rushLeft;

        private void Awake() => Instance = this;

        private void Start()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled += OnKilled;
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Damaged += OnPlayerHit;
            if (RunController.Instance != null) RunController.Instance.StateChanged += OnState;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled -= OnKilled;
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.Damaged -= OnPlayerHit;
            if (RunController.Instance != null) RunController.Instance.StateChanged -= OnState;
        }

        private void OnState(GameState state)
        {
            if (state is GameState.MainMenu or GameState.RunEnd) ResetRun();
        }

        public void ResetRun()
        {
            bool wasActive = Active;
            Active = false;
            _meter = 0f;
            _rushLeft = 0f;
            SetCombo(0);
            BestCombo = 0;
            MeterChanged?.Invoke(0f);
            if (wasActive) RushEnded?.Invoke();
        }

        private void OnKilled(Enemy e)
        {
            SetCombo(Combo + 1);
            _comboTimer = comboWindow;
            if (Active) return;

            float value = e is BossEnemy ? bossKillValue : e.IsElite ? eliteKillValue : 1f;
            _meter += value * (1f + Combo * comboBonus);
            if (_meter >= meterMax) StartRush();
            else MeterChanged?.Invoke(_meter / meterMax);
        }

        private void OnPlayerHit(float damage)
        {
            if (damage <= 0f) return;
            SetCombo(0);
            if (Active) return;
            _meter *= meterKeptOnHit;
            MeterChanged?.Invoke(_meter / meterMax);
        }

        private void StartRush()
        {
            Active = true;
            _meter = 0f;
            _rushLeft = rushSeconds;
            RushStarted?.Invoke();
            MeterChanged?.Invoke(1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (Combo > 0)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f) SetCombo(0);
            }

            if (!Active) return;
            _rushLeft -= dt;
            MeterChanged?.Invoke(Mathf.Max(0f, _rushLeft / rushSeconds));
            if (_rushLeft <= 0f)
            {
                Active = false;
                RushEnded?.Invoke();
            }
        }

        private void SetCombo(int value)
        {
            if (value == Combo) return;
            Combo = value;
            if (Combo > BestCombo) BestCombo = Combo;
            ComboChanged?.Invoke(Combo);
        }
    }
}
