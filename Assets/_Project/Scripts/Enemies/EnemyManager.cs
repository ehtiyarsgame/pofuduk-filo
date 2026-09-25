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

        /// <summary>Raised on the main thread when an enemy dies. XP gems / VFX / audio / WaveDirector listen.
        /// The enemy stays valid until the next Update, when it returns to its pool.</summary>
        public event Action<Enemy> EnemyKilled;
        /// <summary>Every hit that lands (damage numbers, hit sparks). Fires before <see cref="EnemyKilled"/>.</summary>
        public event Action<Enemy, float> EnemyDamaged;

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

        /// <summary>Güç Eşleme: enemy HP follows the player's kill speed (power-match.md).</summary>
        public PowerMatch Power { get; } = new();
        private float _clock;

        /// <summary>New run: forget the last run's calibration.</summary>
        public void ResetPower()
        {
            Power.Reset();
            _clock = 0f;
        }

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
            enemy.Initialize(Formulas.EnemyHp(enemy.BaseHp, RunMinutes, ChapterIndex) * Power.Scale);
            _active.Add(enemy);
            return enemy;
        }

        /// <summary>Called by BulletSystem (main thread) with an index into this frame's proxies.</summary>
        public void ApplyDamage(int proxyIndex, float damage, Vector2 hitPosition)
        {
            if ((uint)proxyIndex >= (uint)Count) return;
            DamageEnemy(_active[proxyIndex], damage);
        }

        /// <summary>Damage from non-bullet sources (areas, chains, orbiters, bombs).</summary>
        /// <returns>True if the hit killed the enemy.</returns>
        public bool DamageEnemy(Enemy enemy, float damage)
        {
            if (enemy == null || enemy.IsDead || !IsOnScreen(enemy)) return false;
            bool killed = enemy.TakeDamage(damage);
            EnemyDamaged?.Invoke(enemy, damage);
            if (!killed) return false;

            enemy.Group?.OnMemberKilled();
            // Normal enemies only, and not during a rush (a 6 s fire-rate spike would teach the wrong baseline).
            if (!enemy.IsElite && enemy is not BossEnemy && enemy.SeenAt >= 0f && !SugarRush.RushActive)
                Power.RecordKill(_clock - enemy.SeenAt);
            EnemyKilled?.Invoke(enemy);
            _toDespawn.Add(enemy); // removed next Update, so indices and iteration stay valid this frame
            return true;
        }

        /// <summary>Living enemies whose hit circle overlaps the query circle. Clears <paramref name="results"/>.</summary>
        public int QueryCircle(Vector2 center, float radius, List<Enemy> results)
        {
            results.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy e = _active[i];
                if (e.IsDead || !IsOnScreen(e)) continue;
                float r = radius + e.HitRadius;
                if (((Vector2)e.transform.position - center).sqrMagnitude <= r * r) results.Add(e);
            }
            return results.Count;
        }

        /// <summary>Nearest living enemy within <paramref name="maxDistance"/> that is not in <paramref name="exclude"/>.</summary>
        public Enemy FindNearest(Vector2 position, float maxDistance, List<Enemy> exclude = null)
        {
            Enemy best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < _active.Count; i++)
            {
                Enemy e = _active[i];
                if (e.IsDead || !IsOnScreen(e) || (exclude != null && exclude.Contains(e))) continue;
                float sq = ((Vector2)e.transform.position - position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = e;
                }
            }
            return best;
        }

        private Camera _camera;
        private Vector2 _screenMin, _screenMax;

        /// <summary>
        /// Enemies can only be hit (and only shoot) once they are inside the visible playfield —
        /// device feedback: long-range builds were killing spawns before the player ever saw them.
        /// </summary>
        public bool IsOnScreen(Enemy e)
        {
            Vector2 p = e.transform.position;
            return p.y < _screenMax.y && p.y > _screenMin.y && p.x > _screenMin.x && p.x < _screenMax.x;
        }

        private void UpdateScreenBounds()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null)
            {
                _screenMin = new Vector2(-999f, -999f);
                _screenMax = new Vector2(999f, 999f);
                return;
            }
            Vector2 c = _camera.transform.position;
            float h = _camera.orthographicSize, w = h * _camera.aspect;
            // The enemy must be visibly on the playfield: not at the very edge, and out from behind the opaque HUD
            // panel (Playfield.TopY). Device feedback 2026-09-25: enemies showed through the old see-through plate
            // but could not be hit — now they are hidden there, so what you see you can hit.
            _screenMin = new Vector2(c.x - w - 0.3f, c.y - h - 2f);
            _screenMax = new Vector2(c.x + w + 0.3f, Playfield.TopY(_camera) - 0.15f);
        }

        /// <summary>Returns every enemy to its pool without kill events (new run / back to menu).</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _active.Count; i++) _pools[_active[i].SourcePrefab].Release(_active[i]);
            _active.Clear();
            _toDespawn.Clear();
            Count = 0;
        }

        /// <summary>Screen-wide bomb: damages every living enemy.</summary>
        public void DamageAll(float damage)
        {
            // Iterate by count snapshot; kills only queue despawns, so the list does not shift.
            int count = _active.Count;
            for (int i = 0; i < count; i++) DamageEnemy(_active[i], damage);
        }

        private void Update()
        {
            FlushDespawns();
            UpdateScreenBounds();

            float dt = Time.deltaTime;
            _clock += dt;
            Power.Tick(dt);
            Vector2 playerPos = PlayerHealth.Instance != null
                ? (Vector2)PlayerHealth.Instance.transform.position
                : Vector2.zero;

            for (int i = 0; i < _active.Count; i++)
            {
                Enemy e = _active[i];
                e.Tick(dt, playerPos);
                if (e.transform.position.y < despawnBelowY)
                {
                    e.Group?.OnMemberLost();
                    _toDespawn.Add(e);
                }
            }

            Count = _active.Count;
            for (int i = 0; i < Count; i++)
            {
                Enemy e = _active[i];
                bool visible = IsOnScreen(e);
                if (visible && e.SeenAt < 0f) e.SeenAt = _clock;
                _proxies[i] = new EnemyProxy
                {
                    // Off-screen enemies are parked far away so no bullet can reach them.
                    Position = visible ? (Vector2)e.transform.position : new Vector2(1e5f + i * 10f, 1e5f),
                    Radius = visible ? e.HitRadius : 0f,
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
