using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>Data for one extinguisher type (agent, colour band, capacity).</summary>
    [CreateAssetMenu(menuName = "FireReady/Extinguisher", fileName = "Extinguisher")]
    public sealed class ExtinguisherDefinition : ScriptableObject
    {
        public AgentType agent;
        public string displayName = "Water";
        public Color bandColor = Color.red;
        [Tooltip("Seconds of continuous spray in a full extinguisher.")]
        [Min(1f)] public float capacitySeconds = 10f;
        public Color sprayColor = new Color(0.8f, 0.9f, 1f, 0.8f);
        [Tooltip("Colour of the puffs where the spray lands.")]
        public Color impactColor = new Color(1f, 1f, 1f, 0.6f);
    }
}
