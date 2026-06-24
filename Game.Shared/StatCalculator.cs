namespace Game.Shared;

/// <summary>
/// Stat formulas live in Shared so the client can *predict* (tooltips,
/// estimated damage) while the server stays the only authority.
/// Base stats follow the design doc:
///   Ork/Demon  Fighter 40/30/10/20  Mage 30/30/20/20   (CON/ATK/WIT/DEX)
///   Elf/Angel  Fighter 30/20/20/30  Mage 20/20/30/30
///   Human      Fighter 35/25/15/25  Mage 25/25/25/25
/// </summary>
public static class StatCalculator
{
    public readonly record struct BaseStats(int Con, int Atk, int Wit, int Dex);

    public static BaseStats GetBaseStats(Race race, BaseClass cls) => (race, cls) switch
    {
        // BaseStats(Con, Atk, Wit, Dex). Atk = the single power stat: STR for fighters,
        // INT for mages. Fighter WIT kept low (casts little); mage WIT per the dye-
        // stand-in design (elf 23 / human 20 / ork 19). Authentic-L2-style bases.
        (Race.Ork, BaseClass.Fighter) => new BaseStats(47, 40, 10, 26),
        (Race.Ork, BaseClass.Mage)    => new BaseStats(31, 31, 19, 20),
        (Race.Elf, BaseClass.Fighter) => new BaseStats(36, 36, 20, 35),
        (Race.Elf, BaseClass.Mage)    => new BaseStats(25, 37, 23, 24),
        (Race.Human, BaseClass.Fighter) => new BaseStats(43, 40, 15, 30),
        (Race.Human, BaseClass.Mage)    => new BaseStats(27, 41, 20, 21),
        _ => new BaseStats(25, 25, 25, 25)
    };

    // Per design: levels increase hp/mp (max/regen), evasion, accuracy,
    // defence, attack — nothing else. Tanks get more HP, mages more MP.

    // ----- Max HP (authentic L2 model) -------------------------------------
    //  MaxHP = [ ClassLevelMod × (level² + 3·level)/2 + Level1Base ] × ConModifier
    //  (gear/buff/passive % and flat bonuses stack afterwards in RecomputeDerived).

