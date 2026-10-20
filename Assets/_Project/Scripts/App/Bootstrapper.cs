using FireReady.AR;
using FireReady.Simulation;
using FireReady.UI;
using UnityEngine;
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

        [Header("UI")]
        [SerializeField] SetupHudView setupHud;

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
            simulation.Construct();
        }

        void Start() => simulation.Begin();

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
            ok &= Check(setupHud, nameof(setupHud));
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
