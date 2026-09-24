using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Headless Android build for GitHub Actions (GameCI unity-builder `buildMethod`).
    /// Generates the content and scene first, so the repo needs no generated assets committed.
    /// </summary>
    public static class CiBuild
    {
        public const string PackageName = "com.ehtiyarsgame.pofudukfilo";

        public static void BuildAndroid()
        {
            try
            {
                PofudukFiloSetup.BuildEverythingNonInteractive();
                ConfigureAndroid();

                int missing = SmokeCheck.CountUnassignedReferences();
                if (missing > 0)
                {
                    Debug.LogError($"[CiBuild] {missing} unassigned reference(s) in the scene; not building a broken APK.");
                    EditorApplication.Exit(1);
                    return;
                }

                string path = Arg("-customBuildPath") ?? "build/Android/PofudukFilo.apk";
                if (!path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) path = System.IO.Path.Combine(path, "PofudukFilo.apk");

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { PofudukFiloSetup.ScenePath },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[CiBuild] {report.summary.result}: {path} ({report.summary.totalSize / (1024 * 1024)} MB, " +
                          $"{report.summary.totalErrors} errors)");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Linux64 (Mono) player of the already-generated scene, for the CI QA run: QaAutopilot
        /// plays it under Xvfb and captures screenshots + telemetry. Run after BuildAndroid.
        /// </summary>
        public static void BuildLinuxQa()
        {
            try
            {
                string path = Arg("-customBuildPath") ?? "build/Linux/PofudukFilo.x86_64";
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { PofudukFiloSetup.ScenePath },
                    locationPathName = path,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                };
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;
                PlayerSettings.runInBackground = true;

                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[CiBuild] Linux QA player {report.summary.result}: {path}");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureAndroid()
        {
#if UNITY_6000_0_OR_NEWER
            NamedBuildTarget android = NamedBuildTarget.Android;
#else
            const BuildTargetGroup android = BuildTargetGroup.Android; // older editors / offline compile check
#endif
            PlayerSettings.SetApplicationIdentifier(android, PackageName);
            PlayerSettings.companyName = "ehtiyarsgame";
            PlayerSettings.productName = "Fluffy Fleet"; // global store name
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Many current phones are 64-bit only, which requires IL2CPP.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false; // an .apk you can install directly

            EnableNewInputSystem();
        }

        /// <summary>
        /// Player Settings ▸ Active Input Handling = Input System only. "Both" is flagged by the Android
        /// build as unsupported; the one legacy call (PlayerController fallback) disables itself.
        /// There is no public API, so the serialized ProjectSettings field is set (0 = old, 1 = new, 2 = both).
        /// </summary>
        private static void EnableNewInputSystem()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p == null)
            {
                Debug.LogWarning("[CiBuild] activeInputHandler not found; touch input may be disabled.");
                return;
            }
            p.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
