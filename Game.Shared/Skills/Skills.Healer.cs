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
    public const string ShroudingHymn = "shrouding_hymn";       // party STEALTH: unaggroed mobs ignore the group
    public const string Madness = "madness";                    // party FRENZY at the top of the family (`BL-34`)

    /// <summary>Healer Armor Mastery per-weight data (lvls 20/25/30/35). Robe = caster lean
    /// (+MP regen / def / max MP); LIGHT is the cleric's identity: he is the one caster who can
    /// wear it and keep working.
    ///
    /// ⚠ RESTRUCTURED 2026-08-07 (owner). The heavy/none penalty is GONE — it comes from Spellcaster
    /// Mastery now, and leaving a copy here would apply it twice once armor masteries began stacking.
    /// The LIGHT row is authored to CANCEL that penalty rather than to state a final number, which is
    /// why the multipliers look strange in isolation:
    ///   cast ×1.90 × Spellcaster ×0.50 = <b>×0.95</b> (his "−5% from a robe")
    ///   atkSpd ×2.00 × Spellcaster ×0.50 = <b>×1.00</b> (no attack-speed loss at all)
    ///   mpRegen ×1.20, and light does NOT get the robe's own ×1.20 — so he regenerates less than in
    ///   a robe but is not cut off, exactly as specified.
    /// Read every light number as "what it takes to land on the intended value AFTER Spellcaster".
    /// If Spellcaster's ×0.50 ever changes, these two must be re-derived — they are not free
    /// constants. (StatMods pct: >0 = faster/more, so ×1.90 is CastSpeedPct 0.90.)</summary>
    private static readonly ArmorMasteryProfile[] HealerArmorMastery =
    {
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 20, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 20, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 25, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 25, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 30, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 30, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(MpRegenPct: 0.2f, PDef: 35, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 35, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f, Evasion: 2)),
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

        // Shrouding Hymn — the BUFFER's half of stealth (BL-69, kind 2). Exactly what the rogue's
        // Prowl does, handed to the whole party for a minute: monsters that have not already noticed
        // the group leave it alone, anything already chasing keeps chasing, and it is not broken by
        // acting. It is how a party crosses a field it has no business fighting through.
        //
        // 1 minute / 30s reuse / 300 MP are his numbers, and the price is the point — 300 MP at the
        // level a cleric learns it is most of the bar, so this is a journey, not a rotation.
        new(ShroudingHymn, "Shrouding Hymn", BaseClass.Mage, SkillEffect.None,
            MpCost: 300, CastTicks: 20, CooldownTicks: 300, Range: 600, Power: 0,
            DurationTicks: 600, BuffKey: "shrouding_hymn", Rank: 1, InitialMpCost: 60,
            Category: SkillCategory.Buff, SpCost: 12000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            GrantsMobStealth: true,
            Description: "For 1 minute, monsters that haven't already noticed you and your nearby " +
                         "allies leave you alone. Anything already chasing keeps chasing."),

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
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_speed", Rank: 1, InitialMpCost: 30,
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMove), CastId(FamCast), CastId(FamEva), CastId(FamAs) },
            Description: "Blesses you and nearby allies: faster casting and movement for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 3200,
                    ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
                    Description: "Move +20, Cast +15%."),
                new SkillLevel(MpCost: 160, InitialMpCost: 32, SpCost: 6400,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU },
                    Description: "Move +33, Cast +23%."),
                new SkillLevel(MpCost: 170, InitialMpCost: 34, SpCost: 12800,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU, BuffAgilityU },
                    Description: "Move +33, Cast +23%, Evasion +2."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 25000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityU, BuffHasteC },
                    Description: "Move +33, Cast +30%, Evasion +2, Attack Speed +15%."),
                new SkillLevel(MpCost: 190, InitialMpCost: 38, SpCost: 50000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteU },
                    Description: "Move +33, Cast +30%, Evasion +4, Attack Speed +23%."),
                new SkillLevel(MpCost: 200, InitialMpCost: 40, SpCost: 100000,
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
        // ⚠ IT IS NOW A LOSSLESS TRANSFER, and that is a REVERSAL (owner 2026-08-17: *"restore mana
        // is @30 - 52 power, 35 - 60 power same mana cost as power"*). It used to cost ~1.2× what it
        // gave, so moving mana around a party burned some — his ruling is that the cost EQUALS the
        // power, so 52 MP buys 52 MP. What still keeps it from being a party-wide mana battery is the
        // targeting rule, not the price: it cannot target yourself or another mana-restorer (see
        // HandleSkill), so it only ever empowers an ally who has no way to refill their own bar.
        new(RestoreMana, "Restore Mana", BaseClass.Mage, SkillEffect.RestoreMp,
            MpCost: 52, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 52,
            Category: SkillCategory.Heal, InitialMpCost: 13, SpCost: 12800,
            Description: "Transfers MP to an ally at no loss. Can't be used on yourself or another "
                       + "mana-restorer.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 52, InitialMpCost: 13, SpCost: 12800, Power: 52,
                    Description: "Transfers 52 MP to an ally (costs 52). Can't be used on yourself or another mana-restorer."),
                new SkillLevel(MpCost: 60, InitialMpCost: 15, SpCost: 25000, Power: 60,
                    Description: "Transfers 60 MP to an ally (costs 60). Can't be used on yourself or another mana-restorer."),
                new SkillLevel(MpCost: 70, InitialMpCost: 20, SpCost: 36000, Power: 70,
                    Description: "Transfers 70 MP to an ally (costs 70). Can't be used on yourself or another mana-restorer."),
            }),

        // Body and Soul — the vitality group. Level 1 is exactly the +10% HP regen this buff has
        // always cast; the higher levels fold in MP regen, then Max HP, then Max MP, reaching the
        // NPC buffer's max at level 6. The four families it draws on (hp_max / mp_max / hp_regen /
        // mp_regen) are SCROLL-ONLY: there is no potion of any of them, which is why their scrolls
        // start at Epic. See docs/design/BuffLadders.md.
        new(HolyBody, "Body and Soul", BaseClass.Mage,
            SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen | SkillEffect.BuffHp | SkillEffect.BuffMp,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_body", Rank: 1, InitialMpCost: 30,
            Category: SkillCategory.Buff, SpCost: 25000,
            ChildBuffs: new[] { Rung(FamHpRegen, 2) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMaxHp), CastId(FamMaxMp), CastId(FamHpRegen), CastId(FamMpRegen) },
            Description: "Blesses you and nearby allies with vitality — regeneration, and at higher ranks Max HP and MP.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 2) },
                    Description: "+10% HP regeneration."),
                new SkillLevel(MpCost: 160, InitialMpCost: 32, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 3), Rung(FamMpRegen, 2) },
                    Description: "+12% HP and +10% MP regeneration."),
                new SkillLevel(MpCost: 170, InitialMpCost: 34, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 4), Rung(FamMpRegen, 3), Rung(FamMaxHp, 1) },
                    Description: "+15% HP and +12% MP regeneration, +10% Max HP."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 5), Rung(FamMpRegen, 4), Rung(FamMaxHp, 2), Rung(FamMaxMp, 1) },
                    Description: "+17% HP and +15% MP regeneration, +15% Max HP, +10% Max MP."),
                new SkillLevel(MpCost: 190, InitialMpCost: 38, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 6), Rung(FamMpRegen, 5), Rung(FamMaxHp, 4), Rung(FamMaxMp, 3) },
                    Description: "+20% HP and +17% MP regeneration, +25% Max HP, +20% Max MP."),
                new SkillLevel(MpCost: 200, InitialMpCost: 40, SpCost: 100000,
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
                // Level 5 = the healer's level-40 row (his `healer 3rd.csv`). Note the jump: +23 M.Atk
                // against level 4's +12 — a 3rd class is where a caster's damage actually moves.
                CasterMastery(new PassiveEffect(MagAtk: 23, PhysAtk: 18, CastSpeedPct: 0.07f, CooldownPct: 0.10f, MpRegenPct: 0.50f, HpRegenPct: 0.10f)),
            },
            Levels: new[]
            {
                new SkillLevel(SpCost: 3200,  Description: "With sword/blunt: +6 M.Atk, +4 P.Atk, -10% skill reuse."),
                // 12800, not 6400 — his CSV's number. The ladder is 3200 / 12800 / 12800 / 25000, which
                // is irregular but authored; it was silently "regularised" here and is now put back.
                new SkillLevel(SpCost: 12800, Description: "With sword/blunt: +8 M.Atk, +6 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 12800, Description: "With sword/blunt: +10 M.Atk, +8 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 25000, Description: "With sword/blunt: +12 M.Atk, +10 P.Atk, +5% cast, -10% reuse, +50% MP regen, +10% HP regen."),
                new SkillLevel(SpCost: 36000, Description: "With sword/blunt: +23 M.Atk, +18 P.Atk, +7% cast, -10% reuse, +50% MP regen, +10% HP regen."),
            }),

        // Force and Ward — the caster's group. Levels 1-2 are the numbers this buff already cast
        // (+18 interrupt resist, then +25 with +25% M.Atk); from level 3 it adds M.Def, and at 6
        // it equals the NPC buffer. M.Atk and M.Def each have a potion AND a scroll, so a Force
        // potion overrides only the M.Atk part. Resolve (interrupt resist) has no consumable at
        // all — a buffer is the only source. See docs/design/BuffLadders.md.
        new(HolyForce, "Force and Ward", BaseClass.Mage,
            SkillEffect.BuffInterruptResist | SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_force", Rank: 1, InitialMpCost: 30,
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffIntr1 },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMagAtk), CastId(FamMagDef), CastId(FamInterrupt) },
            Description: "Steadies the casting of you and nearby allies; higher ranks add Magic Attack and Magic Defence.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 3200,
                    ChildBuffs: new[] { BuffIntr1 },
                    Description: "+18 interrupt resistance (harder to cancel your casts)."),
                new SkillLevel(MpCost: 160, InitialMpCost: 32, SpCost: 6400,
                    ChildBuffs: new[] { BuffIntr2, BuffMAtk2 },
                    Description: "+25 interrupt resistance and +25% M.Atk."),
                new SkillLevel(MpCost: 170, InitialMpCost: 34, SpCost: 12800,
                    ChildBuffs: new[] { BuffIntr3, BuffMAtk2, BuffMDef1 },
                    Description: "+40 interrupt resistance, +25% M.Atk, +10% M.Def."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 25000,
                    ChildBuffs: new[] { BuffIntr3, BuffMAtk3, BuffMDef2 },
                    Description: "+40 interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 190, InitialMpCost: 38, SpCost: 50000,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk3, BuffMDef2 },
                    Description: "+60 interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 200, InitialMpCost: 40, SpCost: 100000,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk3, BuffMDef3 },
                    Description: "+60 interrupt resistance, +32% M.Atk, +30% M.Def."),
            }),

        // Focus and Ferocity — the critical group. Level 1 is the +20% crit rate this buff has
        // always cast; the rest add crit damage and then magic crit, reaching the NPC buffer's
        // max at 6. All three families are SCROLL-ONLY (Epic and up).
        new(HolyFocus, "Focus and Ferocity", BaseClass.Mage,
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_focus", Rank: 1, InitialMpCost: 30,
            Category: SkillCategory.Buff, SpCost: 6400,
            ChildBuffs: new[] { Rung(FamCritRate, 4) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamCritRate), CastId(FamCritDmg), CastId(FamMagCrit) },
            Description: "Sharpens you and nearby allies: critical rate, and at higher ranks critical damage and magic criticals.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150, InitialMpCost: 30, SpCost: 6400,
                    ChildBuffs: new[] { Rung(FamCritRate, 4) },
                    Description: "+20% physical critical rate."),
                new SkillLevel(MpCost: 160, InitialMpCost: 32, SpCost: 12800,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 1) },
                    Description: "+25% critical rate, +10% critical damage."),
                new SkillLevel(MpCost: 170, InitialMpCost: 34, SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 3), Rung(FamMagCrit, 1) },
                    Description: "+25% critical rate, +20% critical damage, +20% magic critical rate."),
                new SkillLevel(MpCost: 180, InitialMpCost: 36, SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 4), Rung(FamMagCrit, 2) },
                    Description: "+30% critical rate, +25% critical damage, +35% magic critical rate."),
                new SkillLevel(MpCost: 190, InitialMpCost: 38, SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 5), Rung(FamMagCrit, 4) },
                    Description: "+30% critical rate, +30% critical damage, +65% magic critical rate."),
                new SkillLevel(MpCost: 200, InitialMpCost: 40, SpCost: 100000,
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

        // ===== MADNESS — the top of the Frenzy family, cast on the whole party (`BL-34`) ===========
        //
        // His ruling, 2026-08-14: *"put it at 76 on the buffer"*, explicitly so **an admin can buff a
        // party with it** in the meantime — *"and when the kits land we will move it"*. So this is a
        // deliberate TEMPORARY home, not the final rung: it sits at the top of the Warchanter's
        // existing 40-74 buff ladder (which stops at the five improved groups at 74) because that is
        // the only 76 slot the game currently has, and the debug admin is a level-90 Warchanter, so
        // putting it there makes it castable the moment the server boots.
        //
        // ⚠ It is an EXCEPTION to the 40+ freeze in the same sense the Warchanter's buff kit already
        // is — he has a CSV for the buffer and named this skill himself. It is NOT licence to invent
        // other 76 skills.
        //
        // Shape: a thin party wrapper, exactly like `HolyFrenzy` one tier down, handing out the new
        // rung 7 to everyone in radius. It carries NO buff of its own — the child competes on the
        // `FamFrenzy` key by Rank, so Madness simply outranks and evicts any weaker Frenzy the party
        // is already wearing (a cleric's, a scroll's, the NPC buffer's), and nothing can override it
        // afterwards. That is the whole reason it is a rung and not a parallel buff: two Frenzies
        // stacking is precisely what the family key exists to prevent.
        //
        // One level only. A ladder of Madness levels would be inventing 40+ content; the rung it hands
        // out is the family's top and there is nothing above it to climb to.
        new(Madness, "Madness", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk
            | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
            | SkillEffect.BuffEvasion,
            // MP sits above the improved groups' 200 — it buffs the party AND it is the family's top.
            MpCost: 220, CastTicks: 15, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "madness", Rank: 1, InitialMpCost: 44,
            Category: SkillCategory.Buff, SpCost: 100000,
            ChildBuffs: new[] { Rung(FamFrenzy, 7) },
            // Party-targeted for the same reason every improved group is (docs/design/BuffLadders.md):
            // the autopilot hard-targets SELF for buffs, so a single-target version could never be
            // handed out by a buffer left on auto-farm — which is the state he tests parties in.
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Description: "Drives you and nearby allies berserk: −6% Max HP/MP, but +9% P.Atk / M.Atk / "
                       + "attack & cast speed, +9 Move Speed and −8 Evasion for 20 minutes."),

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

        // Antidote — targeted CURE: strips DoTs from an ally. A cheaper, focused alternative to a
        // full Cleanse.
        //
        // 🔑 ITS LADDER IS A RANK CEILING, NOT A LIST (owner 2026-08-17: *"antidote have lvl and should
        // cure poison + bleed below some level ... you get a posion 10 and bleed 5 .. ur antidote is lvl
        // 3 (cures below lvl 7) and cures the bleed only"*). WHAT it cures never changes — poison, venom
        // and bleed at every level. What grows is how strong an ailment it can reach, so a cure can be
        // outclassed by a big DoT instead of trivialising every one. `SkillLevel.DispelMaxLevel` exists
        // for this and Antidote is its only user; *"later healers will learn more levels"* (BL-02).
        //
        // The ceilings are HIS (he wrote them into the CSV after seeing a first guess of 3/5): the cure
        // reaches rank 1 at level 1 and rank 2 at level 2 — the ladder is simply level = rank, and it is
        // deliberately TIGHT, so a healer out-levels an ailment slowly rather than all at once.
        //
        // ⚠ IT IS INERT TODAY. Every DoT in the game is authored at Rank 1 (the Venomweaver trio; mob
        // spells carry no rank at all), so any ceiling ≥ 1 currently cures everything. The mechanic
        // only starts to bite once DoTs carry a real rank — which is what his "poison 10" implies.
        new(Antidote, "Antidote", BaseClass.Mage, SkillEffect.Cleanse,
            MpCost: 16, CastTicks: 8, CooldownTicks: 30, Range: 600, Power: 0,
            Category: SkillCategory.Heal, InitialMpCost: 4, SpCost: 3200,
            DispelMask: SkillEffect.Poison | SkillEffect.Venom | SkillEffect.Bleed,
            DispelMaxLevel: 1,
            Description: "Cures poison, venom and bleed from an ally (or self), up to a rank its own "
                       + "level can reach.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 16, InitialMpCost: 4, SpCost: 3200, DispelMaxLevel: 1,
                    Description: "Cures poison, venom and bleed of rank 1 or lower from an ally (or self)."),
                new SkillLevel(MpCost: 20, InitialMpCost: 5, SpCost: 12800, DispelMaxLevel: 2,
                    Description: "Cures poison, venom and bleed of rank 2 or lower from an ally (or self)."),
            }),

        // Resurrection — revive a fallen ally to 30% HP/MP and restore a fraction of the exp they lost to
        // the death penalty. The target must be dead (checked at cast).
        //
        // 🔑 TWO LEVELS, AND LEVEL 1 GIVES THE EXP BACK — NOTHING (owner 2026-08-17: *"resurect - lvl 20 no
        // exp, @30 - 20% exp .. ill add 40+"*). That is the whole shape of the skill: a 2nd-class cleric can
        // put you back on your feet but cannot save your experience, so a res early on is a rescue, not an
        // undo. It replaced a 4-level 25/50/75/100% ladder that was invented here.
        // ⚠ THE 40+ RUNGS ARE HIS AND ARE NOT WRITTEN YET (*"ill add 40+"*, BL-02). The old L3@52 / L4@61
        // Lightbringer grants were removed with the ladder — do not re-invent them to fill the gap.
        // 10s base cast at EVERY level, and deliberately NOT FixedCast: cast speed is the only thing that
        // shortens it, so investing in cast speed is what makes a res usable mid-fight. At the 1999 cast-speed
        // cap that's 333/1999 ≈ 1.67s — fast, but never instant (which would be OP).
        // MP is his too (2026-08-17), authored as the CSV's two columns — 10 up front + 50 on the finish
        // at level 1, 20 + 70 at level 2. `MpCost` is the TOTAL and `InitialMpCost` the up-front slice,
        // so those read 60 and 90 here. It is roughly HALF what this skill used to cost (120/150).
        new(Resurrection, "Resurrection", BaseClass.Mage, SkillEffect.None,
            MpCost: 60, CastTicks: 100, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Heal, InitialMpCost: 10,
            Resurrect: true, ResExpPct: 0f,
            Description: "Revives a fallen ally at 30% HP and MP. Higher levels also give back part of the "
                       + "experience they lost on death.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 60, InitialMpCost: 10, SpCost: 6400,  ResExpPct: 0f,
                    Description: "Revive at 30% HP/MP. Does not give back any lost exp."),
                new SkillLevel(MpCost: 90, InitialMpCost: 20, SpCost: 12800, ResExpPct: 0.20f,
                    Description: "Revive at 30% HP/MP; restore 20% of lost exp."),
                // Level 3 = his `healer 3rd.csv` level-40 row. The rungs above it (his file sketches
                // 40/50/60/70%) sit in the part he marked "not done", so they are NOT written here.
                new SkillLevel(MpCost: 120, InitialMpCost: 30, SpCost: 36000, ResExpPct: 0.30f,
                    Description: "Revive at 30% HP/MP; restore 30% of lost exp."),
            }),
    };
}
