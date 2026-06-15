namespace Game.Shared;

/// <summary>
/// A skill definition. All timings are in server ticks (10 t/s).
/// Range 0 means self-targeted. Magnitude/DurationTicks apply to buffs.
/// RequiredArchetype null = base-class skill (kept after class change);
/// set = signature skill unlocked by the second class.
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
    Archetype? RequiredArchetype = null);

public static class SkillCatalog
{
    private static readonly Dictionary<int, SkillDef> All = new SkillDef[]
    {
        // ----- Base class skills (kept after class change) ----------------------
        new(1, "Power Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 25),
        new(2, "War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, Magnitude: 0.20f),
        new(3, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 12, CastTicks: 10, CooldownTicks: 20, Range: 600, Power: 30),
        new(4, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 20, CastTicks: 15, CooldownTicks: 80, Range: 0, Power: 60),
        new(5, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 15, CastTicks: 10, CooldownTicks: 120, Range: 600, Power: 0,
            DurationTicks: 150, Magnitude: 0.30f),

        // ----- Second-class signature skills -----------------------------------------
        new(10, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
            DurationTicks: 250, Magnitude: 0.50f, RequiredArchetype: Archetype.Tank),
        new(11, "Mighty Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 18, CastTicks: 7, CooldownTicks: 80, Range: 0, Power: 65,
            RequiredArchetype: Archetype.Warrior),
        new(12, "Twin Slash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 12, CastTicks: 3, CooldownTicks: 35, Range: 0, Power: 40,
            RequiredArchetype: Archetype.Rogue),
        new(13, "Power Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 16, CastTicks: 8, CooldownTicks: 50, Range: 900, Power: 55,
            RequiredArchetype: Archetype.Archer),
        new(14, "Greater Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 35, CastTicks: 18, CooldownTicks: 100, Range: 600, Power: 140,
            RequiredArchetype: Archetype.Healer),
        new(15, "Flame Burst", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 28, CastTicks: 14, CooldownTicks: 60, Range: 600, Power: 75,
            RequiredArchetype: Archetype.Nuker),
    }.ToDictionary(s => s.Id);

    public static SkillDef? Get(int id) => All.GetValueOrDefault(id);

    /// <summary>Skills available to a character: base-class skills plus the
    /// signature skill of their second-class archetype (if any).</summary>
    public static IEnumerable<SkillDef> ForCharacter(BaseClass cls, Archetype? archetype) =>
        All.Values
            .Where(s => s.Class == cls &&
                        (s.RequiredArchetype is null || s.RequiredArchetype == archetype))
            .OrderBy(s => s.Id);

    /// <summary>Range 0 = melee skill: uses the character's basic-attack range
    /// (so an archer's bow skills are ranged). Mage second classes get +500
    /// spell range capped at 900 (design doc).</summary>
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

    /// <summary>WIT shortens cast times. 25 WIT ≈ 15% faster; cap 50%.</summary>
    public static int AdjustedCastTicks(int baseTicks, int wit) =>
        Math.Max(2, (int)MathF.Round(baseTicks * (1f - Math.Min(0.5f, wit * 0.006f))));

    /// <summary>Spell fail chance: 3% base + 2% per level the target is above
    /// the caster (never below 1%, capped at 80%).</summary>
    public static float SpellFailChance(int casterLevel, int targetLevel) =>
        Math.Clamp(0.03f + (targetLevel - casterLevel) * 0.02f, 0.01f, 0.80f);

    public static int PhysicalSkillDamage(int power, float effAttack, float effDefence) =>
        Math.Max(1, power + (int)(effAttack * 2 - effDefence));

    public static int MagicSkillDamage(int power, float effAttack, int wit, float effDefence) =>
        Math.Max(1, power + (int)(effAttack + wit * 2 - effDefence / 2));

    public static int HealAmount(int power, int wit) => power + wit * 2;

    public const float CritMultiplierSkills = 2.0f;

    /// <summary>Accuracy bonus on physical skills.</summary>
    public const int PhysicalSkillAccuracyBonus = 10;
}
