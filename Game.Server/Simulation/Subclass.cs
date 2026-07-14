using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// ONE class a character owns. A character owns SEVERAL of these (L2-style subclasses) and plays
/// exactly one at a time — <see cref="Entity.ActiveSubclass"/>.
///
/// THE SPLIT (this is the load-bearing part of the design — get it wrong and it has to be redone):
///
///   CLASS-level  (here, per subclass)   level, XP, skill points, base class, 2nd/3rd class,
///                                       the four core stats, learned skills, skill-bar layout
///   CHARACTER-level (on Entity)         race, inventory, gold, karma, quests, profession,
///                                       auto-hunt settings, world position
///   CLIENT-level (client-settings.json) window position/size, and nothing else
///
/// The core stats live HERE, not on the character, because they are derived from (Race, BaseClass) —
/// swap from a fighter to a mage and CON/ATK/WIT/DEX must swap with it. Race is shared: one body,
/// several trainings.
///
/// <see cref="Entity"/> PROXIES its Level / BaseClass / LearnedSkills / … straight into the active
/// subclass, so every line of game logic that says <c>player.Level</c> keeps working untouched and a
/// class swap is just re-pointing the index. That is the whole reason this refactor is small.
/// </summary>
public class Subclass
{
    /// <summary>Stable slot id. 0 = the class the character was created as (the "main"); it can never
    /// be deleted. Higher slots are added subclasses.</summary>
    public int Slot { get; init; }

    public BaseClass BaseClass { get; set; }

    /// <summary>0 = none; otherwise a ClassCatalog id (taken at level 20).</summary>
    public int SecondClass { get; set; }

    /// <summary>0 = none; otherwise a ThirdClassCatalog id (101-136, taken at 40).</summary>
    public int ThirdClass { get; set; }

    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public int SkillPoints { get; set; }

    // Core stats — from (Race, BaseClass) at creation, then moved only by the level-40 stat swaps.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }

    /// <summary>Learned skills → the level of each.</summary>
    public Dictionary<string, int> LearnedSkills { get; } = new();

    /// <summary>This class's skill-bar layout ("" = an empty slot). Per-CLASS: swap away, swap back,
    /// and the bar is exactly as you left it.</summary>
    public string[] SkillBar { get; set; } = Array.Empty<string>();

    /// <summary>Roll the core stats for this class from the character's race.</summary>
    public void RollBaseStats(Race race)
    {
        var s = StatCalculator.GetBaseStats(race, BaseClass);
        Con = s.Con; Atk = s.Atk; Wit = s.Wit; Dex = s.Dex;
    }
}
