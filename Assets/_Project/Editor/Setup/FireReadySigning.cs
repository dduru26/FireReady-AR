using UnityEditor;
using UnityEngine;

namespace FireReady.EditorSetup
{
    /// <summary>
    /// Stores the Apple signing team in Player Settings so every generated
    /// Xcode project is signed automatically (no re-selecting the team).
    /// </summary>
    public static class FireReadySigning
    {
        const string TeamId = "8CGM596A96"; // Duru Donald (Personal Team)

        [MenuItem("Tools/FireReady/Apply iOS Signing")]
        public static void Apply()
        {
            PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            AssetDatabase.SaveAssets();
            System.IO.File.AppendAllText("Logs/FireReadySetup.log",
                $"[{System.DateTime.Now:HH:mm:ss}] signing set: team {TeamId}, automatic\n");
            Debug.Log("[FireReady] iOS signing set to team " + TeamId);
        }
    }
}
