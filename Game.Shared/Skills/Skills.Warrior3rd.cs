using System;
using System.Linq;

namespace Game.Shared;

/// <summary>THE WARRIOR'S TWO 3rd-CLASS DISCIPLINES, 40-74 — every row of
/// <c>docs/data/classes_skills_csv/warrior 3rd.csv</c> (the RAVAGER) and
/// <c>docs/data/classes_skills_csv/war_aoe 3rd.csv</c> (the WARLORD), landed 2026-09-11.
///
/// <para>🔑 <b>TWO FILES, TWO DISCIPLINES, AND THE WEAPON IS THE WHOLE SPLIT.</b> The Ravager trains
/// a two-handed SWORD (<see cref="WarriorSwordMastery"/>) and keeps the Battle stances; the Warlord
/// trains a two-handed BLUNT (<see cref="WarriorBluntMastery"/>) whose basic attack CLEAVES, and has
/// no stances at all. Everything else — armour, Warrior's Strength, Final Stand, HP Boost, HP
/// Regeneration, Overpower, Battle Regeneration, Battle Resilience, Monster Knowledge — is shared,
/// rung for rung, because both files author it identically. Where the two disagree it is only the
/// LEARN LEVEL or the SP, which ride as per-class overrides on the class table.</para>
///
/// <para>🔴 <b>WHAT IS STILL MISSING, IN HIS OWN WORDS:</b> *"I made some passives and buffs for
/// warrior/aoe 3rd - they are missing only teir dmg and control (active dmg) skills."* So the
/// DAMAGE half of both kits is not here and was not authored. Until it is, the derived
/// <c>war_sundering_blow</c> in Skills.FighterKits3rd.cs stands in — it is the last survivor of the
/// `BL-185` recipe kit, and it goes the day his damage rows land. Do not invent one beside it.</para>
///
/// <para>⚠ <b>`war_sword_mastery` IS GONE AND `warrior_sword_mastery` IS ITS SUCCESSOR</b>, not a
/// second skill. Same class, same slot, same weapon — a rename plus a retune to his ladder, which is
/// the treatment the orphan `mana_barrier` got and the opposite of duplicating. The derived id was
/// five days old (0.116.0), lived only on these two disciplines, and his file authors fifteen rungs
/// where it had eight; leaving both would have paid a Ravager two two-handed sword masteries.</para>
/// </summary>
public static partial class SkillCatalog
{
    // The Ravager's sword line and the Warlord's blunt line. Both are named "Two-Hand Mastery" in his
    // files — the DISPLAY name is the same, the id and the payload are not.
    public const string WarriorSwordMastery = "warrior_sword_mastery";
    public const string WarriorBluntMastery = "warrior_blunt_mastery";
    /// <summary>The P.Atk twin of the tank's Final Defense — his row is that skill's sentence with
    /// "P.Def" swapped for "P.Atk" and no magic column. Its numbers live in
    /// <c>Entity.FinalStandBonus</c>, not here, because they are read LIVE off the HP bar.</summary>
    public const string WarriorFinalStand = "warrior_final_stand";
    public const string WarriorHpRegeneration = "warrior_hp_regeneration";

    // ---- HIS LADDER. Fifteen rungs at these levels, on the armour and both weapon masteries. ----
    internal static readonly int[] Warrior3rdLevels =
        { 40, 43, 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };

