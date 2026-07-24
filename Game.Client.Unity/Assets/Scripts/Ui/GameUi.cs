using System;
using Game.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client
{
    /// <summary>
    /// The client UI, on uGUI + TextMeshPro. Replaces the IMGUI <c>GameHud</c>.
    ///
    /// Why the change: IMGUI was the right tool while the only question was "is it even connected" —
    /// it needs no Canvas, no prefabs and no fonts, so it could be authored entirely outside the
    /// Editor. It is not a destination: it has no layout engine, no styling, it re-runs the whole UI
    /// several times per frame, and movable/resizable windows or a portrait layout are not reachable
    /// from it. uGUI is Unity's standard runtime UI and is where those things live.
    ///
    /// The build is still headless: every panel here is constructed FROM CODE via <see cref="UiKit"/>,
    /// so nothing lives in a prefab or a scene file that only the Editor could edit.
    ///
    /// Structure: this file owns the canvas, the status strip and the account screens (login and
    /// character select). GameUi.World.cs is the same class, continued, and owns everything in world.
    /// </summary>
    public partial class GameUi : MonoBehaviour
    {
        public GameBoot Boot;

        private Canvas _canvas;
        private RectTransform _root;

        // status strip
        private Image _statusDot, _reconnectNotice;
        private TextMeshProUGUI _statusText, _versionText;

        // login
        private RectTransform _loginPanel;
        private TMP_InputField _urlField, _userField, _passField;
        private TextMeshProUGUI _loginStatus;

        // character select
        private RectTransform _selectPanel, _characterList;
        private TextMeshProUGUI _accountLabel, _selectStatus;
        private RectTransform _createPanel;
        private TMP_InputField _nameField;
        private Button _raceButton, _classButton;
        private int _raceIndex, _classIndex;
        private static readonly Race[] Races = { Race.Human, Race.Elf, Race.Ork };   // God is debug-only
        private static readonly BaseClass[] Classes = { BaseClass.Fighter, BaseClass.Mage };

        private float _renderFps;
        private ClientPhase _builtPhase = (ClientPhase)(-1);
        private int _characterRevision = -1;

        private bool _built;

        /// <summary>
        /// Build everything on the first frame — NOT in Awake.
        ///
        /// Awake runs the moment AddComponent is called, which is BEFORE GameBoot assigns itself to
        /// <see cref="Boot"/>. Any builder that touches Boot therefore threw a NullReferenceException
        /// that aborted Awake halfway and left the UI partly constructed: the panels built before the
        /// throw were on screen and clickable, everything after it did not exist, and phase switching
        /// had nothing coherent to show or hide. On the device that looks like the game froze.
        ///
        /// Building here instead means every builder can rely on Boot existing, so the trap cannot be
        /// re-set by the next panel someone adds.
        /// </summary>
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            // Saved look settings first: the canvas reads the reference resolution when it is created,
            // so loading afterwards would build at the default size and only take effect next launch.
            LoadLookPrefs();

            _canvas = UiKit.CreateCanvas("GameUi");
            _canvas.transform.SetParent(transform, false);
            _root = (RectTransform)_canvas.transform;

            BuildStatusStrip();
            BuildLogin();
            BuildCharacterSelect();
            BuildWorld();
            BuildOverlays();
            HookFeedback();
        }

        private void OnDisable()
        {
            Application.wantsToQuit -= OnWantsToQuit;
            UnhookFeedback();
        }

        private bool _fieldsLoaded;

        private void Update()
        {
            if (Boot == null) return;
            EnsureBuilt();

            if (!_fieldsLoaded)
            {
                _fieldsLoaded = true;
                _urlField.text = Boot.ServerUrl;
                _userField.text = Boot.Username;
                _passField.text = Boot.Password;
            }

            RefreshStatusStrip();

            // Screens are BUILT once and shown/hidden; only the visible one is refreshed. Rebuilding
            // per frame is what makes an immediate-mode UI expensive, and it is the habit worth
            // dropping along with IMGUI.
            // Boot.Restoring suppresses the account screens entirely: a silent reconnect after the app
            // was backgrounded re-logs-in and re-enters, and showing login or character-select for the
            // length of that round trip is what produced the "few second freeze then a flash of the
            // character screen" the owner reported. The world stays up; a notice says why it is stuck.
            bool login = !Boot.Restoring &&
                         (Boot.Phase == ClientPhase.Offline || Boot.Phase == ClientPhase.Connecting
                          || Boot.Phase == ClientPhase.Authenticating);
            bool select = !Boot.Restoring &&
                          (Boot.Phase == ClientPhase.CharacterSelect || Boot.Phase == ClientPhase.Entering);
            bool world = Boot.Phase == ClientPhase.InWorld;

            _reconnectNotice.gameObject.SetActive(Boot.Restoring);

            _loginPanel.gameObject.SetActive(login);
            _selectPanel.gameObject.SetActive(select);
            _worldRoot.gameObject.SetActive(world);

            if (login) _loginStatus.text = Boot.StatusMessage;
            if (select) RefreshCharacterSelect();
            if (world) { RefreshWorld(); UpdateKeyboardLift(); }

            UpdateBackButton();
            _builtPhase = Boot.Phase;
        }

        // ----- status strip ----------------------------------------------------------------------

        private void BuildStatusStrip()
        {
            var strip = UiKit.Box(_root, "StatusStrip", UiKit.Panel);
            var rt = UiKit.Rect(strip.gameObject);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 40f);

            _statusDot = UiKit.Box(strip.transform, "Dot", UiKit.Bad);
            UiKit.Place(UiKit.Rect(_statusDot.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(12f, 0f), new Vector2(14f, 14f));

            _statusText = UiKit.Label(strip.transform, "", 18f, UiKit.Text, TextAlignmentOptions.Left);
            UiKit.Place(UiKit.Rect(_statusText.gameObject), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(34f, 0f), new Vector2(700f, 24f));

            _versionText = UiKit.Label(strip.transform, "v" + GameConstants.GameVersion, 14f,
                                       UiKit.TextDim, TextAlignmentOptions.Right);
            UiKit.Place(UiKit.Rect(_versionText.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                        new Vector2(-12f, 0f), new Vector2(120f, 20f));

            // Shown only while a silent reconnect is in flight. It is deliberately a NOTICE and not a
            // full-screen takeover: the world behind it is still the world you were in, and saying so
            // is better than the client pretending you had logged out.
            _reconnectNotice = UiKit.Box(_root, "Reconnecting", new Color(0.10f, 0.12f, 0.15f, 0.95f));
            UiKit.Place(UiKit.Rect(_reconnectNotice.gameObject), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -52f), new Vector2(380f, 44f));
            var notice = UiKit.Label(_reconnectNotice.transform, "Reconnecting ...", 18f,
                                     new Color(0.95f, 0.80f, 0.35f), TextAlignmentOptions.Center);
            UiKit.Stretch(UiKit.Rect(notice.gameObject), 0f, 0f, 0f, 0f);
            _reconnectNotice.gameObject.SetActive(false);
        }

        private void RefreshStatusStrip()
        {
            // Four states, because yellow used to mean two very different things: "waiting at the
            // character screen" (fine) and "in world and the frames have STOPPED" (the worst state the
            // client can be in). Only the small word STALLED told them apart. The failure gets its own
            // colour now, so the dot carries the alarm instead of the label.
            //
            //   green   in world and a frame arrived in the last 2s — the only fully healthy state
            //   ORANGE  in world, socket up, NO frames: connected to a server that is not talking
            //   yellow  connected but not in world (login / character select)
            //   red     not connected at all (SignalR reports Reconnecting as disconnected)
            bool live = Boot.SecondsSinceFrame >= 0f && Boot.SecondsSinceFrame < 2f;
            bool inWorld = Boot.Phase == ClientPhase.InWorld;
            _statusDot.color = inWorld && live      ? UiKit.Good
                             : inWorld && Boot.IsConnected ? new Color(1.00f, 0.55f, 0.15f)
                             : Boot.IsConnected     ? new Color(0.95f, 0.80f, 0.30f)
                             : UiKit.Bad;

            // Render FPS, smoothed. Deliberately shown NEXT TO the network rate, because the two
            // answer different questions and are constantly confused: "net" is how fast the SERVER is
            // reaching us (10/s is the tick rate, and it floors at 1/s on the heartbeat), "fps" is how
            // fast this phone is drawing. Choppy with a healthy net is the renderer; smooth with a
            // dead net means you are watching a world that stopped.
            if (Time.unscaledDeltaTime > 0f)
                _renderFps = Mathf.Lerp(_renderFps, 1f / Time.unscaledDeltaTime,
                                        1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));

            string text = Boot.Phase.ToString();
            if (Boot.Phase == ClientPhase.InWorld)
            {
                text += "  ·  net " + Boot.FramesPerSecond.ToString("0.0") + "/s"
                      + "  ·  " + Mathf.RoundToInt(_renderFps) + " fps";
                if (Boot.Entities != null) text += "  ·  entities " + Boot.Entities.Count;
                // A connected socket that has gone quiet looks identical to a healthy one unless we
                // say so out loud. This is the line that made the empty-world bug visible.
                if (!live) text += "   STALLED";
            }
            _statusText.text = text;
        }

        // ----- login -----------------------------------------------------------------------------

        private void BuildLogin()
        {
            _loginPanel = UiKit.PanelBox(_root, "LoginPanel");
            UiKit.Place(_loginPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(560f, 400f));

            var inner = _loginPanel.GetChild(0);
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Sign in", 30f).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f),
                        new Vector2(300f, 40f));

            _urlField = UiKit.InputField(inner, "http://127.0.0.1:5238/game");
            UiKit.Place(UiKit.Rect(_urlField.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -70f), new Vector2(512f, 46f));

            _userField = UiKit.InputField(inner, "username");
            UiKit.Place(UiKit.Rect(_userField.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -126f), new Vector2(512f, 46f));

            _passField = UiKit.InputField(inner, "password", password: true);
            UiKit.Place(UiKit.Rect(_passField.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -182f), new Vector2(512f, 46f));

            var login = UiKit.TextButton(inner, "Login", () => Submit(register: false));
            UiKit.Place(UiKit.Rect(login.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -242f), new Vector2(248f, 50f));

            var register = UiKit.TextButton(inner, "Register", () => Submit(register: true));
            UiKit.Place(UiKit.Rect(register.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(288f, -242f), new Vector2(248f, 50f));

            _loginStatus = UiKit.Label(inner, "", 16f, UiKit.TextDim, TextAlignmentOptions.TopLeft);
            UiKit.Place(UiKit.Rect(_loginStatus.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -304f), new Vector2(512f, 70f));

        }

        private void Submit(bool register)
        {
            if (Boot == null) return;
            Boot.ServerUrl = _urlField.text;
            Boot.Username = _userField.text;
            Boot.Password = _passField.text;
            _ = Boot.ConnectAndLogin(_userField.text, _passField.text, register);
        }

        // ----- character select ------------------------------------------------------------------

        private void BuildCharacterSelect()
        {
            _selectPanel = UiKit.PanelBox(_root, "CharacterSelect");
            UiKit.Place(_selectPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(700f, 480f));

            var inner = _selectPanel.GetChild(0);
            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "Characters", 30f).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f),
                        new Vector2(300f, 40f));

            _accountLabel = UiKit.Label(inner, "", 15f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_accountLabel.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(24f, -52f), new Vector2(400f, 22f));

            var logout = UiKit.TextButton(inner, "Logout", () => Boot.Logout(), 16f);
            UiKit.Place(UiKit.Rect(logout.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-24f, -16f), new Vector2(130f, 40f));

            var create = UiKit.TextButton(inner, "Create character", () => ShowCreate(true), 16f);
            UiKit.Place(UiKit.Rect(create.gameObject), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-164f, -16f), new Vector2(180f, 40f));

            ScrollRect scroll;
            _characterList = UiKit.ScrollArea(inner, out scroll, 4f);
            var listRoot = (RectTransform)scroll.transform;
            UiKit.Stretch(listRoot, 24f, 86f, 24f, 60f);

            _selectStatus = UiKit.Label(inner, "", 15f, UiKit.TextDim);
            UiKit.Place(UiKit.Rect(_selectStatus.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(24f, 16f), new Vector2(640f, 36f));

            BuildCreateForm(inner);
        }

        private void BuildCreateForm(Transform parent)
        {
            _createPanel = UiKit.PanelBox(parent, "CreateForm");
            UiKit.Place(_createPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(520f, 300f));
            var inner = _createPanel.GetChild(0);

            UiKit.Place(UiKit.Rect(UiKit.Label(inner, "New character", 24f).gameObject),
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -14f),
                        new Vector2(300f, 34f));

            _nameField = UiKit.InputField(inner, "name");
            UiKit.Place(UiKit.Rect(_nameField.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -60f), new Vector2(478f, 46f));

            _raceButton = UiKit.TextButton(inner, "", () =>
            {
                _raceIndex = (_raceIndex + 1) % Races.Length;
                RefreshCreateForm();
            });
            UiKit.Place(UiKit.Rect(_raceButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(20f, -116f), new Vector2(232f, 46f));

            _classButton = UiKit.TextButton(inner, "", () =>
            {
                _classIndex = (_classIndex + 1) % Classes.Length;
                RefreshCreateForm();
            });
            UiKit.Place(UiKit.Rect(_classButton.gameObject), new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(266f, -116f), new Vector2(232f, 46f));

            var confirm = UiKit.TextButton(inner, "Create", () =>
            {
                _ = Boot.CreateCharacter(_nameField.text, Races[_raceIndex], Classes[_classIndex]);
                ShowCreate(false);
            });
            UiKit.Place(UiKit.Rect(confirm.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(20f, 20f), new Vector2(232f, 50f));

            var cancel = UiKit.TextButton(inner, "Cancel", () => ShowCreate(false));
            UiKit.Place(UiKit.Rect(cancel.gameObject), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(266f, 20f), new Vector2(232f, 50f));

            _createPanel.gameObject.SetActive(false);
            RefreshCreateForm();
        }

        private void ShowCreate(bool show)
        {
            _createPanel.gameObject.SetActive(show);
            if (show) _nameField.text = "";
        }

        private void RefreshCreateForm()
        {
            UiKit.SetButtonText(_raceButton, "Race: " + Races[_raceIndex]);
            UiKit.SetButtonText(_classButton, "Class: " + Classes[_classIndex]);
        }

        private void RefreshCharacterSelect()
        {
            _accountLabel.text = "Account: " + Boot.Username;
            _selectStatus.text = Boot.StatusMessage;

            // Rebuild the rows only when the LIST changed. A character row carries a button with a
            // captured id, so rebuilding every frame would also re-register listeners every frame.
            int revision = Boot.Characters != null ? Boot.Characters.Length : 0;
            revision = revision * 31 + (_builtPhase == Boot.Phase ? 0 : 1);
            if (revision == _characterRevision) return;
            _characterRevision = revision;

            for (int i = _characterList.childCount - 1; i >= 0; i--)
                Destroy(_characterList.GetChild(i).gameObject);

            if (Boot.Characters == null || Boot.Characters.Length == 0)
            {
                var empty = UiKit.Label(_characterList, "No characters on this account yet.", 17f, UiKit.TextDim);
                empty.gameObject.AddComponent<LayoutElement>().minHeight = 40f;
                return;
            }

            foreach (var slot in Boot.Characters)
            {
                var row = UiKit.Box(_characterList, "Character", UiKit.PanelLight);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 56f;

                var label = UiKit.Label(row.transform,
                    slot.Name + "    Lv " + slot.Level + "    " + slot.Race + " " + slot.BaseClass,
                    18f, UiKit.Text, TextAlignmentOptions.Left);
                UiKit.Stretch(UiKit.Rect(label.gameObject), 14f, 0f, 150f, 0f);

                int id = slot.Id;   // captured per row — the loop variable would be shared
                var enter = UiKit.TextButton(row.transform, "Enter", () => { _ = Boot.EnterWorld(id); }, 17f);
                UiKit.Place(UiKit.Rect(enter.gameObject), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(-10f, 0f), new Vector2(120f, 42f));
            }
        }
    }
}
