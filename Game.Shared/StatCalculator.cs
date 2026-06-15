namespace Game.Shared;

/// <summary>
/// Stat formulas live in Shared so the client can *predict* (tooltips,
/// estimated damage) while the server stays the only authority.
/// Base stats follow the design doc:
///   Ork/Demon  Fighter 40/30/10/20  Mage 30/30/20/20   (CON/ATK/WIT/DEX)
///   Elf/Angel  Fighter 30/20/20/30  Mage 20/20/30/30
///   Human      Fighter 35/25/15/25  Mage 25/25/25/25
/// </summary>
public static class StatCalculator
{
    public readonly record struct BaseStats(int Con, int Atk, int Wit, int Dex);

    public static BaseStats GetBaseStats(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Ork, BaseClass.Fighter) => new BaseStats(40, 30, 10, 20),
        (Race.Ork, BaseClass.Mage)    => new BaseStats(30, 30, 20, 20),
        (Race.Elf, BaseClass.Fighter) => new BaseStats(30, 20, 20, 30),
        (Race.Elf, BaseClass.Mage)    => new BaseStats(20, 20, 30, 30),
        (Race.Human, BaseClass.Fighter) => new BaseStats(35, 25, 15, 25),
        (Race.Human, BaseClass.Mage)    => new BaseStats(25, 25, 25, 25),
        _ => new BaseStats(25, 25, 25, 25)
    };

    // Per design: levels increase hp/mp (max/regen), evasion, accuracy,
    // defence, attack — nothing else. Tanks get more HP, mages more MP
    // (class scaling multipliers come with the class-tree phase).

    public static int MaxHp(int con, int level) => 50 + con * 4 + level * 10;

    public static int MaxMp(int wit, int level) => 30 + wit * 4 + level * 8;

    public static float HpRegenPerSecond(int con, int level) => 1f + con * 0.05f + level * 0.1f;

    public static float MpRegenPerSecond(int wit, int level) => 1f + wit * 0.05f + level * 0.08f;

    /// <summary>Base chance for a normal attack to miss is 2%; each point of
    /// evasion advantage adds 1%, clamped so a higher-level character almost
    /// never misses a lower one (design: floor ~1%, never above 90%).</summary>
    public static float MissChance(int attackerAccuracy, int targetEvasion)
    {
        const float baseMiss = 0.02f;
        float diff = (targetEvasion - attackerAccuracy) * 0.01f;
        return Math.Clamp(baseMiss + diff, 0.01f, 0.90f);
    }

    public static int Accuracy(int dex, int level) => dex + level;

    public static int Evasion(int dex, int level) => dex + level;

    // ----- Combat (Phase 2) -------------------------------------------------

    /// <summary>Effective attack power. Weapon damage joins this formula
    /// in the items phase: weapon + stat + buffs/passives.</summary>
    public static int AttackPower(int atkStat, int level) => atkStat + level * 2;

    public static int Defence(int con, int level) => con / 2 + level;

    /// <summary>Crit chance from DEX. Race/class/equipment modifiers come
    /// later. 25 DEX = 10%; capped at 50%.</summary>
    public static float CritChance(int dex) => Math.Clamp(0.05f + dex * 0.002f, 0f, 0.50f);

    /// <summary>Raw damage of one basic attack before the crit multiplier.</summary>
    public static int BasicAttackDamage(int attackPower, int defence) =>
        Math.Max(1, attackPower * 2 - defence);

    public const float CritMultiplier = 2.0f;

    // ----- Progression -------------------------------------------------------

    /// <summary>Exp required to go from <paramref name="level"/> to the next.</summary>
    public static long ExpToNext(int level) => 25L * level * level;

    public static int MobExpReward(int mobLevel) => 40 + mobLevel * 35;

    /// <summary>Mob stat block by level. Per design: higher-level mobs must
    /// out-stat lower-level characters.</summary>
    public static BaseStats MobStats(int level) =>
        new(Con: 15 + level * 2, Atk: 8 + level * 3, Wit: 5, Dex: 10 + level);
}
