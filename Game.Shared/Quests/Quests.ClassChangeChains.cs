namespace Game.Shared;

using static Game.Shared.QuestStepType;

/// <summary>
/// Class-change quest chains, generated for EVERY playable second class
/// (ClassCatalog.Playable). Each class gets a uniform two-quest chain plus a
/// class-change requirement, all gated to the right race + base class so a
/// character is only ever offered the chains they can actually pursue:
///
///   Quest 1 "Trial of the &lt;Class&gt;" (lvl 18, Elder Marius) -> Trial Token.
///   Quest 2 "Path of the &lt;Class&gt;" (lvl 20, requires Q1, High Priest Oren)
///       -> &lt;Class&gt;'s Proof.
///   Class change: bring both proofs to Class Master Vael (consumes them).
///
/// The target mobs vary by archetype for a little flavour. To give a class a
/// fully bespoke chain later, remove it from the loop and hand-author a file.
/// </summary>
public static partial class QuestCatalog
{
    private const string TrialGiver = "elder_marius";
    private const string PathGiver = "priest_oren";

    // (firstMob, secondMob) the two quests send you to hunt, by archetype.
    private static (string A, string B) HuntTargets(Archetype a) => a switch
    {
        Archetype.Tank => ("dire_boar", "road_bandit"),
        Archetype.Warrior => ("road_bandit", "dire_boar"),
        Archetype.Rogue => ("cave_spider", "grey_wolf"),
        Archetype.Archer => ("grey_wolf", "brown_boar"),
        Archetype.Healer => ("green_slime", "cave_spider"),
        Archetype.Nuker => ("cave_spider", "green_slime"),
        _ => ("grey_wolf", "brown_boar"),
    };

    static partial void RegisterClassChangeChains()
    {
        foreach (var cls in ClassCatalog.Playable)
        {
            var (mobA, mobB) = HuntTargets(cls.Archetype);
            string mobAName = MobCatalog.Get(mobA).Name;
            string mobBName = MobCatalog.Get(mobB).Name;
            string q1 = $"cc_{cls.Id}_1";
            string q2 = $"cc_{cls.Id}_2";

            Register(new QuestDef(
                Id: q1,
                Name: $"Trial of the {cls.Name}",
                Description: $"Elder Marius will judge whether you are fit to walk the " +
                             $"path of the {cls.Name}. Prove yourself in the field.",
                OfferNpcId: TrialGiver,
                MinLevel: 18,
                ForRace: cls.Race,
                ForBaseClass: cls.Base,
                PreClassChange: true,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Elder Marius", TargetId: TrialGiver),
                    new QuestStep(KillMobs, $"Slay 5 {mobAName}", TargetId: mobA,
                        Count: 5, MinLevel: 1, MaxLevel: 40),
                    new QuestStep(TalkTo, "Return to Elder Marius", TargetId: TrialGiver),
                },
                Reward: new QuestReward(Exp: 400, SkillPoints: 1,
                    ItemIds: new[] { ItemCatalog.ClassTokenId(cls.Id) })));

            Register(new QuestDef(
                Id: q2,
                Name: $"Path of the {cls.Name}",
                Description: $"Bearing the {cls.Name} Trial Token, seek High Priest Oren " +
                             $"and complete the trial that opens the path of the {cls.Name}.",
                OfferNpcId: PathGiver,
                MinLevel: 20,
                RequiresQuestId: q1,
                ForRace: cls.Race,
                ForBaseClass: cls.Base,
                PreClassChange: true,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with High Priest Oren", TargetId: PathGiver),
                    new QuestStep(KillMobs, $"Slay 8 {mobBName}", TargetId: mobB,
                        Count: 8, MinLevel: 1, MaxLevel: 40),
                    new QuestStep(TalkTo, "Return to High Priest Oren", TargetId: PathGiver),
                },
                Reward: new QuestReward(Exp: 800, SkillPoints: 2,
                    ItemIds: new[] { ItemCatalog.ClassProofId(cls.Id) })));
        }
    }
}

public static partial class ClassChangeRequirements
{
    static partial void RegisterClassChangeRequirements()
    {
        foreach (var cls in ClassCatalog.Playable)
        {
            Register(new Requirement(
                SecondClassId: cls.Id,
                ClassName: cls.Name,
                MinLevel: 20,
                RequiredItemIds: new[]
                {
                    ItemCatalog.ClassTokenId(cls.Id),
                    ItemCatalog.ClassProofId(cls.Id),
                },
                NpcId: "master_class"));
        }
    }
}
