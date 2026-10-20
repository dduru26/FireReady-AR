using System.Text;
using FireReady.Core;
using UnityEngine;

namespace FireReady.Audio
{
    /// <summary>
    /// Plays every sound cue in response to EventBus events (FR-24) and publishes a
    /// caption line for each one, so the trainee can follow the audio with the
    /// sound off. Sounds come from the placed scenario's <see cref="ScenarioAudio"/>.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float fireVolumeMin = 0.25f;
        [SerializeField, Range(0f, 1f)] float fireVolumeMax = 0.9f;
        [SerializeField, Range(0f, 1f)] float alarmVolume = 0.3f;
        [SerializeField] float momentaryCaptionSeconds = 2.5f;

        AudioSource m_Fire;
        AudioSource m_Spray;
        AudioSource m_Alarm;
        AudioSource m_OneShot;
        AudioCueSet m_Cues;

        float m_Intensity;
        bool m_FireOn;
        bool m_SprayOn;
        bool m_AlarmOn;
        string m_Momentary;
        float m_MomentaryUntil;
        string m_LastCaption;
        readonly StringBuilder m_Caption = new StringBuilder(64);

        void Awake()
        {
            m_Fire = CreateSource("Fire", loop: true, spatial: 0.6f);
            m_Spray = CreateSource("Spray", loop: true, spatial: 0f);
            m_Alarm = CreateSource("Alarm", loop: true, spatial: 0f);
            m_OneShot = CreateSource("One-shots", loop: false, spatial: 0f);
        }

        AudioSource CreateSource(string name, bool loop, float spatial)
        {
            var go = new GameObject($"Audio {name}");
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = spatial;
            source.minDistance = 1.5f;
            source.maxDistance = 15f;
            return source;
        }

