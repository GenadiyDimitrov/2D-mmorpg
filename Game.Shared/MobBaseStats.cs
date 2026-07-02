namespace Game.Shared;

/// <summary>
/// Mob BASE stat curve by level — the "level modifier" component of the mob stat formula
/// (final = baseCurve(level) × conMod × ∏passives). Sourced from
/// <c>docs/mobs/mob_base_stats.csv</c> (the NORMAL, ×1-passive progression). A champion /
/// outlier mob (e.g. a mini-boss with ~3× HP at its level) is the SAME curve × a Max-HP /
/// P.Def PASSIVE (a MobMod / mastery), never a separate curve — so "assume all monsters are
/// ×1" reproduces the CSV exactly and outliers layer a passive on top.
///
/// Values are linearly interpolated between authored levels and clamped outside [1, 85].
/// This mirrors the game's existing table-driven modifiers (StatCalculator.ConCurve /
/// MenCurve): the curve isn't a clean polynomial (the L2 progression is hand-tuned), so the
/// level term is a tuned table rather than a formula, while CON and passives stay as the
/// multiplicative terms the owner specified.
/// </summary>
public static class MobBaseStats
{
    // level → (HP, MP, P.Def, M.Def, P.Atk, M.Atk). The normal per-level progression; the
    // L40 row uses the standard 1980 HP (the Rift Portling outlier is curve × HP/def passive).
    private static readonly (int Lvl, int Hp, int Mp, int PDef, int MDef, int PAtk, int MAtk)[] Curve =
    {
        (1,    42,   12,   8,   5,    4,    2),
        (4,    68,   22,   12,  9,    7,    4),
        (8,    110,  35,   18,  14,   14,   9),
        (10,   145,  40,   22,  16,   18,   12),
        (12,   180,  52,   26,  20,   23,   15),
        (14,   210,  60,   31,  24,   28,   19),
        (16,   250,  75,   36,  28,   34,   23),
        (18,   310,  90,   42,  32,   41,   27),
        (20,   394,  112,  49,  38,   48,   32),
        (22,   480,  130,  56,  43,   56,   38),
        (24,   590,  155,  64,  50,   65,   44),
        (26,   680,  180,  72,  56,   75,   51),
        (28,   810,  210,  81,  64,   86,   59),
        (30,   940,  240,  90,  71,   98,   67),
        (32,   1100, 280,  101, 79,   112,  77),
        (34,   1320, 320,  111, 88,   127,  87),
        (35,   1420, 340,  118, 92,   135,  93),
        (37,   1650, 390,  130, 102,  152,  105),
        (39,   1910, 440,  143, 112,  171,  118),
        (40,   1980, 440,  143, 112,  171,  118),
        (42,   2350, 540,  165, 130,  203,  141),
        (44,   2620, 600,  179, 141,  228,  158),
        (45,   2780, 630,  187, 147,  241,  167),
        (47,   3100, 700,  201, 159,  269,  187),
        (48,   3280, 730,  209, 165,  284,  197),
        (50,   3650, 810,  226, 179,  316,  220),
        (51,   3850, 850,  234, 185,  333,  232),
        (53,   4300, 940,  251, 199,  370,  258),
        (55,   4800, 1040, 269, 213,  410,  286),
        (56,   5050, 1090, 278, 221,  431,  301),
        (57,   5320, 1140, 288, 229,  454,  317),
        (58,   5600, 1190, 297, 236,  478,  334),
        (60,   6200, 1310, 317, 252,  529,  370),
        (61,   6520, 1370, 327, 260,  556,  389),
        (62,   6850, 1430, 337, 268,  584,  409),
        (63,   7200, 1500, 347, 276,  613,  429),
        (64,   7550, 1560, 358, 285,  644,  451),
        (65,   7920, 1630, 368, 293,  675,  473),
        (66,   8300, 1700, 379, 302,  708,  496),
        (67,   8700, 1780, 391, 311,  742,  520),
        (68,   9100, 1850, 402, 320,  777,  545),
        (69,   9520, 1930, 414, 329,  814,  571),
        (70,   10000,2010, 426, 339,  852,  598),
        (71,   10400,2090, 438, 349,  892,  626),
        (72,   10900,2180, 451, 359,  933,  655),
        (73,   11400,2260, 464, 369,  975,  685),
        (74,   11900,2350, 477, 380,  1020, 716),
        (75,   12420,2440, 490, 390,  1065, 748),
        (76,   12980,2540, 504, 401,  1113, 782),
        (77,   13550,2630, 518, 413,  1162, 817),
        (78,   14150,2730, 532, 424,  1213, 853),
        (79,   14780,2830, 547, 436,  1266, 890),
        (80,   15420,2940, 562, 448,  1321, 929),
        (81,   16100,3050, 578, 460,  1378, 969),
        (82,   16800,3160, 594, 473,  1437, 1011),
        (83,   17520,3280, 610, 486,  1498, 1054),
        (84,   18280,3400, 627, 499,  1561, 1099),
        (85,   19050,3520, 644, 513,  1627, 1145),
    };

    public static int Hp(int level)   => Interp(level, r => r.Hp);
    public static int Mp(int level)   => Interp(level, r => r.Mp);
    public static int PDef(int level) => Interp(level, r => r.PDef);
    public static int MDef(int level) => Interp(level, r => r.MDef);
    public static int PAtk(int level) => Interp(level, r => r.PAtk);
    public static int MAtk(int level) => Interp(level, r => r.MAtk);

    private static int Interp(int level,
        Func<(int Lvl, int Hp, int Mp, int PDef, int MDef, int PAtk, int MAtk), int> sel)
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
