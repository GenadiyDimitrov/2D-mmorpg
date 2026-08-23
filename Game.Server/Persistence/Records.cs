using Game.Shared;

namespace Game.Server.Persistence;

/// <summary>
/// Database row models. These are SEPARATE from the live-game `Entity` class on
/// purpose: the DB is persistence only, never the live game state. We map
/// in-memory entities to these records on save and back on load.
/// </summary>

public class AccountRecord
{
    public int Id { get; set; }
    public required string Username { get; set; }

    /// <summary>PBKDF2 hash of the password (never store plaintext).</summary>
    public required string PasswordHash { get; set; }
    public required string PasswordSalt { get; set; }

    /// <summary>Legacy permanent-ban flag (kept for compat). The timed ban is BannedUntilUtc.</summary>
    public bool IsBanned { get; set; }

    /// <summary>Account is banned until this UTC time — no login until it passes (owner: ban is
    /// per-account + timed). null = not time-banned. Checked at Login.</summary>
    public DateTime? BannedUntilUtc { get; set; }

    // ----- Auto/offline farm budget (per ACCOUNT, daily) ----------------------
    // These used to be per-SESSION counters on the character, which meant a re-log handed the whole
    // cap back and N characters of one account farmed N× the wall clock. The allowance now belongs to
    // the ACCOUNT and is a BALANCE that is spent, not an elapsed counter that is compared to a cap.
    // Stored in TICKS so the drain is lossless (the loop spends one tick at a time).

    /// <summary>Ticks of ONLINE auto-hunt left today, across every character of this account.</summary>
    public long AutoTicksLeft { get; set; }

    /// <summary>Ticks of OFFLINE farming left today, across every character of this account.</summary>
    public long OfflineTicksLeft { get; set; }

    /// <summary>Server-local DATE the two balances were last refilled. A DATE, not a timestamp: the
    /// refill is a fixed server midnight, so it accrues correctly across a restart for free and never
    /// drifts the way a rolling 24h window from the last spend would (owner's call, 2026-08-05).
    /// <see cref="DateOnly.MinValue"/> on a new row → the first read refills it.</summary>
    public DateOnly LastFarmResetDate { get; set; }

    /// <summary>Per-account cap OVERRIDE in seconds — this is the premium knob.
    /// <c>-1</c> = use the server default · <c>0</c> = UNLIMITED (admin testing) · &gt;0 = explicit.
    /// Free is 8h online / 2h offline; premium is 12h / 4h.</summary>
    public int AutoCapSeconds { get; set; } = -1;
    public int OfflineCapSeconds { get; set; } = -1;

    public List<CharacterRecord> Characters { get; set; } = new();
}

public class CharacterRecord
{
    public int Id { get; set; }
    public int AccountId { get; set; }

    /// <summary>Staff role — PER CHARACTER, not per account (owner). One account may hold an admin
    /// character alongside perfectly ordinary ones, so an admin can play the game as a normal player
    /// without their commands (or their name colour) following them around.
    /// Admin = everything; Moderator = jail/kick/chatban only; Player = none.
    /// BAN is the one punishment that stays per-ACCOUNT — see AccountRecord.BannedUntilUtc.</summary>
    public AccountRole Role { get; set; } = AccountRole.Player;

    public required string Name { get; set; }
    public Race Race { get; set; }
    public BaseClass BaseClass { get; set; }
    public int SecondClass { get; set; }
    public int ThirdClass { get; set; }
    /// <summary>⚠ NEW COLUMN 2026-08-17 (4th classes) — needs the `game.db` delete-and-recreate
    /// described on <see cref="CraftExp"/> below.</summary>
    public int FourthClass { get; set; }
    public int Profession { get; set; }   // crafting profession (0 = none)

    /// <summary>RAW crafting exp (`BL-05`), 12 points per same-level craft. The crafting LEVEL is
    /// derived from this and the character's own band, never stored — one number cannot disagree with
    /// itself. Zeroed when the profession is quit.
    /// ⚠ NEW COLUMN 2026-08-13: `EnsureCreated()` does not ALTER an existing table, so this needs the
    /// usual `Game.Server/game.db` (+ `-shm`/`-wal`) delete-and-recreate.</summary>
    public int CraftExp { get; set; }

    public int Level { get; set; } = 1;
    public long Exp { get; set; }

    /// <summary>When set, the character is scheduled for permanent deletion at this
    /// UTC time (a cancellable "pending delete"). null = active. Purged on listing.</summary>
    public DateTime? PendingDeleteAt { get; set; }

    /// <summary>Character is JAILED until this UTC time (owner: jail is per-character + timed). While set,
    /// it spawns in jail on every login and can't chat/whisper/escape. null = free.</summary>
    public DateTime? JailedUntilUtc { get; set; }

