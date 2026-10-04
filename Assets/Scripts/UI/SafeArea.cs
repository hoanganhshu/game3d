using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Fits this RectTransform inside Screen.safeArea (notches, rounded corners, gesture bars)
    /// and refits when the safe area or resolution changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastWidth || Screen.height != lastHeight)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (lastWidth <= 0 || lastHeight <= 0 || safe.width <= 0f || safe.height <= 0f) return;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= lastWidth;
            anchorMin.y /= lastHeight;
            anchorMax.x /= lastWidth;
            anchorMax.y /= lastHeight;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
