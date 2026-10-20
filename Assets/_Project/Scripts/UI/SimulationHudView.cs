using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>
    /// The in-simulation HUD (design screens 8, 9 and 11): fire intensity, the guided
    /// step, distance/aim/sweep chips, the held extinguisher, Alarm, hold-to-Squeeze,
    /// Evacuate, mistake warnings and the completion card. It reads only events and
    /// publishes the trainee's commands back to the EventBus.
    /// </summary>
    public sealed class SimulationHudView : MonoBehaviour
    {
        [Header("Roots")]
        [SerializeField] GameObject hudRoot;
        [SerializeField] GameObject completionCard;

        [Header("Top bar")]
        [SerializeField] TMP_Text scenarioTitle;
        [SerializeField] TMP_Text timerLabel;

        [Header("Intensity")]
        [SerializeField] Image trendBadge;
        [SerializeField] TMP_Text trendLabel;
        [SerializeField] TMP_Text intensityValue;
        [SerializeField] RectTransform intensityFill;
        [SerializeField] RectTransform thresholdMarker;
        [SerializeField] TMP_Text thresholdLabel;

        [Header("Guided step")]
        [SerializeField] GameObject stepCard;
        [SerializeField] TMP_Text stepCounter;
        [SerializeField] TMP_Text stepNext;
        [SerializeField] TMP_Text stepTitle;
        [SerializeField] Image[] stepSegments;

        [Header("Warning")]
        [SerializeField] GameObject warningCard;
        [SerializeField] TMP_Text warningTitle;
        [SerializeField] TMP_Text warningAdvice;
        [SerializeField] float warningSeconds = 4f;

        [Header("Technique chips")]
        [SerializeField] GameObject chipsRow;
        [SerializeField] Image distanceChip;
        [SerializeField] TMP_Text distanceValue;
        [SerializeField] TMP_Text distanceLabel;
        [SerializeField] Image aimChip;
        [SerializeField] TMP_Text aimLabel;
        [SerializeField] Image sweepChip;
        [SerializeField] TMP_Text sweepLabel;

        [Header("Caption")]
        [SerializeField] TMP_Text captionLabel;

        [Header("Extinguisher")]
        [SerializeField] GameObject emptyHandRow;
        [SerializeField] GameObject heldRow;
        [SerializeField] Image bandSwatch;
        [SerializeField] TMP_Text agentName;
        [SerializeField] TMP_Text agentStatus;
        [SerializeField] RectTransform remainingFill;
        [SerializeField] Button pullPinButton;
        [SerializeField] Button putBackButton;

        [Header("Actions")]
        [SerializeField] Button alarmButton;
        [SerializeField] TMP_Text alarmLabel;
        [SerializeField] Image alarmIcon;
        [SerializeField] HoldButton squeezeButton;
        [SerializeField] Button evacuateButton;

        [Header("Completion")]
        [SerializeField] TMP_Text completionTitle;
        [SerializeField] TMP_Text completionBody;
        [SerializeField] TMP_Text completionMeta;
        [SerializeField] Button restartButton;

        static readonly Color Green = UiPalette.Safe;
        static readonly Color Yellow = UiPalette.Warning;
        static readonly Color Red = UiPalette.Danger;
        static readonly Color Neutral = UiPalette.Panel;
        static readonly Color GuidedBlue = UiPalette.Hex(0x1F5FBF);

        SimulationStateId m_State;
        float m_Elapsed;
        int m_ShownSeconds = -1;
        float m_WarningUntil;
        float m_Threshold = 70f;
        string m_Title = "Training";
        bool m_PinPulled;
        bool m_Holding;
        bool m_Spraying;
        bool m_Alarm;
        float m_Remaining;
        AgentSuitability m_Suitability;

        void Awake()
        {
            alarmButton.onClick.AddListener(OnAlarmClicked);
            evacuateButton.onClick.AddListener(() => EventBus.Publish(new EvacuateRequested()));
            pullPinButton.onClick.AddListener(() => EventBus.Publish(new PullPinRequested()));
            putBackButton.onClick.AddListener(() => EventBus.Publish(new ReturnExtinguisherRequested()));
            restartButton.onClick.AddListener(() => EventBus.Publish(new RestartRequested()));
            squeezeButton.HeldChanged += held => EventBus.Publish(new SqueezeChanged(held));

            hudRoot.SetActive(false);
            completionCard.SetActive(false);
            warningCard.SetActive(false);
            ShowEmptyHand();
            SetChipsUnknown();
            UpdateCaption();
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Subscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Subscribe<GuidedStepChanged>(OnStep);
            EventBus.Subscribe<MistakeRecorded>(OnMistake);
            EventBus.Subscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Subscribe<TechniqueChanged>(OnTechnique);
            EventBus.Subscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Subscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Subscribe<PinPulled>(OnPinPulled);
            EventBus.Subscribe<AgentRemainingChanged>(OnRemaining);
            EventBus.Subscribe<SprayStateChanged>(OnSpray);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Unsubscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Unsubscribe<GuidedStepChanged>(OnStep);
            EventBus.Unsubscribe<MistakeRecorded>(OnMistake);
            EventBus.Unsubscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Unsubscribe<TechniqueChanged>(OnTechnique);
            EventBus.Unsubscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Unsubscribe<ExtinguisherReturned>(OnReturned);
            EventBus.Unsubscribe<PinPulled>(OnPinPulled);
            EventBus.Unsubscribe<AgentRemainingChanged>(OnRemaining);
            EventBus.Unsubscribe<SprayStateChanged>(OnSpray);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
        }

        void Update()
        {
            if (m_State == SimulationStateId.Running)
            {
                m_Elapsed += Time.deltaTime;
                int seconds = Mathf.FloorToInt(m_Elapsed);
                if (seconds != m_ShownSeconds)
                {
                    m_ShownSeconds = seconds;
                    timerLabel.text = FormatTime(seconds);
                }
            }

            if (warningCard.activeSelf && Time.unscaledTime >= m_WarningUntil)
            {
                warningCard.SetActive(false);
                stepCard.SetActive(true);
            }
        }

        // ---- State -------------------------------------------------------------------

        void OnStateChanged(SimulationStateChanged e)
        {
            m_State = e.Current;
            bool inSimulation = e.Current == SimulationStateId.Running || e.Current == SimulationStateId.Paused;
            hudRoot.SetActive(inSimulation);
            if (e.Current != SimulationStateId.Running)
                squeezeButton.Release();
        }

        void OnScenarioStarted(ScenarioStarted e)
        {
            m_Title = e.Title;
            m_Threshold = e.EvacuateThreshold;
            scenarioTitle.text = e.Title;
            timerLabel.text = FormatTime(0);
            thresholdLabel.text = $"Evacuate above {m_Threshold:0}";
            var a = thresholdMarker.anchorMin;
            thresholdMarker.anchorMin = new Vector2(m_Threshold / 100f, a.y);
            thresholdMarker.anchorMax = new Vector2(m_Threshold / 100f, thresholdMarker.anchorMax.y);
            thresholdMarker.anchoredPosition = new Vector2(0f, thresholdMarker.anchoredPosition.y);
        }

        // ---- Fire ----------------------------------------------------------------------

        void OnIntensity(FireIntensityChanged e)
        {
            intensityValue.text = Mathf.CeilToInt(e.Intensity).ToString();
            intensityFill.anchorMax = new Vector2(Mathf.Clamp01(e.Intensity / 100f), 1f);

            switch (e.Trend)
            {
                case FireTrend.Shrinking:
                    trendLabel.text = "Shrinking";
                    trendBadge.color = Green;
                    break;
                case FireTrend.Growing:
                    trendLabel.text = e.Intensity >= m_Threshold ? "Too big" : "Growing";
                    trendBadge.color = e.Intensity >= m_Threshold ? Red : UiPalette.Hex(0xB45309);
                    break;
                default:
                    trendLabel.text = "Steady";
                    trendBadge.color = UiPalette.Hex(0x2A3038);
                    break;
            }
        }

        void OnFlareUp(FireFlaredUp e) => ShowWarning($"Flare-up +{e.Amount:0}", e.Reason);

        // ---- Steps and warnings -----------------------------------------------------------

        void OnStep(GuidedStepChanged e)
        {
            stepCounter.text = $"STEP {e.Index + 1} OF {e.Count}";
            stepNext.text = e.Completed >= e.Count ? "Done" : $"Next: {e.Next}";
            stepTitle.text = e.Title;
            for (int i = 0; i < stepSegments.Length; i++)
            {
                float alpha = i < e.Completed ? 1f : (i == e.Completed ? 0.6f : 0.25f);
                stepSegments[i].color = new Color(1f, 1f, 1f, alpha);
            }
        }

        void OnMistake(MistakeRecorded e) => ShowWarning(e.Title, e.Advice);

        void ShowWarning(string title, string advice)
        {
            warningTitle.text = title;
            warningAdvice.text = advice ?? string.Empty;
            warningCard.SetActive(true);
            stepCard.SetActive(false);
            m_WarningUntil = Time.unscaledTime + warningSeconds;
        }

        // ---- Technique -----------------------------------------------------------------

        void OnTechnique(TechniqueChanged e)
        {
            if (!m_Holding)
                return;

            distanceValue.text = $"{e.Distance:0.0} m";
            switch (e.Band)
            {
                case DistanceBand.InRange: distanceLabel.text = "In range"; distanceChip.color = Green; break;
                case DistanceBand.TooClose: distanceLabel.text = "Too close"; distanceChip.color = Red; break;
                default: distanceLabel.text = "Too far"; distanceChip.color = Yellow; break;
            }

            switch (e.Aim)
            {
                case AimQuality.OnBase: aimLabel.text = "On the base"; aimChip.color = Green; break;
                case AimQuality.TooHigh: aimLabel.text = "Aim lower"; aimChip.color = Yellow; break;
                case AimQuality.TooLow: aimLabel.text = "Aim higher"; aimChip.color = Yellow; break;
                default: aimLabel.text = "Off target"; aimChip.color = Red; break;
            }

            bool sweeping = e.Sweep == SweepQuality.Good;
            sweepLabel.text = sweeping ? "Good" : (m_Spraying ? "Not yet" : "Side to side");
            sweepChip.color = sweeping ? Green : (m_Spraying ? Yellow : Neutral);
            SetChipTextColors();
        }

        void SetChipsUnknown()
        {
            distanceValue.text = "-- m";
            distanceLabel.text = "Distance";
            aimLabel.text = "On the base";
            sweepLabel.text = "Side to side";
            distanceChip.color = aimChip.color = sweepChip.color = Neutral;
            SetChipTextColors();
        }

        void SetChipTextColors()
        {
            // Dark text on the yellow warning colour, white elsewhere.
            Tint(distanceChip, distanceValue, distanceLabel);
            Tint(aimChip, aimLabel, null);
            Tint(sweepChip, sweepLabel, null);
        }

        static void Tint(Image chip, TMP_Text a, TMP_Text b)
        {
            var color = chip.color == UiPalette.Warning ? UiPalette.Hex(0x1A1A1A) : Color.white;
            foreach (var t in chip.GetComponentsInChildren<TMP_Text>(true))
                t.color = color;
        }

        // ---- Extinguisher -----------------------------------------------------------

        void OnPickedUp(ExtinguisherPickedUp e)
        {
            m_Holding = true;
            m_PinPulled = false;
            m_Suitability = e.Suitability;
            emptyHandRow.SetActive(false);
            heldRow.SetActive(true);
            bandSwatch.color = e.BandColor;
            agentName.text = e.DisplayName;
            pullPinButton.gameObject.SetActive(true);
            RefreshAgentStatus();
        }

        void OnReturned(ExtinguisherReturned e)
        {
            m_Holding = false;
            ShowEmptyHand();
            SetChipsUnknown();
        }

        void ShowEmptyHand()
        {
            emptyHandRow.SetActive(true);
            heldRow.SetActive(false);
        }

        void OnPinPulled(PinPulled e)
        {
            m_PinPulled = true;
            pullPinButton.gameObject.SetActive(false);
            RefreshAgentStatus();
        }

        void OnRemaining(AgentRemainingChanged e)
        {
            m_Remaining = e.Seconds;
            remainingFill.anchorMax = new Vector2(Mathf.Clamp01(e.Fraction), 1f);
            RefreshAgentStatus();
        }

        void RefreshAgentStatus()
        {
            string pin = m_PinPulled ? "<color=#6FD99A><b>Pin pulled</b></color>" : "<color=#F5C518><b>Pin in</b></color>";
            string warn = m_Suitability == AgentSuitability.Dangerous ? " · <color=#FF6B63>unsafe here</color>"
                        : m_Suitability == AgentSuitability.Poor ? " · <color=#F5C518>poor choice</color>"
                        : string.Empty;
            agentStatus.text = $"{pin} · {m_Remaining:0.0} s left{warn}";
        }

        void OnSpray(SprayStateChanged e)
        {
            m_Spraying = e.Spraying;
            UpdateCaption();
        }

        void UpdateCaption()
        {
            // Captions for the sound cues (FR-24); the audio itself plays from the AudioManager.
            captionLabel.text = m_Spraying ? "Fire crackling · spray hissing" : "Fire crackling";
        }

        // ---- Actions -------------------------------------------------------------------

        void OnAlarmClicked()
        {
            if (m_Alarm)
                return;
            m_Alarm = true;
            alarmLabel.text = "Alarm on";
            alarmIcon.color = UiPalette.Warning;
            EventBus.Publish(new AlarmRaised());
        }

        // ---- Completion ----------------------------------------------------------------

        void OnSessionEnded(SessionEnded e)
        {
            hudRoot.SetActive(false);
            completionCard.SetActive(true);
            squeezeButton.Release();

            switch (e.Outcome)
            {
                case SessionOutcome.Extinguished:
                    completionTitle.text = "Fire extinguished";
                    completionBody.text = "You put the fire out.";
                    break;
                case SessionOutcome.EvacuatedCorrectly:
                    completionTitle.text = "You evacuated";
                    completionBody.text = "Right call: the fire was too big for a portable extinguisher.";
                    break;
                case SessionOutcome.EvacuatedEarly:
                    completionTitle.text = "You evacuated early";
                    completionBody.text = "The fire was still small enough to tackle with the right extinguisher.";
                    break;
                case SessionOutcome.OutOfControl:
                    completionTitle.text = "The fire got out of control";
                    completionBody.text = "Once it passes the evacuate line, raise the alarm and get out.";
                    break;
                case SessionOutcome.Injured:
                    completionTitle.text = "You were injured";
                    completionBody.text = "Water on live electrical equipment can give a fatal shock. Use CO2.";
                    break;
                default:
                    completionTitle.text = "Session ended";
                    completionBody.text = string.Empty;
                    break;
            }
            completionMeta.text = $"{m_Title} · Guided · {FormatTime(Mathf.FloorToInt(e.Duration))}";
        }

        static string FormatTime(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
