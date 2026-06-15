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
    private static readonly (string Name, bool Aggressive)[] MobTypes =
    {
        ("Wolf", false), ("Boar", false), ("Slime", false),
        ("Spider", true), ("Bandit", true)
    };

    // Level-banded hunting grounds: rings around the town (safe zone).
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
                _log.LogError(ex, "Unhandled error in game tick");
            }
        }
    }

    // =========================================================================
    // 1. Commands
    // =========================================================================

    private void ProcessCommands()
    {
        while (_world.Commands.TryDequeue(out var cmd))
        {
            switch (cmd)
            {
                case JoinCommand c: HandleJoin(c); break;
                case LeaveCommand c: HandleLeave(c); break;
                case MoveCmd c: HandleMove(c); break;
                case AttackCmd c: HandleAttack(c); break;
                case SkillCmd c: HandleSkill(c); break;
                case RespawnCmd c: HandleRespawn(c); break;
                case ClassChangeCmd c: HandleClassChange(c); break;
                case EquipCmd c: HandleEquip(c); break;
                case TradeRequestCmd c: HandleTradeRequest(c); break;
                case TradeRespondCmd c: HandleTradeRespond(c); break;
                case TradeOfferCmd c: HandleTradeOffer(c); break;
                case TradeReadyCmd c: HandleTradeReady(c); break;
                case TradeCancelCmd c: HandleTradeCancel(c); break;
                case ChatCmd c: HandleChat(c); break;
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

        // Starter gear so the inventory has something in it.
        entity.Inventory.Add(new InventoryItem { DefId = 1 });  // Rusty Sword
        entity.Inventory.Add(new InventoryItem { DefId = 9 });  // Leather Vest

        _world.Entities[entity.Id] = entity;
        _world.EntityToConnection[entity.Id] = join.ConnectionId;
        _world.ConnectionToEntity[join.ConnectionId] = entity.Id;
        _world.Grid.Add(entity);

        join.Result.TrySetResult(new LoginResult(true, null, entity.Id, entity.X, entity.Y));

        SendInventory(entity);
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
            CancelTradeFor(entity, notifyPartnerOnly: true);
            _world.PendingTradeRequests.Remove(entity.Id);

            if (!entity.Dead)
                _world.Grid.Remove(entity);
            BroadcastSystem($"{entity.Name} left the world.");
        }
    }

    private void HandleMove(MoveCmd move)
    {
        if (!TryGetPlayer(move.ConnectionId, out var entity) || entity.Dead)
            return;

        entity.Engaged = false;
        entity.CombatTargetId = null;
        entity.QueuedSkillId = null;
        CancelCast(entity);

        entity.TargetX = Math.Clamp(move.Move.TargetX, 0, GameConstants.ZoneWidth);
        entity.TargetY = Math.Clamp(move.Move.TargetY, 0, GameConstants.ZoneHeight);
    }

    private void HandleAttack(AttackCmd attack)
    {
        if (!TryGetPlayer(attack.ConnectionId, out var attacker) || attacker.Dead)
            return;

        if (attack.TargetId == attacker.Id ||
            !_world.Entities.TryGetValue(attack.TargetId, out var target) ||
            target.Dead ||
            DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
            return;

        attacker.QueuedSkillId = null;
        CancelCast(attacker);
        attacker.CombatTargetId = target.Id;
        attacker.Engaged = true;
    }

    private void HandleSkill(SkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var caster) || caster.Dead)
            return;

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null || def.Class != caster.BaseClass ||
            (def.RequiredArchetype is not null && def.RequiredArchetype != caster.Archetype))
            return;

        if (caster.SkillCooldowns.TryGetValue(def.Id, out int cd) && cd > 0)
        {
            SendSystemToEntity(caster, $"{def.Name} is not ready.");
            return;
        }

        if (caster.Mp < def.MpCost)
        {
            SendSystemToEntity(caster, "Not enough MP.");
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
                SendSystemToEntity(caster, $"{def.Name} needs a target.");
                return;
            }
            targetId = tid;
        }
        else if (def.Effect is SkillEffect.Heal && def.Range > 0 &&
                 cmd.TargetId is Guid allyId &&
                 _world.Entities.TryGetValue(allyId, out var ally) &&
                 ally.Kind == EntityKind.Player && !ally.Dead)
        {
            targetId = allyId; // ranged heal on a targeted player
        }
        else
        {
            targetId = caster.Id; // self-targeted
        }

        CancelCast(caster);
        caster.QueuedSkillId = def.Id;
        caster.QueuedTargetId = targetId;
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

    // ----- Class change ------------------------------------------------------------

    private void HandleClassChange(ClassChangeCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        if (player.SecondClass != 0)
        {
            SendSystemToEntity(player, "You have already chosen your path.");
            return;
        }

        if (player.Level < GameConstants.ClassChangeLevel)
        {
            SendSystemToEntity(player,
                $"Class change requires level {GameConstants.ClassChangeLevel}.");
            return;
        }

        var def = ClassCatalog.Get(cmd.ClassId);
        if (def is null || def.Race != player.Race || def.Base != player.BaseClass)
            return;

        player.SecondClass = def.Id;
        var (con, atk, wit, dex) = ClassCatalog.StatBonus(def.Archetype);
        player.Con += con;
        player.AtkStat += atk;
        player.Wit += wit;
        player.Dex += dex;
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        BroadcastSystem($"{player.Name} has become a {def.Name}!");
    }

    // ----- Equipment ------------------------------------------------------------------

    private void HandleEquip(EquipCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || ItemCatalog.Get(item.DefId) is not ItemDef def)
            return;

        if (item.Equipped)
        {
            item.Equipped = false;
        }
        else
        {
            if (player.Level < ItemCatalog.RequiredLevel(def.Grade))
            {
                SendSystemToEntity(player,
                    $"{def.Name} requires level {ItemCatalog.RequiredLevel(def.Grade)}.");
                return;
            }

            // Items being traded cannot be equipped mid-trade.
            if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
                trade.OfferOf(player).Contains(item.InstanceId))
                return;

            // One item per slot: unequip the current one.
            foreach (var other in player.Inventory)
            {
                if (other.Equipped &&
                    ItemCatalog.Get(other.DefId) is ItemDef otherDef &&
                    otherDef.Slot == def.Slot)
                    other.Equipped = false;
            }

            item.Equipped = true;
        }

        player.RecomputeDerived();
        SendInventory(player);
    }

    // ----- Trade ---------------------------------------------------------------------------

    private void HandleTradeRequest(TradeRequestCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var requester) || requester.Dead)
            return;

        if (_world.ActiveTrades.ContainsKey(requester.Id))
            return;

        if (!_world.Entities.TryGetValue(cmd.TargetId, out var target) ||
            target.Kind != EntityKind.Player || target.Dead ||
            _world.ActiveTrades.ContainsKey(target.Id))
        {
            SendSystemToEntity(requester, "That player cannot trade right now.");
            return;
        }

        if (DistanceSq(requester, target) > GameConstants.TradeRange * GameConstants.TradeRange)
        {
            SendSystemToEntity(requester, "Too far away to trade.");
            return;
        }

        _world.PendingTradeRequests[target.Id] = requester.Id;

        if (_world.EntityToConnection.TryGetValue(target.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("TradeRequest",
                new TradeRequestNotice(requester.Id, requester.Name));

        SendSystemToEntity(requester, $"Trade request sent to {target.Name}.");
    }

    private void HandleTradeRespond(TradeRespondCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var responder))
            return;

        if (!_world.PendingTradeRequests.Remove(responder.Id, out var requesterId))
            return;

        if (!_world.Entities.TryGetValue(requesterId, out var requester) || requester.Dead)
            return;

        if (!cmd.Accept)
        {
            SendSystemToEntity(requester, $"{responder.Name} declined the trade.");
            return;
        }

        if (_world.ActiveTrades.ContainsKey(requester.Id) ||
            _world.ActiveTrades.ContainsKey(responder.Id))
            return;

        var session = new TradeSession { A = requester, B = responder };
        _world.ActiveTrades[requester.Id] = session;
        _world.ActiveTrades[responder.Id] = session;
        SendTradeState(session);
    }

    private void HandleTradeOffer(TradeOfferCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) ||
            !_world.ActiveTrades.TryGetValue(player.Id, out var session))
            return;

        var offer = session.OfferOf(player);
        offer.Clear();

        foreach (var instanceId in cmd.InstanceIds.Distinct()
                     .Take(GameConstants.TradeMaxOfferSlots))
        {
            var item = player.Inventory.FirstOrDefault(i => i.InstanceId == instanceId);
            if (item is not null && !item.Equipped)
                offer.Add(instanceId);
        }

        // Changing an offer resets both ready flags (no bait-and-switch).
        session.ReadyA = false;
        session.ReadyB = false;
        SendTradeState(session);
    }

    private void HandleTradeReady(TradeReadyCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) ||
            !_world.ActiveTrades.TryGetValue(player.Id, out var session))
            return;

        session.SetReady(player, true);

        if (session.ReadyA && session.ReadyB)
            CompleteTrade(session);
        else
            SendTradeState(session);
    }

    private void HandleTradeCancel(TradeCancelCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        _world.PendingTradeRequests.Remove(player.Id);
        CancelTradeFor(player, notifyPartnerOnly: false);
    }

    private void CompleteTrade(TradeSession session)
    {
        var itemsA = ResolveOffer(session.A, session.OfferA);
        var itemsB = ResolveOffer(session.B, session.OfferB);

        bool valid = itemsA is not null && itemsB is not null &&
            session.A.Inventory.Count - itemsA.Count + itemsB.Count
                <= GameConstants.InventorySize &&
            session.B.Inventory.Count - itemsB.Count + itemsA.Count
                <= GameConstants.InventorySize;

        if (!valid)
        {
            SendSystemToEntity(session.A, "Trade failed (items changed or bags full).");
            SendSystemToEntity(session.B, "Trade failed (items changed or bags full).");
            CloseTrade(session);
            return;
        }

        foreach (var item in itemsA!)
        {
            session.A.Inventory.Remove(item);
            session.B.Inventory.Add(item);
        }
        foreach (var item in itemsB!)
        {
            session.B.Inventory.Remove(item);
            session.A.Inventory.Add(item);
        }

        SendSystemToEntity(session.A, "Trade completed.");
        SendSystemToEntity(session.B, "Trade completed.");
        CloseTrade(session);
        SendInventory(session.A);
        SendInventory(session.B);
    }

    private static List<InventoryItem>? ResolveOffer(Entity owner, List<Guid> offer)
    {
        var items = new List<InventoryItem>();
        foreach (var id in offer)
        {
            var item = owner.Inventory.FirstOrDefault(i => i.InstanceId == id);
            if (item is null || item.Equipped)
                return null;
            items.Add(item);
        }
        return items;
    }

    private void CancelTradeFor(Entity player, bool notifyPartnerOnly)
    {
        if (!_world.ActiveTrades.TryGetValue(player.Id, out var session))
            return;

        var partner = session.PartnerOf(player);
        SendSystemToEntity(partner, $"{player.Name} cancelled the trade.");
        if (!notifyPartnerOnly)
            SendSystemToEntity(player, "Trade cancelled.");
        CloseTrade(session);
    }

    private void CloseTrade(TradeSession session)
    {
        _world.ActiveTrades.Remove(session.A.Id);
        _world.ActiveTrades.Remove(session.B.Id);

        var closed = new TradeStateUpdate(false, "", Array.Empty<InventoryItemDto>(),
            Array.Empty<InventoryItemDto>(), false, false);
        SendTo(session.A, "Trade", closed);
        SendTo(session.B, "Trade", closed);
    }

    private void SendTradeState(TradeSession session)
    {
        SendTo(session.A, "Trade", BuildTradeState(session, session.A));
        SendTo(session.B, "Trade", BuildTradeState(session, session.B));
    }

    private TradeStateUpdate BuildTradeState(TradeSession session, Entity viewer)
    {
        var partner = session.PartnerOf(viewer);
        return new TradeStateUpdate(
            true,
            partner.Name,
            OfferDtos(viewer, session.OfferOf(viewer)),
            OfferDtos(partner, session.OfferOf(partner)),
            session.ReadyOf(viewer),
            session.ReadyOf(partner));
    }

    private static InventoryItemDto[] OfferDtos(Entity owner, List<Guid> offer) =>
        offer.Select(id => owner.Inventory.FirstOrDefault(i => i.InstanceId == id))
            .Where(i => i is not null)
            .Select(i => i!.ToDto())
            .ToArray();

    // ----- Chat -----------------------------------------------------------------------------

    private void HandleChat(ChatCmd chat)
    {
        if (!TryGetPlayer(chat.ConnectionId, out var sender))
            return;

        var text = chat.Text.Trim();
        if (text.Length is 0 or > 200)
            return;

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
            _ = _hub.Clients.Client(chat.ConnectionId).SendAsync("Chat", whisper);
            return;
        }

        var message = new ChatMessage(sender.Name, text, channel);

        if (channel == ChatChannel.World)
        {
            _ = _hub.Clients.All.SendAsync("Chat", message);
            return;
        }

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
            float dx = mob.X - mob.HomeX;
            float dy = mob.Y - mob.HomeY;
            if (dx * dx + dy * dy > GameConstants.MobLeashRange * GameConstants.MobLeashRange)
                ResetMob(mob);
            return;
        }

        if (--mob.WanderTicks > 0)
            return;

        mob.WanderTicks = _rng.Next(30, 120);

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
            if (--entity.CastTicksRemaining <= 0)
            {
                entity.CastingSkillId = null;
                if (SkillCatalog.Get(castingId) is SkillDef def)
                    ExecuteSkill(entity, def);
            }
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

        float range = SkillCatalog.EffectiveRange(def, caster.Archetype, caster.BasicAttackRange);

        if (!selfTargeted && DistanceSq(caster, target) > range * range)
        {
            caster.TargetX = target.X;
            caster.TargetY = target.Y;
            return;
        }

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

        bool selfTargeted = caster.CastTargetId == caster.Id;
        Entity? target = selfTargeted ? caster
            : caster.CastTargetId is Guid tid ? _world.Entities.GetValueOrDefault(tid) : null;

        if (target is null || (target.Dead && target != caster))
        {
            SendSystemToEntity(caster, "Target lost.");
            return;
        }

        float range = SkillCatalog.EffectiveRange(def, caster.Archetype, caster.BasicAttackRange);
        if (!selfTargeted && DistanceSq(caster, target) > range * range * 1.7f)
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
            case SkillEffect.BuffDef:
            {
                ApplyBuff(target, def);
                BroadcastCombat(caster, target, 0, CombatOutcome.Buff, def.Name);
                break;
            }

            case SkillEffect.DebuffDef:
            {
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
        if (!target.Dead)
        {
            caster.CombatTargetId = target.Id;
            caster.Engaged = true;
        }

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

        if (attacker.Kind == EntityKind.Mob &&
            GameConstants.InSafeZone(target.X, target.Y))
        {
            ResetMob(attacker);
            return;
        }

        float range = attacker.BasicAttackRange;
        if (DistanceSq(attacker, target) > range * range)
        {
            attacker.TargetX = target.X;
            attacker.TargetY = target.Y;
            return;
        }

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
            _world.Grid.Remove(victim);
            victim.RespawnTicks = GameConstants.MobRespawnTicks;

            if (killer.Kind == EntityKind.Player)
            {
                AwardExp(killer, StatCalculator.MobExpReward(victim.Level));
                RollDrop(killer, victim);
            }
        }
        else
        {
            CancelTradeFor(victim, notifyPartnerOnly: false);
            BroadcastSystem($"{victim.Name} was slain by {killer.Name}.");
        }
    }

    private void RollDrop(Entity killer, Entity mob)
    {
        var def = ItemCatalog.RollDrop(mob.Level, _rng);
        if (def is null)
            return;

        if (killer.Inventory.Count >= GameConstants.InventorySize)
        {
            SendSystemToEntity(killer, $"{mob.Name} dropped {def.Name} — inventory full!");
            return;
        }

        killer.Inventory.Add(new InventoryItem { DefId = def.Id });
        SendSystemToEntity(killer, $"You looted: {def.Name} [{def.Grade}/{def.Rarity}]");
        SendInventory(killer);
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
            player.Hp = player.MaxHp;
            player.Mp = player.MaxMp;
            BroadcastSystem($"{player.Name} reached level {player.Level}!");

            if (player.Level >= GameConstants.ClassChangeLevel && player.SecondClass == 0)
                SendSystemToEntity(player,
                    "You may now choose a second class! (Class button, top right)");
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
            return;

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
    // 3. Broadcast
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

            if (!visible.Any(d => d.Id == player.Id))
                visible.Add(player.ToDto());

            sends.Add(_hub.Clients.Client(connectionId)
                .SendAsync("Snapshot", new WorldSnapshot(visible.ToArray())));
        }

        try { await Task.WhenAll(sends); }
        catch { /* disconnects clean up via LeaveCommand */ }
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

    private void SendTo(Entity entity, string method, object payload)
    {
        if (_world.EntityToConnection.TryGetValue(entity.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync(method, payload);
    }

    private void SendInventory(Entity player) =>
        SendTo(player, "Inventory", new InventoryUpdate(
            player.Inventory.Select(i => i.ToDto()).ToArray()));

    private void CancelCast(Entity entity)
    {
        if (entity.CastingSkillId is null)
            return;

        entity.CastingSkillId = null;
        entity.CastTargetId = null;
        SendTo(entity, "Cast", new CastInfo("", 0f));
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
