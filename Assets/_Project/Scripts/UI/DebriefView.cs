using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>The completion screen (design screen 11): outcome, score and the six criteria (FR-27).</summary>
    public sealed class DebriefView : MonoBehaviour
    {
        [System.Serializable]
        public struct RuleRow
        {
            public GameObject root;
            public TMP_Text name;
            public TMP_Text points;
            public RectTransform fill;
            public Image fillImage;
            public TMP_Text feedback;
        }

        [SerializeField] GameObject root;
        [SerializeField] Image header;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text meta;
        [SerializeField] TMP_Text score;
        [SerializeField] TMP_Text maxScore;
        [SerializeField] GameObject newBestPill;
        [SerializeField] RuleRow[] rows;
        [SerializeField] Button mainMenuButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button historyButton;

        void Awake()
        {
            mainMenuButton.onClick.AddListener(() => EventBus.Publish(new MainMenuRequested()));
            restartButton.onClick.AddListener(() => EventBus.Publish(new RestartRequested()));
            historyButton.onClick.AddListener(() => EventBus.Publish(new HistoryRequested()));
            root.SetActive(false);
        }

        void OnEnable()
        {
            EventBus.Subscribe<SessionScored>(OnScored);
            EventBus.Subscribe<SessionSaved>(OnSaved);
            EventBus.Subscribe<SessionReset>(OnReset);
        }

        void OnReset(SessionReset e) => root.SetActive(false);

        void OnDisable()
        {
            EventBus.Unsubscribe<SessionScored>(OnScored);
            EventBus.Unsubscribe<SessionSaved>(OnSaved);
            EventBus.Unsubscribe<SessionReset>(OnReset);
        }

        void OnScored(SessionScored e)
        {
            root.SetActive(true);
            newBestPill.SetActive(false);

            string outcome = e.Outcome.ToString();
            header.color = e.Outcome == SessionOutcome.Extinguished || e.Outcome == SessionOutcome.EvacuatedCorrectly
                ? UiPalette.Safe
                : e.Outcome == SessionOutcome.EvacuatedEarly ? UiPalette.Hex(0xB45309) : UiPalette.Danger;
            title.text = e.Outcome switch
            {
                SessionOutcome.Extinguished => "Fire extinguished",
                SessionOutcome.EvacuatedCorrectly => "You evacuated safely",
                SessionOutcome.EvacuatedEarly => "You evacuated early",
                SessionOutcome.OutOfControl => "The fire got out of control",
                SessionOutcome.Injured => "You got an electric shock",
                _ => MenuPalette.OutcomeLabel(outcome)
            };

            int seconds = Mathf.FloorToInt(e.Duration);
            meta.text = $"{e.Title} · {e.Mode} · {seconds / 60}:{seconds % 60:00}";
            score.text = e.Total.ToString();
            maxScore.text = $"/ {e.Max}";

            for (int i = 0; i < rows.Length; i++)
            {
                bool has = e.Lines != null && i < e.Lines.Length;
                var row = rows[i];
                row.root.SetActive(has);
                if (!has) continue;

                var line = e.Lines[i];
                row.name.text = line.Name;
                row.points.text = $"{line.Points} / {line.Max}";
                float fraction = line.Max > 0 ? (float)line.Points / line.Max : 0f;
                row.fill.anchorMax = new Vector2(fraction, 1f);
                row.fillImage.color = fraction >= 0.99f ? UiPalette.Safe : fraction >= 0.5f ? UiPalette.Warning : UiPalette.Danger;
                bool hasFeedback = !string.IsNullOrEmpty(line.Feedback);
                row.feedback.gameObject.SetActive(hasFeedback);
                row.feedback.text = hasFeedback ? line.Feedback : string.Empty;
            }
        }

        void OnSaved(SessionSaved e) => newBestPill.SetActive(e.IsNewBest);
    }
}
