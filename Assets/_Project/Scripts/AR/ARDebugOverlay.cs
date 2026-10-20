using System.Text;
using FireReady.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace FireReady.AR
{
    /// <summary>
    /// Development-only readout of AR state: session state, why tracking is
    /// limited, and how many planes have been found. Hidden in release builds.
    /// </summary>
    public class ARDebugOverlay : MonoBehaviour
    {
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] ARSession session;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        GUIStyle style;
        readonly StringBuilder m_Builder = new StringBuilder();

        void Awake()
        {
            if (planeManager == null)
                planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (session == null)
                session = FindAnyObjectByType<ARSession>();
        }

        static int CountStats()
        {
            int n = 0;
            foreach (var _ in DebugStats.All) n++;
            return n;
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(Screen.height * 0.013f),
                    normal = { textColor = Color.white }
                };
            }

            int planes = planeManager != null ? planeManager.trackables.count : -1;
            string manager = planeManager == null ? "missing"
                : planeManager.enabled ? planeManager.currentDetectionMode.ToString() : "disabled";

            var subsystem = session != null ? session.subsystem : null;
            string tracking = subsystem != null
                ? $"{subsystem.trackingState} ({subsystem.notTrackingReason})"
                : ARSession.notTrackingReason.ToString();

            m_Builder.Clear();
            foreach (var pair in DebugStats.All)
                m_Builder.Append('\n').Append(pair.Key).Append(": ").Append(pair.Value);

            string text =
                $"Session: {ARSession.state}\n" +
                $"Tracking: {tracking}\n" +
                $"Plane detection: {manager}\n" +
                $"Planes found: {planes}" + m_Builder;

            float pad = Screen.height * 0.01f;
            Rect safe = Screen.safeArea;
            var rect = new Rect(safe.x + pad, Screen.height - safe.yMax + Screen.height * 0.075f, safe.width * 0.62f - pad, style.fontSize * 10f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x - pad * 0.5f, rect.y - pad * 0.5f, rect.width, style.fontSize * (5.4f + 1.25f * CountStats())), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, text, style);
        }
#endif
    }
}
