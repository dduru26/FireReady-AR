using System;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Training
{
    /// <summary>
    /// Logs what the trainee does during a session (FR-25) and scores it when the
    /// session ends (FR-26). It only listens to events, so it has no references to
    /// the AR, simulation or UI code.
    /// </summary>
    public sealed class SessionRecorder : MonoBehaviour
    {
        readonly ScoreCalculator m_Calculator = ScoreCalculator.Default();

        SessionLog m_Log;
        bool m_Active;
        bool m_Holding;
        bool m_PinPulled;
        bool m_Spraying;
        AgentSuitability m_HeldSuitability;
        TechniqueChanged m_Technique;

        public SessionLog CurrentLog => m_Log;

        void OnEnable()
        {
            EventBus.Subscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Subscribe<AlarmRaised>(OnAlarm);
            EventBus.Subscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Subscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Subscribe<PinPulled>(OnPin);
            EventBus.Subscribe<SqueezeChanged>(OnSqueeze);
            EventBus.Subscribe<SprayStateChanged>(OnSpray);
            EventBus.Subscribe<TechniqueChanged>(OnTechnique);
            EventBus.Subscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Subscribe<MistakeRecorded>(OnMistake);
            EventBus.Subscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Subscribe<SessionEnded>(OnEnded);
            EventBus.Subscribe<PowerCut>(OnPowerCut);
            EventBus.Subscribe<SessionReset>(OnReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarm);
            EventBus.Unsubscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Unsubscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Unsubscribe<PinPulled>(OnPin);
            EventBus.Unsubscribe<SqueezeChanged>(OnSqueeze);
            EventBus.Unsubscribe<SprayStateChanged>(OnSpray);
            EventBus.Unsubscribe<TechniqueChanged>(OnTechnique);
            EventBus.Unsubscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Unsubscribe<MistakeRecorded>(OnMistake);
            EventBus.Unsubscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Unsubscribe<SessionEnded>(OnEnded);
            EventBus.Unsubscribe<PowerCut>(OnPowerCut);
            EventBus.Unsubscribe<SessionReset>(OnReset);
        }

        void OnPowerCut(PowerCut e)
        {
            if (m_Active) m_Log.PowerCut = true;
        }

        void OnReset(SessionReset e)
        {
            m_Active = false;
            m_Holding = m_PinPulled = m_Spraying = false;
            m_Technique = default;
        }

        void OnScenarioStarted(ScenarioStarted e)
        {
            m_Log = new SessionLog
            {
                Title = e.Title,
                FireClass = e.FireClass,
                Mode = e.Mode,
                Trainee = e.Trainee,
                StartedUtc = DateTime.UtcNow
            };
            m_Active = true;
        }

        void OnAlarm(AlarmRaised e)
        {
            if (!m_Active || m_Log.AlarmRaised) return;
            m_Log.AlarmRaised = true;
            m_Log.AlarmBeforeSpray = !m_Log.HasSprayed;
        }

        void OnPickedUp(ExtinguisherPickedUp e)
        {
            m_Holding = true;
            m_PinPulled = false;
            m_HeldSuitability = e.Suitability;
            if (m_Active) m_Log.Pickups++;
        }

        void OnReturned(ExtinguisherReturned e)
        {
            m_Holding = false;
            m_PinPulled = false;
        }

        void OnPin(PinPulled e) => m_PinPulled = true;

        void OnSqueeze(SqueezeChanged e)
        {
            if (m_Active && e.Held && m_Holding && !m_PinPulled)
                m_Log.PinOrderMistakes++;
        }

        void OnSpray(SprayStateChanged e)
        {
            m_Spraying = e.Spraying;
            if (!m_Active || !e.Spraying) return;

            if (!m_Log.HasSprayed)
            {
                m_Log.HasSprayed = true;
                m_Log.FirstSprayOnBase = m_Technique.Aim == AimQuality.OnBase;
            }

            switch (m_HeldSuitability)
            {
                case AgentSuitability.Suitable: m_Log.UsedSuitable = true; break;
                case AgentSuitability.Poor: m_Log.UsedPoor = true; break;
                case AgentSuitability.Dangerous: m_Log.UsedDangerous = true; break;
            }
        }

        void OnTechnique(TechniqueChanged e) => m_Technique = e;

        void OnIntensity(FireIntensityChanged e)
        {
            if (m_Active) m_Log.PeakIntensity = Mathf.Max(m_Log.PeakIntensity, e.Intensity);
        }

        void OnMistake(MistakeRecorded e)
        {
            if (m_Active) m_Log.Mistakes.Add(e.Title);
        }

        void OnFlareUp(FireFlaredUp e)
        {
            if (m_Active) m_Log.Mistakes.Add($"Flare-up: {e.Reason}");
        }

        void Update()
        {
            if (!m_Active || !m_Spraying)
                return;

            float dt = Time.deltaTime;
            m_Log.SprayTime += dt;
            if (m_Technique.Aim == AimQuality.OnBase) m_Log.OnBaseTime += dt;
            if (m_Technique.Sweep == SweepQuality.Good)
            {
                m_Log.SweepTime += dt;
                if (m_Log.FirstSweepAt < 0f) m_Log.FirstSweepAt = m_Log.SprayTime;
            }
            if (m_Technique.Band == DistanceBand.TooClose) m_Log.TooCloseTime += dt;
            else if (m_Technique.Band == DistanceBand.TooFar) m_Log.TooFarTime += dt;
        }

        void OnEnded(SessionEnded e)
        {
            if (!m_Active)
                return;
            m_Active = false;
            m_Spraying = false;
            m_Log.Outcome = e.Outcome;
            m_Log.Duration = e.Duration;

            var lines = m_Calculator.Calculate(m_Log, out int total, out int max);
            Debug.Log($"[Session] {m_Log.Title}: {e.Outcome}, score {total}/{max}");
            EventBus.Publish(new SessionScored(m_Log.Title, m_Log.FireClass, m_Log.Mode.ToString(), e.Outcome, e.Duration,
                total, max, lines, m_Log.Mistakes.ToArray()));
        }
    }
}
