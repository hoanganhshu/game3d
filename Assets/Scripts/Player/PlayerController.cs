using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// First-person movement on a CharacterController: walk, jump, gravity, yaw on the body,
    /// pitch on the camera pivot with a clamp, plus camera recoil.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float recoilRecoverySpeed = 9f;
        [SerializeField] private float fallResetHeight = -20f;

        private CharacterController controller;
        private PlayerInput input;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private float recoilOffset;
        private bool controlsEnabled = true;
        private Vector3 spawnPosition;
        private Vector3 pivotStandPosition;

        public Transform CameraPivot => cameraPivot;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInput>();
            if (cameraPivot == null)
            {
                Debug.LogError("[PlayerController] cameraPivot is not assigned.");
                cameraPivot = transform;
            }

            yaw = transform.eulerAngles.y;
            spawnPosition = transform.position;
            pivotStandPosition = cameraPivot.localPosition;
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += HandleDied;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= HandleDied;
        }

        private void HandleDied()
        {
            controlsEnabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            UpdateLook(dt);
            UpdateMovement(dt);

            if (!controlsEnabled && cameraPivot != transform)
            {
                // Simple death effect: drop and tilt the camera.
                Vector3 target = new Vector3(pivotStandPosition.x, 0.35f, pivotStandPosition.z);
                cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, target, dt * 4f);
            }
        }

        private void UpdateLook(float dt)
        {
            if (controlsEnabled)
            {
                Vector2 look = input.Look;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            recoilOffset = Mathf.Lerp(recoilOffset, 0f, 1f - Mathf.Exp(-recoilRecoverySpeed * dt));

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != transform)
            {
                float roll = controlsEnabled ? 0f : 25f;
                float finalPitch = Mathf.Clamp(pitch - recoilOffset, minPitch, maxPitch);
                cameraPivot.localRotation = Quaternion.Euler(finalPitch, 0f, roll);
            }
        }

        private void UpdateMovement(float dt)
        {
            Vector2 move = controlsEnabled ? input.Move : Vector2.zero;
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * moveSpeed;

            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controlsEnabled && grounded && input.JumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * dt;
            velocity.y = verticalVelocity;

            controller.Move(velocity * dt);

            if (transform.position.y < fallResetHeight) Teleport(spawnPosition);
        }

        /// <summary>Kicks the camera up and slightly sideways (weapon recoil).</summary>
        public void AddRecoil(float pitchKick, float yawKick)
        {
            recoilOffset = Mathf.Min(recoilOffset + pitchKick, 12f);
            yaw += Random.Range(-yawKick, yawKick);
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
        }
    }
}
