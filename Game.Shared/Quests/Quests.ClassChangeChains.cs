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
                             $"path of the {cls.Name}. Prove yourself in the field.\n\n" +
                             ClassCatalog.ArchetypeBlurb(cls.Archetype) +
                             "\n(Once you begin a class path, the others close to you — choose well.)",
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

    private const string GrandGiver = "master_class3";

    // Three higher-tier targets per archetype (level band 40-80). The final target
    // is the Young Drake — a stepping stone to real boss kills once bosses exist.
    private static (string A, string B, string C) HuntTargets3(Archetype a) => a switch
    {
        Archetype.Tank    => ("stone_golem", "orc_raider", "young_drake"),
        Archetype.Warrior => ("orc_raider", "stone_golem", "young_drake"),
        Archetype.Rogue   => ("wraith", "orc_raider", "young_drake"),
        Archetype.Archer  => ("orc_raider", "wraith", "young_drake"),
        Archetype.Healer  => ("wraith", "stone_golem", "young_drake"),
        Archetype.Nuker   => ("wraith", "orc_raider", "young_drake"),
        _ => ("orc_raider", "wraith", "young_drake"),
    };

    static partial void RegisterThirdClassChains()
    {
        const int lvl = ThirdClassCatalog.ChangeLevel;   // 40
        foreach (var tc in ThirdClassCatalog.Playable)
        {
            var parent = Disciplines.Parent(tc.Discipline);
            var (mobA, mobB, mobC) = HuntTargets3(parent);
            string aName = MobCatalog.Get(mobA).Name;
            string bName = MobCatalog.Get(mobB).Name;
            string cName = MobCatalog.Get(mobC).Name;
            string q1 = $"tc_{tc.Id}_1";
            string q2 = $"tc_{tc.Id}_2";

            // Q1 — the Ordeal: two stiff hunts, longer than the 2nd-class trial.
            Register(new QuestDef(
                Id: q1,
                Name: $"Ordeal of the {tc.Name}",
                Description: $"Grandmaster Thorne sets the ordeal that opens the {tc.Name} " +
                             $"discipline. Only the proven may walk it.\n\n" +
                             ClassCatalog.ArchetypeBlurb(parent) +
                             "\n(Choosing a discipline is final — the other path will close.)",
                OfferNpcId: GrandGiver,
                MinLevel: lvl,
                ForSecondClass: tc.ParentSecondClassId,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Grandmaster Thorne", TargetId: GrandGiver),
                    new QuestStep(KillMobs, $"Slay 12 {aName}", TargetId: mobA,
                        Count: 12, MinLevel: lvl, MaxLevel: 80),
                    new QuestStep(KillMobs, $"Slay 10 {bName}", TargetId: mobB,
                        Count: 10, MinLevel: lvl, MaxLevel: 80),
                    new QuestStep(TalkTo, "Return to Grandmaster Thorne", TargetId: GrandGiver),
                },
                Reward: new QuestReward(Exp: 6000, SkillPoints: 3,
                    ItemIds: new[] { ItemCatalog.ClassTokenId(tc.Id) })));

            // Q2 — the Ascension: a harder hunt capped by a Young Drake kill.
            Register(new QuestDef(
                Id: q2,
                Name: $"Ascension of the {tc.Name}",
                Description: $"Bearing the {tc.Name} Ordeal Mark, complete the ascension — " +
                             $"the final trial of the {tc.Name} discipline.",
                OfferNpcId: GrandGiver,
                MinLevel: lvl,
                RequiresQuestId: q1,
                ForSecondClass: tc.ParentSecondClassId,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Grandmaster Thorne", TargetId: GrandGiver),
                    new QuestStep(KillMobs, $"Slay 15 {bName}", TargetId: mobB,
                        Count: 15, MinLevel: lvl, MaxLevel: 80),
                    new QuestStep(KillMobs, $"Slay 3 {cName}", TargetId: mobC,
                        Count: 3, MinLevel: lvl, MaxLevel: 80),
                    new QuestStep(TalkTo, "Return to Grandmaster Thorne", TargetId: GrandGiver),
                },
                Reward: new QuestReward(Exp: 12000, SkillPoints: 4,
                    ItemIds: new[] { ItemCatalog.ClassProofId(tc.Id) })));
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

    static partial void RegisterThirdClassRequirements()
    {
        foreach (var tc in ThirdClassCatalog.Playable)
        {
            Register(new Requirement(
                SecondClassId: tc.Id,                 // target = the 3rd class id (101-136)
                ClassName: tc.Name,
                MinLevel: ThirdClassCatalog.ChangeLevel,
                RequiredItemIds: new[]
                {
                    ItemCatalog.ClassTokenId(tc.Id),
                    ItemCatalog.ClassProofId(tc.Id),
                },
                NpcId: "master_class3",
                Tier: 3,
                RequiredCurrentClass: tc.ParentSecondClassId));
        }
    }
}
