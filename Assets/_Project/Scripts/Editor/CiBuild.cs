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

        /// <summary>Installable test APK (the "apk" release the owner side-loads).</summary>
        public static void BuildAndroid() => BuildAndroidPlayer(release: false);

        /// <summary>
        /// Store build (release.yml): a signed .aab for Google Play, with PF_RELEASE defined so on-screen error
        /// reporting is off. Keystore comes from ANDROID_KEYSTORE_PATH / _PASS / ANDROID_KEY_ALIAS / _PASS.
        /// </summary>
        public static void BuildAndroidRelease() => BuildAndroidPlayer(release: true);

        private static void BuildAndroidPlayer(bool release)
        {
            try
            {
                PofudukFiloSetup.BuildEverythingNonInteractive();
                ConfigureAndroid();
                AdsBuildSetup.PrepareAndroid();
                if (release && !ConfigureRelease())
                {
                    EditorApplication.Exit(1);
                    return;
                }

                int missing = SmokeCheck.CountUnassignedReferences();
                if (missing > 0)
                {
                    Debug.LogError($"[CiBuild] {missing} unassigned reference(s) in the scene; not building a broken APK.");
                    EditorApplication.Exit(1);
                    return;
                }

                string ext = release ? ".aab" : ".apk";
                string path = Arg("-customBuildPath") ?? "build/Android/PofudukFilo" + ext;
                if (!path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) path = System.IO.Path.Combine(path, "PofudukFilo" + ext);

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { PofudukFiloSetup.ScenePath },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                Debug.Log($"[CiBuild] {report.summary.result}: {path} ({report.summary.totalSize / (1024 * 1024)} MB, " +
                          $"{report.summary.totalErrors} errors, versionCode {PlayerSettings.Android.bundleVersionCode})");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>App bundle, upload-key signing and PF_RELEASE. False (with an error logged) if the keystore is missing.</summary>
        private static bool ConfigureRelease()
        {
            string keystore = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(keystore) || !System.IO.File.Exists(keystore))
            {
                Debug.LogError("[CiBuild] Release build needs ANDROID_KEYSTORE_PATH pointing at the upload keystore.");
                return false;
            }
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS") ?? "";
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS") ?? "";
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEY_PASS") ?? "";
#if UNITY_6000_0_OR_NEWER
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            if (!defines.Contains("PF_RELEASE"))
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.IsNullOrEmpty(defines) ? "PF_RELEASE" : defines + ";PF_RELEASE");
#else
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (!defines.Contains("PF_RELEASE"))
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, string.IsNullOrEmpty(defines) ? "PF_RELEASE" : defines + ";PF_RELEASE");
#endif
            return true;
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
            PlayerSettings.companyName = "Ehtiyars Game";
            PlayerSettings.productName = "Galaxy Paws"; // global store name; package id kept so installs and saves carry over
            PlayerSettings.bundleVersion = "1.0.0";
            // Every CI build gets a higher versionCode (the workflow's run number), so each APK installs over
            // the last one and Google Play accepts each upload.
            if (int.TryParse(Environment.GetEnvironmentVariable("BUILD_NUMBER"), out int buildNumber) && buildNumber > 0)
                PlayerSettings.Android.bundleVersionCode = buildNumber;
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
