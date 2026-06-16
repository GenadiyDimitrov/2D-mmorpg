namespace Game.Shared;

// ===========================================================================
//  QUEST GROUNDWORK (data types only — no live system yet).
//
//  These shapes exist so the rest of the code (and the class-change quest hook
//  SecondClassDef.RequiredQuestId) can reference quests now. The live system —
//  NPC entities, the dialog/quest-log UI, and progress tracking — is a future
//  phase. When it lands, it will read these definitions and persist
//  CharacterQuestState.
// ===========================================================================

/// <summary>The kind of objective a quest step requires.</summary>
public enum QuestStepType
{
    TalkTo = 0,      // talk to NPC (TargetId = npc id)
    KillMobs = 1,    // kill N mobs of a type within a level band
    CollectItem = 2, // gather N of an item (TargetId = item key)
    ReachLevel = 3   // reach a character level (Count = level)
}

/// <summary>One ordered step of a quest.</summary>
public record QuestStep(
    QuestStepType Type,
    string Text,                // shown in the quest log
    string TargetId = "",       // npc id / mob type / item key, per Type
    int Count = 1,              // how many (kills, items, or the level)
    int MinLevel = 0,
    int MaxLevel = 0);

/// <summary>What the player gets for completing a quest.</summary>
public record QuestReward(int Exp = 0, int SkillPoints = 0, string[]? ItemIds = null);

/// <summary>A quest definition (static content).</summary>
public record QuestDef(
    string Id,
    string Name,
    string Description,
    QuestStep[] Steps,
    QuestReward Reward,
    int MinLevel = 1);

/// <summary>Per-character progress on a quest (persisted as JSON later).
/// StepIndex is the current step; Counter tracks kills/collected for that step.</summary>
public record CharacterQuestState(string QuestId, int StepIndex, int Counter, bool Completed);

/// <summary>
/// Placeholder catalog — empty for now. When the quest phase begins, quests
/// (including class-change quests referenced by SecondClassDef.RequiredQuestId)
/// get authored here, likely split into per-line files like the class skills.
/// </summary>
public static class QuestCatalog
{
    private static readonly Dictionary<string, QuestDef> All = new();

    public static QuestDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);
    public static IEnumerable<QuestDef> AllQuests => All.Values;
}
