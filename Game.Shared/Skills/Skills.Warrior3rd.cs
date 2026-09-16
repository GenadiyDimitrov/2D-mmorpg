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
            // ⚠ `BL-237` — THE 4th TIER CONTINUES THIS LADDER AND ONLY THIS ONE. `war_aoe 4th.csv` is
            //   still a placeholder, so the Warlord's blunt mastery below stops at 74.
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                SpCost: Warrior3rdSp[i],
                Description: $"Two-handed sword: +{W3SwordAtk[i]} P.Atk, +{W3MasteryCritDmg[i]:0} critical damage.")
                ).Concat(WarriorSwordMasteryFourthRungs()).ToArray(),
            WeaponMasteryLevels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i =>
                new WeaponMasteryProfile(
                    Sword: new PassiveEffect(PhysAtk: W3SwordAtk[i], CritDamageFlat: W3MasteryCritDmg[i]),
                    RequiredWeapon: WeaponType.AnySword,
                    RequiredHands: WeaponHands.Two))
                .Concat(WarriorSwordMasteryFourthProfiles()).ToArray()),

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
                       + "and below 25% HP each raise your P.Atk — and, deeper down, your accuracy.",

            Levels: new[]
            {
                // ✅ THE ACCURACY IS HIS, added to BOTH 3rd-tier files in the `BL-237` pass. It is a
                //    second live channel (Entity.FinalStandAccuracy), not a rider on the P.Atk one, and
                //    it starts a band later at every rung — so the first rung pays it only at 25%.
                new SkillLevel(SpCost: 28_000,
                    Description: "Below 75% HP +5% P.Atk; below 50% +10%; below 25% +20% and +2 accuracy."),
                new SkillLevel(SpCost: 74_000,
                    Description: "Below 75% HP +7% P.Atk; below 50% +15% and +2 accuracy; "
                               + "below 25% +25% and +4 accuracy."),
                new SkillLevel(SpCost: 120_000,
                    Description: "Below 75% HP +10% P.Atk and +2 accuracy; below 50% +20% and +4 accuracy; "
                               + "below 25% +30% and +8 accuracy."),
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

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE HUMAN RAVAGER'S FOCUS KIT (`BL-237`, `warrior 3rd.csv`, landed 2026-09-14) — HUMAN ONLY.
    //
    //  Two skills FILL a pool and three SPEND it. The pool is the self-buff of `warrior_focus`, and its
    //  stack count is the Focus count; see ChargeRule for the whole mechanic and why it is a buff.
    //  Every number below is his row, read column by column; nothing here is derived.
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    public const string WarriorFocus              = "warrior_focus";
    public const string WarriorFocusMastery       = "warrior_focus_mastery";
    public const string WarriorFocusedBlast       = "warrior_focused_blast";
    public const string WarriorFocusedDoubleSlash = "warrior_double_slash";
    public const string WarriorFocusedTripleSlash = "warrior_tripple_slash";   // his spelling, kept (ids are append-only)

    // Focus and Focus Mastery share one ladder: the same six levels and the same six caps.
    internal static readonly int[] WarriorFocusLevels = { 43, 49, 55, 62, 68, 74 };
    private static readonly int[] FocusCaps        = { 2, 3, 4, 6, 8, 10 };
    private static readonly int[] FocusSp          = { 17_000, 25_000, 40_000, 85_000, 160_000, 440_000 };
    private static readonly int[] FocusMasterySp   = { 18_000, 25_000, 40_000, 85_000, 160_000, 440_000 };

    internal static readonly int[] FocusedBlastLevels = { 40, 46, 52, 58, 62, 66, 70, 74 };
    private static readonly int[] FocusedBlastPower  = { 1000, 1600, 2100, 2700, 3300, 3900, 4400, 5000 };
    private static readonly int[] FocusedBlastMp     = { 36, 43, 47, 55, 58, 65, 70, 78 };
    private static readonly int[] FocusedBlastSp     = { 28_000, 40_000, 74_000, 88_000, 170_000, 280_000, 390_000, 880_000 };

    internal static readonly int[] FocusedDoubleLevels = { 46, 49, 52, 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
    private static readonly int[] FocusedDoublePower  = { 800, 950, 1050, 1200, 1350, 1500, 1650, 1800, 1950, 2050, 2200, 2350, 2500 };
    private static readonly int[] FocusedDoubleMp     = { 58, 60, 62, 65, 70, 71, 73, 77, 79, 81, 83, 85, 88 };
    private static readonly int[] FocusedDoubleSp     =
        { 40_000, 50_000, 74_000, 80_000, 88_000, 120_000, 170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    internal static readonly int[] FocusedTripleLevels = { 55, 58, 60, 62, 64, 66, 68, 70, 72, 74 };
    private static readonly int[] FocusedTriplePower  = { 1200, 1350, 1500, 1650, 1800, 1950, 2050, 2200, 2350, 2500 };
    private static readonly int[] FocusedTripleMp     = { 75, 80, 81, 83, 87, 89, 91, 93, 95, 98 };
    private static readonly int[] FocusedTripleSp     =
        { 80_000, 88_000, 120_000, 170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000 };

    private static SkillDef[] WarriorFocusSkills() => new SkillDef[]
    {
        // ═══ FOCUS — gather one charge ═══════════════════════════════════════════════════════════
        // *"Gather 'Focus' up to N, Cost 20 HP and 5 MP"*; comment: *"Each use increases
        // warrior_focus_count + 1; Adds it as self buff for 10 mins; After 10 mins all charges disapear;
        // Each use resets duration; Cannot be used if maximum is reached"*.
        // 🔑 `MaxStacks: 10` is the POOL's absolute ceiling (his "up to 10" at 74, and Focus Force's in
        //    the 4th file), NOT the rung's cap — the rung's cap is `Caps`. It is here so a relog restores
        //    a 10-charge pool instead of clamping it to 1 (RestorePersistedBuffs clamps to the def's).
        // ⚠ NOT CANCELLABLE: a Cancel strips blessings, and this is a resource the warrior built by
        //   swinging, not something anyone cast on him. My call — his row does not say.
        new(WarriorFocus, "Focus", BaseClass.Fighter, SkillEffect.None,
            MpCost: 5, CastTicks: 5, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: WarriorFocus, MaxStacks: 10, Cancellable: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two, HpCost: 20,
            Charge: new ChargeRule(WarriorFocus, Caps: FocusCaps, GatherPerUse: 1),
            Description: "Gather one charge of Focus, which your Focused strikes spend for extra power. "
                       + "The charges last 10 minutes and each use restarts the clock. Costs HP as well as MP.",
            Levels: Enumerable.Range(0, WarriorFocusLevels.Length).Select(i => new SkillLevel(
                MpCost: 5, SpCost: FocusSp[i],
                Description: $"Gather Focus, up to {FocusCaps[i]} charges. Costs 20 HP and 5 MP.")).ToArray()),

        // ═══ FOCUS MASTERY — basic swings gather it too ══════════════════════════════════════════
        // *"Chance to increase 'Focus' up to N per: Basic attack (15%), Critical attack (30%)"*.
        // 🔑 Read as ONE roll per landed swing: 30% when the swing crit, 15% when it did not. A miss
        //    gathers nothing. The weapon gate is the skill's own `RequiredWeapon`, the proc-passive idiom
        //    (Combo Mastery reads the same two fields).
        new(WarriorFocusMastery, "Focus Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            Charge: new ChargeRule(WarriorFocus, Caps: FocusCaps, OnBasicHit: 0.15f, OnBasicCrit: 0.30f),
            Description: "Passive. With a two-handed sword, your basic attacks sometimes gather Focus — "
                       + "twice as often when they crit.",
            Levels: Enumerable.Range(0, WarriorFocusLevels.Length).Select(i => new SkillLevel(
                SpCost: FocusMasterySp[i],
                Description: $"Basic attacks gather Focus up to {FocusCaps[i]}: 15% per hit, 30% per critical hit.")).ToArray()),

        // ═══ FOCUSED BLAST — the ranged one, spends up to 2 ══════════════════════════════════════
        // *"Deals Physical damage with +N power, Cannot be blocked, Can double, Consume 'Focus' to
        // increase power with 10% (Max 2)"*. Range 600 is his — a blast, not a melee strike, so the
        // RANGE-40 law does not reach it.
        FocusedStrike(WarriorFocusedBlast, "Focused Blast", FocusedBlastLevels, FocusedBlastPower,
            FocusedBlastMp, FocusedBlastSp, castTicks: 20, cooldownTicks: 30, range: 600, hits: 1,
            spendMax: 2, powerPerCharge: 0.10f,
            description: "Hurl a focused blow at a distant enemy. It cannot be blocked, and spends up to 2 Focus for more power.",
            power4: W4FocusedBlastPower, mp4: W4FocusedBlastMp, step4: 2),

        // ═══ FOCUSED DOUBLE SLASH — two hits, spends up to 3 ONCE ════════════════════════════════
        // Comment: *"consume warrior_focus once per skil use - not each slash"*. The bonus is taken
        // once, before the first slash, and both slashes swing with it. Cast 1.5 / reuse 3 per his
        // second-round ruling (it had been copied from the Triple).
        FocusedStrike(WarriorFocusedDoubleSlash, "Focused Double Slash", FocusedDoubleLevels, FocusedDoublePower,
            FocusedDoubleMp, FocusedDoubleSp, castTicks: 15, cooldownTicks: 30, range: 40, hits: 2,
            spendMax: 3, powerPerCharge: 0.15f,
            description: "Slash twice. Neither slash can be blocked, and the pair spends up to 3 Focus for more power.",
            power4: W4FocusedMultiPower, mp4: W4DoubleMp),

        // ═══ FOCUSED TRIPPLE SLASH — three hits, spends up to 4 ONCE ═════════════════════════════
        FocusedStrike(WarriorFocusedTripleSlash, "Focused Tripple Slash", FocusedTripleLevels, FocusedTriplePower,
            FocusedTripleMp, FocusedTripleSp, castTicks: 20, cooldownTicks: 50, range: 40, hits: 3,
            spendMax: 4, powerPerCharge: 0.15f,
            description: "Slash three times. No slash can be blocked, and the flurry spends up to 4 Focus for more power.",
            power4: W4FocusedMultiPower, mp4: W4HeavyMp),
    };

    /// <summary>One of the three Focused strikes. They differ only in their ladder, their timing, how
    /// many times they hit and how much Focus they may spend — the flags are identical in all three rows
    /// of his file (<c>Cannot be blocked, Can double</c>, two-handed sword).</summary>
    /// <remarks>🔑 "Cannot be blocked" is <c>BlockAccuracy: 1</c>: the block roll is
    /// <c>BlockChance − BlockAccuracy</c> clamped at 0, so no shield in the game can get above it.</remarks>
    /// <remarks>⚠ <paramref name="power4"/>/<paramref name="mp4"/> are the 4th tier's continuation of
    /// the SAME ladder (`warrior 4th.csv`, `BL-237`); <paramref name="step4"/> is 1 for the two that
    /// learn every level and 2 for the Blast, which learns every other. The numbers live in
    /// Skills.Warrior4th.cs.</remarks>
    private static SkillDef FocusedStrike(string id, string name, int[] levels, int[] power, int[] mp,
        int[] sp, int castTicks, int cooldownTicks, float range, int hits, int spendMax, float powerPerCharge,
        string description, int[] power4, int[] mp4, int step4 = 1)
    {
        string Rung(int p) =>
            (hits == 1 ? $"Power {p}" : $"Strikes {hits} times for power {p} each")
          + $". Cannot be blocked, can double. Spends up to {spendMax} Focus for "
          + $"+{powerPerCharge * 100f:0}% power each.";

        return new(id, name, BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: mp[0], CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: range, Power: power[0],
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f, HitCount: hits,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            Charge: new ChargeRule(WarriorFocus, SpendMax: spendMax, PowerPerCharge: powerPerCharge),
            Description: description,
            Levels: Enumerable.Range(0, levels.Length).Select(i => new SkillLevel(
                Power: power[i], MpCost: mp[i], SpCost: sp[i], Description: Rung(power[i])))
                .Concat(F4Rungs(power4.Length, step4, (i, s, gold) => new SkillLevel(
                    Power: power4[i], MpCost: mp4[i], SpCost: s, GoldCost: gold,
                    Description: Rung(power4[i]))))
                .ToArray());
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  `BL-237` — THE REST OF `warrior 3rd.csv`, AND EVERY LADDER IT SHARES WITH `warrior 4th.csv`.
    //
    //  🔑 THE RACE COLUMN IS THE SPLIT, and it splits the RAVAGER only. Charge is on both files and
    //     both disciplines with no race cell at all; Antidote is the Elf's in both. Everything below
    //     that carries a race carries it on the Ravager alone — `war_aoe 3rd.csv` authors none of it.
    //       • HUMAN — Champion Presence + Slash (P.Def rot) + the whole Focus kit above.
    //       • DEMON — Berserker Presence + Slash (P/M.Atk rot) + Battle Frenzy + Sword Shock + Demonic Smash.
    //       • ELF   — Saints Presence + Slash (speed rot) + Saints Sword Dance + Sword Blast + Antidote.
    //
    //  🔑 ONE POWER COLUMN, THREE SLASHES. All three race Slashes share the same fifteen power cells
    //     and the same MP ladder — only the DEBUFF differs. That is the whole race identity here, and
    //     it is why one helper builds all three.
    //
    //  🔑 ONE LADDER ACROSS BOTH TIERS. His 4th file continues every one of these columns rather than
    //     starting a new skill, so each def carries its 3rd-tier rungs and then its 4th-tier ones — the
    //     archer's `.Concat(ArcherFourth…Rungs())` shape. The 4th-tier NUMBERS live in
    //     Skills.Warrior4th.cs; only the concatenation is here.
    //
    //  ⚠ ALL THREE SLASHES LAND AT ×0.7 AND SWORD SHOCK AT ×1 — his ruling, and the numbers live in
    //    `docs/data/debuff_landmods.csv`, never in a comment on these rows (`BL-232`).
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    public const string WarriorCharge            = "warrior_charge";
    public const string WarriorChampionPresence  = "warrior_champion_presence";
    public const string WarriorBerserkerPresence = "warrior_berserker_presence";
    public const string WarriorSaintsPresence    = "warrior_saints_presence";
    public const string WarriorHumanSlash        = "warrior_human_slash";
    public const string WarriorDemonSlash        = "warrior_demon_slash";
    public const string WarriorElfSlash          = "warrior_elf_slash";
    public const string WarriorBattleFrenzy      = "warrior_battle_frenzy";
    public const string WarriorSwordShock        = "warrior_sword_shock";
    public const string WarriorDemonicSmash      = "warrior_demonic_smash";
    public const string WarriorSwordBlast        = "warrior_sword_blast";
    public const string WarriorSwordDance        = "warrior_sword_dance";
    /// <summary>ONE STROKE of Saints Sword Dance — the sub-skill the wrapper fires ten times. Never
    /// learned, never on a bar, no MP of its own. See <see cref="SkillDef.ChannelSkill"/>.</summary>
    public const string WarriorSwordDanceStroke  = "warrior_sword_dance_stroke";

    // ---- The MP column every fifteen-rung active in his 3rd file shares, read off the rows. ----
    private static readonly int[] W3ActiveMp =
        { 36, 40, 43, 45, 47, 50, 55, 56, 58, 62, 65, 68, 70, 75, 78 };

    // ---- THE SLASH LADDER: one power column, three debuff columns. ----
    private static readonly int[] W3SlashPower =
        { 750, 1000, 1250, 1500, 1750, 2000, 2250, 2500, 2750, 3000, 3250, 3500, 3750, 4000, 4250 };
    // Human: *"Decrease P.Def of Enemy with N%"* — climbs to 23 at rung 6 and PLATEAUS (authored),
    // then steps once more to 25 for the whole 4th tier.
    private static readonly float[] W3HumanSlashDef =
        { .10f, .13f, .16f, .19f, .21f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f, .23f };
    // Demon: *"Decrease P/M.Atk of Enemy with N%"* — one step, 5% to 10% at rung 6; 12% above 76.
    private static readonly float[] W3DemonSlashAtk =
        { .05f, .05f, .05f, .05f, .05f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f, .10f };
    // Elf: *"Decrease Attack/Cast/Move Speed of Enemy with N%"* — to 20 at rung 6, 23 above 76.
    private static readonly float[] W3ElfSlashSpeed =
        { .10f, .12f, .14f, .16f, .18f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f, .20f };

    // ---- THE DEMON'S TWO STRIKES. Demonic Smash is EXACTLY 3x Sword Shock on every 3rd-tier rung,
    //      which is what made a mis-typed 659 findable in the review; keep the columns side by side.
    private static readonly int[] W3SwordShockPower =
        { 500, 650, 800, 950, 1050, 1200, 1350, 1500, 1650, 1800, 1950, 2050, 2200, 2350, 2500 };
    private static readonly int[] W3DemonicSmashPower =
        { 1500, 1950, 2400, 2850, 3150, 3600, 4050, 4500, 4950, 5400, 5850, 6150, 6600, 7050, 7500 };
    // ...and the Elf's dance is the same column at 0.3x — ONE stroke of ten.
    private static readonly int[] W3SwordDancePower =
        { 150, 195, 240, 285, 315, 360, 405, 450, 495, 540, 585, 615, 660, 705, 750 };

    // ---- BATTLE FRENZY, the Demon's low-HP burn. Three rungs at 60/66/74, and his 4th file adds
    //      none — it stops here, like the Battle stances and Monster Knowledge.
    internal static readonly int[] W3FrenzyLevels = { 60, 66, 74 };
    private static readonly int[]   W3FrenzyMp        = { 40, 60, 80 };
    private static readonly int[]   W3FrenzySp        = { 120_000, 280_000, 880_000 };
    private static readonly float[] W3FrenzyCcResist  = { .40f, .60f, .80f };
    private static readonly float[] W3FrenzyCancelRes = { .20f, .30f, .40f };
    private static readonly int[]   W3FrenzySpeed     = { 10, 20, 30 };
    private static readonly int[]   W3FrenzyAcc       = { 2, 4, 6 };
    private static readonly float[] W3FrenzyAtkSpeed  = { .10f, .20f, .30f };
    private static readonly int[]   W3FrenzyCritRate  = { 30, 65, 100 };
    private static readonly float[] W3FrenzyCritDmg   = { .30f, .65f, 1.00f };
    private static readonly float[] W3FrenzyHealRecv  = { .60f, .70f, .80f };

    // ---- THE PRESENCES: 64 and 74 here, 76 and 85 in the 4th file — four rungs of one ladder. ----
    internal static readonly int[] W3PresenceLevels = { 64, 74 };
    internal static readonly int[] W4PresenceLevels = { 76, 85 };
    private static readonly int[]  PresenceMp = { 40, 60, 80, 100 };

    private static SkillDef[] Warrior3rdRaceSkills() => new SkillDef[]
    {
        // ═══ CHARGE — the gap-closer, both disciplines, one rung at 40 and one at 76 ═════════════
        // *"Charges to enemy"*, range 400, no damage cell at all. That is `SkillEffect.Blink` with a
        // TARGET, which lands the caster behind it — the same primitive Shadowstep and Phantom Jump
        // use; only those two carry a strike on top and this one does not.
        // 🔑 SWORD **OR** BLUNT, his `sword|blunt/2` cell and his ruling: *"400 (3rd) / 600 (4th) is
        //    its RANGE; usable with a 2h sword or blunt"*. `|` is OR and `/2` narrows both to two
        //    hands — so it is the one active in his file the WARLORD can also press.
        new(WarriorCharge, "Charge", BaseClass.Fighter, SkillEffect.Blink,
            MpCost: 40, CastTicks: 5, CooldownTicks: 30, Range: 400, Power: 0,
            Category: SkillCategory.Physical, SpCost: 28_000,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            Description: "Close the ground to an enemy in one stride. Requires a two-handed sword or blunt.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 40, SpCost: 28_000, Range: 400f,
                    Description: "Charges to an enemy up to 400 away."),
                // 76 — `warrior 4th.csv`: the same stride, 200 further, for 50 MP.
                new SkillLevel(MpCost: 50, SpCost: W4NewSp(76), GoldCost: W4NewGold(76), Range: 600f,
                    Description: "Charges to an enemy up to 600 away."),
            }),

        // ═══ THE THREE PRESENCES — one per race, ten minutes, five-second reuse ═══════════════════
        // 🔑 EACH IS ITS OWN KEY AND THEY NEVER MEET. A character has one race, so no Ravager can ever
        //    hold two of these; keys are separate because they are separate abilities, not a family.
        // *"Increase: Attack Speed with 5%, PVP Dmg with 5%"*. "PVP Dmg" is all THREE PvP channels,
        // exactly as Monster Knowledge's "PVE Dmg" is all three PvE ones — he names the CONTEXT.
        Presence(WarriorChampionPresence, "Champion Presence",
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffPvpSkillDamage
            | SkillEffect.BuffPvpMagicDamage | SkillEffect.BuffPvpBasicDamage,
            new[] { .05f, .10f, .15f, .20f }, new[] { .05f, .10f, .15f, .25f },
            (aspd, pvp) => new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtkSpeed, aspd),
                new(SkillEffect.BuffPvpSkillDamage, pvp),
                new(SkillEffect.BuffPvpMagicDamage, pvp),
                new(SkillEffect.BuffPvpBasicDamage, pvp),
            },
            "The bearing of a champion: you swing faster, and you hit PLAYERS harder.",
            (aspd, pvp) => $"+{aspd * 100f:0}% attack speed and +{pvp * 100f:0}% PvP damage for 10 minutes."),

        // *"Increase: P.Atk with 10%, Acc +3"* — a percentage and a FLAT, which is why the two
        // magnitudes carry different modes.
        Presence(WarriorBerserkerPresence, "Berserker Presence",
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAccuracy,
            new[] { .10f, .15f, .20f, .25f }, new[] { 3f, 5f, 5f, 7f },
            (atk, acc) => new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, atk),
                new(SkillEffect.BuffAccuracy, acc, ModifierMode.Flat),
            },
            "The bearing of a berserker: every blow lands harder and truer.",
            (atk, acc) => $"+{atk * 100f:0}% P.Atk and +{acc:0} accuracy for 10 minutes."),

        SaintsPresenceDef(),

        // ═══ THE THREE SLASHES — one strike, three rots ══════════════════════════════════════════
        Slash(WarriorHumanSlash, SkillEffect.DebuffDef, W3HumanSlashDef, W4HumanSlashDef,
            v => new EffectMagnitude[] { new(SkillEffect.DebuffDef, v) },
            "A slash that opens armour: the target's guard fails it for 15s.",
            v => $"cuts its P.Def by {v * 100f:0}%"),

        // ⚠ `DebuffAtk` IS BOTH CHANNELS in one flag — *"reduce the target's attack power (both
        //   channels)"* — which is exactly his *"P/M.Atk"*. One magnitude, not two.
        Slash(WarriorDemonSlash, SkillEffect.DebuffAtk, W3DemonSlashAtk, W4DemonSlashAtk,
            v => new EffectMagnitude[] { new(SkillEffect.DebuffAtk, v) },
            "A slash that unmakes: the target's blows and its spells both weaken for 15s.",
            v => $"cuts its P.Atk and M.Atk by {v * 100f:0}%"),

        // ⚠ THREE SPEEDS, THREE FLAGS. *"Attack/Cast/Move Speed"* — move speed is `Slow`, which is
        //   CONTROL and therefore boss-immune ON ITS OWN; here it rides a damage skill, so the strike
        //   and the other two rots still land on a boss. See SkillEffect.ControlCc.
        Slash(WarriorElfSlash,
            SkillEffect.Slow | SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            W3ElfSlashSpeed, W4ElfSlashSpeed,
            v => new EffectMagnitude[]
            {
                new(SkillEffect.Slow, v),
                new(SkillEffect.DebuffAtkSpeed, v),
                new(SkillEffect.DebuffCastSpeed, v),
            },
            "A slash that drags: the target swings, casts and walks slower for 15s.",
            v => $"cuts its attack, cast and move speed by {v * 100f:0}%"),

        // ═══ BATTLE FRENZY — the Demon's last stand, and the only self-buff with a real price ════
        //
        // 🔑 NINE CHANNELS ON ONE ROW, and eight of them are gifts. Resistance to debuffs, resistance
        //    to buff-removal, move speed, accuracy, attack speed, crit rate (FLAT — his *"by 30"*, not
        //    "with 30%"), crit damage, and half off every physical skill's MP. The ninth is the price.
        //
        // 🔑 *"Decrease received HP 60%"* IS THE HEALING YOU RECEIVE, cut by 60% — the anti-heal
        //    channel, read the only way that pairs with *"can be used when HP is less or equal to
        //    30%"*: you go berserk at a third of your bar and nobody can top you back up.
        //
        // 🔑 IT IS A **NEGATIVE** `HealReceivedPct`, NOT `SkillEffect.DebuffHealRecv`. The flag is in
        //    `AnyDebuff`, and `SkillMath.IsHostile` reads the flag mask — declaring it would have made
        //    a warrior's own war-cry HOSTILE and parked it in his DEBUFF row, un-dismissable, stripped
        //    by a cancel and dropped on relog (the `BL-228` lesson in reverse). The buff-side field
        //    already existed for the Demon's *"+30% healing received"* and multiplies the same
        //    `Entity.HealReceivedMod`; a minus sign is the whole difference. **A downside you chose is
        //    not a curse somebody cast on you.**
        //
        // ⚠ `CountsTowardBuffLimit: false`, like the two Battle stances and for their reason: a buff
        //   you may only press below 30% HP is an emergency, not a slot you plan around.
        new(WarriorBattleFrenzy, "Battle Frenzy", BaseClass.Fighter,
            SkillEffect.BuffMoveSpeed | SkillEffect.BuffAccuracy | SkillEffect.BuffAtkSpeed
            | SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffCancelResist,
            MpCost: W3FrenzyMp[0], CastTicks: 20, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 600, BuffKey: "warrior_battle_frenzy", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequireHpBelowFraction: 0.30f, SpCost: W3FrenzySp[0],
            PhysMpCostPct: 0.50f, BuffHealReceivedPct: -W3FrenzyHealRecv[0],
            Description: "A last stand: you fight faster, truer and far more savagely — and no healing "
                       + "reaches you while it lasts. Usable only at 30% HP or less.",
            Levels: Enumerable.Range(0, W3FrenzyLevels.Length).Select(i => new SkillLevel(
                MpCost: W3FrenzyMp[i], SpCost: W3FrenzySp[i],
                CcResistPhysical: W3FrenzyCcResist[i], CcResistMagical: W3FrenzyCcResist[i],
                PhysMpCostPct: 0.50f, HealReceivedPct: -W3FrenzyHealRecv[i],
                Magnitudes: new EffectMagnitude[]
                {
                    new(SkillEffect.BuffMoveSpeed, W3FrenzySpeed[i], ModifierMode.Flat),
                    new(SkillEffect.BuffAccuracy, W3FrenzyAcc[i], ModifierMode.Flat),
                    new(SkillEffect.BuffAtkSpeed, W3FrenzyAtkSpeed[i]),
                    new(SkillEffect.BuffCritRate, W3FrenzyCritRate[i], ModifierMode.Flat),
                    new(SkillEffect.BuffCritDamage, W3FrenzyCritDmg[i]),
                    new(SkillEffect.BuffCancelResist, W3FrenzyCancelRes[i]),
                },
                Description:
                    $"+{W3FrenzyCcResist[i] * 100f:0}% debuff resistance, +{W3FrenzyCancelRes[i] * 100f:0}% "
                  + $"buff-removal resistance, +{W3FrenzySpeed[i]} speed, +{W3FrenzyAcc[i]} accuracy, "
                  + $"+{W3FrenzyAtkSpeed[i] * 100f:0}% attack speed, +{W3FrenzyCritRate[i]} crit rate and "
                  + $"+{W3FrenzyCritDmg[i] * 100f:0}% crit damage for 60s, and physical skills cost half "
                  + $"MP — but healing you receive is cut by {W3FrenzyHealRecv[i] * 100f:0}%.")).ToArray()),

        // ═══ SWORD SHOCK — the Demon's stun ══════════════════════════════════════════════════════
        // ⚠ HIS DURR CELL READ 0 while the DESCR said *"Stuns for 5s"*. A zero-tick stun is not a
        //   skill; the cell moved to 5 in the same commit as this line, in BOTH tiers. Same shape as
        //   the Human archer's Magic Arrow, whose own cell has always read 5.
        WarriorStrike(WarriorSwordShock, "Sword Shock", SkillEffect.Stun,
            W3SwordShockPower, W4SwordShockPower,
            castTicks: 10, cooldownTicks: 50, range: 40f, durationTicks: 50,
            _ => Array.Empty<EffectMagnitude>(),
            "A blow to the head: the target drops where it stands.",
            _ => "and stuns it for 5s", mp4: W4SlashMp),

        // ═══ DEMONIC SMASH — the Demon's big one. No rider, 3x Sword Shock's power ════════════════
        WarriorStrike(WarriorDemonicSmash, "Demonic Smash", SkillEffect.None,
            W3DemonicSmashPower, W4DemonicSmashPower,
            castTicks: 20, cooldownTicks: 50, range: 40f, durationTicks: 0,
            _ => Array.Empty<EffectMagnitude>(),
            "Everything you have, in one swing.",
            _ => "", mp4: W4HeavyMp),

        // ═══ SWORD BLAST — the Elf's reach ═══════════════════════════════════════════════════════
        // 🔑 IT IS THE FOCUSED BLAST'S LADDER EXACTLY — same eight levels, same powers, same MP, same
        //    SP, same 600 range, in both tiers. The Human pays for that reach in Focus; the Elf just
        //    swings.
        // ⚠ Range 600, so the RANGE-40 melee law does not reach it (same as Focused Blast).
        WarriorStrike(WarriorSwordBlast, "Sword Blast", SkillEffect.None,
            FocusedBlastPower, W4FocusedBlastPower,
            castTicks: 20, cooldownTicks: 30, range: 600f, durationTicks: 0,
            _ => Array.Empty<EffectMagnitude>(),
            "A stroke thrown far further than a sword should reach.",
            _ => "", mp: FocusedBlastMp, sp: FocusedBlastSp,
            mp4: W4FocusedBlastMp, step4: 2),

        // ═══ SAINTS SWORD DANCE — the Elf's channel ══════════════════════════════════════════════
        //
        // *"Deals Physical damage with +150 power 10 times over 2s"*, AOE 150, `target/aoe`. That is a
        // CHANNEL WRAPPER, the shape he chose for Arrow Barrage and for the same reason: each stroke is
        // a REAL skill execution with its own miss, crit and splash, so nothing is special-cased.
        // 🔑 THE LADDER LIVES ON THE WRAPPER and the stroke authors Power 0 — a channel's shots resolve
        //    at the wrapper's level and take its power (Entity.ChannelPower), which is Twin Arrows'
        //    shape rather than Arrow Barrage's single-rung one.
        // ⚠ DURATION 2s IS THE DANCE, not a buff: ten strokes at one every 0.2s, his DURR cell.
        new(WarriorSwordDance, "Saints Sword Dance", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: W3ActiveMp[0], CastTicks: 5, CooldownTicks: 50, Range: 40, Power: W3SwordDancePower[0],
            DurationTicks: 20,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            // ⚠ THE WRAPPER CARRIES THE RADIUS TOO, and it is not a duplicate of the stroke's: a
            //   wrapper resolves nothing, so this never sweeps anything (the offensive sweep sits
            //   BELOW the channel's early return). What it does is draw the ring at cast time and tell
            //   `Retarget.FromDef` the skill is an AREA one — his `target/aoe` cell, ruled twice.
            AreaRadius: 150f, AreaAtTarget: true,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            ChannelSkill: WarriorSwordDanceStroke, ChannelShots: 10, ChannelIntervalTicks: 2,
            SpCost: Warrior3rdSp[0],
            Description: "Two seconds of blade: ten strokes, each catching everything around your target.",
            Levels: Enumerable.Range(0, Warrior3rdLevels.Length).Select(i => new SkillLevel(
                Power: W3SwordDancePower[i], MpCost: W3ActiveMp[i], SpCost: Warrior3rdSp[i],
                Description: SwordDanceRungText(W3SwordDancePower[i])))
                .Concat(F4Rungs(W4SwordDancePower.Length, 1, (i, sp, gold) => new SkillLevel(
                    Power: W4SwordDancePower[i], MpCost: W4HeavyMp[i], SpCost: sp, GoldCost: gold,
                    Description: SwordDanceRungText(W4SwordDancePower[i])))).ToArray()),

        // ONE STROKE. No MP (the wrapper charges once), no power (the wrapper's rung supplies it),
        // never learned. The AoE lives HERE because it is the stroke that splashes, not the wrapper.
        new(WarriorSwordDanceStroke, "Saints Sword Dance", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 40, Power: 0,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            AreaRadius: 150f, AreaAtTarget: true, TargetMode: TargetMode.EnemiesInRadius,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            Description: "One stroke of a sword dance."),
    };

    private static string SwordDanceRungText(int power) =>
        $"Ten strokes over 2s, each for power {power:N0} on everything within 150 of the target. "
      + "Cannot be blocked, can double.";

    /// <summary>One of the two two-channel Presences: a ten-minute self-buff on a five-second reuse.
    /// TWO rungs in the 3rd tier (64, 74) and TWO more in the 4th (76, 85) — one ladder, four rungs,
    /// because a presence is one ability and his 4th file simply continues its column.</summary>
    private static SkillDef Presence(string id, string name, SkillEffect effect,
                                     float[] a, float[] b,
                                     Func<float, float, EffectMagnitude[]> mags, string blurb,
                                     Func<float, float, string> rung) =>
        new(id, name, BaseClass.Fighter, effect,
            MpCost: PresenceMp[0], CastTicks: 10, CooldownTicks: 50, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: id, Rank: 1,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            SpCost: PresenceSp(0),
            Magnitudes: mags(a[0], b[0]),
            Description: blurb,
            Levels: Enumerable.Range(0, a.Length).Select(i => new SkillLevel(
                MpCost: PresenceMp[i], SpCost: PresenceSp(i), GoldCost: PresenceGold(i),
                Magnitudes: mags(a[i], b[i]),
                Description: rung(a[i], b[i]))).ToArray());

    /// <summary>Saints Presence on its own, because it is the one Presence carrying THREE channels and
    /// the helper above takes two.</summary>
    private static SkillDef SaintsPresenceDef()
    {
        // *"Increase: P.Crit.Rate with 5%, P.Crit.Dmg with 10%, Speed +3"*. The crit RATE is a
        // percentage OF your own rate (the multiplicative channel), the crit DAMAGE a fraction added to
        // the multiplier, and the speed a flat move-speed point — three modes, his three notations.
        // ⚠ Speed is MOVE speed here; his attack-speed rows always say "Atk.Speed".
        float[] rate  = { .05f, .10f, .15f, .20f };
        float[] dmg   = { .10f, .15f, .20f, .25f };
        int[]   speed = { 3, 5, 5, 7 };
        EffectMagnitude[] Mags(int i) => new EffectMagnitude[]
        {
            new(SkillEffect.BuffCritRate, rate[i]),
            new(SkillEffect.BuffCritDamage, dmg[i]),
            new(SkillEffect.BuffMoveSpeed, speed[i], ModifierMode.Flat),
        };
        return new(WarriorSaintsPresence, "Saints Presence", BaseClass.Fighter,
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMoveSpeed,
            MpCost: PresenceMp[0], CastTicks: 10, CooldownTicks: 50, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: WarriorSaintsPresence, Rank: 1,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            SpCost: PresenceSp(0),
            Magnitudes: Mags(0),
            Description: "The bearing of a saint: you find the gap more often, open it wider, and move like it.",
            Levels: Enumerable.Range(0, rate.Length).Select(i => new SkillLevel(
                MpCost: PresenceMp[i], SpCost: PresenceSp(i), GoldCost: PresenceGold(i),
                Magnitudes: Mags(i),
                Description: $"+{rate[i] * 100f:0}% crit rate, +{dmg[i] * 100f:0}% crit damage and "
                           + $"+{speed[i]} speed for 10 minutes.")).ToArray());
    }

    /// <summary>One of the three race Slashes — fifteen 3rd-tier rungs and fifteen 4th-tier ones of one
    /// power column with one rot on top. <c>Physical/Debuf</c>, 15s, cast 1 / reuse 3, range 40,
    /// two-handed sword, cannot be blocked, can double. Landing is the ATK-vs-CON contest at the ×0.7
    /// his landmod file carries.</summary>
    private static SkillDef Slash(string id, SkillEffect rider, float[] third, float[] fourth,
                                  Func<float, EffectMagnitude[]> mags,
                                  string blurb, Func<float, string> what) =>
        WarriorStrike(id, "Slash", rider, W3SlashPower, W4SlashPower,
            castTicks: 10, cooldownTicks: 30, range: 40f, durationTicks: 150,
            i => mags(i < third.Length ? third[i] : fourth[i - third.Length]), blurb,
            i => what(i < third.Length ? third[i] : fourth[i - third.Length]) + " for 15s",
            mp4: W4SlashMp, landMod: 0.7f, replaces: new[] { Smash });

    /// <summary>ONE OF THE WARRIOR'S AUTHORED STRIKES, both tiers on one ladder. Every one of them is
    /// the same row with different numbers: physical damage on a two-handed sword, *"Cannot be blocked,
    /// Can double"*, a power column on his shared MP/SP ladders, and optionally a rider contested on CON.
    ///
    /// <remarks>🔑 "Cannot be blocked" is <c>BlockAccuracy: 1</c> — the block roll is
    /// <c>BlockChance − BlockAccuracy</c> clamped at 0, so no shield can get above it. The same reading
    /// the Focused strikes use.
    /// ⚠ A rider means <c>DebuffSchool.Physical</c>: the rot is CONTESTED (ATK vs CON), not automatic.
    /// A rider-less strike declares no school, so nothing about it can fail but the miss roll.
    /// ⚠ THE INDEX <paramref name="mags"/> AND <paramref name="what"/> RECEIVE IS THE RUNG ACROSS BOTH
    /// TIERS — rung 16 is the 4th tier's first. Deliberate: a Slash is one ladder of thirty.</remarks></summary>
    private static SkillDef WarriorStrike(string id, string name, SkillEffect rider,
                                          int[] power3, int[] power4,
                                          int castTicks, int cooldownTicks, float range, int durationTicks,
                                          Func<int, EffectMagnitude[]> mags, string blurb,
                                          Func<int, string> what,
                                          int[]? mp = null, int[]? sp = null,
                                          int[]? mp4 = null, int step4 = 1, float landMod = 1f,
                                          string[]? replaces = null)
    {
        mp ??= W3ActiveMp;
        sp ??= Warrior3rdSp;
        mp4 ??= W4SlashMp;
        string Rung(int i, int p) =>
            $"Strikes for power {p:N0}{(what(i).Length > 0 ? " " + what(i) : "")}. Cannot be blocked, can double.";

        return new(id, name, BaseClass.Fighter, SkillEffect.PhysicalDamage | rider,
            MpCost: mp[0], CastTicks: castTicks, CooldownTicks: cooldownTicks, Range: range, Power: power3[0],
            DurationTicks: durationTicks, BuffKey: rider == SkillEffect.None ? null : id,
            Rank: rider == SkillEffect.None ? 0 : 1,
            DebuffSchool: rider == SkillEffect.None ? DebuffSchool.None : DebuffSchool.Physical,
            // 🔑 HIS LANDING MODIFIER, and it comes from `docs/data/debuff_landmods.csv` and nowhere
            //    else (`BL-232`): the three Slashes ×0.7, Sword Shock ×1. A damage skill that also
            //    curses lands less often than a bare curse — *"dmg + debuff should have lower chance
            //    than a solo debuff"* — and a bare stun is priced at par against that rule.
            DebuffLandMod: landMod, Replaces: replaces,
            Category: SkillCategory.Physical, CanDouble: true, BlockAccuracy: 1f,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            SpCost: sp[0],
            Magnitudes: mags(0),
            Description: blurb,
            Levels: Enumerable.Range(0, power3.Length).Select(i => new SkillLevel(
                Power: power3[i], MpCost: mp[i], SpCost: sp[i], Magnitudes: mags(i),
                Description: Rung(i, power3[i])))
                .Concat(F4Rungs(power4.Length, step4, (i, s, gold) => new SkillLevel(
                    Power: power4[i], MpCost: mp4[i], SpCost: s, GoldCost: gold,
                    Magnitudes: mags(power3.Length + i),
                    Description: Rung(power3.Length + i, power4[i]))))
                .ToArray());
    }

    /// <summary>One rung of HP Regeneration. A helper because the sitting pair and the always-on flat
    /// must never be confused for each other — they are three numbers on one row of his file and two
    /// of them only pay while the character is on the ground.</summary>
    private static SkillLevel W3RegenRung(float hpReg, float sitHp, float sitMp, int sp) =>
        new SkillLevel(SpCost: sp,
            Passive: new PassiveEffect(HpRegen: hpReg, HpRegenSitting: sitHp, MpRegenSitting: sitMp),
            Description: $"+{hpReg:0.0} HP regen/s. While sitting, a further +{sitHp:0} HP/s and +{sitMp:0.0} MP/s.");
}
