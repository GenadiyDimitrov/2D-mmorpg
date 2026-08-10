namespace Game.Shared;

/// <summary>
/// THE TUTORIAL CHAIN — playtest-19 <b>M5</b>, the owner's own 15-beat outline.
///
/// <para><b>The point is not the kills. It is meeting every NPC in town</b>, and each step says what
/// that NPC is FOR. His outline, verbatim in <c>docs/testing/Playtest-Archive.md</c>:</para>
///
/// <code>
/// 1  Gatekeeper Pell    — free teleports until 40
/// 2  kill 5 pigs        -> level 3
/// 3  Huntmaster Cera    — repeatable hunt contracts (take one)
/// 4  kill 5 foxes       -> level 6
/// 5  Spirit Helper Nyra — support magic 6-75 (take the buff)
/// 6  Apothecary Miren   — potions/scrolls + the free daily Rune (take it)
/// 7  kill X goblin riders -> level 10
/// 8  Armsmaster Dolan   — the NEWBIE EQUIPMENT as the reward
/// 9-10  reach 15 -> Dolan again for the 1-day rune + jewel box
/// 11-15 reach 18 -> Elder Marius, 19 -> High Priest Oren, 20 -> Class Master Vael
/// </code>
///
/// <para><b>⚠ THE STRUCTURAL RULE HE STATED.</b> *"The 3 class quests can be taken withouth the
/// chain / the chain is only to meet the NPCs / U just can lvl up to 20 go do the 3 quests and done..
/// / The chain is for the newebie equipment an the end reward."* So the chain <b>WRAPS</b> the three
/// existing class quests, the Apothecary's daily rune and the Huntmaster's contracts — it points at
/// them, and it must never gate them. That is why parts 2 and 5 use plain <c>TalkTo</c> steps at
/// those NPCs: talking is all the chain asks, and whether you also take what they offer is your
/// business. Nothing here has a <c>RequiresQuestId</c> pointing at a class quest, and no class quest
/// requires anything here.</para>
///
/// <para><b>Why five quests and not one 15-step quest.</b> A <see cref="QuestReward"/> pays out on
/// COMPLETION, and his outline pays three times along the way (the gear at 8, the rune + jewels at
/// 9-10, the finishing kit at 15). Each payout therefore has to end a quest. The parts chain with
/// <c>RequiresQuestId</c>, so the order is still fixed and no half can be skipped.</para>
///
/// <para><b>This chain REPLACES the old starter chain</b> (<c>starter_kit</c> / <c>starter_blooded</c>,
/// deleted with <c>Quests.Starter.cs</c>). Those two were the same errand — Dolan handing over the
/// Newbie kit at 10 and finishing it at 15 — and keeping both would have paid a new character two
/// full kits. Parts 3 and 4 below ARE those quests, with the NPC-meeting beats built around them and
/// their tested pacing (12 000 exp ≈ 52% of level 10, 30 000 ≈ 39% of level 15) carried over intact.
/// The werewolf hunt is kept in part 4 for the same reason: it is the 122 mobs that actually carry a
/// player from 12 to 15, which his "reach 15" beat assumes but does not supply.</para>
///
/// <para><b>M6 — the Newbie kit is a LOANER.</b> Parts 3 and 4 hand out the BOUND, 30-day copies
/// (<c>ItemCatalog.BoundCopies</c>), not the ladder gear: unsellable, untradable, destroyable,
/// gone after 30 days.</para>
///
/// <para><b>`58a` — it now TEACHES as well as introduces.</b> The owner's complaint after playing it
/// was that the chain met every NPC in town and *"teaches nothing before the pigs"*: nothing said how
/// to open a bag, open a box, put the contents on, swing at something, or turn auto-farm on. Part 1
/// gains three <see cref="QuestStepType.DoAction"/> beats — open a box, equip twice, use a skill — and
/// part 5 gains the auto-farm one, at the *"reach 18"* slot he picked for it. Each is proved by DOING
/// it, using commands the server already handles, so none of them is a tooltip that ticks itself.
///
/// ⚠ A DoAction step is a GATE, which is why the box beat asks for ONE box and the rune explanation
/// stayed prose: asking for two boxes would strand a player who opened their creation boxes before
/// walking to Cera, and an "open Miren's rune box" step would gate the chain on her DAILY quest —
/// exactly what the structural rule above forbids.</para>
/// </summary>
public static partial class QuestCatalog
{
    // The cast, in the order a new character meets them. Ids from WorldMap's Brackenford roster.
    private const string NpcGatekeeper = "gatekeeper_brackenford";   // Gatekeeper Pell
    private const string NpcHuntmaster = "hunter_brackenford";       // Huntmaster Cera
    private const string NpcBuffer     = "buffer_newbie";            // Spirit Helper Nyra
    private const string NpcApothecary = "merchant_potions";         // Apothecary Miren
    private const string NpcArmsmaster = "merchant_gear";            // Armsmaster Dolan
    private const string NpcElder      = "elder_marius";             // Elder Marius
    private const string NpcPriest     = "priest_oren";              // High Priest Oren
    private const string NpcClassMaster = "master_class";            // Class Master Vael

