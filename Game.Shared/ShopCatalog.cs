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
        // WEAPONS go to the Armsmaster; ARMOR, shields and jewels to the Outfitter. One vendor holding
        // the whole F/E/D ladder at three qualities is ~150 rows, and that flat wall is most of what
        // made the shop unreadable (owner, playtest-13).
        //
        // The separate "(Lesser)" line is GONE (owner, 2026-07-30): "we should have lesser items no
        // longer, they've become the common ones". It was a parallel item set at the same levels as the
        // real ladder, priced by the same table but flagged Epic — so the shop was showing Epic-priced
        // Lesser gear beside the ladder's own Common/Uncommon/Rare. One ladder per slot per grade now,
        // and the low QUALITIES are what "cheap gear" means.
        static bool IsWeapon(ItemDef d) => d.Slot == EquipSlot.Weapon;

        // The REAL gear ladder at the three shop grades. Only F/E/D is ever sold (owner) — C and above
        // are crafted, dropped or taken off a boss — and only up to RARE, because Epic is where set
        // bonuses and rolled attributes begin and that tier is not for sale at any price.
        //
        // ItemLevel 1/20/40 are the F/E/D tiers; the price for each comes from the owner's table in
        // ItemCatalog.TieredGearPrice, scaled down for the low qualities (they drop freely, so at full
        // price nobody would buy one).
        var shopGrades = new[] { ItemCatalog.FGradeLevel, 20, 40 };
        var shopQualities = new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare };
        var ladderGear = ItemCatalog.AllItems
            .Where(d => shopGrades.Contains(d.ItemLevel)
                && shopQualities.Contains(d.Rarity)
                && d.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel
                )
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
                // Common buff potions (the weak rung is vendor-stocked; the Uncommon rung drops).
                // Individual scrolls are NOT sold — see the Blessing Box below.
                ItemCatalog.SpeedPotionC,
                ItemCatalog.CastPotionC,
                ItemCatalog.AtkPotionC,
                ItemCatalog.EvaPotionC,
                ItemCatalog.MightPotionC,
                ItemCatalog.BulwarkPotionC,
                ItemCatalog.ForcePotionC,
                ItemCatalog.WardPotionC,
                ItemCatalog.AimPotionC,
                // The Blessing Box (250k) — buff SCROLLS are sold only as a pick-10 box, and only here
                // (playtest-17 E3). The Dash potion left this shelf with the same change: it is
                // drop-and-boss-points only now, deliberately not something gold can top up.
                ItemCatalog.BoxBuffScrolls,
                // Scroll of Return (500g) — a faster escape than the free 30s Return skill. The
                // Ultimate scroll is NOT sold here (special vendor later).
                ItemCatalog.ScrollReturn,
                // Scroll of Resurrection (1500g) — revive a dead ally (no exp restored). The
                // Ultimate resurrection scroll (restores all lost exp) is NOT sold here.
                ItemCatalog.ScrollResurrect,
                // Reagents: Skill Stone (400g — Angel's Protection etc.) + Elemental Stone (20k — nuker burst).
                ItemCatalog.SkillStone,
                ItemCatalog.ElementalStone,
                // Rune boxes — 1h/2h only (24h/30d are premium/pass, debug-only for now). Fighters buy the
                // War Rune box, mages the Spell Rune; anyone may buy either (e.g. a buffer for melee).
                ItemCatalog.BoxWarRune1h,
                ItemCatalog.BoxWarRune2h,
                ItemCatalog.BoxSpellRune1h,
                ItemCatalog.BoxSpellRune2h,
                // The Rune of Tincture — the only source of a title COLOUR since `59r` made it a thing
                // you spend rather than a free command. Sold rather than dropped: it is pure cosmetic,
                // so a gold sink is exactly what it should be.
                ItemCatalog.TitleColorRune,
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
            }.Concat(WeaponsOf(ladderGear)).ToArray()),

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
            }.Concat(ArmorOf(ladderGear)).ToArray()),
        };

        var dict = new Dictionary<string, ShopDef>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in shops)
            dict[s.NpcId] = s;

        // EVERY main town sells the same three inventories (owner, 2026-07-29). The ring towns' vendor
        // ids are the Brackenford id plus a town suffix (see WorldMap.RingTownServices), so the stock is
        // shared by REFERENCE rather than copied — one list to edit, and a town can never quietly end up
        // selling last month's catalogue. Anything town-specific later just overrides its own key.
        foreach (var npc in WorldMap.Npcs)
        {
            if (npc.Role != NpcRole.Vendor || dict.ContainsKey(npc.Id)) continue;
            int cut = npc.Id.LastIndexOf('_');
            if (cut <= 0) continue;
            string baseId = npc.Id.Substring(0, cut);
            if (dict.TryGetValue(baseId, out var template))
                dict[npc.Id] = template with { NpcId = npc.Id };
        }
        return dict;
    }

    public static ShopDef? Get(string? npcId) =>
        npcId is null ? null : Shops.GetValueOrDefault(npcId);

    public static bool Sells(string npcId, string itemId) =>
        Get(npcId) is ShopDef shop && Array.IndexOf(shop.ItemIds, itemId) >= 0;
}
