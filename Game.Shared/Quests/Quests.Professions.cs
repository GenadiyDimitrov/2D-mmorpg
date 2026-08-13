namespace Game.Shared;

/// <summary>
/// The five CRAFTING PROFESSION joining quests (`BL-05`, owner's playtest-21 `66a`).
///
/// <para>*"U go to the 'Master apothecary Roger' or watever → U accept a quest → he explains what a
/// apothecery can craft and other means of aquirering the items(potions) like drop/bosses etc to lure
/// you so u become one of his aprenteces.. → u compleate the quest and u can take his proffesion."*</para>
///
/// <para>🔑 <b>The quest IS the explainer.</b> Its step text is where "here is what this profession
/// makes, and here is how else you could get those items" lives, because the moment a player reads it is
/// the moment they are choosing between five one-way doors. That is why each chain is three beats and
/// not one: talk (the pitch), gather (you do the job once, with your own hands), talk (you are hired).
/// A single TalkTo would be a menu with a cutscene, which is what he asked this not to be.</para>
///
/// <para>🔑 <b>Gated at character level 20</b> — the same gate as L1 crafting itself. A profession you
/// cannot use for twenty levels is a trap, and taking one before seeing the other four is exactly the
/// bad commitment the quit flow exists to undo.</para>
///
/// <para>🔑 <b>AnyTownNpc</b>: every town's copy of a master offers and takes his own quest. Same reason
/// as the hunting contracts (playtest-19 M11 — *"i have no way to go back to the 1st town just to take
/// it"*), and it is one quest id, so it still cannot be run twice.</para>
///
/// <para>⚠ <b>The quest is remembered forever; the LEVELS are not.</b> His 2026-08-12 ruling — *"Skip the
/// quest if it's once done, but still lose levels if switching. Like a mix from both."* — is implemented
/// in two different places on purpose. This file is only the first half: the quest is an ordinary quest,
/// so <c>CompletedQuests</c> already persists it and a returning apprentice is re-hired on the spot with
/// no chain to redo. The second half is <c>HandleQuitProfession</c>, which zeroes <c>CraftExp</c> every
/// single time. The quest is knowledge and you cannot un-know it; the levels are practice and you lose
/// those by walking away.</para>
/// </summary>
public static partial class QuestCatalog
{
    public const string QuestJoinWeaponSmith  = "join_weaponsmith";
    public const string QuestJoinArmorSmith   = "join_armorsmith";
    public const string QuestJoinJeweler      = "join_jeweler";
    public const string QuestJoinPotionMaster = "join_potionmaster";
    public const string QuestJoinScrollScribe = "join_scribe";

    /// <summary>The joining quest id for a profession, or null. The server grants the profession when
    /// THIS quest completes, so the two tables must not drift — they are keyed off the same enum.</summary>
    public static string? JoiningQuestFor(Profession p) => p switch
    {
        Profession.WeaponSmith   => QuestJoinWeaponSmith,
        Profession.ArmorSmith    => QuestJoinArmorSmith,
        Profession.Jeweler       => QuestJoinJeweler,
        Profession.PotionMaster  => QuestJoinPotionMaster,
        Profession.ScrollScribe  => QuestJoinScrollScribe,
        _ => null,
    };

    /// <summary>The profession a completed joining quest grants, or None.</summary>
    public static Profession ProfessionGrantedBy(string questId) => questId switch
    {
        QuestJoinWeaponSmith  => Profession.WeaponSmith,
        QuestJoinArmorSmith   => Profession.ArmorSmith,
        QuestJoinJeweler      => Profession.Jeweler,
        QuestJoinPotionMaster => Profession.PotionMaster,
        QuestJoinScrollScribe => Profession.ScrollScribe,
        _ => Profession.None,
    };

    /// <summary>The character level at which a master will take an apprentice — the same 20 that opens
    /// crafting level 1 (<see cref="Crafting.CharLevelFor"/>).</summary>
    public const int ProfessionJoinLevel = 20;

