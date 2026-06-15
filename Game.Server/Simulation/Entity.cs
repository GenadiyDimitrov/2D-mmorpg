using Game.Shared;

namespace Game.Server.Simulation;

/// <summary>A timed stat modifier (buff or debuff) on an entity.</summary>
public class BuffInstance
{
    public required SkillEffect Type { get; init; }
    public required float Magnitude { get; init; }
    public int TicksRemaining { get; set; }
    public required string Name { get; init; }
}

/// <summary>One item instance in a player's inventory.</summary>
public class InventoryItem
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public required int DefId { get; init; }
    public bool Equipped { get; set; }

    public InventoryItemDto ToDto() => new(InstanceId, DefId, Equipped);
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
    public int AttackPower { get; set; }
    public int Defence { get; set; }
    public int Accuracy { get; set; }
    public int Evasion { get; set; }
    public float CritChance { get; set; }
    public float BasicAttackRange { get; set; } = GameConstants.MeleeRange;

    public long Exp { get; set; }

    // ----- Inventory (players only) ----------------------------------------------

    public List<InventoryItem> Inventory { get; } = new();

    // ----- Buffs / debuffs ------------------------------------------------------------

    public List<BuffInstance> Buffs { get; } = new();

    public float EffectiveAttack
    {
        get
        {
            float multiplier = 1f;
            foreach (var buff in Buffs)
                if (buff.Type == SkillEffect.BuffAtk)
                    multiplier += buff.Magnitude;
            return AttackPower * multiplier;
        }
    }

    public float EffectiveDefence
    {
        get
        {
            float multiplier = 1f;
            foreach (var buff in Buffs)
            {
                if (buff.Type == SkillEffect.BuffDef)
                    multiplier += buff.Magnitude;
                else if (buff.Type == SkillEffect.DebuffDef)
                    multiplier -= buff.Magnitude;
            }
            return Defence * Math.Max(0f, multiplier);
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

        foreach (var item in Inventory)
        {
            if (!item.Equipped || ItemCatalog.Get(item.DefId) is not ItemDef def)
                continue;

            AttackPower += def.AtkBonus;
            Defence += def.DefBonus;
            MaxHp += def.HpBonus;
            MaxMp += def.MpBonus;
            Evasion += def.EvaBonus;

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

        Hp = Math.Min(Hp, MaxHp);
        Mp = Math.Min(Mp, MaxMp);
    }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level,
            Hp, MaxHp, Mp, MaxMp, SecondClass, Dead);
}
