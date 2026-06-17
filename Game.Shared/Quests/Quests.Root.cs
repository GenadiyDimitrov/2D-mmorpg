namespace Game.Shared;

/// <summary>
/// Quest registration root. Each per-chain file under Quests/ implements a
/// Register method; this calls them all. To add a quest chain, create a new
/// file and add its Register call here.
/// </summary>
public static partial class QuestCatalog
{
    static partial void RegisterAll()
    {
        RegisterHumanMageClericChain();
        // Add more chains here, e.g. RegisterHumanMageSorcererChain();
    }

    // Implemented in the per-chain files.
    static partial void RegisterHumanMageClericChain();
}

public static partial class ClassChangeRequirements
{
    static partial void RegisterAll()
    {
        RegisterClericChange();
        // Add more class changes here.
    }

    static partial void RegisterClericChange();
}
