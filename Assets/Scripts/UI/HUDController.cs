using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// In-game HUD: health bar, ammo in magazine and reserve, weapon name, enemies left, score,
    /// timer, crosshair, hit marker, damage flash, reload indicator and the pause button.
    /// It is driven entirely by GameEvents. Text only changes when a value changes.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private static readonly Color HealthyColor = new Color(0.12f, 0.9f, 0.42f, 1f);
        private static readonly Color MidHealthColor = new Color(1f, 0.72f, 0.12f, 1f);
        private static readonly Color LowHealthColor = new Color(0.95f, 0.25f, 0.2f, 1f);
        private static readonly Color AmmoColor = Color.white;
        private static readonly Color EmptyAmmoColor = new Color(1f, 0.3f, 0.25f, 1f);

        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Text healthText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text reserveText;
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text enemiesText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private GameObject reloadIndicator;
        [SerializeField] private Image damageFlash;
        [SerializeField] private CanvasGroup hitMarker;
        [SerializeField] private Button pauseButton;

        private Image healthFillImage;
        private Outline healthFillGlow;
        private float damageAlpha;
        private float hitAlpha;
        private int lastShownSecond = -1;
        private Color damageBaseColor = new Color(0.8f, 0f, 0f, 1f);

        private void Awake()
        {
            if (healthFill != null) healthFillImage = healthFill.GetComponent<Image>();
            if (healthFill != null && healthFill.parent != null)
            {
                Image background = healthFill.parent.GetComponent<Image>();
                if (background != null) background.color = new Color(0.018f, 0.025f, 0.04f, 0.98f);
                Outline outline = healthFill.parent.GetComponent<Outline>();
                if (outline == null) outline = healthFill.parent.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.18f, 0.75f, 1f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);
                BuildHealthBarDetails(healthFill.parent as RectTransform);
            }
            if (healthFillImage != null)
            {
                healthFillGlow = healthFill.GetComponent<Outline>();
                if (healthFillGlow == null) healthFillGlow = healthFill.gameObject.AddComponent<Outline>();
                healthFillGlow.effectDistance = new Vector2(2f, -2f);
            }
            if (healthText != null)
            {
                Shadow shadow = healthText.GetComponent<Shadow>();
                if (shadow == null) shadow = healthText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }
            if (damageFlash != null)
            {
                damageBaseColor = damageFlash.color;
                SetFlashAlpha(0f);
            }
            if (hitMarker != null) hitMarker.alpha = 0f;
            if (reloadIndicator != null) reloadIndicator.SetActive(false);
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPausePressed);
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.PlayerDamaged += OnPlayerDamaged;
            GameEvents.AmmoChanged += OnAmmoChanged;
            GameEvents.ReloadStateChanged += OnReloadStateChanged;
            GameEvents.EnemiesRemainingChanged += OnEnemiesRemainingChanged;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.HitConfirmed += OnHitConfirmed;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.PlayerDamaged -= OnPlayerDamaged;
            GameEvents.AmmoChanged -= OnAmmoChanged;
            GameEvents.ReloadStateChanged -= OnReloadStateChanged;
            GameEvents.EnemiesRemainingChanged -= OnEnemiesRemainingChanged;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.HitConfirmed -= OnHitConfirmed;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (damageAlpha > 0f)
            {
                damageAlpha = Mathf.Max(0f, damageAlpha - dt * 1.6f);
                SetFlashAlpha(damageAlpha);
            }

            if (hitAlpha > 0f && hitMarker != null)
            {
                hitAlpha = Mathf.Max(0f, hitAlpha - dt * 5f);
                hitMarker.alpha = hitAlpha;
            }

            LevelManager level = LevelManager.Instance;
            if (level != null && timeText != null)
            {
                int second = (int)level.ElapsedTime;
                if (second != lastShownSecond)
                {
                    lastShownSecond = second;
                    timeText.text = NumberStrings.FormatTime(second);
                }
            }
        }

        private void OnPausePressed()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.Pause();
        }

        private void OnHealthChanged(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (healthFill != null) healthFill.anchorMax = new Vector2(normalized, 1f);
            if (healthFillImage != null)
            {
                healthFillImage.color = normalized > 0.5f
                    ? Color.Lerp(MidHealthColor, HealthyColor, (normalized - 0.5f) * 2f)
                    : Color.Lerp(LowHealthColor, MidHealthColor, normalized * 2f);
            }
            if (healthText != null) healthText.text = NumberStrings.Get(Mathf.CeilToInt(current));
            if (healthFillGlow != null)
            {
                Color glow = healthFillImage != null ? healthFillImage.color : HealthyColor;
                glow.a = 0.65f;
                healthFillGlow.effectColor = glow;
            }
        }

        private void BuildHealthBarDetails(RectTransform track)
        {
            if (track == null || track.Find("HUDHighlight") != null) return;

            GameObject highlightObject = new GameObject("HUDHighlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform highlight = highlightObject.GetComponent<RectTransform>();
            highlight.SetParent(healthFill, false);
            highlight.anchorMin = new Vector2(0f, 0.58f);
            highlight.anchorMax = Vector2.one;
            highlight.offsetMin = new Vector2(2f, 0f);
            highlight.offsetMax = new Vector2(-2f, -2f);
            Image highlightImage = highlightObject.GetComponent<Image>();
            highlightImage.color = new Color(1f, 1f, 1f, 0.2f);
            highlightImage.raycastTarget = false;

            for (int i = 1; i < 5; i++)
            {
                GameObject tickObject = new GameObject("HealthTick" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform tick = tickObject.GetComponent<RectTransform>();
                tick.SetParent(track, false);
                float x = i / 5f;
                tick.anchorMin = new Vector2(x, 0.12f);
                tick.anchorMax = new Vector2(x, 0.88f);
                tick.sizeDelta = new Vector2(2f, 0f);
                Image tickImage = tickObject.GetComponent<Image>();
                tickImage.color = new Color(0f, 0f, 0f, 0.38f);
                tickImage.raycastTarget = false;
            }
        }

        private void OnPlayerDamaged()
        {
            damageAlpha = 0.45f;
            SetFlashAlpha(damageAlpha);
        }

        private void OnAmmoChanged(Weapon weapon)
        {
            if (weapon == null) return;
            if (ammoText != null)
            {
                ammoText.text = NumberStrings.Get(weapon.AmmoInMagazine);
                ammoText.color = weapon.AmmoInMagazine > 0 ? AmmoColor : EmptyAmmoColor;
            }
            if (reserveText != null) reserveText.text = NumberStrings.Get(weapon.ReserveAmmo);
            if (weaponNameText != null && weapon.Data != null) weaponNameText.text = weapon.Data.weaponName;
            if (reloadIndicator != null) reloadIndicator.SetActive(weapon.IsReloading);
        }

        private void OnReloadStateChanged(bool reloading, float duration)
        {
            if (reloadIndicator != null) reloadIndicator.SetActive(reloading);
        }

        private void OnEnemiesRemainingChanged(int remaining, int total)
        {
            if (enemiesText != null) enemiesText.text = NumberStrings.Get(remaining);
        }

        private void OnScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = NumberStrings.Get(score);
        }

        private void OnHitConfirmed(bool killed)
        {
            hitAlpha = killed ? 1.6f : 1f;
            if (hitMarker != null) hitMarker.alpha = 1f;
        }

        private void SetFlashAlpha(float alpha)
        {
            if (damageFlash == null) return;
            Color color = damageBaseColor;
            color.a = alpha;
            damageFlash.color = color;
        }
    }
}
