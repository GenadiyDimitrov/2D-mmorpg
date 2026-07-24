namespace Game.Shared;

/// <summary>
/// A vendor's wares: the item ids it sells. Buy prices come from each item's
/// Value (ItemCatalog.BuyPrice); the shop only decides WHAT is for sale. Selling
/// is independent — any vendor buys back any sellable item the player owns.
/// </summary>
public record ShopDef(string NpcId, string Title, string[] ItemIds);

/// <summary>
/// Per-NPC shop definitions, keyed by the vendor's NpcDef id (WorldMap.Npcs).
/// Vendor-sold gear is created PLAIN (no rolled attributes) at buy time.
/// </summary>
public static class ShopCatalog
{
    public const string PotionMerchant = "merchant_potions";
    public const string GearMerchant = "merchant_gear";

    private static readonly Dictionary<string, ShopDef> Shops = Build();

    private static Dictionary<string, ShopDef> Build()
    {
        var shops = new[]
        {
            new ShopDef(PotionMerchant, "Apothecary", new[]
            {
                // Healing potions — Common + Uncommon only. The RARE tier (GreaterPotion) is removed from
                // the vendor (owner): rare potions should come from drops/rewards, not gold, matching the
                // potion-economy design where the top tier stays out of the shop.
                ItemCatalog.MinorPotion,
                ItemCatalog.HealingPotion,
                // Common buff potions (the weak tier is vendor-only; stronger ones drop).
                ItemCatalog.SpeedPotionC,
                ItemCatalog.CastPotionC,
                ItemCatalog.AtkPotionC,
                // Scroll of Return (500g) — a faster escape than the free 30s Return skill. The
                // Ultimate scroll is NOT sold here (special vendor later).
                ItemCatalog.ScrollReturn,
                // Scroll of Resurrection (1500g) — revive a dead ally (no exp restored). The
                // Ultimate resurrection scroll (restores all lost exp) is NOT sold here.
                ItemCatalog.ScrollResurrect,
                // Reagents: Skill Stone (400g — Angel's Protection etc.) + Elemental Stone (20k — nuker burst).
                ItemCatalog.SkillStone,
                ItemCatalog.ElementalStone,
                // Shot boxes — 1h/2h only (24h/30d are premium/pass, debug-only for now). Fighters buy the
                // Soulshot box, mages the Spiritshot; anyone may buy either (e.g. a buffer for melee).
                ItemCatalog.BoxSoulshot1h,
                ItemCatalog.BoxSoulshot2h,
                ItemCatalog.BoxSpiritshot1h,
                ItemCatalog.BoxSpiritshot2h,
                // NOTE: enchant + attribute scrolls are intentionally DROP-ONLY (not sold).
            }),
            new ShopDef(GearMerchant, "Armsmaster", new[]
            {
                // TRAINING tier (400g each) — the level 1-10 gear. Stocked so a new player who picked the
                // wrong weapon, or lost one, can just buy another instead of being stuck with it.
                ItemCatalog.TrainingSword,
                ItemCatalog.TrainingClub,
                ItemCatalog.TrainingKnives,
                ItemCatalog.TrainingBow,
                ItemCatalog.TrainingWand,
                ItemCatalog.TrainingLeather,
                ItemCatalog.TrainingRobe,
                // BROKEN jewels — also drop from level 1-5 mobs. Sold here so the first accessory is
                // reachable without waiting on a drop (owner: "jewels are dropped from lvl 1-5 mobs and
                // sold in shop").
                ItemCatalog.BrokenEarring,
                ItemCatalog.BrokenRing,
                ItemCatalog.BrokenNecklace,
                // Basic plain weapons (F, common).
                ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.WeaponKey(WeaponType.Dual,  ItemGrade.F, ItemRarity.Common),
                ItemCatalog.WeaponKey(WeaponType.Bow,   ItemGrade.F, ItemRarity.Common),
                ItemCatalog.WeaponKey(WeaponType.TwoHandedBlunt, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.IronMace,
                ItemCatalog.AshWand,
                // Basic plain body armor (F, common) + accessories.
                ItemCatalog.ArmorKey(ArmorWeight.Heavy, ArmorSlot.Body, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.ArmorKey(ArmorWeight.Light, ArmorSlot.Body, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.ArmorKey(ArmorWeight.Robe,  ArmorSlot.Body, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.ArmorKey(ArmorWeight.None, ArmorSlot.Head,   ItemGrade.F, ItemRarity.Common),
                ItemCatalog.ArmorKey(ArmorWeight.None, ArmorSlot.Gloves, ItemGrade.F, ItemRarity.Common),
                ItemCatalog.ArmorKey(ArmorWeight.None, ArmorSlot.Boots,  ItemGrade.F, ItemRarity.Common),
                // Starter shield + jewel.
                ItemCatalog.WoodenShield,
                ItemCatalog.BrassAmulet,
            }),
        };

        var dict = new Dictionary<string, ShopDef>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in shops)
            dict[s.NpcId] = s;
        return dict;
    }

    public static ShopDef? Get(string? npcId) =>
        npcId is null ? null : Shops.GetValueOrDefault(npcId);

    public static bool Sells(string npcId, string itemId) =>
        Get(npcId) is ShopDef shop && Array.IndexOf(shop.ItemIds, itemId) >= 0;
}
