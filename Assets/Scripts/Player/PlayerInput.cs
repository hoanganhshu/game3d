using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FPSGame
{
    /// <summary>
    /// Collects player input once per frame from either the touch controls (virtual joystick,
    /// touch buttons, swipe on the right half of the screen) or keyboard + mouse.
    /// Runs before PlayerController and WeaponManager.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInput : MonoBehaviour
    {
        // A swipe across the full screen height turns the camera this many degrees at sensitivity 1.
        private const float TouchDegreesPerScreenHeight = 150f;
        private const float MouseDegreesPerUnit = 2f;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool RunHeld { get; private set; }
        public bool SwitchPressed { get; private set; }
        public int SelectWeapon { get; private set; } = -1;

        private int lookFingerId = -1;
        private bool mouseLookActive;
        private Vector3 lastMousePosition;

        private PointerEventData pointerData;
        private EventSystem pointerDataOwner;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(8);

        private void Update()
        {
            ResetFrame();

            if (!GameManager.IsPlaying)
            {
                lookFingerId = -1;
                mouseLookActive = false;
                return;
            }

            float sensitivity = SaveManager.Instance != null ? SaveManager.Instance.Sensitivity : 1f;
            if (InputMode.TouchMode)
            {
                ReadTouch(sensitivity);
                // Touch controls and the keyboard are intentionally active together. This is
                // useful for Editor testing, desktop touch screens and hybrid devices.
                MergeKeyboard();
            }
            else ReadDesktop(sensitivity);
        }

        private void ResetFrame()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            FireHeld = false;
            FirePressed = false;
            ReloadPressed = false;
            JumpPressed = false;
            CrouchHeld = false;
            RunHeld = false;
            SwitchPressed = false;
            SelectWeapon = -1;
        }

        // ---------------- Touch ----------------

        private void ReadTouch(float sensitivity)
        {
            Vector2 lookPixels = Vector2.zero;

            MobileControls controls = MobileControls.Instance;
            if (controls != null)
            {
                if (controls.MoveJoystick != null) Move = controls.MoveJoystick.Value;

                TouchButton fire = controls.FireButton;
                if (fire != null)
                {
                    FireHeld = fire.IsHeld;
                    FirePressed = fire.ConsumePress();
                    lookPixels += fire.ConsumeDrag();
                }

                CrouchHeld = controls.CrouchButton != null && controls.CrouchButton.IsHeld;
                JumpPressed = controls.JumpButton != null && controls.JumpButton.ConsumePress();
                ReloadPressed = controls.ReloadButton != null && controls.ReloadButton.ConsumePress();
                SwitchPressed = controls.SwitchButton != null && controls.SwitchButton.ConsumePress();
            }

            int touchCount = Input.touchCount;
            if (touchCount > 0)
            {
                bool lookFingerPresent = false;
                for (int i = 0; i < touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId == lookFingerId)
                    {
                        lookFingerPresent = true;
                        if (touch.phase == TouchPhase.Moved)
                        {
                            lookPixels += touch.deltaPosition;
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            lookFingerId = -1;
                        }
                    }
                    else if (touch.phase == TouchPhase.Began && lookFingerId == -1 &&
                             touch.position.x > Screen.width * 0.5f && !IsOverUI(touch.position))
                    {
                        lookFingerId = touch.fingerId;
                        lookFingerPresent = true;
                    }
                }

                if (!lookFingerPresent) lookFingerId = -1;
            }
            else
            {
                lookFingerId = -1;

                // Mouse stand-in for a finger, to test touch mode (F2) in the Editor.
                if (!Application.isMobilePlatform)
                {
                    if (Input.GetMouseButtonDown(0) && Input.mousePosition.x > Screen.width * 0.5f &&
                        !IsOverUI(Input.mousePosition))
                    {
                        mouseLookActive = true;
                        lastMousePosition = Input.mousePosition;
                    }

                    if (mouseLookActive)
                    {
                        if (Input.GetMouseButton(0))
                        {
                            Vector3 delta = Input.mousePosition - lastMousePosition;
                            lookPixels += new Vector2(delta.x, delta.y);
                            lastMousePosition = Input.mousePosition;
                        }
                        else
                        {
                            mouseLookActive = false;
                        }
                    }
                }
            }

            float pixelsToDegrees = TouchDegreesPerScreenHeight / Mathf.Max(1, Screen.height) * sensitivity;
            Look = lookPixels * pixelsToDegrees;
        }

        private void MergeKeyboard()
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            Vector2 keyboardMove = new Vector2(x, y);
            if (keyboardMove.sqrMagnitude > 1f) keyboardMove.Normalize();
            if (keyboardMove.sqrMagnitude > 0f) Move = keyboardMove;

            ReloadPressed |= Input.GetKeyDown(KeyCode.R);
            FireHeld |= Input.GetKey(KeyCode.F);
            FirePressed |= Input.GetKeyDown(KeyCode.F);
            JumpPressed |= Input.GetKeyDown(KeyCode.Space);
            CrouchHeld |= Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
            RunHeld |= Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            SwitchPressed |= Input.GetKeyDown(KeyCode.Q) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectWeapon = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeapon = 1;
        }

        /// <summary>
        /// Raycasts the UI directly. IsPointerOverGameObject can miss a touch that began this
        /// frame because the EventSystem may not have processed it yet.
        /// </summary>
        private bool IsOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (pointerData == null || pointerDataOwner != eventSystem)
            {
                pointerData = new PointerEventData(eventSystem);
                pointerDataOwner = eventSystem;
            }

            pointerData.position = screenPosition;
            raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, raycastResults);
            return raycastResults.Count > 0;
        }

        // ---------------- Keyboard + mouse ----------------

        private void ReadDesktop(float sensitivity)
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;

            Vector2 move = new Vector2(x, y);
            if (move.sqrMagnitude > 1f) move.Normalize();
            Move = move;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) *
                       (MouseDegreesPerUnit * sensitivity);
                FireHeld = Input.GetMouseButton(0);
                FirePressed = Input.GetMouseButtonDown(0);
            }

            // Keyboard fire remains available when the cursor is not locked, which makes
            // the desktop controls usable alongside the visible touch HUD.
            FireHeld |= Input.GetKey(KeyCode.F);
            FirePressed |= Input.GetKeyDown(KeyCode.F);

            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            CrouchHeld = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            RunHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            SwitchPressed = Input.GetKeyDown(KeyCode.Q) || Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectWeapon = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeapon = 1;
        }
    }
}
