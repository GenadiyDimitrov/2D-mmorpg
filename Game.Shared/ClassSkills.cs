namespace Game.Shared;

/// <summary>One skill a class can learn, and the level at which it becomes
/// learnable. SpCost comes from the SkillDef; level gates visibility in the
/// "Skills to Learn" tab.</summary>
/// <summary>One skill a class can learn at LearnLevel. DisplayName/Icon are
/// OPTIONAL per-class presentation overrides — the underlying skill id, effect,
/// and BuffKey stay shared, but this class sees its own name/icon on the skill
/// bar, buff bar and skills window. Leave them null to use the SkillDef's
/// canonical name.</summary>
public readonly record struct ClassSkill(
    string SkillId, int LearnLevel, string? DisplayName = null, string? Icon = null,
    int SkillLevel = 1);

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
    public readonly record struct ClassKey(Race Race, BaseClass Base, Archetype? Archetype, Discipline? Discipline);

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
        Archetype? archetype, Discipline? discipline = null)
    {
        EnsureInit();
        var key = new ClassKey(race, baseClass, archetype, discipline);
        return Map.TryGetValue(key, out var list) ? list : Array.Empty<ClassSkill>();
    }

    /// <summary>Every skill a character can learn at its current tier: the 2nd-class
    /// list always, PLUS the 3rd-class discipline list once a discipline is chosen.
    /// This is what all the learn/display helpers below search.</summary>
    public static IEnumerable<ClassSkill> Cumulative(Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline)
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
                yield return baseClass == BaseClass.Mage
                    ? new ClassSkill(SkillCatalog.MasteryRobe, 1)
                    : new ClassSkill(SkillCatalog.MasteryLight, 1);
                break;
            // 2nd classes use DATA-DRIVEN per-archetype Armor Mastery skills (one skill,
            // its effect depends on the worn weight; replaces the old split masteries).
            case Archetype.Tank:
                yield return new ClassSkill(SkillCatalog.TankArmorMastery, second);
                break;
            case Archetype.Warrior:
                yield return new ClassSkill(SkillCatalog.WarriorArmorMastery, second);
                break;
            case Archetype.Rogue:
                yield return new ClassSkill(SkillCatalog.RogueArmorMastery, second);
                break;
            case Archetype.Archer:
                yield return new ClassSkill(SkillCatalog.ArcherArmorMastery, second);
                break;
            case Archetype.Nuker:
                yield return new ClassSkill(SkillCatalog.NukerArmorMastery, second);
                break;
            case Archetype.Healer:
                // Healer's data-driven Armor Mastery is registered in its own class table.
                break;
        }
    }

    /// <summary>Skills whose LearnLevel &lt;= the character's level — i.e. the
    /// ones currently offered in the "Skills to Learn" tab (before SP/learned
    /// filtering). Includes 3rd-class skills once a discipline is set.</summary>
    public static IEnumerable<ClassSkill> LearnableAt(Race race, BaseClass baseClass,
        Archetype? archetype, int level, Discipline? discipline = null)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (level >= cs.LearnLevel)
                yield return cs;
    }

    /// <summary>The character-level at which a class can learn a specific SKILL LEVEL
    /// (0 if that (skill, level) isn't on the class list).</summary>
    public static int LearnLevelOf(string skillId, int skillLevel, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (cs.SkillId == skillId && cs.SkillLevel == skillLevel)
                return cs.LearnLevel;
        return 0;
    }

    /// <summary>The highest skill-level of a skill this class can ever learn (0 = none).</summary>
    public static int MaxClassLevelOf(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null)
    {
        int max = 0;
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (cs.SkillId == skillId && cs.SkillLevel > max)
                max = cs.SkillLevel;
        return max;
    }

    /// <summary>Can this class ever learn this skill at all?</summary>
    public static bool CanClassLearn(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (cs.SkillId == skillId)
                return true;
        return false;
    }

    /// <summary>The class-specific display name for a skill (falls back to the
    /// SkillDef's canonical name). Same shared id, different label per class.</summary>
    public static string DisplayName(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.DisplayName))
                return cs.DisplayName!;
        return SkillCatalog.Get(skillId)?.Name ?? skillId;
    }

    /// <summary>The class-specific icon key for a skill (null if none set).</summary>
    public static string? Icon(string skillId, Race race, BaseClass baseClass,
        Archetype? archetype, Discipline? discipline = null)
    {
        foreach (var cs in Cumulative(race, baseClass, archetype, discipline))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.Icon))
                return cs.Icon;
        return null;
    }
}
