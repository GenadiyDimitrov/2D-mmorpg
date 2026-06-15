using Game.Server.Hubs;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Simulation;

/// <summary>
/// The heart of the server: a fixed-tick loop (10 t/s).
/// Each tick: drain commands -> simulate (AI, skills, combat, regen) ->
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

    // Level-banded hunting grounds: rings around the town (safe zone).
    // A mob's level is decided by where its home is — and leashing keeps it
    // there, so a lvl-15 Bandit never wanders into the lvl 1-3 ring.
    private static readonly (float MinDist, float MaxDist, int MinLvl, int MaxLvl, int Count)[] SpawnBands =
    {
        (1300f, 3500f, 1, 3, 14),
        (3500f, 6000f, 4, 7, 12),
        (6000f, 8500f, 8, 12, 10),
        (8500f, 10500f, 13, 18, 10),
    };

    private readonly World _world;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameLoopService> _log;
    private readonly Random _rng = new();
    private int _tick;

    public GameLoopService(World world, IHubContext<GameHub> hub, ILogger<GameLoopService> log)
    {
        _world = world;
        _hub = hub;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        SpawnMobs();
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
                case SkillCmd skill: HandleSkill(skill); break;
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

        // Clicking the ground cancels engagement, queued skills and casting.
        entity.Engaged = false;
        entity.CombatTargetId = null;
        entity.QueuedSkillId = null;
        CancelCast(entity, move.ConnectionId);

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

        if (DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
            return;

        attacker.QueuedSkillId = null;
        CancelCast(attacker, attack.ConnectionId);
        attacker.CombatTargetId = target.Id;
        attacker.Engaged = true;
    }

    private void HandleSkill(SkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var caster) || caster.Dead)
            return;

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null || def.Class != caster.BaseClass)
            return;

        if (caster.SkillCooldowns.TryGetValue(def.Id, out int cd) && cd > 0)
        {
            SendSystemTo(cmd.ConnectionId, $"{def.Name} is not ready.");
            return;
        }

        if (caster.Mp < def.MpCost)
        {
            SendSystemTo(cmd.ConnectionId, "Not enough MP.");
            return;
        }

        bool offensive = def.Effect is SkillEffect.PhysicalDamage
            or SkillEffect.MagicDamage or SkillEffect.DebuffDef;

        Guid targetId;
        if (offensive)
        {
            if (cmd.TargetId is not Guid tid ||
                tid == caster.Id ||
                !_world.Entities.TryGetValue(tid, out var target) ||
                target.Dead ||
                DistanceSq(caster, target) > GameConstants.ViewRange * GameConstants.ViewRange)
            {
                SendSystemTo(cmd.ConnectionId, $"{def.Name} needs a target.");
                return;
            }
            targetId = tid;
        }
        else
        {
            targetId = caster.Id; // self-targeted (Heal, War Cry)
        }

        CancelCast(caster, cmd.ConnectionId);
        caster.QueuedSkillId = def.Id;
        caster.QueuedTargetId = targetId;
        // Engaged auto-attack resumes after the skill (set on execution).
    }

    private void HandleRespawn(RespawnCmd respawn)
    {
        if (!TryGetPlayer(respawn.ConnectionId, out var entity) || !entity.Dead)
            return;

        entity.Dead = false;
        entity.Hp = entity.MaxHp;
        entity.Mp = entity.MaxMp;
        entity.Buffs.Clear();
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

        if (channel == ChatChannel.Whisper)
        {
            var targetName = chat.WhisperTarget?.Trim();
            if (string.IsNullOrEmpty(targetName))
                return;

            var target = _world.Entities.Values.FirstOrDefault(e =>
                e.Kind == EntityKind.Player &&
                string.Equals(e.Name, targetName, StringComparison.OrdinalIgnoreCase));

            if (target is null || !_world.EntityToConnection.TryGetValue(target.Id, out var targetConn))
            {
                SendSystemTo(chat.ConnectionId, $"{targetName} is not online.");
                return;
            }

            var whisper = new ChatMessage(sender.Name, text, ChatChannel.Whisper, target.Name);
            _ = _hub.Clients.Client(targetConn).SendAsync("Chat", whisper);
            _ = _hub.Clients.Client(chat.ConnectionId).SendAsync("Chat", whisper); // echo
            return;
        }

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

    private void Simulate()
    {
        _tick++;
        bool regenTick = _tick % GameConstants.RegenIntervalTicks == 0;

        foreach (var entity in _world.Entities.Values)
        {
            if (entity.AttackCooldown > 0)
                entity.AttackCooldown--;

            TickSkillCooldowns(entity);
            TickBuffs(entity);

            if (entity.Dead)
            {
                if (entity.Kind == EntityKind.Mob && --entity.RespawnTicks <= 0)
                    RespawnMob(entity);
                continue;
            }

            if (entity.Kind == EntityKind.Mob)
                MobAi(entity);

            UpdateAction(entity);
            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);

            if (regenTick)
                Regenerate(entity);
        }
    }

    private static void TickSkillCooldowns(Entity entity)
    {
        if (entity.SkillCooldowns.Count == 0)
            return;

        foreach (var key in entity.SkillCooldowns.Keys.ToList())
        {
            if (--entity.SkillCooldowns[key] <= 0)
                entity.SkillCooldowns.Remove(key);
        }
    }

    private static void TickBuffs(Entity entity)
    {
        for (int i = entity.Buffs.Count - 1; i >= 0; i--)
        {
            if (--entity.Buffs[i].TicksRemaining <= 0)
                entity.Buffs.RemoveAt(i);
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
                if (candidate.Kind != EntityKind.Player || candidate.Dead ||
                    GameConstants.InSafeZone(candidate.X, candidate.Y))
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
            float tx = Math.Clamp(mob.HomeX + _rng.Next(-1000, 1001), 0, GameConstants.ZoneWidth);
            float ty = Math.Clamp(mob.HomeY + _rng.Next(-1000, 1001), 0, GameConstants.ZoneHeight);

            // Mobs never walk into the safe zone.
            if (!GameConstants.InSafeZone(tx, ty))
            {
                mob.TargetX = tx;
                mob.TargetY = ty;
            }
        }
    }

    private void ResetMob(Entity mob)
    {
        mob.Engaged = false;
        mob.CombatTargetId = null;
        mob.Hp = mob.MaxHp;
        mob.Buffs.Clear();
        mob.TargetX = mob.HomeX;
        mob.TargetY = mob.HomeY;
    }

    private void RespawnMob(Entity mob)
    {
        mob.Dead = false;
        mob.Hp = mob.MaxHp;
        mob.Mp = mob.MaxMp;
        mob.X = mob.HomeX;
        mob.Y = mob.HomeY;
        mob.TargetX = null;
        mob.TargetY = null;
        _world.Grid.Add(mob);
    }

    // ----- Action state machine: casting > queued skill > auto-attack ------------

    private void UpdateAction(Entity entity)
    {
        if (entity.CastingSkillId is int castingId)
        {
            UpdateCasting(entity, castingId);
            return;
        }

        if (entity.QueuedSkillId is int queuedId)
        {
            UpdateQueuedSkill(entity, queuedId);
            return;
        }

        if (entity.Engaged)
            UpdateAutoAttack(entity);
    }

    private void UpdateCasting(Entity caster, int skillId)
    {
        if (--caster.CastTicksRemaining > 0)
            return;

        caster.CastingSkillId = null;
        var def = SkillCatalog.Get(skillId);
        if (def is not null)
            ExecuteSkill(caster, def);
    }

    private void UpdateQueuedSkill(Entity caster, int skillId)
    {
        var def = SkillCatalog.Get(skillId);
        if (def is null || caster.QueuedTargetId is not Guid targetId)
        {
            caster.QueuedSkillId = null;
            return;
        }

        bool selfTargeted = targetId == caster.Id;
        Entity? target = selfTargeted ? caster
            : _world.Entities.GetValueOrDefault(targetId);

        if (target is null || target.Dead ||
            (!selfTargeted && DistanceSq(caster, target) >
                GameConstants.ViewRange * GameConstants.ViewRange))
        {
            caster.QueuedSkillId = null;
            return;
        }

        if (!selfTargeted && def.Range > 0 &&
            DistanceSq(caster, target) > def.Range * def.Range)
        {
            // Run into skill range (L2-style), re-aiming every tick.
            caster.TargetX = target.X;
            caster.TargetY = target.Y;
            return;
        }

        // In range: stand still and start the wind-up.
        caster.TargetX = null;
        caster.TargetY = null;
        caster.QueuedSkillId = null;
        caster.CastingSkillId = def.Id;
        caster.CastTargetId = targetId;
        caster.CastTicksRemaining = SkillCatalog.AdjustedCastTicks(def.CastTicks, caster.Wit);

        if (_world.EntityToConnection.TryGetValue(caster.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Cast", new CastInfo(
                def.Name, caster.CastTicksRemaining * GameConstants.TickSeconds));
        }
    }

    private void ExecuteSkill(Entity caster, SkillDef def)
    {
        if (caster.Mp < def.MpCost)
        {
            SendSystemToEntity(caster, "Not enough MP.");
            return;
        }

        bool selfTargeted = caster.CastTargetId == caster.Id || def.Range == 0 &&
            def.Effect is SkillEffect.Heal or SkillEffect.BuffAtk;

        Entity? target = selfTargeted ? caster
            : caster.CastTargetId is Guid tid ? _world.Entities.GetValueOrDefault(tid) : null;

        if (target is null || (target.Dead && target != caster))
        {
            SendSystemToEntity(caster, "Target lost.");
            return;
        }

        // Allow slight drift during the cast, but not kiting across the map.
        if (!selfTargeted && def.Range > 0 &&
            DistanceSq(caster, target) > def.Range * def.Range * 1.7f)
        {
            SendSystemToEntity(caster, "Target out of range.");
            return;
        }

        caster.Mp -= def.MpCost;
        caster.SkillCooldowns[def.Id] = def.CooldownTicks;

        switch (def.Effect)
        {
            case SkillEffect.PhysicalDamage:
            {
                // Physical skills carry bonus accuracy but can still miss.
                float miss = StatCalculator.MissChance(
                    caster.Accuracy + SkillCatalog.PhysicalSkillAccuracyBonus,
                    target.Evasion);

                if (_rng.NextDouble() < miss)
                {
                    BroadcastCombat(caster, target, 0, CombatOutcome.Miss, def.Name);
                }
                else
                {
                    int damage = SkillCatalog.PhysicalSkillDamage(
                        def.Power, caster.EffectiveAttack, target.EffectiveDefence);

                    if (_rng.NextDouble() < caster.CritChance)
                    {
                        damage = (int)(damage * StatCalculator.CritMultiplier);
                        BroadcastCombat(caster, target, damage, CombatOutcome.Crit, def.Name);
                    }
                    else
                    {
                        BroadcastCombat(caster, target, damage, CombatOutcome.Hit, def.Name);
                    }

                    target.Hp -= damage;
                }

                AfterOffensiveSkill(caster, target);
                break;
            }

            case SkillEffect.MagicDamage:
            {
                // Spells don't miss — they fail (level difference).
                float fail = SkillCatalog.SpellFailChance(caster.Level, target.Level);

                if (_rng.NextDouble() < fail)
                {
                    BroadcastCombat(caster, target, 0, CombatOutcome.Fail, def.Name);
                }
                else
                {
                    int damage = SkillCatalog.MagicSkillDamage(
                        def.Power, caster.EffectiveAttack, caster.Wit, target.EffectiveDefence);
                    target.Hp -= damage;
                    BroadcastCombat(caster, target, damage, CombatOutcome.Hit, def.Name);
                }

                AfterOffensiveSkill(caster, target);
                break;
            }

            case SkillEffect.Heal:
            {
                int amount = SkillCatalog.HealAmount(def.Power, caster.Wit);
                target.Hp = Math.Min(target.MaxHp, target.Hp + amount);
                BroadcastCombat(caster, target, amount, CombatOutcome.Heal, def.Name);
                break;
            }

            case SkillEffect.BuffAtk:
            {
                ApplyBuff(target, def);
                BroadcastCombat(caster, target, 0, CombatOutcome.Buff, def.Name);
                break;
            }

            case SkillEffect.DebuffDef:
            {
                // Debuffs are spells: they can fail too.
                float fail = SkillCatalog.SpellFailChance(caster.Level, target.Level);

                if (_rng.NextDouble() < fail)
                {
                    BroadcastCombat(caster, target, 0, CombatOutcome.Fail, def.Name);
                }
                else
                {
                    ApplyBuff(target, def);
                    BroadcastCombat(caster, target, 0, CombatOutcome.Buff, def.Name);
                }

                AfterOffensiveSkill(caster, target);
                break;
            }
        }

        if (target.Hp <= 0 && !target.Dead)
            Kill(target, caster);
    }

    private static void ApplyBuff(Entity target, SkillDef def)
    {
        // Re-applying refreshes the duration instead of stacking.
        var existing = target.Buffs.FirstOrDefault(b => b.Name == def.Name);
        if (existing is not null)
        {
            existing.TicksRemaining = def.DurationTicks;
            return;
        }

        target.Buffs.Add(new BuffInstance
        {
            Type = def.Effect,
            Magnitude = def.Magnitude,
            TicksRemaining = def.DurationTicks,
            Name = def.Name
        });
    }

    private void AfterOffensiveSkill(Entity caster, Entity target)
    {
        // Auto-attack continues after the skill (L2-style)...
        if (!target.Dead)
        {
            caster.CombatTargetId = target.Id;
            caster.Engaged = true;
        }

        // ...and the victim retaliates if it's a peaceful mob.
        Retaliate(target, caster);
    }

    private static void Retaliate(Entity victim, Entity attacker)
    {
        if (victim.Kind == EntityKind.Mob && !victim.Engaged && !victim.Dead)
        {
            victim.CombatTargetId = attacker.Id;
            victim.Engaged = true;
        }
    }

    // ----- Auto-attack ---------------------------------------------------------------

    private void UpdateAutoAttack(Entity attacker)
    {
        if (attacker.CombatTargetId is not Guid targetId)
        {
            attacker.Engaged = false;
            return;
        }

        if (!_world.Entities.TryGetValue(targetId, out var target) ||
            target.Dead ||
            DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
        {
            Disengage(attacker);
            return;
        }

        // Mobs drop aggro on players standing in the safe zone.
        if (attacker.Kind == EntityKind.Mob &&
            GameConstants.InSafeZone(target.X, target.Y))
        {
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

    private void Disengage(Entity entity)
    {
        entity.Engaged = false;
        entity.CombatTargetId = null;
        if (entity.Kind == EntityKind.Mob)
            ResetMob(entity);
    }

    private void ResolveBasicAttack(Entity attacker, Entity target)
    {
        float missChance = StatCalculator.MissChance(attacker.Accuracy, target.Evasion);

        if (_rng.NextDouble() < missChance)
        {
            BroadcastCombat(attacker, target, 0, CombatOutcome.Miss);
        }
        else
        {
            int damage = StatCalculator.BasicAttackDamage(
                (int)attacker.EffectiveAttack, (int)target.EffectiveDefence);

            if (_rng.NextDouble() < attacker.CritChance)
            {
                damage = (int)(damage * StatCalculator.CritMultiplier);
                BroadcastCombat(attacker, target, damage, CombatOutcome.Crit);
            }
            else
            {
                BroadcastCombat(attacker, target, damage, CombatOutcome.Hit);
            }

            target.Hp -= damage;
        }

        Retaliate(target, attacker);

        if (target.Hp <= 0)
            Kill(target, attacker);
    }

    private void Kill(Entity victim, Entity killer)
    {
        victim.Hp = 0;
        victim.Dead = true;
        victim.Engaged = false;
        victim.CombatTargetId = null;
        victim.QueuedSkillId = null;
        victim.CastingSkillId = null;
        victim.TargetX = null;
        victim.TargetY = null;
        victim.Buffs.Clear();

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
        if (entity.Engaged || entity.CastingSkillId is not null)
            return; // out-of-combat regen only

        // Safe zone: regen several times faster (until we add /sit).
        int multiplier = entity.Kind == EntityKind.Player &&
                         GameConstants.InSafeZone(entity.X, entity.Y)
            ? GameConstants.SafeZoneRegenMultiplier
            : 1;

        if (entity.Hp < entity.MaxHp)
        {
            int regen = Math.Max(1,
                (int)StatCalculator.HpRegenPerSecond(entity.Con, entity.Level)) * multiplier;
            entity.Hp = Math.Min(entity.MaxHp, entity.Hp + regen);
        }

        if (entity.Mp < entity.MaxMp)
        {
            int regen = Math.Max(1,
                (int)StatCalculator.MpRegenPerSecond(entity.Wit, entity.Level)) * multiplier;
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

        float nx, ny;
        if (dist <= step)
        {
            nx = tx;
            ny = ty;
        }
        else
        {
            nx = e.X + dx / dist * step;
            ny = e.Y + dy / dist * step;
        }

        // Mobs cannot set foot inside the safe zone, ever.
        if (e.Kind == EntityKind.Mob && GameConstants.InSafeZone(nx, ny))
        {
            e.TargetX = null;
            e.TargetY = null;
            return;
        }

        e.X = nx;
        e.Y = ny;
        if (nx == tx && ny == ty)
        {
            e.TargetX = null;
            e.TargetY = null;
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
                .Select(e => e.ToDto())
                .ToList();

            // Defensive: the viewer must always see themselves.
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

    private void BroadcastCombat(Entity attacker, Entity target, int damage,
        CombatOutcome outcome, string? skill = null)
    {
        var evt = new CombatEvent(
            attacker.Id, attacker.Name, target.Id, target.Name, damage, outcome, skill);

        foreach (var nearby in _world.Grid.Nearby(attacker))
        {
            if (_world.EntityToConnection.TryGetValue(nearby.Id, out var conn))
                _ = _hub.Clients.Client(conn).SendAsync("Combat", evt);
        }
    }

    private void BroadcastSystem(string text) =>
        _ = _hub.Clients.All.SendAsync("Chat", new ChatMessage("SYSTEM", text, ChatChannel.System));

    private void SendSystemTo(string connectionId, string text) =>
        _ = _hub.Clients.Client(connectionId)
            .SendAsync("Chat", new ChatMessage("SYSTEM", text, ChatChannel.System));

    private void SendSystemToEntity(Entity entity, string text)
    {
        if (_world.EntityToConnection.TryGetValue(entity.Id, out var conn))
            SendSystemTo(conn, text);
    }

    private void CancelCast(Entity entity, string connectionId)
    {
        if (entity.CastingSkillId is null)
            return;

        entity.CastingSkillId = null;
        entity.CastTargetId = null;
        _ = _hub.Clients.Client(connectionId).SendAsync("Cast", new CastInfo("", 0f));
    }

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

    /// <summary>Mobs spawn in level-banded rings around the town: the further
    /// from the safe zone, the higher the level. Leashing keeps them in band.</summary>
    private void SpawnMobs()
    {
        float cx = GameConstants.ZoneWidth / 2;
        float cy = GameConstants.ZoneHeight / 2;

        foreach (var band in SpawnBands)
        {
            for (int i = 0; i < band.Count; i++)
            {
                double angle = _rng.NextDouble() * Math.PI * 2;
                double dist = band.MinDist + _rng.NextDouble() * (band.MaxDist - band.MinDist);

                float x = Math.Clamp(cx + (float)(Math.Cos(angle) * dist), 0, GameConstants.ZoneWidth);
                float y = Math.Clamp(cy + (float)(Math.Sin(angle) * dist), 0, GameConstants.ZoneHeight);

                var (name, aggressive) = MobTypes[_rng.Next(MobTypes.Length)];
                int level = _rng.Next(band.MinLvl, band.MaxLvl + 1);
                var stats = StatCalculator.MobStats(level);

                var mob = new Entity
                {
                    Name = name,
                    Kind = EntityKind.Mob,
                    X = x,
                    Y = y,
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
}
