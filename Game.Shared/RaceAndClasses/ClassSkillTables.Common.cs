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
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(FlameBolt, 20), new ClassSkill(Heal, 25),
                new ClassSkill(GreaterWeakness, 30));
        }

        // Healer (cleric) 2nd-class kit — authored separately because Holy Bolt takes a
        // per-race NAME and God is intentionally excluded (not a playable race/class).
        RegisterHealers();

        // Per-line extras / overrides (authored in their own files).
        RegisterHumanMage();
    }

    /// <summary>2nd-class Healer kit (lvls 20/25/30/35), shared by Human/Elf/Ork. Holy
    /// Bolt is ONE skill with a per-race DISPLAY NAME (Holy/Moonlight/Spirit Bolt). God
    /// is skipped on purpose — it's a test-only dummy, not a playable race/class. Force,
    /// Focus, Frenzy, Might lvl 4 (vampirism) and the data-driven Armor Mastery arrive in
    /// later increments.</summary>
    private static void RegisterHealers()
    {
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
        {
            string holyBolt = race switch
            {
                Race.Elf => "Moonlight Bolt",
                Race.Ork => "Spirit Bolt",
                _        => "Holy Bolt",
            };

            ClassSkills.Register(race, BaseClass.Mage, Archetype.Healer,
                // Holy Bolt — same skill, per-race name. Continues the Magic Bolt curve.
                new ClassSkill(HolyStrike, 20, DisplayName: holyBolt, SkillLevel: 1),
                new ClassSkill(HolyStrike, 25, DisplayName: holyBolt, SkillLevel: 2),
                new ClassSkill(HolyStrike, 30, DisplayName: holyBolt, SkillLevel: 3),
                new ClassSkill(HolyStrike, 35, DisplayName: holyBolt, SkillLevel: 4),

                // Heal — continues the base-mage line (lvls 3-6).
                new ClassSkill(Heal, 20, SkillLevel: 3),
                new ClassSkill(Heal, 25, SkillLevel: 4),
                new ClassSkill(Heal, 30, SkillLevel: 5),
                new ClassSkill(Heal, 35, SkillLevel: 6),

                // Quick Heal — fast single-target heal.
                new ClassSkill(QuickHeal, 20, SkillLevel: 1),
                new ClassSkill(QuickHeal, 25, SkillLevel: 2),
                new ClassSkill(QuickHeal, 30, SkillLevel: 3),
                new ClassSkill(QuickHeal, 35, SkillLevel: 4),

                // Party Heal — AoE heal to nearby allies.
                new ClassSkill(PartyHeal, 20, SkillLevel: 1),
                new ClassSkill(PartyHeal, 25, SkillLevel: 2),
                new ClassSkill(PartyHeal, 30, SkillLevel: 3),
                new ClassSkill(PartyHeal, 35, SkillLevel: 4),

                // Might — continues the base-mage buff (lvls 2-3; lvl 4 w/ vampirism = Inc 2).
                new ClassSkill(Might, 20, SkillLevel: 2),
                new ClassSkill(Might, 25, SkillLevel: 3),

                // Speed — cast/move/evasion buff.
                new ClassSkill(HolySpeed, 20, SkillLevel: 1),
                new ClassSkill(HolySpeed, 25, SkillLevel: 2),
                new ClassSkill(HolySpeed, 30, SkillLevel: 3),
                new ClassSkill(HolySpeed, 35, SkillLevel: 4),

                // Body — HP-regen buff (35 only).
                new ClassSkill(HolyBody, 35, SkillLevel: 1),

                // Restore Mana — MP restore on an ally (35 only).
                new ClassSkill(RestoreMana, 35, SkillLevel: 1),

                // Anti-Magic — continues the base-mage passive (lvls 3-6).
                new ClassSkill(MageAntiMagic, 20, SkillLevel: 3),
                new ClassSkill(MageAntiMagic, 25, SkillLevel: 4),
                new ClassSkill(MageAntiMagic, 30, SkillLevel: 5),
                new ClassSkill(MageAntiMagic, 35, SkillLevel: 6),

                // Spell Mastery — caster passive (replaces Weapon Mastery).
                new ClassSkill(SpellMastery, 20, SkillLevel: 1),
                new ClassSkill(SpellMastery, 25, SkillLevel: 2),
                new ClassSkill(SpellMastery, 30, SkillLevel: 3),
                new ClassSkill(SpellMastery, 35, SkillLevel: 4));
        }
    }
}
