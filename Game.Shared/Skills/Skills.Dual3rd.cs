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

    /// <summary>Killing Stab / Swift Stab — the same power ladder, 1250 → 6400. It is his, and it is
    /// a straight arithmetic climb apart from the widening stride at 52.</summary>
    private static readonly int[] StabPower =
    {
        1250, 1500, 1750, 2000, 2400, 2800, 3200, 3600,
        4000, 4400, 4800, 5200, 5600, 6000, 6400,
    };

    /// <summary>Venom Stab — deliberately about half of <see cref="StabPower"/>, because the Demon is
    /// paid for the difference in stacks that Venom Burst spends.</summary>
    private static readonly int[] VenomStabPower =
    {
        675, 750, 875, 1000, 1200, 1400, 1600, 1800,
        2000, 2200, 2400, 2600, 2800, 3000, 3200,
    };

    /// <summary>Heavy Stab — his *"power 950 twice"*: TWO resolutions (<see cref="SkillDef.HitCount"/>)
    /// of this number, on a 3-second cast rather than Killing Stab's one.</summary>
    private static readonly int[] HeavyStabPower =
    {
        950, 1125, 1300, 1500, 1800, 2100, 2400, 2700,
        3000, 3300, 3600, 3900, 4200, 4500, 4800,
    };

    /// <summary>Venom Burst — damage PER CONSUMED STACK, ×250 at 40 climbing to ×1280 at 74.</summary>
    private static readonly int[] VenomBurstPerStack =
    {
        250, 300, 350, 400, 480, 560, 640, 720,
        800, 880, 960, 1040, 1120, 1200, 1280,
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

    /// <summary>🔑 A BLOW'S NON-CRIT FLOOR IS 1%, NOT the engine's default 10%. Every stab row in this
    /// file reads "power N - only when skill does critical - otherwise N/100" — 1250/12, 6400/64 —
    /// where the 2nd class's Piercing Stab reads 314/31, a tenth. He has made the 3rd-tier blow far
    /// more all-or-nothing than the one it continues, and that is the identity of the branch.</summary>
    private const float ThirdTierBlowFloor = 0.01f;

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
                           + $"×{1f + dualAtkSpd[i]:0.00} attack speed.")),
            WeaponMasteryLevels: Enumerable.Range(0, BulwarkLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Dual: new PassiveEffect(
                        PhysAtk: dualAtk[i], PhysAtkPct: 0.085f,
                        CritDamageFlat: dualCritDmg[i], Accuracy: 3,
                        CritRate: dualCritRate[i], AtkSpeedPct: dualAtkSpd[i]),
                    RequiredWeapon: WeaponType.Dual)).ToArray()));

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
            i => $"Blow power {StabPower[i]:N0} on a critical; {(int)MathF.Round(StabPower[i] * ThirdTierBlowFloor)} otherwise."));

        // ═══ SWIFT STAB — the Elf's ══════════════════════════════════════════════════════════════
        // Killing Stab's power on HALF the cast time, and it leaves a 5-second rush behind it.
        list.Add(StabSkill(SwiftStab, "Swift Stab", StabPower, castTicks: 5,
            "A blur of a blow that carries you forward with it.",
            i => $"Blow power {StabPower[i]:N0} on a critical; {(int)MathF.Round(StabPower[i] * ThirdTierBlowFloor)} otherwise. "
               + "Leaves +5 speed and +15% attack speed for 5s.",
            selfBuff: SwiftStabRush));

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
        list.Add(StabSkill(HeavyStab, "Heavy Stab", HeavyStabPower, castTicks: 30,
            "Two heavy blows, wound up and delivered. Each bites on its own.",
            i => $"Strikes 2 times; blow power {HeavyStabPower[i]:N0} each on a critical, "
               + $"{(int)MathF.Round(HeavyStabPower[i] * ThirdTierBlowFloor)} otherwise.",
            hitCount: 2));

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
            SkillEffect.PhysicalDamage | SkillEffect.Venom
            | SkillEffect.DebuffAtk | SkillEffect.DebuffDef,
            MpCost: StabMp[0], CastTicks: 10, CooldownTicks: 30, Range: 40, Power: VenomStabPower[0],
            DurationTicks: 300, BuffKey: "venom", Rank: 3, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_venom", MaxStacks: 10, StacksPerCast: 1,
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            CanDouble: true, BlowOnCrit: true, BlowFailFraction: ThirdTierBlowFloor,
            CritRateMod: 2.0f, RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtk, 0.15f), new(SkillEffect.DebuffDef, 0.15f),
            },
            Description: "A poisoned blade — less damage than a killing blow, but it banks venom "
                       + "for Venom Burst to spend.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: VenomStabPower[i], MpCost: StabMp[i], SpCost: RogueSp[i],
                Rank: VenomTier[i], StacksPerCast: VenomStacksPerCast[i],
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.DebuffAtk, 0.15f), new(SkillEffect.DebuffDef, 0.15f),
                },
                Description: $"Blow power {VenomStabPower[i]:N0} on a critical; "
                           + $"{(int)MathF.Round(VenomStabPower[i] * ThirdTierBlowFloor)} otherwise. "
                           + $"Adds {VenomStacksPerCast[i]} tier-{VenomTier[i]} venom stack(s), max 10."))));

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
            SkillEffect.PhysicalDamage | SkillEffect.Venom
            | SkillEffect.DebuffAtk | SkillEffect.DebuffDef,
            MpCost: StabMp[0], CastTicks: 10, CooldownTicks: 100, Range: 40, Power: VenomBurstPerStack[0],
            DurationTicks: 300, BuffKey: "venom", Rank: 3, SharesLadderKey: true,
            DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_venom", ConsumeStackKey: "venom_venom", MaxStacks: 10,
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            CanDouble: true, RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtk, 0.15f), new(SkillEffect.DebuffDef, 0.15f),
            },
            Description: "Detonates every venom stack on the target for damage per stack — and if "
                       + "there are none, lays the first one instead.",
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: VenomBurstPerStack[i], MpCost: StabMp[i], SpCost: RogueSp[i],
                Rank: VenomTier[i], StacksPerCast: VenomStacksPerCast[i],
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.DebuffAtk, 0.15f), new(SkillEffect.DebuffDef, 0.15f),
                },
                Description: $"Power {VenomBurstPerStack[i]:N0} per consumed venom stack (up to ×10). "
                           + $"With no stacks on the target, lays {VenomStacksPerCast[i]} tier-{VenomTier[i]} instead."))));

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
                                 int hitCount = 1, string? selfBuff = null)
        => new(id, name, BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: StabMp[0], CastTicks: castTicks, CooldownTicks: 30, Range: 40, Power: power[0],
            Category: SkillCategory.Physical, SpCost: RogueSp[0],
            CanDouble: true, BlowOnCrit: true, BlowFailFraction: ThirdTierBlowFloor,
            CritRateMod: 2.0f, HitCount: hitCount, SelfBuff: selfBuff,
            RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { PreciseShot },
            Description: blurb,
            Levels: BulwarkRungs(i => new SkillLevel(
                Power: power[i], MpCost: StabMp[i], SpCost: RogueSp[i], Description: rung(i))));

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
