using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Pure functions that judge the trainee's distance and aim. No Unity objects,
    /// so the rules can be unit-tested in edit mode.
    /// </summary>
    public static class TechniqueEvaluator
    {
        public const float MinDistance = 1.2f;
        public const float MaxDistance = 3.0f;

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        public static DistanceBand Band(float distance) =>
            distance < MinDistance ? DistanceBand.TooClose :
            distance > MaxDistance ? DistanceBand.TooFar :
            DistanceBand.InRange;

        /// <summary>Judges where the aim ray passes the fire: base, too high, too low, or off.</summary>
        public static AimQuality Aim(Ray aim, Vector3 basePosition, float baseRadius, float flameHeight)
        {
            var dir = aim.direction.normalized;
            var toBase = basePosition - aim.origin;
            var dirFlat = new Vector2(dir.x, dir.z);
            var toBaseFlat = new Vector2(toBase.x, toBase.z);

            float t;
            if (dirFlat.sqrMagnitude < 1e-6f)
            {
                // Looking straight down: use where the ray reaches the base height.
                if (Mathf.Abs(dir.y) < 1e-6f) return AimQuality.OffTarget;
                t = toBase.y / dir.y;
            }
            else
            {
                // Closest approach to the fire in the horizontal plane.
                t = Vector2.Dot(toBaseFlat, dirFlat) / dirFlat.sqrMagnitude;
            }

            if (t <= 0f)
                return AimQuality.OffTarget;

            var point = aim.origin + dir * t;
            float lateral = HorizontalDistance(point, basePosition);
            float vertical = point.y - basePosition.y;

            if (lateral > baseRadius + 0.25f) return AimQuality.OffTarget;
            if (vertical < -0.25f) return AimQuality.TooLow;
            if (vertical > flameHeight + 0.35f) return AimQuality.OffTarget;
            if (vertical > 0.18f) return AimQuality.TooHigh;
            return AimQuality.OnBase;
        }

        public static float AimFactor(AimQuality aim) => aim switch
        {
            AimQuality.OnBase => 1f,
            AimQuality.TooLow => 0.4f,
            AimQuality.TooHigh => 0.25f,
            _ => 0f
        };

        public static float DistanceFactor(DistanceBand band) => band switch
        {
            DistanceBand.InRange => 1f,
            DistanceBand.TooClose => 0.7f,
            _ => 0f // out of reach
        };

        public static float SweepFactor(SweepQuality sweep) => sweep == SweepQuality.Good ? 1f : 0.55f;
    }
}