    static partial void RegisterProfessionQuests()
    {
        // Each master asks for a small pile of the COMMON material his own profession refines. That is
        // the pitch made concrete: the errand is one hour of the thing the job actually is, paid for
        // out of drops you already have, and it teaches the mat type you will live in.
        void Join(string id, string name, Profession prof, string matName, MaterialType mat,
                  string blurb, string elsewhere, string handOff)
        {
            Register(new QuestDef(
                Id: id,
                Name: name,
                Description: blurb,
                OfferNpcId: WorldMap.CraftMasterId(prof),
                MinLevel: ProfessionJoinLevel,
                AnyTownNpc: true,
                Steps: new[]
                {
                    new QuestStep(QuestStepType.TalkTo,
                        "Hear the master out", TargetId: WorldMap.CraftMasterId(prof)),
                    new QuestStep(QuestStepType.CollectItem,
                        $"Bring 20 {matName} — creatures drop them everywhere, and you already have some",
                        TargetId: Crafting.MaterialId(mat, ItemRarity.Common), Count: 20),
                    new QuestStep(QuestStepType.TalkTo,
                        handOff, TargetId: WorldMap.CraftMasterId(prof)),
                },
                // No exp, no gold, and that is deliberate: the reward IS the profession, and paying for
                // it as well would make "try all five, quit four" a farming route.
                Reward: new QuestReward()));
            _ = elsewhere;   // held in the blurb; see the ⚠ below
        }

        // ⚠ THE "OTHER MEANS" HALF OF HIS PITCH lives in each Description, not in a step, because a step
        // is read once while the description stays in the quest log — and *"other means of aquirering
        // the items like drop/bosses etc"* is information you want AFTER you have picked, when you are
        // deciding whether to farm a thing or make it.

        Join(QuestJoinWeaponSmith, "The Smith's Apprentice", Profession.WeaponSmith,
            "Common Ingots", MaterialType.Ingot,
            "A Weapon Smith refines INGOTS and forges weapons from E grade all the way to S. A forged "
          + "weapon is never ordinary: the attempt yields a Legendary or a Mythic piece, or it fails and "
          + "the metal is gone. You will not out-forge the world — weapons drop, and bosses carry the "
          + "best of them — but a drop is what the world chose to give you, and a forge is what you chose "
          + "to make.",
            "weapons also drop from creatures and bosses",
            "Take the hammer from Master Smith");

        Join(QuestJoinArmorSmith, "The Armorer's Apprentice", Profession.ArmorSmith,
            "Common Leather", MaterialType.Leather,
            "An Armorer refines LEATHER and makes every piece you wear that is not a weapon or a jewel — "
          + "bodies, helmets, gloves, boots and shields. A full set costs about what one weapon costs, "
          + "and a shield about what a helmet does. Armor drops too, and the finest is worn off a boss's "
          + "back; what a smith sells is the piece nobody had to die for.",
            "armor also drops from creatures and bosses",
            "Take the apron from Master Armorer");

        Join(QuestJoinJeweler, "The Jeweler's Apprentice", Profession.Jeweler,
            "Common Gems", MaterialType.Gem,
            "A Jeweler refines GEMS and sets rings, earrings and necklaces. Five slots, and a full set "
          + "costs one weapon — the small pieces are cheap and the necklace is half of it. Jewels are the "
          + "rarest thing a creature drops and the first thing a boss is hunted for, which is precisely "
          + "why someone should be able to make them.",
            "jewels also drop from creatures and bosses",
            "Take the loupe from Master Jeweler");

        Join(QuestJoinPotionMaster, "The Apothecary's Apprentice", Profession.PotionMaster,
            "Common Wood", MaterialType.Wood,
            "An Apothecary refines WOOD and brews everything you drink: healing potions, the nine blessing "
          + "potions, and the dash draughts from the weakest to the Supreme. Potions rain off creatures "
          + "and out of blessing boxes — you will never be short of them. What an Apothecary sells is "
          + "CHOICE: the potion you wanted, today, instead of the one that dropped.",
            "potions also drop from creatures and come in blessing boxes",
            "Take the mortar from Master Apothecary");

        Join(QuestJoinScrollScribe, "The Scribe's Apprentice", Profession.ScrollScribe,
            "Common Thread", MaterialType.Thread,
            "A Scribe refines THREAD and inks scrolls: return and resurrection, the enchant scrolls of "
          + "every grade from D to S, attribute scrolls, and the blessings that outlast a potion. Below "
          + "A grade the world hands out enchant scrolls freely. Above it, it does not hand out any at "
          + "all — and that is where a Scribe stops being a convenience and becomes the only supply "
          + "there is.",
            "enchant scrolls drop below A grade; above it they come from elites and bosses only",
            "Take the quill from Master Scribe");
    }
}
