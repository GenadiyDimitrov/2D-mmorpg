using Game.Server.Simulation;
using Game.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    /// <summary>Character names on an account, for the selection screen. Permanently
    /// removes any characters whose pending-delete timer has elapsed first.</summary>
    public async Task<List<CharacterSummary>> ListCharactersAsync(int accountId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var now = DateTime.UtcNow;
        var expired = await db.Characters
            .Where(c => c.AccountId == accountId && c.PendingDeleteAt != null && c.PendingDeleteAt <= now)
            .ToListAsync();
        if (expired.Count > 0)
        {
            db.Characters.RemoveRange(expired);
            await db.SaveChangesAsync();
        }

        return await db.Characters
            .Where(c => c.AccountId == accountId)
            .Select(c => new CharacterSummary(
                c.Id, c.Name, c.Race, c.BaseClass, c.SecondClass, c.Level, c.PendingDeleteAt))
            .ToListAsync();
    }

    public record CharacterSummary(
        int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass, int Level,
        DateTime? PendingDeleteAt);

    /// <summary>Schedule (or immediately perform) a character deletion. Returns the
    /// UTC time it will be permanently removed, or null if it was deleted right away
    /// (low level / zero delay). Throws nothing; bad ids are a no-op returning null.</summary>
    public async Task<(bool Ok, DateTime? DeleteAt, string? Error)> RequestDeleteCharacterAsync(
        int accountId, int characterId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.AccountId == accountId);
        if (rec is null)
            return (false, null, "Character not found.");

        var delay = GameConstants.CharacterDeleteDelay(rec.Level);
        if (delay <= TimeSpan.Zero)
        {
            db.Characters.Remove(rec);
            await db.SaveChangesAsync();
            return (true, null, null);          // gone immediately
        }

        rec.PendingDeleteAt = DateTime.UtcNow + delay;
        await db.SaveChangesAsync();
        return (true, rec.PendingDeleteAt, null);
    }

    /// <summary>Cancel a pending deletion (restore the character).</summary>
    public async Task<bool> CancelDeleteCharacterAsync(int accountId, int characterId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.AccountId == accountId);
        if (rec is null || rec.PendingDeleteAt is null)
            return false;
        rec.PendingDeleteAt = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> CreateCharacterAsync(
        int accountId, string name, Race race, BaseClass baseClass)
    {
        name = name.Trim();
        if (name.Length is 0 or > GameConstants.MaxCharacterNameLength)
            return (false, $"Name must be 1-{GameConstants.MaxCharacterNameLength} characters.");

        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Characters.CountAsync(c => c.AccountId == accountId) >= GameConstants.MaxCharactersPerAccount)
            return (false, $"Account is full ({GameConstants.MaxCharactersPerAccount} characters max).");

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

        // Starter gear so a brand-new character isn't empty. All NEWBIE (untradeable,
        // no attributes). Armor + jewels arrive in BOXES the player opens; weapons are
        // direct for now (a weapons selection-box lands next). A mage gets the staff +
        // robe armor box; a fighter gets the four weapons + light armor box.
        if (baseClass == BaseClass.Mage)
        {
            record.Items.Add(NewItem(ItemCatalog.NewbieStaff));
            record.Items.Add(NewItem(ItemCatalog.BoxNewbieArmorRobe));
        }
        else
        {
            record.Items.Add(NewItem(ItemCatalog.BoxNewbieWeapons));   // selection box: pick 2
            record.Items.Add(NewItem(ItemCatalog.BoxNewbieArmorLight));
        }
        record.Items.Add(NewItem(ItemCatalog.BoxNewbieJewels));
        record.Items.Add(NewItem(ItemCatalog.MinorPotion, 5));
        record.Items.Add(NewItem(ItemCatalog.GreaterPotion, 2));

        db.Characters.Add(record);
        await db.SaveChangesAsync();
        return (true, null);
    }

    private static ItemRecord NewItem(string defId, int qty = 1)
    {
        var rec = new ItemRecord { InstanceId = Guid.NewGuid(), DefId = defId, Quantity = qty };
        if (ItemCatalog.Get(defId) is ItemDef def && def.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Jewel)
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

        if (rec is null || rec.PendingDeleteAt is not null)
            return null;   // can't play a character scheduled for deletion

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
            Gold = rec.Gold,
            SecondClass = rec.SecondClass,
            ThirdClass = rec.ThirdClass,
            PersistentId = rec.Id,
            SkillPoints = rec.SkillPoints,
            Profession = (Profession)rec.Profession
        };

        // Learned skills are stored "id:level" (legacy bare "id" = level 1).
        foreach (var token in rec.LearnedSkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = token.IndexOf(':');
            if (colon < 0)
                entity.LearnedSkills[token] = 1;
            else
                entity.LearnedSkills[token[..colon]] =
                    int.TryParse(token[(colon + 1)..], out int lvl) ? Math.Max(1, lvl) : 1;
        }

        foreach (var qid in rec.CompletedQuestsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.CompletedQuests.Add(qid);

        foreach (var rid in rec.KnownRecipesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.KnownRecipes.Add(rid);

        if (!string.IsNullOrEmpty(rec.ActiveQuestsJson))
        {
            try
            {
                var states = JsonSerializer.Deserialize<List<CharacterQuestState>>(rec.ActiveQuestsJson);
                if (states is not null)
                    foreach (var st in states)
                        entity.ActiveQuests[st.QuestId] = st;
            }
            catch { /* ignore malformed quest json */ }
        }

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

    /// <summary>An immutable copy of a character's persistent state, taken on the
    /// game-loop (single-writer) thread. The async DB write reads only this — never a
    /// live, concurrently-mutating <see cref="Entity"/> — so saving can't race the tick
    /// (no torn reads / "collection modified" from X/Y, inventory or skills changing).</summary>
    public sealed record CharacterSnapshot(
        int CharacterId, Race Race, BaseClass BaseClass, int Level, long Exp, long Gold,
        int SecondClass, int ThirdClass, int SkillPoints, int Profession,
        int Con, int Atk, int Wit, int Dex, float X, float Y,
        string LearnedSkillsCsv, string CompletedQuestsCsv, string ActiveQuestsJson,
        string KnownRecipesCsv,
        IReadOnlyList<ItemSnapshot> Items)
    {
        /// <summary>Capture a character. MUST be called on the tick thread. Returns
        /// null for entities with no persistent row (not yet saved).</summary>
        public static CharacterSnapshot? From(Entity e)
        {
            if (e.PersistentId is not int id) return null;
            var items = new List<ItemSnapshot>(e.Inventory.Count);
            foreach (var i in e.Inventory)
                items.Add(new ItemSnapshot(
                    i.PersistentInstanceId ?? Guid.NewGuid(), i.DefId, i.Equipped,
                    i.Enchant, i.Quantity, new List<ItemAttribute>(i.Attributes)));
            return new CharacterSnapshot(
                id, e.Race, e.BaseClass, e.Level, e.Exp, e.Gold,
                e.SecondClass, e.ThirdClass, e.SkillPoints, (int)e.Profession,
                e.Con, e.AtkStat, e.Wit, e.Dex, e.X, e.Y,
                string.Join(',', e.LearnedSkills.Select(kv => $"{kv.Key}:{kv.Value}")),
                string.Join(',', e.CompletedQuests),
                JsonSerializer.Serialize(e.ActiveQuests.Values.ToList()),
                string.Join(',', e.KnownRecipes),
                items);
        }
    }

    public sealed record ItemSnapshot(
        Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity,
        List<ItemAttribute> Attributes);

    /// <summary>Persist one snapshot back to its character row (logout / event save).
    /// Replaces the item set wholesale — simplest correct approach for now.</summary>
    public async Task SaveCharacterAsync(CharacterSnapshot snap)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.Characters
            .Include(c => c.Items).ThenInclude(i => i.Attributes)
            .FirstOrDefaultAsync(c => c.Id == snap.CharacterId);
        if (rec is null)
            return;

        ApplySnapshot(db, rec, snap);
        await db.SaveChangesAsync();
    }

    /// <summary>Batched periodic save: one DbContext, one SaveChanges for the whole
    /// set (avoids a thundering herd of concurrent connections / SQLite write locks).</summary>
    public async Task SaveCharactersAsync(IReadOnlyList<CharacterSnapshot> snaps)
    {
        if (snaps.Count == 0)
            return;

        await using var db = await _factory.CreateDbContextAsync();
        foreach (var snap in snaps)
        {
            var rec = await db.Characters
                .Include(c => c.Items).ThenInclude(i => i.Attributes)
                .FirstOrDefaultAsync(c => c.Id == snap.CharacterId);
            if (rec is not null)
                ApplySnapshot(db, rec, snap);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Copy a snapshot onto a tracked record (no SaveChanges — caller batches).</summary>
    private static void ApplySnapshot(GameDbContext db, CharacterRecord rec, CharacterSnapshot snap)
    {
        rec.Race = snap.Race;               // can change via DEBUG character reset
        rec.BaseClass = snap.BaseClass;
        rec.Level = snap.Level;
        rec.Exp = snap.Exp;
        rec.Gold = snap.Gold;
        rec.SecondClass = snap.SecondClass;
        rec.ThirdClass = snap.ThirdClass;
        rec.Profession = snap.Profession;
        rec.SkillPoints = snap.SkillPoints;
        rec.LearnedSkillsCsv = snap.LearnedSkillsCsv;
        rec.CompletedQuestsCsv = snap.CompletedQuestsCsv;
        rec.ActiveQuestsJson = snap.ActiveQuestsJson;
        rec.KnownRecipesCsv = snap.KnownRecipesCsv;
        rec.Con = snap.Con;
        rec.Atk = snap.Atk;
        rec.Wit = snap.Wit;
        rec.Dex = snap.Dex;
        rec.X = snap.X;
        rec.Y = snap.Y;

        // Rebuild the item set from the snapshot.
        db.Items.RemoveRange(rec.Items);
        rec.Items = snap.Items.Select(i => new ItemRecord
        {
            InstanceId = i.InstanceId,
            DefId = i.DefId,
            Equipped = i.Equipped,
            Enchant = i.Enchant,
            Quantity = i.Quantity,
            Attributes = i.Attributes.ToList()
        }).ToList();
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
