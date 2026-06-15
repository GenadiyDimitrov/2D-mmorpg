namespace Game.Shared;

/// <summary>
/// A skill definition. All timings are in server ticks (10 t/s).
/// Range 0 means self-targeted. Magnitude/DurationTicks apply to buffs.
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
    float Magnitude = 0f);

/// <summary>
/// The skill book. Lives in Shared so the client can build the skill bar and
/// tooltips, while the server independently validates and resolves everything.
/// The class tree (2nd classes at lvl 20) extends this catalog in Phase 4.
/// </summary>
public static class SkillCatalog
{
    private static readonly Dictionary<int, SkillDef> All = new SkillDef[]
    {
        // ----- Fighter ------------------------------------------------------
        // Physical skills get +10 accuracy (design: "less likely to miss,
        // but evasion-focused builds still feel evasion working").
        new(1, "Power Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 5, CooldownTicks: 30,
            Range: GameConstants.MeleeRange, Power: 25),

        new(2, "War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300,
            Range: 0, Power: 0, DurationTicks: 300, Magnitude: 0.20f),

        // ----- Mage --------------------------------------------------------------
        // Spells never miss — they FAIL, scaling 2%/level the target is above
        // the caster (design doc).
        new(3, "Magic Bolt", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 12, CastTicks: 10, CooldownTicks: 20,
            Range: 600, Power: 30),

        new(4, "Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 20, CastTicks: 15, CooldownTicks: 80,
            Range: 0, Power: 60),

        new(5, "Weakness", BaseClass.Mage, SkillEffect.DebuffDef,
            MpCost: 15, CastTicks: 10, CooldownTicks: 120,
            Range: 600, Power: 0, DurationTicks: 150, Magnitude: 0.30f),
    }.ToDictionary(s => s.Id);

    public static SkillDef? Get(int id) => All.GetValueOrDefault(id);

    public static IEnumerable<SkillDef> ForClass(BaseClass cls) =>
        All.Values.Where(s => s.Class == cls).OrderBy(s => s.Id);

    /// <summary>WIT shortens cast times (design: every skill has a cast time
    /// that depends on WIT). 25 WIT ≈ 15% faster; capped at 50%, floor 0.2s.</summary>
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

    /// <summary>Accuracy bonus on physical skills.</summary>
    public const int PhysicalSkillAccuracyBonus = 10;
}
