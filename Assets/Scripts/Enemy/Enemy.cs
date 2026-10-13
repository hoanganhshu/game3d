using UnityEngine;
using UnityEngine.AI;

namespace FPSGame
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    /// <summary>
    /// NavMeshAgent enemy with the states Patrol -> Chase -> Attack -> Dead. It spots the player
    /// by distance, field of view and line of sight (checked a few times per second, not every
    /// frame). It shoots with an accuracy roll, shows a health bar and may drop a pickup on death.
    /// The boss uses the same script with bigger numbers.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        private const float SenseInterval = 0.2f;
        private const float RepathInterval = 0.3f;
        private const float DeathFallDuration = 0.5f;
        private const float CorpseLifetime = 4f;

        [Header("Stats")]
        [SerializeField] private bool isBoss;
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float patrolSpeed = 2.2f;
        [SerializeField] private float chaseSpeed = 4f;
        [SerializeField] private float detectionRange = 18f;
        [SerializeField] private float attackRange = 13f;
        [SerializeField] private float fieldOfView = 140f;
        [SerializeField] private float loseSightTime = 5f;
        [SerializeField] private float fireInterval = 1.2f;
        [SerializeField] private float damage = 3.5f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.55f;
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private int scoreValue = 100;
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.5f;
        [SerializeField] private float growlRange = 16f;
        [SerializeField] private Vector2 growlInterval = new Vector2(4.5f, 8f);

        [Header("References")]
        [SerializeField] private Transform eye;
        [SerializeField] private Transform gunPoint;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private EnemyHealthBar healthBar;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private EnemyAnimatorBridge animationBridge;

        private static int colorPropertyId = -1;
        private static Material tracerMaterial;

        private NavMeshAgent agent;
        private Collider ownCollider;
        private NavMeshPath path;
        private MaterialPropertyBlock propertyBlock;
        private LineRenderer shotTracer;
        private GameObject runtimeHeadFlash;
        private Transform player;
        private PlayerHealth playerHealth;

        private float health;
        private Vector3 homePosition;
        private bool canSeePlayer;
        private float nextSenseTime;
        private float nextRepathTime;
        private float nextFireTime;
        private float lastSeenTime = -999f;
        private float waitUntil = -1f;
        private float flashOffTime;
        private float hitFlashOffTime;
        private float deathTime;
        private Quaternion deathStartRotation;
        private Quaternion deathEndRotation;
        private float nextGrowlTime;
        private Vector3 lastSafePosition;
        private float avoidUntil;
        private bool combatRangesExpanded;

        public EnemyState State { get; private set; } = EnemyState.Patrol;
        public bool IsBoss => isBoss;
        public int ScoreValue => scoreValue;
        public bool IsAlive => State != EnemyState.Dead;

        private bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

        private void Awake()
        {
            if (colorPropertyId < 0) colorPropertyId = Shader.PropertyToID("_Color");

            agent = GetComponent<NavMeshAgent>();
            ownCollider = GetComponent<Collider>();
            path = new NavMeshPath();
            propertyBlock = new MaterialPropertyBlock();
            if (animationBridge == null) animationBridge = GetComponentInChildren<EnemyAnimatorBridge>();
            health = maxHealth;
            homePosition = transform.position;
            if (muzzleFlash != null) muzzleFlash.SetActive(false);
        }

        public void Initialize(Transform target, PlayerHealth targetHealth)
        {
            player = target;
            playerHealth = targetHealth;
            homePosition = transform.position;
            agent.speed = patrolSpeed;
            if (!combatRangesExpanded)
            {
                detectionRange = Mathf.Max(detectionRange, isBoss ? 42f : 32f) * 3f;
                // If the player is visible anywhere inside the detection radius, the enemy
                // is also allowed to fire. Line of sight in Sense() still prevents wall shots.
                attackRange = detectionRange;
                combatRangesExpanded = true;
            }
            fieldOfView = Mathf.Max(fieldOfView, 220f);
            lastSafePosition = transform.position;
            nextSenseTime = Time.time + Random.Range(0f, SenseInterval);
            nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
            nextGrowlTime = Time.time + Random.Range(1.5f, 4f);
            if (healthBar != null) healthBar.SetValue(1f);
            if (animationBridge != null) animationBridge.ResetState();
        }

        private void Update()
        {
            if (State == EnemyState.Dead)
            {
                UpdateDeath();
                return;
            }

            UpdateFlashes();

            if (animationBridge != null)
            {
                float motionSpeed = AgentReady ? agent.velocity.magnitude : 0f;
                animationBridge.UpdateMotion(motionSpeed, State);
            }

            if (player == null || playerHealth == null || !AgentReady) return;

            float playerDistanceSqr = (player.position - transform.position).sqrMagnitude;
            if (playerDistanceSqr <= growlRange * growlRange && Time.time >= nextGrowlTime)
            {
                nextGrowlTime = Time.time + Random.Range(growlInterval.x, growlInterval.y);
                AudioManager.PlaySoundAt(SoundType.EnemyGrowl, transform.position + Vector3.up, isBoss ? 0.9f : 0.58f);
            }

            if (!GameManager.IsPlaying)
            {
                if (!agent.isStopped) agent.isStopped = true;
                return;
            }

            if (Time.time >= nextSenseTime)
            {
                nextSenseTime = Time.time + SenseInterval;
                Sense();
            }

            switch (State)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Chase:
                    UpdateChase();
                    break;
                case EnemyState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        // ---------------- Perception ----------------

        private void Sense()
        {
            canSeePlayer = false;
            if (!playerHealth.IsAlive) return;

            Vector3 toPlayer = player.position - transform.position;
            float sqrDistance = toPlayer.sqrMagnitude;
            float range = State == EnemyState.Patrol ? detectionRange : detectionRange * 1.6f;
            if (sqrDistance > range * range) return;

            // Inside weapon range the enemy must react regardless of its initial facing.
            // The old four-metre threshold made a nearby enemy appear completely passive.
            if (State == EnemyState.Patrol && sqrDistance > attackRange * attackRange)
            {
                Vector3 flat = toPlayer;
                flat.y = 0f;
                if (Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f) return;
            }

            Vector3 eyePosition = eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
            Vector3 targetPoint = player.position + Vector3.up * 1.4f;
            Vector3 direction = targetPoint - eyePosition;
            float distance = direction.magnitude;

            if (distance < 0.01f)
            {
                canSeePlayer = true;
            }
            else
            {
                RaycastHit[] hits = Physics.RaycastAll(eyePosition, direction / distance, distance + 0.5f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                bool foundBlocker = false;
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                    if (hit.transform == player || hit.transform.IsChildOf(player))
                    {
                        canSeePlayer = true;
                        break;
                    }

                    foundBlocker = true;
                    break;
                }

                // Some player controllers do not expose a raycast collider at chest height.
                // An empty ray therefore means a clear, unobstructed segment to the target.
                if (!foundBlocker && !canSeePlayer) canSeePlayer = true;
            }

            if (canSeePlayer) lastSeenTime = Time.time;
        }

        // ---------------- States ----------------

        private void UpdatePatrol()
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = patrolSpeed;

            if (canSeePlayer)
            {
                EnterChase();
                return;
            }

            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.3f))
            {
                if (waitUntil < 0f)
                {
                    waitUntil = Time.time + Random.Range(1f, 3f);
                }
                else if (Time.time >= waitUntil)
                {
                    waitUntil = -1f;
                    PickPatrolPoint();
                }
            }
        }

        private void PickPatrolPoint()
        {
            for (int attempt = 0; attempt < 16; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas) &&
                    HasClearPhysicalRoute(hit.position) &&
                    agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    return;
                }
            }

            // No clear candidate from this position: turn away immediately and retry shortly.
            transform.Rotate(0f, Random.Range(100f, 260f), 0f);
            waitUntil = Time.time + 0.18f;
        }

        private void EnterChase()
        {
            State = EnemyState.Chase;
            nextRepathTime = 0f;
            waitUntil = -1f;
        }

        private void UpdateChase()
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.speed = chaseSpeed;

            float sqrDistance = (player.position - transform.position).sqrMagnitude;
            if (canSeePlayer && sqrDistance <= attackRange * attackRange)
            {
                State = EnemyState.Attack;
                agent.ResetPath();
                return;
            }

            if (!canSeePlayer && Time.time - lastSeenTime > loseSightTime)
            {
                State = EnemyState.Patrol;
                waitUntil = -1f;
                agent.ResetPath();
                return;
            }

            if (Time.time >= nextRepathTime && Time.time >= avoidUntil)
            {
                nextRepathTime = Time.time + RepathInterval;
                agent.SetDestination(player.position);
            }
        }

        private void UpdateAttack()
        {
            agent.isStopped = true;
            agent.updateRotation = false;

            if (!playerHealth.IsAlive)
            {
                State = EnemyState.Patrol;
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float sqrDistance = toPlayer.sqrMagnitude;
            if (sqrDistance > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(toPlayer), 360f * Time.deltaTime);
            }

            if (!canSeePlayer || sqrDistance > attackRange * attackRange * 1.2f)
            {
                EnterChase();
                return;
            }

            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireInterval * Random.Range(0.85f, 1.15f);
                Shoot(Mathf.Sqrt(sqrDistance));
            }
        }

        private void Shoot(float distance)
        {
            if (animationBridge != null) animationBridge.PlayAttack();
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
                flashOffTime = Time.time + 0.06f;
            }

            // Imported Meshy weapon sockets are inconsistent between models. Start the shot
            // at head/eye height so the tracer visibly comes from the monster toward the player.
            Vector3 soundPosition = eye != null
                ? eye.position + transform.forward * 0.28f
                : transform.position + Vector3.up * 1.45f + transform.forward * 0.28f;
            ShowHeadMuzzleFlash(soundPosition);
            AudioManager.PlaySoundAt(SoundType.EnemyShot, soundPosition, 0.8f);
            Vector3 targetPoint = player.position + Vector3.up * 0.85f;
            ShowShotTracer(soundPosition, targetPoint);

            float distance01 = Mathf.Clamp01(distance / Mathf.Max(1f, attackRange));
            float hitChance = Mathf.Lerp(0.9f, accuracy * 0.55f, distance01);
            if (Random.value <= hitChance)
            {
                playerHealth.TakeDamage(damage, player.position, -transform.forward);
            }
        }

        private void LateUpdate()
        {
            if (!AgentReady || State == EnemyState.Dead) return;

            float radius = Mathf.Max(0.18f, agent.radius * 0.72f);
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.18f);
            Vector3 top = transform.position + Vector3.up * Mathf.Max(radius + 0.2f, agent.height - radius);
            Collider[] overlaps = Physics.OverlapCapsule(bottom, top, radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            bool blocked = false;
            foreach (Collider overlap in overlaps)
            {
                if (!IsBlockingCollider(overlap)) continue;
                blocked = true;
                break;
            }

            if (blocked)
            {
                agent.Warp(lastSafePosition);
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                if (State == EnemyState.Patrol)
                {
                    waitUntil = -1f;
                    PickPatrolPoint();
                }
                else if (State == EnemyState.Chase)
                {
                    SetAvoidanceDestination();
                }
            }
            else
            {
                lastSafePosition = transform.position;
            }
        }

        // ---------------- Damage & death ----------------

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (State == EnemyState.Dead || amount <= 0f) return;

            health -= amount;
            if (animationBridge != null) animationBridge.PlayHit();
            if (healthBar != null) healthBar.SetValue(health / maxHealth);

            SetBodyColor(Color.white);
            hitFlashOffTime = Time.time + 0.08f;

            if (health <= 0f)
            {
                Die();
                return;
            }

            // Being attacked always alerts the enemy. Re-evaluate line of sight immediately:
            // shoot back when the attacker is visible, otherwise chase/search the player's
            // last known position until loseSightTime expires.
            lastSeenTime = Time.time;
            nextSenseTime = 0f;
            if (player != null && playerHealth != null && playerHealth.IsAlive)
            {
                Sense();
                if (canSeePlayer && AgentReady)
                {
                    State = EnemyState.Attack;
                    agent.ResetPath();
                    nextFireTime = Mathf.Min(nextFireTime, Time.time + 0.12f);
                }
                else
                {
                    EnterChase();
                }
            }
        }

        private void Die()
        {
            State = EnemyState.Dead;
            if (animationBridge != null) animationBridge.PlayDeath();
            health = 0f;

            if (agent.enabled)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
            }
            if (ownCollider != null) ownCollider.enabled = false;
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (muzzleFlash != null) muzzleFlash.SetActive(false);
            if (shotTracer != null) shotTracer.enabled = false;

            hitFlashOffTime = 0f;
            SetBodyColor(new Color(0.25f, 0.25f, 0.25f, 1f));

            AudioManager.PlaySoundAt(SoundType.EnemyDeath, transform.position);
            DropLoot();

            deathTime = Time.time;
            deathStartRotation = transform.rotation;
            deathEndRotation = transform.rotation * Quaternion.Euler(-90f, 0f, 0f);

            GameEvents.RaiseEnemyKilled(this);
        }

        private void UpdateDeath()
        {
            if (animationBridge != null && animationBridge.HasDeathAnimation)
            {
                animationBridge.UpdateDeath(Time.deltaTime);
                if (Time.time - deathTime > CorpseLifetime) Destroy(gameObject);
                return;
            }

            float t = Mathf.Clamp01((Time.time - deathTime) / DeathFallDuration);
            transform.rotation = Quaternion.Slerp(deathStartRotation, deathEndRotation, t * t);
            if (Time.time - deathTime > CorpseLifetime) Destroy(gameObject);
        }

        private void DropLoot()
        {
            GameAssets assets = GameAssets.Instance;
            if (assets == null) return;

            Vector3 position = transform.position + Vector3.up * 0.5f;
            if (isBoss)
            {
                PoolManager.SpawnFromPool(assets.healthPickupPrefab, position + transform.right, Quaternion.identity);
                PoolManager.SpawnFromPool(assets.ammoPickupPrefab, position - transform.right, Quaternion.identity);
                return;
            }

            if (Random.value > dropChance) return;
            GameObject prefab = Random.value < 0.5f ? assets.healthPickupPrefab : assets.ammoPickupPrefab;
            PoolManager.SpawnFromPool(prefab, position, Quaternion.identity);
        }

        // ---------------- Visual feedback ----------------

        private void UpdateFlashes()
        {
            if (flashOffTime > 0f && Time.time >= flashOffTime)
            {
                flashOffTime = 0f;
                if (muzzleFlash != null) muzzleFlash.SetActive(false);
                if (runtimeHeadFlash != null) runtimeHeadFlash.SetActive(false);
                if (shotTracer != null) shotTracer.enabled = false;
            }

            if (hitFlashOffTime > 0f && Time.time >= hitFlashOffTime)
            {
                hitFlashOffTime = 0f;
                ClearBodyColor();
            }
        }

        private void ShowShotTracer(Vector3 start, Vector3 end)
        {
            if (shotTracer == null)
            {
                GameObject tracerObject = new GameObject("EnemyShotTracer");
                tracerObject.transform.SetParent(transform, false);
                shotTracer = tracerObject.AddComponent<LineRenderer>();
                shotTracer.useWorldSpace = true;
                shotTracer.positionCount = 2;
                shotTracer.startWidth = 0.045f;
                shotTracer.endWidth = 0.015f;
                shotTracer.numCapVertices = 3;
                shotTracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shotTracer.receiveShadows = false;
                if (tracerMaterial == null)
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader != null) tracerMaterial = new Material(shader) { name = "Runtime_EnemyTracer" };
                }
                if (tracerMaterial != null) shotTracer.sharedMaterial = tracerMaterial;
            }

            Color color = isBoss ? new Color(1f, 0.12f, 0.04f, 1f) : new Color(1f, 0.35f, 0.08f, 1f);
            shotTracer.startColor = color;
            shotTracer.endColor = new Color(color.r, color.g, color.b, 0.15f);
            shotTracer.SetPosition(0, start);
            shotTracer.SetPosition(1, end);
            shotTracer.enabled = true;
            flashOffTime = Mathf.Max(flashOffTime, Time.time + 0.09f);
        }

        private void ShowHeadMuzzleFlash(Vector3 position)
        {
            if (runtimeHeadFlash == null)
            {
                runtimeHeadFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                runtimeHeadFlash.name = "EnemyHeadMuzzleFlash";
                runtimeHeadFlash.transform.SetParent(transform, true);
                Collider flashCollider = runtimeHeadFlash.GetComponent<Collider>();
                if (flashCollider != null) Destroy(flashCollider);
                Renderer flashRenderer = runtimeHeadFlash.GetComponent<Renderer>();
                Shader shader = Shader.Find("Sprites/Default");
                if (flashRenderer != null && shader != null)
                {
                    Material material = new Material(shader) { name = "Runtime_EnemyHeadFlash" };
                    material.color = isBoss
                        ? new Color(1f, 0.08f, 0.02f, 1f)
                        : new Color(1f, 0.48f, 0.04f, 1f);
                    flashRenderer.sharedMaterial = material;
                    flashRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    flashRenderer.receiveShadows = false;
                }
            }

            runtimeHeadFlash.transform.position = position;
            runtimeHeadFlash.transform.localScale = Vector3.one * (isBoss ? 0.3f : 0.2f);
            runtimeHeadFlash.SetActive(true);
            flashOffTime = Mathf.Max(flashOffTime, Time.time + 0.09f);
        }

        private bool HasClearPhysicalRoute(Vector3 destination)
        {
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 0.25f) return true;

            float radius = Mathf.Max(0.18f, agent.radius * 0.65f);
            Vector3 origin = transform.position + Vector3.up * Mathf.Max(radius + 0.25f, agent.height * 0.45f);
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, delta / distance, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (IsBlockingCollider(hit.collider)) return false;
            }
            return true;
        }

        private bool IsBlockingCollider(Collider candidate)
        {
            if (candidate == null || candidate == ownCollider || candidate.transform.IsChildOf(transform)) return false;
            if (player != null && (candidate.transform == player || candidate.transform.IsChildOf(player))) return false;
            if (candidate.GetComponentInParent<Enemy>() != null) return false;
            if (candidate.name == "NavigationFloor") return false;
            return true;
        }

        private void SetAvoidanceDestination()
        {
            Vector3 forward = player != null ? player.position - transform.position : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward) * (Random.value < 0.5f ? -1f : 1f);

            for (int attempt = 0; attempt < 6; attempt++)
            {
                float sideDistance = agent.radius * (4f + attempt) + 1.25f;
                Vector3 candidate = transform.position + side * sideDistance - forward * (0.5f + attempt * 0.2f);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) &&
                    HasClearPhysicalRoute(hit.position) && agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    avoidUntil = Time.time + 0.9f;
                    nextRepathTime = avoidUntil;
                    return;
                }
                side = -side;
            }

            transform.Rotate(0f, 150f, 0f);
            avoidUntil = Time.time + 0.35f;
            nextRepathTime = avoidUntil;
        }

        private void SetBodyColor(Color color)
        {
            if (bodyRenderer == null) return;
            propertyBlock.SetColor(colorPropertyId, color);
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ClearBodyColor()
        {
            if (bodyRenderer == null) return;
            propertyBlock.Clear();
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
