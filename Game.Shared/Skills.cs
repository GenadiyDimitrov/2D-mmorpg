namespace Game.Shared;

/// <summary>
/// A skill definition. Id is now a STABLE STRING KEY (e.g. "magic_bolt",
/// "greater_heal"). Timings are in server ticks (10/s). Effect is a [Flags]
/// value; per-effect magnitudes live in Magnitudes with flat/percent modes.
/// Buff identity/stacking: BuffKey + Rank + Replaces (see ApplyBuff).
/// </summary>
public record SkillDef(
    string Id,
    string Name,
    BaseClass Class,
    SkillEffect Effect,
    int MpCost,
    int CastTicks,
    int CooldownTicks,
    float Range,
    int Power,
    int DurationTicks = 0,
    EffectMagnitude[]? Magnitudes = null,
    string BuffKey = "",
    int Rank = 0,
    string[]? Replaces = null,
    string Description = "",
    int SpCost = 1,
    SkillCategory Category = SkillCategory.Physical,
    TargetMode TargetMode = TargetMode.SelfOrTarget,
    float AreaRadius = 0f,
    int InterruptDefense = 0,
    int InterruptPower = 0,
    int InitialMpCost = -1,
    float BlockAccuracy = 0f,
    bool SureHit = false,
    PassiveEffect? Passive = null,
    string Abbrev = "")
{
    public float MagnitudeOf(SkillEffect effect, ModifierMode mode)
    {
        if (Magnitudes is null) return 0f;
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == effect && m.Mode == mode) sum += m.Value;
        return sum;
    }

    /// <summary>MP charged when the cast STARTS. Default (-1) = 0 up front,
    /// full cost on completion (so existing skills are unchanged). Set
    /// InitialMpCost to split (e.g. 34 of 50 up front, 16 on finish).</summary>
    public int InitialMp => InitialMpCost < 0 ? 0 : Math.Min(InitialMpCost, MpCost);

    /// <summary>MP charged when the cast COMPLETES (the remainder).</summary>
    public int FinishMp => MpCost - InitialMp;
}

/// <summary>Skill window grouping. Passive = a learned, always-on effect (armor
/// masteries, discipline passives) — never cast and never placed on the action bar.</summary>
public enum SkillCategory { Physical = 0, Magic = 1, Buff = 2, Debuff = 3, Heal = 4, Passive = 5 }

/// <summary>
/// An always-on bonus carried by a learnable PASSIVE skill (discipline passives).
/// Applied in Entity.RecomputeDerived for every learned skill whose SkillDef sets
/// one. ALL fields default to 0 = no effect (so `default`/`new()` is safely inert —
/// unlike MasteryEffect's speed factors). Pct fields are fractions (0.10 = +10%);
/// speed Pct fields make you FASTER (0.10 = 10% faster). Flat ints add directly.
/// </summary>
public readonly record struct PassiveEffect(
    float MaxHpPct = 0f, float MaxMpPct = 0f,
    int Defence = 0, int MagicDefence = 0,
    int Attack = 0, float AttackPct = 0f,
    int Evasion = 0, int Accuracy = 0,
    float CritRate = 0f, float CritDamage = 0f, float MagicCritRate = 0f,
    float HpRegen = 0f, float MpRegen = 0f,
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f, float MoveSpeedPct = 0f,
    // Combat-resolution "sure" floors (see docs/CombatResolution.md). These are
    // GUARANTEES (the resolver takes the MAX across passives, not a sum):
    float EvadeFloor = 0f,        // min chance to dodge physical (rogue/archer)
    float HitFloor = 0f,          // min chance THIS entity lands a physical hit (warrior)
    float MagicFailFloor = 0f);   // min chance a spell fizzles vs this entity (tank/mage anti-magic)

/// <summary>Who a (beneficial) skill affects. SelfOnly = caster only;
/// AlliesInRadius = caster + nearby player characters (a "party" buff until real
/// party groups exist).</summary>
public enum TargetMode { SelfOrTarget = 0, SelfOnly = 1, AlliesInRadius = 2 }

