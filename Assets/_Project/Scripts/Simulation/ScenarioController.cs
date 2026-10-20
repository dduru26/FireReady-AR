using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Runs the active scenario: finds the fire in the placed scene, advances it only
    /// while the session is Running (so a pause freezes it), and ends the session on
    /// extinguish, out-of-control, injury or evacuation (FR-20, FR-21).
    /// </summary>
    public sealed class ScenarioController : MonoBehaviour
    {
        ExtinguisherController m_Extinguishers;
        SimulationManager m_Simulation;

        FireHazard m_Fire;
        string m_Title = "Training";
        bool m_Running;
        bool m_Started;
        bool m_Ended;

        public FireHazard Fire => m_Fire;

        public void Construct(ExtinguisherController extinguishers, SimulationManager simulation)
        {
            m_Extinguishers = extinguishers;
            m_Simulation = simulation;
        }

        void OnEnable()
        {
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<EvacuateRequested>(OnEvacuate);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<EvacuateRequested>(OnEvacuate);
        }

        void OnScenePlaced(ScenePlaced e)
        {
            if (e.IsReposition || e.SceneRoot == null)
                return;

            m_Fire = e.SceneRoot.GetComponentInChildren<FireHazard>(true);
            var info = e.SceneRoot.GetComponentInChildren<ScenarioInfo>(true);
            if (info != null)
                m_Title = info.Title;

            if (m_Fire == null)
                Debug.LogWarning("[Scenario] The placed scene has no FireHazard.");
            m_Extinguishers.SetScene(m_Fire, e.SceneRoot.position.y);
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            m_Running = e.Current == SimulationStateId.Running;
            if (m_Running && !m_Started && m_Fire != null)
            {
                m_Started = true;
                EventBus.Publish(new ScenarioStarted(m_Title, m_Fire.FireClass, m_Fire.EvacuateThreshold));
                m_Fire.Announce();
            }
        }

        void Update()
        {
            if (!m_Running || m_Ended || m_Fire == null)
                return;

            var outcome = m_Fire.Step(Time.deltaTime, m_Extinguishers.Contact);
            if (outcome != SessionOutcome.None)
                End(outcome);
        }

        void OnEvacuate(EvacuateRequested e)
        {
            if (!m_Running || m_Ended || m_Fire == null)
                return;

            End(m_Fire.Intensity >= m_Fire.EvacuateThreshold
                ? SessionOutcome.EvacuatedCorrectly
                : SessionOutcome.EvacuatedEarly);
        }

        void End(SessionOutcome outcome)
        {
            m_Ended = true;
            Debug.Log($"[Scenario] Ended: {outcome} after {m_Simulation.RunningTime:0.0} s");
            EventBus.Publish(new SessionEnded(outcome, m_Simulation.RunningTime));
        }
    }
}
