namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// Default second-class kits, shared by all races for each archetype. Per-line
/// files (e.g. Classes.Human.Mage.cs) can register ADDITIONAL skills on top —
/// the registry appends, so a race/class can diverge without duplicating the
/// whole list. This is where the "same archetype, same core kit" rule lives.
/// </summary>
public static partial class ClassSkillTables
{
    // This partial method is declared in ClassSkillTables.cs and called from its
    // static ctor; here we provide the body.
    static partial void RegisterSecondClasses()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork, Race.God })
        {
            // Fighter archetypes
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Tank,
                new ClassSkill(PowerStrike, 20), new ClassSkill(Fortify, 20),
                new ClassSkill(ShieldMastery, 20), new ClassSkill(Disrupt, 20));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Warrior,
                new ClassSkill(GreaterWarCry, 20), new ClassSkill(MightyBlow, 20));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue,
                new ClassSkill(BattleFury, 20), new ClassSkill(TwinSlash, 20));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Archer,
                new ClassSkill(BattleFury, 20), new ClassSkill(PowerShot, 20));

            // Mage archetypes — default kit; Human Mage adds more below.
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Healer,
                new ClassSkill(GreaterHeal, 20), new ClassSkill(HolyStrike, 20),
                new ClassSkill(Weakness, 20));
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(FlameBolt, 20), new ClassSkill(Heal, 20),
                new ClassSkill(GreaterWeakness, 20));
        }

        // Per-line extras / overrides (authored in their own files).
        RegisterHumanMage();
    }
}
