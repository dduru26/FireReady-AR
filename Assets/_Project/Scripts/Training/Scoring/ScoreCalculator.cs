using System.Collections.Generic;
using FireReady.Core;

namespace FireReady.Training
{
    /// <summary>Runs a set of scoring rules over a session log.</summary>
    public sealed class ScoreCalculator
    {
        readonly IReadOnlyList<IScoringRule> m_Rules;

        public ScoreCalculator(IReadOnlyList<IScoringRule> rules) => m_Rules = rules;

        /// <summary>The six criteria shown in the debrief, 100 points in total.</summary>
        public static ScoreCalculator Default() => new ScoreCalculator(new IScoringRule[]
        {
            new ExtinguisherChoiceRule(),
            new SafetyActionsRule(),
            new PassOrderRule(),
            new DistanceRule(),
            new AimAndSweepRule(),
            new OutcomeRule()
        });

        public ScoreLine[] Calculate(SessionLog log, out int total, out int max)
        {
            var lines = new ScoreLine[m_Rules.Count];
            total = 0;
            max = 0;
            for (int i = 0; i < m_Rules.Count; i++)
            {
                lines[i] = m_Rules[i].Evaluate(log);
                total += lines[i].Points;
                max += lines[i].Max;
            }
            return lines;
        }
    }
}
