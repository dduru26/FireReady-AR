using FireReady.Core;
using UnityEngine;

namespace FireReady.Training
{
    /// <summary>
    /// Tracks the six guided steps (FR-22) from gameplay events and announces the
    /// step the trainee should do next. Steps can be done in any order; the prompt
    /// always shows the first one not yet done.
    /// </summary>
    public sealed class GuidedStepTracker : MonoBehaviour
    {
        static readonly string[] k_Titles =
        {
            "Raise the alarm",
            "Pick a suitable extinguisher",
            "Pull the pin",
            "Stand 1.2 to 3 m away, aim at the base",
            "Squeeze and sweep side to side",
            "Put it out, or evacuate if it gets too big"
        };

        const int Alarm = 0, PickSuitable = 1, Pin = 2, Position = 3, Sweep = 4, Finish = 5;

        readonly bool[] m_Done = new bool[k_Titles.Length];
        bool m_Active;
        bool m_HoldingSuitable;
        bool m_Spraying;
        TechniqueChanged m_Technique;
        int m_LastAnnounced = -1;
        int m_LastCompleted = -1;

        public int StepCount => k_Titles.Length;
        public bool IsDone(int step) => m_Done[step];

        void OnEnable()
        {
            EventBus.Subscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Subscribe<AlarmRaised>(OnAlarm);
            EventBus.Subscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Subscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Subscribe<PinPulled>(OnPin);
            EventBus.Subscribe<TechniqueChanged>(OnTechnique);
            EventBus.Subscribe<SprayStateChanged>(OnSpray);
            EventBus.Subscribe<SessionEnded>(OnEnded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarm);
            EventBus.Unsubscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Unsubscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Unsubscribe<PinPulled>(OnPin);
            EventBus.Unsubscribe<TechniqueChanged>(OnTechnique);
            EventBus.Unsubscribe<SprayStateChanged>(OnSpray);
            EventBus.Unsubscribe<SessionEnded>(OnEnded);
        }

        void OnScenarioStarted(ScenarioStarted e)
        {
            m_Active = true;
            Announce(force: true);
        }

        void OnAlarm(AlarmRaised e) => Complete(Alarm);

        void OnPickedUp(ExtinguisherPickedUp e)
        {
            m_HoldingSuitable = e.Suitability == AgentSuitability.Suitable;
            m_Done[PickSuitable] = m_HoldingSuitable;
            m_Done[Pin] = false;
            Announce();
        }

        void OnReturned(ExtinguisherReturned e)
        {
            m_HoldingSuitable = false;
            m_Done[PickSuitable] = false;
            m_Done[Pin] = false;
            Announce();
        }

        void OnPin(PinPulled e)
        {
            if (m_HoldingSuitable)
                Complete(Pin);
        }

        void OnTechnique(TechniqueChanged e)
        {
            m_Technique = e;
            if (m_HoldingSuitable && e.Band == DistanceBand.InRange && e.Aim == AimQuality.OnBase)
                Complete(Position);
            CheckSweep();
        }

        void OnSpray(SprayStateChanged e)
        {
            m_Spraying = e.Spraying;
            CheckSweep();
        }

        void CheckSweep()
        {
            if (m_Spraying && m_HoldingSuitable && m_Technique.Sweep == SweepQuality.Good)
                Complete(Sweep);
        }

        void OnEnded(SessionEnded e)
        {
            Complete(Finish);
            m_Active = false;
        }

        void Complete(int step)
        {
            if (m_Done[step])
                return;
            m_Done[step] = true;
            Announce();
        }

        void Announce(bool force = false)
        {
            if (!m_Active && !force)
                return;

            int current = -1, completed = 0;
            for (int i = 0; i < m_Done.Length; i++)
            {
                if (m_Done[i]) completed++;
                else if (current < 0) current = i;
            }

            if (!force && current == m_LastAnnounced && completed == m_LastCompleted)
                return;
            m_LastAnnounced = current;
            m_LastCompleted = completed;

            int next = -1;
            for (int i = current + 1; current >= 0 && i < m_Done.Length; i++)
            {
                if (!m_Done[i]) { next = i; break; }
            }

            string title = current >= 0 ? k_Titles[current] : "All steps done";
            string nextTitle = next >= 0 ? ShortTitle(next) : "finish";
            EventBus.Publish(new GuidedStepChanged(Mathf.Max(current, 0), m_Done.Length, completed, title, nextTitle));
        }

        static string ShortTitle(int step) => step switch
        {
            Alarm => "raise the alarm",
            PickSuitable => "pick an extinguisher",
            Pin => "pull the pin",
            Position => "aim at the base",
            Sweep => "squeeze and sweep",
            _ => "put it out"
        };
    }
}
