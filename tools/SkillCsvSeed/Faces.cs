using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Game.Shared;

// =====================================================================================================
//  `BL-327` — SKILL FACES: docs/data/skill_faces.csv → Game.Shared/SkillFaces.g.cs
//
//  The owner, 2026-09-30: *"the class csv is the numbers per lvl while the face is the display"*.
//  Columns: SKILL_ID,NAME,RACE,CLASS,DESCRIPTION,COMMENT. RACE/CLASS blank = everyone; a row with an
//  empty SKILL_ID is a section header and is skipped.
//
//  THE PLACEHOLDERS (his `@`, widened so a multi-number description can say WHICH number):
//    @            the skill's POWER (Holy Bolt's "+@ power")
//    @{m.def}     a named number — any word from DESCR-KEYS.md, or the metric key itself
//    @{p.def%}    force the percent reading; @{p.def#} the flat one (only needed when a skill has both)
//    @{duration}  the buff's duration ("20 min")
//    [ … ]        shown only when every placeholder inside has a value at that level
//  An unbracketed placeholder with no value at a level drops the CLAUSE around it (the text between
//  commas/semicolons) — his *"use the maximum … the lower lvls will take from there"*.
//
//  Numbers render WITHOUT a sign (he writes the `+`) and percents carry their `%`.
//
//  --seed-faces   ONE-OFF: writes the first file from the code's names/descriptions (refuses to overwrite)
//  --gen-faces    renders every row for every level and writes SkillFaces.g.cs
//  --check        calls Faces.Check: bad ids/races/classes/keys, a skill with no blank row, a stale .g.cs
// =====================================================================================================

internal static class Faces
{
    private const string Header = "SKILL_ID,NAME,RACE,CLASS,DESCRIPTION,COMMENT";

    internal sealed record Row(string Id, string Name, string Race, string Class, string Descr, string Comment, int Line);

    private static string CsvPath(string repoRoot) => Path.Combine(repoRoot, "docs", "data", "skill_faces.csv");
    private static string GenPath(string repoRoot) => Path.Combine(repoRoot, "Game.Shared", "SkillFaces.g.cs");

    // ---------------------------------------------------------------------------------------------
    //  RENDER
    // ---------------------------------------------------------------------------------------------

    private static readonly Regex Placeholder = new(@"@(\{(?<key>[^}]*)\})?", RegexOptions.Compiled);
    private static readonly Regex Bracket = new(@"\[(?<in>[^\[\]]*)\]", RegexOptions.Compiled);

    /// <summary>Resolve one placeholder key to (metric, forced pct?). null = no such word.</summary>
    internal static (string Metric, bool? Pct)? Key(string key)
    {
        key = key.Trim().ToLowerInvariant();
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
        if (metric == "duration")
        {
            int ticks = def.DurationTicksAt(level);
            return ticks > 0 ? Duration(ticks) : null;
        }
        float? Pick(bool pct) =>
            pool.TryGetValue((metric, pct), out var l) && l.Find(v => v != 0f) is float v && v != 0f ? v : null;
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
    //  READ
    // ---------------------------------------------------------------------------------------------

    internal static List<Row>? Read(string repoRoot)
    {
        string path = CsvPath(repoRoot);
        if (!File.Exists(path)) return null;
        var rows = new List<Row>();
        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            var f = SplitCsv(lines[i]);
            while (f.Count < 6) f.Add("");
            if (f[0].Trim().Length == 0) continue;
            rows.Add(new Row(f[0].Trim(), f[1].Trim(), f[2].Trim().ToLowerInvariant(), f[3].Trim(), f[4].Trim(),
                             f[5].Trim(), i + 1));
        }
        return rows;
    }

