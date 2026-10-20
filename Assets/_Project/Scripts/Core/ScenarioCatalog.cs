using System.Collections.Generic;
using UnityEngine;

namespace FireReady.Core
{
    /// <summary>The scenarios offered on the Scenario select screen, in order.</summary>
    [CreateAssetMenu(menuName = "FireReady/Scenario Catalog", fileName = "ScenarioCatalog")]
    public sealed class ScenarioCatalog : ScriptableObject
    {
        public List<ScenarioDefinition> scenarios = new List<ScenarioDefinition>();
    }
}
