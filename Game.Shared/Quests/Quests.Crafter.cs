namespace Game.Shared;

/// <summary>
/// THE CRAFTER QUEST (`BL-273` part 2, 0.203.0) — the Master Crafter's trial, at level 40.
///
/// <para>His steps, verbatim (2026-09-24): *"talk it says to: 1. gather: atleast 20 quest wood and atleast
/// 20 iron from zone A, atleast 20 quest gems from zone B and atleast 2 recipies (40% less punishing) +
/// atleast 1 hammer head from zone C -> 2. bring mats back -> 3. ask u to learn rcp if not learned -> 4. ask
/// u to try craft -> 5.1. fail go to 1. -> 5.2 succeed gets the "Blacksmiths Hammer [Quest Item]" -> 6. give
/// him the hammer -> 7. u are crafter (10 slots - L0 on generic and typed)"*.</para>
///
/// <para>🔑 <b>The quest IS the first craft.</b> Everything a crafter does later happens once here: gather,
/// learn a recipe into a slot, spend a recipe on an attempt at the Master, and lose it all on a fail. That
/// is why a failed craft sends you back to step 1 rather than letting you retry: the mats and the used
/// recipe are gone, exactly as they will be at T80.</para>
///
/// <para>⚠ The gathered mats are <c>PaidAtHandIn: false</c>: the CRAFT spends them, so the final talk must
/// not re-check them (it would find none and send you back to gather). Only the hammer is paid to him.
/// The quest's gather lines drop the tokens while the quest is active and pay nothing (modifier 0); any
/// spares are collected at the hand-in.</para>
///
/// <para>⚠ Zones and drop chances are PLACEHOLDERS (he named zones A/B/C, not mobs): A = Dune Orc Archers
/// (40), B = Harpies (42), C = Marsh Marauders (46), the tougher camp for the rarer drops.</para>
/// </summary>
public static partial class QuestCatalog
{
    public const string QuestBecomeCrafter = "become_crafter";

    /// <summary>The step a failed hammer craft sends you back to (the first gather step).</summary>
    public const int CrafterQuestGatherStep = 1;

    /// <summary>The step on which the hammer may be crafted (his "try craft").</summary>
    public const int CrafterQuestCraftStep = 8;

    static partial void RegisterCrafterQuest()
    {
        string master = WorldMap.CraftMasterId;
        Register(new QuestDef(
            Id: QuestBecomeCrafter,
            Name: "The Master's Trial",
            Description:
                "The Master Crafter takes anyone who can make one thing with their own hands. Gather what a "
              + "hammer needs, learn its recipe, and try to forge it at his anvil. A failed attempt eats the "
              + "materials and the recipe you used, just as every craft will. Finish it and you are a "
              + "crafter for good: ten recipe slots, and every craft you make from then on raises your "
              + "crafting level and your weapon, armour or jewel level.",
            OfferNpcId: master,
            MinLevel: Crafting.CrafterQuestLevel,
            AnyTownNpc: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo, "Hear the Master Crafter out", TargetId: master),
                new QuestStep(QuestStepType.CollectItem,
                    "Gather at least 20 Seasoned Hardwood from the Dune Orc Archers",
                    TargetId: ItemCatalog.CrafterQuestWood, Count: 20, PaidAtHandIn: false),
                new QuestStep(QuestStepType.CollectItem,
                    "Gather at least 20 Raw Iron from the Dune Orc Archers",
                    TargetId: ItemCatalog.CrafterQuestIron, Count: 20, PaidAtHandIn: false),
                new QuestStep(QuestStepType.CollectItem,
                    "Gather at least 20 Rough Gems from the Harpies",
                    TargetId: ItemCatalog.CrafterQuestGem, Count: 20, PaidAtHandIn: false),
                new QuestStep(QuestStepType.CollectItem,
                    "Take at least 2 hammer recipes from the Marsh Marauders (one to learn, one to use)",
                    TargetId: ItemCatalog.CrafterQuestRecipe, Count: 2, PaidAtHandIn: false),
                new QuestStep(QuestStepType.CollectItem,
                    "Take a Hammer Head from the Marsh Marauders",
                    TargetId: ItemCatalog.CrafterHammerHead, Count: 1, PaidAtHandIn: false),
                new QuestStep(QuestStepType.TalkTo, "Bring the materials back to the Master Crafter",
                    TargetId: master),
                new QuestStep(QuestStepType.DoAction,
                    "Learn the hammer recipe: use one from your bag",
                    TargetId: QuestActions.LearnRecipe),
                new QuestStep(QuestStepType.CollectItem,
                    "Try to craft the Blacksmith's Hammer at the Master Crafter (a fail sends you back to gather)",
                    TargetId: ItemCatalog.CrafterHammer, Count: 1),
                new QuestStep(QuestStepType.TalkTo, "Give the hammer to the Master Crafter", TargetId: master),
            },
            // The reward IS becoming a crafter (granted on completion by the server).
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("dune_orc_archer", ItemCatalog.CrafterQuestWood, 0.5f, 0f),
                new QuestGather("dune_orc_archer", ItemCatalog.CrafterQuestIron, 0.5f, 0f),
                new QuestGather("harpy", ItemCatalog.CrafterQuestGem, 0.5f, 0f),
                new QuestGather("marsh_marauder", ItemCatalog.CrafterQuestRecipe, 0.15f, 0f),
                new QuestGather("marsh_marauder", ItemCatalog.CrafterHammerHead, 0.10f, 0f),
            }));
    }
}
