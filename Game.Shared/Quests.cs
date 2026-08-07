namespace Game.Shared;

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

/// <summary>What the player gets on completion. ItemIds grants quest items.</summary>
public record QuestReward(int Exp = 0, int SkillPoints = 0, string[]? ItemIds = null, int Gold = 0);

/// <summary>
/// One "this creature yields this token" line of a GATHERING quest (owner, playtest-13: *"can be kill
/// mobs indefinitely, gathering quest items as u farm in a specific zone"*).
///
/// While the quest is active, every credited kill of <paramref name="MobId"/> rolls
/// <paramref name="DropChance"/> for one <paramref name="ItemId"/>. On turn-in the tokens are counted,
/// consumed, and paid out ON TOP of the quest's own reward.
///
/// <para><b>RewardModifier is the owner's per-mob <c>QuestItemRewardModifier</c></b>: each token pays
/// that fraction of the creature's OWN kill exp and gold, again. So a token is always worth what the
/// thing that dropped it was worth — a quest authored once stays level-appropriate, and the only
/// number an author picks is "how much of a bonus is farming this worth", which is what the knob is
/// for. Deriving it from the mob is also why the modifier is per-MOB and not per-quest: 20 skeletons
/// and 55 bears pay separately, exactly as he wrote it.</para>
/// </summary>
public record QuestGather(
    string MobId,
    string ItemId,
    float DropChance = 1f,
    float RewardModifier = 0.25f);

/// <summary>
/// A quest definition (static content). OfferNpcId is the NPC who gives it;
/// RequiresQuestId chains quests (this only offers once that one is complete).
/// </summary>
public record QuestDef(
    string Id,
    string Name,
    string Description,
    string OfferNpcId,
    QuestStep[] Steps,
    QuestReward Reward,
    int MinLevel = 1,
    string? RequiresQuestId = null,
    // Class-quest gating: only offer to this race / base class (null = any), and
    // (when PreClassChange) only while the character has no second class yet.
    Race? ForRace = null,
    BaseClass? ForBaseClass = null,
    bool PreClassChange = false,
    // 3rd-class gating: only offer to a character holding this 2nd class, and only
    // before they take a 3rd class (these chains stop the moment a discipline is chosen).
    int? ForSecondClass = null,
    // ----- Level RANGE (owner, playtest-13). MinLevel above is the floor; this is the ceiling, past
    //       which the quest can no longer be ACCEPTED (0 = no ceiling). A quest already in progress is
    //       never cancelled by out-levelling it — only taking it fresh is blocked, which is what stops
    //       a level-60 walking back to farm the starter chain.
    //       CLASS quests deliberately have NO ceiling: you need your job whenever you get round to it.
    int MaxLevel = 0,
    // ----- DAILY: the quest can be taken again after the server-time day rolls over. Completing it
    //       moves it out of CompletedQuests and into the daily ledger instead, so it never permanently
    //       closes. 0 = a normal one-shot quest.
    bool Daily = false,
    // ----- REPEATABLE: handing it in does not retire it. The id is still recorded in CompletedQuests
    //       (so "have I ever done this" and RequiresQuestId chains still work), but the offer filter
    //       ignores that record and the NPC hands it straight back. Combine with Daily to get the
    //       owner's "can be taken again — if not daily limited": Daily wins, and the day-stamp gates it.
    bool Repeatable = false,
    // ----- GATHERING lines: creatures that drop this quest's tokens while it is active, each with its
    //       own QuestItemRewardModifier. A quest with Gathers is the "endless" kind — its only step is
    //       normally a TalkTo back at the giver, so you hand in whenever you have farmed enough.
    QuestGather[]? Gathers = null,
    // ----- ANY TOWN: every town's copy of the offering NPC gives this quest, and any of them takes it
    //       back (owner, playtest-19 M11: "i have no way to go back to the 1st town just to take it").
    //       ONE quest id, so it still cannot be run twice in a day — what widens is only WHERE.
    //       OfferNpcId stays the starter town's id; WorldMap.IsSameService matches the rest.
    bool AnyTownNpc = false)
{
    /// <summary>Does <paramref name="npcId"/> count as this quest's giver / turn-in NPC?
    /// Exact id normally; any town's copy of that service when <see cref="AnyTownNpc"/> is set.</summary>
    public bool GivenBy(string npcId) =>
        AnyTownNpc ? WorldMap.IsSameService(OfferNpcId, npcId) : OfferNpcId == npcId;

    /// <summary>Does a TalkTo step aimed at <paramref name="stepTargetId"/> accept this NPC? Same rule
    /// as <see cref="GivenBy"/> — a quest you can take anywhere must be handed in anywhere.</summary>
    public bool StepTargetMatches(string? stepTargetId, string npcId) =>
        stepTargetId is not null
        && (AnyTownNpc ? WorldMap.IsSameService(stepTargetId, npcId) : stepTargetId == npcId);

    /// <summary>Can a character of this level ACCEPT the quest? (Being mid-quest is unaffected.)</summary>
    public bool LevelInRange(int level) => level >= MinLevel && (MaxLevel <= 0 || level <= MaxLevel);

    /// <summary>The gather lines, never null.</summary>
    public QuestGather[] GatherLines => Gathers ?? Array.Empty<QuestGather>();
}

