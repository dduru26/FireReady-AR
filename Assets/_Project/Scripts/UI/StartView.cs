using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>Start screen (design screen 1): trainee name, Start training, Session history.</summary>
    public sealed class StartView : StateScreen
    {
        const string NameKey = "FireReady.TraineeName";

        [SerializeField] TMP_InputField traineeName;
        [SerializeField] Button startButton;
        [SerializeField] Button historyButton;
        [SerializeField] ScenarioSelectView scenarioSelect;

        public string TraineeName => traineeName != null ? traineeName.text.Trim() : string.Empty;

        protected override void Awake()
        {
            base.Awake();
            // A preference, not session data, so PlayerPrefs is the right place for it.
            traineeName.text = PlayerPrefs.GetString(NameKey, string.Empty);
            traineeName.onEndEdit.AddListener(SaveName);
            startButton.onClick.AddListener(() =>
            {
                SaveName(traineeName.text);
                scenarioSelect.Show(TraineeName);
            });
            historyButton.onClick.AddListener(() => EventBus.Publish(new HistoryRequested()));
        }

        void SaveName(string value)
        {
            PlayerPrefs.SetString(NameKey, value.Trim());
            PlayerPrefs.Save();
        }

        protected override bool VisibleIn(SimulationStateId state) => state == SimulationStateId.MainMenu;

        protected override void OnShown() => scenarioSelect.Hide();
    }
}
