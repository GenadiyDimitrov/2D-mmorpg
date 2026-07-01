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
            // Tank (CSV tank 20-35): Heavy Armor + Shield Mastery, Tank Anti-Magic, any-weapon
            // Weapon Mastery, Defensive Wall, Taunt (Provoke), Shield Stun, Stay!.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Tank,
                new ClassSkill(TankArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(TankShieldMastery, 20, SkillLevel: 1),
                new ClassSkill(TankAntiMagic, 20, SkillLevel: 1),
                new ClassSkill(TankWeaponMastery, 20, SkillLevel: 1),
                new ClassSkill(DefensiveWall, 20, SkillLevel: 1),
                new ClassSkill(Provoke, 20),
                new ClassSkill(TankArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(TankShieldMastery, 24, SkillLevel: 2),
                new ClassSkill(TankAntiMagic, 24, SkillLevel: 2),
                new ClassSkill(TankWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(TankArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(TankShieldMastery, 28, SkillLevel: 3),
                new ClassSkill(TankAntiMagic, 28, SkillLevel: 3),
                new ClassSkill(TankWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(TankShieldStun, 28, SkillLevel: 1),
                new ClassSkill(TankArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(TankShieldMastery, 32, SkillLevel: 4),
                new ClassSkill(TankAntiMagic, 32, SkillLevel: 4),
                new ClassSkill(TankWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(TankArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(TankAntiMagic, 36, SkillLevel: 5),
                new ClassSkill(TankWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(TankStay, 36, SkillLevel: 1));
            // Warrior (CSV warrior 20-35): Two-Hand Mastery + Body Mastery (5 levels each),
            // Strike continues (levels 4-8), and the low-HP Battle stances.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Warrior,
                new ClassSkill(WarriorArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(WarriorWeaponMastery, 20, SkillLevel: 1),
                new ClassSkill(BodyMastery, 20, SkillLevel: 1),
                new ClassSkill(Strike, 20, SkillLevel: 4),
                new ClassSkill(WarriorArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(WarriorWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(BodyMastery, 24, SkillLevel: 2),
                new ClassSkill(Strike, 24, SkillLevel: 5),
                new ClassSkill(WarriorArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(WarriorWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(BodyMastery, 28, SkillLevel: 3),
                new ClassSkill(Strike, 28, SkillLevel: 6),
                new ClassSkill(BattleRegeneration, 28, SkillLevel: 1),
                new ClassSkill(WarriorArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(WarriorWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(BodyMastery, 32, SkillLevel: 4),
                new ClassSkill(Strike, 32, SkillLevel: 7),
                new ClassSkill(BattlePresence, 32, SkillLevel: 1),
                new ClassSkill(WarriorArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(WarriorWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(BodyMastery, 36, SkillLevel: 5),
                new ClassSkill(Strike, 36, SkillLevel: 8),
                new ClassSkill(BattleDefence, 36, SkillLevel: 1));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue,
                new ClassSkill(BattleFury, 20), new ClassSkill(TwinSlash, 24));
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Archer,
                new ClassSkill(BattleFury, 20), new ClassSkill(PowerShot, 24));

            // Mage archetypes — 2nd-class learn cadence: 20, 25, 30, 35.
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(FlameBolt, 20), new ClassSkill(Heal, 25),
                new ClassSkill(GreaterWeakness, 30), new ClassSkill(DispelMagic, 35),
                // Anti-Magic on ALL mage classes (continues the base-mage line, lvls 3-6).
                new ClassSkill(MageAntiMagic, 20, SkillLevel: 3),
                new ClassSkill(MageAntiMagic, 25, SkillLevel: 4),
                new ClassSkill(MageAntiMagic, 30, SkillLevel: 5),
                new ClassSkill(MageAntiMagic, 35, SkillLevel: 6),
                // Spell Mastery — same caster passive as the healer (replaces Weapon Mastery;
                // carries the bow cast-speed penalty). Mages have no weapon-TYPE mastery.
                new ClassSkill(SpellMastery, 20, SkillLevel: 1),
                new ClassSkill(SpellMastery, 25, SkillLevel: 2),
                new ClassSkill(SpellMastery, 30, SkillLevel: 3),
                new ClassSkill(SpellMastery, 35, SkillLevel: 4));
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

                // Might — continues the base-mage buff (lvls 2-4; lvl 4 adds melee vampirism).
                new ClassSkill(Might, 20, SkillLevel: 2),
                new ClassSkill(Might, 25, SkillLevel: 3),
                new ClassSkill(Might, 30, SkillLevel: 4),

                // Force — interrupt resist (+M.Atk @rank 2).
                new ClassSkill(HolyForce, 20, SkillLevel: 1),
                new ClassSkill(HolyForce, 25, SkillLevel: 2),

                // Focus — physical crit-rate buff (25). Frenzy — berserk buff (35).
                new ClassSkill(HolyFocus, 25, SkillLevel: 1),
                new ClassSkill(HolyFrenzy, 35, SkillLevel: 1),

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
                new ClassSkill(SpellMastery, 35, SkillLevel: 4),

                // Armor Mastery — data-driven passive (replaces Robe/Light mastery).
                new ClassSkill(ArmorMasterySkill, 20, SkillLevel: 1),
                new ClassSkill(ArmorMasterySkill, 25, SkillLevel: 2),
                new ClassSkill(ArmorMasterySkill, 30, SkillLevel: 3),
                new ClassSkill(ArmorMasterySkill, 35, SkillLevel: 4),

                // Combat Stance — TOGGLE: trade M.Atk for P.Atk to melee-farm (mace).
                new ClassSkill(CombatStance, 20, SkillLevel: 1),

                // Antidote — targeted cure (poison/venom).
                new ClassSkill(Antidote, 25, SkillLevel: 1));
        }
    }
}
