namespace Game.Shared;

// ===========================================================================
//  PREMIUM REWARD RUNES — the BUFF side. The table itself (channels, ladder, wording) is
//  RewardRunes.cs; the items are in Items.cs. All three read the same rows.
//
//  Nothing here is new machinery. A rune is already an ITEM that grants a BUFF while it sits in
//  the main bag (ItemDef.IsRune + RuneBuffSkillId, reconciled ~1/s by GameLoopService.
//  ReconcileTimedItems), so the buff bar shows it, the wall clock expires it and a warehouse
//  deposit switches it off — all for free. The only new thing is the PAYLOAD: RewardRates, which
//  rides as a FIELD because the SkillEffect flag enum is full (1L << 62 was the last bit, and four
//  reward channels would have wanted four of them).
// ===========================================================================

public static partial class SkillCatalog
{
    /// <summary>The five ladder runes + the two zeroing ones. Assembled in BuildCatalog.</summary>
    private static IEnumerable<SkillDef> RewardRuneSkills()
    {
        foreach (var ch in RewardRunes.All)
        {
            // One skill per channel; its LEVELS are the rungs. Each level carries its own rates AND
            // its own sentence, so the buff bar's popup states that rune's actual number instead of a
            // generic blurb (ApplyBuff stores DescriptionAt for a leveled buff).
            var levels = new SkillLevel[RewardRunes.Ladder.Length];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new SkillLevel(
                    SpCost: 0,
                    Description: ch.Line(RewardRunes.Percent(i)),
                    Rewards: ch.RatesAt(RewardRunes.Ladder[i]));

            yield return RewardRune(ch.SkillId, ch.Name, ch.Abbrev,
                default, ch.Line(RewardRunes.Percent(0)), levels);
        }

        yield return RewardRune(RewardRunes.SinisterId, RewardRunes.SinisterName, "SIN",
            new RewardRates(StopsExpSp: true), RewardRunes.SinisterLine);

        yield return RewardRune(RewardRunes.SinnersId, RewardRunes.SinnersName, "SNR",
            new RewardRates(StopsExpSp: true, StopsGoldDrop: true), RewardRunes.SinnersLine);
    }

    /// <summary>One reward-rune buff. <paramref name="levels"/> null = single-rung (the two zeroing
    /// runes), in which case <paramref name="rates"/> is the payload.</summary>
    private static SkillDef RewardRune(string id, string name, string abbrev,
        RewardRates rates, string desc, SkillLevel[]? levels = null) =>
        new(id, name, BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            // DurationTicks is NOMINAL only: ReconcileTimedItems overwrites TicksRemaining from the
            // ITEM's wall clock every second, which is what lets a rune's timer run while offline.
            DurationTicks: 36000, BuffKey: id, Rank: 1,
            Category: SkillCategory.Buff, BuffRow: BuffRow.Consumable,
            Abbrev: abbrev, Description: desc, SpCost: 0,
            Levels: levels, Rewards: rates);
}
