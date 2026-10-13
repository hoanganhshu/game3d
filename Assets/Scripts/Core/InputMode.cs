using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Decides whether the game reads the on-screen touch controls or keyboard + mouse.
    /// Touch mode is on for mobile builds. In the Editor / standalone builds it can be
    /// toggled with F2 to test the touch layout with the mouse.
    /// </summary>
    public static class InputMode
    {
        public static bool TouchMode { get; private set; }

        public static event System.Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Changed = null;
            // Keep the mobile layout available in the Editor as well so the game can be
            // tested with mouse-as-touch without producing a separate UI-only build.
            TouchMode = Application.isMobilePlatform || Application.isEditor;
        }

        public static void SetTouchMode(bool enabled)
        {
            if (TouchMode == enabled) return;
            TouchMode = enabled;
            Changed?.Invoke(enabled);
        }
    }
}
