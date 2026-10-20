using UnityEngine;

namespace FireReady.Core
{
    // ---- Scenario -------------------------------------------------------------

    public readonly struct ScenarioStarted : IGameEvent
    {
        public readonly string Title;
        public readonly FireClass FireClass;
        public readonly float EvacuateThreshold;
        public readonly TrainingMode Mode;
        public readonly string Trainee;

        public ScenarioStarted(string title, FireClass fireClass, float evacuateThreshold,
            TrainingMode mode = TrainingMode.Guided, string trainee = null)
        {
            Title = title;
            FireClass = fireClass;
            EvacuateThreshold = evacuateThreshold;
            Mode = mode;
            Trainee = trainee;
        }
    }

    /// <summary>The trainee chose a scenario and mode and pressed Continue.</summary>
    public readonly struct ScenarioSelected : IGameEvent
    {
        public readonly ScenarioDefinition Scenario;
        public readonly TrainingMode Mode;
        public readonly string Trainee;

        public ScenarioSelected(ScenarioDefinition scenario, TrainingMode mode, string trainee)
        {
            Scenario = scenario;
            Mode = mode;
            Trainee = trainee;
        }
    }

    public readonly struct ScenarioLoadProgress : IGameEvent
    {
        public readonly float Progress; // 0 to 1
        public ScenarioLoadProgress(float progress) => Progress = progress;
    }

    /// <summary>The scenario prefab is in memory (loaded through Addressables).</summary>
    public readonly struct ScenarioLoaded : IGameEvent
    {
        public readonly ScenarioDefinition Scenario;
        public readonly GameObject Prefab;

        public ScenarioLoaded(ScenarioDefinition scenario, GameObject prefab)
        {
            Scenario = scenario;
            Prefab = prefab;
        }
    }

    public readonly struct ScenarioLoadFailed : IGameEvent
    {
        public readonly string Error;
        public ScenarioLoadFailed(string error) => Error = error;
    }

    public readonly struct LoadingCancelled : IGameEvent { }
    public readonly struct BriefingAccepted : IGameEvent { }

    /// <summary>
    /// Everything per-session is cleared (Restart or Main menu). Systems drop the
    /// placed scene, the held extinguisher and their counters, without reloading the scene.
    /// </summary>
    public readonly struct SessionReset : IGameEvent { }

    /// <summary>The trainee switched off the power at the wall (S3).</summary>
    public readonly struct PowerCut : IGameEvent { }

    // ---- Fire -------------------------------------------------------------------

    public readonly struct FireIntensityChanged : IGameEvent
    {
        public readonly float Intensity;   // 0 to 100
        public readonly FireTrend Trend;

        public FireIntensityChanged(float intensity, FireTrend trend)
        {
            Intensity = intensity;
            Trend = trend;
        }
    }

    public readonly struct FireFlaredUp : IGameEvent
    {
        public readonly float Amount;
        public readonly string Reason;

        public FireFlaredUp(float amount, string reason)
        {
            Amount = amount;
            Reason = reason;
        }
    }

    public readonly struct FireExtinguished : IGameEvent { }

    // ---- Extinguisher -------------------------------------------------------------

    public readonly struct ExtinguisherPickedUp : IGameEvent
    {
        public readonly AgentType Agent;
        public readonly string DisplayName;
        public readonly Color BandColor;
        public readonly AgentSuitability Suitability;

        public ExtinguisherPickedUp(AgentType agent, string displayName, Color bandColor, AgentSuitability suitability)
        {
            Agent = agent;
            DisplayName = displayName;
            BandColor = bandColor;
            Suitability = suitability;
        }
    }

    public readonly struct ExtinguisherReturned : IGameEvent { }

    public readonly struct PinPulled : IGameEvent { }

    public readonly struct SprayStateChanged : IGameEvent
    {
        public readonly bool Spraying;
        public SprayStateChanged(bool spraying) => Spraying = spraying;
    }