    /// <summary>Every problem in the file. Empty = clean.</summary>
    private static List<string> Problems(List<Row> rows)
    {
        var errs = new List<string>();
        var races = new HashSet<string> { "", "human", "elf", "demon" };
        var classNames = AllClassNames();
        var seen = new HashSet<string>();
        foreach (var r in rows)
        {
            string at = $"line {r.Line} ({r.Id})";
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
        var blank = rows.Where(r => r.Race.Length == 0 && r.Class.Length == 0).Select(r => r.Id).ToHashSet();
        foreach (var def in SkillCatalog.AllSkills)
            if (!blank.Contains(def.Id)) errs.Add($"{def.Id}: NO FACE — needs a row with blank RACE and CLASS");
        return errs;
    }

    private static HashSet<string> AllClassNames()
    {
        var s = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Fighter", "Mage" };
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
        sb.Append("//   `BL-327` — GENERATED from docs/data/skill_faces.csv by\n");
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
        var rows = Read(repoRoot);
        if (rows is null) { Console.Error.WriteLine("docs/data/skill_faces.csv is missing — run --seed-faces once."); return 1; }
        var errs = Problems(rows);
        foreach (var e in errs) Console.WriteLine("  🟡 FACE " + e);
        File.WriteAllText(GenPath(repoRoot), Generate(rows));
        Console.WriteLine($"SkillFaces.g.cs: {rows.Count} faces written, {errs.Count} problem(s).");
        return errs.Count == 0 ? 0 : 1;
    }

    /// <summary>For `--check`: the file's problems plus a stale generated file. Returns the defect count.</summary>
    internal static int Check(string repoRoot)
    {
        var rows = Read(repoRoot);
        if (rows is null) { Console.WriteLine("  🟡 FACE docs/data/skill_faces.csv is missing"); return 1; }
        var errs = Problems(rows);
        string gen = GenPath(repoRoot);
        if (!File.Exists(gen) || File.ReadAllText(gen).Replace("\r\n", "\n") != Generate(rows))
            errs.Add("SkillFaces.g.cs is STALE — run `SkillCsvSeed -- --gen-faces`");
        foreach (var e in errs) Console.WriteLine("  🟡 FACE " + e);
        Console.WriteLine($"skill_faces.csv: {rows.Count} faces, {errs.Count} problem(s).");
        return errs.Count;
    }

    // ---------------------------------------------------------------------------------------------
    //  SEED (one-off)
    // ---------------------------------------------------------------------------------------------

    private static readonly Regex DurationWords =
        new(@"(?<n>\d+)\s*(?<u>hours?|h|minutes?|mins?|seconds?|secs?|s)\b", RegexOptions.IgnoreCase);

    /// <summary>The code's description with its numbers turned into placeholders wherever the number is
    /// provably the skill's own at its top rung. What is left typed is reported in the COMMENT column.</summary>
    internal static (string Template, List<string> Typed) Templatize(SkillDef def)
    {
        int top = Math.Max(1, def.MaxLevel);
        string text = def.DescriptionAt(top);
        if (string.IsNullOrWhiteSpace(text)) text = def.Description ?? "";
        text = text.Replace("\r", " ").Replace("\n", " ").Trim();
        var pool = Descr.Pool(def, top, null);
        var edits = new List<(int At, int Len, string With)>();

        int durTicks = def.DurationTicksAt(top);
        foreach (Match m in DurationWords.Matches(text))
        {
            double n = double.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
            string u = m.Groups["u"].Value.ToLowerInvariant();
            double secs = u.StartsWith("h") ? n * 3600 : u.StartsWith("m") ? n * 60 : n;
            if (durTicks > 0 && Math.Abs(secs - durTicks * GameConstants.TickSeconds) < 0.5)
                edits.Add((m.Index, m.Length, "@{duration}"));
        }

        foreach (var t in Descr.Tokens(text, new List<string>()))
        {
            if (t.At < 0 || edits.Any(e => t.At < e.At + e.Len && e.At < t.At + t.Len)) continue;
            string raw = text.Substring(t.At, t.Len);
            string sign = raw.TrimStart().StartsWith("-") || raw.TrimStart().StartsWith("−") ? "-"
                        : raw.TrimStart().StartsWith("+") ? "+" : "";
            if (t.Mult) sign = t.Value < 0 ? "-" : "+";
            bool match = pool.TryGetValue((t.Metric, t.Pct), out var vals)
                         && vals.Any(v => Math.Abs(Math.Abs(v) - Math.Abs(t.Value)) < 0.0005f);
            if (!match) continue;
            string key = t.Metric == "power" && !t.Pct ? "@" : "@{" + t.Metric + (NeedsSuffix(pool, t.Metric, t.Pct) ? (t.Pct ? "%" : "#") : "") + "}";
            // keep a leading space the regex may have swallowed
            string lead = raw.Length > 0 && raw[0] == ' ' ? " " : "";
            edits.Add((t.At, t.Len, lead + sign + key));
        }

        var sb = new StringBuilder(text);
        foreach (var e in edits.OrderByDescending(e => e.At)) sb.Remove(e.At, e.Len).Insert(e.At, e.With);
        string templ = sb.ToString();
        var typed = Regex.Matches(Placeholder.Replace(templ, ""), @"\d+(\.\d+)?%?").Select(m => m.Value).ToList();
        return (templ, typed);
    }

    private static bool NeedsSuffix(Dictionary<(string, bool), List<float>> pool, string metric, bool pct)
    {
        bool hasPct = pool.TryGetValue((metric, true), out var p) && p.Any(v => v != 0f);
        // the default reading is "percent if there is one", so only a FLAT reading beside a percent needs '#'
        return !pct && hasPct;
    }

    internal static int Seed(string csvDir, string repoRoot, bool force)
    {
        string path = CsvPath(repoRoot);
        if (File.Exists(path) && !force)
        {
            Console.Error.WriteLine("docs/data/skill_faces.csv exists — it is his now. (--force overwrites.)");
            return 1;
        }

        // Where each id is learned, in file order — the COMMENT column, and the order he authors in.
        var order = new[] { "fighter 1st", "mage 1st", "warrior 2nd", "tank 2nd", "rogue 2nd", "cleric 2nd",
                            "nuker 2nd", "warrior 3rd", "war_aoe 3rd", "tank 3rd", "dual 3rd", "archer 3rd",
                            "healer 3rd", "buffer 3rd", "nuker 3rd", "warrior 4th", "war_aoe 4th", "tank 4th",
                            "dual 4th", "archer 4th", "healer 4th", "buffer 4th", "nuker 4th", "shared 4th",
                            "buffs", "whisps_skills" };
        var files = new Dictionary<string, List<string>>();
        var seq = new List<string>();
        foreach (var name in order)
        {
            string f = Path.Combine(csvDir, name + ".csv");
            if (!File.Exists(f)) continue;
            var lines = File.ReadAllLines(f);
            if (lines.Length == 0) continue;
            var head = SplitCsv(lines[0]);
            int idCol = head.FindIndex(h => h.Trim() is "SKILL_ID" or "ID");
            if (idCol < 0) continue;
            foreach (var l in lines.Skip(1))
            {
                var c = SplitCsv(l);
                if (c.Count <= idCol) continue;
                string id = c[idCol].Trim();
                if (id.Length == 0 || SkillCatalog.Get(id) is null) continue;
                if (!files.TryGetValue(id, out var fl)) { files[id] = fl = new List<string>(); seq.Add(id); }
                if (!fl.Contains(name)) fl.Add(name);
            }
        }

        // The racial faces that exist today (the Holy Bolt names, the three Mights he split in BL-263).
        var extra = new Dictionary<string, List<(string Race, string Class, string Name, string Descr)>>
        {
            ["holy_bolt"] = new()
            {
                ("human", "", "Holy Bolt", ""), ("elf", "", "Moonlight Bolt", ""), ("demon", "", "Spirit Bolt", ""),
            },
            ["cast_atk_phys"] = new()
            {
                ("human", "", "Blessing of Might", "A magical blessing that increases P.Atk by @{patk} for @{duration}."),
                ("elf",   "", "Forest Might",      "By the help of the forest: +@{patk} P.Atk for @{duration}."),
                ("demon", "", "Demonic Strength",  "Signing a demonic contract: +@{patk} P.Atk for @{duration}."),
            },
        };

        // The tank's Backlash: one id, the rung carries the race (ClassSkillTables.Fourth).
        extra["backlash"] = new()
        {
            ("human", "", "Physical Backlash", ""), ("elf", "", "Magical Backlash", ""), ("demon", "", "Physical Backlash", ""),
        };
        // The three per-class Momentum names that were `ClassSkill.DisplayName` overrides until BL-327.
        var momentum = extra["reuse_reset_momentum"] = new();
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            foreach (var d in new[] { Discipline.Ravager, Discipline.Warlord })
                momentum.Add(("", ClassNames.Fourth(d, race), "Battle Momentum", ""));
        }
        foreach (var (race, d) in new[] { (Race.Human, Discipline.Sharpshooter), (Race.Elf, Discipline.Trapper), (Race.Demon, Discipline.Hunter) })
            momentum.Add(("", ClassNames.Fourth(d, race), "Bow Momentum", ""));
        foreach (var (race, d) in new[] { (Race.Human, Discipline.Nullblade), (Race.Elf, Discipline.Phantom), (Race.Demon, Discipline.Venomweaver) })
            momentum.Add(("", ClassNames.Fourth(d, race), "Stab Momentum", ""));

        string Sect(SkillDef d) => d.Category == SkillCategory.Passive ? "PASSIVE"
                                 : d.Category == SkillCategory.Buff ? "BUFF" : "ACTIVE";
        var sb = new StringBuilder(Header + "\n");
        int typedCount = 0;
        void Emit(SkillDef def, string where)
        {
            var (templ, typed) = Templatize(def);
            if (typed.Count > 0) typedCount++;
            string comment = where + (typed.Count > 0 ? $" | typed numbers kept: {string.Join(" ", typed.Distinct())}" : "");
            // Typed numbers + text that CHANGES per level = the top rung's numbers would show at every level. Leave
            // the cell empty (the game's own per-level text) and say so, until he writes a template.
            bool perLevel = Enumerable.Range(1, Math.Max(1, def.MaxLevel)).Select(def.DescriptionAt).Distinct().Count() > 1;
            if (typed.Count > 0 && perLevel)
            {
                comment = where + " | EMPTY = the game's per-level text; top rung reads: " + templ.Replace(",", ";");
                templ = "";
            }
            sb.Append(Line(def.Id, def.Name, "", "", templ, comment));
            if (extra.TryGetValue(def.Id, out var ex))
                foreach (var (race, cls, name, d) in ex)
                    sb.Append(Line(def.Id, name, race, cls, d.Length > 0 ? d : templ, ""));
        }
        foreach (var sect in new[] { "ACTIVE", "BUFF", "PASSIVE" })
        {
            sb.Append($",,,,,---------------------------- {sect} ----------------------------\n");
            foreach (var id in seq)
                if (SkillCatalog.Get(id) is SkillDef def && Sect(def) == sect)
                    Emit(def, string.Join(", ", files[id]));
        }
        sb.Append(",,,,,---------------------------- NOT IN A CLASS CSV (mobs, NPCs, items, internal) ----------------------------\n");
        foreach (var def in SkillCatalog.AllSkills.Where(d => !files.ContainsKey(d.Id)).OrderBy(d => d.Id, StringComparer.Ordinal))
            Emit(def, "");

        File.WriteAllText(path, sb.ToString().Replace("\n", "\r\n"), new UTF8Encoding(false));
        Console.WriteLine($"skill_faces.csv seeded: {SkillCatalog.AllSkills.Count()} skills, {typedCount} with typed numbers left.");
        return 0;
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
