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
        [SerializeField] private float moveSpeed = 8.25f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField, Min(1)] private int maxConsecutiveJumps = 3;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float recoilRecoverySpeed = 9f;
        [SerializeField] private float fallResetHeight = -20f;
        [SerializeField] private float runSpeedMultiplier = 1.65f;
        [SerializeField] private float runAcceleration = 4f;
        [SerializeField] private float runDeceleration = 6f;
        [SerializeField] private float crouchHeightMultiplier = 0.58f;
        [SerializeField] private float crouchSpeedMultiplier = 0.58f;
        [SerializeField] private float crouchTransitionSpeed = 10f;

        private CharacterController controller;
        private PlayerInput input;
        private float yaw;
        private float pitch;
        private float verticalVelocity;
        private int jumpsUsed;
        private float recoilOffset;
        private bool controlsEnabled = true;
        private Vector3 spawnPosition;
        private Vector3 pivotStandPosition;
        private float currentMoveSpeed;
        private float standHeight;
        private Vector3 standCenter;

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
            currentMoveSpeed = moveSpeed;
            standHeight = controller.height;
            standCenter = controller.center;
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
            UpdateCrouch(dt);
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
            bool wantsRun = controlsEnabled && input.RunHeld && move.y > 0.1f;
            float postureSpeed = input.CrouchHeld ? crouchSpeedMultiplier : 1f;
            float targetSpeed = moveSpeed * (wantsRun ? runSpeedMultiplier : 1f) * postureSpeed;
            float acceleration = wantsRun ? runAcceleration : runDeceleration;
            currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, acceleration * dt);
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * currentMoveSpeed;

            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
                jumpsUsed = 0;
            }

            if (controlsEnabled && input.JumpPressed && jumpsUsed < maxConsecutiveJumps)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpsUsed++;
            }
            verticalVelocity += gravity * dt;
            velocity.y = verticalVelocity;

            controller.Move(velocity * dt);

            bool walking = grounded && controlsEnabled && input.Move.sqrMagnitude > 0.04f;
            float runAmount = Mathf.InverseLerp(moveSpeed, moveSpeed * runSpeedMultiplier, currentMoveSpeed);
            AudioManager.SetFootsteps(walking, runAmount);

            if (transform.position.y < fallResetHeight) Teleport(spawnPosition);
        }

        private void UpdateCrouch(float dt)
        {
            if (controller == null || cameraPivot == transform) return;

            bool wantsCrouch = controlsEnabled && input.CrouchHeld;
            float targetHeight = wantsCrouch ? standHeight * crouchHeightMultiplier : standHeight;
            if (!wantsCrouch && !CanStandUp()) targetHeight = controller.height;

            float bottom = standCenter.y - standHeight * 0.5f;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * dt);
            Vector3 center = controller.center;
            center.y = bottom + controller.height * 0.5f;
            controller.center = center;

            Vector3 targetPivot = pivotStandPosition;
            if (wantsCrouch) targetPivot.y -= standHeight * (1f - crouchHeightMultiplier) * 0.7f;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPivot,
                1f - Mathf.Exp(-crouchTransitionSpeed * dt));
        }

        private bool CanStandUp()
        {
            float radius = Mathf.Max(0.05f, controller.radius * 0.92f);
            Vector3 worldCenter = transform.TransformPoint(standCenter);
            float halfSegment = Mathf.Max(0f, standHeight * 0.5f - radius);
            Vector3 bottom = worldCenter - transform.up * halfSegment;
            Vector3 top = worldCenter + transform.up * halfSegment;
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap.transform == transform || overlap.transform.IsChildOf(transform)) continue;
                return false;
            }
            return true;
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
            jumpsUsed = 0;
            currentMoveSpeed = moveSpeed;
        }
    }
}
