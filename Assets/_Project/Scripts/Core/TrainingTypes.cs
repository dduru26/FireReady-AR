namespace FireReady.Core
{
    /// <summary>Extinguishing agents, matching the UK colour bands.</summary>
    public enum AgentType
    {
        Water,        // red
        Foam,         // cream
        CO2,          // black
        DryPowder,    // blue
        WetChemical   // yellow
    }

    /// <summary>The kinds of fire the scenarios use.</summary>
    public enum FireClass
    {
        ClassA,       // paper, wood, textiles
        ClassF,       // cooking oil
        Electrical    // live electrical equipment
    }

    /// <summary>How well an agent suits the current fire.</summary>
    public enum AgentSuitability
    {
        Suitable,
        Poor,       // works badly but is not dangerous
        Dangerous   // makes things worse or hurts the trainee
    }

    public enum DistanceBand { TooClose, InRange, TooFar }

    public enum AimQuality { OffTarget, TooLow, TooHigh, OnBase }

    public enum SweepQuality { NotYet, Good }

    public enum FireTrend { Steady, Growing, Shrinking }

    public enum SessionOutcome
    {
        None,
        Extinguished,
        EvacuatedCorrectly,   // left once the fire was beyond a portable extinguisher
        EvacuatedEarly,       // left while the fire was still small
        OutOfControl,         // the fire reached 100 before anything was done
        Injured,              // e.g. water on a live electrical fire
        Abandoned
    }
}
