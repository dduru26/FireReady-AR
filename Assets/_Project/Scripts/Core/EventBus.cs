using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace FireReady.Core
{
    /// <summary>Marker for anything that travels on the <see cref="EventBus"/>.</summary>
    public interface IGameEvent { }

    /// <summary>
    /// Typed publish/subscribe hub (Observer pattern). Systems talk through events
    /// instead of holding references to each other, so the AR layer, the simulation
    /// and the UI stay decoupled. Events are structs, so publishing does not allocate.
    /// </summary>
    public static class EventBus
    {
        static readonly List<Action> s_Clearers = new List<Action>();

        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
            => Channel<T>.Add(handler);

        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
            => Channel<T>.Remove(handler);

        public static void Publish<T>(T evt) where T : struct, IGameEvent
            => Channel<T>.Raise(evt);

        /// <summary>Removes every subscriber of every event type.</summary>
        public static void ClearAll()
        {
            foreach (var clear in s_Clearers)
                clear();
        }

        // Static state survives play sessions when domain reload is off, so start clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => ClearAll();

        static class Channel<T> where T : struct, IGameEvent
        {
            static readonly List<Action<T>> s_Handlers = new List<Action<T>>();

            static Channel() => s_Clearers.Add(() => s_Handlers.Clear());

            public static void Add(Action<T> handler)
            {
                if (handler != null && !s_Handlers.Contains(handler))
                    s_Handlers.Add(handler);
            }

            public static void Remove(Action<T> handler) => s_Handlers.Remove(handler);

            public static void Raise(T evt)
            {
                if (s_Handlers.Count == 0)
                    return;

                // Copy first so handlers can subscribe or unsubscribe while we iterate.
                var snapshot = ListPool<Action<T>>.Get();
                snapshot.AddRange(s_Handlers);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    try { snapshot[i](evt); }
                    catch (Exception e) { Debug.LogException(e); }
                }
                ListPool<Action<T>>.Release(snapshot);
            }
        }
    }
}
