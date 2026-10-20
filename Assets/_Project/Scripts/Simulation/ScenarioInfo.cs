using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>Sits on the root of a scenario prefab and names it.</summary>
    public sealed class ScenarioInfo : MonoBehaviour
    {
        [SerializeField] string title = "Office waste bin";
        public string Title => title;
    }
}
