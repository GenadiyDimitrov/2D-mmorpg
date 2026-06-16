namespace Game.Shared;

public enum ItemGrade { F = 0, E = 1, B = 2, A = 3, S = 4 }

public enum ItemRarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, God = 99 }

public enum EquipSlot { Weapon = 0, Armor = 1, Consumable = 2, Scroll = 3 }

public enum ArmorWeight { None = 0, Heavy = 1, Light = 2, Robe = 3 }

/// <summary>Broad weapon category. Drives which skills work and the base
/// attack range. All classes CAN equip any weapon; skills gate usefulness.</summary>
public enum WeaponType { None = 0, Sword = 1, Dual = 2, Bow = 3, Staff = 4 }

/// <summary>Enchant scroll failure behaviour (design doc):
/// Common -> item breaks on fail; Uncommon -> enchant resets to +0;
/// Rare -> enchant drops by 1 (never breaks).</summary>
public enum ScrollKind { None = 0, Common = 1, Uncommon = 2, Rare = 3 }

/// <summary>
/// An item template. The Id is a STABLE STRING KEY (e.g. "sword_e_rare") — it
/// is the item's permanent identity, stored in saves and referenced by loot
/// tables, the debug menu, etc. IDs are never renumbered; new items get new
/// keys. WeaponRange &gt; 0 marks ranged weapons (bows/staves).
/// </summary>
public record ItemDef(
    string Id,
    string Name,
    EquipSlot Slot,
    ItemGrade Grade,
    ItemRarity Rarity,
    ArmorWeight Weight = ArmorWeight.None,
    WeaponType WeaponType = WeaponType.None,
    int AtkBonus = 0,
    int DefBonus = 0,
    int HpBonus = 0,
    int MpBonus = 0,
    int EvaBonus = 0,
    float WeaponRange = 0,
    // ----- Consumables (potions) -----
    float HealPercentPerSecond = 0f,
    float InstantHealPercent = 0f,
    int PotionDurationTicks = 0,
    int PotionCooldownTicks = 0,
    ScrollKind ScrollKind = ScrollKind.None,
    // ----- Fixed (non-rolled) attributes, e.g. for the legendary one-off -----
    ItemAttribute[]? FixedAttributes = null);

public static class ItemCatalog
{
    // -----------------------------------------------------------------------
    // Stable string keys for hand-referenced items (potions, scrolls, legendary).
    // Weapon/armor keys are generated as "<type>_<grade>_<rarity>" — see below.
    // -----------------------------------------------------------------------
    public const string MinorPotion = "potion_minor";
    public const string HealingPotion = "potion_healing";
    public const string GreaterPotion = "potion_greater";
    public const string ScrollCommon = "scroll_common";
    public const string ScrollUncommon = "scroll_uncommon";
    public const string ScrollRare = "scroll_rare";
    public const string GodWeapon = "god_judgment";
    public const string GodArmor = "god_robes";

    public static string WeaponKey(WeaponType type, ItemGrade grade, ItemRarity rarity) =>
        $"{type.ToString().ToLowerInvariant()}_{grade.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    public static string ArmorKey(ArmorWeight weight, ItemGrade grade, ItemRarity rarity) =>
        $"{weight.ToString().ToLowerInvariant()}_{grade.ToString().ToLowerInvariant()}_{rarity.ToString().ToLowerInvariant()}";

    private static readonly Dictionary<string, ItemDef> All = BuildCatalog();

