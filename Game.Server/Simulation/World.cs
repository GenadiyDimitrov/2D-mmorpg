using System.Collections.Concurrent;
using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// All live game state. The SignalR hub never touches the dictionaries
/// directly — it only enqueues commands. The game loop drains the queue,
/// so every mutation happens on a single thread. That is the whole
/// concurrency model: one writer, zero locks.
/// </summary>
public class World
{
    /// <summary>Commands from hub threads to the simulation thread.</summary>
    public ConcurrentQueue<IGameCommand> Commands { get; } = new();

    // Everything below is owned by the game-loop thread.

    public Dictionary<Guid, Entity> Entities { get; } = new();

    /// <summary>Player entity id -> SignalR connection id (for sending snapshots).</summary>
    public Dictionary<Guid, string> EntityToConnection { get; } = new();

    /// <summary>SignalR connection id -> player entity id (for handling commands).</summary>
    public Dictionary<string, Guid> ConnectionToEntity { get; } = new();

    public CellGrid Grid { get; } = new(
        GameConstants.ZoneWidth, GameConstants.ZoneHeight, GameConstants.CellSize);
}

// ---------------------------------------------------------------------------
// Commands
// ---------------------------------------------------------------------------

public interface IGameCommand { }

/// <summary>Player wants to enter the world. The hub awaits the
/// TaskCompletionSource so login feels like a normal request/response
/// even though it is processed inside the tick loop.</summary>
public record JoinCommand(
    string ConnectionId,
    LoginRequest Request,
    TaskCompletionSource<LoginResult> Result) : IGameCommand;

public record LeaveCommand(string ConnectionId) : IGameCommand;

public record MoveCmd(string ConnectionId, MoveCommand Move) : IGameCommand;

public record ChatCmd(string ConnectionId, string Text, ChatChannel Channel) : IGameCommand;
