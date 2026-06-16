namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// Partial class split across RaceAndClasses/*.cs. Each file registers the
/// skills for one race+base-class line via its static constructor. Touch()
/// forces the static ctors to run on first use.
///
/// To add/adjust a class's skills, edit (or add) the matching partial file —
/// e.g. Classes.Human.Mage.cs for the Human mage tree. You declare, per class,
/// the skill ids and the level at which each becomes learnable.
/// </summary>
public static partial class ClassSkillTables
{
    /// <summary>No-op that guarantees this type (and its partials' static ctor)
    /// is initialized so all Register calls have run.</summary>
    public static void Touch() { }

    // Base-class kits (shared by everyone before the level-20 change).
    static ClassSkillTables()
    {
        // --- Base Fighter ---
        ClassSkills.Register(Race.Human, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.Elf, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.Ork, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.God, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 1));

        // --- Base Mage ---
        ClassSkills.Register(Race.Human, BaseClass.Mage, null,
            new ClassSkill(MagicBolt, 1), new ClassSkill(Weakness, 3), new ClassSkill(Heal, 5));
        ClassSkills.Register(Race.Elf, BaseClass.Mage, null,
            new ClassSkill(MagicBolt, 1), new ClassSkill(Weakness, 3), new ClassSkill(Heal, 5));
        ClassSkills.Register(Race.Ork, BaseClass.Mage, null,
            new ClassSkill(MagicBolt, 1), new ClassSkill(Weakness, 3), new ClassSkill(Heal, 5));
        ClassSkills.Register(Race.God, BaseClass.Mage, null,
            new ClassSkill(MagicBolt, 1), new ClassSkill(Weakness, 1), new ClassSkill(Heal, 1));

        // Second-class kits live in the per-line partial files (RegisterXxx()).
        RegisterSecondClasses();
    }

    // Implemented across the partial files; each appends its lines.
    static partial void RegisterSecondClasses();
}
