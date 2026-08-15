using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client
{
    /// <summary>
    /// Move + RESIZE + LOCK for a window, remembered on the DEVICE.
    ///
    /// His playtest-23 ask, verbatim: *"Make the chat windowses resizable (a small button with a lock so
    /// it's locked in position and in size - persistent for the apk not the server), size position lock
    /// status is persistant for the apk"* · *"I want to be able to move the window side to side or on top
    /// of the other without they obscure my view."*
    ///
    /// <para>🔑 **PlayerPrefs, not the server, and that is his instruction, not a shortcut.** Where a
    /// window sits is a property of the SCREEN it is being read on, not of the character — the same
    /// account on a tablet wants a different layout, and a layout pushed from the server would fight the
    /// one the device just learned. It also means this costs nothing in protocol or persistence.</para>
    ///
    /// <para>Dragging is <see cref="DragMove"/>'s job and stays there; this adds the grip, the lock and
    /// the memory, and asks DragMove to stand down while locked.</para>
    /// </summary>
    public class WindowGeometry : MonoBehaviour
    {
        public RectTransform Panel;
        public DragMove Drag;
        public RectTransform Grip;
        public Vector2 MinSize = new Vector2(320f, 200f);
        public string Key = "";

        /// <summary>Locked = neither moves nor resizes. The grip disappears with it, because a grip you
        /// can see and cannot use is worse than no grip.</summary>
        public bool Locked { get; private set; }

        public event Action LockChanged;

        // Saving is DEFERRED rather than done per drag frame: a drag writes a new position every frame,
        // and PlayerPrefs on Android is a file. Wait for the geometry to sit still, then write once.
        private const float SettleSeconds = 0.4f;
        private Vector2 _lastPos, _lastSize;
        private float _settleAt = -1f;

        private void Start()
        {
            Restore();
            _lastPos = Panel.anchoredPosition;
            _lastSize = Panel.sizeDelta;
        }

        private void Update()
        {
            if (Panel == null) return;
            if (Panel.anchoredPosition != _lastPos || Panel.sizeDelta != _lastSize)
            {
                _lastPos = Panel.anchoredPosition;
                _lastSize = Panel.sizeDelta;
                _settleAt = Time.unscaledTime + SettleSeconds;
            }
            else if (_settleAt > 0f && Time.unscaledTime >= _settleAt)
            {
                _settleAt = -1f;
                Save();
            }
        }

        public void ToggleLock()
        {
            Locked = !Locked;
            Apply();
            Save();
            LockChanged?.Invoke();
        }

        private void Apply()
        {
            if (Drag != null) Drag.Locked = Locked;
            if (Grip != null) Grip.gameObject.SetActive(!Locked);
        }

        /// <summary>Resize by a drag delta, clamped so a window can never be shrunk past the point where
        /// its own title bar and buttons stop fitting — the phone equivalent of losing a window off the
        /// edge, and just as unrecoverable.</summary>
        public void Resize(Vector2 delta)
        {
            if (Locked || Panel == null) return;
            var size = Panel.sizeDelta + new Vector2(delta.x, -delta.y);   // y grows downward on a grip
            var parent = Panel.parent as RectTransform;
            Vector2 max = parent != null ? parent.rect.size : new Vector2(4000f, 4000f);
            Panel.sizeDelta = new Vector2(Mathf.Clamp(size.x, MinSize.x, max.x),
                                          Mathf.Clamp(size.y, MinSize.y, max.y));
        }

        private string K(string suffix) => "win." + Key + "." + suffix;

        private void Save()
        {
            if (string.IsNullOrEmpty(Key) || Panel == null) return;
            PlayerPrefs.SetFloat(K("x"), Panel.anchoredPosition.x);
            PlayerPrefs.SetFloat(K("y"), Panel.anchoredPosition.y);
            PlayerPrefs.SetFloat(K("w"), Panel.sizeDelta.x);
            PlayerPrefs.SetFloat(K("h"), Panel.sizeDelta.y);
            PlayerPrefs.SetInt(K("lock"), Locked ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Restore()
        {
            if (string.IsNullOrEmpty(Key) || Panel == null) return;
            // Absence is the signal, not a sentinel value: a window that has never been moved keeps
            // whatever the layout code gave it, which is the position that was designed for it.
            if (PlayerPrefs.HasKey(K("x")))
                Panel.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(K("x")), PlayerPrefs.GetFloat(K("y")));
            if (PlayerPrefs.HasKey(K("w")))
            {
                var parent = Panel.parent as RectTransform;
                Vector2 max = parent != null ? parent.rect.size : new Vector2(4000f, 4000f);
                Panel.sizeDelta = new Vector2(
                    Mathf.Clamp(PlayerPrefs.GetFloat(K("w")), MinSize.x, max.x),
                    Mathf.Clamp(PlayerPrefs.GetFloat(K("h")), MinSize.y, max.y));
            }
            Locked = PlayerPrefs.GetInt(K("lock"), 0) == 1;
            Apply();
            // Fire it even though nothing "changed": the lock BUTTON is built before Start runs, so its
            // caption is showing the default and only this tells it what was restored.
            LockChanged?.Invoke();
        }
    }

    /// <summary>The corner grip. A separate one-job component so the geometry owner does not have to be
    /// an event handler as well — and so the grip can sit anywhere in the hierarchy the layout wants.</summary>
    public class ResizeGrip : MonoBehaviour, IDragHandler
    {
        public WindowGeometry Geometry;

        private Canvas _canvas;

        public void OnDrag(PointerEventData eventData)
        {
            if (Geometry == null) return;
            // Screen pixels -> canvas units, the same correction DragMove makes and for the same reason:
            // on a 1440-wide phone against a 1280 design space the grip would otherwise outrun the finger.
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Geometry.Resize(eventData.delta / scale);
        }
    }
}
