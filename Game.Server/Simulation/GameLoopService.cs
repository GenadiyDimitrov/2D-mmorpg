using Game.Server.Hubs;
using Game.Server.Persistence;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Simulation;

/// <summary>
/// The heart of the server: a fixed-tick loop (10 t/s).
/// Each tick: drain commands -> simulate -> broadcast snapshots.
/// </summary>
public class GameLoopService : BackgroundService
{
    // Which mob types are aggressive (chase on sight). Names match WorldMap zones.
    private static bool IsAggressive(string mobName) => MobCatalog.IsAggressive(mobName);

    private readonly World _world;
    private readonly IHubContext<GameHub> _hub;
    private readonly ILogger<GameLoopService> _log;
    private readonly Game.Server.Persistence.PersistenceService _db;
    private readonly Random _rng = new();
    private long _tick;

    /// <summary>Per-zone spawner runtime (population + respawn scheduling).</summary>
    private readonly List<ZoneRuntime> _zones = new();
    private DayPhase _lastPhase = DayPhase.Day;

    public GameLoopService(World world, IHubContext<GameHub> hub,
        ILogger<GameLoopService> log, Game.Server.Persistence.PersistenceService db)
    {
        _world = world;
        _hub = hub;
        _log = log;
        _db = db;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        GameClock.Epoch = DateTime.UtcNow;
        await InitZonesAsync();
        SpawnNpcs();
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
                case EnterWorldCommand c: HandleEnterWorld(c); break;
                case AdminCmd c: HandleAdmin(c); break;
                case LeaveCommand c: HandleLeave(c); break;
                case MoveCmd c: HandleMove(c); break;
                case AttackCmd c: HandleAttack(c); break;
                case SkillCmd c: HandleSkill(c); break;
                case LearnSkillCmd c: HandleLearnSkill(c); break;
                case TalkCmd c: HandleTalk(c); break;
                case QuestActionCmd c: HandleQuestAction(c); break;
                case BuyItemCmd c: HandleBuy(c); break;
                case SellItemCmd c: HandleSell(c); break;
                case TeleportCmd c: HandleTeleport(c); break;
                case SetMoveStateCmd c: HandleSetMoveState(c); break;
                case CancelCastCmd c: HandleCancelCast(c); break;
                case RemoveBuffCmd c: HandleRemoveBuff(c); break;
                case RespawnCmd c: HandleRespawn(c); break;
                case ClassChangeCmd c: HandleClassChange(c); break;
                case EquipCmd c: HandleEquip(c); break;
                case UsePotionCmd c: HandleUsePotion(c); break;
                case EnchantCmd c: HandleEnchant(c); break;
                case RerollAttributesCmd c: HandleRerollAttributes(c); break;
                case RemoveItemCmd c: HandleRemoveItem(c); break;
                case DebugGiveCmd c: HandleDebugGive(c); break;
                case DebugLevelCmd c: HandleDebugLevel(c); break;
                case DebugGoldCmd c: HandleDebugGold(c); break;
                case DebugSpCmd c: HandleDebugSp(c); break;
                case DebugResetCmd c: HandleDebugReset(c); break;
                case DebugThirdClassCmd c: HandleDebugThirdClass(c); break;
                case TradeRequestCmd c: HandleTradeRequest(c); break;
                case TradeRespondCmd c: HandleTradeRespond(c); break;
                case TradeOfferCmd c: HandleTradeOffer(c); break;
                case TradeReadyCmd c: HandleTradeReady(c); break;
                case TradeCancelCmd c: HandleTradeCancel(c); break;
                case ChatCmd c: HandleChat(c); break;
            }
        }
    }

    private void HandleEnterWorld(EnterWorldCommand cmd)
    {
        var entity = cmd.Entity;

        // Spawn position: where they logged off, nudged into the world bounds.
        entity.X = Math.Clamp(entity.X, 0, GameConstants.ZoneWidth);
        entity.Y = Math.Clamp(entity.Y, 0, GameConstants.ZoneHeight);

        _world.Entities[entity.Id] = entity;
        _world.EntityToConnection[entity.Id] = cmd.ConnectionId;
        _world.ConnectionToEntity[cmd.ConnectionId] = entity.Id;
        _world.Grid.Add(entity);

        cmd.Result.TrySetResult(new LoginResult(true, null, entity.Id, entity.X, entity.Y, GameClock.Epoch));

        AutoLearnCoreSkills(entity);
        SendInventory(entity);
        SendStats(entity);
        SendLearned(entity);
        SendQuestLog(entity);
        SendGold(entity);
        if (entity.IsAdmin)
            SendSystemToEntity(entity, "Admin privileges active. Type /help for commands.");
        BroadcastSystem($"{entity.Name} entered the world.");
        _log.LogInformation("Player {Name} entered (char {Id})", entity.Name, entity.PersistentId);
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

            // Persist on logout (fire-and-forget off the loop thread).
            SaveEntity(entity);
            BroadcastSystem($"{entity.Name} left the world.");
        }
    }

    /// <summary>Save a character without blocking the tick loop. The snapshot is taken
    /// HERE (on the single-writer thread) so the async DB write never reads the live,
    /// mutating entity; the DB I/O runs off-thread.</summary>
    private void SaveEntity(Entity entity)
    {
        if (PersistenceService.CharacterSnapshot.From(entity) is { } snap)
            RunSave(() => _db.SaveCharacterAsync(snap));
    }

    /// <summary>Periodic crash-safety save of every online player. Snapshots all of
    /// them on-thread, then hands the batch to ONE background write (one DbContext,
    /// one SaveChanges) — no thundering herd of connections off the tick.</summary>
    private void AutoSaveAll()
    {
        List<PersistenceService.CharacterSnapshot>? snaps = null;
        foreach (var entity in _world.Entities.Values)
            if (entity.Kind == EntityKind.Player &&
                PersistenceService.CharacterSnapshot.From(entity) is { } snap)
                (snaps ??= new()).Add(snap);

        if (snaps is { Count: > 0 })
            RunSave(() => _db.SaveCharactersAsync(snaps));
    }

    /// <summary>Fire-and-forget a DB write off the tick thread, logging any failure
    /// (so an exception in a background save can't go unobserved).</summary>
    private void RunSave(Func<Task> save) => _ = Task.Run(async () =>
    {
        try { await save(); }
        catch (Exception ex) { _log.LogError(ex, "Background character save failed"); }
    });

    private void HandleMove(MoveCmd move)
    {
        if (!TryGetPlayer(move.ConnectionId, out var entity) || entity.Dead)
            return;

        // Can't move while standing up from a sit (recovery window).
        if (entity.StandUpTicks > 0)
            return;

        // Casting roots you — movement is rejected until the cast finishes or you
        // cancel it explicitly (ESC). Moving does NOT cancel the cast.
        if (entity.CastingSkillId is not null)
            return;

        // Moving stands you up instantly (no delay when you choose to move).
        if (entity.MoveState == MoveState.Sitting)
            entity.MoveState = MoveState.Running;

        entity.Engaged = false;
        entity.CombatTargetId = null;
        entity.QueuedSkillId = null;

        entity.TargetX = Math.Clamp(move.Move.TargetX, 0, GameConstants.ZoneWidth);
        entity.TargetY = Math.Clamp(move.Move.TargetY, 0, GameConstants.ZoneHeight);
    }

    private void HandleSetMoveState(SetMoveStateCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (player.StandUpTicks > 0) return;   // mid stand-up, ignore

        // Sitting requires being idle (not engaged / not casting).
        if (cmd.State == MoveState.Sitting && (player.Engaged || player.CastingSkillId is not null))
            return;

        // Walk<->Run is instant; entering Sit stops movement.
        player.MoveState = cmd.State;
        if (cmd.State == MoveState.Sitting)
        {
            player.TargetX = null;
            player.TargetY = null;
        }
        SendStats(player);
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

    /// <summary>Grant (for free) the class's core skills whose level is met.
    /// Nothing is auto-learned anymore EXCEPT a mage's starter nuke (Magic Bolt), so
    /// mages can deal damage from level 1. Every other skill (incl. 2nd/3rd-class
    /// skills) must be learned with SP from the skills window.</summary>
    private void AutoLearnCoreSkills(Entity player)
    {
        // Auto-grant LEVEL 1 of skills the player should start with — but never
        // DOWNGRADE a skill the player has since leveled up (e.g. learned Magic Bolt 3).
        // Don't re-add Magic Bolt once a superior skill (Flame Bolt, etc.) replaced it.
        if (player.BaseClass == BaseClass.Mage && !player.HasSkill(SkillCatalog.MagicBolt)
            && !IsSuperseded(player, SkillCatalog.MagicBolt))
            player.LearnedSkills[SkillCatalog.MagicBolt] = 1;

        // The class's natural armor-weight mastery, free at level 1 (level 1 only).
        string mastery = player.BaseClass == BaseClass.Mage ? SkillCatalog.MasteryRobe : SkillCatalog.MasteryLight;
        if (!player.HasSkill(mastery))
            player.LearnedSkills[mastery] = 1;

        // Combat "training" passive (soulshot/spiritshot stand-in): auto-granted, with
        // the LEVEL chosen by character level (+10%…+100% atk; see TrainingLevelFor).
        int trainLvl = StatCalculator.TrainingLevelFor(player.Level);
        if (trainLvl > 0)
            player.LearnedSkills[player.BaseClass == BaseClass.Mage
                ? SkillCatalog.SpiritTraining
                : SkillCatalog.PhysicalTraining] = trainLvl;

        // Class identity "sure" floor passive for the current class tier (level = tier).
        if (SkillCatalog.FloorPassiveFor(player.Archetype, player.Level) is { } floor)
            player.LearnedSkills[floor.Id] = floor.Level;
    }

    /// <summary>True if the player has learned a skill that REPLACES the given id
    /// (e.g. Flame Bolt replaces Magic Bolt) — a cross-skill upgrade, not a level.</summary>
    private static bool IsSuperseded(Entity player, string skillId)
    {
        foreach (var learnedId in player.LearnedSkills.Keys)
            if (SkillCatalog.Get(learnedId)?.Replaces is { } rep && Array.IndexOf(rep, skillId) >= 0)
                return true;
        return false;
    }

    private void HandleLearnSkill(LearnSkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null)
            return;

        // Learning advances to the NEXT level of the skill (level 1 if not yet known).
        int cur = player.SkillLevelOf(def.Id);
        int target = cur + 1;
        if (target > def.MaxLevel)
        {
            SendSystemToEntity(player, $"{def.Name} is already at its highest level.");
            return;
        }

        // Cross-skill upgrade already replaces this one (e.g. Flame Bolt replaced Magic Bolt).
        if (cur == 0 && IsSuperseded(player, def.Id))
        {
            SendSystemToEntity(player, $"You already know a superior version of {def.Name}.");
            return;
        }

        // This (skill, level) must be on the class list and the level gate met.
        int gate = ClassSkills.LearnLevelOf(def.Id, target, player.Race, player.BaseClass, player.Archetype, player.Discipline);
        if (gate == 0)
        {
            SendSystemToEntity(player, cur == 0
                ? $"Your class cannot learn {def.Name}."
                : $"{def.Name} cannot be raised further by your class.");
            return;
        }
        if (player.Level < gate)
        {
            SendSystemToEntity(player, def.MaxLevel > 1
                ? $"{def.Name} (Lv.{target}) requires level {gate}."
                : $"{def.Name} requires level {gate}.");
            return;
        }

        int cost = def.SpCostAt(target);
        if (player.SkillPoints < cost)
        {
            SendSystemToEntity(player, $"Not enough skill points ({cost} needed).");
            return;
        }

        player.SkillPoints -= cost;
        player.LearnedSkills[def.Id] = target;

        // Cross-skill replacement (FlameBolt replaces MagicBolt) — only on first learn.
        if (cur == 0 && def.Replaces is { Length: > 0 })
            foreach (var replacedId in def.Replaces)
                player.LearnedSkills.Remove(replacedId);

        // Recompute so passives take effect immediately, not just on the next equip/level.
        player.RecomputeDerived();

        SendSystemToEntity(player, def.MaxLevel > 1 ? $"Learned {def.Name} (Lv.{target})!" : $"Learned {def.Name}!");
        SendStats(player);
        SendLearned(player);
        SaveEntity(player);
    }

    private void HandleSkill(SkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var caster) || caster.Dead)
            return;

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null || !caster.HasSkill(def.Id))
            return;

        // Passives (armor masteries) are always-on; they can't be cast.
        if (def.Category == SkillCategory.Passive)
            return;

        if (caster.SkillCooldowns.TryGetValue(def.Id, out int cd) && cd > 0)
        {
            SendSystemToEntity(caster, $"{def.Name} is not ready.");
            return;
        }

        if (caster.Mp < def.MpCostAt(caster.SkillLevelOf(def.Id)))
        {
            SendSystemToEntity(caster, "Not enough MP.");
            return;
        }

        bool offensive = (def.Effect & (SkillEffect.PhysicalDamage
            | SkillEffect.MagicDamage | SkillEffect.AnyDebuff)) != 0;

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
        else if ((def.Effect & (SkillEffect.Heal | SkillEffect.RestoreMp | SkillEffect.Cleanse | SkillEffect.AnyBuff)) != 0 &&
                 def.TargetMode != TargetMode.SelfOnly && def.Range > 0 &&
                 cmd.TargetId is Guid allyId &&
                 _world.Entities.TryGetValue(allyId, out var ally) &&
                 ally.Kind == EntityKind.Player && !ally.Dead)
        {
            targetId = allyId; // ranged heal / cleanse / buff on a targeted ally
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
        // Respawn at the nearest town (the world is large now).
        var town = WorldMap.NearestSafeZone(entity.X, entity.Y);
        entity.X = town.X + _rng.Next(-250, 250);
        entity.Y = town.Y + _rng.Next(-250, 250);
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
        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendStats(player);
        SendLearned(player);
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

            // One item per slot: unequip the current one. Also enforce the
            // two-handed rule: a 2H weapon and a shield cannot coexist (a 2H
            // weapon occupies the offhand), so equipping one drops the other.
            bool equippingTwoHandWeapon = def.Slot == EquipSlot.Weapon && def.Hands == WeaponHands.TwoHand;
            bool equippingShield = def.Slot == EquipSlot.Shield;
            foreach (var other in player.Inventory)
            {
                if (!other.Equipped || ItemCatalog.Get(other.DefId) is not ItemDef otherDef)
                    continue;

                // Same slot — for armor, the body-part slot must also match (so a
                // helmet and a chest piece coexist, but two helmets don't).
                if (otherDef.Slot == def.Slot &&
                    (def.Slot != EquipSlot.Armor || otherDef.ArmorSlot == def.ArmorSlot))
                    other.Equipped = false;
                else if (equippingTwoHandWeapon && otherDef.Slot == EquipSlot.Shield)
                    other.Equipped = false;                                   // 2H weapon drops shield
                else if (equippingShield && otherDef.Slot == EquipSlot.Weapon
                         && otherDef.Hands == WeaponHands.TwoHand)
                    other.Equipped = false;                                   // shield drops 2H weapon
            }

            item.Equipped = true;
        }

        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
    }

    private void HandleEnchant(EnchantCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        var scroll = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.ScrollInstanceId);
        var target = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.TargetInstanceId);

        if (scroll is null || target is null ||
            ItemCatalog.Get(scroll.DefId) is not ItemDef scrollDef ||
            ItemCatalog.Get(target.DefId) is not ItemDef targetDef ||
            !ItemCatalog.IsEnchantScroll(scrollDef) || !ItemCatalog.IsEquippable(targetDef))
        {
            SendSystemToEntity(player, "Invalid enchant.");
            return;
        }

        if (target.Enchant >= EnchantRules.MaxEnchant)
        {
            SendSystemToEntity(player, $"{targetDef.Name} is already at max enchant.");
            return;
        }

        var (result, newLevel) = EnchantRules.Attempt(target.Enchant, scrollDef.ScrollKind, _rng);

        // The scroll is always consumed (one from the stack).
        ConsumeOne(player, scroll);

        bool destroyed = false;
        string outcome;
        switch (result)
        {
            case EnchantResult.Success:
                target.Enchant = newLevel;
                outcome = $"Success! {targetDef.Name} is now +{newLevel}.";
                break;
            case EnchantResult.Broke:
                player.Inventory.Remove(target);
                destroyed = true;
                outcome = $"{targetDef.Name} shattered!";
                break;
            case EnchantResult.Reset:
                target.Enchant = 0;
                outcome = $"Enchant failed — {targetDef.Name} reset to +0.";
                break;
            case EnchantResult.Downgraded:
                target.Enchant = newLevel;
                outcome = $"Enchant failed — {targetDef.Name} dropped to +{newLevel}.";
                break;
            default:
                outcome = "Nothing happened.";
                break;
        }

        if (target.Equipped && !destroyed)
            player.RecomputeDerived();

        SendTo(player, "Enchant", new EnchantResultDto(
            targetDef.Name, destroyed ? 0 : target.Enchant, outcome, destroyed));
        SendSystemToEntity(player, outcome);
        SendInventory(player);
        if (target.Equipped || destroyed)
            SendStats(player);
    }

    private void HandleRerollAttributes(RerollAttributesCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        var scroll = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.ScrollInstanceId);
        var target = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.TargetInstanceId);

        if (scroll is null || target is null ||
            ItemCatalog.Get(scroll.DefId) is not ItemDef scrollDef ||
            ItemCatalog.Get(target.DefId) is not ItemDef targetDef ||
            !ItemCatalog.IsAttributeScroll(scrollDef) || !ItemCatalog.IsEquippable(targetDef))
        {
            SendSystemToEntity(player, "Invalid reroll.");
            return;
        }

        if (target.Attributes.Count == 0)
        {
            SendSystemToEntity(player, $"{targetDef.Name} has no attributes to reroll.");
            return;
        }

        // Build the lock mask from the requested indices, clamped to the scroll's capacity.
        int capacity = AttributeSystem.RerollLockCapacity(scrollDef.AttrScroll);
        var locked = new bool[target.Attributes.Count];
        int lockedCount = 0;
        foreach (var idx in cmd.LockedIndices.Distinct())
        {
            if (idx < 0 || idx >= locked.Length || lockedCount >= capacity) continue;
            locked[idx] = true;
            lockedCount++;
        }

        bool forceMax = scrollDef.AttrScroll == AttrScrollKind.Legendary;
        target.Attributes = AttributeSystem.Reroll(targetDef, target.Attributes, locked, forceMax, _rng);

        ConsumeOne(player, scroll);
        if (target.Equipped)
            player.RecomputeDerived();

        string outcome = forceMax
            ? $"{targetDef.Name} attributes rerolled to MAX."
            : $"{targetDef.Name} attributes rerolled.";
        SendTo(player, "Reroll", new RerollResultDto(targetDef.Name, outcome));
        SendSystemToEntity(player, outcome);
        SendInventory(player);
        if (target.Equipped)
            SendStats(player);
    }

    private void HandleRemoveItem(RemoveItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null)
            return;

        // Quest items can't be destroyed.
        if (ItemCatalog.Get(item.DefId) is ItemDef qd && ItemCatalog.IsQuestItem(qd))
        {
            SendSystemToEntity(player, "Quest items cannot be discarded.");
            return;
        }

        // Block destroying items that are currently in a trade offer.
        if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
            trade.OfferOf(player).Contains(item.InstanceId))
            return;

        bool wasEquipped = item.Equipped;

        if (item.Quantity > 1)
            item.Quantity--;             // drop one from the stack
        else
            player.Inventory.Remove(item);

        if (wasEquipped)
            player.RecomputeDerived();

        SendInventory(player);
        if (wasEquipped)
            SendStats(player);
    }

