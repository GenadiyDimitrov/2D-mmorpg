namespace Game.Shared;

/// <summary>Cross-class skills shared by many classes / not tied to one discipline:
/// armor-weight masteries, the combat-training passives, the class-identity "sure"
/// floor passives (and their auto-grant mapping), buff-potion buffs, the HP Boost
/// line, and Wind Walk. Class-defining combat skills live in their own files.</summary>
public static partial class SkillCatalog
{
    // ---- Armor-weight masteries (learnable PASSIVES; applied by ArmorMastery in
    //      RecomputeDerived when the matching weight is worn AND learned). ----
    public const string MasteryHeavy = "mastery_heavy";
    public const string MasteryLight = "mastery_light";
    public const string MasteryRobe  = "mastery_robe";
    // ---- Combat "training" passives, auto-granted at level 40 (soulshot/spiritshot
    //      stand-in). Doubling the atk STAT gives ×2 physical (linear) but ×1.414
    //      magic (√mAtk) — the soulshot/spiritshot ratio. ----
    public const string PhysicalTraining = "physical_training";
    public const string SpiritTraining   = "spirit_training";
    // ---- Class identity "sure" floor passives (auto-granted at class-change
    //      milestones 20/40/76). The floor VALUES live here in the SkillDefs, not
    //      hardcoded in StatCalculator. See FloorPassiveFor + docs/CombatResolution.md. ----
    public const string EvadeMastery1 = "evade_mastery_1";   // Rogue 10%
    public const string EvadeMastery2 = "evade_mastery_2";   // Rogue 20%
    public const string EvadeMastery3 = "evade_mastery_3";   // Rogue 30%
    public const string EvadeMasteryA1 = "evade_mastery_a1"; // Archer 5%
    public const string EvadeMasteryA2 = "evade_mastery_a2"; // Archer 10%
    public const string EvadeMasteryA3 = "evade_mastery_a3"; // Archer 15%
    public const string PrecisionMastery1 = "precision_mastery_1"; // Warrior hit 10%
    public const string PrecisionMastery2 = "precision_mastery_2"; // Warrior hit 20%
    public const string PrecisionMastery3 = "precision_mastery_3"; // Warrior hit 30%
    public const string AntiMagic1 = "anti_magic_1";   // Tank 10%
    public const string AntiMagic2 = "anti_magic_2";   // Tank 15%
    public const string AntiMagic3 = "anti_magic_3";   // Tank 20%
    public const string SpellWard  = "spell_ward";     // Mage (Nuker/Healer) 10% from 40
    // ---- Buff-potion buffs (consumed, not cast). ----
    public const string PBuffSpeedC = "pbuff_speed_c";
    public const string PBuffSpeedU = "pbuff_speed_u";
    public const string PBuffSpeedR = "pbuff_speed_r";
    public const string PBuffCastC = "pbuff_cast_c";
    public const string PBuffCastU = "pbuff_cast_u";
    public const string PBuffCastR = "pbuff_cast_r";
    public const string PBuffAtkC = "pbuff_atk_c";
    public const string PBuffAtkU = "pbuff_atk_u";
    public const string PBuffAtkR = "pbuff_atk_r";
    // ---- Learnable HP Boost line (3 ranks, same BuffKey). ----
    public const string HpBoost1 = "hp_boost_1";
    public const string HpBoost2 = "hp_boost_2";
    public const string HpBoost3 = "hp_boost_3";
    public const string WindWalk = "wind_walk";
    public const string MassWindWalk = "mass_wind_walk";

