namespace Game.Shared;

/// <summary>
/// THE ALL-CLASSES 4th-TIER KIT — `docs/data/classes_skills_csv/shared 4th.csv`, his "ALL CLASSES"
/// block (2026-08-26): *"i created a Shared 4th file that all classes share the same skills - every
/// class get to learn them"*.
///
/// <para>Five passives, in two price bands he authored in that file's header:
/// <list type="bullet">
///   <item><b>76 — Strong Mind / Strong Body</b>, 6.5kk SP + 1kk gold. The two halves of debuff
///         resistance, split by the school that defends them: SPT for magical, CON for physical.</item>
///   <item><b>83 — Arcane Protection / Magic Proficiency / Physical Proficiency</b>, 500kk SP + 100kk
///         gold. ⚠ 500kk, not the 6.500kk his healer file carried before he moved this block out —
///         he corrected it in the split, and 6.5kk was clearly a stray copy of the 76 row's price.</item>
/// </list></para>
///
/// <para><b>Every class learns all five</b>, including the two "proficiency" rows that read as though
/// one is for casters and one for fighters. They are not gated on class: Physical Proficiency simply
/// does nothing without a weapon it names, and Magic Proficiency's +5% M.Atk is worth little to a
/// warrior — the row itself is the filter, which is how the weapon masteries already work.</para>
///
/// <para>The SIGILS are the other half of this file's CSV and live in Skills.Sigils.cs; who learns
/// what and when is <c>ClassSkillTables.Fourth.cs</c>.</para>
/// </summary>
public static partial class SkillCatalog
{
    public const string StrongBody          = "strong_body";
    public const string StrongMind          = "strong_mind";
    public const string ArcaneProtection    = "arcane_protection";
    public const string MagicProficiency    = "magic_proficiency";
    public const string PhysicalProficiency = "physical_proficiency";

    /// <summary>His two shared-4th price bands, so the learn table never restates them.</summary>
    public const int Shared4thSp76   =   6_500_000;
    public const int Shared4thGold76 =   1_000_000;
    public const int Shared4thSp83   = 500_000_000;
    public const int Shared4thGold83 = 100_000_000;

    // The two proc payloads. Same rules as the sigils': fixed timings, no buff slot.
    private const string ArcaneProtectionWard = "arcane_protection_ward";
    private const string PhysProwessSurge  = "physical_proficiency_surge";

