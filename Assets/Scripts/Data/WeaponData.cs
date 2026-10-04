using UnityEngine;

namespace FPSGame
{
    /// <summary>Weapon stats. The Pistol and Rifle assets are created by ProjectBootstrapper.</summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "FPSGame/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName = "WEAPON";
        [Tooltip("Automatic weapons fire while the trigger is held; semi-auto fires once per press.")]
        public bool automatic;
        public float damage = 20f;
        [Tooltip("Shots per second.")]
        public float fireRate = 5f;
        public int magazineSize = 12;
        public int startReserve = 48;
        public int maxReserve = 120;
        public float reloadTime = 1.3f;
        public float range = 100f;
        [Tooltip("Random spread, as a fraction of the aim direction.")]
        public float spread = 0.01f;
        [Tooltip("Upward camera kick per shot, in degrees.")]
        public float recoilPitch = 1.5f;
        [Tooltip("Random sideways camera kick per shot, in degrees.")]
        public float recoilYaw = 0.4f;
        public SoundType fireSound = SoundType.PistolShot;
    }
}
