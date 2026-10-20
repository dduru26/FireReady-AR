using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>S1: burning paper in a waste bin (Class A).</summary>
    public sealed class ClassAFire : FireHazard
    {
        public override FireClass FireClass => FireClass.ClassA;

        public override AgentResponse ResponseTo(AgentType agent)
        {
            switch (agent)
            {
                case AgentType.Water:       return AgentResponse.Good(1f);
                case AgentType.Foam:        return AgentResponse.Good(1f);
                case AgentType.DryPowder:   return AgentResponse.Good(0.85f);
                case AgentType.WetChemical: return AgentResponse.Good(0.7f);
                case AgentType.CO2:
                    return AgentResponse.Poor(0.3f, "CO2 does little to burning paper. Use water or foam (red or cream band).");
                default: return AgentResponse.Poor(0f, null);
            }
        }
    }
}
