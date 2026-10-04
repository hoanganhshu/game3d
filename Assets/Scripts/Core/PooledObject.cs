using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// An object that can be reused through an ObjectPool. If lifetime is above 0 it returns
    /// itself to the pool after that many seconds. Child particle systems restart on spawn.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1f;

        private ObjectPool pool;
        private float releaseTime = float.MaxValue;
        private ParticleSystem[] particleSystems;

        public bool IsInPool { get; private set; }

        private void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        internal void AttachToPool(ObjectPool owner)
        {
            pool = owner;
        }

        internal void MarkSpawned()
        {
            IsInPool = false;
        }

        internal void MarkReleased()
        {
            IsInPool = true;
        }

        public void OnSpawned()
        {
            releaseTime = lifetime > 0f ? Time.time + lifetime : float.MaxValue;

            if (particleSystems == null) return;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem ps = particleSystems[i];
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void Update()
        {
            if (Time.time >= releaseTime) Release();
        }

        public void Release()
        {
            releaseTime = float.MaxValue;
            if (pool != null) pool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
