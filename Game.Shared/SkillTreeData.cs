namespace Game.Shared;

// =====================================================================================================
//  THE SKILL TREE — `BL-330`, ONE builder for the page and the game (owner, 2026-10-01).
//
//  *"skill tree per race. U select a race then select fighter or mage and then from there onward"*, and
//  for the game: *"we just can make a full skill tree at any class master. When opening each time it
//  preselects whatever u have (u can change and compare with other classes)"*.
//
//  Built from the COMPILED tables (`ClassSkills`, `ClassCatalog`, `Disciplines`, `ClassNames`) and the
//  faces (`SkillFaces`), so it says what the game teaches and can never drift from it. The page
//  (`SkillCsvSeed --skill-tree` → docs/design/SkillTree.html) and the client's Skill Tree window both read
//  THIS, which is what keeps the two from disagreeing.
//
//  Each step of a path lists only what THAT step adds: the 1st class's own list; what a fighter or mage
//  keeps for life (race layer + grade, from `Cumulative` minus the base list); the 2nd class's list; the
//  3rd class's (its discipline list, again by difference); the 4th's own list. The all-classes 4th kit
//  is listed once (<see cref="SkillTreeData.Shared4th"/>). The STAT SWAPS and the SIGILS are in no path —
//  both are bought on their own shelf, the same for every race (his §120a: *"remove the skill swap and the
//  sigils -> add them as separate tab next to race"*).
//
//  Text is the face's own (TMP tags and all): the client renders them, the page strips them.
// =====================================================================================================

/// <summary>One rung as the tree shows it: the character level it is learned at, the rung, a name only
/// when that rung has its own (the Grade passives), and its text.</summary>
public sealed record SkillTreeRung(int Level, int Rung, string? Name, string Text);

/// <summary>One skill on one step: its face name, its kind, every rung that step teaches in learn order,
/// and an optional tag (who may buy a swap, which slot a sigil fills).</summary>
public sealed record SkillTreeSkill(string Id, string Name, SkillCategory Category, SkillTreeRung[] Rungs,
                                    string? Tag = null)
{
    public int FirstLevel => Rungs.Length > 0 ? Rungs[0].Level : 0;
}

/// <summary>A 3rd class and the 4th it ascends into.</summary>
public sealed record SkillTreeThird(Discipline Discipline, string ThirdName, string FourthName,
                                    SkillTreeSkill[] Third, SkillTreeSkill[] Fourth);

/// <summary>A 2nd class and the 3rd classes it opens.</summary>
public sealed record SkillTreeSecond(Archetype Archetype, string Name, string Blurb, SkillTreeSkill[] Second,
                                     SkillTreeThird[] Thirds);

/// <summary>Fighter or Mage, for one race.</summary>
public sealed record SkillTreeBase(BaseClass BaseClass, SkillTreeSkill[] First, SkillTreeSkill[] Life,
                                   SkillTreeSecond[] Seconds);

/// <summary>One sigil flavour's three, in slot order.</summary>
public sealed record SkillTreeSigils(string Name, SkillTreeSkill[] Skills);

public static class SkillTreeData
{
    public static readonly Race[] Races = { Race.Human, Race.Elf, Race.Demon };

    /// <summary>Every stat-swap id (the Warchanter's shelf holds all of them) — kept out of the 3rd-class lists.</summary>
    private static readonly HashSet<string> SwapIds =
        new(SkillCatalog.StatSwapsFor(BaseClass.Mage, Discipline.Warchanter));

    private static readonly Dictionary<Race, SkillTreeBase[]> RaceCache = new();
    private static SkillTreeSkill[]? _shared4th, _swaps;
    private static SkillTreeSigils[]? _sigils;

    /// <summary>Fighter and Mage for <paramref name="race"/>, every path down to the 4th class. Built once.</summary>
    public static SkillTreeBase[] For(Race race)
    {
        if (RaceCache.TryGetValue(race, out var cached)) return cached;
        var bases = new List<SkillTreeBase>();
        foreach (var bc in new[] { BaseClass.Fighter, BaseClass.Mage })
        {
            var lin1 = SkillFaces.Lineage(race, bc, null, null, false);
            var own1 = ClassSkills.ForClass(race, bc, null, null);
            var life = Minus(ClassSkills.Cumulative(race, bc, null, null), own1);

            var seconds = new List<SkillTreeSecond>();
            foreach (var sc in ClassCatalog.OptionsFor(race, bc))
            {
                var arch = sc.Archetype;
                var lin2 = SkillFaces.Lineage(race, bc, arch, null, false);
                var thirds = new List<SkillTreeThird>();
                var (a, b) = Disciplines.Of(race, arch);
                foreach (var d in b is Discipline bd ? new[] { a, bd } : new[] { a })
                {
                    var lin3 = SkillFaces.Lineage(race, bc, arch, d, false);
                    var lin4 = SkillFaces.Lineage(race, bc, arch, d, true);
                    var own3 = Minus(ClassSkills.Cumulative(race, bc, arch, d), ClassSkills.Cumulative(race, bc, arch, null))
                               .Where(cs => !SwapIds.Contains(cs.SkillId));
                    var own4 = ClassSkills.ForClass(race, bc, arch, d, fourth: true);
                    if (_shared4th is null)
                    {
                        var all4 = Minus(Minus(ClassSkills.Cumulative(race, bc, arch, d, true),
                                               ClassSkills.Cumulative(race, bc, arch, d)), own4)
                                   .Where(cs => !SkillCatalog.AllSigilIds.Contains(cs.SkillId));
                        _shared4th = Group(all4, null, null);
                    }
                    thirds.Add(new SkillTreeThird(d, ClassNames.Third(d, race), ClassNames.Fourth(d, race),
                                                  Group(own3, race, lin3), Group(own4, race, lin4)));
                }
                seconds.Add(new SkillTreeSecond(arch, sc.Name, ClassCatalog.ArchetypeBlurb(arch),
                                                Group(ClassSkills.ForClass(race, bc, arch, null), race, lin2),
                                                thirds.ToArray()));
            }
            bases.Add(new SkillTreeBase(bc, Group(own1, race, lin1), Group(life, race, lin1), seconds.ToArray()));
        }
        return RaceCache[race] = bases.ToArray();
    }

