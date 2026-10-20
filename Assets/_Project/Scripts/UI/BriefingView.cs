using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>Briefing (design screen 7): the scenario, its class and the six tasks, then Start.</summary>
    public sealed class BriefingView : StateScreen
    {
        [SerializeField] TMP_Text classTag;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text description;
        [SerializeField] TMP_Text firstTask;
        [SerializeField] Button startButton;

        protected override void Awake()
        {
            base.Awake();
            startButton.onClick.AddListener(() => EventBus.Publish(new BriefingAccepted()));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<ScenarioSelected>(OnSelected);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<ScenarioSelected>(OnSelected);
        }

        protected override bool VisibleIn(SimulationStateId state) => state == SimulationStateId.Briefing;

        void OnSelected(ScenarioSelected e)
        {
            var s = e.Scenario;
            classTag.text = s.ClassTag;
            subtitle.text = $"SCENARIO {s.number} · {e.Mode.ToString().ToUpperInvariant()}";
            title.text = s.title;
            description.text = s.briefing;
            firstTask.text = s.fireClass == FireClass.Electrical
                ? "Raise the alarm and cut the power at the wall"
                : "Raise the alarm";
        }
    }
}
