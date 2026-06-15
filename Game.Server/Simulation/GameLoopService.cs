using Game.Server.Hubs;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Simulation;

/// <summary>
/// The heart of the server: a fixed-tick loop (10 t/s).
/// Each tick: drain commands -> simulate (AI, chase, combat, regen) ->
/// broadcast snapshots.
/// </summary>
public class GameLoopService : BackgroundService
{
    // Spiders and Bandits attack on sight; the rest only retaliate.
    private static readonly (string Name, bool Aggressive)[] MobTypes =
    {
        ("Wolf", false), ("Boar", false), ("Slime", false),
        ("Spider", true), ("Bandit", true)
    };

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
        SpawnMobs(20);
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

    // =========================================================================
    // 1. Commands (the only place hub input enters the simulation)
    // =========================================================================

    private void ProcessCommands()
    {
        while (_world.Commands.TryDequeue(out var cmd))
        {
            switch (cmd)
            {
                case JoinCommand join: HandleJoin(join); break;
                case LeaveCommand leave: HandleLeave(leave); break;
                case MoveCmd move: HandleMove(move); break;
                case AttackCmd attack: HandleAttack(attack); break;
                case RespawnCmd respawn: HandleRespawn(respawn); break;
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
            Con = stats.Con,
            AtkStat = stats.Atk,
            Wit = stats.Wit,
            Dex = stats.Dex
        };
        entity.RecomputeDerived();
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
            if (!entity.Dead)
                _world.Grid.Remove(entity);
            BroadcastSystem($"{entity.Name} left the world.");
            _log.LogInformation("Player {Name} left", entity.Name);
        }
    }

    private void HandleMove(MoveCmd move)
    {
        if (!TryGetPlayer(move.ConnectionId, out var entity) || entity.Dead)
            return;

        // Clicking the ground cancels the current engagement (classic MMO).
        entity.Engaged = false;
        entity.CombatTargetId = null;

        // Server-side validation: destination clamped to the zone. Speed is
        // enforced implicitly — the entity moves at its own Speed regardless
        // of what the client claims.
        entity.TargetX = Math.Clamp(move.Move.TargetX, 0, GameConstants.ZoneWidth);
        entity.TargetY = Math.Clamp(move.Move.TargetY, 0, GameConstants.ZoneHeight);
    }

