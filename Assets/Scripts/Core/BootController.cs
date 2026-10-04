using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPSGame
{
    /// <summary>Boot scene: the managers are created by SceneBootstrap, then we go to the main menu.</summary>
    public class BootController : MonoBehaviour
    {
        [SerializeField] private float minimumDisplayTime = 0.6f;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(minimumDisplayTime);

            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            else SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
