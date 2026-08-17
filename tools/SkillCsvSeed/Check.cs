using System.Globalization;
using System.Text;
using Game.Shared;

// =====================================================================================================
//  --check — THE RECHECK. Every CSV in docs/data/classes_skills_csv/ against what the game actually
//  registers, row by row. His ask, 2026-08-17: *"the recheck of all seven files against the registered
//  tables"*.
//
//  🔑 WHY A TOOL AND NOT A READ-THROUGH: the seven authored files are ~180 rows and a rung is implied by
//  ORDER, not written down — `Heal` appears four times at 20/25/30/35 and those are levels 1-4. Eyeballing
//  that is exactly how a wrong balance number gets ratified (docs: "measure, don't derive").
//
//  🔑 THE CSV IS THE AUTHORITY. Everything this prints is a defect in the CODE unless he says otherwise —
//  it never suggests editing a CSV to match.
//
//  MATCHING: by NAME, normalised (lowercased, non-alphanumerics dropped) because his files carry
//  `Anti magic` / `Weapon mastery` against the catalog's `Anti-Magic` / `Weapon Mastery`. Rungs are then
//  paired by ascending learn level within a name.
// =====================================================================================================

internal static class Check
{
    private sealed record Spec(string File, BaseClass Base, Archetype? Archetype, int Min, int Max);

    // ⚠ The BAND matters. A 2nd-class ladder does not stop where his file stops — `Elemental Bolt` is
    // registered at 20…80 while `nuker 2nd.csv` authors 20-35, and without the band every one of those
    // upper rungs reads as "the CSV is missing a row". The band is the tier, not the filename.
    private static readonly Spec[] Specs =
    {
        new("fighter 1st", BaseClass.Fighter, null,              1, 19),
        new("mage 1st",    BaseClass.Mage,    null,              1, 19),
        new("tank 2nd",    BaseClass.Fighter, Archetype.Tank,    20, 39),
        new("warrior 2nd", BaseClass.Fighter, Archetype.Warrior, 20, 39),
        new("rogue 2nd",   BaseClass.Fighter, Archetype.Rogue,   20, 39),
        new("nuker 2nd",   BaseClass.Mage,    Archetype.Nuker,   20, 39),
        new("cleric 2nd",  BaseClass.Mage,    Archetype.Healer,  20, 39),
    };

    /// <summary>One rung, from either side, reduced to the fields worth comparing.
    ///
    /// <para>⚠ <b>Mp is the TOTAL, not the two columns.</b> The two-stage model (`InitialMpCost` paid on
    /// cast, `FinishMp` on landing) is an engine concept his sheet does not share: on a physical active he
    /// writes the whole cost in INIT MP and leaves FINIT MP at 0 (`Strike` 20/0), while the code books the
    /// same skill as 0/20. Comparing the columns separately reports every physical skill in the game as
    /// two defects and hides the one that matters — a total that actually differs.</para></summary>
    private sealed record Rung(string Name, int LearnLevel, float Range, float Cast, float Cd,
                               float Duration, int Mp, int Sp);

    public static int Run(string dir)
    {
        int problems = 0;
        foreach (var spec in Specs)
        {
            string path = Path.Combine(dir, spec.File + ".csv");
            Console.WriteLine();
            Console.WriteLine($"===== {spec.File}.csv");
            if (!File.Exists(path)) { Console.WriteLine("  MISSING FILE"); problems++; continue; }

            var csv = ReadCsv(path);
            var code = ReadRegistered(spec);
            problems += Compare(csv, code, spec);
        }

        Console.WriteLine();
        Console.WriteLine(problems == 0
            ? "No discrepancies. Every authored row matches a registered skill, and vice versa."
            : $"{problems} discrepanc(y/ies) above. The CSV is the authority — each one is a code defect until he rules otherwise.");
        return 0;
    }

    // ---- the CSV side -------------------------------------------------------------------------------

