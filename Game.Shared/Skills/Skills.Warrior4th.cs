using System;
using System.Linq;

namespace Game.Shared;

/// <summary>THE WARRIOR'S 4th CLASS, 76-90 — every row of
/// <c>docs/data/classes_skills_csv/warrior 4th.csv</c> (`BL-237`, landed 2026-09-14, built 2026-09-16).
///
/// <para>🔑 <b>IT IS A CONTINUATION, NOT A KIT.</b> Twelve of its sixteen families are the SAME skill
/// ids his 3rd file authors, with fifteen more rungs bolted on the same ladders — so the numbers here
/// are arrays and the <c>SkillDef</c>s stay in Skills.Warrior3rd.cs, concatenated through
/// <see cref="F4Rungs"/> exactly as the archer's and the tank's 4th tiers do. Only FOUR skills are new,
/// and all four are one-rung race tools at 78: <b>Focus Force</b> and <b>Focus Limit</b> (Human),
/// <b>Parry</b> (Demon) and <b>Saints Blessing</b> (Elf).</para>
///
/// <para>🔑 <b>THE PRICE LADDER IS THE SHARED ONE.</b> Every SP/gold cell in his file is <see cref="F4"/>
/// to the coin — 6.5kk/11kk/16kk/80kk of SP over 76-79 and then gold alone, 5kk climbing to 100kk — and
/// the three one-rung 78 skills are <see cref="F4New"/>(78) = 16kk + 1kk. Nothing here is a new economy;
/// it is the same ascension ladder the healer's file established.</para>
///
/// <para>⚠ <b>THE WARLORD GETS NONE OF THIS.</b> `war_aoe 4th.csv` is still a two-line placeholder, so
/// the blunt discipline stops at 74 — and that is his file, not an omission here. The ONE skill both
/// disciplines share from this file is Charge's second rung, which is registered on both.</para>
///
/// <para>⚠ <b>WHAT HIS 4th FILE DELIBERATELY DOES NOT CONTINUE</b>, and it is a long list: Final Stand,
/// HP Boost, HP Regeneration, both Battle stances, Battle Resilience, Battle Regeneration, Monster
/// Knowledge, Warrior's Strength, Focus, Focus Mastery, Battle Frenzy and Antidote all stop at their
/// 74 rungs. Recorded in `BL-237` as *"not a question"* — do not extend one by analogy.</para>
/// </summary>
public static partial class SkillCatalog
{
    public const string WarriorFocusForce     = "warrior_focus_ranged";
    public const string WarriorFocusLimit     = "warrior_focus_max";
    public const string WarriorParry          = "warrior_parry";
    public const string WarriorSaintsBlessing = "warrior_saints_blessing";

    /// <summary>76 → 90, one rung a level. The band every fifteen-rung family in his file uses.</summary>
    internal static readonly int[] Warrior4thLevels =
        { 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90 };

    /// <summary>76 → 90 every OTHER level — Focused Blast's and Sword Blast's column.</summary>
    internal static readonly int[] Warrior4thEven = { 76, 78, 80, 82, 84, 86, 88, 90 };

    // ---- The two MP columns his fifteen-rung actives share. The LIGHT one is the Slashes', Sword
    //      Shock's and the Focused Blast's neighbours; the HEAVY one is Demonic Smash's, the Triple
    //      Slash's and the Sword Dance's. (The Double Slash sits between them and has its own.)
    internal static readonly int[] W4SlashMp =
        { 80, 82, 84, 86, 88, 90, 92, 95, 98, 100, 102, 104, 106, 108, 110 };
    internal static readonly int[] W4HeavyMp =
        { 100, 102, 104, 106, 108, 110, 112, 115, 118, 120, 122, 124, 126, 128, 130 };
    internal static readonly int[] W4DoubleMp =
        { 90, 92, 94, 96, 98, 100, 102, 105, 108, 110, 112, 114, 116, 118, 120 };
    internal static readonly int[] W4FocusedBlastMp = { 80, 84, 88, 92, 98, 102, 106, 110 };

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING POWER COLUMNS. Each is read straight off his rows; nothing here is derived.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>The Slash power column — all three races share it, as they do at the 3rd tier.
    /// ⚠ Its stride WIDENS at 81 (+100 a rung to 80, then +150, and +200 over the last two). His
    /// column; a stride that changes is not a typo, a ladder that goes DOWN is.</summary>
    internal static readonly int[] W4SlashPower =
        { 4500, 4600, 4700, 4800, 4900, 5050, 5200, 5350, 5500, 5650, 5800, 5950, 6100, 6300, 6500 };
    /// <summary>...and the three rots, each FLAT across the whole tier: one last step up from the
    /// 3rd tier's plateau and then nothing.</summary>
    internal static readonly float[] W4HumanSlashDef  = Flat15(.25f);
    internal static readonly float[] W4DemonSlashAtk  = Flat15(.12f);
    internal static readonly float[] W4ElfSlashSpeed  = Flat15(.23f);

