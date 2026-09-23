using System;
using System.Collections.Generic;
using PofudukFilo.Bullets;
using PofudukFilo.Core;
using PofudukFilo.Player;
using PofudukFilo.Pooling;
using Unity.Collections;
using UnityEngine;

namespace PofudukFilo.Enemies
{
    /// <summary>
    /// Owns every live enemy: spawns from pools, ticks them from a single loop and publishes a
    /// NativeArray of collision proxies for BulletSystem's jobs.
    ///
    /// Index stability: BulletSystem reports hits by proxy index in LateUpdate, so killed
    /// enemies are only removed at the start of the next Update (deferred despawn).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class EnemyManager : MonoBehaviour
    {
        public static EnemyManager Instance { get; private set; }

        [SerializeField] private int maxEnemies = 256;
        [SerializeField] private int prewarmPerPrefab = 48;
        [SerializeField] private Enemy[] prewarmPrefabs = Array.Empty<Enemy>();
        [SerializeField] private float despawnBelowY = -8f;

        /// <summary>Position, xp value, gold value. XP gems / VFX / audio listen to this.</summary>
        public event Action<Vector2, int, int> EnemyKilled;

        private readonly List<Enemy> _active = new(256);
        private readonly List<Enemy> _toDespawn = new(64);
        private readonly Dictionary<Enemy, PrefabPool<Enemy>> _pools = new();
        private NativeArray<EnemyProxy> _proxies;
        private Transform _poolRoot;

        public NativeArray<EnemyProxy> Proxies => _proxies;
        public int Count { get; private set; }
        public int ActiveCount => _active.Count;

        /// <summary>Minutes since the run started; drives HP scaling. Set by the run/wave director.</summary>
        public float RunMinutes { get; set; }
        public int ChapterIndex { get; set; }

        private void Awake()
        {
            Instance = this;
            _proxies = new NativeArray<EnemyProxy>(maxEnemies, Allocator.Persistent);
            _poolRoot = new GameObject("[EnemyPool]").transform;
            foreach (Enemy prefab in prewarmPrefabs)
                GetPool(prefab).Prewarm(prewarmPerPrefab);
        }

        private void OnDestroy()
        {
            // BulletSystem completes its jobs in its own OnDestroy/LateUpdate before we get here
            // in normal teardown; the proxies are only read by those jobs.
            if (_proxies.IsCreated) _proxies.Dispose();
            if (Instance == this) Instance = null;
        }

        public Enemy Spawn(Enemy prefab, Vector2 position)
        {
            if (_active.Count >= maxEnemies) return null;

            Enemy enemy = GetPool(prefab).Get(position);
            enemy.SourcePrefab = prefab;
            enemy.Initialize(Formulas.EnemyHp(enemy.BaseHp, RunMinutes, ChapterIndex));
            _active.Add(enemy);
            return enemy;
        }

        /// <summary>Called by BulletSystem (main thread) with an index into this frame's proxies.</summary>
        public void ApplyDamage(int proxyIndex, float damage, Vector2 hitPosition)
        {
            if ((uint)proxyIndex >= (uint)Count) return;
            Enemy enemy = _active[proxyIndex];
            if (enemy.TakeDamage(damage))
            {
                EnemyKilled?.Invoke(enemy.transform.position, enemy.XpValue, enemy.GoldValue);
                _toDespawn.Add(enemy);
            }
        }

        private void Update()
        {
            FlushDespawns();

            float dt = Time.deltaTime;
            Vector2 playerPos = PlayerHealth.Instance != null
                ? (Vector2)PlayerHealth.Instance.transform.position
                : Vector2.zero;

            for (int i = 0; i < _active.Count; i++)
            {
                Enemy e = _active[i];
                e.Tick(dt, playerPos);
                if (e.transform.position.y < despawnBelowY) _toDespawn.Add(e);
            }

            Count = _active.Count;
            for (int i = 0; i < Count; i++)
            {
                Enemy e = _active[i];
                _proxies[i] = new EnemyProxy
                {
                    Position = (Vector2)e.transform.position,
                    Radius = e.HitRadius,
                    Id = e.Id
                };
            }
        }

        private void FlushDespawns()
        {
            for (int i = 0; i < _toDespawn.Count; i++)
            {
                Enemy e = _toDespawn[i];
                int index = _active.IndexOf(e);
                if (index < 0) continue; // already removed (e.g. killed and left the screen the same frame)

                int last = _active.Count - 1;
                _active[index] = _active[last];
                _active.RemoveAt(last);
                _pools[e.SourcePrefab].Release(e);
            }
            _toDespawn.Clear();
        }

        private PrefabPool<Enemy> GetPool(Enemy prefab)
        {
            if (!_pools.TryGetValue(prefab, out PrefabPool<Enemy> pool))
            {
                pool = new PrefabPool<Enemy>(prefab, _poolRoot, prewarmPerPrefab, maxEnemies);
                _pools.Add(prefab, pool);
            }
            return pool;
        }
    }
}
