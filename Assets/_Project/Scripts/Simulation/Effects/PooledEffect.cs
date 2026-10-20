using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// A short particle burst that returns itself to its pool when it finishes.
    /// The particle system's Stop Action must be set to Callback.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledEffect : MonoBehaviour
    {
        ParticleSystem m_System;
        EffectPool m_Owner;
        bool m_InUse;

        internal void Bind(EffectPool owner)
        {
            m_Owner = owner;
            m_System = GetComponent<ParticleSystem>();
            var main = m_System.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            main.playOnAwake = false;
            main.loop = false;
        }

        internal void Play(Vector3 position, Quaternion rotation, Color color)
        {
            m_InUse = true;
            transform.SetPositionAndRotation(position, rotation);
            var main = m_System.main;
            main.startColor = color;
            m_System.Clear(true);
            m_System.Play(true);
        }

        internal void ResetForPool()
        {
            m_InUse = false;
            m_System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Called by Unity when every particle has died.
        void OnParticleSystemStopped()
        {
            if (m_InUse && m_Owner != null)
                m_Owner.Release(this);
        }
    }
}
