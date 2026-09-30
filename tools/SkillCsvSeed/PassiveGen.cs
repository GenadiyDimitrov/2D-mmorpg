using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Game.Shared;

// =====================================================================================================
//  `--gen-passives` — `BL-314`: THE SHARED PASSIVE LADDERS ARE GENERATED FROM HIS CSVs.
//
//  🔑 WHY GENERATED. The passive split (`docs/design/PassiveSplit.md` §10-§12) turned every class's armour and
//  weapon bundle into single-stat pieces that several classes climb at their OWN levels: `light_armor_mastery`
//  alone is 166 rows across nine files. One shared ladder per id is the union of every value any class authors
//  (a rung only where a number moves), and each class learns its own rungs of it at its own price. Typed by hand
//  that is ~1,400 learn rows and ~300 rungs that must agree with the files forever; generated, the CSV is the
//  only place a number lives, which is the two-way contract with nothing to drift.
//
//  WHAT IT WRITES (both files carry a do-not-edit header):
//    Game.Shared/Skills/Skills.PassiveLadders.g.cs       — the id constants and one SkillDef per ladder
//    Game.Shared/RaceAndClasses/ClassSkillTables.Passives.g.cs — every class's learn rows for them
//  The hand-written half (the rung builders, the proc piece) is Game.Shared/Skills/Skills.PassiveLadders.cs.
//
//  🔑 PRICES. The CSV cell IS the price (`BL-326`): the SP column is split per level by weight
//  (`--reweigh-sp`, SpWeights.cs) and this writes it through unchanged, plus the price table for every other row.
//
//  ⚠ It refuses rather than guesses: an unknown stat word, two values for one stat on a row, or a class whose
//  rungs would go DOWN as its levels go up stops the run with the file and row.
// =====================================================================================================

internal static partial class PassiveGen
{
    private enum Kind { Armor, Plain, Weapon, Hand }

    /// <summary>One generated id: how its rungs are carried, the order its stats sort a ladder by, and the
    /// words the Skills window shows. `Armor` = an ArmorMasteryProfile of StatMods (the pieces that came out of
    /// armour bundles keep the armour channel, so their percentages compose exactly as before); `Plain` = a
    /// PassiveEffect gated by RequiredArmor; `Weapon` = a WeaponMasteryProfile gated by the WEAPON cell;
    /// `Hand` = the def is hand-written (only the learn rows are generated).</summary>
    private sealed record Spec(string Id, Kind Kind, BaseClass Cls, string Desc, params string[] Order);

