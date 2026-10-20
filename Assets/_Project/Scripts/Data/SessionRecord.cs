using System;
using System.Collections.Generic;
using FireReady.Core;

namespace FireReady.Data
{
    /// <summary>One saved session, written as its own JSON file (DMP: sessions/{id}.json).</summary>
    [Serializable]
    public sealed class SessionRecord
    {
        public int version = 1;
        public string id;
        public string title;
        public string fireClass;
        public string mode;
        public string trainee;
        public string startedUtc;
        public string endedUtc;
        public float durationSeconds;
        public string outcome;
        public int score;
        public int maxScore;
        public List<ScoreLine> rules = new List<ScoreLine>();
        public List<string> mistakes = new List<string>();

        public SessionSummary ToSummary() => new SessionSummary
        {
            id = id,
            title = title,
            fireClass = fireClass,
            mode = mode,
            endedUtc = endedUtc,
            outcome = outcome,
            score = score,
            maxScore = maxScore
        };
    }

    /// <summary>The list shown in History, kept in index.json so the screen opens without reading every file.</summary>
    [Serializable]
    internal sealed class SessionIndex
    {
        public int version = 1;
        public List<SessionSummary> sessions = new List<SessionSummary>();
    }
}
