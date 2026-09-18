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
        // --- Base Fighter --- (CSV fighter 1st, learn cadence 5/10/15). Strike (sword/blunt),
        // Stab (dual BLOW) and Shot (bow) are weapon-gated core actives that keep leveling into
        // the 2nd-class warrior/rogue tables. Everything is SP-learned (no auto-grant).
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            ClassSkills.Register(race, BaseClass.Fighter, null,
                new ClassSkill(FighterArmorMastery, 5,  SkillLevel: 1),
                new ClassSkill(FighterWeaponMastery, 5, SkillLevel: 1),
                // Spirit Mastery — ONE rung, and the only place any fighter ever buys the ×1.1 MP
                // regen (2026-09-11, his `fighter 1st.csv`). No 2nd class replaces it, so a warrior,
                // rogue or tank keeps it for the rest of the game. See SkillCatalog.FighterSpiritMastery.
                new ClassSkill(FighterSpiritMastery, 5, SkillLevel: 1),
                new ClassSkill(Strike, 5, SkillLevel: 1),
                new ClassSkill(Stab,   5, SkillLevel: 1),
                new ClassSkill(Shot,   5, SkillLevel: 1),
                new ClassSkill(FighterArmorMastery, 10,  SkillLevel: 2),
                new ClassSkill(FighterWeaponMastery, 10, SkillLevel: 2),
                new ClassSkill(Strike, 10, SkillLevel: 2),
                new ClassSkill(Stab,   10, SkillLevel: 2),
                new ClassSkill(Shot,   10, SkillLevel: 2),
                new ClassSkill(FighterArmorMastery, 15,  SkillLevel: 3),
                new ClassSkill(FighterWeaponMastery, 15, SkillLevel: 3),
                new ClassSkill(Strike, 15, SkillLevel: 3),
                new ClassSkill(Stab,   15, SkillLevel: 3),
                new ClassSkill(Shot,   15, SkillLevel: 3));
        // (The God base-fighter line was deleted 2026-08-07 with the rest of the God layer.)

        // --- Base Mage --- (1st-class path, levels 1/7/14). Magic Bolt Lv.1 and Spellcaster
        // Mastery are auto-granted; everything below is learned with SP.
        // ⚠ 2026-08-07: `MasteryRobe` is ROBE ARMOR MASTERY now — a 2-level, bonus-only skill first
        // learned at 7. It is no longer auto-granted at 1 and no longer carries any penalty; the
        // wrong-weight/wrong-weapon rule moved to Spellcaster Mastery (auto-granted, never replaced).
        // 🔴 SELF HEAL IS NOT HERE ANY MORE (2026-09-17, `BL-258`). His race pass deleted the three
        // base-mage rows (1/7/14, power 42/67/107) and re-authored the skill as the ELF's nine-rung
        // ladder under `elf_self_heal` — see ClassSkills.MageRaceSkills. A Human or Demon mage has no
        // self-heal at all now; that is the point of the race split, not an omission.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            ClassSkills.Register(race, BaseClass.Mage, null,
                new ClassSkill(MagicBolt, 7, SkillLevel: 2),
                new ClassSkill(MasteryRobe, 7, SkillLevel: 1),       // Robe Armor Mastery +7 P.Def
                // The base mage's first buffs. These used to be ONE skill (the group "Might") — it is
                // the Warchanter's now, so the base mage learns the two singles instead (owner
                // 2026-07-31). ⚠ They are NOT learned together any more: he split the `mage 1st.csv`
                // row on 2026-08-19 and put Bulwark at 14 (*"i splitted them"*), so a level-7 mage buys
                // offence and waits a tier for defence. 20 MP and 960 SP each — his numbers.
                //
                // 🔑 `BL-263` — MIGHT IS THREE SKILLS NOW, one per race, and this loop picks the
                //    race's. They are WRAPPERS over one rung (SkillCatalog.MageMightFor): same
                //    +8% P.Atk, same price, different name/description/icon. The generic
                //    `cast_atk_phys` is still what a buffer CLASS casts from 20 up, and it
                //    `Replaces` all three — see ClassSkillTables.Common.
                // ⚠ Stays in the BASE-CLASS list rather than the race injector: it is one rung at 7
                //   that a 2nd class supersedes, not a ladder that follows you. Same lifecycle it
                //   has always had.
                new ClassSkill(MageMightFor(race), 7),               // Might   +8% P.Atk
                new ClassSkill(MageAntiMagic, 7, SkillLevel: 1),     // +12 M.Def
                new ClassSkill(MagicBolt, 14, SkillLevel: 3),
                new ClassSkill(CastId(FamPhysDef), 14),              // Bulwark +8% P.Def
                new ClassSkill(MageAntiMagic, 14, SkillLevel: 2),    // +16 M.Def + 5% fizzle
                new ClassSkill(MasteryRobe, 14, SkillLevel: 2),      // Robe Armor Mastery +9 P.Def
                new ClassSkill(WeaponMastery, 14));                  // +4 M.Atk / +2 P.Atk

        // …and the ONE row that is a single race's: the HUMAN's Vampiric Bolt taster at 14 (his
        // `mage 1st.csv` row carries `Human` in the Race column, 2026-09-17).
        //
        // 🔑 IT IS A TASTER, AND IT KEEPS THE OLD ID ON PURPOSE. The Human's real ladder is
        //    `human_vampiric_bolt`, injected centrally from 20 and never taken away; this single rung
        //    is base-class content that the cleric's Holy Bolt `Replaces` at 20, exactly as it
        //    replaces Magic Bolt. Two ids is what lets one of those two things happen without the
        //    other — which is why he changed the id rather than extending this row.
        ClassSkills.Register(Race.Human, BaseClass.Mage, null,
            new ClassSkill(VampiricBolt, 14));
        // (The God base-mage line was deleted 2026-08-07 with the rest of the God layer.)

        // Second-class kits live in the per-line partial files (RegisterXxx()).
        RegisterSecondClasses();
        // 3rd-class (discipline) kits — placeholder lists in 24.0; fleshed out per
        // archetype in the content slices.
        RegisterThirdClasses();
        // 4th-class (ASCENDED discipline) kits — the 76-90 band. Registered against a key that
        // carries a TIER, so nothing here reaches a character who has not paid the Rite of Ascension.
        RegisterFourthClasses();
    }

    // Implemented across the partial files; each appends its lines.
    static partial void RegisterSecondClasses();
    static partial void RegisterThirdClasses();
    static partial void RegisterFourthClasses();
}
