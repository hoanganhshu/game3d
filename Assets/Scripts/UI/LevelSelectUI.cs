using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Level select: one card per level with best stars and score. Locked levels show a lock
    /// overlay and cannot be clicked. Back returns to the main menu.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour, IBackHandler
    {
        private const int StarsPerLevel = 3;

        [SerializeField] private Button[] levelButtons = new Button[0];
        [SerializeField] private Text[] infoTexts = new Text[0];
        [SerializeField] private Image[] starImages = new Image[0];
        [SerializeField] private GameObject[] lockOverlays = new GameObject[0];
        [SerializeField] private Button backButton;
        [SerializeField] private Color starOnColor = new Color(1f, 0.8f, 0.15f, 1f);
        [SerializeField] private Color starOffColor = new Color(0.22f, 0.24f, 0.3f, 1f);

        private void Awake()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                int levelIndex = i;
                levelButtons[i].onClick.AddListener(() => OnLevelClicked(levelIndex));
            }

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void Start()
        {
            Refresh();
        }

        private void OnEnable()
        {
            BackButtonManager.Push(this);
        }

        private void OnDisable()
        {
            BackButtonManager.Remove(this);
        }

        private void Refresh()
        {
            SaveManager save = SaveManager.Instance;
            for (int i = 0; i < levelButtons.Length; i++)
            {
                bool unlocked = save != null ? save.IsUnlocked(i) : i == 0;
                int stars = save != null ? save.GetBestStars(i) : 0;
                int bestScore = save != null ? save.GetBestScore(i) : 0;

                if (levelButtons[i] != null) levelButtons[i].interactable = unlocked;
                if (i < lockOverlays.Length && lockOverlays[i] != null) lockOverlays[i].SetActive(!unlocked);

                if (i < infoTexts.Length && infoTexts[i] != null)
                {
                    infoTexts[i].text = !unlocked ? "LOCKED" : bestScore > 0 ? "BEST  " + bestScore : "NOT PLAYED";
                }

                for (int s = 0; s < StarsPerLevel; s++)
                {
                    int index = i * StarsPerLevel + s;
                    if (index < starImages.Length && starImages[index] != null)
                    {
                        starImages[index].color = s < stars ? starOnColor : starOffColor;
                    }
                }
            }
        }

        private void OnLevelClicked(int levelIndex)
        {
            SaveManager save = SaveManager.Instance;
            if (save != null && !save.IsUnlocked(levelIndex)) return;

            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.LoadLevel(levelIndex);
        }

        private void OnBackClicked()
        {
            AudioManager.PlaySound(SoundType.Click);
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
        }

        public bool OnBack()
        {
            OnBackClicked();
            return true;
        }
    }
}
