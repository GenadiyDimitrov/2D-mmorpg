namespace Game.Shared;

// ---------------------------------------------------------------------------
// Network contracts. These records are serialized by SignalR (System.Text.Json)
// in both directions. Keep them flat and small — they go over the wire 10x/sec.
// ---------------------------------------------------------------------------

/// <summary>Client -> Server: enter the world with a character.</summary>
public record LoginRequest(string CharacterName, Race Race, BaseClass BaseClass);

/// <summary>Server -> Client: result of a login attempt.</summary>
public record LoginResult(
    bool Success,
    string? Error,
    Guid EntityId,
    float X,
    float Y,
    DateTime ServerEpochUtc = default);

/// <summary>One visible entity's state inside a snapshot.</summary>
public record EntityDto(
    Guid Id,
    string Name,
    EntityKind Kind,
    Race Race,
    BaseClass BaseClass,
    float X,
    float Y,
    float Speed,
    int Level,
    int Hp,
    int MaxHp,
    int Mp,
    int MaxMp,
    int SecondClass,
    int ThirdClass,
    bool Dead,
    // A link-dead player in the reconnect grace window: clients draw a "Disconnected" title
    // above the head. Offline-FARMING players are NOT flagged (they look like normal players).
    bool Disconnected = false,
    // PvP name colour: Innocent = white, Flagged = purple, Pk = red.
    PvpFlag Flag = PvpFlag.Innocent,
    // Mobs only: this one attacks on sight. Clients mark it with a "*" after the name so you can see
    // what to tiptoe around BEFORE it decides for you. Cached on the entity at spawn, so this costs a
    // bool per snapshot and no catalog lookups.
    bool Aggressive = false);

/// <summary>Client -> Server: "move me toward this point" (click-to-move).
/// Moving cancels engagement, queued skills, and casting (classic MMO).</summary>
public record MoveCommand(float TargetX, float TargetY);

/// <summary>Server -> Client, every tick: everything you can currently see
/// (including yourself). Anything not listed has left your view range.
/// SUPERSEDED for the live path by <see cref="SnapshotDelta"/> (kept for reference/compat).</summary>
public record WorldSnapshot(EntityDto[] Entities);

/// <summary>The fields of an entity that change tick-to-tick — the LEAN per-tick update. The STATIC
/// fields (name, class, level, max HP/MP, aggressive, …) are sent ONCE as a full <see cref="EntityDto"/>
/// spawn and never repeated, so this is all the wire needs while an entity is just moving/fighting.</summary>
public record EntityLean(
    Guid Id, float X, float Y, float Speed,
    int Hp, int Mp, bool Dead, bool Disconnected, PvpFlag Flag);

/// <summary>Server -> Client, every tick: a DELTA of the viewer's world.
///   Spawns   = entities that just ENTERED view (or whose static data changed) — full DTOs.
///   Updates  = entities still in view whose dynamic fields changed — lean.
///   Despawns = entities that LEFT view (or were removed).
/// An entity absent from all three is UNCHANGED — the client keeps what it has (unlike WorldSnapshot,
/// where absence meant "removed"). This stops re-sending ~11 static fields per entity 10×/second.</summary>
public record SnapshotDelta(EntityDto[] Spawns, EntityLean[] Updates, Guid[] Despawns);

/// <summary>Server -> Client: a chat line. To is set for whispers.</summary>
public record ChatMessage(string From, string Text, ChatChannel Channel, string? To = null);

/// <summary>Server -> Clients near the fight: one resolved combat action.
/// Damage doubles as the heal amount for Heal; Skill is set for skill-based
/// outcomes (and carries the buff/debuff name for Buff).</summary>
public record CombatEvent(
    Guid AttackerId,
    string AttackerName,
    Guid TargetId,
    string TargetName,
    int Damage,
    CombatOutcome Outcome,
    string? Skill = null);

/// <summary>Server -> the owning client: exp/level progress after a kill.</summary>
public record ProgressUpdate(
    int Level,
    long Exp,
    long ExpToNext,
    bool LeveledUp);

/// <summary>Server -> the casting client: show/update the cast bar.
/// Seconds &lt;= 0 means the cast was cancelled — hide the bar.</summary>
public record CastInfo(string SkillName, float Seconds);

