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

    // (firstMob, secondMob) the two quests send you to hunt, by archetype. mobA is killed
    // at Lv 16-20, mobB at Lv 17-21 — both must spawn in-band at the Stonewatch grounds:
    // orc_archer(16), skeleton_grunt(18), shield_skeleton(20). (mobB must be ≥17, so never
    // the level-16 orc_archer.) The archetype variety is just which you hunt first.
    private static (string A, string B) HuntTargets(Archetype a) => a switch
    {
        Archetype.Tank => ("skeleton_grunt", "shield_skeleton"),
        Archetype.Warrior => ("orc_archer", "skeleton_grunt"),
        Archetype.Rogue => ("orc_archer", "skeleton_grunt"),
        Archetype.Archer => ("orc_archer", "skeleton_grunt"),
        Archetype.Healer => ("skeleton_grunt", "shield_skeleton"),
        Archetype.Nuker => ("skeleton_grunt", "shield_skeleton"),
        _ => ("skeleton_grunt", "shield_skeleton"),
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

    // The 3rd-class chain hunts mobs around the 35/37/39 quest levels; each target's natural
    // level sits inside its kill-step band, and all spawn in the Emberfall/Greymarsh grounds.
    private const string Mob3A = "medusa";                 // Ordeal I  — Lv 34, band 30-40
    private const string Mob3B = "marsh_mantis_soldier";   // Ordeal II — Lv 37, band 32-42
    private const string Mob3C = "fen_lizardman_archer";   // Ascension — Lv 39, band 35-45

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

            // Q1 (lvl 35) — Ordeal I: Mob3A (medusa), level band 30-40.
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

            // Q2 (lvl 37) — Ordeal II: Mob3B (marsh_mantis_soldier), band 32-42. Gate only (no
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

            // Q3 (lvl 39) — Ascension: Mob3C (fen_lizardman_archer), band 35-45. Awards the proof
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

    /// <summary>The 4th class (level 76). Unlike tiers 2 and 3 there is no quest chain and no
    /// per-class token: ONE bought item, the Rite of Ascension, is the whole gate for now (owner,
    /// 2026-08-17). The long chain replaces the purchase later; this registration survives it
    /// unchanged, because a requirement only asks what you HOLD.
    ///
    /// <para>RequiredCurrentClass is the parent THIRD class here — the field is tier-relative
    /// ("the class you must already be"), which is why it is not named RequiredSecondClass.</para></summary>
    static partial void RegisterFourthClassRequirements()
    {
        foreach (var fc in FourthClassCatalog.Playable)
        {
            Register(new Requirement(
                SecondClassId: fc.Id,                 // target = the 4th-class id (201-236)
                ClassName: fc.Name,
                MinLevel: FourthClassCatalog.ChangeLevel,
                RequiredItemIds: new[] { ItemCatalog.FourthClassKey },
                NpcId: "master_class4",
                Tier: 4,
                RequiredCurrentClass: fc.ParentThirdClassId));
        }
    }
}
