using System.Linq;

namespace Game.Shared;

// ===========================================================================
//  THE BULWARK (tank), 76-90 — `docs/data/classes_skills_csv/tank 4th.csv`, 205 authored rows.
//
//  ✅ HE CALLED THE TANK FINISHED ON 2026-09-04: *"im done with tank 2/3/4 so its ready to build
//  after the npc buffer"*. The `NOT DONE` banner is gone from the file, the three placeholder rows
//  this file used to hold are laddered 76→90 with real numbers, and `tank 4th` earned its line in
//  `Check.Specs` the same day. Everything below is read off his rows.
//
//  🔑 THE SHAPE OF THE TIER, and it is the healer's and the buffer's shape exactly:
//      · TWO BAND SHAPES — every level 76-90 for the two masteries (fifteen rungs), every OTHER
//        level for everything else (eight rungs). The six whisp CALLS split into the same A/B sets
//        the 3rd tier uses: the A whisps ladder 76, 78 … 90 and the B whisps 77, 79 … 89 then 90.
//      · ONE PRICE LADDER — `F4` for raising a skill you own, `F4New` for one first learned here.
//        Up to 79 a rung costs SP and a token gold; from 80 it costs NO SP at all and gold that
//        climbs 5kk → 100kk. That is why so many rungs below read `SpCost: 0`.
//
//  🔑 WHAT IS NEW AT THIS TIER (seven things, and nothing else was invented):
//      · WEAPON MASTERY  — fifteen rungs, and it gains ATTACK SPEED for the first time. ⚠ HE HAD
//        FORGOTTEN IT and wrote the rows the same day, after the first build shipped without them
//        (*"and I have forgotten the weapon mastery"*). The 40+ rule earned its keep here: the answer
//        to a ladder his file does not carry is to ASK, never to invent one.
//      · MAGIC WALL      — the M.Def half of Defensive Wall with no movement price, 76 → 90.
//      · TAUNTING WALL   — one rung at 80: a mass taunt AND a Defensive Wall, in one cast.
//      · PERFECT WHISP   — one whisp that does what six do, 80 → 90.
//      · BACKLASH        — three rungs, race-split, and BOUGHT now (see Skills.Common.cs).
//      · WHISP MASTERY 2 — the third slot, at 83, exactly where his 3rd-tier note said it would be.
//      · SILENCING SHOCK — the Elf's magical silence, laddered beside the Human/Demon's Numbing
//        Shock. `BL-155`'s engine has served both since 0.110.0; these are the authored rows.
//
//  ✅ TWO LADDER DIPS WERE REFUSED AND HE THEN RATIFIED BOTH (2026-09-04, *"fix all the csv to match
//  what you told me needed fixing"*). Recorded because the reasoning is the standing monotonic rule
//  doing its job, and because the first one's ORIGIN is now known:
//      1. HEAVY ARMOR MASTERY's `mpReg` read **x3.4 on all fifteen rows**, against **x5.1** at the
//         last 3rd-tier rung. 🔑 IT IS THE MAGE'S NUMBER: `healer 4th.csv` carries `mpReg +3.4` on all
//         fifteen of ITS armour-mastery rows, and his own words were *"the mage's one we did, it
//         stayed from there"*. Held at **5.1** and the cells corrected.
//      2. SHIELD SMASH - POWER's crit-damage ladder RESTARTED at `15%` against `35%` at 74, then
//         re-trod 19/24/28/33 back up. Its twin, Shield Smash - Rate, is FLAT at its own ceiling
//         (50%/25%) for all eight 4th-tier rungs, so this one is flat at 35%/15% — on both sides now.
//
//  ⚠ ONE ID IN HIS FILE WAS A PASTE AND IS CORRECTED ON BOTH SIDES: the eight `Silencing Shock` rows
//  carried SKILL_ID `tank_shield_stun` — Shield Shock's id — while being a different name, a
//  different TYPE (`Magical/Debuff` vs `physical active`), a different range, cast, reuse, duration
//  and the only rows in the block with a RACE. Two skills cannot share one id: the engine keys
//  cooldowns, saved bars and buff families on it. Set to `tank_silence_magical`, which is what
//  `BL-155` built the engine half against.
// ===========================================================================

public partial class SkillCatalog
{
    // ---- The ids that are new at the 4th tier. Everything else in his file EXTENDS a skill that
    //      already exists: provoke, charm, mass_provoke, tank_fear, tank_freeze, tank_stay,
    //      tank_shield_stun, tank_smash_rate/power, tank_armor_mastery, tank_anti_magic,
    //      defensive_wall, tank_pull, tank_silence_physical/magical, backlash, undying_will and the
    //      six whisp calls.
    public const string TankPull            = "tank_pull";
    public const string TankSilencePhysical = "tank_silence_physical";
    public const string TankSilenceMagical  = "tank_silence_magical";
    public const string TankMagicWall       = "magic_wall";
    public const string TankTauntingWall    = "tauting_wall";          // his spelling, kept: it is the id
    /// <summary>Tauting Wall's own half of itself. A payload def, never learned and never on a bar —
    /// the same shape Aggravated State's proc rungs use, and the thing <see cref="SkillDef.SelfBuff"/>
    /// points at.</summary>
    public const string TankTauntingWallGuard = "tauting_wall_guard";
    public const string TankWhispHelp       = "tank_whisp_help";

