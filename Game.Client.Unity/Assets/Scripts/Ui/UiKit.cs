using System;
using System.Collections;
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

        // ----- icons -----------------------------------------------------------------------------
        //
        // 🔑 WHY THESE ARE DRAWN FROM RECTANGLES AND NOT TYPED AS CHARACTERS (playtest 24, `87e`).
        // He asked for a bin, a speech bubble and a padlock. The bundled LiberationSans TMP atlas is
        // STATIC and holds ~250 glyphs — no 🗑, no 💬, no 🔒, and TMP draws a missing glyph as a hollow
        // box, which is the "[]" that has turned up on the close buttons and the bullet lists twice
        // before. Adding glyphs to the atlas needs the Editor, which the owner never opens. So an icon
        // here is a handful of Images laid out in NORMALISED coordinates inside the button's face: no
        // font, no atlas, no sprite asset, and it scales with whatever size the button is given.

        /// <summary>The icon set. Deliberately tiny — each one is hand-composed below, so this grows
        /// only when a button genuinely cannot be a word.</summary>
        public enum Icon { Lock, Unlock, Bin, Bubble }

        /// <summary>One rectangle of an icon, positioned in the face's NORMALISED space (0..1, y up),
        /// so the whole glyph scales with the button instead of being pinned to a pixel size.</summary>
        private static void IconPart(Transform face, float x0, float y0, float x1, float y1,
                                     Color colour, float rotation = 0f)
        {
            var box = Box(face, "Part", colour, blocksInput: false);
            var rt = Rect(box.gameObject);
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            if (rotation != 0f) rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private static void PaintIcon(Transform face, Icon icon)
        {
            switch (icon)
            {
                case Icon.Lock:
                    // Body, then a shackle whose two posts both reach it — a closed loop.
                    IconPart(face, 0.16f, 0.06f, 0.84f, 0.56f, Text);
                    IconPart(face, 0.28f, 0.56f, 0.38f, 0.86f, Text);
                    IconPart(face, 0.62f, 0.56f, 0.72f, 0.86f, Text);
                    IconPart(face, 0.28f, 0.80f, 0.72f, 0.92f, Text);
                    IconPart(face, 0.45f, 0.20f, 0.55f, 0.40f, PanelLight);   // keyhole
                    break;
                case Icon.Unlock:
                    // The same body with the shackle swung OPEN to the left: only one post comes down,
                    // and it stops short of the body. That gap is the whole difference at 44px.
                    IconPart(face, 0.16f, 0.06f, 0.84f, 0.56f, Text);
                    IconPart(face, 0.62f, 0.56f, 0.72f, 0.86f, Text);
                    IconPart(face, 0.20f, 0.80f, 0.72f, 0.92f, Text);
                    IconPart(face, 0.20f, 0.66f, 0.30f, 0.92f, Text);
                    IconPart(face, 0.45f, 0.20f, 0.55f, 0.40f, PanelLight);   // keyhole
                    break;
                case Icon.Bin:
                    IconPart(face, 0.38f, 0.86f, 0.62f, 0.96f, Text);         // handle
                    IconPart(face, 0.08f, 0.72f, 0.92f, 0.84f, Text);         // lid
                    IconPart(face, 0.18f, 0.08f, 0.82f, 0.68f, Text);         // body
                    IconPart(face, 0.34f, 0.18f, 0.42f, 0.58f, PanelLight);   // ribs
                    IconPart(face, 0.58f, 0.18f, 0.66f, 0.58f, PanelLight);
                    break;
                case Icon.Bubble:
                    IconPart(face, 0.08f, 0.34f, 0.92f, 0.94f, Text);         // body
                    IconPart(face, 0.18f, 0.16f, 0.36f, 0.40f, Text, 45f);    // tail
                    IconPart(face, 0.22f, 0.52f, 0.78f, 0.60f, PanelLight);   // two lines of "speech"
                    IconPart(face, 0.22f, 0.70f, 0.66f, 0.78f, PanelLight);
                    break;
            }
        }

        /// <summary>A button whose face is a drawn icon rather than text. Same shape and states as
        /// <see cref="TextButton"/> so the two sit together in a row without looking like different
        /// widgets.</summary>
        public static Button IconButton(Transform parent, Icon icon, Action onClick)
        {
            var image = Box(parent, "IconButton", PanelLight);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.highlightedColor = new Color(0.26f, 0.31f, 0.37f, 1f);
            colors.pressedColor = Accent;
            colors.disabledColor = new Color(0.13f, 0.15f, 0.18f, 0.6f);
            button.colors = colors;

            // A named, transparent face so SetIcon can find and repaint it without touching the button.
            var face = Box(image.transform, "Icon", new Color(0, 0, 0, 0), blocksInput: false);
            Stretch(Rect(face.gameObject), 9f, 5f, 9f, 5f);
            PaintIcon(face.transform, icon);

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        /// <summary>Repaint an <see cref="IconButton"/> — the lock toggle is the only caller.
        /// ⚠ Detaches before destroying: Destroy is deferred to end-of-frame, so the old rectangles
        /// would otherwise be drawn UNDER the new ones for a frame and the padlock would flicker into
        /// an unreadable smear at exactly the moment you tapped it.</summary>
        public static void SetIcon(Button button, Icon icon)
        {
            if (button == null) return;
            var face = button.transform.Find("Icon");
            if (face == null) return;
            for (int i = face.childCount - 1; i >= 0; i--)
            {
                var part = face.GetChild(i);
                part.SetParent(null, false);
                UnityEngine.Object.Destroy(part.gameObject);
            }
            PaintIcon(face, icon);
        }

        /// <summary>Where the <paramref name="slot"/>-th button sits in a title bar, counted from the
        /// right and INSIDE the close button. Slot 0 is the lock (<see cref="MakeAdjustable"/> owns
        /// it); windows that want more title-bar buttons take 1, 2, … so nothing has to know the
        /// pixel arithmetic twice.</summary>
        public static Button TitleBarIcon(RectTransform panel, int slot, Icon icon, Action onClick)
        {
            var bar = panel.GetChild(0).Find("TitleBar");
            if (bar == null) return null;
            var button = IconButton(bar.transform, icon, onClick);
            Place(Rect(button.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                  new Vector2(-58f - slot * 48f, 0f), new Vector2(44f, 34f));
            return button;
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
        /// <summary>Make a <see cref="WindowChrome"/> window RESIZABLE and LOCKABLE, and remember its
        /// position, size and lock state on the DEVICE (playtest 23 — *"persistent for the apk not the
        /// server"*). Two things are added: a lock button in the title bar left of the X, and a drag grip
        /// in the bottom-right corner.
        ///
        /// <para><paramref name="key"/> is the PlayerPrefs namespace and must be stable across builds —
        /// change it and the user's layout silently resets to the default. Use the window's own name.</para>
        ///
        /// <para>Only the windows he named get this. It is not applied wholesale: a grip costs a corner
        /// of every window it is on, and most windows here are sized to their content and have nothing to
        /// gain from being stretched.</para></summary>
        public static WindowGeometry MakeAdjustable(RectTransform panel, string key, Vector2 minSize)
        {
            var geometry = panel.gameObject.AddComponent<WindowGeometry>();
            geometry.Panel = panel;
            geometry.Key = key;
            geometry.MinSize = minSize;

            var inner = panel.GetChild(0);
            var bar = inner.Find("TitleBar");
            if (bar != null) geometry.Drag = bar.GetComponent<DragMove>();

            // The grip: bottom-right, deliberately larger than it looks. A 20px visual corner is an
            // impossible target for a thumb, so the hit box is 44 and the glyph inside it is the hint.
            var grip = TextButton(inner, "//", () => { }, 15f);
            var gripRect = Rect(grip.gameObject);
            Place(gripRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f, 4f), new Vector2(44f, 44f));
            // Not a button in the end — it only ever drags. Killing the Button component stops it eating
            // the press as a click and leaves the drag handler the only thing listening.
            UnityEngine.Object.Destroy(grip);
            gripRect.gameObject.AddComponent<ResizeGrip>().Geometry = geometry;
            geometry.Grip = gripRect;
            // 🔴 `87e`(d): the grip used to VANISH when locked — *"is shown only on unlocked so it can
            // cut into the text"*. Appearing and disappearing is what made it a hazard: the content is
            // laid out once, so the corner it covers is occupied either way, and a control that is
            // sometimes there cannot be designed around. It stays put now and merely DIMS when locked
            // (WindowGeometry.Apply), so the corner it owns is the same corner at all times.
            geometry.GripImage = gripRect.GetComponent<Image>();
            geometry.GripLabel = gripRect.GetComponentInChildren<TextMeshProUGUI>();

            if (bar != null)
            {
                // 🔴 A PADLOCK, not "L"/"U" (`87e`(d)). It could not be a glyph — see the icon block
                // above for why the atlas cannot supply one — so it is drawn.
                var lockButton = IconButton(bar.transform, Icon.Unlock, geometry.ToggleLock);
                Place(Rect(lockButton.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                      new Vector2(-58f, 0f), new Vector2(44f, 34f));
                void Paint() => SetIcon(lockButton, geometry.Locked ? Icon.Lock : Icon.Unlock);
                geometry.LockChanged += Paint;
                Paint();
            }
            return geometry;
        }

        /// <summary>Retitle a window built by <see cref="WindowChrome"/>. Used by the target frame,
        /// whose title IS the target's name (playtest 23) rather than the word "Target". Finds the
        /// label by walking the title bar, so no caller has to hold a reference it would otherwise
        /// only need for this.</summary>
        public static void SetWindowTitle(RectTransform panel, string title)
        {
            if (panel == null) return;
            var bar = panel.GetChild(0).Find("TitleBar");
            if (bar == null) return;
            var found = bar.Find("TitleLabel");
            if (found == null) return;
            var label = found.GetComponent<TextMeshProUGUI>();
            if (label != null) label.text = title;
        }

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
            // Named so SetWindowTitle can find it by NAME rather than by "the first TMP in the bar" —
            // the bar can gain buttons (the lock, from MakeAdjustable) and each of them carries a label.
            label.gameObject.name = "TitleLabel";
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
            // Focus must place the caret, never select-all: with select-all on, the first keystroke
            // REPLACES a pre-filled value instead of editing it, which killed Reply ("/w name "),
            // the whisper action and re-editing the saved server URL. One switch, whole client.
            field.onFocusSelectAll = false;

            // ⚠ THE OTHER HALF OF THAT SWITCH, and without it the first half is a worse bug (owner,
            // 0.47.0: *"if there is a 1 I cannot make it 10 — it becomes 01"*, and the saved password
            // could not be edited at all). Two separate things were wrong:
            //
            // 1. `onFocusSelectAll = false` leaves the caret at the field's STORED position, which for
            //    text set from code is 0. So focusing a pre-filled box put the caret at the FRONT and
            //    every keystroke PREPENDED. Fixed by CaretToEnd below: focus lands at the end, which is
            //    what "edit this value" means everywhere else on a phone.
            // 2. On Android the soft keyboard owned the text buffer, so the caret was the OS's and
            //    tapping inside the field could not move it — there was no way to reach the character
            //    you wanted to delete. `shouldHideMobileInput = true` hands the text back to TMP: the
            //    keyboard only delivers keystrokes, TMP draws and places its own caret, and a tap
            //    inside the field positions it. (If a device ever refuses to open a keyboard for a
            //    hidden input, THIS is the line to flip back — the symptom would be "no keyboard at
            //    all", never "the caret is stuck".)
            field.shouldHideMobileInput = true;
            field.gameObject.AddComponent<CaretToEnd>();

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

    /// <summary>Puts the caret at the END of whatever a text box already holds, the moment it takes
    /// focus. Attached to every <see cref="UiKit.InputField"/>.
    ///
    /// A box you focus is one you mean to EDIT — the saved password, the rate that says 1, the server
    /// URL. Landing at the front instead turns every keystroke into a prepend (1 → 01), which is how
    /// this shipped in 0.47.0 once select-on-focus was turned off for B6.
    ///
    /// ⚠ It waits a frame on purpose. `ActivateInputField` and the soft keyboard both finish AFTER
    /// the select event, and either will overwrite a caret set inside the handler.
    /// ⚠ It hooks SELECT, not click — so it fires when the box gains focus and never again. Tapping
    /// inside an already-focused box still repositions the caret normally, which is the whole point of
    /// having given TMP the caret back.</summary>
    public sealed class CaretToEnd : MonoBehaviour, ISelectHandler
    {
        private TMP_InputField _field;

        private void Awake() => _field = GetComponent<TMP_InputField>();

        public void OnSelect(BaseEventData _)
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(PlaceCaret());
        }

        private IEnumerator PlaceCaret()
        {
            yield return null;
            if (_field == null) yield break;
            int end = _field.text?.Length ?? 0;
            _field.caretPosition = end;
            _field.stringPosition = end;
            _field.selectionAnchorPosition = end;
            _field.selectionFocusPosition = end;
            _field.ForceLabelUpdate();
        }
    }
}
