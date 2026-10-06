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
/// mage's day job already (a cure, a self-heal), so his mage block answers a different question. Since
/// 2026-09-29 it is ONE DAMAGE ANSWER PER RACE from level 14: the Human the DRAIN (Vampiric Bolt), the
/// Elf the SLOW (Frost Spikes — the Elf nuker's spell, now every Elf mystic's), the Demon a 10-second
/// OFFENCE burst (Over the Limit) he spends on the pull that matters.</para>
///
/// <para>🔑 <b>AND EVERY RACE GETS A BLESSING AT 7</b>, auto-granted and free: three always-on
/// passives, one per race, which is where the "what does my race do for me" question gets its answer
/// for a class whose kit is otherwise identical across the three.</para>
///
/// <para>🔴 <b>THE ELF SELF HEAL IS GONE</b> (2026-09-29, his row deletion — it made way for Frost
/// Spikes). `elf_self_heal` was the old base-mage `self_heal` re-authored; neither id exists now, and
/// the healer's `Heal` no longer replaces anything.</para></summary>
public static partial class SkillCatalog
{
    // ═══ THE MAGE RACE BLOCK ════════════════════════════════════════════════════════════════════
    public const string ElfBlessing        = "elf_blessing";
    public const string DemonBlessing      = "demon_blessing";
    public const string HumanBlessing      = "human_blessing";
    public const string DemonOverLimit     = "demon_over_limit";
    public const string HumanVampiricBolt  = "human_vampiric_bolt";

    // ═══ THE RACIAL MIGHTS ARE FACES NOW (`BL-327`, 2026-09-30) ══════════════════════════════════
    // Until 0.217.0 they were three wrapper SKILLS (`elf_/demon_/human_cast_atk_phys`, `BL-263`) that
    // carried nothing but a name, a description and an icon over rung 1 of `cast_atk_phys`. His ruling:
    // *"merge them as one ill split them in the file as faces"* — so a mage learns `cast_atk_phys` rung 1
    // at 7 (ClassSkillTables) and Forest Might / Demonic Strength / Blessing of Might are three rows of
    // docs/data/skill_faces.csv. The face follows the CASTER, so it reads right on anyone he buffs.

    /// <summary>The one level every blessing is granted at — his three rows all read 7.</summary>
    public const int MageBlessingLevel = 7;

    /// <summary>Learn levels of the Demon's Over the Limit — his four rows, 14 through 70 (reworked 2026-09-29).</summary>
    public static readonly int[] DemonOverLimitLevels = { 14, 40, 60, 70 };
    /// <summary>Learn levels of the Human's Vampiric Bolt ladder — his thirty-four rows, 14 through
    /// 90. Rung 1 is the level-14 taster (its own `vampiric_bolt` id until 2026-09-29), 2-5 the old
    /// `nuker 2nd.csv` cadence, 6-19 the old `nuker 3rd.csv` bands, and
    /// 20-34 one a level across the 4th tier, exactly where they were before the skill changed hands.
    ///
    /// ⚠ NOT TIER-GATED. The 76-90 rungs sit in `mage 1st.csv` like the rest of the block, so they
    /// are gated by LEVEL alone — unlike the `nuker 4th.csv` rows they replaced, which needed the
    /// Rite of Ascension. That is what "the race layer follows you" costs, and it is his placement.</summary>
    public static readonly int[] HumanVampiricLevels =
    {
        14, 20, 25, 30, 35, 40, 44, 48, 52, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74,
        76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90,
    };
    /// <summary>Learn levels of the Elf's Frost Spikes — the SAME thirty-four the Human's drain runs on
    /// (2026-09-29: the nuker's Elf spell moved into the race block and grew 14/20/25/30/35 rungs
    /// under it). Its own name so the two can part the day one of his rows does.</summary>
    public static int[] ElfFrostSpikesLevels => HumanVampiricLevels;

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

