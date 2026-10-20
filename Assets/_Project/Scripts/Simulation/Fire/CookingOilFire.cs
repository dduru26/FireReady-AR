using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>S2: a pan of burning cooking oil (Class F).</summary>
    public sealed class CookingOilFire : FireHazard
    {
        [SerializeField] float waterFlareUp = 30f;
        [SerializeField] float flareUpCooldown = 2f;

        float m_LastFlareUp = float.NegativeInfinity;

        public override FireClass FireClass => FireClass.ClassF;

        public override AgentResponse ResponseTo(AgentType agent)
        {
            switch (agent)
            {
                case AgentType.WetChemical: return AgentResponse.Good(1f);
                case AgentType.Water:
                    return AgentResponse.Dangerous("Water spreads burning oil. Stop and use the wet chemical extinguisher (yellow band).");
                case AgentType.Foam:
                    return AgentResponse.Poor(0.35f, "Foam is not made for cooking oil. Use wet chemical (yellow band).");
                case AgentType.DryPowder:
                    return AgentResponse.Poor(0.45f, "Powder may knock it down but the oil re-ignites. Use wet chemical (yellow band).");
                case AgentType.CO2:
                    return AgentResponse.Poor(0.3f, "CO2 can blow burning oil out of the pan. Use wet chemical (yellow band).");
                default: return AgentResponse.Poor(0f, null);
            }
        }

        protected override SessionOutcome OnDangerousAgent(AgentType agent, float deltaTime)
        {
            if (Time.time - m_LastFlareUp >= flareUpCooldown)
            {
                m_LastFlareUp = Time.time;
                FlareUp(waterFlareUp, "Water spreads burning oil");
            }
            return SessionOutcome.None;
        }
    }
}
