using System.Globalization;
using Game.Shared;


/// <summary>`docs/data/debuff_landmods.csv` vs the code — the SAME contract the class CSVs run on.
///
/// <para>🔑 HIS RULING, 2026-09-13: *"take all the debuffs each single skill make them in a table and
/// put the modifiers there … Then each new debuff to go there and to ask for modifier edit … The
/// current classes csv descriptions to remove the modifiers and those modifiers to be red from that
/// new file"*. So the `(success chance xN)` text is GONE from the four class CSVs that carried it and
/// `SUCCESS` in this one file is the authority for <see cref="SkillDef.DebuffLandMod"/>.</para>
///
/// <para>⚠ WITHOUT THIS CHECK THE FILE IS DECORATION. That is the whole lesson of the class CSVs — a
/// mirrored file with no verifier drifts silently, and a reference that trails the build is worse than
/// none. Two failures are reported, and they are different things:</para>
/// <list type="bullet">
///   <item><b>DRIFT</b> — the file says one number and the build ships another. The CSV is the
///   authority, so the CODE owes the change.</item>
///   <item><b>MISSING</b> — a debuff skill exists in the catalog with no row here. That is his *"each
///   new debuff to go there and to ask for modifier edit"*: regenerate with
///   <c>--dump-landmod-csv</c>, then ASK HIM for the modifier. Never pick one.</item>
/// </list>
internal static class LandMods
{
    public const string FileName = "debuff_landmods.csv";

    /// <summary>Returns the number of problems found. Prints nothing when the file is absent —
    /// a repo without it yet is not a failing build.</summary>
    public static int Run(string dataDir)
    {
        // ⚠ The caller hands us `docs/data/classes_skills_csv`; this file lives one level up in `docs/data`.
        string path = Path.Combine(dataDir, FileName);
        if (!File.Exists(path))
        {
            var up = Path.GetDirectoryName(dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (up is not null) path = Path.Combine(up, FileName);
        }
        Console.WriteLine();
        Console.WriteLine($"===== {FileName}");
        if (!File.Exists(path))
        {
            Console.WriteLine("  not present — generate it with:");
            Console.WriteLine("    dotnet run --project tools/BalanceMatrix -- --dump-landmod-csv");
            return 0;
        }

        var authored = new Dictionary<string, (float Want, string Name)>(StringComparer.Ordinal);
        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var c = SplitCsv(lines[i]);
            if (c.Length < 6 || c[1].Length == 0) continue;
            if (!TryMod(c[5], out float want))
            {
                Console.WriteLine($"  UNREADABLE SUCCESS  {c[1],-32} \"{c[5]}\" — expected a number like 0.85 or x0.85");
                continue;
            }
            authored[c[1]] = (want, c[0]);
        }

        int problems = 0, checkedRows = 0;
        foreach (var kv in authored.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (SkillCatalog.Get(kv.Key) is not SkillDef def)
            {
                Console.WriteLine($"  NO SUCH SKILL      {kv.Key,-32} (row \"{kv.Value.Name}\" — a rename, or a typo)");
                problems++;
                continue;
            }
            int top = Math.Max(1, def.Levels?.Length ?? 1);
            float shipped = def.DebuffLandModAt(top);
            checkedRows++;
            if (Math.Abs(shipped - kv.Value.Want) > 0.001f)
            {
                Console.WriteLine($"  DRIFT              {kv.Key,-32} file x{kv.Value.Want:0.##}  code x{shipped:0.##}"
                                + "   → the FILE is the authority; the code owes this.");
                problems++;
            }
        }

        // The other direction: a debuff in the catalog that nobody has priced.
        int missing = 0;
        foreach (var def in SkillCatalog.AllSkills.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            if (def.Passive is not null) continue;
            if (!SkillMath.IsHostile(def)) continue;
            int top = Math.Max(1, def.Levels?.Length ?? 1);
            var eff = (def.StackLevelAt(top)?.Effect ?? def.Effect) | def.Effect;
            if ((eff & SkillEffect.Taunt) != 0 && def.DebuffSchool == DebuffSchool.None) continue;
            if (authored.ContainsKey(def.Id)) continue;
            Console.WriteLine($"  NOT IN THE FILE    {def.Id,-32} \"{def.Name}\" — regenerate, then ASK HIM for the modifier.");
            missing++;
        }

        problems += missing;
        Console.WriteLine($"  {checkedRows} row(s) verified against the code."
                        + (problems == 0 ? "  OK." : $"  {problems} problem(s)."));
        return problems;
    }

    /// <summary>Accepts `0.85`, `x0.85`, `X0.85` and a stray space — he types the modifier the way he
    /// writes it in chat, and a checker that refuses his own notation is a checker he stops running.</summary>
    private static bool TryMod(string s, out float v)
    {
        s = s.Trim().TrimStart('x', 'X').Trim();
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }

    private static string[] SplitCsv(string line)
    {
        var outp = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (q && i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                else q = !q;
            }
            else if (ch == ',' && !q) { outp.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        outp.Add(cur.ToString());
        return outp.ToArray();
    }
}
