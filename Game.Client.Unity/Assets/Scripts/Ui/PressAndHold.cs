using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client
{
    /// <summary>
    /// Distinguishes a TAP from a PRESS-AND-HOLD on the same widget — the phone's answer to
    /// left-click vs right-click.
    ///
    /// It deliberately does not use <c>Button.onClick</c>: the button would fire its click on release
    /// regardless, so a hold would both clear the slot AND cast the skill that was in it. Both
    /// gestures are decided here, on release, from how long the finger was down.
    /// </summary>
    public class PressAndHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float HoldSeconds = 0.5f;
        public Action OnTap;
        public Action OnHold;

        /// <summary>Optional gate — the slot is grey/disabled, so neither gesture should fire.</summary>
        public Func<bool> Enabled;

        private float _downAt = -1f;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downAt = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_downAt < 0f) return;
            float held = Time.unscaledTime - _downAt;
            _downAt = -1f;

            if (Enabled != null && !Enabled()) return;

            if (held >= HoldSeconds) { if (OnHold != null) OnHold(); }
            else { if (OnTap != null) OnTap(); }
        }

        /// <summary>A finger that slides off the widget cancels — otherwise dragging across the bar to
        /// swipe pages would fire whichever slot the finger happened to land on.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _downAt = -1f;
        }
    }
}
