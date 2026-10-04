using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace FPSGame
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Won,
        Lost
    }

    /// <summary>
    /// Persistent game flow controller: game state, pause, scene navigation, Back button and cursor.
    /// Lives on the Managers prefab (DontDestroyOnLoad).
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public static event System.Action<GameState> StateChanged;

        public static bool IsPlaying => Instance != null && Instance.State == GameState.Playing;

        public GameState State { get; private set; } = GameState.Menu;

        public int CurrentLevelIndex { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            StateChanged = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // On Android the hardware/gesture Back button is reported as KeyCode.Escape.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                BackButtonManager.HandleBack();
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.F2))
            {
                InputMode.SetTouchMode(!InputMode.TouchMode);
                ApplyCursorState();
            }

            // Re-lock the cursor after the Editor released it (e.g. the game view lost focus).
            if (State == GameState.Playing && !InputMode.TouchMode &&
                Cursor.lockState != CursorLockMode.Locked &&
                Input.GetMouseButtonDown(0) && !IsPointerOverUI())
            {
                ApplyCursorState();
            }
#endif
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void SetState(GameState newState)
        {
            State = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;

            // Stop the Space key from "submitting" whatever button was clicked last.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

            ApplyCursorState();
            StateChanged?.Invoke(newState);
        }

        private void ApplyCursorState()
        {
            bool lockCursor = State == GameState.Playing && !InputMode.TouchMode && !Application.isMobilePlatform;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }

        /// <summary>Called by LevelManager once the level is built and the player spawned.</summary>
        public void BeginLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void PlayHighestUnlocked()
        {
            int unlocked = SaveManager.Instance != null ? SaveManager.Instance.UnlockedLevels : 1;
            LoadLevel(Mathf.Clamp(unlocked, 1, LevelDatabase.Count) - 1);
        }

        public void LoadLevel(int index)
        {
            CurrentLevelIndex = Mathf.Clamp(index, 0, LevelDatabase.Count - 1);
            LoadScene(SceneNames.GetLevel(CurrentLevelIndex));
        }

        public void RestartLevel()
        {
            LoadLevel(CurrentLevelIndex);
        }

        public bool HasNextLevel => CurrentLevelIndex + 1 < LevelDatabase.Count;

        public void LoadNextLevel()
        {
            if (HasNextLevel) LoadLevel(CurrentLevelIndex + 1);
            else LoadLevelSelect();
        }

        public void LoadMainMenu()
        {
            LoadScene(SceneNames.MainMenu);
        }

        public void LoadLevelSelect()
        {
            LoadScene(SceneNames.LevelSelect);
        }

        private void LoadScene(string sceneName)
        {
            SetState(GameState.Menu);
            if (SceneLoader.Instance != null) SceneLoader.Instance.LoadScene(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        public void QuitGame()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.Flush();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
