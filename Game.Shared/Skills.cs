namespace Game.Shared;

/// <summary>
/// A skill definition. All timings are in server ticks (10 t/s).
/// Range 0 means self-targeted (or "use my basic-attack range" for melee
/// damage skills — see SkillMath.EffectiveRange). Magnitude/DurationTicks
/// apply to buffs.
/// </summary>
public record SkillDef(
    int Id,
    string Name,
    BaseClass Class,
    SkillEffect Effect,
    int MpCost,
    int CastTicks,
    int CooldownTicks,
    float Range,
    int Power,
    int DurationTicks = 0,
    float Magnitude = 0f,
    float Magnitude2 = 0f,
    string Description = "");

// ===========================================================================
//  WHERE TO EDIT SKILLS
//
//  1. SkillCatalog.All           -> define every skill (its numbers + text).
//  2. ClassProgression           -> decide which skills each class gets,
//                                    whether they REPLACE a base skill, and at
//                                    what level they unlock.
//
//  To add, e.g., a Witch DoT at level 25 the Sorcerer doesn't get:
//    - add a SkillDef in SkillCatalog.All
//    - add a SkillGrant(thatId, unlockLevel: 25) to a Race-specific row in
//      ClassProgression.RaceOverrides ((Race.Ork, Archetype.Nuker)).
//  No other code changes needed — server validates and client renders from
//  these tables.
// ===========================================================================

/// <summary>One skill a class receives. ReplacesSkillId != 0 means this skill
/// upgrades/removes a base skill (e.g. Flamebolt replaces Magic Bolt).
/// UnlockLevel gates it (20 = on class change; 25/30/... = later unlocks).</summary>
public record SkillGrant(int SkillId, int UnlockLevel = 20, int ReplacesSkillId = 0);

public static class SkillCatalog
{
    // ----- Skill ids (named for readability in the progression tables) ---------
    public const int PowerStrike = 1, WarCry = 2, MagicBolt = 3, Heal = 4, Weakness = 5;
    public const int Fortify = 10, MightyBlow = 11, TwinSlash = 12, PowerShot = 13;
    public const int GreaterHeal = 14, FlameBolt = 15, HolyStrike = 16, StrongWeakness = 17;
    public const int BattleFury = 18, GreaterWarCry = 19;

    private static readonly Dictionary<int, SkillDef> All = new SkillDef[]
    {
        // ===== Base class skills =====
        // Mage main skills: ~4s cast (WIT reduces), ~1s cooldown -> chain-cast.
        new(PowerStrike, "Power Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 30,
            Description: "A forceful melee blow. Bonus accuracy, but can still miss."),
        new(WarCry, "War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, Magnitude: 0.20f,
            Description: "Battle shout: +20% Attack Power for 30s."),
        new(MagicBolt, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 12, CastTicks: 40, CooldownTicks: 10, Range: 600, Power: 45,
            Description: "Hurls a bolt of force. Spells fail rather than miss."),
        new(Heal, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 20, CastTicks: 40, CooldownTicks: 10, Range: 0, Power: 60,
            Description: "Restores your own HP. Scales with WIT."),
        new(Weakness, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 15, CastTicks: 40, CooldownTicks: 30, Range: 600, Power: 0,
            DurationTicks: 150, Magnitude: 0.30f,
            Description: "Curses the target: -30% Defence for 15s."),

        // ===== Fighter second-class skills =====
        new(Fortify, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
            DurationTicks: 250, Magnitude: 0.50f,
            Description: "Tank stance: +50% Defence for 25s."),
        new(MightyBlow, "Mighty Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 18, CastTicks: 7, CooldownTicks: 60, Range: 0, Power: 85,
            Description: "A devastating two-hand strike for heavy damage."),
        new(TwinSlash, "Twin Slash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 12, CastTicks: 3, CooldownTicks: 25, Range: 0, Power: 55,
            Description: "Two rapid dagger slashes. Short cast and cooldown."),
        new(PowerShot, "Power Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 16, CastTicks: 8, CooldownTicks: 40, Range: 900, Power: 70,
            Description: "A long-range aimed shot dealing heavy damage."),

        // ===== Mage second-class skills =====
        new(GreaterHeal, "Greater Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 35, CastTicks: 45, CooldownTicks: 15, Range: 600, Power: 150,
            Description: "A powerful heal that can target an ally at range."),
        new(FlameBolt, "Flamebolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 24, CastTicks: 45, CooldownTicks: 10, Range: 600, Power: 95,
            Description: "A searing bolt — the nuker's stronger basic attack."),
        new(HolyStrike, "Holy Strike", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 20, CastTicks: 45, CooldownTicks: 10, Range: 600, Power: 70,
            Description: "A bolt of light — the healer's offensive spell."),
        new(StrongWeakness, "Greater Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 22, CastTicks: 40, CooldownTicks: 30, Range: 600, Power: 0,
            DurationTicks: 200, Magnitude: 0.45f,
            Description: "A deeper curse: -45% Defence for 20s."),

        // Rogue/Archer: replaces War Cry with attack + move speed.
        new(BattleFury, "Battle Fury", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, Magnitude: 0.20f, Magnitude2: 0.15f,
            Description: "+20% Attack and +15% Move Speed for 30s."),

        // Warrior: upgrades War Cry to a stronger attack buff.
        new(GreaterWarCry, "Greater War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 18, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, Magnitude: 0.30f,
            Description: "Battle shout: +30% Attack Power for 30s."),
    }.ToDictionary(s => s.Id);

    public static SkillDef? Get(int id) => All.GetValueOrDefault(id);

    public static string DescriptionOf(int id) => Get(id)?.Description ?? "";
}

