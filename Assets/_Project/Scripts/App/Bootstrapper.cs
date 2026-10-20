using FireReady.AR;
using FireReady.Core;
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

        [Header("UI")]
        [SerializeField] SetupHudView setupHud;
        [SerializeField] SimulationHudView simulationHud;

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
            simulation.Construct();
        }

        void Start() => simulation.Begin();

        void OnEnable() => EventBus.Subscribe<RestartRequested>(OnRestart);
        void OnDisable() => EventBus.Unsubscribe<RestartRequested>(OnRestart);

        void OnRestart(RestartRequested e)
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