    /// <summary>Character is KICKED until this UTC time — it can't ENTER the world until it passes, though
    /// the account can still log in and play OTHER characters (owner: kick is per-character + timed).</summary>
    public DateTime? KickedUntilUtc { get; set; }

    /// <summary>Character is CHAT-BANNED until this UTC time — it plays normally but can't type in any
    /// channel (owner: the light-touch punishment between a warning and a jailing). null = free.</summary>
    public DateTime? ChatBannedUntilUtc { get; set; }

    public long Gold { get; set; }

    public int SkillPoints { get; set; }

    /// <summary>Learned skill ids, comma-separated (simple + migration-free).</summary>
    public string LearnedSkillsCsv { get; set; } = "";

    /// <summary>Completed quest ids, comma-separated.</summary>
    public string CompletedQuestsCsv { get; set; } = "";

    /// <summary>Recipe ids learned from drops (DropOnly recipes), comma-separated.</summary>
    public string KnownRecipesCsv { get; set; } = "";

    /// <summary>Friend character names, comma-separated. Per character.</summary>
    public string FriendsCsv { get; set; } = "";

    /// <summary>Blocked (ignored) character names, comma-separated. Per character. (Schema change — delete
    /// game.db to recreate.)</summary>
    public string BlockedCsv { get; set; } = "";

    /// <summary>The character's SOCIAL options as a <see cref="SocialOptions"/> flag set — the blanket
    /// chat blocks and the trade/party auto-declines (playtest-19 M2). One int so a new toggle costs no
    /// further schema change. (Schema change — delete game.db to recreate.)</summary>
    public int SocialOptions { get; set; }

    // ----- Charisma (reputation). Schema change — delete game.db to recreate. -----
    public int Charisma { get; set; }
    public long CharismaLifetime { get; set; }
    public int LikesRemainingToday { get; set; } = GameConstants.DailyLikeBudget;
    public string LikeBudgetDay { get; set; } = "";

    /// <summary>WHERE the worn title comes from: a leaderboard category, a staff title id,
    /// <see cref="TitleCatalog.Custom"/>, or "" for none. The SOURCE, not the words — a granted title is
    /// only drawn while the character still holds it. (Schema change — delete game.db to recreate.)</summary>
    public string TitleCategory { get; set; } = "";

    /// <summary>The title this character WROTE for itself, kept even while a granted one is worn so it
    /// can be switched back to without retyping. "" = never wrote one.
    /// (Schema change 0.55.0 — delete game.db to recreate.)</summary>
    public string CustomTitle { get; set; } = "";

    /// <summary>Colour of <see cref="CustomTitle"/>, RRGGBB with no '#'. "" = the default.
    /// (Schema change 0.55.0 — delete game.db to recreate.)</summary>
    public string CustomTitleColor { get; set; } = "";

    /// <summary>Has this character been granted the right to write its own title? Off by default.
    /// (Schema change 0.55.0 — delete game.db to recreate.)</summary>
    public bool MayWriteTitle { get; set; }

    /// <summary>Active quests as JSON list of CharacterQuestState.</summary>
    public string ActiveQuestsJson { get; set; } = "";

    /// <summary>Auto-hunt config as JSON (AutoHuntConfigDto). Empty = defaults/off.</summary>
    public string AutoHuntJson { get; set; } = "";

    /// <summary>Active BUFFS as JSON (PersistenceService.BuffSnapshot list). Buffs used to die on every
    /// logout simply because nothing stored them; the owner's rule is that a buff ends only when it
    /// expires, is dispelled/cancelled, or the subclass changes. Expiry is stored as WALL-CLOCK UTC, so
    /// time spent offline still burns the duration.</summary>
    public string BuffsJson { get; set; } = "";

    /// <summary>Equipment presets A/B/C as JSON: a Guid[][] of worn item instance ids. Empty = none.</summary>
    public string EquipPresetsJson { get; set; } = "";

    /// <summary>Which owned class the character is currently playing (a <see cref="SubclassRecord.Slot"/>).
    /// Slot 0 is the class they were created as.</summary>
    public int ActiveSubclassSlot { get; set; }

    /// <summary>Every class this character owns. THE SOURCE OF TRUTH for anything class-level: level,
    /// XP, skill points, 2nd/3rd class, core stats, learned skills, skill bar.
    ///
    /// ⚠ The matching columns ON THIS ROW (BaseClass / SecondClass / ThirdClass / Level / Exp /
    /// SkillPoints / Con / Atk / Wit / Agi / LearnedSkillsCsv) are a **mirror of the ACTIVE subclass**,
    /// rewritten from it on every save. They exist so the character-SELECT screen can list a character
    /// without loading its subclasses. Never read them for gameplay — read the subclass.</summary>
    public List<SubclassRecord> Subclasses { get; set; } = new();

