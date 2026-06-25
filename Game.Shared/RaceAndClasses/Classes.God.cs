namespace Game.Shared;

using static Game.Shared.SkillCatalog;

public static partial class ClassSkillTables
{
    private static void RegisterGod()
    {
        ClassSkills.Register(Race.God, BaseClass.Mage, Archetype.Nuker,
            // Cleric sees Wind Walk as "Holy Speed" (same shared wind_walk id).
            new ClassSkill(WindWalk, 20, DisplayName: "God Speed")
            , new ClassSkill(MassWindWalk, 30, DisplayName: "God Procession")
            , new ClassSkill(HpBoost, 40, SkillLevel: 1)   // +5%  Max HP
            , new ClassSkill(HpBoost, 50, SkillLevel: 2)   // +15% Max HP
            , new ClassSkill(HpBoost, 60, SkillLevel: 3)   // +35% Max HP
            , new ClassSkill(GreaterHeal, 1) // +35% Max HP at 40
            , new ClassSkill(GreaterWarCry, 2)
            , new ClassSkill(GreaterWeakness, 3)
            , new ClassSkill(Fortify, 4)
            , new ClassSkill(FlameBolt, 5)
            );
    }
}
