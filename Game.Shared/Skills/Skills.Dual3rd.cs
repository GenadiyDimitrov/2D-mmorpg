using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE MELEE ROGUE'S 3rd CLASS, 40-74 — every row of
/// `docs/data/classes_skills_csv/dual 3rd.csv`. Built 2026-09-09 on his word: *"build/fix rogue 2nd,
/// archer and duals 3rd"*.
///
/// <para>🔑 <b>RACE DECIDES THE DAMAGE SKILL, and that is the whole shape of this file.</b> The three
/// melee disciplines — Nullblade (Human), Phantom (Elf), Venomweaver (Demon) — share four things
/// (Armor Mastery, Dual Mastery, Prowl/Vanish/Sprint/Evasion Boost/Lure) and then split completely:
/// <list type="bullet">
///   <item>Human keeps <b>Killing Stab</b> and adds <b>Heavy Stab</b> — one big blow, twice.</item>
///   <item>Elf keeps <b>Killing Stab</b> and adds <b>Swift Stab</b> — a fast blow that speeds him up.</item>
///   <item>Demon gets NEITHER, and takes <b>Venom Stab</b> + <b>Venom Burst</b> instead: half the
///         power per hit, banked as stacks and spent in one detonation.</item>
/// </list>
/// His RACE column says so cell by cell — Killing Stab is `Human;Elf`, Venom Stab and Venom Burst are
/// `Demon`, Swift Stab is `Elf`, Heavy Stab is `Human`. Phantom Jump is per-race THREE TIMES OVER,
/// with a different rider each (stun / charm / fear).</para>
///
/// <para>🔑 <b>THE FILE RUNS ON ONE LADDER</b> — 40/43/46/49/52/55/58/60/62/64/66/68/70/72/74, the
/// SAME fifteen levels and the SAME SP schedule the tank's file uses, so <see cref="BulwarkLevels"/>
/// and its SP ladder are reused, the latter with ONE number changed (see <see cref="RogueSp"/>). The
/// exceptions are all his:
/// Phantom Jump is three rungs at 52/60/74, Antidote six at 52/58/62/66/70/74, Lure three at
/// 52/62/74, and Prowl / Vanish / Sprint / Evasion Boost are single rungs at 40 / 60 / 46 / 60.</para>
///
/// <para>⚠ <b>WHAT IS AUTHORED HERE AND NOT BY HIM: the Lure rungs.</b> He asked for them by name
/// (*"on duals 3rd add the 'lure' skill rows"*) and gave the three levels and the three MP prices
/// (52/62/74 at 65/80/95 MP); the SP comes off the file's own ladder and the 200/400/600 reach is the
/// ladder the skill has carried since `BL-70`. Those rows were written INTO `dual 3rd.csv` in the same
/// commit — see the rule at the top of CLAUDE.md.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- THE MELEE ROGUE'S 3rd-CLASS IDS. His `SKILL_ID` column, verbatim, misspellings included:
    //      a skill id is a WIRE VALUE and a save value, so "correcting" `rouge`/`pahantom` after the
    //      fact would orphan every character who had learned one. Same rule as `wc_ork_*`.
    public const string DualWeaponMastery = "dual_weapon_mastery";
    public const string KillingStab       = "killing_stab";
    public const string VenomStab         = "venom_stab";
    public const string SwiftStab         = "swift_stab";
    public const string HeavyStab         = "heavy_stab";
    /// <summary>The self-buff Swift Stab leaves behind (+5 speed, +15% attack speed, 5s). A payload
    /// def in the <see cref="SkillDef.SelfBuff"/> shape — never learned, never on a bar.</summary>
    public const string SwiftStabRush     = "swift_stab_rush";
    public const string PhantomJumpHuman  = "rouge_huamn_pahantom_jump";
    public const string PhantomJumpElf    = "rouge_elf_pahantom_jump";
    /// <summary>🔴 THE ONE ID THAT IS NOT HIS. His Demon Phantom Jump block reads
    /// `rouge_elf_pahantom_jump` on all three rows — the same id as the Elf's, with a different TYPE
    /// (`physical` vs `magical`) and a different rider (fear+90% slow vs charm+75% slow). Two skills
    /// cannot share one id, so the Demon's got its own and the CSV rows were corrected to match, in
    /// the same commit. Flagged to him rather than left: it reads as a copy-paste of the Elf block.</summary>
    public const string PhantomJumpDemon  = "rouge_demon_pahantom_jump";
    /// <summary>The SELF-only cure the Elf gets in all three of his 3rd-tier fighter files
    /// (`dual 3rd`, `archer 3rd`, `tank 3rd`). Not the healer's <see cref="Antidote"/>, which is
    /// targeted and on a different ladder — see the note on the def.</summary>
    public const string ElfAntidote       = "elf_antidote";
    // ---- `BL-188`, THE BLOW LADDER (2026-09-09). One buff family, three race variants — the same
    //      shape Phantom Jump already uses, and for the same reason: the PAYLOADS differ, and two
    //      skills cannot share one id. Vital Points is shared by all three.
    public const string LethalFocus       = "lethal_focus";       // Human — half rate, half crit damage
    public const string LethalPrecision   = "lethal_precision";   // Elf   — all crit damage
    public const string LethalFrenzy      = "lethal_frenzy";      // Demon — all blow rate
    public const string VitalPoints       = "vital_points";       // 52/64/74 passive, +10/15/20% rate

    // ---- HIS LADDERS ----------------------------------------------------------------------------

    /// <summary>THE SP LADDER BOTH ROGUE FILES RUN ON. It is the tank's <see cref="BulwarkSp"/> with
    /// ONE number different — rung 6 (level 55) is <b>80,000</b> here and 81,000 there.
    ///
    /// <para>⚠ That one digit is the reason this array exists instead of a reuse. It looked like a
    /// typo and is not: `dual 3rd.csv` AND `archer 3rd.csv` both say 80 at 55, on every fifteen-rung
    /// family in both files — thirteen skills agreeing is authoring, not a slip. `--check` reported it
    /// on 24 rows the first time these files were walked, which is exactly what it is for.</para></summary>
    internal static readonly int[] RogueSp =
    {
        28_000, 35_000, 40_000, 50_000, 74_000, 80_000, 88_000, 120_000,
        170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
    };

    /// <summary>The MP ladder every one of the five stab families shares — his own column, and the
    /// same fifteen numbers in all five blocks. (He filled these in on 2026-09-09; the file shipped
    /// the day before with an MP of 0 on every damage row, which he called *"my slip"*.)</summary>
    private static readonly int[] StabMp =
        { 36, 40, 43, 45, 47, 50, 55, 56, 58, 62, 65, 68, 70, 75, 78 };

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  🔑 EVERY LADDER BELOW WAS HALVED ON 2026-09-11 (`BL-203`), AND THE HALF IS THE POINT.
    //
    //  His measurement: *"now ~11k dmg on a 90 mob with 19k hp .. And 78k mob hit for 8k. A bit too
    //  much. Let atleast this dmg to be a double dmg"* — so the number he was seeing does not go
    //  away, it moves BEHIND A ROLL. Every stab is flagged `CanDouble` in the same increment, so a
    //  melee rogue carrying Overpower still lands the old figure; he just no longer lands it every
    //  time.
    //
    //  🔴 BUT HALVING THE POWER DOES NOT HALVE THE DAMAGE, and the difference is not small. Power is
    //  a TERM INSIDE the ratio — `K·(atk·lvlMod + power)/def` — so the attacker's own P.Atk rides
    //  through untouched. Measured at 90 in mythic gear (`BalanceMatrix --stab`), where that term is
    //  worth ~2,470 of the numerator: a Killing Stab went from ~6,970 to 3,978 (×0.57, not ×0.5), and
    //  a DOUBLED one lands 7,956 — about 14% ABOVE what the skill used to do flat. So the trade is
    //  better than "half, unless you double": the ceiling actually rose slightly and only the floor
    //  came down. ⚠ Do not restate this as "2 × the new IS the old" anywhere; that is the mistake
    //  this paragraph exists to stop.
    //
    //  ⚠ THE RATIOS BETWEEN THE FOUR FAMILIES ARE UNCHANGED except where he changed one by name
    //  (Venom Stab, below). Heavy stays ×0.75 per hit of Killing, Venom Burst ×0.20 per stack, so
    //  ten stacks is still twice a Killing Stab. Halving the whole file keeps every one of those.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Killing Stab / Swift Stab — the same power ladder, 625 → 3200. It is his ladder
    /// HALVED (`BL-203`): it ran 1250 → 6400 and every rung is exactly half of what he authored,
    /// keeping the straight arithmetic climb and the widening stride at 52.</summary>
    private static readonly int[] StabPower =
    {
        625, 750, 875, 1000, 1200, 1400, 1600, 1800,
        2000, 2200, 2400, 2600, 2800, 3000, 3200,
    };

    /// <summary>Venom Stab — <b>the same ladder as Killing Stab</b>, and that is his ruling of
    /// 2026-09-11: *"make venomWeaver - venom stab to have the same power as killing strike (the new
    /// /2 dmg)"*.
    ///
    /// <para>🔑 SO THE DEMON'S TRADE MOVED. Venom Stab used to be deliberately HALF of a Killing
    /// Stab, the Venomweaver being paid the difference in stacks. After the halving that discount
    /// would have stacked with it — half of a half — and the one discipline with no Killing Stab at
    /// all would have been the one hit twice. It now pays for its stacks with its ROTATION (a burst
    /// on a 10s reuse) rather than with per-blow power.</para>
    ///
    /// <para>⚠ In practice this ladder barely moved: the old venom numbers were 675 → 3200, so only
    /// the first rung changes. It is the KILLING/SWIFT/HEAVY families that halved.</para></summary>
    private static readonly int[] VenomStabPower = StabPower;

    /// <summary>Heavy Stab — his *"power 950 twice"*: TWO resolutions (<see cref="SkillDef.HitCount"/>)
    /// of this number, on a 3-second cast rather than Killing Stab's one. HALVED (`BL-203`) from
    /// 950 → 4800; the odd rung rounds up (1125 → 563), which keeps the ×0.75-of-Killing ratio.</summary>
    private static readonly int[] HeavyStabPower =
    {
        475, 563, 650, 750, 900, 1050, 1200, 1350,
        1500, 1650, 1800, 1950, 2100, 2250, 2400,
    };

    /// <summary>Venom Burst — damage PER CONSUMED STACK, ×125 at 40 climbing to ×640 at 74.
    /// HALVED (`BL-203`) from his 250 → 1280, so ten stacks is still exactly twice a Killing Stab.</summary>
    private static readonly int[] VenomBurstPerStack =
    {
        125, 150, 175, 200, 240, 280, 320, 360,
        400, 440, 480, 520, 560, 600, 640,
    };

    /// <summary>His venom TIER per rung — 3,3,4,4,5,5,6,6,7,7,8,8,9,9,10. It is the rank an Antidote
    /// has to reach to strip it, and the Elf's own Antidote ladder (4 → 9) is deliberately one step
    /// behind at every level: a Venomweaver's top-rung venom cannot be cured by anything in this file.
    /// Neither flat nor level+1, which is why <see cref="SkillLevel.Rank"/> had to exist.</summary>
    private static readonly int[] VenomTier =
        { 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 };

    /// <summary>How many stacks ONE Venom Stab lays — 1,1,2,2,2,2,2,2,3,3,3,3,3,3,3. The cap stays at
    /// ten throughout, so what a rung buys is how fast the burst fills.</summary>
    private static readonly int[] VenomStacksPerCast =
        { 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3 };

    // 🔑 `ThirdTierBlowFloor` (1%) LIVED HERE AND IS GONE (`BL-193`, 2026-09-10). His stab rows used
    //    to read "power N - only when skill does critical - otherwise N/100", but the floor those
    //    second numbers described no longer exists: a blow that fails now strikes as an ordinary
    //    BASIC ATTACK. The CSV cells were rewritten to say so in the same increment.

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  ARMOUR — rungs 6-20 of the ROGUE's own mastery, APPENDED. Not a new skill: see the header of
    //  Skills.FighterKits3rd.cs for why a second armour mastery either double-grants the light
    //  branch's crit-rate resistance or deletes the weights it does not re-state.
    //
    //  ⚠ THESE REPLACE THE DERIVED ONES (`RogueArmorMasteryThirdRungs`, half the tank's P.Def ladder,
    //    2026-09-06). They were provisional by construction — the note on them said so — and his file
    //    is a good deal more generous: 28 → 70 flat P.Def against the derived 32 → 86, but with the
    //    evasion, speed, crit-rate resistance and BOTH regen columns growing as well, none of which
    //    the derived rungs moved at all.
    //  ⚠ LIGHT ONLY, on his WEIGHT column — see the note on `RogueArmor` in Skills.Masteries.cs.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>His P.Def column: +28 at 40, +3 a rung, +70 at 74.</summary>
    private static int RogueArmorPDef(int i) => 28 + i * 3;

    /// <summary>Evasion: 12,12,13,13,13,13,14,… — it moves twice in fifteen rungs and then stops.
    /// ⚠ The ARCHER's file holds it at 12 throughout, which is the ONLY difference between the two
    /// armour ladders and the reason they are two skills rather than one. Do not unify them.</summary>
    private static readonly int[] RogueArmorEva =
        { 12, 12, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14 };

    /// <summary>Crit-rate resistance: 25% to level 58, then 35% from 60. Both files, same step.</summary>
    private static float RogueArmorCritRes(int i) => i <= 6 ? 0.25f : 0.35f;

    /// <summary>Move speed: +7 on the first rung only, +11 from 43. Flat, never a percent
    /// (playtest-20: *"Also speed is +7 flat not x1.07"*).</summary>
    private static float RogueArmorSpeed(int i) => i == 0 ? 7f : 11f;

    /// <summary>His `mpReg +1.8` … `+2.5`, carried as a MULTIPLIER (value − 1) because that is what
    /// the same cell already meant at the 2nd class: `rogue 2nd.csv`'s top rung reads `mpReg +1.8`
    /// against a stored <c>MpRegenPct: 0.8f</c>, and this ladder starts at the very same 1.8. His MP
    /// ruling carves armour masteries out of the flat-regen rule explicitly (*"except armor masteries
    /// the 20% increase"*), so one continuous multiplier column runs 20 → 74.</summary>
    private static readonly float[] RogueArmorMpReg =
        { 0.8f, 0.9f, 1.0f, 1.1f, 1.2f, 1.2f, 1.2f, 1.2f, 1.3f, 1.3f, 1.3f, 1.4f, 1.4f, 1.4f, 1.5f };

    /// <summary>His `hpReg +2.5` … `+6.0`, FLAT HP/s (`BL-92`) and continuous with the 2nd class's
    /// top rung, which is also 2.5.</summary>
    private static readonly float[] RogueArmorHpReg =
        { 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 4.5f, 4.5f, 4.5f, 5.0f, 5.0f, 5.0f, 5.5f, 5.5f, 5.5f, 6.0f };

    internal static SkillLevel[] RogueArmorMasteryThirdRungs() =>
        BulwarkRungs(i => new SkillLevel(SpCost: RogueSp[i],
            Description: $"With light armor: +{RogueArmorPDef(i)} P.Def, +{RogueArmorEva[i]} evasion, "
                       + $"+{RogueArmorSpeed(i):0} speed, {RogueArmorCritRes(i) * 100:0}% less often "
                       + $"critted, ×{1f + RogueArmorMpReg[i]:0.0} MP regen, +{RogueArmorHpReg[i]:0.0} HP/s."));

    internal static ArmorMasteryProfile[] RogueArmorMasteryThirdProfiles() =>
        Enumerable.Range(0, BulwarkLevels.Length).Select(i => new ArmorMasteryProfile(
            Robe: default, None: default, Heavy: default,
            Light: new StatMods(
                PDef: RogueArmorPDef(i), Evasion: RogueArmorEva[i],
                CritRateResist: RogueArmorCritRes(i), MoveSpeed: RogueArmorSpeed(i),
                MpRegenPct: RogueArmorMpReg[i], HpRegen: RogueArmorHpReg[i]))).ToArray();

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILLS
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Dual3rdSkills()
    {
        var list = new List<SkillDef>();

        // ═══ DUAL MASTERY — the melee branch's weapon passive, replacing the rogue's ═════════════
        //
        // 🔑 IT IS A SEPARATE SKILL, not appended rungs, and that is HIS structure: the row carries
        //    its own `SKILL_ID` and `REPLACES [rogue_weapon_mastery]`. The armour line beside it does
        //    the opposite (same id, appended rungs) — the difference is that an armour mastery's
        //    profile has to re-state every weight it does not want to lose, while a weapon mastery
        //    that replaces its predecessor simply takes over the one weapon it cares about. Replacing
        //    is also what DROPS THE BOW: a melee rogue who learns this loses the bow half of Rogue
        //    Weapon Mastery, which is the point of choosing the dagger branch.
        //
        // ⚠ AND IT CARRIES A PROC, which no weapon mastery before it did. His row: *"With 3% chance to
        //   decrease skill mp consumption with 60% and increase crit.dmg with 10% for 5 sec"*, and the
        //   CD/DURATION cells (8 / 5) are the proc's internal cooldown and the buff's life. Gated to
        //   duals through RequiredWeapon, which the proc machinery honours.
        int[] dualAtk    = { 20, 23, 26, 30, 35, 40, 45, 50, 53, 56, 60, 65, 70, 75, 80 };
        int[] dualCritDmg = { 232, 259, 282, 322, 443, 481, 526, 556, 587, 738, 769, 803, 836, 960, 1015 };
        float[] dualCritRate = { .30f, .30f, .30f, .30f, .40f, .40f, .40f, .40f, .40f, .40f, .40f, .40f, .50f, .50f, .50f };
        float[] dualAtkSpd   = { .05f, .05f, .07f, .07f, .07f, .07f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f };

        list.Add(new SkillDef(DualWeaponMastery, "Dual Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Replaces: new[] { RogueWeaponMastery },
            RequiredWeapon: WeaponType.Dual,
            ProcChance: 0.03f, ProcCooldownTicks: 80,
            ProcSelfRungs: Enumerable.Repeat(DualMasteryRush, BulwarkLevels.Length).ToArray(),
            Description: "Passive. Two blades in your hands hit harder, more often and far crueller "
                       + "when they bite. No effect with anything else.",
            Levels: BulwarkRungs(i => new SkillLevel(SpCost: RogueSp[i],
                Description: $"Duals: +{dualAtk[i]} P.Atk, ×1.085 P.Atk, +{dualCritDmg[i]} crit damage, "
                           + $"+3 accuracy, ×{1f + dualCritRate[i]:0.0} crit rate, "
                           + $"×{1f + dualAtkSpd[i]:0.00} attack speed."))
                .Concat(DualMasteryFourthRungs()).ToArray(),
            WeaponMasteryLevels: Enumerable.Range(0, BulwarkLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Dual: new PassiveEffect(
                        PhysAtk: dualAtk[i], PhysAtkPct: 0.085f,
                        CritDamageFlat: dualCritDmg[i], Accuracy: 3,
                        CritRate: dualCritRate[i], AtkSpeedPct: dualAtkSpd[i]),
                    RequiredWeapon: WeaponType.Dual))
                .Concat(DualMasteryFourthProfiles()).ToArray()));

        // The proc's payload. FLAT across all fifteen rungs — his numbers do not ladder (3% / 60% /
        // 10% on every row), so one def is repeated rather than fifteen written.
        // ⚠ `PhysMpCostPct` is the MP discount and `BuffCritDamage` the crit half; both ride on the
        //   BUFF, so `EffectiveMpCost` picks the discount up for free on the next stab.
        list.Add(new SkillDef(DualMasteryRush, "Dual Mastery", BaseClass.Fighter,
            SkillEffect.BuffCritDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 50, BuffKey: "dual_mastery_rush", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly,
            PhysMpCostPct: 0.60f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCritDamage, 0.10f) },
            Description: "Your blades find the rhythm: for 5s your physical skills cost 60% less MP "
                       + "and your critical hits land 10% harder."));

        // ═══ KILLING STAB — Human and Elf ════════════════════════════════════════════════════════
        //
        // The dagger branch's main hand. A BLOW: full power only on a crit, a 1% floor otherwise —
        // ten times harsher than the 2nd class's Piercing Stab, which is his authored difference.
        //
        // ⚠ `Replaces: [precise_shot]` IS HIS CELL AND IS BUILT AS WRITTEN, and it is almost certainly
        //   a copy-paste from the archer file, where Twin Arrows replacing the bow skill is exactly
        //   right. On a dagger discipline it retires a BOW skill the character can no longer use and
        //   leaves Piercing Stab — the thing this actually continues — in the list forever. Harmless
        //   (a melee rogue was never going to cast Precise Shot), reported rather than "fixed": the
        //   cost of guessing wrong is deleting a skill he wanted kept. Same cell on all five families.
        list.Add(StabSkill(KillingStab, "Killing Stab", StabPower, castTicks: 10,
            "Drives both blades home. Full power only when it crits.",
            i => $"Blow power {StabPower[i]:N0} on a critical; a normal attack otherwise.",
            fourth: StabFourthRungs(StabPower4)));

        // ═══ SWIFT STAB — the Elf's ══════════════════════════════════════════════════════════════
        // Killing Stab's power on HALF the cast time, and it leaves a 5-second rush behind it.
        //
        // 🔑 REUSE 5s, NOT 3s (`BL-203`, owner 2026-09-11: *"swift strike reuse to 5s ... to balance
        //    the dmg~reuse for races"*). The three races carry the SAME power ladder now, so the only
        //    thing left to price them apart is time: the Elf pays for a half-length cast with a
        //    longer wait, the Human for two resolutions with a longer one still (Heavy Stab, 7.5s),
        //    and the Demon's Venom Stab keeps the 3s because its damage is banked, not dealt.
        list.Add(StabSkill(SwiftStab, "Swift Stab", StabPower, castTicks: 5,
            "A blur of a blow that carries you forward with it.",
            i => $"Blow power {StabPower[i]:N0} on a critical; a normal attack otherwise. "
               + "Leaves +5 speed and +15% attack speed for 5s.",
            selfBuff: SwiftStabRush, fourth: StabFourthRungs(StabPower4), cooldownTicks: 50));

        list.Add(new SkillDef(SwiftStabRush, "Swift Stab", BaseClass.Fighter,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffAtkSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 50, BuffKey: "swift_stab_rush", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffMoveSpeed, 5, ModifierMode.Flat),
                new(SkillEffect.BuffAtkSpeed, 0.15f),
            },
            Description: "+5 speed and +15% attack speed for 5s."));

        // ═══ HEAVY STAB — the Human's ════════════════════════════════════════════════════════════
        // *"power 950 twice"* — TWO independent resolutions (HitCount), each rolling its own crit,
        // on a 3-second cast. Against a blow floor of 1% that is a genuine gamble: two chances to
        // land the big number, and two chances to land almost nothing.
        // 🔑 REUSE 7.5s, NOT 3s (`BL-203`) — the longest of the three, because it is the only family
        //    that resolves TWICE: at ×0.75 power per hit it lands 1.5 Killing Stabs a cast, and on a
        //    3-second reuse that was simply more damage per minute than either sibling. See Swift
        //    Stab above for the shape of the three-way trade.
        list.Add(StabSkill(HeavyStab, "Heavy Stab", HeavyStabPower, castTicks: 30,
            "Two heavy blows, wound up and delivered. Each bites on its own.",
            i => $"Strikes 2 times; blow power {HeavyStabPower[i]:N0} each on a critical, "
               + "a normal attack otherwise.",
            hitCount: 2, fourth: StabFourthRungs(HeavyStabPower4), cooldownTicks: 75));

        // ═══ VENOM STAB — the Demon's ════════════════════════════════════════════════════════════
        //
        // 🔑 THE SAME BLOW AT HALF POWER, PAID FOR IN STACKS. It carries the venom DoT flags as well
        //    as its damage, so it lands on a contest of its own after the blow resolves — and its
        //    STACK KEY is `venom_venom`, the one Venom Burst has consumed since the primitives were
        //    written. That is the documented way two skills pool a counter, and it is what makes the
        //    Demon's two rows one rotation instead of two skills.
        // ⚠ THE TIER LADDERS (3 → 10) and so does the stacks-per-cast (1 → 3). Both needed a new
        //   per-rung slot; see SkillLevel.Rank and SkillLevel.StacksPerCast.
        list.Add(new SkillDef(VenomStab, "Venom Stab", BaseClass.Fighter,
            // ⚠ The DEBUFF FLAGS are gone with the magnitudes: `ApplyBuff` ORs in whatever the venom
            //   family's rider needs (`DotTiers.Rider`), so a flag stated here would only be a lie the
            //   day the family's rider changes.
            SkillEffect.PhysicalDamage | SkillEffect.Venom,
            MpCost: StabMp[0], CastTicks: 10, CooldownTicks: 30, Range: 40, Power: VenomStabPower[0],
            DurationTicks: 300, BuffKey: "venom", Rank: 3, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_venom", MaxStacks: 10, StacksPerCast: 1,
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            BlowOnCrit: true,
            // `BL-188` - the unauthored x2.0 on the crit rate is gone; a blow rolls Entity.BlowRate now.
            // `BL-203` — [Double], same as the other three families. See StabSkill.
            CanDouble: true,
            RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            // ⚠ NO RIDER MAGNITUDES: a DoT's side effect belongs to the (kind, tier) TABLE now
            //   (`DotTiers.Rider`), not to the skill that delivered it — his 2026-09-10 ruling,
            //   *"remove the dot side effect from the skills"*. Authoring one here would be
            //   APPLIED IN ADDITION and quietly double the real one.
            Description: "A poisoned blade — less damage than a killing blow, but it banks venom "
                       + "for Venom Burst to spend.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: VenomStabPower[i], MpCost: StabMp[i], SpCost: RogueSp[i],
                Rank: VenomTier[i], StacksPerCast: VenomStacksPerCast[i],
                Description: $"Blow power {VenomStabPower[i]:N0} on a critical; "
                           + "a normal attack otherwise. "
                           + $"Banks {VenomStacksPerCast[i]} venom stack(s) whenever the strike connects (max 10), "
                           + $"and lands a tier-{VenomTier[i]} venom on a contest."))
                .Concat(VenomStabFourthRungs()).ToArray()));

        // ═══ VENOM BURST — the Demon's detonator ═════════════════════════════════════════════════
        //
        // ⚠ THIS IS THE EXISTING `venom_burst` ID, re-authored. It was one of the Venomweaver
        //   primitives (power 12, one level) written long before any CSV and purged from every learn
        //   table on 2026-08-10, so nobody holds it and nothing is being taken away. His file gives it
        //   fifteen rungs and a real number.
        //
        // 🔑 IT IS ALSO ITS OWN STARTER. *"If no stacks present apply 1 venom stacks"* — so it carries
        //    the venom flags too, and the engine skips the DoT arm only when the damage arm actually
        //    spent a counter (see `spentStacks` in ExecuteSkill). A Demon with an empty target opens
        //    with this and it behaves as a weak Venom Stab; with ten stacks banked it is ×10.
        list.Add(new SkillDef(VenomBurst, "Venom Burst", BaseClass.Fighter,
            // ⚠ The DEBUFF FLAGS are gone with the magnitudes: `ApplyBuff` ORs in whatever the venom
            //   family's rider needs (`DotTiers.Rider`), so a flag stated here would only be a lie the
            //   day the family's rider changes.
            SkillEffect.PhysicalDamage | SkillEffect.Venom,
            MpCost: StabMp[0], CastTicks: 10, CooldownTicks: 100, Range: 40, Power: VenomBurstPerStack[0],
            DurationTicks: 300, BuffKey: "venom", Rank: 3, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_venom", ConsumeStackKey: "venom_venom", MaxStacks: 10,
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            // ═══ `BL-207` — THE BURST IS A STAB. ════════════════════════════════════════════════
            //
            // 🔑 HIS MODEL, AND IT MATCHES THE ARITHMETIC EXACTLY (2026-09-11): *"venom burst is a
            //    single stab skill that it's effective power depend on stacks count. It's not like
            //    barrage -> 10 stabs x1.5k power; it's one stab x15k power (so if it lands with 10
            //    stacks it's like a killing stab with a double)"*. At 90 the per-stack power is 1,500
            //    and a Killing Stab is 7,500, so ten stacks IS two Killing Stabs, to the digit.
            //
            // 🔑 SO IT ROLLS THE BLOW GATE AND IT CAN DOUBLE, like every other stab. That is the
            //    whole of his race-parity design — each race reaches ~x5 a normal stab per 10s:
            //      · Elf    — 2 Swift + 3 Killing, short cast, short reuse
            //      · Human  — 3 Killing + ~1.5 Heavy, one slow strike at x1.5
            //      · Demon  — ~3 Venom Stabs to bank 9, then ONE burst worth x2
            //    A burst that could not double was the one hole in it: the other two races' payoff
            //    strikes can, so the Demon's had to.
            // ⚠ THIS IS A DAMAGE CHANGE IN BOTH DIRECTIONS. It used to land ALWAYS and flat (no crit
            //   values at all, `CanCrit` unset); it now lands on the blow rate and is resolved WITH
            //   the crit-damage values like its siblings. Bigger when it lands, nothing when it does
            //   not — which is exactly the trade the other two races already make.
            BlowOnCrit: true,
            CanDouble: true,
            // 🔴 `FixedLandChance: 0.80f` LIVED HERE FOR ONE VERSION AND IS GONE (`BL-204`, superseded
            //    by `BL-207` the same day). It made the venom RIDER land on a flat 80% because a lost
            //    rider printed `Fail` over a cast that had already spent the pool. He replaced the
            //    whole idea with a better one: *"can we make venom burst to be with normal land rate
            //    (30% like other stabs) and on fail not to take all stacks but to restore 3"*. The
            //    failure that costs stacks is the BLOW now, it is survivable, and the rider goes back
            //    to the ordinary contest every other venom runs on. The cosmetic `Fail` is fixed at
            //    its source instead — `ExecuteSkill` no longer rolls a rider it has already decided
            //    to skip. ⚠ Do not re-add the field: the SkillDef property went with it.
            RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            // ⚠ NO RIDER MAGNITUDES: a DoT's side effect belongs to the (kind, tier) TABLE now
            //   (`DotTiers.Rider`), not to the skill that delivered it — his 2026-09-10 ruling,
            //   *"remove the dot side effect from the skills"*. Authoring one here would be
            //   APPLIED IN ADDITION and quietly double the real one.
            Description: "Detonates every venom stack on the target for damage per stack, taking the venom "
                       + "itself with it — and if there are none, lays the first one instead.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: VenomBurstPerStack[i], MpCost: StabMp[i], SpCost: RogueSp[i],
                Rank: VenomTier[i], StacksPerCast: VenomStacksPerCast[i],
                Description: $"Power {VenomBurstPerStack[i]:N0} per consumed venom stack (up to ×10). "
                           + $"With no stacks on the target, lays {VenomStacksPerCast[i]} tier-{VenomTier[i]} instead."))
                .Concat(VenomBurstFourthRungs()).ToArray()));

        // ═══ PHANTOM JUMP ×3 — a gap-closer with a different cruelty per race ════════════════════
        //
        // 🔑 THREE RUNGS AT 52 / 60 / 74, and the ladder is REACH (450 / 600 / 750) exactly as Lure's
        //    is: how far away you can start the jump IS the skill. MP 90/120/150, SP off the file's
        //    own ladder at those three levels.
        // 🔑 EVERY ONE IS `Blink` + a rider that lands on a contest. The blink is unconditional (you
        //    always arrive); the rider can be resisted, which is the same split Charm runs on.
        // ⚠ THE ELF'S IS MAGICAL — his TYPE cell says `magical debuff`, so it is saved by SPT, the
        //   same ruling that made the tank's Charm magical (`BL-133`). The other two are physical.
        list.Add(PhantomJump(PhantomJumpHuman, SkillEffect.Stun, DebuffSchool.Physical, charms: false,
            slow: 0f, "renders it unconscious",
            "Closes the distance in a blink and puts the target out cold for 3s."));
        list.Add(PhantomJump(PhantomJumpElf, SkillEffect.Slow, DebuffSchool.Magical, charms: true,
            slow: 0.75f, "charms it and cuts its movement by 75%",
            "Closes the distance in a blink; the target follows you helplessly, at a quarter pace."));
        list.Add(PhantomJump(PhantomJumpDemon, SkillEffect.Fear | SkillEffect.Slow, DebuffSchool.Physical,
            charms: false, slow: 0.90f, "terrifies it and cuts its movement by 90%",
            "Closes the distance in a blink and leaves the target fleeing, barely able to move."));

        // ═══ ANTIDOTE (Elf) — the SELF cure ══════════════════════════════════════════════════════
        //
        // ⚠ NOT the healer's `antidote`, and deliberately a separate id: his is `elf_antidote`, it is
        //   `self/single` in every one of the three fighter files that carry it, and its ceiling
        //   ladder (rank 4 → 9 at 52/58/62/66/70/74) is its own. Registering the healer's skill here
        //   would have handed a rogue a targeted cure on the healer's ladder.
        //
        // 🔑 IT IS ONE SKILL SHARED BY THREE FILES — `dual 3rd.csv`, `archer 3rd.csv` and
        //    `tank 3rd.csv` all author the identical six rows for their Elf. That last one is why
        //    `--check` has been reporting a 🔴 NOT REGISTERED Antidote against the tank since the
        //    Bulwark was built: the rows existed, the skill they name did not.
        list.Add(new SkillDef(ElfAntidote, "Antidote", BaseClass.Fighter, SkillEffect.Cleanse,
            MpCost: 42, CastTicks: 10, CooldownTicks: 100, Range: 0, Power: 0,
            // ⚠ AN EXPLICIT BuffKey, even though a cure lands no buff at all: the startup ladder guard
            // keys on the display NAME when none is given, and "Antidote" is also the healer's skill.
            // Two multi-rung ladders on one key make each other's rungs compete (`BL-85`).
            BuffKey: "elf_antidote",
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly,
            DispelMask: SkillEffect.Poison | SkillEffect.Venom | SkillEffect.Bleed,
            DispelMaxLevel: 4, SpCost: RogueSp[4],
            Description: "Purges poison, venom and bleeding from your own blood.",
            Levels: new SkillLevel[]
            {
                new(MpCost: 42, SpCost:  74_000, DispelMaxLevel: 4, Description: "Cures poison, venom and bleed of rank 4 or lower from yourself."),
                new(MpCost: 50, SpCost:  88_000, DispelMaxLevel: 5, Description: "Cures poison, venom and bleed of rank 5 or lower from yourself."),
                new(MpCost: 53, SpCost: 170_000, DispelMaxLevel: 6, Description: "Cures poison, venom and bleed of rank 6 or lower from yourself."),
                new(MpCost: 57, SpCost: 280_000, DispelMaxLevel: 7, Description: "Cures poison, venom and bleed of rank 7 or lower from yourself."),
                new(MpCost: 60, SpCost: 390_000, DispelMaxLevel: 8, Description: "Cures poison, venom and bleed of rank 8 or lower from yourself."),
                new(MpCost: 64, SpCost: 880_000, DispelMaxLevel: 9, Description: "Cures poison, venom and bleed of rank 9 or lower from yourself."),
            }));

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  `BL-188` — THE BLOW LADDER (owner ruling 2026-09-09)
        // ═══════════════════════════════════════════════════════════════════════════════════════
        //
        // A blow's landing rate stopped being the crit chain that day and became its own stat:
        // `0.30 × buffs × passives × BlowAgiMod(AGI)`, capped at 80%. These are the 3rd tier's two
        // contributions to that product — a race-split BUFF at 40/60/70 and a PASSIVE at 52/64/74.
        //
        // 🔑 THE RACE SPLIT IS THE BALANCE, and it is his: *"the race based buffs balance the blow
        //    rate and lack of dex and atk"*. Each rung carries a budget (10 / 15 / 20%) and the race
        //    decides how it is SPENT — the Elf, who already leads on AGI (36 vs 30 vs 28) and so on
        //    `BlowAgiMod`, spends all of it on crit DAMAGE; the Demon, who trails on AGI, spends all
        //    of it on rate; the Human splits it. The three end within four points of each other with
        //    the whole ladder up, which is the point of doing it this way rather than with one buff.
        //
        //        rung (level)        Human            Elf              Demon
        //        1  (40)     +5% rate, +5% dmg    +10% crit dmg    +10% rate
        //        2  (60)   +7.5% rate, +7.5% dmg  +15% crit dmg    +15% rate
        //        3  (70)    +10% rate, +10% dmg   +20% crit dmg    +20% rate
        //
        // ⚠ The BUFF flag on all three is `BuffCritRate` and it carries NO magnitude. A buff must
        //   declare something in `SkillEffect.AnyBuff` to land at all and the enum has been full
        //   since `1L << 62`, so the rate rides in the `BlowRatePct` FIELD and the crit-damage half
        //   in a real `BuffCritDamage` magnitude. SkillText reads the field directly, so the card
        //   never advertises the carrier.
        float[] focusBudget = { 0.10f, 0.15f, 0.20f };
        int[]   focusMp     = { 100, 125, 150 };
        // "(sp for lvl)" — his own instruction: the price is whatever the file's ladder charges at
        // that level. 40 / 60 / 70 are rungs 1, 8 and 13 of `RogueSp`.
        int[]   focusSp     = { RogueSp[0], RogueSp[7], RogueSp[12] };

        // 🔴🔑 `BL-214`, 2026-09-12 — `BuffCritDamage` WAS MISSING FROM THIS MASK AND THE ELF'S
        //    ENTIRE BUFF DID NOTHING. The comment four lines above already said the crit-damage half
        //    rides "in a real `BuffCritDamage` magnitude" — it does, and the def never declared the
        //    flag, so `RecomputeDerived`'s `buff.Has(BuffCritDamage)` gate threw all of it away. Lethal
        //    Precision is crit damage and nothing else, so the ELF's race buff was worth exactly zero
        //    from `BL-188` (0.121.0) until now, and the HUMAN's was worth half of what it claimed. The
        //    Demon's, being pure rate on the `BlowRatePct` FIELD, was the only one that ever worked —
        //    which is why `--blowrate` showed the elf's "+ race buff" column not moving and nobody read
        //    it as a defect.
        // ⚠ Adding the flag is safe for a rung that authors no magnitude: `buff.Percent(flag)` returns
        //   0 and the fold is a ×1. That is the same pattern the healer's Marks use deliberately.
        SkillDef Focus(string id, string name, Func<int, float> rate, Func<int, float> critDmg,
                       string blurb, Func<int, string> rung) =>
            new(id, name, BaseClass.Fighter, SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage,
                MpCost: focusMp[0], CastTicks: 0, CooldownTicks: 900, Range: 0, Power: 0,
                DurationTicks: 3000, BuffKey: id, Rank: 1,
                Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
                RequiredWeapon: WeaponType.Dual, SpCost: focusSp[0],
                BlowRatePct: rate(0), Description: blurb,
                Magnitudes: critDmg(0) > 0f
                    ? new EffectMagnitude[] { new(SkillEffect.BuffCritDamage, critDmg(0)) }
                    : Array.Empty<EffectMagnitude>(),
                Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                    MpCost: focusMp[i], SpCost: focusSp[i], BlowRatePct: rate(i),
                    Magnitudes: critDmg(i) > 0f
                        ? new EffectMagnitude[] { new(SkillEffect.BuffCritDamage, critDmg(i)) }
                        : Array.Empty<EffectMagnitude>(),
                    Description: rung(i))).ToArray());

        list.Add(Focus(LethalFocus, "Lethal Focus",
            i => focusBudget[i] / 2f, i => focusBudget[i] / 2f,
            "Five minutes of cold attention: your blows land more often AND bite deeper. Requires duals.",
            i => $"5 min: blow landing rate ×{1f + focusBudget[i] / 2f:0.00} and +{focusBudget[i] / 2f * 100f:0.#} crit damage."));

        list.Add(Focus(LethalPrecision, "Lethal Precision",
            _ => 0f, i => focusBudget[i],
            "Five minutes of elven exactness: when a blow lands it lands ruinously. Requires duals.",
            i => $"5 min: +{focusBudget[i] * 100f:0.#} crit damage."));

        list.Add(Focus(LethalFrenzy, "Lethal Frenzy",
            i => focusBudget[i], _ => 0f,
            "Five minutes of red haste: far more of your blows find the gap. Requires duals.",
            i => $"5 min: blow landing rate ×{1f + focusBudget[i]:0.00}."));

        // ═══ VITAL POINTS — the shared 52/64/74 passive, +10 / 15 / 20% blow rate ════════════════
        // No race split: all three melee disciplines learn the same three rungs.
        float[] vitalPoints = { 0.10f, 0.15f, 0.20f };
        int[]   vitalSp     = { RogueSp[4], RogueSp[9], RogueSp[14] };
        list.Add(new SkillDef(VitalPoints, "Vital Points", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: vitalSp[0],
            Passive: new PassiveEffect(BlowRate: vitalPoints[0]),
            Description: "Passive. You know where the seams in armour are, and you find them oftener.",
            Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                SpCost: vitalSp[i], Passive: new PassiveEffect(BlowRate: vitalPoints[i]),
                Description: $"Blow landing rate ×{1f + vitalPoints[i]:0.00}.")).ToArray()));

        return list.ToArray();
    }

    /// <summary>One of the four straight BLOW families. They differ only in power, cast time, hit
    /// count and whether they leave a self-buff — everything else is the same fifteen-rung row: duals,
    /// reach 40, 3s reuse, <see cref="StabMp"/>, <see cref="RogueSp"/>, crit-only with a 1% floor,
    /// and his `[precise_shot]` REPLACES cell.
    /// <para>⚠ <see cref="SkillDef.CritRateMod"/> is 2.0, copied from Piercing Stab: a blow's landing
    /// chance was never the raw crit rate. Without it a level-74 rogue would be landing his signature
    /// skill's full number about a third of the time and the 1% floor the rest.</para></summary>
    private static SkillDef StabSkill(string id, string name, int[] power, int castTicks,
                                 string blurb, Func<int, string> rung,
                                 int hitCount = 1, string? selfBuff = null,
                                 SkillLevel[]? fourth = null, int cooldownTicks = 30)
        => new(id, name, BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: StabMp[0], CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: 40, Power: power[0],
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            BlowOnCrit: true,
            // `BL-188` - see Killing Stab: the blow gate left the crit chain on 2026-09-09.
            // `BL-203` — [Double]. The halving above and this flag are ONE change: the old power is
            // still reachable, on the rate Overpower grants. ⚠ A blow rolls its double INSIDE
            // ResolveBlow (after the crit-damage values, never instead of them), so a doubled stab
            // is the crit number ×2 — which is exactly the figure he measured before the halving.
            CanDouble: true,
            HitCount: hitCount, SelfBuff: selfBuff,
            RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            Description: blurb,
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: power[i], MpCost: StabMp[i], SpCost: RogueSp[i], Description: rung(i)))
                .Concat(fourth ?? Array.Empty<SkillLevel>()).ToArray());

    /// <summary>One race's Phantom Jump. Three rungs, and the ladder is the reach.</summary>
    private static SkillDef PhantomJump(string id, SkillEffect rider, DebuffSchool school,
                                        bool charms, float slow, string what, string blurb)
    {
        int[] mp = { 90, 120, 150 };
        int[] sp = { 74_000, 120_000, 880_000 };
        float[] range = { 450f, 600f, 750f };
        var mags = slow > 0f
            ? new EffectMagnitude[] { new(SkillEffect.Slow, slow) }
            : Array.Empty<EffectMagnitude>();

        return new SkillDef(id, "Phantom Jump", BaseClass.Fighter, rider | SkillEffect.Blink,
            MpCost: mp[0], CastTicks: 5, CooldownTicks: 200, Range: range[0], Power: 0,
            DurationTicks: 30, BuffKey: id, Rank: 1,
            DebuffSchool: school, Charms: charms,
            Category: SkillCategory.Debuff, PhysicalCast: school == DebuffSchool.Physical,
            SpCost: sp[0],
            Magnitudes: mags,
            Description: blurb,
            Levels: Enumerable.Range(0, 3).Select(i => new SkillLevel(
                MpCost: mp[i], SpCost: sp[i], Range: range[i], Magnitudes: mags,
                Description: $"Blinks to a target up to {range[i]:0} away and {what} for 3s.")).ToArray());
    }

    /// <summary>The Dual Mastery proc's payload id. Declared beside the const block it belongs to.</summary>
    public const string DualMasteryRush = "dual_mastery_rush";
}
