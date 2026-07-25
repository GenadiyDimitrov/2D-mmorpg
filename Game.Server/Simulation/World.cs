using System.Collections.Concurrent;
using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A live trade between two players. Owned by the loop thread.</summary>
public class TradeSession
{
    public required Entity A { get; init; }
    public required Entity B { get; init; }
    public List<Guid> OfferA { get; } = new();
    public List<Guid> OfferB { get; } = new();
    public bool ReadyA { get; set; }
    public bool ReadyB { get; set; }

    /// <summary>Gold each side is putting into the trade (so you can PAY for what the other gives).</summary>
    public long GoldA { get; set; }
    public long GoldB { get; set; }

    public Entity PartnerOf(Entity e) => e == A ? B : A;
    public List<Guid> OfferOf(Entity e) => e == A ? OfferA : OfferB;
    public bool ReadyOf(Entity e) => e == A ? ReadyA : ReadyB;
    public void SetReady(Entity e, bool value) { if (e == A) ReadyA = value; else ReadyB = value; }
    public long GoldOf(Entity e) => e == A ? GoldA : GoldB;
    public void SetGold(Entity e, long v) { if (e == A) GoldA = v; else GoldB = v; }
}

/// <summary>A placed TRAP (Trapper skill). Server-only (not an Entity, so it's invisible to the
/// snapshot — a dedicated visual is client work). When a hostile steps within Radius the trap
/// delivers its skill's damage + CC to that intruder, attributed to the owner, then is removed.</summary>
public class TrapInstance
{
    public required Guid OwnerId { get; init; }
    public required string SkillId { get; init; }
    public int Level { get; init; } = 1;
    public float X { get; init; }
    public float Y { get; init; }
    public float Radius { get; init; }
    public int LifeTicks { get; set; }
}

/// <summary>A live adventuring party. Owned by the loop thread. The leader can invite/kick;
/// members share XP (split among those in range) and are the targets of AoE ally heals/buffs.</summary>
public class Party
{
    public Guid LeaderId { get; set; }
    public List<Guid> Members { get; } = new();   // includes the leader; order = join order

    /// <summary>The loot rule a new party starts on (a per-player configurable default will come
    /// with the settings panel).</summary>
    public const LootMode DefaultLootMode = LootMode.Random;

    /// <summary>How item loot is distributed. Gold is always split regardless.</summary>
    public LootMode LootMode { get; set; } = DefaultLootMode;

    /// <summary>Ever-increasing cursor for RoundRobin loot (mod eligible-count at use).</summary>
    public int RoundRobinCursor { get; set; } = -1;

    // ----- Pending loot-rule vote (leader proposes; every OTHER member must accept) -----
    /// <summary>The mode being voted on, or null when no vote is in progress.</summary>
    public LootMode? PendingLootMode { get; set; }
    /// <summary>Members who still have to accept the pending change (leader excluded).</summary>
    public HashSet<Guid> LootVotePending { get; } = new();
    /// <summary>Absolute tick the pending vote auto-cancels at.</summary>
    public long LootVoteExpireTick { get; set; }

    public bool Contains(Guid id) => Members.Contains(id);
}

/// <summary>
/// All live game state. The SignalR hub never touches the dictionaries
/// directly — it only enqueues commands. The game loop drains the queue,
/// so every mutation happens on a single thread. One writer, zero locks.
/// </summary>
public class World
{
    public ConcurrentQueue<IGameCommand> Commands { get; } = new();

    // Everything below is owned by the game-loop thread.

    public Dictionary<Guid, Entity> Entities { get; } = new();
    public Dictionary<Guid, string> EntityToConnection { get; } = new();
    public Dictionary<string, Guid> ConnectionToEntity { get; } = new();

    /// <summary>Both participants map to the same session.</summary>
    public Dictionary<Guid, TradeSession> ActiveTrades { get; } = new();

    /// <summary>targetEntityId -> requesterEntityId (one pending request each).</summary>
    public Dictionary<Guid, Guid> PendingTradeRequests { get; } = new();

