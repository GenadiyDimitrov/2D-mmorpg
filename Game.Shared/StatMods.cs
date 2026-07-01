namespace Game.Shared;

// ===========================================================================
//  StatMods — ONE stat-modifier bundle for every buff / debuff / passive /
//  mastery / item source. See docs/StatMods.md for the full design + phases.
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
    float MpRegen = 0f, float MpRegenPct = 0f)
{
    // NOTE: resists, vamp, cooldown, interrupt, the PvE/PvP×skill/magic/basic matrix,
    // shield block/def, bow range, restore-mp bonus and the combat FLOORS are added as the
    // matching sources are migrated (docs/StatMods.md phases 2-5). Kept out of Phase 1 so the
    // shape can be reviewed on the core combat stats first.

    /// <summary>Fold a set of source mods into running totals: percents SUM, flats SUM
    /// (the current-engine convention, kept for migration parity — see docs/StatMods.md;
    /// switching to compound percents is a one-line change here).</summary>
    public static StatTotals Combine(IEnumerable<StatMods> sources)
    {
        var t = new StatTotals();
        foreach (var s in sources) t = t.Add(s);
        return t;
    }
}

/// <summary>Running totals accumulated from many <see cref="StatMods"/> — flats summed,
/// percents summed. Turned into a final stat via <see cref="Apply"/>: `(base + flat) ×
/// (1 + pct)`. A mutable-by-copy value type (each Add returns a new total).</summary>
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
    float MpRegen = 0f, float MpRegenPct = 0f)
{
    public StatTotals Add(in StatMods s) => new(
        MaxHp + s.MaxHp, MaxHpPct + s.MaxHpPct,
        MaxMp + s.MaxMp, MaxMpPct + s.MaxMpPct,
        PDef + s.PDef, PDefPct + s.PDefPct,
        MDef + s.MDef, MDefPct + s.MDefPct,
        PAtk + s.PAtk, PAtkPct + s.PAtkPct,
        MAtk + s.MAtk, MAtkPct + s.MAtkPct,
        Accuracy + s.Accuracy,
        Evasion + s.Evasion, EvasionPct + s.EvasionPct,
        CritRate + s.CritRate, CritRatePct + s.CritRatePct,
        CritDamage + s.CritDamage, CritDamagePct + s.CritDamagePct,
        MagicCritRate + s.MagicCritRate,
        AtkSpeedPct + s.AtkSpeedPct, CastSpeedPct + s.CastSpeedPct,
        MoveSpeed + s.MoveSpeed, MoveSpeedPct + s.MoveSpeedPct,
        HpRegen + s.HpRegen, HpRegenPct + s.HpRegenPct,
        MpRegen + s.MpRegen, MpRegenPct + s.MpRegenPct);

    /// <summary>Apply a (flat, pct) pair to a base value: `(base + flat) × (1 + pct)`,
    /// floored at 0. The single place the combine convention is defined.</summary>
    public static float Apply(float baseValue, float flat, float pct) =>
        Math.Max(0f, (baseValue + flat) * (1f + pct));
}
