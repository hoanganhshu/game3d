using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Saves settings and level progress in PlayerPrefs and applies settings (volume, quality).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class SaveManager : MonoBehaviour
    {
        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 3f;
        public const int QualityLow = 0;
        public const int QualityMedium = 1;
        public const int QualityHigh = 2;

        private const string KeySensitivity = "settings.sensitivity";
        private const string KeyVolume = "settings.volume";
        private const string KeyQuality = "settings.quality";
        private const string KeyShowFps = "settings.showFps";
        private const string KeyUnlocked = "progress.unlocked";
        private const string KeyBestScorePrefix = "progress.bestScore.";
        private const string KeyBestStarsPrefix = "progress.bestStars.";

        public static SaveManager Instance { get; private set; }

        public event System.Action SettingsChanged;

        public float Sensitivity { get; private set; } = 1f;
        public float Volume { get; private set; } = 0.8f;
        public int Quality { get; private set; } = QualityMedium;
        public bool ShowFps { get; private set; }
        public int UnlockedLevels { get; private set; } = 1;

        private string[] bestScoreKeys;
        private string[] bestStarsKeys;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            bestScoreKeys = new string[LevelDatabase.Count];
            bestStarsKeys = new string[LevelDatabase.Count];
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                bestScoreKeys[i] = KeyBestScorePrefix + i;
                bestStarsKeys[i] = KeyBestStarsPrefix + i;
            }

            Load();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void Load()
        {
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, 1f), MinSensitivity, MaxSensitivity);
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, 0.8f));
            Quality = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualityMedium), QualityLow, QualityHigh);
            ShowFps = PlayerPrefs.GetInt(KeyShowFps, 0) == 1;
            UnlockedLevels = Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlocked, 1), 1, LevelDatabase.Count);
        }

        public void Flush()
        {
            PlayerPrefs.Save();
        }

        // ---------------- Settings ----------------

        public void SetSensitivity(float value)
        {
            Sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(KeySensitivity, Sensitivity);
            SettingsChanged?.Invoke();
        }

        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyVolume, Volume);
            AudioListener.volume = Volume;
            SettingsChanged?.Invoke();
        }

        public void SetQuality(int quality)
        {
            Quality = Mathf.Clamp(quality, QualityLow, QualityHigh);
            PlayerPrefs.SetInt(KeyQuality, Quality);
            ApplyQuality();
            SettingsChanged?.Invoke();
        }

        public void SetShowFps(bool show)
        {
            ShowFps = show;
            PlayerPrefs.SetInt(KeyShowFps, show ? 1 : 0);
            SettingsChanged?.Invoke();
        }

        public void ApplySettings()
        {
            AudioListener.volume = Volume;
            ApplyQuality();
        }

        private void ApplyQuality()
        {
            int target = FindQualityIndex(QualitySettings.names, Quality);
            if (target >= 0 && target != QualitySettings.GetQualityLevel())
            {
                QualitySettings.SetQualityLevel(target, true);
            }

            // Quality levels may change vSync; keep the 60 FPS target.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private static int FindQualityIndex(string[] names, int quality)
        {
            if (names == null || names.Length == 0) return -1;

            string wanted = quality == QualityLow ? "Low" : quality == QualityMedium ? "Medium" : "High";
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(names[i], wanted, System.StringComparison.OrdinalIgnoreCase)) return i;
            }

            return Mathf.Clamp(Mathf.RoundToInt(quality / 2f * (names.Length - 1)), 0, names.Length - 1);
        }

        // ---------------- Progress ----------------

        public bool IsUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < UnlockedLevels;
        }

        public int GetBestScore(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= bestScoreKeys.Length) return 0;
            return PlayerPrefs.GetInt(bestScoreKeys[levelIndex], 0);
        }

        public int GetBestStars(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= bestStarsKeys.Length) return 0;
            return PlayerPrefs.GetInt(bestStarsKeys[levelIndex], 0);
        }

        /// <summary>Stores a finished level and unlocks the next one.</summary>
        /// <returns>true if the score is a new best.</returns>
        public bool SubmitResult(int levelIndex, int score, int stars)
        {
            if (levelIndex < 0 || levelIndex >= LevelDatabase.Count) return false;

            bool newBest = score > GetBestScore(levelIndex);
            if (newBest) PlayerPrefs.SetInt(bestScoreKeys[levelIndex], score);
            if (stars > GetBestStars(levelIndex)) PlayerPrefs.SetInt(bestStarsKeys[levelIndex], stars);

            int unlock = Mathf.Min(levelIndex + 2, LevelDatabase.Count);
            if (unlock > UnlockedLevels)
            {
                UnlockedLevels = unlock;
                PlayerPrefs.SetInt(KeyUnlocked, UnlockedLevels);
            }

            Flush();
            return newBest;
        }
    }
}
