using Game.Server.Simulation;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Hubs;

/// <summary>
/// Thin connection layer. No game logic lives here — every call becomes a
/// command on the world queue and is executed by the game-loop thread.
/// </summary>
public class GameHub : Hub
{
    private readonly World _world;

    public GameHub(World world) => _world = world;

    /// <summary>Enter the world. Awaits the simulation thread's answer so the
    /// client gets a normal request/response experience.</summary>
    public async Task<LoginResult> Login(LoginRequest request)
    {
        var tcs = new TaskCompletionSource<LoginResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        _world.Commands.Enqueue(new JoinCommand(Context.ConnectionId, request, tcs));

        // The loop runs at 10 t/s, so this resolves within ~100 ms.
        var timeout = Task.Delay(TimeSpan.FromSeconds(5));
        var finished = await Task.WhenAny(tcs.Task, timeout);

        return finished == tcs.Task
            ? await tcs.Task
            : new LoginResult(false, "Server busy, try again.", Guid.Empty, 0, 0);
    }

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

    public Task Respawn()
    {
        _world.Commands.Enqueue(new RespawnCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task Chat(string text, ChatChannel channel)
    {
        _world.Commands.Enqueue(new ChatCmd(Context.ConnectionId, text, channel));
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _world.Commands.Enqueue(new LeaveCommand(Context.ConnectionId));
        return base.OnDisconnectedAsync(exception);
    }
}