    /// <summary>PvP reputation: PK karma (>0 = red), and lifetime PK / PvP kill counts.</summary>
    public int Karma { get; set; }
    public int PkCount { get; set; }
    public int PvpCount { get; set; }
    public int ConsecutivePk { get; set; }

    /// <summary>Lifetime seconds this character has been online — powers the online-time leaderboard.</summary>
    public long TotalOnlineSeconds { get; set; }

    /// <summary>True if this character DIED while offline-farming / link-dead (away from keyboard). It
    /// logs back in DEAD (res prompt), not healed — closes the "go offline to dodge a death, come back
    /// full HP" exploit. Cleared when the character actually respawns.</summary>
    public bool DiedWhileAway { get; set; }

    // Core stats are derived from race/class/level, but second-class and item
    // bonuses are permanent additions, so we persist the raw core stats.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Agi { get; set; }
    public int Spt { get; set; }

    // Last known position so you log back in where you left off.
    public float X { get; set; }
    public float Y { get; set; }

    public List<ItemRecord> Items { get; set; } = new();
}

/// <summary>
/// ONE class a character owns (IG-style subclass). A character has several; it plays one at a time
/// (<see cref="CharacterRecord.ActiveSubclassSlot"/>).
///
/// Everything CLASS-level lives here. Everything CHARACTER-level (race, inventory, gold, karma,
/// quests, profession, auto-hunt, position) stays on <see cref="CharacterRecord"/>. See Subclass.cs
/// for why the split is drawn exactly there.
/// </summary>
public class SubclassRecord
{
    public int Id { get; set; }
    public int CharacterId { get; set; }

    /// <summary>Stable id within the character. 0 = the class they were created as (never removable).</summary>
    public int Slot { get; set; }

    /// <summary>Per-class race (a subclass can be a different race — cross-race subclasses).</summary>
    public Race Race { get; set; }
    public BaseClass BaseClass { get; set; }
    public int SecondClass { get; set; }
    public int ThirdClass { get; set; }
    /// <summary>0 = none; a FourthClassCatalog id (201-236). ⚠ NEW COLUMN 2026-08-17 — see the
    /// EnsureCreated note on CharacterRecord.CraftExp: delete `game.db` (+ `-shm`/`-wal`).</summary>
    public int FourthClass { get; set; }

    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public int SkillPoints { get; set; }

    // Core stats: from (Race, BaseClass), then moved only by the level-40 stat swaps. Per class,
    // because swapping a fighter for a mage must swap CON/ATK/WIT/AGI with it.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Agi { get; set; }
    public int Spt { get; set; }

    /// <summary>Learned skills as "id:level" pairs, comma-separated.</summary>
    public string LearnedSkillsCsv { get; set; } = "";

    /// <summary>This class's skill-bar layout as a JSON string array ("" = an empty slot).</summary>
    public string SkillBarJson { get; set; } = "";
}

public class ItemRecord
{
    public int Id { get; set; }
    public int CharacterId { get; set; }

    /// <summary>The live-game InstanceId, preserved across saves.</summary>
    public Guid InstanceId { get; set; }

    public required string DefId { get; set; }
    public bool Equipped { get; set; }
    public int Enchant { get; set; }
    public int Quantity { get; set; } = 1;

    /// <summary>Rolled attributes — stored as a JSON column (EF Core ToJson).
    /// Rolled once at drop time and immutable thereafter (except by an explicit
    /// reroll), so persisting them verbatim is exactly right.</summary>
    public List<ItemAttribute> Attributes { get; set; } = new();

    /// <summary>Wall-clock expiry for a timed item (a war/spell rune); null = never expires. Persisted so a rune
    /// keeps counting down across relogs/restarts and is purged on load if the time has passed.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>True = this item lives in the character's private WAREHOUSE, not the bag. Routes the item
    /// to the right list on load. (Schema change — delete game.db to recreate.)</summary>
    public bool InWarehouse { get; set; }

    // ----- Per-instance overrides (`58d`) — MUST persist ------------------------------------------
    // Without these a given item silently reverts to an ordinary catalog copy on the next login: the
    // bound Soulcrystal becomes tradable, the renamed sword loses its name, and the Rune of Sinners —
    // whose entire point is that you cannot get rid of it — could be sold after a relog.
    // (Schema change — delete game.db to recreate.)

    /// <summary>Instance sell price; -1 = unsellable, null = use the def.</summary>
    public long? SellPriceOverride { get; set; }

