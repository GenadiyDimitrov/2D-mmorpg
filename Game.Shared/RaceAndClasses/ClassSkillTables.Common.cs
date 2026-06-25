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
            // Fighter archetypes — 2nd-class learn cadence: 20, 24, 28, 32, 36.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Tank,
                new ClassSkill(Fortify, 20),
                new ClassSkill(ShieldMastery, 24), new ClassSkill(Disrupt, 28));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Warrior,
                new ClassSkill(GreaterWarCry, 20), new ClassSkill(MightyBlow, 24));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue,
                new ClassSkill(BattleFury, 20), new ClassSkill(TwinSlash, 24));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Archer,
                new ClassSkill(BattleFury, 20), new ClassSkill(PowerShot, 24));

            // Mage archetypes — 2nd-class learn cadence: 20, 25, 30, 35.
            // NOTE: the Healer (cleric) line is intentionally EMPTY pending the new
            // cleric@20 + healer/buffer@40 CSVs. The skill DEFS still exist (Skills.Mage.cs
            // / Lightbringer / Warchanter) — only the learn assignments were dropped.
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(FlameBolt, 20), new ClassSkill(Heal, 25),
                new ClassSkill(GreaterWeakness, 30));
        }

        // Per-line extras / overrides (authored in their own files).
        RegisterHumanMage();
    }
}
