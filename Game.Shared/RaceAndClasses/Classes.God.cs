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
            , new ClassSkill(HpBoost1, 40)   // +5%  Max HP at 20
            , new ClassSkill(HpBoost2, 50)   // +15% Max HP at 30
            , new ClassSkill(HpBoost3, 60) // +35% Max HP at 40
            , new ClassSkill(GreaterHeal, 1) // +35% Max HP at 40
            , new ClassSkill(GreaterWarCry, 2)
            , new ClassSkill(GreaterWeakness, 3)
            , new ClassSkill(Fortify, 4)
            , new ClassSkill(FlameBolt, 5)
            );
    }
}
