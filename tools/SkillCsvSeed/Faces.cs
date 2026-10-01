using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Game.Shared;

// =====================================================================================================
//  `BL-327` — SKILL FACES: how a skill LOOKS → Game.Shared/SkillFaces.g.cs
//
//  The owner, 2026-09-30: *"the class csv is the numbers per lvl while the face is the display"* — and the
//  same day, after two files proved confusing, *"build it that way"*: THE CLASS CSV OWNS THE PLAIN LOOK.
//
//  THREE SOURCES, ONE LIST OF FACES:
//    classes_skills_csv/*.csv   NAME = the skill's name; DESCRIPTION (the LAST column) = what the player
//                               reads, written on ONE row of the skill (blank elsewhere; two that differ is
//                               a check error). DESCR stays the numbers per level, read by --check.
//    skill_faces.csv            EXCEPTIONS ONLY — a row with a RACE or a CLASS: Forest Strength (elf), the
//                               harmonist's Bow Expertise, "NPC Blood Mark" (CLASS = NPC, the spirit
//                               helper's label). Blank DESCRIPTION = the plain one from the class CSV.
//    skill_faces_other.csv      SKILL_ID,NAME,DESCRIPTION,COMMENT for a skill NO class CSV lists — mobs,
//                               NPC blessings, items, internal pieces.
//
//  ONE NAME PER SKILL: when a skill's rows disagree on NAME (Momentum reads "Battle"/"Bow"/"Stab"/"Arcane"
//  by file), the names an exception row explains — or the code's own per-level names (Grade F…S) — are
//  set aside, and exactly ONE must be left. That one is the plain name; two left over is a check error,
//  which is how a typo in one file ("Wirlwind") gets caught.
//
//  THE PLACEHOLDERS (his `@`, widened so a multi-number description can say WHICH number):
//    @            the skill's POWER (Holy Bolt's "+@ power")
//    @{m.def}     a named number — any word from DESCR-KEYS.md, or the metric key itself
//    @{p.def%}    force the percent reading; @{p.def#} the flat one (only needed when a skill has both)
//    @{mpcost.2}  the 2nd number of that word (Mana Blessing's magic MP cost; plain @{mpcost} = the 1st)
//    @{duration}  the buff's duration ("20 min")
//    [ … ]        shown only when every placeholder inside has a value at that level
//  An unbracketed placeholder with no value at a level drops the CLAUSE around it (the text between
//  commas/semicolons) — his *"use the maximum … the lower lvls will take from there"*.
//  An EMPTY description = the game's own per-level text.
//
//  Numbers render WITHOUT a sign (he writes the `+`) and percents carry their `%`.
//
//  --gen-faces     renders every face for every level and writes SkillFaces.g.cs
//  --check         calls Faces.Check: bad ids/races/classes/keys, disagreeing names, a skill with no name,
//                  a row in the wrong file, a stale .g.cs
//  --faces-to-csv  ONE-OFF (done 2026-09-30): moved the plain names/descriptions out of the old face files
// =====================================================================================================

internal static class Faces
{
    internal sealed record Row(string Id, string Name, string Race, string Class, string Descr, string Comment, int Line,
                               string File);

    private const string FacesName = "skill_faces.csv", OtherName = "skill_faces_other.csv";
    private const string FacesHeader = "SKILL_ID,NAME,RACE,CLASS,DESCRIPTION,COMMENT";
    private const string OtherHeader = "SKILL_ID,NAME,DESCRIPTION,COMMENT";
    private const string DescrHeader = "DESCRIPTION";

    private static string DataPath(string repoRoot, string name) => Path.Combine(repoRoot, "docs", "data", name);
    private static string ClassDir(string repoRoot) => Path.Combine(repoRoot, "docs", "data", "classes_skills_csv");
    private static string GenPath(string repoRoot) => Path.Combine(repoRoot, "Game.Shared", "SkillFaces.g.cs");

