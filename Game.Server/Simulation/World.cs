using System.Collections.Concurrent;
using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A live trade between two players. Owned by the loop thread.</summary>
public class TradeSession
{
    public required Entity A { get; init; }
    public required Entity B { get; init; }
    /// <summary>What each side has on the table: item instance + HOW MANY of it (a stack can be
    /// offered in part). The server owns these lists; the clients only propose replacements.</summary>
    public List<TradeOfferEntry> OfferA { get; } = new();
    public List<TradeOfferEntry> OfferB { get; } = new();
    public bool ReadyA { get; set; }
    public bool ReadyB { get; set; }

    /// <summary>Gold each side is putting into the trade (so you can PAY for what the other gives).</summary>
    public long GoldA { get; set; }
    public long GoldB { get; set; }

    public Entity PartnerOf(Entity e) => e == A ? B : A;
    public List<TradeOfferEntry> OfferOf(Entity e) => e == A ? OfferA : OfferB;

    /// <summary>Is this instance on the table for that side? Guards the ways an item could otherwise
    /// leave a bag mid-trade (sell, deposit, destroy) while still showing in the partner's window.</summary>
    public bool Offers(Entity e, Guid instanceId)
    {
        foreach (var entry in OfferOf(e))
            if (entry.InstanceId == instanceId) return true;
        return false;
    }
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

/// <summary>A placed TOTEM (the Ork healer's totem line). Server-only for now, exactly like
/// <see cref="TrapInstance"/> — a dedicated visual is client work, and until it exists a totem is felt
/// (allies visibly heal / refill) rather than seen. The mirror image of a trap: a trap waits once for an
/// ENEMY and dies, a totem pulses repeatedly at ALLIES on a timer until its life runs out.
/// <para>⚠ Deliberately NOT an Entity. A new <c>EntityKind</c> would have to be audited through ~137
/// server call sites, 54 of which ask "is this a mob / not a player" — a totem would silently become a
/// valid aggro, damage or loot target in whichever one was missed. PETS are the case that will justify
/// paying that cost, because a pet must move, fight and be targetable; a totem needs none of it.</para></summary>
public class TotemInstance
{
    /// <summary>Identity for the CLIENT's benefit — the key it keeps a drawn disc under, so a recast
    /// that moves a totem replaces the circle instead of leaving a ghost behind. The server itself
    /// still addresses a totem by (owner, skill), which is what "one per owner per skill" means.</summary>
    public Guid Id { get; } = Guid.NewGuid();
    public required Guid OwnerId { get; init; }
    public required string SkillId { get; init; }
    public int Level { get; init; } = 1;
    public float X { get; init; }
    public float Y { get; init; }
    public float Radius { get; init; }
    /// <summary>WHICH POOL this totem fills, snapshotted from the placing skill's own Effect:
    /// <see cref="SkillEffect.Heal"/> = HP (Healing Totem), <see cref="SkillEffect.RestoreMp"/> = MP
    /// (Mana Totem). A skill carrying both pulses both. Snapshotted rather than looked up each pulse
    /// for the same reason <see cref="PulseAmount"/> is: a totem is an OBJECT, and what it does was
    /// decided when it was planted.</summary>
    public SkillEffect Effect { get; init; } = SkillEffect.Heal;
    /// <summary>Amount applied to each ally in radius per pulse (the skill's Power at its level) —
    /// HP or MP according to <see cref="Effect"/>.</summary>
    public int PulseAmount { get; init; }
    /// <summary>Ticks between pulses, and the countdown to the next one.</summary>
    public int PulseTicks { get; init; } = 10;
    public int NextPulseIn { get; set; }
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

    /// <summary>accountId -> the ACCOUNT-wide bank, shared by every character on that account.
    /// Loaded once (first login of any of its characters) and kept live from then on: two characters
    /// of one account can be in the world at the same time — offline farming makes that ordinary —
    /// and they must see ONE list, not two copies of it that quietly diverge.</summary>
    public Dictionary<int, List<InventoryItem>> AccountWarehouses { get; } = new();

