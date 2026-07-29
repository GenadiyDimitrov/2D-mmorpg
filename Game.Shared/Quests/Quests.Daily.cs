namespace Game.Shared;

/// <summary>
/// DAILY quests — repeatable once per server day.
///
/// The first (owner, playtest-13): the Apothecary hands out a free 1-hour shot box, every day, from
/// level 6 to 75. It is deliberately a "no kills, accept then finish" errand — its whole job is to put
/// shots in the hands of someone who has not got 150 000 gold spare, so the early game is not
/// shot-less while the mid game still buys them. The window closes at 75 because by then gold is not
/// the constraint.
///
/// The reward is the SELECTION box, so a fighter takes soulshots and a mage spiritshots from the same
/// quest. Quest-granted boxes are UNTRADABLE (the vendor-bought ones are not) — a free daily that
/// could be farmed across characters and sold would just be a gold faucet.
/// </summary>
public static partial class QuestCatalog
{
    /// <summary>Apothecary Miren — she already sells the shots, so she is who you would ask.</summary>
    private const string DailyShotGiver = "merchant_potions";

    public const string QuestDailyShots = "daily_shots";

    static partial void RegisterDailyQuests()
    {
        Register(new QuestDef(
            Id: QuestDailyShots,
            Name: "The Apothecary's Favour",
            Description: "Miren keeps a box of shot runes behind the counter for those still finding "
                       + "their feet. Ask, and she will part with one — once a day, and only while you "
                       + "still look like you need it.",
            OfferNpcId: DailyShotGiver,
            MinLevel: 6,
            MaxLevel: 75,
            Daily: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo, "Ask Apothecary Miren for a shot box",
                              TargetId: DailyShotGiver),
            },
            Reward: new QuestReward(ItemIds: new[] { ItemCatalog.BoxDailyShotChoice })));
    }
}
