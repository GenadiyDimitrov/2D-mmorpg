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
    string SkillId, int LearnLevel, string? DisplayName = null, string? Icon = null);

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
    public readonly record struct ClassKey(Race Race, BaseClass Base, Archetype? Archetype);

    private static readonly Dictionary<ClassKey, List<ClassSkill>> Map = new();

    /// <summary>Called by per-class files to register a class's skill list.
    /// Safe to call multiple times for the same key (appends).</summary>
    public static void Register(Race race, BaseClass baseClass, Archetype? archetype,
        params ClassSkill[] skills)
    {
        var key = new ClassKey(race, baseClass, archetype);
        if (!Map.TryGetValue(key, out var list))
            Map[key] = list = new List<ClassSkill>();
        list.AddRange(skills);
    }

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

    /// <summary>All skills a class can EVER learn (any level), with learn-levels.</summary>
    public static IReadOnlyList<ClassSkill> ForClass(Race race, BaseClass baseClass, Archetype? archetype)
    {
        EnsureInit();
        var key = new ClassKey(race, baseClass, archetype);
        return Map.TryGetValue(key, out var list) ? list : Array.Empty<ClassSkill>();
    }

    /// <summary>Skills whose LearnLevel &lt;= the character's level — i.e. the
    /// ones currently offered in the "Skills to Learn" tab (before SP/learned
    /// filtering).</summary>
    public static IEnumerable<ClassSkill> LearnableAt(Race race, BaseClass baseClass,
        Archetype? archetype, int level)
    {
        foreach (var cs in ForClass(race, baseClass, archetype))
            if (level >= cs.LearnLevel)
                yield return cs;
    }

    /// <summary>The learn-level for a specific skill on a class (0 if not in list).</summary>
    public static int LearnLevelOf(string skillId, Race race, BaseClass baseClass, Archetype? archetype)
    {
        foreach (var cs in ForClass(race, baseClass, archetype))
            if (cs.SkillId == skillId)
                return cs.LearnLevel;
        return 0;
    }

    /// <summary>Can this class ever learn this skill at all?</summary>
    public static bool CanClassLearn(string skillId, Race race, BaseClass baseClass, Archetype? archetype)
    {
        foreach (var cs in ForClass(race, baseClass, archetype))
            if (cs.SkillId == skillId)
                return true;
        return false;
    }

    /// <summary>The class-specific display name for a skill (falls back to the
    /// SkillDef's canonical name). Same shared id, different label per class.</summary>
    public static string DisplayName(string skillId, Race race, BaseClass baseClass, Archetype? archetype)
    {
        foreach (var cs in ForClass(race, baseClass, archetype))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.DisplayName))
                return cs.DisplayName!;
        return SkillCatalog.Get(skillId)?.Name ?? skillId;
    }

    /// <summary>The class-specific icon key for a skill (null if none set).</summary>
    public static string? Icon(string skillId, Race race, BaseClass baseClass, Archetype? archetype)
    {
        foreach (var cs in ForClass(race, baseClass, archetype))
            if (cs.SkillId == skillId && !string.IsNullOrEmpty(cs.Icon))
                return cs.Icon;
        return null;
    }
}
