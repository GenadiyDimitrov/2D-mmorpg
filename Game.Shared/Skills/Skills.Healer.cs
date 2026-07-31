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
        new(HolySpeed, "Swift and Sure", BaseClass.Mage,
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

        // Body and Soul — the vitality group. Level 1 is exactly the +10% HP regen this buff has
        // always cast; the higher levels fold in MP regen, then Max HP, then Max MP, reaching the
        // NPC buffer's max at level 6. The four families it draws on (hp_max / mp_max / hp_regen /
        // mp_regen) are SCROLL-ONLY: there is no potion of any of them, which is why their scrolls
        // start at Epic. See docs/design/BuffLadders.md.
        new(HolyBody, "Body and Soul", BaseClass.Mage,
            SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen | SkillEffect.BuffHp | SkillEffect.BuffMp,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_body", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff, SpCost: 25000,
            ChildBuffs: new[] { Rung(FamHpRegen, 2) },
            Description: "Blesses an ally (or self) with vitality — regeneration, and at higher ranks Max HP and MP.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, InitialMpCost: 5, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 2) },
                    Description: "+10% HP regeneration."),
                new SkillLevel(MpCost: 35, InitialMpCost: 7, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 3), Rung(FamMpRegen, 2) },
                    Description: "+12% HP and +10% MP regeneration."),
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 4), Rung(FamMpRegen, 3), Rung(FamMaxHp, 1) },
                    Description: "+15% HP and +12% MP regeneration, +10% Max HP."),
                new SkillLevel(MpCost: 65, InitialMpCost: 13, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 5), Rung(FamMpRegen, 4), Rung(FamMaxHp, 2), Rung(FamMaxMp, 1) },
                    Description: "+17% HP and +15% MP regeneration, +15% Max HP, +10% Max MP."),
                new SkillLevel(MpCost: 80, InitialMpCost: 16, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 6), Rung(FamMpRegen, 5), Rung(FamMaxHp, 4), Rung(FamMaxMp, 3) },
                    Description: "+20% HP and +17% MP regeneration, +25% Max HP, +20% Max MP."),
                new SkillLevel(MpCost: 95, InitialMpCost: 19, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 6), Rung(FamMpRegen, 6), Rung(FamMaxHp, 6), Rung(FamMaxMp, 6) },
                    Description: "+20% HP and MP regeneration, +35% Max HP and Max MP."),
            }),

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

        // Force and Ward — the caster's group. Levels 1-2 are the numbers this buff already cast
        // (+18 interrupt resist, then +25 with +25% M.Atk); from level 3 it adds M.Def, and at 6
        // it equals the NPC buffer. M.Atk and M.Def each have a potion AND a scroll, so a Force
        // potion overrides only the M.Atk part. Resolve (interrupt resist) has no consumable at
        // all — a buffer is the only source. See docs/design/BuffLadders.md.
        new(HolyForce, "Force and Ward", BaseClass.Mage,
            SkillEffect.BuffInterruptResist | SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_force", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffIntr1 },
            Description: "Steadies an ally's casting (harder to interrupt/cancel); higher ranks add Magic Attack and Magic Defence.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, InitialMpCost: 5,  SpCost: 3200,
                    ChildBuffs: new[] { BuffIntr1 },
                    Description: "+18 interrupt resistance (harder to cancel your casts)."),
                new SkillLevel(MpCost: 50, InitialMpCost: 10, SpCost: 6400,
                    ChildBuffs: new[] { BuffIntr2, BuffMAtk2 },
                    Description: "+25 interrupt resistance and +25% M.Atk."),
                new SkillLevel(MpCost: 65, InitialMpCost: 13, SpCost: 12800,
                    ChildBuffs: new[] { BuffIntr3, BuffMAtk2, BuffMDef1 },
                    Description: "+40 interrupt resistance, +25% M.Atk, +10% M.Def."),
                new SkillLevel(MpCost: 80, InitialMpCost: 16, SpCost: 25000,
                    ChildBuffs: new[] { BuffIntr3, BuffMAtk3, BuffMDef2 },
                    Description: "+40 interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 95, InitialMpCost: 19, SpCost: 50000,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk3, BuffMDef2 },
                    Description: "+60 interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 110, InitialMpCost: 22, SpCost: 100000,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk3, BuffMDef3 },
                    Description: "+60 interrupt resistance, +32% M.Atk, +30% M.Def."),
            }),

        // Focus and Ferocity — the critical group. Level 1 is the +20% crit rate this buff has
        // always cast; the rest add crit damage and then magic crit, reaching the NPC buffer's
        // max at 6. All three families are SCROLL-ONLY (Epic and up).
        new(HolyFocus, "Focus and Ferocity", BaseClass.Mage,
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_focus", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff, SpCost: 6400,
            ChildBuffs: new[] { Rung(FamCritRate, 4) },
            Description: "Sharpens an ally's aim: critical rate, and at higher ranks critical damage and magic criticals.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 25, InitialMpCost: 5, SpCost: 6400,
                    ChildBuffs: new[] { Rung(FamCritRate, 4) },
                    Description: "+20% physical critical rate."),
                new SkillLevel(MpCost: 40, InitialMpCost: 8, SpCost: 12800,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 1) },
                    Description: "+25% critical rate, +10% critical damage."),
                new SkillLevel(MpCost: 55, InitialMpCost: 11, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 3), Rung(FamMagCrit, 1) },
                    Description: "+25% critical rate, +20% critical damage, +20% magic critical rate."),
                new SkillLevel(MpCost: 70, InitialMpCost: 14, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 4), Rung(FamMagCrit, 2) },
                    Description: "+30% critical rate, +25% critical damage, +35% magic critical rate."),
                new SkillLevel(MpCost: 85, InitialMpCost: 17, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 5), Rung(FamMagCrit, 4) },
                    Description: "+30% critical rate, +30% critical damage, +65% magic critical rate."),
                new SkillLevel(MpCost: 100, InitialMpCost: 20, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 6), Rung(FamMagCrit, 6) },
                    Description: "+30% critical rate, +35% critical damage, double magic critical rate."),
            }),

        // Frenzy — reckless surge: lower Max HP/MP for more offence + speed. The one family whose
        // rung is a WHOLE buff rather than one stat (the owner wants the scroll to carry "the full
        // frenzy"), so this is a thin wrapper: each level hands out one rung of the frenzy ladder.
        // Level 1 = what it cast before; level 6 = the NPC buffer's. Scroll-only (Epic and up).
        new(HolyFrenzy, "Frenzy", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk
            | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
            | SkillEffect.BuffEvasion,
            MpCost: 125, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_frenzy", Rank: 1, InitialMpCost: 25,
            Category: SkillCategory.Buff, SpCost: 25000,
            ChildBuffs: new[] { Rung(FamFrenzy, 1) },
            Description: "A reckless surge: less Max HP/MP, but more attack and speed for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 125, InitialMpCost: 25, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 1) },
                    Description: "−30% Max HP/MP, +5% offence and speed, +5 move, −8 evasion."),
                new SkillLevel(MpCost: 135, InitialMpCost: 27, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 2) },
                    Description: "−26% Max HP/MP, +6% offence and speed, +6 move, −8 evasion."),
                new SkillLevel(MpCost: 145, InitialMpCost: 29, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 3) },
                    Description: "−22% Max HP/MP, +6% offence and speed, +6 move, −8 evasion."),
                new SkillLevel(MpCost: 155, InitialMpCost: 31, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 4) },
                    Description: "−18% Max HP/MP, +7% offence and speed, +7 move, −8 evasion."),
                new SkillLevel(MpCost: 165, InitialMpCost: 33, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 5) },
                    Description: "−14% Max HP/MP, +7% offence and speed, +7 move, −8 evasion."),
                new SkillLevel(MpCost: 175, InitialMpCost: 35, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 6) },
                    Description: "−10% Max HP/MP, +8% offence and speed, +8 move, −8 evasion."),
            }),

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
