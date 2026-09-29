using Game.Shared;

/// <summary>
/// `BL-314` — THE SP BUDGET, measured before the passive split is designed. His why (2026-09-28): *"after lvl 20
/// or so u have SP to spare ... the sum of all the passives is 3-4 times more that the current single one - until
/// 75 .. after the sum should be x1"*.
///
/// <para>Two numbers per level, per class path, both read off the live data:</para>
/// <list type="bullet">
/// <item><b>SP earned</b> in a level = <c>ExpCurve.ExpToNext(L) × ExpCurve.SpToExpRatio</c>. SP is a constant 1/20 of
///   the same kill's EXP, so however tough the mob, levelling from L to L+1 pays exactly this at ×1 rates, solo or
///   partied (both are split alike). Quest SP and SP scrolls are extra and are not counted.</item>
/// <item><b>SP owed</b> at level L = every rung of the path's kit with <c>LearnLevel == L</c>, priced through
///   <see cref="ClassSkill.SpCostFor"/> (per-class overrides included). The path is the 1st-class list, the 2nd, the
///   3rd and the ascended 4th, de-duplicated by (skill, rung). Of each EXCLUSIVE group (the level-40 stat swaps and
///   similar pick-one sets) only the dearest member counts: you can own one.</item>
/// </list>
///
/// <para><c>dotnet run --project tools/BalanceMatrix -- --sp-budget</c> prints the summary for every path;
/// add a discipline name (<c>--sp-budget Magus</c>) for its level-by-level table.</para>
/// </summary>
static class SpBudget
{
    private sealed record Path(string Name, Race Race, BaseClass Base, Archetype Arch, Discipline Disc);

    private sealed record Row(int Level, long Cost, long PassiveCost, int Rungs);

