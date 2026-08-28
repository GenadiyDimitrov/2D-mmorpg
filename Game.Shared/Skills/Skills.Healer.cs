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
    public const string HolyShield = "holy_shield";  // "Shield Bless and Harden" — the SHIELD group
    public const string HolyFrenzy = "holy_frenzy";  // "Frenzy" — berserk trade-off buff
    public const string CombatStance = "healer_combat_stance";  // TOGGLE: trade M.Atk for P.Atk
    public const string Antidote = "antidote";                  // cure: removes poison/venom
    public const string Resurrection = "resurrection";          // revive a fallen ally (4 levels)
    public const string ShroudingHymn = "shrouding_hymn";       // party STEALTH: unaggroed mobs ignore the group
    // 🔑 THE ID IS `madness` AND STAYS — the SKILL is called "War Frenzy" since 2026-08-21, and it is
    //    the level-56 PARTY Frenzy, not a level-76 capstone. Ids are append-only; read the comment.
    public const string WarFrenzy = "madness";                  // party FRENZY, one tier above Frenzy @52

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
        new(Robe:  new StatMods(PDef: 20, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 20, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(PDef: 25, MaxMp: 20),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 25, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(PDef: 30, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 30, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f)),
        new(Robe:  new StatMods(PDef: 35, MaxMp: 30),
            Light: new StatMods(MpRegenPct: 0.2f, PDef: 35, CastSpeedPct: 0.90f, AtkSpeedPct: 1.00f, Evasion: 2)),
        // ---- Rungs 5-18 (@40 to 74) — THE BUFFER'S, not the healer's (`buffer 3rd.csv`, 2026-08-20:
        //      *"Continue the line"*; completed 2026-08-21 when he finished the file). The healer
        //      branches away at 40 onto his own robe-only Healer Armor Mastery (Skills.Lightbringer.cs),
        //      so from here this ladder belongs to the Warchanter alone.
        //
        // 🔑 THE THREE WEIGHTS ARE IDENTICAL FROM RUNG 5, AND THE SPEED CLAUSES ARE GONE. His rows read
        //    *"Robe/Light/Heavy: mpReg x1.2, pDef +N, maxMP +M"* — one line covering all three weights,
        //    with no cast/attack-speed clause anywhere in them. That is not an omission: from 40 the
        //    penalty-cancelling belongs to the RACE masteries (Chanter Heavy for Human/Demon, Harmonist
        //    Light for Elf, Skills.Warchanter3rd.Kit.cs), and leaving a copy here would apply it TWICE.
        //    ⚠ Rung 5 used to carry `CastSpeedPct 0.90, AtkSpeedPct 1.00` on its Light row — written
        //    before the race masteries existed. Stacked with Harmonist Light's own x1.8/x2 it drove an
        //    Elf Warchanter straight into the cast-speed clamp. Do not put them back.
        BufferArmor(39, 70), BufferArmor(44, 70), BufferArmor(50, 100), BufferArmor(50, 100),
        BufferArmor(53, 140), BufferArmor(56, 140), BufferArmor(58, 150), BufferArmor(64, 150),
        BufferArmor(68, 150), BufferArmor(72, 180), BufferArmor(75, 180), BufferArmor(79, 180),
        BufferArmor(83, 200), BufferArmor(87, 200),
    };

    /// <summary>One Warchanter Armor Mastery rung: flat P.Def and Max MP in EVERY armour weight, and
    /// NOTHING ELSE — which is exactly what his one-line "Robe/Light/Heavy: pDef +X, max mp +Y;" rows say.
    ///
    /// <para>🔑 THE ×1.2 MP REGEN LEFT THIS SKILL ON 2026-08-27 — *"the mp regen is moved to the
    /// represented masteries per race (human/ork heavy, elf light)"*. It now rides on
    /// <c>WcChanterHeavy</c> (Human;Demon) and <c>WcHarmonistLight</c> (Elf) instead, in
    /// Skills.Warchanter3rd.Kit.cs. Same outcome for a buffer wearing his race's armour, and it makes
    /// the grant a REWARD FOR WEARING IT: a Human Warchanter in light armour now gets no ×1.2 at all,
    /// where this skill used to hand it to him regardless.</para>
    ///
    /// <para>⚠ The "exactly one ×1.2 per mage" rule (`BL-92`) still holds and still matters — robe gets
    /// it from the born Spellcaster Mastery, light/heavy from the race mastery. Never re-add
    /// <c>MpRegenPct</c> here: that is what made every robed mage ×1.44 the first time.</para></summary>
    private static ArmorMasteryProfile BufferArmor(int pDef, int maxMp)
    {
        var m = new StatMods(PDef: pDef, MaxMp: maxMp);
        return new ArmorMasteryProfile(Robe: m, Light: m, Heavy: m);
    }

    private static SkillDef[] HealerSkills() => new SkillDef[]
    {
        // Quick Heal — fast single-target heal (same powers as Heal, much shorter cast).
        new(QuickHeal, "Quick Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 45, CastTicks: 20, CooldownTicks: 10, Range: 600, Power: 151,
            Category: SkillCategory.Heal,  
            Description: "A fast heal on an ally (or yourself). Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 151, MpCost: 45,  SpCost: 3200,  Description: "Quick heal power 151."),
                new SkillLevel(Power: 195, MpCost: 57,  SpCost: 6400,  Description: "Quick heal power 195."),
                new SkillLevel(Power: 245, MpCost: 65,  SpCost: 12800, Description: "Quick heal power 245."),
                new SkillLevel(Power: 301, MpCost: 67,  SpCost: 25000, Description: "Quick heal power 301."),
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
            DurationTicks: 600, BuffKey: "shrouding_hymn", Rank: 1, CountsTowardBuffLimit: false,  
            Category: SkillCategory.Buff, SpCost: 880000,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            GrantsMobStealth: true,
            Description: "For 1 minute, monsters that haven't already noticed you and your nearby " +
                         "allies leave you alone. Anything already chasing keeps chasing."),

        // Party Heal — AoE heal to nearby allies (lower power than single-target).
        // ⚠ RANGE 0 IS THE MECHANIC, NOT A TIDY-UP (owner, 2026-08-28): *"The party heals (party heal,
        // great party heal, ultimate party heal) should be cast able without a target .. So 0/x
        // party/AOE"*. A support cast only takes an ally target when `def.Range > 0` (see the target
        // resolution in GameLoopService); at 0 it falls through to the self-cast branch, so the skill
        // fires with nothing selected — or with an ENEMY selected — and the circle lands on the caster.
        // Radius 1000 is his: *"party heals are 0/1000 1k range around caster"*.
        new(PartyHeal, "Party Heal", BaseClass.Mage, SkillEffect.Heal,
            MpCost: 60, CastTicks: 70, CooldownTicks: 60, Range: 0, Power: 121,
            Category: SkillCategory.Heal,
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 1000f,
            Description: "Heals you and nearby allies within 1000. Scales with WIT.",
            Levels: new[]
            {
                new SkillLevel(Power: 121, MpCost: 60,  SpCost: 3200,  Description: "Party heal power 121."),
                new SkillLevel(Power: 156, MpCost: 76,  SpCost: 6400,  Description: "Party heal power 156."),
                new SkillLevel(Power: 196, MpCost: 94,  SpCost: 12800, Description: "Party heal power 196."),
                new SkillLevel(Power: 241, MpCost: 96,  SpCost: 25000, Description: "Party heal power 241."),
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
            DurationTicks: 12000, BuffKey: "holy_speed", Rank: 1,  
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMove), CastId(FamCast), CastId(FamEva), CastId(FamAs) },
            Description: "Blesses you and nearby allies: faster casting and movement for 20 minutes.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150,  SpCost: 3200,
                    ChildBuffs: new[] { BuffSwiftU, BuffAlacrityC },
                    Description: "Move +20, Cast +15%."),
                new SkillLevel(MpCost: 160,  SpCost: 6400,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU },
                    Description: "Move +33, Cast +23%."),
                new SkillLevel(MpCost: 170,  SpCost: 12800,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityU, BuffAgilityU },
                    Description: "Move +33, Cast +23%, Evasion +2."),
                new SkillLevel(MpCost: 180,  SpCost: 25000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityU, BuffHasteC },
                    Description: "Move +33, Cast +30%, Evasion +2, Attack Speed +15%."),
                new SkillLevel(MpCost: 190,  SpCost: 50000,
                    ChildBuffs: new[] { BuffSwiftR, BuffAlacrityR, BuffAgilityR, BuffHasteU },
                    Description: "Move +33, Cast +30%, Evasion +4, Attack Speed +23%."),
                new SkillLevel(MpCost: 200,  SpCost: 100000,
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
                // 3200 / 6400 / 12800 / 25000 — his re-priced `cleric 2nd.csv` (2026-08-19). The first
                // two rungs were 9600 / 12800, which made Armor Mastery the single most expensive
                // thing a level-20 cleric could buy.
                new SkillLevel(SpCost: 3200),
                new SkillLevel(SpCost: 6400),
                new SkillLevel(SpCost: 12800),
                new SkillLevel(SpCost: 25000),
            // Rungs 5-18 = the Warchanter's 40-74 band, priced off his own SP column.
            }.Concat(BandSp14.Select(sp => new SkillLevel(SpCost: sp))).ToArray(),
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
            Category: SkillCategory.Heal,  SpCost: 12800,
            Description: "Transfers MP to an ally at no loss. Can't be used on yourself or another "
                       + "mana-restorer.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 52,  SpCost: 12800, Power: 52,
                    Description: "Transfers 52 MP to an ally (costs 52). Can't be used on yourself or another mana-restorer."),
                new SkillLevel(MpCost: 60,  SpCost: 25000, Power: 60,
                    Description: "Transfers 60 MP to an ally (costs 60). Can't be used on yourself or another mana-restorer."),
                // Level 3 = his `healer 3rd.csv` @40 row: 70 power for 70 MP, and 19k SP (it was 36k
                // before his file priced it — Restore Mana runs on the cheap support column, not the
                // combat band ladder).
                new SkillLevel(MpCost: 70,  SpCost: 19000, Power: 70,
                    Description: "Transfers 70 MP to an ally (costs 70). Can't be used on yourself or another mana-restorer."),
            }.Concat(HealerRestoreManaRungs()).ToArray()),

        // Body and Soul — the vitality group. Level 1 is exactly the +10% HP regen this buff has
        // always cast; the higher levels fold in MP regen, then Max HP, then Max MP, reaching the
        // NPC buffer's max at level 6. The four families it draws on (hp_max / mp_max / hp_regen /
        // mp_regen) are SCROLL-ONLY: there is no potion of any of them, which is why their scrolls
        // start at Epic. See docs/design/BuffLadders.md.
        new(HolyBody, "Body and Soul", BaseClass.Mage,
            SkillEffect.BuffHpRegen | SkillEffect.BuffMpRegen | SkillEffect.BuffHp | SkillEffect.BuffMp,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_body", Rank: 1,  
            Category: SkillCategory.Buff, SpCost: 25000,
            ChildBuffs: new[] { Rung(FamHpRegen, 2) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMaxHp), CastId(FamMaxMp), CastId(FamHpRegen), CastId(FamMpRegen) },
            Description: "Blesses you and nearby allies with vitality — regeneration, and at higher ranks Max HP and MP.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150,  SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 2) },
                    Description: "+10% HP regeneration."),
                new SkillLevel(MpCost: 160,  SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 3), Rung(FamMpRegen, 2) },
                    Description: "+12% HP and +10% MP regeneration."),
                new SkillLevel(MpCost: 170,  SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 4), Rung(FamMpRegen, 3), Rung(FamMaxHp, 1) },
                    Description: "+15% HP and +12% MP regeneration, +10% Max HP."),
                new SkillLevel(MpCost: 180,  SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 5), Rung(FamMpRegen, 4), Rung(FamMaxHp, 2), Rung(FamMaxMp, 1) },
                    Description: "+17% HP and +15% MP regeneration, +15% Max HP, +10% Max MP."),
                new SkillLevel(MpCost: 190,  SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamHpRegen, 6), Rung(FamMpRegen, 5), Rung(FamMaxHp, 4), Rung(FamMaxMp, 3) },
                    Description: "+20% HP and +17% MP regeneration, +25% Max HP, +20% Max MP."),
                new SkillLevel(MpCost: 200,  SpCost: 100000,
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
                CasterMastery(new PassiveEffect(MagAtk: 8,  PhysAtk: 6,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegen: 1.1f)),
                CasterMastery(new PassiveEffect(MagAtk: 10, PhysAtk: 8,  CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegen: 1.1f)),
                CasterMastery(new PassiveEffect(MagAtk: 12, PhysAtk: 10, CastSpeedPct: 0.05f, CooldownPct: 0.10f, MpRegen: 1.5f, HpRegen: 1.1f)),
                // Level 5 = the level-40 row — THE BUFFER'S since 2026-08-20 (`buffer 3rd.csv`: *"Continue
                // the line"*). It was authored off `healer 3rd.csv`, but the healer has replaced this
                // skill with Spellcaster Weapon Mastery, so the P.Atk half now belongs to the class that
                // actually swings. Note the jump: +23 M.Atk against level 4's +12 — a 3rd class is where
                // a caster's damage actually moves. ⚠ Reuse is 15% here, not the 10% of rungs 1-4: both
                // his 40-level rows say 15%, and the code had carried 10% since the rung was written.
                // ⚠ AND FROM HERE THE WEAPON PAIR CHANGES. Rungs 1-4 are the cleric's `cleric 2nd.csv`
                // rows and read "with sword/blunt". Every one of his 40+ rows reads *"With blunt/bow
                // weapon"* — because the Warchanter's three races are blunt (Human/Ork) and bow (Elf),
                // and a sword buffer is not a thing he authored. So rungs 5-18 use BufferMastery, not
                // CasterMastery. ⚠ P.Atk here is 15, not the 18 this rung carried before his file was
                // finished: his row says "mAtk +23, mAtk +15" and the CSV is the authority.
                BufferMastery(new PassiveEffect(MagAtk: 23, PhysAtk: 15, CastSpeedPct: 0.07f, CooldownPct: 0.15f, MpRegen: 1.5f, HpRegen: 1.1f)),
                BufferMastery(new PassiveEffect(MagAtk: 29, PhysAtk: 20, CastSpeedPct: 0.07f, CooldownPct: 0.15f, MpRegen: 1.9f, HpRegen: 1.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 45, PhysAtk: 25, CastSpeedPct: 0.07f, CooldownPct: 0.20f, MpRegen: 1.9f, HpRegen: 1.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 45, PhysAtk: 30, CastSpeedPct: 0.07f, CooldownPct: 0.20f, MpRegen: 2.3f, HpRegen: 1.7f)),
                BufferMastery(new PassiveEffect(MagAtk: 52, PhysAtk: 35, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.3f, HpRegen: 1.7f)),
                BufferMastery(new PassiveEffect(MagAtk: 57, PhysAtk: 40, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.3f, HpRegen: 2.1f)),
                BufferMastery(new PassiveEffect(MagAtk: 62, PhysAtk: 45, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.7f, HpRegen: 2.1f)),
                BufferMastery(new PassiveEffect(MagAtk: 67, PhysAtk: 50, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.7f, HpRegen: 2.1f)),
                BufferMastery(new PassiveEffect(MagAtk: 72, PhysAtk: 55, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.7f, HpRegen: 2.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 77, PhysAtk: 60, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 2.7f, HpRegen: 2.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 83, PhysAtk: 65, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 3.1f, HpRegen: 2.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 88, PhysAtk: 70, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 3.1f, HpRegen: 2.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 94, PhysAtk: 75, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 3.1f, HpRegen: 2.6f)),
                BufferMastery(new PassiveEffect(MagAtk: 99, PhysAtk: 80, CastSpeedPct: 0.10f, CooldownPct: 0.20f, MpRegen: 3.4f, HpRegen: 2.7f)),
            },
            Levels: new[]
            {
                new SkillLevel(SpCost: 3200,  Description: "With sword/blunt: +6 M.Atk, +4 P.Atk, -10% skill reuse."),
                // 6400 again: he re-priced this rung himself on 2026-08-19, so the ladder is the plain
                // 3200 / 6400 / 12800 / 25000. (It was 12800 for two days — his 2026-08-17 sheet had it
                // that way and this comment used to explain why. His sheet is still the authority; the
                // number it carries just changed back.)
                new SkillLevel(SpCost: 6400, Description: "With sword/blunt: +8 M.Atk, +6 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 12800, Description: "With sword/blunt: +10 M.Atk, +8 P.Atk, +5% cast, -10% reuse, +10% MP regen."),
                new SkillLevel(SpCost: 25000, Description: "With sword/blunt: +12 M.Atk, +10 P.Atk, +5% cast, -10% reuse, +50% MP regen, +10% HP regen."),
            }.Concat(BandSp14.Select(sp => new SkillLevel(SpCost: sp))).ToArray()),

        // Force and Ward — the caster's group. Levels 1-2 are the numbers this buff already cast
        // (+18% interrupt resist, then +25 with +25% M.Atk); from level 3 it adds M.Def, and at 6
        // it equals the NPC buffer. M.Atk and M.Def each have a potion AND a scroll, so a Force
        // potion overrides only the M.Atk part. Resolve (interrupt resist) has no consumable at
        // all — a buffer is the only source. See docs/design/BuffLadders.md.
        new(HolyForce, "Force and Ward", BaseClass.Mage,
            SkillEffect.BuffInterruptResist | SkillEffect.BuffMagAtk | SkillEffect.BuffMagicDef,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_force", Rank: 1,  
            Category: SkillCategory.Buff,
            ChildBuffs: new[] { BuffIntr1 },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamMagAtk), CastId(FamMagDef), CastId(FamInterrupt) },
            Description: "Steadies the casting of you and nearby allies; higher ranks add Magic Attack and Magic Defence.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150,  SpCost: 3200,
                    ChildBuffs: new[] { BuffIntr1 },
                    Description: "+18% interrupt resistance (harder to cancel your casts)."),
                new SkillLevel(MpCost: 160,  SpCost: 6400,
                    ChildBuffs: new[] { BuffIntr2, BuffMAtk2 },
                    Description: "+25% interrupt resistance and +25% M.Atk."),
                new SkillLevel(MpCost: 170,  SpCost: 12800,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk2, BuffMDef1 },
                    Description: "+40% interrupt resistance, +25% M.Atk, +10% M.Def."),
                new SkillLevel(MpCost: 180,  SpCost: 25000,
                    ChildBuffs: new[] { BuffIntr4, BuffMAtk4, BuffMDef2 },
                    Description: "+40% interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 190,  SpCost: 50000,
                    ChildBuffs: new[] { BuffIntr7, BuffMAtk4, BuffMDef2 },
                    Description: "+54% interrupt resistance, +32% M.Atk, +20% M.Def."),
                new SkillLevel(MpCost: 200,  SpCost: 100000,
                    ChildBuffs: new[] { BuffIntr7, BuffMAtk4, BuffMDef4 },
                    Description: "+54% interrupt resistance, +32% M.Atk, +30% M.Def."),
            }),

        // Focus and Ferocity — the critical group. Level 1 is the +20% crit rate this buff has
        // always cast; the rest add crit damage and then magic crit, reaching the NPC buffer's
        // max at 6. All three families are SCROLL-ONLY (Epic and up).
        new(HolyFocus, "Focus and Ferocity", BaseClass.Mage,
            SkillEffect.BuffCritRate | SkillEffect.BuffCritDamage | SkillEffect.BuffMagicCritRate,
            MpCost: 150, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_focus", Rank: 1,  
            Category: SkillCategory.Buff, SpCost: 6400,
            ChildBuffs: new[] { Rung(FamCritRate, 4) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamCritRate), CastId(FamCritDmg), CastId(FamMagCrit) },
            Description: "Sharpens you and nearby allies: critical rate, and at higher ranks critical damage and magic criticals.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 150,  SpCost: 6400,
                    ChildBuffs: new[] { Rung(FamCritRate, 4) },
                    Description: "+20% physical critical rate."),
                new SkillLevel(MpCost: 160,  SpCost: 12800,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 1) },
                    Description: "+25% critical rate, +10% critical damage."),
                new SkillLevel(MpCost: 170,  SpCost: 25000,
                    ChildBuffs: new[] { Rung(FamCritRate, 5), Rung(FamCritDmg, 3), Rung(FamMagCrit, 1) },
                    Description: "+25% critical rate, +20% critical damage, +20% magic critical rate."),
                new SkillLevel(MpCost: 180,  SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 4), Rung(FamMagCrit, 2) },
                    Description: "+30% critical rate, +25% critical damage, +35% magic critical rate."),
                new SkillLevel(MpCost: 190,  SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamCritRate, 6), Rung(FamCritDmg, 5), Rung(FamMagCrit, 4) },
                    Description: "+30% critical rate, +30% critical damage, +65% magic critical rate."),
                new SkillLevel(MpCost: 200,  SpCost: 100000,
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
            DurationTicks: 12000, BuffKey: "holy_frenzy", Rank: 1,  
            Category: SkillCategory.Buff, SpCost: 25000,
            ChildBuffs: new[] { Rung(FamFrenzy, 1) },
            Description: "A reckless surge: less Max HP/MP, but more attack and speed for 20 minutes.",
            Levels: new[]
            {
                // ⚠ Level 1 is 40 MP / 12800 SP, his 2026-08-19 `cleric 2nd.csv` row (8 + 32). That is a
                // third of what it used to cost (125 / 25000) and it does NOT propagate up the ladder —
                // rungs 3 and 6 are his buffer prices and are unchanged, so the jump from L1 to L3 is
                // steep on purpose: a level-35 cleric's Frenzy is cheap, a buffer's is not.
                new SkillLevel(MpCost: 40,  SpCost: 12800,
                    ChildBuffs: new[] { Rung(FamFrenzy, 1) },
                    // ⚠ WAS "−30% Max HP/MP … −8 evasion", and had been stale since the rung was
                    // re-authored: `FrenzyRung(1, 0.07f, 0.05f, 5, eva: 5)` gives −7% and −5. His
                    // `cleric 2nd.csv` row agrees with the RUNG, so the prose was the wrong half.
                    // It only surfaced once the card started printing the child's real numbers
                    // underneath the sentence that contradicted them.
                    Description: "−7% Max HP/MP, +5% offence and speed, +5 move, −5 evasion."),
                // ⚠ Level 2 is 80 MP / 38k SP — his `healer 3rd.csv` @52 row, which is where BOTH the
                // healer and the buffer learn this rung (*"Frenzy(L2) is learned from Healers and
                // Buffers at 52"*). It was 135 / 25000 before his file priced it.
                new SkillLevel(MpCost: 80,  SpCost: 38000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 2) },
                    Description: "−10% Max HP/MP, +8% offence and speed, +8 move, −8 evasion."),
                new SkillLevel(MpCost: 145,  SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 3) },
                    Description: "−22% Max HP/MP, +6% offence and speed, +6 move, −8 evasion."),
                new SkillLevel(MpCost: 155,  SpCost: 50000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 4) },
                    Description: "−18% Max HP/MP, +7% offence and speed, +7 move, −8 evasion."),
                new SkillLevel(MpCost: 165,  SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 5) },
                    Description: "−14% Max HP/MP, +7% offence and speed, +7 move, −8 evasion."),
                new SkillLevel(MpCost: 175,  SpCost: 100000,
                    ChildBuffs: new[] { Rung(FamFrenzy, 6) },
                    Description: "−10% Max HP/MP, +8% offence and speed, +8 move, −8 evasion."),
            }),

        // ===== SHIELD BLESS AND HARDEN — the shield group (`buffer 3rd.csv` @66, owner 2026-08-19) ==
        //
        // 🔑 A REAL GROUP, on his word: *"tje shield bless & harder is a copy .. so it should rapladse
        // Shield Harder and Shield Bless"*. So it names both singles in `ChildBuffs` (which is what
        // makes the engine treat it as a group — it covers their families at GROUP rank, evicting them
        // and refusing anything weaker afterwards) and in `Replaces` (which collapses them off the
        // learn list, the same way Might and Bulwark eats its four).
        //
        // ⏰ ITS CHILDREN ARE RUNG 1 ONLY BECAUSE RUNG 1 IS ALL THAT EXISTS. He is authoring the
        // singles' ladder in the healer file right now (Shield Hardening is drafted at +5% @40, +10% @48),
        // and *"once the healers buffs are in place they will be the same for the buffer 3rd"*. When
        // they land, each family becomes a real ladder and these two `Rung(..., 1)` calls point at the
        // TOP index instead. Same for levels: the groups and Harmonies get them at 60+ — this def has
        // one level today because there is one rung to hand out.
        //
        // 🔑 BLOCK CHANCE, NOT BLOCK REDUCTION (owner, playtest 22): reduction is never raised. Both
        // numbers are a PERCENT of what the shield already carries, so the buff is self-gating — a
        // character with no shield has 0 shield defence and 0 block chance, and 0 × 1.5 is still 0.
        // BlockChance is clamped to StatCaps.BlockChance afterwards, as always.
        new(HolyShield, "Shield Bless and Harden", BaseClass.Mage,
            SkillEffect.BuffShieldDef | SkillEffect.BuffBlockChance,
            MpCost: 200, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "holy_shield", Rank: 1,  
            Category: SkillCategory.Buff, SpCost: 100000,
            ChildBuffs: new[] { Rung(FamShieldDef, 3), Rung(FamShieldBlock, 6) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            Replaces: new[] { CastId(FamShieldDef), CastId(FamShieldBlock) },
            Description: "Blesses you and nearby allies with a sturdier shield: +50% shield P.Def and "
                       + "+30% block chance for 20 minutes. Does nothing for anyone not carrying one."),

        // ═══ WAR FRENZY — the party Frenzy, at 56 (`buffer 3rd.csv` row 36) ══════════════════════
        //
        // 🔑 THIS IS "MADNESS", RENAMED AND RE-LEVELLED (owner, 2026-08-21: *"madness is now named War
        // Frenzy and its the 56lvl group frenzy buff"*). The ID IS STILL `madness` and must stay that
        // way — ids are append-only and characters carry them. Read the comment, not the const.
        //
        // What changed with the rename, and why each part moved:
        //  • **Level 76 → 56.** Its old home was explicitly temporary — *"put it at 76 on the buffer …
        //    and when the kits land we will move it"*. His kit landed; this is the move.
        //  • **Rung 7 → rung 2.** His row's payload is the SAME text as Frenzy @52, which is rung 2.
        //    That is deliberate: War Frenzy's reward is the PARTY, not a bigger number. It arrives
        //    one learn tier after Frenzy tops out at 52, exactly like every other party echo.
        //  • **MP 220 → 165, SP 100k → 45k.** His column, and it satisfies the group-MP rule:
        //    Frenzy's top-rung 80 + the 85 his band charges at level 56.
        //  • **`Replaces` added.** His REPLACES column says `[Frenzy]`, so the single collapses off
        //    the learn list once the party version is bought.
        //
        // ⚠ NOTHING REACHES `FamFrenzy` RUNG 7 ANY MORE. It was invented for the level-76 Madness and
        // is now a rung no CSV names — allowed (the Resolve family carries one too), but do not treat
        // it as live content. ⚠ And note rungs 3-6 are still WEAKER than his authored rung 2; that
        // regression predates this change and is his to rule on.
        //
        // Party-targeted for the same reason every group is: the autopilot hard-targets SELF for
        // buffs, so a single-target version could never be handed out by a buffer left on auto-farm.
        new(WarFrenzy, "War Frenzy", BaseClass.Mage,
            SkillEffect.BuffHp | SkillEffect.BuffMp | SkillEffect.BuffPhysAtk | SkillEffect.BuffMagAtk
            | SkillEffect.BuffCastSpeed | SkillEffect.BuffAtkSpeed | SkillEffect.BuffMoveSpeed
            | SkillEffect.BuffEvasion,
            MpCost: 165, CastTicks: 10, CooldownTicks: 10, Range: 600, Power: 0,
            DurationTicks: 12000, BuffKey: "madness", Rank: 1,
            Category: SkillCategory.Buff, SpCost: 45000,
            ChildBuffs: new[] { Rung(FamFrenzy, 2) },
            TargetMode: TargetMode.AlliesInRadius, AreaRadius: 800f,
            // ⚠ `HolyFrenzy` IS THE SINGLE, and it is the one that has to be named here. This list
            // read `CastId(FamFrenzy)` alone — `cast_frenzy`, the generic ladder caster — and NO CLASS
            // LEARNS THAT ID: both the Warchanter (52) and the Lightbringer (52) are granted
            // `holy_frenzy`, the def whose display name is plainly "Frenzy". So his REPLACES column
            // said `[Frenzy]`, the code agreed on paper, and the skill it named was never on anyone's
            // learn list — the single survived buying the party version (owner, 2026-08-21:
            // *"haven't removed my learned Frenzy skill .. Should replace it"*). `cast_frenzy` is kept
            // beside it so a future class granted the generic ladder collapses the same way.
            Replaces: new[] { HolyFrenzy, CastId(FamFrenzy) },
            Description: "Drives you and nearby allies to fight recklessly: −10% Max HP/MP, but +8% "
                       + "P.Atk / M.Atk / attack & cast speed, +8 Move Speed and −8 Evasion for 20 minutes."),

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
            Category: SkillCategory.Heal,  SpCost: 3200,
            DispelMask: SkillEffect.Poison | SkillEffect.Venom | SkillEffect.Bleed,
            DispelMaxLevel: 1,
            Description: "Cures poison, venom and bleed from an ally (or self), up to a rank its own "
                       + "level can reach.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 16,  SpCost: 3200, DispelMaxLevel: 1,
                    Description: "Cures poison, venom and bleed of rank 1 or lower from an ally (or self)."),
                // 25000, his 2026-08-19 price — the second cure is a level-35 purchase, not a cheap one.
                new SkillLevel(MpCost: 20,  SpCost: 25000, DispelMaxLevel: 2,
                    Description: "Cures poison, venom and bleed of rank 2 or lower from an ally (or self)."),
                // Levels 3-9 = his `healer 3rd.csv` Antidote rows, at 44/52/58/62/66/70/74 — SEVEN rungs
                // reaching rank 9, RE-RULED 2026-08-20.
                //
                // 🔑 THE CEILING IS ONE BAND BEHIND THE ELF'S HEALER BLESSING, which is the whole design:
                // the Elf's heal-and-cure gets each rank first and Antidote catches up a rung later, so a
                // dedicated cure never out-reaches the race that specialises in curing. See
                // SkillCatalog.HealerAntidoteRungs for his wording and for why his level-64 row went.
            }.Concat(HealerAntidoteRungs()).Concat(HealerFourthAntidoteRungs()).ToArray()),

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
        // MP is his too (2026-08-17): 60 at level 1, 90 at level 2 — roughly HALF what this skill used
        // to cost (120/150). Those were authored as the CSV's old two columns (10+50 / 20+70); the
        // sheet carries one TOTAL now and the engine splits every skill 20/80 (`SkillMath.InitialMpFraction`).
        new(Resurrection, "Resurrection", BaseClass.Mage, SkillEffect.None,
            MpCost: 60, CastTicks: 100, CooldownTicks: 100, Range: 600, Power: 0,
            Category: SkillCategory.Heal,  
            Resurrect: true, ResExpPct: 0f,
            Description: "Revives a fallen ally at 30% HP and MP. Higher levels also give back part of the "
                       + "experience they lost on death.",
            Levels: new[]
            {
                // SP 1700, his 2026-08-19 price: the first res is meant to be affordable the moment a
                // cleric can cast it, so it costs about the same as one rung of a buff.
                new SkillLevel(MpCost: 60,  SpCost: 1700,  ResExpPct: 0f,
                    Description: "Revive at 30% HP/MP. Does not give back any lost exp."),
                new SkillLevel(MpCost: 90,  SpCost: 12800, ResExpPct: 0.20f,
                    Description: "Revive at 30% HP/MP; restore 20% of lost exp."),
                // Levels 3-16 = his `healer 3rd.csv` rows, 40 → 74. The exp restored climbs 35% → 100%
                // in even 5-point steps, so a level-74 Lightbringer undoes a death completely.
                //
                // 🔑 THE CAST TIME IS PART OF THE LADDER — 10s at 40 down to 5s from 62 — which is the
                // only ladder in the game that shortens a cast, and the reason `SkillLevel.CastTicks`
                // exists at all. A res you can land inside a fight is something you BUY.
                // 🔑 SP IS THE BUFF LADDER, rung for rung — his ruling 2026-08-20: *"Resurrection sp
                // should match the buffs of the same lvl"*. Not the far dearer COMBAT band ladder an
                // attack or a heal runs on: a res is priced like a blessing learned at the same level.
            }.Concat(HealerResurrectionRungs()).Concat(HealerFourthResurrectionRungs()).ToArray()),
    };
}