    private static Dictionary<string, ItemDef> BuildCatalog()
    {
        var list = new List<ItemDef>();

        // ===================================================================
        //  WEAPONS — every type x grade x rarity. All classes can equip these;
        //  whether a class's SKILLS work depends on the weapon (design doc).
        // ===================================================================
        // Per-type display names and the base attack at F-common; higher grade
        // and rarity scale up from there.
        var weaponInfo = new (WeaponType Type, string Noun, int BaseAtk, float Range, int MpBonus)[]
        {
            (WeaponType.Sword, "Sword", 6,  0,   0),
            (WeaponType.Dual,  "Daggers", 5, 0,  0),   // dual: lower per-hit, faster (handled by class)
            (WeaponType.Bow,   "Bow",   7,  400, 0),
            (WeaponType.Staff, "Staff", 4,  300, 20),  // staff: lower atk, gives MP, ranged spell
        };

        foreach (var w in weaponInfo)
        {
            foreach (var grade in new[] { ItemGrade.F, ItemGrade.E })
            {
                // Grade scaling: E roughly doubles F.
                int gradeAtk = grade == ItemGrade.F ? w.BaseAtk : w.BaseAtk * 2 + 4;
                int gradeMp = grade == ItemGrade.F ? w.MpBonus : w.MpBonus * 2;

                foreach (var rarity in new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare })
                {
                    // Rarity scaling: +40% per tier on top of grade.
                    int atk = (int)(gradeAtk * (1f + 0.40f * (int)rarity));
                    int mp = (int)(gradeMp * (1f + 0.20f * (int)rarity));

                    string gradeName = grade == ItemGrade.F ? "Worn" : "Steel";
                    string rarityName = rarity switch
                    {
                        ItemRarity.Uncommon => "Fine ",
                        ItemRarity.Rare => "Masterwork ",
                        _ => ""
                    };

                    list.Add(new ItemDef(
                        WeaponKey(w.Type, grade, rarity),
                        $"{rarityName}{gradeName} {w.Noun}",
                        EquipSlot.Weapon, grade, rarity,
                        WeaponType: w.Type,
                        AtkBonus: atk,
                        MpBonus: mp,
                        WeaponRange: w.Range));
                }
            }
        }

        // ===================================================================
        //  ARMOR — robe / light / heavy x grade x rarity.
        // ===================================================================
        var armorInfo = new (ArmorWeight Weight, string Noun, int BaseDef, int Hp, int Mp, int Eva)[]
        {
            (ArmorWeight.Heavy, "Plate",   6, 30, 0,  0),
            (ArmorWeight.Light, "Leather", 4, 10, 0,  6),
            (ArmorWeight.Robe,  "Robe",    2, 0,  30, 0),
        };

        foreach (var a in armorInfo)
        {
            foreach (var grade in new[] { ItemGrade.F, ItemGrade.E })
            {
                int gd = grade == ItemGrade.F ? a.BaseDef : a.BaseDef * 2 + 3;
                int ghp = grade == ItemGrade.F ? a.Hp : a.Hp * 2;
                int gmp = grade == ItemGrade.F ? a.Mp : a.Mp * 2;
                int gev = grade == ItemGrade.F ? a.Eva : a.Eva * 2;

                foreach (var rarity in new[] { ItemRarity.Common, ItemRarity.Uncommon, ItemRarity.Rare })
                {
                    float rmul = 1f + 0.35f * (int)rarity;
                    string gradeName = grade == ItemGrade.F ? "Worn" : "Tempered";
                    string rarityName = rarity switch
                    {
                        ItemRarity.Uncommon => "Fine ",
                        ItemRarity.Rare => "Masterwork ",
                        _ => ""
                    };

                    list.Add(new ItemDef(
                        ArmorKey(a.Weight, grade, rarity),
                        $"{rarityName}{gradeName} {a.Noun}",
                        EquipSlot.Armor, grade, rarity,
                        Weight: a.Weight,
                        DefBonus: (int)(gd * rmul),
                        HpBonus: (int)(ghp * rmul),
                        MpBonus: (int)(gmp * rmul),
                        EvaBonus: (int)(gev * rmul)));
                }
            }
        }

        // ===================================================================
        //  POTIONS
        // ===================================================================
        list.Add(new ItemDef(MinorPotion, "Minor Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Common,
            HealPercentPerSecond: 0.01f, PotionDurationTicks: 150, PotionCooldownTicks: 300));
        list.Add(new ItemDef(HealingPotion, "Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Uncommon,
            HealPercentPerSecond: 0.02f, PotionDurationTicks: 150, PotionCooldownTicks: 300));
        list.Add(new ItemDef(GreaterPotion, "Greater Healing Potion", EquipSlot.Consumable,
            ItemGrade.F, ItemRarity.Rare,
            InstantHealPercent: 0.50f, PotionCooldownTicks: 300));

