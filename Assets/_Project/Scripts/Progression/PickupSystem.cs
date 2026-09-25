using System;
using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Enemies;
using PofudukFilo.Player;
using PofudukFilo.Weapons;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PofudukFilo.Progression
{
    [Serializable]
    public struct PickupVisual
    {
        public Mesh mesh;
        public Material material;
        public float scale;
    }

    /// <summary>
    /// XP gems, gold and rare items as plain data (architecture.md §2 rule: many + simple → data).
    /// Drops on enemy death, flies to the player inside the magnet radius, merges past 250 gems.
    /// Spawns always go to a pending list, so they are safe while the job runs.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class PickupSystem : MonoBehaviour
    {
        public static PickupSystem Instance { get; private set; }

        [Header("References")]
        [SerializeField] private XpSystem xpSystem;
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private WaveDirector waveDirector;

        [Header("Visuals (index = PickupKind)")]
        [SerializeField] private PickupVisual[] visuals = new PickupVisual[7];
        [SerializeField] private int renderLayer;
        [SerializeField] private float renderZ = -0.5f;

        [Header("Movement")]
        [SerializeField] private float baseMagnetRadius = 2.4f; // QA run 13: gems piled up uncollected at 1.5
        [SerializeField] private float collectRadius = 0.35f;
        [SerializeField] private float driftSpeed = 1.6f; // gems rain toward the ship's zone
        [SerializeField] private float attractAcceleration = 30f;
        [Tooltip("Pickups fly to the ship by themselves after this long (device feedback 2026-09-24: uncollected gems " +
                 "carpeted the screen and were mistaken for shots). 0 = only the magnet radius.")]
        [SerializeField] private float autoCollectAfter = 1.2f;
        [SerializeField] private float popSpeed = 2.5f;
        [SerializeField] private float bottomMargin = 1f;

        [Header("Drops")]
        [SerializeField] private float coinChance = 0.04f; // horde (threat.md §3.6): 3.25× the kills, so a coin per kill is rarer (was 0.12)
        [SerializeField] private int goldPerCoinValue = 5;
        [SerializeField] private float heartChance = 0.00055f; // ×1/3.25 for the horde (threat.md §3.6); was 0.0018. Earlier: device feedback 2026-09-25: "ölmek imkansız" at 0.004; 0.0012 too stingy (QA 45)
        [SerializeField] private float magnetChance = 0f; // removed: pickups already fly to the ship on their own
        [SerializeField] private float bombChance = 0.0006f; // ×1/3.25 for the horde (was 0.002)
        [SerializeField] private int formationClearGems = 5;
        [SerializeField] private int mergeThreshold = 250;

        [Header("Effects")]
        [SerializeField] private float heartHealFraction = 0.15f;
        [SerializeField] private float magnetSeconds = 1.5f;
        [SerializeField] private float bombDamage = 200f;
        [SerializeField] private int grazeXp = 1;

        /// <summary>Kind, value, position — hook VFX/audio here.</summary>
        public event Action<PickupKind, int, Vector2> Collected;
        public event Action<int> RunGoldChanged;

        private NativeList<PickupData> _pickups;
        private NativeList<PickupData> _pending;
        private NativeQueue<PickupCollected> _collected;
        private JobHandle _handle;
        private bool _scheduled;
        private float _globalMagnetUntil;
        private int _xpCount;
        private InstancedDrawer _drawer;
        private Camera _camera;

        public int RunGold { get; private set; }
        public int Count => _pickups.Length;

        private void Awake()
        {
            Instance = this;
            _pickups = new NativeList<PickupData>(512, Allocator.Persistent);
            _pending = new NativeList<PickupData>(128, Allocator.Persistent);
            _collected = new NativeQueue<PickupCollected>(Allocator.Persistent);

            var meshes = new Mesh[visuals.Length];
            var materials = new Material[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
            {
                meshes[i] = visuals[i].mesh;
                materials[i] = visuals[i].material;
            }
            _drawer = new InstancedDrawer(meshes, materials, renderLayer);
        }

        private void Start()
        {
            _camera = Camera.main;
            EnemyManager.Instance.EnemyKilled += OnEnemyKilled;
            if (waveDirector != null) waveDirector.FormationCleared += OnFormationCleared;
            if (BulletSystem.Instance != null) BulletSystem.Instance.Grazed += OnGrazed;
        }

        private void OnDestroy()
        {
            _handle.Complete();
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyKilled -= OnEnemyKilled;
            if (waveDirector != null) waveDirector.FormationCleared -= OnFormationCleared;
            if (BulletSystem.Instance != null) BulletSystem.Instance.Grazed -= OnGrazed;
            _pickups.Dispose();
            _pending.Dispose();
            _collected.Dispose();
            if (Instance == this) Instance = null;
        }

        /// <summary>Clears pickups and the run's gold counter (new run).</summary>
        /// <summary>Resume a saved run (run-resume.md).</summary>
        public void RestoreRunGold(int gold)
        {
            RunGold = Mathf.Max(0, gold);
            RunGoldChanged?.Invoke(RunGold);
        }

        public void ResetRun()
        {
            _handle.Complete();
            _scheduled = false;
            _pickups.Clear();
            _pending.Clear();
            _collected.Clear();
            _xpCount = 0;
            RunGold = 0;
            RunGoldChanged?.Invoke(RunGold);
        }

        // ---------------------------------------------------------------- Spawning

        public void SpawnXp(Vector2 position, int value) => Spawn(XpKindFor(value), position, value);

        public void Spawn(PickupKind kind, Vector2 position, int value)
        {
            Vector2 pop = Random.insideUnitCircle * popSpeed + Vector2.up * popSpeed * 0.5f;
            _pending.Add(new PickupData
            {
                Position = position,
                Velocity = pop,
                Value = value,
                Kind = kind,
                Alive = true
            });
        }

        private static PickupKind XpKindFor(int value) => PickupRules.XpKindFor(value);

        private static bool IsXp(PickupKind kind) => PickupRules.IsXp(kind);

        private void OnEnemyKilled(Enemy enemy)
        {
            Vector2 pos = enemy.transform.position;
            SpawnXp(pos, enemy.XpValue);

            if (enemy.IsElite)
            {
                int eliteCoin = CoinValue(enemy.GoldValue * goldPerCoinValue * (1f + Bonus(StatType.EliteGold)));
                for (int i = 0; i < 5; i++) SpawnGold(pos, eliteCoin);
                return;
            }

            if (Random.value < coinChance) SpawnGold(pos, CoinValue(enemy.GoldValue * goldPerCoinValue));

            float roll = Random.value;
            if (roll < bombChance) Spawn(PickupKind.Bomb, pos, 0);
            else if (roll < bombChance + magnetChance) Spawn(PickupKind.Magnet, pos, 0);
            else if (roll < bombChance + magnetChance + heartChance) Spawn(PickupKind.Heart, pos, 0);
        }

        private void OnFormationCleared(FormationGroup group)
        {
            // "Formation Cleared!" XP shower (game-concept.md §3.3).
            for (int i = 0; i < formationClearGems; i++)
                Spawn(PickupKind.XpMedium, group.Center + Random.insideUnitCircle, 5);
            SpawnGold(group.Center, CoinValue(goldPerCoinValue * 3));
        }

        /// <summary>
        /// Coin value = base × Power coefficient (meta upgrades, set by RunController) × run-time factor, so a stronger
        /// player deeper in a run picks up bigger coins (economy.md §3.1).
        /// </summary>
        private int CoinValue(float baseValue)
        {
            float minutes = EnemyManager.Instance != null ? EnemyManager.Instance.RunMinutes : 0f;
            return Mathf.Max(1, Mathf.RoundToInt(Core.Formulas.CoinValue(baseValue, CoinPowerMultiplier, minutes)
                * Meta.Maps.Current.GoldMultiplier)); // harder maps pay more (maps.md §3)
        }

        private void SpawnGold(Vector2 pos, int value) => Spawn(PickupRules.GoldKindFor(value), pos, value);

        /// <summary>The player's Power coefficient (Formulas.PowerRating), applied to every coin this run.</summary>
        public float CoinPowerMultiplier { get; set; } = 1f;

        private void OnGrazed(Vector2 position)
        {
            if (xpSystem != null) xpSystem.AddXp(Mathf.RoundToInt(grazeXp * (1f + Bonus(StatType.GrazeXp))));
        }

        // ---------------------------------------------------------------- Frame flow

        private void Update()
        {
            MergePending();

            PlayerHealth player = PlayerHealth.Instance;
            if (player == null || _camera == null) return;

            float magnetMultiplier = inventory != null ? 1f + inventory.Stats.GetBonus(StatType.MagnetRadius) : 1f;
            float bottom = _camera.transform.position.y - _camera.orthographicSize - bottomMargin;

            NativeArray<PickupData> array = _pickups.AsArray();
            _handle = new UpdatePickupsJob
            {
                Pickups = array,
                DeltaTime = Time.deltaTime,
                PlayerPosition = (Vector2)player.transform.position,
                MagnetRadius = baseMagnetRadius * magnetMultiplier,
                CollectRadius = collectRadius,
                GlobalMagnet = Time.time < _globalMagnetUntil,
                AutoCollectAfter = autoCollectAfter,
                DriftSpeed = driftSpeed,
                AttractAcceleration = attractAcceleration,
                BottomY = bottom,
                Collected = _collected.AsParallelWriter()
            }.Schedule(array.Length, 64);
            _scheduled = true;
        }

        private void LateUpdate()
        {
            if (!_scheduled) return;
            _handle.Complete();
            _scheduled = false;

            while (_collected.TryDequeue(out PickupCollected c)) Apply(c);

            for (int i = _pickups.Length - 1; i >= 0; i--)
            {
                if (_pickups[i].Alive) continue;
                if (IsXp(_pickups[i].Kind)) _xpCount--;
                _pickups.RemoveAtSwapBack(i);
            }

            for (int i = 0; i < _pickups.Length; i++)
            {
                PickupData p = _pickups[i];
                float scale = visuals[(int)p.Kind].scale;
                _drawer.Add((int)p.Kind, p.Position, 0f, scale > 0f ? scale : 0.3f, renderZ);
            }
            _drawer.FlushAll();
        }

        /// <summary>
        /// Moves pending spawns into the simulation. Past the merge threshold, new XP is folded
        /// into the nearest idle gem instead (total XP preserved, game-concept.md §3.4).
        /// </summary>
        private void MergePending()
        {
            for (int i = 0; i < _pending.Length; i++)
            {
                PickupData p = _pending[i];
                if (IsXp(p.Kind) && _xpCount >= mergeThreshold && TryMergeIntoNearest(p))
                    continue;

                if (IsXp(p.Kind)) _xpCount++;
                _pickups.Add(p);
            }
            _pending.Clear();
        }

        private bool TryMergeIntoNearest(in PickupData gem)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _pickups.Length; i++)
            {
                PickupData p = _pickups[i];
                if (!p.Alive || p.Attracted || !IsXp(p.Kind)) continue;
                float sq = math.distancesq(p.Position, gem.Position);
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }
            if (best < 0) return false;

            PickupData target = _pickups[best];
            target.Value += gem.Value;
            target.Kind = XpKindFor(target.Value);
            _pickups[best] = target;
            return true;
        }

        private float Bonus(StatType stat) => inventory != null ? inventory.Stats.GetBonus(stat) : 0f;

        private void Apply(in PickupCollected c)
        {
            switch (c.Kind)
            {
                case PickupKind.XpSmall:
                case PickupKind.XpMedium:
                case PickupKind.XpLarge:
                    if (xpSystem != null) xpSystem.AddXp(c.Value);
                    break;
                case PickupKind.Gold:
                case PickupKind.GoldBig:
                case PickupKind.GoldBar:
                    RunGold += Mathf.RoundToInt(c.Value * (1f + Bonus(StatType.GoldGain)));
                    RunGoldChanged?.Invoke(RunGold);
                    break;
                case PickupKind.Magnet:
                    _globalMagnetUntil = Time.time + magnetSeconds;
                    break;
                case PickupKind.Heart:
                    PlayerHealth player = PlayerHealth.Instance;
                    if (player != null) player.Heal(player.MaxHp * heartHealFraction);
                    break;
                case PickupKind.Bomb:
                    if (BulletSystem.Instance != null) BulletSystem.Instance.RequestClearEnemyBullets();
                    EnemyManager.Instance.DamageAll(bombDamage);
                    break;
            }
            Collected?.Invoke(c.Kind, c.Value, c.Position);
        }
    }
}
