namespace Game.Shared;

/// <summary>
/// A multiplier/flat modifier layer applied by an armor-weight MASTERY. Speed and
/// regen/max fields are FACTORS where 1.0 = no change, &gt;1 = better (faster / more),
/// &lt;1 = worse; flat fields are additive. Combined into derived stats in
/// Entity.RecomputeDerived after gear, class and set bonuses.
/// </summary>
public readonly record struct MasteryEffect(
    float AtkSpeed = 1f,     // >1 faster basic attacks
    float CastSpeed = 1f,    // >1 faster casts
    float MoveSpeed = 1f,    // >1 faster movement
    float HpRegen = 1f,      // >1 more HP regen
    float MpRegen = 1f,      // >1 more MP regen
    float MaxHp = 1f,        // >1 more max HP (FACTOR)
    float MaxMp = 1f,        // >1 more max MP (FACTOR)
    int MaxHpFlat = 0,       // flat max HP added before the factor
    int MaxMpFlat = 0,       // flat max MP added before the factor
    int Evasion = 0,
    int Accuracy = 0,
    int Defence = 0,
    int MagicDefence = 0,
    int InterruptResist = 0,
    // Per-CHARACTER-LEVEL coefficients (the old hardcoded "defL"/"level/2" terms, now
    // data): each adds (int)(level * coefficient) on top of the flat value above.
    float DefPerLevel = 0f,
    float MagicDefPerLevel = 0f,
    float InterruptResistPerLevel = 0f,
    float CritRate = 0f,     // flat crit-rate points (0..1)
    float CritDamage = 0f,   // flat crit-multiplier bonus
    // Multiplier FACTORS on the defence pools (1.0 = no change; tank heavy p.def ×1.07).
    float DefenceMult = 1f,
    float MagicDefenceMult = 1f,
    // Defensive resist fractions (heavy = crit-dmg reduction / bow resist; rogue light =
    // crit-rate resist). Added to the entity's running resist totals.
    float CritDmgResist = 0f,
    float CritRateResist = 0f,
    float BowResist = 0f,
    // Bonus MP added whenever an MP-restore effect (Restore Spirit/Mana) lands on the wearer
    // (nuker robe mastery "mpWhenRestored +N").
    int RestoreMpBonus = 0);

/// <summary>One armor-weight mastery resolved for a specific worn weight.</summary>
public readonly record struct MasteryResult(MasteryEffect Effect, string Label);

/// <summary>
/// Armor-weight masteries. A class is TRAINED in a weight: wearing it grants a bonus
/// (the class's identity), wearing an UNtrained heavy/light set applies a penalty
/// (it's heavy / the straps hinder you); robe never penalizes. Tank/Warrior never
/// take penalties. Driven by base class + archetype for now (the base→class-change
/// EVOLUTION is encoded here); the learnable/leveled skill layer + SP cost come later,
/// same path as the other passives. "Def per level" is approximated off character level
/// until that layer exists. All numbers are placeholders — tune freely.
/// </summary>
public static class ArmorMastery
{
    // NOTE: `new()` on a record STRUCT runs the implicit parameterless ctor and
    // ZEROES every field — it does NOT apply the `= 1f` primary-ctor defaults. A
    // zeroed effect multiplies MaxHp/MoveSpeed/regen to 0 and divides cast speed by
    // 0, which is exactly the "0 HP/MP, 0 move, 150% slower cast" bug for an
    // unarmored character. Construct the 1.0 factors explicitly.
    public static readonly MasteryEffect Neutral = new(
        AtkSpeed: 1f, CastSpeed: 1f, MoveSpeed: 1f,
        HpRegen: 1f, MpRegen: 1f, MaxHp: 1f, MaxMp: 1f);

    /// <summary>The passive skill id that trains a given armor weight ("" for None).</summary>
    public static string SkillIdFor(ArmorWeight w) => w switch
    {
        ArmorWeight.Heavy => SkillCatalog.MasteryHeavy,
        ArmorWeight.Light => SkillCatalog.MasteryLight,
        ArmorWeight.Robe  => SkillCatalog.MasteryRobe,
        _ => ""
    };

