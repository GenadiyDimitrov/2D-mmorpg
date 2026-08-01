namespace Game.Shared;

/// <summary>
/// REPEATABLE quests — the Huntmaster's contracts (owner, playtest-13: *"we need repeatable quests"*).
///
/// He named three shapes, and all three are the same <see cref="QuestDef.Repeatable"/> flag:
///
/// <list type="bullet">
/// <item><b>Endless gathering</b> — *"can be kill mobs indefinitely (gathering quest items as u farm in
///   a specific zone)"*. One step: come back when you feel like it. While it is active, the named
///   creatures each drop their own token, and turning in pays for every token you carry. Take one,
///   farm for an hour, hand in 20 ribs and 55 pelts, take it again.</item>
/// <item><b>Finite</b> — *"kill 10 of those, 50 of those, gets reward at the end — normal quest just
///   dont close on finish"*. An ordinary kill quest that happens to be repeatable.</item>
/// <item><b>Talk-to</b> — the same, with nothing to kill. The Apothecary's daily
///   (Quests.Daily.cs) is one of these, day-limited.</item>
/// </list>
///
/// <para><b>The payout has no authored numbers.</b> Each gather line's <c>RewardModifier</c> is the
/// owner's <c>QuestItemRewardModifier</c>: a token pays that fraction of its creature's OWN kill exp
/// and gold, again. So a contract is worth ~+25-35% on the hour you farm it, at every level, and
/// nothing here has to be re-tuned when the exp curve moves. The finite contracts' completion bonus is
/// written the same way — "five kills' worth" — rather than as a number that would silently rot.</para>
///
/// <para><b>Why one contract per CITY.</b> The Huntmaster stands in every city and offers the fields
/// that city manages, so the three creatures on his list span his whole band: a fresh arrival farms the
/// first, someone about to move on farms the third, and the modifier rises with the creature. That is
/// also why the accept window is his band ±4 rather than exact — you should be able to take the
/// contract on the walk in.</para>
///
/// <para>⚠ No main reward on the gathering contracts, deliberately (owner: *"usually no daily limited
/// repeated quests will have no main reward and be only farm quests"*). The tokens ARE the reward; a
/// fixed lump on top would make hand-in-immediately the best play.</para>
/// </summary>
public static partial class QuestCatalog
{
    public const string QuestHuntBrackenford = "hunt_brackenford";
    public const string QuestHuntStonewatch  = "hunt_stonewatch";
    public const string QuestHuntGreymarsh   = "hunt_greymarsh";
    public const string QuestHuntIronreach   = "hunt_ironreach";
    public const string QuestHuntFrostmere   = "hunt_frostmere";
    public const string QuestHuntCullBears   = "hunt_cull_bears";
    public const string QuestHuntRedhorn     = "hunt_redhorn_orders";

    /// <summary>A finite contract's completion bonus, in kills of the creature it asks for. Written as
    /// a multiple of the real reward so it tracks the exp/gold curves instead of dating.</summary>
    private static QuestReward KillsWorth(int mobLevel, int kills) => new(
        Exp: (int)(StatCalculator.MobExpReward(mobLevel) * kills),
        Gold: StatCalculator.MobGoldReward(mobLevel) * kills);