    internal static readonly int[] W4SwordShockPower =
        { 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900, 4000 };
    /// <summary>🔑 DEMONIC SMASH IS STILL EXACTLY 3× SWORD SHOCK, rung for rung, all fifteen of them
    /// (+300 a rung against the Shock's +100). That relationship held across the whole 3rd tier too, and
    /// it is the cheapest check there is on this pair: a cell off the multiple is a typo. It is how the
    /// mis-typed 659 was found in the review.</summary>
    internal static readonly int[] W4DemonicSmashPower =
        { 7800, 8100, 8400, 8700, 9000, 9300, 9600, 9900, 10200, 10500, 10800, 11100, 11400, 11700, 12000 };
    internal static readonly int[] W4SwordDancePower =
        { 780, 810, 840, 870, 900, 930, 960, 990, 1020, 1050, 1080, 1110, 1140, 1170, 1200 };
    internal static readonly int[] W4FocusedBlastPower =
        { 5200, 5600, 6000, 6400, 6800, 7200, 7600, 8000 };
    /// <summary>The Double and the Triple share ONE power column at the 4th tier — 2600 → 4000, +100 a
    /// rung. They differ only in how many times it lands and what they may spend.</summary>
    internal static readonly int[] W4FocusedMultiPower =
        { 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300, 3400, 3500, 3600, 3700, 3800, 3900, 4000 };

    private static float[] Flat15(float v) => Enumerable.Repeat(v, 15).ToArray();

    // ---- The Presence ladder's prices, four rungs across two tiers (64, 74 | 76, 85). ----
    private static int PresenceSp(int rung) => rung switch
    {
        0 => 190_000, 1 => 880_000, 2 => F4(0).Sp, _ => F4(9).Sp,
    };
    private static int PresenceGold(int rung) => rung switch
    {
        0 => 0, 1 => 0, 2 => F4(0).Gold, _ => F4(9).Gold,
    };

    /// <summary>What a skill first LEARNED at <paramref name="level"/> costs in this tier — the "New
    /// Skills" column. A thin name over <see cref="F4New"/> so the warrior files read the same as the
    /// healer's.</summary>
    private static int W4NewSp(int level) => F4New(level).Sp;
    private static int W4NewGold(int level) => F4New(level).Gold;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING RUNGS — each returns ONLY the 4th-tier half; the def in Skills.Warrior3rd.cs
    //  (or Skills.Masteries.cs, for the armour) concatenates it.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>ARMOR MASTERY rungs 21-35. P.Def 124 → 155 (+2 a rung, widening to +3 at the top),
    /// HP regen FLAT at 4.0, light evasion 10 → 12, heavy a further 60 → 80 P.Def and 200 → 300 HP.
    /// <para>⚠ His level-87 cell reads 146 where the +2 stride wants 147 — left exactly as authored,
    /// because the ladder still RISES (145 → 146 → 149) and only a DIP is a defect. Listed in `BL-237`
    /// as cosmetic and untouched.</para></summary>
    private static readonly int[] W4ArmorDef =
        { 124, 126, 128, 130, 132, 134, 136, 138, 140, 142, 145, 146, 149, 152, 155 };
    private static readonly int[] W4ArmorLightEva =
        { 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 12 };
    private static readonly int[] W4ArmorHeavyDef =
        { 60, 60, 60, 60, 70, 70, 70, 70, 70, 80, 80, 80, 80, 80, 80 };
    private static readonly int[] W4ArmorHeavyHp =
        { 200, 200, 200, 200, 250, 250, 250, 250, 250, 300, 300, 300, 300, 300, 300 };
    private const float W4ArmorHpReg = 4.0f;