/// <summary>Per-character progress on a quest (persisted). StepIndex = current
/// step; Counter = kills/collected for that step; Completed when fully done.
///
/// <paramref name="Tracked"/> = pinned to the on-screen tracker. It lives HERE, on the state that is
/// already persisted per character, rather than in a list of its own: the pin has no meaning without
/// the quest it pins, so a quest handed in or abandoned takes its pin with it and nothing has to
/// prune anything (playtest-18 Q1 — the tracker used to be a client-side list that nothing ever
/// wrote anywhere, so it died with the app and was per-INSTALL rather than per character).</summary>
public record CharacterQuestState(string QuestId, int StepIndex, int Counter, bool Completed,
                                  bool Tracked = false);

/// <summary>
/// THE place to manage all quests. Per-quest content is authored in the
/// Quests/ folder (e.g. Quests.HumanMageCleric.cs) which calls Register(...).
/// Mirrors how class skills are organized.
/// </summary>
public static partial class QuestCatalog
{
    private static readonly Dictionary<string, QuestDef> All = new();

    /// <summary>gather token id -> the one quest that owns it (see <see cref="Register"/>).</summary>
    private static readonly Dictionary<string, string> GatherItemOwner =
        new(StringComparer.OrdinalIgnoreCase);

    private static bool _initialized;
    private static void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        RegisterAll();   // implemented across the Quests/ partial files
    }

    static partial void RegisterAll();

    internal static void Register(QuestDef quest)
    {
        if (!All.TryAdd(quest.Id, quest))
            throw new InvalidOperationException($"Duplicate quest id '{quest.Id}'.");
        // A gather token belongs to exactly ONE quest, globally. Two lines of one quest sharing a token
        // would be counted twice at turn-in and paid at whichever modifier came first; two QUESTS
        // sharing one would make abandoning either confiscate the other's progress. Both are silent, so
        // they are refused at startup rather than debugged later.
        foreach (var g in quest.GatherLines)
            if (!GatherItemOwner.TryAdd(g.ItemId, quest.Id))
                throw new InvalidOperationException(
                    $"Quest '{quest.Id}' gathers '{g.ItemId}', which already belongs to "
                    + $"'{GatherItemOwner[g.ItemId]}'. A gather token belongs to one quest.");
    }

    public static QuestDef? Get(string id)
    {
        EnsureInit();
        return id is null ? null : All.GetValueOrDefault(id);
    }

    /// <summary>If <paramref name="questId"/> is a class-change chain quest
    /// ("cc_{classId}_{n}" = tier 2, "tc_{classId}_{n}" = tier 3), returns the
    /// target class id and tier; otherwise (0, 0). Used to enforce that picking a
    /// class commits you to it (other classes' chains stop being offered).</summary>
    public static (int ClassId, int Tier) ClassChainOf(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return (0, 0);
        var parts = questId.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[1], out int id))
        {
            if (parts[0] == "cc") return (id, 2);
            if (parts[0] == "tc") return (id, 3);
        }
        return (0, 0);
    }

    public static IEnumerable<QuestDef> AllQuests
    {
        get { EnsureInit(); return All.Values; }
    }

    /// <summary>One mob template a quest asks the player to KILL, and the level band the step
    /// accepts. See <see cref="KillTargets"/>.</summary>
    public record KillTarget(string MobId, int MinLevel, int MaxLevel);

    private static KillTarget[]? _killTargets;

    /// <summary>Every mob template some quest asks you to KILL, merged across quests to the WIDEST
    /// level band any step accepts.
    ///
    /// This is what the world generator uses to give a quest mob its own dedicated spawner
    /// (<see cref="SpawnZone.Dedicated"/>), so killing one respawns THAT creature instead of rolling
    /// the camp's roster again. Derived rather than hand-listed on purpose: a new kill quest gets its
    /// guaranteed population for free, and a list would silently rot the first time a quest's target
    /// changed (the same reasoning as rosters deriving from the band).</summary>
    public static KillTarget[] KillTargets
    {
        get
        {
            if (_killTargets is not null) return _killTargets;

            var widest = new Dictionary<string, KillTarget>(StringComparer.OrdinalIgnoreCase);

            // A GATHERING quest farms its creature exactly as hard as a kill step does, so its mobs
            // want the same guaranteed spawner — and the same startup warning when a TargetId is
            // misspelt. The band is the creature's own (it has a natural level), expressed here as
            // unbounded: a gather line names a mob, never a level window.
            foreach (var q in AllQuests)
                foreach (var g in q.GatherLines)
                {
                    if (g.MobId.Length == 0) continue;
                    widest[g.MobId] = new KillTarget(g.MobId, 0, 0);
                }

            foreach (var q in AllQuests)
                foreach (var step in q.Steps)
                {
                    if (step.Type != QuestStepType.KillMobs || step.TargetId.Length == 0) continue;
                    // A step with no band (0/0) accepts any level, so it WIDENS to everything: 0 stays 0
                    // as "unbounded" rather than collapsing the band to level zero.
                    if (widest.TryGetValue(step.TargetId, out var cur))
                    {
                        widest[step.TargetId] = cur with
                        {
                            MinLevel = cur.MinLevel == 0 || step.MinLevel == 0
                                ? 0 : Math.Min(cur.MinLevel, step.MinLevel),
                            MaxLevel = cur.MaxLevel == 0 || step.MaxLevel == 0
                                ? 0 : Math.Max(cur.MaxLevel, step.MaxLevel),
                        };
                    }
                    else
                    {
                        widest[step.TargetId] = new KillTarget(step.TargetId, step.MinLevel, step.MaxLevel);
                    }
                }

            return _killTargets = widest.Values.OrderBy(t => t.MobId, StringComparer.Ordinal).ToArray();
        }
    }

    /// <summary>Does any quest ask the player to kill this mob template?</summary>
    public static bool IsKillTarget(string mobId) =>
        KillTargets.Any(t => string.Equals(t.MobId, mobId, StringComparison.OrdinalIgnoreCase));

    /// <summary>Quests a given NPC offers to a character: right level + race +
    /// base class, not yet taken/completed, prerequisite already completed, and
    /// (for pre-class-change quests) no second class yet.</summary>
    public static IEnumerable<QuestDef> OfferedBy(string npcId, int level,
        Race race, BaseClass baseClass, int secondClass, int thirdClass,
        Func<string, bool> isCompleted, Func<string, bool> isActive)
    {
        foreach (var q in AllQuests)
        {
            if (!q.GivenBy(npcId)) continue;
            // LEVEL RANGE, not just a floor (owner): a quest you have outgrown stops being offered
            // rather than sitting in the list for a level-60 to farm. Class quests set no ceiling.
            if (!q.LevelInRange(level)) continue;
            if (q.ForRace is Race r && r != race) continue;
            if (q.ForBaseClass is BaseClass b && b != baseClass) continue;
            if (q.PreClassChange && secondClass != 0) continue;
            // 3rd-class chains: only for the matching 2nd class, and never once a
            // 3rd class is taken (so the other discipline's chain stops being offered).
            if (q.ForSecondClass is int sc && (sc != secondClass || thirdClass != 0)) continue;
            // A DAILY is never permanently "completed" — the caller's isCompleted answers for today's
            // stamp instead, so the quest reappears when the server day rolls over.
            if (isActive(q.Id)) continue;
            if (isCompleted(q.Id)) continue;
            if (q.RequiresQuestId is not null && !isCompleted(q.RequiresQuestId)) continue;
            yield return q;
        }
    }
}

