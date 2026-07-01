using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A timed stat modifier (buff or debuff) on an entity. Carries a
/// flags Effect (one buff can touch several stats) and a per-effect magnitude
/// array with flat/percent modes. Identified by Key; same-Key buffs compare by
/// Rank; a buff also unconditionally removes any active buff in Replaces.</summary>
public class BuffInstance
{
    // Settable: a stacking effect re-snapshots these to the current stack LEVEL each stack.
    public required SkillEffect Effect { get; set; }
    public required EffectMagnitude[] Magnitudes { get; set; }
    public int TicksRemaining { get; set; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    public string Key { get; init; } = "";
    public int Rank { get; init; }
    public string[] Replaces { get; init; } = Array.Empty<string>();

    /// <summary>A TOGGLE/stance buff: never expires on its own (the player clicks the
    /// skill again, or double-clicks the buff, to end it). TickBuffs skips it.</summary>
    public bool Toggle { get; init; }

    // ----- Damage-over-time (DoT) stacks -----
    /// <summary>Current stack count (1..MaxStacks). Magnitudes scale with this.</summary>
    public int Stacks { get; set; } = 1;
    /// <summary>Maximum stacks this effect can build (1 = non-stacking).</summary>
    public int MaxStacks { get; set; } = 1;
    /// <summary>DoT damage per stack per second (0 = not a DoT).</summary>
    public int DotPower { get; set; }
    /// <summary>Entity that applied this effect (for DoT damage attribution / kill credit).</summary>
    public Guid SourceId { get; set; }
    /// <summary>An internal mechanic effect (e.g. a DoT stack counter): not shown on the buff
    /// bar and not touched by cure/cancel — only its own burst skill consumes it.</summary>
    public bool Internal { get; set; }
    /// <summary>Can this effect be removed by cure/cancel? (false = immune.)</summary>
    public bool Cancellable { get; set; } = true;
    /// <summary>Remaining absorb pool for a Shield effect (damage soaked before HP). The buff
    /// is removed when it hits 0.</summary>
    public int ShieldPool { get; set; }

    public bool Has(SkillEffect flag) => (Effect & flag) != 0;

    public bool IsDebuff => (Effect & SkillEffect.AnyDebuff) != 0;

    /// <summary>Sum of this buff's flat entries for an effect. For a stacking effect the
    /// Magnitudes are re-snapshotted to the current stack LEVEL on each stack (see
    /// ApplyBuff + SkillDef.StackLevels), so no per-read scaling is needed here.</summary>
    public float Flat(SkillEffect flag)
    {
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == flag && m.Mode == ModifierMode.Flat) sum += m.Value;
        return sum;
    }

    /// <summary>Sum of this buff's percent entries for an effect.</summary>
    public float Percent(SkillEffect flag)
    {
        float sum = 0f;
        foreach (var m in Magnitudes)
            if (m.Effect == flag && m.Mode == ModifierMode.Percent) sum += m.Value;
        return sum;
    }
}

/// <summary>One item instance in a player's inventory.</summary>
public class InventoryItem
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public required string DefId { get; init; }
    public bool Equipped { get; set; }
    public int Enchant { get; set; }

    /// <summary>Stack size for consumables/scrolls. Gear is always 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Rolled bonus attributes (gear only).</summary>
    public List<ItemAttribute> Attributes { get; set; } = new();

    /// <summary>DB instance id, preserved across saves (null = never persisted).</summary>
    public Guid? PersistentInstanceId { get; set; }

    public InventoryItemDto ToDto() =>
        new(InstanceId, DefId, Equipped, Enchant, Quantity, Attributes.ToArray());
}

