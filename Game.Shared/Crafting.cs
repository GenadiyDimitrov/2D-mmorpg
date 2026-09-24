namespace Game.Shared;

/// <summary>The three crafting TYPES (`BL-273` part 2, 0.203.0) plus the generic bucket. There are NO
/// professions any more (owner, 2026-09-23: *"As we remove the professions we have no lock and no way to
/// disable crafting once the quest is done"*): anyone who finishes the level-40 crafter quest crafts
/// everything, and the TYPE levels are what tell a weaponsmith from an armorer. Potions, scrolls and
/// refines are <see cref="General"/>: they raise only the generic level.</summary>
public enum CraftType { General = 0, Weapon = 1, Armour = 2, Jewels = 3 }

/// <summary>The 5 crafting material types. Every rarity DROPS from mobs, and anyone who has the refine
/// recipe can refine it. ⚠ Replaced by Nightsilver / Nightsilk in `BL-273` part 3 (step 10).</summary>
public enum MaterialType { Ingot = 0, Thread, Wood, Leather, Gem }

public static class Crafting
{
    /// <summary>Material rarities — the FULL six, matching <see cref="ItemRarity"/> exactly. The old
    /// material ladder; it lives until `BL-273` part 3 replaces the mats.</summary>
    public static readonly ItemRarity[] MaterialRarities =
        { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare,
          ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic };

    public static readonly MaterialType[] MaterialTypes =
        { MaterialType.Ingot, MaterialType.Thread, MaterialType.Wood, MaterialType.Leather, MaterialType.Gem };

    /// <summary>Stable item id for a material of a type + rarity, e.g. "mat_gem_rare".</summary>
    public static string MaterialId(MaterialType type, ItemRarity rarity) =>
        $"mat_{type.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    /// <summary>Display name, e.g. "Rare Gem".</summary>
    public static string MaterialName(MaterialType type, ItemRarity rarity) =>
        $"{rarity} {type}";

    // =====================================================================================
    //  BECOMING A CRAFTER (`BL-273` part 2, 0.203.0) — design doc §2.2, all rounds.
    // =====================================================================================

    /// <summary>The character level the crafter quest is offered at (*"do a quest @40"*).</summary>
    public const int CrafterQuestLevel = 40;

    /// <summary>The top of the generic level AND of each type level. Both run 0-10 (*"start at lvl 0 with
    /// 10 slots and 0%"*).</summary>
    public const int MaxCraftLevel = 10;

    /// <summary>Recipe slots at generic level 0, and what each generic level adds: L0 = 10, L10 = 60.
    /// Only the GENERIC level gives slots; the type levels give % only.</summary>
    public const int BaseSlots = 10, SlotsPerLevel = 5;

    /// <summary>+0.5% success per level, generic and type alike, so a maxed crafter carries +10% on his
    /// own type (5% + 5%).</summary>
    public const float BonusPerLevel = 0.005f;

    /// <summary>The bonus counts ONLY on crafts at or above this item level (T76/T80): *"u need alot of
    /// grinding to get to that bonus so a 100% rcp don't get u anything it's a more of a decrease of
    /// losses when crafting high gear"*.</summary>
    public const int BonusFromItemLevel = 76;

    /// <summary>Recipe slots at a generic level.</summary>
    public static int Slots(int genericLevel) =>
        BaseSlots + SlotsPerLevel * System.Math.Clamp(genericLevel, 0, MaxCraftLevel);

    /// <summary>The success bonus a crafter adds to a recipe's own %: (generic + type) × 0.5%, and only on
    /// T76/T80 gear. Generic recipes have no type level, so pass 0 for it.</summary>
    public static float SuccessBonus(int itemLevel, int genericLevel, int typeLevel) =>
        itemLevel >= BonusFromItemLevel
            ? BonusPerLevel * (System.Math.Clamp(genericLevel, 0, MaxCraftLevel)
                               + System.Math.Clamp(typeLevel, 0, MaxCraftLevel))
            : 0f;

