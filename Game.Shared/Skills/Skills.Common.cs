namespace Game.Shared;

/// <summary>Cross-class skills shared by many classes / not tied to one discipline:
/// armor-weight masteries, the combat-training passives, the class-identity "sure"
/// floor passives (and their auto-grant mapping), buff-potion buffs, the HP Boost
/// line, and Wind Walk. Class-defining combat skills live in their own files.</summary>
public static partial class SkillCatalog
{
    // ---- Base MAGE armor mastery (learnable PASSIVE, per-level StatMods data; applied
    //      in RecomputeDerived by the worn body weight). Fighters get FighterArmorMastery
    //      instead; 2nd classes REPLACE these with their archetype mastery. ----
    public const string MasteryRobe  = "mastery_robe";
    // ---- Combat "training" passives, auto-granted at level 40 (soulshot/spiritshot
    //      stand-in). Doubling the atk STAT gives ×2 physical (linear) but ×1.414
    //      magic (√mAtk) — the soulshot/spiritshot ratio. ----
    public const string PhysicalTraining = "physical_training";  // multi-level (9): +10%…+100% atk
    public const string SpiritTraining   = "spirit_training";    // multi-level (9): +atk + cast speed
    // ---- Class identity "sure" floor passives — now ONE multi-level skill each
    //      (auto-granted at the class-change milestone, level = tier 1/2/3). The floor
    //      VALUES live in the SkillDef Levels, not in code. See FloorPassiveFor. ----
    public const string EvadeMastery = "evade_mastery"; // Rogue   10/20/30%
    public const string Reflexes     = "reflexes";      // Archer    5/10/15%
    public const string Precision    = "precision";     // Warrior 10/20/30% hit floor
    public const string AntiMagic    = "anti_magic";    // Tank    10/15/20% magic fizzle
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
    // ---- Learnable HP Boost — ONE multi-level skill (3 levels: +5/+15/+35%). ----
    public const string HpBoost = "hp_boost";
    public const string WindWalk = "wind_walk";
    public const string MassWindWalk = "mass_wind_walk";
    // ---- "Class Balance" — one zeroed passive per class, auto-granted. See ClassBalanceFor. ----
    public const string BalanceTank    = "class_balance_tank";
    public const string BalanceWarrior = "class_balance_warrior";
    public const string BalanceRogue   = "class_balance_rogue";
    public const string BalanceArcher  = "class_balance_archer";
    public const string BalanceNuker   = "class_balance_nuker";
    public const string BalanceHealer  = "class_balance_healer";
    public const string BalanceFighter = "class_balance_fighter";   // base class, pre-2nd
    public const string BalanceMage    = "class_balance_mage";      // base class, pre-2nd
    // ---- Universal "Return" line: teleport to the nearest town. All auto-granted; the scroll
    //      variants require (and consume) their scroll item. Fixed cast + fixed cooldown. ----
    public const string ReturnSkill          = "return_town";        // 30s cast, 5min cd, fragile
    public const string ScrollReturnSkill    = "use_scroll_return";  // 10s cast, needs a scroll
    public const string ScrollReturnUltSkill = "use_scroll_return_ult"; // ~0.4s, needs an ult scroll

