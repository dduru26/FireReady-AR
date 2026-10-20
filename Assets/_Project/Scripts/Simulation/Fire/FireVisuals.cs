using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>Scales the flame and smoke particles with the fire's intensity.</summary>
    public sealed class FireVisuals : MonoBehaviour
    {
        [SerializeField] ParticleSystem flames;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] ParticleSystem embers;
        [SerializeField] float maxFlameRate = 90f;
        [SerializeField] float maxSmokeRate = 18f;
        [SerializeField] float maxEmberRate = 12f;
        [Tooltip("Flame particle size range (metres) at full intensity.")]
        [SerializeField] Vector2 flameParticleSize = new Vector2(0.1f, 0.24f);
        [Tooltip("Size scale at intensity 0 and at intensity 100.")]
        [SerializeField] Vector2 flameScale = new Vector2(0.5f, 1.6f);

        float m_Applied = -1f;

        public void Apply(float intensity)
        {
            if (Mathf.Abs(intensity - m_Applied) < 0.25f)
                return;
            m_Applied = intensity;

            float t = Mathf.Clamp01(intensity / 100f);
            SetRate(flames, t <= 0f ? 0f : Mathf.Lerp(12f, maxFlameRate, t));
            SetRate(smoke, t <= 0f ? 2f : Mathf.Lerp(2f, maxSmokeRate, t));
            SetRate(embers, t * maxEmberRate);

            if (flames != null)
            {
                var main = flames.main;
                float scale = Mathf.Lerp(flameScale.x, flameScale.y, t);
                main.startSize = new ParticleSystem.MinMaxCurve(flameParticleSize.x * scale, flameParticleSize.y * scale);
            }
        }

        static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null)
                return;
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = rate;
        }
    }
}
