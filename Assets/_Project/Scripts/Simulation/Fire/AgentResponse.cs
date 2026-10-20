using FireReady.Core;

namespace FireReady.Simulation
{
    /// <summary>How a fire reacts to one extinguishing agent.</summary>
    public readonly struct AgentResponse
    {
        public readonly AgentSuitability Suitability;
        /// <summary>Multiplier on the suppression rate (0 = no effect).</summary>
        public readonly float Effectiveness;
        /// <summary>Shown to the trainee when the agent is wrong for this fire.</summary>
        public readonly string Warning;

        public AgentResponse(AgentSuitability suitability, float effectiveness, string warning = null)
        {
            Suitability = suitability;
            Effectiveness = effectiveness;
            Warning = warning;
        }

        public static AgentResponse Good(float effectiveness = 1f) =>
            new AgentResponse(AgentSuitability.Suitable, effectiveness);

        public static AgentResponse Poor(float effectiveness, string warning) =>
            new AgentResponse(AgentSuitability.Poor, effectiveness, warning);

        public static AgentResponse Dangerous(string warning) =>
            new AgentResponse(AgentSuitability.Dangerous, 0f, warning);
    }
}
