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

/// <summary>
/// Live server-side state of one thing in the world.
/// Lives only in memory — persistence (EF Core snapshots) is a later phase.
/// Mutated exclusively by the game-loop thread.
/// </summary>
public class Entity
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required EntityKind Kind { get; init; }

    public Race Race { get; init; }
    public BaseClass BaseClass { get; init; }

    public float X { get; set; }
    public float Y { get; set; }

    /// <summary>Click-to-move destination. Null = standing still.</summary>
    public float? TargetX { get; set; }
    public float? TargetY { get; set; }

    public float Speed { get; set; } = GameConstants.BasePlayerSpeed;

    // ----- Core stats (CON/ATK/WIT/DEX per the design doc) ------------------

    public int Con { get; set; }
    public int AtkStat { get; set; }
    public int Wit { get; set; }
    public int Dex { get; set; }

    // ----- Derived stats (recomputed on level-up) ----------------------------

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

    public long Exp { get; set; }

    // ----- Buffs / debuffs -------------------------------------------------------

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
                if (buff.Type == SkillEffect.DebuffDef)
                    multiplier -= buff.Magnitude;
            return Defence * Math.Max(0f, multiplier);
        }
    }

    // ----- Combat / skill state -----------------------------------------------------

    /// <summary>Who this entity is trying to attack. Null = peaceful.</summary>
    public Guid? CombatTargetId { get; set; }

    /// <summary>True while actively chasing/auto-attacking the combat target.</summary>
    public bool Engaged { get; set; }

    /// <summary>Ticks until the next basic attack is allowed.</summary>
    public int AttackCooldown { get; set; }

    /// <summary>Skill waiting for the entity to get into range.</summary>
    public int? QueuedSkillId { get; set; }
    public Guid? QueuedTargetId { get; set; }

    /// <summary>Skill currently being cast (wind-up).</summary>
    public int? CastingSkillId { get; set; }
    public Guid? CastTargetId { get; set; }
    public int CastTicksRemaining { get; set; }

    /// <summary>skillId -> ticks until ready again.</summary>
    public Dictionary<int, int> SkillCooldowns { get; } = new();

    public bool Dead { get; set; }

    // ----- Mob-only state ------------------------------------------------------------

    /// <summary>Spawn point; mobs leash and respawn here.</summary>
    public float HomeX { get; set; }
    public float HomeY { get; set; }

    /// <summary>Attacks players on sight within MobAggroRange.</summary>
    public bool Aggressive { get; set; }

    /// <summary>Mob AI: ticks until the next wander decision.</summary>
    public int WanderTicks { get; set; }

    /// <summary>Dead mob: ticks until respawn.</summary>
    public int RespawnTicks { get; set; }

    /// <summary>Interest-management cell. Maintained by CellGrid.</summary>
    public (int Cx, int Cy) Cell { get; set; }

    /// <summary>Recomputes everything derived from core stats + level.
    /// Call on creation and on every level-up.</summary>
    public void RecomputeDerived()
    {
        MaxHp = StatCalculator.MaxHp(Con, Level);
        MaxMp = StatCalculator.MaxMp(Wit, Level);
        AttackPower = StatCalculator.AttackPower(AtkStat, Level);
        Defence = StatCalculator.Defence(Con, Level);
        Accuracy = StatCalculator.Accuracy(Dex, Level);
        Evasion = StatCalculator.Evasion(Dex, Level);
        CritChance = StatCalculator.CritChance(Dex);
    }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level,
            Hp, MaxHp, Mp, MaxMp, Dead);
}
