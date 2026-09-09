using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE ARCHER'S DERIVED KIT, RETIRED 2026-09-09 — four skills that lived for three days.
///
/// <para>🔴 <b>NOTHING HERE IS LEARNABLE ANY MORE.</b> `archer 3rd.csv` landed and Skills.Archer3rd.cs
/// carries his authored rows instead; these four were the ×1.25 derivation off the Elf Harmonist that
/// `BL-185` built on 2026-09-06 while he wrote the file. Every one of them is retired by a
/// <see cref="SkillDef.Replaces"/> clause on its authored successor:
/// <list type="bullet">
///   <item><c>archer_bow_mastery</c> → <c>bow_weapon_mastery</c></item>
///   <item><c>archer_split_volley</c> → <c>archer_twin_arrows</c></item>
///   <item><c>archer_bow_expertise</c> → <c>wc_bow_expertise</c> (his own cell names the buffer's id)</item>
///   <item><c>archer_crit_focus</c> → <c>bow_weapon_mastery</c>, whose crit-damage column replaces it</item>
/// </list></para>
///
/// <para>🔑 <b>WHY THE DEFS SURVIVE AT ALL.</b> The house rule, stated in four places and learned the
/// hard way: <c>LearnedSkills</c> persists IDS, so deleting a def breaks every character who bought
/// one. 0.116.0 shipped these to his phone — a level-90 archer made to check the damage matrix is
/// exactly what they were built for — so a character holding one is not hypothetical. Kept, orphaned,
/// and above all REPLACED, because <c>archer_bow_mastery</c> left in place would have stacked a second
/// bow passive on top of his authored one and <c>archer_crit_focus</c> would have been a permanent
/// +20% crit damage nobody could account for.</para>
///
/// <para>⚠ DO NOT RE-GRANT THESE and do not extend them. When the numbers here disagree with
/// Skills.Archer3rd.cs, his file is right by construction.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- ARCHER (Sharpshooter / Hunter / Trapper) ----
    /// <summary>Archer bow mastery — the Harmonist Bow Mastery ladder, P.Atk ×1.25, same +400 range.</summary>
    public const string ArcherBowMastery = "archer_bow_mastery";
    /// <summary>Archer two-arrow skill — Sound Burst's ladder at ×1.25, same 900 range and same
    /// <see cref="SkillDef.HitCount"/> of 2 (two independent resolutions, not one double hit).</summary>
    public const string ArcherSplitVolley = "archer_split_volley";
    /// <summary>Archer Bow Expertise — *"have the same bow expertise"*, i.e. the HARMONIST's +12% rung
    /// rather than the rogue's +8%. ⚠ A CLONE, not a re-registration: `wc_bow_expertise` is declared
    /// <see cref="BaseClass.Mage"/> and this class is a Fighter. Same <c>BuffKey</c> and Rank as the
    /// harmonist's, so an archer and a buffer in the same party never stack two of them.</summary>
    public const string ArcherBowExpertise = "archer_bow_expertise";
    /// <summary>Archer crit passive — his *"passive that increase crit dmg +20% and +700flat"*.</summary>
    public const string ArcherCritFocus = "archer_crit_focus";

    private static SkillDef[] ArcherKitRetiredSkills()
    {
        var list = new List<SkillDef>();
        int[] kitMastSp = { 36_000, 64_000, 81_000, 120_000, 190_000, 320_000, 390_000, 880_000 };

        // ---- Archer Bow Mastery — Harmonist Bow Mastery's { 100 … 600 } at ×1.25. The +400 range is
        //      copied FLAT and unscaled: it does not ladder on the harmonist either, and range is not
        //      damage. ----
        int[] archerBowAtk = new[] { 100, 200, 300, 400, 500, 540, 560, 600 }.Select(Up).ToArray();
        list.Add(new SkillDef(ArcherBowMastery, "Archer Bow Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Your bow reaches much further and hits much harder.",
            Levels: archerBowAtk.Select((a, i) => new SkillLevel(SpCost: kitMastSp[i],
                Description: $"Bow: +{a} P.Atk, +400 range.")).ToArray(),
            WeaponMasteryLevels: archerBowAtk.Select(a => new WeaponMasteryProfile(
                Bow: new PassiveEffect(PhysAtk: a, BowRange: 400f))).ToArray()));

        // ---- Archer Split Volley — Sound Burst's thirteen rungs at ×1.25 power, and its HitCount of
        //      2: TWO independent resolutions of the same power, so each rolls its own crit and its
        //      own evasion check. That is what makes the archer a crit class rather than a big-hit
        //      one, and it is the reason his IG table's archer crit multiplier climbs to ×5. ----
        list.Add(new SkillDef(ArcherSplitVolley, "Split Volley", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: SoundMp[0], CastTicks: 30, CooldownTicks: 30, Range: 900, Power: Up(SoundPower[0]),
            Category: SkillCategory.Physical,
            RequiredWeapon: WeaponType.Bow, HitCount: 2,
            Description: "Looses two arrows on one breath — each resolves on its own.",
            Levels: Enumerable.Range(0, SoundPower.Length).Select(i => new SkillLevel(
                Power: Up(SoundPower[i]), MpCost: SoundMp[i], SpCost: BandSp13[i],
                Description: $"Strikes 2 times for power {Up(SoundPower[i])} each.")).ToArray()));

        // ---- Archer Bow Expertise — the harmonist's rung, cloned onto the Fighter class. Numbers are
        //      his verbatim: +12% attack speed, 85 MP, 20-minute duration, 42,000 SP. NOT scaled by
        //      KitFactor — he asked for "the same bow expertise", not a better one. ----
        list.Add(new SkillDef(ArcherBowExpertise, "Bow Expertise", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 85, CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "bow_expertise", Rank: 2,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 42_000,
            RequiredWeapon: WeaponType.Bow,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.12f) },
            Description: "Steadies your aim: +12% attack speed while wielding a bow, for 20 minutes."));

        // ---- Archer Killing Focus — his *"passive that increase crit dmg +20% and +700flat"*.
        //
        // ⚠ WHAT THE 700 IS ACTUALLY WORTH, so nobody is surprised by it later: `CritDamageFlat`
        //   joins P.Atk INSIDE the ratio (StatCalculator.CritFlatFactor), so at a buffed level-90
        //   P.Atk of ~4500 it is +15.6% on a BASIC crit — and once Split Volley's power is in the
        //   numerator it falls to roughly +5%. The +20% multiplier is the larger half by far. Both
        //   numbers are his and both are here; this note exists because "700" reads much bigger than
        //   it plays. ⚠ For scale, today's whole `RogueWM` ladder tops out at CritDamageFlat 165.
        //
        // ONE RUNG, deliberately: he gave one pair of numbers, not a ladder, and inventing eight rungs
        // to reach them would be authoring where he did not.
        list.Add(new SkillDef(ArcherCritFocus, "Killing Focus", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Your critical hits land far harder.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 120_000,
                    Passive: new PassiveEffect(CritDamage: 0.20f, CritDamageFlat: 700f),
                    Description: "+20% critical damage, and +700 attack inside a critical hit."),
            }));

        return list.ToArray();
    }
}
