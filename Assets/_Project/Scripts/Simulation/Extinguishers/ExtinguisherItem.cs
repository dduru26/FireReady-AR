using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// One extinguisher on the rack. It can be picked up (moved into the trainee's
    /// hand), have its pin pulled, spray, and be put back where it came from.
    /// </summary>
    public sealed class ExtinguisherItem : MonoBehaviour
    {
        [SerializeField] ExtinguisherDefinition definition;
        [SerializeField] GameObject pin;
        [SerializeField] ParticleSystem spray;
        [SerializeField] Transform nozzle;

        Transform m_RackParent;
        Vector3 m_RackPosition;
        Quaternion m_RackRotation;
        Vector3 m_RackScale;
        Collider[] m_Colliders;

        public ExtinguisherDefinition Definition => definition;
        public bool PinPulled { get; private set; }
        public float RemainingSeconds { get; private set; }
        public float RemainingFraction => definition.capacitySeconds > 0f ? RemainingSeconds / definition.capacitySeconds : 0f;
        public bool IsEmpty => RemainingSeconds <= 0f;
        public Transform Nozzle => nozzle != null ? nozzle : transform;

        void Awake()
        {
            RemainingSeconds = definition != null ? definition.capacitySeconds : 0f;
            m_Colliders = GetComponentsInChildren<Collider>(true);
            SetSpraying(false);
        }

        public void PickUp(Transform hand)
        {
            m_RackParent = transform.parent;
            m_RackPosition = transform.localPosition;
            m_RackRotation = transform.localRotation;
            m_RackScale = transform.localScale;

            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            SetCollidersEnabled(false);
        }

        public void ReturnToRack()
        {
            SetSpraying(false);
            transform.SetParent(m_RackParent, false);
            transform.localPosition = m_RackPosition;
            transform.localRotation = m_RackRotation;
            transform.localScale = m_RackScale;
            SetCollidersEnabled(true);
        }

        public void PullPin()
        {
            PinPulled = true;
            if (pin != null)
                pin.SetActive(false);
        }

        public void Discharge(float seconds) => RemainingSeconds = Mathf.Max(0f, RemainingSeconds - seconds);

        public void SetSpraying(bool spraying)
        {
            if (spray == null)
                return;
            var emission = spray.emission;
            emission.enabled = spraying;
            if (spraying && !spray.isPlaying)
                spray.Play(true);
        }

        public void AimSpray(Vector3 direction)
        {
            if (spray != null && direction.sqrMagnitude > 0.0001f)
                spray.transform.rotation = Quaternion.LookRotation(direction);
        }

        void SetCollidersEnabled(bool enabled)
        {
            foreach (var c in m_Colliders)
                c.enabled = enabled;
        }
    }
}
