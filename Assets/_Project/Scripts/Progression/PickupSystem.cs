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
        [SerializeField] private float baseMagnetRadius = 1.5f;
        [SerializeField] private float collectRadius = 0.35f;
        [SerializeField] private float driftSpeed = 0.6f;
        [SerializeField] private float attractAcceleration = 30f;
        [SerializeField] private float popSpeed = 2.5f;
        [SerializeField] private float bottomMargin = 1f;

        [Header("Drops")]
        [SerializeField] private float coinChance = 0.12f;
        [SerializeField] private int goldPerCoinValue = 5;
        [SerializeField] private float heartChance = 0.004f;
        [SerializeField] private float magnetChance = 0.003f;
        [SerializeField] private float bombChance = 0.002f;
        [SerializeField] private int formationClearGems = 10;
        [SerializeField] private int mergeThreshold = 250;

        [Header("Effects")]
        [SerializeField] private float heartHealFraction = 0.2f;
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
                for (int i = 0; i < 5; i++) Spawn(PickupKind.Gold, pos, enemy.GoldValue * goldPerCoinValue);
                return;
            }

            if (Random.value < coinChance) Spawn(PickupKind.Gold, pos, enemy.GoldValue * goldPerCoinValue);

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
            Spawn(PickupKind.Gold, group.Center, goldPerCoinValue * 3);
        }

        private void OnGrazed(Vector2 position)
        {
            if (xpSystem != null) xpSystem.AddXp(grazeXp);
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
                    RunGold += c.Value;
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
