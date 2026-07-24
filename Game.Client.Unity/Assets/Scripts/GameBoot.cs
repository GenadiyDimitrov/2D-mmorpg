using System;
using System.Collections.Generic;
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

        // admin/admin is the DEBUG seed account — a level-90 character in full gear with every skill,
        // which is what you want on the phone when the point is to test a window rather than to level
        // something up. It only exists in a DEBUG server build.
        [Header("Dev login (prefilled into the login screen)")]
        public string Username = "admin";
        public string Password = "admin";
        public string CharacterName = "Pathfinder";
        public Race Race = Race.Human;
        public BaseClass BaseClass = BaseClass.Fighter;

        [Tooltip("Skip the login screen and use the credentials above (handy in the Editor).")]
        public bool AutoLogin = false;

        [Header("Scene refs (auto-created when empty)")]
        public EntityManager Entities;
        public CameraRig CameraRig;
        public MoveMarker Marker;
        public ZoneOverlay Zones;

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

        /// <summary>
        /// The skill bar, exactly as the SERVER sent it — 60 slots of skill id / "action:…" / "item:…"
        /// / null. **This client never authors a bar.** The server owns it and does the placement; a
        /// client that wrote back a bar it had not been told to write is what destroyed real layouts
        /// in the WPF client twice (a Learned push arriving while the client held a different bar).
        /// So: render this, and only send SetSkillBar when the PLAYER edits a slot.
        /// </summary>
        public string[] SkillBar { get; private set; } = new string[GameConstants.SkillBarSlots];

        /// <summary>Learned skill id → level, for greying out what isn't castable.</summary>
        public readonly Dictionary<string, int> Learned = new Dictionary<string, int>();

        /// <summary>The bag, as last sent by the server (it pushes the whole thing on any change).</summary>
        public InventoryItemDto[] Inventory { get; private set; } = new InventoryItemDto[0];

        /// <summary>Party roster (empty when you are not in one) and the agreed loot rule.</summary>
        public PartyMemberDto[] Party { get; private set; } = new PartyMemberDto[0];
        public LootMode PartyLoot { get; private set; } = LootMode.Random;

        /// <summary>A pending party invitation, or null.</summary>
        public PartyInviteDto PendingInvite { get; private set; }

        public async void PartyInvite(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.PartyInviteAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Invite: " + ex.Message); }
        }

        /// <summary>Open a box/chest from the inventory (random grants loot; a selection box replies with
        /// a "Selection" push that the UI turns into a chooser).</summary>
        public async void OpenBox(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.OpenBoxAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Open: " + ex.Message); }
        }

        /// <summary>Confirm the picked item(s) from a selection box.</summary>
        public async void SelectBoxItems(Guid instanceId, string[] itemIds)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SelectBoxItemsAsync(instanceId, itemIds); }
            catch (Exception ex) { ClientLog.Warn("Select: " + ex.Message); }
        }

        /// <summary>Walk after a player until you move or they leave (null stops following).</summary>
        public async void Follow(Guid? targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.FollowAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Follow: " + ex.Message); }
        }

        /// <summary>Attack whatever the targeted player is attacking.</summary>
        public async void Assist(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.AssistAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Assist: " + ex.Message); }
        }

        public async void AnswerPartyInvite(bool accept)
        {
            PendingInvite = null;
            try { await _net.PartyRespondAsync(accept); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyLeave()
        {
            try { await _net.PartyLeaveAsync(); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyKick(Guid targetId)
        {
            try { await _net.PartyKickAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        public async void PartyChangeLeader(Guid targetId)
        {
            try { await _net.PartyChangeLeaderAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        /// <summary>Save the currently-worn gear into preset slot 0/1/2 (A/B/C).</summary>
        public async void SaveEquipPreset(int slot)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.SaveEquipPresetAsync(slot); }
            catch (Exception ex) { ClientLog.Warn("Preset: " + ex.Message); }
        }

        /// <summary>Re-equip preset slot 0/1/2. Server refuses in combat + reports skipped items.</summary>
        public async void ApplyEquipPreset(int slot)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.ApplyEquipPresetAsync(slot); }
            catch (Exception ex) { ClientLog.Warn("Preset: " + ex.Message); }
        }

        /// <summary>Fetch a leaderboard board and hand the result back on the main thread.</summary>
        public async void RequestLeaderboard(string category, Action<LeaderboardDto> onResult)
        {
            if (Phase != ClientPhase.InWorld || _net == null) return;
            try
            {
                var dto = await _net.RequestLeaderboardAsync(category);
                if (dto != null) Main(() => onResult(dto));
            }
            catch (Exception ex) { ClientLog.Warn("Rank: " + ex.Message); }
        }

        /// <summary>Find a current party member's id by name (for /ptkick, /ptcl). Null if not in party.</summary>
        public Guid? PartyMemberId(string name)
        {
            if (Party == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var m in Party)
                if (m.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) return m.Id;
            return null;
        }

        /// <summary>The instance id of an unequipped bag item with this def id (any stack), or null —
        /// for the quick-use bar, where a slot holds "item:&lt;defId&gt;" and any matching stack works.</summary>
        public Guid? FindBagItem(string defId)
        {
            if (Inventory == null || string.IsNullOrEmpty(defId)) return null;
            foreach (var it in Inventory)
                if (!it.Equipped && it.DefId == defId) return it.InstanceId;
            return null;
        }

        /// <summary>Send a friend command. The hub takes a NAME rather than an id on purpose — friendship
        /// has to work on someone who is offline or out of view, which no entity id can express.</summary>
        public async void FriendCommand(string action, string name)
        {
            try { await _net.FriendCommandAsync(action, name); }
            catch (Exception ex) { ClientLog.Warn("Friend: " + ex.Message); }
        }

        /// <summary>The NAME of the currently targeted player, or null when the target is missing, is a
        /// mob, or is yourself. Used by the name-only actions, which take a target instead of typing.</summary>
        public string TargetPlayerName()
        {
            if (!TargetId.HasValue || Entities == null) return null;
            if (TargetId.Value == SelfId) return null;
            return Entities.TryGetState(TargetId.Value, out var e)
                   && e.Kind == EntityKind.Player ? e.Name : null;
        }

        /// <summary>A nearby PLAYER entity by name (for /ptinv). Null if not in view.</summary>
        public Guid? FindPlayerByName(string name)
        {
            if (Entities == null || string.IsNullOrWhiteSpace(name)) return null;
            foreach (var kv in Entities.States)
                if (kv.Value.Kind == EntityKind.Player &&
                    kv.Value.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return kv.Key;
            return null;
        }

        /// <summary>Select the nearest living enemy (mob) within range; pressing again steps to the
        /// next-nearest, so you can flick between the ones in front of you.</summary>
        public void TargetClosest()
        {
            const float maxRange = 2500f, maxRangeSq = maxRange * maxRange;
            if (Entities == null || !Entities.TryGetState(SelfId, out var self)) return;

            var enemies = new List<(Guid Id, float DistSq)>();
            foreach (var kv in Entities.States)
            {
                var e = kv.Value;
                if (e.Kind != EntityKind.Mob || e.Dead) continue;
                float dx = e.X - self.X, dy = e.Y - self.Y;
                float d2 = dx * dx + dy * dy;
                if (d2 <= maxRangeSq) enemies.Add((kv.Key, d2));
            }
            if (enemies.Count == 0) { ClientLog.Warn("No enemy in range."); return; }

            enemies.Sort((a, b) => a.DistSq.CompareTo(b.DistSq));
            int idx = TargetId.HasValue ? enemies.FindIndex(x => x.Id == TargetId.Value) : -1;
            TargetId = enemies[(idx + 1) % enemies.Count].Id;   // -1 → nearest; else the next one out
        }

        public async void PartySetLoot(LootMode mode)
        {
            try { await _net.PartySetLootModeAsync(mode); }
            catch (Exception ex) { ClientLog.Warn("Party: " + ex.Message); }
        }

        /// <summary>The open NPC conversation, or null. Everything an NPC offers — quests, class
        /// change, shop, teleport, buffs, skill reset — arrives in this ONE push.</summary>
        public NpcDialog Dialog { get; private set; }

        /// <summary>The NPC being talked to. Every dialog action needs it, because the server checks
        /// you are still standing in front of that specific NPC.</summary>
        public Guid DialogNpcId { get; private set; }

        public async void TalkToNpc(Guid npcEntityId)
        {
            if (Phase != ClientPhase.InWorld) return;
            DialogNpcId = npcEntityId;
            try { await _net.TalkToNpcAsync(npcEntityId); }
            catch (Exception ex) { ClientLog.Warn("Talk: " + ex.Message); }
        }

        public void CloseDialog() { Dialog = null; DialogNpcId = Guid.Empty; }

        public async void QuestAction(string action, string id)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.QuestActionAsync(action, id, DialogNpcId); }
            catch (Exception ex) { ClientLog.Warn("Quest: " + ex.Message); }
        }

        public async void BufferAction(string action, string skillId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BufferActionAsync(DialogNpcId, action, skillId ?? ""); }
            catch (Exception ex) { ClientLog.Warn("Buffer: " + ex.Message); }
        }

        public async void BuyItem(string defId, int quantity)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.BuyItemAsync(DialogNpcId, defId, quantity); }
            catch (Exception ex) { ClientLog.Warn("Buy: " + ex.Message); }
        }

        public async void SellItem(Guid instanceId, int quantity)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.SellItemAsync(DialogNpcId, instanceId, quantity); }
            catch (Exception ex) { ClientLog.Warn("Sell: " + ex.Message); }
        }

        public async void TeleportTo(string zoneId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.TeleportAsync(DialogNpcId, zoneId); }
            catch (Exception ex) { ClientLog.Warn("Teleport: " + ex.Message); }
        }

        public async void ForgetSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld || DialogNpcId == Guid.Empty) return;
            try { await _net.ForgetSkillAsync(DialogNpcId, skillId); }
            catch (Exception ex) { ClientLog.Warn("Forget: " + ex.Message); }
        }

        /// <summary>The quest log, as last pushed by the server.</summary>
        public QuestLog Quests { get; private set; }

        /// <summary>The expanded target window's contents, or null. Arrives only after asking.</summary>
        public TargetDetails Details { get; private set; }

        /// <summary>A pending resurrect offer, or null.</summary>
        public ResurrectOffer PendingResurrect { get; private set; }

        public async void InspectTarget(Guid targetId, bool withDrops)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.InspectTargetAsync(targetId, withDrops); }
            catch (Exception ex) { ClientLog.Warn("Inspect: " + ex.Message); }
        }

        public async void AnswerResurrect(bool accept)
        {
            PendingResurrect = null;   // clear locally; the server sends no "offer withdrawn" push
            try { await _net.ResurrectResponseAsync(accept); }
            catch (Exception ex) { ClientLog.Warn("Resurrect: " + ex.Message); }
        }

        /// <summary>Active buffs and debuffs. The server pushes these once a second while any are
        /// running, and once more when the last one drops.</summary>
        public BuffDto[] Buffs { get; private set; } = new BuffDto[0];

        /// <summary>Cancel a buff you no longer want (a movement-speed buff before a stealth approach,
        /// a mistaken toggle). Debuffs are NOT cancellable — that would defeat the point of them.</summary>
        public async void RemoveBuff(string buffKey)
        {
            if (Phase != ClientPhase.InWorld || string.IsNullOrEmpty(buffKey)) return;
            try { await _net.RemoveBuffAsync(buffKey); }
            catch (Exception ex) { ClientLog.Warn("RemoveBuff: " + ex.Message); }
        }

        /// <summary>Unspent skill points, from the stats push.</summary>
        public int SkillPoints => Stats != null ? Stats.SkillPoints : 0;

        /// <summary>The class currently being PLAYED — race, base/second/third class and its own level.
        /// A subclass swap replaces it, which is why the skills window reads this rather than anything
        /// remembered from the character screen.</summary>
        public SubclassDto ActiveClass { get; private set; }

        /// <summary>Every class this character owns (server-pushed). Drives the debug Class tab.</summary>
        public SubclassDto[] Subclasses { get; private set; } = System.Array.Empty<SubclassDto>();

        /// <summary>The UI, so a server push can refresh a panel that is currently showing stale data.</summary>
        public GameUi Ui { get; private set; }

        /// <summary>The server's last-reported tuning values (rates/karma/caps). Null until requested.</summary>
        public DebugConfigDto DebugConfig { get; private set; }

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

            // 🔴 UNITY CAPS ANDROID AT 30 FPS BY DEFAULT. Nothing in this project ever set
            // targetFrameRate, so the client had been running at 30 the whole time — and 30fps is
            // visible as judder on a phone whose panel does 60+, no matter how good the network is.
            // Every "the movement is choppy" report had this underneath it.
            //
            // -1 would mean "as fast as the platform allows", which on mobile means cooking the
            // battery for frames nobody asked for. 60 is the honest target for a game this simple.
            Application.targetFrameRate = 60;

            // KEEP THE SCREEN ON. An MMO is watched as much as it is touched — you stand still while
            // regenerating, while auto-hunting, while reading a drop list — and the phone reads all of
            // that as "idle" and dims out. Tapping every ten seconds to stop the screen sleeping is
            // not gameplay. Unity restores the system setting when the app loses focus, so this only
            // applies while the game is in front.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

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

            var ui = FindAnyObjectByType<GameUi>();
            if (ui == null) ui = gameObject.AddComponent<GameUi>();
            ui.Boot = this;
            Ui = ui;   // so server pushes can refresh a panel that is showing stale data

            if (FindAnyObjectByType<GroundGrid>() == null)
                new GameObject("GroundGrid").AddComponent<GroundGrid>();

            // The scene's default 10×10 plane is far smaller than the world and almost the same grey
            // as the entity markers. WorldGround fixes both, on whatever object is already there.
            if (FindAnyObjectByType<WorldGround>() == null)
            {
                var ground = GameObject.Find("Ground");
                if (ground != null) ground.AddComponent<WorldGround>();
            }

            // HELD, not looked up on demand: FindAnyObjectByType skips INACTIVE objects, so once the
            // overlay was switched off the next lookup returned null and it could never be switched
            // back on. A toggle that only works in one direction is worse than no toggle.
            if (Zones == null)
            {
                Zones = FindAnyObjectByType<ZoneOverlay>();
                if (Zones == null) Zones = new GameObject("ZoneOverlay").AddComponent<ZoneOverlay>();
            }

            if (Marker == null)
            {
                Marker = FindAnyObjectByType<MoveMarker>();
                if (Marker == null) Marker = new GameObject("MoveMarker").AddComponent<MoveMarker>();
            }
        }

        private async void Start()
        {
            ClientLog.Info("Client v" + GameConstants.GameVersion + " ready. Server: " + ServerUrl);
            if (AutoLogin) await ConnectAndLogin(Username, Password, register: false);
        }

        private void Update()
        {
            // The destination ring lives exactly as long as the walk it describes. Prediction ends when
            // you arrive, when something cancels the walk, or when the server turns out to be taking you
            // somewhere else entirely (a skill on a far target makes it close on the ENEMY instead) —
            // and in every one of those cases a ring still sitting on the ground is a promise the game
            // is no longer keeping.
            if (Phase == ClientPhase.InWorld && Marker != null && Marker.IsShown
                && Entities != null && !Entities.SelfIsPredicting)
                Marker.Hide();

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

                    // REMEMBER THE URL AS SOON AS IT CONNECTS, not after a successful login.
                    //
                    // It used to be saved only in the login-success branch below, which meant a URL
                    // that reached the server but failed to authenticate — wrong password, an account
                    // that does not exist yet, a version refusal — was forgotten, and the address had
                    // to be typed again on a PHONE KEYBOARD next launch. That is exactly the situation
                    // when moving between networks, which is the only time the address changes at all.
                    //
                    // Connecting is the right test: it proves the address is reachable, and that is
                    // the only thing the address field is responsible for being right about.
                    PlayerPrefs.SetString(PrefUrl, ServerUrl);
                    PlayerPrefs.Save();
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
            _net.BuffsReceived += b => Main(() => Buffs = b?.Buffs ?? new BuffDto[0]);
            _net.TargetDetailsReceived += d => Main(() => Details = d);
            _net.PvpStateReceived += p => Main(() =>
            {
                if (p == null) return;
                PvpEnabled = p.Pvp;          // authoritative — the server may have refused the toggle
                Karma = p.Karma;
                PkCount = p.PkCount;
                PvpCount = p.PvpCount;
            });
            _net.QuestLogReceived += q => Main(() => Quests = q);
            _net.DialogReceived += d => Main(() => Dialog = d);
            _net.AutoConfigReceived += c => Main(() =>
            {
                if (c == null) return;
                AutoConfig = c;                 // the authoritative, already-clamped config
                AutoHunting = c.Enabled;
                // Sync the per-slot Auto marks to the server's truth — but only once it actually carries
                // skills, so a fresh character keeps the client's default (basic attack on).
                if (c.Skills != null && c.Skills.Length > 0)
                {
                    AutoSkills.Clear();
                    foreach (var s in c.Skills) if (s.Enabled) AutoSkills.Add(s.SkillId);
                }
            });
            _net.AutoHuntStatusReceived += st => Main(() => { if (st != null) AutoHunting = st.Enabled; });
            _net.RegionReceived += r => Main(() => Ui?.ShowRegionNotice(r));
            _net.NoticeReceived += m => Main(() => Ui?.ShowToast(m));
            _net.SelectionReceived += o => Main(() => Ui?.ShowBoxSelection(o));
            _net.PartyReceived += p => Main(() =>
            {
                Party = p?.Members ?? new PartyMemberDto[0];
                if (p != null) PartyLoot = p.LootMode;
            });
            _net.PartyInviteReceived += i => Main(() =>
            {
                PendingInvite = i;
                // The loot rule is named in the invite ON PURPOSE: joining a party silently changes
                // who gets the drops, and that is not something to discover after the fact.
                ClientLog.Good(i.InviterName + " invites you to a party (loot: " + i.LootMode + ").");
            });
            _net.ResurrectOfferReceived += o => Main(() =>
            {
                PendingResurrect = o;
                ClientLog.Good(o.FromName + " offers to resurrect you (" + (int)(o.ExpPct * 100f) + "% exp back).");
            });
            _net.InventoryReceived += i => Main(() =>
                Inventory = i?.Items ?? new InventoryItemDto[0]);
            _net.LearnedReceived += l => Main(() =>
            {
                Learned.Clear();
                if (l?.Skills != null)
                    foreach (var s in l.Skills) Learned[s.Id] = s.Level;
            });
            _net.SubclassesReceived += s => Main(() =>
            {
                if (s?.Classes == null) return;
                // Keep the WHOLE list, not just the active one: the debug Class tab lists every class
                // you own so you can swap between them, which is the owner's way of comparing two
                // builds in the same gear without relogging.
                Subclasses = s.Classes;
                foreach (var c in s.Classes) if (c.Active) { ActiveClass = c; break; }
                Ui?.RefreshDebugPanel();   // a swap that already happened must not still be offered
            });
            _net.DebugConfigReceived += c => Main(() =>
            {
                DebugConfig = c;
                Ui?.FillTuning(c);
            });
            _net.TradeRequestReceived += t => Main(() => Ui?.OnTradeRequest(t));
            _net.TradeStateReceived += t => Main(() => Ui?.OnTradeState(t));
            _net.SkillBarReceived += b => Main(() =>
            {
                // Copy into a fixed 60 rather than trusting the length: the bar is rendered by index
                // and a short array from an older server would throw on every frame.
                var slots = new string[GameConstants.SkillBarSlots];
                if (b?.Slots != null)
                    for (int i = 0; i < slots.Length && i < b.Slots.Length; i++) slots[i] = b.Slots[i];
                SkillBar = slots;
            });
            _net.ChatReceived += m => Main(() => ClientLog.Info(m.From + ": " + m.Text));
            _net.CombatReceived += OnCombat;
            _net.CastReceived += c => Main(() =>
            {
                // Seconds <= 0 is the server saying the cast ENDED (finished or was cancelled), not a
                // zero-length cast — treating it as one would leave the bar stuck full.
                if (c == null || c.Seconds <= 0f) { CastingSkill = null; return; }
                CastingSkill = c.SkillName;
                CastStartedAt = Time.realtimeSinceStartup;
                CastEndsAt = CastStartedAt + c.Seconds;

                // The walk is over HERE — when the SERVER confirms a cast started and roots you — and
                // not when the button was tapped. See UseSkill for why guessing was wrong.
                CancelMoveOrder();
            });
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
            Restoring = true;
            try
            {
                ClientLog.Info("Reconnected — restoring session …");
                var auth = await _net.LoginAsync(_authUser, _authPass);
                if (!auth.Success) { Fail("Re-login failed: " + auth.Error); return; }

                if (_lastCharacterId >= 0 && Phase == ClientPhase.InWorld)
                    await EnterWorld(_lastCharacterId, silent: true);
                else
                    await RefreshCharacters();
                ClientLog.Good("Session restored.");
            }
            catch (Exception ex) { Fail("Restore failed: " + Describe(ex)); }
            finally { Restoring = false; }
        }

        /// <summary>
        /// True while a silent reconnect is re-logging in and re-entering the world.
        ///
        /// Backgrounding the app on a phone drops the socket, so coming back always runs this — and it
        /// USED to show the character-select screen for the length of the round trip, because the
        /// restore called EnterWorld and the UI treats <see cref="ClientPhase.Entering"/> as "choosing
        /// a character". Nothing was broken; it just wore the wrong screen. The UI now keeps the world
        /// up and puts a "Reconnecting" notice over it.
        /// </summary>
        public bool Restoring { get; private set; }

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

        /// <param name="silent">A re-entry after a transport blip rather than a player choosing a
        /// character. It keeps the phase at InWorld so the UI does not flash the character-select
        /// screen for the length of the round trip.</param>
        public async Task EnterWorld(int characterId, bool silent = false)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                if (!silent) Phase = ClientPhase.Entering;
                StatusMessage = silent ? "Reconnecting …" : "Entering world …";

                // Wipe the old world BEFORE the request goes out, not after it returns. The server starts
                // streaming the moment the character is in the world, and those frames can land while we
                // are still awaiting the reply — clearing afterwards threw away the one full spawn of
                // your own entity, which is then never re-sent because a standing player never changes.
                // That is the "waiting for your entity …" bug: mobs trickled back in as they wandered,
                // you never did.
                if (Entities != null) { Entities.Clear(); Entities.SetSelf(Guid.Empty); }
                if (CameraRig != null) CameraRig.Target = null;   // re-acquire on the next frame
                if (Marker != null) { Marker.Follow = null; Marker.Hide(); }

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

        /// <summary>Clear everything that belongs to the CHARACTER you were playing, so re-entering the
        /// world (as the same or a different character) never shows a ghost from the last session — the
        /// party roster that hung around after a relog, a half-open NPC dialog, a stale target window.
        /// The server re-pushes what still applies on entry; this just makes "nothing" the default.</summary>
        private void ResetWorldTransients()
        {
            TargetId = null;
            Party = new PartyMemberDto[0];
            PendingInvite = null;
            PendingResurrect = null;
            Dialog = null;
            Details = null;
            DialogNpcId = Guid.Empty;
        }

        public async void LeaveWorld()
        {
            try
            {
                await _net.LeaveWorldAsync();
                Main(() =>
                {
                    if (Entities != null) Entities.Clear();
                    ResetWorldTransients();
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
                if (Marker != null) { Marker.Follow = null; Marker.Hide(); }
                ResetWorldTransients();
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

        /// <summary>Raised on the main thread for every combat event that involves YOU — the HUD turns
        /// these into floating damage numbers.</summary>
        public event Action<CombatEvent> CombatHappened;

        /// <summary>What this character is casting, and when it finishes (realtime). Name is null when
        /// nothing is being cast.</summary>
        public string CastingSkill { get; private set; }
        public float CastStartedAt { get; private set; }
        public float CastEndsAt { get; private set; }

        private void OnCombat(CombatEvent e)
        {
            Main(() =>
            {
                if (e.AttackerId != _selfId && e.TargetId != _selfId) return;
                if (CombatHappened != null) CombatHappened(e);
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
            if (self != null)
            {
                CameraRig.Target = self.transform;
                if (Marker != null) Marker.Follow = self.transform;   // so it clears on arrival
            }
        }

        // ----- Commands ------------------------------------------------------------------------

        /// <summary>True while a cast is in progress — casting ROOTS you (commit on start), so the
        /// server rejects movement until it finishes or you cancel it.</summary>
        public bool IsCasting => !string.IsNullOrEmpty(CastingSkill)
                                 && Time.realtimeSinceStartup < CastEndsAt;

        public async void Move(float serverX, float serverY)
        {
            if (Phase != ClientPhase.InWorld) return;

            // The server DROPS a move command while casting (HandleMove returns early). Sending one
            // anyway and dropping a destination ring on the ground advertises an order that was thrown
            // away — the character stands still next to a marker promising otherwise. Say why instead.
            //
            // Deliberately NOT queued-until-the-cast-ends: that is rare in the genre (WoW/FFXIV cancel
            // the cast on move; L2 roots you and ignores the click) and this project's design is the
            // L2 one — "casting roots you, ESC cancels".
            if (IsCasting)
            {
                ClientLog.Warn("Can't move while casting — tap the cast bar (or press Back) to cancel.");
                return;
            }

            // Don't predict a walk the server will DROP — that mismatch is what rubber-bands you. It
            // drops one while you're SITTING, and whenever your effective speed is 0 (stun / root): the
            // last delta's self-speed is that number, so a 0 there means "you can't move right now".
            bool sitting = Stats != null && Stats.MoveState == MoveState.Sitting;
            bool immobile = Entities != null && Entities.TryGetState(SelfId, out var selfState)
                            && selfState.Speed <= 0.01f;
            if (sitting || immobile)
            {
                ClientLog.Warn(sitting ? "Stand up first — you can't move while sitting."
                                       : "You can't move right now.");
                return;
            }

            // Drop the destination ring here rather than in TouchInput, so EVERY move order shows one
            // — including any future ones that don't come from a tap.
            if (Marker != null) Marker.ShowAt(WorldMapper.ToUnity(serverX, serverY));

            // PREDICT IMMEDIATELY. Waiting for the server's first position means every step you take
            // starts a round trip late, and no amount of smoothing hides that on the character you are
            // driving. The walk is deterministic — straight toward the target at Speed — so the client
            // can run exactly the same simulation the server will, and be corrected if it is wrong.
            if (Entities != null) Entities.PredictSelfMoveTo(serverX, serverY);

            try { await _net.MoveAsync(serverX, serverY); }
            catch (Exception ex) { ClientLog.Warn("Move: " + ex.Message); }
        }

        /// <summary>
        /// Fire one skill-bar slot. A slot holds a skill id, an "action:…" token or an "item:…" token;
        /// the server never interprets them, the CLIENT dispatches. Actions the mobile client hasn't
        /// grown yet say so out loud rather than doing nothing — a dead button that stays silent is
        /// indistinguishable from a broken one.
        /// </summary>
        public void UseSlot(string token)
        {
            if (Phase != ClientPhase.InWorld || string.IsNullOrEmpty(token)) return;

            if (ActionCatalog.FromToken(token) is ActionDef action)
            {
                // No blanket CancelMoveOrder here. "Every action either stops you or retargets you" is
                // simply untrue: a Run/Walk toggle changes your SPEED and you keep going, a basic
                // attack with no target does nothing at all, and standing up is not a stop. Each case
                // below already cancels the walk when it really ends it (Attack and SetMoveState do),
                // so the blanket call could only ever be wrong — and it was: tapping a bar slot cut the
                // walk's prediction dead while the server walked on.
                switch (action.Id)
                {
                    case GameConstants.ActionBasicAttack:
                        if (TargetId.HasValue) Attack(TargetId.Value);
                        else ClientLog.Warn("No target.");
                        break;
                    case GameConstants.ActionSitStand:
                        SetMoveState(Stats != null && Stats.MoveState == MoveState.Sitting
                                     ? MoveState.Running : MoveState.Sitting);
                        break;
                    case GameConstants.ActionRunWalk:
                        // Walk/run only changes SPEED, and only once you're standing — toggling it while
                        // seated must NOT stand you up (owner). Stand first with Sit/Stand.
                        if (Stats != null && Stats.MoveState == MoveState.Sitting)
                            ClientLog.Warn("Stand up first — walk/run only changes speed while standing.");
                        else
                            SetMoveState(Stats != null && Stats.MoveState == MoveState.Walking
                                         ? MoveState.Running : MoveState.Walking);
                        break;
                    case GameConstants.ActionTargetClosest:
                        TargetClosest();
                        break;
                    case GameConstants.ActionTradeTarget:
                        if (TargetId.HasValue) { var id = TargetId.Value; Trade(n => n.TradeRequestAsync(id), "request"); }
                        else ClientLog.Warn("Target a player to trade.");
                        break;
                    case GameConstants.ActionPartyInvite:
                        if (TargetId.HasValue) PartyInvite(TargetId.Value);
                        else ClientLog.Warn("Target a player to invite.");
                        break;
                    case GameConstants.ActionFollowTarget:
                        if (TargetId.HasValue) Follow(TargetId.Value);
                        else ClientLog.Warn("Target a player to follow.");
                        break;
                    case GameConstants.ActionAssistTarget:
                        if (TargetId.HasValue) Assist(TargetId.Value);
                        else ClientLog.Warn("Target a player to assist.");
                        break;

                    // ---- Name-only commands: the TARGET supplies the name, so nothing is typed. The
                    //      friend hub takes a NAME (friendship must work on someone who is offline), so
                    //      these resolve the target to its name first; the party ones take an id. ----
                    case GameConstants.ActionFriendAdd:
                        if (TargetPlayerName() is string addName) FriendCommand("add", addName);
                        else ClientLog.Warn("Target a player to add as a friend.");
                        break;
                    case GameConstants.ActionFriendRemove:
                        if (TargetPlayerName() is string remName) FriendCommand("remove", remName);
                        else ClientLog.Warn("Target a player to remove from your friends.");
                        break;
                    case GameConstants.ActionFriendList:
                        FriendCommand("list", "");
                        break;
                    case GameConstants.ActionPartyLeave:
                        PartyLeave();
                        break;
                    case GameConstants.ActionPartyKick:
                        if (TargetId.HasValue) PartyKick(TargetId.Value);
                        else ClientLog.Warn("Target a party member to remove.");
                        break;
                    case GameConstants.ActionPartyLeader:
                        if (TargetId.HasValue) PartyChangeLeader(TargetId.Value);
                        else ClientLog.Warn("Target a party member to pass leadership to.");
                        break;
                    default:
                        ClientLog.Warn(action.Name + " isn't available on the phone yet.");
                        break;
                }
                return;
            }

            if (GameConstants.IsItemSlot(token))
            {
                // Drink/use one of that item from the bag. The slot holds "item:<defId>", so any stack
                // of that potion satisfies it — this is the quick-use bar (owner).
                string defId = token.Substring(GameConstants.SkillBarItemPrefix.Length);
                if (FindBagItem(defId) is Guid iid) UsePotion(iid);
                else ClientLog.Warn("You have no " + (ItemCatalog.Get(defId)?.Name ?? defId) + " to use.");
                return;
            }

            if (GameConstants.IsPresetSlot(token))
            {
                if (int.TryParse(token.Substring(GameConstants.SkillBarPresetPrefix.Length), out int ps))
                    ApplyEquipPreset(ps);
                return;
            }

            UseSkill(token);
        }

        /// <summary>
        /// Whether this character is willing to fight other PLAYERS.
        ///
        /// With it OFF you can only hit mobs — the server refuses a swing at a player outright, which
        /// is what stops a stray tap in a crowd from starting a fight you did not want. It is not a
        /// client-side courtesy: <c>CanPvpHit</c> re-checks it, along with safe zones.
        ///
        /// Set optimistically on tap and then CORRECTED by the server's PvpState push, which is the
        /// authority — it also refuses the toggle outright in a safe zone, and a button that keeps
        /// claiming "PvP ON" after a refusal is worse than one that lags by a tick.
        /// </summary>
        public bool PvpEnabled { get; private set; }

        /// <summary>Reputation, straight from the PvpState push. Karma &gt; 0 makes you a PK: guards
        /// attack, you drop gear on death, and towns stop being safe for you.</summary>
        public int Karma { get; private set; }
        public int PkCount { get; private set; }
        public int PvpCount { get; private set; }

        public async void TogglePvp()
        {
            if (Phase != ClientPhase.InWorld) return;
            PvpEnabled = !PvpEnabled;
            ClientLog.Info(PvpEnabled
                ? "PvP ON — you can hit other players, and they can hit you back."
                : "PvP off — mobs only.");
            try { await _net.TogglePvpAsync(PvpEnabled); }
            catch (Exception ex) { ClientLog.Warn("PvP: " + ex.Message); }
        }

        /// <summary>
        /// Idle farming. The AUTOPILOT LIVES ON THE SERVER — targeting, skill choice, potions, the
        /// lot — so this is genuinely one flag, not a client bot. That is also why it keeps running
        /// when the app is closed.
        /// </summary>
        public bool AutoHunting { get; private set; }

        /// <summary>Skill ids the player has marked for auto-use, plus the pseudo-id
        /// <see cref="AutoHuntIds.BasicAttack"/>. The per-slot Auto toggle writes here.</summary>
        public readonly HashSet<string> AutoSkills = new HashSet<string> { AutoHuntIds.BasicAttack };

        /// <summary>The last config the SERVER confirmed (potions %, buff potions, farm range, ranks).
        /// The auto-potions and auto-farm windows read and edit THIS; every push preserves the fields
        /// it does not own, because <c>SetAutoHuntConfig</c> replaces the whole config wholesale —
        /// hardcoding the untouched half (as the toggle used to) silently reset the player's settings on
        /// every on/off.</summary>
        public AutoHuntConfigDto AutoConfig { get; private set; } =
            new AutoHuntConfigDto(false, 60, 40, false, new AutoSkillDto[0], new string[0]);

        /// <summary>Build a full config that flips Enabled and reflects the current auto-skill marks,
        /// while carrying every other field (potions, farm) forward from the cached config.</summary>
        private AutoHuntConfigDto BuildAutoConfig(bool enabled)
        {
            var skills = new List<AutoSkillDto>();
            foreach (var id in AutoSkills)
                if (id == AutoHuntIds.BasicAttack || Learned.ContainsKey(id))
                {
                    int extra = 0;   // preserve any per-skill reuse the server already knows
                    if (AutoConfig.Skills != null)
                        foreach (var s in AutoConfig.Skills)
                            if (s.SkillId == id) { extra = s.ExtraDelayTicks; break; }
                    skills.Add(new AutoSkillDto(id, true, extra));
                }
            return AutoConfig with { Enabled = enabled, Skills = skills.ToArray() };
        }

        public async void ToggleAutoHunt()
        {
            if (Phase != ClientPhase.InWorld) return;
            AutoHunting = !AutoHunting;

            try
            {
                // The CONFIG carries the actions: the server's autopilot only uses skills it was GIVEN,
                // and an empty list is why auto-hunt "just wandered". Basic attack is the pseudo-skill
                // that makes it melee at all; without it a fighter walks up to a mob and stares at it.
                // Sending the WHOLE config (not a bare toggle) keeps the potion/farm settings the player
                // configured in their windows — a bare enable used to overwrite them with defaults.
                await _net.SetAutoHuntConfigAsync(BuildAutoConfig(AutoHunting));
                ClientLog.Info(AutoHunting
                    ? "Auto-hunt ON (" + AutoSkills.Count + " action(s))."
                    : "Auto-hunt off.");
            }
            catch (Exception ex)
            {
                AutoHunting = !AutoHunting;   // the server never heard us; keep the button honest
                ClientLog.Warn("AutoHunt: " + ex.Message);
            }
        }

        /// <summary>Push a config edited by the auto-potions / auto-farm windows. Optimistic on the
        /// cache; the server's echo confirms the clamped values.</summary>
        public async void PushAutoConfig(AutoHuntConfigDto cfg)
        {
            if (Phase != ClientPhase.InWorld || cfg == null) return;
            AutoConfig = cfg;
            AutoHunting = cfg.Enabled;
            try { await _net.SetAutoHuntConfigAsync(cfg); }
            catch (Exception ex) { ClientLog.Warn("Auto config: " + ex.Message); }
        }

        /// <summary>Mark/unmark a skill for auto-use, and push the change if auto-hunt is running.</summary>
        public void ToggleAutoSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            if (!AutoSkills.Remove(skillId)) AutoSkills.Add(skillId);
            if (AutoHunting) PushAutoConfig(BuildAutoConfig(true));
        }

        public bool CounterAttack { get; private set; }

        public async void ToggleCounterAttack()
        {
            if (Phase != ClientPhase.InWorld) return;
            CounterAttack = !CounterAttack;
            ClientLog.Info(CounterAttack ? "Counter-attack ON." : "Counter-attack off.");
            try { await _net.ToggleCounterAttackAsync(CounterAttack); }
            catch (Exception ex) { ClientLog.Warn("CounterAttack: " + ex.Message); }
        }

        /// <summary>ESC/cancel a cast in progress. The server keeps the initial MP and starts the
        /// cooldown — cancelling is a choice with a cost, not a free undo.</summary>
        public async void CancelCast()
        {
            if (Phase != ClientPhase.InWorld) return;
            // Clear it locally too: the server has no "cast cancelled" push, so waiting for one would
            // leave the bar sitting there until the original duration ran out.
            CastingSkill = null;
            try { await _net.CancelCastAsync(); }
            catch (Exception ex) { ClientLog.Warn("CancelCast: " + ex.Message); }
        }

        public async void LearnSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.LearnSkillAsync(skillId); }
            catch (Exception ex) { ClientLog.Warn("Learn: " + ex.Message); }
        }

        /// <summary>
        /// Put a token in a bar slot and send the WHOLE bar back.
        ///
        /// This is the one write the client is allowed to make, and only because the PLAYER moved
        /// something. Writing a bar the client authored itself — reacting to a Learned push, say —
        /// is what destroyed real layouts in the WPF client twice: it looked right on screen while
        /// the server's copy was already wrong.
        /// </summary>
        public async void AssignSlot(int index, string token)
        {
            if (Phase != ClientPhase.InWorld) return;
            if (SkillBar == null || index < 0 || index >= SkillBar.Length) return;

            var slots = (string[])SkillBar.Clone();
            slots[index] = token;
            SkillBar = slots;               // optimistic: the server echoes the bar back anyway
            try { await _net.SetSkillBarAsync(slots); }
            catch (Exception ex) { ClientLog.Warn("SkillBar: " + ex.Message); }
        }

        /// <summary>Swap two bar slots and send the bar ONCE. Two AssignSlot calls would send two
        /// bars, and the second would be built from a copy that never saw the first.</summary>
        public async void SwapSlots(int from, int to)
        {
            if (Phase != ClientPhase.InWorld || SkillBar == null) return;
            if (from < 0 || to < 0 || from >= SkillBar.Length || to >= SkillBar.Length || from == to) return;

            var slots = (string[])SkillBar.Clone();
            var moved = slots[from];
            slots[from] = slots[to];
            slots[to] = moved;
            SkillBar = slots;
            try { await _net.SetSkillBarAsync(slots); }
            catch (Exception ex) { ClientLog.Warn("SkillBar: " + ex.Message); }
        }

        /// <summary>
        /// Ask the server to cast. Deliberately does NOT cancel the walk.
        ///
        /// 🔴 It used to, and that was a guess about a decision only the server makes. Tap a skill with
        /// no target and the server REFUSES it — you keep walking — but the client had already dropped
        /// the destination ring and killed the prediction. The ring vanished while the character walked
        /// on (reported: "when I click the skill the ground move-flag disappears"), and the abandoned
        /// prediction handed the character back to the interpolator mid-walk, which is where the visible
        /// judder came from.
        ///
        /// The walk now ends when the server says a cast STARTED (see the CastReceived handler), which
        /// is the same moment it actually roots you. Refused casts change nothing, because nothing
        /// happened.
        /// </summary>
        public async void UseSkill(string skillId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.UseSkillAsync(skillId, TargetId); }
            catch (Exception ex) { ClientLog.Warn("UseSkill: " + ex.Message); }
        }

        /// <summary>
        /// Drop the destination ring, because the walk it described is over.
        ///
        /// The rule is: the ring means "I am going THERE". Anything that stops the character or sends
        /// them somewhere else makes it a lie — casting (which roots you), attacking (you chase the
        /// target instead), sitting, dying, teleporting. Leaving it on the ground pointing at a place
        /// you are no longer walking to is worse than not having it.
        /// </summary>
        public void CancelMoveOrder()
        {
            if (Marker != null) Marker.Hide();
            // Stop predicting too, or the character keeps walking locally toward a destination the
            // server has already thrown away — which then reads as a rubber-band when it corrects.
            if (Entities != null) Entities.CancelSelfPrediction();
        }

        /// <summary>Equip or unequip — the SERVER decides which, from the item's current state, so the
        /// client can't disagree with it about what is worn.</summary>
        public async void EquipItem(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.EquipItemAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("Equip: " + ex.Message); }
        }

        public async void UsePotion(Guid instanceId)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.UsePotionAsync(instanceId); }
            catch (Exception ex) { ClientLog.Warn("UsePotion: " + ex.Message); }
        }

        /// <summary>Drop/destroy an item. all=true bins the whole stack, false a single unit — the
        /// server enforces which is legal (a non-stackable ignores the distinction).</summary>
        public async void RemoveItem(Guid instanceId, bool all, int quantity = 0)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await _net.RemoveItemAsync(instanceId, all, quantity); }
            catch (Exception ex) { ClientLog.Warn("RemoveItem: " + ex.Message); }
        }

        /// <summary>Fire-and-forget trade call. Same shape as <see cref="Debug"/>: the trade window
        /// never applies anything itself, it just tells the server and redraws from what comes back.</summary>
        public async void Trade(Func<NetworkChannel, Task> call, string what)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await call(_net); }
            catch (Exception ex) { ClientLog.Warn("Trade " + what + ": " + ex.Message); }
        }

        /// <summary>Fire-and-forget debug call. Every one of these is re-checked server-side against
        /// the account role — <see cref="IsAdmin"/> only decides whether we bother SHOWING the panel.</summary>
        public async void Debug(Func<NetworkChannel, Task> call, string what)
        {
            if (Phase != ClientPhase.InWorld) return;
            try { await call(_net); }
            catch (Exception ex) { ClientLog.Warn(what + ": " + ex.Message); }
        }

        public async void Attack(Guid targetId)
        {
            if (Phase != ClientPhase.InWorld) return;
            TargetId = targetId;
            // Attacking supersedes the walk order, so the destination ring has to go. You chase the
            // TARGET, not the spot you last tapped, so the ring would never be reached and would sit
            // on the ground pointing at nothing.
            //
            // CancelMoveOrder rather than just hiding the ring: the prediction has to stop for the
            // same reason the ring does. Chasing a target is a walk the SERVER steers — it follows a
            // moving mob — so predicting a straight line to the old tap point would diverge and snap.
            CancelMoveOrder();
            try { await _net.AttackAsync(targetId); }
            catch (Exception ex) { ClientLog.Warn("Attack: " + ex.Message); }
        }

        public async void SetMoveState(MoveState state)
        {
            if (state == MoveState.Sitting) CancelMoveOrder();   // sitting cancels the walk
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

                // Party target-commands (leader-only ones are re-checked server-side). Every one has an
                // action button too (target frame / party window); these are the typed equivalents.
                if (raw.Equals("/ptleave", StringComparison.OrdinalIgnoreCase))
                { PartyLeave(); return; }
                if (raw.StartsWith("/ptinv ", StringComparison.OrdinalIgnoreCase))
                {
                    var id = FindPlayerByName(raw.Substring(7).Trim());
                    if (id is Guid g) PartyInvite(g); else ClientLog.Warn("No player '" + raw.Substring(7).Trim() + "' nearby.");
                    return;
                }
                if (raw.StartsWith("/ptkick ", StringComparison.OrdinalIgnoreCase))
                {
                    var id = PartyMemberId(raw.Substring(8).Trim());
                    if (id is Guid g) PartyKick(g); else ClientLog.Warn("Not a party member.");
                    return;
                }
                if (raw.StartsWith("/ptcl ", StringComparison.OrdinalIgnoreCase))
                {
                    var id = PartyMemberId(raw.Substring(6).Trim());
                    if (id is Guid g) PartyChangeLeader(g); else ClientLog.Warn("Not a party member.");
                    return;
                }

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
