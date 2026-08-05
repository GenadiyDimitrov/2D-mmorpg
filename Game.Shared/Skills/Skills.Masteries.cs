namespace Game.Shared;

/// <summary>DATA-DRIVEN armor-mastery skills per 2nd-class archetype (the healer's lives
/// in Skills.Healer.cs). Each carries a per-worn-weight <see cref="ArmorMasteryProfile"/>:
/// a BONUS for the trained weight(s), a PENALTY for off-weights (robe never penalises;
/// tank/warrior are immune so their off-weights are inert = default). These REPLACE the base
/// fighter/mage masteries; every stat is explicit per skill level — no character-level or
/// class formula (per [[stats-via-skills-not-hardcoded]]).</summary>
public static partial class SkillCatalog
{
    public const string TankArmorMastery    = "tank_armor_mastery";
    public const string WarriorArmorMastery = "warrior_armor_mastery";
    public const string RogueArmorMastery   = "rogue_armor_mastery";
    public const string ArcherArmorMastery  = "archer_armor_mastery";
    public const string NukerArmorMastery   = "nuker_armor_mastery";

    /// <summary>Rogue armor level. The CSV splits in two, exactly like the warrior's:
    /// <c>with all</c> (MP regen + flat P.Def, and HP regen on the last rung) applies in EVERY
    /// weight, and <c>with light</c> (evasion, crit-rate resist, speed) only in LIGHT. Off-weights
    /// keep the "with all" half and simply miss the light half — still no active penalty for a
    /// fighter (owner ruling 2026-07-01). Everything used to be gated on light, which left a
    /// rogue in robe/heavy with no MP regen, no HP regen and no P.Def at all.</summary>
    private static ArmorMasteryProfile RogueArmor(StatMods all, int lightEva, float lightSpeedPct = 0f) =>
        new(Robe: all, Heavy: all,
            Light: all with { Evasion = lightEva, CritRateResist = 0.15f, MoveSpeedPct = lightSpeedPct });

    /// <summary>Tank Heavy Armor Mastery level: HEAVY armor grants flat P.Def, ×1.07 P.Def,
    /// 15% crit-damage reduction, ×mpReg MP regen and −2 evasion. Off-weights are inert (tank is
    /// immune to armor penalties). (CSV tank "heavy: mpReg x1.1, p.def +N, p.def x1.07, crit dmg
    /// reduction 15%, eva -2"; the @36 level is mpReg ×3.4.)</summary>
    private static ArmorMasteryProfile TankHeavy(int def, float mpReg = 1.1f) => new(
        Robe:  default,
        Light: default,
        Heavy: new StatMods(MpRegenPct: mpReg - 1f, PDef: def, PDefPct: 0.07f,
            CritDmgResist: 0.15f, Evasion: -2));

    /// <summary>Warrior armor-mastery level: flat P.Def + ×1.1 MP regen on all weights; light
    /// armor also adds the given evasion. (CSV warrior "with all mp[Reg] x1.1, p.def +N; light eva +E".)</summary>
    private static ArmorMasteryProfile WarriorArmor(int def, int lightEva) => new(
        Robe:  new StatMods(PDef: def, MpRegenPct: 0.1f),
        Light: new StatMods(PDef: def, MpRegenPct: 0.1f, Evasion: lightEva),
        Heavy: new StatMods(PDef: def, MpRegenPct: 0.1f));

