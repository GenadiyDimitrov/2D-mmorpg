namespace Game.Shared;

/// <summary>
/// THE DAILY RECIPE QUESTS (`BL-274` part 3, 0.208.0; design doc §2.3 Q4, its follow-up, and "Step 13
/// proposal", all RULED 2026-09-24).
///
/// T76 and T80 gear is CRAFTED ONLY, and its recipes come from boss/elite drops plus these quests. Three givers
/// stand in Frostmere, one per KIND (weapons, armour, jewels). Each offers:
/// <list type="bullet">
/// <item>a <b>T76</b> quest, levels 75-85, and a <b>T80</b> quest, levels 80+;</item>
/// <item>the two share ONE daily stamp (<see cref="QuestDef.DailyGroup"/>), so between 80 and 85 you pick one,
///       and the most a day pays is <b>three books, one per kind</b>;</item>
/// <item>the errand: talk to the OTHER two givers, each wants 8 kills of the kind's own carriers in the band (so
///       the day's kills also feed the right parts), then back to the giver;</item>
/// <item>the reward: <b>one 40% recipe book</b>, uniform within the kind (1/8 weapons, 1/7 armour, 1/3 jewels),
///       and nothing else. The 16 kills pay their own EXP and drops.</item>
/// </list>
/// </summary>
public static partial class QuestCatalog
{
    public const string RecipeWeaponGiver = "recipe_weaponwright";
    public const string RecipeArmourGiver = "recipe_armourer";
    public const string RecipeJewelGiver  = "recipe_jeweller";

    /// <summary>The recipe % the quests pay (Q4: *"T76 quest 40%, T80 quest 40%"*).</summary>
    public const int RecipeQuestPercent = 40;

    /// <summary>The quest id of a kind's quest at a tier, e.g. <c>daily_recipe_weapon_t76</c>.</summary>
    public static string RecipeQuestId(string kind, int tier) => $"daily_recipe_{kind}_t{tier}";

    /// <summary>One kind's giver and pool, and its carriers per tier: (mob id, plural name) for contact A, then B.</summary>
    private record RecipeKind(string Kind, string Giver, string GiverName, string Noun, string[] Keys,
                              (string Id, string Plural)[] T76, (string Id, string Plural)[] T80);

    static partial void RegisterRecipeDailies()
    {
        var kinds = new[]
        {
            new RecipeKind("weapon", RecipeWeaponGiver, "Weaponwright Harrow", "weapon", MobCatalog.WeaponKeys,
                T76: new[] { ("redhorn_soldier", "Redhorn Soldiers"), ("redhorn_general", "Redhorn Generals") },
                T80: new[] { ("wrathborn_demon", "Wrathborn Demons"), ("radiant_scout", "Radiant Scouts") }),
            new RecipeKind("armour", RecipeArmourGiver, "Armourer Edda", "armour", MobCatalog.ArmourKeys,
                T76: new[] { ("sunland_orc_captain", "Sunland Orc Captains"), ("sunland_orc_commander", "Sunland Orc Commanders") },
                T80: new[] { ("scarlet_mantis", "Scarlet Mantises"), ("splinter_mantis_drone", "Splinter Mantis Drones") }),
            // Each band has only ONE jewellery creature, so the second contact asks for a neighbour (step 13 table).
            new RecipeKind("jewel", RecipeJewelGiver, "Jeweller Ossian", "jewellery", MobCatalog.JewelKeys,
                T76: new[] { ("emberwyrm_drake", "Emberwyrm Drakes"), ("redhorn_general", "Redhorn Generals") },
                T80: new[] { ("radiant_mage", "Radiant Mages"), ("radiant_berserker", "Radiant Berserkers") }),
        };

        for (int k = 0; k < kinds.Length; k++)
        {
            var me = kinds[k];
            // Contacts: the OTHER two givers, in the ring's order, so each giver sends you round the other two.
            var a = kinds[(k + 1) % kinds.Length];
            var b = kinds[(k + 2) % kinds.Length];

            foreach (int tier in new[] { 76, 80 })
            {
                var mobs = tier == 76 ? me.T76 : me.T80;
                string grade = tier == 76 ? "Adamantine" : "Soulcrystal";
                Register(new QuestDef(
                    Id: RecipeQuestId(me.Kind, tier),
                    Name: $"{me.GiverName.Split(' ')[^1]}'s Commission ({grade})",
                    Description: $"{me.GiverName} keeps the {grade} {me.Noun} patterns under lock and key, and parts "
                               + $"with one a day to whoever helps the Frostmere workshops. {a.GiverName} and "
                               + $"{b.GiverName} each have a job for you first.",
                    OfferNpcId: me.Giver,
                    MinLevel: tier == 76 ? 75 : 80,
                    MaxLevel: tier == 76 ? 85 : 0,
                    Daily: true,
                    DailyGroup: $"daily_recipe_{me.Kind}",
                    Steps: new[]
                    {
                        new QuestStep(QuestStepType.TalkTo, $"Speak to {a.GiverName}", TargetId: a.Giver),
                        new QuestStep(QuestStepType.KillMobs, $"Slay 8 {mobs[0].Plural} for {a.GiverName}",
                                      TargetId: mobs[0].Id, Count: 8),
                        new QuestStep(QuestStepType.TalkTo, $"Speak to {b.GiverName}", TargetId: b.Giver),
                        new QuestStep(QuestStepType.KillMobs, $"Slay 8 {mobs[1].Plural} for {b.GiverName}",
                                      TargetId: mobs[1].Id, Count: 8),
                        new QuestStep(QuestStepType.TalkTo, $"Return to {me.GiverName}", TargetId: me.Giver),
                    },
                    Reward: new QuestReward(
                        RandomItemIds: me.Keys
                            .Select(key => ItemCatalog.RecipeBookId($"craft_{key}_t{tier}", RecipeQuestPercent))
                            .ToArray(),
                        RandomLabel: $"one {grade} {me.Noun} recipe ({RecipeQuestPercent}%), "
                                   + $"at random of {me.Keys.Length}")));
            }
        }
    }
}
