namespace Game.Shared;

/// <summary>
/// THE LIGHTBRINGER'S 4th TIER, 76-90 — `docs/data/classes_skills_csv/healer 4th.csv` (255 rows,
/// which he calls finished, 2026-08-26). The first 4th-class kit in the game.
///
/// <para><b>Two halves.</b> Twenty of his families are the SAME skills the 3rd class already teaches,
/// simply continued past 74 — those are the <c>HealerFourth*Rungs()</c> builders below, concatenated
/// onto the 3rd-tier arrays at each skill's own definition site. Eleven are new, and are defined
/// here in full.</para>
///
/// <para><b>THE PRICE LADDER IS HIS FILE'S HEADER</b>, and it changes shape at 80: up to 79 a rung
/// costs SP and a token 1kk of gold; from 80 it costs <b>no SP at all</b> and a gold price that climbs
/// 5kk → 100kk. That is why <see cref="HealerFourthSp"/> is mostly zeroes and is not a mistake — past
/// 80 the currency of progress is gold, and SP is spent on SP Bottles instead. A skill LEARNED (rather
/// than levelled) at 80-83 uses his separate "New Skills/Ultimates" column, which is what
/// <see cref="Fourth4NewSp"/> carries.</para>
///
/// <para><b>⚠ TWO LADDER DIPS were corrected, per the standing monotonic rule</b> (a value going
/// backwards is a typo — interpolate or report, never accept):
/// <list type="bullet">
///   <item><b>Party Great Heal @82</b> read 760, below the 770 at 81 and repeating the 760 at 80.
///         Set to <b>775</b>, which keeps the 770 → 780 stride intact.</item>
///   <item><b>Mana Blessing @90</b> read an MP cost of <b>20</b> against 190 at 88. Set to
///         <b>200</b>, continuing the +10 per rung the whole ladder runs on.</item>
/// </list>
/// Both are flagged back to him; if he meant either, the CSV is the authority and they come back.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  HIS PRICE LADDER, indexed by (character level − 76).
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>SP to raise an EXISTING skill by one rung, 76 → 90. Zero from 80 up — his header's
    /// "Old-LvlUp" column is gold-only past 79.</summary>
    private static readonly int[] HealerFourthSp =
    {
        6_500_000, 11_000_000, 16_000_000, 80_000_000,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    };

    /// <summary>Gold to raise an existing skill by one rung, 76 → 90.</summary>
    private static readonly int[] HealerFourthGold =
    {
        1_000_000, 1_000_000, 1_000_000, 1_000_000,
        5_000_000, 7_500_000, 10_000_000, 15_000_000,
        30_000_000, 50_000_000, 75_000_000,
        100_000_000, 100_000_000, 100_000_000, 100_000_000,
    };

    /// <summary>The "New Skills / Ultimates" column — what a skill first LEARNED at this level costs,
    /// which past 79 is far dearer than levelling one you already own. 84 and 85 are authored in SP
    /// BOTTLES rather than SP; nothing in `healer 4th.csv` is first learned there, so they are left at
    /// the 83 price rather than inventing a bottle-denominated learn cost.</summary>
    private static readonly int[] Fourth4NewSp =
    {
        6_500_000, 11_000_000, 16_000_000, 80_000_000,
        150_000_000, 200_000_000, 300_000_000, 500_000_000,
        500_000_000, 500_000_000, 500_000_000, 500_000_000, 500_000_000, 500_000_000, 500_000_000,
    };

    private static readonly int[] Fourth4NewGold =
    {
        1_000_000, 1_000_000, 1_000_000, 1_000_000,
        10_000_000, 25_000_000, 50_000_000, 100_000_000,
        100_000_000, 100_000_000, 100_000_000,
        100_000_000, 100_000_000, 100_000_000, 100_000_000,
    };

    /// <summary>The SP/gold pair for raising an existing skill at character level 76 + <paramref
    /// name="i"/> × <paramref name="step"/>. step 1 = his every-level ladders, step 2 = the
    /// every-other-level ones.</summary>
    private static (int Sp, int Gold) F4(int i, int step = 1)
    {
        int b = Math.Clamp(i * step, 0, HealerFourthSp.Length - 1);
        return (HealerFourthSp[b], HealerFourthGold[b]);
    }

    /// <summary>The same, for a skill first LEARNED at <paramref name="level"/>.</summary>
    private static (int Sp, int Gold) F4New(int level)
    {
        int b = Math.Clamp(level - 76, 0, Fourth4NewSp.Length - 1);
        return (Fourth4NewSp[b], Fourth4NewGold[b]);
    }

    /// <summary>Build <paramref name="count"/> 4th-tier rungs, handing the builder the rung index and
    /// that rung's SP and gold. <paramref name="step"/> picks the band shape (1 = every level from 76,
    /// 2 = every other).</summary>
    private static SkillLevel[] F4Rungs(int count, int step, Func<int, int, int, SkillLevel> mk) =>
        Enumerable.Range(0, count).Select(i => { var (sp, g) = F4(i, step); return mk(i, sp, g); }).ToArray();

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS. Each returns ONLY the 4th-tier rungs; the definition site concatenates.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Anti-Magic rungs 21-35 (his 76-90 rows). M.Def 113 → 149, and magic resistance steps
    /// 30% → 35% at 86 — the first move it has made since 70.</summary>
    internal static SkillLevel[] HealerFourthAntiMagicRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] mDef   = { 113, 116, 119, 121, 123, 125, 127, 129, 131, 133, 135, 138, 140, 144, 149 };
        float[] mRes = { .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f, .30f,
                         .35f, .35f, .35f, .35f, .35f };
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Passive: new PassiveEffect(MagicDefence: mDef[i], MagicResist: mRes[i]),
            Description: $"+{mDef[i]} magic defence and {mRes[i] * 100:0}% magic resistance.");
    });

    /// <summary>Spellcaster Weapon Mastery rungs 15-29. Only the M.Atk moves — reuse −20%, cast +10%
    /// and both regen multipliers are flat across the whole 4th tier in his file.</summary>
    private static readonly WeaponRung[] HealerFourthWeaponRungs =
        BuildFourthWeaponRungs();

    private static WeaponRung[] BuildFourthWeaponRungs()
    {
        int[] mAtk = { 101, 102, 104, 105, 106, 108, 109, 110, 112, 113, 115, 116, 117, 119, 120 };
        // ⚠ BOTH regen columns are FLAT per-second grants since `BL-92` (2026-08-26), read straight
        // off his row: `mpReg +3.4` → 3.4f and `hpReg +2.7` → 2.7f. The whole rung, not its excess.
        // HP followed MP the same day; see the note on WeaponRung.
        return mAtk.Select(m => new WeaponRung(m, 0.20f, 0.10f, 3.4f, 2.7f)).ToArray();
    }

    internal static SkillLevel[] HealerFourthWeaponMasteryRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        var r = HealerFourthWeaponRungs[i];
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"With a wand or staff: +{r.MAtk} M.Atk, +{r.Cast * 100:0}% cast, "
                       + $"−{r.Reuse * 100:0}% reuse, MP regen +{r.MpFlat:0.#}/s, HP regen +{r.HpReg:0.#}/s.");
    });

    /// <summary>Healer Armor Mastery rungs 15-29. Four numbers move now, not two: P.Def and Max MP as
    /// before, plus an M.Def PERCENT (2% → 25%) and, from 78, an MP-cost reduction (5% → 10%).</summary>
    internal static readonly RobeRung4[] HealerFourthRobeRungs =
    {
        new( 89, 220, .02f, 0f),    new( 91, 220, .04f, 0f),    new( 92, 250, .05f, .05f),
        new( 93, 250, .07f, .05f),  new( 95, 250, .08f, .05f),  new( 96, 290, .10f, .08f),
        new( 97, 290, .11f, .08f),  new( 99, 300, .13f, .08f),  new(100, 300, .14f, .08f),
        new(101, 300, .16f, .08f),  new(103, 330, .17f, .10f),  new(104, 330, .19f, .10f),
        new(105, 350, .20f, .10f),  new(107, 350, .22f, .10f),  new(108, 400, .25f, .10f),
    };

    /// <summary>One 4th-tier robe rung: the two flats, an M.Def percent and an MP-cost cut.</summary>
    internal readonly record struct RobeRung4(int PDef, int MaxMp, float MDefPct, float MpCostPct);

    internal static SkillLevel[] HealerFourthArmorMasteryRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        var r = HealerFourthRobeRungs[i];
        string mp = r.MpCostPct > 0f ? $", MP costs −{r.MpCostPct * 100:0}%" : "";
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"In a robe: +{r.PDef} P.Def, +{r.MaxMp} Max MP, +{r.MDefPct * 100:0}% M.Def, "
                       + $"MP regen x1.2{mp}.");
    });

    /// <summary>The robe profile for a 4th-tier rung.</summary>
    internal static ArmorMasteryProfile HealerRobe4(RobeRung4 r) =>
        new(Robe: new StatMods(PDef: r.PDef, MaxMp: r.MaxMp,
                               MDefPct: r.MDefPct, MpCostPct: r.MpCostPct));

    /// <summary>Holy Ray rungs 15-29 — his 76-90 rows.</summary>
    internal static SkillLevel[] HealerFourthHolyRayRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 88, 90, 91, 93, 94, 96, 99, 100, 101, 102, 103, 105, 106, 108, 109 };
        int[] mp  = { 69, 71, 73, 77, 79, 91, 95,  97,  99, 103, 105, 107, 111, 113, 115 };
        return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
            Description: $"Magic damage, m.Atk +{pow[i]}.");
    });

    /// <summary>The heal MP ladder every 4th-tier single-target heal shares: 122 → 150, +2 a rung.
    /// The party versions are exactly twice it, which is the same rule as the 3rd tier.</summary>
    private static readonly int[] HealMp4 =
        { 122, 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 144, 146, 148, 150 };

    /// <summary>Great Heal rungs 15-29.</summary>
    internal static SkillLevel[] HealerFourthGreatHealRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 865, 875, 890, 900, 915, 920, 955, 960, 980, 1000, 1050, 1100, 1200, 1300, 1400 };
        return new SkillLevel(Power: pow[i], MpCost: HealMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Heals a single ally for {pow[i]}.");
    });

    /// <summary>Party Great Heal rungs 15-29. ⚠ The @82 rung is 775, not the 760 he typed — see the
    /// class summary's dip note.</summary>
    internal static SkillLevel[] HealerFourthPartyHealRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 720, 730, 740, 750, 760, 770, 775, 780, 790, 800, 830, 860, 900, 950, 1000 };
        return new SkillLevel(Power: pow[i], MpCost: HealMp4[i] * 2, SpCost: sp, GoldCost: gold,
            Description: $"Heals you and nearby party members for {pow[i]}.");
    });

    /// <summary>Quick Great Heal (Human) rungs 15-29 — Great Heal's power on a 2s cast, at its own
    /// steeper MP.</summary>
    internal static SkillLevel[] HealerFourthQuickHealRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 865, 875, 890, 900, 915, 920, 955, 960, 980, 1000, 1050, 1100, 1200, 1300, 1400 };
        int[] mp  = { 181, 183, 185, 190, 192, 195, 200, 202, 204,  206,  208,  210,  215,  218,  220 };
        return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
            Description: $"Heals a single ally for {pow[i]} on a 2s cast.");
    });

    /// <summary>Healer Blessing (Elf) rungs 15-29. ⚠ The cure ceiling STOPS CLIMBING — his rows all say
    /// "rank 10 or lower", and 10 is the top rank there is, so every 4th-tier rung cures everything a
    /// rank can express. What the ladder buys past here is the heal.</summary>
    internal static SkillLevel[] HealerFourthBlessingRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 780, 790, 800, 810, 820, 830, 850, 870, 890, 910, 950, 990, 1080, 1170, 1260 };
        return new SkillLevel(Power: pow[i], MpCost: HealMp4[i], SpCost: sp, GoldCost: gold,
            DispelMaxLevel: 10,
            Description: $"Heals an ally for {pow[i]} and cures their bleed and poison of rank 10 or lower.");
    });

    /// <summary>Healing Totem (Ork) rungs 15-29.</summary>
    internal static SkillLevel[] HealerFourthTotemRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        int[] pow = { 152, 153, 154, 155, 156, 157, 159, 162, 166, 170, 174, 178, 182, 186, 200 };
        int[] mp  = { 520, 523, 526, 529, 532, 535, 541, 550, 562, 574, 586, 598, 610, 622, 634 };
        return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
            Description: $"A totem healing +{pow[i]}/s within 300 for 30s.");
    });

    /// <summary>Ultimate Heal rungs 10-17 — the even bands. ⚠ The REAGENT doubles at the 4th tier:
    /// his rows say *"Consumes 2 skill stone"* where the 3rd tier's said one, so `ConsumableAmount` became
    /// a per-LEVEL field and only these eight rungs pay two. The 3rd tier still pays one.</summary>
    internal static SkillLevel[] HealerFourthUltimateHealRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        int[] pow = { 1100, 1200, 1300, 1400, 1500, 1600, 1800, 2000 };
        return new SkillLevel(Power: pow[i], MpCost: HealMp4[i * 2], SpCost: sp, GoldCost: gold,
            ConsumableAmount: 2,
            Description: $"Heals a single ally for {pow[i]}. Consumes 2 Skill Stones.");
    });

    /// <summary>Ultimate Party Heal rungs 10-17. Same powers, twice the MP, five stones.</summary>
    internal static SkillLevel[] HealerFourthUltimatePartyRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        int[] pow = { 1100, 1200, 1300, 1400, 1500, 1600, 1800, 2000 };
        return new SkillLevel(Power: pow[i], MpCost: HealMp4[i * 2] * 2, SpCost: sp, GoldCost: gold,
            ConsumableAmount: 5,
            // His 4th-tier rows bring the party version into line with the single-target one: a 5s cast
            // on a 2s reuse, where the 3rd tier was 7s on 5s.
            CastTicks: 50, CooldownTicks: 20,
            Description: $"Heals you and nearby party members for {pow[i]}. Consumes 5 Skill Stones.");
    });

    /// <summary>Mana Ray rungs 11-18. Power is PER MILLE of the target's max MP (145 → 18%).</summary>
    internal static SkillLevel[] HealerFourthManaRayRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        int[] pow = { 145, 150, 155, 160, 165, 170, 175, 180 };
        int[] mp  = { 362, 370, 384, 400, 408, 416, 430, 440 };
        return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
            Description: $"Drains {pow[i] / 10f:0.#}% of the target's maximum MP (half that on a monster).");
    });

    /// <summary>The MP price of a 4th-tier contested curse, shared by Mana Strain, Weapon Break and
    /// the three race debuffs exactly as the 3rd tier's <c>DebuffMp</c> was.</summary>
    private static readonly int[] DebuffMp4 = { 69, 73, 79, 95, 99, 105, 111, 115 };

    /// <summary>Mana Strain rungs 12-19: +205% → +250% MP cost on the victim.</summary>
    internal static SkillLevel[] HealerFourthManaStrainRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] pct = { 2.05f, 2.10f, 2.15f, 2.20f, 2.25f, 2.30f, 2.40f, 2.50f };
        return new SkillLevel(MpCost: DebuffMp4[i], SpCost: sp, GoldCost: gold,
            PhysMpCostPct: -pct[i], MagicMpCostPct: -pct[i],
            Description: $"Raises the target's physical and magic MP costs by {pct[i] * 100:0}% for 60s.");
    });

    /// <summary>Weapon Break rungs 5-12: −16% → −25% to both attack channels.</summary>
    internal static SkillLevel[] HealerFourthWeaponBreakRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] pct = { .16f, .17f, .18f, .19f, .20f, .21f, .23f, .25f };
        return new SkillLevel(MpCost: DebuffMp4[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, pct[i]) },
            Description: $"−{pct[i] * 100:0}% P.Atk and M.Atk for 30s.");
    });

    /// <summary>Gravity (Human) rungs 15-22. ⚠ The plateau his 3rd tier ended on (23%) RESUMES CLIMBING
    /// here, 23% → 30% — the ceiling was a 3rd-class ceiling, not a permanent one.</summary>
    internal static SkillLevel[] HealerFourthGravityRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] pct = { .23f, .24f, .25f, .26f, .27f, .28f, .29f, .30f };
        return new SkillLevel(MpCost: DebuffMp4[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, pct[i]), new(SkillEffect.DebuffCastSpeed, pct[i]),
            },
            Description: $"−{pct[i] * 100:0}% attack speed and cast speed for 30s.");
    });

    /// <summary>Bind (Elf) rungs 15-22. Unlike the 3rd tier, where NOTHING but the price moved, both
    /// the DURATION (31 → 40s) and the LANDING multiplier (×0.7 → ×0.8) climb — his two comment cells
    /// say so in as many words: *"intentional increasing in duration"*, *"Intentionall increasing in
    /// chance"*. `DebuffLandMod` is per-LEVEL, so both ladders are live.</summary>
    internal static SkillLevel[] HealerFourthBindRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        int[] dur = { 310, 320, 330, 340, 350, 360, 380, 400 };   // ticks
        float[] land = { .70f, .70f, .71f, .73f, .75f, .77f, .80f, .80f };
        return new SkillLevel(MpCost: DebuffMp4[i], SpCost: sp, GoldCost: gold,
            DurationTicks: dur[i],
            DebuffLandMod: land[i],
            Description: $"Holds an enemy in place for {dur[i] / 10}s.");
    });

    /// <summary>Armor Break (Ork) rungs 15-22. Like Gravity, the 3rd tier's 30/15 plateau resumes:
    /// P.Def 31 → 40%, M.Def 15 → 20%. ⚠ The exact HALF relationship the 3rd tier kept is broken by
    /// his own numbers here (31/15, 33/16, 35/17) — his rows, left alone.</summary>
    internal static SkillLevel[] HealerFourthArmorBreakRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] pDef = { .31f, .32f, .33f, .34f, .35f, .36f, .38f, .40f };
        float[] mDef = { .15f, .16f, .16f, .17f, .17f, .18f, .19f, .20f };
        return new SkillLevel(MpCost: DebuffMp4[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, pDef[i]), new(SkillEffect.BuffMagicDef, -mDef[i]),
            },
            Description: $"−{pDef[i] * 100:0}% P.Def and −{mDef[i] * 100:0}% M.Def for 30s.");
    });

    /// <summary>Mana Blessing rungs 4-11: physical MP cost −21% → −30%, magic −11% → −20%. ⚠ The @90
    /// MP cost is 200, not the 20 he typed — see the dip note in the class summary.</summary>
    internal static SkillLevel[] HealerFourthManaBlessingRungs() => F4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] phys = { .21f, .22f, .23f, .24f, .25f, .26f, .28f, .30f };
        float[] mag  = { .11f, .12f, .13f, .14f, .15f, .16f, .18f, .20f };
        int[] mp     = { 130, 140, 150, 160, 170, 180, 190, 200 };
        return new SkillLevel(MpCost: mp[i], SpCost: sp, GoldCost: gold,
            PhysMpCostPct: phys[i], MagicMpCostPct: mag[i],
            Description: $"Physical skills cost {phys[i] * 100:0}% less MP and spells {mag[i] * 100:0}% less, for 20 minutes.");
    });

    /// <summary>Resurrection rungs 17-18 (76 and 80), and then it is finished. Both restore ALL the
    /// lost experience — what still climbs is how full you stand up, 35% then 40%.</summary>
    internal static SkillLevel[] HealerFourthResurrectionRungs() => new[]
    {
        new SkillLevel(MpCost: 205, SpCost: HealerFourthSp[0], GoldCost: HealerFourthGold[0],
            ResExpPct: 1.00f, ResHpPct: 0.35f, CastTicks: 50, CooldownTicks: 50,
            Description: "Revive at 35% HP/MP; restore 100% of lost exp (5s cast)."),
        new SkillLevel(MpCost: 250, SpCost: HealerFourthSp[4], GoldCost: HealerFourthGold[4],
            ResExpPct: 1.00f, ResHpPct: 0.40f, CastTicks: 50, CooldownTicks: 50,
            Description: "Revive at 40% HP/MP; restore 100% of lost exp (5s cast)."),
    };

    /// <summary>Resurrection Field rungs 5-6 (76 and 80). The area res keeps its 70% ceiling on the
    /// experience — deliberately worse per head than the single-target one, which is the whole trade —
    /// and buys the same fullness climb plus a halved cast.</summary>
    internal static SkillLevel[] HealerFourthResFieldRungs() => new[]
    {
        new SkillLevel(MpCost: 410, SpCost: HealerFourthSp[0], GoldCost: HealerFourthGold[0],
            ResExpPct: 0.70f, ResHpPct: 0.35f, AreaRadius: 900f, CastTicks: 100,
            Description: "Revives fallen allies within 900 at 35% HP/MP; restores 70% of lost exp."),
        new SkillLevel(MpCost: 500, SpCost: HealerFourthSp[4], GoldCost: HealerFourthGold[4],
            ResExpPct: 0.70f, ResHpPct: 0.40f, AreaRadius: 900f, CastTicks: 50,
            Description: "Revives fallen allies within 900 at 40% HP/MP; restores 70% of lost exp."),
    };

    /// <summary>Antidote rung 10, at 76 — and the LAST it will ever have: rank 10 is the top rank a
    /// debuff can carry, so there is nothing above it left to cure.</summary>
    internal static SkillLevel[] HealerFourthAntidoteRungs() => new[]
    {
        new SkillLevel(MpCost: 72, SpCost: HealerFourthSp[0], GoldCost: HealerFourthGold[0],
            DispelMaxLevel: 10,
            Description: "Cures poison, venom and bleed of any rank from an ally (or self)."),
    };

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  ELEVEN NEW SKILLS
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    public const string HealerShieldMastery  = "healer_shield_mastery";
    public const string ArcaneResistance     = "arcane_resistance";
    public const string HolyBlessing         = "holy_blessing";
    public const string HolySoul             = "holy_soul";
    public const string HealersPower         = "healers_power";
    public const string HealerPartyBlessing  = "healer_party_blessing";
    public const string ElvenRestoration     = "elven_restoration";
    public const string LifeRestoration      = "life_restoration";
    public const string SpiritRestoration    = "spirit_restoration";
    public const string HolyMark             = "holy_mark";
    public const string LifeMark             = "life_mark";
    public const string BloodMark            = "blood_mark";
    public const string UrgentGreatHeal      = "urgent_great_heal";   // @83 — capped triage area heal

    /// <summary>The MARK buffs all share ONE key, which is his *"Do not Stack with Other 'Mark'
    /// Skills"* — the same trick Great Might and Great Bulwark use. An ally wears one Mark, never two,
    /// whichever race's healer got to them first.</summary>
    /// <summary>The buff key EVERY Mark lands on — his *"Do not Stack with Other 'Mark' Skills"*, and the
    /// same trick Great Might and Great Bulwark use. An ally wears ONE Mark, whichever race's healer got
    /// to them first.
    ///
    /// <para>🔑 THE BUFFER'S SHARES IT TOO. His `buffer 4th.csv` has a party-wide Mark at 79 — named
    /// <b>Harmony Mark</b> on 2026-08-26 (he asked for one that combined Holy / Life / Blood and took
    /// the recommendation): it keeps the family's `&lt;Word&gt; Mark` shape rather than flipping to "Mark of
    /// Harmony", and HARMONY is the buffer's own signature the way Holy/Life/Blood are the three races'.
    /// ⚠ NOT BUILT — `buffer 4th.csv` was still in progress. When it lands its id is `harmony_mark` and
    /// it MUST carry this key, or a healer's Mark and a buffer's would stack.</para></summary>
    private const string MarkKey = "healer_mark";

    private static SkillDef[] Lightbringer4thSkills()
    {
        var (sp76, gold76) = F4New(76);
        var (sp78, gold78) = F4New(78);
        var (sp83, gold83) = F4New(83);

        // One Mark. Everything they share — 900 range, 5s cast, 5 minutes, 4 skill stones, the three
        // universal +10/+20/+20% lines — stated once; the three per-race extras are the parameter.
        SkillDef Mark(string id, string name, string extraText, EffectMagnitude[] extras,
                      float ccMag = 0f, float ccPhys = 0f, float magicCritDmg = 0f, float magicAcc = 0f) =>
            new(id, name, BaseClass.Mage,
                SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk | SkillEffect.BuffDef
                | SkillEffect.BuffMagicDef | SkillEffect.BuffMoveSpeed | SkillEffect.BuffAtkSpeed
                | SkillEffect.BuffCastSpeed | extras.Aggregate(SkillEffect.None, (a, m) => a | m.Effect),
                MpCost: 300, CastTicks: 50, CooldownTicks: 50, Range: 900, Power: 0,
                DurationTicks: 3000, BuffKey: MarkKey, Rank: 1,
                Category: SkillCategory.Buff, SpCost: sp78,
                TargetMode: TargetMode.SelfOrTarget,
                ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 4,
                CcResistMagical: ccMag, CcResistPhysical: ccPhys, MagicCritDamage: magicCritDmg,
                BuffMagicAccuracy: magicAcc,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffPhysAtk,   0.10f, ModifierMode.Percent),
                    new(SkillEffect.BuffMagAtk,    0.10f, ModifierMode.Percent),
                    new(SkillEffect.BuffDef,       0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffMagicDef,  0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffMoveSpeed, 0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffAtkSpeed,  0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffCastSpeed, 0.20f, ModifierMode.Percent),
                }.Concat(extras).ToArray(),
                Levels: new[] { new SkillLevel(MpCost: 300, SpCost: sp78, GoldCost: gold78) },
                Description: "P.Atk and M.Atk +10%, both defences +20%, and move, attack and cast "
                           + "speed +20%, for five minutes. " + extraText
                           + " Consumes 4 Skill Stones. Only one Mark at a time.");

        // One Restoration. All three are a 900-range party ultimate on a ONE-HOUR reuse — the button
        // you press once per boss, which is why the reuse is the balance rather than the numbers.
        SkillDef Restoration(string id, string name, SkillEffect effect, EffectMagnitude[] mags,
                             string blurb, int duration = 0, string key = "", float healRecvPct = 0f) =>
            new(id, name, BaseClass.Mage, effect,
                MpCost: 300, CastTicks: 50, CooldownTicks: 36000, Range: 900, Power: 0,
                DurationTicks: duration, BuffKey: key, Rank: duration > 0 ? 1 : 0,
                Category: SkillCategory.Heal, SpCost: sp83,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 900f,
                FixedCooldown: true,
                BuffHealReceivedPct: healRecvPct,
                Magnitudes: mags,
                Levels: new[] { new SkillLevel(MpCost: 300, SpCost: sp83, GoldCost: gold83) },
                Description: blurb);

        return new SkillDef[]
        {
            // ---- HEALER SHIELD MASTERY @76. One rung, no ladder — and it does nothing at all without
            //      a shield, which for a robe-wearing healer is a real choice against a two-handed staff.
            new(HealerShieldMastery, "Healer's Shield Mastery", BaseClass.Mage, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive, SpCost: sp76,
                // 🔑 TRULY SHIELD-GATED (`PassiveEffect.RequiresShield`), which is what his row says:
                // *"When Sheild is equiped"*. It used to be a plain passive with the shield test left
                // to the player's honour, so a healer who swapped to a two-handed staff kept both
                // numbers — the choice this skill exists to pose was not being posed at all.
                Passive: new PassiveEffect(RequiresShield: true, HealPowerPct: 0.10f, MpRegenPct: 0.10f),
                Levels: new[] { new SkillLevel(SpCost: sp76, GoldCost: gold76,
                    Passive: new PassiveEffect(RequiresShield: true, HealPowerPct: 0.10f, MpRegenPct: 0.10f),
                    Description: "With a shield equipped: healing power +10% and MP regeneration +10%.") },
                Description: "With a shield equipped: healing power +10% and MP regeneration +10%."),

            // ---- ARCANE RESISTANCE @76 — a 20-minute blessing whose whole payload is buff DURABILITY.
            new(ArcaneResistance, "Arcane Resistance", BaseClass.Mage, SkillEffect.BuffCancelResist,
                MpCost: 130, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
                DurationTicks: 12000, BuffKey: "arcane_resistance", Rank: 1,
                Category: SkillCategory.Buff, SpCost: sp76,
                TargetMode: TargetMode.SelfOrTarget,
                Magnitudes: new EffectMagnitude[]
                    { new(SkillEffect.BuffCancelResist, 0.30f, ModifierMode.Percent) },
                Levels: new[] { new SkillLevel(MpCost: 130, SpCost: sp76, GoldCost: gold76) },
                Description: "For 20 minutes each of the target's buffs has a 30% chance to survive "
                           + "an enemy's dispel."),

            // ---- HOLY BLESSING @78 — the total cleanse. Antidote's ceiling is a rank; this has none:
            //      *"Remove every debuff (burning and venom 11 as well)"*, i.e. above what a rank can
            //      express. Priced in a HOLY STONE, which is what stops it retiring Antidote.
            new(HolyBlessing, "Holy Blessing", BaseClass.Mage, SkillEffect.Cleanse,
                MpCost: 72, CastTicks: 8, CooldownTicks: 30, Range: 600, Power: 0,
                Category: SkillCategory.Heal, SpCost: sp76,
                TargetMode: TargetMode.SelfOrTarget,
                // DispelMask None = every debuff, DispelMaxLevel 0 = at any rank. Both defaults, stated
                // for the reader: this is the one cure in the game with no ceiling of any kind.
                DispelMask: SkillEffect.None, DispelCount: 0, DispelMaxLevel: 0,
                ConsumableId: ItemCatalog.HolyStone, ConsumableAmount: 1,
                Levels: new[] { new SkillLevel(MpCost: 72, SpCost: sp76, GoldCost: gold76) },
                Description: "Strips EVERY harmful effect from an ally, at any rank. Consumes one Holy Stone."),

            // ---- HOLY SOUL @76 — a TOGGLE, and the only one a healer has. ⚠ Its cast-speed clause is a
            //      PENALTY, which his comment column confirms is deliberate (*"Intentional decrease in
            //      Cast speed"*): you trade throughput for endurance, and pay 50 HP a second for it.
            //      🔑 The 50 HP/s BITES NOW: `SkillDef.HpPerSecond`, the twin of MpPerSecond, charged by
            //      the same TickToggleUpkeep. Until it existed this toggle was a straight MP-cost win,
            //      which is NOT what his row says. The stance drops itself while HP still remains — it
            //      can never be the thing that kills you.
            new(HolySoul, "Holy Soul", BaseClass.Mage, SkillEffect.BuffCastSpeed,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                BuffKey: "holy_soul", Rank: 1,
                Category: SkillCategory.Buff, SpCost: sp76,
                TargetMode: TargetMode.SelfOnly,
                Toggle: true, CountsTowardBuffLimit: false, HpPerSecond: 50,
                PhysMpCostPct: 0.30f, MagicMpCostPct: 0.30f,
                Magnitudes: new EffectMagnitude[]
                    { new(SkillEffect.BuffCastSpeed, -0.10f, ModifierMode.Percent) },
                Levels: new[] { new SkillLevel(SpCost: sp76, GoldCost: gold76) },
                Description: "Stance. Every skill costs 30% less MP, but you cast 10% slower and burn "
                           + "50 HP a second."),

            // ---- HEALER'S POWER @80 — a 15-second self-buff on a 5-minute reuse. FLAT heal power, so
            //      it is worth the same on a small heal as on a big one: it is a burst button for the
            //      moment the tank is about to die, not a rotation.
            new(HealersPower, "Healer's Power", BaseClass.Mage, SkillEffect.None,
                MpCost: 100, CastTicks: 10, CooldownTicks: 3000, Range: 0, Power: 0,
                DurationTicks: 150, BuffKey: "healers_power", Rank: 1,
                Category: SkillCategory.Buff, SpCost: F4New(80).Sp,
                TargetMode: TargetMode.SelfOnly,
                Levels: HealersPowerRungs(),
                Description: "For 15 seconds every heal you cast lands for far more."),


            // ---- HEALER PARTY BLESSING @83 (ELF ONLY) — the party version of the Elf's heal-and-cure,
            //      eight rungs at 83-90. The other two races have no equivalent; that is his file.
            new(HealerPartyBlessing, "Healer Party Blessing", BaseClass.Mage,
                SkillEffect.Heal | SkillEffect.Cleanse,
                MpCost: 272, CastTicks: 30, CooldownTicks: 50, Range: 600, Power: 780,
                Category: SkillCategory.Heal, SpCost: sp83,
                TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
                DispelMask: SkillEffect.Bleed | SkillEffect.Poison | SkillEffect.Venom,
                DispelMaxLevel: 10,
                Levels: HealerPartyBlessingRungs(),
                Description: "Heals your whole party and cures their bleed and poison in one cast."),

            // ---- THE THREE RESTORATIONS @83. One per race, a full party recovery on a ONE-HOUR reuse.
            //      🔑 They are three DIFFERENT skills rather than three rungs of one because their
            //      payloads have nothing in common: the Elf refills mana, the Human refills life, and
            //      the Ork gives up the totality for a 15-second healing-received window on top.
            // ---- URGENT GREAT HEAL @83 — the last unbuilt row in `healer 4th.csv`, and the one he
            //      pointed at on 2026-08-27 (*"urgent great heal in healer 4th is placed/authored"*).
            //      His row, verbatim: *"Heals 10 most injured friendly target around the caster
            //      (including caster in 900 range) starting with most injured one by 30% and every
            //      next target is healed -2% less form the one before, Consumes 5 skill stones"*.
            //
            // 🔑 IT IS A % HEAL, SO POWER IS 0. Like Urgent Heal (which it Replaces), the size comes
            //    from the TARGET's own pool and not from the healer's sheet — which is why a level-83
            //    button is still the right size for a 15k tank and needs no rung after it.
            //
            // 🔑 THE ENGINE PIECE IT NEEDED IS `MaxTargets` + `TargetFalloff`, both new. Every area
            //    heal before this one paid the same amount to everyone it reached, so a cap and a
            //    per-rank decay had nowhere to live. The triage ORDERING lives with them in
            //    GameLoopService's heal branch — see the comment there for why it sorts on the
            //    fraction missing rather than on raw HP.
            //
            // 🔑 ELEVEN SLOTS, NOT TEN — HE SETTLED THE TAIL HIMSELF, 2026-08-27: *"so if we make it
            //    the 10 moost injured around the caster and the caster is 11th that make it 30~10%
            //    heal"*. Ten allies PLUS the caster is eleven, and `30 - 2 x index` over eleven lands
            //    exactly on his 30/28/26/…/12/10. At ten it stopped at 12% and the trailing "10%" in
            //    his row had nowhere to come from; this is where it comes from.
            //
            // ⚠ THE ELEVENTH SLOT CANNOT BE REACHED TODAY, and that is worth knowing rather than
            //    discovering in a raid. `PlayersInRadius` is PARTY-only and a full party is NINE
            //    (GameConstants' heal comments size every party number on 9), so the real span in play
            //    is 30% down to 30-2x8 = **14%**. The cap is authored at his number so that the day
            //    "friendly" widens past the party — alliances, raids — the ladder is already correct
            //    and nobody has to remember why it said ten.
            new(UrgentGreatHeal, "Urgent Great Heal", BaseClass.Mage, SkillEffect.Heal,
                MpCost: 500, CastTicks: 30, CooldownTicks: 50, Range: 0, Power: 0,
                Category: SkillCategory.Heal, SpCost: sp83,
                TargetMode: TargetMode.FriendlyInRadius, AreaRadius: 900f,
                MaxTargets: 11, TargetFalloff: 0.02f,
                ConsumableId: ItemCatalog.SkillStone, ConsumableAmount: 5,
                Replaces: new[] { UrgentHeal },
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.30f, ModifierMode.Percent) },
                Levels: new[] { new SkillLevel(MpCost: 500, SpCost: sp83, GoldCost: gold83) },
                Description: "Finds the ten most badly hurt allies within 900, plus yourself, and heals "
                           + "them worst-first — 30% of their own maximum HP for the worst, 2% less for "
                           + "each one after, down to 10%. Consumes 5 Skill Stones."),

            Restoration(ElvenRestoration, "Elven Restoration",
                SkillEffect.RestoreMp | SkillEffect.Heal,
                new EffectMagnitude[]
                {
                    new(SkillEffect.RestoreMp, 1.00f, ModifierMode.Percent),
                    new(SkillEffect.Heal,      0.50f, ModifierMode.Percent),
                },
                "Refills your whole party's MP completely and half their HP. Once an hour."),

            Restoration(LifeRestoration, "Life Restoration",
                SkillEffect.Heal | SkillEffect.RestoreMp,
                new EffectMagnitude[]
                {
                    new(SkillEffect.Heal,      1.00f, ModifierMode.Percent),
                    new(SkillEffect.RestoreMp, 0.50f, ModifierMode.Percent),
                },
                "Refills your whole party's HP completely and half their MP. Once an hour."),

            // The Ork's is the only one that leaves something behind — 30% of both bars now, and every
            // heal that lands on the party for the next 15 seconds is 30% bigger.
            Restoration(SpiritRestoration, "Spirit Restoration",
                SkillEffect.Heal | SkillEffect.RestoreMp | SkillEffect.BuffHpRegen,
                new EffectMagnitude[]
                {
                    new(SkillEffect.Heal,      0.30f, ModifierMode.Percent),
                    new(SkillEffect.RestoreMp, 0.30f, ModifierMode.Percent),
                },
                "Restores 30% of your party's HP and MP, and for 15 seconds every heal they receive "
                + "is 30% stronger. Once an hour.",
                duration: 150, key: "spirit_restoration", healRecvPct: 0.30f),

            // ---- THE THREE MARKS @78. One per race, one buff key: an ally wears ONE Mark.
            // ⚠ HIS 2026-08-26 EDIT added an ACCURACY line to each of the three, and it is not the same
            //    line on each: the Elf reads "M.Acc +4", the Human "P.Acc +4", the Ork "P/M.Acc +3".
            // 🔑 M.ACC IS THE MIRROR OF M.EVASION, and it is real now. He asked what it was and answered
            //    his own question — *"the mAcc is magic fizzle chance? what does Magic evasion do? so the
            //    oposite"* — so `MagicAccuracy` takes flat percentage POINTS back off the caster's own fail
            //    roll, exactly as `MagicFailBonus` adds them for the defender.
            // ⚠ This does NOT reopen the caster-side accuracy STAT the 2026-08-10 rework deleted. That was
            //    something you carried and levelled; this is a flat grant a named skill hands out, on the
            //    same footing M.Evasion has had since 2026-08-11. Nothing derives it and no gear rolls it.
            Mark(HolyMark, "Holy Mark",
                "The Elf's Mark also grants +10% resistance to Spirit debuffs, +4 magic accuracy, "
                + "+20% magic critical rate and +20% maximum MP.",
                new EffectMagnitude[]
                {
                    new(SkillEffect.BuffMagicCritRate, 0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffMp,            0.20f, ModifierMode.Percent),
                },
                ccMag: 0.10f, magicAcc: 4f),

            Mark(LifeMark, "Life Mark",
                "The Human's Mark also grants +5% resistance to Constitution debuffs, +4 accuracy, "
                + "+20% physical critical rate and +20% maximum HP.",
                new EffectMagnitude[]
                {
                    new(SkillEffect.BuffAccuracy, 4f,    ModifierMode.Flat),
                    new(SkillEffect.BuffCritRate, 0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffHp,       0.20f, ModifierMode.Percent),
                },
                ccPhys: 0.05f),

            // ⚠ The Ork's EVASION line is GONE — his 2026-08-26 edit replaced it with melee vampirism
            //    and dropped both accuracies from 4 to 3, which he says is deliberate. Do not restore it.
            Mark(BloodMark, "Blood Mark",
                "The Ork's Mark also grants +3% melee vampirism, +3 accuracy, +20% critical damage of "
                + "both kinds, and +20% HP and MP regeneration.",
                new EffectMagnitude[]
                {
                    new(SkillEffect.BuffMeleeVamp,  0.03f, ModifierMode.Percent),
                    new(SkillEffect.BuffAccuracy,   3f,    ModifierMode.Flat),
                    new(SkillEffect.BuffCritDamage, 0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffHpRegen,    0.20f, ModifierMode.Percent),
                    new(SkillEffect.BuffMpRegen,    0.20f, ModifierMode.Percent),
                },
                magicCritDmg: 0.20f, magicAcc: 3f),
        };
    }

    /// <summary>Healer's Power, five rungs at 80/83/85/87/90 — his own irregular levels. Every one of
    /// them is a LEVEL-UP price except the first, which is a new skill at 80.</summary>
    private static SkillLevel[] HealersPowerRungs()
    {
        int[] level = { 80, 83, 85, 87, 90 };
        int[] flat  = { 1000, 1250, 1500, 1750, 2000 };
        int[] mp    = { 100, 150, 200, 250, 300 };
        return Enumerable.Range(0, level.Length).Select(i =>
        {
            var (sp, gold) = i == 0 ? F4New(level[i]) : F4(level[i] - 76);
            return new SkillLevel(MpCost: mp[i], SpCost: sp, GoldCost: gold,
                HealPowerFlat: flat[i],
                Description: $"For 15 seconds your heals land for +{flat[i]}.");
        }).ToArray();
    }

    /// <summary>Healer Party Blessing, eight rungs at 83-90 — every level, not every other.</summary>
    private static SkillLevel[] HealerPartyBlessingRungs()
    {
        int[] pow = { 780, 790, 800, 830, 860, 900, 950, 1000 };
        int[] mp  = { 272, 276, 280, 284, 288, 292, 296, 300 };
        return Enumerable.Range(0, pow.Length).Select(i =>
        {
            var (sp, gold) = i == 0 ? F4New(83) : F4(83 + i - 76);
            return new SkillLevel(Power: pow[i], MpCost: mp[i], SpCost: sp, GoldCost: gold,
                DispelMaxLevel: 10,
                Description: $"Heals your party for {pow[i]} and cures their bleed and poison.");
        }).ToArray();
    }
}
