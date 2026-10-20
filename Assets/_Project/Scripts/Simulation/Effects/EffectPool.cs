using FireReady.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace FireReady.Simulation
{
    /// <summary>
    /// Object Pool for short particle effects (spray impacts, steam). Effects are
    /// created once, reused for every spawn and reset on return, so spraying causes
    /// no Instantiate/Destroy calls and no garbage after warm-up.
    /// </summary>
    public sealed class EffectPool : MonoBehaviour
    {
        [SerializeField] PooledEffect prefab;
        [SerializeField] string statsLabel = "Pool";
        [SerializeField, Min(1)] int prewarm = 12;
        [SerializeField, Min(1)] int maxSize = 48;

        ObjectPool<PooledEffect> m_Pool;
        int m_Created;
        int m_Spawns;
        float m_NextStats;

        public int CountActive => m_Pool?.CountActive ?? 0;
        public int CountInactive => m_Pool?.CountInactive ?? 0;
        public int Created => m_Created;
        public int Spawns => m_Spawns;

        void Awake()
        {
            m_Pool = new ObjectPool<PooledEffect>(Create, OnGet, OnRelease, OnDestroyEffect,
                collectionCheck: false, defaultCapacity: prewarm, maxSize: maxSize);

            // Warm up so the first spray does not allocate.
            var warm = new PooledEffect[prewarm];
            for (int i = 0; i < prewarm; i++) warm[i] = m_Pool.Get();
            for (int i = 0; i < prewarm; i++) m_Pool.Release(warm[i]);
        }

        public void Spawn(Vector3 position, Quaternion rotation, Color color)
        {
            if (prefab == null)
                return;
            var effect = m_Pool.Get();
            effect.Play(position, rotation, color);
            m_Spawns++;
        }

        internal void Release(PooledEffect effect) => m_Pool.Release(effect);

        PooledEffect Create()
        {
            var effect = Instantiate(prefab, transform);
            effect.Bind(this);
            effect.gameObject.SetActive(false);
            m_Created++;
            return effect;
        }

        static void OnGet(PooledEffect effect) => effect.gameObject.SetActive(true);

        static void OnRelease(PooledEffect effect)
        {
            effect.ResetForPool();
            effect.gameObject.SetActive(false);
        }

        static void OnDestroyEffect(PooledEffect effect)
        {
            if (effect != null)
                Destroy(effect.gameObject);
        }

        void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Time.unscaledTime < m_NextStats)
                return;
            m_NextStats = Time.unscaledTime + 0.5f;
            DebugStats.Set(statsLabel, $"{CountActive} active, {CountInactive} idle, {m_Created} created, {m_Spawns} spawns");
#endif
        }
    }
}
