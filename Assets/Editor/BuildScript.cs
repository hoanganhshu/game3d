using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FPSGame.Editor
{
    /// <summary>
    /// Builds the Android APK.
    ///
    /// Command line:
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android
    ///         -executeMethod FPSGame.Editor.BuildScript.BuildAndroid -logFile build.log
    ///
    /// Runs ProjectBootstrapper.SetupAll first, sets PlayerSettings, then builds
    /// Builds/Android/FPSGame.apk. In batch mode it exits with a non-zero code on failure.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputPath = "Builds/Android/FPSGame.apk";        public const string CompanyName = "MyName";
        public const string ProductName = "FPS Game";
        public const string PackageName = "com.myname.fpsgame";
        public const int MinAndroidApiLevel = 26;

        [MenuItem("Tools/Build Android APK", false, 20)]
        public static void BuildAndroidMenu()
        {
            BuildAndroid();
        }

        /// <summary>
        /// ARM64 APK. Unity 6.6 no longer builds x86_64 for Android; x86_64 emulator images
        /// (Android 11+) run it through their built-in ARM translation (see README).
        /// </summary>
        public static void BuildAndroid()
        {
            Build(OutputPath, AndroidArchitecture.ARM64);
        }

        private static void Build(string outputPath, AndroidArchitecture architectures)
        {
            try
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    throw new BuildFailedException(
                        "Android Build Support is not installed for this Unity version. " +
                        "Install the Android Build Support module (with SDK/NDK and OpenJDK) from Unity Hub.");
                }

                ProjectBootstrapper.SetupAll();
                ConfigurePlayerSettings();
                UseEmbeddedAndroidTools();
                PlayerSettings.Android.targetArchitectures = architectures;

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                    !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    throw new BuildFailedException("Could not switch the active build target to Android.");
                }

                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                EditorUserBuildSettings.development = false;

                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                var options = new BuildPlayerOptions
                {
                    scenes = ProjectBootstrapper.ScenePaths,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException("Build " + summary.result + " with " + summary.totalErrors + " error(s).");
                }

                Debug.Log("[Build] APK built (" + architectures + "): " + Path.GetFullPath(outputPath) +
                          " (" + (new FileInfo(outputPath).Length / (1024f * 1024f)).ToString("0.0") + " MB, " +
                          summary.totalTime.TotalSeconds.ToString("0") + " s)");

                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[Build] FAILED: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                else
                {
                    EditorUtility.DisplayDialog("Build failed", exception.Message, "OK");
                }
            }
        }

        /// <summary>
        /// Uses the JDK / SDK / NDK / Gradle installed with Unity whenever those folders exist.
        /// A build that ran before the tools were installed switches "Installed with Unity" off
        /// (saved in EditorPrefs), and every later build then fails with "JDK not found".
        /// </summary>
        private static void UseEmbeddedAndroidTools()
        {
            string androidPlayer = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
            EnableEmbeddedTool("JdkUseEmbedded", Path.Combine(androidPlayer, "OpenJDK"));
            EnableEmbeddedTool("SdkUseEmbedded", Path.Combine(androidPlayer, "SDK"));
            EnableEmbeddedTool("NdkUseEmbedded", Path.Combine(androidPlayer, "NDK"));
            EnableEmbeddedTool("GradleUseEmbedded", Path.Combine(androidPlayer, "Tools", "gradle"));
        }

        private static void EnableEmbeddedTool(string preferenceKey, string directory)
        {
            if (!Directory.Exists(directory))
            {
                Debug.LogWarning("[Build] " + directory + " not found. Install it via Unity Hub (Android Build Support).");
                return;
            }

            if (!EditorPrefs.GetBool(preferenceKey, true))
            {
                EditorPrefs.SetBool(preferenceKey, true);
                Debug.Log("[Build] Re-enabled '" + preferenceKey + "' (" + directory + ").");
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // Landscape only (both directions)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // SDK levels: target = highest installed. The original spec asked for min API 24, but
            // Unity 6.6 only supports API 26 (Android 8.0) and above, so 26 is the lowest valid value.
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)MinAndroidApiLevel;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;

            // Signed with the debug keystore
            PlayerSettings.Android.useCustomKeystore = false;

            // Display
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);
            PlayerSettings.runInBackground = false;

            AssetDatabase.SaveAssets();
        }
    }
}
