namespace Game.Shared;

/// <summary>Base Fighter kit — the skills available to all fighters before (and
/// after) the level-20 class change. Archer's bow skill (Power Shot) lives here
/// too, since archers are a fighter archetype and have no separate base file.</summary>
public static partial class SkillCatalog
{
    public const string PowerStrike = "power_strike";
    public const string WarCry = "war_cry";
    public const string GreaterWarCry = "greater_war_cry";
    public const string BattleFury = "battle_fury";
    public const string Fortify = "fortify";
    public const string ShieldMastery = "shield_mastery";
    public const string MightyBlow = "mighty_blow";
    public const string TwinSlash = "twin_slash";
    public const string PowerShot = "power_shot";
    public const string Disrupt = "disrupt";
    public const string CleavingStrike = "cleaving_strike";   // first "[Double]" skill (warrior)
    public const string ShieldBash = "shield_bash";           // physical Stun (contested CC)
    public const string TerrifyingRoar = "terrifying_roar";   // physical Fear (contested CC)
    public const string Hamstring = "hamstring";              // physical Slow (contested CC)
    public const string WarFocus = "war_focus";               // self-buff: +skill damage

    private static SkillDef[] FighterSkills() => new SkillDef[]
    {
        new(PowerStrike, "Power Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 30,
            Category: SkillCategory.Physical,
            Description: "A forceful melee blow. Bonus accuracy, but can still miss."),

        // Cleaving Strike — first "[Double]" skill (P1 primitive demo). A big single-target
        // slash that can deal ×2 damage on a chance from the higher of DEX/ATK (cap 30%).
        new(CleavingStrike, "Cleaving Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 20, CastTicks: 5, CooldownTicks: 60, Range: 0, Power: 70,
            Category: SkillCategory.Physical, CanDouble: true,
            Description: "A heavy slash (power 70) that can strike for DOUBLE damage [Double]."),

        // Shield Bash — contested STUN (P1 primitive demo): cannot move/cast/attack for 3s.
        // Lands on ATK-vs-CON (stun is always physical); bosses immune. Numbers placeholder.
        new(ShieldBash, "Shield Bash", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 20, CastTicks: 5, CooldownTicks: 150, Range: 0, Power: 0,
            DurationTicks: 30, BuffKey: "stun", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Description: "Bash the target, stunning it for 3s (cannot move or act). ATK-vs-CON; bosses immune."),

        // Terrifying Roar — contested FEAR (P1 primitive demo): cannot cast/attack for 5s
        // (can still move). Warriors apply physical fear; lands on ATK-vs-CON; bosses immune.
        new(TerrifyingRoar, "Terrifying Roar", BaseClass.Fighter, SkillEffect.Fear,
            MpCost: 25, CastTicks: 5, CooldownTicks: 200, Range: 0, Power: 0,
            DurationTicks: 50, BuffKey: "fear", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Description: "A fearsome roar — the target cannot cast or attack for 5s (can still move). ATK-vs-CON; bosses immune."),

        // Hamstring — contested PHYSICAL Slow (the physical counterpart to the mage's Frost
        // Bind): ATK-vs-CON, −60% move speed for 8s. Shows slow can be physical OR magical.
        new(Hamstring, "Hamstring", BaseClass.Fighter, SkillEffect.Slow,
            MpCost: 18, CastTicks: 5, CooldownTicks: 80, Range: 0, Power: 0,
            DurationTicks: 80, BuffKey: "slow", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.60f) },
            Description: "A crippling cut — −60% move speed for 8s. Lands on an ATK-vs-CON contest."),

        // War Focus — self-buff granting +20% PHYSICAL skill damage for 30s. Demonstrates the
        // split skill-damage stat (physical channel only, so it never boosts spells/heals).
        new(WarFocus, "War Focus", BaseClass.Fighter, SkillEffect.BuffPhysSkillDamage,
            MpCost: 20, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "war_focus", Rank: 1,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffPhysSkillDamage, 0.20f) },
            Description: "Sharpens your technique: +20% physical skill damage for 30s."),

        new(WarCry, "War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "might", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.20f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +20% Attack Power for 30s."),

        new(GreaterWarCry, "Greater War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 18, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "might", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.30f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +30% Attack Power for 30s."),

        new(BattleFury, "Battle Fury", BaseClass.Fighter,
            SkillEffect.BuffAtk | SkillEffect.BuffMoveSpeed,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "battle_fury", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtk, 0.20f),
                new(SkillEffect.BuffMoveSpeed, 0.15f),
            },
            Category: SkillCategory.Buff,
            Description: "+20% Attack and +15% Move Speed for 30s."),

        new(Fortify, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
            DurationTicks: 250, BuffKey: "fortify", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.50f) },
            Category: SkillCategory.Buff,
            Description: "Tank stance: +50% Defence for 25s."),

        new(ShieldMastery, "Shield Mastery", BaseClass.Fighter,
            SkillEffect.BuffBlockChance | SkillEffect.BuffShieldDef,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "shield_mastery", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                // +30% block chance, +50% shield defence (only with a shield).
                new(SkillEffect.BuffBlockChance, 0.30f, ModifierMode.Percent),
                new(SkillEffect.BuffShieldDef, 0.50f, ModifierMode.Percent),
            },
            Category: SkillCategory.Buff, SpCost: 2000, TargetMode: TargetMode.SelfOnly,
            Description: "Tank passive: greatly improves your shield's block " +
                         "chance and defence (only while a shield is equipped)."),

        new(MightyBlow, "Mighty Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 18, CastTicks: 7, CooldownTicks: 60, Range: 0, Power: 85,
            Category: SkillCategory.Physical, SureHit: true,
            Description: "A devastating two-hand strike for heavy damage — never misses "
                       + "(ignores evasion). The warrior's answer to dodgy targets."),

        new(TwinSlash, "Twin Slash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 12, CastTicks: 3, CooldownTicks: 25, Range: 0, Power: 55,
            Category: SkillCategory.Physical,
            Description: "Two rapid dagger slashes. Short cast and cooldown."),

        new(PowerShot, "Power Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 16, CastTicks: 8, CooldownTicks: 40, Range: 900, Power: 70,
            Category: SkillCategory.Physical,
            Description: "A long-range aimed shot dealing heavy damage."),

        // The dedicated interrupt: INSTANT (CastTicks 0), tiny damage, but
        // overwhelming InterruptPower so it ALWAYS breaks an enemy cast.
        new(Disrupt, "Disrupt", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 0, CooldownTicks: 80, Range: 0, Power: 5,
            Category: SkillCategory.Physical, InterruptPower: 99999, SureHit: true,
            Description: "Instant strike that never misses and always interrupts an enemy's cast."),
    };
}