    public const string QuestTutorialWelcome = "tutorial_welcome";
    public const string QuestTutorialBlessing = "tutorial_blessing";
    public const string QuestTutorialArmed = "tutorial_armed";
    public const string QuestTutorialBlooded = "tutorial_blooded";
    public const string QuestTutorialTrade = "tutorial_trade";

    static partial void RegisterTutorialChain()
    {
        // ---- PART 1 (beats 1-3): Pell, five pups, level 3, back to Cera. -------------------------
        // Cera GIVES it rather than Pell: beat 1 is "go and meet the gatekeeper", and a quest whose
        // first step is "talk to the man who just gave it to you" reads as a bug. She stands beside
        // him at top-centre, so the walk is the same one.
        // A CEILING of 20 (the only one in the chain): this is the tutorial, and a level-60 alt has
        // no business farming it. Parts 2-5 are uncapped so that out-levelling it mid-chain — which a
        // player who wanders off and hunts will do — can never strand them halfway.
        Register(new QuestDef(
            Id: QuestTutorialWelcome,
            Name: "Welcome, Traveller",
            Description: "Huntmaster Cera looks you over. \"New, then. Go and say hello to Pell at the "
                       + "gate first — he'll move you between the towns for nothing until you're 40, and "
                       + "you'll want that. Then blood yourself on the pups down the road and come back.\"",
            OfferNpcId: NpcHuntmaster,
            MinLevel: 1,
            MaxLevel: 20,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Speak with Gatekeeper Pell — he teleports you between towns, free until level 40",
                    TargetId: NpcGatekeeper),
                // ---- `58a`: TEACH, before the pigs. ------------------------------------------------
                // The chain introduced every NPC in town and never once said how to open a bag, open a
                // box, put the contents on, or swing at anything (owner, playtest-20: *"teaches nothing
                // before the pigs"*). These three beats are the missing half, and each is proved by
                // DOING it — the server sees every one of them already.
                // ONE box, not both. A DoAction step is a gate, and a player who opened their creation
                // boxes before walking to Cera cannot conjure a second one — asking for two would strand
                // exactly the eager player this is written for. One still teaches the gesture, and a
                // fresh character always has two.
                new QuestStep(QuestStepType.DoAction,
                    "Open your bag and open a box from it — tap the box to open it",
                    TargetId: QuestActions.OpenBox),
                new QuestStep(QuestStepType.DoAction,
                    "Put your gear on — tap a weapon or an armor piece in the bag to equip it",
                    TargetId: QuestActions.EquipItem, Count: 2),
                new QuestStep(QuestStepType.DoAction,
                    "Try a skill from the bar at the bottom — tapping a creature attacks it, a skill uses that",
                    TargetId: QuestActions.UseSkill),
                new QuestStep(QuestStepType.KillMobs, "Slay 5 Ridgeback Pups",
                    TargetId: "ridgeback_pup", Count: 5),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 3", Count: 3),
                new QuestStep(QuestStepType.TalkTo,
                    "Return to Huntmaster Cera — her hunting contracts pay gold and exp, over and over",
                    TargetId: NpcHuntmaster),
            },
            // ~50% of a level at 3 (level 3->4 costs 805), plus enough gold to replace a training
            // weapon (400g each) if the pups went badly.
            Reward: new QuestReward(Exp: 400, Gold: 500, SkillPoints: 1)));

        // ---- PART 2 (beats 4-6): five foxes, level 6, Nyra, Miren. -------------------------------
        // Level 6 is not an arbitrary target — it is the floor of Nyra's buff band (6-75), so the
        // chain sends you to her on the first level she will actually bless you.
        Register(new QuestDef(
            Id: QuestTutorialBlessing,
            Name: "Blessings and Bottles",
            Description: "\"Take a contract if you want one — I'll be here. But there are two more you "
                       + "should know. Nyra blesses anyone from six to seventy-five, and Miren keeps the "
                       + "potions. Thin out the foxes on the way.\"",
            OfferNpcId: NpcHuntmaster,
            MinLevel: 3,
            RequiresQuestId: QuestTutorialWelcome,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 5 Foxes", TargetId: "fox", Count: 5),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 6", Count: 6),
                new QuestStep(QuestStepType.TalkTo,
                    "Visit Spirit Helper Nyra and take her blessing — she buffs levels 6 to 75, free",
                    TargetId: NpcBuffer),
                // "what the rune does (after Miren)" — his `58a` list. Said in the TEXT, not made into a
                // step you must perform. ⚠ The rune comes from Miren's DAILY quest, and the structural
                // rule at the top of this file is that the chain points at the daily and must never gate
                // it; an "open her rune box" action step would have done exactly that to anyone who
                // skipped the daily.
                new QuestStep(QuestStepType.TalkTo,
                    "Visit Apothecary Miren — potions, scrolls, and a free Rune daily. A rune is worn like "
                  + "a jewel and raises your damage until it expires; take today's",
                    TargetId: NpcApothecary),
            },
            // ~50% of a level at 6 (level 6->7 costs 5 249).
            Reward: new QuestReward(Exp: 2600, Gold: 1500, SkillPoints: 1)));

        // ---- PART 3 (beats 7-8): goblins, level 10, the NEWBIE KIT from Dolan. -------------------
        // This is the old "A Proper Kit" (starter_kit), rebuilt onto his goblin beat. Same giver,
        // same level, same reward, same exp — the boxes now hold the BOUND 30-day copies (M6).
        Register(new QuestDef(
            Id: QuestTutorialArmed,
            Name: "Properly Armed",
            Description: "Armsmaster Dolan has looked over your training gear and been polite about it. "
                       + "\"Clear the goblin scouts off the road and come back at ten. I'll see you "
                       + "properly equipped — for a month, anyway. After that you earn your own.\"",
            OfferNpcId: NpcArmsmaster,
            MinLevel: 6,
            RequiresQuestId: QuestTutorialBlessing,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 10 Goblin Scouts (Lv 6-12)",
                    TargetId: "goblin_scout", Count: 10, MinLevel: 6, MaxLevel: 12),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 10", Count: 10),
                new QuestStep(QuestStepType.TalkTo,
                    "Return to Armsmaster Dolan — he buys and sells weapons, and he owes you a kit",
                    TargetId: NpcArmsmaster),
            },
            // The armor + weapon halves. Both are SELECTION boxes, so the reward stays class-agnostic
            // exactly as the creation kit is.
            Reward: new QuestReward(Exp: 12000, SkillPoints: 2, ItemIds: new[]
            {
                ItemCatalog.BoxNewbieArmorChoice,
                ItemCatalog.BoxNewbieWeapons,
            })));

        // ---- PART 4 (beats 9-10): reach 15, back to Dolan for the rune + jewels. -----------------
        // The old "Blooded" (starter_blooded), unchanged but for its prerequisite.
        Register(new QuestDef(
            Id: QuestTutorialBlooded,
            Name: "Blooded",
            Description: "New gear is worth little until it has been used. Take the werewolves, grow "
                       + "into your equipment, and Dolan will finish the set.",
            OfferNpcId: NpcArmsmaster,
            MinLevel: 10,
            RequiresQuestId: QuestTutorialArmed,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 15 Werewolves (Lv 10-16)",
                    TargetId: "werewolf", Count: 15, MinLevel: 10, MaxLevel: 16),
                // The step that makes this a JOURNEY rather than an errand — the chain has to carry
                // the player to 15, so ask for it outright instead of hoping the kills do it.
                new QuestStep(QuestStepType.ReachLevel, "Reach level 15", Count: 15),
                new QuestStep(QuestStepType.TalkTo, "Return to Armsmaster Dolan", TargetId: NpcArmsmaster),
            },
            // The jewels and the 1-day rune — the two things deliberately REMOVED from character
            // creation. Reaching them is the point of the chain.
            Reward: new QuestReward(Exp: 30000, SkillPoints: 3, ItemIds: new[]
            {
                ItemCatalog.BoxNewbieJewels,
                ItemCatalog.BoxNewbieRuneChoice,
            })));

        // ---- PART 5 (beats 11-15): 18 -> Marius, 19 -> Oren, 20 -> Vael. -------------------------
        // ⚠ The three TalkTo steps POINT at the class quest givers; they do not gate them. Marius,
        // Oren and Vael offer their own quests on their own terms, chain or no chain — a player who
        // ignored all of this and walked to them at 20 gets exactly the same three quests. All this
        // part adds is that they know where those three stand.
        Register(new QuestDef(
            Id: QuestTutorialTrade,
            Name: "A Trade to Learn",
            Description: "\"Fifteen. Good. From here it's a profession you want, not a bigger sword. "
                       + "Elder Marius sets the first trial, High Priest Oren the second, and Class "
                       + "Master Vael makes it official. Go and be introduced — they'll want to see you "
                       + "grow into each one.\"",
            OfferNpcId: NpcArmsmaster,
            MinLevel: 15,
            RequiresQuestId: QuestTutorialBlooded,
            Steps: new[]
            {
                // The auto-pot / auto-farm beat goes HERE — his `58a` words: *"the 'reach 18' part after
                // Dorian is the right slot for those"*. It is: by 18 you own potions, a full bar and a
                // reason to leave the phone alone for a while.
                new QuestStep(QuestStepType.DoAction,
                    "Open Auto and switch auto-farm on — it fights and drinks for you while you are away",
                    TargetId: QuestActions.AutoHunt),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 18", Count: 18),
                new QuestStep(QuestStepType.TalkTo,
                    "Speak with Elder Marius — he sets the first trial of your profession",
                    TargetId: NpcElder),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 19", Count: 19),
                new QuestStep(QuestStepType.TalkTo,
                    "Speak with High Priest Oren — the second trial is his",
                    TargetId: NpcPriest),
                new QuestStep(QuestStepType.ReachLevel, "Reach level 20", Count: 20),
                new QuestStep(QuestStepType.TalkTo,
                    "Speak with Class Master Vael — he performs the class change itself",
                    TargetId: NpcClassMaster),
            },
            // His completion kit, every piece BOUND (M5): "x1 Ultimate Scroll of Escape, x1 Ultimate
            // Scroll of Resurrection, x5 Mythic Dash Potion, x5 Instant Health Potion — every one
            // untradable/unsellable". QuestReward carries no quantities, so a stack of five is five
            // entries; they are stackable consumables and merge into one stack on the way in.
            // ~50% of a level at 20 (level 20->21 costs 187 922).
            Reward: new QuestReward(Exp: 90000, SkillPoints: 5, Gold: 20000, ItemIds: new[]
            {
                ItemCatalog.ScrollReturnUltimateBound,
                ItemCatalog.ScrollResurrectUltimateBound,
                ItemCatalog.DashPotionMBound,
                ItemCatalog.DashPotionMBound,
                ItemCatalog.DashPotionMBound,
                ItemCatalog.DashPotionMBound,
                ItemCatalog.DashPotionMBound,
                ItemCatalog.InstantPotionBound,
                ItemCatalog.InstantPotionBound,
                ItemCatalog.InstantPotionBound,
                ItemCatalog.InstantPotionBound,
                ItemCatalog.InstantPotionBound,
            })));
    }
}
