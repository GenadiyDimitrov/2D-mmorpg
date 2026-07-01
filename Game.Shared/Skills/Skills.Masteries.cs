namespace Game.Shared;

/// <summary>DATA-DRIVEN armor-mastery skills per 2nd-class archetype (the healer's lives
/// in Skills.Healer.cs). Each carries a per-worn-weight <see cref="ArmorMasteryProfile"/>:
/// a BONUS for the trained weight(s), a PENALTY for off-weights (robe never penalises;
/// tank/warrior are immune so their off-weights are Neutral). These REPLACE the shared
/// mastery_heavy/light/robe skills and move the per-class numbers out of the hardcoded
/// ArmorMastery table into data (per [[stats-via-skills-not-hardcoded]]). Level-scaled
/// defence is preserved via MasteryEffect.*PerLevel coefficients.</summary>
public static partial class SkillCatalog
{
    public const string TankArmorMastery    = "tank_armor_mastery";
    public const string WarriorArmorMastery = "warrior_armor_mastery";
    public const string RogueArmorMastery   = "rogue_armor_mastery";
    public const string ArcherArmorMastery  = "archer_armor_mastery";
    public const string NukerArmorMastery   = "nuker_armor_mastery";

    private static readonly MasteryEffect FighterHeavyPenalty =
        new(AtkSpeed: 0.8f, CastSpeed: 0.8f, MoveSpeed: 0.8f, Evasion: -3, Accuracy: -3);
    private static readonly MasteryEffect MageLightPenalty =
        new(AtkSpeed: 0.8f, CastSpeed: 0.8f, MoveSpeed: 0.8f, Evasion: -3, Accuracy: -3);
    private static readonly MasteryEffect MageHeavyPenalty =
        new(AtkSpeed: 0.5f, CastSpeed: 0.5f, MoveSpeed: 0.5f, HpRegen: 0.5f, MpRegen: 0.5f,
            Evasion: -10, Accuracy: -10);

    /// <summary>Rogue armor level: LIGHT armor gets the given bonus, heavy penalises, robe is
    /// inert. (CSV rogue "with light: …; heavy hinders".)</summary>
    private static ArmorMasteryProfile RogueLight(MasteryEffect light) =>
        new(Robe: ArmorMastery.Neutral, Light: light, Heavy: FighterHeavyPenalty);

    /// <summary>Tank Heavy Armor Mastery level: HEAVY armor grants flat P.Def, ×1.07 P.Def,
    /// 15% crit-damage reduction, ×1.1 max MP and −2 evasion. Off-weights are inert (tank is
    /// immune to armor penalties). (CSV tank "heavy: mp x1.1, p.def +N, p.def x1.07, crit dmg
    /// reduction 15%, eva -2". The @36 "mp x3.4" is treated as x1.1 — a likely CSV typo.)</summary>
    private static ArmorMasteryProfile TankHeavy(int def) => new(
        Robe:  ArmorMastery.Neutral,
        Light: ArmorMastery.Neutral,
        Heavy: new MasteryEffect(MaxMp: 1.1f, Defence: def, DefenceMult: 1.07f,
            CritDmgResist: 0.15f, Evasion: -2));

    /// <summary>Warrior armor-mastery level: flat P.Def + ×1.1 max MP on all weights; light
    /// armor also adds the given evasion. (CSV warrior "with all mp x1.1, p.def +N; light eva +E".)</summary>
    private static ArmorMasteryProfile WarriorArmor(int def, int lightEva) => new(
        Robe:  new MasteryEffect(Defence: def, MaxMp: 1.1f),
        Light: new MasteryEffect(Defence: def, MaxMp: 1.1f, Evasion: lightEva),
        Heavy: new MasteryEffect(Defence: def, MaxMp: 1.1f));

