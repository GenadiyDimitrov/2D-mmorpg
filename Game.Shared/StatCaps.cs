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

    /// <summary>Physical crit RATE ceiling (50%, from DEX).</summary>
    public const float PhysicalCritRate = 0.50f;

    /// <summary>Magic crit RATE ceiling (20%, from WIT).</summary>
    public const float MagicCritRate = 0.20f;

    /// <summary>Physical SKILL "[Double]" RATE ceiling (25%, from the ATK stat — owner
    /// ruling 2026-08-05, docs/design/CritBlowAndDouble.md §1; was 30% off max(DEX,ATK)).</summary>
    public const float PhysicalDoubleRate = 0.25f;

    /// <summary>Physical crit DAMAGE ceiling (x10).</summary>
    public const float PhysicalCritDamage = 10.0f;

    /// <summary>Magic crit DAMAGE ceiling (x3).</summary>
    public const float MagicCritDamage = 3.0f;

    /// <summary>Magic-fail absolute floor (a spell can always fizzle ≥1%).</summary>
    public const float MagicFailFloor = 0.01f;

    /// <summary>Magic-fail ceiling (level gap can push fail up to 90%).</summary>
    public const float MagicFailMax = 0.90f;

    // ----- Unified hit resolution (see docs/design/CombatResolution.md) -----
    // One resolver decides land-vs-avoid for BOTH channels (physical miss, magic fail).
    // The stat roll lives inside a soft [AvoidBase, AvoidSoftCeil] band; true 0/100% are
    // reached only by the level-gap lockout and by Sure-Hit / Immunity flags.

    /// <summary>Avoid (miss/fail) chance at equal stats — the universal base.
    /// Symmetric guarantee: never below 5% to land, never above 95% from stats alone.</summary>
    public const float AvoidBase = 0.05f;

    /// <summary>Soft ceiling on the stat-roll portion (95% land floor lives at 1−this).
    /// The level-gap term and Immunity can still push the final avoid to 100%.</summary>
    public const float AvoidSoftCeil = 0.95f;

    /// <summary>Avoid chance moved per point of (defenderAvoidStat − attackerHitStat).
    /// First-pass tuning knob (1%/pt); the class floors carry most of the identity.</summary>
    public const float AvoidStatSlope = 0.01f;

    /// <summary>Legacy single crit-chance cap (kept for old references).</summary>
    public const float CritChance = 0.50f;

    /// <summary>Block chance ceiling — a fully-built tank can reach ~100%.</summary>
    public const float BlockChance = 1.0f;

    /// <summary>Block damage-reduction ceiling (max fraction removed on block).</summary>
    public const float BlockReduction = 0.80f;
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

    /// <summary>Ticks to stand up — after being hit OR choosing to stand — before you can move / cast /
    /// act again (the standing animation). 3s at 10 ticks/sec (owner, 2026-07-23).</summary>
    public const int StandUpTicks = 30;

    /// <summary>Seconds seated after which standing up is INSTANT (owner, 2026-07-24). The stand-up
    /// recovery exists to stop sit/stand SPAM — tapping sit for the regen tick and popping straight back
    /// up — so it should only cost that. Someone who genuinely rested has already spent far more time
    /// than the delay, and charging them again just makes resting feel bad. Being HIT while seated still
    /// pays the full recovery regardless: that is a combat interrupt, not a voluntary stand.</summary>
    public const float SettledSeconds = 3f;
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
