namespace Game.Shared;

/// <summary>Lightbringer — 3rd-class Healer discipline (lvl 40): the pure healer.
/// Same idea (keep the party alive) expressed three ways by race — Human single-
/// target throughput, Elf area coverage + control, Ork font/anti-heal — plus the
/// shared party buff + passive that complete the kit.
/// (Who learns these, and when, is in RaceAndClasses/ClassSkillTables.Third.cs.)</summary>
public static partial class SkillCatalog
{
    // Shared kit (buff + passive).
    public const string LbBlessing = "lb_blessing";
    public const string LbDevotion = "lb_devotion";   // passive
    // Per-race spells.
    public const string LbHumanMend = "lb_human_mend";     // strong fast single heal
    public const string LbHumanPurify = "lb_human_purify"; // cleanse an ally
    public const string LbElfDawn = "lb_elf_dawn";         // AoE heal + cleanse
    public const string LbElfWarden = "lb_elf_warden";     // root enemy + self de-taunt
    public const string LbOrkFont = "lb_ork_font";         // AoE heal (totem stand-in)
    public const string LbOrkSap = "lb_ork_sap";           // anti-heal debuff

    private static SkillDef[] LightbringerSkills() => new SkillDef[]
    {
        // ----- Shared: party buff + passive that complete the 4-skill kit -----
        new(LbBlessing, "Blessing of Light", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffDef,
            MpCost: 50, CastTicks: 20, CooldownTicks: 30, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "lb_blessing", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, 0.15f), new(SkillEffect.BuffDef, 0.15f),
            },
            Category: SkillCategory.Buff, TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600,
            SpCost: 500, Description: "Party: +15% max HP and +15% defence."),
        new(LbDevotion, "Devotion", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, SpCost: 500,
            Passive: new PassiveEffect(MaxMpPct: 0.10f, MpRegen: 2f, MagicDefence: 10),
            Description: "Passive. +10% max MP, +MP regen, +10 magic defence."),

        // --- Human: strong, fast single-target heal + cleanse ---
        new(LbHumanMend, "Mending Light", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 42, CastTicks: 22, CooldownTicks: 12, Range: 500, Power: 230,
            Category: SkillCategory.Heal, SpCost: 6000,
            Description: "A swift, powerful heal on a single ally. The Human " +
                         "Lightbringer's hallmark: more single-target throughput, less AoE."),
        new(LbHumanPurify, "Purify", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 24, CastTicks: 8, CooldownTicks: 50, Range: 500, Power: 0,
            Category: SkillCategory.Heal, SpCost: 6000,
            Description: "Removes harmful effects (curses, anti-heal, roots) from an ally."),

        // --- Elf: AoE heal + cleanse, and a control/utility tool ---
        new(LbElfDawn, "Dawn Bloom", BaseClass.Mage, SkillEffect.Heal | SkillEffect.Cleanse,
            MpCost: 60, CastTicks: 30, CooldownTicks: 40, Range: 0, Power: 120,
            Category: SkillCategory.Heal, SpCost: 6000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            Description: "Heals AND cleanses all nearby allies. The Elf Lightbringer " +
                         "trades single-target power for area coverage."),
        new(LbElfWarden, "Warding Step", BaseClass.Mage, SkillEffect.Root | SkillEffect.Detaunt,
            MpCost: 30, CastTicks: 6, CooldownTicks: 120, Range: 500, Power: 0,
            DurationTicks: 80, BuffKey: "root", Rank: 1,
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Holds an enemy in place for 8s and sheds the caster's aggro " +
                         "from nearby foes (they look elsewhere)."),

        // --- Ork: AoE heal (totem stand-in for now) + anti-heal debuff ---
        new(LbOrkFont, "Spirit Font", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 55, CastTicks: 28, CooldownTicks: 40, Range: 0, Power: 110,
            Category: SkillCategory.Heal, SpCost: 6000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            Description: "Calls a font of spirit energy that heals nearby allies. " +
                         "(A placed totem will replace this when summons arrive.)"),
        new(LbOrkSap, "Soul Sap", BaseClass.Mage, SkillEffect.DebuffHealRecv,
            MpCost: 28, CastTicks: 8, CooldownTicks: 150, Range: 500, Power: 0,
            DurationTicks: 150, BuffKey: "antiheal", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.50f) },
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Curses an enemy so it recovers only half the HP from any " +
                         "healing for 15s."),
    };
}
