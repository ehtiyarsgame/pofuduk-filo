using UnityEngine;
using UnityEngine.Pool;

namespace PofudukFilo.Pooling
{
    /// <summary>Optional hooks for pooled objects to reset their state.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>
    /// Thin wrapper over UnityEngine.Pool.ObjectPool for prefab components.
    /// Prewarm during loading — an Instantiate during a run is treated as a bug
    /// (docs/architecture/architecture.md §5).
    /// </summary>
    public sealed class PrefabPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly ObjectPool<T> _pool;

        public int CountActive => _pool.CountActive;

        public PrefabPool(T prefab, Transform parent, int defaultCapacity, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<T>(Create, OnGet, OnRelease, OnDestroyItem,
                collectionCheck: Application.isEditor, defaultCapacity, maxSize);
        }

        public void Prewarm(int count)
        {
            var buffer = new T[count];
            for (int i = 0; i < count; i++) buffer[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(buffer[i]);
        }

        public T Get(Vector3 position)
        {
            T item = _pool.Get();
            item.transform.position = position;
            // Called after positioning so OnSpawned sees the spawn position.
            if (item is IPoolable poolable) poolable.OnSpawned();
            return item;
        }

        public void Release(T item) => _pool.Release(item);

        private T Create()
        {
            T item = Object.Instantiate(_prefab, _parent);
            item.gameObject.SetActive(false);
            return item;
        }

        private static void OnGet(T item)
        {
            item.gameObject.SetActive(true);
        }

        private static void OnRelease(T item)
        {
            if (item is IPoolable poolable) poolable.OnDespawned();
            item.gameObject.SetActive(false);
        }

        private static void OnDestroyItem(T item)
        {
            if (item != null) Object.Destroy(item.gameObject);
        }
    }
}
