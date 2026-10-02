using System.Globalization;
using System.Text;
using Game.Shared;

// =====================================================================================================
//  `--reprice-sp` — `BL-334`: EVERY SKILL BELOW 76 IS PRICED BY ONE FORMULA OFF THE EXP CURVE (owner, 2026-10-02).
//
//  His why: *"I want the skills to have weight but then again I want each time skills to be with rising SP ... Not lvl
//  35 buff to cost 45k 40 to cost 68k and 44 to cost 36k ... I want to go up."* The `BL-326` pot split (one pot per
//  level, divided by weight) made a crowded level cheaper per skill, which left 365 falling steps across the ladders.
//
//  THE RULE.   price(row) = weight(skill) × SP income at its learn level × c(file, level)
//    - SP income at L = ExpCurve.ExpToNext(L) × SpToExpRatio: the SP one level of kills pays. It is read from the code,
//      so an EXP change moves every price with it on the next run (his condition for keeping SP at 1/20 of EXP).
//    - weight = docs/data/sp_weights.csv (his), the same file the pot split used.
//    - c = a smooth curve per class FILE through one anchor per level band (geometric interpolation between the band
//      middles), SOLVED ON EVERY RUN so each band the file owns lands on its affordability target
//      (docs/data/sp_bands.csv, × the file's ±% in docs/data/sp_adj.csv). Nothing is stored: his ruling, 2026-10-02,
//      *"freeze is not required (no files no nothing just formula) ... each skill change will fix the class sp cost and
//      class won't move from its x"*. So adding twelve skills to a class makes each of its skills cheaper, never the
//      class dearer. (0.225.0 froze the anchors in a file for one build; he retired it the same day.)
//    - THE FLOOR: a rung costs at least ×1.01 of the rung before it on the same ladder (same skill, same race, along
//      the 1st → 2nd → 3rd path), his "floor the skill to at least 1% more from its level before". The formula is
//      smooth, so the floor only catches the small bends where an EXP wall meets a falling c.
//
//  Only learn levels 1-75 are priced here; 76+ is a separate discussion (his ruling) and its cells are left alone.
//  A row priced 0 is an auto-grant and stays free, so a new skill needs ANY number in its SP cell to be priced.
// =====================================================================================================

internal static partial class PassiveGen
{
    private const int SpCurveTop = 75;
    private const double LadderFloor = 1.01;

    private sealed record Band(int From, int To, double X)
    {
        public double Mid => (From + To) / 2.0;
    }

    private sealed class CurveRow
    {
        public string File = "";
        public double Adj;                       // his ±%, applied to the targets of the bands this file owns
        public double?[] C = Array.Empty<double?>();   // the anchors, solved fresh every run
    }

    private static string BandsPath(string csvDir) => Path.Combine(csvDir, "..", "sp_bands.csv");
    private static string AdjPath(string csvDir) => Path.Combine(csvDir, "..", "sp_adj.csv");

    private static int Tier(string file) => file.EndsWith("1st") ? 1 : file.EndsWith("2nd") ? 2 : file.EndsWith("3rd") ? 3 : 4;

    /// <summary>The 1st → 2nd → 3rd chains. A path is what one character of one race learns below 76.</summary>
    private static readonly (string First, string Second, string Third)[] Chains =
    {
        ("fighter 1st", "tank 2nd",    "tank 3rd"),
        ("fighter 1st", "warrior 2nd", "warrior 3rd"),
        ("fighter 1st", "warrior 2nd", "war_aoe 3rd"),
        ("fighter 1st", "rogue 2nd",   "dual 3rd"),
        ("fighter 1st", "rogue 2nd",   "archer 3rd"),
        ("mage 1st",    "nuker 2nd",   "nuker 3rd"),
        ("mage 1st",    "cleric 2nd",  "healer 3rd"),
        ("mage 1st",    "cleric 2nd",  "buffer 3rd"),
    };