    private void HandleAttack(AttackCmd attack)
    {
        if (!TryGetPlayer(attack.ConnectionId, out var attacker) || attacker.Dead)
            return;

        if (attack.TargetId == attacker.Id ||
            !_world.Entities.TryGetValue(attack.TargetId, out var target) ||
            target.Dead)
            return;

        // Target must be within view range to engage.
        if (DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
            return;

        attacker.CombatTargetId = target.Id;
        attacker.Engaged = true;
    }

    private void HandleRespawn(RespawnCmd respawn)
    {
        if (!TryGetPlayer(respawn.ConnectionId, out var entity) || !entity.Dead)
            return;

        entity.Dead = false;
        entity.Hp = entity.MaxHp;
        entity.Mp = entity.MaxMp;
        entity.X = GameConstants.ZoneWidth / 2 + _rng.Next(-300, 300);
        entity.Y = GameConstants.ZoneHeight / 2 + _rng.Next(-300, 300);
        entity.TargetX = null;
        entity.TargetY = null;
        _world.Grid.UpdatePosition(entity);
    }

    private void HandleChat(ChatCmd chat)
    {
        if (!TryGetPlayer(chat.ConnectionId, out var sender))
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

    // =========================================================================
    // 2. Simulation
    // =========================================================================

    private int _tick;

    private void Simulate()
    {
        _tick++;
        bool regenTick = _tick % GameConstants.RegenIntervalTicks == 0;

        foreach (var entity in _world.Entities.Values)
        {
            if (entity.AttackCooldown > 0)
                entity.AttackCooldown--;

            if (entity.Dead)
            {
                if (entity.Kind == EntityKind.Mob && --entity.RespawnTicks <= 0)
                    RespawnMob(entity);
                continue;
            }

            if (entity.Kind == EntityKind.Mob)
                MobAi(entity);

            UpdateCombat(entity);
            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);

            if (regenTick)
                Regenerate(entity);
        }
    }

    // ----- Mob AI --------------------------------------------------------------

    private void MobAi(Entity mob)
    {
        if (mob.Engaged)
        {
            // Leash: chased too far from home -> reset, walk back, heal full.
            float dx = mob.X - mob.HomeX;
            float dy = mob.Y - mob.HomeY;
            if (dx * dx + dy * dy > GameConstants.MobLeashRange * GameConstants.MobLeashRange)
                ResetMob(mob);
            return;
        }

        if (--mob.WanderTicks > 0)
            return;

        mob.WanderTicks = _rng.Next(30, 120); // next decision in 3-12s

        // Aggressive mobs look for prey at each decision point.
        if (mob.Aggressive)
        {
            foreach (var candidate in _world.Grid.Nearby(mob))
            {
                if (candidate.Kind != EntityKind.Player || candidate.Dead)
                    continue;

                if (DistanceSq(mob, candidate) <=
                    GameConstants.MobAggroRange * GameConstants.MobAggroRange)
                {
                    mob.CombatTargetId = candidate.Id;
                    mob.Engaged = true;
                    return;
                }
            }
        }

        if (_rng.NextDouble() < 0.7)
        {
            mob.TargetX = Math.Clamp(mob.HomeX + _rng.Next(-1500, 1501), 0, GameConstants.ZoneWidth);
            mob.TargetY = Math.Clamp(mob.HomeY + _rng.Next(-1500, 1501), 0, GameConstants.ZoneHeight);
        }
    }

    private void ResetMob(Entity mob)
    {
        mob.Engaged = false;
        mob.CombatTargetId = null;
        mob.Hp = mob.MaxHp;
        mob.TargetX = mob.HomeX;
        mob.TargetY = mob.HomeY;
    }

    private void RespawnMob(Entity mob)
    {
        mob.Dead = false;
        mob.Hp = mob.MaxHp;
        mob.X = mob.HomeX;
        mob.Y = mob.HomeY;
        mob.TargetX = null;
        mob.TargetY = null;
        _world.Grid.Add(mob);
    }

    // ----- Combat -----------------------------------------------------------------

    /// <summary>L2-style engagement: run into range, then auto-attack on cooldown.
    /// The *intent* always reaches the target (lag-friendly); the stats decide
    /// whether it misses, crits, or lands.</summary>
    private void UpdateCombat(Entity attacker)
    {
        if (!attacker.Engaged || attacker.CombatTargetId is not Guid targetId)
            return;

        if (!_world.Entities.TryGetValue(targetId, out var target) ||
            target.Dead ||
            DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
        {
            attacker.Engaged = false;
            attacker.CombatTargetId = null;
            if (attacker.Kind == EntityKind.Mob)
                ResetMob(attacker);
            return;
        }

        if (DistanceSq(attacker, target) > GameConstants.MeleeRange * GameConstants.MeleeRange)
        {
            // Chase: re-aim at the target's current position every tick.
            attacker.TargetX = target.X;
            attacker.TargetY = target.Y;
            return;
        }

        // In range: stand and fight.
        attacker.TargetX = null;
        attacker.TargetY = null;

        if (attacker.AttackCooldown > 0)
            return;

        attacker.AttackCooldown = attacker.Kind == EntityKind.Player
            ? GameConstants.PlayerAttackIntervalTicks
            : GameConstants.MobAttackIntervalTicks;

        ResolveBasicAttack(attacker, target);
    }

    private void ResolveBasicAttack(Entity attacker, Entity target)
    {
        float missChance = StatCalculator.MissChance(attacker.Accuracy, target.Evasion);

        CombatOutcome outcome;
        int damage = 0;

        if (_rng.NextDouble() < missChance)
        {
            outcome = CombatOutcome.Miss;
        }
        else
        {
            damage = StatCalculator.BasicAttackDamage(attacker.AttackPower, target.Defence);
            if (_rng.NextDouble() < attacker.CritChance)
            {
                damage = (int)(damage * StatCalculator.CritMultiplier);
                outcome = CombatOutcome.Crit;
            }
            else
            {
                outcome = CombatOutcome.Hit;
            }

            target.Hp -= damage;
        }

        BroadcastCombat(attacker, target, damage, outcome);

        // Mobs retaliate when attacked (even on a miss).
        if (target.Kind == EntityKind.Mob && !target.Engaged && !target.Dead)
        {
            target.CombatTargetId = attacker.Id;
            target.Engaged = true;
        }

        if (target.Hp <= 0)
            Kill(target, attacker);
    }

    private void Kill(Entity victim, Entity killer)
    {
        victim.Hp = 0;
        victim.Dead = true;
        victim.Engaged = false;
        victim.CombatTargetId = null;
        victim.TargetX = null;
        victim.TargetY = null;

        // Killer disengages from a dead target automatically next tick.
        BroadcastCombat(killer, victim, 0, CombatOutcome.Death);

        if (victim.Kind == EntityKind.Mob)
        {
            // Corpse disappears: out of the grid until respawn.
            _world.Grid.Remove(victim);
            victim.RespawnTicks = GameConstants.MobRespawnTicks;

            if (killer.Kind == EntityKind.Player)
                AwardExp(killer, StatCalculator.MobExpReward(victim.Level));
        }
        else
        {
            // Player corpse stays visible where it fell.
            BroadcastSystem($"{victim.Name} was slain by {killer.Name}.");
        }
    }

    private void AwardExp(Entity player, int amount)
    {
        player.Exp += amount;

        bool leveled = false;
        while (player.Exp >= StatCalculator.ExpToNext(player.Level))
        {
            player.Exp -= StatCalculator.ExpToNext(player.Level);
            player.Level++;
            leveled = true;
        }

        if (leveled)
        {
            player.RecomputeDerived();
            player.Hp = player.MaxHp;   // level-up heals — feels great, costs nothing
            player.Mp = player.MaxMp;
            BroadcastSystem($"{player.Name} reached level {player.Level}!");
        }

        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), leveled));
        }
    }

    private void Regenerate(Entity entity)
    {
        if (entity.Engaged)
            return; // out-of-combat regen only

        if (entity.Hp < entity.MaxHp)
        {
            int regen = Math.Max(1,
                (int)StatCalculator.HpRegenPerSecond(entity.Con, entity.Level));
            entity.Hp = Math.Min(entity.MaxHp, entity.Hp + regen);
        }

        if (entity.Mp < entity.MaxMp)
        {
            int regen = Math.Max(1,
                (int)StatCalculator.MpRegenPerSecond(entity.Wit, entity.Level));
            entity.Mp = Math.Min(entity.MaxMp, entity.Mp + regen);
        }
    }

    // ----- Movement --------------------------------------------------------------

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

    // =========================================================================
    // 3. Broadcast — each player gets a personalized snapshot of what they see
    // =========================================================================

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
                .Where(e => !e.Dead || e.Kind == EntityKind.Player) // player corpses visible
                .Select(e => e.ToDto())
                .ToList();

            // A dead player was removed from no grid — but make sure the
            // viewer always sees themselves even in edge cases.
            if (!visible.Any(d => d.Id == player.Id))
                visible.Add(player.ToDto());

            sends.Add(_hub.Clients.Client(connectionId)
                .SendAsync("Snapshot", new WorldSnapshot(visible.ToArray())));
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

    private void BroadcastCombat(Entity attacker, Entity target, int damage, CombatOutcome outcome)
    {
        var evt = new CombatEvent(
            attacker.Id, attacker.Name, target.Id, target.Name, damage, outcome);

        foreach (var nearby in _world.Grid.Nearby(attacker))
        {
            if (_world.EntityToConnection.TryGetValue(nearby.Id, out var conn))
                _ = _hub.Clients.Client(conn).SendAsync("Combat", evt);
        }
    }

    private void BroadcastSystem(string text) =>
        _ = _hub.Clients.All.SendAsync("Chat", new ChatMessage("SYSTEM", text, ChatChannel.System));

    // =========================================================================
    // Helpers / spawning
    // =========================================================================

    private bool TryGetPlayer(string connectionId, out Entity entity)
    {
        entity = null!;
        return _world.ConnectionToEntity.TryGetValue(connectionId, out var id) &&
               _world.Entities.TryGetValue(id, out entity!);
    }

    private static float DistanceSq(Entity a, Entity b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private void SpawnMobs(int count)
    {
        float cx = GameConstants.ZoneWidth / 2;
        float cy = GameConstants.ZoneHeight / 2;

        for (int i = 0; i < count; i++)
        {
            var (name, aggressive) = MobTypes[_rng.Next(MobTypes.Length)];
            int level = _rng.Next(1, 6);
            var stats = StatCalculator.MobStats(level);

            var mob = new Entity
            {
                Name = name,
                Kind = EntityKind.Mob,
                X = Math.Clamp(cx + _rng.Next(-5000, 5001), 0, GameConstants.ZoneWidth),
                Y = Math.Clamp(cy + _rng.Next(-5000, 5001), 0, GameConstants.ZoneHeight),
                Speed = 160,
                Level = level,
                Con = stats.Con,
                AtkStat = stats.Atk,
                Wit = stats.Wit,
                Dex = stats.Dex,
                Aggressive = aggressive
            };
            mob.RecomputeDerived();
            mob.Hp = mob.MaxHp;
            mob.Mp = mob.MaxMp;
            mob.HomeX = mob.X;
            mob.HomeY = mob.Y;

            _world.Entities[mob.Id] = mob;
            _world.Grid.Add(mob);
        }
    }
}
