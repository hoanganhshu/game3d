using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace FPSGame
{
    /// <summary>
    /// Runs one level: builds the map from LevelDatabase, bakes the NavMesh at runtime, spawns
    /// the player and enemies, tracks time, kills and score, and decides win or lose
    /// (stars, saving, unlocking the next level).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private PoolManager poolManager;
        [SerializeField] private float winDelay = 0.8f;
        [SerializeField] private float loseDelay = 1.5f;

        public static LevelManager Instance { get; private set; }

        public int LevelIndex => levelIndex;
        public LevelDefinition Definition { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public PlayerHealth PlayerStats { get; private set; }
        public WeaponManager PlayerWeapons { get; private set; }
        public float ElapsedTime { get; private set; }
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int TotalEnemies { get; private set; }
        public int EnemiesRemaining { get; private set; }

        private bool finished;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += HandleEnemyKilled;
            GameEvents.PlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= HandleEnemyKilled;
            GameEvents.PlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            Definition = LevelDatabase.Get(levelIndex);

            GameAssets assets = GameAssets.Instance;
            if (assets == null || assets.playerPrefab == null)
            {
                Debug.LogError("[LevelManager] GameAssets is not set up. Run Tools > Setup Everything.");
                return;
            }

            if (levelBuilder == null) levelBuilder = new GameObject("Level").AddComponent<LevelBuilder>();
            if (poolManager == null) poolManager = gameObject.AddComponent<PoolManager>();

            levelBuilder.Build(Definition, assets);
            BakeNavMesh();
            levelBuilder.EnableArenaCollisions();
            SpawnPlayer(assets);
            PrewarmPools(assets);
            SpawnEnemies(assets);

            GameEvents.RaiseScoreChanged(Score);
            GameEvents.RaiseEnemiesRemainingChanged(EnemiesRemaining, TotalEnemies);

            if (GameManager.Instance != null) GameManager.Instance.BeginLevel(levelIndex);
        }

        private void Update()
        {
            if (!finished && GameManager.IsPlaying) ElapsedTime += Time.deltaTime;
        }

        private void BakeNavMesh()
        {
            if (navMeshSurface == null) navMeshSurface = levelBuilder.gameObject.AddComponent<NavMeshSurface>();

            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navMeshSurface.layerMask = ~0;
            Physics.SyncTransforms();
            navMeshSurface.BuildNavMesh();
        }

        private void SpawnPlayer(GameAssets assets)
        {
            Quaternion rotation = Quaternion.Euler(0f, Definition.playerYaw, 0f);
            Vector3 spawnPosition = levelBuilder.ScaleAndClampToArena(Definition.playerSpawn, 1.2f);
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit spawnHit, 8f, NavMesh.AllAreas))
            {
                spawnPosition = spawnHit.position + Vector3.up * 0.01f;
            }
            else
            {
                // The arena always has a clean navigation floor at world origin. Falling
                // back here keeps the player inside the map if authored data is ever invalid.
                spawnPosition = new Vector3(0f, 0.01f, 0f);
            }
            spawnPosition = FindClearPlayerSpawn(spawnPosition);
            spawnPosition = SnapPlayerToVisibleDeck(spawnPosition);

            GameObject player = Instantiate(assets.playerPrefab, spawnPosition, rotation);
            player.name = "Player";
            player.transform.localScale = Vector3.one * 0.75f;

            PlayerTransform = player.transform;
            PlayerStats = player.GetComponent<PlayerHealth>();
            PlayerWeapons = player.GetComponent<WeaponManager>();
        }

        private static Vector3 FindClearPlayerSpawn(Vector3 desired)
        {
            const float capsuleRadius = 0.34f;
            for (int ring = 0; ring <= 5; ring++)
            {
                float radius = ring * 2.5f;
                int samples = ring == 0 ? 1 : 12;
                for (int i = 0; i < samples; i++)
                {
                    float angle = samples == 1 ? 0f : i * Mathf.PI * 2f / samples;
                    Vector3 probe = desired + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) continue;

                    // Start the capsule above the floor skin. Meshy Arena 1's visible deck
                    // sits slightly above the hidden NavMesh plane and must not be mistaken
                    // for a wall blocking the spawn.
                    Vector3 bottom = hit.position + Vector3.up * 0.55f;
                    Vector3 top = hit.position + Vector3.up * 1.55f;
                    if (!Physics.CheckCapsule(bottom, top, capsuleRadius, Physics.DefaultRaycastLayers,
                            QueryTriggerInteraction.Ignore))
                    {
                        return hit.position + Vector3.up * 0.01f;
                    }
                }
            }

            Debug.LogWarning("[LevelManager] No completely clear player spawn was found; using the authored position.");
            return desired;
        }

        private Vector3 SnapPlayerToVisibleDeck(Vector3 navPosition)
        {
            if (levelBuilder == null || levelBuilder.GeometryRoot == null) return navPosition;

            Vector3 origin = navPosition + Vector3.up * 12f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 24f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.name == "NavigationFloor") continue;
                if (!hit.transform.IsChildOf(levelBuilder.GeometryRoot)) continue;
                if (hit.normal.y < 0.72f) continue;
                // Ignore roofs, tall props and decorative underside. The visible deck is
                // expected to remain close to the hidden navigation plane.
                if (hit.point.y < navPosition.y - 0.12f || hit.point.y > navPosition.y + 0.8f) continue;
                return new Vector3(navPosition.x, hit.point.y + 0.02f, navPosition.z);
            }

            return navPosition;
        }

        private void PrewarmPools(GameAssets assets)
        {
            poolManager.Prewarm(assets.impactEffectPrefab, 16);
            poolManager.Prewarm(assets.bloodEffectPrefab, 16);
            poolManager.Prewarm(assets.healthPickupPrefab, 4);
            poolManager.Prewarm(assets.ammoPickupPrefab, 4);
        }

        private void SpawnEnemies(GameAssets assets)
        {
            TotalEnemies = 0;
            for (int i = 0; i < Definition.enemies.Count; i++)
            {
                EnemySpawnDef spawn = Definition.enemies[i];
                GameObject prefab = spawn.kind == EnemyKind.Boss ? assets.bossPrefab : assets.enemyPrefab;
                if (prefab == null) continue;

                Vector3 position = levelBuilder.ScaleAndClampToArena(spawn.position, 0.8f);
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                {
                    // NavMeshAgent owns the root height. Do not add a visual Y offset here;
                    // EnemyAnimatorBridge grounds the animated mesh against this root.
                    position = hit.position;
                }

                else
                {
                    Debug.LogWarning("[LevelManager] Enemy spawn " + i + " is not on the NavMesh.");
                }

                position = FindClearEnemySpawn(position);

                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject enemyObject = Instantiate(prefab, position, rotation, enemiesRoot);
                enemyObject.transform.localScale *= 0.75f;
                if (enemyObject.TryGetComponent(out Enemy enemy))
                {
                    enemy.Initialize(PlayerTransform, PlayerStats);
                    TotalEnemies++;
                }
            }

            EnemiesRemaining = TotalEnemies;
        }

        private static Vector3 FindClearEnemySpawn(Vector3 desired)
        {
            const float capsuleRadius = 0.36f;
            // Meshy arenas can contain a large solid structure at an authored spawn. Search
            // far enough to get completely outside it instead of falling back inside the mesh.
            for (int ring = 0; ring <= 20; ring++)
            {
                float radius = ring * 1.5f;
                int samples = ring == 0 ? 1 : 16;
                for (int i = 0; i < samples; i++)
                {
                    float angle = samples == 1 ? 0f : i * Mathf.PI * 2f / samples;
                    Vector3 probe = desired + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 1.2f, NavMesh.AllAreas)) continue;

                    // The walkable combat deck is around y=0. Decorative blocks in Map 1
                    // also receive NavMesh polygons; reject those raised surfaces explicitly.
                    if (hit.position.y > 0.16f || hit.position.y < -0.08f) continue;

                    Vector3 bottom = hit.position + Vector3.up * 0.43f;
                    Vector3 top = hit.position + Vector3.up * 1.55f;
                    if (Physics.CheckCapsule(bottom, top, capsuleRadius,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;

                    return hit.position + Vector3.up * 0.01f;
                }
            }

            Debug.LogError("[LevelManager] No clear enemy spawn was found near " + desired +
                           "; moving it to the nearest safe NavMesh point.");
            if (NavMesh.SamplePosition(Vector3.zero, out NavMeshHit fallback, 30f, NavMesh.AllAreas))
                return fallback.position + Vector3.up * 0.01f;
            return new Vector3(0f, 0.01f, 0f);
        }

        private void HandleEnemyKilled(Enemy enemy)
        {
            if (finished || enemy == null) return;

            Kills++;
            Score += enemy.ScoreValue;
            EnemiesRemaining = Mathf.Max(0, EnemiesRemaining - 1);
            GameEvents.RaiseScoreChanged(Score);
            GameEvents.RaiseEnemiesRemainingChanged(EnemiesRemaining, TotalEnemies);

            if (EnemiesRemaining == 0) StartCoroutine(WinRoutine());
        }

        private void HandlePlayerDied()
        {
            if (finished) return;
            StartCoroutine(LoseRoutine());
        }

        private IEnumerator WinRoutine()
        {
            finished = true;
            yield return new WaitForSeconds(winDelay);

            float healthPercent = PlayerStats != null ? PlayerStats.Normalized : 0f;
            int finalScore = Score + ScoreCalculator.TimeBonus(ElapsedTime, Definition.parTime) +
                             ScoreCalculator.HealthBonus(healthPercent);
            int stars = ScoreCalculator.CalculateStars(ElapsedTime, healthPercent, Definition.parTime);

            bool newBest = false;
            int bestScore = finalScore;
            SaveManager save = SaveManager.Instance;
            if (save != null)
            {
                newBest = save.SubmitResult(levelIndex, finalScore, stars);
                bestScore = save.GetBestScore(levelIndex);
            }

            Score = finalScore;
            GameEvents.RaiseScoreChanged(Score);

            var result = new LevelResult
            {
                levelIndex = levelIndex,
                score = finalScore,
                stars = stars,
                time = ElapsedTime,
                healthPercent = healthPercent,
                kills = Kills,
                isNewBest = newBest,
                bestScore = bestScore,
                hasNextLevel = levelIndex + 1 < LevelDatabase.Count
            };

            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Won);
            AudioManager.PlaySound(SoundType.Win);
            GameEvents.RaiseLevelCompleted(result);
        }

        private IEnumerator LoseRoutine()
        {
            finished = true;
            yield return new WaitForSeconds(loseDelay);

            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Lost);
            AudioManager.PlaySound(SoundType.Lose);
            GameEvents.RaiseLevelFailed();
        }
    }
}
