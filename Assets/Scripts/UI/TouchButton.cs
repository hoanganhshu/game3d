using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FPSGame
{
    /// <summary>
    /// A hold-able on-screen button. It reports whether it is held, and a press that is kept
    /// until read, so presses are never lost whichever of the EventSystem or the reader runs
    /// first in a frame. It can also pass on drag movement, so the fire button can aim while firing.
    /// </summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler,
        IInitializePotentialDragHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private Image targetImage;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color pressedColor = new Color(1f, 1f, 1f, 0.65f);
        [SerializeField] private bool allowDragLook;

        private int activePointer = NoPointer;
        private bool pressPending;
        private Vector2 dragAccumulated;

        public bool IsHeld => activePointer != NoPointer;

        public void ConfigureColors(Color normal, Color pressed)
        {
            normalColor = normal;
            pressedColor = pressed;
            ApplyColor(IsHeld);
        }

        private void OnEnable()
        {
            ApplyColor(false);
        }

        private void OnDisable()
        {
            activePointer = NoPointer;
            pressPending = false;
            dragAccumulated = Vector2.zero;
            ApplyColor(false);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointer != NoPointer) return;
            activePointer = eventData.pointerId;
            pressPending = true;
            ApplyColor(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointer) return;
            activePointer = NoPointer;
            ApplyColor(false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (allowDragLook && eventData.pointerId == activePointer) dragAccumulated += eventData.delta;
        }

        /// <summary>Returns true once for each press.</summary>
        public bool ConsumePress()
        {
            if (!pressPending) return false;
            pressPending = false;
            return true;
        }

        /// <summary>Returns the drag movement (pixels) since the last call.</summary>
        public Vector2 ConsumeDrag()
        {
            Vector2 drag = dragAccumulated;
            dragAccumulated = Vector2.zero;
            return drag;
        }

        private void ApplyColor(bool pressed)
        {
            if (targetImage != null) targetImage.color = pressed ? pressedColor : normalColor;
        }
    }
}