    /// <summary>The files a row of <paramref name="file"/> takes its ladder floor from (itself included).</summary>
    private static string[] Predecessors(string file) => Tier(file) switch
    {
        1 => new[] { file },
        2 => new[] { Chains.First(c => c.Second == file).First, file },
        _ => Chains.Where(c => c.Third == file).Select(c => new[] { c.First, c.Second, file }).First(),
    };

    /// <summary>Does this file OWN the band (set its target), or only borrow a curve there? 1st files own 1-19, 2nd
    /// files 20-39, 3rd files 40-75. A 1st file's racial rows above 19 borrow the mean of the files that own the band.</summary>
    private static bool Owns(string file, Band b) => Tier(file) switch
    {
        1 => b.To <= 19,
        2 => b.From >= 20 && b.To <= 39,
        _ => b.From >= 40,
    };

    private static double SpIncome(int level) => ExpCurve.ExpToNext(level) * (double)ExpCurve.SpToExpRatio;

    // ---- the two data files ----------------------------------------------------------------------------------

    private static List<Band> LoadBands(string csvDir, List<string> errors)
    {
        var bands = new List<Band>();
        string path = BandsPath(csvDir);
        if (!File.Exists(path)) { errors.Add("docs/data/sp_bands.csv is missing"); return bands; }
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            var c = SplitCsv(line);
            if (c.Count < 3 || c[0].Trim().Length == 0) continue;
            if (int.TryParse(c[0].Trim(), out int from) && int.TryParse(c[1].Trim(), out int to)
                && double.TryParse(c[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x) && x > 0)
                bands.Add(new Band(from, to, x));
            else errors.Add($"sp_bands.csv: cannot read '{line}' (FROM,TO,X)");
        }
        bands.Sort((a, b) => a.From.CompareTo(b.From));
        for (int i = 0; i < bands.Count; i++)
            if (bands[i].From != (i == 0 ? 1 : bands[i - 1].To + 1)) errors.Add($"sp_bands.csv: band {bands[i].From}-{bands[i].To} leaves a gap or overlaps");
        if (bands.Count > 0 && bands[^1].To != SpCurveTop) errors.Add($"sp_bands.csv: the last band must end at {SpCurveTop}");
        return bands;
    }

