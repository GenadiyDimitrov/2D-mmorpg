using System.Globalization;
using System.Text;
using Game.Shared;

// =====================================================================================================
//  `--reweigh-sp` — `BL-326`: EACH LEVEL'S SP IS ONE POT, SPLIT BY WEIGHT (owner, 2026-09-29).
//
//  His why: *"not one active skill to cost 880k SP and one passive that give me +0.1mp regen to cost 2600k ...
//  sum all the sp/lvl and split it for skills as weighted ... active skills are x1 passives should be less"*.
//  It replaced the passive ×k (`SpScarcity`, 0.215.0), which made every 20-75 passive rung 3.5-9× an active one.
//
//  THE RULE. For one class file, one level and one race, the pot is the sum of the SP cells of every row that race
//  learns there. Each row takes  pot × its weight / the sum of the weights. So the pot is conserved (the kit costs
//  what it cost, and his affordability targets still hold) and only the split moves. A race-only row counts by the
//  share of races that learn it, which for a symmetric kit is each race's own pot, and makes a rerun a fixed point.
//  His worked example, Human archer at 60: pot 2,868k over ten skills; three light pieces at 0.33, three at 1, four
//  strikes at 1.5 → 95k / 287k / 430k (built: 94.7k / 287k / 431k).
//
//  THE WEIGHTS ARE HIS: `docs/data/sp_weights.csv`, one row per skill id. A skill not in the file gets a default
//  (passive 0.33, buff/utility 1, damage/debuff/heal/trap 1.5) and is ADDED to the file as `default`, so a new skill
//  shows up there for him to price. Setting every weight to 1 is his "just the sum divided by the count".
//
//  Rows the class tables do not carry (the central race blocks, auto-granted SP-0 rows) keep their price and stay out
//  of the pot. After rewriting the cells it regenerates (`--gen-passives`), which also writes the price table the
//  engine loads (`ClassSkillTables.SpPrices.g.cs`). Rerunning it is harmless: same pot, same weights, same split.
// =====================================================================================================

internal static partial class PassiveGen
{
    private sealed record PriceRow(FileKey Fk, int LineIndex, int Level, string Id, string Name, Race[] Races, long Sp);

    private static BaseClass BaseOf(FileKey fk, Archetype? a) =>
        fk.Call.Contains("BaseClass.Mage") || a is Archetype.Nuker or Archetype.Healer ? BaseClass.Mage : BaseClass.Fighter;

    private static bool Fourth(FileKey fk) => fk.File.EndsWith("4th");

    /// <summary>The class-table row a CSV row is priced into for one race, or null when the tables do not carry it
    /// (a central injector or an auto-grant: its price is not the class table's to set).</summary>
    private static ClassSkills.ClassKey? KeyFor(FileKey fk, Race race, string id, int level)
    {
        var (a, d) = fk.Key(race);
        if (fk.File.EndsWith("3rd") || Fourth(fk)) { if (d is null) return null; }
        var b = BaseOf(fk, a);
        bool f = Fourth(fk);
        foreach (var cs in ClassSkills.ForClass(race, b, a, d, f))
            if (cs.SkillId == id && cs.LearnLevel == level) return new ClassSkills.ClassKey(race, b, a, d, f);
        return null;
    }

