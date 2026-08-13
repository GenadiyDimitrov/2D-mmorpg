using Game.Server.Simulation;
using Game.Shared;
using Microsoft.Data.Sqlite;
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

    /// <summary>Character saves run SERIALLY, never concurrently.
    ///
    /// <see cref="ApplySnapshot"/> rebuilds a character's item set wholesale
    /// (<c>db.Items.RemoveRange(rec.Items)</c> then re-add). Saves are fired off the tick thread onto
    /// the thread pool, so two of them could overlap for the SAME character: both load items
    /// [i1, i2], both queue DELETE i1/i2, the first commits, and the second's DELETE then affects 0
    /// rows → <c>DbUpdateConcurrencyException</c> and the whole save is LOST. That is silent data
    /// loss, and it fired constantly once the skill bar started saving on every rearrangement.
    ///
    /// SQLite is a single-writer store anyway, so there is nothing to gain from parallel writes.
    /// One gate for every save path keeps it correct no matter how many call sites appear later.</summary>
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public PersistenceService(IDbContextFactory<GameDbContext> factory) => _factory = factory;

    public async Task EnsureCreatedAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }

#if DEBUG
    /// <summary>DEV ONLY — DESTRUCTIVE. If the database on disk no longer matches the model, DELETE it
    /// and build a fresh one. Returns true if it did.
    ///
    /// `EnsureCreated` only creates a database when the file is ABSENT; it never adds a column to an
    /// existing one. So every schema change during development means deleting `game.db` by hand, and
    /// forgetting is not a quiet failure — the server starts fine and then throws "table Characters has
    /// no column named X" on the first save, from deep inside an EF batch, which reads like a bug in
    /// whatever you were building rather than the stale file it is. (It cost a debugging cycle this very
    /// session.) The owner's rule while in development: no character is worth preserving, so just wipe it.
    ///
    /// It is `#if DEBUG` and it will STAY that way. Migrations are the real answer the moment there is
    /// data worth keeping, and until then a release build must never be able to delete a database. The
    /// day this project ships, this method should not exist to be called by accident.</summary>
    public async Task<bool> ResetIfSchemaStaleAsync(ILogger? log = null)
    {
        string? path;
        await using (var db = await _factory.CreateDbContextAsync())
        {
            path = db.Database.GetDbConnection().DataSource;
            if (!File.Exists(path)) return false;   // nothing on disk; EnsureCreated will make it

            // Touch every table the game actually writes. Reading one row materialises ALL mapped
            // columns, so a missing column fails here — at startup, in one obvious place — instead of
            // mid-save later. A cheap query against an empty table is free.
            // OrderBy(Id) on each: EF warns (10103) about First/FirstOrDefault with neither a filter nor
            // an order, because the row you get is then whatever the engine felt like returning. It does
            // not matter here — any row materialises the columns, which is the whole point — but the
            // warning was real noise on every startup, and "any row" is cheap to state explicitly.
            try
            {
                _ = await db.Accounts.OrderBy(x => x.Id).FirstOrDefaultAsync();
                _ = await db.Characters.OrderBy(x => x.Id).FirstOrDefaultAsync();
                _ = await db.Subclasses.OrderBy(x => x.Id).FirstOrDefaultAsync();
                _ = await db.Items.OrderBy(x => x.Id).FirstOrDefaultAsync();
                _ = await db.BossTimers.OrderBy(x => x.Id).FirstOrDefaultAsync();
                return false;   // schema is current
            }
            catch (SqliteException ex)
            {
                log?.LogWarning("Database schema is stale ({Message}). Recreating {Path}.",
                    ex.Message, path);
            }
        }

        // The context is disposed, but SQLite POOLS connections and a pooled one keeps a file handle —
        // deleting without this throws "file in use" on Windows.
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { path, path + "-shm", path + "-wal" })
            if (File.Exists(file)) File.Delete(file);

        await using (var fresh = await _factory.CreateDbContextAsync())
            await fresh.Database.EnsureCreatedAsync();

        log?.LogWarning("Database recreated from scratch — all characters were discarded (DEBUG only).");
        return true;
    }
