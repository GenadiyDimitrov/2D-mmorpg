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
    Local = 0,   // visible within ViewRange
    World = 1,   // visible to everyone (slow mode comes later)
    System = 2   // server / admin messages
}
