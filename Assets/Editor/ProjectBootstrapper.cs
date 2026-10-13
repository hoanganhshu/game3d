using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FPSGame.Editor
{
    /// <summary>
    /// Builds the whole project from code: folders, textures, materials, weapon data, prefabs,
    /// the GameAssets registry, all six scenes with their UI and references, and Build Settings.
    /// Safe to run again; generated assets are overwritten.
    ///
    /// Menu:          Tools > Setup Everything
    /// Command line:  -executeMethod FPSGame.Editor.ProjectBootstrapper.SetupAll
    /// </summary>
    public static class ProjectBootstrapper
    {
        private const string ArtFolder = "Assets/Art";
        private const string MaterialsFolder = "Assets/Materials";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string DataFolder = "Assets/Data";
        private const string ResourcesFolder = "Assets/Resources";
        private const string ScenesFolder = "Assets/Scenes";

        public static readonly string[] ScenePaths =
        {
            ScenesFolder + "/" + SceneNames.Boot + ".unity",
            ScenesFolder + "/" + SceneNames.MainMenu + ".unity",
            ScenesFolder + "/" + SceneNames.LevelSelect + ".unity",
            ScenesFolder + "/" + SceneNames.Level1 + ".unity",
            ScenesFolder + "/" + SceneNames.Level2 + ".unity",
            ScenesFolder + "/" + SceneNames.Level3 + ".unity"
        };

        // ---------------- Palette ----------------
        private static readonly Color BackgroundColor = new Color(0.06f, 0.07f, 0.1f, 1f);
        private static readonly Color PanelColor = new Color(0.1f, 0.12f, 0.17f, 0.97f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PrimaryButtonColor = new Color(0.18f, 0.42f, 0.85f, 1f);
        private static readonly Color SecondaryButtonColor = new Color(0.25f, 0.28f, 0.36f, 1f);
        private static readonly Color DangerButtonColor = new Color(0.72f, 0.22f, 0.2f, 1f);
        private static readonly Color SuccessColor = new Color(0.35f, 0.9f, 0.45f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.78f, 0.2f, 1f);
        private static readonly Color TextColor = new Color(0.95f, 0.96f, 0.98f, 1f);
        private static readonly Color MutedTextColor = new Color(0.65f, 0.68f, 0.76f, 1f);
        private static readonly Color SkyColor = new Color(0.56f, 0.7f, 0.86f, 1f);
        private static readonly Color StarOnColor = new Color(1f, 0.8f, 0.15f, 1f);
        private static readonly Color StarOffColor = new Color(0.22f, 0.24f, 0.3f, 1f);

        // ---------------- Generated assets (filled during SetupAll) ----------------
        private static Sprite circleSprite;
        private static Texture2D checkerTexture;

        private static Material floorMaterial;
        private static Material wallMaterial;
        private static Material obstacleMaterial;
        private static Material enemyMaterial;
        private static Material bossMaterial;
        private static Material visorMaterial;
        private static Material weaponDarkMaterial;
        private static Material weaponAccentMaterial;
        private static Material muzzleFlashMaterial;
        private static Material impactMaterial;
        private static Material bloodMaterial;
        private static Material healthPickupMaterial;
        private static Material ammoPickupMaterial;
        private static Material healthBarBackMaterial;
        private static Material healthBarFillMaterial;

        private static WeaponData pistolData;
        private static WeaponData rifleData;

        private static GameObject playerPrefab;
        private static GameObject enemyPrefab;
        private static GameObject bossPrefab;
        private static GameObject impactPrefab;
        private static GameObject bloodPrefab;
        private static GameObject healthPickupPrefab;
        private static GameObject ammoPickupPrefab;

        // =====================================================================
        // Entry points
        // =====================================================================

        [MenuItem("Tools/Setup Everything", false, 0)]
        public static void SetupEverythingMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("FPS Game", "Exit Play mode before running the setup.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            SetupAll();
            EditorUtility.DisplayDialog("FPS Game",
                "Setup complete.\n\nThe Boot scene is open - press Play to start.\n" +
                "Keyboard + mouse in the Editor (F2 toggles the touch controls).", "OK");
        }

        public static void SetupAll()
        {
            try
            {
                Debug.Log("[Setup] Starting project setup...");

                EnsureLegacyInputEnabled();
                EnsureFolders();
                circleSprite = CreateCircleSprite();
                checkerTexture = CreateCheckerTexture();
                CreateMaterials();
                CreateWeaponData();

                // A clean scene to build prefabs in.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreatePrefabs();
                CreateGameAssets();

                CreateBootScene();
                CreateMainMenuScene();
                CreateLevelSelectScene();
                for (int i = 0; i < LevelDatabase.Count; i++) CreateLevelScene(i);

                ConfigureBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.OpenScene(ScenePaths[0], OpenSceneMode.Single);
                Debug.Log("[Setup] Project setup complete. " + ScenePaths.Length + " scenes added to Build Settings.");
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[Setup] FAILED: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        // =====================================================================
        // Project settings
        // =====================================================================

        /// <summary>
        /// The game uses the legacy Input/Touch API. Unity 6 templates may enable only the new
        /// Input System (activeInputHandler = 1), which makes UnityEngine.Input throw. Switch to
        /// "Both" (2) in that case. The change takes effect the next time the editor starts.
        /// </summary>
        private static void EnsureLegacyInputEnabled()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0 || settings[0] == null)
            {
                Debug.LogWarning("[Setup] Could not load ProjectSettings.asset to check the input handler.");
                return;
            }

            var serializedSettings = new SerializedObject(settings[0]);
            SerializedProperty handler = serializedSettings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue != 1) return;

            handler.intValue = 2;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[Setup] Active Input Handling switched to 'Both' so the legacy Input API works. " +
                             "Restart the editor (or run the next batchmode command) for it to take effect.");
        }

        // =====================================================================
        // Folders & textures
        // =====================================================================

        private static void EnsureFolders()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ScenesFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Sprite CreateCircleSprite()
        {
            string path = ArtFolder + "/Circle.png";
            const int size = 128;
            var pixels = new Color32[size * size];
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - 1f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            WritePng(path, size, size, pixels);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.Exception("Could not import " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new System.Exception("Circle sprite could not be loaded.");
            return sprite;
        }

        private static Texture2D CreateCheckerTexture()
        {
            string path = ArtFolder + "/Checker.png";
            const int size = 64;
            var light = new Color32(200, 204, 210, 255);
            var dark = new Color32(160, 166, 175, 255);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLight = (x < size / 2) == (y < size / 2);
                    pixels[y * size + x] = isLight ? light : dark;
                }
            }

            WritePng(path, size, size, pixels);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new System.Exception("Could not import " + path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new System.Exception("Checker texture could not be loaded.");
            return texture;
        }

        private static void WritePng(string assetPath, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        // =====================================================================
        // Materials & data
        // =====================================================================

        private static void CreateMaterials()
        {
            floorMaterial = CreateMaterial("Floor", Color.white, false, checkerTexture);
            wallMaterial = CreateMaterial("Wall", new Color(0.42f, 0.47f, 0.56f), false);
            obstacleMaterial = CreateMaterial("Obstacle", new Color(0.72f, 0.5f, 0.28f), false);
            enemyMaterial = CreateMaterial("Enemy", new Color(0.82f, 0.18f, 0.16f), false);
            bossMaterial = CreateMaterial("Boss", new Color(0.48f, 0.16f, 0.7f), false);
            weaponDarkMaterial = CreateMaterial("WeaponDark", new Color(0.12f, 0.13f, 0.15f), false);
            weaponAccentMaterial = CreateMaterial("WeaponAccent", new Color(0.35f, 0.37f, 0.42f), false);
            visorMaterial = CreateMaterial("EnemyVisor", new Color(1f, 0.85f, 0.2f), true);
            muzzleFlashMaterial = CreateMaterial("MuzzleFlash", new Color(1f, 0.85f, 0.4f), true);
            impactMaterial = CreateMaterial("Impact", new Color(0.85f, 0.8f, 0.6f), true);
            bloodMaterial = CreateMaterial("Blood", new Color(0.7f, 0.05f, 0.05f), true);
            healthPickupMaterial = CreateMaterial("HealthPickup", new Color(0.2f, 0.95f, 0.35f), true);
            ammoPickupMaterial = CreateMaterial("AmmoPickup", new Color(1f, 0.8f, 0.15f), true);
            healthBarBackMaterial = CreateMaterial("HealthBarBack", new Color(0.08f, 0.08f, 0.08f), true);
            healthBarFillMaterial = CreateMaterial("HealthBarFill", new Color(0.9f, 0.2f, 0.2f), true);
        }

        private static Material CreateMaterial(string materialName, Color color, bool unlit, Texture texture = null)
        {
            string path = MaterialsFolder + "/" + materialName + ".mat";
            Shader shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) throw new System.Exception("Shader not found for material " + materialName);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = color;
            if (!unlit)
            {
                material.SetFloat("_Glossiness", 0.15f);
                material.SetFloat("_Metallic", 0f);
                material.mainTexture = texture;
                material.enableInstancing = true;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateWeaponData()
        {
            pistolData = LoadOrCreateAsset<WeaponData>(DataFolder + "/Pistol.asset");
            pistolData.weaponName = "PISTOL";
            pistolData.automatic = false;
            pistolData.damage = 25f;
            pistolData.fireRate = 4f;
            pistolData.magazineSize = 12;
            pistolData.startReserve = 48;
            pistolData.maxReserve = 96;
            pistolData.reloadTime = 1.2f;
            pistolData.range = 80f;
            pistolData.spread = 0.008f;
            pistolData.recoilPitch = 2.2f;
            pistolData.recoilYaw = 0.4f;
            pistolData.fireSound = SoundType.PistolShot;
            EditorUtility.SetDirty(pistolData);

            rifleData = LoadOrCreateAsset<WeaponData>(DataFolder + "/Rifle.asset");
            rifleData.weaponName = "RIFLE";
            rifleData.automatic = true;
            rifleData.damage = 14f;
            rifleData.fireRate = 10f;
            rifleData.magazineSize = 30;
            rifleData.startReserve = 90;
            rifleData.maxReserve = 180;
            rifleData.reloadTime = 1.8f;
            rifleData.range = 120f;
            rifleData.spread = 0.02f;
            rifleData.recoilPitch = 1.0f;
            rifleData.recoilYaw = 0.6f;
            rifleData.fireSound = SoundType.RifleShot;
            EditorUtility.SetDirty(rifleData);
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        // =====================================================================
        // Prefabs
        // =====================================================================

        private static void CreatePrefabs()
        {
            impactPrefab = CreateImpactPrefab("BulletImpact", impactMaterial, new Color(0.9f, 0.85f, 0.65f));
            bloodPrefab = CreateImpactPrefab("BloodImpact", bloodMaterial, new Color(0.75f, 0.05f, 0.05f));
            healthPickupPrefab = CreatePickupPrefab("HealthPickup", PickupType.Health);
            ammoPickupPrefab = CreatePickupPrefab("AmmoPickup", PickupType.Ammo);
            enemyPrefab = CreateEnemyPrefab("Enemy", false);
            bossPrefab = CreateEnemyPrefab("Boss", true);
            playerPrefab = CreatePlayerPrefab();
            CreateManagersPrefab();
        }

        private static GameObject CreateImpactPrefab(string prefabName, Material material, Color color)
        {
            var root = new GameObject(prefabName);
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = color;
            main.gravityModifier = 1f;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)12) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            PooledObject pooled = root.AddComponent<PooledObject>();
            SetFloat(pooled, "lifetime", 0.6f);

            return SavePrefab(root, PrefabsFolder + "/" + prefabName + ".prefab");
        }

        private static GameObject CreatePickupPrefab(string prefabName, PickupType type)
        {
            var root = new GameObject(prefabName);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            if (type == PickupType.Health)
            {
                CreatePart(PrimitiveType.Cube, "CrossHorizontal", visual, Vector3.zero, new Vector3(0.6f, 0.2f, 0.2f), healthPickupMaterial, true);
                CreatePart(PrimitiveType.Cube, "CrossVertical", visual, Vector3.zero, new Vector3(0.2f, 0.6f, 0.2f), healthPickupMaterial, true);
            }
            else
            {
                CreatePart(PrimitiveType.Cube, "Box", visual, Vector3.zero, new Vector3(0.5f, 0.3f, 0.32f), ammoPickupMaterial, true);
                CreatePart(PrimitiveType.Cylinder, "Round1", visual, new Vector3(-0.12f, 0.24f, 0f), new Vector3(0.08f, 0.09f, 0.08f), weaponAccentMaterial, true);
                CreatePart(PrimitiveType.Cylinder, "Round2", visual, new Vector3(0f, 0.24f, 0f), new Vector3(0.08f, 0.09f, 0.08f), weaponAccentMaterial, true);
                CreatePart(PrimitiveType.Cylinder, "Round3", visual, new Vector3(0.12f, 0.24f, 0f), new Vector3(0.08f, 0.09f, 0.08f), weaponAccentMaterial, true);
            }

            PooledObject pooled = root.AddComponent<PooledObject>();
            SetFloat(pooled, "lifetime", 30f);

            Pickup pickup = root.AddComponent<Pickup>();
            SetEnum(pickup, "type", (int)type);
            SetRef(pickup, "visual", visual);

            return SavePrefab(root, PrefabsFolder + "/" + prefabName + ".prefab");
        }

        private static GameObject CreateEnemyPrefab(string prefabName, bool boss)
        {
            var root = new GameObject(prefabName);

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.45f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.45f;
            agent.height = 2f;
            agent.baseOffset = 0f;
            agent.speed = 2.2f;
            agent.angularSpeed = 400f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = boss ? 30 : 50;

            GameObject body = CreatePart(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1f, 0f),
                new Vector3(0.9f, 1f, 0.9f), boss ? bossMaterial : enemyMaterial, false);
            GameObject visor = CreatePart(PrimitiveType.Cube, "Visor", root.transform, new Vector3(0f, 1.55f, 0.36f),
                new Vector3(0.6f, 0.14f, 0.2f), visorMaterial, true);
            GameObject gun = CreatePart(PrimitiveType.Cube, "Gun", root.transform, new Vector3(0.38f, 1.15f, 0.38f),
                new Vector3(0.1f, 0.12f, 0.6f), weaponDarkMaterial, false);

            // Prefer the polished Meshy character when it has been imported. The primitive body
            // remains a safe fallback so project generation never depends on an external asset.
            const string meshyVisualPath = "Assets/Art/Characters/EnemyGrunt/Generated/EnemyGruntVisual.prefab";
            GameObject meshyVisual = AssetDatabase.LoadAssetAtPath<GameObject>(meshyVisualPath);
            if (meshyVisual != null)
            {
                GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(meshyVisual);
                visualInstance.name = "EnemyGruntVisual";
                visualInstance.transform.SetParent(root.transform, false);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;
                visualInstance.transform.localScale = Vector3.one;
                body.SetActive(false);
                visor.SetActive(false);
                gun.SetActive(false);
            }

            Transform gunPoint = new GameObject("GunPoint").transform;
            gunPoint.SetParent(root.transform, false);
            gunPoint.localPosition = new Vector3(0.38f, 1.15f, 0.72f);
            GameObject flash = CreatePart(PrimitiveType.Sphere, "MuzzleFlash", gunPoint, Vector3.zero,
                Vector3.one * 0.18f, muzzleFlashMaterial, true);
            flash.SetActive(false);

            Transform eye = new GameObject("Eye").transform;
            eye.SetParent(root.transform, false);
            eye.localPosition = new Vector3(0f, 1.6f, 0.15f);

            Transform bar = new GameObject("HealthBar").transform;
            bar.SetParent(root.transform, false);
            bar.localPosition = new Vector3(0f, 2.35f, 0f);
            CreatePart(PrimitiveType.Cube, "Background", bar, Vector3.zero, new Vector3(1f, 0.14f, 0.02f), healthBarBackMaterial, true);
            GameObject fill = CreatePart(PrimitiveType.Cube, "Fill", bar, new Vector3(0f, 0f, -0.015f),
                new Vector3(0.96f, 0.1f, 0.02f), healthBarFillMaterial, true);
            EnemyHealthBar healthBar = bar.gameObject.AddComponent<EnemyHealthBar>();
            SetRef(healthBar, "fill", fill.transform);

            Enemy enemy = root.AddComponent<Enemy>();
            SetRef(enemy, "eye", eye);
            SetRef(enemy, "gunPoint", gunPoint);
            SetRef(enemy, "muzzleFlash", flash);
            SetRef(enemy, "healthBar", healthBar);
            SetRef(enemy, "bodyRenderer", body.GetComponent<MeshRenderer>());

            EnemyAnimatorBridge bridge = root.GetComponentInChildren<EnemyAnimatorBridge>();
            if (bridge != null) SetRef(enemy, "animationBridge", bridge);

            SetBool(enemy, "isBoss", boss);
            if (boss)
            {
                root.transform.localScale = Vector3.one * 1.7f;
                SetFloat(enemy, "maxHealth", 700f);
                SetFloat(enemy, "patrolSpeed", 1.8f);
                SetFloat(enemy, "chaseSpeed", 3.2f);
                SetFloat(enemy, "detectionRange", 26f);
                SetFloat(enemy, "attackRange", 18f);
                SetFloat(enemy, "fieldOfView", 180f);
                SetFloat(enemy, "loseSightTime", 8f);
                SetFloat(enemy, "fireInterval", 0.55f);
                SetFloat(enemy, "damage", 9f);
                SetFloat(enemy, "accuracy", 0.5f);
                SetFloat(enemy, "patrolRadius", 5f);
                SetInt(enemy, "scoreValue", 1000);
                SetFloat(enemy, "dropChance", 1f);
            }
            else
            {
                SetFloat(enemy, "maxHealth", 60f);
                SetFloat(enemy, "patrolSpeed", 2.2f);
                SetFloat(enemy, "chaseSpeed", 4f);
                SetFloat(enemy, "detectionRange", 18f);
                SetFloat(enemy, "attackRange", 13f);
                SetFloat(enemy, "fieldOfView", 140f);
                SetFloat(enemy, "loseSightTime", 5f);
                SetFloat(enemy, "fireInterval", 1.2f);
                SetFloat(enemy, "damage", 7f);
                SetFloat(enemy, "accuracy", 0.55f);
                SetFloat(enemy, "patrolRadius", 8f);
                SetInt(enemy, "scoreValue", 100);
                SetFloat(enemy, "dropChance", 0.5f);
            }

            return SavePrefab(root, PrefabsFolder + "/" + prefabName + ".prefab");
        }

        private static GameObject CreatePlayerPrefab()
        {
            var root = new GameObject("Player");

            var characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.4f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.4f;
            characterController.slopeLimit = 45f;
            characterController.skinWidth = 0.05f;
            characterController.minMoveDistance = 0f;

            root.AddComponent<PlayerInput>();
            PlayerController controller = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerHealth>();
            WeaponManager weaponManager = root.AddComponent<WeaponManager>();

            Transform pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot, false);
            Camera playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = SkyColor;
            playerCamera.nearClipPlane = 0.03f;
            playerCamera.farClipPlane = 200f;
            playerCamera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();

            Transform holder = new GameObject("WeaponHolder").transform;
            holder.SetParent(cameraObject.transform, false);
            holder.localPosition = new Vector3(0.2f, -0.2f, 0.25f);

            Weapon pistol = CreateWeaponView(holder, "Pistol", pistolData, false);
            Weapon rifle = CreateWeaponView(holder, "Rifle", rifleData, true);

            SetRef(controller, "cameraPivot", pivot);
            SetRefs(weaponManager, "weapons", pistol, rifle);
            SetRef(weaponManager, "playerCamera", playerCamera);

            return SavePrefab(root, PrefabsFolder + "/Player.prefab");
        }

        private static Weapon CreateWeaponView(Transform holder, string weaponName, WeaponData data, bool rifle)
        {
            var root = new GameObject(weaponName);
            root.transform.SetParent(holder, false);

            Transform model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);

            Vector3 muzzlePosition;
            if (rifle)
            {
                CreateViewPart("Body", model, new Vector3(0f, 0f, 0.02f), new Vector3(0.07f, 0.09f, 0.36f), weaponDarkMaterial);
                CreateViewPart("Barrel", model, new Vector3(0f, 0.015f, 0.26f), new Vector3(0.035f, 0.035f, 0.14f), weaponDarkMaterial);
                CreateViewPart("Magazine", model, new Vector3(0f, -0.08f, 0.06f), new Vector3(0.05f, 0.12f, 0.06f), weaponAccentMaterial);
                CreateViewPart("Stock", model, new Vector3(0f, -0.02f, -0.2f), new Vector3(0.06f, 0.08f, 0.1f), weaponAccentMaterial);
                CreateViewPart("Grip", model, new Vector3(0f, -0.07f, -0.08f), new Vector3(0.05f, 0.1f, 0.05f), weaponDarkMaterial);
                CreateViewPart("Sight", model, new Vector3(0f, 0.06f, 0.05f), new Vector3(0.02f, 0.03f, 0.08f), weaponAccentMaterial);
                muzzlePosition = new Vector3(0f, 0.015f, 0.34f);
            }
            else
            {
                CreateViewPart("Slide", model, new Vector3(0f, 0.02f, 0.03f), new Vector3(0.052f, 0.05f, 0.2f), weaponAccentMaterial);
                CreateViewPart("Frame", model, new Vector3(0f, -0.015f, 0.02f), new Vector3(0.048f, 0.03f, 0.17f), weaponDarkMaterial);
                CreateViewPart("Grip", model, new Vector3(0f, -0.08f, -0.04f), new Vector3(0.045f, 0.12f, 0.06f), weaponDarkMaterial);
                muzzlePosition = new Vector3(0f, 0.02f, 0.14f);
            }

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(model, false);
            muzzle.localPosition = muzzlePosition;

            var lightObject = new GameObject("MuzzleLight");
            lightObject.transform.SetParent(muzzle, false);
            Light muzzleLight = lightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 3f;
            muzzleLight.color = new Color(1f, 0.8f, 0.45f);
            muzzleLight.shadows = LightShadows.None;
            muzzleLight.enabled = false;

            GameObject flash = CreatePart(PrimitiveType.Sphere, "MuzzleFlash", muzzle, Vector3.zero,
                Vector3.one * (rifle ? 0.09f : 0.07f), muzzleFlashMaterial, true);
            MeshRenderer flashRenderer = flash.GetComponent<MeshRenderer>();
            flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
            flash.SetActive(false);

            Weapon weapon = root.AddComponent<Weapon>();
            SetRef(weapon, "data", data);
            SetRef(weapon, "muzzle", muzzle);
            SetRef(weapon, "muzzleLight", muzzleLight);
            SetRef(weapon, "muzzleFlash", flash);
            SetRef(weapon, "model", model);
            return weapon;
        }

        private static void CreateViewPart(string partName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = CreatePart(PrimitiveType.Cube, partName, parent, position, scale, material, true);
            MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
            partRenderer.shadowCastingMode = ShadowCastingMode.Off;
            partRenderer.receiveShadows = false;
        }

        private static void CreateManagersPrefab()
        {
            var root = new GameObject("Managers");
            root.AddComponent<SaveManager>();
            root.AddComponent<GameManager>();
            root.AddComponent<AudioManager>();
            SceneLoader loader = root.AddComponent<SceneLoader>();
            FPSCounter fpsCounter = root.AddComponent<FPSCounter>();

            Canvas canvas = UIFactory.CreateCanvas("SystemCanvas", root.transform, 100);

            // Loading screen (full screen; blocks input while loading)
            Image loadingRoot = UIFactory.CreatePanel("LoadingScreen", canvas.transform, BackgroundColor);
            Text loadingTitle = UIFactory.CreateText("Title", loadingRoot.transform, "LOADING", 72,
                TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(loadingTitle.rectTransform, new Vector2(0f, 90f), new Vector2(900f, 120f));

            Image barBack = UIFactory.CreateImage("ProgressBack", loadingRoot.transform, new Color(0.18f, 0.2f, 0.26f, 1f));
            UIFactory.PlaceCenter(barBack.rectTransform, new Vector2(0f, -20f), new Vector2(900f, 34f));
            Image barFill = UIFactory.CreateImage("ProgressFill", barBack.transform, AccentColor);
            UIFactory.SetAnchors(barFill.rectTransform, Vector2.zero, new Vector2(0f, 1f));

            Text percent = UIFactory.CreateText("Percent", loadingRoot.transform, "0%", 40,
                TextAnchor.MiddleCenter, MutedTextColor);
            UIFactory.PlaceCenter(percent.rectTransform, new Vector2(0f, -90f), new Vector2(400f, 60f));
            loadingRoot.gameObject.SetActive(false);

            // FPS counter (top-right, inside the safe area)
            RectTransform safeArea = UIFactory.CreateSafeArea(canvas.transform);
            Text fpsText = UIFactory.CreateText("FPSText", safeArea, "FPS 60", 30, TextAnchor.UpperRight, Color.green, FontStyle.Bold);
            UIFactory.Place(fpsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-180f, -24f), new Vector2(240f, 50f));
            fpsText.enabled = false;

            SetRef(loader, "loadingRoot", loadingRoot.gameObject);
            SetRef(loader, "progressFill", barFill.rectTransform);
            SetRef(loader, "progressText", percent);
            SetRef(fpsCounter, "label", fpsText);

            SavePrefab(root, ResourcesFolder + "/" + SceneBootstrap.ManagersResourcePath + ".prefab");
        }

        private static void CreateGameAssets()
        {
            GameAssets assets = LoadOrCreateAsset<GameAssets>(ResourcesFolder + "/" + GameAssets.ResourcePath + ".asset");
            assets.playerPrefab = playerPrefab;
            assets.enemyPrefab = enemyPrefab;
            assets.bossPrefab = bossPrefab;
            assets.impactEffectPrefab = impactPrefab;
            assets.bloodEffectPrefab = bloodPrefab;
            assets.healthPickupPrefab = healthPickupPrefab;
            assets.ammoPickupPrefab = ammoPickupPrefab;
            assets.floorMaterial = floorMaterial;
            assets.wallMaterial = wallMaterial;
            assets.obstacleMaterial = obstacleMaterial;
            EditorUtility.SetDirty(assets);
        }

        // =====================================================================
        // Scenes
        // =====================================================================

        private static Scene NewScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.55f, 0.6f);
            RenderSettings.fog = false;

            new GameObject("SceneBootstrap").AddComponent<SceneBootstrap>();
            return scene;
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new System.Exception("Failed to save scene " + path);
        }

        private static Camera CreateMenuCamera(Color background)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera menuCamera = go.AddComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = background;
            menuCamera.cullingMask = 0;
            go.AddComponent<AudioListener>();
            return menuCamera;
        }

        private static void CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            Light sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Hard;
            sun.shadowStrength = 0.7f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = sun;
        }

        private static void CreateBootScene()
        {
            Scene scene = NewScene();
            CreateMenuCamera(Color.black);
            new GameObject("BootController").AddComponent<BootController>();

            Canvas canvas = UIFactory.CreateCanvas("BootCanvas", null, 0);
            UIFactory.CreatePanel("Background", canvas.transform, BackgroundColor);
            Text title = UIFactory.CreateText("Title", canvas.transform, "FPS ARENA", 120, TextAnchor.MiddleCenter,
                TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 40f), new Vector2(1400f, 180f));
            Text subtitle = UIFactory.CreateText("Subtitle", canvas.transform, "Initializing...", 40,
                TextAnchor.MiddleCenter, MutedTextColor);
            UIFactory.PlaceCenter(subtitle.rectTransform, new Vector2(0f, -80f), new Vector2(1000f, 60f));

            SaveScene(scene, ScenePaths[0]);
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = NewScene();
            CreateMenuCamera(BackgroundColor);
            UIFactory.CreateEventSystem();

            Canvas canvas = UIFactory.CreateCanvas("MenuCanvas", null, 0);
            MainMenuUI menu = canvas.gameObject.AddComponent<MainMenuUI>();
            UIFactory.CreatePanel("Background", canvas.transform, BackgroundColor);

            Image stripe = UIFactory.CreateImage("AccentStripe", canvas.transform, new Color(0.18f, 0.42f, 0.85f, 0.25f));
            UIFactory.SetAnchors(stripe.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.64f));

            RectTransform safe = UIFactory.CreateSafeArea(canvas.transform);

            Text title = UIFactory.CreateText("Title", safe, "FPS ARENA", 130, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1400f, 170f));
            Text subtitle = UIFactory.CreateText("Subtitle", safe, "A mobile shooter built entirely from code", 38,
                TextAnchor.MiddleCenter, MutedTextColor);
            UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1400f, 60f));

            Button play = UIFactory.CreateButton("PlayButton", safe, "PLAY", 52, PrimaryButtonColor, out Text playLabel);
            UIFactory.PlaceCenter((RectTransform)play.transform, new Vector2(0f, 20f), new Vector2(640f, 120f));
            Button levelSelect = UIFactory.CreateButton("LevelSelectButton", safe, "LEVEL SELECT", 44, SecondaryButtonColor, out _);
            UIFactory.PlaceCenter((RectTransform)levelSelect.transform, new Vector2(0f, -120f), new Vector2(640f, 110f));
            Button settings = UIFactory.CreateButton("SettingsButton", safe, "SETTINGS", 44, SecondaryButtonColor, out _);
            UIFactory.PlaceCenter((RectTransform)settings.transform, new Vector2(0f, -250f), new Vector2(640f, 110f));
            Button quit = UIFactory.CreateButton("QuitButton", safe, "QUIT", 44, DangerButtonColor, out _);
            UIFactory.PlaceCenter((RectTransform)quit.transform, new Vector2(0f, -380f), new Vector2(640f, 110f));

            Image exitHint = UIFactory.CreateImage("ExitHint", safe, new Color(0f, 0f, 0f, 0.8f));
            UIFactory.Place(exitHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(760f, 80f));
            Text exitText = UIFactory.CreateText("Text", exitHint.transform, "Press BACK again to quit", 36,
                TextAnchor.MiddleCenter, TextColor);
            UIFactory.Stretch(exitText.rectTransform);
            exitHint.gameObject.SetActive(false);

            SettingsPanel settingsPanel = BuildSettingsPanel(canvas.transform);

            SetRef(menu, "playButton", play);
            SetRef(menu, "playLabel", playLabel);
            SetRef(menu, "levelSelectButton", levelSelect);
            SetRef(menu, "settingsButton", settings);
            SetRef(menu, "quitButton", quit);
            SetRef(menu, "settingsPanel", settingsPanel);
            SetRef(menu, "exitHint", exitHint.gameObject);

            SaveScene(scene, ScenePaths[1]);
        }

        private static void CreateLevelSelectScene()
        {
            Scene scene = NewScene();
            CreateMenuCamera(BackgroundColor);
            UIFactory.CreateEventSystem();

            Canvas canvas = UIFactory.CreateCanvas("LevelSelectCanvas", null, 0);
            LevelSelectUI levelSelect = canvas.gameObject.AddComponent<LevelSelectUI>();
            UIFactory.CreatePanel("Background", canvas.transform, BackgroundColor);
            RectTransform safe = UIFactory.CreateSafeArea(canvas.transform);

            Text title = UIFactory.CreateText("Title", safe, "SELECT LEVEL", 96, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 140f));

            int count = LevelDatabase.Count;
            var buttons = new Button[count];
            var infoTexts = new Text[count];
            var stars = new Image[count * 3];
            var locks = new GameObject[count];
            float spacing = 560f;
            float startX = -spacing * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                LevelDefinition definition = LevelDatabase.Get(i);
                Button card = UIFactory.CreateButton("Level" + (i + 1) + "Card", safe, "LEVEL " + (i + 1), 60, PanelColor, out Text cardTitle);
                UIFactory.PlaceCenter((RectTransform)card.transform, new Vector2(startX + spacing * i, -30f), new Vector2(480f, 580f));
                UIFactory.Place(cardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(440f, 80f));

                Text nameText = UIFactory.CreateText("Name", card.transform, definition.displayName.ToUpperInvariant(), 36,
                    TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold);
                UIFactory.Place(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(440f, 50f));

                string enemyInfo = definition.EnemyCount - definition.BossCount + " ENEMIES" +
                                   (definition.BossCount > 0 ? " + BOSS" : string.Empty);
                Text enemiesText = UIFactory.CreateText("Enemies", card.transform, enemyInfo, 30,
                    TextAnchor.MiddleCenter, MutedTextColor);
                UIFactory.Place(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(440f, 44f));

                for (int s = 0; s < 3; s++)
                {
                    Image star = UIFactory.CreateImage("Star" + (s + 1), card.transform, StarOffColor, circleSprite);
                    UIFactory.PlaceCenter(star.rectTransform, new Vector2((s - 1) * 110f, -10f), new Vector2(84f, 84f));
                    stars[i * 3 + s] = star;
                }

                Text info = UIFactory.CreateText("Info", card.transform, "NOT PLAYED", 40, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
                UIFactory.Place(info.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(440f, 60f));

                Image lockOverlay = UIFactory.CreateImage("LockOverlay", card.transform, new Color(0f, 0f, 0f, 0.65f));
                UIFactory.Stretch(lockOverlay.rectTransform);
                Image lockBody = UIFactory.CreateImage("LockBody", lockOverlay.transform, MutedTextColor);
                UIFactory.PlaceCenter(lockBody.rectTransform, new Vector2(0f, -30f), new Vector2(110f, 90f));
                Image lockShackle = UIFactory.CreateImage("LockShackle", lockOverlay.transform, MutedTextColor, circleSprite);
                UIFactory.PlaceCenter(lockShackle.rectTransform, new Vector2(0f, 30f), new Vector2(90f, 90f));
                Image lockHole = UIFactory.CreateImage("LockHole", lockShackle.transform, new Color(0.1f, 0.1f, 0.12f, 1f), circleSprite);
                UIFactory.PlaceCenter(lockHole.rectTransform, Vector2.zero, new Vector2(50f, 50f));
                lockBody.transform.SetAsLastSibling();

                buttons[i] = card;
                infoTexts[i] = info;
                locks[i] = lockOverlay.gameObject;
            }

            Button back = UIFactory.CreateButton("BackButton", safe, "BACK", 44, SecondaryButtonColor, out _);
            UIFactory.Place((RectTransform)back.transform, Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(320f, 110f));

            SetRefs(levelSelect, "levelButtons", buttons);
            SetRefs(levelSelect, "infoTexts", infoTexts);
            SetRefs(levelSelect, "starImages", stars);
            SetRefs(levelSelect, "lockOverlays", locks);
            SetRef(levelSelect, "backButton", back);
            SetColor(levelSelect, "starOnColor", StarOnColor);
            SetColor(levelSelect, "starOffColor", StarOffColor);

            SaveScene(scene, ScenePaths[2]);
        }

        private static void CreateLevelScene(int levelIndex)
        {
            Scene scene = NewScene();
            CreateDirectionalLight();
            UIFactory.CreateEventSystem();

            var levelObject = new GameObject("Level");
            LevelBuilder builder = levelObject.AddComponent<LevelBuilder>();
            NavMeshSurface surface = levelObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Transform enemiesRoot = new GameObject("Enemies").transform;

            var managerObject = new GameObject("LevelManager");
            LevelManager levelManager = managerObject.AddComponent<LevelManager>();
            PoolManager poolManager = managerObject.AddComponent<PoolManager>();

            SetInt(levelManager, "levelIndex", levelIndex);
            SetRef(levelManager, "levelBuilder", builder);
            SetRef(levelManager, "navMeshSurface", surface);
            SetRef(levelManager, "enemiesRoot", enemiesRoot);
            SetRef(levelManager, "poolManager", poolManager);

            BuildGameUI();

            SaveScene(scene, ScenePaths[3 + levelIndex]);
        }

        // =====================================================================
        // In-game UI
        // =====================================================================

        private static void BuildGameUI()
        {
            Canvas canvas = UIFactory.CreateCanvas("GameCanvas", null, 10);
            HUDController hud = canvas.gameObject.AddComponent<HUDController>();
            InGameUI inGame = canvas.gameObject.AddComponent<InGameUI>();

            // Damage flash: full screen, outside the safe area, drawn behind everything else.
            Image damageFlash = UIFactory.CreateImage("DamageFlash", canvas.transform, new Color(0.85f, 0f, 0f, 0f));
            UIFactory.Stretch(damageFlash.rectTransform);

            RectTransform hudRoot = UIFactory.CreateRect("HUD", canvas.transform);
            UIFactory.Stretch(hudRoot);
            RectTransform safe = UIFactory.CreateSafeArea(hudRoot);

            // First child of the safe area, so the HUD elements draw on top of it.
            BuildMobileControls(safe);

            // Crosshair
            RectTransform crosshair = UIFactory.CreateRect("Crosshair", safe);
            UIFactory.PlaceCenter(crosshair, Vector2.zero, new Vector2(60f, 60f));
            CreateCrosshairBar(crosshair, new Vector2(0f, 15f), new Vector2(4f, 16f));
            CreateCrosshairBar(crosshair, new Vector2(0f, -15f), new Vector2(4f, 16f));
            CreateCrosshairBar(crosshair, new Vector2(-15f, 0f), new Vector2(16f, 4f));
            CreateCrosshairBar(crosshair, new Vector2(15f, 0f), new Vector2(16f, 4f));
            CreateCrosshairBar(crosshair, Vector2.zero, new Vector2(4f, 4f));

            RectTransform hitMarker = UIFactory.CreateRect("HitMarker", safe);
            UIFactory.PlaceCenter(hitMarker, Vector2.zero, new Vector2(60f, 60f));
            CanvasGroup hitGroup = hitMarker.gameObject.AddComponent<CanvasGroup>();
            hitGroup.alpha = 0f;
            hitGroup.interactable = false;
            hitGroup.blocksRaycasts = false;
            for (int i = 0; i < 4; i++)
            {
                Image tick = UIFactory.CreateImage("Tick" + i, hitMarker, new Color(1f, 0.25f, 0.2f, 1f));
                float angle = 45f + 90f * i;
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                UIFactory.PlaceCenter(tick.rectTransform, direction * 20f, new Vector2(16f, 4f));
                tick.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            Text reloadText = UIFactory.CreateText("ReloadIndicator", safe, "RELOADING...", 40, TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold);
            UIFactory.PlaceCenter(reloadText.rectTransform, new Vector2(0f, -90f), new Vector2(500f, 60f));
            reloadText.gameObject.SetActive(false);

            // Health (top-left)
            Text hpLabel = UIFactory.CreateText("HealthLabel", safe, "HP", 40, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            UIFactory.Place(hpLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(80f, 50f));
            Image healthBack = UIFactory.CreateImage("HealthBack", safe, new Color(0f, 0f, 0f, 0.55f));
            UIFactory.Place(healthBack.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -32f), new Vector2(420f, 46f));
            Image healthFill = UIFactory.CreateImage("HealthFill", healthBack.transform, new Color(0.3f, 0.85f, 0.35f, 1f));
            UIFactory.SetAnchors(healthFill.rectTransform, Vector2.zero, Vector2.one);
            Text healthText = UIFactory.CreateText("HealthValue", healthBack.transform, "100", 34, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            UIFactory.Stretch(healthText.rectTransform);

            // Score (top-left, under health)
            Text scoreLabel = UIFactory.CreateText("ScoreLabel", safe, "SCORE", 32, TextAnchor.MiddleLeft, MutedTextColor, FontStyle.Bold);
            UIFactory.Place(scoreLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -100f), new Vector2(140f, 44f));
            Text scoreText = UIFactory.CreateText("ScoreValue", safe, "0", 40, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            UIFactory.Place(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(180f, -98f), new Vector2(300f, 50f));

            // Enemies and time (top-center)
            Text enemiesLabel = UIFactory.CreateText("EnemiesLabel", safe, "ENEMIES LEFT", 30, TextAnchor.MiddleCenter, MutedTextColor, FontStyle.Bold);
            UIFactory.Place(enemiesLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 40f));
            Text enemiesText = UIFactory.CreateText("EnemiesValue", safe, "0", 60, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.Place(enemiesText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(300f, 74f));
            Text timeText = UIFactory.CreateText("Time", safe, "00:00", 34, TextAnchor.MiddleCenter, TextColor);
            UIFactory.Place(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(300f, 48f));

            // Ammo (bottom-center)
            Text ammoText = UIFactory.CreateText("AmmoValue", safe, "0", 84, TextAnchor.LowerRight, Color.white, FontStyle.Bold);
            UIFactory.Place(ammoText.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 24f), new Vector2(220f, 100f));
            Text slash = UIFactory.CreateText("AmmoSlash", safe, "/", 52, TextAnchor.LowerCenter, MutedTextColor, FontStyle.Bold);
            UIFactory.Place(slash.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(36f, 70f));
            Text reserveText = UIFactory.CreateText("ReserveValue", safe, "0", 52, TextAnchor.LowerLeft, MutedTextColor, FontStyle.Bold);
            UIFactory.Place(reserveText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(18f, 32f), new Vector2(220f, 70f));
            Text weaponName = UIFactory.CreateText("WeaponName", safe, "PISTOL", 32, TextAnchor.LowerCenter, AccentColor, FontStyle.Bold);
            UIFactory.Place(weaponName.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(400f, 46f));

            // Pause button (top-right)
            Button pauseButton = UIFactory.CreateButton("PauseButton", safe, "II", 50, new Color(0f, 0f, 0f, 0.5f), out _);
            UIFactory.Place((RectTransform)pauseButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(120f, 120f));

            // Popups (later siblings draw on top)
            GameObject pausePanel = BuildPausePanel(canvas.transform, out Button resume, out Button restart,
                out Button pauseSettings, out Button pauseMenu);
            GameObject winPanel = BuildWinPanel(canvas.transform, out Text winScore, out Text winDetail, out Text winBest,
                out Image[] winStars, out Button nextLevel, out Button winMenu);
            GameObject losePanel = BuildLosePanel(canvas.transform, out Button loseRetry, out Button loseMenu);
            SettingsPanel settingsPanel = BuildSettingsPanel(canvas.transform);

            SetRef(hud, "healthFill", healthFill.rectTransform);
            SetRef(hud, "healthText", healthText);
            SetRef(hud, "ammoText", ammoText);
            SetRef(hud, "reserveText", reserveText);
            SetRef(hud, "weaponNameText", weaponName);
            SetRef(hud, "enemiesText", enemiesText);
            SetRef(hud, "scoreText", scoreText);
            SetRef(hud, "timeText", timeText);
            SetRef(hud, "reloadIndicator", reloadText.gameObject);
            SetRef(hud, "damageFlash", damageFlash);
            SetRef(hud, "hitMarker", hitGroup);
            SetRef(hud, "pauseButton", pauseButton);

            SetRef(inGame, "hudRoot", hudRoot.gameObject);
            SetRef(inGame, "pausePanel", pausePanel);
            SetRef(inGame, "winPanel", winPanel);
            SetRef(inGame, "losePanel", losePanel);
            SetRef(inGame, "settingsPanel", settingsPanel);
            SetRef(inGame, "resumeButton", resume);
            SetRef(inGame, "restartButton", restart);
            SetRef(inGame, "pauseSettingsButton", pauseSettings);
            SetRef(inGame, "pauseMenuButton", pauseMenu);
            SetRef(inGame, "winScoreText", winScore);
            SetRef(inGame, "winDetailText", winDetail);
            SetRef(inGame, "winBestText", winBest);
            SetRefs(inGame, "winStars", winStars);
            SetRef(inGame, "nextLevelButton", nextLevel);
            SetRef(inGame, "winMenuButton", winMenu);
            SetRef(inGame, "loseRetryButton", loseRetry);
            SetRef(inGame, "loseMenuButton", loseMenu);
            SetColor(inGame, "starOnColor", StarOnColor);
            SetColor(inGame, "starOffColor", StarOffColor);
        }

        private static void CreateCrosshairBar(Transform parent, Vector2 position, Vector2 size)
        {
            Image bar = UIFactory.CreateImage("Bar", parent, new Color(1f, 1f, 1f, 0.9f));
            UIFactory.PlaceCenter(bar.rectTransform, position, size);
        }

        private static MobileControls BuildMobileControls(Transform parent)
        {
            RectTransform container = UIFactory.CreateRect("MobileControls", parent);
            UIFactory.Stretch(container);
            MobileControls controls = container.gameObject.AddComponent<MobileControls>();

            RectTransform root = UIFactory.CreateRect("Controls", container);
            UIFactory.Stretch(root);

            // Joystick zone: the lower-left part of the screen. The joystick follows the touch.
            RectTransform zone = UIFactory.CreateRect("JoystickZone", root);
            UIFactory.SetAnchors(zone, Vector2.zero, new Vector2(0.42f, 0.75f));
            TouchZone touchZone = zone.gameObject.AddComponent<TouchZone>();
            touchZone.color = new Color(1f, 1f, 1f, 0f);
            touchZone.raycastTarget = true;

            Image joystickBack = UIFactory.CreateImage("JoystickBackground", zone, new Color(1f, 1f, 1f, 0.18f), circleSprite);
            UIFactory.PlaceCenter(joystickBack.rectTransform, Vector2.zero, new Vector2(300f, 300f));
            Image joystickHandle = UIFactory.CreateImage("JoystickHandle", joystickBack.transform, new Color(1f, 1f, 1f, 0.55f), circleSprite);
            UIFactory.PlaceCenter(joystickHandle.rectTransform, Vector2.zero, new Vector2(130f, 130f));

            VirtualJoystick joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            SetRef(joystick, "background", joystickBack.rectTransform);
            SetRef(joystick, "handle", joystickHandle.rectTransform);

            // Action buttons (bottom-right)
            Vector2 bottomRight = new Vector2(1f, 0f);
            TouchButton fire = CreateTouchButton("FireButton", root, "FIRE", 44, bottomRight, new Vector2(-245f, 245f), 205f,
                new Color(1f, 0.3f, 0.25f, 0.4f), true);
            TouchButton reload = CreateTouchButton("ReloadButton", root, "RELOAD", 28, bottomRight, new Vector2(-445f, 165f), 132f,
                new Color(1f, 1f, 1f, 0.28f), false);
            TouchButton jump = CreateTouchButton("JumpButton", root, "JUMP", 30, bottomRight, new Vector2(-135f, 440f), 132f,
                new Color(1f, 1f, 1f, 0.28f), false);
            TouchButton swap = CreateTouchButton("SwitchButton", root, "SWAP", 30, bottomRight, new Vector2(-425f, 405f), 132f,
                new Color(1f, 1f, 1f, 0.28f), false);

            SetRef(controls, "controlsRoot", root.gameObject);
            SetRef(controls, "moveJoystick", joystick);
            SetRef(controls, "fireButton", fire);
            SetRef(controls, "reloadButton", reload);
            SetRef(controls, "jumpButton", jump);
            SetRef(controls, "switchButton", swap);
            return controls;
        }

        private static TouchButton CreateTouchButton(string buttonName, Transform parent, string label, int fontSize,
            Vector2 anchor, Vector2 position, float size, Color color, bool allowDragLook)
        {
            Image image = UIFactory.CreateImage(buttonName, parent, color, circleSprite, true);
            UIFactory.Place(image.rectTransform, anchor, new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

            Text text = UIFactory.CreateText("Label", image.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            UIFactory.Stretch(text.rectTransform);

            TouchButton button = image.gameObject.AddComponent<TouchButton>();
            Color pressed = color;
            pressed.a = Mathf.Min(1f, color.a + 0.35f);
            SetRef(button, "targetImage", image);
            SetColor(button, "normalColor", color);
            SetColor(button, "pressedColor", pressed);
            SetBool(button, "allowDragLook", allowDragLook);
            return button;
        }

        private static Image CreateDialog(string panelName, Transform canvas, Vector2 boxSize, out RectTransform box)
        {
            Image dim = UIFactory.CreatePanel(panelName, canvas, DimColor);
            Image boxImage = UIFactory.CreateImage("Box", dim.transform, PanelColor, null, true);
            UIFactory.PlaceCenter(boxImage.rectTransform, Vector2.zero, boxSize);
            box = boxImage.rectTransform;
            return dim;
        }

        private static Button CreateDialogButton(string buttonName, Transform parent, string label, Color color, Vector2 position, Vector2 size)
        {
            Button button = UIFactory.CreateButton(buttonName, parent, label, 42, color, out _);
            UIFactory.PlaceCenter((RectTransform)button.transform, position, size);
            return button;
        }

        private static GameObject BuildPausePanel(Transform canvas, out Button resume, out Button restart,
            out Button settings, out Button menu)
        {
            Image root = CreateDialog("PausePanel", canvas, new Vector2(720f, 780f), out RectTransform box);

            Text title = UIFactory.CreateText("Title", box, "PAUSED", 76, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 290f), new Vector2(600f, 110f));

            Vector2 size = new Vector2(540f, 110f);
            resume = CreateDialogButton("ResumeButton", box, "RESUME", PrimaryButtonColor, new Vector2(0f, 140f), size);
            restart = CreateDialogButton("RestartButton", box, "RESTART", SecondaryButtonColor, new Vector2(0f, 10f), size);
            settings = CreateDialogButton("SettingsButton", box, "SETTINGS", SecondaryButtonColor, new Vector2(0f, -120f), size);
            menu = CreateDialogButton("MainMenuButton", box, "MAIN MENU", DangerButtonColor, new Vector2(0f, -250f), size);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject BuildWinPanel(Transform canvas, out Text score, out Text detail, out Text best,
            out Image[] stars, out Button nextLevel, out Button menu)
        {
            Image root = CreateDialog("WinPanel", canvas, new Vector2(1000f, 860f), out RectTransform box);

            Text title = UIFactory.CreateText("Title", box, "LEVEL COMPLETE", 76, TextAnchor.MiddleCenter, SuccessColor, FontStyle.Bold);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 330f), new Vector2(900f, 110f));

            stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                Image star = UIFactory.CreateImage("Star" + (i + 1), box, StarOffColor, circleSprite);
                UIFactory.PlaceCenter(star.rectTransform, new Vector2((i - 1) * 160f, 190f), new Vector2(120f, 120f));
                stars[i] = star;
            }

            score = UIFactory.CreateText("Score", box, "SCORE  0", 62, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(score.rectTransform, new Vector2(0f, 60f), new Vector2(900f, 80f));
            detail = UIFactory.CreateText("Detail", box, "TIME 00:00", 34, TextAnchor.MiddleCenter, MutedTextColor);
            UIFactory.PlaceCenter(detail.rectTransform, new Vector2(0f, -15f), new Vector2(940f, 50f));
            best = UIFactory.CreateText("Best", box, "BEST  0", 40, TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold);
            UIFactory.PlaceCenter(best.rectTransform, new Vector2(0f, -80f), new Vector2(900f, 56f));

            Vector2 size = new Vector2(560f, 105f);
            nextLevel = CreateDialogButton("NextLevelButton", box, "NEXT LEVEL", PrimaryButtonColor, new Vector2(0f, -200f), size);
            menu = CreateDialogButton("MenuButton", box, "MAIN MENU", SecondaryButtonColor, new Vector2(0f, -330f), size);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static GameObject BuildLosePanel(Transform canvas, out Button retry, out Button menu)
        {
            Image root = CreateDialog("LosePanel", canvas, new Vector2(780f, 620f), out RectTransform box);

            Text title = UIFactory.CreateText("Title", box, "YOU DIED", 84, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f), FontStyle.Bold);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 200f), new Vector2(700f, 120f));
            Text subtitle = UIFactory.CreateText("Subtitle", box, "The enemies got you. Try again!", 36, TextAnchor.MiddleCenter, MutedTextColor);
            UIFactory.PlaceCenter(subtitle.rectTransform, new Vector2(0f, 100f), new Vector2(720f, 56f));

            Vector2 size = new Vector2(540f, 110f);
            retry = CreateDialogButton("RetryButton", box, "RETRY", PrimaryButtonColor, new Vector2(0f, -40f), size);
            menu = CreateDialogButton("MenuButton", box, "MAIN MENU", SecondaryButtonColor, new Vector2(0f, -180f), size);

            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static SettingsPanel BuildSettingsPanel(Transform canvas)
        {
            Image root = CreateDialog("SettingsPanel", canvas, new Vector2(1120f, 860f), out RectTransform box);
            SettingsPanel panel = root.gameObject.AddComponent<SettingsPanel>();

            Text title = UIFactory.CreateText("Title", box, "SETTINGS", 72, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(title.rectTransform, new Vector2(0f, 340f), new Vector2(800f, 100f));

            const float labelX = -330f;
            const float controlX = 130f;
            const float valueX = 460f;

            // Sensitivity
            CreateRowLabel(box, "SensitivityLabel", "CAMERA SENSITIVITY", new Vector2(labelX, 200f));
            Slider sensitivity = UIFactory.CreateSlider("SensitivitySlider", box, SaveManager.MinSensitivity,
                SaveManager.MaxSensitivity, PrimaryButtonColor, circleSprite);
            UIFactory.PlaceCenter((RectTransform)sensitivity.transform, new Vector2(controlX, 200f), new Vector2(480f, 60f));
            sensitivity.value = 1f;
            Text sensitivityValue = UIFactory.CreateText("SensitivityValue", box, "1.0", 38, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(sensitivityValue.rectTransform, new Vector2(valueX, 200f), new Vector2(130f, 60f));

            // Volume
            CreateRowLabel(box, "VolumeLabel", "VOLUME", new Vector2(labelX, 70f));
            Slider volume = UIFactory.CreateSlider("VolumeSlider", box, 0f, 1f, PrimaryButtonColor, circleSprite);
            UIFactory.PlaceCenter((RectTransform)volume.transform, new Vector2(controlX, 70f), new Vector2(480f, 60f));
            volume.value = 0.8f;
            Text volumeValue = UIFactory.CreateText("VolumeValue", box, "80%", 38, TextAnchor.MiddleCenter, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(volumeValue.rectTransform, new Vector2(valueX, 70f), new Vector2(130f, 60f));

            // Quality
            CreateRowLabel(box, "QualityLabel", "GRAPHICS QUALITY", new Vector2(labelX, -60f));
            string[] qualityNames = { "LOW", "MEDIUM", "HIGH" };
            var qualityButtons = new Button[qualityNames.Length];
            for (int i = 0; i < qualityNames.Length; i++)
            {
                Button button = UIFactory.CreateButton("Quality" + qualityNames[i], box, qualityNames[i], 32, SecondaryButtonColor, out _);
                UIFactory.PlaceCenter((RectTransform)button.transform, new Vector2(controlX + 30f + (i - 1) * 205f, -60f), new Vector2(190f, 84f));
                qualityButtons[i] = button;
            }

            // FPS counter
            CreateRowLabel(box, "FpsLabel", "SHOW FPS COUNTER", new Vector2(labelX, -190f));
            Button fpsToggle = UIFactory.CreateButton("FpsToggle", box, "OFF", 36, SecondaryButtonColor, out Text fpsLabel);
            UIFactory.PlaceCenter((RectTransform)fpsToggle.transform, new Vector2(controlX + 30f - 205f, -190f), new Vector2(190f, 84f));

            Button close = UIFactory.CreateButton("CloseButton", box, "CLOSE", 44, PrimaryButtonColor, out _);
            UIFactory.PlaceCenter((RectTransform)close.transform, new Vector2(0f, -330f), new Vector2(420f, 110f));

            SetRef(panel, "sensitivitySlider", sensitivity);
            SetRef(panel, "sensitivityValueText", sensitivityValue);
            SetRef(panel, "volumeSlider", volume);
            SetRef(panel, "volumeValueText", volumeValue);
            SetRefs(panel, "qualityButtons", qualityButtons);
            SetRef(panel, "fpsToggleButton", fpsToggle);
            SetRef(panel, "fpsToggleLabel", fpsLabel);
            SetRef(panel, "closeButton", close);
            SetColor(panel, "selectedColor", PrimaryButtonColor);
            SetColor(panel, "normalColor", SecondaryButtonColor);

            root.gameObject.SetActive(false);
            return panel;
        }

        private static void CreateRowLabel(Transform parent, string objectName, string content, Vector2 position)
        {
            Text label = UIFactory.CreateText(objectName, parent, content, 36, TextAnchor.MiddleLeft, TextColor, FontStyle.Bold);
            UIFactory.PlaceCenter(label.rectTransform, position, new Vector2(400f, 60f));
        }

        // =====================================================================
        // Build settings
        // =====================================================================

        private static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[ScenePaths.Length];
            for (int i = 0; i < ScenePaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePaths[i]) == null)
                {
                    throw new System.Exception("Scene missing: " + ScenePaths[i]);
                }
                scenes[i] = new EditorBuildSettingsScene(ScenePaths[i], true);
            }
            EditorBuildSettings.scenes = scenes;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static GameObject CreatePart(PrimitiveType type, string partName, Transform parent, Vector3 localPosition,
            Vector3 localScale, Material material, bool removeShadows)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider != null) Object.DestroyImmediate(partCollider);

            MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
            partRenderer.sharedMaterial = material;
            if (removeShadows)
            {
                partRenderer.shadowCastingMode = ShadowCastingMode.Off;
                partRenderer.receiveShadows = false;
            }
            return part;
        }

        private static GameObject SavePrefab(GameObject instance, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
            Object.DestroyImmediate(instance);
            if (!success || prefab == null) throw new System.Exception("Failed to save prefab " + path);
            return prefab;
        }

        private static SerializedProperty FindProperty(SerializedObject serializedObject, string field)
        {
            SerializedProperty property = serializedObject.FindProperty(field);
            if (property == null)
            {
                throw new System.Exception("Serialized field '" + field + "' not found on " +
                                           serializedObject.targetObject.GetType().Name);
            }
            return property;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (value == null)
            {
                throw new System.Exception("Tried to assign null to " + target.GetType().Name + "." + field);
            }

            var serializedObject = new SerializedObject(target);
            SerializedProperty property = FindProperty(serializedObject, field);
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            serializedObject.Update();
            if (FindProperty(serializedObject, field).objectReferenceValue != value)
            {
                throw new System.Exception("Type mismatch assigning " + value.GetType().Name + " to " +
                                           target.GetType().Name + "." + field);
            }
        }

        private static void SetRefs(Object target, string field, params Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = FindProperty(serializedObject, field);
            if (!property.isArray) throw new System.Exception(target.GetType().Name + "." + field + " is not an array.");

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    throw new System.Exception("Tried to assign null to " + target.GetType().Name + "." + field + "[" + i + "]");
                }
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var serializedObject = new SerializedObject(target);
            FindProperty(serializedObject, field).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var serializedObject = new SerializedObject(target);
            FindProperty(serializedObject, field).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var serializedObject = new SerializedObject(target);
            FindProperty(serializedObject, field).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Object target, string field, Color value)
        {
            var serializedObject = new SerializedObject(target);
            FindProperty(serializedObject, field).colorValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int enumIndex)
        {
            var serializedObject = new SerializedObject(target);
            FindProperty(serializedObject, field).enumValueIndex = enumIndex;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
