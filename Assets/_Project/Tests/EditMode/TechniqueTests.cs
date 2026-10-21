using FireReady.Core;
using FireReady.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FireReady.Tests
{
    public class TechniqueTests
    {
        [TestCase(0.8f, DistanceBand.TooClose)]
        [TestCase(1.2f, DistanceBand.InRange)]
        [TestCase(2.5f, DistanceBand.InRange)]
        [TestCase(3.5f, DistanceBand.TooFar)]
        public void DistanceBands(float distance, DistanceBand expected) =>
            Assert.AreEqual(expected, TechniqueEvaluator.Band(distance));

        static Ray AimAt(Vector3 from, Vector3 target) => new Ray(from, (target - from).normalized);

        [Test]
        public void AimingAtTheBase_IsOnBase()
        {
            var fireBase = new Vector3(0f, 0.4f, 2f);
            var ray = AimAt(new Vector3(0f, 1.4f, 0f), fireBase);
            Assert.AreEqual(AimQuality.OnBase, TechniqueEvaluator.Aim(ray, fireBase, 0.18f, 0.6f));
        }

        [Test]
        public void AimingAtTheFlames_IsTooHigh()
        {
            var fireBase = new Vector3(0f, 0.4f, 2f);
            var ray = AimAt(new Vector3(0f, 1.4f, 0f), fireBase + Vector3.up * 0.5f);
            Assert.AreEqual(AimQuality.TooHigh, TechniqueEvaluator.Aim(ray, fireBase, 0.18f, 0.6f));
        }

        [Test]
        public void AimingSideways_IsOffTarget()
        {
            var fireBase = new Vector3(0f, 0.4f, 2f);
            var ray = AimAt(new Vector3(0f, 1.4f, 0f), fireBase + Vector3.right * 1.5f);
            Assert.AreEqual(AimQuality.OffTarget, TechniqueEvaluator.Aim(ray, fireBase, 0.18f, 0.6f));
        }

        [Test]
        public void TwoSwingsInsideTheWindow_CountAsSweeping()
        {
            var sweep = new SweepDetector();
            Assert.AreEqual(SweepQuality.NotYet, sweep.Update(0f, 0f));
            sweep.Update(12f, 0.4f);   // swing right
            sweep.Update(-12f, 0.9f);  // reverse 1
            Assert.AreEqual(SweepQuality.Good, sweep.Update(12f, 1.4f)); // reverse 2
        }

        [Test]
        public void HoldingStill_IsNotSweeping()
        {
            var sweep = new SweepDetector();
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(SweepQuality.NotYet, sweep.Update(2f * Mathf.Sin(i), i * 0.1f));
        }
    }
}
