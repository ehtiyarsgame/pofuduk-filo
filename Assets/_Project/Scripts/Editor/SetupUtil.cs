using System.IO;
using UnityEditor;
using UnityEngine;

namespace PofudukFilo.EditorTools
{
    /// <summary>Asset and SerializedObject helpers for the one-click setup.</summary>
    internal static class SetupUtil
    {
        public const string Root = "Assets/_Project/Generated";

        public static string PathFor(string sub, string file)
        {
            string dir = $"{Root}/{sub}";
            Directory.CreateDirectory(dir);
            return $"{dir}/{file}";
        }

        /// <summary>Creates or overwrites a ScriptableObject asset, keeping its GUID on re-runs.</summary>
        public static T SaveAsset<T>(T asset, string sub, string name) where T : Object
        {
            string path = PathFor(sub, name + ".asset");
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Material SaveMaterial(Material mat, string name)
        {
            string path = PathFor("Materials", name + ".mat");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                existing.enableInstancing = mat.enableInstancing;
                existing.renderQueue = mat.renderQueue;
                Object.DestroyImmediate(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        public static T SavePrefab<T>(GameObject go, string name) where T : Component
        {
            string path = PathFor("Prefabs", name + ".prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<T>();
        }

        // ---------------------------------------------------------------- SerializedObject setters

        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Setup] {target.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            Assign(p, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"[Setup] {target.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedProperty p, object value)
        {
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v: p.vector2Value = v; break;
                case string s: p.stringValue = s; break;
                case null: p.objectReferenceValue = null; break;
                default: Debug.LogError($"[Setup] Unsupported value type {value.GetType()} for {p.propertyPath}"); break;
            }
        }
    }
}
