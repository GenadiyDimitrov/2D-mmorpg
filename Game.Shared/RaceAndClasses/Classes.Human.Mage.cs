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

        // Sorcerer (Nuker) has NO extra beyond the shared archetype kit.
        //
        // Wind Walk @25 used to live here and was removed 2026-07-31 (playtest-15): a nuker
        // self-buffing move speed stacked on top of the cleric's Wind Walk line — same effect,
        // different BuffKey holder — and made the class quietly faster than intended. Speed is the
        // BUFFER's identity to give, so the skill stays in the catalog for cleric/god and the nuker
        // no longer learns it.
    }
}