    /// <summary>The class CSVs in the order he authors them — the order a skill's home row is looked for in.</summary>
    private static readonly string[] ClassFiles =
    {
        "fighter 1st", "mage 1st", "warrior 2nd", "tank 2nd", "rogue 2nd", "cleric 2nd", "nuker 2nd",
        "warrior 3rd", "war_aoe 3rd", "tank 3rd", "dual 3rd", "archer 3rd", "healer 3rd", "buffer 3rd", "nuker 3rd",
        "warrior 4th", "war_aoe 4th", "tank 4th", "dual 4th", "archer 4th", "healer 4th", "buffer 4th", "nuker 4th",
        "shared 4th", "buffs", "whisps_skills",
    };

    // ---------------------------------------------------------------------------------------------
    //  RENDER
    // ---------------------------------------------------------------------------------------------

    private static readonly Regex Placeholder = new(@"@(\{(?<key>[^}]*)\})?", RegexOptions.Compiled);
    private static readonly Regex Bracket = new(@"\[(?<in>[^\[\]]*)\]", RegexOptions.Compiled);
    /// <summary>`@{mpcost.2}` = the 2nd such number (Mana Blessing: physical, then magic MP cost).</summary>
    private static readonly Regex Nth = new(@"\.(?<n>\d+)$", RegexOptions.Compiled);

    /// <summary>Resolve one placeholder key to (metric, forced pct?). null = no such word.</summary>
    internal static (string Metric, bool? Pct)? Key(string key)
    {
        key = key.Trim().ToLowerInvariant();
        key = Nth.Replace(key, "");
        if (key.Length == 0) return ("power", false);
        bool? pct = null;
        if (key.EndsWith("%")) { pct = true; key = key[..^1].TrimEnd(); }
        else if (key.EndsWith("#")) { pct = false; key = key[..^1].TrimEnd(); }
        if (key == "duration") return ("duration", false);
        foreach (var (metric, words) in Descr.Aliases)
            if (metric == key || words.Contains(key)) return (metric, pct);
        return null;
    }

    /// <summary>The rendered value of one placeholder at one level, or null when the skill has none there.</summary>
    private static string? Value(string? key, SkillDef def, int level, Dictionary<(string, bool), List<float>> pool)
    {
        var k = Key(key ?? "");
        if (k is null) return null;
        var (metric, forced) = k.Value;
        var nth = Nth.Match((key ?? "").Trim());
        int skip = nth.Success ? int.Parse(nth.Groups["n"].Value) - 1 : 0;
        if (metric == "duration")
        {
            int ticks = def.DurationTicksAt(level);
            return ticks > 0 ? Duration(ticks) : null;
        }
        float? Pick(bool pct) =>
            pool.TryGetValue((metric, pct), out var l) && l.Where(v => v != 0f).Skip(skip).FirstOrDefault() is float v && v != 0f ? v : null;
        bool usePct = forced ?? Pick(true) is not null;
        if (Pick(usePct) is not float val) return null;
        return usePct ? Num(Math.Abs(val) * 100f) + "%" : Num(Math.Abs(val));
    }