    /// <summary>Every party MEMBER id maps to the shared <see cref="Party"/> object.</summary>
    public Dictionary<Guid, Party> Parties { get; } = new();

    /// <summary>Live placed traps (Trapper). Scanned each tick for intruders.</summary>
    public List<TrapInstance> Traps { get; } = new();

    /// <summary>invitedEntityId -> inviterEntityId (one pending party invite each).</summary>
    public Dictionary<Guid, Guid> PendingPartyInvites { get; } = new();

    /// <summary>invitedEntityId -> absolute tick the pending invite auto-expires at.</summary>
    public Dictionary<Guid, long> PendingPartyInviteExpiry { get; } = new();

    public CellGrid Grid { get; } = new(
        GameConstants.ZoneWidth, GameConstants.ZoneHeight, GameConstants.CellSize);
}

// ---------------------------------------------------------------------------
// Commands
// ---------------------------------------------------------------------------

public interface IGameCommand { }

public record EnterWorldCommand(
    string ConnectionId,
    Entity Entity,
    TaskCompletionSource<LoginResult> Result) : IGameCommand;

public record LeaveCommand(string ConnectionId) : IGameCommand;

public record MoveCmd(string ConnectionId, MoveCommand Move) : IGameCommand;

public record ChatCmd(
    string ConnectionId,
    string Text,
    ChatChannel Channel,
    string? WhisperTarget = null) : IGameCommand;

public record AttackCmd(string ConnectionId, Guid TargetId) : IGameCommand;

public record SkillCmd(string ConnectionId, string SkillId, Guid? TargetId) : IGameCommand;

/// <summary>Learn a skill by spending skill points.</summary>
public record LearnSkillCmd(string ConnectionId, string SkillId) : IGameCommand;

/// <summary>Open dialog with an NPC.</summary>
public record TalkCmd(string ConnectionId, Guid NpcEntityId) : IGameCommand;

/// <summary>Quest action: accept / complete / changeclass.</summary>
public record QuestActionCmd(string ConnectionId, string Action, string Id, Guid NpcEntityId) : IGameCommand;

/// <summary>Buy an item from a vendor NPC.</summary>
public record BuyItemCmd(string ConnectionId, Guid NpcEntityId, string ItemDefId, int Quantity) : IGameCommand;

/// <summary>Sell an inventory item to a vendor NPC.</summary>
public record SellItemCmd(string ConnectionId, Guid NpcEntityId, Guid InstanceId, int Quantity) : IGameCommand;

/// <summary>Re-buy a recently-sold item (by its index in the buy-back list) at a vendor.</summary>
public record BuyBackCmd(string ConnectionId, Guid NpcEntityId, int Index) : IGameCommand;

/// <summary>Pay a gatekeeper to warp to a safe zone.</summary>
public record TeleportCmd(string ConnectionId, Guid NpcEntityId, string ZoneId) : IGameCommand;

/// <summary>Ask a skill-reset NPC to UN-LEARN a permanent, mutually-exclusive skill (a level-40
/// stat swap), freeing its group so a different trade-off can be committed to. Free to do — the
/// gold already spent is NOT refunded.</summary>
public record ForgetSkillCmd(string ConnectionId, Guid NpcEntityId, string SkillId) : IGameCommand;

/// <summary>Change movement state (run / walk / sit).</summary>
public record SetMoveStateCmd(string ConnectionId, MoveState State) : IGameCommand;

/// <summary>Player cancelled their own cast (ESC) — stops it and starts cooldown.</summary>
public record CancelCastCmd(string ConnectionId) : IGameCommand;

/// <summary>Player manually removed one of their buffs (double-click on the buff bar).</summary>
public record RemoveBuffCmd(string ConnectionId, string BuffKey) : IGameCommand;

/// <summary>Player opened a box/chest from their inventory — rolls its loot table.</summary>
public record OpenBoxCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Player confirmed their picks from a SELECTION box.</summary>
public record SelectBoxItemsCmd(string ConnectionId, Guid InstanceId, string[] ItemIds) : IGameCommand;

