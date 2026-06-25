namespace Game.Shared;

using static Game.Shared.SkillCatalog;

/// <summary>
/// Human Mage line — the per-class place to author skills BEYOND the shared
/// archetype kit. The core Healer/Nuker kit is registered in
/// ClassSkillTables.Common.cs; here we add the learnable progression unique to
/// Human clerics/sorcerers (HP Boost ranks, Wind Walk, etc.). 3rd/4th classes
/// (Bishop at 40, Cardinal at 76) will be added here when class tiers land.
/// </summary>
public static partial class ClassSkillTables
{
    private static void RegisterHumanMage()
    {
        // Cleric (Healer) learnable progression is dropped pending the new cleric@20
        // CSV — re-author here (HP Boost, Wind Walk as "Holy Speed", etc.) when it lands.

        // Sorcerer (Nuker) extra.
        ClassSkills.Register(Race.Human, BaseClass.Mage, Archetype.Nuker,
            new ClassSkill(WindWalk, 25));
    }
}
