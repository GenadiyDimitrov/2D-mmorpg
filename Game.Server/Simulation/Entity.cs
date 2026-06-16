using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A timed stat modifier (buff or debuff) on an entity. Carries a
/// flags Effect (one buff can touch several stats) and a per-effect magnitude
/// array with flat/percent modes. Identified by Key; same-Key buffs compare by
/// Rank; a buff also unconditionally removes any active buff in Replaces.</summary>
public class BuffInstance
{
    public required SkillEffect Effect { get; init; }
    public required EffectMagnitude[] Magnitudes { get; init; }
    public int TicksRemaining { get; set; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";

    public string Key { get; init; } = "";
    public int Rank { get; init; }
    public string[] Replaces { get; init; } = Array.Empty<string>();

    public bool Has(SkillEffect flag) => (Effect & flag) != 0;

    public bool IsDebuff => Has(SkillEffect.DebuffDef);

    /// <summary>Sum of this buff's flat entries for an effect.</summary>
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

    public Race Race { get; init; }
    public BaseClass BaseClass { get; init; }

    /// <summary>DB character id (null for mobs / unsaved).</summary>
    public int? PersistentId { get; set; }

    /// <summary>Account-level admin flag (elevated commands, god mode).</summary>
    public bool IsAdmin { get; set; }

    /// <summary>God mode: takes no damage (admin only).</summary>
    public bool GodMode { get; set; }

    /// <summary>Jailed players are teleported to jail and cannot move out.</summary>
    public bool Jailed { get; set; }

    /// <summary>0 = none; otherwise a ClassCatalog id.</summary>
    public int SecondClass { get; set; }

    public Archetype? Archetype =>
        SecondClass > 0 ? ClassCatalog.Get(SecondClass)?.Archetype : null;

    public float X { get; set; }
    public float Y { get; set; }

    public float? TargetX { get; set; }
    public float? TargetY { get; set; }

    public float Speed { get; set; } = GameConstants.BasePlayerSpeed;

    // ----- Core stats (CON/ATK/WIT/DEX) --------------------------------------

    public int Con { get; set; }
    public int AtkStat { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }

    // ----- Derived stats (recomputed on level-up / equip / class change) -------

    public int Level { get; set; } = 1;
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }
    public int AttackPower { get; set; }      // feeds SKILLS
    public int BasicAttackPower { get; set; } // feeds auto-attacks (archetype-scaled)
    public int Defence { get; set; }
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public float CritChance { get; set; }
    public float BasicAttackRange { get; set; } = GameConstants.MeleeRange;

    /// <summary>Cast-time multiplier from item Cast Speed attributes (0.8 = 20% faster).</summary>
    public float CastSpeedMultiplier { get; set; } = 1f;
    /// <summary>Attack-interval multiplier from Attack Speed attributes.</summary>
    public float AttackSpeedMultiplier { get; set; } = 1f;

    public long Exp { get; set; }

    // ----- Inventory (players only) ----------------------------------------------

    public List<InventoryItem> Inventory { get; } = new();

    // ----- Buffs / debuffs ------------------------------------------------------------

    public List<BuffInstance> Buffs { get; } = new();

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

    public float EffectiveAttack => ModifiedStat(AttackPower, SkillEffect.BuffAtk);

    /// <summary>Buffed attack power for BASIC attacks (archetype-scaled).</summary>
    public float EffectiveBasicAttack => ModifiedStat(BasicAttackPower, SkillEffect.BuffAtk);

    /// <summary>Move speed including move-speed buffs (flat + percent).</summary>
    public float EffectiveSpeed => ModifiedStat(Speed, SkillEffect.BuffMoveSpeed);

    /// <summary>Defence including BuffDef (adds) and DebuffDef (subtracts).</summary>
    public float EffectiveDefence =>
        ModifiedStat(Defence, SkillEffect.BuffDef, SkillEffect.DebuffDef);

    /// <summary>Evasion including evasion buffs (flat + percent).</summary>
    public float EffectiveEvasion => ModifiedStat(Evasion, SkillEffect.BuffEvasion);

