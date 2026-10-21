using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Base class for every fire (Template Method). <see cref="Step"/> fixes the order
    /// of one simulation tick — grow, apply the spray, react to the agent, update
    /// the visuals, check the thresholds — while each fire class supplies its own
    /// growth rate and its own reaction to each agent.
    /// </summary>
    public abstract class FireHazard : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField, Range(0f, 100f)] float startIntensity = 30f;
        [SerializeField, Range(0f, 100f)] float evacuateThreshold = 70f;
        [SerializeField] float baseGrowthPerSecond = 0.6f;
        [SerializeField] float growthPerIntensity = 0.02f;
        [SerializeField] float suppressionPerSecond = 16f;

        [Header("Geometry")]
        [Tooltip("Centre of the base of the flames; the trainee should aim here.")]
        [SerializeField] Transform flameBase;
        [SerializeField] float baseRadius = 0.18f;

        [Header("Visuals")]
        [SerializeField] FireVisuals visuals;

        float m_Intensity;
        float m_LastReported = -1f;
        FireTrend m_LastTrend;
        bool m_Ended;

        public float Intensity => m_Intensity;
        public float EvacuateThreshold => evacuateThreshold;
        public bool IsOut => m_Intensity <= 0f;
        public Vector3 BasePosition => flameBase != null ? flameBase.position : transform.position;
        public float BaseRadius => baseRadius;
        public abstract FireClass FireClass { get; }

        /// <summary>The flames reach about this high above the base (metres).</summary>
        public float FlameHeight => Mathf.Lerp(0.15f, 0.9f, m_Intensity / 100f);

        protected virtual void Awake()
        {
            m_Intensity = startIntensity;
            visuals?.Apply(m_Intensity);
        }

        /// <summary>How this fire reacts to an agent. Each fire class answers differently.</summary>
        public abstract AgentResponse ResponseTo(AgentType agent);

        /// <summary>Growth per second before any spray (hook; may depend on state such as power).</summary>
        protected virtual float GrowthRate() => baseGrowthPerSecond + m_Intensity * growthPerIntensity;

        /// <summary>
        /// Called when an agent that is dangerous for this fire is sprayed on it.
        /// Returns the outcome that ends the session, or None to keep going.
        /// </summary>
        protected virtual SessionOutcome OnDangerousAgent(AgentType agent, float deltaTime) => SessionOutcome.None;

        /// <summary>One simulation tick (the template method).</summary>
        /// <param name="spray">What is hitting the fire this tick, if anything.</param>
        public SessionOutcome Step(float deltaTime, in SprayContact spray)
        {
            if (m_Ended)
                return SessionOutcome.None;

            float change = GrowthRate() * deltaTime;
            var outcome = SessionOutcome.None;

            if (spray.Active)
            {
                var response = ResponseTo(spray.Agent);
                if (response.Suitability == AgentSuitability.Dangerous)
                    outcome = OnDangerousAgent(spray.Agent, deltaTime);
                else
                    change -= suppressionPerSecond * response.Effectiveness * spray.Quality * deltaTime;
            }

            SetIntensity(m_Intensity + change);

            if (outcome == SessionOutcome.None)
            {
                if (m_Intensity <= 0f) outcome = SessionOutcome.Extinguished;
                else if (m_Intensity >= 100f) outcome = SessionOutcome.OutOfControl;
            }

            if (outcome != SessionOutcome.None)
            {
                m_Ended = true;
                if (outcome == SessionOutcome.Extinguished)
                    EventBus.Publish(new FireExtinguished());
            }
            return outcome;
        }

        /// <summary>Sudden growth, e.g. water on burning oil.</summary>
        protected void FlareUp(float amount, string reason)
        {
            SetIntensity(m_Intensity + amount);
            EventBus.Publish(new FireFlaredUp(amount, reason));
        }

        void SetIntensity(float value)
        {
            float previous = m_Intensity;
            m_Intensity = Mathf.Clamp(value, 0f, 100f);
            visuals?.Apply(m_Intensity);

            var trend = m_Intensity > previous + 0.0001f ? FireTrend.Growing
                      : m_Intensity < previous - 0.0001f ? FireTrend.Shrinking
                      : FireTrend.Steady;

            if (Mathf.Abs(m_Intensity - m_LastReported) >= 0.5f || trend != m_LastTrend ||
                m_Intensity <= 0f || m_Intensity >= 100f)
            {
                m_LastReported = m_Intensity;
                m_LastTrend = trend;
                EventBus.Publish(new FireIntensityChanged(m_Intensity, trend));
            }
        }

        /// <summary>Sets the fire to a given intensity (used by tests and tools).</summary>
        public void Ignite(float intensity)
        {
            m_Ended = false;
            SetIntensity(intensity);
        }

        /// <summary>Re-sends the current intensity (used when the scenario starts).</summary>
        public void Announce() => EventBus.Publish(new FireIntensityChanged(m_Intensity, FireTrend.Steady));
    }

    /// <summary>What the extinguisher is doing to the fire during one tick.</summary>
    public readonly struct SprayContact
    {
        public readonly bool Active;
        public readonly AgentType Agent;
        /// <summary>0 to 1: aim, distance and sweep combined.</summary>
        public readonly float Quality;

        public SprayContact(AgentType agent, float quality)
        {
            Active = true;
            Agent = agent;
            Quality = Mathf.Clamp01(quality);
        }

        public static SprayContact None => default;
    }
}
