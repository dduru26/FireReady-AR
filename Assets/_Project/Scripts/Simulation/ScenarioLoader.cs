using System.Collections;
using FireReady.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FireReady.Simulation
{
    /// <summary>
    /// Loads the chosen scenario's prefab through Addressables when a scenario
    /// starts and releases it when the session is reset (Restart or Main menu),
    /// so only one scenario's models, effects and sounds are in memory at a time.
    /// </summary>
    public sealed class ScenarioLoader : MonoBehaviour
    {
        [Tooltip("Keep the loading screen up at least this long so it can be read.")]
        [SerializeField] float minimumLoadingSeconds = 0.8f;

        AsyncOperationHandle<GameObject> m_Handle;
        Coroutine m_Loading;
        string m_LoadedAddress;

        public bool HasContent => m_Handle.IsValid() && m_Handle.Status == AsyncOperationStatus.Succeeded;

        void OnEnable()
        {
            EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Subscribe<SessionReset>(OnSessionReset);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);
            EventBus.Unsubscribe<SessionReset>(OnSessionReset);
        }

        void OnDestroy() => Release();

        void OnStateChanged(SimulationStateChanged e)
        {
            if (e.Current != SimulationStateId.Loading)
                return;

            var manager = GetComponent<SimulationManager>();
            var selection = manager != null ? manager.Selection : null;
            if (!selection.HasValue || selection.Value.Scenario == null)
            {
                EventBus.Publish(new ScenarioLoadFailed("No scenario selected."));
                return;
            }

            if (m_Loading != null)
                StopCoroutine(m_Loading);
            m_Loading = StartCoroutine(Load(selection.Value.Scenario));
        }

        IEnumerator Load(ScenarioDefinition scenario)
        {
            float started = Time.unscaledTime;
            Release();

            Debug.Log($"[Addressables] Loading '{scenario.address}'");
            m_Handle = Addressables.LoadAssetAsync<GameObject>(scenario.address);
            m_LoadedAddress = scenario.address;

            while (!m_Handle.IsDone)
            {
                EventBus.Publish(new ScenarioLoadProgress(m_Handle.PercentComplete * 0.9f));
                yield return null;
            }

            if (m_Handle.Status != AsyncOperationStatus.Succeeded || m_Handle.Result == null)
            {
                var error = m_Handle.OperationException != null ? m_Handle.OperationException.Message : "Unknown error";
                Debug.LogError($"[Addressables] Failed to load '{scenario.address}': {error}");
                Release();
                m_Loading = null;
                EventBus.Publish(new ScenarioLoadFailed(error));
                yield break;
            }

            // Let the loading screen finish its bar before moving on.
            while (Time.unscaledTime - started < minimumLoadingSeconds)
            {
                float t = (Time.unscaledTime - started) / minimumLoadingSeconds;
                EventBus.Publish(new ScenarioLoadProgress(Mathf.Lerp(0.9f, 1f, t)));
                yield return null;
            }

            EventBus.Publish(new ScenarioLoadProgress(1f));
            Debug.Log($"[Addressables] Loaded '{scenario.address}'");
            m_Loading = null;
            EventBus.Publish(new ScenarioLoaded(scenario, m_Handle.Result));
        }

        void OnSessionReset(SessionReset e)
        {
            if (m_Loading != null)
            {
                StopCoroutine(m_Loading);
                m_Loading = null;
            }
            // Release after the placed instance has been destroyed this frame.
            StartCoroutine(ReleaseAtEndOfFrame());
        }

        IEnumerator ReleaseAtEndOfFrame()
        {
            var handle = m_Handle;
            m_Handle = default;
            yield return null;
            if (handle.IsValid())
            {
                Addressables.Release(handle);
                Debug.Log($"[Addressables] Released '{m_LoadedAddress}'");
            }
        }

        void Release()
        {
            if (m_Handle.IsValid())
            {
                Addressables.Release(m_Handle);
                Debug.Log($"[Addressables] Released '{m_LoadedAddress}'");
            }
            m_Handle = default;
        }
    }
}
