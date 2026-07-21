using System;
using System.Threading.Tasks;
using Game.Shared;
using UnityEngine;

namespace Game.Client
{
    /// <summary>What the client is currently doing. The HUD renders one screen per phase, so
    /// "is it connected / did login work / am I in the world" is always answerable ON the device.</summary>
    public enum ClientPhase
    {
        Offline,        // nothing attempted yet, or we dropped
        Connecting,     // SignalR handshake in flight
        Authenticating, // login/register in flight
        CharacterSelect,// authenticated, choosing a character
        Entering,       // EnterWorld in flight
        InWorld,
    }

    /// <summary>
    /// The client's orchestrator and single source of truth for connection state: connect → login →
    /// pick a character → enter the world → stream deltas into the EntityManager.
    ///
    /// Everything the HUD needs to answer "is this thing actually working?" is a public field here.
    /// The scene only needs THIS component — the EntityManager, CameraRig, TouchInput and HUD are
    /// created at runtime if they aren't wired in the Inspector, so a fresh scene can't be
    /// half-wired into a silent black screen.
    /// </summary>
    public class GameBoot : MonoBehaviour
    {
        [Header("Server")]
        [Tooltip("Emulator: http://10.0.2.2:5238/game   Cabled phone (adb reverse): http://127.0.0.1:5238/game   Same Wi-Fi: http://<PC-LAN-IP>:5238/game")]
        public string ServerUrl = "http://127.0.0.1:5238/game";

        [Header("Dev login (prefilled into the login screen)")]
        public string Username = "phonedev";
        public string Password = "phonedev1";
        public string CharacterName = "Pathfinder";
        public Race Race = Race.Human;
        public BaseClass BaseClass = BaseClass.Fighter;

        [Tooltip("Skip the login screen and use the credentials above (handy in the Editor).")]
        public bool AutoLogin = false;

        [Header("Scene refs (auto-created when empty)")]
        public EntityManager Entities;
        public CameraRig CameraRig;

        // ----- State the HUD reads -------------------------------------------------------------
        public ClientPhase Phase { get; private set; } = ClientPhase.Offline;
        public string StatusMessage { get; private set; } = "Not connected";
        public string LastError { get; set; }
        public CharacterSlot[] Characters { get; private set; } = Array.Empty<CharacterSlot>();
        public Guid SelfId => _selfId;
        public Guid? TargetId { get; set; }

        /// <summary>Server frames received, and when the last one landed — the honest answer to
        /// "is the connection alive?", which a connected-but-silent socket would otherwise fake.</summary>
        public int FramesReceived { get; private set; }
        public float LastFrameTime { get; private set; }
        public float FramesPerSecond { get; private set; }
        public float SecondsSinceFrame => FramesReceived == 0 ? -1f : Time.realtimeSinceStartup - LastFrameTime;

        public StatsUpdate Stats { get; private set; }
        public ProgressUpdate Progress { get; private set; }
        public long Gold { get; private set; }

        /// <summary>Staff role of the character in world — mirrors the WPF client, which only bothers
        /// sending admin commands when it believes it's allowed (the server re-checks regardless).</summary>
        public AccountRole Role { get; private set; } = AccountRole.Player;
        public bool IsAdmin => Role == AccountRole.Admin || Role == AccountRole.Moderator;

        private NetworkChannel _net;
        private Guid _selfId;

        // Remembered so a silent reconnect can restore the session (see NetworkChannel.Reconnected).
        private string _authUser, _authPass;
        private int _lastCharacterId = -1;
        private float _fpsWindowStart;
        private int _fpsWindowCount;
        private float _enteredAt;
        private float _lastResync = -99f;
        private bool _busy;

        public bool IsBusy => _busy;
        public bool IsConnected => _net != null && _net.IsConnected;

        private const string PrefUrl = "l2clone.serverUrl";
        private const string PrefUser = "l2clone.username";

        private void Awake()
        {
            ClientLog.Hook();
            _ = UnityMainThreadDispatcher.Instance;

            // Typing an IP and a username on a phone keyboard every launch is its own punishment.
            ServerUrl = PlayerPrefs.GetString(PrefUrl, ServerUrl);
            Username = PlayerPrefs.GetString(PrefUser, Username);

            EnsureSceneRefs();
        }