/// <summary>Server -> a fallen player: an ally (or a scroll) offers to resurrect you. The client shows a
/// confirm prompt; the player accepts/declines (see ResurrectResponse) so they don't revive on top of the
/// mob that killed them. ExpPct is the fraction of lost exp restored; ExpRestored is the resulting amount.</summary>
public record ResurrectOffer(string FromName, float ExpPct, long ExpRestored);

/// <summary>Server -> nearby clients: a MOB started casting (drives a cast bar over the mob's head,
/// so a boss's telegraphed slam is visible/dodgeable). Seconds 0 = the cast ended/was cancelled.</summary>
public record MobCastInfo(Guid CasterId, string SkillName, float Seconds);


/// <summary>One item instance in a player's inventory.</summary>
public record InventoryItemDto(Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity, ItemAttribute[] Attributes);

/// <summary>Server -> owning client: full inventory sync (sent on change).</summary>
public record InventoryUpdate(InventoryItemDto[] Items);

/// <summary>Server -> client: someone wants to trade with you.</summary>
public record TradeRequestNotice(Guid FromId, string FromName);

/// <summary>Server -> both traders: full trade state (sent on every change).
/// Active=false closes the trade window.</summary>
public record TradeStateUpdate(
    bool Active,
    string PartnerName,
    InventoryItemDto[] MyOffer,
    InventoryItemDto[] TheirOffer,
    bool MyReady,
    bool TheirReady,
    long MyGold = 0,
    long TheirGold = 0);


/// <summary>Server -> owning client: full derived stats for the Stats window.
/// Sent whenever stats change (level, equip, class change).</summary>
public record StatsUpdate(
    int Con, int Atk, int Wit, int Dex,
    int MaxHp, int MaxMp, int AttackPower, int Defence,
    int Accuracy, int Evasion, float CritChance, float BasicAttackRange,
    int SecondClass, float MoveSpeed, float CastModifier,
    float CastSpeedMult, float AttackSpeedMult, int SkillPoints, MoveState MoveState,
    int MagicAttack, float MagicCritChance,
    bool HasShield, float BlockChance, float BlockReduction, int ShieldDefense,
    int MagicDefence, string ActiveSet, string ArmorMastery,
    // Extended debug stats (regens per second + the buff/effect layer).
    float HpRegen = 0f, float MpRegen = 0f, float CritDamage = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float CooldownReduction = 0f,
    float MagicFailResist = 0f, float MagicFailFloor = 0f,
    float CritRateResist = 0f, float CritDmgResist = 0f, float BowResist = 0f,
    int InterruptResist = 0,
    // DEBUG / L2-reference: the OLD-style internal M.Atk (base·levelMod²·buffs²) the shrunk display hides.
    int MagicAttackInternal = 0,
    // Heal stats (no M.Atk): output = (HealPowerFlat + skillPower)·HealPowerMod; received = (HealReceivedFlat
    // + output)·HealReceivedMod. Default 0/×1.
    int HealPowerFlat = 0, float HealPowerMod = 1f,
    int HealReceivedFlat = 0, float HealReceivedMod = 1f);

/// <summary>Server -> owning client: a potion cooldown started (seconds),
/// or an active potion effect changed. Cooldown 0 = ready.</summary>
public record PotionStatus(float CooldownSeconds, string ActiveEffect);


/// <summary>One active buff/debuff on the player, for the buff bar + tooltip. Stacks &gt; 1
/// for a stacking effect (shown as "Name xN"). Icon = an emoji/glyph for the square (server-resolved,
/// per-class); "" falls back to the name's initials on the client.</summary>
public record BuffDto(string Name, string Description, float SecondsLeft, bool IsDebuff,
    string Key = "", int Stacks = 1, BuffRow Row = BuffRow.Buff, string Icon = "");

/// <summary>Server -> client: the character's learned skills (id + current level) + SP.</summary>
public record LearnedSkills(SkillRef[] Skills, int SkillPoints);

/// <summary>A learned skill reference: its id and the level the character has it at.</summary>
public record SkillRef(string Id, int Level);