    /// <summary>Resolve the mastery effect + a UI label for a player of (cls, arch)
    /// wearing BODY armor of <paramref name="worn"/> weight. The bonus for an
    /// APPROPRIATE weight applies only when its passive has been learned
    /// (<paramref name="hasMastery"/>); appropriate-but-unlearned is neutral (no
    /// penalty); an INAPPROPRIATE weight always penalises (robe never does, and
    /// tank/warrior/healer are immune).</summary>
    public static MasteryResult Resolve(BaseClass cls, Archetype? arch, ArmorWeight worn,
        int level, Func<ArmorWeight, bool> hasMastery)
    {
        if (worn == ArmorWeight.None)
            return new MasteryResult(Neutral, "");

        string W(ArmorWeight w) => w.ToString();

        // Appropriate weight for this class: the bonus is GATED behind learning the
        // mastery passive. Unlearned = neutral (no penalty) — learn it to unlock.
        if (MatchedEffect(cls, arch, worn, level) is MasteryEffect matched)
            return hasMastery(worn)
                ? new MasteryResult(matched, $"{W(worn)} Mastery")
                : new MasteryResult(Neutral, $"{W(worn)} (learn {W(worn)} Mastery)");

        // Inappropriate weight: penalty (with the usual exemptions).
        return Penalty(cls, arch, worn);
    }

    /// <summary>The trained bonus for a class wearing an APPROPRIATE weight, or null if
    /// that weight isn't one this class trains. 2nd-class archetypes now carry their
    /// mastery numbers in DATA (Skills.Masteries.cs / Skills.Healer.cs) and take the
    /// data-driven path in Entity.RecomputeDerived; only the BASE-class default remains
    /// here as the pre-class-change fallback (Mage trains robe, Fighter trains light).</summary>
    private static MasteryEffect? MatchedEffect(BaseClass cls, Archetype? arch, ArmorWeight worn, int level)
    {
        int defL = level;
        if (cls == BaseClass.Mage)
            return worn == ArmorWeight.Robe
                ? new MasteryEffect(CastSpeed: 1.3f, MpRegen: 1.2f, MaxMp: 1.1f, Defence: defL / 2)
                : null;
        return worn == ArmorWeight.Light
            ? new MasteryEffect(AtkSpeed: 1.3f, HpRegen: 1.2f, Evasion: 3, Accuracy: 3, Defence: defL / 2)
            : null;
    }

    /// <summary>Penalty for wearing a weight this class does NOT train. Robe never
    /// penalises; tank/warrior/healer take no penalty for an off weight.</summary>
    private static MasteryResult Penalty(BaseClass cls, Archetype? arch, ArmorWeight worn)
    {
        string W(ArmorWeight w) => w.ToString();
        if (arch is Archetype.Tank or Archetype.Warrior or Archetype.Healer)
            return new MasteryResult(Neutral, $"{W(worn)} (no penalty)");

        bool mageLike = cls == BaseClass.Mage;
        return worn switch
        {
            ArmorWeight.Heavy => new MasteryResult(mageLike
                ? new MasteryEffect(AtkSpeed: 0.5f, CastSpeed: 0.5f, MoveSpeed: 0.5f,
                    HpRegen: 0.5f, MpRegen: 0.5f, Evasion: -10, Accuracy: -10)
                : new MasteryEffect(AtkSpeed: 0.8f, CastSpeed: 0.8f, MoveSpeed: 0.8f,
                    Evasion: -3, Accuracy: -3), "Heavy — untrained"),
            ArmorWeight.Light => new MasteryResult(
                new MasteryEffect(AtkSpeed: 0.8f, CastSpeed: 0.8f, MoveSpeed: 0.8f,
                    Evasion: -3, Accuracy: -3), "Light — untrained"),
            _ => new MasteryResult(Neutral, "Robe (no penalty)")   // robe
        };
    }
}