// ===========================================================================
//  SKILL CATALOG — every skill, keyed by string id.
//
//  WHERE TO EDIT:
//   - Add a skill's *definition* (numbers, text) here in SkillCatalog.All.
//   - Decide *who learns it and at what level* in the per-class files under
//     RaceAndClasses/ (e.g. Classes.Human.Mage.cs), via ClassSkills.Register.
// ===========================================================================
public static class SkillCatalog
{
    // ---- Stable string ids -------------------------------------------------
    public const string PowerStrike = "power_strike";
    public const string WarCry = "war_cry";
    public const string GreaterWarCry = "greater_war_cry";
    public const string BattleFury = "battle_fury";
    public const string MagicBolt = "magic_bolt";
    public const string Heal = "heal";
    public const string Weakness = "weakness";
    public const string Fortify = "fortify";
    public const string ShieldMastery = "shield_mastery";
    public const string MightyBlow = "mighty_blow";
    public const string TwinSlash = "twin_slash";
    public const string PowerShot = "power_shot";
    public const string GreaterHeal = "greater_heal";
    public const string FlameBolt = "flame_bolt";
    public const string HolyStrike = "holy_strike";
    public const string GreaterWeakness = "greater_weakness";
    public const string Disrupt = "disrupt";
    // Buff-potion buffs (not learnable; applied by consuming a buff potion). Rarity =
    // rank within a line, so a rarer potion supersedes a weaker one of the same line.
    public const string PBuffSpeedC = "pbuff_speed_c";
    public const string PBuffSpeedU = "pbuff_speed_u";
    public const string PBuffSpeedR = "pbuff_speed_r";
    public const string PBuffCastC = "pbuff_cast_c";
    public const string PBuffCastU = "pbuff_cast_u";
    public const string PBuffCastR = "pbuff_cast_r";
    public const string PBuffAtkC = "pbuff_atk_c";
    public const string PBuffAtkU = "pbuff_atk_u";
    public const string PBuffAtkR = "pbuff_atk_r";
    // Example learnable buff line (HP boost) used by healers/clerics+.
    public const string HpBoost1 = "hp_boost_1";
    public const string HpBoost2 = "hp_boost_2";
    public const string HpBoost3 = "hp_boost_3";
    public const string WindWalk = "wind_walk";
    public const string MassWindWalk = "mass_wind_walk";
    // ---- Lightbringer (3rd-class Healer discipline), Phase 24.1 ----
    public const string LbHumanMend = "lb_human_mend";     // strong fast single heal
    public const string LbHumanPurify = "lb_human_purify"; // cleanse an ally
    public const string LbElfDawn = "lb_elf_dawn";         // AoE heal + cleanse
    public const string LbElfWarden = "lb_elf_warden";     // root enemy + self de-taunt
    public const string LbOrkFont = "lb_ork_font";         // AoE heal (totem stand-in)
    public const string LbOrkSap = "lb_ork_sap";           // anti-heal debuff
    // ---- Armor-weight masteries (learnable PASSIVES). Effect is applied by
    //      ArmorMastery in RecomputeDerived when the matching weight is worn AND
    //      the passive is learned; the SkillDef just makes it visible/learnable. ----
    public const string MasteryHeavy = "mastery_heavy";
    public const string MasteryLight = "mastery_light";
    public const string MasteryRobe  = "mastery_robe";
    // ---- Combat "training" passives, auto-granted at level 40 (the standing-in for
    //      soulshot/spiritshot until a real shot system exists). Doubling the atk STAT
    //      gives ×2 physical (linear) but ×1.414 magic (√mAtk) — the soulshot/spiritshot
    //      ratio. Later these can be split into ranks across levels. ----
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
    // ---- Lightbringer (Healer A) — buff + passive to fill the 4-skill template ----
    public const string LbBlessing = "lb_blessing";
    public const string LbDevotion = "lb_devotion";   // passive
    // ---- Warchanter (Healer B) — buffer: per-race kits (DMG + mega-buff + HoT + passive) ----
    public const string WcHumanBolt = "wc_human_bolt";
    public const string WcHumanChant = "wc_human_chant";
    public const string WcHumanRenew = "wc_human_renew";
    public const string WcHumanPass = "wc_human_pass";
    public const string WcElfBolt = "wc_elf_bolt";
    public const string WcElfChant = "wc_elf_chant";
    public const string WcElfRenew = "wc_elf_renew";
    public const string WcElfPass = "wc_elf_pass";
    public const string WcOrkBolt = "wc_ork_bolt";
    public const string WcOrkChant = "wc_ork_chant";
    public const string WcOrkRenew = "wc_ork_renew";
    public const string WcOrkPass = "wc_ork_pass";

