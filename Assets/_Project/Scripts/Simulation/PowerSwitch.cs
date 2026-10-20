using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>The wall switch in S3. Tapping it cuts the power to the burning strip.</summary>
    public sealed class PowerSwitch : MonoBehaviour
    {
        [SerializeField] ElectricalFire fire;
        [SerializeField] Transform rocker;
        [SerializeField] Renderer indicator;
        [SerializeField] Color onColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color offColor = new Color(0.2f, 0.2f, 0.2f);

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock m_Block;

        public bool IsOn { get; private set; } = true;

        void Awake() => SetIndicator(onColor);

        public void SwitchOff()
        {
            if (!IsOn)
                return;
            IsOn = false;
            if (fire != null)
                fire.CutPower();
            if (rocker != null)
                rocker.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            SetIndicator(offColor);
            EventBus.Publish(new PowerCut());
        }

        void SetIndicator(Color color)
        {
            if (indicator == null)
                return;
            m_Block ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(m_Block);
            m_Block.SetColor(s_BaseColor, color);
            indicator.SetPropertyBlock(m_Block);
        }
    }
}
