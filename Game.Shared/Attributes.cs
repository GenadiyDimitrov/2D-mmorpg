namespace Game.Shared;

/// <summary>
/// A bonus attribute on an item. Since 0.45.0 an item carries AT MOST ONE, and it is
/// NEVER rolled on drop — a dropped item is always bare, and the ONLY way an attribute
/// appears is an attribute scroll (see <see cref="AttributeSystem.ApplyScroll"/>).
///
/// That is the whole economic point (owner, 2026-08-02): you don't burn scrolls on trash
/// when the next drop can be a better base. Item QUALITY (Common…Mythic) is irrelevant to
/// attributes — the table below is absolute, so a Common sword can carry the same maximum
/// roll as a Mythic one. Quality buys raw stats and set identity; scrolls buy the attribute.
///
/// ARMOR carries no attribute at all — armor identity is its SET bonus.
/// </summary>
public enum AttributeType
{
    HealthPercent = 0,
    ManaPercent = 1,
    SpeedPercent = 2,       // move speed
    CastSpeedPercent = 3,   // shortens cast time (stacks with WIT)
    AttackSpeedPercent = 4, // shortens basic-attack interval
    AttackPercent = 5,      // BOTH channels (legacy / god gear)
    EvasionPercent = 6,
    DefencePercent = 7,
    // Phase 14 additions — APPEND-ONLY (stored by int in saves; never reorder).
    Accuracy = 8,    // LEGACY flat accuracy points — no longer rollable, kept for old saves
    HpRegen = 9,     // LEGACY flat HP per second — ditto
    MpRegen = 10,    // LEGACY flat MP per second — ditto
    CritRate = 11,   // physical crit-rate points (percent)
    CritDamage = 12, // crit-damage bonus points (percent of the crit multiplier)
    // Caster-weapon (wand/staff) rolls — APPEND-ONLY.
    MagicAttackPercent = 13, // +% M.Atk
    MagicCritRate = 14,      // magic crit-rate points (percent)
    // 0.45.0 — the scroll-only set. Accuracy/regen became PERCENT because their base stats
    // now grow with level (accuracy = DEX + level), so a flat roll would decay to nothing.
    AccuracyPercent = 15,
    HpRegenPercent = 16,
    MpRegenPercent = 17,
    PhysicalAttackPercent = 18, // P.Atk only (the necklace roll; AttackPercent hits both)
    // 2026-08-07 — FLAT evasion points, and the dual weapon's roll from now on. EvasionPercent (6)
    // multiplied the WHOLE stat, DEX + level included, so a maxed roll grew forever and handed a
    // rogue ~30 points at level 36 — three times the entire authored evasion budget, and the reason
    // a same-level mob missed him 35% of the time. Owner's ruling: *"the max for an evasion roll on
    // a weapon should be 5% at max, our evasion is not like L2's so 5 roll is a flat 5% increase."*
    // Since 1 evasion point IS 1% miss (StatCaps.AvoidStatSlope), a flat +5 IS his "5% at max".
    // EvasionPercent stays in the enum, unrollable, so items already carrying it still resolve.
    Evasion = 19,
}

/// <summary>The GRADE band an item's attribute table is read at. This is the real ladder
/// (<see cref="ItemDef.ItemLevel"/> 40/52/61/76/80), NOT the <see cref="ItemGrade"/> enum,
/// which has no C/D and exists for pricing only. Below 40 there are no attributes.</summary>
public enum AttrTier { None = -1, D = 0, C = 1, B = 2, A = 3, S = 4 }

/// <summary>What a scroll DOES to the item's attribute.</summary>
public enum AttrScrollAction
{
    /// <summary>Pick a fresh random type from the item's pool, value random in [min, max].</summary>
    RollType,
    /// <summary>Keep the existing type, re-roll the value random in [min, max].</summary>
    RerollValue,
    /// <summary>Keep the existing type, re-roll the value in the TOP HALF: [min + (max−min)/2, max].</summary>
    RerollValueHigh,
    /// <summary>Pick a fresh random type, value always the MAXIMUM (S-grade only).</summary>
    RollTypeMax,
}

/// <summary>One rolled attribute on an item instance.</summary>
public record ItemAttribute
{
    public AttributeType Type { get; set; }
    public int Value { get; set; }

    public ItemAttribute() { }
    public ItemAttribute(AttributeType type, int value) { Type = type; Value = value; }
}

/// <summary>The outcome of applying an attribute scroll.</summary>
public readonly record struct AttrScrollResult(bool Ok, ItemAttribute? Attribute, string Message);

public static class AttributeSystem
{
    // ===================================================================================
    //  THE LADDER. One row per (item family, attribute): the MIN at each of D/C/B/A/S and
    //  the MAX at each. S is a single value in the owner's table, which falls out of this
    //  shape naturally (min[4] == max[4]).
    // ===================================================================================