    // ---- Multi-level PASSIVE factory: a pure passive whose levels each carry a
    //      PassiveEffect (the floor/lean value for that level). ----
    private static SkillDef LeveledPassive(string id, string name, BaseClass cls, string desc,
        params PassiveEffect[] perLevel) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0, Description: desc,
        Levels: perLevel.Select(p => new SkillLevel(Passive: p)).ToArray());

    // Combat-training passive: 9 levels, +10%…+80% then +100% attack (the soulshot/
    // spiritshot stand-in). At max level the +100% atk = ×2 P.Atk/M.Atk, which is exactly
    // what a shot does: ×2 physical damage (linear), ×1.414 magic (√mAtk).
    // castSpeedFlat mirrors the real spiritshot bonus: a FLAT +40 to the cast stat. It used
    // to be a 0.40 PERCENT, applied as a time cut (×0.6 time = +67% speed), which compounded
    // with WIT/gear/buffs and inflated a buffed L40 mage to ~2200 against the 1999 cap.
    private static SkillDef TrainingPassive(string id, string name, BaseClass cls, float castSpeedFlat, string desc)
    {
        float[] atk = { 0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 1.00f };
        return new(id, name, cls, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 0, Description: desc,
            Levels: atk.Select(p => new SkillLevel(
                Passive: new PassiveEffect(AttackPct: p, CastSpeedFlat: castSpeedFlat))).ToArray());
    }

    /// <summary>The "Class Balance" passive for a class — auto-granted, always level 1.
    /// It does NOTHING today (an all-zero PassiveEffect). It exists as the tuning HOOK that
    /// replaced the hardcoded per-archetype basic-attack multiplier: if a tank later turns
    /// out to hit too softly in PvE, raise PveBasicDamagePct on its balance passive rather
    /// than adding a coefficient back into StatCalculator. Damage stays formula + weapon,
    /// and per-class deviation stays DATA (see docs — stats-via-skills).</summary>
    public static string ClassBalanceFor(Archetype? archetype, BaseClass cls) => archetype switch
    {
        Archetype.Tank    => BalanceTank,
        Archetype.Warrior => BalanceWarrior,
        Archetype.Rogue   => BalanceRogue,
        Archetype.Archer  => BalanceArcher,
        Archetype.Nuker   => BalanceNuker,
        Archetype.Healer  => BalanceHealer,
        _ => cls == BaseClass.Mage ? BalanceMage : BalanceFighter,   // base class, pre-2nd
    };

    /// <summary>A Class Balance passive: one level, an all-zero <see cref="PassiveEffect"/>.
    /// Fill in fields here (PveBasicDamagePct, PvpMagicDamagePct, AttackPct, …) to tune a class.</summary>
    private static SkillDef BalancePassive(string id, string name, BaseClass cls) => new(
        id, name, cls, SkillEffect.None,
        MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
        Category: SkillCategory.Passive, SpCost: 0,
        Description: "Class balance. Reserved for tuning — currently no effect.",
        Levels: new[] { new SkillLevel(Passive: new PassiveEffect()) });

    /// <summary>The identity floor passive an archetype receives at its current class
    /// tier (milestones 20/40/76) — as (skill id, skill LEVEL), or null. Granted in
    /// AutoLearnCoreSkills. The floor VALUES live in the SkillDef Levels, not in code.</summary>
    public static (string Id, int Level)? FloorPassiveFor(Archetype? archetype, int level)
    {
        int tier = level >= 76 ? 3 : level >= 40 ? 2 : level >= 20 ? 1 : 0;
        if (tier == 0) return null;
        return archetype switch
        {
            Archetype.Rogue   => (EvadeMastery, tier),
            Archetype.Archer  => (Reflexes, tier),
            Archetype.Warrior => (Precision, tier),
            Archetype.Tank    => (AntiMagic, tier),
            // Mages get NO auto magic-fail floor — it comes from their LEARNED Anti-Magic
            // (anti_magic_mage), available to every mage class.
            _ => null
        };
    }

    // Base MAGE robe mastery per level (char 1/7/14, mage CSV). ROBE: +20% MP regen and P.Def
    // (no cast change). Any non-robe body — light/heavy AND no armor at all — penalises: attack
    // speed ×0.8 and cast speed ×0.5. Penalty literals are inlined (not shared fields) to avoid
    // static-init ordering across partials. The nuker/healer masteries REPLACE this from level 20.
    private static readonly ArmorMasteryProfile[] MageRobeLevels = new[]
    {
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 0),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 7),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new ArmorMasteryProfile(
            Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 9),
            Light: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
    };

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

        // ---- Learnable HP Boost — ONE skill, 3 levels (+5 / +15 / +35% Max HP) ----
        new(HpBoost, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
            MpCost: 25, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "hp_boost", Rank: 1,
            Category: SkillCategory.Buff,
            Description: "Raises your Max HP for a time.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, SpCost: 1000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.05f) },
                    Description: "Raises Max HP by 5%."),
                new SkillLevel(MpCost: 35, SpCost: 3000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.15f) },
                    Description: "Raises Max HP by 15%."),
                new SkillLevel(MpCost: 45, SpCost: 8000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.35f) },
                    Description: "Raises Max HP by 35%."),
            }),

        // ---- Robe Mastery — base MAGE armor mastery (PASSIVE, per-level StatMods data).
        //      Levels 1/2/3 at char 1/7/14; the nuker/healer 2nd-class masteries REPLACE it.
        //      Robe = caster lean; light/heavy hinder casting. Applied in RecomputeDerived
        //      by the worn body weight. Numbers are placeholders (carried over from the old
        //      formula) pending the real mage table. ----
        new(MasteryRobe, "Robe Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. While wearing a ROBE: faster casting, more MP and MP regen, "
                       + "and defence (rising with level). Light/heavy armor slows your casting.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 0),
                new SkillLevel(SpCost: 480,  Description: "Robe Mastery Lv.2 (+P.Def)."),
                new SkillLevel(SpCost: 2200, Description: "Robe Mastery Lv.3 (+P.Def)."),
            },
            ArmorMasteryLevels: MageRobeLevels),

        // ===== Class Balance — the per-class tuning hook (auto-granted, currently no-ops) =====
        BalancePassive(BalanceTank,    "Class Balance (Tank)",    BaseClass.Fighter),
        BalancePassive(BalanceWarrior, "Class Balance (Warrior)", BaseClass.Fighter),
        BalancePassive(BalanceRogue,   "Class Balance (Rogue)",   BaseClass.Fighter),
        BalancePassive(BalanceArcher,  "Class Balance (Archer)",  BaseClass.Fighter),
        BalancePassive(BalanceFighter, "Class Balance",           BaseClass.Fighter),
        BalancePassive(BalanceNuker,   "Class Balance (Nuker)",   BaseClass.Mage),
        BalancePassive(BalanceHealer,  "Class Balance (Healer)",  BaseClass.Mage),
        BalancePassive(BalanceMage,    "Class Balance",           BaseClass.Mage),

        // ===== Combat training passives (auto-granted; level by character level) =====
        // 9 levels: +10%…+80% attack (40→75) then +100% (76+). The auto-grant level
        // comes from StatCalculator.TrainingLevelFor.
        TrainingPassive(PhysicalTraining, "Physical Training", BaseClass.Fighter, 0f,
            "Passive. Relentless conditioning — physical attack grows with level (+10% to +100%)."),
        TrainingPassive(SpiritTraining, "Spirit Training", BaseClass.Mage, 40f,
            "Passive. Honed focus — +40 casting speed and magic attack growing with level (+10% to +100%)."),

        // ===== Class identity "sure" floor passives (auto-granted at 20/40/76 = lvl 1/2/3) =====
        // Rogue identity now DATA: the evade floor + the archetype crit/evasion LEANS (+20% crit,
        // +20 eva) migrated here from StatCalculator's hardcoded Archetype switches (stats-via-skills).
        LeveledPassive(EvadeMastery, "Evasion Mastery", BaseClass.Fighter,
            "Passive. Dodge floor 10/20/30%, +20% crit chance, +20 evasion.",
            new PassiveEffect(EvadeFloor: 0.10f, CritRate: 0.20f, Evasion: 20),
            new PassiveEffect(EvadeFloor: 0.20f, CritRate: 0.20f, Evasion: 20),
            new PassiveEffect(EvadeFloor: 0.30f, CritRate: 0.20f, Evasion: 20)),
        // Archer identity now DATA: evade floor + +15% crit / +10 eva leans.
        LeveledPassive(Reflexes, "Reflexes", BaseClass.Fighter,
            "Passive. Dodge floor 5/10/15%, +15% crit chance, +10 evasion.",
            new PassiveEffect(EvadeFloor: 0.05f, CritRate: 0.15f, Evasion: 10),
            new PassiveEffect(EvadeFloor: 0.10f, CritRate: 0.15f, Evasion: 10),
            new PassiveEffect(EvadeFloor: 0.15f, CritRate: 0.15f, Evasion: 10)),
        LeveledPassive(Precision, "Precision", BaseClass.Fighter,
            "Passive. Your physical attacks always land at least 10/20/30% of the time.",
            new PassiveEffect(HitFloor: 0.10f), new PassiveEffect(HitFloor: 0.20f), new PassiveEffect(HitFloor: 0.30f)),
        LeveledPassive(AntiMagic, "Anti-Magic", BaseClass.Fighter,
            "Passive. Spells fizzle on you at least 10/15/20% of the time.",
            new PassiveEffect(MagicFailFloor: 0.10f), new PassiveEffect(MagicFailFloor: 0.15f), new PassiveEffect(MagicFailFloor: 0.20f)),

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

        // ---- Return line (universal escape / recall to the nearest town) ----
        // The FREE fallback: a long 30s channel that ANY damage cancels (FragileCast), 5-min reuse.
        // Fixed cast + fixed cooldown = no haste/CD buffs speed it up. For when you forgot scrolls.
        new(ReturnSkill, "Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 300, CooldownTicks: 3000, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, FragileCast: true, TeleportsToTown: true,
            Description: "Channel 30s to return to the nearest town. ANY damage cancels it. 5 min reuse."),

        // Bought scroll: 10s fixed cast, 10s reuse. Consumes one Scroll of Return.
        new(ScrollReturnSkill, "Scroll of Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 100, CooldownTicks: 100, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, TeleportsToTown: true,
            ConsumableId: ItemCatalog.ScrollReturn, ConsumableAmount: 1,
            Description: "Use a Scroll of Return: 10s cast, teleport to the nearest town."),

        // Ultimate scroll: ~0.4s (near-instant) fixed cast, 1s reuse. Consumes one Ultimate scroll.
        new(ScrollReturnUltSkill, "Ultimate Scroll of Return", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 4, CooldownTicks: 10, Range: 0, Power: 0,
            Category: SkillCategory.Magic, SpCost: 0, TargetMode: TargetMode.SelfOnly,
            FixedCast: true, FixedCooldown: true, TeleportsToTown: true,
            ConsumableId: ItemCatalog.ScrollReturnUltimate, ConsumableAmount: 1,
            Description: "Use an Ultimate Scroll of Return: near-instant return to the nearest town."),
    };
}
