using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Everything the trainee does with an extinguisher: pick it up from the rack by
    /// tapping it, pull the pin, hold Squeeze to spray, and put it back. Each frame it
    /// judges distance, aim and sweep and turns them into a spray quality for the fire.
    /// </summary>
    public sealed class ExtinguisherController : MonoBehaviour
    {
        [Header("Hand")]
        [Tooltip("Where the held extinguisher sits relative to the camera.")]
        [SerializeField] Vector3 handOffset = new Vector3(0.13f, -0.33f, 0.45f);
        [SerializeField] float handScale = 0.6f;
        [SerializeField] Vector3 handEuler = new Vector3(-8f, -12f, 0f);
        [SerializeField] float pickUpRange = 6f;

        [Header("Effects")]
        [SerializeField] EffectPool steamPool;
        [SerializeField] EffectPool splashPool;
        [SerializeField] float impactInterval = 0.08f;

        [Header("Mistakes")]
        [SerializeField] float tooCloseGrace = 1.0f;

        Camera m_Camera;
        Transform m_Hand;
        FireHazard m_Fire;

        ExtinguisherItem m_Held;
        bool m_Running;
        bool m_SqueezeHeld;
        bool m_Spraying;
        float m_NextImpact;
        float m_NextRemainingReport;
        float m_TooCloseTimer;
        bool m_WarnedPin;
        bool m_WarnedAgent;
        bool m_WarnedTooClose;

        readonly SweepDetector m_Sweep = new SweepDetector();
        DistanceBand m_Band = DistanceBand.TooFar;
        AimQuality m_Aim = AimQuality.OffTarget;
        float m_Distance = -1f;
        float m_ReportedDistance = -10f;
        DistanceBand m_ReportedBand = (DistanceBand)(-1);
        AimQuality m_ReportedAim = (AimQuality)(-1);
        SweepQuality m_ReportedSweep = (SweepQuality)(-1);

        public bool IsHolding => m_Held != null;
        public bool IsSpraying => m_Spraying;

        /// <summary>What the spray is doing to the fire right now.</summary>
        public SprayContact Contact { get; private set; }

        public void Construct(Camera arCamera)
        {
            m_Camera = arCamera;
            var hand = new GameObject("Extinguisher Hand").transform;
            hand.SetParent(m_Camera.transform, false);
            hand.localPosition = handOffset;
            hand.localRotation = Quaternion.Euler(handEuler);
            hand.localScale = Vector3.one * handScale;
            m_Hand = hand;
        }

        float m_FloorY;

        public void SetScene(FireHazard fire, float floorY)
        {
            m_Fire = fire;
            m_FloorY = floorY;
        }

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<ScreenTapped>(OnScreenTapped);
            EventBus.Subscribe<PullPinRequested>(OnPullPin);
            EventBus.Subscribe<SqueezeChanged>(OnSqueeze);
            EventBus.Subscribe<ReturnExtinguisherRequested>(OnReturnRequested);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
            EventBus.Subscribe<SessionReset>(OnSessionReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<ScreenTapped>(OnScreenTapped);
            EventBus.Unsubscribe<PullPinRequested>(OnPullPin);
            EventBus.Unsubscribe<SqueezeChanged>(OnSqueeze);
            EventBus.Unsubscribe<ReturnExtinguisherRequested>(OnReturnRequested);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
            EventBus.Unsubscribe<SessionReset>(OnSessionReset);
        }

        void OnSessionReset(SessionReset e)
        {
            m_Running = false;
            m_SqueezeHeld = false;
            SetSpraying(false);
            // The held extinguisher lives on the camera, not in the placed scene.
            if (m_Held != null)
                Destroy(m_Held.gameObject);
            m_Held = null;
            m_Fire = null;
            Contact = SprayContact.None;
            m_ReportedBand = (DistanceBand)(-1);
            m_ReportedAim = (AimQuality)(-1);
            m_ReportedSweep = (SweepQuality)(-1);
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            m_Running = e.Current == SimulationStateId.Running;
            if (!m_Running)
            {
                // A pause or a move always lets go of the trigger.
                m_SqueezeHeld = false;
                SetSpraying(false);
            }
        }

        void OnSessionEnded(SessionEnded e)
        {
            m_Running = false;
            m_SqueezeHeld = false;
            SetSpraying(false);
        }

        // ---- Commands ------------------------------------------------------------

        void OnScreenTapped(ScreenTapped e)
        {
            if (!m_Running || m_Camera == null)
                return;

            var ray = m_Camera.ScreenPointToRay(e.Position);
            if (!Physics.Raycast(ray, out var hit, pickUpRange))
                return;

            var item = hit.collider.GetComponentInParent<ExtinguisherItem>();
            if (item == null || item == m_Held)
                return;

            if (m_Held != null)
                ReturnHeld();
            PickUp(item);
        }

        void PickUp(ExtinguisherItem item)
        {
            m_Held = item;
            item.PickUp(m_Hand);
            m_WarnedPin = m_WarnedAgent = m_WarnedTooClose = false;
            m_Sweep.Reset();

            var def = item.Definition;
            var suitability = m_Fire != null ? m_Fire.ResponseTo(def.agent).Suitability : AgentSuitability.Suitable;
            EventBus.Publish(new ExtinguisherPickedUp(def.agent, def.displayName, def.bandColor, suitability));
            if (item.PinPulled)
                EventBus.Publish(new PinPulled());
            ReportRemaining(force: true);
        }

        void OnReturnRequested(ReturnExtinguisherRequested e)
        {
            if (m_Held != null)
                ReturnHeld();
        }

        void ReturnHeld()
        {
            SetSpraying(false);
            m_Held.ReturnToRack();
            m_Held = null;
            Contact = SprayContact.None;
            EventBus.Publish(new ExtinguisherReturned());
        }

        void OnPullPin(PullPinRequested e)
        {
            if (m_Held == null || m_Held.PinPulled)
                return;
            m_Held.PullPin();
            EventBus.Publish(new PinPulled());
        }

        void OnSqueeze(SqueezeChanged e)
        {
            m_SqueezeHeld = e.Held && m_Running;
            if (m_SqueezeHeld && m_Held != null && !m_Held.PinPulled && !m_WarnedPin)
            {
                m_WarnedPin = true;
                EventBus.Publish(new MistakeRecorded("The pin is still in", "Pull the pin before you squeeze."));
            }
        }

        // ---- Per frame -------------------------------------------------------------

        void Update()
        {
            if (!m_Running || m_Camera == null)
                return;

            EvaluateTechnique();

            bool canSpray = m_Held != null && m_SqueezeHeld && m_Held.PinPulled && !m_Held.IsEmpty;
            SetSpraying(canSpray);

            if (m_Spraying)
            {
                float dt = Time.deltaTime;
                m_Held.Discharge(dt);
                m_Held.AimSpray(m_Camera.transform.forward);
                ReportRemaining(force: false);
                SpawnImpacts();
                CheckMistakes(dt);

                float quality = TechniqueEvaluator.AimFactor(m_Aim) *
                                TechniqueEvaluator.DistanceFactor(m_Band) *
                                TechniqueEvaluator.SweepFactor(m_Sweep.Quality);
                Contact = new SprayContact(m_Held.Definition.agent, quality);

                if (m_Held.IsEmpty)
                {
                    SetSpraying(false);
                    ReportRemaining(force: true);
                    EventBus.Publish(new MistakeRecorded("Extinguisher empty", "Put it back and pick up another, or evacuate."));
                }
            }
            else
            {
                Contact = SprayContact.None;
            }
        }

        void EvaluateTechnique()
        {
            if (m_Fire == null)
                return;

            var cam = m_Camera.transform;
            m_Distance = TechniqueEvaluator.HorizontalDistance(cam.position, m_Fire.BasePosition);
            m_Band = TechniqueEvaluator.Band(m_Distance);
            m_Aim = TechniqueEvaluator.Aim(new Ray(cam.position, cam.forward), m_Fire.BasePosition,
                                           m_Fire.BaseRadius, m_Fire.FlameHeight);

            if (m_Spraying)
                m_Sweep.Update(cam.eulerAngles.y, Time.time);

            if (m_Band != m_ReportedBand || m_Aim != m_ReportedAim || m_Sweep.Quality != m_ReportedSweep ||
                Mathf.Abs(m_Distance - m_ReportedDistance) >= 0.1f)
            {
                m_ReportedBand = m_Band;
                m_ReportedAim = m_Aim;
                m_ReportedSweep = m_Sweep.Quality;
                m_ReportedDistance = m_Distance;
                EventBus.Publish(new TechniqueChanged(m_Distance, m_Band, m_Aim, m_Sweep.Quality));
            }
        }

        void CheckMistakes(float dt)
        {
            if (m_Fire != null && !m_WarnedAgent)
            {
                var response = m_Fire.ResponseTo(m_Held.Definition.agent);
                if (response.Suitability != AgentSuitability.Suitable)
                {
                    m_WarnedAgent = true;
                    var title = response.Suitability == AgentSuitability.Dangerous
                        ? $"{m_Held.Definition.displayName} is dangerous here"
                        : $"{m_Held.Definition.displayName} is a poor choice here";
                    EventBus.Publish(new MistakeRecorded(title, response.Warning));
                }
            }

            m_TooCloseTimer = m_Band == DistanceBand.TooClose ? m_TooCloseTimer + dt : 0f;
            if (m_TooCloseTimer >= tooCloseGrace && !m_WarnedTooClose)
            {
                m_WarnedTooClose = true;
                EventBus.Publish(new MistakeRecorded("Too close to the fire",
                    $"Step back. Stay {TechniqueEvaluator.MinDistance:0.0} to {TechniqueEvaluator.MaxDistance:0} m away."));
            }
        }

        void SpawnImpacts()
        {
            if (Time.time < m_NextImpact || m_Fire == null)
                return;
            m_NextImpact = Time.time + impactInterval;

            var def = m_Held.Definition;
            var cam = m_Camera.transform;
            if (m_Aim == AimQuality.OnBase || m_Aim == AimQuality.TooHigh)
            {
                // Steam and smoke where the agent meets the flames.
                var jitter = Random.insideUnitSphere * m_Fire.BaseRadius * 0.6f;
                jitter.y = Mathf.Abs(jitter.y) * 0.5f;
                var at = m_Fire.BasePosition + jitter + (m_Aim == AimQuality.TooHigh ? Vector3.up * 0.3f : Vector3.zero);
                if (steamPool != null) steamPool.Spawn(at, Quaternion.identity, def.impactColor);
            }
            else if (m_Band != DistanceBand.TooFar)
            {
                // Splash where the spray lands on the floor.
                var floorY = m_FloorY;
                var forward = cam.forward;
                if (forward.y < -0.05f)
                {
                    float t = (floorY - cam.position.y) / forward.y;
                    t = Mathf.Min(t, TechniqueEvaluator.MaxDistance);
                    var at = cam.position + forward * t;
                    if (splashPool != null) splashPool.Spawn(at, Quaternion.identity, def.impactColor);
                }
            }
        }

        void SetSpraying(bool spraying)
        {
            if (m_Held != null)
                m_Held.SetSpraying(spraying);
            if (spraying == m_Spraying)
                return;
            m_Spraying = spraying;
            if (!spraying)
            {
                m_Sweep.Reset();
                Contact = SprayContact.None;
            }
            EventBus.Publish(new SprayStateChanged(spraying));
        }

        void ReportRemaining(bool force)
        {
            if (m_Held == null)
                return;
            if (!force && Time.unscaledTime < m_NextRemainingReport)
                return;
            m_NextRemainingReport = Time.unscaledTime + 0.1f;
            EventBus.Publish(new AgentRemainingChanged(m_Held.RemainingSeconds, m_Held.RemainingFraction));
        }
    }
}
