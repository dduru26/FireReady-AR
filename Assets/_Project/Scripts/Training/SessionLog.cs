using System;
using System.Collections.Generic;
using FireReady.Core;

namespace FireReady.Training
{
    /// <summary>Everything measured during one session; the scoring rules read only this.</summary>
    public sealed class SessionLog
    {
        public string Title = "Training";
        public FireClass FireClass;
        public TrainingMode Mode = TrainingMode.Guided;
        public string Trainee;
        public DateTime StartedUtc;

        public bool AlarmRaised;
        public bool AlarmBeforeSpray;
        public bool PowerCut;

        public int Pickups;
        public bool UsedSuitable;
        public bool UsedPoor;
        public bool UsedDangerous;

        public int PinOrderMistakes;
        public bool HasSprayed;
        public bool FirstSprayOnBase;

        public float SprayTime;
        public float OnBaseTime;
        public float SweepTime;
        public float TooCloseTime;
        public float TooFarTime;
        public float FirstSweepAt = -1f;

        public float PeakIntensity;
        public SessionOutcome Outcome;
        public float Duration;

        public readonly List<string> Mistakes = new List<string>();
    }
}
