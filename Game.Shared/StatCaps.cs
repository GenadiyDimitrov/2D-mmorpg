using System;

namespace Game.Shared;

/// <summary>
/// Central place for stat ceilings. Every Effective* getter clamps to its cap
/// here, so caps are discoverable in one spot instead of scattered magic
/// numbers. Most caps are raisable per-entity later (a buff can lift the move
/// cap for a rogue ultimate), so the clamp reads `base cap + bonus` rather than
/// a hard constant.
/// </summary>
public static class StatCaps
{
    /// <summary>Movement speed ceiling for a normal player (fully buffed/geared).
    /// A future rogue ultimate raises this per-entity to outrun even a buffed
    /// mage. Bases sit well below this so gear/buffs climb toward it.</summary>
    public const float MoveSpeed = 250f;

    /// <summary>Attack-speed stat ceiling (IG-style: 1500 ≈ x4.5 attacks/sec).
    /// Enforced once the AGI→attack-speed formula lands (casting round).</summary>
    public const int AttackSpeed = 1500;

    /// <summary>Cast-speed stat ceiling (1999 ≈ x6 faster). Enforced when the
    /// WIT→cast-speed formula lands (casting round).</summary>
    public const int CastSpeed = 1999;

    /// <summary>Physical crit RATE ceiling (50%, from AGI).</summary>
    public const float PhysicalCritRate = 0.50f;

    /// <summary>Magic crit RATE ceiling (20% = his 200 on the 0-1000 scale).
    /// ⚠ A fully-kitted elf mage USED to land on it exactly off Insight alone; since the base
    /// rescale of 2026-08-19 (see <see cref="StatCalculator.MagicCharacterCritBase"/>) he sits
    /// at 16% there and computes 32% at ×4, so this is a real ceiling with headroom under and
    /// over it — which is exactly what the owner asked for ("one day if we want to increase it,
    /// no mage to be short on crit"). Raising this number now genuinely pays a mage.</summary>
    public const float MagicCritRate = 0.20f;

    /// <summary>Physical SKILL "[Double]" RATE ceiling (25%, from the ATK stat — owner
    /// ruling 2026-08-05, docs/design/CritBlowAndDouble.md §1; was 30% off max(AGI,ATK)).</summary>
    public const float PhysicalDoubleRate = 0.25f;

    /// <summary>Physical crit DAMAGE ceiling (x10).</summary>
    public const float PhysicalCritDamage = 10.0f;

    /// <summary>Magic crit DAMAGE — the BASE the multiplier chain starts from (owner ruling
    /// 2026-08-19: *"Magic crit dmg is default x2"*). It was a flat x3 that nothing could touch;
    /// the 4th-class buffer/healer blessings need a knob, so the flat constant became a base and
    /// the knob is <see cref="Entity.MagicCritDamageMult"/>. See StatCalculator.MagicCritMult.</summary>
    public const float MagicCritDamageBase = 2.0f;

    /// <summary>Magic crit DAMAGE ceiling. The designed maximum is x3.38 (base x2 × the buffer's
    /// +30% × the healer's +30%, compounding); this sits above it as headroom, not as a target —
    /// it exists so a future stack of blessings cannot run away, the way PhysicalCritDamage's x10
    /// caps the fighters' flat-crit-damage ladder.</summary>
    public const float MagicCritDamageCap = 5.0f;

    /// <summary>Ceiling on SUMMED interrupt-resistance buffs, as a fraction. Resolve's top rung is
    /// 54%, so nothing today reaches this — it exists because his IG note names a robe set worth
    /// another 50% and the product of the two is what he rejected: *"in the end mages become
    /// [un]interruptable - and i dont want that"*. Any future source stacks INTO this clamp rather
    /// than multiplying past it. The SPT curve is NOT capped here; it is a separate multiplier.</summary>
    public const float InterruptResistMax = 0.80f;

    // ----- Magic landing (owner ruling 2026-08-10, playtest-20 `57d`) ------------------
    // Magic does NOT go through the physical resolver. Its own formula, in percentage POINTS:
    //     fail% = round( LevelBase^(defenderLvl − attackerLvl) × defenderMod × weaponMod )
    // At parity with every modifier at 1 that is round(1) = 1 → a 1% fizzle, 99% success.
    // See StatCalculator.MagicFailChance.

    /// <summary>The level term's base. 1.3^Δ — casting UP is punished hard (Δ+10 ≈ 14% fail,
    /// Δ+16 ≈ 67%), casting DOWN rounds to zero fail from Δ−2 on.</summary>
    public const float MagicLevelBase = 1.3f;

    /// <summary>Fail in percentage POINTS at parity with all modifiers at 1. The whole formula
    /// is scaled off this, so "no level difference = 1% fail" is this constant.</summary>
    public const float MagicFailParityPoints = 1.0f;

