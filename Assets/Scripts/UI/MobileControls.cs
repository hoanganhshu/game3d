using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Holds the on-screen controls and shows them only in touch mode. PlayerInput finds it
    /// through <see cref="Instance"/>, because the player prefab is spawned at runtime.
    /// </summary>
    public class MobileControls : MonoBehaviour
    {
        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private VirtualJoystick moveJoystick;
        [SerializeField] private TouchButton fireButton;
        [SerializeField] private TouchButton reloadButton;
        [SerializeField] private TouchButton jumpButton;
        [SerializeField] private TouchButton switchButton;

        public static MobileControls Instance { get; private set; }

        public VirtualJoystick MoveJoystick => moveJoystick;
        public TouchButton FireButton => fireButton;
        public TouchButton ReloadButton => switchButton;
        public TouchButton CrouchButton => reloadButton;
        public TouchButton JumpButton => jumpButton;
        public TouchButton SwitchButton => null;

        private void Awake()
        {
            Instance = this;
            InputMode.Changed += Apply;
            ResolveMissingReferences();
            ApplyProfessionalStyle();
            Apply(InputMode.TouchMode);
        }

        private void ResolveMissingReferences()
        {
            // Keep controls functional even if a scene was generated before one of these
            // serialized references was added or renamed.
            if (controlsRoot == null)
            {
                Transform root = transform.Find("Controls");
                if (root != null) controlsRoot = root.gameObject;
            }

            if (moveJoystick == null) moveJoystick = GetComponentInChildren<VirtualJoystick>(true);
            TouchButton[] buttons = GetComponentsInChildren<TouchButton>(true);
            foreach (TouchButton button in buttons)
            {
                switch (button.name)
                {
                    case "FireButton": if (fireButton == null) fireButton = button; break;
                    case "ReloadButton": if (reloadButton == null) reloadButton = button; break;
                    case "JumpButton": if (jumpButton == null) jumpButton = button; break;
                    case "SwitchButton": if (switchButton == null) switchButton = button; break;
                }
            }
        }

        private void ApplyProfessionalStyle()
        {
            StyleCircle(moveJoystick != null ? moveJoystick.transform.Find("JoystickBackground") : null,
                new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0.34f));

            if (moveJoystick != null)
                StyleCircle(moveJoystick.transform.Find("JoystickBackground/JoystickHandle"),
                    new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0.52f));

            Color buttonFill = new Color(1f, 1f, 1f, 0.09f);
            Color buttonEdge = new Color(1f, 1f, 1f, 0.36f);
            StyleButton(fireButton, buttonFill, buttonEdge, "UI/Icons/icon-fire", "◎");
            StyleButton(jumpButton, buttonFill, buttonEdge, "UI/Icons/icon-jump", "↑");
            StyleButton(reloadButton, buttonFill, buttonEdge, "UI/Icons/icon-crouch", "▼");
            StyleButton(switchButton, buttonFill, buttonEdge, "UI/Icons/icon-reload", "↻");
        }

        private static void StyleButton(TouchButton button, Color fill, Color edge, string iconPath, string fallbackLabel)
        {
            if (button == null) return;
            StyleCircle(button.transform, fill, edge);
            Color pressed = fill;
            pressed.a = 0.19f;
            button.ConfigureColors(fill, pressed);
            Text text = button.GetComponentInChildren<Text>(true);
            Sprite icon = LoadIcon(iconPath);
            if (icon != null)
            {
                if (text != null) text.gameObject.SetActive(false);
                Transform existing = button.transform.Find("ActionIcon");
                Image iconImage;
                if (existing != null)
                {
                    iconImage = existing.GetComponent<Image>();
                }
                else
                {
                    GameObject iconObject = new GameObject("ActionIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObject.transform.SetParent(button.transform, false);
                    iconImage = iconObject.GetComponent<Image>();
                }

                iconImage.sprite = icon;
                iconImage.color = new Color(1f, 1f, 1f, 0.82f);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                RectTransform rect = iconImage.rectTransform;
                rect.anchorMin = new Vector2(0.16f, 0.16f);
                rect.anchorMax = new Vector2(0.84f, 0.84f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            if (text == null) return;
            text.text = fallbackLabel;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 58;
        }

        private static Sprite LoadIcon(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) return null;
            Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
        }

        private static void StyleCircle(Transform target, Color fill, Color edge)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image != null) image.color = fill;
            Outline outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.gameObject.AddComponent<Outline>();
            outline.effectColor = edge;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
        }

        private void OnDestroy()
        {
            InputMode.Changed -= Apply;
            if (Instance == this) Instance = null;
        }

        private void Apply(bool touchMode)
        {
            if (controlsRoot != null) controlsRoot.SetActive(touchMode);
        }
    }
}
