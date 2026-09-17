using System.Diagnostics;
using Game.Shared;

/// <summary>
/// THE DROP LOOKUP — *"i say what im looking for and it shows me all mob_name / [mob_lvl - elite|boss|
/// normal] / location / drop_rate"* (owner, 2026-09-16, `BL-253`).
///
/// <para>`dotnet run --project tools/BalanceMatrix -- --drops &lt;text&gt;` answers "where does this come
/// from" for any item, by name or by id, substring, case-insensitive.</para>
///
/// ── This file is now a PRINTER and nothing else ────────────────────────────────────────────────
/// 🔑 THE WALK MOVED INTO <see cref="DropIndex"/> (Game.Shared, 0.168.0). It used to live here, and the
/// in-game window he asked for would have been a second copy of it — with the recipe-book roll a *third*
/// copy, reconstructed by hand from `RollBossBonus` and free to drift from it. One walk, three readers:
/// this tool, the server's cached index, and the client window it feeds.
///
/// Everything the old header argued is still true and now lives on `DropIndex`: it walks SPAWNS rather
/// than templates because rank is a property of the spawn; every chance goes through
/// `MobCatalog.EffectiveChance` so the number printed is the number rolled; and the boss MAT PILE, which
/// takes no rate knob at all, is marked so it is never shown scaled.
/// </summary>
internal static class DropFinder
{
    public static void Run(string[] args)
    {
        string query = args.Length > 1 ? string.Join(' ', args[1..]).Trim() : "";
        if (query.Length == 0)
        {
            Console.WriteLine("usage: --drops <item name or id>   e.g. --drops \"greater scroll\", --drops epic_wood");
            return;
        }

        var sw = Stopwatch.StartNew();
        var index = DropIndex.Build();
        sw.Stop();

        var hits = DropIndex.Find(index, query);
        if (hits.Count == 0)
        {
            Console.WriteLine($"\nNothing in the world drops anything matching \"{query}\".");
            Console.WriteLine("(Check the spelling, then check whether it is craft-only or vendor-only —");
            Console.WriteLine(" this tool reports MOB sources and nothing else.)");
            return;
        }

        Console.WriteLine($"\n═══ WHAT DROPS \"{query}\" ═══");
        Console.WriteLine("Chance is PER KILL, with every live rate knob applied (global × group × item).");
        // ⚠ The version stamp and the content hash are GONE (0.171.0, his ruling on `BL-253`: *"if drop
        // indexes are build even after x10 more mobs still faster than reading a file, build each
        // restart"*). There is no cache to identify any more — the build time below is the whole story.
        // This line still quoted them and had stopped compiling; the tool is outside `Game.sln`, so
        // nothing caught it until the shelf work rebuilt it (`BL-163`).
        Console.WriteLine($"(index: {index.Sources.Count} rows, built in {sw.ElapsedMilliseconds} ms, "
                        + "never cached — a restart is the rebuild)\n");

        foreach (var group in hits.GroupBy(h => h.ItemId))
        {
            var rows = group.ToList();
            Console.WriteLine($"── {DropIndex.NameOf(group.Key)}  [{group.Key}]  — {rows.Count} source(s)");
            Console.WriteLine($"   {"creature",-28} {"lvl",-7} {"rank",-6} {"where",-26} {"per kill",10}  note");
            foreach (var r in rows)
                Console.WriteLine($"   {Trim(r.MobName, 28),-28} {Level(r),-7} {Rank(r.Rank),-6} "
                                + $"{Trim(r.Location, 26),-26} {Pct(DropIndex.ChanceFor(r)),10}  {Note(r)}");
            Console.WriteLine();
        }
    }

    private static string Level(DropSource s) =>
        s.MinLevel == s.MaxLevel ? s.MinLevel.ToString() : $"{s.MinLevel}-{s.MaxLevel}";

    /// <summary>The two things a row may need said about it: that its chance is only paid at part of the
    /// band, and that it is the rate-free boss pile rather than a drop table row.</summary>
    private static string Note(DropSource s)
    {
        var bits = new List<string>();
        if (s.MinLevel != s.MaxLevel && s.BestLevel != s.MinLevel) bits.Add($"only from level {s.BestLevel}");
        if (s.IgnoresRates) bits.Add($"boss pile, x{s.MinQty}-{s.MaxQty}, NO rate knobs");
        else if (s.MaxQty > 1) bits.Add($"x{s.MinQty}-{s.MaxQty}");
        return string.Join("; ", bits);
    }

    private static string Rank(MobRank r) => r switch
    {
        MobRank.Boss => "BOSS",
        MobRank.Elite => "elite",
        _ => "normal",
    };

    /// <summary>A per-kill chance in the unit a player reads it in. Above 100% it is COPIES per kill
    /// (MobCatalog.DropCopies), which is what a high server rate means everywhere else.</summary>
    private static string Pct(float c) =>
        c >= 1f ? $"×{c:0.##}"
        : c >= 0.01f ? $"{c * 100f:0.##}%"
        : c >= 0.0001f ? $"{c * 100f:0.####}%"
        : $"{c * 100f:0.000000}%";

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
