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
    // Caster-weapon (wand/staff) rolls — APPEND-ONLY.
    MagicAttackPercent = 13, // +% M.Atk
    MagicCritRate = 14,      // magic crit-rate points (percent)
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

    public static AttributeType[] WeaponPool(WeaponType w) => w.Base() switch
    {
        WeaponType.Sword => new[] { AttributeType.AttackSpeedPercent, AttributeType.AttackPercent, AttributeType.CritRate },
        WeaponType.Blunt => new[] { AttributeType.HealthPercent, AttributeType.AttackPercent, AttributeType.CastSpeedPercent, AttributeType.CritDamage },
        WeaponType.Bow   => new[] { AttributeType.CritRate, AttributeType.CritDamage, AttributeType.AttackSpeedPercent, AttributeType.AttackPercent },
        WeaponType.Dual  => new[] { AttributeType.CritRate, AttributeType.CritDamage, AttributeType.SpeedPercent, AttributeType.EvasionPercent },
        _ => Array.Empty<AttributeType>()
    };

    /// <summary>Per-slot pool for the WEIGHTLESS accessory pieces (Head/Gloves/Boots).
    /// Each carries a single attribute chosen from its slot's pair.</summary>
    public static AttributeType[] ArmorSlotPool(ArmorSlot slot) => slot switch
    {
        ArmorSlot.Head   => new[] { AttributeType.HpRegen, AttributeType.MpRegen },
        ArmorSlot.Gloves => new[] { AttributeType.AttackSpeedPercent, AttributeType.CastSpeedPercent },
        ArmorSlot.Boots  => new[] { AttributeType.SpeedPercent, AttributeType.EvasionPercent },
        _ => Array.Empty<AttributeType>()
    };

    /// <summary>The attribute pool an item rolls from: weapons by type, BODY armor by
    /// weight, accessory armor by slot.</summary>
    public static AttributeType[] PoolFor(ItemDef def)
    {
        if (def.Slot == EquipSlot.Weapon) return WeaponPool(def.WeaponType);
        if (def.Slot == EquipSlot.Armor)
            return def.ArmorSlot == ArmorSlot.Body ? ArmorPool(def.Weight) : ArmorSlotPool(def.ArmorSlot);
        if (def.Slot == EquipSlot.Jewel) return JewelPool;
        return Array.Empty<AttributeType>();
    }

    /// <summary>Jewel attribute pool — jewels carry flat max HP/MP, regen and crit
    /// rolls (their base stat is magic defence; these are the "extra" rolls).</summary>
    public static readonly AttributeType[] JewelPool =
    {
        AttributeType.HealthPercent, AttributeType.ManaPercent,
        AttributeType.HpRegen, AttributeType.MpRegen,
        AttributeType.CritRate, AttributeType.CritDamage,
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

    /// <summary>An item attribute as a <see cref="StatMods"/> bundle — the single mapping the
    /// StatMods-based item/set application reads (percent points → fractions, flats pass through).
    /// AttackPercent hits BOTH channels (as today); MagicAttackPercent is caster-only.</summary>
    public static StatMods ToStatMods(ItemAttribute a)
    {
        float v = a.Value;
        float f = v / 100f;   // percent points → fraction
        return a.Type switch
        {
            AttributeType.HealthPercent      => new StatMods(MaxHpPct: f),
            AttributeType.ManaPercent        => new StatMods(MaxMpPct: f),
            AttributeType.SpeedPercent       => new StatMods(MoveSpeedPct: f),
            AttributeType.CastSpeedPercent   => new StatMods(CastSpeedPct: f),
            AttributeType.AttackSpeedPercent => new StatMods(AtkSpeedPct: f),
            AttributeType.AttackPercent      => new StatMods(PAtkPct: f, MAtkPct: f),
            AttributeType.EvasionPercent     => new StatMods(EvasionPct: f),
            AttributeType.DefencePercent     => new StatMods(PDefPct: f),
            AttributeType.Accuracy           => new StatMods(Accuracy: v),
            AttributeType.HpRegen            => new StatMods(HpRegen: v),
            AttributeType.MpRegen            => new StatMods(MpRegen: v),
            AttributeType.CritRate           => new StatMods(CritRate: f),
            AttributeType.CritDamage         => new StatMods(CritDamage: f),
            AttributeType.MagicAttackPercent => new StatMods(MAtkPct: f),
            AttributeType.MagicCritRate      => new StatMods(MagicCritRate: f),
            _                                => default,
        };
    }

    // ===================================================================================
    //  LEVEL-TIER weapon attributes (docs/gear/gear_sets.csv). Count comes from the item's
    //  LEVEL (not grade+rarity): 40→1, 52→1, 61→2, 76→3 (20 = none). The MAX of each attribute
    //  is per (weapon family, attribute, tier). Caster weapons (IsMagicWeapon) roll the caster pool.
    // ===================================================================================
    public static int TieredWeaponAttributeCount(int itemLevel) =>
        itemLevel >= 76 ? 3 : itemLevel >= 61 ? 2 : itemLevel >= 40 ? 1 : 0;

    public static AttributeType[] TieredWeaponPool(WeaponType type, bool isMagic)
    {
        if (isMagic)
            return new[] { AttributeType.CastSpeedPercent, AttributeType.MagicAttackPercent, AttributeType.MagicCritRate };
        return type.Base() switch
        {
            WeaponType.Sword => new[] { AttributeType.CritRate, AttributeType.AttackSpeedPercent, AttributeType.HealthPercent },
            WeaponType.Blunt => new[] { AttributeType.CritDamage, AttributeType.HealthPercent },
            WeaponType.Dual  => new[] { AttributeType.CritDamage, AttributeType.CritRate, AttributeType.AttackSpeedPercent },
            WeaponType.Bow   => new[] { AttributeType.CritDamage, AttributeType.CritRate, AttributeType.AttackPercent },
            _ => Array.Empty<AttributeType>()
        };
    }

    private static int TierIdx(int level) => level >= 76 ? 3 : level >= 61 ? 2 : level >= 52 ? 1 : 0;

    /// <summary>Max value of a tiered weapon attribute at its level (from the CSV pools).</summary>
    public static int TieredWeaponMax(WeaponType type, bool isMagic, AttributeType attr, int level)
    {
        int i = TierIdx(level);
        return (isMagic, type.Base(), attr) switch
        {
            (true, _, AttributeType.CastSpeedPercent)   => new[] { 15, 15, 15, 15 }[i],
            (true, _, AttributeType.MagicAttackPercent) => new[] { 5, 7, 10, 15 }[i],
            (true, _, AttributeType.MagicCritRate)      => new[] { 5, 6, 8, 10 }[i],
            (false, WeaponType.Sword, AttributeType.CritRate)           => new[] { 5, 10, 15, 20 }[i],
            (false, WeaponType.Sword, AttributeType.AttackSpeedPercent) => new[] { 5, 7, 10, 15 }[i],
            (false, WeaponType.Sword, AttributeType.HealthPercent)      => new[] { 10, 15, 20, 25 }[i],
            (false, WeaponType.Blunt, AttributeType.CritDamage)         => new[] { 15, 20, 25, 30 }[i],
            (false, WeaponType.Blunt, AttributeType.HealthPercent)      => new[] { 20, 25, 30, 35 }[i],
            (false, WeaponType.Dual, AttributeType.CritDamage)          => new[] { 20, 25, 30, 35 }[i],
            (false, WeaponType.Dual, AttributeType.CritRate)            => new[] { 10, 15, 20, 25 }[i],
            (false, WeaponType.Dual, AttributeType.AttackSpeedPercent)  => new[] { 5, 10, 15, 20 }[i],
            (false, WeaponType.Bow, AttributeType.CritDamage)           => new[] { 20, 25, 30, 35 }[i],
            (false, WeaponType.Bow, AttributeType.CritRate)             => new[] { 10, 15, 20, 25 }[i],
            (false, WeaponType.Bow, AttributeType.AttackPercent)        => new[] { 5, 6, 8, 10 }[i],
            _ => 0
        };
    }

    /// <summary>Roll a fresh set of attributes for a dropped item instance: pick
    /// distinct types from the item's pool (by weapon type / armor weight), each
    /// rolled within its grade range.</summary>
    public static List<ItemAttribute> Roll(ItemDef def, Random rng)
    {
        var result = new List<ItemAttribute>();
        if (def.NoAttributes) return result;   // newbie/starter gear never rolls attributes

        // Level-tier WEAPONS: count + max by level (armor tiers carry no attributes for now).
        if (def.Slot == EquipSlot.Weapon && def.ItemLevel > 0)
        {
            var wpool = TieredWeaponPool(def.WeaponType, def.IsMagicWeapon).ToList();
            int wc = Math.Min(TieredWeaponAttributeCount(def.ItemLevel), wpool.Count);
            for (int i = 0; i < wc; i++)
            {
                int idx = rng.Next(wpool.Count);
                var t = wpool[idx];
                wpool.RemoveAt(idx);
                int max = TieredWeaponMax(def.WeaponType, def.IsMagicWeapon, t, def.ItemLevel);
                if (max > 0) result.Add(new ItemAttribute(t, rng.Next(1, max + 1)));
            }
            return result;
        }

        AttributeType[] pool = PoolFor(def);
        if (pool.Length == 0) return result;

        // Armor uses FIXED per-slot counts (body 2, accessories 1) so grade/rarity only
        // changes the VALUE, not the count; jewels carry 1; weapons keep grade+rarity.
        int desired = def.Slot == EquipSlot.Armor
            ? (def.ArmorSlot == ArmorSlot.Body ? 2 : 1)
            : def.Slot == EquipSlot.Jewel ? 1
            : AttributeCount(def.Grade, def.Rarity);
        int count = Math.Min(desired, pool.Length);
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

    /// <summary>How many attribute slots a scroll tier lets you LOCK (keep) while the
    /// rest reroll. Legendary "locks all" conceptually — it rerolls every slot at MAX.</summary>
    public static int RerollLockCapacity(AttrScrollKind kind) => kind switch
    {
        AttrScrollKind.Common => 0,
        AttrScrollKind.Uncommon => 1,
        AttrScrollKind.Rare => 2,
        AttrScrollKind.Legendary => int.MaxValue,
        _ => 0
    };

    /// <summary>Reroll the UNLOCKED attribute slots of an item. Locked slots keep their
    /// exact type+value; unlocked slots get a fresh DISTINCT type from the item's pool
    /// and a value rolled in range — or the MAX of the range when <paramref name="forceMax"/>
    /// (legendary scroll). Slot count and order are preserved.</summary>
    public static List<ItemAttribute> Reroll(ItemDef def, IReadOnlyList<ItemAttribute> current,
        bool[] locked, bool forceMax, Random rng)
    {
        AttributeType[] pool = PoolFor(def);

        var result = new List<ItemAttribute>(current.Count);
        var taken = new HashSet<AttributeType>();
        for (int i = 0; i < current.Count; i++)
            if (i < locked.Length && locked[i]) taken.Add(current[i].Type);

        for (int i = 0; i < current.Count; i++)
        {
            if (i < locked.Length && locked[i]) { result.Add(current[i]); continue; }

            var choices = pool.Where(t => !taken.Contains(t)).ToList();
            if (choices.Count == 0) { result.Add(current[i]); continue; } // pool exhausted: keep

            var type = choices[rng.Next(choices.Count)];
            taken.Add(type);
            var (min, max) = Range(type, def.Grade);
            result.Add(new ItemAttribute(type, forceMax ? max : rng.Next(min, max + 1)));
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
        AttributeType.MagicAttackPercent => "M.Atk",
        AttributeType.MagicCritRate => "Magic Crit Rate",
        _ => type.ToString()
    };
}
