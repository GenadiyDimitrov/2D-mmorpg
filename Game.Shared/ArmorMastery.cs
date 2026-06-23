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
    float MaxHp = 1f,        // >1 more max HP
    float MaxMp = 1f,        // >1 more max MP
    int Evasion = 0,
    int Accuracy = 0,
    int Defence = 0,
    int MagicDefence = 0,
    int InterruptResist = 0,
    float CritRate = 0f,     // flat crit-rate points (0..1)
    float CritDamage = 0f);  // flat crit-multiplier bonus

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
    private static readonly MasteryEffect Neutral = new(
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

    /// <summary>The trained bonus for a class wearing an APPROPRIATE weight, or null
    /// if that weight isn't one this class trains. (Magnitudes unchanged from before;
    /// learning now gates whether they apply.)</summary>
    private static MasteryEffect? MatchedEffect(BaseClass cls, Archetype? arch, ArmorWeight worn, int level)
    {
        int defL = level;
        switch (arch)
        {
            case Archetype.Tank:
                return worn == ArmorWeight.Heavy
                    ? new MasteryEffect(MaxHp: 1.2f, HpRegen: 1.3f, Defence: defL + 20, MagicDefence: 20 + level / 2)
                    : null;
            case Archetype.Warrior:
                return worn switch
                {
                    ArmorWeight.Heavy => new MasteryEffect(MaxHp: 1.1f, HpRegen: 1.2f, Defence: defL),
                    ArmorWeight.Light => new MasteryEffect(MaxHp: 1.05f, AtkSpeed: 1.15f, Evasion: 3, Accuracy: 3, Defence: defL / 2),
                    _ => null
                };
            case Archetype.Healer:
                // Robe = caster lean; Light = solo-farm/melee lean. Both trainable.
                return worn switch
                {
                    ArmorWeight.Robe => new MasteryEffect(CastSpeed: 1.3f, MpRegen: 1.3f, MaxMp: 1.15f, Defence: defL / 2),
                    ArmorWeight.Light => new MasteryEffect(AtkSpeed: 1.3f, MoveSpeed: 1.05f, HpRegen: 1.1f, Evasion: 4, Accuracy: 4, Defence: defL),
                    _ => null
                };
            case Archetype.Nuker:
                return worn == ArmorWeight.Robe
                    ? new MasteryEffect(CastSpeed: 1.4f, MpRegen: 1.3f, MaxMp: 1.15f, InterruptResist: level, MagicDefence: 10 + level / 2, Defence: defL / 2)
                    : null;
            case Archetype.Rogue:
                return worn == ArmorWeight.Light
                    ? new MasteryEffect(AtkSpeed: 1.35f, MoveSpeed: 1.1f, Evasion: 5, Accuracy: 5, Defence: defL / 2)
                    : null;
            case Archetype.Archer:
                return worn == ArmorWeight.Light
                    ? new MasteryEffect(AtkSpeed: 1.3f, CritRate: 0.05f, CritDamage: 0.2f, Evasion: 4, Accuracy: 4, Defence: defL / 2)
                    : null;
            default:
                // Base class (no archetype yet): Mage trains robe, Fighter trains light.
                if (cls == BaseClass.Mage)
                    return worn == ArmorWeight.Robe
                        ? new MasteryEffect(CastSpeed: 1.3f, MpRegen: 1.2f, MaxMp: 1.1f, Defence: defL / 2)
                        : null;
                return worn == ArmorWeight.Light
                    ? new MasteryEffect(AtkSpeed: 1.3f, HpRegen: 1.2f, Evasion: 3, Accuracy: 3, Defence: defL / 2)
                    : null;
        }
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
