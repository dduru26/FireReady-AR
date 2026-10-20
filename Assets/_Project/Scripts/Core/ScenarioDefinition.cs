using UnityEngine;

namespace FireReady.Core
{
    /// <summary>
    /// Describes one scenario for the menus and briefing, and names the Addressables
    /// key of its content. The content itself is not referenced here, so it is only
    /// loaded when the scenario starts.
    /// </summary>
    [CreateAssetMenu(menuName = "FireReady/Scenario", fileName = "Scenario")]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        public int number = 1;
        public string title = "Office waste bin";
        [TextArea] public string tagline = "Burning paper. The place to start.";
        [TextArea] public string briefing = "A waste-paper bin has caught fire beside a desk.";
        public FireClass fireClass = FireClass.ClassA;
        [Tooltip("Addressables key of the scenario prefab.")]
        public string address = "scenario/s1";
        public string loadingTitle = "Preparing the office bin fire…";
        public string[] loadingItems = { "Waste bin and extinguisher rack", "Fire, smoke and spray effects", "Sounds and captions" };

        public string ClassTag => fireClass switch
        {
            FireClass.ClassF => "CLASS F",
            FireClass.Electrical => "ELECTRICAL",
            _ => "CLASS A"
        };

        public string ClassLetter => fireClass switch
        {
            FireClass.ClassF => "F",
            FireClass.Electrical => "E",
            _ => "A"
        };
    }
}
