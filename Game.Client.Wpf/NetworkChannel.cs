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
    public event Action<ChatMessage>? ChatReceived;
    public event Action<CombatEvent>? CombatReceived;
    public event Action<ProgressUpdate>? ProgressReceived;
    public event Action<CastInfo>? CastReceived;
    public event Action<InventoryUpdate>? InventoryReceived;
    public event Action<TradeRequestNotice>? TradeRequestReceived;
    public event Action<TradeStateUpdate>? TradeStateReceived;
    public event Action<StatsUpdate>? StatsReceived;
    public event Action<PotionStatus>? PotionReceived;
    public event Action<BuffUpdate>? BuffsReceived;
    public event Action<EnchantResultDto>? EnchantReceived;
    public event Action<RerollResultDto>? RerollReceived;
    public event Action<GoldUpdate>? GoldReceived;
    public event Action<string>? Disconnected;
    public event Action<string>? ForceDisconnected;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string url)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<WorldSnapshot>("Snapshot", s => SnapshotReceived?.Invoke(s));
        _connection.On<ChatMessage>("Chat", m => ChatReceived?.Invoke(m));
        _connection.On<CombatEvent>("Combat", c => CombatReceived?.Invoke(c));
        _connection.On<ProgressUpdate>("Progress", p => ProgressReceived?.Invoke(p));
        _connection.On<CastInfo>("Cast", c => CastReceived?.Invoke(c));
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
        _connection.On<string>("ForceDisconnect", reason => ForceDisconnected?.Invoke(reason));
        _connection.Closed += ex =>
        {
            Disconnected?.Invoke(ex?.Message ?? "Connection closed.");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    public Task<AuthResponse> RegisterAsync(string username, string password) =>
        _connection!.InvokeAsync<AuthResponse>("Register", new AuthRequest(username, password));

    public Task<AuthResponse> LoginAsync(string username, string password) =>
        _connection!.InvokeAsync<AuthResponse>("Login", new AuthRequest(username, password));

    public Task<CharacterList> ListCharactersAsync() =>
        _connection!.InvokeAsync<CharacterList>("ListCharacters");

    public Task<string?> CreateCharacterAsync(string name, Race race, BaseClass baseClass) =>
        _connection!.InvokeAsync<string?>("CreateCharacter",
            new CreateCharacterRequest(name, race, baseClass));

    public Task<LoginResult> EnterWorldAsync(int characterId) =>
        _connection!.InvokeAsync<LoginResult>("EnterWorld", new EnterWorldRequest(characterId));

    public Task LeaveWorldAsync() =>
        _connection!.SendAsync("LeaveWorld");

    public Task AdminCommandAsync(string command, string argument) =>
        _connection!.SendAsync("AdminCommand", command, argument);

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

    public Task SetMoveStateAsync(MoveState state) =>
        _connection!.SendAsync("SetMoveState", (int)state);

    public Task CancelCastAsync() =>
        _connection!.SendAsync("CancelCast");

    public Task RespawnAsync() =>
        _connection!.SendAsync("Respawn");

    public Task ChangeClassAsync(int classId) =>
        _connection!.SendAsync("ChangeClass", classId);

    public Task EquipItemAsync(Guid instanceId) =>
        _connection!.SendAsync("EquipItem", instanceId);

    public Task UsePotionAsync(Guid instanceId) =>
        _connection!.SendAsync("UsePotion", instanceId);

    public Task EnchantAsync(Guid scrollId, Guid targetId) =>
        _connection!.SendAsync("Enchant", scrollId, targetId);

    public Task RerollAttributesAsync(Guid scrollId, Guid targetId, int[] lockedIndices) =>
        _connection!.SendAsync("RerollAttributes", scrollId, targetId, lockedIndices);

    public Task RemoveItemAsync(Guid instanceId) =>
        _connection!.SendAsync("RemoveItem", instanceId);

    public Task DebugGiveAsync(string defId, int quantity = 1) =>
        _connection!.SendAsync("DebugGive", defId, quantity);

    public Task DebugLevelAsync() =>
        _connection!.SendAsync("DebugLevel");

    public Task DebugGoldAsync(long amount) =>
        _connection!.SendAsync("DebugGold", amount);

    public Task TradeRequestAsync(Guid targetId) =>
        _connection!.SendAsync("TradeRequest", targetId);

    public Task TradeRespondAsync(bool accept) =>
        _connection!.SendAsync("TradeRespond", accept);

    public Task TradeOfferAsync(Guid[] instanceIds) =>
        _connection!.SendAsync("TradeOffer", instanceIds);

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
