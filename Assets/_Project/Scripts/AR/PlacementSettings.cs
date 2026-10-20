using UnityEngine;

namespace FireReady.AR
{
    /// <summary>Tuning values for placement, kept out of code so they can be tweaked in the Inspector.</summary>
    [CreateAssetMenu(menuName = "FireReady/Placement Settings", fileName = "PlacementSettings")]
    public sealed class PlacementSettings : ScriptableObject
    {
        [Tooltip("Smallest side, in metres, the floor plane must have before the scene can be placed (FR-08).")]
        [Min(0.1f)] public float minimumPlaneSide = 1.0f;

        [Tooltip("Only report a new area size when it changes by at least this much (metres).")]
        [Min(0.01f)] public float areaReportStep = 0.1f;

        [Tooltip("Ignore a double-tap that comes this soon after placing, so the placing tap is not read as 'move'.")]
        [Min(0f)] public float repositionCooldown = 0.6f;
    }
}
