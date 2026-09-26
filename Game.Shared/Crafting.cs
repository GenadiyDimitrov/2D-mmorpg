namespace Game.Shared;

/// <summary>The five crafting TYPES plus the generic bucket (`BL-273`, the crafter-points model, 0.204.0).
/// There is no profession lock (owner, 2026-09-23): anyone who finishes the level-40 crafter quest is a
/// crafter. Crafting raises the GENERIC level, and each generic level gives one POINT, which the crafter
/// spends on a type (*"at L10 generic u are a L10 single type or L2 on 5types"*). The type level is what
/// tells a weaponsmith from an apothecary. <see cref="General"/> is the refines and the trial's hammer; its
/// "type level" is the generic level itself.</summary>
public enum CraftType { General = 0, Weapon = 1, Armour = 2, Jewels = 3, Apothecary = 4, Scribe = 5 }   // Scribe: retired into the Apothecary (`BL-305`)

/// <summary>The five BASE materials (`BL-273` part 3, 0.205.0): one rung each, no rarity ladder. The old
/// Uncommon…Mythic rungs are gone; what climbs a ladder now is Nightsilver / Nightsilk. <see cref="Iron"/>
/// is both of the note's *metal* (the bulk) and its *iron* (the alloy) — owner, 2026-09-24: one mat, not
/// two near-synonyms — and gems are the harder one to farm.</summary>
public enum MaterialType { Iron = 0, Thread, Wood, Leather, Gem }

public static class Crafting
{
    public static readonly MaterialType[] MaterialTypes =
        { MaterialType.Iron, MaterialType.Thread, MaterialType.Wood, MaterialType.Leather, MaterialType.Gem };

    /// <summary>Stable item id for a base material, e.g. "mat_gem".</summary>
    public static string MaterialId(MaterialType type) => $"mat_{type.ToString().ToLowerInvariant()}";

    /// <summary>Display name, e.g. "Gem".</summary>
    public static string MaterialName(MaterialType type) => type.ToString();

    // =====================================================================================
    //  THE REFINABLE METAL AND CLOTH (`BL-273` part 3, 0.205.0) — design doc §2.2 point 4 and the
    //  step-10 answers. Five rungs each, 10 of a rung refine into 1 of the next.
    // =====================================================================================

    /// <summary>The rung names, index 0 (normal, no prefix) … 4 (Legendary). A gear tier eats the rung of its
    /// own index: T40 normal · T52 Refined · T61 Rare · T76 Refined Rare · T80 Legendary.</summary>
    public static readonly string[] RefineRungPrefix = { "", "Refined ", "Rare ", "Refined Rare ", "Legendary " };

    /// <summary>Nightsilver: weapons, earrings and rings. Id "nightsilver_0" … "nightsilver_4".</summary>
    public static string NightsilverId(int rung) => $"nightsilver_{rung}";

    /// <summary>Nightsilk: armour, shields and necklaces. Id "nightsilk_0" … "nightsilk_4".</summary>
    public static string NightsilkId(int rung) => $"nightsilk_{rung}";

    public const int RefineRungs = 5;

    /// <summary>Refining is 10 of a rung into 1 of the next (the note's *"10 of lower = 1 of higher"*).</summary>
    public const int RefineRatio = 10;

    /// <summary>What each refine STEP costs in MP (the note: *"normal to refined/refined to rare/ ... ->
    /// 50/100/150/200"*), index = the rung refined INTO, minus one.</summary>
    public static readonly int[] RefineMp = { 50, 100, 150, 200 };

    /// <summary>The generic level and the character level each refine step needs (Q7, ruled 2026-09-24),
    /// index = the rung refined INTO, minus one: → Refined L0/40 · → Rare L3/52 · → Refined Rare L5/61 ·
    /// → Legendary L8/76.</summary>
    public static readonly int[] RefineGate = { 0, 3, 5, 8 };
    public static readonly int[] RefineCharLevel = { 40, 52, 61, 76 };

    /// <summary>Alloy: 20 gems + 20 iron → 1 (the note). Open at generic L0 / character 40, MP 50.</summary>
    public const string AlloyId = "mat_alloy";

