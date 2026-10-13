using System.IO;
using System.Linq;
using FPSGame;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FPSGame.Editor
{
    /// <summary>Builds a reusable Meshy visual prefab and Animator from the downloaded FBX files.</summary>
    public static class MeshyEnemySetup
    {
        private const string Source = "Assets/Art/Characters/EnemyGrunt/MeshySource/Meshy_AI_HumanoidEnemyRobot_biped";
        private const string CharacterPath = Source + "/Meshy_AI_HumanoidEnemyRobot_biped_Character_output.fbx";
        private const string WalkPath = Source + "/Meshy_AI_HumanoidEnemyRobot_biped_Animation_Walking_withSkin.fbx";
        private const string RunPath = Source + "/Meshy_AI_HumanoidEnemyRobot_biped_Animation_Running_withSkin.fbx";
        private const string Generated = "Assets/Art/Characters/EnemyGrunt/Generated";
        private const string ControllerPath = Generated + "/EnemyGrunt.controller";
        private const string PrefabPath = Generated + "/EnemyGruntVisual.prefab";

        [InitializeOnLoadMethod]
        private static void BuildOnceWhenImported()
        {
            if (!File.Exists(CharacterPath) || File.Exists(PrefabPath)) return;
            EditorApplication.delayCall += Build;
        }

        [MenuItem("Tools/Graphics/Build Meshy Enemy")]
        public static void Build()
        {
            if (!File.Exists(CharacterPath) || !File.Exists(WalkPath) || !File.Exists(RunPath))
            {
                Debug.LogError("Meshy enemy FBX files are missing from " + Source);
                return;
            }

            EnsureFolder("Assets/Art/Characters/EnemyGrunt", "Generated");
            ConfigureModel(CharacterPath, ModelImporterAnimationType.Generic, false);
            ConfigureModel(WalkPath, ModelImporterAnimationType.Generic, true);
            ConfigureModel(RunPath, ModelImporterAnimationType.Generic, true);

            AnimationClip walk = FindClip(WalkPath);
            AnimationClip run = FindClip(RunPath);
            AnimatorController controller = BuildController(walk, run);
            BuildVisualPrefab(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Meshy enemy ready: " + PrefabPath);
        }

        private static void ConfigureModel(string path, ModelImporterAnimationType animationType, bool loop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            importer.animationType = animationType;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = loop;
                clip.loopPose = loop;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip FindClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        private static AnimatorController BuildController(AnimationClip walk, AnimationClip run)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimationClip still = new AnimationClip { name = "StillPose" };
            AssetDatabase.AddObjectToAsset(still, controller);

            AnimatorState idle = machine.AddState("Idle"); idle.motion = still;
            AnimatorState walking = machine.AddState("Walk"); walking.motion = walk;
            AnimatorState running = machine.AddState("Run"); running.motion = run;
            AnimatorState attack = machine.AddState("Attack"); attack.motion = still;
            AnimatorState hit = machine.AddState("Hit"); hit.motion = still;
            AnimatorState dead = machine.AddState("Death"); dead.motion = still;
            machine.defaultState = idle;

            AddFloatTransition(idle, walking, AnimatorConditionMode.Greater, 0.08f);
            AddFloatTransition(walking, idle, AnimatorConditionMode.Less, 0.08f);
            AddFloatTransition(walking, running, AnimatorConditionMode.Greater, 0.72f);
            AddFloatTransition(running, walking, AnimatorConditionMode.Less, 0.72f);

            AddAnyTrigger(machine, attack, "Attack", 0.05f);
            AddAnyTrigger(machine, hit, "Hit", 0.03f);
            AddAnyBool(machine, dead, "Dead", true, 0.08f);
            AddReturn(attack, idle, 0.18f);
            AddReturn(hit, idle, 0.14f);
            return controller;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.14f;
            transition.AddCondition(mode, threshold, "Speed");
        }

        private static void AddAnyTrigger(AnimatorStateMachine machine, AnimatorState to, string parameter, float duration)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddAnyBool(AnimatorStateMachine machine, AnimatorState to, string parameter, bool value, float duration)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddReturn(AnimatorState state, AnimatorState destination, float exitTime)
        {
            AnimatorStateTransition transition = state.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.05f;
        }

        private static void BuildVisualPrefab(RuntimeAnimatorController controller)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            GameObject root = new GameObject("EnemyGruntVisual");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            EnemyAnimatorBridge bridge = root.AddComponent<EnemyAnimatorBridge>();
            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