#pragma warning disable CS1998
    private void HandleDebugGive(DebugGiveCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (ItemCatalog.Get(cmd.DefId) is not ItemDef def)
            return;
        if (!AddItem(player, def.Id))
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }
        SendSystemToEntity(player, $"[DEBUG] Added {def.Name}.");
        SendInventory(player);
    }

    private void HandleDebugLevel(DebugLevelCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        // Grant EXACTLY one level. Going through AwardExp would scale by
        // ExpRate (x10), overshooting into several levels at once.
        player.Level++;
        player.Exp = 0;
        OnLevelUp(player);
        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), true));
        SendSystemToEntity(player, $"[DEBUG] Level up -> {player.Level}.");
    }

    private void HandleDebugGold(DebugGoldCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.Gold += Math.Max(0, cmd.Amount);
        SendGold(player);
        SendSystemToEntity(player, $"[DEBUG] +{cmd.Amount:N0} {GameConstants.CurrencyName} (now {player.Gold:N0}).");
    }

    private void HandleDebugSp(DebugSpCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.SkillPoints += (int)Math.Max(0, cmd.Amount);
        SendStats(player);
        SendLearned(player);
        SendSystemToEntity(player, $"[DEBUG] +{cmd.Amount:N0} SP (now {player.SkillPoints:N0}).");
    }

    /// <summary>DEBUG: re-roll the SAME character in place — new race/base class,
    /// back to level 1 with the starter kit, classes/skills/quests/inventory cleared.
    /// Keeps the character row, name, gold and position.</summary>
    private void HandleDebugReset(DebugResetCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        player.Race = cmd.Race;
        player.BaseClass = cmd.BaseClass;
        var s = StatCalculator.GetBaseStats(cmd.Race, cmd.BaseClass);
        player.Con = s.Con; player.AtkStat = s.Atk; player.Wit = s.Wit; player.Dex = s.Dex;

        player.Level = 1;
        player.Exp = 0;
        player.SecondClass = 0;
        player.ThirdClass = 0;
        player.SkillPoints = 0;
        player.LearnedSkills.Clear();
        player.ActiveQuests.Clear();
        player.CompletedQuests.Clear();
        player.Buffs.Clear();
        player.Inventory.Clear();
        GiveStarterKit(player);

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendInventory(player);
        SendStats(player);
        SendLearned(player);
        SendQuestLog(player);
        SaveEntity(player);
        SendSystemToEntity(player, $"[DEBUG] Character reset to level 1 {cmd.Race} {cmd.BaseClass}.");
    }

    /// <summary>DEBUG: take a 3rd-class discipline directly (no quest/items). Forces
    /// the matching parent 2nd class if needed so the archetype stays consistent.</summary>
    private void HandleDebugThirdClass(DebugThirdClassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        if (ThirdClassCatalog.Get(cmd.ThirdClassId) is not ThirdClassDef tcd
            || tcd.Race != player.Race)
        {
            SendSystemToEntity(player, "[DEBUG] Invalid 3rd class.");
            return;
        }

        // Ensure the parent 2nd class (apply its core-stat bonus only if changing from none).
        if (player.SecondClass != tcd.ParentSecondClassId)
        {
            if (player.SecondClass == 0 && ClassCatalog.Get(tcd.ParentSecondClassId) is SecondClassDef p)
            {
                var (con, atk, wit, dex) = ClassCatalog.StatBonus(p.Archetype);
                player.Con += con; player.AtkStat += atk; player.Wit += wit; player.Dex += dex;
            }
            player.SecondClass = tcd.ParentSecondClassId;
        }
        player.ThirdClass = cmd.ThirdClassId;

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendStats(player);
        SendLearned(player);
        SaveEntity(player);
        BroadcastSystem($"{player.Name} has become a {tcd.Name}!");
    }

    /// <summary>Grant the new-character starter kit to a live entity (mirrors
    /// PersistenceService.CreateCharacterAsync). Items arrive unequipped.</summary>
    private void GiveStarterKit(Entity player)
    {
        var bodyWeight = player.BaseClass == BaseClass.Mage ? ArmorWeight.Robe : ArmorWeight.Light;

        if (player.BaseClass == BaseClass.Mage)
            AddItem(player, ItemCatalog.NewbieStaff);
        else
            foreach (var w in new[] { ItemCatalog.NewbieSword1H, ItemCatalog.NewbieDaggers,
                                      ItemCatalog.NewbieSword2H, ItemCatalog.NewbieBow })
                AddItem(player, w);
        AddItem(player, ItemCatalog.ArmorKey(bodyWeight, ArmorSlot.Body, ItemGrade.F, ItemRarity.Common));
        foreach (var slot in new[] { ArmorSlot.Head, ArmorSlot.Gloves, ArmorSlot.Boots })
            AddItem(player, ItemCatalog.ArmorKey(ArmorWeight.None, slot, ItemGrade.F, ItemRarity.Common));
        AddItem(player, ItemCatalog.MinorPotion, 5);
        AddItem(player, ItemCatalog.GreaterPotion, 2);
    }
