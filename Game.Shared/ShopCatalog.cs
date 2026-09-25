using System.Linq;

namespace Game.Shared;

/// <summary>
/// A vendor's wares: the item ids it sells. Buy prices come from each item's
/// Value (ItemCatalog.BuyPrice); the shop only decides WHAT is for sale. Selling
/// is independent — any vendor buys back any sellable item the player owns.
/// </summary>
/// <para>`BL-272` part 2: <paramref name="EssenceOnly"/> makes a shelf charge ESSENCE and no gold — every row
/// is priced by <see cref="Crafting.EssenceShopPrice"/>, and a row with no essence price is not for sale
/// there. Only the T52 essence shop sets it.</para>
/// <para>`BL-290`: <paramref name="Tabs"/> is the shop's OWN tab strip on the BUY side (*"can we make npc vendors
/// their tabs to be custom per vendor ?"*). Null = the generic All / Gear / Use / Mats. The client puts an
/// <b>All</b> tab in front of them itself, so a shop never declares one. The SELL side always keeps the
/// generic tabs: it lists your bag, and "Ring" or "Scrolls" would hide most of it.</para>
public record ShopDef(string NpcId, string Title, string[] ItemIds, bool EssenceOnly = false,
                      ShopTab[]? Tabs = null);

/// <summary>One of a shop's own buy tabs (`BL-290`): a name and the test for what sits under it. A BOX sits
/// under every tab that holds something inside it (<see cref="ShopCatalog.InTab"/>), so the temporary
/// weapon box shows on each weapon tab and the armour box on each armour one, with nothing typed per box.</summary>
public record ShopTab(string Name, Func<ItemDef, bool> Holds);

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
    /// <summary>The T52 essence shop (`BL-272` part 2) — one NPC, in Greymarsh.</summary>
    public const string EssenceMerchant = "merchant_essence";

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
        // are crafted, dropped or taken off a boss.
        //
        // 🔑 `BL-272` (2026-09-23): the shop sells the MYTHIC piece, at its full authored price
        // (ItemCatalog.TieredGearPrice). It used to sell the Common/Uncommon/Rare copies at a cut, and
        // those rungs no longer exist: *"Weapon / armour / jewellery merchants sell Mythic T1, T20 and
        // T40"*. Only the plain base-tier ids are stocked, the same lines the old copies were made of
        // (the alternate set VARIANTS such as "heavy_t40_dmg" stay off the shelf, as they always were).
        // The T52 essence shop and the 2-hour Common boxes are `BL-272` part 2.
        var shopGrades = new[] { ItemCatalog.FGradeLevel, 20, 40 };
        var ladderGear = ItemCatalog.AllItems
            .Where(d => shopGrades.Contains(d.ItemLevel)
                && d.Rarity == ItemRarity.Mythic
                && ItemCatalog.IsBaseTier(d.Id)
                && d.Slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel
                )
            .OrderBy(d => d.ItemLevel).ThenBy(d => d.Slot).ThenBy(d => d.Name)
            .ToArray();

        string[] WeaponsOf(params ItemDef[][] sets) =>
            sets.SelectMany(s => s).Where(IsWeapon).Select(d => d.Id).ToArray();
        string[] ArmorOf(params ItemDef[][] sets) =>
            sets.SelectMany(s => s).Where(d => !IsWeapon(d)).Select(d => d.Id).ToArray();

        // `BL-290` — each shop's OWN buy tabs, from his note: *"Apothecary to have like (pots,scrolls,misc) ; armor
        // vendors to have (body,helm,gloves,boots,Shield,neck,ring,ear); etc... The new npc that sells cobold for
        // essence can have (armor,jewels,weapons)"*. The Armsmaster's "etc." is read as one tab per weapon kind.
        // ⚠ The Training Wand carries no IsMagicWeapon (it is a plain Blunt with M.Atk 7), so it is named here,
        // or it would sit under "Blunt" beside the maces.
        static bool Magic(ItemDef d) =>
            d.Slot == EquipSlot.Weapon && (d.IsMagicWeapon || d.Id == ItemCatalog.TrainingWand);
        static bool Wields(ItemDef d, WeaponType t) => d.Slot == EquipSlot.Weapon && !Magic(d) && d.WeaponType == t;
        // Potions, scrolls and reagents are ALL EquipSlot.Consumable (a Scroll of Return is a "BuffPotion" by
        // subtype), so the id prefix is what tells them apart; every "scroll_" id is named "Scroll of …".
        static bool Potion(ItemDef d) => d.Slot == EquipSlot.Consumable && d.Id.StartsWith("potion_");
        static bool Scroll(ItemDef d) => d.Slot == EquipSlot.Scroll
            || (d.Slot == EquipSlot.Consumable && d.Id.StartsWith("scroll_"));
        static bool Worn(ItemDef d, ArmorSlot s) => d.Slot == EquipSlot.Armor && d.ArmorSlot == s;
        var apothecaryTabs = new ShopTab[]
        {
            new("Potions", Potion),
            new("Scrolls", Scroll),
            new("Misc", d => !Potion(d) && !Scroll(d)),
        };
        var weaponTabs = new ShopTab[]
        {
            // 0.210.1 — four GROUPS, not eight kinds (his follow-up: *"sword 1+2h, blunt 1+2h, magic wand+staff,
            // rogue(or whatever group name u think of) bow+dual"*). The last is labelled by what is in it, not by
            // a class: bows belong to the archer path too.
            new("Sword", d => Wields(d, WeaponType.Sword) || Wields(d, WeaponType.TwoHandedSword)),
            new("Blunt", d => Wields(d, WeaponType.Blunt) || Wields(d, WeaponType.TwoHandedBlunt)),
            new("Magic", Magic),
            new("Bow/Dual", d => Wields(d, WeaponType.Bow) || Wields(d, WeaponType.Dual)),
        };
        var armorTabs = new ShopTab[]
        {
            // 0.210.1 — body / the rest of the set / shield / jewels (his first layout). "Pieces", not "Parts":
            // parts are a crafting material.
            new("Body", d => Worn(d, ArmorSlot.Body)),
            new("Pieces", d => Worn(d, ArmorSlot.Head) || Worn(d, ArmorSlot.Gloves) || Worn(d, ArmorSlot.Boots)),
            new("Shield", d => d.Slot == EquipSlot.Shield),
            new("Jewels", d => d.Slot == EquipSlot.Jewel),
        };
        var essenceTabs = new ShopTab[]
        {
            new("Armor", d => d.Slot is EquipSlot.Armor or EquipSlot.Shield),
            new("Jewels", d => d.Slot == EquipSlot.Jewel),
            new("Weapons", d => d.Slot == EquipSlot.Weapon),
        };

        var shops = new[]
        {
            new ShopDef(PotionMerchant, "Apothecary", new[]
            {
                // Healing potions — Common + Uncommon only. The RARE tier (GreaterPotion) is removed from
                // the vendor (owner): rare potions should come from drops/rewards, not gold, matching the
                // potion-economy design where the top tier stays out of the shop.
                ItemCatalog.MinorPotion,
                ItemCatalog.HealingPotion,
                // MANA potions, same rule (owner, 2026-08-27: *"Common in shop / uncommon shop /
                // rare drop"*) — so the Rare Mana Potion is deliberately absent here too.
                ItemCatalog.MinorManaPotion,
                ItemCatalog.ManaPotion,
                // Common buff potions. Individual scrolls are NOT sold — see the Blessing Box below.
                //
                // 🔑 COMMON ONLY, AND THAT IS THE WHOLE RULE (owner, playtest 28: *"the shop can supply
                // common only and the crafter can supply the rest"*). ⚠ His "apothecary masters" meant
                // the **Potion Master profession**, not this NPC — I read it as the shelf first and
                // stocked the Uncommon rungs here, which he corrected. So the three sources are now:
                //   • DROPS — Swift, Alacrity, Fury and Dash, and nothing else.
                //   • THIS SHELF — the Common rung of all nine families, for gold.
                //   • A PLAYER Potion Master — Common at craft L2, **Uncommon at craft L4**, which is
                //     the only source of an Uncommon Agility/Might/Bulwark/Force/Ward/Aim potion in the
                //     game now that they have left the loot tables.
                // That third line is the point of the change: it hands a whole rung of a consumable to
                // the player economy instead of to a vendor, and buff potions are tradable (they always
                // were — see the `SellPriceOverride: 0` note in Items.cs) precisely so he can sell it.
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
                // The holy and fighter twins of the Elemental Stone, same 20k shelf price (2026-08-26).
                // ⚠ The SP BOTTLE is deliberately NOT here: the SP Broker in Frostmere is its only
                // source, because its price is SP as well as gold and a shelf cannot charge that.
                ItemCatalog.HolyStone,
                ItemCatalog.PhysicalStone,
                // Rune boxes — 1h/2h only (24h/30d are premium/pass, debug-only for now). Fighters buy the
                // War Rune box, mages the Spell Rune; anyone may buy either (e.g. a buffer for melee).
                ItemCatalog.BoxWarRune1h,
                ItemCatalog.BoxWarRune2h,
                ItemCatalog.BoxSpellRune1h,
                ItemCatalog.BoxSpellRune2h,
                // ⚠ THE RUNE OF TINCTURE IS NO LONGER SOLD HERE (owner, playtest-21 `63i`: *"remove the
                // Rune of Tincture from the Apothecary — it will be only event/premium bought"*). It
                // stays a real item with a real use; what changed is that gold is not how you get one,
                // so the title colour is a thing given out, not a thing farmed for. Admin `/give` is
                // the only in-game source until the event/premium shop exists.
                // NOTE: enchant + attribute scrolls are intentionally DROP-ONLY (not sold).
                //
                // THE RITE OF ASCENSION — 100kk, the 4th class (owner, 2026-08-17: *"now can be
                // without quest but go in the apothecary and buy a 100kk 4th_class_item and go to
                // class master with it"*). It sits here as the INTERIM gate: the long quest chain
                // replaces the purchase later, and on that day this one line comes out and the item
                // becomes the chain's reward instead. Deliberately at the Apothecary rather than
                // beside the class master — the gold is the trial for now, and it should cost a walk.
                ItemCatalog.FourthClassKey,
            }, Tabs: apothecaryTabs),
            // WEAPONS. The LEGACY generated grid ("Worn Sword" at P.Atk 6, the Fine/Masterwork
            // prefixes) plus Ash Wand and Iron Mace came off this shelf in playtest-13 and were
            // DELETED OUTRIGHT in playtest-22 — taking them out of the shop only hid them; a treasure
            // chest was still handing one out a year later.
            new ShopDef(GearMerchant, "Armsmaster — Weapons", new[]
            {
                // TRAINING tier (400g each) — the level 1-10 gear. Stocked so a new player who picked the
                // wrong weapon, or lost one, can just buy another instead of being stuck with it.
                ItemCatalog.TrainingSword,
                ItemCatalog.TrainingWand,
            }.Concat(WeaponsOf(ladderGear))
             // `BL-272` part 2: the temporary 2-hour Common weapon box, T40 and T52 (pick one weapon).
             .Concat(ItemCatalog.TempGearTiers.Select(ItemCatalog.TempWeaponBoxId)).ToArray(), Tabs: weaponTabs),

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
                // (Brass Amulet removed 2026-08-13 with the def — *"Brass amulet also need to be gone"*.
                //  The three Broken jewels ARE the pre-F rung; a second, off-ladder necklace beside them
                //  was the same drift 72a caught in the jewels themselves.)
                ItemCatalog.WoodenShield,
            }.Concat(ArmorOf(ladderGear))
             // `BL-272` part 2: the temporary 2-hour Common armour box, T40 and T52 (pick heavy / light /
             // robe; every set carries a shield). No temporary jewellery (ruled).
             .Concat(ItemCatalog.TempGearTiers.Select(ItemCatalog.TempArmorBoxId)).ToArray(), Tabs: armorTabs),

            // `BL-272` part 2 — THE T52 ESSENCE SHOP. One NPC, in Greymarsh (the 40-60 town; his pick
            // 2026-09-24: *"A single new NPC in a T52 town"*). T52 Mythic, every slot, essence ONLY: no
            // gold and no mats (*"so breaking like crazy T40 can get u a t52"*). Prices are
            // Crafting.EssenceShopPrice. T61+ stays drop/craft only.
            new ShopDef(EssenceMerchant, "Assayer — Cobalt for Essence", ItemCatalog.AllItems
                .Where(d => Crafting.EssenceShopPrice(d) is not null)
                .OrderBy(d => d.Slot).ThenBy(d => d.Name)
                .Select(d => d.Id).ToArray(), EssenceOnly: true, Tabs: essenceTabs),

            // `BL-273` part 2 — THE MASTER CRAFTER'S RECIPE SHELF: the T40 and T52 100% gear recipes (*"master can
            // sell t40 and t52"*), at a placeholder 10% of the piece's price. Generic recipes are not items; he
            // teaches those directly (LearnRecipeAtMaster).
            new ShopDef(WorldMap.CraftMasterId, "Master Crafter — Recipes", ItemCatalog.AllItems
                .Where(d => d.RecipePercent == 100 && d.TeachesRecipeId.Length > 0
                    && RecipeCatalog.Get(d.TeachesRecipeId) is { IsGear: true } r && Crafting.MasterSellsRecipeFor(r.GearItemLevel))
                .OrderBy(d => RecipeCatalog.Get(d.TeachesRecipeId)!.GearItemLevel).ThenBy(d => d.Name)
                .Select(d => d.Id).ToArray()),
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
            if (npc.Role is not (NpcRole.Vendor or NpcRole.CraftMaster) || dict.ContainsKey(npc.Id)) continue;
            int cut = npc.Id.LastIndexOf('_');
            if (cut <= 0) continue;
            string baseId = npc.Id.Substring(0, cut);
            if (dict.TryGetValue(baseId, out var template))
                dict[npc.Id] = template with { NpcId = npc.Id };
        }
        return dict;
    }

    /// <summary>Every shop. For the tools that have to ask "is this item on a shelf ANYWHERE" rather
    /// than "does this one NPC sell it" — `BL-147`'s generated consumable-buff page is the first, and
    /// the question only has an honest answer if it can see all the shelves at once.</summary>
    public static IEnumerable<ShopDef> AllShops => Shops.Values;

    public static ShopDef? Get(string? npcId) =>
        npcId is null ? null : Shops.GetValueOrDefault(npcId);

    /// <summary>Does this item sit under this shop tab (`BL-290`)? A box with contents is judged by what is INSIDE
    /// it, never by itself, so a box of scrolls is a scroll and a box of armour is on each piece's tab.</summary>
    public static bool InTab(ShopTab tab, ItemDef def) => InTab(tab, def, 0);

    private static bool InTab(ShopTab tab, ItemDef def, int depth)
    {
        if (def.Slot == EquipSlot.Box && depth < 4 && BoxCatalog.Get(def.Id) is { Entries.Length: > 0 } box)
            return box.Entries.Any(e => ItemCatalog.Get(e.ItemId) is { } d && InTab(tab, d, depth + 1));
        return tab.Holds(def);
    }

    public static bool Sells(string npcId, string itemId) =>
        Get(npcId) is ShopDef shop && Array.IndexOf(shop.ItemIds, itemId) >= 0;
}