    // ---- "Sure" floor passive factory: a pure passive carrying one resolution
    //      floor. Auto-granted (SpCost 0); ranked via BuffKey/Replaces so a higher
    //      tier supersedes the lower one. ----
    private static SkillDef FloorPassive(
        string id, string name, BaseClass cls, string buffKey, int rank, string[]? replaces,
        string desc, float eva = 0f, float hit = 0f, float mag = 0f) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        BuffKey: buffKey, Rank: rank, Replaces: replaces,
        Category: SkillCategory.Passive, SpCost: 0,
        Passive: new PassiveEffect(EvadeFloor: eva, HitFloor: hit, MagicFailFloor: mag),
        Description: desc);

    /// <summary>The identity floor passive an archetype receives at its current
    /// class tier (milestones 20/40/76), or null. Granted in EnsureBaseSkills — the
    /// floor VALUES live in the SkillDefs, not in code.</summary>
    public static string? FloorPassiveFor(Archetype? archetype, int level)
    {
        int tier = level >= 76 ? 3 : level >= 40 ? 2 : level >= 20 ? 1 : 0;
        if (tier == 0) return null;
        return archetype switch
        {
            Archetype.Rogue   => tier == 1 ? EvadeMastery1 : tier == 2 ? EvadeMastery2 : EvadeMastery3,
            Archetype.Archer  => tier == 1 ? EvadeMasteryA1 : tier == 2 ? EvadeMasteryA2 : EvadeMasteryA3,
            Archetype.Warrior => tier == 1 ? PrecisionMastery1 : tier == 2 ? PrecisionMastery2 : PrecisionMastery3,
            Archetype.Tank    => tier == 1 ? AntiMagic1 : tier == 2 ? AntiMagic2 : AntiMagic3,
            Archetype.Nuker or Archetype.Healer => tier >= 2 ? SpellWard : null,  // mages from 40
            _ => null
        };
    }

    private static SkillDef[] CommonSkills() => new SkillDef[]
    {
        // ----- Buff-potion buffs (consumed, not cast). Same BuffKey per line so a
        //       rarer potion supersedes a weaker one; rare = bigger + longer. -----
        new(PBuffSpeedC, "Swiftness (Lesser)", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_speed", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 15, ModifierMode.Flat) },
            Category: SkillCategory.Buff, Description: "+15 Move Speed for 60s."),
        new(PBuffSpeedU, "Swiftness", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_speed", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat) },
            Category: SkillCategory.Buff, Description: "+20 Move Speed for 90s."),
        new(PBuffSpeedR, "Swiftness (Greater)", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_speed", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMoveSpeed, 30, ModifierMode.Flat) },
            Category: SkillCategory.Buff, Description: "+30 Move Speed for 180s."),

        new(PBuffCastC, "Focus (Lesser)", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_cast", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.08f) },
            Category: SkillCategory.Buff, Description: "+8% Cast Speed for 60s."),
        new(PBuffCastU, "Focus", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_cast", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.12f) },
            Category: SkillCategory.Buff, Description: "+12% Cast Speed for 90s."),
        new(PBuffCastR, "Focus (Greater)", BaseClass.Mage, SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_cast", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCastSpeed, 0.20f) },
            Category: SkillCategory.Buff, Description: "+20% Cast Speed for 180s."),

        new(PBuffAtkC, "Haste (Lesser)", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "pbuff_atkspeed", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.08f) },
            Category: SkillCategory.Buff, Description: "+8% Attack Speed for 60s."),
        new(PBuffAtkU, "Haste", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "pbuff_atkspeed", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.12f) },
            Category: SkillCategory.Buff, Description: "+12% Attack Speed for 90s."),
        new(PBuffAtkR, "Haste (Greater)", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 1800, BuffKey: "pbuff_atkspeed", Rank: 3,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.20f) },
            Category: SkillCategory.Buff, Description: "+20% Attack Speed for 180s."),

        // ---- Learnable HP Boost line (3 ranks, same BuffKey) ----
        new(HpBoost1, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
            MpCost: 25, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "hp_boost", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.05f) },
            Category: SkillCategory.Buff, SpCost: 1000,
            Description: "Raises Max HP by 5%."),
        new(HpBoost2, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
            MpCost: 35, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "hp_boost", Rank: 2,
            Replaces: new[] { HpBoost1 },
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.15f) },
            Category: SkillCategory.Buff, SpCost: 3000,
            Description: "Raises Max HP by 15%."),
        new(HpBoost3, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
            MpCost: 45, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "hp_boost", Rank: 3,
            Replaces: new[] { HpBoost1, HpBoost2 },
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.35f) },
            Category: SkillCategory.Buff, SpCost: 8000,
            Description: "Raises Max HP by 35%."),

        // ---- Armor-weight masteries (PASSIVE; not cast, not bar-able). The
        //      bonus is class/archetype-specific and applied in RecomputeDerived
        //      only while the matching armor is worn (see ArmorMastery). ----
        new(MasteryHeavy, "Heavy Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Description: "Passive. While wearing HEAVY body armor, gain your class's "
                       + "heavy-armor bonus (more HP/defence). Untrained heavy still penalises."),
        new(MasteryLight, "Light Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Description: "Passive. While wearing LIGHT body armor, gain your class's "
                       + "light-armor bonus (attack speed, evasion/accuracy, etc.)."),
        new(MasteryRobe, "Robe Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Description: "Passive. While wearing a ROBE, gain your class's robe bonus "
                       + "(cast speed, MP and MP regen)."),

        // ===== Combat training passives (auto-granted at level 40) =====
        new(PhysicalTraining, "Physical Training", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 0,
            Passive: new PassiveEffect(AttackPct: 1.0f),
            Description: "Passive. Relentless conditioning: +100% physical attack."),
        new(SpiritTraining, "Spirit Training", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 0,
            Passive: new PassiveEffect(AttackPct: 1.0f, CastSpeedPct: 0.40f),
            Description: "Passive. Honed focus: +100% magic attack (≈×1.414 spell "
                       + "damage via the √M.Atk curve) and +40% casting speed."),

        // ===== Class identity "sure" floor passives (auto-granted at 20/40/76) =====
        // Rogue — guaranteed physical evasion.
        FloorPassive(EvadeMastery1, "Evasion Mastery", BaseClass.Fighter, "evade_floor", 1, null,
            "Passive. Always at least a 10% chance to dodge physical attacks.", eva: 0.10f),
        FloorPassive(EvadeMastery2, "Evasion Mastery", BaseClass.Fighter, "evade_floor", 2, new[] { EvadeMastery1 },
            "Passive. Always at least a 20% chance to dodge physical attacks.", eva: 0.20f),
        FloorPassive(EvadeMastery3, "Evasion Mastery", BaseClass.Fighter, "evade_floor", 3, new[] { EvadeMastery1, EvadeMastery2 },
            "Passive. Always at least a 30% chance to dodge physical attacks.", eva: 0.30f),
        // Archer — half the rogue's evasion floor.
        FloorPassive(EvadeMasteryA1, "Reflexes", BaseClass.Fighter, "evade_floor", 1, null,
            "Passive. Always at least a 5% chance to dodge physical attacks.", eva: 0.05f),
        FloorPassive(EvadeMasteryA2, "Reflexes", BaseClass.Fighter, "evade_floor", 2, new[] { EvadeMasteryA1 },
            "Passive. Always at least a 10% chance to dodge physical attacks.", eva: 0.10f),
        FloorPassive(EvadeMasteryA3, "Reflexes", BaseClass.Fighter, "evade_floor", 3, new[] { EvadeMasteryA1, EvadeMasteryA2 },
            "Passive. Always at least a 15% chance to dodge physical attacks.", eva: 0.15f),
        // Warrior — guaranteed to land a share of physical hits (caps a target's evasion).
        FloorPassive(PrecisionMastery1, "Precision", BaseClass.Fighter, "hit_floor", 1, null,
            "Passive. Your physical attacks always land at least 10% of the time.", hit: 0.10f),
        FloorPassive(PrecisionMastery2, "Precision", BaseClass.Fighter, "hit_floor", 2, new[] { PrecisionMastery1 },
            "Passive. Your physical attacks always land at least 20% of the time.", hit: 0.20f),
        FloorPassive(PrecisionMastery3, "Precision", BaseClass.Fighter, "hit_floor", 3, new[] { PrecisionMastery1, PrecisionMastery2 },
            "Passive. Your physical attacks always land at least 30% of the time.", hit: 0.30f),
        // Tank — Anti-Magic: spells always have a real chance to fizzle on you.
        FloorPassive(AntiMagic1, "Anti-Magic", BaseClass.Fighter, "anti_magic", 1, null,
            "Passive. Spells fizzle on you at least 10% of the time.", mag: 0.10f),
        FloorPassive(AntiMagic2, "Anti-Magic", BaseClass.Fighter, "anti_magic", 2, new[] { AntiMagic1 },
            "Passive. Spells fizzle on you at least 15% of the time.", mag: 0.15f),
        FloorPassive(AntiMagic3, "Anti-Magic", BaseClass.Fighter, "anti_magic", 3, new[] { AntiMagic1, AntiMagic2 },
            "Passive. Spells fizzle on you at least 20% of the time.", mag: 0.20f),
        // Mage — self-hardening against hostile magic (from 40).
        FloorPassive(SpellWard, "Spell Ward", BaseClass.Mage, "anti_magic", 1, null,
            "Passive. Spells fizzle on you at least 10% of the time.", mag: 0.10f),

        // ---- Wind Walk (move-speed self buff, learnable) ----
        new(WindWalk, "Wind Walk", BaseClass.Mage,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 30, CastTicks: 10, CooldownTicks: 10, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "wind_walk", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                new(SkillEffect.BuffEvasion, 5, ModifierMode.Flat),
            },
            Category: SkillCategory.Buff, SpCost: 1500, TargetMode: TargetMode.SelfOnly,
            Description: "Move +33 and Evasion +5 for 20 minutes (self)."),

        // Party version: same effect + same BuffKey, but buffs nearby allies.
        new(MassWindWalk, "Mass Wind Walk", BaseClass.Mage,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 120, CastTicks: 15, CooldownTicks: 50, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "wind_walk", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                new(SkillEffect.BuffEvasion, 5, ModifierMode.Flat),
            },
            Category: SkillCategory.Buff, SpCost: 5000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Description: "Move +33 and Evasion +5 to nearby allies for 20 minutes."),
    };
}