    private static string Num(float v)
    {
        double r = Math.Round(v, 2);
        return Math.Abs(r - Math.Round(r)) < 0.005 ? ((long)Math.Round(r)).ToString(CultureInfo.InvariantCulture)
                                                   : r.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Duration(int ticks)
    {
        int s = (int)Math.Round(ticks * GameConstants.TickSeconds);
        if (s >= 3600 && s % 3600 == 0) return $"{s / 3600} h";
        if (s >= 60 && s % 60 == 0) return $"{s / 60} min";
        return $"{s} sec";
    }

    /// <summary>Render a template at one level. <paramref name="missing"/> collects placeholders that had
    /// no value anywhere they were not allowed to vanish (a check error).</summary>
    internal static string Render(string template, SkillDef def, int level, List<string>? missing = null)
    {
        var pool = Descr.Pool(def, level, null);

        // 1. [ … ] groups: all-or-nothing.
        string text = Bracket.Replace(template, g =>
        {
            bool ok = true;
            string inner = Placeholder.Replace(g.Groups["in"].Value, p =>
            {
                string? v = Value(p.Groups["key"].Success ? p.Groups["key"].Value : null, def, level, pool);
                if (v is null) ok = false;
                return v ?? "";
            });
            return ok ? inner : "";
        });

        // 2. The rest, clause by clause. A clause keeps its LEADING separator so dropping it is clean.
        var clauses = Regex.Split(text, @"(?=[,;])");
        var kept = new List<string>();
        for (int i = 0; i < clauses.Length; i++)
        {
            bool ok = true;
            string c = Placeholder.Replace(clauses[i], p =>
            {
                string? v = Value(p.Groups["key"].Success ? p.Groups["key"].Value : null, def, level, pool);
                if (v is null) { ok = false; missing?.Add(p.Value); }
                return v ?? "";
            });
            if (ok) kept.Add(c);
            if (!ok && i == clauses.Length - 1 && clauses[i].TrimEnd().EndsWith(".") && kept.Count > 0)
                kept[^1] = kept[^1].TrimEnd() + ".";
        }
        string outp = string.Concat(kept).Trim();
        return outp.TrimStart(',', ';', ' ');
    }

    private static string Flat(string? s) => (s ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();

    /// <summary>Every level's text; collapses to one entry when they are all the same.</summary>
    private static string[] RenderAll(Row r, SkillDef def, List<string>? missing)
    {
        var all = new string[Math.Max(1, def.MaxLevel)];
        var miss = new List<string>();
        // An EMPTY description = the game's own per-level text, until he writes a template to take it over.
        for (int l = 1; l <= all.Length; l++)
            all[l - 1] = r.Descr.Length == 0 ? Flat(def.DescriptionAt(l))
                                             : Render(r.Descr, def, l, miss);
        // A placeholder missing at SOME level is the "lower levels take from there" rule working. Missing at
        // EVERY level means the word names nothing this skill has — that is the author's typo.
        if (missing is not null)
            foreach (Match p in Placeholder.Matches(r.Descr))
            {
                var k = Key(p.Groups["key"].Success ? p.Groups["key"].Value : "");
                if (k is null) { missing.Add($"{p.Value} is not a word in DESCR-KEYS.md"); continue; }
                bool anywhere = false;
                for (int l = 1; l <= all.Length && !anywhere; l++)
                    anywhere = Value(p.Groups["key"].Success ? p.Groups["key"].Value : null, def, l,
                                     Descr.Pool(def, l, null)) is not null;
                if (!anywhere) missing.Add($"{p.Value} — {def.Id} has no such number at any level");
            }
        return all.Distinct().Count() == 1 ? new[] { all[0] } : all;
    }

    // ---------------------------------------------------------------------------------------------
    //  READ — the class CSVs' display half, plus the two face files, as one list of faces
    // ---------------------------------------------------------------------------------------------

    /// <summary>One class-CSV row's display half.</summary>
    private sealed record CsvName(string Id, string Name, string Descr, string File, int Line);

    private static List<CsvName> ReadClassNames(string repoRoot)
    {
        var outp = new List<CsvName>();
        foreach (var file in ClassFiles)
        {
            string path = Path.Combine(ClassDir(repoRoot), file + ".csv");
            if (!File.Exists(path)) continue;
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) continue;
            var head = SplitCsv(lines[0]).Select(h => h.Trim().ToUpperInvariant()).ToList();
            int idCol = head.FindIndex(h => h is "SKILL_ID" or "ID"), nameCol = head.IndexOf("NAME"),
                dCol = head.IndexOf(DescrHeader);
            if (idCol < 0 || nameCol < 0) continue;
            for (int i = 1; i < lines.Length; i++)
            {
                var f = SplitCsv(lines[i]);
                string id = f.Count > idCol ? f[idCol].Trim() : "";
                if (id.Length == 0 || SkillCatalog.Get(id) is null) continue;
                outp.Add(new CsvName(id, f.Count > nameCol ? f[nameCol].Trim() : "",
                                     dCol >= 0 && f.Count > dCol ? f[dCol].Trim() : "", file + ".csv", i + 1));
            }
        }
        return outp;
    }

    /// <summary>A face file's rows. <paramref name="faces"/> = the 6-column exceptions layout; otherwise the
    /// 4-column `skill_faces_other.csv` one. A row with an empty SKILL_ID is a section header.</summary>
    private static List<Row> ReadFile(string repoRoot, string name, bool faces)
    {
        var rows = new List<Row>();
        string path = DataPath(repoRoot, name);
        if (!File.Exists(path)) return rows;
        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            var f = SplitCsv(lines[i]);
            while (f.Count < 6) f.Add("");
            if (f[0].Trim().Length == 0) continue;
            rows.Add(faces
                ? new Row(f[0].Trim(), f[1].Trim(), f[2].Trim().ToLowerInvariant(), f[3].Trim(), f[4].Trim(), f[5].Trim(),
                          i + 1, name)
                : new Row(f[0].Trim(), f[1].Trim(), "", "", f[2].Trim(), f[3].Trim(), i + 1, name));
        }
        return rows;
    }

