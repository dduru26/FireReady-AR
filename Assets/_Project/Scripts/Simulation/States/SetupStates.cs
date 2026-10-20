using FireReady.Core;

namespace FireReady.Simulation
{
    /// <summary>Waiting for ARKit to find the floor.</summary>
    internal sealed class ScanningState : SimulationState
    {
        public ScanningState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Scanning;

        public override void Enter(SimulationStateId from)
        {
            if (Machine.HasPlane)
                Machine.ChangeState(SimulationStateId.Placing);
        }

        public override void OnPlaneDetected() => Machine.ChangeState(SimulationStateId.Placing);
    }

    /// <summary>The reticle is live; a tap or "Place here" anchors the scene.</summary>
    internal sealed class PlacingState : SimulationState
    {
        public PlacingState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Placing;

        public override void OnScenePlaced(ScenePlaced e) => Machine.ChangeState(SimulationStateId.Confirming);
    }

    /// <summary>The scene is anchored; the trainee confirms or moves it.</summary>
    internal sealed class ConfirmingState : SimulationState
    {
        public ConfirmingState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Confirming;

        public override void OnPlacementConfirmed() => Machine.ChangeState(Machine.AfterPlacement);
        public override void OnRepositionRequested() => Machine.ChangeState(SimulationStateId.Placing);
    }
}