        // ===================================================================
        //  ENCHANT SCROLLS
        // ===================================================================
        list.Add(new ItemDef(ScrollCommon, "Enchant Scroll (Common)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Common, ScrollKind: ScrollKind.Common));
        list.Add(new ItemDef(ScrollUncommon, "Enchant Scroll (Uncommon)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Uncommon, ScrollKind: ScrollKind.Uncommon));
        list.Add(new ItemDef(ScrollRare, "Enchant Scroll (Rare)", EquipSlot.Scroll,
            ItemGrade.F, ItemRarity.Rare, ScrollKind: ScrollKind.Rare));

        // ===================================================================
        //  GOD-TIER one-offs (debug). Every attribute maxed at 100%.
        // ===================================================================
        list.Add(new ItemDef(GodWeapon, "God's Judgment", EquipSlot.Weapon,
            ItemGrade.S, ItemRarity.God, WeaponType: WeaponType.Sword,
            AtkBonus: 1000, WeaponRange: 1000,
            FixedAttributes: new ItemAttribute[]
            {
                new(AttributeType.AttackPercent, 100),
                new(AttributeType.AttackSpeedPercent, 100),
                new(AttributeType.CastSpeedPercent, 100),
                new(AttributeType.SpeedPercent, 100),
                new(AttributeType.HealthPercent, 100),
                new(AttributeType.ManaPercent, 100),
                new(AttributeType.EvasionPercent, 100),
                new(AttributeType.DefencePercent, 100),
            }));

        list.Add(new ItemDef(GodArmor, "God's Robes", EquipSlot.Armor,
            ItemGrade.S, ItemRarity.God, Weight: ArmorWeight.Robe,
            DefBonus: 1000, HpBonus: 1000, MpBonus: 1000, EvaBonus: 1000,
            FixedAttributes: new ItemAttribute[]
            {
                new(AttributeType.HealthPercent, 100),
                new(AttributeType.ManaPercent, 100),
                new(AttributeType.DefencePercent, 100),
                new(AttributeType.EvasionPercent, 100),
                new(AttributeType.SpeedPercent, 100),
                new(AttributeType.CastSpeedPercent, 100),
            }));

        // ----- Duplicate-key guard: clear startup error instead of a crash ---
        var dict = new Dictionary<string, ItemDef>();
        foreach (var item in list)
        {
            if (!dict.TryAdd(item.Id, item))
                throw new InvalidOperationException(
                    $"Duplicate item id '{item.Id}' ({item.Name} collides with {dict[item.Id].Name}).");
        }
        return dict;
    }

    public static ItemDef? Get(string id) => id is null ? null : All.GetValueOrDefault(id);

    public static IEnumerable<ItemDef> AllItems => All.Values;

    public static bool IsPotion(ItemDef def) => def.Slot == EquipSlot.Consumable;
    public static bool IsScroll(ItemDef def) => def.Slot == EquipSlot.Scroll;
    public static bool IsEquippable(ItemDef def) => def.Slot is EquipSlot.Weapon or EquipSlot.Armor;

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

/// <summary>One possible drop: an item key, its chance, and the mob-level band
/// it applies to.</summary>
public record LootEntry(string ItemId, float Chance, int MinLevel, int MaxLevel);

/// <summary>
/// Per-mob loot tables, keyed by mob name. Each mob drops different gear;
/// entries are gated by the killed mob's level (low = F, high = E grade).
/// Every mob also rolls the shared potion + scroll tables.
/// </summary>
public static class LootTables
{
    private static readonly LootEntry[] SharedPotions =
    {
        new(ItemCatalog.MinorPotion, 0.10f, 1, 30),
        new(ItemCatalog.HealingPotion, 0.04f, 4, 30),
        new(ItemCatalog.GreaterPotion, 0.01f, 8, 30),
    };

    // Scrolls: rarer than everything else, higher-level mobs only.
    private static readonly LootEntry[] SharedScrolls =
    {
        new(ItemCatalog.ScrollCommon, 0.030f, 5, 30),
        new(ItemCatalog.ScrollUncommon, 0.015f, 9, 30),
        new(ItemCatalog.ScrollRare, 0.006f, 13, 30),
    };

    private static readonly Dictionary<string, LootEntry[]> Tables = BuildTables();

    private static Dictionary<string, LootEntry[]> BuildTables()
    {
        // Helper to roll an F (low level) and E (high level) entry for a key.
        LootEntry F(WeaponType t, ItemRarity r, float c) =>
            new(ItemCatalog.WeaponKey(t, ItemGrade.F, r), c, 1, 10);
        LootEntry E(WeaponType t, ItemRarity r, float c) =>
            new(ItemCatalog.WeaponKey(t, ItemGrade.E, r), c, 11, 30);
        LootEntry FA(ArmorWeight w, ItemRarity r, float c) =>
            new(ItemCatalog.ArmorKey(w, ItemGrade.F, r), c, 1, 10);
        LootEntry EA(ArmorWeight w, ItemRarity r, float c) =>
            new(ItemCatalog.ArmorKey(w, ItemGrade.E, r), c, 11, 30);

        return new Dictionary<string, LootEntry[]>
        {
            ["Boar"] = new[]
            {
                F(WeaponType.Staff, ItemRarity.Common, 0.16f),
                F(WeaponType.Sword, ItemRarity.Common, 0.16f),
                F(WeaponType.Staff, ItemRarity.Uncommon, 0.06f),
                E(WeaponType.Staff, ItemRarity.Common, 0.12f),
                E(WeaponType.Sword, ItemRarity.Common, 0.12f),
            },
            ["Wolf"] = new[]
            {
                FA(ArmorWeight.Light, ItemRarity.Common, 0.16f),
                FA(ArmorWeight.Heavy, ItemRarity.Common, 0.14f),
                FA(ArmorWeight.Light, ItemRarity.Uncommon, 0.05f),
                EA(ArmorWeight.Light, ItemRarity.Common, 0.12f),
                EA(ArmorWeight.Heavy, ItemRarity.Common, 0.10f),
            },
            ["Slime"] = new[]
            {
                FA(ArmorWeight.Robe, ItemRarity.Common, 0.16f),
                F(WeaponType.Staff, ItemRarity.Common, 0.12f),
                FA(ArmorWeight.Robe, ItemRarity.Rare, 0.05f),
                EA(ArmorWeight.Robe, ItemRarity.Common, 0.12f),
            },
            ["Spider"] = new[]
            {
                FA(ArmorWeight.Light, ItemRarity.Common, 0.18f),
                F(WeaponType.Dual, ItemRarity.Uncommon, 0.10f),
                F(WeaponType.Bow, ItemRarity.Uncommon, 0.10f),
                EA(ArmorWeight.Light, ItemRarity.Common, 0.14f),
                E(WeaponType.Bow, ItemRarity.Common, 0.10f),
            },
            ["Bandit"] = new[]
            {
                F(WeaponType.Sword, ItemRarity.Common, 0.16f),
                F(WeaponType.Sword, ItemRarity.Uncommon, 0.10f),
                F(WeaponType.Sword, ItemRarity.Rare, 0.04f),
                FA(ArmorWeight.Heavy, ItemRarity.Uncommon, 0.06f),
                E(WeaponType.Sword, ItemRarity.Common, 0.12f),
            },
        };
    }

    public static List<string> Roll(string mobName, int mobLevel, Random rng)
    {
        var drops = new List<string>();
        if (Tables.TryGetValue(mobName, out var table))
            RollEntries(table, mobLevel, rng, drops);
        RollEntries(SharedPotions, mobLevel, rng, drops);
        RollEntries(SharedScrolls, mobLevel, rng, drops);
        return drops;
    }

    private static void RollEntries(LootEntry[] table, int mobLevel, Random rng, List<string> drops)
    {
        foreach (var entry in table)
        {
            if (mobLevel < entry.MinLevel || mobLevel > entry.MaxLevel) continue;
            if (rng.NextDouble() < entry.Chance) drops.Add(entry.ItemId);
        }
    }
}
