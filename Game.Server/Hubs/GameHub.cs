using System.Collections.Concurrent;
using Game.Server.Persistence;
using Game.Server.Simulation;
using Game.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Game.Server.Hubs;

/// <summary>
/// Connection layer. Auth + character selection happen here (async DB I/O,
/// off the game loop). Once a player enters the world, gameplay calls become
/// commands on the world queue, exactly as before.
/// </summary>
public class GameHub : Hub
{
    private readonly World _world;
    private readonly PersistenceService _db;

    // Connection -> authenticated account (set after login/register).
    private static readonly ConcurrentDictionary<string, AuthState> Sessions = new();

    /// <summary>Who this connection is. NOT what they're allowed to do — staff powers belong to the
    /// CHARACTER they enter the world with (owner), and are re-checked on the game loop against
    /// <see cref="Entity.Role"/> for every moderation command.</summary>
    private record AuthState(int AccountId);

    public GameHub(World world, PersistenceService db)
    {
        _world = world;
        _db = db;
    }

    // ----- Auth --------------------------------------------------------------

    public async Task<AuthResponse> Register(AuthRequest request, string clientVersion = "")
    {
        if (VersionMismatch(clientVersion, request.Protocol) is string v) return new AuthResponse(false, v);
        var result = await _db.RegisterAsync(request.Username, request.Password);
        if (result.Success)
            Sessions[Context.ConnectionId] = new AuthState(result.AccountId);
        return new AuthResponse(result.Success, result.Error, AccountRole.Player);
    }

    public async Task<AuthResponse> Login(AuthRequest request, string clientVersion = "")
    {
        if (VersionMismatch(clientVersion, request.Protocol) is string v) return new AuthResponse(false, v);
        var result = await _db.LoginAsync(request.Username, request.Password);
        if (result.Success)
            Sessions[Context.ConnectionId] = new AuthState(result.AccountId);
        return new AuthResponse(result.Success, result.Error, AccountRole.Player);
    }

    /// <summary>Reject a client this server cannot talk to. The gate is the PROTOCOL number, not the
    /// build label — see GameConstants.ClientRejectionReason for why, and for the legacy fallback that
    /// keeps pre-0.28.25 builds working.</summary>
    private static string? VersionMismatch(string clientVersion, int clientProtocol) =>
        GameConstants.ClientRejectionReason(clientVersion, clientProtocol);

    // ----- Character selection ----------------------------------------------

    /// <summary>Public read-only board — the top characters for one <see cref="Leaderboards"/> category.
    /// No world state touched, so it answers straight from persistence off the game loop.</summary>
    public async Task<LeaderboardDto> RequestLeaderboard(string category)
        => await _db.GetLeaderboardAsync(
            Array.IndexOf(Leaderboards.Categories, category) >= 0 ? category : "level", 15);

    public async Task<CharacterList> ListCharacters()
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return new CharacterList(Array.Empty<CharacterSlot>());

