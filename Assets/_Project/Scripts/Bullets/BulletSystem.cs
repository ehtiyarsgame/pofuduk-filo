using System;
using PofudukFilo.Enemies;
using PofudukFilo.Player;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PofudukFilo.Bullets
{
    /// <summary>
    /// Data-oriented bullet simulation: Burst jobs for movement and collision, a spatial hash
    /// instead of Physics2D, and instanced rendering instead of GameObjects.
    /// Frame flow: Update schedules jobs → LateUpdate completes, applies hits, compacts, draws.
    /// See docs/architecture/architecture.md §3–4.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class BulletSystem : MonoBehaviour
    {
        private const int InstancesPerDraw = 1023;

        public static BulletSystem Instance { get; private set; }

        [SerializeField] private BulletTypeDefinition[] bulletTypes = Array.Empty<BulletTypeDefinition>();
        [SerializeField] private int playerBulletCapacity = 2048;
        [SerializeField] private int enemyBulletCapacity = 1024;
        [SerializeField] private float gridCellSize = 1f;
        [SerializeField] private float despawnMargin = 1.5f;
        [SerializeField] private float grazeRadius = 0.55f;
        [SerializeField] private int renderLayer;

        /// <summary>Raised on the main thread for each graze (near miss) — feeds bonus XP and UI.</summary>
        public event Action<Vector2> Grazed;

        private NativeList<BulletData> _playerBullets;
        private NativeList<BulletData> _enemyBullets;
        private NativeList<BulletData> _pendingPlayer;
        private NativeList<BulletData> _pendingEnemy;
        private NativeParallelMultiHashMap<int, int> _enemyGrid;
        private NativeQueue<BulletHit> _playerHits;
        private NativeQueue<BulletHit> _enemyHits;

        private JobHandle _handle;
        private bool _jobsScheduled;
        private bool _clearEnemyBulletsRequested;
        private float _maxBulletRadius;
        private float4 _bounds;

        private Matrix4x4[][] _matrices;
        private int[] _matrixCounts;
        private RenderParams[] _renderParams;

        public int PlayerBulletCount => _playerBullets.Length;
        public int EnemyBulletCount => _enemyBullets.Length;

        private void Awake()
        {
            Instance = this;

            _playerBullets = new NativeList<BulletData>(playerBulletCapacity, Allocator.Persistent);
            _enemyBullets = new NativeList<BulletData>(enemyBulletCapacity, Allocator.Persistent);
            _pendingPlayer = new NativeList<BulletData>(256, Allocator.Persistent);
            _pendingEnemy = new NativeList<BulletData>(256, Allocator.Persistent);
            _enemyGrid = new NativeParallelMultiHashMap<int, int>(4096, Allocator.Persistent);
            _playerHits = new NativeQueue<BulletHit>(Allocator.Persistent);
            _enemyHits = new NativeQueue<BulletHit>(Allocator.Persistent);

            _matrices = new Matrix4x4[bulletTypes.Length][];
            _matrixCounts = new int[bulletTypes.Length];
            _renderParams = new RenderParams[bulletTypes.Length];
            for (int i = 0; i < bulletTypes.Length; i++)
            {
                _matrices[i] = new Matrix4x4[InstancesPerDraw];
                _renderParams[i] = new RenderParams(bulletTypes[i].material) { layer = renderLayer };
                _maxBulletRadius = Mathf.Max(_maxBulletRadius, bulletTypes[i].hitRadius);
            }

            RecalculateBounds();
        }

        private void OnDestroy()
        {
            _handle.Complete();
            _playerBullets.Dispose();
            _enemyBullets.Dispose();
            _pendingPlayer.Dispose();
            _pendingEnemy.Dispose();
            _enemyGrid.Dispose();
            _playerHits.Dispose();
            _enemyHits.Dispose();
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- Public API

        /// <summary>Queue a player bullet. Safe to call at any time — it joins the simulation next schedule.</summary>
        public void SpawnPlayerBullet(int typeIndex, Vector2 position, Vector2 velocity, float damage,
            int pierce = 0, float lifetime = 3f)
        {
            _pendingPlayer.Add(Create(typeIndex, position, velocity, damage, pierce, lifetime));
        }

        public void SpawnEnemyBullet(int typeIndex, Vector2 position, Vector2 velocity, float damage,
            float lifetime = 8f)
        {
            _pendingEnemy.Add(Create(typeIndex, position, velocity, damage, 0, lifetime));
        }

        /// <summary>Bomb / Supernova Omelette: removes every enemy bullet after this frame's jobs finish.</summary>
        public void RequestClearEnemyBullets() => _clearEnemyBulletsRequested = true;

        public void RecalculateBounds()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            float halfH = cam.orthographicSize + despawnMargin;
            float halfW = cam.orthographicSize * cam.aspect + despawnMargin;
            Vector3 c = cam.transform.position;
            _bounds = new float4(c.x - halfW, c.y - halfH, c.x + halfW, c.y + halfH);
        }

        // ---------------------------------------------------------------- Frame flow

        private void Update()
        {
            _playerBullets.AddRange(_pendingPlayer.AsArray());
            _enemyBullets.AddRange(_pendingEnemy.AsArray());
            _pendingPlayer.Clear();
            _pendingEnemy.Clear();

            float dt = Time.deltaTime;
            EnemyManager enemies = EnemyManager.Instance;
            PlayerHealth player = PlayerHealth.Instance;

            // Player bullets: move → (grid) → collide with enemies.
            NativeArray<BulletData> playerArray = _playerBullets.AsArray();
            JobHandle playerMove = new MoveBulletsJob { Bullets = playerArray, DeltaTime = dt, Bounds = _bounds }
                .Schedule(playerArray.Length, 64);

            JobHandle playerChain = playerMove;
            if (enemies != null && enemies.Count > 0)
            {
                float inv = 1f / gridCellSize;
                JobHandle grid = new BuildEnemyGridJob
                {
                    Enemies = enemies.Proxies,
                    EnemyCount = enemies.Count,
                    InvCellSize = inv,
                    MaxBulletRadius = _maxBulletRadius,
                    Grid = _enemyGrid
                }.Schedule();

                playerChain = new PlayerBulletCollisionJob
                {
                    Bullets = playerArray,
                    Enemies = enemies.Proxies,
                    Grid = _enemyGrid,
                    InvCellSize = inv,
                    Hits = _playerHits.AsParallelWriter()
                }.Schedule(playerArray.Length, 64, JobHandle.CombineDependencies(playerMove, grid));
            }

            // Enemy bullets: move → collide with the single player.
            NativeArray<BulletData> enemyArray = _enemyBullets.AsArray();
            JobHandle enemyChain = new MoveBulletsJob { Bullets = enemyArray, DeltaTime = dt, Bounds = _bounds }
                .Schedule(enemyArray.Length, 64);

            if (player != null && player.IsAlive)
            {
                enemyChain = new EnemyBulletCollisionJob
                {
                    Bullets = enemyArray,
                    PlayerPosition = (Vector2)player.transform.position,
                    PlayerHitRadius = player.HitRadius,
                    PlayerGrazeRadius = grazeRadius,
                    Hits = _enemyHits.AsParallelWriter()
                }.Schedule(enemyArray.Length, 64, enemyChain);
            }

            _handle = JobHandle.CombineDependencies(playerChain, enemyChain);
            _jobsScheduled = true;
            JobHandle.ScheduleBatchedJobs();
        }

        private void LateUpdate()
        {
            if (!_jobsScheduled) return;
            _handle.Complete();
            _jobsScheduled = false;

            ApplyHits();

            if (_clearEnemyBulletsRequested)
            {
                _enemyBullets.Clear();
                _clearEnemyBulletsRequested = false;
            }

            Compact(_playerBullets);
            Compact(_enemyBullets);

            // Enemy bullets are drawn last so they sit on top of the player's (art-bible §2.2).
            Draw(_playerBullets);
            Draw(_enemyBullets);
        }

        private void ApplyHits()
        {
            EnemyManager enemies = EnemyManager.Instance;
            while (_playerHits.TryDequeue(out BulletHit hit))
                enemies.ApplyDamage(hit.EnemyIndex, hit.Damage, hit.Position);

            PlayerHealth player = PlayerHealth.Instance;
            while (_enemyHits.TryDequeue(out BulletHit hit))
            {
                if (hit.Kind == BulletHitKind.Player) player.TakeDamage(hit.Damage);
                else Grazed?.Invoke(hit.Position);
            }
        }

        private static void Compact(NativeList<BulletData> bullets)
        {
            for (int i = bullets.Length - 1; i >= 0; i--)
                if (!bullets[i].Alive) bullets.RemoveAtSwapBack(i);
        }

        private void Draw(NativeList<BulletData> bullets)
        {
            for (int i = 0; i < bullets.Length; i++)
            {
                BulletData b = bullets[i];
                BulletTypeDefinition type = bulletTypes[b.TypeIndex];

                Quaternion rotation = type.alignToVelocity
                    ? Quaternion.Euler(0f, 0f, math.degrees(math.atan2(b.Velocity.y, b.Velocity.x)) - 90f)
                    : Quaternion.identity;

                _matrices[b.TypeIndex][_matrixCounts[b.TypeIndex]++] =
                    Matrix4x4.TRS(new Vector3(b.Position.x, b.Position.y, 0f), rotation, Vector3.one * type.visualScale);

                if (_matrixCounts[b.TypeIndex] == InstancesPerDraw) Flush(b.TypeIndex);
            }

            for (int t = 0; t < bulletTypes.Length; t++)
                if (_matrixCounts[t] > 0) Flush(t);
        }

        private void Flush(int typeIndex)
        {
            Graphics.RenderMeshInstanced(_renderParams[typeIndex], bulletTypes[typeIndex].mesh, 0,
                _matrices[typeIndex], _matrixCounts[typeIndex]);
            _matrixCounts[typeIndex] = 0;
        }

        private BulletData Create(int typeIndex, Vector2 position, Vector2 velocity, float damage, int pierce, float lifetime)
        {
            return new BulletData
            {
                Position = position,
                Velocity = velocity,
                Radius = bulletTypes[typeIndex].hitRadius,
                Damage = damage,
                Lifetime = lifetime,
                Pierce = pierce,
                TypeIndex = typeIndex,
                LastHitEnemyId = -1,
                Alive = true
            };
        }
    }
}
