using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>Loading screen (design screen 3) shown while Addressables load the scenario.</summary>
    public sealed class LoadingView : StateScreen
    {
        [SerializeField] TMP_Text badgeLabel;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] TMP_Text title;
        [SerializeField] RectTransform barFill;
        [SerializeField] TMP_Text percent;
        [SerializeField] TMP_Text[] items;
        [SerializeField] Image[] itemTicks;
        [SerializeField] Button cancelButton;

        protected override void Awake()
        {
            base.Awake();
            cancelButton.onClick.AddListener(() => EventBus.Publish(new LoadingCancelled()));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<ScenarioSelected>(OnSelected);
            EventBus.Subscribe<ScenarioLoadProgress>(OnProgress);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<ScenarioSelected>(OnSelected);
            EventBus.Unsubscribe<ScenarioLoadProgress>(OnProgress);
        }

        protected override bool VisibleIn(SimulationStateId state) => state == SimulationStateId.Loading;

        void OnSelected(ScenarioSelected e)
        {
            var s = e.Scenario;
            badgeLabel.text = s.ClassLetter;
            subtitle.text = $"Scenario {s.number} · {e.Mode}";
            title.text = s.loadingTitle;
            for (int i = 0; i < items.Length; i++)
                items[i].text = s.loadingItems != null && i < s.loadingItems.Length ? s.loadingItems[i] : string.Empty;
            OnProgress(new ScenarioLoadProgress(0f));
        }

        void OnProgress(ScenarioLoadProgress e)
        {
            float p = Mathf.Clamp01(e.Progress);
            barFill.anchorMax = new Vector2(p, 1f);
            percent.text = $"{Mathf.RoundToInt(p * 100f)}%";
            for (int i = 0; i < itemTicks.Length; i++)
            {
                bool done = p >= (i + 1f) / (itemTicks.Length + 0.5f);
                itemTicks[i].color = done ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                items[i].color = done ? Color.white : UiPalette.Hex(0xDCE6F7);
            }
        }
    }
}