    /// <summary>Craft points one ATTEMPT is worth (his pick, 2026-09-24: tier-weighted, and a FAIL counts).
    /// Gear by its tier: T40 1 · T52 2 · T61 3 · T76 5 · T80 8. A generic recipe (potion, scroll, refine) is
    /// 1 and feeds the generic level only. ⚠ Playtest placeholders.</summary>
    public static int CraftPoints(int gearItemLevel) => gearItemLevel switch
    {
        >= 80 => 8,
        >= 76 => 5,
        >= 61 => 3,
        >= 52 => 2,
        _ => 1,
    };

    /// <summary>Cumulative points at which a craft level begins: level N costs 20·N more than N-1, so
    /// L1 = 20, L2 = 60, L3 = 120 … L10 = 1100. The generic and the type levels read the same table.
    /// ⚠ Playtest placeholders (design doc §2.2, 0.203.0 answers #1).</summary>
    public static int PointsForLevel(int level) =>
        10 * System.Math.Clamp(level, 0, MaxCraftLevel) * (System.Math.Clamp(level, 0, MaxCraftLevel) + 1);

    /// <summary>The craft level a point total is worth, 0-10.</summary>
    public static int LevelForPoints(int points)
    {
        int lvl = 0;
        while (lvl < MaxCraftLevel && points >= PointsForLevel(lvl + 1)) lvl++;
        return lvl;
    }

    /// <summary>Which type a crafted output levels: weapons → Weapon, armour and shields → Armour, jewels
    /// → Jewels, and everything else (potions, scrolls, mats) → General.</summary>
    public static CraftType TypeOf(ItemDef? def) => def?.Slot switch
    {
        EquipSlot.Weapon => CraftType.Weapon,
        EquipSlot.Armor or EquipSlot.Shield => CraftType.Armour,
        EquipSlot.Jewel => CraftType.Jewels,
        _ => CraftType.General,
    };

    // ----- RECIPE % (design doc §2.2 #2) -------------------------------------------------------------
    // A gear recipe ITEM carries a success % (20/40/60/100). Learning one fills a slot at that %; a
    // higher % overrides the slot. Crafting spends ONE recipe item of any % at or below the learned one,
    // and the attempt succeeds at the USED item's % (*"i can use 20% when slot is 100% -> the success rate
    // is 20%"*), plus the T76/T80 bonus. Every input is scaled by the used % on the mat curve
    // 20 → 30% · 40 → 50% · 60 → 70% · 100 → 100%, so a maxed crafter pays the 100% cost per success.

    /// <summary>The four recipe percentages.</summary>
    public static readonly int[] RecipePercents = { 20, 40, 60, 100 };

    /// <summary>The fraction of every input a recipe of this % costs.</summary>
    public static float MatFraction(int percent) => percent switch
    {
        <= 20 => 0.3f,
        <= 40 => 0.5f,
        <= 60 => 0.7f,
        _ => 1f,
    };

    /// <summary>An input quantity scaled by the recipe % used, rounded half away from zero and never
    /// below 1 (20 heads at 20% = 6).</summary>
    public static int ScaledQty(int qty, int percent) =>
        percent >= 100 ? qty
        : System.Math.Max(1, (int)System.Math.Round(qty * MatFraction(percent), System.MidpointRounding.AwayFromZero));

    /// <summary>Which recipe %s exist as ITEMS for a gear tier: the ones something gives out (design doc
    /// §2.2, 0.203.0 answers #6). T40/T52 100% (shop + drops); T61 60 (normal) and 100 (elite/boss); T76 20
    /// (normal), 40 (elite, daily quest), 60 (boss); T80 40 (elite, quest), 60 (boss).</summary>
    public static int[] RecipePercentsFor(int itemLevel) => itemLevel switch
    {
        >= 80 => new[] { 40, 60 },
        >= 76 => new[] { 20, 40, 60 },
        >= 61 => new[] { 60, 100 },
        _ => new[] { 100 },
    };

