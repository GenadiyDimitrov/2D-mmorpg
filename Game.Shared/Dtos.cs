namespace Game.Shared;

// ---------------------------------------------------------------------------
// Network contracts. These records are serialized by SignalR (System.Text.Json)
// in both directions. Keep them flat and small — they go over the wire 10x/sec.
// ---------------------------------------------------------------------------

/// <summary>Client -> Server: enter the world with a character.</summary>
public record LoginRequest(string CharacterName, Race Race, BaseClass BaseClass);

/// <summary>Server -> Client: result of a login attempt. <paramref name="Role"/> is the staff role of
/// the CHARACTER you just entered with (roles are per-character, not per-account) — the client uses it
/// only to decide which commands are worth sending; the server authorizes every one of them anyway.</summary>
public record LoginResult(
    bool Success,
    string? Error,
    Guid EntityId,
    float X,
    float Y,
    DateTime ServerEpochUtc = default,
    AccountRole Role = AccountRole.Player);

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
    bool Aggressive = false,
    // The leaderboard title this player is WEARING, already resolved to its display text ("the
    // Wealthy") — clients draw it over the head and never have to know the category ids. Empty for
    // everyone not wearing one, which is nearly everyone, so it costs an empty string per snapshot.
    string Title = "");

/// <summary>What to draw over an NPC's head about quests. Sent per player, because availability is
/// personal — level, race, class and what you have already done all decide it.</summary>
public enum QuestMarkState { None = 0, Available = 1, InProgress = 2, ReadyToHandIn = 3 }

/// <summary>One NPC's quest marker.</summary>
public record QuestMark(Guid NpcEntityId, QuestMarkState State);

/// <summary>Server -> owning client: which NPCs currently have something quest-shaped for YOU.
/// Rides alongside every QuestLog push, so it can never drift out of step with the log. The NPC
/// roster is small (a couple of dozen), so this sends every marked NPC rather than only the visible
/// ones — cheaper than tracking view state, and the marker is already right when one comes on screen.</summary>
public record QuestMarks(QuestMark[] Marks);

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

/// <summary>Server -> the owning client: exp/level progress after a kill.
///
/// SkillPoints rides along because SP is earned on the SAME event as exp, and this is the only push
/// that fires on every kill. It used to travel solely in StatsUpdate, which the kill path never sent,
/// so the SP figure sat at its login value for a whole session and only corrected on relog. Sending
/// the full ~45-field StatsUpdate per kill would fix it far more expensively.</summary>
public record ProgressUpdate(
    int Level,
    long Exp,
    long ExpToNext,
    bool LeveledUp,
    int SkillPoints = 0);

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
public record InventoryItemDto(Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity, ItemAttribute[] Attributes, DateTime? ExpiresAtUtc = null);

/// <summary>Server -> owning client: full inventory sync (sent on change).</summary>
public record InventoryUpdate(InventoryItemDto[] Items);

/// <summary>The character's private warehouse contents (same shape as the bag). Sent when the warehouse
/// window is opened and after every deposit/withdraw.</summary>
public record WarehouseUpdate(InventoryItemDto[] Items);

/// <summary>The ACCOUNT-wide warehouse, shared by every character on the account. Same shape as the
/// private one; the size cap and the per-slot deposit fee are constants both sides already know
/// (<see cref="GameConstants.AccountWarehouseSize"/>, <see cref="GameConstants.AccountWarehouseSlotFee"/>).</summary>
public record AccountWarehouseUpdate(InventoryItemDto[] Items);

/// <summary>Server -> client: someone wants to trade with you.</summary>
public record TradeRequestNotice(Guid FromId, string FromName);

/// <summary>Client -> server: ONE line of a trade offer — an item instance and HOW MANY of it.
/// Quantity is meaningful only for stackables (the server clamps it to 1..stack, and to 1 for gear),
/// which is what lets you put 20 of your 50 potions on the table instead of the whole stack.</summary>
public record TradeOfferEntry(Guid InstanceId, int Quantity);

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
    int Con, int Atk, int Wit, int Dex, int Spt,
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

/// <summary>One reuse timer for the action bar. <paramref name="Id"/> is the bar TOKEN it belongs
/// to — a skill id for a skill slot, an "item:defId" token for a consumable — so the client can
/// look it up with the token it already holds and needs no second mapping.
///
/// There is deliberately no "total" field: the push happens the tick the timer STARTS, so the first
/// Seconds the client sees for an id IS the full reuse. The client keeps that as the denominator and
/// only replaces it when Seconds jumps back UP (a restart) — which costs the server no extra state.</summary>
public record CooldownEntry(string Id, float Seconds);