    /// <summary>Magic-fail ceiling: 95%, so a spell ALWAYS keeps a 5% roll to land. Deliberately
    /// mirrors AvoidSoftCeil — the owner's playtest-19 `M1` ruling ("nothing is unhittable any
    /// more") applies to the magic channel too, and without this clamp 1.3^Δ makes Δ+18 a hard
    /// lockout. The gap still pays zero exp and zero drops long before that; see ExpCurve.</summary>
    public const float MagicFailMax = 0.95f;

    /// <summary>Magic-fail multiplier for an UNTRAINED caster weapon (bow / dual / bare hands) —
    /// owner ruling 2026-08-10, `57d`. ×25 on the fail points: 25% fail at parity, ~55% at Δ+3,
    /// pinned to the 95% ceiling from Δ+6. Hindered, not disarmed.</summary>
    public const float UntrainedWeaponMagicFailMod = 25f;

    // ----- Contested debuffs: the LEVEL term (owner ruling 2026-08-19) -----------------
    // A contested debuff (stun/root/fear/slow/DoT) used to be a PURE stat ratio with no level
    // term at all, while magic damage and physical accuracy both had one. That asymmetry was
    // invisible because it never bit in the direction you would notice: the mob stat it was
    // measured against carried the level growth instead (CON `15 + 2·level` → 175 at 80, so a
    // high-level creature simply could not be stunned; SPT was never even copied onto the entity,
    // so every magical hold landed at the ceiling). His ruling separates the two concerns:
    //
    //   the STAT contest says who is built for this   (flat numbers, authored per mob role)
    //   the LEVEL term says how far you can reach      (this block)
    //
    // 🔑 THE LEVEL TERM SCALES THE DEFENDER'S STAT, NOT THE CHANCE. The ratio formula
    // (StatCalculator.DebuffLandChance) is symmetric in attacker/defender, so ONE geometric
    // factor on the defender's side lands the floor and the ceiling at exactly ±CcLevelFloorGap
    // and leaves parity at exactly ×1 — *"same level should be x1 (and pure stat vs stat)"*.
    // Scaling the CHANCE instead would flatten every build to the same number at a gap; scaling
    // the STAT keeps a debuff-built caster ahead of a nuker at every gap, which is the point.

    /// <summary>Land-chance floor/ceiling for a contested debuff. Nothing is ever a certainty and
    /// nothing is ever impossible — the same rule as <see cref="MagicFailMax"/> and AvoidSoftCeil.</summary>
    public const float CcLandMin = 0.10f;
    /// <inheritdoc cref="CcLandMin"/>
    public const float CcLandMax = 0.90f;

    /// <summary>Levels of gap at which an EQUAL-STAT contest reaches the floor (casting up) or the
    /// ceiling (casting down). <b>18, matched to the fizzle curve</b> (owner: *"match the fizzel 18 it
    /// is"*): 1.3^18 = 112 fail points, which is where <see cref="MagicFailMax"/> clamps, so the level
    /// at which your spells stop landing and the level at which your control stops landing are the same
    /// level. Change THIS to retune the reach — <see cref="CcLevelBase"/> follows automatically.</summary>
    public const int CcLevelFloorGap = 18;

    /// <summary>The per-level factor on the defender's resisting stat, DERIVED so the floor lands
    /// exactly on <see cref="CcLevelFloorGap"/>: at the floor the defender's stat must be
    /// (1−min)/min = 9× the attacker's, so the base is 9^(1/18) ≈ 1.130. Equal stats then give
    /// 1/(1+base^Δ): 50% at parity, 34.7% at Δ+5, 22.5% at Δ+10, 10% at Δ+18 — and exactly
    /// complementary below (65.3% / 77.5% / 90%).</summary>
    public static readonly float CcLevelBase =
        MathF.Pow((1f - CcLandMin) / CcLandMin, 1f / CcLevelFloorGap);

