using Game.Server.Simulation;
using Game.Shared;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Persistence;

/// <summary>
/// The bridge between the live game (in-memory <see cref="Entity"/> objects)
/// and the database. All methods open a short-lived DbContext, do their work,
/// and return — the game loop never holds a DB connection. Called from the
/// hub's connection layer (login/logout), not from the tick loop, so the async
/// DB I/O never blocks simulation.
/// </summary>
public class PersistenceService
{
    private readonly IDbContextFactory<GameDbContext> _factory;

    public PersistenceService(IDbContextFactory<GameDbContext> factory) => _factory = factory;

    public async Task EnsureCreatedAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

    // ----- Accounts ----------------------------------------------------------

    public record AuthResult(bool Success, string? Error, int AccountId, bool IsAdmin);

    public async Task<AuthResult> RegisterAsync(string username, string password)
    {
        username = username.Trim();
        if (username.Length is < 3 or > 20)
            return new AuthResult(false, "Username must be 3-20 characters.", 0, false);
        if (password.Length < 4)
            return new AuthResult(false, "Password must be at least 4 characters.", 0, false);

        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Accounts.AnyAsync(a => a.Username == username))
            return new AuthResult(false, "Username already taken.", 0, false);

        var (hash, salt) = PasswordHasher.Hash(password);

        // First account ever created becomes admin (convenient for testing).
        bool isFirst = !await db.Accounts.AnyAsync();