    // ---- The Perfect Whisp's own kit. Its gears are the SAME SIX BEHAVIOURS the six single whisps
    //      have, at their own numbers — so they need their own ids: a whisp reads every skill it
    //      casts at the SUMMON's rung (`w.Level`), and the Perfect Whisp has six rungs where the
    //      single whisps have sixteen. Pointing it at `whisp_heal` would have given its level-80
    //      first rung the level-46 healing whisp's power.
    //      ⚠ `whisp_clear` IS reused as-is: a cleanse has no ladder (two effects at every rung), so
    //      there is nothing for a second id to carry.
    public const string WhispGreatHeal        = "whisp_great_heal";
    public const string WhispMana             = "whisp_mana";
    public const string WhispGreatArmorBreak  = "whisp_great_armor_break";
    public const string WhispGreatWeaponBreak = "whisp_great_weapon_break";
    public const string WhispGreatGravity     = "whisp_great_gravity";

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  HIS BAND SHAPES. Read off the LEARN @ LVL column, not derived.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Every level, 76-90. Heavy Armor Mastery and Tank Anti-Magic only.</summary>
    internal static readonly int[] TankFourthAll =
        { 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

    /// <summary>Every OTHER level from 76. Eight rungs — almost the whole file.</summary>
    internal static readonly int[] TankFourthEven =
        { 76, 78, 80, 82, 84, 86, 88, 90 };

    /// <summary>Every other level from 77 — the B whisps (Binding / Healing / Weapon Breaking), which
    /// open one level later at every tier.
    /// <para>✅ <b>ITS LAST RUNG IS 90, NOT 91.</b> Eight rungs from an odd start land on 91, and it
    /// shipped that way for one commit because the level was in his file and <c>ExpCurve.MaxLevel</c>
    /// is 100, so it was reachable rather than dead — flagged rather than straightened, since
    /// compressing a ladder he authored would have been an invention. It was a TYPO and he said so:
    /// *"The whisps that are 91 should be lvl 90. The intelisence of vsCode or whatever with tab key
    /// make it go by 2 from 89lvl and I missed it"*. 🔑 <b>An editor's autofill is a source of typos
    /// like any other</b> — the last rung of an odd-start ladder is where to look for one.</para></summary>
    internal static readonly int[] TankFourthOdd =
        { 77, 79, 81, 83, 85, 87, 89, 90 };

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  HIS MP LADDERS. Four of them, shared across the file exactly as he wrote them.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The CONTROL ladder — Mass Taunt, Intimidate, Freeze, Stay. 115 → 185, +10 a rung.</summary>
    private static readonly int[] TankFourthControlMp = { 115, 125, 135, 145, 155, 165, 175, 185 };

    /// <summary>The BASH ladder — both Shield Smashes, Shield Shock, both silences and Grapple.
    /// 123 → 165, +6 a rung.</summary>
    private static readonly int[] TankFourthBashMp = { 123, 129, 135, 141, 147, 153, 159, 165 };

    /// <summary>The WHISP ladder — all six calls and the Perfect Whisp's tail. ⚠ Its stride widens at
    /// the top: +10 to rung 6, then +20 twice (160 → 180 → 200). His column, verbatim.</summary>
    private static readonly int[] TankFourthWhispMp = { 110, 120, 130, 140, 150, 160, 180, 200 };

    /// <summary>Build <paramref name="count"/> rungs over one of the band shapes, handing the builder
    /// the rung index and that rung's SP and gold. Step 1 = <see cref="TankFourthAll"/>, step 2 = the
    /// two every-other-level shapes (which are priced identically — his B-whisp rows carry the same
    /// 6.5kk / 16kk / 0 … column as the A ones).</summary>
    private static SkillLevel[] T4Rungs(int count, int step, System.Func<int, int, int, SkillLevel> mk) =>
        Enumerable.Range(0, count).Select(i => { var (sp, g) = F4(i, step); return mk(i, sp, g); }).ToArray();

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS. Each returns ONLY the 4th-tier rungs; the definition site concatenates.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>HEAVY ARMOR MASTERY rungs 21-35. P.Def 175 → 245, ×1.20 → ×1.30 P.Def, crit-damage
    /// reduction 35% → 50%, evasion −3 → −6.
    /// <para>🔴 The `mpReg` column reads x3.4 on every row and the 3rd tier ends at x5.1 — see the
    /// dip note at the top of this file. Held at 5.1.</para></summary>
    private static readonly int[] TankFourthArmorPDef =
        { 175, 180, 185, 190, 195, 200, 205, 210, 215, 220, 225, 230, 235, 240, 245 };
    private static readonly float[] TankFourthArmorPDefPct =
        { .20f, .20f, .20f, .20f, .25f, .25f, .25f, .25f, .25f, .30f, .30f, .30f, .30f, .30f, .30f };
    private static readonly float[] TankFourthArmorCritRed =
        { .35f, .35f, .35f, .40f, .40f, .40f, .45f, .45f, .45f, .50f, .50f, .50f, .50f, .50f, .50f };
    private static readonly int[] TankFourthArmorEva =
        { -3, -3, -3, -4, -4, -4, -5, -5, -5, -6, -6, -6, -6, -6, -6 };
    /// <summary>MP regen, and it is a <b>FLAT PER-SECOND GRANT</b> — <c>PassiveEffect.MpRegen</c>, not
    /// <c>MpRegenPct</c>. His ruling, 2026-09-04: *"the mp regen of tank is also additive, not
    /// multiplicative … it should be +5.1 and build that way"*, and the `x` in his 3rd- and 4th-tier
    /// cells was a notation slip — his own `tank 2nd.csv` writes `mpReg +3.1` at level 36. Read that
    /// way the whole column is ONE continuous ladder across three tiers, 3.1 → 3.5 → 3.9 → 4.3 → 4.7
    /// → 5.1, in the same units the mage's armour mastery has always used (`healer 4th.csv`: `+3.4`).
    /// <para>⚠ 5.1 (not the 3.4 his 4th-tier cells first carried) is the 3rd tier's last authored
    /// value, held there by the monotonic rule and then ratified by him — see the file header.</para></summary>
    private const float TankFourthArmorMpReg = 5.1f;

    internal static SkillLevel[] TankFourthArmorMasteryRungs() => T4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"With heavy armor: +{TankFourthArmorPDef[i]} P.Def, "
                       + $"×{1f + TankFourthArmorPDefPct[i]:0.00} P.Def, +{TankFourthArmorMpReg:0.0} MP regen/s, "
                       + $"{TankFourthArmorCritRed[i] * 100:0}% less crit damage taken, "
                       + $"{TankFourthArmorEva[i]} evasion."));

    /// <summary>The armour PROFILES for those fifteen rungs — a parallel array, because an armour
    /// mastery's payload rides <c>ArmorMasteryLevels</c> and not the SkillLevel.</summary>
    internal static ArmorMasteryProfile[] TankFourthArmorMasteryProfiles() =>
        Enumerable.Range(0, TankFourthAll.Length).Select(i => new ArmorMasteryProfile(
            Robe: default, Light: default,
            Heavy: new StatMods(
                MpRegen: TankFourthArmorMpReg,   // "mpReg +5.1" is a FLAT grant per second
                PDef: TankFourthArmorPDef[i], PDefPct: TankFourthArmorPDefPct[i],
                CritDmgResist: TankFourthArmorCritRed[i], Evasion: TankFourthArmorEva[i]))).ToArray();

    /// <summary>TANK ANTI-MAGIC rungs 21-35. M.Def 132 → 160 (+2 a rung), magic resistance flat at
    /// 20% — the 3rd tier's ceiling, and it does not move again.
    /// <para>🔑 FROM LEVEL 80 his cell gains *"Twice more chance magic to fail against you"*, which is
    /// <see cref="PassiveEffect.MagicFailMod"/> = 2. The tank is already auto-granted the archetype's
    /// own Anti-Magic at ×2 and this field takes the MAX across passives, so today it changes no
    /// number — it is authored anyway because it is now HIS, on the tank's OWN skill, and would
    /// survive the archetype grant being retuned or removed.</para></summary>
    internal static SkillLevel[] TankFourthAntiMagicRungs() => T4Rungs(15, 1, (i, sp, gold) =>
    {
        int mDef = 132 + i * 2;
        bool fizzle = TankFourthAll[i] >= 80;
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Passive: new PassiveEffect(MagicDefence: mDef, MagicResist: 0.20f,
                                       MagicFailMod: fizzle ? 2f : 0f),
            Description: $"+{mDef} M.Def and 20% magic resistance."
                       + (fizzle ? " Hostile spells are twice as likely to fizzle on you." : ""));
    });

    /// <summary>TANK WEAPON MASTERY rungs 21-35 — <b>the ladder he forgot and added on 2026-09-04</b>:
    /// *"Make the 15 rungs of it in the csv, going from p.atk +90@76 to +200@90, keeps the x1.085 patk
    /// as well and adds 1% @76 to 79, 3% @80 to 84 and +5% @85+ atack speed"*.
    ///
    /// <para>🔑 ATTACK SPEED IS NEW TO THIS PASSIVE. Its 2nd and 3rd tiers are flat P.Atk plus ×1.085
    /// and nothing else; the 4th is where a Bulwark's plain sword-and-board finally starts swinging
    /// faster. Three flat bands, not a per-rung ladder — his own shape, so a rung inside a band buys
    /// only the flat P.Atk.</para>
    ///
    /// <para>⚠ THE FLAT LADDER IS THE STRAIGHT LINE BETWEEN HIS TWO ENDPOINTS, rounded — he named 90
    /// and 200 and nothing in between, so 110 spread over fourteen steps is 7.857 a rung. It is
    /// monotonic at every step, which is the only property the rule cares about; if he wants a shape
    /// rather than a line it is one array.</para></summary>
    private static readonly int[] TankFourthWeaponFlatAtk =
        { 90, 98, 106, 114, 121, 129, 137, 145, 153, 161, 169, 176, 184, 192, 200 };

    /// <summary>His three attack-speed bands: +1% at 76-79, +3% at 80-84, +5% from 85.</summary>
    private static float TankFourthWeaponAtkSpeed(int i) =>
        TankFourthAll[i] <= 79 ? 0.01f : TankFourthAll[i] <= 84 ? 0.03f : 0.05f;

    internal static SkillLevel[] TankFourthWeaponMasteryRungs() => T4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"With a one-handed sword or blunt: ×1.085 P.Atk, "
                       + $"+{TankFourthWeaponFlatAtk[i]} P.Atk and "
                       + $"+{TankFourthWeaponAtkSpeed(i) * 100:0}% attack speed."));

    /// <summary>The weapon PROFILES for those fifteen rungs — the parallel array, because a weapon
    /// mastery's payload rides <c>SkillDef.WeaponMasteryLevels</c> and not the SkillLevel. ⚠ It must
    /// stay the same LENGTH as the rungs above: a rung in one and not the other is a rung you can buy
    /// that grants nothing.</summary>
    internal static WeaponMasteryProfile[] TankFourthWeaponMasteryProfiles() =>
        Enumerable.Range(0, TankFourthAll.Length)
            .Select(i => OneHand(new PassiveEffect(
                PhysAtkPct: 0.085f, PhysAtk: TankFourthWeaponFlatAtk[i],
                AtkSpeedPct: TankFourthWeaponAtkSpeed(i))))
            .ToArray();

    /// <summary>The AGGRO ladder Taunt and Charm share at the 4th tier, rungs 20-27: 12,400 → 18,000,
    /// +800 a rung. His two blocks carry identical numbers, exactly as they did at the 3rd tier —
    /// an Elf tank holds a monster as well as a Human one, which is the whole point of Charm
    /// replacing Taunt rather than sitting beside it.</summary>
    private static readonly int[] TankFourthAggro =
        { 12400, 13200, 14000, 14800, 15600, 16400, 17200, 18000 };

    /// <summary>TAUNT rungs 20-27. Range 800 (where the 3rd tier left it), MP still 0.</summary>
    internal static SkillLevel[] TankFourthTauntRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: 0, SpCost: sp, GoldCost: gold, Range: 800f,
            TauntPower: TankFourthAggro[i],
            Description: $"Locks a monster onto you for 1.5s and adds {TankFourthAggro[i]:N0} to your "
                       + "aggro on it. It does not put you at the top for free — hold it by keeping "
                       + "the taunt up."));

    /// <summary>CHARM rungs 20-27. The same aggro, and still free to cast.</summary>
    internal static SkillLevel[] TankFourthCharmRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: 0, SpCost: sp, GoldCost: gold, Range: 800f,
            TauntPower: TankFourthAggro[i],
            Description: $"Lures an enemy toward you for 3s and adds {TankFourthAggro[i]:N0} to your "
                       + "aggro on it."));

    /// <summary>MASS TAUNT rungs 16-23. 7,480 → 11,400 per head, +560 a rung — a smaller ladder than
    /// the single-target one's, which is the right trade for hitting a pack.</summary>
    internal static SkillLevel[] TankFourthMassTauntRungs() => T4Rungs(8, 2, (i, sp, gold) =>
    {
        int power = 7480 + i * 560;
        return new SkillLevel(MpCost: TankFourthControlMp[i], SpCost: sp, GoldCost: gold,
            AreaRadius: 400f, TauntPower: power,
            Description: $"Taunts everything within 400 for 3s and adds {power:N0} to your aggro on each.");
    });

    /// <summary>INTIMIDATE rungs 16-23. Nothing but price, MP and reach moves — a fear is a fear, and
    /// what a rung buys is the level contest (`DebuffLandChance` reads the RUNG's learn level).</summary>
    internal static SkillLevel[] TankFourthFearRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthControlMp[i], SpCost: sp, GoldCost: gold, Range: 800f));

    /// <summary>FREEZE rungs 16-23. The slow finally moves again after plateauing at 50% for the
    /// whole top half of the 3rd tier: 55% → 65%.</summary>
    internal static SkillLevel[] TankFourthFreezeRungs() => T4Rungs(8, 2, (i, sp, gold) =>
    {
        float[] slow = { .55f, .55f, .60f, .60f, .60f, .65f, .65f, .65f };
        return new SkillLevel(MpCost: TankFourthControlMp[i], SpCost: sp, GoldCost: gold, Range: 800f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, slow[i]) },
            Description: $"Cuts an enemy's movement by {slow[i] * 100:0}% for 30s.");
    });

    /// <summary>STAY rungs 16-23. Ten seconds throughout; the REACH steps 400 → 600 at level 84,
    /// which is the one thing his rows move.</summary>
    internal static SkillLevel[] TankFourthStayRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthControlMp[i], SpCost: sp, GoldCost: gold,
            Range: i >= 4 ? 600f : 400f));

    /// <summary>SHIELD SHOCK rungs 20-27. Price and MP only — nine seconds of stun on a three-second
    /// reuse at ×0.7 landing is already the whole design, and he moved none of it.</summary>
    internal static SkillLevel[] TankFourthShieldShockRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold));

    /// <summary>The POWER ladder both Shield Smashes share at the 4th tier, rungs 16-23:
    /// 4,200 → 7,200. ⚠ Its stride widens at 86 (+400 becomes +600 then +400 again) — his column.</summary>
    private static readonly int[] TankFourthSmashPower =
        { 4200, 4600, 5000, 5400, 5800, 6400, 6800, 7200 };

    /// <summary>SHIELD SMASH - RATE rungs 16-23. The crit-rate penalties are FLAT at the 3rd tier's
    /// ceiling for all eight rungs — 50% P.Crit rate, 25% M.Crit rate — and only the damage climbs.
    /// That is his file, and it is what settles its twin below.</summary>
    internal static SkillLevel[] TankFourthSmashRateRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold,
            Power: TankFourthSmashPower[i], CritRatePenalty: 0.50f, MagicCritRatePenalty: 0.25f,
            Description: $"Power {TankFourthSmashPower[i]:N0}; −50% P.Crit rate and −25% M.Crit rate for 30s."));

    /// <summary>SHIELD SMASH - POWER rungs 16-23. FLAT at the 3rd tier's ceiling — 35% P.Crit damage,
    /// 15% M.Crit damage — for all eight rungs, exactly as its Rate twin is flat at 50%/25%.
    /// <para>✅ His column originally restarted at 15% and re-trod 19/24/28/33 back to 35, which would
    /// have made a level-76 Bulwark's smash worse than his level-74 one. Held flat here and reported;
    /// he then told me to *"fix all the csv to match what you told me needed fixing"*, so the eight
    /// cells now say 35% too and the two sides agree.</para></summary>
    internal static SkillLevel[] TankFourthSmashPowerRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold,
            Power: TankFourthSmashPower[i], CritDamagePenalty: 0.35f, MagicCritDamageDebuff: 0.15f,
            Description: $"Power {TankFourthSmashPower[i]:N0}; −35% P.Crit damage and −15% M.Crit damage for 30s."));

    /// <summary>DEFENSIVE WALL rungs 3-10 — and this is where the skill becomes an ULTIMATE rather
    /// than a panic button. Four things move together and all four are his:
    /// P.Def 3,900 → 5,000 · M.Def 3,400 → 4,500 · reuse 81s → 30s · duration 33s → 60s.
    /// <para>⚠ AND THE MOVEMENT PRICE DEEPENS AS IT GETS STRONGER — ms ×0.45 at 76 down to ×0.20 at
    /// 90. That is not a dip: every other column improves, and a wall that roots you harder the
    /// higher you take it is the trade the skill is made of. Cancel resistance holds at ×1.8.</para></summary>
    internal static SkillLevel[] TankFourthDefensiveWallRungs()
    {
        int[] pDef  = { 3900, 4100, 4300, 4500, 4700, 4800, 4900, 5000 };
        int[] mDef  = { 3400, 3600, 3800, 4000, 4200, 4300, 4400, 4500 };
        float[] ms  = { .45f, .40f, .35f, .30f, .25f, .23f, .22f, .20f };   // the MULTIPLIER his cell writes
        int[] reuse = { 810, 720, 630, 540, 450, 360, 330, 300 };           // seconds
        int[] dur   = { 33, 36, 39, 42, 45, 50, 55, 60 };                   // seconds
        return T4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
            MpCost: 50, SpCost: sp, GoldCost: i == 0 ? 1_000 : gold,
            CooldownTicks: reuse[i] * 10, DurationTicks: dur[i] * 10,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, pDef[i], ModifierMode.Flat),
                new(SkillEffect.BuffMagicDef, mDef[i], ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMoveSpeed, ms[i] - 1f, ModifierMode.Percent),
            },
            Description: $"Raise an impregnable guard for {dur[i]}s: +{pDef[i]:N0} P.Def, "
                       + $"+{mDef[i]:N0} M.Def and high cancel resistance, but your movement drops to "
                       + $"×{ms[i]:0.00}."));
    }

    /// <summary>BACKLASH's six rungs, and the ONE place in the tank's kit where a rung index carries
    /// a race. 1-3 are Physical Backlash (Human + Demon), 4-6 Magical Backlash (Elf) — see the note
    /// on the def in Skills.Common.cs for why one id and not two.
    /// <para>His numbers: the matching school 10/20/30%, the opposite one 5/10/15%. Levels 77/80/83,
    /// so the price is <see cref="F4New"/>'s — except the first rung, which he priced at the 76 SP
    /// (6.5kk) and a token 1k of gold rather than 77's 11kk.</para></summary>
    internal static SkillLevel[] BacklashRungs()
    {
        float[] strong = { .10f, .20f, .30f };
        float[] weak   = { .05f, .10f, .15f };
        int[] sp   = { 6_500_000, 150_000_000, 500_000_000 };
        int[] gold = { 1_000, 10_000_000, 100_000_000 };

        SkillLevel Rung(int i, bool physical) => new(
            SpCost: sp[i], GoldCost: gold[i],
            Passive: new PassiveEffect(
                DebuffReflectPhysChance:  physical ? strong[i] : weak[i],
                DebuffReflectMagicChance: physical ? weak[i]   : strong[i]),
            Description: physical
                ? $"{strong[i] * 100:0}% chance a CON debuff and {weak[i] * 100:0}% chance an SPT "
                + "debuff cast at you lands on its caster instead."
                : $"{strong[i] * 100:0}% chance an SPT debuff and {weak[i] * 100:0}% chance a CON "
                + "debuff cast at you lands on its caster instead.");

        return new[] { Rung(0, true), Rung(1, true), Rung(2, true),
                       Rung(0, false), Rung(1, false), Rung(2, false) };
    }

    /// <summary>WHISP MASTERY's second rung, at 83 — *"Increase the limit of active whisps to 3"*.
    /// The base is 1 and the passive ADDS, so rung 2 carries 2. Its price is the LEVELLING ladder's
    /// (SP 0, 15kk gold at 83), not the new-skill one: he is raising a mastery he already owns.</summary>
    internal static SkillLevel TankFourthWhispMasteryRung()
    {
        var (sp, gold) = F4(83 - 76);
        return new SkillLevel(SpCost: sp, GoldCost: gold,
            Passive: new PassiveEffect(WhispSlots: 2),
            Description: "You may keep three whisps at once.");
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE SIX WHISP CALLS, rungs 9-16, and the whisp skills they carry.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>A summon's eight 4th-tier rungs. Identical for all six but the price ladder, exactly
    /// as they were at the 3rd tier — his six blocks are one block written six times.
    /// <para>⚠ The REAGENT does not move: four Skill Stones a call, at every rung.</para></summary>
    internal static SkillLevel[] TankFourthWhispSummonRungs() => T4Rungs(8, 2, (i, sp, gold) =>
        new SkillLevel(MpCost: TankFourthWhispMp[i], SpCost: sp, GoldCost: gold));

    /// <summary>The taunting and charming whisps' aggro at rungs 9-16 — the SAME ladder the tank's
    /// own Taunt and Charm run on, cell for cell, which is the arithmetic behind his *"charm also
    /// adds aggro points"*.</summary>
    internal static SkillLevel[] WhispFourthThreatRungs() =>
        TankFourthAggro.Select(t => new SkillLevel(TauntPower: t)).ToArray();

    /// <summary>The healing whisp's power at rungs 9-16: 850 → 1,200, +50 a rung. A straight line,
    /// like the 3rd tier's +70 — not a curve, and not something to "fix".</summary>
    internal static SkillLevel[] WhispFourthHealRungs() =>
        Enumerable.Range(0, 8).Select(i => new SkillLevel(Power: 850 + i * 50)).ToArray();

    /// <summary>The armor-breaking whisp at rungs 9-16: P.Def 31% → 40%, M.Def 15% → 20%.
    /// ⚠ The 3rd tier's "M.Def is exactly half P.Def" identity BREAKS here and it is his — at rung 16
    /// it is 40/20, which still holds, but the middle of the ladder does not (33/16, 35/17). Read off
    /// the cells rather than derived, which is why both arrays are written out.</summary>
    internal static SkillLevel[] WhispFourthArmorBreakRungs()
    {
        float[] pDef = { .31f, .32f, .33f, .34f, .35f, .36f, .38f, .40f };
        float[] mDef = { .15f, .16f, .16f, .17f, .17f, .18f, .19f, .20f };
        return Enumerable.Range(0, 8).Select(i => new SkillLevel(
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, pDef[i]),
                new(SkillEffect.BuffMagicDef, -mDef[i]),
            },
            Description: $"−{pDef[i] * 100:0}% P.Def and −{mDef[i] * 100:0}% M.Def for 15s.")).ToArray();
    }

    /// <summary>The weapon-breaking whisp at rungs 9-16: 16% → 25% off both attack channels.</summary>
    internal static SkillLevel[] WhispFourthWeaponBreakRungs()
    {
        float[] atk = { .16f, .17f, .18f, .19f, .20f, .21f, .23f, .25f };
        return atk.Select(v => new SkillLevel(
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, v) },
            Description: $"−{v * 100:0}% P.Atk and M.Atk for 15s.")).ToArray();
    }

    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE DEFS — the six skills that are new at this tier, plus the two silences and the pull
    //  rebuilt against his authored rows.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Bulwark4thSkills() => new SkillDef[]
    {
        // ═══ GRAPPLE — the pull (`BL-154`), NOW LADDERED 76 → 90 ═════════════════════════════════
        //
        // ✅ HIS NUMBERS LANDED 2026-09-04 and they confirm the engine's shape: 600 reach, 0.5s cast,
        // 15s reuse, a 1-second stun tail, and a POWER ladder 2,100 → 3,500. The 3,000 the placeholder
        // carried sat mid-ladder; his own opening rung is 2,100 and the top is 3,500, which is still
        // below the 4,000 he calls his standard damage skill.
        //
        // 🔑 IT IS A DAMAGE SKILL, NOT A THREAT SKILL — his reversal of 2026-09-04: *"if it's a taunt
        // skill I want it to not be, and be a normal dmg skill with 3k power"*. `TauntPower > 0` is
        // the first test `BL-83` applies when sorting skills into the never-auto-cast bucket, so a
        // threat-shaped Grapple could not appear in a rotation at all. It builds threat only through
        // the damage it deals.
        //
        // ⚠ THE DRAG IS TIMED, NOT PACED: `PullSeconds` is the whole journey from any distance and the
        // speed is derived. Range buys reach and never buys lockdown — see SkillDef.Pulls.
        new(TankPull, "Grapple", BaseClass.Fighter, SkillEffect.PhysicalDamage | SkillEffect.Stun,
            MpCost: TankFourthBashMp[0], CastTicks: 5, CooldownTicks: 150, Range: 600, Power: 2100,
            DurationTicks: 10,                       // the STUN tail: his 1s, held back until arrival
            Pulls: true, PullSeconds: 1.2f,          // his 1.2s drag
            DebuffSchool: DebuffSchool.Physical, Category: SkillCategory.Debuff,
            BuffKey: "tank_pull_stun", Rank: 1,
            SpCost: 6_500_000,
            Description: "Hauls an enemy across the ground to your side, striking it on arrival and "
                       + "leaving it reeling.",
            Levels: T4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
                MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold, Power: 2100 + i * 200,
                Description: $"Power {2100 + i * 200:N0}. Drags the target to you over 1.2s, hits it "
                           + "and stuns it for 1s on arrival."))),

        // ═══ NUMBING SHOCK — the PHYSICAL silence (`BL-155`), Human + Demon ═══════════════════════
        //
        // 🔑 IT DOES NOT STOP A BASIC ATTACK, which is his boundary in as many words: *"physical skill
        // silence (only basic attack)"*. What it stops is every skill `SkillMath.IsPhysical` is true
        // for — the SAME test the cast-speed model uses, never a second classification.
        //
        // ✅ HIS ROW REPLACED EVERY PLACEHOLDER NUMBER: it is a MELEE bash now (range 40, not 400),
        // a 2-second cast, a 6-second reuse and 5 seconds of silence — not the 30s/8s a placeholder
        // guessed. Renamed from "Numbing Strike" to match his NAME column, and it lands at ×0.5
        // where the Elf's magical twin lands at ×0.7: the physical half is harder to stick and
        // cheaper to re-throw.
        new(TankSilencePhysical, "Numbing Shock", BaseClass.Fighter, SkillEffect.None,
            MpCost: TankFourthBashMp[0], CastTicks: 20, CooldownTicks: 60, Range: 40, Power: 0,
            DurationTicks: 50,
            SilencePhysical: true, DebuffLandMod: 0.5f,
            DebuffSchool: DebuffSchool.Physical, Category: SkillCategory.Debuff,
            BuffKey: "silence_physical", Rank: 1, SharesLadderKey: true,
            SpCost: 6_500_000,
            Description: "A blow to the nerve: the target's body will not perform a skill for 5s, "
                       + "though it can still swing.",
            Levels: T4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
                MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold))),

        // ═══ SILENCING SHOCK — the MAGICAL silence (`BL-155`), Elf ═══════════════════════════════
        //
        // The Elf tank's half, and landing it AND the strike above on one target is a FULL silence —
        // his *"both at once a full silence"* — which needs no third skill to express: two fields,
        // two debuffs, and the cast gate asks the two questions separately.
        //
        // ⚠ RENAMED FROM "Silencing Ward" to his NAME cell, and its id is the ONE correction this
        // pass made to `tank 4th.csv`: all eight rows carried `tank_shield_stun`. See the file header.
        // It reaches 150 (a step further than the Human's 40 — the Elf is the magic knight of the
        // three), casts in 1.5s and holds for 10 seconds at ×0.7.
        new(TankSilenceMagical, "Silencing Shock", BaseClass.Fighter, SkillEffect.None,
            MpCost: TankFourthBashMp[0], CastTicks: 15, CooldownTicks: 60, Range: 150, Power: 0,
            DurationTicks: 100,
            SilenceMagical: true, DebuffLandMod: 0.7f,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            BuffKey: "silence_magical", Rank: 1, SharesLadderKey: true,
            SpCost: 6_500_000,
            Description: "Smothers the words of a spell before they are spoken: the target's magical "
                       + "skills fail for 10s.",
            Levels: T4Rungs(8, 2, (i, sp, gold) => new SkillLevel(
                MpCost: TankFourthBashMp[i], SpCost: sp, GoldCost: gold))),

        // ═══ MAGIC WALL — Defensive Wall's magical half, with no price attached ═══════════════════
        //
        // *"increase m.def +4000"* … *"+6000"*, 30 seconds on a 10-minute reuse for 50 MP. It is the
        // one wall a tank can raise and keep MOVING under, which is what makes it a different skill
        // rather than a weaker rung of the other: Defensive Wall costs you ×0.45 → ×0.20 movement
        // and this costs you nothing.
        //
        // ⚠ ITS OWN BUFF KEY, so the two walls STACK. That is deliberate and it is his: he authored
        // two skills with two reuses and two costs, and a tank who spends both cooldowns at once has
        // earned +5,000 P.Def and +10,500 M.Def for thirty seconds — at the price of being unable to
        // walk out of anything for the whole of it.
        new(TankMagicWall, "Magic Wall", BaseClass.Fighter, SkillEffect.BuffMagicDef,
            MpCost: 50, CastTicks: 5, CooldownTicks: 6000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "magic_wall", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            SpCost: 6_500_000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, 4000, ModifierMode.Flat) },
            Description: "A ward against spellcraft for 30s: a great deal more magic defence, and no "
                       + "cost to your footing.",
            Levels: T4Rungs(8, 2, (i, sp, gold) =>
            {
                int[] mDef = { 4000, 4200, 4400, 4600, 4800, 5000, 5500, 6000 };
                return new SkillLevel(MpCost: 50, SpCost: sp, GoldCost: i == 0 ? 1_000 : gold,
                    Magnitudes: new EffectMagnitude[]
                        { new(SkillEffect.BuffMagicDef, mDef[i], ModifierMode.Flat) },
                    Description: $"+{mDef[i]:N0} M.Def for 30s.");
            })),

        // ═══ TAUNTING WALL — one rung at 80, and the tank's whole job in one cast ═════════════════
        //
        // *"Agro enemies around - locks them onto you for 3s then leaves you 11400 aggro ahead; and
        // then increasing your: p.def +5000; m.def +4500; buff cancel resist x1.8; ms x0.20"*.
        //
        // 🔑 BOTH HALVES ARE THE TOP OF THEIR OWN LADDERS, at level 80, ten levels early: 11,400 is
        // Mass Taunt's level-90 number and the four defensive terms are Defensive Wall's level-90
        // row. That is what a 10-minute reuse buys, and it is why it is a single rung — there is
        // nowhere for it to climb.
        //
        // 🔑 IT IS NOT RACE-SPLIT. Mass Taunt is the Human's alone; this is every tank's, so the Elf
        // and the Demon get their one AoE aggro tool here and nowhere else.
        //
        // ⚠ THE SELF-BUFF RIDES `SkillDef.SelfBuff`, and it has to: `TargetMode.EnemiesInRadius`
        // RETURNS from ExecuteSkill after its sweep, so the ordinary buff arm never sees this skill.
        // The payload below is an ordinary def that is never learned and never on a bar.
        //
        // ⚠ HIS AOE CELL READ 0 AND HIS RANGE CELL 800 — corrected to 800 in the AOE column (his own
        // Mass Taunt row is rng 0 / aoe 400, so the radius belongs in AOE). An `enemy/aoe` with a
        // radius of zero taunts nothing at all; the alternative reading is a dead skill.
        new(TankTauntingWall, "Tauting Wall", BaseClass.Fighter, SkillEffect.Taunt,
            MpCost: 200, CastTicks: 5, CooldownTicks: 6000, Range: 800, Power: 0,
            DurationTicks: 30, AreaRadius: 800f, TauntPower: 11400,
            Category: SkillCategory.Debuff, PhysicalCast: true,
            TargetMode: TargetMode.EnemiesInRadius,
            SelfBuff: TankTauntingWallGuard,
            SpCost: 150_000_000,
            Levels: new[]
            {
                new SkillLevel(MpCost: 200, SpCost: 150_000_000, GoldCost: 10_000_000,
                    AreaRadius: 800f, TauntPower: 11400),
            },
            Description: "Roars at everything within 800 — each of them turns on you for 3s and adds "
                       + "11,400 to your aggro — and then you plant yourself: +5,000 P.Def, +4,500 "
                       + "M.Def and high cancel resistance for 30s, at a fifth of your movement."),

        // The wall half of the above. A payload def: no learn row, no bar square, no price.
        // ⚠ ITS OWN KEY, not Defensive Wall's. A tank who has just spent a 10-minute cooldown must
        // not have it evicted by a 30-second Defensive Wall he casts a moment later, and rank rules
        // between two skills that are each other's equal would decide that by remaining time.
        new(TankTauntingWallGuard, "Tauting Wall", BaseClass.Fighter,
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffCancelResist
            | SkillEffect.BuffMoveSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "tauting_wall", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 0,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 5000, ModifierMode.Flat),
                new(SkillEffect.BuffMagicDef, 4500, ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMoveSpeed, -0.80f, ModifierMode.Percent),
            },
            Description: "+5,000 P.Def, +4,500 M.Def and high cancel resistance for 30s; your "
                       + "movement drops to ×0.20."),

        // ═══ PERFECT WHISP — one whisp that does what six do, 80 → 90 ════════════════════════════
        //
        // *"Call a whisp to help remove bad effects, heal its master (Power 900), restore mp of its
        // master (Power 100), break armor of enemy (Deacreases P.Def by 30% and MDef by 15%), break
        // weapon of enemy (Decreases P/M.Atk 15%), Decrease enemy Atack speed and cast speed by 23%"*.
        //
        // 🔑 SIX GEARS, AND ONLY TWO OF THEM LADDER. The heal climbs 900 → 1,000 and the mana 100 →
        // 200; the three debuffs and the cleanse are FLAT across all six rungs, which is his file and
        // is what keeps it a utility whisp rather than a better version of the three it replaces —
        // at level 90 the single Armor Breaking Whisp strips 40%/20% where this one strips 30%/15%.
        //
        // 🔑 IT COSTS A WHISP SLOT LIKE ANY OTHER, so the choice it really offers is breadth against
        // depth: one Perfect Whisp, or a Taunting and a Binding one. Whisp Mastery's third slot at
        // 83 is what makes that a choice rather than an ultimatum.
        //
        // ⚠ IT IS NOT RACE-SPLIT — his row carries no RACE, so all three tanks call it, and it is the
        // only whisp any of them shares.
        WhispSummon(TankWhispHelp, "Perfect Whisp", WhispClear, null,
            "Calls a spirit that does a little of everything — mends, refuels, and wears your enemy "
            + "down.",
            extra: new[] { WhispGreatHeal, WhispMana, WhispGreatArmorBreak,
                           WhispGreatWeaponBreak, WhispGreatGravity },
            levels: Enumerable.Range(0, 6).Select(i =>
            {
                // His price row: LEARNED at 80 (150kk SP + 10kk gold), then the ordinary levelling
                // ladder from 82 — which is F4's indices 6, 8, 10, 12, 14.
                var (sp, gold) = i == 0 ? F4New(80) : F4(3 + i, 2);
                return new SkillLevel(MpCost: TankFourthWhispMp[i + 2], SpCost: sp, GoldCost: gold);
            }).ToArray()),

        // ---- The Perfect Whisp's own five gears. Six rungs each, indexed by the SUMMON's rung.
        //      ⚠ These are `whisp_*` ids, so `IsWhispSkill` keeps them out of every player-facing
        //      list exactly as it does the other nine: never learned, never bought, never on a bar.

        // HEAL — Power 900 → 1,000, +20 a rung. No HP band on it, unlike the single healing whisp's
        // two gears: he authored ONE heal here, so it tops the master up whenever it is off reuse
        // and he is hurt (WhispSupportWanted's default arm).
        new(WhispGreatHeal, "Whisp Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 0, CastTicks: 10, CooldownTicks: 200, Range: 0, Power: 900,
            Category: SkillCategory.Heal,
            Description: "The whisp mends its master.",
            Levels: Enumerable.Range(0, 6).Select(i => new SkillLevel(Power: 900 + i * 20)).ToArray()),

        // MANA — Power 100 → 200, +20 a rung. The FIRST whisp skill to touch the MP bar; the support
        // arm of TryWhispAct learned `RestoreMp` in the same pass.
        new(WhispMana, "Whisp Mana", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 0, CastTicks: 10, CooldownTicks: 300, Range: 0, Power: 100,
            Category: SkillCategory.Heal,
            Description: "The whisp pours a little of itself back into its master.",
            Levels: Enumerable.Range(0, 6).Select(i => new SkillLevel(Power: 100 + i * 20)).ToArray()),

        // ARMOR BREAK — 30% / 15%, flat. Same family key as every other armor break in the game, so
        // a healer's real one always outranks it (*"Whisp debuffs do not stack with the player
        // version"*). ⚠ The sign convention is the healer's: P.Def rides DebuffDef POSITIVE, M.Def
        // rides BuffMagicDef NEGATIVE.
        new(WhispGreatArmorBreak, "Whisp Armor Break", BaseClass.Mage,
            SkillEffect.DebuffDef | SkillEffect.BuffMagicDef,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "armor_break", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, 0.30f),
                new(SkillEffect.BuffMagicDef, -0.15f),
            },
            Description: "The whisp frays an enemy's guard for 15s: −30% P.Def, −15% M.Def."),

        // WEAPON BREAK — 15% off both channels, flat. `DebuffAtk` covers P.Atk and M.Atk at once.
        new(WhispGreatWeaponBreak, "Whisp Weapon Break", BaseClass.Mage, SkillEffect.DebuffAtk,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "weapon_break", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffAtk, 0.15f) },
            Description: "The whisp blunts an enemy's weapon and their magic alike for 15s: −15%."),

        // GRAVITY — 23% off attack AND cast speed. `whisp_gravity` has existed since `BL-109` and was
        // never summoned by anything; this is the first call that carries a gravity gear, at his own
        // number rather than that def's 7%.
        new(WhispGreatGravity, "Whisp Gravity", BaseClass.Mage,
            SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            MpCost: 0, CastTicks: 10, CooldownTicks: 100, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "gravity", Rank: 1, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.23f), new(SkillEffect.DebuffCastSpeed, 0.23f),
            },
            Description: "The whisp weighs an enemy down for 15s: −23% attack and cast speed."),
    };
}
