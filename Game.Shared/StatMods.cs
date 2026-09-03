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
    // Accuracy / evasion. Both have a flat and a percent form; the percent multiplies the
    // finished stat (AGI + level + flats), which is what a bow's "Accuracy +30%" rolls.
    float Accuracy = 0f, float AccuracyPct = 0f,
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
    float InterruptResist = 0f,   // interrupt resistance as a FRACTION (0.10 = 10%) — see StatCalculator.InterruptChance
    float CritDmgResist = 0f, float CritRateResist = 0f, float BowResist = 0f,
    float CcResist = 0f,   // reduces the LAND chance of contested CC (stun/fear/root/slow/DoT) vs you
    // MP RESTORED, as a FRACTION (0.60 = "+60% MP from any restore that lands on you") — the MP twin
    // of the heal channel's HealReceivedPct, and the nuker robe mastery's whole payload.
    // ⚠ It was a FLAT "+N MP per restore" until 2026-08-19. Flat could not survive periodic restores:
    // a mana totem pulsing 30 times paid the flat 30 times, so a +80 rung was worth 2400 MP off a
    // 300 MP totem. A percent scales with whatever lands on you, cast or tick, which is exactly how
    // healing already behaves (owner: *"so it works like the heal hot … a 100 heal/s with 30%
    // increase will heal 130/s"*). His conversion anchor: the old +80 on Restore Spirit's 120-130
    // ≈ 200-210, and ×1.60 lands on the same number — so the ladder is the old flat × 0.75.
    float RestoreMpPct = 0f,
    // Primary-stat deltas (flat, SUMMED). Applied to the entity's core stats BEFORE the derived
    // stats are computed, so a set's "CON +3" actually raises HP, "AGI +1" raises eva/acc/crit, etc.
    // (item/set sources — the "formula counts for them" per owner).
    float Str = 0f, float Agi = 0f, float Con = 0f, float Int = 0f, float Wit = 0f, float Spt = 0f,
    // Lifesteal + reflect fractions (from gear/sets). Reflect returns a fraction of taken MELEE
    // damage to the attacker (bows excluded, never re-reflects) — live in ApplyDamage; capped at 50%.
    float MeleeVamp = 0f, float SpellVamp = 0f, float Reflect = 0f,
    // Shield defence multiplier (the CSV's "shield.p.def x1.25"). Only bites while a shield is
    // equipped — used by the heavy sets' SHIELD-conditional bonus.
    float ShieldDefPct = 0f,
    // ===== The S-grade set bonuses (gear_sets.csv, him 2026-08-11) needed four more channels. =======
    // ⚠ APPENDED AT THE END ON PURPOSE. Scaled() and StatTotals.Add() build a new value POSITIONALLY,
    // so inserting a field in the middle of this list silently misaligns every field after it — the
    // compiler cannot catch it, they are all floats. New fields go here.
    //
    // FLAT crit rate on his 0-1000 scale, as the CSV writes it ("CritRate +100" = +10 points = +0.10
    // chance). It is deliberately NOT `CritRate` above, which is a MULTIPLIER (×1.2): gear crit-rate is
    // flat and lands OUTSIDE every multiplier, which is the whole point of the crit model — multipliers
    // only reward whoever already has a big base, so the flat term is what carries a blunt warrior.
    float CritRateFlat = 0f,
    // FLAT crit damage — the class CSVs' "crit dmg +80" / this set's "critdmg +200". Joins ATTACK inside
    // the damage ratio on a crit only; does nothing off a crit. NOT the `CritDamage` field above, which
    // is a multiplier bonus on top of the ×2 base.
    float CritDamageFlat = 0f,
    // MAGIC damage reduction, authored as the CSVs write it: "mReduction x1.02" = 0.02f. Lands as a
    // DIVISOR inside M.Def (1 + total), so 0.02 is literally ×1/1.02 magic damage taken — his notation
    // and the mechanic are the same number. Not a fizzle chance (ruling `57d`, 2026-08-10).
    float MagicResist = 0f,
    // PvP damage RECEIVED, as a delta: −0.05 = "PVP Dmg Received x0.95". Only bites player-vs-player.
    float PvpDamageTakenPct = 0f,
    // ATK — the ONE power stat (STR for a fighter, INT for a mage; see StatCalculator.GetBaseStats).
    // Added 2026-08-19 with the contested-debuff rework, where his rule is that gear counts:
    // *"an armor con/atk/spt should count and statSwap as well"*. CON and SPT already folded in
    // through the primary-stat block above; ATK had no field at all to fold, so an armour set could
    // not raise it even in principle.
    //
    // `Str` and `Int` above fold into the SAME place (Entity.RecomputeDerived's primary-stat pre-pass)
    // — they are ATK under the two names the gear CSVs write it with, fighter and mage. This field is
    // for authoring ATK directly. The three are summed; a set never carries more than one of them.
    float Atk = 0f,
    // MAGIC crit DAMAGE — a fraction added to the caster's ×2 base (0.30 = +30% → ×2.6). The magic
    // twin of `CritDamage` above, which is PHYSICAL-only and must stay that way (owner 2026-08-06:
    // a fighter's crit-damage gear must not pay a mage). Added 2026-08-19 with the magic-crit rework
    // so a robe set or a spellcaster mastery CAN carry it; nothing authors it yet.
    float MagicCritDamage = 0f,
    // MP-COST REDUCTION as a fraction (0.05 = "Decrease Mp Consumption with 5%"). ONE number for BOTH
    // channels, unlike the buff side's PhysMpCostPct/MagicMpCostPct pair: the things that author this
    // are armour masteries and armour sets, and neither has ever wanted to make a physical skill cheaper
    // without also making a spell cheaper. The 78+ rungs of Healer Armor Mastery are the first user.
    // ⚠ APPENDED, per the note above — Scaled() and StatTotals.Add() build positionally.
    float MpCostPct = 0f)
{
    // NOTE: cooldown, interrupt POWER, the PvE/PvP×skill/magic/basic matrix, shield BLOCK CHANCE, bow
    // range and the combat FLOORS are added as the passive/buff sources migrate (docs/design/StatMods.md).

    /// <summary>Every field multiplied by <paramref name="f"/>. Used to derive a set bonus for a lower
    /// QUALITY of the same gear: the authored numbers are the MYTHIC set, and Epic/Legendary get the
    /// same shape at 70% / 85%. Scaling uniformly is deliberate — choosing which fields shrink would be
    /// a per-set design decision, and this keeps one authored set as the single source of truth.
    ///
    /// ⚠ A COUNTABLE FLAT COMES BACK AS A WHOLE NUMBER (owner, 2026-09-03: *"when lowering stats of
    /// rarity make them integers ... 1.4 is 1 ... The other % based stat can be left as is"*). He was
    /// reading the EPIC copy of the heavy A set, whose authored `Str: 2` printed as **ATK +1.4** — a
    /// primary stat is a COUNT of points and there is no such thing as four tenths of one. So every
    /// field below that is a countable magnitude (pools, defences, attack, accuracy/evasion, move
    /// speed, the six primary stats, ATK, flat crit damage) is rounded HERE, at the one place a
    /// derived quality is built. <see cref="ClassFlatBonus.Scaled"/> has rounded its own int fields
    /// all along; this makes the two halves of one set bonus agree.
    ///
    /// 🔑 EVERYTHING FRACTIONAL BY NATURE IS LEFT ALONE, and that is not a detail: those fields carry
    /// their percent as a FRACTION (0.02 = 2%), so rounding one would not shave it — it would DELETE
    /// it. That is every `*Pct`, the multiplier-shaped `CritRate` / `CritDamage` / `MagicCritRate`,
    /// the resist fractions, vamp/reflect, `MagicResist`, `MagicCritDamage`, `MpCostPct`, and
    /// `CritRateFlat` (authored as 0.10 despite its name). Regen is a per-tick RATE and legitimately
    /// fractional, so it stays too. ⚠ A field appended to this record must be sorted into one half or
    /// the other — the default (no rounding) is the safe one.</summary>
    public StatMods Scaled(float f)
    {
        float R(float v) => MathF.Round(v * f);   // countable flats only — see the note above
        return new(
            R(MaxHp), MaxHpPct * f, R(MaxMp), MaxMpPct * f,
            R(PDef), PDefPct * f, R(MDef), MDefPct * f,
            R(PAtk), PAtkPct * f, R(MAtk), MAtkPct * f,
            R(Accuracy), AccuracyPct * f, R(Evasion), EvasionPct * f,
            CritRate * f, CritRatePct * f, CritDamage * f, CritDamagePct * f, MagicCritRate * f,
            AtkSpeedPct * f, CastSpeedPct * f, R(MoveSpeed), MoveSpeedPct * f,
            HpRegen * f, HpRegenPct * f, MpRegen * f, MpRegenPct * f,
            InterruptResist * f,
            CritDmgResist * f, CritRateResist * f, BowResist * f,
            CcResist * f, RestoreMpPct * f,
            R(Str), R(Agi), R(Con), R(Int), R(Wit), R(Spt),
            MeleeVamp * f, SpellVamp * f, Reflect * f,
            ShieldDefPct * f,
            CritRateFlat * f, R(CritDamageFlat), MagicResist * f, PvpDamageTakenPct * f,
            R(Atk), MagicCritDamage * f, MpCostPct * f);
    }

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
    float Accuracy = 0f, float AccuracyPct = 0f,
    float Evasion = 0f, float EvasionPct = 0f,
    float CritRate = 0f, float CritRatePct = 0f,
    float CritDamage = 0f, float CritDamagePct = 0f,
    float MagicCritRate = 0f,
    float AtkSpeedPct = 0f, float CastSpeedPct = 0f,
    float MoveSpeed = 0f, float MoveSpeedPct = 0f,
    float HpRegen = 0f, float HpRegenPct = 0f,
    float MpRegen = 0f, float MpRegenPct = 0f,
    float InterruptResist = 0f,   // interrupt resistance as a FRACTION (0.10 = 10%) — see StatCalculator.InterruptChance
    float CritDmgResist = 0f, float CritRateResist = 0f, float BowResist = 0f,
    float CcResist = 0f,
    float RestoreMpPct = 0f,
    float Str = 0f, float Agi = 0f, float Con = 0f, float Int = 0f, float Wit = 0f, float Spt = 0f,
    float MeleeVamp = 0f, float SpellVamp = 0f, float Reflect = 0f,
    float ShieldDefPct = 0f,
    float CritRateFlat = 0f, float CritDamageFlat = 0f, float MagicResist = 0f,
    float PvpDamageTakenPct = 0f,
    float Atk = 0f,
    float MagicCritDamage = 0f,
    float MpCostPct = 0f)
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
        Accuracy + s.Accuracy, Mul(AccuracyPct, s.AccuracyPct),
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
        RestoreMpPct + s.RestoreMpPct,
        Str + s.Str, Agi + s.Agi, Con + s.Con, Int + s.Int, Wit + s.Wit, Spt + s.Spt,
        MeleeVamp + s.MeleeVamp, SpellVamp + s.SpellVamp, Reflect + s.Reflect,
        Mul(ShieldDefPct, s.ShieldDefPct),
        // All four SUM here (this is the mastery/Combine path, which is additive by design). NOTE the
        // armor-SET path in Entity.RecomputeDerived compounds PvpDamageTakenPct instead, because the
        // heavy-S set carries the clause twice — once on the set, once on its shield extra — and the CSV
        // means ×0.95 × ×0.95, not −10%.
        CritRateFlat + s.CritRateFlat, CritDamageFlat + s.CritDamageFlat,
        MagicResist + s.MagicResist, PvpDamageTakenPct + s.PvpDamageTakenPct,
        // MagicCritDamage SUMS here like every other flat in this path; Entity.RecomputeDerived
        // turns the total into the ONE multiplier (1 + total) it feeds MagicCritMult.
        Atk + s.Atk, MagicCritDamage + s.MagicCritDamage, MpCostPct + s.MpCostPct);

    /// <summary>Apply a (flat, pct) pair to a base value: `(base + flat) × (1 + pct)`,
    /// floored at 0. The single place the combine convention is defined.</summary>
    public static float Apply(float baseValue, float flat, float pct) =>
        Math.Max(0f, (baseValue + flat) * (1f + pct));
}
