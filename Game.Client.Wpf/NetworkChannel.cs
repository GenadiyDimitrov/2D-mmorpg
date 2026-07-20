using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Game.Client.Wpf;

/// <summary>
/// The single seam between game code and the wire. No WPF dependencies —
/// reusable in the Unity client as-is.
/// </summary>
public class NetworkChannel : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<WorldSnapshot>? SnapshotReceived;
    public event Action<SnapshotDelta>? SnapshotDeltaReceived;
    public event Action<Guid>? SetTargetReceived;
    public event Action<ChatMessage>? ChatReceived;
    public event Action<CombatEvent>? CombatReceived;
    public event Action<ProgressUpdate>? ProgressReceived;
    public event Action<CastInfo>? CastReceived;
    public event Action<MobCastInfo>? MobCastReceived;
    public event Action<InventoryUpdate>? InventoryReceived;
    public event Action<TradeRequestNotice>? TradeRequestReceived;
    public event Action<TradeStateUpdate>? TradeStateReceived;
    public event Action<StatsUpdate>? StatsReceived;
    public event Action<PotionStatus>? PotionReceived;
    public event Action<BuffUpdate>? BuffsReceived;
    public event Action<EnchantResultDto>? EnchantReceived;
    public event Action<RerollResultDto>? RerollReceived;
    public event Action<GoldUpdate>? GoldReceived;
    public event Action<SelectionOffer>? SelectionReceived;
    public event Action<TargetDetails>? TargetDetailsReceived;
    public event Action<PartyInviteDto>? PartyInviteReceived;
    public event Action<PartyUpdate>? PartyReceived;
    public event Action<PartyLootVoteDto>? PartyLootVoteReceived;
    public event Action<AutoHuntStatus>? AutoHuntReceived;
    public event Action<AutoHuntConfigDto>? AutoConfigReceived;
    public event Action<SkillBarDto>? SkillBarReceived;
    public event Action<SubclassListDto>? SubclassesReceived;
    public event Action<LogoutResult>? LogoutResultReceived;
    public event Action<PvpState>? PvpStateReceived;
    public event Action<DebugConfigDto>? DebugConfigReceived;
    public event Action<ResurrectOffer>? ResurrectOfferReceived;
    public event Action? ResurrectOfferExpired;
    public event Action<string>? Disconnected;
    public event Action<string>? ForceDisconnected;
    public event Action<AdminStateDto>? AdminStateReceived;
    public event Action<AdminBagDto>? AdminBagReceived;
    public event Action<AdminBagDto>? AdminGivePickerReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string url)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<WorldSnapshot>("Snapshot", s => SnapshotReceived?.Invoke(s));
        _connection.On<SnapshotDelta>("SnapshotDelta", d => SnapshotDeltaReceived?.Invoke(d));
        _connection.On<Guid>("SetTarget", id => SetTargetReceived?.Invoke(id));
        _connection.On<ChatMessage>("Chat", m => ChatReceived?.Invoke(m));
        _connection.On<CombatEvent>("Combat", c => CombatReceived?.Invoke(c));
        _connection.On<ProgressUpdate>("Progress", p => ProgressReceived?.Invoke(p));
        _connection.On<CastInfo>("Cast", c => CastReceived?.Invoke(c));
        _connection.On<MobCastInfo>("MobCast", c => MobCastReceived?.Invoke(c));
        _connection.On<InventoryUpdate>("Inventory", i => InventoryReceived?.Invoke(i));
        _connection.On<TradeRequestNotice>("TradeRequest", t => TradeRequestReceived?.Invoke(t));
        _connection.On<TradeStateUpdate>("Trade", t => TradeStateReceived?.Invoke(t));
        _connection.On<StatsUpdate>("Stats", st => StatsReceived?.Invoke(st));
        _connection.On<LearnedSkills>("Learned", l => LearnedReceived?.Invoke(l));
        _connection.On<NpcDialog>("Dialog", d => DialogReceived?.Invoke(d));
        _connection.On<QuestLog>("QuestLog", q => QuestLogReceived?.Invoke(q));
        _connection.On<PotionStatus>("Potion", pt => PotionReceived?.Invoke(pt));
        _connection.On<BuffUpdate>("Buffs", b => BuffsReceived?.Invoke(b));
        _connection.On<EnchantResultDto>("Enchant", en => EnchantReceived?.Invoke(en));
        _connection.On<RerollResultDto>("Reroll", r => RerollReceived?.Invoke(r));
        _connection.On<GoldUpdate>("Gold", g => GoldReceived?.Invoke(g));
        _connection.On<SelectionOffer>("Selection", o => SelectionReceived?.Invoke(o));
        _connection.On<TargetDetails>("TargetDetails", d => TargetDetailsReceived?.Invoke(d));
        _connection.On<PartyInviteDto>("PartyInvite", p => PartyInviteReceived?.Invoke(p));
        _connection.On<PartyUpdate>("Party", p => PartyReceived?.Invoke(p));
        _connection.On<PartyLootVoteDto>("PartyLootVote", p => PartyLootVoteReceived?.Invoke(p));
        _connection.On<AutoHuntStatus>("AutoHunt", s => AutoHuntReceived?.Invoke(s));
        _connection.On<AutoHuntConfigDto>("AutoConfig", c => AutoConfigReceived?.Invoke(c));
        _connection.On<SkillBarDto>("SkillBar", b => SkillBarReceived?.Invoke(b));
        _connection.On<SubclassListDto>("Subclasses", s => SubclassesReceived?.Invoke(s));
        _connection.On<LogoutResult>("LogoutResult", r => LogoutResultReceived?.Invoke(r));
        _connection.On<PvpState>("PvpState", s => PvpStateReceived?.Invoke(s));
        _connection.On<DebugConfigDto>("DebugConfig", c => DebugConfigReceived?.Invoke(c));
        _connection.On<ResurrectOffer>("ResurrectOffer", o => ResurrectOfferReceived?.Invoke(o));
        _connection.On<bool>("ResurrectOfferExpired", _ => ResurrectOfferExpired?.Invoke());
        _connection.On<string>("ForceDisconnect", reason => ForceDisconnected?.Invoke(reason));
        _connection.On<AdminStateDto>("AdminState", s => AdminStateReceived?.Invoke(s));
        _connection.On<AdminBagDto>("AdminBag", b => AdminBagReceived?.Invoke(b));
        _connection.On<AdminBagDto>("AdminGivePicker", b => AdminGivePickerReceived?.Invoke(b));
        _connection.Closed += ex =>
        {
            Disconnected?.Invoke(ex?.Message ?? "Connection closed.");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    // Pass our compiled-in version so the server can reject an out-of-date client (stale protocol).
    public Task<AuthResponse> RegisterAsync(string username, string password) =>
        _connection!.InvokeAsync<AuthResponse>("Register", new AuthRequest(username, password), GameConstants.GameVersion);

    public Task<AuthResponse> LoginAsync(string username, string password) =>
        _connection!.InvokeAsync<AuthResponse>("Login", new AuthRequest(username, password), GameConstants.GameVersion);

    public Task<CharacterList> ListCharactersAsync() =>
        _connection!.InvokeAsync<CharacterList>("ListCharacters");

    public Task<string?> CreateCharacterAsync(string name, Race race, BaseClass baseClass) =>
        _connection!.InvokeAsync<string?>("CreateCharacter",
            new CreateCharacterRequest(name, race, baseClass));

    public Task<LoginResult> EnterWorldAsync(int characterId) =>
        _connection!.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(characterId));

    public Task<string?> DeleteCharacterAsync(int characterId) =>
        _connection!.InvokeAsync<string?>("DeleteCharacter", characterId);

    public Task<string?> CancelDeleteCharacterAsync(int characterId) =>
        _connection!.InvokeAsync<string?>("CancelDeleteCharacter", characterId);

    public Task LeaveWorldAsync() =>
        _connection!.SendAsync("LeaveWorld");

    public Task AdminCommandAsync(string command, string argument) =>
        _connection!.SendAsync("AdminCommand", command, argument);

    /// <summary>Admin /give: hand one of my items to another online player.</summary>
    public Task AdminGiveItemAsync(string targetName, Guid instanceId, int quantity) =>
        _connection!.SendAsync("AdminGiveItem", targetName, instanceId, quantity);

    /// <summary>Admin /bag: destroy an item in another player's bag.</summary>
    public Task AdminRemoveItemAsync(string targetName, Guid instanceId) =>
        _connection!.SendAsync("AdminRemoveItem", targetName, instanceId);

    /// <summary>Friend list: action = "add" / "remove" / "list". Any player.</summary>
    public Task FriendCommandAsync(string action, string name) =>
        _connection!.SendAsync("FriendCommand", action, name);

    /// <summary>Follow a player (null = stop). Assist = attack whatever they're attacking.</summary>
    public Task FollowAsync(Guid? targetId) =>
        _connection!.SendAsync("Follow", targetId);

    public Task AssistAsync(Guid targetId) =>
        _connection!.SendAsync("Assist", targetId);

    public Task MoveAsync(float targetX, float targetY) =>
        _connection!.SendAsync("Move", new MoveCommand(targetX, targetY));

    public Task AttackAsync(Guid targetId) =>
        _connection!.SendAsync("Attack", targetId);

    public event Action<LearnedSkills>? LearnedReceived;
    public event Action<NpcDialog>? DialogReceived;
    public event Action<QuestLog>? QuestLogReceived;

    public Task UseSkillAsync(string skillId, Guid? targetId) =>
        _connection!.SendAsync("UseSkill", skillId, targetId);

    public Task LearnSkillAsync(string skillId) =>
        _connection!.SendAsync("LearnSkill", skillId);

    public Task TalkToNpcAsync(Guid npcEntityId) =>
        _connection!.SendAsync("TalkToNpc", npcEntityId);

    public Task QuestActionAsync(string action, string id, Guid npcEntityId) =>
        _connection!.SendAsync("QuestAction", action, id, npcEntityId);

    public Task BuyItemAsync(Guid npcEntityId, string itemDefId, int quantity) =>
        _connection!.SendAsync("BuyItem", npcEntityId, itemDefId, quantity);

    public Task SellItemAsync(Guid npcEntityId, Guid instanceId, int quantity) =>
        _connection!.SendAsync("SellItem", npcEntityId, instanceId, quantity);

    public Task TeleportAsync(Guid npcEntityId, string zoneId) =>
        _connection!.SendAsync("Teleport", npcEntityId, zoneId);

    public Task ForgetSkillAsync(Guid npcEntityId, string skillId) =>
        _connection!.SendAsync("ForgetSkill", npcEntityId, skillId);

    public Task BufferActionAsync(Guid npcEntityId, string action, string skillId) =>
        _connection!.SendAsync("BufferAction", npcEntityId, action, skillId);

    public Task SetMoveStateAsync(MoveState state) =>
        _connection!.SendAsync("SetMoveState", (int)state);

    public Task CancelCastAsync() =>
        _connection!.SendAsync("CancelCast");

    public Task RemoveBuffAsync(string buffKey) =>
        _connection!.SendAsync("RemoveBuff", buffKey);

    public Task OpenBoxAsync(Guid instanceId) =>
        _connection!.SendAsync("OpenBox", instanceId);

    public Task SelectBoxItemsAsync(Guid instanceId, string[] itemIds) =>
        _connection!.SendAsync("SelectBoxItems", instanceId, itemIds);

    /// <summary>Inspect the target. withDrops = also compute + send the mob's DROP list — only the
    /// [Details] click asks for that, so the 1s refresh loop never recomputes the static drop table.</summary>
    public Task InspectTargetAsync(Guid targetId, bool withDrops = false) =>
        _connection!.SendAsync("InspectTarget", targetId, withDrops);

    public Task RespawnAsync() =>
        _connection!.SendAsync("Respawn");

    public Task ChangeClassAsync(int classId) =>
        _connection!.SendAsync("ChangeClass", classId);

    public Task EquipItemAsync(Guid instanceId) =>
        _connection!.SendAsync("EquipItem", instanceId);

    public Task UsePotionAsync(Guid instanceId) =>
        _connection!.SendAsync("UsePotion", instanceId);

    /// <summary>Use a targeted consumable (a resurrection scroll) on a dead ally.</summary>
    public Task UsePotionOnAsync(Guid instanceId, Guid targetId) =>
        _connection!.SendAsync("UsePotionOn", instanceId, targetId);

    /// <summary>Answer a pending resurrection offer (true = revive, false = stay dead).</summary>
    public Task ResurrectResponseAsync(bool accept) =>
        _connection!.SendAsync("ResurrectResponse", accept);

    public Task EnchantAsync(Guid scrollId, Guid targetId) =>
        _connection!.SendAsync("Enchant", scrollId, targetId);

    public Task RerollAttributesAsync(Guid scrollId, Guid targetId, int[] lockedIndices) =>
        _connection!.SendAsync("RerollAttributes", scrollId, targetId, lockedIndices);

    public Task RemoveItemAsync(Guid instanceId, bool all = false) =>
        _connection!.SendAsync("RemoveItem", instanceId, all);

    public Task DebugGiveAsync(string defId, int quantity = 1) =>
        _connection!.SendAsync("DebugGive", defId, quantity);

    /// <summary>DEBUG: shift level by delta (+1 / +10 / -1 / -10). Delevel keeps learned skills.</summary>
    public Task DebugLevelAsync(int delta) =>
        _connection!.SendAsync("DebugLevel", delta);

    /// <summary>DEBUG: full NPC buff set on yourself, any level, no walk to the NPC.</summary>
    public Task DebugBuffAsync() =>
        _connection!.SendAsync("DebugBuff");

    public Task DebugKarmaAsync(int delta) =>
        _connection!.SendAsync("DebugKarma", delta);

    /// <summary>DEBUG: add a SUBCLASS (another class this character owns) and switch to it.</summary>
    public Task DebugAddSubclassAsync(int thirdClassId) =>
        _connection!.SendAsync("DebugAddSubclass", thirdClassId);

    /// <summary>Switch to a class this character already owns.</summary>
    public Task SwitchSubclassAsync(int slot) =>
        _connection!.SendAsync("SwitchSubclass", slot);

    public Task DebugLearnAllAsync() =>
        _connection!.SendAsync("DebugLearnAll");

    public Task DebugGoldAsync(long amount) =>
        _connection!.SendAsync("DebugGold", amount);

    public Task DebugSpAsync(long amount) =>
        _connection!.SendAsync("DebugSp", amount);

    public Task DebugResetAsync(Race race, BaseClass baseClass) =>
        _connection!.SendAsync("DebugReset", (int)race, (int)baseClass);

    public Task DebugThirdClassAsync(int thirdClassId) =>
        _connection!.SendAsync("DebugThirdClass", thirdClassId);

    public Task DebugTeleportAsync(float x, float y) =>
        _connection!.SendAsync("DebugTeleport", x, y);

    public Task PartyInviteAsync(Guid targetId) =>
        _connection!.SendAsync("PartyInvite", targetId);

    public Task PartyRespondAsync(bool accept) =>
        _connection!.SendAsync("PartyRespond", accept);

    public Task PartyLeaveAsync() =>
        _connection!.SendAsync("PartyLeave");

    public Task PartyKickAsync(Guid targetId) =>
        _connection!.SendAsync("PartyKick", targetId);

    public Task PartySetLootModeAsync(LootMode mode) =>
        _connection!.SendAsync("PartySetLootMode", mode);

    public Task PartyLootVoteAsync(bool accept) =>
        _connection!.SendAsync("PartyLootVote", accept);

    public Task SetAutoHuntConfigAsync(AutoHuntConfigDto config) =>
        _connection!.SendAsync("SetAutoHuntConfig", config);

    public Task SetSkillBarAsync(string[] slots) =>
        _connection!.SendAsync("SetSkillBar", slots);

    public Task ToggleAutoHuntAsync(bool enabled) =>
        _connection!.SendAsync("ToggleAutoHunt", enabled);

    public Task LogoutAsync() => _connection!.SendAsync("Logout");

    public Task StartOfflineFarmAsync() => _connection!.SendAsync("StartOfflineFarm");

    public Task TogglePvpAsync(bool enabled) => _connection!.SendAsync("TogglePvp", enabled);

    public Task ToggleCounterAttackAsync(bool enabled) => _connection!.SendAsync("ToggleCounterAttack", enabled);

    public Task RequestDebugConfigAsync() => _connection!.SendAsync("RequestDebugConfig");

    public Task SetDebugConfigAsync(DebugConfigDto config) => _connection!.SendAsync("SetDebugConfig", config);

    public Task TradeRequestAsync(Guid targetId) =>
        _connection!.SendAsync("TradeRequest", targetId);

    public Task TradeRespondAsync(bool accept) =>
        _connection!.SendAsync("TradeRespond", accept);

    public Task TradeOfferAsync(Guid[] instanceIds) =>
        _connection!.SendAsync("TradeOffer", instanceIds);

    public Task TradeGoldAsync(long gold) =>
        _connection!.SendAsync("TradeGold", gold);

    public Task TradeReadyAsync() =>
        _connection!.SendAsync("TradeReady");

    public Task TradeCancelAsync() =>
        _connection!.SendAsync("TradeCancel");

    public Task ChatAsync(string text, ChatChannel channel, string? whisperTarget = null) =>
        _connection!.SendAsync("Chat", text, channel, whisperTarget);

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
