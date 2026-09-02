using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared;

// =====================================================================================================
//  `--learn-audit` — CAN THE CLASS ACTUALLY BUY THE RUNGS ITS OWN TABLE OFFERS IT?
//
//  Playtest 29, his find: *"the harmonist never learns serenity / vigor / vampiric rage / force /
//  insight"*. They are in his authored buffer kit, they are on the class table, and they are still
//  not purchasable.
//
//  🔑 THE CAUSE IS A `+1`. Both the Learn tab (`GameUi.Skills.BuildLearnTab`) and the server
//  (`GameLoopService.HandleLearnSkill`) computed the next purchase as `owned + 1` and then asked the
//  class table for exactly that rung. That is only correct for a ladder a class owns from rung 1 with
//  no holes. The buff singles are neither: a 3rd class CONTINUES the 2nd class's ladder (Serenity's
//  first buffer row is rung 2, Vampirism/Force/Insight rung 3, Vigor rung 4) and it takes every OTHER
//  rung as it climbs (Serenity 2 → 4 → 6). Every entry point above 1 whose lower rungs the 2nd class
//  does not teach is dead, and so is every rung past the first hole.
//
//  This walks the REAL class tables — every (race, archetype), every discipline that race actually
//  has, both tiers — and replays the buy chain. It reports what the old rule could never reach.
//  ⚠ It enumerates what EXISTS: `Disciplines.Of(race, archetype)` decides which disciplines a race
//  has, never the cross product of every race with every discipline.
//
//  Level gates are deliberately IGNORED here. The question is reachability at level 90 with infinite
//  SP; a rung that no level can ever buy is the bug being hunted.
// =====================================================================================================

internal static class LearnAudit
{
    /// <summary>Set by `--learn-audit --old` to replay the pre-fix `owned + 1` rule, so the report can
    /// be produced against either engine and the fix can be shown to close it.</summary>
    public static bool UseOldRule;

    public static int Run()
    {
        // (skillId, unreachable rungs) -> the classes that suffer it. Grouped, because one authored
        // buff single is shared by all three buffer disciplines and printing it three times reads as
        // three problems.
        var findings = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var detail = new Dictionary<string, string>(StringComparer.Ordinal);
        int classes = 0;

        foreach (var (label, list) in EveryClass())
        {
            classes++;
            foreach (var group in list.GroupBy(cs => cs.SkillId, StringComparer.Ordinal))
            {
                var rungs = group.Select(cs => cs.SkillLevel).Distinct().OrderBy(v => v).ToList();
                if (rungs.Count == 0) continue;

                // Replay the chain. `Reached` is the highest rung the engine would let you buy.
                int owned = StartingRung(group.Key);
                while (true)
                {
                    int next = UseOldRule
                        ? (rungs.Contains(owned + 1) ? owned + 1 : 0)
                        : rungs.FirstOrDefault(r => r > owned);
                    if (next == 0) break;
                    owned = next;
                }

                var dead = rungs.Where(r => r > owned).ToList();
                if (dead.Count == 0) continue;

                string key = group.Key;
                if (!findings.TryGetValue(key, out var who))
                    findings[key] = who = new SortedSet<string>(StringComparer.Ordinal);
                who.Add(label);
                detail[key] = $"authored {string.Join("/", rungs)}, "
                            + $"reachable {(owned == 0 ? "NONE" : "1.." + owned)}, "
                            + $"dead {string.Join("/", dead)}";
            }
        }

        Console.WriteLine($"--- LEARN AUDIT ({(UseOldRule ? "OLD `owned + 1` rule" : "current rule")}) "
                        + $"— {classes} real class/tier combinations walked ---");
        Console.WriteLine();

        if (findings.Count == 0)
        {
            Console.WriteLine("  ✅ every rung on every class table is reachable.");
            return 0;
        }

        foreach (var (id, who) in findings.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            string name = SkillCatalog.Get(id)?.Name ?? "(no def)";
            Console.WriteLine($"  🔴 {id,-32} \"{name}\"");
            Console.WriteLine($"       {detail[id]}");
            Console.WriteLine($"       {who.Count} class(es): {string.Join(", ", who)}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {findings.Count} skill(s) with rungs no character can ever buy.");
        return findings.Count == 0 ? 0 : 1;
    }

    /// <summary>Every class list a real character can hold, WITH THE TIERS IT CLIMBED THROUGH.
    ///
    /// <para>🔑 <c>ClassSkills.Cumulative</c> starts at the SECOND-class list — it does not include the
    /// base-class table. That is right for the Learn tab (a level-40 healer is not still shopping the
    /// base mage's shelf) but wrong for reachability: the rungs a mage bought at 7 and 14 are rungs he
    /// OWNS, and the 2nd class continues those ladders from where they stopped. Auditing a discipline
    /// against `Cumulative` alone reports every continued ladder as broken — 24 classes' worth of
    /// Anti-Magic that is in fact perfectly reachable. Union the tiers instead.</para>
    ///
    /// <para>⚠ It enumerates what EXISTS: <c>Disciplines.Of</c> decides which disciplines a race
    /// actually has, never the cross product.</para></summary>
    private static IEnumerable<(string Label, IReadOnlyList<ClassSkill> List)> EveryClass()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
            {
                var baseList = ClassSkills.ForClass(race, bc, null, null).ToList();
                yield return ($"{race} {bc}", baseList);

                foreach (Archetype a in Enum.GetValues<Archetype>())
                {
                    if (BaseOf(a) != bc) continue;
                    var second = ClassSkills.Cumulative(race, bc, a, null).ToList();
                    if (second.Count == 0) continue;
                    yield return ($"{race} {a}", baseList.Concat(second).ToList());

                    var (d1, d2) = Disciplines.Of(race, a);
                    foreach (var d in d2 is null ? new[] { d1 } : new[] { d1, d2.Value })
                    {
                        yield return ($"{race} {d}",
                            baseList.Concat(ClassSkills.Cumulative(race, bc, a, d)).ToList());
                        yield return ($"{race} {d} 4th",
                            baseList.Concat(ClassSkills.Cumulative(race, bc, a, d, fourth: true)).ToList());
                    }
                }
            }
    }

    /// <summary>Rung 1 of these is AUTO-GRANTED on login (<c>GameLoopService.AutoLearnCoreSkills</c>),
    /// never bought — so their class tables legitimately start at rung 2 and the walk must start from
    /// 1, not 0. Magic Bolt is the only ladder in that set; the rest of the auto-grants are single-rung
    /// passives with no ladder to climb.</summary>
    private static int StartingRung(string skillId) =>
        skillId == SkillCatalog.MagicBolt ? 1 : 0;

    /// <summary>The mirror of <c>ClassSkills.BaseOf</c>, which is private.</summary>
    private static BaseClass BaseOf(Archetype a) =>
        a is Archetype.Healer or Archetype.Nuker ? BaseClass.Mage : BaseClass.Fighter;
}
