using FireReady.Core;
using UnityEngine;

namespace FireReady.Simulation
{
    /// <summary>
    /// Detects a side-to-side sweep from the yaw of the aim: two direction changes of
    /// at least <see cref="MinSwingDegrees"/> inside the time window count as sweeping.
    /// </summary>
    public sealed class SweepDetector
    {
        public float MinSwingDegrees { get; set; } = 8f;
        public float WindowSeconds { get; set; } = 2.5f;

        float m_Extreme;
        int m_Direction;
        bool m_HasSample;
        float m_LastReversal = float.NegativeInfinity;
        float m_PreviousReversal = float.NegativeInfinity;

        public SweepQuality Quality { get; private set; }

        public void Reset()
        {
            m_HasSample = false;
            m_Direction = 0;
            m_LastReversal = m_PreviousReversal = float.NegativeInfinity;
            Quality = SweepQuality.NotYet;
        }

        public SweepQuality Update(float yawDegrees, float time)
        {
            if (!m_HasSample)
            {
                m_Extreme = yawDegrees;
                m_HasSample = true;
            }
            else
            {
                float delta = Mathf.DeltaAngle(m_Extreme, yawDegrees);
                int sign = delta > 0f ? 1 : delta < 0f ? -1 : 0;

                if (m_Direction == 0)
                {
                    if (Mathf.Abs(delta) >= MinSwingDegrees)
                    {
                        m_Direction = sign;
                        m_Extreme = yawDegrees;
                    }
                }
                else if (sign == m_Direction)
                {
                    m_Extreme = yawDegrees; // still swinging the same way
                }
                else if (Mathf.Abs(delta) >= MinSwingDegrees)
                {
                    m_Direction = -m_Direction;
                    m_Extreme = yawDegrees;
                    m_PreviousReversal = m_LastReversal;
                    m_LastReversal = time;
                }
            }

            Quality = time - m_PreviousReversal <= WindowSeconds ? SweepQuality.Good : SweepQuality.NotYet;
            return Quality;
        }
    }
}
