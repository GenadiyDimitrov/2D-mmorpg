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
    public const string LbOrkFont = "lb_ork_font";         // the Healing Totem
    public const string LbOrkSap = "lb_ork_sap";           // anti-heal debuff
    // ----- His authored level-40 block (`healer 3rd.csv`, 2026-08-17) -----
    // Shared upgrades: each REPLACES its 2nd-class original rather than stacking beside it.
    public const string HolyRay = "holy_ray";              // replaces Holy Bolt
    public const string GreatHeal = "great_heal";          // replaces Heal
    public const string PartyGreatHeal = "party_great_heal"; // replaces Party Heal
    public const string Conceal = "conceal";               // self-only mob stealth
    // One control debuff per race, all contested ATK vs SPT.
    public const string LbHumanGravity = "lb_human_gravity";  // slows attack + cast
    public const string LbElfBind = "lb_elf_bind";            // 30s hold
    public const string LbOrkArmorBreak = "lb_ork_armor_break"; // shreds P.Def / M.Def

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

        // ----- His shared level-40 upgrades. Each REPLACES its 2nd-class original, which is what the
        //       CSV's REPLACES column says and what keeps the bar from filling with obsolete rows. -----
        new(HolyRay, "Holy Ray", BaseClass.Mage, SkillEffect.MagicDamage,
            MpCost: 30, CastTicks: 25, CooldownTicks: 10, Range: 600, Power: 42,
            Category: SkillCategory.Magic, InitialMpCost: 0, SpCost: 36000,
            Replaces: new[] { HolyStrike },
            Description: "The healer's attack spell: faster and stronger than Holy Bolt, at shorter range."),
        new(GreatHeal, "Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 62, CastTicks: 50, CooldownTicks: 20, Range: 600, Power: 400,
            Category: SkillCategory.Heal, InitialMpCost: 13, SpCost: 36000,
            Replaces: new[] { Heal },
            Description: "Heals a single ally for 400."),
        new(PartyGreatHeal, "Party Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 124, CastTicks: 70, CooldownTicks: 50, Range: 600, Power: 320,
            Category: SkillCategory.Heal, InitialMpCost: 26, SpCost: 36000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 600f,
            Replaces: new[] { PartyHeal },
            Description: "Heals you and nearby party members for 320."),
        // Conceal — the SELF-only twin of Shrouding Hymn: same promise (only mobs that have not already
        // noticed you), a third of the duration, and no party to carry. 100 MP with none of it up front,
        // exactly as authored — you commit the whole cost on the finish.
        new(Conceal, "Conceal", BaseClass.Mage, SkillEffect.None,
            MpCost: 100, CastTicks: 50, CooldownTicks: 100, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "conceal", Rank: 1, InitialMpCost: 0,
            Category: SkillCategory.Buff, SpCost: 36000,
            TargetMode: TargetMode.SelfOnly, GrantsMobStealth: true,
            Description: "For 30s, monsters that haven't already noticed you leave you alone. " +
                         "Anything already chasing you keeps chasing."),

        // --- Human: strong, fast single-target heal + the Gravity debuff ---
        //
        // 🔑 `LbHumanMend` IS his "Quick Great Heal" — same race, same level 40, same job (the Human's
        // single-target throughput answer), so the id is reused and retuned to his numbers rather than
        // retired next to a near-duplicate. It shipped as "Mending Light" at power 230.
        new(LbHumanMend, "Quick Great Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 93, CastTicks: 20, CooldownTicks: 10, Range: 600, Power: 400,
            Category: SkillCategory.Heal, InitialMpCost: 19, SpCost: 36000,
            Replaces: new[] { QuickHeal },
            Description: "A fast, powerful heal on a single ally. The Human Lightbringer's " +
                         "hallmark: more single-target throughput, less area coverage."),
        new(LbHumanGravity, "Gravity", BaseClass.Mage,
            SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "gravity", Rank: 1, InitialMpCost: 7,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.23f), new(SkillEffect.DebuffCastSpeed, 0.12f),
            },
            Description: "Weighs an enemy down for 30s: −23% attack speed and −12% cast speed."),
        new(LbHumanPurify, "Purify", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 24, CastTicks: 8, CooldownTicks: 50, Range: 500, Power: 0,
            Category: SkillCategory.Heal, SpCost: 6000,
            Description: "Removes harmful effects (curses, anti-heal, roots) from an ally."),

        // --- Elf: the heal-and-cure, and Bind ---
        //
        // 🔑 `LbElfDawn` IS his "Healer Blessing" — same race, same level 40, same job (a heal that also
        // cleanses). Reused and retuned, like the Human's. ⚠ Two things his row CHANGED about it: it is
        // SINGLE-TARGET now, not an area heal, and its cure is SCOPED — bleed and poison up to rank 3,
        // rather than the blanket "cleanses everything" it used to be. That scoping is the same rank
        // ceiling Antidote runs on, so the two cures now sit on one ladder.
        new(LbElfDawn, "Healer Blessing", BaseClass.Mage, SkillEffect.Heal | SkillEffect.Cleanse,
            MpCost: 62, CastTicks: 30, CooldownTicks: 10, Range: 600, Power: 320,
            Category: SkillCategory.Heal, InitialMpCost: 13, SpCost: 36000,
            DispelMask: SkillEffect.Bleed | SkillEffect.Poison | SkillEffect.Venom,
            DispelMaxLevel: 3,
            Replaces: new[] { QuickHeal },
            Description: "Heals an ally for 320 and cures their bleed and poison of rank 3 or lower. " +
                         "The Elf Lightbringer answers a hurt ally and a poisoned one with one cast."),
        new(LbElfBind, "Bind", BaseClass.Mage, SkillEffect.Root,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "root", Rank: 1, InitialMpCost: 7,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            Description: "Holds an enemy in place for 30s."),
        new(LbElfWarden, "Warding Step", BaseClass.Mage, SkillEffect.Root | SkillEffect.Detaunt,
            MpCost: 30, CastTicks: 6, CooldownTicks: 120, Range: 500, Power: 0,
            DurationTicks: 80, BuffKey: "root", Rank: 1, DebuffSchool: DebuffSchool.Magical,
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Holds an enemy in place for 8s and sheds the caster's aggro " +
                         "from nearby foes (they look elsewhere)."),

        // --- Ork: the HEALING TOTEM + anti-heal debuff ---
        //
        // 🔑 THE STAND-IN IS NOW THE REAL THING (his `healer 3rd.csv` row, 2026-08-17). This id shipped as
        // "Spirit Font", an instant AoE heal, with its own description promising *"a placed totem will
        // replace this when summons arrive"*. His authored row is that replacement, so the ID IS REUSED
        // rather than retired: same race, same level, same job, and a character who already learned it
        // keeps a working skill instead of an orphan. His numbers: 300 radius, +30/s, 30 seconds.
        //
        // A totem is the Ork's answer to the Human's Quick Great Heal and the Elf's Healer Blessing —
        // the same 40-level slot spent on ground you leave behind rather than on a cast you aim.
        new(LbOrkFont, "Healing Totem", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 93, CastTicks: 10, CooldownTicks: 250, Range: 0, Power: 30,
            Category: SkillCategory.Heal, InitialMpCost: 19, SpCost: 36000,
            // SelfOnly, like the trap: you plant it where you stand, so the cast needs no target and
            // must not be rejected for lacking one.
            TargetMode: TargetMode.SelfOnly,
            PlacesTotem: true, TotemRadius: 300f, TotemLifeTicks: 300, TotemPulseTicks: 10,
            Description: "Plants a totem where you stand. For 30s it heals you and nearby party " +
                         "members for 30 HP a second."),
        // ⚠ There is no `DebuffMagicDef` flag and one must not be invented — the enum is full. M.Def is
        // applied LIVE through `BuffMagicDef` (see Entity.EffectiveMagicDefence), so a NEGATIVE
        // magnitude on the buff flag is how the engine already expresses an M.Def debuff. P.Def has its
        // own `DebuffDef` flag, whose magnitudes are authored positive and subtracted — hence the two
        // signs below looking inconsistent while meaning the same thing.
        new(LbOrkArmorBreak, "Armor Break", BaseClass.Mage,
            SkillEffect.DebuffDef | SkillEffect.BuffMagicDef,
            MpCost: 35, CastTicks: 25, CooldownTicks: 50, Range: 600, Power: 0,
            DurationTicks: 300, BuffKey: "armor_break", Rank: 1, InitialMpCost: 7,
            DebuffSchool: DebuffSchool.Magical, Category: SkillCategory.Debuff, SpCost: 36000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffDef, 0.10f), new(SkillEffect.BuffMagicDef, -0.05f),
            },
            Description: "Shatters an enemy's guard for 30s: −10% P.Def and −5% M.Def."),
        new(LbOrkSap, "Soul Sap", BaseClass.Mage, SkillEffect.DebuffHealRecv,
            MpCost: 28, CastTicks: 8, CooldownTicks: 150, Range: 500, Power: 0,
            DurationTicks: 150, BuffKey: "antiheal", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.DebuffHealRecv, 0.50f) },
            Category: SkillCategory.Debuff, SpCost: 6000,
            Description: "Curses an enemy so it recovers only half the HP from any " +
                         "healing for 15s."),
    };
}
