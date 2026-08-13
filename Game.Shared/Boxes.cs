namespace Game.Shared;

/// <summary>One possible reward in a box: an item id, an independent drop CHANCE
/// (0..1; 1 = always, down to 0.000001 = 1-in-a-million), and a quantity range.
/// Each entry rolls on its own, so a box can yield several items, one, or none.
///
/// <para><paramref name="ForClass"/> makes the entry CLASS-CONDITIONAL: null (the default) is for
/// everyone, otherwise only that base class ever sees it. It exists so the training boxes could stop
/// being selection boxes — *"I think the Training gear should be with normal boxes not selection:
/// armor box -> mage gets robe, fighter gets light; weapon box -> mage gets wand, fighter gets
/// sword"* (him, 63j). Asking a brand-new player to choose between four weapons they cannot yet tell
/// apart is a decision that teaches nothing, and the wrong pick is a worse first hour.</para></summary>
public record BoxEntry(string ItemId, float Chance, int MinQty = 1, int MaxQty = 1,
    BaseClass? ForClass = null);

/// <summary>A box/chest loot table, keyed by the box's item id. PickCount = 0 means a
/// RANDOM box (each entry rolls its Chance). PickCount &gt; 0 makes it a SELECTION box:
/// the entries are OPTIONS and the player chooses exactly that many (Chance ignored).</summary>
public record BoxDef(string Id, BoxEntry[] Entries, int PickCount = 0);

/// <summary>The openable boxes/chests and what they can drop. Keyed by item id, so the
/// open handler just looks up Get(boxItemId). Chances span 1/1 (100%) to 1/1000000.</summary>
public static class BoxCatalog
{
    private static readonly Dictionary<string, BoxDef> All = Build();

