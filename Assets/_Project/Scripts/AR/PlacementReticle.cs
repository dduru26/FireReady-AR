using UnityEngine;

namespace FireReady.AR
{
    /// <summary>The ring that shows where the scene will land. Pure view: no AR logic.</summary>
    public sealed class PlacementReticle : MonoBehaviour
    {
        [SerializeField] Renderer ringRenderer;
        [SerializeField] Color validColor = Color.white;
        [SerializeField] Color invalidColor = new Color(0.96f, 0.77f, 0.09f, 1f); // ISO 7010 warning yellow

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock m_Block;
        bool? m_LastValid;

        public void Show(Pose pose, bool valid)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.SetPositionAndRotation(pose.position, pose.rotation);

            if (m_LastValid != valid && ringRenderer != null)
            {
                m_Block ??= new MaterialPropertyBlock();
                ringRenderer.GetPropertyBlock(m_Block);
                m_Block.SetColor(s_BaseColor, valid ? validColor : invalidColor);
                ringRenderer.SetPropertyBlock(m_Block);
                m_LastValid = valid;
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
