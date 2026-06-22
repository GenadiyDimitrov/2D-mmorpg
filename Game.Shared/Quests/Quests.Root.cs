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
        RegisterClassChangeChains();
        // Add more (non-class-change) chains here.
    }

    // Implemented in Quests.ClassChangeChains.cs.
    static partial void RegisterClassChangeChains();
}

public static partial class ClassChangeRequirements
{
    static partial void RegisterAll()
    {
        RegisterClassChangeRequirements();
    }

    static partial void RegisterClassChangeRequirements();
}