    /// <summary>accountId -> the shared daily farm allowance. Same lifetime rule as
    /// <see cref="AccountWarehouses"/>: loaded on the first login of any of the account's characters
    /// and live from then on, because several of them can be spending it at once.</summary>
    public Dictionary<int, AccountFarmBudget> AccountBudgets { get; } = new();

    /// <summary>Every party MEMBER id maps to the shared <see cref="Party"/> object.</summary>
    public Dictionary<Guid, Party> Parties { get; } = new();

    /// <summary>Live placed traps (Trapper). Scanned each tick for intruders.</summary>
    public List<TrapInstance> Traps { get; } = new();

    /// <summary>Live placed totems (healer). Pulsed each tick at the allies standing in them.</summary>
    public List<TotemInstance> Totems { get; } = new();

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

/// <summary>
/// A command only an ADMIN character may run — the whole former "debug menu".
///
/// These used to be compiled out with <c>#if DEBUG</c> in the hub, which meant the tools the owner
/// actually uses to get a build into a testable state simply did nothing in the RELEASE server published
/// to the phone: the buttons were there, the calls arrived, and nothing happened (owner, 2026-07-30).
/// A compile flag was the wrong gate anyway — the question was never "is this a debug build" but "is
/// this character an admin", which is a runtime fact the server already tracks.
///
/// So they are admin commands now, present in every build and authorised by
/// <see cref="Entity.IsAdmin"/>. Implementing this marker is the ONLY thing a command has to do to be
/// gated: <c>ProcessCommands</c> checks it once, centrally, before dispatch. A per-handler check would
/// mean fifteen places to forget, and forgetting one in a shipped build hands a player free levels.
///
/// What stays <c>#if DEBUG</c> is only what has no admin to authorise it: account REGISTRATION and the
/// admin/test account SEEDING (both run before anyone is logged in), and the destructive stale-schema
/// database reset.
/// </summary>
public interface IAdminCommand : IGameCommand
{
    string ConnectionId { get; }
}

/// <summary><paramref name="AccountBank"/> is the account warehouse as READ FROM THE DB during login.
/// The loop adopts it only if that account has no live list yet — a list already in memory is newer
/// than anything on disk (another character of the same account may be standing in it).
/// <paramref name="AccountBudget"/> is the daily farm allowance, read on the same async pass and
/// adopted under exactly the same rule (an offline farmer is already spending the live one).</summary>
public record EnterWorldCommand(
    string ConnectionId,
    Entity Entity,
    TaskCompletionSource<LoginResult> Result,
    List<InventoryItem>? AccountBank = null,
    AccountFarmBudget? AccountBudget = null) : IGameCommand;

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

/// <summary>DISASSEMBLE one piece of gear into crafting materials (`BL-22`) — *"rarity for mats
/// rarity, grade for mats ammount"*, and *"u give up gold to get mats"*.
///
/// <para>⚠ Deliberately NOT a vendor command: no <c>NpcEntityId</c>. Selling needs a shop to sell to;
/// breaking your own sword does not, and requiring a walk to town would make the alternative to
/// selling strictly worse than selling instead of a genuine choice between them. It is done from the
/// bag, anywhere, like opening a box.</para></summary>
public record DisassembleItemCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Pay a gatekeeper to warp to a safe zone.</summary>
public record TeleportCmd(string ConnectionId, Guid NpcEntityId, string ZoneId) : IGameCommand;

/// <summary>Ask a skill-reset NPC to UN-LEARN a permanent, mutually-exclusive skill (a level-40
/// stat swap), freeing its group so a different trade-off can be committed to. Free to do — the
/// gold already spent is NOT refunded.</summary>
public record ForgetSkillCmd(string ConnectionId, Guid NpcEntityId, string SkillId) : IGameCommand;

/// <summary>Buy a whole BASKET of stat-swap rungs in one charge (the Stats tab, BL-03). All-or-nothing:
/// the tab exists so a nine-rung build can be planned and priced before any of it is paid for, and a
/// basket that fails halfway would commit a build the player never chose — one only the Mindwriter can
/// undo, a whole pair at a time. Rungs may still be bought one at a time via LearnSkill.</summary>
public record BuyStatSwapsCmd(string ConnectionId, StatSwapPurchaseDto[] Picks) : IGameCommand;

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

/// <summary>Apply an attribute scroll to a target item. The item holds AT MOST ONE attribute;
/// what the scroll does to it (create / re-roll the value / re-roll high / max) is the scroll
/// kind's business — see AttributeSystem.ApplyScroll. There is no lock any more.</summary>
public record RerollAttributesCmd(string ConnectionId, Guid ScrollInstanceId,
    Guid TargetInstanceId) : IGameCommand;

/// <summary>Destroy an inventory item (later: sell/dismantle).</summary>
/// <summary>Destroy an item. <paramref name="All"/> = the whole stack; otherwise ONE from the stack
/// (a single item is removed either way).</summary>
// Quantity > 0 removes exactly that many from a stack (the bin numpad); 0 falls back to All (whole
// stack) / single-unit. All still wins for a non-stackable.
public record RemoveItemCmd(string ConnectionId, Guid InstanceId, bool All = false, int Quantity = 0) : IGameCommand;

/// <summary>Undo a bin-delete: put a recently destroyed item back, for free. Deliberately carries NO
/// npc id — you bin things in the field, so the undo must work there (playtest-17 C18).</summary>
public record RestoreItemCmd(string ConnectionId, int Index) : IGameCommand;

/// <summary>Open the private warehouse (fetch its contents). Gated to safe zones.</summary>
public record OpenWarehouseCmd(string ConnectionId) : IGameCommand;
/// <summary>Move a whole item instance bag → warehouse.</summary>
public record WarehouseDepositCmd(string ConnectionId, Guid InstanceId) : IGameCommand;
/// <summary>Move a whole item instance warehouse → bag.</summary>
public record WarehouseWithdrawCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>Open the ACCOUNT warehouse (fetch its contents). Same safe-zone gate as the private one.</summary>
public record OpenAccountWarehouseCmd(string ConnectionId) : IGameCommand;
/// <summary>Move a whole item instance bag → account warehouse. Tradable items only; a NEW slot
/// costs <see cref="GameConstants.AccountWarehouseSlotFee"/> gold.</summary>
public record AccountWarehouseDepositCmd(string ConnectionId, Guid InstanceId) : IGameCommand;
/// <summary>Move a whole item instance account warehouse → bag. Free.</summary>
public record AccountWarehouseWithdrawCmd(string ConnectionId, Guid InstanceId) : IGameCommand;

/// <summary>DEBUG-only: grant an item by def id.</summary>
/// <summary>Admin grant. <b>Quantity is ON the command</b> (66n): the hub used to enqueue one command
/// per UNIT, so "every material x500" became 12 500 commands, each granting one item and serialising
/// the WHOLE inventory afterwards — *"i see each sinlge item increasing 1 by 1 500 times and going to
/// the next ... now the game is Stalled (had to restart)"*. One command, one push.</summary>
public record DebugGiveCmd(string ConnectionId, string DefId, int Quantity = 1) : IAdminCommand;

/// <summary>DEBUG-only: set one item's enchant OUTRIGHT (the `/enchant &lt;value&gt;` picker, D2).
/// Deliberately unrestricted — no grade band, no scroll, no success roll and no MaxEnchant, because
/// the whole point is to reach states the scroll ladder cannot (the owner's own example is
/// `/enchant 999999` on an F weapon, which no scroll may touch at all).</summary>
public record AdminEnchantCmd(string ConnectionId, Guid InstanceId, int Value) : IAdminCommand;

/// <summary>DEBUG-only: strip an attribute off the EQUIPPED weapon (Index = which; -1 = all).
/// Lets you test with only the base weapon / a chosen attribute, not the full rolled set.</summary>
public record DebugCancelAttrCmd(string ConnectionId, int Index) : IAdminCommand;

/// <summary>Craft a recipe (consume its inputs, roll success, produce the output).</summary>
public record CraftCmd(string ConnectionId, string RecipeId) : IGameCommand;

/// <summary>Take a master's profession WITHOUT re-doing his joining quest — open only to someone who has
/// completed it once before (`BL-05`). Addressed by the master's live ENTITY id and range-checked, like
/// every other NPC service: the profession is granted at the man, not from a menu.</summary>
public record JoinProfessionCmd(string ConnectionId, Guid NpcEntityId) : IGameCommand;

/// <summary>Quit the character's profession at his own master, losing every crafting level (`BL-05`).</summary>
public record QuitProfessionCmd(string ConnectionId, Guid NpcEntityId) : IGameCommand;

/// <summary>DEBUG-only: set the player's crafting profession (until level-based assignment lands).</summary>
/// <summary>Set the CRAFTING profession (WeaponSmith … ScrollScribe). Not the class — see
/// <see cref="DebugSecondClassCmd"/>. The two were confused in the debug UI, which sent a 2nd-class id
/// (1-18) here, where it was clamped into the 5-value crafting enum and silently became ScrollScribe.</summary>
public record DebugSetProfessionCmd(string ConnectionId, int Profession) : IAdminCommand;

/// <summary>DEBUG-only: jump straight to a crafting LEVEL (1-6), skipping the exp grind (`BL-05`).
/// The band freeze still applies — a level-20 character set to L6 reads as L2, which is the point:
/// this is for testing the ladder, not for stepping over it.</summary>
public record DebugSetCraftLevelCmd(string ConnectionId, int Level) : IAdminCommand;

/// <summary>Debug: become a 2nd CLASS directly, skipping the quest and level gates the real
/// class-change path enforces.</summary>
public record DebugSecondClassCmd(string ConnectionId, int ClassId) : IAdminCommand;

/// <summary>DEBUG-only: grant one level.</summary>
/// <summary>DEBUG: shift the character's level by <paramref name="Delta"/> (+1 / +10 / −1 / −10).
/// Negative = delevel, which keeps every learned skill (see HandleDebugLevel).</summary>
public record DebugLevelCmd(string ConnectionId, int Delta) : IAdminCommand;

/// <summary>DEBUG-only: learn every skill the class can learn at the current level (free).</summary>
public record DebugLearnAllCmd(string ConnectionId) : IAdminCommand;

/// <summary>DEBUG-only: grant gold.</summary>
public record DebugGoldCmd(string ConnectionId, long Amount) : IAdminCommand;

/// <summary>DEBUG: apply the full NPC buff set to yourself, at any level, without visiting the NPC.</summary>
public record DebugBuffCmd(string ConnectionId) : IAdminCommand;

/// <summary>DEBUG: nudge your karma by a delta (test the red-name gradient + clearing).</summary>
public record DebugKarmaCmd(string ConnectionId, int Delta) : IAdminCommand;

/// <summary>DEBUG: add a new SUBCLASS (a second/third class this character owns) and switch to it.
/// No cap, no delay, no safe-zone requirement — the real rules come with the player-facing system.</summary>
/// <summary>Add a SUBCLASS by its 3rd-class discipline id (a ThirdClassCatalog id). The new class
/// starts at level 1 but with that 3rd class already approved (race/base/2nd derived from it).</summary>
public record DebugAddSubclassCmd(string ConnectionId, int ThirdClassId) : IAdminCommand;

/// <summary>DEBUG: switch to another class this character already owns.</summary>
public record SwitchSubclassCmd(string ConnectionId, int Slot) : IAdminCommand;

/// <summary>DEBUG-only: grant skill points.</summary>
public record DebugSpCmd(string ConnectionId, long Amount) : IAdminCommand;

/// <summary>DEBUG-only: re-roll the current character (new race/base class, reset to
/// level 1 with the starter kit; keeps the same character row + gold).</summary>
public record DebugResetCmd(string ConnectionId, Race Race, BaseClass BaseClass) : IAdminCommand;

/// <summary>DEBUG-only: take a 3rd class (discipline) directly, bypassing the quest
/// chain + items. Parent 2nd class must already match the discipline.</summary>
public record DebugThirdClassCmd(string ConnectionId, int ThirdClassId) : IAdminCommand;

/// <summary>DEBUG-only: take the 4th class directly, bypassing the 100kk Rite of Ascension and the
/// walk to Frostmere. No id argument — a discipline has exactly one ascension, so the only thing
/// this can mean is "ascend the class I am". Toggles: run it again to drop back to the 3rd class,
/// because testing the 76 gate needs BOTH directions and there is no other way back.</summary>
public record DebugFourthClassCmd(string ConnectionId) : IAdminCommand;

/// <summary>Debug-menu teleport to arbitrary world coordinates.</summary>
public record DebugTeleportCmd(string ConnectionId, float X, float Y) : IAdminCommand;

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
/// <summary>`/ptinv &lt;name&gt;` — the name is resolved SERVER-side over every online player, so an
/// invite reaches someone out of view (playtest-19 46d).</summary>
public record PartyInviteByNameCmd(string ConnectionId, string Name) : IGameCommand;
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
/// <summary>Deliberate return to character select. The completion source carries the REFUSAL REASON
/// (null = left successfully), and the hub awaits it for two reasons:
/// • the character SAVE is a background write and the client lists characters the moment this
///   returns, so without waiting the select screen showed the level/class from BEFORE the session;
/// • leaving is refused while in combat — including while a DoT is ticking — and the client has to
///   learn that, or it would sit on the character screen while the entity stayed in the world.</summary>
public record LeaveWorldCmd(string ConnectionId,
    TaskCompletionSource<string?>? Result = null) : IGameCommand;

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
/// <summary>Client -> server: wear the title of this leaderboard category, "" for none, or
/// <see cref="TitleCatalog.Custom"/> for the one you wrote. Refused (with a system line) if the
/// character does not currently hold that board.</summary>
public record SetTitleCmd(string ConnectionId, string Category) : IGameCommand;

/// <summary>Client -> server: `/title &lt;text&gt;` — write your own title and wear it. Empty text
/// clears it. Refused unless the character has been granted the right; the text is validated
/// server-side (<see cref="TitleCatalog.IsValidCustom"/>), never on the client's say-so.</summary>
public record SetCustomTitleCmd(string ConnectionId, string Text) : IGameCommand;

/// <summary>Client -> server: `/titlecolor &lt;name&gt;` — recolour the title you wrote, from
/// <see cref="TitleCatalog.Palette"/>.</summary>
public record SetTitleColorCmd(string ConnectionId, string Color) : IGameCommand;

/// <summary>Worker thread -> the single writer: the boards were just re-read, and these characters
/// hold these title categories (by NAME). Carries the whole answer rather than a "go look" signal so
/// the tick thread never touches the DB — same shape as the moderation commands below.</summary>
public record TitleHoldersCmd(Dictionary<string, List<string>> Holders) : IGameCommand;

public record RequestDebugConfigCmd(string ConnectionId) : IAdminCommand;
public record SetDebugConfigCmd(string ConnectionId, DebugConfigDto Config) : IAdminCommand;

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
public record TradeOfferCmd(string ConnectionId, TradeOfferEntry[] Entries) : IGameCommand;
public record TradeReadyCmd(string ConnectionId) : IGameCommand;
/// <summary>Client -> server: set how much GOLD you're putting into the current trade.</summary>
public record TradeGoldCmd(string ConnectionId, long Gold) : IGameCommand;
public record TradeCancelCmd(string ConnectionId) : IGameCommand;
