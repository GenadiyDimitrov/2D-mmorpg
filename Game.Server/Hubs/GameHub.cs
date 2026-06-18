using System.Collections.Concurrent;
using Game.Server.Persistence;
using Game.Server.Simulation;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Hubs;

/// <summary>
/// Connection layer. Auth + character selection happen here (async DB I/O,
/// off the game loop). Once a player enters the world, gameplay calls become
/// commands on the world queue, exactly as before.
/// </summary>
public class GameHub : Hub
{
    private readonly World _world;
    private readonly PersistenceService _db;

    // Connection -> authenticated account (set after login/register).
    private static readonly ConcurrentDictionary<string, AuthState> Sessions = new();

    private record AuthState(int AccountId, bool IsAdmin);

    public GameHub(World world, PersistenceService db)
    {
        _world = world;
        _db = db;
    }

    // ----- Auth --------------------------------------------------------------

    public async Task<AuthResponse> Register(AuthRequest request)
    {
        var result = await _db.RegisterAsync(request.Username, request.Password);
        if (result.Success)
            Sessions[Context.ConnectionId] = new AuthState(result.AccountId, result.IsAdmin);
        return new AuthResponse(result.Success, result.Error, result.IsAdmin);
    }

    public async Task<AuthResponse> Login(AuthRequest request)
    {
        var result = await _db.LoginAsync(request.Username, request.Password);
        if (result.Success)
            Sessions[Context.ConnectionId] = new AuthState(result.AccountId, result.IsAdmin);
        return new AuthResponse(result.Success, result.Error, result.IsAdmin);
    }

    // ----- Character selection ----------------------------------------------

    public async Task<CharacterList> ListCharacters()
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return new CharacterList(Array.Empty<CharacterSlot>());

