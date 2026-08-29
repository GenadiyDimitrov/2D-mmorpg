using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--id-column` — ADD `SKILL_ID`, once, to every skill CSV, and REWRITE `REPLACES` FROM NAMES TO IDS.
//
//  HIS INSTRUCTION, 2026-08-29:
//    *"we can add a column skill_ID and the name to be just the 'display name' ... and replace the
//     replaces column to be a list of id's not names .... U can fill them for now and I'll look at
//     them ... Now having only names start to take its toll. We can have 10 skills with the same
//     display name but to be actual different skills."*
//
//  🔑 HE IS RIGHT, AND THE EVIDENCE IS IN THIS TOOL'S OWN HISTORY. Two days running, a name-keyed
//  lookup produced a wrong answer nobody could see:
//    - `--weapon-column` gave `rogue 2nd.csv` the TANK's weapon requirement, because THREE different
//      skills are displayed "Weapon Mastery".
//    - `Descr.cs` keys its exception table on the name, so the 2026-08-29 rename silently unhooked it.
//  A display name is a LABEL. The id is the identity, and the sheet has never carried it.
//
//  WHAT THIS WRITES:
//    - `SKILL_ID` at index 2, right after NAME, so the identity sits beside the label he reads by.
//    - `REPLACES` as ids: `[Weapon Mastery]` -> `[tank_weapon_mastery]`.
//
//  ⚠ REPLACES IS AMBIGUOUS BY CONSTRUCTION and that is the whole reason he is retiring it. The cells
//  are space-separated lists of names — and the names themselves contain spaces:
//  `[Shield Harden Shield Bless]` is TWO skills, `[Might Fury Vampirism]` is THREE. It is resolved by
//  GREEDY LONGEST MATCH against the names that file's own class actually knows.
//  🔴 A cell that does not fully resolve is LEFT EXACTLY AS IT IS and printed for him. Some are prose
//  (`[60x1.1^buffIndex per buff]`) and some are shorthand for a family
//  (`[Healer/Mage/Tank/Buffer/Rogue Attack]` = the five Attack sigils). Guessing at those would put
//  invented ids in an authoritative file, which is the one thing this pipeline must never do.
//
//  ⚠ Same splice discipline as `--weapon-column` / `--aoe-column`: one field inserted by character
//  span, never a re-serialise. Idempotent — the header is the guard. A STRUCTURAL pass may never skip
//  a row, so banners and rows below `NOT DONE` get the column too (empty).
// =====================================================================================================

internal static class IdColumn
{
    /// <summary>SKILL_ID goes at index 2 — after NAME, before TYPE.</summary>
    private const int InsertAt = 2;

    public static int Run(string csvDir)
    {
        var files = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"No CSVs under {csvDir}"); return 1; }

        int rows = 0, filled = 0, skipped = 0, replRewritten = 0;
        var noId = new List<string>();
        var noRepl = new List<string>();

        foreach (string path in files)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) continue;
            if (lines[0].IndexOf("SKILL_ID", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"  skip {Path.GetFileName(path)} — already has a SKILL_ID column");
                skipped++;
                continue;
            }

            // Per-file scope, for the same reason `--weapon-column` needs one: a display name is only
            // unique inside one class's own kit.
            var (byName, names) = BuildLookup(WeaponColumn.Scope(Path.GetFileNameWithoutExtension(path)));
            string file = Path.GetFileName(path);

            bool stop = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0) continue;

                if (line.StartsWith("LEARN", StringComparison.Ordinal))
                {
                    lines[i] = Splice(line, "SKILL_ID");
                    continue;
                }
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) stop = true;
                if (line.StartsWith('#')) continue;

                var spans = FieldSpans(line);
                if (spans.Count < InsertAt + 1) continue;

                string name = Field(line, spans, 1).Trim();
                if (name.Length == 0) { lines[i] = Splice(line, ""); continue; }

                string id = "";
                int learn = int.TryParse(Field(lines[i], spans, 0).Trim(), out int lv) ? lv : -1;
                if (!stop && byName.TryGetValue(Norm(name), out var hits))
                {
                    id = Pick(hits, learn);
                    filled++;
                }
                else if (!stop)
                {
                    noId.Add($"  {file,-18} {name,-32} lvl {learn}");
                }

                // ---- REPLACES, rewritten in place BEFORE the splice (the splice shifts spans). ----
                // The column is the LAST bracketed cell on the row; found by content, not by index,
                // because the 3rd/4th files carry an extra RACE column after it.
                //
                // 🔴 THE CODE IS THE SOURCE, NOT THE NAME. Resolving the cell's NAMES was tried first
                // and produced `rogue 2nd`'s Weapon Mastery replacing ITSELF: the row said
                // `[Weapon Mastery]` meaning the FIGHTER's, and the rogue's own kit — which is
                // cumulative, so it contains both — matched its own entry first. The skill already
                // knows what it retires (`SkillDef.Replaces`, ids, unambiguous), so that is what gets
                // written. Name resolution survives only as the fallback for a row whose skill the
                // code does not carry, and it excludes self.
                if (!stop)
                {
                    string before = lines[i];
                    lines[i] = RewriteReplaces(lines[i], id, byName, out string? unresolved);
                    if (before != lines[i]) replRewritten++;
                    if (unresolved is not null) noRepl.Add($"  {file,-18} {name,-28} {unresolved}");
                    spans = FieldSpans(lines[i]);
                }

                lines[i] = Splice(lines[i], id);
                rows++;
            }

            string raw = File.ReadAllText(path);
            string nl = raw.Contains("\r\n") ? "\r\n" : "\n";
            bool trailing = raw.EndsWith("\n");
            File.WriteAllText(path, string.Join(nl, lines) + (trailing ? nl : ""));
            Console.WriteLine($"  wrote {file}");
        }

        Console.WriteLine();
        Console.WriteLine($"{rows} row(s) given a SKILL_ID cell ({filled} resolved); " +
                          $"{replRewritten} REPLACES cell(s) rewritten as ids; {skipped} file(s) skipped.");
        if (noId.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {noId.Count} ROW(S) WHOSE NAME THE CATALOG DOES NOT KNOW — id left EMPTY ---");
            Console.WriteLine("    (an unbuilt tier, or a name that no longer matches — yours to fill in)");
            foreach (string s in noId.Take(30)) Console.WriteLine(s);
            if (noId.Count > 30) Console.WriteLine($"  … and {noId.Count - 30} more");
        }
        if (noRepl.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {noRepl.Count} REPLACES CELL(S) LEFT AS THEY ARE (could not be fully resolved) ---");
            Console.WriteLine("    Prose, or a family shorthand, or a name the class does not know. NOT guessed.");
            foreach (string s in noRepl.Distinct().Take(30)) Console.WriteLine(s);
        }
        return 0;
    }

    /// <summary>Rewrite the row's `[...]` REPLACES cell from names to ids. Returns the line unchanged
    /// (and sets <paramref name="unresolved"/>) when any token fails to resolve — never a partial
    /// rewrite, which would be worse than leaving it alone.</summary>
    private static string RewriteReplaces(string line, string rowSkillId,
                                          Dictionary<string, List<(int Learn, string Id)>> byName,
                                          out string? unresolved)
    {
        unresolved = null;
        int open = line.LastIndexOf('[');
        if (open < 0) return line;
        int close = line.IndexOf(']', open);
        if (close < 0) return line;

        string inner = line[(open + 1)..close].Trim();
        if (inner.Length == 0) return line;                    // `[]` — nothing to replace
        // Already ids? (idempotence, and a hand-authored id list must survive a re-run.)
        if (inner.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                 .All(t => SkillCatalog.Get(t) is not null)) return line;

        // 1. THE CODE, when it knows this row's skill: `Replaces` is already a list of ids.
        if (rowSkillId.Length > 0 && SkillCatalog.Get(rowSkillId) is SkillDef def
            && def.Replaces is { Length: > 0 })
            return line[..(open + 1)] + string.Join(' ', def.Replaces) + line[close..];

        // 2. Otherwise resolve the NAMES, never onto this row's own skill.
        var ids = Tokenise(inner, byName, rowSkillId);
        if (ids is null) { unresolved = $"[{inner}]"; return line; }
        return line[..(open + 1)] + string.Join(' ', ids) + line[close..];
    }

    /// <summary>GREEDY LONGEST MATCH over a space-separated list whose entries may themselves contain
    /// spaces — `Shield Harden Shield Bless` is two skills, `Might Fury Vampirism` is three. Longest
    /// first is what makes that decidable; null the moment any prefix fails.</summary>
    private static List<string>? Tokenise(string inner,
                                          Dictionary<string, List<(int Learn, string Id)>> byName,
                                          string selfId)
    {
        var words = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var outp = new List<string>();
        int i = 0;
        while (i < words.Length)
        {
            string? best = null;
            int bestLen = 0;
            for (int len = Math.Min(5, words.Length - i); len >= 1; len--)
            {
                string candidate = string.Join(' ', words.Skip(i).Take(len));
                if (byName.TryGetValue(Norm(candidate), out var hits))
                {
                    // ⚠ Never resolve onto the row's own skill — a cumulative kit contains the
                    // very skill this row supersedes, under the same display name.
                    var usable = hits.Where(h => h.Id != selfId).ToList();
                    if (usable.Count == 0) continue;
                    best = Pick(usable, -1);
                    bestLen = len;
                    break;
                }
            }
            if (best is null) return null;
            outp.Add(best);
            i += bestLen;
        }
        return outp.Count > 0 ? outp : null;
    }

    /// <summary>The id for a name at a learn level. Every rung of one skill shares an id, so this
    /// only has to choose when a name really is two different skills inside one class — which is the
    /// case his instruction is about. Prefers the exact learn level, then the nearest rung below.</summary>
    private static string Pick(List<(int Learn, string Id)> hits, int learn)
    {
        if (hits.Count == 1 || learn < 0) return hits[0].Id;
        foreach (var h in hits) if (h.Learn == learn) return h.Id;
        var below = hits.Where(h => h.Learn <= learn).OrderByDescending(h => h.Learn).ToList();
        return below.Count > 0 ? below[0].Id : hits[0].Id;
    }

    /// <summary>Normalised display name -> the ids registered under it, with the learn level of each.
    /// Same normalisation as `Check.Norm` (case, punctuation and a trailing ` L2` rung suffix), so the
    /// two agree about what "the same name" means.</summary>
    private static (Dictionary<string, List<(int Learn, string Id)>>, List<string>)
        BuildLookup((BaseClass Base, Archetype? Arch, Discipline[] Disc, bool Fourth)? scope)
    {
        var byName = new Dictionary<string, List<(int, string)>>(StringComparer.Ordinal);
        void Add(string name, int learn, string id)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            string k = Norm(name);
            if (!byName.TryGetValue(k, out var list)) byName[k] = list = new List<(int, string)>();
            if (!list.Any(e => e.Item1 == learn && e.Item2 == id)) list.Add((learn, id));
        }

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            if (scope is null)
            {
                foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                    foreach (var cs in WeaponColumn.AllClassSkills(race, bc)) Feed(race, bc, cs, null, null);
                continue;
            }
            var s = scope.Value;
            if (s.Arch is null)
                foreach (var cs in ClassSkills.ForClass(race, s.Base, null, null)) Feed(race, s.Base, cs, null, null);
            else if (s.Disc.Length == 0)
                foreach (var cs in ClassSkills.Cumulative(race, s.Base, s.Arch, null)) Feed(race, s.Base, cs, s.Arch, null);
            else
                foreach (var d in s.Disc)
                    foreach (var cs in ClassSkills.Cumulative(race, s.Base, s.Arch, d, s.Fourth))
                        Feed(race, s.Base, cs, s.Arch, d);
        }

        void Feed(Race race, BaseClass bc, ClassSkill cs, Archetype? a, Discipline? d)
        {
            if (SkillCatalog.Get(cs.SkillId) is not SkillDef def) return;
            Add(def.Name, cs.LearnLevel, def.Id);
            Add(ClassSkills.DisplayName(cs.SkillId, race, bc, a, d), cs.LearnLevel, def.Id);
        }

        // ⚠ A REPLACES target need not be learnable by this class — a group buff retires SINGLES the
        // buffer casts on others, and a 4th-tier skill can retire one nobody learns any more. So the
        // whole catalog is a FALLBACK for name resolution, added last so a class's own kit wins.
        foreach (var def in SkillCatalog.AllSkills) Add(def.Name, -1, def.Id);

        return (byName, byName.Keys.ToList());
    }

    private static string Norm(string s)
    {
        s = s.Trim();
        int i = s.LastIndexOf(" L", StringComparison.OrdinalIgnoreCase);
        if (i > 0 && int.TryParse(s[(i + 2)..], out _)) s = s[..i];
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    private static string Splice(string line, string value)
    {
        var spans = FieldSpans(line);
        if (spans.Count < InsertAt)
            return line + new string(',', InsertAt - spans.Count) + "," + value;
        int at = spans[InsertAt - 1].End;
        return line[..at] + "," + value + line[at..];
    }

    private readonly record struct Span(int Start, int End);

    private static List<Span> FieldSpans(string line)
    {
        var spans = new List<Span>();
        bool q = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') i++;
                else if (c == '"') q = false;
            }
            else if (c == '"') q = true;
            else if (c == ',') { spans.Add(new Span(start, i)); start = i + 1; }
        }
        spans.Add(new Span(start, line.Length));
        return spans;
    }

    private static string Field(string line, List<Span> spans, int i) =>
        i < spans.Count ? line[spans[i].Start..spans[i].End].Trim('"') : "";
}