    public static void Run(string[] args)
    {
        string? detail = args.Length > 1 ? args[1] : null;
        var paths = new List<Path>();
        foreach (var sc in ClassCatalog.Playable)
        {
            var (a, b) = Disciplines.Of(sc.Race, sc.Archetype);
            foreach (var d in b is { } bb ? new[] { a, bb } : new[] { a })
                paths.Add(new Path($"{ClassNames.Third(d, sc.Race)} ({sc.Race} {d})", sc.Race, sc.Base, sc.Archetype, d));
        }

        // SP earned per level, and the running total a character HOLDS on arriving at a level.
        int max = ExpCurve.MaxLevel;
        var earned = new long[max + 2];
        for (int L = 1; L <= max; L++)
            earned[L] = (long)Math.Round(ExpCurve.ExpToNext(L) * (double)ExpCurve.SpToExpRatio);

        Console.WriteLine("=== BL-314 · SP EARNED vs THE WHOLE KIT's COST (x1 rates, killing at your own level) ===");
        Console.WriteLine("  earned = ExpToNext(L)/20, the SP one level of kills pays.  owed = the rungs that open AT that level.");
        Console.WriteLine("  bank = SP you hold after buying EVERYTHING open to you (earned before arriving − owed so far); < 0 = short.");
        Console.WriteLine();
        Console.WriteLine("  Earned per band:  " + string.Join("  ", Bands.Select(bd => $"{bd.Lo}-{bd.Hi}: {Sum(earned, bd.Lo, bd.Hi),13:N0}")));
        Console.WriteLine();

        Console.WriteLine($"  {"path",-38} | {"owed 1-19",11} {"20-39",11} {"40-59",11} {"60-75",11} {"76-85",11} | {"passive%",8} | "
                        + $"{"x 20-75",7} {"P x3",6} {"P x4",6} {"k>.65",6} | {"bank@20",11} {"bank@40",11} {"bank@60",11} {"bank@75",11} {"bank@85",14}");
        foreach (var p in paths)
        {
            var rows = Owed(p);
            long Owed_(int lo, int hi) => rows.Where(r => r.Level >= lo && r.Level <= hi).Sum(r => r.Cost);
            long total = rows.Sum(r => r.Cost), passive = rows.Sum(r => r.PassiveCost);
            // "x 20-75" = SP earned in 20-75 divided by the SP owed in 20-75: how many kits' worth you earn.
            double ratio = Owed_(20, 75) == 0 ? 0 : Sum(earned, 20, 75) / (double)Owed_(20, 75);
            // The same with the PASSIVE half of 20-75 priced x3 / x4 — his "3-4 times" budget for the split.
            long pas = rows.Where(r => r.Level >= 20 && r.Level <= 75).Sum(r => r.PassiveCost);
            double Split(double k) => Sum(earned, 20, 75) / (double)(Owed_(20, 75) + (k - 1) * pas);
            Console.WriteLine($"  {p.Name,-38} | {Owed_(1, 19),11:N0} {Owed_(20, 39),11:N0} {Owed_(40, 59),11:N0} {Owed_(60, 75),11:N0} {Owed_(76, 85),11:N0}"
                            + $" | {(total == 0 ? 0 : passive / (double)total),8:P0} | {ratio,7:F2} {Split(3),6:F2} {Split(4),6:F2} {(pas == 0 ? 0 : 1 + (Sum(earned, 20, 75) / Target - Owed_(20, 75)) / (double)pas),6:F1} | "
                            + string.Join(" ", new[] { 20, 40, 60, 75 }.Select(at => $"{Bank(earned, rows, at),11:N0}").Append($"{Bank(earned, rows, 85),14:N0}")));
        }
        Console.WriteLine();
        Console.WriteLine("  passive% = the share of the whole kit's SP that is PASSIVES (the part BL-314 would split).");
        Console.WriteLine("  x 20-75 = SP earned in 20-75 / SP the kit costs in 20-75 (>1 = you can buy it all). P x3 / P x4 = the same with");
        Console.WriteLine("  the 20-75 passives priced x3 / x4. k>.65 = the passive multiplier that would land THIS path at 0.65.");

        // (The "his per-archetype k" table that stood here priced the passives ×k by his first guesses. Since the
        //  engine applies the solved k itself (SpScarcity, `BL-314`), "x 20-75" above already IS each path at its k.)

        if (detail is null) return;
        var pick = paths.Where(p => p.Name.Contains(detail, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var p in pick)
        {
            var rows = Owed(p);
            Console.WriteLine();
            Console.WriteLine($"=== {p.Name} — level by level ===");
            Console.WriteLine($"  {"Lvl",4} {"earned",12} {"owed",12} {"of which passive",17} {"rungs",6} {"bank",14}");
            for (int L = 1; L <= max; L++)
            {
                var r = rows.FirstOrDefault(x => x.Level == L);
                if (r is null && L % 5 != 0) continue;
                Console.WriteLine($"  {L,4} {earned[L],12:N0} {r?.Cost ?? 0,12:N0} {r?.PassiveCost ?? 0,17:N0} {r?.Rungs ?? 0,6} {Bank(earned, rows, L),14:N0}");
            }
        }
    }

    /// <summary>The affordability a per-path passive multiplier would aim at: earned / kit over 20-75, at x1. See the
    /// `k>.65` column — the multiplier on that path's 20-75 PASSIVES that lands it here.</summary>
    private const double Target = 0.65;

    private static readonly (int Lo, int Hi)[] Bands = { (1, 19), (20, 39), (40, 59), (60, 75), (76, 85) };

    private static long Sum(long[] a, int lo, int hi)
    {
        long s = 0;
        for (int i = lo; i <= hi && i < a.Length; i++) s += a[i];
        return s;
    }

    /// <summary>SP held on arriving at <paramref name="at"/> after buying every rung that opened at or below it.</summary>
    private static long Bank(long[] earned, List<Row> rows, int at) =>
        Sum(earned, 1, at - 1) - rows.Where(r => r.Level <= at).Sum(r => r.Cost);

    /// <summary>The same with every 20-75 passive rung priced ×<paramref name="k"/>.</summary>
    private static long Bank(long[] earned, List<Row> rows, int at, double k) =>
        Sum(earned, 1, at - 1) - rows.Where(r => r.Level <= at)
            .Sum(r => r.Cost + (r.Level >= 20 && r.Level <= 75 ? (long)((k - 1) * r.PassiveCost) : 0));

    // ================================================================================================================
    // `--sp-budget-csv [dir]` — the same budget priced from the CLASS CSVs instead of the compiled tables. The split
    // (`PassiveSplit.md` §10-§12) lives in the CSVs before the engine learns it, so this is the only place its cost can
    // be measured; pass an old checkout's directory (e.g. `git show 1f90cae:…` into a folder) to compare. It then
    // solves his per-archetype passive multiplier k from his x targets (2026-09-29): the k that lands the archetype's
    // paths, on average, at x = earned(20-75) / kit(20-75) with every 20-75 passive rung priced ×k.
    // ================================================================================================================

    /// <summary>His x targets, 2026-09-29: *"warriors/rogues need more sp to survive as melee, tanks to be effective
    /// defenders, healers with about half so they make decisions, buffers prioritize buffs, archers farm fast enough,
    /// mages need only their main spell"*.</summary>
    private static readonly (string Group, double X, Discipline[] Of)[] Targets =
    {
        ("daggers",      0.65, new[] { Discipline.Nullblade, Discipline.Phantom, Discipline.Venomweaver }),
        ("bows",         0.55, new[] { Discipline.Sharpshooter, Discipline.Trapper, Discipline.Hunter }),
        ("warriors",     0.65, new[] { Discipline.Ravager, Discipline.Warlord }),
        ("tanks",        0.70, new[] { Discipline.Bulwark }),
        ("Magus",        0.60, new[] { Discipline.Magus }),
        ("Lightbringer", 0.55, new[] { Discipline.Lightbringer }),
        ("Warchanter",   0.50, new[] { Discipline.Warchanter }),
    };

    private static string ThirdFile(Discipline d) => d switch
    {
        Discipline.Lightbringer => "healer", Discipline.Warchanter => "buffer", Discipline.Bulwark => "tank",
        Discipline.Magus => "nuker", Discipline.Ravager => "warrior", Discipline.Warlord => "war_aoe",
        Discipline.Sharpshooter or Discipline.Trapper or Discipline.Hunter => "archer",
        _ => "dual",
    };

    private static string SecondFile(Archetype a) => a switch
    {
        Archetype.Tank => "tank", Archetype.Warrior => "warrior", Archetype.Rogue => "rogue",
        Archetype.Nuker => "nuker", _ => "cleric",
    };

    public static void RunCsv(string[] args)
    {
        string dir = args.Length > 1 ? args[1] : "docs/data/classes_skills_csv";
        int max = ExpCurve.MaxLevel;
        var earned = new long[max + 2];
        for (int L = 1; L <= max; L++)
            earned[L] = (long)Math.Round(ExpCurve.ExpToNext(L) * (double)ExpCurve.SpToExpRatio);
        long e2075 = Sum(earned, 20, 75);

        Console.WriteLine($"=== BL-314 · SP BUDGET PRICED FROM THE CSVs ({dir}) — x1 rates ===");
        Console.WriteLine($"  earned 20-75 = {e2075:N0}.  A = actives 20-75, P = passives 20-75 (CSV price), x1 = earned / (A+P).");
        Console.WriteLine("  k = the passive multiplier that lands the path at its target: x = earned / (A + k·P).");
        Console.WriteLine();
        Console.WriteLine($"  {"path",-38} | {"A 20-75",12} {"P 20-75",12} {"P%",4} | {"x1",5} {"target",6} {"k path",6} | {"k grp",6} {"x",5} {"1st short",9} {"by",13}");
        foreach (var (group, x, discs) in Targets)
        {
            var paths = new List<(string Name, Race Race, List<Row> Rows)>();
            foreach (var sc in ClassCatalog.Playable)
            {
                var (a, b) = Disciplines.Of(sc.Race, sc.Archetype);
                foreach (var d in b is { } bb ? new[] { a, bb } : new[] { a })
                    if (discs.Contains(d))
                        paths.Add(($"{ClassNames.Third(d, sc.Race)} ({sc.Race} {d})", sc.Race, CsvOwed(dir, sc.Race, sc.Base, sc.Archetype, d)));
            }
            double KOf(List<Row> rows)
            {
                long all = rows.Where(r => r.Level is >= 20 and <= 75).Sum(r => r.Cost);
                long pas = rows.Where(r => r.Level is >= 20 and <= 75).Sum(r => r.PassiveCost);
                return pas == 0 ? 1 : (e2075 / x - (all - pas)) / pas;
            }
            double kGroup = paths.Average(p => KOf(p.Rows));
            foreach (var (name, _, rows) in paths)
            {
                long all = rows.Where(r => r.Level is >= 20 and <= 75).Sum(r => r.Cost);
                long pas = rows.Where(r => r.Level is >= 20 and <= 75).Sum(r => r.PassiveCost);
                double xAt = e2075 / (all + (kGroup - 1) * pas);
                int at = Enumerable.Range(20, 56).FirstOrDefault(L => Bank(earned, rows, L, kGroup) < 0);
                long low = at == 0 ? 0 : Bank(earned, rows, at, kGroup);
                Console.WriteLine($"  {name,-38} | {all - pas,12:N0} {pas,12:N0} {(all == 0 ? 0 : pas / (double)all),4:P0} | "
                                + $"{e2075 / (double)all,5:F2} {x,6:F2} {KOf(rows),6:F2} | {kGroup,6:F2} {xAt,5:F2} "
                                + $"{(at == 0 ? "-" : at.ToString()),9} {low,13:N0}");
            }
            Console.WriteLine($"  {"→ " + group + " k",-38} | {kGroup:F2}");
        }
        Console.WriteLine("  k grp = the mean of the group's path k; x = each path's affordability at it. 1st short = the first level where");
        Console.WriteLine("  buying everything the moment it opens (20-75 passives at ×k grp) runs you out of SP; by = how far short.");
    }

    /// <summary>One path's owed SP per level, read off its CSV files: 1st + 2nd + 3rd + 4th (+ shared 4th), rows whose
    /// RACE is blank or names this race. SP COST honours `(x1000)` in the header and `k`/`kk` suffixes, like
    /// SkillCsvSeed. Of each exclusive group (looked up in the catalog) only the dearest member counts.</summary>
    private static List<Row> CsvOwed(string dir, Race race, BaseClass bc, Archetype arch, Discipline d)
    {
        var files = new[] { bc == BaseClass.Fighter ? "fighter 1st" : "mage 1st", SecondFile(arch) + " 2nd",
                            ThirdFile(d) + " 3rd", ThirdFile(d) + " 4th", "shared 4th" };
        var rungs = new List<(string Id, int Level, long Sp, bool Passive)>();
        foreach (var f in files)
        {
            string path = System.IO.Path.Combine(dir, f + ".csv");
            if (!File.Exists(path)) continue;
            int sp = -1, rc = -1; double scale = 1;
            foreach (var line in File.ReadLines(path))
            {
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) break;
                var c = SplitCsv(line);
                if (line.StartsWith("LEARN"))
                {
                    sp = c.FindIndex(h => h.Trim().StartsWith("SP COST", StringComparison.OrdinalIgnoreCase));
                    rc = c.FindIndex(h => h.Trim().Equals("RACE", StringComparison.OrdinalIgnoreCase));
                    if (line.Contains("(x1000)", StringComparison.OrdinalIgnoreCase)) scale = 1000;
                    continue;
                }
                if (sp < 0 || c.Count <= sp || !int.TryParse(c[0].Trim(), out int lvl) || c[2].Trim().Length == 0) continue;
                string r = rc >= 0 && rc < c.Count ? c[rc].Trim() : "";
                if (r.Length > 0 && !r.Split(';').Any(x => x.Trim().Equals(race.ToString(), StringComparison.OrdinalIgnoreCase)))
                    continue;
                rungs.Add((c[2].Trim(), lvl, (long)Math.Round(Price(c[sp]) * scale),
                           c[3].Contains("passive", StringComparison.OrdinalIgnoreCase)));
            }
        }
        var drop = new HashSet<string>();
        foreach (var g in rungs.Where(v => (SkillCatalog.Get(v.Id)?.ExclusiveGroup ?? "").Length > 0)
                               .GroupBy(v => SkillCatalog.Get(v.Id)!.ExclusiveGroup))
            foreach (var loser in g.GroupBy(v => v.Id).OrderByDescending(s => s.Sum(v => v.Sp)).Skip(1))
                drop.Add(loser.Key);
        return rungs.Where(v => !drop.Contains(v.Id)).GroupBy(v => v.Level)
            .Select(g => new Row(g.Key, g.Sum(v => v.Sp), g.Where(v => v.Passive).Sum(v => v.Sp), g.Count()))
            .OrderBy(r => r.Level).ToList();
    }

