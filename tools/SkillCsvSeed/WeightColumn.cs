using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--weight-column` — ADD THE `WEIGHT` COLUMN, once, to every skill CSV.  BL-107.
//
//  HIS GRAMMAR, 2026-08-29 — deliberately the SAME `[set]/[axis]` shape as `WEAPON`, on his own
//  instruction: *"I like it to do it same as 'weapon' column. 'heavy/shield' == heavy and shield
//  required … 'heavy|light' == heavy or light required"*:
//      "weight" -> weight1[|weight2…][/shield]
//        (empty)        no requirement — works in anything, naked included
//        heavy          heavy body armour; shield irrelevant
//        light|heavy    light OR heavy — robe and a bare torso get nothing
//        robe           robe only (every mage armour mastery)
//        /shield        a shield equipped, any armour (Shield Mastery rungs 1-2)
//        heavy/shield   heavy AND a shield (Shield Mastery's "+10% P.Def")
//        /noshield      supported by the engine; nothing authors one
//
//  🔑 `|` IS OR AND `/` IS AND, and that distinction is the whole reason the column is not just a set
//  of four words. A SHIELD IS NOT AN ARMOUR WEIGHT — it is a different equip slot that coexists with
//  every weight — so `heavy|shield` under an OR reading pays a robed character with a buckler the
//  very bonus he asked to confine to heavy. Same lesson as the hands token on `WEAPON`, one day later.
//  The rule lives in `ArmorGate`; this tool only reads and writes the cell.
//
//  🔑 WHY THE SCHEMA CHANGE. An armour condition is real, enforced code and was written ONLY in the
//  free-text DESCR ("Robe:", "with light", "When Sheild is equiped"). `--check` cannot compare prose,
//  so nothing in the repo could notice a gate and its row disagreeing — the same blindness that let
//  the elf Warchanter's Combo Mastery claim a weapon it could never proc with.
//
//  ⚠ IT INSERTS ONE FIELD AND TOUCHES NOTHING ELSE — the shared character-span splice
//  (`WeaponColumn.SpliceAt`), never a re-serialise, so quoting, spacing and CRLFs survive
//  byte-for-byte. Run `git diff --numstat docs/data/` after: every file should read N N.
//
//  ⚠ IT IS IDEMPOTENT AND REFUSES TO RUN TWICE. The header is the guard.
//
//  ⚠ BANNER ROWS GET THE COMMA TOO, and rows below a `NOT DONE` banner get the column with an EMPTY
//  cell. 🔑 A SEMANTIC pass may skip rows, a STRUCTURAL pass may NEVER — `--check` reads by index.
// =====================================================================================================

internal static class WeightColumn
{
    /// <summary>Where the new field goes: immediately AFTER `WEAPON` (index 4), so it becomes index 5
    /// and RANGE shifts 5 -> 6. The two gates sit together, ahead of the three targeting columns —
    /// what a skill DEMANDS, then where its effect lands.</summary>
    private const int InsertAt = 5;

    public static int Run(string csvDir)
    {
        var files = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"No CSVs under {csvDir}"); return 1; }

        int rows = 0, unknown = 0, skipped = 0, gated = 0;
        var review = new List<string>();

        foreach (string path in files)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) continue;

            if (lines[0].IndexOf(",WEIGHT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"  skip {Path.GetFileName(path)} — already has a WEIGHT column");
                skipped++;
                continue;
            }

            // 🔑 KEYED ON THE SKILL ID FIRST (his 2026-08-29 column), display name only as a fallback.
            // `--weapon-column` had to key on the NAME and needed a per-file class SCOPE to survive the
            // three different skills all displayed "Weapon Mastery"; the id has no such collision, so
            // the scope here is a fallback path rather than the main one.
            var (req, names) = BuildLookup(WeaponColumn.Scope(Path.GetFileNameWithoutExtension(path)));

            string? Lookup(string id, string name, int learn)
            {
                if (id.Length > 0 && req.TryGetValue(id, out var byId)) return Pick(byId, learn);
                if (names.TryGetValue(name, out var byName)) return Pick(byName, learn);
                return null;
            }

            static string? Pick(List<(int Learn, string Cell)> list, int learn)
            {
                if (list.Count == 0) return null;
                foreach (var e in list) if (e.Learn == learn) return e.Cell;
                var below = list.Where(e => e.Learn <= learn).OrderByDescending(e => e.Learn).ToList();
                return below.Count > 0 ? below[0].Cell : list.OrderBy(e => e.Learn).First().Cell;
            }

            bool stop = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0) continue;

                if (line.StartsWith("LEARN", StringComparison.Ordinal))
                {
                    lines[i] = WeaponColumn.SpliceAt(line, InsertAt, "WEIGHT");
                    continue;
                }

                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) stop = true;
                if (line.StartsWith('#')) continue;

                var spans = WeaponColumn.FieldSpans(line);
                if (spans.Count < InsertAt + 1) continue;

                string name = WeaponColumn.Field(line, spans, 1).Trim();
                if (name.Length == 0)
                {
                    lines[i] = WeaponColumn.SpliceAt(line, InsertAt, "");   // a section banner
                    continue;
                }

                string value;
                string id = WeaponColumn.Field(line, spans, 2).Trim();
                int learn = int.TryParse(WeaponColumn.Field(line, spans, 0).Trim(), out int lv) ? lv : -1;
                if (stop)
                {
                    value = "";                     // a draft row below his banner — his to fill in
                }
                else if (learn >= 0 && Lookup(id, name, learn) is string cell)
                {
                    value = cell;
                    if (cell.Length > 0) gated++;
                }
                else
                {
                    // No SkillDef for this rung — an unbuilt tier. EMPTY is the honest default: the
                    // game demands no armour for it because the game does not have it.
                    value = "";
                    unknown++;
                    review.Add($"  {Path.GetFileName(path),-18} {name,-32} lvl {learn}");
                }

                lines[i] = WeaponColumn.SpliceAt(line, InsertAt, value);
                rows++;
            }

            string raw = File.ReadAllText(path);
            string nl = raw.Contains("\r\n") ? "\r\n" : "\n";
            bool trailing = raw.EndsWith("\n");
            File.WriteAllText(path, string.Join(nl, lines) + (trailing ? nl : ""));
            Console.WriteLine($"  wrote {Path.GetFileName(path)}");
        }

        Console.WriteLine();
        Console.WriteLine($"{rows} row(s) given a WEIGHT cell ({gated} of them carry a real "
                        + $"requirement); {skipped} file(s) already had the column.");
        if (review.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {unknown} ROW(S) THE CATALOG COULD NOT ANSWER (unbuilt tiers) — left EMPTY ---");
            foreach (var r in review.Take(40)) Console.WriteLine(r);
            if (review.Count > 40) Console.WriteLine($"  … and {review.Count - 40} more");
        }
        return 0;
    }

    /// <summary>skill id -> cell at each learn level, plus the same keyed on DISPLAY NAME as a
    /// fallback for a row whose SKILL_ID cell is still blank. ⚠ A name that carries two different
    /// cells at one learn level is dropped from the name map — writing either would be a guess; the
    /// id map never has that problem, which is why it is consulted first.</summary>
    private static (Dictionary<string, List<(int Learn, string Cell)>> ById,
                    Dictionary<string, List<(int Learn, string Cell)>> ByName)
        BuildLookup((BaseClass Base, Archetype? Arch, Discipline[] Disc, bool Fourth)? scope)
    {
        var byId = new Dictionary<string, List<(int, string)>>(StringComparer.OrdinalIgnoreCase);
        var byName = new Dictionary<string, List<(int, string)>>(StringComparer.OrdinalIgnoreCase);
        var badNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(Dictionary<string, List<(int, string)>> map, HashSet<string>? bad,
                 string key, int learn, string cell)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<(int, string)>();
            int at = list.FindIndex(e => e.Item1 == learn);
            if (at < 0) { list.Add((learn, cell)); return; }
            if (!string.Equals(list[at].Item2, cell, StringComparison.Ordinal)) bad?.Add(key);
        }

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            if (scope is null)
            {
                foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                    foreach (var cs in WeaponColumn.AllClassSkills(race, bc)) Feed(race, bc, cs);
                continue;
            }
            var s = scope.Value;
            if (s.Arch is null)
                foreach (var cs in ClassSkills.ForClass(race, s.Base, null, null)) Feed(race, s.Base, cs);
            else if (s.Disc.Length == 0)
                foreach (var cs in ClassSkills.Cumulative(race, s.Base, s.Arch, null)) Feed(race, s.Base, cs, s.Arch);
            else
                foreach (var d in s.Disc)
                    foreach (var cs in ClassSkills.Cumulative(race, s.Base, s.Arch, d, s.Fourth))
                        Feed(race, s.Base, cs, s.Arch, d);
        }

        void Feed(Race race, BaseClass bc, ClassSkill cs, Archetype? a = null, Discipline? d = null)
        {
            if (SkillCatalog.Get(cs.SkillId) is not SkillDef def) return;
            string cell = CellFor(def, cs.SkillLevel);
            Add(byId, null, cs.SkillId, cs.LearnLevel, cell);
            Add(byName, badNames, def.Name, cs.LearnLevel, cell);
            Add(byName, badNames, ClassSkills.DisplayName(cs.SkillId, race, bc, a, d), cs.LearnLevel, cell);
        }

        foreach (string n in badNames) byName.Remove(n);
        return (byId, byName);
    }

    /// <summary>The WEIGHT cell the GAME carries for one rung — what must be true for this row to do
    /// ANYTHING at all.
    ///
    /// 🔑 THREE DIFFERENT PLACES FEED ONE COLUMN, exactly as they do on `WEAPON`. An ACTIVE gates
    /// through <c>SkillDef.RequiredArmor</c>/<c>RequiredShield</c>; an armour-mastery PASSIVE gates
    /// implicitly through the SLOTS its <see cref="ArmorMasteryProfile"/> fills; every other passive
    /// carries <c>PassiveEffect.RequiredArmor</c>/<c>RequiresShield</c>. To a player they are one
    /// sentence — *"this does nothing unless you are wearing X"* — so they are one column.
    ///
    /// ⚠ THE LOOSEST LAYER WINS, not the strictest. A rung may carry several PassiveEffects with
    /// different gates (`SkillLevel.ExtraPassives`): Shield Mastery's block rate needs only a shield
    /// while its "+10% P.Def" needs heavy as well. The cell answers "when does this row pay
    /// anything?", so it reads `/shield`; the per-weight detail belongs in DESCR, which is where he
    /// already writes it. Formatting the strictest gate would advertise a heavy-armour requirement on
    /// a skill a robed Warchanter uses every fight.</summary>
    public static string CellFor(SkillDef def, int level)
    {
        if (def.ArmorMasteryAt(level) is ArmorMasteryProfile am)
        {
            var w = MasteryWeights(am);
            // A profile that fills EVERY weight demands none of them.
            if (w == ArmorWeights.Any) w = ArmorWeights.None;
            return ArmorGate.Format(w, am.RequiredShield);
        }

        var weights = ArmorWeights.None;
        var shield = ShieldGate.Any;
        bool sawLayer = false, anyWeightFree = false, anyShieldFree = false;
        foreach (var pe in def.PassivesAt(level))
        {
            sawLayer = true;
            if (pe.RequiredArmor == ArmorWeights.None) anyWeightFree = true; else weights |= pe.RequiredArmor;
            if (!pe.RequiresShield) anyShieldFree = true;
        }
        if (!sawLayer || anyWeightFree) weights = ArmorWeights.None;
        if (sawLayer && !anyShieldFree) shield = ShieldGate.Required;

        // The skill's OWN gate is a hard one on the whole cast and narrows whatever the passives said.
        if (def.RequiredArmor != ArmorWeights.None)
            weights = weights == ArmorWeights.None ? def.RequiredArmor : (weights & def.RequiredArmor);
        if (def.RequiredShield != ShieldGate.Any) shield = def.RequiredShield;

        return ArmorGate.Format(weights, shield);
    }

    /// <summary>Which weights an armour mastery actually says something about — the union of the slots
    /// carrying a non-inert <see cref="StatMods"/>. ⚠ An omitted weight means "this skill has nothing
    /// to say about it" (the 2026-08-07 convention), which for a GATE is the same as "you get nothing
    /// here" — so the union IS the requirement.</summary>
    public static ArmorWeights MasteryWeights(ArmorMasteryProfile p)
    {
        var w = ArmorWeights.None;
        if (!p.Robe.Equals(default(StatMods))) w |= ArmorWeights.Robe;
        if (!p.Light.Equals(default(StatMods))) w |= ArmorWeights.Light;
        if (!p.Heavy.Equals(default(StatMods))) w |= ArmorWeights.Heavy;
        if (!p.None.Equals(default(StatMods))) w |= ArmorWeights.Bare;
        return w;
    }
}
