using UnityEngine;

namespace FPSGame
{
    /// <summary>
    /// Holds references to the generated prefabs and materials. It lives at
    /// Resources/GameAssets.asset and is filled in by ProjectBootstrapper, so runtime code can
    /// get everything through <see cref="Instance"/> without Inspector setup.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAssets", menuName = "FPSGame/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        public const string ResourcePath = "GameAssets";

        [Header("Characters")]
        public GameObject playerPrefab;
        public GameObject enemyPrefab;
        public GameObject bossPrefab;

        [Header("Effects")]
        public GameObject impactEffectPrefab;
        public GameObject bloodEffectPrefab;

        [Header("Pickups")]
        public GameObject healthPickupPrefab;
        public GameObject ammoPickupPrefab;

        [Header("Level Materials")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material obstacleMaterial;

        private static GameAssets instance;

        public static GameAssets Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameAssets>(ResourcePath);
                    if (instance == null)
                    {
                        Debug.LogError("[GameAssets] Resources/GameAssets.asset is missing. Run Tools > Setup Everything.");
                    }
                }
                return instance;
            }
        }
    }
}
