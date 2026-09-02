namespace Game.Shared;

/// <summary>One skill a class can learn, and the level at which it becomes
/// learnable. SpCost comes from the SkillDef; level gates visibility in the
/// "Skills to Learn" tab.</summary>
/// <summary>One skill a class can learn at LearnLevel. DisplayName/Icon are
/// OPTIONAL per-class presentation overrides — the underlying skill id, effect,
/// and BuffKey stay shared, but this class sees its own name/icon on the skill
/// bar, buff bar and skills window. Leave them null to use the SkillDef's
/// canonical name.</summary>
/// <param name="SpCost">OPTIONAL per-class SP price for this rung. Null = the SkillDef's own
/// <c>SpCostAt(SkillLevel)</c>, which is what almost every entry wants. It exists because SP in this
/// game is priced by the LEVEL YOU LEARN AT, not by the ability: Shield Mastery is ONE skill that the
/// tank buys at 20/28/36/52 for 3200/3200/40000/74000 and the Human Warchanter buys at 40/60/70 for
/// 36000/120000/390000 (his `tank 2nd`, `tank 3rd` and `buffer 3rd` CSVs, 2026-08-21). Splitting that
/// into two SkillDefs would duplicate a ladder he authored identically in both files and invite it to
/// drift; overriding the price keeps one ability with one set of magnitudes.</param>
public readonly record struct ClassSkill(
    string SkillId, int LearnLevel, string? DisplayName = null, string? Icon = null,
    int SkillLevel = 1, int? SpCost = null)
{
    /// <summary>What THIS class pays for THIS rung — the per-class override when it has one, the
    /// skill's own authored price otherwise. Every SP reader goes through here or through
    /// <see cref="ClassSkills.SpCostOf"/>; reading <c>def.SpCostAt</c> directly is now the bug.</summary>
    public int SpCostFor(SkillDef def) => SpCost ?? def.SpCostAt(SkillLevel);
}

/// <summary>
/// THE place to manage which class learns which skill, and when. The actual
/// per-class lists live in the partial files under RaceAndClasses/ (e.g.
/// Classes.Human.Mage.cs) which call Register(...) in their static initializer.
///
/// Keyed by a ClassKey of (Race, BaseClass, Archetype?). Archetype null = the
/// base class (before the level-20 change). When 3rd/4th classes arrive they'll
/// extend ClassKey with a tier; for now archetype identifies the second class.
/// </summary>
public static class ClassSkills
{
    /// <summary>Discipline = null identifies the base class (archetype null) or the
    /// 2nd class (archetype set). A non-null Discipline identifies a 3rd class — its
    /// Archetype is the parent archetype, so the key stays unambiguous.</summary>
    /// <param name="Fourth">TRUE = the FOURTH-tier list for that discipline. ⚠ This field is the whole
    /// reason a 4th-class kit can exist at all: a 4th class is *the same discipline awakened*
    /// (<see cref="FourthClassDef"/>), so without a tier on the key its skills would be registered
    /// against <c>Discipline.Lightbringer</c> and leak to every level-40 Lightbringer. The 4th kit has
    /// its own key and is only ever unioned in when the character has actually ascended.</param>
    public readonly record struct ClassKey(Race Race, BaseClass Base, Archetype? Archetype,
                                           Discipline? Discipline, bool Fourth = false);

    private static readonly Dictionary<ClassKey, List<ClassSkill>> Map = new();

    /// <summary>Called by per-class files to register a 2nd-class (or base-class)
    /// skill list. Safe to call multiple times for the same key (appends).</summary>
    public static void Register(Race race, BaseClass baseClass, Archetype? archetype,
        params ClassSkill[] skills) =>
        RegisterKey(new ClassKey(race, baseClass, archetype, null), skills);

    /// <summary>Register a 3rd-class (discipline) skill list. Base + parent archetype
    /// are derived from the discipline, so callers only name (race, discipline).</summary>
    public static void RegisterThird(Race race, Discipline discipline, params ClassSkill[] skills)
    {
        var archetype = Disciplines.Parent(discipline);
        RegisterKey(new ClassKey(race, BaseOf(archetype), archetype, discipline), skills);
    }

