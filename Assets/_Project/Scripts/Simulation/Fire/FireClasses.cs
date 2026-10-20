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

    /// <summary>S3: a power strip on fire, live until the power is cut.</summary>
    public sealed class ElectricalFire : FireHazard
    {
        [SerializeField] bool powerOn = true;

        public bool PowerOn => powerOn;
        public override FireClass FireClass => FireClass.Electrical;

        public void CutPower() => powerOn = false;

        public override AgentResponse ResponseTo(AgentType agent)
        {
            switch (agent)
            {
                case AgentType.CO2:       return AgentResponse.Good(1f);
                case AgentType.DryPowder: return AgentResponse.Good(0.85f);
                case AgentType.Water:
                case AgentType.Foam:
                    return powerOn
                        ? AgentResponse.Dangerous("Water conducts electricity. Never use it on live equipment. Use CO2 (black band).")
                        : AgentResponse.Poor(0.5f, "With the power off this works, but CO2 (black band) is the right choice.");
                case AgentType.WetChemical:
                    return AgentResponse.Poor(0.3f, "Wet chemical is for cooking oil. Use CO2 (black band).");
                default: return AgentResponse.Poor(0f, null);
            }
        }

        // Live equipment keeps feeding the fire.
        protected override float GrowthRate() => base.GrowthRate() * (powerOn ? 1.3f : 1f);

        protected override SessionOutcome OnDangerousAgent(AgentType agent, float deltaTime) => SessionOutcome.Injured;
    }
}
