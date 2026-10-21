using FireReady.Core;
using FireReady.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace FireReady.Tests
{
    public class FireRulesTests
    {
        static T Make<T>() where T : FireHazard => new GameObject(typeof(T).Name).AddComponent<T>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var fire in Object.FindObjectsByType<FireHazard>(FindObjectsInactive.Include))
                Object.DestroyImmediate(fire.gameObject);
        }

        [Test]
        public void ClassA_WaterIsSuitable_CO2IsPoor()
        {
            var fire = Make<ClassAFire>();
            Assert.AreEqual(AgentSuitability.Suitable, fire.ResponseTo(AgentType.Water).Suitability);
            Assert.AreEqual(AgentSuitability.Poor, fire.ResponseTo(AgentType.CO2).Suitability);
        }

        [Test]
        public void CookingOil_WaterIsDangerous_WetChemicalIsSuitable()
        {
            var fire = Make<CookingOilFire>();
            Assert.AreEqual(AgentSuitability.Dangerous, fire.ResponseTo(AgentType.Water).Suitability);
            Assert.AreEqual(AgentSuitability.Suitable, fire.ResponseTo(AgentType.WetChemical).Suitability);
        }

        [Test]
        public void Electrical_WaterIsDangerousOnlyWhilePowered()
        {
            var fire = Make<ElectricalFire>();
            Assert.AreEqual(AgentSuitability.Dangerous, fire.ResponseTo(AgentType.Water).Suitability);
            fire.CutPower();
            Assert.AreEqual(AgentSuitability.Poor, fire.ResponseTo(AgentType.Water).Suitability);
            Assert.AreEqual(AgentSuitability.Suitable, fire.ResponseTo(AgentType.CO2).Suitability);
        }

        [Test]
        public void UnattendedFire_Grows()
        {
            var fire = Make<ClassAFire>();
            fire.Ignite(40f);
            float before = fire.Intensity;
            fire.Step(1f, SprayContact.None);
            Assert.Greater(fire.Intensity, before);
        }

        [Test]
        public void GoodSprayWithSuitableAgent_Shrinks()
        {
            var fire = Make<ClassAFire>();
            fire.Ignite(40f);
            float before = fire.Intensity;
            fire.Step(1f, new SprayContact(AgentType.Water, 1f));
            Assert.Less(fire.Intensity, before);
        }
    }
}
