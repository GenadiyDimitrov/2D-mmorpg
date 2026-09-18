namespace Game.Shared;

/// <summary>Warchanter — 3rd-class Healer discipline (lvl 40): the buffer. Per-race
/// kits (same magnitudes, race-flavoured names): a mega party buff, a party heal +
/// heal-over-time, the buffer's only direct nuke, and a per-race passive lean.
/// (Who learns these, and when, is in RaceAndClasses/ClassSkillTables.Third.cs.)</summary>
public static partial class SkillCatalog
{
    public const string ArcaneLance = "arcane_lance";
    public const string GrandAnthem = "grand_anthem";
    public const string RenewingVerse = "renewing_verse";
    public const string Resonance = "resonance";
    public const string StarlightLance = "starlight_lance";
    public const string SylvanAnthem = "sylvan_anthem";
    public const string DawnVerse = "dawn_verse";
    public const string Harmony = "harmony";
    public const string SpiritLance = "spirit_lance";
    public const string WarAnthem = "war_anthem";
    public const string SpiritVerse = "spirit_verse";
    public const string TotemicBond = "totemic_bond";

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
        DurationTicks: 100, BuffKey: "wc_renew", Rank: 1, CountsTowardBuffLimit: false,   // a heal, not a blessing
        Magnitudes: new EffectMagnitude[] { new(SkillEffect.HealOverTime, 0.02f) },
        Category: SkillCategory.Heal, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
        SpCost: 500,
        Description: "Party: an instant heal plus 2% max HP per second for 10s.");

    private static SkillDef WcBolt(string id, string name) => new(
        id, name, BaseClass.Mage, SkillEffect.MagicDamage,
        // A proper single-target nuke: ~4s base cast (WIT/gear/buffs shorten it), real power.
        MpCost: 50, CastTicks: 40, CooldownTicks: 20, Range: 750, Power: 120,
        Replaces: new[] { MagicBolt, HolyBolt },   // 3rd-class nuke replaces the lower ones
        Category: SkillCategory.Magic, SpCost: 500,
        Description: "A heavy single-target nuke (replaces Magic Bolt / Holy Strike).");

    private static SkillDef[] WarchanterSkills() => new SkillDef[]
    {
        // Mega party buff (same magnitudes all races; names differ per race).
        WcChant(GrandAnthem, "Grand Anthem"),
        WcChant(SylvanAnthem, "Sylvan Anthem"),
        WcChant(WarAnthem, "War Anthem"),
        // Party heal + heal-over-time.
        WcRenew(RenewingVerse, "Renewing Verse"),
        WcRenew(DawnVerse, "Dawn Verse"),
        WcRenew(SpiritVerse, "Spirit Verse"),
        // Single-target magic nuke (the buffer's only direct damage).
        WcBolt(ArcaneLance, "Arcane Lance"),
        WcBolt(StarlightLance, "Starlight Lance"),
        WcBolt(SpiritLance, "Spirit Lance"),
        // Passives (per-race lean; v1 is a flat caster set — robe/light conditional comes in P1).
        new(Resonance, "Resonance", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            // MagicCritRate is a MULTIPLIER (0.20 = ×1.2), not +20 points — the same convention
            // PassiveEffect.CritRate uses. It was +5 flat POINTS, which on the old 2% WIT base was
            // 2.5× the entire rate and made a human Warchanter the biggest magic-crit source in
            // the game; as a ×1.2 it is a buffer's nudge, which is all it was ever meant to be.
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicCritRate: 0.20f),
            Description: "Passive. +10% max MP, +MP regen, ×1.2 magic crit rate."),
        new(Harmony, "Harmony", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, CastSpeedPct: 0.08f),
            Description: "Passive. +10% max MP, +MP regen, +8% cast speed."),
        new(TotemicBond, "Totemic Bond", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, AttackPct: 0.08f),
            Description: "Passive. +10% max MP, +MP regen, +8% attack (feeds spells)."),
    };
}
