using FireReady.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>
    /// The AR setup screens (Scan, Place, Confirm) plus the Paused overlay.
    /// It only listens to events and publishes the trainee's choices back, so it
    /// never talks to the AR or simulation code directly.
    /// </summary>
    public sealed class SetupHudView : MonoBehaviour
    {
        [Header("Status pill")]
        [SerializeField] Image statusBackground;
        [SerializeField] TMP_Text statusLabel;

        [Header("Cards")]
        [SerializeField] GameObject scanCard;
        [SerializeField] GameObject placeCard;
        [SerializeField] GameObject confirmCard;
        [SerializeField] GameObject runningCard;
        [SerializeField] GameObject pausedOverlay;

        [Header("Place")]
        [SerializeField] TMP_Text areaLabel;
        [SerializeField] Button placeButton;

        [Header("Confirm")]
        [SerializeField] Button moveButton;
        [SerializeField] Button confirmButton;

        [Header("Paused")]
        [SerializeField] TMP_Text pausedTrackingLabel;
        [SerializeField] Image pausedTrackingBackground;
        [SerializeField] Button resumeButton;

        SimulationStateId m_State;
        bool m_Tracking = true;

        void Awake()
        {
            placeButton.onClick.AddListener(() => EventBus.Publish(new PlaceRequested()));
            moveButton.onClick.AddListener(() => EventBus.Publish(new RepositionRequested()));
            confirmButton.onClick.AddListener(() => EventBus.Publish(new PlacementConfirmed()));
            resumeButton.onClick.AddListener(() => EventBus.Publish(new ResumeRequested()));
            ShowOnly(null);
            SetStatus(null, default);
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<PlacementTargetChanged>(OnTargetChanged);
            EventBus.Subscribe<TrackingLost>(OnTrackingLost);
            EventBus.Subscribe<TrackingRestored>(OnTrackingRestored);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<PlacementTargetChanged>(OnTargetChanged);
            EventBus.Unsubscribe<TrackingLost>(OnTrackingLost);
            EventBus.Unsubscribe<TrackingRestored>(OnTrackingRestored);
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            m_State = e.Current;
            switch (e.Current)
            {
                case SimulationStateId.Scanning:
                    ShowOnly(scanCard);
                    SetStatus("Searching for the floor", UiPalette.Panel);
                    break;
                case SimulationStateId.Placing:
                    ShowOnly(placeCard);
                    SetStatus("Floor found", UiPalette.Safe);
                    SetArea(false, false, Vector2.zero);
                    break;
                case SimulationStateId.Confirming:
                    ShowOnly(confirmCard);
                    SetStatus("Scene anchored to the floor", UiPalette.Safe);
                    break;
                case SimulationStateId.Running:
                case SimulationStateId.Completed:
                    // The simulation HUD takes over from here.
                    ShowOnly(null);
                    SetStatus(null, default);
                    break;
                case SimulationStateId.Paused:
                    ShowOnly(pausedOverlay);
                    SetStatus(null, default);
                    RefreshPaused();
                    break;
                default:
                    ShowOnly(null);
                    SetStatus(null, default);
                    break;
            }
        }

        void OnTargetChanged(PlacementTargetChanged e) => SetArea(e.HasTarget, e.IsLargeEnough, e.AreaSize);

        void OnTrackingLost(TrackingLost e)
        {
            m_Tracking = false;
            RefreshPaused();
        }

        void OnTrackingRestored(TrackingRestored e)
        {
            m_Tracking = true;
            RefreshPaused();
        }

        void SetArea(bool hasTarget, bool largeEnough, Vector2 size)
        {
            if (!hasTarget)
            {
                areaLabel.text = "Aim the ring at the floor";
                areaLabel.color = UiPalette.TextMuted;
            }
            else if (!largeEnough)
            {
                areaLabel.text = $"Area too small: {size.x:0.0} × {size.y:0.0} m. Keep scanning.";
                areaLabel.color = UiPalette.WarningText;
            }
            else
            {
                areaLabel.text = $"Clear area: {size.x:0.0} × {size.y:0.0} m";
                areaLabel.color = UiPalette.SafeText;
            }
            placeButton.interactable = hasTarget && largeEnough;
        }

        void RefreshPaused()
        {
            if (m_State != SimulationStateId.Paused)
                return;

            resumeButton.interactable = m_Tracking;
            pausedTrackingLabel.text = m_Tracking
                ? "Floor found again. You can resume."
                : "Point your phone back at the scene to find the floor again.";
            pausedTrackingLabel.color = m_Tracking ? UiPalette.SafeText : UiPalette.WarningText;
            pausedTrackingBackground.color = m_Tracking
                ? UiPalette.Hex(0x12803C, 0.18f)
                : UiPalette.Hex(0xF5C518, 0.14f);
        }

        void ShowOnly(GameObject card)
        {
            SetActive(scanCard, card);
            SetActive(placeCard, card);
            SetActive(confirmCard, card);
            SetActive(runningCard, card);
            SetActive(pausedOverlay, card);
        }

        static void SetActive(GameObject target, GameObject wanted)
        {
            if (target != null)
                target.SetActive(target == wanted);
        }

        void SetStatus(string text, Color background)
        {
            bool show = !string.IsNullOrEmpty(text);
            statusBackground.gameObject.SetActive(show);
            if (!show)
                return;
            statusLabel.text = text;
            statusBackground.color = background;
        }
    }
}
