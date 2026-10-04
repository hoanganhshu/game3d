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
