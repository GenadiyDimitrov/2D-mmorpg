namespace Game.Shared;

/// <summary>What a RANK is worth — the elite/boss multipliers that <c>GameLoopService.BuildMob</c>
/// records on a creature and <c>Entity.ApplyMobScale</c> re-applies at the end of every recompute.
///
/// <para>🔑 THIS EXISTS SO THERE IS ONE COPY. The numbers used to be four literals inside BuildMob
/// and a second, hand-copied set inside <c>tools/BalanceMatrix</c> — so the tool that measures a boss
/// and the code that spawns one could disagree without either being edited. They now read the same
/// functions, which is the only way "measure, don't derive" stays true across a retune.</para>
///
/// <para><b>BL-13 — a boss is 10 to 30 minutes, and the target RISES.</b> Owner, playtest 25:
/// *"It's a Boss the bosses should take 10-15 even 30 mins to kill (depending on the gear). It should
/// feel hard but rewarding .. A 3 min boss is not a boss its a stronger elite mob .. Bosses should
/// have stronger defences, more atk (not one shooting but a tank can feel it), A healer, tank and dds
/// in a party are a must"*. Three things came out of that, and all three live here:</para>
/// <list type="number">
/// <item>The HP multiplier is a CURVE, not a flat ×100 — see <see cref="Hp"/>.</item>
/// <item>A rank now raises DEFENCE (<see cref="Def"/>), which it never did: a boss was pure HP, which
///       is exactly what makes a creature read as a sponge instead of as something armoured.</item>
/// <item>Attack is a BAND, not a bigger number — see <see cref="Atk"/>.</item>
/// </list></summary>
public static class MobRankScale
{
    // ---------------------------------------------------------------------------------------------
    //  HP — why a FLAT multiplier could never work, whatever number you put in it.
    //
    //  A creature's base pool is quadratic in level (MobBaseStats.Hp = 40 + 0.8·L²) while a party's
    //  DPS is roughly FLAT across the game, because gear tracks level: measured, a 3-DD party does
    //  ~370 at 20, ~350 at 40, ~300 at 60, ~460 at 85. So a constant ×100 makes time-to-kill grow with
    //  the square of the level, and it measured exactly that way (0.88.2, before this change):
    //
    //      lvl 20   96s  ·  lvl 40  376s  ·  lvl 60  972s  ·  lvl 76 1514s  ·  lvl 85 1256s
    //
    //  His band is 600-1800s. The top of the game was already inside it; the BOTTOM was 6× too fast,
    //  and a level-20 "boss" that dies in a minute and a half is the "stronger elite mob" he named.
    //  Raising the flat number would have pushed the top out of the band to fix the bottom.
    //
    //  So the multiplier DECAYS with level, at a rate that cancels most of the base curve's growth and
    //  leaves a gentle rise — his *"the target rises"*. It is one smooth function for the same reason
    //  MobBaseStats is: instances and future ranks derive from it, and a kink is inherited.
    //
    //  ⚠ TUNED BY MEASUREMENT, not derived — tools/BalanceMatrix prints the whole table (BL-13) with
    //  the real party, the real gear and the real formulas. Re-measure after ANY change to gear, the
    //  mob curve or the damage model; these two constants are where the answer goes.
    private const float BossHpA = 43_000f;   // scale
    private const float BossHpK = 1.49f;     // decay exponent

    /// <summary>The rank's HP multiplier at this level. Elite is flat (an elite is trash-plus, and his
    /// complaint was never about elites); a BOSS decays — see the block above.
    ///
    /// <para>⚠ It is floored at ×20 so the shape can never invert at the very top of a future 90+
    /// world: a boss must always be a boss. Today the floor is never reached (×27 at 85).</para></summary>
    public static float Hp(MobRank rank, int level) => rank switch
    {
        MobRank.Elite => 4f,
        MobRank.Boss  => MathF.Max(20f, BossHpA / MathF.Pow(MathF.Max(1, level), BossHpK)),
        _             => 1f,
    };

