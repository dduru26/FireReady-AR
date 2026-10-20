using UnityEngine;

namespace FireReady.Audio
{
    /// <summary>Sits on a scenario prefab and hands its sounds to the AudioManager when placed.</summary>
    public sealed class ScenarioAudio : MonoBehaviour
    {
        [SerializeField] AudioCueSet cues;
        [Tooltip("Where the fire sound comes from.")]
        [SerializeField] Transform fireSource;

        public AudioCueSet Cues => cues;
        public Transform FireSource => fireSource != null ? fireSource : transform;
    }
}