        // ═══ DEMON — OVER THE LIMIT, ten seconds of pure havoc ══════════════════════════════════
        //
        // 🔑 REWORKED 2026-09-29 to answer the other two races' DAMAGE SPELLS (the Elf's Frost
        //    Spikes, the Human's Vampiric Bolt, both from 14): his four rows open at 14, not 7, the
        //    window is 10s instead of 5, and the burst now carries P/M crit rate and attack/cast
        //    speed beside the P/M.Atk. Still CAST 0, CD 60 — a window you spend, not upkeep.
        // 🔑 BOTH CHANNELS. `BuffPhysAtk` and `BuffMagAtk` are separate effects since 2026-07-16 (the
        //    shared `BuffAtk` is PHYSICAL only), so "P/M.Atk" needs both magnitudes or half of it is
        //    silently dead on the class that actually casts it. Same for the two crit rates.
        // ⚠ ITS OWN BuffKey AND NO COVERED FAMILIES, on purpose. Put it in `atk_phys`/`atk_mag` and
        //   it would fight the Might/Force ladder: a 20-minute party blessing would refuse the burst
        //   (weaker rank) or the burst would evict the blessing for ten seconds and leave the mage
        //   naked. A burst is a THIRD source, and the family rule is what says so.
        float[] overAtk  = { 0.10f, 0.12f, 0.15f, 0.20f };
        float[] overRest = { 0.05f, 0.06f, 0.08f, 0.10f };
        int[] overMp     = { 14, 62, 76, 114 };
        int[] overSp     = { 480, 36_000, 120_000, 390_000 };
        EffectMagnitude[] OverMags(int i) => new EffectMagnitude[]
        {
            new(SkillEffect.BuffPhysAtk,       overAtk[i]),
            new(SkillEffect.BuffMagAtk,        overAtk[i]),
            new(SkillEffect.BuffCritRate,      overRest[i]),
            new(SkillEffect.BuffMagicCritRate, overRest[i]),
            new(SkillEffect.BuffAtkSpeed,      overRest[i]),
            new(SkillEffect.BuffCastSpeed,     overRest[i]),
        };
        list.Add(new SkillDef(DemonOverLimit, "Over the Limit", BaseClass.Mage,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk | SkillEffect.BuffCritRate
                | SkillEffect.BuffMagicCritRate | SkillEffect.BuffAtkSpeed | SkillEffect.BuffCastSpeed,
            MpCost: overMp[0], CastTicks: 0, CooldownTicks: 600, Range: 0, Power: 0,
            // FIXED reuse (owner, 2026-10-06): fully buffed, reuse cuts had it up 10s of every 25s.
            FixedCooldown: true,
            DurationTicks: 100, BuffKey: "demon_over_limit",
            // 2026-10-02, owner: *"over the limit should not go towards the buff limit ... its a 10s buff"*.
            // It is on a shelf, which puts it in BuffLimitIds; this is the authored veto.
            CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: overSp[0],
            Magnitudes: OverMags(0),
            Description: "Push past what the body will take: ten seconds of pure havoc.",
            Levels: Enumerable.Range(0, DemonOverLimitLevels.Length).Select(i => new SkillLevel(
                MpCost: overMp[i], SpCost: overSp[i], Magnitudes: OverMags(i),
                Description: $"+{overAtk[i] * 100:0}% P.Atk and M.Atk; +{overRest[i] * 100:0}% P/M crit rate, "
                           + $"attack and cast speed for 10s."))
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
        // ⚠ RANGE: 600 at 14, 750 from 20 up. The 40+ rungs were 900 until 2026-09-29 — now Holy Bolt's 750:
        //   *"healers/buffer wont have nukers range .. and nukers wont overuse the additional spell"*.
        int[] vampPower = { 21, 26, 32, 38, 44, 52, 58, 65, 72, 78, 82, 85, 89, 92, 96, 99, 102, 105, 108 };
        int[] vampMp    = { 28, 40, 46, 52, 62, 66, 76, 88, 96, 104, 108, 110, 116, 120, 124, 128, 130, 134, 138 };
        int[] vampSp    =
        {
            2_000, 3_000, 6_000, 12_000, 25_000, 36_000, 43_000, 64_000, 74_000, 81_000,
            88_000, 120_000, 170_000, 190_000, 280_000, 320_000, 390_000, 650_000, 880_000,
        };
        list.Add(new SkillDef(HumanVampiricBolt, "Vampiric Bolt", BaseClass.Mage,
            SkillEffect.MagicDamage,
            MpCost: vampMp[0], CastTicks: 40, CooldownTicks: 60, Range: 600, Power: vampPower[0],
            FixedCooldown: true,   // owner, 2026-10-07: 6s FIXED: *"higher dmg + vamp == longer cd"*, useful but not spammable
            Category: SkillCategory.Magic, SpCost: vampSp[0], Lifesteal: 0.40f,
            // ⚠ PVP POWER ×0.5 (owner, 2026-10-06): *"its a helping in farm not in pvp .. one race/class can be
            // stronger in pve than other .. but the pvp should be balanced"*. The race bolt every human mage keeps;
            // without the cut a healer simply swapped to it once Holy Ray was halved (0.229.3).
            PvpDamageMult: 0.5f,
            Description: "A draining bolt that heals you for 40% of the damage dealt. Half power against players.",
            Levels: Enumerable.Range(0, vampPower.Length).Select(i => new SkillLevel(
                Power: vampPower[i], MpCost: vampMp[i], SpCost: vampSp[i],
                Range: HumanVampiricLevels[i] >= 20 ? 750f : 600f,
                Description: $"Drain power {vampPower[i]}; heals 40% of damage. Half power in PvP."))
                // Rungs 20-34 are the 4th-tier ladder the nuker already had — same fifteen rows,
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
