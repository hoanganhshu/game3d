using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// Helpers for building uGUI hierarchies in code. ProjectBootstrapper uses it to build every
    /// canvas. It uses legacy Text with Unity's built-in font, so no TMP Essential Resources
    /// import is needed.
    /// </summary>
    public static class UIFactory
    {
        public const int UILayer = 5;
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static Font cachedFont;

        public static Font DefaultFont
        {
            get
            {
                if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedFont;
            }
        }

        public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            if (parent != null) go.transform.SetParent(parent, false);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>A full-screen container that shrinks itself to Screen.safeArea.</summary>
        public static RectTransform CreateSafeArea(Transform parent)
        {
            RectTransform rect = CreateRect("SafeArea", parent);
            Stretch(rect);
            rect.gameObject.AddComponent<SafeArea>();
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Half;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Half;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        public static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            Place(rect, Half, Half, position, size);
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null, bool raycastTarget = false)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>A full-screen colored panel that blocks clicks to whatever is behind it.</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            Image image = CreateImage(name, parent, color, null, true);
            Stretch(image.rectTransform);
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment,
            Color color, FontStyle style = FontStyle.Normal, bool shadow = true)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (shadow)
            {
                Shadow textShadow = rect.gameObject.AddComponent<Shadow>();
                textShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                textShadow.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        public static Button CreateButton(string name, Transform parent, string label, int fontSize, Color color, out Text labelText)
        {
            Image image = CreateImage(name, parent, color, null, true);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            labelText = CreateText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            Stretch(labelText.rectTransform);
            return button;
        }

        public static Slider CreateSlider(string name, Transform parent, float minValue, float maxValue, Color fillColor, Sprite handleSprite)
        {
            RectTransform root = CreateRect(name, parent);
            Slider slider = root.gameObject.AddComponent<Slider>();

            Image background = CreateImage("Background", root, new Color(0.2f, 0.22f, 0.28f, 1f), null, true);
            SetAnchors(background.rectTransform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            SetAnchors(fillArea, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
            fillArea.offsetMin = new Vector2(4f, 0f);
            fillArea.offsetMax = new Vector2(-4f, 0f);

            Image fill = CreateImage("Fill", fillArea, fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = Half;
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(24f, 0f);
            handleArea.offsetMax = new Vector2(-24f, 0f);

            Image handle = CreateImage("Handle", handleArea, Color.white, handleSprite, true);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.pivot = Half;
            handle.rectTransform.sizeDelta = new Vector2(48f, 0f);
            handle.rectTransform.anchoredPosition = Vector2.zero;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = false;
            slider.value = minValue;
            return slider;
        }

        public static EventSystem CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            EventSystem eventSystem = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return eventSystem;
        }
    }
}
