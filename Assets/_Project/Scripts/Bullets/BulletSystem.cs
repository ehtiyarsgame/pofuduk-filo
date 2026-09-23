using System;
using PofudukFilo.Core;
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

        private InstancedDrawer _drawer;
        private readonly System.Collections.Generic.List<IBulletAbsorber> _absorbers = new();

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

            var meshes = new Mesh[bulletTypes.Length];
            var materials = new Material[bulletTypes.Length];
            for (int i = 0; i < bulletTypes.Length; i++)
            {
                meshes[i] = bulletTypes[i].mesh;
                materials[i] = bulletTypes[i].material;
                _maxBulletRadius = Mathf.Max(_maxBulletRadius, bulletTypes[i].hitRadius);
            }
            _drawer = new InstancedDrawer(meshes, materials, renderLayer);

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
            float lifetime = 8f, bool absorbable = true)
        {
            BulletData b = Create(typeIndex, position, velocity, damage, 0, lifetime);
            b.Absorbable = absorbable;
            _pendingEnemy.Add(b);
        }

        /// <summary>
        /// Weapons that eat enemy bullets (Bubble Orbit, Gum Rings) register here; absorption runs on
        /// the main thread in LateUpdate after the jobs complete.
        /// </summary>
        public void RegisterAbsorber(IBulletAbsorber absorber)
        {
            if (!_absorbers.Contains(absorber)) _absorbers.Add(absorber);
        }

        public void UnregisterAbsorber(IBulletAbsorber absorber) => _absorbers.Remove(absorber);

        /// <summary>Removes every bullet immediately (new run / back to menu).</summary>
        public void ClearAll()
        {
            _handle.Complete();
            _jobsScheduled = false;
            _playerBullets.Clear();
            _enemyBullets.Clear();
            _pendingPlayer.Clear();
            _pendingEnemy.Clear();
            _playerHits.Clear();
            _enemyHits.Clear();
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
            Absorb();

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

        private void Absorb()
        {
            if (_absorbers.Count == 0) return;

            for (int a = 0; a < _absorbers.Count; a++)
            {
                IBulletAbsorber absorber = _absorbers[a];
                for (int c = 0; c < absorber.AbsorberCount; c++)
                {
                    if (!absorber.CanAbsorb(c)) continue;
                    Vector2 center = absorber.GetAbsorberCenter(c);
                    float radius = absorber.GetAbsorberRadius(c);

                    for (int i = 0; i < _enemyBullets.Length; i++)
                    {
                        BulletData b = _enemyBullets[i];
                        if (!b.Alive || !b.Absorbable) continue;
                        float r = radius + b.Radius;
                        if (math.distancesq(b.Position, (float2)center) > r * r) continue;

                        b.Alive = false;
                        _enemyBullets[i] = b;
                        absorber.OnAbsorbed(c, b.Position);
                        if (!absorber.CanAbsorb(c)) break;
                    }
                }
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
                float angle = type.alignToVelocity
                    ? math.degrees(math.atan2(b.Velocity.y, b.Velocity.x)) - 90f
                    : 0f;
                _drawer.Add(b.TypeIndex, b.Position, angle, type.visualScale);
            }
            // Flush per list so enemy bullets are drawn after (on top of) player bullets.
            _drawer.FlushAll();
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