    /// <summary>Instance tradability, null = use the def. False is what BINDS an item to its owner.</summary>
    public bool? TradableOverride { get; set; }

    /// <summary>A name written for this copy only, null = the def's name.</summary>
    public string? CustomName { get; set; }

    /// <summary>May this instance enter the private warehouse? null = yes.</summary>
    public bool? CanStorePrivate { get; set; }

    /// <summary>May this instance enter the ACCOUNT warehouse? null = the tradable rule.</summary>
    public bool? CanStoreAccount { get; set; }

    /// <summary>Picks still owed by a part-spent SELECTION box (`BL-20`); null = the box def's full
    /// count. MUST persist: the whole point of a partial pick is that you walk away with the box and
    /// come back to it, and without this column the remaining 5 picks would silently become 10 again
    /// on the next login — the box would print scrolls. (Schema change — delete game.db to recreate.)</summary>
    public int? PicksRemaining { get; set; }
}


/// <summary>An item in the ACCOUNT warehouse — a separate table from <see cref="ItemRecord"/> on
/// purpose: this one belongs to the account, not to any character, which is the whole point of it.
/// Hanging it off a character row would tie shared goods to whichever character happened to deposit
/// them, and deleting that character would take the shared bank with it.
/// (Schema addition — delete game.db to recreate.)</summary>
public class AccountItemRecord
{
    public int Id { get; set; }
    public int AccountId { get; set; }

    /// <summary>The live-game InstanceId, preserved across saves.</summary>
    public Guid InstanceId { get; set; }

    public required string DefId { get; set; }
    public int Enchant { get; set; }
    public int Quantity { get; set; } = 1;

    /// <summary>Rolled attributes — a JSON column, same as on the character's items.</summary>
    public List<ItemAttribute> Attributes { get; set; } = new();

    /// <summary>Wall-clock expiry for a timed item (a rune); null = never expires. The account bank
    /// is space, not a time-pause — same rule as the private one.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
}


/// <summary>Persisted respawn time for a boss/elite zone, so a long timer
/// survives a server restart. Keyed by the zone's stable Id.</summary>
public class BossTimerRecord
{
    public int Id { get; set; }
    public required string ZoneId { get; set; }

    /// <summary>UTC time at which the boss should next be alive.</summary>
    public DateTime RespawnAtUtc { get; set; }
}

/// <summary>
/// ONE LINE OF PLAYER CHAT, kept for moderation.
///
/// 🔑 Owner, playtest 28: *"don't we need a chat log — I mean in db as who said what and when and to
/// who … because now an admin/mod should ban based on som1 is trying to sell u for $ on private chat —
/// how the big games work it out? with tickets with a screenshot, or they have their chat log?"*
///
/// The answer is: they have the log. A screenshot is evidence a REPORTER supplies and an accused
/// player can dispute; a server-side log is what the moderator actually reads, and it is the only way
/// to answer "what else has this account been saying" rather than judging one cropped image. Tickets
/// are how the case OPENS — the log is how it is decided. So the log exists now, and the ban tools
/// (`/chatban`, `/jail`, the account ban) already exist to act on it.
///
/// What is stored is exactly his four columns, plus the channel:
///   • <see cref="AtUtc"/>       — when.
///   • <see cref="SenderCharacterId"/> / <see cref="SenderName"/> — who. The ID as well as the name,
///     because a name can be freed by a delete and re-taken by somebody else.
///   • <see cref="Channel"/> + <see cref="ReceiverName"/> — to whom. A whisper names one character;
///     Local and World name nobody, and the channel IS the audience. Keeping them in two columns
///     rather than one overloaded field is what lets "every whisper this account sent" be a query.
///   • <see cref="Text"/>        — what.
///
/// ⚠ It logs what the server ACCEPTED. A message refused for a chat ban, a jail, the world-chat level
/// floor or an empty/over-long body never reaches here — those were not said to anyone. A WHISPER to
/// someone who has blocked you is refused outright and is not logged either; a block on Local or World
/// only filters who RECEIVES the line, so it is logged — it was said, some people just did not hear it.
/// </summary>
public class ChatLogRecord
{
    public int Id { get; set; }
    public DateTime AtUtc { get; set; }

    public int SenderCharacterId { get; set; }
    public required string SenderName { get; set; }

    /// <summary>The <c>ChatChannel</c> value (Local / World / Whisper) as an int.</summary>
    public int Channel { get; set; }

    /// <summary>The whispered-to character's name, or empty for a channel with no single recipient.</summary>
    public string ReceiverName { get; set; } = "";

    public required string Text { get; set; }
}
