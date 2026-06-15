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

    /// <summary>Admins get elevated commands (ban/kick/jail, god mode).</summary>
    public bool IsAdmin { get; set; }

    public bool IsBanned { get; set; }

    public List<CharacterRecord> Characters { get; set; } = new();
}

public class CharacterRecord
{
    public int Id { get; set; }
    public int AccountId { get; set; }

    public required string Name { get; set; }
    public Race Race { get; set; }
    public BaseClass BaseClass { get; set; }
    public int SecondClass { get; set; }

    public int Level { get; set; } = 1;
    public long Exp { get; set; }

    // Core stats are derived from race/class/level, but second-class and item
    // bonuses are permanent additions, so we persist the raw core stats.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }

    // Last known position so you log back in where you left off.
    public float X { get; set; }
    public float Y { get; set; }

    public List<ItemRecord> Items { get; set; } = new();
}

public class ItemRecord
{
    public int Id { get; set; }
    public int CharacterId { get; set; }

    /// <summary>The live-game InstanceId, preserved across saves.</summary>
    public Guid InstanceId { get; set; }

    public int DefId { get; set; }
    public bool Equipped { get; set; }
    public int Enchant { get; set; }
    public int Quantity { get; set; } = 1;

    /// <summary>Rolled attributes — stored as a JSON column (EF Core ToJson).
    /// Rolled once at drop time and immutable thereafter (except by an explicit
    /// reroll), so persisting them verbatim is exactly right.</summary>
    public List<ItemAttribute> Attributes { get; set; } = new();
}
