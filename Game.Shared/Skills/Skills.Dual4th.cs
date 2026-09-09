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
    /// <summary>@80, +40% blow rate. Mutually exclusive with <see cref="BrutalStrike"/>.</summary>
    public const string PerfectStrike = "perfect_strike";
    /// <summary>@80, +30% physical crit damage. Mutually exclusive with <see cref="PerfectStrike"/>.</summary>
    public const string BrutalStrike = "brutal_strike";

    /// <summary>The shared family the @80 pair competes on — the reason neither can be held with the
    /// other. Named for what it is rather than for either skill, exactly as `great_blessing` is.</summary>
    private const string StrikeChoiceKey = "dagger_strike_choice";

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

        var perfect = Choice(PerfectStrike, "Perfect Strike", 0.40f, 0f,
            "Five minutes in which almost nothing you swing at is missed. Does not stack with "
          + "Brutal Strike — you carry one of the two, never both. Requires duals.",
            "5 min: blow landing rate ×1.40. Replaces Brutal Strike.");

        var brutal = Choice(BrutalStrike, "Brutal Strike", 0f, 0.30f,
            "Five minutes in which what does land is ruinous. Does not stack with Perfect Strike — "
          + "you carry one of the two, never both. Requires duals.",
            "5 min: +30 crit damage. Replaces Perfect Strike.");

        return new[] { instinct, perfect, brutal };
    }
}
