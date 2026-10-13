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
            ArenaUITheme.StyleRoot(transform);
            ArenaUITheme.StyleSceneBackground(transform);
            Image background = transform.Find("Background") != null ? transform.Find("Background").GetComponent<Image>() : null;

            Text title = GetComponentInChildren<Text>(true);
            if (title != null && title.name == "Title")
            {
                title.text = "SELECT ARENA";
                title.color = Color.white;
                Outline titleOutline = title.GetComponent<Outline>();
                if (titleOutline == null) titleOutline = title.gameObject.AddComponent<Outline>();
                titleOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                titleOutline.effectDistance = new Vector2(3f, -3f);
            }

            for (int i = 0; i < levelButtons.Length; i++)
            {
                Button card = levelButtons[i];
                if (card == null) continue;
                AddArenaPreview(card, i);
                Image image = card.GetComponent<Image>();
                if (image != null) image.color = ArenaUITheme.Gunmetal;
                Outline border = card.GetComponent<Outline>();
                if (border == null) border = card.gameObject.AddComponent<Outline>();
                border.effectColor = ArenaUITheme.Silver;
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
                    go.GetComponent<Image>().color = ArenaUITheme.RedGlow;
                }
            }

            if (backButton != null)
            {
                Image image = backButton.GetComponent<Image>();
                if (image != null) image.color = ArenaUITheme.Gunmetal;
                Outline outline = backButton.GetComponent<Outline>();
                if (outline == null) outline = backButton.gameObject.AddComponent<Outline>();
                outline.effectColor = ArenaUITheme.Silver;
                outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        private static void AddArenaPreview(Button card, int index)
        {
            Sprite previewSprite = Resources.Load<Sprite>("UI/ArenaThumb" + (index + 1));
            if (previewSprite == null) return;

            Transform existing = card.transform.Find("ArenaPreview");
            Image preview;
            if (existing != null)
            {
                preview = existing.GetComponent<Image>();
            }
            else
            {
                GameObject go = new GameObject("ArenaPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(card.transform, false);
                preview = go.GetComponent<Image>();
            }

            preview.sprite = previewSprite;
            preview.preserveAspect = false;
            preview.color = Color.white;
            preview.raycastTarget = false;
            RectTransform previewRect = preview.rectTransform;
            previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.anchoredPosition = new Vector2(0f, -14f);
            previewRect.sizeDelta = new Vector2(430f, 250f);
            preview.transform.SetAsFirstSibling();

            Reposition(card.transform.Find("Label"), new Vector2(0f, -278f), new Vector2(440f, 64f));
            Reposition(card.transform.Find("Name"), new Vector2(0f, -342f), new Vector2(440f, 44f));
            Transform enemies = card.transform.Find("Enemies");
            if (enemies != null) enemies.gameObject.SetActive(false);
            for (int star = 1; star <= 3; star++)
            {
                Transform starTransform = card.transform.Find("Star" + star);
                if (starTransform is RectTransform starRect)
                {
                    starRect.anchoredPosition = new Vector2((star - 2) * 92f, -160f);
                    starRect.sizeDelta = new Vector2(68f, 68f);
                }
            }
            Transform info = card.transform.Find("Info");
            if (info is RectTransform infoRect)
            {
                infoRect.anchorMin = infoRect.anchorMax = infoRect.pivot = new Vector2(0.5f, 0f);
                infoRect.anchoredPosition = new Vector2(0f, 24f);
                infoRect.sizeDelta = new Vector2(440f, 50f);
            }
        }

        private static void Reposition(Transform target, Vector2 positionFromTop, Vector2 size)
        {
            if (!(target is RectTransform rect)) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = positionFromTop;
            rect.sizeDelta = size;
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
