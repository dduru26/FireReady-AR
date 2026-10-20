namespace FireReady.Core
{
    /// <summary>The states of one training session (see SimulationManager).</summary>
    public enum SimulationStateId
    {
        None,
        MainMenu,    // Start, scenario select and history screens
        Loading,     // the scenario's Addressables content is loading
        Scanning,    // looking for the floor
        Placing,     // aiming the reticle and placing the scene
        Confirming,  // checking the placement before starting
        Briefing,    // reading the scenario brief before the clock starts
        Running,     // the scenario is live
        Paused,      // tracking was lost; everything is frozen
        Completed
    }
}
