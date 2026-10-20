using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FireReady.UI
{
    /// <summary>A button that reports press and release, for "hold to Squeeze".</summary>
    public sealed class HoldButton : Selectable, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] float pressedScale = 0.92f;

        public event Action<bool> HeldChanged;
        public bool IsHeld { get; private set; }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (IsInteractable())
                SetHeld(true);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            SetHeld(false);
        }

        protected override void OnDisable()
        {
            SetHeld(false);
            base.OnDisable();
        }

        public void Release() => SetHeld(false);

        void SetHeld(bool held)
        {
            if (IsHeld == held)
                return;
            IsHeld = held;
            transform.localScale = Vector3.one * (held ? pressedScale : 1f);
            HeldChanged?.Invoke(held);
        }
    }
}
