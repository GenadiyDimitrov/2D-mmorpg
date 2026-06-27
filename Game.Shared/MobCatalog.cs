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
/// <summary>A mob's "passive skills": stat MODIFIERS applied on top of its level-derived
/// stats, so a template can be a glass-cannon, a MAGIC monster (high M.Def / low P.Def →
/// hard for mages, easy for fighters), an armored brute (high P.Def / low M.Def → the
/// reverse), a bruiser, or a boss. Multipliers default to 1 (no change); resists are
/// fractions (0 = none). Use <see cref="MobCatalog.MobTier"/> for L2-style leveled
/// magnitudes (tier 3 = ×1) if you prefer. Null on a template = no modifiers.</summary>
public readonly record struct MobMod(
    float Hp = 1f, float PDef = 1f, float MDef = 1f,
    float PAtk = 1f, float MAtk = 1f,
    float Evasion = 1f, float Accuracy = 1f,
    float BowResist = 0f,    // fraction of BOW damage taken removed (0..1)
    float CritResist = 0f,   // reduces an attacker's physical crit CHANCE vs this mob
    bool Boss = false,       // raid-boss passive (adds crit/bow resistance on spawn)
    string Name = "")        // display label for the inspect/target window
{
    /// <summary>Human-readable passive lines for the target-inspect window.</summary>
    public IEnumerable<string> Describe()
    {
        if (!string.IsNullOrEmpty(Name)) yield return Name;
        if (Hp != 1f)       yield return $"Max HP {Sign(Hp)}";
        if (PDef != 1f)     yield return $"P.Def {Sign(PDef)}";
        if (MDef != 1f)     yield return $"M.Def {Sign(MDef)}";
        if (PAtk != 1f)     yield return $"P.Atk {Sign(PAtk)}";
        if (MAtk != 1f)     yield return $"M.Atk {Sign(MAtk)}";
        if (Evasion != 1f)  yield return $"Evasion {Sign(Evasion)}";
        if (Accuracy != 1f) yield return $"Accuracy {Sign(Accuracy)}";
        // Bow/Crit resist are rendered from the numeric DTO fields (uniform for mobs
        // and players), so they're not repeated here.
        if (Boss) yield return "Raid Boss";
    }

    private static string Sign(float mult) =>
        mult >= 1f ? $"+{(mult - 1f) * 100:0}%" : $"-{(1f - mult) * 100:0}%";
}

public record MobType(
    string Id,
    string Name,
    float WalkSpeed,
    float RunSpeed,
    bool Aggressive = false,
    DropEntry[]? Drops = null,
    MobMod? Mod = null);     // per-template stat modifiers ("passive skills")

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

    /// <summary>L2-style monster stat TIER → multiplier (tier 3 = normal ×1; lower = weaker,
    /// higher = stronger). e.g. an "HP Lv4" mob = MobTier(4) = ×2 HP. Tunable.</summary>
    public static float MobTier(int tier) => tier switch
    {
        1 => 0.33f, 2 => 0.5f, 3 => 1f, 4 => 2f, 5 => 3f, 6 => 4f, 7 => 5f, _ => 1f
    };

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
                    new DropEntry(ItemCatalog.AttrScrollCommon, 0.06f),
                    new DropEntry(ItemCatalog.SpeedPotionU, 0.05f),
                }),

            // MAGIC monster: high M.Def, low P.Def — hard for mages, easy for fighters.
            new MobType("green_slime", "Green Slime", 35f, 60f,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.30f, 1, 2),
                },
                Mod: new MobMod(MDef: 2f, PDef: 0.5f, Name: "Magic Monster")),

            new MobType("cave_spider", "Cave Spider", 70f, 120f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.25f),
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Dual, ItemGrade.F, ItemRarity.Uncommon), 0.05f),
                    new DropEntry(ItemCatalog.AttrScrollCommon, 0.05f),
                    new DropEntry(ItemCatalog.CastPotionU, 0.05f),
                }),

            new MobType("road_bandit", "Road Bandit", 60f, 108f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Uncommon), 0.06f),
                    new DropEntry(ItemCatalog.ScrollUncommon, 0.08f),
                    new DropEntry(ItemCatalog.MinorPotion, 0.30f, 1, 3),
                    // Reroll scrolls + rarer buff potions, weighted to higher-level spawns.
                    new DropEntry(ItemCatalog.AttrScrollUncommon, 0.05f),
                    new DropEntry(ItemCatalog.AttrScrollRare, 0.02f, 1, 1, MinLevel: 20),
                    new DropEntry(ItemCatalog.AtkPotionU, 0.05f),
                    new DropEntry(ItemCatalog.SpeedPotionR, 0.02f, 1, 1, MinLevel: 20),
                }),

            // ----- Higher-tier creatures for the level 20-80 bands. Same templates
            //       are reused across bands; the zone sets the level (= stats). -----
            new MobType("orc_raider", "Orc Raider", 65f, 118f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.MinorPotion, 0.35f, 1, 3),
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Blunt, ItemGrade.E, ItemRarity.Uncommon), 0.06f),
                    new DropEntry(ItemCatalog.ScrollUncommon, 0.10f),
                    new DropEntry(ItemCatalog.AttrScrollUncommon, 0.05f),
                }),

            // ARMORED brute: high HP & P.Def, low M.Def, low evasion — easy for mages,
            // hard for fighters (the opposite of the magic slime).
            new MobType("stone_golem", "Stone Golem", 40f, 70f,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.HealingPotion, 0.30f, 1, 2),
                    new DropEntry(ItemCatalog.ArmorKey(ArmorWeight.Heavy, ItemGrade.E, ItemRarity.Uncommon), 0.07f),
                    new DropEntry(ItemCatalog.ScrollRare, 0.05f),
                    new DropEntry(ItemCatalog.AttrScrollRare, 0.03f, 1, 1, MinLevel: 40),
                },
                Mod: new MobMod(Hp: MobTier(4), PDef: 2f, MDef: 0.5f, Evasion: 0.5f, Name: "Armored Brute")),

            new MobType("wraith", "Wraith", 80f, 132f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.HealingPotion, 0.30f),
                    new DropEntry(ItemCatalog.ArmorKey(ArmorWeight.Robe, ItemGrade.E, ItemRarity.Uncommon), 0.07f),
                    new DropEntry(ItemCatalog.SilverTalisman, 0.04f),
                    new DropEntry(ItemCatalog.CastPotionU, 0.06f),
                    new DropEntry(ItemCatalog.AttrScrollRare, 0.03f, 1, 1, MinLevel: 40),
                }),

            new MobType("young_drake", "Young Drake", 75f, 145f, Aggressive: true,
                Drops: new[]
                {
                    new DropEntry(ItemCatalog.GreaterPotion, 0.35f, 1, 2),
                    new DropEntry(ItemCatalog.WeaponKey(WeaponType.Sword, ItemGrade.E, ItemRarity.Rare), 0.05f),
                    new DropEntry(ItemCatalog.ScrollRare, 0.08f),
                    new DropEntry(ItemCatalog.AttrScrollRare, 0.05f),
                    new DropEntry(ItemCatalog.SpeedPotionR, 0.05f),
                    new DropEntry(ItemCatalog.AtkPotionR, 0.05f),
                    new DropEntry(ItemCatalog.AttrScrollLegendary, 0.01f, 1, 1, MinLevel: 70),
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
