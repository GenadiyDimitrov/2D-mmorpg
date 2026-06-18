namespace Game.Shared;

/// <summary>
/// Global server rate multipliers — the one place to tune progression speed.
/// Flip ExpRate to 50 for fast testing, back to 1 for live. Applied at the
/// moment exp/sp are awarded and drops roll, on top of per-mob drop chances.
///
/// (Later this can load from a config file / env so rates change without a
/// recompile; for now they're constants.)
/// </summary>
public static class RateConfig
{
    /// <summary>Experience multiplier (x10 = ten times normal exp).</summary>
    public static float ExpRate = 10f;

    /// <summary>Skill-point multiplier. SP still accrues at 1/4 exp; this scales
    /// that result independently so you can tune the SP economy separately.</summary>
    public static float SpRate = 1f;

    /// <summary>Multiplier on each drop's CHANCE (x3 = three times as likely).
    /// Result is clamped to 100%.</summary>
    public static float DropChanceRate = 3f;

    /// <summary>Multiplier on each drop's QUANTITY (stack size).</summary>
    public static float DropAmountRate = 1f;

    // ----- Reserved for the currency phase (gold not implemented yet) -------

    /// <summary>Multiplier on adena (gold) drop AMOUNT once currency exists.</summary>
    public static float AdenaAmountRate = 1f;
}
