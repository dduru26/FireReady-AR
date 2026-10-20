using FireReady.Core;
using UnityEngine;

namespace FireReady.UI
{
    /// <summary>Shows its root only in the listed simulation states.</summary>
    public abstract class StateScreen : MonoBehaviour
    {
        [SerializeField] protected GameObject root;

        protected abstract bool VisibleIn(SimulationStateId state);

        protected virtual void OnEnable() => EventBus.Subscribe<SimulationStateChanged>(OnStateChanged);
        protected virtual void OnDisable() => EventBus.Unsubscribe<SimulationStateChanged>(OnStateChanged);

        protected virtual void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        void OnStateChanged(SimulationStateChanged e)
        {
            bool show = VisibleIn(e.Current);
            if (root != null && root.activeSelf != show)
            {
                root.SetActive(show);
                if (show) OnShown();
            }
        }

        protected virtual void OnShown() { }
    }
}