        var account = new AccountRecord
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsAdmin = isFirst
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return new AuthResult(true, null, account.Id, account.IsAdmin);
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        username = username.Trim();
        await using var db = await _factory.CreateDbContextAsync();

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Username == username);
        if (account is null || !PasswordHasher.Verify(password, account.PasswordHash, account.PasswordSalt))
            return new AuthResult(false, "Invalid username or password.", 0, false);

        if (account.IsBanned)
            return new AuthResult(false, "This account is banned.", 0, false);

        return new AuthResult(true, null, account.Id, account.IsAdmin);
    }

    // ----- Characters --------------------------------------------------------

    /// <summary>Character names on an account, for the selection screen.</summary>
    public async Task<List<CharacterSummary>> ListCharactersAsync(int accountId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Characters
            .Where(c => c.AccountId == accountId)
            .Select(c => new CharacterSummary(c.Id, c.Name, c.Race, c.BaseClass, c.SecondClass, c.Level))
            .ToListAsync();
    }

    public record CharacterSummary(int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass, int Level);

    public async Task<(bool Success, string? Error)> CreateCharacterAsync(
        int accountId, string name, Race race, BaseClass baseClass)
    {
        name = name.Trim();
        if (name.Length is 0 or > GameConstants.MaxCharacterNameLength)
            return (false, $"Name must be 1-{GameConstants.MaxCharacterNameLength} characters.");

        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Characters.AnyAsync(c => c.Name == name))
            return (false, "That character name is taken.");

        var stats = StatCalculator.GetBaseStats(race, baseClass);
        var record = new CharacterRecord
        {
            AccountId = accountId,
            Name = name,
            Race = race,
            BaseClass = baseClass,
            Con = stats.Con,
            Atk = stats.Atk,
            Wit = stats.Wit,
            Dex = stats.Dex,
            X = GameConstants.ZoneWidth / 2,
            Y = GameConstants.ZoneHeight / 2,
        };

        // Starter gear so a brand-new character isn't empty.
        // Starter gear keyed by stable string ids. Give a weapon matching the
        // base class's playstyle and the appropriate armor.
        string starterWeapon = baseClass == BaseClass.Mage
            ? ItemCatalog.WeaponKey(WeaponType.Staff, ItemGrade.F, ItemRarity.Common)
            : ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.F, ItemRarity.Common);
        string starterArmor = baseClass == BaseClass.Mage
            ? ItemCatalog.ArmorKey(ArmorWeight.Robe, ItemGrade.F, ItemRarity.Common)
            : ItemCatalog.ArmorKey(ArmorWeight.Light, ItemGrade.F, ItemRarity.Common);

        record.Items.Add(NewItem(starterWeapon));
        record.Items.Add(NewItem(starterArmor));
        record.Items.Add(NewItem(ItemCatalog.MinorPotion, 5));
        record.Items.Add(NewItem(ItemCatalog.GreaterPotion, 2));

        db.Characters.Add(record);
        await db.SaveChangesAsync();
        return (true, null);
    }

    private static ItemRecord NewItem(string defId, int qty = 1)
    {
        var rec = new ItemRecord { InstanceId = Guid.NewGuid(), DefId = defId, Quantity = qty };
        if (ItemCatalog.Get(defId) is ItemDef def && def.Slot is EquipSlot.Weapon or EquipSlot.Armor)
            rec.Attributes = def.FixedAttributes is { Length: > 0 } fixedAttrs
                ? fixedAttrs.ToList()
                : AttributeSystem.Roll(def, Random.Shared);
        return rec;
    }

    /// <summary>Load a character into a live game Entity (used at world entry).
    /// Verifies the character belongs to the account.</summary>
    public async Task<Entity?> LoadCharacterAsync(int accountId, int characterId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var rec = await db.Characters
            .Include(c => c.Items)
            .ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(c => c.Id == characterId && c.AccountId == accountId);

        if (rec is null)
            return null;

        var entity = new Entity
        {
            Name = rec.Name,
            Kind = EntityKind.Player,
            Race = rec.Race,
            BaseClass = rec.BaseClass,
            X = rec.X,
            Y = rec.Y,
            Speed = GameConstants.BasePlayerSpeed,
            Con = rec.Con,
            AtkStat = rec.Atk,
            Wit = rec.Wit,
            Dex = rec.Dex,
            Level = rec.Level,
            Exp = rec.Exp,
            SecondClass = rec.SecondClass,
            PersistentId = rec.Id,
            SkillPoints = rec.SkillPoints
        };

        foreach (var id in rec.LearnedSkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.LearnedSkills.Add(id);

        foreach (var item in rec.Items)
        {
            entity.Inventory.Add(new InventoryItem
            {
                DefId = item.DefId,
                Equipped = item.Equipped,
                Enchant = item.Enchant,
                Quantity = item.Quantity,
                Attributes = item.Attributes.ToList(),
                PersistentInstanceId = item.InstanceId
            });
        }

        entity.RecomputeDerived();
        entity.Hp = entity.MaxHp;
        entity.Mp = entity.MaxMp;
        return entity;
    }

    /// <summary>Persist a live Entity back to its character row (logout / snapshot).
    /// Replaces the item set wholesale — simplest correct approach for now.</summary>
    public async Task SaveCharacterAsync(Entity entity)
    {
        if (entity.PersistentId is not int characterId)
            return;

        await using var db = await _factory.CreateDbContextAsync();

        var rec = await db.Characters
            .Include(c => c.Items)
            .ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (rec is null)
            return;

        rec.Level = entity.Level;
        rec.Exp = entity.Exp;
        rec.SecondClass = entity.SecondClass;
        rec.SkillPoints = entity.SkillPoints;
        rec.LearnedSkillsCsv = string.Join(',', entity.LearnedSkills);
        rec.Con = entity.Con;
        rec.Atk = entity.AtkStat;
        rec.Wit = entity.Wit;
        rec.Dex = entity.Dex;
        rec.X = entity.X;
        rec.Y = entity.Y;

        // Rebuild the item set from the live inventory.
        db.Items.RemoveRange(rec.Items);
        rec.Items = entity.Inventory.Select(i => new ItemRecord
        {
            InstanceId = i.PersistentInstanceId ?? Guid.NewGuid(),
            DefId = i.DefId,
            Equipped = i.Equipped,
            Enchant = i.Enchant,
            Quantity = i.Quantity,
            Attributes = i.Attributes.ToList()
        }).ToList();

        await db.SaveChangesAsync();
    }

    // ----- Boss timers -------------------------------------------------------

    public async Task<Dictionary<string, DateTime>> LoadBossTimersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.BossTimers.ToDictionaryAsync(t => t.ZoneId, t => t.RespawnAtUtc);
    }

    public async Task SaveBossTimerAsync(string zoneId, DateTime respawnAtUtc)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.BossTimers.FirstOrDefaultAsync(t => t.ZoneId == zoneId);
        if (rec is null)
            db.BossTimers.Add(new BossTimerRecord { ZoneId = zoneId, RespawnAtUtc = respawnAtUtc });
        else
            rec.RespawnAtUtc = respawnAtUtc;
        await db.SaveChangesAsync();
    }

    public async Task ClearBossTimerAsync(string zoneId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.BossTimers.FirstOrDefaultAsync(t => t.ZoneId == zoneId);
        if (rec is not null)
        {
            db.BossTimers.Remove(rec);
            await db.SaveChangesAsync();
        }
    }

    // ----- Admin -------------------------------------------------------------

    public async Task<bool> SetBannedByCharacterNameAsync(string characterName, bool banned)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name == characterName);
        if (character is null)
            return false;

        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == character.AccountId);
        if (account is null)
            return false;

        account.IsBanned = banned;
        await db.SaveChangesAsync();
        return true;
    }
}