    /// <summary>True for an input that does NOT scale with the recipe % (second round, answer 4: *"refined
    /// mats DO NOT scale"*): Nightsilver and Nightsilk, at every rung. They are the same on every recipe of a
    /// tier, which is what gives a 100% recipe its edge over five 20% attempts. Everything else — base mats,
    /// alloy, parts, bars, essence — scales on the 30/50/70/100 curve.</summary>
    public static bool IsFixedInput(string itemId) =>
        itemId.StartsWith("nightsilver_", StringComparison.Ordinal)
        || itemId.StartsWith("nightsilk_", StringComparison.Ordinal);

    /// <summary>What ONE attempt of a recipe needs of an input when spending a recipe of <paramref name="percent"/>.
    /// The server's gate and spend and the client's have/need colouring all read this.</summary>
    public static int InputQty(Recipe recipe, RecipeInput input, int percent) =>
        recipe.IsGear && !IsFixedInput(input.ItemId) ? ScaledQty(input.Qty, percent) : input.Qty;

    // ----- PARTS: the note's "heads" (step 10, answer 4: one per item KIND per tier) -----------------

    /// <summary>The part each gear KIND takes, keyed by the kind (the id prefix before "_t", e.g. "blunt2h").
    /// A body's variants share their weight's part.</summary>
    public static readonly IReadOnlyDictionary<string, string> PartNames = new Dictionary<string, string>
    {
        ["sword2h"] = "Greatsword Blade", ["blunt2h"] = "Maul Head", ["staff"] = "Staff Crown",
        ["bow"] = "Bow Limb", ["duals"] = "Fang Hilt", ["sword1h"] = "Sword Blade",
        ["blunt1h"] = "Mace Head", ["wand"] = "Wand Core",
        ["heavy"] = "Armor Plate", ["light"] = "Hide Panel", ["robe"] = "Robe Weave",
        ["helm"] = "Helm Shell", ["shield"] = "Shield Boss", ["gloves"] = "Gauntlet Frame",
        ["boots"] = "Greave Frame",
        ["necklace"] = "Pendant Setting", ["earring"] = "Stud Setting", ["ring"] = "Band Setting",
    };

    /// <summary>The kind of a tiered gear id: "light_t40_mdef" → "light".</summary>
    public static string GearKind(string gearId)
    {
        int i = gearId.IndexOf("_t", StringComparison.Ordinal);
        return i < 0 ? gearId : gearId.Substring(0, i);
    }

    /// <summary>The part id for a kind at a tier, e.g. "part_blunt2h_t40".</summary>
    public static string PartId(string kind, int itemLevel) => $"part_{kind}_t{itemLevel}";

    /// <summary>The five crafted gear tiers, index 0-4.</summary>
    public static readonly int[] GearTiers = { 40, 52, 61, 76, 80 };

    /// <summary>The largest count one craft command may ask for (the client's "Max" asks for this).</summary>
    public const int MaxCraftCount = 1000;

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

    /// <summary>Recipe slots at a generic level.</summary>
    public static int Slots(int genericLevel) =>
        BaseSlots + SlotsPerLevel * System.Math.Clamp(genericLevel, 0, MaxCraftLevel);

    // ----- THE CRAFTER-POINTS MODEL (owner, 2026-09-24, his "Idea-1"; design doc §2.2) ------------------

    /// <summary>The four types a point can be spent on, in window order. The Scribe folded into the Apothecary
    /// (`BL-305`, 2026-09-26); its enum value stays, since saved type levels index by it, but nothing spends on it.</summary>
    public static readonly CraftType[] SpendableTypes =
        { CraftType.Weapon, CraftType.Armour, CraftType.Jewels, CraftType.Apothecary };

    /// <summary>Points a generic level gives: one per level, so 10 at L10.</summary>
    public static int PointsAtGenericLevel(int genericLevel) => System.Math.Clamp(genericLevel, 0, MaxCraftLevel);

    /// <summary>The TYPE level a gear tier needs (*"an armor smith need L2 for T52 L4 61 L6 76 L8 80"*). The
    /// Apothecary has its own ladder (`BL-305`), authored per row in RecipeCatalog.</summary>
    public static int TierGate(int itemLevel) => itemLevel switch
    {
        >= 80 => 8,
        >= 76 => 6,
        >= 61 => 4,
        >= 52 => 2,
        _ => 0,
    };