    /// <summary>The lowest gear tier with a recipe. F and E gear is not crafted any more; the rework's
    /// tables start at T40.</summary>
    public const int MinCraftedGearLevel = 40;

    /// <summary>What the Master charges for a T40/T52 100% recipe, as a fraction of the item's own buy
    /// price. ⚠ A placeholder (he ruled that the master sells them, not the price).</summary>
    public const float ShopRecipePriceFraction = 0.10f;

    /// <summary>The gear tiers whose 100% recipe the Master sells (*"master can sell t40 and t52"*).</summary>
    public static bool MasterSellsRecipeFor(int itemLevel) => itemLevel is 40 or 52;

    /// <summary>The generic level at which the Master offers a generic recipe for sale, mapped from the old
    /// crafting rung 1-6 it used to sit on (1 → 0, 2 → 2 … 6 → 10). ⚠ A placeholder until step 9b's table.</summary>
    public static int GenericUnlockLevel(int oldRung) => System.Math.Clamp(2 * (oldRung - 1), 0, MaxCraftLevel);

    /// <summary>What learning a generic recipe costs at the Master: 20,000 × (1 + its unlock level).
    /// ⚠ A placeholder until step 9b's table.</summary>
    public static int GenericLearnPrice(int unlockLevel) => 20_000 * (1 + unlockLevel);

    // ----- THE CRAFTER QUEST'S OWN RECIPE --------------------------------------------------------------

    /// <summary>The Blacksmith's Hammer recipe, the one thing a non-crafter may learn and craft (only while
    /// his crafter quest is on its craft step). It takes no slot and is forgotten when the quest completes.</summary>
    public const string HammerRecipeId = "craft_crafter_hammer";

    /// <summary>The % of the quest recipe item (*"2 recipes (40% less punishing)"*).</summary>
    public const int HammerRecipePercent = 40;

    // =====================================================================================
    //  ESSENCE (`BL-273` part 1, 0.200.0; tables rewritten by `BL-287`, 0.201.0) — breaking gear gives
    //  its GRADE's essence, nothing else.
    // =====================================================================================
    //
    // His rule (2026-09-23, design doc §2.4): *"breaking common or even mythic darksteel gives you
    // 'Darksteel essence' (Cobolt -> 'Cobolt Essence' ... etc...) - and crafting to require also this
    // essence that is aquired only by breaking full items -> so not mindlessly selling in the vendor"*.
    // It replaced the `BL-22` disassembly roll, which is deleted outright, not kept beside it.
    //
    // 🔑 THE AMOUNT IS AUTHORED, NEVER COMPUTED. *"changing prices later should not change the essence
    //    amount"*. The tables below are literals on purpose: retuning a price must not move a single cell
    //    here, and nothing reads a price to produce them.
    //
    // How they were written (`BL-287`, owner, 2026-09-24, design doc §2.5 — generated ONCE from the
    // 0.201.0 prices, so the next cell can be written the same way):
    //   * BREAK = 0.4 × the item's own BUY price ÷ its essence's SELL price, rounded half away from zero.
    //     Selling returns 50%, breaking 40% (as essence at its sell price): *"breaking it down gives less
    //     than its actual sell price"*, and *"one common item or 2 [must] not allow me to craft an item"*.
    //   * Mythic and Common alike (a Common is 0.05 × its Mythic, so it breaks for 1/20th); the 0.200.0
    //     "Common breaks for 70%" is gone. T76/T80 have no Commons (their essence DROPS, step 12).
    //   * The essence sell prices are his: D 1500 / C 4500 / B 7500 / A 12500 / S 25000.
    //
    // ⚠ F and E gear (T1 / T20) CANNOT be broken (ruled 2026-09-24): the five essences are D..S, and a
    //   Ferrite or Electrum piece is sold, not broken.

    /// <summary>The five grade essences, index 0 = D (Darksteel) … 4 = S (Soulcrystal). The id is stable;
    /// the display name is "{GradeTheme} Essence".</summary>
    public static readonly string[] EssenceIds =
        { "essence_d", "essence_c", "essence_b", "essence_a", "essence_s" };

