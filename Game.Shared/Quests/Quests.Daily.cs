namespace Game.Shared;

/// <summary>
/// DAILY quests — repeatable once per server day.
///
/// The first (owner, playtest-13): the Apothecary hands out a free 1-hour rune box, every day, from
/// level 6 to 75. It is deliberately a "no kills, accept then finish" errand — its whole job is to put
/// runes in the hands of someone who has not got 150 000 gold spare, so the early game is not
/// rune-less while the mid game still buys them. The window closes at 75 because by then gold is not
/// the constraint.
///
/// The reward is the SELECTION box, so a fighter takes war runes and a mage spell runes from the same
/// quest. Quest-granted boxes are UNTRADABLE (the vendor-bought ones are not) — a free daily that
/// could be farmed across characters and sold would just be a gold faucet.
/// </summary>
public static partial class QuestCatalog
{
    /// <summary>Apothecary Miren — she already sells the runes, so she is who you would ask.</summary>
    private const string DailyRuneGiver = "merchant_potions";

    public const string QuestDailyRunes = "daily_runes";

    static partial void RegisterDailyQuests()
    {
        Register(new QuestDef(
            Id: QuestDailyRunes,
            Name: "The Apothecary's Favour",
            Description: "Miren keeps a box of runes behind the counter for those still finding "
                       + "their feet. Ask, and she will part with one — once a day, and only while you "
                       + "still look like you need it.",
            OfferNpcId: DailyRuneGiver,
            MinLevel: 6,
            MaxLevel: 75,
            Daily: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo, "Ask Apothecary Miren for a rune box",
                              TargetId: DailyRuneGiver),
            },
            Reward: new QuestReward(ItemIds: new[] { ItemCatalog.BoxDailyRuneChoice })));
    }
}