/// <summary>Server -> owning client: the player's current buffs (sent each
/// second while any are active, and once when the last one drops).</summary>
public record BuffUpdate(BuffDto[] Buffs);

/// <summary>Server -> owning client: a SELECTION box was opened — show a chooser. The
/// player picks PickCount of Options, then calls SelectBoxItems with the chosen ids.</summary>
public record SelectionOffer(Guid BoxInstanceId, string BoxName, SelectionOption[] Options, int PickCount);
public record SelectionOption(string ItemId, string Name);

/// <summary>Server -> owning client: the expanded target window (L2-style inspect) —
/// the target's detailed stats and, for a mob, its passive modifier lines.</summary>
public record TargetDetails(
    Guid Id, string Name, int Level, bool IsMob,
    int Hp, int MaxHp, int Mp, int MaxMp,
    int PAtk, int MAtk, int PDef, int MDef,
    int Accuracy, int Evasion, float CritChance,
    float BowResist, float CritResist,
    string[] Passives,
    // Active temporary effects on the target (incl. DoT stack counts), e.g. "Bleed x5",
    // "Slow" — so a Venomweaver/Tempest can read stacks on the enemy.
    string[] Effects,
    // For a MOB only: its level-appropriate drop list, "ItemName (chance%)" (effective chance, after the
    // global drop-rate). Empty for players. Shown behind the [Details] button in the mob target window.
    string[]? Drops = null);

/// <summary>Server -> owning client: the result of an enchant attempt.</summary>
public record EnchantResultDto(string ItemName, int NewEnchant, string Outcome, bool Destroyed);

/// <summary>Server -> owning client: an attribute reroll finished (inventory update
/// carries the new attributes; this drives the reroll popup refresh + a message).</summary>
public record RerollResultDto(string ItemName, string Outcome);

/// <summary>Server -> owning client: the player's gold wallet balance (sent on entry
/// and whenever it changes — kills, quest rewards, vendor buy/sell, teleport fees).</summary>
public record GoldUpdate(long Gold);

/// <summary>Server -> owning client: an incoming party invite from Inviter (accept/decline). Carries
/// the loot rule the invitee would be joining under so they can decide before accepting.</summary>
public record PartyInviteDto(Guid InviterId, string InviterName,
    LootMode LootMode = LootMode.Random);

/// <summary>Server -> a party member: the leader proposes a loot-rule change and needs everyone to
/// agree. Open=true shows the accept/decline prompt; Open=false dismisses it (vote resolved).</summary>
public record PartyLootVoteDto(LootMode Mode, string RequestedBy, bool Open = true);


// ----- Auto-hunt / idle farming (docs/AutoHunt.md) -------------------------

/// <summary>One auto-use skill: the skill id, whether it's on, and an ADDITIONAL post-cast delay
/// (ticks, ≥0) on top of the skill's own reuse (so auto-reuse is never below the default).</summary>
public record AutoSkillDto(string SkillId, bool Enabled, int ExtraDelayTicks);

/// <summary>One class a character owns (an L2-style subclass). Server → client, so the UI can list
/// them and let you swap. <paramref name="Active"/> = the one being played right now.</summary>
public record SubclassDto(
    int Slot, Race Race, BaseClass BaseClass, int SecondClass, int ThirdClass, int Level, bool Active);

/// <summary>Every class this character owns. Pushed on login and after any add/swap.</summary>
public record SubclassListDto(SubclassDto[] Classes);

/// <summary>The character's skill-bar layout: one entry per slot, "" = empty. Travels BOTH ways —
/// server → client on login (restore), client → server on every rearrangement (persist).
///
/// The bar is CHARACTER data, not a client preference. It used to live in the WPF client's
/// client-settings.json, which meant it did not follow the account to another machine, and its load
/// raced the first Learned push on login — which is what silently reshuffled the bar. The server now
/// owns it. (It does not USE it: casting is by skill id, not slot. It just stores it.)</summary>
public record SkillBarDto(string[] Slots);