/// <summary>Player expanded the target window — request the target's detailed
/// stats (and, for a mob, its passive modifier lines). WithDrops = also send the mob's DROP list, which
/// only the [Details] click asks for (the 1s refresh loop leaves it false so the static table isn't
/// recomputed each second).</summary>
public record InspectTargetCmd(string ConnectionId, Guid TargetId, bool WithDrops = false) : IGameCommand;

public record RespawnCmd(string ConnectionId) : IGameCommand;

/// <summary>Advance to a second class (level 20+, once).</summary>
public record ClassChangeCmd(string ConnectionId, int ClassId) : IGameCommand;

/// <summary>Equip or unequip an inventory item (toggles).</summary>
public record EquipCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Drink a potion from the inventory.</summary>
// TargetId lets a targeted consumable (a resurrection scroll) name the DEAD ally it revives; null =
// self/untargeted (ordinary potions).
public record UsePotionCmd(string ConnectionId, Guid InstanceId, Guid? TargetId = null) : IGameCommand;

/// <summary>A dead player's answer to a pending resurrection offer. Accept = revive (restore exp); decline
/// = stay dead (wait out the mobs, or town-respawn).</summary>
public record ResurrectResponseCmd(string ConnectionId, bool Accept) : IGameCommand;

/// <summary>Apply an enchant scroll to a target item.</summary>
public record EnchantCmd(string ConnectionId, Guid ScrollInstanceId, Guid TargetInstanceId) : IGameCommand;

/// <summary>Reroll a target item's rolled attributes with an attribute scroll,
/// locking the slots at LockedIndices (clamped to the scroll's lock capacity).</summary>
public record RerollAttributesCmd(string ConnectionId, Guid ScrollInstanceId,
    Guid TargetInstanceId, int[] LockedIndices) : IGameCommand;

/// <summary>Destroy an inventory item (later: sell/dismantle).</summary>
/// <summary>Destroy an item. <paramref name="All"/> = the whole stack; otherwise ONE from the stack
/// (a single item is removed either way).</summary>
// Quantity > 0 removes exactly that many from a stack (the bin numpad); 0 falls back to All (whole
// stack) / single-unit. All still wins for a non-stackable.
public record RemoveItemCmd(string ConnectionId, Guid InstanceId, bool All = false, int Quantity = 0) : IGameCommand;

/// <summary>Open the private warehouse (fetch its contents). Gated to safe zones.</summary>
public record OpenWarehouseCmd(string ConnectionId) : IGameCommand;
/// <summary>Move a whole item instance bag → warehouse.</summary>
public record WarehouseDepositCmd(string ConnectionId, Guid InstanceId) : IGameCommand;
/// <summary>Move a whole item instance warehouse → bag.</summary>
public record WarehouseWithdrawCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>DEBUG-only: grant an item by def id.</summary>
public record DebugGiveCmd(string ConnectionId, string DefId) : IGameCommand;

/// <summary>DEBUG-only: strip an attribute off the EQUIPPED weapon (Index = which; -1 = all).
/// Lets you test with only the base weapon / a chosen attribute, not the full rolled set.</summary>
public record DebugCancelAttrCmd(string ConnectionId, int Index) : IGameCommand;

/// <summary>Craft a recipe (consume its inputs, roll success, produce the output).</summary>
public record CraftCmd(string ConnectionId, string RecipeId) : IGameCommand;

/// <summary>Choose the character's ONE crafting profession (permanent — can't be changed).</summary>
public record ChooseProfessionCmd(string ConnectionId, int Profession) : IGameCommand;

/// <summary>DEBUG-only: set the player's crafting profession (until level-based assignment lands).</summary>
public record DebugSetProfessionCmd(string ConnectionId, int Profession) : IGameCommand;

/// <summary>DEBUG-only: grant one level.</summary>
/// <summary>DEBUG: shift the character's level by <paramref name="Delta"/> (+1 / +10 / −1 / −10).
/// Negative = delevel, which keeps every learned skill (see HandleDebugLevel).</summary>
public record DebugLevelCmd(string ConnectionId, int Delta) : IGameCommand;

