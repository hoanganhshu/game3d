using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Main menu: Play (goes to the highest unlocked level), Level Select, Settings, Quit.
    /// Back opens a "press again to quit" hint; a second press within 2 seconds quits.
    /// </summary>
    public class MainMenuUI : MonoBehaviour, IBackHandler
    {
        private const float ExitConfirmWindow = 2f;

        [SerializeField] private Button playButton;
        [SerializeField] private Text playLabel;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private GameObject exitHint;

        private float exitArmedUntil = -1f;

        private void Awake()
        {
            ArenaUITheme.StyleRoot(transform);
            ArenaUITheme.StyleSceneBackground(transform);
            Transform stripe = transform.Find("AccentStripe");
            if (stripe != null)
            {
                if (stripe.TryGetComponent(out Image stripeImage)) stripeImage.color = ArenaUITheme.RedGlow;
                RectTransform stripeRect = stripe as RectTransform;
                if (stripeRect != null)
                {
                    stripeRect.anchorMin = new Vector2(0.36f, stripeRect.anchorMin.y);
                    stripeRect.anchorMax = new Vector2(0.64f, stripeRect.anchorMax.y);
                    stripeRect.offsetMin = new Vector2(0f, stripeRect.offsetMin.y);
                    stripeRect.offsetMax = new Vector2(0f, stripeRect.offsetMax.y);
                }
            }
            Transform title = transform.Find("SafeArea/Title");
            if (title == null) title = transform.Find("Title");
            if (title != null && title.TryGetComponent(out Text titleText))
            {
                titleText.text = "MAIN MENU";
                ArenaUITheme.StyleText(titleText);
            }
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelect);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
            if (exitHint != null) exitHint.SetActive(false);
        }

        private void Start()
        {
            if (playLabel != null)
            {
                int unlocked = SaveManager.Instance != null ? SaveManager.Instance.UnlockedLevels : 1;
                int level = Mathf.Clamp(unlocked, 1, LevelDatabase.Count);
                playLabel.text = "PLAY  -  LEVEL " + level;
            }
        }

        private void OnEnable()
        {
            BackButtonManager.Push(this);
        }

        private void OnDisable()
        {
            BackButtonManager.Remove(this);
        }

        private void Update()
        {
            if (exitHint != null && exitHint.activeSelf && Time.unscaledTime >= exitArmedUntil)
            {
                exitHint.SetActive(false);
            }
        }

        public bool OnBack()
        {
            if (Time.unscaledTime < exitArmedUntil)
            {
                OnQuit();
                return true;
            }

            exitArmedUntil = Time.unscaledTime + ExitConfirmWindow;
            if (exitHint != null) exitHint.SetActive(true);
            return true;
        }

        private void OnPlay()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.PlayHighestUnlocked();
        }

        private void OnLevelSelect()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.LoadLevelSelect();
        }

        private void OnSettings()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void OnQuit()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
