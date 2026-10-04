using UnityEngine;

namespace FPSGame
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    }
}
