using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE ARCHER'S 4th CLASS, 76-90 — every row of
/// `docs/data/classes_skills_csv/archer 4th.csv`. Built 2026-09-09, the day after his 3rd tier, on
/// *"archer 4th done as well"* and then his fix of the ladder dips it shipped with.
///
/// <para>🔑 <b>MOST OF IT IS THE 3rd TIER CONTINUING.</b> Nine families just gain fifteen more rungs
/// on the same numbers — Armor Mastery, Bow Mastery, Twin Arrows, Explosive Arrow, three traps and
/// three Magic Arrows. What is genuinely NEW is two things: <b>three party procs at 76</b>, one per
/// race, and <b>five ultimates at 84/85</b> bought with gold and SP BOTTLES rather than SP.</para>
///
/// <para>🔑 <b>THE SP/GOLD LADDER IS THE HEALER'S, EXACTLY.</b> His `SP COST` and `Gold` columns here
/// are the same 6.5kk/11kk/16kk/80kk-then-nothing and 1kk…100kk that `healer 4th.csv` runs on, so
/// <see cref="F4"/> and <see cref="F4Rungs"/> are reused rather than a second copy written. Past 79 a
/// rung is paid for in GOLD alone; the SP column is zero.</para>
///
/// <para>⚠ <b>ONE LADDER IS STILL HIS TO FIX</b> — Armor Mastery's two regen columns read
/// <c>mpReg x1.8; hpReg +1.2</c> on all fifteen rungs, which is `rogue 2nd.csv`'s top rung and a
/// REGRESSION from the 3rd tier's <c>+2.5 / +6.0</c>. They are FROZEN at the 3rd tier's endpoint here
/// rather than built as authored: [[ladders-are-always-monotonic]] says report or interpolate, never
/// accept, and freezing is the interpolation that changes the least. He fixed Twin Arrows' dip when it
/// was reported and left these; `--check` reports them until he does.</para>
/// </summary>
public static partial class SkillCatalog
{
    // ---- The three race procs at 76, and the five ultimates at 84/85. His `SKILL_ID` column.
    public const string BowSwiftMastery   = "bow_swift_mastery";       // Demon
    public const string BowDamageMastery  = "bow_damage_mastery";      // Human
    public const string BowSpiritMastery  = "bow_management_mastery";  // Elf
    public const string ArcherHeavyArrow    = "archer_heavy_arrow";
    public const string ArcherArrowBarrage  = "archer_arrow_barrage";
    public const string ArcherBleedingArrow = "archer_bleeding_arrow";   // Demon
    public const string ArcherDazzlingArrow = "archer_dazzling_arrow";   // Human
    public const string ArcherHealingArrow  = "archer_healing_arrow";    // Elf

    /// <summary>The buff each of the three 76 masteries hands its party. A payload def — never
    /// learned, never on a bar; only <c>TryProcs</c> ever applies one.</summary>
    public const string BowSwiftMasteryBuff  = "bow_swift_mastery_buff";
    public const string BowDamageMasteryBuff = "bow_damage_mastery_buff";
    public const string BowSpiritMasteryBuff = "bow_management_mastery_buff";

    /// <summary>ONE ARROW of Arrow Barrage — the sub-skill the wrapper fires ten times. Its own
    /// SkillDef because that is what makes each arrow a real, separately-resolved hit.</summary>
    public const string ArcherBarrageArrow = "archer_arrow_barrage_arrow";

    // ---- HIS LADDERS, 76-90 ----------------------------------------------------------------------

    // 🔴 THERE IS NO `ArcherFourthBands` FIELD HERE, AND THERE MUST NOT BE. It existed for ten
    //    minutes as `= ClassSkillTables.HealerFourthBands` and the server would not START: a static
    //    FIELD INITIALIZER on `SkillCatalog` that touches `ClassSkillTables` forces THAT type's static
    //    ctor to run inside this one's, and its ctor builds the learn tables — which read the catalog
    //    that does not exist yet. Circular static init, a NullReferenceException 700 lines away in
    //    RegisterLightbringer, and `dotnet build` and `--check` both green. Third time this shape has
    //    bitten (see the note on BuildCatalog, and MobCatalog's null collection field).
    // ⚠ The band lives where it is USED — `ClassSkillTables.Fourth.cs` reads `HealerFourthBands`
    //    directly. Nothing in this file needs it.

