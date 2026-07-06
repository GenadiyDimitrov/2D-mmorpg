namespace Game.Shared;

/// <summary>One ingredient of a recipe: an item id (material or component) and a quantity.</summary>
public record RecipeInput(string ItemId, int Qty);

/// <summary>
/// A crafting recipe: a <see cref="Profession"/> turns <see cref="Inputs"/> into
/// <see cref="OutputId"/> ×<see cref="OutputQty"/>, succeeding with <see cref="SuccessChance"/>
/// (a failed craft consumes the mats — the risk). Auto-known once the crafter's char level reaches
/// <see cref="LearnLevel"/>, UNLESS <see cref="DropOnly"/> (the recipe itself must be found/bought,
/// e.g. the A-grade sets). See docs/Crafting.md.
/// </summary>
public record Recipe(
    string Id,
    Profession Profession,
    string OutputId,
    RecipeInput[] Inputs,
    int OutputQty = 1,
    float SuccessChance = 1f,
    int LearnLevel = 1,
    bool DropOnly = false);

public static class RecipeCatalog
{
    private static readonly Dictionary<string, Recipe> _byId = Build();

    private static Dictionary<string, Recipe> Build()
    {
        var list = new List<Recipe>();
        list.AddRange(RefinementRecipes());
        // Finished-item recipes (gear/potions/scrolls) are added in a later slice, once the
        // per-grade material-cost formula + scaled drop items land.

        var dict = new Dictionary<string, Recipe>();
        foreach (var r in list)
            if (!dict.TryAdd(r.Id, r))
                throw new InvalidOperationException($"Duplicate recipe id '{r.Id}'.");
        return dict;
    }

    // Each material type upgrades using 5 of itself (one rarity lower) + 2 CROSS mats from two
    // DIFFERENT professions' types (also the lower rarity) → forces trade. Refinement is guaranteed
    // (the 5+2 cost is the gate); it's known once the crafter reaches the rarity's level gate.
    private static readonly Dictionary<MaterialType, (MaterialType A, MaterialType B)> Cross = new()
    {
        [MaterialType.Gem]     = (MaterialType.Ingot,  MaterialType.Wood),
        [MaterialType.Ingot]   = (MaterialType.Gem,    MaterialType.Leather),
        [MaterialType.Leather] = (MaterialType.Thread, MaterialType.Wood),
        [MaterialType.Thread]  = (MaterialType.Leather, MaterialType.Gem),
        [MaterialType.Wood]    = (MaterialType.Ingot,  MaterialType.Thread),
    };

    private static readonly (ItemRarity Low, ItemRarity High)[] Steps =
    {
        (ItemRarity.Common, ItemRarity.Uncommon),
        (ItemRarity.Uncommon, ItemRarity.Rare),
        (ItemRarity.Rare, ItemRarity.Epic),
        (ItemRarity.Epic, ItemRarity.Legendary),
    };

    /// <summary>Char level a crafter can refine INTO a given rarity (aligns with the drop gates).</summary>
    private static int RefineLearnLevel(ItemRarity high) => high switch
    {
        ItemRarity.Uncommon => 20,
        ItemRarity.Rare => 40,
        ItemRarity.Epic => 61,
        ItemRarity.Legendary => 76,
        _ => 1
    };

    private static IEnumerable<Recipe> RefinementRecipes()
    {
        foreach (var type in Crafting.MaterialTypes)
        {
            var (a, b) = Cross[type];
            foreach (var (low, high) in Steps)
                yield return new Recipe(
                    $"refine_{type}_{high}".ToLowerInvariant(),
                    Crafting.RefinerOf(type),
                    Crafting.MaterialId(type, high),
                    new[]
                    {
                        new RecipeInput(Crafting.MaterialId(type, low), 5),
                        new RecipeInput(Crafting.MaterialId(a, low), 1),
                        new RecipeInput(Crafting.MaterialId(b, low), 1),
                    },
                    SuccessChance: 1f,
                    LearnLevel: RefineLearnLevel(high));
        }
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    public static IEnumerable<Recipe> ForProfession(Profession p) => _byId.Values.Where(r => r.Profession == p);
}