    private static readonly Dictionary<string, SkillDef> All = BuildCatalog();

    // ---- Warchanter kit factories (same numbers per race; names differ) ----
    private static SkillDef WcChant(string id, string name) => new(
        id, name, BaseClass.Mage,
        SkillEffect.BuffAtk | SkillEffect.BuffDef | SkillEffect.BuffMagicDef
        | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
        | SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen,
        MpCost: 60, CastTicks: 20, CooldownTicks: 50, Range: 0, Power: 0,
        DurationTicks: 12000, BuffKey: "wc_chant", Rank: 1,
        Magnitudes: new EffectMagnitude[]
        {
            new(SkillEffect.BuffAtk, 0.15f), new(SkillEffect.BuffDef, 0.15f),
            new(SkillEffect.BuffMagicDef, 0.30f),
            new(SkillEffect.BuffCastSpeed, 0.30f), new(SkillEffect.BuffAtkSpeed, 0.30f),
            new(SkillEffect.BuffMoveSpeed, 45f, ModifierMode.Flat),
            new(SkillEffect.BuffHp, 0.35f), new(SkillEffect.BuffMp, 0.35f),
            new(SkillEffect.BuffHpRegen, 0.20f), new(SkillEffect.BuffMpRegen, 0.20f),
        },
        Category: SkillCategory.Buff, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
        SpCost: 500,
        Description: "Party: +35% max HP/MP, +30% magic def & cast/attack speed, +15% atk/def, +move & regen.");

