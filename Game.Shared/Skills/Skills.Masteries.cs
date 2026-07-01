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
        // Tank — heavy bonus; immune to off-weight penalties (Neutral elsewhere).
        ArmorMasteryPassive(TankArmorMastery, BaseClass.Fighter, new ArmorMasteryProfile(
            Robe:  ArmorMastery.Neutral,
            Light: ArmorMastery.Neutral,
            Heavy: new MasteryEffect(MaxHp: 1.2f, HpRegen: 1.3f,
                Defence: 20, DefPerLevel: 1f, MagicDefence: 20, MagicDefPerLevel: 0.5f))),

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

        // Rogue — light bonus; heavy penalises; robe never penalises.
        ArmorMasteryPassive(RogueArmorMastery, BaseClass.Fighter, new ArmorMasteryProfile(
            Robe:  ArmorMastery.Neutral,
            Light: new MasteryEffect(AtkSpeed: 1.35f, MoveSpeed: 1.1f, Evasion: 5, Accuracy: 5, DefPerLevel: 0.5f),
            Heavy: FighterHeavyPenalty)),

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
