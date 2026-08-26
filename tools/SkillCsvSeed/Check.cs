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
    /// <summary>`-v`: also print the DESCR reader's ⚪ lines (what it could not verify). Off by default so
    /// a clean run stays one screen; on when you are auditing how much of a file is actually covered.</summary>
    public static bool Verbose;

    /// <summary>Rungs where an authored stat goes DOWN — counted apart from `problems` on purpose: this
    /// is the one thing the tool reports about the CSV rather than about the code.</summary>
    private static int ladderDips;

    private static string Num(float v, bool pct) =>
        pct ? (v * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%"
            : v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <param name="Fourth">The 4th tier — pass the ASCENDED kit through Cumulative. Without it a 76-90
    /// file reports every one of its rows as NOT REGISTERED, because the tier is gated on the Rite of
    /// Ascension and not on the level.</param>
    /// <param name="Also">Extra CSV files whose rows belong to the SAME registered set. `shared 4th.csv`
    /// is the only user: its rows are learned by every ascended class, so they show up in every 4th-tier
    /// Cumulative and would read as "extra, unauthored" against a discipline file that does not contain
    /// them. Reading both files into one CSV side is the honest comparison.</param>
    private sealed record Spec(string File, BaseClass Base, Archetype? Archetype, int Min, int Max,
                               Discipline? Discipline = null, bool Fourth = false, string[]? Also = null);

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
        // ---- 3rd TIER. A discipline spec needs the DISCIPLINE as well, because two disciplines share
        //      an archetype and `Cumulative` would otherwise hand back both kits as one.
        //
        // 🔑 ONLY FILES HE HAS FINISHED GO HERE. `healer 3rd` was added the day the healer was built
        // (2026-08-20). `buffer 3rd` earned its line when he authored its buff/harmony/group layer and
        // marked the rest `NOT DONE` — `ReadCsv` stopped at that banner, so only the authored half was
        // checked. ⚠ HE REMOVED THE BANNER on 2026-08-21 ("Ok i finished the buffer"), so the whole
        // 341-row file is live now and the sixteen still-unbuilt skill families report as 🔴 NOT
        // REGISTERED. That is the pressure working as designed, not a regression.
        // `nuker 3rd`, `dual 3rd` and the rest are still absent: no authored rows.
        //
        // ⚠ The BAND is 40-75, not 40-74: the nuker's Elemental Burst caps its last rung at 75, and a
        // tier's band is the tier, never where one file happens to stop.
        new("healer 3rd",  BaseClass.Mage,    Archetype.Healer,  40, 75, Game.Shared.Discipline.Lightbringer),
        new("buffer 3rd",  BaseClass.Mage,    Archetype.Healer,  40, 75, Game.Shared.Discipline.Warchanter),
        // `tank 3rd` earned its line on 2026-08-21, when he replaced its "start here" placeholder with
        // a real row: Shield Mastery's fourth rung at 52. One row is still a file that has to match.
        // Bulwark, not Vanguard, only because a spec needs ONE discipline and the row is registered to
        // both — check the pair by hand if that ever stops being true.
        new("tank 3rd",    BaseClass.Fighter, Archetype.Tank,    40, 75, Game.Shared.Discipline.Bulwark),
        // `nuker 3rd` earned its line on 2026-08-26, the day the kit was built. Magus, not Tempest,
        // only because a spec needs ONE discipline and the whole kit is registered to both — the same
        // caveat the tank line above carries. Check the pair by hand if that ever stops being true.
        // 🔴 CALM SPIRIT will report as NOT REGISTERED until he lifts his *"w8 on calm spirit"* hold;
        // that is the flag doing its job, not a defect. See RegisterNuker3rd.
        new("nuker 3rd",   BaseClass.Mage,    Archetype.Nuker,   40, 75, Game.Shared.Discipline.Magus),
        // ---- 4th TIER, 76-90. ONE file is authored: `healer 4th.csv`, which he calls finished
        //      (2026-08-26, 255 rows). `Also` folds in `shared 4th.csv` — the ALL-CLASSES block plus the
        //      eighteen Sigils — because those rows are in every ascended class's Cumulative and would
        //      otherwise read as unauthored extras here.
        //      🔴 `buffer 4th` was still in progress on 2026-08-26 and earns its line the day he finishes
        //      it; the other eight 4th files are two-line placeholders. Same rule as the 3rd tier.
        new("healer 4th",  BaseClass.Mage,    Archetype.Healer,  76, 90, Game.Shared.Discipline.Lightbringer,
            Fourth: true, Also: new[] { "shared 4th" }),
    };

    /// <summary>One rung, from either side, reduced to the fields worth comparing.
    ///
    /// <para>⚠ <b>Mp is the TOTAL</b>, on both sides — column 9 in his sheet, <c>MpCostAt</c> in the code.
    /// Until 2026-08-20 the sheet carried it as `INIT MP` + `FINIT MP` and this record summed them,
    /// because the two-stage payment was an engine concept his file did not share: a physical active was
    /// authored 20/0 and booked 0/20, which read as two defects per skill and hid the only thing worth
    /// reporting — a total that differs. The columns are one now and the engine splits 20/80 itself.</para></summary>
    /// <param name="Descr">His free-text DESCR cell (CSV side) — the stat VALUES, read by
    /// <see cref="Descr"/>. Empty on the code side, which carries <paramref name="Def"/> instead.</param>
    /// <param name="Def">The registered skill (code side only), so the DESCR pass can resolve what the
    /// game actually carries at <paramref name="SkillLevel"/>.</param>
    private sealed record Rung(string Name, int LearnLevel, float Range, float Cast, float Cd,
                               float Duration, int Mp, int Sp,
                               string Descr = "", SkillDef? Def = null, int SkillLevel = 1);

    public static int Run(string dir)
    {
        int problems = 0;
        ladderDips = 0;
        foreach (var spec in Specs)
        {
            string path = Path.Combine(dir, spec.File + ".csv");
            Console.WriteLine();
            Console.WriteLine($"===== {spec.File}.csv");
            if (!File.Exists(path)) { Console.WriteLine("  MISSING FILE"); problems++; continue; }

            var csv = ReadCsv(path);
            foreach (var extra in spec.Also ?? Array.Empty<string>())
            {
                string extraPath = Path.Combine(dir, extra + ".csv");
                if (File.Exists(extraPath)) csv.AddRange(ReadCsv(extraPath));
                else { Console.WriteLine("  MISSING FILE " + extra + ".csv"); problems++; }
            }
            var code = ReadRegistered(spec);
            problems += Compare(csv, code, spec);
        }

        Console.WriteLine();
        Console.WriteLine(problems == 0
            ? "No discrepancies. Every authored row matches a registered skill, and vice versa."
            : $"{problems} discrepanc(y/ies) above. The CSV is the authority — each one is a code defect until he rules otherwise.");
        if (ladderDips > 0)
            Console.WriteLine($"{ladderDips} LADDER DIP(S) above — those are in the CSV, not the code: " +
                              "a stat that falls as the rungs rise is a typo or two swapped levels.");
        return 0;
    }

    // ---- the CSV side -------------------------------------------------------------------------------

    private static List<Rung> ReadCsv(string path)
    {
        var rows = new List<Rung>();
        foreach (var line in File.ReadAllLines(path))
        {
            // ⚠ STOP AT HIS "NOT DONE" BANNER. A 3rd-tier file he is still writing marks the line
            // where the authored half ends; everything below it is a seed or a stub and is not his
            // yet. Honouring the marker is what lets a HALF-finished file be checked at all — the
            // buffer was built from rows 1-185 while its passives and attack skills were still
            // being written. A file with no such banner is read whole, exactly as before.
            if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) break;
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("LEARN")) continue;
            var f = SplitCsv(line);
            if (f.Count < 12) continue;
            // ⚠ SKIP THE SECTION BANNERS. The 3rd-tier files separate their level blocks with a row of
            // bare commas ending in `----40----`, which has the full column count and sails through the
            // width test — producing a nameless "skill" at level 0 that then NAME-DRIFT-matched itself
            // onto a real one and reported five invented mismatches. A row with no NAME is not a rung.
            if (f[1].Trim().Length == 0) continue;
            rows.Add(new Rung(f[1].Trim(), I(f[0]), F(f[3]), F(f[5]), F(f[6]), F(f[7]),
                              I(f[9]), I(f[10]), Descr: f[8].Trim()));
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
            foreach (var cs in ClassSkills.Cumulative(race, spec.Base, spec.Archetype, spec.Discipline, spec.Fourth))
            {
                if (cs.LearnLevel < spec.Min || cs.LearnLevel > spec.Max) continue;
                if (!seen.Add((cs.SkillId, cs.SkillLevel, cs.LearnLevel))) continue;
                if (SkillCatalog.Get(cs.SkillId) is not SkillDef def) continue;
                // ⚠ THE STAT-SWAP PASSIVES ARE NOT CSV CONTENT. Ten of them sit at level 40 in every
                // mage table (`Spirit (Power)`, `Insight (Vigour)`, …) and they are bought with GOLD, not
                // SP. They are the "class balance" his 2026-08-10 purge explicitly spared, so they belong
                // in no skill file and would report as ten phantom rows on every 3rd-tier check.
                // ⚠ …but NOT everything with an ExclusiveGroup, which is what this line used to say. The
                // eighteen SIGILS carry one too (it is how "one per slot" is enforced and how the
                // Mindwright finds them), and they ARE authored — every one is a row in `shared 4th.csv`.
                // Skipping them made all eighteen report as 🔴 NOT REGISTERED against code that had them.
                if (SkillCatalog.StatSwapOf(cs.SkillId) is not null) continue;
                // A TOTEM's "duration" is its LIFE, not a buff duration — `PlacesTotem` skills carry
                // DurationTicks 0 and TotemLifeTicks 300, while his DURATION column reads 30 (seconds).
                // Comparing the wrong field made every totem rung a defect.
                float duration = (def.PlacesTotem ? def.TotemLifeTicks : def.DurationTicksAt(cs.SkillLevel)) / 10f;
                float cooldown = def.CooldownTicksAt(cs.SkillLevel) / 10f;
                // A PASSIVE WITH A PROC has no timings of its own, and his CD / DURATION columns on those
                // rows describe the PROC: *"3% chance on attack to increase attack speed with 30%"* with
                // CD 20 and DURATION 15 means a 20-second internal cooldown and a 15-second buff. Reading
                // the skill's own zeroes made every sigil and both 83 proficiencies report two defects.
                if (def.ProcChance > 0f)
                {
                    cooldown = def.ProcCooldownTicks / 10f;
                    if (def.ProcSelfRungs is { Length: > 0 }
                        && SkillCatalog.Get(def.ProcSelfRungs[0]) is SkillDef payload)
                        duration = payload.DurationTicks / 10f;
                }
                rows.Add(new Rung(cs.DisplayName ?? def.Name, cs.LearnLevel,
                    def.RangeAt(cs.SkillLevel), def.CastTicksAt(cs.SkillLevel) / 10f, cooldown,
                    duration,
                    def.MpCostAt(cs.SkillLevel),
                    cs.SpCostFor(def), Def: def, SkillLevel: cs.SkillLevel));
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

                // ---- THE DESCR PASS. Everything above compares one cell to one field; this reads the
                //      free text where the VALUES live (power, +M.Atk, mpReg x1.2, -15% reuse). Only
                //      🟡 lines are defects — ⚪ lines say what the reader could not check, which is the
                //      point: unverifiable has to be visible, not silent. ----
                if (b[i].Def is SkillDef bdef)
                    foreach (var (defect, line) in Descr.Compare(a[i].Descr, bdef, b[i].SkillLevel))
                    {
                        if (defect || Verbose) Console.WriteLine($"  {label} rung {i + 1}:\n{line}");
                        if (defect) problems++;
                    }
            }

            // ---- THE LADDER CHECK — his rule, 2026-08-20: *"the stats should go up not down - if they
            //      got down i made a mistake or swaped two levels"*. It caught nothing on the day it was
            //      written because he had just fixed the row that prompted it (`healer 3rd` @44 reuse read
            //      10% between 15% and 20%), which is exactly the point: it is cheap and it is standing.
            //
            //      ⚠ Reported SEPARATELY and never counted with the rest. Everything else this tool
            //      prints is a code defect measured against an authoritative CSV; this one says the CSV
            //      itself looks wrong, and conflating the two would break the rule the file header states. ----
            for (int i = 1; i < a.Count; i++)
            {
                var lo = Descr.Values(a[i - 1].Descr);
                var hi = Descr.Values(a[i].Descr);
                foreach (var (key, now) in hi)
                {
                    if (!lo.TryGetValue(key, out float was) || now >= was - 0.0001f) continue;
                    var parts = key.Split('|');
                    string scope = parts[2].Length == 0 ? "" : $" [{parts[2]}]";
                    Console.WriteLine($"  🔵 LADDER DIP      {label}{scope} {parts[0]}: " +
                                      $"rung {i} (lvl {a[i - 1].LearnLevel}) = {Num(was, parts[1] == "%")}, " +
                                      $"rung {i + 1} (lvl {a[i].LearnLevel}) = {Num(now, parts[1] == "%")} — " +
                                      "a stat that goes DOWN between rungs is a typo or two swapped levels.");
                    ladderDips++;
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

    /// <summary>An integer cell. ⚠ <b>A `k` SUFFIX IS THOUSANDS</b> — he writes SP as `36k` / `880k` in
    /// the 3rd-tier files where the 1st/2nd ones spell `36000` out, and without this every one of those
    /// ~300 rows parsed as 0 and reported as an SP mismatch against perfectly correct code. A parser
    /// that reads a number it does not understand as ZERO is worse than one that refuses: it produced a
    /// screen of confident, wrong defects the first time `healer 3rd` was checked.</summary>
    private static int I(string s)
    {
        s = s.Trim();
        // ⚠ `kk` IS MILLIONS, and it is not a hypothetical: the whole 4th-tier price ladder is written
        // that way (`6.5kk`, `500kk`, `100kk`). Counting the k's rather than testing for one is what
        // stops `6.5kk` reading as 6500 — the shape that produced the same screen of phantom SP defects
        // the `k` case above was written to kill, one tier later.
        int k = 0;
        while (s.Length > 0 && (s[^1] == 'k' || s[^1] == 'K')) { k++; s = s[..^1].TrimEnd(); }
        if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return 0;
        for (int i = 0; i < k; i++) v *= 1000f;
        return (int)Math.Round(v);
    }

    private static float F(string s) =>
        float.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f;
}
