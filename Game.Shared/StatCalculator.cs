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
        (Race.Ork, BaseClass.Fighter) => new BaseStats(40, 30, 10, 20),
        (Race.Ork, BaseClass.Mage)    => new BaseStats(30, 30, 20, 20),
        (Race.Elf, BaseClass.Fighter) => new BaseStats(30, 20, 20, 30),
        (Race.Elf, BaseClass.Mage)    => new BaseStats(20, 20, 30, 30),
        (Race.Human, BaseClass.Fighter) => new BaseStats(35, 25, 15, 25),
        (Race.Human, BaseClass.Mage)    => new BaseStats(25, 25, 25, 25),
        _ => new BaseStats(25, 25, 25, 25)
    };

    // Per design: levels increase hp/mp (max/regen), evasion, accuracy,
    // defence, attack — nothing else. Tanks get more HP, mages more MP
    // (class scaling multipliers come with the class-tree phase).

    public static int MaxHp(int con, int level) => 50 + con * 4 + level * 10;

    public static int MaxMp(int wit, int level) => 30 + wit * 4 + level * 8;

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

    public static int Defence(int con, int level) => con / 3 + level / 2;

    /// <summary>Base MAGIC defence. Same shape as physical Defence but WITHOUT the
    /// CON term — magic defence does NOT scale with any base stat. Everyone gets a
    /// small level-based floor; JEWELS (and the Tank "Anti Magic" passive) add on
    /// top. Used as the divisor in MagicDamage.</summary>
    public static int MagicDefence(int level) => level / 2;

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

    /// <summary>Scalar constants to place damage numbers in a good range.</summary>
    public const float PhysicalK = 25f;
    public const float MagicK = 18f;

    /// <summary>Physical ratio damage: K·(pAtk·lvlMod + power)/pDef. Never zero
    /// (defence is a divisor). 'power' is 0 for basic attacks, the skill power
    /// for skills. Defence floored at 1 to avoid divide-by-zero.</summary>
    public static int PhysicalDamage(int pAtk, int power, int pDef, int attackerLevel)
    {
        float def = Math.Max(1, pDef);
        float dmg = PhysicalK * (pAtk * LevelMod(attackerLevel) + power) / def;
        return Math.Max(1, (int)dmg);
    }

    /// <summary>Magic ratio damage. Now divides by the target's MAGIC defence
    /// (level base + jewels + Anti-Magic), NOT physical pDef — a fully separate
    /// channel. K·(mAtk·lvlMod + power)/mDef, diminishing on mDef.</summary>
    public static int MagicDamage(int mAtk, int power, int mDef, int casterLevel)
    {
        float def = Math.Max(1, mDef);
        float dmg = MagicK * (mAtk * LevelMod(casterLevel) + power) / def;
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

    // ----- Cast & attack speed (L2-style 333 = 100%) -----------------------
    //
    // L2 model: a "speed" stat where 333 ≈ 100% (normal). Higher stat = faster.
    // WIT drives cast speed, DEX drives attack speed, with per-class weights
    // (a mage's WIT matters more than a fighter's). These are APPROXIMATIONS of
    // the L2 tables — tune the per-class coefficients in CastSpeedStat /
    // AttackSpeedStat. Weapon base speed sets the starting point.

    public const int SpeedBaseline = 333;  // stat value that equals 1.0x speed

    /// <summary>Weapon base attack speed (L2 table: Very Fast 433 … Very Slow 227).
    /// Higher = faster. Daggers/bows fast, blunt/staff slow.</summary>
    public static int WeaponAttackBaseSpeed(WeaponType w) => w switch
    {
        WeaponType.Dual => 379,     // daggers/dual: fast
        WeaponType.Bow => 293,      // slow (but long range)
        WeaponType.Sword => 325,    // normal
        WeaponType.Blunt => 325,    // mace/staff: normal
        _ => 300                    // weaponless
    };

    /// <summary>Weapon base cast speed. Caster weapons (blunt: maces/staves) cast at
    /// normal; bladed/bow weapons are a bit slower casters.</summary>
    public static int WeaponCastBaseSpeed(WeaponType w) => w switch
    {
        WeaponType.Blunt => 333,
        _ => 300
    };

    /// <summary>Cast-speed stat from WIT, weighted by class. Approximates the L2
    /// tables: mages gain ~5%/WIT, fighters ~3%/WIT. Returned as a stat where
    /// 333 = 1.0x; capped by StatCaps.CastSpeed.</summary>
    public static int CastSpeedStat(int wit, BaseClass cls, int weaponBase)
    {
        float perWit = cls == BaseClass.Mage ? 0.05f : 0.03f;
        // weaponBase is the weapon's base cast speed (~333 for normal). WIT adds %.
        float stat = weaponBase * (1f + perWit * wit);
        return Math.Min((int)stat, StatCaps.CastSpeed);
    }

    /// <summary>Attack-speed stat from DEX, weighted by class. ~1%/DEX baseline.
    /// 333 = 1.0x; capped by StatCaps.AttackSpeed.</summary>
    public static int AttackSpeedStat(int dex, BaseClass cls, int weaponBase)
    {
        float perDex = 0.01f;
        float stat = weaponBase * (1f + perDex * dex);
        return Math.Min((int)stat, StatCaps.AttackSpeed);
    }

    /// <summary>Convert a speed stat to a time MULTIPLIER (lower = faster).
    /// 333 → 1.0; 666 → 0.5 (twice as fast); 167 → 2.0 (half speed).</summary>
    public static float SpeedMultiplier(int speedStat) =>
        SpeedBaseline / (float)Math.Max(1, speedStat);

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
