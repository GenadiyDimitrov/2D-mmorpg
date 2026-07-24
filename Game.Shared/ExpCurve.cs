namespace Game.Shared;

/// <summary>
/// The progression curve: how much EXP a level costs, how much a mob pays, the SP ratio, the
/// level-difference penalty and the party-size bonus. One place, so balance work never has to hunt.
///
/// Reference table + the reasoning behind every number: <c>docs/balance/ExpCurve.md</c> (and its
/// TAB-separated twin <c>ExpCurve.csv</c>). Regenerate those if you touch anything here.
///
/// **The player curve is the real Lineage 2 table, deliberately.** It is *not* a formula: its own
/// shape is a power law (~8.492·L^3.2891) only up to level 50, after which SEVEN multiplicative
/// "walls" at 51/56/61/66/72/77/80 stack to ~52x by level 85. No smooth function reproduces a
/// x3.57 step from 79 to 80, so the cumulative totals are stored outright. **The walls ARE the
/// endgame** — levels 79-85 are ~500 000 of the ~632 000 mobs it takes to reach 86. If the endgame
/// ever needs shortening, soften the walls here; never touch the base curves.
/// </summary>
public static class ExpCurve
{
    /// <summary>Highest level the AUTHORED table covers. Index 101 exists so <see cref="ExpToNext"/>
    /// works at 100. Real data all the way — nothing here is extrapolated.
    /// <c>GameConstants.MaxPlayerLevel</c> (90) sits comfortably inside this, and the headroom to 100
    /// is here so the cap can be raised later without touching the curve. Totals stay tiny against
    /// <c>long</c>: level 101 is 1.06e13, about 873 000x under long.MaxValue.</summary>
    public const int MaxLevel = 100;

    /// <summary>Growth ratio used past <see cref="MaxLevel"/> — only reachable if the cap is ever
    /// pushed beyond 100. Taken from the last real step (100→101 over 99→100).</summary>
    private const double TailRatio = 1.60;

    /// <summary>CUMULATIVE exp at the start of each level; index = level, [0] unused. Verified
    /// self-consistent: every entry equals the previous plus that level's cost. Level 86 comes from
    /// 4Game's 85→86 (5 977 044 329) spliced onto the level-85 total — the owner's choice, because
    /// 4Game's own 85 is only ~5.0bn and joining there would jump x8.6 in a single level.</summary>
    private static readonly long[] Cumulative =
    {
        0L,                                   // [0] unused, so index == level
        0L, 68L, 363L, 1168L, 2884L, 6038L,   // 1-6
        11287L, 19423L, 31378L, 48229L, 71202L, 101677L,   // 7-12
        141193L, 191454L, 254330L, 331867L, 426288L, 540000L,   // 13-18
        675596L, 835862L, 1023784L, 1242546L, 1495543L, 1786379L,   // 19-24
        2118876L, 2497077L, 2925250L, 3407897L, 3949754L, 4555796L,   // 25-30
        5231246L, 5981576L, 6812513L, 7730044L, 8740422L, 9850166L,   // 31-36
        11066072L, 12395215L, 13844951L, 15422929L, 17137087L, 18995665L,   // 37-42
        21007203L, 23180550L, 25524868L, 28049635L, 30764654L, 33680052L,   // 43-48
        36806289L, 40154162L, 45525133L, 51262490L, 57383988L, 63907911L,   // 49-54
        70853089L, 80700831L, 91162654L, 102265881L, 114038596L, 126509653L,   // 55-60
        146308200L, 167244337L, 189364894L, 212717908L, 237352644L, 271975263L,   // 61-66
        308443198L, 346827154L, 387199547L, 429634523L, 474207979L, 532694979L,   // 67-72
        606322775L, 696381369L, 804225364L, 931275828L, 1151275834L, 1511275834L,   // 73-78
        2099275834L, 4200000000L, 6300000000L, 8820000000L, 11844000000L, 15472800000L,   // 79-84
        19827360000L,   // 85  <- end of the masterwork source
        // 86-101 splice on 4Game's per-level costs (the owner's choice: 4Game's own level 85 is only
        // ~5.0bn, so joining there would jump x8.6 in one level, where joining here is a smooth x1.37).
        // ⚠ 4Game lists levels 88 and 89 TRANSPOSED — as published, 89 costs 10 839 148 748 against
        // 88's 14 341 344 002, i.e. level 89 is CHEAPER than 88 (x0.76). Swapped back into order here,
        // which makes the run 87->90 read x1.32, x1.32, x1.35 — smooth, and unmistakably the intent.
        // Revert the two if you ever want the published numbers verbatim.
        25804404329L, 34011929300L, 44851078048L, 59192422050L,   // 86-89
        78571273393L, 103507858361L, 135043608016L, 171952937250L,   // 90-93
        223333195296L, 281588413296L, 392658843255L, 607610448273L,   // 94-97
        1031062703942L, 1971736012292L, 5275490484336L, 10561497639606L,   // 98-101
    };

