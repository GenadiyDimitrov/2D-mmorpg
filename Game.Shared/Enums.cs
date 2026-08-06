namespace Game.Shared;

public enum Race
{
    Human = 0,
    Elf = 1,
    Ork = 2,
    God = 99   // debug-only race (creatable in DEBUG builds; usable once made)
}

public enum BaseClass
{
    Fighter = 0,
    Mage = 1
}

public enum EntityKind
{
    Player = 0,
    Mob = 1,
    Npc = 2    // groundwork for quests: stationary, non-combat, talkable
}

public enum ChatChannel
{
    Local = 0,    // visible within ViewRange
    World = 1,    // everyone; sent with '!message'
    System = 2,   // server / admin messages (own panel on top)
    Whisper = 3   // private: /w CharName message
}

public enum CombatOutcome
{
    Hit = 0,
    Crit = 1,
    Miss = 2,    // physical attacks miss (acc vs eva)
    Death = 3,
    Heal = 4,
    Fail = 5,    // spells don't miss — they fail (level difference)
    Buff = 6,    // a buff/debuff was applied (Skill carries the name)
    Block = 7,   // physical hit was blocked by a shield (reduced damage)
    ManaHeal = 8, // MP was restored (shown as +N MP, distinct from HP heal)
    // A "[Double]" physical SKILL fired (flat ×2, our name for L2's physical skill crit). It was
    // reported as Crit until playtest-19 M8, which made a doubling Strike indistinguishable from
    // a critting one — the whole point of the [Double] naming is that you can tell them apart.
    // Appended, so an older client just falls through to the plain-Hit branch.
    Double = 9
}

