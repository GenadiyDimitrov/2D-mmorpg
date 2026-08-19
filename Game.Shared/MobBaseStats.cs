namespace Game.Shared;

/// <summary>
/// Mob BASE stat curve by level — the "level modifier" component of the mob stat formula
/// (final = baseCurve(level) × ∏passives). Dumped to <c>docs/data/mobs/mob_base_stats.csv</c>
/// by <c>dotnet run --project tools/BalanceMatrix -- --dump-mob-csv</c>, which regenerates that
/// file FROM this class so the two can never drift. A champion / outlier mob (a mini-boss with
/// ~3× HP at its level) is the SAME curve × a Max-HP / P.Def PASSIVE (a MobMod / mastery), never
/// a separate curve — so "assume all monsters are ×1" reproduces the CSV exactly and outliers
/// layer a passive on top.
///
/// HP is a lean quadratic; P.Def, M.Def, P.Atk and M.Atk are ONE FAMILY of smooth curves,
/// <c>a·(level + shift)^k</c>, fitted to IG (see the block below them). MP is the only thing left
/// on a hand-authored table, linearly interpolated between the listed levels and clamped outside
/// [1, 85].
/// </summary>
public static class MobBaseStats
{
    // level → MP. The last hand-authored column: MP is not a combat number (nothing a player
    // meets is decided by it) and has never been measured against anything, so it stays a table.
    // P.Atk / M.Atk used to live here as two more columns; they are formulas now — see below.
    private static readonly (int Lvl, int Mp)[] Curve =
    {
        (1,   12), (4,   22), (8,   35), (10,  40), (12,  52), (14,  60), (16,  75), (18,  90),
        (20,  112), (22,  130), (24,  155), (26,  180), (28,  210), (30,  240), (32,  280),
        (34,  320), (35,  340), (37,  390), (39,  440), (40,  440), (42,  540), (44,  600),
        (45,  630), (47,  700), (48,  730), (50,  810), (51,  850), (53,  940), (55,  1040),
        (56,  1090), (57,  1140), (58,  1190), (60,  1310), (61,  1370), (62,  1430), (63,  1500),
        (64,  1560), (65,  1630), (66,  1700), (67,  1780), (68,  1850), (69,  1930), (70,  2010),
        (71,  2090), (72,  2180), (73,  2260), (74,  2350), (75,  2440), (76,  2540), (77,  2630),
        (78,  2730), (79,  2830), (80,  2940), (81,  3050), (82,  3160), (83,  3280), (84,  3400),
        (85,  3520),
    };

    // ---- DEFENCE and ATTACK are ONE SMOOTH FAMILY, refitted to IG on 2026-08-19 (BL-78) ----
    //
    // ⚠ WHY THE OLD NUMBERS WERE NOT WRONG WHEN THEY WERE WRITTEN. The 2026-07-14 fit that this
    // replaces read, correctly for its data: "DEFENCE in IG is LINEAR in level (P.Def ≈ 4.2·lvl,
    // M.Def ≈ 3·lvl, floored at L1)", measured off six named creatures — Keltir L1, Grizzly L17,
    // Ghoul L32, Grandis L40, Invader Shaman L63, Tracker Howl L81. Every one of those readings
    // came from an OLDER CHRONICLE of IG. The public databases do not agree with each other and
    // not by a little: the same creature id 22225 at level 80 reads 3,290 HP / 1,600 P.Atk /
    // 341 P.Def in the old-chronicle databases and 13,763 / 4,514 / 1,053 in the current one —
    // ~3× on defence and attack. So the old table was a faithful fit to a version of IG that has
    // since been re-scaled, which is exactly why our creatures felt like paper against the game
    // he is actually comparing us to. Both halves of that history are kept here on purpose:
    // whoever refits this next needs to know that "measured against IG" is not one number.
    // Full measurement, method and per-level table: docs/balance/MobCurveVsIG.md.
    //
    // THE NEW FIT. 2,831 creatures (every l2elo.com monster at levels 1-83), each read with its
    // NPC SKILL LIST. IG authors a creature exactly the way MobMod / MobMasteries does — a shared
    // base curve plus graded passives — and it says the grade out loud: "Average P. Def. Lv11",
    // "Strong P. Atk. Lv15", "HP Increase (3x)". So the base curve is not a median over a mixed
    // roster: it is the median over the creatures IG ITSELF TAGS AS AVERAGE, which is the ×1 rung.
    // Measured against that same data, the tag ladder is Weak ×0.82 / Average ×1.00 / Strong ×1.21
    // / Very Strong ×1.61 — i.e. MobMasteries.DefTable's own rungs (0.83 / 1.00 / 1.21 / 1.61).
    // The passive layer needed no change; only the curve under it did.
    //
    // ONE FUNCTION, NOT A TABLE — his binding constraint, 2026-08-19: "everything above lvl 20
    // should walk normal curve because there are bosses/instances that will derive from it (with
    // passives)". A boss is base × a passive, so a kink in the base is inherited and multiplied by
    // every derived creature. a·(level + shift)^k is strictly increasing and infinitely smooth for
    // positive a/shift/k, at every level, with no floor and no piecewise band — the old
    // Math.Max(44, …) floor is gone with them, and so is the interpolated P.Atk / M.Atk table
    // (piecewise-linear = a slope change at each of its 57 nodes).
    //
    // Accuracy against the measured IG series: P.Def and M.Def within 4% at every sampled level
    // from 1 to 70 (worst 13% at 30, 9% at 65); P.Atk within 8% from 25 to 70; M.Atk within 11%.
    // The fit runs ~10% hot above 70, which is the price of one curve also fitting 45-70 — the
    // levels ending in 4 and 9 are excluded from the fit because IG's raid bosses sit there and
    // their minions crowd the roster.
    //
    // ⚠ HP IS DELIBERATELY UNTOUCHED. Our base HP shape measures 0.87 → 1.08 of IG's from 40 up
    // and is the one thing the old fit got right. His "the 80 mobs should have 15k not 5" is real
    // but it is NOT this curve: 77% of IG creatures are tagged HP Increase (1x) and 23% carry
    // ×2-×5, so 4,298 base at 76 × 3 = 12,894 and × 5 = 21,490. The bulk is bought by the
    // multiplier layer (MobMod.Hp), which already exists and already works — it is authoring that
    // is owed, not a curve. Below level 20 our HP is ~0.5× IG's; he ruled that acceptable because
    // levelling is fast there.