/// <summary>DEBUG-only: learn every skill the class can learn at the current level (free).</summary>
public record DebugLearnAllCmd(string ConnectionId) : IGameCommand;

/// <summary>DEBUG-only: grant gold.</summary>
public record DebugGoldCmd(string ConnectionId, long Amount) : IGameCommand;

/// <summary>DEBUG: apply the full NPC buff set to yourself, at any level, without visiting the NPC.</summary>
public record DebugBuffCmd(string ConnectionId) : IGameCommand;

/// <summary>DEBUG: nudge your karma by a delta (test the red-name gradient + clearing).</summary>
public record DebugKarmaCmd(string ConnectionId, int Delta) : IGameCommand;

/// <summary>DEBUG: add a new SUBCLASS (a second/third class this character owns) and switch to it.
/// No cap, no delay, no safe-zone requirement — the real rules come with the player-facing system.</summary>
/// <summary>Add a SUBCLASS by its 3rd-class discipline id (a ThirdClassCatalog id). The new class
/// starts at level 1 but with that 3rd class already approved (race/base/2nd derived from it).</summary>
public record DebugAddSubclassCmd(string ConnectionId, int ThirdClassId) : IGameCommand;

/// <summary>DEBUG: switch to another class this character already owns.</summary>
public record SwitchSubclassCmd(string ConnectionId, int Slot) : IGameCommand;

/// <summary>DEBUG-only: grant skill points.</summary>
public record DebugSpCmd(string ConnectionId, long Amount) : IGameCommand;

/// <summary>DEBUG-only: re-roll the current character (new race/base class, reset to
/// level 1 with the starter kit; keeps the same character row + gold).</summary>
public record DebugResetCmd(string ConnectionId, Race Race, BaseClass BaseClass) : IGameCommand;

/// <summary>DEBUG-only: take a 3rd class (discipline) directly, bypassing the quest
/// chain + items. Parent 2nd class must already match the discipline.</summary>
public record DebugThirdClassCmd(string ConnectionId, int ThirdClassId) : IGameCommand;

/// <summary>Debug-menu teleport to arbitrary world coordinates.</summary>
public record DebugTeleportCmd(string ConnectionId, float X, float Y) : IGameCommand;

/// <summary>Admin command (kick/ban/jail/unjail/god). Validated in the hub.</summary>
public record AdminCmd(string ConnectionId, string Command, string Argument) : IGameCommand;

/// <summary>Friend-list action (add / remove / list). Any player — not admin-gated.</summary>
public record FriendCmd(string ConnectionId, string Action, string Name) : IGameCommand;

/// <summary>Ignore list: Action = block / unblock / list.</summary>
public record BlockCmd(string ConnectionId, string Action, string Name) : IGameCommand;

/// <summary>Give a player +1 charisma (from your daily like budget).</summary>
public record LikeCmd(string ConnectionId, string Name) : IGameCommand;

/// <summary>SERVER-internal: apply a charisma change to a character by NAME (online or offline) on the tick
/// thread. Enqueued by the moderation callbacks (which run on worker threads). Zero=true wipes both values.</summary>
public record CharismaAdjustCmd(string Name, int PoolDelta, long LifetimeDelta, bool Zero = false) : IGameCommand;

/// <summary>FOLLOW a player: walk toward them each tick until cancelled. TargetId null = stop following.</summary>
public record FollowCmd(string ConnectionId, Guid? TargetId) : IGameCommand;

/// <summary>ASSIST a player: adopt their current combat target (attack whatever they're attacking).</summary>
public record AssistCmd(string ConnectionId, Guid TargetId) : IGameCommand;

