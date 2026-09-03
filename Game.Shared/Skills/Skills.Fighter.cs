namespace Game.Shared;

/// <summary>Base Fighter kit — the skills available to all fighters before (and
/// after) the level-20 class change. Archer's bow skill (Heavy Draw) lives here
/// too, since archers are a fighter archetype and have no separate base file.</summary>
public static partial class SkillCatalog
{
    public const string PowerStrike = "power_strike";
    public const string WarCry = "war_cry";
    public const string GreaterWarCry = "greater_war_cry";
    public const string BattleFury = "battle_fury";
    public const string Fortify = "fortify";
    public const string ShieldMastery = "shield_mastery";
    public const string MightyBlow = "mighty_blow";
    public const string TwinSlash = "twin_slash";
    public const string PowerShot = "power_shot";
    public const string Disrupt = "disrupt";
    public const string CleavingStrike = "cleaving_strike";   // first "[Double]" skill (warrior)
    public const string ShieldBash = "shield_bash";           // physical Stun (contested CC)
    public const string TerrifyingRoar = "terrifying_roar";   // physical Fear (contested CC)
    public const string Hamstring = "hamstring";              // physical Slow (contested CC)
    public const string WarFocus = "war_focus";               // self-buff: +skill damage
    public const string Rupture = "rupture";                  // applies Bleed (DoT stacks)
    public const string DetonateWounds = "detonate_wounds";   // burst: consumes Bleed stacks
    public const string ToxicSting = "toxic_sting";           // applies Poison (DoT + -AS/cast)
    public const string ToxicBurst = "toxic_burst";           // burst: consumes Poison stacks
    public const string Envenom = "envenom";                  // applies Venom (DoT + -atk/-def)
    public const string VenomBurst = "venom_burst";           // burst: consumes Venom stacks
    public const string Aegis = "aegis";                      // self absorb-shield (% max HP)
    public const string LastStand = "last_stand";             // survive one fatal blow (lethal save)
    public const string Indomitable = "indomitable";          // tank ult: +cancel resist
    public const string Provoke = "provoke";                  // taunt: force a mob onto the tank
    public const string Lure = "lure";                        // rogue: MOB-ONLY taunt, pulls one out of a camp
    public const string Shadowstep = "shadowstep";            // blink behind target + hit
    public const string RepellingShot = "repelling_shot";     // ranged hit + knockback
    public const string Vanish = "vanish";                    // Phantom: full HIDE (kind 1)
    public const string Prowl = "prowl";                      // rogue TOGGLE: unaggroed mobs ignore you (kind 2)
    public const string SignalFlare = "signal_flare";         // archer AoE: strips HIDE + bars re-hiding
    public const string SnareTrap = "snare_trap";             // Trapper: place a rooting damage trap

    // --- Base fighter CORE actives (CSV fighter 1st, continuing into 2nd-class) ---
    public const string Strike = "strike";                    // sword/blunt attack (can double)
    public const string Stab = "stab";                        // dual BLOW (full on crit, else 10%)
    public const string Shot = "shot";                        // bow ranged attack
    public const string FighterArmorMastery = "fighter_armor_mastery";   // all-weight def + mpReg
    public const string FighterWeaponMastery = "fighter_weapon_mastery"; // any-weapon +p.Atk

    // 2nd-class continuations of the base attack chain (each REPLACES the base skill(s) —
    // same pattern as the mage bolt chain). Warriors keep only melee; rogues keep stab+bow.
    public const string Smash = "smash";                           // warrior: continues Strike; replaces Strike/Stab/Shot
    public const string PiercingStab = "piercing_stab";            // rogue: continues Stab; replaces Stab/Strike
    public const string PreciseShot = "precise_shot";              // rogue: continues Shot (range 700); replaces Shot/Strike

    // --- Warrior 2nd-class (CSV warrior 2nd) ---
    // (`body_mastery` — deleted 2026-08-29, his ruling: *"in warrior 2nd file the body_mastery
    //  should be removed (it's hp_boost)"*. Its max-HP half duplicated HP Boost, which the warrior
    //  already learns at 20/28/36; its hpReg half moved into `warrior_armor_mastery`, on every
    //  weight. Retired ids die on load (PersistenceService.ParseLearnedSkills) so no migration.
    //  Don't re-add it.)
    public const string BattleRegeneration = "battle_regeneration";// self-heal 10% max HP
    public const string BattlePresence = "battle_presence";        // HP<60% stance: +p.Atk
    public const string BattleDefence = "battle_defence";          // HP<60% stance: +p.Def

    // --- Tank 2nd-class (CSV tank 2nd) ---
    public const string TankShieldMastery = "tank_shield_mastery"; // passive: +shield def/rate + bow resist
    public const string TankAntiMagic = "tank_anti_magic";         // passive: +magic def
    public const string DefensiveWall = "defensive_wall";          // huge def buff (self, -move)
    public const string TankShieldStun = "tank_shield_stun";       // stun 9s
    public const string TankStay = "tank_stay";                    // root/hold 15s

    // --- Rogue 2nd-class (CSV rogue 2nd) ---
    public const string Sprint = "sprint";                         // burst move-speed buff
    public const string BowExpertise = "bow_expertise";            // bow attack-speed buff
    public const string EvasionBoost = "evasion_boost";            // the rogue's ultimate: +evasion, 30s

    // Base-fighter armor mastery per level (@5/10/15): flat P.Def + MP-regen for ALL weights;
    // at level 3 light armor also aids evasion. No off-weight penalty (fighters adapt).
    private static readonly ArmorMasteryProfile[] FighterArmorLevels = new[]
    {
        new ArmorMasteryProfile(
            new StatMods(PDef: 9,  MpRegenPct: 0.1f),
            new StatMods(PDef: 9,  MpRegenPct: 0.1f),
            new StatMods(PDef: 9,  MpRegenPct: 0.1f)),
        new ArmorMasteryProfile(
            new StatMods(PDef: 12, MpRegenPct: 0.1f),
            new StatMods(PDef: 12, MpRegenPct: 0.1f),
            new StatMods(PDef: 12, MpRegenPct: 0.1f)),
        new ArmorMasteryProfile(
            new StatMods(PDef: 14, MpRegenPct: 0.1f),
            new StatMods(PDef: 14, MpRegenPct: 0.1f, Evasion: 3),
            new StatMods(PDef: 14, MpRegenPct: 0.1f)),
    };

    private static SkillDef[] FighterSkills() => new SkillDef[]
    {
        // ===== Base fighter CORE kit (learned @5/10/15; Strike/Stab/Shot continue into
        //       the 2nd-class warrior/rogue tables via higher levels of the SAME skill) =====

        // Strike — sword/blunt melee skill; adds power to the hit, can [Double].
        new(Strike, "Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 35,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt,
            Description: "A weapon strike (sword or blunt) that adds power to your attack. Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 35,  MpCost: 10,  SpCost: 160,   Description: "Strike — power 35."),
                new SkillLevel(Power: 65,  MpCost: 13,  SpCost: 910,   Description: "Strike — power 65."),
                new SkillLevel(Power: 84,  MpCost: 17,  SpCost: 910,   Description: "Strike — power 84."),
            }),

