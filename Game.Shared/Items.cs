namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2 }

public enum EquipSlot { Weapon = 0, Armor = 1 }

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
    float WeaponRange = 0);

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
    }.ToDictionary(i => i.Id);

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

    /// <summary>Roll a drop for a mob kill. Null = no drop. 30% drop chance;
    /// of drops: 70% common, 25% uncommon, 5% rare. Higher-level mobs can
    /// drop E grade.</summary>
    public static ItemDef? RollDrop(int mobLevel, Random rng)
    {
        if (rng.NextDouble() >= 0.30)
            return null;

        double rarityRoll = rng.NextDouble();
        var rarity = rarityRoll < 0.70 ? ItemRarity.Common
            : rarityRoll < 0.95 ? ItemRarity.Uncommon
            : ItemRarity.Rare;

        var grade = mobLevel >= 13 && rng.NextDouble() < 0.5 ? ItemGrade.E : ItemGrade.F;

        var pool = All.Values
            .Where(i => i.Grade == grade && i.Rarity == rarity)
            .ToArray();

        // E grade has no uncommon armor / rare items yet — degrade gracefully.
        if (pool.Length == 0)
            pool = All.Values.Where(i => i.Grade == grade).ToArray();

        return pool.Length == 0 ? null : pool[rng.Next(pool.Length)];
    }
}
