namespace Game.Shared;

/// <summary>
/// Stat formulas live in Shared so the client can *predict* (tooltips,
/// estimated damage) while the server stays the only authority.
/// The base stat table is GetBaseStats below — authored by the owner and mirrored in
/// `docs/data/classes_skills_csv/README.md` § Classes and Races → Stats.
/// </summary>
public static class StatCalculator
{
    public readonly record struct BaseStats(int Con, int Atk, int Wit, int Agi, int Spt);

    public static BaseStats GetBaseStats(Race race, BaseClass cls) => (race, cls) switch
    {
        // BaseStats(Con, Atk, Wit, Agi, Spt). Atk = the single power stat: STR for fighters,
        // INT for mages. SPT (Spirit) is a FULL stat like the rest — the retired MEN, made
        // visible and investable.
        //
        // 🔑 EVERY COLUMN SUMS TO 153. That is the one balancing rule this table has, and it is the
        // owner's (2026-08-28): a race is a REDISTRIBUTION of the same 153 points, never a bigger
        // pile. Before that ruling the six columns were 153/153/150 and 148/141/162 — the elf mage
        // was 21 points behind the demon mage for no stated reason. ⚠ If you change one number
        // here you MUST take it out of another cell in the SAME column, and update
        // `docs/data/classes_skills_csv/README.md` in the same commit. `BaseStatsSumTo153` in
        // Game.Server's startup checks refuses to boot otherwise.
        //
        // The shape, in his words: *"Elf have wit/agi - demon have con/spt/int human is in
        // between"* — and with SPT at 37 the human mage is now literally the middle value of all
        // five of his stats.
        //
        // ⚠ THE DEMON MAGE'S ATK IS 42, NOT 47. It was 47 from 2026-08-21 to 2026-08-28: 41 ×
        // (25/22), the human mage's ATK scaled by IG's own mystic STR ratio, to fix his measured
        // complaint *"2h blunt ork have almost the same as 1h mace human (with 1000pdef on top)"*.
        // The 153 rule retired it — 47 put the demon MAGE's power stat above every FIGHTER in the
        // game (40/36/41), which is what he caught. His old complaint does NOT come back: at 42
        // the demon Warchanter's maul still measures +32.9% P.Atk over the human's mace-and-shield
        // (BalanceMatrix `--warchanter 90`), a clean two-hander trade rather than the +45.6% that
        // 47 bought. The price is paid by the demon NUKER, who shares the stat: his M.Atk edge over
        // the human drops from +9.8% to +1.6% while he still carries the slowest cast and the
        // lowest magic crit. That is deliberate — the demon mystic buys pool and body (CON 31,
        // SPT 41), not damage.
        (Race.Demon, BaseClass.Fighter) => new BaseStats(47, 41, 10, 28, 27),
        (Race.Demon, BaseClass.Mage)    => new BaseStats(31, 42, 19, 20, 41),
        (Race.Elf, BaseClass.Fighter) => new BaseStats(39, 36, 17, 36, 25),
        (Race.Elf, BaseClass.Mage)    => new BaseStats(25, 37, 23, 32, 36),
        (Race.Human, BaseClass.Fighter) => new BaseStats(43, 40, 14, 30, 26),
        (Race.Human, BaseClass.Mage)    => new BaseStats(29, 41, 20, 26, 37),
        _ => new BaseStats(25, 25, 25, 25, 30)
    };

    /// <summary>The 153 rule, checkable. Returns one line per race/class column that does NOT sum to
    /// 153; empty means the table is sound. Called from the server's startup checks — see the note on
    /// GetBaseStats. All 3 races x 2 base classes are authored, so this covers the whole table.
    /// ⚠ If a Race or a BaseClass is ever ADDED, add it to the loop below too — the pairs are listed
    /// by hand (see the note there), so a new one would otherwise go unchecked.</summary>
    public static IEnumerable<string> BaseStatsNotSummingTo153()
    {
        // Listed explicitly rather than via Enum.GetValues<T>() — Game.Shared also targets
        // netstandard2.1 for the Unity client, where the generic overload does not exist.
        foreach (var race in new[] { Race.Human, Race.Elf, Race.Demon })
            foreach (var cls in new[] { BaseClass.Fighter, BaseClass.Mage })
            {
                var s = GetBaseStats(race, cls);
                int sum = s.Con + s.Atk + s.Wit + s.Agi + s.Spt;
                if (sum != 153)
                    yield return $"{race} {cls}: CON {s.Con} + ATK {s.Atk} + WIT {s.Wit} + "
                               + $"AGI {s.Agi} + SPT {s.Spt} = {sum} (expected 153, off by {sum - 153:+#;-#;0})";
            }
    }

    // Per design: levels increase hp/mp (max/regen), evasion, accuracy,
    // defence, attack — nothing else. Tanks get more HP, mages more MP.

    // ----- Max HP (IG's own shape: a growth rate that STEPS AT CLASS CHANGE) ------
    //  Each level grants  g × (level + 1)  HP, and `g` steps up at every class change.
    //  That step IS the class-growth bonus IG's per-class HP tables carry — the owner's
    //  question on 2026-08-27, answered from his own two tables: a knight's rate jumps
    //  +53% at 2nd class, a mystic's only +13%.
    //
    //  Closed form (the sum of g·(L+1) telescopes):
    //      base(L) = Level1BaseHp + Σ over tiers of  g_tier × ( Q(hi) − Q(lo) ),  Q(L) = (L²+3L)/2
    //      MaxHP   = base(L) × ConHpModifier(effectiveCon)
    //  Tier edges are our class-change levels: 1-19 | 20-39 | 40-75 | 76-85.
    //  Gear/buff % and flats stack AFTER, outside, per the global rule (playtest 28).
    //
    //  🔑 The track is a PURE FUNCTION of (race, class, discipline, level) — it is not
    //  accumulated. So taking a discipline at 40 recomputes the WHOLE curve on the new
    //  track, and a Warchanter visibly gains ~22% HP the moment he class-changes. That is
    //  deliberate: it is how IG's table jump is reproduced without a discontinuity in L.
    //
    //  Fitted to IG's per-class tables (owner supplied, 2026-08-27). Error vs those tables:
    //  0% at 1 / 40 / 80, −3% at 10, +7% at 20 (worst), +5% at 50-60. His three anchors —
    //  tank@40 CON43 = 2380, buffer@40 CON31 = 1180, knight@80 CON43 = 9840 — read
    //  2414 / 1185 / 9970.

