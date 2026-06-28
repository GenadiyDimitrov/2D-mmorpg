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
    public const string ArmorMasterySkill = "armor_mastery";   // data-driven, replaces Robe Mastery
    public const string HolyForce = "holy_force";    // "Force" — interrupt resist (+M.Atk @rank 2)
    public const string HolyFocus = "holy_focus";    // "Focus" — physical crit-rate buff
    public const string HolyFrenzy = "holy_frenzy";  // "Frenzy" — berserk trade-off buff
    public const string CombatStance = "healer_combat_stance";  // TOGGLE: trade M.Atk for P.Atk
    public const string Antidote = "antidote";                  // cure: removes poison/venom

    /// <summary>Healer Armor Mastery per-weight data (lvls 20/25/30/35). Robe = caster lean
    /// (+MP regen / def / max MP); Light = stay-casting + sturdier (+def, slight cast cost,
    /// +eva at L4); Heavy = penalty (slower casts/attacks). Factors: &gt;1 faster/more.</summary>
    private static readonly ArmorMasteryProfile[] HealerArmorMastery =
    {
        new(Robe:  new MasteryEffect(MpRegen: 1.2f, Defence: 20, MaxMpFlat: 20),
            Light: new MasteryEffect(MpRegen: 1.2f, Defence: 20, CastSpeed: 0.95f),
            Heavy: new MasteryEffect(MpRegen: 1.0f, AtkSpeed: 0.8f, CastSpeed: 0.5f)),
        new(Robe:  new MasteryEffect(MpRegen: 1.2f, Defence: 25, MaxMpFlat: 20),
            Light: new MasteryEffect(MpRegen: 1.2f, Defence: 25, CastSpeed: 0.95f),
            Heavy: new MasteryEffect(MpRegen: 1.0f, AtkSpeed: 0.8f, CastSpeed: 0.5f)),
        new(Robe:  new MasteryEffect(MpRegen: 1.2f, Defence: 30, MaxMpFlat: 30),
            Light: new MasteryEffect(MpRegen: 1.2f, Defence: 30, CastSpeed: 0.95f),
            Heavy: new MasteryEffect(MpRegen: 1.0f, AtkSpeed: 0.8f, CastSpeed: 0.5f)),
        new(Robe:  new MasteryEffect(MpRegen: 1.2f, Defence: 35, MaxMpFlat: 30),
            Light: new MasteryEffect(MpRegen: 1.2f, Defence: 35, CastSpeed: 0.95f, Evasion: 2),
            Heavy: new MasteryEffect(MpRegen: 1.0f, AtkSpeed: 0.8f, CastSpeed: 0.5f)),
    };

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

        // Armor Mastery — DATA-DRIVEN passive that replaces Robe Mastery: its effect
        // depends on the BODY armor weight worn (see HealerArmorMastery). Levels carry
        // only the SP cost + max-level; the per-weight stats live in ArmorMasteryLevels.
        new(ArmorMasterySkill, "Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { MasteryRobe, MasteryLight },
            Description: "Passive. Adapts to your armor: ROBE boosts MP, MP-regen and defence; "
                       + "LIGHT keeps you casting while sturdier; HEAVY weighs your casting and attacks down.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 9600),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 25000),
            },
            ArmorMasteryLevels: HealerArmorMastery),

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

        // Spell Mastery — caster passive (replaces Weapon Mastery). Flat M/P.Atk, a 10%
        // reuse-delay reduction, +5% cast speed and (from lvl 2) MP/HP-regen multipliers.
        new(SpellMastery, "Spell Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { WeaponMastery },
            Description: "Passive. Sharpens your spellcasting — more M.Atk/P.Atk, faster casts, "
                       + "shorter reuse. Casting with a bow is half speed.",
            // Same caster bow penalty as Weapon Mastery, at every level (one entry per level).
            WeaponMasteryLevels: new[] { CasterBowPenalty, CasterBowPenalty, CasterBowPenalty, CasterBowPenalty },
            Levels: new[]
            {
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagAtk: 6,  PhysAtk: 4,  CooldownPct: 0.10f),
                    Description: "+6 M.Atk, +4 P.Atk, -10% skill reuse."),
                new SkillLevel(SpCost: 6400,  Passive: new PassiveEffect(MagAtk: 8,  PhysAtk: 6,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.10f),
                    Description: "+8 M.Atk, +6 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 12800, Passive: new PassiveEffect(MagAtk: 10, PhysAtk: 8,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.10f),
                    Description: "+10 M.Atk, +8 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 25000, Passive: new PassiveEffect(MagAtk: 12, PhysAtk: 10, CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.50f, HpRegenPct: 0.10f),
                    Description: "+12 M.Atk, +10 P.Atk, +5% cast, -10% reuse, +50% MP regen, +10% HP regen."),
            }),

        // Force — steadies casting (interrupt resist = "magic-cancel resist"); rank 2 adds
        // a big Magic Attack buff. 2 levels (20/25), castable on an ally or self.
        new(HolyForce, "Force", BaseClass.Mage,
            SkillEffect.BuffInterruptResist | SkillEffect.BuffMagAtk,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_force", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffInterruptResist, 18, ModifierMode.Flat) },
            Description: "Steadies an ally's casting (harder to interrupt/cancel); higher ranks add Magic Attack.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, InitialMpCost: 5,  SpCost: 3200,
                    Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffInterruptResist, 18, ModifierMode.Flat) },
                    Description: "+18 interrupt resistance (harder to cancel your casts)."),
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 6400,
                    Magnitudes: new EffectMagnitude[]
                    {
                        new(SkillEffect.BuffInterruptResist, 25, ModifierMode.Flat),
                        new(SkillEffect.BuffMagAtk, 0.55f),
                    },
                    Description: "+25 interrupt resistance and +55% Magic Attack."),
            }),

        // Focus — +20% physical crit rate (for melee allies). Single level (25).
        new(HolyFocus, "Focus", BaseClass.Mage, SkillEffect.BuffCritRate,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_focus", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff, SpCost: 6400,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCritRate, 0.20f) },
            Description: "Sharpens an ally's aim: +20% physical critical rate for 20 minutes."),

        // Frenzy — reckless surge: lower Max HP/MP for more offence + speed. Single level (35).
        new(HolyFrenzy, "Frenzy", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk
            | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed,
            MpCost: 125, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_frenzy", Rank: 1, InitialMpCost: 25,
            Category: SkillCategory.Buff, SpCost: 25000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffHp, -0.30f), new(SkillEffect.BuffMp, -0.30f),
                new(SkillEffect.BuffPhysAtk, 0.05f), new(SkillEffect.BuffMagAtk, 0.10f),
                new(SkillEffect.BuffCastSpeed, 0.05f), new(SkillEffect.BuffAtkSpeed, 0.05f),
                new(SkillEffect.BuffMoveSpeed, 5, ModifierMode.Flat),
            },
            Description: "A reckless surge: -30% Max HP/MP, but +5% Attack, +10% Magic Attack, "
                       + "+5% cast & attack speed and +5 move speed for 20 minutes."),

        // Combat Stance — TOGGLE. Pours magic into melee: +P.Atk, -M.Atk (weaker heals/
        // spells) while wielding a mace, so a cleric can solo-farm. Click again to end.
        // (First user of the toggle-skill mechanic.) Numbers are placeholders.
        new(CombatStance, "Combat Stance", BaseClass.Mage,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk,
            MpCost: 20, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            BuffKey: "healer_combat_stance", Rank: 1,
            Category: SkillCategory.Buff, Toggle: true, TargetMode: TargetMode.SelfOnly,
            SpCost: 2000,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, 0.50f),    // +50% P.Atk
                new(SkillEffect.BuffMagAtk, -0.50f),    // -50% M.Atk (weaker heals/spells)
            },
            Description: "Toggle. Channel your magic into melee: +50% P.Atk but -50% M.Atk "
                       + "(weaker heals and spells). Click again to end."),

        // Antidote — targeted CURE: removes poison and venom from an ally (DispelMask). A
        // cheaper, focused alternative to a full Cleanse. (Cure-bleed would add Bleed here.)
        new(Antidote, "Antidote", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 16, CastTicks: 8, CooldownTicks: 30, Range: 600, Power: 0,
            Category: SkillCategory.Heal, InitialMpCost: 4,
            DispelMask: SkillEffect.Poison | SkillEffect.Venom,
            Description: "Cures poison and venom from an ally (or self)."),
    };
}
