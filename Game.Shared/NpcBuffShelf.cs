using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Game.Shared;

/// <summary>
/// `BL-163` — THE SPIRIT HELPER'S SHELF, READ FROM A FILE.
///
/// <para>Owner, 2026-09-04: *"that's why I wanted the npc buffer to be like the /buff command not
/// like a wrapper or check player lvl and put him in a range table with available buffs ... and that
/// table can be a file with min lvl,skill_id_rung,price (editable from outside - so a pvp server
/// won't require new npc just change of id's) .. but whatever is working"*.</para>
///
/// <para>🔑 THE TWO THINGS THAT MAKE IT A SERVER-OPERATOR FEATURE rather than a developer one, and
/// both are his:
/// <list type="number">
/// <item><b>The row names the RUNG, not a wrapper.</b> The shelf points at <c>buff_def_mag_3</c> and
/// the NPC grants it exactly the way <c>/buff</c> does. There is no per-blessing <c>Levels</c> array
/// to keep in step with a second table, and no "tier index == SkillLevel index" invariant to guard —
/// the whole `BL-158` startup assertion simply stops existing.</item>
/// <item><b>It is outside C#.</b> A PvP server retunes its buffer by editing ids and prices and
/// restarting. No rebuild, no new NPC, no code change.</item>
/// </list></para>
///
/// <para>🔑 WHY THERE IS STILL A SHELF ID AND NOT JUST A RUNG ID. Two things key off the blessing's
/// identity rather than the rung that happens to be current:
/// <list type="bullet">
/// <item><b>[Save] and the role presets store what you PRESSED</b> (<c>BuffInstance.SourceSkillId</c>),
/// which is why every grant here passes the shelf id as the source. A preset holding RUNG ids would
/// freeze the player at the rung they saved — save Ward at 44 and you would still be buying +23% at
/// 70. Naming the blessing and re-resolving the rung on expansion is also what makes his `BL-150`
/// rule work: *"if some1 buff me with body or soul and i save it and im &lt;40lvl they will not
/// activate .. they will activate after 40+"*.</item>
/// <item><b>Saved presets already in the database hold the <c>npc_*</c> ids</b>, so the shelf ids are
/// exactly the old wrapper ids and are append-only. Renaming one empties every preset that named it,
/// which is a save migration nobody is owed.</item>
/// </list></para>
///
/// <para>⚠ IT LOADS LAZILY AND ONLY WHERE THERE IS A FILE. The server and `tools/BalanceMatrix` read
/// it; the Unity client links the same assembly and must never touch it (there is no repo on a
/// phone), which is why nothing in <c>SkillCatalog</c>'s own construction or validation reads this
/// type. Keep it that way.</para>
/// </summary>
public static class NpcBuffShelf
{
    /// <summary>One rung the NPC sells: the level it unlocks at, the buff that actually lands, which
    /// level of that buff, and the price. Position in a blessing's array is its 1-based TIER.</summary>
    public readonly record struct ShelfRung(int MinLevel, string RungId, int RungLevel, long Price);

    /// <summary>The file's name. It lives beside his other authored data in <c>docs/data/</c>, which is
    /// the copy a dev box and his own box read — edit it and restart and the change is in, with no
    /// build, which is the whole ask. A PUBLISHED server has no repo, so the build also drops a copy
    /// at <c>data/</c> beside the exe and that one is the fallback.</summary>
    public const string FileName = "npc_buff_shelf.csv";

    private static readonly object _gate = new();
    private static Dictionary<string, ShelfRung[]>? _shelf;
    private static string[]? _order;
    private static string _loadedFrom = "";

    /// <summary>Where the loaded file was actually read from. Printed at boot ON PURPOSE: this is the
    /// same trap <c>game.db</c> has, where a second stale copy sits in <c>bin/</c> and a person spends
    /// an hour editing the one the server is not reading.</summary>
    public static string LoadedFrom { get { Ensure(); return _loadedFrom; } }

    /// <summary>Every blessing on the shelf, in the FILE's row order — which is the order of the
    /// buffer window and therefore of the buff bar.</summary>
    public static IReadOnlyList<string> Order { get { Ensure(); return _order!; } }

