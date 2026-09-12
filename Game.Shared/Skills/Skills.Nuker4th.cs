namespace Game.Shared;

/// <summary>
/// THE MAGUS'S 4th TIER, 76-90 — `docs/data/classes_skills_csv/nuker 4th.csv` (236 rows, his
/// *"so i think im done with nuker 4th"*, 2026-09-10). `BL-192`.
///
/// <para><b>Two halves, like every 4th-tier file.</b> NINETEEN families simply continue past 74 —
/// those are the <c>NukerFourth*Rungs()</c> builders below, concatenated onto the 3rd-tier arrays at
/// each skill's own definition site. SIX are new and are defined here in full.</para>
///
/// <para>🔑 <b>THE PRICE LADDER IS THE HEALER'S, EXACTLY</b> — his header is the same one
/// `healer 4th.csv` carries, so <see cref="HealerFourthSp"/> / <see cref="HealerFourthGold"/> and the
/// <c>F4</c>/<c>F4New</c> helpers are reused rather than restated. ⚠ FOUR FAMILIES DO NOT USE IT and
/// are authored per rung instead: Elemental Burst, Thunderstorm, the three race Bursts (100kk gold on
/// all three rungs, SP 0) and Arcane Void (5kk gold at 80/85/90, not the 5/50/100kk the ladder gives).
/// Those are his cells; read the file, never the ladder, for them.</para>
///
/// <para>🔑 <b>THE ROBE MASTERY'S FOUR MOVING COLUMNS ARE THE HEALER'S TO THE DIGIT.</b> P.Def
/// 89→108, Max MP 220→400, M.Def% 2→25 and the MP-cost cut 0→10% are
/// <see cref="HealerFourthRobeRungs"/> unchanged; the ONE thing that still makes the nuker's mastery
/// its own skill is `mpWhenRestored` (60/65/70%). So this file adds a column, it does not copy fifteen.</para>
///
/// <para>🔑 <b>THE FIRST 4th-TIER RUNG OF ALL THREE RACE BURSTS REPEATS THE 3rd TIER'S LAST — AND IT
/// IS NOT A WASTED RUNG.</b> Power 150, the same rider, for 100kk of gold, and the ladder then climbs
/// 150 → 200 → 250. It was reported to him as a rung that buys nothing; his answer (2026-09-11) is the
/// mechanic: *"it don't give power but it gives higher debuff chance .. the magic become lvl 80 not 74
/// .. (it won't fail anyway but atleast debuff will land more often)"*. He is right, and it is exactly
/// the rule this file's Witches Scarecrow runs on — <c>DebuffLandChance</c> reads the RUNG's own LEARN
/// LEVEL, so an identical spell bought at 80 wins the level contest against everything a 74 one loses.
/// A Burst cannot fizzle (`SureHit`), so the level term has nowhere else to show up: buying the rung is
/// buying the rider's landing rate and nothing else. ⚠ Do not "fix" this into a power step.</para>
///
/// <para>⚠ <b>"Decrease Mp Consumption" UNQUALIFIED = BOTH CHANNELS.</b> Where he means one channel he
/// says so — Spell Empowerment is *"magic MP consumption"*, the warrior's toggle was *"p.mp"*. The
/// robe mastery, the shield mastery and Force Empowerment are all unqualified, so they take both, which
/// is also exactly what the healer's identical robe clause already ships as
/// (<see cref="StatMods.MpCostPct"/> is one number for both by design).</para>
/// </summary>
public static partial class SkillCatalog
{
    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  HIS SHARED COLUMNS. Each is stated ONCE — several families read the same line on his sheet,
    //  exactly as they did at the 3rd tier, and restating them is how two of them silently drift.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The BOLT MP line, 69 → 115: Elemental Blast, Quick Blast, Frost Spikes, Frost Pierce.
    /// ⚠ It is byte-for-byte the healer's Holy Ray MP ladder — one 4th-tier mage bolt price, three files.</summary>
    private static readonly int[] NukerBoltMp4 =
        { 69, 71, 73, 77, 79, 91, 95, 97, 99, 103, 105, 107, 111, 113, 115 };

    /// <summary>The AoE MP line, 105 → 144: Elemental Wave and Arcane Wave.</summary>
    private static readonly int[] NukerWaveMp4 =
        { 105, 107, 109, 111, 114, 117, 120, 123, 126, 129, 132, 135, 138, 141, 144 };