/// <summary>Client -> server: the character's full auto-hunt configuration. The use CONDITION for
/// each skill is inferred server-side (buff→if missing, debuff→if target lacks, attack→on cd). The
/// new roaming fields default to sensible values until a settings window exposes them.</summary>
public record AutoHuntConfigDto(
    bool Enabled,
    int HpPotionPct,
    int MpPotionPct,
    bool AutoBuffPotions,
    AutoSkillDto[] Skills,
    string[] BuffPotionIds,
    int FarmRange = 1000,          // radius the auto-hunt searches (clamped [200, 2000])
    bool StaticSpot = false,       // false = roam (scan follows the char); true = fixed circle at the start
    bool AttackNormal = true,      // engage normal-rank mobs
    bool AttackElite = false,      // engage elites
    bool AttackBoss = false);      // engage bosses

/// <summary>The pseudo skill-id for "basic attack" as an opt-in auto action: put it in
/// <see cref="AutoHuntConfigDto.Skills"/> (enabled) and the auto-hunt will melee when no real skill
/// is ready; leave it out/disabled and the character only casts skills (mage style).</summary>
public static class AutoHuntIds
{
    public const string BasicAttack = "basic_attack";
}

/// <summary>Server -> client HUD: an enabled auto-skill's effective reuse and its MP/s draw.</summary>
public record AutoSkillReuse(string SkillId, string Name, float ReuseSeconds, float MpPerSec);

/// <summary>Server -> client HUD: total MP/s of all enabled auto-skills (after cost/CD-reduction
/// buffs) + the per-skill breakdown, refreshed as buffs change.</summary>
public record AutoHuntStatus(bool Enabled, float MpPerSec, AutoSkillReuse[] Skills);

/// <summary>Server -> client: the result of an exit/logout request. Ok=false when blocked (e.g.
/// in combat); the client keeps playing and shows Reason. Ok=true → the client may close.</summary>
public record LogoutResult(bool Ok, string Reason);

/// <summary>Server -> client: the player's PvP toggles + reputation (karma / kill counts) for the HUD.</summary>
public record PvpState(bool Pvp, bool CounterAttack, int Karma = 0, int PkCount = 0, int PvpCount = 0);

/// <summary>Admin-only live-tuning knobs (Debug settings panel). Runtime only — the final values get
/// moved back into the code defaults. Round-trips both ways (server sends current, client applies).</summary>
public record DebugConfigDto(
    float ExpRate, float SpRate, float DropChanceRate, float DropAmountRate, float GoldRate,
    int KarmaBase, float KarmaConsecGrowth, float KarmaLevelGrowth, int KarmaLossPerDeath, int KarmaLossPerMob,
    int IdleCapSeconds, int OfflineCapSeconds, int GraceSeconds,
    // Test skills: the two debug damage skills read Flat=TestSkillPower, Mod=TestSkillMod; testheal heals
    // TestHealPower. Lets the owner read the {Flat, Mod} damage curve live before authoring real skills.
    int TestHealPower = 1000, int TestSkillPower = 0, float TestSkillMod = 1f);

/// <summary>One member row in the party window. Debuffs = the names of the debuffs currently on this
/// member, so a healer sees at a glance who to cleanse without selecting each one.</summary>
public record PartyMemberDto(Guid Id, string Name, int Level, string ClassName,
    int Hp, int MaxHp, int Mp, int MaxMp, bool IsLeader,
    PartyMemberStatus Status = PartyMemberStatus.Online,
    string[]? Debuffs = null);

/// <summary>Server -> party members: the current roster (empty array = you left/were the last
/// member, so the client hides the party window). Sent on membership change and refreshed
/// periodically for live HP/MP bars.</summary>
public record PartyUpdate(PartyMemberDto[] Members, LootMode LootMode = LootMode.Random);


// ----- Accounts & character selection (Phase 5) ----------------------------

/// <summary>Client -> Server: register or login.</summary>
public record AuthRequest(string Username, string Password);

/// <summary>Server -> Client: auth outcome. Token is the account id used for
/// subsequent character calls within this connection.</summary>
public record AuthResponse(bool Success, string? Error, bool IsAdmin);

/// <summary>One character on the account, for the selection screen. PendingDeleteAt
/// (UTC) is set when the character is scheduled for deletion; null = active.</summary>
public record CharacterSlot(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass,
    int Level, DateTime? PendingDeleteAt = null);

