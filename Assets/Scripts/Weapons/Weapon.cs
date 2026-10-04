using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// A hitscan weapon: fire rate, magazine, reserve ammo, timed reload, spread, recoil,
    /// muzzle flash (light + mesh), pooled hit effects and a view-model kick animation.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        private const float FlashDuration = 0.05f;
        private const float EmptyClickDelay = 0.3f;

        [SerializeField] private WeaponData data;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private Transform model;

        public WeaponData Data => data;
        public Transform Muzzle => muzzle;
        public int AmmoInMagazine { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsEquipped => gameObject.activeInHierarchy;

        private Transform aimTransform;
        private PlayerController owner;
        private float nextFireTime;
        private float reloadEndTime;
        private float flashEndTime;
        private float kick;
        private Vector3 modelRestPosition;
        private Quaternion modelRestRotation = Quaternion.identity;
        private bool initialized;

        /// <summary>Called once by WeaponManager (also for weapons that start inactive).</summary>
        public void Initialize(Transform aim, PlayerController ownerController)
        {
            aimTransform = aim != null ? aim : transform;
            owner = ownerController;

            if (data == null)
            {
                Debug.LogError("[Weapon] " + name + " has no WeaponData; using defaults.");
                data = ScriptableObject.CreateInstance<WeaponData>();
            }

            AmmoInMagazine = data.magazineSize;
            ReserveAmmo = data.startReserve;

            if (model != null)
            {
                modelRestPosition = model.localPosition;
                modelRestRotation = model.localRotation;
            }

            SetFlash(false);
            initialized = true;
        }

        private void OnDisable()
        {
            if (IsReloading)
            {
                IsReloading = false;
                GameEvents.RaiseReloadStateChanged(false, 0f);
            }

            SetFlash(false);
            flashEndTime = 0f;
            kick = 0f;
            if (model != null && initialized)
            {
                model.localPosition = modelRestPosition;
                model.localRotation = modelRestRotation;
            }
        }

        private void Update()
        {
            if (!initialized) return;

            if (IsReloading && Time.time >= reloadEndTime) FinishReload();

            if (flashEndTime > 0f && Time.time >= flashEndTime)
            {
                SetFlash(false);
                flashEndTime = 0f;
            }

            AnimateModel();
        }

        /// <returns>true if a shot was fired.</returns>
        public bool TryFire()
        {
            if (!initialized || IsReloading || Time.time < nextFireTime) return false;

            if (AmmoInMagazine <= 0)
            {
                nextFireTime = Time.time + EmptyClickDelay;
                if (!StartReload()) AudioManager.PlaySound(SoundType.Empty, 0.6f);
                return false;
            }

            AmmoInMagazine--;
            nextFireTime = Time.time + 1f / Mathf.Max(0.01f, data.fireRate);

            FireRay();

            SetFlash(true);
            flashEndTime = Time.time + FlashDuration;
            kick = 1f;
            if (owner != null) owner.AddRecoil(data.recoilPitch, data.recoilYaw);
            AudioManager.PlaySound(data.fireSound, 0.7f);
            GameEvents.RaiseAmmoChanged(this);

            if (AmmoInMagazine == 0) StartReload();
            return true;
        }

        private void FireRay()
        {
            Vector2 spread = Random.insideUnitCircle * data.spread;
            Vector3 direction = (aimTransform.forward + aimTransform.right * spread.x + aimTransform.up * spread.y).normalized;

            if (!Physics.Raycast(aimTransform.position, direction, out RaycastHit hit, data.range,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            GameAssets assets = GameAssets.Instance;
            Quaternion effectRotation = Quaternion.LookRotation(hit.normal);

            if (hit.collider.TryGetComponent(out IDamageable target) && target.IsAlive)
            {
                target.TakeDamage(data.damage, hit.point, hit.normal);
                GameEvents.RaiseHitConfirmed(!target.IsAlive);
                AudioManager.PlaySound(SoundType.Hit, 0.45f);
                if (assets != null) PoolManager.SpawnFromPool(assets.bloodEffectPrefab, hit.point, effectRotation);
            }
            else if (assets != null)
            {
                PoolManager.SpawnFromPool(assets.impactEffectPrefab, hit.point + hit.normal * 0.02f, effectRotation);
            }
        }

        /// <returns>true if a reload started.</returns>
        public bool StartReload()
        {
            if (!initialized || IsReloading || AmmoInMagazine >= data.magazineSize || ReserveAmmo <= 0) return false;

            IsReloading = true;
            reloadEndTime = Time.time + data.reloadTime;
            AudioManager.PlaySound(SoundType.Reload, 0.8f);
            GameEvents.RaiseReloadStateChanged(true, data.reloadTime);
            return true;
        }

        private void FinishReload()
        {
            int needed = data.magazineSize - AmmoInMagazine;
            int taken = Mathf.Min(needed, ReserveAmmo);
            AmmoInMagazine += taken;
            ReserveAmmo -= taken;
            IsReloading = false;
            GameEvents.RaiseReloadStateChanged(false, 0f);
            GameEvents.RaiseAmmoChanged(this);
        }

        /// <returns>true if any ammo was added (false when reserve is already full).</returns>
        public bool AddAmmo(int amount)
        {
            if (!initialized || amount <= 0 || ReserveAmmo >= data.maxReserve) return false;

            ReserveAmmo = Mathf.Min(data.maxReserve, ReserveAmmo + amount);
            if (IsEquipped) GameEvents.RaiseAmmoChanged(this);
            return true;
        }

        private void SetFlash(bool on)
        {
            if (muzzleLight != null) muzzleLight.enabled = on;
            if (muzzleFlash != null) muzzleFlash.SetActive(on);
        }

        private void AnimateModel()
        {
            if (model == null) return;

            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 10f);

            float reloadTilt = 0f;
            if (IsReloading && data.reloadTime > 0f)
            {
                float progress = 1f - Mathf.Clamp01((reloadEndTime - Time.time) / data.reloadTime);
                reloadTilt = Mathf.Sin(progress * Mathf.PI) * 35f;
            }

            model.localPosition = modelRestPosition + new Vector3(0f, 0f, -0.05f * kick);
            model.localRotation = modelRestRotation * Quaternion.Euler(reloadTilt - 8f * kick, 0f, 0f);
        }
    }
}