    /// <summary>One authored attribute line: the type plus its per-tier [min, max] window.</summary>
    public readonly record struct AttrRange(AttributeType Type, int[] Min, int[] Max)
    {
        public (int Min, int Max) At(AttrTier tier)
        {
            int i = (int)tier;
            return i < 0 ? (0, 0) : (Min[i], Max[i]);
        }
    }

    private static AttrRange Line(AttributeType t, int[] min, int max) =>
        new(t, min, new[] { max, max, max, max, max });

    // Shared min ramps — the owner authored only a handful of distinct shapes.
    private static readonly int[] RampFast = { 1, 5, 7, 10, 15 };  // → cap 15 (speeds)
    private static readonly int[] RampWide = { 1, 5, 10, 20, 30 }; // → cap 30 or 35
    private static readonly int[] RampMAtk = { 1, 3, 5, 7, 10 };   // → cap 10
    private static readonly int[] RampHp   = { 1, 5, 10, 15, 25 }; // → cap 25 (note: A is 15, not 20)
    private static readonly int[] RampCrit = { 1, 5, 10, 20, 35 }; // → cap 35
    private static readonly int[] RampJewel = { 1, 2, 3, 4, 5 };   // → cap 5

    private static readonly AttrRange[] MagicWeapon =
    {
        Line(AttributeType.CastSpeedPercent, RampFast, 15),
        Line(AttributeType.MagicAttackPercent, RampMAtk, 10),
        Line(AttributeType.ManaPercent, RampWide, 30),
    };

    private static readonly AttrRange[] SwordWeapon =
    {
        Line(AttributeType.AttackSpeedPercent, RampFast, 15),
        Line(AttributeType.CritRate, RampWide, 30),
        Line(AttributeType.HealthPercent, RampHp, 25),
    };

    private static readonly AttrRange[] BluntWeapon =
    {
        Line(AttributeType.AttackSpeedPercent, RampFast, 15),
        Line(AttributeType.CritDamage, RampCrit, 35),
        Line(AttributeType.HealthPercent, RampHp, 25),
    };

    // The dagger/dual pool. ⚠ Evasion is FLAT and capped at 5 (owner, 2026-08-07) — it was
    // EvasionPercent on RampWide/30, a multiplier on a stat that already contains DEX + level, and
    // it alone tripled the rogue's evasion budget. RampEva is its own ramp: no other line may share
    // it, because every other line here is a percentage where 30 is a sane ceiling.
    private static readonly int[] RampEva = { 1, 2, 3, 4, 5 };     // → cap 5 (FLAT evasion points)

    private static readonly AttrRange[] DualWeapon =
    {
        Line(AttributeType.Evasion, RampEva, 5),
        Line(AttributeType.CritRate, RampWide, 30),
        Line(AttributeType.CritDamage, RampCrit, 35),
    };

    private static readonly AttrRange[] BowWeapon =
    {
        Line(AttributeType.AccuracyPercent, RampWide, 30),
        Line(AttributeType.CritRate, RampWide, 30),
        Line(AttributeType.CritDamage, RampCrit, 35),
    };

    private static readonly AttrRange[] RingJewel =
    {
        Line(AttributeType.HpRegenPercent, RampJewel, 5),
        Line(AttributeType.MpRegenPercent, RampJewel, 5),
    };

    private static readonly AttrRange[] EarringJewel =
    {
        Line(AttributeType.HealthPercent, RampJewel, 5),
        Line(AttributeType.ManaPercent, RampJewel, 5),
    };

    private static readonly AttrRange[] NecklaceJewel =
    {
        Line(AttributeType.PhysicalAttackPercent, RampJewel, 5),
        Line(AttributeType.MagicAttackPercent, RampJewel, 5),
    };

    /// <summary>The grade band an item level sits in. Below 40 = no attributes at all.</summary>
    public static AttrTier TierOf(int itemLevel) =>
        itemLevel >= 80 ? AttrTier.S :
        itemLevel >= 76 ? AttrTier.A :
        itemLevel >= 61 ? AttrTier.B :
        itemLevel >= 52 ? AttrTier.C :
        itemLevel >= 40 ? AttrTier.D : AttrTier.None;

    public static string TierName(AttrTier t) => t switch
    {
        AttrTier.D => "D", AttrTier.C => "C", AttrTier.B => "B",
        AttrTier.A => "A", AttrTier.S => "S", _ => "-"
    };

