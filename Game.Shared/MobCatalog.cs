namespace Game.Shared;

/// <summary>One possible drop from a mob: an item, a float chance [0..1], a
/// quantity range, and an OPTIONAL level band. The drop only rolls when the
/// mob's spawned level is within [MinLevel, MaxLevel] (0/0 = any level). This is
/// what lets ONE creature drop different loot at different levels — e.g. a
/// grey_wolf drops common hide at any level but wolf fangs only at level 25+.
/// Chance and amount are scaled by the server RateConfig.</summary>
public record DropEntry(string ItemId, float Chance, int MinQty = 1, int MaxQty = 1,
    int MinLevel = 0, int MaxLevel = 0)
{
    /// <summary>Does this drop apply to a mob spawned at the given level?</summary>
    public bool AppliesAtLevel(int level) =>
        (MinLevel == 0 || level >= MinLevel) && (MaxLevel == 0 || level <= MaxLevel);
}

/// <summary>
/// A mob TEMPLATE: identity (id + display name), movement speeds, behavior, and
/// its drop table. A mob has NO fixed level — the spawning ZONE assigns the
/// level (and stats derive from it). So the same creature can appear at any
/// level with the same drops; a genuinely different creature (different loot)
/// gets its own id.
/// </summary>
public record MobType(
    string Id,
    string Name,
    float WalkSpeed,
    float RunSpeed,
    bool Aggressive = false,
    DropEntry[]? Drops = null);

/// <summary>
/// THE place to manage mobs. Each entry is a creature template with its own drop
/// table; zones reference these by id and assign the level. Run speeds sit below
/// the player move cap (250) and vary so players can kite.
///
/// To add a mob: add a template here with its drops. To make an existing mob
/// tougher somewhere, just spawn it in a higher-level zone (same id, same drops).
/// To give different loot, make a NEW id.
/// </summary>
public static class MobCatalog
{
    private static readonly Dictionary<string, MobType> All = Build();

    private static Dictionary<string, MobType> Build()
    {
        var list = new[]
        {
            new MobType("grey_wolf", "Grey Wolf", 80f, 150f, Aggressive: true,
                Drops: new[]
                {
                    // Any level: common potion + a chance at a basic sword.
                    new DropEntry(ItemCatalog.MinorPotion, 0.30f, 1, 2),
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.F, ItemRarity.Common), 0.05f),
                    // Only when this wolf spawns at level 15+ (tougher zones): a
                    // better armour drop. Same creature id, level-varying loot.
                    new DropEntry(ItemCatalog.ArmorKey(ArmorWeight.Light, ItemGrade.E, ItemRarity.Uncommon),
                        0.06f, 1, 1, MinLevel: 15),
                }),

            new MobType("brown_boar", "Brown Boar", 55f, 100f,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.25f, 1, 2),
                    new DropEntry(ItemCatalog.ArmorKey(ArmorWeight.Light, ItemGrade.F, ItemRarity.Common), 0.04f),
                }),

            new MobType("dire_boar", "Dire Boar", 60f, 110f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.40f, 2, 4),
                    new DropEntry(ItemCatalog.ArmorKey(ArmorWeight.Heavy, ItemGrade.E, ItemRarity.Uncommon), 0.06f),
                    new DropEntry(ItemCatalog.ScrollCommon, 0.10f),
                }),

            new MobType("green_slime", "Green Slime", 35f, 60f,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.30f, 1, 2),
                }),

            new MobType("cave_spider", "Cave Spider", 70f, 120f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.25f),
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Dagger, ItemGrade.F, ItemRarity.Uncommon), 0.05f),
                }),

            new MobType("road_bandit", "Road Bandit", 60f, 108f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Uncommon), 0.06f),
                    new DropEntry(ItemCatalog.ScrollUncommon, 0.08f),
                    new DropEntry(ItemCatalog.MinorPotion, 0.30f, 1, 3),
                }),
        };
        var dict = new Dictionary<string, MobType>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
            if (!dict.TryAdd(m.Id, m))
                throw new InvalidOperationException($"Duplicate mob id '{m.Id}'.");
        return dict;
    }

    /// <summary>Look up a mob template by id. Falls back to a sane default so a
    /// mistyped zone id never crashes spawning.</summary>
    public static MobType Get(string id) =>
        All.TryGetValue(id, out var m) ? m : new MobType(id, id, 60f, 110f);

    public static bool IsAggressive(string id) => Get(id).Aggressive;
}
