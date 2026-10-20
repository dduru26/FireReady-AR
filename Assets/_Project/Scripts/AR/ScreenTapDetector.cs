using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace FireReady.AR
{
    /// <summary>
    /// Turns raw touches into taps and double-taps on the camera view. Touches that
    /// start on a UI element are ignored, so pressing a button never places the scene.
    /// In the editor the mouse acts as a finger.
    /// </summary>
    public sealed class ScreenTapDetector : MonoBehaviour
    {
        [SerializeField] float maxTapDuration = 0.35f;
        [SerializeField] float maxTapMovement = 40f;
        [SerializeField] float doubleTapWindow = 0.35f;
        [SerializeField] float doubleTapRadius = 150f;

        public event Action<Vector2> Tapped;
        public event Action<Vector2> DoubleTapped;

        static readonly List<RaycastResult> s_UiHits = new List<RaycastResult>();
        double m_LastTapTime = double.NegativeInfinity;
        Vector2 m_LastTapPosition;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase != TouchPhase.Ended)
                    continue;

                var duration = touch.time - touch.startTime;
                var movement = (touch.screenPosition - touch.startScreenPosition).magnitude;
                if (duration > maxTapDuration || movement > maxTapMovement)
                    continue;
                if (IsOverUi(touch.startScreenPosition))
                    continue;

                HandleTap(touch.screenPosition, touch.time);
            }
        }

        void HandleTap(Vector2 position, double time)
        {
            bool isDouble = time - m_LastTapTime <= doubleTapWindow &&
                            (position - m_LastTapPosition).magnitude <= doubleTapRadius;

            Tapped?.Invoke(position);

            if (isDouble)
            {
                DoubleTapped?.Invoke(position);
                m_LastTapTime = double.NegativeInfinity; // a third tap starts a new pair
            }
            else
            {
                m_LastTapTime = time;
                m_LastTapPosition = position;
            }
        }

        static bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var data = new PointerEventData(eventSystem) { position = screenPosition };
            s_UiHits.Clear();
            eventSystem.RaycastAll(data, s_UiHits);
            return s_UiHits.Count > 0;
        }
    }
}
