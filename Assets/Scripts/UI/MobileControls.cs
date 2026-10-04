using UnityEngine;

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
        public TouchButton ReloadButton => reloadButton;
        public TouchButton JumpButton => jumpButton;
        public TouchButton SwitchButton => switchButton;

        private void Awake()
        {
            Instance = this;
            InputMode.Changed += Apply;
            Apply(InputMode.TouchMode);
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
