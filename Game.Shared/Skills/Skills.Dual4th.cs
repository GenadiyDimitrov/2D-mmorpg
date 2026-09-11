using System;
using System.Linq;

namespace Game.Shared;

/// <summary>THE MELEE ROGUE'S 4th CLASS — Nullblade (Human) / Shadowblade (Elf) / Venomblade (Demon).
///
/// <para>🔴 <b>THIS FILE IS THREE SKILLS, NOT A KIT.</b> `dual 4th.csv` is still the two-line
/// placeholder and nothing is being invented here: what follows is the part of the ascended melee
/// rogue he ruled OUTRIGHT on 2026-09-09 while settling `BL-188` — the top of the blow ladder — and
/// he asked for it in the same breath (*"u can add those skills in the csvs and in the code"*). The
/// rest of the discipline lands the day he authors the file, and this file grows then.</para>
///
/// <para>⚠ Because the file is unfinished it has NOT earned a <c>Check.Specs</c> line: the checker
/// walks whole files and would report every unauthored family as missing. The three rows below ARE
/// written into `dual 4th.csv`, per the rule at the top of CLAUDE.md, and they are simply unwalked
/// until he finishes the file. Do not add the spec early to make them checked — that was the
/// `nuker 3rd` lesson from the other direction.</para>
///
/// <para>🔑 <b>THE @80 PAIR IS A CHOICE, NOT A STACK.</b> His words: *"they don't stack (like great
/// bulwark/might)"* — so they take that pair's exact recipe: one shared <see cref="SkillDef.BuffKey"/>
/// at <c>Rank 1</c> with <c>FlatRank</c>, which makes casting either EVICT the other and leaves the
/// choice re-makeable mid-fight. Rate or damage, never both. With the whole ladder up that is
/// ~60% landing and a much bigger number when it does, or ~80% landing and a smaller one.</para>
/// </summary>
public static partial class SkillCatalog
{
    /// <summary>+5% blow rate at 76 — every melee rogue, no race split.</summary>
    public const string AssassinationInstinct = "assassination_instinct";
    /// <summary>@80, +40% blow rate. Shares a <c>BuffKey</c> with <see cref="BrutalStrike"/>, so only
    /// one of the two can be UP — both are learned.</summary>
    public const string PerfectStrike = "perfect_strike";
    /// <summary>@80, +30% physical crit damage. Shares a <c>BuffKey</c> with
    /// <see cref="PerfectStrike"/>, so only one of the two can be UP — both are learned.</summary>
    public const string BrutalStrike = "brutal_strike";

    /// <summary>The shared family the @80 pair competes on — the reason neither can be held with the
    /// other. Named for what it is rather than for either skill, exactly as `great_blessing` is.</summary>
    private const string StrikeChoiceKey = "dagger_strike_choice";

    // ---- HIS 2026-09-11 BLOCK: the three race IDENTITY passives and the three race ULTIMATES, which
    //      is what he meant by *"With that duals 4th is finihed (untill dmg is rly tested)"*. Every
    //      number below is authored — read off `dual 4th.csv`, not derived.
    //
    // 🔑 THE PAIRING IS THE DESIGN, and it is one axis per race carried at two strengths: a small
    //    PERMANENT passive (5/7/10% at 80/85/90) and a big TEN-SECOND ultimate on the same axis. The
    //    Human is proofed against magic, the Elf against physical SKILLS, the Demon against people.
    //    So the ultimate is not a different tool from the passive — it is the same defence, briefly
    //    turned up to where it decides a fight.
    /// <summary>Human, 80/85/90 — magic resistance 5/7/10%. Pairs with <see cref="DualMagicArmor"/>.</summary>
    public const string DualAntiMagic = "dual_anti_magic";
    /// <summary>Elf, 80/85/90 — physical-SKILL evasion 5/7/10%. Pairs with <see cref="DualDodge"/>.
    /// ⚠ Not `Evasion`: his cell says *"p.skill evasion"*, which is <c>SkillEvadeChance</c> — the
    /// grant that `BL-06` left as the ONLY way a physical skill can be dodged at all.</summary>
    public const string DualAntiPhysical = "dual_anti_physical";
    /// <summary>Demon, 80/85/90 — PvP damage 5/7/10%. Pairs with <see cref="DualDemonContract"/>.</summary>
    public const string DualDuelExpertise = "dual_duel_expertise";

