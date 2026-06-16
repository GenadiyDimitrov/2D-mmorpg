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
        // Cleric (Healer) learnable progression — these show in "Skills to Learn"
        // at their level and are learned by spending SP.
        ClassSkills.Register(Race.Human, BaseClass.Mage, Archetype.Healer,
            new ClassSkill(WindWalk, 20),
            new ClassSkill(HpBoost1, 20),   // +5%  Max HP at 20
            new ClassSkill(HpBoost2, 30),   // +15% Max HP at 30
            new ClassSkill(HpBoost3, 40));  // +35% Max HP at 40

        // Sorcerer (Nuker) extra.
        ClassSkills.Register(Race.Human, BaseClass.Mage, Archetype.Nuker,
            new ClassSkill(WindWalk, 25));
    }
}
