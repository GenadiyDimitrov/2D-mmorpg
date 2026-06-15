namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2 }

public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2 }

public enum ArmorWeight { None = 0, Heavy = 1, Light = 2, Robe = 3 }

/// <summary>An item template. WeaponRange &gt; 0 marks ranged weapons (bows,
/// staves) and becomes the wielder's basic-attack range.</summary>
public record ItemDef(
    int Id,
    string Name,
    EquipSlot Slot,
    ItemGrade Grade,
    ItemRarity Rarity,
    ArmorWeight Weight = ArmorWeight.None,
    int AtkBonus = 0,
    int DefBonus = 0,
    int HpBonus = 0,
    int MpBonus = 0,
    int EvaBonus = 0,
    float WeaponRange = 0,
    // ----- Consumables (potions) -----
    // HealPercentPerSecond > 0 = heal-over-time potion; InstantHealPercent > 0
    // = instant heal. PotionDurationTicks/PotionCooldownTicks in server ticks.
    float HealPercentPerSecond = 0f,
    float InstantHealPercent = 0f,
    int PotionDurationTicks = 0,
    int PotionCooldownTicks = 0);

public static class ItemCatalog
{
    private static readonly Dictionary<int, ItemDef> All = new ItemDef[]
    {
        // ----- F grade (lvl 0+) — weapons --------------------------------------
        new(1,  "Rusty Sword",    EquipSlot.Weapon, ItemGrade.F, ItemRarity.Common,   AtkBonus: 4),
        new(2,  "Worn Bow",       EquipSlot.Weapon, ItemGrade.F, ItemRarity.Common,   AtkBonus: 5, WeaponRange: 400),
        new(3,  "Old Staff",      EquipSlot.Weapon, ItemGrade.F, ItemRarity.Common,   AtkBonus: 3, MpBonus: 15, WeaponRange: 300),
        new(4,  "Soldier Sword",  EquipSlot.Weapon, ItemGrade.F, ItemRarity.Uncommon, AtkBonus: 7),
        new(5,  "Hunting Bow",    EquipSlot.Weapon, ItemGrade.F, ItemRarity.Uncommon, AtkBonus: 8, WeaponRange: 400),
        new(6,  "Oak Staff",      EquipSlot.Weapon, ItemGrade.F, ItemRarity.Uncommon, AtkBonus: 5, MpBonus: 30, WeaponRange: 300),
        new(7,  "Knight's Blade", EquipSlot.Weapon, ItemGrade.F, ItemRarity.Rare,     AtkBonus: 11),

        // ----- F grade — armor ---------------------------------------------------
        new(8,  "Chainmail",      EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,   ArmorWeight.Heavy, DefBonus: 5, HpBonus: 25),
        new(9,  "Leather Vest",   EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,   ArmorWeight.Light, DefBonus: 3, EvaBonus: 4),
        new(10, "Padded Robe",    EquipSlot.Armor, ItemGrade.F, ItemRarity.Common,   ArmorWeight.Robe,  DefBonus: 2, MpBonus: 30),
        new(11, "Plate Armor",    EquipSlot.Armor, ItemGrade.F, ItemRarity.Uncommon, ArmorWeight.Heavy, DefBonus: 8, HpBonus: 45),
        new(12, "Scout Leather",  EquipSlot.Armor, ItemGrade.F, ItemRarity.Uncommon, ArmorWeight.Light, DefBonus: 5, EvaBonus: 7),
        new(13, "Mystic Robe",    EquipSlot.Armor, ItemGrade.F, ItemRarity.Rare,     ArmorWeight.Robe,  DefBonus: 4, MpBonus: 60),

        // ----- E grade (lvl 20+) ----------------------------------------------------
        new(14, "Steel Sword",    EquipSlot.Weapon, ItemGrade.E, ItemRarity.Common,   AtkBonus: 12),
        new(15, "Composite Bow",  EquipSlot.Weapon, ItemGrade.E, ItemRarity.Common,   AtkBonus: 14, WeaponRange: 400),
        new(16, "Runed Staff",    EquipSlot.Weapon, ItemGrade.E, ItemRarity.Common,   AtkBonus: 9,  MpBonus: 50, WeaponRange: 300),
        new(17, "Crusader Blade", EquipSlot.Weapon, ItemGrade.E, ItemRarity.Uncommon, AtkBonus: 17),
        new(18, "Full Plate",     EquipSlot.Armor,  ItemGrade.E, ItemRarity.Common,   ArmorWeight.Heavy, DefBonus: 13, HpBonus: 80),
        new(19, "Shadow Garb",    EquipSlot.Armor,  ItemGrade.E, ItemRarity.Common,   ArmorWeight.Light, DefBonus: 9,  EvaBonus: 11),
        new(20, "Arcane Robe",    EquipSlot.Armor,  ItemGrade.E, ItemRarity.Common,   ArmorWeight.Robe,  DefBonus: 7,  MpBonus: 90),

        // ----- Potions (Consumables) ------------------------------------------------
        // Shared 30s cooldown across all potions; higher rarity cancels lower,
        // same rarity restarts (see server PotionEffect channel).
        new(30, "Minor Healing Potion", EquipSlot.Consumable, ItemGrade.F, ItemRarity.Common,
            HealPercentPerSecond: 0.01f, PotionDurationTicks: 150, PotionCooldownTicks: 300),
        new(31, "Healing Potion",       EquipSlot.Consumable, ItemGrade.F, ItemRarity.Uncommon,
            HealPercentPerSecond: 0.02f, PotionDurationTicks: 150, PotionCooldownTicks: 300),
        new(32, "Greater Healing Potion", EquipSlot.Consumable, ItemGrade.F, ItemRarity.Rare,
            InstantHealPercent: 0.50f, PotionCooldownTicks: 300),
    }.ToDictionary(i => i.Id);