    /// <summary>Elf @83 — 30% chance to evade physical skills for 10s.</summary>
    public const string DualDodge = "dual_dodge";
    /// <summary>Human @83 — +30% magic resistance for 10s.</summary>
    public const string DualMagicArmor = "dual_magic_armor";
    /// <summary>Demon @83 — +25% PvP damage for 10s.</summary>
    public const string DualDemonContract = "dual_demon_contract";


    // ═════════════════════════════════════════════════════════════════════════════════════════════
    //  THE CONTINUING LADDERS — rungs 16-30 of the 3rd tier's fifteen, one per level 76-90.
    //
    //  🔴 EVERY NUMBER BELOW IS DERIVED, NOT AUTHORED. `dual 4th.csv` holds no rows for these
    //  families and the 40+ rule normally forbids inventing them — he lifted it for this one job, in
    //  these words: *"Build the new skills for duals 4 so it's measurable after 76 (even with lower
    //  power skills)"*. The point is a MEASURABLE melee rogue above 76, not a finished class. Every
    //  row is written into `dual 4th.csv` marked DERIVED so he can overwrite it wholesale, and the
    //  file still has no `Check.Specs` line.
    //
    //  🔑 THE ONE ANCHOR THAT IS HIS is the damage: he gave the melee rogue's Stab as **7k-11k power
    //  at 85 and 10k-15k at 90** (2026-09-06, the reference kit `BalanceMatrix --his` measures
    //  against). So Killing Stab is built to land on **11,000 at 85 and 15,000 at 90** exactly — the
    //  TOP of each band he named — and the other three families keep the ratio they already have to
    //  it at the 3rd tier (Heavy ×0.75, Venom ×0.50, Venom Burst ×0.20 per stack). That is a
    //  continuation of his own numbers rather than an invention on top of them.
    // ═════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>Killing Stab / Swift Stab, 3,450 → 7,500. HALVED on 2026-09-11 (`BL-203`) along with
    /// the whole 3rd tier — see the block comment on <c>StabPower</c> in Skills.Dual3rd.cs for why a
    /// halving rather than a re-tune, and for the `CanDouble` flag that hands the old number back on
    /// a roll.
    ///
    /// <para>⚠ HIS TWO ANCHORS MOVE WITH IT. This ladder was built to land on his *"7k-11k at 85,
    /// 10k-15k at 90"* exactly; halved, it lands on 5,500 at 85 and 7,500 at 90 — the same anchors
    /// under the new scale, and the old ones again whenever Overpower doubles the blow. The stride
    /// still widens at 86 (+225 a rung to 85, then +400) because his own bands widen there.</para></summary>
    private static readonly int[] StabPower4 =
    {
        3450, 3675, 3900, 4125, 4350, 4575, 4800, 5025,
        5250, 5500, 5900, 6300, 6700, 7100, 7500,
    };

    /// <summary>Heavy Stab — ×0.75 of <see cref="StabPower4"/> PER HIT, and it hits twice. Exactly
    /// the ratio the two carry at the 3rd tier (2,400 against 3,200). Odd rungs round up.</summary>
    private static readonly int[] HeavyStabPower4 =
    {
        2588, 2750, 2925, 3088, 3263, 3425, 3600, 3775,
        3938, 4125, 4425, 4725, 5025, 5325, 5625,
    };

    /// <summary>Venom Stab — <b>the same ladder as Killing Stab</b>, his 2026-09-11 ruling. The
    /// ×0.50 the Demon used to pay per blow is gone; the 3rd tier's note explains why keeping it
    /// would have halved the Venomweaver twice. ⚠ The NUMBERS here are unchanged by that: this
    /// ladder was already ×0.50 of the old <see cref="StabPower4"/>, and ×0.50 of the old is
    /// ×1.00 of the new.</summary>
    private static readonly int[] VenomStabPower4 = StabPower4;

