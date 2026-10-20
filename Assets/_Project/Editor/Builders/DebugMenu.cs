using FireReady.Core;
using UnityEditor;
using UnityEngine;

namespace FireReady.EditorTools
{
    /// <summary>Play-mode helpers for checking screens without a real AR session.</summary>
    internal static class DebugMenu
    {
        [MenuItem("Tools/FireReady/Debug/Fake Session Result")]
        static void FakeResult()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
            var lines = new[]
            {
                new ScoreLine("Extinguisher choice", 30, 30),
                new ScoreLine("Safety actions", 10, 10),
                new ScoreLine("PASS order", 15, 15),
                new ScoreLine("Distance", 9, 15, "Too close for 4 s. Stay 1.2 to 3 m back."),
                new ScoreLine("Aim and sweep", 12, 20, "Sweep started late. Sweep from the first second."),
                new ScoreLine("Outcome", 10, 10)
            };
            EventBus.Publish(new ScenarioStarted("Office waste bin", FireClass.ClassA, 70f));
            EventBus.Publish(new SessionScored("Office waste bin", FireClass.ClassA, "Guided",
                SessionOutcome.Extinguished, 161f, 86, 100, lines, new string[0]));
        }

        [MenuItem("Tools/FireReady/Debug/Open History")]
        static void OpenHistory()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
            EventBus.Publish(new HistoryRequested());
        }
    }
}