    /// <summary>Base max HP. Lean quadratic (a normal L80 mob is ~5.2k, an L1 mob ~40);
    /// elites/bosses multiply this with an HP passive rather than riding a separate curve.</summary>
    public static int Hp(int level) => 40 + (int)(0.8f * level * level);

    /// <summary>Base physical defence. IG "Average P. Def." tier: 39 at 1, 102 at 20, 214 at 40,
    /// 385 at 60, 624 at 80 — roughly a doubling of the old curve at the top, level with it at 1.</summary>
    public static int PDef(int level) => (int)(0.00113f * MathF.Pow(level + 44f, 2.743f));

    /// <summary>Base magic defence. IG "Average M. Def." tier: 30 at 1, 82 at 20, 174 at 40,
    /// 311 at 60, 499 at 80. Tracks P.Def at ~0.8× — IG's own ratio, not an assumption.</summary>
    public static int MDef(int level) => (int)(0.0027f * MathF.Pow(level + 38f, 2.542f));

    /// <summary>Base physical attack. IG "Average P. Atk." tier: 8 at 1, 63 at 20, 283 at 40,
    /// 874 at 60, 2,152 at 80 — ~1.65× the old table across the whole midgame and endgame, which
    /// was the flattest and most consistent deficit in the measurement.</summary>
    public static int PAtk(int level) => (int)(1.12e-6f * MathF.Pow(level + 31f, 4.539f));

    /// <summary>Base magic attack. IG "Average M. Atk." tier: 3 at 1, 30 at 20, 146 at 40,
    /// 487 at 60, 1,277 at 80. It rises far less than P.Atk (×1.37 at the top, ×0.93 at 20)
    /// because the old table already sat close to IG here — M.Atk is the one attack column that
    /// was never badly out.</summary>
    public static int MAtk(int level) => (int)(1.14e-7f * MathF.Pow(level + 32f, 4.904f));

    public static int Mp(int level) => Interp(level, r => r.Mp);

    private static int Interp(int level, Func<(int Lvl, int Mp), int> sel)
    {
        if (level <= Curve[0].Lvl) return sel(Curve[0]);
        if (level >= Curve[^1].Lvl) return sel(Curve[^1]);
        for (int i = 1; i < Curve.Length; i++)
        {
            if (level <= Curve[i].Lvl)
            {
                var a = Curve[i - 1];
                var b = Curve[i];
                float t = (level - a.Lvl) / (float)(b.Lvl - a.Lvl);
                return (int)(sel(a) + (sel(b) - sel(a)) * t);
            }
        }
        return sel(Curve[^1]);
    }
}
