using System.Collections.Generic;
using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// One per level scene. Keeps an ObjectPool per prefab (hit effects, pickups) so nothing
    /// is instantiated or destroyed while fighting.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Prewarm(GameObject prefab, int count)
        {
            ObjectPool pool = GetPool(prefab);
            if (pool != null) pool.Prewarm(count);
        }

        public PooledObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            ObjectPool pool = GetPool(prefab);
            return pool != null ? pool.Get(position, rotation) : null;
        }

        /// <summary>Spawns from the active level's pool. Does nothing outside a level.</summary>
        public static PooledObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Instance != null ? Instance.Spawn(prefab, position, rotation) : null;
        }

        private ObjectPool GetPool(GameObject prefab)
        {
            if (prefab == null) return null;

            if (pools.TryGetValue(prefab, out ObjectPool pool)) return pool;

            PooledObject pooledPrefab = prefab.GetComponent<PooledObject>();
            if (pooledPrefab == null)
            {
                Debug.LogWarning("[PoolManager] Prefab " + prefab.name + " has no PooledObject component.");
                return null;
            }

            pool = new ObjectPool(pooledPrefab, transform, 0);
            pools.Add(prefab, pool);
            return pool;
        }
    }
}