        // Stab — dagger (dual) BLOW: full power only on a critical/double, else a soft 10%.
        new(Stab, "Stab", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 88,
            Category: SkillCategory.Physical, CanDouble: true, BlowOnCrit: true, CritRateMod: 2.0f,
            RequiredWeapon: WeaponType.Dual,
            Description: "A dagger blow (duals). Lands for FULL power only on a critical or double — a soft 10% otherwise.",
            Levels: new[]
            {
                new SkillLevel(Power: 88,  MpCost: 10,  SpCost: 160,   Description: "Stab — blow power 88 (10% without a crit)."),
                new SkillLevel(Power: 137, MpCost: 11,  SpCost: 910,   Description: "Stab — blow power 137."),
                new SkillLevel(Power: 210, MpCost: 15,  SpCost: 910,   Description: "Stab — blow power 210."),
            }),

        // Shot — bow ranged attack; can [Double]. Base reach 350 (rogue extends it later).
        new(Shot, "Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 20, CastTicks: 30, CooldownTicks: 60, Range: 350, Power: 78,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.Bow,
            Description: "A bow shot dealing heavy ranged damage (fighter reach 350). Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 78,  MpCost: 20,  SpCost: 160,   Description: "Shot — power 78."),
                new SkillLevel(Power: 122, MpCost: 25,  SpCost: 910,   Description: "Shot — power 122."),
                new SkillLevel(Power: 187, MpCost: 34,  SpCost: 910,   Description: "Shot — power 187."),
            }),

        // Armor Mastery — base fighter, all-weight defence + MP regen (data-driven).
        new(FighterArmorMastery, "Armor Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Improves defence and MP regen with any armor weight "
                       + "(light armor also aids evasion at higher levels).",
            Levels: new[]
            {
                new SkillLevel(SpCost: 160),
                new SkillLevel(SpCost: 910),
                new SkillLevel(SpCost: 910),
            },
            ArmorMasteryLevels: FighterArmorLevels),

        // Weapon Mastery — base fighter, flat + % attack power with ANY weapon.
        new(FighterWeaponMastery, "Weapon Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Increases physical attack power with any weapon equipped.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 160, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 2)),
                new SkillLevel(SpCost: 910, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 3)),
                new SkillLevel(SpCost: 910, Passive: new PassiveEffect(PhysAtkPct: 0.085f, PhysAtk: 4)),
            }),

        // ===== 2nd-class attack-chain continuations (each REPLACES the base skill(s)) =====

        // Smash — WARRIOR: continues the Strike chain and REPLACES Strike/Stab/Shot (a warrior
        // keeps only the melee line). Sword/blunt, can [Double]. 5 levels @20/24/28/32/36.
        new(Smash, "Smash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 20, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 105,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            Replaces: new[] { Strike, Stab, Shot },
            Description: "A crushing sword/blunt blow — the warrior's Strike upgrade. Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 105, MpCost: 20,  SpCost: 3400,  Description: "Smash — power 105."),
                new SkillLevel(Power: 143, MpCost: 23,  SpCost: 6400,  Description: "Smash — power 143."),
                new SkillLevel(Power: 191, MpCost: 25,  SpCost: 12000, Description: "Smash — power 191."),
                new SkillLevel(Power: 251, MpCost: 30,  SpCost: 22000, Description: "Smash — power 251."),
                new SkillLevel(Power: 326, MpCost: 35,  SpCost: 40000, Description: "Smash — power 326."),
            }),

        // Piercing Stab — ROGUE: continues the Stab BLOW chain, REPLACES Stab + Strike. Dual.
        // 5 levels @20/24/28/32/36.
        new(PiercingStab, "Piercing Stab", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 18, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 314,
            Category: SkillCategory.Physical, CanDouble: true, BlowOnCrit: true, CritRateMod: 2.0f,
            RequiredWeapon: WeaponType.Dual,
            Replaces: new[] { Stab, Strike },
            Description: "A precise dagger blow — the rogue's Stab upgrade. Full power only on a "
                       + "critical/double (a soft 10% otherwise).",
            Levels: new[]
            {
                new SkillLevel(Power: 314, MpCost: 18,  SpCost: 1700,  Description: "Piercing Stab — blow power 314."),
                new SkillLevel(Power: 427, MpCost: 21,  SpCost: 3200,  Description: "Piercing Stab — blow power 427."),
                new SkillLevel(Power: 571, MpCost: 24,  SpCost: 6000,  Description: "Piercing Stab — blow power 571."),
                // 28, not the CSV's original 58: he ruled it a typo on 2026-08-11 (*"should be 28.. a
                // typeo"*) and edited `rogue 2nd.csv` line 19 himself. It had sat between level 3's
                // 24 and level 5's 30 as the one spike in the line.
                new SkillLevel(Power: 752, MpCost: 28,  SpCost: 11000, Description: "Piercing Stab — blow power 752."),
                new SkillLevel(Power: 977, MpCost: 30,  SpCost: 20000, Description: "Piercing Stab — blow power 977."),
            }),

        // Precise Shot — ROGUE: continues the Shot chain at RANGE 700, REPLACES Shot + Strike.
        // Bow, can [Double]. 5 levels @20/24/28/32/36. (3rd-class Double Shot @900 comes later.)
        new(PreciseShot, "Precise Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 40, CastTicks: 30, CooldownTicks: 60, Range: 700, Power: 279,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.Bow,
            Replaces: new[] { Shot, Strike },
            Description: "A long-range aimed shot (reach 700) — the rogue's Shot upgrade. Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 279, MpCost: 40,  SpCost: 1700,  Description: "Precise Shot — power 279."),
                new SkillLevel(Power: 379, MpCost: 45,  SpCost: 3200,  Description: "Precise Shot — power 379."),
                new SkillLevel(Power: 507, MpCost: 53,  SpCost: 6000,  Description: "Precise Shot — power 507."),
                new SkillLevel(Power: 669, MpCost: 34,  SpCost: 11000, Description: "Precise Shot — power 669."),
                new SkillLevel(Power: 868, MpCost: 67,  SpCost: 20000, Description: "Precise Shot — power 868."),
            }),

        // ===== Warrior 2nd-class (CSV warrior 2nd) =====

        // Battle Regeneration — instant self-heal for 10% of max HP (short cast, 90s cooldown).
        new(BattleRegeneration, "Battle Regeneration", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 25, CastTicks: 5, CooldownTicks: 900, Range: 0, Power: 0,
            Category: SkillCategory.Heal, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 6000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.10f, ModifierMode.Percent) },
            Description: "Restores 10% of your maximum HP instantly (90s reuse)."),

        // Battle Presence — LOW-HP offensive stance (usable only at ≤60% HP): +35% P.Atk and
        // +2 accuracy for 90s. Requires a sword/blunt; shares the "battle_stance" key with
        // Battle Defence, so activating one ends the other (mutually exclusive).
        new(BattlePresence, "Battle Presence", BaseClass.Fighter,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAccuracy,
            MpCost: 20, CastTicks: 5, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "battle_stance", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 11000,
            RequireHpBelowFraction: 0.60f, RequiredWeapon: WeaponType.AnySword | WeaponType.AnyBlunt, RequiredHands: WeaponHands.Two,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffPhysAtk, 0.35f),
                new(SkillEffect.BuffAccuracy, 2, ModifierMode.Flat),
            },
            Description: "A desperate offensive: +35% P.Atk and +2 accuracy for 90s. Usable only at "
                       + "≤60% HP with a sword/blunt. Cannot be combined with Battle Defence."),

        // Battle Defence — LOW-HP defensive stance (usable only at ≤60% HP): DOUBLE P.Def for
        // 90s. Shares "battle_stance" with Battle Presence (mutually exclusive).
        new(BattleDefence, "Battle Defence", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "battle_stance", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 20000,
            RequireHpBelowFraction: 0.60f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 1.0f, ModifierMode.Percent) },
            Description: "A desperate defence: DOUBLES your P.Def for 90s. Usable only at ≤60% HP. "
                       + "Cannot be combined with Battle Presence."),

        // ===== Tank 2nd-class (CSV tank 2nd) =====

        // Shield Mastery — PASSIVE (4 levels): scales the equipped shield's block chance and defence.
        // 🔑 TWO CLASSES LEARN IT, ON DIFFERENT SCHEDULES (2026-08-21, his `tank 2nd`/`tank 3rd`/
        // `buffer 3rd` rows). The TANK takes rungs 1-3 at 20/28/36 and rung 4 at 52 (3rd class); the
        // HUMAN WARCHANTER takes rungs 1-3 at 40/60/70 and never reaches rung 4. Same ability, same
        // magnitudes — only the learn levels and the SP differ, which is why the price is a per-class
        // `ClassSkill.SpCost` override rather than a second SkillDef.
        //
        // 🔑 BOW RESISTANCE RESTORED 2026-08-21, on rungs 3 and 4 only. It vanished when he re-authored
        // the rows; asked about it he said *"My mistake in the hurry .. Make lvl 3 +16% and lvl 4 +24%
        // bow resist"*. This passive is its ONLY carrier in the whole player kit — `PassiveEffect.BowResist`,
        // `Entity.BowResist` and `SkillEffect.BuffBowResist` have no other source — so deleting it from
        // these two lines makes a built stat unreachable. Note it moved UP the ladder: it used to start
        // at rung 2 (16/16/24), it now starts at rung 3 (—/—/16/24).
        // 🔑 ShieldDefPct is HIS IG PERCENTAGE x5, and that is NOT a buff — it is the other half of
        // cutting every shield's flat defence 5x (Items.cs shDef). His CSV rows are authored in IG
        // units (30/40/50/60%) and this ladder is the compensated build (150/200/250/300%) — his
        // instruction, 2026-08-21: *"the % of the shield mastery are the IG one so fix them in the
        // process"*. The DESCR checker knows: see the `("shield mastery","shielddef")` entry in
        // tools/SkillCsvSeed/Descr.cs, which prints both numbers as ⚪ RULED rather than a defect.
        // His words when the pair was made:
        // "same as .2 just to increase the shield Defence increase skills/passives — 40% tanks to become
        //  200% ... 51 -> 153 ... which is good now for 61lvl without the 3rd class kits". The point of
        // the PAIR (the item cut and this raise) is WHO keeps the defence: a shielded mage/cleric drops
        // to the item's own small number ("just a help for a cleric -15% received dmg -> not
        // immortality") while the tank, who paid SP for it, keeps a meaningful one. Never move one of
        // the two without the other.
        //
        // 🔑 AND THIS LINE IS THE *ONLY* THING THAT SCALED — his ruling, 2026-08-12, do not extend it:
        //     "sheild_mastery.Shield_PDef will be the only part that will increase 5 times the sheild
        //      chance, arrow defence and other passives, sets and buffs that increase the shieldPdef/
        //      chance etc are kept as is."
        // So BlockChancePct below is untouched — "Shield Rate" is copied from his row verbatim
        // (50/70/85/100%) — the Shield Mastery BUFF keeps its +50%, and the heavy sets'
        // "shield.p.def x1.25" clauses keep theirs. Those are all percentages of a number that is now
        // a fifth of what it was, and he wants them that way.
        //
        // 🔑🔑 THE WHOLE SKILL IS `heavy/shield` — EVERY RUNG, not just the "+10% P.Def" (owner,
        // 2026-08-29, `BL-107`). He asked for `/shield` on rungs 1-3 and `heavy/shield` on rung 4 and
        // then changed his own mind on sight, and his reason is a CLASS-BALANCE one, not a flavour one:
        //     *"if we allow the human buffer on a robe+shield … he have additional bonus on the shield
        //      when the other 2 buffers wearing any+shield will get only one. So the human buffer
        //      chooses heavy+shield for becoming semitank and robe+shield works as the other 2 buffers
        //      (no shield bonuses except the 4th class one) … giving more pDef and shield rate+Def on a
        //      robe pushes one class in front a lot."*
        //
        // 🔑 THAT IS THE POINT OF THE GATE HERE: it turns a free bonus into a CHOICE. The Human
        // Warchanter can wear heavy and be a semi-tank, or wear robe and be the buffer his two brothers
        // are — but he cannot have his robe and the shield ladder too. The comparison he is protecting
        // is with the ELF and DEMON buffers, who never had this skill; the demon's two-handed blunt lean
        // (a little P.Atk and accuracy) is a much smaller edge than shield P.Def + block rate on a robe.
        // ⚠ THE 4TH-CLASS SHIELD PASSIVE IS NOT THIS ONE and keeps its plain `/shield` gate — that is
        // the "additional bonus" every buffer gets, and it is what makes robe+shield still worth it.
        //
        // ⚠ SO IT IS ONE PassiveEffect AGAIN, not two. `BL-107` built `SkillLevel.ExtraPassives` for
        // exactly this skill — one rung, two different gates — and his ruling the same day collapsed the
        // two gates into one, so the layer was folded back in. The mechanism stays (see SkillLevel); it
        // simply has no author today. If a rung ever needs two gates again, that is the tool.
        // ⚠ The "+10% P.Def" on rungs 3-4 is the WHOLE physical defence — armour, jewels, the lot — not
        // the shield's own share (that is ShieldDefPct). Plain `DefencePct` under the rung's gate.
        new(TankShieldMastery, "Shield Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. In HEAVY armor with a shield: greatly improves your shield's "
                       + "block chance and defence and, from level 3, your whole P.Def and your "
                       + "resistance to bows. In any other armor it does nothing.",
            Levels: new[]
            {
                // SP here is the TANK's price (his 20/28/36/52 rows). The Human Warchanter's
                // 36000/120000/390000 comes from the ClassSkill.SpCost override on its own table.
                // ⚠ THE GATE IS REAL ON EVERY FIELD since 2026-08-29 (`BL-107`), and two of them only
                // LOOKED gated before: ShieldDefPct and BlockChancePct scale the shield's OWN numbers
                // and are inert with an empty off-hand by accident, but BOW RESISTANCE is an ordinary
                // character stat — rungs 3-4 were paying it to a tank holding a greatsword.
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(RequiresShield: true, RequiredArmor: ArmorWeights.Heavy, ShieldDefPct: 1.50f, BlockChancePct: 0.50f)),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(RequiresShield: true, RequiredArmor: ArmorWeights.Heavy, ShieldDefPct: 2.00f, BlockChancePct: 0.70f)),
                new SkillLevel(SpCost: 28000, Passive: new PassiveEffect(RequiresShield: true, RequiredArmor: ArmorWeights.Heavy, ShieldDefPct: 2.50f, BlockChancePct: 0.85f, DefencePct: 0.10f, BowResist: 0.16f)),   // his `tank 3rd.csv` row at 40 — 28k SP, block rate 85%, bow 16%
                new SkillLevel(SpCost: 74000, Passive: new PassiveEffect(RequiresShield: true, RequiredArmor: ArmorWeights.Heavy, ShieldDefPct: 3.00f, BlockChancePct: 1.00f, DefencePct: 0.10f, BowResist: 0.24f)),
            }),

        // Tank Anti-Magic — passive flat magic defence (5 levels @20/24/28/32/36).
        new(TankAntiMagic, "Tank Anti-Magic", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Increases your magic defence.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700,  Passive: new PassiveEffect(MagicDefence: 25)),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MagicDefence: 30)),
                new SkillLevel(SpCost: 6000,  Passive: new PassiveEffect(MagicDefence: 35)),
                new SkillLevel(SpCost: 11000, Passive: new PassiveEffect(MagicDefence: 40)),
                new SkillLevel(SpCost: 20000, Passive: new PassiveEffect(MagicDefence: 45)),
            }.Concat(TankAntiMagicThirdRungs()).ToArray()),

        // Defensive Wall — the tank's panic button: enormous P.Def & M.Def (flat + ×2), high
        // cancel resistance, but move speed halved, for 30s (long reuse). All channels are
        // ordinary buff magnitudes (BuffDef/BuffMagicDef accept flat AND percent).
        // ⚠ 30s, not 60: he corrected `tank 2nd.csv` during playtest-20 ("Tanks Ultimate is 30s
        // not 60"). 900s reuse for 30s of near-immunity is the intended ratio.
        new(DefensiveWall, "Defensive Wall", BaseClass.Fighter,
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffCancelResist | SkillEffect.BuffMoveSpeed,
            MpCost: 20, CastTicks: 5, CooldownTicks: 9000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "defensive_wall", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            // ⚠ THE TWO `x2` PERCENT TERMS ARE GONE (his tank pass, 2026-09-02). His 2nd-class row now
            // reads flat only — *"increase p.def +1800; m.def +1600"* — where it used to carry `p.def
            // x2; mdef x2` on top. A doubling over a four-figure flat was the largest defensive number
            // in the game; he removed it deliberately. Do not put it back.
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 1800, ModifierMode.Flat),
                new(SkillEffect.BuffMagicDef, 1600, ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMoveSpeed, -0.50f, ModifierMode.Percent),
            },
            Description: "Raise an impregnable guard for 30s: +1800 P.Def, +1600 M.Def and high "
                       + "cancel resistance, but your movement is halved.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 20, SpCost: 3400),
            }.Concat(TankDefensiveWallThirdRungs()).ToArray()),

        // Shield Shock — contested STUN for 9s (physical, ATK-vs-CON; bosses immune).
        // ⚠ RENAMED AND RETUNED IN HIS TANK PASS: it was "Shield Stun", a single level-28 skill on a
        // 10-second reuse. It is now a nineteen-rung ladder from 24, the reuse is THREE seconds, and
        // it lands at ×0.7 — a 9-second stun every 3 seconds would be a perma-lock at ×1, and the
        // landing multiplier is what pays for the cadence.
        new(TankShieldStun, "Shield Shock", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 15, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 0,
            DurationTicks: 90, BuffKey: "stun", Rank: 1, DebuffLandMod: 0.7f, SharesLadderKey: true,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical, SpCost: 6400,
            Description: "Slams the target with your shield, stunning it for 9s. ATK-vs-CON; bosses immune.",
            Levels: new[]
            {
                new SkillLevel(MpCost: 15, SpCost: 6400),
                new SkillLevel(MpCost: 18, SpCost: 12000),
                new SkillLevel(MpCost: 22, SpCost: 22000),
                new SkillLevel(MpCost: 26, SpCost: 40000),
            }.Concat(TankShieldShockThirdRungs()).ToArray()),

        // Stay — contested ROOT (physical hold; target can still act).
        // ⚠ IT MOVED TIER in his tank pass: it was the 2nd class's single level-36 skill and is now
        // the 3rd's whole fifteen-rung ladder from 40. Ten seconds, not the old fifteen, and the
        // exclamation mark is gone from his name for it.
        new(TankStay, "Stay", BaseClass.Fighter, SkillEffect.Root,
            MpCost: 40, CastTicks: 20, CooldownTicks: 40, Range: 400, Power: 0,
            DurationTicks: 100, BuffKey: "root", Rank: 1, SharesLadderKey: true,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical, SpCost: 28000,
            Description: "Roots the target in place for 10s (it can still act). ATK-vs-CON; bosses immune.",
            Levels: TankStayThirdRungs()),

        // ===== Rogue 2nd-class (CSV rogue 2nd) =====

        // Sprint — short, sharp burst of move speed for 15s, on a 30s reuse (half the Dash potion's).
        //
        // G5 (playtest-18): it is now a ONE-CHILD WRAPPER in the DASH family rather than a buff of
        // its own, so the potion and the skill are one ladder and the stronger always wins. Two
        // levels, +40 and +60, and the child carries the rank because Rank cannot vary per level —
        // see the FamDash block in Skills.Common.cs for the full ordering and why.
        //
        // The old "sprint" BuffKey is gone deliberately: while it had a family to itself, drinking a
        // Dash potion under Sprint gave you BOTH move-speed buffs at once, which is exactly the
        // overlap he asked to remove.
        new(Sprint, "Sprint", BaseClass.Fighter, SkillEffect.BuffMoveSpeed,
            MpCost: 10, CastTicks: 2, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 150,
            ChildBuffs: new[] { SkillCatalog.BuffSprint1 },
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            Description: "A burst of speed: +40 move speed for 15s.",
            Levels: new SkillLevel[]
            {
                new(ChildBuffs: new[] { SkillCatalog.BuffSprint1 }, MpCost: 10, SpCost: 3400,
                    Description: "A burst of speed: +40 move speed for 15s."),
                new(ChildBuffs: new[] { SkillCatalog.BuffSprint2 }, MpCost: 16, SpCost: 42000,
                    Description: "A burst of speed: +60 move speed for 15s. Overrides every Dash potion."),
            }),

        // Evasion Boost — the ROGUE's ultimate, the mirror of the tank's Defensive Wall: 30s of
        // greatly raised evasion on a 900s reuse (CSV `rogue 2nd.csv`, added by him in
        // playtest-20). This is the burst his evasion design depends on: with the discipline's
        // stray +32 gone (see Classes.Third.cs), a rogue's resting evasion lead over an equal
        // attacker is ~10-20 points and THIS is what briefly takes it to ~40-50 — *"later all
        // rogues will have an ultimate that increases the evasion with 20-30 ... but for 30 sec"*.
        //
        // ✅ MAGIC EVASION IS BUILT (owner ruling 2026-08-11, `62e`). His CSV said "magic evasion
        // x1.1", which the game had no channel for; asked what he meant, he answered *"the magic
        // evasion should be magic fail chance like 3-4"* — so it is not an evasion roll at all, it
        // is +4 percentage POINTS on the fail chance of spells cast AT you (SkillEffect
        // .BuffMagicEvasion → Entity.MagicFailBonus → StatCalculator.MagicFailChance). At parity
        // that turns a caster's 99% success into 95%; against a caster punching UP it stacks on top
        // of a fail chance that is already climbing. 4, the top of his "3-4" — a 900s ultimate.
        //
        // ✅ SKILL EVASION IS BUILT (BL-06, his `69e` ruling): *"normaly no1 can evade a physical
        // skill … no1 evades only rogues gets a floor while in an ulitmate 25%,40%"*. The CSV's
        // "skill evasion x1.25" turned out not to be a multiplier on anything — it is the 25%, and
        // THIS buff is the only source of it in the game. A physical skill is otherwise never
        // dodged at all (the accuracy-vs-evasion roll was removed from that branch entirely).
        //
        // 🔵 The 40% rung is NOT here. It is the second number of his pair and belongs to a rung
        // this skill does not have — the CSV authors Evasion Boost as a single level, and adding
        // one would re-spec his data (BL-02: the 40+ kits are owed). Same for *"76lvl the physical
        // phantom gets a 90% for 15s"*, which is a 4th-class Phantom skill that does not exist yet.
        //
        // No buff FAMILY on purpose (same as Defensive Wall): an ultimate must stack on top of the
        // Agility ladder, not evict a potion or be evicted by one.
        new(EvasionBoost, "Evasion Boost", BaseClass.Fighter,
            SkillEffect.BuffEvasion | SkillEffect.BuffCancelResist | SkillEffect.BuffMagicEvasion,
            MpCost: 20, CastTicks: 5, CooldownTicks: 9000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "evasion_boost", Rank: 1, CountsTowardBuffLimit: false,
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            SkillEvadeChance: 0.25f,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffEvasion, 20, ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMagicEvasion, 4, ModifierMode.Flat),
            },
            Description: "Slip every blow for 30s: +20 Evasion, a 25% chance to dodge physical "
                       + "SKILLS outright, spells cast at you are 4% more likely to fail, and your "
                       + "buffs strongly resist being cancelled."),

        // Bow Expertise — long self-buff: +8% bow attack speed (requires a bow) for 20 min.
        new(BowExpertise, "Bow Expertise", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 25, CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "bow_expertise", Rank: 1,  
            Category: SkillCategory.Buff, PhysicalCast: true, TargetMode: TargetMode.SelfOnly, SpCost: 22000,
            RequiredWeapon: WeaponType.Bow,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtkSpeed, 0.08f) },
            Description: "Steadies your aim: +8% attack speed while wielding a bow, for 20 minutes."),

        new(PowerStrike, "Brutal Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 30,
            Category: SkillCategory.Physical,
            Description: "A forceful melee blow. Bonus accuracy, but can still miss."),

        // Cleaving Strike — first "[Double]" skill (P1 primitive demo). A big single-target
        // slash that can deal ×2 damage on a chance from the higher of AGI/ATK (cap 30%).
        new(CleavingStrike, "Cleaving Strike", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 20, CastTicks: 5, CooldownTicks: 60, Range: 0, Power: 70,
            Category: SkillCategory.Physical, CanDouble: true,
            Description: "A heavy slash (power 70) that can strike for DOUBLE damage [Double]."),

        // Shield Bash — contested STUN (P1 primitive demo): cannot move/cast/attack for 3s.
        // Lands on ATK-vs-CON (stun is always physical); bosses immune. Numbers placeholder.
        new(ShieldBash, "Shield Bash", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 20, CastTicks: 5, CooldownTicks: 150, Range: 0, Power: 0,
            DurationTicks: 30, BuffKey: "stun", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Description: "Bash the target, stunning it for 3s (cannot move or act). ATK-vs-CON; bosses immune."),

        // Terrifying Roar — contested FEAR (P1 primitive demo): cannot cast/attack for 5s
        // (can still move). Warriors apply physical fear; lands on ATK-vs-CON; bosses immune.
        new(TerrifyingRoar, "Terrifying Roar", BaseClass.Fighter, SkillEffect.Fear,
            MpCost: 25, CastTicks: 5, CooldownTicks: 200, Range: 0, Power: 0,
            DurationTicks: 50, BuffKey: "fear", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Description: "A fearsome roar — the target panics for 5s: it cannot act, and bolts at a run wherever its feet take it. ATK-vs-CON; bosses immune."),

        // Hamstring — contested PHYSICAL Slow (the physical counterpart to the mage's Frost
        // Bind): ATK-vs-CON, −60% move speed for 8s. Shows slow can be physical OR magical.
        new(Hamstring, "Hamstring", BaseClass.Fighter, SkillEffect.Slow,
            MpCost: 18, CastTicks: 5, CooldownTicks: 80, Range: 0, Power: 0,
            DurationTicks: 80, BuffKey: "slow", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.60f) },
            Description: "A crippling cut — −60% move speed for 8s. Lands on an ATK-vs-CON contest."),

        // War Focus — standard 20-min self-buff (per the owner's example): +15% attack speed
        // and +25% PvP physical-skill / basic damage. Demonstrates the split context×source
        // damage matrix (the PvP-damage parts are latent until PvP exists; AS is live).
        new(WarFocus, "War Focus", BaseClass.Fighter,
            SkillEffect.BuffAtkSpeed | SkillEffect.BuffPvpSkillDamage | SkillEffect.BuffPvpBasicDamage,
            MpCost: 20, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "war_focus", Rank: 1,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtkSpeed, 0.15f),
                new(SkillEffect.BuffPvpSkillDamage, 0.25f),
                new(SkillEffect.BuffPvpBasicDamage, 0.25f),
            },
            Description: "A 20-min focus: +15% attack speed and +25% PvP physical-skill & basic damage."),

        // Rupture — applies BLEED (physical DoT): stacks up to 10 (reapply refreshes 30s),
        // ticks DotPower×stacks/sec, and slows the target 15% (bleed's secondary). Lands on
        // AGI-vs-CON. The Venomweaver's stack builder; pair with Detonate Wounds.
        new(Rupture, "Rupture", BaseClass.Fighter, SkillEffect.Bleed | SkillEffect.Slow,
            MpCost: 12, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 5,
            DurationTicks: 300, BuffKey: "bleed", Rank: 1, DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_bleed", MaxStacks: 10,   // per-skill counter (share id to pool stacks)
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Slow, 0.15f) },
            Description: "Opens a bleeding wound — a flat physical DoT (+15% slow) plus a stack "
                       + "that builds toward a burst. Lands on a AGI-vs-CON contest."),

        // Detonate Wounds — BURST: consumes THIS line's bleed stacks (venom_bleed), multiplying
        // damage by the stack count (×10 at full), and can [Double]. Leaves the bleed DoT.
        new(DetonateWounds, "Detonate Wounds", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 25, CastTicks: 5, CooldownTicks: 60, Range: 0, Power: 12,
            Category: SkillCategory.Physical, CanDouble: true, ConsumeStackKey: "venom_bleed",
            Description: "Detonates the target's bleed stacks for damage ×(stacks) [Double], "
                       + "consuming the stacks (the bleed itself remains)."),

        // Toxic Sting — POISON (magical DoT, ATK-vs-WIT): per-tick damage + slows the target's
        // attack & cast speed 15% (poison's secondary). Stacks; Toxic Burst spends them.
        new(ToxicSting, "Toxic Sting", BaseClass.Fighter,
            SkillEffect.Poison | SkillEffect.DebuffAtkSpeed | SkillEffect.DebuffCastSpeed,
            MpCost: 12, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 5,
            DurationTicks: 300, BuffKey: "poison", Rank: 1, DebuffSchool: DebuffSchool.Magical,
            StackKey: "venom_poison", MaxStacks: 10,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtkSpeed, 0.15f), new(SkillEffect.DebuffCastSpeed, 0.15f),
            },
            Description: "Poisons the target — a magic DoT that also slows its attack & cast speed "
                       + "15%. Lands on ATK-vs-WIT; builds stacks for Toxic Burst."),

        new(ToxicBurst, "Toxic Burst", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 25, CastTicks: 5, CooldownTicks: 60, Range: 0, Power: 12,
            Category: SkillCategory.Physical, CanDouble: true, ConsumeStackKey: "venom_poison",
            Description: "Detonates the target's poison stacks for damage ×(stacks) [Double]."),

        // Envenom — VENOM (physical DoT, AGI-vs-CON): per-tick damage + lowers the target's
        // attack 15% and defence 15% (venom's secondary). Stacks; Venom Burst spends them.
        new(Envenom, "Envenom", BaseClass.Fighter,
            SkillEffect.Venom | SkillEffect.DebuffAtk | SkillEffect.DebuffDef,
            MpCost: 12, CastTicks: 5, CooldownTicks: 30, Range: 0, Power: 5,
            DurationTicks: 300, BuffKey: "venom", Rank: 1, DebuffSchool: DebuffSchool.Physical,
            StackKey: "venom_venom", MaxStacks: 10,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.DebuffAtk, 0.15f), new(SkillEffect.DebuffDef, 0.15f),
            },
            Description: "Envenoms the target — a physical DoT that also lowers its attack & "
                       + "defence 15%. Lands on AGI-vs-CON; builds stacks for Venom Burst."),

        new(VenomBurst, "Venom Burst", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 25, CastTicks: 5, CooldownTicks: 60, Range: 0, Power: 12,
            Category: SkillCategory.Physical, CanDouble: true, ConsumeStackKey: "venom_venom",
            Description: "Detonates the target's venom stacks for damage ×(stacks) [Double]."),

        // Aegis — self ABSORB SHIELD: soaks 8% of max HP for 15s (the damage-absorb primitive).
        new(Aegis, "Aegis", BaseClass.Fighter, SkillEffect.Shield,
            MpCost: 20, CastTicks: 0, CooldownTicks: 150, Range: 0, Power: 0,
            DurationTicks: 150, BuffKey: "aegis", Rank: 1, CountsTowardBuffLimit: false, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Shield, 0.08f) },
            Description: "Raises a shield that absorbs 8% of your max HP for 15s before HP is hit."),

        // Last Stand — LETHAL SAVE: the next fatal blow within 10s is survived, reviving you to
        // 50% of max HP (consumes the buff). Long cooldown.
        new(LastStand, "Last Stand", BaseClass.Fighter, SkillEffect.LethalSave,
            MpCost: 30, CastTicks: 0, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: "last_stand", Rank: 1, CountsTowardBuffLimit: false, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.LethalSave, 0.50f) },
            Description: "For 10s, the next blow that would kill you instead leaves you at 50% HP."),

        // Indomitable — tank ULTIMATE: +80% cancel resist for 30s, so the tank's buffs shrug
        // off enemy dispels. (Cancel resist is rolled per-buff in Dispel.)
        new(Indomitable, "Indomitable", BaseClass.Fighter, SkillEffect.BuffCancelResist,
            MpCost: 40, CastTicks: 0, CooldownTicks: 1200, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "indomitable", Rank: 1, CountsTowardBuffLimit: false, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCancelResist, 0.80f) },
            Description: "For 30s your buffs have an 80% chance to resist being cancelled/dispelled."),

        // Taunt — forces a monster's aggro onto you. Two separate guarantees, and they are
        // not the same thing (BL-71):
        //   • it lands you at the TOP of the mob's threat table right now, and holds it there for
        //     DurationTicks no matter what anyone else does — the taunt's promise;
        //   • it then adds TauntPower on top, which is what decides whether you still have the mob
        //     once that window closes. Threat is damage, so the number reads literally: at level 5
        //     another player must out-damage the tank by 5,100 to pull it off him.
        //
        // ⚠ THE LADDER IS THE OWNER'S, from `tank 2nd.csv` (2026-08-17). It replaced the BL-71 ladder
        // that was derived here from his playtest-22 endpoints ("1000-2000 at L1" → "20-30k"), which
        // ran 1500/2000/2800/3800/5100 across 20/24/28/32/36. His is FOUR rungs, starts at 24, and is
        // both higher at the bottom and much flatter: 4500 / 5000 / 5500 / 6000, a straight +500 step.
        // Do not "restore" the geometric curve — a taunt that already beats a 2nd-class nuke at rung 1
        // is the point; the tank is meant to hold aggro from the moment he has the skill at all.
        // The 40+ continuation is still not authored here: it waits on his 3rd/4th CSVs (BL-02).
        //
        // 🔑 THE DISPLAY NAME IS "Taunt" — his name, ruled 2026-08-19 when `tank 2nd.csv` was aligned
        // to the game and the sheet had to be told the game called it something else. ⚠ The skill ID
        // stays `provoke`: ids are append-only and are what a saved skill bar, a hotkey and every
        // persisted character reference by. Renaming the id would silently empty bars on next login.
        // So in code/comments it is still Provoke; on screen and in the CSV it is Taunt.
        // ⚠ A display-name change needs a NEW APK — the client builds its Learn tab from the compiled
        // ClassSkills, not from a server push.
        // ⚠ RETUNED THROUGHOUT IN HIS TANK PASS (2026-09-02): the lock is 3s → **1.5s**, the reach
        // 600 → **400** at the 2nd class, and the MP cost is **0** at every rung. A taunt that costs
        // mana is a taunt a tank stops spamming, which is the opposite of the threat economy
        // `BL-123` settled — the whole design is that he has to keep earning the top of the table.
        // ⚠ HUMAN AND DEMON ONLY from now on: the Elf's is `charm`, which REPLACES it.
        new(Provoke, "Taunt", BaseClass.Fighter, SkillEffect.Taunt,
            MpCost: 0, CastTicks: 0, CooldownTicks: 60, Range: 400, Power: 0,
            DurationTicks: 15,   // the hard-commit window: 1.5s locked onto the taunter
            // `BL-132` — PhysicalCast because his TYPE cell reads `physical active`: a taunt is a roar
            // and a shield-bang, not a spell, so what paces it is attack speed.
            Category: SkillCategory.Debuff, PhysicalCast: true, TauntPower: 4500,
            Levels: new SkillLevel[]
            {
                // SP is his too, and it is the tank's standard 24/28/32/36 price line (Smash's).
                new(MpCost: 0, SpCost: 6400,  TauntPower: 4500,
                    Description: "Locks a monster onto you for 1.5s and adds 4,500 to your aggro on it. It does not put you at the top for free — hold it by keeping the taunt up."),
                new(MpCost: 0, SpCost: 12000, TauntPower: 5000,
                    Description: "Locks a monster onto you for 1.5s and adds 5,000 to your aggro on it. It does not put you at the top for free — hold it by keeping the taunt up."),
                new(MpCost: 0, SpCost: 22000, TauntPower: 5500,
                    Description: "Locks a monster onto you for 1.5s and adds 5,500 to your aggro on it. It does not put you at the top for free — hold it by keeping the taunt up."),
                new(MpCost: 0, SpCost: 40000, TauntPower: 6000,
                    Description: "Locks a monster onto you for 1.5s and adds 6,000 to your aggro on it. It does not put you at the top for free — hold it by keeping the taunt up."),
            }.Concat(TankTauntThirdRungs()).ToArray(),
            Description: "Locks a monster onto you briefly and adds to your aggro on it — it does not put you at the top for free."),

        // Lure — the ROGUE's taunt, and the tactic mob clans exist to make possible (BL-70). His
        // picture: a rogue crossing an elite field, pulling the one creature the party wants and
        // walking it back to safety while the rest of the settlement never learns it happened.
        //
        // Three things make that work, and all three are deliberate:
        //   • it does NO DAMAGE, and damage is the only thing that raises a clan (MobCatalog.Clan) —
        //     so a lure takes exactly one mob out of a camp;
        //   • it is MOB-ONLY. A taunt aimed at a person means nothing, so it says no rather than
        //     fizzling;
        //   • its LADDER IS REACH — 200 / 400 / 600, his numbers. How far away you can start a pull
        //     IS the skill, which is why this is the one place SkillLevel.Range earns its keep.
        //     Level 3 out-ranges a mob's own 400 aggro, so its holder can pull without ever stepping
        //     into the camp's notice.
        //
        // 🔴 WHERE IT IS LEARNED MOVED on 2026-08-19: it was the 2nd-class rogue's at 20/28/36 and is
        // now the melee/DUAL 3rd's, at 40, level 1 only — *"No lure for lvl 29 and below .. It's a
        // skill that need the prawl effect."* Levels 2-3 are therefore UNREACHABLE until he authors
        // their rungs in `dual 3rd.csv`. That is deliberate; see ClassSkillTables.Third.RegisterHideKit.
        //
        // Power 500 is his figure, and it is deliberately far below the tank's Provoke: a lure is
        // how you START a fight, not how you keep a mob off the party.
        new(Lure, "Lure", BaseClass.Fighter, SkillEffect.Taunt,
            MpCost: 12, CastTicks: 0, CooldownTicks: 100, Range: 200, Power: 0,
            DurationTicks: 30, Category: SkillCategory.Debuff,
            TauntPower: 500, MobTargetOnly: true,
            Levels: new SkillLevel[]
            {
                new(MpCost: 12, SpCost: 3400,  TauntPower: 500, Range: 200f,
                    Description: "Pulls ONE monster onto you from 200 range. No damage, so its clan never answers."),
                new(MpCost: 16, SpCost: 12000, TauntPower: 500, Range: 400f,
                    Description: "Pulls ONE monster onto you from 400 range. No damage, so its clan never answers."),
                new(MpCost: 20, SpCost: 40000, TauntPower: 500, Range: 600f,
                    Description: "Pulls ONE monster onto you from 600 range — beyond a monster's own aggro range. No damage, so its clan never answers."),
            },
            Description: "Pulls a single monster onto you without hurting it, so its clan has nothing to answer."),

        // Shadowstep — BLINK behind the target, then strike ([Double]). Rogue gap-closer.
        new(Shadowstep, "Shadowstep", BaseClass.Fighter, SkillEffect.PhysicalDamage | SkillEffect.Blink,
            MpCost: 22, CastTicks: 0, CooldownTicks: 80, Range: 700, Power: 50,
            Category: SkillCategory.Physical, CanDouble: true,
            Description: "Teleport behind the target and strike for heavy damage [Double]."),

        // Repelling Shot — ranged hit + KNOCKBACK (shoves the target back ~200). Trapper tool.
        new(RepellingShot, "Repelling Shot", BaseClass.Fighter,
            SkillEffect.PhysicalDamage | SkillEffect.Knockback,
            MpCost: 18, CastTicks: 5, CooldownTicks: 60, Range: 600, Power: 40,
            Category: SkillCategory.Physical, KnockbackRange: 200f,
            Description: "A forceful shot that damages and knocks the target back."),

        // Vanish — HIDE (BL-69, kind 1): withheld from the world snapshot itself, not merely unseen by
        //          mob AI. The melee rogue's opener setup.
        //
        // ⚠ 30s duration / 2 min cooldown are HIS numbers (playtest 23): *"cool down 2 min, duration -
        // 30s."* They replace the 20s/30s it shipped with — a 30s cooldown on a full hide made the
        // counter (Signal Flare's 30s no-hide stamp) meaningless, because the stamp expired at the same
        // moment the skill came back. At 2 min the counter now buys a real window.
        new(Vanish, "Vanish", BaseClass.Fighter, SkillEffect.None,
            MpCost: 30, CastTicks: 0, CooldownTicks: 1200, Range: 0, Power: 0,
            DurationTicks: 300, Category: SkillCategory.Physical,
            TargetMode: TargetMode.SelfOnly, GrantsHide: true,
            Description: "Vanish completely for 30s — nobody can see or target you, and every monster " +
                         "loses you. Anything but walking ends it."),

        // Prowl — STEALTH (BL-69, kind 2). A stance, not an opener, and the difference is the whole
        // skill: aggressive monsters that have not already noticed you never start, while anything
        // already chasing keeps chasing and hitting. Players see you normally. It does not break when
        // you act — only when you switch it off or run dry. His purpose for it, verbatim:
        // *"toggle-on makes the rogues farm in peacefull zones."*
        //
        // The price is 1 MP/s forever rather than a cast cost, so it is a decision about how you
        // travel rather than a button pressed before each pull.
        new(Prowl, "Prowl", BaseClass.Fighter, SkillEffect.None,
            MpCost: 20, CastTicks: 0, CooldownTicks: 20, Range: 0, Power: 0,
            BuffKey: "prowl", Rank: 1, Category: SkillCategory.Physical,
            TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            Toggle: true, GrantsMobStealth: true, MpPerSecond: 1,
            Description: "Stance: monsters that haven't already noticed you leave you alone. " +
                         "Anything already chasing you keeps chasing. Costs 1 MP per second."),

        // Signal Flare — the REVEAL, and the counter to a full hide. A non-damaging area debuff:
        // every hidden character within 300 is dragged back into view AND cannot hide again for 30s.
        // That second half is the part that matters — stripping a hide the rogue can re-cast a
        // heartbeat later is not a counter, it is an inconvenience.
        //
        // No damage on purpose: it is an ANSWER, not a nuke with a rider, and a damaging version
        // would also raise a mob clan (BL-70) every time someone swept an area for a rogue.
        new(SignalFlare, "Signal Flare", BaseClass.Fighter, SkillEffect.None,
            MpCost: 28, CastTicks: 10, CooldownTicks: 200, Range: 0, Power: 0,
            Category: SkillCategory.Physical, SpCost: 12000,
            TargetMode: TargetMode.SelfOnly, AreaRadius: 300f,
            RequiredWeapon: WeaponType.Bow,
            RevealsHidden: true, NoHideTicks: 300,
            Description: "Fires a flare: everyone hidden within 300 is revealed and cannot hide " +
                         "again for 30s. Deals no damage."),

        // Snare Trap — TRAP: drop a hidden trap; the next monster to step on it takes damage and is
        //          ROOTED (contested). The Trapper's control tool. Uses the skill's own Power + Root.
        new(SnareTrap, "Snare Trap", BaseClass.Fighter,
            SkillEffect.PhysicalDamage | SkillEffect.Root,
            MpCost: 24, CastTicks: 10, CooldownTicks: 100, Range: 0, Power: 55,
            DurationTicks: 50, Category: SkillCategory.Physical,
            TargetMode: TargetMode.SelfOnly, DebuffSchool: DebuffSchool.Physical,
            PlacesTrap: true, TrapRadius: 150f, TrapLifeTicks: 300,
            Description: "Set a trap; the first monster to trip it is damaged and rooted in place."),

        new(WarCry, "War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "might", Rank: 1, CountsTowardBuffLimit: false,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.20f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +20% Attack Power for 30s."),

        new(GreaterWarCry, "Greater War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 18, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "might", Rank: 2, CountsTowardBuffLimit: false,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.30f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +30% Attack Power for 30s."),

        new(BattleFury, "Battle Fury", BaseClass.Fighter,
            SkillEffect.BuffAtk | SkillEffect.BuffMoveSpeed,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "battle_fury", Rank: 1, CountsTowardBuffLimit: false,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtk, 0.20f),
                new(SkillEffect.BuffMoveSpeed, 0.15f),
            },
            Category: SkillCategory.Buff,
            Description: "+20% Attack and +15% Move Speed for 30s."),

        new(Fortify, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
            DurationTicks: 250, BuffKey: "fortify", Rank: 1, CountsTowardBuffLimit: false,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 0.50f) },
            Category: SkillCategory.Buff,
            Description: "Tank stance: +50% Defence for 25s."),

        new(ShieldMastery, "Shield Mastery", BaseClass.Fighter,
            SkillEffect.BuffBlockChance | SkillEffect.BuffShieldDef,
            MpCost: 25, CastTicks: 10, CooldownTicks: 10, Range: 0, Power: 0,
            DurationTicks: 6000, BuffKey: "shield_mastery", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                // +30% block chance, +50% shield defence (only with a shield).
                new(SkillEffect.BuffBlockChance, 0.30f, ModifierMode.Percent),
                new(SkillEffect.BuffShieldDef, 0.50f, ModifierMode.Percent),
            },
            Category: SkillCategory.Buff, SpCost: 2000, TargetMode: TargetMode.SelfOnly,
            Description: "Tank passive: greatly improves your shield's block " +
                         "chance and defence (only while a shield is equipped)."),

        new(MightyBlow, "Mighty Blow", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 18, CastTicks: 7, CooldownTicks: 60, Range: 0, Power: 85,
            Category: SkillCategory.Physical, SureHit: true,
            Description: "A devastating two-hand strike for heavy damage — never misses "
                       + "(ignores evasion). The warrior's answer to dodgy targets."),

        new(TwinSlash, "Twin Slash", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 12, CastTicks: 3, CooldownTicks: 25, Range: 0, Power: 55,
            Category: SkillCategory.Physical,
            Description: "Two rapid dagger slashes. Short cast and cooldown."),

        // ⚠ ORPHANED ON PURPOSE since 2026-08-07 (playtest-19 M7). Heavy Draw has NO learn
        // assignment anywhere: the rogue's @24 grant and the three ranged-discipline renames
        // ("Piercing Shot" / "Snare Shot" / "Rending Shot") were all removed on his ruling —
        // *"remove it - remove it from after 40lvl as well"*. The DEFINITION stays: he ruled on the
        // GRANTS, not the skill, and the level-40 bow CSV is the natural place for it to come back.
        // Do not "clean this up" as dead code without asking him.
        new(PowerShot, "Heavy Draw", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 16, CastTicks: 8, CooldownTicks: 40, Range: 900, Power: 70,
            Category: SkillCategory.Physical,
            Description: "A long-range aimed shot dealing heavy damage."),

        // The dedicated interrupt: INSTANT (CastTicks 0), tiny damage, but
        // overwhelming InterruptPower so it ALWAYS breaks an enemy cast.
        new(Disrupt, "Disrupt", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 0, CooldownTicks: 80, Range: 0, Power: 5,
            Category: SkillCategory.Physical, InterruptPower: 99999, SureHit: true,
            Description: "Instant strike that never misses and always interrupts an enemy's cast."),
    };
}
