namespace Game.Shared;

/// <summary>`BL-277` part 1 — **WAYFARER'S FAVOR**, the 0-20,000 catch-up gauge (his "Vitality", renamed
/// 2026-09-23). Full design and every ruling: <c>docs/design/Rework-2026-09-23.md</c> §3.
///
/// <para>🔑 **Catch-up, not the pace** (his ruling): *"the current pace is the pace .. The vitality only
/// helps someone not so active not to be so far behind."* So it FILLS while you are away (offline, or
/// idle in a city) and DRAINS while you farm, and a full gauge lasts <see cref="HoursPerGauge"/> hours of
/// farming at EVERY level — which is why the drain divides by the same-level normal mob's EXP and the
/// measured kills/h, never by IG's <c>L²·10</c> (that one swings ~2000× across our curve).</para>
///
/// <para>Shared, because the client prints the stage and the bonus and must never re-derive them.</para></summary>
public static class WayfarerFavor
{
    /// <summary>The gauge's ceiling. Offline credit, the town ticker and (later) the boss grant all clamp here.</summary>
    public const int MaxPoints = 20000;

    /// <summary>The TOP of each stage, cumulative — his table verbatim (500 / 4,500 / 1,000 / 4,000 /
    /// 1,500 / 3,500 / 2,000 / 3,000 points wide). Stage N pays <see cref="BonusPerStage"/> × N.</summary>
    public static readonly int[] StageTops = { 500, 5000, 6000, 10000, 11500, 15000, 17000, 20000 };

    /// <summary>+50% EXP/SP per stage: stage 8 (a full gauge) = +400%.</summary>
    public const float BonusPerStage = 0.5f;

    /// <summary>`H` — the hours of farming a FULL gauge lasts, at every level. His ruling (fourth round):
    /// 2, not 3 or 4 — the conservative value, and the one where going offline and playing later can
    /// never beat farming the same wall-clock straight through.</summary>
    public const int HoursPerGauge = 2;

    /// <summary>Default points per whole minute away (offline) or idle in a city — 20,000 in 8 h 20 min.
    /// The LIVE value is <see cref="RateConfig.FavorPerMinute"/> (the admin knob).</summary>
    public const int DefaultPerMinute = 40;

    /// <summary>The 0-8 stage for a point count. Any point at all is stage 1: the gauge pays the moment it
    /// holds something, and 0 is the only stage-0 value.</summary>
    public static int Stage(double points)
    {
        if (points <= 0) return 0;
        for (int i = 0; i < StageTops.Length; i++)
            if (points <= StageTops[i]) return i + 1;
        return StageTops.Length;
    }

    /// <summary>The EXP/SP BONUS the gauge pays right now (0 … 4.0). ADDITIVE with the other personal
    /// bonuses (charisma today, the Blessing later) — his own arithmetic: *"100 base % + 400% + 50% =
    /// x5.5"*. The server rate and the runes multiply the result, as they always have.</summary>
    public static float Bonus(double points) => Stage(points) * BonusPerStage;

    /// <summary>What a kill that paid <paramref name="baseExpShare"/> of BASE EXP (this member's own share,
    /// after the party split and the level gap, before any rate, rune or bonus) costs a level-
    /// <paramref name="level"/> character:
    /// <code>drain = 20000 × (share ÷ sameLevelNormalExp(L)) ÷ (killsPerHour(L) × H)</code>
    /// A same-level normal kill at ~70 kills/h is ~143 points, so a full gauge is ~140 kills = H hours.
    /// Elites and x2/x3 zones pay more EXP and so drain faster — his shape, kept.</summary>
    public static double DrainPerKill(double baseExpShare, int level)
    {
        if (baseExpShare <= 0) return 0;
        double normal = ExpCurve.MobExpReward(level);
        return MaxPoints * (baseExpShare / normal) / (KillsPerHour(level) * (double)HoursPerGauge);
    }

