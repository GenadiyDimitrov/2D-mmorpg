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
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.Register(race, BaseClass.Fighter, null,
                new ClassSkill(FighterArmorMastery, 5,  SkillLevel: 1),
                new ClassSkill(FighterWeaponMastery, 5, SkillLevel: 1),
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
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Ork })
            ClassSkills.Register(race, BaseClass.Mage, null,
                new ClassSkill(SelfHeal, 1),                         // Lv1 self heal (power 42)
                new ClassSkill(MagicBolt, 7, SkillLevel: 2),
                new ClassSkill(MasteryRobe, 7, SkillLevel: 1),       // Robe Armor Mastery +7 P.Def
                new ClassSkill(SelfHeal, 7, SkillLevel: 2),          // self heal power 67
                // The base mage's first buffs. These used to be ONE skill (the group "Might") —
                // it is the Warchanter's now, so the base mage learns the two singles instead
                // (owner 2026-07-31). Same +8%/+8%, two casts, 30 MP each.
                new ClassSkill(CastId(FamPhysAtk), 7),               // Might   +8% P.Atk
                new ClassSkill(CastId(FamPhysDef), 7),               // Bulwark +8% P.Def
                new ClassSkill(MageAntiMagic, 7, SkillLevel: 1),     // +12 M.Def
                new ClassSkill(MagicBolt, 14, SkillLevel: 3),
                new ClassSkill(VampiricBolt, 14),
                new ClassSkill(MageAntiMagic, 14, SkillLevel: 2),    // +16 M.Def + 5% fizzle
                new ClassSkill(SelfHeal, 14, SkillLevel: 3),         // self heal power 107
                new ClassSkill(MasteryRobe, 14, SkillLevel: 2),      // Robe Armor Mastery +9 P.Def
                new ClassSkill(WeaponMastery, 14));                  // +4 M.Atk / +2 P.Atk
        // (The God base-mage line was deleted 2026-08-07 with the rest of the God layer.)

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
