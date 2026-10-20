using System.Collections.Generic;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Owns the session flow as a state machine (State pattern). Each state is its
    /// own class that decides how to react to events; the manager only routes events
    /// to the current state and announces every change on the EventBus.
    /// </summary>
    public sealed class SimulationManager : MonoBehaviour
    {
        [SerializeField] SimulationStateId initialState = SimulationStateId.Scanning;

        readonly Dictionary<SimulationStateId, SimulationState> m_States =
            new Dictionary<SimulationStateId, SimulationState>();

        SimulationState m_Current;
        bool m_Constructed;

        public SimulationStateId CurrentState => m_Current != null ? m_Current.Id : SimulationStateId.None;

        /// <summary>Seconds spent in Running; frozen while paused or repositioning.</summary>
        public float RunningTime { get; private set; }

        public SessionOutcome Outcome { get; private set; }

        // Facts the states share.
        internal bool HasPlane { get; private set; }
        internal bool IsTracking { get; private set; }
        internal bool HasScene { get; private set; }

        /// <summary>Where to go once a placement is confirmed.</summary>
        internal SimulationStateId AfterPlacement { get; set; } = SimulationStateId.Running;

        public void Construct()
        {
            if (m_Constructed)
                return;

            Register(new ScanningState(this));
            Register(new PlacingState(this));
            Register(new ConfirmingState(this));
            Register(new RunningState(this));
            Register(new PausedState(this));
            Register(new CompletedState(this));
            m_Constructed = true;
        }

        public void Begin()
        {
            Construct();
            ChangeState(initialState);
        }

        void Register(SimulationState state) => m_States[state.Id] = state;

        bool m_Transitioning;
        SimulationStateId? m_Queued;

        internal void ChangeState(SimulationStateId next)
        {
            // A state may change state again from Enter(); queue it so listeners
            // always see the transitions in the order they happened.
            if (m_Transitioning)
            {
                m_Queued = next;
                return;
            }

            m_Transitioning = true;
            try
            {
                Transition(next);
                while (m_Queued.HasValue)
                {
                    var queued = m_Queued.Value;
                    m_Queued = null;
                    Transition(queued);
                }
            }
            finally
            {
                m_Transitioning = false;
            }
        }

        void Transition(SimulationStateId next)
        {
            if (!m_States.TryGetValue(next, out var nextState))
            {
                Debug.LogError($"[Simulation] No state registered for {next}");
                return;
            }

            var previous = CurrentState;
            if (previous == next)
                return;

            m_Current?.Exit();
            m_Current = nextState;
            Debug.Log($"[Simulation] {previous} -> {next}");
            EventBus.Publish(new SimulationStateChanged(previous, next));
            m_Current.Enter(previous);
        }

        internal void AddRunningTime(float seconds) => RunningTime += seconds;

        void Update() => m_Current?.Tick(Time.deltaTime);

        void OnEnable()
        {
            EventBus.Subscribe<PlaneDetected>(OnPlaneDetected);
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<PlacementConfirmed>(OnPlacementConfirmed);
            EventBus.Subscribe<RepositionRequested>(OnRepositionRequested);
            EventBus.Subscribe<TrackingLost>(OnTrackingLost);
            EventBus.Subscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Subscribe<ResumeRequested>(OnResumeRequested);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlaneDetected>(OnPlaneDetected);
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<PlacementConfirmed>(OnPlacementConfirmed);
            EventBus.Unsubscribe<RepositionRequested>(OnRepositionRequested);
            EventBus.Unsubscribe<TrackingLost>(OnTrackingLost);
            EventBus.Unsubscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Unsubscribe<ResumeRequested>(OnResumeRequested);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
        }

        void OnPlaneDetected(PlaneDetected e)
        {
            HasPlane = true;
            m_Current?.OnPlaneDetected();
        }

        void OnScenePlaced(ScenePlaced e)
        {
            HasScene = true;
            m_Current?.OnScenePlaced(e);
        }

        void OnPlacementConfirmed(PlacementConfirmed e) => m_Current?.OnPlacementConfirmed();
        void OnRepositionRequested(RepositionRequested e) => m_Current?.OnRepositionRequested();

        void OnTrackingLost(TrackingLost e)
        {
            IsTracking = false;
            m_Current?.OnTrackingLost();
        }

        void OnTrackingRestored(TrackingRestored e)
        {
            IsTracking = true;
            m_Current?.OnTrackingRestored();
        }

        void OnResumeRequested(ResumeRequested e) => m_Current?.OnResumeRequested();

        void OnSessionEnded(SessionEnded e)
        {
            Outcome = e.Outcome;
            m_Current?.OnSessionEnded();
        }
    }
}