        private void EnsureSceneRefs()
        {
            if (Entities == null)
            {
                Entities = FindAnyObjectByType<EntityManager>();
                if (Entities == null)
                    Entities = new GameObject("Entities").AddComponent<EntityManager>();
            }
            Entities.MissingEntity += RequestResync;

            if (CameraRig == null)
            {
                CameraRig = FindAnyObjectByType<CameraRig>();
                if (CameraRig == null && Camera.main != null)
                    CameraRig = Camera.main.gameObject.AddComponent<CameraRig>();
            }

            var input = FindAnyObjectByType<TouchInput>();
            if (input == null) input = gameObject.AddComponent<TouchInput>();
            input.Boot = this;

            var hud = FindAnyObjectByType<GameHud>();
            if (hud == null) hud = gameObject.AddComponent<GameHud>();
            hud.Boot = this;

            if (FindAnyObjectByType<GroundGrid>() == null)
                new GameObject("GroundGrid").AddComponent<GroundGrid>();
        }

        private async void Start()
        {
            ClientLog.Info("Client v" + GameConstants.GameVersion + " ready. Server: " + ServerUrl);
            if (AutoLogin) await ConnectAndLogin(Username, Password, register: false);
        }

        private void Update()
        {
            // Frames/sec over a rolling second: 10/s means a healthy server tick reaching us.
            if (Time.realtimeSinceStartup - _fpsWindowStart >= 1f)
            {
                FramesPerSecond = _fpsWindowCount / Mathf.Max(0.0001f, Time.realtimeSinceStartup - _fpsWindowStart);
                _fpsWindowStart = Time.realtimeSinceStartup;
                _fpsWindowCount = 0;
            }

            // Watchdog for the one failure the delta feed cannot recover from on its own: frames are
            // arriving, we are in the world, and YOUR OWN entity isn't among them. A standing player is
            // byte-identical every tick, so the server never re-sends it and the HUD would sit on
            // "waiting for your entity …" indefinitely. Ask for a resync instead of waiting forever.
            if (Phase == ClientPhase.InWorld && FramesReceived > 0 && Entities != null
                && !Entities.States.ContainsKey(_selfId)
                && Time.realtimeSinceStartup - _enteredAt > 2f)
                RequestResync();
        }

        /// <summary>Tell the server to forget its per-connection diff state so the next frame re-sends
        /// every visible entity in full. Throttled: the conditions that trigger it persist for a few
        /// frames, and one request per tick would be a stampede.</summary>
        public async void RequestResync()
        {
            if (Phase != ClientPhase.InWorld || _net == null || !_net.IsConnected) return;
            if (Time.realtimeSinceStartup - _lastResync < 3f) return;
            _lastResync = Time.realtimeSinceStartup;
            try
            {
                ClientLog.Warn("World state out of sync — asking the server to re-send it.");
                await _net.RequestResyncAsync();
            }
            catch (Exception ex) { ClientLog.Warn("Resync: " + ex.Message); }
        }

        // ----- Flow ----------------------------------------------------------------------------

        public async Task ConnectAndLogin(string username, string password, bool register)
        {
            if (_busy) return;
            _busy = true;
            LastError = null;
            try
            {
                if (!IsConnected)
                {
                    Phase = ClientPhase.Connecting;
                    StatusMessage = "Connecting to " + ServerUrl + " …";
                    ClientLog.Info(StatusMessage);
                    await Connect();
                    ClientLog.Good("Connected.");
                }

                Phase = ClientPhase.Authenticating;
                StatusMessage = register ? "Registering …" : "Logging in …";

                var auth = register
                    ? await _net.RegisterAsync(username, password)
                    : await _net.LoginAsync(username, password);

                if (!auth.Success)
                {
                    Fail(auth.Error ?? (register ? "Registration failed." : "Login failed."));
                    return;
                }

                Username = username;
                _authUser = username;
                _authPass = password;
                PlayerPrefs.SetString(PrefUrl, ServerUrl);
                PlayerPrefs.SetString(PrefUser, username);
                PlayerPrefs.Save();

                ClientLog.Good((register ? "Registered" : "Logged in") + " as " + username + ".");
                await RefreshCharacters();
            }
            catch (Exception ex)
            {
                Fail(Describe(ex));
            }
            finally { _busy = false; }
        }