// ----- Party / grouping -----
public record PartyInviteCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record PartyRespondCmd(string ConnectionId, bool Accept) : IGameCommand;
public record PartyLeaveCmd(string ConnectionId) : IGameCommand;
public record PartyKickCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record PartyChangeLeaderCmd(string ConnectionId, Guid TargetId) : IGameCommand;
// Equipment presets A/B/C (slot 0/1/2): save the worn set, or apply a saved one.
public record SaveEquipPresetCmd(string ConnectionId, int Slot) : IGameCommand;
public record ApplyEquipPresetCmd(string ConnectionId, int Slot) : IGameCommand;
public record PartySetLootModeCmd(string ConnectionId, LootMode Mode) : IGameCommand;
public record PartyLootVoteCmd(string ConnectionId, bool Accept) : IGameCommand;
public record SetAutoHuntConfigCmd(string ConnectionId, AutoHuntConfigDto Config) : IGameCommand;

/// <summary>Client -> server: the player rearranged their skill bar; persist the new layout.</summary>
public record SetSkillBarCmd(string ConnectionId, string[] Slots) : IGameCommand;

/// <summary>Client -> server: a paid buffer action. Action ∈ "full" | "single" | "restore";
/// SkillId is set only for "single".</summary>
public record BufferActionCmd(string ConnectionId, Guid NpcEntityId, string Action, string SkillId) : IGameCommand;

/// <summary>Client -> server: DELIBERATELY leave the world and go back to character select, keeping
/// the connection. Distinct from <see cref="LeaveCommand"/>, which is the DISCONNECT path (link-dead
/// grace / offline farming) — a deliberate exit must actually remove the character from the world.</summary>
public record LeaveWorldCmd(string ConnectionId) : IGameCommand;

/// <summary>Client -> server: "forget what you think I have, send me everything again." The delta feed
/// has no other recovery path — a client that misses one spawn frame never hears about that entity
/// again (a lean update it can't draw, or nothing at all if the entity is standing still), so a client
/// that notices it is missing something must be able to ask for a clean re-send.</summary>
public record ResyncCmd(string ConnectionId) : IGameCommand;
public record ToggleAutoHuntCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record LogoutCmd(string ConnectionId) : IGameCommand;
public record StartOfflineFarmCmd(string ConnectionId) : IGameCommand;
public record TogglePvpCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record ToggleCounterAttackCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record RequestDebugConfigCmd(string ConnectionId) : IGameCommand;
public record SetDebugConfigCmd(string ConnectionId, DebugConfigDto Config) : IGameCommand;

// ----- Moderation follow-ups ------------------------------------------------------------------
// These carry a CHARACTER NAME rather than a connection id: the punishment is decided on a worker
// thread (the target's role may have to be read from the DB) but must be APPLIED by the single
// writer, and the target may well not be online at all — in which case they are simply no-ops and
// the persisted sentence does the work at their next login.

/// <summary>Remove a named character from the world immediately (kick / ban).</summary>
public record ForceRemoveCmd(string CharacterName, string Reason) : IGameCommand;

/// <summary>Pin a named character in jail immediately.</summary>
public record JailNowCmd(string CharacterName, DateTime Until, int Minutes) : IGameCommand;

/// <summary>Silence a named character immediately.</summary>
public record ChatBanNowCmd(string CharacterName, DateTime Until, int Minutes) : IGameCommand;

/// <summary>Admin -> server: hand one of MY items to another online player (from the /give picker).
/// Deliberately ignores tradability — staff can give anything.</summary>
public record AdminGiveItemCmd(string ConnectionId, string TargetName, Guid InstanceId, int Quantity)
    : IGameCommand;

/// <summary>Admin -> server: destroy an item out of another player's bag (from the /bag window).</summary>
public record AdminRemoveItemCmd(string ConnectionId, string TargetName, Guid InstanceId) : IGameCommand;

public record TradeRequestCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record TradeRespondCmd(string ConnectionId, bool Accept) : IGameCommand;
public record TradeOfferCmd(string ConnectionId, Guid[] InstanceIds) : IGameCommand;
public record TradeReadyCmd(string ConnectionId) : IGameCommand;
/// <summary>Client -> server: set how much GOLD you're putting into the current trade.</summary>
public record TradeGoldCmd(string ConnectionId, long Gold) : IGameCommand;
public record TradeCancelCmd(string ConnectionId) : IGameCommand;
