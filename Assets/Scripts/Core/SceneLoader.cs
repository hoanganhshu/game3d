using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Loads scenes with LoadSceneAsync behind a loading screen with a progress bar.
    /// The loading screen lives on the persistent SystemCanvas of the Managers prefab.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Text progressText;

        public static SceneLoader Instance { get; private set; }

        public bool IsLoading { get; private set; }

        private static string[] percentStrings;

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

            if (percentStrings == null)
            {
                percentStrings = new string[101];
                for (int i = 0; i <= 100; i++) percentStrings[i] = i + "%";
            }

            if (loadingRoot != null) loadingRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void LoadScene(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            Time.timeScale = 1f;
            if (loadingRoot != null) loadingRoot.SetActive(true);
            SetProgress(0f);

            // Let the loading screen render before the heavy work starts.
            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError("[SceneLoader] Scene '" + sceneName + "' is not in Build Settings.");
                if (loadingRoot != null) loadingRoot.SetActive(false);
                IsLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;

            float shown = 0f;
            while (operation.progress < 0.9f || shown < 1f)
            {
                float target = Mathf.Clamp01(operation.progress / 0.9f);
                shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 2.5f);
                SetProgress(shown);
                yield return null;
            }

            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;

            // One more frame so the new scene can run Start (level build + NavMesh bake)
            // while still hidden.
            yield return null;

            if (loadingRoot != null) loadingRoot.SetActive(false);
            IsLoading = false;
        }

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (progressFill != null) progressFill.anchorMax = new Vector2(value, 1f);
            if (progressText != null) progressText.text = percentStrings[Mathf.RoundToInt(value * 100f)];
        }
    }
}
