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

    /// <summary>Resolve the mastery effect + a UI label for a player of
    /// (cls, arch) wearing BODY armor of <paramref name="worn"/> weight.</summary>
    public static MasteryResult Resolve(BaseClass cls, Archetype? arch, ArmorWeight worn, int level)
    {
        if (worn == ArmorWeight.None)
            return new MasteryResult(Neutral, "");

        int defL = level;             // matched DEF-per-level stand-in
        string W(ArmorWeight w) => w.ToString();

        // ----- Tank: trained in HEAVY, never penalised in anything else. -----
        if (arch == Archetype.Tank)
            return worn == ArmorWeight.Heavy
                ? new MasteryResult(new MasteryEffect(MaxHp: 1.2f, HpRegen: 1.3f,
                        Defence: defL + 20, MagicDefence: 20 + level / 2), "Heavy Mastery")
                : new MasteryResult(Neutral, $"{W(worn)} (no penalty)");

        // ----- Warrior: trained in BOTH heavy and light, no penalties. -----
        if (arch == Archetype.Warrior)
            return worn switch
            {
                ArmorWeight.Heavy => new MasteryResult(new MasteryEffect(MaxHp: 1.1f, HpRegen: 1.2f,
                    Defence: defL), "Heavy Mastery"),
                ArmorWeight.Light => new MasteryResult(new MasteryEffect(MaxHp: 1.05f, AtkSpeed: 1.15f,
                    Evasion: 3, Accuracy: 3, Defence: defL / 2), "Light Mastery"),
                _ => new MasteryResult(Neutral, "Robe (no penalty)")
            };

        // ----- Healer: trained in BOTH light and robe (no penalty in heavy either).
        //  Robe = the pure-caster lean (cast speed, MP, regen); Light = the solo-farm
        //  lean (attack speed, accuracy, a little survivability) so a healer can melee
        //  trash with a weapon. The player picks their playstyle via armor weight. -----
        if (arch == Archetype.Healer)
            return worn switch
            {
                ArmorWeight.Robe => new MasteryResult(new MasteryEffect(CastSpeed: 1.3f,
                    MpRegen: 1.3f, MaxMp: 1.15f, Defence: defL / 2), "Robe Mastery (caster)"),
                ArmorWeight.Light => new MasteryResult(new MasteryEffect(AtkSpeed: 1.3f,
                    MoveSpeed: 1.05f, HpRegen: 1.1f, Evasion: 4, Accuracy: 4, Defence: defL), "Light Mastery (melee)"),
                _ => new MasteryResult(Neutral, "Heavy (no penalty)")
            };

        // ----- Determine trained weight + matched bonus for the rest. -----
        ArmorWeight trained;
        MasteryEffect matched;
        if (cls == BaseClass.Mage)
        {
            if (arch == Archetype.Nuker)
            { trained = ArmorWeight.Robe; matched = new MasteryEffect(CastSpeed: 1.4f, MpRegen: 1.3f,
                MaxMp: 1.15f, InterruptResist: level, MagicDefence: 10 + level / 2, Defence: defL / 2); }
            else
            { trained = ArmorWeight.Robe; matched = new MasteryEffect(CastSpeed: 1.3f, MpRegen: 1.2f,
                MaxMp: 1.1f, Defence: defL / 2); }     // base Mage
        }
        else // Fighter line
        {
            if (arch == Archetype.Rogue)
            { trained = ArmorWeight.Light; matched = new MasteryEffect(AtkSpeed: 1.35f, MoveSpeed: 1.1f,
                Evasion: 5, Accuracy: 5, Defence: defL / 2); }
            else if (arch == Archetype.Archer)
            { trained = ArmorWeight.Light; matched = new MasteryEffect(AtkSpeed: 1.3f, CritRate: 0.05f,
                CritDamage: 0.2f, Evasion: 4, Accuracy: 4, Defence: defL / 2); }
            else
            { trained = ArmorWeight.Light; matched = new MasteryEffect(AtkSpeed: 1.3f, HpRegen: 1.2f,
                Evasion: 3, Accuracy: 3, Defence: defL / 2); }   // base Fighter
        }

        if (worn == trained)
            return new MasteryResult(matched, $"{W(trained)} Mastery");

        // ----- Untrained weight: penalties (robe never penalises). -----
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