    /// <summary>The HEAVY single-target MP line, 138 → 230 — twice the bolt line, which is what a
    /// drain or a rider costs: Vampiric Bolt, Witches Curse, Witches Scarecrow.</summary>
    private static readonly int[] NukerHeavyMp4 =
        { 138, 142, 146, 154, 158, 182, 190, 194, 198, 206, 210, 214, 222, 226, 230 };

    // ═══ 🔴 ALL THREE ROTATION LADDERS ARE HIS OWN ×1.30, 2026-09-12 ═══════════════════════
    //
    //  *"increase the mages spell power with some points to increase the dmg with atleast 30% on top
    //    of the avr crit dmg we increases (so about 20~40 points up 110-> 130, 138->180/190) after 76"*
    //
    //  🔑 ×1.30 RATHER THAN HIS TWO POINT FIGURES, and the reason is that they disagree with each
    //  other: +20 on 110 is +18%, which is under his own *"atleast 30%"* floor, while +40 on 138 is
    //  +29%. A flat 30% satisfies the requirement at every rung and lands inside the range he gave at
    //  the top — 143 against his "130", 179 against his "180/190". The percentage is the ruling; the
    //  point figures were prefixed "about".
    //
    //  ⚠ ALL THREE LADDERS, NOT JUST THE BLAST. He named the blast's numbers because they are the
    //  ones he reads, but *"the mages spell power"* is a class statement and the rotation is not one
    //  spell: raising only the blast would silently retune Quick Blast and the waves DOWN by 30%
    //  relative to it. Their ratios to each other are exactly as he authored them, to the point.
    //
    //  ⚠ THE ULTIMATES ARE NOT IN THIS — Elemental Burst, Thunderstorm, Arcane Void and the three race
    //  Bursts keep their authored power. They are five-minute showpieces, not *"the dmg"*, and moving
    //  them would change what a mage does in a boss window rather than what he does in a rotation.
    //  One line each if he wants them.
    //
    //  🔑 THE 3rd TIER IS UNTOUCHED, on his *"after 76"*. So the class change at 76 now steps from
    //  108 (the 74 rung) to 143, a +32% ascension jump where it used to be +2%.

    /// <summary>The BLAST power ladder: Elemental Blast and Vampiric Bolt. His 110 → 138 by +2,
    /// ×1.30 (2026-09-12) — <b>143 → 179</b>.</summary>
    private static readonly int[] NukerBlastPower4 =
        { 143, 146, 148, 151, 153, 156, 159, 161, 164, 166, 169, 172, 174, 177, 179 };

    /// <summary>The FAST/RIDER power ladder: Quick Blast and Witches Curse. His 88 → 109, ×1.30 —
    /// <b>114 → 142</b>. ⚠ It was the healer's Holy Ray numbers exactly and no longer is; that
    /// coincidence was never load-bearing, and the healer keeps his own column.</summary>
    private static readonly int[] NukerQuickPower4 =
        { 114, 117, 118, 121, 122, 125, 129, 130, 131, 133, 134, 137, 138, 140, 142 };

    /// <summary>The AREA / RIDER power ladder: Elemental Wave, Arcane Wave, Frost Spikes, Frost
    /// Pierce. His 66 → 105, ×1.30 — <b>86 → 137</b>. Still flatter than the single-target line for
    /// the same reason it was at the 3rd tier — these four either sweep or carry a debuff.</summary>
    private static readonly int[] NukerWavePower4 =
        { 86, 88, 91, 94, 98, 101, 105, 109, 113, 117, 121, 125, 129, 133, 137 };

    /// <summary>`mpWhenRestored`, 60% ×4 / 65% ×5 / 70% ×6 — the ONE robe column that is the nuker's own.
    /// ⚠ It RESUMES at 60%, where the 3rd tier plateaued for its last three rungs, and climbs again.</summary>
    private static readonly float[] NukerFourthRestorePct =
    {
        .60f, .60f, .60f, .60f, .65f, .65f, .65f, .65f, .65f, .70f, .70f, .70f, .70f, .70f, .70f,
    };