    /// <summary>The MP ladder every fifteen-rung family in the file shares.</summary>
    private static readonly int[] ArcherMp4 =
        { 158, 164, 169, 173, 180, 186, 191, 195, 202, 208, 213, 217, 224, 230, 235 };

    /// <summary>Twin Arrows, power PER ARROW — 5200 → 8000, continuing the 3rd tier's 5000.
    /// 🔴 THIS COLUMN WAS THE 3rd TIER'S OWN (1000 → 3300, restarting at level 76) until he corrected
    /// it on 2026-09-09, when the dip was reported. Built only after the fix.</summary>
    private static readonly int[] TwinArrowPower4 =
    {
        5200, 5400, 5600, 5800, 6000, 6200, 6400, 6600,
        6800, 7000, 7200, 7400, 7600, 7800, 8000,
    };

    /// <summary>Explosive Arrow and all three Magic Arrows share one column again: 2600 → 4000,
    /// continuing the 3rd tier's 2500.</summary>
    private static readonly int[] ArcherArrowPower4 =
    {
        2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300,
        3400, 3500, 3600, 3700, 3800, 3900, 4000,
    };

    /// <summary>Bow Mastery's flat P.Atk, 820 → 1300 (the 3rd tier ended at 800).</summary>
    private static readonly int[] BowMasteryAtk4 =
    {
        820, 840, 860, 880, 900, 920, 940, 960,
        980, 1000, 1060, 1120, 1180, 1240, 1300,
    };

    /// <summary>…and its flat crit damage, 682 → 900 (the 3rd tier ended at 665).</summary>
    private static readonly int[] BowMasteryCritDmg4 =
    {
        682, 699, 716, 733, 750, 767, 784, 811,
        828, 845, 862, 879, 886, 893, 900,
    };

    /// <summary>Armor Mastery's P.Def, 72 → 100 (+2 a rung), continuing the 3rd tier's 70.</summary>
    private static int ArcherArmorPDef4(int i) => 72 + i * 2;

    /// <summary>…evasion 15 → 19, and move speed 12 → 15. Both continue the 3rd tier (12 and 11).</summary>
    private static readonly int[] ArcherArmorEva4 =
        { 15, 15, 16, 16, 17, 17, 18, 18, 18, 19, 19, 19, 19, 19, 19 };
    private static readonly int[] ArcherArmorSpeed4 =
        { 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 15, 15, 15, 15 };

    /// <summary>🔴 HIS TWO REGEN CELLS ARE A REGRESSION AND ARE NOT BUILT AS WRITTEN. Every 4th-tier
    /// row reads <c>mpReg x1.8; hpReg +1.2</c> — which is `rogue 2nd.csv`'s LEVEL-36 rung, pasted —
    /// against a 3rd tier that ends at <c>mpReg +2.5</c> (a ×2.5 multiplier, stored 1.5) and
    /// <c>hpReg +6.0</c> flat. Building the cells would make a level-76 archer regenerate a fifth of
    /// what he did at 74.
    /// <para>FROZEN at the 3rd tier's endpoint across all fifteen rungs, which is the smallest change
    /// that obeys the ladder rule AND keeps his own shape (his column is flat across the tier too —
    /// only the value is wrong). One number each for him to correct.</para></summary>
    private const float ArcherArmorMpReg4 = 1.5f;   // his 3rd tier's `mpReg +2.5`, stored as a multiplier
    private const float ArcherArmorHpReg4 = 6.0f;   // his 3rd tier's `hpReg +6.0`, flat HP/s