    /// <summary>The rank's ATTACK multiplier (both channels).
    ///
    /// <para>His clause is *"more atk (not one shooting but a tank can feel it)"* — a BAND, not a
    /// number: the blow must be felt by a tank and must not delete a robe. So it is measured at BOTH
    /// ends, against his own party, in tools/BalanceMatrix.</para>
    ///
    /// <para>🔴 <b>THE BOSS CAME DOWN FROM ×10 TO ×4, AND THAT IS A NUMBER OF HIS I AM MOVING — here is
    /// why.</b> The ×10 is playtest-20's *"P.Atk from x5 -> x20"*, taken as his RATIO (×4) off the real
    /// base of the day. Two things have happened to it since. First, <b>the ground moved underneath
    /// it</b>: 0.73.0 refitted the creature attack curve ~×1.65 upward (BL-78), so ×10 on today's base
    /// is ~×16.5 in the units he ruled in — his own ratio, in today's units, is about ×6. Second, and
    /// this is the part no ratio can settle, <b>his other clause makes the number unpayable</b>:
    /// *"A healer, tank and dds in a party are a must"*. Measured at 76, a boss at ×10 puts <b>752 dps
    /// through a shielded Knight while a Lightbringer's best heal sustains 391</b> — the party he
    /// prescribes loses its tank in thirteen seconds, so a 10-to-30-minute fight is not merely hard,
    /// it is arithmetically impossible. ×4 is the largest multiplier that leaves the healer headroom
    /// (77% of his ceiling at 76, 83% at 85) AND leaves a robe alive through one basic attack (80% of
    /// its pool). At ×10 a boss's ordinary swing killed a robe TWICE OVER at every level from 40 up —
    /// which is *"one shooting"* in the plainest sense of his words.</para>
    ///
    /// <para>⚠ Re-measure this the moment heal powers move (BL-16 is still owed) or the robe pool
    /// changes (BL-78's third clause, which is his to rule): both ends of this band are somebody
    /// else's number, and this one exists to sit between them.</para></summary>
    public static float Atk(MobRank rank) => rank switch
    {
        MobRank.Elite => 1.5f,
        MobRank.Boss  => 4f,
        _             => 1f,
    };

    /// <summary>The rank's DEFENCE multiplier, P.Def and M.Def alike — NEW in BL-13.
    ///
    /// <para>🔑 A boss had no defence term at all: rank was HP and attack only, so a "boss" was the
    /// same paper armour as the trash around it wearing a hundred times the health bar. That is the
    /// mechanical reason a boss fight read as a sponge, and it is the half of his sentence
    /// (*"stronger defences"*) that no number in the game expressed.</para>
    ///
    /// <para>The ladder is deliberately the SAME one the control contest already uses
    /// (<see cref="StatCaps.CcRankMult"/> — elite ×1.33, boss ×2): a rank is one idea, and a creature
    /// that is twice as hard to hold should be twice as hard to cut. Kept as its own function rather
    /// than a call into StatCaps so that retuning the fight never silently retunes the contest.</para>
    ///
    /// <para>⚠ It costs time-to-kill roughly ×2 on a boss, which is why the HP curve above is fitted
    /// AFTER it and not before: defence buys the difficulty, HP buys the length, and the two must be
    /// measured together or you pay for the same minutes twice.</para></summary>
    public static float Def(MobRank rank) => rank switch
    {
        MobRank.Elite => 1.33f,
        MobRank.Boss  => 2.0f,
        _             => 1f,
    };

    /// <summary>Flat accuracy by rank — a boss must be able to land on a dodge build (his playtest-20
    /// *"Acc +20"*). Flat, and applied after the template's own Accuracy multiplier, so a boss gets it
    /// whole rather than scaled by whatever passive the template happens to carry.</summary>
    public static int AccFlat(MobRank rank) => rank == MobRank.Boss ? 20 : 0;
}
