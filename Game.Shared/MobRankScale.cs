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

    // ═══ `BL-167` / `BL-168` — THE OWNER'S TWO LADDERS, 2026-09-05 ═══════════════════════════════
    //
    //  His ruling, verbatim: *"bosses as we desided get x2 atk and x10hp, if boss is solo gets another
    //  x2 on both (atk/hp)"*. So there are exactly two knobs and they multiply the numbers already
    //  fitted below rather than replacing them:
    //
    //      EVERY boss ....... x2 attack, x10 HP    (BossRuneAtk / BossHpMult — "all bosses use our
    //                                               war/spell Runes ug-SS which doubles their p/m.atk")
    //      a SOLO boss ...... x2 more on BOTH      (BossSoloMult — the "solo boss" passive, for a
    //                                               boss with no escort of 2-5 fighters beside it)
    //
    //  🔑 WHY x10 HP IS NOT A CONTRADICTION OF `BL-13` ABOVE, even though the block above fitted this
    //  curve to 600-1800s. `BL-169` (2026-09-05) found that the party those seconds were measured with
    //  was a STRAW PARTY — an unbuffed 2nd-class Knight tank and four unbuffed DDs, because the census
    //  had never been taught about the NPC buffer. Re-measured with the party people actually play,
    //  party dps at 90 is 1,752 rather than 270 and a boss died in 196 SECONDS, not 21 minutes. So the
    //  curve below was never wrong; the stopwatch was. x10 puts a level-90 boss at 3.4M HP / ~33 min
    //  and a solo one at 6.9M / ~65 min — which is also, exactly, what he read off IG in the first
    //  place (*"bosses 85 with 3~6kk hp not like out 350k"*). The x20 end sits above his 10-30 minute
    //  band on paper; he ruled it anyway and said why: *"I just want to feel it and going solo vs boss
    //  to be nearly impossible"*. ⚠ RE-MEASURE THE BAND, don't re-derive it, once there is a real party
    //  to measure with — his *"one day we will make a party of bots to help me fight a Boss"*.
    //
    //  ⚠ THE ATTACK LADDER IS MEASURED ON HIS OWN CHARACTER, not on the rig: a level-90 boss against
    //  his Paladin (2,600 P.Def / 17k HP at epic A, 4,300 / 20k at mythic S, blocking 75% of the time)
    //  costs him 2.1% of his pool per swing today and 1.1% at mythic S — the endgame tank is TWICE as
    //  immune as the mid-tier one, which is the ratchet these two knobs exist to break. At x4 a solo
    //  boss takes 8.6% / 4.2% a swing, and the enrage ladder in GameLoopService carries it from there.
    //  ═══ AND A THIRD RUNG ABOVE THEM: THE WORLD BOSS (2026-09-05, same day) ═════════════════════
    //
    //  🔑 A WORLD BOSS IS A KIND OF ENCOUNTER, NOT A RARE SPAWN. His correction, after 0.113.0 had put
    //  the Valley Treant on the world ladder because it respawns every 21 hours: *"The treat is field
    //  boss (same as dungeon one) world boss is a clan/party of clans mass pvp massacre where the boss
    //  is the target ... and that boss will have about x2~3 aditional stats and x10 additional hp ...
    //  So now if boss have 28k p atk/6kk hp a world one will have 50~60k p atk and 120kk~180kk
    //  hp(6kk x2~3 x10) so several parties can fight it while fighting others for the best loot in the
    //  game"*.
    //
    //  His arithmetic is relative to a SOLO boss — 6kk is what a level-90 solo boss carries after the
    //  x20 above — so that is the base these two multiply, and his parenthesis "(6kk x2~3 x10)" is the
    //  HP rung exactly: x2~3 AND x10, i.e. x20-x30 on a solo boss.
    //
    //  ⚠ 2.5 IS THE MIDPOINT OF A RANGE HE LEFT OPEN ("about x2~3"), not a fitted number, and it is ONE
    //  constant to move if he wants a different point in it. At level 90 it gives a world boss
    //  ~172M HP against the 120-180kk he quoted, and ~128k P.Atk. ⚠ His P.Atk example ("28k -> 50~60k")
    //  reads as x2 off an ESCORTED boss rather than a solo one, which would be ~51k and is what our
    //  SOLO boss already carries; the HP half of his own sentence is unambiguous, so the solo base is
    //  what both rungs use here. If he wants world P.Atk near 60k instead of 128k, that is this
    //  constant at 1.0 — worth asking before a world boss is ever authored.
    private const float BossHpMult    = 10f;   // his x10, on every boss
    private const float BossRuneAtk   = 2f;    // his x2 attack, on every boss (the War/Spell Rune)
    private const float BossSoloMult  = 2f;    // a boss with NO escort: x2 more on attack AND HP
    private const float WorldStatMult = 2.5f;  // world boss: his "x2~3 additional stats", over SOLO
    private const float WorldHpMult   = 10f;   // world boss: his "x10 additional hp", on top of that

    /// <summary>The rank's HP multiplier at this level. Elite is flat (an elite is trash-plus, and his
    /// complaint was never about elites); a BOSS decays — see the block above — and then takes his
    /// ×10, and ×2 again when <paramref name="solo"/>.
    ///
    /// <para>⚠ It is floored at ×20 (before his multipliers) so the shape can never invert at the very
    /// top of a future 90+ world: a boss must always be a boss. Today the floor is never reached.</para>
    ///
    /// <para><paramref name="solo"/> comes from <see cref="BossCatalog.IsSolo"/> — a boss with no
    /// escort. It is meaningless on any other rank and is ignored there.</para></summary>
    public static float Hp(MobRank rank, int level, bool solo = false, bool world = false) => rank switch
    {
        MobRank.Elite => 4f,
        // ⚠ A WORLD boss is measured off the SOLO number whatever its own `Solo` flag says — his
        // "(6kk x2~3 x10)" starts from the solo pool, and a world boss is by definition the one thing
        // several parties converge on, so "does it have an escort" is not a question about it.
        MobRank.Boss  => MathF.Max(20f, BossHpA / MathF.Pow(MathF.Max(1, level), BossHpK))
                         * BossHpMult
                         * (solo || world ? BossSoloMult : 1f)
                         * (world ? WorldStatMult * WorldHpMult : 1f),
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
    /// <para>🔑 <b>AND IT IS ×2 AGAIN SINCE 2026-09-05, ×2 MORE FOR A SOLO BOSS</b> (`BL-167`). The ×4
    /// below is still the fitted base and is deliberately left as its own number; what multiplies it is
    /// <see cref="BossRuneAtk"/> (every boss carries a War/Spell Rune) and <see cref="BossSoloMult"/>
    /// (no escort). The paragraph above says ×4 leaves the healer headroom — that measurement was made
    /// against the straw party `BL-169` retired, and re-measured against the party people actually
    /// play, a boss at ×4 cost the owner's own tank 2.1% of his bar per swing and could not kill him in
    /// five minutes of standing still unhealed.</para></summary>
    /// <para>🔴🔑 <b>THE ELITE IS ×3.0 SINCE 2026-09-12 (`BL-212`), AND IT IS HIS ×2 ON TOP OF THE
    /// ×1.5 THAT WAS HERE.</b> Playtest ruling, verbatim: *"elits should get (x2 p atk on what they have
    /// now) so elit with the double in dmg and double in patk should do ~x4 dmg as of now ..and their
    /// hit will be noticed"*. The other half of that sentence is <see cref="MobDamageOut"/>, the global
    /// ×2 on every creature's finished damage — the two compose to his ×4 on a BASIC attack, which is
    /// what an elite mostly throws.</para>
    ///
    /// <para>⚠ ON A MOB SKILL IT IS LESS THAN ×4, and that is the ratio formula, not a bug: damage is
    /// <c>77·(pAtk + power)/pDef</c>, so doubling pAtk doubles the whole numerator only while `power` is
    /// zero. That is exactly why he asked for the GLOBAL half as damage rather than as P.Atk — see
    /// <see cref="MobDamageOut"/>.</para></summary>
    public static float Atk(MobRank rank, bool solo = false, bool world = false) => rank switch
    {
        MobRank.Elite => 3.0f,
        MobRank.Boss  => 4f * BossRuneAtk
                         * (solo || world ? BossSoloMult : 1f)
                         * (world ? WorldStatMult : 1f),
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
    /// <para>🔑 A WORLD boss takes his ×2~3 here too — *"about x2~3 aditional stats"* is every stat, not
    /// the attack columns alone, and a mass-PvP objective that several parties are meant to grind on
    /// while fighting each other needs the defence as much as the pool. The `solo` ×2 does NOT reach
    /// defence for anyone: his solo rule was explicitly *"x2 on both (atk/hp)"*.</para></summary>
    public static float Def(MobRank rank, bool world = false) => rank switch
    {
        MobRank.Elite => 1.33f,
        MobRank.Boss  => 2.0f * (world ? WorldStatMult : 1f),
        _             => 1f,
    };

    // ═══ `BL-212` — THE GLOBAL CREATURE DAMAGE MULTIPLIER, 2026-09-12 ════════════════════════
    //
    //  *"mobs should get x2 power - after the last update the dmg of monsters get diminished we need
    //    to increase it (not patk just dmg)"*
    //
    //  🔑 WHAT "THE LAST UPDATE" WAS, so the number is understood and not re-derived later.
    //  `BL-185` (0.117.0) gave the PHYSICAL channel the level term M.Def had always had —
    //  StatCalculator.PhysicalDefenceLevelMod — so a defender's P.Def is now multiplied by
    //  `(level + 89)/100`. At level 90 that is ×1.79, and creature damage against him fell by 44%
    //  overnight. IG carries levelMod on both sides where they largely cancel; ours cancels on the
    //  PLAYER's attack (PhysicalAttackFromWeapon has the same term) and does NOT on a creature's,
    //  whose attack curve is a bare `a·(L+31)^4.539`. This ×2 is the correction for that asymmetry,
    //  and it lands slightly above it on purpose — he wants the hit felt, not merely restored.
    //
    //  🔑 WHY IT IS DAMAGE AND NOT P.Atk, IN HIS OWN WORDS: *"not patk just dmg"*. Two reasons, both
    //  real. (1) The creature attack curve is FITTED TO IG off 2,831 measured monsters (`BL-78`,
    //  MobBaseStats.PAtk); moving it makes every future comparison to IG lie, and the number is on the
    //  target-inspect panel. (2) Damage is a RATIO — `77·(pAtk + power)/pDef` — so doubling P.Atk is
    //  ×2 on a basic attack and LESS on any skill that carries power. A multiplier on the finished
    //  number is exactly ×2 for everything a creature does. His instinct was the correct one.
    //
    //  ⚠ IT IS APPLIED IN ONE PLACE: `GameLoopService.FinalizeDamage`, the central damage-OUT
    //  pipeline, gated on the ATTACKER not being a player. So it reaches basic attacks, mob skills and
    //  mob spells alike, and it deliberately does NOT reach a player's reflected damage (which never
    //  enters that pipeline) or a training dummy. Never re-apply it at a call site.
    //
    //  ⚠ A BOSS TAKES IT TOO. His sentence says "mobs", and a boss is a creature: the boss ladder
    //  (×4 × rune × solo) is a multiplier on the same base and rides on top of this. He measured bosses
    //  as *"OK for now ... They do ok dmg no1 survives"* BEFORE this change — re-measure them, and if a
    //  boss now overshoots, the knob to move is `Atk`'s boss rung, not this one.

    /// <summary>Where the endgame ramp starts and ends, and how much it adds on top of the level
    /// term. His 2026-09-12 spec: *"&lt;76 to restore what they lost (be as it was before bl185) and 76
    /// to become harder"*, landing on *"~150% more"* at the level he plays.</summary>
    private const int   EndgameFrom  = 76;
    private const int   EndgameTo    = 90;
    private const float EndgameExtra = 0.40f;

    /// <summary>The multiplier on a creature's FINISHED damage against a PLAYER (`BL-212`). Read the
    /// DEFENDER's level: what this undoes lives in the defender's own P.Def/M.Def.
    ///
    /// <code>
    /// mult(L) = levelMod(L) × (1 + 0.40 × clamp((L−76)/14, 0, 1))
    ///           L 20 → ×1.09    L 52 → ×1.41    L 76 → ×1.65    L 90 → ×2.51
    /// </code>
    ///
    /// <para>🔑 <b>BELOW 76 IT IS EXACTLY THE LEVEL TERM, AND THAT IS THE WHOLE POINT.</b> `BL-185`
    /// gave the physical channel the defender level term M.Def had always had, so a player's P.Def is
    /// multiplied by <c>(level+89)/100</c> and creature damage fell by exactly <c>1/levelMod</c> — 8% at
    /// 20, 29% at 52, 44% at 90. Multiplying by the same number puts back precisely what was taken and
    /// not one point more, at every level. The first `BL-212` shipped a flat ×2, which restored 8% and
    /// then added 84% on top of it at level 20; his correction: *"Let's make it lvl mod as u said."*</para>
    ///
    /// <para>🔑 <b>AND ABOVE 76 IT DELIBERATELY OVERSHOOTS.</b> *"76 to become harder."* His own
    /// arithmetic, measured against his level-90 mage and quoted to the number: a normal creature hit
    /// him for ~130 and an elite for ~300; he wants ~300 and ~750, with the elite's doubled P.Atk
    /// carrying it to ~1500. ×2.51 at 90 delivers 326 / 752 / 1504. The ramp is linear across the last
    /// fifteen levels rather than a step at 76, because a cliff at the class change is the thing
    /// `BL-170` already complains about.</para>
    ///
    /// <para>🔴 <b>A BOSS TAKES NONE OF IT</b> — *"Bosses to compensate with their passive so they
    /// won't change after the base increase"*. Expressed as an exemption rather than as a division of
    /// <see cref="Atk"/>'s boss rung, because this multiplier is LEVEL-SHAPED and that rung is not: a
    /// single constant there could only cancel it at one level. The observable result is his, exactly —
    /// nothing about a boss moves. ⚠ An ELITE is not exempt; it takes this AND its own ×2 attack.</para>
    ///
    /// <para>⚠ GUARDS AND TOWERS RIDE IT TOO. They are creatures, they are what a PK meets, and their
    /// own <c>MobMod.PAtk</c> multiplies on top as it always did.</para></summary>
    /// <param name="rank">The ATTACKER's rank — only <see cref="MobRank.Boss"/> changes the answer.</param>
    /// <param name="targetLevel">The DEFENDER's level, because the term being undone is the defender's.</param>
    public static float DamageOut(MobRank rank, int targetLevel)
    {
        if (rank == MobRank.Boss) return 1f;
        float levelMod = StatCalculator.LevelMod(targetLevel);
        float t = Math.Clamp((targetLevel - EndgameFrom) / (float)(EndgameTo - EndgameFrom), 0f, 1f);
        return levelMod * (1f + EndgameExtra * t);
    }

    /// <summary>Flat accuracy by rank — a boss must be able to land on a dodge build (his playtest-20
    /// *"Acc +20"*). Flat, and applied after the template's own Accuracy multiplier, so a boss gets it
    /// whole rather than scaled by whatever passive the template happens to carry.</summary>
    public static int AccFlat(MobRank rank) => rank == MobRank.Boss ? 20 : 0;
}
