namespace Game.Shared;

/// <summary>`BL-277` part 2 — the buff-bar FACE of the Wayfarer's Blessing (see <see cref="WayfarerBlessing"/>).
///
/// <para>🔑 **Cosmetic, the `BL-98` pattern.** The truth is <c>Entity.BlessingSecondsLeft</c>; the EXP
/// bonus, the Favor protection and the refund all read that, never this buff. The loop re-asserts the
/// icon every second, so death, a subclass swap, a cleanse or a double-click cannot end the Blessing early
/// — removing the buff simply does nothing. For the same reason it is never saved as a buff: the seconds
/// left are saved on the character instead.</para></summary>
public static partial class SkillCatalog
{
    public const string WayfarerBlessingBuff = "wayfarer_blessing";

    /// <summary>`BL-277` part 3 — the buff of a held Favor keep-rune (1 h / 2 h items, one buff). While it
    /// is up a kill does not drain the Favor. A RUNE buff: owned by <c>ReconcileTimedItems</c>, never saved.</summary>
    public const string FavorKeepRuneBuff = "rune_favor_keep";

    /// <summary>`BL-277` part 3 — the buff of a held Blessing booster rune: the Blessing fills ×2 from
    /// every source (<see cref="WayfarerBlessing.BoosterRuneFillRate"/>). A RUNE buff, like the keep-rune.</summary>
    public const string BlessingBoostRuneBuff = "rune_blessing_boost";

    private static SkillDef[] WayfarerSkills() => new[]
    {
        // ⚠ No duration of its own: every apply passes `durationOverride` from the entity's clock.
        new SkillDef(WayfarerBlessingBuff, "Wayfarer's Blessing", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: WayfarerBlessing.DurationSeconds * GameConstants.TickRate,
            BuffKey: WayfarerBlessingBuff, Rank: 1,
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Cancellable: false, CountsTowardBuffLimit: false,
            Description: "Wayfarer's Blessing: +100% EXP and SP from monsters, and kills do not drain "
                       + "the Wayfarer's Favor — each one gives back what it would have drained."),

        // The two rune buffs. Payload-free on purpose: the kill (FavorOnKill) and the fill rate
        // (BlessingFillRate) ASK whether the buff is up, the way the Blessing is asked by its clock.
        // DurationTicks is nominal; ReconcileTimedItems drives it from the item's wall clock.
        WayfarerRune(FavorKeepRuneBuff, "Favor Keep-Rune", "FKR",
            "Held rune: your kills do not drain the Wayfarer's Favor while it is in your bag."),
        WayfarerRune(BlessingBoostRuneBuff, "Blessing Booster Rune", "BBR",
            "Held rune: the Wayfarer's Blessing gauge fills twice as fast from every source while it is in your bag."),
    };

    private static SkillDef WayfarerRune(string id, string name, string abbrev, string desc) =>
        new(id, name, BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 36000, BuffKey: id, Rank: 1,
            // Never against the buff cap — same reason as every rune (Skills.RewardRunes.cs): the
            // reconciliation re-applies it on the next pass, so an eviction would buy nothing.
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable, CountsTowardBuffLimit: false,
            Abbrev: abbrev, Description: desc, SpCost: 0);
}