/// <summary>Server -> owning client: every reuse timer currently running, pushed whenever one
/// STARTS (and once on entering the world). The client counts them down locally — expiry needs no
/// message. A full snapshot each time, not a delta: it is a handful of entries and it self-corrects
/// after any dropped push.</summary>
public record CooldownUpdate(CooldownEntry[] Entries);


/// <summary>One active buff/debuff on the player, for the buff bar + tooltip. Stacks &gt; 1
/// for a stacking effect (shown as "Name xN"). Icon = an emoji/glyph for the square (server-resolved,
/// per-class); "" falls back to the name's initials on the client.</summary>
/// <para>SourceSkillId/SourceName are set ONLY for a child of an improved (GROUP) buff with more
/// than one child, and name the parent: they are what lets the buff bar collapse a whole blessing
/// into one square instead of the four independent buffs it really is (docs/design/BuffLadders.md).
/// Deliberately not set for a potion or a scroll — those are one-child groups, and labelling their
/// square with the bottle's name instead of the effect's would be noise, not grouping.</para>
public record BuffDto(string Name, string Description, float SecondsLeft, bool IsDebuff,
    string Key = "", int Stacks = 1, BuffRow Row = BuffRow.Buff, string Icon = "",
    string SourceSkillId = "", string SourceName = "");

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
    string[]? Drops = null,
    // Extended detail (appended, so older clients ignore it). Gives the inspect window the SAME depth as
    // the character sheet — base attributes, speeds, and the whole combat layer — because "better to
    // have the info and not need it than not have it" (owner). Rank = "Normal"/"Elite"/"Boss", "" for
    // a player.
    int Con = 0, int Atk = 0, int Wit = 0, int Dex = 0, int Spt = 0,
    float MoveSpeed = 0f, float AttackSpeedMult = 1f, float CastSpeedMult = 1f, float AttackRange = 0f,
    float MagicCritChance = 0f, float CritDamage = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float CooldownReduction = 0f,
    float HpRegen = 0f, float MpRegen = 0f,
    int InterruptResist = 0, float CritDmgResist = 0f, float MagicFailResist = 0f,
    string Rank = "");

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


// ----- Auto-hunt / idle farming (docs/design/AutoHunt.md) -------------------------

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
    bool AttackBoss = false,       // engage bosses
    // The auto-potions POTIONS tab: per-potion on/off + HP% threshold. The auto-hunt drinks the
    // highest-threshold ENABLED heal potion that's ready (so common@80 / uncommon@70 / rare@50 act as
    // fallbacks). Empty/null = fall back to the single HpPotionPct + best-potion behaviour.
    AutoPotionDto[]? HealPotions = null,
    // ----- skill CHAINS (playtest-15 design #1) -----
    // How the next skill is chosen inside a priority group. false = "first available": the scan always
    // restarts at the top of the bar (1-2-1-3-1-4…). true = "cyclic": it carries on from the last one
    // used and only wraps once the rest of the group has had its turn (1-2-3-4-1…).
    bool CyclicOrder = false,
    // HP% below which the auto-HEAL chain takes over from buffs/debuffs/attacks. 0 = never auto-heal,
    // 100 = a dedicated healer that heals on cooldown. Distinct from the auto-POTION thresholds.
    int HealThresholdPct = 70,
    // Only ever attack what the party leader is attacking; with no leader target, wait rather than
    // pick your own. (Ignored when you are not in a party, or you ARE the leader.)
    bool AssistPartyLeader = false);

/// <summary>One line in the auto-potions Potions tab: which potion item, whether it's armed, and the
/// HP (or MP) percent below which to drink it.</summary>
public record AutoPotionDto(string ItemId, bool Enabled, int ThresholdPct);

/// <summary>Server -> owning client: you just crossed into a named region. Shown as transient
/// centre-screen text that fades. MinLevel/MaxLevel are the derived band (0/0 = a peaceful area or a
/// town — no band shown). Replaces the always-on zone label (owner: the HUD carries no permanent
/// place text).</summary>
public record RegionNotice(string Name, int MinLevel, int MaxLevel);

/// <summary>One row of a leaderboard: rank position, character, the ranked metric value, and the reward
/// title the #1 in that category wears (empty for everyone else). Value's meaning depends on the
/// category (gold, kills, seconds online, or level).</summary>
public record LeaderboardEntry(int Rank, string Name, int Level, long Value, string Title);

