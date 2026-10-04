using UnityEngine;
using UnityEngine.EventSystems;

namespace FPSGame
{
    /// <summary>
    /// Floating virtual joystick. Touching anywhere in the zone moves the joystick under the
    /// finger; dragging gives a -1..1 vector. Works with touch (multi-touch through the
    /// EventSystem) and with the mouse in the Editor.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IInitializePotentialDragHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float deadZone = 0.12f;
        [SerializeField] private bool floating = true;

        public Vector2 Value { get; private set; }

        private RectTransform zone;
        private Vector2 homePosition;
        private int pointerId = NoPointer;

        private void Awake()
        {
            zone = (RectTransform)transform;
            if (background != null) homePosition = background.anchoredPosition;
        }

        private void OnDisable()
        {
            ResetStick();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // React immediately instead of waiting for the EventSystem drag threshold.
            eventData.useDragThreshold = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer || background == null) return;
            pointerId = eventData.pointerId;

            // The background is anchored at the zone's center pivot, so the local point maps
            // straight to anchoredPosition.
            if (floating && RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position,
                    eventData.pressEventCamera, out Vector2 localPoint))
            {
                background.anchoredPosition = localPoint;
            }

            UpdateStick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            UpdateStick(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            ResetStick();
        }

        private void UpdateStick(PointerEventData eventData)
        {
            if (background == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position,
                    eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 radius = background.rect.size * 0.5f;
            if (radius.x <= 0f || radius.y <= 0f) return;

            Vector2 value = new Vector2(localPoint.x / radius.x, localPoint.y / radius.y);
            if (value.sqrMagnitude > 1f) value.Normalize();

            if (handle != null) handle.anchoredPosition = new Vector2(value.x * radius.x, value.y * radius.y);
            Value = value.magnitude < deadZone ? Vector2.zero : value;
        }

        private void ResetStick()
        {
            pointerId = NoPointer;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.anchoredPosition = homePosition;
        }
    }
}