    /// <summary>Per-race+class level-1 base HP (the curve's fixed constant). Read off IG's
    /// own level-1 row; the per-race spread is IG's (demon &gt; human &gt; elf).</summary>
    public static int Level1BaseHp(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Human, BaseClass.Fighter) => 44,
        (Race.Human, BaseClass.Mage)    => 41,
        (Race.Elf,   BaseClass.Fighter) => 40,
        (Race.Elf,   BaseClass.Mage)    => 37,
        (Race.Demon,   BaseClass.Fighter) => 50,
        (Race.Demon,   BaseClass.Mage)    => 49,
        _ => 44
    };

    /// <summary>The four per-tier growth rates for a character's HP track (1-19, 20-39,
    /// 40-75, 76-85). Six tracks, keyed by DISCIPLINE where the character has one and by
    /// ARCHETYPE before 40 — which is the only way Warchanter (buffer) and Lightbringer
    /// (healer) can differ, since they share <see cref="Archetype.Healer"/>.
    ///
    /// Ordering the owner set (2026-08-27): nuker = healer &lt; buffer &lt; rogue &lt;
    /// warrior &lt; tank. Base HP at 80 reads 3230 / 3934 / 4085 / 4455 / 4901.</summary>
    public static (float T1, float T2, float T3, float T4) HpGrowth(
        BaseClass cls, Archetype? arch, Discipline? disc)
    {
        // A discipline overrides its archetype — this is the whole point of the split.
        switch (disc)
        {
            case Discipline.Warchanter:   return (0.90f, 1.02f, 1.23f, 1.23f);  // buffer: melee, fat
            case Discipline.Lightbringer: return (0.74f, 0.84f, 1.01f, 1.01f);  // pure healer
        }
        return arch switch
        {
            Archetype.Tank    => (0.95f, 1.45f, 1.51f, 1.51f),
            Archetype.Warrior => (0.86f, 1.32f, 1.37f, 1.37f),
            Archetype.Rogue   => (0.79f, 1.21f, 1.26f, 1.26f),
            Archetype.Archer  => (0.79f, 1.21f, 1.26f, 1.26f),
            Archetype.Nuker   => (0.74f, 0.84f, 1.01f, 1.01f),
            Archetype.Healer  => (0.74f, 0.84f, 1.01f, 1.01f),  // pre-40; Warchanter leaves it above
            // Before 2nd class there is no archetype: a fighter sits on the middle of the
            // three fighter tracks, a mage on the shared mystic track.
            _ => cls == BaseClass.Mage ? (0.74f, 0.84f, 1.01f, 1.01f)
                                       : (0.86f, 1.32f, 1.37f, 1.37f)
        };
    }

    /// <summary>The level term, before CON. Public because BalanceMatrix reads it directly.</summary>
    public static float HpBase(Race race, BaseClass cls, int level, Archetype? arch, Discipline? disc)
    {
        var (t1, t2, t3, t4) = HpGrowth(cls, arch, disc);
        float b = Level1BaseHp(race, cls);
        b += t1 * (HpQ(Math.Min(level, 19)) - HpQ(1));
        if (level > 19) b += t2 * (HpQ(Math.Min(level, 39)) - HpQ(19));
        if (level > 39) b += t3 * (HpQ(Math.Min(level, 75)) - HpQ(39));
        if (level > 75) b += t4 * (HpQ(level) - HpQ(75));
        return b;
    }

    /// <summary>Σ(i+1) from 2 to L, in closed form — the curve's level term.</summary>
    private static float HpQ(int level) => (level * level + 3f * level) / 2f;

    /// <summary>CON → Max-HP multiplier, IG's own curve (owner, 2026-08-27): 20 = ×1.00,
    /// 30 = ×1.25, 40 = ×1.80, 50 = ×2.58. It is far steeper than the table it replaced
    /// (which read ×1.83 at CON 50) and it is normalised at CON 20, not 30 — both halves
    /// matter, because the base table above is quoted against THIS curve. Above 50 the
    /// curve is continued geometrically at IG's own ~3.7%/point so stat swaps stay smooth.</summary>
    public static float ConHpModifier(int con) => InterpolateCurve(ConCurve, con);

    private static readonly (int stat, float mod)[] ConCurve =
    {
        (20, 1.00f), (30, 1.25f), (40, 1.80f), (50, 2.58f), (60, 3.72f),
    };

    // ----- SPT (Spirit): a FULL stat, exactly like CON/ATK/AGI/WIT ---------------
    // Owner, 2026-07-20: "if that magic number must exist we must use it as a normal standard stat."
    // MEN's problem was never that it was an int — it was an INVISIBLE int. SPT is the same number
    // made visible, investable and displayed, so it earns its place:
    //
    //   CON → Max HP  + HP regen          SPT → Max MP + MP regen + M.Def
    //
    // Everything that used to be a "±MEN bundle" (the level-40 stat swaps, set bonuses granting
    // mp+mpreg+mdef together) is now simply ±SPT. Plain percentage effects that touch only ONE of
    // the three (a robe mastery's +20% MP regen, a ManaPercent attribute roll) stay percentages —
    // they are ordinary gear, not Spirit.

    /// <summary>SPT → the Max-MP / M.Def / MP-regen multiplier. This is the old IG MEN curve, a gentle
    /// 1.16→1.65 band (NOT the steep CON curve): every class is &gt;1, fighters just have less. Because
    /// it is flat (~1.6% per point), the per-race SPT bases need WIDE gaps to express a 7% difference.</summary>
    public static float SptModifier(int spt) => InterpolateCurve(SptCurve, spt);

    private static readonly (int stat, float mod)[] SptCurve =
    {
        (20, 1.16f), (26, 1.28f), (30, 1.35f), (31, 1.36f),
        (37, 1.44f), (40, 1.49f), (45, 1.57f), (50, 1.65f),
    };

    /// <summary>Linear interpolation of a (stat → multiplier) reference table.</summary>
    private static float InterpolateCurve((int stat, float mod)[] table, int value)
    {
        if (value <= table[0].stat) return table[0].mod;
        if (value >= table[^1].stat) return table[^1].mod;
        for (int i = 1; i < table.Length; i++)
        {
            if (value <= table[i].stat)
            {
                var (s0, m0) = table[i - 1];
                var (s1, m1) = table[i];
                return m0 + (m1 - m0) * (value - s0) / (s1 - s0);
            }
        }
        return table[^1].mod;
    }

    public static int MaxHp(int con, int level, Race race, BaseClass cls,
                            Archetype? arch, Discipline? disc)
        => (int)(HpBase(race, cls, level, arch, disc) * ConHpModifier(con));

    /// <summary>Mob HP — kept on the simple linear curve. The player formula's
    /// exponential CON modifier would explode on mob-scale CON, so mobs use this.</summary>
    public static int MobMaxHp(int con, int level) => 50 + con * 4 + level * 10;

    // ----- Max MP (authentic IG: Base_MP tier curve × Spirit, like HP) --------
    //  MaxMP = (MpClassLevelMod·(L²+3L)/2 + Level1BaseMp) × SpiritModifier
    //  MP scales with SPIRIT (not WIT). Tiers tuned to the L75 raw tracks:
    //  Healer 2000 · Wizard/Nuker 1550 · Buffer 1100 · Fighter/Tank 500.

    public static float MpClassLevelModifier(BaseClass cls, Archetype? arch) => arch switch
    {
        Archetype.Healer => 0.68f,   // ~2000 @L75 raw
        Archetype.Nuker  => 0.53f,   // ~1550
        Archetype.Tank or Archetype.Warrior or Archetype.Rogue or Archetype.Archer => 0.17f,  // ~500
        _ => cls == BaseClass.Mage ? 0.50f : 0.17f   // base class, pre-2nd
    };

    public static int Level1BaseMp(BaseClass cls) => cls == BaseClass.Mage ? 40 : 15;

    public static int MaxMp(int spt, int level, float mpClassLevelMod, int level1BaseMp)
    {
        float rawBase = mpClassLevelMod * (level * level + 3f * level) / 2f + level1BaseMp;
        return (int)(rawBase * SptModifier(spt));
    }

    /// <summary>Mob MP — simple level curve (mobs aren't MP-limited; avoids the
    /// Spirit/tier machinery).</summary>
    public static int MobMaxMp(int level) => 40 + level * 6;

    // ----- Natural regen -------------------------------------------------------
    // The stat is the DOMINANT term, as in IG, where regen is
    //   base(level) × levelMod × CONbonus[CON],  CONbonus[c] = 1.03^(c − 27.632)
    // — an EXPONENTIAL in the stat. Ours used to be linear (+0.05 per point), which made CON almost
    // irrelevant: across CON 20→60 our regen moved ×1.33 where IG's moves ×3.25, so a tank and a mage
    // regenerated at nearly the same rate and CON bought you very little.
    //
    // The curve is re-centred on 40 (`stat - 40`) so the MID-RANGE value is exactly what it was before
    // (old: 1 + 40×0.05 + level×k = 3 + level×k, which is the base below). Nothing shifts for an
    // average-CON character; the change is purely that CON now separates builds.

    /// <summary>Per-point regen multiplier for CON (HP). IG's own 1.03 curve.</summary>
    public static float ConRegenBase = 1.03f;

    public static float HpRegenPerSecond(int con, int level) =>
        (3f + level * 0.1f) * (float)Math.Pow(ConRegenBase, con - 40);

    /// <summary>SPT → the MP-REGEN multiplier, and ONLY regen — Max MP and M.Def keep riding
    /// <see cref="SptModifier"/>. Owner's own curve, 2026-08-26 (`BL-92`): *"40 = 1? 25floor = 0.7
    /// 55ceiling = 1.3"*.
    ///
    /// ⚠ He wrote the step as ×0.05; the RIGHT coefficient is 0.02. His three anchors are 15 stat
    /// points apart across 0.30 of multiplier, so 0.05 would put SPT 25 at 0.25 and SPT 55 at 1.75 —
    /// nothing like the floor and ceiling he named. He accepted 0.02.
    ///
    /// It is deliberately WIDER than the Max-MP curve it replaces (which spans only 0.855→1.066 over
    /// the real stat range), so Spirit finally buys a visible amount of sustain: the demon mage gains,
    /// the elf mage loses ~10%, and EVERY fighter (SPT 25-27) sits on or beside the 0.70 floor.</summary>
    public static float SptRegenModifier(int spt) =>
        Math.Clamp(1f + (spt - 40) * 0.02f, 0.70f, 1.30f);

    /// <summary>MP regen follows SPT, not WIT (owner, 2026-07-20). WIT is cast speed and magic crit;
    /// it was never meant to be the mana stat.
    ///
    /// The base is 2, NOT the 3 the HP curve uses: both come from the old linear formula's constant
    /// term (<c>1 + stat×0.05</c>), but that has to be read at the stat's REAL range. CON sits at
    /// 36-47, so 40 → 3. WIT only ever sat at 10-23, so the honest midpoint is 20 → 2. Using 3 here
    /// would have quietly handed every build 19-36% more MP regen.
    ///
    /// ⚠ 2026-08-26 (`BL-92`): the stat term is <see cref="SptRegenModifier"/>, NOT
    /// <c>SptModifier(spt)/1.4733</c>. The old expression borrowed the Max-MP curve and renormalised
    /// it so a human mage read ×1.00; regen now has its own, wider curve and the human mage reads
    /// 0.98. The BASE was never the problem — measurement (`BalanceMatrix --mpregen`) put a buffed
    /// mage at 196-320% of his own spell-spam drain, and that came from the mastery MULTIPLIERS,
    /// which are now flats. Do not "restore" the old curve to make a number look familiar.</summary>
    public static float MpRegenPerSecond(int spt, int level) =>
        (2f + level * 0.08f) * SptRegenModifier(spt);

    // ----- MOB regen: a % of the mob's own pool, and NOTHING to do with CON --------------------
    //
    // Mobs must not use HpRegenPerSecond. Their CON is 15 + 2·level — a level-90 mob has CON 195,
    // where a player's ENTIRE range is 36-47 — so 1.03^(con−40) compounds ×1.06 per level while
    // MobBaseStats.Hp only grows as 0.8·level². Exponential against polynomial has one ending:
    //
    //     level 37:    29 HP/s  vs a 1,135 bar   (2.6%/s)
    //     level 90: 1,170 HP/s  vs a 6,520 bar   (18%/s — its whole bar every 5.6 seconds)
    //     level 200:  1.5M HP/s vs a 32,040 bar  (47× its bar per second)
    //
    // Owner, 2026-08-01: *"if I'm not top geared and start doing 100-200 the regen will overpower
    // me"*. It did, and it was arithmetic, not gear. Dividing the curve by a constant was considered
    // and rejected: ÷10 keeps it sane to about level 110 and is absurd again by 150, so it is not a
    // fix, it is the same cliff moved 40 levels along — *"I don't want to get caught balancing
    // everything for today's level range and tomorrow need rebalance for introducing higher lvls"*.
    //
    // A fraction of MAX HP has no level term at all, so there is nothing to rebalance, ever, and no
    // boss special case: 5%/s is 20 seconds to full whether the bar is 40 or five million.
    //
    // Players keep the CON curve untouched. Across their real 36-47 band it is a ×1.4 spread — which
    // is what it was designed to be. It only broke when fed a number three times larger than any
    // player will ever have.

    /// <summary>Fraction of its own Max HP a mob regenerates per second WHILE ENGAGED. Deliberately
    /// tiny: its only job is to stop a hopelessly weak attacker from chipping something down forever
    /// (a mob wedged on geometry, say). Read it as a MAXIMUM KILL TIME — a mob healing p of its bar
    /// per second cannot be killed by damage below p, so you must finish inside 1/p seconds. 0.001 =
    /// a ~16-minute wall, and that sentence stays true at every level and every HP total.
    ///
    /// It is NOT the anti-underlevelled mechanic; the level-gap table already is (75% avoid at 19
    /// levels, and pinned to the 5% band edge at 20+). This only catches the in-range chipper that
    /// gap misses.</summary>
    public static float MobHpRegenPctCombat = 0.001f;

    /// <summary>Fraction of its own pool a mob regenerates per second while NOT engaged: 5%/s, so
    /// anything is back to full 20 seconds after it drops combat. This replaced an instant full heal
    /// in ResetMob — the owner wanted the window to exist so a mob you ran from can be re-engaged
    /// while it is still hurt, instead of being pristine the moment you left its view.</summary>
    public static float MobRegenPctIdle = 0.05f;

    /// <summary>Fraction of its own Max MP a mob regenerates per second while engaged. Higher than the
    /// HP figure — mobs are not meant to be MP-limited (see <see cref="MobMaxMp"/>).</summary>
    public static float MobMpRegenPctCombat = 0.01f;

    public static float MobHpRegenPerSecond(int maxHp, bool engaged) =>
        maxHp * (engaged ? MobHpRegenPctCombat : MobRegenPctIdle);

    public static float MobMpRegenPerSecond(int maxMp, bool engaged) =>
        maxMp * (engaged ? MobMpRegenPctCombat : MobRegenPctIdle);

    // ----- Unified hit resolution (see docs/design/CombatResolution.md) -----------
    // Both channels (physical miss, magic fail) call ResolveAvoidChance. It returns
    // the probability the attack is AVOIDED (missed/fizzled). Order of operations:
    //   1. stat roll  → 2. level gap (favors higher level) → 3. class floors + the 5/95 band → 4. flags
    // Precedence top-down: Immunity > SureHit > floors + the 5/95 band > level gap > stat roll.
    //
    // ⚠ Steps 2 and 3 were the other way round until 2026-08-07 (playtest-19 M1), which made a
    // |Δ| ≥ 20 gap a HARD 100% lockout that overrode every floor: an admin with accuracy 9999 and a
    // bow could not land a single hit on a dummy 20 levels above him, and no `precision` rung changed
    // it. The owner overruled that design, and the code backs him — ExpCurve.LevelGapMultiplier
    // already pays ZERO exp AND zero drops from a 13-level gap (GapZero), seven levels before the
    // lockout even started, so the lockout protected nothing and only read as broken. Clamping LAST
    // means G = 1.0 no longer means "lockout"; it means "pinned to the edge of the band".
    // ⚠ The accepted consequence: nothing is unhittable any more. A level-1 connects with a raid boss
    // 5% of the time — for no exp, no drop, and a swift death.

    /// <summary>Level-gap penalty G(|Δ|): the avoid magnitude conferred on the
    /// HIGHER-level combatant (added to their hit AND their evade vs the lower).
    /// Piecewise-linear: white ≤5; +2.5%/lvl to 10%@9; +3%/lvl to 25%@14;
    /// +10%/lvl to 75%@19; 100% at ≥20 — which the floors/band then pull back to
    /// the edge of [5%, 95%] (or to a class floor), so it is a ceiling, not a lockout.</summary>
    public static float LevelGap(int levelDiff)
    {
        int d = Math.Abs(levelDiff);
        if (d <= 5) return 0f;
        if (d <= 9) return 0.025f * (d - 5);            // 6–9   → 2.5,5,7.5,10
        if (d <= 14) return 0.10f + 0.03f * (d - 9);    // 10–14 → 13,16,19,22,25
        if (d <= 19) return 0.25f + 0.10f * (d - 14);   // 15–19 → 35,45,55,65,75
        return 1.0f;                                    // 20+   → the band's edge (NOT a lockout)
    }

    /// <summary>The one resolver. Returns avoid (miss/fail) probability for A→D.
    /// <paramref name="defenderFloor"/> = min avoid vs the defender (rogue evade /
    /// anti-magic). <paramref name="attackerHitFloor"/> = the attacker's min hit
    /// (warrior); caps avoid at 1−floor. Level favors the higher combatant, but the
    /// class floors and the 5/95 band are applied AFTER it and therefore win.
    /// Flags override everything.</summary>
    public static float ResolveAvoidChance(
        int attackerHitStat, int defenderAvoidStat,
        float defenderFloor, float attackerHitFloor,
        int attackerLevel, int defenderLevel,
        bool sureHit = false, bool defenderImmune = false, float baseAvoid = -1f)
    {
        // The universal "always at least" avoid floor. Defaults to StatCaps.AvoidBase (5%),
        // but callers can lower it — e.g. ~1% for spells vs MOBS so players' magic doesn't
        // fizzle on mob targets (mobs have no anti-magic identity).
        if (baseAvoid < 0f) baseAvoid = StatCaps.AvoidBase;

        // 4 (checked first = highest precedence): hard overrides.
        if (defenderImmune) return 1f;
        if (sureHit) return 0f;

        // 1: stat roll, inside the soft band.
        float m = baseAvoid + (defenderAvoidStat - attackerHitStat) * StatCaps.AvoidStatSlope;
        m = Math.Clamp(m, baseAvoid, StatCaps.AvoidSoftCeil);

        // 2: level gap pushes the roll toward the higher-level combatant.
        int diff = attackerLevel - defenderLevel;
        float g = LevelGap(diff);
        if (diff > 0) m = Math.Min(m, 1f - g);     // attacker higher → cap defender avoid
        else if (diff < 0) m = Math.Max(m, g);     // defender higher → force attacker to avoid

        // 3 (LAST, so it wins): class floors form an interior window
        // [defenderFloor, 1 − attackerHitFloor], intersected with the universal 5/95 band.
        // Because this clamp runs after the gap, a 20+ level gap is pinned to the edge of the
        // band instead of locking the fight out: an L20 rogue in an L90 field still dodges its
        // 10% evade floor, an L20 warrior with Precision L1 still lands 10%, and with no floor
        // at all both sides still get the universal 5%.
        float lo = Math.Max(baseAvoid, defenderFloor);
        float hi = Math.Min(StatCaps.AvoidSoftCeil, 1f - attackerHitFloor);
        if (lo > hi) lo = hi = (lo + hi) * 0.5f;   // safety if floors ever sum >100%
        m = Math.Clamp(m, lo, hi);

        return Math.Clamp(m, 0f, 1f);
    }

    // ----- Accuracy / evasion: AGI + LEVEL (owner, 2026-08-02) --------------------------
    //
    // Both sides of the miss roll are `AGI + level`, so SAME AGI + SAME LEVEL is always the
    // 5%/95% base and one point of difference is worth exactly 1% (StatCaps.AvoidStatSlope).
    //
    // This replaced a flat `= AGI`, which was a silent disaster: a player's AGI never grows,
    // while a mob's is `10 + level`. The two crossed at level 20 and diverged 1 point per level
    // in BOTH directions at once — a naked level-90 fighter missed 75% of his swings while the
    // mob, sitting on the 5% floor, never missed him. Level now cancels out and the gear/passive
    // layer is what creates a spread: fighters buy ACCURACY, rogues buy EVASION.

    /// <summary>Physical accuracy: AGI + level (+ weapon/gear/buffs added by the caller).
    /// Cross-level effects still come from the level-gap curve in ResolveAvoidChance —
    /// this term only keeps a same-level pair honest.</summary>
    public static int Accuracy(int agi, int level) => agi + level;

    /// <summary>Physical evasion: AGI + level (+ archetype/gear/buffs added by caller).</summary>
    public static int Evasion(int agi, int level) => agi + level;

    // ----- Combat (Phase 2) -------------------------------------------------

    /// <summary>Effective attack power. Weapon damage joins this formula
    /// in the items phase: weapon + stat + buffs/passives.</summary>
    public static int AttackPower(int atkStat, int level) => atkStat + level * 2;

    // ----- P.Atk (authentic IG shape) ---------------------------------------------------------
    //
    // IG's P.Atk is MULTIPLICATIVE: `P.Atk = basePAtk(=WEAPON) × STRbonus × levelMod` (L2J FuncPAtkMod).
    // The WEAPON is the base; the power stat and level only MULTIPLY it. Unarmed, basePAtk is a tiny
    // FIST value, so you punch for almost nothing — no "if unarmed then penalty" branch is needed, the
    // formula does it. Our old form was additive (`atkStat + level·2 + weapon`), which let the 40-point
    // ATK stat leak through with no weapon (a naked L1 fighter had 42 P.Atk and one-shot trash).
    //
    // We keep ONE power stat (ATK) rather than IG's separate STR, so the "STR bonus" is a gentle
    // multiplier off ATK, centred on the fighter base. Only the P channel uses this; M.Atk keeps its
    // own (base × levelMod²) form — that's the signed-off magic balance and it is NOT touched.

    /// <summary>Fist P.Atk when unarmed — the "weapon" value with no weapon. Small on purpose.</summary>
    public const int UnarmedFistPAtk = 3;

    /// <summary>ATK value the P.Atk multiplier is centred on (≈ the human-fighter base) → ×1.0 there.</summary>
    public const int PAtkStatReference = 40;

    /// <summary>The power-stat multiplier for P.Atk. ~1.0 at the fighter base, scaling gently with ATK.</summary>
    public static float PAtkStatMult(int atkStat) => Math.Max(0.2f, atkStat / (float)PAtkStatReference);

    /// <summary>IG-shape P.Atk: (fist + weapon) × ATKbonus × levelMod. <paramref name="weaponPAtk"/> is
    /// the weapon's own P.Atk contribution (its power × the P channel factor); 0 = unarmed.</summary>
    public static int PhysicalAttackPower(int weaponPAtk, int atkStat, int level) =>
        Math.Max(1, (int)((UnarmedFistPAtk + weaponPAtk) * PAtkStatMult(atkStat) * LevelMod(level)));

    // ----- M.Atk (same MULTIPLICATIVE shape as P.Atk; owner 2026-07-25) -----------------------
    //
    // M.Atk was the ONLY channel still ADDITIVE: base = (atkStat + level·2 + weaponM), which put the
    // ~41-point power stat in as a flat FLOOR at every level. That floor dominates low levels (a lvl-1
    // wand mage read ~40 M.Atk where IG has ~8 → √-damage ~2.2× too high → one-shots) and fades to
    // nothing by the endgame — exactly the level-dependent divergence the owner measured. The WEAPON is
    // now the base and the stat MULTIPLIES it (a small fist value when unarmed), same as P.Atk, so a
    // small wand base yields a small M.Atk and the staff's big base carries the endgame. The weapon's
    // MAtkFactor still does the physical/magic split. levelMod² is applied LATER in RecomputeDerived
    // (magic keeps its own ² level term; physical bakes levelMod into the formula above).
    public const int UnarmedFistMAtk = 3;

    /// <summary>ATK value the M.Atk multiplier is centred on → ×1.0 there.</summary>
    public const int MAtkStatReference = 40;

    /// <summary>How steeply M.Atk scales with the ATK stat — the SECOND of the two stat multipliers
    /// (P.Atk is the first, and stays LINEAR = exponent 1). Magic is super-linear (owner 2026-07-25):
    /// this is the "INT is king for a mage" curve, and it's the lever that restores the endgame after the
    /// move to a weapon-based M.Atk. At 1.75 a same-tier mage matches the old signed-off endgame M.Atk
    /// (~2953 at 85 on the A-grade FLOOR staff) while level 1 stays at ~8 — because (41/40)^1.75 ≈ 1.04 at
    /// the base but (100/40)^1.75 ≈ 5.0 once gear has pushed ATK up. ⚠ It also makes +ATK stat-swaps/dyes
    /// scale magic hard; that's intended (mage identity), flag if it needs a cap.</summary>
    public const float MagicStatExponent = 1.75f;

    /// <summary>The power-stat multiplier for M.Atk — the second, STEEPER of the two channel multipliers.
    /// ~1.0 at the reference, rising super-linearly with ATK (see <see cref="MagicStatExponent"/>).</summary>
    public static float MAtkStatMult(int atkStat) =>
        Math.Max(0.2f, MathF.Pow(atkStat / (float)MAtkStatReference, MagicStatExponent));

    /// <summary>Multiplicative M.Atk base (mirrors <see cref="PhysicalAttackPower"/> without the level
    /// term): (fist + weaponMAtk) × ATKbonus. <paramref name="weaponMAtk"/> is the weapon's own M.Atk
    /// contribution (its authored M.Atk × the M channel factor); 0 = no magic weapon. RecomputeDerived
    /// then applies levelMod² on top of this, exactly as it did to the old additive base.</summary>
    public static int MagicAttackStatScaled(int weaponMAtk, int atkStat) =>
        Math.Max(0, (int)((UnarmedFistMAtk + weaponMAtk) * MAtkStatMult(atkStat)));

    /// <summary>Which LEVEL of the combat-training passive a character should hold at a
    /// given character level (auto-granted; our war rune/spell rune stand-in). 0 below
    /// 40; levels 1–8 step every 5 levels (40→1 … 75→8 = +10%…+80%); 9 from the
    /// 4th-class change (76+ = +100%). The per-level AttackPct lives in the SkillDef.</summary>
    public static int TrainingLevelFor(int level)
    {
        if (level >= 76) return 9;
        if (level < 40) return 0;
        return Math.Min(8, (level - 40) / 5 + 1);
    }

    // ----- Defence (authentic IG: armor/jewels + level² /100, no CON) ------
    //  P.Def = (naked 68 + level²/100 + Σ armor pDef + flat passives) × masteries/buffs
    //  M.Def = (naked 20 + level²/100 + Σ jewel mDef + flat passives) × MEN × buffs
    //  (armor/jewel/passive/mastery/MEN/buff stacking happens in Entity.RecomputeDerived;
    //   these provide the naked baseline + level modifier. Mobs use MobDefence.)

    /// <summary>Player base physical defence: naked baseline + level²/100. No CON.</summary>
    public static int PhysicalDefenceBase(int level) => 68 + level * level / 100;

    /// <summary>Player base magic defence: naked baseline + level²/100. Jewels and the
    /// MEN modifier apply on top in RecomputeDerived. (No base-stat term here.)</summary>
    public static int MagicDefenceBase(int level) => 20 + level * level / 100;

    /// <summary>Mob defence — kept on the old simple curve (mobs have no armor/jewels;
    /// the player naked baseline would make low-level mobs too tanky).</summary>
    public static int MobDefence(int con, int level) => con / 3 + level / 2;

    /// <summary>Crit chance from AGI. Race/class/equipment modifiers come
    /// later. 25 AGI = 10%; capped at 50%.</summary>
    public static float CritChance(int agi) => Math.Clamp(0.05f + agi * 0.002f, 0f, 0.50f);

    /// <summary>Raw damage of one basic attack before the crit multiplier.
    /// Kept for compatibility; the new ratio model is PhysicalDamage below.</summary>
    public static int BasicAttackDamage(int attackPower, int defence) =>
        Math.Max(1, attackPower * 2 - defence);

    public const float CritMultiplier = 2.0f;

    // ===== IG-style ratio damage ===========================================
    //
    // Damage is a RATIO of attack to defence (not a subtraction), so defence
    // gives diminishing returns and never fully blocks. lvlMod scales the whole
    // curve by level. Physical and magic share the shape but differ: physical
    // can be EVADED and crits up to x10; magic can FAIL (resist roll) and crits
    // up to x3. Magic currently divides by physical defence too (magic-resist
    // passives/jewels add a separate multiplier later).

    /// <summary>Level modifier: (level+89)/100. L1=0.90, L11=1.00, L80=1.69.</summary>
    public static float LevelMod(int level) => (level + 89) / 100f;

    // ----- The magic level-scaling terms (authentic IG; verified against L2J's
    //       FuncMAtkMod / FuncMDefMod, 2026-07-14) --------------------------------
    //
    //   M.Atk = base × INTbonus² × levelMod²     (BOTH squared)
    //   M.Def = base × MENbonus  × levelMod      (neither squared)
    //
    // The asymmetry is the whole trick, and we were missing it. Magic damage takes
    // √M.Atk — so squaring the level term CANCELS the square root:
    //
    //   √(levelMod²) = levelMod
    //
    // …which leaves magic damage growing LINEARLY in level, exactly like physical
    // (77·(pAtk+power)/pDef is a pure ratio). Without the square, our M.Atk was flat in
    // level (AtkStat + level·2 + weapon), so √M.Atk was flat too and magic silently fell
    // off a cliff as the numbers grew: a level-85 mage needed ~79 casts to kill a
    // same-level mob while a level-20 mage one-shot his. Same bug at both ends.
    //
    // (We have one ATK power stat rather than IG's separate INT, and it already sits in
    // the base — so only the level term is reproduced here, not INTbonus².)

    /// <summary>M.Atk level scaling: levelMod². Squared on purpose — see the note above.</summary>
    public static float MagicAttackLevelMod(int level) => LevelMod(level) * LevelMod(level);

    /// <summary>M.Def level scaling: levelMod (NOT squared — the counterpart to the above).</summary>
    public static float MagicDefenceLevelMod(int level) => LevelMod(level);

    // Damage balance constants — the authentic IG constants, unmodified.
    //  Physical: 77·(pAtk + power)/pDef
    //  Magic:    91·power·√mAtk/mDef
    // MagicK was previously 8, on the reasoning that our M.Atk is small compared to IG's.
    // That was backwards: because the formula takes √mAtk, a SMALLER M.Atk needs a LARGER
    // K, not a smaller one. The result was magic doing ~1/11th of its intended damage
    // (a L21 healer hit a same-level tank for 15). Both constants are now IG's own.
    public const float PhysicalK = 77f;
    public const float MagicK = 91f;

    /// <summary>PATH B (owner 2026-07-16): M.Atk is STORED as its displayed value = this scale · √(internal),
    /// so the cosmic `base·levelMod²` number shrinks to P.Atk size while the √ (and its level self-balancing)
    /// is preserved. <see cref="MagicDamage"/> is then LINEAR on the stored value (K/scale reproduces the old
    /// `91·power·√internal/mDef` exactly). The internal value = (shown/scale)².</summary>
    public const float MagicAttackDisplayScale = 20f;

    /// <summary>Physical ratio damage (IG model): 77·(pAtk + skillPower)/pDef. No level
    /// term (level is already baked into pAtk/pDef growth). 'power' is 0 for a basic
    /// attack, the skill's power for a skill. Crit / variance / war rune are applied by
    /// the caller. Defence floored at 1.
    /// <paramref name="defenceCoef"/> is the defender's WEAPON-TYPE resistance (see
    /// <see cref="WeaponDefenceCoef"/>): the resist rides INSIDE pDef (so a def-ignoring
    /// skill bypasses it too). >1 = resistant (less damage), &lt;1 = weak (more damage),
    /// ≤0 = no defence at all → def floors at 1 (a "one-shot" of that weapon type).</summary>
    public static int PhysicalDamage(int pAtk, int power, int pDef, int attackerLevel, float defenceCoef = 1f)
    {
        float def = Math.Max(1, pDef * defenceCoef);
        float dmg = PhysicalK * (pAtk + power) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>{Flat, Mod} PHYSICAL skill damage: K·(Flat + Mod·pAtk)/def. Mod scales the skill WITH your
    /// pAtk (gear/atk pays off through skills); Flat is a low-pAtk floor. Legacy skills reach this via
    /// (Flat=Power, Mod=1), reproducing K·(pAtk+Power)/def. See docs/design/DamageModel.md.</summary>
    public static int PhysicalDamageFM(int pAtk, int flat, float mod, int pDef, float defenceCoef = 1f)
    {
        float def = Math.Max(1, pDef * defenceCoef);
        float dmg = PhysicalK * (flat + mod * pAtk) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>Maps an ATTACKER's weapon type to the DEFENDER's matching weapon-type
    /// resistance coefficient (a multiplier on the defender's P.Def, applied only for this
    /// hit). Sword/dual → Pierce, blunt → Blunt, bow → Bow; anything else = neutral (1).
    /// IG convention: swords are neutral vs armored mobs — here Sword+Dual share the Pierce
    /// track (splittable later without touching callers).</summary>
    public static float WeaponDefenceCoef(WeaponType attacker, float pierce, float blunt, float bow) =>
        (attacker & WeaponType.AnyBlunt) != 0 ? blunt
        : attacker == WeaponType.Bow ? bow
        : (attacker & (WeaponType.AnySword | WeaponType.Dual)) != 0 ? pierce
        : 1f;

    /// <summary>Magic ratio damage (IG model): K·skillPower·√mAtk/mDef. The SQUARE ROOT
    /// of M.Atk means stacking raw M.Atk gives diminishing returns. 'power' is the
    /// spell's base power. Divides by MAGIC defence (separate channel). Crit / fail /
    /// blessed-spell rune are applied by the caller. Defence floored at 1.
    /// <para><paramref name="defenceCoef"/> is the defender's MAGIC RESISTANCE, the same shape as the
    /// weapon-type resists on the physical side (<see cref="WeaponDefenceCoef"/>): it rides INSIDE
    /// mDef, so >1 = resistant (1.25 → ×0.8 damage), &lt;1 = weak, and a defence-ignoring effect
    /// bypasses it too.</para></summary>
    public static int MagicDamage(int mAtk, int power, int mDef, int casterLevel, float defenceCoef = 1f)
    {
        float def = Math.Max(1, mDef * defenceCoef);
        // mAtk here is the INTERNAL value (base·levelMod²·buffs²). The √ stays — it is what self-balances
        // magic across levels. The DISPLAY shrinks this to P.Atk size elsewhere (EffectiveMagicAttackShown);
        // this formula is unchanged so mob casters + heals keep working. See Path B in docs/design/DamageModel.md.
        float dmg = MagicK * power * MathF.Sqrt(Math.Max(0, mAtk)) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>{Flat, Mod} MAGIC skill damage: K·(Flat + Mod·√mAtk)/mDef. Mod replaces the old scalar
    /// power (Flat is usually 0 for magic). Legacy skills reach this via (Flat=0, Mod=Power), reproducing
    /// K·Power·√mAtk/mDef exactly. <paramref name="defenceCoef"/> = the defender's magic resistance,
    /// riding inside mDef (see <see cref="MagicDamage"/>). See docs/design/DamageModel.md.</summary>
    public static int MagicDamageFM(int mAtk, int flat, float mod, int mDef, float defenceCoef = 1f)
    {
        float def = Math.Max(1, mDef * defenceCoef);
        float dmg = MagicK * (flat + mod * MathF.Sqrt(Math.Max(0, mAtk))) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>MANA DRAIN (the healer's Mana Ray) — a share of the TARGET'S OWN max MP, NOT the magic
    /// damage number. <paramref name="power"/> is the authored skill power read as PER MILLE, so his
    /// 145 = 14.5% of the pool; that keeps the CSV column and the `DESCR` text authored as an ordinary
    /// "+145 Power" and leaves `SkillCsvSeed --check` reading the same number the engine does.
    /// <para>🔑 WHY IT IGNORES M.Atk, M.Def AND mRes (owner, 2026-08-20, after four models were
    /// measured). M.Def is nearly identical across classes at level 74 (697-782) but MP POOLS differ
    /// <b>4.5×</b> — 696 on a fighter against 3158 on a healer. So any drain whose size is independent
    /// of the pool is lopsided by construction: the magic-damage model emptied a fighter in 1.2 casts
    /// while taking 5-6 on a caster. Magic resistance cannot correct that — pushed to `MagicResist`'s
    /// hard ±0.9 clamp a fighter still fell in 2.0-2.3 casts, and mRes is the same coefficient that
    /// divides real magic damage, so widening it to fix a drain would gut every nuke. A share of the
    /// pool is the only shape where one authored number means the same thing to everyone: 14.5% is
    /// 7.0 casts to zero, whoever it lands on. Re-measure with
    /// `dotnet run --project tools/BalanceMatrix -- --mana-ray &lt;power&gt; &lt;level&gt;`.</para>
    /// <para>⚠ Deliberately NOT multiplied by weapon variance: the drain no longer reads the weapon at
    /// all, and a predictable share is the whole point of the model. The PvE ×0.5, the fizzle and the
    /// magic crit still apply — those are the caller's, not this method's.</para></summary>
    public static int ManaDrain(int targetMaxMp, float power) =>
        Math.Max(1, (int)(Math.Max(0, targetMaxMp) * power / 1000f));

    /// <summary>Weapon damage variance — a ± random band. Spikier weapons (bow,
    /// dagger) get a wider band; steady weapons (blunt) narrower. Returns a
    /// multiplier around 1.0.</summary>
    public static float WeaponVariance(WeaponType weapon, System.Random rng)
    {
        float band = weapon.Base() switch
        {
            WeaponType.Bow => 0.30f,
            WeaponType.Dual => 0.20f,    // daggers/dual: spiky
            WeaponType.Blunt => 0.15f,   // blunt/staff: steadier
            _ => 0.10f
        };
        return 1f + ((float)rng.NextDouble() * 2f - 1f) * band;
    }

    // ----- Crit (split: physical vs magic, each capped) --------------------

    /// <summary>Character crit-rate BASE — his "110" on IG's 0-1000 scale, i.e. 11%.
    /// (docs/design/CritBlowAndDouble.md §5.)</summary>
    public const float CharacterCritBase = 0.110f;

    /// <summary>AGI's contribution to crit rate: a MULTIPLIER of 1% per point, centred on
    /// <see cref="MobAgiReference"/> (30) so a normal mob sits at exactly ×1.00 — the neutral
    /// opponent. LINEAR AND UNCAPPED (owner ruling 2026-08-06): a full-AGI elf archer with a
    /// light set and a stat swap can climb toward the cap, and a max-ATK warrior at AGI 23 pays
    /// ×0.93 — "a low hinder but still a hinder".
    /// 🛑 GUARDRAIL: this is deliberately the SMALLEST of AGI's four jobs. One AGI point is
    /// worth +1.0 percentage point of accuracy and of evasion, ×1.0105 of attack speed, but
    /// only +0.13pp of a dagger's crit. If it ever looks "too weak", that ratio IS the design —
    /// it is what stops AGI becoming the one stat everybody stacks. Do not inflate it.</summary>
    public static float CritAgiMod(int agi) => Math.Max(0f, 1f + (agi - MobAgiReference) * 0.01f);

    /// <summary>The BASE physical crit rate: the <c>110 × weaponFactor × agiMod</c> head of his
    /// IG model (docs/design/CritBlowAndDouble.md §5)
    /// <code>crit = (110 × weaponFactor × agiMod × buffs × passives + flat) × debuffs × enemyLightArmor</code>
    /// The WEAPON multiplies the character base — dagger/bow ×1.2 → 13.2%, sword ×0.8 → 8.8%,
    /// blunt ×0.4 → 4.4% — and AGI is a mild multiplier ON that. AGI is NOT the base any more
    /// (it used to be <c>0.05 + agi × 0.0009</c>, which made the weapon a rounding error).
    /// ⚠ Deliberately NOT clamped: passives and buffs multiply this and flat bonuses add to it,
    /// so the single clamp belongs at the END of the chain (Entity.RecomputeDerived).</summary>
    public static float PhysicalCritBase(int agi, WeaponType weapon) =>
        CharacterCritBase * WeaponCritFactor(weapon) * CritAgiMod(agi);

    /// <summary>Character MAGIC crit-rate BASE — "40" on IG's 0-1000 scale, i.e. 4%
    /// (the magic twin of <see cref="CharacterCritBase"/>; was 50 from 2026-08-06 to 2026-08-19).
    ///
    /// <para>🛑 Lowered on purpose so the CAP IS NO LONGER THE CEILING A MAGE ALREADY LIVES ON
    /// (owner ruling 2026-08-19): *"still max 20% but one day if we want to increase it no mage
    /// to be short on crit"*. At 50 the fully-kitted elf (WIT 30, ×2.00) hit exactly 20% off
    /// Insight alone, so the 4th-class crit-rate buff he is authoring would have bought him
    /// NOTHING, and raising the cap later would have bought him nothing either. At 40 the chain
    /// reads — WIT 30, i.e. elf mage + robe set +2 + stat swap +5:</para>
    /// <code>bare        8.0%      (his "about 7-8% without buffs")
    /// ×2 Insight  16.0%      (his "15-16%")
    /// ×4 buffed   32.0%  →  clamped to the 20% cap, with real headroom above it</code>
    /// and the human (WIT 27 → 6.8%) and demon (WIT 26 → 6.4%) both clear 20% at ×4 too, which
    /// was the other half of the ruling.</summary>
    public const float MagicCharacterCritBase = 0.040f;

    /// <summary>The WIT that sits at exactly ×1.00 — the HUMAN MAGE base, so an ordinary
    /// caster is the neutral reference and every point of spread comes from race, the robe
    /// set and the level-40 stat swap, where it is earned. (The physical twin is
    /// <see cref="MobAgiReference"/>, which anchors on the mob instead because a physical
    /// crit is a CONTEST; magic crit has no defender term, so it anchors on the archetype.)</summary>
    public const int MagicCritWitReference = 20;

    /// <summary>WIT's contribution to magic crit rate: a MULTIPLIER, and ASYMMETRIC by design
    /// (owner ruling 2026-08-06).
    /// <code>above 20: ×(1 + 0.10·(WIT−20))   →  WIT 30 = ×2.00, the fully-kitted elf
    /// below 20: ×(1 + 0.05·(WIT−20))   →  WIT 10 = ×0.50, WIT 5 (a mob) = ×0.25</code>
    /// 🛑 The two slopes are NOT an oversight. A symmetric 0.10 would double you over the ten
    /// points above the anchor and ANNIHILATE you over the ten below it — and real WIT values
    /// live down there (demon fighter 10, every mob 5), so the stat would hit a hard 0 well
    /// inside its own range. The gentler lower slope keeps "below 20 hinders you" true without
    /// a dead zone, and is what leaves a caster mob at a live 1.25%. Clamped at 0 all the same,
    /// so a future WIT debuff cannot drive the rate negative.</summary>
    public static float CritWitMod(int wit) =>
        wit >= MagicCritWitReference
            ? 1f + (wit - MagicCritWitReference) * 0.10f
            : Math.Max(0f, 1f + (wit - MagicCritWitReference) * 0.05f);

    /// <summary>The BASE magic crit rate — the <c>50 × witMod</c> head of the chain
    /// <code>magicCrit = (50 × witMod × buffs × passives + flat) × debuffs</code>
    /// the exact shape <see cref="PhysicalCritBase"/> feeds on the physical side. There is NO
    /// weapon term: magic crit is WIT and buffs only (owner: "it's not weapon based").
    /// ⚠ Deliberately NOT clamped — the single clamp belongs at the END of the chain
    /// (Entity.RecomputeDerived), or a mid-chain clamp silently eats the buffs.</summary>
    public static float MagicCritBase(int wit) => MagicCharacterCritBase * CritWitMod(wit);

    /// <summary>Physical SKILL "[Double]" chance (×2 damage) — a pure ATK curve
    /// (owner ruling 2026-08-05, docs/design/CritBlowAndDouble.md §1):
    /// <code>Double% = min(25, 2.5 + max(0, 0.75·(ATK − 30)))</code>
    /// so ATK 30 → 2.5%, 40 → 10%, 50 → 17.5%, 60+ → 25% (capped).
    /// <paramref name="atkStat"/> is the ATK **stat** (the 30-60 band), never EffectiveAtk /
    /// p.Atk: a better weapon must not buy Double chance, only the build does. AGI makes a blow
    /// LAND; ATK makes it double. Only skills flagged [Double] roll this.</summary>
    public static float PhysicalDoubleChance(int atkStat) =>
        Math.Clamp(0.025f + 0.0075f * Math.Max(0, atkStat - 30), 0.025f, StatCaps.PhysicalDoubleRate);

    /// <summary>Physical crit DAMAGE multiplier, capped x10.</summary>
    public static float PhysicalCritMult(float bonus = 0f) =>
        Math.Min(2.0f + bonus, StatCaps.PhysicalCritDamage);

    /// <summary>The FLAT crit-damage term, expressed as a factor on an already-computed hit.
    /// Crit damage in the class CSVs is a flat "+80" that joins ATTACK inside the ratio, on a
    /// crit only: <c>K·(flat + mod·(pAtk + critFlat))/def</c>. Because everything downstream of
    /// the ratio (variance, weapon coef, the damage-out pipeline) is a linear multiplier, the
    /// whole term reduces to this ratio of the two raw damages — so the caller can apply it to
    /// the finished number and get exactly the same result. Returns 1 with no flat bonus.
    /// A basic attack passes (flat: 0, mod: 1), i.e. the plain (pAtk+critFlat)/pAtk.</summary>
    public static float CritFlatFactor(float pAtk, float critFlat, int flat = 0, float mod = 1f)
    {
        if (critFlat <= 0f) return 1f;
        float normal = flat + mod * pAtk;
        if (normal <= 0f) return 1f;
        return (flat + mod * (pAtk + critFlat)) / normal;
    }

    /// <summary>Magic crit DAMAGE multiplier — <c>×2 base × multipliers × (1 − debuffs)</c>,
    /// which is the owner's formula verbatim (2026-08-19):
    /// *"base critDmg x multiPliers x (1 - debuffs)"*.
    ///
    /// <para>It was a FLAT ×3 taking no bonus at all (2026-08-06 → 2026-08-19). The flat form
    /// existed to stop Ferocity and the crit-damage item attribute — both authored for fighters
    /// — from silently paying a mage, and THAT part still holds: this reads
    /// <see cref="Entity.MagicCritDamageMult"/>, its own channel, never
    /// <c>CritDamageBonus</c>. What changed is that the channel now has a knob in it, because
    /// the 4th-class kits need one: the buffer's and healer's +30% magic-crit-damage blessings
    /// (×2.6 alone, ×3.38 with both, since multipliers COMPOUND).</para>
    ///
    /// <para>⚠ The base dropped ×3 → ×2 in the same ruling. Combined with the crit-RATE
    /// rescale above, a nuker with only Insight goes from <c>0.80 + 0.20×3 = ×1.40</c> average
    /// to <c>0.84 + 0.16×2 = ×1.16</c> — about −17% magic damage until the 4th-class buffs
    /// exist to give it back.</para></summary>
    /// <param name="mult">The caster's compounded magic-crit-damage multiplier (1 = none).</param>
    /// <param name="resist">Summed magic-crit-damage DEBUFFS on the caster (0 = none).</param>
    public static float MagicCritMult(float mult = 1f, float resist = 0f) =>
        Math.Clamp(StatCaps.MagicCritDamageBase * mult * (1f - resist),
                   1f, StatCaps.MagicCritDamageCap);

    // ----- Magic landing: its OWN formula, not the physical resolver ---------------------------
    //
    // Owner ruling 2026-08-10 (playtest-20 `57d`). Magic used to call ResolveAvoidChance with
    // `0, 0` for both stat terms — i.e. NO stat race at all — so every caster in the game sat on
    // the same 1% base vs a mob and the weapon in your hands changed nothing. The replacement is
    // an explicit multiplicative formula in percentage POINTS:
    //
    //     fail% = round( 1.3^(defenderLvl − attackerLvl) × defenderMod × weaponMod )
    //
    //   • level  — 1.3^Δ. Parity = ×1, and round(1 × 1 × 1) = 1, so SAME LEVEL IS 1% FAIL.
    //              Casting DOWN rounds to 0 fail from Δ−2. Casting UP is brutal: Δ+10 ≈ 14%,
    //              Δ+16 ≈ 67%, and from Δ+18 it is pinned to the ceiling.
    //   • defender — 1 for everyone; a TANK's Anti-Magic passive makes it 2 (so 2% at parity and,
    //              because it multiplies, a much bigger share of the level term as the gap grows).
    //   • weapon — 1 with a trained caster weapon, 25 with a bow/dual/bare hands.
    //
    // ⚠ There is deliberately NO caster-side "magic accuracy" stat. The owner's model is the level
    // formula, the occasional tank ×2, and the weapon multiplier — nothing else. Don't reinstate
    // MagicFailResist: it was our only spell-landing stat, it was zero on every character in the
    // game, and halving zero for a bow is exactly what made `57d` invisible.

    /// <summary>Probability a spell FIZZLES on the defender (fail = reduced damage / a debuff that
    /// doesn't land — never zero damage). See the block above for the formula.</summary>
    /// <param name="attackerLevel">The level the SPELL counts as — since 2026-08-24 the caller passes
    /// the RUNG'S learn level (<c>GameLoopService.RungLevel</c>), not the caster's own, so an old rung
    /// decays as the caster outgrows it: *"if I learn 35 lvl spell at lvl 50 it should use the 35"*.
    /// A spell no class list owns (a mob spell, a scroll, the practice dummy) still falls back to the
    /// caster's level, because it has no rung to read.</param>
    /// <param name="defenderMod">Defender's magic-fail modifier: 1 normally, 2 with the tank's
    /// Anti-Magic passive.</param>
    /// <param name="weaponMod">Caster's weapon modifier: 1 trained,
    /// <see cref="StatCaps.UntrainedWeaponMagicFailMod"/> with a bow/dual/bare hands.</param>
    /// <param name="defenderFlatPoints">Defender's MAGIC EVASION in percentage points, added after
    /// the multiplicative part (owner ruling 2026-08-11, `62e`: *"the magic evasion should be magic
    /// fail chance like 3-4"*). FLAT and additive on purpose: multiplying it would make a 4-point
    /// dodge worth almost nothing at parity (1% × 1.04) and enormous at a level gap, which is the
    /// opposite of a defensive burst. The only source today is the rogue's Evasion Boost.</param>
    /// <param name="defenderFlatPoints">"M.Evasion" — percentage POINTS the DEFENDER adds to the roll,
    /// making spells aimed at them fail more often.</param>
    /// <param name="casterFlatPoints">"M.Accuracy" — percentage POINTS the CASTER takes back off it,
    /// and the exact mirror of the line above (owner, 2026-08-26, asking what M.Acc was: *"the mAcc is
    /// magic fizzle chance? what does Magic evasion do? so the oposite"* — yes, and yes).
    ///
    /// <para>⚠ This is NOT a reopening of the caster-side accuracy STAT the 2026-08-10 rework deleted.
    /// That was a stat you carried and levelled; this is a flat grant a specific skill hands out, on
    /// the same footing M.Evasion has had since 2026-08-11. Nothing derives it from WIT or from
    /// anything else, and no gear rolls it — it exists because his Marks author it, and if no skill
    /// authors it the number is 0 and the formula is what it was.</para></summary>
    public static float MagicFailChance(int attackerLevel, int defenderLevel,
                                        float defenderMod = 1f, float weaponMod = 1f,
                                        float defenderFlatPoints = 0f,
                                        float casterFlatPoints = 0f)
    {
        float levelMod = MathF.Pow(StatCaps.MagicLevelBase, defenderLevel - attackerLevel);
        float points = MathF.Round(StatCaps.MagicFailParityPoints * levelMod
                                   * Math.Max(0f, defenderMod) * Math.Max(0f, weaponMod))
                     + Math.Max(0f, defenderFlatPoints)
                     - Math.Max(0f, casterFlatPoints);
        return Math.Clamp(points / 100f, 0f, StatCaps.MagicFailMax);
    }

    // (MagicWeaponFailMod(bool) — REPLACED 2026-08-20 by `Entity.MagicFailSelfMult`, a running PRODUCT
    //  the untrained weapon multiplies ×25 into and a passive can divide back out. A bool could only
    //  ever say "penalised or not", which is exactly what made "the opposite of the bow penalty"
    //  unauthorable. The weaponMod parameter of MagicFailChance is unchanged — only its source is.)

    // ============================================================================================
    //  INTERRUPT — IG'S OWN FORMULA (owner, 2026-08-26). This REPLACED the DPS contest of 0.83.0.
    // ============================================================================================
    //
    //     BaseChance  = (DmgTaken / MaxHP) x random(100..120)
    //     FinalChance = BaseChance x MEN-mod x (1 - Buffs/EquipMod)
    //
    //     his worked example: 1000 damage on a 2000 HP pool -> 50; x1 x (1 - 0.54 Resolve) = 23%.
    //
    // 🔑 THE YARDSTICK IS THE CASTER'S HP POOL, NOT THE SPELL. Everything the 0.83.0 model needed —
    // the spell's damage, its cast time, its reuse, the attacker's DPS, both sides' WIT — is gone.
    // One hit is measured against the one thing that is always defined: how much of the caster it
    // just took off. That is what makes it work for a buff, a resurrection and a 300s nuke alike,
    // and it is why `CastInterruptReference` no longer exists. Its 🔴 THUNDERSTORM finding — a
    // 300s reuse making a spell's own DPS ~0, and so making the game's biggest nuke the easiest
    // cast in the game to break — dies with it: reuse is not an input any more.
    //
    // 🔑 CAST TIME IS STILL IN THERE, but as an EMISSION rather than a term: a longer cast eats more
    // hits, so it breaks more often, at exactly the rate the hits arrive. His *"they cast fast enough
    // for a small window to interrupt"* is the whole defence a mage gets by default.
    //
    // 🔴 TWO DELIBERATE DEPARTURES FROM IG, both his:
    //   1. NO ROBE-SET INTERRUPT RESIST. IG's robe set carries 50% on top of Resolve's 54% and the
    //      product makes a mage effectively uninterruptible — *"and i dont want that"*.
    //   2. THE MEN CURVE IS FLATTENED. IG's is ~4.8%/point (20 MEN = x1.00, 50 = x0.23), which on our
    //      SPT bases prices a level-39 human mage at x0.395 and a 50%-HP hit at 9% — *"a bit low"*.
    //      Ours is the curve he named instead: 20 = x1.00, 50 = x0.67, i.e. 33% resist at 50 SPT.

    /// <summary>SPT (the retired MEN) → the interrupt multiplier. Geometric, exactly like IG's own
    /// table, but at his flattened rate: <b>20 SPT = x1.00, 50 SPT = x0.67</b>. Below 20 it is held at
    /// x1 rather than climbing past it — a low-SPT fighter is not made EASIER to interrupt than the
    /// scale's floor, because the floor is where the whole curve is anchored.
    /// <para>Reference points on our own bases: human fighter 25 → x0.94, elf mage 32 → x0.85,
    /// human mage 39 → x0.78, demon mage 45 → x0.72.</para></summary>
    public static float SpiritInterruptMod(int spt) =>
        spt <= InterruptSpiritFloor ? 1f
        : MathF.Pow(InterruptSpiritAt50, (spt - InterruptSpiritFloor) / 30f);

    /// <summary>The SPT at which the interrupt curve is x1 — IG's "20 MEN = 1".</summary>
    public const int InterruptSpiritFloor = 20;

    /// <summary>The multiplier at SPT 50, which is what sets the curve's steepness. His number:
    /// *"20=1; 50=33% resist"*. IG's own value here is 0.23.</summary>
    public const float InterruptSpiritAt50 = 0.67f;

    /// <summary>IG's <c>random(100~120)</c>, as a multiplier. Rolled fresh on every hit.</summary>
    public const float InterruptRollMin = 1.00f;
    /// <inheritdoc cref="InterruptRollMin"/>
    public const float InterruptRollMax = 1.20f;
    /// <summary>The mean of the roll — what a TABLE should show when it is not simulating.</summary>
    public const float InterruptRollMean = (InterruptRollMin + InterruptRollMax) / 2f;

    /// <summary>Chance ONE hit breaks a cast in progress. IG's formula; see the block above.</summary>
    /// <param name="damageTaken">What this single hit actually took off the caster.</param>
    /// <param name="maxHp">The caster's Max HP — the yardstick.</param>
    /// <param name="spiritMod">The caster's <see cref="SpiritInterruptMod"/>.</param>
    /// <param name="resistPct">Summed interrupt-resist buffs (Resolve) as a FRACTION.</param>
    /// <param name="roll">IG's random term, in [<see cref="InterruptRollMin"/>, <see cref="InterruptRollMax"/>].</param>
    /// <param name="skillMult">The striking skill's <c>InterruptMult</c> — his *"we can make the two
    /// lower dmg elf nuker skills have x3 interrupt chance"*. 1 = ordinary.</param>
    /// <param name="flatBonus">The striking skill's <c>InterruptPower</c> in percentage POINTS, added
    /// after everything else. Disrupt's 99999 is what still makes it a guaranteed cancel.</param>
    public static float InterruptChance(float damageTaken, float maxHp, float spiritMod,
                                        float resistPct, float roll,
                                        float skillMult = 1f, float flatBonus = 0f)
    {
        float flat = Math.Clamp(flatBonus / 100f, 0f, 1f);
        if (damageTaken <= 0f || maxHp <= 0f) return flat;
        float chance = damageTaken / maxHp * roll
                     * Math.Max(0f, spiritMod)
                     * (1f - Math.Clamp(resistPct, 0f, StatCaps.InterruptResistMax))
                     * Math.Max(0f, skillMult)
                     + flat;
        return Math.Clamp(chance, 0f, 1f);
    }

    /// <summary>Chance a contested debuff (slow/stun/root/fear/…) LANDS: the attacker's
    /// ATK (core power stat) vs the defender's resisting stat. 50% when equal, scaling by the
    /// ratio, clamped to [10%, 90%] (per docs/design/Disciplines.md). Bosses are made immune
    /// by the caller.
    /// <para>🔑 THE TWO DEFENDING STATS ARE THE ONES YOU GAVE UP (owner 2026-08-17). Magical
    /// debuffs are resisted by <b>SPT</b>, not WIT — *"the actual stat u give up to increase wit
    /// and atk as a mage, so u get easily debuffed by magic debuffs"*. Physical ones (bleed,
    /// hold, stun) are resisted by <b>CON</b>, *"same logic, you give up con to increase atk and
    /// agi"*. Both sides of the contest are therefore a real build cost, not a free stat: the
    /// glassier your offence, the easier you are to control.</para></summary>
    /// <param name="attackerLevel">Caster's level. <paramref name="defenderLevel"/> above it scales the
    /// defender's stat UP by <see cref="StatCaps.CcLevelBase"/> per level; below it scales it down. Equal
    /// levels = ×1 exactly, so the contest is pure stat vs stat — the owner's rule. See the StatCaps
    /// block for why the level term rides the STAT and not the chance.</param>
    /// <param name="defenderLevel">Target's level.</param>
    public static float DebuffLandChance(int attackerAtk, int defenderStat,
                                         int attackerLevel, int defenderLevel)
    {
        float def = defenderStat
                  * MathF.Pow(StatCaps.CcLevelBase, defenderLevel - attackerLevel);
        float sum = attackerAtk + def;
        if (sum <= 0f) return 0.5f;
        float chance = 0.5f + 0.5f * (attackerAtk - def) / sum;
        return Math.Clamp(chance, StatCaps.CcLandMin, StatCaps.CcLandMax);
    }

    /// <summary>`BL-156` — how much of a contested debuff's AUTHORED duration survives, given the
    /// stat that just lost the landing contest (CON for a physical debuff, SPT for a magical one).
    /// One rule for players and creatures alike.
    ///
    /// <para>Linear from <see cref="StatCaps.DebuffDurationStatBase"/> (×1.00) to
    /// <see cref="StatCaps.DebuffDurationStatFull"/> (the floor), clamped at both ends — so a low stat
    /// never lengthens a debuff and a very high one never removes it. See the note in StatCaps for why
    /// it reads the raw stat rather than the land chance.</para></summary>
    public static float DebuffDurationFactor(int defenderStat)
    {
        float span = StatCaps.DebuffDurationStatFull - StatCaps.DebuffDurationStatBase;
        if (span <= 0f) return 1f;
        float t = (defenderStat - StatCaps.DebuffDurationStatBase) / span;
        float factor = 1f - (1f - StatCaps.DebuffDurationFloor) * t;
        return Math.Clamp(factor, StatCaps.DebuffDurationFloor, 1f);
    }

    // ----- Cast & attack speed (authentic IG model) ------------------------
    //
    // IG: actual cast/attack time = baseTime × 333 / speedStat, where the speed stat
    // is built MULTIPLICATIVELY:
    //   castSpd = ClassBaseCast × WitModifier × weaponFactor × gearFactor × ∏(1+buff%)
    // and capped (cast 1999 = 6× faster than the 333 reference, attack 1500). The full
    // assembly lives in Entity.EffectiveCast/AttackSpeedMultiplier; this file provides
    // the class bases, the exponential WIT curve, weapon factors and the 333 reference.

    public const int SpeedBaseline = 333;  // stat value that equals 1.0x speed


    /// <summary>Class base casting speed, before WIT/gear/buffs. This is the ROBED (correct)
    /// value: a mage sits at the 333 baseline = 1.0× cast time. Demon mages are the slow
    /// casters at 300; every non-mage casts at 300.
    /// It used to be 166 for mages, which silently made EVERY mage cast take ~2× its
    /// nominal time (166 vs the 333 baseline) — a healer's 4s bolt really took ~6.5s.
    /// The "wrong armor" penalty is NOT applied here: Robe Mastery's light/heavy/none
    /// profiles already carry CastSpeedPct −0.5, which halves 333 back down to 166.
    ///
    /// <para>🔑 THE NON-MAGE BASE WAS 150 UNTIL `BL-133` (owner, 2026-09-03): *"why fighters have so
    /// low cast speed ? shouldnt it all have about the 300~400 cast in the begining and only mages have
    /// the spellcaster_mastery ... now my elf figter have 130 base and 182 buffed .. and i think he
    /// must have 260 (or whatever base x wit mod) and ~365 buffed"*. His two numbers were the code's
    /// exactly: an elf fighter's WIT is 17, so 150 × <see cref="CastWitModifier"/>(17) = 150 × 0.864 =
    /// <b>130</b>, and ×1.4 from a cast-speed buff = <b>182</b>. At 300 the same character reads
    /// <b>259</b> and <b>363</b> — his 260 and ~365.</para>
    ///
    /// <para>🔑 THE MODEL HE DESCRIBED IS ALREADY THE MODEL, and only this number disagreed with it.
    /// A caster's advantage is meant to come from WIT and from the mastery PENALTIES for the wrong
    /// armour/weapon (the robe/light/heavy profiles' `CastSpeedPct −0.5`, i.e. his *"386 → 193 without
    /// robe → 96 without wand"*), not from a class base twice as large. ⚠ One correction to his
    /// reading: the elf mage's 386 is NOT Spellcaster Mastery — it is 333 × the WIT modifier of a
    /// 23-WIT elf.</para>
    ///
    /// <para>⚠ WHAT THIS ACTUALLY MOVES IS SMALL, because `BL-132` took every PHYSICAL skill off cast
    /// speed in the same pass: a fighter's cast bar now holds only his MAGICAL debuffs, which is the
    /// point of the change. The class that gains broadly is the Warchanter — a `BaseClass.Fighter`
    /// whose whole kit is songs.</para></summary>
    public static int ClassBaseCastSpeed(Race race, BaseClass cls) =>
        cls != BaseClass.Mage ? 300
        : race == Race.Demon ? 300
        : 333;

    /// <summary>AGI physical-attack-speed modifier — EXPONENTIAL, matching the IG table
    /// (baseline 30 = 1.00: 20→0.90, 35→1.05, 40→1.11, 50→1.23): ~1.05% per AGI
    /// compounded. Clamped so very low AGI can't stall attacks entirely.</summary>
    public static float AttackAgiModifier(int agi) =>
        Math.Clamp(MathF.Pow(1.0105f, agi - 30), 0.4f, 8f);

    /// <summary>WIT casting-speed modifier — EXPONENTIAL, matching the IG table
    /// (20→1.00, 30→1.63, 40→2.65, 50→4.32): ×1.63 per +10 WIT. Clamped so very low
    /// WIT can't stall casting entirely.</summary>
    public static float CastWitModifier(int wit) =>
        Math.Clamp(MathF.Pow(1.63f, (wit - 20) / 10f), 0.4f, 8f);

    /// <summary>Weapon cast factor — ×1.0 for EVERY weapon type. Owner's ruling, 2026-08-10:
    /// *"All weapons should have the same cast speed ×1 (no weapon changes cast speed for a
    /// weapon type, only passives)."*
    ///
    /// It used to be Blunt ×1.0 and everything else ×0.8, which was the WRONG place for that rule
    /// twice over. (1) It double-charged: the wrong-weapon penalty is owned by Spellcaster Mastery
    /// (CasterMastery grants its bonus only with a sword/blunt, and the robe/armor masteries carry
    /// the cast multipliers) — a bare weapon factor taxed the same choice again. (2) It contradicted
    /// the authored profile: sword/blunt is documented as cast ×1, but a SWORD silently took the
    /// ×0.8 branch. Kept as a function rather than deleted so the assembly in
    /// Entity.EffectiveCastSpeedMultiplier keeps its shape and a future per-weapon rule has a home.</summary>
    public static float WeaponCastFactor(WeaponType w) => 1.0f;

    /// <summary>Weapon base attack speed (baseline 333 = 1.0×). Owner's table, 2026-08-10.
    ///
    /// ⚠ Switches on the RAW WeaponType, NOT w.Base(). Folding to the base type was the bug he
    /// found: TwoHandedSword→Sword and TwoHandedBlunt→Blunt meant a 2H swung exactly as fast as a
    /// 1H, and 1H blunt inherited the staff's slow 325 — so blunt and sword disagreed where they
    /// should have matched, and 1H and 2H matched where they should have disagreed.
    ///
    /// A single bow item can override this via ItemDef.AttackSpeedBase (227 = "very slow"), which
    /// Entity.RecomputeDerived prefers when non-zero — that is how two bows differ.</summary>
    public static int WeaponAttackBaseSpeed(WeaponType w) => w switch
    {
        WeaponType.Dual            => 433,  // knives/dual: very fast
        WeaponType.Sword           => 379,  // 1H sword: fast
        WeaponType.Blunt           => 379,  // 1H blunt: fast (same as the 1H sword)
        WeaponType.TwoHandedSword  => 325,  // 2H sword: normal
        WeaponType.TwoHandedBlunt  => 325,  // 2H blunt / staff: normal
        WeaponType.Bow             => 293,  // bow: slow (a "very slow" 227 bow sets AttackSpeedBase)
        _                          => 300,  // weaponless
    };

    /// <summary>MOB per-hit POWER factor for the weapon it holds (BL-14) — the other half of giving
    /// monsters weapon types. *"Archer is slower but does more dmg, the fast attacking have more crit
    /// rate and more atck speed but less dmg."*
    ///
    /// A PLAYER gets this for free from the weapon ITEM: a 2H sword carries more P.Atk than duals, so
    /// a slow weapon buys per-hit damage. A mob has no item — its P.Atk is one level curve — so when
    /// weapons were handed out on 2026-08-10 the ONLY thing the weapon changed was the attack rate.
    /// A club mob (379) simply became 12% worse than a claw mob (433) at nothing, and the fast
    /// attacker was strictly better instead of trading damage for rate. This is the missing trade.
    ///
    /// The reference is the DUAL's 433 — the speed every mob in the game was pinned to before that
    /// change — so this is DPS-neutral against the pin: nothing is nerfed, the mobs that were
    /// silently slowed get their lost damage back as per-hit power, and a claw mob's advantage is
    /// now what he said it should be: rate and CRIT RATE (the dagger/claw weapon crit factor is 3×
    /// a club's), paid for with the smallest hit.
    ///
    /// ⚠ BOW returns 1. An archer mob already pays this exact trade explicitly in its ROLE
    /// (<c>MobRole.Archer</c>: ×2 P.Atk, 450 range, less P.Def), and charging his one sentence twice
    /// would make archers ~3× per arrow. If the role's ×2 is ever removed, this is where it goes.</summary>
    public static float MobWeaponPowerFactor(WeaponType w) =>
        w == WeaponType.Bow ? 1f : 433f / WeaponAttackBaseSpeed(w);

    // (MobBareHandAttackSpeed — added and removed the same day, 2026-08-10. It pinned mobs to 433
    //  because his weaponless 300 would otherwise have slowed the WHOLE bestiary by 31%, every mob
    //  being WeaponType.None. He then ruled the real fix: *"most mobs must have a weapon ... so
    //  weaponless won't be for many mobs"* — see MobCatalog.DefaultWeaponFor. With claws modelled as
    //  Dual (433) the animals never move at all, and the few genuinely weaponless creatures (plants,
    //  magic creatures) correctly take the 300. The pin had no reason left to exist. Don't re-add it.)

    // ----- Progression -------------------------------------------------------

    /// <summary>Exp required to go from <paramref name="level"/> to the next.
    /// The curve itself lives in <see cref="ExpCurve"/>; these stay as the long-standing call sites.</summary>
    public static long ExpToNext(int level) => ExpCurve.ExpToNext(level);

    /// <summary>Base EXP a normal mob of this level pays (before toughness / gap / party / roll).</summary>
    public static long MobExpReward(int mobLevel) => ExpCurve.MobExpReward(mobLevel);

    /// <summary>Base SP a normal mob of this level pays.</summary>
    public static long MobSpReward(int mobLevel) => ExpCurve.MobSpReward(mobLevel);

    /// <summary>Raid ±10-level rule: damage a player deals TO a boss is scaled by how far the
    /// player's level is from the boss's — full within ±5, tapering to a 0.1 floor beyond ~±16.
    /// Both directions (so an over-leveled player can't trivialize a lowbie raid, nor a far
    /// under-leveled one tank it). Retune the bands as needed.</summary>
    public static float RaidLevelGapMult(int attackerLevel, int bossLevel)
    {
        int gap = System.Math.Abs(attackerLevel - bossLevel);
        if (gap <= 5) return 1f;
        if (gap <= 10) return 1f - (gap - 5) * 0.06f;              // 5→1.0 .. 10→0.70
        return System.Math.Max(0.1f, 0.7f - (gap - 10) * 0.1f);   // 11→0.60 .. 16+→0.10
    }

    /// <summary>`BL-98` — the BOSS'S JUDGMENT band. Levels away from the boss you may be and still
    /// take part in its fight; further than this and any hostile act on the boss, or any act of
    /// support aimed at someone fighting it, walks you up the punishment ladder (see
    /// <see cref="BossJudgment"/> and <c>SkillCatalog.BossJudgmentSkill</c>).
    ///
    /// <para>His number, and the word is his too: *"9lvl +-"* — SYMMETRIC, like
    /// <see cref="RaidLevelGapMult"/> right above, which has scaled a player's damage to a boss in
    /// both directions since the ±10 rule was built. The exploit BL-98 was raised for is only the
    /// over-levelled half (a high healer propping up a low raid); the under-levelled half costs a
    /// far-below character nothing he could have contributed anyway, and keeping the rule symmetric
    /// means one number, one sentence to a player, and the same shape as the damage curve.</para></summary>
    public const int BossJudgmentGap = 9;

    /// <summary>Is this level far enough from the boss's to be judged for interfering?
    /// Strictly further than <see cref="BossJudgmentGap"/> — *"if he is in 9lvl of the boss it
    /// doesn't prevent him"*, so a gap of exactly 9 is still inside the fight.</summary>
    public static bool BossJudges(int actorLevel, int bossLevel) =>
        System.Math.Abs(actorLevel - bossLevel) > BossJudgmentGap;

    /// <summary>Base gold a mob drops, by level (scaled by RateConfig.World.Gold
    /// and a small variance at the drop site).</summary>
    public static int MobGoldReward(int mobLevel) => 25 + mobLevel * 8;

    /// <summary>Mob stat block by level. Per design: higher-level mobs must
    /// out-stat lower-level characters.</summary>
    // Atk grows level*2 (was level*3, which out-scaled players and 2-shot squishy
    // classes). Tuning knob — raise/lower the level coefficient to make mobs hit
    // harder/softer globally. (Con/Agi unchanged.)
    public static BaseStats MobStats(int level, MobRole role = MobRole.Melee) =>
        //
        // ⚠ CON AND SPT ARE FLAT AND AUTHORED BY ROLE (owner ruling 2026-08-19) — see MobCcCon /
        // MobCcSpt. They used to be `15 + 2·level` and 30, and the level growth was doing the whole
        // job of the level term that contested debuffs never had: CON 175 at level 80 meant a big
        // creature could not be stunned at all. The level difference now lives in
        // DebuffLandChance where it can be tuned once, and these two say only what the creature IS.
        //
        // ⚠ AGI IS FLAT, and deliberately (owner, 2026-08-02). It used to be `10 + level`, which was
        // the real cause of the accuracy collapse: AGI drives accuracy, evasion, crit rate and attack
        // speed, and a PLAYER's AGI never grows. Making accuracy `AGI + level` on both sides does NOT
        // fix that on its own — the level terms cancel and the mob's own AGI growth still runs away.
        // MobAgiReference is the human-fighter base, so a same-level normal mob is a NEUTRAL opponent
        // (5% both ways) and every point of spread comes from gear and passives, where it is earned.
        new(Con: MobCcCon(role), Atk: MobCcAtk(role), Wit: 5, Agi: MobAgiReference, Spt: MobCcSpt(role));

    // ----- The THREE contested-debuff stats, by ROLE (owner ruling 2026-08-19) ------------
    //
    // 🔑 These are the mob's ONLY use for CON, SPT and ATK. Its HP is MobBaseStats.Hp(level), its MP is
    // MobBaseStats.Mp(level), its P/M.Atk are MobBaseStats.PAtk/MAtk(level) and its regen is a fraction
    // of its own pool — none of them read a core stat (see Entity.RecomputeDerived and
    // GameLoopService.Regenerate). So these numbers are pure identity: change them and nothing moves
    // except how easily the creature controls, and is controlled.
    //
    // The lean is his own defensive rule turned around ("the stat you give up"): a fighter shrugs off
    // stuns and eats holds, a caster is the reverse. Against a typical ATK 40 attacker at the same
    // level that reads as:
    //
    //     role      CON  SPT   stun/bleed   root/hold
    //     Melee      45   38      47.1%        51.3%
    //     Archer     43   40      48.2%        50.0%
    //     Mage       40   58      50.0%        40.8%
    //     (tank)     50   40      44.4%        50.0%   ← MobMod.Con/Spt, not a role
    //
    // A TANK is not a MobRole — Role says how a creature FIGHTS, and a tank fights melee. It is
    // authored per template with MobMod.Con/Spt, in the same place its P.Def passive already lives.

    /// <summary>Flat ATK by fighting role — the OFFENSIVE half of the same contest (owner ruling
    /// 2026-08-19: *"need to make the same for mobs ATK .. if they have 200atk and i 43 con ... ill get
    /// perma stunned ... lower it to normal ranges"*).
    ///
    /// <para>It was <c>8 + 2·level</c> — 168 at level 80, against a player CON of ~43. That is a 4:1
    /// ratio, i.e. the 90% ceiling, i.e. a permanent stun. The same reasoning that flattened CON and
    /// SPT applies here and harder: the level difference is the level term's job now, so this stat says
    /// only how hard the creature LEANS on control. "Normal ranges" is the player band, which every
    /// other formula in this file already assumes — <see cref="PAtkStatReference"/> is 40 and
    /// <see cref="PhysicalDoubleChance"/> caps at 60, so a mob at 168 was outside the domain of its own
    /// math. The MAGE leans highest: a caster is the creature that debuffs.</para>
    ///
    /// <para>⚠ A mob's P.Atk and M.Atk do NOT come from here — they are MobBaseStats.PAtk/MAtk(level),
    /// and Entity.RecomputeDerived only feeds EffectiveAtk into attack power on the PLAYER branch. So
    /// this does not weaken a creature's damage. The one real side effect is
    /// <see cref="PhysicalDoubleChance"/>, which mobs were pinning at its 25% cap and now sit at
    /// 10-13% on — and that only bites on a skill flagged CanDouble, which no mob skill is today.</para>
    ///
    /// <para>⚠ RANK DOES NOT MULTIPLY THIS. An elite gets ×1.5 on its DEFENSIVE pair and nothing here,
    /// and a boss gets neither (it is flatly control-immune instead). That asymmetry is deliberate but
    /// unruled: it means a boss's signature stun lands ~48% on a fighter rather than the ~80% it used
    /// to. If a boss should hit harder with control than a trash mob, this is the one line to change.</para></summary>
    public static int MobCcAtk(MobRole role) => role switch
    {
        MobRole.Mage => 45,
        _            => 40,   // Melee, Archer
    };

    /// <summary>Flat CON (physical-debuff resistance: stun, bleed, venom) by fighting role.</summary>
    public static int MobCcCon(MobRole role) => role switch
    {
        MobRole.Mage   => 40,
        MobRole.Archer => 43,
        _              => 45,   // Melee
    };

    /// <summary>Flat SPT (magical-debuff resistance: root, fear, poison) by fighting role. The mage's
    /// is the outlier on purpose — it is the one creature that is genuinely hard to hold.</summary>
    public static int MobCcSpt(MobRole role) => role switch
    {
        MobRole.Mage   => 58,
        MobRole.Archer => 40,
        _              => 38,   // Melee
    };

    /// <summary>A normal mob's AGI at every level — the human-fighter base, so it is the neutral
    /// benchmark both sides of the miss roll are measured against. A tougher/nimbler creature buys
    /// its evasion with a MobMod passive (the Armor Weight mastery's ±10), not with a steeper curve.</summary>
    public const int MobAgiReference = 30;

    /// <summary>Mob MAGIC defence by level. The universal <see cref="MagicDefence"/>
    /// base (level/2) leaves low-level mobs at ~0 mDef, so spells divide by ~1 and
    /// one-shot them. Mobs get a dedicated mDef on roughly the same curve as their
    /// physical defence, so magic and physical land in a comparable range. (Players
    /// keep the level base + jewels; this is mob-only.)</summary>
    public static int MobMagicDefence(int level) => Math.Max(5, (int)(level * 1.2f));

    // The per-archetype basic-attack multiplier (tank 0.55 / rogue 0.65 / mage 0.15 / …) is
    // GONE. It was a hardcoded class identity that fought the formula: it crippled the tank's
    // and dagger's auto-attacks and had to be compensated for elsewhere. Basic-attack damage
    // is now pure formula, and the WEAPON differentiates — a tank's 1H sword hits for less
    // than a warrior's 2H, a dagger less per hit but far faster, a bow much harder. Per-class
    // nudges belong in the "Class Balance" passive (SkillCatalog.ClassBalanceFor), which is
    // data, not code.

    // Archetype crit/evasion LEANS moved to the rogue's floor passive (Evasion Mastery) and its
    // masteries, per the stats-via-skills rule — no longer hardcoded here. (The archer's `reflexes`
    // twin was deleted 2026-08-07: the merge left one rogue line, so there is one floor passive.)

    /// <summary>Per-weapon crit-rate FACTOR (multiplies the base/AGI crit chance).
    /// From the weapon table's crit_modifier: Sword 0.80, Dual/Bow 1.20, Blunt 0.40.
    /// Blunt trades crit away for accuracy.</summary>
    public static float WeaponCritFactor(WeaponType w) => w.Base() switch
    {
        WeaponType.Dual => 1.20f,
        WeaponType.Bow => 1.20f,
        WeaponType.Sword => 0.80f,
        WeaponType.Blunt => 0.40f,
        _ => 1.0f                    // weaponless: unchanged
    };

    /// <summary>Per-weapon ACCURACY bonus. Blunt weapons are easier to land (high
    /// base accuracy) — the counterpart to their low crit. Tune later.</summary>
    public static int WeaponAccuracyBonus(WeaponType w) => w.Base() switch
    {
        WeaponType.Blunt => 10,
        _ => 0
    };

    // NOTE: every per-archetype IDENTITY stat modifier is now data (learned passives), not a switch
    // table here. Evade/hit/anti-magic FLOORS + the rogue's crit & evasion leans → the floor
    // passives (SkillCatalog.FloorPassiveFor / Evasion Mastery / Precision / Anti-Magic). The tank's old level/2
    // magic-def bonus was REMOVED (his Anti-Magic passive is his magic identity). The rogue's
    // basic-attack interrupt was REMOVED from the base archetype — it becomes a 3rd-class discipline
    // passive (the anti-magic rogue). Only base stats + level-growth CURVES stay hardcoded (allowed).
}