/// <summary>Server -> client (request/response): a ranked board for one <see cref="Leaderboards"/>
/// category — the top N characters by that metric.</summary>
public record LeaderboardDto(string Category, IReadOnlyList<LeaderboardEntry> Entries);

/// <summary>The leaderboard categories + their labels and the honorary title the #1 in each earns.
/// Category ids are append-only strings, like skill ids.</summary>
public static class Leaderboards
{
    public static readonly string[] Categories = { "level", "gold", "pvp", "pk", "online", "charisma" };

    public static string Label(string cat) => cat switch
    {
        "level"  => "Level",
        "gold"   => "Wealth",
        "pvp"    => "PvP Kills",
        "pk"     => "Player Kills",
        "online" => "Time Played",
        "charisma" => "Charisma",
        _        => cat,
    };

    public static bool IsCategory(string? cat) =>
        cat is not null && Array.IndexOf(Categories, cat) >= 0;

    /// <summary>The honorary title the rank-1 character in this category earns.</summary>
    public static string TopTitle(string cat) => cat switch
    {
        "level"  => "the Ascended",
        "gold"   => "the Wealthy",
        "pvp"    => "the Warlord",
        "pk"     => "the Feared",
        "online" => "the Devoted",
        "charisma" => "the Beloved",
        _        => "",
    };
}

/// <summary>
/// Server -> owning client: the titles this character may WEAR, and which one is worn.
///
/// A title is HELD, not owned: you hold it for as long as you are rank 1 of that board, and the server
/// re-reads the boards every few minutes. That is deliberately different from an achievement — "the
/// Wealthy" that stays on a player who has since been out-earned says the opposite of what the board
/// says, and the whole point of the thing is to advertise the board.
///
/// <paramref name="Held"/> and <paramref name="Worn"/> are CATEGORY ids (append-only, like skill ids),
/// not display text: the text comes from <see cref="Leaderboards.TopTitle"/>, so re-wording a title
/// re-words everyone's. Worn = "" means none.
/// </summary>
public record TitlesDto(string[] Held, string Worn);

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
/// <summary>Server -> client auto-hunt state. FarmCenterX/Y is where the STATIC farm circle is
/// anchored — the server owns that anchor, and without it on the wire the client drew the range ring
/// around the CHARACTER, so "keep position" showed a circle that walked off with you instead of
/// marking the spot you were held to (playtest-13). Server-to-client only, so it never round-trips
/// back as part of the config the client saves.</summary>
/// <para><paramref name="IdleSecondsLeft"/> / <paramref name="OfflineSecondsLeft"/> are the two
/// runtime budgets left on the clock (online idle 8h, offline 2h by default), so the client can
/// count the Auto button down instead of the session simply stopping one day with no warning.
/// <c>-1</c> = uncapped (the owner sets a cap of 0 to leave a character farming overnight).
/// New fields with defaults: an older client just ignores them — see GameConstants.ProtocolVersion,
/// where DTO fields are explicitly NOT a protocol break, unlike a hub signature.</para>
public record AutoHuntStatus(bool Enabled, float MpPerSec, AutoSkillReuse[] Skills,
    float FarmCenterX = 0f, float FarmCenterY = 0f,
    int IdleSecondsLeft = -1, int OfflineSecondsLeft = -1);

/// <summary>Server -> client: what the AUTOPILOT is currently on. null = it has nothing.
///
/// The autopilot has always picked a target server-side (CombatTargetId) and never told the client,
/// so while auto-hunting the target window sat empty or stale and you could not see what it had
/// chosen — which also made the targeting RULE impossible to judge (playtest-15). Sent only when the
/// choice CHANGES, like the Cooldowns push: a few messages per fight, not one per tick.</summary>
public record AutoTargetUpdate(Guid? TargetId);

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
    int TestHealPower = 1000, int TestSkillPower = 0, float TestSkillMod = 1f,
    // Regen: the CADENCE (seconds between natural-regen ticks; 3 = L2's period) and how steeply the
    // stat weights it (per-point multiplier — 1.03 is L2's CON curve, 1.0 = stat does nothing).
    // Changing the cadence does NOT change healing speed, only its chunkiness.
    float RegenIntervalSeconds = 3f, float ConRegenBase = 1.03f,
    // Mob regen is a FRACTION OF THE MOB'S OWN POOL per second, not the CON curve (see
    // StatCalculator.MobHpRegenPerSecond). No level term, so neither number ever needs revisiting
    // when the level range grows. IN COMBAT reads as a maximum kill time (0.001 = you must finish
    // inside ~16 minutes); IDLE reads as time-to-full (0.05 = 20 seconds).
    float MobHpRegenPctCombat = 0.001f, float MobRegenPctIdle = 0.05f);

