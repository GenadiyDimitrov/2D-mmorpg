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
    };
}
