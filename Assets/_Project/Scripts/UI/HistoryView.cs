using System;
using System.Collections.Generic;
using System.Globalization;
using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>Session history (design screen 12): saved sessions, newest first (FR-29), and delete all (FR-31).</summary>
    public sealed class HistoryView : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text countValue;
        [SerializeField] TMP_Text bestValue;
        [SerializeField] TMP_Text putOutValue;
        [SerializeField] RectTransform listParent;
        [SerializeField] HistoryRow rowTemplate;
        [SerializeField] TMP_Text emptyLabel;
        [SerializeField] Button deleteAllButton;
        [SerializeField] TMP_Text deleteAllLabel;
        [SerializeField] ScrollRect scroll;
        [SerializeField] int maxRows = 50;

        ISessionHistory m_History;
        readonly List<HistoryRow> m_Rows = new List<HistoryRow>();
        bool m_ConfirmDelete;

        public void Construct(ISessionHistory history) => m_History = history;

        void Awake()
        {
            backButton.onClick.AddListener(Hide);
            deleteAllButton.onClick.AddListener(OnDeleteAll);
            rowTemplate.gameObject.SetActive(false);
            root.SetActive(false);
        }

        void OnEnable()
        {
            EventBus.Subscribe<HistoryRequested>(OnRequested);
            EventBus.Subscribe<SessionReset>(OnReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<HistoryRequested>(OnRequested);
            EventBus.Unsubscribe<SessionReset>(OnReset);
        }

        void OnReset(SessionReset e) => Hide();

        void OnRequested(HistoryRequested e) => Show();

        public void Show()
        {
            root.SetActive(true);
            m_ConfirmDelete = false;
            deleteAllLabel.text = "Delete all sessions";
            Refresh();
        }

        public void Hide() => root.SetActive(false);

        void Refresh()
        {
            var sessions = m_History != null ? m_History.Recent(maxRows) : (IReadOnlyList<SessionSummary>)Array.Empty<SessionSummary>();
            int total = m_History?.Count ?? 0;

            int best = -1, putOut = 0;
            foreach (var s in m_History != null ? m_History.Recent(int.MaxValue) : sessions)
            {
                best = Math.Max(best, s.score);
                if (s.outcome == "Extinguished") putOut++;
            }
            countValue.text = total.ToString();
            bestValue.text = best >= 0 ? best.ToString() : "–";
            putOutValue.text = $"{putOut} / {total}";

            while (m_Rows.Count < sessions.Count)
            {
                var row = Instantiate(rowTemplate, listParent);
                m_Rows.Add(row);
            }
            for (int i = 0; i < m_Rows.Count; i++)
            {
                bool used = i < sessions.Count;
                m_Rows[i].gameObject.SetActive(used);
                if (used) Fill(m_Rows[i], sessions[i]);
            }
            emptyLabel.gameObject.SetActive(sessions.Count == 0);
            if (scroll != null)
            {
                // Rebuild the layout now so the list opens at the newest session.
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1f;
            }
            deleteAllButton.gameObject.SetActive(sessions.Count > 0);
        }

        static void Fill(HistoryRow row, SessionSummary s)
        {
            row.title.text = s.title;
            row.subtitle.text = $"{s.mode} · {FriendlyDate(s.endedUtc)}";
            row.score.text = s.score.ToString();
            row.outcome.text = MenuPalette.OutcomeLabel(s.outcome);
            row.outcome.color = MenuPalette.OutcomeColor(s.outcome);

            switch (s.fireClass)
            {
                case "ClassF":
                    row.badge.color = UiPalette.Warning;
                    row.badgeLabel.text = "F";
                    row.badgeLabel.color = UiPalette.Hex(0x1A1A1A);
                    break;
                case "Electrical":
                    row.badge.color = UiPalette.Hex(0x111242);
                    row.badgeLabel.text = "E";
                    row.badgeLabel.color = UiPalette.Warning;
                    break;
                default:
                    row.badge.color = MenuPalette.Text;
                    row.badgeLabel.text = "A";
                    row.badgeLabel.color = Color.white;
                    break;
            }
        }

        static string FriendlyDate(string isoUtc)
        {
            if (!DateTime.TryParse(isoUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc))
                return string.Empty;
            var local = utc.ToLocalTime();
            var today = DateTime.Now.Date;
            if (local.Date == today) return $"Today, {local:HH:mm}";
            if (local.Date == today.AddDays(-1)) return $"Yesterday, {local:HH:mm}";
            return local.ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
        }

        void OnDeleteAll()
        {
            if (!m_ConfirmDelete)
            {
                m_ConfirmDelete = true;
                deleteAllLabel.text = "Tap again to delete all";
                return;
            }
            m_History?.DeleteAll();
            m_ConfirmDelete = false;
            deleteAllLabel.text = "Delete all sessions";
            Refresh();
        }
    }
}