    /// <summary>shelfId → its ladder, lowest rung first.</summary>
    public static IReadOnlyDictionary<string, ShelfRung[]> Shelf { get { Ensure(); return _shelf!; } }

    /// <summary>Force a (re)load from an explicit path — the server calls this at startup with the
    /// path resolved against its CONTENT ROOT, so the answer never depends on the working directory.
    /// Throws if the file is missing or any row is bad; a shelf that half-loaded would sell nothing
    /// and say nothing about why.</summary>
    public static void Load(string path)
    {
        lock (_gate)
        {
            var (shelf, order) = Parse(File.ReadAllLines(path), path);
            _shelf = shelf;
            _order = order;
            _loadedFrom = Path.GetFullPath(path);
        }
    }

    private static void Ensure()
    {
        if (_shelf is not null) return;
        lock (_gate)
        {
            if (_shelf is not null) return;
            string path = Find()
                ?? throw new FileNotFoundException(
                    $"The NPC buffer's shelf file '{FileName}' was not found. It is looked for in "
                    + $"docs/data/ walking up from '{AppContext.BaseDirectory}', then in data/ beside "
                    + "the executable. Without it the buffer has nothing to sell (`BL-163`).");
            var (shelf, order) = Parse(File.ReadAllLines(path), path);
            _shelf = shelf;
            _order = order;
            _loadedFrom = Path.GetFullPath(path);
        }
    }

