namespace Game.Shared;

// ===========================================================================
//  StatMods — ONE stat-modifier bundle for every buff / debuff / passive /
//  mastery / item source. See docs/design/StatMods.md for the full design + phases.
//
//  PHASE 1 (this file): the type + the combiner. NOTHING consumes it yet, so
//  there is no behavior change — the existing PassiveEffect / MasteryEffect /
//  buff-magnitude paths are migrated onto this in later phases.
//
//  Every field is `0 = no change`, so `default(StatMods)` and `new StatMods()`
//  are both inert. Percent fields are FRACTIONS (0.07 = +7%); flat fields add
//  directly. Combining and application live in ONE place (Combine + Apply) so
//  the exact convention is trivially retunable.
// ===========================================================================

/// <summary>A bundle of stat modifiers from one source. Flat + percent per stat, all
/// defaulting to no-op. Sources are combined by <see cref="Combine"/> and turned into
/// final stats by <see cref="StatTotals.Apply"/>.</summary>
public readonly record struct StatMods(
    // Max HP / MP
    float MaxHp = 0f, float MaxHpPct = 0f,
    float MaxMp = 0f, float MaxMpPct = 0f,
    // Physical / magic defence
    float PDef = 0f, float PDefPct = 0f,
    float MDef = 0f, float MDefPct = 0f,
    // Physical / magic attack
    float PAtk = 0f, float PAtkPct = 0f,
    float MAtk = 0f, float MAtkPct = 0f,
    // Accuracy / evasion
    float Accuracy = 0f,
    float Evasion = 0f, float EvasionPct = 0f,
    // Crit (physical rate/damage, magic rate)
    float CritRate = 0f, float CritRatePct = 0f,
    float CritDamage = 0f, float CritDamagePct = 0f,
    float MagicCritRate = 0f,
    // Speeds (attack/cast are percent-only; move has both). Positive = FASTER / more.
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f,
    float MoveSpeed = 0f, float MoveSpeedPct = 0f,
    // Regen (flat per tick + percent multiplier)
    float HpRegen = 0f, float HpRegenPct = 0f,
    float MpRegen = 0f, float MpRegenPct = 0f,
    // Flat additive extras (masteries): interrupt resist, defensive resist fractions,
    // and the nuker "mpWhenRestored" bonus. All summed.
    float InterruptResist = 0f,
    float CritDmgResist = 0f, float CritRateResist = 0f, float BowResist = 0f,
    float CcResist = 0f,   // reduces the LAND chance of contested CC (stun/fear/root/slow/DoT) vs you
    float RestoreMpBonus = 0f,
    // Primary-stat deltas (flat, SUMMED). Applied to the entity's core stats BEFORE the derived
    // stats are computed, so a set's "CON +3" actually raises HP, "DEX +1" raises eva/acc/crit, etc.
    // (item/set sources — the "formula counts for them" per owner).
    float Str = 0f, float Dex = 0f, float Con = 0f, float Int = 0f, float Wit = 0f, float Spt = 0f,
    // Lifesteal + reflect fractions (from gear/sets). Reflect returns a fraction of taken MELEE
    // damage to the attacker (bows excluded, never re-reflects) — live in ApplyDamage; capped at 50%.
    float MeleeVamp = 0f, float SpellVamp = 0f, float Reflect = 0f,
    // Shield defence multiplier (the CSV's "shield.p.def x1.25"). Only bites while a shield is
    // equipped — used by the heavy sets' SHIELD-conditional bonus.
    float ShieldDefPct = 0f)
{
    // NOTE: cooldown, interrupt POWER, the PvE/PvP×skill/magic/basic matrix, shield BLOCK CHANCE, bow
    // range and the combat FLOORS are added as the passive/buff sources migrate (docs/design/StatMods.md).

    /// <summary>Every field multiplied by <paramref name="f"/>. Used to derive a set bonus for a lower
    /// QUALITY of the same gear: the authored numbers are the MYTHIC set, and Epic/Legendary get the
    /// same shape at 70% / 85%. Scaling uniformly is deliberate — choosing which fields shrink would be
    /// a per-set design decision, and this keeps one authored set as the single source of truth.</summary>
    public StatMods Scaled(float f) => new(
        MaxHp * f, MaxHpPct * f, MaxMp * f, MaxMpPct * f,
        PDef * f, PDefPct * f, MDef * f, MDefPct * f,
        PAtk * f, PAtkPct * f, MAtk * f, MAtkPct * f,
        Accuracy * f, Evasion * f, EvasionPct * f,
        CritRate * f, CritRatePct * f, CritDamage * f, CritDamagePct * f, MagicCritRate * f,
        AtkSpeedPct * f, CastSpeedPct * f, MoveSpeed * f, MoveSpeedPct * f,
        HpRegen * f, HpRegenPct * f, MpRegen * f, MpRegenPct * f,
        InterruptResist * f,
        CritDmgResist * f, CritRateResist * f, BowResist * f,
        CcResist * f, RestoreMpBonus * f,
        Str * f, Dex * f, Con * f, Int * f, Wit * f, Spt * f,
        MeleeVamp * f, SpellVamp * f, Reflect * f,
        ShieldDefPct * f);

    /// <summary>Fold a set of source mods into running totals (flats SUM, percents COMPOUND
    /// — see docs/design/StatMods.md: final = (base + Σflat) × ∏(1+pct%)).</summary>
    public static StatTotals Combine(IEnumerable<StatMods> sources)
    {
        var t = new StatTotals();
        foreach (var s in sources) t = t.Add(s);
        return t;
    }
}