    /// <summary>Every priced row of every class file, with the cell layout needed to rewrite it.</summary>
    private static List<PriceRow> ReadPriceRows(string csvDir)
    {
        var rows = new List<PriceRow>();
        foreach (var fk in Files)
        {
            var lines = File.ReadAllLines(Path.Combine(csvDir, fk.File + ".csv"));
            int spCol = -1, raceCol = -1; double scale = 1;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) break;
                var c = SplitCsv(line);
                if (line.StartsWith("LEARN"))
                {
                    var h = c.Select(x => x.Trim().ToUpperInvariant()).ToList();
                    spCol = h.FindIndex(x => x.StartsWith("SP COST"));
                    raceCol = h.FindIndex(x => x == "RACE");
                    scale = h[spCol].Contains("X1000") ? 1000 : 1;
                    continue;
                }
                if (c.Count < 15 || !int.TryParse(c[0].Trim(), out int lvl)) continue;
                string raceCell = raceCol >= 0 && raceCol < c.Count ? c[raceCol].Trim() : "";
                var races = raceCell.Length == 0 ? Races
                    : raceCell.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                              .Select(x => Enum.Parse<Race>(x, true)).ToArray();
                rows.Add(new PriceRow(fk, li, lvl, c[2].Trim(), c[1].Trim(), races,
                                      (long)Math.Round(Price(spCol < c.Count ? c[spCol] : "") * scale)));
            }
        }
        return rows;
    }

    // ---- the weights file ------------------------------------------------------------------------------------

    private static string WeightsPath(string csvDir) => Path.Combine(csvDir, "..", "sp_weights.csv");

    /// <summary>A skill's weight when he has not given one.</summary>
    private static double DefaultWeight(SkillDef def) => def.Category switch
    {
        SkillCategory.Passive => 0.33,
        SkillCategory.Buff => 1.0,
        SkillCategory.Debuff or SkillCategory.Heal => 1.5,
        _ => def.PowerAt(1) > 0 || def.PlacesTrap || def.ChannelSkill is not null ? 1.5 : 1.0,
    };

    private static string KindOf(SkillDef def) => def.Category == SkillCategory.Passive ? "passive" : def.Category.ToString().ToLowerInvariant();

    /// <summary>Read `sp_weights.csv` (SKILL_ID,NAME,KIND,WEIGHT,SOURCE), add a `default` row for every priced skill
    /// it lacks, and write it back sorted by id.</summary>
    private static Dictionary<string, double> LoadWeights(string csvDir, List<PriceRow> rows, List<string> errors)
    {
        string path = WeightsPath(csvDir);
        var lines = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (File.Exists(path))
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var c = SplitCsv(line);
                if (c.Count >= 5 && c[0].Trim().Length > 0) lines[c[0].Trim()] = c.Select(x => x.Trim()).ToArray();
            }
        int added = 0;
        foreach (var r in rows)
        {
            if (lines.ContainsKey(r.Id) || SkillCatalog.Get(r.Id) is not SkillDef def) continue;
            lines[r.Id] = new[] { r.Id, r.Name, KindOf(def), DefaultWeight(def).ToString("0.##", CultureInfo.InvariantCulture), "default" };
            added++;
        }
        var w = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (id, c) in lines)
            if (double.TryParse(c[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0) w[id] = v;
            else errors.Add($"sp_weights.csv: {id} has weight '{c[3]}' (must be a number above 0)");
        var sb = new StringBuilder("SKILL_ID,NAME,KIND,WEIGHT,SOURCE\r\n");
        foreach (var c in lines.Values.OrderBy(c => c[0], StringComparer.Ordinal))
            sb.Append(string.Join(",", c.Select(x => x.Contains(',') ? "\"" + x + "\"" : x))).Append("\r\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        if (added > 0) Console.WriteLine($"sp_weights.csv: {added} skill(s) added at their default weight.");
        return w;
    }

    // ---- the reweigh -----------------------------------------------------------------------------------------

    public static int Reweigh(string csvDir, string repoRoot, string? show)
    {
        var rows = ReadPriceRows(csvDir);
        var errors = new List<string>();
        var weights = LoadWeights(csvDir, rows, errors);
        if (errors.Count > 0) return Fail(errors);

        // In the pot = priced (SP > 0) and carried by the class table for every race it names.
        var potRows = rows.Where(r => r.Sp > 0 && weights.ContainsKey(r.Id)
                                   && r.Races.All(race => KeyFor(r.Fk, race, r.Id, r.Level) is not null)).ToList();

        // unit[(file, level)] = pot / Σ weights, each row counted by the share of the three races that learn it. For a
        // symmetric kit (every race the same count of skills) that IS each race's own pot / Σ weights, and because every
        // row then takes weight × one unit, a rerun reads back the same unit: the split is a fixed point.
        var unit = new Dictionary<(string, int), double>();
        foreach (var g in potRows.GroupBy(r => (r.Fk.File, r.Level)))
            unit[g.Key] = g.Sum(r => r.Sp * (r.Races.Length / 3.0)) / g.Sum(r => weights[r.Id] * (r.Races.Length / 3.0));

        var price = new Dictionary<PriceRow, int>();
        foreach (var r in potRows)
        {
            int p = Nice(weights[r.Id] * unit[(r.Fk.File, r.Level)]);
            // Within 0.5% of the cell already there = the same price; keeps rounding from flipping cells on a rerun.
            price[r] = Math.Abs(p - r.Sp) <= 0.005 * r.Sp ? (int)r.Sp : p;
        }

        if (show is not null)
            foreach (var g in potRows.Where(r => r.Fk.File.Contains(show, StringComparison.OrdinalIgnoreCase))
                                     .GroupBy(r => (r.Fk.File, r.Level)).OrderBy(g => g.Key.File).ThenBy(g => g.Key.Level))
            {
                Console.WriteLine($"  {g.Key.File} @{g.Key.Level}: pot(Human) {potRows.Where(r => r.Fk.File == g.Key.File && r.Level == g.Key.Level && r.Races.Contains(Race.Human)).Sum(r => r.Sp):N0}");
                foreach (var r in g)
                    Console.WriteLine($"      {r.Id,-34} w {weights[r.Id],4:0.##}  {r.Sp,12:N0} → {price[r],12:N0}  {(r.Races.Length == 3 ? "" : string.Join(";", r.Races))}");
            }

        int changed = 0;
        foreach (var fg in potRows.GroupBy(r => r.Fk.File))
        {
            string path = Path.Combine(csvDir, fg.Key + ".csv");
            bool bom = File.ReadAllBytes(path) is [0xEF, 0xBB, 0xBF, ..];
            string text = File.ReadAllText(path);
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Split(nl);
            var header = SplitCsv(lines.First(l => l.StartsWith("LEARN"))).Select(x => x.Trim().ToUpperInvariant()).ToList();
            int spCol = header.FindIndex(x => x.StartsWith("SP COST"));
            bool x1000 = header[spCol].Contains("X1000");
            foreach (var r in fg)
            {
                if (price[r] == r.Sp) continue;
                string line = lines[r.LineIndex];
                var spans = WeaponColumn.FieldSpans(line);
                string cell = line[spans[spCol].Start..spans[spCol].End].Trim();
                int now = price[r];
                string outCell = x1000 ? (now / 1000.0).ToString("0.###", CultureInfo.InvariantCulture)
                               : cell.EndsWith("kk", StringComparison.OrdinalIgnoreCase) ? (now / 1e6).ToString("0.###", CultureInfo.InvariantCulture) + "kk"
                               : cell.EndsWith("k", StringComparison.OrdinalIgnoreCase) ? (now / 1e3).ToString("0.###", CultureInfo.InvariantCulture) + "k"
                               : now.ToString(CultureInfo.InvariantCulture);
                lines[r.LineIndex] = line[..spans[spCol].Start] + outCell + line[spans[spCol].End..];
                changed++;
            }
            File.WriteAllText(path, string.Join(nl, lines), new UTF8Encoding(bom));
        }
        long before = potRows.Sum(r => r.Sp), after = potRows.Sum(r => (long)price[r]);
        Console.WriteLine($"{changed} SP cell(s) rewritten; the pots summed {before:N0} before, {after:N0} after (rounding).");
        return Run(csvDir, repoRoot, false);
    }

    /// <summary>Three significant figures (below 1,000: to the unit), so the Learn tab reads 95,100 not 95,084.</summary>
    private static int Nice(double v)
    {
        if (v < 1000) return (int)Math.Round(v);
        double step = Math.Pow(10, Math.Floor(Math.Log10(v)) - 2);
        return (int)(Math.Round(v / step) * step);
    }

    // ---- the price table the engine loads --------------------------------------------------------------------

    /// <summary>`ClassSkillTables.SpPrices.g.cs`: every class-table row's SP, read off the CSVs, one line per
    /// (class key, skill, learn level). <see cref="ClassSkills"/> writes it onto the rows once at load, so the CSV
    /// cell IS the price, actives included.</summary>
    private static string WriteSpPrices(string csvDir)
    {
        var sb = new StringBuilder(Header);
        sb.Append("namespace Game.Shared;\n\npublic static partial class ClassSkillTables\n{\n");
        sb.Append("    /// <summary>`BL-326` — race,base,archetype,discipline,fourth,skill,learnLevel,sp. `-` = none.</summary>\n");
        sb.Append("    internal const string SpPrices = @\"\n");
        var seen = new HashSet<string>();
        foreach (var r in ReadPriceRows(csvDir))
            foreach (var race in r.Races)
            {
                if (KeyFor(r.Fk, race, r.Id, r.Level) is not ClassSkills.ClassKey k || r.Sp > int.MaxValue) continue;
                string line = $"{k.Race},{k.Base},{k.Archetype?.ToString() ?? "-"},{k.Discipline?.ToString() ?? "-"},{(k.Fourth ? 1 : 0)},{r.Id},{r.Level},{r.Sp}";
                if (seen.Add(line)) sb.Append(line).Append('\n');
            }
        sb.Append("\";\n}\n");
        return sb.ToString().Replace("\n", "\r\n");
    }
}