    /// <summary>The price EVERY 4th-tier nuker ULTIMATE pays: no SP at all and 100kk of gold, on all
    /// three of its rungs. His column, not the shared ladder's — see the class summary.</summary>
    private const int NukerUltimateGold = 100_000_000;

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS. Each returns ONLY the 4th-tier rungs; the definition site concatenates.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Mage Armor Mastery, rungs 19-33 — the SkillLevel half (price and text only; the robe
    /// payload rides alongside it in <see cref="NukerFourthRobeProfiles"/>).</summary>
    internal static SkillLevel[] NukerFourthArmorMasteryRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        var r = HealerFourthRobeRungs[i];
        float restore = NukerFourthRestorePct[i];
        string mp = r.MpCostPct > 0f ? $", MP costs −{r.MpCostPct * 100:0}%" : "";
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"In a robe: +{r.PDef} P.Def, +{r.MaxMp} Max MP, +{r.MDefPct * 100:0}% M.Def, "
                       + $"+{restore * 100:0}% MP from every restore{mp}.");
    });

    /// <summary>The fifteen robe profiles that go with the rungs above. Four of the five numbers are
    /// the healer's rung; `RestoreMpPct` is the nuker's.</summary>
    internal static ArmorMasteryProfile[] NukerFourthRobeProfiles() =>
        Enumerable.Range(0, 15).Select(i =>
        {
            var r = HealerFourthRobeRungs[i];
            return new ArmorMasteryProfile(Robe: new StatMods(
                PDef: r.PDef, MaxMp: r.MaxMp, MDefPct: r.MDefPct, MpCostPct: r.MpCostPct,
                RestoreMpPct: NukerFourthRestorePct[i]));
        }).ToArray();

    /// <summary>Elemental Blast rungs 15-29.</summary>
    internal static SkillLevel[] NukerFourthBlastRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerBlastPower4[i], MpCost: NukerBoltMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Magic damage, power {NukerBlastPower4[i]}."));

    /// <summary>Quick Blast rungs 15-29. The PvP halving is on the def and never moves.</summary>
    internal static SkillLevel[] NukerFourthQuickRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerQuickPower4[i], MpCost: NukerBoltMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Magic damage, power {NukerQuickPower4[i]}. Half power in PvP."));

    /// <summary>Elemental Wave rungs 15-29 — the PBAoE, radius 200.</summary>
    internal static SkillLevel[] NukerFourthWaveRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerWavePower4[i], MpCost: NukerWaveMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Hits every enemy within 200 for power {NukerWavePower4[i]}."));

    /// <summary>Arcane Wave rungs 15-29 (HUMAN) — the same numbers thrown at the target, radius 400.</summary>
    internal static SkillLevel[] NukerFourthArcaneWaveRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerWavePower4[i], MpCost: NukerWaveMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Hits every enemy within 400 of the target for power {NukerWavePower4[i]}."));

    /// <summary>Vampiric Bolt rungs 20-34 (HUMAN). Blast power on the heavy MP line.</summary>
    internal static SkillLevel[] NukerFourthVampiricRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerBlastPower4[i], MpCost: NukerHeavyMp4[i], SpCost: sp, GoldCost: gold,
            Range: 900f,
            Description: $"Drain power {NukerBlastPower4[i]}; heals 40% of damage."));

    /// <summary>Frost Spikes rungs 15-29 (ELF). The slow steps 40 → 42 → 45% on his 4/5/6 grouping;
    /// the ×2 interrupt and the ×0.7 landing are on the def and do not move.</summary>
    internal static SkillLevel[] NukerFourthFrostSpikesRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        float[] slow = { .40f, .40f, .40f, .40f, .42f, .42f, .42f, .42f, .42f,
                         .45f, .45f, .45f, .45f, .45f, .45f };
        return new SkillLevel(Power: NukerWavePower4[i], MpCost: NukerBoltMp4[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, slow[i]) },
            Description: $"Power {NukerWavePower4[i]}, and a chance to slow by {slow[i] * 100:0}% for 30s.");
    });

    /// <summary>Frost Pierce rungs 15-29 (ELF). ⚠ The bleed RANK IS 10 ON EVERY ROW — the 3rd tier
    /// reached 10 at 74 and 10 is the top rank a cure can reach, so the whole 4th tier holds there.
    /// What the ladder buys above 76 is the direct hit.</summary>
    internal static SkillLevel[] NukerFourthFrostPierceRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: NukerWavePower4[i], MpCost: NukerBoltMp4[i], SpCost: sp, GoldCost: gold,
            Rank: 10,
            Description: $"Power {NukerWavePower4[i]}, and a chance to open a rank-10 bleed for 15s."));

    /// <summary>Witches Curse rungs 15-29 (DEMON). M.Def −30 → −32 → −35%, his 4/5/6 grouping again.</summary>
    internal static SkillLevel[] NukerFourthWitchesCurseRungs() => F4Rungs(15, 1, (i, sp, gold) =>
    {
        float[] mDef = { .30f, .30f, .30f, .30f, .32f, .32f, .32f, .32f, .32f,
                         .35f, .35f, .35f, .35f, .35f, .35f };
        return new SkillLevel(Power: NukerQuickPower4[i], MpCost: NukerHeavyMp4[i], SpCost: sp, GoldCost: gold,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -mDef[i]) },
            Description: $"Power {NukerQuickPower4[i]}, and a chance to cut M.Def by {mDef[i] * 100:0}% for 30s.");
    });

    /// <summary>Witches Scarecrow rungs 15-29 (DEMON). Like Bind, NOTHING but the price moves: a fear
    /// is a fear, and what the ladder buys is the level contest (DebuffLandChance reads the rung's own
    /// learn level).</summary>
    internal static SkillLevel[] NukerFourthScarecrowRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(MpCost: NukerHeavyMp4[i], SpCost: sp, GoldCost: gold,
            Description: "Terrifies the target for 10s."));

    /// <summary>Arcane Void rungs 4-7 (HUMAN), at 76 / 80 / 85 / 90 — and ONLY the MP moves: all four
    /// of his rows read "2~4", which is the <c>DispelCount: 4</c> the 3rd tier's last rung already had.
    ///
    /// <para>⚠ THE GOLD IS 1kk / 5kk / 5kk / 5kk, NOT the shared ladder's 1 / 5 / 50 / 100kk. His
    /// cells, taken at their word.</para></summary>
    internal static SkillLevel[] NukerFourthArcaneVoidRungs()
    {
        int[] mp = { 147, 156, 165, 174 };
        var (sp76, gold76) = F4(0);
        int[] sp = { sp76, 0, 0, 0 };
        int[] gold = { gold76, 5_000_000, 5_000_000, 5_000_000 };
        return Enumerable.Range(0, 4).Select(i =>
            new SkillLevel(MpCost: mp[i], SpCost: sp[i], GoldCost: gold[i], DispelCount: 4,
                Description: "A chance to strip 2-4 positive effects.")).ToArray();
    }

    /// <summary>Elemental Burst rungs 4-6, at 80 / 85 / 90. Power = MP, as on every big nuke in both
    /// his nuker files, and still two Elemental Stones.</summary>
    internal static SkillLevel[] NukerFourthElementalBurstRungs()
    {
        int[] pow = { 200, 225, 250 };
        return pow.Select(p => new SkillLevel(Power: p, MpCost: p, SpCost: 0, GoldCost: NukerUltimateGold,
            Description: $"Magic damage, power {p}. Consumes 2 Elemental Stones.")).ToArray();
    }

    /// <summary>Thunderstorm rungs 4-6, at 80 / 85 / 90. Three stones and a five-second cast.</summary>
    internal static SkillLevel[] NukerFourthThunderstormRungs()
    {
        int[] pow = { 250, 300, 350 };
        return pow.Select(p => new SkillLevel(Power: p, MpCost: p, SpCost: 0, GoldCost: NukerUltimateGold,
            Description: $"Storm damage, power {p}. Consumes 3 Elemental Stones.")).ToArray();
    }

    /// <summary>Arcane Burst rungs 2-4 (HUMAN). SPT resistance −40 → −50%.</summary>
    internal static SkillLevel[] NukerFourthArcaneBurstRungs()
    {
        int[] pow = { 150, 200, 250 };
        float[] spt = { .40f, .45f, .50f };
        return Enumerable.Range(0, 3).Select(i =>
            new SkillLevel(Power: pow[i], MpCost: pow[i], SpCost: 0, GoldCost: NukerUltimateGold,
                CcResistMagical: -spt[i],
                Description: $"Power {pow[i]}, never fizzles, and cuts SPT resistance by "
                           + $"{spt[i] * 100:0}% for 15s.")).ToArray();
    }

    /// <summary>Frost Burst rungs 2-4 (ELF). The freeze holds; its M.Def bite goes −30 → −35%.</summary>
    internal static SkillLevel[] NukerFourthFrostBurstRungs()
    {
        int[] pow = { 150, 200, 250 };
        float[] mDef = { .30f, .32f, .35f };
        return Enumerable.Range(0, 3).Select(i =>
            new SkillLevel(Power: pow[i], MpCost: pow[i], SpCost: 0, GoldCost: NukerUltimateGold,
                Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -mDef[i]) },
                Description: $"Power {pow[i]}, never fizzles, and freezes for 15s: "
                           + $"−{mDef[i] * 100:0}% M.Def, cannot move.")).ToArray();
    }

    /// <summary>Pyro Burst rungs 2-4 (DEMON) — and THE GAME'S FIRST TIER-12 BURN, which is what the
    /// <see cref="DotTiers.MaxCurableTier"/> wall was built for.
    ///
    /// <para>🔑 THE RUNGS ARE BURN TIERS 10 / 11 / 12, and that is not an interpretation: his rows read
    /// −100 / −125 / −150 HP a second and 70 / 72 / 75% HP-and-MP-received, which IS the burn table's
    /// t10 / t11 / t12 line verbatim (`docs/data/dot_table.csv`). So the whole rider is the RANK —
    /// nothing here authors a damage number, exactly as `BL-197` requires.</para>
    ///
    /// <para>🔑 AND THE TOP RUNG IS UNCURABLE BY CONSTRUCTION. *"i want healers holy blessing or
    /// whatever that cures/clences to clence to t11. so pyromancer ultimate is uncurable"* — tier 12 is
    /// refused by <see cref="DotTiers.Curable"/>, so the level-90 Demon nuker's burn cannot be lifted by
    /// Holy Blessing, an Antidote or anything else. No flag on the skill says so; the TIER does.</para></summary>
    internal static SkillLevel[] NukerFourthPyroBurstRungs()
    {
        int[] pow = { 150, 200, 250 };
        int[] tier = { 10, 11, 12 };
        return Enumerable.Range(0, 3).Select(i =>
            new SkillLevel(Power: pow[i], MpCost: pow[i], SpCost: 0, GoldCost: NukerUltimateGold,
                Rank: tier[i],
                // ⚠ HIS `(success chance x1.5)` IS CARRIED AND IS INERT, and both halves of that are
                //   deliberate. His 3rd-tier Pyro Burst row has no such clause and these three do —
                //   almost certainly copied from the Arcane/Frost Burst rows beside them. It changes
                //   nothing either way: a BURN's save is `DebuffSchool.None` (*"for burn nothing
                //   protects .. always land"*), so the landing branch skips the contest this number
                //   would have modified. Carried so the code and his cell read the same, rather than
                //   deleted from his file over a number that cannot bite. Flagged in the report.
                DebuffLandMod: 1.5f,
                Description: $"Power {pow[i]}, never fizzles, then burns at tier {tier[i]} for 15s"
                           + (tier[i] > DotTiers.MaxCurableTier ? " — and nothing can cure it." : "."))).ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  SIX NEW SKILLS
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    public const string NukerShieldMastery = "nuker_shield_mastery";

    /// <summary>⚠ THE CAPITAL F IS HIS, in his own SKILL_ID cell. Ids are append-only strings compared
    /// verbatim by `--check`; it is not ours to tidy.</summary>
    public const string NukerForceEmpowerment = "nuker_Force_empowerment";

    public const string NukerSpellEmpowermentHuman = "nuker_human_spell_empowerment";
    public const string NukerSpellEmpowermentElf = "nuker_elf_spell_empowerment";
    public const string NukerSpellEmpowermentDemon = "nuker_demon_spell_empowerment";

    // The three Spell Empowerment retaliation payloads, one per race, three rungs each. Payload defs:
    // never learned, never on a bar, applied only by the proc machinery (the archer stances' shape).
    private const string NukerEmpowerHumanHit = "nuker_empower_human_hit_";
    private const string NukerEmpowerElfCurse = "nuker_empower_elf_curse_";
    private const string NukerEmpowerDemonCurse = "nuker_empower_demon_curse_";

    private static SkillDef[] Nuker4thSkills()
    {
        var (sp76, gold76) = F4New(76);
        var list = new List<SkillDef>();

        // ═══ MAGE SHIELD MASTERY @76 ═════════════════════════════════════════════════════════════
        //
        // 🔑 A ROBE CASTER'S SHIELD PASSIVE, and the trade is explicit in his own row: *"but shield can
        //    never block (block rate x0)"*. So it is not the tank's shield — you carry it for the M.Atk,
        //    the cheaper spells, the mana and the 100 P.Def, and you give up the one thing a shield
        //    normally does. No new primitive: `BlockChancePct` is already a ×(1 + pct) channel
        //    (Entity.RecomputeDerived), so −1 reaches exactly ×0 and the roll can never come up.
        //
        // ⚠ `RequiresShield` gates the WHOLE effect, which is what his *"When Shield is equiped"* means
        //   — a nuker who swaps to a two-handed staff keeps none of it, penalty included.
        list.Add(new SkillDef(NukerShieldMastery, "Mage Shield Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: sp76,
            Passive: NukerShieldRung(),
            Levels: new[]
            {
                new SkillLevel(SpCost: sp76, GoldCost: gold76, Passive: NukerShieldRung(),
                    Description: "With a shield equipped: +5% M.Atk, −10% MP cost, +10% MP regeneration "
                               + "and +100 P.Def — but the shield can never block."),
            },
            Description: "With a shield equipped: +5% M.Atk, −10% MP cost, +10% MP regeneration and "
                       + "+100 P.Def — but the shield can never block."));

        // ═══ FORCE EMPOWERMENT @78 / 80 / 82 — a TOGGLE ══════════════════════════════════════════
        //
        // 🔑 THE LADDER MAKES THE STANCE CHEAPER, NOT STRONGER — +14/15/16% M.Atk against a mana
        //    surcharge falling 20 → 15 → 10% and an HP drain falling 50 → 40 → 30 a second. Two of its
        //    three columns go DOWN as the rungs rise, which is the right shape for a stance and is not
        //    a ladder dip: what you are buying is sustain.
        //
        // ⚠ Same machinery as the warrior's Overpower Mastery — `Toggle` + `HpPerSecond`, charged by
        //   TickToggleUpkeep, which drops the stance while HP still remains, so it can never kill you.
        //
        // ✅ AND THE DRAIN REALLY LADDERS, since `BL-208` (his ruling the day this shipped: *"Make
        //    togles to can change value of drain per lvl .. Some can drain more mp why some cant drain
        //    less hp?"*). `SkillLevel.HpPerSecond` is the MP field's twin and did not exist until then,
        //    so this stance was the thing that found the gap: all three rungs would have burned the
        //    first one's 50.
        float[] forceAtk = { .14f, .15f, .16f };
        float[] forceMp = { .20f, .15f, .10f };
        int[] forceHp = { 50, 40, 30 };
        int[] forceLvl = { 78, 80, 82 };
        EffectMagnitude[] ForceMags(int i) =>
            new EffectMagnitude[] { new(SkillEffect.BuffMagAtk, forceAtk[i], ModifierMode.Percent) };

        list.Add(new SkillDef(NukerForceEmpowerment, "Force Empowerment", BaseClass.Mage,
            SkillEffect.BuffMagAtk,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: "nuker_force_empowerment", Rank: 1,
            Category: SkillCategory.Buff, SpCost: F4New(78).Sp,
            TargetMode: TargetMode.SelfOnly,
            Toggle: true, CountsTowardBuffLimit: false,
            HpPerSecond: forceHp[0],
            // Unqualified *"mana consumption"* → BOTH channels; see the class summary. NEGATIVE = dearer.
            MagicMpCostPct: -forceMp[0], PhysMpCostPct: -forceMp[0],
            Magnitudes: ForceMags(0),
            Levels: Enumerable.Range(0, 3).Select(i =>
            {
                var (sp, gold) = F4New(forceLvl[i]);
                return new SkillLevel(SpCost: sp, GoldCost: gold,
                    HpPerSecond: forceHp[i],
                    MagicMpCostPct: -forceMp[i], PhysMpCostPct: -forceMp[i],
                    Magnitudes: ForceMags(i),
                    Description: $"Stance. +{forceAtk[i] * 100:0}% M.Atk, every skill costs "
                               + $"{forceMp[i] * 100:0}% more MP, and you burn {forceHp[i]} HP a second.");
            }).ToArray(),
            Description: "Stance. Your magic hits far harder, paid for in mana and in blood."));

        // ═══ MANA BARRIER @85 is NOT here ════════════════════════════════════════════════════════
        //
        // 🔑 It already EXISTED — an orphan def in Skills.Mage.cs carrying his exact numbers (70% of
        //    damage to MP at 0.5 MP per point, 30s) and on nobody's class table, like Dispel Magic
        //    before it. Its ID was renamed to his `nuker_mana_barrier` and its reuse corrected from 30s
        //    to his 300s in place. Authoring a second def would have left two.

        // ═══ THE THREE SPELL EMPOWERMENTS @80 / 85 / 90 — one per race ═══════════════════════════
        //
        // 🔑 EVERY RUNG BUYS THE SAME THING: the M.Atk and the 5% retaliation are FLAT across all three,
        //    and what climbs is how little mana the stance costs you. The identical shape to Force
        //    Empowerment above — which is how his whole 4th tier prices a permanent self-buff.
        //
        // 🔴 THE RETALIATION IS THE ENGINE GAP `BL-192` NAMED. `ProcOnDamaged` and `ProcVictimRungs`
        //    both existed, but `TryOnDamagedProcs` never passed the ATTACKER through, so a defensive
        //    proc had nowhere to put a payload and these three could not have fired at all. The
        //    attacker is passed now, and the victim arm learned to deal DIRECT DAMAGE for the Human's
        //    *"inflicts damage on attackers with power 47"*. See GameLoopService.TryProcs.
        //
        // ⚠ TWO NUMBERS HERE ARE MINE AND NOT HIS: the rider's DURATION (10s) and the proc's internal
        //   cooldown (10s). His cells give the chance and the magnitude and nothing else. Both are the
        //   archer stances' own values — the only other victim-paying procs in the game — so they are at
        //   least the house number rather than an invention. Flagged in the report.
        list.AddRange(SpellEmpowerment(NukerSpellEmpowermentHuman, Race.Human,
            mAtk: 0.12f, mana: new[] { .25f, .20f, .15f },
            rider: "a 5% chance to strike back at whoever hits you",
            riderRung: NukerEmpowerHumanHit));
        list.AddRange(SpellEmpowerment(NukerSpellEmpowermentElf, Race.Elf,
            mAtk: 0.10f, mana: new[] { .20f, .15f, .10f },
            rider: "a 5% chance to cripple whoever hits you",
            riderRung: NukerEmpowerElfCurse));
        list.AddRange(SpellEmpowerment(NukerSpellEmpowermentDemon, Race.Demon,
            mAtk: 0.15f, mana: new[] { .30f, .25f, .20f },
            rider: "a 5% chance to slow whoever hits you to a crawl",
            riderRung: NukerEmpowerDemonCurse));

        return list.ToArray();
    }

    /// <summary>The shield mastery's one rung. A METHOD and not a static field, like every other rung
    /// builder in this file — nothing here can then run before the catalog does.</summary>
    private static PassiveEffect NukerShieldRung() => new(
        RequiresShield: true,
        MagAtkPct: 0.05f, MpRegenPct: 0.10f, Defence: 100,
        // Unqualified "mp consumption −10%" → BOTH channels; see the class summary. POSITIVE = cheaper.
        PhysMpCostPct: 0.10f, MagicMpCostPct: 0.10f,
        // ×(1 + −1) = ×0: the shield is worn, and it never blocks.
        BlockChancePct: -1f);

    /// <summary>ONE race's Spell Empowerment plus its three retaliation payloads. Ten minutes, self
    /// only, and a 5% counter-punch on being hit.</summary>
    private static SkillDef[] SpellEmpowerment(string id, Race race, float mAtk, float[] mana,
                                               string rider, string riderRung)
    {
        // His SP/gold: 150kk + 10kk to LEARN it at 80, then 0 SP and 100kk gold for each rung after.
        (int Sp, int Gold) Price(int i) => i == 0 ? F4New(80) : (0, NukerUltimateGold);

        EffectMagnitude[] Mags() =>
            new EffectMagnitude[] { new(SkillEffect.BuffMagAtk, mAtk, ModifierMode.Percent) };

        string[] rungs = Enumerable.Range(1, 3).Select(n => riderRung + n).ToArray();

        var buff = new SkillDef(id, "Spell Empowerment", BaseClass.Mage, SkillEffect.BuffMagAtk,
            MpCost: 100, CastTicks: 10, CooldownTicks: 50, Range: 0, Power: 0,
            // ⚠ ONE KEY FOR ALL THREE, and `SharesLadderKey` is what says so on purpose — the `BL-85`
            // boot guard is otherwise right that two multi-rung ladders on one key make each other's
            // rungs compete, and it REFUSED THE BUILD until this was declared. It is harmless here for
            // the same reason it is on the archer's three race stances: a Spell Empowerment is one per
            // RACE and a race has exactly one, so no character can ever hold two. Sharing the key is
            // what keeps them one family — one bar square, and a future group buff or potion competes
            // with all three at once rather than with whichever one this Magus happens to own.
            DurationTicks: 6000, BuffKey: "nuker_spell_empowerment", Rank: 1, SharesLadderKey: true,
            Category: SkillCategory.Buff, SpCost: F4New(80).Sp,
            TargetMode: TargetMode.SelfOnly,
            // NEGATIVE = dearer. His *"Increases magic MP consumption by 30%"* — explicitly the MAGIC
            // channel this time, so the physical one is deliberately left alone.
            MagicMpCostPct: -mana[0],
            ProcChance: 0.05f, ProcOnDamaged: true, ProcCooldownTicks: 100,
            ProcVictimRungs: rungs,
            Magnitudes: Mags(),
            Levels: Enumerable.Range(0, 3).Select(i =>
            {
                var (sp, gold) = Price(i);
                return new SkillLevel(MpCost: 100, SpCost: sp, GoldCost: gold,
                    MagicMpCostPct: -mana[i], Magnitudes: Mags(),
                    Description: $"For 10 minutes: +{mAtk * 100:0}% M.Atk, spells cost "
                               + $"{mana[i] * 100:0}% more MP, and {rider}.");
            }).ToArray(),
            Description: $"Ten minutes of borrowed force: +{mAtk * 100:0}% M.Atk at the price of "
                       + $"dearer spells, and {rider}.");

        var list = new List<SkillDef> { buff };
        for (int i = 0; i < 3; i++)
            list.Add(EmpowermentRider(riderRung + (i + 1), race, i));
        return list.ToArray();
    }

    /// <summary>One retaliation payload. NEVER learned and never on a bar — the proc machinery is the
    /// only thing that ever names it.
    ///
    /// <para>🔑 THE HUMAN'S IS THE ONLY ONE THAT DEALS DAMAGE, and it is the reason the victim arm of
    /// <c>TryProcs</c> had to grow a damage branch: the other two races pay in an ordinary debuff, which
    /// `ApplyBuff` already handled. Power 47 / 51 / 55, his cells.</para>
    ///
    /// <para>⚠ APPLIED FLAT, with no debuff contest — a proc's own chance IS its landing chance
    /// (see SkillDef.ProcVictimRungs). Rolling CON or SPT on top would make his 5% a fraction of 5%.</para></summary>
    private static SkillDef EmpowermentRider(string id, Race race, int i)
    {
        int[] humanPower = { 47, 51, 55 };
        return race switch
        {
            // ⚠ THE THREE PAYLOADS CARRY THEIR OWN NAMES, not the buff's. They are what the victim
            //   sees in the floating text and in his debuff bar, and three unrelated effects all
            //   reading "Spell Empowerment" would say nothing about what just happened. The archer's
            //   riders set the precedent — "Bleeding", "Poisoned". The parent skill keeps HIS name.

            // HUMAN — *"inflicts damage on attackers with power 47/51/55"*.
            Race.Human => new SkillDef(id, "Arcane Recoil", BaseClass.Mage, SkillEffect.MagicDamage,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: humanPower[i],
                Category: SkillCategory.Magic,
                Description: $"Arcane backlash, power {humanPower[i]}."),

            // ELF — *"decrease attackers move speed with 30% and Mdef with 10%"*. Flat on every rung.
            Race.Elf => new SkillDef(id, "Chilled", BaseClass.Mage,
                SkillEffect.Slow | SkillEffect.BuffMagicDef,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 100, BuffKey: "nuker_empower_elf", Rank: 1, SharesLadderKey: true,
                Category: SkillCategory.Debuff,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.Slow, 0.30f), new(SkillEffect.BuffMagicDef, -0.10f),
                },
                Description: "−30% move speed and −10% M.Def for 10s."),

            // DEMON — *"decrease attackers cast/attack speed with 23%"*. Flat on every rung.
            _ => new SkillDef(id, "Sapped", BaseClass.Mage,
                SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                DurationTicks: 100, BuffKey: "nuker_empower_demon", Rank: 1, SharesLadderKey: true,
                Category: SkillCategory.Debuff,
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.DebuffAtkSpeed, 0.23f), new(SkillEffect.DebuffCastSpeed, 0.23f),
                },
                Description: "−23% attack speed and cast speed for 10s."),
        };
    }
}
