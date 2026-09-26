namespace Game.Shared;

/// <summary>One ingredient of a recipe: an item id (material or component) and a quantity.</summary>
public record RecipeInput(string ItemId, int Qty);

/// <summary>
/// A crafting recipe (`BL-273` part 2, 0.203.0): turns <see cref="Inputs"/> into <see cref="OutputId"/>
/// ×<see cref="OutputQty"/>. Anyone who has finished the crafter quest may hold it, in one of their
/// recipe SLOTS. There are no professions: <see cref="Type"/> only says which type level an attempt
/// raises (weapon / armour / jewels), and General recipes raise the generic level alone.
///
/// <para>Two kinds, told apart by <see cref="IsGear"/>:</para>
/// <list type="bullet">
/// <item><b>Gear</b> (T40-T80 weapons, armour, shields, jewels). Learned from a recipe ITEM
///   (<see cref="ItemCatalog.RecipeBookId"/>) that carries a % (20/40/60/100), and every attempt SPENDS one
///   such item at or below the learned %. The attempt succeeds at the used %, plus the T76/T80 crafter
///   bonus, and every input but Nightsilver/Nightsilk is scaled by it (<see cref="Crafting.InputQty"/>).
///   <see cref="SuccessChance"/> is unused for gear.</item>
/// <item><b>Generic</b> (potions, scrolls, refines). Bought at the Master for <see cref="LearnPrice"/>; no
///   recipe item per craft; always succeeds (step 9b, 2026-09-24); costs <see cref="GoldAt"/> gold a batch.</item>
/// </list>
/// <para><see cref="UnlockLevel"/> is the <see cref="Type"/> LEVEL needed to learn AND to craft it (the
/// crafter-points model, 0.204.0): a recipe whose type level is gone after a respec stays in its slot, LOCKED,
/// and crafts again once the level is back. For <see cref="CraftType.General"/> it reads the generic level.
/// <see cref="LearnLevel"/> is the CHARACTER level (*"i cannot learn T52 rcp @50"*).</para>
/// <para><see cref="MpCost"/> is charged per ATTEMPT (the note's MP table, 0.205.0). <see cref="Refine"/> marks the
/// Nightsilver / Nightsilk / alloy / bar conversions, which pay no craft points.</para>
/// <para><see cref="BatchValue"/> (Scribe / Apothecary only) is what the batch costs to BUY. The crafter pays
/// <see cref="Crafting.PriceFactor"/> of it in all, the fixed inputs counted at their vendor sell price and
/// gold making up the rest: <see cref="GoldAt"/>.</para>
/// </summary>
public record Recipe(
    string Id,
    CraftType Type,
    string OutputId,
    RecipeInput[] Inputs,
    int OutputQty = 1,
    float SuccessChance = 1f,
    int LearnLevel = 1,
    int UnlockLevel = 0,
    int LearnPrice = 0,
    int GoldCost = 0,
    int GearItemLevel = 0,
    bool QuestOnly = false,
    int BatchValue = 0,
    int MpCost = 0,
    bool Refine = false)
{
    /// <summary>The gold one batch costs a crafter at this type level. A recipe without a
    /// <see cref="BatchValue"/> charges its flat <see cref="GoldCost"/>. A Scribe/Apothecary one charges
    /// <c>PriceFactor(level) × BatchValue</c> minus its inputs at their vendor sell price, rounded to 10 and
    /// never below 10 (*"all need some amount of gold"*).</summary>
    public int GoldAt(int typeLevel)
    {
        if (BatchValue <= 0) return GoldCost;
        double inputs = 0;
        foreach (var i in Inputs)
            if (ItemCatalog.Get(i.ItemId) is ItemDef d) inputs += (double)i.Qty * ItemCatalog.SellPrice(d);
        double gold = Crafting.PriceFactor(typeLevel) * BatchValue - inputs;
        return System.Math.Max(10, (int)System.Math.Round(gold / 10.0) * 10);
    }

    /// <summary>A gear recipe: learned from and spent as a recipe ITEM, and rolled at that item's %.</summary>
    public bool IsGear => GearItemLevel > 0;
}

