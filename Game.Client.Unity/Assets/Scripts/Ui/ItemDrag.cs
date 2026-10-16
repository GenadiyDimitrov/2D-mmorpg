using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// `BL-347` (0.269.0) — an item you can pick up and drop: a bag cell or a worn paper-doll square. Owner, 2026-10-16:
    /// *"make inventory to move items (drag drop) and item to change its slot position (like a custom bag order) and drag
    /// drop on equipment it equip it and drag drop from equipmnet its unequiping it ... like a normal bag"*.
    ///
    /// 🔑 THE DRAG MUST NOT EAT THE SCROLL. The cells sit in a ScrollRect, and a cell that takes drag events keeps them
    /// from it. So: with a MOUSE a drag picks the item up at once (the wheel scrolls); with a FINGER only a press held
    /// <see cref="HoldToLift"/> first picks it up, and a quick swipe is handed on to the ScrollRect untouched. That is
    /// why the skill bar chose a two-step tap instead (`GameUi.Skills`): a swipe there pages the bar.
    ///
    /// A tap is <see cref="Click"/> — but only once <see cref="DoubleWindow"/> has passed without a second tap, which is
    /// then <see cref="DoubleClick"/> (equip / use, the old [e]/[u] fast buttons). The drop goes to the first
    /// <see cref="ItemDropTarget"/> under the pointer.
    /// </summary>
    public sealed class ItemDrag : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float HoldToLift = 0.3f, DoubleWindow = 0.28f;

        public Guid Item;
        public bool FromEquip;
        public Sprite Icon;
        public Color Tint = Color.white;
        public Action Click, DoubleClick;
        public ScrollRect Scroll;

        private float _downAt;
        private bool _lifted, _forwarding;
        private RectTransform _ghost;
        private float _clickAt = -1f;

        public void OnPointerDown(PointerEventData e) => _downAt = Time.unscaledTime;

        public void OnPointerClick(PointerEventData e)
        {
            if (_lifted) return;
            if (DoubleClick != null && _clickAt >= 0f && Time.unscaledTime - _clickAt <= DoubleWindow)
            {
                _clickAt = -1f;
                DoubleClick();
                return;
            }
            if (DoubleClick == null) { Click?.Invoke(); return; }
            _clickAt = Time.unscaledTime;   // Update fires Click once the window passes alone
        }

        private void Update()
        {
            if (_clickAt >= 0f && Time.unscaledTime - _clickAt > DoubleWindow)
            {
                _clickAt = -1f;
                Click?.Invoke();
            }
        }

        public void OnInitializePotentialDrag(PointerEventData e)
        {
            if (Scroll != null) Scroll.OnInitializePotentialDrag(e);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            bool mouse = e.pointerId < 0;   // -1/-2/-3 = mouse buttons; touches are 0 and up
            if (Item == Guid.Empty) return;   // an empty paper-doll square: nothing to pick up
            if (mouse ? e.button != PointerEventData.InputButton.Left : Time.unscaledTime - _downAt < HoldToLift)
            {
                _forwarding = Scroll != null;
                if (_forwarding) Scroll.OnBeginDrag(e);
                return;
            }
            _lifted = true;
            _clickAt = -1f;
            _ghost = MakeGhost(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (_forwarding) { Scroll.OnDrag(e); return; }
            if (_ghost != null) _ghost.position = e.position;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_forwarding) { _forwarding = false; Scroll.OnEndDrag(e); return; }
            if (!_lifted) return;
            _lifted = false;
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;

            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, hits);
            foreach (var h in hits)
            {
                var target = h.gameObject.GetComponentInParent<ItemDropTarget>();
                if (target == null || target.gameObject == gameObject) continue;
                target.Drop?.Invoke(this);
                return;
            }
        }

        private void OnDisable()
        {
            // The window closed (or the list was rebuilt) mid-drag: never leave a ghost on the screen.
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null;
            _lifted = _forwarding = false;
        }

        private RectTransform MakeGhost(PointerEventData e)
        {
            var canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null) return null;
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.SetAsLastSibling();
            rt.sizeDelta = ((RectTransform)transform).rect.size;
            rt.position = e.position;
            var img = go.GetComponent<Image>();
            img.sprite = Icon;
            img.color = Icon != null ? new Color(Tint.r, Tint.g, Tint.b, 0.85f) : new Color(1f, 1f, 1f, 0.35f);
            img.raycastTarget = false;
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;
            return rt;
        }
    }

    /// <summary>`BL-347` — somewhere an <see cref="ItemDrag"/> can land: a bag cell (swap places), the bag list (unequip),
    /// a paper-doll square (equip).</summary>
    public sealed class ItemDropTarget : MonoBehaviour
    {
        public Action<ItemDrag> Drop;
    }
}
