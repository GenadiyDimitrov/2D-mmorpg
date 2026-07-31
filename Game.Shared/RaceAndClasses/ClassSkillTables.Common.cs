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
                new ClassSkill(Smash, 20, SkillLevel: 1),
                new ClassSkill(WarriorArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(WarriorWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(BodyMastery, 24, SkillLevel: 2),
                new ClassSkill(Smash, 24, SkillLevel: 2),
                new ClassSkill(WarriorArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(WarriorWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(BodyMastery, 28, SkillLevel: 3),
                new ClassSkill(Smash, 28, SkillLevel: 3),
                new ClassSkill(BattleRegeneration, 28, SkillLevel: 1),
                new ClassSkill(WarriorArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(WarriorWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(BodyMastery, 32, SkillLevel: 4),
                new ClassSkill(Smash, 32, SkillLevel: 4),
                new ClassSkill(BattlePresence, 32, SkillLevel: 1),
                new ClassSkill(WarriorArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(WarriorWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(BodyMastery, 36, SkillLevel: 5),
                new ClassSkill(Smash, 36, SkillLevel: 5),
                new ClassSkill(BattleDefence, 36, SkillLevel: 1));
            // Rogue (CSV rogue 20-35): Rogue Armor/Weapon Mastery, Stab + Shot continue (levels
            // 4-8), Sprint, Bow Expertise.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue,
                new ClassSkill(RogueArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(RogueWeaponMastery, 20, SkillLevel: 1),
                new ClassSkill(PiercingStab, 20, SkillLevel: 1),
                new ClassSkill(PreciseShot, 20, SkillLevel: 1),
                new ClassSkill(Sprint, 20, SkillLevel: 1),
                new ClassSkill(RogueArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(RogueWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(PiercingStab, 24, SkillLevel: 2),
                new ClassSkill(PreciseShot, 24, SkillLevel: 2),
                new ClassSkill(RogueArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(RogueWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(PiercingStab, 28, SkillLevel: 3),
                new ClassSkill(PreciseShot, 28, SkillLevel: 3),
                new ClassSkill(BowExpertise, 28, SkillLevel: 1),
                new ClassSkill(RogueArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(RogueWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(PiercingStab, 32, SkillLevel: 4),
                new ClassSkill(PreciseShot, 32, SkillLevel: 4),
                new ClassSkill(RogueArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(RogueWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(PiercingStab, 36, SkillLevel: 5),
                new ClassSkill(PreciseShot, 36, SkillLevel: 5),
                // The ARCHER MERGE (2026-07-29) folded the old Archer 2nd class in here. Its whole
                // table was these two lines — which is why archers were hollow — while the Rogue block
                // above already taught BOTH the Stab (dagger) and Shot (bow) ladders. So the merge is
                // this: the rogue keeps both weapons to 40, and the bow/dagger split becomes the 3rd
                // class. Nothing else needed authoring.
                //
                // Battle Fury @20 was the other half of that inherited pair and is GONE as of
                // 2026-07-31 (playtest-15) — it was never in the authored class CSV, it arrived only
                // because the dead Archer table was folded in wholesale. The SkillDef stays in the
                // catalog: five 3rd-class disciplines still grant it under their own names
                // (Shadowstep, Creeping Toxin, Steady Aim, Nullstep, Blood Draw), so deleting the
                // definition would hollow out those disciplines instead.
                new ClassSkill(PowerShot, 24));

            // Nuker (CSV nuker 20-35): Elemental Bolt (replaces Magic Bolt), Quick Bolt,
            // Vampiric Bolt (continues, lvls 2-5), Restore Spirit, Mage Armor Mastery,
            // Anti-Magic (lvls 3-6) and Spell Mastery. Cadence 20/25/30/35.
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(NukerArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(ElementalBolt, 20, SkillLevel: 1),
                new ClassSkill(QuickBolt, 20, SkillLevel: 1),
                new ClassSkill(VampiricBolt, 20, SkillLevel: 2),
                new ClassSkill(MageAntiMagic, 20, SkillLevel: 3),
                new ClassSkill(SpellMastery, 20, SkillLevel: 1),
                new ClassSkill(NukerArmorMastery, 25, SkillLevel: 2),
                new ClassSkill(ElementalBolt, 25, SkillLevel: 2),
                new ClassSkill(QuickBolt, 25, SkillLevel: 2),
                new ClassSkill(VampiricBolt, 25, SkillLevel: 3),
                new ClassSkill(RestoreSpirit, 25, SkillLevel: 1),
                new ClassSkill(MageAntiMagic, 25, SkillLevel: 4),
                new ClassSkill(SpellMastery, 25, SkillLevel: 2),
                new ClassSkill(NukerArmorMastery, 30, SkillLevel: 3),
                new ClassSkill(ElementalBolt, 30, SkillLevel: 3),
                new ClassSkill(QuickBolt, 30, SkillLevel: 3),
                new ClassSkill(VampiricBolt, 30, SkillLevel: 4),
                new ClassSkill(MageAntiMagic, 30, SkillLevel: 5),
                new ClassSkill(SpellMastery, 30, SkillLevel: 3),
                new ClassSkill(NukerArmorMastery, 35, SkillLevel: 4),
                new ClassSkill(ElementalBolt, 35, SkillLevel: 4),
                new ClassSkill(QuickBolt, 35, SkillLevel: 4),
                new ClassSkill(VampiricBolt, 35, SkillLevel: 5),
                new ClassSkill(MageAntiMagic, 35, SkillLevel: 6),
                new ClassSkill(SpellMastery, 35, SkillLevel: 4));

            // The nuke ladder does NOT stop at the 2nd class. In L2 your main nuke keeps
            // gaining levels for life, and that ladder IS the mage's damage scaling — capping
            // it at 35 (power 44) is what left a level-85 mage fighting with a level-35 spell.
            // Levels 5-13 of each bolt, learned every 5 levels from 40 to 80 (power 63 -> 116;
            // 108 @ 74 is L2's anchor). See Skills.Mage.cs for the ladder itself.
            // Vampiric Bolt is one level ahead of the other two (it starts at 14, not 20).
            for (int i = 0; i < 9; i++)
            {
                int learnLevel = 40 + i * 5;
                ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                    new ClassSkill(ElementalBolt, learnLevel, SkillLevel: 5 + i),
                    new ClassSkill(QuickBolt,     learnLevel, SkillLevel: 5 + i),
                    new ClassSkill(VampiricBolt,  learnLevel, SkillLevel: 6 + i));
            }
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

                // Heal — the healer's targeted heal; REPLACES the base-mage Self Heal at 20.
                new ClassSkill(Heal, 20, SkillLevel: 1),   // power 151
                new ClassSkill(Heal, 25, SkillLevel: 2),   // power 195
                new ClassSkill(Heal, 30, SkillLevel: 3),   // power 245
                new ClassSkill(Heal, 35, SkillLevel: 4),   // power 301

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

                // ---- The INDIVIDUAL buffs (owner 2026-07-31). The cleric used to learn the four
                // GROUP buffs — Might, Force, Focus, Speed, Body; those are now the Warchanter's,
                // at 150+ MP. A cleric buffs one effect per cast, at 30-50 MP, which is what makes
                // "five casts vs one" the buffer class's actual advantage.
                //
                // Every rung below is the one the corresponding group level used to hand out, so a
                // cleric who buffs their whole list ends up exactly where they were — it just costs
                // more casts. See docs/design/BuffLadders.md.
                // 20 — what Might L2 / Force L1 / Speed L1 gave.
                new ClassSkill(CastId(FamPhysAtk), 20, SkillLevel: 2),      // Might   +12% P.Atk
                new ClassSkill(CastId(FamMove), 20, SkillLevel: 2),         // Swift   +20 move
                new ClassSkill(CastId(FamCast), 20, SkillLevel: 1),         // Alacrity +15% cast
                new ClassSkill(CastId(FamInterrupt), 20, SkillLevel: 1),    // Resolve +18 interrupt
                // 25 — Might L3, Force L2 (the +25% M.Atk rung), Focus L1, Speed L2.
                new ClassSkill(CastId(FamPhysDef), 25, SkillLevel: 2),      // Bulwark +12% P.Def
                new ClassSkill(CastId(FamMagAtk), 25, SkillLevel: 2),       // Force   +25% M.Atk
                new ClassSkill(CastId(FamInterrupt), 25, SkillLevel: 2),    // Resolve +25 interrupt
                new ClassSkill(CastId(FamCritRate), 25, SkillLevel: 4),     // Focus   +20% crit rate
                new ClassSkill(CastId(FamMove), 25, SkillLevel: 3),         // Swift   +33 move
                new ClassSkill(CastId(FamCast), 25, SkillLevel: 2),         // Alacrity +23% cast
                // 30 — Might L4 (adds vampirism), Speed L3 (adds evasion).
                new ClassSkill(CastId(FamVamp), 30, SkillLevel: 2),         // Vampirism 6%
                new ClassSkill(CastId(FamEva), 30, SkillLevel: 2),          // Agility +2 evasion
                new ClassSkill(CastId(FamAccuracy), 30, SkillLevel: 2),     // Aim     +2 accuracy
                // 35 — Speed L4 (adds attack speed), Body L1, Frenzy.
                new ClassSkill(CastId(FamCast), 35, SkillLevel: 3),         // Alacrity +30% cast
                new ClassSkill(CastId(FamAs), 35, SkillLevel: 1),           // Haste   +15% attack speed
                new ClassSkill(CastId(FamHpRegen), 35, SkillLevel: 2),      // Vigor   +10% HP regen
                new ClassSkill(CastId(FamMagDef), 35, SkillLevel: 1),       // Ward    +10% M.Def
                new ClassSkill(HolyFrenzy, 35, SkillLevel: 1),              // Frenzy (already a single)

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
                new ClassSkill(Antidote, 25, SkillLevel: 1),

                // Resurrection — revive a fallen ally. Learned for SP like any other skill (owner,
                // 2026-07-17: it used to be auto-granted). EVERY cleric gets L1/L2 regardless of the
                // 3rd class they go on to take; L3/L4 are the Lightbringer's alone (see the third table).
                new ClassSkill(Resurrection, 20, SkillLevel: 1),
                new ClassSkill(Resurrection, 40, SkillLevel: 2));
        }
    }
}
