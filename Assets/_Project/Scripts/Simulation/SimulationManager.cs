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
        [SerializeField] SimulationStateId initialState = SimulationStateId.MainMenu;

        readonly Dictionary<SimulationStateId, SimulationState> m_States =
            new Dictionary<SimulationStateId, SimulationState>();

        SimulationState m_Current;
        bool m_Constructed;
        bool m_Transitioning;
        SimulationStateId? m_Queued;

        public SimulationStateId CurrentState => m_Current != null ? m_Current.Id : SimulationStateId.None;

        /// <summary>Seconds spent in Running; frozen while paused or repositioning.</summary>
        public float RunningTime { get; private set; }

        public SessionOutcome Outcome { get; private set; }

        /// <summary>The scenario, mode and trainee chosen on the Scenario select screen.</summary>
        public ScenarioSelected? Selection { get; private set; }

        // Facts the states share.
        internal bool HasPlane { get; private set; }
        internal bool IsTracking { get; private set; }
        internal bool HasScene { get; private set; }

        /// <summary>Where to go once a placement is confirmed.</summary>
        internal SimulationStateId AfterPlacement { get; set; } = SimulationStateId.Briefing;

        public void Construct()
        {
            if (m_Constructed)
                return;

            Register(new MainMenuState(this));
            Register(new LoadingState(this));
            Register(new ScanningState(this));
            Register(new PlacingState(this));
            Register(new ConfirmingState(this));
            Register(new BriefingState(this));
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

        /// <summary>Clears everything that belongs to one session and tells every system to do the same.</summary>
        internal void ResetSession()
        {
            RunningTime = 0f;
            Outcome = SessionOutcome.None;
            HasScene = false;
            AfterPlacement = SimulationStateId.Briefing;
            EventBus.Publish(new SessionReset());
        }

        internal void StartLoading(ScenarioSelected selection)
        {
            Selection = selection;
            ChangeState(SimulationStateId.Loading);
        }

        internal void AddRunningTime(float seconds) => RunningTime += seconds;

        void Update() => m_Current?.Tick(Time.deltaTime);

        // ---- Event routing ---------------------------------------------------------------

        void OnEnable()
        {
            EventBus.Subscribe<ScenarioSelected>(OnScenarioSelected);
            EventBus.Subscribe<ScenarioLoaded>(OnScenarioLoaded);
            EventBus.Subscribe<ScenarioLoadFailed>(OnScenarioLoadFailed);
            EventBus.Subscribe<LoadingCancelled>(OnLoadingCancelled);
            EventBus.Subscribe<PlaneDetected>(OnPlaneDetected);
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<PlacementConfirmed>(OnPlacementConfirmed);
            EventBus.Subscribe<RepositionRequested>(OnRepositionRequested);
            EventBus.Subscribe<BriefingAccepted>(OnBriefingAccepted);
            EventBus.Subscribe<TrackingLost>(OnTrackingLost);
            EventBus.Subscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Subscribe<ResumeRequested>(OnResumeRequested);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
            EventBus.Subscribe<RestartRequested>(OnRestartRequested);
            EventBus.Subscribe<MainMenuRequested>(OnMainMenuRequested);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenarioSelected>(OnScenarioSelected);
            EventBus.Unsubscribe<ScenarioLoaded>(OnScenarioLoaded);
            EventBus.Unsubscribe<ScenarioLoadFailed>(OnScenarioLoadFailed);
            EventBus.Unsubscribe<LoadingCancelled>(OnLoadingCancelled);
            EventBus.Unsubscribe<PlaneDetected>(OnPlaneDetected);
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<PlacementConfirmed>(OnPlacementConfirmed);
            EventBus.Unsubscribe<RepositionRequested>(OnRepositionRequested);
            EventBus.Unsubscribe<BriefingAccepted>(OnBriefingAccepted);
            EventBus.Unsubscribe<TrackingLost>(OnTrackingLost);
            EventBus.Unsubscribe<TrackingRestored>(OnTrackingRestored);
            EventBus.Unsubscribe<ResumeRequested>(OnResumeRequested);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
            EventBus.Unsubscribe<RestartRequested>(OnRestartRequested);
            EventBus.Unsubscribe<MainMenuRequested>(OnMainMenuRequested);
        }

        void OnScenarioSelected(ScenarioSelected e) => m_Current?.OnScenarioSelected(e);
        void OnScenarioLoaded(ScenarioLoaded e) => m_Current?.OnScenarioLoaded();
        void OnScenarioLoadFailed(ScenarioLoadFailed e) => m_Current?.OnScenarioLoadFailed();
        void OnLoadingCancelled(LoadingCancelled e) => m_Current?.OnLoadingCancelled();

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
        void OnBriefingAccepted(BriefingAccepted e) => m_Current?.OnBriefingAccepted();

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

        void OnRestartRequested(RestartRequested e) => m_Current?.OnRestartRequested();
        void OnMainMenuRequested(MainMenuRequested e) => m_Current?.OnMainMenuRequested();
    }
}