    private static List<Rung> ReadCsv(string path)
    {
        var rows = new List<Rung>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("LEARN")) continue;
            var f = SplitCsv(line);
            if (f.Count < 13) continue;
            rows.Add(new Rung(f[1].Trim(), I(f[0]), F(f[3]), F(f[5]), F(f[6]), F(f[7]),
                              I(f[9]) + I(f[10]), I(f[11])));
        }
        return rows;
    }

    /// <summary>A CSV line honouring double quotes — his descriptions are full of commas, and a naive
    /// Split(',') silently shifts every column after one.</summary>
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

    // ---- the code side ------------------------------------------------------------------------------

    /// <summary>What the game registers for this tier, collapsed across the three races. Uses
    /// <see cref="ClassSkills.Cumulative"/>, NOT ForClass: the armour/weapon masteries are injected
    /// centrally by archetype and are authored in his files, so ForClass alone would report every one
    /// of them as missing.</summary>
    private static List<Rung> ReadRegistered(Spec spec)
    {
        var seen = new HashSet<(string, int, int)>();
        var rows = new List<Rung>();
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            foreach (var cs in ClassSkills.Cumulative(race, spec.Base, spec.Archetype, null))
            {
                if (cs.LearnLevel < spec.Min || cs.LearnLevel > spec.Max) continue;
                if (!seen.Add((cs.SkillId, cs.SkillLevel, cs.LearnLevel))) continue;
                if (SkillCatalog.Get(cs.SkillId) is not SkillDef def) continue;
                rows.Add(new Rung(cs.DisplayName ?? def.Name, cs.LearnLevel,
                    def.RangeAt(cs.SkillLevel), def.CastTicks / 10f, def.CooldownTicks / 10f,
                    def.DurationTicks / 10f,
                    def.InitialMpAt(cs.SkillLevel) + def.FinishMpAt(cs.SkillLevel),
                    def.SpCostAt(cs.SkillLevel)));
            }
        return rows;
    }

    /// <summary>Does a skill of this name exist in the catalog at all? An authored row with no class-table
    /// entry means something very different depending on the answer: a skill the catalog has never heard
    /// of is unbuilt, while one it knows is built-but-not-learnable — usually because it is AUTO-GRANTED
    /// (his SP-0 rows: Precision, Evasion Mastery, the magic-fail floor), which is not a defect at all.</summary>
    private static bool InCatalog(string name) =>
        SkillCatalog.AllSkills.Any(d => Norm(d.Name) == Norm(name));

    // ---- the comparison -----------------------------------------------------------------------------

    private static int Compare(List<Rung> csv, List<Rung> code, Spec spec)
    {
        int problems = 0;
        var csvByName  = Group(csv);
        var codeByName = Group(code);

        // Pair up the leftovers by SPELLING before reporting them. `Defencive Wall` / `Defensive Wall`,
        // `Bow Expretise` / `Bow Expertise`, `Stab` / `Piercing Stab` are one skill under two spellings,
        // and printing them as "missing" + "extra" buries the handful of rows that are genuinely absent.
        var aliases = PairUp(csvByName.Keys.Except(codeByName.Keys).ToList(),
                             codeByName.Keys.Except(csvByName.Keys).ToList());
        foreach (var (from, to) in aliases)
        {
            var a0 = csvByName[from]; var b0 = codeByName[to];
            Console.WriteLine($"  🔵 NAME DRIFT      CSV \"{a0[0].Name}\" = code \"{b0[0].Name}\" " +
                              $"(matched on spelling; rungs {a0.Count} vs {b0.Count})");
            csvByName[to] = a0; csvByName.Remove(from);
        }

        foreach (var name in csvByName.Keys.Concat(codeByName.Keys).Distinct().OrderBy(x => x))
        {
            csvByName.TryGetValue(name, out var a);
            codeByName.TryGetValue(name, out var b);
            string label = (a ?? b)![0].Name;

            if (b is null)
            {
                bool built = InCatalog(label);
                bool free  = a!.All(r => r.Sp == 0);
                if (built && free)
                    Console.WriteLine($"  ⚪ AUTO-GRANTED    {label} — authored at SP 0, exists in the " +
                                      "catalog, no class-table row. That is what auto-granted looks like.");
                else
                {
                    Console.WriteLine($"  🔴 NOT REGISTERED  {label} — {a!.Count} authored rung(s) at " +
                                      $"{string.Join('/', a.Select(r => r.LearnLevel))}, the class learns none" +
                                      (built ? " (the skill EXISTS — only the learn row is missing)." : " (no such skill in the catalog)."));
                    problems++;
                }
                continue;
            }
            if (a is null)
            {
                Console.WriteLine($"  🟠 NOT IN THE CSV  {label} — the class learns {b.Count} rung(s) at " +
                                  $"{string.Join('/', b.Select(r => r.LearnLevel))} with nothing authored.");
                problems++; continue;
            }
            // An AUTO-GRANTED first rung (his SP-0 row, e.g. `Magic Bolt` at level 1) has no class-table
            // entry, so leaving it in shifts the whole ladder by one and turns a matching skill into a
            // page of invented mismatches. Drop it, don't compare it.
            while (a.Count > b.Count && a[0].Sp == 0 && b.All(r => r.LearnLevel != a[0].LearnLevel))
            {
                Console.WriteLine($"  ⚪ AUTO-GRANTED    {label} rung at level {a[0].LearnLevel} " +
                                  "(SP 0, no class-table row) — skipped, the rest of the ladder is compared.");
                a = a.Skip(1).ToList();
            }

            if (a.Count != b.Count)
            {
                Console.WriteLine($"  🔴 RUNG COUNT      {label} — CSV has {a.Count} " +
                                  $"({string.Join('/', a.Select(r => r.LearnLevel))}), " +
                                  $"code has {b.Count} ({string.Join('/', b.Select(r => r.LearnLevel))}).");
                problems++;
            }

            for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            {
                var diffs = new List<string>();
                Cmp(diffs, "learn lvl", a[i].LearnLevel, b[i].LearnLevel);
                Cmp(diffs, "range",     a[i].Range,      b[i].Range);
                Cmp(diffs, "cast s",    a[i].Cast,       b[i].Cast);
                Cmp(diffs, "cd s",      a[i].Cd,         b[i].Cd);
                Cmp(diffs, "duration",  a[i].Duration,   b[i].Duration);
                Cmp(diffs, "mp total",  a[i].Mp,         b[i].Mp);
                Cmp(diffs, "sp",        a[i].Sp,         b[i].Sp);
                if (diffs.Count > 0)
                {
                    Console.WriteLine($"  🟡 {label} rung {i + 1} (CSV lvl {a[i].LearnLevel}): " +
                                      string.Join("; ", diffs));
                    problems++;
                }
            }
        }
        return problems;
    }

    private static void Cmp(List<string> into, string field, float csv, float code)
    {
        if (Math.Abs(csv - code) < 0.005f) return;
        into.Add($"{field} CSV {csv.ToString("0.##", CultureInfo.InvariantCulture)} vs code " +
                 $"{code.ToString("0.##", CultureInfo.InvariantCulture)}");
    }

    /// <summary>Greedy best-match between the names only one side has. A pair is accepted when one
    /// name CONTAINS the other (`stab` in `piercingstab`) or the edit distance is small relative to the
    /// length — enough for a transposed letter, not enough to marry two different skills.</summary>
    private static List<(string From, string To)> PairUp(List<string> csvOnly, List<string> codeOnly)
    {
        var pairs = new List<(string, string)>();
        var taken = new HashSet<string>();
        foreach (var a in csvOnly.OrderBy(x => x))
        {
            string? best = null; int bestD = int.MaxValue;
            foreach (var b in codeOnly)
            {
                if (taken.Contains(b)) continue;
                int d = a.Contains(b) || b.Contains(a) ? 0 : Distance(a, b);
                if (d < bestD) { bestD = d; best = b; }
            }
            if (best is not null && bestD <= Math.Max(1, Math.Min(a.Length, best.Length) / 5))
            {
                pairs.Add((a, best));
                taken.Add(best);
            }
        }
        return pairs;
    }

    private static int Distance(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur  = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (int j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1),
                                  prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }

    private static Dictionary<string, List<Rung>> Group(List<Rung> rows) =>
        rows.GroupBy(r => Norm(r.Name))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.LearnLevel).ToList());

    /// <summary>`Anti magic` and `Anti-Magic` are the same skill; so are `Weapon mastery` and
    /// `Weapon Mastery`. Also drops a trailing ` L2`/` L3` rung suffix so the 40+ format collapses onto
    /// the 20-35 one, where a rung is just a repeated name.</summary>
    private static string Norm(string s)
    {
        s = s.Trim();
        int i = s.LastIndexOf(" L", StringComparison.OrdinalIgnoreCase);
        if (i > 0 && int.TryParse(s[(i + 2)..], out _)) s = s[..i];
        var sb = new StringBuilder();
        foreach (char c in s) if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    private static int I(string s) =>
        int.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static float F(string s) =>
        float.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f;
}