    private static readonly Spec[] Specs =
    {
        new("heavy_armor_mastery", Kind.Armor, BaseClass.Fighter, "Passive. Physical defence while wearing HEAVY armour.", "pdef#"),
        new("light_armor_mastery", Kind.Armor, BaseClass.Fighter, "Passive. Physical defence while wearing LIGHT armour.", "pdef#"),
        new("mage_armor_mastery",  Kind.Armor, BaseClass.Mage,    "Passive. Physical defence while wearing a ROBE.", "pdef#"),
        new("rogue_evasion",       Kind.Armor, BaseClass.Fighter, "Passive. Evasion while wearing LIGHT armour.", "eva#"),
        new("rogue_crit_resist",   Kind.Armor, BaseClass.Fighter, "Passive. Resist enemy critical hits while wearing LIGHT armour.", "critrateres%"),
        new("rogue_swift_mastery", Kind.Armor, BaseClass.Fighter, "Passive. Faster run speed while wearing LIGHT armour.", "ms#"),
        new("tank_defence_mastery", Kind.Armor, BaseClass.Fighter, "Passive. In HEAVY armour: a percentage more physical defence, at a small evasion cost.", "pdef%"),
        new("tank_crit_resist",    Kind.Armor, BaseClass.Fighter, "Passive. In HEAVY armour: critical hits against you deal less damage.", "critdmgres%"),
        new("heavy_vitality",      Kind.Armor, BaseClass.Fighter, "Passive. More max HP while wearing HEAVY armour.", "maxhp#"),
        new("hp_regeneration",     Kind.Armor, BaseClass.Fighter, "Passive. Faster HP regeneration.", "hpreg#"),
        new("mp_regeneration",     Kind.Armor, BaseClass.Fighter, "Passive. Faster MP regeneration.", "mpreg#"),
        new("mp_capacity",         Kind.Armor, BaseClass.Mage,    "Passive. More max MP.", "maxmp#"),
        new("mana_recovery",       Kind.Armor, BaseClass.Mage,    "Passive. Every MP restore you receive gives more.", "restoremp%"),
        new("cast_speed_mastery",  Kind.Armor, BaseClass.Mage,    "Passive. Faster casting.", "cast%"),
        new("mdef_mastery",        Kind.Armor, BaseClass.Mage,    "Passive. A percentage more magic defence.", "mdef%"),
        new("mp_cost_mastery",     Kind.Armor, BaseClass.Mage,    "Passive. Your skills cost less MP.", "mpcost%"),
        new("clerics_light_armor_mastery", Kind.Armor, BaseClass.Mage, "Passive. In LIGHT armour: cast and attack as fast as in a robe, with better MP regeneration.", "cast%"),
        new("cleric_heavy_armor_mastery",  Kind.Armor, BaseClass.Mage, "Passive. In HEAVY armour: cast and attack as fast as in a robe, with better MP regeneration.", "cast%"),
        new("anti_magic",          Kind.Plain,  BaseClass.Mage,    "Passive. More magic defence.", "mdef#"),
        new("magic_resistance",    Kind.Plain,  BaseClass.Mage,    "Passive. Enemy spells fizzle against you more often.", "mres%"),
        new("magic_protection",    Kind.Plain,  BaseClass.Fighter, "Passive. Enemy spells are twice as likely to fail against you.", "mfail#"),
        new("fighter_critical_dmg_mastery", Kind.Plain, BaseClass.Fighter, "Passive. Your critical hits deal more damage.", "critdmg#"),
        new("fighter_accuracy",    Kind.Plain,  BaseClass.Fighter, "Passive. Better accuracy.", "acc#"),
        new("fury_mastery",        Kind.Plain,  BaseClass.Fighter, "Passive. Faster attack speed.", "as%"),
        new("cooldown_mastery",    Kind.Plain,  BaseClass.Mage,    "Passive. Shorter reuse delays.", "reuse%"),
        new("warrior_regeneration", Kind.Plain, BaseClass.Fighter, "Passive. Faster HP regeneration, and more HP and MP regeneration while sitting.", "hpreg#"),
        new("weapon_mastery",      Kind.Weapon, BaseClass.Fighter, "Passive. More physical attack with a sword, a blunt or duals.", "patk#"),
        new("bow_mastery",         Kind.Weapon, BaseClass.Fighter, "Passive. More physical attack with a bow.", "patk#"),
        new("spellcaster_weapon_mastery", Kind.Weapon, BaseClass.Mage, "Passive. More magic attack with a sword, a blunt or a bow.", "matk#"),
        new("rogue_critical_rate_mastery", Kind.Weapon, BaseClass.Fighter, "Passive. More critical hits with a bow or duals.", "critrate%"),
        new("rogue_bow_proficiency", Kind.Weapon, BaseClass.Fighter, "Passive. Your bow reaches further.", "bowrange#"),
        new("blunt_cleave",        Kind.Weapon, BaseClass.Fighter, "Passive. With a two-handed blunt, your basic attack hits every enemy around the target.", "cleave#"),
        new("strength_mastery",    Kind.Weapon, BaseClass.Fighter, "Passive. More physical attack. Its higher rungs need a two-handed sword or blunt; with anything else you keep the highest rung you can use.", "patk%"),
        new("dual_weapon_prof",    Kind.Hand,   BaseClass.Fighter, "", "proc"),
    };

