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
    ManaHeal = 8 // MP was restored (shown as +N MP, distinct from HP heal)
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
    // Room to grow up to 1L << 62.

    // Convenience masks.
    AnyDamage = PhysicalDamage | MagicDamage,
    AnyBuff   = BuffAtk | BuffDef | BuffMoveSpeed | BuffAtkSpeed | BuffCastSpeed
              | BuffEvasion | BuffHp | BuffMp | BuffBlockChance | BuffShieldDef
              | BuffMagicDef | BuffHpRegen | BuffMpRegen | HealOverTime
              | BuffPhysAtk | BuffMagAtk | BuffAccuracy | BuffCritRate | BuffMagicCritRate
              | BuffCritDamage | BuffCritDmgResist | BuffCritRateResist | BuffBowResist
              | BuffMagicFailFloor | BuffMagicFailResist | BuffInterruptPower
              | BuffInterruptResist | BuffMeleeVamp | BuffSpellVamp | BuffCooldown,
    // Harmful effects applied to an enemy (offensive; can fail; cleansable).
    AnyDebuff = DebuffDef | DebuffHealRecv | Root,
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
