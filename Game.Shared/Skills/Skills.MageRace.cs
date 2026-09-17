using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>THE MAGE'S RACE LAYER — every row of the race block in
/// <c>docs/data/classes_skills_csv/mage 1st.csv</c>, landed 2026-09-17 (`BL-258`).
///
/// <para>🔑 <b>THE TWIN OF <see cref="FighterRaceSkills"/>, AND THE SAME STRUCTURAL POINT.</b> The
/// file is `mage 1st`, but the ladders run to 74 and 90 — these are not "first-class skills" a
/// level-20 grows out of. They are the RACE's contribution to every mystic, and they follow the
/// character through the 2nd, 3rd and 4th class changes untouched. A cleric, a nuker and a buffer of
/// the same race all learn exactly this, rung for rung, which is why they are injected centrally
/// (<c>ClassSkills.MageRaceSkills</c>) rather than fanned across the per-class tables:
/// <c>Cumulative</c> yields the base-mage list only to a character who has NOT changed class, so
/// listing them there would have quietly deleted them at 20.</para>
///
/// <para>⚠ <b>THE SPLIT IS NOT THE FIGHTER'S, and deliberately so.</b> Two of the fighter's six are a
/// mage's day job already (a cure, a self-heal), so his mage block answers a different question: the
/// Elf gets SUSTAIN (the self-heal, on a ladder that reaches 800 power), the Demon a 5-second OFFENCE
/// burst he can hold for the pull that matters, the Human the DRAIN — Vampiric Bolt, which used to be
/// the Human nuker's alone and is now every Human mystic's.</para>
///
/// <para>🔑 <b>AND EVERY RACE GETS A BLESSING AT 7</b>, auto-granted and free: three always-on
/// passives, one per race, which is where the "what does my race do for me" question gets its answer
/// for a class whose kit is otherwise identical across the three.</para>
///
/// <para>🔴 <b>`self_heal` IS GONE — ITS ID MOVED.</b> The base mage's three-rung Self Heal (1/7/14,
/// power 42/67/107, every race) was deleted from his file and re-authored as the ELF's nine-rung
/// ladder under <see cref="ElfSelfHeal"/>. Ids are append-only as a rule and this one is exempt for
/// the same reason `mana_barrier` was: pre-release, nobody outside this machine can be holding the
/// old string, and two defs with one payload is how a number drifts. The healer's `Heal` no longer
/// `Replaces` it — the race layer is not something a class change takes away.</para></summary>
public static partial class SkillCatalog
{
    // ═══ THE MAGE RACE BLOCK ════════════════════════════════════════════════════════════════════
    public const string ElfBlessing        = "elf_blessing";
    public const string DemonBlessing      = "demon_blessing";
    public const string HumanBlessing      = "human_blessing";
    public const string ElfSelfHeal        = "elf_self_heal";
    public const string DemonOverLimit     = "demon_over_limit";
    public const string HumanVampiricBolt  = "human_vampiric_bolt";

    /// <summary>The one level every blessing is granted at — his three rows all read 7.</summary>
    public const int MageBlessingLevel = 7;

    /// <summary>Learn levels of the Elf's Self Heal — his nine rows, 7 through 74.</summary>
    public static readonly int[] ElfSelfHealLevels = { 7, 20, 30, 40, 48, 58, 64, 70, 74 };
    /// <summary>Learn levels of the Demon's Over the Limit — his five rows, 7 through 70.</summary>
    public static readonly int[] DemonOverLimitLevels = { 7, 20, 40, 60, 70 };
    /// <summary>Learn levels of the Human's Vampiric Bolt ladder — his thirty-three rows, 20 through
    /// 90. Rungs 1-4 are the old `nuker 2nd.csv` cadence, 5-18 the old `nuker 3rd.csv` bands, and
    /// 19-33 one a level across the 4th tier, exactly where they were before the skill changed hands.
    ///
    /// ⚠ NOT TIER-GATED. The 76-90 rungs sit in `mage 1st.csv` like the rest of the block, so they
    /// are gated by LEVEL alone — unlike the `nuker 4th.csv` rows they replaced, which needed the
    /// Rite of Ascension. That is what "the race layer follows you" costs, and it is his placement.</summary>
    public static readonly int[] HumanVampiricLevels =
    {
        20, 25, 30, 35, 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74,
        76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
    };

