using System.Reflection;
using Game.Shared;

/// <summary>
/// `BL-314` — WHAT THE PASSIVES CARRY, the inventory the split is designed from. For every passive any playable path
/// learns: its rungs, its SP, how many paths share it, the stats it pays (read off the live payload fields, not the
/// description) and what gates them (armor weight, weapon, shield). Then the stat-by-stat view: which stats are
/// re-authored inside how many different passives — those are the split's candidates.
///
/// <para><c>dotnet run --project tools/BalanceMatrix -- --passive-inventory</c>; add a skill-id fragment
/// (<c>--passive-inventory armor</c>) to print those passives rung by rung.</para>
/// </summary>
static class PassiveInventory
{
    private sealed class Info
    {
        public required SkillDef Def;
        public readonly SortedSet<string> Stats = new();
        public readonly SortedSet<string> Gates = new();
        public readonly SortedSet<string> Paths = new();
        public readonly SortedDictionary<int, int> RungLearn = new();   // rung -> lowest learn level
        public long Sp;
    }

    public static void Run(string[] args)
    {
        string? detail = args.Length > 1 ? args[1] : null;
        var all = new Dictionary<string, Info>();
        var pathPassives = new List<(string Path, List<string> Ids)>();

        foreach (var sc in ClassCatalog.Playable)
        {
            var (a, b) = Disciplines.Of(sc.Race, sc.Archetype);
            foreach (var d in b is { } bb ? new[] { a, bb } : new[] { a })
            {
                string path = $"{ClassNames.Third(d, sc.Race)} ({sc.Race} {d})";
                var ids = new List<string>();
                var list = ClassSkills.ForClass(sc.Race, sc.Base, null, null)
                    .Concat(ClassSkills.Cumulative(sc.Race, sc.Base, sc.Archetype, d, fourth: true));
                foreach (var cs in list)
                {
                    var def = SkillCatalog.Get(cs.SkillId);
                    if (def is null || def.Category != SkillCategory.Passive) continue;
                    if (!all.TryGetValue(def.Id, out var inf)) all[def.Id] = inf = new Info { Def = def };
                    inf.Paths.Add(path);
                    if (!inf.RungLearn.TryGetValue(cs.SkillLevel, out int lv) || cs.LearnLevel < lv)
                        inf.RungLearn[cs.SkillLevel] = cs.LearnLevel;
                    if (!ids.Contains(def.Id)) ids.Add(def.Id);
                }
                pathPassives.Add((path, ids));
            }
        }

        foreach (var inf in all.Values)
        {
            foreach (var rung in inf.RungLearn.Keys)
            {
                inf.Sp += inf.Def.SpCostAt(rung);
                foreach (var (stat, gate) in StatsAt(inf.Def, rung))
                {
                    inf.Stats.Add(stat);
                    if (gate.Length > 0) inf.Gates.Add(gate);
                }
            }
        }

        Console.WriteLine("=== BL-314 · PASSIVE INVENTORY (every passive a playable path learns) ===");
        Console.WriteLine($"  {"skill id",-34} {"name",-28} {"rungs",5} {"lvls",7} {"SP",12} {"paths",5}  stats  [gates]");
        foreach (var inf in all.Values.OrderByDescending(i => i.Stats.Count).ThenBy(i => i.Def.Id))
        {
            var lv = inf.RungLearn.Values;
            Console.WriteLine($"  {inf.Def.Id,-34} {Trim(inf.Def.Name, 28),-28} {inf.RungLearn.Count,5} {lv.Min(),3}-{lv.Max(),-3} {inf.Sp,12:N0} {inf.Paths.Count,5}  "
                            + string.Join(",", inf.Stats) + (inf.Gates.Count > 0 ? "  [" + string.Join(" ", inf.Gates) + "]" : "")
                            + (inf.Def.Replaces is { Length: > 0 } r ? "  replaces " + string.Join(",", r) : ""));
        }

        Console.WriteLine();
        Console.WriteLine("=== THE SAME STAT, RE-AUTHORED IN HOW MANY PASSIVES ===");
        Console.WriteLine($"  {"stat",-22} {"passives",8} {"multi-stat",10} {"paths",5}  in");
        var byStat = all.Values.SelectMany(i => i.Stats.Select(s => (s, i))).GroupBy(x => x.s);
        foreach (var g in byStat.OrderByDescending(g => g.Count()))
        {
            var infs = g.Select(x => x.i).ToList();
            int multi = infs.Count(i => i.Stats.Count > 1);
            int paths = infs.SelectMany(i => i.Paths).Distinct().Count();
            Console.WriteLine($"  {g.Key,-22} {infs.Count,8} {multi,10} {paths,5}  " + string.Join(",", infs.Select(i => i.Def.Id).Take(12))
                            + (infs.Count > 12 ? ",…" : ""));
        }

        Console.WriteLine();
        Console.WriteLine("=== PER PATH ===");
        Console.WriteLine($"  {"path",-38} {"passives",8} {"multi-stat",10} {"stat slots",10} {"passive SP",14}");
        foreach (var (path, ids) in pathPassives)
        {
            var infs = ids.Select(i => all[i]).ToList();
            Console.WriteLine($"  {path,-38} {infs.Count,8} {infs.Count(i => i.Stats.Count > 1),10} {infs.Sum(i => i.Stats.Count),10} "
                            + $"{infs.Sum(i => i.Sp),14:N0}");
        }

        // If split, a single-stat piece needs a rung only where ITS value changes — the rest of the old rungs were
        // re-stating the same number because a sibling stat moved. This is the row count the split really costs.
        Console.WriteLine();
        Console.WriteLine("=== IF SPLIT: rungs each piece needs (a new rung only where its value changes) ===");
        Console.WriteLine($"  {"skill id",-34} {"rungs now",9} {"pieces",6} {"rungs split",11}  per piece");
        foreach (var inf in all.Values.Where(i => i.Stats.Count > 1 && i.RungLearn.Count >= 3 && !i.Def.Id.StartsWith("swap_"))
                                      .OrderByDescending(i => i.RungLearn.Count).ThenBy(i => i.Def.Id))
        {
            var last = new Dictionary<string, string>();
            var changes = new SortedDictionary<string, int>();
            foreach (var rung in inf.RungLearn.Keys)
                foreach (var (stat, value, gate) in Values(inf.Def, rung).GroupBy(v => (v.Stat, v.Value)).Select(g => g.First()))
                {
                    if (last.TryGetValue(stat, out var was) && was == value) continue;
                    last[stat] = value;
                    changes[stat] = changes.GetValueOrDefault(stat) + 1;
                }
            Console.WriteLine($"  {inf.Def.Id,-34} {inf.RungLearn.Count,9} {changes.Count,6} {changes.Values.Sum(),11}  "
                            + string.Join(" ", changes.Select(c => $"{c.Key}:{c.Value}")));
        }

        if (detail is null) return;
        foreach (var inf in all.Values.Where(i => i.Def.Id.Contains(detail, StringComparison.OrdinalIgnoreCase)).OrderBy(i => i.Def.Id))
        {
            Console.WriteLine();
            Console.WriteLine($"=== {inf.Def.Id} — {inf.Def.Name} — rung by rung ===");
            foreach (var (rung, lvl) in inf.RungLearn)
                Console.WriteLine($"  r{rung,-3} @{lvl,-3} SP {inf.Def.SpCostAt(rung),10:N0}  "
                                + string.Join("  ", StatsAtValues(inf.Def, rung)));
        }
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";

    /// <summary>Every (stat, gate) pair a rung pays, from all three payload shapes.</summary>
    private static IEnumerable<(string Stat, string Gate)> StatsAt(SkillDef def, int rung) =>
        Values(def, rung).Select(v => (v.Stat, v.Gate));

    private static IEnumerable<string> StatsAtValues(SkillDef def, int rung) =>
        Values(def, rung).Select(v => $"{v.Stat}={v.Value}{(v.Gate.Length > 0 ? "@" + v.Gate : "")}");

    private static IEnumerable<(string Stat, string Value, string Gate)> Values(SkillDef def, int rung)
    {
        foreach (var p in def.PassivesAt(rung))
        {
            string gate = Gate(p);
            foreach (var (n, v) in NonDefault(p, "RequiresShield", "RequiredArmor")) yield return (n, v, gate);
        }
        if (def.ArmorMasteryLevels is { Length: > 0 } am && rung <= am.Length)
        {
            var prof = am[rung - 1];
            foreach (var (w, mods) in new[] { ("robe", prof.Robe), ("light", prof.Light), ("heavy", prof.Heavy), ("bare", prof.None) })
                foreach (var (n, v) in NonDefault(mods)) yield return (n, v, "armor:" + w);
        }
        if (def.WeaponMasteryLevels is { Length: > 0 } wm && rung <= wm.Length)
        {
            var prof = wm[rung - 1];
            foreach (var (w, pe) in new[] { ("sword", prof.Sword), ("dual", prof.Dual), ("bow", prof.Bow), ("blunt", prof.Blunt), ("other", prof.Other) })
                foreach (var (n, v) in NonDefault(pe, "RequiresShield", "RequiredArmor")) yield return (n, v, "weapon:" + w);
        }
    }

    private static string Gate(PassiveEffect p)
    {
        var g = new List<string>();
        if (p.RequiredArmor != ArmorWeights.None) g.Add("armor:" + p.RequiredArmor.ToString().Replace(", ", "|").ToLowerInvariant());
        if (p.RequiresShield) g.Add("shield");
        return string.Join("+", g);
    }

    private static IEnumerable<(string Name, string Value)> NonDefault<T>(T value, params string[] skip) where T : struct
    {
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (skip.Contains(prop.Name) || prop.GetIndexParameters().Length > 0) continue;
            object? v = prop.GetValue(value);
            if (v is null) continue;
            var t = prop.PropertyType;
            object? zero = t.IsValueType ? Activator.CreateInstance(t) : null;
            if (Equals(v, zero)) continue;
            if (!t.IsPrimitive && !t.IsEnum) { yield return (prop.Name, "…"); continue; }
            yield return (prop.Name, v is float f ? f.ToString("0.###") : v.ToString()!);
        }
    }
}
