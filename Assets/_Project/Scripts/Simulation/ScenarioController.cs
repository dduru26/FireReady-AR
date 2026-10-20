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
        Camera m_Camera;

        FireHazard m_Fire;
        string m_Title = "Training";
        bool m_Running;
        bool m_Started;
        bool m_Ended;

        public FireHazard Fire => m_Fire;

        public void Construct(ExtinguisherController extinguishers, SimulationManager simulation, Camera arCamera)
        {
            m_Extinguishers = extinguishers;
            m_Simulation = simulation;
            m_Camera = arCamera;
        }

        void OnEnable()
        {
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<EvacuateRequested>(OnEvacuate);
            EventBus.Subscribe<ScreenTapped>(OnScreenTapped);
            EventBus.Subscribe<SessionReset>(OnSessionReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<EvacuateRequested>(OnEvacuate);
            EventBus.Unsubscribe<ScreenTapped>(OnScreenTapped);
            EventBus.Unsubscribe<SessionReset>(OnSessionReset);
        }

        void OnSessionReset(SessionReset e)
        {
            m_Fire = null;
            m_Started = false;
            m_Ended = false;
            m_Running = false;
        }

        /// <summary>Taps on scene objects other than extinguishers, such as the wall switch in S3.</summary>
        void OnScreenTapped(ScreenTapped e)
        {
            if (!m_Running || m_Ended || m_Camera == null)
                return;
            if (Physics.Raycast(m_Camera.ScreenPointToRay(e.Position), out var hit, 6f))
            {
                var power = hit.collider.GetComponentInParent<PowerSwitch>();
                if (power != null)
                    power.SwitchOff();
            }
        }

        void OnScenePlaced(ScenePlaced e)
        {
            if (e.IsReposition || e.SceneRoot == null)
                return;

            m_Fire = e.SceneRoot.GetComponentInChildren<FireHazard>(true);
            var selection = m_Simulation.Selection;
            if (selection.HasValue && selection.Value.Scenario != null)
                m_Title = selection.Value.Scenario.title;
            else
            {
                var info = e.SceneRoot.GetComponentInChildren<ScenarioInfo>(true);
                if (info != null) m_Title = info.Title;
            }

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
                var selection = m_Simulation.Selection;
                var mode = selection.HasValue ? selection.Value.Mode : TrainingMode.Guided;
                var trainee = selection.HasValue ? selection.Value.Trainee : null;
                EventBus.Publish(new ScenarioStarted(m_Title, m_Fire.FireClass, m_Fire.EvacuateThreshold, mode, trainee));
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