    /// <summary>Points earned by <paramref name="minutes"/> away at <paramref name="perMinute"/>, clamped
    /// so the result on top of <paramref name="current"/> never passes <see cref="MaxPoints"/>.</summary>
    public static double Credit(double current, double minutes, int perMinute) =>
        Math.Min(MaxPoints, Math.Max(0, current) + Math.Max(0, minutes) * Math.Max(0, perMinute));

    /// <summary>The farm's kills per hour at this level — the clock the drain divides by. Levels past the
    /// table read its last row.</summary>
    public static int KillsPerHour(int level) =>
        KillsPerHourTable[Math.Clamp(level, 1, KillsPerHourTable.Length) - 1];

    // 🔑 AUTHORED, NOT COMPUTED LIVE (his ruling, design doc §3 point 3): a combat change must never
    // silently move the Favor. Read off BalanceMatrix's M1 clock — the one `--craft-cost` uses — with
    //     dotnet run --project tools/BalanceMatrix -- --favor-kph
    // and re-pasted whenever the farm pace is re-measured. 39.3 s loop overhead + same-level TTK
    // (2026-09-23): walking dominates the farm, so the clock barely moves across the game.
    private static readonly int[] KillsPerHourTable =
    {
         90,  89,  89,  89,  89,  89,  90,  89,  89,  88,   // 1-10
         88,  87,  86,  86,  85,  85,  84,  82,  81,  89,   // 11-20
         88,  88,  88,  87,  87,  87,  86,  86,  85,  85,   // 21-30
         84,  84,  84,  83,  82,  82,  81,  81,  80,  83,   // 31-40
         83,  82,  82,  81,  81,  80,  79,  79,  78,  77,   // 41-50
         77,  79,  79,  78,  77,  77,  76,  75,  75,  74,   // 51-60
         74,  74,  73,  72,  71,  71,  70,  69,  68,  68,   // 61-70
         67,  66,  65,  64,  63,  68,  67,  66,  65,  72,   // 71-80
         72,  71,  71,  70,  69,  69,  68,  67,  67,  66,   // 81-90
         65,  65,  64,  63,  63,  62,  61,  60,  60,  59,   // 91-100
    };
}

/// <summary>`BL-277` part 2 — **WAYFARER'S BLESSING**, the 0-100% gauge that fills while you FIGHT and,
/// when full, fires a 3-minute +100% EXP/SP on its own. Full rulings: <c>docs/design/Rework-2026-09-23.md</c>
/// §3 points 2 and 10 (and his note's "Bonus while actively fighting" block, verbatim in the appendix).
///
/// <para>🔑 **Every source is multiplied by ONE fill rate, applied once, where the gauge is added to**
/// (his fifth-round ruling: *"OK mobs 0.1% x1"* + the modifier multiplies kills, combat minutes, stage
/// drops AND the level-up bump). The rate reads ×1 until charisma (`BL-283`) and the booster rune
/// (`BL-277` part 3) exist.</para>
///
/// <para>Shared, because the client prints the gauge and must never re-derive the numbers.</para></summary>
public static class WayfarerBlessing
{
    /// <summary>The gauge's top. Reaching it fires the Blessing.</summary>
    public const double MaxPercent = 100;

    /// <summary>A normal kill that paid EXP (never a boss kill — that one only GRANTS Favor).</summary>
    public const double PerKill = 0.1;

    /// <summary>A minute in combat — accrued by the second (<c>PerCombatMinute / 60</c> each).</summary>
    public const double PerCombatMinute = 1;

    /// <summary>Each Favor stage a kill's drain carries you down through.</summary>
    public const double PerFavorStageLost = 8;

    /// <summary>Each level gained.</summary>
    public const double PerLevelUp = 30;

    /// <summary>How long a fired Blessing lasts: 3 minutes of time IN THE WORLD (the clock does not run
    /// while logged out — *"not offline"*).</summary>
    public const int DurationSeconds = 180;

    /// <summary>+100% EXP/SP while active — ADDED to the other personal bonuses (charisma, Favor), his
    /// *"the SP/EXP start to show x3.5"* on a ×2.5 Favor rate.</summary>
    public const float Bonus = 1.0f;
}
