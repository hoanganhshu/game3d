using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// In-game screens: the Pause menu (Resume, Restart, Settings, Main Menu), the Win screen
    /// (score, stars, Next Level, Menu) and the Lose screen (Retry, Menu). It also handles Back
    /// in game: pause, resume, or leave after the level ended.
    /// </summary>
    public class InGameUI : MonoBehaviour, IBackHandler
    {
        [Header("Roots")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private SettingsPanel settingsPanel;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button pauseMenuButton;

        [Header("Win")]
        [SerializeField] private Text winScoreText;
        [SerializeField] private Text winDetailText;
        [SerializeField] private Text winBestText;
        [SerializeField] private Image[] winStars = new Image[0];
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button winMenuButton;
        [SerializeField] private Color starOnColor = new Color(1f, 0.8f, 0.15f, 1f);
        [SerializeField] private Color starOffColor = new Color(0.22f, 0.24f, 0.3f, 1f);

        [Header("Lose")]
        [SerializeField] private Button loseRetryButton;
        [SerializeField] private Button loseMenuButton;

        private void Awake()
        {
            StylePauseMenu();
            ArenaUITheme.StylePanel(winPanel);
            ArenaUITheme.StylePanel(losePanel);
            AddListener(resumeButton, OnResume);
            AddListener(restartButton, OnRestart);
            AddListener(pauseSettingsButton, OnOpenSettings);
            AddListener(pauseMenuButton, OnMainMenu);
            AddListener(nextLevelButton, OnNextLevel);
            AddListener(winMenuButton, OnMainMenu);
            AddListener(loseRetryButton, OnRestart);
            AddListener(loseMenuButton, OnMainMenu);

            SetActive(pausePanel, false);
            SetActive(winPanel, false);
            SetActive(losePanel, false);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
        }

        private void StylePauseMenu()
        {
            if (pausePanel == null) return;
            ArenaUITheme.StylePanel(pausePanel);
            Transform box = pausePanel.transform.Find("Box");
            if (box == null) return;

            Image panel = box.GetComponent<Image>();
            if (panel != null) panel.color = ArenaUITheme.Panel;
            Outline frame = box.GetComponent<Outline>();
            if (frame == null) frame = box.gameObject.AddComponent<Outline>();
            frame.effectColor = ArenaUITheme.Silver;
            frame.effectDistance = new Vector2(3f, -3f);

            Text title = box.Find("Title") != null ? box.Find("Title").GetComponent<Text>() : null;
            if (title != null)
            {
                title.text = "PAUSED";
                title.color = Color.white;
            }

            AddAccent(box, "TopAccent", new Vector2(0f, 345f), new Vector2(560f, 6f), ArenaUITheme.RedGlow);
            AddAccent(box, "BottomAccent", new Vector2(0f, -345f), new Vector2(560f, 2f), ArenaUITheme.Silver);
            ArenaUITheme.StyleButton(resumeButton, true);
            ArenaUITheme.StyleButton(restartButton);
            ArenaUITheme.StyleButton(pauseSettingsButton);
            ArenaUITheme.StyleButton(pauseMenuButton, false, true);
        }

        private static void StyleButton(Button button, Color color)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = color;
            Outline outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.85f, 1f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.7f, 0.85f, 0.9f, 1f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        private static void AddAccent(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            if (parent.Find(name) != null) return;
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
        }

        private void OnEnable()
        {
            GameManager.StateChanged += OnStateChanged;
            GameEvents.LevelCompleted += OnLevelCompleted;
            GameEvents.LevelFailed += OnLevelFailed;
            BackButtonManager.Push(this);
        }

        private void OnDisable()
        {
            GameManager.StateChanged -= OnStateChanged;
            GameEvents.LevelCompleted -= OnLevelCompleted;
            GameEvents.LevelFailed -= OnLevelFailed;
            BackButtonManager.Remove(this);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }

        private void OnStateChanged(GameState state)
        {
            SetActive(pausePanel, state == GameState.Paused);
            SetActive(hudRoot, state == GameState.Playing || state == GameState.Paused);

            if (state != GameState.Paused && settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.gameObject.SetActive(false);
            }
        }

        private void OnLevelCompleted(LevelResult result)
        {
            SetActive(winPanel, true);

            if (winScoreText != null) winScoreText.text = "SCORE  " + result.score;
            if (winDetailText != null)
            {
                winDetailText.text = "TIME " + NumberStrings.FormatTime(result.time) +
                                     "   |   HEALTH " + Mathf.RoundToInt(result.healthPercent * 100f) + "%" +
                                     "   |   KILLS " + result.kills;
            }
            if (winBestText != null) winBestText.text = result.isNewBest ? "NEW BEST SCORE!" : "BEST  " + result.bestScore;

            for (int i = 0; i < winStars.Length; i++)
            {
                if (winStars[i] != null) winStars[i].color = i < result.stars ? starOnColor : starOffColor;
            }

            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(result.hasNextLevel);
        }

        private void OnLevelFailed()
        {
            SetActive(losePanel, true);
        }

        public bool OnBack()
        {
            GameManager game = GameManager.Instance;
            if (game == null) return false;

            switch (game.State)
            {
                case GameState.Playing:
                    game.Pause();
                    return true;
                case GameState.Paused:
                    game.Resume();
                    return true;
                case GameState.Won:
                case GameState.Lost:
                    game.LoadMainMenu();
                    return true;
                default:
                    return true;
            }
        }

        private void OnResume()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.Resume();
        }

        private void OnRestart()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        private void OnOpenSettings()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (settingsPanel != null) settingsPanel.Open();
        }

        private void OnMainMenu()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }

        private void OnNextLevel()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
        }
    }
}
