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
    public int Profession { get; set; }   // crafting profession (0 = none)

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

    // ----- Charisma (reputation). Schema change — delete game.db to recreate. -----
    public int Charisma { get; set; }
    public long CharismaLifetime { get; set; }
    public int LikesRemainingToday { get; set; } = GameConstants.DailyLikeBudget;
    public string LikeBudgetDay { get; set; } = "";

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
    /// SkillPoints / Con / Atk / Wit / Dex / LearnedSkillsCsv) are a **mirror of the ACTIVE subclass**,
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
    public int Dex { get; set; }
    public int Spt { get; set; }

    // Last known position so you log back in where you left off.
    public float X { get; set; }
    public float Y { get; set; }

    public List<ItemRecord> Items { get; set; } = new();
}

/// <summary>
/// ONE class a character owns (L2-style subclass). A character has several; it plays one at a time
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

    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public int SkillPoints { get; set; }

    // Core stats: from (Race, BaseClass), then moved only by the level-40 stat swaps. Per class,
    // because swapping a fighter for a mage must swap CON/ATK/WIT/DEX with it.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }
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
