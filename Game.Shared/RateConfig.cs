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
    /// The authored drop tables are the **x1 design** (owner, 2026-07-30): 5% authored means 5% at x1 and
    /// 15% at x3. This is the SERVER's rate knob and it is expected to move — including to absurd values
    /// like x200 for an event — which is exactly why it no longer touches everything (see
    /// <see cref="DropGroupRates"/>).</summary>
    public static float DropChanceRate = 3f;

    /// <summary>Per-GROUP multipliers, composed on top of <see cref="DropChanceRate"/>. The owner's own
    /// example: *"drop chance x200 and armor group multiplier x0.01 — in reality armor will be x2 drops."*
    /// So a group's real rate is `DropChanceRate x DropGroupRates[group]`, and one group can be tuned
    /// without touching another or re-authoring a mob.
    ///
    /// The GUARANTEED groups (mats / scrolls / always) are exempt from <see cref="DropChanceRate"/>
    /// entirely — see <c>MobCatalog.EffectiveRate</c>. Their chances are authored as absolutes (mats 100%,
    /// always 100%, scrolls 70%) and the owner wants them to stay put *"at x10 or x200"*. Multiplying them
    /// by a server rate does not make them more generous, it just pins them at the 100% clamp and throws
    /// away every weight inside the group. Their multiplier here still works, so they remain tunable.
    ///
    /// Live-editable with <c>/droprate</c> (admin), so a rate can be dialled in DURING a playtest rather
    /// than guessed at and rebuilt.</summary>
    public static readonly Dictionary<string, float> DropGroupRates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // The four GEAR groups ship at 1/3, which is not a fudge — it is this system doing its job.
            // The authored table is the x1 design and the server runs at x3, but the owner's acceptance
            // test is an ABSOLUTE one: ~400k of trash gold by level 25. At x3 flat that is 1.08M (2.7x
            // over); x3 x 1/3 measures 402k. So the design stays readable at x1 AND the faucet stays shut
            // at the live rate, which is the whole point of separating the two knobs.
            // If DropChanceRate is ever set back to 1, set these back to 1 with it.
            ["armor"] = 1f / 3f, ["accessory"] = 1f / 3f, ["weapon"] = 1f / 3f, ["jewel"] = 1f / 3f,
            ["mats"] = 1f, ["scrolls"] = 1f, ["always"] = 1f, ["other"] = 1f,
        };

    /// <summary>A group's own multiplier (1 for anything unknown, so a new group is inert until named).</summary>
    public static float DropGroupRate(string group) =>
        DropGroupRates.TryGetValue(group, out float v) ? v : 1f;

    /// <summary>Multiplier on each drop's QUANTITY (stack size).</summary>
    public static float DropAmountRate = 1f;

    // ----- Currency -------

    /// <summary>Multiplier on gold drop AMOUNT.</summary>
    public static float GoldAmountRate = 1f;
}
