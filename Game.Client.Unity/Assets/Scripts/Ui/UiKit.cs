using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// Builders for the uGUI + TextMeshPro interface — everything is constructed FROM CODE, no
    /// prefabs and no Canvas wiring in the scene.
    ///
    /// That constraint is deliberate and it is not laziness: the owner never opens the Editor for
    /// day-to-day work (the client is built, installed and driven headlessly over adb), and anything
    /// that lives in a .prefab or a scene file can only be authored there. Code-built UI keeps the
    /// whole client editable from a text editor, diffable in git, and reviewable in a pull request.
    ///
    /// The one thing that DID need the Editor is the TMP essential resources — see
    /// docs/unity/EditorSetup.md. Without them TMP has no default font and draws nothing.
    /// </summary>
    public static class UiKit
    {
        // ----- palette ---------------------------------------------------------------------------
        // Borrowed from the WPF harness so the two clients read as the same game. Parity of
        // FUNCTION is the goal, not pixel-matching a desktop layout onto a phone.

        public static readonly Color Panel      = new Color(0.11f, 0.13f, 0.16f, 0.92f);
        public static readonly Color PanelLight = new Color(0.17f, 0.20f, 0.24f, 0.95f);
        public static readonly Color Border     = new Color(0.35f, 0.41f, 0.50f, 1f);
        public static readonly Color Text       = new Color(0.90f, 0.92f, 0.95f, 1f);
        public static readonly Color TextDim    = new Color(0.68f, 0.72f, 0.78f, 1f);
        public static readonly Color Accent     = new Color(0.35f, 0.65f, 1.00f, 1f);
        /// <summary>The ACTIVE-tab tint — a muted, slightly transparent blue. The full Accent read as
        /// "too much blue" on a row of tabs, so selection is shown with this quieter fill instead.</summary>
        public static readonly Color TabActive  = new Color(0.22f, 0.34f, 0.50f, 0.90f);
        public static readonly Color Good       = new Color(0.45f, 0.85f, 0.50f, 1f);
        public static readonly Color Bad        = new Color(0.90f, 0.35f, 0.35f, 1f);
        public static readonly Color Hp         = new Color(0.78f, 0.22f, 0.22f, 1f);
        public static readonly Color Mp         = new Color(0.25f, 0.45f, 0.85f, 1f);
        /// <summary>EXP is GREEN, not the old gold: gold sat too close to the yellow used for NPC names
        /// and for a buff about to expire, and progress deserves a colour that means only progress.</summary>
        public static readonly Color Xp         = new Color(0.35f, 0.75f, 0.35f, 1f);
        public static readonly Color BarBg      = new Color(0.08f, 0.09f, 0.11f, 1f);

        /// <summary>Design resolution. The scaler matches on HEIGHT, so a taller or narrower phone
        /// changes how much WIDTH you see rather than rescaling everything.
        ///
        /// Mutable so Settings can change the UI size — but only read at BUILD time, which is why that
        /// setting is labelled "restart".</summary>
        public static Vector2 Reference = new Vector2(1280f, 720f);

        // ----- root ------------------------------------------------------------------------------

        /// <summary>The overlay canvas + the EventSystem uGUI needs to route touches. Without an
        /// EventSystem every button is inert — it looks drawn and simply never fires.</summary>
        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // UnityEngine.Object explicitly: `using System` makes a bare `Object` ambiguous with the
            // C# keyword alias.
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            return canvas;
        }

        // ----- layout helpers --------------------------------------------------------------------

        public static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

        /// <summary>Pin a corner: anchor both min and max to the same point, then position by size and
        /// offset from that corner. Anchors do the responsive work so nothing recomputes per frame.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot,
                                          Vector2 offset, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Stretch to fill the parent, inset by the given margins.</summary>
        public static RectTransform Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        // ----- widgets ---------------------------------------------------------------------------

        /// <summary>
        /// A filled rectangle. <paramref name="blocksInput"/> false for invisible CONTAINERS: an Image
        /// with zero alpha still absorbs raycasts, so a full-screen transparent grouping object would
        /// swallow every tap meant for the world behind it.
        /// </summary>
        public static Image Box(Transform parent, string name, Color colour, bool blocksInput = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = colour;
            image.raycastTarget = blocksInput;
            return image;
        }

        /// <summary>A panel: filled box with a 1px border drawn as a slightly larger box behind it.
        /// Cheaper and more predictable than a sliced sprite we would have to author in the Editor.</summary>
        public static RectTransform PanelBox(Transform parent, string name)
        {
            var border = Box(parent, name, Border);
            var inner = Box(border.transform, "Inner", Panel);
            Stretch(Rect(inner.gameObject), 1f, 1f, 1f, 1f);
            return Rect(border.gameObject);
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float size = 18f,
                                            Color? colour = null,
                                            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = colour ?? Text;
            label.alignment = align;
            label.raycastTarget = false;   // labels must never swallow a tap meant for what is under them
            return label;
        }

        public static Button TextButton(Transform parent, string text, Action onClick, float size = 18f)
        {
            var image = Box(parent, "Button", PanelLight);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = new Color(0.26f, 0.31f, 0.37f, 1f);
            colors.pressedColor = Accent;
            colors.disabledColor = new Color(0.13f, 0.15f, 0.18f, 0.6f);
            button.colors = colors;

            var label = Label(image.transform, text, size, Text, TextAlignmentOptions.Center);
            Stretch(Rect(label.gameObject), 2f, 2f, 2f, 2f);

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Change a button's caption without rebuilding it — the label is its only child text.</summary>
        public static void SetButtonText(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }

        /// <summary>
        /// Standard window chrome: a draggable title bar with the title and a close button. Returns
        /// the bar's height so callers can lay content out beneath it.
        ///
        /// Every window gets this rather than hand-placing its own title and ✕, so they all move, all
        /// close the same way, and a new window inherits both for free.
        /// </summary>
        public static float WindowChrome(RectTransform panel, string title, Action onClose)
        {
            const float height = 46f;
            var inner = panel.GetChild(0);

            var bar = Box(inner, "TitleBar", PanelLight);
            var rt = Rect(bar.gameObject);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);

            bar.gameObject.AddComponent<DragMove>().Target = panel;

            var label = Label(bar.transform, title, 22f, Text, TextAlignmentOptions.Left);
            Place(Rect(label.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                  new Vector2(16f, 0f), new Vector2(320f, 30f));

            // "X", not "✕": the bundled LiberationSans has no glyph for the multiplication sign and
            // TMP draws a missing glyph as a hollow box — which is the "[]" on the close buttons.
            var close = TextButton(bar.transform, "X", onClose, 20f);
            Place(Rect(close.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                  new Vector2(-8f, 0f), new Vector2(46f, 34f));

            return height;
        }

        public static TMP_InputField InputField(Transform parent, string placeholder,
                                                bool password = false, float size = 18f)
        {
            var image = Box(parent, "Input", new Color(0.06f, 0.07f, 0.09f, 1f));
            var field = image.gameObject.AddComponent<TMP_InputField>();

            var viewport = Box(image.transform, "TextArea", new Color(0, 0, 0, 0));
            Stretch(Rect(viewport.gameObject), 8f, 4f, 8f, 4f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var text = Label(viewport.transform, "", size, Text, TextAlignmentOptions.Left);
            Stretch(Rect(text.gameObject), 0f, 0f, 0f, 0f);

            var hint = Label(viewport.transform, placeholder, size, TextDim, TextAlignmentOptions.Left);
            Stretch(Rect(hint.gameObject), 0f, 0f, 0f, 0f);

            field.textViewport = Rect(viewport.gameObject);
            field.textComponent = text;
            field.placeholder = hint;
            field.contentType = password ? TMP_InputField.ContentType.Password
                                         : TMP_InputField.ContentType.Standard;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.text = "";
            return field;
        }

        /// <summary>A vertical scroll area. Returns the CONTENT transform to parent rows onto; it grows
        /// itself via a ContentSizeFitter so callers never compute a scroll height.</summary>
        public static RectTransform ScrollArea(Transform parent, out ScrollRect scroll, float spacing = 2f)
        {
            // The root is a container; the VIEWPORT below is what catches the drag.
            var root = Box(parent, "Scroll", new Color(0, 0, 0, 0), blocksInput: false);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewport = Box(root.transform, "Viewport", new Color(0, 0, 0, 0.001f));
            Stretch(Rect(viewport.gameObject), 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                                         typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var rt = Rect(content);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 0f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = Rect(viewport.gameObject);
            scroll.content = rt;
            return rt;
        }

        /// <summary>
        /// A labelled slider row. Returns the Slider so the caller can read it; the label updates
        /// itself with the live value, because a slider with no number on it is a guess.
        /// </summary>
        public static Slider SliderRow(Transform parent, string title, float min, float max,
                                       float value, string format, Action<float> onChange)
        {
            var row = Box(parent, "SliderRow", new Color(0, 0, 0, 0), blocksInput: false);

            var label = Label(row.transform, "", 15f, Text, TextAlignmentOptions.Left);
            Place(Rect(label.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                  new Vector2(0f, 0f), new Vector2(240f, 22f));

            var track = Box(row.transform, "Track", BarBg);
            Place(Rect(track.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                  new Vector2(250f, -4f), new Vector2(300f, 14f));

            var slider = track.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;

            var fill = Box(track.transform, "Fill", Accent);
            var fillRect = Rect(fill.gameObject);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;

            // No handle: a 14px-tall track dragged with a thumb is fiddly on a phone, and tapping
            // anywhere on the track to set the value is both simpler and easier to hit.
            slider.targetGraphic = track;
            slider.value = value;
            label.text = title + "   " + value.ToString(format);

            slider.onValueChanged.AddListener(v =>
            {
                label.text = title + "   " + v.ToString(format);
                if (onChange != null) onChange(v);
            });
            return slider;
        }

        /// <summary>A value bar (HP/MP/XP): background + a fill whose WIDTH is driven by anchorMax, so
        /// setting a value is one float and never a layout rebuild.</summary>
        public static Image ValueBar(Transform parent, Color fill)
        {
            var bg = Box(parent, "Bar", BarBg);
            var bar = Box(bg.transform, "Fill", fill);
            var rt = Rect(bar.gameObject);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return bar;
        }

        /// <summary>
        /// A number drawn ON a value bar, the way every game HUD does it — centred over the fill
        /// rather than on a separate line underneath.
        ///
        /// Pass the fill returned by <see cref="ValueBar"/>; the label is parented to the bar's
        /// BACKGROUND, so it covers the whole track and does not shrink as the bar empties. It is
        /// outlined because it has to stay readable over both the filled and the empty half, which are
        /// very different brightnesses, and it never blocks raycasts.
        /// </summary>
        public static TextMeshProUGUI BarLabel(Image fill, float size = 12f)
        {
            var label = Label(fill.transform.parent, "", size, Text, TextAlignmentOptions.Center);
            Stretch(Rect(label.gameObject), 4f, 0f, 4f, 0f);
            label.outlineColor = new Color32(0, 0, 0, 200);
            label.outlineWidth = 0.2f;
            return label;
        }

        public static void SetBar(Image fill, float value, float max)
        {
            if (fill == null) return;
            float pct = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(pct, 1f);
            rt.offsetMax = Vector2.zero;
        }

        private static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>();

        /// <summary>
        /// True when a screen POINT is over any interactive UI. TouchInput asks this so pressing a
        /// button doesn't also order a walk to the ground behind it.
        ///
        /// This raycasts the point itself rather than asking
        /// <c>EventSystem.IsPointerOverGameObject(fingerId)</c>, because that answers about a LIVE
        /// pointer and the EventSystem drops a touch's pointer data the moment the finger lifts. Taps
        /// here are decided on RELEASE (so a pinch's first finger can't queue a walk), so the pointer
        /// was always already gone and the check always said "not over UI" — every tap on a window
        /// also walked the character.
        /// </summary>
        public static bool OverUi(Vector2 screenPoint)
        {
            var events = EventSystem.current;
            if (events == null) return false;

            RaycastHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = screenPoint }, RaycastHits);
            return RaycastHits.Count > 0;
        }
    }
}