    /// <summary>The kit every 4th class learns once ascended (`shared 4th.csv`), sigils aside.</summary>
    public static SkillTreeSkill[] Shared4th()
    {
        if (_shared4th is null) For(Race.Human);
        return _shared4th!;
    }

    /// <summary>THE SWAPS: every swap once, its five ranks at 40, tagged with who may buy it
    /// (SkillCatalog.StatSwapsFor).</summary>
    public static SkillTreeSkill[] Swaps()
    {
        if (_swaps != null) return _swaps;
        var fighter = new HashSet<string>(SkillCatalog.StatSwapsFor(BaseClass.Fighter, null));
        var mage = new HashSet<string>(SkillCatalog.StatSwapsFor(BaseClass.Mage, null));
        return _swaps = Group(SwapIds.SelectMany(id => Enumerable.Range(1, 5)
                                  .Select(k => new ClassSkill(id, SkillCatalog.StatSwapLearnLevel, SkillLevel: k))),
                              null, null, keepOrder: true)
            .Select(s => s with
            {
                Tag = fighter.Contains(s.Id) && mage.Contains(s.Id) ? "Every class"
                    : fighter.Contains(s.Id) ? "Fighters and the Warchanter"
                    : mage.Contains(s.Id) ? "Mages, clerics and the Warchanter"
                    : "The Warchanter only",
            })
            .ToArray();
    }

    /// <summary>THE SIGILS, one group per flavour, its three in slot order (SigilsOfGroup sorts
    /// Attack/Defence/Support).</summary>
    public static SkillTreeSigils[] Sigils() =>
        _sigils ??= ((SkillCatalog.SigilFlavour[])Enum.GetValues(typeof(SkillCatalog.SigilFlavour)))
            .Select(f => new SkillTreeSigils(f.ToString(),
                Group(SkillCatalog.SigilsOfGroup(f).Select(id => new ClassSkill(id, SkillCatalog.SigilLearnLevel)),
                      null, null, keepOrder: true)
                    .Select((s, i) => s with { Tag = (SkillCatalog.SigilSlot)i + " slot" })
                    .ToArray()))
            .ToArray();

    /// <summary>The rows of <paramref name="all"/> that <paramref name="minus"/> does not have (a multiset
    /// difference keyed on skill, learn level and rung).</summary>
    private static List<ClassSkill> Minus(IEnumerable<ClassSkill> all, IEnumerable<ClassSkill> minus)
    {
        var left = minus.GroupBy(Key).ToDictionary(g => g.Key, g => g.Count());
        var result = new List<ClassSkill>();
        foreach (var cs in all)
        {
            var k = Key(cs);
            if (left.TryGetValue(k, out int n) && n > 0) { left[k] = n - 1; continue; }
            result.Add(cs);
        }
        return result;
    }

    private static (string, int, int) Key(ClassSkill cs) => (cs.SkillId, cs.LearnLevel, cs.SkillLevel);

    private static SkillTreeSkill[] Group(IEnumerable<ClassSkill> rows, Race? race, IReadOnlyList<string>? lineage,
                                          bool keepOrder = false)
    {
        var skills = rows.GroupBy(r => r.SkillId)
            .Select(g =>
            {
                var def = SkillCatalog.Get(g.Key);
                int top = g.Max(r => r.SkillLevel);
                var face = SkillFaces.For(g.Key, race, lineage);
                string name = SkillFaces.NameOf(face, g.Key, top);
                // One row per RUNG this step teaches, in learn order. Two rungs on one learn level (the swaps'
                // five at 40) stay two rows, and the reader labels them by rank.
                var rungs = g.GroupBy(r => r.SkillLevel)
                    .Select(rg => (l: rg.Min(r => r.LearnLevel), k: rg.Key))
                    .OrderBy(x => x.l).ThenBy(x => x.k)
                    .Select(x =>
                    {
                        string n = SkillFaces.NameOf(face, g.Key, x.k);
                        return new SkillTreeRung(x.l, x.k, n == name ? null : n,
                                                 SkillFaces.DescriptionOf(face, g.Key, x.k).Trim());
                    })
                    .ToArray();
                return new SkillTreeSkill(g.Key, name, def?.Category ?? SkillCategory.Physical, rungs);
            });
        return keepOrder ? skills.ToArray()
             : skills.OrderBy(s => s.FirstLevel).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
