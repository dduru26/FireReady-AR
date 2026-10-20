using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FireReady.EditorTools
{
    /// <summary>Shared helpers for the FireReady editor builders.</summary>
    internal static class BuilderUtils
    {
        const string LogPath = "Logs/FireReadySetup.log";

        public static void Log(string message)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            Debug.Log("[FireReady] " + message);
        }

        public static void SetRef(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(property);
            if (prop == null)
                throw new InvalidOperationException($"{target.GetType().Name} has no field '{property}'");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T LoadOrCreate<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);
    }
}