    /// <summary>SP per rung, his column (the header says `x1000`). Identical to the tank's band ladder
    /// except at rung 6, where he writes 80 and `tank 3rd.csv` writes 81. His file, his number.</summary>
    internal static readonly int[] Warrior3rdSp =
    {
        28_000, 35_000, 40_000, 50_000, 74_000, 80_000, 88_000, 120_000,
        170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
    };

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  ARMOUR — rungs 6-20 APPENDED to the 2nd-class ladder, never a new skill.
    //
    //  🔑 WHY APPEND. An armour mastery's payload rides ArmorMasteryLevels and the profile at the
    //     LEARNED rung supplies every weight at once. A second, separate mastery would either stack
    //     silently with the 2nd-class one or, with `Replaces`, delete the weights it does not
    //     re-state. This is the idiom the tank and the rogue already use.
    //  ⚠ APPEND ONLY. A rung inserted mid-ladder silently re-points every saved SkillLevel above it.
    //
    //  🔴 THESE REPLACE THE DERIVED `BL-185` RUNGS, which were the TANK's heavy profile copied wholesale
    //     ("the same heavy passive as tanks minus the crit dmg reduction") while his file did not exist.
    //     His own shape is quite different: no ×1.07 P.Def multiplier, no −2 evasion, no MP regen, and
    //     LIGHT armour keeps growing instead of being frozen at the level-36 rung.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>His fifteen rows, read column by column. All weights get the P.Def and the HP regen;
    /// LIGHT adds +9 evasion at every rung (a plateau he carried up from level 28 — authored, not a
    /// stall); HEAVY adds its own P.Def and max HP on top.</summary>
    private static readonly int[]   W3ArmorDef     = { 40, 45, 50, 55, 58, 60, 63, 70, 77, 85, 92, 100, 107, 115, 123 };
    private static readonly float[] W3ArmorHpReg   = { 1.7f, 1.7f, 2.1f, 2.1f, 2.6f, 2.6f, 2.7f, 2.7f, 2.7f, 2.7f, 2.7f, 3.4f, 3.4f, 3.4f, 4.0f };
    private static readonly int[]   W3ArmorHeavyDef = { 10, 12, 14, 16, 18, 20, 22, 24, 27, 30, 33, 36, 40, 45, 50 };
    private static readonly int[]   W3ArmorHeavyHp  = { 50, 50, 50, 50, 60, 60, 60, 70, 70, 80, 80, 90, 90, 100, 100 };

