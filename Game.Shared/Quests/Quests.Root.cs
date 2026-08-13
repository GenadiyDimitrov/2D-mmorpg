namespace Game.Shared;

/// <summary>
/// Quest registration root. The class-change chains for every playable second
/// class are generated in Quests.ClassChangeChains.cs; add other (hand-authored)
/// chains by creating a file with a Register method and calling it here.
/// </summary>
public static partial class QuestCatalog
{
    static partial void RegisterAll()
    {
        RegisterTutorialChain();          // lvl 1-20 — "meet the town", and it earns the Newbie kit
        RegisterClassChangeChains();      // 2nd class (lvl 18/20)
        RegisterThirdClassChains();       // 3rd class (lvl 40, longer + harder)
        RegisterDailyQuests();            // repeatable once per server day
        RegisterRepeatableQuests();       // the Huntmasters' endless + finite contracts
        RegisterProfessionQuests();       // the five crafting masters' joining quests (lvl 20)
        // Add more (non-class-change) chains here.
    }

    // Implemented in Quests.Tutorial.cs (playtest-19 M5; it replaced Quests.Starter.cs).
    static partial void RegisterTutorialChain();
    // Implemented in Quests.ClassChangeChains.cs.
    static partial void RegisterClassChangeChains();
    static partial void RegisterThirdClassChains();
    // Implemented in Quests.Daily.cs.
    static partial void RegisterDailyQuests();
    // Implemented in Quests.Repeatable.cs.
    static partial void RegisterRepeatableQuests();
    // Implemented in Quests.Professions.cs.
    static partial void RegisterProfessionQuests();
}

public static partial class ClassChangeRequirements
{
    static partial void RegisterAll()
    {
        RegisterClassChangeRequirements();
        RegisterThirdClassRequirements();
    }

    static partial void RegisterClassChangeRequirements();
    static partial void RegisterThirdClassRequirements();
}