/// <summary>Server -> Client: the account's characters.</summary>
public record CharacterList(CharacterSlot[] Characters);

/// <summary>Client -> Server: create a new character on the account.</summary>
public record CreateCharacterRequest(string Name, Race Race, BaseClass BaseClass);

/// <summary>Client -> Server: enter the world with one of the account's characters.</summary>
public record EnterWorldRequest(int CharacterId);


// ----- Quests (Phase 7) ----------------------------------------------------

/// <summary>Client -> server: talk to an NPC (open dialog).</summary>
public record TalkToNpcRequest(Guid NpcEntityId);

/// <summary>Client -> server: accept / complete / change-class actions.</summary>
public record QuestActionRequest(string Action, string Id, Guid NpcEntityId);

/// <summary>One quest line in an NPC dialog or the quest log. <see cref="Location"/>
/// is a short "who/where" hint for the current step (e.g. "Elder Marius — Brackenford"
/// or "Grey Wolf — near Brackenford (Lv 1-10)"); "" when there's nothing useful to say.</summary>
public record QuestSummary(string Id, string Name, string Description, string CurrentStepText,
    int StepIndex, int StepCount, int Counter, int CounterNeeded, bool Completed, bool CanComplete,
    string Location = "");

/// <summary>A class-change option shown by a class-change NPC. Description is a
/// "what this class does" blurb so the player can choose before committing.</summary>
public record ClassChangeOption(int SecondClassId, string ClassName, bool Meets,
    string[] RequiredItemNames, bool[] HasItem, string Description = "");

/// <summary>One buyable line in a vendor shop.</summary>
public record ShopItemDto(string DefId, string Name, int BuyPrice);

/// <summary>A vendor's wares, attached to the dialog when talking to a vendor.</summary>
public record ShopInfo(string Title, ShopItemDto[] Items);

/// <summary>One teleport destination offered by a gatekeeper. MinLevel/MaxLevel are
/// the level band of the hunting grounds around that town (0/0 = unknown), shown so
/// players know where they're going.</summary>
public record TeleportDest(string ZoneId, string Name, int Fee, int MinLevel = 0, int MaxLevel = 0);

/// <summary>A gatekeeper's destinations, attached to the dialog.</summary>
public record TeleportInfo(TeleportDest[] Destinations);

/// <summary>Server -> client: the dialog when talking to an NPC.</summary>
public record NpcDialog(
    string NpcName,
    string NpcRole,
    QuestSummary[] Offered,      // quests this NPC can give now
    QuestSummary[] Turnable,     // active quests ready to complete here
    QuestSummary[] InProgress,   // active quests not yet complete
    ClassChangeOption[] ClassChanges,
    ShopInfo? Shop = null,       // vendor wares (null for non-vendors)
    TeleportInfo? Teleport = null, // gatekeeper destinations (null for non-gatekeepers)
    SkillResetInfo? SkillReset = null, // un-learnable skills (null for non-reset NPCs)
    BufferInfo? Buffer = null); // buffer options (null for non-buffers)

/// <summary>Server -> client: the skills a reset NPC can un-learn — the permanent, mutually-
/// exclusive picks (the level-40 stat swaps). Removing is FREE, but the gold you spent is NOT
/// refunded; it only frees the group so you can commit again.</summary>
public record SkillResetInfo(ResettableSkill[] Skills);
public record ResettableSkill(string SkillId, string Name, int Level, int GoldSpent);

/// <summary>Server -> client: what the NPC BUFFER offers. Three options: full-buff (all at once),
/// a single buff from the list, and HP/MP restore. Free at ≤40, priced above (see the costs).</summary>
public record BufferInfo(
    bool CanBuff,           // level within the buffer's 6-75 window
    string Message,         // shown when CanBuff is false (too low / too high)
    long FullBuffCost,      // 0 = free
    long RestoreCost,       // cost to restore HP+MP right now (0 = free / already full)
    BufferBuff[] Buffs);    // single buffs, each with its own cost
public record BufferBuff(string SkillId, string Name, long Cost);

/// <summary>Server -> client: the full quest log.</summary>
public record QuestLog(QuestSummary[] Active, string[] Completed);