    private static IEnumerable<SkillDef> MageRaceSkills()
    {
        var list = new List<SkillDef>();

        // ═══ THE THREE BLESSINGS — auto-granted at 7, free, never replaced ══════════════════════
        //
        // 🔑 ONE RUNG EACH and nothing to buy: MP 0, SP 0, "Auto-granted" on all three of his rows.
        //    The grant is in GameLoopService.AutoLearnCoreSkills beside the grade passive, so it
        //    arrives on the level-up that earns it rather than waiting for a relog.
        //
        // ⚠ THE COMMA GROUPS THE PERCENT, which is how his cells read: *"Received HP recovery magic,
        //   M.Atk +5%; Mp regen +10%"* is two effects at 5% and one at 10%, not one at 5%. Same
        //   grammar on the other two. The Human is the odd one — three channels, all at 5%, and
        //   "Natural Regeneration" is BOTH pools (the Elf and the Demon each take one at 10%).
        list.Add(new SkillDef(ElfBlessing, "Forest Blessing", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, TargetMode: TargetMode.SelfOnly, SpCost: 0,
            Passive: new PassiveEffect(HealReceivedPct: 0.05f, MagAtkPct: 0.05f, MpRegenPct: 0.10f),
            Description: "The forest answers an Elf's magic: +5% healing received, +5% M.Atk "
                       + "and +10% MP regeneration."));

        list.Add(new SkillDef(DemonBlessing, "Demonic Blessing", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, TargetMode: TargetMode.SelfOnly, SpCost: 0,
            Passive: new PassiveEffect(MagicCritRate: 0.05f, CastSpeedPct: 0.05f, HpRegenPct: 0.10f),
            Description: "Demon blood runs hot: +5% magic critical rate, +5% cast speed "
                       + "and +10% HP regeneration."));

        list.Add(new SkillDef(HumanBlessing, "Kings Blessing", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, TargetMode: TargetMode.SelfOnly, SpCost: 0,
            Passive: new PassiveEffect(MagicCritDamage: 0.05f, HpRegenPct: 0.05f, MpRegenPct: 0.05f,
                MaxMpPct: 0.05f),
            Description: "A king's favour: +5% magic critical damage, +5% natural regeneration "
                       + "of both pools and +5% Max MP."));

        // ═══ ELF — SELF HEAL, the sustain ladder ════════════════════════════════════════════════
        //
        // 🔑 THE ID `self_heal` BECAME THIS ONE. Same skill, nine rungs instead of three, and the
        //    OTHER TWO RACES NO LONGER HAVE IT AT ALL — that is the change, not the power curve.
        // 🔑 MAGICAL, unlike the fighter's `elf_heal`, which he wrote `Physical/Heal`. His TYPE cell
        //    here reads `Magic/Heal`: it fizzles and it is paced by CAST speed, because a mage has
        //    the WIT to pay for both.
        // ⚠ 5s CAST AND 5s REUSE on every rung — his columns. The old base-mage skill was 5s/2s, so
        //   this is slower to re-use as well as stronger; it is a between-pulls heal, not a combat one.
        int[] healPower = { 60, 100, 200, 300, 400, 500, 600, 700, 800 };
        int[] healMp    = { 14, 30, 44, 62, 76, 95, 105, 114, 120 };
        // ⚠ `3k` / `12k` ARE 3,000 AND 12,000, not the 3,200 / 12,800 the 20-35 tier spells out
        //   elsewhere. The parser reads a `k` as exactly a thousand and the file is the authority,
        //   so the shorthand IS the price. Don't "restore" the old rung values.
        int[] healSp    = { 480, 3_000, 12_000, 36_000, 64_000, 88_000, 190_000, 390_000, 880_000 };
        list.Add(new SkillDef(ElfSelfHeal, "Self Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: healMp[0], CastTicks: 50, CooldownTicks: 50, Range: 0, Power: healPower[0],
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly, SpCost: healSp[0],
            Description: "Restores your own HP.",
            Levels: Enumerable.Range(0, ElfSelfHealLevels.Length).Select(i => new SkillLevel(
                Power: healPower[i], MpCost: healMp[i], SpCost: healSp[i],
                Description: $"Restores your own HP with {healPower[i]} power."))
                .ToArray()));

        // ═══ DEMON — OVER THE LIMIT, the five-second burst ══════════════════════════════════════
        //
        // 🔑 A FIVE-SECOND WINDOW ON A SIXTY-SECOND REUSE, and that shape is the whole skill: his
        //    columns are CAST 0, CD 60, DURR 5. You spend it on the pull that matters, not on
        //    upkeep — which is why the MP is a real nuke's worth at every rung.
        // 🔑 BOTH CHANNELS. `BuffPhysAtk` and `BuffMagAtk` are separate effects since 2026-07-16 (the
        //    shared `BuffAtk` is PHYSICAL only), so "P/M.Atk" needs both magnitudes or half of it is
        //    silently dead on the class that actually casts it.
        // ⚠ ITS OWN BuffKey AND NO COVERED FAMILIES, on purpose. Put it in `atk_phys`/`atk_mag` and
        //   it would fight the Might/Force ladder: a 20-minute party blessing would refuse the burst
        //   (weaker rank) or the burst would evict the blessing for five seconds and leave the mage
        //   naked. A burst is a THIRD source, and the family rule is what says so.
        float[] overPct = { 0.05f, 0.07f, 0.10f, 0.15f, 0.20f };
        int[] overMp    = { 14, 30, 62, 76, 114 };
        int[] overSp    = { 480, 3_000, 36_000, 120_000, 390_000 };
        list.Add(new SkillDef(DemonOverLimit, "Over the Limit", BaseClass.Mage,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk,
            MpCost: overMp[0], CastTicks: 0, CooldownTicks: 600, Range: 0, Power: 0,
            DurationTicks: 50, BuffKey: "demon_over_limit",
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: overSp[0],
            Magnitudes: new[]
            {
                new EffectMagnitude(SkillEffect.BuffPhysAtk, overPct[0]),
                new EffectMagnitude(SkillEffect.BuffMagAtk,  overPct[0]),
            },
            Description: "Push past what the body will take: more P.Atk and M.Atk, briefly.",
            Levels: Enumerable.Range(0, DemonOverLimitLevels.Length).Select(i => new SkillLevel(
                MpCost: overMp[i], SpCost: overSp[i],
                Magnitudes: new[]
                {
                    new EffectMagnitude(SkillEffect.BuffPhysAtk, overPct[i]),
                    new EffectMagnitude(SkillEffect.BuffMagAtk,  overPct[i]),
                },
                Description: $"+{overPct[i] * 100:0}% P.Atk and M.Atk for 5s."))
                .ToArray()));

        // ═══ HUMAN — VAMPIRIC BOLT, the drain ladder ════════════════════════════════════════════
        //
        // 🔴 THE POWER, MP AND RANGE ARE THE OLD `vampiric_bolt` LADDER, RUNG FOR RUNG — rungs 2-34
        //    of it, renumbered 1-33. Nothing about the spell changed; WHO HAS IT did. It was the
        //    Human NUKER's, registered across `nuker 2nd/3rd/4th`; it is now every Human MYSTIC's,
        //    and those rows are deleted from all three files.
        // 🔑 THE LEVEL-14 TASTER SURVIVES UNDER THE OLD ID. `vampiric_bolt` keeps exactly one rung
        //    on the base-mage table (Human only, range 600 now), and the cleric's Holy Bolt
        //    `Replaces` it at 20 — his `cleric 2nd.csv` says so. This ladder starts at 20 and is NOT
        //    replaced by anything: that is the difference between a base-class taster and a race
        //    layer, and it is why he gave the two different ids.
        // ⚠ RANGE IS A LADDER: 750 through the twenties and thirties, 900 from 40 up. His column.
        int[] vampPower = { 26, 32, 38, 44, 52, 58, 65, 72, 78, 82, 85, 89, 92, 96, 99, 102, 105, 108 };
        int[] vampMp    = { 40, 46, 52, 62, 66, 76, 88, 96, 104, 108, 110, 116, 120, 124, 128, 130, 134, 138 };
        int[] vampSp    =
        {
            3_000, 6_000, 12_000, 25_000, 36_000, 43_000, 64_000, 74_000, 81_000,
            88_000, 120_000, 170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
        };
        list.Add(new SkillDef(HumanVampiricBolt, "Vampiric Bolt", BaseClass.Mage,
            SkillEffect.MagicDamage,
            MpCost: vampMp[0], CastTicks: 40, CooldownTicks: 10, Range: 750, Power: vampPower[0],
            Category: SkillCategory.Magic, SpCost: vampSp[0], Lifesteal: 0.40f,
            Description: "A draining bolt that heals you for 40% of the damage dealt.",
            Levels: Enumerable.Range(0, vampPower.Length).Select(i => new SkillLevel(
                Power: vampPower[i], MpCost: vampMp[i], SpCost: vampSp[i],
                Range: HumanVampiricLevels[i] >= 40 ? 900f : 750f,
                Description: $"Drain power {vampPower[i]}; heals 40% of damage."))
                // Rungs 19-33 are the 4th-tier ladder the nuker already had — same fifteen rows,
                // same prices, same gold. Shared rather than re-typed: `NukerFourthVampiricRungs`
                // reads the same `NukerBlastPower4` / `NukerHeavyMp4` arrays its neighbours do.
                .Concat(NukerFourthVampiricRungs()).ToArray()));

        return list;
    }

    /// <summary>The blessing a mage of this race is auto-granted at level 7, or null for a fighter.
    /// One place, read by both the central injector and <c>AutoLearnCoreSkills</c>, so the skill a
    /// character is GIVEN can never be a different one from the skill his window lists.</summary>
    public static string? MageBlessingFor(Race race) => race switch
    {
        Race.Elf => ElfBlessing,
        Race.Demon => DemonBlessing,
        Race.Human => HumanBlessing,
        _ => null,
    };
}
