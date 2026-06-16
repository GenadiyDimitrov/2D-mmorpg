namespace Game.Shared;

/// <summary>Gameplay archetype of a second class. The 18 named classes from
/// the design doc map onto 6 archetypes that drive skills and bonuses;
/// per-class flavour differences (race passives etc.) come later.</summary>
public enum Archetype
{
    Tank = 0,     // heavy+shield: fortress
    Warrior = 1,  // heavy+2h: big hits
    Rogue = 2,    // light+dual: fast melee
    Archer = 3,   // light+bow: ranged basic attacks (+500 range)
    Healer = 4,   // robe+1h: heals/buffs (+500 spell range)
    Nuker = 5     // robe+2h: damage spells (+500 spell range)
}

/// <summary>A second class. RequiredQuestId is groundwork for class-change
/// quests — null means level-gated only (current behaviour); when the quest
/// system lands, a non-null id will require that quest be completed first.</summary>
public record SecondClassDef(int Id, string Name, Race Race, BaseClass Base, Archetype Archetype,
    string? RequiredQuestId = null);

public static class ClassCatalog
{
    private static readonly Dictionary<int, SecondClassDef> All = new SecondClassDef[]
    {
        // Ork / Demon
        new(1,  "Beast",       Race.Ork,   BaseClass.Fighter, Archetype.Tank),
        new(2,  "Warrior",     Race.Ork,   BaseClass.Fighter, Archetype.Warrior),
        new(3,  "Stalker",     Race.Ork,   BaseClass.Fighter, Archetype.Rogue),
        new(4,  "Hunter",      Race.Ork,   BaseClass.Fighter, Archetype.Archer),
        new(5,  "Shaman",      Race.Ork,   BaseClass.Mage,    Archetype.Healer),
        new(6,  "Witch",       Race.Ork,   BaseClass.Mage,    Archetype.Nuker),
        // Elf / Angel
        new(7,  "Templar",     Race.Elf,   BaseClass.Fighter, Archetype.Tank),
        new(8,  "Sentinel",    Race.Elf,   BaseClass.Fighter, Archetype.Warrior),
        new(9,  "Shadowblade", Race.Elf,   BaseClass.Fighter, Archetype.Rogue),
        new(10, "Warden",      Race.Elf,   BaseClass.Fighter, Archetype.Archer),
        new(11, "Priest",      Race.Elf,   BaseClass.Mage,    Archetype.Healer),
        new(12, "Inquisitor",  Race.Elf,   BaseClass.Mage,    Archetype.Nuker),
        // Human
        new(13, "Knight",      Race.Human, BaseClass.Fighter, Archetype.Tank),
        new(14, "Champion",    Race.Human, BaseClass.Fighter, Archetype.Warrior),
        new(15, "Assassin",    Race.Human, BaseClass.Fighter, Archetype.Rogue),
        new(16, "Marksman",    Race.Human, BaseClass.Fighter, Archetype.Archer),
        new(17, "Cleric",      Race.Human, BaseClass.Mage,    Archetype.Healer),
        new(18, "Sorcerer",    Race.Human, BaseClass.Mage,    Archetype.Nuker),
        // God race (debug-only): one fighter path + one mage path.
        new(98, "Demigod",     Race.God,   BaseClass.Fighter, Archetype.Warrior),
        new(99, "Ascendant",   Race.God,   BaseClass.Mage,    Archetype.Nuker),
    }.ToDictionary(c => c.Id);

    public static SecondClassDef? Get(int id) => All.GetValueOrDefault(id);

    public static IEnumerable<SecondClassDef> OptionsFor(Race race, BaseClass baseClass) =>
        All.Values.Where(c => c.Race == race && c.Base == baseClass).OrderBy(c => c.Id);

    /// <summary>Permanent core-stat bonus applied once at class change.</summary>
    public static (int Con, int Atk, int Wit, int Dex) StatBonus(Archetype archetype) =>
        archetype switch
        {
            Archetype.Tank => (10, 2, 0, 2),
            Archetype.Warrior => (4, 8, 0, 2),
            Archetype.Rogue => (2, 4, 0, 10),
            Archetype.Archer => (2, 6, 0, 8),
            Archetype.Healer => (4, 0, 8, 2),
            Archetype.Nuker => (0, 4, 10, 2),
            _ => (0, 0, 0, 0)
        };
}
