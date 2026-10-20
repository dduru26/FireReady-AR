using FireReady.AR;
using FireReady.Core;
using FireReady.Data;
using FireReady.Simulation;
using FireReady.Training;
using FireReady.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace FireReady.App
{
    /// <summary>
    /// Composition root. It is the only class that knows every system: it hands each
    /// one its dependencies through Construct(), then starts the session. Nothing else
    /// uses FindObjectOfType or singletons to reach another system.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrapper : MonoBehaviour
    {
        [Header("AR Foundation")]
        [SerializeField] ARSession arSession;
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ARAnchorManager anchorManager;
        [SerializeField] Camera arCamera;

        [Header("Systems")]
        [SerializeField] SimulationManager simulation;
        [SerializeField] PlacementController placement;
        [SerializeField] PlaneVisibilityController planeVisibility;
        [SerializeField] TrackingMonitor trackingMonitor;
        [SerializeField] ScreenTapDetector tapDetector;
        [SerializeField] ScenarioController scenario;
        [SerializeField] ExtinguisherController extinguishers;
        [SerializeField] GuidedStepTracker guidedSteps;
        [SerializeField] SessionRecorder sessionRecorder;
        [SerializeField] SessionArchiver sessionArchiver;

        [Header("UI")]
        [SerializeField] SetupHudView setupHud;
        [SerializeField] SimulationHudView simulationHud;
        [SerializeField] DebriefView debrief;
        [SerializeField] HistoryView history;

        void Awake()
        {
            if (!Validate())
            {
                enabled = false;
                return;
            }

            placement.Construct(raycastManager, planeManager, anchorManager, arCamera, tapDetector);
            planeVisibility.Construct(planeManager);
            trackingMonitor.Construct(arSession);
            extinguishers.Construct(arCamera);
            scenario.Construct(extinguishers, simulation);

            var sessions = new JsonSessionRepository(Application.persistentDataPath);
            sessionArchiver.Construct(sessions);
            history.Construct(sessions);
            simulation.Construct();
        }

        void Start() => simulation.Begin();

        void OnEnable()
        {
            EventBus.Subscribe<RestartRequested>(OnRestart);
            EventBus.Subscribe<MainMenuRequested>(OnMainMenu);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<RestartRequested>(OnRestart);
            EventBus.Unsubscribe<MainMenuRequested>(OnMainMenu);
        }

        // Until the Start screen exists, Main menu also starts a fresh session.
        void OnMainMenu(MainMenuRequested e) => Reload();
        void OnRestart(RestartRequested e) => Reload();

        void Reload()
        {
            // Reloading the scene gives a clean session; the AR session restarts with it.
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        bool Validate()
        {
            bool ok = true;
            ok &= Check(arSession, nameof(arSession));
            ok &= Check(raycastManager, nameof(raycastManager));
            ok &= Check(planeManager, nameof(planeManager));
            ok &= Check(anchorManager, nameof(anchorManager));
            ok &= Check(arCamera, nameof(arCamera));
            ok &= Check(simulation, nameof(simulation));
            ok &= Check(placement, nameof(placement));
            ok &= Check(planeVisibility, nameof(planeVisibility));
            ok &= Check(trackingMonitor, nameof(trackingMonitor));
            ok &= Check(tapDetector, nameof(tapDetector));
            ok &= Check(scenario, nameof(scenario));
            ok &= Check(extinguishers, nameof(extinguishers));
            ok &= Check(guidedSteps, nameof(guidedSteps));
            ok &= Check(setupHud, nameof(setupHud));
            ok &= Check(simulationHud, nameof(simulationHud));
            ok &= Check(sessionRecorder, nameof(sessionRecorder));
            ok &= Check(sessionArchiver, nameof(sessionArchiver));
            ok &= Check(debrief, nameof(debrief));
            ok &= Check(history, nameof(history));
            return ok;
        }

        bool Check(Object reference, string field)
        {
            if (reference != null)
                return true;
            Debug.LogError($"[Bootstrapper] '{field}' is not assigned.", this);
            return false;
        }
    }
}