#pragma warning restore CS1998

    private void HandleUsePotion(UsePotionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || ItemCatalog.Get(item.DefId) is not ItemDef def || !ItemCatalog.IsPotion(def))
            return;

        // Buff potion: apply its timed buff (independent of the heal cooldown), consume.
        if (ItemCatalog.IsBuffPotion(def) && SkillCatalog.Get(def.BuffSkillId) is SkillDef buffDef)
        {
            ApplyBuff(player, buffDef);
            PushBuffs(player);
            ConsumeOne(player, item);
            SendInventory(player);
            SendSystemToEntity(player, $"{buffDef.Name} active.");
            return;
        }

        if (player.PotionCooldown > 0)
        {
            SendSystemToEntity(player,
                $"Potions on cooldown ({player.PotionCooldown / GameConstants.TickRate}s).");
            return;
        }

        int rarity = (int)def.Rarity;

        // Instant potions (rare): heal now, no lingering effect.
        if (def.InstantHealPercent > 0)
        {
            int amount = Math.Max(1, (int)(player.MaxHp * def.InstantHealPercent));
            player.Hp = Math.Min(player.MaxHp, player.Hp + amount);
            BroadcastCombat(player, player, amount, CombatOutcome.Heal, def.Name);
            ClearPotionEffect(player); // a stronger instant cancels any HoT
        }
        else
        {
            // Heal-over-time. Rarity override: higher cancels lower; same restarts.
            // (Cooldown > effect duration means same-rarity restart shouldn't
            //  normally happen, but we handle it safely.)
            if (rarity >= player.PotionRarity)
            {
                player.PotionRarity = rarity;
                player.PotionHealPercentPerSecond = def.HealPercentPerSecond;
                player.PotionEffectTicks = def.PotionDurationTicks;
                player.PotionEffectName = def.Name;
            }
        }

        // Consume one potion from the stack and start the SHARED cooldown.
        ConsumeOne(player, item);
        player.PotionCooldown = def.PotionCooldownTicks;

        SendInventory(player);
        SendPotionStatus(player);
    }

    private static void ClearPotionEffect(Entity player)
    {
        player.PotionRarity = -1;
        player.PotionHealPercentPerSecond = 0f;
        player.PotionEffectTicks = 0;
        player.PotionEffectName = "";
    }

    /// <summary>Potion channel: ticks every second regardless of combat,
    /// independent of natural regen. Called from the regen tick.</summary>
    private void TickPotionHeal(Entity player)
    {
        if (player.PotionEffectTicks <= 0 || player.Dead)
            return;

        if (player.Hp < player.MaxHp)
        {
            int amount = Math.Max(1, (int)(player.MaxHp * player.PotionHealPercentPerSecond));
            player.Hp = Math.Min(player.MaxHp, player.Hp + amount);
            BroadcastCombat(player, player, amount, CombatOutcome.Heal, player.PotionEffectName);
        }
    }

    private void SendPotionStatus(Entity player)
    {
        float cd = player.PotionCooldown / (float)GameConstants.TickRate;
        SendTo(player, "Potion", new PotionStatus(cd, player.PotionEffectName));
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
            if (item is null || item.Equipped) continue;
            if (ItemCatalog.Get(item.DefId) is ItemDef d && (!d.Tradable || ItemCatalog.IsQuestItem(d)))
                continue;   // untradeable / quest items can't be traded
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
            TransferItem(session.A, session.B, item);
        foreach (var item in itemsB!)
            TransferItem(session.B, session.A, item);

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

    private void HandleAdmin(AdminCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var admin) || !admin.IsAdmin)
            return;

        var command = cmd.Command.ToLowerInvariant();
        var arg = cmd.Argument.Trim();

        switch (command)
        {
            case "help":
                SendSystemToEntity(admin,
                    "Admin: /kick <name>, /ban <name>, /unban <name>, /jail <name>, " +
                    "/unjail <name>, /god, /where <name>");
                break;

            case "god":
                admin.GodMode = !admin.GodMode;
                SendSystemToEntity(admin, $"God mode {(admin.GodMode ? "ON" : "OFF")}.");
                break;

            case "kick":
                if (FindOnlinePlayer(arg) is Entity kickTarget &&
                    _world.EntityToConnection.TryGetValue(kickTarget.Id, out var kickConn))
                {
                    SendSystemToEntity(kickTarget, "You have been kicked by an admin.");
                    SaveEntity(kickTarget);
                    _ = _hub.Clients.Client(kickConn).SendAsync("ForceDisconnect", "Kicked by admin.");
                    BroadcastSystem($"{kickTarget.Name} was kicked.");
                }
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            case "ban":
                BanPlayer(admin, arg, true);
                break;
            case "unban":
                BanPlayer(admin, arg, false);
                break;

            case "jail":
                if (FindOnlinePlayer(arg) is Entity jailTarget)
                {
                    jailTarget.Jailed = true;
                    jailTarget.X = GameConstants.JailX;
                    jailTarget.Y = GameConstants.JailY;
                    jailTarget.TargetX = null;
                    jailTarget.TargetY = null;
                    jailTarget.Engaged = false;
                    _world.Grid.UpdatePosition(jailTarget);
                    SendSystemToEntity(jailTarget, "You have been jailed.");
                    SendSystemToEntity(admin, $"{jailTarget.Name} jailed.");
                }
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            case "unjail":
                if (FindOnlinePlayer(arg) is Entity unjailTarget)
                {
                    unjailTarget.Jailed = false;
                    SendSystemToEntity(unjailTarget, "You have been released.");
                    SendSystemToEntity(admin, $"{unjailTarget.Name} released.");
                }
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            case "where":
                if (FindOnlinePlayer(arg) is Entity who)
                    SendSystemToEntity(admin, $"{who.Name} is at ({(int)who.X}, {(int)who.Y}).");
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            default:
                SendSystemToEntity(admin, $"Unknown command: {command}");
                break;
        }
    }

    private Entity? FindOnlinePlayer(string name) =>
        _world.Entities.Values.FirstOrDefault(e =>
            e.Kind == EntityKind.Player &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private void BanPlayer(Entity admin, string name, bool banned)
    {
        // Persist the ban (works even if the target is offline).
        _ = Task.Run(async () =>
        {
            bool ok = await _db.SetBannedByCharacterNameAsync(name, banned);
            // Kick if currently online and being banned.
            if (ok && banned && FindOnlinePlayer(name) is Entity target &&
                _world.EntityToConnection.TryGetValue(target.Id, out var conn))
            {
                _ = _hub.Clients.Client(conn).SendAsync("ForceDisconnect", "You have been banned.");
            }
        });
        SendSystemToEntity(admin, $"{(banned ? "Banned" : "Unbanned")} {name}.");
    }

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

        if (_tick % GameConstants.AutoSaveIntervalTicks == 0)
            AutoSaveAll();

        UpdateZones();

        foreach (var entity in _world.Entities.Values)
        {
            if (entity.AttackCooldown > 0)
                entity.AttackCooldown--;

            if (entity.Kind == EntityKind.Player)
                TickPotion(entity);

            TickSkillCooldowns(entity);
            TickBuffs(entity);

            if (entity.Dead)
                continue;

            if (entity.Kind == EntityKind.Mob)
                MobAi(entity);

            if (entity.Jailed)
            {
                // Pinned: ignore any movement/skills, keep them at jail.
                entity.TargetX = null;
                entity.TargetY = null;
                entity.X = GameConstants.JailX;
                entity.Y = GameConstants.JailY;
                entity.Engaged = false;
                _world.Grid.UpdatePosition(entity);
                continue;
            }

            UpdateAction(entity);
            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);

            if (regenTick)
            {
                TickHealOverTime(entity);   // HoT heals even in combat (unlike natural regen)
                Regenerate(entity);
                if (entity.Kind == EntityKind.Player)
                    PushBuffs(entity);
            }
        }
    }

    // Reusable scratch buffer for cooldown expiry so the per-tick decrement doesn't
    // allocate a Keys.ToList() for every entity in combat. Single-threaded loop, so
    // one shared buffer is safe (cleared and fully drained within each call).
    private readonly List<string> _expiredCooldowns = new();

    private void TickSkillCooldowns(Entity entity)
    {
        if (entity.SkillCooldowns.Count == 0)
            return;

        // Decrementing an existing key's VALUE doesn't structurally modify the
        // dictionary, so iterating Keys while updating is safe; only the removals
        // (collected here) are deferred until after the loop.
        _expiredCooldowns.Clear();
        foreach (var key in entity.SkillCooldowns.Keys)
        {
            if (--entity.SkillCooldowns[key] <= 0)
                _expiredCooldowns.Add(key);
        }

        foreach (var key in _expiredCooldowns)
            entity.SkillCooldowns.Remove(key);
    }

    private void TickPotion(Entity entity)
    {
        bool changed = false;

        if (entity.PotionCooldown > 0)
        {
            entity.PotionCooldown--;
            if (entity.PotionCooldown == 0)
                changed = true;
        }

        if (entity.PotionEffectTicks > 0)
        {
            // Heal-over-time fires once per second.
            if (entity.PotionEffectTicks % GameConstants.RegenIntervalTicks == 0)
                TickPotionHeal(entity);

            entity.PotionEffectTicks--;
            if (entity.PotionEffectTicks <= 0)
            {
                ClearPotionEffect(entity);
                changed = true;
            }
        }

        if (changed)
            SendPotionStatus(entity);
    }

    private void TickBuffs(Entity entity)
    {
        bool expiredAny = false;
        for (int i = entity.Buffs.Count - 1; i >= 0; i--)
        {
            if (--entity.Buffs[i].TicksRemaining <= 0)
            {
                entity.Buffs.RemoveAt(i);
                expiredAny = true;
            }
        }
        if (!expiredAny) return;

        // A buff fell off — re-bake derived stats and refresh the owner's HUD
        // (icons + stats window) so the lost speed/HP/etc. shows immediately.
        entity.RecomputeDerived();
        if (entity.Kind == EntityKind.Player)
        {
            PushBuffs(entity);
            SendStats(entity);
        }
    }

    // ----- Mob AI --------------------------------------------------------------

    private void MobAi(Entity mob)
    {
        if (mob.DetauntTicks > 0) mob.DetauntTicks--;

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

                // De-taunt: ignore the entity that just shed us, briefly.
                if (mob.DetauntTicks > 0 && mob.DetauntFromId == candidate.Id)
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
            float tx = mob.HomeX + _rng.Next(-1000, 1001);
            float ty = mob.HomeY + _rng.Next(-1000, 1001);

            // Keep wander inside the mob's own zone so they don't drift into
            // neighbours. Pull the target back toward the zone centre if outside.
            var zone = _zones.FirstOrDefault(z => z.Zone.Id == mob.ZoneId)?.Zone;
            if (zone is not null)
            {
                float dx = tx - zone.X, dy = ty - zone.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq > zone.Radius * zone.Radius)
                {
                    float dist = MathF.Sqrt(distSq);
                    float scale = zone.Radius / dist;
                    tx = zone.X + dx * scale;
                    ty = zone.Y + dy * scale;
                }
            }

            tx = Math.Clamp(tx, 0, GameConstants.ZoneWidth);
            ty = Math.Clamp(ty, 0, GameConstants.ZoneHeight);
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

    /// <summary>A mob died: remove it from the world and let its zone schedule
    /// the next spawn. Boss/elite respawn times are persisted to survive restarts.</summary>
    private void OnMobKilled(Entity mob)
    {
        _world.Grid.Remove(mob);
        _world.Entities.Remove(mob.Id, out _);

        var zr = _zones.FirstOrDefault(z => z.Zone.Id == mob.ZoneId);
        if (zr is null)
            return;

        zr.OnDeath(_tick, _rng);

        // Persist boss/elite respawn time (real-world) so it survives a restart.
        if (zr.Zone.Rank != MobRank.Normal && zr.NextPendingTick is long nextTick)
        {
            double secondsAway = (nextTick - _tick) / (double)GameConstants.TickRate;
            var respawnAt = DateTime.UtcNow.AddSeconds(secondsAway);
            _ = Task.Run(() => _db.SaveBossTimerAsync(zr.Zone.Id, respawnAt));
        }
    }

    // ----- Action state machine: casting > queued skill > auto-attack ------------

    private void UpdateAction(Entity entity)
    {
        if (entity.StandUpTicks > 0)
        {
            entity.StandUpTicks--;
            return;   // still recovering: no move/cast/attack this tick
        }

        if (entity.CastingSkillId is string castingId)
        {
            if (--entity.CastTicksRemaining <= 0)
            {
                entity.CastingSkillId = null;
                if (SkillCatalog.Get(castingId) is SkillDef def)
                    ExecuteSkill(entity, def);
            }
            return;
        }

        if (entity.QueuedSkillId is string queuedId)
        {
            UpdateQueuedSkill(entity, queuedId);
            return;
        }

        if (entity.Engaged)
            UpdateAutoAttack(entity);
    }

    private void UpdateQueuedSkill(Entity caster, string skillId)
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

        float range = SkillMath.EffectiveRange(def, caster.Archetype, caster.BasicAttackRange, caster.Level);

        if (!selfTargeted && DistanceSq(caster, target) > range * range)
        {
            caster.TargetX = target.X;
            caster.TargetY = target.Y;
            return;
        }

        // Casting commits: you're rooted until it finishes or you cancel. Range
        // is checked HERE (at start); once casting begins, the spell lands even
        // if the target moves.
        caster.TargetX = null;
        caster.TargetY = null;
        caster.QueuedSkillId = null;
        caster.CastingSkillId = def.Id;
        caster.CastTargetId = targetId;
        // Cast time = base ticks scaled by the cast-speed model (WIT + weapon +
        // buffs, the single source of truth). Lower multiplier = faster cast.
        caster.CastTicksRemaining = Math.Max(2,
            (int)(def.CastTicks * caster.EffectiveCastSpeedMultiplier));

        // Charge the initial MP portion up front (default 0; split skills charge
        // some now, the rest on completion). Level-aware MP cost.
        caster.CastInitialMpPaid = Math.Min(def.InitialMpAt(Math.Max(1, caster.SkillLevelOf(def.Id))), caster.Mp);
        caster.Mp -= caster.CastInitialMpPaid;

        if (_world.EntityToConnection.TryGetValue(caster.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Cast", new CastInfo(
                def.Name, caster.CastTicksRemaining * GameConstants.TickSeconds));
        }
    }

    private void ExecuteSkill(Entity caster, SkillDef def)
    {
        // The caster's learned LEVEL of this skill selects its per-level values
        // (Power / Magnitudes / MP). Default 1 for anything not in the learned set.
        int lvl = Math.Max(1, caster.SkillLevelOf(def.Id));

        if (caster.Mp < def.FinishMpAt(lvl))
        {
            SendSystemToEntity(caster, "Not enough MP.");
            CancelCast(caster);
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

        // Cast already committed at start — no range re-check here; the spell
        // lands even if the target moved. Charge the remaining MP and start CD.
        caster.Mp -= def.FinishMpAt(lvl);
        caster.CastInitialMpPaid = 0;
        // Reuse-delay reduction (Spell Mastery / buffs) shortens the cooldown.
        int cooldown = def.CooldownTicks;
        if (cooldown > 0 && caster.CooldownReduction > 0f)
            cooldown = Math.Max(1, (int)(cooldown * (1f - caster.CooldownReduction)));
        caster.SkillCooldowns[def.Id] = cooldown;

var effect = def.Effect;
        bool offensive = false;
        // The name shown in combat text is the CASTER's class label for this skill, so a
        // race's renamed spell (e.g. Elf "Moonlight Bolt") reads correctly in floating text.
        string castName = ClassSkills.DisplayName(
            def.Id, caster.Race, caster.BaseClass, caster.Archetype, caster.Discipline);

        // ---- Damage (physical) ----
        if (effect.HasFlag(SkillEffect.PhysicalDamage))
        {
            offensive = true;
            float miss = StatCalculator.ResolveAvoidChance(
                caster.Accuracy + SkillMath.PhysicalSkillAccuracyBonus, (int)target.EffectiveEvasion,
                target.EvadeFloor, caster.HitFloor,
                caster.Level, target.Level,
                sureHit: def.SureHit, defenderImmune: target.Immune);

            if (_rng.NextDouble() < miss)
            {
                BroadcastCombat(caster, target, 0, CombatOutcome.Miss, castName);
            }
            else
            {
                int damage = StatCalculator.PhysicalDamage(
                    (int)caster.EffectiveAttack, def.PowerAt(lvl),
                    (int)target.EffectiveDefence, caster.Level);
                damage = (int)(damage * StatCalculator.WeaponVariance(caster.WeaponType, _rng));

                var (finalDmg, outcome) = ResolvePhysicalCritAndBlock(
                    caster, target, damage, caster.CritChance, def.BlockAccuracy);
                damage = finalDmg;
                BroadcastCombat(caster, target, damage, outcome, castName);
                ApplyDamage(target, damage);
                TryInterruptCast(target, def.InterruptPower);
            }
        }

        // ---- Damage (magic) ----
        if (effect.HasFlag(SkillEffect.MagicDamage))
        {
            offensive = true;
            int damage = StatCalculator.MagicDamage(
                (int)caster.EffectiveMagicAttack, def.PowerAt(lvl),
                (int)target.EffectiveMagicDefence, caster.Level);   // magic channel: divides by mDef
            damage = (int)(damage * StatCalculator.WeaponVariance(caster.WeaponType, _rng));

            // WIT drives the caster's offensive magic interrupt power on top of the
            // skill's flat InterruptPower (Disrupt's 99999 still dominates).
            int magicInterrupt = def.InterruptPower + caster.MagicInterruptBonus;

            // Magic "fail" = reduced damage (not zero). Unified resolver: stat term is
            // 0 (no magic pen/resist race yet) so same-level magic sits at the 5% base;
            // the anti-magic floor raises it and the level-gap curve locks out farming up.
            float fail = StatCalculator.ResolveAvoidChance(
                0, 0, target.MagicFailFloor, 0f,
                caster.Level, target.Level,
                sureHit: def.SureHit, defenderImmune: target.Immune,
                baseAvoid: target.Kind == EntityKind.Mob ? 0.01f : -1f);
            if (caster.MagicFailResist > 0f) fail = Math.Max(0f, fail - caster.MagicFailResist);
            if (_rng.NextDouble() < fail)
            {
                damage = Math.Max(1, damage / 3);
                ApplyDamage(target, damage);
                TryInterruptCast(target, magicInterrupt);
                BroadcastCombat(caster, target, damage, CombatOutcome.Fail, castName);
            }
            else
            {
                if (_rng.NextDouble() < caster.MagicCritChance)
                {
                    damage = (int)(damage * StatCalculator.MagicCritMult(caster.CritDamageBonus));
                    BroadcastCombat(caster, target, damage, CombatOutcome.Crit, castName);
                }
                else
                {
                    BroadcastCombat(caster, target, damage, CombatOutcome.Hit, castName);
                }
                ApplyDamage(target, damage);
                TryInterruptCast(target, magicInterrupt);
            }

            // Vampiric: heal the caster for a fraction of the magic damage dealt
            // (the skill's own Lifesteal plus any Spell Vamp buff).
            float spellVamp = def.Lifesteal + caster.SpellVamp;
            if (spellVamp > 0f && damage > 0)
            {
                int leech = (int)(damage * spellVamp);
                if (leech > 0) HealOne(caster, caster, leech, castName);
            }
        }

        // ---- Heal (single ally/self, or AoE to allies in radius) ----
        // Flat power (scales with WIT) plus an optional % of the TARGET's max HP
        // (a Percent magnitude on the Heal effect).
        if (effect.HasFlag(SkillEffect.Heal))
        {
            int flat = SkillMath.HealAmount(def.PowerAt(lvl), caster.EffectiveWit);
            float pct = def.MagnitudeOf(SkillEffect.Heal, ModifierMode.Percent, lvl);
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                    HealOne(caster, ally, flat + (int)(ally.MaxHp * pct), castName);
            else
                HealOne(caster, target, flat + (int)(target.MaxHp * pct), castName);
        }

        // ---- MP Restore (single ally/self, or AoE) — flat power (+optional % of max MP) ----
        if (effect.HasFlag(SkillEffect.RestoreMp))
        {
            int flat = def.PowerAt(lvl);
            float pct = def.MagnitudeOf(SkillEffect.RestoreMp, ModifierMode.Percent, lvl);
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                    RestoreMpOne(caster, ally, flat + (int)(ally.MaxMp * pct), castName);
            else
                RestoreMpOne(caster, target, flat + (int)(target.MaxMp * pct), castName);
        }

        // ---- Cleanse — remove harmful effects from an ally (or allies in radius) ----
        if (effect.HasFlag(SkillEffect.Cleanse))
        {
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                    CleanseDebuffs(caster, ally, castName);
            else
                CleanseDebuffs(caster, target, castName);
        }

        // ---- Debuffs (defence curse / anti-heal / root) — can fizzle like a spell ----
        if ((effect & SkillEffect.AnyDebuff) != 0)
        {
            offensive = true;
            float fail = StatCalculator.ResolveAvoidChance(
                0, 0, target.MagicFailFloor, 0f,
                caster.Level, target.Level,
                sureHit: def.SureHit, defenderImmune: target.Immune,
                baseAvoid: target.Kind == EntityKind.Mob ? 0.01f : -1f);
            if (caster.MagicFailResist > 0f) fail = Math.Max(0f, fail - caster.MagicFailResist);
            if (_rng.NextDouble() < fail)
            {
                BroadcastCombat(caster, target, 0, CombatOutcome.Fail, castName);
            }
            else
            {
                ApplyBuff(target, def, lvl);
                BroadcastCombat(caster, target, 0, CombatOutcome.Buff, castName);
            }
        }

        // ---- De-taunt — shed the caster's aggro from nearby foes (stub) ----
        if (effect.HasFlag(SkillEffect.Detaunt))
            Detaunt(caster);

        // ---- Beneficial buffs (any of the buff flags) ----
        if ((effect & SkillEffect.AnyBuff) != 0)
        {
            // The display name is the CASTER's class label for this skill, so a
            // cleric's Wind Walk shows as "Holy Speed" wherever it lands.
            string buffName = ClassSkills.DisplayName(
                def.Id, caster.Race, caster.BaseClass, caster.Archetype, caster.Discipline);

            if (def.TargetMode == TargetMode.AlliesInRadius)
            {
                // Buff the caster + every nearby player character in range.
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                {
                    ApplyBuff(ally, def, lvl, buffName);
                    BroadcastCombat(caster, ally, 0, CombatOutcome.Buff, buffName);
                }
            }
            else
            {
                var buffTarget = def.TargetMode == TargetMode.SelfOnly ? caster : target;
                ApplyBuff(buffTarget, def, lvl, buffName);
                BroadcastCombat(caster, buffTarget, 0, CombatOutcome.Buff, buffName);
            }
        }

        if (offensive)
            AfterOffensiveSkill(caster, target);

                if (target.Hp <= 0 && !target.Dead)
            Kill(target, caster);
    }

    /// <summary>Apply a buff with the two stacking rules:
    /// (1) Same BuffKey: apply only if the incoming Rank >= existing Rank
    ///     (weaker self-recast is ignored entirely); on apply, replace it.
    /// (2) Replaces: unconditionally remove any active buff whose key is listed,
    ///     regardless of rank or magnitude.</summary>
    private void ApplyBuff(Entity target, SkillDef def, int level = 1, string? displayName = null)
    {
        string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        string shownName = string.IsNullOrEmpty(displayName) ? def.Name : displayName!;

        // Rule 1 — same-key rank comparison.
        var same = target.Buffs.FirstOrDefault(b => b.Key == key);
        if (same is not null)
        {
            if (def.Rank < same.Rank)
                return;                         // weaker: do nothing (no refresh)
            target.Buffs.Remove(same);          // equal/stronger: full replace
        }

        // Rule 2 — explicit Replaces list (unconditional).
        if (def.Replaces is { Length: > 0 })
            target.Buffs.RemoveAll(b => def.Replaces.Contains(b.Key));

        target.Buffs.Add(new BuffInstance
        {
            Effect = def.Effect,
            Magnitudes = def.MagnitudesAt(level) ?? Array.Empty<EffectMagnitude>(),
            TicksRemaining = def.DurationTicks,
            Name = shownName,
            Key = key,
            Rank = def.Rank,
            Replaces = def.Replaces ?? Array.Empty<string>(),
            Description = SkillCatalog.DescriptionOf(def.Id)
        });

        // Re-bake derived stats (Max HP/MP, shield, atk/def) and refresh the owner's
        // HUD: buff icons AND the stats window (cast/attack/move speed are live and
        // read the buff list, so they need a fresh push to show the new numbers).
        target.RecomputeDerived();
        if (target.Kind == EntityKind.Player)
        {
            PushBuffs(target);
            SendStats(target);
        }
    }

    /// <summary>Heal one target, scaled by its anti-heal multiplier, and broadcast.</summary>
    private void HealOne(Entity caster, Entity target, int baseAmount, string skillName)
    {
        if (target.Dead) return;
        int amount = (int)Math.Round(baseAmount * target.HealReceivedMultiplier);
        if (amount > 0)
            target.Hp = Math.Min(target.MaxHp, target.Hp + amount);
        BroadcastCombat(caster, target, amount, CombatOutcome.Heal, skillName);
    }

    /// <summary>Restore one target's MP and broadcast it (mirrors HealOne for the MP
    /// channel; used by MP Restore skills).</summary>
    private void RestoreMpOne(Entity caster, Entity target, int amount, string skillName)
    {
        if (target.Dead) return;
        if (amount > 0)
            target.Mp = Math.Min(target.MaxMp, target.Mp + amount);
        BroadcastCombat(caster, target, amount, CombatOutcome.ManaHeal, skillName);
        if (target.Kind == EntityKind.Player)
            SendStats(target);   // MP isn't surfaced via damage broadcasts — refresh the bar
    }

    /// <summary>Strip all harmful effects (curses, anti-heal, roots) from an ally.</summary>
    private void CleanseDebuffs(Entity caster, Entity target, string skillName)
    {
        if (target.Dead) return;
        int removed = target.Buffs.RemoveAll(b => (b.Effect & SkillEffect.AnyDebuff) != 0);
        if (removed > 0)
        {
            target.RecomputeDerived();   // DebuffDef etc. affected derived stats
            if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
        }
        BroadcastCombat(caster, target, 0, CombatOutcome.Buff, skillName);
    }

    /// <summary>De-taunt stub: nearby mobs targeting the caster drop it and won't
    /// re-aggro the caster for a short window (no real threat system yet).</summary>
    private void Detaunt(Entity caster)
    {
        const int window = 50;   // 5s at 10 ticks/s
        float rangeSq = GameConstants.MobAggroRange * GameConstants.MobAggroRange * 4f;
        foreach (var e in _world.Entities.Values)
        {
            if (e.Kind != EntityKind.Mob || e.Dead) continue;
            if (e.CombatTargetId != caster.Id) continue;
            if (DistanceSq(caster, e) > rangeSq) continue;
            e.Engaged = false;
            e.CombatTargetId = null;
            e.DetauntTicks = window;
            e.DetauntFromId = caster.Id;
            e.TargetX = e.HomeX;
            e.TargetY = e.HomeY;
        }
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
        // A damaged mob aggroes its attacker (even if hit from beyond its normal
        // aggro range — being attacked always provokes) and starts chasing now.
        if (victim.Kind == EntityKind.Mob && !victim.Dead)
        {
            victim.CombatTargetId = attacker.Id;
            victim.Engaged = true;
            victim.TargetX = attacker.X;   // start moving toward the attacker
            victim.TargetY = attacker.Y;
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

        int baseInterval = attacker.Kind == EntityKind.Player
            ? GameConstants.PlayerAttackIntervalTicks
            : GameConstants.MobAttackIntervalTicks;
        attacker.AttackCooldown = Math.Max(2,
            (int)(baseInterval * attacker.EffectiveAttackSpeedMultiplier));

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
        float missChance = StatCalculator.ResolveAvoidChance(
            attacker.Accuracy, (int)target.EffectiveEvasion,
            target.EvadeFloor, attacker.HitFloor,
            attacker.Level, target.Level,
            sureHit: false, defenderImmune: target.Immune);

        if (_rng.NextDouble() < missChance)
        {
            BroadcastCombat(attacker, target, 0, CombatOutcome.Miss);
        }
        else
        {
            int damage = StatCalculator.PhysicalDamage(
                (int)attacker.EffectiveBasicAttack, 0,
                (int)target.EffectiveDefence, attacker.Level);
            damage = (int)(damage * StatCalculator.WeaponVariance(attacker.WeaponType, _rng));

            var (finalDmg, outcome) = ResolvePhysicalCritAndBlock(
                attacker, target, damage, attacker.CritChance, 0f);
            damage = finalDmg;
            BroadcastCombat(attacker, target, damage, outcome);
            ApplyDamage(target, damage);
            // Melee basic-attack vampirism (Might lvl 4 etc.) — bow attacks don't leech.
            if (attacker.MeleeVamp > 0f && damage > 0 && attacker.WeaponType != WeaponType.Bow)
            {
                int leech = (int)(damage * attacker.MeleeVamp);
                if (leech > 0) HealOne(attacker, attacker, leech, "Vampiric");
            }
            // Rogues carry magic-interrupt power on basic attacks; others = 0.
            TryInterruptCast(target, attacker.BasicAttackInterruptPower);
        }

        Retaliate(target, attacker);

        if (target.Hp <= 0)
            Kill(target, attacker);
    }

    /// <summary>Apply damage unless the target is in god mode.</summary>
    private static int ApplyDamage(Entity target, int damage)
    {
        if (target.GodMode)
            return 0;
        target.Hp -= damage;

        // Being hit while sitting breaks the sit and starts the stand-up window:
        // you can't move/cast until it elapses.
        if (target.Kind == EntityKind.Player && target.MoveState == MoveState.Sitting)
        {
            target.MoveState = MoveState.Running;
            target.StandUpTicks = MovementTuning.StandUpTicks;
        }
        return damage;
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
            if (killer.Kind == EntityKind.Player)
            {
                AwardExp(killer, StatCalculator.MobExpReward(victim.Level));
                RollDrop(killer, victim);
                AdvanceKillQuests(killer, victim);
            }

            OnMobKilled(victim);
        }
        else
        {
            CancelTradeFor(victim, notifyPartnerOnly: false);
            BroadcastSystem($"{victim.Name} was slain by {killer.Name}.");
        }
    }

    private void RollDrop(Entity killer, Entity mob)
    {
        if (mob.MobTypeId is null)
            return;

        // Gold always drops (independent of the item table), by mob level x rate
        // with a small +/-20% variance.
        int gold = (int)(StatCalculator.MobGoldReward(mob.Level) * RateConfig.GoldAmountRate
            * (0.8f + (float)_rng.NextDouble() * 0.4f));
        if (gold > 0)
        {
            killer.Gold += gold;
            SendGold(killer);
        }

        var mobType = MobCatalog.Get(mob.MobTypeId);
        if (mobType.Drops is null || mobType.Drops.Length == 0)
            return;

        bool looted = false;
        foreach (var entry in mobType.Drops)
        {
            // Level band: a drop can be restricted to a level range (0/0 = any).
            if (!entry.AppliesAtLevel(mob.Level))
                continue;

            // Per-entry chance, scaled by the server drop-chance rate (clamped 100%).
            float chance = Math.Min(1f, entry.Chance * RateConfig.DropChanceRate);
            if (_rng.NextDouble() > chance)
                continue;

            if (ItemCatalog.Get(entry.ItemId) is not ItemDef def)
                continue;

            // Quantity range, scaled by the drop-amount rate.
            int qty = _rng.Next(entry.MinQty, entry.MaxQty + 1);
            qty = Math.Max(1, (int)(qty * RateConfig.DropAmountRate));

            if (!AddItem(killer, def.Id, qty))
            {
                SendSystemToEntity(killer, $"{mob.Name} dropped {def.Name} — inventory full!");
                continue;
            }

            string qtyLabel = qty > 1 ? $" x{qty}" : "";
            SendSystemToEntity(killer, $"You looted: {def.Name}{qtyLabel} [{def.Grade}/{def.Rarity}]");
            looted = true;
        }

        if (looted)
            SendInventory(killer);
    }

    private void AwardExp(Entity player, int amount)
    {
        // Server rates scale progression (x10 exp for testing, etc.).
        int expGain = (int)(amount * RateConfig.ExpRate);
        player.Exp += expGain;
        // Skill points accrue at a fraction of exp, with their own rate.
        player.SkillPoints += Math.Max(1,
            (int)(amount * GameConstants.SkillPointRatio * RateConfig.SpRate));

        bool leveled = false;
        while (player.Exp >= StatCalculator.ExpToNext(player.Level))
        {
            player.Exp -= StatCalculator.ExpToNext(player.Level);
            player.Level++;
            leveled = true;
        }

        if (leveled)
            OnLevelUp(player);

        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), leveled));
        }
    }

    /// <summary>Side-effects shared by every level-up path (real exp or debug):
    /// recompute stats, full heal, push stats/skills, announce.</summary>
    private void OnLevelUp(Entity player)
    {
        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;
        SendStats(player);
        SendLearned(player);
        BroadcastSystem($"{player.Name} reached level {player.Level}!");

        if (player.Level >= GameConstants.ClassChangeLevel && player.SecondClass == 0)
            SendSystemToEntity(player,
                "You are ready for a second class — seek a class-change quest.");
    }

    private void Regenerate(Entity entity)
    {
        if (entity.Engaged || entity.CastingSkillId is not null)
            return;

        float multiplier = entity.Kind == EntityKind.Player &&
                           GameConstants.InSafeZone(entity.X, entity.Y)
            ? GameConstants.SafeZoneRegenMultiplier
            : 1f;

        // Movement-state bonus (Walking +20%, Sitting +80%) for players.
        if (entity.Kind == EntityKind.Player)
            multiplier *= MovementTuning.RegenMultiplier(entity.MoveState);

        // Regen buffs (e.g. Warchanter's chant): +% to HP/MP regen.
        float hpRegenPct = 0f, mpRegenPct = 0f;
        foreach (var b in entity.Buffs)
        {
            if (b.Has(SkillEffect.BuffHpRegen)) hpRegenPct += b.Percent(SkillEffect.BuffHpRegen);
            if (b.Has(SkillEffect.BuffMpRegen)) mpRegenPct += b.Percent(SkillEffect.BuffMpRegen);
        }

        if (entity.Hp < entity.MaxHp)
        {
            int regen = Math.Max(1,
                (int)((StatCalculator.HpRegenPerSecond(entity.Con, entity.Level) + entity.HpRegenBonus)
                      * multiplier * entity.HpRegenMult * (1f + hpRegenPct)));
            entity.Hp = Math.Min(entity.MaxHp, entity.Hp + regen);
        }

        if (entity.Mp < entity.MaxMp)
        {
            int regen = Math.Max(1,
                (int)((StatCalculator.MpRegenPerSecond(entity.EffectiveWit, entity.Level) + entity.MpRegenBonus)
                      * multiplier * entity.MpRegenMult * (1f + mpRegenPct)));
            entity.Mp = Math.Min(entity.MaxMp, entity.Mp + regen);
        }
    }

    /// <summary>Heal-over-time buffs (e.g. Warchanter's Renew): heal a % of max HP
    /// each second, in or out of combat, until the buff expires.</summary>
    private void TickHealOverTime(Entity entity)
    {
        if (entity.Dead || entity.Hp >= entity.MaxHp)
            return;
        float pct = 0f;
        foreach (var b in entity.Buffs)
            if (b.Has(SkillEffect.HealOverTime)) pct += b.Percent(SkillEffect.HealOverTime);
        if (pct <= 0f)
            return;
        int heal = Math.Max(1, (int)(entity.MaxHp * pct));
        int before = entity.Hp;
        entity.Hp = Math.Min(entity.MaxHp, entity.Hp + heal);
        int healed = entity.Hp - before;
        if (healed > 0)
            BroadcastCombat(entity, entity, healed, CombatOutcome.Heal, "Regen");
    }

    // ----- Movement --------------------------------------------------------------

    private static void MoveTowardTarget(Entity e)
    {
        if (e.TargetX is not float tx || e.TargetY is not float ty)
            return;

        float dx = tx - e.X;
        float dy = ty - e.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float step = e.EffectiveSpeed * GameConstants.TickSeconds;

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

    /// <summary>Add an item to inventory, stacking consumables/scrolls.
    /// Returns false if there was no room for a new stack.</summary>
    private bool AddItem(Entity player, string defId, int quantity = 1, bool rollAttributes = true)
    {
        if (ItemCatalog.Get(defId) is not ItemDef def)
            return false;

        bool stackable = def.Slot is EquipSlot.Consumable or EquipSlot.Scroll;
        if (stackable)
        {
            var existing = player.Inventory.FirstOrDefault(i => i.DefId == defId);
            if (existing is not null)
            {
                existing.Quantity += quantity;
                return true;
            }
        }

        if (player.Inventory.Count >= GameConstants.InventorySize)
            return false;

        var newItem = new InventoryItem { DefId = defId, Quantity = stackable ? quantity : 1 };
        if (rollAttributes && def.Slot is EquipSlot.Weapon or EquipSlot.Armor)
            newItem.Attributes = def.FixedAttributes is { Length: > 0 } fixedAttrs
                ? fixedAttrs.ToList()                    // legendary one-off
                : AttributeSystem.Roll(def, _rng);       // normal random roll
        player.Inventory.Add(newItem);
        return true;
    }

    /// <summary>Move an item from one player to another, merging stacks of
    /// consumables/scrolls on the receiving side.</summary>
    private static void TransferItem(Entity from, Entity to, InventoryItem item)
    {
        from.Inventory.Remove(item);

        if (ItemCatalog.Get(item.DefId) is ItemDef def &&
            def.Slot is EquipSlot.Consumable or EquipSlot.Scroll)
        {
            var existing = to.Inventory.FirstOrDefault(i => i.DefId == item.DefId);
            if (existing is not null)
            {
                existing.Quantity += item.Quantity;
                return;
            }
        }

        to.Inventory.Add(item);
    }

    /// <summary>Consume one from a stack; removes the stack at zero.</summary>
    private static void ConsumeOne(Entity player, InventoryItem item)
    {
        item.Quantity--;
        if (item.Quantity <= 0)
            player.Inventory.Remove(item);
    }

    private void SendInventory(Entity player) =>
        SendTo(player, "Inventory", new InventoryUpdate(
            player.Inventory.Select(i => i.ToDto()).ToArray()));

    private void SendGold(Entity player) =>
        SendTo(player, "Gold", new GoldUpdate(player.Gold));

    private readonly HashSet<Guid> _hadBuffs = new();

    /// <summary>Caster + nearby player characters within radius (party stand-in
    /// until real groups exist). Uses the grid's neighbourhood for efficiency.</summary>
    private IEnumerable<Entity> PlayersInRadius(Entity caster, float radius)
    {
        float r2 = radius * radius;
        yield return caster;
        foreach (var e in _world.Grid.Nearby(caster))
        {
            if (e.Kind != EntityKind.Player || e.Dead || e.Id == caster.Id)
                continue;
            float dx = e.X - caster.X, dy = e.Y - caster.Y;
            if (dx * dx + dy * dy <= r2)
                yield return e;
        }
    }

    private void PushBuffs(Entity player)
    {
        if (player.Buffs.Count == 0)
        {
            // Only send the empty update once, when buffs just expired.
            if (_hadBuffs.Remove(player.Id))
                SendTo(player, "Buffs", new BuffUpdate(Array.Empty<BuffDto>()));
            return;
        }

        _hadBuffs.Add(player.Id);
        var dtos = player.Buffs.Select(b => new BuffDto(
            b.Name, b.Description,
            b.TicksRemaining * GameConstants.TickSeconds, b.IsDebuff, b.Key)).ToArray();
        SendTo(player, "Buffs", new BuffUpdate(dtos));
    }

    private void SendLearned(Entity p) =>
        SendTo(p, "Learned", new LearnedSkills(
            p.LearnedSkills.Select(kv => new SkillRef(kv.Key, kv.Value)).ToArray(), p.SkillPoints));

    private void SendStats(Entity p)
    {
        var (hpReg, mpReg) = StandingRegen(p);
        SendTo(p, "Stats", new StatsUpdate(
            p.Con, p.AtkStat, p.EffectiveWit, p.EffectiveDex,
            p.MaxHp, p.MaxMp, (int)p.EffectiveAttack, (int)p.EffectiveDefence,
            p.Accuracy, (int)p.EffectiveEvasion, p.CritChance, p.BasicAttackRange, p.SecondClass,
            p.EffectiveSpeed, SkillMath.CastModifier(p.Wit), p.EffectiveCastSpeedMultiplier, p.EffectiveAttackSpeedMultiplier, p.SkillPoints, p.MoveState, (int)p.EffectiveMagicAttack, p.MagicCritChance,
            p.HasShield, p.BlockChance, p.BlockReduction, p.ShieldDefense, (int)p.EffectiveMagicDefence,
            p.ActiveArmorSet, p.ArmorMasteryLabel,
            hpReg, mpReg, p.CritDamageBonus,
            p.MeleeVamp, p.SpellVamp, p.CooldownReduction,
            p.MagicFailResist, p.MagicFailFloor,
            p.CritRateResist, p.CritDmgResist, p.BowResist,
            p.InterruptResist));
    }

    /// <summary>The player's STANDING (out-of-combat, running) HP/MP regen per second —
    /// base + flat bonus, ×mastery mult, ×buff regen% — for the stats window.</summary>
    private static (float Hp, float Mp) StandingRegen(Entity p)
    {
        float hpPct = 0f, mpPct = 0f;
        foreach (var b in p.Buffs)
        {
            if (b.Has(SkillEffect.BuffHpRegen)) hpPct += b.Percent(SkillEffect.BuffHpRegen);
            if (b.Has(SkillEffect.BuffMpRegen)) mpPct += b.Percent(SkillEffect.BuffMpRegen);
        }
        float hp = (StatCalculator.HpRegenPerSecond(p.Con, p.Level) + p.HpRegenBonus) * p.HpRegenMult * (1f + hpPct);
        float mp = (StatCalculator.MpRegenPerSecond(p.EffectiveWit, p.Level) + p.MpRegenBonus) * p.MpRegenMult * (1f + mpPct);
        return (hp, mp);
    }

    /// <summary>Stop an in-progress cast. startCooldown=true (player ESC) puts
    /// the skill on cooldown; false (enemy interrupt / forced) does not, so the
    /// caster can retry. The initial MP already paid is NOT refunded.</summary>
    private void CancelCast(Entity entity, bool startCooldown = false)
    {
        if (entity.CastingSkillId is null)
            return;

        if (startCooldown && SkillCatalog.Get(entity.CastingSkillId) is SkillDef def)
            entity.SkillCooldowns[def.Id] = def.CooldownTicks;

        entity.CastingSkillId = null;
        entity.CastTargetId = null;
        entity.CastTicksRemaining = 0;
        entity.CastInitialMpPaid = 0;
        SendTo(entity, "Cast", new CastInfo("", 0f));
    }

    /// <summary>Player pressed ESC to cancel their own cast — starts cooldown.</summary>
    private void HandleCancelCast(CancelCastCmd cmd)
    {
        if (TryGetPlayer(cmd.ConnectionId, out var player))
            CancelCast(player, startCooldown: true);
    }

    /// <summary>Player manually dropped a buff (double-click). Debuffs can't be removed
    /// this way. Re-bakes stats + refreshes the buff bar so the loss shows immediately.</summary>
    private void HandleRemoveBuff(RemoveBuffCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        int removed = player.Buffs.RemoveAll(b => b.Key == cmd.BuffKey && !b.IsDebuff);
        if (removed == 0) return;
        player.RecomputeDerived();
        PushBuffs(player);
        SendStats(player);
    }

    /// <summary>Roll to interrupt a cast when the caster is hit. Resist = caster
    /// stat + the casting skill's InterruptDefense; power = attacker's skill
    /// InterruptPower (0 for normal hits). Interrupt = cast stops, NO cooldown,
    /// caster keeps the MP loss and can retry.</summary>
    /// <summary>Resolve crit and block for a physical hit. The shield first
    /// reduces the attacker's crit CHANCE; if it still crits, the crit lands in
    /// full (crits ignore the shield). If it doesn't crit, roll block — on a
    /// block, apply the shield's flat % damage reduction. blockAccuracy (from a
    /// skill) lowers the effective block chance (most phys skills bypass blocks).
    /// Returns the final damage and the outcome (Crit / Block / Hit).</summary>
    private (int damage, CombatOutcome outcome) ResolvePhysicalCritAndBlock(
        Entity attacker, Entity target, int baseDamage, float critChance, float blockAccuracy)
    {
        // Bow/arrow resistance lowers all damage from a bow attacker (hit/crit/block alike).
        if (attacker.WeaponType == WeaponType.Bow && target.BowResist > 0f)
            baseDamage = Math.Max(1, (int)(baseDamage * (1f - target.BowResist)));

        // Shield AND the target's crit-rate resist lower the attacker's crit CHANCE.
        float effCrit = critChance
            - (target.HasShield ? target.ShieldCritDefense : 0f)
            - target.CritRateResist;
        effCrit = Math.Clamp(effCrit, 0f, 1f);

        if (_rng.NextDouble() < effCrit)
        {
            // Crit-damage resist trims the EXTRA (above-normal) crit damage.
            float mult = StatCalculator.PhysicalCritMult(attacker.CritDamageBonus);
            float extra = (mult - 1f) * (1f - target.CritDmgResist);
            int crit = Math.Max(1, (int)(baseDamage * (1f + extra)));
            return (crit, CombatOutcome.Crit);   // crit ignores the shield
        }

        // Not a crit — try to block (if target has a shield and skill doesn't bypass).
        if (target.HasShield)
        {
            float effBlock = Math.Clamp(target.BlockChance - blockAccuracy, 0f, StatCaps.BlockChance);
            if (_rng.NextDouble() < effBlock)
            {
                int blocked = Math.Max(1, (int)(baseDamage * (1f - target.BlockReduction)));
                return (blocked, CombatOutcome.Block);
            }
        }

        return (baseDamage, CombatOutcome.Hit);
    }

    private void TryInterruptCast(Entity target, int attackerInterruptPower)
    {
        if (target.CastingSkillId is null)
            return;
        var def = SkillCatalog.Get(target.CastingSkillId);
        if (def is null)
            return;

        float chance = StatCalculator.InterruptChance(
            target.InterruptResist, def.InterruptDefense, attackerInterruptPower);
        if (_rng.NextDouble() < chance)
        {
            CancelCast(target, startCooldown: false);
            SendSystemToEntity(target, $"{def.Name} was interrupted!");
        }
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

    /// <summary>Build zone runtimes, restore persisted boss timers, then fill
    /// each zone to its cap for the current time of day.</summary>
    private async Task InitZonesAsync()
    {
        _zones.Clear();
        foreach (var zone in WorldMap.SpawnZones)
            _zones.Add(new ZoneRuntime(zone));

        _lastPhase = GameClock.CurrentPhase(DateTime.UtcNow);

        // Restore boss/elite timers so long respawns survive a restart.
        var timers = await _db.LoadBossTimersAsync();

        foreach (var zr in _zones)
        {
            // Boss/elite with a persisted "still dead" timer: schedule instead of fill.
            if (zr.Zone.Rank != MobRank.Normal &&
                timers.TryGetValue(zr.Zone.Id, out var respawnAt))
            {
                if (respawnAt > DateTime.UtcNow)
                {
                    long ticks = _tick + (long)((respawnAt - DateTime.UtcNow).TotalSeconds * GameConstants.TickRate);
                    zr.ScheduleAt(ticks);
                    continue; // don't spawn yet
                }
            }

            int fill = zr.InitialFill(_lastPhase);
            for (int i = 0; i < fill; i++)
                SpawnOneInZone(zr);
        }
    }

    /// <summary>Per-tick: spawn any matured respawns (cap + time-of-day aware),
    /// and when the day/night phase flips, swap day-only/night-only populations.</summary>
    private void UpdateZones()
    {
        var phase = GameClock.CurrentPhase(DateTime.UtcNow);

        if (phase != _lastPhase)
        {
            _lastPhase = phase;
            OnPhaseChanged(phase);
        }

        foreach (var zr in _zones)
        {
            int due = zr.DueToSpawn(_tick, phase);
            for (int i = 0; i < due; i++)
                SpawnOneInZone(zr);
        }
    }

    /// <summary>When day flips to night (or back), despawn zones that are no
    /// longer active and fill zones that just became active.</summary>
    private void OnPhaseChanged(DayPhase phase)
    {
        // Despawn living mobs from zones that are now inactive.
        foreach (var zr in _zones)
        {
            if (zr.Zone.IsActiveAt(phase))
                continue;
            var toRemove = _world.Entities.Values
                .Where(e => e.Kind == EntityKind.Mob && e.ZoneId == zr.Zone.Id && !e.Dead)
                .ToList();
            foreach (var mob in toRemove)
            {
                _world.Grid.Remove(mob);
                _world.Entities.Remove(mob.Id, out _);
            }
        }

        // Re-init zone alive-counts and fill those now active (and empty).
        foreach (var zr in _zones)
        {
            int alive = _world.Entities.Values.Count(e =>
                e.Kind == EntityKind.Mob && e.ZoneId == zr.Zone.Id && !e.Dead);
            int need = zr.Zone.IsActiveAt(phase) ? zr.Zone.MaxCount - alive : 0;
            for (int i = 0; i < need; i++)
                SpawnOneInZone(zr);
        }

        BroadcastSystem(phase == DayPhase.Night ? "Night falls." : "Day breaks.");
    }

    /// <summary>Place a single mob in a zone, avoiding the safe zone and roads.
    /// Increments the zone's alive count and tags the mob with its zone id.</summary>
    // =======================================================================
    //  NPCs + Quests
    // =======================================================================

    private void SpawnNpcs()
    {
        foreach (var npc in WorldMap.Npcs)
        {
            var entity = new Entity
            {
                Name = npc.Name,
                Kind = EntityKind.Npc,
                X = npc.X,
                Y = npc.Y,
                Speed = 0,
                Level = 1,
                NpcId = npc.Id,
                NpcRole = npc.Role
            };
            entity.RecomputeDerived();
            _world.Entities[entity.Id] = entity;
            _world.Grid.Add(entity);
        }
        _log.LogInformation("Spawned {Count} NPCs", WorldMap.Npcs.Length);
    }

    /// <summary>Resolve a vendor NPC the player is standing next to.</summary>
    private bool TryGetVendorNpc(Entity player, Guid npcEntityId, out Entity npc)
    {
        npc = null!;
        if (!_world.Entities.TryGetValue(npcEntityId, out var e)
            || e.Kind != EntityKind.Npc || e.NpcRole != NpcRole.Vendor)
            return false;

        float dx = e.X - player.X, dy = e.Y - player.Y;
        if (dx * dx + dy * dy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{e.Name} is too far away.");
            return false;
        }
        npc = e;
        return true;
    }

    private void HandleBuy(BuyItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (!TryGetVendorNpc(player, cmd.NpcEntityId, out var npc)) return;

        string npcId = npc.NpcId ?? "";
        if (!ShopCatalog.Sells(npcId, cmd.ItemDefId)
            || ItemCatalog.Get(cmd.ItemDefId) is not ItemDef def)
        {
            SendSystemToEntity(player, "That vendor doesn't sell that.");
            return;
        }

        long unit = ItemCatalog.BuyPrice(def);
        if (unit <= 0)
        {
            SendSystemToEntity(player, "That item is not for sale.");
            return;
        }

        bool stackable = def.Slot is EquipSlot.Consumable or EquipSlot.Scroll;
        int qty = stackable ? Math.Clamp(cmd.Quantity, 1, 999) : 1;
        long total = unit * qty;

        if (player.Gold < total)
        {
            SendSystemToEntity(player,
                $"Not enough {GameConstants.CurrencyName} (need {total:N0}).");
            return;
        }

        // Gear (non-stackable) needs a free slot; stackables merge.
        if (!stackable && player.Inventory.Count >= GameConstants.InventorySize)
        {
            SendSystemToEntity(player, "Your inventory is full.");
            return;
        }

        // Vendor gear is created PLAIN (no rolled attributes).
        if (!AddItem(player, def.Id, qty, rollAttributes: false))
        {
            SendSystemToEntity(player, "Your inventory is full.");
            return;
        }

        player.Gold -= total;
        SendGold(player);
        SendInventory(player);
        SendSystemToEntity(player,
            $"Bought {def.Name}{(qty > 1 ? $" x{qty}" : "")} for {total:N0} {GameConstants.CurrencyName}.");
    }

    private void HandleSell(SellItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (!TryGetVendorNpc(player, cmd.NpcEntityId, out _)) return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || ItemCatalog.Get(item.DefId) is not ItemDef def)
            return;

        if (item.Equipped)
        {
            SendSystemToEntity(player, "Unequip it before selling.");
            return;
        }
        if (!ItemCatalog.IsSellable(def))
        {
            SendSystemToEntity(player, "That can't be sold.");
            return;
        }

        bool stackable = def.Slot is EquipSlot.Consumable or EquipSlot.Scroll;
        int qty = stackable ? Math.Clamp(cmd.Quantity, 1, item.Quantity) : 1;
        long total = (long)ItemCatalog.SellPrice(def) * qty;

        if (stackable)
        {
            item.Quantity -= qty;
            if (item.Quantity <= 0) player.Inventory.Remove(item);
        }
        else
        {
            player.Inventory.Remove(item);
        }

        player.Gold += total;
        SendGold(player);
        SendInventory(player);
        SendSystemToEntity(player,
            $"Sold {def.Name}{(qty > 1 ? $" x{qty}" : "")} for {total:N0} {GameConstants.CurrencyName}.");
    }

    private void HandleTeleport(TeleportCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!_world.Entities.TryGetValue(cmd.NpcEntityId, out var npc)
            || npc.Kind != EntityKind.Npc || npc.NpcRole != NpcRole.Teleporter)
            return;

        float ndx = npc.X - player.X, ndy = npc.Y - player.Y;
        if (ndx * ndx + ndy * ndy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{npc.Name} is too far away.");
            return;
        }

        var home = WorldMap.SafeZoneAt(npc.X, npc.Y);
        var dest = WorldMap.SafeZones.FirstOrDefault(z => z.Id == cmd.ZoneId);
        if (home is null || dest is null || dest.Id == home.Id)
        {
            SendSystemToEntity(player, "You can't travel there.");
            return;
        }

        int fee = GameConstants.TeleportFee(home, dest);
        if (player.Gold < fee)
        {
            SendSystemToEntity(player,
                $"Not enough {GameConstants.CurrencyName} (need {fee:N0}).");
            return;
        }

        player.Gold -= fee;
        // Reposition to the destination centre (small scatter so players don't stack).
        player.X = Math.Clamp(dest.X + _rng.Next(-150, 150), 0, GameConstants.ZoneWidth);
        player.Y = Math.Clamp(dest.Y + _rng.Next(-150, 150), 0, GameConstants.ZoneHeight);
        player.TargetX = null;
        player.TargetY = null;
        _world.Grid.UpdatePosition(player);

        SendGold(player);
        SendSystemToEntity(player,
            $"Teleported to {dest.Name} for {fee:N0} {GameConstants.CurrencyName}.");
    }

    private void HandleTalk(TalkCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (!_world.Entities.TryGetValue(cmd.NpcEntityId, out var npc) || npc.Kind != EntityKind.Npc)
            return;

        // Must be near the NPC.
        float dx = npc.X - player.X, dy = npc.Y - player.Y;
        if (dx * dx + dy * dy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{npc.Name} is too far away.");
            return;
        }

        SendDialog(player, npc);
    }

    /// <summary>Apply the newbie buffer's full buff set (1h) to a lvl 6-75 player. Each
    /// buff shares its player counterpart's BuffKey at a high rank, so it overrides any
    /// weaker self-cast version.</summary>
    private void ApplyNewbieBuffs(Entity player)
    {
        const int minLvl = 6, maxLvl = 75;
        if (player.Level < minLvl)
        {
            SendSystemToEntity(player, $"Come back at level {minLvl} and I'll bless you.");
            return;
        }
        if (player.Level > maxLvl)
        {
            SendSystemToEntity(player, "You are well beyond a newbie buffer's help.");
            return;
        }
        foreach (var id in SkillCatalog.NewbieBuffSet)
            if (SkillCatalog.Get(id) is SkillDef def)
                ApplyBuff(player, def);
        SendSystemToEntity(player, "You are blessed with a buffer's full might!");
    }

    private void SendDialog(Entity player, Entity npc)
    {
        string npcId = npc.NpcId ?? "";

        // Newbie buffer: blesses the player with a buffer's full buff set on talk.
        if (npc.NpcRole == NpcRole.Buffer)
            ApplyNewbieBuffs(player);

        bool Completed(string qid) => player.CompletedQuests.Contains(qid);
        bool Active(string qid) => player.ActiveQuests.ContainsKey(qid);

        // Class choice is irreversible: once you've taken (active OR completed) any
        // class-change chain quest, only that class's chain stays on offer.
        int committed2 = CommittedClassChain(player, 2);
        int committed3 = CommittedClassChain(player, 3);

        var offered = QuestCatalog
            .OfferedBy(npcId, player.Level, player.Race, player.BaseClass, player.SecondClass, player.ThirdClass, Completed, Active)
            .Where(q =>
            {
                var (cid, tier) = QuestCatalog.ClassChainOf(q.Id);
                if (tier == 2 && committed2 != 0 && cid != committed2) return false;
                if (tier == 3 && committed3 != 0 && cid != committed3) return false;
                return true;
            })
            .Select(q => Summarize(player, q, null)).ToArray();

        // Active quests whose CURRENT step is "talk to THIS npc" and it's the
        // final step (turn-in) OR a mid-chain talk.
        var turnable = new List<QuestSummary>();
        var inProgress = new List<QuestSummary>();
        foreach (var (qid, state) in player.ActiveQuests)
        {
            var def = QuestCatalog.Get(qid);
            if (def is null) continue;
            var summary = Summarize(player, def, state);

            // If current step is a TalkTo this npc, advancing happens on talk.
            var step = def.Steps[state.StepIndex];
            if (step.Type == QuestStepType.TalkTo && step.TargetId == npcId)
                turnable.Add(summary);
            else
                inProgress.Add(summary);
        }

        // Class-change options (only for class-change NPCs, and only the classes
        // THIS character could become — their race + base class, before changing).
        var changes = new List<ClassChangeOption>();
        if (npc.NpcRole == NpcRole.ClassChange)
        {
            foreach (var req in ClassChangeRequirements.AtNpc(npcId))
            {
                if (!ClassChangeAvailable(player, req)) continue;

                var names = req.RequiredItemIds
                    .Select(id => ItemCatalog.Get(id)?.Name ?? id).ToArray();
                var has = req.RequiredItemIds
                    .Select(id => player.Inventory.Any(i => i.DefId == id)).ToArray();
                bool meets = player.Level >= req.MinLevel && has.All(h => h);
                changes.Add(new ClassChangeOption(req.SecondClassId, req.ClassName, meets, names, has));
            }
        }

        // Vendor wares (only for vendor NPCs).
        ShopInfo? shop = null;
        if (npc.NpcRole == NpcRole.Vendor && ShopCatalog.Get(npcId) is ShopDef shopDef)
        {
            var items = shopDef.ItemIds
                .Select(id => ItemCatalog.Get(id))
                .Where(d => d is not null)
                .Select(d => new ShopItemDto(d!.Id, d.Name, ItemCatalog.BuyPrice(d)))
                .ToArray();
            shop = new ShopInfo(shopDef.Title, items);
        }

        // Gatekeeper destinations (every safe zone except this one).
        TeleportInfo? teleport = null;
        if (npc.NpcRole == NpcRole.Teleporter
            && WorldMap.SafeZoneAt(npc.X, npc.Y) is SafeZone home)
        {
            var dests = WorldMap.TeleportDestinationsFrom(npcId, home)
                .Select(z =>
                {
                    var band = WorldMap.LevelRangeNear(z);
                    return new TeleportDest(z.Id, z.Name, GameConstants.TeleportFee(home, z),
                        band?.Min ?? 0, band?.Max ?? 0);
                })
                // Order by hunting-ground level so the "next" town is at the top.
                .OrderBy(d => d.MinLevel == 0 ? int.MaxValue : d.MinLevel)
                .ThenBy(d => d.Name)
                .ToArray();
            teleport = new TeleportInfo(dests);
        }

        SendTo(player, "Dialog", new NpcDialog(
            npc.Name, npc.NpcRole.ToString(),
            offered, turnable.ToArray(), inProgress.ToArray(), changes.ToArray(), shop, teleport));

        // Talking can itself advance a TalkTo step.
        AdvanceTalkStep(player, npcId);
    }

    /// <summary>The class id this player has committed to in a tier (2 or 3), from
    /// any active or completed class-change chain quest; 0 if none yet.</summary>
    private static int CommittedClassChain(Entity player, int tier)
    {
        foreach (var qid in player.ActiveQuests.Keys.Concat(player.CompletedQuests))
        {
            var (cid, t) = QuestCatalog.ClassChainOf(qid);
            if (t == tier && cid != 0) return cid;
        }
        return 0;
    }

    private QuestSummary Summarize(Entity player, QuestDef def, CharacterQuestState? state)
    {
        int stepIndex = state?.StepIndex ?? 0;
        int counter = state?.Counter ?? 0;
        var step = def.Steps[Math.Min(stepIndex, def.Steps.Length - 1)];
        int needed = step.Type == QuestStepType.KillMobs ? step.Count : 1;
        // Ready to turn in = on the final step and that step is a TalkTo.
        bool canComplete = state is not null
            && stepIndex == def.Steps.Length - 1
            && def.Steps[^1].Type == QuestStepType.TalkTo;
        return new QuestSummary(def.Id, def.Name, def.Description, step.Text,
            stepIndex, def.Steps.Length, counter, needed,
            state?.Completed ?? false, canComplete, StepLocation(step));
    }

    /// <summary>A "who/where" hint for a quest step: the NPC + town to talk to, or
    /// the mob + nearest hunting ground (with its level band). "" when not useful.</summary>
    private static string StepLocation(QuestStep step) => step.Type switch
    {
        QuestStepType.TalkTo when WorldMap.NpcById(step.TargetId) is NpcDef npc =>
            $"{npc.Name} — {WorldMap.NearestSafeZone(npc.X, npc.Y).Name}",
        QuestStepType.KillMobs => MobLocationHint(step),
        _ => ""
    };

    private static string MobLocationHint(QuestStep step)
    {
        var (town, min, max) = WorldMap.MobHuntingGround(step.TargetId, step.MinLevel, step.MaxLevel);
        if (town.Length == 0) return "";
        string mobName = MobCatalog.Get(step.TargetId).Name;
        return max > 0 ? $"{mobName} — near {town} (Lv {min}-{max})" : $"{mobName} — near {town}";
    }

    private void HandleQuestAction(QuestActionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;

        switch (cmd.Action)
        {
            case "accept": AcceptQuest(player, cmd.Id); break;
            case "complete": CompleteQuestAtNpc(player, cmd.Id, cmd.NpcEntityId); break;
            case "changeclass": DoQuestClassChange(player, cmd.Id, cmd.NpcEntityId); break;
        }
    }

    private void AcceptQuest(Entity player, string questId)
    {
        var def = QuestCatalog.Get(questId);
        if (def is null) return;
        if (player.ActiveQuests.ContainsKey(questId) || player.CompletedQuests.Contains(questId)) return;
        if (player.Level < def.MinLevel) return;
        if (def.RequiresQuestId is not null && !player.CompletedQuests.Contains(def.RequiresQuestId)) return;

        player.ActiveQuests[questId] = new CharacterQuestState(questId, 0, 0, false);
        SendSystemToEntity(player, $"Quest accepted: {def.Name}");
        SendQuestLog(player);
        SaveEntity(player);
    }

    /// <summary>Advance a TalkTo step if the current step targets this npc.</summary>
    private void AdvanceTalkStep(Entity player, string npcId)
    {
        bool changed = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            var def = QuestCatalog.Get(qid);
            var state = player.ActiveQuests[qid];
            if (def is null || state.Completed) continue;

            var step = def.Steps[state.StepIndex];
            if (step.Type == QuestStepType.TalkTo && step.TargetId == npcId)
            {
                // Final step talk = ready to complete (handled by Complete button);
                // mid-chain talk = advance to next step now.
                if (state.StepIndex < def.Steps.Length - 1)
                {
                    player.ActiveQuests[qid] = state with { StepIndex = state.StepIndex + 1, Counter = 0 };
                    SendSystemToEntity(player, $"{def.Name}: {def.Steps[state.StepIndex + 1].Text}");
                    changed = true;
                }
            }
        }
        if (changed) { SendQuestLog(player); SaveEntity(player); }
    }

    private void CompleteQuestAtNpc(Entity player, string questId, Guid npcEntityId)
    {
        var def = QuestCatalog.Get(questId);
        if (def is null || !player.ActiveQuests.TryGetValue(questId, out var state)) return;

        // Must be on the final TalkTo step at the right NPC.
        if (state.StepIndex != def.Steps.Length - 1) return;
        var finalStep = def.Steps[^1];
        if (finalStep.Type != QuestStepType.TalkTo) return;
        if (!_world.Entities.TryGetValue(npcEntityId, out var npc) || npc.NpcId != finalStep.TargetId) return;

        // Grant rewards.
        if (def.Reward.Exp > 0) AwardExp(player, def.Reward.Exp);
        if (def.Reward.SkillPoints > 0)
        {
            player.SkillPoints += def.Reward.SkillPoints;
            SendLearned(player);
        }
        if (def.Reward.ItemIds is { Length: > 0 })
            foreach (var itemId in def.Reward.ItemIds)
                AddItem(player, itemId);

        player.ActiveQuests.Remove(questId);
        player.CompletedQuests.Add(questId);
        SendSystemToEntity(player, $"Quest complete: {def.Name}!");
        SendInventory(player);
        SendQuestLog(player);
        SendDialog(player, npc);
        SaveEntity(player);
    }

    /// <summary>Is this class change offered to this player right now? Encodes the
    /// tier gating: Tier 2 needs no second class yet + matching race/base; Tier 3
    /// needs the right parent 2nd class + no third class yet.</summary>
    private static bool ClassChangeAvailable(Entity player, ClassChangeRequirements.Requirement req)
    {
        if (req.Tier == 2)
        {
            if (player.SecondClass != 0) return false;
            return ClassCatalog.Get(req.SecondClassId) is SecondClassDef scd
                && scd.Base == player.BaseClass && scd.Race == player.Race;
        }
        if (req.Tier == 3)
        {
            if (player.SecondClass == 0 || player.ThirdClass != 0) return false;
            if (req.RequiredCurrentClass != player.SecondClass) return false;
            return ThirdClassCatalog.Get(req.SecondClassId) is ThirdClassDef tcd
                && tcd.Race == player.Race;
        }
        return false;
    }

    private void DoQuestClassChange(Entity player, string targetClassIdStr, Guid npcEntityId)
    {
        if (!int.TryParse(targetClassIdStr, out int classId)) return;
        var req = ClassChangeRequirements.ForClass(classId);
        if (req is null) return;
        if (!_world.Entities.TryGetValue(npcEntityId, out var npc) || npc.NpcId != req.NpcId) return;

        if (!ClassChangeAvailable(player, req))
        {
            SendSystemToEntity(player, "That class isn't available to you.");
            return;
        }
        if (player.Level < req.MinLevel) { SendSystemToEntity(player, $"Requires level {req.MinLevel}."); return; }

        // Must hold all required quest items.
        foreach (var itemId in req.RequiredItemIds)
            if (!player.Inventory.Any(i => i.DefId == itemId))
            {
                SendSystemToEntity(player, "You don't have the required items.");
                return;
            }

        // Consume the quest items.
        foreach (var itemId in req.RequiredItemIds)
        {
            var item = player.Inventory.FirstOrDefault(i => i.DefId == itemId);
            if (item is not null) player.Inventory.Remove(item);
        }

        if (req.Tier == 3) player.ThirdClass = classId;
        else player.SecondClass = classId;

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendInventory(player);
        SendStats(player);
        SendLearned(player);
        SendDialog(player, npc);
        BroadcastSystem($"{player.Name} has become a {req.ClassName}!");
        SaveEntity(player);
    }

    /// <summary>Hook from mob death: advance KillMobs steps on active quests.</summary>
    private void AdvanceKillQuests(Entity player, Entity mob)
    {
        bool changed = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            var def = QuestCatalog.Get(qid);
            var state = player.ActiveQuests[qid];
            if (def is null || state.Completed) continue;

            var step = def.Steps[state.StepIndex];
            if (step.Type != QuestStepType.KillMobs) continue;

            // Match by mob type id (exact), so "Elite Cave Spider" still counts
            // for a "cave_spider" objective regardless of rank prefix.
            bool typeMatch = string.Equals(mob.MobTypeId, step.TargetId, StringComparison.OrdinalIgnoreCase);
            bool levelMatch = (step.MinLevel == 0 || mob.Level >= step.MinLevel)
                && (step.MaxLevel == 0 || mob.Level <= step.MaxLevel);
            if (!typeMatch || !levelMatch) continue;

            int counter = state.Counter + 1;
            if (counter >= step.Count && state.StepIndex < def.Steps.Length - 1)
            {
                player.ActiveQuests[qid] = state with { StepIndex = state.StepIndex + 1, Counter = 0 };
                SendSystemToEntity(player, $"{def.Name}: {def.Steps[state.StepIndex + 1].Text}");
            }
            else
            {
                player.ActiveQuests[qid] = state with { Counter = counter };
                string mobLabel = MobCatalog.Get(step.TargetId).Name;
                SendSystemToEntity(player, $"{def.Name}: {mobLabel} {counter}/{step.Count}");
            }
            changed = true;
        }
        if (changed) { SendQuestLog(player); }
    }

    private void SendQuestLog(Entity player)
    {
        var active = player.ActiveQuests.Values
            .Select(st => { var d = QuestCatalog.Get(st.QuestId); return d is null ? null : Summarize(player, d, st); })
            .Where(x => x is not null).Select(x => x!).ToArray();
        SendTo(player, "QuestLog", new QuestLog(active, player.CompletedQuests.ToArray()));
    }

        private void SpawnOneInZone(ZoneRuntime zr)
    {
        var zone = zr.Zone;
        float x = zone.X, y = zone.Y;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            double angle = _rng.NextDouble() * Math.PI * 2;
            double dist = Math.Sqrt(_rng.NextDouble()) * zone.Radius;
            float tx = zone.X + (float)(Math.Cos(angle) * dist);
            float ty = zone.Y + (float)(Math.Sin(angle) * dist);
            (tx, ty) = WorldMap.ClampToBorder(tx, ty);

            if (!GameConstants.InSafeZone(tx, ty) && !WorldMap.OnRoad(tx, ty))
            {
                x = tx; y = ty;
                break;
            }
        }

        string mobId = zone.MobTypes[_rng.Next(zone.MobTypes.Length)];
        var mobType = MobCatalog.Get(mobId);
        int level = _rng.Next(zone.MinLevel, zone.MaxLevel + 1);
        var stats = StatCalculator.MobStats(level);

        // Elites/bosses are tougher versions of the base mob.
        float hpMul = zone.Rank switch { MobRank.Elite => 4f, MobRank.Boss => 20f, _ => 1f };
        float atkMul = zone.Rank switch { MobRank.Elite => 1.5f, MobRank.Boss => 2.5f, _ => 1f };

        string displayName = zone.Rank switch
        {
            MobRank.Elite => $"Elite {mobType.Name}",
            MobRank.Boss => $"{mobType.Name} Lord",
            _ => mobType.Name
        };

        var mob = new Entity
        {
            Name = displayName,
            Kind = EntityKind.Mob,
            X = x,
            Y = y,
            WalkSpeed = mobType.WalkSpeed,
            RunSpeed = mobType.RunSpeed,
            Speed = mobType.RunSpeed,
            Level = level,
            Con = stats.Con,
            AtkStat = (int)(stats.Atk * atkMul),
            Wit = stats.Wit,
            Dex = stats.Dex,
            Aggressive = mobType.Aggressive || zone.Rank != MobRank.Normal,
            ZoneId = zone.Id,
            Rank = zone.Rank,
            MobTypeId = mobId
        };
        mob.RecomputeDerived();
        // RecomputeDerived leaves mob RunSpeed/WalkSpeed as set above (player-only
        // override), so Speed stays the catalog run speed.
        mob.MaxHp = (int)(mob.MaxHp * hpMul);
        // Dedicated mob magic defence (the level base alone leaves low-level mobs at
        // ~0 mDef, which lets spells one-shot them). Keeps magic ~on par with physical.
        mob.MagicDefence = StatCalculator.MobMagicDefence(level);
        mob.Hp = mob.MaxHp;
        mob.Mp = mob.MaxMp;
        mob.HomeX = mob.X;
        mob.HomeY = mob.Y;

        _world.Entities[mob.Id] = mob;
        _world.Grid.Add(mob);
        zr.OnSpawned();
    }
}
