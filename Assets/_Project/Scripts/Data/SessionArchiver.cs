using System;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Data
{
    /// <summary>Saves every scored session (FR-28) and reports whether it is a new best.</summary>
    public sealed class SessionArchiver : MonoBehaviour
    {
        JsonSessionRepository m_Repository;
        DateTime m_StartedUtc;

        public void Construct(JsonSessionRepository repository) => m_Repository = repository;

        void OnEnable()
        {
            EventBus.Subscribe<ScenarioStarted>(OnStarted);
            EventBus.Subscribe<SessionScored>(OnScored);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenarioStarted>(OnStarted);
            EventBus.Unsubscribe<SessionScored>(OnScored);
        }

        void OnStarted(ScenarioStarted e) => m_StartedUtc = DateTime.UtcNow;

        void OnScored(SessionScored e)
        {
            if (m_Repository == null)
                return;

            int previousBest = m_Repository.BestScore(e.Title);
            var record = new SessionRecord
            {
                title = e.Title,
                fireClass = e.FireClass.ToString(),
                mode = e.Mode,
                startedUtc = m_StartedUtc.ToString("o"),
                endedUtc = DateTime.UtcNow.ToString("o"),
                durationSeconds = e.Duration,
                outcome = e.Outcome.ToString(),
                score = e.Total,
                maxScore = e.Max
            };
            if (e.Lines != null) record.rules.AddRange(e.Lines);
            if (e.Mistakes != null) record.mistakes.AddRange(e.Mistakes);

            try
            {
                m_Repository.Save(record);
                Debug.Log($"[Sessions] Saved {record.id} to {m_Repository.Folder}");
                EventBus.Publish(new SessionSaved(previousBest >= 0 && e.Total > previousBest, m_Repository.Count));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Sessions] Save failed: {ex}");
            }
        }
    }
}