        private async Task Connect()
        {
            _net = new NetworkChannel();
            _net.SnapshotDeltaReceived += OnDelta;
            _net.SnapshotReceived += OnFullSnapshot;
            _net.StatsReceived += s => Main(() => Stats = s);
            _net.ProgressReceived += p => Main(() =>
            {
                Progress = p;
                if (p.LeveledUp) ClientLog.Good("Level up! Now level " + p.Level + ".");
            });
            _net.GoldReceived += g => Main(() => Gold = g.Gold);
            _net.ChatReceived += m => Main(() => ClientLog.Info(m.From + ": " + m.Text));
            _net.CombatReceived += OnCombat;
            _net.Disconnected += m => Main(() =>
            {
                Phase = ClientPhase.Offline;
                StatusMessage = "Disconnected: " + m;
                ClientLog.Error(StatusMessage);
                if (Entities != null) Entities.Clear();
            });
            _net.ForceDisconnected += m => Main(() => ClientLog.Error("Kicked by server: " + m));
            _net.Reconnecting += () => Main(() =>
            {
                StatusMessage = "Connection dropped — reconnecting …";
                ClientLog.Warn(StatusMessage);
            });
            _net.Reconnected += () => Main(() => { _ = RestoreSession(); });

            await _net.ConnectAsync(ServerUrl);
        }

        /// <summary>After a transport reconnect the new connection has no server session, so silently
        /// re-login and re-enter the character we were on. This is what stops a phone-link blip from
        /// dumping you to an empty, "not logged in" character screen.</summary>
        private async Task RestoreSession()
        {
            if (string.IsNullOrEmpty(_authUser)) return;
            try
            {
                ClientLog.Info("Reconnected — restoring session …");
                var auth = await _net.LoginAsync(_authUser, _authPass);
                if (!auth.Success) { Fail("Re-login failed: " + auth.Error); return; }

                if (_lastCharacterId >= 0 && Phase == ClientPhase.InWorld)
                    await EnterWorld(_lastCharacterId);
                else
                    await RefreshCharacters();
                ClientLog.Good("Session restored.");
            }
            catch (Exception ex) { Fail("Restore failed: " + Describe(ex)); }
        }

        public async Task RefreshCharacters()
        {
            try
            {
                var list = await _net.ListCharactersAsync();
                Characters = list.Characters ?? Array.Empty<CharacterSlot>();
                Phase = ClientPhase.CharacterSelect;
                StatusMessage = Characters.Length + " character(s) on this account.";
                ClientLog.Info(StatusMessage);
            }
            catch (Exception ex) { Fail(Describe(ex)); }
        }

        public async Task CreateCharacter(string name, Race race, BaseClass baseClass)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                var err = await _net.CreateCharacterAsync(name, race, baseClass);
                if (!string.IsNullOrEmpty(err)) { Fail("Create failed: " + err); return; }
                ClientLog.Good("Created " + name + ".");
                await RefreshCharacters();
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        public async Task EnterWorld(int characterId)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Phase = ClientPhase.Entering;
                StatusMessage = "Entering world …";

                // Wipe the old world BEFORE the request goes out, not after it returns. The server starts
                // streaming the moment the character is in the world, and those frames can land while we
                // are still awaiting the reply — clearing afterwards threw away the one full spawn of
                // your own entity, which is then never re-sent because a standing player never changes.
                // That is the "waiting for your entity …" bug: mobs trickled back in as they wandered,
                // you never did.
                if (Entities != null) { Entities.Clear(); Entities.SetSelf(Guid.Empty); }
                if (CameraRig != null) CameraRig.Target = null;   // re-acquire on the next frame

                var result = await _net.EnterWorldAsync(characterId);
                if (!result.Success) { Fail("Enter failed: " + result.Error); return; }

