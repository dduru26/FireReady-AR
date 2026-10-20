using System;
using System.Collections.Generic;

namespace FireReady.Core
{
    /// <summary>One scored criterion in the debrief.</summary>
    [Serializable]
    public struct ScoreLine
    {
        public string Name;
        public int Points;
        public int Max;
        public string Feedback;

        public ScoreLine(string name, int points, int max, string feedback = null)
        {
            Name = name;
            Points = Math.Max(0, Math.Min(points, max));
            Max = max;
            Feedback = feedback;
        }
    }

    /// <summary>Short form of a saved session, as listed in History (FR-29).</summary>
    [Serializable]
    public sealed class SessionSummary
    {
        public string id;
        public string title;
        public string fireClass;
        public string mode;
        public string endedUtc;   // ISO 8601, round-trip format
        public string outcome;
        public int score;
        public int maxScore;
    }

    /// <summary>Read access to saved sessions, so the UI never touches files directly.</summary>
    public interface ISessionHistory
    {
        /// <summary>Saved sessions, newest first.</summary>
        IReadOnlyList<SessionSummary> Recent(int max);
        int Count { get; }
        void DeleteAll();
    }
}
