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
//
//  ⚠ 2026-08-17 — the file names moved from level bands (`healer 40-74`) to CLASS TIERS (`healer 3rd`),
//  his call: *"well for fighters 20-35 is not right .. they have skills at 36 .. so 2nd class is more
//  suited and understandable"*. A band in a filename is a claim about content, and it was already false.
//  The refuse-to-overwrite check keys on the NEW names, so the four files it had already written are
//  safe under their new names and only the four new disciplines get generated.
// =====================================================================================================

bool force = args.Contains("--force");

// Walk up to the repo root from bin/Debug/netX/ so the tool works from anywhere.
var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Game.sln"))) dir = dir.Parent;
if (dir is null) { Console.Error.WriteLine("Could not find the repo root (Game.sln)."); return 1; }
string outDir = Path.Combine(dir.FullName, "docs", "data", "classes_skills_csv");
Directory.CreateDirectory(outDir);

// --check writes nothing. It reads the authored files back against the registered tables — see Check.cs.
// `-v` additionally prints what the DESCR reader could NOT verify (⚪ lines): a value whose stat the
// code has no field for, a percent authored against a flat field, and any number in the text the reader
// failed to bind to a stat. Defects (🟡) always print. Read it when you want the COVERAGE, not the bugs.
if (args.Contains("--check")) { Check.Verbose = args.Contains("-v") || args.Contains("--verbose"); return Check.Run(outDir); }

// --retarget rewrites the TARGET column of every file into his `[scope]/[breadth]` scheme (2026-08-27).
// It is a ONE-OFF migration, not part of the normal flow: after it has run, `--check` is what keeps the
// column honest, and a hand edit of his beats anything this would regenerate. See Retarget.cs.
if (args.Contains("--retarget")) return Retarget.Run(outDir);

// `--aoe-column` — BL-96, his second column: RANGE is how far you throw it, AOE is how wide it goes
// off. Also a ONE-OFF migration; idempotent and refuses to run on a file that already has the column.
// See AoeColumn.cs for why two numbers sharing one column was not merely untidy.
if (args.Contains("--aoe-column")) return AoeColumn.Run(outDir);

// ===== HIS DISCIPLINE MAP (2026-08-17) =============================================================
// He redrew the 3rd-class split and named the files himself: *"class 2nd => desc1/desc2 3rd =>
// desc1/desc2 4th"*, with
//     cleric  => buffer / healer
//     rogue   => archer / dual      "same logic as warrior — one is range the other mele"
//     warrior => warrior / war_aoe  "one is aoe and more tanky .. the other is like a mele nuker"
//     tank    => tank               "i think one tank is enough — wont have the vanguard the off tank,
//                                    we have a warrior for that — the varity will come from race"
//     nuker   => nuker              "same logic as the tank, 1 discipline ... 3 identities"
//
// So EIGHT discipline files, not twelve, and RACE (the trailing column) is what makes three identities
// out of one file. Two consequences worth knowing:
//   - `Vanguard` is dropped as a discipline. Nothing is registered for it (the 40+ purge took it), so
//     the tank file loses nothing by seeding from Bulwark alone.
//   - `nuker` seeds from BOTH Magus and Tempest, because he is merging them and their two kits are the
//     only substantial 40+ content in the game outside the Warchanter's buff ladder. FlameBolt lands
//     twice — once as "Annihilate", once as "Chain Lightning" — which is the honest picture of two kits
//     being folded into one, and his to reconcile.
//
// ⚠ The enum in Classes.Third.cs is NOT changed by this. Discipline values persist on characters; the
// collapse happens when the authored CSVs arrive, not because a file was renamed.
var groups = new (string File, Discipline[] Disciplines)[]
{
    ("tank",    new[] { Discipline.Bulwark }),
    ("warrior", new[] { Discipline.Ravager }),      // the melee nuker
    ("war_aoe", new[] { Discipline.Warlord }),      // the aoe/tanky one
    ("dual",    new[] { Discipline.Nullblade, Discipline.Phantom, Discipline.Venomweaver }),
    ("archer",  new[] { Discipline.Sharpshooter, Discipline.Trapper, Discipline.Hunter }),
    ("healer",  new[] { Discipline.Lightbringer }),
    ("buffer",  new[] { Discipline.Warchanter }),
    ("nuker",   new[] { Discipline.Magus, Discipline.Tempest }),
};

// His two tiers. ⚠ The old suffixes were `40-74` / `76-85` and level 75 fell in NEITHER — which silently
// dropped Elemental Burst's 10th rung (registered at char 75). The tier names carry no band promise, so
// 3rd now closes at 75 and nothing falls in a gap.
var bands = new (string Suffix, int Min, int Max)[] { ("3rd", 40, 75), ("4th", 76, 85) };

// ⚠ ONE `MP` COLUMN since 2026-08-20. It was `INIT MP` + `FINIT MP`, and his own sheets never agreed on
// a ratio because the ratio was never his decision — the engine now splits every skill 20/80 itself
// (`SkillMath.InitialMpFraction`). What goes in this column is the skill's WHOLE price.
const string Header = "LEARN @ LVL, NAME,TYPE,RANGE,TARGET, CAST s,CD s,DURRATION s, DESCR," +
                      "MP,SP COST,REPLACES,RACE";

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
        var rows = new List<(int Lvl, int SkillLvl, string Id, string? Name, int? Sp, Race Race)>();
        foreach (var d in disciplines)
            foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
                foreach (var cs in ClassSkills.ForClass(race, BaseOf(d), Disciplines.Parent(d), d))
                    if (cs.LearnLevel >= min && cs.LearnLevel <= max)
                        rows.Add((cs.LearnLevel, cs.SkillLevel, cs.SkillId, cs.DisplayName, cs.SpCost, race));

        var collapsed = rows
            .GroupBy(r => (r.Lvl, r.SkillLvl, r.Id, r.Name, r.Sp))
            .Select(g => (g.Key.Lvl, g.Key.SkillLvl, g.Key.Id, g.Key.Name, g.Key.Sp,
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
                sb.AppendLine(Row(def, r.Lvl, r.SkillLvl, r.Name, tag, r.Sp));
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
static string Row(SkillDef def, int learnLevel, int skillLevel, string? displayName, string raceTag, int? spOverride = null)
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
        def.MpCostAt(skillLevel).ToString(CultureInfo.InvariantCulture),
        (spOverride ?? def.SpCostAt(skillLevel)).ToString(CultureInfo.InvariantCulture),
        Q(replaces), raceTag);

    static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    // Quote anything holding a comma or a quote — a description with a comma in it is the common case
    // and an unquoted one silently shifts every column after it.
    static string Q(string s) =>
        s.Contains(',') || s.Contains('"') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
}