    static partial void RegisterRepeatableQuests()
    {
        // ── Brackenford, levels 1-16 ──────────────────────────────────────────────────────────────
        Register(new QuestDef(
            Id: QuestHuntBrackenford,
            Name: "Bracken Contract",
            Description: "Huntmaster Cera keeps a standing bounty on everything that comes out of the "
                       + "hollow and the downs. Bring back what you take off them — she pays by the "
                       + "piece, and the contract never closes.",
            OfferNpcId: "hunter_brackenford",
            MinLevel: 3, MaxLevel: 20,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Hunt in the Bracken fields, then return to Huntmaster Cera",
                    TargetId: "hunter_brackenford"),
            },
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("fox",         ItemCatalog.TokenFoxPelt,      1f, 0.25f),
                new QuestGather("werewolf",    ItemCatalog.TokenWerewolfFang, 1f, 0.30f),
                new QuestGather("hook_spider", ItemCatalog.TokenSpiderHook,   1f, 0.35f),
            }));

        // ── Stonewatch, levels 16-40 ──────────────────────────────────────────────────────────────
        Register(new QuestDef(
            Id: QuestHuntStonewatch,
            Name: "Stonewatch Contract",
            Description: "The moor, the ridge and the barrens all feed the same ledger. Huntmaster "
                       + "Radd does not care which of them you work — only what you carry back.",
            OfferNpcId: "hunter_stonewatch",
            MinLevel: 15, MaxLevel: 44,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Hunt in the Stonewatch fields, then return to Huntmaster Radd",
                    TargetId: "hunter_stonewatch"),
            },
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("skeleton_grunt", ItemCatalog.TokenCrackedRib, 1f, 0.25f),
                new QuestGather("grizzly_bear",   ItemCatalog.TokenBearPelt,   1f, 0.30f),
                new QuestGather("mantis_worker",  ItemCatalog.TokenMantisClaw, 1f, 0.35f),
            }));

        // ── Greymarsh, levels 40-60 ───────────────────────────────────────────────────────────────
        Register(new QuestDef(
            Id: QuestHuntGreymarsh,
            Name: "Marsh Contract",
            Description: "Huntmaster Sela has a list as long as the mire is deep. Work any of it. "
                       + "She counts what you bring and pays the same day.",
            OfferNpcId: "hunter_greymarsh",
            MinLevel: 38, MaxLevel: 64,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Hunt in the Greymarsh fields, then return to Huntmaster Sela",
                    TargetId: "hunter_greymarsh"),
            },
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("harpy",           ItemCatalog.TokenHarpyFeather,   1f, 0.25f),
                new QuestGather("amber_basilisk",  ItemCatalog.TokenBasiliskScale,  1f, 0.30f),
                new QuestGather("ash_orc_soldier", ItemCatalog.TokenAshOrcInsignia, 1f, 0.35f),
            }));

        // ── Ironreach, levels 60-75 ───────────────────────────────────────────────────────────────
        Register(new QuestDef(
            Id: QuestHuntIronreach,
            Name: "March Contract",
            Description: "Ironreach keeps its borders by paying for them. Huntmaster Torv writes the "
                       + "receipts; the march writes the rest.",
            OfferNpcId: "hunter_ironreach",
            MinLevel: 58, MaxLevel: 79,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Hunt in the Ironreach fields, then return to Huntmaster Torv",
                    TargetId: "hunter_ironreach"),
            },
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("cursed_blade",     ItemCatalog.TokenRustedShard,  1f, 0.25f),
                new QuestGather("dread_knight",     ItemCatalog.TokenDreadSigil,   1f, 0.30f),
                new QuestGather("redhorn_footman",  ItemCatalog.TokenRedhornBadge, 1f, 0.35f),
            }));

        // ── Frostmere, levels 76-90. No ceiling: this is the last band there is. ──────────────────
        Register(new QuestDef(
            Id: QuestHuntFrostmere,
            Name: "Frostmere Contract",
            Description: "Huntmaster Ingra has outlived three garrisons and every creature on her "
                       + "list. Bring proof, take payment, take the contract again.",
            OfferNpcId: "hunter_frostmere",
            MinLevel: 74,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.TalkTo,
                    "Hunt in the Frostmere fields, then return to Huntmaster Ingra",
                    TargetId: "hunter_frostmere"),
            },
            Reward: new QuestReward(),
            Gathers: new[]
            {
                new QuestGather("emberwyrm_drake",         ItemCatalog.TokenEmberScale,     1f, 0.25f),
                new QuestGather("radiant_scout",           ItemCatalog.TokenRadiantPlume,   1f, 0.30f),
                new QuestGather("splinter_mantis_walker",  ItemCatalog.TokenSplinterChitin, 1f, 0.35f),
            }));

        // ── The FINITE shape: a plain kill quest that simply does not close. ──────────────────────
        Register(new QuestDef(
            Id: QuestHuntCullBears,
            Name: "Thin the Herd",
            Description: "The bears on the moor have stopped being wary of people, which is how "
                       + "people stop coming back. Huntmaster Radd wants twenty of them gone. He will "
                       + "want twenty more after that.",
            OfferNpcId: "hunter_stonewatch",
            MinLevel: 18, MaxLevel: 34,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 20 Grizzly Bears",
                    TargetId: "grizzly_bear", Count: 20),
                new QuestStep(QuestStepType.TalkTo, "Return to Huntmaster Radd",
                    TargetId: "hunter_stonewatch"),
            },
            Reward: KillsWorth(22, 5)));

        Register(new QuestDef(
            Id: QuestHuntRedhorn,
            Name: "Standing Orders",
            Description: "The Redhorn keep sending footmen down the march, and Huntmaster Torv keeps "
                       + "sending people to meet them. Twenty-five at a time, as often as you like.",
            OfferNpcId: "hunter_ironreach",
            MinLevel: 68, MaxLevel: 79,
            Repeatable: true,
            Steps: new[]
            {
                new QuestStep(QuestStepType.KillMobs, "Slay 25 Redhorn Footmen",
                    TargetId: "redhorn_footman", Count: 25),
                new QuestStep(QuestStepType.TalkTo, "Return to Huntmaster Torv",
                    TargetId: "hunter_ironreach"),
            },
            Reward: KillsWorth(72, 5)));
    }
}
