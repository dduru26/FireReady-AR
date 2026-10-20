using UnityEngine;

namespace FireReady.Audio
{
    /// <summary>The sounds a scenario uses. It lives with the scenario content, so it loads and unloads with it.</summary>
    [CreateAssetMenu(menuName = "FireReady/Audio Cue Set", fileName = "AudioCueSet")]
    public sealed class AudioCueSet : ScriptableObject
    {
        [Header("Loops")]
        public AudioClip fireLoop;
        public AudioClip sprayLoop;
        public AudioClip alarmLoop;

        [Header("One-shots")]
        public AudioClip warning;
        public AudioClip success;
        public AudioClip flareUp;
        public AudioClip pinClick;
        public AudioClip pickUp;
        public AudioClip shock;
    }
}
