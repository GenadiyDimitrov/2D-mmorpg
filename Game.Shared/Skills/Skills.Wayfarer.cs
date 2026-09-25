namespace Game.Shared;

/// <summary>`BL-277` part 3 — the two Wayfarer RUNE buffs.
///
/// <para>(The Blessing's own buff-bar face, `wayfarer_blessing`, was DELETED 2026-09-25 in `BL-295`: the
/// HUD's Blessing bar shows the gauge and the countdown instead, so there is no buff a player could try
/// to remove. The truth was always <c>Entity.BlessingSecondsLeft</c>.)</para></summary>
public static partial class SkillCatalog
{

    /// <summary>`BL-277` part 3 — the buff of a held Favor keep-rune (1 h / 2 h items, one buff). While it
    /// is up a kill does not drain the Favor. A RUNE buff: owned by <c>ReconcileTimedItems</c>, never saved.</summary>
    public const string FavorKeepRuneBuff = "rune_favor_keep";

    /// <summary>`BL-277` part 3 — the buff of a held Blessing booster rune: the Blessing fills ×2 from
    /// every source (<see cref="WayfarerBlessing.BoosterRuneFillRate"/>). A RUNE buff, like the keep-rune.</summary>
    public const string BlessingBoostRuneBuff = "rune_blessing_boost";

    private static SkillDef[] WayfarerSkills() => new[]
    {
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
