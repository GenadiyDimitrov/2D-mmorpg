namespace Game.Shared;

/// <summary>Base Mage kit — nukes, the basic heal, and the def-curse line,
/// available to all mages (the nuker/healer upgrades that replace the basics live
/// here too; 3rd-class discipline spells are in their own Skills.&lt;Discipline&gt;.cs).</summary>
public static partial class SkillCatalog
{
    public const string MagicBolt = "magic_bolt";
    public const string Heal = "heal";
    public const string Weakness = "weakness";
    public const string GreaterWeakness = "greater_weakness";
    public const string GreaterHeal = "greater_heal";
    public const string FlameBolt = "flame_bolt";
    public const string HolyStrike = "holy_strike";

    private static SkillDef[] MageSkills() => new SkillDef[]
    {
        new(MagicBolt, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 12, CastTicks: 20, CooldownTicks: 10, Range: 500, Power: 45,
            Category: SkillCategory.Magic,
            Description: "Hurls a bolt of force. Spells fail rather than miss."),

        new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 20, CastTicks: 25, CooldownTicks: 10, Range: 0, Power: 60,
            Category: SkillCategory.Heal,
            Description: "Restores your own HP. Scales with WIT."),

        new(Weakness, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 500, Power: 0,
            DurationTicks: 150, BuffKey: "curse_def", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.30f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "Curses the target: -30% Defence for 15s (instant cast, never fizzles)."),

        new(GreaterHeal, "Greater Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 35, CastTicks: 35, CooldownTicks: 15, Range: 500, Power: 150,
            Replaces: new[] { Heal },   // upgrades (replaces) the basic heal
            Category: SkillCategory.Heal,
            Description: "A powerful heal that can target an ally at range (replaces Heal)."),

        new(FlameBolt, "Flamebolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 24, CastTicks: 40, CooldownTicks: 10, Range: 500, Power: 95,
            Replaces: new[] { MagicBolt },   // upgrades (replaces) the basic nuke
            Category: SkillCategory.Magic,
            Description: "A searing bolt — the nuker's stronger basic attack (replaces Magic Bolt)."),

        new(HolyStrike, "Holy Strike", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 20, CastTicks: 30, CooldownTicks: 10, Range: 500, Power: 70,
            Replaces: new[] { MagicBolt },   // the healer's nuke replaces the basic
            Category: SkillCategory.Magic,
            Description: "A bolt of light — the healer's offensive spell (replaces Magic Bolt)."),

        new(GreaterWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 22, CastTicks: 5, CooldownTicks: 300, Range: 500, Power: 0,
            DurationTicks: 200, BuffKey: "curse_def", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.45f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "A deeper curse: -45% Defence for 20s (never fizzles)."),
    };
}
