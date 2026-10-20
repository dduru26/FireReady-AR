using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>Turns a world-space label to face the camera (yaw only).</summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        Camera m_Camera;

        void LateUpdate()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera == null) return;
            }

            var away = transform.position - m_Camera.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