/// <summary>Running totals accumulated from many <see cref="StatMods"/> — flats SUMMED,
/// percents COMPOUNDED (each stored pct = ∏(1+p)−1, so 0 stays inert). Turned into a final
/// stat via <see cref="Apply"/>: `(base + flat) × (1 + pct)`. A value type (each Add returns
/// a new total).</summary>
public readonly record struct StatTotals(
    float MaxHp = 0f, float MaxHpPct = 0f,
    float MaxMp = 0f, float MaxMpPct = 0f,
    float PDef = 0f, float PDefPct = 0f,
    float MDef = 0f, float MDefPct = 0f,
    float PAtk = 0f, float PAtkPct = 0f,
    float MAtk = 0f, float MAtkPct = 0f,
    float Accuracy = 0f,
    float Evasion = 0f, float EvasionPct = 0f,
    float CritRate = 0f, float CritRatePct = 0f,
    float CritDamage = 0f, float CritDamagePct = 0f,
    float MagicCritRate = 0f,
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f,
    float MoveSpeed = 0f, float MoveSpeedPct = 0f,
    float HpRegen = 0f, float HpRegenPct = 0f,
    float MpRegen = 0f, float MpRegenPct = 0f,
    float InterruptResist = 0f,
    float CritDmgResist = 0f, float CritRateResist = 0f, float BowResist = 0f,
    float CcResist = 0f,
    float RestoreMpBonus = 0f,
    float Str = 0f, float Dex = 0f, float Con = 0f, float Int = 0f, float Wit = 0f, float Spt = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float Reflect = 0f,
    float ShieldDefPct = 0f)
{
    /// <summary>Compound two percents: ∏(1+p)−1, so combining is multiplicative and 0 = inert.</summary>
    private static float Mul(float a, float b) => (1f + a) * (1f + b) - 1f;

    public StatTotals Add(in StatMods s) => new(
        MaxHp + s.MaxHp, Mul(MaxHpPct, s.MaxHpPct),
        MaxMp + s.MaxMp, Mul(MaxMpPct, s.MaxMpPct),
        PDef + s.PDef, Mul(PDefPct, s.PDefPct),
        MDef + s.MDef, Mul(MDefPct, s.MDefPct),
        PAtk + s.PAtk, Mul(PAtkPct, s.PAtkPct),
        MAtk + s.MAtk, Mul(MAtkPct, s.MAtkPct),
        Accuracy + s.Accuracy,
        Evasion + s.Evasion, Mul(EvasionPct, s.EvasionPct),
        CritRate + s.CritRate, Mul(CritRatePct, s.CritRatePct),
        CritDamage + s.CritDamage, Mul(CritDamagePct, s.CritDamagePct),
        MagicCritRate + s.MagicCritRate,
        Mul(AtkSpeedPct, s.AtkSpeedPct), Mul(CastSpeedPct, s.CastSpeedPct),
        MoveSpeed + s.MoveSpeed, Mul(MoveSpeedPct, s.MoveSpeedPct),
        HpRegen + s.HpRegen, Mul(HpRegenPct, s.HpRegenPct),
        MpRegen + s.MpRegen, Mul(MpRegenPct, s.MpRegenPct),
        InterruptResist + s.InterruptResist,
        CritDmgResist + s.CritDmgResist, CritRateResist + s.CritRateResist, BowResist + s.BowResist,
        CcResist + s.CcResist,
        RestoreMpBonus + s.RestoreMpBonus,
        Str + s.Str, Dex + s.Dex, Con + s.Con, Int + s.Int, Wit + s.Wit, Spt + s.Spt,
        MeleeVamp + s.MeleeVamp, SpellVamp + s.SpellVamp, Reflect + s.Reflect,
        Mul(ShieldDefPct, s.ShieldDefPct));

    /// <summary>Apply a (flat, pct) pair to a base value: `(base + flat) × (1 + pct)`,
    /// floored at 0. The single place the combine convention is defined.</summary>
    public static float Apply(float baseValue, float flat, float pct) =>
        Math.Max(0f, (baseValue + flat) * (1f + pct));
}