    private static SkillDef ArmorMasteryPassive(string id, BaseClass cls, ArmorMasteryProfile profile) =>
        new(id, "Armor Mastery", cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
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
            Replaces: new[] { FighterArmorMastery },
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
                TankHeavy(40), TankHeavy(47), TankHeavy(54), TankHeavy(61), TankHeavy(70, mpReg: 3.4f),
            }),

        // Warrior — Armor Mastery (CSV warrior 20-35): +P.Def and +max MP with any weight;
        // LIGHT armor additionally boosts evasion. Continues the base fighter mastery (which it
        // replaces) with 5 levels (@20/24/28/32/36).
        new(WarriorArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
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

        // Rogue — Armor Mastery (CSV rogue 20-35): "with all" = ×1.1 MP regen + flat P.Def (at L5
        // ×1.8 MP regen and ×1.2 HP regen); "with light" adds big evasion, +15% crit-rate resist
        // and (from L3) move speed. 5 levels (@20/24/28/32/36). Replaces the base fighter mastery.
        new(RogueArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { FighterArmorMastery },
            Description: "Passive. Improves defence and MP regeneration with any armor weight; "
                       + "in LIGHT armor it also grants greatly increased evasion, resistance to "
                       + "critical hits and (at higher levels) speed.",
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
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 16), lightEva: 7),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 18), lightEva: 11),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 20), lightEva: 13, lightSpeedPct: 0.06f),
                RogueArmor(new StatMods(MpRegenPct: 0.1f, PDef: 22), lightEva: 13, lightSpeedPct: 0.06f),
                RogueArmor(new StatMods(MpRegenPct: 0.8f, HpRegenPct: 0.2f, PDef: 25), lightEva: 13, lightSpeedPct: 0.06f),
            }),

        // Archer — light bonus (crit lean). Heavy/robe/none inert: no fighter penalty, just no bonus.
        ArmorMasteryPassive(ArcherArmorMastery, BaseClass.Fighter, new ArmorMasteryProfile(
            Robe:  default,
            // NOTE: the old profile used DefPerLevel: 0.5f (per-CHARACTER-level def); StatMods has
            // no per-level field, so it's baked to a flat PDef here (~mid of the archer band). Archer
            // mastery numbers are placeholders — retune with the rest. See docs/design/StatMods.md.
            Light: new StatMods(AtkSpeedPct: 0.3f, CritRate: 0.05f, CritDamage: 0.2f, Evasion: 4, Accuracy: 4, PDef: 15),
            Heavy: default)),

        // Nuker — Mage Armor Mastery (CSV nuker 20-35): in ROBE, +MP regen, +P.Def, +max MP
        // and a "mpWhenRestored" bonus (extra MP each time Restore Spirit lands). Light/Heavy
        // penalise casting (mage). 4 levels (@20/25/30/35). Replaces the base Robe/Light mastery.
        new(NukerArmorMastery, "Mage Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { MasteryRobe },
            Description: "Passive. In ROBE: faster MP regen, more defence and max MP, and extra "
                       + "MP from your own MP-restoration. Light/heavy armor slows your casting.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 9600),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 25000),
            },
            ArmorMasteryLevels: new[]
            {
                NukerRobe(pDef: 20, maxMp: 20, restore: 25),
                NukerRobe(pDef: 25, maxMp: 20, restore: 30),
                NukerRobe(pDef: 30, maxMp: 30, restore: 35),
                NukerRobe(pDef: 35, maxMp: 30, restore: 40),
            }),
    };

    // Off-weight caster penalty (factor→pct: ×0.8 → −0.2 attack, ×0.5 → −0.5 cast).
    private static readonly StatMods NukerArmorPenalty =
        new(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f);

    /// <summary>Nuker robe-mastery level: ROBE gets +MP regen, flat P.Def, flat max MP and the
    /// mpWhenRestored bonus; light/heavy penalise casting. (CSV nuker "Robe: mpReg x1.2, pDef +N,
    /// maxMP +M, mpWhenRestored +R; Light/Heavy: as x0.8, cast x0.5".)</summary>
    private static ArmorMasteryProfile NukerRobe(int pDef, int maxMp, int restore) => new(
        Robe:  new StatMods(MpRegenPct: 0.2f, PDef: pDef, MaxMp: maxMp, RestoreMpBonus: restore),
        Light: NukerArmorPenalty,
        Heavy: NukerArmorPenalty,
        None:  NukerArmorPenalty);
}