    /// <summary>The item level each essence is named after (its <see cref="ItemCatalog.GradeTheme"/>).</summary>
    public static readonly int[] EssenceItemLevels = { 40, 52, 61, 76, 80 };

    /// <summary>What a vendor pays for ONE essence (`BL-287`, his numbers): D 1500 / C 4500 / B 7500 /
    /// A 12500 / S 25000. The break tables were authored against these. The essence's <c>Value</c> is twice
    /// this, so the ordinary half-price sell rule pays exactly this number; that Value is a yardstick only,
    /// never a shop price (*"no vendor sells essence"*).</summary>
    public static readonly int[] EssenceSellPrice = { 1_500, 4_500, 7_500, 12_500, 25_000 };

    /// <summary>Which essence grade an item level breaks into, or -1 for F/E (no essence).</summary>
    public static int EssenceGrade(int itemLevel) => itemLevel switch
    {
        >= 80 => 4, >= 76 => 3, >= 61 => 2, >= 52 => 1, >= 40 => 0, _ => -1,
    };

    // Column order of the two tables: 2H, 1H, body, helm/shield, gloves/boots, necklace, earring, ring.
    private static readonly int[][] MythicBreak =
    {
        new[] { 1143, 1029,  686,  381,  229,  571,  190,  95 },   // T40 Darksteel   (2H 4.29M ÷ 1500)
        new[] { 1200, 1080,  720,  400,  240,  600,  200, 100 },   // T52 Cobalt      (2H 13.5M ÷ 4500)
        new[] { 3200, 2880, 1920, 1067,  640, 1600,  533, 267 },   // T61 Bloodsteel  (2H 60M ÷ 7500)
        new[] { 3840, 3456, 2304, 1280,  768, 1920,  640, 320 },   // T76 Adamantine  (2H 120M ÷ 12500)
        new[] { 9600, 8640, 5760, 3200, 1920, 4800, 1600, 800 },   // T80 Soulcrystal (2H 600M ÷ 25000)
    };

    private static readonly int[][] CommonBreak =
    {
        new[] {  57,  51,  34,  19,  11,  29,  10,   5 },   // T40
        new[] {  60,  54,  36,  20,  12,  30,  10,   5 },   // T52
        new[] { 160, 144,  96,  53,  32,  80,  27,  13 },   // T61
    };

    /// <summary>Which column of the break tables a piece of gear reads, or -1 if it is not gear.</summary>
    private static int BreakColumn(ItemDef def) => def.Slot switch
    {
        EquipSlot.Weapon => def.WeaponType.IsTwoHanded() ? 0 : 1,
        EquipSlot.Shield => 3,
        EquipSlot.Armor => def.ArmorSlot switch
        {
            ArmorSlot.Body => 2,
            ArmorSlot.Head => 3,
            ArmorSlot.Gloves or ArmorSlot.Boots => 4,
            _ => -1,
        },
        EquipSlot.Jewel => def.JewelType switch
        {
            JewelType.Necklace => 5,
            JewelType.Earring => 6,
            JewelType.Ring => 7,
            _ => -1,
        },
        _ => -1,
    };

    /// <summary>What breaking an item gives: an essence id and how many.</summary>
    public readonly record struct EssenceYield(string EssenceId, int Qty);