        var chars = await _db.ListCharactersAsync(auth.AccountId);
        return new CharacterList(chars
            .Select(c => new CharacterSlot(c.Id, c.Name, c.Race, c.BaseClass, c.SecondClass, c.Level))
            .ToArray());
    }

    public async Task<string?> CreateCharacter(CreateCharacterRequest request)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return "Not logged in.";

        var (success, error) = await _db.CreateCharacterAsync(
            auth.AccountId, request.Name, request.Race, request.BaseClass);
        return success ? null : error;
    }

    /// <summary>Load a character from the DB and hand it to the game loop.</summary>
    public async Task<LoginResult> EnterWorld(EnterWorldRequest request)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return new LoginResult(false, "Not logged in.", Guid.Empty, 0, 0);

        // Reject if this character is already in the world.
        if (_world.Entities.Values.Any(e =>
                e.Kind == EntityKind.Player && e.PersistentId == request.CharacterId))
            return new LoginResult(false, "That character is already online.", Guid.Empty, 0, 0);

        var entity = await _db.LoadCharacterAsync(auth.AccountId, request.CharacterId);
        if (entity is null)
            return new LoginResult(false, "Character not found.", Guid.Empty, 0, 0);

        entity.IsAdmin = auth.IsAdmin;

        var tcs = new TaskCompletionSource<LoginResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _world.Commands.Enqueue(new EnterWorldCommand(Context.ConnectionId, entity, tcs));

        var timeout = Task.Delay(TimeSpan.FromSeconds(5));
        var finished = await Task.WhenAny(tcs.Task, timeout);
        return finished == tcs.Task
            ? await tcs.Task
            : new LoginResult(false, "Server busy, try again.", Guid.Empty, 0, 0);
    }

    // ----- Gameplay (unchanged: enqueue commands) ----------------------------

    public Task Move(MoveCommand command)
    {
        _world.Commands.Enqueue(new MoveCmd(Context.ConnectionId, command));
        return Task.CompletedTask;
    }

    public Task Attack(Guid targetId)
    {
        _world.Commands.Enqueue(new AttackCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task UseSkill(string skillId, Guid? targetId)
    {
        _world.Commands.Enqueue(new SkillCmd(Context.ConnectionId, skillId, targetId));
        return Task.CompletedTask;
    }

    public Task LearnSkill(string skillId)
    {
        _world.Commands.Enqueue(new LearnSkillCmd(Context.ConnectionId, skillId));
        return Task.CompletedTask;
    }

    public Task TalkToNpc(Guid npcEntityId)
    {
        _world.Commands.Enqueue(new TalkCmd(Context.ConnectionId, npcEntityId));
        return Task.CompletedTask;
    }

    public Task QuestAction(string action, string id, Guid npcEntityId)
    {
        _world.Commands.Enqueue(new QuestActionCmd(Context.ConnectionId, action, id, npcEntityId));
        return Task.CompletedTask;
    }

    public Task SetMoveState(int state)
    {
        _world.Commands.Enqueue(new SetMoveStateCmd(Context.ConnectionId, (MoveState)state));
        return Task.CompletedTask;
    }

    public Task CancelCast()
    {
        _world.Commands.Enqueue(new CancelCastCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task Respawn()
    {
        _world.Commands.Enqueue(new RespawnCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task ChangeClass(int classId)
    {
        _world.Commands.Enqueue(new ClassChangeCmd(Context.ConnectionId, classId));
        return Task.CompletedTask;
    }

    public Task EquipItem(Guid instanceId)
    {
        _world.Commands.Enqueue(new EquipCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task UsePotion(Guid instanceId)
    {
        _world.Commands.Enqueue(new UsePotionCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task Enchant(Guid scrollInstanceId, Guid targetInstanceId)
    {
        _world.Commands.Enqueue(new EnchantCmd(Context.ConnectionId, scrollInstanceId, targetInstanceId));
        return Task.CompletedTask;
    }

    public Task RemoveItem(Guid instanceId)
    {
        _world.Commands.Enqueue(new RemoveItemCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task TradeRequest(Guid targetId)
    {
        _world.Commands.Enqueue(new TradeRequestCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task TradeRespond(bool accept)
    {
        _world.Commands.Enqueue(new TradeRespondCmd(Context.ConnectionId, accept));
        return Task.CompletedTask;
    }

    public Task TradeOffer(Guid[] instanceIds)
    {
        _world.Commands.Enqueue(new TradeOfferCmd(Context.ConnectionId, instanceIds));
        return Task.CompletedTask;
    }

    public Task TradeReady()
    {
        _world.Commands.Enqueue(new TradeReadyCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task TradeCancel()
    {
        _world.Commands.Enqueue(new TradeCancelCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task Chat(string text, ChatChannel channel, string? whisperTarget)
    {
        _world.Commands.Enqueue(new ChatCmd(Context.ConnectionId, text, channel, whisperTarget));
        return Task.CompletedTask;
    }

    // ----- Admin commands ----------------------------------------------------

    public Task AdminCommand(string command, string argument)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth) || !auth.IsAdmin)
            return Task.CompletedTask;
        _world.Commands.Enqueue(new AdminCmd(Context.ConnectionId, command, argument));
        return Task.CompletedTask;
    }

    // ----- Debug (DEBUG builds only) -----------------------------------------

    public Task DebugGive(string defId, int quantity)
    {
#if DEBUG
        _world.Commands.Enqueue(new DebugGiveCmd(Context.ConnectionId, defId));
        // Extra copies for stackables (debug convenience: 10 potions/scrolls).
        for (int i = 1; i < Math.Max(1, quantity); i++)
            _world.Commands.Enqueue(new DebugGiveCmd(Context.ConnectionId, defId));
#endif
        return Task.CompletedTask;
    }

    public Task DebugLevel()
    {
#if DEBUG
        _world.Commands.Enqueue(new DebugLevelCmd(Context.ConnectionId));
#endif
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Sessions.TryRemove(Context.ConnectionId, out _);
        _world.Commands.Enqueue(new LeaveCommand(Context.ConnectionId));
        return base.OnDisconnectedAsync(exception);
    }
}