    /// <summary>Register a 4th-class (ascended discipline) skill list — the 76-90 kit off his
    /// `*.4th.csv` files. Same (race, discipline) as the 3rd class; the TIER on the key is what keeps
    /// it away from a level-40 who has not paid the Rite of Ascension.</summary>
    public static void RegisterFourth(Race race, Discipline discipline, params ClassSkill[] skills)
    {
        var archetype = Disciplines.Parent(discipline);
        RegisterKey(new ClassKey(race, BaseOf(archetype), archetype, discipline, Fourth: true), skills);
    }

    /// <summary>The `shared 4th.csv` kit — EVERY class learns these on ascension, whatever its
    /// discipline (his file: *"ALL CLASSES"*, and *"i created a Shared 4th file that all classes share
    /// the same skills - every class get to learn them"*). Kept as ONE flat list rather than fanned out
    /// across 36 (race, discipline) keys: cheaper, and impossible to get half-right.</summary>
    private static readonly List<ClassSkill> FourthShared = new();

    /// <summary>Add rows to the all-classes 4th-tier kit. Called from ClassSkillTables.Fourth.cs.</summary>
    public static void RegisterFourthShared(params ClassSkill[] skills) => FourthShared.AddRange(skills);

    private static void RegisterKey(ClassKey key, ClassSkill[] skills)
    {
        if (!Map.TryGetValue(key, out var list))
            Map[key] = list = new List<ClassSkill>();
        list.AddRange(skills);
    }

    private static BaseClass BaseOf(Archetype a) =>
        a is Archetype.Healer or Archetype.Nuker ? BaseClass.Mage : BaseClass.Fighter;

