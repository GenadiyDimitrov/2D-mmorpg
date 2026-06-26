namespace Game.Shared;

/// <summary>2nd-class Healer (cleric) kit — the healer-ONLY skills: the fast/AoE heals,
/// the support buffs (Speed/Body) and the casting passive (Spell Mastery). The shared
/// mage skills the Healer simply CONTINUES (Heal, Might, Anti-Magic, Holy Bolt) live in
/// Skills.Mage.cs and gain their higher levels there. Force/Focus/Frenzy + the data-driven
/// Armor Mastery land in later increments (they need new combat primitives / a refactor).</summary>
public static partial class SkillCatalog
{
    public const string QuickHeal = "quick_heal";
    public const string PartyHeal = "party_heal";
    public const string HolySpeed = "holy_speed";   // "Speed" buff (cast + move + evasion)
    public const string HolyBody  = "holy_body";    // "Body" buff (+HP regen)
    public const string SpellMastery = "spell_mastery";
    public const string RestoreMana = "restore_mana";

    private static SkillDef[] HealerSkills() => new SkillDef[]
    {
        // Quick Heal — fast single-target heal (same powers as Heal, much shorter cast).
        new(QuickHeal, "Quick Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 45, CastTicks: 20, CooldownTicks: 10, Range: 600, Power: 151,
            Category: SkillCategory.Heal, InitialMpCost: 9,
            Description: "A fast heal on an ally (or yourself). Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 151, MpCost: 45, InitialMpCost: 9,  SpCost: 3200,  Description: "Quick heal power 151."),
                new SkillLevel(Power: 195, MpCost: 57, InitialMpCost: 12, SpCost: 6400,  Description: "Quick heal power 195."),
                new SkillLevel(Power: 245, MpCost: 65, InitialMpCost: 13, SpCost: 12800, Description: "Quick heal power 245."),
                new SkillLevel(Power: 301, MpCost: 67, InitialMpCost: 15, SpCost: 25000, Description: "Quick heal power 301."),
            }),

        // Party Heal — AoE heal to nearby allies (lower power than single-target).
        new(PartyHeal, "Party Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 60, CastTicks: 70, CooldownTicks: 50, Range: 600, Power: 121,
            Category: SkillCategory.Heal, InitialMpCost: 12,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Description: "Heals you and nearby allies. Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 121, MpCost: 60, InitialMpCost: 12, SpCost: 3200,  Description: "Party heal power 121."),
                new SkillLevel(Power: 156, MpCost: 76, InitialMpCost: 16, SpCost: 6400,  Description: "Party heal power 156."),
                new SkillLevel(Power: 196, MpCost: 94, InitialMpCost: 18, SpCost: 12800, Description: "Party heal power 196."),
                new SkillLevel(Power: 241, MpCost: 96, InitialMpCost: 20, SpCost: 25000, Description: "Party heal power 241."),
            }),

        // Speed — party-castable cast-speed + move-speed (+evasion) buff (20 min).
        new(HolySpeed, "Speed", BaseClass.Mage,
            SkillEffect.BuffCastSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
            MpCost: 50, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_speed", Rank: 1, InitialMpCost: 10,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffCastSpeed, 0.15f),
                new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat),
            },
            Description: "Blesses an ally (or self): faster casting and movement for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 3200,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffCastSpeed, 0.15f),
                        new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat),
                    },
                    Description: "Cast +15%, Move +20."),
                new SkillLevel(MpCost: 75, InitialMpCost: 15, SpCost: 6400,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffCastSpeed, 0.15f),
                        new(SkillEffect.BuffMoveSpeed, 20, ModifierMode.Flat),
                        new(SkillEffect.BuffEvasion, 2, ModifierMode.Flat),
                    },
                    Description: "Cast +15%, Move +20, Evasion +2."),
                new SkillLevel(MpCost: 75, InitialMpCost: 15, SpCost: 12800,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffCastSpeed, 0.15f),
                        new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                        new(SkillEffect.BuffEvasion, 2, ModifierMode.Flat),
                    },
                    Description: "Cast +15%, Move +33, Evasion +2."),
                new SkillLevel(MpCost: 75, InitialMpCost: 15, SpCost: 25000,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffCastSpeed, 0.23f),
                        new(SkillEffect.BuffMoveSpeed, 33, ModifierMode.Flat),
                        new(SkillEffect.BuffEvasion, 2, ModifierMode.Flat),
                    },
                    Description: "Cast +23%, Move +33, Evasion +2."),
            }),

        // Restore Mana — replenishes an ally's MP (flat power). Later "ultimate" restores
        // will add a % of max MP via a Percent magnitude on the RestoreMp effect.
        new(RestoreMana, "Restore Mana", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 30, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 60,
            Category: SkillCategory.Heal, InitialMpCost: 8, SpCost: 25000,
            Description: "Restores 60 MP to an ally (or yourself)."),

        // Body — party-castable HP-regen buff (20 min). Learned at 35 (single level).
        new(HolyBody, "Body", BaseClass.Mage, SkillEffect.BuffHpRegen,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_body", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff, SpCost: 25000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffHpRegen, 0.10f) },
            Description: "Blesses an ally (or self) with +10% HP regeneration for 20 minutes."),

        // Spell Mastery — caster passive (replaces Weapon Mastery). Increment 1 wires the
        // flat M/P.Atk + cast-speed parts; the reuse-delay reduction and the mp/hp-regen
        // MULTIPLIERS land in Increment 2 (they need new PassiveEffect primitives).
        new(SpellMastery, "Spell Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { WeaponMastery },
            Description: "Passive. Sharpens your spellcasting — more M.Atk/P.Atk and faster casts.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagAtk: 6,  PhysAtk: 4),
                    Description: "+6 M.Atk, +4 P.Atk."),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(MagAtk: 8,  PhysAtk: 6,  CastSpeedPct: 0.05f),
                    Description: "+8 M.Atk, +6 P.Atk, +5% cast speed."),
                new SkillLevel(SpCost: 12800, Passive: new PassiveEffect(MagAtk: 10, PhysAtk: 8,  CastSpeedPct: 0.05f),
                    Description: "+10 M.Atk, +8 P.Atk, +5% cast speed."),
                new SkillLevel(SpCost: 25000, Passive: new PassiveEffect(MagAtk: 12, PhysAtk: 10, CastSpeedPct: 0.05f),
                    Description: "+12 M.Atk, +10 P.Atk, +5% cast speed."),
            }),
    };
}
