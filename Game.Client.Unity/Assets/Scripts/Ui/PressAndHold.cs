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
    /// gestures are decided here, from how long the finger was down.
    ///
    /// The hold fires **while the finger is still down**, the moment <see cref="HoldSeconds"/> elapses
    /// (owner, playtest-16: *"I start to hold and after a 1-2s to register it as hold, not to wait for
    /// a release"*). Deciding it on release meant the gesture had no feedback at all until you let go,
    /// so a hold that was already long enough looked identical to one that wasn't — you released and
    /// hoped. Firing on the threshold makes the menu appear under the finger that asked for it.
    /// Release afterwards is then a no-op: the gesture already happened, and the tap must not follow it.
    /// </summary>
    public class PressAndHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>How long the finger must stay down. Longer than Android's ~0.5s on purpose: now
        /// that the hold fires on its own, a threshold that short turns a slow tap into a hold.</summary>
        public float HoldSeconds = 1.0f;

        /// <summary>Screen pixels of finger travel that cancel a pending hold. Without it, dragging a
        /// SCROLLING list slowly would arm a hold — the widget travels with the content, so the pointer
        /// never leaves it and <see cref="OnPointerExit"/> never fires. Drag is not handled here at all
        /// (an IDragHandler on the child would swallow the scroll rect's own drag).</summary>
        public float CancelDistance = 40f;

        public Action OnTap;
        public Action OnHold;

        /// <summary>Optional gate — the slot is grey/disabled, so neither gesture should fire.</summary>
        public Func<bool> Enabled;

        private float _downAt = -1f;
        private bool _holdFired;
        private Vector2 _downPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downAt = Time.unscaledTime;
            _holdFired = false;
            _downPos = eventData.position;
        }

        private void Update()
        {
            if (_downAt < 0f || _holdFired) return;

            if ((PointerPosition() - _downPos).sqrMagnitude > CancelDistance * CancelDistance)
            {
                _downAt = -1f;   // the finger is scrolling, not holding
                return;
            }

            if (Time.unscaledTime - _downAt < HoldSeconds) return;

            _holdFired = true;   // set BEFORE the callback: it may destroy these rows and rebuild them
            if (Enabled != null && !Enabled()) return;
            if (OnHold != null) OnHold();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            bool wasDown = _downAt >= 0f;
            _downAt = -1f;

            // The hold already fired under the finger — releasing must not also count as a tap.
            if (_holdFired) { _holdFired = false; return; }
            if (!wasDown) return;

            if (Enabled != null && !Enabled()) return;
            if (OnTap != null) OnTap();
        }

        /// <summary>A finger that slides off the widget cancels — otherwise dragging across the bar to
        /// swipe pages would fire whichever slot the finger happened to land on.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _downAt = -1f;
        }

        private static Vector2 PointerPosition() =>
            Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    }
}