#endif

    // ----- Accounts ----------------------------------------------------------

    /// <summary><paramref name="IsFirstAccount"/> marks the very first account created on a fresh server
    /// (the owner's). It is NOT an authorization signal — staff powers live on the CHARACTER's Role and
    /// are checked at EnterWorld — it only decides that this account's characters start as Admin.</summary>
    public record AuthResult(bool Success, string? Error, int AccountId, bool IsFirstAccount);

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

        // First account ever created is the owner's — its characters are created as Admin (see
        // CreateCharacterAsync). The role itself lives on the CHARACTER now, not here.
        bool isFirst = !await db.Accounts.AnyAsync();

        var account = new AccountRecord
        {
            Username = username,
            PasswordHash = hash,
            PasswordSalt = salt,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return new AuthResult(true, null, account.Id, isFirst);
    }

    /// <summary>DEBUG convenience: on an EMPTY db, create ready-to-play accounts so you can skip the
    /// (already-tested) register/create flow — just log in, pick the char, enter.
    ///
    /// admin/admin gets a FULLY KITTED character (see <see cref="EndgameKitAsync"/>): a level-90 Human
    /// Warchanter in A-grade robe/staff/jewels with every class skill learned. The schema changes often
    /// during development and every change means deleting the DB, so rebuilding that character by hand
    /// each time was pure repeated toil (owner, 2026-07-20).
    /// test1..test9/test stay plain level-1 Human Fighters — they're the "ordinary player" side of every
    /// moderation and party test, and kitting them out would defeat that.
    ///
    /// No-op if any account already exists.</summary>
    public async Task SeedDebugAccountsAsync()
    {
        await using (var db = await _factory.CreateDbContextAsync())
        {
            if (await db.Accounts.AnyAsync())
                return;   // only seed a fresh, empty database
        }

        async Task SeedAsync(string user, string pass, string charName)
        {
            var acc = await RegisterAsync(user, pass);   // the FIRST account created becomes the owner's
            if (acc.Success)
                await CreateCharacterAsync(acc.AccountId, charName, Race.Human, BaseClass.Fighter);
        }

        await SeedAsync("admin", "admin", "Admin");
        await EndgameKitAsync("Admin");
        for (int i = 1; i <= 9; i++)
            await SeedAsync($"test{i}", "test", $"Test{i}");
    }

    /// <summary>Turn a freshly-created character into a ready-to-test level-90 Human Warchanter in full
    /// A-grade gear, with every class skill learned.
    ///
    /// Everything here is DERIVED rather than hardcoded — the class comes from
    /// <see cref="ThirdClassCatalog"/> by race+discipline, the skills from
    /// <see cref="ClassSkills.Cumulative"/>, the gear from the tier the item catalog itself calls
    /// A-grade. Hardcoded ids would be silently wrong the first time a catalog moved, and a seed that
    /// quietly stops matching the game is worse than no seed: you'd be balance-testing gear that no
    /// longer exists.
    ///
    /// The skill BAR is left EMPTY. Skills are not auto-placed any more (owner) — not here and not by
    /// the server — so a kitted character's 18 skills stay off the bar until the player arranges them,
    /// exactly like any other character.</summary>
    private async Task EndgameKitAsync(string characterName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var character = await db.Characters
            .Include(c => c.Subclasses)
            .Include(c => c.Items)
            .AsSplitQuery()   // two collections in one statement = cartesian product; see LoadCharacterAsync
            .FirstOrDefaultAsync(c => c.Name == characterName);
        if (character is null) return;

        // ---- Class: Human Cleric -> Warchanter (looked up, never hardcoded) ----
        var warchanter = ThirdClassCatalog.Playable
            .FirstOrDefault(c => c.Race == Race.Human && c.Discipline == Discipline.Warchanter);
        if (warchanter is null) return;   // catalog changed; leave the plain starter character alone
        var cleric = ClassCatalog.Get(warchanter.ParentSecondClassId);
        if (cleric is null) return;

        const int level = GameConstants.MaxPlayerLevel;   // 90
        var stats = StatCalculator.GetBaseStats(Race.Human, cleric.Base);

        character.BaseClass = cleric.Base;
        character.SecondClass = cleric.Id;
        character.ThirdClass = warchanter.Id;
        character.Level = level;
        character.Exp = 0;
        character.Gold = 1_000_000_000;   // enough to buy anything while testing
        character.Con = stats.Con;
        character.Atk = stats.Atk;
        character.Wit = stats.Wit;
        character.Agi = stats.Agi;
        character.Spt = stats.Spt;

        // ---- Skills: every class skill whose learn-gate this level meets ----
        var learned = new Dictionary<string, int>();
        foreach (var cs in ClassSkills.Cumulative(
                     Race.Human, cleric.Base, cleric.Archetype, warchanter.Discipline))
        {
            if (cs.LearnLevel > level) continue;
            if (!learned.TryGetValue(cs.SkillId, out int have) || cs.SkillLevel > have)
                learned[cs.SkillId] = cs.SkillLevel;
        }
        // Stat swaps are a permanent BUILD decision, and granting them all cancels out to roughly +0
        // while quietly wrecking the damage numbers — the same reason the debug "learn all" button
        // refuses them. Buy them deliberately in the skills window.
        foreach (var id in learned.Keys.Where(id => SkillCatalog.StatSwapOf(id) is not null).ToList())
            learned.Remove(id);
        // Cross-skill replacements (a higher-tier spell removes the one it supersedes).
        foreach (var id in learned.Keys.ToList())
            if (SkillCatalog.Get(id)?.Replaces is { } replaced)
                foreach (var r in replaced) learned.Remove(r);
        // (The old training passive is gone — the admin tests the runes via the 30-day boxes added below.)

        string learnedCsv = string.Join(',', learned.Select(kv => $"{kv.Key}:{kv.Value}"));
        character.LearnedSkillsCsv = learnedCsv;
        character.SkillPoints = 0;

        // Mirror onto the active subclass row, which is the real source of truth for per-class state.
        var slot0 = character.Subclasses.FirstOrDefault(s => s.Slot == 0);
        if (slot0 is not null)
        {
            slot0.BaseClass = cleric.Base;
            slot0.SecondClass = cleric.Id;
            slot0.ThirdClass = warchanter.Id;
            slot0.Level = level;
            slot0.Exp = 0;
            slot0.Con = stats.Con;
            slot0.Atk = stats.Atk;
            slot0.Wit = stats.Wit;
            slot0.Agi = stats.Agi;
            slot0.Spt = stats.Spt;
            slot0.LearnedSkillsCsv = learnedCsv;
            slot0.SkillBarJson = "";   // empty bar — the player arranges it themselves
        }

        // ---- Gear: the A-grade tier, EQUIPPED. A caster kit: staff + robe + jewels. ----
        character.Items.Clear();   // drop the newbie staff and starter boxes
        foreach (var defId in EndgameKitItemIds())
        {
            var item = NewItem(defId);
            item.Equipped = true;
            character.Items.Add(item);
        }
        character.Items.Add(NewItem(ItemCatalog.GreaterPotion, 100));
        character.Items.Add(NewItem(ItemCatalog.ScrollReturnUltimate, 20));
        // Admin gets both 30-day rune boxes to test the rune system straight away (open → 30d rune).
        character.Items.Add(NewItem(ItemCatalog.BoxWarRune30d));
        character.Items.Add(NewItem(ItemCatalog.BoxSpellRune30d));

        await db.SaveChangesAsync();
    }

    /// <summary>The A-grade caster kit, as item ids. A-grade is the top gear tier
    /// (<see cref="ItemCatalog.TierLetter"/> calls level 76+ "A"), and the tiered ids are
    /// "&lt;key&gt;_t&lt;level&gt;". IG jewel layout: 1 necklace, 2 rings, 2 earrings.
    /// Anything the catalog doesn't have is skipped rather than crashing the seed.</summary>
    private static IEnumerable<string> EndgameKitItemIds()
    {
        const int aGrade = 76;
        var ids = new[]
        {
            $"staff_t{aGrade}", $"robe_t{aGrade}",
            $"helm_t{aGrade}", $"gloves_t{aGrade}", $"boots_t{aGrade}",
            $"necklace_t{aGrade}",
            $"ring_t{aGrade}", $"ring_t{aGrade}",
            $"earring_t{aGrade}", $"earring_t{aGrade}",
        };
        return ids.Where(id => ItemCatalog.Get(id) is not null);
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
        if (account.BannedUntilUtc is DateTime until && until > DateTime.UtcNow)
            return new AuthResult(false, $"This account is banned for another {Remaining(until)}.", 0, false);

        // Staff status is NOT decided here any more — it belongs to whichever CHARACTER you then enter
        // the world with (owner). Login only proves who the account is.
        return new AuthResult(true, null, account.Id, false);
    }

    /// <summary>A short "2d 3h" / "5m" style remaining-time string for a UTC deadline.</summary>
    private static string Remaining(DateTime utcUntil)
    {
        var r = utcUntil - DateTime.UtcNow;
        if (r <= TimeSpan.Zero) return "moments";
        if (r.TotalDays >= 1) return $"{(int)r.TotalDays}d {r.Hours}h";
        if (r.TotalHours >= 1) return $"{(int)r.TotalHours}h {r.Minutes}m";
        if (r.TotalMinutes >= 1) return $"{(int)r.TotalMinutes}m";
        return $"{(int)r.TotalSeconds}s";
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
                c.Id, c.Name, c.Race, c.BaseClass, c.SecondClass, c.Level, c.PendingDeleteAt,
                c.ThirdClass))
            .ToListAsync();
    }

    public record CharacterSummary(
        int Id, string Name, Race Race, BaseClass BaseClass, int SecondClass, int Level,
        DateTime? PendingDeleteAt, int ThirdClass = 0);

    /// <summary>Top <paramref name="count"/> characters for one leaderboard category, read straight from
    /// the DB (so it reflects the last autosave — good enough for a board; live values lag ≤60s).
    /// pvp/pk/gold/online exclude zero-value rows so an empty board shows no one, not random level-1s.</summary>
    public async Task<LeaderboardDto> GetLeaderboardAsync(string category, int count)
    {
        await using var db = await _factory.CreateDbContextAsync();
        // ADMINS are excluded from every board (owner): an admin can /level to 999 and would sit atop the
        // level/online boards forever, which no real player could ever reach. The role is per CHARACTER,
        // so an ordinary character on the admin's own account still ranks — only admin characters are
        // hidden. (Moderators are NOT excluded — they are real players; flag if that should change.)
        var q = db.Characters.Where(c => c.PendingDeleteAt == null && c.Role != AccountRole.Admin);

        List<CharacterRecord> rows = category switch
        {
            "gold"   => await q.Where(c => c.Gold > 0).OrderByDescending(c => c.Gold).Take(count).ToListAsync(),
            "pvp"    => await q.Where(c => c.PvpCount > 0).OrderByDescending(c => c.PvpCount).Take(count).ToListAsync(),
            "pk"     => await q.Where(c => c.PkCount > 0).OrderByDescending(c => c.PkCount).Take(count).ToListAsync(),
            "online" => await q.Where(c => c.TotalOnlineSeconds > 0).OrderByDescending(c => c.TotalOnlineSeconds).Take(count).ToListAsync(),
            "charisma" => await q.Where(c => c.CharismaLifetime > 0).OrderByDescending(c => c.CharismaLifetime).Take(count).ToListAsync(),
            _        => await q.OrderByDescending(c => c.Level).ThenByDescending(c => c.Exp).Take(count).ToListAsync(),
        };

        long Value(CharacterRecord c) => category switch
        {
            "gold"   => c.Gold,
            "pvp"    => c.PvpCount,
            "pk"     => c.PkCount,
            "online" => c.TotalOnlineSeconds,
            "charisma" => c.CharismaLifetime,
            _        => c.Level,
        };

        var entries = rows
            // Display TEXT, like every other title on the wire. The board's own colour is not sent with
            // it: a row already knows which category it is looking at, so the client colours from that.
            .Select((c, i) => new LeaderboardEntry(i + 1, c.Name, c.Level, Value(c),
                                                   i == 0 ? TitleCatalog.Text(category) : ""))
            .ToList();
        return new LeaderboardDto(category, entries);
    }

    /// <summary>
    /// Who is rank 1 of each board right now — the holder of each wearable title.
    ///
    /// Deliberately implemented as one <see cref="GetLeaderboardAsync"/> call per category rather than
    /// its own query: the board's rules (admins excluded, zero rows excluded, the tie-breaks) are not
    /// obvious, and a second copy of them would drift until the title over a head disagreed with the
    /// board the player is looking at. Six top-1 reads every few minutes is nothing.
    ///
    /// Keyed by character NAME because that is all a board row carries, and names are unique.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> GetTitleHoldersAsync()
    {
        var byName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in Leaderboards.Categories)
        {
            var board = await GetLeaderboardAsync(cat, 1);
            if (board.Entries.Count == 0) continue;
            string name = board.Entries[0].Name;
            if (!byName.TryGetValue(name, out var list)) byName[name] = list = new List<string>();
            list.Add(cat);
        }
        return byName;
    }

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

        // Staff characters get the short undo window in every build — see CharacterDeleteDelay.
        var delay = GameConstants.CharacterDeleteDelay(rec.Level, rec.Role == AccountRole.Admin);
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

        int existingChars = await db.Characters.CountAsync(c => c.AccountId == accountId);
        if (existingChars >= GameConstants.MaxCharactersPerAccount)
            return (false, $"Account is full ({GameConstants.MaxCharactersPerAccount} characters max).");

        // Case-insensitive: "Test1" and "test1" must not both exist, or every name-targeted command
        // (jail/kick/ban/whisper/friend) becomes ambiguous.
        var nameLower = name.ToLower();
        if (await db.Characters.AnyAsync(c => c.Name.ToLower() == nameLower))
            return (false, "That character name is taken.");

        // Only the very FIRST character of the very FIRST account on a fresh server is born Admin
        // (convenient for testing — it's the owner's seeded "Admin"). Every LATER character, even on that
        // same account, starts a plain Player and is promoted with /role.
        //
        // This used to make EVERY character of the owner's account an Admin, which quietly broke the
        // per-character role model (see CharacterRecord.Role): the owner made an ordinary character to
        // play as a normal player and it still had every admin command. The role is per CHARACTER on
        // purpose — an account may hold an admin character alongside perfectly ordinary ones.
        bool ownerAccount = accountId == await db.Accounts.OrderBy(a => a.Id).Select(a => a.Id).FirstAsync();
        bool bornAdmin = ownerAccount && existingChars == 0;

        var stats = StatCalculator.GetBaseStats(race, baseClass);
        var record = new CharacterRecord
        {
            AccountId = accountId,
            Role = bornAdmin ? AccountRole.Admin : AccountRole.Player,
            Name = name,
            Race = race,
            BaseClass = baseClass,
            Con = stats.Con,
            Atk = stats.Atk,
            Wit = stats.Wit,
            Agi = stats.Agi,
            Spt = stats.Spt,
            X = GameConstants.ZoneWidth / 2,
            Y = GameConstants.ZoneHeight / 2,
        };

        // The character's FIRST class (slot 0). The load path can also reconstruct this from the mirror
        // columns above — that fallback exists for rows written before subclasses — but a character
        // born today should have a real subclass row from the start, not depend on it.
        record.Subclasses.Add(new SubclassRecord
        {
            Slot = 0,
            Race = race,
            BaseClass = baseClass,
            Level = 1,
            Con = stats.Con,
            Atk = stats.Atk,
            Wit = stats.Wit,
            Agi = stats.Agi,
            Spt = stats.Spt,
            // The bar starts EMPTY (owner). Nothing is auto-placed — the player builds it from the
            // skills window's Skills and Actions tabs.
        });

        // 🔴 NO STARTER BOXES AT CREATION (him, 63j). A new character used to be born holding the two
        // training boxes, and Cera's quest handed them over again, and Pell's step a third time — he
        // finished part 1 with THREE armours and THREE weapons: "Make no inital boxes. After Ceras talk
        // and after I talk to Pell then to get my boxes -> Then so I get the boxes exactly before I need
        // to open them." The tutorial's box step supplies its own props (QuestStep.SupplyItemIds), so
        // creation handing out a set as well was the duplicate, not the quest.
        // ⚠ This also retires the dead end of 0.60.1 at its source: with nothing to open before the
        // quest asks, "open a box" cannot be spent early.
        //
        // Explicitly NO runes and NO jewels at creation (owner). Jewels are earned — the broken line
        // drops from level 1-5 mobs and is sold in the shop — and runes come with the quest.
        record.Items.Add(NewItem(ItemCatalog.MinorPotion, 5));
        record.Items.Add(NewItem(ItemCatalog.GreaterPotion, 2));

        db.Characters.Add(record);
        await db.SaveChangesAsync();
        return (true, null);
    }

    private static ItemRecord NewItem(string defId, int qty = 1)
    {
        var rec = new ItemRecord { InstanceId = Guid.NewGuid(), DefId = defId, Quantity = qty };
        // 0.45.0: items are created BARE — attributes come only from an attribute scroll.
        // The god-tier one-offs keep their authored FixedAttributes.
        if (ItemCatalog.Get(defId) is ItemDef def && def.FixedAttributes is { Length: > 0 } fixedAttrs)
            rec.Attributes = fixedAttrs.ToList();
        return rec;
    }

    /// <summary>Learned skills are stored "id:level" (legacy bare "id" = level 1).</summary>
    /// <summary>Read a saved `id:level,id:level` row back into a learned-skills map, DROPPING any id the
    /// catalog no longer knows.
    ///
    /// The filter is the point (2026-08-12, found while deleting `test_heal` for `BL-37`). Skill ids are
    /// append-only by convention, but ids do get RETIRED — `hp_boost` went with the God layer, the archer
    /// masteries went with the archer→rogue merge, and now the test heal. A retired id kept sitting in
    /// `LearnedSkillsCsv` forever, and `SendLearned` pushes the map's keys verbatim: the client then had a
    /// `SkillRef` it could not resolve in the catalog. A save wrote it straight back, so it was permanent.
    /// Filtering HERE — at the one seam where stored text becomes runtime state — means deleting a skill
    /// is a one-file job from now on, which is what made this worth fixing rather than special-casing.</summary>
    private static void ParseLearnedSkills(string csv, Dictionary<string, int> into)
    {
        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = token.IndexOf(':');
            string id = colon < 0 ? token : token[..colon];
            if (SkillCatalog.Get(id) is null) continue;   // retired id — let it die on load
            into[id] = colon < 0 ? 1
                     : int.TryParse(token[(colon + 1)..], out int lvl) ? Math.Max(1, lvl) : 1;
        }
    }

    private static Subclass ToSubclass(SubclassRecord r)
    {
        var sc = new Subclass
        {
            Slot = r.Slot,
            Race = r.Race,
            BaseClass = r.BaseClass,
            SecondClass = r.SecondClass,
            ThirdClass = r.ThirdClass,
            Level = r.Level,
            Exp = r.Exp,
            SkillPoints = r.SkillPoints,
            Con = r.Con, Atk = r.Atk, Wit = r.Wit, Agi = r.Agi, Spt = r.Spt,
        };
        ParseLearnedSkills(r.LearnedSkillsCsv, sc.LearnedSkills);
        if (!string.IsNullOrEmpty(r.SkillBarJson))
        {
            try { sc.SkillBar = JsonSerializer.Deserialize<string[]>(r.SkillBarJson) ?? Array.Empty<string>(); }
            catch { /* ignore malformed skill-bar json */ }
        }
        return sc;
    }

    /// <summary>Load a character into a live game Entity (used at world entry).
    /// Verifies the character belongs to the account.</summary>
    public async Task<Entity?> LoadCharacterAsync(int accountId, int characterId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // SPLIT QUERY. Pulling Items→Attributes AND Subclasses in one statement is a cartesian product:
        // EF joins them all, so the row count is (items × attributes) × subclasses and every row drags a
        // full copy of the character. A geared character with a stocked warehouse turns a ~50-row read
        // into hundreds. Two extra round trips against a local SQLite file cost far less than that
        // multiplication — and this is the LOGIN path, so it runs for every player entering the world.
        // (This is also what EF's 20504 warning was pointing at.)
        var rec = await db.Characters
            .Include(c => c.Items)
            .ThenInclude(i => i.Attributes)
            .Include(c => c.Subclasses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == characterId && c.AccountId == accountId);

        if (rec is null || rec.PendingDeleteAt is not null)
            return null;   // can't play a character scheduled for deletion

        var entity = new Entity
        {
            Name = rec.Name,
            Kind = EntityKind.Player,
            Race = rec.Race,            // CHARACTER-level: one body, several trainings
            X = rec.X,
            Y = rec.Y,
            Speed = GameConstants.BasePlayerSpeed,
            Gold = rec.Gold,
            PersistentId = rec.Id,
            AccountId = rec.AccountId,
            Profession = (Profession)rec.Profession
        };

        // ---- CLASSES. The subclass rows are the source of truth for anything class-level. A
        // character created before subclasses existed (or a brand-new one) has no rows yet, so slot 0
        // is reconstructed from the character row's mirror columns.
        entity.Subclasses.Clear();
        if (rec.Subclasses.Count > 0)
        {
            foreach (var sc in rec.Subclasses.OrderBy(s => s.Slot))
                entity.Subclasses.Add(ToSubclass(sc));
        }
        else
        {
            var main = new Subclass
            {
                Slot = 0,
                Race = rec.Race,
                BaseClass = rec.BaseClass,
                SecondClass = rec.SecondClass,
                ThirdClass = rec.ThirdClass,
                Level = rec.Level,
                Exp = rec.Exp,
                SkillPoints = rec.SkillPoints,
                Con = rec.Con, Atk = rec.Atk, Wit = rec.Wit, Agi = rec.Agi, Spt = rec.Spt,
            };
            ParseLearnedSkills(rec.LearnedSkillsCsv, main.LearnedSkills);
            entity.Subclasses.Add(main);
        }
        entity.SwitchSubclass(rec.ActiveSubclassSlot);

        foreach (var qid in rec.CompletedQuestsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.CompletedQuests.Add(qid);

        foreach (var rid in rec.KnownRecipesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.KnownRecipes.Add(rid);

        foreach (var fn in rec.FriendsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.Friends.Add(fn);
        foreach (var bn in rec.BlockedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            entity.Blocked.Add(bn);
        entity.Social = (SocialOptions)rec.SocialOptions;
        entity.Charisma = rec.Charisma;
        entity.CharismaLifetime = rec.CharismaLifetime;
        entity.LikesRemainingToday = rec.LikesRemainingToday;
        entity.LikeBudgetDay = rec.LikeBudgetDay;
        // The CHOICE comes back; whether it is still HELD is decided by the loop's title refresh, which
        // is what fills entity.Title. A choice for a board you have since lost simply draws nothing.
        entity.TitleCategory = rec.TitleCategory ?? "";
        entity.CustomTitle = rec.CustomTitle ?? "";
        entity.CustomTitleColor = rec.CustomTitleColor ?? "";
        entity.MayWriteTitle = rec.MayWriteTitle;

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

        // Buffs are PARKED, not applied: rebuilding one goes through GameLoopService.ApplyBuff (stat
        // recompute, stacking rules, pushes), which is tick-thread work — this is a DB load. The game
        // loop drains PendingBuffs when the character enters the world.
        if (!string.IsNullOrEmpty(rec.BuffsJson))
        {
            try
            {
                var buffs = JsonSerializer.Deserialize<List<BuffSnapshot>>(rec.BuffsJson);
                if (buffs is not null) entity.PendingBuffs.AddRange(buffs);
            }
            catch { /* ignore malformed buff json */ }
        }

        if (!string.IsNullOrEmpty(rec.AutoHuntJson))
        {
            try
            {
                var cfg = JsonSerializer.Deserialize<AutoHuntConfigDto>(rec.AutoHuntJson);
                if (cfg is not null)
                {
                    entity.AutoHuntEnabled = cfg.Enabled;
                    entity.AutoHpPotionPct = cfg.HpPotionPct;
                    entity.AutoMpPotionPct = cfg.MpPotionPct;
                    entity.AutoBuffPotions = cfg.AutoBuffPotions;
                    foreach (var s in cfg.Skills ?? Array.Empty<AutoSkillDto>())
                        entity.AutoSkills.Add(s);
                    foreach (var id in cfg.BuffPotionIds ?? Array.Empty<string>())
                        entity.AutoBuffPotionIds.Add(id);
                    foreach (var hp in cfg.HealPotions ?? Array.Empty<AutoPotionDto>())
                        entity.AutoHealPotions.Add(hp);
                    foreach (var b in cfg.Buffs ?? Array.Empty<AutoBuffDto>())
                        entity.AutoBuffs.Add(b);
                    entity.AutoFarmRange = cfg.FarmRange <= 0 ? 1000 : Math.Clamp(cfg.FarmRange, 200, 2000);
                    entity.AutoFarmStatic = cfg.StaticSpot;
                    entity.AutoAttackNormal = cfg.AttackNormal;
                    entity.AutoAttackElite = cfg.AttackElite;
                    entity.AutoAttackBoss = cfg.AttackBoss;
                    entity.AutoCyclic = cfg.CyclicOrder;
                    entity.AutoHealPct = Math.Clamp(cfg.HealThresholdPct, 0, 100);
                    entity.AutoAssistLeader = cfg.AssistPartyLeader;
                }
            }
            catch { /* ignore malformed auto-hunt json */ }
        }

        if (!string.IsNullOrEmpty(rec.EquipPresetsJson))
        {
            try
            {
                var presets = JsonSerializer.Deserialize<Guid[][]>(rec.EquipPresetsJson);
                if (presets is not null)
                    for (int i = 0; i < entity.EquipPresets.Length && i < presets.Length; i++)
                    {
                        entity.EquipPresets[i].Clear();
                        if (presets[i] is not null) entity.EquipPresets[i].AddRange(presets[i]);
                    }
            }
            catch { /* ignore malformed preset json */ }
        }

        // Clamp on load: karma is never negative, and this heals any row corrupted by the old
        // overflow bug (a big-level-gap PK cast a huge double to int → int.MinValue).
        entity.Karma = Math.Clamp(rec.Karma, 0, 1_000_000);
        entity.PkCount = rec.PkCount;
        entity.PvpCount = rec.PvpCount;
        entity.ConsecutivePk = rec.ConsecutivePk;
        entity.TotalOnlineSeconds = rec.TotalOnlineSeconds;

        foreach (var item in rec.Items)
        {
            // A timed item (rune) whose wall-clock ran out while offline is purged on load — it never
            // reaches the bag, so the player logs in without the spent rune (and without its buff).
            if (item.ExpiresAtUtc is DateTime exp && exp <= DateTime.UtcNow)
                continue;

            // An item whose DefId is no longer in the catalog is DROPPED rather than loaded. Without
            // this a renamed or retired def leaves a ghost row in the bag: every `ItemCatalog.Get`
            // downstream returns null, so it has no name, no slot and no price, and the client draws a
            // blank it cannot click. Renames are the common cause (0.42.4 renamed the war/spell runes
            // and their boxes), which used to mean "delete game.db"; now the stale rows simply vanish.
            if (ItemCatalog.Get(item.DefId) is null)
                continue;

            // Warehoused items load into the bank list, never equipped; everything else into the bag.
            (item.InWarehouse ? entity.Warehouse : entity.Inventory).Add(new InventoryItem
            {
                DefId = item.DefId,
                Equipped = item.Equipped && !item.InWarehouse,
                Enchant = item.Enchant,
                Quantity = item.Quantity,
                Attributes = item.Attributes.ToList(),
                PersistentInstanceId = item.InstanceId,
                ExpiresAtUtc = item.ExpiresAtUtc,
                SellPriceOverride = item.SellPriceOverride,
                TradableOverride = item.TradableOverride,
                CustomName = item.CustomName,
                CanStorePrivate = item.CanStorePrivate,
                CanStoreAccount = item.CanStoreAccount,
            });
        }

        entity.RecomputeDerived();
        entity.Mp = entity.MaxMp;
        // Logged out DEAD? The death STICKS — log in DEAD (res prompt), not healed. True for ANY death
        // (a normal one, an offline-farm one, or one during the link-dead grace): otherwise "die → exit
        // to character select → log back in at full HP" is a free death-dodge. Stays dead across relogs
        // until the death is paid for — a town respawn (HandleRespawn) or a res (ResurrectTarget).
        entity.DiedWhileAway = rec.DiedWhileAway;
        if (entity.DiedWhileAway)
        {
            entity.Dead = true;
            entity.Hp = 0;
        }
        else
        {
            entity.Hp = entity.MaxHp;
        }

        // JAIL survives a relog: load the sentence, and if it's still active, spawn IN jail (so a jailed
        // player can't escape by logging out). An expired sentence is cleared on save.
        entity.JailedUntil = rec.JailedUntilUtc;
        if (entity.Jailed)
        {
            // Spread across the yard like any other arrival — see GameConstants.JailArrival. Relogging
            // used to put you back on the exact centre coordinate with everyone else.
            (entity.X, entity.Y) = GameConstants.JailArrival(Random.Shared);
        }
        entity.ChatBannedUntil = rec.ChatBannedUntilUtc;
        entity.Role = rec.Role;   // staff role is per CHARACTER, not per account (owner)
        return entity;
    }

    /// <summary>An immutable copy of a character's persistent state, taken on the
    /// game-loop (single-writer) thread. The async DB write reads only this — never a
    /// live, concurrently-mutating <see cref="Entity"/> — so saving can't race the tick
    /// (no torn reads / "collection modified" from X/Y, inventory or skills changing).</summary>
    /// <summary>One owned class, captured for saving.</summary>
    public sealed record SubclassSnapshot(
        int Slot, Race Race, BaseClass BaseClass, int SecondClass, int ThirdClass,
        int Level, long Exp, int SkillPoints,
        int Con, int Atk, int Wit, int Agi, int Spt,
        string LearnedSkillsCsv, string SkillBarJson)
    {
        public static SubclassSnapshot From(Subclass s) => new(
            s.Slot, s.Race, s.BaseClass, s.SecondClass, s.ThirdClass,
            s.Level, s.Exp, s.SkillPoints,
            s.Con, s.Atk, s.Wit, s.Agi, s.Spt,
            string.Join(',', s.LearnedSkills.Select(kv => $"{kv.Key}:{kv.Value}")),
            JsonSerializer.Serialize(s.SkillBar));
    }

    /// <summary>The BaseClass / Level / Exp / SkillPoints / Con..Agi / LearnedSkillsCsv fields here are
    /// the ACTIVE subclass's values. They are written back to the character row as a MIRROR so the
    /// character-select screen can list a character without loading its classes — the real per-class
    /// state travels in <see cref="Subclasses"/>, which is the source of truth.</summary>
    public sealed record CharacterSnapshot(
        int CharacterId, Race Race, BaseClass BaseClass, int Level, long Exp, long Gold,
        int SecondClass, int ThirdClass, int SkillPoints, int Profession,
        int Con, int Atk, int Wit, int Agi, int Spt, float X, float Y,
        string LearnedSkillsCsv, string CompletedQuestsCsv, string ActiveQuestsJson,
        string KnownRecipesCsv, string FriendsCsv, string BlockedCsv, string AutoHuntJson, string EquipPresetsJson,
        string BuffsJson,
        int ActiveSubclassSlot, IReadOnlyList<SubclassSnapshot> Subclasses,
        int Karma, int PkCount, int PvpCount, int ConsecutivePk, bool DiedWhileAway,
        DateTime? JailedUntilUtc, DateTime? ChatBannedUntilUtc, long TotalOnlineSeconds,
        int Charisma, long CharismaLifetime, int LikesRemainingToday, string LikeBudgetDay,
        string TitleCategory, string CustomTitle, string CustomTitleColor, bool MayWriteTitle,
        int SocialOptions,
        IReadOnlyList<ItemSnapshot> Items)
    {
        /// <summary>Capture a character. MUST be called on the tick thread. Returns
        /// null for entities with no persistent row (not yet saved).</summary>
        public static CharacterSnapshot? From(Entity e)
        {
            if (e.PersistentId is not int id) return null;
            var items = new List<ItemSnapshot>(e.Inventory.Count + e.Warehouse.Count);
            foreach (var i in e.Inventory)
                items.Add(new ItemSnapshot(
                    i.PersistentInstanceId ?? Guid.NewGuid(), i.DefId, i.Equipped,
                    i.Enchant, i.Quantity, new List<ItemAttribute>(i.Attributes), i.ExpiresAtUtc,
                    SellPriceOverride: i.SellPriceOverride, TradableOverride: i.TradableOverride,
                    CustomName: i.CustomName, CanStorePrivate: i.CanStorePrivate,
                    CanStoreAccount: i.CanStoreAccount));
            foreach (var i in e.Warehouse)
                items.Add(new ItemSnapshot(
                    i.PersistentInstanceId ?? Guid.NewGuid(), i.DefId, false,   // never equipped in the bank
                    i.Enchant, i.Quantity, new List<ItemAttribute>(i.Attributes), i.ExpiresAtUtc, InWarehouse: true,
                    SellPriceOverride: i.SellPriceOverride, TradableOverride: i.TradableOverride,
                    CustomName: i.CustomName, CanStorePrivate: i.CanStorePrivate,
                    CanStoreAccount: i.CanStoreAccount));

            var subs = e.Subclasses.Select(SubclassSnapshot.From).ToList();

            return new CharacterSnapshot(
                id, e.Race, e.BaseClass, e.Level, e.Exp, e.Gold,
                e.SecondClass, e.ThirdClass, e.SkillPoints, (int)e.Profession,
                e.Con, e.AtkStat, e.Wit, e.Agi, e.Spt, e.X, e.Y,
                string.Join(',', e.LearnedSkills.Select(kv => $"{kv.Key}:{kv.Value}")),
                string.Join(',', e.CompletedQuests),
                JsonSerializer.Serialize(e.ActiveQuests.Values.ToList()),
                string.Join(',', e.KnownRecipes),
                string.Join(',', e.Friends),
                string.Join(',', e.Blocked),
                JsonSerializer.Serialize(new AutoHuntConfigDto(
                    e.AutoHuntEnabled, e.AutoHpPotionPct, e.AutoMpPotionPct, e.AutoBuffPotions,
                    e.AutoSkills.ToArray(), e.AutoBuffPotionIds.ToArray(),
                    e.AutoFarmRange, e.AutoFarmStatic, e.AutoAttackNormal, e.AutoAttackElite, e.AutoAttackBoss,
                    e.AutoHealPotions.ToArray(), e.AutoCyclic, e.AutoHealPct, e.AutoAssistLeader,
                    e.AutoBuffs.ToArray())),
                JsonSerializer.Serialize(e.EquipPresets),
                JsonSerializer.Serialize(BuffSnapshot.CaptureAll(e)),
                e.ActiveSubclass.Slot, subs,
                e.Karma, e.PkCount, e.PvpCount, e.ConsecutivePk, e.DiedWhileAway,
                e.JailedUntil, e.ChatBannedUntil, e.TotalOnlineSeconds,
                e.Charisma, e.CharismaLifetime, e.LikesRemainingToday, e.LikeBudgetDay,
                e.TitleCategory, e.CustomTitle, e.CustomTitleColor, e.MayWriteTitle,
                (int)e.Social,
                items);
        }
    }

    public sealed record ItemSnapshot(
        Guid InstanceId, string DefId, bool Equipped, int Enchant, int Quantity,
        List<ItemAttribute> Attributes, DateTime? ExpiresAtUtc = null, bool InWarehouse = false,
        long? SellPriceOverride = null, bool? TradableOverride = null, string? CustomName = null,
        bool? CanStorePrivate = null, bool? CanStoreAccount = null);

    /// <summary>One saved buff. Deliberately MINIMAL — the skill id plus the level it was cast at is
    /// enough to rebuild everything else (effect flags, magnitudes, DoT power, shield size) through the
    /// normal ApplyBuff path, which also means a buff restored after a catalog change comes back with
    /// the CURRENT definition rather than a stale snapshot of the old one.
    ///
    /// <paramref name="ExpiresAtUtc"/> is wall-clock (null = a toggle, which has no duration), so an
    /// hour spent logged out costs an hour of a one-hour buff. DisplayName is kept because per-class
    /// flavour names (Holy/Moonlight/Spirit Bolt) are an argument to ApplyBuff, not a property of the
    /// def.</summary>
    /// <param name="SkillId">The skill that CREATED the buff — for a child of an improved (group)
    /// buff that is the child, never the parent: re-applying the parent on login would restore every
    /// sibling at full duration, so a relog would refresh the whole blessing for free.</param>
    /// <param name="SourceSkillId">The id the buff bar groups/icons by (the PARENT for a group
    /// child). "" = same as SkillId, which is also what an older save deserializes to.</param>
    public sealed record BuffSnapshot(
        string SkillId, int Level, DateTime? ExpiresAtUtc, int Stacks, int ShieldPool,
        string DisplayName, string SourceSkillId = "")
    {
        /// <summary>The buffs on an entity that are worth saving.
        ///
        /// Excluded, each for its own reason:
        /// • DEBUFFS — a DoT needs a live applier for damage attribution and kill credit, and its
        ///   SourceId cannot survive a restart. (Relog therefore still clears debuffs; if that becomes
        ///   an exploit, it needs the attribution problem solved first, not a bigger snapshot.)
        /// • INTERNAL stack counters — mechanic state belonging to whoever applied the DoT.
        /// • Buffs with no SourceSkillId — the synthetic grade-penalty rows, which are recomputed.
        /// • RUNE buffs — ReconcileRuneBuffs re-derives these from the held rune items on login, using
        ///   the item's own expiry. Saving them too would apply the same buff twice.</summary>
        public static List<BuffSnapshot> CaptureAll(Entity e)
        {
            var now = DateTime.UtcNow;
            var list = new List<BuffSnapshot>();
            foreach (var b in e.Buffs)
            {
                string skillId = string.IsNullOrEmpty(b.SkillId) ? b.SourceSkillId : b.SkillId;
                if (b.IsDebuff || b.Internal || string.IsNullOrEmpty(skillId)) continue;
                if (SkillCatalog.IsRuneBuff(skillId)) continue;

                DateTime? expires = b.Toggle
                    ? null
                    : now.AddSeconds(b.TicksRemaining * GameConstants.TickSeconds);
                list.Add(new BuffSnapshot(skillId, b.Level, expires, b.Stacks, b.ShieldPool,
                                          b.Name, b.SourceSkillId));
            }
            return list;
        }
    }

    /// <summary>Persist one snapshot back to its character row (logout / event save).
    /// Replaces the item set wholesale — simplest correct approach for now.</summary>
    public async Task SaveCharacterAsync(CharacterSnapshot snap)
    {
        await _saveGate.WaitAsync();          // see _saveGate: overlapping saves lose data
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            var rec = await db.Characters
                .Include(c => c.Items).ThenInclude(i => i.Attributes)
                .Include(c => c.Subclasses)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == snap.CharacterId);
            if (rec is null)
                return;

            ApplySnapshot(db, rec, snap);
            await db.SaveChangesAsync();
        }
        finally { _saveGate.Release(); }
    }

    /// <summary>Batched periodic save: one DbContext, one SaveChanges for the whole
    /// set (avoids a thundering herd of concurrent connections / SQLite write locks).</summary>
    public async Task SaveCharactersAsync(IReadOnlyList<CharacterSnapshot> snaps)
    {
        if (snaps.Count == 0)
            return;

        await _saveGate.WaitAsync();          // see _saveGate: overlapping saves lose data
        try
        {
            await using var db = await _factory.CreateDbContextAsync();
            foreach (var snap in snaps)
            {
                var rec = await db.Characters
                    .Include(c => c.Items).ThenInclude(i => i.Attributes)
                    .Include(c => c.Subclasses)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(c => c.Id == snap.CharacterId);
                if (rec is not null)
                    ApplySnapshot(db, rec, snap);
            }
            await db.SaveChangesAsync();
        }
        finally { _saveGate.Release(); }
    }

    /// <summary>Copy a snapshot onto a tracked record (no SaveChanges — caller batches).</summary>
    private static void ApplySnapshot(GameDbContext db, CharacterRecord rec, CharacterSnapshot snap)
    {
        // ---- CHARACTER-level: shared by every class this character owns.
        rec.Race = snap.Race;               // can change via DEBUG character reset
        rec.Gold = snap.Gold;
        rec.Profession = snap.Profession;
        rec.CompletedQuestsCsv = snap.CompletedQuestsCsv;
        rec.ActiveQuestsJson = snap.ActiveQuestsJson;
        rec.KnownRecipesCsv = snap.KnownRecipesCsv;
        rec.FriendsCsv = snap.FriendsCsv;
        rec.BlockedCsv = snap.BlockedCsv;
        rec.SocialOptions = snap.SocialOptions;
        rec.Charisma = snap.Charisma;
        rec.CharismaLifetime = snap.CharismaLifetime;
        rec.LikesRemainingToday = snap.LikesRemainingToday;
        rec.LikeBudgetDay = snap.LikeBudgetDay;
        rec.TitleCategory = snap.TitleCategory;
        rec.CustomTitle = snap.CustomTitle;
        rec.CustomTitleColor = snap.CustomTitleColor;
        rec.MayWriteTitle = snap.MayWriteTitle;
        rec.AutoHuntJson = snap.AutoHuntJson;
        rec.EquipPresetsJson = snap.EquipPresetsJson;
        rec.BuffsJson = snap.BuffsJson;
        rec.Karma = snap.Karma;
        rec.PkCount = snap.PkCount;
        rec.PvpCount = snap.PvpCount;
        rec.ConsecutivePk = snap.ConsecutivePk;
        rec.TotalOnlineSeconds = snap.TotalOnlineSeconds;
        rec.DiedWhileAway = snap.DiedWhileAway;
        rec.JailedUntilUtc = snap.JailedUntilUtc;   // jail persists across a relog
        rec.ChatBannedUntilUtc = snap.ChatBannedUntilUtc;
        // NOTE: Role is deliberately NOT written back from the snapshot. It is changed only by /role
        // (a direct DB write), so a stale in-memory copy can never demote or promote anyone on autosave.
        rec.X = snap.X;
        rec.Y = snap.Y;

        // ---- MIRROR of the ACTIVE class. NOT the source of truth — it exists so the
        // character-SELECT screen can list a character without loading its subclasses.
        rec.BaseClass = snap.BaseClass;
        rec.Level = snap.Level;
        rec.Exp = snap.Exp;
        rec.SecondClass = snap.SecondClass;
        rec.ThirdClass = snap.ThirdClass;
        rec.SkillPoints = snap.SkillPoints;
        rec.LearnedSkillsCsv = snap.LearnedSkillsCsv;
        rec.Con = snap.Con;
        rec.Atk = snap.Atk;
        rec.Wit = snap.Wit;
        rec.Agi = snap.Agi;
        rec.Spt = snap.Spt;

        // ---- CLASS-level: the real per-class state. Rebuilt wholesale, like the items.
        rec.ActiveSubclassSlot = snap.ActiveSubclassSlot;
        db.Subclasses.RemoveRange(rec.Subclasses);
        rec.Subclasses = snap.Subclasses.Select(s => new SubclassRecord
        {
            Slot = s.Slot,
            Race = s.Race,
            BaseClass = s.BaseClass,
            SecondClass = s.SecondClass,
            ThirdClass = s.ThirdClass,
            Level = s.Level,
            Exp = s.Exp,
            SkillPoints = s.SkillPoints,
            Con = s.Con, Atk = s.Atk, Wit = s.Wit, Agi = s.Agi, Spt = s.Spt,
            LearnedSkillsCsv = s.LearnedSkillsCsv,
            SkillBarJson = s.SkillBarJson,
        }).ToList();

        // Rebuild the item set from the snapshot.
        db.Items.RemoveRange(rec.Items);
        rec.Items = snap.Items.Select(i => new ItemRecord
        {
            InstanceId = i.InstanceId,
            DefId = i.DefId,
            Equipped = i.Equipped,
            Enchant = i.Enchant,
            Quantity = i.Quantity,
            Attributes = i.Attributes.ToList(),
            ExpiresAtUtc = i.ExpiresAtUtc,
            InWarehouse = i.InWarehouse,
            SellPriceOverride = i.SellPriceOverride,
            TradableOverride = i.TradableOverride,
            CustomName = i.CustomName,
            CanStorePrivate = i.CanStorePrivate,
            CanStoreAccount = i.CanStoreAccount,
        }).ToList();
    }

    // ----- Account warehouse -------------------------------------------------

    /// <summary>Load the ACCOUNT-wide bank. Called once per account, on the first login of any of its
    /// characters; the loop keeps the live list from then on (two characters of one account can be in
    /// the world at once — offline farming makes that ordinary — and they must share one list, not two
    /// copies that diverge).</summary>
    public async Task<List<InventoryItem>> LoadAccountWarehouseAsync(int accountId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.AccountItems.Where(i => i.AccountId == accountId).ToListAsync();

        var items = new List<InventoryItem>(rows.Count);
        foreach (var r in rows)
        {
            // Same rule as the bag: an item whose DefId left the catalog (a rename) is dropped rather
            // than resurrected as an unusable row.
            if (ItemCatalog.Get(r.DefId) is null) continue;
            items.Add(new InventoryItem
            {
                DefId = r.DefId,
                Enchant = r.Enchant,
                Quantity = r.Quantity,
                Attributes = r.Attributes.ToList(),
                ExpiresAtUtc = r.ExpiresAtUtc,
                PersistentInstanceId = r.InstanceId,
            });
        }
        return items;
    }

    /// <summary>Rewrite an account's bank wholesale, exactly like the character item set. Snapshot on
    /// the tick thread (<see cref="AccountItemSnapshot.From"/>), write off it.</summary>
    public async Task SaveAccountWarehouseAsync(int accountId, IReadOnlyList<AccountItemSnapshot> items)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var old = await db.AccountItems.Where(i => i.AccountId == accountId).ToListAsync();
        db.AccountItems.RemoveRange(old);
        foreach (var i in items)
            db.AccountItems.Add(new AccountItemRecord
            {
                AccountId = accountId,
                InstanceId = i.InstanceId,
                DefId = i.DefId,
                Enchant = i.Enchant,
                Quantity = i.Quantity,
                Attributes = i.Attributes.ToList(),
                ExpiresAtUtc = i.ExpiresAtUtc,
            });
        await db.SaveChangesAsync();
    }

    public sealed record AccountItemSnapshot(
        Guid InstanceId, string DefId, int Enchant, int Quantity,
        List<ItemAttribute> Attributes, DateTime? ExpiresAtUtc)
    {
        /// <summary>Capture a bank. MUST be called on the tick thread.</summary>
        public static List<AccountItemSnapshot> From(IEnumerable<InventoryItem> items) =>
            items.Select(i => new AccountItemSnapshot(
                i.PersistentInstanceId ?? Guid.NewGuid(), i.DefId, i.Enchant, i.Quantity,
                new List<ItemAttribute>(i.Attributes), i.ExpiresAtUtc)).ToList();
    }

    // ----- Account farm budget -----------------------------------------------

    /// <summary>Load an account's daily farm allowance. Same lifetime rule as the account bank: read
    /// once, on the first login of any of the account's characters, then kept live by the loop (two
    /// characters of one account can be spending it at the same time and must share ONE balance).
    /// Returns null only if the account row has vanished.</summary>
    public async Task<AccountFarmBudget?> LoadAccountBudgetAsync(int accountId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId);
        if (a is null) return null;
        return new AccountFarmBudget
        {
            AccountId         = accountId,
            AutoTicksLeft     = a.AutoTicksLeft,
            OfflineTicksLeft  = a.OfflineTicksLeft,
            LastResetDate     = a.LastFarmResetDate,
            AutoCapSeconds    = a.AutoCapSeconds,
            OfflineCapSeconds = a.OfflineCapSeconds,
        };
    }

    /// <summary>Write a budget back. Takes the values rather than the live object: the caller
    /// snapshots on the tick thread (single-writer rule) and this runs off it.</summary>
    public async Task SaveAccountBudgetAsync(
        int accountId, long autoTicks, long offlineTicks, DateOnly lastReset,
        int autoCapSeconds, int offlineCapSeconds)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId);
        if (a is null) return;
        a.AutoTicksLeft     = autoTicks;
        a.OfflineTicksLeft  = offlineTicks;
        a.LastFarmResetDate = lastReset;
        a.AutoCapSeconds    = autoCapSeconds;
        a.OfflineCapSeconds = offlineCapSeconds;
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

    // ----- Admin moderation (jail / kick / ban) — all target by CHARACTER name so they work even when
    //        the target is offline. Return false only if the name isn't found. -----------------------
    //
    // Every lookup here matches case-INSENSITIVELY. SQLite compares TEXT with `=` case-sensitively, so
    // `/jail test1` used to miss the row for "Test1" and report "No character 'test1'" — while the ONLINE
    // lookup (OrdinalIgnoreCase) found them and jailed them anyway. Action succeeded, message lied.

    /// <summary>Ban the ACCOUNT that owns this character until <paramref name="until"/> (null = lift).</summary>
    public async Task<bool> BanAccountByCharacterNameAsync(string characterName, DateTime? until)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        if (character is null) return false;
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == character.AccountId);
        if (account is null) return false;
        account.BannedUntilUtc = until;
        account.IsBanned = false;   // the timed ban supersedes the legacy permanent flag
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>The canonical (case-preserved) name of a character, or null if no such character —
    /// used to validate a /fadd target that may be offline.</summary>
    public async Task<string?> ResolveCharacterNameAsync(string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        return rec?.Name;
    }

    /// <summary>The friend list of a (possibly OFFLINE) character. Needed because friendship is mutual:
    /// to know whether someone is a real friend or still just a pending request, you have to read THEIR
    /// list, and they may not be logged in.</summary>
    public async Task<HashSet<string>> GetFriendsAsync(string characterName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var csv = await db.Characters
            .Where(c => c.Name.ToLower() == lower)
            .Select(c => c.FriendsCsv)
            .FirstOrDefaultAsync();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(csv))
            foreach (var f in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                set.Add(f);
        return set;
    }

    /// <summary>The kick deadline for a character (if any) — checked at EnterWorld so a kicked character
    /// can't come back until it passes, while the account plays its other characters freely.</summary>
    public async Task<DateTime?> GetKickUntilAsync(int accountId, int characterId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rec = await db.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.AccountId == accountId);
        return rec?.KickedUntilUtc;
    }

    /// <summary>JAIL a character until <paramref name="until"/> (null = release).</summary>
    public async Task<bool> SetJailAsync(string characterName, DateTime? until)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        if (character is null) return false;
        character.JailedUntilUtc = until;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>KICK a character out of the world and lock it out until <paramref name="until"/>.</summary>
    public async Task<bool> SetKickAsync(string characterName, DateTime? until)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        if (character is null) return false;
        character.KickedUntilUtc = until;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Apply a charisma delta to a possibly-OFFLINE character by name (pool clamped [0,cap],
    /// lifetime floored at 0). For liking a logged-off player. Returns their new lifetime, or null if no
    /// such character. If they are ONLINE, the caller must update the live Entity instead (this write
    /// would be overwritten by their next autosave).</summary>
    public async Task<long?> AddCharismaAsync(string characterName, int poolDelta, long lifetimeDelta)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var c = await db.Characters.FirstOrDefaultAsync(ch => ch.Name.ToLower() == lower);
        if (c is null) return null;
        c.Charisma = Math.Clamp(c.Charisma + poolDelta, 0, GameConstants.CharismaPoolCap);
        c.CharismaLifetime = Math.Max(0, c.CharismaLifetime + lifetimeDelta);
        await db.SaveChangesAsync();
        return c.CharismaLifetime;
    }

    /// <summary>Wipe BOTH charisma values of a possibly-offline character (a ban zeroes reputation).</summary>
    public async Task<bool> ZeroCharismaAsync(string characterName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var c = await db.Characters.FirstOrDefaultAsync(ch => ch.Name.ToLower() == lower);
        if (c is null) return false;
        c.Charisma = 0;
        c.CharismaLifetime = 0;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>CHAT-BAN a character until <paramref name="until"/> (null = lift).</summary>
    public async Task<bool> SetChatBanAsync(string characterName, DateTime? until)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        if (character is null) return false;
        character.ChatBannedUntilUtc = until;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Set a CHARACTER's staff role (owner: roles are per-character). Works offline. Returns the
    /// canonical name on success so the caller can echo it, or null if there's no such character.</summary>
    public async Task<string?> SetRoleAsync(string characterName, AccountRole role)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        if (character is null) return null;
        character.Role = role;
        await db.SaveChangesAsync();
        return character.Name;
    }

    /// <summary>The role of a character, for authorizing an action against an OFFLINE target (so a
    /// moderator can't jail an admin who happens to be logged out).</summary>
    public async Task<AccountRole?> GetRoleAsync(string characterName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var lower = characterName.ToLower();
        var character = await db.Characters.FirstOrDefaultAsync(c => c.Name.ToLower() == lower);
        return character?.Role;
    }

    /// <summary>The characters currently jailed (name + release time), for the admin's un-jail list.</summary>
    public async Task<List<JailedInfo>> ListJailedAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        return await db.Characters
            .Where(c => c.JailedUntilUtc != null && c.JailedUntilUtc > now)
            .Select(c => new JailedInfo(c.Name, c.JailedUntilUtc!.Value))
            .ToListAsync();
    }

    public record JailedInfo(string Name, DateTime UntilUtc);
}
