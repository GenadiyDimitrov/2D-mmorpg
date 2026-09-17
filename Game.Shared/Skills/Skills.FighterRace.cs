using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE FIGHTER'S RACE LAYER — every row of the race block in
/// <c>docs/data/classes_skills_csv/fighter 1st.csv</c>, landed 2026-09-17.
///
/// <para>🔑 <b>THE FILE IS `fighter 1st`, BUT THE LADDERS RUN TO 74.</b> That is the whole point of
/// his pass and the one structural thing to understand here: these are not "first-class skills". They
/// are the RACE's contribution to every fighter, bought from level 10 and still climbing at 74, and
/// they follow the character through the 2nd, 3rd and 4th class changes untouched. A warrior, a tank,
/// an archer and a dagger of the same race all learn exactly this, rung for rung.</para>
///
/// <para>🔑 <b>SO THEY ARE INJECTED CENTRALLY, NOT LISTED PER CLASS</b> — see
/// <c>ClassSkills.FighterRaceSkills</c>, which <c>Cumulative</c> yields for every fighter whatever its
/// archetype or discipline. Fanning them out across the eight fighter paths × three races would be
/// twenty-four lists that have to agree with each other forever, and the masteries beside them already
/// settled that argument: one injector, impossible to get half-right.</para>
///
/// <para>🔴 <b>ANTIDOTE MOVED HERE AND GOT SIX YEARS YOUNGER.</b> It used to be a 3rd-class skill with
/// six rungs at 52-74, registered by hand against the Bulwark, the Ravager, the Warlord and the three
/// rogue disciplines. His 2026-09-17 pass DELETED those rows from all four files and re-authored the
/// skill here as NINE rungs from level 10, curing ranks 1-9. The old registrations are gone with them;
/// do not reinstate one, or an Elf buys rungs 4-9 twice.</para>
///
/// <para>⚠ <b>THE RACE SPLIT IS THE IDENTITY, and it is not symmetrical by design.</b> The Elf carries
/// the two SUSTAIN skills (a cure and a self-heal), the Demon the two OFFENSIVE ones (a drain and a
/// bleed), the Human the two DEFENSIVE ones (a resist window and a rest stance). Three very different
/// answers to "what does my race do for me between fights", which is what keeps a Human dagger from
/// playing like an Elf one when their class kits are identical.</para></summary>
public static partial class SkillCatalog
{
    // ═══ THE SIX RACE SKILLS ════════════════════════════════════════════════════════════════════
    // ⚠ `ElfAntidote` MOVED HERE from Skills.Dual3rd.cs on 2026-09-17, const and def together, when
    //   it stopped being a 3rd-class skill. Its id is unchanged and append-only as always.
    public const string ElfAntidote      = "elf_antidote";
    public const string ElfHeal          = "elf_heal";
    public const string DemonDrain       = "demon_drain";
    public const string DemonPain        = "demon_pain";
    public const string HumanParry       = "human_parry";
    public const string HumanRelaxation  = "human_relaxation";
    public const string GradePermission  = "grade_penalty";

