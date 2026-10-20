using UnityEngine;

namespace FireReady.UI
{
    /// <summary>Keeps this RectTransform inside the screen's safe area (notch and home bar).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform m_Rect;
        Rect m_Applied;
        Vector2Int m_ScreenSize;

        void Awake() => m_Rect = (RectTransform)transform;
        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != m_Applied || m_ScreenSize.x != Screen.width || m_ScreenSize.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            if (m_Rect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            var safe = Screen.safeArea;
            m_Applied = safe;
            m_ScreenSize = new Vector2Int(Screen.width, Screen.height);

            m_Rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            m_Rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            m_Rect.offsetMin = Vector2.zero;
            m_Rect.offsetMax = Vector2.zero;
        }
    }
}
