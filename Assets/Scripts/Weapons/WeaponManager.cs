using UnityEngine;

namespace FPSGame
{
    /// <summary>Holds the player's weapons, passes input to the equipped one and switches weapons.</summary>
    [RequireComponent(typeof(PlayerInput))]
    public class WeaponManager : MonoBehaviour
    {
        [SerializeField] private Weapon[] weapons = new Weapon[0];
        [SerializeField] private Camera playerCamera;

        private PlayerInput input;
        private int currentIndex = -1;
        private bool alive = true;

        public Weapon CurrentWeapon =>
            currentIndex >= 0 && currentIndex < weapons.Length ? weapons[currentIndex] : null;

        private void Awake()
        {
            input = GetComponent<PlayerInput>();
            PlayerController controller = GetComponent<PlayerController>();
            Transform aim = playerCamera != null ? playerCamera.transform : transform;

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) continue;
                weapons[i].Initialize(aim, controller);
                weapons[i].gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Equip(0);
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += HandleDied;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= HandleDied;
        }

        private void HandleDied()
        {
            alive = false;
            Weapon weapon = CurrentWeapon;
            if (weapon != null) weapon.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!alive || !GameManager.IsPlaying || weapons.Length == 0) return;

            if (input.SelectWeapon >= 0) Equip(input.SelectWeapon);
            else if (input.SwitchPressed) Equip((currentIndex + 1) % weapons.Length);

            Weapon weapon = CurrentWeapon;
            if (weapon == null) return;

            if (input.ReloadPressed) weapon.StartReload();

            bool trigger = weapon.Data.automatic ? input.FireHeld : input.FirePressed;
            if (trigger) weapon.TryFire();
        }

        public void Equip(int index)
        {
            if (index < 0 || index >= weapons.Length || index == currentIndex || weapons[index] == null) return;

            Weapon previous = CurrentWeapon;
            if (previous != null) previous.gameObject.SetActive(false);

            currentIndex = index;
            Weapon next = weapons[currentIndex];
            next.gameObject.SetActive(true);
            GameEvents.RaiseAmmoChanged(next);
            if (previous != null) AudioManager.PlaySound(SoundType.Click, 0.5f);
        }

        /// <summary>Adds the given number of magazines of reserve ammo to every weapon.</summary>
        /// <returns>true if any weapon took ammo.</returns>
        public bool AddAmmoMagazines(int magazines)
        {
            bool added = false;
            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];
                if (weapon == null || weapon.Data == null) continue;
                if (weapon.AddAmmo(weapon.Data.magazineSize * magazines)) added = true;
            }
            return added;
        }
    }
}