/// <summary>One member row in the party window. Debuffs = the names of the debuffs currently on this
/// member, so a healer sees at a glance who to cleanse without selecting each one.</summary>
public record PartyMemberDto(Guid Id, string Name, int Level, string ClassName,
    int Hp, int MaxHp, int Mp, int MaxMp, bool IsLeader,
    PartyMemberStatus Status = PartyMemberStatus.Online,
    string[]? Debuffs = null,
    // Positive buff NAMES (appended) — so the party window can show who has what up, behind a
    // buffs/debuffs view toggle. Internal counters (DoT stacks) are excluded like Debuffs.
    string[]? Buffs = null);

/// <summary>Server -> party members: the current roster (empty array = you left/were the last
/// member, so the client hides the party window). Sent on membership change and refreshed
/// periodically for live HP/MP bars.</summary>
public record PartyUpdate(PartyMemberDto[] Members, LootMode LootMode = LootMode.Random);


// ----- Accounts & character selection (Phase 5) ----------------------------

/// <summary>
/// Client -> Server: register or login.
///
/// <paramref name="Protocol"/> is the wire contract the client speaks
/// (<see cref="GameConstants.ProtocolVersion"/>), and it lives HERE rather than as an extra hub
/// parameter for one hard-won reason: **SignalR does NOT bind by arity.** A hub method's default
/// parameter value does not make an omitted argument legal — the dispatcher requires the argument
/// count to match, and an older client calling the shorter overload gets "Failed to invoke 'Login'
/// due to an error on the server" on every attempt. (That is exactly what happened: a client one
/// build old could reconnect its socket but never re-authenticate, so it sat connected and frozen.)
///
/// A DTO field has none of that problem. An old client simply omits it from the JSON, the
/// deserializer leaves it 0, and 0 is the documented "too old to say" value that falls back to the
/// legacy build-label list. Extending a DTO is the backwards-compatible move; extending a hub
/// signature is not.
/// </summary>
public record AuthRequest(string Username, string Password, int Protocol = 0);

/// <summary>Server -> Client: auth outcome. Token is the account id used for
/// subsequent character calls within this connection.</summary>
/// <summary>Server -> admin client: another player's bag (for /bag), or the admin's own bag when it is
/// the /give picker. <paramref name="OwnerName"/> is always the character the action TARGETS.</summary>
public record AdminBagDto(string OwnerName, long Gold, InventoryItemDto[] Items);

/// <summary>Server -> client: admin-only state worth showing PERMANENTLY on screen. God mode and forced
/// speeds are invisible otherwise — the only way to recall whether god mode was on was to type /god
/// again and see which way it toggled.</summary>
public record AdminStateDto(
    AccountRole Role, bool GodMode, float? CastSpeed, float? AttackSpeed, float? MoveSpeed);

/// <summary>Account login/register result. Carries no staff role: authorization now belongs to the
/// CHARACTER (see <see cref="LoginResult.Role"/>), so logging in proves identity only.</summary>
public record AuthResponse(bool Success, string? Error, AccountRole Role = AccountRole.Player);

/// <summary>One character on the account, for the selection screen. PendingDeleteAt
/// (UTC) is set when the character is scheduled for deletion; null = active.
///
/// <para><paramref name="OfflineSecondsLeft"/>: null = not offline-farming (the normal case), -1 =
/// farming with no time limit, >= 0 = seconds of offline budget left. The character screen is the
/// ONLY place this can be seen — an offline farmer has no connection and no UI to push it to.</para></summary>
public record CharacterSlot(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass,
    int Level, DateTime? PendingDeleteAt = null, int ThirdClass = 0, int? OfflineSecondsLeft = null);

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
    string Location = "", bool Tracked = false);

/// <summary>A class-change option shown by a class-change NPC. Description is a
/// "what this class does" blurb so the player can choose before committing.</summary>
public record ClassChangeOption(int SecondClassId, string ClassName, bool Meets,
    string[] RequiredItemNames, bool[] HasItem, string Description = "");

/// <summary>One buyable line in a vendor shop.</summary>
public record ShopItemDto(string DefId, string Name, int BuyPrice);

