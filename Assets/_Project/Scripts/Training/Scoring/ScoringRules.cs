using FireReady.Core;
using UnityEngine;

namespace FireReady.Training
{
    /// <summary>30: was the right agent used, and never a dangerous one?</summary>
    public sealed class ExtinguisherChoiceRule : IScoringRule
    {
        public string Name => "Extinguisher choice";
        public int MaxPoints => 30;

        public ScoreLine Evaluate(SessionLog log)
        {
            if (!log.HasSprayed)
                return new ScoreLine(Name, 0, MaxPoints, "You did not use an extinguisher.");
            if (log.UsedDangerous)
                return new ScoreLine(Name, 0, MaxPoints, "You used an extinguisher that is dangerous on this fire.");
            if (log.UsedPoor && log.UsedSuitable)
                return new ScoreLine(Name, 15, MaxPoints, "You switched to the right extinguisher after a poor choice.");
            if (log.UsedPoor)
                return new ScoreLine(Name, 5, MaxPoints, "That extinguisher is a poor match for this fire.");
            return new ScoreLine(Name, MaxPoints, MaxPoints);
        }
    }

    /// <summary>10: raise the alarm, ideally before tackling the fire.</summary>
    public sealed class SafetyActionsRule : IScoringRule
    {
        public string Name => "Safety actions";
        public int MaxPoints => 10;

        public ScoreLine Evaluate(SessionLog log)
        {
            if (log.FireClass == FireClass.Electrical)
            {
                // Live electrical fire: the alarm and cutting the power are worth half each.
                int points = (log.AlarmRaised ? 5 : 0) + (log.PowerCut ? 5 : 0);
                string feedback = !log.PowerCut ? "Cut the power at the wall before you tackle an electrical fire."
                                : !log.AlarmRaised ? "You never raised the alarm."
                                : null;
                return new ScoreLine(Name, points, MaxPoints, feedback);
            }

            if (!log.AlarmRaised)
                return new ScoreLine(Name, 0, MaxPoints, "You never raised the alarm.");
            if (!log.AlarmBeforeSpray)
                return new ScoreLine(Name, 6, MaxPoints, "Raise the alarm before you tackle the fire.");
            return new ScoreLine(Name, MaxPoints, MaxPoints);
        }
    }

    /// <summary>15: Pull, Aim, Squeeze in that order.</summary>
    public sealed class PassOrderRule : IScoringRule
    {
        public string Name => "PASS order";
        public int MaxPoints => 15;

        public ScoreLine Evaluate(SessionLog log)
        {
            if (!log.HasSprayed)
                return new ScoreLine(Name, 0, MaxPoints, "No spraying recorded.");

            int points = MaxPoints - 7 * log.PinOrderMistakes - (log.FirstSprayOnBase ? 0 : 4);
            string feedback = log.PinOrderMistakes > 0 ? "You squeezed before pulling the pin."
                            : !log.FirstSprayOnBase ? "Aim at the base before you squeeze."
                            : null;
            return new ScoreLine(Name, points, MaxPoints, feedback);
        }
    }

    /// <summary>15: stay 1.2 to 3 m away while spraying.</summary>
    public sealed class DistanceRule : IScoringRule
    {
        public string Name => "Distance";
        public int MaxPoints => 15;

        public ScoreLine Evaluate(SessionLog log)
        {
            if (!log.HasSprayed)
                return new ScoreLine(Name, 0, MaxPoints, "No spraying recorded.");

            float penalty = log.TooCloseTime * 1.5f + log.TooFarTime;
            int points = Mathf.RoundToInt(MaxPoints - penalty);
            string feedback = log.TooCloseTime >= 0.5f ? $"Too close for {log.TooCloseTime:0} s. Stay 1.2 to 3 m back."
                            : log.TooFarTime >= 0.5f ? $"Too far for {log.TooFarTime:0} s. The spray could not reach."
                            : null;
            return new ScoreLine(Name, points, MaxPoints, feedback);
        }
    }

    /// <summary>20: keep the spray on the base and sweep side to side.</summary>
    public sealed class AimAndSweepRule : IScoringRule
    {
        public string Name => "Aim and sweep";
        public int MaxPoints => 20;

        public ScoreLine Evaluate(SessionLog log)
        {
            if (!log.HasSprayed || log.SprayTime <= 0f)
                return new ScoreLine(Name, 0, MaxPoints, "No spraying recorded.");

            float aim = log.OnBaseTime / log.SprayTime;
            float sweep = Mathf.Min(1f, log.SweepTime / log.SprayTime * 1.25f);
            int points = Mathf.RoundToInt(10f * aim + 10f * sweep);

            string feedback = aim < 0.7f ? "Keep the spray on the base of the fire."
                            : log.FirstSweepAt < 0f ? "Sweep side to side while you spray."
                            : log.FirstSweepAt > 2f ? "Sweep started late. Sweep from the first second."
                            : sweep < 0.6f ? "Keep sweeping the whole time you spray."
                            : null;
            return new ScoreLine(Name, points, MaxPoints, feedback);
        }
    }

    /// <summary>10: how the session ended (FR-21).</summary>
    public sealed class OutcomeRule : IScoringRule
    {
        public string Name => "Outcome";
        public int MaxPoints => 10;

        public ScoreLine Evaluate(SessionLog log)
        {
            switch (log.Outcome)
            {
                case SessionOutcome.Extinguished:
                case SessionOutcome.EvacuatedCorrectly:
                    return new ScoreLine(Name, MaxPoints, MaxPoints);
                case SessionOutcome.EvacuatedEarly:
                    return new ScoreLine(Name, 4, MaxPoints, "The fire was still small enough to fight.");
                case SessionOutcome.OutOfControl:
                    return new ScoreLine(Name, 0, MaxPoints, "Evacuate once the fire passes the line.");
                case SessionOutcome.Injured:
                    return new ScoreLine(Name, 0, MaxPoints, "Never use water on live electrical equipment.");
                default:
                    return new ScoreLine(Name, 0, MaxPoints, "The session did not finish.");
            }
        }
    }
}
