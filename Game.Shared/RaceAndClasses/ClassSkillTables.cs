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
        // --- Base Fighter --- (1st-class fighter learn cadence: 1, 5, 10, 15)
        ClassSkills.Register(Race.Human, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.Elf, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.Ork, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 5));
        ClassSkills.Register(Race.God, BaseClass.Fighter, null,
            new ClassSkill(PowerStrike, 1), new ClassSkill(WarCry, 1));

        // --- Base Mage --- (1st-class path, levels 1/7/14). Magic Bolt Lv.1 and Robe
        // Mastery Lv.1 are auto-granted; everything below is learned with SP.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.Register(race, BaseClass.Mage, null,
                new ClassSkill(SelfHeal, 1),                         // Lv1 self heal
                new ClassSkill(MagicBolt, 7, SkillLevel: 2),
                new ClassSkill(MasteryRobe, 7, SkillLevel: 2),       // +7 P.Def
                new ClassSkill(Heal, 7, SkillLevel: 1),              // targeted heal (replaces Self Heal)
                new ClassSkill(Might, 7),                            // +8% atk/def buff
                new ClassSkill(MageAntiMagic, 7, SkillLevel: 1),     // +12 M.Def
                new ClassSkill(MagicBolt, 14, SkillLevel: 3),
                new ClassSkill(VampiricBolt, 14),
                new ClassSkill(MageAntiMagic, 14, SkillLevel: 2),    // +16 M.Def + 5% fizzle
                new ClassSkill(Heal, 14, SkillLevel: 2),
                new ClassSkill(MasteryRobe, 14, SkillLevel: 3),      // +10 P.Def
                new ClassSkill(WeaponMastery, 14));                  // +4 M.Atk / +2 P.Atk
        ClassSkills.Register(Race.God, BaseClass.Mage, null,
            new ClassSkill(MagicBolt, 1), new ClassSkill(Weakness, 1), new ClassSkill(Heal, 1));

        // Second-class kits live in the per-line partial files (RegisterXxx()).
        RegisterSecondClasses();
        // 3rd-class (discipline) kits — placeholder lists in 24.0; fleshed out per
        // archetype in the content slices.
        RegisterThirdClasses();
    }

    // Implemented across the partial files; each appends its lines.
    static partial void RegisterSecondClasses();
    static partial void RegisterThirdClasses();
}
