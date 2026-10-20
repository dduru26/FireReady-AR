using FireReady.Core;

namespace FireReady.Training
{
    /// <summary>
    /// One scoring criterion (Strategy pattern). The calculator runs every rule over
    /// the same session log, so criteria can be added or re-weighted without touching
    /// the others.
    /// </summary>
    public interface IScoringRule
    {
        string Name { get; }
        int MaxPoints { get; }
        ScoreLine Evaluate(SessionLog log);
    }
}
