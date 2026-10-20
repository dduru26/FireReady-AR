// FireReady AR - one-time project setup, stage A.
// Runs automatically when the editor loads: switches the platform to iOS
// and installs the packages that stage B (and the app) depend on.
// Stage B lives in its own assembly that only compiles once those packages exist.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace FireReady.EditorSetup
{
    [InitializeOnLoad]
    public static class FireReadySetupStageA
    {
        public const string DoneMarker = "ProjectSettings/FireReadySetup.done";
        public const string LogPath = "Logs/FireReadySetup.log";

        static readonly string[] RequiredPackages =
        {
            "com.unity.xr.arfoundation",
            "com.unity.xr.arkit",
            "com.unity.addressables",
            "com.unity.xr.management"
        };

        static AddAndRemoveRequest request;

        static FireReadySetupStageA()
        {
            EditorApplication.delayCall += Run;
        }

        public static void Log(string message)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss") + " A " + message + "\n");
            Debug.Log("[FireReadySetup] " + message);
        }

        [MenuItem("Tools/FireReady/Install Packages")]
        static void Run()
        {
            if (File.Exists(DoneMarker)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
                EditorSettings.serializationMode = SerializationMode.ForceText;
            if (VersionControlSettings.mode != "Visible Meta Files")
                VersionControlSettings.mode = "Visible Meta Files";

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Log("Switching platform to iOS");
                bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                if (!ok) { Log("Platform switch FAILED (is iOS Build Support installed?)"); return; }
                EditorApplication.delayCall += Run; // continue once the switch has settled
                return;
            }

            string manifest = File.ReadAllText("Packages/manifest.json");
            var missing = RequiredPackages.Where(p => !manifest.Contains("\"" + p + "\"")).ToArray();
            if (missing.Length == 0) return;
            if (request != null && !request.IsCompleted) return;

            Log("Installing packages: " + string.Join(", ", missing));
            request = Client.AddAndRemove(missing, null);
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (request == null || !request.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (request.Status == StatusCode.Success)
                Log("Packages installed: " + string.Join(", ", request.Result.Where(p => RequiredPackages.Contains(p.name)).Select(p => p.name + "@" + p.version)));
            else
                Log("Package install FAILED: " + (request.Error != null ? request.Error.message : "unknown error"));
            request = null;
        }
    }
}
