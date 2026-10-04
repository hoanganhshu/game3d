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
            GameObject player = Instantiate(assets.playerPrefab, Definition.playerSpawn, rotation);
            player.name = "Player";

            PlayerTransform = player.transform;
            PlayerStats = player.GetComponent<PlayerHealth>();
            PlayerWeapons = player.GetComponent<WeaponManager>();
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

                Vector3 position = spawn.position;
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    position = hit.position;
                }
                else
                {
                    Debug.LogWarning("[LevelManager] Enemy spawn " + i + " is not on the NavMesh.");
                }

                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject enemyObject = Instantiate(prefab, position, rotation, enemiesRoot);
                if (enemyObject.TryGetComponent(out Enemy enemy))
                {
                    enemy.Initialize(PlayerTransform, PlayerStats);
                    TotalEnemies++;
                }
            }

            EnemiesRemaining = TotalEnemies;
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