    private static SkillDef WcRenew(string id, string name) => new(
        id, name, BaseClass.Mage, SkillEffect.Heal | SkillEffect.HealOverTime,
        MpCost: 70, CastTicks: 20, CooldownTicks: 300, Range: 0, Power: 150,
        DurationTicks: 100, BuffKey: "wc_renew", Rank: 1,
        Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 0.02f) },
        Category: SkillCategory.Heal, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
        SpCost: 500,
        Description: "Party: an instant heal plus 2% max HP per second for 10s.");

    private static SkillDef WcBolt(string id, string name) => new(
        id, name, BaseClass.Mage, SkillEffect.MagicDamage,
        // A proper single-target nuke: ~4s base cast (WIT/gear/buffs shorten it), real power.
        MpCost: 50, CastTicks: 40, CooldownTicks: 20, Range: 750, Power: 120,
        Replaces: new[] { MagicBolt, HolyStrike },   // 3rd-class nuke replaces the lower ones
        Category: SkillCategory.Magic, SpCost: 500,
        Description: "A heavy single-target nuke (replaces Magic Bolt / Holy Strike).");

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

    private static Dictionary<string, SkillDef> BuildCatalog()
    {
        var list = new List<SkillDef>
        {
            new(PowerStrike, "Power Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 10, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 30,
                Category: SkillCategory.Physical,
                Description: "A forceful melee blow. Bonus accuracy, but can still miss."),

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

            // The dedicated interrupt: INSTANT (CastTicks 0), tiny damage, but
            // overwhelming InterruptPower so it ALWAYS breaks an enemy cast.
            new(Disrupt, "Disrupt", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 10, CastTicks: 0, CooldownTicks: 80, Range: 0, Power: 5,
                Category: SkillCategory.Physical, InterruptPower: 99999, SureHit: true,
                Description: "Instant strike that never misses and always interrupts an enemy's cast."),

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

            new(PowerShot, "Power Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 16, CastTicks: 8, CooldownTicks: 40, Range: 900, Power: 70,
                Category: SkillCategory.Physical,
                Description: "A long-range aimed shot dealing heavy damage."),

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

            // ===== Lightbringer (Healer A) — buff + passive to complete its 4-skill kit =====
            new(LbBlessing, "Blessing of Light", BaseClass.Mage,
                SkillEffect.BuffHp | SkillEffect.BuffDef,
                MpCost: 50, CastTicks: 20, CooldownTicks: 30, Range: 0, Power: 0,
                DurationTicks: 12000, BuffKey: "lb_blessing", Rank: 1,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffHp, 0.15f), new(SkillEffect.BuffDef, 0.15f),
                },
                Category: SkillCategory.Buff, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
                SpCost: 500, Description: "Party: +15% max HP and +15% defence."),
            new(LbDevotion, "Devotion", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: 500,
                Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicDefence: 10),
                Description: "Passive. +10% max MP, +MP regen, +10 magic defence."),

            // ===== Warchanter (Healer B) — buffer: per-race kits =====
            // Mega party buff (same magnitudes all races; names differ per race).
            WcChant(WcHumanChant, "Grand Anthem"),
            WcChant(WcElfChant, "Sylvan Anthem"),
            WcChant(WcOrkChant, "War Anthem"),
            // Party heal + heal-over-time.
            WcRenew(WcHumanRenew, "Renewing Verse"),
            WcRenew(WcElfRenew, "Dawn Verse"),
            WcRenew(WcOrkRenew, "Spirit Verse"),
            // Single-target magic nuke (the buffer's only direct damage).
            WcBolt(WcHumanBolt, "Arcane Lance"),
            WcBolt(WcElfBolt, "Starlight Lance"),
            WcBolt(WcOrkBolt, "Spirit Lance"),
            // Passives (per-race lean; v1 is a flat caster set — robe/light conditional comes in P1).
            new(WcHumanPass, "Resonance", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: 500,
                Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicCritRate: 0.05f),
                Description: "Passive. +10% max MP, +MP regen, +5% magic crit."),
            new(WcElfPass, "Harmony", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: 500,
                Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, CastSpeedPct: 0.08f),
                Description: "Passive. +10% max MP, +MP regen, +8% cast speed."),
            new(WcOrkPass, "Totemic Bond", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: 500,
                Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, AttackPct: 0.08f),
                Description: "Passive. +10% max MP, +MP regen, +8% attack (feeds spells)."),

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

            // ================================================================
            //  LIGHTBRINGER — 3rd-class Healer discipline (lvl 40). Same idea
            //  (keep the party alive), expressed three ways by race.
            // ================================================================

            // --- Human: strong, fast single-target heal + cleanse ---
            new(LbHumanMend, "Mending Light", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 42, CastTicks: 22, CooldownTicks: 12, Range: 500, Power: 230,
                Category: SkillCategory.Heal, SpCost: 6000,
                Description: "A swift, powerful heal on a single ally. The Human " +
                             "Lightbringer's hallmark: more single-target throughput, less AoE."),
            new(LbHumanPurify, "Purify", BaseClass.Mage, SkillEffect.Cleanse,
                MpCost: 24, CastTicks: 8, CooldownTicks: 50, Range: 500, Power: 0,
                Category: SkillCategory.Heal, SpCost: 6000,
                Description: "Removes harmful effects (curses, anti-heal, roots) from an ally."),

            // --- Elf: AoE heal + cleanse, and a control/utility tool ---
            new(LbElfDawn, "Dawn Bloom", BaseClass.Mage, SkillEffect.Heal | SkillEffect.Cleanse,
                MpCost: 60, CastTicks: 30, CooldownTicks: 40, Range: 0, Power: 120,
                Category: SkillCategory.Heal, SpCost: 6000,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
                Description: "Heals AND cleanses all nearby allies. The Elf Lightbringer " +
                             "trades single-target power for area coverage."),
            new(LbElfWarden, "Warding Step", BaseClass.Mage, SkillEffect.Root | SkillEffect.Detaunt,
                MpCost: 30, CastTicks: 6, CooldownTicks: 120, Range: 500, Power: 0,
                DurationTicks: 80, BuffKey: "root", Rank: 1,
                Category: SkillCategory.Debuff, SpCost: 6000,
                Description: "Holds an enemy in place for 8s and sheds the caster's aggro " +
                             "from nearby foes (they look elsewhere)."),

            // --- Ork: AoE heal (totem stand-in for now) + anti-heal debuff ---
            new(LbOrkFont, "Spirit Font", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 55, CastTicks: 28, CooldownTicks: 40, Range: 0, Power: 110,
                Category: SkillCategory.Heal, SpCost: 6000,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
                Description: "Calls a font of spirit energy that heals nearby allies. " +
                             "(A placed totem will replace this when summons arrive.)"),
            new(LbOrkSap, "Soul Sap", BaseClass.Mage, SkillEffect.DebuffHealRecv,
                MpCost: 28, CastTicks: 8, CooldownTicks: 150, Range: 500, Power: 0,
                DurationTicks: 150, BuffKey: "antiheal", Rank: 1,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.50f) },
                Category: SkillCategory.Debuff, SpCost: 6000,
                Description: "Curses an enemy so it recovers only half the HP from any " +
                             "healing for 15s."),
        };

        var dict = new Dictionary<string, SkillDef>();
        foreach (var sk in list)
            if (!dict.TryAdd(sk.Id, sk))
                throw new InvalidOperationException($"Duplicate skill id '{sk.Id}'.");
        return dict;
    }

    public static SkillDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);
    public static string DescriptionOf(string id) => Get(id)?.Description ?? "";
    public static IEnumerable<SkillDef> AllSkills => All.Values;
}

