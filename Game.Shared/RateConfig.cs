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
    /// <summary>Experience multiplier (x10 = ten times normal exp).
    /// ⚠ **x1 is the default now** (owner, 2026-08-05: *"make default x1 exp/drop/sp, I'll tune them if
    /// I need to"*). It shipped at 10 as a testing convenience and stayed there for a year of builds, so
    /// every levelling-pace number anyone has quoted from a playtest was a x10 number.</summary>
    public static float ExpRate = 1f;

    /// <summary>Skill-point multiplier. SP still accrues at 1/4 exp; this scales
    /// that result independently so you can tune the SP economy separately.</summary>
    public static float SpRate = 1f;

    /// <summary>Multiplier on each drop's CHANCE (x3 = three times as likely).
    /// Result is clamped to 100%.
    ///
    /// The authored drop tables are the **x1 design** (owner, 2026-07-30): 5% authored means 5% at x1 and
    /// 15% at x3. This is the SERVER's rate knob and it is expected to move — including to absurd values
    /// like x200 for an event — which is exactly why it no longer touches everything (see
    /// <see cref="DropGroupRates"/>).
    ///
    /// ⚠ **x1 is the default now** (owner, 2026-08-05). It was 3, and the file already warned that
    /// going back to 1 means RE-MEASURE rather than reflexively resetting the group rates with it. So
    /// the x3 was FOLDED INTO the groups that were actually taking it — gear 0.025 → 0.075, `other`
    /// 1 → 3 — and `tools/BalanceMatrix` confirms every delivered number is unchanged: the reference
    /// farm still totals 1,038,115 and attribute scrolls still land at 3.6 %/kill. The knob reads 1
    /// and the game plays exactly as it was measured; only the units moved.</summary>
    public static float DropChanceRate = 1f;

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
            // The four GEAR groups ship at 0.025, which is not a fudge — it is this system doing its job.
            // The authored table is the x1 design and the server runs at x3; this multiplier is what
            // holds the FAUCET shut, and it is set from a measurement, not a guess (playtest-18, owner
            // 2026-08-05). He ran three characters through the same ~14-15 h idle farm: one that sold
            // nothing finished level 34 with 350k (pure coin), one that sold only EQUIPMENT finished with
            // 3.3kk, one that sold everything finished with 4.6kk. BalanceMatrix reproduces all three,
            // and it puts SOLD GEAR at ~10x the mob's own gold drop — mats and potions together are 2%,
            // so gear is the entire faucet and the only group worth cutting.
            //
            // His target was ~1kk over that farm, and the choice of KNOB was deliberate: cutting the
            // sell price alone leaves you buried in junk to click through, so the drop RATE takes the cut
            // (13x rarer) and the price moves the other way (GearSellDivisor 25 -> 10) so that the piece
            // you do find is worth finding. Measured result: ~1.1M over the same farm.
            // Re-run `dotnet run --project tools/BalanceMatrix` after touching this; do not re-derive it.
            //
            // ⚠ These four are NOT a compensation for DropChanceRate any more. If the global rate ever
            // goes back to 1, RE-MEASURE — don't reflexively set these back to 1 with it.
            //
            // 2026-08-05: it DID go back to 1, and this is that re-measure. The four gear groups are
            // 0.025 x 3 = 0.075 and `other` is 1 x 3 = 3, so both deliver exactly what they delivered
            // under the global x3 — the gear faucet stays where the three-character farm measurement
            // put it, and the attribute scrolls stay at the 3.6 %/kill that playtest-18 V2b authored.
            // ⚠ `other` is the INDEPENDENT rolls (attribute scrolls among them), which take the global
            // that the guaranteed groups are exempt from; that asymmetry is the whole reason a single
            // number could not just move to 1 on its own.
            ["armor"] = 0.075f, ["accessory"] = 0.075f, ["weapon"] = 0.075f, ["jewel"] = 0.075f,
            ["mats"] = 1f, ["scrolls"] = 1f, ["always"] = 1f, ["other"] = 3f,
        };

    /// <summary>A group's own multiplier (1 for anything unknown, so a new group is inert until named).</summary>
    public static float DropGroupRate(string group) =>
        DropGroupRates.TryGetValue(group, out float v) ? v : 1f;

    /// <summary>Per-ITEM multipliers, composed on top of the group's rate — the third and finest knob
    /// (owner, playtest-15 §3: *"so a single item, a Scroll of Resurrect, a specific potion, can be
    /// tuned independently of its rarity"*).
    ///
    /// Empty by default: an item with no entry here multiplies by 1, so this changes nothing until
    /// something is deliberately named. Set with <c>/droprate item &lt;id|name&gt; &lt;mult&gt;</c>;
    /// setting it back to 1 removes the override rather than storing a no-op.
    ///
    /// Inside an exclusive drop GROUP an entry's chance is a WEIGHT, so this knob does two things at
    /// once and both are wanted: it scales the item's share of the pick AND its contribution to the
    /// group firing at all. Raising one member of a guaranteed group (always = 100%) therefore takes
    /// share FROM its siblings rather than making the group fire more often, which is exactly what
    /// "tune this one item, not its rarity rung" has to mean.</summary>
    public static readonly Dictionary<string, float> DropItemRates =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>An item's own multiplier (1 when it has no override, which is the normal case).</summary>
    public static float DropItemRate(string itemId) =>
        itemId is not null && DropItemRates.TryGetValue(itemId, out float v) ? v : 1f;

    /// <summary>Multiplier on each drop's QUANTITY (stack size).</summary>
    public static float DropAmountRate = 1f;

    // ----- Currency -------

    /// <summary>Multiplier on gold drop AMOUNT.</summary>
    public static float GoldAmountRate = 1f;
}