    /// <summary>The attribute lines an item can carry: weapons by family (magic weapons use the
    /// caster pool whatever their WeaponType — a staff is a TwoHandedBlunt), jewels by sub-type.
    /// Armor and everything else returns empty: armor identity is its SET.</summary>
    public static AttrRange[] PoolFor(ItemDef def)
    {
        if (def.NoAttributes) return Array.Empty<AttrRange>();
        if (def.Slot == EquipSlot.Weapon)
        {
            if (def.IsMagicWeapon) return MagicWeapon;
            return def.WeaponType.Base() switch
            {
                WeaponType.Sword => SwordWeapon,
                WeaponType.Blunt => BluntWeapon,
                WeaponType.Dual => DualWeapon,
                WeaponType.Bow => BowWeapon,
                _ => Array.Empty<AttrRange>()
            };
        }
        if (def.Slot == EquipSlot.Jewel)
            return def.JewelType switch
            {
                JewelType.Ring => RingJewel,
                JewelType.Earring => EarringJewel,
                JewelType.Necklace => NecklaceJewel,
                _ => Array.Empty<AttrRange>()
            };
        return Array.Empty<AttrRange>();
    }

    /// <summary>Can this item ever hold an attribute? (A weapon/jewel of D grade or better.)</summary>
    public static bool CanHoldAttribute(ItemDef def) =>
        TierOf(def.ItemLevel) != AttrTier.None && PoolFor(def).Length > 0;

    // ===================================================================================
    //  SCROLLS. Each kind serves ONE grade band and does ONE thing. There is no attribute
    //  LOCK any more and (outside S) no guaranteed-maximum scroll.
    // ===================================================================================

    public static AttrScrollAction ActionOf(AttrScrollKind kind) => kind switch
    {
        AttrScrollKind.Common => AttrScrollAction.RollType,
        AttrScrollKind.Uncommon => AttrScrollAction.RerollValue,
        AttrScrollKind.Rare => AttrScrollAction.RerollValueHigh,
        AttrScrollKind.Epic => AttrScrollAction.RollType,
        AttrScrollKind.Legendary => AttrScrollAction.RerollValueHigh,
        AttrScrollKind.Mythic => AttrScrollAction.RollTypeMax,
        _ => AttrScrollAction.RollType
    };

    /// <summary>Which grade bands a scroll kind may be used on. Common/Uncommon/Rare cover the
    /// D-C-B stretch, Epic/Legendary are the A-grade pair, Mythic is S only.</summary>
    public static bool Accepts(AttrScrollKind kind, AttrTier tier) => kind switch
    {
        AttrScrollKind.Common or AttrScrollKind.Uncommon or AttrScrollKind.Rare
            => tier is AttrTier.D or AttrTier.C or AttrTier.B,
        AttrScrollKind.Epic or AttrScrollKind.Legendary => tier == AttrTier.A,
        AttrScrollKind.Mythic => tier == AttrTier.S,
        _ => false
    };

    /// <summary>Human-readable grade band for a scroll, for tooltips and refusal messages.</summary>
    public static string AcceptedGrades(AttrScrollKind kind) => kind switch
    {
        AttrScrollKind.Common or AttrScrollKind.Uncommon or AttrScrollKind.Rare => "D, C or B",
        AttrScrollKind.Epic or AttrScrollKind.Legendary => "A",
        AttrScrollKind.Mythic => "S",
        _ => "-"
    };

    /// <summary>True if this scroll needs the item to ALREADY have an attribute (it re-rolls a
    /// value, it cannot create one). Those are the second-step scrolls of each band.</summary>
    public static bool NeedsExisting(AttrScrollKind kind) =>
        ActionOf(kind) is AttrScrollAction.RerollValue or AttrScrollAction.RerollValueHigh;

    /// <summary>Apply a scroll to an item. <paramref name="current"/> is the item's existing
    /// attribute (null when bare). Returns the NEW attribute, or Ok=false with the reason.
    /// Every rule the player can hit is checked here so the server handler stays thin.</summary>
    public static AttrScrollResult ApplyScroll(ItemDef def, ItemAttribute? current,
        AttrScrollKind kind, Random rng)
    {
        var pool = PoolFor(def);
        if (pool.Length == 0)
            return new(false, null, $"{def.Name} cannot carry an attribute.");

        var tier = TierOf(def.ItemLevel);
        if (tier == AttrTier.None)
            return new(false, null, $"{def.Name} is below D grade — attributes start at D (level 40).");

        if (!Accepts(kind, tier))
            return new(false, null,
                $"That scroll only works on {AcceptedGrades(kind)} grade; {def.Name} is {TierName(tier)} grade.");

        var action = ActionOf(kind);

        if (NeedsExisting(kind) && current is null)
            return new(false, null,
                $"{def.Name} has no attribute to re-roll — give it one with a "
                + $"{(tier == AttrTier.A ? "Epic" : "Common")} attribute scroll first.");

        // Which line are we rolling? A value re-roll keeps the type it already has.
        AttrRange line;
        if (action is AttrScrollAction.RerollValue or AttrScrollAction.RerollValueHigh)
        {
            var existing = pool.FirstOrDefault(r => r.Type == current!.Type);
            if (existing.Max is null)
                // The item carries a type no longer in its pool (an old save). Treat the scroll
                // as a fresh type roll rather than failing — the player keeps their scroll's worth.
                line = pool[rng.Next(pool.Length)];
            else
                line = existing;
        }
        else
        {
            line = pool[rng.Next(pool.Length)];
        }

        var (min, max) = line.At(tier);
        if (max <= 0) return new(false, null, $"{def.Name} cannot carry an attribute.");

        int value = action switch
        {
            AttrScrollAction.RollTypeMax => max,
            AttrScrollAction.RerollValueHigh => RollIn(min + (max - min) / 2, max, rng),
            _ => RollIn(min, max, rng)
        };

        return new(true, new ItemAttribute(line.Type, value), Describe(def, line.Type, value));
    }