    /// <summary>Cast-speed buff multiplier (1 = none; 0.8 = 20% faster).
    /// Combines item cast-speed (CastSpeedMultiplier) with skill buffs.</summary>
    public float EffectiveCastSpeedMultiplier
    {
        get
        {
            float pct = 0f, flat = 0f;
            foreach (var buff in Buffs)
                if (buff.Has(SkillEffect.BuffCastSpeed))
                {
                    pct += buff.Percent(SkillEffect.BuffCastSpeed);
                    flat += buff.Flat(SkillEffect.BuffCastSpeed);
                }
            // Buff reduces cast time; combine with item multiplier.
            float buffMul = Math.Max(0.3f, 1f - pct) ;
            return CastSpeedMultiplier * buffMul;
        }
    }

    /// <summary>Attack-speed buff multiplier from skills, combined with items.</summary>
    public float EffectiveAttackSpeedMultiplier
    {
        get
        {
            float pct = 0f;
            foreach (var buff in Buffs)
                if (buff.Has(SkillEffect.BuffAtkSpeed))
                    pct += buff.Percent(SkillEffect.BuffAtkSpeed);
            float buffMul = Math.Max(0.3f, 1f - pct);
            return AttackSpeedMultiplier * buffMul;
        }
    }

    // ----- Combat / skill state ----------------------------------------------------------

    public Guid? CombatTargetId { get; set; }
    public bool Engaged { get; set; }
    public int AttackCooldown { get; set; }

    public int? QueuedSkillId { get; set; }
    public Guid? QueuedTargetId { get; set; }

    public int? CastingSkillId { get; set; }
    public Guid? CastTargetId { get; set; }
    public int CastTicksRemaining { get; set; }

    public Dictionary<int, int> SkillCooldowns { get; } = new();

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
        MaxHp = StatCalculator.MaxHp(Con, Level);
        MaxMp = StatCalculator.MaxMp(Wit, Level);
        AttackPower = StatCalculator.AttackPower(AtkStat, Level);
        Defence = StatCalculator.Defence(Con, Level);
        Accuracy = StatCalculator.Accuracy(Dex, Level);
        Evasion = StatCalculator.Evasion(Dex, Level);
        CritChance = StatCalculator.CritChance(Dex);
        BasicAttackRange = GameConstants.MeleeRange;
        Speed = GameConstants.BasePlayerSpeed;
        CastSpeedMultiplier = 1f;
        AttackSpeedMultiplier = 1f;

        foreach (var item in Inventory)
        {
            if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef def)
                continue;

            AttackPower += EnchantRules.BonusAt(def.AtkBonus, item.Enchant);
            Defence += EnchantRules.BonusAt(def.DefBonus, item.Enchant);
            MaxHp += EnchantRules.BonusAt(def.HpBonus, item.Enchant);
            MaxMp += EnchantRules.BonusAt(def.MpBonus, item.Enchant);
            Evasion += EnchantRules.BonusAt(def.EvaBonus, item.Enchant);

            if (def.WeaponRange > 0)
            {
                float range = def.WeaponRange;
                // Archer second classes shoot further (cap per design doc).
                if (Archetype == Game.Shared.Archetype.Archer)
                    range = Math.Min(GameConstants.MaxBasicAttackRange,
                        range + GameConstants.ArcherRangeBonus);
                BasicAttackRange = range;
            }
        }

        // ----- Item attributes (rolled per drop) -----
        float hpPct = 0, mpPct = 0, speedPct = 0, castPct = 0, atkSpeedPct = 0, atkPct = 0;
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
                }
            }
        }

        MaxHp += (int)(MaxHp * hpPct / 100f);
        MaxMp += (int)(MaxMp * mpPct / 100f);
        AttackPower += (int)(AttackPower * atkPct / 100f);
        Speed = GameConstants.BasePlayerSpeed * (1f + speedPct / 100f);
        CastSpeedMultiplier = Math.Max(0.4f, 1f - castPct / 100f);
        AttackSpeedMultiplier = Math.Max(0.4f, 1f - atkSpeedPct / 100f);

        // Archetype identity: scale basic-attack power, add crit/eva for
        // archers & rogues. Skills keep using full AttackPower.
        var arch = Archetype;
        BasicAttackPower = Math.Max(1,
            (int)(AttackPower * StatCalculator.BasicAttackMultiplier(arch)));
        CritChance = Math.Clamp(
            CritChance + StatCalculator.ArchetypeCritBonus(arch), 0f, 0.75f);
        Evasion += StatCalculator.ArchetypeEvasionBonus(arch, Level);

        Hp = Math.Min(Hp, MaxHp);
        Mp = Math.Min(Mp, MaxMp);
    }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level,
            Hp, MaxHp, Mp, MaxMp, SecondClass, Dead);
}
