using System.Collections.Generic;
using FireReady.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FireReady.AR
{
    /// <summary>
    /// Places the training scene on the floor (FR-05 to FR-09): raycasts from the
    /// centre of the screen, checks the plane is big enough, anchors the scene where
    /// the trainee taps, and moves it to a new anchor on request.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour
    {
        [SerializeField] PlacementSettings settings;
        [SerializeField] PlacementReticle reticlePrefab;
        [Tooltip("Placeholder content until scenarios load through Addressables.")]
        [SerializeField] GameObject scenePrefab;

        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        ARRaycastManager m_Raycasts;
        ARPlaneManager m_Planes;
        ARAnchorManager m_Anchors;
        Camera m_Camera;
        ScreenTapDetector m_Taps;

        PlacementReticle m_Reticle;
        ARAnchor m_Anchor;
        GameObject m_Scene;

        bool m_Active;
        bool m_PlaneAnnounced;
        float m_LastPlacedTime = float.NegativeInfinity;

        // Current aim.
        ARPlane m_TargetPlane;
        Pose m_TargetPose;
        bool m_HasTarget;
        bool m_TargetLargeEnough;
        Vector2 m_ReportedSize = new Vector2(-1f, -1f);
        bool m_ReportedHasTarget;
        bool m_ReportedLargeEnough;

        public Transform SceneRoot => m_Scene != null ? m_Scene.transform : null;

        public void Construct(ARRaycastManager raycasts, ARPlaneManager planes, ARAnchorManager anchors,
            Camera arCamera, ScreenTapDetector taps)
        {
            m_Raycasts = raycasts;
            m_Planes = planes;
            m_Anchors = anchors;
            m_Camera = arCamera;
            m_Taps = taps;

            if (m_Taps != null)
            {
                m_Taps.Tapped += OnTapped;
                m_Taps.DoubleTapped += OnDoubleTapped;
            }

            if (settings == null)
                settings = ScriptableObject.CreateInstance<PlacementSettings>();

            if (reticlePrefab != null)
            {
                m_Reticle = Instantiate(reticlePrefab);
                m_Reticle.name = "Placement Reticle";
                m_Reticle.Hide();
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<PlaceRequested>(OnPlaceRequested);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<PlaceRequested>(OnPlaceRequested);
        }

        void OnDestroy()
        {
            if (m_Taps != null)
            {
                m_Taps.Tapped -= OnTapped;
                m_Taps.DoubleTapped -= OnDoubleTapped;
            }
        }

        void Update()
        {
            if (m_Planes == null)
                return;

            if (!m_PlaneAnnounced && m_Planes.trackables.count > 0)
            {
                m_PlaneAnnounced = true;
                EventBus.Publish(new PlaneDetected());
            }

            if (m_Active)
                UpdateTarget();
        }

        void UpdateTarget()
        {
            m_HasTarget = false;
            m_TargetLargeEnough = false;
            m_TargetPlane = null;

            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (m_Raycasts.Raycast(centre, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                for (int i = 0; i < s_Hits.Count; i++)
                {
                    if (s_Hits[i].trackable is ARPlane plane && plane.alignment == PlaneAlignment.HorizontalUp)
                    {
                        m_TargetPlane = plane;
                        m_TargetPose = FacingCamera(s_Hits[i].pose.position);
                        m_HasTarget = true;
                        var size = plane.size;
                        m_TargetLargeEnough = Mathf.Min(size.x, size.y) >= settings.minimumPlaneSide;
                        break;
                    }
                }
            }

            if (m_Reticle != null)
            {
                if (m_HasTarget) m_Reticle.Show(m_TargetPose, m_TargetLargeEnough);
                else m_Reticle.Hide();
            }

            ReportTarget();
        }

        /// <summary>Yaw the scene so its front (+Z) faces the trainee.</summary>
        Pose FacingCamera(Vector3 position)
        {
            var toCamera = m_Camera.transform.position - position;
            toCamera.y = 0f;
            var rotation = toCamera.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : Quaternion.identity;
            return new Pose(position, rotation);
        }

        void ReportTarget()
        {
            var size = m_TargetPlane != null ? m_TargetPlane.size : Vector2.zero;
            bool sizeChanged = Mathf.Abs(size.x - m_ReportedSize.x) >= settings.areaReportStep ||
                               Mathf.Abs(size.y - m_ReportedSize.y) >= settings.areaReportStep;

            if (m_HasTarget == m_ReportedHasTarget && m_TargetLargeEnough == m_ReportedLargeEnough && !sizeChanged)
                return;

            m_ReportedHasTarget = m_HasTarget;
            m_ReportedLargeEnough = m_TargetLargeEnough;
            m_ReportedSize = size;
            EventBus.Publish(new PlacementTargetChanged(m_HasTarget, m_TargetLargeEnough, size));
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            m_Active = e.Current == SimulationStateId.Placing;
            if (!m_Active && m_Reticle != null)
                m_Reticle.Hide();

            if (m_Active)
            {
                // Force a fresh report so the UI is correct on entry.
                m_ReportedSize = new Vector2(-1f, -1f);
            }
        }

        void OnTapped(Vector2 screenPosition)
        {
            if (m_Active)
                PlaceAtTarget();
        }

        void OnDoubleTapped(Vector2 screenPosition)
        {
            // Moving is only offered once a scene exists and we are not already placing.
            if (m_Active || m_Scene == null)
                return;
            if (Time.unscaledTime - m_LastPlacedTime < settings.repositionCooldown)
                return;

            EventBus.Publish(new RepositionRequested());
        }

        void OnPlaceRequested(PlaceRequested e)
        {
            if (m_Active)
                PlaceAtTarget();
        }

        bool m_Placing;

        async void PlaceAtTarget()
        {
            if (m_Placing || !m_HasTarget || !m_TargetLargeEnough)
                return;

            m_Placing = true;
            try
            {
                var pose = m_TargetPose;
                var anchor = await CreateAnchorAsync(pose);
                if (anchor == null || this == null)
                {
                    Debug.LogWarning("[Placement] Could not create an anchor.");
                    return;
                }
                AttachScene(anchor);
            }
            finally
            {
                m_Placing = false;
            }
        }

        void AttachScene(ARAnchor anchor)
        {
            bool isReposition = m_Scene != null;
            if (!isReposition)
            {
                m_Scene = scenePrefab != null ? Instantiate(scenePrefab) : new GameObject("Training Scene");
                m_Scene.name = "Training Scene";
            }

            m_Scene.transform.SetParent(anchor.transform, false);
            m_Scene.transform.localPosition = Vector3.zero;
            m_Scene.transform.localRotation = Quaternion.identity;

            if (m_Anchor != null && m_Anchor != anchor)
                Destroy(m_Anchor.gameObject);
            m_Anchor = anchor;

            m_LastPlacedTime = Time.unscaledTime;
            EventBus.Publish(new ScenePlaced(m_Scene.transform, isReposition));
        }

        async Awaitable<ARAnchor> CreateAnchorAsync(Pose pose)
        {
            // A world anchor (not attached to the plane) survives ARKit merging or
            // removing planes, so the scene never disappears with a plane.
            if (m_Anchors != null && m_Anchors.enabled && m_Anchors.subsystem != null)
            {
                var result = await m_Anchors.TryAddAnchorAsync(pose);
                if (result.status.IsSuccess() && result.value != null)
                    return result.value;
            }

            // Fallback: add the anchor component directly at the same pose.
            var go = new GameObject("Scene Anchor");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            return go.AddComponent<ARAnchor>();
        }
    }
}
