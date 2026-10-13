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
            StyleScreen();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;
                int levelIndex = i;
                levelButtons[i].onClick.AddListener(() => OnLevelClicked(levelIndex));
            }

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void StyleScreen()
        {
            Image background = transform.Find("Background") != null ? transform.Find("Background").GetComponent<Image>() : null;
            if (background != null) background.color = new Color(0.015f, 0.025f, 0.045f, 1f);

            Text title = GetComponentInChildren<Text>(true);
            if (title != null && title.name == "Title")
            {
                title.text = "CHOOSE YOUR ARENA";
                title.color = new Color(0.2f, 0.88f, 1f, 1f);
                Outline titleOutline = title.GetComponent<Outline>();
                if (titleOutline == null) titleOutline = title.gameObject.AddComponent<Outline>();
                titleOutline.effectColor = new Color(0f, 0.35f, 0.5f, 0.8f);
                titleOutline.effectDistance = new Vector2(3f, -3f);
            }

            for (int i = 0; i < levelButtons.Length; i++)
            {
                Button card = levelButtons[i];
                if (card == null) continue;
                Image image = card.GetComponent<Image>();
                if (image != null) image.color = new Color(0.035f, 0.055f, 0.085f, 0.98f);
                Outline border = card.GetComponent<Outline>();
                if (border == null) border = card.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.08f, 0.65f, 0.82f, 0.85f);
                border.effectDistance = new Vector2(3f, -3f);

                ColorBlock colors = card.colors;
                colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
                colors.pressedColor = new Color(0.65f, 0.85f, 0.9f, 1f);
                colors.disabledColor = new Color(0.28f, 0.3f, 0.36f, 0.9f);
                colors.fadeDuration = 0.14f;
                card.colors = colors;

                Transform accent = card.transform.Find("CardAccent");
                if (accent == null)
                {
                    GameObject go = new GameObject("CardAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    RectTransform rect = go.GetComponent<RectTransform>();
                    rect.SetParent(card.transform, false);
                    rect.anchorMin = new Vector2(0.08f, 1f);
                    rect.anchorMax = new Vector2(0.92f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -8f);
                    rect.sizeDelta = new Vector2(0f, 8f);
                    go.GetComponent<Image>().color = new Color(0.1f, 0.85f, 1f, 1f);
                }
            }

            if (backButton != null)
            {
                Image image = backButton.GetComponent<Image>();
                if (image != null) image.color = new Color(0.08f, 0.14f, 0.22f, 1f);
                Outline outline = backButton.GetComponent<Outline>();
                if (outline == null) outline = backButton.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.1f, 0.75f, 0.95f, 0.8f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
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