    private static SkillDef ArmorMasteryPassive(string id, BaseClass cls, ArmorMasteryProfile profile) =>
        new(id, "Armor Mastery", cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryHeavy, MasteryLight, MasteryRobe },
            Description: "Passive. Adapts your defences to the armor weight you wear — your "
                       + "trained weight grants a bonus; wearing the wrong weight hinders you.",
            Levels: new[] { new SkillLevel(SpCost: 500) },
            ArmorMasteryLevels: new[] { profile });

    private static SkillDef[] ArmorMasterySkills() => new SkillDef[]
    {
        // Tank — Heavy Armor Mastery (CSV tank 20-35): in HEAVY armor, big flat P.Def plus
        // ×1.07 P.Def, 15% crit-damage reduction and ×1.1 max MP, at a small evasion cost.
        // 5 levels (@20/24/28/32/36). Immune to off-weight penalties (Neutral otherwise).
        new(TankArmorMastery, "Heavy Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryHeavy, MasteryLight, MasteryRobe, FighterArmorMastery },
            Description: "Passive. In HEAVY armor: greatly increased physical defence, reduced "
                       + "critical damage taken and more max MP (slightly lower evasion).",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                TankHeavy(40), TankHeavy(47), TankHeavy(54), TankHeavy(61), TankHeavy(70),
            }),

        // Warrior — Armor Mastery (CSV warrior 20-35): +P.Def and +max MP with any weight;
        // LIGHT armor additionally boosts evasion. Continues the base fighter mastery (which it
        // replaces) with 5 levels (@20/24/28/32/36).
        new(WarriorArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryHeavy, MasteryLight, MasteryRobe, FighterArmorMastery },
            Description: "Passive. Improves defence and maximum MP with any armor weight; "
                       + "light armor also boosts evasion.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                WarriorArmor(19, 6), WarriorArmor(21, 8), WarriorArmor(23, 9),
                WarriorArmor(28, 9), WarriorArmor(32, 9),
            }),

        // Rogue — Armor Mastery (CSV rogue 20-35): in LIGHT armor, big evasion, +15% crit-rate
        // resist, +MP regen and (from L3) move speed; at L5 also +HP regen. Heavy penalises,
        // robe is inert. 5 levels (@20/24/28/32/36). Replaces the base fighter mastery.
        new(RogueArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryHeavy, MasteryLight, MasteryRobe, FighterArmorMastery },
            Description: "Passive. In LIGHT armor: greatly increased evasion, resistance to "
                       + "critical hits, faster MP regen and (at higher levels) speed. Heavy armor hinders you.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700),
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6000),
                new SkillLevel(SpCost: 11000),
                new SkillLevel(SpCost: 20000),
            },
            ArmorMasteryLevels: new[]
            {
                RogueLight(new MasteryEffect(Evasion: 7,  CritRateResist: 0.15f, MpRegen: 1.1f, Defence: 16)),
                RogueLight(new MasteryEffect(Evasion: 11, CritRateResist: 0.15f, MpRegen: 1.1f, Defence: 18)),
                RogueLight(new MasteryEffect(Evasion: 13, CritRateResist: 0.15f, MpRegen: 1.1f, MoveSpeed: 1.06f, Defence: 20)),
                RogueLight(new MasteryEffect(Evasion: 13, CritRateResist: 0.15f, MpRegen: 1.1f, MoveSpeed: 1.06f, Defence: 22)),
                RogueLight(new MasteryEffect(Evasion: 13, CritRateResist: 0.15f, MpRegen: 1.8f, HpRegen: 1.2f, MoveSpeed: 1.06f, Defence: 25)),
            }),

        // Archer — light bonus (crit lean); heavy penalises.
        ArmorMasteryPassive(ArcherArmorMastery, BaseClass.Fighter, new ArmorMasteryProfile(
            Robe:  ArmorMastery.Neutral,
            Light: new MasteryEffect(AtkSpeed: 1.3f, CritRate: 0.05f, CritDamage: 0.2f, Evasion: 4, Accuracy: 4, DefPerLevel: 0.5f),
            Heavy: FighterHeavyPenalty)),

        // Nuker — robe caster bonus; light/heavy penalise (mage).
        ArmorMasteryPassive(NukerArmorMastery, BaseClass.Mage, new ArmorMasteryProfile(
            Robe:  new MasteryEffect(CastSpeed: 1.4f, MpRegen: 1.3f, MaxMp: 1.15f,
                InterruptResistPerLevel: 1f, MagicDefence: 10, MagicDefPerLevel: 0.5f, DefPerLevel: 0.5f),
            Light: MageLightPenalty,
            Heavy: MageHeavyPenalty)),
    };
}
