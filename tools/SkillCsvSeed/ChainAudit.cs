using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--chain-audit` — HIS ID RULE, 2026-08-29:
//
//    *"A chain of classes (fighter/mage) should replace their weaker skills with newer or continuing
//     the line .. but cross chain should have different id's ... a mages 'weapon mastery' is only
//     named that way, the ID should be something like 'mage_weap_mastery' ... but a fighters 'weapon
//     mastery' should not match in id of any of the mages one."*
//
//  🔑 WHAT THE RULE ACTUALLY PROTECTS. A skill id is the unit of identity everywhere: `Replaces`,
//  the learned set a character persists, the skill bar, `LearnedSkills`. Two chains sharing one id
//  means one PassiveEffect, one ladder and one `Replaces` graph serving two classes that are supposed
//  to diverge — and the day either side is retuned, the other moves with it silently. A shared NAME
//  is fine and often right ("Weapon Mastery" reads correctly for both); a shared ID is not.
//
//  This reports three things, worst first:
//    🔴 CROSS-CHAIN ID   — one id learnable by a Fighter class AND a Mage class. The rule broken.
//    🔴 CROSS-CHAIN REPLACES — a skill whose `Replaces` names a skill from the other chain, which
//                              would make a class-change delete a skill it never had.
//    ⚠  DECLARED/TAUGHT MISMATCH — a def declared BaseClass.X taught to the other chain's table.
// =====================================================================================================

internal static class ChainAudit
{
    public static int Run()
    {
        // id -> which base classes actually LEARN it, from the class tables (not from the def's own
        // declaration, which is what we are checking against).
        var learners = new Dictionary<string, HashSet<BaseClass>>(StringComparer.Ordinal);
        var where = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var allFourth = new HashSet<string>(StringComparer.Ordinal);

        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
                foreach (var (cs, label) in AllWithLabel(race, bc))
                {
                    if (label.EndsWith("4th", StringComparison.Ordinal)) allFourth.Add($"{bc}/{label}");
                    if (!learners.TryGetValue(cs.SkillId, out var set))
                        learners[cs.SkillId] = set = new HashSet<BaseClass>();
                    set.Add(bc);
                    if (!where.TryGetValue(cs.SkillId, out var w))
                        where[cs.SkillId] = w = new HashSet<string>();
                    w.Add($"{bc}/{label}");
                }

        // 🔑 ALL-CLASS CONTENT IS NOT A VIOLATION, and it must be separated automatically rather than
        // by a hand-written allowlist that would rot. `shared 4th.csv` (his own ALL-CLASSES block) plus
        // the eighteen Sigils are learned by EVERY ascended class on purpose — they are the 4th tier's
        // shared layer, not one chain reaching into the other. The test is derived, not listed: an id
        // taught to every 4th-tier class in BOTH chains is shared by design; an id taught to only SOME
        // classes on the other side is one chain borrowing from the other, which is what his rule is
        // about.
        var everyFourth = new HashSet<string>(
            where.Where(kv => kv.Key is not null)
                 .Select(kv => kv.Key)
                 .Where(id => allFourth.All(c => where[id].Contains(c))),
            StringComparer.Ordinal);

        int problems = 0, shared = 0;
        Console.WriteLine("--- 🔴 CROSS-CHAIN IDS (one id learned by BOTH a Fighter and a Mage class) ---");
        foreach (var (id, set) in learners.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (set.Count < 2) continue;
            if (everyFourth.Contains(id)) { shared++; continue; }
            string name = SkillCatalog.Get(id)?.Name ?? "(no def)";
            Console.WriteLine($"  {id,-34} \"{name}\"");
            Console.WriteLine($"      {string.Join(", ", where[id].OrderBy(s => s))}");
            problems++;
        }
        if (problems == 0) Console.WriteLine("  none.");
        Console.WriteLine($"  ({shared} id(s) excluded as ALL-CLASS 4th-tier content — `shared 4th` + the Sigils.)");

        Console.WriteLine();
        Console.WriteLine("--- 🔴 CROSS-CHAIN `Replaces` (a skill retiring one from the other chain) ---");
        int repl = 0;
        foreach (var def in SkillCatalog.AllSkills.OrderBy(d => d.Id, StringComparer.Ordinal))
            foreach (string r in def.Replaces ?? Array.Empty<string>())
            {
                if (SkillCatalog.Get(r) is not SkillDef other) continue;
                if (other.Class == def.Class) continue;
                Console.WriteLine($"  {def.Id} ({def.Class}) replaces {r} ({other.Class})");
                repl++;
            }
        if (repl == 0) Console.WriteLine("  none.");

        Console.WriteLine();
        Console.WriteLine("--- ⚠ DECLARED vs TAUGHT (a def's BaseClass against the tables that teach it) ---");
        int decl = 0;
        foreach (var (id, set) in learners.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (SkillCatalog.Get(id) is not SkillDef def) continue;
            foreach (var bc in set)
                if (def.Class != bc)
                {
                    Console.WriteLine($"  {id,-34} declared {def.Class}, taught to {bc} " +
                                      $"({string.Join(", ", where[id].Where(w => w.StartsWith(bc.ToString())))})");
                    decl++;
                }
        }
        if (decl == 0) Console.WriteLine("  none.");

        Console.WriteLine();
        Console.WriteLine($"{problems} cross-chain id(s), {repl} cross-chain Replaces, {decl} declared/taught mismatch(es).");
        return 0;
    }

    /// <summary>Every rung a REAL class of this (race, base) learns.
    ///
    /// 🔴 ONLY LEGAL COMBINATIONS. The first version of this walked
    /// `BaseClass × Archetype × Discipline` as a cross product and reported 44 "cross-chain ids" — every
    /// one of them invented. `ClassSkills.Cumulative` resolves the archetype's own base class
    /// internally (`BaseOf`: Healer/Nuker are Mage, the rest Fighter) and IGNORES the base class you
    /// pass, so asking it for "Mage/Tank" hands back the FIGHTER's tank kit and the audit then
    /// cheerfully concludes that a Mage learns `tank_weapon_mastery`.
    /// 🔑 Same mistake as the display-name lookup in `WeaponColumn`, one day apart: enumerate what
    /// EXISTS, never a cross product, and check the tool before believing what it says about the code.
    /// An archetype belongs to exactly one base class, and a discipline to exactly one archetype
    /// (`Disciplines.Of`, which is race-aware).</summary>
    private static IEnumerable<(ClassSkill, string)> AllWithLabel(Race race, BaseClass bc)
    {
        foreach (var cs in ClassSkills.ForClass(race, bc, null, null)) yield return (cs, "base");
        foreach (Archetype a in Enum.GetValues<Archetype>())
        {
            if (BaseOf(a) != bc) continue;
            foreach (var cs in ClassSkills.Cumulative(race, bc, a, null)) yield return (cs, a.ToString());

            var (d1, d2) = Disciplines.Of(race, a);
            foreach (var d in d2 is null ? new[] { d1 } : new[] { d1, d2.Value })
            {
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d)) yield return (cs, d.ToString());
                foreach (var cs in ClassSkills.Cumulative(race, bc, a, d, true)) yield return (cs, d + "4th");
            }
        }
    }

    /// <summary>The mirror of <c>ClassSkills.BaseOf</c>, which is private. Healer and Nuker are the
    /// Mage chain; Tank, Warrior, Rogue and Archer are the Fighter chain.</summary>
    private static BaseClass BaseOf(Archetype a) =>
        a is Archetype.Healer or Archetype.Nuker ? BaseClass.Mage : BaseClass.Fighter;
}