    private static SkillDef[] Shared4thSkills() => new SkillDef[]
    {
        // ═══ 76 ══════════════════════════════════════════════════════════════════════════════════
        // ⚠ THE NAMES SWAPPED on 2026-08-26 — he had them the other way round and corrected it: STRONG
        //    MIND is the one that resists SPIRIT debuffs and STRONG BODY the one that resists
        //    CONSTITUTION debuffs, which is the reading anyone would have guessed. The VALUES did not
        //    move with the names: SPT keeps 20% and CON keeps 10%.
        new(StrongMind, "Strong Mind", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: Shared4thSp76,
            Levels: new[] { new SkillLevel(SpCost: Shared4thSp76, GoldCost: Shared4thGold76,
                Passive: new PassiveEffect(CcResistMagical: 0.20f),
                Description: "Debuffs that contest your Spirit are 20% less likely to land on you.") },
            Description: "Debuffs that contest your Spirit are 20% less likely to land on you."),

        new(StrongBody, "Strong Body", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: Shared4thSp76,
            Levels: new[] { new SkillLevel(SpCost: Shared4thSp76, GoldCost: Shared4thGold76,
                Passive: new PassiveEffect(CcResistPhysical: 0.10f),
                Description: "Debuffs that contest your Constitution are 10% less likely to land on you.") },
            Description: "Debuffs that contest your Constitution are 10% less likely to land on you."),

        // ═══ 83 ══════════════════════════════════════════════════════════════════════════════════
        // ---- ARCANE PROTECTION (his "Strong Spirit" until 2026-08-26): a flat +15% M.Def, plus a defensive proc that only MAGIC can trigger.
        //      His comment column on this row: *"Flat Increase not mutliplied by buffs"* — hence the
        //      +1000 riding as a FLAT BuffMagicDef magnitude rather than a percent, so a Ward or a
        //      robe set cannot multiply it.
        new(ArcaneProtection, "Arcane Protection", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: Shared4thSp83,
            ProcChance: 0.20f, ProcOnDamaged: true, ProcMagicOnly: true, ProcCooldownTicks: 300,
            ProcSelfRungs: new[] { ArcaneProtectionWard },
            Levels: new[] { new SkillLevel(SpCost: Shared4thSp83, GoldCost: Shared4thGold83,
                Passive: new PassiveEffect(MagicDefencePct: 0.15f),
                Description: "M.Def +15%. When magic damage reaches you, a 20% chance to gain a "
                           + "further +1000 M.Def for 10 seconds.") },
            Description: "M.Def +15%. When magic damage reaches you, a 20% chance to gain a further "
                       + "+1000 M.Def for 10 seconds."),

        // ---- MAGIC PROFICIENCY. ⚠ MagAtkPct is authored as the HONEST effective percent: the passive
        //      path squares it to cancel the √ inside the magic formula (see Entity.RecomputeDerived),
        //      so 0.05 really is +5% magic damage and must not be pre-doubled.
        //      The MP reduction takes BOTH channels — *"decrease MP consumation"* names neither.
        new(MagicProficiency, "Magic Proficiency", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: Shared4thSp83,
            Levels: new[] { new SkillLevel(SpCost: Shared4thSp83, GoldCost: Shared4thGold83,
                Passive: new PassiveEffect(MagAtkPct: 0.05f, CastSpeedPct: 0.05f,
                                           PhysMpCostPct: 0.05f, MagicMpCostPct: 0.05f),
                Description: "M.Atk +5%, casting speed +5%, and every skill costs 5% less MP.") },
            Description: "M.Atk +5%, casting speed +5%, and every skill costs 5% less MP."),

        // ---- PHYSICAL PROFICIENCY: weapon-CONDITIONAL, so it rides WeaponMasteryLevels rather than a
        //      plain Passive — a bow gets accuracy and reach, everything else gets power and speed, an
        //      empty hand gets nothing. The proc is common to all four and is gated by RequiredWeapon,
        //      which is what keeps a caster holding a staff out of it.
        new(PhysicalProficiency, "Physical Proficiency", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: Shared4thSp83,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt | WeaponType.Dual | WeaponType.Bow,
            ProcChance: 0.05f, ProcCooldownTicks: 300,
            ProcSelfRungs: new[] { PhysProwessSurge },
            WeaponMasteryLevels: new[]
            {
                new WeaponMasteryProfile(
                    Sword: new PassiveEffect(PhysAtk: 100, AtkSpeedPct: 0.10f),
                    Blunt: new PassiveEffect(PhysAtk: 100, AtkSpeedPct: 0.10f),
                    Dual:  new PassiveEffect(PhysAtk: 100, AtkSpeedPct: 0.10f),
                    Bow:   new PassiveEffect(Accuracy: 8, BowRange: 50f)),
            },
            Levels: new[] { new SkillLevel(SpCost: Shared4thSp83, GoldCost: Shared4thGold83,
                Description: "Blunt / sword / dual: P.Atk +100 and attack speed +10%. Bow: accuracy "
                           + "+8 and range +50. With any of them, a 5% chance on attack to raise your "
                           + "critical rate and physical skill damage by 20% for 10 seconds.") },
            Description: "Blunt / sword / dual: P.Atk +100 and attack speed +10%. Bow: accuracy +8 "
                       + "and range +50. With any of them, a 5% chance on attack to raise your "
                       + "critical rate and physical skill damage by 20% for 10 seconds."),

        // ═══ THE PROC PAYLOADS ═══════════════════════════════════════════════════════════════════
        new(ArcaneProtectionWard, "Arcane Protection", BaseClass.Fighter, SkillEffect.BuffMagicDef,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: "arcane_protection_ward", Rank: 1,
            // FLAT, per his comment — no buff may multiply it.
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffMagicDef, 1000f, ModifierMode.Flat) },
            FixedCooldown: true, CountsTowardBuffLimit: false,
            Description: "M.Def +1000."),

        // "P.Skill.Power +20%" is the PHYSICAL-SKILL damage channel, both contexts — the 2×3 matrix's
        // PvE and PvP skill cells. Basic attacks are deliberately not in it: he wrote *skill* power.
        new(PhysProwessSurge, "Physical Proficiency", BaseClass.Fighter,
            SkillEffect.BuffCritRate | SkillEffect.BuffPveSkillDamage | SkillEffect.BuffPvpSkillDamage,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: "physical_proficiency_surge", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffCritRate,        0.20f, ModifierMode.Percent),
                new(SkillEffect.BuffPveSkillDamage,  0.20f, ModifierMode.Percent),
                new(SkillEffect.BuffPvpSkillDamage,  0.20f, ModifierMode.Percent),
            },
            FixedCooldown: true, CountsTowardBuffLimit: false,
            Description: "Critical rate +20% and physical skill damage +20%."),
    };
}
