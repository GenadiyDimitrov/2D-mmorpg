using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--weapon-column` — ADD THE `WEAPON` COLUMN, once, to every skill CSV.  BL-105.
//
//  HIS GRAMMAR, 2026-08-29, verbatim:
//      "weapon" -> weaponType1[|weaponType2|weaponType3][/hands]
//        - sword|blunt|bow      == (any sword or any blunt or bow)
//        - sword|blunt|bow/1    == (1 handed sword or 1 handed blunt or bow)
//        - duals                == only duals; 'duals/1' also parse as duals as it don't care for
//                                  hands - just mark it as typo-warning
//        - blunt                == any blunt (mace/maul/staff/wand)
//        - blunt/2              == 2h blunt (staff/maul)
//        - no '/1' or '/2' in that column means any weapon hands
//        - mark as errors anything different from '/1' or '/2' -> like '/' or '/3' or '/a'. They are
//          marked as errors and make the hands invalid.
//
//  🔑 THE HANDS TOKEN NARROWS THE TYPES, NOT THE EQUIPPED WEAPON — his `sword|blunt|bow/1` includes a
//  BOW, because a bow has no one-handed variant to narrow to. The whole rule is
//  `WeaponTypes.Resolve`; this tool only reads and writes the cell.
//
//  🔑 WHY IT IS WORTH A SCHEMA CHANGE. A skill's weapon requirement is real, enforced code, and until
//  today it was written ONLY in the free-text DESCR ("with 2h sword/blunt", "Require: Bow/Blunt",
//  "Blunt:"). `--check` cannot compare prose, so the column could never be verified — and that is
//  exactly how the elf Warchanter's Combo Mastery survived: his row said Bow/Blunt, the code said
//  Blunt alone, and nothing in the repo could notice they disagreed. A proc that never fires is
//  indistinguishable from a 3% roll that keeps missing. With this column it is one line of output.
//
//  ⚠ IT INSERTS ONE FIELD AND TOUCHES NOTHING ELSE — a character-span splice on the raw line, never a
//  re-serialise, so quoting, spacing and CRLFs survive byte-for-byte. Same discipline as
//  `--aoe-column` and `--retarget`; the CSVs have been corrupted twice by tools that read them and
//  wrote them back "helpfully". Run `git diff --numstat docs/data/` after — every file should read
//  N N with N = its line count.
//
//  ⚠ IT IS IDEMPOTENT AND REFUSES TO RUN TWICE. The header is the guard.
//
//  ⚠ BANNER ROWS GET THE COMMA TOO, and rows below a `NOT DONE` banner get the column with an EMPTY
//  cell. 🔑 THE RULE, learned the hard way on `--aoe-column`: a SEMANTIC pass may skip rows, a
//  STRUCTURAL pass may NEVER — `--check` reads by index, so one un-shifted row turns into a cascade
//  of invented mismatches (1,259 of them, that time).
// =====================================================================================================

internal static class WeaponColumn
{
    /// <summary>Where the new field goes: immediately AFTER `TYPE` (index 2), so it becomes index 3 and
    /// RANGE shifts 3 -> 4. It sits with what a skill IS and what it DEMANDS, ahead of the three
    /// targeting columns (RANGE / AOE / TARGET), which are about where the effect lands.</summary>
    private const int InsertAt = 3;

    public static int Run(string csvDir)
    {
        var files = Directory.GetFiles(csvDir, "*.csv").OrderBy(f => f).ToArray();
        if (files.Length == 0) { Console.Error.WriteLine($"No CSVs under {csvDir}"); return 1; }

        int rows = 0, unknown = 0, skipped = 0, gated = 0, collisions = 0;
        var review = new List<string>();
        var clash = new List<string>();

        foreach (string path in files)
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0) continue;

            // 🔴 THE LOOKUP IS BUILT PER FILE, FROM THAT FILE'S OWN CLASS. Building it once across
            // every class — which is what `--aoe-column` does — is WRONG here and was caught only by
            // reading the output: THREE different skills are all displayed "Weapon Mastery" (the
            // fighter's base one, the tank's, the rogue's), so a name-keyed global table gave
            // `rogue 2nd.csv` the TANK's cell, `sword|blunt/1`, for a passive whose own DESCR says
            // bow and dual. `--aoe-column` has the same collision and never showed it, because all
            // three carry a radius of 0 and the wrong answer equalled the right one.
            // 🔑 THE LESSON: a lookup keyed on a DISPLAY NAME needs a scope, and the file already
            // names its scope. `Scope` maps the filename to the class the way `Check.Specs` does.
            var (req, dupes) = BuildLookup(Scope(Path.GetFileNameWithoutExtension(path)));
            foreach (string d in dupes) { clash.Add($"  {Path.GetFileName(path),-18} {d}"); collisions++; }

            string? Lookup(string name, int learn)
            {
                if (!req.TryGetValue(name, out var list) || list.Count == 0) return null;
                foreach (var e in list) if (e.Learn == learn) return e.Cell;
                var below = list.Where(e => e.Learn <= learn).OrderByDescending(e => e.Learn).ToList();
                if (below.Count > 0) return below[0].Cell;
                return list.OrderBy(e => e.Learn).First().Cell;
            }

            if (lines[0].IndexOf(",WEAPON", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Console.WriteLine($"  skip {Path.GetFileName(path)} — already has a WEAPON column");
                skipped++;
                continue;
            }

            bool stop = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0) continue;

                if (line.StartsWith("LEARN", StringComparison.Ordinal))
                {
                    lines[i] = Splice(line, "WEAPON");
                    continue;
                }

                if (line.IndexOf("NOT DONE", StringComparison.OrdinalIgnoreCase) >= 0) stop = true;
                if (line.StartsWith('#')) continue;

                var spans = FieldSpans(line);
                if (spans.Count < InsertAt + 1) continue;

                string name = Field(line, spans, 1).Trim();
                if (name.Length == 0)
                {
                    // A section banner: no name, but its commas carry the layout.
                    lines[i] = Splice(line, "");
                    continue;
                }

                string value;
                int learn = int.TryParse(Field(line, spans, 0).Trim(), out int lv) ? lv : -1;
                if (stop)
                {
                    value = "";                     // a draft row below his banner — his to fill in
                }
                else if (learn >= 0 && Lookup(name, learn) is string cell)
                {
                    value = cell;
                    if (cell.Length > 0) gated++;
                }
                else
                {
                    // No SkillDef for this rung — an unbuilt tier. EMPTY is the honest default: the
                    // game demands no weapon for it because the game does not have it. ⚠ This differs
                    // from `--aoe-column`, which wrote 0: a 0 radius is a real value, whereas an
                    // invented weapon requirement would be a gate nobody authored.
                    value = "";
                    unknown++;
                    review.Add($"  {Path.GetFileName(path),-18} {name,-32} lvl {learn}");
                }

                lines[i] = Splice(line, value);
                rows++;
            }

            string raw = File.ReadAllText(path);
            string nl = raw.Contains("\r\n") ? "\r\n" : "\n";
            bool trailing = raw.EndsWith("\n");
            File.WriteAllText(path, string.Join(nl, lines) + (trailing ? nl : ""));
            Console.WriteLine($"  wrote {Path.GetFileName(path)}");
        }

        Console.WriteLine();
        Console.WriteLine($"{rows} row(s) given a WEAPON cell ({gated} of them carry a real "
                        + $"requirement); {skipped} file(s) already had the column.");
        if (clash.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {collisions} DISPLAY-NAME COLLISION(S) INSIDE ONE FILE'S OWN CLASS ---");
            Console.WriteLine("    Two different skills share a name AND a learn level here, with");
            Console.WriteLine("    different requirements. Written EMPTY rather than guessed.");
            foreach (string c in clash.Take(20)) Console.WriteLine(c);
        }
        if (review.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {unknown} ROW(S) THE CATALOG COULD NOT ANSWER (unbuilt tiers) — left EMPTY ---");
            foreach (var r in review.Take(40)) Console.WriteLine(r);
            if (review.Count > 40) Console.WriteLine($"  … and {review.Count - 40} more");
        }
        return 0;
    }

    /// <summary>What class a CSV filename is about — the same knowledge <c>Check.Specs</c> carries,
    /// needed here because a DISPLAY NAME is only unique inside one class's own kit.
    /// A null archetype means the base class alone (`fighter 1st` / `mage 1st`); an empty discipline
    /// list means "the archetype without a discipline"; `shared 4th` returns null for "every class",
    /// since its rows genuinely are learned by all of them.</summary>
    private static (BaseClass Base, Archetype? Arch, Discipline[] Disc, bool Fourth)? Scope(string file)
    {
        bool fourth = file.EndsWith("4th", StringComparison.OrdinalIgnoreCase);
        var none = Array.Empty<Discipline>();
        return file.ToLowerInvariant() switch
        {
            "fighter 1st" => (BaseClass.Fighter, (Archetype?)null, none, false),
            "mage 1st"    => (BaseClass.Mage,    (Archetype?)null, none, false),
            "tank 2nd"    => (BaseClass.Fighter, Archetype.Tank,    none, false),
            "warrior 2nd" => (BaseClass.Fighter, Archetype.Warrior, none, false),
            "rogue 2nd"   => (BaseClass.Fighter, Archetype.Rogue,   none, false),
            "nuker 2nd"   => (BaseClass.Mage,    Archetype.Nuker,   none, false),
            "cleric 2nd"  => (BaseClass.Mage,    Archetype.Healer,  none, false),
            "tank 3rd" or "tank 4th" =>
                (BaseClass.Fighter, Archetype.Tank, new[] { Discipline.Bulwark }, fourth),
            "warrior 3rd" or "warrior 4th" =>
                (BaseClass.Fighter, Archetype.Warrior, new[] { Discipline.Ravager }, fourth),
            "war_aoe 3rd" or "war_aoe 4th" =>
                (BaseClass.Fighter, Archetype.Warrior, new[] { Discipline.Warlord }, fourth),
            // The dual and archer files each cover THREE disciplines — the rogue's six are one race
            // each since the archer merge, and his file is per ROLE, not per race.
            "dual 3rd" or "dual 4th" =>
                (BaseClass.Fighter, Archetype.Rogue,
                 new[] { Discipline.Phantom, Discipline.Venomweaver, Discipline.Nullblade }, fourth),
            "archer 3rd" or "archer 4th" =>
                (BaseClass.Fighter, Archetype.Rogue,
                 new[] { Discipline.Sharpshooter, Discipline.Trapper, Discipline.Hunter }, fourth),
            "nuker 3rd" or "nuker 4th" =>
                (BaseClass.Mage, Archetype.Nuker, new[] { Discipline.Magus }, fourth),
            "healer 3rd" or "healer 4th" =>
                (BaseClass.Mage, Archetype.Healer, new[] { Discipline.Lightbringer }, fourth),
            "buffer 3rd" or "buffer 4th" =>
                (BaseClass.Mage, Archetype.Healer, new[] { Discipline.Warchanter }, fourth),
            _ => null,      // `shared 4th` and anything new: every class
        };
    }

    /// <summary>Name -> the requirement cell at each learn level, for ONE file's class. Also returns
    /// the names it refused to answer: a display name carrying two different cells at the same learn
    /// level is a genuine ambiguity, and writing either would be a guess.</summary>
    private static (Dictionary<string, List<(int Learn, string Cell)>>, List<string>)
        BuildLookup((BaseClass Base, Archetype? Arch, Discipline[] Disc, bool Fourth)? scope)
    {
        var req = new Dictionary<string, List<(int Learn, string Cell)>>(StringComparer.OrdinalIgnoreCase);
        var bad = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, int learn, string cell)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            key = key.Trim();
            if (!req.TryGetValue(key, out var list)) req[key] = list = new List<(int, string)>();
            int at = list.FindIndex(e => e.Learn == learn);
            if (at < 0) { list.Add((learn, cell)); return; }
            if (!string.Equals(list[at].Cell, cell, StringComparison.Ordinal))
                bad.Add($"{key} @ {learn}: '{list[at].Cell}' vs '{cell}'");
        }

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
        {
            if (scope is null)
            {
                foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                    foreach (var cs in AllClassSkills(race, bc)) Feed(race, bc, cs);
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
            Add(def.Name, cs.LearnLevel, cell);
            // ⚠ The per-class DisplayName override too — his files carry the flavour name, and a
            // class that renames a skill would otherwise miss every one of its rows.
            Add(ClassSkills.DisplayName(cs.SkillId, race, bc, a, d),
                cs.LearnLevel, cell);
        }

        foreach (string name in bad.Select(b => b.Split(" @ ")[0]).Distinct().ToList()) req.Remove(name);
        return (req, bad.ToList());
    }

    /// <summary>The WEAPON cell the GAME carries for one rung.
    ///
    /// 🔑 TWO DIFFERENT FIELDS FEED ONE COLUMN, and that is deliberate. An ACTIVE gates through
    /// `SkillDef.RequiredWeapon`/`RequiredHands`; a weapon-mastery PASSIVE carries its requirement on
    /// the per-rung <see cref="WeaponMasteryProfile"/> instead, and until this column existed those two
    /// were unrelated facts in unrelated places. To a player they are the same sentence — *"this does
    /// nothing unless you are holding X"* — so they are one column.
    ///
    /// ⚠ A mastery profile with NO RequiredWeapon still gates, implicitly, through its filled SLOTS:
    /// `BufferMastery` sets Blunt and Bow and leaves the rest inert, which IS "blunt or bow" even
    /// though `RequiredWeapon` is None. Reading the mask alone would have written an empty cell for
    /// every one of those — the reason this walks the slots when the mask is silent.</summary>
    public static string CellFor(SkillDef def, int level)
    {
        if (def.WeaponMasteryAt(level) is WeaponMasteryProfile wm)
        {
            var (types, hands) = MasteryRequirement(wm);
            if (types != WeaponType.None) return WeaponTypes.Format(types, hands);
        }
        return WeaponTypes.Format(def.RequiredWeapon, def.RequiredHands);
    }

    /// <summary>What a mastery profile actually demands: its explicit mask if it has one, otherwise
    /// the union of the slots it fills. ⚠ `Other` is NOT a type — it is the catch-all for "anything
    /// else, empty hand included", so a profile that fills it demands nothing and returns None.</summary>
    public static (WeaponType Types, WeaponHands Hands) MasteryRequirement(WeaponMasteryProfile wm)
    {
        if (wm.RequiredWeapon != WeaponType.None) return (wm.RequiredWeapon, wm.RequiredHands);
        if (!wm.Other.Equals(default(PassiveEffect))) return (WeaponType.None, WeaponHands.Any);

        WeaponType t = WeaponType.None;
        if (!wm.Sword.Equals(default(PassiveEffect))) t |= WeaponType.AnySword;
        if (!wm.Blunt.Equals(default(PassiveEffect))) t |= WeaponType.AnyBlunt;
        if (!wm.Bow.Equals(default(PassiveEffect)))   t |= WeaponType.Bow;
        if (!wm.Dual.Equals(default(PassiveEffect)))  t |= WeaponType.Dual;
        return (t, wm.RequiredHands);
    }

    /// <summary>Insert one field at <see cref="InsertAt"/> by splicing the raw line.</summary>
    private static string Splice(string line, string value)
    {
        var spans = FieldSpans(line);
        if (spans.Count < InsertAt)
            return line + new string(',', InsertAt - spans.Count) + "," + value;
        int at = spans[InsertAt - 1].End;
        return line[..at] + "," + value + line[at..];
    }

    /// <summary>Every rung any class of this (race, base) can learn, INCLUDING the 4th tier.
    /// ⚠ The 4th tier needs its own argument — see the same note on AoeColumn.</summary>
    private static IEnumerable<ClassSkill> AllClassSkills(Race race, BaseClass bc)
    {
        foreach (var cs in ClassSkills.ForClass(race, bc, null, null)) yield return cs;
        foreach (Archetype a in Enum.GetValues<Archetype>())
        {
            foreach (var cs in ClassSkills.Cumulative(race, bc, a, null)) yield return cs;
            foreach (Discipline d in Enum.GetValues<Discipline>())
            {
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d)) yield return cs;
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d, true)) yield return cs;
            }
        }
    }

    private readonly record struct Span(int Start, int End);

    private static List<Span> FieldSpans(string line)
    {
        var spans = new List<Span>();
        bool q = false;
        int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') i++;
                else if (c == '"') q = false;
            }
            else if (c == '"') q = true;
            else if (c == ',') { spans.Add(new Span(start, i)); start = i + 1; }
        }
        spans.Add(new Span(start, line.Length));
        return spans;
    }

    private static string Field(string line, List<Span> spans, int i) =>
        i < spans.Count ? line[spans[i].Start..spans[i].End].Trim('"') : "";
}