    /// <summary>Learn levels of the Elf's cure — his nine rows, 10 through 72.</summary>
    public static readonly int[] ElfAntidoteLevels = { 10, 24, 40, 49, 55, 60, 64, 68, 72 };
    /// <summary>Learn levels of the Elf's self-heal and the Human's rest stance — both his eight-row
    /// ladder, 15 through 74. One array because his two files use the identical cadence.</summary>
    public static readonly int[] RaceEightLevels = { 15, 28, 40, 49, 58, 64, 70, 74 };
    /// <summary>Learn levels of the Demon's two ladders — his twenty-one rows, 15 through 74. The
    /// densest ladder any fighter owns, and the reason the Demon's race layer reads as a KIT rather
    /// than a pair of occasional buttons.</summary>
    public static readonly int[] DemonLevels =
        { 15, 20, 24, 28, 32, 36, 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
    /// <summary>Learn levels of the Human's Weapon Parry — his five rows, 10 through 70.</summary>
    public static readonly int[] HumanParryLevels = { 10, 20, 40, 60, 70 };

    /// <summary>The SP his race block charges, per rung, for each ladder. Read straight off the file's
    /// SP column — never derived, because the price in this game is the LEVEL YOU LEARN AT and these
    /// ladders learn at different levels from each other.</summary>
    private static readonly int[] AntidoteSp =
        { 460, 3_200, 14_000, 25_000, 44_000, 60_000, 90_000, 160_000, 320_000 };
    private static readonly int[] RaceEightSp =
        { 1_500, 12_000, 28_000, 50_000, 88_000, 190_000, 390_000, 880_000 };
    private static readonly int[] DemonSp =
    {
        910, 1_700, 3_200, 6_000, 11_000, 20_000, 28_000, 35_000, 40_000, 50_000, 74_000,
        80_000, 88_000, 120_000, 170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
    };
    private static readonly int[] HumanParrySp = { 910, 3_400, 28_000, 120_000, 390_000 };

    private static IEnumerable<SkillDef> FighterRaceSkills()
    {
        var list = new List<SkillDef>();

        // ═══ ELF — ANTIDOTE, the SELF cure ══════════════════════════════════════════════════════
        //
        // ⚠ NOT the healer's `antidote`, and deliberately a separate id: this one is `self/single` in
        //   every row he has ever written for it, and its ceiling ladder is its own. Registering the
        //   healer's skill here would hand every fighter a TARGETED cure on the healer's ladder.
        //
        // ⚠ AN EXPLICIT BuffKey, even though a cure lands no buff at all: the startup ladder guard
        //   keys on the display NAME when none is given, and "Antidote" is also the healer's skill.
        //   Two multi-rung ladders on one key make each other's rungs compete (`BL-85`).
        //
        // 🔑 `PhysicalCast` — his TYPE column reads `Physical/Heal`, changed from `Magic/Heal` in the
        //    same pass that moved the skill. It is what keeps the cure off the fizzle roll and paces it
        //    by ATTACK speed: a fighter has no WIT to pay for a magical one. `Category` stays Heal,
        //    which is the ROLE tag — see SkillMath.IsPhysical for why those are two different
        //    questions (`BL-237` was the last time someone conflated them).
        int[] antidoteMp = { 9, 20, 31, 42, 50, 53, 57, 60, 64 };
        list.Add(new SkillDef(ElfAntidote, "Antidote", BaseClass.Fighter, SkillEffect.Cleanse,
            MpCost: antidoteMp[0], CastTicks: 10, CooldownTicks: 100, Range: 0, Power: 0,
            BuffKey: "elf_antidote", PhysicalCast: true,
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly,
            DispelMask: SkillEffect.Poison | SkillEffect.Venom | SkillEffect.Bleed,
            DispelMaxLevel: 1, SpCost: AntidoteSp[0],
            Description: "Purges poison, venom and bleeding from your own blood.",
            Levels: Enumerable.Range(0, ElfAntidoteLevels.Length).Select(i => new SkillLevel(
                MpCost: antidoteMp[i], SpCost: AntidoteSp[i], DispelMaxLevel: i + 1,
                Description: $"Cures poison, venom and bleed of rank {i + 1} or lower from yourself."))
                .ToArray()));

        // ═══ ELF — HEALING LEAF, the self-heal ══════════════════════════════════════════════════
        //
        // 🔑 PHYSICAL, like the cure beside it, and for the same reason: it is a FIGHTER's heal. It
        //    neither fizzles nor scales off WIT — the power ladder IS the skill.
        // 🔑 THE MP IS OURS, NOT HIS. His eight rows left the MP column EMPTY; asked on 2026-09-17 he
        //    ruled *"price it like elf_antidote's ladder"*, so these are the cure's numbers read off at
        //    Healing Leaf's own learn levels. If he ever fills that column in, the file wins.
        int[] healMp = { 13, 23, 31, 42, 52, 57, 62, 66 };
        int[] healPower = { 100, 200, 300, 400, 500, 600, 700, 800 };
        list.Add(new SkillDef(ElfHeal, "Healing Leaf", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: healMp[0], CastTicks: 50, CooldownTicks: 50, Range: 0, Power: healPower[0],
            PhysicalCast: true, Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly,
            SpCost: RaceEightSp[0],
            Description: "Mends your own wounds with the forest's own patience.",
            Levels: Enumerable.Range(0, RaceEightLevels.Length).Select(i => new SkillLevel(
                Power: healPower[i], MpCost: healMp[i], SpCost: RaceEightSp[i],
                Description: $"Mends your wounds, healing you with {healPower[i]} power."))
                .ToArray()));

        // ═══ DEMON — DEMONIC DRAIN, the ranged lifesteal ════════════════════════════════════════
        //
        // 🔑 A PHYSICAL skill that DRAINS — `Lifesteal` is the marker the engine already reads (it is
        //    what makes the mage's Vampiric Bolt heal), so this needs no new channel. 60% of the
        //    damage dealt, at every rung; only the power grows.
        // ⚠ ITS RANGE GROWS AND HIS COLUMN SAYS SO: 400 at the opening rung, 600 through the twenties
        //   and thirties, 800 from 40 up. It is the one fighter skill outside the bow tree whose reach
        //   is a ladder, so the per-rung Range is not decoration — read it, don't flatten it.
        int[] drainMp =
            { 17, 20, 23, 25, 30, 35, 36, 40, 43, 45, 47, 50, 55, 56, 58, 62, 65, 68, 70, 75, 78 };
        int[] drainPower =
        {
            60, 80, 100, 150, 200, 250, 600, 900, 1200, 1500, 1800,
            2100, 2400, 2700, 2900, 3100, 3300, 3500, 3700, 3900, 4000,
        };
        list.Add(new SkillDef(DemonDrain, "Demonic Drain", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: drainMp[0], CastTicks: 30, CooldownTicks: 60, Range: 400, Power: drainPower[0],
            Category: SkillCategory.Physical,
            Lifesteal: 0.60f, CanCrit: true, SpCost: DemonSp[0],
            Description: "A reaching claw of shadow that feeds you what it tears out.",
            Levels: Enumerable.Range(0, DemonLevels.Length).Select(i => new SkillLevel(
                Power: drainPower[i], MpCost: drainMp[i], SpCost: DemonSp[i],
                Range: DemonLevels[i] >= 40 ? 800f : DemonLevels[i] >= 20 ? 600f : 400f,
                Description: $"Deals physical damage with +{drainPower[i]} power and restores "
                           + "60% of it as HP."))
                .ToArray()));

        // ═══ DEMON — DEMONIC PAIN, the bleed ════════════════════════════════════════════════════
        //
        // 🔑 A SOLO DEBUFF: it inflicts bleed and does NOTHING else — no direct damage at all, which
        //    is exactly why its landing modifier is the highest price in the table. Owner, 2026-09-17,
        //    asked for it as `docs/data/debuff_landmods.csv` requires: **x1.5**, his own solo-debuff
        //    rate (*"an armor break should stay as solo debuff and nothing else at x1.5"*).
        //    ⚠ NEVER pick one of these yourself — that row is his column.
        // 🔑 THE RANK IS THE LADDER. All twenty-one rungs are 15 seconds at melee range for the same
        //    MP; what climbs is the DoT TIER, 1 through 9, and the tier is where both the damage and
        //    the -20% move-speed rider come from (`DotTiers`). So a rung is worth buying because the
        //    bleed bites harder, never because the skill got a bigger number of its own.
        int[] painRank = { 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9 };
        list.Add(new SkillDef(DemonPain, "Demonic Pain", BaseClass.Fighter, SkillEffect.Bleed,
            MpCost: drainMp[0], CastTicks: 20, CooldownTicks: 100, Range: 40, Power: 0,
            // 🔑 `FlatRank` — and the startup ladder guard is what forced the question, correctly.
            //    A childless multi-rung buff normally carries its LEVEL in its rank (`BL-85`), so rung
            //    21 would have ranked 21 against Frost Pierce's 3 and every bleed in the game would
            //    have been ordered by which rung bought it. Here the Rank IS the DoT TIER — 1 through
            //    9, his column — so it must go in verbatim. The key stays `bleed`: one bleed per
            //    target (only venom stacks), and the HIGHER TIER wins, which is the right outcome in
            //    both directions.
            DurationTicks: 150, BuffKey: "bleed", FlatRank: true, Rank: painRank[0],
            DotKind: DotKind.Bleed,
            DebuffSchool: DebuffSchool.Physical, DebuffLandMod: 1.5f,
            Category: SkillCategory.Physical, SpCost: DemonSp[0],
            Description: "Opens a wound that will not close — a physical bleed, and nothing else. "
                       + "Lands on an ATK-vs-CON contest.",
            Levels: Enumerable.Range(0, DemonLevels.Length).Select(i => new SkillLevel(
                MpCost: drainMp[i], SpCost: DemonSp[i], Rank: painRank[i],
                Description: $"Inflicts bleed of rank {painRank[i]} on the target for 15s."))
                .ToArray()));

        // ═══ HUMAN — WEAPON PARRY, the resist window ════════════════════════════════════════════
        //
        // 🔑 IT RAISES RESISTANCES, IT DOES NOT SUBTRACT DAMAGE. Owner, 2026-09-17: *"we have MRes
        //    channel .. we need PRes .. its more like Increases mRes and pRes with x%"*. So his row's
        //    "decrease dmg taken with 20%" is the FEELING, not the mechanism — the skill adds 0.20 to
        //    both defence coefficients, which in a ratio-damage game is ~17% less damage, not 20%.
        //    ⚠ Do not "fix" that gap by moving it to a multiplier on the finished number: a resist has
        //      to ride INSIDE the defence or a defence-ignoring skill would still be stopped by it.
        // 🔑 THE mRes HALF RIDES THE FLAG, THE pRes HALF RIDES A FIELD. `BuffMagicResist` is bit 31 and
        //    already existed; the enum has been full since 1L << 62, so pRes is
        //    `SkillDef.PhysicalResistPct`. A buff still needs one AnyBuff flag to land at all, and the
        //    mRes magnitude is what provides it — which is why this skill can never drop the flag half.
        // 🔑 DURATION IS 10s, NOT THE 1 HIS FILE SAID (*"10s (ate the 0)"*, 2026-09-17). One second on
        //    a sixty-second reuse would have been a button nobody presses.
        // ⚠ TWO-HANDED SWORD OR BLUNT ONLY — his `Sword|Blunt/2h`, where `|` is OR and `/` is AND.
        //   A dagger, a bow or a sword-and-board Human cannot parry.
        float[] parryRes = { 0.05f, 0.10f, 0.20f, 0.30f, 0.40f };
        float[] parryCc  = { 0f,    0.05f, 0.10f, 0.15f, 0.20f };
        int[]   parryMp  = { 15, 20, 36, 56, 70 };
        list.Add(new SkillDef(HumanParry, "Weapon Parry", BaseClass.Fighter,
            SkillEffect.BuffMagicResist,
            MpCost: parryMp[0], CastTicks: 10, CooldownTicks: 600, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: "human_parry", PhysicalCast: true,
            Category: SkillCategory.Physical, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            SpCost: HumanParrySp[0],
            Description: "A braced guard behind a two-handed weapon: more physical and magical "
                       + "resistance, and a steadier mind, for ten seconds.",
            Levels: Enumerable.Range(0, HumanParryLevels.Length).Select(i => new SkillLevel(
                MpCost: parryMp[i], SpCost: HumanParrySp[i],
                PhysicalResistPct: parryRes[i],
                CcResistPhysical: parryCc[i], CcResistMagical: parryCc[i],
                Magnitudes: new[] { new EffectMagnitude(SkillEffect.BuffMagicResist, parryRes[i]) },
                Description: parryCc[i] > 0f
                    ? $"+{parryRes[i] * 100:0}% p.Res and m.Res, and +{parryCc[i] * 100:0}% "
                       + "resistance to CON and SPT debuffs, for 10s."
                    : $"+{parryRes[i] * 100:0}% p.Res and m.Res for 10s."))
                .ToArray()));

        // ═══ HUMAN — RELAX, the rest stance ═════════════════════════════════════════════════════
        //
        // 🔑 A THIRD REGEN CHANNEL, not a multiplier and not a flat grant: his rows are a FRACTION OF
        //    YOUR OWN POOL PER SECOND (1% HP/s at the first rung, 5% HP + 3% MP at the last). See
        //    `SkillDef.HpRegenPerSecondPct` for why neither existing channel could say that.
        // 🔑 IT COSTS NOTHING TO HOLD, and that is authored: every one of his eight rows reads 0 MP.
        //    The price is that you are sitting and cannot act, which is a real price while a camp
        //    respawns around you.
        // ⚠ `EndsOnDamageTaken` — *"status is canceld on dmg taken"*, the same field the healer's
        //   Meditation uses. Without it a Human would out-regenerate a mob chewing on him.
        float[] relaxHp = { 0.010f, 0.020f, 0.025f, 0.030f, 0.035f, 0.040f, 0.045f, 0.050f };
        float[] relaxMp = { 0f,     0.01f,  0.01f,  0.01f,  0.02f,  0.02f,  0.02f,  0.03f };
        list.Add(new SkillDef(HumanRelaxation, "Relax", BaseClass.Fighter, SkillEffect.BuffHpRegen,
            MpCost: 0, CastTicks: 50, CooldownTicks: 100, Range: 0, Power: 0,
            DurationTicks: 0, BuffKey: "human_relaxation", Toggle: true, PhysicalCast: true,
            Category: SkillCategory.Physical, TargetMode: TargetMode.SelfOnly,
            EndsOnDamageTaken: true, SpCost: RaceEightSp[0],
            Description: "Sit and let the body do its work. You cannot act, and any damage ends it.",
            Levels: Enumerable.Range(0, RaceEightLevels.Length).Select(i => new SkillLevel(
                SpCost: RaceEightSp[i],
                HpRegenPerSecondPct: relaxHp[i], MpRegenPerSecondPct: relaxMp[i],
                Description: relaxMp[i] > 0f
                    ? $"While seated: {relaxHp[i] * 100:0.#}% of max HP and {relaxMp[i] * 100:0.#}% "
                       + "of max MP every second. Cannot act; ends on damage."
                    : $"While seated: {relaxHp[i] * 100:0.#}% of max HP every second. "
                       + "Cannot act; ends on damage."))
                .ToArray()));

        return list;
    }

    // ═══ THE GRADE PERMISSION PASSIVE ═══════════════════════════════════════════════════════════
    //
    // 🔑 PURELY INFORMATIONAL, and that is the entire specification. Owner, 2026-09-17: *"Just user to
    //    know when he is 58 and got B grade drop that he is not yet allowed to wear."* The grade
    //    system itself has existed since 2026-07-16 (`GradePenalty`) and is unchanged — this skill
    //    grants nothing, blocks nothing and modifies no stat. It exists so the answer to "can I wear
    //    this yet" is sitting in the player's own skill window instead of having to be discovered by
    //    equipping the thing and watching his numbers fall.
    //
    // 🔑 ONE ID, SEVEN NAMED RUNGS — his call: *"cannot grade_penalty be one id and jsut change the
    //    description and Name ? its just informational passive"*. He is right, and it is why
    //    `SkillLevel.Name` and `SkillDef.NameAt` exist: a rung here is "Grade C", never "Grade F Lv.4".
    //
    // ⚠ THE LEVELS ARE NOT RE-DECLARED HERE. They are `GradePenalty.GradeLevels` — the array the
    //   penalty math, the enchant rules and the item tooltips already read. His seven (1/20/40/52/61/
    //   76/80) are that array exactly, which is the point: a second copy would be free to drift, and
    //   then the skill would promise a grade the equip math still charges you for.
    private static IEnumerable<SkillDef> GradePassiveSkills()
    {
        var names = GradePenalty.GradeNames;      // F E D C B A S
        var levels = GradePenalty.GradeLevels;    // 1 20 40 52 61 76 80

        yield return new SkillDef(GradePermission, "Grade " + names[0], BaseClass.Fighter,
            SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, TargetMode: TargetMode.SelfOnly,
            SpCost: 0,
            Description: "Which equipment grade you may wear without penalty.",
            Levels: Enumerable.Range(0, levels.Length).Select(i => new SkillLevel(
                MpCost: 0, SpCost: 0, Name: "Grade " + names[i],
                Description: i + 1 < levels.Length
                    ? $"You may equip {names[i]} grade and below. "
                    + $"{names[i + 1]} grade opens at level {levels[i + 1]}; wearing it sooner costs "
                    + "you stats until you get there."
                    : $"You may equip {names[i]} grade — the highest there is. Nothing is above you."))
                .ToArray());
    }
}
