using FireReady.Core;

namespace FireReady.Simulation
{
    /// <summary>Start, Scenario select and History screens. Waits for a scenario choice.</summary>
    internal sealed class MainMenuState : SimulationState
    {
        public MainMenuState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.MainMenu;

        public override void OnScenarioSelected(ScenarioSelected e) => Machine.StartLoading(e);
        public override void OnMainMenuRequested() { }
        public override void OnRestartRequested() { }
    }

    /// <summary>The scenario content is loading through Addressables.</summary>
    internal sealed class LoadingState : SimulationState
    {
        public LoadingState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Loading;

        public override void OnScenarioLoaded() => Machine.ChangeState(SimulationStateId.Scanning);
        public override void OnScenarioLoadFailed() => OnMainMenuRequested();
        public override void OnLoadingCancelled() => OnMainMenuRequested();
        public override void OnRestartRequested() { }
    }

    /// <summary>The scene is placed; the trainee reads the brief before the clock starts.</summary>
    internal sealed class BriefingState : SimulationState
    {
        public BriefingState(SimulationManager machine) : base(machine) { }
        public override SimulationStateId Id => SimulationStateId.Briefing;

        public override void OnBriefingAccepted() => Machine.ChangeState(SimulationStateId.Running);

        public override void OnRepositionRequested()
        {
            Machine.AfterPlacement = SimulationStateId.Briefing;
            Machine.ChangeState(SimulationStateId.Placing);
        }
    }
}
