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

    public Entity PartnerOf(Entity e) => e == A ? B : A;
    public List<Guid> OfferOf(Entity e) => e == A ? OfferA : OfferB;
    public bool ReadyOf(Entity e) => e == A ? ReadyA : ReadyB;
    public void SetReady(Entity e, bool value) { if (e == A) ReadyA = value; else ReadyB = value; }
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
/// stats (and, for a mob, its passive modifier lines).</summary>
public record InspectTargetCmd(string ConnectionId, Guid TargetId) : IGameCommand;

public record RespawnCmd(string ConnectionId) : IGameCommand;

/// <summary>Advance to a second class (level 20+, once).</summary>
public record ClassChangeCmd(string ConnectionId, int ClassId) : IGameCommand;

/// <summary>Equip or unequip an inventory item (toggles).</summary>
public record EquipCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Drink a potion from the inventory.</summary>
public record UsePotionCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Apply an enchant scroll to a target item.</summary>
public record EnchantCmd(string ConnectionId, Guid ScrollInstanceId, Guid TargetInstanceId) : IGameCommand;

/// <summary>Reroll a target item's rolled attributes with an attribute scroll,
/// locking the slots at LockedIndices (clamped to the scroll's lock capacity).</summary>
public record RerollAttributesCmd(string ConnectionId, Guid ScrollInstanceId,
    Guid TargetInstanceId, int[] LockedIndices) : IGameCommand;

/// <summary>Destroy an inventory item (later: sell/dismantle).</summary>
public record RemoveItemCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

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
public record DebugLevelCmd(string ConnectionId) : IGameCommand;

/// <summary>DEBUG-only: learn every skill the class can learn at the current level (free).</summary>
public record DebugLearnAllCmd(string ConnectionId) : IGameCommand;

/// <summary>DEBUG-only: grant gold.</summary>
public record DebugGoldCmd(string ConnectionId, long Amount) : IGameCommand;

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

// ----- Party / grouping -----
public record PartyInviteCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record PartyRespondCmd(string ConnectionId, bool Accept) : IGameCommand;
public record PartyLeaveCmd(string ConnectionId) : IGameCommand;
public record PartyKickCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record PartySetLootModeCmd(string ConnectionId, LootMode Mode) : IGameCommand;
public record PartyLootVoteCmd(string ConnectionId, bool Accept) : IGameCommand;
public record SetAutoHuntConfigCmd(string ConnectionId, AutoHuntConfigDto Config) : IGameCommand;

/// <summary>Client -> server: the player rearranged their skill bar; persist the new layout.</summary>
public record SetSkillBarCmd(string ConnectionId, string[] Slots) : IGameCommand;

/// <summary>Client -> server: DELIBERATELY leave the world and go back to character select, keeping
/// the connection. Distinct from <see cref="LeaveCommand"/>, which is the DISCONNECT path (link-dead
/// grace / offline farming) — a deliberate exit must actually remove the character from the world.</summary>
public record LeaveWorldCmd(string ConnectionId) : IGameCommand;
public record ToggleAutoHuntCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record LogoutCmd(string ConnectionId) : IGameCommand;
public record StartOfflineFarmCmd(string ConnectionId) : IGameCommand;
public record TogglePvpCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record ToggleCounterAttackCmd(string ConnectionId, bool Enabled) : IGameCommand;
public record RequestDebugConfigCmd(string ConnectionId) : IGameCommand;
public record SetDebugConfigCmd(string ConnectionId, DebugConfigDto Config) : IGameCommand;

public record TradeRequestCmd(string ConnectionId, Guid TargetId) : IGameCommand;
public record TradeRespondCmd(string ConnectionId, bool Accept) : IGameCommand;
public record TradeOfferCmd(string ConnectionId, Guid[] InstanceIds) : IGameCommand;
public record TradeReadyCmd(string ConnectionId) : IGameCommand;
public record TradeCancelCmd(string ConnectionId) : IGameCommand;