    /// <summary>Ensure every per-class file's static constructor has run. The
    /// partial files live in a separate type (ClassSkillTables) whose static
    /// ctor does the Register calls; touching it triggers them.</summary>
    private static bool _initialized;
    private static void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        ClassSkillTables.Touch();
    }

    /// <summary>The skills registered for exactly one tier of a class (the 2nd-class
    /// list when discipline is null, the 3rd-class list when it is set).</summary>
    public static IReadOnlyList<ClassSkill> ForClass(Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        EnsureInit();
        var key = new ClassKey(race, baseClass, archetype, discipline, fourth);
        return Map.TryGetValue(key, out var list) ? list : Array.Empty<ClassSkill>();
    }

    /// <summary>Every skill a character can learn at its current tier: the 2nd-class
    /// list always, PLUS the 3rd-class discipline list once a discipline is chosen.
    /// This is what all the learn/display helpers below search.</summary>
    public static IEnumerable<ClassSkill> Cumulative(Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline, bool fourth = false)
    {
        foreach (var cs in ForClass(race, baseClass, archetype, null))
            yield return cs;
        if (discipline is Discipline d)
            foreach (var cs in ForClass(race, baseClass, archetype, d))
                yield return cs;
        // Armor-weight mastery passives, injected centrally by archetype (so we don't
        // edit all 18 per-class files). Same across races; the effect is class-driven.
        foreach (var cs in MasterySkills(baseClass, archetype))
            yield return cs;
        // The STAT-SWAP passives — GATED ON THE 3RD CLASS (owner, 2026-07-15): they appear only once
        // you've taken your 3rd-class discipline, not merely at level 40. `discipline` is non-null iff
        // a 3rd class is held, so injecting them only then is the whole gate. Which swaps a class may
        // take is per-class (SkillCatalog.StatSwapsFor). The GOLD price (1kk…5kk) is the real cost.
        if (discipline is not null)
            foreach (var id in SkillCatalog.StatSwapsFor(baseClass, discipline))
                for (int lvl = 1; lvl <= 5; lvl++)
                    yield return new ClassSkill(id, SkillCatalog.StatSwapLearnLevel, SkillLevel: lvl);

        // ═══ THE FOURTH TIER ══════════════════════════════════════════════════════════════════════
        // Only once the character has ASCENDED (paid the 100kk Rite at Archmaster Sevrin). Level 76
        // alone is NOT enough — that is the entire point of the flag, and of the tier on ClassKey.
        if (fourth && discipline is Discipline fd)
        {
            foreach (var cs in ForClass(race, baseClass, archetype, fd, fourth: true))
                yield return cs;
            foreach (var cs in FourthShared)      // his `shared 4th.csv` — every class, same rows
                yield return cs;
            // The SIGILS are the shared kit's second block, injected from the catalog rather than
            // listed as learn lines, for the same reason the stat swaps are: they are a fixed grid
            // (6 class flavours × 3 slots) bought on their OWN tab, not from Learn. See Skills.Sigils.cs.
            foreach (var id in SkillCatalog.AllSigilIds)
                yield return new ClassSkill(id, SkillCatalog.SigilLearnLevel);
        }
    }

    /// <summary>The armor-mastery passives a class can learn, with learn levels.
    /// Base classes train their natural weight from level 1; second classes gain
    /// their archetype's weight(s) at the class-change level. The mastery only does
    /// something while that weight is worn (see <see cref="ArmorMastery"/>).</summary>
    private static IEnumerable<ClassSkill> MasterySkills(BaseClass baseClass, Archetype? archetype)
    {
        const int second = GameConstants.ClassChangeLevel;   // 20
        switch (archetype)
        {
            case null:   // base class, before the level-20 change
                // Nothing. Neither base class has a level-1 mastery: the fighter learns its Armor
                // Mastery from the class table at 5, the mage his Robe Armor Mastery at 7.
                //
                // ⚠ `MasteryRobe` used to be yielded here at level 1 — the leftover that caused
                // playtest-20 `57b`: Robe Armor Mastery L1 appeared in BOTH the level-1 and the
                // level-7 learn groups, and buying either made the other vanish while the level-14
                // rung appeared. The 2026-08-07 mastery restructure made it a bonus-only skill
                // bought off the class table at 7/14 and stopped auto-granting it server-side
                // (see the note above the robe clamp in GameLoopService.AutoLearnCoreSkills), but
                // this line kept advertising it at 1. Don't re-add it: it is also the skill a
                // nuker/cleric mastery `Replaces`, and a stray level-1 copy wins the pick in
                // RecomputeDerived by dictionary order and erases their bonuses.
                break;
            // 2nd classes use DATA-DRIVEN per-archetype Armor Mastery skills (one skill,
            // its effect depends on the worn weight; replaces the old split masteries) PLUS
            // a weapon-conditional Weapon Mastery (its effect depends on the held weapon).
            case Archetype.Tank:
                yield return new ClassSkill(SkillCatalog.TankArmorMastery, second);
                yield return new ClassSkill(SkillCatalog.TankWeaponMastery, second);
                break;
            case Archetype.Warrior:
                yield return new ClassSkill(SkillCatalog.WarriorArmorMastery, second);
                yield return new ClassSkill(SkillCatalog.WarriorWeaponMastery, second);
                break;
            case Archetype.Rogue:
                yield return new ClassSkill(SkillCatalog.RogueArmorMastery, second);
                yield return new ClassSkill(SkillCatalog.RogueWeaponMastery, second);
                break;
            // (Archetype.Archer had its own pair here until 2026-08-07. Both ids were deleted with
            //  playtest-19 `0a`/G1: no 2nd class has carried Archer since the archer→rogue merge, and
            //  a bow character takes the ROGUE masteries above — they already hold the bow profiles.)
            case Archetype.Nuker:
                // Mages get NO weapon-type mastery — armor mastery + the flat atk passive
                // carry their identity; weapon type is irrelevant for casters.
                yield return new ClassSkill(SkillCatalog.NukerArmorMastery, second);
                break;
            case Archetype.Healer:
                // Healer's data-driven Armor Mastery is registered in its own class table.
                // No weapon-type mastery (mage) — same reasoning as the nuker above.
                break;
        }
    }

    /// <summary>Skills whose LearnLevel &lt;= the character's level — i.e. the
    /// ones currently offered in the "Skills to Learn" tab (before SP/learned
    /// filtering). Includes 3rd-class skills once a discipline is set.</summary>
    public static IEnumerable<ClassSkill> LearnableAt(Race race, BaseClass baseClass,
        Archetype? archetype, int level, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (level >= cs.LearnLevel)
                yield return cs;
    }

    /// <summary>The character-level at which a class can learn a specific SKILL LEVEL
    /// (0 if that (skill, level) isn't on the class list).</summary>
    public static int LearnLevelOf(string skillId, int skillLevel, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId && cs.SkillLevel == skillLevel)
                return cs.LearnLevel;
        return 0;
    }

    /// <summary>The SP this class pays to take <paramref name="def"/> to <paramref name="skillLevel"/>.
    /// Falls back to the skill's own authored price when the class table carries no override.
    /// See <see cref="ClassSkill.SpCost"/> for why an override exists at all.</summary>
    public static int SpCostOf(SkillDef def, int skillLevel, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == def.Id && cs.SkillLevel == skillLevel && cs.SpCost is int sp)
                return sp;
        return def.SpCostAt(skillLevel);
    }

    /// <summary>The next rung of <paramref name="skillId"/> this class may BUY, given the rung it
    /// already owns: the LOWEST class-table level strictly above <paramref name="owned"/>, or 0 when
    /// the shelf holds nothing further.
    ///
    /// <para>🔑 <b>IT IS NOT <c>owned + 1</c>.</b> That was the rule until 2026-09-02, and it silently
    /// killed twelve of his authored buff singles across fifteen classes. A class table is a SHELF, not
    /// a staircase, and it is authored two ways that `+1` cannot read:</para>
    /// <list type="bullet">
    ///   <item>It may START above rung 1. The single-buff ladders are shared with the CONSUMABLES —
    ///     rung 1 of Force/Ward/Aim is the potion, and the cleric's first row is rung 2. Nobody learns
    ///     rung 1, so `owned + 1` asked for a rung no class table has and got "your class cannot learn
    ///     this" for a skill sitting right there on his CSV.</item>
    ///   <item>It may SKIP rungs as it climbs. The Warchanter takes Serenity at 2 → 4 → 6 and Insight
    ///     at 3 → 6; the rungs between are other classes'. `owned + 1` stalled at the first hole and
    ///     called the rest "cannot be raised further".</item>
    /// </list>
    ///
    /// <para>⚠ Both halves are DELIBERATE authoring, so this is the engine bending to the data and not
    /// the other way round. `tools/SkillCsvSeed --learn-audit` walks every real class and asserts every
    /// authored rung is reachable; `--learn-audit --old` replays the broken rule.</para></summary>
    public static int NextLearnableLevel(string skillId, int owned, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        int next = 0;
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId && cs.SkillLevel > owned && (next == 0 || cs.SkillLevel < next))
                next = cs.SkillLevel;
        return next;
    }

    /// <summary>The highest skill-level of a skill this class can ever learn (0 = none).</summary>
    public static int MaxClassLevelOf(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        int max = 0;
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId && cs.SkillLevel > max)
                max = cs.SkillLevel;
        return max;
    }

    /// <summary>Can this class ever learn this skill at all?</summary>
    public static bool CanClassLearn(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId)
                return true;
        return false;
    }

    /// <summary>The class-specific display name for a skill (falls back to the
    /// SkillDef's canonical name). Same shared id, different label per class.</summary>
    public static string DisplayName(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.DisplayName))
                return cs.DisplayName!;
        return SkillCatalog.Get(skillId)?.Name ?? skillId;
    }

    /// <summary>The class-specific icon key for a skill (null if none set).</summary>
    public static string? Icon(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null, bool fourth = false)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline, fourth))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.Icon))
                return cs.Icon;
        return null;
    }
}
