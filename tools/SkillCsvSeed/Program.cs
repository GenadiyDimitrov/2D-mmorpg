using System.Globalization;
using System.Text;
using Game.Shared;

// =====================================================================================================
//  SKILL CSV SEEDS — the level-40+ authoring sheets, generated ONCE from the compiled class tables.
//
//  His playtest-23 instruction, verbatim: *"once the csv land they will be there (u can add files next
//  to other skills 20-35.Csv the mele rogues one, one for archers, one for buffers and one for healers
//  ..with what u have after 40 so I start with them later on.. `Healers 40-74.csv` and 76-85)"*.
//
//  🔑 WHAT THIS IS NOT: it is not the 40+ kits. `BL-02` still stands — nothing above 40 is invented.
//  What it emits is exactly what the game already registers above level 40 for those four groups, in
//  the authoring format, so he starts from a filled sheet instead of an empty one.
//
//  🔴 IT REFUSES TO OVERWRITE. Once a file exists it belongs to HIM; re-running the tool would silently
//  eat whatever he authored. --force is the deliberate escape hatch and should essentially never be used.
// =====================================================================================================

bool force = args.Contains("--force");

// Walk up to the repo root from bin/Debug/netX/ so the tool works from anywhere.
var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Game.sln"))) dir = dir.Parent;
if (dir is null) { Console.Error.WriteLine("Could not find the repo root (Game.sln)."); return 1; }
string outDir = Path.Combine(dir.FullName, "docs", "data", "classes_skills_csv");
Directory.CreateDirectory(outDir);

// The four groups he named, mapped to the disciplines that actually carry them. `melee rogue` and
// `archer` are THREE disciplines each — the archer merge splits the rogue by race at 40 — which is the
// whole reason the CSV plan is 10 files and not 30 (docs/design/Disciplines.md §1).
var groups = new (string File, Discipline[] Disciplines)[]
{
    ("melee rogue", new[] { Discipline.Nullblade, Discipline.Phantom, Discipline.Venomweaver }),
    ("archer",      new[] { Discipline.Sharpshooter, Discipline.Trapper, Discipline.Hunter }),
    ("healer",      new[] { Discipline.Lightbringer }),
    ("buffer",      new[] { Discipline.Warchanter }),
};

// His two bands. 75 is in neither on purpose — it is his split, not mine, and the 20-35 files have the
// same kind of gap.
var bands = new (string Suffix, int Min, int Max)[] { ("40-74", 40, 74), ("76-85", 76, 85) };

const string Header = "LEARN @ LVL, NAME,TYPE,RANGE,TARGET, CAST s,CD s,DURRATION s, DESCR, " +
                      "INIT MP,FINIT MP ,SP COST,REPLACES,RACE";

int written = 0, skipped = 0;
foreach (var (fileName, disciplines) in groups)
    foreach (var (suffix, min, max) in bands)
    {
        string path = Path.Combine(outDir, $"{fileName} {suffix}.csv");
        if (File.Exists(path) && !force)
        {
            Console.WriteLine($"  skip   {Path.GetFileName(path)}  (exists — it is his now; --force to replace)");
            skipped++;
            continue;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        // (learnLevel, skillLevel, skillId, race) — one row per race that learns it, then collapsed:
        // a skill all three races learn at the same level is ONE row with a blank RACE, which is the
        // format's own rule and the shape he reads fastest.
        var rows = new List<(int Lvl, int SkillLvl, string Id, string? Name, Race Race)>();
        foreach (var d in disciplines)
            foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
                foreach (var cs in ClassSkills.ForClass(race, BaseOf(d), Disciplines.Parent(d), d))
                    if (cs.LearnLevel >= min && cs.LearnLevel <= max)
                        rows.Add((cs.LearnLevel, cs.SkillLevel, cs.SkillId, cs.DisplayName, race));

        var collapsed = rows
            .GroupBy(r => (r.Lvl, r.SkillLvl, r.Id, r.Name))
            .Select(g => (g.Key.Lvl, g.Key.SkillLvl, g.Key.Id, g.Key.Name,
                          Races: g.Select(x => x.Race).Distinct().OrderBy(x => (int)x).ToArray()))
            .OrderBy(r => r.Lvl).ThenBy(r => r.Name ?? r.Id)
            .ToList();

        foreach (var r in collapsed)
        {
            if (SkillCatalog.Get(r.Id) is not SkillDef def) continue;
            // All three races → one row, blank RACE. Fewer → one row per race, which is what a
            // race-specific 40+ skill looks like in his format.
            var tags = r.Races.Length == 3 ? new[] { "" } : r.Races.Select(x => x.ToString().ToLowerInvariant()).ToArray();
            foreach (var tag in tags)
                sb.AppendLine(Row(def, r.Lvl, r.SkillLvl, r.Name, tag));
        }

        if (collapsed.Count == 0)
            sb.AppendLine($"# nothing above level {min} exists yet for this discipline — start here.");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"  wrote  {Path.GetFileName(path)}  ({collapsed.Count} skill row(s))");
        written++;
    }

Console.WriteLine();
Console.WriteLine($"{written} file(s) written, {skipped} left alone.  {outDir}");
return 0;

static BaseClass BaseOf(Discipline d) =>
    Disciplines.Parent(d) is Archetype.Healer or Archetype.Nuker ? BaseClass.Mage : BaseClass.Fighter;

// One CSV line in the 20-35 files' own column order, plus the RACE column the 40+ format adds.
static string Row(SkillDef def, int learnLevel, int skillLevel, string? displayName, string raceTag)
{
    string name = displayName ?? def.Name;
    if (skillLevel > 1) name += $" L{skillLevel}";
    string type = def.Category switch
    {
        SkillCategory.Passive => "Passive",
        SkillCategory.Buff    => "Magic/Buff",
        SkillCategory.Debuff  => "Magic/Debuff",
        SkillCategory.Heal    => "Magic/Heal",
        SkillCategory.Magic   => "Magic/Dmg",
        _                     => "physical active",
    };
    string target = def.TargetMode switch
    {
        TargetMode.SelfOnly        => "self",
        TargetMode.AlliesInRadius  => "self/party",
        TargetMode.EnemiesInRadius => "enemies in radius",
        _                          => "self/target",
    };
    string replaces = def.Replaces is { Length: > 0 } rs
        ? "[" + string.Join(" ", rs.Select(id => SkillCatalog.Get(id)?.Name ?? id)) + "]"
        : "[]";

    return string.Join(",",
        learnLevel.ToString(CultureInfo.InvariantCulture),
        Q(name), type,
        F(def.RangeAt(skillLevel)), target,
        F(def.CastTicks / 10f), F(def.CooldownTicks / 10f), F(def.DurationTicks / 10f),
        Q(def.DescriptionAt(skillLevel)),
        def.InitialMpAt(skillLevel).ToString(CultureInfo.InvariantCulture),
        def.FinishMpAt(skillLevel).ToString(CultureInfo.InvariantCulture),
        def.SpCostAt(skillLevel).ToString(CultureInfo.InvariantCulture),
        Q(replaces), raceTag);

    static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    // Quote anything holding a comma or a quote — a description with a comma in it is the common case
    // and an unquoted one silently shifts every column after it.
    static string Q(string s) =>
        s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
