using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>Optional FPS readout on the persistent system canvas, toggled in Settings.</summary>
    public class FPSCounter : MonoBehaviour
    {
        private const int MaxDisplayedFps = 240;
        private const float UpdateInterval = 0.5f;

        [SerializeField] private Text label;

        private static string[] fpsStrings;

        private float timer;
        private int frames;
        private bool visible;

        private void Awake()
        {
            if (fpsStrings == null)
            {
                fpsStrings = new string[MaxDisplayedFps + 1];
                for (int i = 0; i <= MaxDisplayedFps; i++) fpsStrings[i] = "FPS " + i;
            }
        }

        private void Start()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.SettingsChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (SaveManager.Instance != null) SaveManager.Instance.SettingsChanged -= Refresh;
        }

        private void Refresh()
        {
            visible = SaveManager.Instance != null && SaveManager.Instance.ShowFps;
            if (label != null) label.enabled = visible;
            timer = 0f;
            frames = 0;
        }

        private void Update()
        {
            if (!visible || label == null) return;

            frames++;
            timer += Time.unscaledDeltaTime;
            if (timer < UpdateInterval) return;

            int fps = Mathf.Clamp(Mathf.RoundToInt(frames / timer), 0, MaxDisplayedFps);
            label.text = fpsStrings[fps];
            label.color = fps >= 50 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
            frames = 0;
            timer = 0f;
        }
    }
}
