using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>References for one row in the history list.</summary>
    public sealed class HistoryRow : MonoBehaviour
    {
        public Image badge;
        public TMP_Text badgeLabel;
        public TMP_Text title;
        public TMP_Text subtitle;
        public TMP_Text score;
        public TMP_Text outcome;
    }
}
