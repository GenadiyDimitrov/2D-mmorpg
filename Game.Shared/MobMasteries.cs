namespace Game.Shared;

/// <summary>
/// The leveled MOB MASTERY tables from <c>docs/data/mobs/mobs_passives.csv</c> — the "mob passives like
/// classes" layer. Each mastery is a table indexed by a LEVEL (1-based); the value is a MULTIPLIER
/// (1 = neutral) except Armor Weight's evasion (a FLAT add) — so a mob is tuned by picking a level
/// per mastery. <see cref="Build"/> resolves a set of picks into a <see cref="MobMod"/> (the runtime
/// carrier applied at spawn). Level 0 on any pick = "not used" (neutral). Status resists
/// (stun/fear/…) are deferred to the CC layer, not here.
/// </summary>
public static class MobMasteries
{
    // "Increase P.Atk for the cost of Atk.Spd" — (pAtkMult, atkSpdMult>1 = faster). Neutral = L5.
    private static readonly (float PAtk, float AtkSpd)[] WeaponWeightTable =
    {
        (0.8f, 1.2f), (0.85f, 1.15f), (0.9f, 1.1f), (0.95f, 1.05f), (1f, 1f),
        (1.05f, 0.95f), (1.1f, 0.9f), (1.15f, 0.85f), (1.2f, 0.82f), (1.25f, 0.8f),
        (1.3f, 0.77f), (1.4f, 0.75f), (1.5f, 0.73f), (1.8f, 0.7f), (2f, 0.65f),
        (2.2f, 0.6f), (2.5f, 0.5f),
    };

    // "Increase P.Def for the cost of Evasion" — (pDefMult, evaFlat). Neutral = L2. L1 light, L3 heavy.
    private static readonly (float PDef, int EvaFlat)[] ArmorWeightTable =
    {
        (0.85f, 10), (1f, 0), (1.15f, -10),
    };

    // Single-stat multiplier tracks. 0 = "deals nothing / 1hp"; big tail = near-infinite.
    private static readonly float[] MAtkTable =
        { 0f, 0.15f, 0.18f, 0.22f, 0.27f, 0.32f, 0.39f, 0.47f, 0.57f, 0.69f, 0.83f, 1f, 1.21f, 1.46f, 1.77f, 2.14f, 2.59f, 3.13f, 3.79f, 4.59f, 5.55f, 6.72f, 100f };
    private static readonly float[] PAtkTable =
        { 0f, 0.39f, 0.43f, 0.47f, 0.52f, 0.57f, 0.63f, 0.69f, 0.76f, 0.83f, 0.91f, 1f, 1.1f, 1.21f, 1.33f, 1.46f, 1.61f, 1.77f, 1.94f, 2.14f, 2.35f, 2.59f, 10f };
    // Max HP / Max MP / Regen share the same table (neutral = L2, value 1). Note the deliberate
    // non-monotonic dip at L8/9 (0.25/0.5) — tuning slots, not an ordering.
    private static readonly float[] PoolTable =
        { 0f, 1f, 1.1f, 1.21f, 1.33f, 1.46f, 1.61f, 1.77f, 0.25f, 0.5f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 999999f };
    // P.Def / M.Def (neutral = L12, value 1).
    private static readonly float[] DefTable =
        { 0f, 0.39f, 0.43f, 0.47f, 0.52f, 0.57f, 0.63f, 0.69f, 0.76f, 0.83f, 0.91f, 1f, 1.1f, 1.21f, 1.33f, 1.46f, 1.61f, 1.77f, 1.94f, 2.14f, 2.35f, 2.59f, 10f, 999999f };
    // Weapon-type resistance (P.Def coefficient vs Sword-Dual / Blunt / Bow). Neutral = L7, value 1.
    private static readonly float[] ResistTable =
        { 0f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.11f, 1.25f, 1.43f, 1.67f, 2f, 999999f };

    // Neutral (×1) level of each track — the "average" default every mob sits at.
    public const int WeaponWeightNeutral = 5, ArmorWeightNeutral = 2;
    public const int AtkModNeutral = 12, PoolNeutral = 2, DefModNeutral = 12, ResistNeutral = 7;

    private static float At(float[] table, int level) =>
        level >= 1 && level <= table.Length ? table[level - 1] : 1f;

    /// <summary>Resolve a set of mastery-level PICKS into a MobMod (level 0 = skip/neutral).
    /// Stats hit by more than one track MULTIPLY (e.g. Weapon Weight P.Atk × P.Atk Mod).</summary>
    public static MobMod Build(
        int weaponWeight = 0, int armorWeight = 0,
        int mAtk = 0, int pAtk = 0, int maxHp = 0, int maxMp = 0,
        int hpRegen = 0, int mpRegen = 0, int mDef = 0, int pDef = 0,
        int pierce = 0, int blunt = 0, int bow = 0,
        bool boss = false, string name = "")
    {
        float pAtkMul = 1f, pDefMul = 1f, atkSpd = 1f;
        int evaFlat = 0;

        if (weaponWeight > 0)
        {
            var (wp, ws) = WeaponWeightTable[Clamp(weaponWeight, WeaponWeightTable.Length) - 1];
            pAtkMul *= wp; atkSpd *= ws;
        }
        if (armorWeight > 0)
        {
            var (ap, ae) = ArmorWeightTable[Clamp(armorWeight, ArmorWeightTable.Length) - 1];
            pDefMul *= ap; evaFlat += ae;
        }
        if (pAtk > 0) pAtkMul *= At(PAtkTable, pAtk);
        if (pDef > 0) pDefMul *= At(DefTable, pDef);

        return new MobMod(
            Hp: maxHp > 0 ? At(PoolTable, maxHp) : 1f,
            PDef: pDefMul,
            MDef: mDef > 0 ? At(DefTable, mDef) : 1f,
            PAtk: pAtkMul,
            MAtk: mAtk > 0 ? At(MAtkTable, mAtk) : 1f,
            MaxMp: maxMp > 0 ? At(PoolTable, maxMp) : 1f,
            AtkSpeed: atkSpd,
            HpRegen: hpRegen > 0 ? At(PoolTable, hpRegen) : 1f,
            MpRegen: mpRegen > 0 ? At(PoolTable, mpRegen) : 1f,
            EvaFlat: evaFlat,
            PierceResist: pierce > 0 ? At(ResistTable, pierce) : 1f,
            BluntResist: blunt > 0 ? At(ResistTable, blunt) : 1f,
            BowDefResist: bow > 0 ? At(ResistTable, bow) : 1f,
            Boss: boss,
            Name: name);
    }

    private static int Clamp(int level, int len) => level < 1 ? 1 : level > len ? len : level;
}