public static class RecipeCatalog
{
    // ⚠ Set in the EXPLICIT static constructor, NOT an inline initializer. Build() reads the static Cross
    // and Steps tables below; inline field initializers run in TEXTUAL order, so an inline "= Build()" here
    // would run before Cross/Steps exist and NRE (TypeInitializationException on first craft). The explicit
    // static cctor runs AFTER every field initializer, so Cross/Steps are ready. (Found 2026-07-25.)
    private static readonly Dictionary<string, Recipe> _byId;
    static RecipeCatalog() => _byId = Build();

    private static Dictionary<string, Recipe> Build()
    {
        var list = new List<Recipe>();
        list.AddRange(RefineRecipes());
        list.AddRange(FinishedItemRecipes());
        list.AddRange(ConsumableRecipes());
        list.Add(HammerRecipe());

        var dict = new Dictionary<string, Recipe>();
        foreach (var r in list)
            if (!dict.TryAdd(r.Id, r))
                throw new InvalidOperationException($"Duplicate recipe id '{r.Id}'.");
        return dict;
    }
    /// <summary>The crafter quest's Blacksmith's Hammer (*"at least 20 quest wood and at least 20 iron …
    /// 20 quest gems … 1 hammer head"*). Unscaled: the quest's 40% recipe is a lesson, not a discount.</summary>
    private static Recipe HammerRecipe() => new(
        Crafting.HammerRecipeId, CraftType.General, ItemCatalog.CrafterHammer,
        new[]
        {
            new RecipeInput(ItemCatalog.CrafterQuestWood, 20),
            new RecipeInput(ItemCatalog.CrafterQuestIron, 20),
            new RecipeInput(ItemCatalog.CrafterQuestGem, 20),
            new RecipeInput(ItemCatalog.CrafterHammerHead, 1),
        },
        SuccessChance: Crafting.HammerRecipePercent / 100f,
        LearnLevel: Crafting.CrafterQuestLevel, QuestOnly: true,
        MpCost: 50);   // `BL-306`: every craft costs MP; the D-grade placeholder, as a level-40 generic

    // =====================================================================================
    //  THE REFINES (`BL-273` part 3, 0.205.0; design doc §2.2 "Step 10", ruled 2026-09-24). General
    //  recipes, open to every crafter: no gold, always succeed, one output a craft, and they pay NO craft
    //  points (Crafting.CraftPoints). A count on the craft command makes the thousands of them one tap.
    // =====================================================================================
    private static IEnumerable<Recipe> RefineRecipes()
    {
        Recipe Step(string id, string output, string input, int step) =>
            new(id, CraftType.General, output, new[] { new RecipeInput(input, Crafting.RefineRatio) },
                LearnLevel: Crafting.RefineCharLevel[step], UnlockLevel: Crafting.RefineGate[step],
                LearnPrice: Crafting.LearnPriceLadder[Crafting.RefineGate[step]],
                MpCost: Crafting.RefineMp[step], Refine: true);

        for (int rung = 1; rung < Crafting.RefineRungs; rung++)
        {
            yield return Step($"refine_nightsilver_{rung}", Crafting.NightsilverId(rung), Crafting.NightsilverId(rung - 1), rung - 1);
            yield return Step($"refine_nightsilk_{rung}", Crafting.NightsilkId(rung), Crafting.NightsilkId(rung - 1), rung - 1);
        }

        // Alloy: *"a x50 alloy that is own recipie (20 gems + 20 iron)"*. L0 / 40, MP 50.
        yield return new Recipe("refine_alloy", CraftType.General, Crafting.AlloyId,
            new[] { new RecipeInput(Crafting.MaterialId(MaterialType.Gem), 20), new RecipeInput(Crafting.MaterialId(MaterialType.Iron), 20) },
            LearnLevel: Crafting.CrafterQuestLevel, UnlockLevel: 0, LearnPrice: Crafting.LearnPriceLadder[0],
            MpCost: 50, Refine: true);

        // The Volcanic Bar: *"20 volcanic ash + 20 volcanic stone"*. L7 / 76, MP 200.
        yield return new Recipe("refine_volcanic_bar", CraftType.General, ItemCatalog.VolcanicBar,
            new[] { new RecipeInput(ItemCatalog.VolcanicAsh, 20), new RecipeInput(ItemCatalog.VolcanicStone, 20) },
            LearnLevel: 76, UnlockLevel: 7, LearnPrice: Crafting.LearnPriceLadder[7], MpCost: 200, Refine: true);
    }

