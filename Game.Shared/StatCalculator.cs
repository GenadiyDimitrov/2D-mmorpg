namespace Game.Shared;

/// <summary>
/// Stat formulas live in Shared so the client can *predict* (tooltips,
/// estimated damage) while the server stays the only authority.
/// Base stats follow the design doc:
///   Ork/Demon  Fighter 40/30/10/20  Mage 30/30/20/20   (CON/ATK/WIT/AGI)
///   Elf/Angel  Fighter 30/20/20/30  Mage 20/20/30/30
///   Human      Fighter 35/25/15/25  Mage 25/25/25/25
/// </summary>
public static class StatCalculator
{
    public readonly record struct BaseStats(int Con, int Atk, int Wit, int Agi, int Spt);

    public static BaseStats GetBaseStats(Race race, BaseClass cls) => (race, cls) switch
    {
        // BaseStats(Con, Atk, Wit, Agi, Spt). Atk = the single power stat: STR for fighters,
        // INT for mages. Fighter WIT kept low (casts little); mage WIT per the dye-
        // stand-in design (elf 23 / human 20 / ork 19). Authentic-L2-style bases.
        //
        // SPT (Spirit) is a FULL stat like the rest — the retired MEN, made visible and investable.
        // FIGHTERS keep their original per-race MEN values (ork 27 > elf 26 > human 25) so the ork
        // fighter stays the sturdiest — a flat fighter value erased that (owner, 2026-07-20).
        // MAGES take the owner's spread off the human mage: ork +7%, elf −7%. The curve is flat
        // (~1.6%/point), so those need wide gaps — hence 45 and 32, not 42 and 40.
        (Race.Ork, BaseClass.Fighter) => new BaseStats(47, 40, 10, 26, 27),
        (Race.Ork, BaseClass.Mage)    => new BaseStats(31, 31, 19, 20, 45),
        (Race.Elf, BaseClass.Fighter) => new BaseStats(36, 36, 20, 35, 26),
        (Race.Elf, BaseClass.Mage)    => new BaseStats(25, 37, 23, 24, 32),
        (Race.Human, BaseClass.Fighter) => new BaseStats(43, 40, 15, 30, 25),
        (Race.Human, BaseClass.Mage)    => new BaseStats(27, 41, 20, 21, 39),
        _ => new BaseStats(25, 25, 25, 25, 30)
    };

    // Per design: levels increase hp/mp (max/regen), evasion, accuracy,
    // defence, attack — nothing else. Tanks get more HP, mages more MP.

    // ----- Max HP (authentic L2 model) -------------------------------------
    //  MaxHP = [ ClassLevelMod × (level² + 3·level)/2 + Level1Base ] × ConModifier
    //  (gear/buff/passive % and flat bonuses stack afterwards in RecomputeDerived).

