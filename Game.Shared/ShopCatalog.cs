using System.Linq;

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
    /// <summary>The armor/shield/jewel half of the gear trade — see the split in Build().</summary>
    public const string ArmorMerchant = "merchant_armor";

    private static readonly Dictionary<string, ShopDef> Shops = Build();

    private static Dictionary<string, ShopDef> Build()
    {
        // The LOW sets of each grade (Low F/E/D — ids like "sword1h_t20lo"), sold alongside the training
        // kit so the early ladder is reachable by gold as well as by drops (owner 2026-07-25). Derived
        // from the catalogue so it never drifts from LowTierFillers. (Could be split across town vendors
        // by grade later; one gear NPC for now.)
        // WEAPONS go to the Armsmaster; ARMOR, shields and jewels to the Outfitter. One vendor holding
        // the whole F/E/D ladder at three qualities is ~150 rows, and that flat wall is most of what
        // made the shop unreadable (owner, playtest-13).
        static bool IsWeapon(ItemDef d) => d.Slot == EquipSlot.Weapon;

        var lowGear = ItemCatalog.AllItems
            .Where(d => d.Rarity == ItemRarity.Epic
                && d.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel
                && d.Id.Contains("_t", System.StringComparison.Ordinal)
                && d.Id.EndsWith("lo", System.StringComparison.Ordinal))
            .ToArray();

        // The REAL gear ladder at the three shop grades. Only F/E/D is ever sold (owner) — C and above
        // are crafted, dropped or taken off a boss — and only up to RARE, because Epic is where set
        // bonuses and rolled attributes begin and that tier is not for sale at any price.
        //
        // ItemLevel 1/20/40 are the F/E/D tiers; the price for each comes from the owner's table in
        // ItemCatalog.TieredGearPrice, scaled down for the low qualities (they drop freely, so at full
        // price nobody would buy one).
        var shopGrades = new[] { 1, 20, 40 };
        var shopQualities = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare };
        var ladderGear = ItemCatalog.AllItems
            .Where(d => shopGrades.Contains(d.ItemLevel)
                && shopQualities.Contains(d.Rarity)
                && d.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel
                && !d.Id.Contains("lo_", System.StringComparison.Ordinal))   // not the (Lesser) copies
            .OrderBy(d => d.ItemLevel).ThenBy(d => d.Slot).ThenBy(d => d.Rarity).ThenBy(d => d.Name)
            .ToArray();

        string[] WeaponsOf(params ItemDef[][] sets) =>
            sets.SelectMany(s => s).Where(IsWeapon).Select(d => d.Id).ToArray();
        string[] ArmorOf(params ItemDef[][] sets) =>
            sets.SelectMany(s => s).Where(d => !IsWeapon(d)).Select(d => d.Id).ToArray();

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
            // WEAPONS. The LEGACY generated grid ("Worn Sword" at P.Atk 6, the Fine/Masterwork
            // prefixes) plus Ash Wand and Iron Mace are GONE from the shop (owner, playtest-13): they
            // predate the gear ladder by a whole generation, so the vendor was showing two unrelated
            // eras of equipment in one list. The catalogue still defines them so old saves resolve.
            new ShopDef(GearMerchant, "Armsmaster — Weapons", new[]
            {
                // TRAINING tier (400g each) — the level 1-10 gear. Stocked so a new player who picked the
                // wrong weapon, or lost one, can just buy another instead of being stuck with it.
                ItemCatalog.TrainingSword,
                ItemCatalog.TrainingClub,
                ItemCatalog.TrainingKnives,
                ItemCatalog.TrainingBow,
                ItemCatalog.TrainingWand,
            }.Concat(WeaponsOf(lowGear, ladderGear)).ToArray()),

            // ARMOR, shields and jewels.
            new ShopDef(ArmorMerchant, "Outfitter — Armor & Jewels", new[]
            {
                ItemCatalog.TrainingLeather,
                ItemCatalog.TrainingRobe,
                // BROKEN jewels — also drop from level 1-5 mobs. Sold here so the first accessory is
                // reachable without waiting on a drop (owner: "jewels are dropped from lvl 1-5 mobs and
                // sold in shop"). Starter shield + amulet are the level-1 stopgaps.
                ItemCatalog.BrokenEarring,
                ItemCatalog.BrokenRing,
                ItemCatalog.BrokenNecklace,
                ItemCatalog.WoodenShield,
                ItemCatalog.BrassAmulet,
            }.Concat(ArmorOf(lowGear, ladderGear)).ToArray()),
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