    internal static SkillLevel[] WarriorArmorMasteryFourthRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"+{W4ArmorDef[i]} P.Def and +{W4ArmorHpReg:0.0} HP regen/s in light or heavy; "
                       + $"light armor +{W4ArmorLightEva[i]} evasion; heavy armor a further "
                       + $"+{W4ArmorHeavyDef[i]} P.Def and +{W4ArmorHeavyHp[i]} max HP."));

    internal static ArmorMasteryProfile[] WarriorArmorMasteryFourthProfiles() =>
        Enumerable.Range(0, Warrior4thLevels.Length).Select(i =>
            WarriorArmor(W4ArmorDef[i], lightEva: W4ArmorLightEva[i], hpRegen: W4ArmorHpReg,
                         heavyDef: W4ArmorHeavyDef[i], heavyHp: W4ArmorHeavyHp[i])).ToArray();

    /// <summary>TWO-HAND MASTERY (sword) rungs 16-30. Crit damage 632 → 860, P.Atk 153 → 200.
    /// <para>✅ 678 at level 79 is HIS RULING, not the +17 stride's 683 — *"Two-Hand Mastery 4th:
    /// 666 → 678 → 690 (+12 on both steps, so 80 onward is unchanged)"*. The stride resumes at 81.</para>
    /// <para>⚠ THE WARLORD HAS NO 4th-TIER BLUNT LADDER — `war_aoe 4th.csv` is a placeholder. This is
    /// the Ravager's alone, and it is why the sword and blunt masteries are separate ids.</para></summary>
    private static readonly int[] W4SwordCritDmg =
        { 632, 649, 666, 678, 690, 707, 724, 741, 758, 775, 792, 809, 826, 843, 860 };
    private static readonly int[] W4SwordAtk =
        { 153, 156, 159, 162, 165, 168, 171, 174, 177, 180, 184, 188, 192, 196, 200 };

    internal static SkillLevel[] WarriorSwordMasteryFourthRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"Two-handed sword: +{W4SwordAtk[i]} P.Atk, +{W4SwordCritDmg[i]} critical damage."));

    internal static WeaponMasteryProfile[] WarriorSwordMasteryFourthProfiles() =>
        Enumerable.Range(0, Warrior4thLevels.Length).Select(i => new WeaponMasteryProfile(
            Sword: new PassiveEffect(PhysAtk: W4SwordAtk[i], CritDamageFlat: W4SwordCritDmg[i]),
            RequiredWeapon: WeaponType.AnySword,
            RequiredHands: WeaponHands.Two)).ToArray();

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE FOUR NEW SKILLS — all at 78, all one rung, all race-gated.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Warrior4thSkills() => new SkillDef[]
    {
        // ═══ FOCUS FORCE (Human) — a strike that FILLS the pool ═══════════════════════════════════
        //
        // *"Deals Physical damage with +500 power and gather 'Focus' up to 10, Cost 50 HP and 5 MP"*,
        // range 600, cast 1, NO REUSE AT ALL.
        //
        // 🔑 HIS RULING ON WHAT IT IS: *"Focus Force is IG's normal 'power attack' as a physical skill
        //    that can double; we have no skill crits, so it carries +500 power and gathers Focus."*
        //    So it is a filler the Human presses between the Focused strikes — cheap in MP, dear in HP,
        //    and the HP is the whole cost of a pool this class spends freely.
        //
        // ⚠ THE ONLY GATHERER IN THE GAME THAT ALSO DAMAGES, and that is why `IsChargePoolFull` refuses
        //   to gate a damaging skill: a full pool must not refuse an ATTACK. See GameLoopService — the
        //   gather runs AFTER the damage arm, and adds nothing once the pool is at 10.
        new(WarriorFocusForce, "Focus Force", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 5, CastTicks: 10, CooldownTicks: 0, Range: 600, Power: 500,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            HpCost: 50, SpCost: W4NewSp(78),
            Charge: new ChargeRule(WarriorFocus, Caps: new[] { 10 }, GatherPerUse: 1),
            Description: "A thrown blow that costs blood, not mana — and every one of them gathers Focus.",
            Levels: new[]
            {
                new SkillLevel(Power: 500, MpCost: 5, SpCost: W4NewSp(78), GoldCost: W4NewGold(78),
                    Description: "Strikes for power 500 and gathers 1 Focus, up to 10. Costs 50 HP and 5 MP."),
            }),

        // ═══ FOCUS LIMIT (Human) — fill the pool outright ════════════════════════════════════════
        //
        // *"Immediately set 'Focus' to Max, Cost 80 HP and 20 MP"*, `self/single` (his second-round
        // ruling — it affects the caster only), 90-second reuse.
        //
        // 🔑 IT NEEDS NO NEW MECHANIC: a gather of TEN against a cap of ten fills the pool from any
        //    state, because GatherCharge clamps to the cap. "Set to max" and "add ten, capped at ten"
        //    are the same instruction, and the second one is already built and already tested.
        // ⚠ A gatherer with no damage of its own, so it IS refused at a full pool — 80 HP for nothing
        //   would be the worst button in the game.
        new(WarriorFocusLimit, "Focus Limit", BaseClass.Fighter, SkillEffect.None,
            MpCost: 20, CastTicks: 10, CooldownTicks: 900, Range: 0, Power: 0,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            HpCost: 80, SpCost: W4NewSp(78),
            Charge: new ChargeRule(WarriorFocus, Caps: new[] { 10 }, GatherPerUse: 10),
            Description: "Fill your Focus to the brim in one breath. Costs a great deal of blood.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 20, SpCost: W4NewSp(78), GoldCost: W4NewGold(78),
                    Description: "Sets Focus to its maximum of 10. Costs 80 HP and 20 MP."),
            }),

        // ═══ PARRY (Demon) — the defensive stance ════════════════════════════════════════════════
        //
        // *"Increase P/M.Def with 25%; Decrease move/attack.speed with 10%"*, a TOGGLE at 10 MP/s.
        // Four magnitudes, two of them NEGATIVE — the self-buff-with-a-downside idiom (Defensive Wall,
        // Combat Stance), which is exactly why it is not hostile and sits in the buff row.
        new(WarriorParry, "Parry", BaseClass.Fighter,
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffMoveSpeed
            | SkillEffect.BuffAtkSpeed,
            MpCost: 10, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: "warrior_parry", Rank: 1, MpPerSecond: 10,
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            CountsTowardBuffLimit: false, SpCost: W4NewSp(78),
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 0.25f),
                new(SkillEffect.BuffMagicDef, 0.25f),
                new(SkillEffect.BuffMoveSpeed, -0.10f),
                new(SkillEffect.BuffAtkSpeed, -0.10f),
            },
            Description: "Stance. Fight from behind the blade: +25% P.Def and M.Def for 10% of your "
                       + "movement and your swing, at 10 MP a second.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 10, SpCost: W4NewSp(78), GoldCost: W4NewGold(78),
                    Description: "+25% P.Def and M.Def, −10% move and attack speed, 10 MP/s."),
            }),

        // ═══ SAINTS BLESSING (Elf) — the reflecting stance ═══════════════════════════════════════
        //
        // *"Reflect 30% of normal basic attacks, 15% to reflect debuff and 10% to reflect Physical
        // Damage skill; Decrease move/attack.speed with 10%"*, a TOGGLE at 10 MP/s.
        //
        // 🔑 THREE SEPARATE CHANNELS AND HIS THREE NUMBERS MEAN DIFFERENT THINGS — read the nouns:
        //    • *"Reflect 30% OF normal basic attacks"* — a FRACTION of the damage, every time
        //      (`SkillEffect.BuffReflect`, the armour sets' channel).
        //    • *"15% TO reflect debuff"* — a CHANCE the debuff lands on its caster instead
        //      (`DebuffReflectChance`, the tank's Backlash channel; blanket, so both schools).
        //    • *"10% TO reflect Physical Damage skill"* — a CHANCE, and when it fires the skill's WHOLE
        //      damage goes back (`PhysSkillReflectChance` 0.10 with `Pct` 1.0). That is the shape he
        //      chose for Deflection when offered both (*"a 100% chance to reflect 15%, or 15% chance to
        //      reflect 100%"* — he picked the second), so the same reading is used here.
        //
        // 🔑 THE LAST TWO RIDE BUFF FIELDS, NOT A PassiveEffect. A learned passive applies whether the
        //    stance is up or not, which would have made the toggle free — see SkillDef.PhysSkillReflectChance.
        //    Each folds by MAX against its passive twin, so an Elf Ravager holding Deflection (30% at 76)
        //    keeps the stronger of the two rather than summing to 40%.
        new(WarriorSaintsBlessing, "Saints Blessing", BaseClass.Fighter,
            SkillEffect.BuffReflect | SkillEffect.BuffMoveSpeed | SkillEffect.BuffAtkSpeed,
            MpCost: 10, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: "warrior_saints_blessing", Rank: 1, MpPerSecond: 10,
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly,
            RequiredWeapon: WeaponType.AnySword, RequiredHands: WeaponHands.Two,
            CountsTowardBuffLimit: false, SpCost: W4NewSp(78),
            PhysSkillReflectChance: 0.10f, PhysSkillReflectPct: 1.0f, DebuffReflectChance: 0.15f,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffReflect, 0.30f),
                new(SkillEffect.BuffMoveSpeed, -0.10f),
                new(SkillEffect.BuffAtkSpeed, -0.10f),
            },
            Description: "Stance. Everything aimed at you looks for a way back: basic attacks return "
                       + "30% of their damage, debuffs and physical skills sometimes bounce outright — "
                       + "for 10% of your movement and your swing, at 10 MP a second.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 10, SpCost: W4NewSp(78), GoldCost: W4NewGold(78),
                    Description: "Returns 30% of basic-attack damage; 15% chance to reflect a debuff and "
                               + "10% chance to reflect a physical skill in full. −10% move and attack "
                               + "speed, 10 MP/s."),
            }),
    };
}
