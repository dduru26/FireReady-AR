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

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        GUIStyle style;

        void Awake()
        {
            if (planeManager == null)
                planeManager = FindAnyObjectByType<ARPlaneManager>();
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(Screen.height * 0.018f),
                    normal = { textColor = Color.white }
                };
            }

            int planes = planeManager != null ? planeManager.trackables.count : -1;
            string manager = planeManager == null ? "missing"
                : planeManager.enabled ? planeManager.currentDetectionMode.ToString() : "disabled";

            string text =
                $"Session: {ARSession.state}\n" +
                $"Tracking: {ARSession.notTrackingReason}\n" +
                $"Plane detection: {manager}\n" +
                $"Planes found: {planes}";

            float pad = Screen.height * 0.01f;
            Rect safe = Screen.safeArea;
            var rect = new Rect(safe.x + pad, Screen.height - safe.yMax + pad, safe.width * 0.8f, style.fontSize * 6f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x - pad * 0.5f, rect.y - pad * 0.5f, rect.width, style.fontSize * 5.4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(rect, text, style);
        }
#endif
    }
}