    private static int RollIn(int min, int max, Random rng) =>
        max <= min ? max : rng.Next(min, max + 1);

    private static string Describe(ItemDef def, AttributeType type, int value) =>
        $"{def.Name}: {DisplayName(type)} +{value}{(IsPercent(type) ? "%" : "")}.";

    // ===================================================================================
    //  Rendering + application
    // ===================================================================================

    /// <summary>Almost every rollable attribute is a PERCENT. The three legacy FLAT types are kept
    /// only so items rolled before 0.45.0 still render correctly — and <see cref="AttributeType.Evasion"/>
    /// is FLAT on purpose (2026-08-07): one evasion point already IS one percent of miss chance, so
    /// the roll is authored in points and must not render a second "%" on top.</summary>
    public static bool IsPercent(AttributeType type) => type switch
    {
        AttributeType.Accuracy or AttributeType.HpRegen or AttributeType.MpRegen
            or AttributeType.Evasion => false,
        _ => true
    };

    /// <summary>An item attribute as a <see cref="StatMods"/> bundle — the single mapping the
    /// StatMods-based item/set application reads (percent points → fractions, flats pass through).
    /// AttackPercent hits BOTH channels (god gear); PhysicalAttackPercent and MagicAttackPercent
    /// are the one-channel rolls.</summary>
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
            AttributeType.EvasionPercent     => new StatMods(EvasionPct: f),   // LEGACY, no longer rollable
            AttributeType.Evasion            => new StatMods(Evasion: v),
            AttributeType.DefencePercent     => new StatMods(PDefPct: f),
            AttributeType.Accuracy           => new StatMods(Accuracy: v),
            AttributeType.HpRegen            => new StatMods(HpRegen: v),
            AttributeType.MpRegen            => new StatMods(MpRegen: v),
            AttributeType.CritRate           => new StatMods(CritRate: f),
            AttributeType.CritDamage         => new StatMods(CritDamage: f),
            AttributeType.MagicAttackPercent => new StatMods(MAtkPct: f),
            AttributeType.MagicCritRate      => new StatMods(MagicCritRate: f),
            AttributeType.AccuracyPercent    => new StatMods(AccuracyPct: f),
            AttributeType.HpRegenPercent     => new StatMods(HpRegenPct: f),
            AttributeType.MpRegenPercent     => new StatMods(MpRegenPct: f),
            AttributeType.PhysicalAttackPercent => new StatMods(PAtkPct: f),
            _                                => default,
        };
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
        AttributeType.Evasion => "Evasion",
        AttributeType.DefencePercent => "Defence",
        AttributeType.Accuracy => "Accuracy",
        AttributeType.HpRegen => "HP Regen",
        AttributeType.MpRegen => "MP Regen",
        AttributeType.CritRate => "Crit Rate",
        AttributeType.CritDamage => "Crit Damage",
        AttributeType.MagicAttackPercent => "M.Atk",
        AttributeType.MagicCritRate => "Magic Crit Rate",
        AttributeType.AccuracyPercent => "Accuracy",
        AttributeType.HpRegenPercent => "HP Regen",
        AttributeType.MpRegenPercent => "MP Regen",
        AttributeType.PhysicalAttackPercent => "P.Atk",
        _ => type.ToString()
    };

    /// <summary>The attribute lines an item could hold, as display rows for a tooltip:
    /// "Cast Speed 10~15%". Used by the scroll window so the player can see what a base
    /// is capable of BEFORE spending anything on it.</summary>
    public static IEnumerable<string> PossibleRolls(ItemDef def)
    {
        var tier = TierOf(def.ItemLevel);
        if (tier == AttrTier.None) yield break;
        foreach (var line in PoolFor(def))
        {
            var (min, max) = line.At(tier);
            if (max <= 0) continue;
            string unit = IsPercent(line.Type) ? "%" : "";
            yield return min >= max
                ? $"{DisplayName(line.Type)} {max}{unit}"
                : $"{DisplayName(line.Type)} {min}~{max}{unit}";
        }
    }
}
