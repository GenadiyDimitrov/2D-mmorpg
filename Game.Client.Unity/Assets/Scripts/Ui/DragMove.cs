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

        private Canvas _canvas;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Touching a window raises it: with several open, the one you are handling should be the
            // one you can see.
            if (Target != null) Target.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Target.anchoredPosition += eventData.delta / scale;
            Clamp();
        }

        /// <summary>Keep a sliver of the window on screen. A window dragged fully off has no title bar
        /// left to grab, so it can never be brought back — on a phone there is no window menu to
        /// rescue it with.</summary>
        private void Clamp()
        {
            var parent = Target.parent as RectTransform;
            if (parent == null) return;

            Vector2 half = Target.rect.size * 0.5f;
            Vector2 bounds = parent.rect.size * 0.5f;
            const float keepVisible = 60f;

            var position = Target.anchoredPosition;
            position.x = Mathf.Clamp(position.x, -bounds.x - half.x + keepVisible,
                                                  bounds.x + half.x - keepVisible);
            position.y = Mathf.Clamp(position.y, -bounds.y - half.y + keepVisible,
                                                  bounds.y + half.y - keepVisible);
            Target.anchoredPosition = position;
        }
    }
}