        var chars = await _db.ListCharactersAsync(auth.AccountId);
        return new CharacterList(chars
            .Select(c => new CharacterSlot(c.Id, c.Name, c.Race, c.BaseClass, c.SecondClass, c.Level,
                                          c.PendingDeleteAt, c.ThirdClass))
            .ToArray());
    }

    /// <summary>Schedule (or immediately perform, for low levels) a character deletion.
    /// Returns null on success, or an error string.</summary>
    public async Task<string?> DeleteCharacter(int characterId)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return "Not logged in.";
        var (ok, _, error) = await _db.RequestDeleteCharacterAsync(auth.AccountId, characterId);
        return ok ? null : error;
    }

    /// <summary>Cancel a pending deletion (restore the character).</summary>
    public async Task<string?> CancelDeleteCharacter(int characterId)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return "Not logged in.";
        return await _db.CancelDeleteCharacterAsync(auth.AccountId, characterId)
            ? null : "Nothing to cancel.";
    }

    public async Task<string?> CreateCharacter(CreateCharacterRequest request)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return "Not logged in.";

        var (success, error) = await _db.CreateCharacterAsync(
            auth.AccountId, request.Name, request.Race, request.BaseClass);
        return success ? null : error;
    }

    /// <summary>Load a character from the DB and hand it to the game loop.</summary>
    public async Task<LoginResult> EnterWorld(EnterWorldRequest request)
    {
        if (!Sessions.TryGetValue(Context.ConnectionId, out var auth))
            return new LoginResult(false, "Not logged in.", Guid.Empty, 0, 0);

        // Reject only if the character is GENUINELY online on another connection. An entity that is
        // offline-farming or sitting in the link-dead grace is still in the world, but re-entering it
        // is a RECONNECT — HandleEnterWorld already re-attaches to that live entity. This guard used
        // to reject those too, which (a) made that reconnect path dead code and (b) locked you out of
        // your own character for the 180s grace after returning to character select.
        if (_world.Entities.Values.Any(e =>
                e.Kind == EntityKind.Player && e.PersistentId == request.CharacterId
                && !e.IsOfflineFarming && !e.IsDisconnected))
            return new LoginResult(false, "That character is already online.", Guid.Empty, 0, 0);

        // KICK is per-character + timed: the account can play its OTHER characters, but this one can't
        // enter the world until the lockout passes (owner).
        if (await _db.GetKickUntilAsync(auth.AccountId, request.CharacterId) is DateTime kickUntil
            && kickUntil > DateTime.UtcNow)
        {
            var left = kickUntil - DateTime.UtcNow;
            string t = left.TotalHours >= 1 ? $"{(int)left.TotalHours}h {left.Minutes}m"
                     : left.TotalMinutes >= 1 ? $"{(int)left.TotalMinutes}m" : $"{(int)left.TotalSeconds}s";
            return new LoginResult(false, $"This character is locked for another {t}.", Guid.Empty, 0, 0);
        }

        var entity = await _db.LoadCharacterAsync(auth.AccountId, request.CharacterId);
        if (entity is null)
            return new LoginResult(false, "Character not found.", Guid.Empty, 0, 0);

        // entity.Role came from the character row in LoadCharacterAsync — nothing to overlay here.

        // The ACCOUNT bank is read here, on the async login path, not on the tick thread. The loop
        // discards it if that account already has a live list (see EnterWorldCommand).
        var accountBank = await _db.LoadAccountWarehouseAsync(auth.AccountId);

        var tcs = new TaskCompletionSource<LoginResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _world.Commands.Enqueue(new EnterWorldCommand(Context.ConnectionId, entity, tcs, accountBank));

        var timeout = Task.Delay(TimeSpan.FromSeconds(5));
        var finished = await Task.WhenAny(tcs.Task, timeout);
        return finished == tcs.Task
            ? await tcs.Task
            : new LoginResult(false, "Server busy, try again.", Guid.Empty, 0, 0);
    }

    /// <summary>Leave the world but keep the connection (return to char select). This is a DELIBERATE
    /// exit, so it must NOT go through LeaveCommand — that is the DISCONNECT path, which parks the
    /// character in a 180s link-dead grace (or offline-farming). Doing that on a char-select left the
    /// entity in the world and then refused to let you back into your own character.</summary>
    /// <returns>null when the character left; otherwise the reason it was refused (in combat).</returns>
    /// <remarks>The client must call this with <c>InvokeAsync</c>, NOT <c>SendAsync</c> — SendAsync
    /// returns as soon as the message is written and never waits for the hub method, so both the save
    /// and the refusal would be invisible to it.</remarks>
    public async Task<string?> LeaveWorld()
    {
        // Awaits the SAVE, not just the command. The client calls ListCharacters as soon as this
        // returns, and the save is a background write off the tick — returning immediately meant the
        // select screen read the row as it was at login.
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _world.Commands.Enqueue(new LeaveWorldCmd(Context.ConnectionId, tcs));

        // Never hang the client on a stuck write — a stale row beats a frozen screen.
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        return finished == tcs.Task ? await tcs.Task : null;
    }

    /// <summary>Ask for a full re-send of everything visible (see <see cref="ResyncCmd"/>). Cheap and
    /// idempotent: it only forgets the per-connection diff state, so the next tick sends spawns.</summary>
    public Task RequestResync()
    {
        _world.Commands.Enqueue(new ResyncCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    // ----- Gameplay (unchanged: enqueue commands) ----------------------------

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

    public Task UseSkill(string skillId, Guid? targetId)
    {
        _world.Commands.Enqueue(new SkillCmd(Context.ConnectionId, skillId, targetId));
        return Task.CompletedTask;
    }

    public Task LearnSkill(string skillId)
    {
        _world.Commands.Enqueue(new LearnSkillCmd(Context.ConnectionId, skillId));
        return Task.CompletedTask;
    }

    public Task TalkToNpc(Guid npcEntityId)
    {
        _world.Commands.Enqueue(new TalkCmd(Context.ConnectionId, npcEntityId));
        return Task.CompletedTask;
    }

    public Task QuestAction(string action, string id, Guid npcEntityId)
    {
        _world.Commands.Enqueue(new QuestActionCmd(Context.ConnectionId, action, id, npcEntityId));
        return Task.CompletedTask;
    }

    public Task BuyItem(Guid npcEntityId, string itemDefId, int quantity)
    {
        _world.Commands.Enqueue(new BuyItemCmd(Context.ConnectionId, npcEntityId, itemDefId, quantity));
        return Task.CompletedTask;
    }

    public Task SellItem(Guid npcEntityId, Guid instanceId, int quantity)
    {
        _world.Commands.Enqueue(new SellItemCmd(Context.ConnectionId, npcEntityId, instanceId, quantity));
        return Task.CompletedTask;
    }

    public Task BuyBack(Guid npcEntityId, int index)
    {
        _world.Commands.Enqueue(new BuyBackCmd(Context.ConnectionId, npcEntityId, index));
        return Task.CompletedTask;
    }

    public Task OpenWarehouse()
    {
        _world.Commands.Enqueue(new OpenWarehouseCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task WarehouseDeposit(Guid instanceId)
    {
        _world.Commands.Enqueue(new WarehouseDepositCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task OpenAccountWarehouse()
    {
        _world.Commands.Enqueue(new OpenAccountWarehouseCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task AccountWarehouseDeposit(Guid instanceId)
    {
        _world.Commands.Enqueue(new AccountWarehouseDepositCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task AccountWarehouseWithdraw(Guid instanceId)
    {
        _world.Commands.Enqueue(new AccountWarehouseWithdrawCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task WarehouseWithdraw(Guid instanceId)
    {
        _world.Commands.Enqueue(new WarehouseWithdrawCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task Teleport(Guid npcEntityId, string zoneId)
    {
        _world.Commands.Enqueue(new TeleportCmd(Context.ConnectionId, npcEntityId, zoneId));
        return Task.CompletedTask;
    }

    public Task ForgetSkill(Guid npcEntityId, string skillId)
    {
        _world.Commands.Enqueue(new ForgetSkillCmd(Context.ConnectionId, npcEntityId, skillId));
        return Task.CompletedTask;
    }

    public Task BufferAction(Guid npcEntityId, string action, string skillId)
    {
        _world.Commands.Enqueue(new BufferActionCmd(Context.ConnectionId, npcEntityId, action, skillId ?? ""));
        return Task.CompletedTask;
    }

    public Task SetMoveState(int state)
    {
        _world.Commands.Enqueue(new SetMoveStateCmd(Context.ConnectionId, (MoveState)state));
        return Task.CompletedTask;
    }

    public Task CancelCast()
    {
        _world.Commands.Enqueue(new CancelCastCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task RemoveBuff(string buffKey)
    {
        _world.Commands.Enqueue(new RemoveBuffCmd(Context.ConnectionId, buffKey));
        return Task.CompletedTask;
    }

    public Task OpenBox(Guid instanceId)
    {
        _world.Commands.Enqueue(new OpenBoxCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task SelectBoxItems(Guid instanceId, string[] itemIds)
    {
        _world.Commands.Enqueue(new SelectBoxItemsCmd(Context.ConnectionId, instanceId,
            itemIds ?? System.Array.Empty<string>()));
        return Task.CompletedTask;
    }

    public Task InspectTarget(Guid targetId, bool withDrops = false)
    {
        _world.Commands.Enqueue(new InspectTargetCmd(Context.ConnectionId, targetId, withDrops));
        return Task.CompletedTask;
    }

    public Task Respawn()
    {
        _world.Commands.Enqueue(new RespawnCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task ChangeClass(int classId)
    {
        _world.Commands.Enqueue(new ClassChangeCmd(Context.ConnectionId, classId));
        return Task.CompletedTask;
    }

    public Task EquipItem(Guid instanceId)
    {
        _world.Commands.Enqueue(new EquipCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    public Task UsePotion(Guid instanceId)
    {
        _world.Commands.Enqueue(new UsePotionCmd(Context.ConnectionId, instanceId));
        return Task.CompletedTask;
    }

    /// <summary>Use a TARGETED consumable (a resurrection scroll) on the given entity (a dead ally).</summary>
    public Task UsePotionOn(Guid instanceId, Guid targetId)
    {
        _world.Commands.Enqueue(new UsePotionCmd(Context.ConnectionId, instanceId, targetId));
        return Task.CompletedTask;
    }

    /// <summary>Answer a pending resurrection offer (accept = revive; decline = stay dead).</summary>
    public Task ResurrectResponse(bool accept)
    {
        _world.Commands.Enqueue(new ResurrectResponseCmd(Context.ConnectionId, accept));
        return Task.CompletedTask;
    }

    public Task Enchant(Guid scrollInstanceId, Guid targetInstanceId)
    {
        _world.Commands.Enqueue(new EnchantCmd(Context.ConnectionId, scrollInstanceId, targetInstanceId));
        return Task.CompletedTask;
    }

    public Task RerollAttributes(Guid scrollInstanceId, Guid targetInstanceId)
    {
        _world.Commands.Enqueue(new RerollAttributesCmd(Context.ConnectionId,
            scrollInstanceId, targetInstanceId));
        return Task.CompletedTask;
    }

    public Task RemoveItem(Guid instanceId, bool all, int quantity)
    {
        _world.Commands.Enqueue(new RemoveItemCmd(Context.ConnectionId, instanceId, all, quantity));
        return Task.CompletedTask;
    }

    public Task PartyInvite(Guid targetId)
    {
        _world.Commands.Enqueue(new PartyInviteCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task PartyRespond(bool accept)
    {
        _world.Commands.Enqueue(new PartyRespondCmd(Context.ConnectionId, accept));
        return Task.CompletedTask;
    }

    public Task PartyLeave()
    {
        _world.Commands.Enqueue(new PartyLeaveCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task PartyChangeLeader(Guid targetId)
    {
        _world.Commands.Enqueue(new PartyChangeLeaderCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task SaveEquipPreset(int slot)
    {
        _world.Commands.Enqueue(new SaveEquipPresetCmd(Context.ConnectionId, slot));
        return Task.CompletedTask;
    }

    public Task ApplyEquipPreset(int slot)
    {
        _world.Commands.Enqueue(new ApplyEquipPresetCmd(Context.ConnectionId, slot));
        return Task.CompletedTask;
    }

    public Task PartyKick(Guid targetId)
    {
        _world.Commands.Enqueue(new PartyKickCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task PartySetLootMode(LootMode mode)
    {
        _world.Commands.Enqueue(new PartySetLootModeCmd(Context.ConnectionId, mode));
        return Task.CompletedTask;
    }

    public Task PartyLootVote(bool accept)
    {
        _world.Commands.Enqueue(new PartyLootVoteCmd(Context.ConnectionId, accept));
        return Task.CompletedTask;
    }

    public Task SetAutoHuntConfig(AutoHuntConfigDto config)
    {
        _world.Commands.Enqueue(new SetAutoHuntConfigCmd(Context.ConnectionId, config));
        return Task.CompletedTask;
    }

    public Task SetSkillBar(string[] slots)
    {
        _world.Commands.Enqueue(new SetSkillBarCmd(Context.ConnectionId, slots));
        return Task.CompletedTask;
    }

    public Task ToggleAutoHunt(bool enabled)
    {
        _world.Commands.Enqueue(new ToggleAutoHuntCmd(Context.ConnectionId, enabled));
        return Task.CompletedTask;
    }

    public Task Logout()
    {
        _world.Commands.Enqueue(new LogoutCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task StartOfflineFarm()
    {
        _world.Commands.Enqueue(new StartOfflineFarmCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task TogglePvp(bool enabled)
    {
        _world.Commands.Enqueue(new TogglePvpCmd(Context.ConnectionId, enabled));
        return Task.CompletedTask;
    }

    /// <summary>Wear the title of a leaderboard category, or "" to wear none. The server checks you
    /// actually hold that board — the client only offers what it was told you hold.</summary>
    public Task SetTitle(string category)
    {
        _world.Commands.Enqueue(new SetTitleCmd(Context.ConnectionId, category ?? ""));
        return Task.CompletedTask;
    }

    public Task ToggleCounterAttack(bool enabled)
    {
        _world.Commands.Enqueue(new ToggleCounterAttackCmd(Context.ConnectionId, enabled));
        return Task.CompletedTask;
    }

    public Task RequestDebugConfig()
    {
        _world.Commands.Enqueue(new RequestDebugConfigCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task SetDebugConfig(DebugConfigDto config)
    {
        _world.Commands.Enqueue(new SetDebugConfigCmd(Context.ConnectionId, config));
        return Task.CompletedTask;
    }

    public Task TradeRequest(Guid targetId)
    {
        _world.Commands.Enqueue(new TradeRequestCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task TradeRespond(bool accept)
    {
        _world.Commands.Enqueue(new TradeRespondCmd(Context.ConnectionId, accept));
        return Task.CompletedTask;
    }

    public Task TradeOffer(TradeOfferEntry[] entries)
    {
        _world.Commands.Enqueue(new TradeOfferCmd(Context.ConnectionId, entries ?? Array.Empty<TradeOfferEntry>()));
        return Task.CompletedTask;
    }

    public Task TradeGold(long gold)
    {
        _world.Commands.Enqueue(new TradeGoldCmd(Context.ConnectionId, gold));
        return Task.CompletedTask;
    }

    public Task TradeReady()
    {
        _world.Commands.Enqueue(new TradeReadyCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task TradeCancel()
    {
        _world.Commands.Enqueue(new TradeCancelCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    public Task Chat(string text, ChatChannel channel, string? whisperTarget)
    {
        _world.Commands.Enqueue(new ChatCmd(Context.ConnectionId, text, channel, whisperTarget));
        return Task.CompletedTask;
    }

    /// <summary>Friend list: action = "add" / "remove" / "list". Any player.</summary>
    public Task FriendCommand(string action, string name)
    {
        _world.Commands.Enqueue(new FriendCmd(Context.ConnectionId, action, name ?? ""));
        return Task.CompletedTask;
    }

    public Task BlockCommand(string action, string name)
    {
        _world.Commands.Enqueue(new BlockCmd(Context.ConnectionId, action, name ?? ""));
        return Task.CompletedTask;
    }

    public Task Like(string name)
    {
        _world.Commands.Enqueue(new LikeCmd(Context.ConnectionId, name ?? ""));
        return Task.CompletedTask;
    }

    /// <summary>Follow a player (null = stop). Assist = attack whatever they're attacking.</summary>
    public Task Follow(Guid? targetId)
    {
        _world.Commands.Enqueue(new FollowCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    public Task Assist(Guid targetId)
    {
        _world.Commands.Enqueue(new AssistCmd(Context.ConnectionId, targetId));
        return Task.CompletedTask;
    }

    // ----- Admin commands ----------------------------------------------------

    public Task AdminCommand(string command, string argument)
    {
        // Only checks that the connection is logged in. The REAL authorization is the character's role,
        // which lives on the game loop (HandleAdmin re-checks it, per command, for every action).
        if (!Sessions.ContainsKey(Context.ConnectionId))
            return Task.CompletedTask;
        _world.Commands.Enqueue(new AdminCmd(Context.ConnectionId, command, argument));
        return Task.CompletedTask;
    }

    /// <summary>Admin: hand one of my items to another online player (/give picker).</summary>
    public Task AdminGiveItem(string targetName, Guid instanceId, int quantity)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId))
            return Task.CompletedTask;
        _world.Commands.Enqueue(
            new AdminGiveItemCmd(Context.ConnectionId, targetName, instanceId, quantity));
        return Task.CompletedTask;
    }

    /// <summary>Admin: destroy an item in another player's bag (/bag window).</summary>
    public Task AdminRemoveItem(string targetName, Guid instanceId)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId))
            return Task.CompletedTask;
        _world.Commands.Enqueue(new AdminRemoveItemCmd(Context.ConnectionId, targetName, instanceId));
        return Task.CompletedTask;
    }

    // ----- ADMIN TOOLS (the former "debug menu") -------------------------------------------------
    //
    // These were `#if DEBUG`, which meant the RELEASE server published to the phone accepted every one of
    // these calls and did nothing — the menu was on screen and pressing it was silence (owner, 2026-07-30).
    // A compile flag was the wrong gate: the question is "is this character an admin", which is a runtime
    // fact. They ship in every build now and are authorised on the game loop, once, by IAdminCommand →
    // GameLoopService.IsBlockedForNonAdmin. Same shape as AdminCommand above: the hub only checks that the
    // connection is logged in and enqueues; the real authorisation is the character's role.
    //
    // The WIRE names keep their `Debug` prefix on purpose. Renaming ~50 string literals across two clients
    // and two tools buys no behaviour and fails SILENTLY when one is missed — a fire-and-forget SignalR
    // call to a method that no longer exists does exactly what the #if DEBUG bug did.

    /// <summary>Admin: grant an item by def id (quantity = extra copies, for stackables).</summary>
    public Task DebugGive(string defId, int quantity)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugGiveCmd(Context.ConnectionId, defId));
        for (int i = 1; i < Math.Max(1, quantity); i++)
            _world.Commands.Enqueue(new DebugGiveCmd(Context.ConnectionId, defId));
        return Task.CompletedTask;
    }

    /// <summary>Admin: shift level by delta (+1 / +10 / -1 / -10). Delevel keeps learned skills.</summary>
    public Task DebugLevel(int delta)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        // Clamp the STEP, not the level — the level cap itself is applied on the game loop, where the
        // admin exemption lives. This just stops a malformed payload jumping 10,000 levels.
        _world.Commands.Enqueue(new DebugLevelCmd(Context.ConnectionId, Math.Clamp(delta, -10, 10)));
        return Task.CompletedTask;
    }

    /// <summary>Admin: cancel an attribute on the equipped weapon (index; -1 = all).</summary>
    public Task DebugCancelAttr(int index)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugCancelAttrCmd(Context.ConnectionId, index));
        return Task.CompletedTask;
    }

    /// <summary>Craft a recipe by id (consume inputs → roll success → produce output).</summary>
    public Task Craft(string recipeId)
    {
        _world.Commands.Enqueue(new CraftCmd(Context.ConnectionId, recipeId));
        return Task.CompletedTask;
    }

    /// <summary>Choose the character's permanent crafting profession (1..5).</summary>
    public Task ChooseProfession(int profession)
    {
        _world.Commands.Enqueue(new ChooseProfessionCmd(Context.ConnectionId, profession));
        return Task.CompletedTask;
    }

    /// <summary>Admin: set the CRAFTING profession (0=None..5=ScrollScribe). For the 2nd CLASS use
    /// <see cref="DebugSecondClass"/> — the admin panel used to send class ids here, and they were
    /// clamped into this 5-value enum.</summary>
    public Task DebugSetProfession(int profession)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugSetProfessionCmd(Context.ConnectionId, profession));
        return Task.CompletedTask;
    }

    /// <summary>Admin: become a 2nd CLASS directly (ClassCatalog id), skipping the quest and level
    /// gates. The NPC path stays the real one — this is the "compare two builds now" lever.</summary>
    public Task DebugSecondClass(int classId)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugSecondClassCmd(Context.ConnectionId, classId));
        return Task.CompletedTask;
    }

    /// <summary>Admin: learn every skill the class can learn at the current level, free.</summary>
    public Task DebugLearnAll()
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugLearnAllCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    /// <summary>Admin: grant gold.</summary>
    public Task DebugGold(long amount)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugGoldCmd(Context.ConnectionId, amount));
        return Task.CompletedTask;
    }

    /// <summary>Admin: full NPC buff set on yourself, any level, no walk to the NPC.</summary>
    public Task DebugBuff()
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugBuffCmd(Context.ConnectionId));
        return Task.CompletedTask;
    }

    /// <summary>Admin: nudge karma by a delta (test the red-name gradient + clearing).</summary>
    public Task DebugKarma(int delta)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugKarmaCmd(Context.ConnectionId, delta));
        return Task.CompletedTask;
    }

    /// <summary>Admin: add a SUBCLASS (another class this character owns) and switch to it.</summary>
    public Task DebugAddSubclass(int thirdClassId)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugAddSubclassCmd(Context.ConnectionId, thirdClassId));
        return Task.CompletedTask;
    }

    /// <summary>Switch to a class this character already owns. ADMIN for now — the player-facing version
    /// gates this on a safe zone + a delay, and will wrap this same command.</summary>
    public Task SwitchSubclass(int slot)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new SwitchSubclassCmd(Context.ConnectionId, slot));
        return Task.CompletedTask;
    }

    /// <summary>Admin: grant skill points.</summary>
    public Task DebugSp(long amount)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugSpCmd(Context.ConnectionId, amount));
        return Task.CompletedTask;
    }

    /// <summary>Admin: re-roll this character (new race/base class, back to level 1 with the starter kit).</summary>
    public Task DebugReset(int race, int baseClass)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugResetCmd(Context.ConnectionId, (Race)race, (BaseClass)baseClass));
        return Task.CompletedTask;
    }

    /// <summary>Admin: take a 3rd class (discipline) directly, bypassing the quest chain + items.</summary>
    public Task DebugThirdClass(int thirdClassId)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugThirdClassCmd(Context.ConnectionId, thirdClassId));
        return Task.CompletedTask;
    }

    /// <summary>Admin: teleport to arbitrary world coordinates.</summary>
    public Task DebugTeleport(float x, float y)
    {
        if (!Sessions.ContainsKey(Context.ConnectionId)) return Task.CompletedTask;
        _world.Commands.Enqueue(new DebugTeleportCmd(Context.ConnectionId, x, y));
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Sessions.TryRemove(Context.ConnectionId, out _);
        _world.Commands.Enqueue(new LeaveCommand(Context.ConnectionId));
        return base.OnDisconnectedAsync(exception);
    }
}
