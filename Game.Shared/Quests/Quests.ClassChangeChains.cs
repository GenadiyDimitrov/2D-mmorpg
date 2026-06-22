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

    // (firstMob, secondMob) the two quests send you to hunt, by archetype. Both must
    // be mobs that actually spawn at the quest's level band (16-21) — only dire_boar
    // and road_bandit do (the Stonewatch grounds), so the variety is which you hunt
    // first. (The old flavour mobs cap at lvl 7-15 and made an 18-20 quest trivial.)
    private static (string A, string B) HuntTargets(Archetype a) => a switch
    {
        Archetype.Tank => ("dire_boar", "road_bandit"),
        Archetype.Warrior => ("road_bandit", "dire_boar"),
        Archetype.Rogue => ("road_bandit", "dire_boar"),
        Archetype.Archer => ("road_bandit", "dire_boar"),
        Archetype.Healer => ("dire_boar", "road_bandit"),
        Archetype.Nuker => ("dire_boar", "road_bandit"),
        _ => ("dire_boar", "road_bandit"),
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
                    new QuestStep(KillMobs, $"Slay 5 {mobAName} (Lv 16-20)", TargetId: mobA,
                        Count: 5, MinLevel: 16, MaxLevel: 20),
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
                MinLevel: 19,
                RequiresQuestId: q1,
                ForRace: cls.Race,
                ForBaseClass: cls.Base,
                PreClassChange: true,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with High Priest Oren", TargetId: PathGiver),
                    new QuestStep(KillMobs, $"Slay 8 {mobBName} (Lv 17-21)", TargetId: mobB,
                        Count: 8, MinLevel: 17, MaxLevel: 21),
                    new QuestStep(TalkTo, "Return to High Priest Oren", TargetId: PathGiver),
                },
                Reward: new QuestReward(Exp: 800, SkillPoints: 2,
                    ItemIds: new[] { ItemCatalog.ClassProofId(cls.Id) })));
        }
    }

    private const string GrandGiver = "master_class3";

    // The 3rd-class chain hunts mobs in the 30-45 band (around the 35/37/39 quest
    // levels): orc_raider spawns 28-40, stone_golem 34-46 — both reachable. (wraith
    // starts at 40 and young_drake at 52, so they'd force you above your level; they
    // move to later boss/high-level content instead.)
    private const string Mob3A = "orc_raider";    // Ordeal I — band 30-40
    private const string Mob3B = "stone_golem";   // Ordeal II — band 32-42
    private const string Mob3C = "stone_golem";   // Ascension — band 35-45

    static partial void RegisterThirdClassChains()
    {
        foreach (var tc in ThirdClassCatalog.Playable)
        {
            var parent = Disciplines.Parent(tc.Discipline);
            string aName = MobCatalog.Get(Mob3A).Name;
            string bName = MobCatalog.Get(Mob3B).Name;
            string cName = MobCatalog.Get(Mob3C).Name;
            string q1 = $"tc_{tc.Id}_1";
            string q2 = $"tc_{tc.Id}_2";
            string q3 = $"tc_{tc.Id}_3";

            // Q1 (lvl 35) — Ordeal I: orc_raider, level band 30-40.
            Register(new QuestDef(
                Id: q1,
                Name: $"Ordeal of the {tc.Name}",
                Description: $"Grandmaster Thorne sets the ordeal that opens the {tc.Name} " +
                             $"discipline. Only the proven may walk it.\n\n" +
                             ClassCatalog.ArchetypeBlurb(parent) +
                             "\n(Choosing a discipline is final — the other path will close.)",
                OfferNpcId: GrandGiver,
                MinLevel: 35,
                ForSecondClass: tc.ParentSecondClassId,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Grandmaster Thorne", TargetId: GrandGiver),
                    new QuestStep(KillMobs, $"Slay 12 {aName} (Lv 30-40)", TargetId: Mob3A,
                        Count: 12, MinLevel: 30, MaxLevel: 40),
                    new QuestStep(TalkTo, "Return to Grandmaster Thorne", TargetId: GrandGiver),
                },
                Reward: new QuestReward(Exp: 6000, SkillPoints: 2,
                    ItemIds: new[] { ItemCatalog.ClassTokenId(tc.Id) })));

            // Q2 (lvl 37) — Ordeal II: stone_golem, level band 32-42. Gate only (no
            // item); the chain order forces it before the Ascension.
            Register(new QuestDef(
                Id: q2,
                Name: $"Trial of the {tc.Name}",
                Description: $"The {tc.Name} ordeal continues — harder quarry, deeper into " +
                             $"the highlands.",
                OfferNpcId: GrandGiver,
                MinLevel: 37,
                RequiresQuestId: q1,
                ForSecondClass: tc.ParentSecondClassId,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Grandmaster Thorne", TargetId: GrandGiver),
                    new QuestStep(KillMobs, $"Slay 12 {bName} (Lv 32-42)", TargetId: Mob3B,
                        Count: 12, MinLevel: 32, MaxLevel: 42),
                    new QuestStep(TalkTo, "Return to Grandmaster Thorne", TargetId: GrandGiver),
                },
                Reward: new QuestReward(Exp: 9000, SkillPoints: 3)));

            // Q3 (lvl 39) — Ascension: stone_golem, level band 35-45. Awards the proof
            // that (with the token from Q1) lets Grandmaster Thorne perform the change.
            Register(new QuestDef(
                Id: q3,
                Name: $"Ascension of the {tc.Name}",
                Description: $"Bearing the {tc.Name} marks, complete the ascension — the " +
                             $"final trial of the {tc.Name} discipline.",
                OfferNpcId: GrandGiver,
                MinLevel: 39,
                RequiresQuestId: q2,
                ForSecondClass: tc.ParentSecondClassId,
                Steps: new[]
                {
                    new QuestStep(TalkTo, "Speak with Grandmaster Thorne", TargetId: GrandGiver),
                    new QuestStep(KillMobs, $"Slay 15 {cName} (Lv 35-45)", TargetId: Mob3C,
                        Count: 15, MinLevel: 35, MaxLevel: 45),
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
