using System.Collections.Concurrent;
using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A live trade between two players. Owned by the loop thread.</summary>
public class TradeSession
{
    public required Entity A { get; init; }
    public required Entity B { get; init; }
    public List<Guid> OfferA { get; } = new();
    public List<Guid> OfferB { get; } = new();
    public bool ReadyA { get; set; }
    public bool ReadyB { get; set; }

    public Entity PartnerOf(Entity e) => e == A ? B : A;
    public List<Guid> OfferOf(Entity e) => e == A ? OfferA : OfferB;
    public bool ReadyOf(Entity e) => e == A ? ReadyA : ReadyB;
    public void SetReady(Entity e, bool value) { if (e == A) ReadyA = value; else ReadyB = value; }
}

/// <summary>
/// All live game state. The SignalR hub never touches the dictionaries
/// directly — it only enqueues commands. The game loop drains the queue,
/// so every mutation happens on a single thread. One writer, zero locks.
/// </summary>
public class World
{
    public ConcurrentQueue<IGameCommand> Commands { get; } = new();

    // Everything below is owned by the game-loop thread.

    public Dictionary<Guid, Entity> Entities { get; } = new();
    public Dictionary<Guid, string> EntityToConnection { get; } = new();
    public Dictionary<string, Guid> ConnectionToEntity { get; } = new();

    /// <summary>Both participants map to the same session.</summary>
    public Dictionary<Guid, TradeSession> ActiveTrades { get; } = new();

    /// <summary>targetEntityId -> requesterEntityId (one pending request each).</summary>
    public Dictionary<Guid, Guid> PendingTradeRequests { get; } = new();

    public CellGrid Grid { get; } = new(
        GameConstants.ZoneWidth, GameConstants.ZoneHeight, GameConstants.CellSize);
}

// ---------------------------------------------------------------------------
// Commands
// ---------------------------------------------------------------------------

public interface IGameCommand { }

public record EnterWorldCommand(
    string ConnectionId,
    Entity Entity,
    TaskCompletionSource<LoginResult> Result) : IGameCommand;

public record LeaveCommand(string ConnectionId) : IGameCommand;

public record MoveCmd(string ConnectionId, MoveCommand Move) : IGameCommand;

public record ChatCmd(
    string ConnectionId,
    string Text,
    ChatChannel Channel,
    string? WhisperTarget = null) : IGameCommand;

public record AttackCmd(string ConnectionId, Guid TargetId) : IGameCommand;

public record SkillCmd(string ConnectionId, int SkillId, Guid? TargetId) : IGameCommand;

public record RespawnCmd(string ConnectionId) : IGameCommand;

/// <summary>Advance to a second class (level 20+, once).</summary>
public record ClassChangeCmd(string ConnectionId, int ClassId) : IGameCommand;

/// <summary>Equip or unequip an inventory item (toggles).</summary>
public record EquipCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Drink a potion from the inventory.</summary>
public record UsePotionCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Apply an enchant scroll to a target item.</summary>
public record EnchantCmd(string ConnectionId, Guid ScrollInstanceId, Guid TargetInstanceId) : IGameCommand;

/// <summary>Destroy an inventory item (later: sell/dismantle).</summary>
public record RemoveItemCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>DEBUG-only: grant an item by def id.</summary>
public record DebugGiveCmd(string ConnectionId, int DefId) : IGameCommand;

/// <summary>DEBUG-only: grant one level.</summary>
public record DebugLevelCmd(string ConnectionId) : IGameCommand;

/// <summary>Admin command (kick/ban/jail/unjail/god). Validated in the hub.</summary>
public record AdminCmd(string ConnectionId, string Command, string Argument) : IGameCommand;

public record TradeRequestCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record TradeRespondCmd(string ConnectionId, bool Accept) : IGameCommand;
public record TradeOfferCmd(string ConnectionId, Guid[] InstanceIds) : IGameCommand;
public record TradeReadyCmd(string ConnectionId) : IGameCommand;
public record TradeCancelCmd(string ConnectionId) : IGameCommand;
