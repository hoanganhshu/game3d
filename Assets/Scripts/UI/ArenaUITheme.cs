using UnityEngine;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>Shared gunmetal/crimson visual language for every menu scene.</summary>
    public static class ArenaUITheme
    {
        public static readonly Color Background = Color.white;
        public static readonly Color Panel = new Color(0.025f, 0.03f, 0.038f, 0.8f);
        public static readonly Color Gunmetal = new Color(0.025f, 0.035f, 0.045f, 0.54f);
        public static readonly Color Crimson = new Color(0.36f, 0.015f, 0.025f, 0.64f);
        public static readonly Color Silver = new Color(0.62f, 0.66f, 0.7f, 0.9f);
        public static readonly Color RedGlow = new Color(1f, 0.12f, 0.13f, 0.9f);
        public static readonly Color Gold = new Color(1f, 0.68f, 0.16f, 1f);

        private static Sprite clippedSprite;
        private static Sprite roundedGradientSprite;
        private static Sprite roundedWhiteSprite;

        private static Sprite ClippedSprite
        {
            get
            {
                if (clippedSprite != null) return clippedSprite;
                const int width = 128;
                const int height = 40;
                const int cut = 9;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "ArenaUI_ClippedButton",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                Color[] pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool inside = x + y >= cut && (width - 1 - x) + y >= cut &&
                                      x + (height - 1 - y) >= cut &&
                                      (width - 1 - x) + (height - 1 - y) >= cut;
                        pixels[y * width + x] = inside ? Color.white : Color.clear;
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                clippedSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(14f, 14f, 14f, 14f));
                clippedSprite.name = "ArenaUI_ClippedButtonSprite";
                return clippedSprite;
            }
        }

        public static void StyleRoot(Transform root)
        {
            if (root == null) return;
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant != null && descendant.name == "ThemeAccent")
                    Object.Destroy(descendant.gameObject);
            }
            foreach (Text text in root.GetComponentsInChildren<Text>(true)) StyleText(text);
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string key = button.name.ToLowerInvariant();
                bool primary = key.Contains("play") || key.Contains("resume") || key.Contains("next") ||
                               key.Contains("retry") || key.Contains("level1");
                bool danger = key.Contains("quit") || key.Contains("menu");
                StyleButton(button, primary, danger);
            }
            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true)) StyleSlider(slider);
        }

        public static void StyleSceneBackground(Transform root)
        {
            if (root == null) return;
            Transform background = root.Find("Background");
            if (background == null) return;
            Image image = background.GetComponent<Image>();
            Sprite sprite = Resources.Load<Sprite>("UI/ArenaMenuBackground");
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

            Transform shade = background.Find("CinematicShade");
            if (shade == null)
            {
                GameObject go = new GameObject("CinematicShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetParent(background, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Image overlay = go.GetComponent<Image>();
                overlay.color = new Color(0f, 0f, 0f, 0.28f);
                overlay.raycastTarget = false;
            }
            background.SetAsFirstSibling();
        }

        public static void StyleButton(Button button, bool primary = false, bool danger = false)
        {
            if (button == null) return;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = ClippedSprite;
                image.type = Image.Type.Sliced;
                image.color = primary || danger ? Crimson : Gunmetal;
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = primary ? RedGlow : danger ? new Color(0.75f, 0.12f, 0.14f, 0.8f) : Silver;
            outline.effectDistance = primary ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            Shadow glow = button.GetComponent<Shadow>();
            if (glow == null) glow = button.gameObject.AddComponent<Shadow>();
            glow.effectColor = primary
                ? new Color(1f, 0.03f, 0.04f, 0.5f)
                : new Color(0.75f, 0.8f, 0.84f, 0.2f);
            glow.effectDistance = primary ? new Vector2(5f, -5f) : new Vector2(3f, -3f);
            glow.useGraphicAlpha = true;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = primary ? new Color(1.2f, 0.82f, 0.82f, 1f) : new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.34f, 0.34f, 0.36f, 0.65f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyle.Bold;
                label.text = label.text.ToUpperInvariant();
            }
        }

        public static void StyleProgressBar(Image fill)
        {
            if (fill == null) return;
            fill.sprite = GetRoundedSprite(true);
            fill.type = Image.Type.Simple;
            fill.color = Color.white;
            fill.preserveAspect = false;

            Image track = fill.transform.parent != null ? fill.transform.parent.GetComponent<Image>() : null;
            if (track != null)
            {
                track.sprite = GetRoundedSprite(false);
                track.type = Image.Type.Sliced;
                track.color = new Color(0.025f, 0.03f, 0.04f, 0.82f);
            }
        }

        private static Sprite GetRoundedSprite(bool gradient)
        {
            if (gradient && roundedGradientSprite != null) return roundedGradientSprite;
            if (!gradient && roundedWhiteSprite != null) return roundedWhiteSprite;

            const int width = 256;
            const int height = 32;
            const float radius = 6f;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = gradient ? "ArenaUI_RedWhiteGradient" : "ArenaUI_RoundedTrack",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = Mathf.Clamp(x, radius, width - 1f - radius);
                    float cy = Mathf.Clamp(y, radius, height - 1f - radius);
                    float distance = new Vector2(x - cx, y - cy).magnitude;
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    Color color = gradient
                        ? Color.Lerp(new Color(0.86f, 0.015f, 0.025f, 1f), Color.white, x / (width - 1f))
                        : Color.white;
                    color.a = alpha;
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            if (gradient) roundedGradientSprite = sprite;
            else roundedWhiteSprite = sprite;
            return sprite;
        }

        public static void StylePanel(GameObject target)
        {
            if (target == null) return;
            Transform box = target.transform.Find("Box");
            if (box == null) box = target.transform;
            Image image = box.GetComponent<Image>();
            if (image != null) image.color = Panel;
            Outline outline = box.GetComponent<Outline>();
            if (outline == null && image != null) outline = box.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Silver;
                outline.effectDistance = new Vector2(2f, -2f);
            }
            StyleRoot(box);
        }

        public static void StyleText(Text text)
        {
            if (text == null) return;
            string key = text.name.ToLowerInvariant();
            if (key.Contains("title"))
            {
                text.color = Color.white;
                text.fontStyle = FontStyle.Bold;
                Outline outline = text.GetComponent<Outline>();
                if (outline == null) outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        private static void StyleSlider(Slider slider)
        {
            if (slider == null) return;
            Transform fill = slider.transform.Find("Fill Area/Fill");
            if (fill != null && fill.TryGetComponent(out Image fillImage)) fillImage.color = RedGlow;
            Transform background = slider.transform.Find("Background");
            if (background != null && background.TryGetComponent(out Image bg)) bg.color = Gunmetal;
            if (slider.handleRect != null && slider.handleRect.TryGetComponent(out Image handle)) handle.color = Color.white;
        }

    }
}
