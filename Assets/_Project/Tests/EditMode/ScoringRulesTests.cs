using FireReady.Core;
using FireReady.Training;
using NUnit.Framework;

namespace FireReady.Tests
{
    public class ScoringRulesTests
    {
        static SessionLog PerfectRun() => new SessionLog
        {
            FireClass = FireClass.ClassA,
            AlarmRaised = true,
            AlarmBeforeSpray = true,
            HasSprayed = true,
            UsedSuitable = true,
            FirstSprayOnBase = true,
            SprayTime = 6f,
            OnBaseTime = 6f,
            SweepTime = 6f,
            FirstSweepAt = 0.5f,
            Outcome = SessionOutcome.Extinguished
        };

        [Test]
        public void PerfectRun_Scores100()
        {
            ScoreCalculator.Default().Calculate(PerfectRun(), out int total, out int max);
            Assert.AreEqual(100, max);
            Assert.AreEqual(100, total);
        }

        [Test]
        public void DangerousAgent_GetsNoChoicePoints()
        {
            var log = PerfectRun();
            log.UsedDangerous = true;
            var line = new ExtinguisherChoiceRule().Evaluate(log);
            Assert.AreEqual(0, line.Points);
            Assert.IsNotEmpty(line.Feedback);
        }

        [Test]
        public void SwitchingFromPoorToSuitable_GetsHalfChoicePoints()
        {
            var log = PerfectRun();
            log.UsedPoor = true;
            Assert.AreEqual(15, new ExtinguisherChoiceRule().Evaluate(log).Points);
        }

        [Test]
        public void ElectricalFire_NeedsPowerCutForFullSafetyPoints()
        {
            var log = PerfectRun();
            log.FireClass = FireClass.Electrical;
            Assert.AreEqual(5, new SafetyActionsRule().Evaluate(log).Points);
            log.PowerCut = true;
            Assert.AreEqual(10, new SafetyActionsRule().Evaluate(log).Points);
        }

        [Test]
        public void SqueezingBeforePin_LosesPassOrderPoints()
        {
            var log = PerfectRun();
            log.PinOrderMistakes = 1;
            Assert.AreEqual(8, new PassOrderRule().Evaluate(log).Points);
        }

        [Test]
        public void TooClose_LosesDistancePointsWithFeedback()
        {
            var log = PerfectRun();
            log.TooCloseTime = 4f;
            var line = new DistanceRule().Evaluate(log);
            Assert.AreEqual(9, line.Points);
            StringAssert.Contains("Too close for 4 s", line.Feedback);
        }

        [Test]
        public void EvacuatingEarly_ScoresPartialOutcome()
        {
            var log = PerfectRun();
            log.Outcome = SessionOutcome.EvacuatedEarly;
            Assert.AreEqual(4, new OutcomeRule().Evaluate(log).Points);
        }

        [Test]
        public void NoSpraying_ScoresZeroOnTechniqueRules()
        {
            var log = new SessionLog { Outcome = SessionOutcome.OutOfControl };
            Assert.AreEqual(0, new PassOrderRule().Evaluate(log).Points);
            Assert.AreEqual(0, new DistanceRule().Evaluate(log).Points);
            Assert.AreEqual(0, new AimAndSweepRule().Evaluate(log).Points);
        }
    }
}
