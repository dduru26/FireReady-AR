using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
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
