using System.Collections.Generic;
using UnityEngine;

namespace FPSGame
{
    /// <summary>A simple pool of instances of one PooledObject prefab.</summary>
    public class ObjectPool
    {
        private readonly PooledObject prefab;
        private readonly Transform parent;
        private readonly Stack<PooledObject> available = new Stack<PooledObject>();

        public ObjectPool(PooledObject prefab, Transform parent, int prewarmCount)
        {
            this.prefab = prefab;
            this.parent = parent;
            Prewarm(prewarmCount);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                available.Push(CreateInstance());
            }
        }

        private PooledObject CreateInstance()
        {
            PooledObject instance = Object.Instantiate(prefab, parent);
            instance.AttachToPool(this);
            instance.MarkReleased();
            instance.gameObject.SetActive(false);
            return instance;
        }

        public PooledObject Get(Vector3 position, Quaternion rotation)
        {
            PooledObject instance = null;
            while (instance == null && available.Count > 0)
            {
                instance = available.Pop();
            }
            if (instance == null) instance = CreateInstance();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.MarkSpawned();
            instance.gameObject.SetActive(true);
            instance.OnSpawned();
            return instance;
        }

        public void Release(PooledObject instance)
        {
            if (instance == null || instance.IsInPool) return;
            instance.MarkReleased();
            instance.gameObject.SetActive(false);
            available.Push(instance);
        }
    }
}