    public readonly struct AgentRemainingChanged : IGameEvent
    {
        public readonly float Seconds;
        public readonly float Fraction;

        public AgentRemainingChanged(float seconds, float fraction)
        {
            Seconds = seconds;
            Fraction = fraction;
        }
    }

    /// <summary>Distance, aim and sweep, re-sent whenever one of them changes.</summary>
    public readonly struct TechniqueChanged : IGameEvent
    {
        public readonly float Distance;
        public readonly DistanceBand Band;
        public readonly AimQuality Aim;
        public readonly SweepQuality Sweep;

        public TechniqueChanged(float distance, DistanceBand band, AimQuality aim, SweepQuality sweep)
        {
            Distance = distance;
            Band = band;
            Aim = aim;
            Sweep = sweep;
        }
    }

    /// <summary>Something the trainee did wrong, shown as a warning and logged for the debrief.</summary>
    public readonly struct MistakeRecorded : IGameEvent
    {
        public readonly string Title;
        public readonly string Advice;

        public MistakeRecorded(string title, string advice)
        {
            Title = title;
            Advice = advice;
        }
    }

    // ---- Trainee commands (from the HUD) ----------------------------------------

    public readonly struct AlarmRaised : IGameEvent { }
    public readonly struct EvacuateRequested : IGameEvent { }
    public readonly struct PullPinRequested : IGameEvent { }
    public readonly struct ReturnExtinguisherRequested : IGameEvent { }

    public readonly struct SqueezeChanged : IGameEvent
    {
        public readonly bool Held;
        public SqueezeChanged(bool held) => Held = held;
    }

    public readonly struct RestartRequested : IGameEvent { }

    // ---- Session -------------------------------------------------------------------

    public readonly struct SessionEnded : IGameEvent
    {
        public readonly SessionOutcome Outcome;
        public readonly float Duration;

        public SessionEnded(SessionOutcome outcome, float duration)
        {
            Outcome = outcome;
            Duration = duration;
        }
    }

    /// <summary>The guided step the trainee should do next (FR-22).</summary>
    public readonly struct GuidedStepChanged : IGameEvent
    {
        public readonly int Index;      // 0-based
        public readonly int Count;
        public readonly int Completed;
        public readonly string Title;
        public readonly string Next;

        public GuidedStepChanged(int index, int count, int completed, string title, string next)
        {
            Index = index;
            Count = count;
            Completed = completed;
            Title = title;
            Next = next;
        }
    }
}

namespace FireReady.Core
{
    /// <summary>The session has been scored (FR-26, FR-27).</summary>
    public readonly struct SessionScored : IGameEvent
    {
        public readonly string Title;
        public readonly FireClass FireClass;
        public readonly string Mode;
        public readonly SessionOutcome Outcome;
        public readonly float Duration;
        public readonly int Total;
        public readonly int Max;
        public readonly ScoreLine[] Lines;
        public readonly string[] Mistakes;

        public SessionScored(string title, FireClass fireClass, string mode, SessionOutcome outcome, float duration,
            int total, int max, ScoreLine[] lines, string[] mistakes)
        {
            Title = title;
            FireClass = fireClass;
            Mode = mode;
            Outcome = outcome;
            Duration = duration;
            Total = total;
            Max = max;
            Lines = lines;
            Mistakes = mistakes;
        }
    }

    /// <summary>The scored session was written to storage (FR-28).</summary>
    public readonly struct SessionSaved : IGameEvent
    {
        public readonly bool IsNewBest;
        public readonly int SavedCount;

        public SessionSaved(bool isNewBest, int savedCount)
        {
            IsNewBest = isNewBest;
            SavedCount = savedCount;
        }
    }

    public readonly struct HistoryRequested : IGameEvent { }
    public readonly struct MainMenuRequested : IGameEvent { }
}

namespace FireReady.Core
{
    /// <summary>The caption line for whatever is audible right now (FR-24).</summary>
    public readonly struct CaptionChanged : IGameEvent
    {
        public readonly string Text;
        public CaptionChanged(string text) => Text = text;
    }
}