    // ----- THE DEBUFF SUCCESS MULTIPLIER (owner ruling 2026-08-24, `BL-90`) --------------------
    //
    // *"DebuffLandMod should be floating one value - default 1 … armor/weapon break + gravity +
    //  Arcane/Fros/Pyro blasts(nuker 3rd) should be 75% at parity (x1.5) and the other should be
    //  25% at parity (x0.5)"*
    //
    // 🔑 THERE ARE DELIBERATELY NO TIER CONSTANTS. A first pass gave this four named tiers; he
    // replaced them with "one floating value, default 1" and then authored the values themselves into
    // the CSVs' DESCR column as `(success chance x1.5)`. The number belongs to the SKILL, in his file,
    // not to a tier name in this one — the same rule as every other authored magnitude. Read
    // `SkillDef.DebuffLandMod`; there is nothing to look up here.
    //
    // 🔑 HIS "AT PARITY" ARITHMETIC IS WHAT PINS THE MODEL DOWN. x1.5 = 75% and x0.5 = 25% only if
    // the base at parity is 50% — which is the CONTESTED curve (DebuffLandChance), not the fizzle one
    // (~99%). That is why Armor Break, Weapon Break, Gravity and Mana Strain were moved onto the
    // contested path: their SkillDefs always set DebuffSchool and their descriptions always claimed
    // "Contested ATK vs SPT", but the branch test read the effect-FLAG mask and silently sent them to
    // the fizzle roll instead. See ExecuteSkill / DebuffLandChance.
    //
    // The multiplier is applied AFTER the [CcLandMin, CcLandMax] clamp and the result is re-clamped to
    // [0, CcLandMax]: the 10% floor is a property of the STAT CONTEST (you can always get lucky), so a
    // deliberately unreliable skill is allowed under it, while "nothing is ever a certainty" still
    // holds at the top.

    /// <summary>RANK's multiplier on all THREE contest stats — ATK, CON and SPT together (owner ruling
    /// 2026-08-19: *"elites can get x1.33 atk/con/spt stats increase so it will give them more
    /// resists/chance .. bosses can get x2"*).
    ///
    /// <para>🔑 It multiplies the OFFENSIVE stat as well as the two defensive ones, and that is what
    /// makes a rank read as a BIGGER creature rather than merely a tougher one: an elite is harder to
    /// hold *and* lands its own control more often, off one number. (This supersedes the first pass's
    /// ×1.5-on-defence-only, which had the perverse effect of leaving a boss's signature stun weaker
    /// than a trash mob's.)</para>
    ///
    /// <para>A BOSS takes the ×2 <b>and</b> the control immunity — they are not alternatives. The
    /// immunity covers <see cref="SkillEffect.ControlCc"/> only; against everything else it is merely
    /// very resistant, which is his point exactly: *"even at 10% u still can debuff a boss but
    /// strategicly to not waste mp that can be used to taunt/heal"*. The floor never locks a debuffer
    /// out — it makes the attempt a bad trade, which is a decision rather than a wall.</para></summary>
    public static float CcRankMult(MobRank rank) => rank switch
    {
        MobRank.Elite => 1.33f,
        MobRank.Boss  => 2.0f,
        _             => 1f,
    };

    // ----- Unified hit resolution (see docs/design/CombatResolution.md) -----
    // One resolver decides land-vs-avoid for BOTH channels (physical miss, magic fail).
    // The roll lives inside the [AvoidBase, AvoidSoftCeil] band, which is applied LAST (after the
    // level gap, since 2026-08-07 / playtest-19 M1); true 0/100% are reached only by the
    // Sure-Hit / Immunity flags.

    /// <summary>Avoid (miss/fail) chance at equal stats — the universal base.
    /// Symmetric guarantee: never below 5% to land, never above 95% — at ANY level gap.</summary>
    public const float AvoidBase = 0.05f;

    /// <summary>Soft ceiling on the roll (the 95% land floor lives at 1−this). Only Immunity
    /// can still push the final avoid to 100%; the level gap can no longer (M1).</summary>
    public const float AvoidSoftCeil = 0.95f;

    /// <summary>Avoid chance moved per point of (defenderAvoidStat − attackerHitStat).
    /// First-pass tuning knob (1%/pt); the class floors carry most of the identity.</summary>
    public const float AvoidStatSlope = 0.01f;

    /// <summary>Legacy single crit-chance cap (kept for old references).</summary>
    public const float CritChance = 0.50f;

    /// <summary>Block chance ceiling — a fully-built tank can reach ~100%.</summary>
    public const float BlockChance = 1.0f;

    /// <summary>Block damage-reduction ceiling (max fraction removed on block).</summary>
    public const float BlockReduction = 0.80f;
}

/// <summary>A player's movement/regen state. Walk/Run switch instantly; Sit has
/// a stand-up delay when broken by damage. Regen multipliers stack on top.</summary>
public enum MoveState { Running = 0, Walking = 1, Sitting = 2 }