    internal static SkillLevel[] WarriorArmorMasteryThirdRungs() =>
        Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(SpCost: Warrior3rdSp[i],
            Description: $"+{W3ArmorDef[i]} P.Def and +{W3ArmorHpReg[i]:0.0} HP regen/s in light or heavy; "
                       + $"light armor +9 evasion; heavy armor a further +{W3ArmorHeavyDef[i]} P.Def "
                       + $"and +{W3ArmorHeavyHp[i]} max HP.")).ToArray();

    internal static ArmorMasteryProfile[] WarriorArmorMasteryThirdProfiles() =>
        Enumerable.Range(0, Warrior3rdLevels.Length).Select(i =>
            WarriorArmor(W3ArmorDef[i], lightEva: 9, hpRegen: W3ArmorHpReg[i],
                         heavyDef: W3ArmorHeavyDef[i], heavyHp: W3ArmorHeavyHp[i])).ToArray();

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE SKILLS
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The crit-damage column both weapon masteries share — his files author the SAME fifteen
    /// numbers for the sword and for the blunt. Only the flat P.Atk differs (the blunt trades twenty
    /// points of it for the cleave).</summary>
    private static readonly float[] W3MasteryCritDmg =
        { 145f, 172f, 202f, 235f, 272f, 312f, 355f, 385f, 416f, 449f, 481f, 515f, 548f, 582f, 615f };

    /// <summary>THE RAVAGER'S SWORD LADDER: a clean +7 flat P.Atk a rung, end to end.
    /// ✅ RUNG 8 IS 101, not the 91 his file first carried — `BL-200`, ruled 2026-09-11:
    /// *"Warrior sword mastery should be 94->101->108 ... Typo on both"*. It was the one place in
    /// either warrior file where a ladder went DOWN (a Ravager buying rung 8 at level 60 lost three
    /// points of attack he already had, for 120k SP), and `--check`'s LADDER DIP is what found it.
    /// The CSV cell moved with this line, in the same commit.</summary>
    private static readonly int[] W3SwordAtk =
        { 52, 59, 66, 73, 80, 87, 94, 101, 108, 115, 122, 129, 136, 143, 150 };

    /// <summary>THE WARLORD'S BLUNT LADDER: a clean +7 a rung, twenty points under the sword's at
    /// every step. That gap IS the price of the cleave.</summary>
    private static readonly int[] W3BluntAtk =
        { 32, 39, 46, 53, 60, 67, 74, 81, 88, 95, 102, 109, 116, 123, 130 };

    /// <summary>How many bodies one of the Warlord's basic swings may touch, the real target INCLUDED
    /// (his "max N targets"). Climbs 5 → 10 over the first six rungs and then PLATEAUS, which is
    /// authored: a plateau at the top of a ladder is a decision, not a gap (his 2026-08-26 rule).</summary>
    private static readonly int[] W3BluntCleave =
        { 5, 6, 7, 8, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };

    private static SkillDef[] Warrior3rdSkills() => new SkillDef[]
    {
        // ═══ TWO-HAND MASTERY, THE RAVAGER'S — a TWO-HANDED SWORD only ═══════════════════════════
        // ⚠ It does NOT carry `Replaces: [warrior_weapon_mastery]`, and the Warlord's blunt line
        //   below DOES. That asymmetry is his files': the Ravager's row has an empty REPLACES cell
        //   and the Warlord's names the 2nd-class mastery. It also makes mechanical sense — the
        //   2nd-class mastery is where the Ravager's own CLEAVE-free sword numbers come from at
        //   20-36, and its blunt half is inert in his hands anyway.
        new(WarriorSwordMastery, "Two-Hand Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. A TWO-HANDED SWORD strikes far harder in your hands, and far worse "
                       + "when it finds a gap. No effect one-handed, and none with any other weapon.",
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                SpCost: Warrior3rdSp[i],
                Description: $"Two-handed sword: +{W3SwordAtk[i]} P.Atk, +{W3MasteryCritDmg[i]:0} critical damage.")).ToArray(),
            WeaponMasteryLevels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Sword: new PassiveEffect(PhysAtk: W3SwordAtk[i], CritDamageFlat: W3MasteryCritDmg[i]),
                    RequiredWeapon: WeaponType.AnySword,
                    RequiredHands: WeaponHands.Two)).ToArray()),

        // ═══ TWO-HAND MASTERY, THE WARLORD'S — a TWO-HANDED BLUNT, and it CLEAVES ════════════════
        // 🔑 THIS IS WHAT THE DISCIPLINE IS. Twenty fewer points of P.Atk than the Ravager's sword at
        // every rung, bought back as a basic attack that hits up to TEN bodies within 150 of the one
        // he swung at. See PassiveEffect.CleaveTargets and GameLoopService.ResolveCleave — each extra
        // body takes a WHOLE swing (its own miss roll, crit, block and on-hit riders), not a share.
        // ⚠ `Replaces: [warrior_weapon_mastery]` is HIS cell, and it matters more here than usual:
        //   the 2nd-class mastery ALSO grants a blunt cleave (2-4 targets). Without the replace a
        //   Warlord would hold two cleaves — harmless as written, since Entity takes the LARGER of
        //   the two rather than summing, but two ladders for one mechanic is how they drift apart.
        new(WarriorBluntMastery, "Two-Hand Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { WarriorWeaponMastery },
            Description: "Passive. A TWO-HANDED BLUNT hits harder and crits worse — and every basic "
                       + "swing sweeps everything within 150 of your target. No effect one-handed, "
                       + "and none with any other weapon.",
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                SpCost: Warrior3rdSp[i],
                Description: $"Two-handed blunt: +{W3BluntAtk[i]} P.Atk, +{W3MasteryCritDmg[i]:0} critical "
                           + $"damage, basic attacks strike up to {W3BluntCleave[i]} targets within 150.")).ToArray(),
            WeaponMasteryLevels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Blunt: new PassiveEffect(PhysAtk: W3BluntAtk[i], CritDamageFlat: W3MasteryCritDmg[i],
                        CleaveTargets: W3BluntCleave[i], CleaveRadius: 150f),
                    RequiredWeapon: WeaponType.AnyBlunt,
                    RequiredHands: WeaponHands.Two)).ToArray()),

        // ═══ FINAL STAND — the passive that reads your own HP bar ════════════════════════════════
        // Three rungs at 40 / 52 / 60, both disciplines. Its numbers live in `Entity.FinalStandBonus`
        // for the same reason Final Defense's live in `FinalDefenceBonus`: HP moves every tick and
        // nothing recomputes derived stats when it does, so a buff would have needed a watcher on the
        // damage path, the heal path, the regen tick AND the potion path. A getter cannot be forgotten.
        new(WarriorFinalStand, "Final Stand", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 28_000,
            Description: "Passive. The worse it is going, the harder you swing: below 75%, below 50% "
                       + "and below 25% HP each raise your P.Atk.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 28_000,
                    Description: "Below 75% HP +5% P.Atk; below 50% +10%; below 25% +20%."),
                new SkillLevel(SpCost: 74_000,
                    Description: "Below 75% HP +7% P.Atk; below 50% +15%; below 25% +25%."),
                new SkillLevel(SpCost: 120_000,
                    Description: "Below 75% HP +10% P.Atk; below 50% +20%; below 25% +30%."),
            }),

        // ═══ HP REGENERATION — and the first passive in the game that pays for SITTING ═══════════
        // *"Increase Hp regen +1.4; When sitting Hp regen +1, Mp regen +2.0"*. Two channels: the
        // always-on HP regen (the flat `hpReg` column every other passive uses since `BL-92`) and a
        // sitting-only pair on top of it. See PassiveEffect.HpRegenSitting for why both are FLAT and
        // why they are added OUTSIDE the stance multiplier — sitting already pays ×1.5 on the formula
        // half, and folding his authored +2.0 inside would have silently made it +3.0.
        // ⚠ The MP half is the interesting one: a warrior has no other MP regen source of his own at
        //   all, so this is what lets him sit for ten seconds between pulls instead of thirty.
        new(WarriorHpRegeneration, "HP Regeneration", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 42_000,
            Description: "Passive. Your wounds close faster, and faster still when you sit down to "
                       + "rest — sitting also restores your MP.",
            Levels: new[]
            {
                // 🔑 THE SP HERE IS THE WARLORD'S (42k/65k on the first two rungs). The Ravager's file
                // prices the same two at 28k/50k, which rides as a ClassSkill.SpCost override on his
                // table rather than as a second SkillDef. Rungs 3-7 agree in both files.
                // ⚠ His level-66 cell reads "Hp regen +1,8" — a comma for a decimal point, which is the
                //   Bulgarian separator. 1.8 continues the +0.1 ladder exactly; there is no ambiguity.
                W3RegenRung(1.4f, sitHp: 1f, sitMp: 2.0f, sp: 42_000),
                W3RegenRung(1.5f, sitHp: 1f, sitMp: 2.0f, sp: 65_000),
                W3RegenRung(1.6f, sitHp: 1f, sitMp: 2.0f, sp: 80_000),
                W3RegenRung(1.7f, sitHp: 3f, sitMp: 2.5f, sp: 170_000),
                W3RegenRung(1.8f, sitHp: 3f, sitMp: 2.5f, sp: 280_000),
                W3RegenRung(1.9f, sitHp: 5f, sitMp: 3.0f, sp: 390_000),
                W3RegenRung(2.0f, sitHp: 5f, sitMp: 3.0f, sp: 880_000),
            }),
    };

    /// <summary>One rung of HP Regeneration. A helper because the sitting pair and the always-on flat
    /// must never be confused for each other — they are three numbers on one row of his file and two
    /// of them only pay while the character is on the ground.</summary>
    private static SkillLevel W3RegenRung(float hpReg, float sitHp, float sitMp, int sp) =>
        new SkillLevel(SpCost: sp,
            Passive: new PassiveEffect(HpRegen: hpReg, HpRegenSitting: sitHp, MpRegenSitting: sitMp),
            Description: $"+{hpReg:0.0} HP regen/s. While sitting, a further +{sitHp:0} HP/s and +{sitMp:0.0} MP/s.");
}
