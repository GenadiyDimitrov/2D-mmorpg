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
    /// Result is clamped to 100%.
    ///
    /// **1, deliberately** (playtest-14 §2/§3, 2026-07-30). The owner authors drop rates as the numbers
    /// he wants to SEE in game ("now roughly 20/12/5, target 5/2/0.2"), and he reads them off the target
    /// window. At x3 the authored table was not the table: 5% became 15%, and — worse — the guaranteed
    /// groups (mats 100%, always 100%, scrolls 70%) all saturated at the 100% clamp, which silently threw
    /// away every weight inside them. The 1x table IS the design now; retune a mob's numbers, not this.</summary>
    public static float DropChanceRate = 1f;

    /// <summary>Multiplier on each drop's QUANTITY (stack size).</summary>
    public static float DropAmountRate = 1f;

    // ----- Currency -------

    /// <summary>Multiplier on gold drop AMOUNT.</summary>
    public static float GoldAmountRate = 1f;
}