    /// <summary>The metric keys Descr hands back, mapped to the field each kind carries it in.</summary>
    private static readonly Dictionary<string, string> StatModsField = new()
    {
        ["pdef#"] = "PDef", ["pdef%"] = "PDefPct", ["mdef%"] = "MDefPct", ["maxhp#"] = "MaxHp", ["maxmp#"] = "MaxMp",
        ["eva#"] = "Evasion", ["critrateres%"] = "CritRateResist", ["critdmgres%"] = "CritDmgResist",
        ["hpreg#"] = "HpRegen", ["mpreg#"] = "MpRegen", ["mpreg%"] = "MpRegenPct", ["cast%"] = "CastSpeedPct",
        ["as%"] = "AtkSpeedPct", ["ms#"] = "MoveSpeed", ["restoremp%"] = "RestoreMpPct", ["mpcost%"] = "MpCostPct",
    };

    private static readonly Dictionary<string, string> PassiveField = new()
    {
        ["mdef#"] = "MagicDefence", ["mres%"] = "MagicResist", ["critdmg#"] = "CritDamageFlat", ["acc#"] = "Accuracy",
        ["as%"] = "AtkSpeedPct", ["reuse%"] = "CooldownPct", ["patk#"] = "PhysAtk", ["patk%"] = "PhysAtkPct",
        ["matk#"] = "MagAtk", ["critrate%"] = "CritRate", ["bowrange#"] = "BowRange",
        ["mfail#"] = "MagicFailMod", ["hpreg#"] = "HpRegen", ["hpsit#"] = "HpRegenSitting", ["mpsit#"] = "MpRegenSitting",
        ["cleave#"] = "CleaveTargets", ["cleaverad#"] = "CleaveRadius",
    };

    /// <summary>A file, the class table it feeds, and how one of its rows is registered for one race.</summary>
    private sealed record FileKey(string File, string Call, Func<Race, (Archetype? A, Discipline? D)> Key);

    private static readonly FileKey[] Files =
    {
        new("fighter 1st", "ClassSkills.Register(race, BaseClass.Fighter, null", _ => (null, null)),
        new("mage 1st",    "ClassSkills.Register(race, BaseClass.Mage, null",    _ => (null, null)),
        new("tank 2nd",    "ClassSkills.Register(race, BaseClass.Fighter, Archetype.Tank",    _ => (Archetype.Tank, null)),
        new("warrior 2nd", "ClassSkills.Register(race, BaseClass.Fighter, Archetype.Warrior", _ => (Archetype.Warrior, null)),
        new("rogue 2nd",   "ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue",   _ => (Archetype.Rogue, null)),
        new("nuker 2nd",   "ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker",      _ => (Archetype.Nuker, null)),
        new("cleric 2nd",  "ClassSkills.Register(race, BaseClass.Mage, Archetype.Healer",     _ => (Archetype.Healer, null)),
        new("tank 3rd",    "ClassSkills.RegisterThird(race, Discipline.Bulwark",      _ => (Archetype.Tank, Discipline.Bulwark)),
        new("warrior 3rd", "ClassSkills.RegisterThird(race, Discipline.Ravager",      _ => (Archetype.Warrior, Discipline.Ravager)),
        new("war_aoe 3rd", "ClassSkills.RegisterThird(race, Discipline.Warlord",      _ => (Archetype.Warrior, Discipline.Warlord)),
        new("nuker 3rd",   "ClassSkills.RegisterThird(race, Discipline.Magus",        _ => (Archetype.Nuker, Discipline.Magus)),
        new("healer 3rd",  "ClassSkills.RegisterThird(race, Discipline.Lightbringer", _ => (Archetype.Healer, Discipline.Lightbringer)),
        new("buffer 3rd",  "ClassSkills.RegisterThird(race, Discipline.Warchanter",   _ => (Archetype.Healer, Discipline.Warchanter)),
        new("dual 3rd",    "ClassSkills.RegisterThird(race, Disciplines.Of(race, Archetype.Rogue).A", r => (Archetype.Rogue, Disciplines.Of(r, Archetype.Rogue).A)),
        new("archer 3rd",  "ClassSkills.RegisterThird(race, Disciplines.Of(race, Archetype.Rogue).B!.Value", r => (Archetype.Rogue, Disciplines.Of(r, Archetype.Rogue).B)),
        new("tank 4th",    "ClassSkills.RegisterFourth(race, Discipline.Bulwark",      _ => (Archetype.Tank, Discipline.Bulwark)),
        new("warrior 4th", "ClassSkills.RegisterFourth(race, Discipline.Ravager",      _ => (Archetype.Warrior, Discipline.Ravager)),
        new("war_aoe 4th", "ClassSkills.RegisterFourth(race, Discipline.Warlord",      _ => (Archetype.Warrior, Discipline.Warlord)),
        new("nuker 4th",   "ClassSkills.RegisterFourth(race, Discipline.Magus",        _ => (Archetype.Nuker, Discipline.Magus)),
        new("healer 4th",  "ClassSkills.RegisterFourth(race, Discipline.Lightbringer", _ => (Archetype.Healer, Discipline.Lightbringer)),
        new("buffer 4th",  "ClassSkills.RegisterFourth(race, Discipline.Warchanter",   _ => (Archetype.Healer, Discipline.Warchanter)),
        new("dual 4th",    "ClassSkills.RegisterFourth(race, Disciplines.Of(race, Archetype.Rogue).A", r => (Archetype.Rogue, Disciplines.Of(r, Archetype.Rogue).A)),
        new("archer 4th",  "ClassSkills.RegisterFourth(race, Disciplines.Of(race, Archetype.Rogue).B!.Value", r => (Archetype.Rogue, Disciplines.Of(r, Archetype.Rogue).B)),
    };

