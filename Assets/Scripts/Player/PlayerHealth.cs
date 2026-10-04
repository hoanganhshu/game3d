using UnityEngine;

namespace FPSGame
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public float Normalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;
        public bool IsAlive => CurrentHealth > 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!IsAlive || amount <= 0f || !GameManager.IsPlaying) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            AudioManager.PlaySound(SoundType.PlayerHurt, 0.8f);
            GameEvents.RaisePlayerDamaged();
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f) GameEvents.RaisePlayerDied();
        }

        /// <returns>true if any health was restored.</returns>
        public bool Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || CurrentHealth >= maxHealth) return false;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
            return true;
        }
    }
}