    private static Dictionary<string, BoxDef> Build()
    {
        var list = new[]
        {
            // Newbie Box — guaranteed starter consumables + a chance at a scroll.
            new BoxDef(ItemCatalog.BoxNewbie, new[]
            {
                new BoxEntry(ItemCatalog.MinorPotion, 1.0f, 5, 5),     // always 5
                new BoxEntry(ItemCatalog.GreaterPotion, 1.0f, 2, 2),   // always 2
                new BoxEntry(ItemCatalog.ScrollNormalE, 0.50f),         // 50%
            }),

            // Newbie armor boxes — the class body + the SHARED accessories (100% each).
            // ⚠ These grant the BOUND copies (playtest-19 M6): the Newbie kit is a 30-day loaner,
            // untradable and unsellable. The un-suffixed ids are real Ferrite Mythic ladder gear and
            // must stay ordinary items — see ItemCatalog.BoundCopies.
            new BoxDef(ItemCatalog.BoxNewbieArmorLight, new[]
            {
                new BoxEntry(ItemCatalog.NewbieLightBodyBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieHelmBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieGlovesBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBootsBound, 1.0f),
            }),
            new BoxDef(ItemCatalog.BoxNewbieArmorRobe, new[]
            {
                new BoxEntry(ItemCatalog.NewbieRobeBodyBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieHelmBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieGlovesBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBootsBound, 1.0f),
            }),

            // Newbie WEAPONS SELECTION box — pick ONE starter weapon (the STAFF is in here now, so a mage
            // picks it from the same box as everyone else — the starter reward is class-agnostic).
            // The WAND is here too (checklist C15): a one-handed caster had no starter option at all.
            new BoxDef(ItemCatalog.BoxNewbieWeapons, new[]
            {
                new BoxEntry(ItemCatalog.NewbieSword1HBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieDaggersBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieSword2HBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBowBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieStaffBound, 1.0f),
                new BoxEntry(ItemCatalog.NewbieWandBound, 1.0f),
            }, PickCount: 1),

            // ---- THE TWO TRAINING BOXES ARE NO LONGER SELECTIONS (him, 63j) ----------------------
            // "Also I think the Training gear should be with normal boxes not selection ... Any fighter
            //  cen get trough with a sword. Other training Club and training knives can be deleted."
            // They are handed out by the tutorial's first steps, to a player who has never seen the
            // game — a four-way choice there is a quiz, not a decision, and the wrong answer is a bad
            // first hour. So: no picker, no dialog, tap the box and wear what falls out. The BASE CLASS
            // decides, which is the only thing the game knows about you at level 1.
            // ⚠ The club and the knives are gone with the picker (their defs are deleted too) — the
            // real weapon choice is the Newbie box at the level-10 quest, which stocks all six.
            new BoxDef(ItemCatalog.BoxTrainingWeapons, new[]
            {
                new BoxEntry(ItemCatalog.TrainingSword, 1.0f, ForClass: BaseClass.Fighter),
                new BoxEntry(ItemCatalog.TrainingWand, 1.0f, ForClass: BaseClass.Mage),
            }),

            // TRAINING armor — leather for a fighter, robe for a mage. No accessories: the helm/gloves/
            // boots line starts at the Newbie tier, i.e. at the level-10 quest.
            new BoxDef(ItemCatalog.BoxTrainingArmorChoice, new[]
            {
                new BoxEntry(ItemCatalog.TrainingLeather, 1.0f, ForClass: BaseClass.Fighter),
                new BoxEntry(ItemCatalog.TrainingRobe, 1.0f, ForClass: BaseClass.Mage),
            }),

            // Newbie ARMOR-SET choice — pick ONE of the two armor boxes (fighter light / mage robe).
            new BoxDef(ItemCatalog.BoxNewbieArmorChoice, new[]
            {
                new BoxEntry(ItemCatalog.BoxNewbieArmorLight, 1.0f),
                new BoxEntry(ItemCatalog.BoxNewbieArmorRobe, 1.0f),
            }, PickCount: 1),

            // Newbie RUNE choice — pick ONE 1-day rune box (war rune / spell rune).
            new BoxDef(ItemCatalog.BoxNewbieRuneChoice, new[]
            {
                new BoxEntry(ItemCatalog.BoxWarRune24h, 1.0f),
                new BoxEntry(ItemCatalog.BoxSpellRune24h, 1.0f),
            }, PickCount: 1),

            // The Apothecary's DAILY favour — the same choice, one hour instead of one day.
            new BoxDef(ItemCatalog.BoxDailyRuneChoice, new[]
            {
                new BoxEntry(ItemCatalog.BoxWarRune1h, 1.0f),
                new BoxEntry(ItemCatalog.BoxSpellRune1h, 1.0f),
            }, PickCount: 1),

            // THE BLESSING BOX — pick 10 of the 17 buff scrolls (playtest-17 E3). This is the first
            // pick-MANY box in the game; the server already handled it (`Take(box.PickCount)` over the
            // distinct picks), the client chooser is what had to grow a tally and a Confirm.
            // Order matches the buff bar's reading order: the speed four, the five stat families, then
            // the eight that have no potion — so the list you tick reads like the buffer's own list.
            new BoxDef(ItemCatalog.BoxBuffScrolls, new[]
            {
                new BoxEntry(ItemCatalog.SpeedScrollR, 1.0f),
                new BoxEntry(ItemCatalog.CastScrollR, 1.0f),
                new BoxEntry(ItemCatalog.AtkScrollR, 1.0f),
                new BoxEntry(ItemCatalog.EvaScrollR, 1.0f),
                new BoxEntry(ItemCatalog.MightScrollR, 1.0f),
                new BoxEntry(ItemCatalog.BulwarkScrollR, 1.0f),
                new BoxEntry(ItemCatalog.ForceScrollR, 1.0f),
                new BoxEntry(ItemCatalog.WardScrollR, 1.0f),
                new BoxEntry(ItemCatalog.AimScrollR, 1.0f),
                new BoxEntry(ItemCatalog.BodyScrollM, 1.0f),
                new BoxEntry(ItemCatalog.SoulScrollM, 1.0f),
                new BoxEntry(ItemCatalog.VigorScrollM, 1.0f),
                new BoxEntry(ItemCatalog.SerenityScrollM, 1.0f),
                new BoxEntry(ItemCatalog.FocusScrollM, 1.0f),
                new BoxEntry(ItemCatalog.FerocityScrollM, 1.0f),
                new BoxEntry(ItemCatalog.InsightScrollM, 1.0f),
                new BoxEntry(ItemCatalog.FrenzyScrollM, 1.0f),
            }, PickCount: 10),

            // Newbie jewels box — 2 earrings, 2 rings, 1 necklace (100% each).
            new BoxDef(ItemCatalog.BoxNewbieJewels, new[]
            {
                new BoxEntry(ItemCatalog.NewbieEarringBound, 1.0f, 2, 2),
                new BoxEntry(ItemCatalog.NewbieRingBound, 1.0f, 2, 2),
                new BoxEntry(ItemCatalog.NewbieNecklaceBound, 1.0f),
            }),

            // Treasure Chest — staples always, rarer rewards scaling down to a
            // 1-in-a-million jackpot (demonstrates the full chance range).
            new BoxDef(ItemCatalog.BoxTreasure, new[]
            {
                new BoxEntry(ItemCatalog.HealingPotion, 1.0f, 3, 3),   // always 3
                new BoxEntry(ItemCatalog.ScrollNormalD, 0.50f),       // 50%
                new BoxEntry(ItemCatalog.ScrollNormalC, 0.10f),           // 10%
                new BoxEntry(ItemCatalog.AttrScrollRare, 0.05f),       // 5%
                // The 1% slot was `sword_e_rare` — "Masterwork Steel Sword", off the legacy grid that
                // was deleted in playtest-22, and the ONLY live way anyone could ever receive one of
                // those sixty items. It is the E-grade Rare blade off the real ladder now, which is
                // what that slot always meant to be.
                new BoxEntry("sword1h_t20_rare", 0.01f),   // 1%
                // The jackpot was the God-tier debug sword until 2026-08-07 (playtest-19 `0b`,
                // *"nothing that can't be acquired in game"*). It is now the S-grade Mythic 1H blade —
                // the real top of the sword line, so the 1-in-a-million slot still demonstrates the
                // full chance range but pays out something the game already contains.
                new BoxEntry($"sword1h_t{ItemCatalog.SGradeLevel}", 0.000001f),   // 1 in 1,000,000 jackpot
            }),
        };

        var dict = new Dictionary<string, BoxDef>();
        foreach (var b in list.Concat(TieredAccessoryBoxes()).Concat(RuneBoxes()))
            if (!dict.TryAdd(b.Id, b))
                throw new InvalidOperationException($"Duplicate box id '{b.Id}'.");
        return dict;
    }

