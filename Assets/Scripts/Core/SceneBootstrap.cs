using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Every scene has one. It creates the persistent Managers prefab (GameManager, SaveManager,
    /// AudioManager, SceneLoader) from Resources if it does not exist yet, so any scene can be
    /// played straight from the Editor. It runs before every other script.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SceneBootstrap : MonoBehaviour
    {
        public const string ManagersResourcePath = "Managers";

        private void Awake()
        {
            EnsureManagers();
        }

        public static void EnsureManagers()
        {
            if (GameManager.Instance != null) return;

            GameObject prefab = Resources.Load<GameObject>(ManagersResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[SceneBootstrap] Resources/" + ManagersResourcePath +
                               ".prefab is missing. Run Tools > Setup Everything.");
                return;
            }

            GameObject managers = Instantiate(prefab);
            managers.name = prefab.name;
        }
    }
}