    /// <summary>Where the file is. The REPO copy wins, so that editing <c>docs/data/</c> and
    /// restarting is enough; the copy beside the exe is for a published server with no repo behind
    /// it. ⚠ Deliberately NOT the other way round: a build-copied file that shadows the authored one
    /// is how an edit appears to do nothing.</summary>
    public static string? Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int up = 0; dir is not null && up < 8; up++, dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "docs", "data", FileName);
            if (File.Exists(candidate)) return candidate;
        }
        string beside = Path.Combine(AppContext.BaseDirectory, "data", FileName);
        return File.Exists(beside) ? beside : null;
    }

    // ---- The rest is the read-side API the game asks its four questions through. ------------------

    /// <summary>The 1-based TIER this character qualifies for, or 0 if the blessing is out of reach.
    /// The highest rung at or below their level wins.</summary>
    public static int TierFor(string shelfId, int playerLevel)
    {
        if (!Shelf.TryGetValue(shelfId, out var rungs)) return 0;
        int found = 0;
        for (int i = 0; i < rungs.Length; i++)
            if (playerLevel >= rungs[i].MinLevel) found = i + 1;
        return found;
    }

    /// <summary>The level a blessing first becomes buyable at. 6 for anything not on the shelf, which
    /// is the safe answer: an unlisted id is refused by the shelf membership test long before this.</summary>
    public static int MinLevel(string shelfId) =>
        Shelf.TryGetValue(shelfId, out var rungs) && rungs.Length > 0 ? rungs[0].MinLevel : 6;

    /// <summary>What this character pays. 0 for a free blessing — and 0 for one they cannot buy yet,
    /// which never reaches a charge because the level gate refuses first. Quoting 0 for something
    /// unbuyable beats quoting a price the player would then be refused at.</summary>
    public static long Price(string shelfId, int playerLevel)
    {
        int tier = TierFor(shelfId, playerLevel);
        return tier == 0 ? 0 : Shelf[shelfId][tier - 1].Price;
    }

    /// <summary>The rung this character buys, or null if the blessing is out of reach.</summary>
    public static ShelfRung? RungFor(string shelfId, int playerLevel)
    {
        int tier = TierFor(shelfId, playerLevel);
        return tier == 0 ? null : Shelf[shelfId][tier - 1];
    }

    /// <summary>The rung this character buys, resolved to a real def + the level to apply it at.
    /// This is what the grant and the "would it land" question both run on — they must never resolve
    /// it separately, or a refusal and an outcome can disagree.</summary>
    public static (SkillDef Def, int Level)? RungDefFor(string shelfId, int playerLevel)
    {
        if (RungFor(shelfId, playerLevel) is not ShelfRung rung) return null;
        return SkillCatalog.Get(rung.RungId) is SkillDef def ? (def, rung.RungLevel) : null;
    }

    /// <summary>What the window calls this blessing: the name of its rung, because the rung is the
    /// buff the player ends up wearing and the two must read as the same thing. Every family rung
    /// already carries the blessing's name ("Ward", "Aim", "Fury"), so nothing is authored twice —
    /// and a NEW row in the file needs no label anywhere in code.
    /// <para>The TOP rung is asked, not the player's: the shelf's own label must not change as you
    /// level, and the names are per-family rather than per-rung anyway.</para></summary>
    public static string DisplayName(string shelfId)
    {
        if (!Shelf.TryGetValue(shelfId, out var rungs) || rungs.Length == 0) return shelfId;
        return SkillCatalog.Get(rungs[^1].RungId)?.Name ?? shelfId;
    }

    /// <summary>Is this id on the shelf at all? The server's guard against a hand-made packet naming
    /// a buff the NPC does not sell.</summary>
    public static bool Sells(string shelfId) => Shelf.ContainsKey(shelfId);

    // ---- Parsing + validation --------------------------------------------------------------------

    /// <summary>Read the rows and REFUSE THE WHOLE FILE on any defect. A typo in an operator-edited
    /// file is far likelier than a typo in C#, and the failure mode of tolerating one is a blessing
    /// that is silently unbuyable or one that quietly sells the wrong rung — both invisible until a
    /// player complains. Startup is the only place this can be caught, so it throws.</summary>
    private static (Dictionary<string, ShelfRung[]>, string[]) Parse(string[] lines, string path)
    {
        var rows = new List<(string Shelf, ShelfRung Rung, int LineNo)>();
        var problems = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var cells = line.Split(',');
            // The header, recognised by its first cell rather than by being line 1 — the file is
            // hand-edited and a stray blank or comment above it costs nothing this way.
            if (cells[0].Trim().Equals("SHELF_ID", StringComparison.OrdinalIgnoreCase)) continue;
            if (cells.Length < 5)
            {
                problems.Add($"line {i + 1}: {cells.Length} column(s), expected 5 "
                           + "(SHELF_ID,MIN_LEVEL,RUNG_SKILL_ID,RUNG_LEVEL,PRICE)");
                continue;
            }
            string shelfId = cells[0].Trim();
            string rungId = cells[2].Trim();
            if (shelfId.Length == 0 || rungId.Length == 0)
            {
                problems.Add($"line {i + 1}: SHELF_ID and RUNG_SKILL_ID cannot be blank");
                continue;
            }
            if (!int.TryParse(cells[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int minLevel)
                || minLevel < 1)
            { problems.Add($"line {i + 1}: MIN_LEVEL '{cells[1].Trim()}' is not a level"); continue; }
            if (!int.TryParse(cells[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rungLevel)
                || rungLevel < 1)
            { problems.Add($"line {i + 1}: RUNG_LEVEL '{cells[3].Trim()}' is not a level"); continue; }
            if (!long.TryParse(cells[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long price)
                || price < 0)
            { problems.Add($"line {i + 1}: PRICE '{cells[4].Trim()}' is not a price"); continue; }

            // ⚠ A NON-ASCII ID IS THE ONE DEFECT THAT LOOKS PERFECT. `BL-237` cost a build to a
            //   Cyrillic 'к' sitting invisibly at the end of a skill id, and this file is edited by
            //   the same keyboard. Caught here rather than at the "unknown skill" message below,
            //   because that message would name an id that reads as correct.
            foreach (var (label, value) in new[] { ("SHELF_ID", shelfId), ("RUNG_SKILL_ID", rungId) })
                if (value.Any(c => c > 127))
                    problems.Add($"line {i + 1}: {label} '{value}' has a non-ASCII character in it "
                               + "(a Cyrillic letter that looks Latin is the usual cause)");

            if (SkillCatalog.Get(rungId) is not SkillDef rungDef)
                problems.Add($"line {i + 1}: RUNG_SKILL_ID '{rungId}' is not a skill");
            else if (rungLevel > rungDef.MaxLevel)
                problems.Add($"line {i + 1}: '{rungId}' has {rungDef.MaxLevel} level(s); "
                           + $"RUNG_LEVEL {rungLevel} does not exist");

            rows.Add((shelfId, new ShelfRung(minLevel, rungId, rungLevel, price), i + 1));
        }

        // Group into ladders, keeping FIRST-APPEARANCE order: the file's order is the window's order.
        var order = new List<string>();
        var byShelf = new Dictionary<string, List<(ShelfRung Rung, int LineNo)>>(StringComparer.Ordinal);
        foreach (var (shelfId, rung, lineNo) in rows)
        {
            if (!byShelf.TryGetValue(shelfId, out var list))
            {
                byShelf[shelfId] = list = new List<(ShelfRung, int)>();
                order.Add(shelfId);
            }
            list.Add((rung, lineNo));
        }

        // ---- EVERY LADDER MUST CLIMB. The same two guards `BL-158` added in C#, moved here where the
        //      typo now lives. A ladder that goes backwards is the one defect with no symptom: the
        //      shelf still sells something, just the wrong thing, at the wrong price.
        foreach (var shelfId in order)
        {
            var list = byShelf[shelfId];
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i].Rung.MinLevel <= list[i - 1].Rung.MinLevel)
                    problems.Add($"line {list[i].LineNo}: '{shelfId}' rung {i + 1} unlocks at "
                               + $"{list[i].Rung.MinLevel}, which is not above rung {i}'s "
                               + $"{list[i - 1].Rung.MinLevel} (rows of one blessing must climb)");
                if (list[i].Rung.Price < list[i - 1].Rung.Price)
                    problems.Add($"line {list[i].LineNo}: '{shelfId}' rung {i + 1} costs "
                               + $"{list[i].Rung.Price:N0}, less than rung {i}'s "
                               + $"{list[i - 1].Rung.Price:N0} — a ladder's price never falls");
            }
        }

        var shelf = byShelf.ToDictionary(kv => kv.Key, kv => kv.Value.Select(r => r.Rung).ToArray(),
                                         StringComparer.Ordinal);

        // ---- EVERY SHELF_ID MUST BE A BLESSING THIS ASSEMBLY KNOWS. Without this a typo — `npc_wardd`
        //      — is not an error at all: it becomes a THIRTY-FIRST blessing, with the rung's name on
        //      its button, sitting beside the real Ward. The catalogue is the universe; the file
        //      chooses from it, re-prices it and re-points it, and that is the whole freedom it has.
        foreach (var shelfId in order)
            if (Array.IndexOf(SkillCatalog.NpcShelfCatalogue, shelfId) < 0)
                problems.Add($"SHELF_ID '{shelfId}' is not one of the game's blessings "
                           + "(SkillCatalog.NpcShelfCatalogue lists them) — check the spelling");

        // ---- THE PRESETS STILL HAVE TO BE BUYABLE. His two role loadouts and the free eight name
        //      shelf ids from C#; an operator who deletes a row out of the file would otherwise leave
        //      a [Mage] button quoting a blessing the NPC no longer sells.
        foreach (var (label, set) in new (string, IReadOnlyList<string>)[]
                 {
                     ("the free eight", SkillCatalog.FreeNpcBuffSet),
                     ("the Mage preset", SkillCatalog.MageBuffSet),
                     ("the Fighter preset", SkillCatalog.FighterBuffSet),
                 })
            foreach (var id in set)
                if (!shelf.ContainsKey(id))
                    problems.Add($"'{id}' is in {label} but has no row in the file");

        // ⚠ And the free eight must really be free, or the price rule ("0 = free, there is no other
        //   free/paid rule") is quietly untrue for the one set he named by hand.
        foreach (var id in SkillCatalog.FreeNpcBuffSet)
            if (shelf.TryGetValue(id, out var rungs) && rungs.Any(r => r.Price != 0))
                problems.Add($"'{id}' is one of the free eight but is priced in the file");

        if (problems.Count > 0)
            throw new InvalidOperationException(
                $"The NPC buffer's shelf file is not usable ({path}):{Environment.NewLine}  "
                + string.Join(Environment.NewLine + "  ", problems));

        return (shelf, order.ToArray());
    }
}