    /// <summary>EXP to go from <paramref name="level"/> to the next one. Always positive (the old
    /// 25·L² was too), so callers that treat 0 as "max level" keep behaving as before.
    /// Past <see cref="MaxLevel"/> the value is extrapolated at <see cref="TailRatio"/> rather than
    /// returning 0 — a zero here would make the level-up loop in AwardExp spin forever.</summary>
    public static long ExpToNext(int level)
    {
        if (level < 1) level = 1;
        if (level < MaxLevel) return Cumulative[level + 1] - Cumulative[level];

        long lastStep = Cumulative[MaxLevel + 1] - Cumulative[MaxLevel];   // the real 85→86 cost
        if (level == MaxLevel) return lastStep;
        return (long)(lastStep * Math.Pow(TailRatio, level - MaxLevel));
    }

    /// <summary>Total EXP banked by the time you reach <paramref name="level"/>.</summary>
    public static long TotalExpAt(int level) =>
        Cumulative[Math.Clamp(level, 1, MaxLevel + 1)];

    // ----- Mob reward ---------------------------------------------------------------------------

    // Fitted to the owner's database reading: L50 ~= 8.5k, L75 ~= 30k, L85 ~= 45-50k. The exponent
    // 3.2427 lands remarkably close to the player curve's 3.2891, which is why kills-per-level sits
    // FLAT at ~415-420 through the whole mid game — the two curves grow together and only the walls
    // pull them apart.
    private const double MobCoefficient = 0.0263140;
    private const double MobExponent    = 3.2427;

    /// <summary>Level at/above which the plain power law is used. Below it the curve is interpolated
    /// through <see cref="LowAnchors"/>, because extrapolating the power law down to level 1 collapses
    /// to ~1 exp per mob — which cost 295/805/858 mobs for levels 2/3/4, making the first ten minutes
    /// of a new character the worst grind in the first fifty levels.</summary>
    private const int PowerLawFrom = 30;

    /// <summary>(level, exp) anchors for the early game, chosen so kills-to-level land on the owner's
    /// targets: 1-2-5 mobs for the opening levels, ~20 by level 10, ~120 by level 20. They blend into
    /// the power law exactly at <see cref="PowerLawFrom"/>. **These are the early-game tuning knob** —
    /// moving them changes nothing above level 30.</summary>
    private static readonly (int Level, double Exp)[] LowAnchors =
    {
        (1, 68d), (2, 148d), (3, 268d), (5, 631d), (10, 1149d), (20, 1566d),
    };

    /// <summary>Base EXP a normal mob of this level pays, before toughness, level gap, party and the
    /// random roll. Monotonically increasing across the whole range.</summary>
    public static long MobExpReward(int mobLevel)
    {
        if (mobLevel < 1) mobLevel = 1;
        double v = mobLevel >= PowerLawFrom
            ? PowerLawMob(mobLevel)
            : InterpolateGeometric(LowAnchors, mobLevel, PowerLawFrom, PowerLawMob(PowerLawFrom));
        return Math.Max(1L, (long)Math.Round(v));
    }

    private static double PowerLawMob(int level) =>
        MobCoefficient * Math.Pow(level, MobExponent);

    // ----- Skill points -------------------------------------------------------------------------

    /// <summary>SP as a fraction of the same kill's EXP. The owner's anchors: ~1.0 at the start,
    /// 0.15 at 50, 0.10 at 70, 0.05 at 85 — SP matters most while you are still buying your kit.
    /// ⚠ These four points do NOT sit on any single power law or exponential (fitting 50+85 predicts
    /// 0.075 at 70; fitting 50+70 predicts 0.079 at 85), so they are interpolated rather than fitted.
    /// A consequence worth knowing: <see cref="MobSpReward"/> PEAKS near level 68 and then declines,
    /// because the ratio falls faster than mob exp rises.</summary>
    private static readonly (int Level, double Ratio)[] SpAnchors =
    {
        (1, 1.00d), (50, 0.15d), (70, 0.10d), (85, 0.05d),
    };

    /// <summary>SP-to-EXP ratio at this level.</summary>
    public static float SpRatio(int level) =>
        (float)InterpolateGeometric(SpAnchors, Math.Max(1, level), SpAnchors[^1].Level, SpAnchors[^1].Ratio);