    /// <summary>Venom Burst, PER CONSUMED STACK — ×0.20, so ten stacks is ×2 a Killing Stab.
    /// Halved with the rest (`BL-203`), which is what preserves that ×2.</summary>
    private static readonly int[] VenomBurstPerStack4 =
    {
        690, 735, 780, 825, 870, 915, 960, 1005,
        1050, 1100, 1180, 1260, 1340, 1420, 1500,
    };

    /// <summary>The stab MP ladder, 80 → 110, continuing the 3rd tier's 78.</summary>
    private static readonly int[] StabMp4 =
        { 80, 82, 84, 86, 88, 90, 92, 95, 98, 100, 102, 104, 106, 108, 110 };

    /// <summary>Venom TIER and stacks-per-cast are BOTH FROZEN at the 3rd tier's endpoint — rank 10
    /// is the top rank a debuff can carry (there is nothing above it left to author) and 3 stacks a
    /// cast against a cap of 10 is already the Demon's full rotation. Frozen rather than invented
    /// upward, which is the same call the archer's regen cells got.</summary>
    private const int VenomTier4 = 10, VenomStacks4 = 3;

    /// <summary>Dual Mastery's flat P.Atk, 85 → 150 (the 3rd tier ended at 80).</summary>
    private static readonly int[] DualMasteryAtk4 =
    {
        85, 90, 95, 100, 105, 110, 115, 120,
        125, 130, 134, 138, 142, 146, 150,
    };

    /// <summary>…and its flat crit damage, 1,040 → 1,300 (the 3rd tier ended at 1,015). Same shape
    /// and the same ~+28% span as the bow's 682 → 900.</summary>
    private static readonly int[] DualMasteryCritDmg4 =
    {
        1040, 1060, 1080, 1100, 1120, 1140, 1160, 1180,
        1200, 1220, 1240, 1260, 1275, 1290, 1300,
    };

    /// <summary>…attack speed ×1.10 → ×1.15. Crit RATE is FLAT at ×1.50 across the tier: the 3rd
    /// tier already ends there and a dagger's crit is a third of the way to a 50% cap that the blow
    /// roll no longer even reads (`BL-188`), so climbing it further buys almost nothing.</summary>
    private static readonly float[] DualMasteryAtkSpd4 =
        { .10f, .10f, .11f, .11f, .12f, .12f, .13f, .13f, .13f, .14f, .14f, .15f, .15f, .15f, .15f };

    private const float DualMasteryCritRate4 = 0.50f;

    /// <summary>Rungs 21-35 of the MELEE rogue's Armor Mastery.
    /// 🔑 <b>They are the ARCHER's, deliberately reused</b> — same light armour, same tier, and the two
    /// files differ by a single point of evasion at the 3rd tier (14 against 12). Inventing a second
    /// ladder a point apart would be inventing a difference he has not asked for, and the archer's
    /// numbers are read off `archer 4th.csv`, so half of this is authored rather than none of it.
    /// ⚠ `archer_armor_mastery` REPLACES `rogue_armor_mastery` at 40, so appending here reaches the
    /// three melee disciplines and nobody else.</summary>
    internal static SkillLevel[] RogueArmorMasteryFourthRungs() => ArcherFourthArmorMasteryRungs();

    /// <inheritdoc cref="RogueArmorMasteryFourthRungs"/>
    internal static ArmorMasteryProfile[] RogueArmorMasteryFourthProfiles() =>
        ArcherFourthArmorMasteryProfiles();