/// <summary>
/// What a skill does. A [Flags] enum so ONE skill can carry several effects:
/// Effect = BuffAtk | BuffMoveSpeed | BuffCastSpeed. Each effect's amount is
/// stored separately in the skill's Magnitudes map (see EffectMagnitude), so a
/// buffer-class skill is just "list the flags, give each a number" — no new
/// enum member per combination.
/// </summary>
[Flags]
public enum SkillEffect : long
{
    None           = 0,
    PhysicalDamage = 1L << 0,
    MagicDamage    = 1L << 1,
    Heal           = 1L << 2,   // restores HP (flat power, +% of max HP via a Percent magnitude)
    BuffAtk        = 1L << 3,   // +attack to BOTH channels (pAtk & mAtk)
    BuffDef        = 1L << 4,
    BuffMoveSpeed  = 1L << 5,
    BuffAtkSpeed   = 1L << 6,   // shortens basic-attack interval
    BuffCastSpeed  = 1L << 7,   // shortens cast time
    BuffEvasion    = 1L << 8,
    BuffHp         = 1L << 9,   // Max HP: Percent of max, and/or Flat
    BuffMp         = 1L << 10,  // Max MP: Percent of max, and/or Flat
    DebuffDef      = 1L << 11,
    BuffBlockChance = 1L << 12,  // Shield Mastery: +block chance
    BuffShieldDef   = 1L << 13,  // Shield Mastery: +shield defence / +block reduction
    // ----- Healer (Lightbringer) additions, Phase 24.1 -----
    Cleanse        = 1L << 14,   // remove harmful effects from an ally
    DebuffHealRecv = 1L << 15,   // anti-heal: reduces healing the target receives
    Root           = 1L << 16,   // hold: target cannot move for the duration
    Detaunt        = 1L << 17,   // drop the caster's aggro from nearby mobs (stub until threat)
    // ----- Buffer (Warchanter) additions -----
    BuffMagicDef   = 1L << 18,   // +magic defence (applied live in EffectiveMagicDefence)
    BuffHpRegen    = 1L << 19,   // +HP regen (Percent and/or Flat, applied in Regenerate)
    BuffMpRegen    = 1L << 20,   // +MP regen (Percent and/or Flat, applied in Regenerate)
    HealOverTime   = 1L << 21,   // heals a % of max HP each second for the duration
    // ----- Healer buff/effect layer (Increment 2 primitives) -----
    BuffPhysAtk        = 1L << 22,  // +physical attack ONLY (flat and/or %)
    BuffMagAtk         = 1L << 23,  // +magic attack ONLY (flat and/or %)
    BuffAccuracy       = 1L << 24,  // +accuracy (flat)
    BuffCritRate       = 1L << 25,  // +physical crit rate (flat and/or % of current)
    BuffMagicCritRate  = 1L << 26,  // +magic crit rate
    BuffCritDamage     = 1L << 27,  // +physical crit damage multiplier
    BuffCritDmgResist  = 1L << 28,  // reduce incoming physical crit EXTRA damage (%)
    BuffCritRateResist = 1L << 29,  // reduce attacker physical crit CHANCE vs you (flat)
    BuffBowResist      = 1L << 30,  // reduce damage taken from BOW attacks (%)
    BuffMagicFailFloor = 1L << 31,  // raise the chance enemy spells fail vs you (floor)
    BuffMagicFailResist= 1L << 32,  // your own spells fail LESS (flat reduction to your fail chance)
    BuffInterruptPower = 1L << 33,  // "magic cancel": +your offensive interrupt power
    BuffInterruptResist= 1L << 34,  // "magic cancel resist": +your interrupt resistance
    BuffMeleeVamp      = 1L << 35,  // basic (melee) attacks heal you for % of damage
    BuffSpellVamp      = 1L << 36,  // damage spells heal you for % of damage
    BuffCooldown       = 1L << 37,  // spell reuse-delay reduction (%)
    RestoreMp          = 1L << 38,  // restores MP (flat power, +% of max MP via a Percent magnitude)
    // ----- Combat primitives (P1): crowd control. These LAND via the ATK-vs-CON/WIT
    // debuff contest (StatCalculator.DebuffLandChance), NOT the spell-fizzle model. -----
    Slow               = 1L << 39,  // reduces target move speed by a % (magnitude on Slow)
    Stun               = 1L << 40,  // cannot move, cast or attack for the duration
    Fear               = 1L << 41,  // cannot cast or attack (can still move) for the duration
    // Damage-OUT channels: a 2×3 matrix of context (PvE/PvP) × source (skill=physical
    // skill / magic skill / basic attack). All default off. An "all damage" effect sets
    // all six; a "PvE only" effect sets the three PvE ones; a "PvE skill" effect sets one.
    BuffPveSkillDamage  = 1L << 42,  // +% physical-skill damage vs MOBS
    BuffPveMagicDamage  = 1L << 43,  // +% magic-skill damage vs MOBS
    BuffPveBasicDamage  = 1L << 44,  // +% basic-attack damage vs MOBS
    BuffPvpSkillDamage  = 1L << 45,  // +% physical-skill damage vs PLAYERS
    BuffPvpMagicDamage  = 1L << 46,  // +% magic-skill damage vs PLAYERS
    BuffPvpBasicDamage  = 1L << 47,  // +% basic-attack damage vs PLAYERS
    // ----- Damage-over-time with stacks (Venomweaver). Land via the contest; tick each
    // second for DotPower×Stacks; a burst skill consumes the stacks. Each carries its own
    // secondary debuff as ordinary magnitudes (bleed→Slow, poison→−AS/cast, venom→−atk/def). -----
    Bleed              = 1L << 48,  // physical DoT (DEX-vs-CON); secondary: slows move speed
    Poison             = 1L << 49,  // magical DoT (ATK-vs-WIT); secondary: slows attack/cast
    Venom              = 1L << 50,  // physical DoT (DEX-vs-CON); secondary: lowers atk/def
    Cancel             = 1L << 51,  // offensive: strip POSITIVE buffs from an enemy (dispel)
    Shield             = 1L << 52,  // damage-absorb pool (flat Power + % of max HP) soaked before HP
    ManaShield         = 1L << 53,  // divert a % of damage to MP (Flat = MP per 1 damage)
    LethalSave         = 1L << 54,  // survive one fatal blow, reviving to a % of max HP (consumed)
    BuffCancelResist   = 1L << 55,  // chance each of your buffs RESISTS an enemy cancel/dispel
    Taunt              = 1L << 56,  // force a mob's aggro onto the caster (spikes threat + locks briefly)
    Blink              = 1L << 57,  // teleport the CASTER: behind the target (gap-closer) or away (BlinkRange)
    Knockback          = 1L << 58,  // shove the TARGET away from the caster by KnockbackRange
    // Stat DEBUFFS (NOT in AnyBuff, so they ride safely on an offensive debuff). Percent
    // magnitudes reduce the stat. Used by poison (−AS/cast) and venom (−atk, + DebuffDef).
    DebuffAtk          = 1L << 59,  // reduce the target's attack power (both channels)
    DebuffAtkSpeed     = 1L << 60,  // reduce the target's attack speed
    DebuffCastSpeed    = 1L << 61,  // reduce the target's cast speed
    BuffReflect        = 1L << 62,  // return a fraction of taken MELEE-basic damage to the attacker
    // 1L << 62 is the LAST free bit (63 = sign). Add further buff EFFECTS via StatMods, not new flags.