    /// <summary>Per-race+class level-1 base HP (the quadratic's fixed constant).</summary>
    public static int Level1BaseHp(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Human, BaseClass.Fighter) => 126,
        (Race.Human, BaseClass.Mage)    => 88,
        (Race.Elf,   BaseClass.Fighter) => 113,
        (Race.Elf,   BaseClass.Mage)    => 79,
        (Race.Ork,   BaseClass.Fighter) => 137,
        (Race.Ork,   BaseClass.Mage)    => 131,
        _ => 100
    };

    /// <summary>Class HP growth multiplier on the quadratic level term (the L2 "tier").
    /// Tuned so level-75 raw base HP lands on the L2 tracks: tank ~2.9k, warrior ~2.5k,
    /// rogue/archer ~2.0k, nuker ~1.4k, healer ~1.2k. Before 2nd class (no archetype),
    /// a fighter/mage uses a sensible default.</summary>
    public static float HpClassLevelModifier(BaseClass cls, Archetype? arch) => arch switch
    {
        Archetype.Tank    => 0.96f,
        Archetype.Warrior => 0.83f,
        Archetype.Rogue   => 0.66f,
        Archetype.Archer  => 0.66f,
        Archetype.Nuker   => 0.45f,
        Archetype.Healer  => 0.38f,
        _ => cls == BaseClass.Mage ? 0.42f : 0.80f   // base class, pre-2nd
    };

    /// <summary>CON → Max-HP multiplier — EXPONENTIAL, matching the L2 table
    /// (baseline 30 = 1.00: 20→0.79, 40→1.35, 43→1.48, 50→1.83, 55→2.14): ~3.05%
    /// per CON compounded.</summary>
    public static float ConHpModifier(int con) =>
        Math.Clamp(MathF.Pow(1.0305f, con - 30), 0.4f, 8f);

    /// <summary>Per-race+class MEN (we have no MEN stat; this is the L2 base table).
    /// Mages have high MEN (better magic mitigation), fighters low.</summary>
    public static int BaseMen(Race race, BaseClass cls) => (race, cls) switch
    {
        (Race.Human, BaseClass.Fighter) => 25,
        (Race.Human, BaseClass.Mage)    => 39,
        (Race.Elf,   BaseClass.Fighter) => 26,
        (Race.Elf,   BaseClass.Mage)    => 40,
        (Race.Ork,   BaseClass.Fighter) => 27,
        (Race.Ork,   BaseClass.Mage)    => 42,
        _ => 30
    };

    /// <summary>MEN → Magic-Defence and Max-MP multiplier. The L2 MEN curve is a gentle
    /// 1.16→1.65 band (NOT the CON curve): every class is &gt;1, fighters just have less.
    /// Interpolated from the reference table.</summary>
    public static float MenModifier(int men) => InterpolateCurve(MenCurve, men);

    // Reference modifier tables (stat → multiplier). Linearly interpolated; clamped
    // to the endpoints outside the listed range.
    private static readonly (int stat, float mod)[] MenCurve =
    {
        (20, 1.16f), (26, 1.28f), (30, 1.35f), (31, 1.36f),
        (37, 1.44f), (40, 1.49f), (45, 1.57f), (50, 1.65f),
    };

    /// <summary>Linear interpolation of a (stat → multiplier) reference table.</summary>
    private static float InterpolateCurve((int stat, float mod)[] table, int value)
    {
        if (value <= table[0].stat) return table[0].mod;
        if (value >= table[^1].stat) return table[^1].mod;
        for (int i = 1; i < table.Length; i++)
        {
            if (value <= table[i].stat)
            {
                var (s0, m0) = table[i - 1];
                var (s1, m1) = table[i];
                return m0 + (m1 - m0) * (value - s0) / (s1 - s0);
            }
        }
        return table[^1].mod;
    }

    public static int MaxHp(int con, int level, float classLevelMod, int level1Base)
    {
        float rawBase = classLevelMod * (level * level + 3f * level) / 2f + level1Base;
        return (int)(rawBase * ConHpModifier(con));
    }

    /// <summary>Mob HP — kept on the simple linear curve. The player formula's
    /// exponential CON modifier would explode on mob-scale CON, so mobs use this.</summary>
    public static int MobMaxHp(int con, int level) => 50 + con * 4 + level * 10;

    // ----- Max MP (authentic L2: Base_MP tier curve × MEN, like HP) --------
    //  MaxMP = (MpClassLevelMod·(L²+3L)/2 + Level1BaseMp) × MenModifier
    //  MP scales with MEN (not WIT). Tiers tuned to the L75 raw tracks:
    //  Healer 2000 · Wizard/Nuker 1550 · Buffer 1100 · Fighter/Tank 500.

    public static float MpClassLevelModifier(BaseClass cls, Archetype? arch) => arch switch
    {
        Archetype.Healer => 0.68f,   // ~2000 @L75 raw
        Archetype.Nuker  => 0.53f,   // ~1550
        Archetype.Tank or Archetype.Warrior or Archetype.Rogue or Archetype.Archer => 0.17f,  // ~500
        _ => cls == BaseClass.Mage ? 0.50f : 0.17f   // base class, pre-2nd
    };

    public static int Level1BaseMp(BaseClass cls) => cls == BaseClass.Mage ? 40 : 15;

    public static int MaxMp(int men, int level, float mpClassLevelMod, int level1BaseMp)
    {
        float rawBase = mpClassLevelMod * (level * level + 3f * level) / 2f + level1BaseMp;
        return (int)(rawBase * MenModifier(men));
    }

    /// <summary>Mob MP — simple level curve (mobs aren't MP-limited; avoids the
    /// MEN/tier machinery).</summary>
    public static int MobMaxMp(int level) => 40 + level * 6;

    public static float HpRegenPerSecond(int con, int level) => 1f + con * 0.05f + level * 0.1f;

    public static float MpRegenPerSecond(int wit, int level) => 1f + wit * 0.05f + level * 0.08f;

    /// <summary>Base chance for a normal attack to miss is 2%; each point of
    /// evasion advantage adds 1%, clamped so a higher-level character almost
    /// never misses a lower one (design: floor ~1%, never above 90%).</summary>
    public static float MissChance(int attackerAccuracy, int targetEvasion)
    {
        const float baseMiss = 0.02f;
        float diff = (targetEvasion - attackerAccuracy) * 0.01f;
        return Math.Clamp(baseMiss + diff, 0.01f, 0.90f);
    }

    public static int Accuracy(int dex, int level) => dex + level;

    public static int Evasion(int dex, int level) => dex + level;

    // ----- Combat (Phase 2) -------------------------------------------------

    /// <summary>Effective attack power. Weapon damage joins this formula
    /// in the items phase: weapon + stat + buffs/passives.</summary>
    public static int AttackPower(int atkStat, int level) => atkStat + level * 2;

    // ----- Defence (authentic L2: armor/jewels + level² /100, no CON) ------
    //  P.Def = (naked 68 + level²/100 + Σ armor pDef + flat passives) × masteries/buffs
    //  M.Def = (naked 20 + level²/100 + Σ jewel mDef + flat passives) × MEN × buffs
    //  (armor/jewel/passive/mastery/MEN/buff stacking happens in Entity.RecomputeDerived;
    //   these provide the naked baseline + level modifier. Mobs use MobDefence.)

    /// <summary>Player base physical defence: naked baseline + level²/100. No CON.</summary>
    public static int PhysicalDefenceBase(int level) => 68 + level * level / 100;

    /// <summary>Player base magic defence: naked baseline + level²/100. Jewels and the
    /// MEN modifier apply on top in RecomputeDerived. (No base-stat term here.)</summary>
    public static int MagicDefenceBase(int level) => 20 + level * level / 100;

    /// <summary>Mob defence — kept on the old simple curve (mobs have no armor/jewels;
    /// the player naked baseline would make low-level mobs too tanky).</summary>
    public static int MobDefence(int con, int level) => con / 3 + level / 2;

    /// <summary>Crit chance from DEX. Race/class/equipment modifiers come
    /// later. 25 DEX = 10%; capped at 50%.</summary>
    public static float CritChance(int dex) => Math.Clamp(0.05f + dex * 0.002f, 0f, 0.50f);

    /// <summary>Raw damage of one basic attack before the crit multiplier.
    /// Kept for compatibility; the new ratio model is PhysicalDamage below.</summary>
    public static int BasicAttackDamage(int attackPower, int defence) =>
        Math.Max(1, attackPower * 2 - defence);

    public const float CritMultiplier = 2.0f;

    // ===== L2-style ratio damage ===========================================
    //
    // Damage is a RATIO of attack to defence (not a subtraction), so defence
    // gives diminishing returns and never fully blocks. lvlMod scales the whole
    // curve by level. Physical and magic share the shape but differ: physical
    // can be EVADED and crits up to x10; magic can FAIL (resist roll) and crits
    // up to x3. Magic currently divides by physical defence too (magic-resist
    // passives/jewels add a separate multiplier later).

    /// <summary>Level modifier: (level+89)/100. L1=0.90, L11=1.00, L80=1.69.</summary>
    public static float LevelMod(int level) => (level + 89) / 100f;

    // Damage balance constants (authentic L2 STRUCTURE; constants tuned to OUR scale).
    //  Physical: 77·pAtk/pDef  — L2's 77 transfers (our pAtk/pDef ratio ≈ 1.2).
    //  Magic:    K·power·√mAtk/mDef — L2 uses 91, but that assumes mAtk in the
    //    hundreds/thousands; ours is ~120 (√≈11), so the magic K is recalibrated
    //    DOWN. The √ gives diminishing returns on stacked M.Atk (spell power/cast
    //    speed become the meta), exactly as in L2. FIRST-PASS values — tune via the
    //    class-vs-class matchup matrix.
    public const float PhysicalK = 77f;
    public const float MagicK = 8f;

    /// <summary>Physical ratio damage (L2 model): 77·(pAtk + skillPower)/pDef. No level
    /// term (level is already baked into pAtk/pDef growth). 'power' is 0 for a basic
    /// attack, the skill's power for a skill. Crit / variance / soulshot are applied by
    /// the caller. Defence floored at 1.</summary>
    public static int PhysicalDamage(int pAtk, int power, int pDef, int attackerLevel)
    {
        float def = Math.Max(1, pDef);
        float dmg = PhysicalK * (pAtk + power) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>Magic ratio damage (L2 model): K·skillPower·√mAtk/mDef. The SQUARE ROOT
    /// of M.Atk means stacking raw M.Atk gives diminishing returns. 'power' is the
    /// spell's base power. Divides by MAGIC defence (separate channel). Crit / fail /
    /// blessed-spiritshot are applied by the caller. Defence floored at 1.</summary>
    public static int MagicDamage(int mAtk, int power, int mDef, int casterLevel)
    {
        float def = Math.Max(1, mDef);
        float dmg = MagicK * power * MathF.Sqrt(Math.Max(0, mAtk)) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>Weapon damage variance — a ± random band. Spikier weapons (bow,
    /// dagger) get a wider band; steady weapons (blunt) narrower. Returns a
    /// multiplier around 1.0.</summary>
    public static float WeaponVariance(WeaponType weapon, System.Random rng)
    {
        float band = weapon switch
        {
            WeaponType.Bow => 0.30f,
            WeaponType.Dual => 0.20f,    // daggers/dual: spiky
            WeaponType.Blunt => 0.15f,   // blunt/staff: steadier
            _ => 0.10f
        };
        return 1f + ((float)rng.NextDouble() * 2f - 1f) * band;
    }

    // ----- Crit (split: physical vs magic, each capped) --------------------

    /// <summary>Physical crit RATE from DEX. Cap 50% (DEX 500 ≈ cap).</summary>
    public static float PhysicalCritChance(int dex) =>
        Math.Clamp(0.05f + dex * 0.0009f, 0f, StatCaps.PhysicalCritRate);

    /// <summary>Magic crit RATE from WIT. Cap 20% (WIT 200 ≈ cap).</summary>
    public static float MagicCritChance(int wit) =>
        Math.Clamp(wit * 0.001f, 0f, StatCaps.MagicCritRate);

    /// <summary>Physical crit DAMAGE multiplier, capped x10.</summary>
    public static float PhysicalCritMult(float bonus = 0f) =>
        Math.Min(2.0f + bonus, StatCaps.PhysicalCritDamage);

    /// <summary>Magic crit DAMAGE multiplier, capped x3.</summary>
    public static float MagicCritMult(float bonus = 0f) =>
        Math.Min(2.0f + bonus, StatCaps.MagicCritDamage);

    /// <summary>Magic fail chance — a spell does reduced damage when it "fails".
    /// Always at least 1%, climbs with the target's level advantage up to 90%.
    /// <paramref name="targetMinFail"/> lets the TARGET raise the FLOOR against
    /// itself (Tank "Anti Magic" ~10%, mages ~5%) so casters always have a real
    /// fail chance against the prepared.</summary>
    public static float MagicFailChance(int casterLevel, int targetLevel, float targetMinFail = 0f)
    {
        float floor = Math.Max(StatCaps.MagicFailFloor, targetMinFail);
        return Math.Clamp(0.03f + (targetLevel - casterLevel) * 0.02f, floor, StatCaps.MagicFailMax);
    }

    /// <summary>Offensive MAGIC interrupt power from WIT. Mirrors the WIT scale in
    /// InterruptResist (wit*2) so a WIT-mage out-interrupts an equal-level ATK-mage
    /// while the ATK-mage hits harder. Added to a magic skill's flat InterruptPower.</summary>
    public static int MagicInterruptPower(int wit) => wit * 2;

    // ----- Interruption (caster resist) -----------------------------------

    /// <summary>Base interrupt resistance from WIT + level. Higher = harder to
    /// interrupt. A skill's InterruptDefense adds to this; an attacker's
    /// InterruptPower subtracts. Tune the coefficients here.</summary>
    public static int InterruptResist(int wit, int level) => wit * 2 + level;

    /// <summary>Per-hit chance to interrupt a cast. base + attacker power − caster
    /// resist, clamped. With power=0 and a normal caster, interrupts are
    /// occasional; high InterruptPower (an interrupt skill) makes it reliable;
    /// high InterruptDefense (an ultimate) makes it ~never.</summary>
    public static float InterruptChance(int casterResist, int skillDefense, int attackerPower)
    {
        // Scale the stat difference into a probability. 0 diff ≈ 25% baseline.
        float baseChance = 0.25f;
        float diff = (attackerPower) - (casterResist + skillDefense);
        float chance = baseChance + diff * 0.01f;
        return Math.Clamp(chance, 0f, 1f);
    }

    // ----- Cast & attack speed (authentic L2 model) ------------------------
    //
    // L2: actual cast/attack time = baseTime × 333 / speedStat, where the speed stat
    // is built MULTIPLICATIVELY:
    //   castSpd = ClassBaseCast × WitModifier × weaponFactor × gearFactor × ∏(1+buff%)
    // and capped (cast 1999 = 6× faster than the 333 reference, attack 1500). The full
    // assembly lives in Entity.EffectiveCast/AttackSpeedMultiplier; this file provides
    // the class bases, the exponential WIT curve, weapon factors and the 333 reference.

    public const int SpeedBaseline = 333;  // stat value that equals 1.0x speed


    /// <summary>Class base casting speed (L2 engine constant, before WIT/gear/buffs):
    /// mages 166, fighters 150.</summary>
    public static int ClassBaseCastSpeed(BaseClass cls) =>
        cls == BaseClass.Mage ? 166 : 150;

    /// <summary>Signature-stat bonus a character accrues by level — stands in for the
    /// (not-yet-built) +stat dyes and stat-set bonuses. Mages apply it to WIT, fighters
    /// to DEX. Cumulative milestones: +1@20, +1@30, +2@40, +1@50, +1@60, +1@70, +5@80
    /// (= +12 total). E.g. an elf wizard (base WIT 23) climbs to 30 by level 70, 35 @80.</summary>
    public static int LevelStatBonus(int level)
    {
        int b = 0;
        if (level >= 20) b += 1;
        if (level >= 30) b += 1;
        if (level >= 40) b += 2;
        if (level >= 50) b += 1;
        if (level >= 60) b += 1;
        if (level >= 70) b += 1;
        if (level >= 80) b += 5;
        return b;
    }

    /// <summary>DEX physical-attack-speed modifier — EXPONENTIAL, matching the L2 table
    /// (baseline 30 = 1.00: 20→0.90, 35→1.05, 40→1.11, 50→1.23): ~1.05% per DEX
    /// compounded. Clamped so very low DEX can't stall attacks entirely.</summary>
    public static float AttackDexModifier(int dex) =>
        Math.Clamp(MathF.Pow(1.0105f, dex - 30), 0.4f, 8f);

    /// <summary>WIT casting-speed modifier — EXPONENTIAL, matching the L2 table
    /// (20→1.00, 30→1.63, 40→2.65, 50→4.32): ×1.63 per +10 WIT. Clamped so very low
    /// WIT can't stall casting entirely.</summary>
    public static float CastWitModifier(int wit) =>
        Math.Clamp(MathF.Pow(1.63f, (wit - 20) / 10f), 0.4f, 8f);

    /// <summary>Weapon cast factor: staves/maces are full casters; bladed/bow weapons
    /// cast clumsily. Multiplicative on the cast stat.</summary>
    public static float WeaponCastFactor(WeaponType w) => w switch
    {
        WeaponType.Blunt => 1.0f,   // staff/mace: caster weapon
        _ => 0.8f                   // bladed/bow: clumsy caster
    };

    /// <summary>Weapon base attack speed (authentic L2 bases; baseline 333 = 1.0×).
    /// Daggers/fists fastest, 2H/bow slowest.</summary>
    public static int WeaponAttackBaseSpeed(WeaponType w) => w switch
    {
        WeaponType.Dual => 433,     // daggers/dual: fastest
        WeaponType.Sword => 379,    // 1H sword/blunt
        WeaponType.Bow => 293,      // bow: slow (but long range)
        WeaponType.Blunt => 325,    // staff/2H: slow
        _ => 433                    // fists (weaponless): fast
    };

    // ----- Progression -------------------------------------------------------

    /// <summary>Exp required to go from <paramref name="level"/> to the next.</summary>
    public static long ExpToNext(int level) => 25L * level * level;

    public static int MobExpReward(int mobLevel) => 40 + mobLevel * 35;

    /// <summary>Base gold a mob drops, by level (scaled by RateConfig.GoldAmountRate
    /// and a small variance at the drop site).</summary>
    public static int MobGoldReward(int mobLevel) => 25 + mobLevel * 8;

    /// <summary>Mob stat block by level. Per design: higher-level mobs must
    /// out-stat lower-level characters.</summary>
    public static BaseStats MobStats(int level) =>
        new(Con: 15 + level * 2, Atk: 8 + level * 3, Wit: 5, Dex: 10 + level);

    /// <summary>Mob MAGIC defence by level. The universal <see cref="MagicDefence"/>
    /// base (level/2) leaves low-level mobs at ~0 mDef, so spells divide by ~1 and
    /// one-shot them. Mobs get a dedicated mDef on roughly the same curve as their
    /// physical defence, so magic and physical land in a comparable range. (Players
    /// keep the level base + jewels; this is mob-only.)</summary>
    public static int MobMagicDefence(int level) => Math.Max(5, (int)(level * 1.2f));

    /// <summary>Per-archetype basic-attack damage multiplier — the core of
    /// class identity. Mages barely autoattack (rely on skills/MP); fighters &
    /// archers hit full; rogues & tanks are reduced (they lean on crits/skills
    /// and defence respectively). Base classes use 1.0 until they specialize.</summary>
    public static float BasicAttackMultiplier(Archetype? archetype) => archetype switch
    {
        Archetype.Warrior => 1.10f,
        Archetype.Archer => 1.00f,
        Archetype.Rogue => 0.65f,   // leans on crits + skills
        Archetype.Tank => 0.55f,    // leans on defence
        Archetype.Healer => 0.15f,  // mages: near-zero basic attack
        Archetype.Nuker => 0.15f,
        _ => 1.0f                    // base Fighter/Mage before class change
    };

    /// <summary>Extra crit chance from archetype (archers & rogues spike here).</summary>
    public static float ArchetypeCritBonus(Archetype? archetype) => archetype switch
    {
        Archetype.Archer => 0.15f,
        Archetype.Rogue => 0.20f,
        _ => 0f
    };

    /// <summary>Per-weapon crit-rate FACTOR (multiplies the base/DEX crit chance).
    /// From the weapon table's crit_modifier: Sword 0.80, Dual/Bow 1.20, Blunt 0.40.
    /// Blunt trades crit away for accuracy.</summary>
    public static float WeaponCritFactor(WeaponType w) => w switch
    {
        WeaponType.Dual => 1.20f,
        WeaponType.Bow => 1.20f,
        WeaponType.Sword => 0.80f,
        WeaponType.Blunt => 0.40f,
        _ => 1.0f                    // weaponless: unchanged
    };

    /// <summary>Per-weapon ACCURACY bonus. Blunt weapons are easier to land (high
    /// base accuracy) — the counterpart to their low crit. Tune later.</summary>
    public static int WeaponAccuracyBonus(WeaponType w) => w switch
    {
        WeaponType.Blunt => 10,
        _ => 0
    };

    /// <summary>Extra evasion from archetype (rogues are slippery).</summary>
    public static int ArchetypeEvasionBonus(Archetype? archetype, int level) => archetype switch
    {
        Archetype.Rogue => 10 + level,
        Archetype.Archer => 5 + level / 2,
        _ => 0
    };

    /// <summary>Tank "Anti Magic" passive: extra MAGIC defence on top of the level
    /// base, roughly doubling a tank's innate magic resistance. (Modeled as an
    /// archetype identity bonus like the others; can become a learnable passive
    /// later.)</summary>
    public static int ArchetypeMagicDefenceBonus(Archetype? archetype, int level) => archetype switch
    {
        Archetype.Tank => level / 2,   // doubles the level-based base
        _ => 0
    };

    /// <summary>The MINIMUM magic-fail chance an attacker has against this target.
    /// Tanks ("Anti Magic") and mages harden themselves so spells always have a
    /// real chance to fizzle on them.</summary>
    public static float ArchetypeMagicFailFloor(Archetype? archetype) => archetype switch
    {
        Archetype.Tank => 0.10f,
        Archetype.Nuker => 0.05f,
        Archetype.Healer => 0.05f,
        _ => 0f
    };

    /// <summary>Rogue passive: their BASIC attacks carry magic-interrupt power
    /// (daggers/duals are interrupt machines). Other archetypes' basics don't
    /// interrupt. Tunable; can move to a weapon attribute / learnable passive.</summary>
    public static int ArchetypeBasicInterruptPower(Archetype? archetype, int level) => archetype switch
    {
        Archetype.Rogue => 50 + level,
        _ => 0
    };
}
