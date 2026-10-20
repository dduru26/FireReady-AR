using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>Scenario select (design screen 2): three scenarios, Guided or Assessment, Continue.</summary>
    public sealed class ScenarioSelectView : MonoBehaviour
    {
        [System.Serializable]
        public struct Option
        {
            public Button button;
            public Image background;
            public Image radio;
            public GameObject radioDot;
            public TMP_Text title;
            public TMP_Text tagline;
            public Image badge;
            public TMP_Text badgeLabel;
        }

        [SerializeField] GameObject root;
        [SerializeField] ScenarioCatalog catalog;
        [SerializeField] Option[] options;
        [SerializeField] Button guidedButton;
        [SerializeField] Button assessmentButton;
        [SerializeField] Image guidedBackground;
        [SerializeField] Image assessmentBackground;
        [SerializeField] TMP_Text guidedLabel;
        [SerializeField] TMP_Text assessmentLabel;
        [SerializeField] Button backButton;
        [SerializeField] Button continueButton;

        int m_Selected;
        TrainingMode m_Mode = TrainingMode.Guided;
        string m_Trainee;

        void Awake()
        {
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i].button.onClick.AddListener(() => Select(index));
            }
            guidedButton.onClick.AddListener(() => SetMode(TrainingMode.Guided));
            assessmentButton.onClick.AddListener(() => SetMode(TrainingMode.Assessment));
            backButton.onClick.AddListener(Hide);
            continueButton.onClick.AddListener(Continue);
            Fill();
            root.SetActive(false);
        }

        public void Show(string trainee)
        {
            m_Trainee = trainee;
            Select(m_Selected);
            SetMode(m_Mode);
            root.SetActive(true);
        }

        public void Hide() => root.SetActive(false);

        void Fill()
        {
            for (int i = 0; i < options.Length; i++)
            {
                var def = catalog != null && i < catalog.scenarios.Count ? catalog.scenarios[i] : null;
                options[i].button.gameObject.SetActive(def != null);
                if (def == null) continue;
                options[i].title.text = def.title;
                options[i].tagline.text = def.tagline;
                options[i].badgeLabel.text = def.ClassLetter;
                switch (def.fireClass)
                {
                    case FireClass.ClassF:
                        options[i].badge.color = UiPalette.Warning;
                        options[i].badgeLabel.color = UiPalette.Hex(0x1A1A1A);
                        break;
                    case FireClass.Electrical:
                        options[i].badge.color = UiPalette.Hex(0x111242);
                        options[i].badgeLabel.color = UiPalette.Warning;
                        break;
                    default:
                        options[i].badge.color = MenuPalette.Text;
                        options[i].badgeLabel.color = Color.white;
                        break;
                }
            }
        }

        void Select(int index)
        {
            m_Selected = index;
            for (int i = 0; i < options.Length; i++)
            {
                bool on = i == index;
                options[i].background.color = on ? MenuPalette.Soft : MenuPalette.Surface;
                options[i].radio.color = on ? UiPalette.PrimaryBlue : MenuPalette.Track;
                options[i].radioDot.SetActive(on);
            }
        }

        void SetMode(TrainingMode mode)
        {
            m_Mode = mode;
            bool guided = mode == TrainingMode.Guided;
            guidedBackground.color = guided ? MenuPalette.Text : new Color(0, 0, 0, 0);
            assessmentBackground.color = guided ? new Color(0, 0, 0, 0) : MenuPalette.Text;
            guidedLabel.color = guided ? Color.white : MenuPalette.Text;
            assessmentLabel.color = guided ? MenuPalette.Text : Color.white;
        }

        void Continue()
        {
            if (catalog == null || m_Selected >= catalog.scenarios.Count)
                return;
            root.SetActive(false);
            EventBus.Publish(new ScenarioSelected(catalog.scenarios[m_Selected], m_Mode, m_Trainee));
        }
    }
}
