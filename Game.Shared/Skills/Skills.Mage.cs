namespace Game.Shared;

/// <summary>Base Mage kit — nukes, the basic heal, and the def-curse line,
/// available to all mages (the nuker/healer upgrades that replace the basics live
/// here too; 3rd-class discipline spells are in their own Skills.&lt;Discipline&gt;.cs).</summary>
public static partial class SkillCatalog
{
    public const string MagicBolt = "magic_bolt";
    public const string Heal = "heal";
    public const string SelfHeal = "self_heal";
    public const string Might = "might";
    public const string MageAntiMagic = "anti_magic_mage";
    public const string VampiricBolt = "vampiric_bolt";
    public const string WeaponMastery = "weapon_mastery";
    public const string Weakness = "weakness";
    public const string GreaterWeakness = "greater_weakness";
    public const string GreaterHeal = "greater_heal";
    public const string FlameBolt = "flame_bolt";
    public const string HolyStrike = "holy_strike";

    private static SkillDef[] MageSkills() => new SkillDef[]
    {
        // Magic Bolt — the starter nuke, 3 levels (auto-learn Lv.1; Lv.2/3 learned).
        new(MagicBolt, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 9, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 12,
            Category: SkillCategory.Magic, InitialMpCost: 2,
            Description: "Hurls a bolt of force. Spells fail rather than miss.",
            Levels: new[]
            {
                new SkillLevel(Power: 12, MpCost: 9,  InitialMpCost: 2, SpCost: 0,    Description: "Magic damage, power 12."),
                new SkillLevel(Power: 15, MpCost: 10, InitialMpCost: 2, SpCost: 480,  Description: "Magic damage, power 15."),
                new SkillLevel(Power: 21, MpCost: 15, InitialMpCost: 3, SpCost: 2200, Description: "Magic damage, power 21."),
            }),

        // Self Heal — early self-only heal, replaced by the targeted Heal at 7.
        new(SelfHeal, "Self Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 7, CastTicks: 50, CooldownTicks: 20, Range: 0, Power: 42,
            Category: SkillCategory.Heal, InitialMpCost: 2, SpCost: 160,
            TargetMode: TargetMode.SelfOnly,
            Description: "Restores your own HP (power 42). Scales with WIT."),

        // Heal — targeted heal (ally or self); replaces Self Heal. Lvls 1-2 are the
        // base-mage line; the 2nd-class Healer CONTINUES it at lvls 3-6 (20/25/30/35).
        new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 14, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 67,
            Category: SkillCategory.Heal, InitialMpCost: 3,
            Replaces: new[] { SelfHeal },
            Description: "Restores a friendly target's HP (or your own). Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 67,  MpCost: 14, InitialMpCost: 3,  SpCost: 480,   Description: "Heal power 67."),
                new SkillLevel(Power: 107, MpCost: 22, InitialMpCost: 5,  SpCost: 2200,  Description: "Heal power 107."),
                new SkillLevel(Power: 151, MpCost: 30, InitialMpCost: 6,  SpCost: 3200,  Description: "Heal power 151."),
                new SkillLevel(Power: 195, MpCost: 38, InitialMpCost: 8,  SpCost: 6400,  Description: "Heal power 195."),
                new SkillLevel(Power: 245, MpCost: 44, InitialMpCost: 9,  SpCost: 12800, Description: "Heal power 245."),
                new SkillLevel(Power: 301, MpCost: 52, InitialMpCost: 11, SpCost: 25000, Description: "Heal power 301."),
            }),

        // Might — party-castable Attack & Defence buff (20 min). Lvl 1 = base-mage
        // (+8%/+8%); the Healer CONTINUES it at lvls 2-3 (20/25). Lvl 4 (with basic-
        // attack vampirism) lands in Increment 2.
        new(Might, "Might", BaseClass.Mage, SkillEffect.BuffAtk | SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "mage_might", Rank: 1, InitialMpCost: 4,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtk, 0.08f), new(SkillEffect.BuffDef, 0.08f),
            },
            Category: SkillCategory.Buff, SpCost: 960,
            Description: "Blesses an ally (or self) with +Attack and +Defence for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 20, InitialMpCost: 4,  SpCost: 960,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.08f), new(SkillEffect.BuffDef, 0.08f) },
                    Description: "+8% Attack and +8% Defence for 20 minutes."),
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 3200,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.12f), new(SkillEffect.BuffDef, 0.08f) },
                    Description: "+12% Attack and +8% Defence for 20 minutes."),
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 6400,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.12f), new(SkillEffect.BuffDef, 0.12f) },
                    Description: "+12% Attack and +12% Defence for 20 minutes."),
            }),

        // Anti-Magic — learnable mage passive: +M.Def and a magic-fail (fizzle) floor.
        // Lvls 1-2 = base mage; the Healer CONTINUES it at lvls 3-6 (20/25/30/35). The
        // CSV "mRes %" is modelled as the fizzle floor (the resolver takes the max floor).
        new(MageAntiMagic, "Anti-Magic", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Hardens you against hostile magic.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 480,   Passive: new PassiveEffect(MagicDefence: 12), Description: "+12 magic defence."),
                new SkillLevel(SpCost: 2200,  Passive: new PassiveEffect(MagicDefence: 16, MagicFailFloor: 0.05f),
                    Description: "+16 magic defence and a 5% chance for spells to fizzle on you."),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagicDefence: 20, MagicFailFloor: 0.05f),
                    Description: "+20 magic defence; spells fizzle on you at least 5% of the time."),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(MagicDefence: 25, MagicFailFloor: 0.05f),
                    Description: "+25 magic defence; spells fizzle on you at least 5% of the time."),
                new SkillLevel(SpCost: 12800, Passive: new PassiveEffect(MagicDefence: 30, MagicFailFloor: 0.10f),
                    Description: "+30 magic defence; spells fizzle on you at least 10% of the time."),
                new SkillLevel(SpCost: 25000, Passive: new PassiveEffect(MagicDefence: 36, MagicFailFloor: 0.10f),
                    Description: "+36 magic defence; spells fizzle on you at least 10% of the time."),
            }),

        // Vampiric Bolt — magic nuke that heals the caster for 40% of damage dealt.
        new(VampiricBolt, "Vampiric Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 28, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 21,
            Category: SkillCategory.Magic, InitialMpCost: 6, SpCost: 2200, Lifesteal: 0.40f,
            Description: "A draining bolt (power 21) that heals you for 40% of the damage dealt."),

        // Weapon Mastery — flat attack passive (asymmetric: more M.Atk than P.Atk).
        new(WeaponMastery, "Weapon Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 2200,
            Passive: new PassiveEffect(MagAtk: 4, PhysAtk: 2),
            Description: "Passive. +4 M.Atk and +2 P.Atk."),

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

        // Holy Bolt — the Healer's offensive spell (replaces Magic Bolt). ONE skill;
        // per-race NAME only (Holy/Moonlight/Spirit Bolt) via ClassSkill.DisplayName.
        // 4 levels learned at 20/25/30/35.
        new(HolyStrike, "Holy Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 20, CastTicks: 40, CooldownTicks: 10, Range: 750, Power: 21,
            Replaces: new[] { MagicBolt },   // the healer's nuke replaces the basic
            Category: SkillCategory.Magic, InitialMpCost: 4,
            Description: "A bolt of holy power — the Healer's offensive spell (replaces Magic Bolt). Spells fail rather than miss.",
            Levels: new[]
            {
                new SkillLevel(Power: 21, MpCost: 20, InitialMpCost: 4, SpCost: 3200,  Description: "Magic damage, power 21."),
                new SkillLevel(Power: 25, MpCost: 23, InitialMpCost: 5, SpCost: 3200,  Description: "Magic damage, power 25."),
                new SkillLevel(Power: 30, MpCost: 26, InitialMpCost: 6, SpCost: 12800, Description: "Magic damage, power 30."),
                new SkillLevel(Power: 36, MpCost: 31, InitialMpCost: 7, SpCost: 25000, Description: "Magic damage, power 36."),
            }),

        new(GreaterWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 22, CastTicks: 5, CooldownTicks: 300, Range: 500, Power: 0,
            DurationTicks: 200, BuffKey: "curse_def", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.45f) },
            Category: SkillCategory.Debuff, SureHit: true,
            Description: "A deeper curse: -45% Defence for 20s (never fizzles)."),
    };
}