    /// <summary>The sealed rune boxes → their rune (100%). The DURATION isn't here — it's on the box's
    /// ItemDef (GrantsRuneSeconds), stamped onto the rune at open time.</summary>
    private static IEnumerable<BoxDef> RuneBoxes()
    {
        foreach (var boxId in new[]
                 {
                     ItemCatalog.BoxWarRune1h, ItemCatalog.BoxWarRune2h,
                     ItemCatalog.BoxWarRune24h, ItemCatalog.BoxWarRune30d,
                 })
            yield return new BoxDef(boxId, new[] { new BoxEntry(ItemCatalog.WarRune, 1f) });

        foreach (var boxId in new[]
                 {
                     ItemCatalog.BoxSpellRune1h, ItemCatalog.BoxSpellRune2h,
                     ItemCatalog.BoxSpellRune24h, ItemCatalog.BoxSpellRune30d,
                 })
            yield return new BoxDef(boxId, new[] { new BoxEntry(ItemCatalog.SpellRune, 1f) });
    }

    /// <summary>One accessory box per gear tier → the 3 accessories of that tier (100% each).</summary>
    private static IEnumerable<BoxDef> TieredAccessoryBoxes()
    {
        foreach (int L in new[] { 20, 40, 52, 61, 76 })
            yield return new BoxDef($"box_acc_t{L}", new[]
            {
                new BoxEntry($"gloves_t{L}", 1f),
                new BoxEntry($"boots_t{L}", 1f),
                new BoxEntry($"helm_t{L}", 1f),
            });
    }

    public static BoxDef? Get(string boxItemId) =>
        boxItemId is null ? null : All.GetValueOrDefault(boxItemId);
}