    /// <summary>Per-race+class level-1 base HP (the quadratic's fixed constant).</summary>
    public static int Level1BaseHp(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Human, BaseClass.Fighter) => 126,
        (Race.Human, BaseClass.Mage)    => 88,
        (Race.Elf,   BaseClass.Fighter) => 113,
        (Race.Elf,   BaseClass.Mage)    => 79,
        (Race.Ork,   BaseClass.Fighter) => 137,
        (Race.Ork,   BaseClass.Mage)    => 131,
        _ => 100
    };

    /// <summary>Class HP growth multiplier on the quadratic level term (the L2 "tier").
    /// Tuned so level-75 raw base HP lands on the L2 tracks: tank ~2.9k, warrior ~2.5k,
    /// rogue/archer ~2.0k, nuker ~1.4k, healer ~1.2k. Before 2nd class (no archetype),
    /// a fighter/mage uses a sensible default.</summary>
    public static float HpClassLevelModifier(BaseClass cls, Archetype? arch) => arch switch
    {
        Archetype.Tank    => 1.02f,   // L75 raw ≈ 3100 (the L2 tank track)
        Archetype.Warrior => 0.83f,
        Archetype.Rogue   => 0.66f,
        Archetype.Archer  => 0.66f,
        Archetype.Nuker   => 0.45f,
        Archetype.Healer  => 0.38f,
        _ => cls == BaseClass.Mage ? 0.42f : 0.80f   // base class, pre-2nd
    };

    /// <summary>CON → Max-HP multiplier — interpolated from the real L2 table (baseline
    /// 30 = 1.00). The old exponential was ~7% high mid-range (CON 36 → 1.20 vs 1.12);
    /// the table is accurate at every reference point.</summary>
    public static float ConHpModifier(int con) => InterpolateCurve(ConCurve, con);

    private static readonly (int stat, float mod)[] ConCurve =
    {
        (20, 0.79f), (30, 1.00f), (36, 1.12f), (40, 1.35f),
        (43, 1.48f), (45, 1.57f), (47, 1.67f), (50, 1.83f), (55, 2.14f),
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

    /// <summary>SPT → the Max-MP / M.Def / MP-regen multiplier. This is the old L2 MEN curve, a gentle
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

    public static int MaxHp(int con, int level, float classLevelMod, int level1Base)
    {
        float rawBase = classLevelMod * (level * level + 3f * level) / 2f + level1Base;
        return (int)(rawBase * ConHpModifier(con));
    }

    /// <summary>Mob HP — kept on the simple linear curve. The player formula's
    /// exponential CON modifier would explode on mob-scale CON, so mobs use this.</summary>
    public static int MobMaxHp(int con, int level) => 50 + con * 4 + level * 10;

    // ----- Max MP (authentic L2: Base_MP tier curve × Spirit, like HP) --------
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
    // The stat is the DOMINANT term, as in L2, where regen is
    //   base(level) × levelMod × CONbonus[CON],  CONbonus[c] = 1.03^(c − 27.632)
    // — an EXPONENTIAL in the stat. Ours used to be linear (+0.05 per point), which made CON almost
    // irrelevant: across CON 20→60 our regen moved ×1.33 where L2's moves ×3.25, so a tank and a mage
    // regenerated at nearly the same rate and CON bought you very little.
    //
    // The curve is re-centred on 40 (`stat - 40`) so the MID-RANGE value is exactly what it was before
    // (old: 1 + 40×0.05 + level×k = 3 + level×k, which is the base below). Nothing shifts for an
    // average-CON character; the change is purely that CON now separates builds.

    /// <summary>Per-point regen multiplier for CON (HP). L2's own 1.03 curve.</summary>
    public static float ConRegenBase = 1.03f;

    public static float HpRegenPerSecond(int con, int level) =>
        (3f + level * 0.1f) * (float)Math.Pow(ConRegenBase, con - 40);

    /// <summary>MP regen follows SPT, not WIT (owner, 2026-07-20). WIT is cast speed and magic crit;
    /// it was never meant to be the mana stat. Uses the SAME curve as Max MP and M.Def, so all three
    /// move together off the one stat — which is the whole point of SPT being a stat.
    ///
    /// The base is 2, NOT the 3 the HP curve uses: both come from the old linear formula's constant
    /// term (<c>1 + stat×0.05</c>), but that has to be read at the stat's REAL range. CON sits at
    /// 36-47, so 40 → 3. WIT only ever sat at 10-23, so the honest midpoint is 20 → 2. Using 3 here
    /// would have quietly handed every build 19-36% more MP regen. The /1.4733 renormalises the
    /// curve so a human mage (SPT 39) sits at ×1.00 and the pre-Spirit numbers are preserved.</summary>
    public static float MpRegenPerSecond(int spt, int level) =>
        (2f + level * 0.08f) * (SptModifier(spt) / 1.4733f);

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

    // ----- P.Atk (authentic L2 shape) ---------------------------------------------------------
    //
    // L2's P.Atk is MULTIPLICATIVE: `P.Atk = basePAtk(=WEAPON) × STRbonus × levelMod` (L2J FuncPAtkMod).
    // The WEAPON is the base; the power stat and level only MULTIPLY it. Unarmed, basePAtk is a tiny
    // FIST value, so you punch for almost nothing — no "if unarmed then penalty" branch is needed, the
    // formula does it. Our old form was additive (`atkStat + level·2 + weapon`), which let the 40-point
    // ATK stat leak through with no weapon (a naked L1 fighter had 42 P.Atk and one-shot trash).
    //
    // We keep ONE power stat (ATK) rather than L2's separate STR, so the "STR bonus" is a gentle
    // multiplier off ATK, centred on the fighter base. Only the P channel uses this; M.Atk keeps its
    // own (base × levelMod²) form — that's the signed-off magic balance and it is NOT touched.

    /// <summary>Fist P.Atk when unarmed — the "weapon" value with no weapon. Small on purpose.</summary>
    public const int UnarmedFistPAtk = 3;

    /// <summary>ATK value the P.Atk multiplier is centred on (≈ the human-fighter base) → ×1.0 there.</summary>
    public const int PAtkStatReference = 40;

    /// <summary>The power-stat multiplier for P.Atk. ~1.0 at the fighter base, scaling gently with ATK.</summary>
    public static float PAtkStatMult(int atkStat) => Math.Max(0.2f, atkStat / (float)PAtkStatReference);

    /// <summary>L2-shape P.Atk: (fist + weapon) × ATKbonus × levelMod. <paramref name="weaponPAtk"/> is
    /// the weapon's own P.Atk contribution (its power × the P channel factor); 0 = unarmed.</summary>
    public static int PhysicalAttackPower(int weaponPAtk, int atkStat, int level) =>
        Math.Max(1, (int)((UnarmedFistPAtk + weaponPAtk) * PAtkStatMult(atkStat) * LevelMod(level)));

    // ----- M.Atk (same MULTIPLICATIVE shape as P.Atk; owner 2026-07-25) -----------------------
    //
    // M.Atk was the ONLY channel still ADDITIVE: base = (atkStat + level·2 + weaponM), which put the
    // ~41-point power stat in as a flat FLOOR at every level. That floor dominates low levels (a lvl-1
    // wand mage read ~40 M.Atk where L2 has ~8 → √-damage ~2.2× too high → one-shots) and fades to
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

    // ----- Defence (authentic L2: armor/jewels + level² /100, no CON) ------
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

    // ===== L2-style ratio damage ===========================================
    //
    // Damage is a RATIO of attack to defence (not a subtraction), so defence
    // gives diminishing returns and never fully blocks. lvlMod scales the whole
    // curve by level. Physical and magic share the shape but differ: physical
    // can be EVADED and crits up to x10; magic can FAIL (resist roll) and crits
    // up to x3. Magic currently divides by physical defence too (magic-resist
    // passives/jewels add a separate multiplier later).

    /// <summary>Level modifier: (level+89)/100. L1=0.90, L11=1.00, L80=1.69.</summary>
    public static float LevelMod(int level) => (level + 89) / 100f;

    // ----- The magic level-scaling terms (authentic L2; verified against L2J's
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
    // (We have one ATK power stat rather than L2's separate INT, and it already sits in
    // the base — so only the level term is reproduced here, not INTbonus².)

    /// <summary>M.Atk level scaling: levelMod². Squared on purpose — see the note above.</summary>
    public static float MagicAttackLevelMod(int level) => LevelMod(level) * LevelMod(level);

    /// <summary>M.Def level scaling: levelMod (NOT squared — the counterpart to the above).</summary>
    public static float MagicDefenceLevelMod(int level) => LevelMod(level);

    // Damage balance constants — the authentic L2 constants, unmodified.
    //  Physical: 77·(pAtk + power)/pDef
    //  Magic:    91·power·√mAtk/mDef
    // MagicK was previously 8, on the reasoning that our M.Atk is small compared to L2's.
    // That was backwards: because the formula takes √mAtk, a SMALLER M.Atk needs a LARGER
    // K, not a smaller one. The result was magic doing ~1/11th of its intended damage
    // (a L21 healer hit a same-level tank for 15). Both constants are now L2's own.
    public const float PhysicalK = 77f;
    public const float MagicK = 91f;

    /// <summary>PATH B (owner 2026-07-16): M.Atk is STORED as its displayed value = this scale · √(internal),
    /// so the cosmic `base·levelMod²` number shrinks to P.Atk size while the √ (and its level self-balancing)
    /// is preserved. <see cref="MagicDamage"/> is then LINEAR on the stored value (K/scale reproduces the old
    /// `91·power·√internal/mDef` exactly). The internal value = (shown/scale)².</summary>
    public const float MagicAttackDisplayScale = 20f;

    /// <summary>Physical ratio damage (L2 model): 77·(pAtk + skillPower)/pDef. No level
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
    /// L2 convention: swords are neutral vs armored mobs — here Sword+Dual share the Pierce
    /// track (splittable later without touching callers).</summary>
    public static float WeaponDefenceCoef(WeaponType attacker, float pierce, float blunt, float bow) =>
        (attacker & WeaponType.AnyBlunt) != 0 ? blunt
        : attacker == WeaponType.Bow ? bow
        : (attacker & (WeaponType.AnySword | WeaponType.Dual)) != 0 ? pierce
        : 1f;

    /// <summary>Magic ratio damage (L2 model): K·skillPower·√mAtk/mDef. The SQUARE ROOT
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

    /// <summary>Character crit-rate BASE — his "110" on L2's 0-1000 scale, i.e. 11%.
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
    /// L2 model (docs/design/CritBlowAndDouble.md §5)
    /// <code>crit = (110 × weaponFactor × agiMod × buffs × passives + flat) × debuffs × enemyLightArmor</code>
    /// The WEAPON multiplies the character base — dagger/bow ×1.2 → 13.2%, sword ×0.8 → 8.8%,
    /// blunt ×0.4 → 4.4% — and AGI is a mild multiplier ON that. AGI is NOT the base any more
    /// (it used to be <c>0.05 + agi × 0.0009</c>, which made the weapon a rounding error).
    /// ⚠ Deliberately NOT clamped: passives and buffs multiply this and flat bonuses add to it,
    /// so the single clamp belongs at the END of the chain (Entity.RecomputeDerived).</summary>
    public static float PhysicalCritBase(int agi, WeaponType weapon) =>
        CharacterCritBase * WeaponCritFactor(weapon) * CritAgiMod(agi);

    /// <summary>Character MAGIC crit-rate BASE — his "50" on L2's 0-1000 scale, i.e. 5%.
    /// (owner ruling 2026-08-06; the magic twin of <see cref="CharacterCritBase"/>.)</summary>
    public const float MagicCharacterCritBase = 0.050f;

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
    /// live down there (ork fighter 10, every mob 5), so the stat would hit a hard 0 well
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

    /// <summary>Magic crit DAMAGE multiplier — a FLAT ×3, taking no bonus at all
    /// (owner ruling 2026-08-06).
    /// 🛑 It used to be <c>2.0 + CritDamageBonus</c>, sharing the ONE crit-damage field with
    /// physical — so Ferocity and the crit-damage item attribute, both authored for fighters,
    /// silently paid a mage too. Magic crit is a SEPARATE CHANNEL on both counts now: its own
    /// rate (WIT, not AGI) and its own damage (this constant, not the fighters' buffs). If a
    /// magic crit-damage buff is ever wanted, it needs its OWN field — do not re-point this at
    /// CritDamageBonus.</summary>
    public static float MagicCritMult() => StatCaps.MagicCritDamage;

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
    /// <param name="defenderMod">Defender's magic-fail modifier: 1 normally, 2 with the tank's
    /// Anti-Magic passive.</param>
    /// <param name="weaponMod">Caster's weapon modifier: 1 trained,
    /// <see cref="StatCaps.UntrainedWeaponMagicFailMod"/> with a bow/dual/bare hands.</param>
    /// <param name="defenderFlatPoints">Defender's MAGIC EVASION in percentage points, added after
    /// the multiplicative part (owner ruling 2026-08-11, `62e`: *"the magic evasion should be magic
    /// fail chance like 3-4"*). FLAT and additive on purpose: multiplying it would make a 4-point
    /// dodge worth almost nothing at parity (1% × 1.04) and enormous at a level gap, which is the
    /// opposite of a defensive burst. The only source today is the rogue's Evasion Boost.</param>
    public static float MagicFailChance(int attackerLevel, int defenderLevel,
                                        float defenderMod = 1f, float weaponMod = 1f,
                                        float defenderFlatPoints = 0f)
    {
        float levelMod = MathF.Pow(StatCaps.MagicLevelBase, defenderLevel - attackerLevel);
        float points = MathF.Round(StatCaps.MagicFailParityPoints * levelMod
                                   * Math.Max(0f, defenderMod) * Math.Max(0f, weaponMod))
                     + Math.Max(0f, defenderFlatPoints);
        return Math.Clamp(points / 100f, 0f, StatCaps.MagicFailMax);
    }

    /// <summary>The caster's weapon multiplier for <see cref="MagicFailChance"/>. Untrained =
    /// bow / dual / bare hands, as decided by Spellcaster Mastery in Entity.RecomputeDerived.</summary>
    public static float MagicWeaponFailMod(bool untrainedWeapon) =>
        untrainedWeapon ? StatCaps.UntrainedWeaponMagicFailMod : 1f;

    /// <summary>Offensive MAGIC interrupt power from WIT. Mirrors the WIT scale in
    /// InterruptResist (wit*2) so a WIT-mage out-interrupts an equal-level ATK-mage
    /// while the ATK-mage hits harder. Added to a magic skill's flat InterruptPower.</summary>
    public static int MagicInterruptPower(int wit) => wit * 2;

    // ----- Interruption (caster resist) -----------------------------------

    /// <summary>Base interrupt resistance from WIT + level. Higher = harder to
    /// interrupt. A skill's InterruptDefense adds to this; an attacker's
    /// InterruptPower subtracts. Tune the coefficients here.</summary>
    public static int InterruptResist(int wit, int level) => wit * 2 + level;

    /// <summary>Per-hit chance to interrupt a cast. base + attacker power − caster
    /// resist, clamped. With power=0 and a normal caster, interrupts are
    /// occasional; high InterruptPower (an interrupt skill) makes it reliable;
    /// high InterruptDefense (an ultimate) makes it ~never.</summary>
    public static float InterruptChance(int casterResist, int skillDefense, int attackerPower)
    {
        // Scale the stat difference into a probability. 0 diff ≈ 25% baseline.
        float baseChance = 0.25f;
        float diff = (attackerPower) - (casterResist + skillDefense);
        float chance = baseChance + diff * 0.01f;
        return Math.Clamp(chance, 0f, 1f);
    }

    /// <summary>Chance a contested debuff (slow/stun/root/fear/…) LANDS: the attacker's
    /// ATK (core power stat) vs the defender's resisting stat (CON for physical, WIT for
    /// magical). 50% when equal, scaling by the ratio, clamped to [10%, 90%]
    /// (per docs/design/Disciplines.md). Bosses are made immune by the caller.</summary>
    public static float DebuffLandChance(int attackerAtk, int defenderStat)
    {
        int sum = attackerAtk + defenderStat;
        if (sum <= 0) return 0.5f;
        float chance = 0.5f + 0.5f * (attackerAtk - defenderStat) / sum;
        return Math.Clamp(chance, 0.10f, 0.90f);
    }

    // ----- Cast & attack speed (authentic L2 model) ------------------------
    //
    // L2: actual cast/attack time = baseTime × 333 / speedStat, where the speed stat
    // is built MULTIPLICATIVELY:
    //   castSpd = ClassBaseCast × WitModifier × weaponFactor × gearFactor × ∏(1+buff%)
    // and capped (cast 1999 = 6× faster than the 333 reference, attack 1500). The full
    // assembly lives in Entity.EffectiveCast/AttackSpeedMultiplier; this file provides
    // the class bases, the exponential WIT curve, weapon factors and the 333 reference.

    public const int SpeedBaseline = 333;  // stat value that equals 1.0x speed


    /// <summary>Class base casting speed, before WIT/gear/buffs. This is the ROBED (correct)
    /// value: a mage sits at the 333 baseline = 1.0× cast time. Ork mages are the slow
    /// casters at 300; fighters cast at 150.
    /// It used to be 166 for mages, which silently made EVERY mage cast take ~2× its
    /// nominal time (166 vs the 333 baseline) — a healer's 4s bolt really took ~6.5s.
    /// The "wrong armor" penalty is NOT applied here: Robe Mastery's light/heavy/none
    /// profiles already carry CastSpeedPct −0.5, which halves 333 back down to 166.</summary>
    public static int ClassBaseCastSpeed(Race race, BaseClass cls) =>
        cls != BaseClass.Mage ? 150
        : race == Race.Ork ? 300
        : 333;

    /// <summary>AGI physical-attack-speed modifier — EXPONENTIAL, matching the L2 table
    /// (baseline 30 = 1.00: 20→0.90, 35→1.05, 40→1.11, 50→1.23): ~1.05% per AGI
    /// compounded. Clamped so very low AGI can't stall attacks entirely.</summary>
    public static float AttackAgiModifier(int agi) =>
        Math.Clamp(MathF.Pow(1.0105f, agi - 30), 0.4f, 8f);

    /// <summary>WIT casting-speed modifier — EXPONENTIAL, matching the L2 table
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

    /// <summary>Base gold a mob drops, by level (scaled by RateConfig.GoldAmountRate
    /// and a small variance at the drop site).</summary>
    public static int MobGoldReward(int mobLevel) => 25 + mobLevel * 8;

    /// <summary>Mob stat block by level. Per design: higher-level mobs must
    /// out-stat lower-level characters.</summary>
    // Atk grows level*2 (was level*3, which out-scaled players and 2-shot squishy
    // classes). Tuning knob — raise/lower the level coefficient to make mobs hit
    // harder/softer globally. (Con/Agi unchanged.)
    public static BaseStats MobStats(int level) =>
        // Spt 30 = the neutral middle of the SPT curve. Mobs don't use it (MobMaxMp / MobMagicDefence
        // are their own curves) — it's here so the record is complete rather than defaulting to 0,
        // which would sit at the curve's floor if a mob ever did read it.
        //
        // ⚠ AGI IS FLAT, and deliberately (owner, 2026-08-02). It used to be `10 + level`, which was
        // the real cause of the accuracy collapse: AGI drives accuracy, evasion, crit rate and attack
        // speed, and a PLAYER's AGI never grows. Making accuracy `AGI + level` on both sides does NOT
        // fix that on its own — the level terms cancel and the mob's own AGI growth still runs away.
        // MobAgiReference is the human-fighter base, so a same-level normal mob is a NEUTRAL opponent
        // (5% both ways) and every point of spread comes from gear and passives, where it is earned.
        new(Con: 15 + level * 2, Atk: 8 + level * 2, Wit: 5, Agi: MobAgiReference, Spt: 30);

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
