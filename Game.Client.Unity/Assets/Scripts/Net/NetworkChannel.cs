using System;
using System.Threading.Tasks;
using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Game.Client
{
    /// <summary>
    /// The single seam between game code and the wire — identical to the WPF client's
    /// NetworkChannel (no UI dependencies). SignalR callbacks arrive on a BACKGROUND thread;
    /// consumers (GameBoot) must marshal event handlers onto Unity's main thread
    /// (see UnityMainThreadDispatcher) before touching any UnityEngine API.
    /// </summary>
    public class NetworkChannel : IAsyncDisposable
    {
        private HubConnection _connection;

        public event Action<WorldSnapshot> SnapshotReceived;
        /// <summary>The LIVE world feed. The server stopped sending full "Snapshot" frames long ago —
        /// it sends deltas (spawn/update/despawn). Subscribing only to SnapshotReceived means an
        /// eternally empty world, which is exactly how this client behaved before.</summary>
        public event Action<SnapshotDelta> SnapshotDeltaReceived;
        public event Action<ChatMessage> ChatReceived;
        public event Action<CombatEvent> CombatReceived;
        public event Action<ProgressUpdate> ProgressReceived;
        public event Action<CastInfo> CastReceived;
        public event Action<InventoryUpdate> InventoryReceived;
        public event Action<WarehouseUpdate> WarehouseReceived;
        public event Action<StatsUpdate> StatsReceived;
        public event Action<BuffUpdate> BuffsReceived;
        public event Action<GoldUpdate> GoldReceived;
        public event Action<TargetDetails> TargetDetailsReceived;
        /// <summary>PvP toggles + reputation (karma, PK/PvP counts). The client used to TRACK the PvP
        /// flag locally by flipping a bool on every tap, which is a guess: the server refuses the
        /// toggle in a safe zone, and nothing told the button. This push is the authority.</summary>
        public event Action<PvpState> PvpStateReceived;
        /// <summary>An ally (or a scroll) offers to bring you back. The player must ACCEPT — reviving
        /// automatically would drop you on top of whatever just killed you.</summary>
        public event Action<ResurrectOffer> ResurrectOfferReceived;
        /// <summary>The party roster. An EMPTY member array means "you are not in a party" — that is
        /// how the server says you left or were the last one out.</summary>
        public event Action<PartyUpdate> PartyReceived;
        public event Action<PartyInviteDto> PartyInviteReceived;
        public event Action<LearnedSkills> LearnedReceived;
        /// <summary>The SERVER owns the skill bar and pushes it (always alongside Learned). The client
        /// renders what it is sent and only writes back when the PLAYER edits a slot — see
        /// SetSkillBarAsync.</summary>
        public event Action<SkillBarDto> SkillBarReceived;
        /// <summary>The character's classes, one of them Active. This is the only push that carries the
        /// ACTIVE class's race/base/second/third — which the skills window needs to work out what is
        /// learnable, and which changes under you on a subclass swap.</summary>
        public event Action<SubclassListDto> SubclassesReceived;

        /// <summary>The server's CURRENT tuning values — sent on request and echoed after a change,
        /// with the server's clamping already applied. The panel is always filled from this, never
        /// from what the client last sent.</summary>
        public event Action<DebugConfigDto> DebugConfigReceived;

        /// <summary>Someone wants to trade with you.</summary>
        public event Action<TradeRequestNotice> TradeRequestReceived;

        /// <summary>The whole trade, re-sent on every change. Active=false closes the window — the
        /// server is the only thing that decides a trade is over.</summary>
        public event Action<TradeStateUpdate> TradeStateReceived;
        public event Action<NpcDialog> DialogReceived;
        public event Action<QuestLog> QuestLogReceived;

        /// <summary>The player's stored auto-hunt config, echoed on login and after every change with
        /// the server's clamping applied. The config/farm windows fill from THIS, never from what they
        /// last sent — the server owns the values.</summary>
        public event Action<AutoHuntConfigDto> AutoConfigReceived;
        /// <summary>Live auto-hunt HUD: whether it is running + MP/s. Also the authority on the on/off
        /// state, which the server can flip on its own (idle-time lock, death).</summary>
        public event Action<AutoHuntStatus> AutoHuntStatusReceived;

        /// <summary>You crossed into a named region — shown as transient centre-screen text.</summary>
        public event Action<RegionNotice> RegionReceived;

        /// <summary>A plain server notice (e.g. the 3h "take a break" nudge) — shown as a transient banner.</summary>
        public event Action<string> NoticeReceived;

        /// <summary>A SELECTION box was opened — the server offers choices; the client shows a chooser.</summary>
        public event Action<SelectionOffer> SelectionReceived;
        public event Action<string> Disconnected;
        public event Action<string> ForceDisconnected;
        // WithAutomaticReconnect silently gives us a NEW connection id on a transport blip, and the
        // server drops our session on the old one — so after a reconnect we are connected but NOT
        // authenticated. GameBoot listens here to re-login + re-enter. Without this, a phone-link
        // hiccup logs you out invisibly (empty character list, "not logged in" on any action).
        public event Action Reconnecting;
        public event Action Reconnected;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string url)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<WorldSnapshot>("Snapshot", s => SnapshotReceived?.Invoke(s));
            _connection.On<SnapshotDelta>("SnapshotDelta", d => SnapshotDeltaReceived?.Invoke(d));
            _connection.On<ChatMessage>("Chat", m => ChatReceived?.Invoke(m));
            _connection.On<CombatEvent>("Combat", c => CombatReceived?.Invoke(c));
            _connection.On<ProgressUpdate>("Progress", p => ProgressReceived?.Invoke(p));
            _connection.On<CastInfo>("Cast", c => CastReceived?.Invoke(c));
            _connection.On<InventoryUpdate>("Inventory", i => InventoryReceived?.Invoke(i));
            _connection.On<WarehouseUpdate>("Warehouse", w => WarehouseReceived?.Invoke(w));
            _connection.On<StatsUpdate>("Stats", st => StatsReceived?.Invoke(st));
            _connection.On<LearnedSkills>("Learned", l => LearnedReceived?.Invoke(l));
            _connection.On<SkillBarDto>("SkillBar", b => SkillBarReceived?.Invoke(b));
            _connection.On<SubclassListDto>("Subclasses", s => SubclassesReceived?.Invoke(s));
            _connection.On<DebugConfigDto>("DebugConfig", c => DebugConfigReceived?.Invoke(c));
            _connection.On<TradeRequestNotice>("TradeRequest", t => TradeRequestReceived?.Invoke(t));
            // "Trade", not "TradeState" — the server's name for it (GameLoopService:3122).
            _connection.On<TradeStateUpdate>("Trade", t => TradeStateReceived?.Invoke(t));
            _connection.On<NpcDialog>("Dialog", d => DialogReceived?.Invoke(d));
            _connection.On<QuestLog>("QuestLog", q => QuestLogReceived?.Invoke(q));
            _connection.On<AutoHuntConfigDto>("AutoConfig", c => AutoConfigReceived?.Invoke(c));
            _connection.On<AutoHuntStatus>("AutoHunt", s => AutoHuntStatusReceived?.Invoke(s));
            _connection.On<RegionNotice>("Region", r => RegionReceived?.Invoke(r));
            _connection.On<string>("Notice", m => NoticeReceived?.Invoke(m));
            _connection.On<SelectionOffer>("Selection", o => SelectionReceived?.Invoke(o));
            _connection.On<BuffUpdate>("Buffs", b => BuffsReceived?.Invoke(b));
            _connection.On<GoldUpdate>("Gold", g => GoldReceived?.Invoke(g));
            _connection.On<TargetDetails>("TargetDetails", d => TargetDetailsReceived?.Invoke(d));
            _connection.On<PvpState>("PvpState", p => PvpStateReceived?.Invoke(p));
            _connection.On<ResurrectOffer>("ResurrectOffer", o => ResurrectOfferReceived?.Invoke(o));
            _connection.On<PartyUpdate>("Party", p => PartyReceived?.Invoke(p));
            _connection.On<PartyInviteDto>("PartyInvite", i => PartyInviteReceived?.Invoke(i));
            _connection.On<string>("ForceDisconnect", reason => ForceDisconnected?.Invoke(reason));
            _connection.Closed += ex =>
            {
                Disconnected?.Invoke(ex?.Message ?? "Connection closed.");
                return Task.CompletedTask;
            };
            _connection.Reconnecting += _ => { Reconnecting?.Invoke(); return Task.CompletedTask; };
            _connection.Reconnected += _ => { Reconnected?.Invoke(); return Task.CompletedTask; };

            await _connection.StartAsync();
        }

        // ----- Auth + character flow -----
        // The version argument is the handshake from GameConstants.GameVersion: the server rejects a
        // client whose version differs (an out-of-date client speaks an out-of-date protocol). Because
        // Game.Shared is compiled INTO this client, sending the constant can never drift out of sync.
        public Task<AuthResponse> RegisterAsync(string username, string password) =>
            _connection.InvokeAsync<AuthResponse>("Register",
                new AuthRequest(username, password, GameConstants.ProtocolVersion),
                GameConstants.GameVersion);

        public Task<AuthResponse> LoginAsync(string username, string password) =>
            // The protocol travels INSIDE AuthRequest (see that record for why it cannot be another
            // hub parameter). The build label stays a hub argument — it is already there, every client
            // sends it, and it is what makes the "please update to vX" message readable.
            _connection.InvokeAsync<AuthResponse>("Login",
                new AuthRequest(username, password, GameConstants.ProtocolVersion),
                GameConstants.GameVersion);

        public Task<CharacterList> ListCharactersAsync() =>
            _connection.InvokeAsync<CharacterList>("ListCharacters");

        public Task<string> CreateCharacterAsync(string name, Race race, BaseClass baseClass) =>
            _connection.InvokeAsync<string>("CreateCharacter", new CreateCharacterRequest(name, race, baseClass));

        public Task<LoginResult> EnterWorldAsync(int characterId) =>
            _connection.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(characterId));

        /// <summary>Read-only leaderboard fetch — answered straight from the DB, no world round-trip.</summary>
        public Task<LeaderboardDto> RequestLeaderboardAsync(string category) =>
            _connection.InvokeAsync<LeaderboardDto>("RequestLeaderboard", category);

        /// <summary>Open a box/chest from the inventory. A random box grants its loot immediately; a
        /// SELECTION box replies with a "Selection" push for the player to choose from.</summary>
        public Task OpenBoxAsync(Guid instanceId) => _connection.SendAsync("OpenBox", instanceId);

        /// <summary>Confirm the chosen item(s) from a selection box.</summary>
        public Task SelectBoxItemsAsync(Guid instanceId, string[] itemIds) =>
            _connection.SendAsync("SelectBoxItems", instanceId, itemIds);

        // ----- In-world commands (the slice uses Move + Attack; the rest are ready for later) -----
        public Task MoveAsync(float targetX, float targetY) =>
            _connection.SendAsync("Move", new MoveCommand(targetX, targetY));

        public Task AttackAsync(Guid targetId) =>
            _connection.SendAsync("Attack", targetId);

        public Task UseSkillAsync(string skillId, Guid? targetId) =>
            _connection.SendAsync("UseSkill", skillId, targetId);

        public Task SetMoveStateAsync(MoveState state) =>
            _connection.SendAsync("SetMoveState", (int)state);

        /// <summary>Follow a player (null = stop). Assist = attack whatever they're attacking. The server
        /// already had these; the Unity client just never exposed them, so Follow/Assist had no path.</summary>
        public Task FollowAsync(Guid? targetId) =>
            _connection.SendAsync("Follow", targetId);

        public Task AssistAsync(Guid targetId) =>
            _connection.SendAsync("Assist", targetId);

        public Task LeaveWorldAsync() =>
            _connection.SendAsync("LeaveWorld");

        public Task RequestResyncAsync() =>
            _connection.SendAsync("RequestResync");

        public Task LogoutAsync() =>
            _connection.SendAsync("Logout");

        public Task ChatAsync(string text, ChatChannel channel = ChatChannel.Local, string whisperTarget = null) =>
            _connection.SendAsync("Chat", text, channel, whisperTarget);

        public Task AdminCommandAsync(string command, string argument) =>
            _connection.SendAsync("AdminCommand", command, argument);

        public Task FriendCommandAsync(string action, string name) =>
            _connection.SendAsync("FriendCommand", action, name);

        public Task BlockCommandAsync(string action, string name) =>
            _connection.SendAsync("BlockCommand", action, name);

        public Task LikeAsync(string name) =>
            _connection.SendAsync("Like", name);

        public Task RespawnAsync() =>
            _connection.SendAsync("Respawn");

        public Task TogglePvpAsync(bool enabled) =>
            _connection.SendAsync("TogglePvp", enabled);

        public Task ToggleCounterAttackAsync(bool enabled) =>
            _connection.SendAsync("ToggleCounterAttack", enabled);

        /// <summary>Idle farming: the server drives the character (target, skills, potions) with the
        /// same brain the WPF client uses. Nothing here is client logic — it is one flag.</summary>
        public Task ToggleAutoHuntAsync(bool enabled) =>
            _connection.SendAsync("ToggleAutoHunt", enabled);

        public Task SetAutoHuntConfigAsync(AutoHuntConfigDto config) =>
            _connection.SendAsync("SetAutoHuntConfig", config);

        public Task StartOfflineFarmAsync() =>
            _connection.SendAsync("StartOfflineFarm");

        public Task CancelCastAsync() =>
            _connection.SendAsync("CancelCast");

        public Task LearnSkillAsync(string skillId) =>
            _connection.SendAsync("LearnSkill", skillId);

        /// <summary>Write the bar back. Legitimate ONLY when the PLAYER moved something — never as a
        /// reaction to a server push. See GameBoot.SkillBar.</summary>
        public Task SetSkillBarAsync(string[] slots) =>
            _connection.SendAsync("SetSkillBar", slots);

        // ----- inventory -------------------------------------------------------------------------
        /// <summary>Equip or UNEQUIP — the server toggles based on the item's current state.</summary>
        public Task EquipItemAsync(Guid instanceId) =>
            _connection.SendAsync("EquipItem", instanceId);

        public Task UsePotionAsync(Guid instanceId) =>
            _connection.SendAsync("UsePotion", instanceId);

        // ----- party ------------------------------------------------------------------------------
        public Task PartyInviteAsync(Guid targetId) => _connection.SendAsync("PartyInvite", targetId);
        public Task PartyRespondAsync(bool accept) => _connection.SendAsync("PartyRespond", accept);
        public Task PartyLeaveAsync() => _connection.SendAsync("PartyLeave");
        public Task PartyKickAsync(Guid targetId) => _connection.SendAsync("PartyKick", targetId);
        public Task PartyChangeLeaderAsync(Guid targetId) => _connection.SendAsync("PartyChangeLeader", targetId);
        public Task SaveEquipPresetAsync(int slot) => _connection.SendAsync("SaveEquipPreset", slot);
        public Task ApplyEquipPresetAsync(int slot) => _connection.SendAsync("ApplyEquipPreset", slot);
        public Task PartySetLootModeAsync(LootMode mode) => _connection.SendAsync("PartySetLootMode", mode);

        // ----- NPC interaction --------------------------------------------------------------------
        public Task TalkToNpcAsync(Guid npcEntityId) =>
            _connection.SendAsync("TalkToNpc", npcEntityId);

        /// <summary>action is "accept" / "complete" / "changeclass"; id is the quest id, or the class
        /// id as a string for changeclass. All of them need the NPC, which is why there is no quest
        /// action anywhere but a conversation.</summary>
        public Task QuestActionAsync(string action, string id, Guid npcEntityId) =>
            _connection.SendAsync("QuestAction", action, id, npcEntityId);

        /// <summary>action is "full" / "restore" / "single" (skillId only used by "single").</summary>
        public Task BufferActionAsync(Guid npcEntityId, string action, string skillId) =>
            _connection.SendAsync("BufferAction", npcEntityId, action, skillId);

        public Task BuyItemAsync(Guid npcEntityId, string itemDefId, int quantity) =>
            _connection.SendAsync("BuyItem", npcEntityId, itemDefId, quantity);

        public Task SellItemAsync(Guid npcEntityId, Guid instanceId, int quantity) =>
            _connection.SendAsync("SellItem", npcEntityId, instanceId, quantity);

        public Task OpenWarehouseAsync() => _connection.SendAsync("OpenWarehouse");
        public Task WarehouseDepositAsync(Guid instanceId) => _connection.SendAsync("WarehouseDeposit", instanceId);
        public Task WarehouseWithdrawAsync(Guid instanceId) => _connection.SendAsync("WarehouseWithdraw", instanceId);

        public Task TeleportAsync(Guid npcEntityId, string zoneId) =>
            _connection.SendAsync("Teleport", npcEntityId, zoneId);

        public Task ForgetSkillAsync(Guid npcEntityId, string skillId) =>
            _connection.SendAsync("ForgetSkill", npcEntityId, skillId);

        /// <summary>Ask the server for the expanded target window. withDrops adds a mob's drop list.</summary>
        public Task InspectTargetAsync(Guid targetId, bool withDrops) =>
            _connection.SendAsync("InspectTarget", targetId, withDrops);

        public Task ResurrectResponseAsync(bool accept) =>
            _connection.SendAsync("ResurrectResponse", accept);

        public Task RemoveBuffAsync(string buffKey) =>
            _connection.SendAsync("RemoveBuff", buffKey);

        public Task RemoveItemAsync(Guid instanceId, bool all, int quantity = 0) =>
            _connection.SendAsync("RemoveItem", instanceId, all, quantity);

        // ----- debug (server re-checks admin rights on every one of these) ------------------------
        public Task DebugLevelAsync(int delta) => _connection.SendAsync("DebugLevel", delta);
        public Task DebugLearnAllAsync() => _connection.SendAsync("DebugLearnAll");
        public Task DebugBuffAsync() => _connection.SendAsync("DebugBuff");
        public Task DebugGoldAsync(long amount) => _connection.SendAsync("DebugGold", amount);
        public Task DebugSpAsync(long amount) => _connection.SendAsync("DebugSp", amount);
        public Task DebugTeleportAsync(float x, float y) => _connection.SendAsync("DebugTeleport", x, y);
        public Task DebugGiveAsync(string defId, int quantity) =>
            _connection.SendAsync("DebugGive", defId, quantity);
        public Task DebugKarmaAsync(int delta) => _connection.SendAsync("DebugKarma", delta);
        public Task DebugSetProfessionAsync(int profession) =>
            _connection.SendAsync("DebugSetProfession", profession);
        public Task DebugThirdClassAsync(int thirdClassId) =>
            _connection.SendAsync("DebugThirdClass", thirdClassId);
        public Task DebugAddSubclassAsync(int thirdClassId) =>
            _connection.SendAsync("DebugAddSubclass", thirdClassId);
        public Task SwitchSubclassAsync(int slot) => _connection.SendAsync("SwitchSubclass", slot);
        public Task DebugResetAsync(Race race, BaseClass baseClass) =>
            _connection.SendAsync("DebugReset", (int)race, (int)baseClass);
        public Task DebugCancelAttrAsync(int index) => _connection.SendAsync("DebugCancelAttr", index);

        // Live tuning. Request populates the panel from the AUTHORITATIVE current values rather than
        // from whatever the client last sent — the server clamps, and the echo is how we learn what it
        // actually accepted.
        // ----- trade (server-side already; this is the client half) -------------------------------
        public Task TradeRequestAsync(Guid targetId) => _connection.SendAsync("TradeRequest", targetId);
        public Task TradeRespondAsync(bool accept) => _connection.SendAsync("TradeRespond", accept);
        public Task TradeOfferAsync(Guid[] instanceIds) => _connection.SendAsync("TradeOffer", instanceIds);
        public Task TradeGoldAsync(long gold) => _connection.SendAsync("TradeGold", gold);
        public Task TradeReadyAsync() => _connection.SendAsync("TradeReady");
        public Task TradeCancelAsync() => _connection.SendAsync("TradeCancel");

        public Task RequestDebugConfigAsync() => _connection.SendAsync("RequestDebugConfig");
        public Task SetDebugConfigAsync(DebugConfigDto config) =>
            _connection.SendAsync("SetDebugConfig", config);

        public async ValueTask DisposeAsync()
        {
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }
}
