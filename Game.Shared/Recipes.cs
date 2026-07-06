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
        list.AddRange(FinishedItemRecipes());

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

    // ----- Finished-item (Epic SET) recipes: each tiered gear piece is craftable from mats. Cost =
    //  a BULK rarity (×2) + an ACCENT rarity (one higher), scaled by grade × slot, split by the item's
    //  material composition. Reproduces the owner's E-body anchor (100 common 50/20/20/10 + 50 unc
    //  25/10/10/5 ingot/leather/thread/gem). Sets = 100% success; A-grade (76) recipe is DropOnly. -----
    private static ItemRarity BulkRarity(int lvl) =>
        lvl >= 76 ? ItemRarity.Epic : lvl >= 52 ? ItemRarity.Rare : lvl >= 40 ? ItemRarity.Uncommon : ItemRarity.Common;
    private static ItemRarity AccentRarity(int lvl) =>
        lvl >= 76 ? ItemRarity.Legendary : lvl >= 52 ? ItemRarity.Epic : lvl >= 40 ? ItemRarity.Rare : ItemRarity.Uncommon;
    private static float GradeMult(int lvl) =>
        lvl >= 76 ? 2.8f : lvl >= 61 ? 2.2f : lvl >= 52 ? 1.7f : lvl >= 40 ? 1.3f : 1.0f;

    private static float SlotWeight(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => 1.0f,
        EquipSlot.Shield => 0.5f,
        EquipSlot.Jewel  => d.JewelType == JewelType.Necklace ? 0.5f : 0.3f,
        EquipSlot.Armor  => d.ArmorSlot switch { ArmorSlot.Body => 1.0f, ArmorSlot.Head => 0.5f, _ => 0.35f },
        _ => 0.5f
    };

    private static Profession ProfOf(ItemDef d) => d.Slot switch
    {
        EquipSlot.Weapon => Profession.WeaponSmith,
        EquipSlot.Armor or EquipSlot.Shield => Profession.ArmorSmith,
        EquipSlot.Jewel => Profession.Jeweler,
        _ => Profession.None
    };

    private static (MaterialType Type, float Frac)[] Composition(ItemDef d)
    {
        switch (d.Slot)
        {
            case EquipSlot.Weapon:
                return new[] { (MaterialType.Ingot, 0.6f), (MaterialType.Gem, 0.2f), (MaterialType.Wood, 0.2f) };
            case EquipSlot.Jewel:
                return new[] { (MaterialType.Gem, 0.6f), (MaterialType.Ingot, 0.2f), (MaterialType.Leather, 0.2f) };
            case EquipSlot.Shield:
                return new[] { (MaterialType.Ingot, 0.6f), (MaterialType.Leather, 0.2f), (MaterialType.Gem, 0.2f) };
            case EquipSlot.Armor when d.ArmorSlot == ArmorSlot.Body:
                return d.Weight switch
                {
                    ArmorWeight.Heavy => new[] { (MaterialType.Ingot, 0.5f), (MaterialType.Leather, 0.2f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) },
                    ArmorWeight.Robe  => new[] { (MaterialType.Thread, 0.5f), (MaterialType.Ingot, 0.2f), (MaterialType.Leather, 0.2f), (MaterialType.Gem, 0.1f) },
                    _                 => new[] { (MaterialType.Leather, 0.5f), (MaterialType.Ingot, 0.2f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) },
                };
            case EquipSlot.Armor:   // weightless accessories (helm/gloves/boots)
                return new[] { (MaterialType.Leather, 0.4f), (MaterialType.Ingot, 0.3f), (MaterialType.Thread, 0.2f), (MaterialType.Gem, 0.1f) };
            default:
                return System.Array.Empty<(MaterialType, float)>();
        }
    }

    private static IEnumerable<Recipe> FinishedItemRecipes()
    {
        foreach (var d in ItemCatalog.AllItems)
        {
            if (d.ItemLevel <= 0) continue;   // only the tiered gear
            if (d.Slot is not (EquipSlot.Weapon or EquipSlot.Armor or EquipSlot.Shield or EquipSlot.Jewel)) continue;
            var prof = ProfOf(d);
            if (prof == Profession.None) continue;

            int bulk = System.Math.Max(1, (int)(100 * SlotWeight(d) * GradeMult(d.ItemLevel)));
            int accent = System.Math.Max(1, bulk / 2);
            var bulkR = BulkRarity(d.ItemLevel);
            var accentR = AccentRarity(d.ItemLevel);

            var inputs = new List<RecipeInput>();
            foreach (var (type, frac) in Composition(d))
            {
                inputs.Add(new RecipeInput(Crafting.MaterialId(type, bulkR), System.Math.Max(1, (int)(bulk * frac))));
                inputs.Add(new RecipeInput(Crafting.MaterialId(type, accentR), System.Math.Max(1, (int)(accent * frac))));
            }

            yield return new Recipe(
                $"craft_{d.Id}", prof, d.Id, inputs.ToArray(),
                SuccessChance: 1f,                 // Epic set = guaranteed; the mats are the gate
                LearnLevel: d.ItemLevel,
                DropOnly: d.ItemLevel >= 76);      // A-grade recipes come from bosses/trade
        }
    }

    public static Recipe? Get(string id) => id is null ? null : _byId.GetValueOrDefault(id);
    public static IEnumerable<Recipe> All => _byId.Values;
    public static IEnumerable<Recipe> ForProfession(Profession p) => _byId.Values.Where(r => r.Profession == p);
}