    /// <summary>Base SP a normal mob of this level pays.</summary>
    public static long MobSpReward(int mobLevel) =>
        Math.Max(1L, (long)Math.Round(MobExpReward(mobLevel) * SpRatio(mobLevel)));

    // ----- Level difference ---------------------------------------------------------------------

    /// <summary>Free band: within this many levels of the mob you keep 100%.</summary>
    public const int GapFree = 5;
    /// <summary>At or beyond this gap you earn NOTHING.</summary>
    public const int GapZero = 13;
    /// <summary>Compounding loss per level once past <see cref="GapFree"/>.</summary>
    private const double GapFalloff = 0.85;

    /// <summary>EXP/DROP multiplier for a level difference. **Symmetric on purpose** (owner): fighting
    /// UP is penalised as well as down. Killing higher mobs is already slower, and the up-penalty is
    /// what stops someone kill-stealing a level-78 mob with a level-1 bow and gaining twenty levels on
    /// one lucky shot. It is also the anti-powerlevel mechanism: at a 12-level gap you keep ~32%, so
    /// farming far above your level nets about what your own band would pay anyway.
    /// (Real L2 uses (5/6)^(gap-5) and only penalises downward; ours is deliberately different.)</summary>
    public static float LevelGapMultiplier(int levelDifference)
    {
        int gap = Math.Abs(levelDifference);
        if (gap <= GapFree) return 1f;
        if (gap >= GapZero) return 0f;
        return (float)Math.Pow(GapFalloff, gap - GapFree);
    }

    // ----- Party --------------------------------------------------------------------------------

    /// <summary>Multiplier applied to the party's EXP pot for a group of this size. Calibrated by the
    /// owner so a WELL-BUILT party wins and an idle-alt party loses: two players kill ~2x as fast and
    /// gain; six must kill 3x as fast, i.e. 3 DD + 3 support merely to break even, so swapping a healer
    /// for a DD pulls ahead; nine need 4x. Parking eight do-nothing alts kills at solo speed for a
    /// quarter of the exp. **The per-head share falling as the party grows is the POINT** — do not
    /// "fix" it.</summary>
    public static float PartyBonus(int memberCount)
    {
        if (memberCount <= 1) return 1f;
        int n = Math.Min(memberCount, 9);
        return n <= 6 ? 1f + 0.2f * (n - 1)   // 2->1.2 .. 6->2.0
                      : 2f + 0.1f * (n - 6);  // 7->2.1 .. 9->2.3
    }

    // ----- Randomness ---------------------------------------------------------------------------

    /// <summary>Half-width of the random swing on a kill's reward (±20%).</summary>
    public const double RandomSpread = 0.20;

    /// <summary>Roll the ±20% reward multiplier. Applied to the FINAL award (owner: "if I will get
    /// 20000 exp the mob can give 16-24k"), and applied to EXP and SP together from ONE roll so the
    /// ratio does not drift. In a party the single roll is shared by every member — rolling per member
    /// would show 16k to one player and 24k to another off the same corpse, which reads as a bug.
    ///
    /// No floor and no exemption at low level, deliberately (owner): at level 1 a sub-1.0 roll costs a
    /// whole extra mob while a high roll saves nothing, so level 1 averages 1.5 kills instead of 1 —
    /// judged not a problem, since reaching level 10 costs 87 mobs (~70-104 with the roll) and the gap
    /// thins to nothing as the counts grow. **Do not reintroduce a floor.**</summary>
    public static double RandomFactor(Random rng) =>
        1.0 - RandomSpread + 2.0 * RandomSpread * rng.NextDouble();

    // ----- Shared helper ------------------------------------------------------------------------

    /// <summary>Geometric (log-linear) interpolation across ascending anchors — the natural shape for
    /// a quantity that grows/decays multiplicatively. Past the last anchor the caller's tail value is
    /// used.</summary>
    private static double InterpolateGeometric(
        (int Level, double Value)[] anchors, int level, int tailLevel, double tailValue)
    {
        if (level <= anchors[0].Level) return anchors[0].Value;

        for (int i = 0; i < anchors.Length - 1; i++)
        {
            var (l0, v0) = anchors[i];
            var (l1, v1) = anchors[i + 1];
            if (level <= l1)
                return Lerp(l0, v0, l1, v1, level);
        }

        var (lastL, lastV) = anchors[^1];
        return level >= tailLevel ? tailValue : Lerp(lastL, lastV, tailLevel, tailValue, level);

        static double Lerp(int l0, double v0, int l1, double v1, int at)
        {
            double t = (at - l0) / (double)(l1 - l0);
            return Math.Exp(Math.Log(v0) + t * (Math.Log(v1) - Math.Log(v0)));
        }
    }
}
