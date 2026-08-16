using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        public Image GripImage;
        public TMPro.TextMeshProUGUI GripLabel;
        public Vector2 MinSize = new Vector2(320f, 200f);
        public string Key = "";

        /// <summary>Locked = neither moves nor resizes. The grip STAYS VISIBLE and dims (`87e`(d)) — it
        /// used to be hidden outright, which meant the bottom-right corner was covered while unlocked
        /// and free while locked, so nothing could be laid out against it.</summary>
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
            // Dim, never hide (see Locked). A locked grip is inert because Resize refuses, so the only
            // job left here is to say so.
            float alpha = Locked ? 0.25f : 1f;
            if (GripImage != null)
            {
                var c = GripImage.color; c.a = alpha; GripImage.color = c;
            }
            if (GripLabel != null)
            {
                var c = GripLabel.color; c.a = alpha; GripLabel.color = c;
            }
        }

        /// <summary>Resize by a drag delta, clamped so a window can never be shrunk past the point where
        /// its own title bar and buttons stop fitting — the phone equivalent of losing a window off the
        /// edge, and just as unrecoverable.
        ///
        /// <para>🔴 THE GRIP MOVES, THE WINDOW'S TOP-LEFT DOES NOT (`87e`(b), playtest 24): *"I drag down
        /// it goes from bottom to top increasing its height but the bottom is the frozen position. The
        /// drag button should move not the top/left."* He is describing a PIVOT, not a bug in the
        /// arithmetic: the size was growing correctly, but a uGUI rect grows away from its pivot, and
        /// both of these windows are pinned by a BOTTOM corner (chat bottom-left, combat bottom-right).
        /// So height was added upwards and the grip — which is at the bottom — never followed the
        /// finger. Compensating the anchored position by the pivot pins the top-left corner instead, for
        /// any anchor either window is ever given.</para></summary>
        public void Resize(Vector2 delta)
        {
            if (Locked || Panel == null) return;
            Vector2 old = Panel.sizeDelta;
            var size = old + new Vector2(delta.x, -delta.y);   // y grows downward on a grip
            var parent = Panel.parent as RectTransform;
            Vector2 max = parent != null ? parent.rect.size : new Vector2(4000f, 4000f);
            var applied = new Vector2(Mathf.Clamp(size.x, MinSize.x, max.x),
                                      Mathf.Clamp(size.y, MinSize.y, max.y));
            Panel.sizeDelta = applied;

            // Keep the TOP-LEFT corner where it was. Left edge = pos.x − pivot.x·w, top edge =
            // pos.y + (1−pivot.y)·h; hold both constant and solve for the new position.
            Vector2 grew = applied - old;
            Panel.anchoredPosition += new Vector2(Panel.pivot.x * grew.x,
                                                  -(1f - Panel.pivot.y) * grew.y);
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