    public static bool IsPotion(ItemDef def) => def.Slot == EquipSlot.Consumable;

    public static ItemDef? Get(int id) => All.GetValueOrDefault(id);

    /// <summary>Grade gates per design doc: F 0+, E 20+, B 40+, A 60+, S 80+.</summary>
    public static int RequiredLevel(ItemGrade grade) => grade switch
    {
        ItemGrade.F => 0,
        ItemGrade.E => 20,
        ItemGrade.B => 40,
        ItemGrade.A => 60,
        _ => 80
    };
}

/// <summary>One possible drop: an item, its chance, and the mob-level band
/// it applies to. A mob rolls every entry in its table independently.</summary>
public record LootEntry(int ItemId, float Chance, int MinLevel, int MaxLevel);

/// <summary>
/// Per-mob loot tables, keyed by mob name. Each mob type drops different
/// gear; entries are gated by the killed mob's level so a low boar gives F
/// gear and a high one gives E gear (design doc).
/// Every mob also rolls the shared potion table on top of its own.
/// </summary>
public static class LootTables
{
    // Potions any mob can drop (low chance), independent of type.
    private static readonly LootEntry[] SharedPotions =
    {
        new(30, 0.10f, 1, 30),   // Minor Healing Potion (common)
        new(31, 0.04f, 4, 30),   // Healing Potion (uncommon)
        new(32, 0.01f, 8, 30),   // Greater Healing Potion (rare)
    };

    private static readonly Dictionary<string, LootEntry[]> Tables = new()
    {
        // Boar — weapons (F 1-10, E 11+)
        ["Boar"] = new[]
        {
            new LootEntry(3, 0.18f, 1, 10),   // Old Staff
            new LootEntry(1, 0.18f, 1, 10),   // Rusty Sword
            new LootEntry(6, 0.06f, 1, 10),   // Oak Staff (uncommon)
            new LootEntry(16, 0.12f, 11, 30), // Runed Staff (E)
            new LootEntry(14, 0.12f, 11, 30), // Steel Sword (E)
        },
        // Wolf — armor (F 1-10, E 11+)
        ["Wolf"] = new[]
        {
            new LootEntry(9, 0.18f, 1, 10),   // Leather Vest
            new LootEntry(8, 0.14f, 1, 10),   // Chainmail
            new LootEntry(12, 0.05f, 1, 10),  // Scout Leather (uncommon)
            new LootEntry(19, 0.12f, 11, 30), // Shadow Garb (E)
            new LootEntry(18, 0.10f, 11, 30), // Full Plate (E)
        },
        // Slime — robes & mage gear
        ["Slime"] = new[]
        {
            new LootEntry(10, 0.18f, 1, 10),  // Padded Robe
            new LootEntry(3, 0.12f, 1, 10),   // Old Staff
            new LootEntry(13, 0.05f, 1, 10),  // Mystic Robe (rare)
            new LootEntry(20, 0.12f, 11, 30), // Arcane Robe (E)
        },
        // Spider (aggressive) — light armor + daggers, better odds
        ["Spider"] = new[]
        {
            new LootEntry(9, 0.20f, 1, 10),   // Leather Vest
            new LootEntry(12, 0.10f, 1, 10),  // Scout Leather (uncommon)
            new LootEntry(5, 0.10f, 1, 10),   // Hunting Bow (uncommon)
            new LootEntry(19, 0.16f, 11, 30), // Shadow Garb (E)
            new LootEntry(15, 0.10f, 11, 30), // Composite Bow (E)
        },
        // Bandit (aggressive) — swords + the best F drops
        ["Bandit"] = new[]
        {
            new LootEntry(1, 0.18f, 1, 10),   // Rusty Sword
            new LootEntry(4, 0.10f, 1, 10),   // Soldier Sword (uncommon)
            new LootEntry(7, 0.05f, 1, 10),   // Knight's Blade (rare)
            new LootEntry(11, 0.06f, 1, 10),  // Plate Armor (uncommon)
            new LootEntry(17, 0.12f, 11, 30), // Crusader Blade (E)
        },
    };

    /// <summary>Roll all drops for one kill. Returns 0..N item ids (each table
    /// entry is an independent roll, plus the shared potion table).</summary>
    public static List<int> Roll(string mobName, int mobLevel, Random rng)
    {
        var drops = new List<int>();

        if (Tables.TryGetValue(mobName, out var table))
            RollEntries(table, mobLevel, rng, drops);

        RollEntries(SharedPotions, mobLevel, rng, drops);
        return drops;
    }

    private static void RollEntries(LootEntry[] table, int mobLevel, Random rng, List<int> drops)
    {
        foreach (var entry in table)
        {
            if (mobLevel < entry.MinLevel || mobLevel > entry.MaxLevel)
                continue;
            if (rng.NextDouble() < entry.Chance)
                drops.Add(entry.ItemId);
        }
    }
}
