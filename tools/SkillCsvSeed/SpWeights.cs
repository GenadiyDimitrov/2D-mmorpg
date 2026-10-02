using System.Globalization;
using System.Text;
using Game.Shared;

// =====================================================================================================
//  The SP-cell plumbing shared by `--reprice-sp` (`BL-334`, SpCurve.cs): reading every priced row, the weights file,
//  writing cells back in their own unit, and the price table the engine loads.
//
//  History: `BL-326` (2026-09-29) split each level's pot by weight here (`--reweigh-sp`), and `--scale-sp` scaled a
//  level band. Both are gone (2026-10-02): a crowded level made each rung cheaper, so ladders fell. The weights stay.
//
//  THE WEIGHTS ARE HIS: `docs/data/sp_weights.csv`, one row per skill id. A skill not in the file gets a default
//  (passive 0.33, buff/utility 1, damage/debuff/heal/trap 1.5) and is ADDED to the file as `default`, so a new skill
//  shows up there for him to price.
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

    /// <summary>Writes each row's new price into its SP COST cell, keeping the cell's own unit (k / kk / ×1000), the
    /// file's line endings and its BOM. Returns how many cells changed.</summary>
    private static int WriteSpCells(string csvDir, Dictionary<PriceRow, int> price)
    {
        int changed = 0;
        foreach (var fg in price.Keys.GroupBy(r => r.Fk.File))
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
        return changed;
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
