using Game.Server.Hubs;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Simulation;

/// <summary>
/// The heart of the server: a fixed-tick loop (10 t/s).
/// Each tick: drain commands -> simulate -> broadcast snapshots.
/// </summary>
public class GameLoopService : BackgroundService
{
    private static readonly string[] MobNames =
        { "Wolf", "Boar", "Spider", "Bandit", "Slime" };

    private readonly World _world;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameLoopService> _log;
    private readonly Random _rng = new();

    public GameLoopService(World world, IHubContext<GameHub> hub, ILogger<GameLoopService> log)
    {
        _world = world;
        _hub = hub;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        SpawnMobs(15);
        _log.LogInformation("Game loop started at {Rate} ticks/sec", GameConstants.TickRate);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(GameConstants.TickSeconds));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                ProcessCommands();
                Simulate();
                await BroadcastSnapshotsAsync();
            }
            catch (Exception ex)
            {
                // One bad tick must never kill the world.
                _log.LogError(ex, "Unhandled error in game tick");
            }
        }
    }

    // -----------------------------------------------------------------------
    // 1. Commands (the only place hub input enters the simulation)
    // -----------------------------------------------------------------------

    private void ProcessCommands()
    {
        while (_world.Commands.TryDequeue(out var cmd))
        {
            switch (cmd)
            {
                case JoinCommand join: HandleJoin(join); break;
                case LeaveCommand leave: HandleLeave(leave); break;
                case MoveCmd move: HandleMove(move); break;
                case ChatCmd chat: HandleChat(chat); break;
            }
        }
    }

    private void HandleJoin(JoinCommand join)
    {
        var name = join.Request.CharacterName.Trim();

        if (name.Length is 0 or > GameConstants.MaxCharacterNameLength)
        {
            join.Result.TrySetResult(new LoginResult(false,
                $"Name must be 1-{GameConstants.MaxCharacterNameLength} characters.",
                Guid.Empty, 0, 0));
            return;
        }

        bool taken = _world.Entities.Values.Any(e =>
            e.Kind == EntityKind.Player &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

        if (taken)
        {
            join.Result.TrySetResult(new LoginResult(false,
                "That name is already online.", Guid.Empty, 0, 0));
            return;
        }

        var stats = StatCalculator.GetBaseStats(join.Request.Race, join.Request.BaseClass);
        var entity = new Entity
        {
            Name = name,
            Kind = EntityKind.Player,
            Race = join.Request.Race,
            BaseClass = join.Request.BaseClass,
            X = GameConstants.ZoneWidth / 2 + _rng.Next(-300, 300),
            Y = GameConstants.ZoneHeight / 2 + _rng.Next(-300, 300),
            Speed = GameConstants.BasePlayerSpeed,
            MaxHp = StatCalculator.MaxHp(stats.Con, 1),
            MaxMp = StatCalculator.MaxMp(stats.Wit, 1)
        };
        entity.Hp = entity.MaxHp;
        entity.Mp = entity.MaxMp;

        _world.Entities[entity.Id] = entity;
        _world.EntityToConnection[entity.Id] = join.ConnectionId;
        _world.ConnectionToEntity[join.ConnectionId] = entity.Id;
        _world.Grid.Add(entity);

        join.Result.TrySetResult(new LoginResult(true, null, entity.Id, entity.X, entity.Y));

        BroadcastSystem($"{entity.Name} entered the world.");
        _log.LogInformation("Player {Name} joined ({Race} {Class})",
            entity.Name, entity.Race, entity.BaseClass);
    }

    private void HandleLeave(LeaveCommand leave)
    {
        if (!_world.ConnectionToEntity.Remove(leave.ConnectionId, out var entityId))
            return;

        _world.EntityToConnection.Remove(entityId);

        if (_world.Entities.Remove(entityId, out var entity))
        {
            _world.Grid.Remove(entity);
            BroadcastSystem($"{entity.Name} left the world.");
            _log.LogInformation("Player {Name} left", entity.Name);
        }
    }

    private void HandleMove(MoveCmd move)
    {
        if (!_world.ConnectionToEntity.TryGetValue(move.ConnectionId, out var entityId) ||
            !_world.Entities.TryGetValue(entityId, out var entity))
            return;

        // Server-side validation: the destination is clamped to the zone.
        // (Speed is enforced implicitly — the entity moves at its own Speed,
        //  no matter what the client claims.)
        entity.TargetX = Math.Clamp(move.Move.TargetX, 0, GameConstants.ZoneWidth);
        entity.TargetY = Math.Clamp(move.Move.TargetY, 0, GameConstants.ZoneHeight);
    }

    private void HandleChat(ChatCmd chat)
    {
        if (!_world.ConnectionToEntity.TryGetValue(chat.ConnectionId, out var entityId) ||
            !_world.Entities.TryGetValue(entityId, out var sender))
            return;

        var text = chat.Text.Trim();
        if (text.Length is 0 or > 200)
            return;

        // Players cannot send System messages (that is for admins, later).
        var channel = chat.Channel == ChatChannel.System ? ChatChannel.Local : chat.Channel;
        var message = new ChatMessage(sender.Name, text, channel);

        if (channel == ChatChannel.World)
        {
            _ = _hub.Clients.All.SendAsync("Chat", message);
            return;
        }

        // Local: only players within view range hear it.
        foreach (var nearby in _world.Grid.Nearby(sender))
        {
            if (_world.EntityToConnection.TryGetValue(nearby.Id, out var conn))
                _ = _hub.Clients.Client(conn).SendAsync("Chat", message);
        }
    }

    private void BroadcastSystem(string text) =>
        _ = _hub.Clients.All.SendAsync("Chat", new ChatMessage("SYSTEM", text, ChatChannel.System));

    // -----------------------------------------------------------------------
    // 2. Simulation
    // -----------------------------------------------------------------------

    private void Simulate()
    {
        foreach (var entity in _world.Entities.Values)
        {
            if (entity.Kind == EntityKind.Mob)
                WanderAi(entity);

            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);
        }
    }

    private void WanderAi(Entity mob)
    {
        if (--mob.WanderTicks > 0)
            return;

        mob.WanderTicks = _rng.Next(30, 120); // next decision in 3-12s

        if (_rng.NextDouble() < 0.7)
        {
            mob.TargetX = Math.Clamp(mob.X + _rng.Next(-1500, 1501), 0, GameConstants.ZoneWidth);
            mob.TargetY = Math.Clamp(mob.Y + _rng.Next(-1500, 1501), 0, GameConstants.ZoneHeight);
        }
    }

    private static void MoveTowardTarget(Entity e)
    {
        if (e.TargetX is not float tx || e.TargetY is not float ty)
            return;

        float dx = tx - e.X;
        float dy = ty - e.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float step = e.Speed * GameConstants.TickSeconds;

        if (dist <= step)
        {
            e.X = tx;
            e.Y = ty;
            e.TargetX = null;
            e.TargetY = null;
        }
        else
        {
            e.X += dx / dist * step;
            e.Y += dy / dist * step;
        }
    }

    // -----------------------------------------------------------------------
    // 3. Broadcast — each player gets a personalized snapshot of what they see
    // -----------------------------------------------------------------------

    private async Task BroadcastSnapshotsAsync()
    {
        if (_world.EntityToConnection.Count == 0)
            return;

        var sends = new List<Task>(_world.EntityToConnection.Count);

        foreach (var (entityId, connectionId) in _world.EntityToConnection)
        {
            if (!_world.Entities.TryGetValue(entityId, out var player))
                continue;

            var visible = _world.Grid.Nearby(player)
                .Select(e => e.ToDto())
                .ToArray();

            sends.Add(_hub.Clients.Client(connectionId)
                .SendAsync("Snapshot", new WorldSnapshot(visible)));
        }

        try
        {
            await Task.WhenAll(sends);
        }
        catch
        {
            // A client that disconnected mid-send will throw; the
            // LeaveCommand from OnDisconnectedAsync cleans it up next tick.
        }
    }

    // -----------------------------------------------------------------------
    // Mob spawning
    // -----------------------------------------------------------------------

    private void SpawnMobs(int count)
    {
        float cx = GameConstants.ZoneWidth / 2;
        float cy = GameConstants.ZoneHeight / 2;

        for (int i = 0; i < count; i++)
        {
            int level = _rng.Next(1, 6);
            var mob = new Entity
            {
                Name = MobNames[_rng.Next(MobNames.Length)],
                Kind = EntityKind.Mob,
                X = Math.Clamp(cx + _rng.Next(-5000, 5001), 0, GameConstants.ZoneWidth),
                Y = Math.Clamp(cy + _rng.Next(-5000, 5001), 0, GameConstants.ZoneHeight),
                Speed = 120,
                Level = level,
                MaxHp = StatCalculator.MaxHp(20 + level * 2, level)
            };
            mob.Hp = mob.MaxHp;

            _world.Entities[mob.Id] = mob;
            _world.Grid.Add(mob);
        }
    }
}
