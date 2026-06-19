namespace Game.Shared;

/// <summary>
/// Bonus attributes that roll randomly onto an item when it drops. WHICH types can
/// roll is decided by the item's WEAPON TYPE / ARMOR WEIGHT (the pool); HOW BIG a
/// roll is scales with GRADE; HOW MANY roll comes from grade + rarity. The MAX of
/// each range is the ceiling attribute scrolls will let you grind toward (Phase 15).
/// </summary>
public enum AttributeType
{
    HealthPercent = 0,
    ManaPercent = 1,
    SpeedPercent = 2,       // move speed
    CastSpeedPercent = 3,   // shortens cast time (stacks with WIT)
    AttackSpeedPercent = 4, // shortens basic-attack interval
    AttackPercent = 5,
    EvasionPercent = 6,
    DefencePercent = 7,
    // Phase 14 additions — APPEND-ONLY (stored by int in saves; never reorder).
    Accuracy = 8,    // flat accuracy points
    HpRegen = 9,     // flat HP per second
    MpRegen = 10,    // flat MP per second
    CritRate = 11,   // physical crit-rate points (percent)
    CritDamage = 12, // crit-damage bonus points (percent of the crit multiplier)
}

/// <summary>One rolled attribute on an item instance.</summary>
public record ItemAttribute
{
    public AttributeType Type { get; set; }
    public int Value { get; set; }

    public ItemAttribute() { }
    public ItemAttribute(AttributeType type, int value) { Type = type; Value = value; }
}

public static class AttributeSystem
{
    /// <summary>How many attributes an item rolls, by grade + rarity (design doc):
    /// F: common 0 / uncommon 1 / rare 2; E: common 1 / uncommon 2 / rare 3.
    /// Higher grades follow the same +1-per-grade pattern.</summary>
    public static int AttributeCount(ItemGrade grade, ItemRarity rarity)
    {
        int gradeBonus = (int)grade;                 // F=0, E=1, B=2, A=3, S=4
        int rarityBonus = rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 1,
            ItemRarity.Rare => 2,
            ItemRarity.Epic => 3,
            ItemRarity.Legendary => 4,
            ItemRarity.God => 6,
            _ => 0
        };
        return gradeBonus + rarityBonus;
    }

    // ----- WHICH attributes can roll (the pool), by weapon type / armor weight -----
    // Phase 14 design. Anything else (jewels, etc.) rolls nothing.

    public static AttributeType[] WeaponPool(WeaponType w) => w switch
    {
        WeaponType.Sword => new[] { AttributeType.AttackSpeedPercent, AttributeType.AttackPercent, AttributeType.CritRate },
        WeaponType.Blunt => new[] { AttributeType.HealthPercent, AttributeType.AttackPercent, AttributeType.CastSpeedPercent, AttributeType.CritDamage },
        WeaponType.Bow   => new[] { AttributeType.CritRate, AttributeType.CritDamage, AttributeType.AttackSpeedPercent, AttributeType.AttackPercent },
        WeaponType.Dual  => new[] { AttributeType.CritRate, AttributeType.CritDamage, AttributeType.SpeedPercent, AttributeType.EvasionPercent },
        _ => Array.Empty<AttributeType>()
    };

    public static AttributeType[] ArmorPool(ArmorWeight a) => a switch
    {
        // Heavy: hp, attack-speed, hp-regen, accuracy.
        ArmorWeight.Heavy => new[]
        {
            AttributeType.HealthPercent, AttributeType.AttackSpeedPercent,
            AttributeType.HpRegen, AttributeType.Accuracy
        },
        // Light: the versatile set (eva/acc, regen, hp/mp, as/cast).
        ArmorWeight.Light => new[]
        {
            AttributeType.EvasionPercent, AttributeType.Accuracy,
            AttributeType.HpRegen, AttributeType.MpRegen,
            AttributeType.HealthPercent, AttributeType.ManaPercent,
            AttributeType.AttackSpeedPercent, AttributeType.CastSpeedPercent
        },
        // Robe: cast, mp-regen, max mp.
        ArmorWeight.Robe => new[]
        {
            AttributeType.CastSpeedPercent, AttributeType.MpRegen, AttributeType.ManaPercent
        },
        _ => Array.Empty<AttributeType>()
    };

    /// <summary>HOW BIG a roll of a given type is at a given grade. Percent stats
    /// share one curve; flat (accuracy, regen) and crit stats get their own smaller
    /// curves. Tune freely — these are placeholders.</summary>
    public static (int Min, int Max) Range(AttributeType type, ItemGrade grade)
    {
        int g = (int)grade; // F0 .. S4
        return type switch
        {
            AttributeType.Accuracy   => (1 + g, 5 + g * 4),      // flat accuracy
            AttributeType.HpRegen    => (1 + g, 3 + g * 2),      // flat HP/s
            AttributeType.MpRegen    => (1 + g, 3 + g * 2),      // flat MP/s
            AttributeType.CritRate   => (1 + g, 3 + g * 2),      // % points (precious)
            AttributeType.CritDamage => (2 + g * 2, 8 + g * 4),  // % of crit multiplier
            _                        => (1 + g * 2, 10 + g * 8), // generic percent stats
        };
    }

    /// <summary>Percent stats render with a "%"; flat stats (accuracy, regen) don't.</summary>
    public static bool IsPercent(AttributeType type) => type switch
    {
        AttributeType.Accuracy or AttributeType.HpRegen or AttributeType.MpRegen => false,
        _ => true
    };

    /// <summary>Roll a fresh set of attributes for a dropped item instance: pick
    /// distinct types from the item's pool (by weapon type / armor weight), each
    /// rolled within its grade range.</summary>
    public static List<ItemAttribute> Roll(ItemDef def, Random rng)
    {
        var result = new List<ItemAttribute>();

        AttributeType[] pool = def.Slot switch
        {
            EquipSlot.Weapon => WeaponPool(def.WeaponType),
            EquipSlot.Armor => ArmorPool(def.Weight),
            _ => Array.Empty<AttributeType>()
        };
        if (pool.Length == 0) return result;

        int count = Math.Min(AttributeCount(def.Grade, def.Rarity), pool.Length);
        if (count <= 0) return result;

        var available = pool.ToList();
        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(available.Count);
            var type = available[idx];
            available.RemoveAt(idx); // distinct types

            var (min, max) = Range(type, def.Grade);
            result.Add(new ItemAttribute(type, rng.Next(min, max + 1)));
        }

        return result;
    }

    public static string DisplayName(AttributeType type) => type switch
    {
        AttributeType.HealthPercent => "Max HP",
        AttributeType.ManaPercent => "Max MP",
        AttributeType.SpeedPercent => "Move Speed",
        AttributeType.CastSpeedPercent => "Cast Speed",
        AttributeType.AttackSpeedPercent => "Attack Speed",
        AttributeType.AttackPercent => "Attack",
        AttributeType.EvasionPercent => "Evasion",
        AttributeType.DefencePercent => "Defence",
        AttributeType.Accuracy => "Accuracy",
        AttributeType.HpRegen => "HP Regen",
        AttributeType.MpRegen => "MP Regen",
        AttributeType.CritRate => "Crit Rate",
        AttributeType.CritDamage => "Crit Damage",
        _ => type.ToString()
    };
}