/// <summary>
/// Item-gated class changes. To become a class, the character must HOLD the
/// listed quest items (and meet the level); the NPC consumes them on change.
/// Different target class = different required items = different chain.
/// Authored in Quests/ClassChanges.cs.
/// </summary>
public static partial class ClassChangeRequirements
{
    // TargetClassId is the class you BECOME (a 2nd-class id for Tier 2, a 3rd-class
    // id for Tier 3). RequiredCurrentClass gates Tier 3: you must already hold that
    // 2nd class. (Field kept named SecondClassId for back-compat with the DTO.)
    public record Requirement(int SecondClassId, string ClassName, int MinLevel,
        string[] RequiredItemIds, string NpcId,
        int Tier = 2, int RequiredCurrentClass = 0);

    private static readonly List<Requirement> All = new();

    private static bool _initialized;
    private static void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        RegisterAll();
    }

    static partial void RegisterAll();

    internal static void Register(Requirement req) => All.Add(req);

    /// <summary>Class changes this NPC can perform.</summary>
    public static IEnumerable<Requirement> AtNpc(string npcId)
    {
        EnsureInit();
        return All.Where(r => r.NpcId == npcId);
    }

    public static Requirement? ForClass(int secondClassId)
    {
        EnsureInit();
        return All.FirstOrDefault(r => r.SecondClassId == secondClassId);
    }
}