    private static Dictionary<string, CurveRow> LoadCurve(string csvDir, List<Band> bands, List<string> errors)
    {
        var curve = new Dictionary<string, CurveRow>(StringComparer.Ordinal);
        foreach (var fk in Files.Where(f => Tier(f.File) <= 3))
            curve[fk.File] = new CurveRow { File = fk.File, C = new double?[bands.Count] };
        string path = AdjPath(csvDir);
        if (!File.Exists(path)) return curve;   // no file = every ADJ 0
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            var c = SplitCsv(line).Select(x => x.Trim()).ToList();
            if (c.Count < 2 || c[0].Length == 0) continue;
            if (!curve.TryGetValue(c[0], out var row)) { errors.Add($"sp_adj.csv: no class file named '{c[0]}'"); continue; }
            if (c[1].Length > 0 && !double.TryParse(c[1].TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out row.Adj))
                errors.Add($"sp_adj.csv: {c[0]} ADJ '{c[1]}' is not a number");
        }
        return curve;
    }

    /// <summary>c(file, level): geometric interpolation between the file's anchors at the band middles, flat past the
    /// first and last anchor.</summary>
    private static double CurveAt(CurveRow r, List<Band> bands, int level)
    {
        var pts = bands.Select((b, i) => (b.Mid, C: r.C[i])).Where(p => p.C is > 0).Select(p => (p.Mid, C: p.C!.Value)).ToList();
        if (pts.Count == 0) return 0;
        if (level <= pts[0].Mid) return pts[0].C;
        if (level >= pts[^1].Mid) return pts[^1].C;
        for (int i = 0; i < pts.Count - 1; i++)
            if (level <= pts[i + 1].Mid)
            {
                double t = (level - pts[i].Mid) / (pts[i + 1].Mid - pts[i].Mid);
                return Math.Exp(Math.Log(pts[i].C) + t * (Math.Log(pts[i + 1].C) - Math.Log(pts[i].C)));
            }
        return pts[^1].C;
    }

    // ---- pricing ---------------------------------------------------------------------------------------------

    /// <summary>The rows this formula prices: learn level 1-75, priced (SP above 0) and carried by the class table for
    /// every race they name — the same set the pot split used.</summary>
    private static List<PriceRow> CurveRows(List<PriceRow> rows) =>
        rows.Where(r => r.Sp > 0 && r.Level <= SpCurveTop && Tier(r.Fk.File) <= 3
                     && r.Races.All(race => KeyFor(r.Fk, race, r.Id, r.Level) is not null)).ToList();

    /// <summary>Every row's price under the given anchors, with the ×1.01 ladder floor.</summary>
    private static Dictionary<PriceRow, int> PriceAll(List<PriceRow> rows, Dictionary<string, double> weights,
                                                      List<Band> bands, Dictionary<string, CurveRow> curve)
    {
        var price = new Dictionary<PriceRow, int>();
        var byId = new Dictionary<string, List<(PriceRow Row, int Sp)>>(StringComparer.Ordinal);
        foreach (var r in rows.OrderBy(r => Tier(r.Fk.File)).ThenBy(r => r.Level))
        {
            double raw = weights[r.Id] * SpIncome(r.Level) * CurveAt(curve[r.Fk.File], bands, r.Level);
            var pre = Predecessors(r.Fk.File);
            if (!byId.TryGetValue(r.Id, out var seen)) byId[r.Id] = seen = new();
            long floor = 0;
            foreach (var (p, sp) in seen)
                if (p.Level < r.Level && sp > floor && pre.Contains(p.Fk.File) && p.Races.Intersect(r.Races).Any())
                    floor = sp;
            int v = Nice(raw);
            if (floor > 0 && v < floor * LadderFloor) v = NiceUp(floor * LadderFloor);
            price[r] = Math.Max(1, v);
            seen.Add((r, price[r]));
        }
        return price;
    }

    /// <summary><see cref="Nice"/>, but never below <paramref name="v"/>.</summary>
    private static int NiceUp(double v)
    {
        if (v < 1000) return (int)Math.Ceiling(v);
        double step = Math.Pow(10, Math.Floor(Math.Log10(v)) - 2);
        return (int)(Math.Ceiling(v / step) * step);
    }

    /// <summary>Per path (race × chain), the SP earned in a band over what the band's rows cost: x above 1 = you can buy
    /// all of it.</summary>
    private static List<(Race Race, (string First, string Second, string Third) Chain, double[] X)> PathX(
        List<PriceRow> rows, Dictionary<PriceRow, int> price, List<Band> bands)
    {
        var earned = bands.Select(b => Enumerable.Range(b.From, b.To - b.From + 1).Sum(SpIncome)).ToArray();
        var list = new List<(Race, (string, string, string), double[])>();
        foreach (var chain in Chains)
            foreach (var race in Races)
            {
                var files = new[] { chain.First, chain.Second, chain.Third };
                var mine = rows.Where(r => files.Contains(r.Fk.File) && r.Races.Contains(race)).ToList();
                var x = new double[bands.Count];
                for (int b = 0; b < bands.Count; b++)
                {
                    long cost = mine.Where(r => r.Level >= bands[b].From && r.Level <= bands[b].To).Sum(r => (long)price[r]);
                    x[b] = cost > 0 ? earned[b] / cost : double.NaN;
                }
                list.Add((race, chain, x));
            }
        return list;
    }

    /// <summary>Fits every file's anchors so every band it
    /// own lands on its target × (1 + ADJ). Starts from 1 every run, so the result depends only on the data.</summary>
    private static void Solve(List<PriceRow> rows, Dictionary<string, double> weights, List<Band> bands,
                              Dictionary<string, CurveRow> curve)
    {
        foreach (var (f, r) in curve)
                for (int b = 0; b < bands.Count; b++)
                    if (Owns(f, bands[b]) && rows.Any(x => x.Fk.File == f && x.Level >= bands[b].From && x.Level <= bands[b].To))
                        r.C[b] = 1;
        void Borrow()   // a 1st file's anchors in the bands it does not own: the geometric mean of the owners
        {
            foreach (var first in curve.Values.Where(r => Tier(r.File) == 1))
                for (int b = 0; b < bands.Count; b++)
                {
                    if (Owns(first.File, bands[b])) continue;
                    var own = Chains.Where(c => c.First == first.File).SelectMany(c => new[] { c.Second, c.Third }).Distinct()
                                    .Select(f => curve[f].C[b]).Where(v => v is > 0).Select(v => Math.Log(v!.Value)).ToList();
                    first.C[b] = own.Count > 0 && rows.Any(x => x.Fk.File == first.File && x.Level >= bands[b].From && x.Level <= bands[b].To)
                        ? Math.Exp(own.Average()) : null;
                }
        }
        Borrow();
        for (int it = 0; it < 80; it++)
        {
            var price = PriceAll(rows, weights, bands, curve);
            var px = PathX(rows, price, bands);
            foreach (var (f, r) in curve)
            {
                for (int b = 0; b < bands.Count; b++)
                {
                    if (!Owns(f, bands[b]) || r.C[b] is null) continue;
                    var xs = px.Where(p => (Tier(f) == 1 ? p.Chain.First : Tier(f) == 2 ? p.Chain.Second : p.Chain.Third) == f)
                               .Select(p => p.X[b]).Where(x => !double.IsNaN(x)).Select(x => Math.Log(x)).ToList();
                    if (xs.Count == 0) continue;
                    double target = bands[b].X * (1 + r.Adj / 100.0);
                    r.C[b] *= Math.Pow(Math.Exp(xs.Average()) / target, 0.7);
                }
            }
            Borrow();
        }
        foreach (var r in curve.Values)
            for (int b = 0; b < r.C.Length; b++)
                if (r.C[b] is { } v) r.C[b] = double.Parse(v.ToString("G4", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    // ---- the commands ----------------------------------------------------------------------------------------

    /// <summary>`--reprice-sp [--show file]`.</summary>
    public static int Reprice(string csvDir, string repoRoot, string? show)
    {
        var errors = new List<string>();
        var all = ReadPriceRows(csvDir);
        var weights = LoadWeights(csvDir, all, errors);
        var bands = LoadBands(csvDir, errors);
        if (errors.Count > 0) return Fail(errors);
        var curve = LoadCurve(csvDir, bands, errors);
        if (errors.Count > 0) return Fail(errors);
        var rows = CurveRows(all).Where(r => weights.ContainsKey(r.Id)).ToList();

        Solve(rows, weights, bands, curve);

        var price = PriceAll(rows, weights, bands, curve);
        foreach (var r in rows)   // within 0.5% of the cell already there = the same price, so a rerun never flips cells
            if (Math.Abs(price[r] - r.Sp) <= 0.005 * r.Sp) price[r] = (int)r.Sp;

        if (show is not null)
            foreach (var g in rows.Where(r => r.Fk.File.Contains(show, StringComparison.OrdinalIgnoreCase))
                                  .GroupBy(r => (r.Fk.File, r.Level)).OrderBy(g => g.Key.File).ThenBy(g => g.Key.Level))
            {
                Console.WriteLine($"  {g.Key.File} @{g.Key.Level}: income {SpIncome(g.Key.Level):N0}, c {CurveAt(curve[g.Key.File], bands, g.Key.Level):0.###}");
                foreach (var r in g)
                    Console.WriteLine($"      {r.Id,-34} w {weights[r.Id],4:0.##}  {r.Sp,12:N0} → {price[r],12:N0}  {(r.Races.Length == 3 ? "" : string.Join(";", r.Races))}");
            }

        PrintPathX(rows, price, bands);
        int changed = WriteSpCells(csvDir, price);
        Console.WriteLine($"{changed} SP cell(s) rewritten at 1-{SpCurveTop}.");
        return Run(csvDir, repoRoot, false);
    }

    private static void PrintPathX(List<PriceRow> rows, Dictionary<PriceRow, int> price, List<Band> bands)
    {
        Console.WriteLine($"  {"path",-36} " + string.Join(" ", bands.Select(b => $"{b.From + "-" + b.To,9}")) + "   (x = SP earned / kit cost; target)");
        Console.WriteLine($"  {"",-36} " + string.Join(" ", bands.Select(b => $"{b.X,9:0.00}")));
        foreach (var (race, chain, x) in PathX(rows, price, bands))
            Console.WriteLine($"  {race + " " + chain.Third,-36} " + string.Join(" ", x.Select(v => double.IsNaN(v) ? $"{"-",9}" : $"{v,9:0.00}")));
    }

    /// <summary>`--check`'s half: every 1-75 cell must equal the formula, solved fresh (STALE otherwise — the bands,
    /// curve, the weights or the EXP table moved and nobody repriced), and no ladder may fall.</summary>
    public static int CheckSp(string csvDir)
    {
        var errors = new List<string>();
        var all = ReadPriceRows(csvDir);
        var bands = LoadBands(csvDir, errors);
        var curve = LoadCurve(csvDir, bands, errors);
        var weights = ReadWeightsOnly(csvDir);
        var rows = CurveRows(all);
        foreach (var id in rows.Select(r => r.Id).Distinct().Where(id => !weights.ContainsKey(id)))
            errors.Add($"SP: {id} has no row in sp_weights.csv — run `--reprice-sp`");
        if (errors.Count == 0)
        {
            Solve(rows, weights, bands, curve);
            var price = PriceAll(rows, weights, bands, curve);
            int stale = rows.Count(r => Math.Abs(price[r] - r.Sp) > 0.005 * r.Sp);
            if (stale > 0)
            {
                var r0 = rows.First(r => Math.Abs(price[r] - r.Sp) > 0.005 * r.Sp);
                errors.Add($"SP STALE: {stale} cell(s) at 1-{SpCurveTop} differ from the formula (first: {r0.Fk.File} {r0.Id} @{r0.Level} " +
                           $"{r0.Sp:N0}, formula {price[r0]:N0}) — run `SkillCsvSeed -- --reprice-sp`");
            }
        }
        var falls = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var chain in Chains)
            foreach (var race in Races)
            {
                var files = new[] { chain.First, chain.Second, chain.Third };
                foreach (var g in rows.Where(r => files.Contains(r.Fk.File) && r.Races.Contains(race)).GroupBy(r => r.Id))
                {
                    var s = g.OrderBy(r => r.Level).ToList();
                    for (int i = 1; i < s.Count; i++)
                        if (s[i].Sp < s[i - 1].Sp)
                            falls.Add($"SP FALLS: {g.Key} ({race}) {s[i - 1].Fk.File} @{s[i - 1].Level} {s[i - 1].Sp:N0} → {s[i].Fk.File} @{s[i].Level} {s[i].Sp:N0}");
                }
            }
        errors.AddRange(falls);
        foreach (var e in errors) Console.WriteLine("  🔴 " + e);
        if (errors.Count == 0) Console.WriteLine($"✅ SP: every 1-{SpCurveTop} price matches the curve and every ladder rises.");
        return errors.Count == 0 ? 0 : 1;
    }

    private static Dictionary<string, double> ReadWeightsOnly(string csvDir)
    {
        var w = new Dictionary<string, double>(StringComparer.Ordinal);
        string path = WeightsPath(csvDir);
        if (!File.Exists(path)) return w;
        foreach (var line in File.ReadAllLines(path).Skip(1))
        {
            var c = SplitCsv(line);
            if (c.Count >= 4 && double.TryParse(c[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0)
                w[c[0].Trim()] = v;
        }
        return w;
    }
}
