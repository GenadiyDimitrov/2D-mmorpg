namespace Game.Shared;

/// <summary>One possible reward in a box: an item id, an independent drop CHANCE
/// (0..1; 1 = always, down to 0.000001 = 1-in-a-million), and a quantity range.
/// Each entry rolls on its own, so a box can yield several items, one, or none.</summary>
public record BoxEntry(string ItemId, float Chance, int MinQty = 1, int MaxQty = 1);

/// <summary>A box/chest loot table, keyed by the box's item id.</summary>
public record BoxDef(string Id, BoxEntry[] Entries);

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
        foreach (var b in list)
            if (!dict.TryAdd(b.Id, b))
                throw new InvalidOperationException($"Duplicate box id '{b.Id}'.");
        return dict;
    }

    public static BoxDef? Get(string boxItemId) =>
        boxItemId is null ? null : All.GetValueOrDefault(boxItemId);
}
