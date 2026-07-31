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
    public const string Resurrection = "resurrection";          // revive a fallen ally (4 levels)

    /// <summary>Healer Armor Mastery per-weight data (lvls 20/25/30/35). Robe = caster lean
    /// (+MP regen / def / max MP); Light = stay-casting + sturdier (+def, slight cast cost,
    /// +eva at L4); Heavy = penalty (slower casts/attacks). StatMods: pct &gt;0 = faster/more.</summary>
    private static readonly ArmorMasteryProfile[] HealerArmorMastery =
    {
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 20, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 20, CastSpeedPct: -0.05f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 25, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 25, CastSpeedPct: -0.05f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 30, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 30, CastSpeedPct: -0.05f),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 35, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 35, CastSpeedPct: -0.05f, Evasion: 2),
            Heavy: new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f),
            None:  new StatMods(AtkSpeedPct: -0.2f, CastSpeedPct: -0.5f)),
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

        // Improved Speed — the first IMPROVED (group) buff: it applies no buff of its own, only its
        // CHILDREN — one rung of each of the four speed families (swift / alacrity / agility / haste).
        // Each child competes on its own family key, so a rare Alacrity potion can override just the
        // cast part and leave the rest of the blessing standing. Levels are pure child references;
        // every value below is a rung the potions and scrolls also sell. See docs/design/BuffLadders.md.
        //
        // Levels 5-6 exist as data but have no learn slot yet: the cleric table stops at level 4
        // (char 35) and the Warchanter discipline tables are still commented out pending their CSVs.
        new(HolySpeed, "Improved Speed", BaseClass.Mage,
            SkillEffect.BuffCastSpeed | SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion | SkillEffect.BuffAtkSpeed,
            MpCost: 50, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_speed", Rank: 1, InitialMpCost: 10,
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
            Description: "Blesses an ally (or self): faster casting and movement for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 3200,
                    ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
                    Description: "Move +20, Cast +15%."),
                new SkillLevel(MpCost: 75, InitialMpCost: 15, SpCost: 6400,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU },
                    Description: "Move +33, Cast +23%."),
                new SkillLevel(MpCost: 75, InitialMpCost: 15, SpCost: 12800,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU, BuffAgilityU },
                    Description: "Move +33, Cast +23%, Evasion +2."),
                new SkillLevel(MpCost: 90, InitialMpCost: 18, SpCost: 25000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityU, BuffHasteC },
                    Description: "Move +33, Cast +30%, Evasion +2, Attack Speed +15%."),
                new SkillLevel(MpCost: 105, InitialMpCost: 21, SpCost: 50000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteU },
                    Description: "Move +33, Cast +30%, Evasion +4, Attack Speed +23%."),
                new SkillLevel(MpCost: 120, InitialMpCost: 24, SpCost: 100000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteR },
                    Description: "Move +33, Cast +30%, Evasion +4, Attack Speed +33%."),
            }),

        // Armor Mastery — DATA-DRIVEN passive that replaces Robe Mastery: its effect
        // depends on the BODY armor weight worn (see HealerArmorMastery). Levels carry
        // only the SP cost + max-level; the per-weight stats live in ArmorMasteryLevels.
        new(ArmorMasterySkill, "Armor Mastery", BaseClass.Mage, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive, Replaces: new[] { MasteryRobe },
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
        // Costs MORE MP than it restores (~1.2×), so it's a net MP TRANSFER to a non-caster.
        // Cannot target yourself or another mana-restorer (see HandleSkill), so a healer can't
        // refund their own/another healer's mana — it's for empowering non-MP-restoring allies.
        new(RestoreMana, "Restore Mana", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 72, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 60,
            Category: SkillCategory.Heal, InitialMpCost: 18, SpCost: 25000,
            Description: "Transfers 60 MP to an ally (costs 72 — a net loss). Can't be used on "
                       + "yourself or another mana-restorer."),

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
                       + "shorter reuse. Casting with anything but a sword or blunt is half speed.",
            // The per-level bonus applies ONLY with a sword/blunt; other/empty = cast x0.5 (no bonus).
            WeaponMasteryLevels: new[]
            {
                CasterMastery(new PassiveEffect(MagAtk: 6,  PhysAtk: 4,  CooldownPct: 0.10f)),
                CasterMastery(new PassiveEffect(MagAtk: 8,  PhysAtk: 6,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.10f)),
                CasterMastery(new PassiveEffect(MagAtk: 10, PhysAtk: 8,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.10f)),
                CasterMastery(new PassiveEffect(MagAtk: 12, PhysAtk: 10, CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegenPct: 0.50f, HpRegenPct: 0.10f)),
            },
            Levels: new[]
            {
                new SkillLevel(SpCost: 3200,  Description: "With sword/blunt: +6 M.Atk, +4 P.Atk, -10% skill reuse."),
                new SkillLevel(SpCost: 6400,  Description: "With sword/blunt: +8 M.Atk, +6 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 12800, Description: "With sword/blunt: +10 M.Atk, +8 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 25000, Description: "With sword/blunt: +12 M.Atk, +10 P.Atk, +5% cast, -10% reuse, +50% MP regen, +10% HP regen."),
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
                        new(SkillEffect.BuffMagAtk, 0.25f),
                    },
                    Description: "+25 interrupt resistance and +25% Magic Attack."),
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
                new(SkillEffect.BuffPhysAtk, 0.05f), new(SkillEffect.BuffMagAtk, 0.05f),
                new(SkillEffect.BuffCastSpeed, 0.05f), new(SkillEffect.BuffAtkSpeed, 0.05f),
                new(SkillEffect.BuffMoveSpeed, 5, ModifierMode.Flat),
            },
            Description: "A reckless surge: -30% Max HP/MP, but +5% Attack, +5% Magic Attack, "
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

        // Resurrection — revive a fallen ally to 30% HP/MP and restore a fraction of the exp they lost to
        // the death penalty. 4 levels (25/50/75/100%), learned for SP at 20/40/52/61 like any other skill.
        // The target must be dead (checked at cast).
        // 10s base cast at EVERY level, and deliberately NOT FixedCast: cast speed is the only thing that
        // shortens it, so investing in cast speed is what makes a res usable mid-fight. At the 1999 cast-speed
        // cap that's 333/1999 ≈ 1.67s — fast, but never instant (which would be OP).
        new(Resurrection, "Resurrection", BaseClass.Mage, SkillEffect.None,
            MpCost: 120, CastTicks: 100, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Heal, InitialMpCost: 24,
            Resurrect: true, ResExpPct: 0.25f,
            Description: "Revives a fallen ally at 30% HP and MP and restores part of the experience they "
                       + "lost on death (25% to 100% by level).",
            Levels: new[]
            {
                new SkillLevel(MpCost: 120, InitialMpCost: 24, SpCost: 6400,  ResExpPct: 0.25f, Description: "Revive at 30% HP/MP; restore 25% of lost exp."),
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 12800, ResExpPct: 0.50f, Description: "Revive at 30% HP/MP; restore 50% of lost exp."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 25000, ResExpPct: 0.75f, Description: "Revive at 30% HP/MP; restore 75% of lost exp."),
                new SkillLevel(MpCost: 210, InitialMpCost: 42, SpCost: 50000, ResExpPct: 1.00f, Description: "Revive at 30% HP/MP; restore 100% of lost exp."),
            }),
    };
}
