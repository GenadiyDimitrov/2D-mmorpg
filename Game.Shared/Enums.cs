namespace Game.Shared;

public enum Race
{
    Human = 0,
    Elf = 1,
    Ork = 2
}

public enum BaseClass
{
    Fighter = 0,
    Mage = 1
}

public enum EntityKind
{
    Player = 0,
    Mob = 1
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

public enum SkillEffect
{
    PhysicalDamage = 0,
    MagicDamage = 1,
    Heal = 2,
    BuffAtk = 3,
    DebuffDef = 4
}