    /// <summary>Rungs 16-30 of Dual Mastery.</summary>
    internal static SkillLevel[] DualMasteryFourthRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(SpCost: sp, GoldCost: gold,
            Description: $"Duals: +{DualMasteryAtk4[i]} P.Atk, ×1.085 P.Atk, "
                       + $"+{DualMasteryCritDmg4[i]} crit damage, +3 accuracy, "
                       + $"×{1f + DualMasteryCritRate4:0.0} crit rate, "
                       + $"×{1f + DualMasteryAtkSpd4[i]:0.00} attack speed."));

    /// <inheritdoc cref="DualMasteryFourthRungs"/>
    internal static WeaponMasteryProfile[] DualMasteryFourthProfiles() =>
        Enumerable.Range(0, 15).Select(i => new WeaponMasteryProfile(
            Dual: new PassiveEffect(
                PhysAtk: DualMasteryAtk4[i], PhysAtkPct: 0.085f,
                CritDamageFlat: DualMasteryCritDmg4[i], Accuracy: 3,
                CritRate: DualMasteryCritRate4, AtkSpeedPct: DualMasteryAtkSpd4[i]),
            RequiredWeapon: WeaponType.Dual)).ToArray();

    /// <summary>Rungs 16-30 of a straight BLOW family (Killing / Swift / Heavy).</summary>
    internal static SkillLevel[] StabFourthRungs(int[] power) => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: power[i], MpCost: StabMp4[i], SpCost: sp, GoldCost: gold,
            Description: $"Blow power {power[i]:N0} on a critical; "
                       + "a normal attack otherwise."));

    /// <summary>Rungs 16-30 of Venom Stab — the venom rider rides along, frozen at tier 10.</summary>
    internal static SkillLevel[] VenomStabFourthRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: VenomStabPower4[i], MpCost: StabMp4[i], SpCost: sp, GoldCost: gold,
            Rank: VenomTier4, StacksPerCast: VenomStacks4,
            Description: $"Blow power {VenomStabPower4[i]:N0} on a critical; "
                       + "a normal attack otherwise. "
                       + $"Adds {VenomStacks4} tier-{VenomTier4} venom stack(s), max 10."));

    /// <summary>Rungs 16-30 of Venom Burst.</summary>
    internal static SkillLevel[] VenomBurstFourthRungs() => F4Rungs(15, 1, (i, sp, gold) =>
        new SkillLevel(Power: VenomBurstPerStack4[i], MpCost: StabMp4[i], SpCost: sp, GoldCost: gold,
            Rank: VenomTier4, StacksPerCast: VenomStacks4,
            Description: $"Power {VenomBurstPerStack4[i]:N0} per consumed venom stack (up to ×10). "
                       + $"With no stacks on the target, lays {VenomStacks4} tier-{VenomTier4} instead."));

    private static SkillDef[] Dual4thSkills()
    {
        var (sp76, gold76) = F4New(76);
        var (sp80, gold80) = F4New(80);

        // ═══ ASSASSINATION INSTINCT — 76, the small permanent rung ═══════════════════════════════
        // His name, his number: *"@76 all get assassination instinct passive that increase blow rate
        // with 5%"*. One rung, no race split, no weapon gate (a passive cannot be "cast wrong").
        var instinct = new SkillDef(AssassinationInstinct, "Assassination Instinct", BaseClass.Fighter,
            SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: sp76,
            Passive: new PassiveEffect(BlowRate: 0.05f),
            Description: "Passive. Killing has become a reflex: a few more of your blows find the gap.",
            Levels: new[]
            {
                new SkillLevel(SpCost: sp76, GoldCost: gold76,
                    Passive: new PassiveEffect(BlowRate: 0.05f),
                    Description: "Blow landing rate ×1.05."),
            });

        // ═══ THE @80 CHOICE — 200 MP, five minutes up, five minutes down ═════════════════════════
        // 5 min = 3000 ticks for BOTH the duration and the reuse, so the pair is very nearly a
        // permanent stance you may re-pick at each expiry rather than a burst.
        SkillDef Choice(string id, string name, float blowRate, float critDmg, string blurb, string rung) =>
            new(id, name, BaseClass.Fighter, blowRate > 0f ? SkillEffect.BuffCritRate : SkillEffect.BuffCritDamage,
                MpCost: 200, CastTicks: 0, CooldownTicks: 3000, Range: 0, Power: 0,
                DurationTicks: 3000, BuffKey: StrikeChoiceKey, Rank: 1, FlatRank: true,
                Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
                RequiredWeapon: WeaponType.Dual, SpCost: sp80,
                BlowRatePct: blowRate,
                Magnitudes: critDmg > 0f
                    ? new EffectMagnitude[] { new(SkillEffect.BuffCritDamage, critDmg) }
                    : Array.Empty<EffectMagnitude>(),
                Description: blurb,
                Levels: new[]
                {
                    new SkillLevel(MpCost: 200, SpCost: sp80, GoldCost: gold80, BlowRatePct: blowRate,
                        Magnitudes: critDmg > 0f
                            ? new EffectMagnitude[] { new(SkillEffect.BuffCritDamage, critDmg) }
                            : Array.Empty<EffectMagnitude>(),
                        Description: rung),
                });

        // 🔑 BOTH ARE LEARNED, AND THE CHOICE IS PER FIGHT, NOT PER CHARACTER. His 2026-09-10
        // clarification: *"brutal/perfect strike can be bot learned but they just dont stack as buffs
        // .. a dual class can have them both and chose depending on situatuion which to use"*. That is
        // already how this is built — both sit in the learn table at 80 and the exclusion is the
        // shared `BuffKey`, so casting one evicts the other from the bar and nothing is ever unlearned.
        // ⚠ Say it that way in the player-facing text too: "Replaces Brutal Strike" reads like a
        // learn-tab consequence, which is precisely the thing he was ruling out.
        var perfect = Choice(PerfectStrike, "Perfect Strike", 0.40f, 0f,
            "Five minutes in which almost nothing you swing at is missed. You keep Brutal Strike as "
          + "well — they simply cannot be up at the same time, so pick one per fight. Requires duals.",
            "5 min: blow landing rate ×1.40. Takes the place of Brutal Strike while it is up.");

        var brutal = Choice(BrutalStrike, "Brutal Strike", 0f, 0.30f,
            "Five minutes in which what does land is ruinous. You keep Perfect Strike as well — they "
          + "simply cannot be up at the same time, so pick one per fight. Requires duals.",
            "5 min: +30 crit damage. Takes the place of Perfect Strike while it is up.");

        // ═══ THE THREE RACE IDENTITY PASSIVES — 80 / 85 / 90, 5% → 7% → 10% ══════════════════════
        //
        // 🔑 THREE RUNGS, NOT FIFTEEN. The GOLD ladder is `F4New`'s at those three levels (5kk / 50kk /
        //    100kk) and matches his cells exactly, so it is read rather than repeated.
        // ⚠ THE SP IS ZERO AND IS **NOT** `F4New`'s — his `SP COST` cell reads 0 on all nine rows,
        //   while `F4New(80)` would charge 150kk. The CSV is the authority; `--check` caught this the
        //   moment `dual 4th` earned its spec line, which is the whole reason the spec was added.
        // ⚠ NO WEAPON GATE. His WEAPON cell is empty on all nine rows, unlike Dual Mastery's `duals`,
        //   and that is consistent: this is who the character IS, not what he is holding.
        float[] identity = { 0.05f, 0.07f, 0.10f };
        int[] identityLevels = { 80, 85, 90 };

        SkillDef Identity(string id, string name, Func<float, PassiveEffect> effect,
                          string blurb, Func<float, string> rung) =>
            new(id, name, BaseClass.Fighter, SkillEffect.None,
                MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
                Category: SkillCategory.Passive,
                SpCost: 0,
                Passive: effect(identity[0]),
                Description: blurb,
                Levels: identityLevels.Select((lv, i) =>
                    new SkillLevel(SpCost: 0, GoldCost: F4New(lv).Gold,
                        Passive: effect(identity[i]),
                        Description: rung(identity[i]))).ToArray());

        var antiMagic = Identity(DualAntiMagic, "Anti-Magic",
            v => new PassiveEffect(MagicResist: v),
            "Passive. Spells slide off you a little more each time you learn to expect them.",
            v => $"Magic resistance +{v * 100:0}%.");

        var antiPhysical = Identity(DualAntiPhysical, "Anti-Physical",
            v => new PassiveEffect(SkillEvadeChance: v),
            "Passive. You read the wind-up. Some blows simply never arrive.",
            v => $"A {v * 100:0}% chance to evade a physical SKILL outright.");

        var duelExpertise = Identity(DualDuelExpertise, "Duel-Expertise",
            // ⚠ ALL THREE PvP CHANNELS. His cell is `PvP.Dmg`, unqualified — a dagger's damage comes
            //   from skills, basics and (through Venom Burst) neither cleanly, and a bonus that
            //   covered only one of the three would read as broken on the other two.
            v => new PassiveEffect(PvpSkillDamagePct: v, PvpMagicDamagePct: v, PvpBasicDamagePct: v),
            "Passive. Killing people is a different craft from killing monsters, and you have it.",
            v => $"+{v * 100:0}% damage against PLAYERS (skills, spells and basic attacks alike).");

        // ═══ THE THREE RACE ULTIMATES — 83, ten seconds, 90-second reuse ═════════════════════════
        //
        // 🔑 TEN SECONDS ON A NINETY-SECOND REUSE IS A PANIC BUTTON, not a stance — an 11% uptime you
        //    spend on the one exchange that would otherwise kill you. That is why each is the BIG
        //    version of its race's own passive rather than a new axis: the Human's 10% magic
        //    resistance becomes 30% for ten seconds, and so on.
        // ⚠ MP 0 AND SP 500kk ARE HIS, from the file. An ultimate paid for once in SP and never again
        //   in MP is a deliberate shape here — the reuse is the cost.
        // ⚠ NO BUFF FAMILY, for the same reason Evasion Boost and Defensive Wall have none: an
        //   ultimate must stack on top of whatever ladder it resembles rather than evict a potion.
        // ⚠ `PhysicalCast` — his TYPE cell is `Physical Buff`, so it is paced by attack speed and
        //   stopped by a PHYSICAL silence, like every other thing a rogue does.
        var (ultSp, ultGold) = (500_000_000, 100_000_000);

        SkillDef Ultimate83(string id, string name, SkillEffect effect, EffectMagnitude[] mags,
                            float skillEvade, string blurb, string rung) =>
            new(id, name, BaseClass.Fighter, effect,
                MpCost: 0, CastTicks: 0, CooldownTicks: 900, Range: 0, Power: 0,
                DurationTicks: 100, BuffKey: id, Rank: 1,
                Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly,
                SpCost: ultSp, SkillEvadeChance: skillEvade,
                Magnitudes: mags,
                Description: blurb,
                Levels: new[]
                {
                    new SkillLevel(MpCost: 0, SpCost: ultSp, GoldCost: ultGold,
                        SkillEvadeChance: skillEvade, Magnitudes: mags, Description: rung),
                });

        var dodge = Ultimate83(DualDodge, "Dodge", SkillEffect.None,
            Array.Empty<EffectMagnitude>(), 0.30f,
            "Ten seconds in which almost nothing aimed at you connects. Then it does.",
            "10s: a 30% chance to evade any physical SKILL outright.");

        var magicArmor = Ultimate83(DualMagicArmor, "Magical Armor", SkillEffect.BuffMagicResist,
            new EffectMagnitude[] { new(SkillEffect.BuffMagicResist, 0.30f) }, 0f,
            "Ten seconds wearing the shape of a spell, so the spells find nothing to hold.",
            "10s: +30% magic resistance.");

        var demonContract = Ultimate83(DualDemonContract, "Demon Contract",
            SkillEffect.BuffPvpSkillDamage | SkillEffect.BuffPvpMagicDamage | SkillEffect.BuffPvpBasicDamage,
            new EffectMagnitude[]
            {
                new(SkillEffect.BuffPvpSkillDamage, 0.25f),
                new(SkillEffect.BuffPvpMagicDamage, 0.25f),
                new(SkillEffect.BuffPvpBasicDamage, 0.25f),
            }, 0f,
            "Ten seconds of something older than you doing the aiming. It only ever wants people.",
            "10s: +25% damage against PLAYERS.");

        return new[]
        {
            instinct, perfect, brutal,
            antiMagic, antiPhysical, duelExpertise,
            dodge, magicArmor, demonContract,
        };
    }
}