/// <summary>Combat math + range/cast helpers.</summary>
public static class SkillMath
{
    /// <summary>Class-change tier from level: 1 (1-20), 2 (21-40), 3 (40+).
    /// Skill ranges scale with tier so reach grows as you advance.</summary>
    public static int RangeTier(int level) =>
        level >= 41 ? 3 : level >= 21 ? 2 : 1;

    /// <summary>Effective range of a skill for a given caster. The skill's Range
    /// field is the tier-1 base; magic and bow skills step up by tier:
    ///   magic: 500 / 750 / 900   bow: 350 / 600 / 900
    /// Non-tiered skills (melee, range 0) just use their own value / basic range.</summary>
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange, int level)
    {
        if (def.Range <= 0)
            return basicAttackRange;

        int tier = RangeTier(level);

        bool isSpell = def.Effect.HasFlag(SkillEffect.MagicDamage)
            || def.Effect.HasFlag(SkillEffect.DebuffDef)
            || def.Effect.HasFlag(SkillEffect.Heal);

        // Bow skills: archer ranged physical attacks scale 350/600/900.
        bool isBowSkill = def.Effect.HasFlag(SkillEffect.PhysicalDamage)
            && archetype is Archetype.Archer && def.Range >= 300;

        if (isSpell)
            return tier switch { 3 => 900f, 2 => 750f, _ => 500f };
        if (isBowSkill)
            return tier switch { 3 => 900f, 2 => 600f, _ => 350f };

        return def.Range;
    }

    // Backwards-compatible overload (assumes tier 1) for any caller without level.
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange) =>
        EffectiveRange(def, archetype, basicAttackRange, 1);

    public const int CastBaselineWit = 25;
    public const float CastPercentPerWit = 0.012f;

    public static float CastModifier(int wit) =>
        Math.Clamp((CastBaselineWit - wit) * CastPercentPerWit, -0.50f, 0.50f);

    public static int AdjustedCastTicks(int baseTicks, int wit) =>
        Math.Max(2, (int)MathF.Round(baseTicks * (1f + CastModifier(wit))));

    public static float SpellFailChance(int casterLevel, int targetLevel) =>
        Math.Clamp(0.03f + (targetLevel - casterLevel) * 0.02f, 0.01f, 0.80f);

    public static int PhysicalSkillDamage(int power, float effAttack, float effDefence) =>
        Math.Max(1, power + (int)(effAttack * 2 - effDefence));

    public static int MagicSkillDamage(int power, float effAttack, int wit, float effDefence) =>
        Math.Max(1, power + (int)(effAttack + wit * 2 - effDefence / 2));

    public static int HealAmount(int power, int wit) => power + wit * 2;

    public const float CritMultiplierSkills = 2.0f;
    public const int PhysicalSkillAccuracyBonus = 10;
}