                _selfId = result.EntityId;
                _lastCharacterId = characterId;
                Role = result.Role;
                Main(() =>
                {
                    if (Entities != null) Entities.SetSelf(_selfId);   // re-tints anything already spawned
                    _enteredAt = Time.realtimeSinceStartup;
                    Phase = ClientPhase.InWorld;
                    StatusMessage = "In world";
                    ClientLog.Good("In world at (" + Mathf.RoundToInt(result.X) + ", " + Mathf.RoundToInt(result.Y) + ")."
                                   + (IsAdmin ? "  [" + Role + "]" : ""));
                });
            }
            catch (Exception ex) { Fail(Describe(ex)); }
            finally { _busy = false; }
        }

        public async void LeaveWorld()
        {
            try
            {
                await _net.LeaveWorldAsync();
                Main(() =>
                {
                    if (Entities != null) Entities.Clear();
                    TargetId = null;
                    Phase = ClientPhase.CharacterSelect;
                    StatusMessage = "Left the world.";
                });
                await RefreshCharacters();
            }
            catch (Exception ex) { ClientLog.Warn("Leave: " + ex.Message); }
        }

        /// <summary>Sign out of the account and go back to the login screen. The connection itself is
        /// dropped, because the server's session (which account, which character) is keyed by the
        /// CONNECTION — keeping the socket open would leave us signed in on the server while the client
        /// showed a login form. The stored credentials are cleared too, or the reconnect handler would
        /// helpfully log us straight back in.</summary>
        public async void Logout()
        {
            _authUser = _authPass = null;
            _lastCharacterId = -1;
            Role = AccountRole.Player;

            try { if (_net != null && _net.IsConnected && Phase == ClientPhase.InWorld) await _net.LeaveWorldAsync(); }
            catch (Exception ex) { ClientLog.Warn("Logout (leave): " + ex.Message); }

            try { if (_net != null) await _net.DisposeAsync(); }
            catch (Exception ex) { ClientLog.Warn("Logout (close): " + ex.Message); }

            _net = null;
            Main(() =>
            {
                if (Entities != null) { Entities.Clear(); Entities.SetSelf(Guid.Empty); }
                if (CameraRig != null) CameraRig.Target = null;
                TargetId = null;
                Stats = null; Progress = null; Gold = 0;
                Characters = Array.Empty<CharacterSlot>();
                LastError = null;
                Phase = ClientPhase.Offline;
                StatusMessage = "Signed out.";
                ClientLog.Info(StatusMessage);
            });
        }

        // ----- Server events -------------------------------------------------------------------

        private void OnDelta(SnapshotDelta delta)
        {
            Main(() =>
            {
                CountFrame();
                if (Entities == null) return;
                Entities.ApplyDelta(delta);
                AcquireCamera();
                // A despawned target is no longer a target — otherwise the HUD shows a ghost.
                if (TargetId.HasValue && !Entities.States.ContainsKey(TargetId.Value)) TargetId = null;
            });
        }

        private void OnFullSnapshot(WorldSnapshot snapshot)
        {
            Main(() =>
            {
                CountFrame();
                if (Entities == null) return;
                Entities.ApplySnapshot(snapshot.Entities);
                AcquireCamera();
            });
        }

        private void OnCombat(CombatEvent e)
        {
            Main(() =>
            {
                if (e.AttackerId != _selfId && e.TargetId != _selfId) return;
                string verb = e.AttackerId == _selfId ? "You → " + e.TargetName : e.AttackerName + " → you";
                ClientLog.Info(verb + "  " + e.Outcome + (e.Damage != 0 ? " " + e.Damage : "")
                               + (string.IsNullOrEmpty(e.Skill) ? "" : " (" + e.Skill + ")"));
            });
        }

        private void CountFrame()
        {
            FramesReceived++;
            _fpsWindowCount++;
            LastFrameTime = Time.realtimeSinceStartup;
        }

        private void AcquireCamera()
        {
            if (CameraRig == null || CameraRig.Target != null) return;
            var self = Entities.Find(_selfId);
            if (self != null) CameraRig.Target = self.transform;
        }

        // ----- Commands ------------------------------------------------------------------------

        public async void Move(float serverX, float serverY)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.MoveAsync(serverX, serverY); }
            catch (Exception ex) { ClientLog.Warn("Move: " + ex.Message); }
        }

        public async void Attack(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            TargetId = targetId;
            try { await _net.AttackAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Attack: " + ex.Message); }
        }

        public async void SetMoveState(MoveState state)
        {
            try { await _net.SetMoveStateAsync(state); }
            catch (Exception ex) { ClientLog.Warn("MoveState: " + ex.Message); }
        }

        public async void Respawn()
        {
            try { await _net.RespawnAsync(); }
            catch (Exception ex) { ClientLog.Warn("Respawn: " + ex.Message); }
        }

        /// <summary>The one entry point for the chat/command box. Mirrors the WPF client's routing so
        /// slash commands actually DO something instead of being broadcast as chat text:
        ///   !text            → world chat
        ///   /w Name message  → whisper
        ///   /fadd|/frem Name, /flist → friends (any player)
        ///   /anything else   → admin command (only if this character has a staff role; the server
        ///                      re-checks). A non-admin slash is reported locally, not sent as chat.
        ///   plain text       → local chat</summary>
        public async void Say(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            raw = raw.Trim();
            try
            {
                if (raw.StartsWith("!"))
                {
                    var body = raw.Substring(1).Trim();
                    if (body.Length > 0) await _net.ChatAsync(body, ChatChannel.World);
                    return;
                }

                if (raw.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = raw.Substring(3).Trim();
                    int sp = rest.IndexOf(' ');
                    if (sp <= 0) { ClientLog.Warn("Usage: /w <name> <message>"); return; }
                    await _net.ChatAsync(rest.Substring(sp + 1).Trim(), ChatChannel.Whisper, rest.Substring(0, sp));
                    return;
                }

                if (raw.StartsWith("/fadd ", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("add", raw.Substring(6).Trim()); return; }
                if (raw.StartsWith("/frem ", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("remove", raw.Substring(6).Trim()); return; }
                if (raw.Equals("/flist", StringComparison.OrdinalIgnoreCase))
                { await _net.FriendCommandAsync("list", ""); return; }

                if (raw.StartsWith("/"))
                {
                    if (!IsAdmin) { ClientLog.Warn("Unknown command: " + raw); return; }
                    var body = raw.Substring(1).Trim();
                    int sp = body.IndexOf(' ');
                    string cmd = sp < 0 ? body : body.Substring(0, sp);
                    string arg = sp < 0 ? "" : body.Substring(sp + 1).Trim();
                    await _net.AdminCommandAsync(cmd, arg);
                    return;
                }

                await _net.ChatAsync(raw, ChatChannel.Local);
            }
            catch (Exception ex) { ClientLog.Warn("Chat: " + ex.Message); }
        }

        // ----- Helpers -------------------------------------------------------------------------

        private void Fail(string message)
        {
            LastError = message;
            StatusMessage = message;
            ClientLog.Error(message);
            if (Phase == ClientPhase.Connecting || Phase == ClientPhase.Authenticating)
                Phase = IsConnected ? ClientPhase.Authenticating : ClientPhase.Offline;
            else if (Phase == ClientPhase.Entering)
                Phase = ClientPhase.CharacterSelect;
        }

        /// <summary>Turn transport exceptions into something a human staring at a phone can act on.
        /// "No connection could be made" on a device almost always means the URL points at the
        /// phone's own localhost without an `adb reverse`, or at a PC firewall.</summary>
        private string Describe(Exception ex)
        {
            var inner = ex;
            while (inner.InnerException != null) inner = inner.InnerException;
            string msg = inner.Message;

            if (msg.Contains("refused") || msg.Contains("No connection") || msg.Contains("unreachable"))
                msg += "  — is the server running, and is the URL reachable FROM THE PHONE? "
                     + "(cable: adb reverse tcp:5238 tcp:5238 then use 127.0.0.1; Wi-Fi: use the PC's LAN IP)";
            return msg;
        }

        private static void Main(Action a) => UnityMainThreadDispatcher.Instance.Enqueue(a);

        private async void OnDestroy()
        {
            if (_net != null) await _net.DisposeAsync();
        }
    }
}
