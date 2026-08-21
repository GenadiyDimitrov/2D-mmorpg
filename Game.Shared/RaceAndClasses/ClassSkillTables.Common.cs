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
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
        {
            // Fighter archetypes — 2nd-class learn cadence: 20, 24, 28, 32, 36.
            // Tank (CSV tank 2nd): Heavy Armor + Shield Mastery, Tank Anti-Magic, any-weapon
            // Weapon Mastery, Defensive Wall, Taunt (Provoke), Shield Stun, Stay!.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Tank,
                new ClassSkill(TankArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(TankShieldMastery, 20, SkillLevel: 1),
                new ClassSkill(TankAntiMagic, 20, SkillLevel: 1),
                new ClassSkill(TankWeaponMastery, 20, SkillLevel: 1),
                new ClassSkill(DefensiveWall, 20, SkillLevel: 1),
                // Provoke is a LADDER, not a one-off (BL-71): its taunt POWER is what keeps a mob
                // on the tank once the 3s lock expires, so it has to grow with the damage the party
                // behind him is doing. ⚠ It is the ONE tank line that does NOT start at 20 — the
                // owner authored it at 24/28/32/36 in `tank 2nd.csv` (2026-08-17) and deleted the
                // level-20 row himself, so a tank has no taunt for his first four levels. That gap is
                // DELIBERATE and was confirmed when queried ("delibered is taunt at 24") — it is not a
                // transcription slip, so do not helpfully restore a rung at 20.
                new ClassSkill(TankArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(TankShieldMastery, 24, SkillLevel: 2),
                new ClassSkill(TankAntiMagic, 24, SkillLevel: 2),
                new ClassSkill(TankWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(Provoke, 24, SkillLevel: 1),
                new ClassSkill(TankArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(TankShieldMastery, 28, SkillLevel: 3),
                new ClassSkill(TankAntiMagic, 28, SkillLevel: 3),
                new ClassSkill(TankWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(TankShieldStun, 28, SkillLevel: 1),
                new ClassSkill(Provoke, 28, SkillLevel: 2),
                new ClassSkill(TankArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(TankShieldMastery, 32, SkillLevel: 4),
                new ClassSkill(TankAntiMagic, 32, SkillLevel: 4),
                new ClassSkill(TankWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(Provoke, 32, SkillLevel: 3),
                new ClassSkill(TankArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(TankAntiMagic, 36, SkillLevel: 5),
                new ClassSkill(TankWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(Provoke, 36, SkillLevel: 4),
                new ClassSkill(TankStay, 36, SkillLevel: 1));
            // Warrior (CSV warrior 2nd): Two-Hand Mastery + Body Mastery (5 levels each),
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
            // Rogue (CSV rogue 2nd): Rogue Armor/Weapon Mastery, Stab + Shot continue (levels
            // 4-8), Sprint, Bow Expertise.
            ClassSkills.Register(race, BaseClass.Fighter, Archetype.Rogue,
                new ClassSkill(RogueArmorMastery, 20, SkillLevel: 1),
                new ClassSkill(RogueWeaponMastery, 20, SkillLevel: 1),
                new ClassSkill(PiercingStab, 20, SkillLevel: 1),
                new ClassSkill(PreciseShot, 20, SkillLevel: 1),
                new ClassSkill(Sprint, 20, SkillLevel: 1),
                // 🔴 Lure (BL-70) MOVED 20/28/36 -> 40, melee/dual disciplines only, by his ruling of
                // 2026-08-19: *"move lure from rogue to dual 3rd (@40) … No lure for lvl 29 and below ..
                // It's a skill that need the prawl effect."* The pull only makes sense with the stance
                // that lets you walk away from the camp afterwards, and Prowl is a 40+ melee-rogue skill
                // — so a lure at 20 was a tool whose other half arrived twenty levels later. It is
                // registered in ClassSkillTables.Third.RegisterHideKit() next to Prowl, at LEVEL 1 only;
                // the 200/400/600 reach ladder is HIS to place as he authors `dual 3rd.csv`.
                // 🔴 Prowl MOVED 20 -> 40 (melee rogue only) by his playtest-23 ruling: *"`Prowl` should
                // be learnable by all mele rogues @40 3rd class (not auto, like a normal skill)"*. It is
                // registered in ClassSkillTables.Third.RegisterHideKit(), not here — the 2nd-class rogue
                // block covers BOTH weapons to 40 (the archer merge), so leaving it at 20 handed the
                // dagger's stance to every future archer as well. The earlier note here argued the
                // opposite ("a farming tool that arrives at 40 is a farming tool for someone else") and
                // he has overruled it; the farming stance is now part of what the melee split BUYS.
                new ClassSkill(RogueArmorMastery, 24, SkillLevel: 2),
                new ClassSkill(RogueWeaponMastery, 24, SkillLevel: 2),
                new ClassSkill(PiercingStab, 24, SkillLevel: 2),
                new ClassSkill(PreciseShot, 24, SkillLevel: 2),
                new ClassSkill(RogueArmorMastery, 28, SkillLevel: 3),
                new ClassSkill(RogueWeaponMastery, 28, SkillLevel: 3),
                new ClassSkill(PiercingStab, 28, SkillLevel: 3),
                new ClassSkill(PreciseShot, 28, SkillLevel: 3),
                // Evasion Boost — the rogue's ultimate (CSV rogue 2nd, added playtest-20).
                new ClassSkill(EvasionBoost, 28, SkillLevel: 1),
                // 🔴 Signal Flare MOVED 28 -> 60 (archer disciplines only) by his playtest-23 ruling:
                // *"`Signal Flare` should be learnable by all archers @60 3rd class"*. See
                // ClassSkillTables.Third.RegisterHideKit(). 🔑 The counter now sits ABOVE what it counters
                // (Vanish @60 on the melee rogue) rather than twelve levels below it, which is the shape
                // he asked for: you meet the hide before you can answer it.
                new ClassSkill(RogueArmorMastery, 32, SkillLevel: 4),
                new ClassSkill(RogueWeaponMastery, 32, SkillLevel: 4),
                new ClassSkill(PiercingStab, 32, SkillLevel: 4),
                new ClassSkill(PreciseShot, 32, SkillLevel: 4),
                new ClassSkill(RogueArmorMastery, 36, SkillLevel: 5),
                new ClassSkill(RogueWeaponMastery, 36, SkillLevel: 5),
                new ClassSkill(PiercingStab, 36, SkillLevel: 5),
                new ClassSkill(PreciseShot, 36, SkillLevel: 5),
                // Bow Expertise moved 28 -> 36: he corrected the CSV in playtest-20 ("The Bow
                // expertice was with the 36 lvl skills but it was lvl 28 so i fixed it as well").
                new ClassSkill(BowExpertise, 36, SkillLevel: 1),
                // Sprint level 2 (+60) — G5 gave the LEVEL and its value but not where it is learned,
                // and the authored rogue CSV stops at 36. ⚠ 40 is MY pick: it is the next rung on this
                // block's own 4-level cadence and the level the 3rd-class disciplines already sit at.
                // One line to move when his level-40 CSV lands; without it level 2 is unreachable.
                //
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
                //
                // 🔴 Heavy Draw (`PowerShot`) @24 was the LAST survivor of that pair and is GONE as of
                // 2026-08-07 (playtest-19 M7): *"I continue to get Heavy Draw on a rogue 24lvl -
                // remove it - remove it from after 40lvl as well"*. Same reasoning as Battle Fury —
                // never in the authored rogue CSV, inherited wholesale from the dead Archer table.
                // ⚠ The SkillDef STAYS in the catalog (Skills.Fighter.cs); see the note there.
                new ClassSkill(Sprint, 40, SkillLevel: 2));

            // Nuker (CSV nuker 2nd): Elemental Bolt (replaces Magic Bolt), Quick Bolt,
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

            // The nuke ladder does NOT stop at the 2nd class. In IG your main nuke keeps
            // gaining levels for life, and that ladder IS the mage's damage scaling — capping
            // it at 35 (power 44) is what left a level-85 mage fighting with a level-35 spell.
            // Levels 5-13 of each bolt, learned every 5 levels from 40 to 80 (power 63 -> 116;
            // 108 @ 74 is IG's anchor). See Skills.Mage.cs for the ladder itself.
            // Vampiric Bolt is one level ahead of the other two (it starts at 14, not 20).
            //
            // Restore Spirit rides the SAME cadence above 35, and for the same reason (2026-08-07):
            // one level for life meant the mage's only mana tool was worth 20 MP forever while his
            // bolt grew from 30 to 116, so it slowed the drain instead of sustaining a rotation.
            // ⚠ Its level 1 @25 is the AUTHORED CSV and stays exactly where it is, above; levels
            // 2-10 arrive 40 → 80, reaching the owner's 120-MP / 200-HP endpoint at 80.
            //
            // Mage Armor Mastery gets rungs 5-8 here too (@40/50/60/70), carrying his "mpRestore to
            // a +80" — a LATE-level number, so it cannot live inside the 20-35 CSV rungs. Only the
            // restore bonus grows on those rungs; see Skills.Masteries.cs.
            for (int i = 0; i < 9; i++)
            {
                int learnLevel = 40 + i * 5;
                ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                    new ClassSkill(ElementalBolt, learnLevel, SkillLevel: 5 + i),
                    new ClassSkill(QuickBolt,     learnLevel, SkillLevel: 5 + i),
                    new ClassSkill(VampiricBolt,  learnLevel, SkillLevel: 6 + i),
                    new ClassSkill(RestoreSpirit, learnLevel, SkillLevel: 2 + i));
            }
            ClassSkills.Register(race, BaseClass.Mage, Archetype.Nuker,
                new ClassSkill(NukerArmorMastery, 40, SkillLevel: 5),
                new ClassSkill(NukerArmorMastery, 50, SkillLevel: 6),
                new ClassSkill(NukerArmorMastery, 60, SkillLevel: 7),
                new ClassSkill(NukerArmorMastery, 70, SkillLevel: 8));
        }

        // Healer (cleric) 2nd-class kit — authored separately because Holy Bolt takes a per-race NAME.
        RegisterHealers();

        // Per-line extras / overrides (authored in their own files).
        RegisterHumanMage();
    }

    /// <summary>2nd-class Healer kit (lvls 20/25/30/35), shared by Human/Elf/Ork. Holy
    /// Bolt is ONE skill with a per-race DISPLAY NAME (Holy/Moonlight/Spirit Bolt). Force,
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

                // ⚠ SHROUDING HYMN IS NOT LEARNED HERE ANY MORE (owner, 2026-08-19). He moved its row
                // out of `cleric 2nd.csv` and into `buffer 3rd.csv`, which settles the one row in the
                // cleric file whose values were never his: it is the BUFFER's party stealth, not every
                // cleric's. The grant moved with it (ClassSkillTables.Third, Warchanter) and kept his
                // level 30 — a Warchanter exists only from 40, so in practice it arrives with the class.

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
                //
                // ⚠ THE LEVELS BELOW ARE HIS, re-cut on 2026-08-19 (*"the buffs were all over the place
                // i fixed them"*). Four families moved and two rows left the file entirely — do not
                // restore any of it from the older ladder:
                //   · Swift L3 moved 25 → 30, Alacrity L2 moved 25 → 35 (each family now gains one
                //     rung per learn level instead of two at 20-25 and nothing after).
                //   · Haste is GONE from the cleric — attack speed is a buffer reward from 40.
                //   · Clarity is NEW at 25, at his 20% (the healer's 30% became rung 2).
                // 20 — one rung each of offence, movement, casting and interrupt.
                new ClassSkill(CastId(FamPhysAtk), 20, SkillLevel: 2),      // Might   +12% P.Atk
                new ClassSkill(CastId(FamMove), 20, SkillLevel: 2),         // Swift   +20 move
                new ClassSkill(CastId(FamCast), 20, SkillLevel: 1),         // Alacrity +15% cast
                new ClassSkill(CastId(FamInterrupt), 20, SkillLevel: 1),    // Resolve +18 interrupt
                // 25 — the defensive/magic half, plus the crit and debuff-resist blessings.
                new ClassSkill(CastId(FamPhysDef), 25, SkillLevel: 2),      // Bulwark +12% P.Def
                new ClassSkill(CastId(FamCcResMag), 25, SkillLevel: 1),     // Clarity 20% vs SPT debuffs
                new ClassSkill(CastId(FamMagAtk), 25, SkillLevel: 2),       // Force   +25% M.Atk
                new ClassSkill(CastId(FamInterrupt), 25, SkillLevel: 2),    // Resolve +25 interrupt
                new ClassSkill(CastId(FamCritRate), 25, SkillLevel: 4),     // Focus   +20% crit rate
                // 30
                new ClassSkill(CastId(FamVamp), 30, SkillLevel: 2),         // Vampirism 6%
                new ClassSkill(CastId(FamEva), 30, SkillLevel: 2),          // Agility +2 evasion
                new ClassSkill(CastId(FamMove), 30, SkillLevel: 3),         // Swift   +33 move
                new ClassSkill(CastId(FamAccuracy), 30, SkillLevel: 1),     // Aim     +1 accuracy
                // 35 — the cleric's last rungs. Alacrity STOPS at L2 (+23%): cast speed past that is a
                // 3rd-class reward — collected 2026-08-21 as the Lightbringer's rung 3 at 48 (+30%),
                // in ClassSkillTables.Third.cs. No Haste row at all any more.
                new ClassSkill(CastId(FamHpRegen), 35, SkillLevel: 2),      // Vigor   +10% HP regen
                new ClassSkill(CastId(FamMagDef), 35, SkillLevel: 1),       // Ward    +10% M.Def
                new ClassSkill(CastId(FamCast), 35, SkillLevel: 2),         // Alacrity +23% cast
                new ClassSkill(HolyFrenzy, 35, SkillLevel: 1),              // Frenzy (already a single)

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

                // ⚠ COMBAT STANCE IS NOT LEARNED ANY MORE (owner 2026-08-17: *"the only one created
                // after the csv was the combat stence which we will remove and add it to the buffer
                // later in different form"*). It was the one cleric skill this project invented rather
                // than took from his CSV, which is exactly why it is the one being taken back out. The
                // SkillDef stays in the catalog — the toggle returns on the BUFFER in a different
                // shape — and nothing is orphaned by dropping the row: the fighter's stealth toggle
                // (Skills.Fighter.cs) is a second user of the toggle mechanic.

                // Antidote — targeted cure. It always cures poison/venom/bleed; the level is a RANK
                // CEILING on how strong a DoT it can strip. His levels: 20 and 35, with more coming
                // on the 3rd-class healers (*"later healers will learn more levels"*).
                new ClassSkill(Antidote, 20, SkillLevel: 1),
                new ClassSkill(Antidote, 35, SkillLevel: 2),

                // Restore Mana — a lossless MP transfer to an ally. His levels and his powers: 52 at
                // 30, 60 at 35, each costing exactly what it gives.
                new ClassSkill(RestoreMana, 30, SkillLevel: 1),
                new ClassSkill(RestoreMana, 35, SkillLevel: 2),

                // Resurrection — revive a fallen ally. Learned for SP like any other skill (owner,
                // 2026-07-17: it used to be auto-granted). His levels: 20 (revive only, no exp back)
                // and 30 (20% of the lost exp). EVERY cleric gets both, whatever 3rd class follows.
                // ⚠ There are no 40+ rungs any more — he is authoring those himself ("ill add 40+").
                new ClassSkill(Resurrection, 20, SkillLevel: 1),
                new ClassSkill(Resurrection, 30, SkillLevel: 2));
        }
    }
}
