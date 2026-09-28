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