/// <summary>Speed + regen tuning for movement states.</summary>
public static class MovementTuning
{
    /// <summary>Regen multiplier per stance — HP and MP alike. Owner's ladder, 2026-08-26 (`BL-92`),
    /// adapted from IG's own (moving ×0.7, standing ×1, sitting ×1.5):
    ///
    ///     running 0.70 · walking 0.85 · STANDING STILL 1.00 · sitting 1.50
    ///
    /// 🔑 <b>STANDING IS NEW AND IT IS THE WHOLE POINT.</b> Until now we had no standing state at all
    /// — a player who stopped moving was still <see cref="MoveState.Running"/> — which is exactly why
    /// IG's ladder could not be copied and why the old one had to run BACKWARDS (running ×1.0 as the
    /// baseline, walking a ×1.2 bonus). With a standing rung the baseline sits where IG puts it and
    /// movement is a COST, so the farm loop becomes a real choice: stand or walk to recover, run to
    /// fight and pay for it.
    ///
    /// ⚠ <paramref name="moving"/> is DERIVED (an entity with no <c>TargetX</c> is standing), NOT a
    /// new <see cref="MoveState"/> value: that enum is PERSISTED on characters and on the wire —
    /// Running=0, Walking=1, Sitting=2 — and must never be renumbered. Sitting ignores the flag.</summary>
    public static float RegenMultiplier(MoveState state, bool moving = true) => state switch
    {
        MoveState.Sitting => 1.5f,
        _ when !moving    => 1.0f,          // standing still (incl. casting / trading blows)
        MoveState.Walking => 0.85f,
        _                 => 0.70f,         // running
    };

    /// <summary>Fraction of run speed used while walking.</summary>
    public const float WalkSpeedFactor = 0.5f;

    /// <summary>Ticks to stand up — after being hit OR choosing to stand — before you can move / cast /
    /// act again (the standing animation). 3s at 10 ticks/sec (owner, 2026-07-23).</summary>
    public const int StandUpTicks = 30;

    /// <summary>Seconds seated after which standing up is INSTANT (owner, 2026-07-24). The stand-up
    /// recovery exists to stop sit/stand SPAM — tapping sit for the regen tick and popping straight back
    /// up — so it should only cost that. Someone who genuinely rested has already spent far more time
    /// than the delay, and charging them again just makes resting feel bad. Being HIT while seated still
    /// pays the full recovery regardless: that is a combat interrupt, not a voluntary stand.</summary>
    public const float SettledSeconds = 3f;
}

/// <summary>
/// `BL-122` — BASE RUN SPEEDS PER RACE + BASE CLASS, AUTHORED BY THE OWNER 2026-09-02. Six numbers,
/// given verbatim; 250 is the buffed CAP (per-entity, see <see cref="Entity"/>.MoveSpeedCap) and
/// these sit far below it so buffs and passives climb toward it. Walk speed is derived
/// (<see cref="MovementTuning.WalkSpeedFactor"/>).
///
/// <para>🔑 <b>THE TARGET IS THE BUFFED FIGURE, NOT THE BASE.</b> His *"speed should be around 180
/// for slow and 210 for faster classes"* describes where a party-buffed player LANDS, not what this
/// table holds — I read it as the base first and proposed 180-210 here, which would have been ~65
/// points too fast. The buff stack is <b>+61</b> (Swift / Wind Grace +33, Harmony of Speed +20,
/// Frenzy +8), so the table below produces 170-204: Human fighter 115+61 = 176 ≈ his "180 for slow",
/// Elf fighter 143+61 = 204 ≈ his "210 for faster classes". A rogue's own +60 from sprint + passives
/// then puts him over 250, which is his *"they usually max it out"*.</para>
///
/// <para>⚠ <b>NO DEX TERM, DELIBERATELY.</b> *"IG is base class+race speed x dex mod but i dont want
/// dex to affect speed (rogues have enough passives so their ms to rise even more)"*. Nothing in the
/// codebase has ever multiplied speed by DEX, so this is a rule to KEEP, not a change to make — do
/// not "restore" the IG modifier when porting a formula from a reference table that carries it.</para>
///
/// <para>⚠ The old ordering comment is gone with the old numbers: <b>fighter no longer beats mage in
/// every race.</b> The Demon's two are 112 fighter / 113 mage — the mage is a point FASTER — and the
/// Elf fighter's 143 is a 28-point outlier over every other row rather than the top of a smooth
/// ladder. That is his table as given, and none of it is a typo to interpolate away
/// (cf. the monotonic-ladder rule, which is about a LADDER of rungs, not across race rows).</para>
/// </summary>
public static class SpeedTable
{
    public static float BaseRunSpeed(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Elf,     BaseClass.Fighter) => 143f,
        (Race.Elf,     BaseClass.Mage)    => 114f,
        (Race.Demon,   BaseClass.Fighter) => 112f,
        (Race.Demon,   BaseClass.Mage)    => 113f,
        (Race.Human,   BaseClass.Fighter) => 115f,
        (Race.Human,   BaseClass.Mage)    => 109f,
        // (The God debug race's 200 was deleted 2026-08-07 with the layer — `/spd m <v>` replaces it.)
        // Unreachable — all six combinations are covered — but kept as the human fighter, not as an
        // invented number that would silently become the fastest row if a race were ever added.
        _ => 115f
    };
}
