namespace Game.Shared;

/// <summary>
/// Central place for stat ceilings. Every Effective* getter clamps to its cap
/// here, so caps are discoverable in one spot instead of scattered magic
/// numbers. Most caps are raisable per-entity later (a buff can lift the move
/// cap for a rogue ultimate), so the clamp reads `base cap + bonus` rather than
/// a hard constant.
/// </summary>
public static class StatCaps
{
    /// <summary>Movement speed ceiling for a normal player (fully buffed/geared).
    /// A future rogue ultimate raises this per-entity to outrun even a buffed
    /// mage. Bases sit well below this so gear/buffs climb toward it.</summary>
    public const float MoveSpeed = 250f;

    /// <summary>Attack-speed stat ceiling (L2-style: 1500 ≈ x4.5 attacks/sec).
    /// Enforced once the DEX→attack-speed formula lands (casting round).</summary>
    public const int AttackSpeed = 1500;

    /// <summary>Cast-speed stat ceiling (1999 ≈ x6 faster). Enforced when the
    /// WIT→cast-speed formula lands (casting round).</summary>
    public const int CastSpeed = 1999;

    /// <summary>Crit chance ceiling.</summary>
    public const float CritChance = 0.50f;
}

/// <summary>A player's movement/regen state. Walk/Run switch instantly; Sit has
/// a stand-up delay when broken by damage. Regen multipliers stack on top.</summary>
public enum MoveState { Running = 0, Walking = 1, Sitting = 2 }

/// <summary>Speed + regen tuning for movement states.</summary>
public static class MovementTuning
{
    /// <summary>Regen multiplier per state (Running ×1, Walking +20%, Sitting +80%).</summary>
    public static float RegenMultiplier(MoveState state) => state switch
    {
        MoveState.Walking => 1.2f,
        MoveState.Sitting => 1.8f,
        _ => 1.0f
    };

    /// <summary>Fraction of run speed used while walking.</summary>
    public const float WalkSpeedFactor = 0.5f;

    /// <summary>Ticks to stand up after being hit while sitting (~0.7s) before
    /// you can move/cast/act again.</summary>
    public const int StandUpTicks = 7;
}

/// <summary>
/// Base run speeds per race + base class. 250 is the buffed CAP; these bases sit
/// below it so gear/buffs/passives climb toward it. Elf fastest, Human slowest;
/// within a race, fighters (esp. rogues) beat mages. Walk speed is derived.
/// </summary>
public static class SpeedTable
{
    public static float BaseRunSpeed(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Elf,   BaseClass.Fighter) => 165f,
        (Race.Elf,   BaseClass.Mage)    => 145f,
        (Race.Ork,   BaseClass.Fighter) => 155f,
        (Race.Ork,   BaseClass.Mage)    => 138f,
        (Race.Human, BaseClass.Fighter) => 148f,
        (Race.Human, BaseClass.Mage)    => 130f,
        (Race.God,   _)                 => 200f,   // debug race: fast
        _ => 140f
    };
}