    // Convenience masks.
    AnyDamage = PhysicalDamage | MagicDamage,
    AnyBuff   = BuffAtk | BuffDef | BuffMoveSpeed | BuffAtkSpeed | BuffCastSpeed
              | BuffEvasion | BuffHp | BuffMp | BuffBlockChance | BuffShieldDef
              | BuffMagicDef | BuffHpRegen | BuffMpRegen | HealOverTime
              | BuffPhysAtk | BuffMagAtk | BuffAccuracy | BuffCritRate | BuffMagicCritRate
              | BuffCritDamage | BuffCritDmgResist | BuffCritRateResist | BuffBowResist
              | BuffMagicFailFloor | BuffMagicFailResist | BuffInterruptPower
              | BuffInterruptResist | BuffMeleeVamp | BuffSpellVamp | BuffCooldown
              | BuffPveSkillDamage | BuffPveMagicDamage | BuffPveBasicDamage
              | BuffPvpSkillDamage | BuffPvpMagicDamage | BuffPvpBasicDamage
              | Shield | ManaShield | LethalSave | BuffCancelResist | BuffReflect,
    // Harmful effects applied to an enemy (offensive; can fail; cleansable).
    AnyDebuff = DebuffDef | DebuffHealRecv | Root | Slow | Stun | Fear | Bleed | Poison | Venom
              | DebuffAtk | DebuffAtkSpeed | DebuffCastSpeed,
    // Stacking damage-over-time effects (consumed by burst skills).
    AnyDot = Bleed | Poison | Venom,
    // Crowd-control / DoT that lands via the ATK-vs-CON/WIT contest (not the fizzle model).
    // Resolved in their own ExecuteSkill branch; excluded from the legacy debuff branch.
    ContestCc = Slow | Stun | Fear | Root | Bleed | Poison | Venom,
}

/// <summary>Which stat a debuff contests against when landing: physical debuffs are
/// ATK vs the target's CON, magical debuffs ATK vs the target's WIT (per docs/design/Disciplines.md).
/// None = not a contested debuff (uses the older fizzle/sure-hit path).</summary>
public enum DebuffSchool { None = 0, Physical = 1, Magical = 2 }

/// <summary>A player's PvP name state (L2-style): Innocent = white, Flagged = purple (recently
/// attacked another player), Pk = red (has karma from killing innocents).</summary>
public enum PvpFlag : byte { Innocent = 0, Flagged = 1, Pk = 2 }

/// <summary>Staff role, held PER CHARACTER (owner) — an admin account can have ordinary characters too.
/// Player = normal; Moderator = a trusted PLAYER who may only jail/kick/chatban (so policing keeps the
/// player spirit of the game); Admin = everything, and only an Admin may act on a Moderator.
/// The server AUTHORIZES every non-debug admin action against this — unlike the DEBUG cheats, which are
/// removed by `#if DEBUG` and never ship.</summary>
public enum AccountRole { Player = 0, Moderator = 1, Admin = 2 }

/// <summary>A party member's presence, for the roster's AFK indicator. Auto = online but
/// auto-hunting (AFK-ish); Offline = disconnected but still offline-farming in the world.</summary>
public enum PartyMemberStatus { Online = 0, Auto = 1, Offline = 2 }

/// <summary>How a party distributes ITEM loot from a kill. Gold is ALWAYS split among in-range
/// members regardless of this setting. Solo players always behave as FindersKeepers.</summary>
public enum LootMode
{
    /// <summary>Loot goes to whoever landed the kill (the default, and the solo behavior).</summary>
    FindersKeepers = 0,
    /// <summary>Each dropped item goes to a random in-range member.</summary>
    Random = 1,
    /// <summary>Each dropped item goes to the next member in rotation (round-robin).</summary>
    RoundRobin = 2,
    /// <summary>All loot goes to the party leader (if in range; else the killer).</summary>
    LeaderOnly = 3,
}

/// <summary>A control state on a target, for conditional-damage skills ("+X% vs slowed/
/// rooted"). Flags so a skill can reward any of several states.</summary>
[Flags]
public enum TargetCondition
{
    None = 0, Slowed = 1, Rooted = 2, Stunned = 4, Feared = 8
}

/// <summary>Whether a magnitude is a flat add or a percentage of the base stat.
/// Combined per stat as: final = (base + Sum(flat)) * (1 + Sum(percent)).</summary>
public enum ModifierMode { Percent = 0, Flat = 1 }

/// <summary>One effect's magnitude on a skill/buff, e.g. (BuffMoveSpeed, 33, Flat)
/// or (BuffAtk, 0.20f, Percent). A skill carries an array so each stat has its
/// own value AND its own flat/percent mode (you can even list the same effect
/// twice: one Flat, one Percent).</summary>
public readonly record struct EffectMagnitude(
    SkillEffect Effect, float Value, ModifierMode Mode = ModifierMode.Percent);
