using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Settings popup (camera sensitivity, volume, Low/Medium/High quality, FPS counter),
    /// shared by the main menu and the pause menu. Values are saved through SaveManager
    /// (PlayerPrefs). Back closes it.
    /// </summary>
    public class SettingsPanel : MonoBehaviour, IBackHandler
    {
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Text sensitivityValueText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        [SerializeField] private Button[] qualityButtons = new Button[0];
        [SerializeField] private Button fpsToggleButton;
        [SerializeField] private Text fpsToggleLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Color selectedColor = new Color(0.18f, 0.42f, 0.85f, 1f);
        [SerializeField] private Color normalColor = new Color(0.25f, 0.28f, 0.36f, 1f);

        private bool listenersAdded;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            ArenaUITheme.StylePanel(gameObject);
            AddListeners();
        }

        private void OnEnable()
        {
            BackButtonManager.Push(this);
            Refresh();
        }

        private void OnDisable()
        {
            BackButtonManager.Remove(this);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!gameObject.activeSelf) return;
            AudioManager.PlaySound(SoundType.Click);
            if (SaveManager.Instance != null) SaveManager.Instance.Flush();
            gameObject.SetActive(false);
        }

        public bool OnBack()
        {
            Close();
            return true;
        }

        private void AddListeners()
        {
            if (listenersAdded) return;
            listenersAdded = true;

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = SaveManager.MinSensitivity;
                sensitivitySlider.maxValue = SaveManager.MaxSensitivity;
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] == null) continue;
                int quality = i;
                qualityButtons[i].onClick.AddListener(() => OnQualitySelected(quality));
            }

            if (fpsToggleButton != null) fpsToggleButton.onClick.AddListener(OnFpsToggled);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Refresh()
        {
            SaveManager save = SaveManager.Instance;
            if (save == null) return;

            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(save.Sensitivity);
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(save.Volume);
            UpdateLabels(save);
        }

        private void UpdateLabels(SaveManager save)
        {
            if (sensitivityValueText != null) sensitivityValueText.text = save.Sensitivity.ToString("0.0");
            if (volumeValueText != null) volumeValueText.text = NumberStrings.Get(Mathf.RoundToInt(save.Volume * 100f)) + "%";

            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] == null || qualityButtons[i].image == null) continue;
                qualityButtons[i].image.color = i == save.Quality ? ArenaUITheme.Crimson : ArenaUITheme.Gunmetal;
            }

            if (fpsToggleLabel != null) fpsToggleLabel.text = save.ShowFps ? "ON" : "OFF";
            if (fpsToggleButton != null && fpsToggleButton.image != null)
            {
                fpsToggleButton.image.color = save.ShowFps ? ArenaUITheme.Crimson : ArenaUITheme.Gunmetal;
            }
        }

        private void OnSensitivityChanged(float value)
        {
            SaveManager save = SaveManager.Instance;
            if (save == null) return;
            save.SetSensitivity(value);
            UpdateLabels(save);
        }

        private void OnVolumeChanged(float value)
        {
            SaveManager save = SaveManager.Instance;
            if (save == null) return;
            save.SetVolume(value);
            UpdateLabels(save);
        }

        private void OnQualitySelected(int quality)
        {
            SaveManager save = SaveManager.Instance;
            if (save == null) return;
            AudioManager.PlaySound(SoundType.Click);
            save.SetQuality(quality);
            UpdateLabels(save);
        }

        private void OnFpsToggled()
        {
            SaveManager save = SaveManager.Instance;
            if (save == null) return;
            AudioManager.PlaySound(SoundType.Click);
            save.SetShowFps(!save.ShowFps);
            UpdateLabels(save);
        }
    }
}
