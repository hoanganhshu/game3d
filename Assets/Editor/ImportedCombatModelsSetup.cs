using System.IO;
using System.Linq;
using FPSGame;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FPSGame.Editor
{
    /// <summary>Imports the selected Meshy gun demons and first-person arms into the existing gameplay prefabs.</summary>
    public static class ImportedCombatModelsSetup
    {
        private const string NormalRoot = "Assets/Art/Characters/GunDemonNormal";
        private const string RedRoot = "Assets/Art/Characters/GunDemonRed";
        private const string ViewRoot = "Assets/Art/Characters/FirstPersonArms";

        private const string NormalWalk = NormalRoot + "/Source/Normal_Walk.fbx";
        private const string NormalRun = NormalRoot + "/Source/Normal_Run.fbx";
        private const string NormalAttack = NormalRoot + "/Source/Normal_Attack.fbx";
        private const string NormalDeath = NormalRoot + "/Source/Normal_Death.fbx";

        private const string RedIdle = RedRoot + "/Source/Red_Idle.fbx";
        private const string RedWalk = RedRoot + "/Source/Red_Walk.fbx";
        private const string RedRun = RedRoot + "/Source/Red_Run.fbx";
        private const string RedAttack = RedRoot + "/Source/Red_Attack.fbx";
        private const string RedDeath = RedRoot + "/Source/Red_Death.fbx";

        private const string ViewModel = ViewRoot + "/Source/FirstPersonArms.fbx";
        private const string EnemyPrefab = "Assets/Prefabs/Enemy.prefab";
        private const string BossPrefab = "Assets/Prefabs/Boss.prefab";
        private const string PlayerPrefab = "Assets/Prefabs/Player.prefab";

        [InitializeOnLoadMethod]
        private static void IntegrateOnceAfterImport()
        {
            if (!File.Exists(NormalWalk) || File.Exists(NormalRoot + "/GunDemonNormalVisual.prefab")) return;
            EditorApplication.delayCall += Integrate;
        }

        [MenuItem("Tools/Graphics/Integrate Imported Combat Models")]
        public static void Integrate()
        {
            string[] required = { NormalWalk, NormalRun, NormalAttack, NormalDeath, RedIdle, RedWalk, RedRun, RedAttack, RedDeath, ViewModel };
            if (required.Any(path => !File.Exists(path)))
            {
                Debug.LogError("[Combat Models] One or more source FBX files are missing.");
                return;
            }

            ConfigureCharacter(NormalWalk, true);
            ConfigureCharacter(NormalRun, true);
            ConfigureCharacter(NormalAttack, false);
            ConfigureCharacter(NormalDeath, false);
            ConfigureCharacter(RedIdle, true);
            ConfigureCharacter(RedWalk, true);
            ConfigureCharacter(RedRun, true);
            ConfigureCharacter(RedAttack, false);
            ConfigureCharacter(RedDeath, false);
            ConfigureStaticModel(ViewModel);

            Material normalMaterial = BuildMaterial(
                NormalRoot + "/GunDemonNormal.mat",
                NormalRoot + "/Source/Meshy_AI_Hellforged_Sentinel_biped_texture_0.png",
                NormalRoot + "/Source/Meshy_AI_Hellforged_Sentinel_biped_texture_0_normal.png",
                NormalRoot + "/Source/Meshy_AI_Hellforged_Sentinel_biped_texture_0_metallic.png");
            Material redMaterial = BuildMaterial(
                RedRoot + "/GunDemonRed.mat",
                RedRoot + "/Source/Meshy_AI_Crimson_Maw_Core_biped_texture_0.png",
                RedRoot + "/Source/Meshy_AI_Crimson_Maw_Core_biped_texture_0_normal.png",
                RedRoot + "/Source/Meshy_AI_Crimson_Maw_Core_biped_texture_0_metallic.png");
            Material armsMaterial = BuildMaterial(
                ViewRoot + "/FirstPersonArms.mat",
                ViewRoot + "/Source/Meshy_AI_Cybernetic_Marksman_1012015958_texture.png",
                ViewRoot + "/Source/Meshy_AI_Cybernetic_Marksman_1012015958_texture_normal.png",
                ViewRoot + "/Source/Meshy_AI_Cybernetic_Marksman_1012015958_texture_metallic.png");

            AnimatorController normalController = BuildController(
                NormalRoot + "/GunDemonNormal.controller",
                FindClip(NormalWalk), FindClip(NormalWalk), FindClip(NormalRun), FindClip(NormalAttack), FindClip(NormalDeath), true);
            AnimatorController redController = BuildController(
                RedRoot + "/GunDemonRed.controller",
                FindClip(RedIdle), FindClip(RedWalk), FindClip(RedRun), FindClip(RedAttack), FindClip(RedDeath), false);

            GameObject normalVisual = BuildCharacterVisual(NormalWalk, normalController, normalMaterial, NormalRoot + "/GunDemonNormalVisual.prefab", 2.0f);
            GameObject redVisual = BuildCharacterVisual(RedIdle, redController, redMaterial, RedRoot + "/GunDemonRedVisual.prefab", 2.8f);
            GameObject armsVisual = BuildViewVisual(ViewModel, armsMaterial, ViewRoot + "/FirstPersonArmsVisual.prefab");

            ReplaceEnemyVisual(EnemyPrefab, normalVisual, 1.75f, 1.25f);
            ReplaceEnemyVisual(BossPrefab, redVisual, 2.45f, 1.8f);
            ReplacePlayerViewModels(armsVisual);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Combat Models] Integrated normal gun demon, red gun demon, Animator states, and first-person arms.");
        }

        private static void ConfigureCharacter(string path, bool loop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = loop;
                clip.loopPose = loop;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootRotation = true;
                clip.heightFromFeet = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalOrientation = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureStaticModel(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static Material BuildMaterial(string materialPath, string albedoPath, string normalPath, string metallicPath)
        {
            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = Shader.Find("Standard");
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath));
            material.EnableKeyword("_METALLICGLOSSMAP");
            material.SetFloat("_Metallic", 0.45f);
            material.SetFloat("_Glossiness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimationClip FindClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        private static AnimatorController BuildController(string path, AnimationClip idleClip, AnimationClip walkClip,
            AnimationClip runClip, AnimationClip attackClip, AnimationClip deathClip, bool freezeIdle)
        {
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MotionRate", AnimatorControllerParameterType.Float);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle"); idle.motion = idleClip; idle.speed = freezeIdle ? 0f : 1f;
            AnimatorState walk = machine.AddState("Walk"); walk.motion = walkClip;
            AnimatorState run = machine.AddState("Run"); run.motion = runClip;
            walk.speedParameterActive = true; walk.speedParameter = "MotionRate";
            run.speedParameterActive = true; run.speedParameter = "MotionRate";
            AnimatorState attack = machine.AddState("Attack"); attack.motion = attackClip;
            AnimatorState hit = machine.AddState("Hit"); hit.motion = idleClip; hit.speed = 0f;
            AnimatorState death = machine.AddState("Death"); death.motion = deathClip;
            machine.defaultState = idle;

            AddFloatTransition(idle, walk, AnimatorConditionMode.Greater, 0.05f);
            AddFloatTransition(walk, idle, AnimatorConditionMode.Less, 0.05f);
            AddFloatTransition(walk, run, AnimatorConditionMode.Greater, 0.72f);
            AddFloatTransition(run, walk, AnimatorConditionMode.Less, 0.72f);
            AddAnyTrigger(machine, attack, "Attack", 0.04f);
            AddAnyTrigger(machine, hit, "Hit", 0.03f);
            // Death is entered explicitly by EnemyAnimatorBridge.PlayDeath(). Keeping an
            // Any State bool transition here can re-enter Death and pin it to frame zero.
            AddReturn(attack, idle, 0.9f);
            AddReturn(hit, idle, 0.08f);
            return controller;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.18f;
            transition.hasFixedDuration = true;
            transition.AddCondition(mode, threshold, "Speed");
        }

        private static void AddAnyTrigger(AnimatorStateMachine machine, AnimatorState state, string parameter, float duration)
        {
            var transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddReturn(AnimatorState state, AnimatorState destination, float exitTime)
        {
            var transition = state.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.14f;
            transition.hasFixedDuration = true;
        }

        private static GameObject BuildCharacterVisual(string modelPath, RuntimeAnimatorController controller, Material material, string prefabPath, float targetHeight)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            Transform sphere = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Icosphere");
            if (sphere != null) sphere.gameObject.SetActive(false);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.activeSelf) renderer.sharedMaterial = material;
            NormalizeModel(model.transform, targetHeight, true);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EnemyAnimatorBridge bridge = root.AddComponent<EnemyAnimatorBridge>();
            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("hasDeathAnimation").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildViewVisual(string modelPath, Material material, string prefabPath)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject root = new GameObject("FirstPersonArmsVisual");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "MeshyArmsAndGun";
            model.transform.SetParent(root.transform, false);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;
            NormalizeModel(model.transform, 0.85f, false);
            SetLayerRecursively(root, 0);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void NormalizeModel(Transform model, float targetSize, bool placeOnGround)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float sourceSize = placeOnGround ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = targetSize / Mathf.Max(0.001f, sourceSize);
            model.localScale = Vector3.Scale(model.localScale, Vector3.one * scale);

            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            Vector3 offset = -bounds.center;
            if (placeOnGround) offset.y += bounds.extents.y;
            model.position += offset;
        }

        private static void ReplaceEnemyVisual(string prefabPath, GameObject visualPrefab, float eyeHeight, float gunHeight)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                foreach (string oldName in new[] { "Body", "Visor", "Gun", "ImportedVisual" })
                {
                    Transform old = root.transform.Find(oldName);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }

                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.name = "ImportedVisual";
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                // Enemy roots sit about 8 cm above the baked NavMesh in the current levels.
                // Sink the rendered feet by the same amount so they visually touch the floor.
                SnapVisualToGround(visual.transform, root.transform, 0.08f);

                Transform eye = root.transform.Find("Eye");
                if (eye != null) eye.localPosition = new Vector3(0f, eyeHeight, 0.15f);
                Transform gunPoint = root.transform.Find("GunPoint");
                if (gunPoint != null) gunPoint.localPosition = new Vector3(0f, gunHeight, 0.75f);

                Enemy enemy = root.GetComponent<Enemy>();
                EnemyAnimatorBridge bridge = visual.GetComponent<EnemyAnimatorBridge>();
                Renderer renderer = visual.GetComponentInChildren<Renderer>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("animationBridge").objectReferenceValue = bridge;
                serialized.FindProperty("bodyRenderer").objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SnapVisualToGround(Transform visual, Transform characterRoot, float groundSink)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float targetY = characterRoot.position.y - groundSink;
            visual.position += Vector3.up * (targetY - bounds.min.y);
        }

        private static void ReplacePlayerViewModels(GameObject armsVisual)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform holder = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WeaponHolder");
                if (holder == null) throw new System.InvalidOperationException("WeaponHolder was not found in Player.prefab");
                foreach (Weapon weapon in holder.GetComponentsInChildren<Weapon>(true))
                {
                    Transform model = new SerializedObject(weapon).FindProperty("model").objectReferenceValue as Transform;
                    if (model == null) continue;
                    Transform old = model.Find("ImportedFirstPersonArms");
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(armsVisual, model);
                    instance.name = "ImportedFirstPersonArms";
                    // Pull the view model away from the lower-right edge so the player can see
                    // the forearms as well as the weapon without covering the crosshair.
                    instance.transform.localPosition = new Vector3(-0.06f, -0.16f, 0.68f);
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    instance.transform.localScale = Vector3.one * 0.56f;
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
