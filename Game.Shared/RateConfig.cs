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
    /// <summary>THE SERVER'S OWN RATES — exp, sp, gold, drop chance, drop amount in one
    /// <see cref="RateSet"/> rather than five loose statics.
    ///
    /// <para>Make the game N times faster with <c>World = RateSet.Uniform(30f)</c>: you kill 1/30 as many
    /// creatures per level, and every reward is x30, so rewards-PER-LEVEL — which is what the economy
    /// actually is — come out identical to x1. Above 100% a drop chance pays COPIES
    /// (<see cref="MobCatalog.DropCopies"/>), so nothing is lost to a clamp and no second knob is needed;
    /// that is why <c>Uniform</c> deliberately leaves <c>DropAmount</c> at 1.</para>
    ///
    /// <para>⚠ **x1 is the default** (owner, 2026-08-05: *"make default x1 exp/drop/sp, I'll tune them if
    /// I need to"*). Exp shipped at 10 as a testing convenience and stayed there for a year of builds, so
    /// every levelling-pace number anyone quoted from a playtest before that date was a x10 number.</para>
    ///
    /// <para>The authored drop tables are the **x1 design** (owner, 2026-07-30): 5% authored means 5% at
    /// x1 and 15% at x3. Drop chance is expected to move — including to absurd values like x200 for an
    /// event — which is what <see cref="DropGroupRates"/> is for. ⚠ Drop chance went back to 1 on
    /// 2026-08-05 and the x3 was FOLDED INTO the groups actually taking it (gear 0.025 → 0.075, `other`
    /// 1 → 3); `tools/BalanceMatrix` confirmed every delivered number was unchanged. Only the units
    /// moved — so if it ever moves again, RE-MEASURE rather than reflexively resetting the groups.</para>
    ///
    /// <para>Live-editable from the admin tuning panel and, for drops, <c>/droprate global|amount</c>.</para></summary>
    public static RateSet World = RateSet.One;

    /// <summary>QUEST rewards only, composed ON TOP of <see cref="World"/>. At <see cref="RateSet.One"/>
    /// it changes nothing on its own — but routing quest rewards through it is what finally makes them
    /// obey the server rates at all: quest GOLD and quest SP used to be added raw, so on a x30 server
    /// every quest paid 1/30 of what the same effort paid in the field. Only <c>Exp</c>, <c>Sp</c> and
    /// <c>Gold</c> are read here; a quest hands out authored items, never a drop roll.</summary>
    public static RateSet Quest = RateSet.One;

    /// <summary>Per-GROUP multipliers, composed on top of <see cref="DropChanceRate"/>. The owner's own
    /// example: *"drop chance x200 and armor group multiplier x0.01 — in reality armor will be x2 drops."*
    /// So a group's real rate is `DropChanceRate x DropGroupRates[group]`, and one group can be tuned
    /// without touching another or re-authoring a mob.
    ///
    /// ⚠ The GUARANTEED groups (mats / scrolls / always) USED TO BE exempt from <see cref="DropChanceRate"/>
    /// and no longer are (2026-08-18). That exemption existed because of the 100% CLAMP: multiplying a
    /// 100% group by a server rate could not make it more generous, it only pinned it at the clamp and
    /// threw away every weight inside the group. <c>MobCatalog.DropCopies</c> removed the clamp, so a
    /// 100% group at x30 now fires thirty weighted picks with the table's proportions intact — and the
    /// owner's *"at x10 or x200 I still want the group chances at their current ones"* is honoured by the
    /// AUTHORED numbers staying untouched, not by the knob skipping them.
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

}
