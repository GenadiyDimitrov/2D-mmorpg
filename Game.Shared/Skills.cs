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
    float AreaRadius = 0f)
{
    public float MagnitudeOf(SkillEffect effect, ModifierMode mode)
    {
        if (Magnitudes is null) return 0f;
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == effect && m.Mode == mode) sum += m.Value;
        return sum;
    }
}

/// <summary>Skill window grouping.</summary>
public enum SkillCategory { Physical = 0, Magic = 1, Buff = 2, Debuff = 3, Heal = 4 }

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
    public const string MightyBlow = "mighty_blow";
    public const string TwinSlash = "twin_slash";
    public const string PowerShot = "power_shot";
    public const string GreaterHeal = "greater_heal";
    public const string FlameBolt = "flame_bolt";
    public const string HolyStrike = "holy_strike";
    public const string GreaterWeakness = "greater_weakness";
    // Example learnable buff line (HP boost) used by healers/clerics+.
    public const string HpBoost1 = "hp_boost_1";
    public const string HpBoost2 = "hp_boost_2";
    public const string HpBoost3 = "hp_boost_3";
    public const string WindWalk = "wind_walk";
    public const string MassWindWalk = "mass_wind_walk";

    private static readonly Dictionary<string, SkillDef> All = BuildCatalog();

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
                MpCost: 12, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 45,
                Category: SkillCategory.Magic,
                Description: "Hurls a bolt of force. Spells fail rather than miss."),

            new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 20, CastTicks: 40, CooldownTicks: 10, Range: 0, Power: 60,
                Category: SkillCategory.Heal,
                Description: "Restores your own HP. Scales with WIT."),

            new(Weakness, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
                MpCost: 15, CastTicks: 40, CooldownTicks: 30, Range: 600, Power: 0,
                DurationTicks: 150, BuffKey: "curse_def", Rank: 1,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.30f) },
                Category: SkillCategory.Debuff,
                Description: "Curses the target: -30% Defence for 15s."),

            new(Fortify, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
                MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
                DurationTicks: 250, BuffKey: "fortify", Rank: 1,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.50f) },
                Category: SkillCategory.Buff,
                Description: "Tank stance: +50% Defence for 25s."),

            new(MightyBlow, "Mighty Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 18, CastTicks: 7, CooldownTicks: 60, Range: 0, Power: 85,
                Category: SkillCategory.Physical,
                Description: "A devastating two-hand strike for heavy damage."),

            new(TwinSlash, "Twin Slash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 12, CastTicks: 3, CooldownTicks: 25, Range: 0, Power: 55,
                Category: SkillCategory.Physical,
                Description: "Two rapid dagger slashes. Short cast and cooldown."),

            new(PowerShot, "Power Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
                MpCost: 16, CastTicks: 8, CooldownTicks: 40, Range: 900, Power: 70,
                Category: SkillCategory.Physical,
                Description: "A long-range aimed shot dealing heavy damage."),

            new(GreaterHeal, "Greater Heal", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 35, CastTicks: 45, CooldownTicks: 15, Range: 600, Power: 150,
                Category: SkillCategory.Heal,
                Description: "A powerful heal that can target an ally at range."),

            new(FlameBolt, "Flamebolt", BaseClass.Mage, SkillEffect.MagicDamage,
                MpCost: 24, CastTicks: 45, CooldownTicks: 10, Range: 600, Power: 95,
                Category: SkillCategory.Magic,
                Description: "A searing bolt — the nuker's stronger basic attack."),

            new(HolyStrike, "Holy Strike", BaseClass.Mage, SkillEffect.MagicDamage,
                MpCost: 20, CastTicks: 45, CooldownTicks: 10, Range: 600, Power: 70,
                Category: SkillCategory.Magic,
                Description: "A bolt of light — the healer's offensive spell."),

            new(GreaterWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
                MpCost: 22, CastTicks: 40, CooldownTicks: 30, Range: 600, Power: 0,
                DurationTicks: 200, BuffKey: "curse_def", Rank: 2,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffDef, 0.45f) },
                Category: SkillCategory.Debuff,
                Description: "A deeper curse: -45% Defence for 20s."),

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
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.15f) },
                Category: SkillCategory.Buff, SpCost: 3000,
                Description: "Raises Max HP by 15%."),
            new(HpBoost3, "HP Boost", BaseClass.Mage, SkillEffect.BuffHp,
                MpCost: 45, CastTicks: 10, CooldownTicks: 5, Range: 0, Power: 0,
                DurationTicks: 6000, BuffKey: "hp_boost", Rank: 3,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHp, 0.35f) },
                Category: SkillCategory.Buff, SpCost: 8000,
                Description: "Raises Max HP by 35%."),

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
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange)
    {
        if (def.Range <= 0)
            return basicAttackRange;

        bool isSpell = def.Effect.HasFlag(SkillEffect.MagicDamage)
            || def.Effect.HasFlag(SkillEffect.DebuffDef)
            || def.Effect.HasFlag(SkillEffect.Heal);

        if (isSpell && archetype is Archetype.Healer or Archetype.Nuker)
            return Math.Min(900f, def.Range + 500f);

        return def.Range;
    }

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
