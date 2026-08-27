using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--aoe-column` — ADD THE `AOE` COLUMN, once, to every skill CSV.  BL-96.
//
//  HIS PROPOSAL, 2026-08-28: *"the column range is spell range (what distance form target) while
//  description should say the AOE range for the actual effect ... Or we should add another column aoe
//  range after every range column so a party heal should be 0 range and 900/600 aoe while targeted heal
//  is 600 range and 0 aoe ... So it's easier to read and understands"*, and then the go-ahead once he
//  saw what it fixes: *"Elemental wave - 200 AOE around caster 0 cast range - maybe enemy/AOE if we
//  have the two range columns then they will be 0,200 and that will work"*.
//
//  🔑 WHY IT IS WORTH A SCHEMA CHANGE, AND IT IS NOT COSMETIC. Two numbers were sharing one column and
//  a word. RANGE meant "how far can I throw this" for a nuke and "how wide does this go off" for a
//  party heal, so the same column could not be compared against the same code field — which is why the
//  radius has never been a CHECKED number in this pipeline, only prose in DESCR. It also forced the
//  TARGET column to carry information it was explicitly ruled not to carry: on 2026-08-27 he ruled
//  `[scope]/[breadth]` deliberately does NOT say where the circle sits, then on 2026-08-28 described
//  Elemental Wave as `self/aoe` — meaning exactly that. Two columns dissolve the contradiction:
//  TARGET says WHO, RANGE says HOW FAR YOU THROW IT, AOE says HOW WIDE IT GOES OFF. Elemental Wave is
//  `0,200,enemy/aoe` — his own worked example.
//
//  ⚠ IT INSERTS ONE FIELD AND TOUCHES NOTHING ELSE. Same discipline as `--retarget`: a character-span
//  splice on the raw line, never a re-serialise, so quoting, spacing and CRLFs survive byte-for-byte.
//  The CSVs have been corrupted twice by tools that read them and wrote them back "helpfully".
//  Run `git diff --numstat docs/data/` after — every file should read N N with N = its line count.
//
//  ⚠ IT IS IDEMPOTENT AND REFUSES TO RUN TWICE. The header is the guard: a file whose header already
//  names an AOE column is skipped. Running this twice would otherwise shift every column again and
//  silently destroy 1,268 rows, and the damage would look like a diff nobody could read.
//
//  ⚠ BANNER ROWS GET THE COMMA TOO. A `,,,,,,,,,,,,,-----Force-----` row is not a rung, but it IS a
//  line whose commas position his banner in the right column. Skipping them would leave every banner
//  one column to the left of where it has always sat.
// =====================================================================================================

internal static class AoeColumn
{
    /// <summary>Where the new field goes: immediately AFTER RANGE (index 3), so it becomes index 4 and
    /// TARGET shifts from 4 to 5. "after every range column", his words.</summary>
    private const int InsertAt = 4;

    public static int Run(string csvDir)
    {
        var files = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"No CSVs under {csvDir}"); return 1; }

        // (display name, learn level) -> the radius the GAME carries at that rung. Built from the class
        // tables for the same reason `--retarget` does: the catalog is the authority, and a row can
        // then only be wrong here if the code is wrong too.
        // ⚠ NAME -> EVERY RUNG, not name+level -> one rung. An exact (name, learn level) key was tried
        // first and left 46 rows at 0 while the code plainly had a radius: the CSV's LEARN@LVL does not
        // always equal the `ClassSkill.LearnLevel` the catalog registered (a 4th-tier row re-teaching a
        // 3rd-tier skill is the common case). Keeping every rung and picking the best match below makes
        // the lookup robust to that without pretending the two numbering schemes are the same.
        var radius = new Dictionary<string, List<(int Learn, float R)>>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, int learn, float r)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!radius.TryGetValue(key.Trim(), out var list))
                radius[key.Trim()] = list = new List<(int, float)>();
            if (!list.Any(e => e.Learn == learn)) list.Add((learn, r));
        }

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                foreach (var cs in AllClassSkills(race, bc))
                {
                    if (SkillCatalog.Get(cs.SkillId) is not SkillDef def) continue;
                    float r = def.AreaRadiusAt(cs.SkillLevel);
                    Add(def.Name, cs.LearnLevel, r);
                    Add(ClassSkills.DisplayName(cs.SkillId, race, bc, null, null), cs.LearnLevel, r);
                }

        // The radius for a row: its own rung if the catalog registered that learn level,
        // otherwise the highest rung at or below it (a radius ladder only ever steps UP, so the rung
        // you have reached is the one you are standing on), otherwise the lowest rung there is.
        float? Lookup(string name, int learn)
        {
            if (!radius.TryGetValue(name, out var list) || list.Count == 0) return null;
            foreach (var e in list) if (e.Learn == learn) return e.R;
            var below = list.Where(e => e.Learn <= learn).OrderByDescending(e => e.Learn).ToList();
            if (below.Count > 0) return below[0].R;
            return list.OrderBy(e => e.Learn).First().R;
        }

        int rows = 0, unknown = 0, skipped = 0;
        var review = new List<string>();

        foreach (string path in files)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) continue;

            if (lines[0].IndexOf(",AOE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"  skip {Path.GetFileName(path)} — already has an AOE column");
                skipped++;
                continue;
            }

            bool stop = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0) continue;

                // The HEADER gets the column NAME. Checked before the NOT-DONE gate so a file whose
                // banner sits high still gets a correct header.
                if (line.StartsWith("LEARN", StringComparison.Ordinal))
                {
                    lines[i] = Splice(line, "AOE");
                    continue;
                }

                // ⚠ IT DOES *NOT* STOP AT HIS `NOT DONE` BANNER, AND THAT IS THE DIFFERENCE FROM
                // `--retarget`. This was tried the other way first and it was wrong in a way worth
                // recording: `--retarget` rewrites the MEANING of a cell, so leaving his draft rows
                // alone is a courtesy. This inserts a COLUMN, which is STRUCTURE — skipping rows
                // leaves one file carrying two different schemas, and `--check` reads by index, so
                // every un-shifted row then reports a cascade of nonsense ("cast s CSV 0 vs code 2.5;
                // cd s CSV 2.5 vs code 5" — every field compared against its neighbour). It produced
                // 1,259 invented discrepancies on the first run.
                //
                // 🔑 THE RULE: a SEMANTIC pass may skip rows; a STRUCTURAL pass may never. A file has
                // exactly one shape or it has none.
                //
                // Draft rows past the banner still get the column, with an empty value — the shape is
                // right and the number is his to fill in, which is the same deal every other column
                // in those rows already has.
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) stop = true;
                if (line.StartsWith('#')) continue;

                var spans = FieldSpans(line);
                if (spans.Count < InsertAt + 1) continue;

                string name = Field(line, spans, 1).Trim();
                if (name.Length == 0)
                {
                    // A banner row: no name, but its commas carry the layout. One empty field keeps
                    // the banner where he put it.
                    lines[i] = Splice(line, "");
                    continue;
                }

                string value;
                int learn = int.TryParse(Field(line, spans, 0).Trim(), out int lv) ? lv : -1;
                if (stop)
                {
                    // A draft row below his NOT DONE banner: give it the column and leave the cell
                    // EMPTY. `--check` does not walk these files past the banner, and an authored 0
                    // would be a number nobody wrote pretending to be a decision.
                    value = "";
                }
                else if (learn >= 0 && Lookup(name, learn) is float r)
                {
                    value = r > 0f ? ((int)r).ToString() : "0";
                }
                else
                {
                    // No SkillDef for this rung — an unbuilt tier. 0 is the honest default (nothing
                    // in the game has a radius for it yet) and every one is printed for his eye,
                    // exactly as `--retarget` does with the rows the catalog cannot answer.
                    value = "0";
                    unknown++;
                    review.Add($"  {Path.GetFileName(path),-18} {name,-30} lvl {learn}");
                }

                lines[i] = Splice(line, value);
                rows++;
            }

            // Preserve the file's own line ending — a silent LF rewrite would show as a whole-file
            // diff and bury the real change.
            string raw = File.ReadAllText(path);
            string nl = raw.Contains("\r\n") ? "\r\n" : "\n";
            bool trailing = raw.EndsWith("\n");
            File.WriteAllText(path, string.Join(nl, lines) + (trailing ? nl : ""));
            Console.WriteLine($"  wrote {Path.GetFileName(path)}");
        }

        Console.WriteLine();
        Console.WriteLine($"{rows} row(s) given an AOE value; {skipped} file(s) already had the column.");
        if (review.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {unknown} ROW(S) THE CATALOG COULD NOT ANSWER (unbuilt tiers) — written as 0 ---");
            foreach (var r in review.Take(40)) Console.WriteLine(r);
            if (review.Count > 40) Console.WriteLine($"  … and {review.Count - 40} more");
        }
        return 0;
    }

    /// <summary>Insert one field at <see cref="InsertAt"/> by splicing the raw line. Everything before
    /// the insertion point and everything after it is copied verbatim — no re-serialise, no requoting.
    /// A line with too few fields is padded, which only ever happens on a malformed row.</summary>
    private static string Splice(string line, string value)
    {
        var spans = FieldSpans(line);
        if (spans.Count < InsertAt)
            return line + new string(',', InsertAt - spans.Count) + "," + value;
        int at = spans[InsertAt - 1].End;      // just past the RANGE field, before its comma
        return line[..at] + "," + value + line[at..];
    }

    /// <summary>Every rung any class of this (race, base) can learn, INCLUDING the 4th tier.
    ///
    /// ⚠ THE 4th TIER NEEDS ITS OWN ARGUMENT AND IS EASY TO MISS. `Cumulative(race, base, archetype,
    /// discipline)` returns the 3rd-tier kit; the ascended kit only comes back when the `fourth` flag
    /// is passed as well — the same trap `Check.Specs` documents ("Without it a 76-90 file reads as
    /// unauthored"). Omitting it here left twelve `healer 4th` rows with an AOE of 0 while the code
    /// plainly carried 600/900, and the tool reported them as CSV defects rather than as its own
    /// blind spot.</summary>
    private static IEnumerable<ClassSkill> AllClassSkills(Race race, BaseClass bc)
    {
        foreach (var cs in ClassSkills.ForClass(race, bc, null, null)) yield return cs;
        foreach (Archetype a in Enum.GetValues<Archetype>())
        {
            foreach (var cs in ClassSkills.Cumulative(race, bc, a, null)) yield return cs;
            foreach (Discipline d in Enum.GetValues<Discipline>())
            {
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d)) yield return cs;
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d, true)) yield return cs;
            }
        }
    }

    private readonly record struct Span(int Start, int End);

    /// <summary>Character spans of each comma-separated field, honouring double quotes — the same
    /// rule `Check.SplitCsv` and `Retarget.FieldSpans` read with.</summary>
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