    /// <summary>The names a skill may carry on a class-CSV row WITHOUT being its plain name: every exception
    /// row's name, and the code's own per-level names (Grade F…S).</summary>
    private static HashSet<string> Explained(string id, SkillDef def, IEnumerable<Row> exceptions)
    {
        var s = exceptions.Where(r => r.Id == id && (r.Race.Length > 0 || r.Class.Length > 0))
                          .Select(r => r.Name).ToHashSet();
        for (int l = 1; l <= Math.Max(1, def.MaxLevel); l++)
            if (def.NameAt(l) is var n && n != def.Name) s.Add(n);
        return s;
    }

    /// <summary>Every face: each skill's PLAIN one built from its class-CSV rows (or `skill_faces_other.csv`),
    /// then the exceptions. <paramref name="errs"/> gets what only this assembly can see.</summary>
    internal static List<Row> Load(string repoRoot, List<string> errs)
    {
        var exceptions = ReadFile(repoRoot, FacesName, true);
        var other = ReadFile(repoRoot, OtherName, false);
        var csv = ReadClassNames(repoRoot);
        var rows = new List<Row>();

        foreach (var g in csv.GroupBy(c => c.Id))
        {
            var def = SkillCatalog.Get(g.Key)!;
            var explained = Explained(g.Key, def, exceptions);
            var own = g.Select(c => c.Name).Where(n => n.Length > 0 && !explained.Contains(n)).Distinct().ToList();
            var descrs = g.Where(c => c.Descr.Length > 0).ToList();
            if (own.Count > 1)
                errs.Add($"{g.Key}: NAME disagrees across its class-CSV rows — " +
                         string.Join(" / ", own.Select(n => $"\"{n}\" ({string.Join(", ",
                             g.Where(c => c.Name == n).Take(2).Select(c => $"{c.File} line {c.Line}"))})")) +
                         $". Pick one spelling, or add a RACE/CLASS row in {FacesName} for the other.");
            if (descrs.Select(c => c.Descr).Distinct().Count() > 1)
                errs.Add($"{g.Key}: DESCRIPTION differs between " +
                         string.Join(", ", descrs.Select(c => $"{c.File} line {c.Line}")) + " — write it on one row");
            var home = descrs.FirstOrDefault() ?? g.FirstOrDefault(c => own.Contains(c.Name)) ?? g.First();
            string name = own.Count == 0 || own.Contains(home.Name) ? home.Name : own[0];
            rows.Add(new Row(g.Key, name, "", "", home.Descr, "", home.Line, home.File));
        }

