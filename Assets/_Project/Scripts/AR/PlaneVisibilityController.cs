using FireReady.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FireReady.AR
{
    /// <summary>
    /// Shows plane visuals and keeps plane detection running only while the trainee
    /// is scanning or placing. Turning both off afterwards removes plane rendering
    /// and ARKit plane work from every frame of the simulation (a performance optimisation).
    /// </summary>
    public sealed class PlaneVisibilityController : MonoBehaviour
    {
        ARPlaneManager m_Planes;
        PlaneDetectionMode m_DetectionMode = PlaneDetectionMode.Horizontal;
        bool m_Visible = true;

        public void Construct(ARPlaneManager planes)
        {
            m_Planes = planes;
            if (m_Planes != null && m_Planes.requestedDetectionMode != PlaneDetectionMode.None)
                m_DetectionMode = m_Planes.requestedDetectionMode;
        }

        void OnEnable() => EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
        void OnDisable() => EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);

        void OnStateChanged(SimulationStateChanged e)
        {
            bool show = e.Current == SimulationStateId.Scanning || e.Current == SimulationStateId.Placing;
            SetVisible(show);
        }

        public void SetVisible(bool visible)
        {
            if (m_Planes == null || visible == m_Visible)
                return;

            m_Visible = visible;
            m_Planes.requestedDetectionMode = visible ? m_DetectionMode : PlaneDetectionMode.None;

            // Disable only the visualiser so ARKit keeps updating the planes underneath;
            // they come back instantly if the trainee moves the scene.
            foreach (var plane in m_Planes.trackables)
            {
                var visualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
                if (visualizer != null)
                    visualizer.enabled = visible;
            }
        }
    }
}