/// <summary>
/// THE place to define what each class knows. Add later unlocks (level 25+)
/// and per-class flavour skills here.
/// </summary>
public static class ClassProgression
{
    // Base kits (before class change): shared by a base class.
    private static readonly SkillGrant[] BaseFighter =
    {
        new(SkillCatalog.PowerStrike, 1), new(SkillCatalog.WarCry, 5)
    };
    private static readonly SkillGrant[] BaseMage =
    {
        new(SkillCatalog.MagicBolt, 1), new(SkillCatalog.Weakness, 3), new(SkillCatalog.Heal, 5)
    };

    /// <summary>Second-class kits, shared by all races of an archetype.
    /// ReplacesSkillId removes the base skill and swaps in the upgrade.</summary>
    private static readonly Dictionary<Archetype, SkillGrant[]> SecondClassByArchetype = new()
    {
        [Archetype.Tank] = new[]
        {
            new SkillGrant(SkillCatalog.PowerStrike),
            new SkillGrant(SkillCatalog.Fortify, 20, SkillCatalog.WarCry),
        },
        [Archetype.Warrior] = new[]
        {
            new SkillGrant(SkillCatalog.GreaterWarCry, 20, SkillCatalog.WarCry),
            new SkillGrant(SkillCatalog.MightyBlow, 20, SkillCatalog.PowerStrike),
        },
        [Archetype.Rogue] = new[]
        {
            new SkillGrant(SkillCatalog.BattleFury, 20, SkillCatalog.WarCry),
            new SkillGrant(SkillCatalog.TwinSlash, 20, SkillCatalog.PowerStrike),
        },
        [Archetype.Archer] = new[]
        {
            new SkillGrant(SkillCatalog.BattleFury, 20, SkillCatalog.WarCry),
            new SkillGrant(SkillCatalog.PowerShot, 20, SkillCatalog.PowerStrike),
        },
        [Archetype.Healer] = new[]
        {
            new SkillGrant(SkillCatalog.GreaterHeal, 20, SkillCatalog.Heal),
            new SkillGrant(SkillCatalog.HolyStrike, 20, SkillCatalog.MagicBolt),
            new SkillGrant(SkillCatalog.Weakness),
        },
        [Archetype.Nuker] = new[]
        {
            new SkillGrant(SkillCatalog.FlameBolt, 20, SkillCatalog.MagicBolt),
            new SkillGrant(SkillCatalog.Heal),
            new SkillGrant(SkillCatalog.StrongWeakness, 20, SkillCatalog.Weakness),
        },
    };

    /// <summary>Per-(Race, Archetype) EXTRA skills for true class identity.
    /// Add level-25+ flavour here so the Witch and Sorcerer diverge. These are
    /// ADDED on top of the shared archetype kit.
    /// Example (commented until you define the skills):
    ///   [(Race.Ork, Archetype.Nuker)]   = new[] { new SkillGrant(dotId, 25) },
    ///   [(Race.Human, Archetype.Nuker)] = new[] { new SkillGrant(burstId, 25) },
    /// </summary>
    private static readonly Dictionary<(Race, Archetype), SkillGrant[]> RaceOverrides = new()
    {
        // (empty for now — ready for per-class skills)
    };

    /// <summary>All skill grants a character has access to (ignoring level).</summary>
    public static IEnumerable<SkillGrant> Grants(Race race, BaseClass baseClass, Archetype? archetype)
    {
        if (archetype is null)
            return baseClass == BaseClass.Fighter ? BaseFighter : BaseMage;

        IEnumerable<SkillGrant> grants = SecondClassByArchetype.TryGetValue(archetype.Value, out var kit)
            ? kit
            : Array.Empty<SkillGrant>();

        if (RaceOverrides.TryGetValue((race, archetype.Value), out var extra))
            grants = grants.Concat(extra);

        return grants;
    }

    /// <summary>Skills the character can actually use right now (unlock level met).
    /// Resolves replacements: if an upgrade is unlocked, the replaced base skill
    /// is hidden.</summary>
    public static IEnumerable<SkillDef> UsableSkills(Race race, BaseClass baseClass, Archetype? archetype, int level)
    {
        var grants = Grants(race, baseClass, archetype).ToList();

        var replaced = grants
            .Where(g => g.ReplacesSkillId != 0 && level >= g.UnlockLevel)
            .Select(g => g.ReplacesSkillId)
            .ToHashSet();

        foreach (var grant in grants)
        {
            if (level < grant.UnlockLevel) continue;
            if (replaced.Contains(grant.SkillId)) continue;
            if (SkillCatalog.Get(grant.SkillId) is SkillDef def)
                yield return def;
        }
    }

    public static bool CanUse(int skillId, Race race, BaseClass baseClass, Archetype? archetype, int level) =>
        UsableSkills(race, baseClass, archetype, level).Any(s => s.Id == skillId);
}

/// <summary>Combat math + range/cast helpers.</summary>
public static class SkillMath
{
    public static float EffectiveRange(SkillDef def, Archetype? archetype, float basicAttackRange)
    {
        if (def.Range <= 0)
            return basicAttackRange;

        bool isSpell = def.Effect is SkillEffect.MagicDamage or SkillEffect.DebuffDef
            or SkillEffect.Heal;

        if (isSpell && archetype is Archetype.Healer or Archetype.Nuker)
            return Math.Min(900f, def.Range + 500f);

        return def.Range;
    }

    /// <summary>WIT 25 is the baseline (0% modifier). Each point above 25
    /// speeds casting, each point below slows it, at 1.2% per point. Negative
    /// result = faster. Clamped to [-50%, +50%].</summary>
    public const int CastBaselineWit = 25;
    public const float CastPercentPerWit = 0.012f;

    /// <summary>Cast-time modifier as a fraction. Negative = faster.</summary>
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
