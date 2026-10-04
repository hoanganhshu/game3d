using UnityEngine;

namespace FPSGame
{
    public enum PickupType
    {
        Health,
        Ammo
    }

    /// <summary>
    /// Spinning health or ammo pickup. It checks distance to the player, which is cheaper and
    /// more reliable than trigger physics with a CharacterController. It returns to its pool
    /// when collected.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupType type;
        [SerializeField] private float healthAmount = 35f;
        [SerializeField] private int ammoMagazines = 2;
        [SerializeField] private float pickupRadius = 1.3f;
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 120f;
        [SerializeField] private float bobHeight = 0.15f;

        private PooledObject pooled;
        private Vector3 basePosition;
        private float phase;

        public PickupType Type => type;

        private void Awake()
        {
            pooled = GetComponent<PooledObject>();
        }

        private void OnEnable()
        {
            basePosition = transform.position;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (visual != null)
            {
                visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
                visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 3f + phase) * bobHeight, 0f);
            }

            if (!GameManager.IsPlaying) return;

            LevelManager level = LevelManager.Instance;
            if (level == null || level.PlayerTransform == null) return;

            Vector3 offset = level.PlayerTransform.position - basePosition;
            offset.y *= 0.5f;
            if (offset.sqrMagnitude > pickupRadius * pickupRadius) return;

            if (!TryApply(level)) return;

            AudioManager.PlaySound(SoundType.Pickup);
            if (pooled != null) pooled.Release();
            else Destroy(gameObject);
        }

        private bool TryApply(LevelManager level)
        {
            switch (type)
            {
                case PickupType.Health:
                    return level.PlayerStats != null && level.PlayerStats.Heal(healthAmount);
                case PickupType.Ammo:
                    return level.PlayerWeapons != null && level.PlayerWeapons.AddAmmoMagazines(ammoMagazines);
                default:
                    return false;
            }
        }
    }
}