    // =====================================================================================
    //  THE GEAR RECIPES (`BL-273` part 3, 0.205.0) — AUTHORED TABLES, one cell per tier × slot.
    //
    //  🔑 NEVER A FORMULA (owner, 2026-09-23: *"please do not do anything as formula because moving one
    //     will break all others ... The fractions per part are just guidelines not formula locked"*). Any
    //     cell below can move alone. They were WRITTEN ONCE from his guide shares (1H 0.8, body 0.6,
    //     helmet/shield/necklace 0.4, earring 0.3, gloves/boots 0.2, ring 0.1 of the 2H) and the note's
    //     bulk splits (heavy metal 2 : leather 1, light leather 2 : thread 1, robe thread 4 : leather 1,
    //     helmet 3 : 2 : 1, shield 10 : 1 : 1, gloves/boots 2 : 1 : 1; rings/earrings iron, the necklace
    //     thread), rounded half away from zero and never below 1.
    //
    //  The 2H column is the note: wood AND iron 400/800/1200/1600/2000, alloy 10…50, 20 parts every tier,
    //  Nightsilver 300 normal / 200 Refined / 150 Rare / 50 Refined Rare / 10 Legendary, Volcanic Bars 40 / 70
    //  at T76 / T80 (second round), essence 400…2000 (third round, §2.2's ruled table).
    //
    //  Columns: 2H, 1H, heavy, light, robe, helmet, shield, gloves, boots, necklace, earring, ring.
    //  Rows: T40, T52, T61, T76, T80. Everything scales with the recipe % EXCEPT Nightsilver / Nightsilk
    //  (Crafting.IsFixedInput).
    // =====================================================================================
    private static readonly int[][] GearWood =
    {
        new[] {  400,  320,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T40
        new[] {  800,  640,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T52
        new[] { 1200,  960,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T61
        new[] { 1600, 1280,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T76
        new[] { 2000, 1600,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T80
    };
    private static readonly int[][] GearIron =
    {
        new[] {  400,  320,  320,    0,    0,  160,  267,   80,   80,    0,  240,   80 },   // T40
        new[] {  800,  640,  640,    0,    0,  320,  533,  160,  160,    0,  480,  160 },   // T52
        new[] { 1200,  960,  960,    0,    0,  480,  800,  240,  240,    0,  720,  240 },   // T61
        new[] { 1600, 1280, 1280,    0,    0,  640, 1067,  320,  320,    0,  960,  320 },   // T76
        new[] { 2000, 1600, 1600,    0,    0,  800, 1333,  400,  400,    0, 1200,  400 },   // T80
    };
    private static readonly int[][] GearLeather =
    {
        new[] {    0,    0,  160,  320,   96,  107,   27,   40,   40,    0,    0,    0 },   // T40
        new[] {    0,    0,  320,  640,  192,  213,   53,   80,   80,    0,    0,    0 },   // T52
        new[] {    0,    0,  480,  960,  288,  320,   80,  120,  120,    0,    0,    0 },   // T61
        new[] {    0,    0,  640, 1280,  384,  427,  107,  160,  160,    0,    0,    0 },   // T76
        new[] {    0,    0,  800, 1600,  480,  533,  133,  200,  200,    0,    0,    0 },   // T80
    };
    private static readonly int[][] GearThread =
    {
        new[] {    0,    0,    0,  160,  384,   53,   27,   40,   40,  320,    0,    0 },   // T40
        new[] {    0,    0,    0,  320,  768,  107,   53,   80,   80,  640,    0,    0 },   // T52
        new[] {    0,    0,    0,  480, 1152,  160,   80,  120,  120,  960,    0,    0 },   // T61
        new[] {    0,    0,    0,  640, 1536,  213,  107,  160,  160, 1280,    0,    0 },   // T76
        new[] {    0,    0,    0,  800, 1920,  267,  133,  200,  200, 1600,    0,    0 },   // T80
    };
    private static readonly int[][] GearAlloy =
    {
        new[] {   10,    8,    6,    6,    6,    4,    4,    2,    2,    4,    3,    1 },   // T40
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T52
        new[] {   30,   24,   18,   18,   18,   12,   12,    6,    6,   12,    9,    3 },   // T61
        new[] {   40,   32,   24,   24,   24,   16,   16,    8,    8,   16,   12,    4 },   // T76
        new[] {   50,   40,   30,   30,   30,   20,   20,   10,   10,   20,   15,    5 },   // T80
    };
    private static readonly int[][] GearParts =
    {
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T40
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T52
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T61
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T76
        new[] {   20,   16,   12,   12,   12,    8,    8,    4,    4,    8,    6,    2 },   // T80
    };
    /// <summary>Nightsilver (weapons, earring, ring) or Nightsilk (the rest), at the tier's own rung.</summary>
    private static readonly int[][] GearNight =
    {
        new[] {  300,  240,  180,  180,  180,  120,  120,   60,   60,  120,   90,   30 },   // T40 normal
        new[] {  200,  160,  120,  120,  120,   80,   80,   40,   40,   80,   60,   20 },   // T52 Refined
        new[] {  150,  120,   90,   90,   90,   60,   60,   30,   30,   60,   45,   15 },   // T61 Rare
        new[] {   50,   40,   30,   30,   30,   20,   20,   10,   10,   20,   15,    5 },   // T76 Refined Rare
        new[] {   10,    8,    6,    6,    6,    4,    4,    2,    2,    4,    3,    1 },   // T80 Legendary
    };
    private static readonly int[][] GearBars =
    {
        new[] {    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T40
        new[] {    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T52
        new[] {    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 },   // T61
        new[] {   40,   32,   24,   24,   24,   16,   16,    8,    8,   16,   12,    4 },   // T76
        new[] {   70,   56,   42,   42,   42,   28,   28,   14,   14,   28,   21,    7 },   // T80
    };
    /// <summary>The tier's grade essence, RULED 2026-09-23 (third round, §2.2): the body columns share one
    /// cell, as do helmet / shield / necklace and gloves / boots.</summary>
    private static readonly int[][] GearEssence =
    {
        new[] {  400,  320,  240,  240,  240,  160,  160,   80,   80,  160,  120,   40 },   // T40 D
        new[] {  800,  640,  480,  480,  480,  320,  320,  160,  160,  320,  240,   80 },   // T52 C
        new[] { 1200,  960,  720,  720,  720,  480,  480,  240,  240,  480,  360,  120 },   // T61 B
        new[] { 1600, 1280,  960,  960,  960,  640,  640,  320,  320,  640,  480,  160 },   // T76 A
        new[] { 2000, 1600, 1200, 1200, 1200,  800,  800,  400,  400,  800,  600,  200 },   // T80 S
    };
    /// <summary>MP per attempt, by tier and column. The column RATIOS are the note's (*"weapons 1h/2h 300/400, armor
    /// body 200, gloves/boots/earrings 100, helmet/shield/neckclace 150, rings 50"*), which was one row for every
    /// tier; `BL-306` (owner, 2026-09-26) made it rise with the tier: *"a weapon recipe t40 cost 400mp and a fighter
    /// have 200 ... u can make t40 to need 200 at most and go from there as checking the fighter can craft atleast
    /// one wepon at that lvl"*. The 2H is placed under the LOWEST fighter pool at the tier's own level in the
    /// previous tier's gear (`BalanceMatrix --craft-mp`: 203 / 325 / 437 / 778 / 876), the rest keep the note's
    /// ratios, rounded to 5.</summary>
    private static readonly int[][] GearMp =
    {
        new[] {  200,  150,  100,  100,  100,   75,   75,   50,   50,   75,   50,   25 },   // T40
        new[] {  300,  225,  150,  150,  150,  115,  115,   75,   75,  115,   75,   40 },   // T52
        new[] {  400,  300,  200,  200,  200,  150,  150,  100,  100,  150,  100,   50 },   // T61
        new[] {  700,  525,  350,  350,  350,  265,  265,  175,  175,  265,  175,   90 },   // T76
        new[] {  800,  600,  400,  400,  400,  300,  300,  200,  200,  300,  200,  100 },   // T80
    };

    /// <summary>The table column a piece of gear reads, or -1.</summary>
    private static int GearColumn(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => d.WeaponType.IsTwoHanded() ? 0 : 1,
        EquipSlot.Shield => 6,
        EquipSlot.Armor => d.ArmorSlot switch
        {
            ArmorSlot.Body => d.Weight switch { ArmorWeight.Heavy => 2, ArmorWeight.Light => 3, _ => 4 },
            ArmorSlot.Head => 5,
            ArmorSlot.Gloves => 7,
            ArmorSlot.Boots => 8,
            _ => -1,
        },
        EquipSlot.Jewel => d.JewelType switch
        {
            JewelType.Necklace => 9,
            JewelType.Earring => 10,
            JewelType.Ring => 11,
            _ => -1,
        },
        _ => -1,
    };

    /// <summary>Weapons, earrings and rings take Nightsilver; armour, shields and the necklace take Nightsilk
    /// (second round, answer 5).</summary>
    private static bool TakesNightsilver(int column) => column is 0 or 1 or 10 or 11;

    private static IEnumerable<Recipe> FinishedItemRecipes()
    {
        foreach (var d in ItemCatalog.AllItems)
        {
            if (d.ItemLevel <= 0) continue;                // only the tiered gear
            // Only the authored (Mythic) piece is craftable; its Common copy is drop-only.
            if (d.Rarity != ItemRarity.Mythic) continue;
            if (!Crafting.IsGearSlot(d.Slot)) continue;
            int t = System.Array.IndexOf(Crafting.GearTiers, d.ItemLevel);
            int c = GearColumn(d);
            if (t < 0 || c < 0) continue;                  // F and E gear is not crafted

            var inputs = new List<RecipeInput>();
            void Add(string id, int qty) { if (qty > 0) inputs.Add(new RecipeInput(id, qty)); }
            Add(Crafting.MaterialId(MaterialType.Wood), GearWood[t][c]);
            Add(Crafting.MaterialId(MaterialType.Iron), GearIron[t][c]);
            Add(Crafting.MaterialId(MaterialType.Leather), GearLeather[t][c]);
            Add(Crafting.MaterialId(MaterialType.Thread), GearThread[t][c]);
            Add(Crafting.AlloyId, GearAlloy[t][c]);
            Add(Crafting.PartId(Crafting.GearKind(d.Id), d.ItemLevel), GearParts[t][c]);
            Add(TakesNightsilver(c) ? Crafting.NightsilverId(t) : Crafting.NightsilkId(t), GearNight[t][c]);
            Add(ItemCatalog.VolcanicBar, GearBars[t][c]);
            Add(Crafting.EssenceIds[t], GearEssence[t][c]);

            // The success % is the recipe ITEM's, not the recipe's (see Recipe): SuccessChance is unused here.
            yield return new Recipe(
                $"craft_{d.Id}", Crafting.TypeOf(d), d.Id, inputs.ToArray(),
                LearnLevel: d.ItemLevel, UnlockLevel: Crafting.TierGate(d.ItemLevel), GearItemLevel: d.ItemLevel,
                MpCost: GearMp[t][c]);
        }
    }

    // =====================================================================================
    //  STEP 9b — THE GENERIC-RECIPE TABLE (owner, 2026-09-24; design doc §2.2 "Your answers" and
    //  "The crafter-points model"). Every number below is AUTHORED, one literal per row.
    //
    //  • TYPE LEVEL (UnlockLevel): commons are open to every crafter at L0 (*"all can do T40 crafts + common
    //    scrolls common buff pots and common regen pots"*). An uncommon needs the type and its tier's gate:
    //    T40 L1 · T52 L2 · T61 L4 (the smiths' gate, and L1 at T40 because the type must be learned at all).
    //    Runes 1h L7 / 2h L10; rare HP L7 / rare MP L10 (*"apoth … adds rare pots at the end"*).
    //  • A BUFF's tier is its class skill's LAST rung (*"if a skill is learned at 40 but last lvl is at 52
    //    that s T52"*), measured 2026-09-24 over every class table: T40 = Might, Bulwark, Alacrity, Swift;
    //    T52 = Aim, Force, Ward, Fury, Agility, Focus, Ferocity, Frenzy, Vigor, Serenity; T61 = Body, Soul,
    //    Resolve, Insight, Vampirism. The tier sets the character level, the gate and the essence (D/C/B).
    //  • PRICE: BatchValue is what the batch costs to buy (shelf price; HP/MP 60/120 · 250/500 · 5000/10000,
    //    buff potions 1,500 / 5,000, buff scrolls 36,000, rune boxes 150k / 280k). The crafter pays
    //    ×0.9 → ×0.55 of it by type level (Crafting.PriceFactor); essence and mats are fixed, gold floats.
    //  • LEARN PRICE: the ladder of the rung each line was ruled on (Crafting.LearnPriceLadder).
    //  • OUT, by his ruling: stones, Return/Resurrection (+ Ultimates), every Dash, the Instant potion, and
    //    enchant + attribute scrolls (never craftable).
    // =====================================================================================
    /// <summary>`BL-306` — a generic batch's MP, per attempt: *"every craft must cost mp (refines and generics
    /// and apoth as well)"*. He gave no numbers, so these are PLACEHOLDERS on the refine ladder's own scale
    /// (<see cref="Crafting.RefineMp"/> 50/100/150/200), keyed to the line's grade: D (40) 50 · C (52) 100 ·
    /// B (61) 150 · the 70+ lines (rare potions, rune boxes) 200. Any row may be retuned alone.</summary>
    private static int GenericMp(int charLevel) =>
        charLevel >= 70 ? 200 : charLevel >= 61 ? 150 : charLevel >= 52 ? 100 : 50;

    private static IEnumerable<Recipe> ConsumableRecipes()
    {
        static RecipeInput M(MaterialType t, int n) => new(Crafting.MaterialId(t), n);
        static RecipeInput E(int grade, int n) => new(Crafting.EssenceIds[grade], n);
        static RecipeInput V(string id, int n) => new(id, n);

        Recipe R(CraftType type, string output, int qty, int charLevel, int gate, int ladder, int batchValue,
                 params RecipeInput[] inputs) =>
            new($"craft_{output}", type, output, inputs,
                OutputQty: qty, LearnLevel: charLevel, UnlockLevel: gate,
                LearnPrice: Crafting.LearnPriceLadder[ladder], BatchValue: batchValue,
                MpCost: GenericMp(charLevel));

        const CraftType Apo = CraftType.Apothecary, Scr = CraftType.Scribe;
        const int Gem = 0, Wood = 1, Iron = 2, Leather = 3;
        static RecipeInput Mat(int k, int n) => M(k switch
        {
            0 => MaterialType.Gem, 1 => MaterialType.Wood, 2 => MaterialType.Iron, _ => MaterialType.Leather,
        }, n);

        // ---- HP / MP ------------------------------------------------------------------------------------
        //                                   batch  char gate ladder  batch value    inputs
        yield return R(Apo, ItemCatalog.MinorPotion,       100, 40, 0, 0,   6_000,   Mat(Gem, 5),  E(0, 1));
        yield return R(Apo, ItemCatalog.MinorManaPotion,   100, 40, 0, 0,  12_000,   Mat(Gem, 10), E(0, 2));
        yield return R(Apo, ItemCatalog.HealingPotion,      50, 52, 2, 2,  12_500,   Mat(Gem, 10), E(1, 1));
        yield return R(Apo, ItemCatalog.ManaPotion,         50, 52, 2, 2,  25_000,   Mat(Gem, 20), E(1, 2));
        // Rare: *"they are not rly sold anywhere so crafting is the only way"*. Unreachable until the volcanic
        // ash/stone drop in step 11.
        yield return R(Apo, ItemCatalog.GreaterPotion,      10, 76, 7, 5,  50_000,   Mat(Gem, 10), E(2, 1),
                       V(ItemCatalog.VolcanicAsh, 1), V(ItemCatalog.VolcanicStone, 1));
        yield return R(Apo, ItemCatalog.GreaterManaPotion,  10, 76, 10, 5, 100_000,  Mat(Gem, 20), E(2, 2),
                       V(ItemCatalog.VolcanicAsh, 2), V(ItemCatalog.VolcanicStone, 2));

        // ---- BUFF POTIONS: x6. Common = gems + wood, open to all (ladder L1); Uncommon adds the tier's
        //      essence behind the Apothecary gate (ladder L3). (common, uncommon, the family's tier)
        foreach (var (c, u, tier) in new[]
        {
            (ItemCatalog.MightPotionC,   ItemCatalog.MightPotionU,   40),
            (ItemCatalog.BulwarkPotionC, ItemCatalog.BulwarkPotionU, 40),
            (ItemCatalog.CastPotionC,    ItemCatalog.CastPotionU,    40),
            (ItemCatalog.SpeedPotionC,   ItemCatalog.SpeedPotionU,   40),
            (ItemCatalog.AimPotionC,     ItemCatalog.AimPotionU,     52),
            (ItemCatalog.ForcePotionC,   ItemCatalog.ForcePotionU,   52),
            (ItemCatalog.WardPotionC,    ItemCatalog.WardPotionU,    52),
            (ItemCatalog.AtkPotionC,     ItemCatalog.AtkPotionU,     52),
            (ItemCatalog.EvaPotionC,     ItemCatalog.EvaPotionU,     52),
        })
        {
            yield return R(Apo, c, 6, tier, 0, 1, 9_000, Mat(Gem, 3), Mat(Wood, 3));
            yield return tier >= 52
                ? R(Apo, u, 6, tier, 2, 3, 30_000, Mat(Gem, 5), Mat(Wood, 5), E(1, 1))
                : R(Apo, u, 6, tier, 1, 3, 30_000, Mat(Gem, 5), Mat(Wood, 5), E(0, 2));
        }

        // ---- BUFF SCROLLS: x2, leather + iron. The nine "basic" ones count as the common line (open to
        //      all, ladder L2); the ten "other" ones are the uncommon line: the Scribe gate + the tier's
        //      essence (ladder L4).
        foreach (var (id, tier) in new[]
        {
            (ItemCatalog.MightScrollR, 40), (ItemCatalog.BulwarkScrollR, 40), (ItemCatalog.CastScrollR, 40),
            (ItemCatalog.SpeedScrollR, 40), (ItemCatalog.AimScrollR, 52), (ItemCatalog.ForceScrollR, 52),
            (ItemCatalog.WardScrollR, 52), (ItemCatalog.AtkScrollR, 52), (ItemCatalog.EvaScrollR, 52),
        })
            yield return R(Scr, id, 2, tier, 0, 2, 72_000, Mat(Leather, 3), Mat(Iron, 3));
        foreach (var (id, tier) in new[]
        {
            (ItemCatalog.FocusScrollM, 52), (ItemCatalog.FerocityScrollM, 52), (ItemCatalog.FrenzyScrollM, 52),
            (ItemCatalog.VigorScrollM, 52), (ItemCatalog.SerenityScrollM, 52),
            (ItemCatalog.BodyScrollM, 61), (ItemCatalog.SoulScrollM, 61), (ItemCatalog.ResolveScrollM, 61),
            (ItemCatalog.InsightScrollM, 61), (ItemCatalog.VampScrollM, 61),
        })
            yield return tier >= 61
                ? R(Scr, id, 2, tier, 4, 4, 72_000, Mat(Leather, 5), Mat(Iron, 5), E(2, 2))
                : R(Scr, id, 2, tier, 2, 4, 72_000, Mat(Leather, 5), Mat(Iron, 5), E(1, 2));

        // ---- RUNE BOXES: x3 (*"1h from shop cost 450k … 2h x3 shop cost 840"*). The 2h is unreachable until
        //      the Volcanic Bar refine (step 10) and its ash/stone (step 11) exist.
        foreach (var box in new[] { ItemCatalog.BoxWarRune1h, ItemCatalog.BoxSpellRune1h })
            yield return R(Scr, box, 3, 70, 7, 7, 450_000, Mat(Iron, 10), Mat(Gem, 10), Mat(Wood, 10), E(2, 8));
        foreach (var box in new[] { ItemCatalog.BoxWarRune2h, ItemCatalog.BoxSpellRune2h })
            yield return R(Scr, box, 3, 80, 10, 10, 840_000, V(ItemCatalog.VolcanicBar, 2), E(4, 4));
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    /// <summary>The recipes the Master SELLS to learn (generic, not the quest's own): by type, then gate,
    /// then character level.</summary>
    public static IEnumerable<Recipe> GenericForSale => _byId.Values
        .Where(r => !r.IsGear && !r.QuestOnly)
        .OrderBy(r => r.Type).ThenBy(r => r.UnlockLevel).ThenBy(r => r.LearnLevel).ThenBy(r => r.Id);
}
