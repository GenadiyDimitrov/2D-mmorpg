using System.Globalization;
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

    /// <summary>The main entity sweep iterates THIS, not `_world.Entities.Values` directly. The loop
    /// body spawns, despawns, teleports and releases from jail, and any one of those structurally
    /// modifies the dictionary — which took the whole tick down with "Collection was modified"
    /// (playtest-19 M3, live since 0.45.0). Reused across ticks so the snapshot costs no allocation.</summary>
    private readonly List<Entity> _tickBuffer = new();

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
        LoadDebugConfig();   // restore persisted debug tuning (rates/karma/caps) between runs
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

    /// <summary>Commands a JAILED character may not issue. Gated here at the dispatcher rather than in
    /// each handler so a new action can't silently become a jail loophole: serving a sentence means no
    /// fighting, no skills, no items, no trading/party, no shopping and no teleporting. MOVEMENT is
    /// deliberately absent — a jailed player can pace around inside the cell (HandleMove clamps them to
    /// it) — as are chat (HandleChat answers with its own message) and leaving/logging out.</summary>
    private bool IsBlockedWhileJailed(IGameCommand cmd)
    {
        string? conn = cmd switch
        {
            AttackCmd c => c.ConnectionId,
            SkillCmd c => c.ConnectionId,
            UsePotionCmd c => c.ConnectionId,
            EquipCmd c => c.ConnectionId,
            RemoveItemCmd c => c.ConnectionId,
            RestoreItemCmd c => c.ConnectionId,
            OpenBoxCmd c => c.ConnectionId,
            SelectBoxItemsCmd c => c.ConnectionId,
            EnchantCmd c => c.ConnectionId,
            RerollAttributesCmd c => c.ConnectionId,
            CraftCmd c => c.ConnectionId,
            JoinProfessionCmd c => c.ConnectionId,
            QuitProfessionCmd c => c.ConnectionId,
            BuyItemCmd c => c.ConnectionId,
            SellItemCmd c => c.ConnectionId,
            BuyBackCmd c => c.ConnectionId,
            DisassembleItemCmd c => c.ConnectionId,   // `BL-22` — a jailed player handles no items
            OpenWarehouseCmd c => c.ConnectionId,
            WarehouseDepositCmd c => c.ConnectionId,
            WarehouseWithdrawCmd c => c.ConnectionId,
            OpenAccountWarehouseCmd c => c.ConnectionId,
            AccountWarehouseDepositCmd c => c.ConnectionId,
            AccountWarehouseWithdrawCmd c => c.ConnectionId,
            TeleportCmd c => c.ConnectionId,
            TalkCmd c => c.ConnectionId,
            TradeRequestCmd c => c.ConnectionId,
            TradeRespondCmd c => c.ConnectionId,
            TradeOfferCmd c => c.ConnectionId,
            TradeGoldCmd c => c.ConnectionId,
            TradeReadyCmd c => c.ConnectionId,
            PartyInviteCmd c => c.ConnectionId,
            PartyInviteByNameCmd c => c.ConnectionId,
            PartyRespondCmd c => c.ConnectionId,
            FollowCmd c => c.ConnectionId,
            AssistCmd c => c.ConnectionId,
            BufferActionCmd c => c.ConnectionId,
            _ => null,
        };
        if (conn is null || !TryGetPlayer(conn, out var p) || !p.Jailed)
            return false;

        SendSystemToEntity(p, "You can't do that while jailed.");
        return true;
    }

    /// <summary>THE authorisation gate for every <see cref="IAdminCommand"/> — the whole former debug menu.
    ///
    /// One check, before dispatch, rather than fifteen handlers each remembering to look. These used to be
    /// compiled out with <c>#if DEBUG</c> in the hub, so the release server published to the phone accepted
    /// the calls and did nothing: the buttons were on screen and pressing them was silence (owner). The
    /// gate belongs here because "is this character an admin" is a runtime fact, and the loop is the only
    /// thread that may read entity state.
    ///
    /// A non-admin gets told, rather than ignored. There is nothing to hide: the client already hides the
    /// menu unless your account role says otherwise, so a request arriving from a non-admin means either a
    /// stale client or someone poking the hub — and in both cases silence is the reply that costs an hour
    /// to diagnose.</summary>
    private bool IsBlockedForNonAdmin(IGameCommand cmd)
    {
        if (cmd is not IAdminCommand admin) return false;
        if (!TryGetPlayer(admin.ConnectionId, out var p)) return true;   // not in the world: nothing to do
        if (p.IsAdmin) return false;

        SendSystemToEntity(p, "That is an admin-only command.");
        _log.LogWarning("Non-admin {Name} tried {Command}", p.Name, cmd.GetType().Name);
        return true;
    }

    private void ProcessCommands()
    {
        while (_world.Commands.TryDequeue(out var cmd))
        {
            if (IsBlockedWhileJailed(cmd)) continue;
            if (IsBlockedForNonAdmin(cmd)) continue;

            switch (cmd)
            {
                case EnterWorldCommand c: HandleEnterWorld(c); break;
                case AdminCmd c: HandleAdmin(c); break;
                case ForceRemoveCmd c: HandleForceRemove(c); break;
                case JailNowCmd c: HandleJailNow(c); break;
                case CharismaAdjustCmd c: HandleCharismaAdjust(c); break;
                case ChatBanNowCmd c: HandleChatBanNow(c); break;
                case AdminGiveItemCmd c: HandleAdminGiveItem(c); break;
                case AdminRemoveItemCmd c: HandleAdminRemoveItem(c); break;
                case FriendCmd c: HandleFriend(c); break;
                case BlockCmd c: HandleBlock(c); break;
                case LikeCmd c: HandleLike(c); break;
                case FollowCmd c: HandleFollow(c); break;
                case AssistCmd c: HandleAssist(c); break;
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
                case ForgetSkillCmd c: HandleForgetSkill(c); break;
                case BuyStatSwapsCmd c: HandleBuyStatSwaps(c); break;
                case BufferActionCmd c: HandleBufferAction(c); break;
                case SetMoveStateCmd c: HandleSetMoveState(c); break;
                case CancelCastCmd c: HandleCancelCast(c); break;
                case RemoveBuffCmd c: HandleRemoveBuff(c); break;
                case OpenBoxCmd c: HandleOpenBox(c); break;
                case SelectBoxItemsCmd c: HandleSelectBoxItems(c); break;
                case InspectTargetCmd c: HandleInspectTarget(c); break;
                case RespawnCmd c: HandleRespawn(c); break;
                case ClassChangeCmd c: HandleClassChange(c); break;
                case EquipCmd c: HandleEquip(c); break;
                case UsePotionCmd c: HandleUsePotion(c); break;
                case ResurrectResponseCmd c: HandleResurrectResponse(c); break;
                case EnchantCmd c: HandleEnchant(c); break;
                case AdminEnchantCmd c: HandleAdminEnchant(c); break;
                case RerollAttributesCmd c: HandleRerollAttributes(c); break;
                case RemoveItemCmd c: HandleRemoveItem(c); break;
                case RestoreItemCmd c: HandleRestoreItem(c); break;
                case BuyBackCmd c: HandleBuyBack(c); break;
                case DisassembleItemCmd c: HandleDisassembleItem(c); break;
                case OpenWarehouseCmd c: HandleOpenWarehouse(c); break;
                case WarehouseDepositCmd c: HandleWarehouseDeposit(c); break;
                case WarehouseWithdrawCmd c: HandleWarehouseWithdraw(c); break;
                case OpenAccountWarehouseCmd c: HandleOpenAccountWarehouse(c); break;
                case AccountWarehouseDepositCmd c: HandleAccountWarehouseDeposit(c); break;
                case AccountWarehouseWithdrawCmd c: HandleAccountWarehouseWithdraw(c); break;
                case DebugGiveCmd c: HandleDebugGive(c); break;
                case DebugCancelAttrCmd c: HandleDebugCancelAttr(c); break;
                case CraftCmd c: HandleCraft(c); break;
                case JoinProfessionCmd c: HandleJoinProfession(c); break;
                case QuitProfessionCmd c: HandleQuitProfession(c); break;
                case DebugSetProfessionCmd c: HandleDebugSetProfession(c); break;
                case DebugSetCraftLevelCmd c: HandleDebugSetCraftLevel(c); break;
                case DebugSecondClassCmd c: HandleDebugSecondClass(c); break;
                case DebugLevelCmd c: HandleDebugLevel(c); break;
                case DebugLearnAllCmd c: HandleDebugLearnAll(c); break;
                case DebugGoldCmd c: HandleDebugGold(c); break;
                case DebugBuffCmd c: HandleDebugBuff(c); break;
                case DebugKarmaCmd c: HandleDebugKarma(c); break;
                case DebugAddSubclassCmd c: HandleDebugAddSubclass(c); break;
                case SwitchSubclassCmd c: HandleSwitchSubclass(c); break;
                case DebugSpCmd c: HandleDebugSp(c); break;
                case DebugResetCmd c: HandleDebugReset(c); break;
                case DebugThirdClassCmd c: HandleDebugThirdClass(c); break;
                case DebugFourthClassCmd c: HandleDebugFourthClass(c); break;
                case DebugTeleportCmd c: HandleDebugTeleport(c); break;
                case TradeRequestCmd c: HandleTradeRequest(c); break;
                case TradeRespondCmd c: HandleTradeRespond(c); break;
                case TradeOfferCmd c: HandleTradeOffer(c); break;
                case TradeGoldCmd c: HandleTradeGold(c); break;
                case TradeReadyCmd c: HandleTradeReady(c); break;
                case TradeCancelCmd c: HandleTradeCancel(c); break;
                case PartyInviteCmd c: HandlePartyInvite(c); break;
                case PartyInviteByNameCmd c: HandlePartyInviteByName(c); break;
                case PartyRespondCmd c: HandlePartyRespond(c); break;
                case PartyLeaveCmd c: HandlePartyLeave(c); break;
                case PartyChangeLeaderCmd c: HandlePartyChangeLeader(c); break;
                case SaveEquipPresetCmd c: HandleSaveEquipPreset(c); break;
                case ApplyEquipPresetCmd c: HandleApplyEquipPreset(c); break;
                case PartyKickCmd c: HandlePartyKick(c); break;
                case PartySetLootModeCmd c: HandlePartySetLootMode(c); break;
                case PartyLootVoteCmd c: HandlePartyLootVote(c); break;
                case SetAutoHuntConfigCmd c: HandleSetAutoHuntConfig(c); break;
                case SetSkillBarCmd c: HandleSetSkillBar(c); break;
                case LeaveWorldCmd c: HandleLeaveWorld(c); break;
                case ResyncCmd c: _lastSentByConn.Remove(c.ConnectionId); break;
                case ToggleAutoHuntCmd c: HandleToggleAutoHunt(c); break;
                case LogoutCmd c: HandleLogout(c); break;
                case StartOfflineFarmCmd c: HandleStartOfflineFarm(c); break;
                case TogglePvpCmd c: HandleTogglePvp(c); break;
                case SetTitleCmd c: HandleSetTitle(c); break;
                case SetCustomTitleCmd c: HandleSetCustomTitle(c); break;
                case SetTitleColorCmd c: HandleSetTitleColor(c); break;
                case TitleHoldersCmd c: ApplyTitleHolders(c); break;
                case ToggleCounterAttackCmd c: HandleToggleCounterAttack(c); break;
                case RequestDebugConfigCmd c: HandleRequestDebugConfig(c); break;
                case SetDebugConfigCmd c: HandleSetDebugConfig(c); break;
                case ChatCmd c: HandleChat(c); break;
            }
        }
    }

    private void HandleEnterWorld(EnterWorldCommand cmd)
    {
        var entity = cmd.Entity;

        // RECONNECT: if this character is still in the world (offline-farming OR in the link-dead
        // grace), re-attach to that LIVE entity (it holds the latest state) and discard the loaded copy.
        var existing = _world.Entities.Values.FirstOrDefault(e =>
            (e.IsOfflineFarming || e.IsDisconnected) &&
            e.PersistentId is int pid && pid == entity.PersistentId);
        if (existing is not null)
        {
            entity = existing;
            entity.IsOfflineFarming = false;
            entity.IsDisconnected = false;
            entity.DisconnectGraceTicks = 0;
        }
        else
        {
            // Spawn position: where they logged off, nudged into the world bounds.
            entity.X = Math.Clamp(entity.X, GameConstants.WorldMinX, GameConstants.ZoneWidth);
            entity.Y = Math.Clamp(entity.Y, GameConstants.WorldMinY, GameConstants.ZoneHeight);
            _world.Entities[entity.Id] = entity;
            _world.Grid.Add(entity);
            // Anchor the static-spot centre at the login position (persisted auto-hunt has no saved centre).
            entity.FarmCenterX = entity.X;
            entity.FarmCenterY = entity.Y;
        }

        _world.EntityToConnection[entity.Id] = cmd.ConnectionId;
        _world.ConnectionToEntity[cmd.ConnectionId] = entity.Id;

        // Adopt the account bank read during login ONLY if this account has no live list. One already
        // in memory is newer than the disk read — another character of the same account may be in the
        // world (offline farming) and may have moved something since.
        if (cmd.AccountBank is not null && entity.AccountId != 0)
            _world.AccountWarehouses.TryAdd(entity.AccountId, cmd.AccountBank);

        // Same adoption rule for the daily farm allowance. NOTHING is reset here: this is exactly
        // where the old per-session counters were zeroed, which is what made "re-log for another 2h"
        // work. The balance is spent, and only midnight puts it back.
        if (cmd.AccountBudget is not null && entity.AccountId != 0)
            _world.AccountBudgets.TryAdd(entity.AccountId, cmd.AccountBudget);

        cmd.Result.TrySetResult(
            new LoginResult(true, null, entity.Id, entity.X, entity.Y, GameClock.Epoch, entity.Role));

        AutoLearnCoreSkills(entity);
        RestorePersistedBuffs(entity);   // before SendStats — the buffs change the numbers it sends
        SendInventory(entity);
        SendRestorable(entity);  // empty on a fresh login, but the window must not show a stale list
        SendWarehouse(entity);   // the bank travels with login so the client can show it without a town trip
        SendAccountWarehouse(entity);
        SendStats(entity);
        SendLearned(entity);   // sends the skill BAR with it, in the right order — see SendLearned
        SendCooldowns(entity); // after the bar: the overlay has nothing to sit on before it exists
        SendSubclasses(entity);
        SendCrafting(entity);  // profession + unlocked blueprints; the craft window is empty without it
        SendQuestLog(entity);
        SendGold(entity);
        SendAutoHuntConfig(entity);   // restore the saved auto-hunt settings in the client UI
        SendAutoHuntStatus(entity);
        SendSocialOptions(entity);    // the Options window's switches (M2)
        SendPvpState(entity);
        RefreshTitle(entity);         // resolves the saved title choice against the boards, and pushes it
        SendProgress(entity);         // see SendProgress: without this the EXP bar starts EMPTY
        if (_world.Parties.TryGetValue(entity.Id, out var rejoinParty))
            SendPartyUpdate(rejoinParty);   // clear the offline icon for the rest of the party
        else
            // NOT in a party: still push an EMPTY roster, or a client that WAS partied before it
            // relogged keeps showing the old members forever (it only ever hears about a party from a
            // push, and nothing was telling it the party is gone). State the client needs on arrival
            // must be pushed on arrival — same rule as the EXP-bar/Progress fix.
            SendTo(entity, "Party", new PartyUpdate(Array.Empty<PartyMemberDto>()));
        if (entity.IsStaff)
            SendSystemToEntity(entity,
                $"{entity.Role} privileges active on this character. Type /help for commands.");
        NotifyFriendsOnline(entity);   // "X is back online" — MUTUAL friends only, see NotifyFriendsPresence
        if (GameConstants.AnnounceWorldEntryExit)
            BroadcastSystem($"{entity.Name} entered the world.");
        _log.LogInformation("Player {Name} entered (char {Id})", entity.Name, entity.PersistentId);
    }

    // Combat state decays 30s after the last damage; the disconnect grace = _graceSeconds.
    private const int CombatDecayTicks = 300;
    private int DisconnectGraceLimit => _graceSeconds * GameConstants.TickRate;

    /// <summary>A player is "in combat" for 30s after the last damage dealt/taken.</summary>
    /// <summary>In combat = traded blows recently, OR something is still ticking damage on you.
    ///
    /// The DoT half is what stops combat-logging out of a bleed (owner, 2026-07-29). A Venomweaver
    /// stacks a DoT, you quit to character select, and the debuff is gone — debuffs are deliberately
    /// NOT persisted, because a DoT needs a live applier for damage attribution. Gating the EXIT is the
    /// answer instead of saving the debuff: you may leave once you have escaped, killed them or died
    /// AND nothing is ticking on you.</summary>
    private bool IsInCombat(Entity e) =>
        (e.LastCombatTick > 0 && _tick - e.LastCombatTick < CombatDecayTicks)
        || e.Buffs.Any(b => (b.Effect & SkillEffect.AnyDot) != 0);

    /// <summary>A connection dropped (network or client close). Decide the character's fate:
    /// auto-hunting or mid-combat → keep offline-farming; otherwise a short link-dead grace so a
    /// reconnect resumes seamlessly; dead / already transitioned → normal removal.</summary>
    private void HandleLeave(LeaveCommand leave)
    {
        if (!_world.ConnectionToEntity.Remove(leave.ConnectionId, out var entityId))
            return;
        _world.EntityToConnection.Remove(entityId);
        if (!_world.Entities.TryGetValue(entityId, out var entity))
            return;

        // Offline farm: ONLY genuine offline farming (auto-hunt on & not cap-locked), alive, out of
        // town. This is the state the 2h offline cap governs.
        // ⚠ The budget check belongs HERE, not only in the tick: without it a drop with an empty
        // allowance would park the character as an offline farmer and the very next tick would kick it
        // straight back out ("X keeps hunting while away" / "X stopped hunting", one after the other).
        if (!entity.Dead && !GameConstants.InSafeZone(entity.X, entity.Y) &&
            entity.AutoHuntEnabled && HasOfflineBudget(entity))
        {
            BeginOfflineFarm(entity);
            BroadcastSystem($"{entity.Name} keeps hunting while away.");
            return;
        }

        // Everyone else who's alive → a link-dead GRACE (stays in the party, reconnect resumes). A
        // mid-combat drop keeps defending its current fight (anti-combat-log) and the 180s grace
        // timer is PAUSED until combat ends — NO 2h offline cap, NO forced re-enable.
        if (!entity.Dead && !entity.IsOfflineFarming && !entity.IsDisconnected)
        {
            BeginDisconnectGrace(entity);
            return;
        }

        // Dead / already offline: straight to the normal removal chain.
        NormalLeave(entity);
    }

    /// <summary>Keep the character in the world as an offline farmer (driven by AutoPilot, no
    /// connection). Stays in its party with the OFFLINE roster tag.</summary>
    private void BeginOfflineFarm(Entity entity)
    {
        entity.IsOfflineFarming = true;
        // NOTHING is reset here either — this was the second half of the old defect, and it is what
        // made the offline cap refill on top of the login refill.
        entity.OfflineSecondsLeft = AutoOfflineSecondsLeft(entity);   // right from the first second
        CancelTradeFor(entity, notifyPartnerOnly: true);
        _world.PendingTradeRequests.Remove(entity.Id);
        if (_world.Parties.TryGetValue(entity.Id, out var party))
        {
            ReassignLeaderIfNeeded(party);
            SendPartyUpdate(party);
        }
        SaveEntity(entity);
    }

    /// <summary>Freeze the character in the world for a short reconnect grace (link-dead). It shows
    /// a "Disconnected" title to all + the OFFLINE tag to its party; it does not act or farm.</summary>
    private void BeginDisconnectGrace(Entity entity)
    {
        entity.IsDisconnected = true;
        entity.DisconnectGraceTicks = DisconnectGraceLimit;
        entity.QueuedSkillId = null;
        if (entity.CastingSkillId is not null) CancelCast(entity, startCooldown: false);
        // Engaged/CombatTargetId are KEPT: a mid-combat drop keeps defending its current target
        // until the fight ends (the grace timer stays paused while in combat — see Simulate).
        CancelTradeFor(entity, notifyPartnerOnly: true);
        _world.PendingTradeRequests.Remove(entity.Id);
        if (_world.Parties.TryGetValue(entity.Id, out var party))
        {
            ReassignLeaderIfNeeded(party);
            SendPartyUpdate(party);
        }
        SaveEntity(entity);
    }

    /// <summary>The normal exit chain: leave the party, remove the entity, save.</summary>
    private Task NormalLeave(Entity entity)
    {
        NotifyFriendsPresence(entity, online: false);   // while they're still in Entities to compare against
        // Shed every mob locked onto them BEFORE they vanish, or those mobs sit Engaged on an id that
        // no longer resolves — MobAi's engaged branch returns early, so they would never re-aggro and
        // never wander again. MobAi now self-heals from that too, but clearing it here is the tidy half.
        DropAggroOn(entity);
        _world.Entities.Remove(entity.Id, out _);
        CancelTradeFor(entity, notifyPartnerOnly: true);
        _world.PendingTradeRequests.Remove(entity.Id);
        RemoveFromParty(entity, "left the world");
        // ALWAYS drop out of the grid. This used to be `if (!entity.Dead)`, which leaked a GHOST CORPSE
        // on every logout-while-dead: the entity left `Entities` but stayed in the `Grid`, so it kept
        // being broadcast to everyone forever, and nothing could resurrect it (the res path looks the
        // target up in `Entities`, where it no longer was). Logging in again then built a SECOND entity
        // beside the orphan, so corpses stacked one per relog. A dead entity is only kept in the grid
        // while it is still IN the world (so allies can see and revive it) — leaving is leaving.
        _world.Grid.Remove(entity);
        var saved = SaveEntity(entity);
        SaveBudgetOf(entity);   // spent allowance must not be handed back by a crash before the autosave
        if (GameConstants.AnnounceWorldEntryExit)
            BroadcastSystem($"{entity.Name} left the world.");
        return saved;
    }

    /// <summary>End a link-dead grace that expired (or whose owner died): the normal removal chain.
    /// Deferred out of the entity loop.</summary>
    private void EndDisconnectGrace(Guid id)
    {
        if (!_world.Entities.TryGetValue(id, out var e) || !e.IsDisconnected)
            return;
        e.IsDisconnected = false;
        NormalLeave(e);
    }

    /// <summary>Back to character select. A DELIBERATE exit: the character really leaves the world, so
    /// you can walk straight back in as the same character. (The DISCONNECT path — LeaveCommand — is
    /// the one that keeps you in the world offline-farming or link-dead; routing char-select through
    /// it left a ghost behind that then refused your own re-entry for 180 seconds.)</summary>
    private void HandleLeaveWorld(LeaveWorldCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
        {
            cmd.Result?.TrySetResult(null);   // nothing to save; never leave the hub waiting
            return;
        }

        // Character select is an EXIT, so it is gated exactly like /exit — otherwise it is the
        // combat-log hole: quit to the character screen mid-fight and every debuff on you is gone,
        // since debuffs are not persisted. IsInCombat also counts an active DoT.
        if (IsInCombat(player))
        {
            SendSystemToEntity(player, "You can't leave while in combat.");
            cmd.Result?.TrySetResult("You can't leave while in combat.");
            return;
        }

        _world.ConnectionToEntity.Remove(cmd.ConnectionId);
        _world.EntityToConnection.Remove(player.Id);
        // Signal the hub only once the save has landed, so the character list it fetches next shows
        // this session's level and class rather than the row from login.
        NormalLeave(player).ContinueWith(_ => cmd.Result?.TrySetResult(null),
                                         TaskScheduler.Default);
    }

    private void HandleLogout(LogoutCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (IsInCombat(player))
        {
            SendTo(player, "LogoutResult", new LogoutResult(false, "You can't exit while in combat."));
            return;
        }
        SendTo(player, "LogoutResult", new LogoutResult(true, ""));
        _world.ConnectionToEntity.Remove(cmd.ConnectionId);
        _world.EntityToConnection.Remove(player.Id);
        NormalLeave(player);
    }

    private void HandleStartOfflineFarm(StartOfflineFarmCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        // Refuse rather than start a session that ends on the next tick.
        if (!HasOfflineBudget(player))
        {
            SendSystemToEntity(player,
                "Your account's offline farming time for today is used up. It refills at midnight.");
            return;
        }
        player.AutoHuntEnabled = true;
        player.FarmCenterX = player.X; player.FarmCenterY = player.Y;   // anchor the static circle here
        // Tell the client (it returns to the account screen), then drop the connection + go offline.
        // Say how long it will run (32q) — the session ends on a budget you otherwise can't see.
        int offlineLeft = AutoOfflineSecondsLeft(player);
        _ = _hub.Clients.Client(cmd.ConnectionId).SendAsync("ForceDisconnect",
            offlineLeft < 0
                ? "You are now farming offline (no time limit)."
                : $"You are now farming offline for {HumanTime(offlineLeft)}.");
        _world.ConnectionToEntity.Remove(cmd.ConnectionId);
        _world.EntityToConnection.Remove(player.Id);
        BeginOfflineFarm(player);
        BroadcastSystem($"{player.Name} keeps hunting while away.");
    }

    // ----- PvP / flag / karma (IG-style). Runtime-tunable via the Debug settings panel; the values
    // here are the code DEFAULTS (move final picks back into these initializers). -----
    private const int PvpFlagTicks = 600;   // 60s purple flag after a pvp action
    private int _karmaBase = 200;              // karma for a 1st, same-level innocent kill
    private double _karmaConsecGrowth = 1.1;   // ×per consecutive PK  (+10% each)
    private double _karmaLevelGrowth = 1.2;    // ×per level the victim is BELOW the killer (+20%)
    private int _karmaLossPerDeath = 200;      // karma shed on each death
    private int _karmaLossPerMob = 20;         // karma shed per mob kill (grind it off while farming)

    // Debug test skills (admin live-tuning): the two test damage skills use Flat=_testSkillPower,
    // Mod=_testSkillMod. For reading the {Flat, Mod} curve live. (`_testHealPower` went with the test
    // heal itself, 2026-08-12, `BL-37`.)
    private int _testSkillPower = 0;
    private float _testSkillMod = 1f;
    private const int KarmaMaxPerKill = 15_000; // owner: cap one PK at 10-20k (~750 mob kills to shed)
    private const int KarmaGapFloor   = 10;     // level gap ≤ this → just the base karma (200)
    private const int KarmaGapCap     = 50;     // level gap ≥ this → the per-kill cap (skyrocket endpoint)

    /// <summary>A player's name state: red (karma), else purple (recent pvp), else white.</summary>
    private PvpFlag FlagOf(Entity p) =>
        p.Karma > 0 ? PvpFlag.Pk
        : _tick < p.PvpFlagUntilTick ? PvpFlag.Flagged
        : PvpFlag.Innocent;

    private void HandleTogglePvp(TogglePvpCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;
        p.PvpEnabled = cmd.Enabled;
        SendPvpState(p);
        SendSystemToEntity(p, p.PvpEnabled
            ? "PvP ON — your attacks/skills can hit other players (not in towns)."
            : "PvP OFF.");
    }

    private void HandleToggleCounterAttack(ToggleCounterAttackCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;
        p.CounterAttack = cmd.Enabled;
        SendPvpState(p);
        SendSystemToEntity(p, p.CounterAttack
            ? "Counter-attack ON — you retaliate against players who attack you while auto-hunting."
            : "Counter-attack OFF.");
    }

    private void SendPvpState(Entity p) =>
        SendTo(p, "PvpState", new PvpState(p.PvpEnabled, p.CounterAttack, p.Karma, p.PkCount, p.PvpCount));

    // =========================================================================
    // Wearable titles
    // =========================================================================
    //
    // A title is HELD while you are rank 1 of its board, not EARNED once and kept. The alternative was
    // a persisted "titles I have ever won" set, and it says the wrong thing: "the Wealthy" worn by a
    // player who was out-earned a month ago contradicts the board it came from, and the board is the
    // entire point of the title. Holding it also needs no new schema and no writes to offline rows —
    // only the CHOICE is persisted (Entity.TitleCategory), and it survives losing and regaining the
    // board, so a lost title comes back on its own.

    /// <summary>Character NAME -> the categories they currently top. Written ONLY by the single writer
    /// (via <see cref="TitleHoldersCmd"/>); the DB read that fills it happens on a worker.</summary>
    private Dictionary<string, List<string>> _titleHolders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How often the boards are re-read. Five minutes: the boards themselves are only as fresh
    /// as the last autosave (≤60s), nothing about a title is urgent, and re-reading them per minute
    /// would make a two-way tie flicker over two players' heads.</summary>
    private const int TitleRefreshTicks = GameConstants.TickRate * 300;

    /// <summary>Fire the first read on the very first tick (so titles are right the moment anyone logs
    /// in), then every <see cref="TitleRefreshTicks"/>.</summary>
    private void TickTitles()
    {
        if (_tick % TitleRefreshTicks == 1) RefreshTitleHolders();
    }

    private void RefreshTitleHolders() => _ = Task.Run(async () =>
    {
        try { _world.Commands.Enqueue(new TitleHoldersCmd(await _db.GetTitleHoldersAsync())); }
        catch (Exception ex) { _log.LogError(ex, "Title holder refresh failed"); }
    });

    private void ApplyTitleHolders(TitleHoldersCmd cmd)
    {
        var before = _titleHolders;
        _titleHolders = cmd.Holders;

        foreach (var p in _world.Entities.Values)
        {
            if (p.Kind != EntityKind.Player) continue;

            // A title you just won is worth SAYING — it is the whole reward, and nothing else on screen
            // would tell you. (On the first read after a restart `before` is empty, so whoever is online
            // is told once; that is the same message they would have got had they won it while online.)
            before.TryGetValue(p.Name, out var had);
            if (_titleHolders.TryGetValue(p.Name, out var has))
                foreach (var cat in has)
                    if (had is null || !had.Contains(cat))
                        SendSystemToEntity(p,
                            $"You now top the {Leaderboards.Label(cat)} board — the title "
                            + $"\"{TitleCatalog.Text(cat)}\" is yours to wear (Rank window).");

            RefreshTitle(p);
        }
    }

    /// <summary>Recompute what this player's plate should read and tell their client. Called on every
    /// board refresh, on login, and when they pick a different one.</summary>
    /// <summary>
    /// Resolve the CHOICE (<see cref="Entity.TitleCategory"/>) into the text + colour that actually go
    /// over the head. Three shapes end up in the same two fields, which is the point: a board title, a
    /// staff title and one the player wrote are indistinguishable to everything downstream.
    /// </summary>
    private void RefreshTitle(Entity p, bool notifyLoss = true)
    {
        string was = p.Title;

        if (p.TitleCategory == TitleCatalog.Custom)
        {
            // A written title survives losing a board — there is no board. It IS revoked if the right
            // to write one is taken away, or the whole grant would be pointless to withdraw.
            bool ok = p.CanWriteTitle && p.CustomTitle.Length > 0;
            p.Title = ok ? p.CustomTitle : "";
            p.TitleColor = ok ? p.CustomTitleColor : "";
        }
        else if (HoldsTitle(p, p.TitleCategory))
        {
            p.Title = TitleCatalog.Text(p.TitleCategory);
            p.TitleColor = TitleCatalog.ColorHex(p.TitleCategory);
        }
        else
        {
            p.Title = "";
            p.TitleColor = "";
        }

        // The CHOICE is deliberately left alone when the board is lost — regain it and the title comes
        // straight back on, with nothing to re-pick.
        if (notifyLoss && was.Length > 0 && p.Title.Length == 0)
            SendSystemToEntity(p, $"\"{was}\" is no longer yours — someone else tops that board.");

        SendTitles(p);
    }

    /// <summary>What this character may wear: the boards they top, plus their STAFF title if any (C17).
    /// The staff one is held by ROLE, so it is not in the board holder map and cannot be taken.</summary>
    private string[] HeldTitles(Entity p)
    {
        var staff = TitleCatalog.ForRole(p.Role);
        if (!_titleHolders.TryGetValue(p.Name, out var cats) || cats.Count == 0) return staff;
        if (staff.Length == 0) return cats.ToArray();

        var all = new List<string>(staff);
        all.AddRange(cats);
        return all.ToArray();
    }

    private bool HoldsTitle(Entity p, string title) =>
        title.Length > 0 &&
        (Array.IndexOf(TitleCatalog.ForRole(p.Role), title) >= 0
         || (_titleHolders.TryGetValue(p.Name, out var cats) && cats.Contains(title)));

    /// <summary>Worn is reported as "" when the chosen title is not currently held, so the picker shows
    /// the truth (nothing worn) even though the choice itself is still remembered.</summary>
    private void SendTitles(Entity p) =>
        SendTo(p, "Titles", new TitlesDto(
            HeldTitles(p),
            p.Title.Length > 0 ? p.TitleCategory : "",
            p.CanWriteTitle, p.CustomTitle, p.CustomTitleColor));

    private void HandleSetTitle(SetTitleCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;

        string cat = cmd.Category ?? "";

        // Switching BACK to the title you wrote. It is not "held" (nothing grants it but the right),
        // so it takes its own branch rather than one more special case inside HoldsTitle.
        if (cat == TitleCatalog.Custom)
        {
            if (!p.CanWriteTitle || p.CustomTitle.Length == 0)
            {
                SendSystemToEntity(p, "You have no title of your own to wear.");
                SendTitles(p);
                return;
            }
            p.TitleCategory = cat;
            RefreshTitle(p, notifyLoss: false);
            SendSystemToEntity(p, $"You are wearing \"{p.Title}\".");
            return;
        }

        if (cat.Length > 0 && !TitleCatalog.IsTitle(cat)) return;

        if (cat.Length > 0 && !HoldsTitle(p, cat))
        {
            SendSystemToEntity(p, "You don't hold that title.");
            SendTitles(p);   // put the client's picker back in step with the truth
            return;
        }

        p.TitleCategory = cat;
        RefreshTitle(p, notifyLoss: false);   // taking it OFF is not losing it
        SendSystemToEntity(p, cat.Length == 0
            ? "Title removed."
            : $"You are wearing \"{TitleCatalog.Text(cat)}\".");
    }

    /// <summary>
    /// `/title &lt;text&gt;` — write your own title, if you have been granted the right.
    ///
    /// Setting it also WEARS it: typing a title and then having to go and select it in a window is a
    /// step with no decision in it. `/title` with no text takes the written one off (and forgets it),
    /// which is the only way back to a bare name without opening the Rank window.
    /// </summary>
    private void HandleSetCustomTitle(SetCustomTitleCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;

        if (!p.CanWriteTitle)
        {
            SendSystemToEntity(p, "You have not been granted the right to name yourself.");
            return;
        }

        string text = (cmd.Text ?? "").Trim();
        if (text.Length == 0)
        {
            p.CustomTitle = "";
            if (p.TitleCategory == TitleCatalog.Custom) p.TitleCategory = "";
            RefreshTitle(p, notifyLoss: false);
            SendSystemToEntity(p, "Your own title is cleared.");
            return;
        }

        // The SERVER is the authority on this, not the client that also checks it — the client check
        // only saves a round trip, and a hand-rolled client could skip it entirely.
        if (!TitleCatalog.IsValidCustom(text, out string reason))
        {
            SendSystemToEntity(p, reason);
            return;
        }

        p.CustomTitle = text;
        // WHITE, not the board titles' gold (`59r`). Colour is what the rune buys; a title anyone can
        // type must not arrive already wearing the colour an earned one falls back to.
        if (p.CustomTitleColor.Length == 0) p.CustomTitleColor = TitleCatalog.CustomDefaultHex;
        p.TitleCategory = TitleCatalog.Custom;
        RefreshTitle(p, notifyLoss: false);
        SendSystemToEntity(p, $"You are wearing \"{text}\".  (use a {ItemCatalog.TitleRuneName} to recolour it)");
    }

    /// <summary>Recolour the title you wrote — a named palette only, so a written title cannot be
    /// dressed up in the PK board's dark red.
    ///
    /// <para>⚠ It requires a <see cref="ItemCatalog.TitleColorRune"/> in the bag (owner, playtest-20
    /// `59r`: *"the /titlecolor to be a item like a rune that give you the right to use the /titlecolor
    /// + clicking on the title color rune item to open the colors as a list"*). HOLDING it is the
    /// right — it is deliberately NOT consumed, because his second clause has you clicking the same
    /// rune to open the list, which a one-shot item could not survive. The chat command still works and
    /// is simply the typed form of the same gate; the rune's click sends this exact command.</para></summary>
    private void HandleSetTitleColor(SetTitleColorCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;

        if (!p.CanWriteTitle)
        {
            SendSystemToEntity(p, "You have not been granted the right to name yourself.");
            return;
        }
        if (!TitleCatalog.TryPaletteColor(cmd.Color, out string hex))
        {
            SendSystemToEntity(p, "Colours: " + TitleCatalog.PaletteNames());
            return;
        }

        if (!p.Inventory.Any(i => i.DefId == ItemCatalog.TitleColorRune))
        {
            SendSystemToEntity(p, $"You need a {ItemCatalog.TitleRuneName} to colour your title.");
            return;
        }

        p.CustomTitleColor = hex;
        RefreshTitle(p, notifyLoss: false);   // a no-op unless the written title is the one being worn
        SendSystemToEntity(p, p.TitleCategory == TitleCatalog.Custom
            ? "Title recoloured."
            : "Colour saved — it applies when you wear your own title.");
    }

    /// <summary>
    /// Push level / exp / exp-to-next.
    ///
    /// Every other caller sends this on a CHANGE — exp gained, exp lost, level up, subclass swap —
    /// and nothing sent it on ENTERING THE WORLD. So a character's EXP bar stayed blank from login
    /// until its first kill, and a MAX-LEVEL character's stayed blank forever. State the client needs
    /// on arrival has to be pushed on arrival; a change-only push assumes a client that was already
    /// watching.
    /// </summary>
    private void SendProgress(Entity p, bool leveled = false) =>
        SendTo(p, "Progress", new ProgressUpdate(
            p.Level, p.Exp, StatCalculator.ExpToNext(p.Level), leveled, p.SkillPoints));

    // ----- Debug live-tuning (admin only) -----
    private DebugConfigDto CurrentDebugConfig() => new(
        RateConfig.World.Exp, RateConfig.World.Sp, RateConfig.World.DropChance, RateConfig.World.Gold,
        _karmaBase, (float)_karmaConsecGrowth, (float)_karmaLevelGrowth, _karmaLossPerDeath, _karmaLossPerMob,
        _idleCapSeconds, _offlineCapSeconds, _graceSeconds,
        _testSkillPower, _testSkillMod,
        GameConstants.RegenIntervalSeconds, StatCalculator.ConRegenBase,
        StatCalculator.MobHpRegenPctCombat, StatCalculator.MobRegenPctIdle);

    private void HandleRequestDebugConfig(RequestDebugConfigCmd cmd)
    {
        if (TryGetPlayer(cmd.ConnectionId, out var p) && p.IsAdmin)
            SendTo(p, "DebugConfig", CurrentDebugConfig());
    }

    private void HandleSetDebugConfig(SetDebugConfigCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p) || !p.IsAdmin)
            return;
        ApplyDebugConfig(cmd.Config);
        SaveDebugConfig();   // persist between runs
        RefillAllBudgets();  // the caps just moved; the balances in the tank have to follow, or the
                             // new cap is invisible until tomorrow (see RefillAllBudgets)
        SendSystemToEntity(p, "[DEBUG] Tuning applied + saved (debug-config.json). Farm allowances refilled.");
        SendTo(p, "DebugConfig", CurrentDebugConfig());   // echo back the clamped values
    }

    private void ApplyDebugConfig(DebugConfigDto c)
    {
        // DropAmount is NOT on the panel: it is the stack-size knob, not a rate, and the two boxes read
        // as two rate knobs that must both be raised (owner, 2026-08-18 — "fix the two boxes ... to
        // become one global drop"). Carried through untouched here; `/droprate amount` still tunes it.
        RateConfig.World = new RateSet(
            c.ExpRate, c.SpRate, c.GoldRate, c.DropChanceRate, RateConfig.World.DropAmount).Clamped();
        _karmaBase          = Math.Max(0, c.KarmaBase);
        _karmaConsecGrowth  = Math.Max(1.0, c.KarmaConsecGrowth);
        _karmaLevelGrowth   = Math.Max(1.0, c.KarmaLevelGrowth);
        _karmaLossPerDeath  = Math.Max(0, c.KarmaLossPerDeath);
        _karmaLossPerMob    = Math.Max(0, c.KarmaLossPerMob);
        _idleCapSeconds     = Math.Clamp(c.IdleCapSeconds, 0, 24 * 3600);   // 0 = unlimited
        _offlineCapSeconds  = Math.Clamp(c.OfflineCapSeconds, 0, 24 * 3600);
        _graceSeconds       = Math.Clamp(c.GraceSeconds, 5, 3600);
        _testSkillPower     = Math.Max(0, c.TestSkillPower);
        _testSkillMod       = Math.Max(0f, c.TestSkillMod);

        // Regen cadence: clamped to whole ticks (the loop can't fire between them) and to a sane
        // 0.1s–60s band. 3s = IG. The stat bases go no lower than 1.0 — below that MORE of the stat
        // would mean LESS regen, which is never what you want to test.
        GameConstants.RegenIntervalTicks =
            Math.Clamp((int)MathF.Round(c.RegenIntervalSeconds * GameConstants.TickRate), 1, 600);
        StatCalculator.ConRegenBase = Math.Clamp(c.ConRegenBase, 1f, 1.2f);

        // In combat: 0 = mobs never heal in a fight (defensible — the level-gap lockout is the real
        // anti-underlevelled rule); 0.1 = 10% of the bar per second, faster than most players out-damage.
        // Idle is floored at 0.001 rather than 0 because ResetMob no longer heals: at exactly 0 a mob
        // that once took a scratch would carry it until something killed it.
        StatCalculator.MobHpRegenPctCombat = Math.Clamp(c.MobHpRegenPctCombat, 0f, 0.1f);
        StatCalculator.MobRegenPctIdle = Math.Clamp(c.MobRegenPctIdle, 0.001f, 1f);
    }

    // Lives NEXT TO THE EXE (Debug/publish output), like an options.ini — NOT a build item, so an
    // update/rebuild never overwrites it; created with defaults on first run.
    private static readonly string DebugConfigFile =
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "debug-config.json");

    private void LoadDebugConfig()
    {
        try
        {
            if (System.IO.File.Exists(DebugConfigFile))
            {
                if (System.Text.Json.JsonSerializer.Deserialize<DebugConfigDto>(
                        System.IO.File.ReadAllText(DebugConfigFile)) is DebugConfigDto c)
                    ApplyDebugConfig(c);
            }
            else
            {
                SaveDebugConfig();   // create the default file so it's there to edit
            }
        }
        catch { /* ignore a malformed debug config */ }
    }

    private void SaveDebugConfig()
    {
        try { System.IO.File.WriteAllText(DebugConfigFile, System.Text.Json.JsonSerializer.Serialize(CurrentDebugConfig())); }
        catch { /* best-effort */ }
    }

    /// <summary>Yourself, or someone in your party. This is the ONLY set a support skill (heal /
    /// restore / cleanse / buff) may reach. Every member of a party maps to the same Party object,
    /// so reference equality is the membership test.</summary>
    private bool SameParty(Entity a, Entity b) =>
        a.Id == b.Id
        || (_world.Parties.TryGetValue(a.Id, out var pa)
            && _world.Parties.TryGetValue(b.Id, out var pb)
            && ReferenceEquals(pa, pb));

    /// <summary>May <paramref name="caster"/> land a SINGLE-TARGET support skill — heal, MP restore,
    /// cleanse, buff or resurrect — on <paramref name="target"/>? (`BL-59`.)
    ///
    /// <para>🔑 He RE-SPECCED this on 2026-08-14 and the new rule is TARGET-based. The old one was
    /// about the caster (*"you cannot res a party member while YOU are flagged"*); that entry is
    /// superseded. What he wants now:</para>
    /// <list type="bullet">
    ///   <item>a NON-party player may be supported <b>only while they are clean</b> (white);</item>
    ///   <item>a pvp-flagged or PK player may be supported <b>only by their own party</b>;</item>
    ///   <item>doing it anyway — i.e. from inside their party — <b>flags the supporter</b>
    ///         (<see cref="FlagForSupporting"/>, which already existed for heals and MP);</item>
    ///   <item>res inside the party is fine for <b>both</b> pvp and pk.</item>
    /// </list>
    ///
    /// <para>⚠ This OPENS something that used to be shut. Support was party-only: the manual cast
    /// tested <see cref="SameParty"/> and anything else fell through to a self-cast. Helping a passing
    /// stranger is now legal, and the flag is what prices it — which is the whole point of moving the
    /// test from the caster to the target.</para>
    ///
    /// <para>⚠ MINE, not his: the duel clause at the end. Falling through to self when the target is
    /// someone you are actually fighting is a fix he reported himself (*"healing mid-duel healed the
    /// man you were fighting"*), and opening non-party support would have quietly undone it for the
    /// window where the person you attacked is still white. Party members are exempt, since two
    /// party members are not duelling each other.</para></summary>
    private bool CanSupport(Entity caster, Entity target)
    {
        if (caster.Id == target.Id) return true;
        bool sameParty = SameParty(caster, target);
        // Flagged or red? Only their own party may help them.
        if (FlagOf(target) != PvpFlag.Innocent) return sameParty;
        if (sameParty) return true;
        // Clean stranger: allowed, unless the two of you are mid-fight.
        return target.LastPvpAttackerId != caster.Id && caster.LastPvpAttackerId != target.Id;
    }

    /// <summary>May 'attacker' damage 'target'? A mob on either side = always (normal PvE). Player→
    /// player requires out of safe zones, and: a RED/PURPLE target is freely attackable (retaliation
    /// / executing an outlaw), while attacking an INNOCENT (white) needs the PvP opt-in.
    ///
    /// YOUR OWN PARTY IS NEVER ATTACKABLE — not with PvP on, not if they have gone red, not by any
    /// route (owner, playtest-15). This is a usability rule, not a karma one: in a mass fight a
    /// mis-tap on the ally standing next to you would silently turn you into the enemy's best asset.
    /// Because it lives in here, every path inherits it at once — basic attack, offensive skills, the
    /// autopilot's retaliation and the PvP counter-attack all ask this one question.</summary>
    private bool CanPvpHit(Entity attacker, Entity target)
    {
        if (attacker.Kind != EntityKind.Player || target.Kind != EntityKind.Player)
            return true;
        if (attacker.Id == target.Id || target.Dead)
            return false;
        if (SameParty(attacker, target))
            return false;
        if (GameConstants.InSafeZone(attacker.X, attacker.Y) || GameConstants.InSafeZone(target.X, target.Y))
            return false;
        return FlagOf(target) != PvpFlag.Innocent || attacker.PvpEnabled;
    }

    /// <summary>Why a swing at this player was refused. Split out so the party rule can say so
    /// plainly — "enable PvP" is misleading advice when PvP would not have helped.</summary>
    private string PvpRefusalReason(Entity attacker, Entity target) =>
        SameParty(attacker, target)
            ? "You can't attack a member of your own party."
            : "You can't attack that player here. (Enable PvP; not in towns.)";

    /// <summary>Award kill consequences for a player killing a player: an INNOCENT victim → PK
    /// (karma + red name, consecutive/level-scaled); a FLAGGED/RED victim → a justified PvP kill.</summary>
    private void ApplyPvpKill(Entity killer, Entity victim)
    {
        if (FlagOf(victim) == PvpFlag.Innocent)
        {
            // Per-kill karma is driven by the LEVEL GAP (killer − victim), owner's quadratic curve
            // (2026-07-16): a flat baseline up to a +10 gap, then it "skyrockets" to the per-kill cap at
            // a +50 gap. This replaced an exponential 1.2^gap that was CLAMPED at gap 15 — so a huge gap
            // (a lvl-82 on a lvl-1) actually UNDER-awarded (~3k) instead of hitting the cap.
            //   karma(gap) = base                                , gap ≤ 10
            //              = base + (cap-base)·((gap-10)/40)²     , 10 < gap < 50   (quadratic)
            //              = cap                                  , gap ≥ 50
            // (_karmaLevelGrowth is no longer used by this curve; it's kept only for config compat.)
            int gap = Math.Max(0, killer.Level - victim.Level);
            double levelKarma =
                gap <= KarmaGapFloor ? _karmaBase :
                gap >= KarmaGapCap   ? KarmaMaxPerKill :
                _karmaBase + (KarmaMaxPerKill - _karmaBase)
                    * Math.Pow((gap - KarmaGapFloor) / (double)(KarmaGapCap - KarmaGapFloor), 2);

            // Consecutive-PK growth still multiplies on top; the final per-kill amount is capped at 15k.
            int consec = Math.Min(killer.ConsecutivePk, 15);
            double raw = levelKarma * Math.Pow(_karmaConsecGrowth, consec);
            int gain = (int)Math.Clamp(raw, 0, KarmaMaxPerKill);
            killer.Karma += gain;
            killer.ConsecutivePk++;
            killer.PkCount++;
            // PKing costs REPUTATION: drain both charisma values by karma × 0.01 (persisted by the
            // SaveEntity(killer) at the end of this method). A griefer can't sit atop the charisma board.
            GrantCharisma(killer, -(int)Math.Round(gain * GameConstants.CharismaKillPenaltyPerKarma),
                                  -(long)Math.Round(gain * GameConstants.CharismaKillPenaltyPerKarma));
            SendSystemToEntity(killer, $"You killed an innocent — Karma +{gain} (now {killer.Karma:N0}). You are now a PK.");
        }
        else
        {
            killer.PvpCount++;
            SendSystemToEntity(killer, $"PvP kill. (Total {killer.PvpCount})");
        }
        SendPvpState(killer);
        SaveEntity(killer);
    }

    /// <summary>Shed karma (death or a mob kill). Clears the red flag + resets the consecutive-PK
    /// counter at 0. Refreshes the HUD; only persists on the meaningful transition to 0 (the interim
    /// decreasing value rides the 60s autosave, so a grinding PK doesn't hammer the DB).</summary>
    private void ReduceKarma(Entity p, int amount)
    {
        if (p.Karma <= 0)
            return;
        p.Karma = Math.Max(0, p.Karma - amount);
        SendPvpState(p);
        if (p.Karma == 0)
        {
            p.ConsecutivePk = 0;
            BroadcastSystem($"{p.Name}'s karma has cleared.");
            SaveEntity(p);
        }
    }

    /// <summary>Save a character without blocking the tick loop. The snapshot is taken
    /// HERE (on the single-writer thread) so the async DB write never reads the live,
    /// mutating entity; the DB I/O runs off-thread.</summary>
    /// <summary>Snapshot + queue a background save. Returns the write's Task so a caller that must
    /// not race it (the leave-to-character-select path) can await; everything else ignores it.</summary>
    private Task SaveEntity(Entity entity)
    {
        if (PersistenceService.CharacterSnapshot.From(entity) is { } snap)
            return RunSave(() => _db.SaveCharacterAsync(snap));
        return Task.CompletedTask;
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

        SaveDirtyBudgets();
    }

    /// <summary>Write back every account allowance that has been spent since the last save. Snapshots
    /// the values on THIS thread and hands primitives to the background write, so the live object is
    /// never read off the tick. The Dirty flag is what stops an idle account rewriting its row every
    /// minute for nothing.</summary>
    private void SaveDirtyBudgets()
    {
        foreach (var b in _world.AccountBudgets.Values)
        {
            if (!b.Dirty) continue;
            b.Dirty = false;
            var (id, auto, off, date, ac, oc) =
                (b.AccountId, b.AutoTicksLeft, b.OfflineTicksLeft, b.LastResetDate,
                 b.AutoCapSeconds, b.OfflineCapSeconds);
            RunSave(() => _db.SaveAccountBudgetAsync(id, auto, off, date, ac, oc));
        }
    }

    /// <summary>Flush ONE account's allowance immediately — used on the paths where the character is
    /// leaving for good, so a crash before the next autosave can't hand back time already spent.</summary>
    private void SaveBudgetOf(Entity p)
    {
        if (p.AccountId == 0 || !_world.AccountBudgets.TryGetValue(p.AccountId, out var b) || !b.Dirty)
            return;
        b.Dirty = false;
        var (id, auto, off, date, ac, oc) =
            (b.AccountId, b.AutoTicksLeft, b.OfflineTicksLeft, b.LastResetDate,
             b.AutoCapSeconds, b.OfflineCapSeconds);
        RunSave(() => _db.SaveAccountBudgetAsync(id, auto, off, date, ac, oc));
    }

    /// <summary>Fire-and-forget a DB write off the tick thread, logging any failure
    /// (so an exception in a background save can't go unobserved).</summary>
    private Task RunSave(Func<Task> save) => Task.Run(async () =>
    {
        try { await save(); }
        catch (Exception ex) { _log.LogError(ex, "Background character save failed"); }
    });

    private void HandleMove(MoveCmd move)
    {
        if (!TryGetPlayer(move.ConnectionId, out var entity) || entity.Dead)
            return;

        // Standing up no longer blocks MOVEMENT (only actions) — you can walk off the moment you stand.
        // Blocking movement here while the client had already stood you up was the rubber-band.

        // Casting roots you — movement is rejected until the cast finishes or you
        // cancel it explicitly (ESC). Moving does NOT cancel the cast.
        if (entity.CastingSkillId is not null)
            return;

        // A move-tap while sitting does NOTHING (owner) — you must stand up first (sit/stand toggle),
        // which starts the stand-up delay. Tapping the ground no longer silently stands you.
        if (entity.MoveState == MoveState.Sitting)
            return;

        entity.Engaged = false;
        entity.CombatTargetId = null;
        entity.QueuedSkillId = null;
        entity.FollowTargetId = null;   // a manual move breaks a follow
        entity.AttackCommandTargetId = null;   // ...and withdraws the order to melee

        float tx = Math.Clamp(move.Move.TargetX, GameConstants.WorldMinX, GameConstants.ZoneWidth);
        float ty = Math.Clamp(move.Move.TargetY, GameConstants.WorldMinY, GameConstants.ZoneHeight);

        // WALLS: you may only walk within the domain you're STANDING in — the positive overworld can't be
        // walked out of into the negative dungeon/jail quadrant, and a dungeon can't be walked out of.
        // Only a teleport crosses between them. (Jail is confined separately, just below.)
        (tx, ty) = ConfineToDomain(entity, tx, ty);

        // JAILED players may walk, but only inside the cell (owner): clamp the destination back onto the
        // jail circle instead of rejecting the move outright, so they can pace around rather than stand
        // frozen. Escape skills/scrolls are blocked separately (TeleportsToTown).
        if (entity.Jailed)
            (tx, ty) = ClampToJail(tx, ty);

        entity.TargetX = tx;
        entity.TargetY = ty;
    }

    /// <summary>Clamp a move destination to the domain the player is CURRENTLY in: the positive overworld
    /// [0,Zone], the jail circle, or — when they're in the negative quadrant — the bounding box of the
    /// dungeon they're in (or nearest to). This is the "wall". Teleport/PlaceEntity do NOT go through
    /// here, so they alone can move a player across a domain boundary. Jailed players are confined by
    /// ClampToJail instead.
    ///
    /// <para>The geometry itself lives in <see cref="WorldDomain"/> (Game.Shared) because the CLIENT now
    /// enforces the same wall before it ever sends a move (0.57.0, B10) — this clamp is the anti-cheat
    /// backstop, not the everyday mechanism, and the two halves must not be able to disagree.</para></summary>
    private static (float, float) ConfineToDomain(Entity e, float tx, float ty)
    {
        if (e.Jailed) return (tx, ty);
        return WorldDomain.At(e.X, e.Y).Clamp(tx, ty);
    }

    /// <summary>Safety net for broken geodata / a prediction slip: if a (non-jailed) player has ended up
    /// more than <see cref="WallTolerance"/> OUTSIDE every dungeon while in the negative quadrant, a ward
    /// teleports them back into the nearest dungeon. Movement is already walled; this catches the rest.</summary>
    private const float WallTolerance = 500f;
    private void EnforceDungeonWalls(Entity p)
    {
        if (p.Jailed || (p.X >= 0 && p.Y >= 0)) return;   // jail + overworld handled elsewhere

        // The jail is a legitimate place to STAND without being jailed (an admin visiting an inmate via
        // /tp). It sits in the negative quadrant but is not a dungeon, so without this check the ward
        // below fired on every visitor and yanked them into the nearest dungeon — the owner's
        // "/jail test1 then /tp test1 puts me in the dungeon, not the jail".
        if (InJail(p.X, p.Y)) return;

        // Measured against each dungeon's real WORLD — outline plus entrance — not its bounding box.
        // The box was 45% ground that is not the Hollow Crypt (playtest-20 `61h`), so the ward read
        // "safely inside" for a player standing well outside the dungeon he could see.
        Region? nearest = null; float best = float.MaxValue;
        foreach (var d in RegionMap.Dungeons)
        {
            float outside = WorldDomain.OfDungeon(d).DistanceOutside(p.X, p.Y);
            if (outside <= WallTolerance) return;   // inside, or within the tolerance band at the wall
            if (outside < best) { best = outside; nearest = d; }
        }
        if (nearest != null)
        {
            var a = nearest.Arrival(_rng);
            PlaceEntity(p, a.X, a.Y);
            SendSystemToEntity(p, "A ward pulls you back inside the dungeon.");
        }
    }

    /// <summary>Is this point inside the jail cell? The jail is its own DOMAIN (like the overworld or a
    /// dungeon), so both the movement wall and the ward have to recognise it — otherwise a non-jailed
    /// visitor standing there looks like someone loose in the negative quadrant.</summary>
    private static bool InJail(float x, float y) => WorldDomain.Jail.Contains(x, y);

    /// <summary>Pull a point back inside the jail cell, keeping its direction from the centre.</summary>
    private static (float X, float Y) ClampToJail(float x, float y) => WorldDomain.Jail.Clamp(x, y);

    private void HandleSetMoveState(SetMoveStateCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (player.StandUpTicks > 0) return;   // mid stand-up, ignore

        // Sitting requires being idle (not engaged / not casting).
        if (cmd.State == MoveState.Sitting && (player.Engaged || player.CastingSkillId is not null))
            return;

        // Standing UP from a sit costs the stand-up recovery (the standing animation): you can't move,
        // cast or act until it elapses. Walk<->Run while already standing stays instant.
        //
        // EXCEPT after a real rest (owner): if you have been seated at least SettledSeconds, standing is
        // INSTANT. The recovery exists to stop sit/stand spam — tapping sit for the regen tick and
        // popping straight back up — and that is the only thing it should cost. Someone who actually sat
        // down to rest has already paid far more time than the delay, so charging them again just makes
        // resting feel bad. Being HIT while seated still applies the full delay (see Kill/damage path):
        // that is a combat interrupt, not a voluntary stand, and it must stay punishing.
        if (player.MoveState == MoveState.Sitting && cmd.State != MoveState.Sitting)
        {
            long seatedTicks = _tick - player.SatDownTick;
            if (seatedTicks < MovementTuning.SettledSeconds * GameConstants.TickRate)
                player.StandUpTicks = MovementTuning.StandUpTicks;
        }

        player.MoveState = cmd.State;
        if (cmd.State == MoveState.Sitting)
        {
            player.SatDownTick = _tick;
            player.TargetX = null;
            player.TargetY = null;
        }
        SendStats(player);
    }

    private void HandleAttack(AttackCmd attack)
    {
        if (!TryGetPlayer(attack.ConnectionId, out var attacker) || attacker.Dead)
            return;

        // Can't act during the stand-up recovery (nor while still sitting).
        if (attacker.StandUpTicks > 0 || attacker.MoveState == MoveState.Sitting)
            return;

        if (attack.TargetId == attacker.Id ||
            !_world.Entities.TryGetValue(attack.TargetId, out var target) ||
            target.Dead ||
            DistanceSq(attacker, target) > GameConstants.ViewRange * GameConstants.ViewRange)
            return;

        // NPCs are scenery with a job — vendors, teleporters, quest givers. Nothing stopped a client
        // from attacking one, and since they carry HP like any entity they could be KILLED (found on
        // the phone, 2026-07-21: the Unity client targeted an NPC and killed it). The WPF client only
        // ever avoided it by never sending the command, which is not a rule — this is.
        if (target.Kind == EntityKind.Npc)
        {
            SendSystemToEntity(attacker, "You can't attack " + target.Name + ".");
            return;
        }

        // You cannot swing at what you cannot see (BL-69). Silent, unlike the NPC refusal above: the
        // whole point of a hide is that there is nothing there to be told about.
        if (!CanSee(attacker, target))
            return;

        if (target.Kind == EntityKind.Player && !CanPvpHit(attacker, target))
        {
            SendSystemToEntity(attacker, PvpRefusalReason(attacker, target));
            return;
        }

        attacker.QueuedSkillId = null;
        CancelCast(attacker);
        attacker.FollowTargetId = null;   // attacking breaks a follow
        attacker.CombatTargetId = target.Id;
        attacker.Engaged = true;
        // THIS is the explicit order to melee. Everything that closes the distance to swing traces
        // back to here; nothing else may set it.
        attacker.AttackCommandTargetId = target.Id;
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

        // Spellcaster Mastery — every mage has it from level 1 and NOTHING replaces it: it is the one
        // place the "robe or nothing / wand or nothing" rule lives, so a 2nd-class mastery can be pure
        // bonus (2026-08-07 restructure). It supersedes the retired Weapon Proficiency, which is
        // stripped here so an existing character stops carrying a dead skill in their window.
        if (player.BaseClass == BaseClass.Mage)
        {
            player.LearnedSkills.TryAdd(SkillCatalog.SpellcasterMastery, 1);
            player.LearnedSkills.Remove(SkillCatalog.WeaponProficiency);
        }

        // ⚠ Robe Armor Mastery is NOT auto-granted any more — it is a bonus-only skill bought off the
        // class table at 7/14. It is also the one a nuker/cleric mastery Replaces, and re-granting it
        // was the bug that erased their +max MP, +P.Def and the whole mpWhenRestored bonus: the pick
        // in RecomputeDerived went by dictionary order and the re-added level-1 skill won.
        //
        // MIGRATION: it dropped from 3 levels to 2 in the same pass. A saved character sitting on
        // level 3 would ask for a rung that no longer exists — ArmorMasteryAt is bounds-safe and
        // returns null, so the mage would quietly lose his robe P.Def entirely rather than crash.
        // Clamp instead. Harmless once no character carries the old level.
        if (player.SkillLevelOf(SkillCatalog.MasteryRobe) is int robeLv && robeLv > 0
            && SkillCatalog.Get(SkillCatalog.MasteryRobe)?.ArmorMasteryLevels is { Length: > 0 } robeRungs
            && robeLv > robeRungs.Length)
            player.LearnedSkills[SkillCatalog.MasteryRobe] = robeRungs.Length;

        // ---- Divine Focus — DELETED 2026-08-20 (owner): *"Remove the Divine Focus of cleric/buffers/
        //      healers -> if a healer wants to use sword so be it, swords have lower mAtk and no cast
        //      speed atri"*. The penalty (heal output ×0.5, ×0.75 for a Warchanter, while holding no
        //      wand/staff) is gone, together with the M.Atk ×0.6 that Spellcaster Mastery charged for
        //      the same choice — the WEAPON's own numbers are the trade now.
        //      ⚠ It was AUTO-GRANTED, so every healer alive carries the id. Strip it on login, or it
        //      sits in their skill window forever as a passive with no def behind it. ----
        player.LearnedSkills.Remove("divine_focus");

        // Resurrection is NOT auto-granted (owner, 2026-07-17) — it is bought with SP off the class
        // tables like any other skill: L1 @20 / IG @40 on the cleric list (every cleric keeps those
        // through any 3rd class), L3 @52 / L4 @61 on the Lightbringer list. See ClassSkillTables.

        // (The old combat-"training" passive that stood in for soul/spell runes is GONE — runes are now
        // held RUNE items that grant the same buff, see ReconcileRuneBuffs / SkillCatalog.WarRuneBuff.)

        // Class identity "sure" floor passive for the current class tier (level = tier).
        // ⚠ The DISCIPLINE is passed because the rogue's ladder is tied to the CLASS CHANGE, not to
        // the level (owner, 2026-08-07): Lv1 at the 2nd class, Lv2 only on taking a MELEE discipline,
        // and Lv3 never — its milestone is the 4th class change, which does not exist yet. Plain
        // assignment, so picking a bow discipline at 40 DOWNGRADES a granted Lv2 back to Lv1, and a
        // rogue who hits 76 no longer silently gains a Lv3. Both are intended. See FloorPassiveFor.
        if (SkillCatalog.FloorPassiveFor(player.Archetype, player.Level, player.Discipline) is { } floor)
            player.LearnedSkills[floor.Id] = floor.Level;

        // The SECOND identity passive — the skill-defence channels (BL-07 warrior Deflection /
        // BL-08 tank Backlash). Its own ladder because it starts at the 3rd class change, not the
        // 2nd. Plain assignment like the floor above, so a rung is never stuck once granted.
        if (SkillCatalog.ReflectPassiveFor(player.Archetype, player.Level) is { } reflect)
            player.LearnedSkills[reflect.Id] = reflect.Level;

        // Class Balance passive — the per-class tuning hook. ⚠ COMMENTED OUT 2026-08-07 on the
        // owner's ruling (*"class_balance should be commented for now"*, playtest-19 `0a`): the defs
        // and this grant come out of the live path but stay in the file, ready to come back. They
        // were all-zero PassiveEffects, so nothing changes numerically. DO NOT DELETE.
        // player.LearnedSkills[SkillCatalog.ClassBalanceFor(player.Archetype, player.BaseClass)] = 1;
        // …and strip the ones already persisted on existing characters — the defs are gone from the
        // catalog, so a leftover entry is a learned id nothing can render. Delete this loop when the
        // hook comes back.
        // (No such cleanup is needed for `reflexes` / `archer_*_mastery`, deleted in the same pass:
        //  nothing has ever carried Archetype.Archer since the merge, so nobody was granted them.)
        foreach (var id in SkillCatalog.ClassBalanceIds) player.LearnedSkills.Remove(id);

        // Novice's Grace — display-only newbie protection, shown below the death-penalty level and removed at it.
        if (player.Level < GameConstants.DeathExpPenaltyMinLevel)
            player.LearnedSkills.TryAdd(SkillCatalog.NoviceGrace, 1);
        else
            player.LearnedSkills.Remove(SkillCatalog.NoviceGrace);

        // ==================== TEST ONLY — DELETE ME ====================
        // (The power-1000 test heal was auto-granted here at 76 until 2026-08-12, `BL-37`. Deleted:
        //  both numbers it existed to read are decided. A character who still carries `test_heal` in a
        //  saved row loses it on the next load — PersistenceService.ParseLearnedSkills now drops any id
        //  the catalog no longer knows, which is what makes deleting a skill a one-file job.)
        // Two debug damage skills at ANY level — Flat/Mod come from the Debug panel (TestSkillPower/
        // TestSkillMod), for reading the {Flat, Mod} damage curve live.
        player.LearnedSkills.TryAdd(SkillCatalog.TestPhysSkill, 1);
        player.LearnedSkills.TryAdd(SkillCatalog.TestMagicSkill, 1);
        // ==============================================================

        // The universal Return skill (teleport to nearest town) IS a learned skill — everyone has it.
        // The SCROLL versions are NOT: the item grants them. You don't learn a scroll, you use it —
        // double-clicking the scroll invokes its skill directly (see UsePotion), which needs no
        // learned entry. They used to be auto-learned, which wrongly put them in your skill list.
        player.LearnedSkills.TryAdd(SkillCatalog.ReturnSkill, 1);
        player.LearnedSkills.Remove(SkillCatalog.ScrollReturnSkill);
        player.LearnedSkills.Remove(SkillCatalog.ScrollReturnUltSkill);
        player.LearnedSkills.Remove(SkillCatalog.ScrollResurrectSkill);
        player.LearnedSkills.Remove(SkillCatalog.ScrollResurrectUltSkill);

        // Angel's Protection (noblesse) — every class learns it at 76 for now. LATER it becomes a long
        // subclass quest reward (see the death/res design note); this auto-grant is the stopgap.
        //
        // The right to WRITE YOUR OWN TITLE rides on exactly the same gate (owner, 2026-08-07): the two
        // are meant to be rewards of the SAME quest once that quest exists. Granting both from ONE
        // place now means that quest replaces a single condition later, instead of hunting down two
        // that have quietly drifted apart — which is the whole reason he asked for them together.
        if (player.Level >= 76)
        {
            player.LearnedSkills.TryAdd(SkillCatalog.AngelsProtection, 1);

            // Announced and pushed only on the EDGE, so it lands once — the moment you hit 76, or on
            // the first login of a character who was already past it. This runs from OnLevelUp as well
            // as from login, and nothing else would tell the client the Rank window has grown a new
            // row: SendTitles is the only push that carries the right.
            if (!player.MayWriteTitle)
            {
                player.MayWriteTitle = true;
                SendSystemToEntity(player,
                    $"You may name yourself: /title <text> ({TitleCatalog.MaxCustomLength} characters), "
                    + "/titlecolor <colour>.");
                SendTitles(player);
            }
        }
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

        // Mutually-exclusive group: you may hold ONE skill per group, and the choice is permanent.
        // Only blocks the FIRST level — levelling the one you already picked is fine.
        //
        // ⚠ STAT SWAPS ARE EXEMPT since 2026-08-10. Their limits are numeric now (+5 per stat, 9 rungs
        // total) and a stat may legitimately be raised by two different pairs, so "one per group" would
        // contradict the rule below. They keep an ExclusiveGroup only so the reset NPC can still find
        // them — see SkillCatalog.StatSwapConflict and ResettableSkillsOf.
        bool isStatSwap = SkillCatalog.StatSwapOf(def.Id) is not null;
        if (cur == 0 && !isStatSwap && !string.IsNullOrEmpty(def.ExclusiveGroup))
        {
            foreach (var (learnedId, _) in player.LearnedSkills)
            {
                if (learnedId == def.Id) continue;
                if (SkillCatalog.Get(learnedId) is not SkillDef other
                    || other.ExclusiveGroup != def.ExclusiveGroup) continue;
                SendSystemToEntity(player,
                    $"You have already committed to {other.Name}. It cannot be combined with {def.Name}.");
                return;
            }
        }

        // THE STAT-SWAP LIMITS: at most +5 on any one stat, and 9 rungs in total across every swap.
        // Checked at EVERY level, not just the first — each level is another rung against the budget.
        if (SkillCatalog.StatSwapConflict(def.Id, target, player.LearnedSkills) is { } clash)
        {
            SendSystemToEntity(player, clash);
            return;
        }

        int cost = def.SpCostAt(target);
        if (player.SkillPoints < cost)
        {
            SendSystemToEntity(player, $"Not enough skill points ({cost} needed).");
            return;
        }

        // GOLD price. A stat swap is priced PER RUNG by how many rungs the character already owns
        // (1/2/3/4/5/5/5/5/5 kk), so the same nine cost 35kk however they are spread; everything else
        // uses its own authored per-level cost.
        long gold = isStatSwap
            ? SkillCatalog.StatSwapPriceRange(
                  SkillCatalog.StatSwapRungsOwned(player.LearnedSkills),
                  SkillCatalog.StatSwapRungsOwned(player.LearnedSkills) + (target - cur))
            : def.GoldCostAt(target);
        if (gold > 0 && player.Gold < gold)
        {
            SendSystemToEntity(player,
                $"{def.Name} (Lv.{target}) costs {gold:N0} {GameConstants.CurrencyName}.");
            return;
        }

        player.SkillPoints -= cost;
        if (gold > 0) player.Gold -= gold;
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
        if (gold > 0) SendGold(player);
        SaveEntity(player);
    }

    private void HandleSkill(SkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var caster) || caster.Dead)
            return;

        // No casting during the stand-up recovery, nor while seated — stand first.
        if (caster.StandUpTicks > 0 || caster.MoveState == MoveState.Sitting)
            return;

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null || !caster.HasSkill(def.Id))
            return;

        // Passives (armor masteries) are always-on; they can't be cast.
        if (def.Category == SkillCategory.Passive)
            return;

        // JAILED players can't ESCAPE — no Return / teleport-to-town skills (owner).
        if (caster.Jailed && def.TeleportsToTown)
        {
            SendSystemToEntity(caster, "You can't escape while jailed.");
            return;
        }

        // Stunned/feared casters can't act.
        if (caster.IsActionLocked)
        {
            SendSystemToEntity(caster, caster.IsStunned ? "You are stunned." : "You are too afraid to act.");
            return;
        }

        // Toggle skills (stances) flip instantly: on -> apply self-buff, off -> remove it.
        if (def.Toggle)
        {
            HandleToggle(caster, def);
            return;
        }

        if (caster.SkillCooldowns.TryGetValue(def.Id, out int cd) && cd > 0)
        {
            SendSystemToEntity(caster, $"{def.Name} is not ready.");
            return;
        }

        // 🔑 THE GATE ASKS FOR THE WHOLE PRICE, AND FOR THE PRICE THIS CASTER ACTUALLY PAYS (owner,
        // 2026-08-20). Two things, both easy to get wrong:
        //   - the WHOLE price, not the 20% charged up front — the split is an engine detail and a cast
        //     you cannot afford to finish must never start;
        //   - the EFFECTIVE price, so a debuff that triples MP cost really does lock you out of a
        //     100-MP skill below 300, and a 20% reduction lets you cast it at 80. The old check read
        //     the authored number and ignored both.
        if (caster.Mp < EffectiveMpCost(caster, def, Math.Max(1, caster.SkillLevelOf(def.Id))))
        {
            SendSystemToEntity(caster, "Not enough MP.");
            return;
        }

        // An HP price is refused exactly like an MP one (owner, playtest-20 `55c` — a standing rule,
        // not just Restore Spirit). STRICTLY greater, not >=: ExecuteSkill clamps the payment with
        // Math.Max(1, …), so casting at exactly the cost used to survive on 1 HP instead of being
        // refused, and casting below it was a discount. Refusing here makes the price real.
        int hpPrice = def.HpCostAt(caster.SkillLevelOf(def.Id));
        if (hpPrice > 0 && caster.Hp <= hpPrice)
        {
            SendSystemToEntity(caster, "Not enough HP.");
            return;
        }

        // Reagent gate: skills with a ConsumableId need that item to cast. Checked up
        // front for feedback; actually consumed when the cast completes (in ExecuteSkill).
        if (!string.IsNullOrEmpty(def.ConsumableId) &&
            CountItem(caster, def.ConsumableId) < def.ConsumableAmount)
        {
            string itemName = ItemCatalog.Get(def.ConsumableId)?.Name ?? def.ConsumableId;
            SendSystemToEntity(caster, $"{def.Name} requires {def.ConsumableAmount}x {itemName}.");
            return;
        }

        // Weapon requirement: a skill gated to certain weapon types (Strike = sword/blunt,
        // Stab = dual, Shot = bow) can only be used while a matching weapon is equipped.
        if (def.RequiredWeapon != WeaponType.None && (def.RequiredWeapon & caster.WeaponType) == 0)
        {
            string need = def.RequiredWeapon.ToString().ToLowerInvariant().Replace(",", " or");
            SendSystemToEntity(caster, $"{def.Name} requires a {need} weapon.");
            return;
        }

        // HP-gated activation (warrior Battle Presence/Defence): only usable at low HP.
        if (def.RequireHpBelowFraction > 0f && caster.Hp > caster.MaxHp * def.RequireHpBelowFraction)
        {
            SendSystemToEntity(caster, $"{def.Name} can only be used at or below {(int)(def.RequireHpBelowFraction * 100)}% HP.");
            return;
        }

        bool offensive = (def.Effect & (SkillEffect.PhysicalDamage
            | SkillEffect.MagicDamage | SkillEffect.AnyDebuff | SkillEffect.Cancel | SkillEffect.Taunt)) != 0;

        Guid targetId;
        // A support skill lands on a party member only if IsAllyTargetable says so — see that helper for
        // why the test cannot be Effect-only.
        if (def.PlacesTrap || def.PlacesTotem || def.GrantsHide)
        {
            // Self-delivered: a trap drops at the caster's feet, a totem is planted there, stealth
            // cloaks the caster. Even though these carry damage/CC/heal flags (their deferred payload),
            // they need no live target — and a totem must reach this arm or it would be refused for
            // having nothing selected.
            targetId = caster.Id;
        }
        else if (def.Resurrect)
        {
            // Resurrection is the one skill that WANTS a dead target (a fallen ally chosen via the party
            // window or a Shift-click corpse-select). A dead caster can't cast at all (refused above), so
            // the skill only ever revives someone else — self-res is the scroll's job (item-use path).
            if (cmd.TargetId is not Guid rid || rid == caster.Id ||
                !_world.Entities.TryGetValue(rid, out var corpse) ||
                corpse.Kind != EntityKind.Player || !corpse.Dead ||
                DistanceSq(caster, corpse) > GameConstants.ViewRange * GameConstants.ViewRange)
            {
                SendSystemToEntity(caster, "Resurrection needs a fallen ally as its target.");
                return;
            }
            // `BL-59`: an outlaw's corpse is his party's business. Death does not clear karma, so a
            // dead PK is still red here — which is what makes "res in the same party is allowed for
            // both pvp and pk" a real permission rather than a technicality. A REFUSAL, not a
            // fall-through to self: a res has no self-cast meaning, and silently doing nothing over a
            // body is the kind of thing that reads as a broken skill.
            if (!CanSupport(caster, corpse))
            {
                SendSystemToEntity(caster,
                    $"{corpse.Name} is an outlaw — only their own party can resurrect them.");
                return;
            }
            // 🔴 THE FLAG IS PAID AT THE START OF THE CAST, not when the corpse accepts (playtest 23):
            // *"the flag should happen at the initializing the resurrect ..not after the dead agrees to
            // resurrect. In a mass pvp if my friend is dead(flagged/pk) and I start to resurrect I become
            // pvp while I resurrect him ... So other ppl can kill me or attempt to stop the
            // resurrection."* It used to be charged in ResurrectTarget, i.e. after a 10s channel AND a
            // prompt the corpse might never answer — so the whole window in which someone could contest
            // the res was a window in which the resurrector was untouchable. This is the point of the
            // rule: raising an outlaw is an act, and it prices itself while it is happening.
            //
            // ⚠ Charged here rather than at cast COMPLETION for the same reason — completion is already
            // 10 seconds too late. It is a no-op on an innocent target, so a normal res still flags
            // nobody; and it is not refunded if the cast is interrupted, which is correct: you were
            // visibly holding a channel over an outlaw's body.
            FlagForSupporting(caster, corpse);
            targetId = rid;
        }
        else if (offensive)
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
            // The SAME rule as a basic attack: NPCs are not valid targets for an offensive skill
            // either. Guarding only HandleAttack left this route wide open — the owner could still
            // land nukes on a vendor (they just could not kill him, because damage floors at 1 HP).
            // Two entry points to "hit that thing" need the same answer at both.
            if (target.Kind == EntityKind.Npc)
            {
                SendSystemToEntity(caster, "You can't attack " + target.Name + ".");
                return;
            }
            // You cannot aim at what you cannot see (BL-69). The snapshot already withholds a hidden
            // character, but a target id the client is still holding from a moment ago would sail
            // straight through — so the server checks rather than trusting the omission.
            if (!CanSee(caster, target))
            {
                SendSystemToEntity(caster, $"{def.Name} needs a target.");
                return;
            }
            // MOB-ONLY skills (the rogue's Lure) refuse a person out loud. The taunt handler would
            // ignore a player target anyway, but a skill that silently does nothing is a bug report.
            if (def.MobTargetOnly && target.Kind != EntityKind.Mob)
            {
                SendSystemToEntity(caster, $"{def.Name} only works on monsters.");
                return;
            }
            if (target.Kind == EntityKind.Player && !CanPvpHit(caster, target))
            {
                SendSystemToEntity(caster, PvpRefusalReason(caster, target));
                return;
            }
            targetId = tid;
        }
        // A HIDDEN ally is not a support target (BL-69): *"The healer targeting u from the party
        // window won't see u as healable target until u reveal yourself."* Failing this test falls
        // through to the self-cast below rather than erroring, which is precisely how an out-of-range
        // party member already behaves — "act as u r not nearby", in his words.
        //
        // `BL-59`: the party test that used to stand here is now CanSupport, which lets the cast reach
        // a clean non-party player as well and shuts the door on a flagged or red one. See that helper
        // — the permission is the TARGET's flag now, not the caster's.
        else if (IsAllyTargetable(def) &&
                 def.TargetMode != TargetMode.SelfOnly && def.Range > 0 &&
                 cmd.TargetId is Guid allyId &&
                 _world.Entities.TryGetValue(allyId, out var ally) &&
                 ally.Kind == EntityKind.Player && !ally.Dead && !ally.Hidden &&
                 CanSupport(caster, ally))
        {
            targetId = allyId; // ranged heal / cleanse / buff on a party member OR a clean stranger
        }
        else
        {
            // Self-cast. Crucially this is where a support skill lands when you have an ENEMY
            // targeted: it used to accept ANY player, so healing mid-duel healed the man you were
            // fighting. Falling through rather than refusing is deliberate and predates `BL-59` — an
            // unreachable support target behaves like an absent one.
            targetId = caster.Id;
        }

        // Restore Mana can't target yourself or another mana-restorer (no self/healer refunds).
        if ((def.Effect & SkillEffect.RestoreMp) != 0 &&
            _world.Entities.TryGetValue(targetId, out var mpTarget) &&
            mpTarget.HasSkill(SkillCatalog.RestoreMana))
        {
            SendSystemToEntity(caster, "Restore Mana can't be used on a mana-restorer.");
            return;
        }

        CancelCast(caster);

        // Casting a skill CANCELS the auto-attack chase (owner). Double-clicking a mob starts a walk
        // into melee (Engaged + CombatTargetId); without this, that chase RESUMES the moment the queued
        // skill finishes, so the character kept walking to the target after the cast — the cast only
        // paused the walk instead of ending it. The queued skill does its OWN approach (UpdateQueuedSkill
        // walks into cast range), so dropping the chase here loses nothing. A FIGHTER's offensive skill
        // re-engages afterwards via AfterOffensiveSkill (skill → melee combo preserved); a mage stays put.
        caster.Engaged = false;
        caster.CombatTargetId = null;
        caster.TargetX = null;
        caster.TargetY = null;

        caster.QueuedSkillId = def.Id;
        caster.QueuedTargetId = targetId;
    }

    /// <summary>Flip a toggle (stance) skill. If its self-buff is active, remove it
    /// (free); otherwise charge the activation MP and apply it indefinitely. The buff bar
    /// double-click (HandleRemoveBuff) also turns it off.</summary>
    private void HandleToggle(Entity caster, SkillDef def)
    {
        // A one-child WRAPPER holds its buff under the CHILD's family key, not its own; a GROUP holds
        // one buff under its own key, so the ordinary path below finds that. No shipped toggle is
        // either, but the wrapper case would otherwise be an un-turn-off-able stance.
        string key = def.ChildBuffs is { Length: 1 } toggleKid
                     && SkillCatalog.Get(toggleKid[0]) is SkillDef kid
                   ? (string.IsNullOrEmpty(kid.BuffKey) ? kid.Name : kid.BuffKey)
                   : string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        var existing = caster.Buffs.FirstOrDefault(b => b.Key == key);
        if (existing is not null)
        {
            caster.Buffs.Remove(existing);
            caster.RecomputeDerived();
            PushBuffs(caster);
            SendStats(caster);
            SendSystemToEntity(caster, $"{def.Name} deactivated.");
            return;
        }

        int level = caster.SkillLevelOf(def.Id);
        // A toggle has no cast, so there is nothing to split — it pays the whole effective price at
        // once, through the same helper the cast gate uses.
        int mp = EffectiveMpCost(caster, def, Math.Max(1, level));
        if (caster.Mp < mp)
        {
            SendSystemToEntity(caster, "Not enough MP.");
            return;
        }
        caster.Mp -= mp;
        ApplyBuff(caster, def, level, toggle: true);   // refreshes stats + buff bar
        SendSystemToEntity(caster, $"{def.Name} activated.");
    }

    private void HandleRespawn(RespawnCmd respawn)
    {
        if (!TryGetPlayer(respawn.ConnectionId, out var entity) || !entity.Dead)
            return;

        entity.Dead = false;
        entity.DiedWhileAway = false;   // the death has now been paid — clear the persisted stick-dead flag
        entity.LostExp = 0;             // town respawn = no exp restore (a resurrection would have restored it)
        entity.PendingResFromId = null; // drop any unanswered res offer — they chose the town instead
        entity.PendingResTicks = 0;
        entity.Hp = entity.MaxHp;
        entity.Mp = entity.MaxMp;
        entity.Buffs.Clear();
        // Respawn in the city that MANAGES the field you fell in, and only fall back to the nearest town
        // when no field does — open ground, the boss vale, a dungeon (owner: "each field has its parent
        // city; dying returns you to that city, and as a failsafe keep the nearest-city formula").
        //
        // Nearest-town alone was wrong in the one case that matters: cities are 13-15k apart and a city's
        // fields reach ~7k, so dying on the far edge of a field could put you in a DIFFERENT city — one
        // whose gatekeeper doesn't list the field you just died in, leaving you to walk back.
        var town = RegionMap.ManagingCity(entity.X, entity.Y)
                   ?? WorldMap.NearestSafeZone(entity.X, entity.Y);
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

        // (No archetype-uniqueness check: you may own several classes of the same 2nd class, as long as
        // they branch into different 3rd-class DISCIPLINES — that check lives on the 3rd-class change.)

        // NOTE: the class change no longer raises main stats. You keep the CON/ATK/WIT/AGI you
        // were born with; the level-40 stat-swap passives are the only way to move them.
        player.SecondClass = def.Id;
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
            // No level gate on equipping any more: you MAY equip above-grade gear — you just take the
            // GRADE PENALTY (its weapon ATK / armor DEF is scaled down until you reach the grade's level;
            // see GradePenalty + Entity.RecomputeDerived). Owner 2026-07-16.

            // Items being traded cannot be equipped mid-trade.
            if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
                trade.Offers(player, item.InstanceId))
                return;

            // JEWELS have DESIGNATED slots per sub-type: 2 rings, 2 earrings, 1 necklace. A full
            // pair no longer REFUSES the new jewel — it behaves like gloves and displaces one
            // (owner, playtest-15 §14). Which one: the WEAKEST by rarity, and when they tie, the
            // one in SLOT 1. Since the slots are ordered strongest-first, "the weakest, lowest
            // slot index" is simply the first entry whose strength equals the minimum.
            if (def.Slot == EquipSlot.Jewel)
            {
                var worn = player.Inventory
                    .Where(i => i.Equipped && i != item
                                && ItemCatalog.Get(i.DefId) is ItemDef j
                                && j.Slot == EquipSlot.Jewel && j.JewelType == def.JewelType)
                    .OrderBy(i => ItemCatalog.JewelSlotOrder(ItemCatalog.Get(i.DefId)!, i.Enchant))
                    .ToList();

                // `while`, not `if`: a character wearing more than the cap (older save, changed cap)
                // sheds down to it rather than staying over budget forever.
                int max = ItemCatalog.MaxOfJewelType(def.JewelType);
                while (worn.Count >= max && worn.Count > 0)
                {
                    long weakest = worn.Min(i => ItemCatalog.JewelStrength(ItemCatalog.Get(i.DefId)!, i.Enchant));
                    var loser = worn.First(i => ItemCatalog.JewelStrength(ItemCatalog.Get(i.DefId)!, i.Enchant) == weakest);
                    loser.Equipped = false;
                    worn.Remove(loser);
                    if (ItemCatalog.Get(loser.DefId) is ItemDef lost)
                        SendSystemToEntity(player, $"You take off your {lost.Name}.");
                }
            }

            // One item per slot: unequip the current one. Also enforce the
            // two-handed rule: a 2H weapon and a shield cannot coexist (a 2H
            // weapon occupies the offhand), so equipping one drops the other.
            bool equippingTwoHandWeapon = def.Slot == EquipSlot.Weapon && def.WeaponType.IsTwoHanded();
            bool equippingShield = def.Slot == EquipSlot.Shield;
            foreach (var other in player.Inventory)
            {
                if (!other.Equipped || ItemCatalog.Get(other.DefId) is not ItemDef otherDef)
                    continue;

                // Same slot — for armor, the body-part slot must also match (so a
                // helmet and a chest piece coexist, but two helmets don't). JEWELS are
                // exempt (multiple allowed up to the cap, checked above).
                if (otherDef.Slot == def.Slot && def.Slot != EquipSlot.Jewel &&
                    (def.Slot != EquipSlot.Armor || otherDef.ArmorSlot == def.ArmorSlot))
                    other.Equipped = false;
                else if (equippingTwoHandWeapon && otherDef.Slot == EquipSlot.Shield)
                    other.Equipped = false;                                   // 2H weapon drops shield
                else if (equippingShield && otherDef.Slot == EquipSlot.Weapon
                         && otherDef.WeaponType.IsTwoHanded())
                    other.Equipped = false;                                   // shield drops 2H weapon
            }

            item.Equipped = true;
            AdvanceActionQuests(player, QuestActions.EquipItem);   // the tutorial's equip beat (`58a`)
        }

        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
        SaveEntity(player);   // persist equip changes immediately (survive restarts)
    }

    private static readonly string[] PresetLabels = { "A", "B", "C" };

    /// <summary>Snapshot the currently-worn items into preset A/B/C (their instance ids).</summary>
    private void HandleSaveEquipPreset(SaveEquipPresetCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (cmd.Slot < 0 || cmd.Slot >= player.EquipPresets.Length) return;

        var preset = player.EquipPresets[cmd.Slot];
        preset.Clear();
        foreach (var it in player.Inventory)
            if (it.Equipped) preset.Add(it.InstanceId);

        SaveEntity(player);
        SendSystemToEntity(player, $"Saved your equipment as preset {PresetLabels[cmd.Slot]} ({preset.Count} item(s)).");
    }

    /// <summary>Re-equip a saved loadout: unequip everything, then equip each preset item still in the
    /// bag. Refused in combat (owner). Items sold/traded/destroyed are skipped and reported.</summary>
    private void HandleApplyEquipPreset(ApplyEquipPresetCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (cmd.Slot < 0 || cmd.Slot >= player.EquipPresets.Length) return;

        if (IsInCombat(player))
        {
            SendSystemToEntity(player, "You can't swap equipment in combat.");
            return;
        }
        var preset = player.EquipPresets[cmd.Slot];
        if (preset.Count == 0)
        {
            SendSystemToEntity(player, $"Preset {PresetLabels[cmd.Slot]} is empty — save it first.");
            return;
        }

        foreach (var it in player.Inventory) it.Equipped = false;   // strip current gear

        _world.ActiveTrades.TryGetValue(player.Id, out var trade);
        int missing = 0;
        foreach (var iid in preset)
        {
            var it = player.Inventory.FirstOrDefault(i => i.InstanceId == iid);
            // An item in a live trade offer can't be equipped; a missing one was sold/traded/destroyed.
            if (it is null || (trade is not null && trade.Offers(player, iid))) { missing++; continue; }
            it.Equipped = true;
        }

        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
        SaveEntity(player);
        SendSystemToEntity(player, missing == 0
            ? $"Equipped preset {PresetLabels[cmd.Slot]}."
            : $"Equipped preset {PresetLabels[cmd.Slot]} — {missing} item(s) were missing and skipped.");
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

        // GRADE BAND (0.49.0, D1). A scroll serves exactly one grade of gear, so this is the check that
        // makes the ladder mean anything — before it, the Common scroll found at level 10 was a
        // legitimate tool against endgame gear. Named on BOTH sides, because "wrong grade" alone leaves
        // the player guessing which of the two they got wrong.
        if (!EnchantRules.Accepts(scrollDef, targetDef))
        {
            var need = EnchantRules.GradeOf(targetDef);
            SendSystemToEntity(player,
                $"{scrollDef.Name} only works on {EnchantRules.GradeName(scrollDef.ScrollGrade)} grade; "
                + $"{targetDef.Name} is {EnchantRules.GradeName(need)} grade"
                + (need == EnchantGrade.None
                    ? " — F grade cannot be enchanted at all." : "."));
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
            case EnchantResult.Failed:
                // The SAFE scroll's failure. Say what was PRESERVED, not "nothing happened" — the
                // scroll is gone and that is the whole price, so the player has to be able to see
                // what they bought with it.
                outcome = $"Enchant failed — {targetDef.Name} stays at +{target.Enchant}.";
                break;
            default:
                outcome = "Nothing happened.";
                break;
        }

        if (target.Equipped && !destroyed)
            player.RecomputeDerived();

        // Send the fresh inventory FIRST, so the client's _inventory is up to date before
        // the Enchant result re-renders the enchant popup — otherwise the popup and the
        // inventory list desync by one enchant step (the ±1 mismatch).
        SendInventory(player);
        SendTo(player, "Enchant", new EnchantResultDto(
            targetDef.Name, destroyed ? 0 : target.Enchant, outcome, destroyed));
        SendSystemToEntity(player, outcome);
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

        // ONE attribute per item (0.45.0). AttributeSystem owns every rule — grade band, whether
        // the scroll can create an attribute or only re-roll one, and the value window — so a
        // refusal here is always a message the player can act on, and the scroll is NOT consumed.
        var current = target.Attributes.Count > 0 ? target.Attributes[0] : null;
        var roll = AttributeSystem.ApplyScroll(targetDef, current, scrollDef.AttrScroll, _rng);
        if (!roll.Ok || roll.Attribute is null)
        {
            SendSystemToEntity(player, roll.Message);
            return;
        }

        target.Attributes = new List<ItemAttribute> { roll.Attribute };

        ConsumeOne(player, scroll);
        if (target.Equipped)
            player.RecomputeDerived();

        string outcome = roll.Message;
        // Inventory first (see HandleEnchant) so the reroll popup re-renders from fresh data.
        SendInventory(player);
        SendTo(player, "Reroll", new RerollResultDto(targetDef.Name, outcome));
        SendSystemToEntity(player, outcome);
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

        // Runes are DELETE-PROTECTED (owner: can't fat-finger it off, like a buff). To switch one off,
        // move it to the warehouse; it expires on its own either way.
        if (ItemCatalog.Get(item.DefId) is { IsRune: true })
        {
            SendSystemToEntity(player, "A rune can't be deleted — move it to the warehouse to switch it off.");
            return;
        }

        // Block destroying items that are currently in a trade offer.
        if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
            trade.Offers(player, item.InstanceId))
            return;

        bool wasEquipped = item.Equipped;
        int destroyed;   // how many units actually went in the bin — what an undo has to put back

        if (cmd.Quantity > 0 && cmd.Quantity < item.Quantity)
        {
            destroyed = cmd.Quantity;
            item.Quantity -= cmd.Quantity;   // bin numpad: drop exactly N of the stack
        }
        else if (item.Quantity > 1 && !cmd.All && cmd.Quantity == 0)
        {
            destroyed = 1;
            item.Quantity--;                 // drop ONE from the stack
        }
        else
        {
            destroyed = item.Quantity;
            player.Inventory.Remove(item);   // whole stack (or a single item)
        }

        // C18: remember it so the bin can be UNDONE. Free, because nothing was paid for it. This is the
        // half of the buy-back design he actually needed — the accident happens in the field, with a
        // [Del] button, not at a vendor.
        player.Restorable.Add(new BuyBackEntry
        {
            DefId = item.DefId, Quantity = destroyed, Enchant = item.Enchant,
            Attributes = new List<ItemAttribute>(item.Attributes),
            UnitPrice = 0,
        });
        while (player.Restorable.Count > GameConstants.RestoreSlots) player.Restorable.RemoveAt(0);

        if (wasEquipped)
            player.RecomputeDerived();

        SendInventory(player);
        SendRestorable(player);
        if (wasEquipped)
            SendStats(player);
    }

    private void SendRestorable(Entity player) =>
        SendTo(player, "Restore", new RestoreUpdate(
            player.Restorable.Select((e, i) => new BuyBackEntryDto(
                i, e.DefId, ItemCatalog.Get(e.DefId)?.Name ?? e.DefId, e.Quantity, e.Enchant, 0))
                .ToArray()));

    /// <summary>Undo a bin-delete. FREE and with no vendor in sight — see <see cref="RestoreItemCmd"/>.
    /// The entry is consumed either way it succeeds, so an undo can't be used twice.</summary>
    private void HandleRestoreItem(RestoreItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (cmd.Index < 0 || cmd.Index >= player.Restorable.Count) return;

        var entry = player.Restorable[cmd.Index];
        if (ItemCatalog.Get(entry.DefId) is not ItemDef def)
        {
            // The def left the catalog (a rename) — drop the dead row rather than resurrect junk.
            player.Restorable.RemoveAt(cmd.Index);
            SendRestorable(player);
            return;
        }

        if (player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }

        player.Inventory.Add(new InventoryItem
        {
            DefId = entry.DefId, Quantity = entry.Quantity, Enchant = entry.Enchant,
            Attributes = new List<ItemAttribute>(entry.Attributes),
        });
        player.Restorable.RemoveAt(cmd.Index);

        SendInventory(player);
        SendRestorable(player);
        SendSystemToEntity(player,
            $"Restored {def.Name}{(entry.Quantity > 1 ? $" x{entry.Quantity}" : "")}.");
        SaveEntity(player);
    }

    private void SendWarehouse(Entity player) =>
        SendTo(player, "Warehouse", new WarehouseUpdate(
            player.Warehouse.Select(i => i.ToDto()).ToArray()));

    /// <summary>The private warehouse is reachable only in a town (safe zone), like IG's warehouse keeper —
    /// so you can't stash mid-fight. Sends the reason and returns false when out of town.</summary>
    private bool WarehouseReachable(Entity player)
    {
        if (GameConstants.InSafeZone(player.X, player.Y)) return true;
        SendSystemToEntity(player, "You can only reach your warehouse in a town.");
        return false;
    }

    private void HandleOpenWarehouse(OpenWarehouseCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;
        SendWarehouse(player);
    }

    private void HandleWarehouseDeposit(WarehouseDepositCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        // QUEST ITEMS NEVER LEAVE THE BAG (playtest-17 B4, and §39e is the bug it closes): a token
        // parked in here stopped counting toward its quest step, yet Complete still took it — so the
        // step silently stalled and the only cure was remembering you had banked it. The private bank
        // was the last disposal path that still accepted one; sell, trade, the bin and the account
        // bank all already refuse.
        if (ItemCatalog.Get(item.DefId) is ItemDef questDef && ItemCatalog.IsQuestItem(questDef))
        {
            SendSystemToEntity(player, $"{questDef.Name} belongs in your quest bag — it can't be stored.");
            return;
        }

        // ...and since `58d`, an INSTANCE may refuse the private bank on its own account. The private
        // warehouse had no such gate at all — it takes anything that is not a quest item — which is
        // exactly the hole the Rune of Sinners would have escaped through: *"Keeper cannot accept this
        // item ... as its bound to your soul for the time it has left."*
        // A SoulBound DEF (the Rune of Sinners) refuses it too, so the punishment does not depend on
        // whoever handed it out remembering the right flags.
        if (ItemCatalog.Get(item.DefId) is ItemDef boundDef && !item.StorablePrivate(boundDef))
        {
            SendSystemToEntity(player, $"The keeper will not accept {item.Name(boundDef)}.");
            return;
        }

        // Can't stash an item that's in a live trade offer.
        if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
            trade.Offers(player, item.InstanceId))
            return;

        // A stackable merges into the row that's already in the bank instead of taking a new one —
        // otherwise depositing 5 gems and later 6 more left TWO rows of the same material, and a
        // full warehouse refused a deposit it had room for.
        var def = ItemCatalog.Get(item.DefId);
        var mergeInto = def is { IsStackable: true }
            ? player.Warehouse.FirstOrDefault(i => i.DefId == item.DefId)
            : null;

        if (mergeInto is null && player.Warehouse.Count >= GameConstants.WarehouseSize)
        {
            SendSystemToEntity(player, "Warehouse full.");
            return;
        }

        item.Equipped = false;                 // nothing is worn from the bank
        player.Inventory.Remove(item);
        if (mergeInto is not null) mergeInto.Quantity += item.Quantity;
        else player.Warehouse.Add(item);

        ReconcileTimedItems(player);            // a deposited rune stops applying its buff (no longer in the bag)
        player.RecomputeDerived();             // reflect the un-equip
        SendInventory(player);
        SendWarehouse(player);
        SendStats(player);
        SaveEntity(player);
    }

    private void HandleWarehouseWithdraw(WarehouseWithdrawCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;

        var item = player.Warehouse.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        // Same merge rule coming back out — a withdrawn stack joins the bag row it belongs to.
        var def = ItemCatalog.Get(item.DefId);
        var mergeInto = def is { IsStackable: true }
            ? player.Inventory.FirstOrDefault(i => i.DefId == item.DefId)
            : null;

        if (mergeInto is null && player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }

        player.Warehouse.Remove(item);
        if (mergeInto is not null) mergeInto.Quantity += item.Quantity;
        else player.Inventory.Add(item);

        ReconcileTimedItems(player);            // a withdrawn rune re-applies its buff (back in the bag)
        player.RecomputeDerived();
        SendInventory(player);
        SendWarehouse(player);
        SendStats(player);
        SaveEntity(player);
    }

    // ----- Account warehouse ----------------------------------------------------------------
    //
    // The private warehouse is a bigger bag; this one is a DOOR BETWEEN YOUR CHARACTERS, which is a
    // different thing and priced differently. Two rules carry it:
    //   * TRADABLE ONLY. An item that cannot be traded is bound to the character that earned it —
    //     quest items, bound gear. Letting the account bank move them would make it a laundering
    //     route around the tradable flag rather than a convenience.
    //   * 10 000 GOLD PER SLOT. Charged when the deposit has to OPEN a slot; merging into a stack
    //     already in there is free. The fee buys the slot, not the deposit, so the second thousand
    //     of a material costs nothing — and a mule account is no longer free storage.
    // Withdrawing is always free: charging to get your own things back is a trap, not a cost.

    /// <summary>The account bank, creating an empty one if this account has never opened it. Reached
    /// only for a logged-in character, so the account id is always real by here.</summary>
    private List<InventoryItem> AccountBankOf(Entity player)
    {
        if (!_world.AccountWarehouses.TryGetValue(player.AccountId, out var bank))
            _world.AccountWarehouses[player.AccountId] = bank = new List<InventoryItem>();
        return bank;
    }

    private void SendAccountWarehouse(Entity player) =>
        SendTo(player, "AccountWarehouse", new AccountWarehouseUpdate(
            AccountBankOf(player).Select(i => i.ToDto()).ToArray()));

    /// <summary>Persist the account bank AND push it to every character of that account who is in the
    /// world — the list is shared, so a second character standing at a keeper must not go on showing
    /// the contents from before their own character moved something.</summary>
    private void SaveAndSyncAccountBank(Entity player)
    {
        int accountId = player.AccountId;
        var snapshot = PersistenceService.AccountItemSnapshot.From(AccountBankOf(player));
        _ = _db.SaveAccountWarehouseAsync(accountId, snapshot);

        foreach (var e in _world.Entities.Values)
            if (e.Kind == EntityKind.Player && e.AccountId == accountId &&
                _world.EntityToConnection.ContainsKey(e.Id))
                SendAccountWarehouse(e);
    }

    private void HandleOpenAccountWarehouse(OpenAccountWarehouseCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;
        SendAccountWarehouse(player);
    }

    private void HandleAccountWarehouseDeposit(AccountWarehouseDepositCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;

        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        // Can't stash an item that's in a live trade offer.
        if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
            trade.Offers(player, item.InstanceId))
            return;

        var def = ItemCatalog.Get(item.DefId);
        if (def is null) return;
        // ⚠ Read through the INSTANCE (`58d`), not the def: a given item carries its own storage rules,
        // and `CanStoreAccount` falls back to the standing tradable-only rule when it has no opinion.
        // That is what lets the Rune of Sinners be refused by a keeper the def alone would have allowed.
        if (!item.StorableAccount(def) || ItemCatalog.IsQuestItem(def))
        {
            SendSystemToEntity(player, $"{item.Name(def)} is bound to this character — it can't go in the account warehouse.");
            return;
        }

        var bank = AccountBankOf(player);
        var mergeInto = def.IsStackable ? bank.FirstOrDefault(i => i.DefId == item.DefId) : null;

        if (mergeInto is null)
        {
            if (bank.Count >= GameConstants.AccountWarehouseSize)
            {
                SendSystemToEntity(player, "Account warehouse full.");
                return;
            }
            if (player.Gold < GameConstants.AccountWarehouseSlotFee)
            {
                SendSystemToEntity(player,
                    $"A new account-warehouse slot costs {GameConstants.AccountWarehouseSlotFee:N0} {GameConstants.CurrencyName}.");
                return;
            }
            player.Gold -= GameConstants.AccountWarehouseSlotFee;
            SendSystemToEntity(player,
                $"Paid {GameConstants.AccountWarehouseSlotFee:N0} {GameConstants.CurrencyName} for an account-warehouse slot.");
        }

        item.Equipped = false;                 // nothing is worn from the bank
        player.Inventory.Remove(item);
        item.PersistentInstanceId = null;      // it leaves this character's item rows for the account's
        if (mergeInto is not null) mergeInto.Quantity += item.Quantity;
        else bank.Add(item);

        ReconcileTimedItems(player);            // a deposited rune stops applying its buff
        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
        SendGold(player);
        SaveEntity(player);
        SaveAndSyncAccountBank(player);
    }

    private void HandleAccountWarehouseWithdraw(AccountWarehouseWithdrawCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!WarehouseReachable(player)) return;

        var bank = AccountBankOf(player);
        var item = bank.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        var def = ItemCatalog.Get(item.DefId);
        var mergeInto = def is { IsStackable: true }
            ? player.Inventory.FirstOrDefault(i => i.DefId == item.DefId)
            : null;

        if (mergeInto is null && player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }

        bank.Remove(item);
        item.PersistentInstanceId = null;      // it belongs to this character's item rows now
        if (mergeInto is not null) mergeInto.Quantity += item.Quantity;
        else player.Inventory.Add(item);

        ReconcileTimedItems(player);            // a withdrawn rune re-applies its buff (back in the bag)
        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
        SaveEntity(player);
        SaveAndSyncAccountBank(player);
    }

#pragma warning disable CS1998
    private void HandleDebugTeleport(DebugTeleportCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        player.X = Math.Clamp(cmd.X, GameConstants.WorldMinX, GameConstants.ZoneWidth);
        player.Y = Math.Clamp(cmd.Y, GameConstants.WorldMinY, GameConstants.ZoneHeight);
        player.TargetX = null;
        player.TargetY = null;
        _world.Grid.UpdatePosition(player);
        SendSystemToEntity(player, $"Teleported to ({(int)player.X}, {(int)player.Y}).");
    }

    private void HandleDebugGive(DebugGiveCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (ItemCatalog.Get(cmd.DefId) is not ItemDef def)
            return;

        // 🔴 ONE COMMAND, ONE PUSH (66n). The quantity used to live in the HUB, which enqueued a
        // separate command per unit — so "every material x500" was 12 500 commands, each granting one
        // material and then serialising the entire inventory: *"i see each sinlge item increasing 1 by
        // 1 500 times and going to the next ... now the game is Stalled (had to restart)"*, and it
        // finished mid-grant, which is why one material sat at ~6800 while the rest were short.
        // A stackable is now a SINGLE AddItem; only genuinely non-stackable gear still loops, and that
        // is bounded by the bag's slot count rather than by the number he typed.
        int want = Math.Clamp(cmd.Quantity, 1, 10_000);
        int added = 0;
        if (def.IsStackable)
        {
            if (AddItem(player, def.Id, want)) added = want;
        }
        else
        {
            for (int i = 0; i < want; i++)
            {
                if (!AddItem(player, def.Id)) break;
                added++;
            }
        }
        if (added == 0)
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }
        if (added < want)
            SendSystemToEntity(player, $"Inventory full — {added} of {want} {def.Name} added.");
        // No chat line on success: the debug menu's item buttons are the most-pressed thing in it, and
        // taking ten potions filled the log with ten identical rows (owner). The inventory refresh below
        // IS the feedback. The rarely-used debug actions — teleport coordinates, karma, class change —
        // keep theirs, because those genuinely tell you something the UI does not.
        SendInventory(player);
    }

    /// <summary>DEBUG (`/enchant &lt;value&gt;`, D2): set one item's enchant outright.
    ///
    /// This bypasses EVERY rule the scroll path enforces — the grade band, the scroll, the success
    /// roll and <see cref="EnchantRules.MaxEnchant"/> — on purpose: its job is to reach states no
    /// scroll can produce so the STAT side of enchanting can be tested in one step instead of forty
    /// rolls. The only bound is an anti-overflow ceiling, which sits far above the owner's 999999.
    /// </summary>
    private void HandleAdminEnchant(AdminEnchantCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || ItemCatalog.Get(item.DefId) is not ItemDef def)
        {
            SendSystemToEntity(player, "No such item.");
            return;
        }
        if (!ItemCatalog.IsEquippable(def))
        {
            SendSystemToEntity(player, $"{def.Name} cannot be enchanted.");
            return;
        }
        // The enchant deltas are FLAT PER LEVEL (up to 30/level for armour HP), so a few million keeps
        // every derived stat inside int range while still being absurd enough for any test.
        item.Enchant = Math.Clamp(cmd.Value, 0, 1_000_000);
        if (item.Equipped)
        {
            player.RecomputeDerived();
            SendStats(player);
        }
        SendInventory(player);
        SaveEntity(player);
        SendSystemToEntity(player, $"[DEBUG] {def.Name} set to +{item.Enchant}.");
    }

    /// <summary>DEBUG: strip an attribute (or all) off the equipped weapon, so you can test with
    /// the base weapon or a single chosen attribute instead of the full rolled set.</summary>
    private void HandleDebugCancelAttr(DebugCancelAttrCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        var weapon = player.Inventory.FirstOrDefault(it => it.Equipped
            && ItemCatalog.Get(it.DefId)?.Slot == EquipSlot.Weapon);
        if (weapon is null || weapon.Attributes.Count == 0)
        {
            SendSystemToEntity(player, "[DEBUG] No equipped weapon with attributes.");
            return;
        }
        if (cmd.Index < 0)
        {
            weapon.Attributes.Clear();
            SendSystemToEntity(player, "[DEBUG] Cleared all weapon attributes.");
        }
        else if (cmd.Index < weapon.Attributes.Count)
        {
            var removed = weapon.Attributes[cmd.Index];
            weapon.Attributes.RemoveAt(cmd.Index);
            SendSystemToEntity(player, $"[DEBUG] Cancelled {AttributeSystem.DisplayName(removed.Type)}.");
        }
        else
        {
            SendSystemToEntity(player, $"[DEBUG] No attribute at index {cmd.Index} (have {weapon.Attributes.Count}).");
            return;
        }
        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
    }

    /// <summary>Craft a recipe: check profession + master + crafting rung + character level + inputs,
    /// consume the inputs, roll the outcome, and award crafting exp (`BL-05`).
    ///
    /// <para>Two kinds of roll live here. A material or consumable recipe rolls its own
    /// <see cref="Recipe.SuccessChance"/> — succeed or lose the mats. A GEAR recipe rolls the owner's
    /// three-way table instead (<see cref="Crafting.GearCraftOdds"/>): Mythic, Legendary, or a failure
    /// that eats the materials. Only Legendary and Mythic gear is craftable at all, which is why there
    /// is no third success rung to fall to.</para></summary>
    private void HandleCraft(CraftCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (RecipeCatalog.Get(cmd.RecipeId) is not Recipe recipe)
        {
            SendSystemToEntity(player, "Unknown recipe.");
            return;
        }
        if (recipe.Profession != Profession.None && player.Profession != recipe.Profession)
        {
            SendSystemToEntity(player, $"Requires the {recipe.Profession} profession.");
            return;
        }
        // ⚠ THE MASTER IS THE WORKSHOP (owner: *"better at NPC — and craft happens with their respected
        // masters"*). The window still opens anywhere, in browse mode, because the have/need colouring
        // is what tells you WHAT TO FARM and that is a decision made in the field — but nothing is made
        // away from the man who taught you.
        if (MasterNpcNear(player) is null)
        {
            SendSystemToEntity(player,
                $"You must be with your master to craft. ({ProfessionMasterWhere(player.Profession)})");
            return;
        }

        int rung = player.CraftLevel;
        if (rung <= 0)
        {
            SendSystemToEntity(player, "You have no crafting profession.");
            return;
        }
        // The crafting-level gate, which is a SECOND gate and not the character one below: everything at
        // or below your rung, plus exactly one rung above (*"L5 should not be available"* to an L3).
        if (!Crafting.CanCraftAt(recipe.CraftLevel, rung))
        {
            SendSystemToEntity(player,
                $"That is a level {recipe.CraftLevel} recipe — you are level {rung}. "
                + (recipe.CraftLevel == rung + 2
                    ? "You can only attempt one level above your own."
                    : "Craft your way up to it."));
            return;
        }
        // DropOnly recipes (A-grade sets) must be learned from a dropped recipe BOOK; auto-known
        // recipes just need the level gate.
        if (recipe.DropOnly)
        {
            if (!player.KnownRecipes.Contains(recipe.Id))
            {
                SendSystemToEntity(player, "You haven't learned that recipe — unlock it with its blueprint first.");
                return;
            }
        }
        else if (player.Level < recipe.LearnLevel)
        {
            SendSystemToEntity(player, $"You must be level {recipe.LearnLevel} to craft this.");
            return;
        }

        // Endgame (DropOnly) recipes ALSO consume ONE BLUEPRINT per craft — the very item that unlocked the
        // recipe (owner's design: 1 blueprint to learn + 1 every craft, so the first craft costs 2). The
        // blueprint is the recipe's own book item, which always exists for a DropOnly (A-grade) recipe.
        string? blueprintId = recipe.DropOnly ? ItemCatalog.RecipeBookId(recipe.Id) : null;
        if (blueprintId != null && ItemCatalog.Get(blueprintId) is null) blueprintId = null;

        foreach (var inp in recipe.Inputs)
            if (CountItem(player, inp.ItemId) < inp.Qty)
            {
                SendSystemToEntity(player, "You don't have the required materials.");
                return;
            }
        if (blueprintId != null && CountItem(player, blueprintId) < 1)
        {
            SendSystemToEntity(player, "You need a blueprint to craft this — one is consumed each time.");
            return;
        }
        foreach (var inp in recipe.Inputs)
            ConsumeItem(player, inp.ItemId, inp.Qty);

        // ---- THE ROLL. Gear takes the owner's three-way table; everything else its own SuccessChance.
        var outDef = ItemCatalog.Get(recipe.OutputId);
        bool isGear = outDef is not null && Crafting.IsGearSlot(outDef.Slot);
        string? madeId = null;
        if (isGear)
        {
            var odds = Crafting.GearCraftOdds(recipe.CraftLevel);
            double roll = _rng.NextDouble();
            if (roll < odds.Mythic) madeId = recipe.OutputId;                     // the authored piece
            else if (roll < odds.Mythic + odds.Legendary)
                madeId = ItemCatalog.QualityId(recipe.OutputId, ItemRarity.Legendary);
            // else: fail — the mats are gone, which is the crafting economy's first real sink.
        }
        else if (_rng.NextDouble() < recipe.SuccessChance)
        {
            madeId = recipe.OutputId;
        }

        // The BLUEPRINT is spent on a SUCCESS only. The owner's fail rule names the MATERIALS —
        // *"a fail consumes the materials and produces nothing"* — and a blueprint is not one: it is the
        // recipe itself, dropped at 0.1% off an A-grade boss. Burning it on a 75% failure would make an
        // S craft cost four boss blueprints per item and put the top rung out of reach of the drop rate
        // that feeds it.
        if (madeId != null && blueprintId != null)
            ConsumeItem(player, blueprintId, 1);

        if (madeId != null)
        {
            AddItem(player, madeId, recipe.OutputQty);
            string madeName = ItemCatalog.Get(madeId)?.Name ?? madeId;
            string quality = isGear ? $" ({ItemCatalog.Get(madeId)?.Rarity})" : "";
            SendSystemToEntity(player, $"Crafted {madeName}{quality}"
                + (recipe.OutputQty > 1 ? $" x{recipe.OutputQty}." : "."));
        }
        else
        {
            SendSystemToEntity(player, "Craft failed — the materials were lost.");
        }

        AwardCraftExp(player, recipe.CraftLevel);
        SendInventory(player);
    }

    /// <summary>Crafting exp for one ATTEMPT, capped at the band the character has earned (`BL-05`).
    ///
    /// 🔑 **Paid on a FAILURE too.** His spec counts *"crafts"* (*"x10 crafts per difference of same
    /// level"*) and the materials are spent either way, so a failed attempt is work done — the levels
    /// are practice, which is exactly why quitting a profession loses them. Paying only on success would
    /// also make the A and S rungs level 2× and 4× slower than the numbers he wrote, purely as a side
    /// effect of a table he authored for a different purpose. One line to flip if he disagrees.
    ///
    /// ⚠ The cap NEVER LOWERS stored exp. <see cref="Crafting.CapExp"/> is a clamp, and a clamp applied
    /// to an already-high total would delete progress the moment a character's band appeared to shrink.
    /// <see cref="Entity.CraftBandCap"/> reads the best subclass precisely so that cannot happen, and
    /// this is the second guard on the same silent, hours-destroying failure.</summary>
    private void AwardCraftExp(Entity player, int recipeLevel)
    {
        int gain = Crafting.CraftExp(recipeLevel, player.CraftLevel);
        if (gain <= 0) return;                       // -2 rungs and below pay nothing, by his rule

        int before = player.CraftLevel;
        int capped = Crafting.CapExpToBand(player.CraftExp + gain, player.CraftBandCap);
        if (capped <= player.CraftExp) return;       // frozen at the band's mark — nothing to say
        player.CraftExp = capped;

        int after = player.CraftLevel;
        if (after > before)
        {
            SendSystemToEntity(player, $"Your {player.Profession} skill reached level {after}.");
            SaveEntity(player);                      // a level is worth not trusting to the 60s autosave
        }
        SendCrafting(player);
    }

    /// <summary>Grant a profession and start it at L1, 0% — *"After quest u become l1"*. Shared by the
    /// joining-quest completion and the re-join path (a master who has already taught you takes you
    /// straight back), so both can never disagree about what joining does.</summary>
    private void GrantProfession(Entity player, Profession prof)
    {
        player.Profession = prof;
        player.CraftExp = 0;
        SendSystemToEntity(player,
            $"You are now a {prof} — crafting level 1. Your master's workshop is where you make things.");
        SendCrafting(player);
        SaveEntity(player);
    }

    /// <summary>Join a master's profession WITHOUT re-doing his quest (`BL-05`).
    ///
    /// 🔑 His ruling, 2026-08-12: *"Skip the quest if it's once done, but still lose levels if switching.
    /// Like a mix from both."* So this path is open only to someone who has already completed THIS
    /// master's joining quest, and it still starts them at L1, 0% — the quest is knowledge and cannot be
    /// un-known; the levels are practice and are lost by walking away. It needs no new storage: joining
    /// quests are ordinary quests and <c>CompletedQuests</c> already persists.</summary>
    private void HandleJoinProfession(JoinProfessionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (CraftMasterAt(player, cmd.NpcEntityId) is not Profession prof)
            return;
        if (player.Profession == prof)
        {
            SendSystemToEntity(player, $"You are already a {prof}.");
            return;
        }
        if (player.Level < QuestCatalog.ProfessionJoinLevel)
        {
            SendSystemToEntity(player,
                $"A master takes no apprentice below level {QuestCatalog.ProfessionJoinLevel}.");
            return;
        }
        // Never done his quest → he has not made his pitch yet, and the pitch is the point.
        if (QuestCatalog.JoiningQuestFor(prof) is not string qid || !player.CompletedQuests.Contains(qid))
        {
            SendSystemToEntity(player, "Take his apprenticeship quest first.");
            return;
        }
        // ⚠ Switching AWAY from a profession you still hold destroys its levels. The client confirms
        // with the number spelled out; this is the server refusing to do it silently.
        if (player.Profession != Profession.None)
        {
            SendSystemToEntity(player,
                $"Quit your {player.Profession} at his own master first — his levels do not carry over.");
            return;
        }
        GrantProfession(player, prof);
    }

    /// <summary>Quit the character's profession at his OWN master, losing every crafting level
    /// (`BL-05`) — *"if some1 desides that he dont like the proffesion can go to his master and quits
    /// (losing all his levels) → then he can go to the other master and start the quests and at lvl
    /// 0."*
    ///
    /// ⚠ This is the one destructive action in the feature. The confirmation with the loss spelled out
    /// in numbers is the CLIENT's job (same as the Mindwriter and the stat basket); the server's job is
    /// to refuse it at the wrong NPC, so a mis-sent command can never cost someone L5.</summary>
    private void HandleQuitProfession(QuitProfessionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (player.Profession == Profession.None)
        {
            SendSystemToEntity(player, "You have no profession to quit.");
            return;
        }
        if (CraftMasterAt(player, cmd.NpcEntityId) != player.Profession)
        {
            SendSystemToEntity(player, "Only your own master can release you.");
            return;
        }
        var was = player.Profession;
        int wasLevel = player.CraftLevel;
        player.Profession = Profession.None;
        player.CraftExp = 0;
        SendSystemToEntity(player,
            $"You are no longer a {was}. Crafting level {wasLevel} is gone — a new master starts you at 1.");
        SendCrafting(player);
        SaveEntity(player);
    }

    /// <summary>The nearby NPC entity with this npc id (any town's copy of the same service), or null.
    /// Uses the same interaction range every other NPC service does.</summary>
    /// <summary>The profession taught by the craft master this player is standing at, or null with the
    /// refusal already sent. Same shape as every other NPC service: resolve the LIVE entity, check its
    /// role, check the range — which together mean a join or a quit can only be aimed at a master who
    /// really is spawned and really is in front of you.</summary>
    private Profession? CraftMasterAt(Entity player, Guid npcEntityId)
    {
        if (!_world.Entities.TryGetValue(npcEntityId, out var npc)
            || npc.Kind != EntityKind.Npc || npc.NpcRole != NpcRole.CraftMaster
            || WorldMap.CraftMasterProfession(npc.NpcId ?? "") is var taught
                && taught == Profession.None)
        {
            SendSystemToEntity(player, "That is not a crafting master.");
            return null;
        }
        float dx = npc.X - player.X, dy = npc.Y - player.Y;
        if (dx * dx + dy * dy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{npc.Name} is too far away.");
            return null;
        }
        return WorldMap.CraftMasterProfession(npc.NpcId ?? "");
    }

    /// <summary>The npc id of the craft master this player is standing at, if it is THEIR OWN master —
    /// the gate on actually making anything (owner: *"craft happens with their respected masters"*).
    /// Null everywhere else, which is what puts the crafting window into browse mode.</summary>
    /// ⚠ Reads the STATIC WorldMap table, not the live entity dictionary. NPCs never move, and this is
    /// called once a second for every player who holds a profession — scanning every entity in the world
    /// for that would be the most expensive thing in the tick. Five rows, five distance checks.
    private static string? MasterNpcNear(Entity player)
    {
        if (player.Profession == Profession.None) return null;
        foreach (var n in WorldMap.Npcs)
        {
            if (n.Role != NpcRole.CraftMaster) continue;
            if (WorldMap.CraftMasterProfession(n.Id) != player.Profession) continue;
            float dx = n.X - player.X, dy = n.Y - player.Y;
            if (dx * dx + dy * dy <= GameConstants.TalkRange * GameConstants.TalkRange)
                return n.Id;
        }
        return null;
    }

    /// <summary>Once a second: has this crafter walked into (or out of) his master's range? If so push
    /// the crafting state so the window's Craft buttons go live or dead on their own.
    ///
    /// 🔑 A LATCH, not a poll of the client. It pushes only on the EDGE, so standing in the workshop
    /// costs one message rather than one a second, and the whole rest of the world costs nothing but the
    /// five distance checks in <see cref="MasterNpcNear"/>. The alternative — leaving the client to guess
    /// — was rejected because "browse vs craft" is the SERVER's rule (owner: *"craft happens with their
    /// respected masters"*) and a client that guesses it wrong shows a live button that refuses.</summary>
    private void TickCraftMasterProximity(Entity player)
    {
        if (player.Profession == Profession.None) return;
        bool atMaster = MasterNpcNear(player) is not null;
        if (atMaster == player.AtCraftMaster) return;
        player.AtCraftMaster = atMaster;
        SendCrafting(player);
    }

    /// <summary>Where to go to craft, for the "you must be with your master" refusal.</summary>
    private static string ProfessionMasterWhere(Profession prof) => prof switch
    {
        Profession.WeaponSmith  => "the Master Smith, in any town",
        Profession.ArmorSmith   => "the Master Armorer, in any town",
        Profession.Jeweler      => "the Master Jeweler, in any town",
        Profession.PotionMaster => "the Master Apothecary, in any town",
        Profession.ScrollScribe => "the Master Scribe, in any town",
        _ => "a crafting master",
    };


    private void HandleDebugSetProfession(DebugSetProfessionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.Profession = (Profession)Math.Clamp(cmd.Profession, 0, (int)Profession.ScrollScribe);
        if (player.Profession == Profession.None) player.CraftExp = 0;
        SendSystemToEntity(player, $"[DEBUG] Crafting profession set to {player.Profession}.");
        SendCrafting(player);
    }

    /// <summary>DEBUG: jump to a crafting level by setting the exp to that level's mark. The BAND still
    /// clamps it — a level-20 character set to L6 still reads L2 — because the freeze is the half of the
    /// ladder most worth being able to test.</summary>
    private void HandleDebugSetCraftLevel(DebugSetCraftLevelCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        int lvl = Math.Clamp(cmd.Level, 1, Crafting.MaxCraftLevel);
        player.CraftExp = Crafting.CraftLevelMarks[lvl - 1];
        SendSystemToEntity(player,
            $"[DEBUG] Crafting exp set to level {lvl} — in force: {player.CraftLevel} (band cap {player.CraftBandCap}).");
        SendCrafting(player);
    }

    /// <summary>Debug: become a 2nd CLASS on the spot, skipping the quest and level gates the NPC path
    /// enforces. This is the "compare two builds in the same gear" lever the owner actually wanted from
    /// the debug panel's class list — which was wired to the CRAFTING profession instead, so every class
    /// id above 4 was clamped to ScrollScribe (playtest-13).
    ///
    /// Race and base class are still checked: a Human Fighter cannot debug into an Elf Mage's class, and
    /// letting it would produce a character whose skill tables do not match its own race.</summary>
    private void HandleDebugSecondClass(DebugSecondClassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        var def = ClassCatalog.Get(cmd.ClassId);
        if (def is null || def.Race != player.Race || def.Base != player.BaseClass)
        {
            SendSystemToEntity(player,
                "[DEBUG] That class belongs to another race or base class — use Reset first.");
            return;
        }

        player.SecondClass = def.Id;
        player.ThirdClass = 0;   // the old discipline belonged to the old 2nd class
        player.FourthClass = 0;  // …and the ascension belonged to the old discipline
        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendStats(player);
        SendLearned(player);
        SendSubclasses(player);   // the class label the client shows comes from here
        SaveEntity(player);       // same as the 3rd-class path — a class change shouldn't wait for autosave
        SendSystemToEntity(player, $"[DEBUG] You are now a {def.Name}.");
    }

    // ===== SUBCLASSES ==========================================================================
    //
    // One character owns SEVERAL classes and plays one at a time. Class-level state (level, XP, skill
    // points, 2nd/3rd class, core stats, learned skills, skill bar) lives on the Subclass; everything
    // character-level (inventory, gold, karma, quests, auto-hunt, position) is shared. See Subclass.cs.
    //
    // Today these are DEBUG-only entry points: no cap on how many, no swap delay, no safe-zone
    // requirement. Those rules belong to the player-facing system (owner: cap 3-4, safe zone, 5-min
    // delay) and are deliberately not baked into the swap itself — HandleSwitchSubclass does the state
    // work, and the rules will gate the COMMAND, not the mechanism.

    /// <summary>Add a SUBCLASS chosen by its 3rd-class DISCIPLINE (owner rework, 2026-07-15). You pick
    /// a discipline from ALL races/disciplines (not a bare base class); the new class starts at level 1
    /// but with that 3rd class already APPROVED — race, base class and 2nd class all come from it, so
    /// the 2nd/3rd-class quests are skipped as a bonus. (Once a 4th tier exists this is unchanged: a 3rd
    /// class still has one 4th path, still quested.)
    ///
    /// Rules: character must be level 76+ (stand-in for the future 4th class). Normal accounts cap at
    /// <see cref="GameConstants.MaxSubclasses"/>; ADMINS are unlimited. NO duplicate DISCIPLINE (a
    /// Tempest bars every Tempest, across races). Every equipped item is UNEQUIPPED — you don't play a
    /// level-1 class in level-76 gear.</summary>
    private void HandleDebugAddSubclass(DebugAddSubclassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;

        // Completeness gate (normal accounts): every class you already own must be level 75+ AND hold its
        // 3rd class before you may add another. No 4th tier exists, so "3rd class + level 75" is the gate —
        // and requiring ALL owned classes to clear it stops stacking half-levelled subclasses (a freshly
        // added class starts at level 1, so you level each one to 75 before the next add). Admins bypass
        // this, same as the count cap below.
        if (!player.IsAdmin)
        {
            var incomplete = player.Subclasses
                .FirstOrDefault(s => s.Level < ThirdClassCatalog.SubclassLevel || s.ThirdClass <= 0);
            if (incomplete is not null)
            {
                SendSystemToEntity(player,
                    $"Every class must reach level {ThirdClassCatalog.SubclassLevel} and its 3rd class before you can add another.");
                return;
            }
        }

        // Count cap — admins are unlimited (the no-duplicate-discipline filter still applies to them).
        if (!player.IsAdmin && player.Subclasses.Count >= GameConstants.MaxSubclasses)
        {
            SendSystemToEntity(player,
                $"You can own at most {GameConstants.MaxSubclasses} classes.");
            return;
        }

        if (ThirdClassCatalog.Get(cmd.ThirdClassId) is not { } tcd)
            return;

        // No two of the same discipline (across races) — checked against ALL owned classes, active too.
        if (!player.CanAddDiscipline(cmd.ThirdClassId))
        {
            // Names the CLASS, not the raw discipline: since names went per-race an Ork's Bulwark
            // is called an Ironhide, and printing the enum would show a word he has never seen.
            SendSystemToEntity(player, $"You already walk that path — {tcd.Name} shares it.");
            return;
        }

        // Unequip everything — a fresh level-1 class doesn't play in the old class's gear.
        foreach (var item in player.Inventory) item.Equipped = false;

        int slot = player.Subclasses.Max(s => s.Slot) + 1;
        var parent = ClassCatalog.Get(tcd.ParentSecondClassId);
        var sc = new Subclass
        {
            Slot = slot,
            Race = tcd.Race,                       // the discipline's OWN race (cross-race allowed)
            BaseClass = parent?.Base ?? BaseClass.Fighter,
            SecondClass = tcd.ParentSecondClassId, // 2nd class pre-approved
            ThirdClass = tcd.Id,                   // 3rd class pre-approved (skips the quests)
        };
        sc.RollBaseStats();
        player.Subclasses.Add(sc);

        ActivateSubclass(player, slot,
            $"Added {tcd.Race} {tcd.Name} as class #{slot} (level 1). Your gear was unequipped.");
        SendInventory(player);
    }

    /// <summary>Switch to a class this character already owns — under the player-facing rules he
    /// finally gave on 2026-08-14 (`BL-36`). Until now the swap was a bare debug entry point; the
    /// comment above has said since it was written that the rules would gate the COMMAND rather than
    /// the mechanism, and this is that gate. <see cref="ActivateSubclass"/> is untouched.
    ///
    /// <para>His three rules:</para>
    /// <list type="number">
    ///   <item>Out of combat — required either way.</item>
    ///   <item>In a town or peace zone: INSTANT, no wait at all.</item>
    ///   <item>Anywhere else: a <see cref="GameConstants.SubclassSwapDelaySeconds"/> wait.</item>
    /// </list>
    ///
    /// <para>🔑 And the clause that shapes the whole method — *"When changed out if town and 5min
    /// start to count and enter in town the countdown stays … w8 the 5mins then change (city don't
    /// trigger the cd) both waits it."* A running timer is NOT shortcut by reaching a town. That is
    /// why the pending-swap check sits ABOVE the safe-zone fast path and not below it: if the order
    /// were reversed, walking into the nearest town would skip the wait, which is exactly the thing
    /// he ruled out. The town rule decides whether a timer STARTS, never whether one finishes.</para>
    ///
    /// <para>⚠ MINE, not his — he did not rule on these and each is one line to change: re-asking for
    /// the same class reports the time left rather than restarting it; asking for a DIFFERENT class
    /// mid-count is refused rather than silently re-aiming the timer (re-aiming would let you burn one
    /// wait and then pick any class at the end of it); and a death cancels the change outright.</para></summary>
    private void HandleSwitchSubclass(SwitchSubclassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        if (player.Subclasses.All(s => s.Slot != cmd.Slot))
        {
            SendSystemToEntity(player, "You don't have that class.");
            return;
        }

        // A timer already running wins over everything below, wherever the player is standing.
        if (player.PendingSubclassSlot >= 0)
        {
            int secs = Math.Max(1, player.SubclassSwapTicks / GameConstants.TickRate);
            SendSystemToEntity(player, player.PendingSubclassSlot == cmd.Slot
                ? $"You are already changing class — {secs}s left."
                : $"You are already changing to another class — {secs}s left.");
            return;
        }

        if (player.ActiveSubclass.Slot == cmd.Slot)
            return;

        // Out of combat, in BOTH cases.
        if (IsInCombat(player))
        {
            SendSystemToEntity(player, "You can't change class in combat.");
            return;
        }

        // In town / a peace zone: on the spot.
        if (GameConstants.InSafeZone(player.X, player.Y))
        {
            ActivateSubclass(player, cmd.Slot, null);
            return;
        }

        // Out in the world: commit now, change in five minutes.
        player.PendingSubclassSlot = cmd.Slot;
        player.SubclassSwapTicks = GameConstants.SubclassSwapDelaySeconds * GameConstants.TickRate;
        SendSystemToEntity(player,
            $"Changing class in {GameConstants.SubclassSwapDelaySeconds / 60} minutes. "
            + "Reaching a town will not make it any faster.");
    }

    /// <summary>Count a pending class change down and fire it (`BL-36`). Called once per tick for a
    /// living player, from anywhere in the world — a town included, on his explicit ruling.</summary>
    private void TickSubclassSwap(Entity player)
    {
        if (player.PendingSubclassSlot < 0) return;

        // The class you were changing to may have gone (a debug reset, a wipe) — drop it quietly
        // rather than swapping into a slot that no longer exists.
        if (player.Subclasses.All(s => s.Slot != player.PendingSubclassSlot))
        {
            player.PendingSubclassSlot = -1;
            player.SubclassSwapTicks = 0;
            return;
        }

        if (--player.SubclassSwapTicks > 0)
        {
            // One tick-a-second heads-up over the last five seconds, and at each of the last minutes.
            int left = player.SubclassSwapTicks;
            if (left % GameConstants.TickRate == 0)
            {
                int secs = left / GameConstants.TickRate;
                if (secs <= 5 || (secs % 60 == 0))
                    SendSystemToEntity(player, $"Changing class in {secs}s…");
            }
            return;
        }

        int slot = player.PendingSubclassSlot;
        player.PendingSubclassSlot = -1;
        player.SubclassSwapTicks = 0;
        ActivateSubclass(player, slot, "The change is complete.");
    }

    /// <summary>Make a class the active one and rebuild everything that hangs off it.
    ///
    /// A swap changes the character out from under every derived value — level, core stats, learned
    /// skills, and therefore every passive, every mastery and every stat in RecomputeDerived. So the
    /// whole client-visible state is re-pushed, and the things that belong to the class you LEFT are
    /// dropped: buffs (they were cast on a different class), the cast in progress, and the combat
    /// target. The INVENTORY is untouched — it is character-level, and keeping your gear across a swap
    /// is the entire point of the debug flow (compare two classes in the same gear).</summary>
    private void ActivateSubclass(Entity player, int slot, string? message)
    {
        CancelCast(player);
        player.QueuedSkillId = null;
        Disengage(player);
        player.Buffs.Clear();          // buffs belong to the class that was cast on

        player.SwitchSubclass(slot);

        AutoLearnCoreSkills(player);   // the new class's auto-granted skills (starter nuke, training, …)
        player.RecomputeDerived();
        player.Hp = Math.Min(player.Hp, player.MaxHp);
        player.Mp = Math.Min(player.Mp, player.MaxMp);

        SendStats(player);
        SendLearned(player);   // carries this class's OWN bar with it — see SendLearned
        PushBuffs(player);
        SendSubclasses(player);
        // Each class carries its OWN level, so swapping changes what quests are on offer — and with
        // them the "!" markers over NPC heads. Without this the markers kept describing the class you
        // just swapped AWAY from: the SmokeTest found a level-81 main showing no markers at all,
        // because the last push had been computed while a level-5 subclass was active.
        SendQuestLog(player);
        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), false, player.SkillPoints));

        SendSystemToEntity(player, message
            ?? $"[DEBUG] Now playing class #{slot}: {player.BaseClass} (level {player.Level}).");
        SaveEntity(player);
    }

    /// <summary>The crafting window's whole server-side input: the one permanent profession and the
    /// blueprints this character has unlocked. Everything else it draws comes from the shared
    /// RecipeCatalog, so this is deliberately two fields — see <see cref="CraftingUpdate"/>.</summary>
    private void SendCrafting(Entity p) =>
        SendTo(p, "Crafting", new CraftingUpdate(
            (int)p.Profession, p.KnownRecipes.ToArray(),
            p.CraftLevel, p.CraftExp, p.CraftBandCap, AtMaster: MasterNpcNear(p) is not null));

    private void SendSubclasses(Entity p) =>
        SendTo(p, "Subclasses", new SubclassListDto(p.Subclasses
            .OrderBy(s => s.Slot)
            .Select(s => new SubclassDto(
                s.Slot, s.Race, s.BaseClass, s.SecondClass, s.ThirdClass, s.Level,
                s.Slot == p.ActiveSubclass.Slot, s.FourthClass))
            .ToArray()));

    /// <summary>The level ceiling this character is subject to. ADMINS ARE EXEMPT — an admin needs to
    /// be able to push past the cap to test the top of the curve without lifting it for everyone.</summary>
    private static int LevelCapFor(Entity player) =>
        player.IsAdmin ? int.MaxValue : GameConstants.MaxPlayerLevel;

    /// <summary>DEBUG: move the character's level by a delta (+1 / +10 / −1 / −10).
    ///
    /// DELEVELLING DOES NOT TOUCH LearnedSkills (owner). You keep everything you learned, so you can
    /// drop to 40, check how something feels, and climb back without re-learning your whole kit. The
    /// "Skills to Learn" tab already gates by level, so it simply stops offering what you can no
    /// longer reach — nothing had to change there.
    ///
    /// The ONE thing that IS re-synced is the auto-granted combat-training passive, whose level is a
    /// pure function of character level (StatCalculator.TrainingLevelFor). It is not a skill you chose
    /// — the server re-grants it on every level-up — and leaving a level-9 (+100% attack) passive on a
    /// character you just dropped to 40 would silently inflate every damage number you were delevelling
    /// in order to measure.</summary>
    private void HandleDebugLevel(DebugLevelCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        // Level EXACTLY by the delta. Going through AwardExp would scale by ExpRate (x10) and
        // overshoot into several levels at once.
        int cap = LevelCapFor(player);
        int target = Math.Clamp(player.Level + cmd.Delta, 1, cap);
        if (target == player.Level)
        {
            SendSystemToEntity(player, cmd.Delta > 0
                ? $"[DEBUG] Already at the level cap ({cap})."
                : "[DEBUG] Already at level 1.");
            return;
        }

        bool up = target > player.Level;
        player.Level = target;
        player.Exp = 0;

        if (up)
        {
            OnLevelUp(player);
        }
        else
        {
            // Delevel: keep the learned skills, rebuild the stats off the new level.
            player.RecomputeDerived();
            player.Hp = Math.Min(player.Hp, player.MaxHp);
            player.Mp = Math.Min(player.Mp, player.MaxMp);
            SendStats(player);
            SendLearned(player);
        }

        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), up, player.SkillPoints));
        // Silent (owner): the level and the XP bar both move on screen, and holding +10 to climb to 80
        // otherwise wrote eight rows into the log for something already visible.
        SaveEntity(player);   // persist so debug levels survive a server restart
    }

    private void HandleDebugLearnAll(DebugLearnAllCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        // Highest learnable level per skill whose learn-gate is met at the current level.
        var byId = new Dictionary<string, int>();
        foreach (var cs in ClassSkills.Cumulative(player.Race, player.BaseClass, player.Archetype, player.Discipline))
        {
            if (cs.LearnLevel > player.Level) continue;
            if (!byId.TryGetValue(cs.SkillId, out var lvl) || cs.SkillLevel > lvl)
                byId[cs.SkillId] = cs.SkillLevel;
        }

        // NEVER grant the stat swaps here. They used to all be granted, which cancelled them out to
        // roughly +0 — but the fix is NOT to auto-pick a legal subset: any subset is an arbitrary
        // BUILD decision, and the obvious greedy one (take each in turn, skip what it bans) lands on
        // four swaps that all sacrifice ATK — our single power stat — for -20 ATK. That would quietly
        // wreck the damage numbers this button exists to test. A swap is a permanent gold purchase;
        // buy it deliberately in the skills window.
        var swaps = byId.Keys.Where(id => SkillCatalog.StatSwapOf(id) is not null).ToList();
        foreach (var id in swaps) byId.Remove(id);

        foreach (var (id, lvl) in byId)
            player.LearnedSkills[id] = lvl;
        // Cross-skill replacements (e.g. Flame Bolt replaces Magic Bolt).
        foreach (var id in byId.Keys.ToList())
            if (SkillCatalog.Get(id)?.Replaces is { } rep)
                foreach (var r in rep) player.LearnedSkills.Remove(r);

        player.RecomputeDerived();
        SendSystemToEntity(player, $"[DEBUG] Learned all class skills for level {player.Level}.");
        if (swaps.Count > 0)
            SendSystemToEntity(player,
                $"[DEBUG] Skipped {swaps.Count} stat-swap passives — they are a permanent build choice " +
                "(and cannot all be held at once). Buy the ones you want in the skills window.");
        SendStats(player);
        SendLearned(player);
        SaveEntity(player);
    }

    private void HandleDebugGold(DebugGoldCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.Gold += Math.Max(0, cmd.Amount);
        SendGold(player);
        SendSystemToEntity(player, $"[DEBUG] +{cmd.Amount:N0} {GameConstants.CurrencyName} (now {player.Gold:N0}).");
        SaveEntity(player);
    }

    private void HandleDebugSp(DebugSpCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.SkillPoints += (int)Math.Max(0, cmd.Amount);
        SendStats(player);
        SendLearned(player);
        SendSystemToEntity(player, $"[DEBUG] +{cmd.Amount:N0} SP (now {player.SkillPoints:N0}).");
        SaveEntity(player);
    }

    /// <summary>DEBUG: re-roll the SAME character in place — new race/base class, back to level 1,
    /// classes/skills/quests cleared. Keeps the character row, name, gold and position.
    ///
    /// The INVENTORY is deliberately KEPT (owner reversed the earlier "wipe it" call): you re-roll to
    /// test another class, and losing the gear you built up each time made that painful. Everything is
    /// UNEQUIPPED instead — the old class's kit is usually wrong for the new one — and the starter kit
    /// is topped up only with the pieces you don't already own, so repeated re-rolls don't silt the
    /// bag up with duplicate newbie boxes.
    ///
    /// SUBCLASSES ARE DROPPED, on purpose. This is a whole-character re-roll, and RACE is
    /// character-level: any other class you owned had its core stats rolled for the OLD race, so
    /// keeping them would leave the character carrying classes whose stats no longer match its body.
    /// You are left with exactly one class again, the one you just picked. (To keep a class and add
    /// another, use the SUBCLASS buttons — that is what they are for.)</summary>
    private void HandleDebugReset(DebugResetCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;

        player.Subclasses.Clear();
        var main = new Subclass { Slot = 0, Race = cmd.Race, BaseClass = cmd.BaseClass };
        main.RollBaseStats();
        player.Subclasses.Add(main);
        player.SwitchSubclass(0);

        player.ActiveQuests.Clear();
        player.CompletedQuests.Clear();
        player.Buffs.Clear();
        foreach (var item in player.Inventory) item.Equipped = false;
        GiveStarterKit(player, skipOwned: true);

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendInventory(player);
        SendStats(player);
        SendLearned(player);   // carries the bar with it — see SendLearned
        SendSubclasses(player);
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

        // LEVEL GATE (owner, 2026-07-17: a level-33 character could take a 3rd class here). This path
        // skips the quest and its items on purpose — but level 40 is the RULE, not part of the walk, and
        // a below-40 3rd class silently drags the auto-granted training passive + stat swaps (both gated
        // on ThirdClass > 0) below the level they were tuned for. CanTakeThirdClass is only a
        // discipline-uniqueness test, so it never covered this. Deliberately NOT admin-exempt: debug-level
        // to 40 first, which is one button away.
        if (player.Level < ThirdClassCatalog.ChangeLevel)
        {
            SendSystemToEntity(player,
                $"[DEBUG] A 3rd class requires level {ThirdClassCatalog.ChangeLevel} (you are {player.Level}).");
            return;
        }

        // You may not walk the same DISCIPLINE twice across your classes (see Entity). No archetype
        // check — several classes may share a 2nd class as long as their disciplines differ.
        if (!player.CanTakeThirdClass(cmd.ThirdClassId))
        {
            SendSystemToEntity(player, $"Another of your classes already walks the {tcd.Name} path.");
            return;
        }

        // Ensure the parent 2nd class. (No core-stat bonus: class changes no longer touch
        // main stats — see the 2nd-class path.)
        if (player.SecondClass != tcd.ParentSecondClassId)
            player.SecondClass = tcd.ParentSecondClassId;
        player.ThirdClass = cmd.ThirdClassId;
        // A debug hop to a DIFFERENT discipline invalidates any ascension already taken — a 4th
        // class is only ever its own 3rd's. Cleared unconditionally rather than compared, because
        // re-picking the same discipline is a no-op that costs nothing to redo.
        player.FourthClass = 0;

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendStats(player);
        SendLearned(player);
        SendSubclasses(player);   // keep the client's class list fresh so the add-class picker filters the main
        SaveEntity(player);
        BroadcastSystem($"{player.Name} has become a {tcd.Name}!");
    }

    /// <summary>DEBUG: ascend to the 4th class, or drop back to the 3rd. Deliberately a TOGGLE —
    /// the real change is one-way and irreversible, so the only way to test the level-76 gate and
    /// the not-yet-ascended UI twice in one session is to be able to step back down.
    ///
    /// Unlike the 3rd-class debug this does NOT check the level: an admin ascends to inspect the
    /// state, and forcing a /level to 76 first would only add a step. The REAL path
    /// (ClassChangeAvailable + DoQuestClassChange) still enforces every gate.</summary>
    private void HandleDebugFourthClass(DebugFourthClassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;

        if (player.FourthClass != 0)
        {
            player.FourthClass = 0;
            SendSystemToEntity(player, "[DEBUG] Dropped back to your 3rd class.");
        }
        else if (player.ThirdClass == 0)
        {
            SendSystemToEntity(player, "[DEBUG] Take a 3rd class first — a 4th class ascends one.");
            return;
        }
        else if (FourthClassCatalog.ForParent(player.ThirdClass) is not { } fcd)
        {
            SendSystemToEntity(player, "[DEBUG] That 3rd class has no ascension registered.");
            return;
        }
        else
        {
            player.FourthClass = fcd.Id;
            SendSystemToEntity(player, $"[DEBUG] Ascended: you are a {fcd.Name}.");
        }

        // No AutoLearnCoreSkills: a 4th class grants NO skills yet (the kit waits on the owner's
        // 4th CSVs — see Classes.Fourth.cs). RecomputeDerived is still called because the craft
        // band cap reads FourthClass, and SendCrafting so the change is visible without a relog.
        player.RecomputeDerived();
        SendStats(player);
        SendSubclasses(player);   // the ONLY message that moves the client's class label
        SendCrafting(player);
        SaveEntity(player);
    }

    /// <summary>Grant the new-character starter kit to a live entity (mirrors
    /// PersistenceService.CreateCharacterAsync). Items arrive unequipped.</summary>
    /// <summary><paramref name="skipOwned"/> = only hand over pieces the player does not already have.
    /// Used by the debug re-roll, which now KEEPS the inventory: a fighter re-rolled into a mage should
    /// gain the staff he lacks without collecting a second copy of every newbie box he already holds.</summary>
    private void GiveStarterKit(Entity player, bool skipOwned = false)
    {
        void Give(string defId, int qty = 1)
        {
            if (skipOwned && player.Inventory.Any(i => i.DefId == defId)) return;
            AddItem(player, defId, qty);
        }

        // Matches CreateCharacterAsync: potions only. The two training boxes left creation on
        // 2026-08-12 (him, 63j) — the tutorial's own steps supply them at the moment they are needed,
        // and handing them out here as well is what gave him three of everything.
        // No jewels and no runes at creation either — jewels are earned from level 1-5 mobs or bought,
        // and the rune arrives with the level-10 starter quest along with the Newbie set.
        Give(ItemCatalog.MinorPotion, 5);
        Give(ItemCatalog.GreaterPotion, 2);
    }
#pragma warning restore CS1998

    private void HandleUsePotion(UsePotionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        // The Rune of Tincture has no use-skill: it opens the palette (`59r`). Answered by
        // SetTitleColor, which is the command that consumes it — opening a list costs nothing.
        if (item.DefId == ItemCatalog.TitleColorRune)
        {
            if (!player.CanWriteTitle)
            {
                SendSystemToEntity(player, "You have not been granted the right to name yourself.");
                return;
            }
            SendTo(player, "TitleColors", new TitleColorOffer(
                Array.ConvertAll(TitleCatalog.Palette, c => c.Name)));
            return;
        }

        UsePotion(player, item, cmd.TargetId);
    }

    /// <summary>A dead player answered a resurrection offer. Accept → revive (restoring the offered exp);
    /// decline → clear the offer and stay dead. A stale offer (already respawned/expired) is a no-op.</summary>
    private void HandleResurrectResponse(ResurrectResponseCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (player.PendingResFromId is not Guid fromId) return;   // no pending offer
        float pct = player.PendingResExpPct;
        player.PendingResFromId = null;
        player.PendingResTicks = 0;
        if (!cmd.Accept || !player.Dead) return;                  // declined, or already revived/respawned
        // The rescuer is only used for a courtesy line; if they've since left, the target still revives.
        var rescuer = _world.Entities.GetValueOrDefault(fromId) ?? player;
        ResurrectTarget(rescuer, player, pct);
    }

    /// <summary>Consume one potion (HP HoT/instant, or a buff potion). Shared by the manual
    /// UsePotion command and the auto-hunt auto-potion path. Returns true if something was drunk
    /// (false = not a potion, or on cooldown). Buff potions ignore the shared heal cooldown.</summary>
    private bool UsePotion(Entity player, InventoryItem item, Guid? targetId = null)
    {
        if (player.Dead || ItemCatalog.Get(item.DefId) is not ItemDef def || !ItemCatalog.IsPotion(def))
            return false;
        if (SkillCatalog.Get(def.UseSkillId) is not SkillDef skill)
            return false;   // an inert consumable (a reagent like the Elemental Stone)

        // JAILED players can't ESCAPE — Return scrolls (and any teleport-to-town item) are blocked (owner).
        if (player.Jailed && skill.TeleportsToTown)
        {
            SendSystemToEntity(player, "You can't escape while jailed.");
            return false;
        }

        // A consumable with a CAST TIME (the Return / Resurrection scrolls) is channelled: queue the skill
        // and let the normal cast pipeline run it. It consumes the item itself (its ConsumableId) when the
        // cast lands, and refunds it if interrupted. The skill is NOT learned — the ITEM grants it.
        if (skill.CastTicks > 0)
        {
            if (player.CastingSkillId is not null || player.QueuedSkillId is not null)
                return false;
            if (player.SkillCooldowns.TryGetValue(skill.Id, out int cd) && cd > 0)
            {
                SendSystemToEntity(player, $"{skill.Name} is on cooldown.");
                return false;
            }
            // A buff scroll is charged for when the cast LANDS, so a read that would be refused on
            // rank has to be stopped here — otherwise the scroll is spent on nothing at all.
            if ((skill.Effect & SkillEffect.AnyBuff) != 0 && !BuffWouldLand(player, skill, 1))
            {
                SendSystemToEntity(player, $"{skill.Name} would have no effect — a stronger blessing is already active.");
                return false;
            }
            // A resurrection scroll targets a DEAD ALLY (like the healer's res), not the user. Validate the
            // named target the same way the cast command does; everything else channels on the user.
            if (skill.Resurrect)
            {
                if (targetId is not Guid rid || rid == player.Id ||
                    !_world.Entities.TryGetValue(rid, out var corpse) ||
                    corpse.Kind != EntityKind.Player || !corpse.Dead ||
                    DistanceSq(player, corpse) > GameConstants.ViewRange * GameConstants.ViewRange)
                {
                    SendSystemToEntity(player, $"{skill.Name} needs a fallen ally as its target.");
                    return false;
                }
                // Same rule as the cast path: reading a res scroll over an outlaw flags you NOW, for
                // the duration of the read, not when they stand up (playtest 23).
                FlagForSupporting(player, corpse);
                player.QueuedSkillId = skill.Id;
                player.QueuedTargetId = rid;
                return true;
            }
            player.QueuedSkillId = skill.Id;
            player.QueuedTargetId = player.Id;
            // A buff scroll's skill names no ConsumableId (there are 48 of them and the id would have
            // to be authored on both sides), so remember the INSTANCE that started the cast and charge
            // for that when it lands. Skills that DO name their item keep the old path — setting this
            // as well would consume two scrolls for one read.
            if (string.IsNullOrEmpty(skill.ConsumableId))
                player.CastFromItemInstance = item.InstanceId;
            return true;
        }

        // Instant consumable (drink it). Each HEALING potion has its OWN drink cooldown (owner: a potion
        // shares a cooldown only with itself); buff potions are free of it.
        bool healing = ItemCatalog.IsHealPotion(def);
        if (healing && player.PotionCooldowns.TryGetValue(def.Id, out var pcd) && pcd > 0)
            return false;

        // Can't drink a WEAKER heal potion while a STRONGER one's effect is still running (owner). Refuse
        // rather than consume it — ApplyBuff would silently ignore the weaker buff and waste the potion.
        // (A same-tier re-drink is allowed — it restarts the HoT, losing part of it by design.)
        if (healing && !string.IsNullOrEmpty(skill.BuffKey) && skill.Rank > 0)
            foreach (var active in player.Buffs)
                if (active.Key == skill.BuffKey && active.Rank > skill.Rank)
                {
                    SendSystemToEntity(player, $"A stronger effect ({active.Name}) is already active.");
                    return false;
                }

        // A ZERO-cast consumable still has its own reuse timer — the channelled path above set that for
        // it, so without this an instant scroll would have no cooldown at all.
        if (player.SkillCooldowns.TryGetValue(skill.Id, out int icd) && icd > 0)
        {
            SendSystemToEntity(player, $"{skill.Name} is on cooldown.");
            return false;
        }
        // The SKILL decides what happens — we only deliver it. An instant Heal restores a % of max
        // HP; anything with a lasting effect (a HoT potion, a buff potion) becomes an ordinary buff,
        // so it lands on the buff bar and supersedes weaker ones by BuffKey + Rank.
        //
        // TeleportsToTown must be handled HERE as well as on the cast-completion path: the Ultimate
        // Scroll of Return is the escape button, so it has NO cast — and a 0-tick skill never reaches
        // the cast pipeline. Without this it would be eaten for no effect.
        // Drinking or reading something reveals you (BL-69) — his list is "hitting, a skill, a
        // potion". Only movement is free.
        BreakHide(player);

        if (skill.TeleportsToTown)
            ReturnToTown(player);
        if ((skill.Effect & SkillEffect.Heal) != 0)
        {
            float pct = skill.MagnitudeOf(SkillEffect.Heal, ModifierMode.Percent);
            int amount = Math.Max(1, skill.Power + (int)(player.MaxHp * pct));
            player.Hp = Math.Min(player.MaxHp, player.Hp + amount);
            BroadcastCombat(player, player, amount, CombatOutcome.Heal, skill.Name);
        }
        if ((skill.Effect & SkillEffect.AnyBuff) != 0)
        {
            // REFUSED on rank (something stronger — or equally strong but longer — is already up):
            // don't eat the item and don't start its reuse. This used to consume it either way,
            // which was rare before improved buffs became ladders and is common now.
            if (!ApplyBuff(player, skill))
            {
                SendSystemToEntity(player, $"{skill.Name} had no effect — a stronger blessing is already active.");
                return false;
            }
            PushBuffs(player);
        }

        // Reuse starts only once the item has actually done something (see above).
        if (skill.CooldownTicks > 0)
            player.SkillCooldowns[skill.Id] = skill.CooldownTicks;   // fixed: never shortened by reuse buffs

        ConsumeOne(player, item);
        if (healing && def.PotionCooldownTicks > 0)
            player.PotionCooldowns[def.Id] = def.PotionCooldownTicks;

        SendInventory(player);
        SendPotionStatus(player);
        SendCooldowns(player);   // both channels: the drink timer AND the scroll's own reuse
        if (!healing) SendSystemToEntity(player, $"{skill.Name} active.");
        return true;
    }

    /// <summary>Push every reuse timer the player has running, keyed by the ACTION-BAR TOKEN
    /// (skill id, or "item:defId" for a drink cooldown) so the client can match it against the bar it
    /// already holds. Called whenever a timer STARTS — the client counts down from there, so an
    /// expiry costs no message and the per-tick decrement stays silent.</summary>
    private void SendCooldowns(Entity player)
    {
        if (player.Kind != EntityKind.Player) return;
        int n = player.SkillCooldowns.Count + player.PotionCooldowns.Count;
        if (n == 0)
        {
            // Still send the EMPTY set: it is what clears a stale overlay after a cooldown was
            // wiped rather than ticked away (a subclass swap, a death, an admin reset).
            SendTo(player, "Cooldowns", new CooldownUpdate(Array.Empty<CooldownEntry>()));
            return;
        }
        var entries = new List<CooldownEntry>(n);
        foreach (var kv in player.SkillCooldowns)
            if (kv.Value > 0) entries.Add(new CooldownEntry(kv.Key, kv.Value * GameConstants.TickSeconds));
        foreach (var kv in player.PotionCooldowns)
            if (kv.Value > 0) entries.Add(new CooldownEntry(GameConstants.ItemSlotToken(kv.Key),
                                                            kv.Value * GameConstants.TickSeconds));
        SendTo(player, "Cooldowns", new CooldownUpdate(entries.ToArray()));
    }

    private void SendPotionStatus(Entity player)
    {
        // The lingering effect is a BUFF now (it shows on the buff bar). The potion channel still owns
        // the drink cooldowns, now PER-POTION; report the longest one remaining as the single HUD value.
        int maxCd = 0;
        foreach (var c in player.PotionCooldowns.Values) if (c > maxCd) maxCd = c;
        SendTo(player, "Potion", new PotionStatus(maxCd / (float)GameConstants.TickRate, ""));
    }

    // ----- Trade ---------------------------------------------------------------------------

    private void HandleTradeRequest(TradeRequestCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var requester)) return;

        // DEATH does not bar a trade, on either side (owner, 2026-08-07 — this REVERSES the first
        // reading of M4). His case decides it: you die, your friend has no resurrection scroll, and the
        // scroll is in YOUR bag — refusing the trade puts the one item that could fix the situation out
        // of reach. Nothing is dodged by allowing it: whatever a death costs is already spent by the
        // time there is a corpse to trade with, and the PK/flagged ban below is what stops gear being
        // laundered. Party invite is allowed for the same reason — being dead is when you need people.

        if (_world.ActiveTrades.ContainsKey(requester.Id))
            return;

        if (!_world.Entities.TryGetValue(cmd.TargetId, out var target) ||
            target.Kind != EntityKind.Player)
        {
            SendSystemToEntity(requester, "That player cannot trade right now.");
            return;
        }
        // `/decline-t` (M2): refused before it ever reaches their screen. The requester is told, so a
        // request that went nowhere doesn't read as a lost packet.
        if (target.Refuses(SocialOptions.DeclineTrades, requester))
        {
            SendSystemToEntity(requester, $"{target.Name} is not accepting trades.");
            return;
        }
        if (_world.ActiveTrades.ContainsKey(target.Id))
        {
            SendSystemToEntity(requester, $"{target.Name} is already trading.");
            return;
        }

        // `BL-59` — PK (red) only. His re-spec, verbatim: trade is *"allowed with **pvp**, NOT with
        // pk"*. It used to refuse BOTH, which made the purple flag a 60-second trading ban that a
        // player earned simply by defending themselves — and purple is a temporary state, not a
        // sentence. Karma is the sentence, so karma is what blocks a trade.
        if (FlagOf(requester) == PvpFlag.Pk)
        {
            SendSystemToEntity(requester, "You can't trade while you are a PK.");
            return;
        }
        if (FlagOf(target) == PvpFlag.Pk)
        {
            SendSystemToEntity(requester, $"{target.Name} is a PK and can't trade.");
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

        // Dead is fine on both sides — see HandleTradeRequest. Only "gone from the world" refuses.
        if (!_world.Entities.TryGetValue(requesterId, out var requester))
            return;

        if (!cmd.Accept)
        {
            SendSystemToEntity(requester, $"{responder.Name} declined the trade.");
            return;
        }

        if (_world.ActiveTrades.ContainsKey(requester.Id) ||
            _world.ActiveTrades.ContainsKey(responder.Id))
            return;

        // Re-check at accept time (a flag can change after the request) — PK only, per `BL-59`.
        if (FlagOf(requester) == PvpFlag.Pk || FlagOf(responder) == PvpFlag.Pk)
        {
            SendSystemToEntity(responder, "You can't trade while either of you is a PK.");
            SendSystemToEntity(requester, "You can't trade while either of you is a PK.");
            return;
        }

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

        var seen = new HashSet<Guid>();
        foreach (var entry in cmd.Entries)
        {
            if (!seen.Add(entry.InstanceId)) continue;
            if (offer.Count >= GameConstants.TradeMaxOfferSlots) break;

            var item = player.Inventory.FirstOrDefault(i => i.InstanceId == entry.InstanceId);
            if (item is null || item.Equipped) continue;
            var d = ItemCatalog.Get(item.DefId);
            if (d is not null && (!item.Tradable(d) || ItemCatalog.IsQuestItem(d)))
                continue;   // untradeable / quest items can't be traded (per INSTANCE since `58d`)

            // Clamp the count here rather than trusting the client: only a stackable can be split,
            // and never past what is actually in the stack.
            int qty = d is { IsStackable: true }
                ? Math.Clamp(entry.Quantity, 1, item.Quantity)
                : 1;
            offer.Add(new TradeOfferEntry(entry.InstanceId, qty));
        }

        // Changing an offer resets both ready flags (no bait-and-switch).
        session.ReadyA = false;
        session.ReadyB = false;
        SendTradeState(session);
    }

    private void HandleTradeGold(TradeGoldCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) ||
            !_world.ActiveTrades.TryGetValue(player.Id, out var session))
            return;

        // Clamp to what you actually have (and non-negative). Final ownership is re-checked at
        // completion, so a mid-trade spend can't overdraw.
        session.SetGold(player, Math.Clamp(cmd.Gold, 0, player.Gold));

        // Changing your offer resets both ready flags (no bait-and-switch), same as changing items.
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

    // ----- Party / grouping ------------------------------------------------------------------
    private const int PartyMaxSize = 9;
    private const int PartyInviteTimeoutTicks = 300;   // ~30s: stale invites auto-expire

    // Scratch list so the invite sweep doesn't allocate/modify the dict while enumerating it.
    private readonly List<Guid> _expiredInvites = new();

    /// <summary>Drop party invites nobody answered in time so they stop blocking re-invites; tell
    /// the inviter it lapsed. (The invitee's prompt auto-dismisses client-side on the same timer.)</summary>
    private void SweepPartyInvites()
    {
        if (_world.PendingPartyInviteExpiry.Count == 0)
            return;
        _expiredInvites.Clear();
        foreach (var (targetId, expireTick) in _world.PendingPartyInviteExpiry)
            if (_tick >= expireTick)
                _expiredInvites.Add(targetId);
        foreach (var targetId in _expiredInvites)
        {
            _world.PendingPartyInviteExpiry.Remove(targetId);
            if (_world.PendingPartyInvites.Remove(targetId, out var inviterId) &&
                _world.Entities.TryGetValue(inviterId, out var inviter) &&
                _world.Entities.TryGetValue(targetId, out var target))
                SendSystemToEntity(inviter, $"{target.Name} didn't respond to your party invite.");
        }
    }

    private void HandlePartyInvite(PartyInviteCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var inviter))
            return;
        if (!_world.Entities.TryGetValue(cmd.TargetId, out var target))
        {
            SendSystemToEntity(inviter, "You can't invite that player.");
            return;
        }
        DoPartyInvite(inviter, target);
    }

    /// <summary>Invite BY NAME (`/ptinv <name>`). The client used to resolve the name itself out of
    /// the entities it could see, so an invite failed with "no player x nearby" for anyone out of
    /// view — while the same party worked fine once you walked away (playtest-19 46d). A name is
    /// resolved here instead, against every player in the world.</summary>
    private void HandlePartyInviteByName(PartyInviteByNameCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var inviter))
            return;
        string name = (cmd.Name ?? "").Trim();
        if (name.Length == 0) return;

        var target = _world.Entities.Values.FirstOrDefault(e =>
            e.Kind == EntityKind.Player &&
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            SendSystemToEntity(inviter, $"No player named '{name}' is online.");
            return;
        }
        DoPartyInvite(inviter, target);
    }

    /// <summary>The shared invite rules, once the target has been resolved by id or by name.
    ///
    /// DEATH is deliberately not a bar, on either side (owner, playtest-19 M4): being dead is exactly
    /// when you want to be pulled into a party, because that is where the resurrection comes from.
    ///
    /// <para>Neither is a PVP FLAG or KARMA, on either side — *"party invite to a pvp/pk player:
    /// allowed"* (`BL-59`). There is no flag test below and none is wanted: the party is precisely
    /// what makes helping an outlaw legal (see <see cref="CanSupport"/>), so barring the invite would
    /// make that permission unreachable. Recorded here because "no code" is otherwise indistinguishable
    /// from "nobody thought about it".</para></summary>
    private void DoPartyInvite(Entity inviter, Entity target)
    {
        if (target.Kind != EntityKind.Player || target.Id == inviter.Id)
        {
            SendSystemToEntity(inviter, "You can't invite that player.");
            return;
        }
        // AFK players (auto-hunting or offline-farming) won't respond — don't leave a stuck invite.
        if (target.AutoHuntEnabled || target.IsOfflineFarming)
        {
            SendSystemToEntity(inviter, $"{target.Name} is auto-hunting and can't be invited right now.");
            return;
        }
        // If the inviter is already in a party, only the leader can invite, and it must have room.
        if (_world.Parties.TryGetValue(inviter.Id, out var party))
        {
            if (party.LeaderId != inviter.Id)
            {
                SendSystemToEntity(inviter, "Only the party leader can invite.");
                return;
            }
            if (party.Members.Count >= PartyMaxSize)
            {
                SendSystemToEntity(inviter, "The party is full.");
                return;
            }
        }
        if (_world.Parties.ContainsKey(target.Id))
        {
            SendSystemToEntity(inviter, $"{target.Name} is already in a party.");
            return;
        }
        // `/decline-p` (M2) — same shape as the trade refusal, and equally not applicable to staff.
        if (target.Refuses(SocialOptions.DeclineParty, inviter))
        {
            SendSystemToEntity(inviter, $"{target.Name} is not accepting party invitations.");
            return;
        }
        if (_world.PendingPartyInvites.ContainsKey(target.Id))
        {
            SendSystemToEntity(inviter, $"{target.Name} is considering another invite.");
            return;
        }

        // Show the invitee the loot rule they'd be joining under: the inviter's party mode if they
        // already have one, else the default a new party will be created with.
        LootMode joinMode = _world.Parties.TryGetValue(inviter.Id, out var invParty)
            ? invParty.LootMode : Party.DefaultLootMode;
        _world.PendingPartyInvites[target.Id] = inviter.Id;
        _world.PendingPartyInviteExpiry[target.Id] = _tick + PartyInviteTimeoutTicks;
        SendTo(target, "PartyInvite", new PartyInviteDto(inviter.Id, inviter.Name, joinMode));
        SendSystemToEntity(inviter, $"Party invite sent to {target.Name}.");
    }

    private void HandlePartyRespond(PartyRespondCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var responder))
            return;
        if (!_world.PendingPartyInvites.Remove(responder.Id, out var inviterId))
            return;
        _world.PendingPartyInviteExpiry.Remove(responder.Id);
        if (!_world.Entities.TryGetValue(inviterId, out var inviter) || inviter.Dead)
            return;

        if (!cmd.Accept)
        {
            SendSystemToEntity(inviter, $"{responder.Name} declined your party invite.");
            return;
        }
        if (_world.Parties.ContainsKey(responder.Id))   // joined something else meanwhile
            return;

        // Get the inviter's party, creating it (inviter = leader) on the first invite.
        if (!_world.Parties.TryGetValue(inviter.Id, out var party))
        {
            party = new Party { LeaderId = inviter.Id };
            party.Members.Add(inviter.Id);
            _world.Parties[inviter.Id] = party;
        }
        if (party.Members.Count >= PartyMaxSize)
        {
            SendSystemToEntity(responder, "That party is full.");
            SendSystemToEntity(inviter, "Your party is full.");
            if (party.Members.Count == 1) _world.Parties.Remove(inviter.Id);   // undo a just-created empty party
            return;
        }

        party.Members.Add(responder.Id);
        _world.Parties[responder.Id] = party;
        SendPartyUpdate(party);
        BroadcastToParty(party, $"{responder.Name} joined the party.");
    }

    private void HandlePartyLeave(PartyLeaveCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        RemoveFromParty(player, "left the party");
    }

    private void HandlePartyChangeLeader(PartyChangeLeaderCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var leader))
            return;
        if (!_world.Parties.TryGetValue(leader.Id, out var party) || party.LeaderId != leader.Id)
        {
            SendSystemToEntity(leader, "Only the party leader can pass leadership.");
            return;
        }
        if (cmd.TargetId == leader.Id || !party.Contains(cmd.TargetId))
            return;
        party.LeaderId = cmd.TargetId;
        SendPartyUpdate(party);
        if (_world.Entities.TryGetValue(cmd.TargetId, out var newLeader))
            BroadcastToParty(party, $"{newLeader.Name} is now the party leader.");
    }

    private void HandlePartyKick(PartyKickCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var leader))
            return;
        if (!_world.Parties.TryGetValue(leader.Id, out var party) || party.LeaderId != leader.Id)
        {
            SendSystemToEntity(leader, "Only the party leader can remove members.");
            return;
        }
        if (cmd.TargetId == leader.Id || !party.Contains(cmd.TargetId))
            return;
        if (_world.Entities.TryGetValue(cmd.TargetId, out var target))
            RemoveFromParty(target, "was removed from the party");
    }

    // A loot-rule vote auto-cancels if the party hasn't all agreed within this window (~30s).
    private const int LootVoteTimeoutTicks = 300;

    /// <summary>Leader PROPOSES a loot-rule change: it doesn't apply until every other member
    /// accepts (unanimous). Opens a vote and prompts the members.</summary>
    private void HandlePartySetLootMode(PartySetLootModeCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var leader))
            return;
        if (!_world.Parties.TryGetValue(leader.Id, out var party) || party.LeaderId != leader.Id)
        {
            SendSystemToEntity(leader, "Only the party leader can change the loot rule.");
            return;
        }
        if (party.PendingLootMode is not null)
        {
            SendSystemToEntity(leader, "A loot-rule vote is already in progress.");
            return;
        }
        if (party.LootMode == cmd.Mode)
            return;

        party.PendingLootMode = cmd.Mode;
        party.LootVotePending.Clear();
        foreach (var mid in party.Members)
            if (mid != leader.Id) party.LootVotePending.Add(mid);
        party.LootVoteExpireTick = _tick + LootVoteTimeoutTicks;

        // No other members to ask (shouldn't happen — parties are >= 2) → apply straight away.
        if (party.LootVotePending.Count == 0)
        {
            ApplyLootMode(party);
            return;
        }

        var prompt = new PartyLootVoteDto(cmd.Mode, leader.Name);
        foreach (var mid in party.LootVotePending)
            if (_world.Entities.TryGetValue(mid, out var m))
                SendTo(m, "PartyLootVote", prompt);
        SendSystemToEntity(leader,
            $"Proposed loot rule {LootModeLabel(cmd.Mode)} — waiting for the party to agree.");
        BroadcastToParty(party, $"{leader.Name} proposes the loot rule become {LootModeLabel(cmd.Mode)}.");
    }

    /// <summary>A member accepts/declines the pending loot-rule vote. Unanimous accept applies it;
    /// any decline cancels it.</summary>
    private void HandlePartyLootVote(PartyLootVoteCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var voter))
            return;
        if (!_world.Parties.TryGetValue(voter.Id, out var party) ||
            party.PendingLootMode is not LootMode mode ||
            !party.LootVotePending.Contains(voter.Id))
            return;

        if (!cmd.Accept)
        {
            ClearLootVote(party);
            BroadcastToParty(party,
                $"{voter.Name} declined the loot-rule change to {LootModeLabel(mode)}. Cancelled.");
            return;
        }
        party.LootVotePending.Remove(voter.Id);
        if (party.LootVotePending.Count == 0)
            ApplyLootMode(party);
        else
            BroadcastToParty(party,
                $"{voter.Name} agreed ({party.LootVotePending.Count} still to vote).");
    }

    /// <summary>Commit the agreed pending loot mode and clear the vote.</summary>
    private void ApplyLootMode(Party party)
    {
        if (party.PendingLootMode is not LootMode mode)
            return;
        party.LootMode = mode;
        party.RoundRobinCursor = -1;   // restart rotation on a rule change
        ClearLootVote(party);          // dismisses prompts + resyncs the roster with the new mode
        BroadcastToParty(party, $"Loot rule set to {LootModeLabel(mode)} (agreed by all).");
    }

    /// <summary>End any pending loot vote: drop the state, dismiss the members' prompts, and resync
    /// the roster (so a leader's combo snaps back to the authoritative mode on a cancel).</summary>
    private void ClearLootVote(Party party)
    {
        bool wasPending = party.PendingLootMode is not null;
        party.PendingLootMode = null;
        party.LootVotePending.Clear();
        party.LootVoteExpireTick = 0;
        if (!wasPending)
            return;
        var close = new PartyLootVoteDto(default, "", Open: false);
        foreach (var mid in party.Members)
            if (_world.Entities.TryGetValue(mid, out var m))
                SendTo(m, "PartyLootVote", close);
        SendPartyUpdate(party);
    }

    private static string LootModeLabel(LootMode mode) => mode switch
    {
        LootMode.FindersKeepers => "Finders Keepers",
        LootMode.Random         => "Random",
        LootMode.RoundRobin     => "Round Robin",
        LootMode.LeaderOnly     => "Leader Only",
        _                       => mode.ToString(),
    };

    // ----- Auto-hunt / idle farming (docs/design/AutoHunt.md) -------------------------

    // Roaming: how far past the farm circle a current target may be chased (soft static spot), and
    // how close to the static centre counts as "back home".
    private const float AutoChaseMargin = 400f;
    private const float AutoReturnEpsilon = 150f;

    // Server-default DAILY caps (docs/design/AutoHunt.md): online auto 8h, offline farm 2h; disconnect
    // grace 180s. Tunable in seconds via the Debug panel / /testcaps. Premium (12h/4h) is the
    // per-account override on AccountFarmBudget, which wins over these.
    private int _idleCapSeconds = 8 * 3600;
    private int _offlineCapSeconds = 2 * 3600;
    private int _graceSeconds = 180;

    // 0 (or less) = UNLIMITED (never caps) — for leaving people farming/levelling to gauge speed.
    private int AutoIdleCapSecondsFor(AccountFarmBudget b) => AccountFarmBudget.ResolveCap(b.AutoCapSeconds, _idleCapSeconds);
    private int AutoOfflineCapSecondsFor(AccountFarmBudget b) => AccountFarmBudget.ResolveCap(b.OfflineCapSeconds, _offlineCapSeconds);

    /// <summary>The account's live daily allowance, refilled if the server date has rolled over.
    /// Returns null only for an account-less entity (a mob, or a character created before accounts) —
    /// which is treated as UNLIMITED everywhere, because there is nothing to bill.
    ///
    /// <para>Lazily created rather than required: a character can reach the world down paths that never
    /// went through the login read (the debug seeder, a test harness), and refusing to farm because a
    /// row wasn't pre-loaded would be a bug, not a policy.</para></summary>
    private AccountFarmBudget? BudgetOf(Entity p)
    {
        if (p.AccountId == 0) return null;
        if (!_world.AccountBudgets.TryGetValue(p.AccountId, out var b))
            _world.AccountBudgets[p.AccountId] = b = new AccountFarmBudget { AccountId = p.AccountId };
        b.EnsureFresh(AutoIdleCapSecondsFor(b), AutoOfflineCapSecondsFor(b), GameConstants.TickRate);
        return b;
    }

    /// <summary>Is there ONLINE auto-hunt time left on this account today? (Unlimited → always.)</summary>
    private bool HasIdleBudget(Entity p)
        => BudgetOf(p) is not { } b || AutoIdleCapSecondsFor(b) <= 0 || b.AutoTicksLeft > 0;

    /// <summary>Is there OFFLINE farming time left on this account today?</summary>
    private bool HasOfflineBudget(Entity p)
        => BudgetOf(p) is not { } b || AutoOfflineCapSecondsFor(b) <= 0 || b.OfflineTicksLeft > 0;

    /// <summary>Top every live balance back up to its current cap, as if midnight had just passed.
    /// Called ONLY from the two admin paths that change a cap (the Debug panel and /testcaps): with a
    /// daily balance, lowering the cap to 30s does nothing on its own — the 8h already in the tank is
    /// what the loop spends, so the tester would sit there for eight hours waiting for the "30s cap".
    /// It is deliberately NOT called when the debug config is LOADED at startup, or a server restart
    /// would refill every player's day.</summary>
    private void RefillAllBudgets()
    {
        foreach (var b in _world.AccountBudgets.Values)
        {
            b.AutoTicksLeft    = (long)Math.Max(0, AutoIdleCapSecondsFor(b)) * GameConstants.TickRate;
            b.OfflineTicksLeft = (long)Math.Max(0, AutoOfflineCapSecondsFor(b)) * GameConstants.TickRate;
            b.LastResetDate    = DateOnly.FromDateTime(DateTime.Now);
            b.Dirty            = true;
        }
        SaveDirtyBudgets();
    }

    // Offline sessions that hit their cap / died this tick — removed after the entity loop so we
    // never mutate _world.Entities while iterating it.
    private readonly List<Guid> _endOfflineQueue = new();

    // Link-dead grace windows that expired / died this tick — removed after the entity loop.
    private readonly List<Guid> _endGraceQueue = new();

    /// <summary>The player rearranged their bar — store the new layout and persist it. Deliberately
    /// NOT validated against LearnedSkills: an unknown or since-replaced id simply won't render, and
    /// rejecting the whole bar because one slot went stale would lose the player's layout. The server
    /// never casts from the bar (UseSkill carries the skill id), so a junk slot is inert.</summary>
    private void HandleSetSkillBar(SetSkillBarCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p))
            return;
        p.ActiveSkillBar = cmd.Slots ?? Array.Empty<string>();   // the bar belongs to the class you're playing
        // The tutorial's "put something on your bar" beat (63j). This command is the ONLY one the client
        // sends on a player edit — the server's own pushes never come back through here — so a bar that
        // arrives with anything in it is a bar the player just built.
        if (p.ActiveSkillBar.Any(s => !string.IsNullOrEmpty(s)))
            AdvanceActionQuests(p, QuestActions.AssignBar);
        SaveEntity(p);
    }

    private void HandleSetAutoHuntConfig(SetAutoHuntConfigCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p))
            return;
        var c = cmd.Config;
        // An exhausted daily allowance blocks turning it ON; every other setting still saves.
        bool haveBudget = HasIdleBudget(p);
        p.AutoHuntEnabled   = c.Enabled && haveBudget;
        if (c.Enabled && !haveBudget)
            SendSystemToEntity(p, "Your account's auto-hunt time for today is used up.");
        // 🔴 THE TUTORIAL'S AUTO-FARM BEAT IS CREDITED HERE, NOT ONLY IN HandleToggleAutoHunt (63j:
        // "Cannot complete the Auto-on part of the quest ... clicked auto-on on skills clicked
        //  auto-farm clicked offline...nothing works to allow me to continue"). The Auto button stopped
        // sending ToggleAutoHunt when it was changed to push the WHOLE config — the actions have to
        // travel with the enable or the autopilot just wanders — and ToggleAutoHuntAsync now has no
        // caller at all, so the only path that credited the step was dead code. The hub method is kept
        // for the protocol; both paths credit.
        // ⚠ Deliberately NOT gated on a false->true transition: re-saving the auto window with the
        // switch already on credits too, which is the gesture he actually described. Re-crediting is
        // free — AdvanceActionQuests only matches while that step is the current one.
        if (p.AutoHuntEnabled)
            AdvanceActionQuests(p, QuestActions.AutoHunt);
        p.AutoHpPotionPct   = Math.Clamp(c.HpPotionPct, 0, 100);
        p.AutoMpPotionPct   = Math.Clamp(c.MpPotionPct, 0, 100);
        p.AutoBuffPotions   = c.AutoBuffPotions;
        p.AutoSkills.Clear();
        foreach (var s in c.Skills ?? Array.Empty<AutoSkillDto>())
            p.AutoSkills.Add(new AutoSkillDto(s.SkillId, s.Enabled, Math.Max(0, s.ExtraDelayTicks)));
        WarnUncastableAutoSkills(p);
        p.AutoBuffPotionIds.Clear();
        foreach (var id in c.BuffPotionIds ?? Array.Empty<string>())
            p.AutoBuffPotionIds.Add(id);
        p.AutoHealPotions.Clear();
        foreach (var hp in c.HealPotions ?? Array.Empty<AutoPotionDto>())
            p.AutoHealPotions.Add(new AutoPotionDto(hp.ItemId, hp.Enabled, Math.Clamp(hp.ThresholdPct, 0, 100)));
        // null = "no opinion", NOT "clear it". The Buffs tab always sends all 17 families, armed or not,
        // so an absent array can only come from a caller that does not know the field exists — and the
        // cost of guessing wrong is a silently emptied tab. Turning every row off is still expressible:
        // that is 17 rows with both flags false, an array, not a null.
        if (c.Buffs is not null)
        {
            p.AutoBuffs.Clear();
            foreach (var b in c.Buffs)
                if (!string.IsNullOrEmpty(b.Family))
                    p.AutoBuffs.Add(b);
        }
        p.AutoFarmRange   = Math.Clamp(c.FarmRange, 200, 2000);
        p.AutoFarmStatic  = c.StaticSpot;
        p.AutoAttackNormal = c.AttackNormal;
        p.AutoAttackElite = c.AttackElite;
        p.AutoAttackBoss  = c.AttackBoss;
        p.AutoCyclic      = c.CyclicOrder;
        p.AutoHealPct     = Math.Clamp(c.HealThresholdPct, 0, 100);
        p.AutoMpPct       = Math.Clamp(c.MpThresholdPct, 0, 100);
        p.AutoAssistLeader = c.AssistPartyLeader;
        p.AutoReadyTick.Clear();
        Array.Clear(p.AutoChainCursor);   // the bar changed under the cursors; start the cycle over
        if (p.AutoHuntEnabled) { p.FarmCenterX = p.X; p.FarmCenterY = p.Y; }   // (re)anchor the static circle
        SendAutoHuntStatus(p);   // persisted by the normal autosave/logout snapshot
    }

    /// <summary>Say out loud which armed rows the chain is going to ignore.
    ///
    /// <para>🔑 This is the other half of the Provoke find (playtest 23: *"Provoke is not auto used in
    /// any form"*, and *"check the cyclic logic ...I feel there is a problem"*). A skill that classifies
    /// as <see cref="AutoSkillKind.Other"/> is skipped in silence — the row sits on the bar with its Auto
    /// mark lit and simply never fires, which from the outside is indistinguishable from a broken cycle.
    /// Provoke was the case that mattered and it is fixed; what stays in that bucket is the handful of
    /// skills that genuinely should not autopilot (a hide, a reveal, a trap, a resurrection), and the
    /// player is now told rather than left to time them.</para></summary>
    private void WarnUncastableAutoSkills(Entity p)
    {
        var ignored = p.AutoSkills
            .Where(s => s.Enabled && SkillCatalog.Get(s.SkillId) is SkillDef d && ClassifyAuto(d) == AutoSkillKind.Other)
            .Select(s => SkillCatalog.Get(s.SkillId)!.Name)
            .ToList();
        if (ignored.Count == 0) return;
        SendSystemToEntity(p, $"Auto-hunt cannot cast {string.Join(", ", ignored)} — " +
                              (ignored.Count == 1 ? "it is" : "they are") + " for you to press yourself.");
    }

    private void HandleToggleAutoHunt(ToggleAutoHuntCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p))
            return;
        if (cmd.Enabled && !HasIdleBudget(p))
        {
            SendSystemToEntity(p, "Your account's auto-hunt time for today is used up. It refills at midnight.");
            SendAutoHuntConfig(p);
            return;
        }
        p.AutoHuntEnabled = cmd.Enabled;
        if (p.AutoHuntEnabled)
        {
            p.FarmCenterX = p.X; p.FarmCenterY = p.Y;   // anchor the static circle here
            AdvanceActionQuests(p, QuestActions.AutoHunt);   // the tutorial's auto-farm beat (`58a`)
        }
        SendAutoHuntConfig(p);
        SendAutoHuntStatus(p);
        if (_world.Parties.TryGetValue(p.Id, out var pty))
            SendPartyUpdate(pty);   // toggle the party AFK/Auto icon promptly
        // Say the budget on every switch (32q). The button counts down too, but a chat line is what
        // you still have five minutes later when the button is off-screen behind a window.
        int left = AutoIdleSecondsLeft(p);
        SendSystemToEntity(p, p.AutoHuntEnabled
            ? left < 0 ? "Auto-hunt ON (no time limit)." : $"Auto-hunt ON — {HumanTime(left)} of idle time left."
            : left < 0 ? "Auto-hunt OFF." : $"Auto-hunt OFF — {HumanTime(left)} of idle time left.");
    }

    /// <summary>"2h 05m" / "5m 30s" / "42s" — the chat-line spelling of a budget. The client's own
    /// ShortTime does the compact button version; this one is meant to be read in a sentence.</summary>
    private static string HumanTime(int seconds)
    {
        if (seconds <= 0) return "0s";
        int h = seconds / 3600, m = seconds % 3600 / 60, s = seconds % 60;
        return h > 0 ? $"{h}h {m:00}m" : m > 0 ? $"{m}m {s:00}s" : $"{s}s";
    }

    /// <summary>Per-tick automation for a player (called before UpdateAction). Auto-potions always
    /// run; the hunt loop (target/engage/auto-skill) runs only when AutoHuntEnabled.</summary>
    private void AutoPilot(Entity p)
    {
        AutoPotions(p);

        if (!p.AutoHuntEnabled)
        {
            // F1 (playtest-18): switching to manual HANDS THE TARGET OVER, it does not cancel it.
            // Turning auto off used to push null, which closed the target window mid-fight and forced
            // a re-select to finish a mob that the server was in fact still swinging at. Engaged /
            // AttackCommandTargetId are deliberately left alone here — only the autopilot stops.
            // The window still clears the moment the target is actually gone (dead / out of the
            // dictionary), and the paths that genuinely end the fight (StopAutoHunt on death or a
            // spent budget) clear CombatTargetId themselves, so this pushes null for them.
            //
            // ⚠ playtest-20 #8 — "casting Stab drops your target for the cast duration, then it
            // returns". This line runs every tick in MANUAL play too, and starting a cast clears
            // CombatTargetId on purpose (see HandleUseSkill: casting cancels the auto-attack chase).
            // So the next tick pushed null, the client assigns AutoTarget straight to TargetId, and
            // the target frame vanished for the length of the cast — then came back because a
            // FIGHTER's offensive skill re-engages in AfterOffensiveSkill. On a mage it would simply
            // have stayed gone. A dropped CHASE is not a dropped SELECTION: while a cast or a queued
            // skill is in flight with no chase, push nothing and leave the player's own target alone.
            // The genuinely-ended-fight paths still push their null, because they are not casting.
            //
            // 🔴 playtest-21 `65d` — this pushed a NULL as well, and a null is a REVOCATION of a
            // selection the server never made. His case: attack A, manually tap B while A still
            // lives, A dies → CombatTargetId goes stale → this pushed null → the client assigned it
            // straight over B. *"the 1st dies and closes my second so i need to retarget."*
            // In manual play the server may HAND a target over; it may not take one away, because it
            // does not know what the player has selected — only the client does, and the client now
            // drops its own target when THAT target dies (his rule: `target == current target`).
            // Forgetting the sent id without sending keeps the dedupe honest, so re-acquiring the
            // same mob later still pushes.
            if (p.CombatTargetId is not null || (p.CastingSkillId is null && p.QueuedSkillId is null))
            {
                if (LiveTarget(p, p.CombatTargetId) is Guid live) PushAutoTarget(p, live);
                else p.SentAutoTargetId = null;
            }
            return;
        }
        if (p.CastingSkillId is not null || p.QueuedSkillId is not null)
            return;   // let an in-progress cast/queue resolve

        // Counter-attack: if a player just hit us and counter-attack is on, retaliate — unless we're
        // about to finish a nearly-dead mob (owner-delegated heuristic: <25% HP = finish it first).
        if (p.CounterAttack && CounterTarget(p) is Entity foe)
        {
            bool finishMob = p.CombatTargetId is Guid ct &&
                _world.Entities.TryGetValue(ct, out var cm) && cm.Kind == EntityKind.Mob &&
                !cm.Dead && cm.MaxHp > 0 && (float)cm.Hp / cm.MaxHp < 0.25f;
            if (!finishMob)
            {
                p.CombatTargetId = foe.Id;
                PushAutoTarget(p, foe.Id);
                p.Engaged = AutoBasicAttackEnabled(p);
                p.AttackCommandTargetId = p.Engaged ? foe.Id : null;
                if (TryAutoSkill(p, foe))
                    return;
                return;
            }
        }

        // ASSIST MODE (owner, playtest-15): "you only assist — if the party leader has no target you
        // wait; don't choose on your own." So it replaces acquisition AND retaliation AND roaming: an
        // assisting alt that wanders off after whatever hit it is not assisting. No leader target =
        // stand still with nothing selected.
        var target = AutoAssistTarget(p);
        if (target is null && AutoAssisting(p))
        {
            p.Engaged = false;
            p.CombatTargetId = null;
            p.AttackCommandTargetId = null;
            PushAutoTarget(p, null);
            // Still run the chain with NO target: heals and buffs need none, which is exactly the
            // between-pulls moment a healing/buffing alt is for. Attacks and debuffs require a target
            // and skip themselves, and roaming is deliberately not reached.
            TryAutoSkill(p, null);
            return;
        }

        // RETALIATION FIRST (owner, playtest-15): something already swinging at you outranks whatever
        // merely happens to be nearest. He was being shot by orc archers while the autopilot calmly
        // worked through the closest mobs. See RetaliationTarget for the two guards that keep it from
        // thrashing (finish a nearly-dead mob; stay on an attacker you are already fighting).
        target ??= RetaliationTarget(p) ?? ValidAutoTarget(p) ?? AcquireAutoTarget(p);
        bool basic = AutoBasicAttackEnabled(p);
        if (target is not null)
        {
            p.CombatTargetId = target.Id;
            p.Engaged = basic;   // only auto BASIC-attack if the Basic Attack row is enabled
            // While the autopilot drives, IT owns the melee order: enabling the Basic Attack row is
            // the command, and with the row off there is no standing order for a skill to resume.
            p.AttackCommandTargetId = basic ? target.Id : null;
        }
        else
        {
            p.Engaged = false;
            p.CombatTargetId = null;
            p.AttackCommandTargetId = null;
        }
        PushAutoTarget(p, p.CombatTargetId);

        // A queued skill (buffs/heals need no target; attack/debuff do) is cast+chased by UpdateAction.
        if (TryAutoSkill(p, target))
            return;

        if (target is not null)
        {
            // Have a target but nothing to cast this tick. Basic on → UpdateAutoAttack (via Engaged)
            // handles the approach and the swing.
            //
            // Basic OFF (a mage, an archer) → STAND STILL. This used to walk you onto the target "so a
            // skill can land when ready", which is why the owner's mage ran into melee range and then
            // just stood on top of the mob between casts (playtest-15). It was never needed: a queued
            // skill does its own approach — UpdateQueuedSkill walks only as far as CAST range — so the
            // walk here did nothing except close a distance the caster wanted to keep.
            return;
        }

        // No target in the farm circle → roam (or return to the static centre).
        AutoRoam(p);
    }

    /// <summary>The player who recently attacked us and is a valid PvP retaliation target (alive,
    /// in range, out of town, self-defense window active), or null.</summary>
    private Entity? CounterTarget(Entity p)
    {
        if (p.LastPvpAttackerId is not Guid aid || _tick >= p.PvpFlagUntilTick)
            return null;
        if (!_world.Entities.TryGetValue(aid, out var a) || a.Kind != EntityKind.Player)
            return null;
        if (!CanPvpHit(p, a))
            return null;
        return DistanceSq(p, a) <= GameConstants.ViewRange * GameConstants.ViewRange ? a : null;
    }

    /// <summary>Tell the client what the autopilot is on, but only when it CHANGES — the loop runs
    /// 10x/s and the target usually does not move between kills.</summary>
    /// <summary>The id back, but only while it still names something alive and in the world — otherwise
    /// null. Used by the manual hand-over in <see cref="AutoPilot"/>: keeping a target is only correct
    /// as long as there IS one.</summary>
    private Guid? LiveTarget(Entity p, Guid? id) =>
        id is Guid g && _world.Entities.TryGetValue(g, out var t) && !t.Dead && t.Id != p.Id ? id : null;

    private void PushAutoTarget(Entity p, Guid? targetId)
    {
        if (p.SentAutoTargetId == targetId)
            return;
        p.SentAutoTargetId = targetId;
        SendTo(p, "AutoTarget", new AutoTargetUpdate(targetId));
    }

    /// <summary>A mob that is ATTACKING us — preferred over whatever is merely nearest (owner,
    /// playtest-15: "a mob hitting you is higher priority than nearest").
    ///
    /// Two guards stop this from making the autopilot indecisive:
    ///  1. A nearly-dead current target (&lt;25% HP) is FINISHED first. Swapping off a mob about to die
    ///     wastes the damage already spent — the same heuristic the PvP counter-attack already used.
    ///  2. If the mob we are already on is itself attacking us, keep it. Otherwise two attackers at
    ///     similar range would swap the target back and forth every tick and neither would die.
    /// Searched over the farm circle plus the chase margin, so retaliation cannot drag the autopilot
    /// out of its farm area.</summary>
    private Entity? RetaliationTarget(Entity p)
    {
        Entity? cur = null;
        if (p.CombatTargetId is Guid ct && _world.Entities.TryGetValue(ct, out var c) &&
            c.Kind == EntityKind.Mob && !c.Dead)
            cur = c;

        if (cur is not null && cur.MaxHp > 0 && (float)cur.Hp / cur.MaxHp < 0.25f)
            return null;                                  // finish it
        if (cur is not null && cur.CombatTargetId == p.Id)
            return cur;                                   // already trading with an attacker

        var (cx, cy) = FarmCenter(p);
        float margin = p.AutoFarmRange + AutoChaseMargin;
        Entity? best = null;
        float bestSq = float.MaxValue;
        foreach (var e in _world.Grid.Nearby(p))
        {
            if (e.Kind != EntityKind.Mob || e.Dead || e.TrainingDummy) continue;
            if (e.CombatTargetId != p.Id) continue;       // only things actually on us
            if (GameConstants.InSafeZone(e.X, e.Y) || !CanAttackRank(p, e)) continue;
            float ecx = e.X - cx, ecy = e.Y - cy;
            if (ecx * ecx + ecy * ecy > margin * margin) continue;
            float d = DistanceSq(p, e);
            if (d < bestSq) { bestSq = d; best = e; }
        }
        return best;
    }

    /// <summary>Is this character actually in ASSIST mode right now? The toggle only means something
    /// inside a party you don't lead — the leader assisting himself would simply never fight.</summary>
    private bool AutoAssisting(Entity p) =>
        p.AutoAssistLeader && _world.Parties.TryGetValue(p.Id, out var party) && party.LeaderId != p.Id;

    /// <summary>What the party leader is on, if that is something we may hit: alive, a mob whose rank
    /// the config allows (or a legal PvP target), and inside the farm circle plus the chase margin so
    /// assisting cannot tow the alt across the map. Null when not assisting or the leader is idle.</summary>
    private Entity? AutoAssistTarget(Entity p)
    {
        if (!p.AutoAssistLeader || !_world.Parties.TryGetValue(p.Id, out var party) || party.LeaderId == p.Id)
            return null;
        if (!_world.Entities.TryGetValue(party.LeaderId, out var leader) || leader.Dead)
            return null;
        if (leader.CombatTargetId is not Guid tid || !_world.Entities.TryGetValue(tid, out var t) || t.Dead)
            return null;
        if (t.Id == p.Id) return null;                                   // the leader is targeting US
        if (t.Kind == EntityKind.Mob)
        {
            if (t.TrainingDummy || GameConstants.InSafeZone(t.X, t.Y) || !CanAttackRank(p, t)) return null;
        }
        else if (t.Kind != EntityKind.Player || !CanPvpHit(p, t))
        {
            return null;
        }
        float margin = p.AutoFarmRange + AutoChaseMargin;
        return DistanceSq(p, t) <= margin * margin ? t : null;
    }

    /// <summary>"Basic Attack" opted into the auto-skill list — the auto-hunt may melee.</summary>
    private static bool AutoBasicAttackEnabled(Entity p) =>
        p.AutoSkills.Any(s => s.Enabled && s.SkillId == AutoHuntIds.BasicAttack);

    /// <summary>Whether the config permits engaging this mob's rank (mobs / elites / bosses).</summary>
    private static bool CanAttackRank(Entity p, Entity mob) => mob.Rank switch
    {
        MobRank.Boss  => p.AutoAttackBoss,
        MobRank.Elite => p.AutoAttackElite,
        _             => p.AutoAttackNormal,
    };

    /// <summary>Move when there's nothing to fight: static spot → walk back to the centre; roam →
    /// wander to a fresh random point within the farm range (re-scanning as it goes).</summary>
    private void AutoRoam(Entity p)
    {
        if (p.AutoFarmStatic)
        {
            float dx = p.FarmCenterX - p.X, dy = p.FarmCenterY - p.Y;
            if (dx * dx + dy * dy > AutoReturnEpsilon * AutoReturnEpsilon)
            {
                p.TargetX = p.FarmCenterX;
                p.TargetY = p.FarmCenterY;
            }
            else { p.TargetX = null; p.TargetY = null; }
            return;
        }
        // Roam: wander to a fresh point WITHIN the farm circle around home (the scan itself follows
        // the character via FarmCenter). Bounded so a roamer doesn't drift across the map, and skips
        // safe zones / roads so it doesn't idle in a town.
        if (p.TargetX is null)
        {
            for (int i = 0; i < 8; i++)
            {
                double ang = _rng.NextDouble() * Math.PI * 2;
                float dist = p.AutoFarmRange * (float)Math.Sqrt(_rng.NextDouble());   // uniform in the disc
                var (rx, ry) = WorldMap.ClampToBorder(
                    p.FarmCenterX + (float)(Math.Cos(ang) * dist),
                    p.FarmCenterY + (float)(Math.Sin(ang) * dist));
                if (!GameConstants.InSafeZone(rx, ry) && !WorldMap.OnRoad(rx, ry))
                {
                    p.TargetX = rx;
                    p.TargetY = ry;
                    break;
                }
            }
        }
    }

    private void AutoPotions(Entity p)
    {
        // Per-potion cooldowns are enforced inside UsePotion now, so no shared pre-gate here.
        if (p.MaxHp > 0 && p.AutoHealPotions.Count > 0)
        {
            // Potions-tab mode: try each ARMED potion from the highest threshold down. The first one
            // that's ready (UsePotion gates cooldown + tier-suppression) drinks and we stop — so
            // common@80 / uncommon@70 / rare@50 behave as fallbacks.
            int hpPctNow = (int)(p.Hp * 100L / p.MaxHp);
            foreach (var line in p.AutoHealPotions.Where(l => l.Enabled).OrderByDescending(l => l.ThresholdPct))
            {
                if (hpPctNow >= line.ThresholdPct) continue;
                if (p.Inventory.FirstOrDefault(i => i.DefId == line.ItemId && !i.Equipped) is InventoryItem pot
                    && UsePotion(p, pot))
                    break;
            }
        }
        else if (p.AutoHpPotionPct > 0 && p.MaxHp > 0 &&
                 p.Hp * 100 < p.MaxHp * p.AutoHpPotionPct &&
                 BestHealPotion(p) is InventoryItem hpPot)
            UsePotion(p, hpPot);

        // MP potions don't exist as items yet — reserved plumbing (BestManaPotion returns null).
        if (p.AutoMpPotionPct > 0 && p.MaxMp > 0 &&
            p.Mp * 100 < p.MaxMp * p.AutoMpPotionPct &&
            BestManaPotion(p) is InventoryItem mpPot)
            UsePotion(p, mpPot);

        // The BUFFS tab (BL-04) takes over the moment it has been configured. It is per-FAMILY, which
        // the old per-ITEM list could not be, so it is a replacement rather than a filter on top.
        if (p.AutoBuffs.Count > 0)
        {
            AutoBuffFamilies(p);
            return;
        }

        // Buff potions: keep the configured ones up — or, if none are listed, every buff potion in
        // the bag (a "keep all buffs up" convenience). Iterate a snapshot since UsePotion mutates.
        if (p.AutoBuffPotions)
        {
            bool all = p.AutoBuffPotionIds.Count == 0;
            foreach (var item in p.Inventory.ToList())
            {
                if (ItemCatalog.Get(item.DefId) is not ItemDef d || !ItemCatalog.IsBuffPotion(d) ||
                    SkillCatalog.Get(d.UseSkillId) is not SkillDef bd)
                    continue;
                if (!all && !p.AutoBuffPotionIds.Contains(item.DefId))
                    continue;
                // A BURST potion (Dash: 15s up, 1 min reuse) is for the player's finger, not the
                // autopilot — on "keep every buff up" it would drink the whole stack, a bottle a
                // minute, for fifteen seconds of speed each time.
                if (bd.DurationTicks > 0 && bd.DurationTicks < 600)
                    continue;
                // "Already up" must be asked of the potion's CHILDREN — a buff potion applies no buff
                // under its own key any more, so the old test never matched and the autopilot would
                // drink the entire stack, one bottle per cycle. It is also the right question: if a
                // stronger blessing already covers that family, the potion would be refused anyway.
                if (BuffAlreadyUp(p, bd, 1))
                    continue;
                UsePotion(p, item);
            }
        }
    }

    /// <summary>
    /// Keep the armed buff FAMILIES up (BL-04). One line per family, and inside a family the owner's
    /// order: <i>"rarity first, then scroll &gt; potion — uncommon scroll → uncommon potion → common
    /// scroll → common potion"</i>, capped at the rarity the line allows.
    ///
    /// <para>The walk STOPS at the first candidate whose buff is already up, rather than carrying on
    /// down the list. That is the whole safety property: without it, a character holding a Rare scroll's
    /// blessing would fall through to the Uncommon potion, which <c>ApplyBuff</c> would refuse — but
    /// only after the bottle was gone. The loop must never reach a rung it cannot improve on.</para>
    ///
    /// <para>Everything the tab can arm is a real item in the bag; a family with nothing to spend
    /// simply does nothing, silently, which is what an idle farm wants.</para>
    /// </summary>
    private void AutoBuffFamilies(Entity p)
    {
        foreach (var line in p.AutoBuffs)
        {
            if (!line.Potion && !line.Scroll) continue;

            foreach (var candidate in BuffConsumables.PickOrder(line.Family, line.Potion, line.Scroll,
                                                               line.MaxRarity))
            {
                if (ItemCatalog.Get(candidate.ItemId) is not ItemDef d
                    || SkillCatalog.Get(d.UseSkillId) is not SkillDef wrapper)
                    continue;

                // Asked BEFORE the bag, not after: whether the family is covered has nothing to do with
                // what you are carrying, and asking it first means an already-buffed character costs one
                // cheap Buffs scan per tick instead of an inventory walk per rung.
                if (BuffAlreadyUp(p, wrapper, 1)) break;

                if (p.Inventory.FirstOrDefault(i => i.DefId == candidate.ItemId && !i.Equipped)
                        is InventoryItem item && UsePotion(p, item))
                    break;
            }
        }
    }

    /// <summary>Highest-rarity HP potion in the bag (heal-over-time or instant), or null.</summary>
    private static InventoryItem? BestHealPotion(Entity p)
    {
        InventoryItem? best = null; int bestScore = -1;
        foreach (var it in p.Inventory)
        {
            if (ItemCatalog.Get(it.DefId) is not ItemDef d) continue;
            if (!ItemCatalog.IsHealPotion(d)) continue;
            int score = (int)d.Rarity;
            if (score > bestScore) { bestScore = score; best = it; }
        }
        return best;
    }

    /// <summary>Mana potions aren't in the catalog yet — reserved for when they are.</summary>
    private static InventoryItem? BestManaPotion(Entity p) => null;

    /// <summary>The farm-circle centre: the character in roam mode, the fixed start point in static.</summary>
    private static (float X, float Y) FarmCenter(Entity p) =>
        p.AutoFarmStatic ? (p.FarmCenterX, p.FarmCenterY) : (p.X, p.Y);

    /// <summary>The current combat target if it's still a valid auto-hunt victim. Kept within the
    /// farm circle PLUS a soft chase margin (so a kited mob can be chased a bit outside).</summary>
    private Entity? ValidAutoTarget(Entity p)
    {
        if (p.CombatTargetId is Guid tid && _world.Entities.TryGetValue(tid, out var t) &&
            t.Kind == EntityKind.Mob && !t.Dead && !t.TrainingDummy &&
            !GameConstants.InSafeZone(t.X, t.Y) && CanAttackRank(p, t))
        {
            var (cx, cy) = FarmCenter(p);
            float margin = p.AutoFarmRange + AutoChaseMargin;
            float dx = t.X - cx, dy = t.Y - cy;
            if (dx * dx + dy * dy <= margin * margin)
                return t;
        }
        return null;
    }

    /// <summary>Nearest attackable mob INSIDE the farm circle (centre = char in roam / start in
    /// static), matching the rank filter; skips dummies, dead, safe-zone.</summary>
    private Entity? AcquireAutoTarget(Entity p)
    {
        var (cx, cy) = FarmCenter(p);
        float rangeSq = (float)p.AutoFarmRange * p.AutoFarmRange;
        Entity? best = null;
        float bestSq = float.MaxValue;
        foreach (var e in _world.Grid.Nearby(p))
        {
            if (e.Kind != EntityKind.Mob || e.Dead || e.TrainingDummy) continue;
            if (GameConstants.InSafeZone(e.X, e.Y) || !CanAttackRank(p, e)) continue;
            float ecx = e.X - cx, ecy = e.Y - cy;
            if (ecx * ecx + ecy * ecy > rangeSq) continue;   // inside the farm circle only
            float d = DistanceSq(p, e);                       // pick the closest to the character
            if (d < bestSq) { bestSq = d; best = e; }
        }
        return best;
    }

    // ⚠ AutoChainCursor is sized to this enum — adding a member means widening that array too.
    private enum AutoSkillKind { Attack, Debuff, Buff, Heal, MpHeal, Taunt, Other }

    private static AutoSkillKind ClassifyAuto(SkillDef def)
    {
        var e = def.Effect;
        // 🔴 DECLARED MANUAL beats every rule below it (owner 2026-08-19, on Mana Ray: *"its a strategy
        // move - depleate boss/enemy mp not a farming tool"*). It has to be FIRST: Mana Ray carries
        // MagicDamage and would be claimed by the attack test two lines down, and Mana Strain is a
        // debuff that the debuff test would claim just as happily. `Other` is the never-cast bucket, and
        // WarnUncastableAutoSkills already tells the player it is theirs to press — so this is an
        // exclusion that explains itself instead of one that looks like a bug.
        if (def.NeverAuto) return AutoSkillKind.Other;
        // 🔑 A LIFESTEAL attack is a HEAL and NOTHING ELSE (him, 2026-08-13): *"I want it only with a
        // treshold .. if I want it permanent ill do cycle or 100% treshold"*. Checked BEFORE the damage
        // test, which would otherwise claim it for the attack chain. Vampiric Bolt is the only skill in
        // the game with Lifesteal, so this is the whole of `BL-67` part 1.
        //
        // It briefly had TWO homes — heal group when hurt, attack chain otherwise — so a nuker would not
        // lose it from his rotation. He ruled that out: the threshold IS the control, and a 100% one (or
        // a cyclic chain) is how you ask for it permanently. A skill that fires from two different gates
        // cannot be reasoned about from the settings screen, which is the whole complaint behind BL-67.
        if (def.Lifesteal > 0f) return AutoSkillKind.Heal;
        if ((e & (SkillEffect.PhysicalDamage | SkillEffect.MagicDamage)) != 0) return AutoSkillKind.Attack;
        // 🔴 A PURE TAUNT IS ITS OWN RUNG (playtest 23: *"Provoke is not auto used in any form"*). It had
        // no rung at all and fell through to `Other`, the never-cast bucket — so no taunt in the game had
        // ever fired from the chain, which makes an auto-farming tank a damage dealer with less damage.
        // 🔑 Why it landed there: Provoke carries `SkillEffect.Taunt` and `SkillCategory.Debuff`, and the
        // debuff test below asks for ContestCc or a DebuffSchool. A taunt is neither — it is not contested
        // and it has no school to resist — so every branch missed it.
        // Tested AFTER damage on purpose: a taunt that also hits (a Shield Bash shape) belongs in the
        // attack rotation, where its damage is the reason to press it; its taunt lands either way.
        if (def.TauntPower > 0) return AutoSkillKind.Taunt;
        if ((e & SkillEffect.Heal) != 0) return AutoSkillKind.Heal;
        // An MP-restore is its OWN priority group (BL-67), sitting directly under Heal: him,
        // *"below the Heal as priority but above all other (need mp to cast/buff)"*. It used to share
        // the Heal group and be told apart inside it by IsManaRestore, which worked but meant one
        // threshold armed two different resources — the reason Restore Spirit looked dead at a 100%
        // heal threshold. Before THAT it fell through to Other, the never-auto-cast bucket.
        if (IsManaRestore(def)) return AutoSkillKind.MpHeal;
        if (def.Category == SkillCategory.Buff || (e & SkillEffect.AnyBuff) != 0) return AutoSkillKind.Buff;
        if ((e & SkillEffect.ContestCc) != 0 || def.DebuffSchool != DebuffSchool.None) return AutoSkillKind.Debuff;
        return AutoSkillKind.Other;
    }


    /// <summary>Is this buff already running on the entity, so the autopilot should skip it?
    ///
    /// A ONE-CHILD wrapper (a potion, a scroll, a buffer's single blessing) puts up no buff under its
    /// own key — the CHILD's family key is what lands — so testing the wrapper's key would never match
    /// and the autopilot would re-queue it every cycle: MP drained, the party re-stamped, an offline
    /// buffer dry in minutes. A GROUP does land under its own key, so that one tests directly.</summary>
    private static bool BuffAlreadyUp(Entity p, SkillDef def, int level)
    {
        // BuffPlan resolves a wrapper to its child, so this asks about the buff that actually lands.
        // A group covering the family counts too: don't drink a Might potion under Might and Bulwark.
        var (key, rank, _, _) = BuffPlan(def, level);
        return p.Buffs.Any(b => (b.Key == key || b.CoveredKeys.Contains(key)) && b.Rank >= rank);
    }

    /// <summary>How little time may be left on an auto-buff before the chain renews it (owner: "below
    /// 60s"). Capped at HALF the buff's own duration, because a two-minute buff would otherwise spend
    /// half its life "about to expire" and a 30s one would never be fresh at all.</summary>
    private const int AutoBuffRefreshTicks = 600;   // 60s at 10 ticks/s

    /// <summary>Does the chain consider this buff already taken care of? Yes when a STRICTLY STRONGER
    /// buff of the family is up (recasting under it would just be refused by ApplyBuff and burn the MP),
    /// or when our own rank is up with more than the refresh window left.</summary>
    private static bool AutoBuffCovered(Entity e, SkillDef def, int window, int rank)
    {
        string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        foreach (var b in e.Buffs)
        {
            // A group covering this family counts as covering the single too — recasting the single
            // under an improved buff would just be refused, and the autopilot must not keep trying.
            if (b.Key != key && !b.CoveredKeys.Contains(key)) continue;
            if (b.Rank > rank) return true;                          // something better is running
            if (b.Rank == rank) return b.Toggle || b.TicksRemaining > window;
        }
        return false;                                                // absent, or only a weaker one
    }

    /// <summary>The auto-chain's version of <see cref="BuffAlreadyUp"/>: same group-aware walk, but a
    /// buff also counts as missing when it is about to run out (owner: "not active / below 60s / a
    /// lesser effect"). Kept separate from BuffAlreadyUp, which the auto-POTION faucet uses — renewing
    /// a bottle a minute early is a different (and more expensive) proposition than recasting a skill.</summary>
    private static bool AutoBuffUpToDate(Entity p, SkillDef def, int level)
    {
        // One child = the wrapper hands out that family's rung; ask about the CHILD.
        if (def.ChildBuffsAt(level) is { Length: 1 } one
            && SkillCatalog.Get(one[0]) is SkillDef child)
            return AutoBuffCovered(p, child, RefreshWindow(child), child.Rank);
        // A group lands under its own key at GROUP rank, which is what a renewal has to beat.
        bool isGroup = def.ChildBuffsAt(level) is { Length: > 1 };
        return AutoBuffCovered(p, def, RefreshWindow(def), isGroup ? GroupRank(level) : def.Rank);
    }

    private static int RefreshWindow(SkillDef def) =>
        def.DurationTicks > 0 ? Math.Min(AutoBuffRefreshTicks, def.DurationTicks / 2) : 0;

    /// <summary>Queue the next auto-skill. Priority is by GROUP (owner, playtest-15):
    /// <b>heals → buffs/debuffs → attacks</b>, and only the first group with something to cast gets to
    /// act this tick. Within a group the order is the bar order and
    /// <see cref="Entity.AutoCyclic"/> decides where the scan starts. Returns true if one was queued.</summary>
    private bool TryAutoSkill(Entity p, Entity? target)
    {
        // BL-67: MpHeal is its own rung between Heal and everything else — *"below the Heal as priority
        // but above all other (need mp to cast/buff)"*. Each rung is now armed by its OWN resource, so
        // an HP threshold can no longer decide whether a mana restore is allowed to run.
        if (AutoHealWanted(p) && TryAutoChain(p, target, AutoSkillKind.Heal)) return true;
        if (AutoManaWanted(p) && TryAutoChain(p, target, AutoSkillKind.MpHeal)) return true;
        if (TryAutoChain(p, target, AutoSkillKind.Buff)) return true;
        if (TryAutoChain(p, target, AutoSkillKind.Debuff)) return true;
        // A TAUNT outranks the damage rotation (playtest 23). It has to: the whole point of a taunt is
        // that it lands BEFORE the party out-damages you, and a tank's attack chain is never idle, so a
        // rung below Attack would only ever fire on a tick where nothing else could — which for a tank
        // is no tick at all.
        if (TryAutoChain(p, target, AutoSkillKind.Taunt)) return true;
        return TryAutoChain(p, target, AutoSkillKind.Attack);
    }

    /// <summary>Is the heal chain armed? Below the threshold, or at a threshold of 100 — a dedicated
    /// healer, which the owner's spec makes the ONE piece of auto-support an alt is allowed to give
    /// ("if the healer sets his threshold to 100% he always heals on cooldown"). 0 = never.</summary>
    private static bool AutoHealWanted(Entity p) =>
        p.AutoHealPct > 0 && (p.AutoHealPct >= 100 || (p.MaxHp > 0 && p.Hp * 100f / p.MaxHp < p.AutoHealPct));

    /// <summary>The HP floor a mana restore must stay above, because the restore is PAID IN HP —
    /// unguarded, the autopilot would burn a mage to the 1-HP floor to buy mana it then dies holding.
    ///
    /// <para>🔴 This WAS a flat 60, and that is the bug he reported: *"Restore Spirit → now it doesnt
    /// work anyway ... nor as cyclic nor as 100% hp treshold"*. His own worked case is
    /// *"50% MP_treshold + 30% HP_treshold ... Restore Spirit to be used (MP &lt;= 50%) and if it lowers
    /// me (HP &lt;= 30%) to use the Vampiric Bolt to heal me"* — he is deliberately spending HP down to
    /// his heal threshold, and a hardcoded 60 stopped that dead at 60% with no way to see why.</para>
    ///
    /// <para>So the floor is now HIS heal threshold: the two chains hand off to each other at exactly the
    /// line he set. Clamped [15, 60] at both ends for the cases the threshold cannot express — 0 ("never
    /// heal") must not become "spend all your HP", and 100 ("heal on cooldown") must not become "never
    /// restore mana below full HP", which would lock a healer out of his own mana chain.</para></summary>
    private static int AutoManaMinHpPct(Entity p) => Math.Clamp(p.AutoHealPct, 15, 60);

    /// <summary>Is the MpHeal chain armed? Independent of the heal slider — the mage the owner farmed
    /// with was at FULL HP and empty MP, which is exactly the state an HP threshold cannot see.
    /// The MP line is <see cref="Entity.AutoMpPct"/> (0 = never); it was a hardcoded 60 until BL-67.</summary>
    private static bool AutoManaWanted(Entity p) =>
        p.AutoMpPct > 0
        && p.MaxMp > 0 && p.Mp * 100f / p.MaxMp < p.AutoMpPct
        && p.MaxHp > 0 && p.Hp * 100f / p.MaxHp >= AutoManaMinHpPct(p);

    /// <summary>One priority group's turn: walk the auto-skill list from the group's cursor (cyclic) or
    /// from the top (first-available) and queue the first entry that can fire.
    ///
    /// CYCLIC wraps rather than waits. A strict "never go back to 1 until the last one has been used"
    /// would park the character doing nothing while a long-reuse skill recharges; wrapping only AFTER
    /// the rest of the group has been offered its turn keeps the 1-2-3-4-1 shape the owner asked for and
    /// still degrades to "cast what you can" when the tail is on cooldown.</summary>
    private bool TryAutoChain(Entity p, Entity? target, AutoSkillKind kind)
    {
        int n = p.AutoSkills.Count;
        if (n == 0) return false;
        int start = p.AutoCyclic ? ((p.AutoChainCursor[(int)kind] % n) + n) % n : 0;

        for (int k = 0; k < n; k++)
        {
            int i = (start + k) % n;
            var entry = p.AutoSkills[i];
            if (!entry.Enabled) continue;
            if (SkillCatalog.Get(entry.SkillId) is not SkillDef def || ClassifyAuto(def) != kind) continue;
            if (!p.HasSkill(def.Id)) continue;
            if (p.SkillCooldowns.ContainsKey(def.Id)) continue;
            if (_tick < p.AutoReadyTick.GetValueOrDefault(def.Id)) continue;
            // 🔴 THE AUTOPILOT MUST HONOUR THE SAME GATES AS A TAP (him, 2026-08-12): *"in auto farm the
            // char uses stab and strike with blunt or knives .. while in manual use it's declined because
            // of required weapon"*. HandleUseSkill checks these; this chain only ever checked cooldown
            // and cost, so auto-farm was casting a dual-only blow off a mace. The gate belongs HERE
            // rather than downstream: an unusable entry has to be SKIPPED so the cursor moves on to a
            // skill that can fire, not merely refused and the turn wasted.
            if (def.RequiredWeapon != WeaponType.None && (def.RequiredWeapon & p.WeaponType) == 0) continue;
            if (def.RequireHpBelowFraction > 0f && p.Hp > p.MaxHp * def.RequireHpBelowFraction) continue;

            int lvl = Math.Max(1, p.SkillLevelOf(def.Id));
            if (p.Mp < EffectiveMpCost(p, def, lvl)) continue;
            // A skill PAID IN HP (Restore Spirit) must keep a margin — the cost floors at 1 HP, so
            // an unguarded autopilot would trade a mage's whole bar away one cast at a time.
            if (def.HpCostAt(lvl) > 0 && p.Hp <= def.HpCostAt(lvl) * 2) continue;

            Guid tgtId;
            switch (kind)
            {
                case AutoSkillKind.Buff:
                    if (AutoBuffUpToDate(p, def, lvl)) continue;
                    tgtId = p.Id; break;
                case AutoSkillKind.Heal:
                    // A LIFESTEAL nuke heals by DEALING DAMAGE, so it wants the ENEMY here, not an ally.
                    // 🔑 This is also why the marker is `Lifesteal` and not the SkillEffect.Heal flag:
                    // that flag routes a skill through the heal pipeline, which lands on the skill's
                    // TARGET — and this target is the mob, so it would have healed what it was shooting.
                    if (def.Lifesteal > 0f)
                    {
                        if (target is null) continue;
                        tgtId = target.Id; break;
                    }
                    if (AutoHealTarget(p, def) is not Entity ht) continue;
                    tgtId = ht.Id; break;
                case AutoSkillKind.MpHeal:
                    if (AutoManaTarget(p, def) is not Entity mt) continue;
                    tgtId = mt.Id; break;
                case AutoSkillKind.Debuff:
                    // Missing or WEAKER on the enemy (owner) — the old test was "any buff with this
                    // key", which let a rank-1 poison block the rank-3 one for its whole duration.
                    if (target is null || AutoBuffCovered(target, def, 0, def.Rank)) continue;
                    tgtId = target.Id; break;
                case AutoSkillKind.Taunt:
                    // Mob-only, exactly like a manual Provoke, and pointless on one already locked to
                    // you by an unexpired taunt — re-taunting inside your own commit window burns the
                    // reuse and buys no cushion. Anything else (someone else's mob, a mob merely
                    // attacking you) is fair game: the cushion is what a tank is renewing.
                    if (target is null || target.Kind != EntityKind.Mob) continue;
                    if (target.TauntLockTicks > 0 && target.CombatTargetId == p.Id) continue;
                    tgtId = target.Id; break;
                case AutoSkillKind.Attack:
                    if (target is null) continue;
                    tgtId = target.Id; break;
                default:
                    continue;   // Other → never auto-cast
            }

            p.QueuedSkillId = def.Id;
            p.QueuedTargetId = tgtId;
            p.AutoReadyTick[def.Id] = _tick + AutoCycleTicks(p, def, entry.ExtraDelayTicks);
            p.AutoChainCursor[(int)kind] = (i + 1) % n;
            return true;
        }
        return false;
    }

    /// <summary>An MP-restore rather than an HP heal — a skill that restores mana and does NOT also
    /// heal (a hybrid is treated as a heal, because HP is the resource you die without).</summary>
    private static bool IsManaRestore(SkillDef def) =>
        (def.Effect & SkillEffect.RestoreMp) != 0 && (def.Effect & SkillEffect.Heal) == 0;

    /// <summary>Who this MP-restore should land on: the emptiest party member (or yourself) under the
    /// mana threshold and in range. Mirrors AutoHealTarget on the OTHER bar, and honours the same
    /// "not on a mana-restorer" rule the manual cast enforces, so the autopilot never queues a cast
    /// the command handler is going to refuse.</summary>
    private Entity? AutoManaTarget(Entity p, SkillDef def)
    {
        Entity? best = null;
        float bestPct = float.MaxValue;
        bool Wants(Entity e) => e.MaxMp > 0 && e.Mp * 100f / e.MaxMp < p.AutoMpPct
                             && !e.HasSkill(SkillCatalog.RestoreMana);

        // The HP price is the caster's, so the caster's own HP gates every target, not just self.
        if (p.AutoMpPct <= 0) return null;
        if (p.MaxHp <= 0 || p.Hp * 100f / p.MaxHp < AutoManaMinHpPct(p)) return null;

        if (Wants(p)) { best = p; bestPct = p.Mp * 100f / p.MaxMp; }

        if (IsAllyTargetable(def) && def.TargetMode != TargetMode.SelfOnly
            && _world.Parties.TryGetValue(p.Id, out var party))
        {
            float range = SkillMath.EffectiveRange(def, p.Archetype, p.BasicAttackRange, p.Level, p.SkillLevelOf(def.Id));
            foreach (var id in party.Members)
            {
                if (id == p.Id) continue;
                if (!_world.Entities.TryGetValue(id, out var m) || m.Dead || !Wants(m)) continue;
                if (m.Hidden) continue;   // hidden = not here (BL-69)
                if (DistanceSq(p, m) > range * range) continue;
                float pct = m.Mp * 100f / m.MaxMp;
                if (pct < bestPct) { bestPct = pct; best = m; }
            }
        }
        return best;
    }

    /// <summary>Who this heal should land on: the most injured party member under the threshold and in
    /// range, else yourself. A heal that cannot reach anybody who needs it returns null so the chain
    /// falls through to buffs/attacks instead of stalling on it.</summary>
    private Entity? AutoHealTarget(Entity p, SkillDef def)
    {
        Entity? best = null;
        float bestPct = float.MaxValue;
        bool Wants(Entity e) => e.MaxHp > 0 && (p.AutoHealPct >= 100 || e.Hp * 100f / e.MaxHp < p.AutoHealPct);

        if (Wants(p)) { best = p; bestPct = p.Hp * 100f / p.MaxHp; }

        if (IsAllyTargetable(def) && _world.Parties.TryGetValue(p.Id, out var party))
        {
            float range = SkillMath.EffectiveRange(def, p.Archetype, p.BasicAttackRange, p.Level, p.SkillLevelOf(def.Id));
            foreach (var id in party.Members)
            {
                if (id == p.Id) continue;
                if (!_world.Entities.TryGetValue(id, out var m) || m.Dead || !Wants(m)) continue;
                if (m.Hidden) continue;   // hidden = not here, so not a heal target (BL-69)
                if (DistanceSq(p, m) > range * range) continue;
                float pct = m.Hp * 100f / m.MaxHp;
                if (pct < bestPct) { bestPct = pct; best = m; }
            }
        }
        return best;
    }

    /// <summary>Estimated full recast cycle in ticks: cast time + (cooldown-reduced) reuse + the
    /// user's extra delay. Used both to gate the auto-recast and to price MP/s.</summary>
    private int AutoCycleTicks(Entity p, SkillDef def, int extraDelay)
    {
        float castMult = def.Category == SkillCategory.Physical
            ? p.EffectiveAttackSpeedMultiplier : p.EffectiveCastSpeedMultiplier;
        int castTicks = Math.Max(2, (int)(def.CastTicks * castMult));
        int reducedCd = def.CooldownTicks;
        if (reducedCd > 0 && p.CooldownReduction > 0f)
            reducedCd = Math.Max(1, (int)(reducedCd * (1f - p.CooldownReduction)));
        return castTicks + reducedCd + Math.Max(0, extraDelay);
    }

    /// <summary>Push the auto-hunt HUD: total MP/s of enabled auto-skills (after cost/CD-reduction
    /// buffs) + each skill's reuse and MP/s. Sent on config change and each regen tick.</summary>
    private void SendAutoHuntStatus(Entity p)
    {
        var rows = new List<AutoSkillReuse>();
        float totalMps = 0f;
        foreach (var entry in p.AutoSkills)
        {
            if (!entry.Enabled) continue;
            if (SkillCatalog.Get(entry.SkillId) is not SkillDef def || !p.HasSkill(def.Id)) continue;
            int lvl = Math.Max(1, p.SkillLevelOf(def.Id));
            float mp = def.MpCostAt(lvl) * MpCostFactor(p, def);
            float reuseSec = Math.Max(0.1f, AutoCycleTicks(p, def, entry.ExtraDelayTicks) * GameConstants.TickSeconds);
            float mps = mp / reuseSec;
            totalMps += mps;
            string name = ClassSkills.DisplayName(def.Id, p.Race, p.BaseClass, p.Archetype, p.Discipline);
            rows.Add(new AutoSkillReuse(def.Id, name, reuseSec, mps));
        }
        SendTo(p, "AutoHunt", new AutoHuntStatus(p.AutoHuntEnabled, totalMps, rows.ToArray(),
                                                 p.FarmCenterX, p.FarmCenterY,
                                                 AutoIdleSecondsLeft(p), AutoOfflineSecondsLeft(p)));
    }

    /// <summary>Seconds left on the ACCOUNT's online auto-hunt allowance today; -1 when uncapped.
    /// The balance only drains while auto-hunt is actually running, so this is a genuine "time left",
    /// not wall time — and it is SHARED, so a second character of the same account sees it fall.</summary>
    private int AutoIdleSecondsLeft(Entity p)
        => BudgetOf(p) is not { } b || AutoIdleCapSecondsFor(b) <= 0 ? -1
         : Math.Max(0, (int)(b.AutoTicksLeft * GameConstants.TickSeconds));

    /// <summary>Seconds left on the ACCOUNT's offline allowance; -1 when uncapped. Meaningful before
    /// you go offline too — it is what an offline session started now would get.</summary>
    private int AutoOfflineSecondsLeft(Entity p)
        => BudgetOf(p) is not { } b || AutoOfflineCapSecondsFor(b) <= 0 ? -1
         : Math.Max(0, (int)(b.OfflineTicksLeft * GameConstants.TickSeconds));

    /// <summary>Echo the full stored config so the client UI reflects the persisted settings.</summary>
    private void SendSkillBar(Entity p) =>
        SendTo(p, "SkillBar", new SkillBarDto(p.ActiveSkillBar));

    // 🔴 EVERY field of the config must be echoed here, or it is DESTROYED on the next push, not merely
    // mis-drawn. The client keeps this echo as its whole idea of the config and sends `AutoConfig with
    // { …the bit I edited… }` back, so a field this method omits comes back null and HandleSetAutoHuntConfig
    // clears it. `Buffs` was omitted when the BUFFS tab was added (BL-04) and that is exactly what happened:
    // the tab looked empty after a relog AND the first press of the Auto button wiped it on the server.
    // 🔑 Appending a field to this DTO means touching FOUR sites — the record, the handler, the snapshot
    // and THIS — and only this one fails silently. (Same shape of miss as `67i`/`74b`.)
    private void SendAutoHuntConfig(Entity p) =>
        SendTo(p, "AutoConfig", new AutoHuntConfigDto(
            p.AutoHuntEnabled, p.AutoHpPotionPct, p.AutoMpPotionPct, p.AutoBuffPotions,
            p.AutoSkills.ToArray(), p.AutoBuffPotionIds.ToArray(),
            p.AutoFarmRange, p.AutoFarmStatic, p.AutoAttackNormal, p.AutoAttackElite, p.AutoAttackBoss,
            p.AutoHealPotions.ToArray(), p.AutoCyclic, p.AutoHealPct, p.AutoAssistLeader,
            p.AutoBuffs.ToArray(), p.AutoMpPct));

    /// <summary>Spend ONE tick of the ACCOUNT's daily allowance for this player. Called each tick per
    /// farming character, which IS the drain rule: N characters of one account spend N ticks a tick,
    /// so one gets the full 2h and ten get twelve minutes each, with no special case for the count.
    /// Online exhaustion stops auto-hunt; offline exhaustion queues the logout.</summary>
    private void TickAutoHuntBudget(Entity p)
    {
        var b = BudgetOf(p);

        if (p.IsOfflineFarming)
        {
            if (b is not null && AutoOfflineCapSecondsFor(b) > 0)
            {
                b.Dirty = true;
                if (--b.OfflineTicksLeft <= 0)
                {
                    b.OfflineTicksLeft = 0;
                    _endOfflineQueue.Add(p.Id);
                }
            }
            p.OfflineSecondsLeft = AutoOfflineSecondsLeft(p);   // read by the character screen
        }
        else if (b is not null && AutoIdleCapSecondsFor(b) > 0)
        {
            b.Dirty = true;
            if (--b.AutoTicksLeft <= 0)
            {
                b.AutoTicksLeft = 0;
                StopAutoHunt(p, "your account's auto-hunt time for today is used up.");
            }
        }
    }

    /// <summary>Turn auto-hunt off (and disengage). No-ops the UI pushes automatically for an offline
    /// (connectionless) entity.
    ///
    /// <para>There is no "locked until re-log" flag any more: the ACCOUNT balance is the gate now, and
    /// re-logging no longer refills it. The old flag was cleared at login, which is precisely how the
    /// cap was escaped.</para></summary>
    private void StopAutoHunt(Entity p, string reason)
    {
        p.AutoHuntEnabled = false;
        p.Engaged = false;
        p.CombatTargetId = null;
        p.QueuedSkillId = null;
        SendAutoHuntConfig(p);
        SendAutoHuntStatus(p);
        SendSystemToEntity(p, $"Auto-hunt stopped: {reason}");
    }

    /// <summary>End an offline-farming session: turn auto off (so it doesn't immediately re-arm on
    /// the next login), then remove + save the character (a normal logout). Deferred out of the
    /// entity loop.</summary>
    private void EndOfflineSession(Guid id)
    {
        if (!_world.Entities.TryGetValue(id, out var e) || !e.IsOfflineFarming)
            return;
        e.IsOfflineFarming = false;
        e.AutoHuntEnabled = false;   // require a manual re-enable next login
        RemoveFromParty(e, "logged out");   // truly gone now — leave the party
        _world.Entities.Remove(id, out _);
        _world.Grid.Remove(e);
        SaveEntity(e);
        SaveBudgetOf(e);   // the allowance it just spent must survive a crash before the next autosave
        BroadcastSystem($"{e.Name} stopped hunting.");
    }

    /// <summary>Remove an entity from its party (leave/kick/disconnect). Reassigns the leader if
    /// needed, disbands a party that drops below 2, and refreshes everyone's roster.</summary>
    private void RemoveFromParty(Entity entity, string reason)
    {
        if (!_world.Parties.Remove(entity.Id, out var party))
            return;
        party.Members.Remove(entity.Id);
        // A membership change invalidates any in-flight loot vote.
        if (party.PendingLootMode is not null)
        {
            ClearLootVote(party);
            BroadcastToParty(party, "The loot-rule vote was cancelled (party changed).");
        }
        SendTo(entity, "Party", new PartyUpdate(Array.Empty<PartyMemberDto>()));   // client hides window

        // Disband when only one member would remain.
        if (party.Members.Count <= 1)
        {
            foreach (var mid in party.Members.ToList())
            {
                _world.Parties.Remove(mid);
                if (_world.Entities.TryGetValue(mid, out var last))
                {
                    SendTo(last, "Party", new PartyUpdate(Array.Empty<PartyMemberDto>()));
                    SendSystemToEntity(last, $"{entity.Name} {reason}. The party has disbanded.");
                }
            }
            party.Members.Clear();
            return;
        }

        if (party.LeaderId == entity.Id)
            party.LeaderId = party.Members[0];   // oldest remaining member becomes leader
        BroadcastToParty(party, $"{entity.Name} {reason}.");
        SendPartyUpdate(party);
    }

    /// <summary>Class label for the party window: 3rd class name, else 2nd, else the base class.</summary>
    private static string PartyClassLabel(Entity e)
    {
        if (e.FourthClass != 0 && FourthClassCatalog.Get(e.FourthClass) is FourthClassDef fcd) return fcd.Name;
        if (e.ThirdClass != 0 && ThirdClassCatalog.Get(e.ThirdClass) is ThirdClassDef tcd) return tcd.Name;
        if (e.SecondClass != 0 && ClassCatalog.Get(e.SecondClass) is SecondClassDef scd) return scd.Name;
        return e.BaseClass.ToString();
    }

    private void SendPartyUpdate(Party party)
    {
        var members = new List<PartyMemberDto>(party.Members.Count);
        foreach (var mid in party.Members)
            if (_world.Entities.TryGetValue(mid, out var m))
            {
                // Debuff NAMES so a healer sees who needs a cleanse from the roster (Internal counters
                // like DoT stacks aren't shown — they're not something you cure).
                var debuffs = m.Buffs.Where(b => b.IsDebuff && !b.Internal)
                    .Select(b => b.Name).Distinct().ToArray();
                var buffs = m.Buffs.Where(b => !b.IsDebuff && !b.Internal)
                    .Select(b => b.Name).Distinct().ToArray();
                members.Add(new PartyMemberDto(m.Id, m.Name, m.Level, PartyClassLabel(m),
                    (int)m.Hp, m.MaxHp, (int)m.Mp, m.MaxMp, mid == party.LeaderId,
                    // Offline first (a disconnected member cannot be reached for a different and more
                    // basic reason), then HIDDEN — which is the one the healer needs, because it is
                    // the only status that means "still here, still yours, and still untargetable".
                    m.IsOfflineFarming || m.IsDisconnected ? PartyMemberStatus.Offline
                        : m.Hidden ? PartyMemberStatus.Hidden
                        : m.AutoHuntEnabled ? PartyMemberStatus.Auto
                        : PartyMemberStatus.Online,
                    debuffs.Length > 0 ? debuffs : null,
                    buffs.Length > 0 ? buffs : null));
            }
        var dto = new PartyUpdate(members.ToArray(), party.LootMode);
        foreach (var mid in party.Members)
            if (_world.Entities.TryGetValue(mid, out var m))
                SendTo(m, "Party", dto);
    }

    /// <summary>If the party leader is offline-farming, hand leadership to the first non-offline
    /// member so the party isn't stuck with a leader who can't invite/kick/set loot.</summary>
    private void ReassignLeaderIfNeeded(Party party)
    {
        if (_world.Entities.TryGetValue(party.LeaderId, out var leader) && !leader.IsOfflineFarming)
            return;
        foreach (var mid in party.Members)
            if (_world.Entities.TryGetValue(mid, out var m) && !m.IsOfflineFarming)
            {
                party.LeaderId = mid;
                return;
            }
    }

    private void BroadcastToParty(Party party, string text)
    {
        foreach (var mid in party.Members)
            if (_world.Entities.TryGetValue(mid, out var m))
                SendSystemToEntity(m, text);
    }

    private void CompleteTrade(TradeSession session)
    {
        var itemsA = ResolveOffer(session.A, session.OfferA);
        var itemsB = ResolveOffer(session.B, session.OfferB);

        // Re-check gold ownership at completion (someone may have spent since offering).
        bool goldOk = session.GoldA >= 0 && session.GoldB >= 0
            && session.A.Gold >= session.GoldA && session.B.Gold >= session.GoldB;

        bool valid = goldOk && itemsA is not null && itemsB is not null &&
            BagFitsAfterTrade(session.A, itemsA, itemsB) &&
            BagFitsAfterTrade(session.B, itemsB, itemsA);

        if (!valid)
        {
            SendSystemToEntity(session.A, "Trade failed (items/gold changed or bags full).");
            SendSystemToEntity(session.B, "Trade failed (items/gold changed or bags full).");
            CloseTrade(session);
            return;
        }

        foreach (var (item, qty) in itemsA!)
            TransferPart(session.A, session.B, item, qty);
        foreach (var (item, qty) in itemsB!)
            TransferPart(session.B, session.A, item, qty);

        // Gold changes hands (net, so a swap of equal offers is a no-op).
        if (session.GoldA != 0 || session.GoldB != 0)
        {
            session.A.Gold += session.GoldB - session.GoldA;
            session.B.Gold += session.GoldA - session.GoldB;
        }

        SendSystemToEntity(session.A, "Trade completed.");
        SendSystemToEntity(session.B, "Trade completed.");
        CloseTrade(session);
        SendInventory(session.A);
        SendInventory(session.B);
        SendGold(session.A);
        SendGold(session.B);
        SaveEntity(session.A);
        SaveEntity(session.B);
    }

    /// <summary>Re-resolve an offer against the bag AT COMPLETION TIME. Null = the offer no longer
    /// holds (item gone, item equipped since, or the stack shrank below what was promised) and the
    /// whole trade must fail — never partially.</summary>
    private static List<(InventoryItem Item, int Qty)>? ResolveOffer(Entity owner, List<TradeOfferEntry> offer)
    {
        var items = new List<(InventoryItem, int)>();
        foreach (var entry in offer)
        {
            var item = owner.Inventory.FirstOrDefault(i => i.InstanceId == entry.InstanceId);
            if (item is null || item.Equipped || entry.Quantity <= 0 || entry.Quantity > item.Quantity)
                return null;
            items.Add((item, entry.Quantity));
        }
        return items;
    }

    /// <summary>Will this bag still fit after giving <paramref name="outgoing"/> and receiving
    /// <paramref name="incoming"/>? A PARTIAL stack frees no slot (the remainder stays), and an
    /// incoming stackable costs no slot if a stack of it survives here — which is exactly what
    /// <see cref="TransferPart"/> then does, so the check and the move agree.</summary>
    private static bool BagFitsAfterTrade(Entity owner,
                                          List<(InventoryItem Item, int Qty)> outgoing,
                                          List<(InventoryItem Item, int Qty)> incoming)
    {
        int slots = owner.Inventory.Count(i => !i.Equipped);
        var stacksHere = new HashSet<string>(owner.Inventory
            .Where(i => !i.Equipped && ItemCatalog.Get(i.DefId) is { IsStackable: true })
            .Select(i => i.DefId));

        foreach (var (item, qty) in outgoing)
        {
            if (qty < item.Quantity) continue;          // partial — the stack stays put
            slots--;
            if (!owner.Inventory.Any(i => !i.Equipped && i.DefId == item.DefId && !ReferenceEquals(i, item)))
                stacksHere.Remove(item.DefId);
        }

        foreach (var (item, _) in incoming)
        {
            bool stackable = ItemCatalog.Get(item.DefId) is { IsStackable: true };
            if (stackable && stacksHere.Contains(item.DefId)) continue;   // merges into what's here
            slots++;
            if (stackable) stacksHere.Add(item.DefId);
        }

        return slots <= GameConstants.InventorySize;
    }

    /// <summary>Move <paramref name="qty"/> of an item across. A full stack moves as the same
    /// instance; a partial one is SPLIT — the source keeps the remainder and a fresh instance carries
    /// the traded part (so the receiver never inherits the sender's persistence row).</summary>
    private static void TransferPart(Entity from, Entity to, InventoryItem item, int qty)
    {
        if (qty >= item.Quantity)
        {
            TransferItem(from, to, item);
            return;
        }

        item.Quantity -= qty;
        TransferItem(from, to, new InventoryItem
        {
            DefId = item.DefId,
            Enchant = item.Enchant,
            Quantity = qty,
            Attributes = new List<ItemAttribute>(item.Attributes),
            ExpiresAtUtc = item.ExpiresAtUtc,
        });
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
            session.ReadyOf(partner),
            session.GoldOf(viewer),
            session.GoldOf(partner));
    }

    /// <summary>Draw the offer as items. The DTO's Quantity is the OFFERED count, not the stack's —
    /// the window has to show what is on the table, or "50 potions" reads as a promise of all 50.</summary>
    private static InventoryItemDto[] OfferDtos(Entity owner, List<TradeOfferEntry> offer) =>
        offer.Select(e => (Entry: e, Item: owner.Inventory.FirstOrDefault(i => i.InstanceId == e.InstanceId)))
            .Where(p => p.Item is not null)
            .Select(p => p.Item!.ToDto() with { Quantity = Math.Min(p.Entry.Quantity, p.Item!.Quantity) })
            .ToArray();

    // ----- Chat -----------------------------------------------------------------------------

    /// <summary>Parse an admin arg "name [minutes]" — if the LAST token is a number it's the minutes and
    /// the rest is the (possibly multi-word) name; otherwise the whole thing is the name at the default.</summary>
    private static (string Name, int Minutes) ParseNameMinutes(string arg, int defaultMinutes)
    {
        int sp = arg.LastIndexOf(' ');
        if (sp > 0 && int.TryParse(arg[(sp + 1)..], out int m) && m > 0)
            return (arg[..sp].Trim(), m);
        return (arg, defaultMinutes);
    }

    /// <summary>Split a command tail into tokens, keeping a "quoted run" together as ONE token —
    /// `/give` needs it for <c>"Admin Sword"</c>, and an EMPTY pair of quotes has to survive as an empty
    /// token, because that is how the owner spells "keep the default name" in a positional argument
    /// list. A plain Split would drop it and shift every argument after it by one.</summary>
    private static string[] SplitArgs(string text)
    {
        var outp = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuote = false, started = false;
        void Flush() { if (started) { outp.Add(cur.ToString()); cur.Clear(); started = false; } }
        foreach (char c in text ?? "")
        {
            if (c == '"') { inQuote = !inQuote; started = true; continue; }
            if (!inQuote && char.IsWhiteSpace(c)) { Flush(); continue; }
            cur.Append(c);
            started = true;
        }
        Flush();
        return outp.ToArray();
    }

    /// <summary>Parse a duration for `/give`: <c>0</c> = no clock, else a number with a unit —
    /// <c>s</c>/<c>m</c>/<c>h</c>/<c>d</c>/<c>w</c>. ⚠ <c>m</c> is MINUTES, not months (owner, `58d`:
    /// *"1m means one MINUTE, 1d one day"*), which is the one reading a reader is likely to get wrong.
    /// A bare number is taken as minutes.</summary>
    private static bool TryParseDuration(string text, out int seconds)
    {
        seconds = 0;
        text = (text ?? "").Trim().ToLowerInvariant();
        if (text.Length == 0) return false;
        if (text is "0" or "-") return true;                 // explicitly no clock

        char unit = text[^1];
        bool hasUnit = char.IsLetter(unit);
        string number = hasUnit ? text[..^1] : text;
        if (!double.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out double n) || n < 0)
            return false;

        int mult = !hasUnit ? 60 : unit switch
        {
            's' => 1, 'm' => 60, 'h' => 3600, 'd' => 86_400, 'w' => 604_800, _ => -1,
        };
        if (mult < 0) return false;
        seconds = (int)Math.Round(n * mult);
        return true;
    }

    /// <summary>true/false/1/0/yes/no, or null when the token means "no opinion, use the def" (`-`).</summary>
    private static bool? ParseTriState(string text) => (text ?? "").Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "yes" or "y" => true,
        "0" or "false" or "no" or "n" => false,
        _ => null,
    };

    /// <summary>Parse a gold amount: optional sign, underscore separators, and a k/m/b/t suffix
    /// (10^3 / 10^6 / 10^9 / 10^12). "-10m", "1_002_003_004_005" and "500" all parse. Returns false on
    /// anything else so a typo can't silently become a fortune.</summary>
    private static bool TryParseGold(string text, out long amount)
    {
        amount = 0;
        text = text.Trim().Replace("_", "").Replace(",", "");
        if (text.Length == 0) return false;

        long multiplier = 1;
        char suffix = char.ToLowerInvariant(text[^1]);
        if (suffix is 'k' or 'm' or 'b' or 't')
        {
            multiplier = suffix switch
            {
                'k' => 1_000L,
                'm' => 1_000_000L,
                'b' => 1_000_000_000L,
                _   => 1_000_000_000_000L,
            };
            text = text[..^1];
        }
        // Allow a decimal with a suffix ("1.5m") — it reads naturally and costs nothing.
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            return false;
        decimal scaled = value * multiplier;
        if (scaled > long.MaxValue || scaled < long.MinValue) return false;
        amount = (long)scaled;
        return true;
    }

    /// <summary>Which commands each staff role may issue. A MODERATOR is a trusted PLAYER, not a GM:
    /// they police behaviour (jail / kick / chatban and the lookups that support it) and nothing else —
    /// no god mode, no teleporting, no item or gold creation (owner).</summary>
    private static readonly HashSet<string> ModeratorCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "help", "jail", "unjail", "jailed", "kick", "chatban", "unchatban", "where",
    };

    /// <summary>May <paramref name="actor"/> use a moderation command on a character of role
    /// <paramref name="targetRole"/>? Strictly downward: you can only act on someone RANKED BELOW you.
    ///
    /// This one comparison delivers everything the hierarchy needs — an Admin can act on Moderators and
    /// Players but not on other Admins, a Moderator only on Players, and (because your own role is never
    /// below itself) NOBODY can jail/kick/ban themselves. `/jail admin` used to succeed and lock the
    /// owner in their own jail.</summary>
    private static bool Outranks(Entity actor, AccountRole targetRole) => targetRole < actor.Role;

    private void HandleAdmin(AdminCmd cmd)
    {
        // SERVER-AUTHORIZED (owner): every moderation action re-checks the caller's role here, in
        // addition to the hub's session check — these SHIP in release, so authorization can't rely on a
        // compile flag the way the DEBUG cheats do.
        if (!TryGetPlayer(cmd.ConnectionId, out var admin) || !admin.IsStaff)
            return;

        var command = cmd.Command.ToLowerInvariant();
        var arg = cmd.Argument.Trim();

        // A moderator's allow-list. Unknown commands fall through to the switch's default.
        if (admin.Role == AccountRole.Moderator && !ModeratorCommands.Contains(command))
        {
            SendSystemToEntity(admin, $"Moderators can't use /{command}.");
            return;
        }

        switch (command)
        {
            case "help":
                SendSystemToEntity(admin, admin.Role == AccountRole.Moderator
                    ? "Moderator: /jail <name> [min], /unjail <name>, /kick <name> [min], " +
                      "/chatban <name> [min], /unchatban <name>, /jailed, /where <name>"
                    : "Admin: /jail, /unjail, /kick, /ban, /unban, /chatban, /unchatban, /jailed, " +
                      "/role <name> <player|moderator|admin>, /tp <name>, /where <name>, /god, /invis, " +
                      "/spd <m|a|c> <v> (bare /spd resets), /bag <name>, /give <name>, " +
                      "/givegold <name> <amount>, /droprate [group|gear|global|amount|item <id>] [mult], " +
                      "/titleright <name> <on|off>");
                break;

            case "god":
                admin.GodMode = !admin.GodMode;
                SendSystemToEntity(admin, $"God mode {(admin.GodMode ? "ON" : "OFF")}.");
                SendAdminState(admin);   // persistent on-screen indicator, not just this one line
                break;

            // ADMIN INVISIBILITY (BL-69, kind 3) — the absolute one. Nothing in the simulation ends
            // it: not acting, not an AoE, not the archer's flare, not another staff member. It goes
            // off when this is typed again and at no other moment.
            //
            // It does NOT make him invulnerable, which is his own distinction — an AoE can still land
            // on a hidden admin, and /god is the separate switch for not caring.
            case "invis":
                admin.AdminInvisible = !admin.AdminInvisible;
                if (admin.AdminInvisible) DropAggroOn(admin);   // shed anything already chasing him
                SendSystemToEntity(admin, admin.AdminInvisible
                    ? "Invisible. Nobody can see or target you (you are still hittable — /god for that)."
                    : "Visible again.");
                break;

            case "titleright":
            {
                // Grant/revoke the right to WRITE your own title (`/title`). The owner's intent is that
                // this is eventually earned by doing something in the game; until that exists, staff
                // hand it out, and this command is the hook it will replace.
                //
                // ⚠ ONLINE characters only, unlike /role. /role has a DB path because a staff promotion
                // has to stick to someone who is not playing; this one is cosmetic, and an offline path
                // would mean a second write route to keep in step for no benefit yet.
                int rsp = arg.LastIndexOf(' ');
                if (rsp <= 0)
                {
                    SendSystemToEntity(admin, "Usage: /titleright <name> <on|off>   (online characters)");
                    break;
                }
                string rightTarget = arg[..rsp].Trim();
                string onOff = arg[(rsp + 1)..].Trim().ToLowerInvariant();
                bool? grant = onOff switch
                {
                    "on" or "yes" or "1" => true,
                    "off" or "no" or "0" => false,
                    _ => null,
                };
                if (grant is null) { SendSystemToEntity(admin, "Usage: /titleright <name> <on|off>"); break; }

                if (FindOnlinePlayer(rightTarget) is not Entity subject)
                {
                    SendSystemToEntity(admin, $"'{rightTarget}' is not online.");
                    break;
                }

                subject.MayWriteTitle = grant.Value;
                // Revoking has to take a written title straight off — RefreshTitle re-resolves the
                // choice, and a custom one with no right resolves to nothing.
                RefreshTitle(subject, notifyLoss: false);
                SendSystemToEntity(subject, grant.Value
                    ? $"You may name yourself: /title <text> ({TitleCatalog.MaxCustomLength} characters), "
                      + "/titlecolor <colour>."
                    : "Your right to name yourself has been withdrawn.");
                SendSystemToEntity(admin, $"{subject.Name}: title right {(grant.Value ? "granted" : "withdrawn")}.");
                break;
            }

            case "role":
            {
                // Grant/revoke a staff role on a CHARACTER (owner: roles are per-character, so an admin
                // account can still have ordinary characters). Works on offline characters.
                int sp = arg.LastIndexOf(' ');
                if (sp <= 0)
                {
                    SendSystemToEntity(admin, "Usage: /role <name> <player|moderator|admin>");
                    break;
                }
                string targetName = arg[..sp].Trim();
                string roleText = arg[(sp + 1)..].Trim().ToLowerInvariant();
                AccountRole? newRole = roleText switch
                {
                    "player" or "none" => AccountRole.Player,
                    "moderator" or "mod" => AccountRole.Moderator,
                    "admin" => AccountRole.Admin,
                    _ => null,
                };
                if (newRole is null)
                {
                    SendSystemToEntity(admin, $"Unknown role '{roleText}'. Use player, moderator or admin.");
                    break;
                }
                _ = Task.Run(async () =>
                {
                    // You may only re-rank someone currently BELOW you, and never up to your own rank —
                    // otherwise a second admin could be minted by anyone who already has the command.
                    var current = FindOnlinePlayer(targetName)?.Role ?? await _db.GetRoleAsync(targetName);
                    if (current is null)
                    {
                        SendSystemToEntity(admin, $"No character '{targetName}'.");
                        return;
                    }
                    if (!Outranks(admin, current.Value) || newRole.Value > admin.Role)
                    {
                        SendSystemToEntity(admin, $"You can't change {targetName}'s role.");
                        return;
                    }
                    string? canonical = await _db.SetRoleAsync(targetName, newRole.Value);
                    if (canonical is null) { SendSystemToEntity(admin, $"No character '{targetName}'."); return; }

                    // Apply live if they're logged in, so it takes effect without a relog.
                    if (FindOnlinePlayer(canonical) is Entity live)
                    {
                        live.Role = newRole.Value;
                        if (newRole.Value != AccountRole.Admin) live.GodMode = false;
                        SendAdminState(live);
                        // The staff TITLE is held by role (C17), so a demotion has to strip a worn one
                        // here and a promotion has to offer it — without this the picker (and the
                        // plate) would keep the old role's title until the next relog.
                        RefreshTitle(live);
                        SendSystemToEntity(live, $"Your role is now {newRole.Value}.");
                    }
                    SendSystemToEntity(admin, $"{canonical} is now {newRole.Value}.");
                });
                break;
            }

            case "kick":
            {
                // Per-character, timed: boot out of the world + lock THAT character out for the minutes
                // given (default 10). Works offline too (persists; EnterWorld enforces it).
                var (name, minutes) = ParseNameMinutes(arg, 10);
                var until = DateTime.UtcNow.AddMinutes(minutes);
                int kickPen = GameConstants.CharismaModerationPenalty(GameConstants.CharismaKickPenaltyPerHour, minutes);
                ModerateAsync(admin, name, $"kicked for {minutes}m", async canonical =>
                {
                    await _db.SetKickAsync(canonical, until);
                    // Remove them on the TICK thread — this is world state, and we're on a worker here.
                    _world.Commands.Enqueue(new ForceRemoveCmd(canonical, $"Kicked by staff ({minutes}m)."));
                    _world.Commands.Enqueue(new CharismaAdjustCmd(canonical, -kickPen, -kickPen));
                });
                break;
            }

            case "ban":
            {
                // Per-ACCOUNT, timed: no login at all until it expires (default 60m). Offline-safe.
                // (Ban is the one punishment that is not per-character — see CharacterRecord.Role.)
                var (name, minutes) = ParseNameMinutes(arg, 60);
                var until = DateTime.UtcNow.AddMinutes(minutes);
                ModerateAsync(admin, name, $"account banned for {minutes}m", async canonical =>
                {
                    await _db.BanAccountByCharacterNameAsync(canonical, until);
                    _world.Commands.Enqueue(new ForceRemoveCmd(canonical, $"Account banned ({minutes}m)."));
                    _world.Commands.Enqueue(new CharismaAdjustCmd(canonical, 0, 0, Zero: true));   // a ban zeroes reputation
                });
                break;
            }

            case "unban":
            {
                string name = arg;
                _ = Task.Run(async () =>
                {
                    bool ok = await _db.BanAccountByCharacterNameAsync(name, null);
                    SendSystemToEntity(admin, ok ? $"{name}'s account unbanned." : $"No character '{name}'.");
                });
                break;
            }

            case "jail":
            {
                // Per-character, timed. Persist so it SURVIVES a relog (load spawns them in jail), and if
                // they're online, jail them right now.
                var (name, minutes) = ParseNameMinutes(arg, 30);
                var until = DateTime.UtcNow.AddMinutes(minutes);
                int jailPen = GameConstants.CharismaModerationPenalty(GameConstants.CharismaJailPenaltyPerHour, minutes);
                ModerateAsync(admin, name, $"jailed for {minutes}m", async canonical =>
                {
                    await _db.SetJailAsync(canonical, until);
                    _world.Commands.Enqueue(new JailNowCmd(canonical, until, minutes));
                    _world.Commands.Enqueue(new CharismaAdjustCmd(canonical, -jailPen, -jailPen));
                });
                break;
            }

            case "unjail":
            {
                string name = arg;
                if (FindOnlinePlayer(name) is Entity unjailTarget)
                    ReleaseFromJail(unjailTarget, "You have been released from jail.");
                _ = Task.Run(async () =>
                {
                    bool ok = await _db.SetJailAsync(name, null);
                    SendSystemToEntity(admin, ok ? $"{name} released." : $"No character '{name}'.");
                });
                break;
            }

            case "chatban":
            {
                // Play on, but silent. The light-touch punishment between a warning and a jailing.
                var (name, minutes) = ParseNameMinutes(arg, 30);
                var until = DateTime.UtcNow.AddMinutes(minutes);
                int cbPen = GameConstants.CharismaModerationPenalty(GameConstants.CharismaChatBanPenaltyPerHour, minutes);
                ModerateAsync(admin, name, $"chat-banned for {minutes}m", async canonical =>
                {
                    await _db.SetChatBanAsync(canonical, until);
                    _world.Commands.Enqueue(new ChatBanNowCmd(canonical, until, minutes));
                    _world.Commands.Enqueue(new CharismaAdjustCmd(canonical, -cbPen, -cbPen));
                });
                break;
            }

            case "unchatban":
            {
                string name = arg;
                if (FindOnlinePlayer(name) is Entity unmute)
                {
                    unmute.ChatBannedUntil = null;
                    SendSystemToEntity(unmute, "You can speak again.");
                }
                _ = Task.Run(async () =>
                {
                    bool ok = await _db.SetChatBanAsync(name, null);
                    SendSystemToEntity(admin, ok ? $"{name} can speak again." : $"No character '{name}'.");
                });
                break;
            }

            case "jailed":
                _ = Task.Run(async () =>
                {
                    var list = await _db.ListJailedAsync();
                    if (list.Count == 0) { SendSystemToEntity(admin, "No characters are jailed."); return; }
                    SendSystemToEntity(admin, $"Jailed ({list.Count}):");
                    foreach (var j in list)
                    {
                        var left = j.UntilUtc - DateTime.UtcNow;
                        string t = left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes}m"
                                 : $"{Math.Max(0, (int)left.TotalMinutes)}m";
                        SendSystemToEntity(admin, $"  {j.Name} — {t} left  (/unjail {j.Name})");
                    }
                });
                break;

            case "tp":
                // Teleport the ADMIN to a named online player (admin-only movement aid).
                if (FindOnlinePlayer(arg) is Entity dest)
                {
                    PlaceEntity(admin, dest.X + _rng.Next(-60, 60), dest.Y + _rng.Next(-60, 60));
                    admin.TargetX = null;
                    admin.TargetY = null;
                    admin.Engaged = false;
                    SendSystemToEntity(admin, $"Teleported to {dest.Name}.");
                }
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            case "tpme":
                // REVERSE teleport (owner): bring a named online player TO the admin.
                if (FindOnlinePlayer(arg) is Entity summoned)
                {
                    PlaceEntity(summoned, admin.X + _rng.Next(-60, 60), admin.Y + _rng.Next(-60, 60));
                    summoned.TargetX = null;
                    summoned.TargetY = null;
                    summoned.Engaged = false;
                    SendSystemToEntity(admin, $"Summoned {summoned.Name} to you.");
                    SendSystemToEntity(summoned, $"{admin.Name} summoned you.");
                }
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            case "where":
                if (FindOnlinePlayer(arg) is Entity who)
                    SendSystemToEntity(admin, $"{who.Name} is at ({(int)who.X}, {(int)who.Y}).");
                else SendSystemToEntity(admin, $"{arg} is not online.");
                break;

            // `/spd <m|a|c> <value>` forces one speed stat; `/spd` on its own resets all three.
            // RENAMED 2026-08-07 from the four-command `/speed-move|atack|cast|reset` family (owner) —
            // one verb, a one-letter channel, and the reset is the bare command instead of a fifth
            // name to remember. ⚠ This is half the debug rig that replaced the deleted God gear (the
            // other half is `/enchant <value>`), so it is load-bearing: don't let it regress.
            case "spd":
            {
                if (arg.Length == 0)
                {
                    admin.AdminCastSpeed = null;
                    admin.AdminAttackSpeed = null;
                    admin.AdminMoveSpeed = null;
                    SendSystemToEntity(admin, "Speeds back to normal.");
                    SendStats(admin);
                    SendAdminState(admin);
                    break;
                }

                int ssp = arg.IndexOf(' ');
                string which = (ssp < 0 ? arg : arg[..ssp]).ToLowerInvariant();
                string valueText = ssp < 0 ? "" : arg[(ssp + 1)..].Trim();
                bool known = which is "m" or "a" or "c";
                if (!known ||
                    !float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || v <= 0)
                {
                    SendSystemToEntity(admin,
                        "Usage: /spd <m|a|c> <value>  (m = move, a = attack, c = cast; e.g. /spd m 250). "
                      + "/spd on its own resets all three.");
                    break;
                }

                // Deliberately UNCAPPED (owner) — the point is to see what a silly number does.
                string label;
                if (which == "m") { admin.AdminMoveSpeed = v;   label = "move"; }
                else if (which == "c") { admin.AdminCastSpeed = v; label = "cast"; }
                else { admin.AdminAttackSpeed = v; label = "attack"; }
                SendSystemToEntity(admin, $"{label} speed forced to {v:0.##}.");
                SendStats(admin);
                SendAdminState(admin);
                break;
            }

            // `/stat <name> <value>` forces ONE stat outright; `/stat` on its own clears every override.
            // The owner's `54e`: *"an admin-only stat override for every stat — acc 999999, eva, crit
            // dmg, crit rate… one command, overriding all"*. It is the general form of `/spd`, and it
            // accepts the three speed channels too (m/a/c) so there is ONE command to remember; `/spd`
            // still works and still resets the speeds, because it is load-bearing debug rig.
            //
            // Deliberately UNCAPPED and applied AFTER caps, passives, gear and the mob scale — the whole
            // point is to type a silly number and watch what the game does with it.
            case "stat":
            {
                if (arg.Length == 0)
                {
                    admin.AdminStats = null;
                    admin.AdminCastSpeed = null;
                    admin.AdminAttackSpeed = null;
                    admin.AdminMoveSpeed = null;
                    admin.RecomputeDerived();
                    SendSystemToEntity(admin, "All stat overrides cleared.");
                    SendStats(admin);
                    SendAdminState(admin);
                    break;
                }

                int stsp = arg.IndexOf(' ');
                string statKey = (stsp < 0 ? arg : arg[..stsp]).Trim().ToLowerInvariant();
                string statValue = stsp < 0 ? "" : arg[(stsp + 1)..].Trim();
                bool knownStat = Array.Exists(Entity.AdminStatKeys, k => k.Key == statKey);

                if (!knownStat ||
                    !float.TryParse(statValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float statV))
                {
                    // The list is generated from the table, so it can never drift from what is accepted.
                    SendSystemToEntity(admin, "Usage: /stat <name> <value>   (/stat alone clears all)");
                    foreach (var (key, what) in Entity.AdminStatKeys)
                        SendSystemToEntity(admin, $"   {key,-7} {what}");
                    break;
                }

                // The speed channels already have somewhere to live — route them there rather than
                // giving the same three numbers two homes that can disagree.
                switch (statKey)
                {
                    case "m": admin.AdminMoveSpeed = statV; break;
                    case "a": admin.AdminAttackSpeed = statV; break;
                    case "c": admin.AdminCastSpeed = statV; break;
                    default:
                        admin.AdminStats ??= new Dictionary<string, float>();
                        admin.AdminStats[statKey] = statV;
                        admin.RecomputeDerived();
                        break;
                }

                string statWhat = Array.Find(Entity.AdminStatKeys, k => k.Key == statKey).What;
                SendSystemToEntity(admin, $"{statWhat} forced to {statV:0.##}.");
                SendStats(admin);
                SendAdminState(admin);
                break;
            }

            case "givegold":
            {
                int gsp = arg.LastIndexOf(' ');
                if (gsp <= 0 || !TryParseGold(arg[(gsp + 1)..], out long amount))
                {
                    SendSystemToEntity(admin,
                        "Usage: /givegold <name> <amount>  — k/m/b/t suffixes and 1_000_000 both work; " +
                        "a negative amount takes gold away.");
                    break;
                }
                string goldTarget = arg[..gsp].Trim();
                if (FindOnlinePlayer(goldTarget) is not Entity gt)
                {
                    SendSystemToEntity(admin, $"{goldTarget} is not online.");
                    break;
                }
                // Clamp at zero rather than refusing: taking "all of it" shouldn't need the exact figure.
                long before = gt.Gold;
                gt.Gold = amount >= 0
                    ? (gt.Gold > long.MaxValue - amount ? long.MaxValue : gt.Gold + amount)
                    : Math.Max(0, gt.Gold + amount);
                long delta = gt.Gold - before;
                SendGold(gt);
                SaveEntity(gt);
                SendSystemToEntity(admin, $"{gt.Name}: {delta:+#,##0;-#,##0;0} gold (now {gt.Gold:#,##0}).");
                if (gt.Id != admin.Id)
                    SendSystemToEntity(gt, delta >= 0
                        ? $"You received {delta:#,##0} gold."
                        : $"{-delta:#,##0} gold was taken from you.");
                break;
            }

            case "droprate":
            {
                // Live drop tuning, per GROUP. This is a chat command rather than a row in the tuning
                // panel on purpose: the panel's payload is a wire DTO, and adding eight fields to it
                // would bump the protocol and need a matching Unity build — for a knob whose whole value
                // is being adjustable mid-playtest, on the phone, without rebuilding anything.
                string[] parts = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    SendSystemToEntity(admin,
                        $"Global drop rate x{RateConfig.World.DropChance:0.###} (reaches EVERY group; above "
                        + $"100% a drop pays copies)   |   stack size x{RateConfig.World.DropAmount:0.###}");
                    foreach (var (name, mul) in RateConfig.DropGroupRates.OrderBy(k => k.Key))
                        SendSystemToEntity(admin, $"  {name,-10} x{mul:0.###}");
                    // Per-item overrides are listed only when there ARE any — an empty section every
                    // time would be four wasted lines on a phone screen.
                    if (RateConfig.DropItemRates.Count > 0)
                    {
                        SendSystemToEntity(admin, "Per-item overrides:");
                        foreach (var (id, mul) in RateConfig.DropItemRates.OrderBy(k => k.Key))
                            SendSystemToEntity(admin,
                                $"  {ItemCatalog.Get(id)?.Name ?? id} ({id}) x{mul:0.###}");
                    }
                    SendSystemToEntity(admin,
                        "Usage: /droprate <group|gear|global|amount> <multiplier>  — 'gear' sets all four " +
                        "equipment groups at once, 'global' sets the server-wide rate, 'amount' sets " +
                        "stack size (not a rate).");
                    SendSystemToEntity(admin,
                        "       /droprate item <id or name> <multiplier>  — tunes ONE item on its own " +
                        "(x1 clears it). Inside a group this moves its share, not its rarity rung.");
                    break;
                }
                // ITEM first: its id/name may contain spaces, so it is parsed as "everything between
                // 'item' and the trailing multiplier" rather than by a fixed token count.
                if (parts[0].Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length < 3 || !float.TryParse(parts[^1],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float imult) || imult < 0f)
                    {
                        SendSystemToEntity(admin, "Usage: /droprate item <id or name> <multiplier>");
                        break;
                    }
                    string needle = string.Join(' ', parts[1..^1]).Trim();
                    // Accept the ITEM ID or the display NAME: the drop list on the phone shows names,
                    // and nothing in the client ever shows an id, so id-only would mean guessing.
                    var target = ItemCatalog.Get(needle)
                        ?? ItemCatalog.AllItems.FirstOrDefault(d =>
                               d.Name.Equals(needle, StringComparison.OrdinalIgnoreCase));
                    if (target is null)
                    {
                        var near = ItemCatalog.AllItems
                            .Where(d => d.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                            .Take(5).Select(d => $"{d.Name} ({d.Id})").ToArray();
                        SendSystemToEntity(admin, near.Length > 0
                            ? $"No item '{needle}'. Did you mean: {string.Join(", ", near)}"
                            : $"No item '{needle}'.");
                        break;
                    }
                    if (Math.Abs(imult - 1f) < 0.0001f)
                    {
                        RateConfig.DropItemRates.Remove(target.Id);
                        SendSystemToEntity(admin, $"{target.Name} ({target.Id}) back to its table value.");
                    }
                    else
                    {
                        RateConfig.DropItemRates[target.Id] = imult;
                        SendSystemToEntity(admin, $"{target.Name} ({target.Id}) = x{imult:0.###}. "
                            + "Inside a drop group this changes its SHARE of that group's one roll.");
                    }
                    break;
                }
                if (parts.Length < 2 || !float.TryParse(parts[1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float mult) || mult < 0f)
                {
                    SendSystemToEntity(admin, "Usage: /droprate <group|gear|global> <multiplier>");
                    break;
                }
                string which = parts[0].ToLowerInvariant();
                if (which == "global")
                {
                    RateConfig.World = RateConfig.World with { DropChance = mult };
                    SendSystemToEntity(admin, $"Global drop rate = x{mult:0.###} — every group, and above "
                        + "100% a drop pays that many copies instead of clamping.");
                }
                else if (which == "amount")
                {
                    // The stack-size knob, off the tuning panel on purpose (it read as a second rate and
                    // invited setting both, which squares the multiplier on everything stackable). Still
                    // reachable here, because a knob with no way to reach it is a knob that rots.
                    RateConfig.World = RateConfig.World with { DropAmount = mult };
                    SendSystemToEntity(admin, $"Stack size = x{mult:0.###} (stackables only — a piece of "
                        + "gear is one row per copy however high this goes). Not a rate: leave it at 1 "
                        + "unless you want bigger stacks specifically.");
                }
                else if (which == "gear")
                {
                    foreach (var g in new[] { "armor", "accessory", "weapon", "jewel" })
                        RateConfig.DropGroupRates[g] = mult;
                    SendSystemToEntity(admin, $"armor/accessory/weapon/jewel = x{mult:0.###} "
                        + $"(effective x{RateConfig.World.DropChance * mult:0.###} with the global rate).");
                }
                else if (RateConfig.DropGroupRates.ContainsKey(which))
                {
                    RateConfig.DropGroupRates[which] = mult;
                    // Every group takes the global now (the mats/scrolls/always exemption came off with
                    // the 100% clamp), so the effective rate is one multiplication for all of them.
                    SendSystemToEntity(admin, $"{which} = x{mult:0.###} "
                        + $"(effective x{mult * RateConfig.World.DropChance:0.###}).");
                }
                else
                {
                    SendSystemToEntity(admin,
                        $"Unknown group '{which}'. Known: {string.Join(", ", RateConfig.DropGroupRates.Keys.OrderBy(k => k))}, gear, global.");
                }
                break;
            }

            case "bag":
            {
                // Read-only-ish view of another player's inventory, with a remove button per row.
                if (FindOnlinePlayer(arg) is not Entity bagTarget)
                {
                    SendSystemToEntity(admin, $"{arg} is not online.");
                    break;
                }
                SendTo(admin, "AdminBag",
                    new AdminBagDto(bagTarget.Name, bagTarget.Gold,
                        bagTarget.Inventory.Select(i => i.ToDto()).ToArray()));
                break;
            }

            case "give":
            {
                // TWO forms, told apart by the argument count (owner's `58d` design, playtest-20):
                //
                //   /give <player>
                //       opens the admin's OWN inventory as a picker; the transfer arrives later as an
                //       AdminGiveItemCmd. Deliberately ignores tradability — staff can hand over
                //       anything, including untradeable and quest items.
                //
                //   /give <player> <itemId> [sellPrice] [tradable] [timed] ["name"] [enchant]
                //                           [canStorePrivate] [canStoreAccount] [amount]
                //       SPAWNS a tagged instance. His example:
                //         /give Gena sword1h_t10 -1 0 1d "Admin Sword" 5   ->  Admin Sword +5 (temporary bound)
                //       Everything after the item id is optional and positional; `-` means "no opinion,
                //       use the def" in any slot. This is the only route to Mythic-tier gear for testing
                //       and, since 2026-08-12, the only way to hand out a Rune of Sinners.
                var g = SplitArgs(arg);
                if (g.Length == 0)
                {
                    SendSystemToEntity(admin,
                        "Usage: /give <player>  |  /give <player> <itemId> [sellPrice] [tradable] [timed] [\"name\"] [enchant] [canStorePrivate] [canStoreAccount] [amount]  "
                      + "(`-` = leave a slot alone; e.g. /give Gena mat_iron - - - - - - - 1000)");
                    break;
                }
                if (FindOnlinePlayer(g[0]) is not Entity giveTarget)
                {
                    SendSystemToEntity(admin, $"{g[0]} is not online.");
                    break;
                }
                if (g.Length == 1)
                {
                    SendTo(admin, "AdminGivePicker",
                        new AdminBagDto(giveTarget.Name, giveTarget.Gold,
                            admin.Inventory.Select(i => i.ToDto()).ToArray()));
                    break;
                }

                if (ItemCatalog.Get(g[1]) is not ItemDef giveDef)
                {
                    SendSystemToEntity(admin, $"No item with id '{g[1]}'.");
                    break;
                }

                string Arg(int i) => i < g.Length ? g[i] : "-";

                long? sellOverride = null;
                if (Arg(2) is var sellText && sellText != "-" && sellText.Length > 0)
                {
                    if (sellText.Trim() == "-1") sellOverride = -1;                      // unsellable
                    else if (TryParseGold(sellText, out long sv) && sv != 0) sellOverride = sv;
                    // 0 (and anything unparseable) deliberately leaves it null = "use the def", which is
                    // his stated meaning of 0. A stored 0 would mean "worth nothing" — a different claim.
                }

                bool? tradeOverride = ParseTriState(Arg(3));

                int timedSeconds = 0;
                if (Arg(4) != "-" && !TryParseDuration(Arg(4), out timedSeconds))
                {
                    SendSystemToEntity(admin, $"'{Arg(4)}' is not a duration. Use 0, or a number with s/m/h/d/w (1m = one MINUTE).");
                    break;
                }

                string? customName = null;
                if (5 < g.Length && g[5].Length > 0 && g[5] != "-")
                    customName = g[5].Length > GameConstants.CustomItemNameMax
                        ? g[5][..GameConstants.CustomItemNameMax] : g[5];

                int giveEnchant = 0;
                if (Arg(6) != "-") int.TryParse(Arg(6), out giveEnchant);

                bool? storePriv = ParseTriState(Arg(7));
                bool? storeAcct = ParseTriState(Arg(8));

                // [amount], last and usually left alone — *"if i want to get 1000 mats not to have to
                // write command 1000 times"* (playtest-22). Same clamp as the debug menu's own give
                // (`74d`/`66n`): 10,000, which is well past any real need and far short of the number
                // that stalled the loop. `-` or omitted means 1, like every other slot.
                int giveQty = 1;
                if (Arg(9) != "-" && !int.TryParse(Arg(9), out giveQty))
                {
                    SendSystemToEntity(admin, $"'{Arg(9)}' is not an amount.");
                    break;
                }
                giveQty = Math.Clamp(giveQty, 1, 10_000);

                // ⚠ ALWAYS A FRESH ROW, never a merge into an existing stack. A tagged instance carries
                // properties the stack it would join does not, and merging would either silently spread
                // them across items the admin did not mean to touch or silently drop them — and the
                // whole point of `58d` is that the tag belongs to THIS copy.
                //
                // 🔑 That is also why the amount splits two ways. A STACKABLE is one row carrying the
                // quantity, so 1000 mats cost one bag slot — the case he asked for. Non-stackable GEAR
                // has to be N rows, because "ten swords" is ten things that enchant and bind
                // separately, so it is bounded by the bag rather than by the number he typed.
                int freeSlots = GameConstants.InventorySize - giveTarget.Inventory.Count(i => !i.Equipped);
                if (freeSlots <= 0)
                {
                    SendSystemToEntity(admin, $"{giveTarget.Name}'s inventory is full.");
                    break;
                }
                int rows = giveDef.IsStackable ? 1 : Math.Min(giveQty, freeSlots);

                InventoryItem given = null!;
                for (int n = 0; n < rows; n++)
                {
                    given = new InventoryItem
                    {
                        DefId = giveDef.Id,
                        Quantity = giveDef.IsStackable ? giveQty : 1,
                        Enchant = Math.Max(0, giveEnchant),
                        SellPriceOverride = sellOverride,
                        TradableOverride = tradeOverride,
                        CustomName = customName,
                        CanStorePrivate = storePriv,
                        CanStoreAccount = storeAcct,
                    };
                    if (giveDef.FixedAttributes is { Length: > 0 } giveAttrs)
                        given.Attributes = giveAttrs.ToList();

                    // An explicit duration wins; otherwise a rune/loaner still gets its own default clock,
                    // exactly as AddItem would have stamped it.
                    if (timedSeconds > 0)
                        given.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(timedSeconds);
                    else if (giveDef.IsRune && giveDef.GrantsRuneSeconds > 0)
                        given.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(giveDef.GrantsRuneSeconds);
                    else if (giveDef.LifetimeSeconds > 0)
                        given.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(giveDef.LifetimeSeconds);

                    giveTarget.Inventory.Add(given);
                }

                // ONE push for the whole grant, not one per row — the lesson of `66n`, where a push per
                // unit is what stalled the server.
                ReconcileTimedItems(giveTarget);   // a granted RUNE must start applying its buff at once
                giveTarget.RecomputeDerived();
                SendInventory(giveTarget);
                SendStats(giveTarget);
                SaveEntity(giveTarget);

                int gaveTotal = giveDef.IsStackable ? giveQty : rows;
                string tag = ItemTag.Of(given.Sellable(giveDef), given.Tradable(giveDef),
                                        given.ExpiresAtUtc is not null);
                string label = given.Name(giveDef) + (given.Enchant > 0 ? $" +{given.Enchant}" : "")
                             + (gaveTotal > 1 ? $" x{gaveTotal}" : "")
                             + (tag.Length > 0 ? " " + tag : "");
                SendSystemToEntity(admin, $"[DEBUG] Gave {giveTarget.Name}: {label}.");
                if (gaveTotal < giveQty)
                    SendSystemToEntity(admin, $"Inventory full — {gaveTotal} of {giveQty} fit.");
                if (giveTarget.Id != admin.Id)
                    SendSystemToEntity(giveTarget, $"You received {label}.");
                break;
            }

            case "testcaps":
                bool shortCaps = arg is not ("off" or "0" or "false");
                (_idleCapSeconds, _offlineCapSeconds, _graceSeconds) = shortCaps
                    ? (30, 20, 15) : (8 * 3600, 2 * 3600, 180);
                RefillAllBudgets();   // or the balance already in the tank outlives the new cap
                SendSystemToEntity(admin, shortCaps
                    ? "[DEBUG] Short caps ON: idle 30s / offline 20s / disconnect grace 15s. Allowances refilled."
                    : "[DEBUG] Short caps OFF: idle 8h / offline 2h / grace 180s. Allowances refilled.");
                break;

            // The PREMIUM knob, per account: /farmcap <player> <autoHours> <offlineHours>.
            // -1 = follow the server default, 0 = unlimited. Free is 8/2, premium 12/4.
            case "farmcap":
            {
                var fc = (arg ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (fc.Length < 3 || !float.TryParse(fc[1], out float fcAuto) || !float.TryParse(fc[2], out float fcOff))
                {
                    SendSystemToEntity(admin, "Usage: /farmcap <player> <autoHours> <offlineHours>  (-1 = server default, 0 = unlimited)");
                    break;
                }
                if (FindOnlinePlayer(fc[0]) is not Entity fcTarget || BudgetOf(fcTarget) is not { } fcBudget)
                {
                    SendSystemToEntity(admin, $"{fc[0]} is not online.");
                    break;
                }
                fcBudget.AutoCapSeconds    = fcAuto < 0 ? -1 : (int)(fcAuto * 3600);
                fcBudget.OfflineCapSeconds = fcOff  < 0 ? -1 : (int)(fcOff  * 3600);
                fcBudget.AutoTicksLeft     = (long)Math.Max(0, AutoIdleCapSecondsFor(fcBudget)) * GameConstants.TickRate;
                fcBudget.OfflineTicksLeft  = (long)Math.Max(0, AutoOfflineCapSecondsFor(fcBudget)) * GameConstants.TickRate;
                fcBudget.LastResetDate     = DateOnly.FromDateTime(DateTime.Now);
                fcBudget.Dirty             = true;
                SaveDirtyBudgets();
                static string Allowance(int sec) => sec < 0 ? "unlimited" : HumanTime(sec);
                SendSystemToEntity(admin,
                    $"[DEBUG] {fcTarget.Name}'s account: auto {Allowance(AutoIdleSecondsLeft(fcTarget))} / offline {Allowance(AutoOfflineSecondsLeft(fcTarget))} per day.");
                SendAutoHuntStatus(fcTarget);
                break;
            }

            default:
                SendSystemToEntity(admin, $"Unknown command: {command}");
                break;
        }
    }

    /// <summary>Apply a kick/ban to an ONLINE character (a no-op if they aren't). The persisted lockout
    /// was already written by the worker; this is the part that has to happen on the tick thread.</summary>
    private void HandleForceRemove(ForceRemoveCmd cmd)
    {
        if (FindOnlinePlayer(cmd.CharacterName) is Entity target)
            ForceRemovePlayer(target, cmd.Reason);
    }

    private void HandleJailNow(JailNowCmd cmd)
    {
        if (FindOnlinePlayer(cmd.CharacterName) is not Entity target) return;
        JailNow(target, cmd.Until);
        SendSystemToEntity(target, $"You have been jailed for {cmd.Minutes} minutes.");
    }

    private void HandleChatBanNow(ChatBanNowCmd cmd)
    {
        if (FindOnlinePlayer(cmd.CharacterName) is not Entity target) return;
        target.ChatBannedUntil = cmd.Until;
        SaveEntity(target);
        SendSystemToEntity(target, $"You have been silenced for {cmd.Minutes} minutes.");
    }

    /// <summary>/give: move one of the admin's items to another online player. No tradability check
    /// (owner) — this is a staff tool, and handing over an untradeable or god item is the point.</summary>
    private void HandleAdminGiveItem(AdminGiveItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var admin) || !admin.IsAdmin) return;
        if (FindOnlinePlayer(cmd.TargetName) is not Entity target)
        {
            SendSystemToEntity(admin, $"{cmd.TargetName} is not online.");
            return;
        }
        var item = admin.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        string itemName = ItemCatalog.Get(item.DefId)?.Name ?? item.DefId;
        int qty = Math.Clamp(cmd.Quantity, 1, Math.Max(1, item.Quantity));

        if (qty < item.Quantity)
        {
            // Partial stack: the giver keeps the remainder, the receiver gets a NEW instance (two
            // inventories must never share an item instance).
            item.Quantity -= qty;
            TransferItem(admin, target, new InventoryItem
            {
                DefId = item.DefId,
                Enchant = item.Enchant,
                Quantity = qty,
                Attributes = new List<ItemAttribute>(item.Attributes),
            });
            // `item` itself stays in the admin's bag, just smaller — TransferItem only moves the copy.
        }
        else
        {
            item.Equipped = false;
            item.PersistentInstanceId = null;   // it belongs to another character's row now
            TransferItem(admin, target, item);
            admin.RecomputeDerived();
        }

        SendInventory(admin);
        SendInventory(target);
        SendStats(admin);
        SendStats(target);
        SaveEntity(admin);
        SaveEntity(target);
        SendSystemToEntity(admin, $"Gave {qty}x {itemName} to {target.Name}.");
        SendSystemToEntity(target, $"{admin.Name} gave you {qty}x {itemName}.");
    }

    /// <summary>/bag: destroy an item out of another player's inventory.</summary>
    private void HandleAdminRemoveItem(AdminRemoveItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var admin) || !admin.IsAdmin) return;
        if (FindOnlinePlayer(cmd.TargetName) is not Entity target) return;
        var item = target.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;

        string itemName = ItemCatalog.Get(item.DefId)?.Name ?? item.DefId;
        bool wasEquipped = item.Equipped;
        target.Inventory.Remove(item);
        if (wasEquipped) target.RecomputeDerived();
        SendInventory(target);
        SendStats(target);
        SaveEntity(target);
        SendSystemToEntity(admin, $"Removed {itemName} from {target.Name}.");
        SendSystemToEntity(target, $"A staff member removed {itemName} from your bag.");
        // Refresh the still-open admin view.
        SendTo(admin, "AdminBag",
            new AdminBagDto(target.Name, target.Gold, target.Inventory.Select(i => i.ToDto()).ToArray()));
    }

    /// <summary>Run a punishment against a named character after checking the actor outranks them.
    ///
    /// The rank check has to happen on a WORKER (the target may be offline, so their role comes from the
    /// DB), which is also why <paramref name="action"/> must not touch world state directly — it hands
    /// that back to the tick thread as a command. The name is resolved to its CANONICAL spelling first,
    /// so `/jail test1` on a character called "Test1" both works AND reports the truth: it used to
    /// perform the action through the case-insensitive online lookup while the case-SENSITIVE database
    /// lookup failed, printing "No character 'test1'" over a jailing that had just happened.</summary>
    private void ModerateAsync(Entity actor, string targetName, string pastTense, Func<string, Task> action)
    {
        if (targetName.Length == 0)
        {
            SendSystemToEntity(actor, "Who?");
            return;
        }
        _ = Task.Run(async () =>
        {
            var online = FindOnlinePlayer(targetName);
            string? canonical = online?.Name ?? await _db.ResolveCharacterNameAsync(targetName);
            if (canonical is null)
            {
                SendSystemToEntity(actor, $"No character '{targetName}'.");
                return;
            }
            var targetRole = online?.Role ?? await _db.GetRoleAsync(canonical) ?? AccountRole.Player;
            if (!Outranks(actor, targetRole))
            {
                SendSystemToEntity(actor, string.Equals(canonical, actor.Name, StringComparison.OrdinalIgnoreCase)
                    ? "You can't do that to yourself."
                    : $"{canonical} is {targetRole} — you can't do that to them.");
                return;
            }
            await action(canonical);
            SendSystemToEntity(actor, $"{canonical} {pastTense}.");
        });
    }

    /// <summary>Push the admin-only client indicators (god mode, forced speeds) so the state is VISIBLE
    /// instead of something you rediscover by typing /god again and watching which way it toggles.</summary>
    private void SendAdminState(Entity e)
    {
        if (e.Kind != EntityKind.Player) return;
        SendTo(e, "AdminState", new AdminStateDto(
            e.Role, e.GodMode, e.AdminCastSpeed, e.AdminAttackSpeed, e.AdminMoveSpeed));
    }

    private Entity? FindOnlinePlayer(string name) =>
        _world.Entities.Values.FirstOrDefault(e =>
            e.Kind == EntityKind.Player &&
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Friend list add / remove / list (per character). NOT admin-gated. "list" reports online
    /// status; add validates the name is a real character (even offline).</summary>
    private void HandleFriend(FriendCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;
        var name = cmd.Name.Trim();

        switch (cmd.Action.ToLowerInvariant())
        {
            case "add":
                if (name.Length == 0) return;
                if (string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase))
                {
                    SendSystemToEntity(p, "You can't add yourself.");
                    return;
                }
                // Validate the target exists (may be offline) on a worker; add the CANONICAL name.
                _ = Task.Run(async () =>
                {
                    string? canonical = await _db.ResolveCharacterNameAsync(name);
                    if (canonical is null) { SendSystemToEntity(p, $"No character '{name}'."); return; }
                    if (!p.Friends.Add(canonical)) { SendSystemToEntity(p, $"{canonical} is already on your list."); return; }
                    SaveEntity(p);

                    // Friendship is MUTUAL (owner): adding someone is only half of it. Until they add
                    // you back you get NO presence information about them — and they are deliberately
                    // NOT told you added them ("he doesn't care about you"). The moment it becomes
                    // mutual, both sides find out.
                    if (await IsMutualFriendAsync(p, canonical))
                    {
                        SendSystemToEntity(p, $"{canonical} is now your friend.");
                        if (FindOnlinePlayer(canonical) is Entity nowFriend)
                            SendSystemToEntity(nowFriend, $"{p.Name} is now your friend.");
                    }
                    else
                    {
                        SendSystemToEntity(p, $"Friend request sent to {canonical}. [pending]");
                    }
                });
                break;

            case "remove":
                var toRemove = p.Friends.FirstOrDefault(f => string.Equals(f, name, StringComparison.OrdinalIgnoreCase));
                if (toRemove is not null && p.Friends.Remove(toRemove))
                {
                    SaveEntity(p);
                    SendSystemToEntity(p, $"Removed {toRemove} from friends.");
                }
                else SendSystemToEntity(p, $"{name} is not on your friend list.");
                break;

            case "list":
            {
                if (p.Friends.Count == 0) { SendSystemToEntity(p, "Your friend list is empty."); return; }
                var names = p.Friends.OrderBy(x => x).ToList();
                _ = Task.Run(async () =>
                {
                    SendSystemToEntity(p, $"Friends ({names.Count}):");
                    foreach (var f in names)
                    {
                        // One-sided entries show [pending] and NOTHING about presence — you don't get to
                        // watch someone who hasn't accepted you (owner).
                        string tag = !await IsMutualFriendAsync(p, f) ? "[pending]"
                                   : FindOnlinePlayer(f) is not null ? "[online]"
                                   : "[offline]";
                        SendSystemToEntity(p, $"  {f} {tag}");
                    }
                });
                break;
            }
        }
    }

    /// <summary>Ignore list: block / unblock / list. Blocking is ONE-SIDED and silent — the blocked player
    /// is never told. It only filters what YOU receive (whisper / world / local chat); it does not stop you
    /// messaging them. Persisted per character like the friend list.</summary>
    private void HandleBlock(BlockCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;
        var name = cmd.Name.Trim();

        switch (cmd.Action.ToLowerInvariant())
        {
            case "block":
                if (name.Length == 0) return;
                if (string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase))
                {
                    SendSystemToEntity(p, "You can't block yourself.");
                    return;
                }
                // Resolve to the CANONICAL name (may be offline), like a friend add.
                _ = Task.Run(async () =>
                {
                    string? canonical = await _db.ResolveCharacterNameAsync(name);
                    if (canonical is null) { SendSystemToEntity(p, $"No character '{name}'."); return; }
                    // Block and friend may coexist — blocking only filters CHAT; friend presence (a system
                    // message, not chat) still comes through, so there's nothing to reconcile.
                    if (!p.Blocked.Add(canonical)) { SendSystemToEntity(p, $"{canonical} is already blocked."); return; }
                    SaveEntity(p);
                    SendSystemToEntity(p, $"Blocked {canonical}. You won't see their messages.");
                });
                break;

            case "unblock":
                var toRemove = p.Blocked.FirstOrDefault(b => string.Equals(b, name, StringComparison.OrdinalIgnoreCase));
                if (toRemove is not null && p.Blocked.Remove(toRemove))
                {
                    SaveEntity(p);
                    SendSystemToEntity(p, $"Unblocked {toRemove}.");
                }
                else SendSystemToEntity(p, $"{name} is not on your block list.");
                break;

            case "list":
                if (p.Blocked.Count == 0) SendSystemToEntity(p, "Your block list is empty.");
                else
                {
                    SendSystemToEntity(p, $"Blocked ({p.Blocked.Count}):");
                    foreach (var b in p.Blocked.OrderBy(x => x))
                        SendSystemToEntity(p, $"  {b}");
                }
                // The blanket toggles belong in the same answer — "who can reach me" is one question.
                SendSystemToEntity(p, "Options: " + DescribeSocial(p.Social));
                break;

            // The blanket toggles (M2). Each is its own command and each simply FLIPS, so the same
            // word both sets and clears it — there is no /unblock-w to remember.
            case "all":       ToggleSocial(p, SocialOptions.BlockAllChat,  "All player chat"); break;
            case "whispers":  ToggleSocial(p, SocialOptions.BlockWhispers, "Whispers"); break;
            case "global":    ToggleSocial(p, SocialOptions.BlockGlobal,   "World chat"); break;
            case "trades":    ToggleSocial(p, SocialOptions.DeclineTrades, "Trade requests"); break;
            case "party":     ToggleSocial(p, SocialOptions.DeclineParty,  "Party invitations"); break;
        }
    }

    /// <summary>Flip one social toggle, persist it, and say which way it went. ⚠ Staff are exempt from
    /// every one of these at the delivery site — see <see cref="Entity.Refuses"/>.</summary>
    private void ToggleSocial(Entity p, SocialOptions option, string label)
    {
        bool on = (p.Social & option) != 0;
        p.Social = on ? p.Social & ~option : p.Social | option;
        SaveEntity(p);
        SendSocialOptions(p);
        SendSystemToEntity(p, on
            ? $"{label}: allowed again."
            : $"{label}: blocked. Staff can still reach you.");
    }

    /// <summary>Push the social toggles so the Options window draws the SERVER's answer. The window
    /// must never render its own optimistic guess — a toggle the server refused would sit there lying.</summary>
    private void SendSocialOptions(Entity player) =>
        SendTo(player, "SocialOptions", new SocialOptionsUpdate((int)player.Social));

    /// <summary>The social toggles as one readable line ("nothing blocked" when clear).</summary>
    private static string DescribeSocial(SocialOptions s)
    {
        if (s == SocialOptions.None) return "nothing blocked.";
        var parts = new List<string>();
        if ((s & SocialOptions.BlockAllChat) != 0) parts.Add("all chat");
        if ((s & SocialOptions.BlockWhispers) != 0) parts.Add("whispers");
        if ((s & SocialOptions.BlockGlobal) != 0) parts.Add("world chat");
        if ((s & SocialOptions.DeclineTrades) != 0) parts.Add("trades");
        if ((s & SocialOptions.DeclineParty) != 0) parts.Add("party invites");
        return string.Join(", ", parts) + " blocked.";
    }

    // ----- Charisma (reputation) -----

    /// <summary>Move a charisma delta onto a LIVE entity: pool clamped [0,cap], lifetime floored at 0.</summary>
    private static void GrantCharisma(Entity target, int poolDelta, long lifetimeDelta)
    {
        target.Charisma = Math.Clamp(target.Charisma + poolDelta, 0, GameConstants.CharismaPoolCap);
        target.CharismaLifetime = Math.Max(0, target.CharismaLifetime + lifetimeDelta);
    }

    /// <summary>Apply a charisma change to a character by name on the tick thread (online → live entity;
    /// offline → DB). Enqueued by the worker-thread moderation callbacks.</summary>
    private void HandleCharismaAdjust(CharismaAdjustCmd cmd)
    {
        if (FindOnlinePlayer(cmd.Name) is Entity online)
        {
            if (cmd.Zero) { online.Charisma = 0; online.CharismaLifetime = 0; }
            else GrantCharisma(online, cmd.PoolDelta, cmd.LifetimeDelta);
            SaveEntity(online);
            return;
        }
        _ = Task.Run(async () =>
        {
            if (cmd.Zero) await _db.ZeroCharismaAsync(cmd.Name);
            else await _db.AddCharismaAsync(cmd.Name, cmd.PoolDelta, cmd.LifetimeDelta);
        });
    }

    /// <summary>Refill the daily like budget if it's a new UTC day.</summary>
    private static void RefreshLikeBudget(Entity p)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (p.LikeBudgetDay != today)
        {
            p.LikeBudgetDay = today;
            p.LikesRemainingToday = GameConstants.DailyLikeBudget;
        }
    }

    /// <summary>Give a player +1 charisma from your daily budget. Works on an offline target (DB write).</summary>
    private void HandleLike(LikeCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p)) return;
        var name = cmd.Name.Trim();
        if (name.Length == 0) return;
        if (string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase))
        {
            SendSystemToEntity(p, "You can't like yourself.");
            return;
        }

        RefreshLikeBudget(p);
        if (p.LikesRemainingToday <= 0)
        {
            SendSystemToEntity(p, "You've used all your likes today. They refresh at midnight (UTC).");
            return;
        }

        // Online target: apply on the tick thread (safe entity mutation).
        if (FindOnlinePlayer(name) is Entity online)
        {
            p.LikesRemainingToday--;
            GrantCharisma(online, 1, 1);
            SaveEntity(p);
            SaveEntity(online);
            SendSystemToEntity(p, $"You liked {online.Name}. ({p.LikesRemainingToday} likes left today)");
            SendSystemToEntity(online, $"{p.Name} liked you — charisma is now {online.Charisma}.");
            return;
        }

        // Offline target: spend the like, then resolve + apply in the DB on a worker (no live entity to race).
        // A typo'd offline name simply costs the like — no off-tick refund (keeps the single-writer rule).
        p.LikesRemainingToday--;
        SaveEntity(p);
        _ = Task.Run(async () =>
        {
            string? canonical = await _db.ResolveCharacterNameAsync(name);
            if (canonical is null) { SendSystemToEntity(p, $"No character '{name}'."); return; }
            await _db.AddCharismaAsync(canonical, 1, 1);
            SendSystemToEntity(p, $"You liked {canonical} (offline). ({p.LikesRemainingToday} likes left today)");
        });
    }

    /// <summary>Start/stop FOLLOWING a player. Only players are followable, and never yourself. Follow
    /// ends the current attack (you're tailing, not fighting) — assist is the "fight with them" verb.</summary>
    private void HandleFollow(FollowCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p) || p.Dead) return;
        if (cmd.TargetId is not Guid tid)
        {
            if (p.FollowTargetId is not null)
            {
                p.FollowTargetId = null;
                SendSystemToEntity(p, "You stop following.");
            }
            return;
        }
        if (tid == p.Id || !_world.Entities.TryGetValue(tid, out var target) ||
            target.Kind != EntityKind.Player || target.Dead)
            return;
        p.FollowTargetId = tid;
        p.Engaged = false;
        p.CombatTargetId = null;
        p.AttackCommandTargetId = null;
        SendSystemToEntity(p, $"You follow {target.Name}.");
    }

    /// <summary>ASSIST: adopt the target player's CURRENT combat target — attack whatever they're
    /// attacking (a mob, or in PvP a foe). One-shot; does nothing if they aren't fighting anything.</summary>
    private void HandleAssist(AssistCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p) || p.Dead) return;
        if (!_world.Entities.TryGetValue(cmd.TargetId, out var ally) || ally.Kind != EntityKind.Player)
            return;
        if (ally.CombatTargetId is not Guid foeId || !_world.Entities.TryGetValue(foeId, out var foe) || foe.Dead)
        {
            SendSystemToEntity(p, $"{ally.Name} isn't attacking anything.");
            return;
        }
        // Route through the normal attack path so all the PvP/target validation applies, and tell the
        // client to point its target frame at the foe (so "assist" visibly takes their target).
        p.FollowTargetId = null;
        HandleAttack(new AttackCmd(cmd.ConnectionId, foeId));
        SendTo(p, "SetTarget", foeId);
    }

    /// <summary>Walk a following player toward their target each tick, stopping a short distance away so
    /// they don't stack on top. The follow ends if the target logs off, dies, or leaves view.</summary>
    private const float FollowStopDistance = 90f;
    private void TickFollow(Entity p)
    {
        if (p.FollowTargetId is not Guid tid) return;
        if (p.Dead || !_world.Entities.TryGetValue(tid, out var target) || target.Dead ||
            DistanceSq(p, target) > GameConstants.ViewRange * GameConstants.ViewRange)
        {
            p.FollowTargetId = null;
            SendSystemToEntity(p, "You stop following.");
            return;
        }
        // Re-aim at the target's current position each tick (auto-repath). MoveTowardTarget does the walk.
        float dx = target.X - p.X, dy = target.Y - p.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist <= FollowStopDistance)
        {
            p.TargetX = null;   // close enough — hold position until they move again
            p.TargetY = null;
        }
        else
        {
            p.TargetX = target.X;
            p.TargetY = target.Y;
        }
    }

    /// <summary>When a player enters the world, tell any ONLINE player who has them as a friend that
    /// they're back. Called from HandleEnterWorld.</summary>
    private void NotifyFriendsOnline(Entity entered) => NotifyFriendsPresence(entered, online: true);

    /// <summary>Tell a player's MUTUAL friends they came online / went offline. Both parties are online
    /// by definition of "someone to notify", so the mutuality test needs no DB round-trip here — a
    /// one-sided [pending] entry gets nothing, which is the point: presence is only shared between people
    /// who have both agreed to it (owner).</summary>
    private void NotifyFriendsPresence(Entity who, bool online)
    {
        foreach (var other in _world.Entities.Values)
            if (other.Kind == EntityKind.Player && other.Id != who.Id
                && other.Friends.Contains(who.Name)      // they listed us
                && who.Friends.Contains(other.Name))     // and we listed them → mutual
                SendSystemToEntity(other, $"{who.Name} is now {(online ? "Online" : "Offline")}.");
    }

    /// <summary>Is <paramref name="otherName"/> a REAL friend of <paramref name="p"/> — i.e. has the
    /// other side added them back? Reads the other party's list from memory when they're online and from
    /// the DB when they aren't. Call from a worker thread; it may hit the database.</summary>
    private async Task<bool> IsMutualFriendAsync(Entity p, string otherName)
    {
        if (!p.Friends.Contains(otherName)) return false;
        if (FindOnlinePlayer(otherName) is Entity online)
            return online.Friends.Contains(p.Name);
        var theirs = await _db.GetFriendsAsync(otherName);
        return theirs.Contains(p.Name);
    }

    /// <summary>Pin an ONLINE player in jail right now (teleport + drop combat/movement). Persistence +
    /// the relog spawn are handled by the caller's SetJailAsync.</summary>
    private void JailNow(Entity target, DateTime until)
    {
        target.JailedUntil = until;
        // Somewhere in the YARD, not on its centre point. Stacking every inmate on one coordinate is
        // what made a 300x500 room read as "1px x 1px" (owner, playtest-20 `61d`).
        (target.X, target.Y) = GameConstants.JailArrival(_rng);
        target.TargetX = null;
        target.TargetY = null;
        target.Engaged = false;
        target.CombatTargetId = null;
        if (target.CastingSkillId is not null) CancelCast(target, startCooldown: false);
        _world.Grid.UpdatePosition(target);
        SaveEntity(target);
    }

    /// <summary>Let a character out of jail and send them to the STARTING town — never the nearest one,
    /// which would tell them (and anyone watching) roughly where the jail sits on the map.</summary>
    private void ReleaseFromJail(Entity target, string message)
    {
        target.JailedUntil = null;
        var town = WorldMap.StartingTown;
        target.X = town.X + _rng.Next(-250, 250);
        target.Y = town.Y + _rng.Next(-250, 250);
        target.TargetX = null;
        target.TargetY = null;
        _world.Grid.UpdatePosition(target);
        SendSystemToEntity(target, message);
        SaveEntity(target);
        _ = Task.Run(() => _db.SetJailAsync(target.Name, null));
    }

    /// <summary>Boot a character out of the world RIGHT NOW (kick / ban) and take its entity with it.
    ///
    /// Sending "ForceDisconnect" alone was not enough: the client dropped its connection, which arrives
    /// as an ordinary LeaveCommand, and that path deliberately KEEPS you in the world — offline-farming
    /// or link-dead for the 180s grace. So a kicked player left a ghost standing there: targetable,
    /// killable, buffable, and still holding the name, so their own account was refused re-entry with
    /// "character is already online". A punishment must not depend on the punished client cooperating,
    /// so removal happens here, server-side, before the notification goes out.</summary>
    private void ForceRemovePlayer(Entity target, string reason)
    {
        if (_world.EntityToConnection.TryGetValue(target.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("ForceDisconnect", reason);
            _world.ConnectionToEntity.Remove(conn);
            _world.EntityToConnection.Remove(target.Id);
        }
        // Clear the states that would otherwise re-add them to the world on the way out.
        target.IsOfflineFarming = false;
        target.IsDisconnected = false;
        target.AutoHuntEnabled = false;
        if (target.CastingSkillId is not null) CancelCast(target, startCooldown: false);
        NormalLeave(target);
    }

        private void HandleChat(ChatCmd chat)
    {
        if (!TryGetPlayer(chat.ConnectionId, out var sender))
            return;

        var text = chat.Text.Trim();
        if (text.Length is 0 or > 200)
            return;

        // JAILED players are silenced — no chat, no whisper (owner). Admin/system messages to them still
        // come through; this only blocks what THEY send. A CHAT BAN is the same silence without the cell.
        if (sender.Jailed)
        {
            SendSystemToEntity(sender, "You can't speak while jailed.");
            return;
        }
        if (sender.ChatBanned)
        {
            var left = sender.ChatBannedUntil!.Value - DateTime.UtcNow;
            string t = left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes}m"
                     : left.TotalMinutes >= 1 ? $"{(int)left.TotalMinutes}m" : $"{(int)left.TotalSeconds}s";
            SendSystemToEntity(sender, $"You are silenced for another {t}.");
            return;
        }

        // A player may only speak on Local / World / Whisper. System and Combat (D5) are server->client
        // FEEDS, so a message arriving on either is demoted to Local rather than trusted — otherwise
        // anyone could post a convincing fake "You looted:" line into someone else's combat window.
        var channel = chat.Channel is ChatChannel.System or ChatChannel.Combat
            ? ChatChannel.Local : chat.Channel;

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

            // Blocked: the recipient ignores your whisper. Told to the sender (a dead-end whisper that
            // silently vanished would read as a bug), never to the recipient. `/block` and `/block-w`
            // (M2) refuse the whole channel; neither can shut out STAFF.
            if (target.Refuses(SocialOptions.BlockWhispers, sender)
                || target.Refuses(SocialOptions.BlockAllChat, sender)
                || target.Blocked.Contains(sender.Name))
            {
                SendSystemTo(chat.ConnectionId, $"{target.Name} is not accepting your messages.");
                return;
            }

            var whisper = new ChatMessage(sender.Name, text, ChatChannel.Whisper, target.Name);
            _ = _hub.Clients.Client(targetConn).SendAsync("Chat", whisper);
            _ = _hub.Clients.Client(chat.ConnectionId).SendAsync("Chat", whisper);
            return;
        }

        // WORLD chat has a level floor (GameConstants.WorldChatMinLevel). It is the one channel that
        // reaches every player at once, so it is the one worth making throwaway accounts for; local and
        // whisper stay open, so a new player can still ask for help where they are standing. Staff are
        // exempt — announcing is part of the job.
        if (channel == ChatChannel.World &&
            sender.Level < GameConstants.WorldChatMinLevel && !sender.IsStaff)
        {
            SendSystemToEntity(sender,
                $"World chat opens at level {GameConstants.WorldChatMinLevel}. "
                + "Local chat and whispers work now.");
            return;
        }

        var message = new ChatMessage(sender.Name, text, channel);

        if (channel == ChatChannel.World)
        {
            // Deliver to every online player EXCEPT those who have blocked the sender — by name, or by
            // refusing world chat (`/block-g`) or all chat (`/block`) wholesale. Staff are never filtered.
            foreach (var (entId, conn) in _world.EntityToConnection)
            {
                if (!_world.Entities.TryGetValue(entId, out var e)) continue;
                if (e.Blocked.Contains(sender.Name)) continue;
                if (e.Refuses(SocialOptions.BlockGlobal, sender)) continue;
                if (e.Refuses(SocialOptions.BlockAllChat, sender)) continue;
                _ = _hub.Clients.Client(conn).SendAsync("Chat", message);
            }
            return;
        }

        foreach (var nearby in _world.Grid.Nearby(sender))
        {
            if (nearby.Blocked.Contains(sender.Name)) continue;   // they've ignored you
            if (nearby.Refuses(SocialOptions.BlockAllChat, sender)) continue;   // …or everyone (M2)
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
        bool secondTick = _tick % GameConstants.SecondIntervalTicks == 0;   // DoT/HoT/buffs/party (always 1s)
        bool regenTick  = _tick % Math.Max(1, GameConstants.RegenIntervalTicks) == 0;   // natural regen (tunable)

        if (_tick % GameConstants.AutoSaveIntervalTicks == 0)
            AutoSaveAll();

        TickTitles();

        UpdateZones();

        // SNAPSHOT, not the live dictionary — see _tickBuffer. Anything in this body may spawn or
        // despawn an entity, and enumerating a Dictionary through that throws and kills the tick.
        _tickBuffer.Clear();
        _tickBuffer.AddRange(_world.Entities.Values);

        foreach (var entity in _tickBuffer)
        {
            // Removed by an earlier iteration of THIS sweep (killed, despawned, logged out) — the
            // snapshot still holds the object, so re-check before ticking it.
            if (!_world.Entities.ContainsKey(entity.Id)) continue;

            if (entity.AttackCooldown > 0)
                entity.AttackCooldown--;
            if (entity.HideTicks > 0)
                entity.HideTicks--;
            if (entity.NoHideTicks > 0)
                entity.NoHideTicks--;

            if (entity.Kind == EntityKind.Player)
            {
                if (_tick % GameConstants.SecondIntervalTicks == 0) TickToggleUpkeep(entity);
                TickPotion(entity);
                TickRegionNotice(entity);
                TickOnlineTime(entity);
                EnforceDungeonWalls(entity);
                if (_tick % GameConstants.TickRate == 0) ReconcileTimedItems(entity);   // runes, ~1/s
                if (_tick % GameConstants.TickRate == 0) TickCraftMasterProximity(entity);
            }

            TickSkillCooldowns(entity);
            TickBuffs(entity);

            if (entity.Kind == EntityKind.Player)
                entity.FlagState = FlagOf(entity);   // name colour for the snapshot

            if (entity.Dead)
            {
                // Expire a pending resurrection offer the player didn't answer in time.
                if (entity.PendingResFromId is not null && --entity.PendingResTicks <= 0)
                {
                    entity.PendingResFromId = null;
                    SendTo(entity, "ResurrectOfferExpired", true);
                }
                continue;
            }

            // A pending class change counts down here (`BL-36`) — deliberately AFTER the Dead check,
            // so a corpse's timer neither runs nor fires. Dying cancels it outright (Kill).
            if (entity.Kind == EntityKind.Player)
                TickSubclassSwap(entity);

            if (entity.Kind == EntityKind.Mob)
                MobAi(entity);

            // A dummy that hits BACK, once per tick = his "1 damage every 0.1s" (`56c`). Outside MobAi
            // deliberately: MobAi returns immediately for any dummy, and these must stay stationary,
            // unaggressive and threat-free — they are a metronome, not a fight.
            if (entity.TrainingDummy && entity.DummyStrikes != DummyAttack.None)
                StrikeFromDummy(entity);

            if (entity.JailedUntil is not null)
            {
                if (!entity.Jailed)
                {
                    // Sentence served. Don't just unlock them where they stand — that teaches everyone
                    // where the jail is. Ship them home to their STARTING town (owner).
                    ReleaseFromJail(entity, "Your sentence is over. You have been released.");
                }
                else
                {
                    // Serving: they may walk, but only inside the cell. Everything else is blocked by
                    // IsBlockedWhileJailed; here we only make sure they can't drift out (e.g. knockback,
                    // a stale destination from before the jailing).
                    entity.Engaged = false;
                    entity.CombatTargetId = null;
                    entity.FollowTargetId = null;
                    (entity.X, entity.Y) = ClampToJail(entity.X, entity.Y);
                    if (entity.TargetX is float tgx && entity.TargetY is float tgy)
                        (entity.TargetX, entity.TargetY) = ClampToJail(tgx, tgy);
                }
            }

            // Link-dead grace. While still IN COMBAT (mid-fight drop), keep defending the current
            // target — no AutoPilot — with the grace timer PAUSED (anti-combat-log). Once out of
            // combat, freeze and count down to the normal removal.
            if (entity.IsDisconnected)
            {
                if (IsInCombat(entity))
                {
                    UpdateAction(entity);
                    MoveTowardTarget(entity);
                    _world.Grid.UpdatePosition(entity);
                    continue;
                }
                if (--entity.DisconnectGraceTicks <= 0)
                    _endGraceQueue.Add(entity.Id);
                continue;
            }

            if (entity.Kind == EntityKind.Player)
            {
                AutoPilot(entity);   // auto-potions always; hunt loop if enabled (may queue a skill)
                if (entity.AutoHuntEnabled || entity.IsOfflineFarming)
                    TickAutoHuntBudget(entity);   // idle/offline runtime caps
                TickFollow(entity);  // walk toward a followed player (auto-repath)
            }

            UpdateAction(entity);
            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);

            if (secondTick)
            {
                TickDots(entity);           // damage-over-time (bleed/poison/venom) ticks per second
                TickHealOverTime(entity);   // HoT heals even in combat (unlike natural regen)
                if (entity.Kind == EntityKind.Player)
                {
                    PushBuffs(entity);
                    if (entity.AutoSkills.Count > 0)
                        SendAutoHuntStatus(entity);   // keep MP/s live as buffs change
                }
            }

            // Natural regen runs on its OWN (slower, 3s) cadence — deliberately NOT the 1s one above,
            // so retuning regen can never change how fast a DoT ticks.
            if (regenTick)
                Regenerate(entity);
        }

        TickTraps();
        TickTotems();

        // End offline sessions that hit their cap or died (deferred so we don't mutate the entity
        // dict mid-iteration).
        if (_endOfflineQueue.Count > 0)
        {
            foreach (var id in _endOfflineQueue)
                EndOfflineSession(id);
            _endOfflineQueue.Clear();
        }
        if (_endGraceQueue.Count > 0)
        {
            foreach (var id in _endGraceQueue)
                EndDisconnectGrace(id);
            _endGraceQueue.Clear();
        }

        if (secondTick)
        {
            RefreshPartyRosters();   // live HP/MP + AFK status for the party window
            SweepPartyInvites();     // drop invites nobody answered
        }
    }

    /// <summary>Advance placed traps: expire the timed-out ones, and fire any whose radius a
    /// hostile has entered (delivering the trap skill's payload to that intruder, then removing it).</summary>
    private void TickTraps()
    {
        if (_world.Traps.Count == 0)
            return;
        for (int i = _world.Traps.Count - 1; i >= 0; i--)
        {
            var trap = _world.Traps[i];
            if (--trap.LifeTicks <= 0)
            {
                _world.Traps.RemoveAt(i);
                continue;
            }
            if (!_world.Entities.TryGetValue(trap.OwnerId, out var owner) || owner.Dead)
            {
                _world.Traps.RemoveAt(i);   // owner gone — drop the trap
                continue;
            }
            var victim = FindTrapVictim(trap, owner);
            if (victim is null)
                continue;
            FireTrap(owner, victim, trap);
            _world.Traps.RemoveAt(i);
        }
    }

    /// <summary>The target's control resistance for ONE school — Clarity against the SPT-defended
    /// (magical) debuffs, Fortitude against the CON-defended (physical) ones. A debuff with no school
    /// at all is resisted by neither: it is not part of the stat contest in the first place.</summary>
    private static float SchoolCcResist(Entity target, DebuffSchool school) => school switch
    {
        DebuffSchool.Magical  => target.CcResistMagical,
        DebuffSchool.Physical => target.CcResistPhysical,
        _ => 0f,
    };

    /// <summary>A RAID BOSS is immune to CONTROL — stun, root, fear and slow simply never land on it,
    /// at any stat, at any level (owner ruling 2026-08-19: *"bosses x0 .. never stunn never
    /// root/fear/confuse ... only dot/bleeds dmg/def mp debuffs"*).
    ///
    /// <para>It is a hard zero rather than a big CON/SPT number on purpose. A boss's whole design is a
    /// 10-30 minute fight that a party has to survive (playtest 25), and a control effect that lands
    /// even 10% of the time — the floor DebuffLandChance can never go below — is a fight the party wins
    /// by chain-rolling it instead. The affliction half of the contest is untouched: bleeds, poisons and
    /// venoms still land, still tick, and still carry their own stat debuffs, which is exactly the
    /// allow-list he named.</para>
    ///
    /// <para>⚠ The test is "PURELY control". A DoT that carries a slow as a rider (Rupture =
    /// Bleed | Slow) still lands whole — see SkillEffect.ControlCc for why that concession is
    /// deliberate rather than an oversight.</para></summary>
    /// <summary>The level a cast SKILL RUNG counts as — the character level at which the caster's class
    /// learns that rung, NOT the caster's own level (owner ruling 2026-08-19: *"it should be difference
    /// enemy lvl and skill learned lvl .. not casters"*).
    ///
    /// <para>His case is a hold whose every rung is identical — same 30s duration, nothing else on the
    /// sheet — so with the caster's level driving the contest, *"casting it lvl 1 (@40) or lvl 10 (@74)
    /// when character is lvl 75 wont do nothing of a difference"*. Reading the RUNG makes the ladder the
    /// whole point of the skill: at level 75 the @74 rung lands ~48% and the @40 rung sits on the floor.
    /// It is also what makes an old rung decay on its own — *"if im lvl 80 and cast lvl 74 debuff it
    /// should be weaker than a 80 lvl debuff"*.</para>
    ///
    /// <para>🔑 Same rule, same fallback as BL-71's buff threat, which prices a buff on the level its
    /// class learns it at for exactly this reason. A skill no class list owns — a mob spell, a scroll —
    /// has no rung level, and only then does the caster's own stand in.</para></summary>
    private static int RungLevel(Entity caster, SkillDef def, int lvl)
    {
        int learn = ClassSkills.LearnLevelOf(def.Id, lvl, caster.Race, caster.BaseClass,
                                             caster.Archetype, caster.Discipline);
        return learn > 0 ? learn : caster.Level;
    }

    private static bool BossShrugsOff(Entity target, SkillEffect effect) =>
        target.Rank == MobRank.Boss
        && (effect & SkillEffect.ControlCc) != 0
        && (effect & SkillEffect.AnyDot) == 0;

    /// <summary>Advance placed totems: expire the timed-out ones and pulse the rest at the allies
    /// standing inside them. A totem outlives its owner's DEATH on purpose — it is planted ground,
    /// not a channel, and a totem that vanished the moment the healer fell would be worth least at
    /// exactly the moment it is needed most. It is dropped only when the owner leaves the world.</summary>
    private void TickTotems()
    {
        if (_world.Totems.Count == 0)
            return;
        for (int i = _world.Totems.Count - 1; i >= 0; i--)
        {
            var totem = _world.Totems[i];
            if (--totem.LifeTicks <= 0)
            {
                _world.Totems.RemoveAt(i);
                continue;
            }
            if (!_world.Entities.TryGetValue(totem.OwnerId, out var owner))
            {
                _world.Totems.RemoveAt(i);   // owner gone from the world entirely — drop it
                continue;
            }
            if (--totem.NextPulseIn > 0)
                continue;
            totem.NextPulseIn = totem.PulseTicks;

            string name = ClassSkills.DisplayName(
                totem.SkillId, owner.Race, owner.BaseClass, owner.Archetype, owner.Discipline);
            bool heals    = totem.Effect.HasFlag(SkillEffect.Heal);
            bool restores = totem.Effect.HasFlag(SkillEffect.RestoreMp);
            foreach (var ally in AlliesAroundPoint(owner, totem.X, totem.Y, totem.Radius))
            {
                if (heals)
                    HealOne(owner, ally, totem.PulseAmount, 0, name);
                // No special case: the restore-received multiplier is a PERCENT, so a pulse scales
                // the same way a cast does and a robed nuker standing in a mana totem gets ×1.6 of
                // it — the owner's own worked example (10/s → 16/s, ~30/s → 48/s at the top).
                if (restores)
                    RestoreMpOne(owner, ally, totem.PulseAmount, name);
            }
        }
    }

    /// <summary>The owner and their party-mates standing within `radius` of a POINT. The same rules as
    /// <see cref="PlayersInRadius"/> — party members only, never the dead, never someone Hidden
    /// (BL-69) — but centred on a placed object rather than on the caster, because the whole skill of
    /// a totem is where you put it and then walking away from it.</summary>
    private IEnumerable<Entity> AlliesAroundPoint(Entity owner, float x, float y, float radius)
    {
        float r2 = radius * radius;
        bool InRange(Entity e)
        {
            float dx = e.X - x, dy = e.Y - y;
            return dx * dx + dy * dy <= r2;
        }
        if (!owner.Dead && InRange(owner))
            yield return owner;
        if (!_world.Parties.TryGetValue(owner.Id, out var party))
            yield break;   // solo: the owner alone
        foreach (var e in _world.Grid.Nearby(owner))
        {
            if (e.Kind != EntityKind.Player || e.Dead || e.Id == owner.Id)
                continue;
            if (!party.Contains(e.Id) || e.Hidden)
                continue;
            if (InRange(e))
                yield return e;
        }
    }

    /// <summary>The nearest hostile within a trap's radius, or null. A trap triggers on mobs (and,
    /// once PvP exists, enemy players); never on the owner or allies.</summary>
    private Entity? FindTrapVictim(TrapInstance trap, Entity owner)
    {
        float r2 = trap.Radius * trap.Radius;
        Entity? best = null;
        float bestD = float.MaxValue;
        foreach (var e in _world.Entities.Values)
        {
            if (e.Dead || e.Kind != EntityKind.Mob || e.TrainingDummy)
                continue;
            float dx = e.X - trap.X, dy = e.Y - trap.Y;
            float d = dx * dx + dy * dy;
            if (d <= r2 && d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    /// <summary>Deliver a sprung trap's payload: the trap skill's damage (physical/magic) + any
    /// contested CC (Root/Stun/Slow), attributed to the owner. A compact reuse of the cast path
    /// (no crit/block nuance — traps are control tools; retune later).</summary>
    private void FireTrap(Entity owner, Entity victim, TrapInstance trap)
    {
        if (SkillCatalog.Get(trap.SkillId) is not SkillDef def)
            return;
        string name = ClassSkills.DisplayName(
            def.Id, owner.Race, owner.BaseClass, owner.Archetype, owner.Discipline);
        DeliverSimpleHit(owner, victim, def, trap.Level, name);
    }

    /// <summary>Apply a skill's damage (physical/magic) + any contested CC to one victim, attributed
    /// to attacker — a compact version of the cast path with no crit/block nuance. Shared by TRAP
    /// triggers and boss AoE slams. Kills the victim if it drops to 0.</summary>
    private void DeliverSimpleHit(Entity attacker, Entity victim, SkillDef def, int lvl, string name)
    {
        if (victim.Dead) return;
        var effect = def.Effect;

        if (effect.HasFlag(SkillEffect.PhysicalDamage))
        {
            // BL-06: a physical skill lands unless the victim was GRANTED a skill-evade chance. This
            // path never rolled accuracy in the first place, so the only new thing is the grant.
            if (!def.SureHit && (victim.Immune || _rng.NextDouble() < victim.SkillEvadeChance))
            {
                BroadcastCombat(attacker, victim, 0, CombatOutcome.Miss, name);
                return;
            }
            var (pFlat, pMod) = def.PhysDamageAt(lvl);
            int dmg = StatCalculator.PhysicalDamageFM(
                (int)attacker.EffectiveAttack, pFlat, pMod, (int)victim.EffectiveDefence,
                StatCalculator.WeaponDefenceCoef(attacker.WeaponType, victim.PierceDefCoef, victim.BluntDefCoef, victim.BowDefCoef));
            dmg = FinalizeDamage(attacker, victim, dmg, DamageKind.SkillPhysical, def);
            BroadcastCombat(attacker, victim, dmg, CombatOutcome.Hit, name);
            ApplyDamage(victim, dmg, attacker);
            ReflectPhysicalSkill(attacker, victim, dmg, name);   // BL-07
        }
        if (effect.HasFlag(SkillEffect.MagicDamage) && !victim.Dead)
        {
            var (mFlat, mMod) = def.MagicDamageAt(lvl);
            int dmg = StatCalculator.MagicDamageFM(
                (int)attacker.EffectiveMagicAttack, mFlat, mMod, (int)victim.EffectiveMagicDefence,
                victim.MagicDefCoef);   // magic resistance, mirroring WeaponDefenceCoef above
            dmg = FinalizeDamage(attacker, victim, dmg, DamageKind.SkillMagic, def);
            BroadcastCombat(attacker, victim, dmg, CombatOutcome.Hit, name);
            ApplyDamage(victim, dmg, attacker);
        }
        // Contested CC (Root/Stun/Slow): the control payload.
        if ((effect & SkillEffect.ContestCc) != 0 && !victim.Dead)
        {
            int atkStat = attacker.EffectiveAtk;
            int defStat = def.DebuffSchool == DebuffSchool.Magical ? victim.EffectiveSpt : victim.EffectiveCon;
            float land = victim.Immune || BossShrugsOff(victim, effect)
                ? 0f
                : StatCalculator.DebuffLandChance(atkStat, defStat,
                                                  RungLevel(attacker, def, lvl), victim.Level);
            land *= 1f - victim.CcResist;
            land *= 1f - SchoolCcResist(victim, def.DebuffSchool);
            if (_rng.NextDouble() < land)
            {
                ApplyBuff(victim, def, lvl);
                BroadcastCombat(attacker, victim, 0, CombatOutcome.Buff, name);
            }
        }
        if (victim.Hp <= 0 && !victim.Dead)
            Kill(victim, attacker);
    }

    /// <summary>BL-07 — PHYSICAL SKILL REFLECT. A skill that lands on someone carrying Deflection has
    /// a chance to be thrown back at its caster for a fraction of the damage it dealt (the warrior
    /// default is 15%/30% chance × the FULL damage — his own pick of the two shapes he offered).
    ///
    /// Deliberately separate from <see cref="Entity.MeleeReflect"/>, which is the armor sets' counter
    /// to vampirism and fires on BASIC attacks only. A caster is never hit by both for one blow.
    ///
    /// Applied directly, never through the skill pipeline, so a reflected skill cannot be reflected
    /// again — two Deflection warriors hitting each other terminate after one bounce.</summary>
    private void ReflectPhysicalSkill(Entity caster, Entity target, int damage, string name)
    {
        if (damage <= 0 || target.PhysSkillReflectChance <= 0f || caster == target) return;
        if (_rng.NextDouble() >= target.PhysSkillReflectChance) return;
        int reflected = (int)(damage * target.PhysSkillReflectPct);
        if (reflected <= 0) return;
        // `reflected: true` — Deflection is the defender's gear/passive answering, not an act of his,
        // so it flags nobody (87a). Same clause as the armor sets' MeleeReflect.
        reflected = ApplyDamage(caster, reflected, target, reflected: true);
        BroadcastCombat(target, caster, reflected, CombatOutcome.Hit, name + " [Reflect]");
        if (caster.Hp <= 0) Kill(caster, target);
    }

    /// <summary>BL-08 — DEBUFF REFLECT. Rolled before a debuff's own land/fizzle contest: on a hit the
    /// effect is applied to the CASTER instead and the intended target takes nothing at all
    /// (*"u cast on tank he reflects u get the debuff"*).
    ///
    /// The bounced copy is applied directly, so the caster gets no resist roll and no second reflect —
    /// a debuff can bounce exactly once. Self-casts and mob-on-mob effects never bounce.
    ///
    /// <para>🔑 The third reflect path of `87a`, and it needed no change — but only by accident, so it
    /// is written down: the bounce goes through <see cref="ApplyBuff"/> and NOT
    /// <see cref="ApplyDotStack"/>, so a reflected bleed carries no <c>SourceId</c>. Its ticks are
    /// credited to nobody, which means <see cref="ApplyDamage"/> never sees a player attacker and the
    /// reflector is never flagged. ⚠ If anyone ever "fixes" that by stamping the reflector as the
    /// source for kill credit, the anti-PK exploit comes back through this door — pass
    /// <c>reflected: true</c> down with it.</para></summary>
    private bool TryReflectDebuff(Entity caster, Entity target, SkillDef def, int lvl,
        int durationOverride, string castName)
    {
        if (caster == target || target.DebuffReflectChance <= 0f) return false;
        if (_rng.NextDouble() >= target.DebuffReflectChance) return false;
        ApplyBuff(caster, def, lvl, durationOverride: durationOverride);
        BroadcastCombat(target, caster, 0, CombatOutcome.Buff, castName + " [Reflect]");
        return true;
    }

    /// <summary>Hostiles of the caster within a radius. For a MOB caster that is nearby players; for a
    /// PLAYER caster it is nearby creatures — <b>plus nearby players, but only with the PvP toggle
    /// ON</b>. Used by every area skill (boss slams today, the AOE warrior class when `BL-02` lands).
    ///
    /// <para>🔑 `BL-77`, playtest 24 — THE PVP FLAG IS THE AREA FILTER. His rule: *"pvp-off = using AOE
    /// skills hit only nearby monsters"* · *"pvp-on = hit nearby players as well"* · *"goes for all AOE
    /// skills."* Until now the toggle was read only where a player picked a SINGLE target; an area cast
    /// has no target to check, so its victim set was decided by kind alone and could never touch a
    /// player at all. Putting the rule here means every area skill inherits it at once — the same
    /// reason <see cref="CanPvpHit"/> lives in one place.</para>
    ///
    /// <para>⚠ The player arm delegates to <see cref="CanPvpHit"/>, so an area cast is bound by every
    /// rule a single swing is: never your own party, never in or into a safe zone, never yourself.
    /// A HIDDEN player is deliberately still a candidate — an area hit is positional, and the hit
    /// itself is what drags him out (<see cref="ApplyDamage"/>). An admin-invisible one is not.</para>
    ///
    /// <para>⚠ MINE, not his, and one line to reverse: <c>PvpEnabled</c> is tested *as well as*
    /// <c>CanPvpHit</c>, so with PvP OFF an area skill reaches nobody — not even a player who is
    /// already purple or red and whom you could freely hit with a single attack. His rule reads that
    /// strictly, and the loose reading re-opens the exploit he reported in the same breath: a flagged
    /// aggressor standing in your mob pile would be clipped by your AoE, and clipping a purple player
    /// flags YOU.</para></summary>
    private IEnumerable<Entity> EnemiesInRadius(Entity caster, float radius)
    {
        float r2 = radius * radius;
        bool mobCaster = caster.Kind == EntityKind.Mob;
        foreach (var e in _world.Grid.Nearby(caster))
        {
            if (e.Dead || e.TrainingDummy)
                continue;
            bool hostile = e.Kind switch
            {
                // A creature is fair game for a player's AoE and never for another creature's.
                EntityKind.Mob => !mobCaster,
                // A player: a mob's slam always reaches him (outside town); another player's area
                // skill only with PvP on, and then only where a normal attack would land.
                EntityKind.Player => mobCaster
                    ? !GameConstants.InSafeZone(e.X, e.Y)
                    : caster.PvpEnabled && !e.AdminInvisible && CanPvpHit(caster, e),
                _ => false,
            };
            if (!hostile)
                continue;
            float dx = e.X - caster.X, dy = e.Y - caster.Y;
            if (dx * dx + dy * dy <= r2)
                yield return e;
        }
    }

    // Scratch set so the per-second roster refresh only sends each party once (the Parties
    // dict maps every member -> the shared Party object).
    private readonly HashSet<Party> _rosterSeen = new();

    private void RefreshPartyRosters()
    {
        if (_world.Parties.Count == 0)
            return;
        _rosterSeen.Clear();
        foreach (var party in _world.Parties.Values)
            if (_rosterSeen.Add(party))
            {
                // Auto-cancel a loot vote nobody finished in time.
                if (party.PendingLootMode is LootMode pm && _tick >= party.LootVoteExpireTick)
                {
                    ClearLootVote(party);   // dismisses prompts + resyncs
                    BroadcastToParty(party,
                        $"The loot-rule vote for {LootModeLabel(pm)} timed out. Cancelled.");
                }
                SendPartyUpdate(party);
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

    /// <summary>Push a "you entered X" notice when the player crosses into a different region. Between
    /// regions (the wild), the current id clears so re-entering the same field notices again. Cheap:
    /// a bbox rejects almost every region in four comparisons.</summary>
    private void TickRegionNotice(Entity entity)
    {
        var region = RegionMap.At(entity.X, entity.Y);
        string id = region?.Id ?? "";
        if (id == entity.CurrentRegionId) return;
        entity.CurrentRegionId = id;
        if (region is null) return;   // left a region into the wild — no notice

        var band = RegionMap.LevelBand(region.Id);
        SendTo(entity, "Region", new RegionNotice(region.Name, band?.Min ?? 0, band?.Max ?? 0));
    }

    /// <summary>Accrues session + lifetime online time and fires the "take a break" reminder every 3h
    /// of continuous play. Lifetime seconds feed the online-time leaderboard.</summary>
    private void TickOnlineTime(Entity entity)
    {
        entity.SessionOnlineTicks++;
        if (_tick % GameConstants.TickRate == 0)
            entity.TotalOnlineSeconds++;

        // Every BreakReminderSeconds of THIS session, nudge the player to rest (a health notice, not a
        // penalty). ⚠⚠ TEMPORARILY 10 MINUTES FOR PLAYTEST 24 — his request, because `13a` has sat
        // untested for six passes purely because nobody plays 3 hours straight to see it: *"change it to
        // 10mins. (tag it to return to default 3h after test)"*. **PUT IT BACK TO 3h ONCE HE HAS SEEN THE
        // BANNER.** The constant is in GameConstants so there is exactly one number to move.
        long threeHoursTicks = GameConstants.BreakReminderSeconds * GameConstants.TickRate;
        if (entity.SessionOnlineTicks > 0 && entity.SessionOnlineTicks % threeHoursTicks == 0)
            SendTo(entity, "Notice", "You've been playing for an extended period — please take a break.");
    }

    // (The old RuneBuffKeys array is gone: SkillCatalog.IsRuneBuff answers the same question from the
    //  catalog, so a new reward rune cannot be forgotten in a second list and linger after its item.)

    /// <summary>Delete every item whose wall-clock has run out — bag, warehouse and account bank, worn
    /// or not — and keep each rune's buff in sync with the MAIN inventory: apply/keep the buff for any
    /// held unexpired rune (driving its remaining from the item's ExpiresAtUtc), and drop a rune buff
    /// whose rune is gone (expired, or moved to the warehouse — a rune only applies from the main bag).
    /// Runes were the first timed item, not the only one: the 30-day Newbie loaner kit expires here too.
    /// Cheap; runs ~1/s + on box-open + on login.</summary>
    /// <summary>Re-apply the buffs this character was carrying when it last left the world.
    ///
    /// Buffs used to die on every logout purely because nothing saved them, which the owner called out
    /// in playtest-13: a buff should end when it EXPIRES, is dispelled/cancelled, or the subclass
    /// changes — not because you closed the game. Rebuilding goes through the normal ApplyBuff path so
    /// the buff comes back with its CURRENT definition; only the remaining time, stack count and shield
    /// pool are carried over from the save.
    ///
    /// Time offline counts: the snapshot stores a wall-clock expiry, so an hour away spends an hour of
    /// a one-hour buff, and anything that ran out while logged out simply never comes back.</summary>
    private void RestorePersistedBuffs(Entity p)
    {
        if (p.PendingBuffs.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var snap in p.PendingBuffs)
        {
            if (SkillCatalog.Get(snap.SkillId) is not SkillDef def) continue;   // skill retired since

            bool toggle = snap.ExpiresAtUtc is null;
            int ticksLeft = int.MaxValue;
            if (!toggle)
            {
                double secondsLeft = (snap.ExpiresAtUtc!.Value - now).TotalSeconds;
                if (secondsLeft <= 0) continue;                                  // expired while away
                ticksLeft = Math.Max(1, (int)(secondsLeft / GameConstants.TickSeconds));
            }

            // Restore the exact buff that was saved, with the time it had left — and with the group
            // it belonged to (SourceSkillId), so an improved buff's children come back collapsed
            // under the same icon instead of scattering into four squares after a relog.
            ApplyBuff(p, def, snap.Level, displayName: snap.DisplayName, refresh: false, toggle: toggle,
                      durationOverride: toggle ? -1 : ticksLeft,
                      sourceSkillId: string.IsNullOrEmpty(snap.SourceSkillId) ? null : snap.SourceSkillId,
                      // The bar ROW belongs to whatever granted it (a potion's child stays in the
                      // consumable row); the child's own def only knows the plain buff row.
                      rowOverride: SkillCatalog.Get(snap.SourceSkillId)?.BuffRow);

            // Stacks and shield pool still need carrying over. Find it by the same key ApplyBuff used
            // (BuffPlan, so a wrapper resolves to the child that actually landed).
            string key = BuffPlan(def, snap.Level).Key;
            if (p.Buffs.FirstOrDefault(b => b.Key == key) is not BuffInstance restored) continue;
            restored.TicksRemaining = ticksLeft;
            restored.Stacks = Math.Clamp(snap.Stacks, 1, Math.Max(1, restored.MaxStacks));
            if (restored.ShieldPool > 0) restored.ShieldPool = snap.ShieldPool;
        }

        p.PendingBuffs.Clear();   // one-shot: a later logout re-captures from the live list
        p.RecomputeDerived();
        p.Hp = Math.Min(p.Hp, p.MaxHp);   // a restored +MaxHP buff must not leave HP over the new cap
        p.Mp = Math.Min(p.Mp, p.MaxMp);
        PushBuffs(p);   // the periodic push is ~1/s; don't make the bar appear a second late
    }

    private void ReconcileTimedItems(Entity p)
    {
        if (p.Kind != EntityKind.Player) return;
        var now = DateTime.UtcNow;
        bool invChanged = false, statsChanged = false;

        // 1. Purge every EXPIRED item, not just runes (playtest-19 M6: the Newbie kit is a 30-day
        //    loaner). The gate used to be `IsRune`, which meant any other timed item would sit in the
        //    bag forever with a dead clock — the item model has carried a per-instance ExpiresAtUtc
        //    since the runes, and only the sweep was rune-shaped.
        //    A WORN piece expires too: it is removed from the bag and the stats recomputed, otherwise
        //    the loaner armour would keep paying its defence for as long as you never unequipped it.
        for (int i = p.Inventory.Count - 1; i >= 0; i--)
        {
            var it = p.Inventory[i];
            if (it.ExpiresAtUtc is not DateTime exp || exp > now) continue;
            if (ItemCatalog.Get(it.DefId) is not ItemDef d) continue;
            if (it.Equipped) statsChanged = true;
            p.Inventory.RemoveAt(i);
            invChanged = true;
            SendSystemToEntity(p, $"{d.Name} has expired.");
        }

        // 2. Which rune buffs SHOULD be up, at which LEVEL, and until when?
        //    A reward-rune ladder is ONE skill with a rung per level, so several held runes can name
        //    the same buff: the STRONGEST rung wins, and among equal rungs the latest expiry. When the
        //    +100% rune runs out the +20% one in the bag takes over on the next pass, all by itself.
        var wantUntil = new Dictionary<string, (int Level, DateTime Until, string Name)>(StringComparer.Ordinal);
        foreach (var it in p.Inventory)
        {
            if (it.ExpiresAtUtc is not DateTime exp || exp <= now) continue;
            if (ItemCatalog.Get(it.DefId) is not { IsRune: true } d || string.IsNullOrEmpty(d.RuneBuffSkillId)) continue;
            int level = Math.Max(1, d.RuneBuffLevel);
            if (!wantUntil.TryGetValue(d.RuneBuffSkillId, out var cur)
                || level > cur.Level
                || (level == cur.Level && exp > cur.Until))
                wantUntil[d.RuneBuffSkillId] = (level, exp, d.Name);
        }

        // 3. Apply/keep wanted buffs; drive their remaining from the wall-clock (survives offline on login).
        foreach (var (skillId, want) in wantUntil)
        {
            if (SkillCatalog.Get(skillId) is not SkillDef skill) continue;
            var existing = p.Buffs.FirstOrDefault(b => b.Key == skillId);
            // A running buff at the WRONG rung has to go before the right one can land: every rung
            // shares the family key at the same Rank, so ApplyBuff's equal-rank rule would keep
            // whichever had longer left — and a +5% rune with an hour on it would hold off the +100%.
            if (existing != null && existing.Level != want.Level)
            {
                p.Buffs.Remove(existing);
                existing = null;
                statsChanged = true;
            }
            if (existing == null)
            {
                // The bar square is named after the ITEM, so a rung is visible on it: "Rune of
                // Experience (50%)" rather than a bare "Rune of Experience" that every rung shares.
                ApplyBuff(p, skill, want.Level, displayName: want.Name);
                existing = p.Buffs.FirstOrDefault(b => b.Key == skillId);
                statsChanged = true;
            }
            if (existing != null)
                existing.TicksRemaining = (int)Math.Clamp((want.Until - now).TotalSeconds * GameConstants.TickRate, 1, int.MaxValue);
        }

        // 4. Remove rune buffs whose rune is gone.
        for (int i = p.Buffs.Count - 1; i >= 0; i--)
            if (SkillCatalog.IsRuneBuff(p.Buffs[i].Key) && !wantUntil.ContainsKey(p.Buffs[i].Key))
            {
                p.Buffs.RemoveAt(i);
                statsChanged = true;
            }

        // Warehoused items don't apply a rune buff, but they STILL expire (bank = space, not a
        // time-pause). Same widening as the bag sweep above: any timed item, not only a rune.
        bool whChanged = false;
        for (int i = p.Warehouse.Count - 1; i >= 0; i--)
        {
            var it = p.Warehouse[i];
            if (it.ExpiresAtUtc is DateTime wexp && wexp <= now && ItemCatalog.Get(it.DefId) is ItemDef wd)
            {
                p.Warehouse.RemoveAt(i);
                whChanged = true;
                SendSystemToEntity(p, $"{wd.Name} in your warehouse has expired.");
            }
        }

        // Same for the ACCOUNT bank — it's shared storage, not a stasis field either.
        if (p.AccountId != 0 && _world.AccountWarehouses.TryGetValue(p.AccountId, out var acctBank))
        {
            bool acctChanged = false;
            for (int i = acctBank.Count - 1; i >= 0; i--)
            {
                var it = acctBank[i];
                if (it.ExpiresAtUtc is DateTime aexp && aexp <= now && ItemCatalog.Get(it.DefId) is ItemDef ad)
                {
                    acctBank.RemoveAt(i);
                    acctChanged = true;
                    SendSystemToEntity(p, $"{ad.Name} in your account warehouse has expired.");
                }
            }
            if (acctChanged) SaveAndSyncAccountBank(p);
        }

        if (statsChanged)
        {
            p.RecomputeDerived();
            PushBuffs(p);
            SendStats(p);
        }
        if (invChanged) SendInventory(p);
        if (whChanged) SendWarehouse(p);
    }

    private void TickPotion(Entity entity)
    {
        bool changed = false;

        // Tick down each per-potion cooldown; drop the ones that just hit 0 so the dict stays small.
        if (entity.PotionCooldowns.Count > 0)
        {
            List<string> done = null;
            foreach (var key in entity.PotionCooldowns.Keys.ToList())
            {
                int v = entity.PotionCooldowns[key] - 1;
                if (v <= 0) { (done ??= new()).Add(key); changed = true; }
                else entity.PotionCooldowns[key] = v;
            }
            if (done != null) foreach (var k in done) entity.PotionCooldowns.Remove(k);
        }

        // The potion heal-over-time is an ordinary buff now, so TickBuffs/TickHealOverTime run it —
        // there is no separate potion effect channel to tick any more. Only the per-potion drink
        // cooldowns above are still potion-specific.
        if (changed)
            SendPotionStatus(entity);
    }

    private void TickBuffs(Entity entity)
    {
        bool expiredAny = false;
        for (int i = entity.Buffs.Count - 1; i >= 0; i--)
        {
            if (entity.Buffs[i].Toggle) continue;   // stances never expire on their own
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

    /// <summary>One tick of a dummy that hits back — 1 damage to every player standing inside
    /// <see cref="GameConstants.DummyStrikeRange"/>, through the REAL resolution for its channel
    /// (owner, playtest-20 `56c`).
    ///
    /// <para>The damage is fixed at 1 and the rate is one per tick, so ten seconds is a hundred
    /// samples and the thing being measured is the OUTCOME, not the number: the magic dummy shows
    /// fail / hit / crit, the physical one miss / hit / crit / block, each labelled in the combat feed.
    /// That is the whole point — the owner could not observe mob magic crit because a real fight gives
    /// you five hits and a dead mob.</para>
    ///
    /// <para>It reuses the same resolvers combat uses rather than approximating them; a measuring
    /// instrument that disagrees with the thing it measures is worse than none. No threat, no
    /// retaliation, no interrupt and no kill check: a dummy is not in a fight, and 1 damage against a
    /// level-80 pool cannot kill the person counting. God mode still stops it, via ApplyDamage.</para></summary>
    private void StrikeFromDummy(Entity dummy)
    {
        float range = GameConstants.DummyStrikeRange;
        foreach (var target in _world.Grid.Nearby(dummy))
        {
            if (target.Kind != EntityKind.Player || target.Dead) continue;
            float dx = target.X - dummy.X, dy = target.Y - dummy.Y;
            if (dx * dx + dy * dy > range * range) continue;

            if (dummy.DummyStrikes == DummyAttack.Magic)
            {
                // Mirrors the magic branch of ExecuteSkill: fail is reduced damage (not zero), crit is
                // the flat x3 of the magic channel — never CritDamageBonus, which is the fighters'.
                float fail = target.Immune ? 1f
                           : StatCalculator.MagicFailChance(dummy.Level, target.Level, target.MagicFailMod,
                                                            1f, target.MagicFailBonus);
                if (_rng.NextDouble() < fail)
                {
                    int dmg = Math.Max(1, 1 / 3);
                    ApplyDamage(target, dmg, dummy);
                    BroadcastCombat(dummy, target, dmg, CombatOutcome.Fail, "Practice Bolt");
                }
                else if (_rng.NextDouble() < dummy.MagicCritChance)
                {
                    int dmg = (int)(1 * dummy.EffectiveMagicCritDamage);
                    ApplyDamage(target, dmg, dummy);
                    BroadcastCombat(dummy, target, dmg, CombatOutcome.Crit, "Practice Bolt");
                }
                else
                {
                    ApplyDamage(target, 1, dummy);
                    BroadcastCombat(dummy, target, 1, CombatOutcome.Hit, "Practice Bolt");
                }
            }
            else
            {
                // Mirrors ResolveBasicAttack's contest, minus the damage formula: miss, then the shared
                // crit-and-block resolver, so evasion, the evade floor, block chance and crit rate are
                // all the numbers the real thing uses.
                float missChance = StatCalculator.ResolveAvoidChance(
                    dummy.Accuracy, (int)target.EffectiveEvasion,
                    target.EvadeFloor, dummy.HitFloor,
                    dummy.Level, target.Level,
                    sureHit: false, defenderImmune: target.Immune);
                if (_rng.NextDouble() < missChance)
                {
                    BroadcastCombat(dummy, target, 0, CombatOutcome.Miss, "Practice Strike");
                    continue;
                }
                var (dmg, outcome) = ResolvePhysicalCritAndBlock(dummy, target, 1, dummy.CritChance, 0f, 1f);
                ApplyDamage(target, dmg, dummy);
                BroadcastCombat(dummy, target, dmg, outcome, "Practice Strike");
            }
        }
    }

    private void MobAi(Entity mob)
    {
        if (mob.TrainingDummy) return;   // stationary, never wanders or aggroes
        if (mob.DetauntTicks > 0) mob.DetauntTicks--;
        if (mob.TauntLockTicks > 0) mob.TauntLockTicks--;
        if (mob.Engaged && _tick % GameConstants.SecondIntervalTicks == 0) DecayThreat(mob);

        if (mob.Engaged)
        {
            // A target that left the world, died or went out of range used to leave the mob Engaged
            // FOREVER. This branch returns early, so such a mob never re-scanned for aggro and never
            // wandered again — it just stood where it was, mute. That is BOTH halves of the dungeon
            // report: "mobs don't aggro or fight back" and "the mobs are clamped together in the crypt"
            // (frozen on the spot they were standing when the player teleported away from the debug
            // menu). Nothing cleared it, because DropAggroOn was only ever wired to the stealth path.
            if (!HasLiveTarget(mob))
            {
                if (mob.CombatTargetId is Guid stale) mob.Threat.Remove(stale);
                mob.CombatTargetId = null;
                RetargetByThreat(mob);          // someone else may still be hitting it
                if (!HasLiveTarget(mob)) { Disengage(mob); return; }
            }

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
                    candidate.Stealthed ||
                    GameConstants.InSafeZone(candidate.X, candidate.Y))
                    continue;

                // De-taunt: ignore the entity that just shed us, briefly.
                if (mob.DetauntTicks > 0 && mob.DetauntFromId == candidate.Id)
                    continue;

                if (DistanceSq(mob, candidate) <=
                    GameConstants.MobAggroRange * GameConstants.MobAggroRange)
                {
                    // A PULL IS WORTH THREAT. It used to be worth nothing: the mob walked over with an
                    // empty table, so the very first point of damage from ANYONE — including someone
                    // who wandered past afterwards — became the top of the table and owned it. The
                    // person it actually came for was not on the list at all.
                    AddThreat(mob, candidate, mob.MaxHp * GameConstants.ThreatAggroPullFraction);
                    mob.CombatTargetId = candidate.Id;
                    mob.Engaged = true;
                    return;
                }
            }
        }

        if (_rng.NextDouble() < 0.7)
        {
            // The wander span has to FIT THE ZONE. It was a flat +/-1000 against the crypt's rooms of
            // radius 300-350, so nearly every target landed outside and got projected exactly ONTO the
            // rim — six mobs sharing one home all walking to the same small circle, which is what read
            // as "the mobs are clamped together". Scale the span to the room, and land INSIDE the
            // circle rather than on it.
            var zone = _zones.FirstOrDefault(z => z.Zone.Id == mob.ZoneId)?.Zone;
            float span = zone is not null ? Math.Min(1000f, zone.Radius * 0.6f) : 1000f;

            float tx = mob.HomeX + (float)(_rng.NextDouble() * 2.0 - 1.0) * span;
            float ty = mob.HomeY + (float)(_rng.NextDouble() * 2.0 - 1.0) * span;

            // Keep wander inside the mob's own zone so they don't drift into neighbours.
            if (zone is not null)
            {
                float inner = zone.Radius * 0.9f;   // inside the rim, not parked on it
                float dx = tx - zone.X, dy = ty - zone.Y;
                float distSq = dx * dx + dy * dy;
                if (distSq > inner * inner)
                {
                    float dist = MathF.Sqrt(distSq);
                    float scale = inner / dist;
                    tx = zone.X + dx * scale;
                    ty = zone.Y + dy * scale;
                }
            }

            tx = Math.Clamp(tx, GameConstants.WorldMinX, GameConstants.ZoneWidth);
            ty = Math.Clamp(ty, GameConstants.WorldMinY, GameConstants.ZoneHeight);
            if (!GameConstants.InSafeZone(tx, ty))
            {
                mob.TargetX = tx;
                mob.TargetY = ty;
            }
        }
    }

    /// <summary>Does this mob still have a target worth staying engaged on — one that exists, is alive
    /// and is in view? Anything else means the fight is over and the mob should go home.</summary>
    private bool HasLiveTarget(Entity mob) =>
        mob.CombatTargetId is Guid id
        && _world.Entities.TryGetValue(id, out var t) && !t.Dead
        && DistanceSq(mob, t) <= GameConstants.ViewRange * GameConstants.ViewRange;

    private void ResetMob(Entity mob)
    {
        mob.Engaged = false;
        mob.CombatTargetId = null;
        mob.Buffs.Clear();
        mob.Threat.Clear();
        mob.TauntLockTicks = 0;
        mob.CriedForHelp = false;   // the pull is over — the camp will answer the next one (BL-70)
        mob.TargetX = mob.HomeX;
        mob.TargetY = mob.HomeY;
        mob.CombatTicks = 0;
        mob.BossSkillCooldown = 0;
        mob.SkillCooldowns.Clear();   // fresh boss-skill reuse on a leash reset

        // NOT healed here any more, and that is the whole point of the change: a mob used to be
        // pristine the instant you left its view (this ran from Disengage as well as from the leash),
        // so the 20-second climb back to full simply did not exist and nothing could ever be
        // re-engaged while it was still hurt. It now walks home wounded and regenerates.
        //
        // Three things therefore do NOT belong here either — they are properties of "this pull is
        // over and the creature is whole again", not of "it stopped chasing you". They live in
        // MobRecoveryCheck, which fires when the bar is actually full:
        //   • the damage LEDGER (owner: you take it to 30%, run, and you are still on the ledger if
        //     someone else finishes it or you come back — it resets at 100% + out of combat),
        //   • ENRAGE (a boss that disengages at 30% is still the enraged boss you left),
        //   • the boss PHASE cursor — re-arming it at 30% HP would make AdvanceBossPhases fire every
        //     threshold at once on the next pull, announces, add waves and all.
    }

    /// <summary>A disengaged mob that has regenerated to full is a FRESH mob again: it owes nobody,
    /// its boss script re-arms and its enrage lapses. Called from the regen tick, which is the only
    /// place that can observe the bar reaching the top on its own.</summary>
    private void MobRecoveryCheck(Entity mob)
    {
        if (mob.Engaged || mob.Dead || mob.Hp < mob.MaxHp)
            return;
        if (mob.DamageLog.Count == 0 && mob.BossPhaseIndex == 0 && !mob.Enraged)
            return;   // already fresh — nothing to undo

        mob.DamageLog.Clear();
        mob.LastHitterId = null;
        mob.BossPhaseIndex = 0;
        if (mob.Enraged)
        {
            mob.AttackPower = (int)(mob.AttackPower / 1.5f);
            mob.MagicAttack = (int)(mob.MagicAttack / 1.5f);
            mob.BasicAttackPower = (int)(mob.BasicAttackPower / 1.5f);
            mob.AttackSpeedMultiplier /= 0.7f;
            mob.Enraged = false;
        }
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

        // A mob from a dedicated spawner respawns as ITSELF (owner, playtest-14) — the werewolf you
        // killed for a quest comes back a werewolf instead of re-rolling the camp's roster.
        zr.OnDeath(_tick, _rng, mob.SpawnerMobId);

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

        // Stun/Fear lock out actions: break any cast, drop the queued skill, do nothing.
        // (Stun also zeroes EffectiveSpeed so movement stops; Fear lets you still move.)
        if (entity.IsActionLocked)
        {
            if (entity.CastingSkillId is not null) CancelCast(entity, startCooldown: false);
            entity.QueuedSkillId = null;
            return;
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
        {
            // Boss: enrage timer + special-skill scheduling. If it queued a skill this tick,
            // let the queued-skill path pick it up next tick instead of also auto-attacking.
            if (entity.Rank == MobRank.Boss && BossTick(entity))
                return;

            if (entity.CasterMob) MobCasterAi(entity);
            else UpdateAutoAttack(entity);
        }
    }

    // Boss combat tuning: enrage after ~90s of a dragged-out fight (independent of phase enrages).
    private const int BossEnrageTicks = 900;

    // The kit a boss with no BossCatalog profile uses: just the generic slam.
    private static readonly BossSkillEntry[] DefaultBossKit = { new(SkillCatalog.BossSlamSkill) };

    /// <summary>Per-tick boss logic while engaged: the enrage timer, the HP-threshold phase script
    /// (announce / enrage / adds), and the skill rotation (cast the first ready boss skill with a
    /// foe in range). Returns true if it queued a skill this tick (so the caller skips the
    /// auto-attack). Reuse is the per-skill <see cref="SkillDef.CooldownTicks"/> via SkillCooldowns.</summary>
    private bool BossTick(Entity boss)
    {
        boss.CombatTicks++;
        var profile = BossCatalog.Get(boss.MobTypeId ?? "");

        // Enrage timer: a one-time rage if the fight drags on.
        if (!boss.Enraged && boss.CombatTicks >= BossEnrageTicks)
            EnrageBoss(boss, "flies into a rage!");

        // Phase script: fire every phase whose HP threshold we've now crossed.
        if (profile is not null)
            AdvanceBossPhases(boss, profile);

        // Skill rotation: cast the first ready boss skill that has a foe in its radius.
        if (boss.CastingSkillId is null && boss.QueuedSkillId is null &&
            boss.CombatTargetId is Guid tid && _world.Entities.TryGetValue(tid, out var tgt) && !tgt.Dead &&
            SelectBossSkill(boss, profile, tgt) is string skillId)
        {
            QueueMobSpell(boss, skillId, boss.Id);   // AoE self-centered → target self
            return true;
        }
        return false;
    }

    /// <summary>Latch the one-time rage buff: +50% P/M/basic atk, faster swings. Idempotent (a
    /// phase-enrage and the timer-enrage can't double up). Undone by ResetMob on a leash.</summary>
    private void EnrageBoss(Entity boss, string shout)
    {
        if (boss.Enraged) return;
        boss.Enraged = true;
        boss.AttackPower = (int)(boss.AttackPower * 1.5f);
        boss.MagicAttack = (int)(boss.MagicAttack * 1.5f);
        boss.BasicAttackPower = (int)(boss.BasicAttackPower * 1.5f);
        boss.AttackSpeedMultiplier *= 0.7f;   // lower multiplier = faster swings
        BroadcastCombat(boss, boss, 0, CombatOutcome.Buff, "Enrage");
        BroadcastSystem($"{boss.Name} {shout}");
    }

    /// <summary>Fire each not-yet-triggered phase whose HP threshold the boss has dropped to/below
    /// (announce + optional enrage + optional add wave). Uses a while-loop so a big single hit that
    /// skips past several thresholds fires them all in one tick.</summary>
    private void AdvanceBossPhases(Entity boss, BossProfile profile)
    {
        float hpFrac = boss.MaxHp > 0 ? (float)boss.Hp / boss.MaxHp : 0f;
        while (boss.BossPhaseIndex < profile.Phases.Length &&
               hpFrac <= profile.Phases[boss.BossPhaseIndex].HpFraction)
        {
            var phase = profile.Phases[boss.BossPhaseIndex];
            boss.BossPhaseIndex++;
            BroadcastSystem(phase.Announce);
            if (phase.Enrage) EnrageBoss(boss, "roars with fury!");
            if (phase.AddTemplateId is string addId && phase.AddCount > 0)
                SummonAdds(boss, addId, phase.AddCount, boss.Level + phase.AddLevelOffset);
        }
    }

    /// <summary>Pick the first ready boss skill: HP fraction inside the entry's window, off cooldown,
    /// and a foe within the skill's AoE radius. Null = nothing to cast this tick (auto-attack).</summary>
    private string? SelectBossSkill(Entity boss, BossProfile? profile, Entity target)
    {
        float hpFrac = boss.MaxHp > 0 ? (float)boss.Hp / boss.MaxHp : 0f;
        foreach (var e in profile?.Skills ?? DefaultBossKit)
        {
            if (hpFrac < e.MinHpFraction || hpFrac > e.MaxHpFraction) continue;
            if (boss.SkillCooldowns.ContainsKey(e.SkillId)) continue;
            if (SkillCatalog.Get(e.SkillId) is not SkillDef sk) continue;
            if (DistanceSq(boss, target) <= sk.AreaRadius * sk.AreaRadius)
                return e.SkillId;
        }
        return null;
    }

    /// <summary>Spawn a wave of adds near the boss (Normal rank, no zone → no respawn), already
    /// engaged on the boss's current target.</summary>
    private void SummonAdds(Entity boss, string templateId, int count, int level)
    {
        level = Math.Clamp(level, 1, 85);
        _world.Entities.TryGetValue(boss.CombatTargetId ?? Guid.Empty, out var target);
        for (int i = 0; i < count; i++)
        {
            double angle = _rng.NextDouble() * Math.PI * 2;
            float dist = 80f + (float)_rng.NextDouble() * 80f;
            float ax = boss.X + (float)(Math.Cos(angle) * dist);
            float ay = boss.Y + (float)(Math.Sin(angle) * dist);
            (ax, ay) = WorldMap.ClampToBorder(ax, ay);
            var add = BuildMob(templateId, level, MobRank.Normal, ax, ay, "");
            if (target is not null && !target.Dead)
                AddThreat(add, target, 500f);   // engage + point at the boss's target
        }
    }

    /// <summary>Caster-mob combat: no basic attack — nuke from range, jab up close, both gated on
    /// MP + reuse (via the normal cast pipeline). Out of MP for BOTH spells → stand helpless.</summary>
    private void MobCasterAi(Entity mob)
    {
        if (mob.CombatTargetId is not Guid targetId ||
            !_world.Entities.TryGetValue(targetId, out var target) || target.Dead ||
            DistanceSq(mob, target) > GameConstants.ViewRange * GameConstants.ViewRange)
        {
            Disengage(mob);
            return;
        }
        if (GameConstants.InSafeZone(target.X, target.Y))
        {
            ResetMob(mob);
            return;
        }

        var nuke = SkillCatalog.Get(SkillCatalog.MobNukeSkill)!;
        var bolt = SkillCatalog.Get(SkillCatalog.MobBoltSkill)!;
        int nukeCost = nuke.MpCostAt(mob.SkillLevelOf(nuke.Id));
        int boltCost = bolt.MpCostAt(mob.SkillLevelOf(bolt.Id));

        // No MP for EITHER spell → the mob is spent: stop and take it (players finish it off).
        if (mob.Mp < Math.Min(nukeCost, boltCost))
        {
            mob.TargetX = null;
            mob.TargetY = null;
            return;
        }

        bool nukeReady = !mob.SkillCooldowns.ContainsKey(nuke.Id) && mob.Mp >= nukeCost;
        bool boltReady = !mob.SkillCooldowns.ContainsKey(bolt.Id) && mob.Mp >= boltCost;
        float distSq = DistanceSq(mob, target);

        if (nukeReady && distSq <= nuke.Range * nuke.Range)
        {
            QueueMobSpell(mob, nuke.Id, targetId);
            return;
        }
        if (boltReady && distSq <= bolt.Range * bolt.Range)
        {
            QueueMobSpell(mob, bolt.Id, targetId);
            return;
        }
        // In nuke range but it's on cooldown → hold position and wait for the reuse.
        if (distSq <= nuke.Range * nuke.Range)
        {
            mob.TargetX = null;
            mob.TargetY = null;
            return;
        }
        // Too far for any spell → close to nuke range.
        mob.TargetX = target.X;
        mob.TargetY = target.Y;
    }

    private void QueueMobSpell(Entity mob, string skillId, Guid targetId)
    {
        mob.QueuedSkillId = skillId;
        mob.QueuedTargetId = targetId;
    }

    /// <summary>MP-cost multiplier from the caster's MP-cost buffs AND debuffs — PHYSICAL-category
    /// skills use the physical reduction, everything else (magic/buff/heal) the magic one.
    ///
    /// ⚠ It is NOT capped at 1. The reduction is clamped to [-2, +0.8] in <c>RecomputeDerived</c>, so
    /// this returns 0.2× (a fully buffed caster) through 3× (a fully debuffed one) — the ×3 is the
    /// owner's own worked example and the reason every MP question has to go through here.</summary>
    private static float MpCostFactor(Entity caster, SkillDef def) =>
        1f - (def.Category == SkillCategory.Physical
            ? caster.PhysMpCostReduction
            : caster.MagicMpCostReduction);

    /// <summary>What this skill COSTS this caster, in full: the authored total scaled by his MP-cost
    /// buffs/debuffs. This is the one number the player is quoted, the one the cast gate demands, and
    /// the one the 20/80 payment is sliced out of — never re-derive it at a call site.</summary>
    private static int EffectiveMpCost(Entity caster, SkillDef def, int skillLevel) =>
        (int)(def.MpCostAt(skillLevel) * MpCostFactor(caster, def));

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

        // A resurrect skill is the ONE cast that WANTS a dead target; every other skill drops it.
        if (target is null || (target.Dead && !def.Resurrect) ||
            (!selfTargeted && DistanceSq(caster, target) >
                GameConstants.ViewRange * GameConstants.ViewRange))
        {
            caster.QueuedSkillId = null;
            return;
        }

        float range = SkillMath.EffectiveRange(def, caster.Archetype, caster.BasicAttackRange, caster.Level, caster.SkillLevelOf(def.Id));

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
        // Cast time = base ticks scaled by the speed model (lower multiplier = faster).
        // PHYSICAL skills scale by ATTACK speed (AGI + weapon), not cast speed — a fighter
        // has poor WIT-driven cast speed, so making a melee strike depend on it made
        // physical skills feel sluggish. Magic/buff/heal skills still use cast speed.
        // Mobs cast at the skill's AUTHORED time (their low-WIT cast multiplier would otherwise
        // distort the tuned 1.5s/4s mob-spell timings); players use the speed model.
        float speedMult = def.FixedCast || caster.Kind == EntityKind.Mob ? 1f
            : def.Category == SkillCategory.Physical
                ? caster.EffectiveAttackSpeedMultiplier
                : caster.EffectiveCastSpeedMultiplier;
        caster.CastTicksRemaining = Math.Max(2,
            (int)(def.CastTicks * speedMult));

        // Charge the up-front slice — 20% of the EFFECTIVE total for every skill (the remaining 80%
        // lands with the cast). The gate in HandleCastSkill already proved he can afford all of it, so
        // the Math.Min is only a floor against an MP drain landing between the gate and here.
        int initialMp = SkillMath.InitialMpOf(EffectiveMpCost(caster, def, Math.Max(1, caster.SkillLevelOf(def.Id))));
        caster.CastInitialMpPaid = Math.Min(initialMp, caster.Mp);
        caster.Mp -= caster.CastInitialMpPaid;

        // Show the caster's CLASS-specific name for the skill (e.g. "Moonlight Bolt").
        string shown = ClassSkills.DisplayName(def.Id, caster.Race, caster.BaseClass,
            caster.Archetype, caster.Discipline);
        float castSeconds = caster.CastTicksRemaining * GameConstants.TickSeconds;
        if (_world.EntityToConnection.TryGetValue(caster.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Cast", new CastInfo(shown, castSeconds));
        }
        else if (caster.Kind == EntityKind.Mob)
        {
            // A MOB has no connection — broadcast a cast bar over its head to nearby players so a
            // boss's telegraphed slam is visible (and dodgeable/interruptible).
            var info = new MobCastInfo(caster.Id, shown, castSeconds);
            foreach (var nearby in _world.Grid.Nearby(caster))
                if (_world.EntityToConnection.TryGetValue(nearby.Id, out var pc))
                    _ = _hub.Clients.Client(pc).SendAsync("MobCast", info);
        }
    }

    private void ExecuteSkill(Entity caster, SkillDef def)
    {
        // The caster's learned LEVEL of this skill selects its per-level values
        // (Power / Magnitudes / MP). Default 1 for anything not in the learned set.
        int lvl = Math.Max(1, caster.SkillLevelOf(def.Id));
        // The REMAINDER of the price, not a second independently-rounded 80%: whatever the cast has
        // already paid comes off the effective total. That keeps the two halves summing to exactly the
        // number the player was quoted, and re-prices the balance if an MP-cost buff expired mid-cast.
        int finishMp = Math.Max(0, EffectiveMpCost(caster, def, lvl) - caster.CastInitialMpPaid);

        if (caster.Mp < finishMp)
        {
            SendSystemToEntity(caster, "Not enough MP.");
            CancelCast(caster);
            return;
        }

        // The same HP gate as at cast START (`55c`) — re-checked here because a cast roots you and
        // you can be beaten below the price while it runs. Without this the payment below would
        // still clamp you to 1 HP and land the skill for free.
        if (def.HpCostAt(lvl) is int hpPrice && hpPrice > 0 && caster.Hp <= hpPrice)
        {
            SendSystemToEntity(caster, "Not enough HP.");
            CancelCast(caster);
            return;
        }

        // The tutorial's "use a skill" beat (`58a`). Here rather than at cast START: a cast that was
        // interrupted or cancelled is not a skill you used, and past this point the MP is paid.
        AdvanceActionQuests(caster, QuestActions.UseSkill);

        // A HIDE ends when a skill EXECUTES (BL-69) — not when it is clicked. His rule, and it is
        // the reason a gap-closer works at all: *"i want to click the skill and im not in range to
        // start to move towards the target but still invisible once the skill is executed then i
        // appear."* So the break belongs here, past the point of no return, and the cast-start path
        // must leave a hide alone. A skill that GRANTS a hide re-applies it further down, after this.
        //
        // "a skill" means ANY skill, not just an offensive one — his list is "hitting, a skill, a
        // potion", with movement as the only exception.
        BreakHide(caster);

        bool selfTargeted = caster.CastTargetId == caster.Id;
        Entity? target = selfTargeted ? caster
            : caster.CastTargetId is Guid tid ? _world.Entities.GetValueOrDefault(tid) : null;

        // Resurrect is the ONLY skill that may target a DEAD ally; everything else needs a live target.
        if (target is null
            || (!def.Resurrect && target.Dead && target != caster)
            || (def.Resurrect && !target.Dead))
        {
            SendSystemToEntity(caster, def.Resurrect ? "You can only resurrect a fallen ally." : "Target lost.");
            return;
        }

        // The consumable that STARTED this cast (a buff scroll): take one unit now that it lands.
        // Gone from the bag mid-cast (traded, dropped, sold) = cancel without charging the finish MP,
        // exactly like a missing reagent.
        if (caster.CastFromItemInstance is Guid usedInstance)
        {
            caster.CastFromItemInstance = null;
            var used = caster.Inventory.FirstOrDefault(i => i.InstanceId == usedInstance);
            if (used is null)
            {
                SendSystemToEntity(caster, $"You no longer have the {def.Name.Replace("Scroll of ", "")} scroll.");
                CancelCast(caster);
                return;
            }
            ConsumeOne(caster, used);
            SendInventory(caster);
        }

        // Reagent: consume the required item now that the cast lands (re-check in case it
        // was traded/dropped mid-cast). Missing = cancel without charging the finish MP.
        if (!string.IsNullOrEmpty(def.ConsumableId))
        {
            if (!ConsumeItem(caster, def.ConsumableId, def.ConsumableAmount))
            {
                string itemName = ItemCatalog.Get(def.ConsumableId)?.Name ?? def.ConsumableId;
                SendSystemToEntity(caster, $"You no longer have {def.ConsumableAmount}x {itemName}.");
                CancelCast(caster);
                return;
            }
            SendInventory(caster);
        }

        // Cast already committed at start — no range re-check here; the spell
        // lands even if the target moved. Charge the remaining MP and start CD.
        caster.Mp -= finishMp;
        // Restore Spirit: HP→MP. The price is PER LEVEL (55 @25 … 200 @80), so it must be read
        // through HpCostAt — def.HpCost is only level 1's.
        if (def.HpCostAt(lvl) > 0) caster.Hp = Math.Max(1, caster.Hp - def.HpCostAt(lvl));
        caster.CastInitialMpPaid = 0;
        // Reuse-delay reduction (Spell Mastery / buffs) shortens the cooldown — unless the skill
        // has a FIXED cooldown (Return, ultimates).
        int cooldown = def.CooldownTicks;
        if (cooldown > 0 && !def.FixedCooldown && caster.CooldownReduction > 0f)
            cooldown = Math.Max(1, (int)(cooldown * (1f - caster.CooldownReduction)));
        caster.SkillCooldowns[def.Id] = cooldown;
        SendCooldowns(caster);   // the bar's reuse overlay starts the tick the reuse does

        // ---- Return: teleport the caster to the nearest safe town, then finish (the whole effect).
        if (def.TeleportsToTown)
        {
            ReturnToTown(caster);
            return;
        }

        // ---- Resurrect: revive a fallen ally (or self via a scroll) to 30% HP/MP and restore ResExpPct of
        //      the exp they lost to the death penalty, then finish. ----
        if (def.Resurrect)
        {
            OfferResurrect(caster, target, def.ResExpPctAt(lvl));
            return;
        }

        var effect = def.Effect;
        bool offensive = false;
        // The name shown in combat text is the CASTER's class label for this skill, so a
        // race's renamed spell (e.g. Elf "Moonlight Bolt") reads correctly in floating text.
        string castName = ClassSkills.DisplayName(
            def.Id, caster.Race, caster.BaseClass, caster.Archetype, caster.Discipline);

        // ---- Trap placement: drop the trap at the caster's feet and finish. Its damage/CC
        //      payload (this skill's Effect/Power) fires later, when a hostile trips it. ----
        if (def.PlacesTrap)
        {
            _world.Traps.Add(new TrapInstance
            {
                OwnerId = caster.Id, SkillId = def.Id, Level = lvl,
                X = caster.X, Y = caster.Y,
                Radius = def.TrapRadius, LifeTicks = Math.Max(1, def.TrapLifeTicks)
            });
            BroadcastCombat(caster, caster, 0, CombatOutcome.Buff, castName);
            SendSystemToEntity(caster, $"{castName} armed.");
            return;
        }

        // ---- Totem placement: plant it at the caster's feet and finish. Unlike a trap it fires
        //      immediately and then on its own timer, so standing in it pays from the first second
        //      rather than after a full pulse interval. ----
        if (def.PlacesTotem)
        {
            // ONE totem per owner PER SKILL — a recast moves it, it does not add a second one.
            // Not fussiness: his ladder authors cooldowns SHORTER than the lifetime (Healing Totem
            // is 25s reuse on a 30s totem), so without this every healer would run a permanent
            // stack of overlapping totems and the pulse would multiply by however many he could
            // squeeze up. Different totems (healing + mana) are different skills and coexist.
            _world.Totems.RemoveAll(t => t.OwnerId == caster.Id && t.SkillId == def.Id);
            _world.Totems.Add(new TotemInstance
            {
                OwnerId = caster.Id, SkillId = def.Id, Level = lvl,
                X = caster.X, Y = caster.Y,
                Radius = def.TotemRadius,
                // WHICH pool it fills comes from the skill's own Effect, so a Mana Totem is an
                // ordinary RestoreMp skill with PlacesTotem set — no second totem type, no new flag.
                Effect = effect,
                PulseAmount = def.PowerAt(lvl),
                PulseTicks = Math.Max(1, def.TotemPulseTicks),
                NextPulseIn = 0,
                LifeTicks = Math.Max(1, def.TotemLifeTicks)
            });
            BroadcastCombat(caster, caster, 0, CombatOutcome.Buff, castName);
            SendSystemToEntity(caster, $"{castName} planted.");
            return;
        }

        // ---- REVEAL (BL-69): drag every hidden character in radius back into view and bar them
        //      from hiding again. Deals nothing, so it never raises a mob clan. ----
        if (def.RevealsHidden)
            RevealHidden(caster, def, castName);

        // ---- HIDE (BL-69, kind 1): vanish from everything. Applied here, AFTER the generic
        //      "a completed cast reveals you" above, so casting the hide does not instantly undo it.
        //
        //      The reveal timing is the owner's and it is what makes a gap-closer work: you press a
        //      skill while out of range, you WALK to the target still invisible, and you appear when
        //      the skill executes. Nothing on the cast-START path may touch a hide, which is why the
        //      break lives at cast completion and not in HandleCastSkill. ----
        if (def.GrantsHide)
        {
            if (caster.NoHideTicks > 0)
            {
                SendSystemToEntity(caster, "You are marked — you cannot hide yet.");
            }
            else
            {
                caster.HideTicks = Math.Max(1, def.DurationTicks);
                DropAggroOn(caster);   // vanishing sheds mobs already locked on
                BroadcastCombat(caster, caster, 0, CombatOutcome.Buff, castName);
                SendSystemToEntity(caster, "You slip into the shadows.");
            }
        }

        // ---- Offensive AoE (boss slam): hit every hostile in radius, then finish. Uses the
        //      compact hit path (shared with traps). ----
        if (def.TargetMode == TargetMode.EnemiesInRadius)
        {
            foreach (var foe in EnemiesInRadius(caster, def.AreaRadius).ToList())
            {
                // BL-77: the flag lands on the REACH. Before the hit, so a miss, a zero roll or a
                // target who dies to the first victim's cleave still costs you the purple name —
                // you chose to sweep an area with players in it.
                FlagForPvpAction(caster, foe);
                DeliverSimpleHit(caster, foe, def, lvl, castName);
            }
            return;
        }

        // ---- Damage (physical) ----
        if (effect.HasFlag(SkillEffect.PhysicalDamage))
        {
            offensive = true;
            // BL-06 — A PHYSICAL SKILL IS NOT SUBJECT TO THE ACCURACY-vs-EVASION ROLL. His `69e`
            // ruling: *"normaly no1 can evade a physical skill … now on then i miss a skill which is
            // anoying - stab fails... then stab should land but misses ... no1 evades only rogues
            // gets a floor while in an ulitmate"*. So the ONLY ways a skill fails to land are the
            // defender's explicit grant (Evasion Boost, 25%) and total immunity.
            //
            // ⚠ This is why the caster's accuracy, the caster's HitFloor (Precision) and the
            // defender's EvadeFloor (Evasion Mastery) no longer appear here at all — they were the
            // thing being complained about. All three still govern BASIC attacks, untouched.
            float miss = target.Immune ? 1f : def.SureHit ? 0f : target.SkillEvadeChance;

            if (_rng.NextDouble() < miss)
            {
                BroadcastCombat(caster, target, 0, CombatOutcome.Miss, castName);
            }
            else
            {
                var (pFlat, pMod) = def.Id == SkillCatalog.TestPhysSkill
                    ? (_testSkillPower, _testSkillMod) : def.PhysDamageAt(lvl);   // test skill: live debug Flat/Mod
                int damage = StatCalculator.PhysicalDamageFM(
                    (int)caster.EffectiveAttack, pFlat, pMod,
                    (int)target.EffectiveDefence,
                    StatCalculator.WeaponDefenceCoef(caster.WeaponType, target.PierceDefCoef, target.BluntDefCoef, target.BowDefCoef));
                damage = (int)(damage * StatCalculator.WeaponVariance(caster.WeaponType, _rng));
                damage = FinalizeDamage(caster, target, damage, DamageKind.SkillPhysical, def);

                // DoT BURST: consume THIS skill's stack counter (by key — so only its own
                // applier line can detonate), multiplying damage by the stacks (×10 at full),
                // then remove the counter. The bleed DAMAGE effect itself is left in place.
                if (!string.IsNullOrEmpty(def.ConsumeStackKey) &&
                    target.Buffs.FirstOrDefault(b => b.Key == def.ConsumeStackKey) is BuffInstance ctr)
                {
                    damage = Math.Max(1, damage * ctr.Stacks);
                    target.Buffs.Remove(ctr);
                    target.RecomputeDerived();
                    if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
                }

                // FLAT crit damage joins pAtk inside the ratio, on a crit only — as a factor here
                // because everything after the ratio is linear (see StatCalculator.CritFlatFactor).
                float critFlat = StatCalculator.CritFlatFactor(
                    caster.EffectiveAttack, caster.CritDamageFlat, pFlat, pMod);

                // BLOW skills (dagger Stab) land full damage only on a crit, and a landed one is
                // computed with the crit-damage values, else a soft 10% floor. "[Double]" skills
                // roll a flat ×2 off the caster's ATK (2.5-25%). Everything else lands FLAT:
                // Can Crit and Can Double are exclusive OPT-IN flags (playtest-19 M8 — "if a skill
                // is not described as Can Crit or Can Double it doesn't do it"), which is why a
                // crit-less skill still goes through ResolvePhysicalCritAndBlock but with a zero
                // crit chance — it must keep the block roll. docs/design/CritBlowAndDouble.md.
                var (finalDmg, outcome) = def.BlowOnCrit
                    ? ResolveBlow(caster, target, damage, def, critFlat)
                    : def.CanDouble
                        ? ResolvePhysicalDouble(caster, target, damage,
                            StatCalculator.PhysicalDoubleChance(caster.AtkStat),
                            def.BlockAccuracy)
                        : ResolvePhysicalCritAndBlock(
                            caster, target, damage, def.CanCrit ? caster.CritChance * def.CritRateMod : 0f,
                            def.BlockAccuracy, critFlat);
                damage = finalDmg;
                BroadcastCombat(caster, target, damage, outcome, castName);
                ApplyDamage(target, damage, caster);
                ReflectPhysicalSkill(caster, target, damage, castName);   // BL-07
                TryInterruptCast(target, def.InterruptPower);
            }
        }

        // ---- Damage (magic) ----
        if (effect.HasFlag(SkillEffect.MagicDamage))
        {
            offensive = true;
            var (mFlat, mMod) = def.Id == SkillCatalog.TestMagicSkill
                ? (_testSkillPower, _testSkillMod) : def.MagicDamageAt(lvl);   // test skill: live debug Flat/Mod
            int damage = StatCalculator.MagicDamageFM(
                (int)caster.EffectiveMagicAttack, mFlat, mMod,
                (int)target.EffectiveMagicDefence,    // magic channel: divides by mDef
                target.MagicDefCoef);                 // ...times his MAGIC RESISTANCE (1.25 → ×0.8)
            damage = (int)(damage * StatCalculator.WeaponVariance(caster.WeaponType, _rng));
            damage = FinalizeDamage(caster, target, damage, DamageKind.SkillMagic, def);

            // WIT drives the caster's offensive magic interrupt power on top of the
            // skill's flat InterruptPower (Disrupt's 99999 still dominates).
            int magicInterrupt = def.InterruptPower + caster.MagicInterruptBonus;

            // Magic "fail" = reduced damage (not zero). Magic has its OWN formula, not the physical
            // resolver: 1.3^levelGap × the defender's anti-magic modifier × the CASTER'S OWN CHAIN
            // (MagicFailSelfMult — ×25 with a bow, divisible back out by a passive). See
            // StatCalculator.MagicFailChance.
            float fail = def.SureHit ? 0f
                       : target.Immune ? 1f
                       : StatCalculator.MagicFailChance(caster.Level, target.Level,
                             target.MagicFailMod,
                             caster.MagicFailSelfMult,
                             target.MagicFailBonus);
            // MANA RAY: the identical number, taken off MP instead of HP. Nothing above this line
            // knows or cares — same formula, same M.Def divisor, same mRes, same fizzle, same crit,
            // and the "half vs monsters" is the skill's own PveDamageMult, already applied by
            // FinalizeDamage. See SkillDef.DamageToMp.
            bool toMp = def.DamageToMp;
            if (_rng.NextDouble() < fail)
            {
                damage = Math.Max(1, damage / 3);
                ApplyDamage(target, damage, caster, toMp: toMp);
                TryInterruptCast(target, magicInterrupt);
                BroadcastCombat(caster, target, damage, CombatOutcome.Fail, castName);
            }
            else
            {
                if (_rng.NextDouble() < caster.MagicCritChance)
                {
                    // The MAGIC channel's own multiplier — NOT caster.CritDamageBonus. That field is
                    // Ferocity + the crit-damage item attribute, both authored for fighters; feeding
                    // it here made a mage's crit ride on buffs bought for the physical channel.
                    // `base x2 × multipliers × (1 − debuffs)` (owner 2026-08-19); it was a flat x3.
                    damage = (int)(damage * caster.EffectiveMagicCritDamage);
                    BroadcastCombat(caster, target, damage, CombatOutcome.Crit, castName);
                }
                else
                {
                    BroadcastCombat(caster, target, damage, CombatOutcome.Hit, castName);
                }
                ApplyDamage(target, damage, caster, toMp: toMp);
                TryInterruptCast(target, magicInterrupt);
            }

            // Vampiric: heal the caster for a fraction of the magic damage dealt
            // (the skill's own Lifesteal plus any Spell Vamp buff).
            // ⚠ NEVER off a mana hit: a Spell Vamp buff worn while casting Mana Ray would otherwise
            // turn someone else's MP into the caster's HP, which is a second drain nobody authored.
            float spellVamp = toMp ? 0f : def.Lifesteal + caster.SpellVamp;
            if (spellVamp > 0f && damage > 0)
            {
                int leech = (int)(damage * spellVamp);
                if (leech > 0) HealOne(caster, caster, leech, 0, castName);   // lifesteal = a FLAT heal
            }
        }

        // ---- Heal (single ally/self, or AoE to allies in radius) ----
        // TWO halves: a FLAT part (the skill's power through the healer's HealPower stats), plus an
        // optional % of the TARGET's max HP that ignores those and ignores heal-reduction. See HealOne.
        if (effect.HasFlag(SkillEffect.Heal))
        {
            // (The Divine Focus ×0.5/×0.75 non-magic-weapon scaling was removed here 2026-08-20 — see
            //  AutoLearnCoreSkills. A heal is now exactly power → HealPowerFlat/Mod → the target's own
            //  HealReceived*, with no weapon gate anywhere in the chain.)
            int flat = SkillMath.HealAmount(def.PowerAt(lvl), caster.HealPowerFlat, caster.HealPowerMod);
            float pct = def.MagnitudeOf(SkillEffect.Heal, ModifierMode.Percent, lvl);
            var helped = new HashSet<Guid>();
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                {
                    HealOne(caster, ally, flat, (int)(ally.MaxHp * pct), castName);
                    helped.Add(ally.Id);
                }
            else
            {
                HealOne(caster, target, flat, (int)(target.MaxHp * pct), castName);
                helped.Add(target.Id);
            }

            // A heal is aggro (BL-71), and it scales with HOW MANY it reached — his 2026-08-14 rule,
            // which puts a heal on the same footing as a buff: a per-head value times the heads.
            //
            // Deliberately computed from the AUTHORED power and cast time, not from the HP that
            // actually landed: it is a design number, so a full-HP target, a heal-reduction debuff or
            // the caster's weapon must not change who the monster hits.
            AddSupportThreat(caster, helped,
                SkillDef.SupportThreat(def.PowerAt(lvl), def.CastTicks, helped.Count));
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

        // ---- Cleanse / Cure — remove debuffs from an ally (or allies in radius). DispelMask
        //      narrows it (e.g. cure-poison = Poison|Venom); empty = all debuffs. ----
        if (effect.HasFlag(SkillEffect.Cleanse))
        {
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                    Dispel(caster, ally, def, positive: false, castName, lvl);
            else
                Dispel(caster, target, def, positive: false, castName, lvl);
        }

        // ---- Cancel / Dispel — strip POSITIVE buffs from an enemy (DispelCount = random N). ----
        if (effect.HasFlag(SkillEffect.Cancel))
        {
            offensive = true;
            Dispel(caster, target, def, positive: true, castName, lvl);
        }

        // ---- Crowd control + DoT (Slow/Stun/Fear/Root, Bleed/Poison/Venom) — lands via the
        //      contest (docs/design/Disciplines.md), NOT the fizzle model. Bosses are immune. The
        //      attacker stat is AGI for bleed/venom, ATK otherwise; defender CON (phys) / WIT (magic). ----
        // ---- [Double] on a BUFF or DEBUFF = DOUBLE DURATION (docs/design/CritBlowAndDouble.md §4,
        //      IG's level-76 Skill Mastery). The SAME ATK roll the damage side uses, rolled ONCE per
        //      cast — an area blessing doubles for everyone or for no one — and only for a PLAYER's
        //      own cast: potions, scrolls and the NPC buffer come through other paths and never roll.
        //      -1 = no override, i.e. the skill's authored duration. ----
        bool durationDoubled = def.DurationTicks > 0 && caster.Kind == EntityKind.Player
            && _rng.NextDouble() < StatCalculator.PhysicalDoubleChance(caster.AtkStat);
        int doubledTicks = durationDoubled ? def.DurationTicks * 2 : -1;

        if ((effect & SkillEffect.ContestCc) != 0)
        {
            offensive = true;
            // BL-08: the reflect roll comes FIRST — before the contest, because a bounced debuff is
            // not "resisted", it is redirected. A tank who bounces your stun is not tested against it,
            // and you are not tested against your own.
            if (!TryReflectDebuff(caster, target, def, lvl, doubledTicks, castName))
            {
                bool agiBased = (effect & (SkillEffect.Bleed | SkillEffect.Venom)) != 0;
                int atkStat = agiBased ? (int)caster.EffectiveAgi : caster.EffectiveAtk;
                int defStat = def.DebuffSchool == DebuffSchool.Magical ? target.EffectiveSpt : target.EffectiveCon;
                float land = target.Immune || BossShrugsOff(target, effect)
                    ? 0f
                    : StatCalculator.DebuffLandChance(atkStat, defStat,
                                                      RungLevel(caster, def, lvl), target.Level);
                land *= 1f - target.CcResist;   // gear/buff CC resistance lowers the land chance
                land *= 1f - SchoolCcResist(target, def.DebuffSchool);   // …and the per-school blessing
                if (_rng.NextDouble() < land)
                {
                    if ((effect & SkillEffect.AnyDot) != 0)
                        ApplyDotStack(caster, target, def, lvl);   // stacking DoT (refresh on reapply)
                    else
                        ApplyBuff(target, def, lvl, durationOverride: doubledTicks);   // single CC buff
                    BroadcastCombat(caster, target, 0, CombatOutcome.Buff,
                        durationDoubled ? castName + " [Double]" : castName);
                }
                else
                {
                    BroadcastCombat(caster, target, 0, CombatOutcome.Fail, castName);  // resisted
                }
            }
        }

        // ---- Debuffs (defence curse / anti-heal / root) — can fizzle like a spell ----
        //      (Contested CC above is excluded so it doesn't double-resolve.)
        if ((effect & SkillEffect.AnyDebuff & ~SkillEffect.ContestCc) != 0)
        {
            offensive = true;
            // BL-08 first, same rule as the contested branch above: a bounce is not a fizzle.
            if (!TryReflectDebuff(caster, target, def, lvl, doubledTicks, castName))
            {
                float fail = def.SureHit ? 0f
                           : target.Immune ? 1f
                           : StatCalculator.MagicFailChance(caster.Level, target.Level,
                                 target.MagicFailMod,
                                 caster.MagicFailSelfMult,
                                 target.MagicFailBonus);
                if (_rng.NextDouble() < fail)
                {
                    BroadcastCombat(caster, target, 0, CombatOutcome.Fail, castName);
                }
                else
                {
                    ApplyBuff(target, def, lvl, durationOverride: doubledTicks);
                    BroadcastCombat(caster, target, 0, CombatOutcome.Buff,
                        durationDoubled ? castName + " [Double]" : castName);
                }
            }
        }

        // ---- De-taunt — shed the caster's aggro from nearby foes ----
        if (effect.HasFlag(SkillEffect.Detaunt))
            Detaunt(caster);

        // ---- Taunt — force a mob's aggro onto the caster. TWO guarantees, deliberately separate
        //      (BL-71): you go to the TOP of the table and are locked there briefly, and then the
        //      skill's authored TauntPower is the CUSHION that decides whether you still hold it
        //      afterwards. The old rule was `top × 1.2 + 100` for every taunt at every level, which
        //      is not a number anyone can author against — 20% of the top is a rounding error once
        //      a DD is landing 7-8k a skill, which is exactly the complaint this comes from. ----
        if (effect.HasFlag(SkillEffect.Taunt) && target.Kind == EntityKind.Mob && !target.TrainingDummy)
        {
            offensive = true;
            int power = def.TauntPowerAt(lvl);
            if (power <= 0) power = 500;   // an unauthored taunt still does something

            // Jump to the top of the table FIRST, then add. Without the jump a taunt would only be
            // "+power" and could land you second; without the add it would be a 3s inconvenience.
            float top = 0f;
            foreach (var (id, v) in target.Threat)
                if (id != caster.Id && v > top) top = v;
            float mine = target.Threat.GetValueOrDefault(caster.Id);
            target.Threat[caster.Id] = Math.Max(mine, top) + power;

            target.CombatTargetId = caster.Id;
            target.Engaged = true;
            target.TauntLockTicks = def.DurationTicks > 0
                ? def.DurationTicks : GameConstants.TauntLockTicksDefault;
            BroadcastCombat(caster, target, 0, CombatOutcome.Buff, castName);
        }

        // ---- Movement: blink (move the caster) / knockback (shove the target). A blink with
        //      no target (self-cast escape) jumps away from the nearest hostile instead. ----
        if (effect.HasFlag(SkillEffect.Blink))
        {
            if (target != caster) { offensive = true; DoBlink(caster, target, def.BlinkRange); }
            else BlinkAwayFromNearest(caster, Math.Max(1f, def.BlinkRange));
        }
        if (effect.HasFlag(SkillEffect.Knockback)) { offensive = true; DoKnockback(caster, target, def.KnockbackRange); }

        // ---- Beneficial buffs (any of the buff flags, or a pure MARKER buff — Angel's Protection,
        //      the buffer's party stealth — which carries no stat effect at all) ----
        if ((effect & SkillEffect.AnyBuff) != 0 || def.KeepsBuffsOnDeath || def.GrantsMobStealth)
        {
            // The display name is the CASTER's class label for this skill, so a
            // cleric's Wind Walk shows as "Holy Speed" wherever it lands.
            string buffName = ClassSkills.DisplayName(
                def.Id, caster.Race, caster.BaseClass, caster.Archetype, caster.Discipline);
            // A doubled duration is announced on the floating text, or it is invisible.
            string shownName = durationDoubled ? buffName + " [Double]" : buffName;

            var blessed = new HashSet<Guid>();
            if (def.TargetMode == TargetMode.AlliesInRadius)
            {
                // Buff the caster + every nearby player character in range.
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                {
                    ApplyBuff(ally, def, lvl, buffName, durationOverride: doubledTicks);
                    BroadcastCombat(caster, ally, 0, CombatOutcome.Buff, shownName);
                    FlagForSupporting(caster, ally);   // `BL-59` — blessing an outlaw flags you
                    blessed.Add(ally.Id);
                }
            }
            else
            {
                var buffTarget = def.TargetMode == TargetMode.SelfOnly ? caster : target;
                ApplyBuff(buffTarget, def, lvl, buffName, durationOverride: doubledTicks);
                BroadcastCombat(caster, buffTarget, 0, CombatOutcome.Buff, shownName);
                FlagForSupporting(caster, buffTarget);   // `BL-59`
                blessed.Add(buffTarget.Id);
            }

            // A BUFF IS AGGRO TOO (BL-71, his 2026-08-14 ruling). Priced on the level the class learns
            // this rung at — not the caster's — because that is the difference he asked for: *"If I
            // learn a buff at 50 and another at 70 the 50 one should have less aggro value."* A skill
            // no class list owns (a buff scroll) has no such level, and only then does the caster's
            // own stand in.
            //
            // ⚠ A buff cast BEFORE the pull is worth nothing, and that is not an oversight: threat is
            // only handed to mobs already fighting somebody the cast helped. A buffer draws aggro for
            // re-buffing MID-FIGHT, which is exactly when he should. His own note that buffs run
            // "20 or so minutes" is what makes the big number safe.
            AddSupportThreat(caster, blessed,
                SkillDef.BuffThreat(RungLevel(caster, def, lvl), blessed.Count));
        }

        if (offensive)
            AfterOffensiveSkill(caster, target);

                if (target.Hp <= 0 && !target.Dead)
            Kill(target, caster);
    }

    /// <summary>The rank an IMPROVED (group) buff lands at. Far above any single's (a family ladder
    /// has at most six rungs), because a group is by definition the max version of everything it
    /// contains: no potion, scroll or single blessing may override one part of it. The LEVEL is added
    /// so a higher rank of the same group still replaces a lower one that is already running.</summary>
    private static int GroupRank(int level) => 100 + level;

    /// <summary>What a skill will actually put up: the family key it lands under, the rank it
    /// competes at, the families it covers (a group only) and how long it runs. A ONE-CHILD wrapper
    /// resolves to its child, because that is the buff that lands — the wrapper only lends it a
    /// duration. This is the single source of truth for both applying a buff and asking whether it
    /// WOULD apply.</summary>
    private static (string Key, int Rank, string[] Covered, int Duration) BuffPlan(SkillDef def, int level)
    {
        var kids = def.ChildBuffsAt(level);
        if (kids is { Length: 1 } && SkillCatalog.Get(kids[0]) is SkillDef only)
        {
            var inner = BuffPlan(only, 1);
            return (inner.Key, inner.Rank, inner.Covered, def.DurationTicks);
        }
        string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        if (kids is { Length: > 1 })
        {
            var covered = new List<string>(kids.Length);
            foreach (var childId in kids)
                if (SkillCatalog.Get(childId) is SkillDef child)
                    covered.Add(string.IsNullOrEmpty(child.BuffKey) ? child.Name : child.BuffKey);
            return (key, GroupRank(level), covered.ToArray(), def.DurationTicks);
        }
        return (key, def.Rank, Array.Empty<string>(), def.DurationTicks);
    }

    /// <summary>Would this buff land, or be refused by something stronger? Asked BEFORE a channelled
    /// consumable starts its cast — a buff scroll is taken from the bag when the cast LANDS, so
    /// without this you would spend a second reading a scroll and lose it for nothing.</summary>
    private static bool BuffWouldLand(Entity target, SkillDef def, int level)
    {
        var (key, rank, covered, duration) = BuffPlan(def, level);
        foreach (var b in target.Buffs)
        {
            if (!BuffsConflict(b, key, covered)) continue;
            if (rank < b.Rank) return false;
            if (rank == b.Rank && b.TicksRemaining > duration) return false;
        }
        return true;
    }

    /// <summary>Do these two occupy any of the same families? A single competes on its own key; a
    /// group competes on every family it covers. Groups that share nothing (Might and Bulwark vs
    /// Swift and Sure) coexist untouched.</summary>
    private static bool BuffsConflict(BuffInstance active, string key, string[] covered)
    {
        foreach (var f in active.Families)
        {
            if (f == key) return true;
            foreach (var c in covered) if (f == c) return true;
        }
        return false;
    }

    /// <summary>Apply a buff with the two stacking rules:
    /// (1) FAMILY conflict, then Rank: apply only if the incoming Rank >= the rank of every active
    ///     buff whose family set overlaps this one's (weaker is ignored entirely); on apply, replace
    ///     them. On EQUAL rank the one with the longer time left wins — potions and scrolls share
    ///     tiers and differ only in duration, so without that a 20-minute potion would silently eat
    ///     a 1-hour scroll.
    /// (2) Replaces: unconditionally remove any active buff whose key is listed,
    ///     regardless of rank or magnitude.
    /// A skill with ONE child hands out that child (the family's rung) and keeps only the duration
    /// and bar row; a skill with SEVERAL is an improved GROUP and lands as one covering buff.
    /// See docs/design/BuffLadders.md.
    /// Returns TRUE if anything actually landed: a consumable must not be eaten when it didn't.</summary>
    /// <param name="durationOverride">Ticks to run for, overriding the skill's own DurationTicks
    /// (-1 = use the skill's). A wrapper's child runs for the WRAPPER's duration.</param>
    /// <param name="sourceSkillId">The skill id stamped on the buff for the bar's icon
    /// (null = this skill's own). A wrapper stamps its child with the WRAPPER's id, so the square
    /// shows the potion or the blessing that produced it.</param>
    /// <param name="rowOverride">Which buff-bar row the effect lands in, overriding the skill's own
    /// (null = its own). A wrapper's child lands in the WRAPPER's row, so a potion's buff still
    /// shows as "from your bag" rather than in the buffer's row.</param>
    private bool ApplyBuff(Entity target, SkillDef def, int level = 1, string? displayName = null,
        bool refresh = true, bool toggle = false, int maxStacks = -1,
        int durationOverride = -1, string? sourceSkillId = null, BuffRow? rowOverride = null)
    {
        // ---- ONE-CHILD WRAPPER (a potion, a scroll, a buffer class's single blessing): it owns the
        //      duration and the bar row, but the buff that lands is the CHILD — the family's rung,
        //      under the family's key, at the family's rank. That is what lets a Greater potion and a
        //      cleric's Might compete: they are literally the same buff from different bottles. ----
        var kids = def.ChildBuffsAt(level);
        if (kids is { Length: 1 } && SkillCatalog.Get(kids[0]) is SkillDef onlyChild)
            return ApplyBuff(target, onlyChild, 1, refresh: refresh, toggle: toggle,
                             durationOverride: durationOverride >= 0 ? durationOverride : def.DurationTicks,
                             sourceSkillId: string.IsNullOrEmpty(sourceSkillId) ? def.Id : sourceSkillId,
                             rowOverride: rowOverride ?? def.BuffRow);

        // ---- IMPROVED (group) buff — MORE than one child. It is ONE buff carrying every child's
        //      numbers, on the group's own key, at GROUP rank, declaring the families it COVERS.
        //      Covering is the whole mechanic (docs/design/BuffLadders.md): the group removes the
        //      singles of those families on the way in, and no single, potion or scroll can override
        //      it afterwards — it is always the strongest version of everything it contains.
        //
        //      0.36-0.41 applied the children INDEPENDENTLY instead, so a potion could take over one
        //      part of a blessing. That is not how the game reads: an improved buff is the max-level
        //      version of its parts, and casting it over your bits and pieces is meant to be a
        //      straight upgrade. It also cost a bar square per part, which is what a 24-slot buff
        //      limit would spend its whole budget on. ----
        SkillEffect groupEffect = SkillEffect.None;
        EffectMagnitude[]? groupMags = null;
        bool isGroup = kids is { Length: > 1 };
        if (isGroup)
        {
            var mags = new List<EffectMagnitude>();
            foreach (var childId in kids!)
            {
                if (SkillCatalog.Get(childId) is not SkillDef child) continue;
                groupEffect |= child.Effect;
                mags.AddRange(child.MagnitudesAt(1) ?? Array.Empty<EffectMagnitude>());
            }
            groupMags = mags.ToArray();
        }

        // Key / rank / covered families come from the SAME resolver the "would this land?" test uses,
        // so a refusal message can never disagree with what actually happens.
        var (key, rank, covered, _) = BuffPlan(def, level);
        string shownName = string.IsNullOrEmpty(displayName) ? def.Name : displayName!;
        int eff = maxStacks >= 0 ? maxStacks : def.EffectiveMaxStacks;
        int duration = toggle ? int.MaxValue
                              : (durationOverride >= 0 ? durationOverride : def.DurationTicks);

        // Stacking effect (MaxStacks > 1): reapplying ADDS a stack (capped) and refreshes,
        // rather than replacing. If the skill has a per-stack table, the status re-snapshots
        // that level's Effect + Magnitudes (so a slow can grow, or become a freeze at stack N).
        if (eff > 1 && target.Buffs.FirstOrDefault(b => b.Key == key) is BuffInstance stack)
        {
            stack.Stacks = Math.Min(eff, stack.Stacks + 1);
            stack.MaxStacks = eff;
            stack.TicksRemaining = duration;   // refresh
            stack.AppliedAtTick = _tick;       // re-applied = young again, for the slot cap
            if (def.StackLevelAt(stack.Stacks) is StackLevel slv)
            {
                stack.Effect = slv.Effect;
                stack.Magnitudes = slv.Magnitudes;
            }
            if (refresh)
            {
                target.RecomputeDerived();
                if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
            }
            return true;
        }

        // Rule 1 — CONFLICT BY FAMILY, then rank. Two buffs conflict when their family sets overlap:
        // the same single twice, a single against a group that covers its family, or two groups that
        // share one. Disjoint groups (Might and Bulwark vs Swift and Sure) never see each other.
        var conflicts = target.Buffs.Where(b => BuffsConflict(b, key, covered)).ToList();
        foreach (var c in conflicts)
        {
            if (rank < c.Rank)
                return false;                   // weaker: do nothing (no refresh)
            // Equal rank = the same numbers from a different source (a potion and a scroll of the
            // tier are identical but for how long they last). Keep whichever runs LONGER, or a
            // 20-minute potion silently eats the 1-hour scroll you just read.
            if (rank == c.Rank && c.TicksRemaining > duration)
                return false;
        }
        foreach (var c in conflicts)
            target.Buffs.Remove(c);             // equal/stronger: full replace

        // Rule 2 — explicit Replaces list (unconditional).
        if (def.Replaces is { Length: > 0 })
            target.Buffs.RemoveAll(b => def.Replaces.Contains(b.Key));

        // Rule 3 — the SLOT CAP. Run last, after the two rules above have freed whatever they were
        // going to free, so a buff that merely replaces another never evicts a third by accident.
        BuffRow landingRow = rowOverride ?? def.BuffRow;
        if (CountsAgainstBuffCap(landingRow, toggle))
            EvictOldestBuffIfFull(target);

        // A leveled-stack effect starts at stack 1's entry; otherwise the skill's own effect.
        var first = def.StackLevelAt(1);
        target.Buffs.Add(new BuffInstance
        {
            Effect = isGroup ? groupEffect : (first?.Effect ?? def.Effect),
            Magnitudes = isGroup ? groupMags!
                       : first?.Magnitudes ?? def.MagnitudesAt(level) ?? Array.Empty<EffectMagnitude>(),
            CoveredKeys = covered,
            TicksRemaining = duration,
            Toggle = toggle,
            // STEALTH (BL-69, kind 2) rides on the buff, so it ends the moment the buff does — by
            // whichever of the many routes a buff can leave. See BuffInstance.HidesFromMobs.
            HidesFromMobs = def.GrantsMobStealth,
            // DoT damage effect (bleed/poison/venom): carries its per-tick damage so TickDots
            // hits for DotPower each second. Damage does NOT stack — stacks live on a separate
            // counter (see ApplyDotStack); the burst reads the counter, not this.
            DotPower = (def.Effect & SkillEffect.AnyDot) != 0 ? def.PowerAt(level) : 0,
            // Absorb shield: flat Power + a % of the target's max HP (a Percent Shield magnitude).
            ShieldPool = (def.Effect & SkillEffect.Shield) != 0
                ? def.PowerAt(level) + (int)(target.MaxHp * def.MagnitudeOf(SkillEffect.Shield, ModifierMode.Percent, level))
                : 0,
            MaxStacks = eff,
            AppliedAtTick = _tick,
            Cancellable = def.Cancellable,
            SourceRow = rowOverride ?? def.BuffRow,   // which buff-bar row this lands in (debuffs override it)
            // The skill whose icon the bar shows. For a one-child wrapper that is the WRAPPER (the
            // potion / the blessing), so a Swift potion and a cleric's Swift look like what cast them.
            SourceSkillId = string.IsNullOrEmpty(sourceSkillId) ? def.Id : sourceSkillId!,
            SkillId = def.Id,          // what can rebuild this exact buff (persistence saves THIS)
            Name = shownName,
            Key = key,
            Rank = rank,
            Level = level,
            Replaces = def.Replaces ?? Array.Empty<string>(),
            PhysMpCostPct = def.PhysMpCostPct,
            MagicMpCostPct = def.MagicMpCostPct,
            SkillEvadeChance = def.SkillEvadeChance,   // BL-06, rogue ultimate only
            EndsOnDamageTaken = def.EndsOnDamageTaken, // Meditation: gone the moment anything lands
            // Reward-rune payload, at the LEVEL that landed: a Rune of Experience (20%) is level 3 of
            // one ladder skill, so reading the def's own field here would hand out the +5% rung.
            Rewards = def.RewardsAt(level),
            KeepsBuffsOnDeath = def.KeepsBuffsOnDeath,
            AutoResurrect = def.AutoResurrect,
            // What the auto-res will hand back (`BL-35`). ResExpPctAt, not the def's bare field, for the
            // same reason Rewards uses RewardsAt just above: if these ever grow levels, the buff must
            // carry the rung that actually landed.
            AutoResExpPct = def.ResExpPctAt(level),
            // Clarity / Fortitude — per-LEVEL for the same reason as everything else here: the buff has
            // to carry the rung that actually landed, not the skill's first one.
            CcResistMagical = def.CcResistMagicalAt(level),
            CcResistPhysical = def.CcResistPhysicalAt(level),
            // Magic crit damage — per-LEVEL for the same reason.
            MagicCritDamage = def.MagicCritDamageAt(level),
            MagicCritDamageDebuff = def.MagicCritDamageDebuffAt(level),
            // The bar's tap popup reads the LEVEL's text whenever a level authored one — a group's
            // numbers live there ("Move +33, Cast +30%, Evasion +4, Attack Speed +33%"), and so do a
            // reward rune's ("+50% experience"), where the skill's own blurb is only the first rung.
            // DescriptionAt falls back to the skill's own text, so a level that authored none is
            // unchanged from when this read DescriptionOf(def.Id).
            Description = def.DescriptionAt(level)
        });

        // Re-bake derived stats (Max HP/MP, shield, atk/def) and refresh the owner's
        // HUD: buff icons AND the stats window (cast/attack/move speed are live and
        // read the buff list, so they need a fresh push to show the new numbers).
        // refresh=false lets a caller apply several buffs then refresh once (NPC buffer).
        if (refresh)
        {
            target.RecomputeDerived();
            if (target.Kind == EntityKind.Player)
            {
                PushBuffs(target);
                SendStats(target);
            }
        }
        return true;
    }

    /// <summary>Does a buff landing in this row occupy one of the <see cref="GameConstants.MaxBuffSlots"/>
    /// slots — and, equivalently, may it be evicted to make room for another?
    ///
    /// Only what a player COLLECTS: the buffer's blessings (row Buff) and what came out of the bag
    /// (row Consumable). Three things are deliberately outside the budget:
    ///   • DEBUFFS — you did not choose them. Counting them would let an enemy's poison push a
    ///     blessing off your bar, making every DoT a dispel; refusing them would make a full bar a
    ///     debuff immunity. Both are worse than not counting them.
    ///   • Row Item — armor sets, weapon abilities, the War/Spell Rune. These are re-applied by
    ///     reconciliation the moment they vanish, so evicting one buys a slot for a fraction of a
    ///     second and costs a flicker on the bar.
    ///   • TOGGLES — you switched it on and only you switch it off. A toggle that silently died
    ///     because you drank a potion would look like a bug, and re-arming it is a manual act.</summary>
    private static bool CountsAgainstBuffCap(BuffRow row, bool toggle) =>
        !toggle && row is BuffRow.Buff or BuffRow.Consumable;

    /// <summary>Make room for one more collected buff, dropping the OLDEST if the target is already at
    /// the cap (owner's rule: drop the oldest, never refuse the new one — a refusal arrives mid-fight
    /// and sends you hunting the bar for something to cancel). Loops rather than dropping exactly one,
    /// so a cap lowered at runtime settles instead of leaking a slot per cast.</summary>
    private void EvictOldestBuffIfFull(Entity target)
    {
        while (true)
        {
            var counted = target.Buffs.Where(b => CountsAgainstBuffCap(b.SourceRow, b.Toggle)).ToList();
            if (counted.Count < GameConstants.MaxBuffSlots) return;

            // Oldest by application, and among equals the one expiring soonest — on login every
            // restored buff shares a tick, and there "oldest" alone would be an arbitrary pick.
            var victim = counted
                .OrderBy(b => b.AppliedAtTick)
                .ThenBy(b => b.TicksRemaining)
                .First();
            target.Buffs.Remove(victim);
            if (target.Kind == EntityKind.Player)
                SendSystemToEntity(target,
                    $"{victim.Name} faded — you can only hold {GameConstants.MaxBuffSlots} buffs at once.");
        }
    }

    /// <summary>Apply a damage-over-time. Two SEPARATE statuses (the IG split): (1) the bleed
    /// DAMAGE effect — shared key, overrides by Rank (stronger wins), flat per-tick damage, does
    /// NOT stack, cure/cancel target it by flag+level; (2) a per-skill STACK COUNTER (StackKey,
    /// Internal) that just counts 1..Max and is what a burst consumes — independent of (1), so a
    /// stronger overriding bleed or a cure never touches another applier's stacks.</summary>
    private void ApplyDotStack(Entity caster, Entity target, SkillDef def, int level)
    {
        // (1) The damage effect — flat per-tick, overrides by Rank (force non-stacking so the
        // skill's MaxStacks, which governs the counter, doesn't stack the damage effect).
        ApplyBuff(target, def, level, refresh: false, maxStacks: 1);
        string dmgKey = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        if (target.Buffs.FirstOrDefault(b => b.Key == dmgKey) is BuffInstance dmg)
            dmg.SourceId = caster.Id;   // credit DoT kills to the applier

        // (2) The stack counter — separate, internal, per StackKey; max = the skill's MaxStacks.
        int cap = Math.Max(1, def.MaxStacks);
        if (!string.IsNullOrEmpty(def.StackKey))
        {
            var ctr = target.Buffs.FirstOrDefault(b => b.Key == def.StackKey);
            if (ctr is not null)
            {
                ctr.Stacks = Math.Min(cap, ctr.Stacks + 1);
                ctr.MaxStacks = cap;
                ctr.TicksRemaining = def.DurationTicks;   // refresh
                ctr.SourceId = caster.Id;
            }
            else
            {
                target.Buffs.Add(new BuffInstance
                {
                    Effect = SkillEffect.None,           // no stats: a pure counter
                    Magnitudes = Array.Empty<EffectMagnitude>(),
                    TicksRemaining = def.DurationTicks,
                    Stacks = 1,
                    MaxStacks = cap,
                    Internal = true,
                    SourceId = caster.Id,
                    Name = def.Name + " (stacks)",
                    Key = def.StackKey,
                });
            }
        }

        target.RecomputeDerived();   // secondary debuff magnitudes (slow etc.) take effect
        if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
    }

    /// <summary>Tick all damage-over-time effects on an entity once (per second): each DoT
    /// deals DotPower×Stacks; damage is credited to the applier (kill credit + drops).</summary>
    private void TickDots(Entity entity)
    {
        if (entity.Dead) return;
        int total = 0;
        Entity? source = null;
        foreach (var b in entity.Buffs)
        {
            if ((b.Effect & SkillEffect.AnyDot) == 0) continue;
            total += Math.Max(1, b.DotPower * b.Stacks);
            source ??= _world.Entities.GetValueOrDefault(b.SourceId);
        }
        if (total <= 0) return;
        var attacker = source ?? entity;
        ApplyDamage(entity, total, source);
        BroadcastCombat(attacker, entity, total, CombatOutcome.Hit, "DoT");
        if (entity.Hp <= 0 && !entity.Dead) Kill(entity, attacker);
    }

    /// <summary>Heal one target, scaled by its anti-heal multiplier, and broadcast.</summary>
    /// <summary>Heal a target. The two halves behave differently ON PURPOSE:
    ///   <paramref name="flat"/> — skill power × the healer's M.Atk. Heal-REDUCTION (anti-heal
    ///     debuffs, and the planned anti-heal ultimates) bites this half.
    ///   <paramref name="pct"/>  — a % of the target's max HP. Ignores M.Atk AND ignores
    ///     heal-reduction, so when a tank pops his anti-heal ultimate the big flat heals wither
    ///     and only the % heals still land. That's what % heals are FOR.
    /// </summary>
    private void HealOne(Entity caster, Entity target, int flat, int pct, string skillName)
    {
        if (target.Dead) return;
        // Target's HEAL RECEIVED: (flat + HealReceivedFlat)·HealReceivedMod (anti-heal debuffs lower the mod;
        // buffs/passives raise it). The % half (pct) ignores this, as before.
        int amount = (int)Math.Round((flat + target.HealReceivedFlat) * Math.Max(0f, target.HealReceivedMod)) + pct;
        if (amount > 0)
            target.Hp = Math.Min(target.MaxHp, target.Hp + amount);
        FlagForSupporting(caster, target);
        BroadcastCombat(caster, target, amount, CombatOutcome.Heal, skillName);
    }

    /// <summary>Supporting an OUTLAW makes you one: healing / restoring / cleansing a player who is
    /// FLAGGED (purple) or a PK (red) flags the supporter too. Otherwise a "clean" healer could prop
    /// up a PK from behind with no risk at all. Self-support never flags; an already-red supporter
    /// stays red (karma outranks the purple flag).</summary>
    private void FlagForSupporting(Entity caster, Entity target)
    {
        if (caster.Kind != EntityKind.Player || target.Kind != EntityKind.Player
            || caster.Id == target.Id)
            return;
        if (FlagOf(target) == PvpFlag.Innocent || FlagOf(caster) == PvpFlag.Pk)
            return;
        caster.PvpFlagUntilTick = _tick + PvpFlagTicks;
        SendPvpState(caster);
    }

    /// <summary>`BL-77` — FLAG ON THE REACH, NOT ON THE DAMAGE. A deliberate hostile act aimed at
    /// another player turns you purple even when it deals nothing at all: *"flare with pvp on reveals
    /// nearby players and Act as hit so flags"* · *"any skill that does no dmg and can be casted on a
    /// player if the PvP is off is (monster only) but if pvp is on it cast on a player and flags."*
    ///
    /// <para><see cref="ApplyDamage"/> already does this for anything that lands damage; this is the
    /// same rule for the acts that land none — a reveal, and every future no-damage hostile area
    /// skill. Same two exemptions as the damage path: you never flag against a PK (executing an outlaw
    /// is justified) and a PK stays red.</para>
    ///
    /// <para>🔑 Only the ACTOR flags. The player who was revealed or clipped did nothing, and flagging
    /// him is the exploit shape from `87a` in a different coat — one where the aggressor manufactures
    /// a legal victim. Support is not routed here either: a heal or a buff aimed at a stranger is
    /// castable on a player but is not an attack, and it has its own rule in
    /// <see cref="FlagForSupporting"/>. ⚠ Both of those are answers to open questions on the `BL-77`
    /// entry — they are the shapes every other system in this game already has, not his words.</para></summary>
    private void FlagForPvpAction(Entity caster, Entity target)
    {
        if (caster.Kind != EntityKind.Player || target.Kind != EntityKind.Player
            || caster.Id == target.Id)
            return;
        if (FlagOf(target) == PvpFlag.Pk || FlagOf(caster) == PvpFlag.Pk)
            return;
        caster.PvpFlagUntilTick = _tick + PvpFlagTicks;
        SendPvpState(caster);
    }

    /// <summary>Restore one target's MP and broadcast it (mirrors HealOne for the MP
    /// channel; used by MP Restore skills).</summary>
    private void RestoreMpOne(Entity caster, Entity target, int amount, string skillName)
    {
        if (target.Dead) return;
        if (amount > 0)
        {
            // Target's MP-RESTORE RECEIVED — the exact shape HealOne applies to a heal, and the
            // reason there is no longer a "periodic restores are different" branch here: a percent
            // scales with whatever landed, so ONE pipe serves a cast and a totem pulse alike
            // (owner 2026-08-19: *"the mana over time will go trough the same pipe as other mana
            // restores"*). The old flat +N had to be suppressed on ticks or it paid 30 times.
            amount = (int)Math.Round(amount * Math.Max(0f, target.RestoreMpMod));
            target.Mp = Math.Min(target.MaxMp, target.Mp + amount);
        }
        FlagForSupporting(caster, target);   // refuelling an outlaw flags you, same as healing one
        BroadcastCombat(caster, target, amount, CombatOutcome.ManaHeal, skillName);
        if (target.Kind == EntityKind.Player)
            SendStats(target);   // MP isn't surfaced via damage broadcasts — refresh the bar
    }

    /// <summary>Remove effects from a target — CURE (positive=false: strip the target's
    /// debuffs, e.g. cure-poison) or CANCEL (positive=true: strip an enemy's buffs). Honours
    /// the skill's DispelMask (effect filter), DispelMaxLevel (Rank ≤) and DispelCount
    /// (0 = all matching; N = up to N at random). Skips Internal and non-Cancellable effects.
    /// <para>The RANK CEILING is read PER LEVEL: a cure's ladder is how strong an ailment it can reach,
    /// not which ailments it knows — a level-3 Antidote takes a rank-5 bleed and leaves a rank-10
    /// poison alone.</para></summary>
    private void Dispel(Entity caster, Entity target, SkillDef def, bool positive, string skillName, int lvl = 1)
    {
        if (target.Dead) return;
        SkillEffect mask = def.DispelMask;
        int maxRank = def.DispelMaxLevelAt(lvl);
        var cands = target.Buffs.Where(b =>
            !b.Internal && b.Cancellable &&
            (positive ? !b.IsDebuff : b.IsDebuff) &&
            (mask == SkillEffect.None || (b.Effect & mask) != 0) &&
            (maxRank <= 0 || b.Rank <= maxRank)).ToList();

        // Random subset if a count is set and there are more candidates than that.
        if (def.DispelCount > 0 && cands.Count > def.DispelCount)
        {
            for (int i = 0; i < def.DispelCount; i++)
            {
                int j = _rng.Next(i, cands.Count);
                (cands[i], cands[j]) = (cands[j], cands[i]);
            }
            cands = cands.Take(def.DispelCount).ToList();
        }

        // CANCEL: each targeted buff rolls a SAVE against the victim's cancel resist — a saved
        // buff survives (so a high-resist tank keeps most of its buffs). Cure has no save.
        bool removedAny = false;
        foreach (var b in cands)
        {
            if (positive && target.CancelResist > 0f && _rng.NextDouble() < target.CancelResist)
                continue;   // resisted the cancel
            target.Buffs.Remove(b);
            removedAny = true;
        }
        if (removedAny)
        {
            target.RecomputeDerived();
            if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
        }
        BroadcastCombat(caster, target, 0, CombatOutcome.Buff, skillName);
    }

    /// <summary>De-taunt: nearby mobs targeting the caster drop most of their threat on the
    /// caster (so they retarget to someone else) and briefly won't re-aggro the caster.</summary>
    private void Detaunt(Entity caster)
    {
        const int window = 50;   // 5s at 10 ticks/s
        float rangeSq = GameConstants.MobAggroRange * GameConstants.MobAggroRange * 4f;
        foreach (var e in _world.Entities.Values)
        {
            if (e.Kind != EntityKind.Mob || e.Dead) continue;
            if (e.CombatTargetId != caster.Id) continue;
            if (DistanceSq(caster, e) > rangeSq) continue;
            e.Threat[caster.Id] = e.Threat.GetValueOrDefault(caster.Id) * 0.1f;   // shed 90% threat
            e.DetauntTicks = window;
            e.DetauntFromId = caster.Id;
            e.TauntLockTicks = 0;
            RetargetByThreat(e);   // hand aggro to the next-highest, if any
            if (e.CombatTargetId == caster.Id)   // no one else: drop combat, leash home
            {
                e.Engaged = false;
                e.CombatTargetId = null;
                e.TargetX = e.HomeX;
                e.TargetY = e.HomeY;
            }
        }
    }

    /// <summary>Move an entity to a point: clamp to the world, stop its current move, update
    /// the interest grid. Used by blink (caster) and knockback (target).</summary>
    private void PlaceEntity(Entity e, float x, float y)
    {
        e.X = Math.Clamp(x, GameConstants.WorldMinX, GameConstants.ZoneWidth);
        e.Y = Math.Clamp(y, GameConstants.WorldMinY, GameConstants.ZoneHeight);
        e.TargetX = null;
        e.TargetY = null;
        _world.Grid.UpdatePosition(e);
    }

    /// <summary>Blink the caster: range 0 = just behind the target (gap-closer); range &gt; 0
    /// = that far away from the target (escape).</summary>
    private void DoBlink(Entity caster, Entity target, float range)
    {
        float dx = target.X - caster.X, dy = target.Y - caster.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist < 0.01f) return;
        float nx = dx / dist, ny = dy / dist;
        if (range > 0f)   // blink AWAY from the target
            PlaceEntity(caster, caster.X - nx * range, caster.Y - ny * range);
        else              // blink to just behind the target
            PlaceEntity(caster, target.X + nx * (GameConstants.MeleeRange * 0.5f),
                                target.Y + ny * (GameConstants.MeleeRange * 0.5f));
    }

    /// <summary>Self-cast escape blink: jump <paramref name="range"/> away from the nearest
    /// hostile (the mob most likely chasing). No-op if nothing is near.</summary>
    private void BlinkAwayFromNearest(Entity caster, float range)
    {
        Entity? nearest = null; float bestSq = float.MaxValue;
        foreach (var e in _world.Grid.Nearby(caster))
        {
            if (e.Kind != EntityKind.Mob || e.Dead) continue;
            float d = DistanceSq(caster, e);
            if (d < bestSq) { bestSq = d; nearest = e; }
        }
        if (nearest is null) return;
        DoBlink(caster, nearest, range);   // range > 0 → blink away from it
    }

    /// <summary>Shove the target away from the caster by range.</summary>
    private void DoKnockback(Entity caster, Entity target, float range)
    {
        float dx = target.X - caster.X, dy = target.Y - caster.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float nx = dist < 0.01f ? 1f : dx / dist;
        float ny = dist < 0.01f ? 0f : dy / dist;
        PlaceEntity(target, target.X + nx * range, target.Y + ny * range);
    }

    // ===== INVISIBILITY (BL-69) =========================================================

    /// <summary>End a full HIDE (kind 1). Silent and free when there was none, so callers can just
    /// call it on any action rather than testing first.
    ///
    /// Deliberately touches NOTHING else: a stealth buff is not broken by acting (that is the
    /// difference between the two kinds), and admin invisibility is broken by nothing at all.</summary>
    private void BreakHide(Entity e)
    {
        if (e.HideTicks <= 0) return;
        e.HideTicks = 0;
        if (e.Kind == EntityKind.Player)
            SendSystemToEntity(e, "You step out of the shadows.");
    }

    /// <summary>Charge the per-second upkeep of any TOGGLE the entity is running (BL-69: Prowl's
    /// 1 MP/s). A stance nobody can pay for drops itself rather than running free.</summary>
    private void TickToggleUpkeep(Entity e)
    {
        List<BuffInstance>? broke = null;
        foreach (var b in e.Buffs)
        {
            if (!b.Toggle || string.IsNullOrEmpty(b.SkillId)) continue;
            if (SkillCatalog.Get(b.SkillId) is not SkillDef def || def.MpPerSecond <= 0) continue;
            if (e.Mp >= def.MpPerSecond) e.Mp -= def.MpPerSecond;
            else (broke ??= new()).Add(b);
        }
        if (broke is null) return;

        foreach (var b in broke)
        {
            e.Buffs.Remove(b);
            SendSystemToEntity(e, $"{b.Name} ends — not enough MP to hold it.");
        }
        e.RecomputeDerived();
        PushBuffs(e);
        SendStats(e);
    }

    /// <summary>Can this viewer see that entity at all? The one gate the world snapshot and every
    /// "pick a target" path share, so a thing nobody renders is also a thing nobody can click.
    ///
    /// HIDDEN MEANS HIDDEN FROM EVERYONE — party and staff included (owner, 2026-08-14: *"yes a hide
    /// hides you from all ... Also it hides you from the staff as well"*). This was built the narrow
    /// way first, exempting the hider's own party and staff on the reasoning that a party member no
    /// healer can reach is a bug report; he overruled it, and his answer covers the objection: you
    /// cannot die hidden, because taking or dealing damage reveals you first.
    ///
    /// 🔑 You are NOT removed from the party or from anything else — you simply *"act as u r not
    /// nearby"*. So the roster still lists you; what goes is being renderable, clickable and
    /// heal-targetable until you come back.
    ///
    /// Staff lose sight, not control: <c>/tp</c>, <c>/tpme</c>, <c>/jail</c> and <c>/where</c> all
    /// resolve a character by NAME and never consult this, which is deliberate — *"they still can
    /// teleport them self on you or you on them or can jail you ... for the 30 sec you are hidden
    /// they will live with it."*</summary>
    private static bool CanSee(Entity viewer, Entity e) =>
        e.Id == viewer.Id || !e.Hidden;

    /// <summary>The archer's answer to a rogue who is simply not there (BL-69): end every HIDE in
    /// radius and stamp those it caught so they cannot hide again for a while. The second half is
    /// the part that makes it a counter — a hide you can re-cast a heartbeat later is not countered,
    /// it is inconvenienced.
    ///
    /// <para>🔴 It walked <c>PlayersInRadius</c> until playtest 23, and that made it a NO-OP that could
    /// never catch anybody — *"Flare does nothing ...cannot find flagged player next to me. Doesn't
    /// cancel his vanish skill."* That helper is the PARTY-support enumeration: it returns the caster
    /// plus party members only, and it deliberately skips <c>e.Hidden</c> because a party heal must not
    /// silently find someone nobody can see. Both halves are exactly wrong here — the flare's whole
    /// subject is a hidden NON-party enemy. So the two rules cancelled: reveal only your own party,
    /// and never the hidden ones.</para>
    ///
    /// <para>This walks the grid itself instead — but through the same area filter every other AoE now
    /// uses (`BL-77`, playtest 24). It was side-less until then: *everyone* in radius was a candidate.
    /// His rule makes the PvP toggle the filter — *"any skill that does no dmg and can be casted on a
    /// player if the PvP is off is (monster only) but if pvp is on it cast on a player and flags"* — and
    /// a flare is exactly that skill. Nothing hides but players, so with PvP OFF this now catches
    /// nobody and says so; with it ON it catches anyone <see cref="CanPvpHit"/> would let you swing at
    /// (so: not your own party, not in town) and it FLAGS YOU, once, on the first person it reaches.
    /// That is his *"Act as hit so flags"* — the flare deals no damage, so no damage path can flag
    /// it.</para></summary>
    private void RevealHidden(Entity caster, SkillDef def, string castName)
    {
        int caught = 0;
        foreach (var e in EnemiesInRadius(caster, def.AreaRadius).ToList())
        {
            if (e.Kind != EntityKind.Player) continue;   // creatures do not hide (BL-69 is player-side)
            if (e.HideTicks <= 0 && def.NoHideTicks <= 0) continue;
            FlagForPvpAction(caster, e);   // BL-77: reaching him is the act, revealed or not
            if (e.HideTicks > 0) { BreakHide(e); caught++; }
            if (def.NoHideTicks > 0)
            {
                e.NoHideTicks = Math.Max(e.NoHideTicks, def.NoHideTicks);
                SendSystemToEntity(e, "The flare marks you — you cannot hide.");
            }
        }
        BroadcastCombat(caster, caster, 0, CombatOutcome.Buff, castName);
        SendSystemToEntity(caster,
            caught > 0 ? $"Signal flare: {caught} hidden {(caught == 1 ? "enemy" : "enemies")} revealed."
            : caster.Kind == EntityKind.Player && !caster.PvpEnabled
                ? "Signal flare: with PvP off it sweeps creatures only — no player was touched."
                : "Signal flare: nobody was hiding.");
    }

    /// <summary>Shed every mob currently aggro'd on an entity (used when it stealths): forget its
    /// threat and, if it's the current target, return the mob to wandering.</summary>
    private void DropAggroOn(Entity entity)
    {
        foreach (var mob in _world.Entities.Values)
        {
            if (mob.Kind != EntityKind.Mob) continue;
            mob.Threat.Remove(entity.Id);
            if (mob.CombatTargetId == entity.Id)
            {
                mob.CombatTargetId = null;
                mob.Engaged = false;
            }
        }
    }

    private void AfterOffensiveSkill(Entity caster, Entity target)
    {
        BreakHide(caster);   // acting reveals you (BL-69) — a stealth BUFF is untouched by this

        // BL-77, the SINGLE-TARGET half of the same hole the flare had: a hostile skill that lands no
        // damage — a taunt, a cancel, a debuff that was resisted — never touched ApplyDamage, so it
        // never flagged. Aiming one at a player is already gated on CanPvpHit (so with PvP off it was
        // refused, "monster only"); what was missing is that with PvP on it must cost you the purple
        // name. Idempotent with the damage path, which will have flagged you a moment ago.
        FlagForPvpAction(caster, target);

        // A skill NEVER starts a melee chase. The rule is the owner's and it has no class in it:
        // nothing walks you into melee range unless you explicitly commanded it (owner, playtest-15).
        // This used to read `BaseClass != Mage`, which spared the nuker and still charged everyone
        // else — but a bow rogue who opens with a shot did not ask to close, and neither does anyone
        // else who only pressed a skill.
        //
        // What survives is the melee combo, because that IS commanded: HandleAttack records the order
        // in AttackCommandTargetId and a cast does not clear it, so a fighter who tapped Attack and
        // then fired a skill at the SAME target resumes swinging afterwards. Mobs are unaffected —
        // their AI is not taking orders from a hot bar.
        bool commanded = caster.Kind != EntityKind.Player || caster.AttackCommandTargetId == target.Id;
        if (!target.Dead && commanded)
        {
            caster.CombatTargetId = target.Id;
            caster.Engaged = true;
        }

        Retaliate(target, caster);
    }

    private void Retaliate(Entity victim, Entity attacker)
    {
        // Being targeted by an offensive action always provokes a mob — even a non-damaging
        // one (debuff/CC) — so add a little threat (damage adds the rest in ApplyDamage).
        // Training dummies never aggro.
        if (victim.Kind == EntityKind.Mob && !victim.Dead && !victim.TrainingDummy)
            AddThreat(victim, attacker, 1f);
    }

    /// <summary>Add aggro to a mob's threat table and (re)target the highest-threat foe,
    /// unless a taunt is currently locking it. Only player actions build threat.</summary>
    private void AddThreat(Entity mob, Entity attacker, float amount)
    {
        if (attacker.Kind != EntityKind.Player) return;
        mob.Threat[attacker.Id] = mob.Threat.GetValueOrDefault(attacker.Id) + amount;
        mob.Engaged = true;
        if (mob.TauntLockTicks <= 0 || mob.CombatTargetId is null)
            RetargetByThreat(mob);
    }

    /// <summary>Bleed an engaged mob's threat table once per second (BL-71).
    ///
    /// The decay is PROPORTIONAL, so on its own it can never re-order the table — it changes no
    /// decision the very tick it runs. What it changes is the size of the ABSOLUTE gaps, and that is
    /// the point: a taunt's cushion is a flat number, so a tank who taunts once at the pull and then
    /// stops is overtaken by the party's damage a minute later, exactly as he should be. It is also
    /// what keeps a healer's contribution recent rather than cumulative over a ten-minute fight.
    ///
    /// Entries that fall under the floor are dropped, which is the only pruning the table gets for
    /// someone who threw one debuff and walked away.</summary>
    private static void DecayThreat(Entity mob)
    {
        if (mob.Threat.Count == 0) return;
        List<Guid>? drop = null;
        foreach (var id in mob.Threat.Keys.ToList())
        {
            float v = mob.Threat[id] * GameConstants.ThreatDecayPerSecond;
            if (v < GameConstants.ThreatFloor) (drop ??= new()).Add(id);
            else mob.Threat[id] = v;
        }
        if (drop is not null)
            foreach (var id in drop) mob.Threat.Remove(id);
    }

    /// <summary>Support threat (BL-71, owner's rule): a heal is worth <c>power / castSeconds × 10</c>
    /// to every monster that is currently fighting somebody it helped.
    ///
    /// Before this a healer was invisible to every mob in the game — nothing but damage and a
    /// 1-point poke from an offensive cast ever wrote to a threat table, so the one class whose whole
    /// job is to undo the party's damage intake contributed nothing to the decision of who gets hit.
    ///
    /// "Fighting somebody it helped" is the whole targeting rule: the mobs that care are the ones
    /// whose current target, or whose threat table, already contains a healed ally. A heal cast in
    /// another zone therefore costs nothing, and one cast is counted ONCE per mob however many
    /// allies of that mob's fight it topped up.</summary>
    private void AddSupportThreat(Entity caster, HashSet<Guid> helped, float amount)
    {
        if (amount <= 0f || helped.Count == 0 || caster.Kind != EntityKind.Player) return;
        foreach (var mob in _world.Grid.Nearby(caster))
        {
            if (mob.Kind != EntityKind.Mob || mob.Dead || mob.TrainingDummy || !mob.Engaged) continue;
            bool fightingOne = (mob.CombatTargetId is Guid t && helped.Contains(t))
                            || mob.Threat.Keys.Any(helped.Contains);
            if (fightingOne) AddThreat(mob, caster, amount);
        }
    }

    /// <summary>A wounded mob calls its social clan (BL-70): every clanmate within
    /// <see cref="GameConstants.MobClanCallRadius"/> that is not already in a fight joins this one.
    ///
    /// Three deliberate limits, each of which is the difference between a camp and a zone-wide riot:
    ///   • it fires on DAMAGE only, and only ONCE per fight (<see cref="Entity.CriedForHelp"/>);
    ///   • a clanmate already fighting somebody is left alone — it does not switch victims;
    ///   • an answering mob is seeded with the same threat a pull is worth, so the person who
    ///     started it owns the whole camp rather than whoever happens to hit each one first.
    ///
    /// The answering mobs do NOT cry in turn. A chain would take a settlement apart in one pull, and
    /// the radius is the fight's size — not a fuse.</summary>
    private void CryForHelp(Entity mob, Entity attacker)
    {
        // 🔴 OFF since playtest 23 (BL-73) — see GameConstants.MobClansEnabled for why, and note that
        // the reason is the world's spawn density, not anything in this method.
        if (!GameConstants.MobClansEnabled) return;
        if (mob.CriedForHelp || mob.TrainingDummy || attacker.Kind != EntityKind.Player) return;
        string clan = MobCatalog.Get(mob.MobTypeId).Clan;
        if (string.IsNullOrEmpty(clan)) return;

        mob.CriedForHelp = true;
        float r2 = GameConstants.MobClanCallRadius * GameConstants.MobClanCallRadius;
        foreach (var friend in _world.Grid.Nearby(mob))
        {
            if (friend.Kind != EntityKind.Mob || friend.Dead || friend.Id == mob.Id) continue;
            if (friend.TrainingDummy || friend.Engaged) continue;
            if (!string.Equals(MobCatalog.Get(friend.MobTypeId).Clan, clan, StringComparison.Ordinal))
                continue;
            if (DistanceSq(mob, friend) > r2) continue;
            if (GameConstants.InSafeZone(friend.X, friend.Y)) continue;

            friend.CriedForHelp = true;   // it came to a cry; it does not raise one of its own
            AddThreat(friend, attacker, friend.MaxHp * GameConstants.ThreatAggroPullFraction);
            friend.CombatTargetId = attacker.Id;
            friend.Engaged = true;
        }
    }

    /// <summary>Point the mob at its highest-threat living target (stale/dead entries skipped).</summary>
    private void RetargetByThreat(Entity mob)
    {
        Guid? best = null; float bestV = -1f;
        foreach (var (id, v) in mob.Threat)
        {
            if (v <= bestV) continue;
            // Hidden means hidden from the AI too (BL-69). DropAggroOn already wipes a hider from
            // every table, so this is the belt to that braces — a threat entry written in the same
            // tick someone vanished must not hand them the mob back.
            if (_world.Entities.TryGetValue(id, out var e) && !e.Dead && !e.Hidden) { bestV = v; best = id; }
        }
        if (best is Guid g) mob.CombatTargetId = g;
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
        entity.AttackCommandTargetId = null;   // the order died with the target (or it left view)
        if (entity.Kind == EntityKind.Mob)
            ResetMob(entity);
    }

    private void ResolveBasicAttack(Entity attacker, Entity target)
    {
        BreakHide(attacker);   // attacking reveals you (BL-69)

        // 🔴 A BASIC ATTACK CREDITS THE TUTORIAL'S "use it on something" BEAT TOO (him, 63j: "Fighter
        // dont have a skill (I had to use TestSkill) to continue with quest"). The beat used to be
        // credited only from the cast-completed path, and a level-1 fighter has no castable skill at
        // all — so the step that teaches you to attack was unreachable by the class that attacks.
        // His own re-spec spells the pair out ("put atkAction or bolt to bar", "use skill/atkAction"),
        // so the basic attack is a first-class answer here, not a fallback.
        if (attacker.Kind == EntityKind.Player)
            AdvanceActionQuests(attacker, QuestActions.UseSkill);

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
                (int)target.EffectiveDefence, attacker.Level,
                StatCalculator.WeaponDefenceCoef(attacker.WeaponType, target.PierceDefCoef, target.BluntDefCoef, target.BowDefCoef));
            damage = (int)(damage * StatCalculator.WeaponVariance(attacker.WeaponType, _rng));
            damage = FinalizeDamage(attacker, target, damage, DamageKind.Basic, null);

            // A basic attack is K·pAtk/def, so the flat crit-damage add is simply (pAtk+flat)/pAtk.
            var (finalDmg, outcome) = ResolvePhysicalCritAndBlock(
                attacker, target, damage, attacker.CritChance, 0f,
                StatCalculator.CritFlatFactor(attacker.EffectiveBasicAttack, attacker.CritDamageFlat));
            damage = finalDmg;
            BroadcastCombat(attacker, target, damage, outcome);
            ApplyDamage(target, damage, attacker);
            // Melee basic-attack vampirism (Might lvl 4 etc.) — bow attacks don't leech.
            if (attacker.MeleeVamp > 0f && damage > 0 && attacker.WeaponType != WeaponType.Bow)
            {
                int leech = (int)(damage * attacker.MeleeVamp);
                if (leech > 0) HealOne(attacker, attacker, leech, 0, "Vampiric");   // lifesteal = a FLAT heal
            }
            // Melee reflect (counter to vamp): return a fraction of the taken damage to the
            // attacker. MELEE only (bows excluded); applied directly, so it never re-reflects.
            if (target.MeleeReflect > 0f && damage > 0 && attacker.WeaponType != WeaponType.Bow)
            {
                int reflected = (int)(damage * target.MeleeReflect);
                if (reflected > 0)
                {
                    // `reflected: true` — the armour set answers for itself; it never flags its wearer (87a).
                    reflected = ApplyDamage(attacker, reflected, target, reflected: true);
                    BroadcastCombat(target, attacker, reflected, CombatOutcome.Hit, "Reflect");
                    if (attacker.Hp <= 0) Kill(attacker, target);
                }
            }
            // Rogues carry magic-interrupt power on basic attacks; others = 0.
            TryInterruptCast(target, attacker.BasicAttackInterruptPower);
        }

        Retaliate(target, attacker);

        if (target.Hp <= 0)
            Kill(target, attacker);
    }

    /// <summary>Apply damage unless the target is in god mode.
    ///
    /// <para>🔑 <paramref name="reflected"/> — THE FLAG FOLLOWS INTENT (playtest 24, `87a`). Reflect
    /// damage runs through here with the roles SWAPPED — the defender becomes the `attacker` argument —
    /// so the flag block below was turning the *defender* purple for a blow he never struck. His words:
    /// *"Reflect should not flag me — that's a big anti pk exploit ... som1 comes to me and wants to
    /// kill me but I don't want to ..so he hits me see I become pvp flag and he just kills me."* An
    /// aggressor could farm consent by walking into a reflect. What your GEAR does back to an attacker
    /// on its own is not an act of yours, so it flags nobody and records no attacker — the pairing to
    /// `BL-77`, where what you deliberately do with PvP on flags you even when it deals nothing.</para>
    ///
    /// <para>⚠ Everything else about a reflect is unchanged: it still damages, still kills, and the
    /// kill is still a justified PvP kill rather than a PK, because the aggressor flagged himself with
    /// his own blow one line earlier.</para></summary>
    /// <param name="toMp">MANA damage (the healer's Mana Ray): everything a hit does still happens —
    /// the combat timer, PvP flagging, the hide break, threat, the damage ledger — but the number comes
    /// off MP instead of HP, and the three HP-only defences in the middle of this method are skipped:
    /// an absorb shield soaks blows, a mana shield diverting mana damage into mana is a loop, and a
    /// lethal save has nothing to save from since MP simply floors at 0. One method rather than a
    /// parallel one, so the flagging and threat rules above can never drift apart between them.</param>
    private int ApplyDamage(Entity target, int damage, Entity? attacker = null, bool reflected = false,
        bool toMp = false)
    {
        if (target.GodMode)
            return 0;

        bool pvp = damage > 0 && attacker is { Kind: EntityKind.Player } && target.Kind == EntityKind.Player;

        // PvP safety: a player can't damage another player inside a safe zone (someone ran to town).
        if (pvp && (GameConstants.InSafeZone(target.X, target.Y) || GameConstants.InSafeZone(attacker!.X, attacker.Y)))
            return 0;

        // Combat state: any damage dealt/taken (re)starts the 30s combat timer on the players
        // involved (gates exit/teleport; drives the disconnect fate).
        if (damage > 0)
        {
            if (target.Kind == EntityKind.Player) target.LastCombatTick = _tick;
            if (attacker is { Kind: EntityKind.Player }) attacker.LastCombatTick = _tick;
        }

        // PvP flagging: the victim records its attacker (for counter-attack), and the ATTACKER goes
        // PURPLE (freely attackable) — unless the target is a PK (attacking a red player is justified,
        // no flag). An already-red attacker stays red (FlagOf prioritises karma).
        // `reflected` skips BOTH halves (see the summary): a reflect is not an act of its owner's, so
        // it neither flags him nor lets the aggressor claim him as "the man who attacked me".
        if (pvp && !reflected)
        {
            target.LastPvpAttackerId = attacker!.Id;
            if (FlagOf(target) != PvpFlag.Pk)
                attacker.PvpFlagUntilTick = _tick + PvpFlagTicks;
        }

        // Taking damage reveals a hidden character (BL-69). This is what makes his "any AoE damage
        // also reveals" true without a special case in every AoE: an area hit is positional, so it
        // finds someone who is hidden, and the hit itself is what drags them out.
        if (damage > 0 && target.HideTicks > 0)
            BreakHide(target);

        // …and it ends a buff that said it could not survive being hit (the healer's Meditation). Here
        // for the same reason the hide break is here: this is the ONE place every source of damage
        // arrives, so a DoT tick, an AoE, a reflect and an ordinary swing all end it for free. A mana
        // hit counts — being hit is being hit, whichever pool paid for it.
        if (damage > 0 && target.Buffs.Any(b => b.EndsOnDamageTaken))
        {
            target.Buffs.RemoveAll(b => b.EndsOnDamageTaken);
            target.RecomputeDerived();
            if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
        }

        // Threat: damage to a mob from a known attacker builds aggro (retargets to top threat).
        if (attacker is not null && target.Kind == EntityKind.Mob && damage > 0)
        {
            AddThreat(target, attacker, damage);

            // …and the SOCIAL CLAN cry (BL-70). Right here, in the damage path, is the whole rule:
            // his ruling is that a mob calls its camp when it *starts to take damage* and at no other
            // moment — not on a taunt, not on a debuff, not on walking into aggro range. Putting the
            // call anywhere else would break the lure, which is the tactic this exists to enable.
            CryForHelp(target, attacker);

            // …and the DAMAGE ledger, which is what rewards are actually paid on. Threat can't serve
            // that role: taunt and detaunt move it around by design, so it answers "who is the mob
            // angry at", not "who earned this". Only PLAYER damage is banked — a mob killed by another
            // mob, or by a trap, owes nobody.
            if (attacker.Kind == EntityKind.Player)
                target.DamageLog[attacker.Id] =
                    target.DamageLog.TryGetValue(attacker.Id, out var had) ? had + damage : damage;
        }

        // ---- MANA damage (Mana Ray) leaves here. Everything above applies to it unchanged; the three
        //      HP defences below do not (see the `toMp` remark on this method). MP floors at 0, so this
        //      branch can never kill and never calls Kill — a drained caster is disarmed, not dead.
        if (toMp)
        {
            int drained = Math.Min(damage, target.Mp);
            target.Mp -= drained;
            // Being hit while sitting breaks the sit, exactly as a normal blow does below.
            if (target.Kind == EntityKind.Player && target.MoveState == MoveState.Sitting)
            {
                target.MoveState = MoveState.Running;
                target.StandUpTicks = MovementTuning.StandUpTicks;
            }
            if (target.Kind == EntityKind.Player) SendStats(target);
            return drained;
        }

        // Absorb shields soak damage before HP; a depleted shield is removed.
        if (damage > 0 && target.Buffs.Any(b => b.Has(SkillEffect.Shield) && b.ShieldPool > 0))
        {
            bool changed = false;
            for (int i = target.Buffs.Count - 1; i >= 0 && damage > 0; i--)
            {
                var b = target.Buffs[i];
                if (!b.Has(SkillEffect.Shield) || b.ShieldPool <= 0) continue;
                int absorbed = Math.Min(b.ShieldPool, damage);
                b.ShieldPool -= absorbed;
                damage -= absorbed;
                if (b.ShieldPool <= 0) { target.Buffs.RemoveAt(i); changed = true; }
            }
            if (changed && target.Kind == EntityKind.Player) PushBuffs(target);
        }

        // Mana shield: divert a fraction of the remaining damage to MP (rate = MP per 1 dmg),
        // limited by available MP.
        if (damage > 0 && target.Mp > 0 &&
            target.Buffs.FirstOrDefault(b => b.Has(SkillEffect.ManaShield)) is BuffInstance ms)
        {
            float frac = ms.Percent(SkillEffect.ManaShield);
            float rate = ms.Flat(SkillEffect.ManaShield);
            if (frac > 0f && rate > 0f)
            {
                int divert = Math.Min((int)(damage * frac), (int)(target.Mp / rate));
                if (divert > 0)
                {
                    target.Mp -= (int)(divert * rate);
                    damage -= divert;
                }
            }
        }

        // Lethal save: a fatal blow is survived once, reviving to a % of max HP (buff consumed).
        if (damage > 0 && target.Hp - damage <= 0 &&
            target.Buffs.FirstOrDefault(b => b.Has(SkillEffect.LethalSave)) is BuffInstance save)
        {
            target.Buffs.Remove(save);
            target.Hp = Math.Max(1, (int)(target.MaxHp * save.Percent(SkillEffect.LethalSave)));
            if (target.Kind == EntityKind.Player) { PushBuffs(target); SendStats(target); }
            return 0;
        }

        target.Hp -= damage;

        // Training dummy never dies: it takes (and displays) the hit but floors at 1 HP.
        //
        // NPCs get the same floor. Vendors, teleporters and quest givers are furniture with a job —
        // killing one silently removes a service from the world. Guarding the ATTACK command was not
        // enough: it only covered basic attacks, and skills (and DoTs, and AoE) reach damage by other
        // routes, so an NPC could still be killed with a nuke. This line is the ONE place HP ever
        // decreases, which makes it the only place the rule cannot be routed around.
        //
        // FUTURE: PK guards are meant to be mortal but nearly unkillable — a tank + healer + damage
        // dealer should be able to bring one down if they really want to, like a small raid boss with
        // no drop and no XP, respawning after ~5 minutes. That will need its own NpcRole rather than
        // an exception here (see docs/Roadmap.md).
        if ((target.TrainingDummy || target.Kind == EntityKind.Npc) && target.Hp < 1) target.Hp = 1;

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
        // Death ends a hide (BL-69). Damage already reveals you before it lands, so his *"there
        // should be no way u die In a hidden state"* holds by construction — this is the belt to that
        // brace, and it is what keeps a CORPSE findable and resurrectable by the party.
        victim.HideTicks = 0;
        victim.Engaged = false;
        victim.CombatTargetId = null;
        victim.AttackCommandTargetId = null;
        victim.QueuedSkillId = null;
        // A class change in progress dies with you (`BL-36`). ⚠ Mine, not his: he ruled on where the
        // timer runs, not on what interrupts it. Cancelling is the conservative half — the alternative
        // is standing up as a level-1 subclass you no longer wanted to be, which is a worse surprise
        // than being told to ask again.
        if (victim.PendingSubclassSlot >= 0)
        {
            victim.PendingSubclassSlot = -1;
            victim.SubclassSwapTicks = 0;
            SendSystemToEntity(victim, "Your class change was interrupted by your death.");
        }
        // Through CancelCast, not a bare `CastingSkillId = null`: cancelling is what PUSHES the cast
        // bar's clear (a mob's to everyone nearby, a player's to themselves). Killing a caster
        // mid-spell left that bar hanging over its corpse until it happened to time out.
        CancelCast(victim);
        victim.TargetX = null;
        victim.TargetY = null;
        // Angel's Protection (noblesse): if a "keeps buffs on death" buff is up, death removes ONLY the
        // protection buff(s) and every other buff survives; otherwise death clears all buffs as usual.
        // Preservation buffs share BuffKey "buff_preservation" + Rank, so only the strongest is ever held —
        // one is consumed here. A consumed buff flagged AutoResurrect also auto-revives (handled below).
        bool keptBuffs = victim.Buffs.Any(b => b.KeepsBuffsOnDeath);
        // The auto-res half (`BL-35`), read BEFORE the buffs are removed just below — the buff is the
        // only thing that knows how much exp was promised, and it is about to be consumed. Max across
        // any that are up: the family key keeps one, but if two ever coexist the better promise wins
        // rather than whichever happened to be first in the list.
        bool autoRes = false;
        float autoResExpPct = 0f;
        foreach (var b in victim.Buffs)
            if (b.KeepsBuffsOnDeath && b.AutoResurrect)
            {
                autoRes = true;
                autoResExpPct = Math.Max(autoResExpPct, b.AutoResExpPct);
            }
        if (keptBuffs)
            victim.Buffs.RemoveAll(b => b.KeepsBuffsOnDeath);
        else
            victim.Buffs.Clear();
        // Refresh the survivor's HUD when buffs were KEPT (their stats/bar must reflect what's still up).
        if (keptBuffs && victim.Kind == EntityKind.Player)
        {
            victim.RecomputeDerived();
            PushBuffs(victim);
            SendStats(victim);
        }

        BroadcastCombat(killer, victim, 0, CombatOutcome.Death);

        if (victim.Kind == EntityKind.Mob)
        {
            // Last hit is RECORDED but is no longer what rewards are paid on (owner: keep it as a
            // counter for raid/epic bosses). The reward "killer" is the top DAMAGER, so a lucky final
            // blow can't steal a mob somebody else fought down.
            victim.LastHitterId = killer.Kind == EntityKind.Player ? killer.Id : null;

            var earner = TopDamager(victim)
                         ?? (killer.Kind == EntityKind.Player ? killer : null);

            if (earner is not null)
            {
                // Open the reward tally so the exp pass and the gold pass land in ONE chat line.
                _killTally = new Dictionary<Entity, (long Exp, long Sp, long Gold)>();
                AwardKillExp(earner, victim);   // splits across contenders by damage share
                RollDrop(earner, victim);       // drops go WHOLLY to the most damage
                // After both, so the line carries the whole kill — and after RollDrop's loot lines,
                // so the summary reads as the closing line of the kill rather than the opening one.
                FlushKillTally();
                // Kill-quest credit for the earner + every party member in range.
                foreach (var m in KillCreditMembers(earner))
                    AdvanceKillQuests(m, victim);
                // A PK works off karma by grinding — each mob kill sheds a little.
                ReduceKarma(earner, _karmaLossPerMob);
            }

            OnMobKilled(victim);
        }
        else
        {
            CancelTradeFor(victim, notifyPartnerOnly: false);
            BroadcastSystem($"{victim.Name} was slain by {killer.Name}.");

            // PvP kill consequences (a player killed a player): pvp/pk counts + karma.
            if (killer.Kind == EntityKind.Player && killer.Id != victim.Id)
                ApplyPvpKill(killer, victim);

            // Any death sheds a big chunk of a PK's karma (the red flag clears at 0).
            ReduceKarma(victim, _karmaLossPerDeath);

            // Death costs 5% of the level's exp (floored at 0 — no delevel), on ANY death incl. PvP.
            // (Later a noblesse buff will waive it on boss/instance/PvP deaths — only normal-monster deaths
            // cost exp while noblesse is up.) Tracks the lost exp so a resurrection can restore a fraction.
            ApplyDeathExpPenalty(victim);

            // DEATH STICKS ACROSS A LOGOUT — for EVERY death, not just an offline-farm/link-dead one
            // (owner, 2026-07-17). It used to be set only on those two paths, so an ordinary death +
            // "Exit to character select" + log back in stood you up at FULL HP: a free death-dodge, and
            // the exact opposite of the res flow. Cleared when the death is actually paid for — a town
            // respawn (HandleRespawn) or a resurrection (ResurrectTarget).
            victim.DiedWhileAway = true;

            // Death stops auto-hunt. An offline farmer's session ends (deferred logout); a link-dead
            // grace ends; an online idle hunter just stops (can re-enable after respawn).
            if (victim.IsOfflineFarming)
                _endOfflineQueue.Add(victim.Id);
            else if (victim.IsDisconnected)
                _endGraceQueue.Add(victim.Id);
            else if (victim.AutoHuntEnabled)
                StopAutoHunt(victim, "you were defeated.");

            // 🔴 THE PRESERVATION SKILLS ARE A DEATH PROMPT, NOT A REFUSAL TO DIE (`BL-35`, re-specced
            // in playtest 23). They used to call ResurrectTarget here, which stood you straight back up
            // at 30% with no prompt — and from the chair that is indistinguishable from never dying,
            // which is exactly how he read it: *"now is literally undying will ... U just don't die u
            // heal +30% when your hp reaches 0."*
            //
            // What he asked for instead, verbatim: *"the tanks and healers are like you die (mobs stop
            // attacking etc ..the hole pipe) and get a resurrection promp if you click yes u resurrect
            // on the spot, else back to town ...it's like you die with angels protection on and some1
            // instantly resurects you"* · *"I want phebyx blood - u die -> u stay dead until you click
            // the resurrection prompt."*
            //
            // 🔑 So the ONLY change is which pipe this line calls. Everything above has already run —
            // the death broadcast, the aggro shed, the karma, the death penalty, the auto-hunt stop —
            // which is precisely his *"the hole pipe"*. The offer never expires
            // (<see cref="ResurrectOfferForever"/>), so you lie there for as long as you like and the
            // decision stays yours; declining leaves you dead with the ordinary town respawn still on
            // the screen, which is his *"else back to town"*.
            //
            // ⚠ The death penalty ALREADY APPLIED and that ordering is still deliberate: his rule is
            // "you die, you have the penalty". The exp comes back through the res, not by skipping the
            // penalty — so a 100% skill nets to zero while a partial one still costs you.
            //
            // ⚠ The buff is consumed either way, above. Declining spends it: you took the death.
            if (autoRes)
                OfferResurrect(victim, victim, autoResExpPct, ResurrectOfferForever);
        }
    }

    private void RollDrop(Entity killer, Entity mob)
    {
        if (mob.MobTypeId is null)
            return;

        // DROPS are decided by the KILLER (owner): their level gap to the mob scales both gold and the
        // drop chances, on the same symmetric 0.85^(gap-5) curve as exp, zero at 13. This is what stops
        // a level-1 bow last-hitting a level-78 mob for its loot table.
        float dropGap = ExpCurve.LevelGapMultiplier(killer.Level - mob.Level);
        if (dropGap <= 0f)
            return;

        // In-range kill-credit members (killer + party members within share range). Solo = [killer].
        var eligible = KillCreditMembers(killer);
        _world.Parties.TryGetValue(killer.Id, out var party);

        // Gold ALWAYS splits evenly among in-range members regardless of loot mode; the killer takes
        // the remainder. Solo = it all goes to the killer. (Level x rate, +/-20% variance.)
        int gold = (int)(StatCalculator.MobGoldReward(mob.Level) * RateConfig.World.Gold * dropGap
            * (0.8f + (float)_rng.NextDouble() * 0.4f));
        if (gold > 0)
            AwardGold(killer, eligible, gold);

        var mobType = MobCatalog.Get(mob.MobTypeId);
        if (mobType.Drops is null || mobType.Drops.Length == 0)
            return;

        // Only entries valid at this mob's level. Independent entries roll on their own; entries
        // sharing a GroupId > 0 form a mutually-exclusive group (roll once, pick one weighted).
        var applicable = mobType.Drops.Where(e => e.AppliesAtLevel(mob.Level)).ToList();

        // ELITE and BOSS kills REPLACE the gear half of the table with their own rank row (playtest-14
        // §3) — the elite column has no Common rung at all, and the boss column is Epic/Legendary/Mythic.
        // Rank is a property of the SPAWN (the zone assigns it), not of the template, so it can only be
        // applied here; mats, scrolls and the always-group are untouched and still roll.
        if (mob.Rank != MobRank.Normal)
        {
            applicable.RemoveAll(e => MobCatalog.IsGearGroup(e.GroupId));
            applicable.AddRange(MobCatalog.GearDrops(mob.Level, mob.Rank));
            // The enchant-scroll layer ADDS rather than replaces (0.49.0 D1): an elite still rolls the
            // ordinary scrolls group, and the Greater/Safe types exist nowhere else.
            applicable.AddRange(MobCatalog.EnchantScrollDrops(mob.Level, mob.Rank));
            // Same shape, same reason, for the TOP crafting mats (`BL-05`): Epic/Legendary/Mythic
            // materials have no normal-mob faucet at all, so B, A and S gear is only craftable because
            // elites pay them. See MobCatalog.EliteMatDrops.
            if (mob.MobTypeId is not null)
                applicable.AddRange(MobCatalog.EliteMatDrops(
                    mob.Level, mob.Rank, MobCatalog.Get(mob.MobTypeId).Category));
        }

        // Everyone who received something this kill (refresh their inventory once at the end).
        var touched = new HashSet<Entity>();

        // ONE entry, firing `copies` times (MobCatalog.DropCopies — the rate above 100% is copies, not a
        // clamp). Every copy draws its OWN recipient, which is what keeps RoundRobin/Random spreading a
        // x30 kill across the party instead of dumping the whole pile on one member; the copies are then
        // batched per recipient so a stackable arrives as ONE stack and ONE chat line rather than thirty.
        void Award(DropEntry entry, int copies)
        {
            if (ItemCatalog.Get(entry.ItemId) is not ItemDef def || copies <= 0)
                return;
            bool stack = def.IsStackable;
            var perPlayer = new Dictionary<Entity, int>();
            for (int c = 0; c < copies; c++)
            {
                var who = LootRecipient(killer, eligible, party);
                // A stackable accumulates its rolled QUANTITY; a piece of gear is one item per copy
                // (AddItem writes Quantity=1 for anything non-stackable, so a count is all that means).
                int n = stack ? _rng.Next(entry.MinQty, entry.MaxQty + 1) : 1;
                perPlayer[who] = perPlayer.TryGetValue(who, out int had) ? had + n : n;
            }

            foreach (var (to, rolled) in perPlayer)
            {
                int got;
                if (stack)
                {
                    // World.DropAmount is the STACK-SIZE knob and still applies only here — it never made
                    // gear drop twice (one row per piece) and it still doesn't. The rate multiplier lives
                    // in the copies now, so these two knobs no longer overlap.
                    int qty = Math.Max(1, (int)(rolled * RateConfig.World.DropAmount));
                    got = AddItem(to, def.Id, qty) ? qty : 0;
                }
                else
                {
                    // Non-stackable: one bag slot each, so stop at the first refusal — the rest is lost
                    // exactly as a single over-cap drop always was.
                    got = 0;
                    while (got < rolled && AddItem(to, def.Id, 1)) got++;
                }

                if (got <= 0)
                {
                    SendSystemToEntity(to, $"{mob.Name} dropped {def.Name} — inventory full!");
                    continue;
                }
                if (got < rolled)
                    SendSystemToEntity(to, $"{mob.Name} dropped {rolled}x {def.Name} — only {got} fit!");

                string qtyLabel = got > 1 ? $" x{got}" : "";
                SendCombatToEntity(to, "LOOT", $"You looted: {def.Name}{qtyLabel} [{def.Grade}/{def.Rarity}]");
                // Let the rest of the in-range party see where it went.
                if (eligible.Count > 1)
                    foreach (var m in eligible)
                        if (m.Id != to.Id)
                            SendCombatToEntity(m, "LOOT", $"{to.Name} looted {def.Name}{qtyLabel}.");
                touched.Add(to);
            }
        }

        // The KILLER's Rune of Drop scales every roll on this kill, the same entity whose level gap
        // already scales them. It is passed INTO EffectiveChance so the inspect screen (which asks the
        // same function with the same player) shows exactly the chance that is rolled here.
        float dropMult = killer.Runes.DropChance;

        // Independent entries (GroupId == 0): each its own rate-scaled roll. Above 100% the excess is
        // COPIES, not a discarded remainder — a 3.6% row at x30 is 108%, i.e. one guaranteed plus an 8%
        // second, so the knob delivers exactly x30 instead of the x27.8 a clamp would have paid.
        foreach (var entry in applicable.Where(e => e.GroupId == 0))
            Award(entry, MobCatalog.DropCopies(
                MobCatalog.EffectiveChance(entry, dropMult) * dropGap, _rng.NextDouble()));

        // Drop groups (GroupId > 0): roll the SUMMED chance, then take that many weighted picks. One
        // pick is the old behaviour and still the normal case at x1 — the group stays mutually exclusive
        // (owner: "never 20 light armors off one lucky kill") because a member's authored chance IS its
        // marginal drop chance and the group's trigger is their sum, never a second authored number.
        //
        // 🔑 Above 100% the group fires REPEATEDLY, each copy an independent weighted pick, which is what
        // preserves the authored weights at any rate — and is why the guaranteed groups no longer need
        // to be exempt from the global (a 100% mats group at x30 = 30 picks in exact table proportion).
        foreach (var group in applicable.Where(e => e.GroupId != 0).GroupBy(e => e.GroupId))
        {
            var members = group.ToList();
            int copies = MobCatalog.DropCopies(
                members.Sum(e => MobCatalog.EffectiveChance(e, dropMult)) * dropGap, _rng.NextDouble());
            if (copies <= 0)
                continue;
            // Weighted pick within the group. The weight is the PER-ITEM-TUNED chance, the same
            // quantity the trigger above was summed from — take the raw authored chance here instead
            // and a per-item multiplier would move how often the group fires without moving which
            // member it lands on, which is the one thing the knob exists to do.
            double weightSum = members.Sum(e => (double)MobCatalog.ItemWeight(e));
            if (weightSum <= 0)
                continue;
            // Copies are tallied per member first, so thirty picks land as a handful of Award calls
            // (and a handful of chat lines) rather than thirty.
            var picks = new Dictionary<DropEntry, int>();
            for (int c = 0; c < copies; c++)
            {
                double pick = _rng.NextDouble() * weightSum;
                foreach (var e in members)
                {
                    pick -= MobCatalog.ItemWeight(e);
                    if (pick <= 0)
                    {
                        picks[e] = picks.TryGetValue(e, out int had) ? had + 1 : 1;
                        break;
                    }
                }
            }
            foreach (var (e, n) in picks)
                Award(e, n);
        }

        // Boss/elite pile goes to ONE recipient per the loot rule (mats stay together).
        var bossTo = LootRecipient(killer, eligible, party);
        if (RollBossBonus(bossTo, mob, mobType))
            touched.Add(bossTo);

        foreach (var t in touched)
            SendInventory(t);
    }

    /// <summary>Split gold evenly among in-range members; the killer keeps the remainder. Solo (or a
    /// single eligible member) = the killer takes it all. Gold ignores the party's item loot mode.
    ///
    /// <para>A Rune of Gold is applied to the RECIPIENT's own share, exactly as an Exp rune applies to
    /// the recipient's own exp — never to the pot before it is split. Scaling the pot would pay a party
    /// of five for one member's premium rune, and would let the killer's Rune of Sinners zero everyone
    /// else's coin.</para></summary>
    private void AwardGold(Entity killer, List<Entity> eligible, int gold)
    {
        void Pay(Entity m, int share)
        {
            int paid = (int)(share * m.Runes.Gold);
            m.Gold += paid;
            TallyReward(m, 0, 0, paid);
            SendGold(m);
        }

        if (eligible.Count <= 1)
        {
            Pay(killer, gold);
            return;
        }
        int each = gold / eligible.Count;
        int remainder = gold - each * eligible.Count;
        foreach (var m in eligible)
            Pay(m, each + (m.Id == killer.Id ? remainder : 0));
    }

    /// <summary>Pick who receives one loot item, per the party's <see cref="LootMode"/>. Solo, no
    /// party, or a single eligible member always returns the killer.</summary>
    private Entity LootRecipient(Entity killer, List<Entity> eligible, Party? party)
    {
        if (party is null || eligible.Count <= 1)
            return killer;
        switch (party.LootMode)
        {
            case LootMode.Random:
                return eligible[_rng.Next(eligible.Count)];
            case LootMode.LeaderOnly:
                return eligible.FirstOrDefault(m => m.Id == party.LeaderId) ?? killer;
            case LootMode.RoundRobin:
                // Rotate over in-range members in stable join order.
                var ordered = party.Members
                    .Where(id => eligible.Any(e => e.Id == id))
                    .Select(id => eligible.First(e => e.Id == id))
                    .ToList();
                if (ordered.Count == 0)
                    return killer;
                party.RoundRobinCursor++;
                return ordered[party.RoundRobinCursor % ordered.Count];
            case LootMode.FindersKeepers:
            default:
                return killer;
        }
    }

    /// <summary>Elite/boss EXTRA loot: a pile of crafting mats (rarity + amount by rank) and a chance
    /// at the finished tiered set piece — bosses are the reliable gear/mat source (docs/design/Crafting.md).</summary>
    private bool RollBossBonus(Entity recipient, Entity mob, MobType mobType)
    {
        if (mob.Rank is not (MobRank.Boss or MobRank.Elite))
            return false;
        bool boss = mob.Rank == MobRank.Boss;
        int tier = mob.Level >= 76 ? 76 : mob.Level >= 61 ? 61 : mob.Level >= 52 ? 52 : mob.Level >= 40 ? 40 : 20;
        MaterialType primary = mobType.Category switch
        {
            MobCategory.Animal or MobCategory.Plant => MaterialType.Leather,
            MobCategory.Undead or MobCategory.Insect => MaterialType.Thread,
            MobCategory.MagicCreature or MobCategory.Angel => MaterialType.Gem,
            _ => MaterialType.Ingot,
        };

        void GiveMat(MaterialType t, ItemRarity r, int qty)
        {
            if (qty > 0) AddItem(recipient, Crafting.MaterialId(t, r), qty);
        }

        GiveMat(primary, ItemRarity.Common, boss ? _rng.Next(6, 11) : _rng.Next(2, 4));
        GiveMat(MaterialType.Gem, ItemRarity.Common, boss ? _rng.Next(4, 8) : _rng.Next(1, 3));
        GiveMat(primary, ItemRarity.Uncommon, boss ? _rng.Next(2, 5) : 1);
        if (boss && mob.Level >= 30 && _rng.NextDouble() < 0.5) GiveMat(primary, ItemRarity.Rare, 1);
        if (boss && mob.Level >= 76 && _rng.NextDouble() < 0.2) GiveMat(primary, ItemRarity.Epic, 1);

        // The GEAR a boss or elite drops is no longer decided here — RollDrop swaps the normal gear groups
        // for MobCatalog.GearDrops(level, rank), which is the owner's §3 rank table (elite U 10 / R 2 /
        // E 0.2; boss E 70 / L 40 / M 2) across all four slot families. What stays here is the mat pile
        // above and the RECIPE roll below, neither of which is a per-slot rarity roll.

        // RECIPE BOOKS (§3: boss armor 50% / weapon 40% / jewel 60%, elite 0.1%). Books only EXIST from
        // A grade up — every recipe below 76 is learned by LEVEL, not found (RecipeCatalog.DropOnly), so
        // there is no item to drop for the owner's "below level 74 also drop a recipe at 0.1%". That rung
        // needs recipe books authored for the lower grades first; flagged, not faked.
        if (tier >= 76)
        {
            string PickRecipe(params string[] keys) =>
                ItemCatalog.RecipeBookId($"craft_{keys[_rng.Next(keys.Length)]}_t{tier}");
            void RecipeRoll(float chance, params string[] keys)
            {
                if (_rng.NextDouble() < chance) AddItem(recipient, PickRecipe(keys));
            }
            if (boss)
            {
                RecipeRoll(0.50f, "heavy", "light", "robe", "helm", "gloves", "boots", "shield");
                RecipeRoll(0.40f, "sword1h", "sword2h", "blunt1h", "blunt2h", "duals", "bow", "wand", "staff");
                RecipeRoll(0.60f, "necklace", "ring", "earring");
            }
            else
            {
                RecipeRoll(0.001f, "heavy", "light", "robe", "sword1h", "sword2h", "bow", "wand",
                    "necklace", "ring", "earring");
            }
        }

        SendSystemToEntity(recipient, $"{mob.Name} dropped crafting materials!");
        return true;
    }

    /// <summary>The killer + any alive party members within share range (ViewRange). Solo = just
    /// the killer. Used for both XP split and kill-quest credit.</summary>
    private List<Entity> KillCreditMembers(Entity killer)
    {
        var list = new List<Entity> { killer };
        if (_world.Parties.TryGetValue(killer.Id, out var party))
        {
            float r2 = GameConstants.ViewRange * GameConstants.ViewRange;
            foreach (var mid in party.Members)
                if (mid != killer.Id && _world.Entities.TryGetValue(mid, out var m) &&
                    !m.Dead && DistanceSq(killer, m) <= r2)
                    list.Add(m);
        }
        return list;
    }

    /// <summary>EXP for killing one mob: the level curve scaled by how long this particular mob takes
    /// to kill, times the rank's efficiency bonus (`BL-49`).
    ///
    /// <para>His ruling, 2026-08-14: *"bosses should give exp based on how long it takes to kill a
    /// normal mob vs boss (x1.2~2) — killing a boss gives you twice (or 1.5) the exp for the same time
    /// of normal fighting. Not a real formula to calculate it, just a curve to have."*</para>
    ///
    /// <para>So the payout is <c>base × killTimeRatio × efficiency</c>, and the whole design reads off
    /// that: an hour spent on bosses is worth <see cref="RankExpEfficiency"/> hours spent on trash.
    /// Setting efficiency to 1.0 would make bosses exactly break-even and pointless; the bonus IS the
    /// reason to fight one.</para>
    ///
    /// <para>🔑 **Why a time RATIO is honest and needs no simulation.** Time-to-kill is
    /// <c>EHP / yourDPS</c>, and this compares two mobs at the SAME level against the SAME player, so
    /// your DPS cancels out completely. What is left is the mob's own effective bulk, and nothing about
    /// the killer enters the number — which is what keeps a boss worth the same multiple to a geared
    /// character and to a naked one. (It also means the 11x absolute-TTK swing across levels found under
    /// `BL-13` does NOT distort this: that is a property of the boss HP curve, not of the ratio.)</para>
    ///
    /// <para>🔑 **This supersedes the old HP-only "toughness", which was silently capped at 20x while a
    /// boss carries 100x HP** — so every field boss in the game paid a fifth of what it owed, and paid
    /// the SAME as a mob merely 20x bulky. That cap is why "the elite/boss EXP multiplier wants a look"
    /// was on the list.</para>
    ///
    /// <para>⚠ Derived, not authored: if he re-curves boss HP under `BL-13`, the EXP follows on its own
    /// and there is no second table to remember to change.</para></summary>
    private long MobExpValue(Entity mob) =>
        Math.Max(1L, (long)(StatCalculator.MobExpReward(mob.Level)
                            * MobKillTimeRatio(mob) * RankExpEfficiency(mob.Rank)
                            * RespawnScarcity(mob)));

    /// <summary>SP for one kill. The same scaling applies, so a bulky mob pays its multiple of SP
    /// exactly as it pays its multiple of EXP; the level-dependent SP:EXP ratio lives in ExpCurve.</summary>
    private long MobSpValue(Entity mob) =>
        Math.Max(1L, (long)(StatCalculator.MobSpReward(mob.Level)
                            * MobKillTimeRatio(mob) * RankExpEfficiency(mob.Rank)
                            * RespawnScarcity(mob)));

    /// <summary>🔴 THE SECOND HALF OF THE TIME COST: what you spend WAITING for this thing to come back.
    ///
    /// <para>His playtest-23 ruling: *"a 90 elite gives ~200k exp while boss gives 6kk ... 30times more
    /// and feel like a waste ... make it give atleast 20kk ... we should take the **respawn time** and the
    /// time it takes a **1 dd** to kill the boss **not 5**."* The kill-time half was already here; the
    /// wait was not counted at all, so a creature you may fight twice an hour was paid as if you could
    /// line up thirty of them.</para>
    ///
    /// <para>The ratio is against the world's own authored trash cadence
    /// (<see cref="GameConstants.BaselineRespawnSeconds"/> = 22s, the number every ordinary field uses),
    /// so an ordinary mob comes out at exactly ×1.00 and nothing about normal levelling moves. A field
    /// boss at 30 minutes is 81.8× that; an elite at 60-90s is 2.7-4.1×.</para>
    ///
    /// <para>⚠ THE EXPONENT IS THE ONE INVENTED NUMBER HERE, and it is deliberately not 1.0. Paying the
    /// wait in full assumes you stand at the corpse for thirty minutes doing nothing, which nobody does —
    /// you farm the camp around it, and that time is already being paid for by the trash. The exponent is
    /// what says "a share of the wait", and 0.25 is the share that lands a level-90 field boss on HIS
    /// stated target: 8kk from the kill-time half × 3.0 here ≈ 24kk, against his *"at least 20kk"*.
    /// It is one number to move if he wants bosses richer or poorer.</para>
    ///
    /// <para>Read off the SPAWN ZONE, not the template: the same creature is worth more where it is rare.
    /// A mob with no resolvable zone (a summon, a debug spawn) scores 1.0 and is unaffected.</para></summary>
    private float RespawnScarcity(Entity mob)
    {
        if (mob.ZoneId is null) return 1f;
        var zr = _zones.FirstOrDefault(z => z.Zone.Id == mob.ZoneId);
        if (zr is null) return 1f;
        float ratio = (float)zr.Zone.RespawnSeconds / GameConstants.BaselineRespawnSeconds;
        if (ratio <= 1f) return 1f;   // as common as trash, or commoner: no scarcity premium
        return Math.Clamp(MathF.Pow(ratio, GameConstants.RespawnScarcityExponent), 1f, 12f);
    }

    /// <summary>How much LONGER this spawn takes to kill than a plain mob of its level, as a multiple.
    ///
    /// <para>Damage in this game is a RATIO — <c>K·(atk·lvlMod + power)/def</c> — so defence is a
    /// divisor and time-to-kill is proportional to <c>HP × defence</c>, not to HP alone. Both halves are
    /// read off the SPAWNED entity, so a rank multiplier, a MobMod HP passive and a buff a mob is
    /// standing in are all already counted; neither half is re-derived from the template.</para>
    ///
    /// <para>Today only the HP half actually moves (rank scales HP and P.Atk, not defence), so this
    /// reduces to the HP ratio — but the defence term is here because the moment a boss is given real
    /// defence, its EXP must follow without anyone remembering to come back here.</para>
    ///
    /// <para>⚠ The clamp is a sanity rail against a corrupt spawn, NOT a balance knob: it sits above the
    /// 100x a field boss legitimately carries, which is exactly the mistake the old 20x cap made. The
    /// floor keeps a deliberately frail mob from paying nothing at all.</para></summary>
    private static float MobKillTimeRatio(Entity mob)
    {
        float hpRatio = mob.MaxHp / (float)Math.Max(1, MobBaseStats.Hp(mob.Level));
        float defRatio = mob.EffectiveDefence / Math.Max(1f, MobBaseStats.PDef(mob.Level));
        return Math.Clamp(hpRatio * Math.Max(0.25f, defRatio), 0.25f, 400f);
    }

    /// <summary>His *"x1.2~2"* — how much better an hour spent on this rank is than an hour spent on
    /// trash. A curve, in his words, not a formula: normal is break-even by definition, an elite is a
    /// small premium for a fight you can still take solo, and a boss pays his own "twice (or 1.5)".
    ///
    /// <para>🔴 A BOSS IS 2.0, NOT 1.5, SINCE PLAYTEST 23 — and it moved because he struck out the
    /// ARGUMENT for 1.5, not because the number was measured wrong. That argument was: "a boss is fought
    /// by a PARTY — five people kill it five times faster and split the pot five ways, so the efficiency
    /// each sees is exactly this constant." His answer: *"we should take the respawn time and the time it
    /// takes a **1 dd** to kill the boss **not 5**."* Priced for one damage dealer there is no five-way
    /// split in the number any more, and what is left is the top of his own *"x1.2~2"*.</para>
    ///
    /// <para>An elite stays at 1.2. Nothing about it was ever justified by a party split — an elite is a
    /// fight you take solo, which is the whole reason it sits between trash and a boss.</para>
    ///
    /// <para>A world boss has no rank of its own (it is a <see cref="MobRank.Boss"/> carrying extra HP
    /// via its template), so it lands here at 2.0 and is paid for its real bulk by the ratio above and
    /// for its 21-hour respawn by <see cref="RespawnScarcity"/>. `BL-13` still says it wants a rank of
    /// its own; nothing here needs a fourth rung until it gets one.</para></summary>
    private static float RankExpEfficiency(MobRank rank) => rank switch
    {
        MobRank.Elite => 1.2f,
        MobRank.Boss  => 2.0f,
        _             => 1.0f,
    };

    /// <summary>Death exp penalty: lose 5% of the level's exp, floored at 0 (no delevel). Stores the lost
    /// amount in LostExp so a resurrection skill/scroll can restore a fraction; a normal town respawn drops it.</summary>
    private void ApplyDeathExpPenalty(Entity p)
    {
        if (p.Kind != EntityKind.Player) return;
        p.LostExp = 0;
        // Low-level "newbie protection": below this level, death costs no exp (nothing for a scroll to
        // restore). Later a noblesse-style passive will also waive the loss on boss/instance deaths.
        if (p.Level < GameConstants.DeathExpPenaltyMinLevel) return;
        long penalty = (long)(StatCalculator.ExpToNext(p.Level) * 0.05);
        long lost = Math.Min(p.Exp, penalty);
        p.LostExp = lost;
        if (lost <= 0) return;
        p.Exp -= lost;
        SendSystemToEntity(p, $"You lost {lost:N0} experience.");
        if (_world.EntityToConnection.TryGetValue(p.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                p.Level, p.Exp, StatCalculator.ExpToNext(p.Level), false, p.SkillPoints));
    }

    /// <summary>Can this skill be aimed at a PARTY MEMBER, or only at yourself?
    ///
    /// The obvious test — "does it carry a support Effect bit?" — silently broke Angel's Protection: it is
    /// a pure MARKER buff whose entire payload is a flag FIELD (KeepsBuffsOnDeath), because the SkillEffect
    /// enum is full. Its Effect is None, so an Effect-only test dropped it into the self-cast branch and it
    /// could not be placed on anybody — it merely LOOKED like a targeted buff (Range 600, SelfOrTarget).
    /// Hence the second arm: ANY Buff-category skill is ally-targetable. New marker buffs get this free.</summary>
    private static bool IsAllyTargetable(SkillDef def) =>
        (def.Effect & (SkillEffect.Heal | SkillEffect.RestoreMp | SkillEffect.Cleanse | SkillEffect.AnyBuff)) != 0
        || def.Category == SkillCategory.Buff;

    /// <summary>Teleport a player to the nearest safe town and drop them out of combat. Shared by the
    /// CHANNELLED Return path (the cast completing) and the INSTANT one (the Ultimate scroll, whose whole
    /// purpose is to have no cast at all — see UsePotion).</summary>
    private void ReturnToTown(Entity caster)
    {
        var town = WorldMap.NearestSafeZone(caster.X, caster.Y);
        PlaceEntity(caster, town.X + _rng.Next(-150, 150), town.Y + _rng.Next(-150, 150));
        caster.Engaged = false;
        caster.CombatTargetId = null;
        caster.QueuedSkillId = null;
        SendSystemToEntity(caster, $"You return to {town.Name}.");
    }

    /// <summary>Ticks a resurrection OFFER lingers before it auto-expires (30s at 10 t/s).</summary>
    private const int ResurrectOfferTicks = 300;

    /// <summary>An offer that does NOT expire — the preservation skills' own window (see
    /// <see cref="Kill"/>). His rule, playtest 23: *"u die -> u stay dead until you click the
    /// resurrection prompt."* A promise you paid a level-83 skill for is not something to lose by
    /// looking away; the only ways out of it are accepting, declining, or walking back to town.
    /// ⚠ Not <c>int.MaxValue</c>: the tick loop decrements this every tick, so the value has to survive
    /// the subtraction forever without wrapping past zero. A week of ticks does that with room to spare
    /// and still reads as a number rather than a sentinel.</summary>
    private const int ResurrectOfferForever = 10 * 60 * 60 * 24 * 7;

    /// <summary>Offer a resurrection to a fallen player: they see a confirm prompt (who revived them + how
    /// much exp comes back) and must ACCEPT before actually reviving — so they don't stand up on top of the
    /// mob that killed them. Generic on caster==target, so a self-res reuses this same pipe.</summary>
    private void OfferResurrect(Entity caster, Entity target, float expPct, int ticks = ResurrectOfferTicks)
    {
        if (target is null || target.Kind != EntityKind.Player || !target.Dead) return;
        expPct = Math.Clamp(expPct, 0f, 1f);
        target.PendingResFromId = caster.Id;
        target.PendingResExpPct = expPct;
        target.PendingResTicks = ticks;
        long wouldRestore = (long)(target.LostExp * expPct);
        if (_world.EntityToConnection.TryGetValue(target.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("ResurrectOffer",
                new ResurrectOffer(caster.Name, expPct, wouldRestore, SelfRes: caster.Id == target.Id));
        if (target != caster)
            SendSystemToEntity(caster, $"You offer to resurrect {target.Name}.");
    }

    /// <summary>Revive a fallen player to 30% HP/MP and restore <paramref name="expPct"/> of the exp they
    /// lost to the death penalty (0 for the basic scroll, 1.0 for the highest res). No-op on a living target
    /// or a non-player (mobs are removed on death, not resurrected). The next StateUpdate (Dead=false) clears
    /// the client's death overlay.</summary>
    private void ResurrectTarget(Entity caster, Entity target, float expPct)
    {
        if (target is null || target.Kind != EntityKind.Player || !target.Dead) return;
        target.PendingResFromId = null;
        target.PendingResTicks = 0;
        target.Dead = false;
        // The death is paid for — stop it sticking, or the next logout would log them back in dead.
        target.DiedWhileAway = false;
        target.RecomputeDerived();
        target.Hp = Math.Max(1, (int)(target.MaxHp * 0.30f));
        target.Mp = (int)(target.MaxMp * 0.30f);
        long restore = (long)(target.LostExp * Math.Clamp(expPct, 0f, 1f));
        target.LostExp = 0;
        if (restore > 0)
        {
            target.Exp += restore;
            SendSystemToEntity(target, $"You have been resurrected. {restore:N0} experience restored.");
            if (_world.EntityToConnection.TryGetValue(target.Id, out var conn))
                _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                    target.Level, target.Exp, StatCalculator.ExpToNext(target.Level), false, target.SkillPoints));
        }
        else
        {
            SendSystemToEntity(target, "You have been resurrected.");
        }
        if (target != caster) SendSystemToEntity(caster, "You resurrect a fallen ally.");
        // `BL-59` — standing an outlaw back up is supporting one, and it flags you like a heal does.
        // Placed here rather than at the OFFER so it costs nothing when the offer is declined or
        // expires; and it is a no-op for a self-res or an innocent target (see FlagForSupporting).
        FlagForSupporting(caster, target);
        if (target.Kind == EntityKind.Player) SendStats(target);
    }

    /// <summary>Award one mob kill's EXP/SP. **The pot is SHARED, the penalty is PERSONAL** (owner,
    /// 2026-07-24):
    /// <code>
    ///   pot    = mobValue * randomRoll(0.80-1.20) * partyBonus(n)   // no level penalty here
    ///   share  = pot / memberCount                                   // equal, everyone alike
    ///   each m = share * levelGapMultiplier(m.Level - mob.Level)     // personal
    /// </code>
    /// The ONE random roll is shared by the whole party — rolling per member would show 16k to one
    /// player and 24k to another off the same corpse, which reads as a bug and looks like a rigged
    /// split. It covers EXP and SP together so their ratio can't drift.
    ///
    /// The killer does NOT gate the party: the pot is the full mob value whoever landed the kill, so a
    /// level-60 who kills a level-75 mob earns 0 *for himself* while his level-75 mates bank full
    /// shares. Anti-powerlevelling is enforced entirely by the PERSONAL penalty, from both directions —
    /// a low-level can't be dragged through a high zone, and a high-level gains nothing babysitting a
    /// low one. (This replaced an older rule where the killer's own gap zeroed everybody, and a
    /// level-weighted split; both are gone on purpose.) Kill-QUEST credit is untouched.</summary>
    private void AwardKillExp(Entity topDamager, Entity victim)
    {
        // ONE roll for the whole kill, shared by everyone on it (see ExpCurve.RandomFactor).
        double roll = ExpCurve.RandomFactor(_rng);
        long baseExp = MobExpValue(victim);
        long baseSp = MobSpValue(victim);

        long total = 0;
        foreach (var d in victim.DamageLog.Values) total += d;

        if (total <= 0)
        {
            // No ledger at all — e.g. finished by a DoT whose caster has since left, or a kill with no
            // recorded player damage. Pay the nominated killer's group in full rather than nobody.
            PayKillShare(topDamager, victim, baseExp, baseSp, roll, 1.0);
            return;
        }

        // CONTESTED KILLS split by damage share (owner): a party that did 80% of the damage takes 80%
        // of the exp and the other side takes 20%. Contributions POOL BY PARTY, so a party is measured
        // as one contender rather than as several small ones.
        var groups = new Dictionary<Guid, (Entity Rep, long Damage)>();
        foreach (var (id, dmg) in victim.DamageLog)
        {
            // Someone who left the world is skipped, but their damage STAYS in the total: their share is
            // forfeited, not redistributed. Otherwise having a friend log off would inflate your cut.
            if (!_world.Entities.TryGetValue(id, out var p) || p.Kind != EntityKind.Player) continue;
            Guid key = _world.Parties.TryGetValue(p.Id, out var party) ? party.LeaderId : p.Id;
            groups[key] = groups.TryGetValue(key, out var g) ? (g.Rep, g.Damage + dmg) : (p, dmg);
        }

        foreach (var g in groups.Values)
            PayKillShare(g.Rep, victim, baseExp, baseSp, roll, g.Damage / (double)total);
    }

    /// <summary>Pay one contender (a solo player or a party) its slice of a kill. The pot is shared and
    /// the penalty is personal: pot = base × roll × partyBonus × damageShare, split EQUALLY between the
    /// in-range members, then each member's own level gap versus the MOB scales their share.</summary>
    private void PayKillShare(Entity rep, Entity victim, long baseExp, long baseSp, double roll, double share)
    {
        var members = KillCreditMembers(rep);
        if (members.Count == 0 || share <= 0) return;

        float bonus = ExpCurve.PartyBonus(members.Count);
        double shareExp = baseExp * roll * bonus * share / members.Count;
        double shareSp = baseSp * roll * bonus * share / members.Count;

        foreach (var m in members)
        {
            float gap = ExpCurve.LevelGapMultiplier(m.Level - victim.Level);
            if (gap <= 0f) continue;   // 13+ levels out: nothing at all
            // Personal amplifiers, applied at the same stage as the level gap (owner): the shared party
            // share × the mob-level gap × this member's own CHARISMA bonus (1.0…1.5).
            float cha = GameConstants.CharismaExpMultiplier(m.Charisma);
            AwardExp(m, (long)(shareExp * gap * cha), (long)(shareSp * gap * cha));
        }
    }

    /// <summary>Who actually earned this kill: the player who dealt the MOST damage, not whoever landed
    /// the final blow. This is what drops and quest credit key off (owner). Returns null when nobody in
    /// the ledger is still in the world.</summary>
    private Entity? TopDamager(Entity mob)
    {
        Entity? best = null;
        long bestDmg = -1;
        foreach (var (id, dmg) in mob.DamageLog)
            if (dmg > bestDmg && _world.Entities.TryGetValue(id, out var e)
                && e.Kind == EntityKind.Player && !e.Dead)
            {
                bestDmg = dmg;
                best = e;
            }
        return best;
    }

    // ----- The per-kill reward line ---------------------------------------------------------------
    //
    // "Exp: +eee, SP: +sss, Gold: +ggg" — one line per kill, per player (owner, playtest-14).
    //
    // Exp/SP and gold are banked by two unrelated paths (AwardKillExp -> AwardExp, and RollDrop ->
    // AwardGold), each already looping over the in-range party members. Having either one announce its
    // own share would produce two lines per member on a party kill, interleaved with the loot lines. So
    // a kill OPENS a tally, both paths add into it, and one line per recipient is flushed at the end.
    //
    // The tally is null outside a kill, so the other AwardExp callers (quest rewards) can't feed it and
    // don't need to know it exists.
    private Dictionary<Entity, (long Exp, long Sp, long Gold)>? _killTally;

    private void TallyReward(Entity p, long exp, long sp, long gold)
    {
        if (_killTally is null || p.Kind != EntityKind.Player) return;
        _killTally.TryGetValue(p, out var t);
        _killTally[p] = (t.Exp + exp, t.Sp + sp, t.Gold + gold);
    }

    /// <summary>Emit the one-line kill summary to everyone who earned something, then close the tally.
    /// Called after BOTH the exp and the drop pass, so the numbers are the whole kill.</summary>
    private void FlushKillTally()
    {
        if (_killTally is null) return;
        foreach (var (p, t) in _killTally)
        {
            if (t.Exp <= 0 && t.Sp <= 0 && t.Gold <= 0) continue;
            SendCombatToEntity(p, "EXP",
                $"Exp: +{t.Exp:N0}, SP: +{t.Sp:N0}, {GameConstants.CurrencyName}: +{t.Gold:N0}");
        }
        _killTally = null;
    }

    /// <summary>Bank EXP (and optionally SP) on a player, applying the server rates and rolling any
    /// levels that result. Pass <paramref name="spAmount"/> = -1 to derive SP from the exp the old way
    /// (quest rewards, which carry no mob level of their own).</summary>
    private void AwardExp(Entity player, long amount, long spAmount = -1)
    {
        // Server rates scale progression (x10 exp for testing, etc.), then the player's OWN reward
        // multipliers — the premium Exp/SP runes, and the two zeroing runes which make these 0. This is
        // where the Rune of Sinister does its work: *"stops the exp gain (so a grinder can grind and no
        // lvl up)"*. Both channels are read here because this is the one place both are scaled.
        var rates = RateConfig.World * player.Runes;
        long expGained = (long)(amount * rates.Exp);
        player.Exp += expGained;

        long sp = spAmount >= 0
            ? (long)(spAmount * rates.Sp)
            : (long)(amount * GameConstants.SkillPointRatio * rates.Sp);
        // SkillPoints SATURATES at int.MaxValue — deliberate (owner, 2026-07-24), not a stopgap. A full
        // 1->85 earns ~1.5e9 SP at x1, so the ceiling is genuinely reachable at higher SP rates, but the
        // planned sink makes that fine: SP EXTRACTION will convert 1 000 000 000 SP into one "SP bottle"
        // item, and skills will then cost bottles + gold rather than raw SP. Because SP is drained into
        // bottles instead of piling up forever, the counter never needs to be a long. Roadmapped as
        // deferred — see docs/Roadmap.md. What must NEVER happen is silent wrapping to negative.
        int spBefore = player.SkillPoints;
        // The 1-SP floor exists so a tiny reward never rounds to nothing — but a ZEROED channel must
        // stay zero, or the Rune of Sinister would still hand out a skill point per kill and read as
        // broken. A rune that says "no SP" is not a rounding case.
        long spBanked = rates.Sp <= 0f ? 0L : Math.Max(1L, sp);
        player.SkillPoints = (int)Math.Min(int.MaxValue, player.SkillPoints + spBanked);
        // Report what was actually BANKED, not what was computed — at the saturation ceiling those
        // differ, and a line claiming SP you did not receive is worse than no line.
        TallyReward(player, expGained, player.SkillPoints - spBefore, 0);

        bool leveled = false;
        while (player.Level < LevelCapFor(player)
               && player.Exp >= StatCalculator.ExpToNext(player.Level))
        {
            player.Exp -= StatCalculator.ExpToNext(player.Level);
            player.Level++;
            leveled = true;
        }

        // At the cap, park EXP at the bar's start rather than letting it pile up invisibly — an
        // unbounded Exp on a capped character would dump several instant levels the moment the cap
        // was ever raised.
        if (player.Level >= LevelCapFor(player))
            player.Exp = 0;

        if (leveled)
            OnLevelUp(player);

        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
        {
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), leveled, player.SkillPoints));
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
        // The ACTIVE class carries its own level, and the client gates the Learn tab on it. Without
        // this push it kept the value from login, so reaching a learn level mid-session left the new
        // skills greyed out until a relog (playtest-13: locked at 7, fine at 14 — after a relog).
        SendSubclasses(player);
        AdvanceLevelQuests(player);   // any active "reach level N" step may now be satisfied
        // A level-up can OPEN a quest (and close one, now that quests have level ceilings), so the log
        // and its NPC markers have to be re-pushed even when no active quest moved. AdvanceLevelQuests
        // only pushes when it changed something, so without this the "!" over an NPC's head appeared
        // one unrelated quest event late — caught by the SmokeTest, which found no markers at all on a
        // level-81 character who had every starter quest available.
        SendQuestLog(player);
        BroadcastSystem($"{player.Name} reached level {player.Level}!");

        if (player.Level >= GameConstants.ClassChangeLevel && player.SecondClass == 0)
            SendSystemToEntity(player,
                "You are ready for a second class — seek a class-change quest.");
    }

    private void Regenerate(Entity entity)
    {
        // NO combat penalty (owner, 2026-07-29). Regen used to stop dead while Engaged or mid-cast;
        // that rule was ours, not IG's — IG modifies regen by STANCE, never by being in combat — and it
        // is the stance stack below that is meant to express "resting vs fighting". It also broke
        // sustained play outright: auto-farm re-asserts Engaged every tick a target exists, so a farming
        // fighter regenerated nothing at all until they stopped (playtest-13). Regen is now governed by
        // stance, SPT/CON, the safe zone and buffs only.
        float multiplier = entity.Kind == EntityKind.Player &&
                           GameConstants.InSafeZone(entity.X, entity.Y)
            ? GameConstants.SafeZoneRegenMultiplier
            : 1f;

        // Movement-state bonus (Walking +20%, Sitting +80%) for players.
        if (entity.Kind == EntityKind.Player)
            multiplier *= MovementTuning.RegenMultiplier(entity.MoveState);

        // Regen buffs (e.g. Warchanter's chant): +% to HP/MP regen.
        //
        // …and, since 2026-08-19, a FLAT per-second grant as well. Every regen buff in the game had
        // been a percentage of a formula whose base is tiny early on, which is fine for "+20% MP
        // regen" and useless for the healer's Meditation (*"Add MP regen +30/s for 30 seconds"*): 30
        // per second is not a multiple of anything, it is a number. The Flat mode already existed on
        // the magnitude — only these two lines were missing, so a Flat BuffMpRegen was silently
        // reading as zero. Added to the same place gear and passives put theirs (entity.MpRegenBonus),
        // i.e. INSIDE the stance/safe-zone multiplier: sitting to meditate should pay.
        float hpRegenPct = 0f, mpRegenPct = 0f;
        float hpRegenFlat = 0f, mpRegenFlat = 0f;
        foreach (var b in entity.Buffs)
        {
            if (b.Has(SkillEffect.BuffHpRegen))
            {
                hpRegenPct += b.Percent(SkillEffect.BuffHpRegen);
                hpRegenFlat += b.Flat(SkillEffect.BuffHpRegen);
            }
            if (b.Has(SkillEffect.BuffMpRegen))
            {
                mpRegenPct += b.Percent(SkillEffect.BuffMpRegen);
                mpRegenFlat += b.Flat(SkillEffect.BuffMpRegen);
            }
        }

        // The formulas are authored PER SECOND, so a tick pays out one period's worth. This is what
        // keeps the tunable cadence honest: moving the interval 1s→3s makes regen arrive in bigger,
        // rarer chunks WITHOUT changing how fast anyone actually heals, so the panel compares feel
        // rather than secretly rebalancing the game by the same factor.
        float period = GameConstants.RegenIntervalSeconds;

        // A mob is NOT a player with big numbers: it regenerates a fraction of its own pool, because
        // its CON is on a curve the player formula was never meant to be fed (StatCalculator's
        // MobHpRegenPerSecond explains what that produced). Mob masteries (mod.HpRegen) still scale it.
        //
        // Mobs also split on COMBAT, which players deliberately do not (owner, 2026-07-29 — regen by
        // stance, never by being in combat). That rule was about auto-farm starving a player who never
        // leaves combat; a mob has no such problem, and the split is what lets the idle rate be fast
        // enough to matter (5%/s → 20s to full) without a fight becoming a war of attrition.
        bool player = entity.Kind == EntityKind.Player;
        bool engaged = !player && entity.Engaged;

        if (entity.Hp < entity.MaxHp)
        {
            float perSecond = player
                ? StatCalculator.HpRegenPerSecond(entity.Con, entity.Level) + entity.HpRegenBonus + hpRegenFlat
                : StatCalculator.MobHpRegenPerSecond(entity.MaxHp, engaged);
            int regen = Math.Max(1,
                (int)(perSecond * multiplier * entity.HpRegenMult * (1f + hpRegenPct) * period));
            entity.Hp = Math.Min(entity.MaxHp, entity.Hp + regen);
        }

        if (entity.Mp < entity.MaxMp)
        {
            float perSecond = player
                ? StatCalculator.MpRegenPerSecond(entity.EffectiveSpt, entity.Level) + entity.MpRegenBonus + mpRegenFlat
                : StatCalculator.MobMpRegenPerSecond(entity.MaxMp, engaged);
            int regen = Math.Max(1,
                (int)(perSecond * multiplier * entity.MpRegenMult * (1f + mpRegenPct) * period));
            entity.Mp = Math.Min(entity.MaxMp, entity.Mp + regen);
        }

        if (!player) MobRecoveryCheck(entity);
    }

    /// <summary>Heal-over-time buffs (e.g. Warchanter's Renew): heal a % of max HP
    /// each second, in or out of combat, until the buff expires.</summary>
    /// <summary>Skill tag on a heal-over-time floater. The client keys its potion-tinted "+N" off this,
    /// so a potion tick is distinguishable from a heal cast on you or from ambient regen.</summary>
    public const string HotFloaterTag = "HoT";

    private void TickHealOverTime(Entity entity)
    {
        if (entity.Dead || entity.Hp >= entity.MaxHp)
            return;
        float pct = 0f, flat = 0f;
        foreach (var b in entity.Buffs)
            if (b.Has(SkillEffect.HealOverTime))
            {
                pct  += b.Percent(SkillEffect.HealOverTime);   // e.g. Warchanter Renew (% of max HP/s)
                flat += b.Flat(SkillEffect.HealOverTime);       // the flat potion HoTs (HP/s)
            }
        if (pct <= 0f && flat <= 0f)
            return;
        // Flat HoT is hindered by heal-received debuffs; the % HoT is not — the same split HealOne uses,
        // and the whole point of the flat-potion design (you can't out-heal a debuff with a flat potion,
        // but the % channels stay reliable).
        int heal = Math.Max(1, (int)(entity.MaxHp * pct + flat * Math.Max(0f, entity.HealReceivedMod)));
        int before = entity.Hp;
        entity.Hp = Math.Min(entity.MaxHp, entity.Hp + heal);
        int healed = entity.Hp - before;
        // Tag the tick "HoT" rather than "Regen" so the client can tell a POTION apart from ambient
        // regeneration — they were producing an identical green "+N", which is why the potion's healing
        // read as having no floating text at all (owner). Note the early-out above: drinking at full HP
        // heals nothing and so shows nothing, which is correct but looks the same as broken.
        if (healed > 0)
            BroadcastCombat(entity, entity, healed, CombatOutcome.Heal, HotFloaterTag);
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

    // Per-connection memory of the LAST full DTO we sent for each visible entity, so each tick we can
    // send only the DELTA: a full DTO when an entity enters view or its static data changes, a lean
    // update when only dynamic fields moved, a despawn when it leaves. Pruned when a connection drops.
    private readonly Dictionary<string, Dictionary<Guid, EntityDto>> _lastSentByConn = new();

    /// <summary>
    /// When each connection last received anything, so a quiet world still produces a HEARTBEAT.
    ///
    /// The delta broadcast deliberately sends nothing when nothing changed, which is right for
    /// bandwidth and wrong for diagnosis: "the world is calm" and "the server has stopped talking to
    /// me" looked IDENTICAL on the client — both are silence. The client's frames/sec is its only
    /// health signal, and it read 0/s for a player standing still in an empty town.
    ///
    /// So every connection gets at least one (possibly empty) delta per second. An empty delta costs
    /// three empty arrays and turns frames/sec into a real liveness measure with a floor, which is
    /// what lets the client call a genuine stall a stall.
    /// </summary>
    private readonly Dictionary<string, DateTime> _lastSentAtByConn = new();

    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(1);

    private async Task BroadcastSnapshotsAsync()
    {
        if (_world.EntityToConnection.Count == 0)
        {
            if (_lastSentByConn.Count > 0) _lastSentByConn.Clear();
            if (_lastSentAtByConn.Count > 0) _lastSentAtByConn.Clear();
            return;
        }

        var sends = new List<Task>(_world.EntityToConnection.Count);

        foreach (var (entityId, connectionId) in _world.EntityToConnection)
        {
            if (!_world.Entities.TryGetValue(entityId, out var player))
                continue;

            // Everything this viewer can currently see (self always included).
            //
            // A PLAYER'S LEVEL IS PRIVATE (owner, 2026-07-20): it is intel you don't want to hand an
            // enemy, so it never leaves the server for anyone but its owner — hiding it in the client
            // alone would be no protection, since the number would still be on the wire for a modified
            // one to read. MOBS keep their level (the con-colour and the decision to engage depend on
            // it); party members' levels travel separately on PartyMemberDto, which only the party sees.
            var current = new Dictionary<Guid, EntityDto>();
            foreach (var e in _world.Grid.Nearby(player))
            {
                // INVISIBILITY (BL-69) is enforced HERE, by omission, and that is the whole of "a
                // buff nobody renders, targets or checks as nearby": a hidden character simply never
                // reaches the viewer, so the client cannot draw them, cannot click them and cannot
                // hold them in its nearby list. The despawn diff below does the rest — the moment
                // someone hides, everyone who could see them is told they left.
                if (!CanSee(player, e)) continue;
                var dto = e.ToDto();
                if (e.Kind == EntityKind.Player) dto = dto with { Level = 0 };
                current[e.Id] = dto;
            }
            current[player.Id] = player.ToDto();   // self last: your OWN level is yours to see

            if (!_lastSentByConn.TryGetValue(connectionId, out var last))
                _lastSentByConn[connectionId] = last = new Dictionary<Guid, EntityDto>();

            List<EntityDto>? spawns = null;
            List<EntityLean>? updates = null;

            foreach (var (id, dto) in current)
            {
                if (!last.TryGetValue(id, out var prev))
                    (spawns ??= new()).Add(dto);                 // newly in view → full
                else if (!prev.Equals(dto))
                {
                    if (Entity.StaticFieldsEqual(prev, dto) && _world.Entities.TryGetValue(id, out var ent))
                        (updates ??= new()).Add(ent.ToLean());   // only dynamic changed → lean
                    else
                        (spawns ??= new()).Add(dto);             // a static field changed → re-send full
                }
                // else: byte-identical to what they already have → send nothing.
            }

            // Anything they HAD but can no longer see → despawn.
            List<Guid>? despawns = null;
            foreach (var id in last.Keys)
                if (!current.ContainsKey(id))
                    (despawns ??= new()).Add(id);

            _lastSentByConn[connectionId] = current;   // this is now "what they have"

            // Nothing changed for this viewer — stay silent, UNLESS they are due a heartbeat. Silence
            // and a dead server are indistinguishable to a client, so a calm world still has to say
            // something once a second.
            var now = DateTime.UtcNow;
            if (spawns is null && updates is null && despawns is null)
            {
                if (_lastSentAtByConn.TryGetValue(connectionId, out var lastAt)
                    && now - lastAt < HeartbeatInterval)
                    continue;
            }
            _lastSentAtByConn[connectionId] = now;

            sends.Add(_hub.Clients.Client(connectionId).SendAsync("SnapshotDelta",
                new SnapshotDelta(
                    spawns?.ToArray() ?? Array.Empty<EntityDto>(),
                    updates?.ToArray() ?? Array.Empty<EntityLean>(),
                    despawns?.ToArray() ?? Array.Empty<Guid>())));
        }

        // Drop diff state for connections that are gone (logout / disconnect), so it doesn't leak.
        if (_lastSentByConn.Count > _world.EntityToConnection.Count)
        {
            var live = _world.EntityToConnection.Values.ToHashSet();
            foreach (var conn in _lastSentByConn.Keys.Where(c => !live.Contains(c)).ToList())
            {
                _lastSentByConn.Remove(conn);
                _lastSentAtByConn.Remove(conn);   // or the heartbeat clock leaks a row per logout
            }
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

    /// <summary>
    /// A line for the COMBAT feed (D5) — loot and the per-kill reward line — rather than the System
    /// tab it used to share with refusals, learn notices and diagnostics. Killing one mob writes two
    /// to four of these, so a minute of hunting buried every whisper and every "you can't do that
    /// here" under it; on its own channel the client can park them in a window of their own.
    ///
    /// <paramref name="kind"/> ("LOOT" / "EXP") rides in the message's From field, which on this
    /// channel is a colour tag the client does not print. A dedicated DTO field would be tidier, but
    /// From is already a routing tag here ("SYSTEM"), and doing it this way keeps the change to one
    /// enum value — an older client falls through to its Local case and still READS the line.
    /// </summary>
    private void SendCombatToEntity(Entity entity, string kind, string text)
    {
        if (_world.EntityToConnection.TryGetValue(entity.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Chat", new ChatMessage(kind, text, ChatChannel.Combat));
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

        bool stackable = def.IsStackable;
        if (stackable)
        {
            var existing = player.Inventory.FirstOrDefault(i => i.DefId == defId);
            if (existing is not null)
            {
                existing.Quantity += quantity;
                return true;
            }
        }

        if (player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
            return false;   // worn gear doesn't occupy a bag slot

        var newItem = new InventoryItem { DefId = defId, Quantity = stackable ? quantity : 1 };
        // 0.45.0: nothing drops WITH an attribute any more. A dropped weapon/jewel is bare and
        // the player decides whether it is worth a scroll (owner: "you won't waste scrolls on
        // trash when you know the next drop can be better"). Only the god-tier one-offs, which
        // author their attributes in the catalog, still arrive with any.
        if (rollAttributes && def.FixedAttributes is { Length: > 0 } fixedAttrs)
            newItem.Attributes = fixedAttrs.ToList();

        // A RUNE gets its wall-clock expiry stamped the moment it's ACQUIRED (owner: not only boxes stamp).
        // The default is the rune's own GrantsRuneSeconds; a box that grants it OVERRIDES this afterwards
        // with its own duration. So every rune, from any source, always carries an expiry.
        if (def.IsRune && def.GrantsRuneSeconds > 0)
            newItem.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(def.GrantsRuneSeconds);
        // A TIMED item (the 30-day Newbie loaner kit) gets the same treatment from its own
        // LifetimeSeconds. Non-stackable by nature — the stack merge above returns before here, so two
        // acquisitions can never end up sharing one expiry.
        else if (def.LifetimeSeconds > 0)
            newItem.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(def.LifetimeSeconds);

        player.Inventory.Add(newItem);
        return true;
    }

    /// <summary>Total quantity of an item the player holds (sums across stacks).</summary>
    private static int CountItem(Entity player, string defId)
    {
        int n = 0;
        foreach (var it in player.Inventory)
            if (it.DefId == defId) n += it.Quantity;
        return n;
    }

    /// <summary>Remove <paramref name="amount"/> of an item across stacks. Returns false
    /// (removing nothing) if the player doesn't have enough.</summary>
    private static bool ConsumeItem(Entity player, string defId, int amount)
    {
        if (amount <= 0) return true;
        if (CountItem(player, defId) < amount) return false;
        int remaining = amount;
        for (int i = player.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var it = player.Inventory[i];
            if (it.DefId != defId) continue;
            int take = Math.Min(it.Quantity, remaining);
            it.Quantity -= take;
            remaining -= take;
            if (it.Quantity <= 0) player.Inventory.RemoveAt(i);
        }
        return true;
    }

    /// <summary>Move an item from one player to another, merging stacks of
    /// consumables/scrolls on the receiving side.</summary>
    private static void TransferItem(Entity from, Entity to, InventoryItem item)
    {
        from.Inventory.Remove(item);

        if (ItemCatalog.Get(item.DefId) is { IsStackable: true })
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

    private void SendInventory(Entity player)
    {
        SendTo(player, "Inventory", new InventoryUpdate(
            player.Inventory.Select(i => i.ToDto()).ToArray()));

        // A COLLECT step is credited HERE, off the one funnel every item gain and loss already pushes
        // through — see AdvanceCollectQuests for why. ⚠ Re-entrancy: the advance pushes the quest log,
        // which supplies step props, which can add an item and push the bag again. The guard makes that
        // terminate at depth one rather than leaning on the prop grant being idempotent.
        if (_creditingCollectSteps) return;
        _creditingCollectSteps = true;
        try { AdvanceCollectQuests(player); }
        finally { _creditingCollectSteps = false; }
    }

    private bool _creditingCollectSteps;

    private void SendGold(Entity player) =>
        SendTo(player, "Gold", new GoldUpdate(player.Gold));

    private readonly HashSet<Guid> _hadBuffs = new();

    /// <summary>Caster + PARTY members within radius (the AoE ally target set). If the caster is
    /// not in a party, only the caster is affected — your heals/buffs no longer splash onto random
    /// strangers. Uses the grid's neighbourhood for efficiency.</summary>
    private IEnumerable<Entity> PlayersInRadius(Entity caster, float radius)
    {
        float r2 = radius * radius;
        yield return caster;
        if (!_world.Parties.TryGetValue(caster.Id, out var party))
            yield break;   // solo: self only
        foreach (var e in _world.Grid.Nearby(caster))
        {
            if (e.Kind != EntityKind.Player || e.Dead || e.Id == caster.Id)
                continue;
            if (!party.Contains(e.Id))
                continue;   // party members only
            // A HIDDEN party member is not here (BL-69). His rule is "act as u r not nearby", and a
            // party heal that silently found someone nobody can see would be exactly the leak that
            // makes a hide worth nothing — you would be locatable by watching a healer's numbers.
            if (e.Hidden)
                continue;
            float dx = e.X - caster.X, dy = e.Y - caster.Y;
            if (dx * dx + dy * dy <= r2)
                yield return e;
        }
    }

    /// <summary>Is this skill an improved (GROUP) buff with MORE THAN ONE child? Only those are worth
    /// collapsing on the buff bar. A potion and a scroll are one-child groups by the same mechanism,
    /// and merging one square into one square would only replace the effect's name with the bottle's.
    /// Checks every LEVEL because a group buff's levels are pure child references — the cleric's
    /// Improved Speed carries no children on the def itself.</summary>
    private static bool IsMultiChildGroup(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || SkillCatalog.Get(skillId) is not SkillDef def) return false;
        if (def.ChildBuffs is { Length: > 1 }) return true;
        if (def.Levels != null)
            foreach (var lvl in def.Levels)
                if (lvl.ChildBuffs is { Length: > 1 }) return true;
        return false;
    }

    /// <summary>The group's name as THIS character knows it (per-class flavour), for the one square
    /// its children collapse into.</summary>
    private static string GroupDisplayName(Entity p, string skillId) =>
        SkillCatalog.Get(skillId) is SkillDef def
            ? ClassSkills.DisplayName(def.Id, p.Race, p.BaseClass, p.Archetype, p.Discipline)
            : "";

    private void PushBuffs(Entity player)
    {
        var dtos = player.Buffs.Where(b => !b.Internal).Select(b => new BuffDto(
            b.Name, b.Description,
            b.Toggle ? -1f : b.TicksRemaining * GameConstants.TickSeconds, b.IsDebuff, b.Key, b.Stacks,
            b.Row, BuffIcon(player, b.SourceSkillId),
            IsMultiChildGroup(b.SourceSkillId) ? b.SourceSkillId : "",
            IsMultiChildGroup(b.SourceSkillId) ? GroupDisplayName(player, b.SourceSkillId) : "")).ToList();

        // The GRADE PENALTY rides along as a synthetic, never-expiring DEBUFF row. It is not a real
        // BuffInstance (nothing casts it — it's a property of what you're wearing), but without a row on
        // the bar there is NO way to tell whether it's applying, which is exactly what the owner hit when
        // he tried to verify it and had to report "not sure if the penalty is working".
        dtos.AddRange(GradePenaltyRows(player));

        if (dtos.Count == 0)
        {
            // Only send the empty update once, when the last row just went away.
            if (_hadBuffs.Remove(player.Id))
                SendTo(player, "Buffs", new BuffUpdate(Array.Empty<BuffDto>()));
            return;
        }

        _hadBuffs.Add(player.Id);
        SendTo(player, "Buffs", new BuffUpdate(dtos.ToArray()));
    }

    /// <summary>The emoji/glyph for a buff's SOURCE skill, resolved for the owner's class (so a cleric's
    /// Holy Speed can differ from a mage's Wind Walk). "" = no icon → the client shows the name's initials.</summary>
    private static string BuffIcon(Entity p, string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return "";
        string? classIcon = ClassSkills.Icon(skillId, p.Race, p.BaseClass, p.Archetype, p.Discipline);
        if (!string.IsNullOrWhiteSpace(classIcon)) return classIcon!;
        string defIcon = SkillCatalog.Get(skillId)?.Icon ?? "";
        return defIcon.Length > 0 ? defIcon : SkillIcons.For(skillId);
    }

    /// <summary>The grade-penalty debuff row(s) for the buff bar: one for over-grade armour/jewels, one
    /// for an over-grade weapon (they penalise different stats, so they are separate rows).
    /// SecondsLeft -1 = no timer, like a toggle: this lasts exactly as long as you wear the gear.</summary>
    private static IEnumerable<BuffDto> GradePenaltyRows(Entity p)
    {
        // NAME stays clean — the multiplier belongs in the description, not the label (owner).
        if (p.GradeArmorGap > 0)
        {
            int pct = (int)Math.Round((1f - p.GradeArmorPenalty) * 100f);
            yield return new BuffDto(
                "Over-Grade Armor",
                $"Your armor/jewels are {p.GradeArmorGap} grade(s) above you (x{p.GradeArmorPenalty:0.##}): "
                + $"-{pct}% P.Def, M.Def, evasion, and cast/attack/move speed. "
                + "Level up, or wear your own grade, to clear it.",
                -1f, true, "grade_penalty_armor", 1, BuffRow.Debuff, "🛡");
        }
        if (p.GradeWeaponGap > 0)
        {
            int pct = (int)Math.Round((1f - p.GradeWeaponPenalty) * 100f);
            yield return new BuffDto(
                "Over-Grade Weapon",
                $"Your weapon is {p.GradeWeaponGap} grade(s) above you (x{p.GradeWeaponPenalty:0.##}): "
                + $"-{pct}% P.Atk, M.Atk, crit rate, crit damage and accuracy. "
                + "Level up, or wield your own grade, to clear it.",
                -1f, true, "grade_penalty_weapon", 1, BuffRow.Debuff, "⚔");
        }
    }

    /// <summary>Reconcile the ACTIVE class's skill bar with what that class actually knows: drop
    /// assignments for skills it no longer has (one that got REPLACED by a better version, or that
    /// belongs to a class you swapped away from). It VALIDATES ONLY — it never adds and never moves.
    /// The bar is the player's layout, not ours.
    ///
    /// AUTO-PLACEMENT WAS REMOVED (owner, 2026-07-20). It used to park every newly-learned active skill
    /// in the first free slot, which made the bar rearrange itself under the player: every level-up and
    /// every skill learned dropped new icons into whatever gaps existed, and — worse — a skill the
    /// player had deliberately REMOVED from the bar was still *learned*, so the next push put it
    /// straight back. There is no way to keep a deliberately sparse bar against a helper that treats
    /// every gap as a mistake. A new character now starts with the two built-in actions
    /// (<see cref="GameConstants.DefaultSkillBar"/>) and the player places skills themselves.
    ///
    /// THIS LIVES ON THE SERVER ON PURPOSE. It used to run in the CLIENT on every Learned push, and the
    /// client SAVED the result. That meant any code path which pushed Learned while the client still
    /// held a different bar would persist a mangled layout — and the client would then receive the real
    /// bar and *look* correct while the server's copy was already destroyed. It bit twice (login, then
    /// the subclass switch) before being understood. The server owns the bar; the client now only writes
    /// it when the PLAYER edits it (drag, assign, remove).</summary>
    private static void SyncSkillBar(Entity p)
    {
        var slots = new string[GameConstants.SkillBarSlots];
        Array.Fill(slots, "");   // "" = an empty slot. new string[n] is full of NULLs, and the
                                 // free-slot search below looks for "" — leave them null and it finds
                                 // none, so a character with no saved bar (i.e. every new one) would
                                 // get NOTHING placed on it. Caught by tools/SmokeTest.

        var bar = p.ActiveSkillBar;
        for (int i = 0; i < slots.Length && i < bar.Length; i++)
            slots[i] = bar[i] ?? "";

        // Forget what this class no longer knows — but NEVER an item slot ("item:<defId>"), an action
        // slot ("action:<id>"), or an equip-PRESET slot ("preset:<abc>"): none is a learned skill, so
        // all three would otherwise be wiped here, yet all are valid entries the player placed on purpose
        // (a potion one click away; the basic-attack/target-closest buttons; an A/B/C gear swap).
        // ⚠ preset: was MISSING from this list, so an equip preset placed on the bar was stripped on the
        // very next re-sync (login / level-up / learn) — it vanished on relog. Device playtest 0.28.79.
        for (int i = 0; i < slots.Length; i++)
            if (!string.IsNullOrEmpty(slots[i])
                && !GameConstants.IsItemSlot(slots[i]) && !GameConstants.IsActionSlot(slots[i])
                && !GameConstants.IsPresetSlot(slots[i])
                && !p.LearnedSkills.ContainsKey(slots[i]))
                slots[i] = "";

        // NOTHING is added here — see the summary. Newly-learned skills stay off the bar until the
        // player drags them on.

        p.ActiveSkillBar = slots;
    }

    /// <summary>Push the character's skills — and, with them, the bar those skills live on.
    ///
    /// The bar is ALWAYS sent FIRST and from the SAME method, so the two can never arrive out of order.
    /// That ordering used to be the caller's problem, and two callers got it wrong. Now there is nothing
    /// to get wrong.</summary>
    private void SendLearned(Entity p)
    {
        SyncSkillBar(p);
        SendSkillBar(p);
        SendTo(p, "Learned", new LearnedSkills(
            p.LearnedSkills.Select(kv => new SkillRef(kv.Key, kv.Value)).ToArray(), p.SkillPoints));
    }

    private void SendStats(Entity p)
    {
        var (hpReg, mpReg) = StandingRegen(p);
        SendTo(p, "Stats", new StatsUpdate(
            p.Con, p.AtkStat, p.EffectiveWit, p.EffectiveAgi, p.EffectiveSpt,
            p.MaxHp, p.MaxMp, (int)p.EffectiveAttack, (int)p.EffectiveDefence,
            p.Accuracy, (int)p.EffectiveEvasion, p.CritChance, p.BasicAttackRange, p.SecondClass,
            p.EffectiveSpeed, SkillMath.CastModifier(p.Wit), p.EffectiveCastSpeedMultiplier, p.EffectiveAttackSpeedMultiplier, p.SkillPoints, p.MoveState, (int)p.EffectiveMagicAttackShown, p.MagicCritChance,
            p.HasShield, p.BlockChance, p.BlockReduction, p.ShieldDefense, (int)p.EffectiveMagicDefence,
            p.ActiveArmorSet, p.ArmorMasteryLabel,
            hpReg, mpReg, p.CritDamageBonus,
            p.MeleeVamp, p.SpellVamp, p.CooldownReduction,
            p.MagicResist, p.MagicFailMod,
            p.CritRateResist, p.CritDmgResist, p.BowResist,
            p.InterruptResist, (int)p.EffectiveMagicAttack,   // MagicAttackInternal: the cosmic IG-reference value
            p.HealPowerFlat, p.HealPowerMod, p.HealReceivedFlat, p.HealReceivedMod,
            p.CritDamageFlat, p.EffectiveMagicCritDamage));
    }

    /// <summary>The player's HP/MP regen per second AS IT IS ACTUALLY PAID right now — base + flat
    /// bonus, ×mastery mult, ×buff regen%, and then the same stance and safe-zone multipliers
    /// <see cref="Regenerate"/> applies.
    ///
    /// 🔑 It used to report the RUNNING baseline unconditionally, and that is a display that
    /// contradicts the rule it describes: walking is +20% and sitting +80%, but the stats window read
    /// the same number in all three stances, so the one place you could check the bonus said it did
    /// not exist. He caught it in playtest-22 — *"MP regen is unchanged when walking/running - or
    /// atleast vissually - it seems like its only visually"* — and he was exactly right: the server
    /// was paying the bonus the whole time. Now the number moves when the stance does.</summary>
    private static (float Hp, float Mp) StandingRegen(Entity p)
    {
        float hpPct = 0f, mpPct = 0f, hpFlat = 0f, mpFlat = 0f;
        foreach (var b in p.Buffs)
        {
            if (b.Has(SkillEffect.BuffHpRegen)) { hpPct += b.Percent(SkillEffect.BuffHpRegen); hpFlat += b.Flat(SkillEffect.BuffHpRegen); }
            if (b.Has(SkillEffect.BuffMpRegen)) { mpPct += b.Percent(SkillEffect.BuffMpRegen); mpFlat += b.Flat(SkillEffect.BuffMpRegen); }
        }
        // Read off Regenerate, in the same order, so the two cannot drift apart — INCLUDING the flat
        // per-second buff grant (Meditation), which is added beside the gear/passive flat bonus there.
        float stance = MovementTuning.RegenMultiplier(p.MoveState)
                     * (GameConstants.InSafeZone(p.X, p.Y) ? GameConstants.SafeZoneRegenMultiplier : 1f);
        float hp = (StatCalculator.HpRegenPerSecond(p.Con, p.Level) + p.HpRegenBonus + hpFlat) * p.HpRegenMult * (1f + hpPct) * stance;
        float mp = (StatCalculator.MpRegenPerSecond(p.EffectiveSpt, p.Level) + p.MpRegenBonus + mpFlat) * p.MpRegenMult * (1f + mpPct) * stance;
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
        {
            entity.SkillCooldowns[def.Id] = def.CooldownTicks;
            SendCooldowns(entity);   // ESC pays the reuse — the bar has to show that it did
        }

        entity.CastingSkillId = null;
        entity.CastTargetId = null;
        entity.CastTicksRemaining = 0;
        entity.CastInitialMpPaid = 0;
        entity.CastFromItemInstance = null;   // interrupted: the scroll stays in the bag
        if (entity.Kind == EntityKind.Mob)
        {
            // Clear the mob's cast bar on nearby clients (interrupt/cancel).
            var info = new MobCastInfo(entity.Id, "", 0f);
            foreach (var nearby in _world.Grid.Nearby(entity))
                if (_world.EntityToConnection.TryGetValue(nearby.Id, out var pc))
                    _ = _hub.Clients.Client(pc).SendAsync("MobCast", info);
        }
        else
        {
            SendTo(entity, "Cast", new CastInfo("", 0f));
        }
    }

    /// <summary>Player pressed ESC to cancel their own cast — starts cooldown.</summary>
    private void HandleCancelCast(CancelCastCmd cmd)
    {
        if (TryGetPlayer(cmd.ConnectionId, out var player))
            CancelCast(player, startCooldown: true);
    }

    /// <summary>Open a box/chest: consume one and roll each loot entry independently
    /// (chance 0..1). Gear arrives with rolled attributes. The box is consumed first so
    /// at least one slot is free for the loot.</summary>
    /// <summary>Learn the recipe a recipe-book item teaches (adds it to the char's KnownRecipes,
    /// which unlocks the DropOnly recipes). Consumes the book; a duplicate is refused (kept).</summary>
    private void HandleLearnRecipe(Entity player, InventoryItem item, ItemDef def)
    {
        string recipeId = def.TeachesRecipeId;
        if (RecipeCatalog.Get(recipeId) is not Recipe recipe)
        {
            SendSystemToEntity(player, "This recipe is no longer valid.");
            return;
        }
        if (player.KnownRecipes.Contains(recipeId))
        {
            SendSystemToEntity(player, "You already know that recipe.");
            return;
        }
        player.KnownRecipes.Add(recipeId);
        if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);

        string outName = ItemCatalog.Get(recipe.OutputId)?.Name ?? recipe.OutputId;
        // The blueprint is spent to UNLOCK, and each craft spends one more — tell the player up front.
        SendSystemToEntity(player,
            $"Unlocked the blueprint for {outName}. Each craft consumes another blueprint" +
            (recipe.Profession != Profession.None ? $" and needs the {recipe.Profession} profession." : "."));
        SendInventory(player);
        SendCrafting(player);   // the craft window lists a DropOnly recipe only once it is known
        SaveEntity(player);
    }

    /// <summary>Break one piece of gear down into crafting materials (`BL-22`).
    ///
    /// <para>His spec is two clauses and both are in <see cref="Crafting.Disassemble"/>, not here:
    /// *"rarity for mats rarity, grade for mats ammount"*. This method is only the transaction —
    /// what may be broken, and what the player loses by doing it.</para>
    ///
    /// <para>🔑 **"U give up gold to get mats."** That is the entire economic design, and it needs no
    /// code: the item is consumed, and it is the same item that would otherwise have been sold. Nothing
    /// pays gold here, and nothing should — the moment salvage also paid something, it would stop being
    /// a choice and become the strictly better option.</para>
    ///
    /// <para>⚠ The gates are deliberately the SELLING gates, not new ones. If a vendor would not buy it,
    /// this will not eat it: an EQUIPPED piece, and anything the instance itself marks unsellable (a
    /// bound newbie loaner, the Rune of Sinners). Otherwise "unsellable" would have become a loophole
    /// that launders a bound item into tradable materials, which is the one thing those tags exist to
    /// prevent. His *"trash"* means gear you were going to vendor.</para></summary>
    private void HandleDisassembleItem(DisassembleItemCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null) return;
        if (ItemCatalog.Get(item.DefId) is not ItemDef def) return;

        if (item.Equipped)
        {
            SendSystemToEntity(player, "Take it off first.");
            return;
        }
        if (!item.Sellable(def))
        {
            SendSystemToEntity(player, $"{item.Name(def)} can't be broken down.");
            return;
        }
        if (Crafting.Disassemble(def) is not Crafting.Salvage salvage)
        {
            SendSystemToEntity(player, $"{item.Name(def)} yields no materials.");
            return;
        }

        string matId = Crafting.MaterialId(salvage.Type, salvage.Rarity);
        string matName = Crafting.MaterialName(salvage.Type, salvage.Rarity);

        // Consume ONE first, so the freed slot is available to the materials. Gear is never stackable,
        // so this is a row removal; the Quantity branch is defensive, matching every other consumer.
        if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);

        if (!AddItem(player, matId, salvage.Qty, rollAttributes: false))
        {
            // The bag was full even after the piece came out of it — the materials are a different
            // stack. Put the item back rather than destroying it for nothing.
            AddItem(player, item.DefId, 1, rollAttributes: false);
            SendSystemToEntity(player, "Not enough room for the materials.");
            SendInventory(player);
            return;
        }

        SendInventory(player);
        SaveEntity(player);
        SendSystemToEntity(player,
            $"Broke down {item.Name(def)} into {salvage.Qty} x {matName}.");
    }

    private void HandleOpenBox(OpenBoxCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || item.Equipped) return;
        if (ItemCatalog.Get(item.DefId) is not ItemDef def || def.Slot != EquipSlot.Box) return;

        // Recipe book: teaches its recipe instead of rolling a loot table.
        if (ItemCatalog.IsRecipeBook(def))
        {
            HandleLearnRecipe(player, item, def);
            return;
        }

        if (BoxCatalog.Get(item.DefId) is not BoxDef box)
        {
            SendSystemToEntity(player, "This box can't be opened.");
            return;
        }

        // SELECTION box: don't consume yet — send the chooser; the player confirms picks.
        if (box.PickCount > 0)
        {
            var options = box.Entries
                .Where(e => e.ForClass is not BaseClass only || player.BaseClass == only)
                .Select(e => new SelectionOption(e.ItemId, ItemCatalog.Get(e.ItemId)?.Name ?? e.ItemId))
                .ToArray();
            // The offer carries what THIS box still owes, not what the def started with (`BL-20`):
            // re-opening a Blessing Box you took 5 from offers 5, and the client's counter reads 0/5.
            SendTo(player, "Selection", new SelectionOffer(item.InstanceId, def.Name, options, PicksAvailable(item, box)));
            return;
        }

        // RUNE box: grant the single rune and STAMP its wall-clock expiry (the box's GrantsRuneSeconds)
        // starting NOW — buying the sealed box never started the clock; opening does. Needs a free slot.
        if (def.GrantsRuneSeconds > 0 && box.Entries.Length >= 1
            && ItemCatalog.Get(box.Entries[0].ItemId) is { IsRune: true } runeDef)
        {
            var before = player.Inventory.Where(i => i.DefId == runeDef.Id).Select(i => i.InstanceId).ToHashSet();
            if (!AddItem(player, runeDef.Id, 1, rollAttributes: false))   // AddItem stamps the rune's DEFAULT expiry
            {
                SendSystemToEntity(player, "Open the box with a free inventory slot.");
                return;   // box NOT consumed
            }
            if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);
            // OVERRIDE the default with the BOX's duration (1h/2h/24h/30d) on the rune just added.
            var rune = player.Inventory.FirstOrDefault(i => i.DefId == runeDef.Id && !before.Contains(i.InstanceId));
            if (rune != null) rune.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(def.GrantsRuneSeconds);
            ReconcileTimedItems(player);   // apply its buff immediately
            SendInventory(player);
            SaveEntity(player);
            SendSystemToEntity(player, $"{def.Name} opened — {runeDef.Name} is now active.");
            return;
        }

        // Consume one box (frees a slot for the loot).
        if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);

        var got = new List<string>();
        bool full = false;
        foreach (var entry in box.Entries)
        {
            // A class-conditional entry (the training boxes) is invisible to the other base class.
            if (entry.ForClass is BaseClass only && player.BaseClass != only) continue;
            if (_rng.NextDouble() >= entry.Chance) continue;
            int qty = entry.MaxQty > entry.MinQty
                ? _rng.Next(entry.MinQty, entry.MaxQty + 1)
                : entry.MinQty;
            if (qty <= 0) continue;

            // Stackables merge in one AddItem; non-stackable gear needs one call each.
            bool stackable = ItemCatalog.Get(entry.ItemId)?.Slot is EquipSlot.Consumable or EquipSlot.Scroll;
            int added = 0;
            if (stackable)
            {
                if (AddItem(player, entry.ItemId, qty, rollAttributes: true)) added = qty;
            }
            else
            {
                for (int k = 0; k < qty; k++)
                {
                    if (!AddItem(player, entry.ItemId, 1, rollAttributes: true)) { full = true; break; }
                    added++;
                }
            }
            if (added > 0)
                got.Add($"{ItemCatalog.Get(entry.ItemId)?.Name ?? entry.ItemId}{(added > 1 ? $" x{added}" : "")}");
            if (full) { SendSystemToEntity(player, "Your inventory is full — some loot was lost."); break; }
        }

        SendInventory(player);
        SaveEntity(player);
        SendSystemToEntity(player, got.Count > 0
            ? $"{def.Name}: {string.Join(", ", got)}."
            : $"{def.Name}: nothing this time.");
        AdvanceActionQuests(player, QuestActions.OpenBox);   // the tutorial's box beat (`58a`)
    }

    /// <summary>How many picks a selection box still owes: its own part-spent counter if it has one,
    /// otherwise the def's full <see cref="BoxDef.PickCount"/> (`BL-20`).</summary>
    private static int PicksAvailable(InventoryItem item, BoxDef box) =>
        Math.Clamp(item.PicksRemaining ?? box.PickCount, 0, box.PickCount);

    /// <summary>Player confirmed their picks from a SELECTION box: validate the chosen
    /// ids against the box's options, grant them, and KEEP the box if picks are left over.
    ///
    /// <para>🔑 `BL-20`, his words: *"I'll want to be able to pick 5 and I get my 5 scrolls + the box
    /// for the other 5."* So a partial pick is now legal and costs nothing — you take what you want,
    /// and the box stays in the bag carrying the rest (<see cref="InventoryItem.PicksRemaining"/>).
    /// The box is consumed only when its last pick is spent.</para>
    ///
    /// <para>This is the third position this code has held, and the middle one was the bug: originally
    /// a partial pick CONSUMED the whole box and forfeited the remainder (playtest-19 `48g` — 7 of 10
    /// from a 250k box), which was then fixed by REFUSING anything but a full spend. That refusal was
    /// right only as long as there was nowhere to put the leftovers. Now there is.</para>
    ///
    /// <para>⚠ Granting happens BEFORE the counter is written down, and the counter is decremented by
    /// what was actually GRANTED, not by what was asked for — so picks lost to a full inventory stay
    /// in the box instead of evaporating. That is the same failure `48g` was about, one layer in.</para>
    ///
    /// <para>Picks are a BUDGET, not a set of ticks: the same option may be taken several times, so a
    /// pick-of-10 can be 5 + 3 + 2 (owner, playtest-20 `53a` — he named that shape). It used to
    /// <c>Distinct()</c> the request and demand that many DIFFERENT items, which is the over-correction
    /// he hit: wanting five of one scroll was not expressible, so the box refused the pick. Repeats in
    /// <see cref="SelectBoxItemsCmd.ItemIds"/> ARE the quantity; nothing else had to change to say it.</para></summary>
    private void HandleSelectBoxItems(SelectBoxItemsCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || item.Equipped) return;
        if (ItemCatalog.Get(item.DefId) is not ItemDef def || def.Slot != EquipSlot.Box) return;
        if (BoxCatalog.Get(item.DefId) is not BoxDef box || box.PickCount <= 0) return;

        // The same class filter the offer was built with, re-applied on the way back in: the offer is
        // advisory, the confirm is authoritative.
        var optionIds = box.Entries
            .Where(e => e.ForClass is not BaseClass only || player.BaseClass == only)
            .Select(e => e.ItemId).ToHashSet();
        var chosen = cmd.ItemIds.Where(optionIds.Contains).ToList();

        // At least one, at most what the box still owes. Overspending is still refused outright —
        // an 11th pick from a box holding 10 is a client that has lost track, not a choice to honour.
        int available = PicksAvailable(item, box);
        if (chosen.Count < 1 || chosen.Count > available)
        {
            SendSystemToEntity(player,
                $"Select between 1 and {available} item{(available == 1 ? "" : "s")} — you have {chosen.Count}.");
            return;
        }

        // ⚠ A STACKED selection box would share one counter across every copy in the row. No box def is
        // IsStackable, so this cannot happen today; if one ever becomes stackable, the old all-or-
        // nothing rule is the safe answer rather than a silently wrong count.
        if (item.Quantity > 1 && chosen.Count < available)
        {
            SendSystemToEntity(player, $"Spend all {available} picks, or hold only one {def.Name} at a time.");
            return;
        }

        // Grant the chosen items — counted, so five of one scroll is one stack operation and one line
        // of feedback rather than five. The box is NOT consumed yet: it still occupies its own slot,
        // which is what keeps the bag arithmetic honest while the picks come in.
        var got = new List<string>();
        int granted = 0;
        foreach (var group in chosen.GroupBy(id => id))
        {
            int qty = group.Count();
            string name = ItemCatalog.Get(group.Key)?.Name ?? group.Key;
            int added = 0;
            for (int k = 0; k < qty; k++)
            {
                if (!AddItem(player, group.Key, 1, rollAttributes: true)) break;
                added++;
            }
            granted += added;
            if (added > 0) got.Add(added > 1 ? $"{name} x{added}" : name);
            if (added < qty)
            {
                SendSystemToEntity(player, "Your inventory is full — the picks you couldn't carry stay in the box.");
                break;
            }
        }

        // Spend exactly what was GRANTED. Anything left keeps the box alive at the smaller number;
        // the last pick is what finally consumes it.
        int left = available - granted;
        if (left > 0)
        {
            item.PicksRemaining = left;
        }
        else
        {
            if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);
        }

        SendInventory(player);
        SaveEntity(player);
        SendSystemToEntity(player, got.Count > 0
            ? $"{def.Name}: {string.Join(", ", got)}."
              + (left > 0 ? $" {left} pick{(left == 1 ? "" : "s")} left in the box." : "")
            : $"{def.Name}: nothing chosen.");
        if (granted > 0)
            AdvanceActionQuests(player, QuestActions.OpenBox);   // the tutorial's box beat (`58a`)
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

    /// <summary>Build the expanded target window: the target's detailed stats and,
    /// for a mob, its passive modifier lines (from the MobCatalog template).</summary>
    private void HandleInspectTarget(InspectTargetCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (!_world.Entities.TryGetValue(cmd.TargetId, out var t)) return;
        if (t.Kind == EntityKind.Npc) return;   // plain NPCs have nothing to inspect

        bool isMob = t.Kind == EntityKind.Mob;
        var template = isMob && t.MobTypeId is not null ? MobCatalog.Get(t.MobTypeId) : null;
        var passiveLines = new List<string>();
        // BL-47 — a player-built creature says so FIRST, above its passives, because the loadout is the
        // thing under test and its passives are only what the loadout did not cover. Reusing this list
        // is what keeps the demo free of a protocol change and therefore of a new APK.
        if (template?.Build is MobBuild built) passiveLines.AddRange(built.Describe());
        if (template?.Mod is MobMod mod) passiveLines.AddRange(mod.Describe());
        string[] passives = passiveLines.ToArray();

        // A mob's ACTIVE kit — the Skills tab (playtest-20: "a new tab for the mob's skills, actives
        // and passives"). `LearnedSkills` is the WHOLE truth about what a mob can throw at you:
        // BuildMob parks the Mage role's two spells there and a Boss's BossCatalog kit (or the generic
        // slam) — nothing else grants a mob a skill. So an empty list is not missing data, it is the
        // answer "this one only swings", and the client says exactly that.
        //
        // Formatted HERE rather than shipping ids for the client to resolve, because that is what the
        // other two lists in this window already do (Passives, Drops) and because the numbers are
        // level-resolved: the same spell id is a different power on a level-20 and a level-70 caster.
        string[] skills = Array.Empty<string>();
        if (isMob && t.LearnedSkills.Count > 0)
        {
            var lines = new List<string>();
            foreach (var kv in t.LearnedSkills.OrderBy(kv => SkillCatalog.Get(kv.Key)?.Name ?? kv.Key))
            {
                if (SkillCatalog.Get(kv.Key) is not SkillDef def) continue;
                lines.Add(def.MaxLevel > 1 ? $"{def.Name}  (Lv {kv.Value})" : def.Name);

                var facts = new List<string> { def.Category.ToString() };
                if (def.Range > 0f) facts.Add($"{def.Range:0} range");
                if (def.AreaRadius > 0f) facts.Add($"{def.AreaRadius:0} radius");
                facts.Add($"{def.CastTicks / 10f:0.#}s cast");
                facts.Add($"{def.CooldownTicks / 10f:0.#}s reuse");
                lines.Add("   " + string.Join(" · ", facts));

                var cost = new List<string>();
                if (def.PowerAt(kv.Value) > 0) cost.Add($"power {def.PowerAt(kv.Value)}");
                if (def.MpCostAt(kv.Value) > 0) cost.Add($"{def.MpCostAt(kv.Value)} MP");
                if (cost.Count > 0) lines.Add("   " + string.Join(" · ", cost));
                if (!string.IsNullOrEmpty(def.Description)) lines.Add("   " + def.Description);
            }
            skills = lines.ToArray();
        }

        // Active temporary effects on the target — including DoT stack counters (so the
        // attacker can read "Bleed x5" on the enemy and time a burst).
        var effects = t.Buffs
            .Select(b => b.Stacks > 1 ? $"{b.Name} x{b.Stacks}" : b.Name)
            .ToArray();

        // A mob's level-appropriate DROPS (behind the client's [Details] button). Chance shown is the
        // EFFECTIVE one (after the global drop-rate). Computed ONLY when the client asks (the [Details]
        // click sets WithDrops) — the 1s refresh loop leaves it false, so the static drop table isn't
        // re-resolved and re-serialized every second for every player inspecting a mob.
        //
        // GROUPED entries are collapsed to ONE line per group, showing the group's own chance and the
        // items it can pick between. That is not cosmetic: since the drop rework a mob carries ~97 entries
        // (four slot families x four qualities x every line in the family), and listing them raw gave a
        // 97-row popup of near-identical 0.6% lines that told the player nothing. One line per group —
        // "Leathers / Bulwark / Robe (5%)" — is both shorter AND more truthful, because the 5% really is
        // one roll shared between them, not three independent ones.
        string[]? drops = null;
        if (cmd.WithDrops && isMob && t.MobTypeId is not null && MobCatalog.Get(t.MobTypeId).Drops is { } table)
        {
            var rows = table.Where(d => d.AppliesAtLevel(t.Level)).ToList();
            if (t.Rank != MobRank.Normal)
            {
                rows.RemoveAll(d => MobCatalog.IsGearGroup(d.GroupId));
                rows.AddRange(MobCatalog.GearDrops(t.Level, t.Rank));
                rows.AddRange(MobCatalog.EnchantScrollDrops(t.Level, t.Rank));
                rows.AddRange(MobCatalog.EliteMatDrops(
                    t.Level, t.Rank, MobCatalog.Get(t.MobTypeId).Category));
            }
            string ItemLine(DropEntry d)
            {
                string name = ItemCatalog.Get(d.ItemId)?.Name ?? d.ItemId;
                return d.MaxQty > 1 ? $"{name} x{d.MinQty}-{d.MaxQty}" : name;
            }
            // "Armor · Rare", "Mats", "Scrolls" — the group's tuning name (the word /droprate takes),
            // plus the rarity for the four gear families, which is the half that tells them apart.
            string GroupTitle(int groupId)
            {
                string name = MobCatalog.GroupName(groupId);
                string head = char.ToUpperInvariant(name[0]) + name.Substring(1);
                return MobCatalog.IsGearGroup(groupId)
                    ? $"{head} · {(ItemRarity)((groupId - 10) % 10)}" : head;
            }

            // The chances are shown as THIS player would roll them, so a Rune of Drop moves the numbers
            // on the screen it moves in the kill roll. Reading the def's rate here instead would make the
            // inspect list quietly lie to every player wearing one.
            float lookMult = player.Runes.DropChance;

            // 🔴 …AND THE LEVEL GAP, which this list did NOT apply and the kill roll always did (playtest
            // 23): *"the drop value with double drop rune shows double chances ..the problem is there
            // should be the same penalty as exp/sp when mob and player have a difference and that penalty
            // is not displayed ... you can add the actual drop rates from the penalty as well."* It is the
            // same `ExpCurve.LevelGapMultiplier(killer.Level - mob.Level)` RollDrop uses, off the same two
            // levels — so the tab now shows the chance THIS character rolls against THIS creature, which
            // is what the rune half already promised and the gap half quietly broke.
            //
            // 🔑 The rune was the tell. Both are per-player scalars on the same roll; showing one and
            // hiding the other is worse than showing neither, because the visible one certifies the
            // number as personal and it is then wrong by up to 100%.
            float lookGap = isMob ? ExpCurve.LevelGapMultiplier(player.Level - t.Level) : 1f;
            float Shown(DropEntry d) => MobCatalog.EffectiveChance(d, lookMult) * lookGap;

            // Above 100% a percentage stops meaning anything ("250%" is not a chance), so the label
            // switches to what the roll actually does at that rate: copies per kill. Plain "x", never
            // "×" — the client's TMP atlas is static and does not carry the multiplication sign.
            static string Odds(double c) => c >= 1.0 ? $"x{c:0.##}/kill" : $"{c * 100:0.##}%";

            var lines = new List<string>();
            // The header states the penalty rather than leaving the reader to wonder why a 5% row reads
            // 1.4%. Silent at ×1.00 — an in-band kill is the common case and needs no explanation — and
            // it names the gap, because the gap is the thing the player can actually change.
            if (lookGap < 0.999f)
            {
                int gap = player.Level - t.Level;
                lines.Add(lookGap <= 0f
                    ? $"⚠ {Math.Abs(gap)} levels apart — this creature drops NOTHING for you."
                    : $"⚠ {Math.Abs(gap)} levels apart: every chance below is already cut to {lookGap * 100:0.#}%.");
            }
            // GroupId 0 rolls independently, so each entry is its own row carrying its own chance.
            foreach (var d in rows.Where(d => d.GroupId == 0))
                lines.Add($"{ItemLine(d)}  ({Odds(Shown(d))})");
            // A GROUP is ONE roll shared by its members, so it reads as a TREE (32f): a title line with
            // the group's own chance, then the items it can land on indented beneath. As flat rows a
            // single 5% group looked like five separate 5% drops, which is five times the truth.
            foreach (var g in rows.Where(d => d.GroupId != 0).GroupBy(d => d.GroupId))
            {
                // 🔑 THE GROUP LINE CARRIES NO NUMBER (owner, 2026-08-18). It reads as a WRAPPER — what
                // the members have in common (they are mutually exclusive) — and every number on screen
                // is an item's own effective per-kill chance. His reasoning: a group % invites the
                // reading "the group fires at 7%, and then the item has its own chance on top", which is
                // not what this engine does — a member's authored chance IS its marginal, and the
                // trigger is merely their sum. Showing the sum made the reader do arithmetic to recover
                // the only number they actually wanted: *"C is good because you see effective drop per
                // item ... you see 0.25% and know that this item will be dropped in about 400 kills."*
                //
                // ⚠ This supersedes playtest-16's "add the rows also individual %" tree, which printed
                // BOTH. The members still print individually — only the group's own % line is gone.
                lines.Add(GroupTitle(g.Key));
                double weightSum = g.Sum(d => (double)MobCatalog.ItemWeight(d));
                double trigger = g.Sum(d => (double)MobCatalog.EffectiveChance(d, lookMult)) * lookGap;
                foreach (var d in g.GroupBy(ItemLine))
                {
                    if (weightSum <= 0) { lines.Add("   " + d.Key); continue; }
                    double share = d.Sum(x => (double)MobCatalog.ItemWeight(x)) / weightSum;
                    lines.Add($"   {d.Key}  ({Odds(trigger * share)})");
                }
            }
            drops = lines.ToArray();
        }

        var (hpReg, mpReg) = StandingRegen(t);
        SendTo(player, "TargetDetails", new TargetDetails(
            // Level: real for a mob, withheld for a player (see the snapshot builder) — unless you are
            // inspecting yourself, where it is your own to read.
            t.Id, t.Name, isMob || t.Id == player.Id ? t.Level : 0, isMob,
            t.Hp, t.MaxHp, t.Mp, t.MaxMp,
            t.AttackPower, (int)t.EffectiveMagicAttackShown,   // shrunk display, matches the stats window
            (int)t.EffectiveDefence, (int)t.EffectiveMagicDefence,
            t.Accuracy, t.Evasion, t.CritChance,
            t.BowResist, t.CritRateResist,
            passives, effects, drops,
            // Extended: same fields the character sheet reads, off the target Entity's own getters.
            Con: t.Con, Atk: t.AtkStat, Wit: (int)t.EffectiveWit, Agi: (int)t.EffectiveAgi, Spt: (int)t.EffectiveSpt,
            MoveSpeed: t.EffectiveSpeed, AttackSpeedMult: t.EffectiveAttackSpeedMultiplier,
            CastSpeedMult: t.EffectiveCastSpeedMultiplier, AttackRange: t.BasicAttackRange,
            MagicCritChance: t.MagicCritChance, CritDamage: t.CritDamageBonus,
            MeleeVamp: t.MeleeVamp, SpellVamp: t.SpellVamp, CooldownReduction: t.CooldownReduction,
            HpRegen: hpReg, MpRegen: mpReg,
            InterruptResist: t.InterruptResist, CritDmgResist: t.CritDmgResist, MagicResist: t.MagicResist,
            Rank: isMob ? t.Rank.ToString() : "",
            Skills: skills,
            // Behaviour (playtest 23). Aggression is read off the SPAWN, not the template: a zone can
            // turn a passive creature hostile (`SpawnZone.AllAggressive`), and the sheet has to describe
            // the thing in front of you rather than its species.
            Aggressive: isMob && t.Aggressive,
            SocialClan: isMob && GameConstants.MobClansEnabled && t.MobTypeId is string mid
                        ? MobCatalog.Get(mid).Clan : ""));
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
    /// <paramref name="critFlatFactor"/> carries the attacker's FLAT crit damage (the CSVs'
    /// "crit dmg +80", already turned into a factor by StatCalculator.CritFlatFactor because it
    /// joins pAtk INSIDE the ratio) — it applies on a crit only, before the crit multiplier.
    /// Returns the final damage and the outcome (Crit / Block / Hit).</summary>
    private (int damage, CombatOutcome outcome) ResolvePhysicalCritAndBlock(
        Entity attacker, Entity target, int baseDamage, float critChance, float blockAccuracy,
        float critFlatFactor = 1f)
    {
        // Bow/arrow resistance lowers all damage from a bow attacker (hit/crit/block alike).
        if (attacker.WeaponType == WeaponType.Bow && target.BowResist > 0f)
            baseDamage = Math.Max(1, (int)(baseDamage * (1f - target.BowResist)));

        // Shield AND the target's crit-rate resist lower the attacker's crit CHANCE. The resist is
        // his `enemy_light_armor_mastery` and is a MULTIPLIER, never a subtraction: subtracting a
        // rogue's 0.15 annihilated every low-crit build (an 11.4% blunt warrior critted 0.0%);
        // as a multiplier he keeps 9.7%. Same reasoning as the flat term in the crit-rate model.
        float effCrit = Math.Clamp(
            (critChance - (target.HasShield ? target.ShieldCritDefense : 0f))
            * (1f - target.CritRateResist), 0f, 1f);

        if (_rng.NextDouble() < effCrit)
        {
            // Crit-damage resist trims the EXTRA (above-normal) crit damage. The flat
            // crit-damage term is inside critFlatFactor, so the above-normal part of the
            // crit is (factor × mult − 1): flat first (it is attack), multiplier on top.
            float mult = StatCalculator.PhysicalCritMult(attacker.CritDamageBonus);
            float extra = (critFlatFactor * mult - 1f) * (1f - target.CritDmgResist);
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

    /// <summary>Resolution for a "[Double]" physical SKILL — our name for IG's physical skill
    /// crit: a flat ×2 and NOTHING else (it never touches crit-damage values, which is the whole
    /// point of the name). Chance is the caster's ATK curve (2.5-25%, StatCalculator.
    /// PhysicalDoubleChance), lowered by shield/crit-rate resist and ignoring the block on a
    /// double (like a crit); otherwise a normal block roll. Skills without the [Double] flag
    /// never reach here (they use the basic crit path, unchanged).</summary>
    private (int damage, CombatOutcome outcome) ResolvePhysicalDouble(
        Entity attacker, Entity target, int baseDamage, float doubleChance, float blockAccuracy)
    {
        if (attacker.WeaponType == WeaponType.Bow && target.BowResist > 0f)
            baseDamage = Math.Max(1, (int)(baseDamage * (1f - target.BowResist)));

        float eff = Math.Clamp(
            (doubleChance - (target.HasShield ? target.ShieldCritDefense : 0f))
            * (1f - target.CritRateResist), 0f, 1f);
        if (doubleChance > 0f && _rng.NextDouble() < eff)
        {
            // ×2 = +100% over normal, trimmed by the target's crit-damage resist.
            float extra = 1f * (1f - target.CritDmgResist);
            return (Math.Max(1, (int)(baseDamage * (1f + extra))), CombatOutcome.Double);
        }

        if (target.HasShield)
        {
            float effBlock = Math.Clamp(target.BlockChance - blockAccuracy, 0f, StatCaps.BlockChance);
            if (_rng.NextDouble() < effBlock)
                return (Math.Max(1, (int)(baseDamage * (1f - target.BlockReduction))), CombatOutcome.Block);
        }

        return (baseDamage, CombatOutcome.Hit);
    }

    /// <summary>Resolution for a BLOW skill (dagger Stab) — docs/design/CritBlowAndDouble.md §2.
    /// CRIT is the gate: the blow deals its full damage only if it crits (dagger crit chance,
    /// lowered by shield/crit resist). A landed blow is then computed WITH THE CRIT-DAMAGE VALUES
    /// — the flat crit-damage add (critFlatFactor) and the crit multiplier — because a blow scales
    /// off crit damage, not off p.Atk (7-11k of skill power against under 1k of p.Atk). ONLY after
    /// that does it roll a DOUBLE (the caster's ATK curve) for a further ×2. A blow that FAILS to
    /// crit deals a flat BlowFailFraction of its damage — that floor can neither crit nor double
    /// (a soft floor, not IG's 0-damage whiff). Blows bypass shields, so the floor isn't blocked.</summary>
    private (int damage, CombatOutcome outcome) ResolveBlow(
        Entity attacker, Entity target, int baseDamage, SkillDef def, float critFlatFactor = 1f)
    {
        // The blow's OWN crit modifier rides on the character's rate (IG: a blow never landed on
        // the raw crit rate). It is what pays for crit going multiplicative — see CritRateMod.
        float effCrit = Math.Clamp(
            (attacker.CritChance * def.CritRateMod - (target.HasShield ? target.ShieldCritDefense : 0f))
            * (1f - target.CritRateResist), 0f, 1f);

        if (_rng.NextDouble() >= effCrit)
            // Missed the crit: soft floor only — cannot crit or double.
            return (Math.Max(1, (int)(baseDamage * def.BlowFailFraction)), CombatOutcome.Hit);

        // Crit landed → apply the crit-damage values (flat add inside the ratio, then the
        // multiplier), trimmed by the target's crit-damage resist exactly as a normal crit is.
        float mult = StatCalculator.PhysicalCritMult(attacker.CritDamageBonus);
        float extra = (critFlatFactor * mult - 1f) * (1f - target.CritDmgResist);
        int damage = Math.Max(1, (int)(baseDamage * (1f + extra)));

        // THEN roll a separate double on top (ATK, never AGI — AGI already bought the crit above).
        if (def.CanDouble)
        {
            float dbl = Math.Clamp(
                (StatCalculator.PhysicalDoubleChance(attacker.AtkStat)
                 - (target.HasShield ? target.ShieldCritDefense : 0f))
                * (1f - target.CritRateResist), 0f, 1f);
            if (_rng.NextDouble() < dbl)
                // ×2, trimmed by crit-dmg resist. Reported as Double so the player can tell the
                // two mechanics apart — a doubled blow is visibly not "just a bigger crit".
                return (Math.Max(1, (int)(damage * (1f + (1f - target.CritDmgResist)))), CombatOutcome.Double);
        }
        return (damage, CombatOutcome.Crit);
    }

    /// <summary>Which damage channel a hit belongs to, for the damage-out pipeline.</summary>
    private enum DamageKind { Basic, SkillPhysical, SkillMagic }

    /// <summary>Central damage-OUT pipeline: applies the attacker's channel bonus (phys-skill
    /// / magic-skill / basic), the PvP/PvE context bonus (target a player vs a mob), and a
    /// skill's per-context multiplier. All factors default neutral, so this is a no-op until
    /// effects/skills set them — the base layout for the future PvP/PvE damage system.</summary>
    private int FinalizeDamage(Entity attacker, Entity target, int dmg, DamageKind kind, SkillDef? skill)
    {
        bool pvp = attacker.Kind == EntityKind.Player && target.Kind == EntityKind.Player;
        // Pick the single matrix cell for this context × source.
        float bonus = (pvp, kind) switch
        {
            (false, DamageKind.SkillPhysical) => attacker.PveSkillDamageBonus,
            (false, DamageKind.SkillMagic)    => attacker.PveMagicDamageBonus,
            (false, DamageKind.Basic)         => attacker.PveBasicDamageBonus,
            (true,  DamageKind.SkillPhysical) => attacker.PvpSkillDamageBonus,
            (true,  DamageKind.SkillMagic)    => attacker.PvpMagicDamageBonus,
            _                                 => attacker.PvpBasicDamageBonus,
        };
        float skillMult = skill is null ? 1f : (pvp ? skill.PvpDamageMult : skill.PveDamageMult);
        // Conditional damage: +% when the target is in one of the skill's rewarded states.
        float condBonus = (skill is not null && skill.ConditionalOn != TargetCondition.None
            && TargetMatches(target, skill.ConditionalOn)) ? skill.ConditionalDamagePct : 0f;
        // Raid ±10 rule: a player's damage to a BOSS is scaled by the level gap (anti-cheese).
        float raidMult = (target.Rank == MobRank.Boss && attacker.Kind == EntityKind.Player)
            ? StatCalculator.RaidLevelGapMult(attacker.Level, target.Level) : 1f;
        // The RECEIVING side of the PvP matrix: the target's own gear can cut what it takes from another
        // player (the S heavy/light sets' "PVP Dmg Received x0.95"). PvP only — `pvp` already means
        // player-hits-player, so a mob's swing is never reduced by it. Defaults to 1.
        float takenMult = pvp ? target.PvpDamageTaken : 1f;
        float result = dmg * (1f + bonus) * (1f + condBonus) * skillMult * raidMult * takenMult;
        // A skill explicitly multiplied to 0 in this context deals 0 (e.g. a mob-only nuke
        // vs a player); otherwise a real hit is at least 1.
        return skillMult <= 0f ? 0 : Math.Max(1, (int)result);
    }

    /// <summary>Does the target satisfy any of the rewarded control states?</summary>
    private static bool TargetMatches(Entity t, TargetCondition c) =>
        (c.HasFlag(TargetCondition.Slowed)  && t.IsSlowed)  ||
        (c.HasFlag(TargetCondition.Rooted)  && t.IsRooted)  ||
        (c.HasFlag(TargetCondition.Stunned) && t.IsStunned) ||
        (c.HasFlag(TargetCondition.Feared)  && t.IsFeared);

    private void TryInterruptCast(Entity target, int attackerInterruptPower)
    {
        if (target.CastingSkillId is null)
            return;
        var def = SkillCatalog.Get(target.CastingSkillId);
        if (def is null)
            return;

        // Fragile casts (Return) are cancelled by ANY damage — no interrupt contest.
        float chance = def.FragileCast ? 1f
            : StatCalculator.InterruptChance(
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

            foreach (string? dedicated in zr.InitialFill(_lastPhase))
                SpawnOneInZone(zr, dedicated);
        }

        // Say out loud which quest targets NO camp guarantees. A quest whose TargetId is misspelt, or
        // whose level window no longer overlaps the camp that holds the creature, is otherwise invisible
        // until a player takes it and cannot finish it.
        string[] unserved = WorldPlan.UnservedKillTargets();
        if (unserved.Length > 0)
            _log.LogWarning("Quest kill targets with no dedicated spawner: {Targets}",
                            string.Join(", ", unserved));
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
            foreach (string? dedicated in zr.DueToSpawn(_tick, phase))
                SpawnOneInZone(zr, dedicated);
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

        // Re-init zone alive-counts and fill those now active (and empty). The counts are re-seeded from
        // the world rather than adjusted, because a phase flip REMOVES mobs without killing them — no
        // OnDeath runs, so the zone's own tallies (mixed pool and each dedicated bucket) are stale.
        foreach (var zr in _zones)
        {
            zr.ResetAlive(_world.Entities.Values
                .Where(e => e.Kind == EntityKind.Mob && e.ZoneId == zr.Zone.Id && !e.Dead)
                .Select(e => e.MobTypeId ?? ""));

            foreach (string? dedicated in zr.RefillNeeded(phase))
                SpawnOneInZone(zr, dedicated);
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
            // "Elder Marius" is drawn as `Elder` over `Marius` (owner) — the role goes on the TITLE
            // line every plate already has, and the name line holds a name. One long run-on was what
            // made a row of NPCs in a town square unreadable, and it is also what made them hard to
            // pick out by name. ⚠ The catalog keeps the FULL authored name: quest hints, the dialog
            // header and every doc still say "Elder Marius".
            var (role, personal) = TitleCatalog.SplitNpcName(npc.Name);
            var entity = new Entity
            {
                Name = personal,
                Title = role,
                TitleColor = role.Length > 0 ? TitleCatalog.NpcHex : "",
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

        // Report what each region actually CONTAINS. Region membership is geometric, so a polygon
        // authored slightly wrong contains no spawners and fails silently — it would still draw, still
        // teleport, and simply never show a level band. Printing the counts at startup makes a bad
        // outline obvious on the first run instead of during a playtest.
        foreach (var region in RegionMap.All)
        {
            var band = RegionMap.LevelBand(region.Id);
            // The managing city and the gate names are printed too: both are AUTHORED-ONCE, DERIVED-EVERYWHERE
            // (the city decides where you respawn, the gates are the gatekeeper's whole menu), and both are
            // invisible until you die in the wrong place or open a gatekeeper on a phone.
            _log.LogInformation("Region {Name}: {Spawners} spawner(s), {Band}{City}",
                region.Name,
                RegionMap.SpawnersIn(region.Id).Length,
                band is null ? "peaceful" : $"Lv {band.Value.Min}-{band.Value.Max}",
                region.CityId.Length == 0 ? "" : $", managed by {Towns.ById(region.CityId)?.Name ?? region.CityId}");
            foreach (var gate in region.Gates)
                if (region.Kind == RegionKind.Field)
                    _log.LogInformation("    gate '{Gate}' — {Desc}", gate.Name, gate.Description);
        }
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

        // A PK (red) is an outlaw — vendors won't deal with them. A merely FLAGGED (purple) player
        // still can (owner). Selling to a vendor is unaffected either way.
        if (FlagOf(player) == PvpFlag.Pk)
        {
            SendSystemToEntity(player, "Merchants won't trade with a PK. Clear your karma first.");
            return;
        }

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
        if (!stackable && player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
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
        // Per INSTANCE since `58d`: a copy handed out with sellPrice -1 is refused even though the
        // catalog would happily buy the ordinary version of it.
        if (!item.Sellable(def))
        {
            SendSystemToEntity(player, "That can't be sold.");
            return;
        }

        bool stackable = def.Slot is EquipSlot.Consumable or EquipSlot.Scroll;
        int qty = stackable ? Math.Clamp(cmd.Quantity, 1, item.Quantity) : 1;
        long total = item.SellPrice(def) * qty;

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

        // Remember it for BUY-BACK — re-buyable at any vendor for the same price, restored faithfully
        // (enchant + rolled attributes). In-memory, newest last, oldest dropped past the cap.
        player.BuyBack.Add(new BuyBackEntry
        {
            DefId = def.Id, Quantity = qty, Enchant = item.Enchant,
            Attributes = new List<ItemAttribute>(item.Attributes),
            UnitPrice = ItemCatalog.SellPrice(def),
        });
        while (player.BuyBack.Count > GameConstants.BuyBackSlots) player.BuyBack.RemoveAt(0);

        SendGold(player);
        SendInventory(player);
        SendBuyBack(player);
        SendSystemToEntity(player,
            $"Sold {def.Name}{(qty > 1 ? $" x{qty}" : "")} for {total:N0} {GameConstants.CurrencyName}.");
    }

    private void SendBuyBack(Entity player) =>
        SendTo(player, "BuyBack", new BuyBackUpdate(
            player.BuyBack.Select((e, i) => new BuyBackEntryDto(
                i, e.DefId, ItemCatalog.Get(e.DefId)?.Name ?? e.DefId, e.Quantity, e.Enchant, e.UnitPrice))
                .ToArray()));

    /// <summary>Re-buy a recently-sold item by its list index, for the same gold it was sold for.</summary>
    private void HandleBuyBack(BuyBackCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        if (!TryGetVendorNpc(player, cmd.NpcEntityId, out _)) return;
        if (cmd.Index < 0 || cmd.Index >= player.BuyBack.Count) return;

        var entry = player.BuyBack[cmd.Index];
        if (ItemCatalog.Get(entry.DefId) is not ItemDef def)
        {
            player.BuyBack.RemoveAt(cmd.Index);
            SendBuyBack(player);
            return;
        }

        long cost = entry.UnitPrice * entry.Quantity;
        if (player.Gold < cost)
        {
            SendSystemToEntity(player, $"You need {cost:N0} {GameConstants.CurrencyName} to buy that back.");
            return;
        }
        if (player.Inventory.Count(i => !i.Equipped) >= GameConstants.InventorySize)
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }

        player.Gold -= cost;
        player.Inventory.Add(new InventoryItem
        {
            DefId = entry.DefId, Quantity = entry.Quantity, Enchant = entry.Enchant,
            Attributes = new List<ItemAttribute>(entry.Attributes),
        });
        player.BuyBack.RemoveAt(cmd.Index);

        SendGold(player);
        SendInventory(player);
        SendBuyBack(player);
        SendSystemToEntity(player,
            $"Bought back {def.Name}{(entry.Quantity > 1 ? $" x{entry.Quantity}" : "")} for {cost:N0} {GameConstants.CurrencyName}.");
        SaveEntity(player);
    }

    /// <summary>The skills a reset NPC can un-learn: the ones you committed to permanently (any
    /// skill with an ExclusiveGroup — today, the level-40 stat swaps). Includes the gold you sank
    /// into it, so the player can see exactly what he's writing off.</summary>
    private static IEnumerable<ResettableSkill> ResettableSkillsOf(Entity player)
    {
        // A swap rung is priced by its POSITION in the character's nine, not by which pair it belongs
        // to, so "what did this skill cost" has no per-skill answer any more. What IS exact, and is the
        // number that matters when you are deciding whether to forget it, is what forgetting it writes
        // off: its rungs are the TOPMOST positions you hold, so removing them frees the dearest ones.
        int rungs = SkillCatalog.StatSwapRungsOwned(player.LearnedSkills);

        foreach (var (id, level) in player.LearnedSkills)
        {
            if (SkillCatalog.Get(id) is not SkillDef def || string.IsNullOrEmpty(def.ExclusiveGroup))
                continue;
            long spent = SkillCatalog.StatSwapOf(id) is not null
                ? SkillCatalog.StatSwapPriceRange(rungs - level, rungs)
                : SumGold(def, level);
            yield return new ResettableSkill(id, def.Name, level, (int)spent);
        }

        static int SumGold(SkillDef def, int level)
        {
            int spent = 0;
            for (int l = 1; l <= level; l++) spent += def.GoldCostAt(l);
            return spent;
        }
    }

    /// <summary>Un-learn a permanent, mutually-exclusive skill so its group is free to commit to
    /// again. Removing costs NOTHING — but the gold already spent is NOT refunded. That's the whole
    /// deal: you may change your mind, you may not undo the price of being wrong.</summary>
    /// <summary>Buy a planned set of stat-swap rungs in ONE charge (the Stats tab, BL-03).
    ///
    /// <para>Every rule is the same one <see cref="HandleLearnSkill"/> enforces — the class shelf, the
    /// level gate, +5 per stat, 9 rungs total, the price ladder — because they are asked through the
    /// same shared helpers. What this adds is the ATOMICITY: nothing is charged and nothing is learned
    /// until the whole basket has passed, so a player who plans nine rungs and can only afford seven is
    /// told so and keeps their gold, instead of being left holding seven rungs of a build that only the
    /// Mindwriter can undo — and it undoes a whole pair at a time.</para></summary>
    private void HandleBuyStatSwaps(BuyStatSwapsCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;

        // Fold duplicate lines: the client sends one per pair, but a malformed basket that named the
        // same pair twice would otherwise be validated as two independent runs and undercount the caps.
        var basket = new List<(string SkillId, int Rungs)>();
        foreach (var pick in cmd.Picks ?? Array.Empty<StatSwapPurchaseDto>())
        {
            if (pick is null || pick.Rungs <= 0 || string.IsNullOrEmpty(pick.SkillId)) continue;
            int at = basket.FindIndex(b => b.SkillId == pick.SkillId);
            if (at >= 0) basket[at] = (pick.SkillId, basket[at].Rungs + pick.Rungs);
            else basket.Add((pick.SkillId, pick.Rungs));
        }
        if (basket.Count == 0) return;

        // The class shelf and the level gate, per LINE — StatSwapBasketConflict polices the numeric
        // caps but knows nothing about who may buy what.
        foreach (var (skillId, rungs) in basket)
        {
            if (SkillCatalog.Get(skillId) is not SkillDef def)
                return;
            int target = player.SkillLevelOf(def.Id) + rungs;
            int gate = ClassSkills.LearnLevelOf(def.Id, target, player.Race, player.BaseClass,
                                                player.Archetype, player.Discipline);
            if (gate == 0)
            {
                SendSystemToEntity(player, $"Your class cannot trade {def.Name}.");
                return;
            }
            if (player.Level < gate)
            {
                SendSystemToEntity(player, $"{def.Name} requires level {gate}.");
                return;
            }
        }

        if (SkillCatalog.StatSwapBasketConflict(player.LearnedSkills, basket, out long gold) is { } clash)
        {
            SendSystemToEntity(player, clash);
            return;
        }
        if (gold > 0 && player.Gold < gold)
        {
            SendSystemToEntity(player,
                $"That costs {gold:N0} {GameConstants.CurrencyName} — you have {player.Gold:N0}.");
            return;
        }

        if (gold > 0) player.Gold -= gold;
        int rungsBought = 0;
        foreach (var (skillId, rungs) in basket)
        {
            player.LearnedSkills[skillId] = player.SkillLevelOf(skillId) + rungs;
            rungsBought += rungs;
        }

        // A swap moves CON, so the pools move with it. Clamping after is the same care HandleForgetSkill
        // takes: losing CON can lower Max HP under the current value.
        player.RecomputeDerived();
        player.Hp = Math.Min(player.Hp, player.MaxHp);
        player.Mp = Math.Min(player.Mp, player.MaxMp);

        SendSystemToEntity(player, rungsBought == 1
            ? $"1 stat rung committed for {gold:N0} {GameConstants.CurrencyName}."
            : $"{rungsBought} stat rungs committed for {gold:N0} {GameConstants.CurrencyName}.");
        SendStats(player);
        SendLearned(player);
        SendGold(player);
        SaveEntity(player);
    }

    private void HandleForgetSkill(ForgetSkillCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!_world.Entities.TryGetValue(cmd.NpcEntityId, out var npc)
            || npc.Kind != EntityKind.Npc || npc.NpcRole != NpcRole.SkillReset)
            return;

        float dx = npc.X - player.X, dy = npc.Y - player.Y;
        if (dx * dx + dy * dy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{npc.Name} is too far away.");
            return;
        }

        if (SkillCatalog.Get(cmd.SkillId) is not SkillDef def
            || string.IsNullOrEmpty(def.ExclusiveGroup)
            || !player.HasSkill(def.Id))
        {
            SendSystemToEntity(player, "That skill cannot be reset.");
            return;
        }

        player.LearnedSkills.Remove(def.Id);
        player.RecomputeDerived();
        player.Hp = Math.Min(player.Hp, player.MaxHp);   // losing +CON can lower Max HP
        player.Mp = Math.Min(player.Mp, player.MaxMp);

        SendSystemToEntity(player,
            $"{def.Name} forgotten. You may commit to a different path — the gold is not refunded.");
        SendStats(player);
        SendLearned(player);
        SendDialog(player, npc);   // refresh the list
        SaveEntity(player);
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
        if (home is null)
        {
            SendSystemToEntity(player, "You can't travel there.");
            return;
        }

        // A destination is EITHER another city OR one of this city's own field gates. The gate branch is
        // what makes "go hunting" a named choice instead of a random landing spot in a polygon, and
        // restricting it to the gatekeeper's OWN fields is the owner's rule: a city's gatekeeper knows its
        // own hunting grounds and the roads to the other cities, nothing further.
        float tx, ty;
        string destName;
        var town = WorldMap.SafeZones.FirstOrDefault(z => z.Id == cmd.ZoneId);
        if (town is not null && town.Id != home.Id)
        {
            (tx, ty, destName) = (town.X, town.Y, town.Name);
        }
        else if (RegionMap.GateById(cmd.ZoneId) is (TeleportPoint gate, Region field)
                 && field.CityId == home.Id)
        {
            (tx, ty, destName) = (gate.At.X, gate.At.Y, gate.Name);
        }
        else
        {
            SendSystemToEntity(player, "You can't travel there.");
            return;
        }

        int fee = GameConstants.TeleportFee(player.Level, home.X, home.Y, tx, ty);
        if (player.Gold < fee)
        {
            SendSystemToEntity(player,
                $"Not enough {GameConstants.CurrencyName} (need {fee:N0}).");
            return;
        }

        player.Gold -= fee;

        // Land BESIDE the destination town's own gatekeeper rather than on the town CENTRE (owner,
        // playtest-19 M12): travelling on used to mean landing, then walking across town to the next
        // gatekeeper. The FEE is still measured centre-to-centre — it is what the menu quoted, and the
        // landing spot must not silently change the price. Field gates keep their own arrival point.
        float landX = tx, landY = ty;
        if (town is not null && town.Id != home.Id && WorldMap.GatekeeperIn(town) is NpcDef destGk)
            (landX, landY) = (destGk.X + 150f, destGk.Y + 150f);

        // Small scatter so a party arriving together doesn't stack on one pixel.
        player.X = Math.Clamp(landX + _rng.Next(-150, 150), GameConstants.WorldMinX, GameConstants.ZoneWidth);
        player.Y = Math.Clamp(landY + _rng.Next(-150, 150), GameConstants.WorldMinY, GameConstants.ZoneHeight);
        player.TargetX = null;
        player.TargetY = null;
        _world.Grid.UpdatePosition(player);

        SendGold(player);
        SendSystemToEntity(player,
            $"Teleported to {destName} for {fee:N0} {GameConstants.CurrencyName}.");
        AdvanceActionQuests(player, QuestActions.Teleport);   // the tutorial's "use Pell" beat (63j)
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

    // ----- NPC buffer: three PAID options (owner, 2026-07-15) --------------------------------------
    //
    // Level window 6-75 (unchanged — the full-buff NPC is the solo stopgap to 75). FREE at ≤40; PAID
    // above 40. Prices are tunable consts, sized against mob gold drops at 35-45 (~1h of farming should
    // roughly cover a full buff). All three options gate the same way.
    //
    // TODO (owner, deferred): the buffs are MAX-LEVEL for everyone; they should scale with character
    // level once the buff skills become multi-level (same blocker as level-appropriate buffs). The cost
    // formula already reads a per-buff level, so it scales automatically when that lands.

    private const int BufferMinLvl = 6, BufferMaxLvl = 75, BufferFreeUnderLvl = 40;
    // Halved when the buffer's five bundled blessings were split into singles (0.40.0): the set
    // went from 9 buttons to 19, and per-buff pricing would have doubled the full-set bill on an
    // economy the owner had just signed off. 19 x 1500 x 5 ~= the old 9 x 3000 x 5.
    private const long BuffCostPerLevel = 1_500;   // per buff, per buff-LEVEL, when 41-75
    private const long RestoreCostCap = 10_000;    // per pool (HP, MP); a full restore of both = 20k

    // The NPC buffs are single-level defs today but they are the MAX-STRENGTH set, so we price each as
    // "level 5" (the owner's own example: 10 buffs × lvl 5 × 3k = 150k). Calibrated against mob gold:
    // MobGoldReward = 25 + lvl·8 ≈ 345/mob at lvl 40, dropped on EVERY kill, so ~120-170k gold/hour of
    // farming — a full set (9 buffs × 5 × 3k = 135k) ≈ ~1h of farming, which is the intent. When buffs
    // become multi-level, swap this nominal 5 for the real per-buff level and the cost tracks it.
    private const int BufferBuffNominalLevel = 5;

    /// <summary>Cost to cast one buff for this player (0 if ≤40).</summary>
    private static long SingleBuffCost(Entity player, string skillId)
    {
        if (player.Level <= BufferFreeUnderLvl) return 0;
        int lvl = player.SkillLevelOf(skillId) is var l && l > 0 ? l : BufferBuffNominalLevel;
        return BuffCostPerLevel * lvl;
    }

    /// <summary>Cost to restore missing HP + MP: cap·(1−hp/max) + cap·(1−mp/max). 0 if ≤40 or full.</summary>
    private static long RestoreCost(Entity player)
    {
        if (player.Level <= BufferFreeUnderLvl) return 0;
        float hpMiss = player.MaxHp > 0 ? 1f - (float)player.Hp / player.MaxHp : 0f;
        float mpMiss = player.MaxMp > 0 ? 1f - (float)player.Mp / player.MaxMp : 0f;
        return (long)(RestoreCostCap * hpMiss + RestoreCostCap * mpMiss);
    }

    private void SendBufferDialog(Entity player, Entity npc)
    {
        bool canBuff = player.Level is >= BufferMinLvl and <= BufferMaxLvl;
        string message = player.Level < BufferMinLvl
            ? $"Come back at level {BufferMinLvl} and I'll bless you."
            : player.Level > BufferMaxLvl
                ? "You are well beyond a newbie buffer's help."
                : player.Level <= BufferFreeUnderLvl ? "My blessings are free until level 40." : "";

        long fullCost = canBuff
            ? SkillCatalog.NewbieBuffSet.Sum(id => SingleBuffCost(player, id)) : 0;

        var buffs = canBuff
            ? SkillCatalog.NewbieBuffSet
                .Select(id => new BufferBuff(id, SkillCatalog.Get(id)?.Name ?? id, SingleBuffCost(player, id)))
                .ToArray()
            : Array.Empty<BufferBuff>();

        var info = new BufferInfo(canBuff, message, fullCost, RestoreCost(player), buffs);
        SendTo(player, "Dialog", new NpcDialog(
            npc.Name, npc.NpcRole.ToString(),
            Array.Empty<QuestSummary>(), Array.Empty<QuestSummary>(), Array.Empty<QuestSummary>(),
            Array.Empty<ClassChangeOption>(), null, null, null, info));
    }

    private void HandleBufferAction(BufferActionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        if (!_world.Entities.TryGetValue(cmd.NpcEntityId, out var npc)
            || npc.Kind != EntityKind.Npc || npc.NpcRole != NpcRole.Buffer) return;

        float dx = npc.X - player.X, dy = npc.Y - player.Y;
        if (dx * dx + dy * dy > GameConstants.TalkRange * GameConstants.TalkRange)
        {
            SendSystemToEntity(player, $"{npc.Name} is too far away.");
            return;
        }
        if (player.Level is < BufferMinLvl or > BufferMaxLvl)
        {
            SendSystemToEntity(player, "That buffer can't help you at your level.");
            return;
        }

        bool Charge(long cost)
        {
            if (cost <= 0) return true;
            if (player.Gold < cost)
            {
                SendSystemToEntity(player, $"You need {cost:N0} {GameConstants.CurrencyName} for that.");
                return false;
            }
            player.Gold -= cost;
            SendGold(player);
            SaveEntity(player);
            return true;
        }

        switch (cmd.Action)
        {
            case "full":
                if (!Charge(SkillCatalog.NewbieBuffSet.Sum(id => SingleBuffCost(player, id)))) return;
                GrantFullBuffSet(player);
                SendSystemToEntity(player, "You are blessed with a buffer's full might!");
                break;

            case "single":
                if (SkillCatalog.Get(cmd.SkillId) is not SkillDef def
                    || !SkillCatalog.NewbieBuffSet.Contains(cmd.SkillId))
                {
                    SendSystemToEntity(player, "That buff isn't on offer.");
                    return;
                }
                if (!Charge(SingleBuffCost(player, cmd.SkillId))) return;
                ApplyBuff(player, def, refresh: false);
                player.RecomputeDerived();
                PushBuffs(player);
                SendStats(player);
                SendSystemToEntity(player, $"{def.Name} granted.");
                break;

            case "restore":
                if (player.Hp >= player.MaxHp && player.Mp >= player.MaxMp)
                {
                    SendSystemToEntity(player, "You are already at full health and mana.");
                    return;
                }
                if (!Charge(RestoreCost(player))) return;
                player.Hp = player.MaxHp;
                player.Mp = player.MaxMp;
                SendStats(player);
                SendSystemToEntity(player, "Restored to full health and mana.");
                break;

            default:
                return;
        }

        SendBufferDialog(player, npc);   // refresh (restore cost drops to 0, gold changed)
    }

    /// <summary>Lay a whole buff set on a player. The buffer NPC passes its own set; the debug
    /// button passes the ADMIN set, which is the same list plus Harmony.</summary>
    private void GrantFullBuffSet(Entity player, string[]? set = null)
    {
        foreach (var id in set ?? SkillCatalog.NewbieBuffSet)
            if (SkillCatalog.Get(id) is SkillDef def)
                ApplyBuff(player, def, refresh: false);
        // One refresh after all buffs (instead of per-buff recompute/push).
        player.RecomputeDerived();
        PushBuffs(player);
        SendStats(player);
    }

    /// <summary>DEBUG: the full buff set, at any level, without walking to the NPC. Deliberately has
    /// NO level gate — the NPC's 6-75 window is a game rule, and debug exists to sidestep the walk,
    /// not to re-enforce it. This is the ONLY way to get buffed above 75 today, which matters because
    /// the balance numbers the owner signs off on are BUFFED numbers — and the only way to see
    /// Harmony at all, which is why this uses the ADMIN set (owner 2026-07-31).</summary>
    private void HandleDebugBuff(DebugBuffCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        GrantFullBuffSet(player, SkillCatalog.AdminBuffSet);
        // Silent (owner): the buff bar filling up is the feedback.
    }

    /// <summary>DEBUG: adjust karma by a delta. Clamped to [0, 1M]; clearing to 0 resets the PK streak
    /// and the red name (same as ReduceKarma's clear path).</summary>
    private void HandleDebugKarma(DebugKarmaCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        bool wasRed = player.Karma > 0;
        player.Karma = (int)Math.Clamp((long)player.Karma + cmd.Delta, 0, 1_000_000);
        if (player.Karma == 0 && wasRed)
        {
            player.ConsecutivePk = 0;
            BroadcastSystem($"{player.Name}'s karma has cleared.");
        }
        SendPvpState(player);
        SendSystemToEntity(player, $"[DEBUG] Karma {(cmd.Delta >= 0 ? "+" : "")}{cmd.Delta} → {player.Karma:N0}.");
        SaveEntity(player);
    }

    private void SendDialog(Entity player, Entity npc)
    {
        string npcId = npc.NpcId ?? "";

        // Buffer: open a dialog with the three paid options (full-buff / single buff / restore).
        // ⚠ This branch returns BEFORE the AdvanceTalkStep at the bottom of the method, so it must
        // advance the talk step itself — the tutorial chain's "visit Nyra" beat is a plain TalkTo at
        // a Buffer, and without this it could never be completed by anyone (playtest, 0.57.0).
        if (npc.NpcRole == NpcRole.Buffer)
        {
            SendBufferDialog(player, npc);
            AdvanceTalkStep(player, npcId);
            return;
        }

        var offered = OfferedQuests(player, npcId).Select(q => Summarize(player, q, null)).ToArray();

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
            if (step.Type == QuestStepType.TalkTo && def.StepTargetMatches(step.TargetId, npcId))
                turnable.Add(summary);
            // C5: otherwise it is only THIS NPC's business if this NPC is the one who gave it. The
            // list used to be every active quest you were carrying, at every NPC in the world — the
            // owner's "today every NPC shows three". The quest LOG is where you read your own quests;
            // an NPC window answers "what can I do with YOU".
            else if (def.GivenBy(npcId))
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
                // "What this class does" blurb so the (irreversible) choice is informed:
                // 2nd class = its archetype blurb; 3rd AND 4th = the discipline blurb (a 4th class
                // is the same discipline awakened, so it describes itself with the same line).
                // ⚠ Each tier reads its OWN catalog — the ids share one space but not one table, so
                // the old `Tier >= 3` shape would have looked a 4th class up among the 3rd's and
                // silently produced an empty blurb.
                string blurb = req.Tier switch
                {
                    4 => FourthClassCatalog.Get(req.SecondClassId) is FourthClassDef fcd
                        ? Disciplines.Blurb(fcd.Discipline) : "",
                    3 => ThirdClassCatalog.Get(req.SecondClassId) is ThirdClassDef tcd
                        ? Disciplines.Blurb(tcd.Discipline) : "",
                    _ => ClassCatalog.Get(req.SecondClassId) is SecondClassDef scd
                        ? ClassCatalog.ArchetypeBlurb(scd.Archetype) : "",
                };
                changes.Add(new ClassChangeOption(req.SecondClassId, req.ClassName, meets, names, has, blurb));
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
            SendBuyBack(player);   // the vendor also shows what you recently sold, to re-buy
        }

        // Gatekeeper destinations: THIS city's own field gates first, then every other town.
        //
        // Owner: "each gatekeeper teleports you to their own fields + the other cities", and each gate is a
        // NAMED point, not a random spot in a polygon. Listing the local fields first is the ordering that
        // matches why you walked over — you are far more often going hunting than emigrating.
        TeleportInfo? teleport = null;
        if (npc.NpcRole == NpcRole.Teleporter
            && WorldMap.SafeZoneAt(npc.X, npc.Y) is SafeZone home)
        {
            var dests = new List<TeleportDest>();

            foreach (var field in RegionMap.FieldsOf(home.Id))
            {
                var band = RegionMap.LevelBand(field.Id);
                foreach (var gate in field.Gates)
                    dests.Add(new TeleportDest(
                        gate.Id, gate.Name,
                        GameConstants.TeleportFee(player.Level, home.X, home.Y, gate.At.X, gate.At.Y),
                        band?.Min ?? 0, band?.Max ?? 0, gate.Description, field.Name));
            }

            dests.AddRange(WorldMap.TeleportDestinationsFrom(npcId, home)
                .Select(z =>
                {
                    var band = WorldMap.LevelRangeNear(z);
                    return new TeleportDest(z.Id, z.Name, GameConstants.TeleportFee(player.Level, home, z),
                        band?.Min ?? 0, band?.Max ?? 0, "City", "");
                })
                // Order by hunting-ground level so the "next" city is at the top.
                .OrderBy(d => d.MinLevel == 0 ? int.MaxValue : d.MinLevel)
                .ThenBy(d => d.Name));

            teleport = new TeleportInfo(dests.ToArray());
        }

        // Skill reset (only for reset NPCs): the permanent, mutually-exclusive picks you've made.
        SkillResetInfo? reset = null;
        if (npc.NpcRole == NpcRole.SkillReset)
            reset = new SkillResetInfo(ResettableSkillsOf(player).OrderBy(s => s.Name).ToArray());

        // Crafting master (`BL-05`): his three buttons, or none of them if his joining quest is simply
        // on offer above like anyone else's.
        CraftMasterInfo? craft = null;
        if (npc.NpcRole == NpcRole.CraftMaster
            && WorldMap.CraftMasterProfession(npcId) is var taught && taught != Profession.None)
        {
            bool isMine = player.Profession == taught;
            bool doneHisQuest = QuestCatalog.JoiningQuestFor(taught) is string jq
                                && player.CompletedQuests.Contains(jq);
            craft = new CraftMasterInfo(
                (int)taught,
                CanOpenWorkshop: isMine,
                // Re-join is only offered when you are free to take it: holding another profession, you
                // must quit at ITS master first, which is his ruling that the levels are lost every time
                // rather than traded away at whichever counter you happen to be standing at.
                CanRejoin: !isMine && doneHisQuest
                           && player.Profession == Profession.None
                           && player.Level >= QuestCatalog.ProfessionJoinLevel,
                CanQuit: isMine,
                CurrentLevel: isMine ? player.CraftLevel : 0);
            // Standing here IS the workshop, and the window may already be open from the menu.
            if (isMine && !player.AtCraftMaster)
            {
                player.AtCraftMaster = true;
                SendCrafting(player);
            }
        }

        SendTo(player, "Dialog", new NpcDialog(
            npc.Name, npc.NpcRole.ToString(),
            offered, turnable.ToArray(), inProgress.ToArray(), changes.ToArray(), shop, teleport, reset,
            Warehouse: npc.NpcRole == NpcRole.Warehouse,
            CraftMaster: craft));

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
        int needed = step.Type is QuestStepType.KillMobs or QuestStepType.CollectItem
                   ? Math.Max(1, step.Count) : 1;
        // 🔑 A COLLECT step reads the BAG, never a stored tally: mats leave a bag (sold, salvaged, spent
        // on a craft) as easily as they arrive, so a counter incremented on pickup would drift the first
        // time one is spent and then lie about a step the player can no longer satisfy.
        if (step.Type == QuestStepType.CollectItem && state is not null)
            counter = Math.Min(needed, CountItem(player, step.TargetId ?? ""));
        // Ready to turn in = on the final step and that step is a TalkTo.
        bool canComplete = state is not null
            && stepIndex == def.Steps.Length - 1
            && def.Steps[^1].Type == QuestStepType.TalkTo;
        return new QuestSummary(def.Id, def.Name, def.Description, GatherText(player, def, state, step.Text),
            stepIndex, def.Steps.Length, counter, needed,
            state?.Completed ?? false, canComplete, StepLocation(def, step), state?.Tracked ?? false);
    }

    /// <summary>The step line for a GATHERING contract, which has no step of its own worth reading —
    /// its only step is "come back when you're done", and what the player actually wants to know is
    /// what to hunt and how much they are carrying.
    ///
    /// Deliberately folded into the existing step TEXT rather than sent as new fields: this is the whole
    /// feature made visible on a client that has not been rebuilt. The 3-tab quest window will want it
    /// structured; it can have it then, with one protocol bump instead of two.</summary>
    private static string GatherText(Entity player, QuestDef def, CharacterQuestState? state, string stepText)
    {
        var lines = def.GatherLines;
        if (lines.Length == 0) return stepText;

        var parts = lines.Select(g =>
        {
            string item = ItemCatalog.Get(g.ItemId)?.Name ?? g.ItemId;
            // Not yet taken: name what drops it, so "is this contract worth my level" is answerable
            // before accepting. Taken: the running count is the only thing that matters.
            return state is null
                ? $"{item} ({MobCatalog.Get(g.MobId).Name})"
                : $"{item} {CountItem(player, g.ItemId)}";
        });
        return $"{stepText}\n{(state is null ? "Collects" : "Gathered")}: {string.Join(", ", parts)}";
    }

    /// <summary>A "who/where" hint for a quest step: the NPC + town to talk to, or
    /// the mob + nearest hunting ground (with its level band). "" when not useful.</summary>
    private static string StepLocation(QuestDef def, QuestStep step) => step.Type switch
    {
        // An any-town errand must NOT name one town's NPC — that is the trip the player was trying to
        // avoid (M11). Name the service instead, since every town has one.
        QuestStepType.TalkTo when def.AnyTownNpc && WorldMap.NpcById(step.TargetId) is NpcDef any =>
            $"{ServiceNoun(any.Name)} — any town",
        QuestStepType.TalkTo when WorldMap.NpcById(step.TargetId) is NpcDef npc =>
            $"{npc.Name} — {WorldMap.NearestSafeZone(npc.X, npc.Y).Name}",
        QuestStepType.KillMobs => MobLocationHint(step),
        _ => ""
    };

    /// <summary>"Apothecary Miren" → "Apothecary". Service NPCs are named "&lt;Title&gt; &lt;Name&gt;",
    /// and for an any-town quest the TITLE is the useful half — every town has one of those.</summary>
    private static string ServiceNoun(string npcName)
    {
        int space = npcName.IndexOf(' ');
        return space > 0 ? npcName.Substring(0, space) : npcName;
    }

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
            case "accept": AcceptQuest(player, cmd.Id, cmd.NpcEntityId); break;
            case "abandon": AbandonQuest(player, cmd.Id); break;
            case "track": ToggleQuestTracked(player, cmd.Id); break;
            case "complete": CompleteQuestAtNpc(player, cmd.Id, cmd.NpcEntityId); break;
            case "changeclass": DoQuestClassChange(player, cmd.Id, cmd.NpcEntityId); break;
        }
    }

    private void AcceptQuest(Entity player, string questId, Guid npcEntityId)
    {
        var def = QuestCatalog.Get(questId);
        if (def is null) return;
        if (player.ActiveQuests.ContainsKey(questId)) return;
        // A DAILY re-opens once the server day rolls over, a REPEATABLE immediately; anything else is
        // one-shot. Same rule as the offer list — see QuestClosed. The daily gets its own line first,
        // because "not today" is worth saying and "already done, forever" is not.
        if (def.Daily && !DailyQuestReady(player, questId))
        {
            SendSystemToEntity(player, $"{def.Name} can be taken again tomorrow.");
            return;
        }
        if (QuestClosed(player, questId)) return;
        // LEVEL RANGE (owner): too low OR too high blocks the ACCEPT. Class quests carry no ceiling.
        if (!def.LevelInRange(player.Level))
        {
            SendSystemToEntity(player, player.Level < def.MinLevel
                ? $"{def.Name} requires level {def.MinLevel}."
                : $"You have outgrown {def.Name} (levels {def.MinLevel}-{def.MaxLevel}).");
            return;
        }
        if (def.RequiresQuestId is not null && !player.CompletedQuests.Contains(def.RequiresQuestId)) return;

        // Taking a quest PINS it (playtest-18 Q2): you accepted it because you mean to do it now, and
        // the alternative is a tracker that is empty exactly when it would be most useful. It YIELDS at
        // the cap instead of evicting — an automatic pin has no business pushing off one you chose.
        bool autoTrack = player.ActiveQuests.Values.Count(s => s.Tracked) < GameConstants.MaxTrackedQuests;
        player.ActiveQuests[questId] = new CharacterQuestState(questId, 0, 0, false, autoTrack);
        SendSystemToEntity(player, $"Quest accepted: {def.Name}");
        // A quest can OPEN on a "reach level N" step the player already satisfies (they took it late),
        // and nothing else would ever re-check it — level-ups are the only other trigger. Likewise a
        // collect step you are already carrying the items for.
        AdvanceLevelQuests(player);
        AdvanceCollectQuests(player);
        SendQuestLog(player);
        // Re-send the OPEN dialog so it shows the objective straight away. Without this the panel kept
        // rendering the pre-accept text — the quest still offered, no word on what to kill — and the
        // player had to close the NPC and talk to it again to be told (playtest-13). Completing a quest
        // already refreshed the dialog this way; accepting simply never passed the NPC through.
        if (npcEntityId != Guid.Empty
            && _world.Entities.TryGetValue(npcEntityId, out var npc) && npc.Kind == EntityKind.Npc)
            SendDialog(player, npc);
        SaveEntity(player);
    }

    /// <summary>Give a quest up. All progress on it is lost, and if the character has since climbed
    /// past the quest's level ceiling they cannot take it again — which is exactly why the client asks
    /// for confirmation first (owner, playtest-13). A COMPLETED quest is not abandonable; there is
    /// nothing to give up.</summary>
    private void AbandonQuest(Entity player, string questId)
    {
        if (!player.ActiveQuests.Remove(questId)) return;
        var def = QuestCatalog.Get(questId);
        string name = def?.Name ?? questId;
        SendSystemToEntity(player, def is not null && !def.LevelInRange(player.Level)
            ? $"Abandoned: {name}. You are outside its level range and cannot take it again."
            : $"Abandoned: {name}.");
        // Giving up a GATHERING contract destroys what you gathered for it. It has to: a quest item
        // cannot be discarded (that rule protects the class-change proofs), so tokens left behind would
        // be dead weight in the bag with no way out — and a token is worthless without the contract
        // anyway. Safe to do bluntly because a gather token belongs to exactly one quest, which
        // QuestCatalog.Register enforces at startup.
        if (def is not null)
        {
            bool tookAny = false;
            foreach (var g in def.GatherLines)
            {
                int held = CountItem(player, g.ItemId);
                if (held <= 0) continue;
                ConsumeItem(player, g.ItemId, held);
                tookAny = true;
            }
            if (tookAny) { SendInventory(player); SendSystemToEntity(player, "Your gathered trophies are discarded."); }
        }
        SendQuestLog(player);
        SaveEntity(player);
    }

    /// <summary>Pin / unpin a quest on the on-screen tracker.
    ///
    /// The pin is CHARACTER state and the server owns it (playtest-18 Q1: *"Quest tracking is not
    /// persistant"*). It was a client-side <c>List&lt;string&gt;</c> that nothing ever wrote anywhere —
    /// not to the server, not even to PlayerPrefs — so it died with the app and belonged to the INSTALL
    /// rather than to the character. It now rides <see cref="CharacterQuestState"/>, which is already
    /// persisted, so it survives a relog and follows the character to another phone.
    ///
    /// Pinning past the cap drops a pin rather than refusing: the player asked for THIS one, and a
    /// button that silently does nothing reads as broken.</summary>
    private void ToggleQuestTracked(Entity player, string questId)
    {
        if (!player.ActiveQuests.TryGetValue(questId, out var state)) return;

        if (!state.Tracked)
        {
            // Oldest-first is only as good as the dictionary's order, which is insertion order in
            // practice — good enough to decide which of five pins yields, and always leaves room.
            var pins = player.ActiveQuests.Where(kv => kv.Value.Tracked && kv.Key != questId)
                             .Select(kv => kv.Key).ToList();
            for (int i = 0; pins.Count - i >= GameConstants.MaxTrackedQuests; i++)
                player.ActiveQuests[pins[i]] = player.ActiveQuests[pins[i]] with { Tracked = false };
        }

        player.ActiveQuests[questId] = state with { Tracked = !state.Tracked };
        SendQuestLog(player);
        SaveEntity(player);
    }

    /// <summary>Has the server day rolled over since this daily was last completed? The stamp is kept
    /// in the character's completed-quest set as "<id>@<yyyy-MM-dd>", so it needs no new column and a
    /// day's worth of dailies costs one string each.</summary>
    private static bool DailyQuestReady(Entity player, string questId)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return !player.CompletedQuests.Contains(DailyStamp(questId, today));
    }

    /// <summary>Is this quest CLOSED to the player — done, and not coming back? The one answer both the
    /// NPC's offer list and the "!" markers ask, so they can never disagree.
    ///
    /// Three shapes, in priority order:
    ///   • DAILY     — closed only for TODAY (it records a dated stamp, not its bare id).
    ///   • REPEATABLE— never closed. The id IS still in CompletedQuests, so "have you ever done this"
    ///                 and RequiresQuestId chains keep working; this is what makes the NPC hand it back.
    ///   • anything else — closed once completed.
    /// Daily is checked FIRST, which is what gives the owner's "can be taken again — if not daily
    /// limited": a quest marked both is offered once a day, not endlessly.</summary>
    private static bool QuestClosed(Entity player, string questId) => QuestCatalog.Get(questId) switch
    {
        { Daily: true } => !DailyQuestReady(player, questId),
        { Repeatable: true } => false,
        _ => player.CompletedQuests.Contains(questId),
    };

    private static string DailyStamp(string questId, string day) => $"{questId}@{day}";

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
            if (step.Type == QuestStepType.TalkTo && def.StepTargetMatches(step.TargetId, npcId))
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
        // Same reason as in AdvanceKillQuests: a talk step can hand over to a "reach level N" step the
        // player already satisfies, and only a level-up would otherwise re-check it. A COLLECT step is
        // the same case — the master finishes his pitch and you were already carrying the twenty ingots.
        if (changed) { AdvanceLevelQuests(player); AdvanceCollectQuests(player); SendQuestLog(player); SaveEntity(player); }
    }

    private void CompleteQuestAtNpc(Entity player, string questId, Guid npcEntityId)
    {
        var def = QuestCatalog.Get(questId);
        if (def is null || !player.ActiveQuests.TryGetValue(questId, out var state)) return;

        // Must be on the final TalkTo step at the right NPC.
        if (state.StepIndex != def.Steps.Length - 1) return;
        var finalStep = def.Steps[^1];
        if (finalStep.Type != QuestStepType.TalkTo) return;
        if (!_world.Entities.TryGetValue(npcEntityId, out var npc)
            || !def.StepTargetMatches(finalStep.TargetId, npc.NpcId ?? "")) return;

        // THE COLLECT STEPS ARE PAID HERE — a CollectItem step only ever checked that the bag HELD the
        // mats (see AdvanceCollectQuests), and the master takes them when he hires you. Re-checked first
        // because the hold can be broken between the field and the counter: sell, salvage or craft with
        // the twenty ingots and you go back to that step instead of getting the profession for nothing.
        for (int i = 0; i < def.Steps.Length; i++)
        {
            var s = def.Steps[i];
            if (s.Type != QuestStepType.CollectItem || s.TargetId is null) continue;
            int need = Math.Max(1, s.Count);
            if (CountItem(player, s.TargetId) >= need) continue;
            SendSystemToEntity(player,
                $"{def.Name}: bring {need}x {ItemCatalog.Get(s.TargetId)?.Name ?? s.TargetId}.");
            player.ActiveQuests[questId] = state with { StepIndex = i, Counter = 0 };
            SendQuestLog(player);
            SendDialog(player, npc);
            return;
        }
        foreach (var s in def.Steps)
            if (s.Type == QuestStepType.CollectItem && s.TargetId is not null)
                ConsumeItem(player, s.TargetId, Math.Max(1, s.Count));

        // Grant rewards. The gathered tokens are cashed FIRST so their exp joins the quest's own in one
        // levelling pass rather than two.
        var (gatherExp, gatherGold) = CashInGatheredTokens(player, def);

        // QUEST rates ride on top of the world's (RateConfig.Quest, One by default). Exp and SP go
        // through AwardExp so they take World x the player's runes there and land in ONE levelling pass.
        //
        // 🔑 Gold and SP used to be added RAW here, which meant a x30 server paid quests at x1 — the same
        // effort was worth a thirtieth in a quest as in the field, and no knob could fix it because the
        // rates never reached this code at all.
        var qr = RateConfig.Quest;
        long exp = (long)((def.Reward.Exp + gatherExp) * qr.Exp);
        long sp  = (long)(def.Reward.SkillPoints * qr.Sp);
        if (exp > 0 || sp > 0)
        {
            // ⚠ A quest that authors SP gets it ON TOP of the SP its exp derives — that is what the two
            // separate statements did before, and passing only the authored figure would silently drop
            // the derived half. -1 keeps the plain "derive it from exp" path for the common case.
            AwardExp(player, exp,
                sp > 0 ? (long)(exp * GameConstants.SkillPointRatio) + sp : -1);
            if (sp > 0)
                SendLearned(player);   // the Learn tab prices skills against SP, so refresh it
        }
        long gold = (long)((def.Reward.Gold + gatherGold) * qr.Gold * RateConfig.World.Gold
                           * player.Runes.Gold);
        if (gold > 0)
        {
            player.Gold += gold;
            SendGold(player);
        }
        if (def.Reward.ItemIds is { Length: > 0 })
            foreach (var itemId in def.Reward.ItemIds)
                AddItem(player, itemId);

        player.ActiveQuests.Remove(questId);
        if (def.Daily)
        {
            // A daily records TODAY's stamp rather than closing permanently, so it re-opens when the
            // server day rolls over. The plain id is deliberately NOT added — that would retire it.
            player.CompletedQuests.Add(DailyStamp(questId, DateTime.UtcNow.ToString("yyyy-MM-dd")));
        }
        // A REPEATABLE still records its bare id: it is genuinely completed, prerequisites that name it
        // are satisfied, and the "completed" list stays a true history. What makes it repeatable is that
        // the offer filter ignores that record (QuestClosed) — not a missing entry.
        else player.CompletedQuests.Add(questId);

        // A MASTER'S JOINING QUEST grants his profession the moment it completes (`BL-05`) — *"u
        // compleate the quest and u can take his proffesion"*. Done here rather than in a separate
        // "accept apprenticeship" step because the quest IS the acceptance; a second confirmation
        // after a three-beat chain would be a dialog asking whether you meant the thing you just did.
        if (QuestCatalog.ProfessionGrantedBy(questId) is var granted && granted != Profession.None)
            GrantProfession(player, granted);

        SendSystemToEntity(player, $"Quest complete: {def.Name}!");
        SendInventory(player);
        SendQuestLog(player);
        SendDialog(player, npc);
        SaveEntity(player);
    }

    /// <summary>Is this class change offered to this player right now? Encodes the
    /// tier gating: Tier 2 needs no second class yet + matching race/base; Tier 3
    /// needs the right parent 2nd class + no third class yet.
    ///
    /// It ALSO enforces class uniqueness across your subclasses — you may not walk the same DISCIPLINE
    /// twice (see Entity; archetypes are NOT restricted). This one method both LISTS the offered classes
    /// at the NPC and GATES the change itself, so a barred class is never even shown, and can't be taken
    /// if it somehow is.</summary>
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
                && tcd.Race == player.Race
                && player.CanTakeThirdClass(tcd.Id);
        }
        if (req.Tier == 4)
        {
            // You must hold the EXACT parent 3rd class and not already be a 4th. No
            // CanTakeFourthClass sibling is needed: a 4th class is the ascension of the discipline
            // you already walk, so the "never the same discipline twice across subclasses" rule was
            // already enforced when the 3rd class was taken — there is no second choice to police.
            if (player.ThirdClass == 0 || player.FourthClass != 0) return false;
            if (req.RequiredCurrentClass != player.ThirdClass) return false;
            return FourthClassCatalog.Get(req.SecondClassId) is FourthClassDef fcd
                && fcd.Race == player.Race;
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
            if (CountItem(player, itemId) < 1)
            {
                SendSystemToEntity(player, "You don't have the required items.");
                return;
            }

        // Consume ONE of each, by quantity — quest items stack now (the gathering contracts need it),
        // so removing the ROW would take the whole stack.
        foreach (var itemId in req.RequiredItemIds)
            ConsumeItem(player, itemId, 1);

        if (req.Tier == 4) player.FourthClass = classId;
        else if (req.Tier == 3) player.ThirdClass = classId;
        else player.SecondClass = classId;

        AutoLearnCoreSkills(player);
        player.RecomputeDerived();
        player.Hp = player.MaxHp;
        player.Mp = player.MaxMp;

        SendInventory(player);
        SendStats(player);
        SendLearned(player);
        // THE class-change push, and the one this handler was missing (playtest-15: "my class doesn't
        // update, I need to relog, and then the skills window is slow to show my unlearned list").
        // The client's ActiveClass — the label it shows AND what the Skills window gates the Learn tab
        // on — is set ONLY by this message. Stats carries SecondClass but nothing reads it for that,
        // so the class silently stayed the old one until the next login re-sent the list. The debug
        // class-change, the subclass swap and the character reset all send it; the REAL, quest-gated
        // class change was the one path that did not.
        SendSubclasses(player);
        // The 4th class lifts the crafting band cap (Crafting.RequireFourthClassForL5), and that
        // panel is not otherwise re-pushed until the next master visit.
        SendCrafting(player);
        // A new class changes which quests are offered, and with them the "!" markers over NPC heads —
        // the same reason the subclass swap re-sends this.
        SendQuestLog(player);
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
        // Finishing a kill step can make a "reach level N" step CURRENT, and level-ups are the only
        // other thing that checks those — so a player already past the level would sit there forever.
        if (changed) { AdvanceLevelQuests(player); SendQuestLog(player); }

        // Gathering contracts run BESIDE the step machine, not through it: their tokens accrue whatever
        // step the quest is on, and the quest's own step is only ever "come back when you're done".
        // Log without the marker sweep — see SendQuestLog.
        if (RollGatherTokens(player, mob) && !changed) SendQuestLog(player, withMarks: false);
    }

    /// <summary>Mob death, gathering half: drop this creature's token for every active contract that
    /// asks for it (owner, playtest-13: *"gathering quest items as u farm in a specific zone"*).
    ///
    /// Called from <see cref="AdvanceKillQuests"/>, so it inherits kill CREDIT — the top damager and
    /// every party member in range, exactly who the kill counted for. Returns true if anything dropped,
    /// so the caller re-sends the log and the counts on screen move as you farm.</summary>
    private bool RollGatherTokens(Entity player, Entity mob)
    {
        bool any = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            if (QuestCatalog.Get(qid) is not QuestDef def) continue;
            foreach (var g in def.GatherLines)
            {
                if (!string.Equals(mob.MobTypeId, g.MobId, StringComparison.OrdinalIgnoreCase)) continue;
                if (g.DropChance < 1f && _rng.NextDouble() > g.DropChance) continue;
                // A full bag silently eats the token otherwise, and the player has no way to tell a
                // failed roll from a failed ADD — so say so, once, and don't count it.
                if (!AddItem(player, g.ItemId))
                {
                    SendSystemToEntity(player, "Your inventory is full — the trophy was left behind.");
                    continue;
                }
                any = true;
                // A gather token IS a drop, so it belongs in the combat log beside every other drop —
                // not in SYSTEM. It used to announce "<contract>: Bear Pelt 93" there, once per kill,
                // which drowned the system channel in a channel the player reads for warnings
                // (playtest-22: *"its a drop item ... now its system chat flood"*). The running count
                // went with it: the quest window is already refreshed on the same kill and is where a
                // contract's progress is meant to be read.
                SendCombatToEntity(player, "LOOT",
                    $"You looted: {ItemCatalog.Get(g.ItemId)?.Name ?? g.ItemId} [Q]");
            }
        }
        if (any) SendInventory(player);
        return any;
    }

    /// <summary>Turn-in, gathering half: count the contract's tokens, consume them, and return what
    /// they are worth.
    ///
    /// The owner's formula, verbatim — *"20 * QuestItemRewardModifier(sceletons) * Exp + 20 *
    /// QuestItemRewardModifier(sceletons) * Gold + 55 * ..."*. "Exp" here is the CREATURE's own kill
    /// value, so a token is always worth a fraction of the thing that dropped it and nothing in a
    /// contract has to be re-tuned when the curves move. The mob's NATURAL level is the right one to
    /// read: every gather creature has one, and a zone only overrides levels where it says so.
    ///
    /// Two multipliers a real kill applies are deliberately NOT applied here: the mob's toughness
    /// (a template has none — it belongs to a live entity) and the level-gap penalty (the token pays
    /// for the creature, not for who cashed it). Both make the token pay slightly LESS than its share
    /// of the kill, which is the safe direction, and the gap in particular cannot be farmed: a
    /// creature far below you pays a fraction of a level-4 fox.
    ///
    /// Exp is returned rather than awarded here so the caller banks it in one pass with the quest's own
    /// reward; gold gets the same server rate a dropped coin does.</summary>
    private (long Exp, long Gold) CashInGatheredTokens(Entity player, QuestDef def)
    {
        long exp = 0, gold = 0;
        foreach (var g in def.GatherLines)
        {
            int held = CountItem(player, g.ItemId);
            if (held <= 0) continue;
            int mobLevel = Math.Max(1, MobCatalog.Get(g.MobId).Level);
            // RAW, both of them: the quest-reward site applies World x Quest x the player's runes to
            // exp and gold together. Scaling gold here (as this line used to) and not exp meant the two
            // halves of one hand-in obeyed different rates.
            exp  += (long)(held * g.RewardModifier * StatCalculator.MobExpReward(mobLevel));
            gold += (long)(held * g.RewardModifier * StatCalculator.MobGoldReward(mobLevel));
            ConsumeItem(player, g.ItemId, held);
            SendSystemToEntity(player,
                $"Handed over {held}x {ItemCatalog.Get(g.ItemId)?.Name ?? g.ItemId}.");
        }
        return (exp, gold);
    }

    /// <summary>Advance any active quest sitting on a <see cref="QuestStepType.ReachLevel"/> step whose
    /// level the player has now met.
    ///
    /// The step type has existed in the enum since quests were written but was NEVER handled anywhere —
    /// no quest used it, so nothing noticed. A quest that reached such a step would simply stall
    /// forever. Called on every level-up AND when a quest is accepted, because a player may already be
    /// past the required level when they take the quest (or when the step becomes current).</summary>
    /// <summary>Credit one <see cref="QuestStepType.DoAction"/> step — the tutorial's "now actually do
    /// it" beats (owner, playtest-20 `58a`). Called from the handlers that already process the action,
    /// so there is no new client message and no way to claim an action you did not perform.
    ///
    /// <para>Counts up like a kill step rather than completing on the first one, so "use a skill three
    /// times" is expressible. Silent when nothing is waiting on that action, which is almost always.</para></summary>
    private void AdvanceActionQuests(Entity player, string action)
    {
        if (player.Kind != EntityKind.Player || player.ActiveQuests.Count == 0) return;

        bool changed = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            var def = QuestCatalog.Get(qid);
            var state = player.ActiveQuests[qid];
            if (def is null || state.Completed) continue;

            var step = def.Steps[state.StepIndex];
            if (step.Type != QuestStepType.DoAction || step.TargetId != action) continue;

            int counter = state.Counter + 1;
            int need = Math.Max(1, step.Count);
            if (counter < need)
            {
                player.ActiveQuests[qid] = state with { Counter = counter };
                SendSystemToEntity(player, $"{def.Name}: {step.Text}  ({counter}/{need})");
            }
            else if (state.StepIndex < def.Steps.Length - 1)
            {
                player.ActiveQuests[qid] = state with { StepIndex = state.StepIndex + 1, Counter = 0 };
                SendSystemToEntity(player, $"{def.Name}: {def.Steps[state.StepIndex + 1].Text}");
            }
            else
            {
                // Last step: park the counter at its target, the shape a finished kill step ends in.
                player.ActiveQuests[qid] = state with { Counter = need };
            }
            changed = true;
        }
        if (changed) SendQuestLog(player);
    }

    /// <summary>Credit any active quest sitting on a <see cref="QuestStepType.CollectItem"/> step whose
    /// item the player now holds enough of.
    ///
    /// <para>Playtest-23: the five profession joining quests each ask for 20 common mats and sat at 0/20
    /// forever — farming them moved nothing, and neither did being handed 200 with <c>/give</c>. The step
    /// type was declared in the enum, rendered by the quest window and persisted, but NOTHING anywhere
    /// advanced it: exactly the hole <see cref="QuestStepType.ReachLevel"/> sat in until a quest finally
    /// used it. Every crafting profession in the game was unreachable.</para>
    ///
    /// <para>Hung off <see cref="SendInventory"/> — the one funnel every item gain and loss already
    /// pushes through — for the same reason <see cref="SupplyStepItems"/> hangs off
    /// <see cref="SendQuestLog"/>: a future source of items (a drop, a craft, a trade, a warehouse
    /// withdrawal, an admin grant) cannot forget to credit a collect step, because it cannot forget to
    /// show the player their own bag.</para>
    ///
    /// <para>🔑 Nothing is consumed here. A collect step is a HOLD requirement and the mats are handed
    /// over at the master (<see cref="CompleteQuestAtNpc"/>): items must not evaporate out in a field the
    /// instant the 20th one drops, and the turn-in re-checks the count anyway, so nobody is hired for
    /// free by selling the pile on the way in.</para></summary>
    private void AdvanceCollectQuests(Entity player)
    {
        if (player.Kind != EntityKind.Player || player.ActiveQuests.Count == 0) return;

        bool changed = false, onCollectStep = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            var def = QuestCatalog.Get(qid);
            var state = player.ActiveQuests[qid];
            if (def is null || state.Completed) continue;

            var step = def.Steps[state.StepIndex];
            if (step.Type != QuestStepType.CollectItem || step.TargetId is null) continue;
            onCollectStep = true;
            if (CountItem(player, step.TargetId) < Math.Max(1, step.Count)) continue;

            if (state.StepIndex < def.Steps.Length - 1)
            {
                player.ActiveQuests[qid] = state with { StepIndex = state.StepIndex + 1, Counter = 0 };
                SendSystemToEntity(player, $"{def.Name}: {def.Steps[state.StepIndex + 1].Text}");
            }
            else
            {
                // Last step: park the counter at its target, the shape a finished kill step ends in.
                player.ActiveQuests[qid] = state with { Counter = Math.Max(1, step.Count) };
            }
            changed = true;
        }
        // Same reason as in AdvanceTalkStep: a collect step can hand over to a "reach level N" step the
        // player already satisfies, and only a level-up would otherwise re-check it.
        if (changed) { AdvanceLevelQuests(player); SendQuestLog(player); SaveEntity(player); }
        // 🔴 A PARTIAL pile has to be pushed too, and this is the half that IS the reported symptom. The
        // count is computed live, so the server was always right — but nothing told the client, so the
        // tracker sat on whatever number it was last sent and read exactly like a counter that does not
        // count. Markers are skipped: a mat landing in a bag cannot change who has a "!" over their head.
        else if (onCollectStep) SendQuestLog(player, withMarks: false);
    }

    private void AdvanceLevelQuests(Entity player)
    {
        bool changed = false;
        foreach (var qid in player.ActiveQuests.Keys.ToList())
        {
            var def = QuestCatalog.Get(qid);
            var state = player.ActiveQuests[qid];
            if (def is null || state.Completed) continue;

            var step = def.Steps[state.StepIndex];
            if (step.Type != QuestStepType.ReachLevel || player.Level < step.Count) continue;

            if (state.StepIndex < def.Steps.Length - 1)
            {
                player.ActiveQuests[qid] = state with { StepIndex = state.StepIndex + 1, Counter = 0 };
                SendSystemToEntity(player, $"{def.Name}: {def.Steps[state.StepIndex + 1].Text}");
            }
            else
            {
                // A ReachLevel step LAST in the chain has nothing left to do — mark it ready to hand in
                // by parking the counter at its target, the same shape a finished kill step ends in.
                player.ActiveQuests[qid] = state with { Counter = step.Count };
            }
            changed = true;
        }
        if (changed) SendQuestLog(player);
    }

    /// <summary><paramref name="withMarks"/> = false sends the LOG only. The marker sweep walks every
    /// entity in the world and then re-runs the whole offer filter per NPC, which is fine at the
    /// handful of moments the answer can change (accept / complete / abandon / level / login) and is
    /// not fine per mob kill — which is what a gathering contract, always active while you farm, would
    /// otherwise make it. A token dropping cannot change any marker: the contract was already
    /// hand-in-able the moment it was taken.</summary>
    /// <summary>Hand over the props of every active quest's CURRENT step (<see
    /// cref="QuestStep.SupplyItemIds"/>), for anything the bag does not already hold.
    ///
    /// <para>Owner, 2026-08-11: *"update the quest to give you the boxes after u speak with cera"*. He
    /// opened both creation boxes before taking the tutorial, so its "open a box" beat had nothing to
    /// open and the chain dead-ended — a DoAction step is a gate, and a gate whose prop is gone is a
    /// wall. A step that requires an object now supplies that object.</para>
    ///
    /// <para>It runs from <see cref="SendQuestLog"/> on purpose: that is the ONE call every quest-state
    /// change already funnels through (accept, each of the four advance paths, and login), so a future
    /// step type cannot forget to supply itself, and a character ALREADY stranded — his live one — is
    /// repaired the moment he logs in rather than needing a manual grant. It is a no-op for every quest
    /// in the game but this one: only a step that declares props does anything at all.</para></summary>
    private void SupplyStepItems(Entity player)
    {
        bool granted = false;
        foreach (var state in player.ActiveQuests.Values)
        {
            if (state.Completed) continue;
            var def = QuestCatalog.Get(state.QuestId);
            if (def is null || state.StepIndex < 0 || state.StepIndex >= def.Steps.Length) continue;
            var supplies = def.Steps[state.StepIndex].SupplyItemIds;
            if (supplies is null) continue;
            foreach (var itemId in supplies)
            {
                // "Holds none" is the whole guard, which is what makes this idempotent — see the field's
                // comment for why the props must be worthless for that to be safe.
                if (CountItem(player, itemId) > 0) continue;
                if (!AddItem(player, itemId)) continue;   // full bag: try again on the next push
                granted = true;
                SendSystemToEntity(player,
                    $"{def.Name}: {ItemCatalog.Get(itemId)?.Name ?? itemId} added to your bag.");
            }
        }
        if (granted) { SendInventory(player); SaveEntity(player); }
    }

    private void SendQuestLog(Entity player, bool withMarks = true)
    {
        SupplyStepItems(player);
        var active = player.ActiveQuests.Values
            .Select(st => { var d = QuestCatalog.Get(st.QuestId); return d is null ? null : Summarize(player, d, st); })
            .Where(x => x is not null).Select(x => x!).ToArray();
        SendTo(player, "QuestLog", new QuestLog(active, player.CompletedQuests.ToArray(),
                                                BuildQuestEntries(player)));
        if (withMarks) SendQuestMarks(player);
    }

    /// <summary>Every quest this character can SEE, in whatever state it is in — the three tabs of the
    /// quest window (owner, playtest-13: *"the quest windows should show active/unavailable/compleated"*).
    ///
    /// What is hidden rather than listed as locked is the owner's own rule (*"not compatables can be
    /// hidden"*): another race's or another class's quest is not a goal you can work towards, so it is
    /// noise. What you have merely not reached yet — a level floor, an unfinished prerequisite — IS
    /// listed, with the reason, because that is a plan.
    ///
    /// The gating below mirrors <see cref="QuestCatalog.OfferedBy"/> deliberately: if this list said
    /// "available" about something the NPC would not hand over, the window would be lying.</summary>
    private static QuestEntry[] BuildQuestEntries(Entity player)
    {
        int committed2 = CommittedClassChain(player, 2);
        int committed3 = CommittedClassChain(player, 3);
        var entries = new List<QuestEntry>();

        foreach (var def in QuestCatalog.AllQuests)
        {
            player.ActiveQuests.TryGetValue(def.Id, out var state);
            bool everDone = player.CompletedQuests.Contains(def.Id);

            // ----- hidden: nothing this character could ever do -------------------------------------
            if (state is null && !everDone)
            {
                if (def.ForRace is Race r && r != player.Race) continue;
                if (def.ForBaseClass is BaseClass b && b != player.BaseClass) continue;
                if (def.PreClassChange && player.SecondClass != 0) continue;
                // A class chain belongs to the class you hold; once a tier is committed (or already
                // taken) the other chains of that tier are gone for good.
                if (def.ForSecondClass is int sc && (sc != player.SecondClass || player.ThirdClass != 0))
                    continue;
                var (cid, tier) = QuestCatalog.ClassChainOf(def.Id);
                if (tier == 2 && committed2 != 0 && cid != committed2) continue;
                if (tier == 3 && committed3 != 0 && cid != committed3) continue;
            }

            entries.Add(BuildQuestEntry(player, def, state, everDone));
        }

        // Active first (what you are doing), then what you could take, then locked, then done — and
        // inside each, by level, so the list reads as an order to do things in.
        return entries
            .OrderBy(e => e.State switch
            {
                QuestAvailability.Active => 0,
                QuestAvailability.Available => 1,
                QuestAvailability.Locked => 2,
                _ => 3,
            })
            .ThenBy(e => e.MinLevel)
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static QuestEntry BuildQuestEntry(Entity player, QuestDef def,
                                              CharacterQuestState? state, bool everDone)
    {
        var availability = QuestAvailability.Available;
        string status = "";

        if (state is not null)
        {
            availability = QuestAvailability.Active;
        }
        else if (QuestClosed(player, def.Id))
        {
            availability = QuestAvailability.Completed;
            status = def.Daily ? "Done today — again after the server day rolls over" : "Completed";
        }
        else if (player.Level < def.MinLevel)
        {
            availability = QuestAvailability.Locked;
            status = $"Requires level {def.MinLevel}";
        }
        else if (def.MaxLevel > 0 && player.Level > def.MaxLevel)
        {
            availability = QuestAvailability.Locked;
            status = $"Outgrown — level {def.MaxLevel} at most";
        }
        else if (def.RequiresQuestId is string prereq && !player.CompletedQuests.Contains(prereq))
        {
            availability = QuestAvailability.Locked;
            status = $"Requires: {QuestCatalog.Get(prereq)?.Name ?? prereq}";
        }
        else if (def.Daily) status = everDone ? "Daily — ready again" : "Daily";
        else if (def.Repeatable) status = everDone ? "Repeatable — take it again" : "Repeatable";

        // Ready to hand in = on the final step and that step is a TalkTo (same rule as Summarize).
        bool canComplete = state is not null
            && state.StepIndex == def.Steps.Length - 1
            && def.Steps[^1].Type == QuestStepType.TalkTo;
        if (state is not null)
            status = canComplete || state.Completed ? "Ready to hand in" : "In progress";

        var steps = new QuestStepDto[def.Steps.Length];
        for (int i = 0; i < def.Steps.Length; i++)
        {
            var step = def.Steps[i];
            int needed = step.Type switch
            {
                QuestStepType.KillMobs or QuestStepType.CollectItem
                    or QuestStepType.DoAction => Math.Max(1, step.Count),
                _ => 1,
            };
            bool current = state is not null && state.StepIndex == i && !state.Completed;
            // A step BEFORE the one you are on is done; the current one carries the live counter. On a
            // quest you have not taken, nothing is done and nothing is current — it reads as a plan.
            bool done = state is not null && (state.Completed || state.StepIndex > i);
            // A collect step's counter is the BAG, same as in Summarize — the two windows must not
            // disagree about how many ingots you are holding.
            int have = current && step.Type == QuestStepType.CollectItem
                     ? Math.Min(needed, CountItem(player, step.TargetId ?? ""))
                     : current ? state!.Counter : done ? needed : 0;
            steps[i] = new QuestStepDto(step.Text, StepLocation(def, step),
                                        have, needed, done, current);
        }

        var gathers = def.GatherLines
            .Select(g => new QuestGatherDto(
                ItemCatalog.Get(g.ItemId)?.Name ?? g.ItemId,
                MobCatalog.Get(g.MobId).Name,
                state is null ? 0 : CountItem(player, g.ItemId),
                g.DropChance, g.RewardModifier))
            .ToArray();

        var giver = WorldMap.NpcById(def.OfferNpcId);
        // An any-town quest names the SERVICE and "any town" — naming one town's NPC would send the
        // player on exactly the journey the flag exists to spare them (M11).
        string giverName = giver is null ? ""
                         : def.AnyTownNpc ? ServiceNoun(giver.Name) : giver.Name;
        string giverTown = giver is null ? ""
                         : def.AnyTownNpc ? "any town" : WorldMap.NearestSafeZone(giver.X, giver.Y).Name;

        return new QuestEntry(
            def.Id, def.Name, def.Description, availability, status,
            giverName, giverTown,
            def.MinLevel, def.MaxLevel, def.Repeatable, def.Daily, canComplete,
            state?.StepIndex ?? 0, steps, gathers, RewardText(def.Reward), state?.Tracked ?? false);
    }

    /// <summary>The reward, as one line. Zero-valued parts are left out entirely — "SP: 0" is not
    /// information, it is a field that happens to exist.</summary>
    private static string RewardText(QuestReward reward)
    {
        var parts = new List<string>();
        if (reward.Exp > 0) parts.Add($"{reward.Exp:N0} exp");
        if (reward.SkillPoints > 0) parts.Add($"{reward.SkillPoints:N0} SP");
        if (reward.Gold > 0) parts.Add($"{reward.Gold:N0} {GameConstants.CurrencyName}");
        // Items are authored as a FLAT list with repeats — five Dash Potions are five entries — so they
        // are counted here rather than printed one row each (owner, playtest-20 #11). First-seen order
        // is kept so the authored order still reads as written. Plain "x5", not "×5": the client's TMP
        // atlas is static and draws unknown glyphs hollow.
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        foreach (var id in reward.ItemIds ?? Array.Empty<string>())
        {
            if (!counts.TryGetValue(id, out int n)) order.Add(id);
            counts[id] = n + 1;
        }
        foreach (var id in order)
        {
            string name = ItemCatalog.Get(id)?.Name ?? id;
            parts.Add(counts[id] > 1 ? $"{name} x{counts[id]}" : name);
        }
        return string.Join(" · ", parts);
    }

    /// <summary>Tell the client which NPCs have something for this player, so it can put a marker over
    /// their heads (owner, playtest-13: "quest giver need indication for new quest"). Sent from
    /// SendQuestLog so it is emitted at every point the answer can change — accept, complete, abandon,
    /// level-up, login — without a second set of call sites to keep in step.
    ///
    /// READY-TO-HAND-IN beats IN-PROGRESS beats AVAILABLE: if an NPC is both the end of one quest and
    /// the start of another, the thing you can finish NOW is the more useful thing to show.</summary>
    private void SendQuestMarks(Entity player)
    {
        var marks = new List<QuestMark>();
        foreach (var npc in _world.Entities.Values)
        {
            if (npc.Kind != EntityKind.Npc || string.IsNullOrEmpty(npc.NpcId)) continue;
            var state = QuestMarkState.None;

            // Anything active whose CURRENT step points at this NPC.
            foreach (var st in player.ActiveQuests.Values)
            {
                if (QuestCatalog.Get(st.QuestId) is not QuestDef d) continue;
                bool handIn = st.Completed || st.StepIndex >= d.Steps.Length - 1;
                var step = st.StepIndex >= 0 && st.StepIndex < d.Steps.Length ? d.Steps[st.StepIndex] : null;
                bool talkHere = step is { Type: QuestStepType.TalkTo }
                                && d.StepTargetMatches(step.TargetId, npc.NpcId);
                bool givenHere = d.GivenBy(npc.NpcId);
                if (givenHere || talkHere)
                    state = (handIn && talkHere) || (givenHere && st.Completed)
                        ? QuestMarkState.ReadyToHandIn
                        : state == QuestMarkState.ReadyToHandIn ? state : QuestMarkState.InProgress;
            }

            if (state == QuestMarkState.None && OfferedQuestCount(player, npc.NpcId) > 0)
                state = QuestMarkState.Available;

            if (state != QuestMarkState.None) marks.Add(new QuestMark(npc.Id, state));
        }
        SendTo(player, "QuestMarks", new QuestMarks(marks.ToArray()));
    }

    /// <summary>Exactly the quests this NPC would hand over if you talked to them right now — level
    /// range, race/class gating, prerequisites and the daily's day-stamp (all inside
    /// <see cref="QuestCatalog.OfferedBy"/>), PLUS the class-chain commitment: class choice is
    /// irreversible, so once you have taken (active OR completed) any chain quest of a tier, only
    /// that class's chain stays on offer.
    ///
    /// ⚠ ONE method on purpose, playtest-20 #10: *"Elder Marius shows a `!` with no quest to give
    /// after the first 2nd-class quest is done."* The dialog applied the commitment filter and the
    /// quest MARK did not, so every rival class chain Marius still nominally offers kept lighting
    /// his head up while the window he opened was empty. A marker that disagrees with the window is
    /// worse than no marker — keep both readings on this one method.</summary>
    private IEnumerable<QuestDef> OfferedQuests(Entity player, string npcId)
    {
        bool Completed(string qid) => QuestClosed(player, qid);
        bool Active(string qid) => player.ActiveQuests.ContainsKey(qid);
        int committed2 = CommittedClassChain(player, 2);
        int committed3 = CommittedClassChain(player, 3);

        return QuestCatalog
            .OfferedBy(npcId, player.Level, player.Race, player.BaseClass,
                       player.SecondClass, player.ThirdClass, Completed, Active)
            .Where(q =>
            {
                var (cid, tier) = QuestCatalog.ClassChainOf(q.Id);
                if (tier == 2 && committed2 != 0 && cid != committed2) return false;
                if (tier == 3 && committed3 != 0 && cid != committed3) return false;
                return true;
            });
    }

    /// <summary>How many quests this NPC would offer the player right now. See <see cref="OfferedQuests"/>.</summary>
    private int OfferedQuestCount(Entity player, string npcId) => OfferedQuests(player, npcId).Count();

    /// <summary>Place one mob in a zone. <paramref name="dedicatedMobId"/> names the template when the
    /// spawn comes from one of the zone's per-template spawners; null means the mixed pool, which rolls
    /// the roster.</summary>
    private void SpawnOneInZone(ZoneRuntime zr, string? dedicatedMobId = null)
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

        string mobId = dedicatedMobId ?? zone.MobTypes[_rng.Next(zone.MobTypes.Length)];
        var mobType = MobCatalog.Get(mobId);
        // A mob with a natural level brings its own (its authored base curve is tuned for it);
        // otherwise the zone assigns the level. ForceZoneLevel flips that: the ZONE wins, which is how
        // the 85-90 field re-uses the top roster to fill the last levels to the cap.
        int level = mobType.Level > 0 && !zone.ForceZoneLevel
            ? mobType.Level
            : _rng.Next(zone.MinLevel, zone.MaxLevel + 1);
        var mob = BuildMob(mobId, level, zone.Rank, x, y, zone.Id);
        mob.SpawnerMobId = dedicatedMobId;
        zr.OnSpawned(dedicatedMobId);
    }

    /// <summary>Create, configure and register one live mob: base stats from the level curve, the
    /// rank multipliers (elite/boss), the template's MobMod passives, the role archetype, and — for a
    /// Boss — its unique skill kit (BossCatalog) or the generic slam. Zone spawns pass the zone rank/id;
    /// boss ADDS pass Normal rank + an empty zone id so they don't schedule a zone respawn.</summary>
    /// <summary>Does this spawn attack on sight?
    ///
    /// The template flag is no longer the whole answer (owner, playtest-13): 71 of 80 templates are
    /// aggressive, so every field above level 10 was wall-to-wall aggro and a melee character walking
    /// into a band-appropriate zone was ganked by several casters and melee at once. The rule now:
    ///
    ///   • ELITES always attack on sight (unchanged) — BOSSES never do, they are pulled deliberately.
    ///   • Dungeons, instances and elite/boss grounds keep FULL aggression — that is their character,
    ///     and you go there on purpose.
    ///   • An ordinary field has exactly ONE aggressive type (the zone's first roster entry), so it
    ///     still bites, but you can fight one thing at a time.
    ///
    /// A mob whose template is passive stays passive everywhere — this only ever REMOVES aggression.</summary>
    private static bool ResolveAggression(string mobId, MobType mobType, MobRank rank, string zoneId)
    {
        if (rank == MobRank.Elite) return true;
        if (!mobType.Aggressive) return false;

        var zone = WorldMap.SpawnZones.FirstOrDefault(z => z.Id == zoneId);
        if (zone is null) return mobType.Aggressive;   // boss ADDs and debug spawns keep the template
        return zone.AllAggressive || zone.IsAggressiveType(mobId);
    }

    private Entity BuildMob(string mobId, int level, MobRank rank, float x, float y, string zoneId)
    {
        var mobType = MobCatalog.Get(mobId);
        // A PLAYER-BUILT creature overwrites these a few lines down, in ApplyMobBuild, with a real
        // player stat block plus his ±5 race lean (BL-47). Everything else here treats it as the
        // ordinary mob it still is, which is the point: only the numbers move.
        var stats = StatCalculator.MobStats(level, mobType.Role);

        // THE THREE CONTEST STATS (owner ruling 2026-08-19). Role default → the template's own MobMod
        // override (how a TANK creature is authored) → the RANK multiplier, which scales ATTACK as well
        // as the two defences (elite ×1.33, boss ×2 — StatCaps.CcRankMult). A boss takes the ×2 AND the
        // control immunity; the immunity is handled where the contest is rolled.
        //
        // 🔴 SPT USED TO BE ZERO ON EVERY MOB IN THE GAME. The block below assigns five core stats and
        // Spt was simply not one of them, so DebuffLandChance(atk, 0) came out at 1.0 and clamped to the
        // 90% ceiling — every root, hold and fear has been landing 9 times in 10 on everything since the
        // contest was written, raid bosses included. That is the bug this line closes, and it is why the
        // magical school gets NOTICEABLY harder with this change while the physical school gets easier.
        float ccRank = StatCaps.CcRankMult(rank);
        int ccCon = (int)MathF.Round((mobType.Mod is MobMod cm && cm.Con > 0 ? cm.Con : stats.Con) * ccRank);
        int ccSpt = (int)MathF.Round((mobType.Mod is MobMod sm && sm.Spt > 0 ? sm.Spt : stats.Spt) * ccRank);
        int ccAtk = (int)MathF.Round(stats.Atk * ccRank);

        // Elites/bosses are tougher versions of the base mob.
        //
        // ⚠ 2026-08-10 (playtest-20, his find #6): the BOSS rungs were raised hard. He soloed a raid
        // boss on an Elf dagger with nothing but common potions and the NPC buffer — 58k HP fell to
        // 1400-2000 per stab — and ruled the whole rank underweight: *"HP from x20 -> x100 (from
        // 50-60k to 250-300k), Acc +20, PAtk from x5 -> x20. MAtk seems ok."*
        //
        // HP is his number exactly (x20 -> x100). Accuracy is new: the rank had none, which is why a
        // dodge build could stand in front of a boss and simply not be hit.
        //
        // 🔴 P.Atk is the ONE place I did not take his number literally, and it is worth knowing why:
        // he quoted the boss as being at "x5" today, but the rank multiplier here has always been
        // x2.5 — there is no x5 anywhere in the boss path. Taking "x20" literally would be an 8x
        // damage jump, not the 4x his own before/after describes, so this applies his RATIO (x4) to
        // the real base: 2.5 -> 10. If he meant a literal x20, this is a one-number change.
        float hpMul = rank switch { MobRank.Elite => 4f, MobRank.Boss => 100f, _ => 1f };
        float atkMul = rank switch { MobRank.Elite => 1.5f, MobRank.Boss => 10f, _ => 1f };
        // Flat accuracy by rank — a boss must be able to land on a dodge build.
        int accFlat = rank switch { MobRank.Boss => 20, _ => 0 };

        // The weapon the creature fights with — it must be known BEFORE RecomputeDerived, because
        // that is where WeaponAttackBase is derived from it. Archer role wins (a bow IS the role),
        // then the template's own MobMod.Weapon passive, then the category default.
        //
        // 🔴 This also fixes a bug that predates the weapon passive: the Archer case below used to
        // assign mob.WeaponType AFTER RecomputeDerived had already run, so WeaponAttackBase kept the
        // bare-hand value and archer mobs never actually swung at the bow's 293. Setting it here is
        // what makes the role's own weapon count.
        WeaponType mobWeapon =
            mobType.Role == MobRole.Archer ? WeaponType.Bow
            : mobType.Mod is MobMod wm && wm.Weapon != WeaponType.None ? wm.Weapon
            : MobCatalog.DefaultWeaponFor(mobType.Category);

        // ⚠ A dummy used to be hard-named "Training Dummy (Lv N)", which threw away the template's own
        // name — so the plain dummy, the magic one and the striking one were three IDENTICAL plates in
        // a row (owner, `63h`: *"both dummies act as the old"*). The authored name carries the
        // distinction; the level suffix is what a dummy is for.
        // ⚠ An ELITE no longer carries "Elite " in its NAME (owner, 2026-08-12) — the rank moved to the
        // title line below, where it is coloured and cannot be mistaken for part of the creature's
        // name. A boss keeps its "Lord" suffix: that is a name, not a rank marker.
        string displayName = mobType.Dummy ? $"{mobType.Name} (Lv {level})" : rank switch
        {
            MobRank.Boss => $"{mobType.Name} Lord",
            _ => mobType.Name
        };

        // THE TITLE LINE. An authored one (the training dummies) wins; otherwise the RANK writes it.
        // Dungeon vs field is read from the coordinates, which is already how this codebase identifies
        // a dungeon (SpawnZone.AllAggressive: "dungeons are the negative quadrant by construction, so
        // that is what identifies one — no extra flag to keep in sync"). Reusing the rule rather than
        // adding a second one keeps them from drifting apart.
        string title = mobType.Title;
        string titleHex = TitleCatalog.NpcHex;
        if (title.Length == 0)
        {
            bool inDungeon = x < 0f || y < 0f;
            (title, titleHex) = rank switch
            {
                MobRank.Elite => ("Elite", TitleCatalog.EliteHex),
                MobRank.Boss when inDungeon => ("Dungeon Boss", TitleCatalog.DungeonBossHex),
                MobRank.Boss => ("Field Boss", TitleCatalog.FieldBossHex),
                _ => ("", TitleCatalog.NpcHex),
            };
        }

        var mob = new Entity
        {
            Name = displayName,
            // The title line, drawn above the name exactly like an NPC's role — see above for where it
            // comes from. The training dummies author theirs (`Normal` / `Physical` / `Magic`) and wear
            // the NPC grey-blue for the same reason NPCs do: a label to read once, not an achievement.
            // Elites and bosses get theirs from the rank, in colours that are meant to be loud.
            Title = title,
            TitleColor = title.Length > 0 ? titleHex : "",
            Kind = EntityKind.Mob,
            X = x,
            Y = y,
            WalkSpeed = mobType.WalkSpeed,
            RunSpeed = mobType.RunSpeed,
            Speed = mobType.RunSpeed,
            Level = level,
            Con = ccCon,           // physical-debuff resistance and NOTHING else (HP is MobBaseStats)
            // ⚠ This comment used to read "eva/acc/crit only" and that was simply wrong: AtkStat feeds
            // NEITHER evasion, accuracy nor crit (those are AGI). It feeds the contested-debuff roll and
            // StatCalculator.PhysicalDoubleChance, and nothing else — mob P/M.Atk come from the base curve.
            AtkStat = ccAtk,       // how hard this creature lands CONTROL on you (flat, by role × rank)
            Wit = stats.Wit,
            Agi = stats.Agi,
            Spt = ccSpt,           // magical-debuff resistance and NOTHING else (MP is MobBaseStats)
            // ELITES attack on sight; BOSSES do not (owner). A raid/field boss sits in its lair and is
            // fought when you choose to pull it — making it aggressive turned every approach into an
            // ambush and put a "*" on the Treant. Boss difficulty comes from its kit, not from jumping you.
            Aggressive = ResolveAggression(mobId, mobType, rank, zoneId),
            ZoneId = zoneId,
            Rank = rank,
            InnateWeaponType = mobWeapon,
            MobTypeId = mobId
        };
        // ⚠ The zone-rank and MobMod stat multipliers are RECORDED on the entity, not multiplied in
        // here (playtest-20 #7). RecomputeDerived rebuilds a mob's HP/attack/defence from the level
        // curve alone and runs again on every buff, debuff and mod change — so a factor applied once
        // at spawn was silently erased by the first debuff that landed. ApplyMobScale re-applies
        // these at the end of every recompute instead. The base curve (incl. M.Def) comes from
        // RecomputeDerived; RunSpeed/WalkSpeed it leaves alone (player-only override), so Speed
        // stays the catalog run speed.
        mob.MobHpScale = hpMul;
        mob.MobPAtkScale = atkMul;
        mob.MobMAtkScale = atkMul;
        mob.MobAccFlat = accFlat;

        // BL-47 — DRESS a player-built creature. This has to happen before the recompute below, because
        // the equip loop inside RecomputeDerived is what turns worn gear into stats; a piece added
        // afterwards would sit in the bag doing nothing until the next recompute happened to run.
        //
        // 🔑 The bag is HELD, never looted. A mob's loot is its DROP TABLE and nothing in the death path
        // so much as looks at its inventory — which is exactly the shape he asked for (*"not a dropped
        // one..but just to hold stuff"*) and is why the War Rune can be handed to a creature at all.
        if (mobType.Build is MobBuild build) mob.ApplyMobBuild(build);

        // The template's MobMod and its ROLE stat lean are read straight off the catalog by
        // ApplyMobScale (via MobTypeId), so nothing about them needs copying here. What DOES belong
        // here is anything a recompute cannot re-derive: learned skills and the spawn-time flags.
        if (mobType.Role == MobRole.Mage)
        {
            mob.CasterMob = true;
            int spellLevel = SkillCatalog.MobSpellLevel(level);
            mob.LearnedSkills[SkillCatalog.MobNukeSkill] = spellLevel;
            mob.LearnedSkills[SkillCatalog.MobBoltSkill] = spellLevel;
        }

        // Boss rank: learn its unique skill kit (BossCatalog) if it has one, else the generic
        // telegraphed AoE slam. Both cast on reuse timers by BossTick. Caster (Mage-role) bosses
        // keep their spells too.
        if (mob.Rank == MobRank.Boss)
        {
            if (BossCatalog.Get(mobId) is BossProfile profile)
                foreach (var s in profile.Skills)
                    mob.LearnedSkills[s.SkillId] = 1;
            else
                mob.LearnedSkills[SkillCatalog.BossSlamSkill] = 1;
        }

        // Training dummy: TAKES damage (so you see the numbers) but never dies — a huge HP pool +
        // big regen (both applied by ApplyMobScale, so a debuff can't strip them), plus a
        // death-floor in ApplyDamage. Stationary, never attacks.
        if (mobType.Dummy)
        {
            mob.TrainingDummy = true;
            mob.Aggressive = false;
            mob.WalkSpeed = 0; mob.RunSpeed = 0; mob.Speed = 0;
            // ...except the two that hit BACK (`56c`). Still immortal and still rooted — they simply
            // strike once per tick at anyone standing in range. See StrikeFromDummy.
            mob.DummyStrikes = mobType.Strikes;
        }

        // Second pass, now that the scales and the dummy/caster flags are set: this is the recompute
        // that actually produces the mob's final stats. Every one of them is re-derivable from here
        // on, which is the point — RecomputeDerived is idempotent for mobs now.
        mob.RecomputeDerived();

        // BL-47 — a HELD rune's power is the buff it keeps up, and the reconciliation loop that keeps it
        // up for a player is player-only and clock-driven (ReconcileTimedItems). A creature has no clock
        // and no login, so the buff is applied once here and never expires: for a mob the rune is not a
        // consumable, it is part of what the creature IS. This is the whole of comparison #4 vs #5 —
        // whether a rune can stand in for an authored attack passive.
        if (mobType.Build is MobBuild rb && rb.Held.Length > 0
            && ItemCatalog.Get(rb.Held) is { RuneBuffSkillId: { Length: > 0 } runeBuffId } runeDef
            && SkillCatalog.Get(runeBuffId) is SkillDef runeSkill)
        {
            ApplyBuff(mob, runeSkill, Math.Max(1, runeDef.RuneBuffLevel),
                      displayName: runeDef.Name, durationOverride: int.MaxValue);
        }

        mob.Hp = mob.MaxHp;
        mob.Mp = mob.MaxMp;
        mob.HomeX = mob.X;
        mob.HomeY = mob.Y;
        mob.DamageLog.Clear();
        mob.LastHitterId = null;

        _world.Entities[mob.Id] = mob;
        _world.Grid.Add(mob);
        return mob;
    }
}