        var inCsv = csv.Select(c => c.Id).ToHashSet();
        foreach (var r in other)
        {
            if (inCsv.Contains(r.Id))
                errs.Add($"{OtherName} line {r.Line} ({r.Id}): a class CSV lists this skill — its name and description " +
                         "live there; delete this row");
            else rows.Add(r);
        }
        // An exception with no DESCRIPTION wears the plain one — Blessing of Might only renames Might.
        var plainDescr = rows.ToDictionary(r => r.Id, r => r.Descr);
        foreach (var r in exceptions)
        {
            if (r.Race.Length == 0 && r.Class.Length == 0)
                errs.Add($"{FacesName} line {r.Line} ({r.Id}): no RACE or CLASS — the plain name lives in its class CSV " +
                         $"({OtherName} for a skill no class learns)");
            else rows.Add(r.Descr.Length > 0 ? r : r with { Descr = plainDescr.GetValueOrDefault(r.Id, "") });
        }
        return rows;
    }

    /// <summary>Every problem in the faces. Empty = clean.</summary>
    private static List<string> Problems(List<Row> rows)
    {
        var errs = new List<string>();
        var races = new HashSet<string> { "", "human", "elf", "demon" };
        var classNames = AllClassNames();
        var seen = new HashSet<string>();
        foreach (var r in rows)
        {
            string at = $"{r.File} line {r.Line} ({r.Id})";
            if (SkillCatalog.Get(r.Id) is not SkillDef def) { errs.Add($"{at}: no skill has this id"); continue; }
            if (r.Id.Any(c => c > 127)) errs.Add($"{at}: non-ASCII character in the id");
            if (!races.Contains(r.Race)) errs.Add($"{at}: race '{r.Race}' — use human / elf / demon or leave it blank");
            if (r.Class.Length > 0 && !classNames.Contains(r.Class)) errs.Add($"{at}: class '{r.Class}' is not a class name");
            if (r.Name.Length == 0) errs.Add($"{at}: empty NAME");
            if (!seen.Add($"{r.Id}|{r.Race}|{r.Class.ToLowerInvariant()}")) errs.Add($"{at}: duplicate row (same id, race and class)");
            var miss = new List<string>();
            RenderAll(r, def, miss);
            foreach (var m in miss) errs.Add($"{at}: {m}");
        }
        var plain = rows.Where(r => r.Race.Length == 0 && r.Class.Length == 0).Select(r => r.Id).ToHashSet();
        foreach (var def in SkillCatalog.AllSkills)
            if (!plain.Contains(def.Id))
                errs.Add($"{def.Id}: NO NAME — give it a row in its class CSV, or in {OtherName} if no class learns it");
        return errs;
    }

    private static HashSet<string> AllClassNames()
    {
        // "NPC" = the spirit helper's shelf label (SkillFaces.NpcClass), for a shelf item that is also a class skill.
        var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Fighter", "Mage", SkillFaces.NpcClass };
        foreach (var c in ClassCatalog.Playable) s.Add(c.Name);
        foreach (var c in ThirdClassCatalog.Playable) s.Add(c.Name);
        foreach (var c in FourthClassCatalog.Playable) s.Add(c.Name);
        return s;
    }


    // ---------------------------------------------------------------------------------------------
    //  GENERATE / CHECK
    // ---------------------------------------------------------------------------------------------

    private static string Generate(List<Row> rows)
    {
        var sb = new StringBuilder();
        sb.Append("// <auto-generated>\n");
        sb.Append("//   `BL-327` — GENERATED from the class CSVs' NAME/DESCRIPTION + docs/data/skill_faces.csv +\n");
        sb.Append("//   docs/data/skill_faces_other.csv by\n");
        sb.Append("//     dotnet run --project tools/SkillCsvSeed -- --gen-faces\n");
        sb.Append("//   DO NOT EDIT BY HAND: edit the CSV row and regenerate. See tools/SkillCsvSeed/Faces.cs.\n");
        sb.Append("//   One line per face: id, race, class, name, then the description per level (one = every level).\n");
        sb.Append("// </auto-generated>\n");
        sb.Append("namespace Game.Shared;\n\n");
        sb.Append("public static partial class SkillFaces\n{\n");
        sb.Append("    internal const string Data =\n");
        foreach (var r in rows)
        {
            if (SkillCatalog.Get(r.Id) is not SkillDef def) continue;
            var fields = new List<string> { r.Id, r.Race, r.Class, r.Name };
            fields.AddRange(RenderAll(r, def, null));
            sb.Append("        \"").Append(Esc(string.Join("\t", fields.Select(f => f.Replace('\t', ' '))))).Append("\\n\" +\n");
        }
        sb.Append("        \"\";\n}\n");
        return sb.ToString();
    }

    private static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\t", "\\t");

    internal static int Gen(string repoRoot)
    {
        var errs = new List<string>();
        var rows = Load(repoRoot, errs);
        errs.AddRange(Problems(rows));
        foreach (var e in errs) Console.WriteLine("  🟡 FACE " + e);
        File.WriteAllText(GenPath(repoRoot), Generate(rows));
        Console.WriteLine($"SkillFaces.g.cs: {rows.Count} faces written, {errs.Count} problem(s).");
        return errs.Count == 0 ? 0 : 1;
    }

    /// <summary>For `--check`: every problem plus a stale generated file. Returns the defect count.</summary>
    internal static int Check(string repoRoot)
    {
        var errs = new List<string>();
        var rows = Load(repoRoot, errs);
        errs.AddRange(Problems(rows));
        string gen = GenPath(repoRoot);
        if (!File.Exists(gen) || File.ReadAllText(gen).Replace("\r\n", "\n") != Generate(rows))
            errs.Add("SkillFaces.g.cs is STALE — run `SkillCsvSeed -- --gen-faces`");
        foreach (var e in errs) Console.WriteLine("  🟡 FACE " + e);
        Console.WriteLine($"faces (class CSVs + {FacesName} + {OtherName}): {rows.Count} faces, {errs.Count} problem(s).");
        return errs.Count;
    }

    // ---------------------------------------------------------------------------------------------
    //  ONE-OFF: the old face files → class CSVs (2026-09-30)
    // ---------------------------------------------------------------------------------------------

    /// <summary>`--faces-to-csv`. Reads the OLD `skill_faces.csv` + `skill_faces_single.csv` (a blank row per
    /// skill), then: a class CSV gets a last `DESCRIPTION` column, each skill's blank-row text lands on ONE of its
    /// rows (the first one bearing its plain name), a class-CSV NAME that is neither the plain name nor an
    /// explained variant is corrected to what the game shows; skills no class CSV lists go to
    /// `skill_faces_other.csv`; `skill_faces.csv` keeps its race/class rows verbatim. Refuses to run twice.</summary>
    internal static int Migrate(string repoRoot)
    {
        if (File.Exists(DataPath(repoRoot, OtherName))) { Console.Error.WriteLine($"{OtherName} exists — already done."); return 1; }
        const string OldSingle = "skill_faces_single.csv";
        var old = ReadFile(repoRoot, FacesName, true).Concat(ReadFile(repoRoot, OldSingle, true)).ToList();
        var plain = new Dictionary<string, Row>();
        foreach (var r in old.Where(r => r.Race.Length == 0 && r.Class.Length == 0)) plain.TryAdd(r.Id, r);
        var variants = old.Where(r => r.Race.Length > 0 || r.Class.Length > 0).ToList();

        // Pass 1: every class-CSV row of a known skill, with its name corrected where it has to be.
        var files = new List<(string Path, List<string> Lines, string Eol, bool Bom, int Width, int IdCol, int NameCol)>();
        var hits = new List<(int File, int Line, string Id, string Name, int Fields)>();
        var renames = new List<string>();
        foreach (var file in ClassFiles)
        {
            string path = Path.Combine(ClassDir(repoRoot), file + ".csv");
            if (!File.Exists(path)) continue;
            var (lines, eol, bom) = ReadRaw(path);
            var head = SplitCsv(lines[0]).Select(h => h.Trim().ToUpperInvariant()).ToList();
            int idCol = head.FindIndex(h => h is "SKILL_ID" or "ID"), nameCol = head.IndexOf("NAME");
            if (idCol < 0 || nameCol < 0 || head.Contains(DescrHeader)) continue;
            files.Add((path, lines, eol, bom, head.Count, idCol, nameCol));
            int fi = files.Count - 1;
            for (int i = 1; i < lines.Count; i++)
            {
                var f = SplitCsv(lines[i]);
                string id = f.Count > idCol ? f[idCol].Trim() : "";
                if (id.Length == 0 || SkillCatalog.Get(id) is not SkillDef def || !plain.TryGetValue(id, out var p)) continue;
                string nm = f.Count > nameCol ? f[nameCol].Trim() : "";
                if (nm != p.Name && !Explained(id, def, variants).Contains(nm))
                {
                    lines[i] = ReplaceField(lines[i], nameCol, p.Name);
                    renames.Add($"{file}.csv line {i + 1}: \"{nm}\" → \"{p.Name}\"");
                    nm = p.Name;
                }
                hits.Add((fi, i, id, nm, SplitCsv(lines[i]).Count));
            }
        }

        // Pass 2: each skill's description onto ONE row — the first bearing its plain name.
        var skipped = new List<string>();
        foreach (var g in hits.GroupBy(h => h.Id))
        {
            var p = plain[g.Key];
            if (p.Descr.Length == 0) continue;
            var home = g.Where(h => h.Name == p.Name).Concat(g).First();
            var (path, lines, _, _, width, _, _) = files[home.File];
            if (home.Fields > width)
            {
                skipped.Add($"{g.Key}: {Path.GetFileName(path)} line {home.Line + 1} has more cells than its header");
                continue;
            }
            lines[home.Line] = lines[home.Line] + new string(',', width - home.Fields) + "," + Quote(p.Descr);
        }
        foreach (var (path, lines, eol, bom, _, _, _) in files)
        {
            lines[0] += "," + DescrHeader;
            WriteRaw(path, lines, eol, bom);
        }

        // The face files.
        var inCsv = hits.Select(h => h.Id).ToHashSet();
        var sbOther = new StringBuilder(OtherHeader + "\n");
        foreach (var p in plain.Values.Where(p => !inCsv.Contains(p.Id)))
            sbOther.Append(Line(p.Id, p.Name, p.Descr, p.Comment));
        File.WriteAllText(DataPath(repoRoot, OtherName), sbOther.ToString().Replace("\n", "\r\n"), new UTF8Encoding(false));

        var raw = new Dictionary<string, string[]>
        {
            [FacesName] = File.ReadAllLines(DataPath(repoRoot, FacesName)),
            [OldSingle] = File.ReadAllLines(DataPath(repoRoot, OldSingle)),
        };
        var sbFaces = new StringBuilder(FacesHeader + "\n");
        foreach (var v in variants) sbFaces.Append(raw[v.File][v.Line - 1]).Append('\n');
        File.WriteAllText(DataPath(repoRoot, FacesName), sbFaces.ToString().Replace("\n", "\r\n"), new UTF8Encoding(false));
        File.Delete(DataPath(repoRoot, OldSingle));

        foreach (var r in renames) Console.WriteLine("  NAME " + r);
        foreach (var s in skipped) Console.WriteLine("  🟡 NOT MOVED " + s);
        Console.WriteLine($"{files.Count} class CSVs got a DESCRIPTION column; {renames.Count} NAME(s) corrected; " +
                          $"{plain.Count - inCsv.Count} skill(s) → {OtherName}; {variants.Count} exception row(s) kept.");
        return skipped.Count == 0 ? 0 : 1;
    }

    /// <summary>A file's lines exactly as written: its line ending and its BOM are handed back to restore.</summary>
    private static (List<string> Lines, string Eol, bool Bom) ReadRaw(string path)
    {
        var bytes = File.ReadAllBytes(path);
        bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        string text = new UTF8Encoding(false).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
        string eol = text.Contains("\r\n") ? "\r\n" : "\n";
        return (text.Split('\n').Select(l => l.TrimEnd('\r')).ToList(), eol, bom);
    }

    private static void WriteRaw(string path, List<string> lines, string eol, bool bom) =>
        File.WriteAllText(path, string.Join(eol, lines), new UTF8Encoding(bom));

    /// <summary>Replace one field of a CSV line, leaving every other character of it as written.</summary>
    private static string ReplaceField(string line, int index, string value)
    {
        int field = 0, start = 0;
        bool q = false;
        for (int i = 0; i <= line.Length; i++)
        {
            if (i < line.Length && line[i] == '"') q = !q;
            if (i == line.Length || (line[i] == ',' && !q))
            {
                if (field == index) return line[..start] + Quote(value) + line[i..];
                field++;
                start = i + 1;
            }
        }
        return line + new string(',', index - field + 1) + Quote(value);
    }

    private static string Line(params string[] f) => string.Join(",", f.Select(Quote)) + "\n";

    private static string Quote(string s) =>
        s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    private static List<string> SplitCsv(string line)
    {
        var outp = new List<string>();
        var sb = new StringBuilder();
        bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (c == '"') q = false;
                else sb.Append(c);
            }
            else if (c == '"') q = true;
            else if (c == ',') { outp.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        outp.Add(sb.ToString());
        return outp;
    }
}
