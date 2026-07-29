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
        RegisterStarterChain();           // lvl 10/12 — earns the Newbie kit (was given at creation)
        RegisterClassChangeChains();      // 2nd class (lvl 18/20)
        RegisterThirdClassChains();       // 3rd class (lvl 40, longer + harder)
        RegisterDailyQuests();            // repeatable once per server day
        // Add more (non-class-change) chains here.
    }

    // Implemented in Quests.Starter.cs.
    static partial void RegisterStarterChain();
    // Implemented in Quests.ClassChangeChains.cs.
    static partial void RegisterClassChangeChains();
    static partial void RegisterThirdClassChains();
    // Implemented in Quests.Daily.cs.
    static partial void RegisterDailyQuests();
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
