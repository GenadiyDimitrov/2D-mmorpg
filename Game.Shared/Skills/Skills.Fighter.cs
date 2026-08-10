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
    public const string Shadowstep = "shadowstep";            // blink behind target + hit
    public const string RepellingShot = "repelling_shot";     // ranged hit + knockback
    public const string Vanish = "vanish";                    // Phantom: stealth (invisible to mobs)
    public const string SnareTrap = "snare_trap";             // Trapper: place a rooting damage trap

    // --- Base fighter CORE actives (CSV fighter 01-15, continuing into 2nd-class) ---
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

    // --- Warrior 2nd-class (CSV warrior 20-35) ---
    public const string BodyMastery = "body_mastery";              // +max HP + HP regen
    public const string BattleRegeneration = "battle_regeneration";// self-heal 10% max HP
    public const string BattlePresence = "battle_presence";        // HP<60% stance: +p.Atk
    public const string BattleDefence = "battle_defence";          // HP<60% stance: +p.Def

    // --- Tank 2nd-class (CSV tank 20-35) ---
    public const string TankShieldMastery = "tank_shield_mastery"; // passive: +shield def/rate + bow resist
    public const string TankAntiMagic = "tank_anti_magic";         // passive: +magic def
    public const string DefensiveWall = "defensive_wall";          // huge def buff (self, -move)
    public const string TankShieldStun = "tank_shield_stun";       // stun 9s
    public const string TankStay = "tank_stay";                    // root/hold 15s

    // --- Rogue 2nd-class (CSV rogue 20-35) ---
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
                new SkillLevel(Power: 35,  MpCost: 10, InitialMpCost: 10, SpCost: 160,   Description: "Strike — power 35."),
                new SkillLevel(Power: 65,  MpCost: 13, InitialMpCost: 13, SpCost: 910,   Description: "Strike — power 65."),
                new SkillLevel(Power: 84,  MpCost: 17, InitialMpCost: 17, SpCost: 910,   Description: "Strike — power 84."),
            }),

        // Stab — dagger (dual) BLOW: full power only on a critical/double, else a soft 10%.
        new(Stab, "Stab", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 10, CastTicks: 10, CooldownTicks: 30, Range: 40, Power: 88,
            Category: SkillCategory.Physical, CanDouble: true, BlowOnCrit: true, CritRateMod: 2.0f,
            RequiredWeapon: WeaponType.Dual,
            Description: "A dagger blow (duals). Lands for FULL power only on a critical or double — a soft 10% otherwise.",
            Levels: new[]
            {
                new SkillLevel(Power: 88,  MpCost: 10, InitialMpCost: 10, SpCost: 160,   Description: "Stab — blow power 88 (10% without a crit)."),
                new SkillLevel(Power: 137, MpCost: 11, InitialMpCost: 11, SpCost: 910,   Description: "Stab — blow power 137."),
                new SkillLevel(Power: 210, MpCost: 15, InitialMpCost: 15, SpCost: 910,   Description: "Stab — blow power 210."),
            }),

        // Shot — bow ranged attack; can [Double]. Base reach 350 (rogue extends it later).
        new(Shot, "Shot", BaseClass.Fighter, SkillEffect.PhysicalDamage,
            MpCost: 20, CastTicks: 30, CooldownTicks: 60, Range: 350, Power: 78,
            Category: SkillCategory.Physical, CanDouble: true,
            RequiredWeapon: WeaponType.Bow,
            Description: "A bow shot dealing heavy ranged damage (fighter reach 350). Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 78,  MpCost: 20, InitialMpCost: 20, SpCost: 160,   Description: "Shot — power 78."),
                new SkillLevel(Power: 122, MpCost: 25, InitialMpCost: 25, SpCost: 910,   Description: "Shot — power 122."),
                new SkillLevel(Power: 187, MpCost: 34, InitialMpCost: 34, SpCost: 910,   Description: "Shot — power 187."),
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
            RequiredWeapon: WeaponType.TwoHandedSword | WeaponType.TwoHandedBlunt,
            Replaces: new[] { Strike, Stab, Shot },
            Description: "A crushing sword/blunt blow — the warrior's Strike upgrade. Can strike for DOUBLE.",
            Levels: new[]
            {
                new SkillLevel(Power: 105, MpCost: 20, InitialMpCost: 20, SpCost: 3400,  Description: "Smash — power 105."),
                new SkillLevel(Power: 143, MpCost: 23, InitialMpCost: 23, SpCost: 6400,  Description: "Smash — power 143."),
                new SkillLevel(Power: 191, MpCost: 25, InitialMpCost: 25, SpCost: 12000, Description: "Smash — power 191."),
                new SkillLevel(Power: 251, MpCost: 30, InitialMpCost: 30, SpCost: 22000, Description: "Smash — power 251."),
                new SkillLevel(Power: 326, MpCost: 35, InitialMpCost: 35, SpCost: 40000, Description: "Smash — power 326."),
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
                new SkillLevel(Power: 314, MpCost: 18, InitialMpCost: 18, SpCost: 1700,  Description: "Piercing Stab — blow power 314."),
                new SkillLevel(Power: 427, MpCost: 21, InitialMpCost: 21, SpCost: 3200,  Description: "Piercing Stab — blow power 427."),
                new SkillLevel(Power: 571, MpCost: 24, InitialMpCost: 24, SpCost: 6000,  Description: "Piercing Stab — blow power 571."),
                new SkillLevel(Power: 752, MpCost: 58, InitialMpCost: 58, SpCost: 11000, Description: "Piercing Stab — blow power 752."),
                new SkillLevel(Power: 977, MpCost: 30, InitialMpCost: 30, SpCost: 20000, Description: "Piercing Stab — blow power 977."),
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
                new SkillLevel(Power: 279, MpCost: 40, InitialMpCost: 40, SpCost: 1700,  Description: "Precise Shot — power 279."),
                new SkillLevel(Power: 379, MpCost: 45, InitialMpCost: 45, SpCost: 3200,  Description: "Precise Shot — power 379."),
                new SkillLevel(Power: 507, MpCost: 53, InitialMpCost: 53, SpCost: 6000,  Description: "Precise Shot — power 507."),
                new SkillLevel(Power: 669, MpCost: 34, InitialMpCost: 34, SpCost: 11000, Description: "Precise Shot — power 669."),
                new SkillLevel(Power: 868, MpCost: 67, InitialMpCost: 67, SpCost: 20000, Description: "Precise Shot — power 868."),
            }),

        // ===== Warrior 2nd-class (CSV warrior 20-35) =====

        // Body Mastery — flat max HP + HP-regen multiplier (passive, 5 levels @20/24/28/32/36).
        new(BodyMastery, "Body Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Hardens your body — more maximum HP and faster HP regeneration.",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700,  Passive: new PassiveEffect(MaxHp: 60)),
                new SkillLevel(SpCost: 3200,  Passive: new PassiveEffect(MaxHp: 60,  HpRegenPct: 0.10f)),
                new SkillLevel(SpCost: 6000,  Passive: new PassiveEffect(MaxHp: 100, HpRegenPct: 0.10f)),
                new SkillLevel(SpCost: 11000, Passive: new PassiveEffect(MaxHp: 100, HpRegenPct: 0.60f)),
                new SkillLevel(SpCost: 20000, Passive: new PassiveEffect(MaxHp: 150, HpRegenPct: 0.60f)),
            }),

        // Battle Regeneration — instant self-heal for 10% of max HP (short cast, 90s cooldown).
        new(BattleRegeneration, "Battle Regeneration", BaseClass.Fighter, SkillEffect.Heal,
            MpCost: 25, CastTicks: 5, CooldownTicks: 900, Range: 0, Power: 0,
            Category: SkillCategory.Heal, TargetMode: TargetMode.SelfOnly, SpCost: 6000,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Heal, 0.10f, ModifierMode.Percent) },
            Description: "Restores 10% of your maximum HP instantly (90s reuse)."),

        // Battle Presence — LOW-HP offensive stance (usable only at ≤60% HP): +35% P.Atk and
        // +2 accuracy for 90s. Requires a sword/blunt; shares the "battle_stance" key with
        // Battle Defence, so activating one ends the other (mutually exclusive).
        new(BattlePresence, "Battle Presence", BaseClass.Fighter,
            SkillEffect.BuffPhysAtk | SkillEffect.BuffAccuracy,
            MpCost: 20, CastTicks: 5, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 900, BuffKey: "battle_stance", Rank: 1,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 11000,
            RequireHpBelowFraction: 0.60f, RequiredWeapon: WeaponType.TwoHandedSword | WeaponType.TwoHandedBlunt,
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
            DurationTicks: 900, BuffKey: "battle_stance", Rank: 1,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 20000,
            RequireHpBelowFraction: 0.60f,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffDef, 1.0f, ModifierMode.Percent) },
            Description: "A desperate defence: DOUBLES your P.Def for 90s. Usable only at ≤60% HP. "
                       + "Cannot be combined with Battle Presence."),

        // ===== Tank 2nd-class (CSV tank 20-35) =====

        // Shield Mastery — PASSIVE (4 levels @20/24/28/32): scales the equipped shield's
        // block chance and defence, and adds bow resistance. Inert without a shield.
        new(TankShieldMastery, "Shield Mastery", BaseClass.Fighter, SkillEffect.None,
            MpCost: 0, CastTicks: 0, CooldownTicks: 0, Range: 0, Power: 0,
            Category: SkillCategory.Passive,
            Description: "Passive. Greatly improves your shield's block chance and defence, and "
                       + "reduces damage from bows (only while a shield is equipped).",
            Levels: new[]
            {
                new SkillLevel(SpCost: 1700, Passive: new PassiveEffect(ShieldDefPct: 0.30f, BlockChancePct: 0.50f)),
                new SkillLevel(SpCost: 3200, Passive: new PassiveEffect(ShieldDefPct: 0.30f, BlockChancePct: 0.50f, BowResist: 0.16f)),
                new SkillLevel(SpCost: 6000, Passive: new PassiveEffect(ShieldDefPct: 0.40f, BlockChancePct: 0.70f, BowResist: 0.16f)),
                new SkillLevel(SpCost: 11000, Passive: new PassiveEffect(ShieldDefPct: 0.40f, BlockChancePct: 0.70f, BowResist: 0.24f)),
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
            }),

        // Defensive Wall — the tank's panic button: enormous P.Def & M.Def (flat + ×2), high
        // cancel resistance, but move speed halved, for 30s (long reuse). All channels are
        // ordinary buff magnitudes (BuffDef/BuffMagicDef accept flat AND percent).
        // ⚠ 30s, not 60: he corrected `tank 20-35.csv` during playtest-20 ("Tanks Ultimate is 30s
        // not 60"). 900s reuse for 30s of near-immunity is the intended ratio.
        new(DefensiveWall, "Defensive Wall", BaseClass.Fighter,
            SkillEffect.BuffDef | SkillEffect.BuffMagicDef | SkillEffect.BuffCancelResist | SkillEffect.BuffMoveSpeed,
            MpCost: 20, CastTicks: 5, CooldownTicks: 9000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "defensive_wall", Rank: 1,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffDef, 1800, ModifierMode.Flat),
                new(SkillEffect.BuffDef, 1.0f, ModifierMode.Percent),
                new(SkillEffect.BuffMagicDef, 1600, ModifierMode.Flat),
                new(SkillEffect.BuffMagicDef, 1.0f, ModifierMode.Percent),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
                new(SkillEffect.BuffMoveSpeed, -0.50f, ModifierMode.Percent),
            },
            Description: "Raise an impregnable guard for 30s: massively higher physical & magic "
                       + "defence and cancel resistance, but your movement is halved."),

        // Shield Stun — contested STUN for 9s (physical, ATK-vs-CON; bosses immune).
        new(TankShieldStun, "Shield Stun", BaseClass.Fighter, SkillEffect.Stun,
            MpCost: 30, CastTicks: 10, CooldownTicks: 100, Range: 40, Power: 0,
            DurationTicks: 90, BuffKey: "stun", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical, SpCost: 12000,
            Description: "Slams the target with your shield, stunning it for 9s. ATK-vs-CON; bosses immune."),

        // Stay! — contested ROOT for 15s (physical hold; target can still act).
        new(TankStay, "Stay!", BaseClass.Fighter, SkillEffect.Root,
            MpCost: 30, CastTicks: 5, CooldownTicks: 150, Range: 400, Power: 0,
            DurationTicks: 150, BuffKey: "root", Rank: 1,
            Category: SkillCategory.Debuff, DebuffSchool: DebuffSchool.Physical, SpCost: 40000,
            Description: "Roots the target in place for 15s (it can still act). ATK-vs-CON; bosses immune."),

        // ===== Rogue 2nd-class (CSV rogue 20-35) =====

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
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            Description: "A burst of speed: +40 move speed for 15s.",
            Levels: new SkillLevel[]
            {
                new(ChildBuffs: new[] { SkillCatalog.BuffSprint1 }, MpCost: 10, SpCost: 3400,
                    Description: "A burst of speed: +40 move speed for 15s."),
                new(ChildBuffs: new[] { SkillCatalog.BuffSprint2 }, MpCost: 16, SpCost: 42000,
                    Description: "A burst of speed: +60 move speed for 15s. Overrides every Dash potion."),
            }),

        // Evasion Boost — the ROGUE's ultimate, the mirror of the tank's Defensive Wall: 30s of
        // greatly raised evasion on a 900s reuse (CSV `rogue 20-35.csv`, added by him in
        // playtest-20). This is the burst his evasion design depends on: with the discipline's
        // stray +32 gone (see Classes.Third.cs), a rogue's resting evasion lead over an equal
        // attacker is ~10-20 points and THIS is what briefly takes it to ~40-50 — *"later all
        // rogues will have an ultimate that increases the evasion with 20-30 ... but for 30 sec"*.
        //
        // 🔴 TWO CSV CHANNELS ARE NOT BUILT: "skill evasion x1.25" and "magic evasion x1.1". The
        // game has exactly ONE evasion channel (SkillEffect.BuffEvasion, consumed by
        // StatCalculator.ResolveAvoidChance) — dodging a physical SKILL separately from a basic
        // attack, and dodging MAGIC at all, are new resolution mechanics, not new numbers. They are
        // deliberately omitted rather than approximated: folding them into the flat +20 would
        // silently make this stronger than authored. Add the channels first, then these two lines.
        //
        // No buff FAMILY on purpose (same as Defensive Wall): an ultimate must stack on top of the
        // Agility ladder, not evict a potion or be evicted by one.
        new(EvasionBoost, "Evasion Boost", BaseClass.Fighter,
            SkillEffect.BuffEvasion | SkillEffect.BuffCancelResist,
            MpCost: 20, CastTicks: 5, CooldownTicks: 9000, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "evasion_boost", Rank: 1,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 3400,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffEvasion, 20, ModifierMode.Flat),
                new(SkillEffect.BuffCancelResist, 0.80f, ModifierMode.Percent),
            },
            Description: "Slip every blow for 30s: +20 Evasion and your buffs strongly resist "
                       + "being cancelled."),

        // Bow Expertise — long self-buff: +8% bow attack speed (requires a bow) for 20 min.
        new(BowExpertise, "Bow Expertise", BaseClass.Fighter, SkillEffect.BuffAtkSpeed,
            MpCost: 25, CastTicks: 30, CooldownTicks: 20, Range: 0, Power: 0,
            DurationTicks: 12000, BuffKey: "bow_expertise", Rank: 1, InitialMpCost: 5,
            Category: SkillCategory.Buff, TargetMode: TargetMode.SelfOnly, SpCost: 22000,
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
            Description: "A fearsome roar — the target cannot cast or attack for 5s (can still move). ATK-vs-CON; bosses immune."),

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
            DurationTicks: 150, BuffKey: "aegis", Rank: 1, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.Shield, 0.08f) },
            Description: "Raises a shield that absorbs 8% of your max HP for 15s before HP is hit."),

        // Last Stand — LETHAL SAVE: the next fatal blow within 10s is survived, reviving you to
        // 50% of max HP (consumes the buff). Long cooldown.
        new(LastStand, "Last Stand", BaseClass.Fighter, SkillEffect.LethalSave,
            MpCost: 30, CastTicks: 0, CooldownTicks: 3000, Range: 0, Power: 0,
            DurationTicks: 100, BuffKey: "last_stand", Rank: 1, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.LethalSave, 0.50f) },
            Description: "For 10s, the next blow that would kill you instead leaves you at 50% HP."),

        // Indomitable — tank ULTIMATE: +80% cancel resist for 30s, so the tank's buffs shrug
        // off enemy dispels. (Cancel resist is rolled per-buff in Dispel.)
        new(Indomitable, "Indomitable", BaseClass.Fighter, SkillEffect.BuffCancelResist,
            MpCost: 40, CastTicks: 0, CooldownTicks: 1200, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "indomitable", Rank: 1, TargetMode: TargetMode.SelfOnly,
            Category: SkillCategory.Buff,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffCancelResist, 0.80f) },
            Description: "For 30s your buffs have an 80% chance to resist being cancelled/dispelled."),

        // Provoke — TAUNT: forces a monster's aggro onto you (spikes its threat above the
        // current top and locks it onto you for ~3s).
        new(Provoke, "Provoke", BaseClass.Fighter, SkillEffect.Taunt,
            MpCost: 15, CastTicks: 0, CooldownTicks: 60, Range: 600, Power: 0,
            Category: SkillCategory.Debuff,
            Description: "Forces a monster to attack you — spikes its aggro and locks onto you briefly."),

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

        // Vanish — STEALTH: become invisible to monsters for the duration (broken by any offensive
        //          action). Sheds mobs already locked on. The Phantom's opener setup.
        new(Vanish, "Vanish", BaseClass.Fighter, SkillEffect.None,
            MpCost: 30, CastTicks: 0, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 200, Category: SkillCategory.Physical,
            TargetMode: TargetMode.SelfOnly, GrantsStealth: true,
            Description: "Slip into the shadows — monsters can't see you until you act."),

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
            DurationTicks: 300, BuffKey: "might", Rank: 1,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.20f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +20% Attack Power for 30s."),

        new(GreaterWarCry, "Greater War Cry", BaseClass.Fighter, SkillEffect.BuffAtk,
            MpCost: 18, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "might", Rank: 2,
            Magnitudes: new EffectMagnitude[] { new(SkillEffect.BuffAtk, 0.30f) },
            Category: SkillCategory.Buff,
            Description: "Battle shout: +30% Attack Power for 30s."),

        new(BattleFury, "Battle Fury", BaseClass.Fighter,
            SkillEffect.BuffAtk | SkillEffect.BuffMoveSpeed,
            MpCost: 15, CastTicks: 5, CooldownTicks: 300, Range: 0, Power: 0,
            DurationTicks: 300, BuffKey: "battle_fury", Rank: 1,
            Magnitudes: new EffectMagnitude[]
            {
                new(SkillEffect.BuffAtk, 0.20f),
                new(SkillEffect.BuffMoveSpeed, 0.15f),
            },
            Category: SkillCategory.Buff,
            Description: "+20% Attack and +15% Move Speed for 30s."),

        new(Fortify, "Fortify", BaseClass.Fighter, SkillEffect.BuffDef,
            MpCost: 20, CastTicks: 5, CooldownTicks: 250, Range: 0, Power: 0,
            DurationTicks: 250, BuffKey: "fortify", Rank: 1,
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
