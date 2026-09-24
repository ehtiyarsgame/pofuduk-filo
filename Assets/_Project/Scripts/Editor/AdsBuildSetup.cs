using System;
using System.IO;
using System.Reflection;
using PofudukFilo.Core;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PofudukFilo.EditorTools
{
    /// <summary>
    /// Batch-mode preparation for the Google Mobile Ads plugin, via reflection so this compiles
    /// whether or not the package is installed: writes the AdMob App ID into the plugin's
    /// settings asset (the manifest meta-data comes from it — a missing id crashes the app at
    /// launch), enables Unity's custom Gradle templates, and runs the External Dependency
    /// Manager's Android resolver so the SDK's AARs are declared before BuildPlayer.
    /// </summary>
    public static class AdsBuildSetup
    {
        public static void PrepareAndroid()
        {
            Type settingsType = FindType("GoogleMobileAds.Editor.GoogleMobileAdsSettings");
            if (settingsType == null)
            {
                Debug.Log("[Ads] Google Mobile Ads plugin not installed; building without ads.");
                return;
            }

            SetAppId(settingsType);
            EnableGradleTemplates();
            Resolve();
        }

        private static void SetAppId(Type settingsType)
        {
            Object settings = null;
            MethodInfo load = settingsType.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (load != null) settings = load.Invoke(null, null) as Object;
            if (settings == null)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:" + settingsType.Name))
                {
                    settings = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), settingsType);
                    if (settings != null) break;
                }
            }
            if (settings == null)
            {
                Debug.LogError("[Ads] GoogleMobileAdsSettings asset not found; the app would crash on launch.");
                return;
            }

            var so = new SerializedObject(settings);
            SerializedProperty id = so.FindProperty("adMobAndroidAppId");
            if (id == null)
            {
                Debug.LogError("[Ads] adMobAndroidAppId field not found on " + settingsType.FullName);
                return;
            }
            id.stringValue = AdsConfig.AndroidAppId;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Ads] AdMob Android App ID set.");
        }

        /// <summary>Unity uses a template when the file exists in Assets/Plugins/Android (= the Player Settings checkbox).</summary>
        private static void EnableGradleTemplates()
        {
            string engine = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            string source = Path.Combine(engine, "Tools", "GradleTemplates");
            const string target = "Assets/Plugins/Android";
            Directory.CreateDirectory(target);
            foreach (string name in new[] { "mainTemplate.gradle", "settingsTemplate.gradle", "gradleTemplate.properties" })
            {
                string from = Path.Combine(source, name), to = Path.Combine(target, name);
                if (File.Exists(to)) continue;
                if (File.Exists(from)) File.Copy(from, to);
                else Debug.LogWarning("[Ads] Gradle template missing: " + from);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Resolve()
        {
            Type resolver = FindType("GooglePlayServices.PlayServicesResolver");
            MethodInfo sync = resolver?.GetMethod("ResolveSync", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(bool) }, null);
            if (sync == null)
            {
                Debug.LogWarning("[Ads] External Dependency Manager not found; Android dependencies may be missing.");
                return;
            }
            object ok = sync.Invoke(null, new object[] { true });
            Debug.Log($"[Ads] Android dependencies resolved: {ok}");
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
