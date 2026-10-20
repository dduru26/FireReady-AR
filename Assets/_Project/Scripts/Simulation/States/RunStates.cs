using FireReady.Core;

namespace FireReady.Simulation
{
    /// <summary>The scenario is live and the clock is running.</summary>
    internal sealed class RunningState : SimulationState
    {
        public RunningState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Running;

        public override void Tick(float deltaTime) => Machine.AddRunningTime(deltaTime);

        public override void OnTrackingLost() => Machine.ChangeState(SimulationStateId.Paused);

        public override void OnSessionEnded() => Machine.ChangeState(SimulationStateId.Completed);

        public override void OnRepositionRequested()
        {
            // Come straight back to the running scenario once the scene is moved (FR-09).
            Machine.AfterPlacement = SimulationStateId.Running;
            Machine.ChangeState(SimulationStateId.Placing);
        }
    }

    /// <summary>
    /// Tracking was lost (FR-23). Nothing ticks while paused; the trainee resumes
    /// manually once tracking is back, so the fire never moves unseen.
    /// </summary>
    internal sealed class PausedState : SimulationState
    {
        public PausedState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Paused;

        public override void OnResumeRequested()
        {
            if (Machine.IsTracking)
                Machine.ChangeState(SimulationStateId.Running);
        }

        public override void OnRepositionRequested()
        {
            Machine.AfterPlacement = SimulationStateId.Running;
            Machine.ChangeState(SimulationStateId.Placing);
        }
    }

    /// <summary>The session is over; the completion screen takes it from here.</summary>
    internal sealed class CompletedState : SimulationState
    {
        public CompletedState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Completed;
    }
}
