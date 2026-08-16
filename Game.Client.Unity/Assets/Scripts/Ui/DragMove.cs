using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client
{
    /// <summary>
    /// Makes a window draggable by its title bar. Put this on the BAR and point it at the window.
    ///
    /// The drag delta arrives in screen pixels while anchoredPosition is in canvas units, so it is
    /// divided by the canvas scale factor — without that, a window on a 1440-wide phone would shoot
    /// away from the finger at roughly twice the speed on a design space of 720.
    /// </summary>
    public class DragMove : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public RectTransform Target;

        /// <summary>Set by <see cref="WindowGeometry"/>: a locked window stays where it was put
        /// (playtest 23 — *"a small button with a lock so it's locked in position and in size"*). Raising
        /// it on touch still works, because that is not moving it.</summary>
        public bool Locked;

        private Canvas _canvas;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Touching a window raises it: with several open, the one you are handling should be the
            // one you can see.
            if (Target != null) Target.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || Locked) return;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Target.anchoredPosition += eventData.delta / scale;
            Clamp();
        }

        /// <summary>Keep a sliver of the window on screen. A window dragged fully off has no title bar
        /// left to grab, so it can never be brought back — on a phone there is no window menu to
        /// rescue it with.
        ///
        /// <para>🔴 IT ASSUMED EVERY WINDOW WAS CENTRE-ANCHORED, and none of the movable ones are
        /// (`87e`(a), playtest 24: the combat window *"cannot go left below certain distance"*). The old
        /// arithmetic read anchoredPosition as an offset from the parent's CENTRE and clamped it to
        /// ±(halfParent + halfWindow); that is only true at anchor/pivot (0.5, 0.5). The chat window is
        /// pinned bottom-LEFT and the combat window bottom-RIGHT, so for the combat window the same
        /// numbers allowed a long drag off the right edge and stopped it dead a few pixels past the
        /// left one — the asymmetry he hit. Working in the parent's own coordinates instead makes anchor
        /// and pivot drop out, and both directions get the same 60 units of guaranteed handle.</para></summary>
        private void Clamp()
        {
            var parent = Target.parent as RectTransform;
            if (parent == null) return;

            const float keepVisible = 60f;

            // The window's rect expressed in the PARENT's local space, which is where anchoredPosition
            // lives: start from the anchor point, then step back by the pivot.
            Vector2 size = Target.rect.size;
            var anchor = new Vector2(
                Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, (Target.anchorMin.x + Target.anchorMax.x) * 0.5f),
                Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, (Target.anchorMin.y + Target.anchorMax.y) * 0.5f));
            Vector2 position = Target.anchoredPosition;
            Vector2 min = anchor + position - new Vector2(Target.pivot.x * size.x, Target.pivot.y * size.y);
            Vector2 max = min + size;

            // Push back only as far as it takes to leave `keepVisible` on screen on each axis.
            float dx = 0f, dy = 0f;
            if (max.x < parent.rect.xMin + keepVisible) dx = parent.rect.xMin + keepVisible - max.x;
            else if (min.x > parent.rect.xMax - keepVisible) dx = parent.rect.xMax - keepVisible - min.x;
            if (max.y < parent.rect.yMin + keepVisible) dy = parent.rect.yMin + keepVisible - max.y;
            else if (min.y > parent.rect.yMax - keepVisible) dy = parent.rect.yMax - keepVisible - min.y;

            if (dx != 0f || dy != 0f)
                Target.anchoredPosition = position + new Vector2(dx, dy);
        }
    }
}
