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
    public const string TankWeaponMastery    = "tank_weapon_mastery";
    public const string WarriorWeaponMastery = "warrior_weapon_mastery";
    public const string RogueWeaponMastery   = "rogue_weapon_mastery";
    public const string ArcherWeaponMastery  = "archer_weapon_mastery";

    /// <summary>A caster weapon-mastery level: the given <paramref name="bonus"/> (M/P.Atk,
    /// reuse, cast/regen) applies ONLY with the wizard's weapon — a SWORD or BLUNT (1H/2H).
    /// ANYTHING ELSE — bow, dual, or an EMPTY HAND — gets NO bonus and halves cast speed
    /// (CastSpeedPct -1 ⇒ ×2 cast time). Stacked with the robe mastery's non-robe cast ×0.5,
    /// a bare-handed unarmoured mage casts at ×0.25. "Not using your optimal gear = penalty."</summary>
    internal static WeaponMasteryProfile CasterMastery(PassiveEffect bonus)
    {
        var penalty = new PassiveEffect(CastSpeedPct: -1.0f);
        return new(Sword: bonus, Blunt: bonus, Dual: penalty, Bow: penalty, Other: penalty);
    }

    /// <summary>A two-handed sword/blunt profile carrying the same PassiveEffect for both
    /// (the warrior 2H mastery doesn't distinguish sword vs blunt), gated to TwoHand.</summary>
    private static WeaponMasteryProfile TwoHand(PassiveEffect pe) =>
        new(Sword: pe, Blunt: pe, RequiredWeapon: WeaponType.TwoHanded);

    /// <summary>A rogue Weapon Mastery level: shared crit/acc/atk-speed on both dual and bow,
    /// plus each weapon's own flat P.Atk, and +200 range for the bow. (crit-rate ×1.2 is
    /// approximated as +critRate flat.)</summary>
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
        // Warrior — Two-Handed Mastery (CSV warrior 20-35): big P.Atk + crit damage with a
        // 2H sword/blunt, at the cost of some defence (p.def ×0.8) and evasion. 5 levels.
        new(WarriorWeaponMastery, "Two-Hand Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Mastery of TWO-HANDED swords and blunts: much greater attack "
                       + "power and critical damage, but lower defence and evasion. No effect one-handed.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12000),
                new SkillLevel(SpCost: 22000),
                new SkillLevel(SpCost: 40000),
            },
            WeaponMasteryLevels: new[]
            {
                // crit dmg is the CSV's FLAT +35/+48/+64/+84/+106 (attack added inside the crit),
                // not a multiplier — it used to be read as ×2.35 … ×3.06. See CritBlowAndDouble.md §3.
                TwoHand(new PassiveEffect(PhysAtkPct: 0.30f, PhysAtk: 13, CritDamageFlat: 35f,  Accuracy: 3, Evasion: -3, DefencePct: -0.20f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 15, CritDamageFlat: 48f,  Accuracy: 6, Evasion: -3, DefencePct: -0.20f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 17, CritDamageFlat: 64f,  Accuracy: 6, Evasion: -3, DefencePct: -0.20f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 20, CritDamageFlat: 84f,  Accuracy: 6, Evasion: -3, DefencePct: -0.20f)),
                TwoHand(new PassiveEffect(PhysAtkPct: 0.50f, PhysAtk: 20, CritDamageFlat: 106f, Accuracy: 6, Evasion: -3, DefencePct: -0.20f)),
            }),

        // Rogue — Weapon Mastery (CSV rogue 20-35): DUAL and BOW both gain +8.5% P.Atk plus
        // shared crit-damage / accuracy / crit-rate / attack-speed; each also gets its own flat
        // P.Atk, and BOW gains +200 range. 5 levels. Replaces the base fighter weapon mastery.
        new(RogueWeaponMastery, "Weapon Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Sharpens dual-wield and bow attacks: more attack power, "
                       + "critical damage, accuracy and (with a bow) greater range.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12000),
                new SkillLevel(SpCost: 22000),
                new SkillLevel(SpCost: 40000),
            },
            WeaponMasteryLevels: new[]
            {
                // crit dmg = the CSV's FLAT +35/+64/+80/+140/+165. The @24 and @28 rungs used to be
                // SWAPPED (80 before 64) as well as read as a multiplier. See CritBlowAndDouble.md §3.
                RogueWM(critDmg: 35f,  acc: 0, critRate: 0f,    atkSpd: 0f,    dualAtk: 8,  bowAtk: 30),
                RogueWM(critDmg: 64f,  acc: 3, critRate: 0f,    atkSpd: 0f,    dualAtk: 11, bowAtk: 42),
                RogueWM(critDmg: 80f,  acc: 3, critRate: 0f,    atkSpd: 0f,    dualAtk: 14, bowAtk: 56),
                RogueWM(critDmg: 140f, acc: 3, critRate: 0.10f, atkSpd: 0f,    dualAtk: 17, bowAtk: 74),
                RogueWM(critDmg: 165f, acc: 3, critRate: 0.10f, atkSpd: 0.05f, dualAtk: 21, bowAtk: 96),
            }),

        // Archer — bow: crit-damage + accuracy lean.
        WeaponMasteryPassive(ArcherWeaponMastery, "Bow Mastery", BaseClass.Fighter,
            "Increases attack power, critical damage and accuracy while wielding a bow.",
            new WeaponMasteryProfile(
                Bow: new PassiveEffect(PhysAtkPct: 0.12f, CritDamageFlat: 20f, Accuracy: 5))),

        // Tank — Weapon Mastery (CSV tank 20-35): flat + 8.5% attack power with ANY weapon.
        // 5 levels (@20/24/28/32/36). Replaces the base fighter weapon mastery.
        new(TankWeaponMastery, "Weapon Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { FighterWeaponMastery },
            Description: "Passive. Increases physical attack power with any weapon equipped.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3400,  Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 6)),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 8)),
                new SkillLevel(SpCost: 12000, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 10)),
                new SkillLevel(SpCost: 22000, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 12)),
                new SkillLevel(SpCost: 40000, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 15)),
            }),
    };
}
