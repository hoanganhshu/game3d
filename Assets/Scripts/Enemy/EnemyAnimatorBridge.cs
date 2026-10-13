using UnityEngine;

namespace FPSGame
{
    /// <summary>Maps Enemy gameplay states to the Meshy humanoid Animator.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int CombatId = Animator.StringToHash("Combat");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int MotionRateId = Animator.StringToHash("MotionRate");
        private static readonly int DeathStateId = Animator.StringToHash("Death");

        [SerializeField] private Animator animator;
        [SerializeField] private float runSpeed = 4f;
        [SerializeField] private bool hasDeathAnimation;
        [SerializeField] private float deathDuration = 2.2f;
        [SerializeField] private float speedDamping = 0.2f;
        [SerializeField] private float groundSink = 0.02f;

        private float deathElapsed;
        private Renderer[] visualRenderers;
        private Transform characterRoot;
        private bool dead;

        private bool AnimatorReady => animator != null && animator.runtimeAnimatorController != null;

        public bool HasDeathAnimation => hasDeathAnimation && AnimatorReady;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            visualRenderers = GetComponentsInChildren<Renderer>(true);
            characterRoot = transform.parent != null ? transform.parent : transform;
        }

        public void ResetState()
        {
            if (!AnimatorReady) return;
            animator.Rebind();
            animator.speed = 1f;
            deathElapsed = 0f;
            dead = false;
            animator.Update(0f);
            animator.SetBool(DeadId, false);
        }

        public void UpdateMotion(float worldSpeed, EnemyState state)
        {
            if (!AnimatorReady) return;
            float normalizedSpeed = Mathf.Clamp01(worldSpeed / Mathf.Max(0.01f, runSpeed));
            animator.SetFloat(SpeedId, normalizedSpeed, speedDamping, Time.deltaTime);
            float authoredSpeed = normalizedSpeed > 0.72f ? runSpeed : runSpeed * 0.55f;
            float motionRate = worldSpeed < 0.05f ? 1f : Mathf.Clamp(worldSpeed / Mathf.Max(0.01f, authoredSpeed), 0.7f, 1.3f);
            animator.SetFloat(MotionRateId, motionRate, 0.15f, Time.deltaTime);
            animator.SetBool(CombatId, state == EnemyState.Attack);
        }

        public void PlayAttack()
        {
            if (AnimatorReady) animator.SetTrigger(AttackId);
        }

        public void PlayHit()
        {
            if (AnimatorReady) animator.SetTrigger(HitId);
        }

        public void PlayDeath()
        {
            if (!AnimatorReady) return;
            animator.ResetTrigger(AttackId);
            animator.ResetTrigger(HitId);
            animator.SetBool(DeadId, true);
            dead = true;
            // Enter immediately. A bool-only Any State transition can otherwise wait behind
            // an attack/hit transition and make the corpse appear frozen in its last pose.
            animator.Play(DeathStateId, 0, 0f);
            animator.Update(0f);
            // Meshy Generic clips in this model do not advance reliably after the
            // NavMeshAgent is disabled, so sample the non-looping death clip explicitly.
            animator.speed = 0f;
            deathElapsed = 0f;
        }

        public void UpdateDeath(float deltaTime)
        {
            if (!HasDeathAnimation) return;
            deathElapsed = Mathf.Min(deathElapsed + deltaTime, Mathf.Max(0.01f, deathDuration));
            animator.Play(DeathStateId, 0, deathElapsed / Mathf.Max(0.01f, deathDuration));
            animator.Update(0f);
        }

        private void LateUpdate()
        {
            if (dead || visualRenderers == null || visualRenderers.Length == 0 || characterRoot == null) return;

            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in visualRenderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!found) return;

            // The NavMeshAgent root already sits on the baked walkable surface. Ground the
            // visual against that root instead of raycasting: a ray could hit a nearby wall,
            // prop top or uneven decorative mesh and randomly sink/raise different enemies.
            float feetY = characterRoot.position.y - groundSink;
            float correction = feetY - bounds.min.y;
            if (Mathf.Abs(correction) > 0.004f)
                transform.position += Vector3.up * correction;
        }
    }
}