        void OnEnable()
        {
            EventBus.Subscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Subscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Subscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Subscribe<SprayStateChanged>(OnSpray);
            EventBus.Subscribe<AlarmRaised>(OnAlarm);
            EventBus.Subscribe<PinPulled>(OnPin);
            EventBus.Subscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Subscribe<MistakeRecorded>(OnMistake);
            EventBus.Subscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Subscribe<FireExtinguished>(OnExtinguished);
            EventBus.Subscribe<SessionEnded>(OnSessionEnded);
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<SessionReset>(OnReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<ScenePlaced>(OnScenePlaced);
            EventBus.Unsubscribe<ScenarioStarted>(OnScenarioStarted);
            EventBus.Unsubscribe<FireIntensityChanged>(OnIntensity);
            EventBus.Unsubscribe<SprayStateChanged>(OnSpray);
            EventBus.Unsubscribe<AlarmRaised>(OnAlarm);
            EventBus.Unsubscribe<PinPulled>(OnPin);
            EventBus.Unsubscribe<ExtinguisherPickedUp>(OnPickedUp);
            EventBus.Unsubscribe<MistakeRecorded>(OnMistake);
            EventBus.Unsubscribe<FireFlaredUp>(OnFlareUp);
            EventBus.Unsubscribe<FireExtinguished>(OnExtinguished);
            EventBus.Unsubscribe<SessionEnded>(OnSessionEnded);
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<SessionReset>(OnReset);
        }

        // ---- Scenario -----------------------------------------------------------------

        void OnScenePlaced(ScenePlaced e)
        {
            if (e.SceneRoot == null)
                return;
            var audio = e.SceneRoot.GetComponentInChildren<ScenarioAudio>(true);
            if (audio == null)
                return;
            m_Cues = audio.Cues;
            // Follow the fire so the sound comes from the scene, wherever it is moved.
            m_Fire.transform.SetParent(audio.FireSource, false);
            m_Fire.transform.localPosition = Vector3.zero;
        }

        void OnScenarioStarted(ScenarioStarted e)
        {
            if (m_Cues == null)
                return;
            m_Fire.clip = m_Cues.fireLoop;
            m_Spray.clip = m_Cues.sprayLoop;
            m_Alarm.clip = m_Cues.alarmLoop;
            m_FireOn = true;
            m_Fire.volume = fireVolumeMin;
            m_Fire.Play();
            RefreshCaption();
        }

        void OnIntensity(FireIntensityChanged e)
        {
            m_Intensity = e.Intensity;
            float t = Mathf.Clamp01(e.Intensity / 100f);
            m_Fire.volume = Mathf.Lerp(fireVolumeMin, fireVolumeMax, t);
            m_Fire.pitch = Mathf.Lerp(0.9f, 1.15f, t);
            RefreshCaption();
        }

        void OnSpray(SprayStateChanged e)
        {
            m_SprayOn = e.Spraying;
            if (e.Spraying && m_Spray.clip != null) m_Spray.Play();
            else m_Spray.Stop();
            RefreshCaption();
        }

        void OnAlarm(AlarmRaised e)
        {
            m_AlarmOn = true;
            if (m_Alarm.clip != null)
            {
                m_Alarm.volume = alarmVolume;
                m_Alarm.Play();
            }
            RefreshCaption();
        }

        void OnPin(PinPulled e) => PlayOnce(m_Cues != null ? m_Cues.pinClick : null, "Pin clicks out");
        void OnPickedUp(ExtinguisherPickedUp e) => PlayOnce(m_Cues != null ? m_Cues.pickUp : null, null);
        void OnMistake(MistakeRecorded e) => PlayOnce(m_Cues != null ? m_Cues.warning : null, "Warning tone");
        void OnFlareUp(FireFlaredUp e) => PlayOnce(m_Cues != null ? m_Cues.flareUp : null, "Whoosh: the fire flares up");

        void OnExtinguished(FireExtinguished e)
        {
            m_FireOn = false;
            m_Fire.Stop();
            PlayOnce(m_Cues != null ? m_Cues.success : null, "Success chime");
        }

        void OnSessionEnded(SessionEnded e)
        {
            m_SprayOn = false;
            m_Spray.Stop();
            if (e.Outcome == SessionOutcome.Injured)
                PlayOnce(m_Cues != null ? m_Cues.shock : null, "Electric buzz");
            if (e.Outcome != SessionOutcome.OutOfControl)
            {
                m_FireOn = false;
                m_Fire.Stop();
            }
            // The alarm keeps sounding through the debrief only if the fire won.
            if (e.Outcome == SessionOutcome.Extinguished)
            {
                m_AlarmOn = false;
                m_Alarm.Stop();
            }
            RefreshCaption();
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            bool paused = e.Current == SimulationStateId.Paused || e.Current == SimulationStateId.Placing;
            if (paused)
            {
                m_Fire.Pause();
                m_Spray.Pause();
            }
            else if (e.Current == SimulationStateId.Running)
            {
                if (m_FireOn) m_Fire.UnPause();
                if (m_SprayOn) m_Spray.UnPause();
            }
            if (e.Current == SimulationStateId.Completed)
            {
                // Quiet the room under the debrief.
                m_Alarm.Stop();
                m_Fire.Stop();
            }
        }

        void OnReset(SessionReset e)
        {
            m_Fire.Stop();
            m_Spray.Stop();
            m_Alarm.Stop();
            m_OneShot.Stop();
            m_Fire.transform.SetParent(transform, false);
            m_FireOn = m_SprayOn = m_AlarmOn = false;
            m_Momentary = null;
            m_Cues = null;
            RefreshCaption();
        }

        void PlayOnce(AudioClip clip, string caption)
        {
            if (clip != null)
                m_OneShot.PlayOneShot(clip);
            if (!string.IsNullOrEmpty(caption))
            {
                m_Momentary = caption;
                m_MomentaryUntil = Time.unscaledTime + momentaryCaptionSeconds;
            }
            RefreshCaption();
        }

        void Update()
        {
            if (m_Momentary != null && Time.unscaledTime >= m_MomentaryUntil)
            {
                m_Momentary = null;
                RefreshCaption();
            }
        }

        // ---- Captions ---------------------------------------------------------------------

        void RefreshCaption()
        {
            m_Caption.Clear();
            if (m_FireOn)
                Append(m_Intensity >= 70f ? "Flames roaring" : m_Intensity >= 35f ? "Fire crackling loudly" : "Fire crackling");
            if (m_SprayOn) Append("spray hissing");
            if (m_AlarmOn) Append("alarm ringing");
            if (m_Momentary != null) Append(m_Momentary);

            var caption = m_Caption.ToString();
            if (caption == m_LastCaption)
                return;
            m_LastCaption = caption;
            EventBus.Publish(new CaptionChanged(caption));
        }

        void Append(string text)
        {
            if (m_Caption.Length > 0) m_Caption.Append(" · ");
            m_Caption.Append(text);
        }
    }
}
