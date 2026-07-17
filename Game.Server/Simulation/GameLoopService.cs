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
                case ForgetSkillCmd c: HandleForgetSkill(c); break;
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
                case RerollAttributesCmd c: HandleRerollAttributes(c); break;
                case RemoveItemCmd c: HandleRemoveItem(c); break;
                case DebugGiveCmd c: HandleDebugGive(c); break;
                case DebugCancelAttrCmd c: HandleDebugCancelAttr(c); break;
                case CraftCmd c: HandleCraft(c); break;
                case ChooseProfessionCmd c: HandleChooseProfession(c); break;
                case DebugSetProfessionCmd c: HandleDebugSetProfession(c); break;
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
                case DebugTeleportCmd c: HandleDebugTeleport(c); break;
                case TradeRequestCmd c: HandleTradeRequest(c); break;
                case TradeRespondCmd c: HandleTradeRespond(c); break;
                case TradeOfferCmd c: HandleTradeOffer(c); break;
                case TradeGoldCmd c: HandleTradeGold(c); break;
                case TradeReadyCmd c: HandleTradeReady(c); break;
                case TradeCancelCmd c: HandleTradeCancel(c); break;
                case PartyInviteCmd c: HandlePartyInvite(c); break;
                case PartyRespondCmd c: HandlePartyRespond(c); break;
                case PartyLeaveCmd c: HandlePartyLeave(c); break;
                case PartyKickCmd c: HandlePartyKick(c); break;
                case PartySetLootModeCmd c: HandlePartySetLootMode(c); break;
                case PartyLootVoteCmd c: HandlePartyLootVote(c); break;
                case SetAutoHuntConfigCmd c: HandleSetAutoHuntConfig(c); break;
                case SetSkillBarCmd c: HandleSetSkillBar(c); break;
                case LeaveWorldCmd c: HandleLeaveWorld(c); break;
                case ToggleAutoHuntCmd c: HandleToggleAutoHunt(c); break;
                case LogoutCmd c: HandleLogout(c); break;
                case StartOfflineFarmCmd c: HandleStartOfflineFarm(c); break;
                case TogglePvpCmd c: HandleTogglePvp(c); break;
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
            entity.X = Math.Clamp(entity.X, 0, GameConstants.ZoneWidth);
            entity.Y = Math.Clamp(entity.Y, 0, GameConstants.ZoneHeight);
            _world.Entities[entity.Id] = entity;
            _world.Grid.Add(entity);
            // Anchor the static-spot centre at the login position (persisted auto-hunt has no saved centre).
            entity.FarmCenterX = entity.X;
            entity.FarmCenterY = entity.Y;
        }

        _world.EntityToConnection[entity.Id] = cmd.ConnectionId;
        _world.ConnectionToEntity[cmd.ConnectionId] = entity.Id;

        // Fresh session: refill the runtime budgets and clear any cap lock.
        entity.AutoIdleElapsedTicks = 0;
        entity.AutoOfflineElapsedTicks = 0;
        entity.AutoHuntLocked = false;

        cmd.Result.TrySetResult(new LoginResult(true, null, entity.Id, entity.X, entity.Y, GameClock.Epoch));

        AutoLearnCoreSkills(entity);
        SendInventory(entity);
        SendStats(entity);
        SendLearned(entity);   // sends the skill BAR with it, in the right order — see SendLearned
        SendSubclasses(entity);
        SendQuestLog(entity);
        SendGold(entity);
        SendAutoHuntConfig(entity);   // restore the saved auto-hunt settings in the client UI
        SendAutoHuntStatus(entity);
        SendPvpState(entity);
        if (_world.Parties.TryGetValue(entity.Id, out var rejoinParty))
            SendPartyUpdate(rejoinParty);   // clear the offline icon for the rest of the party
        if (entity.IsAdmin)
            SendSystemToEntity(entity, "Admin privileges active. Type /help for commands.");
        BroadcastSystem($"{entity.Name} entered the world.");
        _log.LogInformation("Player {Name} entered (char {Id})", entity.Name, entity.PersistentId);
    }

    // Combat state decays 30s after the last damage; the disconnect grace = _graceSeconds.
    private const int CombatDecayTicks = 300;
    private int DisconnectGraceLimit => _graceSeconds * GameConstants.TickRate;

    /// <summary>A player is "in combat" for 30s after the last damage dealt/taken.</summary>
    private bool IsInCombat(Entity e) =>
        e.LastCombatTick > 0 && _tick - e.LastCombatTick < CombatDecayTicks;

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
        if (!entity.Dead && !GameConstants.InSafeZone(entity.X, entity.Y) &&
            entity.AutoHuntEnabled && !entity.AutoHuntLocked)
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
        entity.AutoOfflineElapsedTicks = 0;
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
    private void NormalLeave(Entity entity)
    {
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
        SaveEntity(entity);
        BroadcastSystem($"{entity.Name} left the world.");
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
            return;
        _world.ConnectionToEntity.Remove(cmd.ConnectionId);
        _world.EntityToConnection.Remove(player.Id);
        NormalLeave(player);
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
        player.AutoHuntEnabled = true;
        player.AutoHuntLocked = false;
        player.FarmCenterX = player.X; player.FarmCenterY = player.Y;   // anchor the static circle here
        // Tell the client (it returns to the account screen), then drop the connection + go offline.
        _ = _hub.Clients.Client(cmd.ConnectionId).SendAsync("ForceDisconnect", "You are now farming offline.");
        _world.ConnectionToEntity.Remove(cmd.ConnectionId);
        _world.EntityToConnection.Remove(player.Id);
        BeginOfflineFarm(player);
        BroadcastSystem($"{player.Name} keeps hunting while away.");
    }

    // ----- PvP / flag / karma (L2-style). Runtime-tunable via the Debug settings panel; the values
    // here are the code DEFAULTS (move final picks back into these initializers). -----
    private const int PvpFlagTicks = 600;   // 60s purple flag after a pvp action
    private int _karmaBase = 200;              // karma for a 1st, same-level innocent kill
    private double _karmaConsecGrowth = 1.1;   // ×per consecutive PK  (+10% each)
    private double _karmaLevelGrowth = 1.2;    // ×per level the victim is BELOW the killer (+20%)
    private int _karmaLossPerDeath = 200;      // karma shed on each death
    private int _karmaLossPerMob = 20;         // karma shed per mob kill (grind it off while farming)

    // Debug test skills (admin live-tuning): the two test damage skills use Flat=_testSkillPower,
    // Mod=_testSkillMod; the test heal restores _testHealPower. For reading the {Flat, Mod} curve live.
    private int _testHealPower = 1000;
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

    // ----- Debug live-tuning (admin only) -----
    private DebugConfigDto CurrentDebugConfig() => new(
        RateConfig.ExpRate, RateConfig.SpRate, RateConfig.DropChanceRate, RateConfig.DropAmountRate,
        RateConfig.GoldAmountRate,
        _karmaBase, (float)_karmaConsecGrowth, (float)_karmaLevelGrowth, _karmaLossPerDeath, _karmaLossPerMob,
        _idleCapSeconds, _offlineCapSeconds, _graceSeconds,
        _testHealPower, _testSkillPower, _testSkillMod);

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
        SendSystemToEntity(p, "[DEBUG] Tuning applied + saved (debug-config.json).");
        SendTo(p, "DebugConfig", CurrentDebugConfig());   // echo back the clamped values
    }

    private void ApplyDebugConfig(DebugConfigDto c)
    {
        RateConfig.ExpRate        = Math.Max(0f, c.ExpRate);
        RateConfig.SpRate         = Math.Max(0f, c.SpRate);
        RateConfig.DropChanceRate = Math.Max(0f, c.DropChanceRate);
        RateConfig.DropAmountRate = Math.Max(0f, c.DropAmountRate);
        RateConfig.GoldAmountRate = Math.Max(0f, c.GoldRate);
        _karmaBase          = Math.Max(0, c.KarmaBase);
        _karmaConsecGrowth  = Math.Max(1.0, c.KarmaConsecGrowth);
        _karmaLevelGrowth   = Math.Max(1.0, c.KarmaLevelGrowth);
        _karmaLossPerDeath  = Math.Max(0, c.KarmaLossPerDeath);
        _karmaLossPerMob    = Math.Max(0, c.KarmaLossPerMob);
        _idleCapSeconds     = Math.Clamp(c.IdleCapSeconds, 0, 24 * 3600);   // 0 = unlimited
        _offlineCapSeconds  = Math.Clamp(c.OfflineCapSeconds, 0, 24 * 3600);
        _graceSeconds       = Math.Clamp(c.GraceSeconds, 5, 3600);
        _testHealPower      = Math.Max(0, c.TestHealPower);
        _testSkillPower     = Math.Max(0, c.TestSkillPower);
        _testSkillMod       = Math.Max(0f, c.TestSkillMod);
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

    /// <summary>May 'attacker' damage 'target'? A mob on either side = always (normal PvE). Player→
    /// player requires out of safe zones, and: a RED/PURPLE target is freely attackable (retaliation
    /// / executing an outlaw), while attacking an INNOCENT (white) needs the PvP opt-in.</summary>
    private bool CanPvpHit(Entity attacker, Entity target)
    {
        if (attacker.Kind != EntityKind.Player || target.Kind != EntityKind.Player)
            return true;
        if (attacker.Id == target.Id || target.Dead)
            return false;
        if (GameConstants.InSafeZone(attacker.X, attacker.Y) || GameConstants.InSafeZone(target.X, target.Y))
            return false;
        return FlagOf(target) != PvpFlag.Innocent || attacker.PvpEnabled;
    }

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

        if (target.Kind == EntityKind.Player && !CanPvpHit(attacker, target))
        {
            SendSystemToEntity(attacker, "You can't attack that player here. (Enable PvP; not in towns.)");
            return;
        }

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

        // Base MAGE gets Robe Mastery free at level 1; fighters learn their Armor Mastery
        // from the class table at level 5 (no level-1 armor mastery).
        if (player.BaseClass == BaseClass.Mage && !player.HasSkill(SkillCatalog.MasteryRobe))
            player.LearnedSkills[SkillCatalog.MasteryRobe] = 1;

        // Weapon Proficiency — every mage learns it at level 1 (untrained-weapon cast-speed penalty).
        if (player.BaseClass == BaseClass.Mage)
            player.LearnedSkills.TryAdd(SkillCatalog.WeaponProficiency, 1);

        // Divine Focus — the Healer 2nd class learns Lv1 at 20; the Warchanter discipline upgrades to Lv2 at
        // 40 (softer heal penalty in fighter gear). Never downgrade a Warchanter back to Lv1.
        if (player.Archetype == Archetype.Healer)
        {
            int want = player.Discipline == Discipline.Warchanter ? 2 : 1;
            if (player.SkillLevelOf(SkillCatalog.DivineFocus) < want)
                player.LearnedSkills[SkillCatalog.DivineFocus] = want;
        }

        // Resurrection is NOT auto-granted (owner, 2026-07-17) — it is bought with SP off the class
        // tables like any other skill: L1 @20 / L2 @40 on the cleric list (every cleric keeps those
        // through any 3rd class), L3 @52 / L4 @61 on the Lightbringer list. See ClassSkillTables.

        // Combat "training" passive (soulshot/spiritshot stand-in): auto-granted, level chosen by
        // character level (+10%…+100% atk; see TrainingLevelFor). GATED ON THE 3RD CLASS (owner): like
        // the stat swaps, it only appears once you've taken your 3rd-class discipline, not merely at 40.
        SyncTrainingPassive(player);

        // Class identity "sure" floor passive for the current class tier (level = tier).
        if (SkillCatalog.FloorPassiveFor(player.Archetype, player.Level) is { } floor)
            player.LearnedSkills[floor.Id] = floor.Level;

        // Class Balance passive — the per-class tuning hook. No effect today; it's where a
        // class's damage gets nudged later instead of hardcoding a coefficient.
        player.LearnedSkills[SkillCatalog.ClassBalanceFor(player.Archetype, player.BaseClass)] = 1;

        // Novice's Grace — display-only newbie protection, shown below the death-penalty level and removed at it.
        if (player.Level < GameConstants.DeathExpPenaltyMinLevel)
            player.LearnedSkills.TryAdd(SkillCatalog.NoviceGrace, 1);
        else
            player.LearnedSkills.Remove(SkillCatalog.NoviceGrace);

        // ==================== TEST ONLY — DELETE ME ====================
        // A power-1000 heal for EVERY class at 76, so the heal formula can be calibrated (and the
        // tank-vs-healer gap read directly). Remove this block with the rest — search "TEST ONLY".
        if (player.Level >= 76) player.LearnedSkills.TryAdd(SkillCatalog.TestHeal, 1);
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
        if (player.Level >= 76)
            player.LearnedSkills.TryAdd(SkillCatalog.AngelsProtection, 1);
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
        if (cur == 0 && !string.IsNullOrEmpty(def.ExclusiveGroup))
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

        // THE DIRECTION RULE (stat swaps): every stat you touch commits to one direction. You may not
        // raise a stat you have sold, nor sell a stat you have bought. Without this you could buy a
        // circular +A−B, +B−C, +C−A ring that nets to +0 for 45kk of gold. Stacking a second skill
        // that also LOWERS the same stat is still allowed. See SkillCatalog.StatSwapConflict.
        if (cur == 0 && SkillCatalog.StatSwapConflict(def.Id, player.LearnedSkills.Keys) is { } clash)
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

        // GOLD price (the stat-swap passives cost gold, not SP: 1kk…5kk per level).
        int gold = def.GoldCostAt(target);
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

        var def = SkillCatalog.Get(cmd.SkillId);
        if (def is null || !caster.HasSkill(def.Id))
            return;

        // Passives (armor masteries) are always-on; they can't be cast.
        if (def.Category == SkillCategory.Passive)
            return;

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

        if (caster.Mp < def.MpCostAt(caster.SkillLevelOf(def.Id)))
        {
            SendSystemToEntity(caster, "Not enough MP.");
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
        if (def.PlacesTrap || def.GrantsStealth)
        {
            // Self-delivered: a trap drops at the caster's feet; stealth cloaks the caster. Even
            // though a trap carries damage/CC flags (its deferred payload), it needs no live target.
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
            if (target.Kind == EntityKind.Player && !CanPvpHit(caster, target))
            {
                SendSystemToEntity(caster, "You can't attack that player here. (Enable PvP; not in towns.)");
                return;
            }
            targetId = tid;
        }
        else if (IsAllyTargetable(def) &&
                 def.TargetMode != TargetMode.SelfOnly && def.Range > 0 &&
                 cmd.TargetId is Guid allyId &&
                 _world.Entities.TryGetValue(allyId, out var ally) &&
                 ally.Kind == EntityKind.Player && !ally.Dead &&
                 SameParty(caster, ally))
        {
            targetId = allyId; // ranged heal / cleanse / buff on a PARTY member
        }
        else
        {
            // Self-cast. Crucially this is where a support skill lands when you have an ENEMY
            // targeted: it used to accept ANY player, so healing mid-duel healed the man you were
            // fighting. A support skill can only ever reach yourself or a party member.
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
        string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
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
        int mp = def.MpCostAt(level);
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

        // (No archetype-uniqueness check: you may own several classes of the same 2nd class, as long as
        // they branch into different 3rd-class DISCIPLINES — that check lives on the 3rd-class change.)

        // NOTE: the class change no longer raises main stats. You keep the CON/ATK/WIT/DEX you
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
                trade.OfferOf(player).Contains(item.InstanceId))
                return;

            // JEWELS stack PER SUB-TYPE: 2 rings, 2 earrings, 1 necklace. Refuse when
            // that sub-type's slots are full (other slots are one-per-slot).
            if (def.Slot == EquipSlot.Jewel)
            {
                int sameType = player.Inventory.Count(i => i.Equipped && i != item
                    && ItemCatalog.Get(i.DefId) is ItemDef j
                    && j.Slot == EquipSlot.Jewel && j.JewelType == def.JewelType);
                if (sameType >= ItemCatalog.MaxOfJewelType(def.JewelType))
                {
                    string label = def.JewelType.ToString().ToLowerInvariant();
                    SendSystemToEntity(player,
                        $"You can't wear another {label} ({ItemCatalog.MaxOfJewelType(def.JewelType)} max).");
                    return;
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
        }

        player.RecomputeDerived();
        SendInventory(player);
        SendStats(player);
        SaveEntity(player);   // persist equip changes immediately (survive restarts)
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

        // Block destroying items that are currently in a trade offer.
        if (_world.ActiveTrades.TryGetValue(player.Id, out var trade) &&
            trade.OfferOf(player).Contains(item.InstanceId))
            return;

        bool wasEquipped = item.Equipped;

        if (item.Quantity > 1 && !cmd.All)
            item.Quantity--;             // drop ONE from the stack
        else
            player.Inventory.Remove(item);   // whole stack (or a single item)

        if (wasEquipped)
            player.RecomputeDerived();

        SendInventory(player);
        if (wasEquipped)
            SendStats(player);
    }

#pragma warning disable CS1998
    private void HandleDebugTeleport(DebugTeleportCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead) return;
        player.X = Math.Clamp(cmd.X, 0, GameConstants.ZoneWidth);
        player.Y = Math.Clamp(cmd.Y, 0, GameConstants.ZoneHeight);
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
        if (!AddItem(player, def.Id))
        {
            SendSystemToEntity(player, "Inventory full.");
            return;
        }
        SendSystemToEntity(player, $"[DEBUG] Added {def.Name}.");
        SendInventory(player);
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

    /// <summary>Craft a recipe: check profession + learned + inputs, consume inputs, roll the
    /// success chance, and produce the output on success (a failed craft still consumes the mats).</summary>
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
        // DropOnly recipes (A-grade sets) must be learned from a dropped recipe BOOK; auto-known
        // recipes just need the level gate.
        if (recipe.DropOnly)
        {
            if (!player.KnownRecipes.Contains(recipe.Id))
            {
                SendSystemToEntity(player, "You haven't learned that recipe (find its recipe book).");
                return;
            }
        }
        else if (player.Level < recipe.LearnLevel)
        {
            SendSystemToEntity(player, $"You must be level {recipe.LearnLevel} to craft this.");
            return;
        }
        foreach (var inp in recipe.Inputs)
            if (CountItem(player, inp.ItemId) < inp.Qty)
            {
                SendSystemToEntity(player, "You don't have the required materials.");
                return;
            }
        foreach (var inp in recipe.Inputs)
            ConsumeItem(player, inp.ItemId, inp.Qty);

        string outName = ItemCatalog.Get(recipe.OutputId)?.Name ?? recipe.OutputId;
        if (_rng.NextDouble() < recipe.SuccessChance)
        {
            AddItem(player, recipe.OutputId, recipe.OutputQty);
            SendSystemToEntity(player, $"Crafted {outName}" + (recipe.OutputQty > 1 ? $" x{recipe.OutputQty}." : "."));
        }
        else
        {
            SendSystemToEntity(player, $"Craft failed — the materials were lost.");
        }
        SendInventory(player);
    }

    /// <summary>Choose the character's one permanent profession (rejected if already chosen).
    /// Persists with the character via the snapshot; recipes then auto-unlock by level.</summary>
    private void HandleChooseProfession(ChooseProfessionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        if (player.Profession != Profession.None)
        {
            SendSystemToEntity(player, $"You are already a {player.Profession} — professions can't be changed.");
            return;
        }
        if (cmd.Profession < 1 || cmd.Profession > (int)Profession.ScrollScribe)
        {
            SendSystemToEntity(player, "Invalid profession.");
            return;
        }
        player.Profession = (Profession)cmd.Profession;
        SendSystemToEntity(player, $"You are now a {player.Profession}. (Recipes unlock as you level.)");
    }

    private void HandleDebugSetProfession(DebugSetProfessionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player))
            return;
        player.Profession = (Profession)Math.Clamp(cmd.Profession, 0, (int)Profession.ScrollScribe);
        SendSystemToEntity(player, $"[DEBUG] Profession set to {player.Profession}.");
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
            SendSystemToEntity(player, $"You already have a {tcd.Discipline} class.");
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

    /// <summary>Switch to a class this character already owns.</summary>
    private void HandleSwitchSubclass(SwitchSubclassCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        if (player.ActiveSubclass.Slot == cmd.Slot)
            return;
        if (player.Subclasses.All(s => s.Slot != cmd.Slot))
        {
            SendSystemToEntity(player, "You don't have that class.");
            return;
        }
        ActivateSubclass(player, cmd.Slot, null);
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
        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), false));

        SendSystemToEntity(player, message
            ?? $"[DEBUG] Now playing class #{slot}: {player.BaseClass} (level {player.Level}).");
        SaveEntity(player);
    }

    private void SendSubclasses(Entity p) =>
        SendTo(p, "Subclasses", new SubclassListDto(p.Subclasses
            .OrderBy(s => s.Slot)
            .Select(s => new SubclassDto(
                s.Slot, s.Race, s.BaseClass, s.SecondClass, s.ThirdClass, s.Level,
                s.Slot == p.ActiveSubclass.Slot))
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
            // Delevel: keep the learned skills, but re-sync the level-derived training passive
            // (see the note above) and rebuild the stats off the new level.
            SyncTrainingPassive(player);
            player.RecomputeDerived();
            player.Hp = Math.Min(player.Hp, player.MaxHp);
            player.Mp = Math.Min(player.Mp, player.MaxMp);
            SendStats(player);
            SendLearned(player);
        }

        if (_world.EntityToConnection.TryGetValue(player.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("Progress", new ProgressUpdate(
                player.Level, player.Exp, StatCalculator.ExpToNext(player.Level), up));
        SendSystemToEntity(player, $"[DEBUG] Level {(up ? "up" : "down")} -> {player.Level}.");
        SaveEntity(player);   // persist so debug levels survive a server restart
    }

    /// <summary>Re-point the auto-granted combat-training passive at the level the character is NOW.
    /// The single grant point for this passive (called on login, level-up and delevel).
    ///
    /// GATED ON THE 3RD CLASS (owner, 2026-07-15): the passive only exists once you've taken your
    /// 3rd-class discipline — the same rule as the level-40 stat swaps. Without a 3rd class, or below
    /// its level band, it is removed outright.</summary>
    private static void SyncTrainingPassive(Entity player)
    {
        string id = player.BaseClass == BaseClass.Mage
            ? SkillCatalog.SpiritTraining
            : SkillCatalog.PhysicalTraining;

        int lvl = player.ThirdClass > 0 ? StatCalculator.TrainingLevelFor(player.Level) : 0;
        if (lvl > 0) player.LearnedSkills[id] = lvl;
        else player.LearnedSkills.Remove(id);
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
            SendSystemToEntity(player, $"Another of your classes is already a {tcd.Discipline}.");
            return;
        }

        // Ensure the parent 2nd class. (No core-stat bonus: class changes no longer touch
        // main stats — see the 2nd-class path.)
        if (player.SecondClass != tcd.ParentSecondClassId)
            player.SecondClass = tcd.ParentSecondClassId;
        player.ThirdClass = cmd.ThirdClassId;

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

        if (player.BaseClass == BaseClass.Mage)
        {
            Give(ItemCatalog.NewbieStaff);
            Give(ItemCatalog.BoxNewbieArmorRobe);
        }
        else
        {
            Give(ItemCatalog.BoxNewbieWeapons);   // selection box: pick 2
            Give(ItemCatalog.BoxNewbieArmorLight);
        }
        Give(ItemCatalog.BoxNewbieJewels);
        Give(ItemCatalog.MinorPotion, 5);
        Give(ItemCatalog.GreaterPotion, 2);
    }
#pragma warning restore CS1998

    private void HandleUsePotion(UsePotionCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is not null)
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
                player.QueuedSkillId = skill.Id;
                player.QueuedTargetId = rid;
                return true;
            }
            player.QueuedSkillId = skill.Id;
            player.QueuedTargetId = player.Id;
            return true;
        }

        // Instant consumable (drink it). Healing potions share one drink cooldown; buff potions
        // are free of it.
        bool healing = ItemCatalog.IsHealPotion(def);
        if (healing && player.PotionCooldown > 0)
            return false;

        // A ZERO-cast consumable still has its own reuse timer — the channelled path above set that for
        // it, so without this an instant scroll would have no cooldown at all.
        if (player.SkillCooldowns.TryGetValue(skill.Id, out int icd) && icd > 0)
        {
            SendSystemToEntity(player, $"{skill.Name} is on cooldown.");
            return false;
        }
        if (skill.CooldownTicks > 0)
            player.SkillCooldowns[skill.Id] = skill.CooldownTicks;   // fixed: never shortened by reuse buffs

        // The SKILL decides what happens — we only deliver it. An instant Heal restores a % of max
        // HP; anything with a lasting effect (a HoT potion, a buff potion) becomes an ordinary buff,
        // so it lands on the buff bar and supersedes weaker ones by BuffKey + Rank.
        //
        // TeleportsToTown must be handled HERE as well as on the cast-completion path: the Ultimate
        // Scroll of Return is the escape button, so it has NO cast — and a 0-tick skill never reaches
        // the cast pipeline. Without this it would be eaten for no effect.
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
            ApplyBuff(player, skill);
            PushBuffs(player);
        }

        ConsumeOne(player, item);
        if (healing) player.PotionCooldown = def.PotionCooldownTicks;

        SendInventory(player);
        SendPotionStatus(player);
        if (!healing) SendSystemToEntity(player, $"{skill.Name} active.");
        return true;
    }

    private void SendPotionStatus(Entity player)
    {
        // The lingering effect is a BUFF now (it shows on the buff bar), so the only thing the
        // potion channel still owns is the shared drink cooldown.
        float cd = player.PotionCooldown / (float)GameConstants.TickRate;
        SendTo(player, "Potion", new PotionStatus(cd, ""));
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

        // No trading while a PK (red) or FLAGGED (purple) — on either side (owner). An outlaw can't
        // launder gear/gold through a trade.
        if (FlagOf(requester) != PvpFlag.Innocent)
        {
            SendSystemToEntity(requester, "You can't trade while flagged or a PK.");
            return;
        }
        if (FlagOf(target) != PvpFlag.Innocent)
        {
            SendSystemToEntity(requester, $"{target.Name} can't trade right now (flagged or a PK).");
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

        // Re-check flags at accept time (they can change after the request).
        if (FlagOf(requester) != PvpFlag.Innocent || FlagOf(responder) != PvpFlag.Innocent)
        {
            SendSystemToEntity(responder, "You can't trade while either of you is flagged or a PK.");
            SendSystemToEntity(requester, "You can't trade while either of you is flagged or a PK.");
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
        if (!TryGetPlayer(cmd.ConnectionId, out var inviter) || inviter.Dead)
            return;
        if (!_world.Entities.TryGetValue(cmd.TargetId, out var target) ||
            target.Kind != EntityKind.Player || target.Dead || target.Id == inviter.Id)
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

    // ----- Auto-hunt / idle farming (docs/AutoHunt.md) -------------------------

    // Roaming: how far past the farm circle a current target may be chased (soft static spot), and
    // how close to the static centre counts as "back home".
    private const float AutoChaseMargin = 400f;
    private const float AutoReturnEpsilon = 150f;

    // Runtime caps (docs/AutoHunt.md): online idle 8h, offline 2h; disconnect grace 180s. Tunable in
    // seconds via the Debug panel / /testcaps. Purchasable extensions (12h/4h) are a future hook.
    private int _idleCapSeconds = 8 * 3600;
    private int _offlineCapSeconds = 2 * 3600;
    private int _graceSeconds = 180;
    // 0 (or less) = UNLIMITED (never caps) — for leaving people farming/levelling to gauge speed.
    private long AutoIdleCapTicks(Entity p) => _idleCapSeconds <= 0 ? long.MaxValue : (long)_idleCapSeconds * GameConstants.TickRate;
    private long AutoOfflineCapTicks(Entity p) => _offlineCapSeconds <= 0 ? long.MaxValue : (long)_offlineCapSeconds * GameConstants.TickRate;

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
        SaveEntity(p);
    }

    private void HandleSetAutoHuntConfig(SetAutoHuntConfigCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p))
            return;
        var c = cmd.Config;
        // A cap lock blocks re-enabling until re-log; other settings still save.
        p.AutoHuntEnabled   = c.Enabled && !p.AutoHuntLocked;
        if (c.Enabled && p.AutoHuntLocked)
            SendSystemToEntity(p, "Auto-hunt is locked until you re-log.");
        p.AutoHpPotionPct   = Math.Clamp(c.HpPotionPct, 0, 100);
        p.AutoMpPotionPct   = Math.Clamp(c.MpPotionPct, 0, 100);
        p.AutoBuffPotions   = c.AutoBuffPotions;
        p.AutoSkills.Clear();
        foreach (var s in c.Skills ?? Array.Empty<AutoSkillDto>())
            p.AutoSkills.Add(new AutoSkillDto(s.SkillId, s.Enabled, Math.Max(0, s.ExtraDelayTicks)));
        p.AutoBuffPotionIds.Clear();
        foreach (var id in c.BuffPotionIds ?? Array.Empty<string>())
            p.AutoBuffPotionIds.Add(id);
        p.AutoFarmRange   = Math.Clamp(c.FarmRange, 200, 2000);
        p.AutoFarmStatic  = c.StaticSpot;
        p.AutoAttackNormal = c.AttackNormal;
        p.AutoAttackElite = c.AttackElite;
        p.AutoAttackBoss  = c.AttackBoss;
        p.AutoReadyTick.Clear();
        if (p.AutoHuntEnabled) { p.FarmCenterX = p.X; p.FarmCenterY = p.Y; }   // (re)anchor the static circle
        SendAutoHuntStatus(p);   // persisted by the normal autosave/logout snapshot
    }

    private void HandleToggleAutoHunt(ToggleAutoHuntCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var p))
            return;
        if (cmd.Enabled && p.AutoHuntLocked)
        {
            SendSystemToEntity(p, "Auto-hunt is locked until you re-log.");
            SendAutoHuntConfig(p);
            return;
        }
        p.AutoHuntEnabled = cmd.Enabled;
        if (p.AutoHuntEnabled) { p.FarmCenterX = p.X; p.FarmCenterY = p.Y; }   // anchor the static circle here
        SendAutoHuntConfig(p);
        SendAutoHuntStatus(p);
        if (_world.Parties.TryGetValue(p.Id, out var pty))
            SendPartyUpdate(pty);   // toggle the party AFK/Auto icon promptly
        SendSystemToEntity(p, p.AutoHuntEnabled ? "Auto-hunt ON." : "Auto-hunt OFF.");
    }

    /// <summary>Per-tick automation for a player (called before UpdateAction). Auto-potions always
    /// run; the hunt loop (target/engage/auto-skill) runs only when AutoHuntEnabled.</summary>
    private void AutoPilot(Entity p)
    {
        AutoPotions(p);

        if (!p.AutoHuntEnabled)
            return;
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
                p.Engaged = AutoBasicAttackEnabled(p);
                if (TryAutoSkill(p, foe))
                    return;
                if (!p.Engaged) { p.TargetX = foe.X; p.TargetY = foe.Y; }   // mage: close in for skills
                return;
            }
        }

        var target = ValidAutoTarget(p) ?? AcquireAutoTarget(p);
        bool basic = AutoBasicAttackEnabled(p);
        if (target is not null)
        {
            p.CombatTargetId = target.Id;
            p.Engaged = basic;   // only auto BASIC-attack if the Basic Attack row is enabled
        }
        else
        {
            p.Engaged = false;
            p.CombatTargetId = null;
        }

        // A queued skill (buffs/heals need no target; attack/debuff do) is cast+chased by UpdateAction.
        if (TryAutoSkill(p, target))
            return;

        if (target is not null)
        {
            // Have a target but nothing to cast this tick. Basic on → UpdateAutoAttack (via Engaged)
            // handles it. Basic off (mage) → walk toward the target so a skill can land when ready.
            if (!basic)
            {
                p.TargetX = target.X;
                p.TargetY = target.Y;
            }
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
        if (p.AutoHpPotionPct > 0 && p.PotionCooldown <= 0 && p.MaxHp > 0 &&
            p.Hp * 100 < p.MaxHp * p.AutoHpPotionPct &&
            BestHealPotion(p) is InventoryItem hpPot)
            UsePotion(p, hpPot);

        // MP potions don't exist as items yet — reserved plumbing (BestManaPotion returns null).
        if (p.AutoMpPotionPct > 0 && p.PotionCooldown <= 0 && p.MaxMp > 0 &&
            p.Mp * 100 < p.MaxMp * p.AutoMpPotionPct &&
            BestManaPotion(p) is InventoryItem mpPot)
            UsePotion(p, mpPot);

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
                string key = string.IsNullOrEmpty(bd.BuffKey) ? bd.Name : bd.BuffKey;
                if (p.Buffs.Any(b => b.Key == key))
                    continue;   // buff already up
                UsePotion(p, item);
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

    private enum AutoSkillKind { Attack, Debuff, Buff, Heal, Other }

    private static AutoSkillKind ClassifyAuto(SkillDef def)
    {
        var e = def.Effect;
        if ((e & (SkillEffect.PhysicalDamage | SkillEffect.MagicDamage)) != 0) return AutoSkillKind.Attack;
        if ((e & SkillEffect.Heal) != 0) return AutoSkillKind.Heal;
        if (def.Category == SkillCategory.Buff || (e & SkillEffect.AnyBuff) != 0) return AutoSkillKind.Buff;
        if ((e & SkillEffect.ContestCc) != 0 || def.DebuffSchool != DebuffSchool.None) return AutoSkillKind.Debuff;
        return AutoSkillKind.Other;
    }

    /// <summary>Queue the first eligible auto-skill (known, enabled, off base-cd AND past its extra
    /// delay, MP affordable, condition met). Returns true if one was queued.</summary>
    private bool TryAutoSkill(Entity p, Entity? target)
    {
        foreach (var entry in p.AutoSkills)
        {
            if (!entry.Enabled) continue;
            if (SkillCatalog.Get(entry.SkillId) is not SkillDef def || !p.HasSkill(def.Id)) continue;
            if (p.SkillCooldowns.ContainsKey(def.Id)) continue;
            if (_tick < p.AutoReadyTick.GetValueOrDefault(def.Id)) continue;

            int lvl = Math.Max(1, p.SkillLevelOf(def.Id));
            int mpNeed = (int)((def.InitialMpAt(lvl) + def.FinishMpAt(lvl)) * MpCostFactor(p, def));
            if (p.Mp < mpNeed) continue;

            string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
            Guid tgtId;
            switch (ClassifyAuto(def))
            {
                case AutoSkillKind.Buff:
                    if (p.Buffs.Any(b => b.Key == key)) continue;   // already up
                    tgtId = p.Id; break;
                case AutoSkillKind.Heal:
                    if (p.MaxHp <= 0 || (float)p.Hp / p.MaxHp >= 0.70f) continue;
                    tgtId = p.Id; break;
                case AutoSkillKind.Debuff:
                    if (target is null || target.Buffs.Any(b => b.Key == key)) continue;
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
            return true;
        }
        return false;
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
            float mp = (def.InitialMpAt(lvl) + def.FinishMpAt(lvl)) * MpCostFactor(p, def);
            float reuseSec = Math.Max(0.1f, AutoCycleTicks(p, def, entry.ExtraDelayTicks) * GameConstants.TickSeconds);
            float mps = mp / reuseSec;
            totalMps += mps;
            string name = ClassSkills.DisplayName(def.Id, p.Race, p.BaseClass, p.Archetype, p.Discipline);
            rows.Add(new AutoSkillReuse(def.Id, name, reuseSec, mps));
        }
        SendTo(p, "AutoHunt", new AutoHuntStatus(p.AutoHuntEnabled, totalMps, rows.ToArray()));
    }

    /// <summary>Echo the full stored config so the client UI reflects the persisted settings.</summary>
    private void SendSkillBar(Entity p) =>
        SendTo(p, "SkillBar", new SkillBarDto(p.ActiveSkillBar));

    private void SendAutoHuntConfig(Entity p) =>
        SendTo(p, "AutoConfig", new AutoHuntConfigDto(
            p.AutoHuntEnabled, p.AutoHpPotionPct, p.AutoMpPotionPct, p.AutoBuffPotions,
            p.AutoSkills.ToArray(), p.AutoBuffPotionIds.ToArray(),
            p.AutoFarmRange, p.AutoFarmStatic, p.AutoAttackNormal, p.AutoAttackElite, p.AutoAttackBoss));

    /// <summary>Advance the auto-hunt runtime cap for a player. Online = the idle cap (stop + lock);
    /// offline = the offline cap (queue a logout). Called each tick while auto-hunt is enabled.</summary>
    private void TickAutoHuntBudget(Entity p)
    {
        if (p.IsOfflineFarming)
        {
            if (++p.AutoOfflineElapsedTicks >= AutoOfflineCapTicks(p))
                _endOfflineQueue.Add(p.Id);
        }
        else if (++p.AutoIdleElapsedTicks >= AutoIdleCapTicks(p))
        {
            StopAutoHunt(p, "the idle time limit was reached — re-log to hunt again.", locked: true);
        }
    }

    /// <summary>Turn auto-hunt off (and disengage). locked=true also blocks re-enabling until the
    /// next login (cap reached). No-ops the UI pushes automatically for an offline (connectionless)
    /// entity.</summary>
    private void StopAutoHunt(Entity p, string reason, bool locked)
    {
        p.AutoHuntEnabled = false;
        if (locked) p.AutoHuntLocked = true;
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
                members.Add(new PartyMemberDto(m.Id, m.Name, m.Level, PartyClassLabel(m),
                    (int)m.Hp, m.MaxHp, (int)m.Mp, m.MaxMp, mid == party.LeaderId,
                    m.IsOfflineFarming || m.IsDisconnected ? PartyMemberStatus.Offline
                        : m.AutoHuntEnabled ? PartyMemberStatus.Auto
                        : PartyMemberStatus.Online,
                    debuffs.Length > 0 ? debuffs : null));
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
            session.A.Inventory.Count - itemsA.Count + itemsB.Count
                <= GameConstants.InventorySize &&
            session.B.Inventory.Count - itemsB.Count + itemsA.Count
                <= GameConstants.InventorySize;

        if (!valid)
        {
            SendSystemToEntity(session.A, "Trade failed (items/gold changed or bags full).");
            SendSystemToEntity(session.B, "Trade failed (items/gold changed or bags full).");
            CloseTrade(session);
            return;
        }

        foreach (var item in itemsA!)
            TransferItem(session.A, session.B, item);
        foreach (var item in itemsB!)
            TransferItem(session.B, session.A, item);

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
            session.ReadyOf(partner),
            session.GoldOf(viewer),
            session.GoldOf(partner));
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

            case "testcaps":
                bool shortCaps = arg is not ("off" or "0" or "false");
                (_idleCapSeconds, _offlineCapSeconds, _graceSeconds) = shortCaps
                    ? (30, 20, 15) : (8 * 3600, 2 * 3600, 180);
                SendSystemToEntity(admin, shortCaps
                    ? "[DEBUG] Short caps ON: idle 30s / offline 20s / disconnect grace 15s."
                    : "[DEBUG] Short caps OFF: idle 8h / offline 2h / grace 180s.");
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
            if (entity.StealthTicks > 0)
                entity.StealthTicks--;

            if (entity.Kind == EntityKind.Player)
                TickPotion(entity);

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
            }

            UpdateAction(entity);
            MoveTowardTarget(entity);
            _world.Grid.UpdatePosition(entity);

            if (regenTick)
            {
                TickDots(entity);           // damage-over-time (bleed/poison/venom) ticks per second
                TickHealOverTime(entity);   // HoT heals even in combat (unlike natural regen)
                Regenerate(entity);
                if (entity.Kind == EntityKind.Player)
                {
                    PushBuffs(entity);
                    if (entity.AutoSkills.Count > 0)
                        SendAutoHuntStatus(entity);   // keep MP/s live as buffs change
                }
            }
        }

        TickTraps();

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

        if (regenTick)
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
            var (pFlat, pMod) = def.PhysDamageAt(lvl);
            int dmg = StatCalculator.PhysicalDamageFM(
                (int)attacker.EffectiveAttack, pFlat, pMod, (int)victim.EffectiveDefence,
                StatCalculator.WeaponDefenceCoef(attacker.WeaponType, victim.PierceDefCoef, victim.BluntDefCoef, victim.BowDefCoef));
            dmg = FinalizeDamage(attacker, victim, dmg, DamageKind.SkillPhysical, def);
            BroadcastCombat(attacker, victim, dmg, CombatOutcome.Hit, name);
            ApplyDamage(victim, dmg, attacker);
        }
        if (effect.HasFlag(SkillEffect.MagicDamage) && !victim.Dead)
        {
            var (mFlat, mMod) = def.MagicDamageAt(lvl);
            int dmg = StatCalculator.MagicDamageFM(
                (int)attacker.EffectiveMagicAttack, mFlat, mMod, (int)victim.EffectiveMagicDefence);
            dmg = FinalizeDamage(attacker, victim, dmg, DamageKind.SkillMagic, def);
            BroadcastCombat(attacker, victim, dmg, CombatOutcome.Hit, name);
            ApplyDamage(victim, dmg, attacker);
        }
        // Contested CC (Root/Stun/Slow): the control payload.
        if ((effect & SkillEffect.ContestCc) != 0 && !victim.Dead)
        {
            int atkStat = attacker.AtkStat;
            int defStat = def.DebuffSchool == DebuffSchool.Magical ? (int)victim.EffectiveWit : victim.Con;
            float land = victim.Immune ? 0f : StatCalculator.DebuffLandChance(atkStat, defStat);
            land *= 1f - victim.CcResist;
            if (_rng.NextDouble() < land)
            {
                ApplyBuff(victim, def, lvl);
                BroadcastCombat(attacker, victim, 0, CombatOutcome.Buff, name);
            }
        }
        if (victim.Hp <= 0 && !victim.Dead)
            Kill(victim, attacker);
    }

    /// <summary>Hostiles of the caster within a radius: for a MOB caster that's nearby players;
    /// for a PLAYER caster it's nearby mobs. Used by enemy-AoE skills (boss slams).</summary>
    private IEnumerable<Entity> EnemiesInRadius(Entity caster, float radius)
    {
        float r2 = radius * radius;
        var wantKind = caster.Kind == EntityKind.Mob ? EntityKind.Player : EntityKind.Mob;
        foreach (var e in _world.Grid.Nearby(caster))
        {
            if (e.Kind != wantKind || e.Dead || e.TrainingDummy)
                continue;
            if (wantKind == EntityKind.Player && GameConstants.InSafeZone(e.X, e.Y))
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

    private void TickPotion(Entity entity)
    {
        bool changed = false;

        if (entity.PotionCooldown > 0)
        {
            entity.PotionCooldown--;
            if (entity.PotionCooldown == 0)
                changed = true;
        }

        // The potion heal-over-time is an ordinary buff now, so TickBuffs/TickHealOverTime run it —
        // there is no separate potion effect channel to tick any more. Only the shared drink
        // cooldown above is still potion-specific.
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

    private void MobAi(Entity mob)
    {
        if (mob.TrainingDummy) return;   // stationary, never wanders or aggroes
        if (mob.DetauntTicks > 0) mob.DetauntTicks--;
        if (mob.TauntLockTicks > 0) mob.TauntLockTicks--;

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
                    candidate.Stealthed ||
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
        mob.Threat.Clear();
        mob.TauntLockTicks = 0;
        mob.TargetX = mob.HomeX;
        mob.TargetY = mob.HomeY;

        // Boss: drop enrage (undo its stat spike) and reset the combat/skill timers so the next
        // pull starts fresh.
        if (mob.Enraged)
        {
            mob.AttackPower = (int)(mob.AttackPower / 1.5f);
            mob.MagicAttack = (int)(mob.MagicAttack / 1.5f);
            mob.BasicAttackPower = (int)(mob.BasicAttackPower / 1.5f);
            mob.AttackSpeedMultiplier /= 0.7f;
            mob.Enraged = false;
        }
        mob.CombatTicks = 0;
        mob.BossSkillCooldown = 0;
        mob.BossPhaseIndex = 0;       // re-arm the phase script for the next pull
        mob.SkillCooldowns.Clear();   // fresh boss-skill reuse on a leash reset
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

    /// <summary>MP-cost multiplier from the caster's MP-cost-reduction buffs — PHYSICAL-category
    /// skills use the physical reduction, everything else (magic/buff/heal) the magic one.</summary>
    private static float MpCostFactor(Entity caster, SkillDef def) =>
        1f - (def.Category == SkillCategory.Physical
            ? caster.PhysMpCostReduction
            : caster.MagicMpCostReduction);

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
        // Cast time = base ticks scaled by the speed model (lower multiplier = faster).
        // PHYSICAL skills scale by ATTACK speed (DEX + weapon), not cast speed — a fighter
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

        // Charge the initial MP portion up front (default 0; split skills charge
        // some now, the rest on completion). Level-aware MP cost, reduced by MP-cost buffs.
        int initialMp = (int)(def.InitialMpAt(Math.Max(1, caster.SkillLevelOf(def.Id))) * MpCostFactor(caster, def));
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
        int finishMp = (int)(def.FinishMpAt(lvl) * MpCostFactor(caster, def));

        if (caster.Mp < finishMp)
        {
            SendSystemToEntity(caster, "Not enough MP.");
            CancelCast(caster);
            return;
        }

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
        if (def.HpCost > 0) caster.Hp = Math.Max(1, caster.Hp - def.HpCost);   // Restore Spirit: HP→MP
        caster.CastInitialMpPaid = 0;
        // Reuse-delay reduction (Spell Mastery / buffs) shortens the cooldown — unless the skill
        // has a FIXED cooldown (Return, ultimates).
        int cooldown = def.CooldownTicks;
        if (cooldown > 0 && !def.FixedCooldown && caster.CooldownReduction > 0f)
            cooldown = Math.Max(1, (int)(cooldown * (1f - caster.CooldownReduction)));
        caster.SkillCooldowns[def.Id] = cooldown;

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

        // ---- Stealth: go invisible to mob AI. Coexists with any self-buff on the same skill;
        //      broken early by taking an offensive action (see AfterOffensiveSkill / basic attack). ----
        if (def.GrantsStealth)
        {
            caster.StealthTicks = Math.Max(1, def.DurationTicks);
            DropAggroOn(caster);   // vanishing sheds mobs already locked on
            BroadcastCombat(caster, caster, 0, CombatOutcome.Buff, castName);
            SendSystemToEntity(caster, "You slip into the shadows.");
        }

        // ---- Offensive AoE (boss slam): hit every hostile in radius, then finish. Uses the
        //      compact hit path (shared with traps). ----
        if (def.TargetMode == TargetMode.EnemiesInRadius)
        {
            foreach (var foe in EnemiesInRadius(caster, def.AreaRadius).ToList())
                DeliverSimpleHit(caster, foe, def, lvl, castName);
            return;
        }

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

                // BLOW skills (dagger Stab) land full damage only on a crit/double, else a soft
                // 10% floor. "[Double]" skills roll a ×2 from the higher of DEX/ATK (cap 30%);
                // ordinary skills keep the basic crit path (unchanged).
                var (finalDmg, outcome) = def.BlowOnCrit
                    ? ResolveBlow(caster, target, damage, def)
                    : def.CanDouble
                        ? ResolvePhysicalDouble(caster, target, damage,
                            StatCalculator.PhysicalDoubleChance(Math.Max((int)caster.EffectiveDex, caster.AtkStat)),
                            def.BlockAccuracy)
                        : ResolvePhysicalCritAndBlock(
                            caster, target, damage, caster.CritChance, def.BlockAccuracy);
                damage = finalDmg;
                BroadcastCombat(caster, target, damage, outcome, castName);
                ApplyDamage(target, damage, caster);
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
                (int)target.EffectiveMagicDefence);   // magic channel: divides by mDef
            damage = (int)(damage * StatCalculator.WeaponVariance(caster.WeaponType, _rng));
            damage = FinalizeDamage(caster, target, damage, DamageKind.SkillMagic, def);

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
                ApplyDamage(target, damage, caster);
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
                ApplyDamage(target, damage, caster);
                TryInterruptCast(target, magicInterrupt);
            }

            // Vampiric: heal the caster for a fraction of the magic damage dealt
            // (the skill's own Lifesteal plus any Spell Vamp buff).
            float spellVamp = def.Lifesteal + caster.SpellVamp;
            if (spellVamp > 0f && damage > 0)
            {
                int leech = (int)(damage * spellVamp);
                if (leech > 0) HealOne(caster, caster, leech, 0, castName);   // lifesteal = a FLAT heal
            }
        }

        // ---- Heal (single ally/self, or AoE to allies in radius) ----
        // TWO halves: a FLAT part driven by the healer's M.Atk (so a caster weapon heals hard and a
        // damage weapon doesn't), plus an optional % of the TARGET's max HP that ignores M.Atk and
        // ignores heal-reduction. See HealOne.
        if (effect.HasFlag(SkillEffect.Heal))
        {
            // Divine Focus: healing OUTPUT scaled down when the healer has no magic weapon (×0.5 / ×0.75).
            int healPower = def.Id == SkillCatalog.TestHeal ? _testHealPower : def.PowerAt(lvl);   // test heal: live debug power
            int flat = (int)(SkillMath.HealAmount(healPower, caster.HealPowerFlat, caster.HealPowerMod) * caster.HealOutputMult);
            float pct = def.MagnitudeOf(SkillEffect.Heal, ModifierMode.Percent, lvl);
            if (def.TargetMode == TargetMode.AlliesInRadius)
                foreach (var ally in PlayersInRadius(caster, def.AreaRadius))
                    HealOne(caster, ally, flat, (int)(ally.MaxHp * pct), castName);
            else
                HealOne(caster, target, flat, (int)(target.MaxHp * pct), castName);
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
                    Dispel(caster, ally, def, positive: false, castName);
            else
                Dispel(caster, target, def, positive: false, castName);
        }

        // ---- Cancel / Dispel — strip POSITIVE buffs from an enemy (DispelCount = random N). ----
        if (effect.HasFlag(SkillEffect.Cancel))
        {
            offensive = true;
            Dispel(caster, target, def, positive: true, castName);
        }

        // ---- Crowd control + DoT (Slow/Stun/Fear/Root, Bleed/Poison/Venom) — lands via the
        //      contest (docs/Disciplines.md), NOT the fizzle model. Bosses are immune. The
        //      attacker stat is DEX for bleed/venom, ATK otherwise; defender CON (phys) / WIT (magic). ----
        if ((effect & SkillEffect.ContestCc) != 0)
        {
            offensive = true;
            bool dexBased = (effect & (SkillEffect.Bleed | SkillEffect.Venom)) != 0;
            int atkStat = dexBased ? (int)caster.EffectiveDex : caster.AtkStat;
            int defStat = def.DebuffSchool == DebuffSchool.Magical ? (int)target.EffectiveWit : target.Con;
            float land = target.Immune ? 0f : StatCalculator.DebuffLandChance(atkStat, defStat);
            land *= 1f - target.CcResist;   // gear/buff CC resistance lowers the land chance
            if (_rng.NextDouble() < land)
            {
                if ((effect & SkillEffect.AnyDot) != 0)
                    ApplyDotStack(caster, target, def, lvl);   // stacking DoT (refresh on reapply)
                else
                    ApplyBuff(target, def, lvl);               // single CC buff
                BroadcastCombat(caster, target, 0, CombatOutcome.Buff, castName);
            }
            else
            {
                BroadcastCombat(caster, target, 0, CombatOutcome.Fail, castName);  // resisted
            }
        }

        // ---- Debuffs (defence curse / anti-heal / root) — can fizzle like a spell ----
        //      (Contested CC above is excluded so it doesn't double-resolve.)
        if ((effect & SkillEffect.AnyDebuff & ~SkillEffect.ContestCc) != 0)
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

        // ---- De-taunt — shed the caster's aggro from nearby foes ----
        if (effect.HasFlag(SkillEffect.Detaunt))
            Detaunt(caster);

        // ---- Taunt — force a mob's aggro onto the caster: spike threat above the current
        //      top and lock it briefly so it commits to the tank. ----
        if (effect.HasFlag(SkillEffect.Taunt) && target.Kind == EntityKind.Mob)
        {
            offensive = true;
            float top = target.Threat.Count > 0 ? target.Threat.Values.Max() : 0f;
            target.Threat[caster.Id] = top * 1.2f + 100f;
            target.CombatTargetId = caster.Id;
            target.Engaged = true;
            target.TauntLockTicks = 30;   // ~3s committed to the taunter
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

        // ---- Beneficial buffs (any of the buff flags, or a pure marker buff like Angel's Protection) ----
        if ((effect & SkillEffect.AnyBuff) != 0 || def.KeepsBuffsOnDeath)
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
    private void ApplyBuff(Entity target, SkillDef def, int level = 1, string? displayName = null,
        bool refresh = true, bool toggle = false, int maxStacks = -1)
    {
        string key = string.IsNullOrEmpty(def.BuffKey) ? def.Name : def.BuffKey;
        string shownName = string.IsNullOrEmpty(displayName) ? def.Name : displayName!;
        int eff = maxStacks >= 0 ? maxStacks : def.EffectiveMaxStacks;

        // Stacking effect (MaxStacks > 1): reapplying ADDS a stack (capped) and refreshes,
        // rather than replacing. If the skill has a per-stack table, the status re-snapshots
        // that level's Effect + Magnitudes (so a slow can grow, or become a freeze at stack N).
        if (eff > 1 && target.Buffs.FirstOrDefault(b => b.Key == key) is BuffInstance stack)
        {
            stack.Stacks = Math.Min(eff, stack.Stacks + 1);
            stack.MaxStacks = eff;
            stack.TicksRemaining = toggle ? int.MaxValue : def.DurationTicks;   // refresh
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
            return;
        }

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

        // A leveled-stack effect starts at stack 1's entry; otherwise the skill's own effect.
        var first = def.StackLevelAt(1);
        target.Buffs.Add(new BuffInstance
        {
            Effect = first?.Effect ?? def.Effect,
            Magnitudes = first?.Magnitudes ?? def.MagnitudesAt(level) ?? Array.Empty<EffectMagnitude>(),
            TicksRemaining = toggle ? int.MaxValue : def.DurationTicks,
            Toggle = toggle,
            // DoT damage effect (bleed/poison/venom): carries its per-tick damage so TickDots
            // hits for DotPower each second. Damage does NOT stack — stacks live on a separate
            // counter (see ApplyDotStack); the burst reads the counter, not this.
            DotPower = (def.Effect & SkillEffect.AnyDot) != 0 ? def.PowerAt(level) : 0,
            // Absorb shield: flat Power + a % of the target's max HP (a Percent Shield magnitude).
            ShieldPool = (def.Effect & SkillEffect.Shield) != 0
                ? def.PowerAt(level) + (int)(target.MaxHp * def.MagnitudeOf(SkillEffect.Shield, ModifierMode.Percent, level))
                : 0,
            MaxStacks = eff,
            Cancellable = def.Cancellable,
            SourceRow = def.BuffRow,   // which buff-bar row this lands in (debuffs override it)
            SourceSkillId = def.Id,    // so the buff bar can show the skill's icon
            Name = shownName,
            Key = key,
            Rank = def.Rank,
            Replaces = def.Replaces ?? Array.Empty<string>(),
            PhysMpCostPct = def.PhysMpCostPct,
            MagicMpCostPct = def.MagicMpCostPct,
            KeepsBuffsOnDeath = def.KeepsBuffsOnDeath,
            AutoResurrect = def.AutoResurrect,
            Description = SkillCatalog.DescriptionOf(def.Id)
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
    }

    /// <summary>Apply a damage-over-time. Two SEPARATE statuses (the L2 split): (1) the bleed
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

    /// <summary>Restore one target's MP and broadcast it (mirrors HealOne for the MP
    /// channel; used by MP Restore skills).</summary>
    private void RestoreMpOne(Entity caster, Entity target, int amount, string skillName)
    {
        if (target.Dead) return;
        if (amount > 0)
        {
            amount += target.RestoreMpBonus;   // nuker robe mastery "mpWhenRestored"
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
    /// (0 = all matching; N = up to N at random). Skips Internal and non-Cancellable effects.</summary>
    private void Dispel(Entity caster, Entity target, SkillDef def, bool positive, string skillName)
    {
        if (target.Dead) return;
        SkillEffect mask = def.DispelMask;
        var cands = target.Buffs.Where(b =>
            !b.Internal && b.Cancellable &&
            (positive ? !b.IsDebuff : b.IsDebuff) &&
            (mask == SkillEffect.None || (b.Effect & mask) != 0) &&
            (def.DispelMaxLevel <= 0 || b.Rank <= def.DispelMaxLevel)).ToList();

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
        e.X = Math.Clamp(x, 0f, GameConstants.ZoneWidth);
        e.Y = Math.Clamp(y, 0f, GameConstants.ZoneHeight);
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
        caster.StealthTicks = 0;   // any offensive action breaks stealth

        // Mages don't fall back to melee auto-attack after a spell: chasing the mob
        // to swing a staff is never what a nuker/healer wants. Fighters still engage
        // so a melee skill flows into auto-attacks.
        if (!target.Dead && caster.BaseClass != BaseClass.Mage)
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

    /// <summary>Point the mob at its highest-threat living target (stale/dead entries skipped).</summary>
    private void RetargetByThreat(Entity mob)
    {
        Guid? best = null; float bestV = -1f;
        foreach (var (id, v) in mob.Threat)
        {
            if (v <= bestV) continue;
            if (_world.Entities.TryGetValue(id, out var e) && !e.Dead) { bestV = v; best = id; }
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
        if (entity.Kind == EntityKind.Mob)
            ResetMob(entity);
    }

    private void ResolveBasicAttack(Entity attacker, Entity target)
    {
        attacker.StealthTicks = 0;   // attacking breaks stealth

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

            var (finalDmg, outcome) = ResolvePhysicalCritAndBlock(
                attacker, target, damage, attacker.CritChance, 0f);
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
                    reflected = ApplyDamage(attacker, reflected, target);
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

    /// <summary>Apply damage unless the target is in god mode.</summary>
    private int ApplyDamage(Entity target, int damage, Entity? attacker = null)
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
        if (pvp)
        {
            target.LastPvpAttackerId = attacker!.Id;
            if (FlagOf(target) != PvpFlag.Pk)
                attacker.PvpFlagUntilTick = _tick + PvpFlagTicks;
        }

        // Threat: damage to a mob from a known attacker builds aggro (retargets to top threat).
        if (attacker is not null && target.Kind == EntityKind.Mob && damage > 0)
            AddThreat(target, attacker, damage);

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
        if (target.TrainingDummy && target.Hp < 1) target.Hp = 1;

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
        // Angel's Protection (noblesse): if a "keeps buffs on death" buff is up, death removes ONLY the
        // protection buff(s) and every other buff survives; otherwise death clears all buffs as usual.
        // Preservation buffs share BuffKey "buff_preservation" + Rank, so only the strongest is ever held —
        // one is consumed here. A consumed buff flagged AutoResurrect also auto-revives (handled below).
        bool keptBuffs = victim.Buffs.Any(b => b.KeepsBuffsOnDeath);
        bool autoRes = victim.Buffs.Any(b => b.KeepsBuffsOnDeath && b.AutoResurrect);
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
            if (killer.Kind == EntityKind.Player)
            {
                AwardKillExp(killer, victim);
                RollDrop(killer, victim);   // loot still goes to the killer (loot rules deferred)
                // Kill-quest credit for the killer + every party member in range.
                foreach (var m in KillCreditMembers(killer))
                    AdvanceKillQuests(m, victim);
                // A PK works off karma by grinding — each mob kill sheds a little.
                ReduceKarma(killer, _karmaLossPerMob);
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
                StopAutoHunt(victim, "you were defeated.", locked: false);

            // Auto-resurrect (future tank self-res / healer target-auto-res): the consumed preservation buff
            // stands the player back up at 30% HP/MP in place, no prompt. The death penalty above still
            // applied ("you die, you have the penalty"). Buffs already survived (keptBuffs).
            if (autoRes)
                ResurrectTarget(victim, victim, 0f);
        }
    }

    private void RollDrop(Entity killer, Entity mob)
    {
        if (mob.MobTypeId is null)
            return;

        // In-range kill-credit members (killer + party members within share range). Solo = [killer].
        var eligible = KillCreditMembers(killer);
        _world.Parties.TryGetValue(killer.Id, out var party);

        // Gold ALWAYS splits evenly among in-range members regardless of loot mode; the killer takes
        // the remainder. Solo = it all goes to the killer. (Level x rate, +/-20% variance.)
        int gold = (int)(StatCalculator.MobGoldReward(mob.Level) * RateConfig.GoldAmountRate
            * (0.8f + (float)_rng.NextDouble() * 0.4f));
        if (gold > 0)
            AwardGold(killer, eligible, gold);

        var mobType = MobCatalog.Get(mob.MobTypeId);
        if (mobType.Drops is null || mobType.Drops.Length == 0)
            return;

        // Only entries valid at this mob's level. Independent entries roll on their own; entries
        // sharing a GroupId > 0 form a mutually-exclusive group (roll once, pick one weighted).
        var applicable = mobType.Drops.Where(e => e.AppliesAtLevel(mob.Level)).ToList();

        // Everyone who received something this kill (refresh their inventory once at the end).
        var touched = new HashSet<Entity>();
        void Award(DropEntry entry)
        {
            if (ItemCatalog.Get(entry.ItemId) is not ItemDef def)
                return;
            int qty = _rng.Next(entry.MinQty, entry.MaxQty + 1);
            qty = Math.Max(1, (int)(qty * RateConfig.DropAmountRate));
            // The recipient is chosen PER ITEM so RoundRobin/Random spread across the party.
            var to = LootRecipient(killer, eligible, party);
            if (!AddItem(to, def.Id, qty))
            {
                SendSystemToEntity(to, $"{mob.Name} dropped {def.Name} — inventory full!");
                return;
            }
            string qtyLabel = qty > 1 ? $" x{qty}" : "";
            SendSystemToEntity(to, $"You looted: {def.Name}{qtyLabel} [{def.Grade}/{def.Rarity}]");
            // Let the rest of the in-range party see where it went.
            if (eligible.Count > 1)
                foreach (var m in eligible)
                    if (m.Id != to.Id)
                        SendSystemToEntity(m, $"{to.Name} looted {def.Name}{qtyLabel}.");
            touched.Add(to);
        }

        // Independent entries (GroupId == 0): each its own rate-scaled roll.
        foreach (var entry in applicable.Where(e => e.GroupId == 0))
        {
            float chance = Math.Min(1f, entry.Chance * RateConfig.DropChanceRate);
            if (_rng.NextDouble() <= chance)
                Award(entry);
        }

        // Drop groups (GroupId > 0): roll once at the summed chance, then pick one weighted member.
        foreach (var group in applicable.Where(e => e.GroupId != 0).GroupBy(e => e.GroupId))
        {
            var members = group.ToList();
            float total = Math.Min(1f, members.Sum(e => e.Chance) * RateConfig.DropChanceRate);
            if (_rng.NextDouble() > total)
                continue;
            // Weighted pick within the group (weights = the raw member chances).
            double weightSum = members.Sum(e => (double)e.Chance);
            double pick = _rng.NextDouble() * weightSum;
            foreach (var e in members)
            {
                pick -= e.Chance;
                if (pick <= 0) { Award(e); break; }
            }
        }

        // Boss/elite pile goes to ONE recipient per the loot rule (mats stay together).
        var bossTo = LootRecipient(killer, eligible, party);
        if (RollBossBonus(bossTo, mob, mobType))
            touched.Add(bossTo);

        foreach (var t in touched)
            SendInventory(t);
    }

    /// <summary>Split gold evenly among in-range members; the killer keeps the remainder. Solo (or a
    /// single eligible member) = the killer takes it all. Gold ignores the party's item loot mode.</summary>
    private void AwardGold(Entity killer, List<Entity> eligible, int gold)
    {
        if (eligible.Count <= 1)
        {
            killer.Gold += gold;
            SendGold(killer);
            return;
        }
        int each = gold / eligible.Count;
        int remainder = gold - each * eligible.Count;
        foreach (var m in eligible)
        {
            m.Gold += each + (m.Id == killer.Id ? remainder : 0);
            SendGold(m);
        }
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
    /// at the finished tiered set piece — bosses are the reliable gear/mat source (docs/Crafting.md).</summary>
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

        // A chance at a gear body by family: a BOSS drops the finished Epic SET piece; an ELITE
        // drops the weaker scaled Rare copy (the full set stays a boss/craft goal).
        string weight = mobType.Category switch
        {
            MobCategory.Undead or MobCategory.Angel or MobCategory.MagicCreature => "robe",
            MobCategory.Animal or MobCategory.Plant or MobCategory.Insect => "light",
            _ => "heavy",
        };
        if (boss)
        {
            if (_rng.NextDouble() < 0.5) AddItem(recipient, $"{weight}_t{tier}");
            // A-grade (76) bosses can drop a DropOnly recipe BOOK for the tier's set body.
            if (tier >= 76 && _rng.NextDouble() < 0.10)
                AddItem(recipient, ItemCatalog.RecipeBookId($"craft_{weight}_t{tier}"));
        }
        else if (_rng.NextDouble() < 0.20)
        {
            AddItem(recipient, $"{weight}_t{tier}_rare");
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

    /// <summary>EXP for killing one mob: the level curve scaled by how TOUGH this particular
    /// mob actually is. L2 pays XP by toughness within a level band — a level-85 Drake Warrior
    /// carries ~8.5x a normal same-level mob's HP and pays ~7.5x the EXP. We paid purely by
    /// LEVEL, so a boss with 10x the HP gave exactly as much EXP as the trash standing next to
    /// it. Toughness is read straight off the spawned mob (its rank multiplier and MobMod HP
    /// passive are already baked into MaxHp) relative to the level's base curve, so a mob that
    /// buys bulk with an "HP Increase (2x)" passive automatically pays 2x.</summary>
    private static int MobExpValue(Entity mob)
    {
        float toughness = Math.Clamp(mob.MaxHp / (float)Math.Max(1, MobBaseStats.Hp(mob.Level)), 0.25f, 20f);
        return Math.Max(1, (int)(StatCalculator.MobExpReward(mob.Level) * toughness));
    }

    /// <summary>Award a mob kill's EXP: solo → all to the killer; party → split among members in
    /// range, weighted by level (anti-leech), with a small size bonus to reward grouping.</summary>
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
                p.Level, p.Exp, StatCalculator.ExpToNext(p.Level), false));
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

    /// <summary>Offer a resurrection to a fallen player: they see a confirm prompt (who revived them + how
    /// much exp comes back) and must ACCEPT before actually reviving — so they don't stand up on top of the
    /// mob that killed them. Generic on caster==target, so a future self-res reuses this same pipe.</summary>
    private void OfferResurrect(Entity caster, Entity target, float expPct)
    {
        if (target is null || target.Kind != EntityKind.Player || !target.Dead) return;
        expPct = Math.Clamp(expPct, 0f, 1f);
        target.PendingResFromId = caster.Id;
        target.PendingResExpPct = expPct;
        target.PendingResTicks = ResurrectOfferTicks;
        long wouldRestore = (long)(target.LostExp * expPct);
        if (_world.EntityToConnection.TryGetValue(target.Id, out var conn))
            _ = _hub.Clients.Client(conn).SendAsync("ResurrectOffer",
                new ResurrectOffer(caster.Name, expPct, wouldRestore));
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
                    target.Level, target.Exp, StatCalculator.ExpToNext(target.Level), false));
        }
        else
        {
            SendSystemToEntity(target, "You have been resurrected.");
        }
        if (target != caster) SendSystemToEntity(caster, "You resurrect a fallen ally.");
        if (target.Kind == EntityKind.Player) SendStats(target);
    }

    private void AwardKillExp(Entity killer, Entity victim)
    {
        int total = MobExpValue(victim);
        // Only members within PartyExpMaxLevelGap levels of the KILLER share the exp; anyone further
        // out earns a flat zero (owner, 2026-07-17 — anti-powerlevelling). Filtering BEFORE the split
        // means the in-band members divide the whole reward and the out-of-band member doesn't dilute
        // the size bonus either. The killer is always in his own band. Kill-QUEST credit is untouched.
        var share = KillCreditMembers(killer)
            .Where(m => Math.Abs(m.Level - killer.Level) <= GameConstants.PartyExpMaxLevelGap)
            .ToList();
        if (share.Count <= 1)
        {
            AwardExp(killer, total);
            return;
        }
        float bonus = 1f + 0.10f * (share.Count - 1);   // grouping incentive (retune later)
        long levelSum = share.Sum(m => (long)m.Level);
        foreach (var m in share)
        {
            int amt = (int)(total * bonus * ((float)m.Level / levelSum));
            if (amt > 0) AwardExp(m, amt);
        }
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

        bool stackable = def.Slot is EquipSlot.Consumable or EquipSlot.Scroll or EquipSlot.Material;
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
        if (rollAttributes && def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel)
            newItem.Attributes = def.FixedAttributes is { Length: > 0 } fixedAttrs
                ? fixedAttrs.ToList()                    // legendary one-off
                : AttributeSystem.Roll(def, _rng);       // normal random roll
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

        if (ItemCatalog.Get(item.DefId) is ItemDef def &&
            def.Slot is EquipSlot.Consumable or EquipSlot.Scroll or EquipSlot.Material)
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
            float dx = e.X - caster.X, dy = e.Y - caster.Y;
            if (dx * dx + dy * dy <= r2)
                yield return e;
        }
    }

    private void PushBuffs(Entity player)
    {
        var dtos = player.Buffs.Where(b => !b.Internal).Select(b => new BuffDto(
            b.Name, b.Description,
            b.Toggle ? -1f : b.TicksRemaining * GameConstants.TickSeconds, b.IsDebuff, b.Key, b.Stacks,
            b.Row, BuffIcon(player, b.SourceSkillId))).ToList();

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
    /// belongs to a class you swapped away from), then park any newly-learned ACTIVE skill in the first
    /// free slot. It never MOVES a skill the player placed — the bar is their layout, not ours.
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

        // Forget what this class no longer knows — but NEVER an item slot ("item:<defId>"): it isn't a
        // learned skill, so it would otherwise be wiped here, yet it's a perfectly valid bar entry the
        // player placed on purpose (a potion / return scroll they want one click away).
        for (int i = 0; i < slots.Length; i++)
            if (!string.IsNullOrEmpty(slots[i]) && !GameConstants.IsItemSlot(slots[i])
                && !p.LearnedSkills.ContainsKey(slots[i]))
                slots[i] = "";

        // Park anything newly learned. Ordinal order so it is deterministic, not hash order.
        var placed = slots.Where(s => !string.IsNullOrEmpty(s)).ToHashSet();
        foreach (var id in p.LearnedSkills.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (placed.Contains(id)) continue;
            if (SkillCatalog.Get(id) is not { } def || def.Category == SkillCategory.Passive)
                continue;   // passives are always on; they never take a bar slot
            int free = Array.IndexOf(slots, "");
            if (free < 0) break;
            slots[free] = id;
            placed.Add(id);
        }

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
            p.Con, p.AtkStat, p.EffectiveWit, p.EffectiveDex,
            p.MaxHp, p.MaxMp, (int)p.EffectiveAttack, (int)p.EffectiveDefence,
            p.Accuracy, (int)p.EffectiveEvasion, p.CritChance, p.BasicAttackRange, p.SecondClass,
            p.EffectiveSpeed, SkillMath.CastModifier(p.Wit), p.EffectiveCastSpeedMultiplier, p.EffectiveAttackSpeedMultiplier, p.SkillPoints, p.MoveState, (int)p.EffectiveMagicAttackShown, p.MagicCritChance,
            p.HasShield, p.BlockChance, p.BlockReduction, p.ShieldDefense, (int)p.EffectiveMagicDefence,
            p.ActiveArmorSet, p.ArmorMasteryLabel,
            hpReg, mpReg, p.CritDamageBonus,
            p.MeleeVamp, p.SpellVamp, p.CooldownReduction,
            p.MagicFailResist, p.MagicFailFloor,
            p.CritRateResist, p.CritDmgResist, p.BowResist,
            p.InterruptResist, (int)p.EffectiveMagicAttack,   // MagicAttackInternal: the cosmic L2-reference value
            p.HealPowerFlat, p.HealPowerMod, p.HealReceivedFlat, p.HealReceivedMod));
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
        SendSystemToEntity(player, $"Learned recipe: {outName}. (Requires the {recipe.Profession} profession to craft.)");
        SendInventory(player);
        SaveEntity(player);
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
                .Select(e => new SelectionOption(e.ItemId, ItemCatalog.Get(e.ItemId)?.Name ?? e.ItemId))
                .ToArray();
            SendTo(player, "Selection", new SelectionOffer(item.InstanceId, def.Name, options, box.PickCount));
            return;
        }

        // Consume one box (frees a slot for the loot).
        if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);

        var got = new List<string>();
        bool full = false;
        foreach (var entry in box.Entries)
        {
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
    }

    /// <summary>Player confirmed their picks from a SELECTION box: validate the chosen
    /// ids against the box's options (up to PickCount), consume the box, grant them.</summary>
    private void HandleSelectBoxItems(SelectBoxItemsCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player)) return;
        var item = player.Inventory.FirstOrDefault(i => i.InstanceId == cmd.InstanceId);
        if (item is null || item.Equipped) return;
        if (ItemCatalog.Get(item.DefId) is not ItemDef def || def.Slot != EquipSlot.Box) return;
        if (BoxCatalog.Get(item.DefId) is not BoxDef box || box.PickCount <= 0) return;

        var optionIds = box.Entries.Select(e => e.ItemId).ToHashSet();
        var chosen = cmd.ItemIds.Distinct().Where(optionIds.Contains).Take(box.PickCount).ToList();
        if (chosen.Count == 0)
        {
            SendSystemToEntity(player, "Select at least one item.");
            return;
        }

        // Consume one box, then grant the chosen items.
        if (item.Quantity > 1) item.Quantity--; else player.Inventory.Remove(item);

        var got = new List<string>();
        foreach (var id in chosen)
        {
            if (AddItem(player, id, 1, rollAttributes: true))
                got.Add(ItemCatalog.Get(id)?.Name ?? id);
            else { SendSystemToEntity(player, "Your inventory is full — some picks were lost."); break; }
        }
        SendInventory(player);
        SaveEntity(player);
        SendSystemToEntity(player, got.Count > 0
            ? $"{def.Name}: {string.Join(", ", got)}."
            : $"{def.Name}: nothing chosen.");
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
        string[] passives = isMob && t.MobTypeId is not null
            && MobCatalog.Get(t.MobTypeId).Mod is MobMod mod
                ? mod.Describe().ToArray()
                : Array.Empty<string>();

        // Active temporary effects on the target — including DoT stack counters (so the
        // attacker can read "Bleed x5" on the enemy and time a burst).
        var effects = t.Buffs
            .Select(b => b.Stacks > 1 ? $"{b.Name} x{b.Stacks}" : b.Name)
            .ToArray();

        // A mob's level-appropriate DROPS (behind the client's [Details] button). Chance shown is the
        // EFFECTIVE one (after the global drop-rate), so it matches what the player will actually see.
        string[]? drops = null;
        if (isMob && t.MobTypeId is not null && MobCatalog.Get(t.MobTypeId).Drops is { } table)
            drops = table
                .Where(d => d.AppliesAtLevel(t.Level))
                .Select(d =>
                {
                    float chance = Math.Min(1f, d.Chance * RateConfig.DropChanceRate);
                    string name = ItemCatalog.Get(d.ItemId)?.Name ?? d.ItemId;
                    string qty = d.MaxQty > 1 ? $" x{d.MinQty}-{d.MaxQty}" : "";
                    return $"{name}{qty}  ({chance * 100:0.#}%)";
                })
                .ToArray();

        SendTo(player, "TargetDetails", new TargetDetails(
            t.Id, t.Name, t.Level, isMob,
            t.Hp, t.MaxHp, t.Mp, t.MaxMp,
            t.AttackPower, (int)t.EffectiveMagicAttackShown,   // shrunk display, matches the stats window
            (int)t.EffectiveDefence, (int)t.EffectiveMagicDefence,
            t.Accuracy, t.Evasion, t.CritChance,
            t.BowResist, t.CritRateResist,
            passives, effects, drops));
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

    /// <summary>Resolution for a "[Double]" physical SKILL: a ×2 chance from the higher of
    /// the caster's DEX/ATK (cap 30%), lowered by shield/crit-rate resist and ignoring the
    /// block on a double (like a crit); otherwise a normal block roll. Skills without the
    /// [Double] flag never reach here (they use the basic crit path, unchanged).</summary>
    private (int damage, CombatOutcome outcome) ResolvePhysicalDouble(
        Entity attacker, Entity target, int baseDamage, float doubleChance, float blockAccuracy)
    {
        if (attacker.WeaponType == WeaponType.Bow && target.BowResist > 0f)
            baseDamage = Math.Max(1, (int)(baseDamage * (1f - target.BowResist)));

        float eff = Math.Clamp(doubleChance
            - (target.HasShield ? target.ShieldCritDefense : 0f)
            - target.CritRateResist, 0f, 1f);
        if (doubleChance > 0f && _rng.NextDouble() < eff)
        {
            // ×2 = +100% over normal, trimmed by the target's crit-damage resist.
            float extra = 1f * (1f - target.CritDmgResist);
            return (Math.Max(1, (int)(baseDamage * (1f + extra))), CombatOutcome.Crit);
        }

        if (target.HasShield)
        {
            float effBlock = Math.Clamp(target.BlockChance - blockAccuracy, 0f, StatCaps.BlockChance);
            if (_rng.NextDouble() < effBlock)
                return (Math.Max(1, (int)(baseDamage * (1f - target.BlockReduction))), CombatOutcome.Block);
        }

        return (baseDamage, CombatOutcome.Hit);
    }

    /// <summary>Resolution for a BLOW skill (dagger Stab). CRIT is the gate: the blow deals
    /// its FULL "actual" damage only if it crits (dagger crit chance, lowered by shield/crit
    /// resist). ONLY after a landed crit does it roll a DOUBLE (chance from the higher of
    /// DEX/ATK) that multiplies the actual damage ×2. A blow that FAILS to crit deals a flat
    /// BlowFailFraction of its damage — that floor can neither crit nor double (a soft floor,
    /// not L2's 0-damage whiff). Blows bypass shields, so the floor isn't blocked.</summary>
    private (int damage, CombatOutcome outcome) ResolveBlow(
        Entity attacker, Entity target, int baseDamage, SkillDef def)
    {
        float effCrit = Math.Clamp(attacker.CritChance
            - (target.HasShield ? target.ShieldCritDefense : 0f)
            - target.CritRateResist, 0f, 1f);

        if (_rng.NextDouble() >= effCrit)
            // Missed the crit: soft floor only — cannot crit or double.
            return (Math.Max(1, (int)(baseDamage * def.BlowFailFraction)), CombatOutcome.Hit);

        // Crit landed → deal the full actual damage, THEN roll a separate double on top.
        int damage = baseDamage;
        if (def.CanDouble)
        {
            float dbl = Math.Clamp(
                StatCalculator.PhysicalDoubleChance(Math.Max((int)attacker.EffectiveDex, attacker.AtkStat))
                - (target.HasShield ? target.ShieldCritDefense : 0f)
                - target.CritRateResist, 0f, 1f);
            if (_rng.NextDouble() < dbl)
                damage = Math.Max(1, (int)(damage * (1f + (1f - target.CritDmgResist))));  // ×2, trimmed by crit-dmg resist
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
        float result = dmg * (1f + bonus) * (1f + condBonus) * skillMult * raidMult;
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

    /// <summary>The skills a reset NPC can un-learn: the ones you committed to permanently (any
    /// skill with an ExclusiveGroup — today, the level-40 stat swaps). Includes the gold you sank
    /// into it, so the player can see exactly what he's writing off.</summary>
    private static IEnumerable<ResettableSkill> ResettableSkillsOf(Entity player)
    {
        foreach (var (id, level) in player.LearnedSkills)
        {
            if (SkillCatalog.Get(id) is not SkillDef def || string.IsNullOrEmpty(def.ExclusiveGroup))
                continue;
            int spent = 0;
            for (int l = 1; l <= level; l++) spent += def.GoldCostAt(l);
            yield return new ResettableSkill(id, def.Name, level, spent);
        }
    }

    /// <summary>Un-learn a permanent, mutually-exclusive skill so its group is free to commit to
    /// again. Removing costs NOTHING — but the gold already spent is NOT refunded. That's the whole
    /// deal: you may change your mind, you may not undo the price of being wrong.</summary>
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
    private const long BuffCostPerLevel = 3_000;   // per buff, per buff-LEVEL, when 41-75
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

    /// <summary>Lay the full NPC buff set on a player. Shared by the buffer NPC and the debug button —
    /// the ONLY difference between them is the NPC's level gate, which is exactly the point.</summary>
    private void GrantFullBuffSet(Entity player)
    {
        foreach (var id in SkillCatalog.NewbieBuffSet)
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
    /// the balance numbers the owner signs off on are BUFFED numbers.</summary>
    private void HandleDebugBuff(DebugBuffCmd cmd)
    {
        if (!TryGetPlayer(cmd.ConnectionId, out var player) || player.Dead)
            return;
        GrantFullBuffSet(player);
        SendSystemToEntity(player, "[DEBUG] Full buff set applied (1 hour).");
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
        if (npc.NpcRole == NpcRole.Buffer)
        {
            SendBufferDialog(player, npc);
            return;
        }

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
                // "What this class does" blurb so the (irreversible) choice is informed:
                // 2nd class = its archetype blurb; 3rd class = its discipline blurb.
                string blurb = req.Tier >= 3
                    ? (ThirdClassCatalog.Get(req.SecondClassId) is ThirdClassDef tcd
                        ? Disciplines.Blurb(tcd.Discipline) : "")
                    : (ClassCatalog.Get(req.SecondClassId) is SecondClassDef scd
                        ? ClassCatalog.ArchetypeBlurb(scd.Archetype) : "");
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

        // Skill reset (only for reset NPCs): the permanent, mutually-exclusive picks you've made.
        SkillResetInfo? reset = null;
        if (npc.NpcRole == NpcRole.SkillReset)
            reset = new SkillResetInfo(ResettableSkillsOf(player).OrderBy(s => s.Name).ToArray());

        SendTo(player, "Dialog", new NpcDialog(
            npc.Name, npc.NpcRole.ToString(),
            offered, turnable.ToArray(), inProgress.ToArray(), changes.ToArray(), shop, teleport, reset));

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
        // A mob with a natural level brings its own (its authored base curve is tuned for it);
        // otherwise the zone assigns the level.
        int level = mobType.Level > 0 ? mobType.Level : _rng.Next(zone.MinLevel, zone.MaxLevel + 1);
        BuildMob(mobId, level, zone.Rank, x, y, zone.Id);
        zr.OnSpawned();
    }

    /// <summary>Create, configure and register one live mob: base stats from the level curve, the
    /// rank multipliers (elite/boss), the template's MobMod passives, the role archetype, and — for a
    /// Boss — its unique skill kit (BossCatalog) or the generic slam. Zone spawns pass the zone rank/id;
    /// boss ADDS pass Normal rank + an empty zone id so they don't schedule a zone respawn.</summary>
    private Entity BuildMob(string mobId, int level, MobRank rank, float x, float y, string zoneId)
    {
        var mobType = MobCatalog.Get(mobId);
        var stats = StatCalculator.MobStats(level);

        // Elites/bosses are tougher versions of the base mob.
        float hpMul = rank switch { MobRank.Elite => 4f, MobRank.Boss => 20f, _ => 1f };
        float atkMul = rank switch { MobRank.Elite => 1.5f, MobRank.Boss => 2.5f, _ => 1f };

        string displayName = mobType.Dummy ? $"Training Dummy (Lv {level})" : rank switch
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
            AtkStat = stats.Atk,   // eva/acc/crit only; mob P/M.Atk comes from the base curve
            Wit = stats.Wit,
            Dex = stats.Dex,
            Aggressive = mobType.Aggressive || rank != MobRank.Normal,
            ZoneId = zoneId,
            Rank = rank,
            MobTypeId = mobId
        };
        mob.RecomputeDerived();
        // RecomputeDerived leaves mob RunSpeed/WalkSpeed as set above (player-only
        // override), so Speed stays the catalog run speed. HP/atk get the zone-rank
        // multipliers here; the base curve (incl. M.Def) already came from RecomputeDerived.
        mob.MaxHp = (int)(mob.MaxHp * hpMul);
        if (atkMul != 1f)
        {
            mob.AttackPower = (int)(mob.AttackPower * atkMul);
            mob.MagicAttack = (int)(mob.MagicAttack * atkMul);
            mob.BasicAttackPower = (int)(mob.BasicAttackPower * atkMul);
        }

        // Template "passive skills": per-mob stat modifiers (magic monster, armored
        // brute, boss, …) applied on top of the level-derived + rank stats.
        if (mobType.Mod is MobMod mod)
        {
            mob.MaxHp = Math.Max(1, (int)(mob.MaxHp * mod.Hp));
            mob.MaxMp = Math.Max(1, (int)(mob.MaxMp * mod.MaxMp));
            mob.Defence = Math.Max(1, (int)(mob.Defence * mod.PDef));
            mob.MagicDefence = Math.Max(1, (int)(mob.MagicDefence * mod.MDef));
            mob.AttackPower = Math.Max(1, (int)(mob.AttackPower * mod.PAtk));
            mob.MagicAttack = Math.Max(1, (int)(mob.MagicAttack * mod.MAtk));
            mob.BasicAttackPower = Math.Max(1, (int)(mob.BasicAttackPower * mod.PAtk));
            mob.Evasion = (int)(mob.Evasion * mod.Evasion) + mod.EvaFlat;
            mob.Accuracy = (int)(mob.Accuracy * mod.Accuracy);
            // Leveled-mastery extras: attack speed (>1 = faster → shorter interval), HP/MP regen.
            if (mod.AtkSpeed != 1f) mob.AttackSpeedMultiplier /= mod.AtkSpeed;
            if (mod.HpRegen != 1f) mob.HpRegenMult *= mod.HpRegen;
            if (mod.MpRegen != 1f) mob.MpRegenMult *= mod.MpRegen;
            mob.BowResist = Math.Clamp(mod.BowResist, 0f, 0.9f);
            mob.CritRateResist = Math.Clamp(mod.CritResist, 0f, 1f);
            // Weapon-type resistance coefficients (P.Def route; applied per-hit by attacker weapon).
            mob.PierceDefCoef = mod.PierceResist;
            mob.BluntDefCoef = mod.BluntResist;
            mob.BowDefCoef = mod.BowDefResist;
            if (mod.Boss)   // raid-boss passive: resists crits + arrows
            {
                mob.CritRateResist = Math.Max(mob.CritRateResist, 0.3f);
                mob.BowResist = Math.Max(mob.BowResist, 0.3f);
            }
        }

        // Mob ROLE: ranged/caster archetypes on top of the base+passive stats.
        switch (mobType.Role)
        {
            case MobRole.Archer:
                // Fires from ~450 range with a bow; higher P.Atk but light armor (less P.Def,
                // a little more evasion). Uses the normal auto-attack — just at longer range.
                mob.WeaponType = WeaponType.Bow;
                mob.BasicAttackRange = 450f;
                mob.AttackPower = Math.Max(1, (int)(mob.AttackPower * 2f));
                mob.BasicAttackPower = Math.Max(1, (int)(mob.BasicAttackPower * 2f));
                mob.Defence = Math.Max(1, (int)(mob.Defence * 0.85f));
                mob.Evasion += 8;
                break;
            case MobRole.Mage:
                // No basic attack — casts the mob spells (learned at the level its own maps to).
                // Higher M.Atk, lower P.Atk / P.Def; MP-gated (out of MP → helpless).
                mob.CasterMob = true;
                mob.MagicAttack = Math.Max(1, (int)(mob.MagicAttack * 1.5f));
                mob.AttackPower = Math.Max(1, (int)(mob.AttackPower * 0.5f));
                mob.BasicAttackPower = 1;
                mob.Defence = Math.Max(1, (int)(mob.Defence * 0.7f));
                mob.BasicAttackRange = 0f;
                int spellLevel = SkillCatalog.MobSpellLevel(level);
                mob.LearnedSkills[SkillCatalog.MobNukeSkill] = spellLevel;
                mob.LearnedSkills[SkillCatalog.MobBoltSkill] = spellLevel;
                break;
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

        mob.Hp = mob.MaxHp;
        mob.Mp = mob.MaxMp;
        mob.HomeX = mob.X;
        mob.HomeY = mob.Y;

        // Training dummy: TAKES damage (so you see the numbers) but never dies — a huge HP
        // pool + big regen, plus a death-floor in ApplyDamage. Stationary, never attacks.
        if (mobType.Dummy)
        {
            mob.TrainingDummy = true;
            mob.Aggressive = false;
            mob.WalkSpeed = 0; mob.RunSpeed = 0; mob.Speed = 0;
            mob.MaxHp = 1_000_000;
            mob.Hp = mob.MaxHp;
            mob.HpRegenBonus = 10_000;   // ~10k HP/sec (it's never "engaged", so regen runs)
        }

        _world.Entities[mob.Id] = mob;
        _world.Grid.Add(mob);
        return mob;
    }
}
