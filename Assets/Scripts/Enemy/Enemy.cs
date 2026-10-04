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
        [SerializeField] private float damage = 7f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.55f;
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private int scoreValue = 100;
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.5f;

        [Header("References")]
        [SerializeField] private Transform eye;
        [SerializeField] private Transform gunPoint;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private EnemyHealthBar healthBar;
        [SerializeField] private Renderer bodyRenderer;

        private static int colorPropertyId = -1;

        private NavMeshAgent agent;
        private Collider ownCollider;
        private NavMeshPath path;
        private MaterialPropertyBlock propertyBlock;
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
            nextSenseTime = Time.time + Random.Range(0f, SenseInterval);
            nextFireTime = Time.time + Random.Range(0.5f, fireInterval);
            if (healthBar != null) healthBar.SetValue(1f);
        }

        private void Update()
        {
            if (State == EnemyState.Dead)
            {
                UpdateDeath();
                return;
            }

            UpdateFlashes();

            if (player == null || playerHealth == null || !AgentReady) return;

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

            // While patrolling, only see what is in front, unless the player is very close.
            if (State == EnemyState.Patrol && sqrDistance > 16f)
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
            else if (Physics.Raycast(eyePosition, direction / distance, out RaycastHit hit, distance + 0.5f,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                canSeePlayer = hit.transform == player;
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
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas) &&
                    agent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    return;
                }
            }
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

            if (Time.time >= nextRepathTime)
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
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
                flashOffTime = Time.time + 0.06f;
            }

            Vector3 soundPosition = gunPoint != null ? gunPoint.position : transform.position;
            AudioManager.PlaySoundAt(SoundType.EnemyShot, soundPosition, 0.8f);

            float hitChance = accuracy * Mathf.Lerp(1f, 0.5f, distance / Mathf.Max(1f, attackRange));
            if (Random.value <= hitChance)
            {
                playerHealth.TakeDamage(damage, player.position, -transform.forward);
            }
        }

        // ---------------- Damage & death ----------------

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (State == EnemyState.Dead || amount <= 0f) return;

            health -= amount;
            if (healthBar != null) healthBar.SetValue(health / maxHealth);

            SetBodyColor(Color.white);
            hitFlashOffTime = Time.time + 0.08f;

            if (health <= 0f)
            {
                Die();
                return;
            }

            // Getting shot alerts the enemy even if it did not see the player.
            if (State == EnemyState.Patrol)
            {
                lastSeenTime = Time.time;
                EnterChase();
            }
        }

        private void Die()
        {
            State = EnemyState.Dead;
            health = 0f;

            if (agent.enabled)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.enabled = false;
            }
            if (ownCollider != null) ownCollider.enabled = false;
            if (healthBar != null) healthBar.gameObject.SetActive(false);
            if (muzzleFlash != null) muzzleFlash.SetActive(false);

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
            }

            if (hitFlashOffTime > 0f && Time.time >= hitFlashOffTime)
            {
                hitFlashOffTime = 0f;
                ClearBodyColor();
            }
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
