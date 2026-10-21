using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>Scales the flames, core, smoke, embers and firelight with the fire's intensity.</summary>
    public sealed class FireVisuals : MonoBehaviour
    {
        [SerializeField] ParticleSystem flames;
        [SerializeField] ParticleSystem core;
        [SerializeField] Light fireLight;
        [SerializeField] float maxCoreRate = 60f;
        [SerializeField] float maxLightIntensity = 4f;
        [SerializeField] float maxLightRange = 3f;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] ParticleSystem embers;
        [SerializeField] float maxFlameRate = 150f;
        [SerializeField] float maxSmokeRate = 26f;
        [SerializeField] float maxEmberRate = 12f;
        [Tooltip("Flame particle size range (metres) at full intensity.")]
        [SerializeField] Vector2 flameParticleSize = new Vector2(0.16f, 0.36f);
        [Tooltip("Size scale at intensity 0 and at intensity 100.")]
        [SerializeField] Vector2 flameScale = new Vector2(0.7f, 2.2f);

        float m_Applied = -1f;
        float m_LightBase;
        float m_Seed;

        void Awake() => m_Seed = Random.value * 100f;

        void Update()
        {
            // Firelight flickers around its base brightness.
            if (fireLight == null || m_LightBase <= 0f)
                return;
            float n = Mathf.PerlinNoise(m_Seed, Time.time * 6f);
            fireLight.intensity = m_LightBase * Mathf.Lerp(0.7f, 1.15f, n);
        }

        public void Apply(float intensity)
        {
            if (Mathf.Abs(intensity - m_Applied) < 0.25f)
                return;
            m_Applied = intensity;

            float t = Mathf.Clamp01(intensity / 100f);
            SetRate(flames, t <= 0f ? 0f : Mathf.Lerp(12f, maxFlameRate, t));
            SetRate(smoke, t <= 0f ? 2f : Mathf.Lerp(2f, maxSmokeRate, t));
            SetRate(embers, t * maxEmberRate);
            SetRate(core, t <= 0f ? 0f : Mathf.Lerp(10f, maxCoreRate, t));

            if (fireLight != null)
            {
                m_LightBase = t * maxLightIntensity;
                fireLight.range = Mathf.Lerp(1f, maxLightRange, t);
                fireLight.enabled = t > 0f;
            }

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
