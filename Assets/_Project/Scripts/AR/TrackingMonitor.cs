using FireReady.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace FireReady.AR
{
    /// <summary>
    /// Watches ARKit's tracking quality and announces when it is lost or back.
    /// Short dips are ignored so a quick hand movement does not pause the simulation.
    /// </summary>
    public sealed class TrackingMonitor : MonoBehaviour
    {
        [Tooltip("Seconds of poor tracking before it counts as lost.")]
        [SerializeField] float lostAfter = 1.0f;
        [Tooltip("Seconds of good tracking before it counts as restored.")]
        [SerializeField] float restoredAfter = 0.5f;

        ARSession m_Session;
        bool m_Tracking;
        float m_Timer;

        public bool IsTracking => m_Tracking;

        public void Construct(ARSession session) => m_Session = session;

        void Update()
        {
            var subsystem = m_Session != null ? m_Session.subsystem : null;
            bool tracking = subsystem != null && subsystem.running &&
                            subsystem.trackingState == TrackingState.Tracking;

            if (tracking == m_Tracking)
            {
                m_Timer = 0f;
                return;
            }

            m_Timer += Time.unscaledDeltaTime;
            if (m_Timer < (tracking ? restoredAfter : lostAfter))
                return;

            m_Tracking = tracking;
            m_Timer = 0f;

            if (tracking)
            {
                EventBus.Publish(new TrackingRestored());
            }
            else
            {
                var reason = subsystem != null ? subsystem.notTrackingReason.ToString() : "No AR session";
                EventBus.Publish(new TrackingLost(reason));
            }
        }
    }
}
