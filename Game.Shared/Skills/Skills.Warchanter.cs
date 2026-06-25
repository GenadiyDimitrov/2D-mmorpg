namespace Game.Shared;

/// <summary>Warchanter — 3rd-class Healer discipline (lvl 40): the buffer. Per-race
/// kits (same magnitudes, race-flavoured names): a mega party buff, a party heal +
/// heal-over-time, the buffer's only direct nuke, and a per-race passive lean.
/// (Who learns these, and when, is in RaceAndClasses/ClassSkillTables.Third.cs.)</summary>
public static partial class SkillCatalog
{
    public const string WcHumanBolt = "wc_human_bolt";
    public const string WcHumanChant = "wc_human_chant";
    public const string WcHumanRenew = "wc_human_renew";
    public const string WcHumanPass = "wc_human_pass";
    public const string WcElfBolt = "wc_elf_bolt";
    public const string WcElfChant = "wc_elf_chant";
    public const string WcElfRenew = "wc_elf_renew";
    public const string WcElfPass = "wc_elf_pass";
    public const string WcOrkBolt = "wc_ork_bolt";
    public const string WcOrkChant = "wc_ork_chant";
    public const string WcOrkRenew = "wc_ork_renew";
    public const string WcOrkPass = "wc_ork_pass";

    // ---- Warchanter kit factories (same numbers per race; names differ) ----
    private static SkillDef WcChant(string id, string name) => new(
        id, name, BaseClass.Mage,
        SkillEffect.BuffAtk | SkillEffect.BuffDef | SkillEffect.BuffMagicDef
        | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
        | SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen,
        MpCost: 60, CastTicks: 20, CooldownTicks: 50, Range: 0, Power: 0,
        DurationTicks: 12000, BuffKey: "wc_chant", Rank: 1,
        Magnitudes: new EffectMagnitude[]
        {
            new(SkillEffect.BuffAtk, 0.15f), new(SkillEffect.BuffDef, 0.15f),
            new(SkillEffect.BuffMagicDef, 0.30f),
            new(SkillEffect.BuffCastSpeed, 0.30f), new(SkillEffect.BuffAtkSpeed, 0.30f),
            new(SkillEffect.BuffMoveSpeed, 45f, ModifierMode.Flat),
            new(SkillEffect.BuffHp, 0.35f), new(SkillEffect.BuffMp, 0.35f),
            new(SkillEffect.BuffHpRegen, 0.20f), new(SkillEffect.BuffMpRegen, 0.20f),
        },
        Category: SkillCategory.Buff, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
        SpCost: 500,
        Description: "Party: +35% max HP/MP, +30% magic def & cast/attack speed, +15% atk/def, +move & regen.");

    private static SkillDef WcRenew(string id, string name) => new(
        id, name, BaseClass.Mage, SkillEffect.Heal | SkillEffect.HealOverTime,
        MpCost: 70, CastTicks: 20, CooldownTicks: 300, Range: 0, Power: 150,
        DurationTicks: 100, BuffKey: "wc_renew", Rank: 1,
        Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 0.02f) },
        Category: SkillCategory.Heal, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
        SpCost: 500,
        Description: "Party: an instant heal plus 2% max HP per second for 10s.");

    private static SkillDef WcBolt(string id, string name) => new(
        id, name, BaseClass.Mage, SkillEffect.MagicDamage,
        // A proper single-target nuke: ~4s base cast (WIT/gear/buffs shorten it), real power.
        MpCost: 50, CastTicks: 40, CooldownTicks: 20, Range: 750, Power: 120,
        Replaces: new[] { MagicBolt, HolyStrike },   // 3rd-class nuke replaces the lower ones
        Category: SkillCategory.Magic, SpCost: 500,
        Description: "A heavy single-target nuke (replaces Magic Bolt / Holy Strike).");

    private static SkillDef[] WarchanterSkills() => new SkillDef[]
    {
        // Mega party buff (same magnitudes all races; names differ per race).
        WcChant(WcHumanChant, "Grand Anthem"),
        WcChant(WcElfChant, "Sylvan Anthem"),
        WcChant(WcOrkChant, "War Anthem"),
        // Party heal + heal-over-time.
        WcRenew(WcHumanRenew, "Renewing Verse"),
        WcRenew(WcElfRenew, "Dawn Verse"),
        WcRenew(WcOrkRenew, "Spirit Verse"),
        // Single-target magic nuke (the buffer's only direct damage).
        WcBolt(WcHumanBolt, "Arcane Lance"),
        WcBolt(WcElfBolt, "Starlight Lance"),
        WcBolt(WcOrkBolt, "Spirit Lance"),
        // Passives (per-race lean; v1 is a flat caster set — robe/light conditional comes in P1).
        new(WcHumanPass, "Resonance", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicCritRate: 0.05f),
            Description: "Passive. +10% max MP, +MP regen, +5% magic crit."),
        new(WcElfPass, "Harmony", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, CastSpeedPct: 0.08f),
            Description: "Passive. +10% max MP, +MP regen, +8% cast speed."),
        new(WcOrkPass, "Totemic Bond", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, AttackPct: 0.08f),
            Description: "Passive. +10% max MP, +MP regen, +8% attack (feeds spells)."),
    };
}