    private static readonly Race[] Races = { Race.Human, Race.Elf, Race.Demon };

    /// <summary>One authored row of a generated id.</summary>
    private sealed record Row(string File, int Line, int Level, string Id, string Name, string Weapon, string Weight,
                              string Descr, long Sp, long Gold, string[] RaceSet, string[] Replaces)
    {
        public string Sig = "";                  // the rung identity: stats + gates
        public SortedDictionary<string, float> Stats = new();
        public int Rung;
    }

    public static int Run(string csvDir, string repoRoot, bool baseSp)
    {
        var specs = Specs.ToDictionary(s => s.Id);
        var rows = new List<Row>();
        var errors = new List<string>();

        foreach (var fk in Files)
        {
            string path = Path.Combine(csvDir, fk.File + ".csv");
            var lines = File.ReadAllLines(path);
            int spCol = -1, goldCol = -1, raceCol = -1, repCol = -1; double spScale = 1;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) break;
                var c = SplitCsv(line);
                if (line.StartsWith("LEARN"))
                {
                    var h = c.Select(x => x.Trim().ToUpperInvariant()).ToList();
                    spCol = h.FindIndex(x => x.StartsWith("SP COST"));
                    goldCol = h.FindIndex(x => x == "GOLD" || x == "GOLD COST");
                    raceCol = h.FindIndex(x => x == "RACE");
                    repCol = h.FindIndex(x => x == "REPLACES");
                    spScale = h[spCol].Contains("X1000") ? 1000 : 1;
                    continue;
                }
                if (c.Count < 15 || !int.TryParse(c[0].Trim(), out int lvl)) continue;
                string id = c[2].Trim();
                if (!specs.ContainsKey(id)) continue;
                string Cell(int i) => i >= 0 && i < c.Count ? c[i].Trim() : "";
                var raceSet = Cell(raceCol).Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var rep = Cell(repCol).Trim('[', ']').Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                rows.Add(new Row(fk.File, li + 1, lvl, id, c[1].Trim(), c[4].Trim(), c[5].Trim(), c[12].Trim(),
                                 (long)Math.Round(Price(Cell(spCol)) * spScale), (long)Math.Round(Price(Cell(goldCol))),
                                 raceSet, rep));
            }
        }

        // ---- 1. read every row into stats + gates ------------------------------------------------------------
        foreach (var r in rows)
        {
            var spec = specs[r.Id];
            string where = $"{r.File}.csv:{r.Line} ({r.Id} @{r.Level})";
            try { r.Stats = ReadStats(spec, r.Descr); }
            catch (Exception e) { errors.Add($"{where}: {e.Message}"); continue; }
            var fields = spec.Kind is Kind.Armor ? StatModsField : PassiveField;
            foreach (var k in r.Stats.Keys)
                if (spec.Kind != Kind.Hand && !fields.ContainsKey(k))
                    errors.Add($"{where}: '{k}' has no {(spec.Kind is Kind.Armor ? "StatMods" : "PassiveEffect")} field");
            if (spec.Kind != Kind.Hand && !r.Stats.ContainsKey(spec.Order[0]))
                errors.Add($"{where}: no '{spec.Order[0]}' value read from \"{r.Descr}\"");
            if (spec.Kind is not Kind.Weapon && r.Weapon.Length > 0 && spec.Kind != Kind.Hand)
                errors.Add($"{where}: a WEAPON gate on a {spec.Kind} piece");
            r.Sig = string.Join(",", r.Stats.Select(kv => $"{kv.Key}={kv.Value.ToString("0.####", CultureInfo.InvariantCulture)}"))
                  + $"|W={Norm(r.Weapon)}|A={Norm(r.Weight)}";
        }
        if (errors.Count > 0) return Fail(errors);

        // ---- 2. one ladder per id: the distinct rungs, in order of their leading stat --------------------------
        var ladders = new Dictionary<string, List<Row>>();   // id → one representative row per rung, in rung order
        foreach (var g in rows.GroupBy(r => r.Id))
        {
            var spec = specs[g.Key];
            var distinct = g.GroupBy(r => r.Sig)
                .Select(s => s.OrderBy(r => r.Level).First())
                .OrderBy(r => spec.Kind == Kind.Hand ? 0 : Math.Abs(r.Stats[spec.Order[0]]))
                .ThenBy(r => g.Where(x => x.Sig == r.Sig).Min(x => x.Level))
                .ToList();
            for (int i = 0; i < distinct.Count; i++)
                foreach (var r in g.Where(x => x.Sig == distinct[i].Sig)) r.Rung = i + 1;
            ladders[g.Key] = distinct;
        }

        // ---- 3. every class must climb: its rung index rises with its learn level ------------------------------
        foreach (var fk in Files)
            foreach (var race in Races)
                foreach (var g in rows.Where(r => r.File == fk.File && Applies(r, race)).GroupBy(r => r.Id))
                {
                    int prev = 0, prevLvl = 0;
                    foreach (var r in g.OrderBy(r => r.Level))
                    {
                        if (r.Rung <= prev)
                            errors.Add($"{fk.File}.csv:{r.Line} ({r.Id}, {race}) rung {r.Rung} @{r.Level} is not above rung {prev} @{prevLvl} — a ladder dip or two classes' rungs crossing.");
                        prev = r.Rung; prevLvl = r.Level;
                    }
                }
        if (errors.Count > 0) return Fail(errors);

        // ---- 4. prices: the CSV cell IS the price (`BL-326`; the ×k of 0.215.0 is gone) ----------------------
        var basePrice = new Dictionary<Row, int>();
        foreach (var r in rows)
        {
            if (r.Sp > int.MaxValue) { errors.Add($"{r.File}.csv:{r.Line}: SP {r.Sp} does not fit an int"); continue; }
            basePrice[r] = (int)r.Sp;
        }
        if (errors.Count > 0) return Fail(errors);

        // ---- 5. write -----------------------------------------------------------------------------------------
        string defs = Path.Combine(repoRoot, "Game.Shared", "Skills", "Skills.PassiveLadders.g.cs");
        string tables = Path.Combine(repoRoot, "Game.Shared", "RaceAndClasses", "ClassSkillTables.Passives.g.cs");
        File.WriteAllText(defs, WriteDefs(specs, ladders, rows, basePrice));
        File.WriteAllText(tables, WriteTables(rows, ladders, basePrice));
        string prices = Path.Combine(repoRoot, "Game.Shared", "RaceAndClasses", "ClassSkillTables.SpPrices.g.cs");
        File.WriteAllText(prices, WriteSpPrices(csvDir));
        Console.WriteLine($"{rows.Count} rows → {ladders.Count} ladders, {ladders.Values.Sum(l => l.Count)} rungs.");
        foreach (var (id, l) in ladders.OrderBy(kv => kv.Key))
            Console.WriteLine($"  {id,-30} {l.Count,3} rung(s)  {string.Join("  ", l.Select(r => Show(specs[id], r)))}");
        Console.WriteLine($"wrote {Path.GetRelativePath(repoRoot, defs)}");
        Console.WriteLine($"wrote {Path.GetRelativePath(repoRoot, tables)}");
        Console.WriteLine("⚠ face descriptions read these numbers: build, then run `--gen-faces` (`--check` flags it STALE otherwise).");
        return 0;
    }

    private static int Fail(List<string> errors)
    {
        foreach (var e in errors) Console.Error.WriteLine("  🔴 " + e);
        Console.Error.WriteLine($"{errors.Count} error(s); nothing written.");
        return 1;
    }

    private static bool Applies(Row r, Race race) =>
        r.RaceSet.Length == 0 || r.RaceSet.Any(x => x.Equals(race.ToString(), StringComparison.OrdinalIgnoreCase));

    private static string Norm(string cell) => cell.Trim().ToLowerInvariant();

    private static string Show(Spec spec, Row r) => spec.Kind == Kind.Hand ? "(hand)" :
        r.Stats[spec.Order[0]].ToString("0.###", CultureInfo.InvariantCulture)
        + (r.Weapon.Length > 0 && spec.Id == "strength_mastery" ? "/" + r.Weapon : "");

    // ---- DESCR → stats -----------------------------------------------------------------------------------------

    private static readonly Regex WarriorRegen =
        new(@"hp regen \+([\d.]+);\s*when sitting hp regen \+([\d.]+),\s*mp regen \+([\d.]+)", RegexOptions.IgnoreCase);
    private static readonly Regex Cleave = new(@"in (\d+) range \(max (\d+) targets\)", RegexOptions.IgnoreCase);

    private static SortedDictionary<string, float> ReadStats(Spec spec, string descr)
    {
        var s = new SortedDictionary<string, float>();
        switch (spec.Id)
        {
            case "warrior_regeneration":
            {
                var m = WarriorRegen.Match(descr);
                if (!m.Success) throw new Exception($"cannot read \"{descr}\"");
                s["hpreg#"] = F(m.Groups[1].Value); s["hpsit#"] = F(m.Groups[2].Value); s["mpsit#"] = F(m.Groups[3].Value);
                return s;
            }
            case "blunt_cleave":
            {
                var m = Cleave.Match(descr);
                if (!m.Success) throw new Exception($"cannot read \"{descr}\"");
                s["cleaverad#"] = F(m.Groups[1].Value); s["cleave#"] = F(m.Groups[2].Value);
                return s;
            }
            case "magic_protection":
                if (!descr.StartsWith("Twice", StringComparison.OrdinalIgnoreCase)) throw new Exception($"cannot read \"{descr}\"");
                s["mfail#"] = 2f;
                return s;
            case "dual_weapon_prof":
                s["proc"] = 1f;
                return s;
        }
        foreach (var (key, v) in Descr.Values(descr))
        {
            var parts = key.Split('|');   // metric | % or # | scope
            string k = parts[0] + parts[1];
            if (s.TryGetValue(k, out var had) && had != v) throw new Exception($"two values for {k}: {had} and {v}");
            s[k] = v;
        }
        return s;
    }

    private static float F(string v) => float.Parse(v, CultureInfo.InvariantCulture);

    // ---- the C# ------------------------------------------------------------------------------------------------

    private const string Header =
        "// <auto-generated>\n" +
        "//   `BL-314` — GENERATED from docs/data/classes_skills_csv by\n" +
        "//     dotnet run --project tools/SkillCsvSeed -- --gen-passives\n" +
        "//   DO NOT EDIT BY HAND: edit the CSV row and regenerate. See tools/SkillCsvSeed/PassiveGen.cs.\n" +
        "// </auto-generated>\n";

    public static string Const(string id) =>
        string.Concat(id.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

    private static string WriteDefs(Dictionary<string, Spec> specs, Dictionary<string, List<Row>> ladders,
                                    List<Row> rows, Dictionary<Row, int> basePrice)
    {
        // ⚠ ONE METHOD PER LADDER, AND THE RUNGS AS PLAIN NUMBERS. The first cut built every rung as a
        //   `new PassiveEffect(...)` inside one array initialiser — 756 struct temporaries of ~450 bytes in a
        //   single frame, which overflowed the stack the moment the catalog loaded (and IL2CPP would have
        //   done the same on the phone). A RungRow is a small class on the heap; the builder lambda turns its
        //   numbers into the stat struct one rung at a time, inside Ladder's own loop.
        var sb = new StringBuilder(Header);
        sb.Append("namespace Game.Shared;\n\npublic static partial class SkillCatalog\n{\n");
        foreach (var id in ladders.Keys.OrderBy(x => x))
            sb.Append($"    public const string {Const(id)} = \"{id}\";\n");
        var gen = ladders.Where(kv => specs[kv.Key].Kind != Kind.Hand).OrderBy(kv => kv.Key).ToList();
        sb.Append("\n    /// <summary>Every generated passive ladder. Hand-written pieces (Kind.Hand) are not here.</summary>\n");
        sb.Append("    private static SkillDef[] PassiveLadderSkills() => new[]\n    {\n");
        foreach (var (id, _) in gen) sb.Append($"        Piece{Const(id)}(),\n");
        sb.Append("    };\n");
        foreach (var (id, rungs) in gen)
        {
            var spec = specs[id];
            var keys = rungs[0].Stats.Keys.ToList();
            if (rungs.Any(r => !r.Stats.Keys.SequenceEqual(keys)))
                throw new Exception($"{id}: its rungs carry different stats — one piece is one stat set");
            var map = spec.Kind == Kind.Armor ? StatModsField : PassiveField;
            string ctor = spec.Kind == Kind.Armor ? "StatMods" : "PassiveEffect";
            string make = $"r => new {ctor}({string.Join(", ", keys.Select((k, i) => $"{map[k]}: {Cast(map, map[k])}r.S[{i}]"))})";
            string builder = spec.Kind switch { Kind.Armor => "ArmorLadder", Kind.Plain => "PlainLadder", _ => "WeaponLadder" };
            string extra = id == "strength_mastery" ? ", payHighestGatedRung: true" : "";
            sb.Append($"\n    private static SkillDef Piece{Const(id)}() => {builder}({Const(id)}, {Q(CanonicalName(rows, id))}, BaseClass.{spec.Cls},\n");
            sb.Append($"        {Q(spec.Desc)},\n        {make}{extra},\n");
            for (int i = 0; i < rungs.Count; i++)
            {
                var r = rungs[i];
                string vals = string.Join(", ", keys.Select(k => r.Stats[k].ToString("0.####", CultureInfo.InvariantCulture) + "f"));
                string gate = spec.Kind == Kind.Weapon
                    ? (r.Weapon.Length > 0 ? $", Weapon: {Weapon(r.Weapon).Replace(", WeaponHands.", ", Hands: WeaponHands.")}" : "")
                    : (r.Weight.Length > 0 ? $", Armor: {Weights(r.Weight)}" : "");
                string text = Q(r.Descr.TrimEnd(';', ' ').Replace("  ", " "));
                sb.Append($"        new RungRow(new[] {{ {vals} }}, {basePrice[r]}, {r.Gold}, {text}{gate}){(i < rungs.Count - 1 ? "," : ");")}\n");
            }
        }
        sb.Append("}\n");
        return sb.ToString().Replace("\n", "\r\n");
    }

    /// <summary>StatMods is all floats; PassiveEffect carries these five as ints.</summary>
    private static string Cast(Dictionary<string, string> map, string field) =>
        map == PassiveField && field is "MagicDefence" or "PhysAtk" or "MagAtk" or "Accuracy" or "CleaveTargets" ? "(int)" : "";

    private static string CanonicalName(List<Row> rows, string id) =>
        rows.Where(r => r.Id == id).GroupBy(r => r.Name).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;

    private static string WriteTables(List<Row> rows, Dictionary<string, List<Row>> ladders, Dictionary<Row, int> basePrice)
    {
        var sb = new StringBuilder(Header);
        sb.Append("namespace Game.Shared;\n\nusing static Game.Shared.SkillCatalog;\n\n");
        sb.Append("public static partial class ClassSkillTables\n{\n");
        sb.Append("    /// <summary>Every class's learn rows for the generated passive ladders, file by file. SP is the ×1 base;\n");
        sb.Append("    /// the CSV cell, as authored (`BL-326`).</summary>\n");
        // One method per file: a single body of ~1,400 struct rows is the same frame-size risk the defs had.
        var used = Files.Where(fk => rows.Any(r => r.File == fk.File)).ToList();
        sb.Append("    private static void RegisterPassiveLadders()\n    {\n");
        foreach (var fk in used) sb.Append($"        Passives{Method(fk.File)}();\n");
        sb.Append("    }\n");
        foreach (var fk in used)
        {
            var fileRows = rows.Where(r => r.File == fk.File).OrderBy(r => r.Level).ThenBy(r => r.Id).ToList();
            sb.Append($"\n    /// <summary>`{fk.File}.csv`.</summary>\n    private static void Passives{Method(fk.File)}()\n    {{\n");
            // Rows for every race in one loop; race-specific rows in a call of their own.
            foreach (var group in fileRows.GroupBy(r => string.Join(";", r.RaceSet.Select(x => x.ToLowerInvariant()).OrderBy(x => x))))
            {
                string races = group.Key.Length == 0 ? "Races"
                    : "new[] { " + string.Join(", ", group.First().RaceSet.Select(x => "Race." + char.ToUpperInvariant(x[0]) + x[1..].ToLowerInvariant())) + " }";
                sb.Append($"        foreach (var race in {races})\n");
                sb.Append($"            {fk.Call},\n");
                var list = group.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    var r = list[i];
                    var args = new List<string> { Const(r.Id), r.Level.ToString(), $"SkillLevel: {r.Rung}", $"SpCost: {basePrice[r]}" };
                    if (r.Gold > 0) args.Add($"GoldCost: {r.Gold}");
                    if (r.Replaces.Length > 0) args.Add($"Replaces: new[] {{ {string.Join(", ", r.Replaces.Select(Q))} }}");
                    sb.Append($"                new ClassSkill({string.Join(", ", args)}){(i < list.Count - 1 ? "," : ");")}\n");
                }
            }
            sb.Append("    }\n");
        }
        sb.Append("}\n");
        return sb.ToString().Replace("\n", "\r\n");
    }

    private static string Method(string file) =>
        string.Concat(file.Split(' ', '_').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));

    private static string Weights(string cell)
    {
        if (!ArmorGate.TryParseRequirement(cell, out var w, out _, out var err, out _) || err is not null)
            throw new Exception($"bad WEIGHT '{cell}'");
        return w == ArmorWeights.None ? "ArmorWeights.None"
            : string.Join(" | ", new[] { ArmorWeights.Bare, ArmorWeights.Robe, ArmorWeights.Light, ArmorWeights.Heavy }
                                 .Where(x => (w & x) == x).Select(x => "ArmorWeights." + x));
    }

    private static string Weapon(string cell)
    {
        if (!WeaponTypes.TryParseRequirement(cell, out var t, out var hands, out var err, out _) || err is not null)
            throw new Exception($"bad WEAPON '{cell}'");
        string types = t == WeaponType.None ? "WeaponType.None"
            : string.Join(" | ", new[] { WeaponType.Sword, WeaponType.Blunt, WeaponType.Dual, WeaponType.Bow,
                                         WeaponType.TwoHandedSword, WeaponType.TwoHandedBlunt }
                                 .Where(x => (t & x) == x).Select(x => "WeaponType." + x));
        return $"{types}, WeaponHands.{hands}";
    }

    private static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    /// <summary>A price cell: `567`, `9.3`, `10.8k`, `3.3kk`, blank.</summary>
    private static double Price(string s)
    {
        s = s.Trim();
        int k = 0;
        while (s.Length > 0 && (s[^1] == 'k' || s[^1] == 'K')) { k++; s = s[..^1].TrimEnd(); }
        if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return 0;
        return v * Math.Pow(1000, k);
    }

    private static List<string> SplitCsv(string line)
    {
        var outp = new List<string>();
        var sb = new StringBuilder();
        bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else if (c == '"') q = false;
                else sb.Append(c);
            }
            else if (c == '"') q = true;
            else if (c == ',') { outp.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        outp.Add(sb.ToString());
        return outp;
    }
}
