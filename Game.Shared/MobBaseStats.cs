namespace Game.Shared;

/// <summary>
/// Mob BASE stat curve by level — the "level modifier" component of the mob stat formula
/// (final = baseCurve(level) × conMod × ∏passives). Sourced from
/// <c>docs/data/mobs/mob_base_stats.csv</c> (the NORMAL, ×1-passive progression). A champion /
/// outlier mob (e.g. a mini-boss with ~3× HP at its level) is the SAME curve × a Max-HP /
/// P.Def PASSIVE (a MobMod / mastery), never a separate curve — so "assume all monsters are
/// ×1" reproduces the CSV exactly and outliers layer a passive on top.
///
/// HP / P.Def / M.Def are FORMULAS (see the block below them) — measured off the retail L2
/// mob table in 2026-07-14 and re-derived, because the old authored columns were the wrong
/// SHAPE. MP / P.Atk / M.Atk stay a hand-authored table, linearly interpolated between the
/// listed levels and clamped outside [1, 85]; they were measured to already track L2.
///
/// The CSV is regenerated to match, so it stays a faithful dump of what the code does.
/// </summary>
public static class MobBaseStats
{
    // level → (MP, P.Atk, M.Atk). These three were MEASURED against the retail L2 mob table
    // and already track it within ~30%, so they stay hand-authored here. HP / P.Def / M.Def
    // were NOT — they are now formulas below (see the comment block).
    private static readonly (int Lvl, int Mp, int PAtk, int MAtk)[] Curve =
    {        (1,   12,    4,     2),
        (4,   22,    7,     4),
        (8,   35,    14,    9),
        (10,  40,    18,    12),
        (12,  52,    23,    15),
        (14,  60,    28,    19),
        (16,  75,    34,    23),
        (18,  90,    41,    27),
        (20,  112,   48,    32),
        (22,  130,   56,    38),
        (24,  155,   65,    44),
        (26,  180,   75,    51),
        (28,  210,   86,    59),
        (30,  240,   98,    67),
        (32,  280,   112,   77),
        (34,  320,   127,   87),
        (35,  340,   135,   93),
        (37,  390,   152,   105),
        (39,  440,   171,   118),
        (40,  440,   171,   118),
        (42,  540,   203,   141),
        (44,  600,   228,   158),
        (45,  630,   241,   167),
        (47,  700,   269,   187),
        (48,  730,   284,   197),
        (50,  810,   316,   220),
        (51,  850,   333,   232),
        (53,  940,   370,   258),
        (55,  1040,  410,   286),
        (56,  1090,  431,   301),
        (57,  1140,  454,   317),
        (58,  1190,  478,   334),
        (60,  1310,  529,   370),
        (61,  1370,  556,   389),
        (62,  1430,  584,   409),
        (63,  1500,  613,   429),
        (64,  1560,  644,   451),
        (65,  1630,  675,   473),
        (66,  1700,  708,   496),
        (67,  1780,  742,   520),
        (68,  1850,  777,   545),
        (69,  1930,  814,   571),
        (70,  2010,  852,   598),
        (71,  2090,  892,   626),
        (72,  2180,  933,   655),
        (73,  2260,  975,   685),
        (74,  2350,  1020,  716),
        (75,  2440,  1065,  748),
        (76,  2540,  1113,  782),
        (77,  2630,  1162,  817),
        (78,  2730,  1213,  853),
        (79,  2830,  1266,  890),
        (80,  2940,  1321,  929),
        (81,  3050,  1378,  969),
        (82,  3160,  1437,  1011),
        (83,  3280,  1498,  1054),
        (84,  3400,  1561,  1099),
        (85,  3520,  1627,  1145),

    };

    // ---- HP and DEFENCE are FORMULAS, fitted to the retail L2 mob table (2026-07-14) ----
    //
    // The old authored HP/P.Def/M.Def columns were all QUADRATIC in level. Measured against
    // real L2 mobs (Keltir L1, Grizzly L17, Ghoul L32, Grandis L40, Invader Shaman L63,
    // Tracker Howl L81) that turned out to be wrong in two different directions at once:
    //
    //   * DEFENCE in L2 is LINEAR in level (P.Def ≈ 4.2·lvl, M.Def ≈ 3·lvl, floored at L1).
    //     A quadratic curve crosses the real one around level 43 — so our low-level mobs were
    //     paper (M.Def 5 at L1 where L2 has 30: a level-21 mage nuked a level-24 mob for 2k)
    //     and our high-level mobs were walls (M.Def 448 at L80 where L2 has ~253). One bug,
    //     both ends.
    //   * HP was 2.8-4.5x too high above level 45 (15,420 at L80 where L2's Tracker Howl has
    //     ~5,500), which is what made the endgame grind balloon to ~79 casts a kill.
    //
    // Mob P.Atk/M.Atk/MP were measured to already track L2 within ~30%, so they stay on the
    // authored table above.
    //
    // Fat mobs are NOT a separate curve: L2 makes a specific mob tanky with an "HP Increase
    // (2x/3x)" PASSIVE on top of this lean base (a level-85 Drake Warrior = 18.6k base HP x
    // ~2.5). That is exactly our MobMod / MobMasteries layer — so keep the curve lean and let
    // elites and bosses buy their bulk with passives.

    /// <summary>Base max HP. Lean quadratic (a normal L80 mob is ~5.2k, an L1 mob ~41);
    /// elites/bosses multiply this with an HP passive rather than riding a separate curve.</summary>
    public static int Hp(int level) => 40 + (int)(0.8f * level * level);

    /// <summary>Base physical defence — LINEAR in level, as in retail L2 (≈4.2·lvl).</summary>
    public static int PDef(int level) => Math.Max(44, (int)(4.2f * level));

    /// <summary>Base magic defence — LINEAR in level, as in retail L2 (≈3·lvl).</summary>
    public static int MDef(int level) => Math.Max(30, (int)(3.0f * level));

    public static int Mp(int level)   => Interp(level, r => r.Mp);
    public static int PAtk(int level) => Interp(level, r => r.PAtk);
    public static int MAtk(int level) => Interp(level, r => r.MAtk);

    private static int Interp(int level, Func<(int Lvl, int Mp, int PAtk, int MAtk), int> sel)
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
