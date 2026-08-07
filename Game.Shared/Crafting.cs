namespace Game.Shared;

/// <summary>Crafting professions — one per character (auto-learned by level; no re-spec for now).
/// Each REFINES one material type and crafts one item family. See docs/design/Crafting.md.</summary>
public enum Profession { None = 0, WeaponSmith, ArmorSmith, Jeweler, PotionMaster, ScrollScribe }

/// <summary>The 5 crafting material types. Each is REFINED (raw → higher rarity) only by its owning
/// profession, but every rarity also DROPS from mobs — so professions are an efficiency/trade path,
/// not a gate. Finished items need several types → cross-profession trade.</summary>
public enum MaterialType { Ingot = 0, Thread, Wood, Leather, Gem }

public static class Crafting
{
    /// <summary>Which profession refines a material type (and is the one that can upgrade it).</summary>
    public static Profession RefinerOf(MaterialType type) => type switch
    {
        MaterialType.Ingot   => Profession.WeaponSmith,
        MaterialType.Leather => Profession.ArmorSmith,
        MaterialType.Gem     => Profession.Jeweler,
        MaterialType.Wood    => Profession.PotionMaster,
        MaterialType.Thread  => Profession.ScrollScribe,
        _ => Profession.None
    };

    // Material rarities used (Legendary is the top).
    public static readonly ItemRarity[] MaterialRarities =
        { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary };

    public static readonly MaterialType[] MaterialTypes =
        { MaterialType.Ingot, MaterialType.Thread, MaterialType.Wood, MaterialType.Leather, MaterialType.Gem };

    /// <summary>Stable item id for a material of a type + rarity, e.g. "mat_gem_rare".</summary>
    public static string MaterialId(MaterialType type, ItemRarity rarity) =>
        $"mat_{type.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    /// <summary>Display name, e.g. "Rare Gem".</summary>
    public static string MaterialName(MaterialType type, ItemRarity rarity) =>
        $"{rarity} {type}";
}
