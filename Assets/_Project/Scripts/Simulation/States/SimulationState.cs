using FireReady.Core;

namespace FireReady.Simulation
{
    /// <summary>
    /// Base class for one state of the session. Every event handler does nothing by
    /// default, so each state only overrides the events it cares about.
    /// </summary>
    internal abstract class SimulationState
    {
        protected readonly SimulationManager Machine;

        protected SimulationState(SimulationManager machine) => Machine = machine;

        public abstract SimulationStateId Id { get; }

        public virtual void Enter(SimulationStateId from) { }
        public virtual void Exit() { }
        public virtual void Tick(float deltaTime) { }

        public virtual void OnPlaneDetected() { }
        public virtual void OnScenePlaced(ScenePlaced e) { }
        public virtual void OnPlacementConfirmed() { }
        public virtual void OnRepositionRequested() { }
        public virtual void OnTrackingLost() { }
        public virtual void OnTrackingRestored() { }
        public virtual void OnResumeRequested() { }
        public virtual void OnSessionEnded() { }
    }
}
