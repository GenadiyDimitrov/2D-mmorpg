namespace Game.Shared;

/// <summary>
/// The STARTER chain: the level-10 quest that hands over the Newbie kit.
///
/// A new character used to be given the Newbie set outright, and it was strong enough to one-shot the
/// first zones (owner, playtest-11). It starts in the feeble TRAINING tier instead, and this chain is
/// how the Newbie gear is earned. The owner's intent: *"the newbie boxes must be a quest at lvl 10
/// (levelling while doing it to ~15 — to help getting to level 30 and making money or getting better
/// drops)"*, so the chain deliberately spans levels 10→15 rather than being a single hand-in.
///
/// Two parts, so the reward arrives in two useful pieces rather than one lump at the end:
///   1. "A Proper Kit"  (lvl 10) — kill Ashen Wolves, come back → the ARMOR + WEAPON boxes.
///   2. "Blooded"       (lvl 12) — kill Werewolves and REACH LEVEL 15 → the JEWELS + shot rune.
/// Part 2 gates on part 1, so the order is fixed and the second half cannot be skipped.
/// </summary>
public static partial class QuestCatalog
{
    /// <summary>Armsmaster Dolan gives it — the gear vendor handing out gear needs no explaining, and a
    /// new player is already walking to him to spend their first gold on training kit.</summary>
    private const string StarterGiver = "merchant_gear";

    public const string QuestStarterKit = "starter_kit";
    public const string QuestStarterBlooded = "starter_blooded";

    static partial void RegisterStarterChain()
    {
        Register(new QuestDef(
            Id: QuestStarterKit,
            Name: "A Proper Kit",
            Description: "Armsmaster Dolan has looked over your training gear and been polite about it. "
                       + "Thin out the wolves on the road and he will see you properly equipped.",
            OfferNpcId: StarterGiver,
            MinLevel: 10,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo, "Speak with Armsmaster Dolan", TargetId: StarterGiver),
                new QuestStep(QuestStepType.KillMobs, "Slay 10 Ashen Wolves (Lv 8-14)",
                    TargetId: "ashen_wolf", Count: 10, MinLevel: 8, MaxLevel: 14),
                new QuestStep(QuestStepType.TalkTo, "Return to Armsmaster Dolan", TargetId: StarterGiver),
            },
            // The armor + weapon halves of the old starter kit. Both are SELECTION boxes, so the reward
            // stays class-agnostic exactly as the creation kit is.
            Reward: new QuestReward(Exp: 12000, SkillPoints: 2, ItemIds: new[]
            {
                ItemCatalog.BoxNewbieArmorChoice,
                ItemCatalog.BoxNewbieWeapons,
            })));

        Register(new QuestDef(
            Id: QuestStarterBlooded,
            Name: "Blooded",
            Description: "New gear is worth little until it has been used. Take the werewolves, "
                       + "grow into your equipment, and Dolan will finish the set.",
            OfferNpcId: StarterGiver,
            MinLevel: 12,
            RequiresQuestId: QuestStarterKit,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 15 Werewolves (Lv 10-16)",
                    TargetId: "werewolf", Count: 15, MinLevel: 10, MaxLevel: 16),
                // The step that makes this a JOURNEY rather than an errand — the owner wants the chain
                // to carry the player to ~15, so ask for it outright instead of hoping the kills do it.
                new QuestStep(QuestStepType.ReachLevel, "Reach level 15", Count: 15),
                new QuestStep(QuestStepType.TalkTo, "Return to Armsmaster Dolan", TargetId: StarterGiver),
            },
            // The jewels and the shot rune — the two things deliberately REMOVED from character
            // creation. Reaching them is the point of the chain.
            Reward: new QuestReward(Exp: 30000, SkillPoints: 3, ItemIds: new[]
            {
                ItemCatalog.BoxNewbieJewels,
                ItemCatalog.BoxNewbieRuneChoice,
            })));
    }
}