/// <summary>
/// Live server-side state of one thing in the world.
/// Mutated exclusively by the game-loop thread.
/// </summary>
public class Entity
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required EntityKind Kind { get; init; }

    // Settable (not init) so a DEBUG character-reset can re-roll race/base class
    // in place. Normal play never changes these after creation.
    public Race Race { get; set; }
    public BaseClass BaseClass { get; set; }

    /// <summary>DB character id (null for mobs / unsaved).</summary>
    public int? PersistentId { get; set; }

    /// <summary>Unspent skill points (earned with exp, spent to learn skills).</summary>
    public int SkillPoints { get; set; }

    /// <summary>Learned skills → the current LEVEL of each (1 for single-level skills).
    /// A skill is "known" iff it's a key here; its level selects the SkillDef.*At(level)
    /// values (Power/Magnitudes/Passive/MpCost).</summary>
    public Dictionary<string, int> LearnedSkills { get; } = new();

    /// <summary>The learned level of a skill, or 0 if not known.</summary>
    public int SkillLevelOf(string id) => LearnedSkills.GetValueOrDefault(id);

    /// <summary>True if the character knows the skill at any level.</summary>
    public bool HasSkill(string id) => LearnedSkills.ContainsKey(id);

    /// <summary>Active quests -> progress (step index + counter).</summary>
    public Dictionary<string, CharacterQuestState> ActiveQuests { get; } = new();

    /// <summary>Completed quest ids.</summary>
    public HashSet<string> CompletedQuests { get; } = new();

    /// <summary>NPC id this entity represents (NPCs only).</summary>
    public string? NpcId { get; set; }
    public NpcRole NpcRole { get; set; }

    /// <summary>Account-level admin flag (elevated commands, god mode).</summary>
    public bool IsAdmin { get; set; }

    /// <summary>God mode: takes no damage (admin only).</summary>
    public bool GodMode { get; set; }

    /// <summary>Jailed players are teleported to jail and cannot move out.</summary>
    public bool Jailed { get; set; }

    /// <summary>0 = none; otherwise a ClassCatalog id.</summary>
    public int SecondClass { get; set; }

    /// <summary>0 = none; otherwise a ThirdClassCatalog id (101-136).</summary>
    public int ThirdClass { get; set; }

    public Archetype? Archetype =>
        SecondClass > 0 ? ClassCatalog.Get(SecondClass)?.Archetype : null;

    /// <summary>The 3rd-class discipline once chosen (null before lvl-40 change).
    /// Discipline + Race selects the skill list; the parent archetype is unchanged.</summary>
    public Discipline? Discipline =>
        ThirdClass > 0 ? ThirdClassCatalog.Get(ThirdClass)?.Discipline : null;

    public float X { get; set; }
    public float Y { get; set; }

    public float? TargetX { get; set; }
    public float? TargetY { get; set; }

    /// <summary>Computed RUN speed (race+class base + gear/buffs), clamped to
    /// the move cap. This is the value movement uses when running.</summary>
    public float Speed { get; set; } = GameConstants.BasePlayerSpeed;

    /// <summary>Movement/regen state (players). Mobs use Engaged to pick walk/run.</summary>
    public MoveState MoveState { get; set; } = MoveState.Running;

    /// <summary>Ticks remaining in the stand-up recovery after sitting was broken.
    /// While &gt; 0 the player can't move/cast/act.</summary>
    public int StandUpTicks { get; set; }

    /// <summary>Per-entity move-speed ceiling (default 250; a future rogue
    /// ultimate raises it to outrun even a buffed mage).</summary>
    public float MoveSpeedCap { get; set; } = StatCaps.MoveSpeed;

    /// <summary>Mob walk/run speeds (from MobCatalog). Players derive walk from run.</summary>
    public float WalkSpeed { get; set; }
    public float RunSpeed { get; set; }

    // ----- Core stats (CON/ATK/WIT/DEX) --------------------------------------

    public int Con { get; set; }
    public int AtkStat { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }

    /// <summary>WIT used for ALL gameplay math (cast speed, MP, magic crit, interrupt,
    /// heals). MAGES gain WIT at level milestones to stand in for the not-yet-built dye
    /// + WIT-set bonuses; non-mages use flat WIT. Stored <see cref="Wit"/> stays the
    /// persisted base. See StatCalculator.LevelStatBonus.</summary>
    public int EffectiveWit =>
        Wit + (BaseClass == BaseClass.Mage ? StatCalculator.LevelStatBonus(Level) : 0);

    /// <summary>DEX used for ALL gameplay math (attack speed, crit, evasion, accuracy).
    /// FIGHTERS gain DEX at level milestones (the same dye stand-in); mages use flat
    /// DEX. Stored <see cref="Dex"/> stays the persisted base.</summary>
    public int EffectiveDex =>
        Dex + (BaseClass == BaseClass.Fighter ? StatCalculator.LevelStatBonus(Level) : 0);

    // ----- Derived stats (recomputed on level-up / equip / class change) -------

    public int Level { get; set; } = 1;
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }
    public int AttackPower { get; set; }      // physical attack (pAtk); feeds SKILLS
    public int BasicAttackPower { get; set; } // feeds auto-attacks (archetype-scaled)
    public int MagicAttack { get; set; }      // magic attack (mAtk); feeds spells
    public int Defence { get; set; }
    public int MagicDefence { get; set; }     // magic-only defence (level base + jewels + Anti-Magic)
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public WeaponType WeaponType { get; set; } = WeaponType.None;
    /// <summary>Hands of the equipped weapon (1H/2H) — lets weapon masteries gate on it
    /// (e.g. Warrior trains only 2H, Tank only 1H). Defaults to 1H / unarmed.</summary>
    public WeaponHands WeaponHands { get; set; } = WeaponHands.OneHand;
    public float CritChance { get; set; }       // physical crit rate
    public float MagicCritChance { get; set; }  // magic crit rate (from WIT)
    public int InterruptResist { get; set; }    // resist casting interruption (from WIT)
    public int MagicInterruptBonus { get; set; } // OFFENSIVE magic interrupt power (from WIT)
    public int BasicAttackInterruptPower { get; set; } // interrupt power carried by basic attacks (rogues)
    public float MagicFailFloor { get; set; }   // anti-magic floor: min magic-fail chance attackers have vs this entity
    public float EvadeFloor { get; set; }        // rogue: guaranteed min chance to dodge physical attacks
    public float HitFloor { get; set; }          // warrior: guaranteed min chance THIS entity lands a physical attack
    public bool Immune { get; set; }             // ultimate total-avoid (future buff); attacks always miss/fail
    public float HpRegenBonus { get; set; }     // flat HP/s from gear attributes
    public float MpRegenBonus { get; set; }     // flat MP/s from gear attributes
    public float HpRegenMult { get; set; } = 1f; // HP-regen multiplier (armor mastery)
    public float MpRegenMult { get; set; } = 1f; // MP-regen multiplier (armor mastery)
    public float CritDamageBonus { get; set; }  // crit-multiplier bonus from gear (e.g. +0.20x)
    // ----- Healer buff/effect layer (folded from buffs + passives in RecomputeDerived) -----
    public float CooldownReduction { get; set; } // spell reuse-delay reduction (0..cap)
    public float CritRateResist { get; set; }    // reduces an attacker's physical crit CHANCE vs you
    public float CritDmgResist { get; set; }     // reduces incoming physical crit EXTRA damage
    public float BowResist { get; set; }         // reduces damage taken from BOW attacks
    public float MagicFailResist { get; set; }   // reduces YOUR spells' own fail chance
    public float MeleeVamp { get; set; }         // basic (melee) attack lifesteal fraction
    public float SpellVamp { get; set; }         // damage-spell lifesteal fraction
    // ----- Damage-OUT bonuses (fractions): 2×3 matrix context (PvE/PvP) × source
    //       (skill=physical skill / magic / basic). The damage pipeline reads ONE. -----
    public float PveSkillDamageBonus { get; set; }   // +% physical-skill damage vs mobs
    public float PveMagicDamageBonus { get; set; }   // +% magic-skill damage vs mobs
    public float PveBasicDamageBonus { get; set; }   // +% basic-attack damage vs mobs
    public float PvpSkillDamageBonus { get; set; }   // +% physical-skill damage vs players
    public float PvpMagicDamageBonus { get; set; }   // +% magic-skill damage vs players
    public float PvpBasicDamageBonus { get; set; }   // +% basic-attack damage vs players
    public float CancelResist { get; set; }          // chance each buff resists an enemy cancel
    public string ActiveArmorSet { get; set; } = ""; // name of the completed armor set bonus, "" if none
    public string ArmorMasteryLabel { get; set; } = ""; // armor-weight mastery status for the UI

    // ----- Shield / block (0 if no shield equipped) -----
    public bool HasShield { get; set; }
    public float BlockChance { get; set; }       // chance to block a physical hit
    public float BlockReduction { get; set; }    // damage fraction removed on block
    public int ShieldDefense { get; set; }       // flat defence from the shield
    public float ShieldCritDefense { get; set; } // reduces attacker crit chance
    public float BasicAttackRange { get; set; } = GameConstants.MeleeRange;

    /// <summary>Cast-time multiplier from item Cast Speed attributes (0.8 = 20% faster).</summary>
    public float CastSpeedMultiplier { get; set; } = 1f;
    /// <summary>Attack-interval multiplier from Attack Speed attributes.</summary>
    public float AttackSpeedMultiplier { get; set; } = 1f;

    public long Exp { get; set; }

    /// <summary>Gold wallet (currency). Drops from mobs; spent at vendors / teleports.</summary>
    public long Gold { get; set; }

    // ----- Inventory (players only) ----------------------------------------------

    public List<InventoryItem> Inventory { get; } = new();

    // ----- Buffs / debuffs ------------------------------------------------------------

    public List<BuffInstance> Buffs { get; } = new();

    /// <summary>Held in place by a Root effect — cannot move until it expires.</summary>
    public bool IsRooted
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Root)) return true;
            return false;
        }
    }

    /// <summary>Stunned: cannot move, cast or attack while any Stun effect is active.</summary>
    public bool IsStunned
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Stun)) return true;
            return false;
        }
    }

    /// <summary>Feared: cannot cast or attack (but may still move) while any Fear is active.</summary>
    public bool IsFeared
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Fear)) return true;
            return false;
        }
    }

    /// <summary>Cannot take an action (cast/attack/skill) this tick due to a stun or fear.</summary>
    public bool IsActionLocked => IsStunned || IsFeared;

    /// <summary>Slowed: any Slow debuff is active (for conditional-damage skills).</summary>
    public bool IsSlowed
    {
        get
        {
            foreach (var b in Buffs) if (b.Has(SkillEffect.Slow)) return true;
            return false;
        }
    }

    /// <summary>Multiplier on healing RECEIVED (anti-heal debuffs lower it). 1 = normal.</summary>
    public float HealReceivedMultiplier
    {
        get
        {
            float reduce = 0f;
            foreach (var b in Buffs) if (b.Has(SkillEffect.DebuffHealRecv))
                reduce += b.Percent(SkillEffect.DebuffHealRecv);
            return Math.Clamp(1f - reduce, 0f, 1f);
        }
    }

    /// <summary>De-taunt stub (no threat system yet): on a mob, while &gt;0 it will
    /// not re-aggro <see cref="DetauntFromId"/> (the entity that shed it).</summary>
    public int DetauntTicks { get; set; }
    public Guid? DetauntFromId { get; set; }

    /// <summary>Apply all buffs for one effect to a base value using the
    /// standard formula: (base + sum flats) * (1 + sum percents). Optionally a
    /// second (debuff) flag subtracts its percents/flats (used for defence).</summary>
    private float ModifiedStat(float baseValue, SkillEffect plusFlag, SkillEffect minusFlag = SkillEffect.None)
    {
        float flat = 0f, percent = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(plusFlag))
            {
                flat += buff.Flat(plusFlag);
                percent += buff.Percent(plusFlag);
            }
            if (minusFlag != SkillEffect.None && buff.Has(minusFlag))
            {
                flat -= buff.Flat(minusFlag);
                percent -= buff.Percent(minusFlag);
            }
        }
        return Math.Max(0f, (baseValue + flat) * (1f + percent));
    }

    /// <summary>Folds TWO additive buff flags (a shared one + a channel-specific one),
    /// e.g. BuffAtk (both channels) plus BuffPhysAtk / BuffMagAtk (one channel only).</summary>
    private float ModifiedStatDual(float baseValue, SkillEffect plusA, SkillEffect plusB)
    {
        float flat = 0f, percent = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(plusA)) { flat += buff.Flat(plusA); percent += buff.Percent(plusA); }
            if (buff.Has(plusB)) { flat += buff.Flat(plusB); percent += buff.Percent(plusB); }
        }
        return Math.Max(0f, (baseValue + flat) * (1f + percent));
    }

    public float EffectiveAttack => AtkDebuffed(ModifiedStatDual(AttackPower, SkillEffect.BuffAtk, SkillEffect.BuffPhysAtk));

    /// <summary>Buffed magic attack (mAtk): the shared BuffAtk plus magic-only BuffMagAtk.</summary>
    public float EffectiveMagicAttack => AtkDebuffed(ModifiedStatDual(MagicAttack, SkillEffect.BuffAtk, SkillEffect.BuffMagAtk));

    /// <summary>Buffed attack power for BASIC attacks (archetype-scaled). Basic attacks
    /// are physical, so they take the shared BuffAtk plus physical-only BuffPhysAtk.</summary>
    public float EffectiveBasicAttack => AtkDebuffed(ModifiedStatDual(BasicAttackPower, SkillEffect.BuffAtk, SkillEffect.BuffPhysAtk));

    /// <summary>Apply DebuffAtk (e.g. venom) as a multiplicative reduction to an attack value.</summary>
    private float AtkDebuffed(float v)
    {
        float pct = 0f;
        foreach (var b in Buffs) if (b.Has(SkillEffect.DebuffAtk)) pct += b.Percent(SkillEffect.DebuffAtk);
        return Math.Max(0f, v * (1f - pct));
    }

    /// <summary>Move speed including move-speed buffs (flat + percent).</summary>
    /// <summary>Current move speed: 0 if sitting or standing up, walk or run base
    /// by state, plus move-speed buffs, clamped to the (raisable) move cap.</summary>
    public float EffectiveSpeed
    {
        get
        {
            if (IsRooted || IsStunned) return 0f;   // held in place by Root or Stun

            if (Kind == EntityKind.Mob)
            {
                // Mobs walk while wandering, run while aggroed/engaged.
                float mobBase = Engaged ? RunSpeed : WalkSpeed;
                if (mobBase <= 0) mobBase = Speed;
                return ModifiedStat(mobBase, SkillEffect.BuffMoveSpeed) * (1f - SlowFraction);
            }

            if (StandUpTicks > 0 || MoveState == MoveState.Sitting)
                return 0f;
            float baseSpeed = MoveState == MoveState.Walking ? WalkSpeed : RunSpeed;
            if (baseSpeed <= 0) baseSpeed = Speed;   // fallback
            float withBuffs = ModifiedStat(baseSpeed, SkillEffect.BuffMoveSpeed) * (1f - SlowFraction);
            return Math.Min(withBuffs, MoveSpeedCap);
        }
    }

    /// <summary>Total move-speed reduction from Slow debuffs (summed Percent of the Slow
    /// effect), clamped to 90% so a slow never fully stops you (that's Root's job).</summary>
    private float SlowFraction
    {
        get
        {
            float pct = 0f;
            foreach (var b in Buffs) if (b.Has(SkillEffect.Slow)) pct += b.Percent(SkillEffect.Slow);
            return Math.Clamp(pct, 0f, 0.9f);
        }
    }

    /// <summary>Defence including BuffDef (adds) and DebuffDef (subtracts).</summary>
    public float EffectiveDefence =>
        ModifiedStat(Defence + ShieldDefense, SkillEffect.BuffDef, SkillEffect.DebuffDef);

    /// <summary>Magic defence — the divisor for incoming magic damage. Separate
    /// channel from physical defence; sourced from level base + jewels + the Tank
    /// "Anti Magic" passive, plus any BuffMagicDef (e.g. Warchanter's chant).</summary>
    public float EffectiveMagicDefence => ModifiedStat(MagicDefence, SkillEffect.BuffMagicDef);

    /// <summary>Evasion including evasion buffs (flat + percent).</summary>
    public float EffectiveEvasion => ModifiedStat(Evasion, SkillEffect.BuffEvasion);

    /// <summary>Weapon's base attack speed stat (333 = normal). Set from the equipped
    /// weapon type in RecomputeDerived. (Cast speed uses class base × weapon factor
    /// directly in EffectiveCastSpeedMultiplier, so it needs no stored field.)</summary>
    public int WeaponAttackBase { get; set; } = StatCalculator.SpeedBaseline;

    /// <summary>Cast-time multiplier (lower = faster). WIT-driven stat (L2-style
    /// 333 = 1.0x), then skill cast-speed buffs shorten it further.</summary>
    public float EffectiveCastSpeedMultiplier
    {
        get
        {
            // Authentic L2: castSpd = classBase × witModifier × weaponFactor
            //   × gearFactor × ∏(1 + buff%), then time = 333 / castSpd (cap 1999 = 6×).
            // witModifier is EXPONENTIAL (×1.63 per +10 WIT). gearFactor = robe mastery /
            // attributes / passives (CastSpeedMultiplier is their combined TIME multiplier,
            // <1 = faster, so 1/it = speed factor: robe ≈ ×1.4, non-robe ≈ ×0.5). Buffs
            // STACK MULTIPLICATIVELY, matching L2.
            float baseCast = StatCalculator.ClassBaseCastSpeed(BaseClass)
                             * StatCalculator.WeaponCastFactor(WeaponType);
            float witMod = StatCalculator.CastWitModifier(EffectiveWit);
            float gearFactor = 1f / Math.Max(0.05f, CastSpeedMultiplier);
            float buffMult = 1f;
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffCastSpeed)) buffMult *= 1f + buff.Percent(SkillEffect.BuffCastSpeed);
                if (buff.Has(SkillEffect.DebuffCastSpeed)) buffMult *= 1f - buff.Percent(SkillEffect.DebuffCastSpeed);
            }

            float castSpd = baseCast * witMod * gearFactor * buffMult;
            castSpd = Math.Clamp(castSpd, 30f, StatCaps.CastSpeed);
            return StatCalculator.SpeedBaseline / castSpd;   // time multiplier (lower = faster)
        }
    }

    /// <summary>Attack-interval multiplier (lower = faster). DEX-driven stat,
    /// then attack-speed buffs shorten it further.</summary>
    public float EffectiveAttackSpeedMultiplier
    {
        get
        {
            // Authentic L2: atkSpd = weaponBase × dexModifier × gearFactor × ∏(1+buff%),
            // cap 1500. dexModifier is EXPONENTIAL (baseline 30 DEX = 1.0). Buffs stack
            // multiplicatively (matching cast speed).
            float dexFactor = StatCalculator.AttackDexModifier(EffectiveDex);
            float gearFactor = 1f / Math.Max(0.05f, AttackSpeedMultiplier);
            float buffMult = 1f;
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffAtkSpeed)) buffMult *= 1f + buff.Percent(SkillEffect.BuffAtkSpeed);
                if (buff.Has(SkillEffect.DebuffAtkSpeed)) buffMult *= 1f - buff.Percent(SkillEffect.DebuffAtkSpeed);
            }

            float atkSpd = WeaponAttackBase * dexFactor * gearFactor * buffMult;
            atkSpd = Math.Clamp(atkSpd, 30f, StatCaps.AttackSpeed);
            return StatCalculator.SpeedBaseline / atkSpd;    // time multiplier (lower = faster)
        }
    }

    // ----- Combat / skill state ----------------------------------------------------------

    public Guid? CombatTargetId { get; set; }
    public bool Engaged { get; set; }
    public int AttackCooldown { get; set; }

    /// <summary>Threat/aggro table (mobs): attacker entity id → accumulated threat. The mob
    /// targets the highest-threat entity. Taunt spikes it; detaunt drops it.</summary>
    public Dictionary<Guid, float> Threat { get; } = new();
    /// <summary>While &gt; 0 a taunt locks the mob onto its taunter (ignores threat retargeting).</summary>
    public int TauntLockTicks { get; set; }

    public string? QueuedSkillId { get; set; }
    public Guid? QueuedTargetId { get; set; }

    public string? CastingSkillId { get; set; }
    public Guid? CastTargetId { get; set; }
    public int CastTicksRemaining { get; set; }

    /// <summary>MP already charged for the in-progress cast (the initial portion),
    /// so we know what was spent if it's interrupted/cancelled and what remains
    /// to charge on completion.</summary>
    public int CastInitialMpPaid { get; set; }

    public Dictionary<string, int> SkillCooldowns { get; } = new();

    // ----- Potion channel (separate from natural regen; ticks in combat too) ----
    /// <summary>Shared cooldown across ALL potions, in ticks.</summary>
    public int PotionCooldown { get; set; }
    /// <summary>Active heal-over-time potion: rarity decides override priority.</summary>
    public int PotionRarity { get; set; } = -1;       // -1 = none
    public float PotionHealPercentPerSecond { get; set; }
    public int PotionEffectTicks { get; set; }
    public string PotionEffectName { get; set; } = "";

    public bool Dead { get; set; }

    // ----- Mob-only state ----------------------------------------------------------------

    public float HomeX { get; set; }
    public float HomeY { get; set; }

    /// <summary>Spawn zone this mob belongs to (for zone-managed respawn).</summary>
    public string? ZoneId { get; set; }

    /// <summary>Mob template id (MobCatalog) — for drops + quest kill matching.</summary>
    public string? MobTypeId { get; set; }
    /// <summary>Training dummy: immortal (GodMode), stationary, never attacks/aggroes.</summary>
    public bool TrainingDummy { get; set; }
    public MobRank Rank { get; set; }
    public bool Aggressive { get; set; }
    public int WanderTicks { get; set; }
    public int RespawnTicks { get; set; }

    /// <summary>Interest-management cell. Maintained by CellGrid.</summary>
    public (int Cx, int Cy) Cell { get; set; }

    /// <summary>Recomputes everything derived from core stats, level and
    /// equipped items. Call on creation, level-up, equip changes and class
    /// change.</summary>
    public void RecomputeDerived()
    {
        MaxHp = Kind == EntityKind.Player
            ? StatCalculator.MaxHp(Con, Level,
                StatCalculator.HpClassLevelModifier(BaseClass, Archetype),
                StatCalculator.Level1BaseHp(Race, BaseClass))
            : StatCalculator.MobMaxHp(Con, Level);
        // MP now scales with MEN (per-race/class) on a tier curve, like HP. Mobs use
        // a simple level curve.
        MaxMp = Kind == EntityKind.Player
            ? StatCalculator.MaxMp(StatCalculator.BaseMen(Race, BaseClass), Level,
                StatCalculator.MpClassLevelModifier(BaseClass, Archetype),
                StatCalculator.Level1BaseMp(BaseClass))
            : StatCalculator.MobMaxMp(Level);
        AttackPower = StatCalculator.AttackPower(AtkStat, Level);
        MagicAttack = StatCalculator.AttackPower(AtkStat, Level); // mAtk also from ATK
        // Defence (authentic L2): players use armor/jewel-driven base + level²/100,
        // no CON term (armor/jewels/masteries/buffs stack below). Mobs keep the simple
        // curve. Magic def gets the MEN multiplier applied at the very end.
        Defence = Kind == EntityKind.Player
            ? StatCalculator.PhysicalDefenceBase(Level)
            : StatCalculator.MobDefence(Con, Level);
        MagicDefence = (Kind == EntityKind.Player
                ? StatCalculator.MagicDefenceBase(Level)
                : StatCalculator.MobDefence(Con, Level))
            + StatCalculator.ArchetypeMagicDefenceBonus(Archetype, Level);
        // Resolution "sure" floors come from learned passives (Evasion Mastery / Precision /
        // Anti-Magic / Spell Ward), applied in the passive loop below. Base 0 — the
        // universal 5% land/avoid floor lives in the resolver, not here.
        MagicFailFloor = 0f;
        EvadeFloor = 0f;
        HitFloor = 0f;
        Immune = false;
        CooldownReduction = 0f;
        CritRateResist = 0f;
        CritDmgResist = 0f;
        BowResist = 0f;
        MagicFailResist = 0f;
        MeleeVamp = 0f;
        SpellVamp = 0f;
        PveSkillDamageBonus = 0f;
        PveMagicDamageBonus = 0f;
        PveBasicDamageBonus = 0f;
        PvpSkillDamageBonus = 0f;
        PvpMagicDamageBonus = 0f;
        PvpBasicDamageBonus = 0f;
        CancelResist = 0f;
        Accuracy = StatCalculator.Accuracy(EffectiveDex);
        Evasion = StatCalculator.Evasion(EffectiveDex);
        CritChance = StatCalculator.PhysicalCritChance(EffectiveDex);
        MagicCritChance = StatCalculator.MagicCritChance(EffectiveWit);
        InterruptResist = StatCalculator.InterruptResist(EffectiveWit, Level);
        MagicInterruptBonus = StatCalculator.MagicInterruptPower(EffectiveWit);
        BasicAttackInterruptPower = StatCalculator.ArchetypeBasicInterruptPower(Archetype, Level);
        BasicAttackRange = GameConstants.MeleeRange;
        WeaponType = WeaponType.None;
        WeaponHands = WeaponHands.OneHand;
        // Base run speed: players from race+class table, mobs from their spawn-set
        // RunSpeed. Gear/buffs raise it below; EffectiveSpeed clamps to the cap.
        if (Kind == EntityKind.Player)
        {
            RunSpeed = SpeedTable.BaseRunSpeed(Race, BaseClass);
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
        }
        Speed = Kind == EntityKind.Player ? RunSpeed : (RunSpeed > 0 ? RunSpeed : Speed);
        CastSpeedMultiplier = 1f;
        AttackSpeedMultiplier = 1f;

        HasShield = false;
        BlockChance = 0f;
        BlockReduction = 0f;
        ShieldDefense = 0;
        ShieldCritDefense = 0f;
        HpRegenMult = 1f;
        MpRegenMult = 1f;
        ArmorMasteryLabel = "";

        var bodyWeight = ArmorWeight.None;   // equipped BODY-slot armor weight (for masteries)

        foreach (var item in Inventory)
        {
            if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef def)
                continue;

            if (def.Slot == EquipSlot.Armor && def.ArmorSlot == ArmorSlot.Body)
                bodyWeight = def.Weight;

            AttackPower += EnchantRules.BonusAt(def.AtkBonus, item.Enchant);
            MagicAttack += EnchantRules.BonusAt(def.MAtkBonus, item.Enchant);
            Defence += EnchantRules.BonusAt(def.DefBonus, item.Enchant);
            MagicDefence += EnchantRules.BonusAt(def.MDefBonus, item.Enchant);  // jewels
            MaxHp += EnchantRules.BonusAt(def.HpBonus, item.Enchant);
            MaxMp += EnchantRules.BonusAt(def.MpBonus, item.Enchant);
            Evasion += EnchantRules.BonusAt(def.EvaBonus, item.Enchant);

            if (def.Slot == EquipSlot.Weapon)
            {
                WeaponType = def.WeaponType;
                WeaponHands = def.Hands;
            }

            if (def.Slot == EquipSlot.Shield)
            {
                HasShield = true;
                BlockChance = def.BlockChance;
                BlockReduction = def.BlockReduction;
                ShieldDefense += def.ShieldDefense;
                ShieldCritDefense = def.ShieldCritDefense;
                Evasion -= def.ShieldEvasionPenalty;   // shield lowers evasion
            }

            if (def.WeaponRange > 0)
            {
                float range = def.WeaponRange;
                // Archer bow range grows by class-change tier (passives):
                //   tier 1 (1-20): base 400; tier 2 (21-40): +200; tier 3 (40+): +500.
                if (Archetype == Game.Shared.Archetype.Archer)
                {
                    int tier = SkillMath.RangeTier(Level);
                    float bonus = tier >= 3 ? 500f : tier >= 2 ? 200f : 0f;
                    range = Math.Min(GameConstants.MaxBasicAttackRange, range + bonus);
                }
                BasicAttackRange = range;
            }
        }

        // ----- Item attributes (rolled per drop) -----
        float hpPct = 0, mpPct = 0, speedPct = 0, castPct = 0, atkSpeedPct = 0, atkPct = 0, evaPct = 0, defPct = 0;
        float accFlat = 0, hpRegFlat = 0, mpRegFlat = 0, critRatePct = 0, critDmgPct = 0;
        foreach (var item in Inventory)
        {
            if (!item.Equipped) continue;
            foreach (var attr in item.Attributes)
            {
                switch (attr.Type)
                {
                    case AttributeType.HealthPercent: hpPct += attr.Value; break;
                    case AttributeType.ManaPercent: mpPct += attr.Value; break;
                    case AttributeType.SpeedPercent: speedPct += attr.Value; break;
                    case AttributeType.CastSpeedPercent: castPct += attr.Value; break;
                    case AttributeType.AttackSpeedPercent: atkSpeedPct += attr.Value; break;
                    case AttributeType.AttackPercent: atkPct += attr.Value; break;
                    case AttributeType.EvasionPercent: evaPct += attr.Value; break;
                    case AttributeType.DefencePercent: defPct += attr.Value; break;
                    case AttributeType.Accuracy: accFlat += attr.Value; break;
                    case AttributeType.HpRegen: hpRegFlat += attr.Value; break;
                    case AttributeType.MpRegen: mpRegFlat += attr.Value; break;
                    case AttributeType.CritRate: critRatePct += attr.Value; break;
                    case AttributeType.CritDamage: critDmgPct += attr.Value; break;
                }
            }
        }

        MaxHp += (int)(MaxHp * hpPct / 100f);
        MaxMp += (int)(MaxMp * mpPct / 100f);
        AttackPower += (int)(AttackPower * atkPct / 100f);
        MagicAttack += (int)(MagicAttack * atkPct / 100f);
        Evasion += (int)(Evasion * evaPct / 100f);
        Defence += (int)(Defence * defPct / 100f);
        if (Kind == EntityKind.Player)
        {
            RunSpeed = SpeedTable.BaseRunSpeed(Race, BaseClass) * (1f + speedPct / 100f);
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
            Speed = RunSpeed;   // running by default; EffectiveSpeed picks state + clamps
        }
        CastSpeedMultiplier = Math.Max(0.4f, 1f - castPct / 100f);
        AttackSpeedMultiplier = Math.Max(0.4f, 1f - atkSpeedPct / 100f);
        Accuracy += (int)accFlat;
        HpRegenBonus = hpRegFlat;
        MpRegenBonus = mpRegFlat;
        CritDamageBonus = critDmgPct / 100f;   // e.g. 20 -> +0.20x crit multiplier

        // ----- Flat class bonuses (class identity; additive over gear) -----
        if (Kind == EntityKind.Player && SecondClass > 0
            && ClassCatalog.Get(SecondClass)?.Bonus is ClassFlatBonus b)
        {
            MaxHp += b.MaxHp;
            MaxMp += b.MaxMp;
            Defence += b.Defence;
            AttackPower += b.Attack;
            Evasion += b.Evasion;
            Accuracy += b.Accuracy;
            // Primary deltas feed nothing further here (derived already computed),
            // but are exposed for future systems; applied as flat secondary above.
        }

        // 3rd-class discipline lean (stacks on top of the 2nd-class bonus).
        if (Kind == EntityKind.Player && ThirdClass > 0
            && ThirdClassCatalog.Get(ThirdClass)?.Bonus is ClassFlatBonus tb)
        {
            MaxHp += tb.MaxHp;
            MaxMp += tb.MaxMp;
            Defence += tb.Defence;
            AttackPower += tb.Attack;
            Evasion += tb.Evasion;
            Accuracy += tb.Accuracy;
        }

        // ----- Armor set bonus (BODY-DRIVEN): the worn BODY's set grants the bonus when
        // Head/Gloves/Boots are filled with that set's accessory line. This lets the
        // light & robe newbie bodies SHARE one accessory line (each body its own bonus).
        // A classic single-id set (AccessorySetId = "") just matches its own id. -----
        ActiveArmorSet = "";
        if (Kind == EntityKind.Player)
        {
            string bodySet = "", headSet = "", glovesSet = "", bootsSet = "";
            foreach (var item in Inventory)
            {
                if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef sd
                    || sd.Slot != EquipSlot.Armor || string.IsNullOrEmpty(sd.SetId))
                    continue;
                switch (sd.ArmorSlot)
                {
                    case ArmorSlot.Body: bodySet = sd.SetId; break;
                    case ArmorSlot.Head: headSet = sd.SetId; break;
                    case ArmorSlot.Gloves: glovesSet = sd.SetId; break;
                    case ArmorSlot.Boots: bootsSet = sd.SetId; break;
                }
            }
            foreach (var set in ArmorSetCatalog.All)
            {
                string accId = string.IsNullOrEmpty(set.AccessorySetId) ? set.Id : set.AccessorySetId;
                var required = set.RequiredSlots ?? ArmorSetCatalog.DefaultSlots;
                bool complete = true;
                foreach (var slot in required)
                {
                    string worn = slot switch
                    {
                        ArmorSlot.Body => bodySet,
                        ArmorSlot.Head => headSet,
                        ArmorSlot.Gloves => glovesSet,
                        ArmorSlot.Boots => bootsSet,
                        _ => ""
                    };
                    string need = slot == ArmorSlot.Body ? set.Id : accId;
                    if (worn != need) { complete = false; break; }
                }
                if (complete)
                {
                    MaxHp += set.Bonus.MaxHp;
                    MaxMp += set.Bonus.MaxMp;
                    Defence += set.Bonus.Defence;
                    AttackPower += set.Bonus.Attack;
                    MagicAttack += set.Bonus.Attack;   // set Attack feeds both channels
                    Evasion += set.Bonus.Evasion;
                    Accuracy += set.Bonus.Accuracy;
                    // Optional PERCENT set bonuses (e.g. newbie light +2% P.Def, robe +15% cast).
                    if (set.DefencePct != 0f) Defence += (int)(Defence * set.DefencePct);
                    if (set.CastSpeedPct != 0f)
                        CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier * (1f - set.CastSpeedPct), 0.4f, 2.5f);
                    ActiveArmorSet = set.Name;
                    break;
                }
            }
        }

        // Archetype identity: scale basic-attack power, add crit/eva for
        // archers & rogues. Skills keep using full AttackPower.
        var arch = Archetype;
        BasicAttackPower = Math.Max(1,
            (int)(AttackPower * StatCalculator.BasicAttackMultiplier(arch)));
        // Weapon shapes crit: blunt low, dual/bow high (WeaponType known post-equip).
        // Factor scales the DEX crit; archetype bonus and GEAR crit-rate add on top
        // (gear crit isn't scaled by the weapon factor). Blunt's low crit is offset
        // by its accuracy bonus.
        CritChance = Math.Clamp(
            CritChance * StatCalculator.WeaponCritFactor(WeaponType)
            + StatCalculator.ArchetypeCritBonus(arch)
            + critRatePct / 100f, 0f, 0.75f);
        Evasion += StatCalculator.ArchetypeEvasionBonus(arch, Level);
        Accuracy += StatCalculator.WeaponAccuracyBonus(WeaponType);

        // Skill-buff Max HP/MP (e.g. HP Boost line, Frenzy): flat add and/or % of max.
        float buffHpPct = 0f, buffMpPct = 0f, buffHpFlat = 0f, buffMpFlat = 0f;
        foreach (var buff in Buffs)
        {
            if (buff.Has(SkillEffect.BuffHp)) { buffHpPct += buff.Percent(SkillEffect.BuffHp); buffHpFlat += buff.Flat(SkillEffect.BuffHp); }
            if (buff.Has(SkillEffect.BuffMp)) { buffMpPct += buff.Percent(SkillEffect.BuffMp); buffMpFlat += buff.Flat(SkillEffect.BuffMp); }
        }
        MaxHp = (int)((MaxHp + buffHpFlat) * (1f + buffHpPct));
        MaxMp = (int)((MaxMp + buffMpFlat) * (1f + buffMpPct));

        WeaponAttackBase = StatCalculator.WeaponAttackBaseSpeed(WeaponType);

        // ----- Shield Mastery buffs (tank passives) scale the shield values.
        //  Percent magnitudes add fractionally; flat add directly. Only matter
        //  when a shield is equipped, so a mage's buffed shield is still weak. ---
        if (HasShield)
        {
            foreach (var buff in Buffs)
            {
                if (buff.Has(SkillEffect.BuffBlockChance))
                {
                    BlockChance += buff.Flat(SkillEffect.BuffBlockChance);
                    BlockChance *= 1f + buff.Percent(SkillEffect.BuffBlockChance);
                }
                if (buff.Has(SkillEffect.BuffShieldDef))
                {
                    ShieldDefense += (int)buff.Flat(SkillEffect.BuffShieldDef);
                    ShieldDefense = (int)(ShieldDefense * (1f + buff.Percent(SkillEffect.BuffShieldDef)));
                    BlockReduction += buff.Percent(SkillEffect.BuffShieldDef) * 0.2f;
                }
            }
            BlockChance = Math.Clamp(BlockChance, 0f, StatCaps.BlockChance);
            BlockReduction = Math.Clamp(BlockReduction, 0f, StatCaps.BlockReduction);
        }

        // ----- Armor-weight MASTERY (final layer): bonus for the trained weight,
        // penalty for an untrained heavy/light body. Robe never penalises; tanks &
        // warriors are immune. Speed factors (>1 faster) divide the TIME multipliers. ---
        if (Kind == EntityKind.Player)
        {
            // A learned DATA-DRIVEN armor mastery (a skill carrying per-weight MasteryEffects)
            // REPLACES the hardcoded ArmorMastery table for this entity. Same pattern future
            // classes reuse for weapon-type-conditional passives.
            MasteryEffect mEff = ArmorMastery.Neutral;
            string mLabel;
            bool dataMastery = false;
            foreach (var (skillId, skillLevel) in LearnedSkills)
            {
                if (SkillCatalog.Get(skillId)?.ArmorMasteryAt(skillLevel) is not ArmorMasteryProfile prof)
                    continue;
                mEff = bodyWeight switch
                {
                    ArmorWeight.Robe  => prof.Robe,
                    ArmorWeight.Light => prof.Light,
                    ArmorWeight.Heavy => prof.Heavy,
                    _ => ArmorMastery.Neutral,
                };
                dataMastery = true;
                break;
            }
            if (dataMastery)
                mLabel = bodyWeight == ArmorWeight.None ? "Armor Mastery" : $"Armor Mastery ({bodyWeight})";
            else
                (mEff, mLabel) = ArmorMastery.Resolve(BaseClass, Archetype, bodyWeight, Level,
                    w => LearnedSkills.ContainsKey(ArmorMastery.SkillIdFor(w)));
            ArmorMasteryLabel = mLabel;

            AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier / mEff.AtkSpeed, 0.4f, 2.5f);
            CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier / mEff.CastSpeed, 0.4f, 2.5f);
            RunSpeed *= mEff.MoveSpeed;
            WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor;
            Speed = RunSpeed;
            HpRegenMult = mEff.HpRegen;
            MpRegenMult = mEff.MpRegen;
            MaxHp = (int)((MaxHp + mEff.MaxHpFlat) * mEff.MaxHp);
            MaxMp = (int)((MaxMp + mEff.MaxMpFlat) * mEff.MaxMp);
            Evasion += mEff.Evasion;
            Accuracy += mEff.Accuracy;
            Defence += mEff.Defence + (int)(Level * mEff.DefPerLevel);
            MagicDefence += mEff.MagicDefence + (int)(Level * mEff.MagicDefPerLevel);
            if (mEff.DefenceMult != 1f) Defence = (int)(Defence * mEff.DefenceMult);
            if (mEff.MagicDefenceMult != 1f) MagicDefence = (int)(MagicDefence * mEff.MagicDefenceMult);
            InterruptResist += mEff.InterruptResist + (int)(Level * mEff.InterruptResistPerLevel);
            if (mEff.CritRate != 0f) CritChance = Math.Clamp(CritChance + mEff.CritRate, 0f, 0.75f);
            CritDamageBonus += mEff.CritDamage;
            CritDmgResist += mEff.CritDmgResist;
            CritRateResist += mEff.CritRateResist;
            BowResist += mEff.BowResist;

            // A learned skill can SUPERSEDE another's passive via Replaces[] (e.g. Spell
            // Mastery replaces Weapon Mastery): collect those ids so the base passive
            // doesn't double-apply. (Non-passive replaced skills are harmless no-ops here.)
            var replacedPassives = new HashSet<string>();
            foreach (var (skillId, _) in LearnedSkills)
                if (SkillCatalog.Get(skillId)?.Replaces is { } rep)
                    foreach (var r in rep) replacedPassives.Add(r);

            // Fold one PassiveEffect into the derived stats. Shared by the always-on
            // discipline passives AND the weapon-conditional masteries below (which pass
            // the profile entry for the currently-held weapon). An all-zero pe is inert.
            void ApplyPassive(PassiveEffect pe)
            {
                MaxHp += pe.MaxHp + (int)(MaxHp * pe.MaxHpPct);
                MaxMp += pe.MaxMp + (int)(MaxMp * pe.MaxMpPct);
                Defence += pe.Defence;
                MagicDefence += pe.MagicDefence;
                if (pe.DefencePct != 0f) Defence = (int)(Defence * (1f + pe.DefencePct));
                if (pe.MagicDefencePct != 0f) MagicDefence = (int)(MagicDefence * (1f + pe.MagicDefencePct));
                AttackPower += pe.Attack + (int)(AttackPower * (pe.AttackPct + pe.PhysAtkPct)) + pe.PhysAtk;
                MagicAttack += pe.Attack + (int)(MagicAttack * (pe.AttackPct + pe.MagAtkPct)) + pe.MagAtk;
                Evasion += pe.Evasion;
                Accuracy += pe.Accuracy;
                if (pe.CritRate != 0f) CritChance = Math.Clamp(CritChance + pe.CritRate, 0f, 0.75f);
                CritDamageBonus += pe.CritDamage;
                if (pe.MagicCritRate != 0f) MagicCritChance = Math.Clamp(MagicCritChance + pe.MagicCritRate, 0f, 0.5f);
                HpRegenBonus += pe.HpRegen;
                MpRegenBonus += pe.MpRegen;
                if (pe.HpRegenPct != 0f) HpRegenMult *= 1f + pe.HpRegenPct;
                if (pe.MpRegenPct != 0f) MpRegenMult *= 1f + pe.MpRegenPct;
                if (pe.AtkSpeedPct != 0f) AttackSpeedMultiplier = Math.Clamp(AttackSpeedMultiplier * (1f - pe.AtkSpeedPct), 0.4f, 2.5f);
                if (pe.CastSpeedPct != 0f) CastSpeedMultiplier = Math.Clamp(CastSpeedMultiplier * (1f - pe.CastSpeedPct), 0.4f, 2.5f);
                if (pe.MoveSpeedPct != 0f) { RunSpeed *= 1f + pe.MoveSpeedPct; WalkSpeed = RunSpeed * MovementTuning.WalkSpeedFactor; Speed = RunSpeed; }
                CooldownReduction += pe.CooldownPct;
                CritRateResist += pe.CritRateResist;
                CritDmgResist += pe.CritDmgResist;
                BowResist += pe.BowResist;
                // Bow range bonus applies only while a bow is equipped (rogue/archer mastery).
                if (pe.BowRange != 0f && WeaponType == WeaponType.Bow)
                    BasicAttackRange = Math.Min(GameConstants.MaxBasicAttackRange, BasicAttackRange + pe.BowRange);
                // Shield passive (only with a shield equipped): scale block chance / shield def.
                if (HasShield)
                {
                    if (pe.BlockChancePct != 0f) BlockChance *= 1f + pe.BlockChancePct;
                    if (pe.ShieldDefPct != 0f)
                    {
                        ShieldDefense = (int)(ShieldDefense * (1f + pe.ShieldDefPct));
                        BlockReduction += pe.ShieldDefPct * 0.2f;
                    }
                }
                MagicFailResist += pe.MagicFailResist;
                MagicInterruptBonus += pe.InterruptPower;
                InterruptResist += pe.InterruptResist;
                MeleeVamp += pe.MeleeVamp;
                SpellVamp += pe.SpellVamp;
                PveSkillDamageBonus += pe.PveSkillDamagePct;
                PveMagicDamageBonus += pe.PveMagicDamagePct;
                PveBasicDamageBonus += pe.PveBasicDamagePct;
                PvpSkillDamageBonus += pe.PvpSkillDamagePct;
                PvpMagicDamageBonus += pe.PvpMagicDamagePct;
                PvpBasicDamageBonus += pe.PvpBasicDamagePct;
                CancelResist += pe.CancelResistPct;
                // Resolution floors are GUARANTEES — take the strongest (max), never sum.
                EvadeFloor = Math.Max(EvadeFloor, pe.EvadeFloor);
                HitFloor = Math.Max(HitFloor, pe.HitFloor);
                MagicFailFloor = Math.Max(MagicFailFloor, pe.MagicFailFloor);
            }

            // ----- Learnable PASSIVES (discipline passives, weapon masteries): each learned
            // skill whose SkillDef carries a PassiveEffect applies it, on top of everything.
            // A weapon mastery applies the entry for the currently-equipped weapon type. -----
            foreach (var (skillId, skillLevel) in LearnedSkills)
            {
                if (replacedPassives.Contains(skillId)) continue;
                var sd = SkillCatalog.Get(skillId);
                if (sd is null) continue;
                if (sd.PassiveAt(skillLevel) is PassiveEffect pe) ApplyPassive(pe);
                if (sd.WeaponMasteryAt(skillLevel) is WeaponMasteryProfile wm)
                    ApplyPassive(wm.For(WeaponType, WeaponHands));
            }
            // (The combat-training attack bonus is now a normal LEVELED passive — its
            // per-level AttackPct flows through the loop above, no special-casing.)

            // Shield-passive scaling above can push block over caps — re-clamp.
            if (HasShield)
            {
                BlockChance = Math.Clamp(BlockChance, 0f, StatCaps.BlockChance);
                BlockReduction = Math.Clamp(BlockReduction, 0f, StatCaps.BlockReduction);
            }
        }

        // MEN multiplies the whole flat magic-defence pool (base + jewels + passives),
        // per the L2 M.Def formula. We have no MEN stat, so use the per-race/class base.
        // (Buffs apply on top in EffectiveMagicDefence.) Players only; mob mDef is
        // overwritten at spawn.
        if (Kind == EntityKind.Player)
            MagicDefence = (int)(MagicDefence *
                StatCalculator.MenModifier(StatCalculator.BaseMen(Race, BaseClass)));

        // ----- Timed-buff contributions to BAKED stats (the stats computed once here;
        // atk/def/speed read buffs live in their Effective* getters instead). Re-folded on
        // every buff apply/expire because both ApplyBuff and TickBuffs call this. Fraction
        // effects accept either Flat or Percent magnitudes (summed as a fraction). -----
        foreach (var buff in Buffs)
        {
            if (buff.Has(SkillEffect.BuffAccuracy)) Accuracy += (int)buff.Flat(SkillEffect.BuffAccuracy);
            if (buff.Has(SkillEffect.BuffCritRate))
                CritChance = (CritChance + buff.Flat(SkillEffect.BuffCritRate)) * (1f + buff.Percent(SkillEffect.BuffCritRate));
            if (buff.Has(SkillEffect.BuffMagicCritRate))
                MagicCritChance = (MagicCritChance + buff.Flat(SkillEffect.BuffMagicCritRate)) * (1f + buff.Percent(SkillEffect.BuffMagicCritRate));
            if (buff.Has(SkillEffect.BuffCritDamage))
                CritDamageBonus += buff.Flat(SkillEffect.BuffCritDamage) + buff.Percent(SkillEffect.BuffCritDamage);
            if (buff.Has(SkillEffect.BuffCritRateResist)) CritRateResist += buff.Flat(SkillEffect.BuffCritRateResist) + buff.Percent(SkillEffect.BuffCritRateResist);
            if (buff.Has(SkillEffect.BuffCritDmgResist)) CritDmgResist += buff.Flat(SkillEffect.BuffCritDmgResist) + buff.Percent(SkillEffect.BuffCritDmgResist);
            if (buff.Has(SkillEffect.BuffBowResist)) BowResist += buff.Flat(SkillEffect.BuffBowResist) + buff.Percent(SkillEffect.BuffBowResist);
            if (buff.Has(SkillEffect.BuffMagicFailResist)) MagicFailResist += buff.Flat(SkillEffect.BuffMagicFailResist) + buff.Percent(SkillEffect.BuffMagicFailResist);
            if (buff.Has(SkillEffect.BuffMeleeVamp)) MeleeVamp += buff.Flat(SkillEffect.BuffMeleeVamp) + buff.Percent(SkillEffect.BuffMeleeVamp);
            if (buff.Has(SkillEffect.BuffSpellVamp)) SpellVamp += buff.Flat(SkillEffect.BuffSpellVamp) + buff.Percent(SkillEffect.BuffSpellVamp);
            if (buff.Has(SkillEffect.BuffCooldown)) CooldownReduction += buff.Flat(SkillEffect.BuffCooldown) + buff.Percent(SkillEffect.BuffCooldown);
            if (buff.Has(SkillEffect.BuffPveSkillDamage)) PveSkillDamageBonus += buff.Flat(SkillEffect.BuffPveSkillDamage) + buff.Percent(SkillEffect.BuffPveSkillDamage);
            if (buff.Has(SkillEffect.BuffPveMagicDamage)) PveMagicDamageBonus += buff.Flat(SkillEffect.BuffPveMagicDamage) + buff.Percent(SkillEffect.BuffPveMagicDamage);
            if (buff.Has(SkillEffect.BuffPveBasicDamage)) PveBasicDamageBonus += buff.Flat(SkillEffect.BuffPveBasicDamage) + buff.Percent(SkillEffect.BuffPveBasicDamage);
            if (buff.Has(SkillEffect.BuffPvpSkillDamage)) PvpSkillDamageBonus += buff.Flat(SkillEffect.BuffPvpSkillDamage) + buff.Percent(SkillEffect.BuffPvpSkillDamage);
            if (buff.Has(SkillEffect.BuffPvpMagicDamage)) PvpMagicDamageBonus += buff.Flat(SkillEffect.BuffPvpMagicDamage) + buff.Percent(SkillEffect.BuffPvpMagicDamage);
            if (buff.Has(SkillEffect.BuffPvpBasicDamage)) PvpBasicDamageBonus += buff.Flat(SkillEffect.BuffPvpBasicDamage) + buff.Percent(SkillEffect.BuffPvpBasicDamage);
            if (buff.Has(SkillEffect.BuffCancelResist)) CancelResist += buff.Flat(SkillEffect.BuffCancelResist) + buff.Percent(SkillEffect.BuffCancelResist);
            if (buff.Has(SkillEffect.BuffInterruptPower)) MagicInterruptBonus += (int)buff.Flat(SkillEffect.BuffInterruptPower);
            if (buff.Has(SkillEffect.BuffInterruptResist)) InterruptResist += (int)buff.Flat(SkillEffect.BuffInterruptResist);
            if (buff.Has(SkillEffect.BuffMagicFailFloor))
                MagicFailFloor = Math.Max(MagicFailFloor, buff.Flat(SkillEffect.BuffMagicFailFloor) + buff.Percent(SkillEffect.BuffMagicFailFloor));
        }
        // Clamp the buff-touched fractions to sane ranges.
        CritChance = Math.Clamp(CritChance, 0f, 0.75f);
        MagicCritChance = Math.Clamp(MagicCritChance, 0f, 0.5f);
        CritRateResist = Math.Clamp(CritRateResist, 0f, 1f);
        CritDmgResist = Math.Clamp(CritDmgResist, 0f, 0.9f);
        BowResist = Math.Clamp(BowResist, 0f, 0.9f);
        MagicFailResist = Math.Clamp(MagicFailResist, 0f, 0.9f);
        CooldownReduction = Math.Clamp(CooldownReduction, 0f, 0.8f);
        MeleeVamp = Math.Clamp(MeleeVamp, 0f, 1f);
        SpellVamp = Math.Clamp(SpellVamp, 0f, 1f);

        Hp = Math.Min(Hp, MaxHp);
        Mp = Math.Min(Mp, MaxMp);
    }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level,
            Hp, MaxHp, Mp, MaxMp, SecondClass, ThirdClass, Dead);
}
