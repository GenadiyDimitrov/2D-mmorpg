using System;
using System.Collections.Generic;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>
    /// The whole client UI: connection strip, login, character select/create, and the in-world HUD.
    ///
    /// It is deliberately IMGUI (OnGUI). uGUI/TextMeshPro would look nicer but needs a Canvas,
    /// prefabs, font assets and Inspector wiring — none of which survive being authored outside the
    /// Unity Editor. IMGUI is pure code: it drops into a scene that contains nothing but GameBoot
    /// and works on the device immediately. That trade is right while the job is "tell me whether
    /// the thing is connected and moving"; a proper uGUI pass belongs with the real art pass.
    ///
    /// Everything is drawn through a scaled GUI.matrix so the layout is authored once against a
    /// ~500-unit-tall virtual screen and stays finger-sized on a phone.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        public GameBoot Boot;

        [Tooltip("Virtual height the UI is authored against; the real screen scales to it.")]
        public float ReferenceShortSide = 500f;

        private float _scale = 1f;
        private float _vw, _vh;

        // login form state
        private string _url = "";
        private string _user = "";
        private string _pass = "";
        private bool _urlLoaded;

        // character creation
        private bool _creating;
        private string _newName = "";
        private int _raceIndex;
        private int _classIndex;
        private static readonly Race[] Races = { Race.Human, Race.Elf, Race.Ork };  // God is debug-only
        private static readonly BaseClass[] Classes = { BaseClass.Fighter, BaseClass.Mage };

        // console
        private bool _showConsole;
        private Vector2 _consoleScroll;
        private int _seenRevision = -1;
        private string _chat = "";

        private readonly List<Rect> _blockRects = new List<Rect>();

        private GUIStyle _panel, _label, _small, _title, _button, _field;
        private Texture2D _panelTex, _barBg, _barHp, _barMp, _barXp;

        // confirm dialog (the back-button exit ladder)
        private string _confirmMessage;
        private string _confirmOk;
        private Action _confirmAction;
        private bool _quitConfirmed;
        private int _lastBackFrame = -1;

        // ----- back button / graceful exit --------------------------------------------------------

        /// <summary>
        /// The phone's back button walks OUT of the game one step at a time instead of killing it:
        /// in world → character select, character select → log out, login screen → quit. Every step
        /// asks first, so a stray back press can never drop a live session.
        ///
        /// Android delivers back as <see cref="KeyCode.Escape"/>, and the player would otherwise quit
        /// the process outright — hence the <see cref="Application.wantsToQuit"/> veto as well: we
        /// only let the app die once the player has actually confirmed it.
        /// </summary>
        private void Awake()
        {
            Application.wantsToQuit += OnWantsToQuit;
        }

        private void OnDestroy()
        {
            Application.wantsToQuit -= OnWantsToQuit;
        }

        private bool OnWantsToQuit()
        {
            if (_quitConfirmed) return true;
            AskBack();
            return false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) AskBack();
            UpdateSkillBarSwipe();
        }

        private void AskBack()
        {
            if (Boot == null) return;

            // A single back press can arrive as BOTH an Escape key and a quit request; without this
            // the dialog would open and immediately close again in the same frame.
            if (_lastBackFrame == Time.frameCount) return;
            _lastBackFrame = Time.frameCount;

            if (_confirmMessage != null) { Dismiss(); return; }   // back also means "cancel"

            switch (Boot.Phase)
            {
                case ClientPhase.InWorld:
                    Ask("Leave the world and return to character select?", "Leave", Boot.LeaveWorld);
                    break;
                case ClientPhase.CharacterSelect:
                case ClientPhase.Entering:
                    Ask("Log out of this account?", "Log out", Boot.Logout);
                    break;
                default:
                    Ask("Quit the game?", "Quit", Quit);
                    break;
            }
        }

        private void Ask(string message, string okLabel, Action action)
        {
            _confirmMessage = message;
            _confirmOk = okLabel;
            _confirmAction = action;
        }

        private void Dismiss()
        {
            _confirmMessage = null;
            _confirmOk = null;
            _confirmAction = null;
        }

        private void Quit()
        {
            _quitConfirmed = true;
            Application.Quit();
        }

        // ----- input blocking -------------------------------------------------------------------

        /// <summary>True when a screen point (bottom-left origin, as Input reports) lands on a UI
        /// panel — TouchInput asks this so tapping "Login" doesn't also order a walk to that spot.</summary>
        public bool BlocksScreenPoint(Vector2 point)
        {
            var guiPoint = new Vector2(point.x, Screen.height - point.y);
            foreach (var r in _blockRects) if (r.Contains(guiPoint)) return true;
            return false;
        }

        /// <summary>Register a rect (in VIRTUAL units) as UI, converting to real screen pixels.
        /// Only collected on Repaint: OnGUI runs several times per frame (Layout, Repaint, one pass
        /// per input event) and collecting on every pass would pile up duplicates.</summary>
        private void Block(Rect virtualRect)
        {
            if (Event.current.type != EventType.Repaint) return;
            _blockRects.Add(new Rect(virtualRect.x * _scale, virtualRect.y * _scale,
                                     virtualRect.width * _scale, virtualRect.height * _scale));
        }

        // ----- soft keyboard ---------------------------------------------------------------------

        /// <summary>Height the on-screen keyboard occupies, in the HUD's virtual units (0 when hidden).
        /// Some devices report a zero area even while the keyboard is up, so fall back to ~45% of the
        /// screen. In the Editor the keyboard is never visible, so this is a no-op there.</summary>
        private float KeyboardHeightVirtual()
        {
            if (!TouchScreenKeyboard.visible) return 0f;
            float px = TouchScreenKeyboard.area.height;
            if (px <= 1f) px = Screen.height * 0.45f;
            return px / _scale;
        }

        /// <summary>Slide a panel up just enough that its bottom clears the soft keyboard, without
        /// letting it ride up under the status strip.</summary>
        private Rect LiftAboveKeyboard(Rect r)
        {
            float kb = KeyboardHeightVirtual();
            if (kb <= 0f) return r;
            float keyboardTop = _vh - kb;
            float overlap = (r.y + r.height + 6f) - keyboardTop;
            if (overlap > 0f) r.y -= overlap;
            if (r.y < 26f) r.y = 26f;
            return r;
        }

        // ----- lifecycle ------------------------------------------------------------------------

        private void EnsureStyles()
        {
            if (_panel != null) return;

            _panelTex = SolidTexture(new Color(0.07f, 0.08f, 0.11f, 0.92f));
            _barBg = SolidTexture(new Color(0.15f, 0.16f, 0.2f, 1f));
            _barHp = SolidTexture(new Color(0.78f, 0.22f, 0.24f, 1f));
            _barMp = SolidTexture(new Color(0.24f, 0.45f, 0.85f, 1f));
            _barXp = SolidTexture(new Color(0.85f, 0.7f, 0.25f, 1f));

            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = _panelTex;
            _panel.border = new RectOffset(2, 2, 2, 2);
            _panel.padding = new RectOffset(8, 8, 8, 8);

            _label = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            _label.normal.textColor = new Color(0.9f, 0.92f, 0.95f);

            _small = new GUIStyle(_label) { fontSize = 11 };
            _small.normal.textColor = new Color(0.68f, 0.72f, 0.78f);

            _title = new GUIStyle(_label) { fontSize = 18, fontStyle = FontStyle.Bold };

            _button = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            _field = new GUIStyle(GUI.skin.textField) { fontSize = 14 };
        }

        private static Texture2D SolidTexture(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        private void OnGUI()
        {
            if (Boot == null) return;
            EnsureStyles();

            if (!_urlLoaded)
            {
                _url = Boot.ServerUrl;
                _user = Boot.Username;
                _pass = Boot.Password;
                _newName = Boot.CharacterName;
                _urlLoaded = true;
            }

            _scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / ReferenceShortSide);
            _vw = Screen.width / _scale;
            _vh = Screen.height / _scale;

            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_scale, _scale, 1f));

            if (Event.current.type == EventType.Repaint) _blockRects.Clear();

            DrawStatusStrip();

            switch (Boot.Phase)
            {
                case ClientPhase.Offline:
                case ClientPhase.Connecting:
                case ClientPhase.Authenticating:
                    DrawLogin();
                    break;
                case ClientPhase.CharacterSelect:
                case ClientPhase.Entering:
                    DrawCharacterSelect();
                    break;
                case ClientPhase.InWorld:
                    DrawNameplates();
                    DrawInWorld();
                    break;
            }

            if (_showConsole) DrawConsole();
            if (_confirmMessage != null) DrawConfirm();

            GUI.matrix = prevMatrix;
        }

        // ----- confirm dialog ---------------------------------------------------------------------

        private void DrawConfirm()
        {
            // The WHOLE screen is registered as UI while a dialog is open, so a tap meant for
            // "Cancel" can never also order a walk to the ground behind it.
            Block(new Rect(0, 0, _vw, _vh));

            var box = new Rect(_vw * 0.5f - 150f, _vh * 0.5f - 55f, 300f, 110f);
            GUI.Box(box, GUIContent.none, _panel);
            GUI.Label(new Rect(box.x + 14f, box.y + 14f, box.width - 28f, 44f), _confirmMessage, _label);

            float bw = (box.width - 42f) * 0.5f;
            if (GUI.Button(new Rect(box.x + 14f, box.yMax - 40f, bw, 28f), "Cancel", _button))
                Dismiss();

            if (GUI.Button(new Rect(box.xMax - 14f - bw, box.yMax - 40f, bw, 28f), _confirmOk, _button))
            {
                var action = _confirmAction;
                Dismiss();
                if (action != null) action();
            }
        }

        // ----- status strip ---------------------------------------------------------------------

        private void DrawStatusStrip()
        {
            var r = new Rect(0, 0, _vw, 24);
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            bool live = Boot.SecondsSinceFrame >= 0f && Boot.SecondsSinceFrame < 2f;
            Color dot = Boot.Phase == ClientPhase.InWorld && live ? new Color(0.4f, 0.95f, 0.45f)
                      : Boot.IsConnected ? new Color(0.95f, 0.8f, 0.3f)
                      : new Color(0.9f, 0.35f, 0.35f);

            GUI.color = dot;
            GUI.Box(new Rect(7, 8, 9, 9), GUIContent.none, _panel);
            GUI.color = Color.white;

            // The frame counter is the honest liveness signal: a socket can sit "Connected" while the
            // server has stopped sending, and only "frames: N @ x/s" tells those two apart.
            string feed = Boot.FramesReceived == 0
                ? "no frames yet"
                : "frames " + Boot.FramesReceived + " @ " + Boot.FramesPerSecond.ToString("0.0") + "/s"
                  + (live ? "" : "  STALLED " + Boot.SecondsSinceFrame.ToString("0.0") + "s");

            GUI.Label(new Rect(22, 4, _vw - 130, 20),
                Boot.Phase + " · " + feed + " · entities " + (Boot.Entities != null ? Boot.Entities.Count : 0), _label);

            if (GUI.Button(new Rect(_vw - 104, 3, 48, 18), "Log", _button)) _showConsole = !_showConsole;
            GUI.Label(new Rect(_vw - 52, 4, 50, 20), "v" + GameConstants.GameVersion, _small);
        }

        // ----- login ------------------------------------------------------------------------------

        private void DrawLogin()
        {
            float w = Mathf.Min(360f, _vw - 24f);
            float h = 250f;
            var r = new Rect((_vw - w) / 2f, Mathf.Max(34f, (_vh - h) / 2f - 20f), w, h);
            r = LiftAboveKeyboard(r);   // so the field you're typing in isn't behind the soft keyboard
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20));
            GUILayout.Label("Sign in", _title);

            GUILayout.Label("Server", _small);
            _url = GUILayout.TextField(_url, _field, GUILayout.Height(26));

            GUILayout.Label("Username", _small);
            _user = GUILayout.TextField(_user, _field, GUILayout.Height(26));

            GUILayout.Label("Password", _small);
            _pass = GUILayout.PasswordField(_pass, '*', _field, GUILayout.Height(26));

            GUILayout.Space(6);
            GUI.enabled = !Boot.IsBusy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Login", _button, GUILayout.Height(34))) Submit(register: false);
            GUILayout.Space(6);
            if (GUILayout.Button("Register", _button, GUILayout.Height(34))) Submit(register: true);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Space(4);
            var msgStyle = new GUIStyle(_small) { wordWrap = true };
            if (!string.IsNullOrEmpty(Boot.LastError)) msgStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
            GUILayout.Label(Boot.IsBusy ? "Working …" : Boot.StatusMessage, msgStyle);

            GUILayout.EndArea();
        }

        private void Submit(bool register)
        {
            Boot.ServerUrl = _url.Trim();
            _ = Boot.ConnectAndLogin(_user.Trim(), _pass, register);
        }

        // ----- character select --------------------------------------------------------------------

        private void DrawCharacterSelect()
        {
            float w = Mathf.Min(400f, _vw - 24f);
            float h = Mathf.Min(320f, _vh - 60f);
            var r = new Rect((_vw - w) / 2f, 34f, w, h);
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 10, r.width - 24, r.height - 20));

            if (_creating) { DrawCreateForm(); GUILayout.EndArea(); return; }

            GUILayout.Label("Characters", _title);
            GUILayout.Label("Account: " + Boot.Username, _small);

            if (Boot.Characters.Length == 0)
                GUILayout.Label("No characters on this account yet.", _small);

            foreach (var c in Boot.Characters)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(c.Name + "   Lv " + c.Level + "  " + c.Race + " " + c.BaseClass, _label,
                                GUILayout.Height(30));
                GUILayout.FlexibleSpace();
                GUI.enabled = !Boot.IsBusy;
                if (GUILayout.Button("Enter", _button, GUILayout.Width(72), GUILayout.Height(30)))
                    _ = Boot.EnterWorld(c.Id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create character", _button, GUILayout.Height(32)))
            {
                _creating = true;
                Boot.LastError = null;
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Logout", _button, GUILayout.Width(84), GUILayout.Height(32)))
            {
                _pass = "";           // don't leave the previous account's password sitting in the form
                Boot.Logout();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label(Boot.IsBusy ? "Working …" : Boot.StatusMessage,
                            new GUIStyle(_small) { wordWrap = true });
            GUILayout.EndArea();
        }

        private void DrawCreateForm()
        {
            GUILayout.Label("New character", _title);

            GUILayout.Label("Name", _small);
            _newName = GUILayout.TextField(_newName, _field, GUILayout.Height(26));

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Race", _small, GUILayout.Width(50));
            if (GUILayout.Button(Races[_raceIndex].ToString(), _button, GUILayout.Height(28)))
                _raceIndex = (_raceIndex + 1) % Races.Length;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Class", _small, GUILayout.Width(50));
            if (GUILayout.Button(Classes[_classIndex].ToString(), _button, GUILayout.Height(28)))
                _classIndex = (_classIndex + 1) % Classes.Length;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUI.enabled = !Boot.IsBusy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", _button, GUILayout.Height(34)))
            {
                _ = Boot.CreateCharacter(_newName.Trim(), Races[_raceIndex], Classes[_classIndex]);
                _creating = false;
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Cancel", _button, GUILayout.Height(34))) _creating = false;
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUILayout.Space(4);
            GUILayout.Label(Boot.StatusMessage, new GUIStyle(_small) { wordWrap = true });
        }

        // ----- in-world ------------------------------------------------------------------------------

        private void DrawInWorld()
        {
            DrawSelfPanel();
            DrawTargetPanel();
            DrawSkillBar();
            DrawCommandBar();
            DrawActionBar();
        }

        // ----- skill bar --------------------------------------------------------------------------

        private const int BarColumns = 6;
        private const int BarRows = 2;
        private const int SlotsPerPage = BarColumns * BarRows;   // 12 — one PAGE of the server's 60
        private const int BarPages = GameConstants.SkillBarSlots / SlotsPerPage;   // 5

        private int _barPage;
        private int _swipeFinger = -1;
        private float _swipeStartX;
        private Rect _barScreenRect;

        /// <summary>
        /// Two rows of six on the right — page N of the server's 60-slot bar, swipe left/right to
        /// change page. Row 1 is slots 1-6 of the page, row 2 is 7-12, so the layout matches what the
        /// WPF client shows for the same character.
        ///
        /// It renders <see cref="GameBoot.SkillBar"/> verbatim and never writes one back. See the
        /// comment on that property for why that rule is absolute.
        /// </summary>
        private void DrawSkillBar()
        {
            const float slot = 44f, pad = 4f;
            float w = BarColumns * slot + (BarColumns + 1) * pad;
            float h = BarRows * slot + (BarRows + 1) * pad + 16f;   // +16 for the page strip
            var r = new Rect(_vw - w - 6f, _vh - h - 46f, w, h);

            Block(r);
            GUI.Box(r, GUIContent.none, _panel);
            _barScreenRect = new Rect(r.x * _scale, r.y * _scale, r.width * _scale, r.height * _scale);

            var bar = Boot.SkillBar;
            int first = _barPage * SlotsPerPage;

            for (int i = 0; i < SlotsPerPage; i++)
            {
                int index = first + i;
                float x = r.x + pad + (i % BarColumns) * (slot + pad);
                float y = r.y + pad + (i / BarColumns) * (slot + pad);
                DrawSlot(new Rect(x, y, slot, slot), index,
                         bar != null && index < bar.Length ? bar[index] : null);
            }

            // Page strip: "‹  2 / 5  ›". The arrows exist because a swipe is invisible until someone
            // tells you it's there — they are the discoverable version of the same gesture.
            float sy = r.yMax - 15f;
            if (GUI.Button(new Rect(r.x + pad, sy, 22f, 13f), "‹", _small)) PageBy(-1);
            GUI.Label(new Rect(r.x + 28f, sy, w - 56f, 13f),
                      "  " + (_barPage + 1) + " / " + BarPages, _small);
            if (GUI.Button(new Rect(r.xMax - pad - 22f, sy, 22f, 13f), "›", _small)) PageBy(1);
        }

        private void PageBy(int delta)
        {
            _barPage = Mathf.Clamp(_barPage + delta, 0, BarPages - 1);
        }

        private void DrawSlot(Rect r, int index, string token)
        {
            // The hotkey number is the slot's position ON THE PAGE, 1-12 — the same numbering the WPF
            // client puts under its first two rows.
            string hotkey = (index % SlotsPerPage + 1).ToString();

            if (string.IsNullOrEmpty(token))
            {
                GUI.Box(r, GUIContent.none, _panel);
                GUI.Label(new Rect(r.x + 3, r.y + 1, 16, 12), hotkey, _small);
                return;
            }

            string face = SlotFace(token, out bool usable);
            GUI.enabled = usable;
            if (GUI.Button(r, face, _button)) Boot.UseSlot(token);
            GUI.enabled = true;
            GUI.Label(new Rect(r.x + 3, r.y + 1, 16, 12), hotkey, _small);
        }

        /// <summary>What to print on a slot. Skills use their authored icon or the catalog-wide
        /// abbreviation — resolved through <see cref="Abbreviations"/> rather than derived here, since
        /// deriving per-skill is what once gave three different heal-over-times the same "HOT".</summary>
        private string SlotFace(string token, out bool usable)
        {
            usable = true;

            if (ActionCatalog.FromToken(token) is ActionDef action)
                return string.IsNullOrEmpty(action.Icon) ? Abbreviations.For(action.Name) : action.Icon;

            if (GameConstants.IsItemSlot(token))
                return "▣";

            var def = SkillCatalog.Get(token);
            if (def == null) { usable = false; return "?"; }

            // Grey out what this character has not learned — the bar is per-class and a subclass can
            // legitimately be holding a bar full of skills it does not have.
            usable = Boot.Learned.Count == 0 || Boot.Learned.ContainsKey(token);

            if (!string.IsNullOrWhiteSpace(def.Icon)) return def.Icon;
            return string.IsNullOrWhiteSpace(def.Abbrev) ? Abbreviations.For(def.Name) : def.Abbrev;
        }

        /// <summary>Swipe across the bar to change page. Handled from raw touches rather than IMGUI
        /// events: IMGUI has no gesture concept, and the buttons underneath would swallow a drag.</summary>
        private void UpdateSkillBarSwipe()
        {
            if (Boot == null || Boot.Phase != ClientPhase.InWorld) return;
            const float swipePixels = 60f;

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);

                // Input reports touches from the BOTTOM-left; the bar rect is in GUI space, which runs
                // from the TOP-left. Comparing them raw would arm the swipe at the mirrored position.
                var guiPoint = new Vector2(t.position.x, Screen.height - t.position.y);

                if (t.phase == TouchPhase.Began && _barScreenRect.Contains(guiPoint))
                {
                    _swipeFinger = t.fingerId;
                    _swipeStartX = t.position.x;
                }
                else if (t.fingerId == _swipeFinger &&
                         (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
                {
                    float dx = t.position.x - _swipeStartX;
                    if (Mathf.Abs(dx) >= swipePixels * _scale) PageBy(dx < 0 ? 1 : -1);
                    _swipeFinger = -1;
                }
            }
        }

        /// <summary>Always-visible chat + command line, so commands don't require opening the log.
        /// Admins get a hint that slash commands work; everything routes through Boot.Say.</summary>
        private void DrawCommandBar()
        {
            float bh = 26f;
            float y = _vh - 34f - 6f - bh - 4f;   // sits just above the action bar
            var r = new Rect(6, y, _vw - 12, bh);
            r = LiftAboveKeyboard(r);
            Block(r);

            GUI.SetNextControlName("cmdline");
            _chat = GUI.TextField(new Rect(r.x, r.y, r.width - 116, bh), _chat, _field);

            string hint = Boot.IsAdmin ? "/help, /god…" : "say / !world / /w";
            if (string.IsNullOrEmpty(_chat))
                GUI.Label(new Rect(r.x + 6, r.y + 4, r.width - 130, bh), hint, _small);

            bool enter = Event.current.type == EventType.KeyDown
                         && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                         && GUI.GetNameOfFocusedControl() == "cmdline";

            if (GUI.Button(new Rect(r.x + r.width - 110, r.y, 52, bh), "Send", _button) || enter)
                SubmitChat();
            if (GUI.Button(new Rect(r.x + r.width - 54, r.y, 54, bh), "Log", _button))
                _showConsole = !_showConsole;
        }

        private void SubmitChat()
        {
            if (!string.IsNullOrWhiteSpace(_chat)) Boot.Say(_chat);
            _chat = "";
            GUI.FocusControl(null);   // drop focus so movement taps aren't eaten and the keyboard closes
        }

        private void DrawSelfPanel()
        {
            EntityDto self = null;
            if (Boot.Entities != null) Boot.Entities.TryGetState(Boot.SelfId, out self);

            var r = new Rect(6, 30, 200, 78);
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            if (self == null)
            {
                GUI.Label(new Rect(r.x + 8, r.y + 8, r.width - 16, 20), "waiting for your entity …", _small);
                return;
            }

            int level = Boot.Progress != null ? Boot.Progress.Level : self.Level;
            GUI.Label(new Rect(r.x + 8, r.y + 5, r.width - 16, 18), self.Name + "   Lv " + level, _label);

            Bar(new Rect(r.x + 8, r.y + 25, r.width - 16, 12), self.Hp, self.MaxHp, _barHp, "HP");
            Bar(new Rect(r.x + 8, r.y + 40, r.width - 16, 12), self.Mp, self.MaxMp, _barMp, "MP");

            if (Boot.Progress != null && Boot.Progress.ExpToNext > 0)
                Bar(new Rect(r.x + 8, r.y + 55, r.width - 16, 8),
                    (int)Boot.Progress.Exp, (int)Boot.Progress.ExpToNext, _barXp, null);

            // Raw server coordinates: the fastest way to confirm movement is real and not a camera
            // drift — walk, and watch these change.
            GUI.Label(new Rect(r.x + 8, r.y + 64, r.width - 16, 14),
                "pos " + Mathf.RoundToInt(self.X) + ", " + Mathf.RoundToInt(self.Y)
                + "   spd " + self.Speed.ToString("0") + "   gold " + Boot.Gold, _small);
        }

        private void DrawTargetPanel()
        {
            if (!Boot.TargetId.HasValue) return;
            if (Boot.Entities == null || !Boot.Entities.TryGetState(Boot.TargetId.Value, out var t)) return;

            var r = new Rect(_vw - 206, 30, 200, 56);
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            string level = t.Kind == EntityKind.Player && t.Level <= 0 ? "" : "  Lv " + t.Level;
            GUI.Label(new Rect(r.x + 8, r.y + 5, r.width - 16, 18),
                      t.Name + level + (t.Dead ? "  (dead)" : ""), _label);
            Bar(new Rect(r.x + 8, r.y + 26, r.width - 16, 12), t.Hp, t.MaxHp, _barHp, "HP");
            GUI.Label(new Rect(r.x + 8, r.y + 40, r.width - 16, 14),
                      t.Kind + (t.Aggressive ? "  aggressive" : ""), _small);
        }

        private void DrawActionBar()
        {
            float bh = 34f;
            var r = new Rect(6, _vh - bh - 6, _vw - 12, bh);
            Block(r);

            float bw = Mathf.Min(84f, (r.width - 24) / 5f);
            float x = r.x;

            if (GUI.Button(new Rect(x, r.y, bw, bh), "Attack", _button))
            {
                if (Boot.TargetId.HasValue) Boot.Attack(Boot.TargetId.Value);
            }
            x += bw + 4;
            if (GUI.Button(new Rect(x, r.y, bw, bh), "Sit/Stand", _button))
            {
                bool sitting = Boot.Stats != null && Boot.Stats.MoveState == MoveState.Sitting;
                Boot.SetMoveState(sitting ? MoveState.Running : MoveState.Sitting);
            }
            x += bw + 4;
            if (GUI.Button(new Rect(x, r.y, bw, bh), "Walk/Run", _button))
            {
                bool walking = Boot.Stats != null && Boot.Stats.MoveState == MoveState.Walking;
                Boot.SetMoveState(walking ? MoveState.Running : MoveState.Walking);
            }
            x += bw + 4;
            if (GUI.Button(new Rect(x, r.y, bw, bh), "Respawn", _button)) Boot.Respawn();
            x += bw + 4;
            if (GUI.Button(new Rect(x, r.y, bw, bh), "Leave", _button)) Boot.LeaveWorld();
        }

        private void Bar(Rect r, int value, int max, Texture2D fill, string label)
        {
            GUI.DrawTexture(r, _barBg);
            float pct = max > 0 ? Mathf.Clamp01((float)value / max) : 0f;
            if (pct > 0f) GUI.DrawTexture(new Rect(r.x, r.y, r.width * pct, r.height), fill);
            if (label != null)
                GUI.Label(new Rect(r.x + 3, r.y - 2, r.width, r.height + 4),
                          label + " " + value + " / " + max, _small);
        }

        // ----- nameplates ----------------------------------------------------------------------------

        private void DrawNameplates()
        {
            var cam = Camera.main;
            if (cam == null || Boot.Entities == null) return;

            foreach (var kv in Boot.Entities.States)
            {
                var e = kv.Value;
                var view = Boot.Entities.Find(e.Id);
                if (view == null) continue;

                var sp = cam.WorldToScreenPoint(view.transform.position + Vector3.up * 0.9f);
                if (sp.z <= 0f) continue;   // behind the camera

                float gx = sp.x / _scale;
                float gy = (Screen.height - sp.y) / _scale;
                if (gx < -60 || gx > _vw + 60 || gy < 0 || gy > _vh) continue;

                var style = new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = e.Id == Boot.SelfId ? new Color(0.6f, 1f, 0.6f)
                                       : e.Kind == EntityKind.Mob ? new Color(1f, 0.6f, 0.6f)
                                       : e.Kind == EntityKind.Npc ? new Color(1f, 0.95f, 0.6f)
                                       : new Color(0.7f, 0.9f, 1f);

                string name = e.Name + (e.Aggressive ? "*" : "");
                GUI.Label(new Rect(gx - 60, gy - 22, 120, 14), name, style);

                if (e.MaxHp > 0 && e.Kind != EntityKind.Npc)
                {
                    var bar = new Rect(gx - 22, gy - 9, 44, 4);
                    GUI.DrawTexture(bar, _barBg);
                    float pct = Mathf.Clamp01((float)e.Hp / e.MaxHp);
                    if (pct > 0f) GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * pct, bar.height), _barHp);
                }
            }
        }

        // ----- console --------------------------------------------------------------------------------

        private void DrawConsole()
        {
            // A pure LOG VIEWER now — text input lives in the always-on command bar, so there's only
            // ever one focused field (two IMGUI text fields sharing state fight over focus on mobile).
            float h = Mathf.Min(200f, _vh * 0.5f);
            float bottom = Boot.Phase == ClientPhase.InWorld ? 74f : 30f;   // clear the command/action bars
            var r = new Rect(6, _vh - h - bottom, _vw - 12, h);
            Block(r);
            GUI.Box(r, GUIContent.none, _panel);

            var inner = new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 34);
            var lines = ClientLog.Lines;

            // Auto-scroll to the newest line, but only when a new line actually arrived — otherwise
            // the view would fight the user's finger every frame.
            if (_seenRevision != ClientLog.Revision)
            {
                _seenRevision = ClientLog.Revision;
                _consoleScroll.y = float.MaxValue;
            }

            _consoleScroll = GUI.BeginScrollView(inner, _consoleScroll,
                                                 new Rect(0, 0, inner.width - 18, lines.Count * 14 + 4));
            for (int i = 0; i < lines.Count; i++)
            {
                var style = new GUIStyle(_small);
                style.normal.textColor = lines[i].Color;
                GUI.Label(new Rect(2, i * 14, inner.width - 22, 14), lines[i].Text, style);
            }
            GUI.EndScrollView();

            float by = r.y + r.height - 28;
            if (GUI.Button(new Rect(r.x + r.width - 128, by, 60, 24), "Clear", _button)) ClientLog.Clear();
            if (GUI.Button(new Rect(r.x + r.width - 64, by, 58, 24), "Close", _button)) _showConsole = false;
        }
    }
}
