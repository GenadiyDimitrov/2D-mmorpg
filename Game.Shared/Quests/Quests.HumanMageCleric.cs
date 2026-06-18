namespace Game.Shared;

using static Game.Shared.QuestStepType;

/// <summary>
/// The Human Mage -> Cleric class-change chain (the worked example).
///
///   Quest 1 "A Test of Devotion" (any mage, lvl 18): given by Elder Marius —
///     talk, kill 5 spiders, return -> rewards the Mark of Faith.
///   Quest 2 "The Cleric's Path" (requires Quest 1 + lvl 20): given by High
///     Priest Oren — talk, kill 8 wolves, return -> rewards the Cleric's Proof.
///   Class change: bring Mark of Faith + Cleric's Proof to Class Master Vael ->
///     become a Cleric (items consumed).
///
/// Replicate this file for other classes (Sorcerer, Orc lines, etc.): new quest
/// ids, new quest items, new requirement row.
/// </summary>
public static partial class QuestCatalog
{
    static partial void RegisterHumanMageClericChain()
    {
        Register(new QuestDef(
            Id: "cleric_1_devotion",
            Name: "A Test of Devotion",
            Description: "Elder Marius asks you to prove your devotion by clearing " +
                         "the spiders troubling the village.",
            OfferNpcId: "elder_marius",
            MinLevel: 18,
            Steps: new[]
            {
                new QuestStep(TalkTo, "Speak with Elder Marius", TargetId: "elder_marius"),
                new QuestStep(KillMobs, "Slay 5 Cave Spiders", TargetId: "cave_spider",
                    Count: 5, MinLevel: 1, MaxLevel: 40),
                new QuestStep(TalkTo, "Return to Elder Marius", TargetId: "elder_marius"),
            },
            Reward: new QuestReward(Exp: 400, SkillPoints: 1,
                ItemIds: new[] { ItemCatalog.MarkOfFaith })));

        Register(new QuestDef(
            Id: "cleric_2_path",
            Name: "The Cleric's Path",
            Description: "Bearing the Mark of Faith, seek High Priest Oren and " +
                         "complete the trial that opens the path of the Cleric.",
            OfferNpcId: "priest_oren",
            MinLevel: 20,
            RequiresQuestId: "cleric_1_devotion",
            Steps: new[]
            {
                new QuestStep(TalkTo, "Speak with High Priest Oren", TargetId: "priest_oren"),
                new QuestStep(KillMobs, "Slay 8 Grey Wolves", TargetId: "grey_wolf",
                    Count: 8, MinLevel: 1, MaxLevel: 40),
                new QuestStep(TalkTo, "Return to High Priest Oren", TargetId: "priest_oren"),
            },
            Reward: new QuestReward(Exp: 800, SkillPoints: 2,
                ItemIds: new[] { ItemCatalog.ClericsProof })));
    }
}

public static partial class ClassChangeRequirements
{
    static partial void RegisterClericChange()
    {
        // Cleric = second class id 17 (see ClassCatalog). Requires both proofs,
        // performed by Class Master Vael, who consumes them.
        Register(new Requirement(
            SecondClassId: 17,
            ClassName: "Cleric",
            MinLevel: 20,
            RequiredItemIds: new[] { ItemCatalog.MarkOfFaith, ItemCatalog.ClericsProof },
            NpcId: "master_class"));
    }
}
