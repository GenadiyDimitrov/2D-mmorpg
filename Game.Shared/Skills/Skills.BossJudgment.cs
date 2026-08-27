namespace Game.Shared;

/// <summary>
/// `BL-98` — **THE BOSS'S JUDGMENT**, a six-rung punishment ladder for interfering in a raid more
/// than <see cref="StatCalculator.BossJudgmentGap"/> levels from your own. Owner, 2026-08-28.
///
/// <para>🔑 <b>THE RUNGS ALTERNATE: ODD = PETRIFIED, EVEN = REMEMBERED.</b> That is the whole shape,
/// and it is what makes the ladder work at all — you cannot offend while you are frozen, so an odd
/// rung has no "offend again" rule to write. The even rungs carry no effect whatsoever; they are the
/// window in which a repeat costs you more.</para>
///
/// <code>
/// rung  what it is            lasts    runs out into   offend while holding it
/// L1    PETRIFIED              3 min   → L2            (cannot act)
/// L2    remembered             1 h     → clean         → L3
/// L3    PETRIFIED             30 min   → L4            (cannot act)
/// L4    remembered             1 h     → clean         → L5
/// L5    PETRIFIED              2 h     → L6            (cannot act)
/// L6    remembered            24 h     → clean         → L5   ← the cycle: L5 ⇄ L6 forever
/// </code>
///
/// <para>*"u cycle l5&lt;&gt;L6 until u stop for 24h and the start form l1"* — so the ONLY way off the
/// top of the ladder is to let a full 24 hours pass without offending, which drops you to clean and
/// the next offence starts again at L1.</para>
///
/// <para>🔑 <b>PETRIFIED NEEDED NO NEW MACHINERY — IT IS <see cref="SkillEffect.Stun"/> +
/// <c>FreezesHp</c>.</b> "Cannot act" is exactly what Stun already means (<c>Entity.IsStunned</c> →
/// <c>IsActionLocked</c> gates the cast, breaks one in flight, drops the queued and chained skill,
/// and zeroes <c>EffectiveSpeed</c>); "doesn't take any dmg" is exactly what the Immortality Sigil's
/// HP freeze already means (<c>Entity.HpFrozen</c>, checked inside <c>ApplyDamage</c> and
/// <c>HealOne</c>). That also settled the enum problem before it was one: <see cref="SkillEffect"/>
/// has no free bits left, so a `Petrify` flag was never available.</para>
///
/// <para>🔴 <b>UNREMOVABLE, AND THAT IS ENFORCED BY THE ARCHITECTURE, NOT BY A FLAG.</b> His words:
/// *"This curse is unremovable..no cleanse no healers whatever nothing .. It's a punishment."* The
/// truth is <c>Entity.BossJudgmentRung</c> + <c>BossJudgmentUntil</c>; the BUFF is only its visible
/// face, and <c>GameLoopService.TickBossJudgment</c> re-asserts it every second. So death (which
/// clears the buff list), a subclass swap (which also does), a cleanse, a relog or a bug all fail to
/// remove it — removing the buff simply does nothing, because nothing reads the buff.</para>
/// </summary>
public static partial class SkillCatalog
{
    /// <summary>The PETRIFIED rungs (L1/L3/L5) — Stun + HP freeze.</summary>
    public const string BossJudgmentSkill = "boss_judgment";
    /// <summary>The REMEMBERED rungs (L2/L4/L6) — no effect at all, just the escalation window.</summary>
    public const string BossJudgmentMarkSkill = "boss_judgment_mark";

    /// <summary>Both defs share it, so the buff bar and <c>Entity.Petrified</c> have one thing to
    /// look for however far up the ladder someone is.</summary>
    public const string BossJudgmentKey = "boss_judgment";

    private static SkillDef[] BossJudgmentSkills() => new[]
    {
        // ⚠ Neither def carries a duration or a rung: EVERY apply passes `durationOverride` and a
        // `displayName`, because the ladder — not the skill — owns those. Two defs rather than one
        // with six levels only because `Effect` and `FreezesHp` are per-DEF fields, not per-level.
        new SkillDef(BossJudgmentSkill, "Boss's Judgment", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: BossJudgment.Ticks(1), BuffKey: BossJudgmentKey, Rank: 1,
            Category: SkillCategory.Debuff, BuffRow: BuffRow.Debuff,
            FreezesHp: true, Cancellable: false,
            FixedCooldown: true, CountsTowardBuffLimit: false,
            Description: "Petrified by a raid boss you had no business touching. You cannot act, and "
                       + "nothing can damage or heal you. Nothing removes this."),

        new SkillDef(BossJudgmentMarkSkill, "Boss's Judgment", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: BossJudgment.Ticks(2), BuffKey: BossJudgmentKey, Rank: 1,
            Category: SkillCategory.Debuff, BuffRow: BuffRow.Debuff,
            Cancellable: false,
            FixedCooldown: true, CountsTowardBuffLimit: false,
            Description: "A raid boss remembers you. Interfere again before this fades and the next "
                       + "petrifaction is far longer. Nothing removes this."),
    };
}

/// <summary>`BL-98` — the ladder itself, as data. Kept beside the formulas rather than in the server
/// so the numbers can be read (and tested) without a running world.</summary>
public static class BossJudgment
{
    public const int TopRung = 6;

    /// <summary>Odd rungs freeze you. Even rungs only remember you.</summary>
    public static bool IsPetrify(int rung) => rung is 1 or 3 or 5;

    /// <summary>How long a rung lasts, in seconds. His numbers.</summary>
    public static int Seconds(int rung) => rung switch
    {
        1 => 180,      // 3 minutes
        2 => 3600,     // 1 hour
        3 => 1800,     // 30 minutes
        4 => 3600,     // 1 hour   (*"same as L2 just aaply L5 after"*)
        5 => 7200,     // 2 hours
        6 => 86400,    // 24 hours (*"same as l2/4 but for 24h"*)
        _ => 0,
    };

    public static int Ticks(int rung) => Seconds(rung) * GameConstants.TickRate;

    /// <summary>What this rung turns into when it runs out ON ITS OWN. 0 = clean.
    /// A petrifaction always hands over to its memory rung; a memory rung that survives its whole
    /// span un-offended is the end of the ladder.</summary>
    public static int OnExpiry(int rung) => rung switch
    {
        1 => 2,
        3 => 4,
        5 => 6,
        _ => 0,        // 2/4/6 → clean; *"until u stop for 24h and the start form l1"*
    };

    /// <summary>What a fresh offence costs someone holding <paramref name="rung"/> (0 = clean).
    /// ⚠ 1/3/5 return themselves: you cannot act while petrified, so this can only be reached by a
    /// same-tick double-charge (an AoE that reaches two participants), and it must not escalate.</summary>
    public static int OnOffence(int rung) => rung switch
    {
        0 => 1,
        2 => 3,
        4 => 5,
        6 => 5,        // *"Each L6 removes L6 and applies L5"* — the top of the ladder is a cycle
        _ => rung,
    };

    /// <summary>The rung's name as a player sees it: "Boss's Judgment L3 (petrified)".</summary>
    public static string Label(int rung) =>
        $"Boss's Judgment L{rung}" + (IsPetrify(rung) ? " (petrified)" : "");

    /// <summary>A rung's span in words, for the system line that announces it.</summary>
    public static string Spoken(int rung)
    {
        int s = Seconds(rung);
        return s >= 3600 ? $"{s / 3600} hour{(s / 3600 == 1 ? "" : "s")}" : $"{s / 60} minutes";
    }
}