/// <summary>A vendor's wares, attached to the dialog when talking to a vendor.</summary>
public record ShopInfo(string Title, ShopItemDto[] Items);

/// <summary>One entry in the buy-back list: an item you recently SOLD, re-buyable for what you got for it.
/// Index is the entry's position in the list (the client passes it back to re-buy).</summary>
public record BuyBackEntryDto(int Index, string DefId, string Name, int Quantity, int Enchant, long UnitPrice);

/// <summary>The character's current buy-back list (recently-sold items). Sent when a shop opens and after
/// every sell / buy-back. In-memory only — it does not survive logout.</summary>
public record BuyBackUpdate(BuyBackEntryDto[] Items);

/// <summary>Server -> one player: the recently BINNED items, restorable for free (C18). Same row shape
/// as the buy-back list — <c>UnitPrice</c> is always 0 — but its own message, because it is its own
/// list with its own cap and it is reachable in the FIELD rather than at a vendor.</summary>
public record RestoreUpdate(BuyBackEntryDto[] Items);

/// <summary>One teleport destination offered by a gatekeeper.
///
/// <paramref name="DestId"/> is EITHER a city's safe-zone id OR a named field gate's id
/// (<see cref="TeleportPoint"/>) — a gatekeeper now sends you to a specific camp doorstep, not just to
/// another town (owner: *"a city gatekeeper should list all the owned fields and their teleporting
/// points, removing the random teleporting factor"*). It was called ZoneId while towns were the only
/// possible destination.
///
/// MinLevel/MaxLevel are the level band you are travelling TO (0/0 = unknown), and
/// <paramref name="Group"/> is the field a gate belongs to (empty for a city), so the client can list
/// gates under their field instead of as a flat wall of names.</summary>
public record TeleportDest(string DestId, string Name, int Fee, int MinLevel = 0, int MaxLevel = 0,
                           string Description = "", string Group = "");

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
    BufferInfo? Buffer = null, // buffer options (null for non-buffers)
    bool Warehouse = false); // true for a Warehouse Keeper — the client shows an "Open Warehouse" button

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

// ----- The quest WINDOW's view (0.43.0) ------------------------------------
//
// QuestSummary above is the DIALOG's view: one line about the step you are on, already formatted by
// the server. The window needs the other thing — every quest this character can ever see, whether it
// is takeable, and what each of its steps was — so the three tabs (active / available / completed) and
// the per-quest detail window can be drawn without the client knowing any quest rules.

/// <summary>Where a quest stands for THIS character, which is also the tab it lands in.
/// <see cref="Available"/> and <see cref="Locked"/> share one tab: a list of what you cannot do yet,
/// with no way to see what you CAN take, is only half an answer.</summary>
public enum QuestAvailability { Available = 0, Active = 1, Completed = 2, Locked = 3 }

/// <summary>One objective line of a quest, structured. Until 0.43.0 a step reached the client only as
/// a pre-formatted sentence — enough for one line in the log, useless for a detail window that shows
/// every step with its own progress and tick.</summary>
public record QuestStepDto(string Text, string Location, int Counter, int Needed,
                           bool Done, bool Current);

/// <summary>One gathering line of a contract: what drops, off what, how many you carry, and what a
/// token is worth (a fraction of that creature's own kill exp+gold — see <c>QuestGather</c>).
/// 0.42.9 folded this into the step TEXT to avoid a protocol bump; this is it structured, as promised.</summary>
public record QuestGatherDto(string ItemName, string MobName, int Held,
                             float DropChance, float RewardModifier);

/// <summary>One quest as the quest WINDOW sees it. <paramref name="Status"/> is the one line that
/// explains the state — "Requires level 20", "Ready to hand in", "Repeatable", "Again tomorrow" —
/// so a locked row never just sits there greyed out without saying why.</summary>
public record QuestEntry(
    string Id, string Name, string Description,
    QuestAvailability State, string Status,
    string GiverName, string GiverLocation,
    int MinLevel, int MaxLevel,
    bool Repeatable, bool Daily, bool CanComplete,
    int StepIndex,
    QuestStepDto[] Steps,
    QuestGatherDto[] Gathers,
    string RewardText,
    bool Tracked = false);

/// <summary>Server -> client: the full quest log. <paramref name="Active"/> and
/// <paramref name="Completed"/> stay as they were (the on-screen tracker reads them);
/// <paramref name="Entries"/> is every quest this character can see, in every state.</summary>
public record QuestLog(QuestSummary[] Active, string[] Completed, QuestEntry[] Entries);