    /// <summary>What a skill first LEARNED at 84 or 85 costs: 100,000,000 gold and SP BOTTLES, with
    /// the SP column empty. His five ultimates are the first thing in the game to be bought that way
    /// on the fighter side (`healer 4th.csv` authors none at those two levels — see the note on
    /// <c>Fourth4NewSp</c>, which had to leave them at the 83 price for exactly that reason).</summary>
    private const int UltimateGold = 100_000_000;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS — rungs 16-30 of the 3rd tier's fifteen. Each returns ONLY the
    //  4th-tier rungs; the 3rd-tier definition site concatenates them.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    internal static SkillLevel[] ArcherFourthArmorMasteryRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"With light armor: +{ArcherArmorPDef4(i)} P.Def, +{ArcherArmorEva4[i]} evasion, "
                       + $"+{ArcherArmorSpeed4[i]} speed, 35% less often critted, "
                       + $"×{1f + ArcherArmorMpReg4:0.0} MP regen, +{ArcherArmorHpReg4:0.0} HP/s."));

    internal static ArmorMasteryProfile[] ArcherFourthArmorMasteryProfiles() =>
        Enumerable.Range(0, 15).Select(i => new ArmorMasteryProfile(
            Robe: default, None: default, Heavy: default,
            Light: new StatMods(
                PDef: ArcherArmorPDef4(i), Evasion: ArcherArmorEva4[i],
                CritRateResist: 0.35f, MoveSpeed: ArcherArmorSpeed4[i],
                MpRegenPct: ArcherArmorMpReg4, HpRegen: ArcherArmorHpReg4))).ToArray();

    internal static SkillLevel[] ArcherFourthBowMasteryRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"Bow: +{BowMasteryAtk4[i]} P.Atk, +400 range, ×1.085 P.Atk, "
                       + $"+{BowMasteryCritDmg4[i]} crit damage, +3 accuracy, ×1.2 crit rate, "
                       + $"×1.05 attack speed."));

    internal static WeaponMasteryProfile[] ArcherFourthBowMasteryProfiles() =>
        Enumerable.Range(0, 15).Select(i => new WeaponMasteryProfile(
            Bow: new PassiveEffect(
                PhysAtk: BowMasteryAtk4[i], PhysAtkPct: 0.085f, BowRange: 400f,
                CritDamageFlat: BowMasteryCritDmg4[i], Accuracy: 3,
                CritRate: 0.20f, AtkSpeedPct: 0.05f))).ToArray();

    /// <summary>Twin Arrows rungs 16-30. ⚠ The per-arrow power is what moves; the wrapper fires the
    /// SAME sub-skill twice at every rung (see <see cref="ArcherTwinArrows"/>).</summary>
    internal static SkillLevel[] ArcherFourthTwinArrowRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: TwinArrowPower4[i], MpCost: ArcherMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Looses 2 arrows, each for power {TwinArrowPower4[i]:N0}."));

    internal static SkillLevel[] ArcherFourthExplosiveArrowRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: ArcherArrowPower4[i], MpCost: ArcherMp4[i], SpCost: sp, GoldCost: gold,
            AreaRadius: 200f,
            Description: $"Bursts for power {ArcherArrowPower4[i]:N0} on everything within 200."));

    /// <summary>Any of the three traps, rungs 16-30. ⚠ THE TIER STOPS CLIMBING: all fifteen rows read
    /// tier 10, which is the top rank there is — the same ceiling his Antidote hits at 76. So a 4th-tier
    /// rung buys reach and reliability, never a stronger ailment.</summary>
    internal static SkillLevel[] ArcherFourthTrapRungs(bool tiered) => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(MpCost: ArcherMp4[i], SpCost: sp, GoldCost: gold,
            Rank: tiered ? 10 : 0,
            Description: tiered
                ? "Catches everything within 400 of the trap for 30s (tier 10) when it springs."
                : "Holds everything within 400 of the trap for 30s when it springs."));

    internal static SkillLevel[] ArcherFourthMagicArrowRungs(EffectMagnitude[] mags, string what) =>
        F4Rungs(15, 1, (i, sp, gold) =>
            new SkillLevel(Power: ArcherArrowPower4[i], MpCost: ArcherMp4[i], SpCost: sp, GoldCost: gold,
                Magnitudes: mags,
                Description: $"Strikes for power {ArcherArrowPower4[i]:N0} {what}."));

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  WHAT IS NEW AT THE 4th TIER
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    private static SkillDef[] Archer4thSkills()
    {
        var list = new List<SkillDef>();
        var (mastSp, mastGold) = F4New(76);
        var (ultSp84, _) = F4New(84);

        // ═══ THE THREE 76 MASTERIES — one per race, and the archer's first PARTY contribution ═════
        //
        // 🔑 EACH IS A PASSIVE PROC THAT BUFFS THE WHOLE PARTY, not just its owner: his TARGET cell is
        //    `self/party` at 900 radius, and the DESCR says *"for self and party"* on all three. That
        //    is `ProcPartyRungs`, and the caster takes the same rung rather than a stronger one — his
        //    rows name ONE number, unlike the tank's Aggravated State which names two.
        // ⚠ 3% on hit, 30s internal cooldown, 30s buff. Bow-gated, and the proc machinery honours it.
        list.Add(PartyMastery(BowSwiftMastery, "Swift Mastery", BowSwiftMasteryBuff, mastSp, mastGold,
            "Passive. Your rhythm carries the whole party: now and then everyone quickens."));
        list.Add(PartyMastery(BowDamageMastery, "Damage Mastery", BowDamageMasteryBuff, mastSp, mastGold,
            "Passive. Your aim sharpens the whole party: now and then everyone hits harder."));
        list.Add(PartyMastery(BowSpiritMastery, "Spirit Mastery", BowSpiritMasteryBuff, mastSp, mastGold,
            "Passive. Your economy steadies the whole party: now and then everyone casts cheaper, "
          + "faster and crueller."));

        // The three payloads. ⚠ `Damage Mastery` is the FINAL-damage channel (`PhysDamageMult` /
        // `MagicDamageMult`), not a P.Atk buff — his words are *"increase p/m final damage"*, and that
        // is the shot channel `BL-185` built, applied in FinalizeDamage. A +10% P.Atk buff would have
        // been worth a fraction of it inside an additive ratio, which is the whole lesson of 0.117.0.
        list.Add(new SkillDef(BowSwiftMasteryBuff, "Swift Mastery", BaseClass.Fighter,
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "bow_swift_mastery", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, AreaRadius: 900f,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtkSpeed, 0.10f), new(SkillEffect.BuffCastSpeed, 0.10f),
            },
            Description: "+10% attack and cast speed for 30s."));

        list.Add(new SkillDef(BowDamageMasteryBuff, "Damage Mastery", BaseClass.Fighter,
            SkillEffect.BuffPveSkillDamage | SkillEffect.BuffPvpSkillDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "bow_damage_mastery", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, AreaRadius: 900f,
            PhysDamageMult: 1.10f, MagicDamageMult: 1.10f,
            Description: "+10% final physical and magical damage for 30s."));

        list.Add(new SkillDef(BowSpiritMasteryBuff, "Spirit Mastery", BaseClass.Fighter,
            SkillEffect.BuffCritDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "bow_management_mastery", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, AreaRadius: 900f,
            PhysMpCostPct: 0.20f, MagicMpCostPct: 0.20f,
            // ⚠ "p.skill CAST TIME by 20%" is a cast-speed grant, not a reuse one — his word is `cast
            //   time`. It rides `BuffCastSpeed`'s magnitude like every other cast-speed buff.
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffCritDamage, 0.10f), new(SkillEffect.BuffCastSpeed, 0.20f),
            },
            Description: "−20% MP on every skill, +20% cast speed and +10% critical damage for 30s."));

        // ═══ HEAVY ARROW (84) — one shot, and the biggest number in the archer's book ═════════════
        list.Add(Ultimate(ArcherHeavyArrow, "Heavy Arrow", SkillEffect.PhysicalDamage,
            level: 84, bottles: 2, power: 17000, mp: 195, durationTicks: 0,
            mags: Array.Empty<EffectMagnitude>(),
            "One arrow drawn to the ear and loosed. There is nothing clever about it.",
            "Strikes for power 17,000."));

        // ═══ THE THREE RACE ULTIMATES (85) ═══════════════════════════════════════════════════════
        list.Add(Ultimate(ArcherBleedingArrow, "Bleeding Arrow",
            SkillEffect.PhysicalDamage | SkillEffect.Bleed | SkillEffect.Slow,
            level: 85, bottles: 5, power: 15000, mp: 208, durationTicks: 300,
            mags: new EffectMagnitude[] { new(SkillEffect.Slow, 0.30f) },
            "An arrow that opens a wound nothing in this game can close.",
            "Strikes for power 15,000 and leaves a tier-11 bleed and 30% slow for 30s.",
            rank: 11, school: DebuffSchool.Physical));

        list.Add(Ultimate(ArcherDazzlingArrow, "Dazzling Arrow",
            SkillEffect.PhysicalDamage | SkillEffect.Stun | SkillEffect.Cancel,
            level: 85, bottles: 5, power: 15000, mp: 208, durationTicks: 100,
            mags: Array.Empty<EffectMagnitude>(),
            "A burst of light and noise: the target is out on its feet, and whatever was protecting "
          + "it is gone.",
            "Strikes for power 15,000, stuns for 10s and strips up to 3 buffs.",
            school: DebuffSchool.Physical, dispelCount: 3));

        list.Add(Ultimate(ArcherHealingArrow, "Healing Arrow",
            SkillEffect.PhysicalDamage,
            level: 85, bottles: 5, power: 15000, mp: 208, durationTicks: 0,
            mags: Array.Empty<EffectMagnitude>(),
            "What it takes out of them, it puts back into you.",
            "Strikes for power 15,000 and heals you for 40% of the damage dealt.",
            lifesteal: 0.40f));

        // ═══ ARROW BARRAGE (85) — THE FIRST CHANNEL IN THE GAME ══════════════════════════════════
        //
        // 🔑 HIS DESIGN, AND HIS REASONING FOR IT (2026-09-09). He offered two shapes and killed the
        //    first himself: a pulsating ground effect *"removes our game logic point — always hit then
        //    calculates evasions etc"*. The one he kept is a WRAPPER: *"inside the wrapper each arrow
        //    is same skill (power 2500, range 900, aoe 150, etc..) and its cast 10 times or until
        //    wrapper stops — thats a sure single target and colateral around it"*.
        //
        // 🔑 SO THE ARROW IS A REAL SKILL, not a tick. `archer_arrow_barrage_arrow` carries the power,
        //    the range and the 150 radius, and each of the ten goes through `ExecuteSkill` on its own —
        //    its own crit, its own block, its own splash. That is what preserves the rule.
        //
        // ⚠ His comment cell is the spec: *"Like a channeling skill; Start to cast and for the next 2
        //   second it continue to cast 1arrow/200ms; can be canceled like normal skill"*. So the 3s
        //   cast finishes, and THEN two seconds of arrows run — cancellable throughout, and the reuse
        //   starts when the channel ends, not when the cast did.
        list.Add(Ultimate(ArcherArrowBarrage, "Arrow Barrage", SkillEffect.PhysicalDamage,
            // ⚠ DURATION 20 IS THE VOLLEY, not a buff — two seconds of arrows, and his DURR cell says
            //   2. Authored rather than derived from shots × interval: a channel's length is a thing
            //   the player is told, and Twin Arrows fires the same way in 0.4s while its own cell
            //   correctly reads 0. Two arrows is not a channel; ten is.
            level: 85, bottles: 5, power: 0, mp: 208, durationTicks: 20,
            mags: Array.Empty<EffectMagnitude>(),
            "Ten arrows in two seconds, and none of them politely.",
            "Looses 10 arrows over 2s, each for power 2,500 with a 150 splash.",
            channelSkill: ArcherBarrageArrow, channelShots: 10, channelIntervalTicks: 2));

        // ONE ARROW. Never learned and never on a bar — the wrapper is what the player owns.
        // ⚠ MP is ZERO here: the wrapper charges his 208 once, for the whole volley. Ten arrows each
        //   charging MP would be a different skill and a different price.
        list.Add(new SkillDef(ArcherBarrageArrow, "Arrow Barrage", BaseClass.Fighter,
            SkillEffect.PhysicalDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 900, Power: 2500,
            Category: SkillCategory.Physical,
            AreaRadius: 150f, AreaAtTarget: true, TargetMode: TargetMode.EnemiesInRadius,
            RequiredWeapon: WeaponType.Bow,
            Description: "One arrow of a barrage: power 2,500 where it lands, and 150 around it."));

        return list.ToArray();
    }

    /// <summary>One of the three level-76 party masteries. They differ only in which payload they
    /// hand out; everything else — 3% on a landed hit, a 30s lockout, a 30s buff, a bow — is shared.
    /// <para>⚠ The party rung and the SELF rung are the SAME def, because his row names one number
    /// (*"for self and party"*). <c>ProcSelfRungs</c> and <c>ProcPartyRungs</c> both point at it, and
    /// a shared buff key means the caster standing in his own aura never holds two.</para></summary>
    private static SkillDef PartyMastery(string id, string name, string payload, int sp, int gold,
                                         string blurb)
        => new(id, name, BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: sp,
            RequiredWeapon: WeaponType.Bow,
            ProcChance: 0.03f, ProcCooldownTicks: 300,
            ProcSelfRungs: new[] { payload },
            ProcPartyRungs: new[] { payload },
            Description: blurb,
            Levels: new[] { new SkillLevel(SpCost: sp, GoldCost: gold, Description: blurb) });

    /// <summary>One of the five 84/85 ultimates. All share his row shape — 900 range, a 3-second draw,
    /// a 10-second reuse, 100kk gold and SP BOTTLES instead of SP — and differ only in their rider.</summary>
    private static SkillDef Ultimate(string id, string name, SkillEffect effect,
                                     int level, int bottles, int power, int mp, int durationTicks,
                                     EffectMagnitude[] mags, string blurb, string rungText,
                                     int rank = 0, DebuffSchool school = DebuffSchool.None,
                                     int dispelCount = 0, float lifesteal = 0f,
                                     string? channelSkill = null, int channelShots = 0,
                                     int channelIntervalTicks = 0)
        => new(id, name, BaseClass.Fighter, effect,
            MpCost: mp, CastTicks: 30, CooldownTicks: 100, Range: 900, Power: power,
            DurationTicks: durationTicks, BuffKey: id, Rank: rank,
            DebuffSchool: school, DispelCount: dispelCount, Lifesteal: lifesteal,
            Category: SkillCategory.Physical,
            RequiredWeapon: WeaponType.Bow,
            ChannelSkill: channelSkill, ChannelShots: channelShots,
            ChannelIntervalTicks: channelIntervalTicks,
            // 🔑 PAID IN BOTTLES, NOT SP — his `SP Bottles` column, and the SP column on these five
            //    rows is empty. A bottle is 1kkk SP, and five of them is 5kkk against an `int` that
            //    stops at 2.147kkk; spending them as a CURRENCY is what keeps `Entity.SkillPoints`
            //    from having to become a `long` (owner, 2026-08-26). Do not "fix" that.
            LearnConsumableId: ItemCatalog.SpBottle, LearnConsumableAmount: bottles,
            Magnitudes: mags,
            Description: blurb,
            Levels: new[]
            {
                new SkillLevel(Power: power, MpCost: mp, SpCost: 0, GoldCost: UltimateGold,
                    LearnConsumableAmount: bottles, Magnitudes: mags,
                    Description: rungText),
            });
}
