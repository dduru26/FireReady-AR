namespace FireReady.Core
{
    /// <summary>The states of one training session (see SimulationManager).</summary>
    public enum SimulationStateId
    {
        None,
        Scanning,    // looking for the floor
        Placing,     // aiming the reticle and placing the scene
        Confirming,  // checking the placement before starting
        Running,     // the scenario is live
        Paused,      // tracking was lost; everything is frozen
        Completed
    }
}
