using System.Collections.Generic;
using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>
/// ONE class a character owns. A character owns SEVERAL of these (IG-style subclasses) and plays
/// exactly one at a time — <see cref="Entity.ActiveSubclass"/>.
///
/// THE SPLIT (this is the load-bearing part of the design — get it wrong and it has to be redone):
///
///   CLASS-level  (here, per subclass)   level, XP, skill points, base class, 2nd/3rd class,
///                                       the four core stats, learned skills, skill-bar layout,
///                                       and the bar's AUTO marks
///   CHARACTER-level (on Entity)         race, inventory, gold, karma, quests, profession,
///                                       auto-hunt PREFERENCES (enabled, potion thresholds),
///                                       world position
///
/// ⚠ "auto-hunt settings" used to sit wholly on the character line above, and that sentence WAS the
/// 2026-08-28 auto-on bug: the bar was class-level while the marks that arm it were character-level,
/// so one feature straddled the split. Only the preferences belong on the right; the marks moved.
///   CLIENT-level (client-settings.json) window position/size, and nothing else
///
/// The core stats live HERE, not on the character, because they are derived from (Race, BaseClass) —
/// swap from a fighter to a mage and CON/ATK/WIT/AGI must swap with it. Race is shared: one body,
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

    /// <summary>PER CLASS (owner, 2026-07-15): a subclass can be a DIFFERENT RACE from the others —
    /// you pick a full 3rd-class discipline across all races, and each discipline is a specific race's
    /// version (a human Tempest vs an elf Tempest). So race is no longer purely character-level; the
    /// active subclass's race is what <see cref="Entity.Race"/> reports.</summary>
    public Race Race { get; set; }

    public BaseClass BaseClass { get; set; }

    /// <summary>0 = none; otherwise a ClassCatalog id (taken at level 20).</summary>
    public int SecondClass { get; set; }

    /// <summary>0 = none; otherwise a ThirdClassCatalog id (101-136, taken at 40).</summary>
    public int ThirdClass { get; set; }

    /// <summary>0 = none; otherwise a FourthClassCatalog id (201-236, taken at 76). Always the
    /// ascension of <see cref="ThirdClass"/> — never a different discipline — so it is redundant
    /// with it by construction and stored anyway, because "has the player taken it" is the only
    /// thing that can be asked of a threshold.</summary>
    public int FourthClass { get; set; }

    public int Level { get; set; } = 1;
    public long Exp { get; set; }
    public int SkillPoints { get; set; }

    // Core stats — from (Race, BaseClass) at creation, then moved only by the level-40 stat swaps.
    public int Con { get; set; }
    public int Atk { get; set; }
    public int Wit { get; set; }
    public int Agi { get; set; }
    public int Spt { get; set; }

    /// <summary>Learned skills → the level of each.</summary>
    public Dictionary<string, int> LearnedSkills { get; } = new();

    /// <summary>This class's skill-bar layout ("" = an empty slot). Per-CLASS: swap away, swap back,
    /// and the bar is exactly as you left it.</summary>
    public string[] SkillBar { get; set; } = Array.Empty<string>();

    /// <summary>This class's AUTO marks — which of its bar slots fire on their own.
    ///
    /// 🔑 MOVED HERE FROM Entity 2026-08-28, and the split it fixes was documented at the top of this
    /// very file: the bar was called class-level and "auto-hunt settings" character-level, so the two
    /// halves of ONE feature sat on opposite sides of the line. Owner's report: *"I'm buffer and have
    /// in skill belt the atack as auto on .. Then I change to/add new subclass ..and I put atack on
    /// belt it's auto-on from the getgo … if I have never put a skill on bar and haven't never make
    /// it auto-on it should never be auto on"*.
    ///
    /// The marks are keyed by SKILL ID with no slot and no class, so any subclass whose bar happened
    /// to contain that id rendered it already-armed. Filtering the shared list on swap would not have
    /// worked either — it is one list, so pruning it for the incoming class destroys the outgoing
    /// class's marks for good. It has to be per-class storage, which is what this is.
    ///
    /// ⚠ This is the SAME BUG as playtest-17 B1, one level down: that one leaked auto marks between
    /// CHARACTERS and was fixed by clearing them on character change (GameBoot.ResetWorldTransients).
    /// A subclass swap never leaves the world, so it never passed through that fix.
    ///
    /// ⚠ The REST of the auto-hunt config (enabled, potion thresholds, buff potion ids) stays on the
    /// character on purpose — those are preferences about how YOU play, not about this class's kit.
    /// </summary>
    public List<AutoSkillDto> AutoSkills { get; } = new();

    /// <summary>Roll the core stats for this class from its OWN (Race, BaseClass).</summary>
    public void RollBaseStats()
    {
        var s = StatCalculator.GetBaseStats(Race, BaseClass);
        Con = s.Con; Atk = s.Atk; Wit = s.Wit; Agi = s.Agi; Spt = s.Spt;
    }
}
