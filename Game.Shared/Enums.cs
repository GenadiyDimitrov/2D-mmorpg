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
    Buff = 6     // a buff/debuff was applied (Skill carries the name)
}

/// <summary>
/// What a skill does. A [Flags] enum so ONE skill can carry several effects:
/// Effect = BuffAtk | BuffMoveSpeed | BuffCastSpeed. Each effect's amount is
/// stored separately in the skill's Magnitudes map (see EffectMagnitude), so a
/// buffer-class skill is just "list the flags, give each a number" — no new
/// enum member per combination.
/// </summary>
[Flags]
public enum SkillEffect
{
    None           = 0,
    PhysicalDamage = 1 << 0,
    MagicDamage    = 1 << 1,
    Heal           = 1 << 2,
    BuffAtk        = 1 << 3,
    BuffDef        = 1 << 4,
    BuffMoveSpeed  = 1 << 5,
    BuffAtkSpeed   = 1 << 6,   // shortens basic-attack interval
    BuffCastSpeed  = 1 << 7,   // shortens cast time
    BuffEvasion    = 1 << 8,
    BuffHp         = 1 << 9,
    BuffMp         = 1 << 10,
    DebuffDef      = 1 << 11,
    // Room to grow: DebuffSpeed = 1 << 12, Stun = 1 << 13, ...

    // Convenience masks.
    AnyDamage = PhysicalDamage | MagicDamage,
    AnyBuff   = BuffAtk | BuffDef | BuffMoveSpeed | BuffAtkSpeed | BuffCastSpeed
              | BuffEvasion | BuffHp | BuffMp,
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