    private static double Price(string s)
    {
        s = s.Trim();
        int k = 0;
        while (s.Length > 0 && (s[^1] == 'k' || s[^1] == 'K')) { k++; s = s[..^1].TrimEnd(); }
        if (!double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)) return 0;
        return v * Math.Pow(1000, k);
    }

    /// <summary>A CSV line honouring double quotes (his descriptions are full of commas).</summary>
    private static List<string> SplitCsv(string line)
    {
        var outp = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool q = false;
        foreach (char ch in line)
        {
            if (ch == '"') q = !q;
            else if (ch == ',' && !q) { outp.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        outp.Add(sb.ToString());
        return outp;
    }

    private static List<Row> Owed(Path p)
    {
        var rungs = new Dictionary<(string, int), (int Level, int Cost, SkillDef Def)>();
        void Take(IEnumerable<ClassSkill> list)
        {
            foreach (var cs in list)
            {
                var def = SkillCatalog.Get(cs.SkillId);
                if (def is null) continue;
                var key = (cs.SkillId, cs.SkillLevel);
                int cost = cs.SpCostFor(def);
                if (!rungs.TryGetValue(key, out var have) || cs.LearnLevel < have.Level)
                    rungs[key] = (cs.LearnLevel, cost, def);
            }
        }
        Take(ClassSkills.ForClass(p.Race, p.Base, null, null));
        Take(ClassSkills.Cumulative(p.Race, p.Base, p.Arch, p.Disc, fourth: true));

        // Pick-one sets: keep only the dearest member of each exclusive group.
        var drop = new HashSet<string>();
        foreach (var g in rungs.Values.Where(v => v.Def.ExclusiveGroup.Length > 0).GroupBy(v => v.Def.ExclusiveGroup))
        {
            var totals = g.GroupBy(v => v.Def.Id).Select(s => (Id: s.Key, Sp: s.Sum(v => (long)v.Cost))).OrderByDescending(s => s.Sp).ToList();
            foreach (var loser in totals.Skip(1)) drop.Add(loser.Id);
        }

        return rungs.Values.Where(v => !drop.Contains(v.Def.Id))
            .GroupBy(v => v.Level)
            .Select(g => new Row(g.Key, g.Sum(v => (long)v.Cost),
                                 g.Where(v => v.Def.Category == SkillCategory.Passive).Sum(v => (long)v.Cost), g.Count()))
            .OrderBy(r => r.Level).ToList();
    }
}
