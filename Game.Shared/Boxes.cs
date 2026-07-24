namespace Game.Shared;

/// <summary>One possible reward in a box: an item id, an independent drop CHANCE
/// (0..1; 1 = always, down to 0.000001 = 1-in-a-million), and a quantity range.
/// Each entry rolls on its own, so a box can yield several items, one, or none.</summary>
public record BoxEntry(string ItemId, float Chance, int MinQty = 1, int MaxQty = 1);

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
                new BoxEntry(ItemCatalog.ScrollCommon, 0.50f),         // 50%
            }),

            // Newbie armor boxes — the class body + the SHARED accessories (100% each).
            new BoxDef(ItemCatalog.BoxNewbieArmorLight, new[]
            {
                new BoxEntry(ItemCatalog.NewbieLightBody, 1.0f),
                new BoxEntry(ItemCatalog.NewbieHelm, 1.0f),
                new BoxEntry(ItemCatalog.NewbieGloves, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBoots, 1.0f),
            }),
            new BoxDef(ItemCatalog.BoxNewbieArmorRobe, new[]
            {
                new BoxEntry(ItemCatalog.NewbieRobeBody, 1.0f),
                new BoxEntry(ItemCatalog.NewbieHelm, 1.0f),
                new BoxEntry(ItemCatalog.NewbieGloves, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBoots, 1.0f),
            }),

            // Newbie WEAPONS SELECTION box — pick ONE starter weapon (the STAFF is in here now, so a mage
            // picks it from the same box as everyone else — the starter reward is class-agnostic).
            new BoxDef(ItemCatalog.BoxNewbieWeapons, new[]
            {
                new BoxEntry(ItemCatalog.NewbieSword1H, 1.0f),
                new BoxEntry(ItemCatalog.NewbieDaggers, 1.0f),
                new BoxEntry(ItemCatalog.NewbieSword2H, 1.0f),
                new BoxEntry(ItemCatalog.NewbieBow, 1.0f),
                new BoxEntry(ItemCatalog.NewbieStaff, 1.0f),
            }, PickCount: 1),

            // Newbie ARMOR-SET choice — pick ONE of the two armor boxes (fighter light / mage robe).
            new BoxDef(ItemCatalog.BoxNewbieArmorChoice, new[]
            {
                new BoxEntry(ItemCatalog.BoxNewbieArmorLight, 1.0f),
                new BoxEntry(ItemCatalog.BoxNewbieArmorRobe, 1.0f),
            }, PickCount: 1),

            // Newbie SHOT-RUNE choice — pick ONE 1-day rune box (soulshot / spiritshot).
            new BoxDef(ItemCatalog.BoxNewbieRuneChoice, new[]
            {
                new BoxEntry(ItemCatalog.BoxSoulshot24h, 1.0f),
                new BoxEntry(ItemCatalog.BoxSpiritshot24h, 1.0f),
            }, PickCount: 1),

            // Newbie jewels box — 2 earrings, 2 rings, 1 necklace (100% each).
            new BoxDef(ItemCatalog.BoxNewbieJewels, new[]
            {
                new BoxEntry(ItemCatalog.NewbieEarring, 1.0f, 2, 2),
                new BoxEntry(ItemCatalog.NewbieRing, 1.0f, 2, 2),
                new BoxEntry(ItemCatalog.NewbieNecklace, 1.0f),
            }),

            // Treasure Chest — staples always, rarer rewards scaling down to a
            // 1-in-a-million jackpot (demonstrates the full chance range).
            new BoxDef(ItemCatalog.BoxTreasure, new[]
            {
                new BoxEntry(ItemCatalog.HealingPotion, 1.0f, 3, 3),   // always 3
                new BoxEntry(ItemCatalog.ScrollUncommon, 0.50f),       // 50%
                new BoxEntry(ItemCatalog.ScrollRare, 0.10f),           // 10%
                new BoxEntry(ItemCatalog.AttrScrollRare, 0.05f),       // 5%
                new BoxEntry(ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Rare), 0.01f),   // 1%
                new BoxEntry(ItemCatalog.GodWeapon, 0.000001f),        // 1 in 1,000,000 jackpot
            }),
        };

        var dict = new Dictionary<string, BoxDef>();
        foreach (var b in list.Concat(TieredAccessoryBoxes()).Concat(ShotBoxes()))
            if (!dict.TryAdd(b.Id, b))
                throw new InvalidOperationException($"Duplicate box id '{b.Id}'.");
        return dict;
    }

    /// <summary>The sealed shot boxes → their rune (100%). The DURATION isn't here — it's on the box's
    /// ItemDef (GrantsRuneSeconds), stamped onto the rune at open time.</summary>
    private static IEnumerable<BoxDef> ShotBoxes()
    {
        foreach (var boxId in new[]
                 {
                     ItemCatalog.BoxSoulshot1h, ItemCatalog.BoxSoulshot2h,
                     ItemCatalog.BoxSoulshot24h, ItemCatalog.BoxSoulshot30d,
                 })
            yield return new BoxDef(boxId, new[] { new BoxEntry(ItemCatalog.SoulshotRune, 1f) });

        foreach (var boxId in new[]
                 {
                     ItemCatalog.BoxSpiritshot1h, ItemCatalog.BoxSpiritshot2h,
                     ItemCatalog.BoxSpiritshot24h, ItemCatalog.BoxSpiritshot30d,
                 })
            yield return new BoxDef(boxId, new[] { new BoxEntry(ItemCatalog.SpiritshotRune, 1f) });
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
