namespace Game.Shared;

/// <summary>
/// THE NUKER'S 3rd-CLASS KIT, 40 → 74 — every rung read off `docs/data/classes_skills_csv/nuker 3rd.csv`
/// (his file, 208 authored rows, finished before the healer's was and never built until now).
///
/// <para>🔑 <b>THE BANDS AND THE SP LADDER ARE THE HEALER'S, EXACTLY.</b> His nuker file runs the same
/// fourteen learn levels (40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74) and the same SP column
/// (36k → 880k). That is not a coincidence to be normalised away later — it is the 3rd tier's cadence for
/// a MAGE, and both files were authored against it. So this file reuses <see cref="HealerBands"/> and
/// <see cref="HealerRungs"/> rather than restating fourteen numbers that could then drift apart.</para>
///
/// <para>🔑 <b>THREE OF THE FOUR PASSIVES WERE ALREADY BUILT AND ARE SIMPLY SHARED.</b> Anti-Magic's
/// rungs 7-20 are <see cref="HealerAntiMagicRungs"/> (his own *"one shared ladder, so all three files"*),
/// and Spellcaster Weapon Mastery is <see cref="HealerWeaponMasterySkill"/> — whose ladder matched this
/// file's rows to the last digit, which is exactly what the note on that skill predicted would happen
/// ("the nuker file will want the same row"). Only Mage Armor Mastery is the nuker's own, because it
/// alone carries `mpWhenRestored`. Nothing was copied; the LEARN LINES point at the same defs.</para>
///
/// <para>🔑 <b>THE RACE SPLITS THE KIT, NOT THE DISCIPLINE.</b> Same shape as the Lightbringer. Eleven
/// families are shared; Human takes Arcane Wave / Vampiric Bolt / Arcane Void / Arcane Burst, Elf takes
/// Frost Spikes / Frost Pierce / Frost Burst, Ork takes Witches Curse / Witches Scarecrow / Pyro Burst.
/// ⚠ His file carries no DISCIPLINE column, and the nuker archetype has two (Magus and Tempest), so the
/// table registers this kit to BOTH — the same treatment `tank 3rd` already gets. See the note on
/// RegisterNuker3rd. Splitting them is a later ruling of his, and a one-line change here.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- New ids. APPEND-ONLY, collision-guarded at startup, and every one of them is a name he
    //      authored in the CSV rather than one we invented.
    public const string CalmSpirit       = "calm_spirit";        // per-STANCE MP regen (BL-92)
    public const string ElementalBlast   = "elemental_blast";    // replaces Elemental Bolt (the main nuke)
    public const string QuickBlast       = "quick_blast";        // replaces Quick Bolt (short range, fast)
    public const string ElementalWave    = "elemental_wave";     // PBAoE around the caster, 200 radius
    public const string ArcaneWave       = "arcane_wave";        // HUMAN — AoE around the TARGET, 400 radius
    public const string FrostSpikes      = "frost_spikes";       // ELF   — damage + slow, doubles interrupt
    public const string FrostPierce      = "frost_pierce";       // ELF   — damage + bleed, doubles interrupt
    public const string WitchesCurse     = "witches_curse";      // ORK   — damage + M.Def curse
    public const string WitchesScarecrow = "witches_scarecrow";  // ORK   — fear
    public const string ArcaneVoid       = "arcane_void";        // HUMAN — cancel (strips positive effects)
    public const string Thunderstorm     = "thunderstorm";       // the 5s siege nuke, 3 Elemental Stones
    public const string ArcaneBurst      = "arcane_burst";       // HUMAN 74 — never fizzles, eats SPT resist
    public const string FrostBurst       = "frost_burst";        // ELF   74 — never fizzles, freezes
    public const string PyroBurst        = "pyro_burst";         // ORK   74 — never fizzles, BURNS

    /// <summary>His MP column for the 33 → 69 family (Elemental Blast, Quick Blast, Frost Spikes,
    /// Frost Pierce). Four skills share one line on his sheet; state it once.</summary>
    private static readonly int[] NukerBoltMp =
        { 33, 38, 44, 48, 52, 54, 55, 58, 60, 62, 64, 65, 67, 69 };

    /// <summary>His MP column for the AoE pair (Elemental Wave, Arcane Wave), 53 → 103.</summary>
    private static readonly int[] NukerWaveMp =
        { 53, 59, 65, 70, 77, 80, 83, 87, 89, 93, 95, 98, 100, 103 };

    /// <summary>His MP column for the heavy single-target line (Vampiric Bolt, Witches Curse,
    /// Witches Scarecrow), 66 → 138 — twice the bolt line, which is what a rider costs.</summary>
    private static readonly int[] NukerHeavyMp =
        { 66, 76, 88, 96, 104, 108, 110, 116, 120, 124, 128, 130, 134, 138 };

    /// <summary>The bolt POWER ladder — Elemental Blast and Vampiric Bolt share it (52 → 108, and 108
    /// at 74 is IG's own anchor for a top nuke).</summary>
    private static readonly int[] NukerBlastPower =
        { 52, 58, 65, 72, 78, 82, 85, 89, 92, 96, 99, 102, 105, 108 };

    /// <summary>The "fast/rider" POWER ladder — Quick Blast and Witches Curse.
    ///
    /// <para>⚠ HIS @52 ROW READ 52, THE SAME AS @48, AND IS <b>57</b> HERE. This is the identical defect
    /// the Lightbringer's Holy Ray had at the identical rung of the identical ladder (his 2026-08-20
    /// ruling: a rung whose description repeats the one below it is the ERROR), and 57 continues the +5
    /// stride while smoothing the +11 jump to 63. ⚠ Not to be confused with a debuff MAGNITUDE plateau
    /// at the TOP of a ladder, which he restored on purpose on 2026-08-26 — this is a damage number in
    /// the middle. Both CSV rows were corrected in the same commit.</para></summary>
    private static readonly int[] NukerQuickPower =
        { 42, 47, 52, 57, 63, 66, 68, 71, 74, 77, 79, 82, 84, 87 };

    /// <summary>The AoE / rider POWER ladder — Elemental Wave, Arcane Wave, Frost Spikes, Frost Pierce.
    /// Flatter than the single-target line on purpose: these four either hit several things or carry a
    /// debuff, and he priced them at roughly 60% of Elemental Blast for it.</summary>
    private static readonly int[] NukerWavePower =
        { 30, 35, 39, 43, 46, 48, 50, 52, 54, 56, 58, 60, 62, 64 };

    /// <summary>CALM SPIRIT — six rungs at 40/48/56/62/68/74, and the only skill in the game that
    /// changes how a STANCE pays. Built 2026-08-26 (`BL-92`); REWRITTEN 2026-08-27 off his own edit
    /// to `nuker 3rd.csv`.
    ///
    /// <para>🔴 <b>THERE IS NO RUN MULTIPLIER ANY MORE, AND THAT IS THE FIX.</b> The first build gave
    /// running ×0.30 climbing to ×0.70 — but that MULTIPLIED the 0.70 run stance, so learning the
    /// passive took a running mage from 7.7 MP/s to 3.3 at rung 1 and never caught back up: an
    /// unremovable passive that made running strictly worse than not having it, at every rung
    /// including the last. Measured and reported on 2026-08-27; he fixed it in the CSV the same day
    /// and the code follows the file, as always. Running is now simply the 0.70 stance, untouched.</para>
    ///
    /// <para>🔑 <b>WHAT IT BUYS IS THE WALK COLUMN</b>, ×1.06 → ×1.16, with a ×1.01 on STANDING from
    /// rung 4. His aim is unchanged — *"both walk/still is the same mp regen in the end … keep farming
    /// while kiting (slowly)"* — but it is now bought rather than paid for: 0.85 × 1.16 = 0.986 at the
    /// top rung, so a walking mage ends level with a standing one instead of a running one being
    /// punished into it. ⚠ Both columns are now AUTHORED in the CSV; the stand column used to be
    /// DERIVED as `0.85 × walk` and no longer is. Read them off the file.</para>
    ///
    /// <para>⚠ These are MULTIPLIERS, not flats, and that is not an inconsistency with the rest of
    /// `BL-92` (where the mastery ladder went flat). A flat pair balances walk against stand at exactly
    /// ONE level — the difference it must cover is `0.15 × base`, and base moves with level, gear and
    /// buffs. Only a multiplier on the stance cancels it at every rung. See PassiveEffect.MpRegenRunMult.</para>
    ///
    /// <para>⚠ MP cost is ZERO on every rung. His CSV carried 35→69 in the MP column; he ruled it a
    /// copy-paste typo on 2026-08-26 (*"its passive - mp consumtion"*) and the file was corrected.</para></summary>
    private static SkillLevel[] CalmSpiritRungs()
    {
        // His `nuker 3rd.csv`, 2026-08-27, read straight off the DESCR column. A 0 means the rung does
        // not touch that stance at all — Entity.RecomputeDerived skips a zero rather than multiplying
        // by it — which is how "no run multiplier" is expressed without a magic 1.0.
        //                    40     48     56     62     68     74
        float[] walk  = { 1.06f, 1.08f, 1.10f, 1.12f, 1.14f, 1.16f };
        float[] stand = {    0f,    0f,    0f, 1.01f, 1.01f, 1.01f };
        int[]   sp    = { 36000, 64000, 81000, 170000, 320000, 880000 };

        return Enumerable.Range(0, 6).Select(i =>
            new SkillLevel(SpCost: sp[i], MpCost: 0,
                Passive: new PassiveEffect(
                    MpRegenWalkMult: walk[i], MpRegenStandMult: stand[i]),
                Description: stand[i] > 0f
                    ? $"MP regeneration ×{walk[i]:0.00} while walking, ×{stand[i]:0.00} while standing still."
                    : $"MP regeneration ×{walk[i]:0.00} while walking.")).ToArray();
    }

    private static SkillDef[] Nuker3rdSkills() => new SkillDef[]
    {
        // ═══ CALM SPIRIT ═════════════════════════════════════════════════════════════════════════
        new(CalmSpirit, "Calm Spirit", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 36000,
            Description: "Passive. Stillness feeds the mind: your mana returns far faster when you "
                       + "walk or stand than when you run.",
            Levels: CalmSpiritRungs()),

        // ═══ THE TWO REPLACEMENT NUKES ═══════════════════════════════════════════════════════════
        //
        // Elemental Blast and Quick Blast REPLACE Elemental Bolt and Quick Bolt, which is what finally
        // retires the 40+ half of the 2nd-class bolt ladder. That ladder was OURS (his `nuker 2nd.csv`
        // stops at 35 and the code continued the line to 80 so a level-80 mage was not fighting with a
        // level-35 spell); his 3rd file is the real thing and it wins. The bolts keep their defs and
        // their 20-35 rungs — see the purge note in ClassSkillTables.Common.cs.

        new(ElementalBlast, "Elemental Blast", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: NukerBoltMp[0], CastTicks: 40, CooldownTicks: 10, Range: 900,
            Power: NukerBlastPower[0],
            Category: SkillCategory.Magic, SpCost: 36000,
            Replaces: new[] { ElementalBolt },
            Description: "The nuker's main attack spell — raw elemental force at long range.",
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(Power: NukerBlastPower[i], MpCost: NukerBoltMp[i], SpCost: sp,
                    Description: $"Magic damage, power {NukerBlastPower[i]}."))),

        // ⚠ PVP POWER ×0.5 — his row, on every rung ("Power in PVP x0.5"). A 2s cast at 300 range that
        // hits for 80% of the main nuke is a duelling tool, not a farming one, and halving it in PvP is
        // how he kept it from being the only spell anyone casts at another player. `PvpDamageMult` is a
        // SkillDef field that already existed for exactly this and had no user until now.
        new(QuickBlast, "Quick Blast", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: NukerBoltMp[0], CastTicks: 20, CooldownTicks: 5, Range: 300,
            Power: NukerQuickPower[0],
            Category: SkillCategory.Magic, SpCost: 36000,
            Replaces: new[] { QuickBolt },
            PvpDamageMult: 0.5f,
            Description: "A fast, short-range blast (2s cast, half-second reuse). Half power against players.",
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(Power: NukerQuickPower[i], MpCost: NukerBoltMp[i], SpCost: sp,
                    Description: $"Magic damage, power {NukerQuickPower[i]}. Half power in PvP."))),

        // ═══ THE TWO AREA SPELLS ═════════════════════════════════════════════════════════════════
        //
        // 🔑 THEY ARE DIFFERENT SHAPES, AND THAT IS THE WHOLE POINT — one is centred on YOU, the other
        // on your TARGET. Elemental Wave is range 200 / radius 200: a mage who casts it is standing in
        // the pack. Arcane Wave is range 900 / radius 400: the same idea thrown from safety, which is
        // why it is the HUMAN's and costs a race slot.
        // ⚠ RANGE 0, NOT 200 (owner 2026-08-28): *"elemental wave is self/aoe with 0 range so it hits
        // around the caster with description range"*. Range and RADIUS are two different numbers —
        // range is how far you may throw the spell, radius is how wide it goes off. This one is not
        // thrown at all; it erupts where you stand, so its range is 0 and its reach is the 200 radius.
        // ⚠ TargetMode.EnemiesInRadius is what makes it actually SWEEP. Without it the skill drew its
        // circle and then hit the single target only, which is his "AOE don't work".
        new(ElementalWave, "Elemental Wave", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: NukerWaveMp[0], CastTicks: 40, CooldownTicks: 40, Range: 0,
            Power: NukerWavePower[0], AreaRadius: 200f,
            TargetMode: TargetMode.EnemiesInRadius,
            Category: SkillCategory.Magic, SpCost: 36000,
            Description: "Erupts around you, striking every enemy within 200.",
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(Power: NukerWavePower[i], MpCost: NukerWaveMp[i], SpCost: sp,
                    Description: $"Hits every enemy within 200 for power {NukerWavePower[i]}."))),

        // ⚠ `AreaAtTarget: true` — his whole point: *"the arcane wave should AOE around the mob not
        // the player like elemental wave … enemy/aoe with 900 range and hit 400 range around the
        // enemy … Not the caster"*. He also asked to check whether 400/900 were swapped: they were
        // NOT — 900 range (thrown from safety) and 400 radius (the blast) is what the code already
        // said and what the design comment above describes. Only the CENTRE was wrong.
        new(ArcaneWave, "Arcane Wave", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: NukerWaveMp[0], CastTicks: 40, CooldownTicks: 10, Range: 900,
            Power: NukerWavePower[0], AreaRadius: 400f, AreaAtTarget: true,
            TargetMode: TargetMode.EnemiesInRadius,
            Category: SkillCategory.Magic, SpCost: 36000,
            Description: "Detonates arcane force around your target, striking everything within 400 of it.",
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(Power: NukerWavePower[i], MpCost: NukerWaveMp[i], SpCost: sp,
                    Description: $"Hits every enemy within 400 of the target for power {NukerWavePower[i]}."))),

        // ═══ THE ELF'S TWO RIDERS ════════════════════════════════════════════════════════════════
        //
        // 🔑 BOTH CARRY `InterruptMult: 2` AND `DebuffLandMod` BELOW 1, AND THOSE TWO ARE UNRELATED ON
        // PURPOSE. His comment on the rows is explicit: *"does dmg but have a lower success rate for the
        // slow - interrupt unaffected"*. The debuff is the unreliable half; breaking a cast is the
        // reliable half, and doubling it is what makes the elf nuker the anti-caster.

        new(FrostSpikes, "Frost Spikes", BaseClass.Mage, SkillEffect.MagicDamage | SkillEffect.Slow,
            MpCost: NukerBoltMp[0], CastTicks: 25, CooldownTicks: 10, Range: 900,
            Power: NukerWavePower[0],
            DurationTicks: 300, BuffKey: "slow", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 36000,
            DebuffLandMod: 0.7f,   // his CSV: "(success chance x0.7)" = 35% at parity
            InterruptMult: 2f,     // his CSV: "(interrupt chance x2)"
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.15f) },
            Description: "Shards of ice: damage, a chance to slow for 30s, and twice the usual chance "
                       + "to break the target's cast.",
            // 🔑 THE SLOW PLATEAUS AT 40% FROM 74 only because that is where his ladder ends; every rung
            // below it climbs. Read the ladder, not this comment, if it is ever extended.
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                float[] slow = { .15f, .20f, .20f, .25f, .25f, .28f, .28f, .31f, .31f, .34f, .34f, .37f, .37f, .40f };
                return new SkillLevel(Power: NukerWavePower[i], MpCost: NukerBoltMp[i], SpCost: sp,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, slow[i]) },
                    Description: $"Power {NukerWavePower[i]}, and a chance to slow by {slow[i] * 100:0}% for 30s.");
            })),

        // ⚠ BLEED IS A `Rank`, NOT A MAGNITUDE. His rows read "bleed effect rank 3 … rank 10", and rank
        // is what a cure has to out-reach (Antidote's DispelMaxLevel). The DoT's damage per second is
        // the skill's Power — see TickDots — so the rank ladder and the power ladder move together.
        new(FrostPierce, "Frost Pierce", BaseClass.Mage, SkillEffect.MagicDamage | SkillEffect.Bleed,
            MpCost: NukerBoltMp[0], CastTicks: 25, CooldownTicks: 10, Range: 900,
            Power: NukerWavePower[0],
            DurationTicks: 150, BuffKey: "bleed", Rank: 3,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 36000,
            DebuffLandMod: 0.5f,   // his CSV: "(success chance x0.5)" = 25% at parity
            InterruptMult: 2f,
            Description: "Impales the target: damage, a chance to open a 15s bleed, and twice the usual "
                       + "chance to break their cast.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                int[] rank = { 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 };
                return new SkillLevel(Power: NukerWavePower[i], MpCost: NukerBoltMp[i], SpCost: sp,
                    Description: $"Power {NukerWavePower[i]}, and a chance to open a rank-{rank[i]} bleed for 15s.");
            })),

        // ═══ THE ORK'S TWO ═══════════════════════════════════════════════════════════════════════

        // M.Def debuff: a NEGATIVE `BuffMagicDef` magnitude, because there is no DebuffMagicDef flag and
        // the enum is full — the same idiom Armor Break uses. See the note there.
        new(WitchesCurse, "Witches Curse", BaseClass.Mage,
            SkillEffect.MagicDamage | SkillEffect.BuffMagicDef,
            MpCost: NukerHeavyMp[0], CastTicks: 25, CooldownTicks: 10, Range: 900,
            Power: NukerQuickPower[0],
            DurationTicks: 300, BuffKey: "witches_curse", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 36000,
            DebuffLandMod: 0.7f,   // his CSV: "(success chance x0.7)"
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -0.10f) },
            Description: "A hexing bolt: damage, and a chance to rot the target's magic defence for 30s.",
            Levels: HealerRungs(0, 14, (i, sp) =>
            {
                float[] mDef = { .10f, .10f, .13f, .13f, .17f, .17f, .20f, .20f, .23f, .23f, .27f, .27f, .30f, .30f };
                return new SkillLevel(Power: NukerQuickPower[i], MpCost: NukerHeavyMp[i], SpCost: sp,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -mDef[i]) },
                    Description: $"Power {NukerQuickPower[i]}, and a chance to cut M.Def by {mDef[i] * 100:0}% for 30s.");
            })),

        // ⚠ A NOTE FOR ANYONE READING THIS FILE BY EYE: `nuker 3rd.csv` ends WITHOUT A TRAILING
        // NEWLINE, so `wc -l` and a plain `sed -n '1,212p'` both lose its last row — which is this
        // ladder's 74th-level rung. It is fourteen rungs like everything else in the file. `--check`
        // reads the file properly and caught it; the eyeball did not.
        new(WitchesScarecrow, "Witches Scarecrow", BaseClass.Mage, SkillEffect.Fear,
            MpCost: NukerHeavyMp[0], CastTicks: 20, CooldownTicks: 50, Range: 900, Power: 0,
            DurationTicks: 100, BuffKey: "fear", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            DebuffLandMod: 0.5f,   // his CSV: "(success chance x0.5)" = 25% at parity
            Description: "Terrifies the target for 10s — it can still run, but it cannot attack or cast.",
            // Like Bind, NOTHING but the price moves: a fear is a fear, and what the ladder buys is the
            // level contest (DebuffLandChance reads the RUNG's learn level).
            Levels: HealerRungs(0, 14, (i, sp) =>
                new SkillLevel(MpCost: NukerHeavyMp[i], SpCost: sp,
                    Description: "Terrifies the target for 10s."))),

        // ═══ THE HUMAN'S CANCEL ══════════════════════════════════════════════════════════════════
        //
        // ⚠ HIS ROW SAYS `0,self` FOR RANGE AND TARGET AND BOTH ARE COPY-PASTE FROM PHASE SHIFT ABOVE
        // IT (identical MP column, 96/116/138). The DESCR says "of the TARGET", and a self-cast that
        // strips your own buffs is not a skill. Built as an enemy cast at spell range; his CSV row was
        // corrected in the same commit and the change is flagged in the report.
        new(ArcaneVoid, "Arcane Void", BaseClass.Mage, SkillEffect.Cancel,
            MpCost: 96, CastTicks: 40, CooldownTicks: 300, Range: 900, Power: 0,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 74000,
            DebuffLandMod: 0.3f,   // his CSV: "(success chance x0.3)" = 15% at parity, and his
                                   // Comment column says "lower success rate" on all three rows.
            DispelCount: 2,
            Description: "Tears the blessings off an enemy — but rarely.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 96,  SpCost: 74000,  DispelCount: 2,
                    Description: "A chance to strip 1-2 positive effects."),   // 52
                new SkillLevel(MpCost: 116, SpCost: 170000, DispelCount: 3,
                    Description: "A chance to strip 2-3 positive effects."),   // 62
                new SkillLevel(MpCost: 138, SpCost: 650000, DispelCount: 4,
                    Description: "A chance to strip 2-4 positive effects."),   // 72
            }),

        // ═══ THE SIEGE NUKE ══════════════════════════════════════════════════════════════════════
        //
        // 🔑 A 5-SECOND CAST ON A 300-SECOND REUSE. Under the 0.84.0 interrupt model that is survivable
        // in a way it was not under 0.83.0's DPS contest — reuse is no longer an input, so the game's
        // biggest nuke is not automatically the easiest cast to break. Five seconds still eats five
        // seconds of incoming hits, which is the honest price and the one he wanted.
        //
        // ⚠ HIS SP COLUMN READS `4000` ON THE @62 AND @70 ROWS. That is a typo against an SP ladder
        // that reads 170k and 390k at those bands for every other family in the file, and 880k on this
        // skill's own last rung — a 4,000 SP spell at 62 would be free. Corrected to the band price and
        // his CSV rows fixed with it. See [[ladders-are-always-monotonic]].
        new(Thunderstorm, "Thunderstorm", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 178, CastTicks: 50, CooldownTicks: 3000, Range: 900, Power: 178,
            Category: SkillCategory.Magic, SpCost: 170000,
            ConsumableId: ItemCatalog.ElementalStone, ConsumableAmount: 3,
            Description: "Calls down a storm on one enemy. Five seconds to cast, five minutes to reuse, "
                       + "three Elemental Stones.",
            Levels: new[]
            {
                new SkillLevel(Power: 178, MpCost: 178, SpCost: 170000,
                    Description: "Storm damage, power 178. Consumes 3 Elemental Stones."),   // 62
                new SkillLevel(Power: 204, MpCost: 204, SpCost: 390000,
                    Description: "Storm damage, power 204. Consumes 3 Elemental Stones."),   // 70
                new SkillLevel(Power: 216, MpCost: 216, SpCost: 880000,
                    Description: "Storm damage, power 216. Consumes 3 Elemental Stones."),   // 74
            }),

        // ═══ THE THREE LEVEL-74 BURSTS — ONE PER RACE ════════════════════════════════════════════
        //
        // 🔑 `SureHit` IS "NEVER FIZZLE", and it already existed: the fizzle roll reads `def.SureHit ?
        // 0f : …` in all three landing arms. These three are the only spells in the game that cannot
        // fizzle, which is what a 300s-reuse capstone is worth. Their RIDER still rolls — at ×1.5, his
        // "75% at parity" — so a burst always damages and usually debuffs.

        // ⚠ "Decrease SPT resistance by 40%" is a NEGATIVE CcResistMagical. It needs no new primitive:
        // RecomputeDerived already sums the buff field and clamps the result to [0, 0.8], so this eats
        // up to 40 points of the target's Clarity/Fortitude and can never push it below zero.
        new(ArcaneBurst, "Arcane Burst", BaseClass.Mage, SkillEffect.MagicDamage,
            // ⚠ HIS DURRATION CELL READS 0 on this row while Frost Burst and Pyro Burst — the same
            // skill for the other two races, same MP, same SP, same reuse — both read 15. A resistance
            // debuff that lasts zero seconds is not a debuff, so this is 15s and his row was corrected
            // with it. Flagged in the report: if he meant the Human's burst to be pure damage with no
            // rider, the fix is to delete the CcResistMagical line, not to restore the 0.
            MpCost: 150, CastTicks: 10, CooldownTicks: 3000, Range: 900, Power: 150,
            DurationTicks: 150, BuffKey: "arcane_burst", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 880000,
            SureHit: true, DebuffLandMod: 1.5f,
            CcResistMagical: -0.40f,
            Description: "Raw arcane force that never fails, and tears open the target's resistance to "
                       + "everything else you cast.",
            Levels: new[]
            {
                new SkillLevel(Power: 150, MpCost: 150, SpCost: 880000, CcResistMagical: -0.40f,
                    Description: "Power 150, never fizzles, and cuts SPT resistance by 40% for 30s."),
            }),

        // Freeze = Root + a 30% M.Def cut, one buff, so a cure that lifts the hold lifts both.
        new(FrostBurst, "Frost Burst", BaseClass.Mage,
            SkillEffect.MagicDamage | SkillEffect.Root | SkillEffect.BuffMagicDef,
            MpCost: 150, CastTicks: 10, CooldownTicks: 3000, Range: 900, Power: 150,
            DurationTicks: 150, BuffKey: "frost_burst", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 880000,
            SureHit: true, DebuffLandMod: 1.5f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -0.30f) },
            Description: "Encases the target in ice: it cannot move and its magic defence shatters.",
            Levels: new[]
            {
                new SkillLevel(Power: 150, MpCost: 150, SpCost: 880000,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, -0.30f) },
                    Description: "Power 150, never fizzles, and freezes for 15s: −30% M.Def, cannot move."),
            }),

        // 🔑 BURN NEEDED NO NEW PRIMITIVE AT ALL — it is `Cancellable: false`. His note on the row:
        // *"new effect like a DOT but its not poision nor bleed nor venom -> burn = true dmg per second
        // no cure and can kill"*. All four halves were already in the engine: TickDots calls ApplyDamage
        // with a RAW number (no defence — every DoT here is already true damage) and calls Kill if the
        // bar empties, and `Cancellable: false` is the existing "this effect cannot be cured or
        // cancelled" switch that DispelFrom honours. The flag stays `Poison` for the tick and for the
        // ATK-vs-SPT contest; what makes it a BURN rather than a poison is that nothing lifts it.
        // ⚠ Grep the engine before declaring a primitive missing — see [[sp-bottle-broker-and-stones]].
        //
        // ⚠ The heal/mana half is TWO channels, and only one of them existed. `DebuffHealRecv` already
        // cut healing RECEIVED; `MpReceivedPct` is its new mana twin and it multiplies the SAME
        // `Entity.RestoreMpMod` that the robe mastery's mpWhenRestored raises — so a burning mage's
        // Restore Spirit is cut by exactly the number his row names. His row asks for both halves
        // ("decrease hp/mp received by 70%"); either alone is half a skill.
        new(PyroBurst, "Pyro Burst", BaseClass.Mage,
            SkillEffect.MagicDamage | SkillEffect.Poison | SkillEffect.DebuffHealRecv,
            MpCost: 150, CastTicks: 10, CooldownTicks: 3000, Range: 900, Power: 150,
            DurationTicks: 150, BuffKey: "pyro_burn", Rank: 1,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Magic, SpCost: 880000,
            SureHit: true, DebuffLandMod: 1.5f,
            Cancellable: false, DotPower: 100, MpReceivedPct: 0.70f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.70f) },
            Description: "Sets the target alight: 100 damage a second that nothing can put out, and "
                       + "almost no healing or mana will reach them.",
            Levels: new[]
            {
                // ⚠ THE IMPACT AND THE BURN ARE TWO DIFFERENT NUMBERS, and this is the first skill in
                // the game where they part company. Every DoT until now took its per-second damage from
                // the skill's own Power, because a bleed IS its DoT; Pyro Burst hits for 150 on impact
                // and then burns for HIS 100 a second. Hence `DotPower` on the def — 0 still means
                // "use Power", so nothing else in the catalog moved.
                new SkillLevel(Power: 150, MpCost: 150, SpCost: 880000, DotPower: 100,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.70f) },
                    Description: "Power 150, never fizzles, then burns for 100/s for 15s — uncurable — "
                               + "and cuts healing and mana received by 70%."),
            }),
    };
}
