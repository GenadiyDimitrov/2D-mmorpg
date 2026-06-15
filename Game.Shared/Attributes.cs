namespace Game.Shared;

/// <summary>
/// Bonus attributes that roll randomly onto an item when it drops. The TYPE
/// pool and the roll RANGES both scale with grade (F is a small, weak pool;
/// E unlocks more types and stronger rolls). Counts come from rarity.
/// </summary>
public enum AttributeType
{
    HealthPercent = 0,
    ManaPercent = 1,
    SpeedPercent = 2,
    CastSpeedPercent = 3,   // shortens cast time (stacks with WIT)
    AttackSpeedPercent = 4, // shortens basic-attack interval
    AttackPercent = 5
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
        int rarityBonus = (int)rarity;               // common=0, uncommon=1, rare=2
        return gradeBonus + rarityBonus;
    }

    /// <summary>(type -> (min, max)) roll range available at a given grade.
    /// The pool GROWS and the ranges STRENGTHEN as grade climbs.</summary>
    private static readonly Dictionary<ItemGrade, Dictionary<AttributeType, (int Min, int Max)>> Pools = new()
    {
        [ItemGrade.F] = new()
        {
            [AttributeType.HealthPercent] = (1, 10),
            [AttributeType.SpeedPercent]  = (1, 10),
        },
        [ItemGrade.E] = new()
        {
            [AttributeType.HealthPercent]      = (10, 30),
            [AttributeType.ManaPercent]        = (10, 30),
            [AttributeType.SpeedPercent]       = (1, 20),
            [AttributeType.CastSpeedPercent]   = (1, 20),
            [AttributeType.AttackSpeedPercent] = (1, 20),
            [AttributeType.AttackPercent]      = (1, 20),
        },
        // B/A/S inherit E's pool with stronger ranges until tuned individually.
        [ItemGrade.B] = new()
        {
            [AttributeType.HealthPercent]      = (20, 45),
            [AttributeType.ManaPercent]        = (20, 45),
            [AttributeType.SpeedPercent]       = (5, 25),
            [AttributeType.CastSpeedPercent]   = (5, 25),
            [AttributeType.AttackSpeedPercent] = (5, 25),
            [AttributeType.AttackPercent]      = (5, 25),
        },
        [ItemGrade.A] = new()
        {
            [AttributeType.HealthPercent]      = (30, 55),
            [AttributeType.ManaPercent]        = (30, 55),
            [AttributeType.SpeedPercent]       = (8, 30),
            [AttributeType.CastSpeedPercent]   = (8, 30),
            [AttributeType.AttackSpeedPercent] = (8, 30),
            [AttributeType.AttackPercent]      = (8, 30),
        },
        [ItemGrade.S] = new()
        {
            [AttributeType.HealthPercent]      = (40, 70),
            [AttributeType.ManaPercent]        = (40, 70),
            [AttributeType.SpeedPercent]       = (10, 35),
            [AttributeType.CastSpeedPercent]   = (10, 35),
            [AttributeType.AttackSpeedPercent] = (10, 35),
            [AttributeType.AttackPercent]      = (10, 35),
        },
    };

    /// <summary>Roll a fresh set of attributes for a dropped item instance.
    /// Picks distinct types from the grade pool, each rolled in its range.</summary>
    public static List<ItemAttribute> Roll(ItemDef def, Random rng)
    {
        var result = new List<ItemAttribute>();

        // Only weapons/armor carry attributes.
        if (def.Slot is not (EquipSlot.Weapon or EquipSlot.Armor))
            return result;

        int count = AttributeCount(def.Grade, def.Rarity);
        if (count <= 0 || !Pools.TryGetValue(def.Grade, out var pool) || pool.Count == 0)
            return result;

        var available = pool.Keys.ToList();
        count = Math.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(available.Count);
            var type = available[idx];
            available.RemoveAt(idx); // distinct types

            var (min, max) = pool[type];
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
        _ => type.ToString()
    };
}