    /// <summary>The authored essence a piece of gear breaks into (`BL-273`), or null if it cannot be
    /// broken: not gear, untiered (quest/debug one-offs), F/E grade, or a rarity the tables do not
    /// hold. Mythic reads <c>MythicBreak</c>, Common reads <c>CommonBreak</c>; there is no third rung.
    /// Server and client both ask this, so the Break button can never offer what the server refuses.</summary>
    public static EssenceYield? BreakYield(ItemDef? def)
    {
        if (def is null || def.ItemLevel <= 0) return null;
        // Temporary 2-hour gear (`BL-272` part 2) never breaks: it is bought with gold, and a bought
        // piece that broke into essence would be a vendor selling essence (*"no vendor sells essence"*).
        if (def.WornLifetimeSeconds > 0) return null;
        int grade = EssenceGrade(def.ItemLevel);
        int col = BreakColumn(def);
        if (grade < 0 || col < 0) return null;
        int[][] table = def.Rarity switch
        {
            ItemRarity.Mythic => MythicBreak,
            ItemRarity.Common => CommonBreak,
            _ => Array.Empty<int[]>(),
        };
        if (grade >= table.Length) return null;
        int qty = table[grade][col];
        return qty > 0 ? new EssenceYield(EssenceIds[grade], qty) : null;
    }

    // ----- THE T52 ESSENCE SHOP (`BL-272` part 2, 0.202.0) -------------------------------------------
    // His rulings: *"The T52 dedicated shop costs ESSENCE, not craft mats … 500 T52 essence (¼ of its
    // worth) plus the other ¾ in T40 essence (~5-10k) … so breaking like crazy T40 can get u a t52"*; third
    // round: **essence only, no gold**. Re-read after `BL-287` moved the break values (2026-09-24, his pick
    // of three): ¼ of the item's BUY price in Cobalt essence and ¾ in Darksteel, each at its essence's SELL
    // price, so the essence handed over is worth the item's full price. The T52 2H: 13.5M → 750 C + 6750 D.
    // 🔑 AUTHORED, like the break tables: generated once from the 0.202.0 prices (C = 0.25 × buy ÷ 4500,
    //    D = 0.75 × buy ÷ 1500, rounded half away from zero) and never recomputed from a live price.
    //    Same column order as the break tables.
    private static readonly int[] T52ShopCobalt    = {  750,  675,  450,  250,  150,  375,  125,  63 };
    private static readonly int[] T52ShopDarksteel = { 6750, 6075, 4050, 2250, 1350, 3375, 1125, 563 };

    /// <summary>The item level the essence shop sells.</summary>
    public const int EssenceShopTier = 52;

    /// <summary>What the essence shop charges for a piece, as (essence id, qty) pairs, or null if the piece
    /// is not on its shelf: a T52 Mythic base-tier weapon, armour piece, shield or jewel.</summary>
    public static EssenceYield[]? EssenceShopPrice(ItemDef? def)
    {
        if (def is null || def.ItemLevel != EssenceShopTier || def.Rarity != ItemRarity.Mythic
            || !ItemCatalog.IsBaseTier(def.Id)) return null;
        int col = BreakColumn(def);
        if (col < 0) return null;
        return new[]
        {
            new EssenceYield(EssenceIds[EssenceGrade(EssenceShopTier)], T52ShopCobalt[col]),
            new EssenceYield(EssenceIds[0], T52ShopDarksteel[col]),
        };
    }

    /// <summary>What a SHATTERED enchant leaves behind (`BL-273`, second round): failing +N → N+1 with a
    /// Normal scroll returns <b>N × 10%</b> of the item's break value (+3→4 = 30%, +10→11 = 100%,
    /// +15→16 = 150%). *"if u invest in safe enchants just to break it .. good for u"* — enchanting up to
    /// break is intended. Only a Normal scroll destroys the item, so only that path calls this; Commons
    /// cannot be enchanted, so in practice it is Mythic only. Rounded down.</summary>
    public static EssenceYield? ShatterYield(ItemDef? def, int enchantBefore)
    {
        if (BreakYield(def) is not EssenceYield y || enchantBefore <= 0) return null;
        int qty = y.Qty * enchantBefore / 10;
        return qty > 0 ? y with { Qty = qty } : null;
    }

    /// <summary>True if a slot is GEAR (a recipe-% craft of weapon, armour, shield or jewel) rather than a
    /// material or consumable (a plain <see cref="Recipe.SuccessChance"/> roll).</summary>
    public static bool IsGearSlot(EquipSlot slot) =>
        slot is EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel;
}
