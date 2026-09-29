namespace Game.Shared;

/// <summary>DATA-DRIVEN weapon-mastery skills for the FIGHTER 2nd-class archetypes
/// (Tank/Warrior/Rogue/Archer). Each carries a per-equipped-weapon
/// <see cref="WeaponMasteryProfile"/>: holding the class's intended weapon grants a bonus
/// (its identity); any other weapon simply grants nothing (no penalty — unlike armor
/// weight). The effect reuses <see cref="PassiveEffect"/> and flows through the SAME
/// passive-application path in Entity.RecomputeDerived, just gated on the equipped
/// WeaponType. The weapon sibling of the armor masteries (Skills.Masteries.cs);
/// Increment 2 of [[weapon-armor-mastery-design]].
///
/// MAGES (Nuker/Healer) intentionally have NO weapon-type mastery: their identity comes
/// from armor mastery (robe, +light for healers / +heavy for buffers) plus the flat
/// pAtk/mAtk passive (Weapon/Spell Mastery). Weapon TYPE doesn't matter for casters.
///
/// NUMBERS ARE PLACEHOLDERS — tune during testing.</summary>
public static partial class SkillCatalog
{
    // ⚠ `warriors_strength` — HIS SPELLING, and the id is his to spell. The C# const reads
    //   `WarriorsStrength` because that is what every other identifier in this file does; the STRING
    //   is what lands in a save file and in his CSV, and those two must agree letter for letter.
    // (`archer_weapon_mastery` — deleted 2026-08-07, playtest-19 `0a`/G1. Orphaned by the
    //  archer→rogue merge: Rogue Weapon Mastery already carries the BOW rungs, so this was a
    //  second bow passive nobody could be granted. Don't re-add it.)

    /// <summary>A caster weapon-mastery level: the given <paramref name="bonus"/> (M/P.Atk,
    /// reuse, cast/regen) applies ONLY with the wizard's weapon — a SWORD or BLUNT (1H/2H).
    /// ANYTHING ELSE — bow, dual, or an EMPTY HAND — gets NO bonus and halves cast speed
    /// (CastSpeedPct -1 ⇒ ×2 cast time). Stacked with the robe mastery's non-robe cast ×0.5,
    /// a bare-handed unarmoured mage casts at ×0.25. "Not using your optimal gear = penalty."</summary>
    /// ⚠ 2026-08-07: the wrong-weapon PENALTY is gone from here (it was `CastSpeedPct: -1.0f` on
    /// dual/bow/other). Spellcaster Mastery owns every weapon penalty now — owner: *"no other weapon
    /// penalties, they come from spellcaster"* — and stacking a −100% cast on top of Spellcaster's
    /// ×0.5 was double-charging the same rule. A caster mastery is now purely "sword or blunt earns
    /// this bonus; anything else earns nothing", which is how every OTHER weapon mastery already reads.
    internal static WeaponMasteryProfile CasterMastery(PassiveEffect bonus) =>
        new(Sword: bonus, Blunt: bonus);

    /// <summary>The WARCHANTER's version of the same thing: BLUNT or BOW, never sword. Every one of his
    /// `buffer 3rd.csv` Spell Mastery rows reads *"With blunt/bow weapon"*, because his buffer's three
    /// races hold a blunt (Human, Demon) or a bow (Elf) and nothing else. It is a separate helper rather
    /// than a parameter so the difference is visible at every call site — a caster mastery that pays on
    /// a BOW is unusual, and it only works at all because Harmonist Bow Proficiency cancels the
    /// untrained-weapon penalty that would otherwise be eating half the same character's magic.</summary>
    internal static WeaponMasteryProfile BufferMastery(PassiveEffect bonus) =>
        new(Blunt: bonus, Bow: bonus);

    /// <summary>A two-handed sword/blunt profile carrying the same PassiveEffect for both
    /// (the warrior 2H mastery doesn't distinguish sword vs blunt), gated to TwoHand.
    /// ⚠ The sword-vs-blunt split between the two warrior DISCIPLINES (melee = 2H sword,
    /// AoE = 2H blunt) is a 3rd-class rule and does NOT belong here — at 2nd class the warrior is
    /// one class and takes either. See `BL-104`.</summary>
    private static WeaponMasteryProfile TwoHand(PassiveEffect pe) =>
        new(Sword: pe, Blunt: pe,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt,
            RequiredHands: WeaponHands.Two);



    /// <summary>A rogue Weapon Mastery level: shared crit/acc/atk-speed on both dual and bow,
    /// plus each weapon's own flat P.Atk, and +200 range for the bow. <paramref name="critRate"/>
    /// is a MULTIPLIER on the weapon's crit base (0.20 = ×1.20) — his one rogue crit passive.</summary>
    private static WeaponMasteryProfile RogueWM(float critDmg, int acc, float critRate, float atkSpd, int dualAtk, int bowAtk) =>
        new(Dual: new PassiveEffect(PhysAtkPct: 0.085f, CritDamageFlat: critDmg, Accuracy: acc, CritRate: critRate, AtkSpeedPct: atkSpd, PhysAtk: dualAtk),
            Bow:  new PassiveEffect(PhysAtkPct: 0.085f, CritDamageFlat: critDmg, Accuracy: acc, CritRate: critRate, AtkSpeedPct: atkSpd, PhysAtk: bowAtk, BowRange: 200f));

    private static SkillDef WeaponMasteryPassive(string id, string name, BaseClass cls,
        string desc, WeaponMasteryProfile profile) =>
        new(id, name, cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. " + desc,
            Levels: new[] { new SkillLevel(SpCost: 500) },
            WeaponMasteryLevels: new[] { profile });

    private static SkillDef[] WeaponMasterySkills() => new SkillDef[]
    {



        // (Archer "Bow Mastery" DELETED 2026-08-07 with its id — the rogue mastery above carries
        //  the bow profile since the merge.)

    };
}
