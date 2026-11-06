using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// GameUi, continued: `BL-364` items 1 + 2 — the KEYS window (every desktop shortcut, rebindable) and KEYBOARD
    /// MOVEMENT. The bindings themselves live in <see cref="KeyBindings"/>; this file is the window that edits them, the
    /// per-frame reading of them (<see cref="UpdateHotkeys"/>), and the walk that WASD/the arrows drive.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        private RectTransform _keysPanel;
        private TextMeshProUGUI _keysStatus;
        private readonly System.Collections.Generic.List<(KeyBindings.KeyAction action, int slot, Button button)> _keyButtons
            = new System.Collections.Generic.List<(KeyBindings.KeyAction, int, Button)>();

        /// <summary>The box waiting for a key (null = none), and the frame a capture ended — the Esc that cancels a
        /// capture must not also walk the Back stack (<see cref="UpdateBackButton"/> reads <see cref="KeyCaptureOwnsEsc"/>).</summary>
        private KeyBindings.KeyAction _captureAction;
        private int _captureSlot;
        private int _captureEndFrame = -1;

        private bool KeyCaptureOwnsEsc => _captureAction != null || _captureEndFrame == Time.frameCount;

        private const string KeysHelp = "Click a box, then press the new key (with Shift / Ctrl / Alt if you want them). "
                                        + "Esc cancels, Backspace clears.";

        private void BuildKeysWindow()
        {
            // On the setup canvas, beside Settings and Sound: it opens from Settings, which the login screen reaches.
            _keysPanel = UiKit.PanelBox(_setupRoot, "Keys");
            UiKit.Place(_keysPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(640f, 690f));
            var frame = _keysPanel.GetChild(0);
            float chrome = UiKit.WindowChrome(_keysPanel, "Keys", () => { EndKeyCapture(); CloseWindow(_keysPanel); });

            _keysStatus = UiKit.Label(frame, KeysHelp, 14f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_keysStatus.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(18f, -chrome - 6f), new Vector2(604f, 40f));
            const float statusH = 50f;

            // The rows scroll, as in Settings: 46 of them do not fit a phone-sized screen.
            var view = UiKit.Box(frame, "Viewport", new Color(0, 0, 0, 0.001f));
            var viewRect = UiKit.Stretch(UiKit.Rect(view.gameObject), 0f, chrome + statusH, 0f, SetupFooter);
            view.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.viewport = viewRect;
            scroll.content = content;

            float y = -6f;
            string group = null;
            foreach (var action in KeyBindings.All)
            {
                if (action.Group != group)
                {
                    group = action.Group;
                    var head = UiKit.Label(content, group, 16f, UiKit.Accent, TextAlignmentOptions.BottomLeft);
                    UiKit.Place(UiKit.Rect(head.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(18f, y), new Vector2(400f, 30f));
                    y -= 34f;
                }

                var name = UiKit.Label(content, action.Name, 15f, UiKit.Text, TextAlignmentOptions.Left);
                UiKit.Place(UiKit.Rect(name.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(30f, y), new Vector2(280f, 34f));
                for (int slot = 0; slot < 2; slot++)
                {
                    var a = action;
                    int s = slot;
                    var box = UiKit.TextButton(content, "", () => BeginKeyCapture(a, s), 15f);
                    UiKit.Place(UiKit.Rect(box.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                                new Vector2(316f + slot * 152f, y), new Vector2(144f, 34f));
                    _keyButtons.Add((action, slot, box));
                }
                y -= 40f;
            }
            content.sizeDelta = new Vector2(0f, -y);

            var save = UiKit.TextButton(frame, "Save", () => PlayerPrefs.Save(), 16f);
            UiKit.Place(UiKit.Rect(save.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(18f, 16f), new Vector2(140f, 40f));
            var reset = UiKit.TextButton(frame, "Reset keys", () =>
            {
                EndKeyCapture();
                KeyBindings.ResetAll();
                RefreshKeyLabels();
                _keysStatus.text = "Every key is back to its default.";
            }, 16f);
            UiKit.Place(UiKit.Rect(reset.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(170f, 16f), new Vector2(220f, 40f));

            RefreshKeyLabels();
            _keysPanel.gameObject.SetActive(false);
        }

        private void OpenKeysWindow() => ToggleWindow(_keysPanel);

        private void RefreshKeyLabels()
        {
            foreach (var (action, slot, button) in _keyButtons)
            {
                bool waiting = action == _captureAction && slot == _captureSlot;
                UiKit.SetButtonText(button, waiting ? "press a key…" : action.Keys[slot].Display());
                button.targetGraphic.color = waiting ? new Color(0.20f, 0.45f, 0.25f, 0.95f) : UiKit.PanelLight;
            }
            if (_pageLabel != null) RefreshSkillBar();   // the squares' corner tags are the bound keys
        }

        private void BeginKeyCapture(KeyBindings.KeyAction action, int slot)
        {
            _captureAction = action;
            _captureSlot = slot;
            _keysStatus.text = "Press the new key for \"" + action.Name + "\".  Esc cancels, Backspace clears.";
            RefreshKeyLabels();
        }

        private void EndKeyCapture()
        {
            if (_captureAction == null) return;
            _captureAction = null;
            _captureEndFrame = Time.frameCount;
            RefreshKeyLabels();
        }

        /// <summary>Runs every frame from <see cref="Update"/> — the window is reachable from the login screen, where the
        /// in-world hotkeys do not run.</summary>
        private void UpdateKeyCapture()
        {
            if (_captureAction == null) return;
            if (_keysPanel == null || !_keysPanel.gameObject.activeInHierarchy) { EndKeyCapture(); return; }
            var key = KeyBindings.PressedThisFrame();
            if (key == null) return;

            var action = _captureAction;
            int slot = _captureSlot;
            if (key == KeyCode.Escape)
            {
                EndKeyCapture();
                _keysStatus.text = KeysHelp;
                return;
            }
            if (key == KeyCode.Backspace || key == KeyCode.Delete)
            {
                KeyBindings.Bind(action, slot, KeyBind.None);
                EndKeyCapture();
                _keysStatus.text = "\"" + action.Name + "\" key cleared.";
                return;
            }

            var bind = new KeyBind(key.Value, KeyBind.ShiftHeld, KeyBind.CtrlHeld, KeyBind.AltHeld);
            string taken = KeyBindings.Bind(action, slot, bind);
            EndKeyCapture();
            _keysStatus.text = bind.Display() + " is now \"" + action.Name + "\"."
                               + (taken != null ? "  It was taken from \"" + taken + "\", which is now unbound there." : "");
        }

        // ----- the per-frame reading -------------------------------------------------------------

        /// <summary>`BL-360` + `BL-364` — the keyboard, in the world. Every key is <see cref="KeyBindings"/>' (the Keys
        /// window changes them); the defaults are 0.265.0's: 1…0 = the main bar's ten squares, Shift+1…0 = extra bar 1,
        /// Ctrl+1…0 = extra bar 2 (Alt in a browser), PgUp/PgDn = the bar's page, B C K L M = Bag / Character / Skills /
        /// Quests / Menu, Enter = type in chat, WASD + arrows = walk. Esc = Back, as before, and is not rebindable.
        /// Silent while a text field has the caret — typing "1" or "w" in chat must not cast or walk.</summary>
        private void UpdateHotkeys()
        {
            var selected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            bool typing = selected != null && selected.GetComponent<TMP_InputField>() is { isFocused: true };
            if (typing || _captureAction != null) { StopKeyMove(); return; }

            UpdateKeyMove();
            if (!Input.anyKeyDown) return;

            for (int bar = 0; bar < 3; bar++)
                for (int n = 0; n < KeyBindings.BarSlots; n++)
                {
                    if (!KeyBindings.Down(KeyBindings.BarSlot(bar, n))) continue;
                    int visible = bar * SlotsPerPage + n;
                    if (bar == 0 ? n < _mainCount : visible - _mainCount < _extraSlots) FireSlot(visible);
                    return;
                }

            if (KeyBindings.Down(KeyBindings.PageNext)) PageBy(1);
            else if (KeyBindings.Down(KeyBindings.PagePrev)) PageBy(-1);
            else if (KeyBindings.Down(KeyBindings.Bag)) ToggleWindow(_bagPanel);
            else if (KeyBindings.Down(KeyBindings.Character)) ToggleWindow(_statsPanel);
            else if (KeyBindings.Down(KeyBindings.Skills)) ToggleWindow(_skillsPanel);
            else if (KeyBindings.Down(KeyBindings.Quests)) ToggleWindow(_questPanel);
            else if (KeyBindings.Down(KeyBindings.Menu)) ToggleWindow(_menuPanel);
            else if (KeyBindings.Down(KeyBindings.Chat) && _chatSubmitFrame != Time.frameCount)
            {
                _commandField.Select();
                _commandField.ActivateInputField();
            }
        }

        // ----- keyboard movement (`BL-364` item 2) -----------------------------------------------

        /// <summary>
        /// WASD / the arrows walk you RELATIVE TO THE CAMERA (W = up the screen, whichever way it is turned). The server
        /// only takes a point (`Move(x, y)`), so a held key sends a point <see cref="KeyMoveLead"/> seconds of walking
        /// ahead of where you stand, and re-sends it every <see cref="KeyMoveResend"/>s or the moment the direction
        /// changes, so the point never runs out under you. Letting go sends the spot you are on, which stops you.
        /// The lead is long enough to ride out a late packet and short enough that a lost "stop" walks you less
        /// than a step.
        /// </summary>
        private const float KeyMoveLead = 0.6f, KeyMoveResend = 0.25f;
        private bool _keyMoving;
        private Vector3 _keyMoveDir;
        private float _keyMoveSentAt;

        private void UpdateKeyMove()
        {
            float x = (KeyBindings.Held(KeyBindings.MoveRight) ? 1f : 0f) - (KeyBindings.Held(KeyBindings.MoveLeft) ? 1f : 0f);
            float z = (KeyBindings.Held(KeyBindings.MoveForward) ? 1f : 0f) - (KeyBindings.Held(KeyBindings.MoveBack) ? 1f : 0f);
            if (x == 0f && z == 0f) { StopKeyMove(); return; }

            var self = Boot.Entities != null ? Boot.Entities.Find(Boot.SelfId) : null;
            if (self == null || !Boot.Entities.TryGetState(Boot.SelfId, out var dto)) return;

            // The camera's yaw turns the keys: at pitch 90 the screen's up IS this direction (CameraRig.Pose).
            float yaw = Boot.CameraRig != null ? Boot.CameraRig.Yaw : 0f;
            var dir = (Quaternion.Euler(0f, yaw, 0f) * new Vector3(x, 0f, z)).normalized;

            bool turned = !_keyMoving || Vector3.Angle(dir, _keyMoveDir) > 3f;
            if (!turned && Time.unscaledTime - _keyMoveSentAt < KeyMoveResend) return;

            // At least a few units ahead even when the speed is not known yet (0 = stunned/rooted: Move says why).
            float lead = Mathf.Max(dto.Speed * KeyMoveLead, 20f) * WorldMapper.Scale;
            var target = WorldMapper.ToServer(self.transform.position + dir * lead);
            _keyMoving = true;
            _keyMoveDir = dir;
            _keyMoveSentAt = Time.unscaledTime;
            Boot.CancelWalkToTalk();   // a new order, exactly as a ground tap is
            Boot.Move(target.x, target.y, fromKeys: true);
        }

        private void StopKeyMove()
        {
            if (!_keyMoving) return;
            _keyMoving = false;
            var self = Boot.Entities != null ? Boot.Entities.Find(Boot.SelfId) : null;
            if (self == null) return;
            var here = WorldMapper.ToServer(self.transform.position);
            Boot.Move(here.x, here.y, fromKeys: true);
        }
    }
}
