using Game.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Game.Client.Wpf;

/// <summary>
/// The single seam between game code and the wire. Today it wraps SignalR;
/// if the transport ever changes (LiteNetLib, raw TCP), only this file does.
/// The same class drops into the Unity client later — it has no WPF
/// dependencies at all.
/// </summary>
public class NetworkChannel : IAsyncDisposable
{
    private HubConnection? _connection;

    public event Action<WorldSnapshot>? SnapshotReceived;
    public event Action<ChatMessage>? ChatReceived;
    public event Action<CombatEvent>? CombatReceived;
    public event Action<ProgressUpdate>? ProgressReceived;
    public event Action<CastInfo>? CastReceived;
    public event Action<string>? Disconnected;

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
        _connection.Closed += ex =>
        {
            Disconnected?.Invoke(ex?.Message ?? "Connection closed.");
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
    }

    public Task<LoginResult> LoginAsync(LoginRequest request) =>
        _connection!.InvokeAsync<LoginResult>("Login", request);

    public Task MoveAsync(float targetX, float targetY) =>
        _connection!.SendAsync("Move", new MoveCommand(targetX, targetY));

    public Task AttackAsync(Guid targetId) =>
        _connection!.SendAsync("Attack", targetId);

    public Task UseSkillAsync(int skillId, Guid? targetId) =>
        _connection!.SendAsync("UseSkill", skillId, targetId);

    public Task RespawnAsync() =>
        _connection!.SendAsync("Respawn");

    public Task ChatAsync(string text, ChatChannel channel, string? whisperTarget = null) =>
        _connection!.SendAsync("Chat", text, channel, whisperTarget);

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