    /// <summary>What a smith's L9 and L10 add to a gear recipe's %: *"L9 and L10 adds 5% craft chance (so 70%
    /// at the end)"*. Replaces 0.203.0's +0.5%/level at T76/T80.</summary>
    public static float GearSuccessBonus(int typeLevel) =>
        typeLevel >= 10 ? 0.10f : typeLevel >= 9 ? 0.05f : 0f;

    /// <summary>What an Apothecary batch costs as a fraction of its base price: ×0.9 at L0, falling
    /// 0.035 a level to ×0.55 at L10 (*"all scribe/apoth crafts are x0.9 of base and going to x0.55 at
    /// L10"*). The floor stays above the vendor's 50%, so crafting to vendor is 0 or a small loss.</summary>
    public static float PriceFactor(int typeLevel) =>
        0.9f - 0.035f * System.Math.Clamp(typeLevel, 0, MaxCraftLevel);

    /// <summary>Respecs a character gets in a lifetime (*"a char can respec only 5 times/lifetime with
    /// increasing cost"*).</summary>
    public const int MaxRespecs = 5;

    /// <summary>The gold price of the Nth respec (0-based). ⚠ A placeholder ladder (1M doubling); he ruled
    /// "increasing", not the numbers.</summary>
    public static readonly long[] RespecPrices = { 1_000_000, 2_000_000, 4_000_000, 8_000_000, 16_000_000 };

    /// <summary>Craft points one ATTEMPT is worth (his pick, 2026-09-24: tier-weighted, and a FAIL counts).
    /// Gear by its tier: T40 1 · T52 2 · T61 3 · T76 5 · T80 8. An Apothecary recipe is 1 a BATCH, and a
    /// REFINE (Nightsilver/Nightsilk, alloy, the bar) is **0** (step 10, ruled 2026-09-24: one T61 weapon is
    /// 1,650 refines, which at 1 each would pass generic L10 on conversions alone). Every attempt feeds the
    /// generic level only; type levels are bought with its points. ⚠ Playtest placeholders.</summary>
    public static int CraftPoints(Recipe recipe) =>
        recipe.Refine ? 0 : recipe.IsGear ? CraftPoints(recipe.GearItemLevel) : 1;

    /// <summary>The gear half of <see cref="CraftPoints(Recipe)"/>, by tier.</summary>
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

    /// <summary>A 100% recipe book's price (the Master's T40/T52 shelf, and so its 50% vendor sale) as a fraction
    /// of its item's own buy price; a lower-% book costs its SHARE of that (owner, 2026-09-24, `BL-274` part 1:
    /// *"Recipe 100% can be a 10% of its prise .. So a 20% recipe will cost 2%"*). See <see cref="RecipePrice"/>.</summary>
    public const float ShopRecipePriceFraction = 0.10f;

    /// <summary>A recipe book's Value: 10% of its item's price × its own % (100% → 10%, 60% → 6%, 20% → 2%).</summary>
    public static int RecipePrice(int itemPrice, int percent) =>
        Math.Max(1, (int)Math.Round(itemPrice * (double)ShopRecipePriceFraction * percent / 100.0));

    /// <summary>A PART's price as a fraction of its full item's buy price (`BL-274` part 1, same reason and same
    /// pick; owner 2026-09-24: *"Mats should sell for alot less than an actual crafted weapon"*): 20 parts sell for 10% of the item, so selling the heads never beats crafting with them.</summary>
    public const double PartPriceFraction = 0.01;

    /// <summary>The gear tiers whose 100% recipe the Master sells (*"master can sell t40 and t52"*).</summary>
    public static bool MasterSellsRecipeFor(int itemLevel) => itemLevel is 40 or 52;

    /// <summary>What learning a generic recipe costs at the Master, by the rung it was ruled on (step 9b,
    /// owner 2026-09-24: *"i agree on rcp buy price that you wrote"*): L0 20k · L1 50k · L2 100k · L3 200k ·
    /// L4 400k · L5 700k · L6 1M · L7 1.5M · L8 2M · L9 3M · L10 4M. Each recipe names its rung explicitly.</summary>
    public static readonly int[] LearnPriceLadder =
        { 20_000, 50_000, 100_000, 200_000, 400_000, 700_000, 1_000_000, 1_500_000, 2_000_000, 3_000_000, 4_000_000 };

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
