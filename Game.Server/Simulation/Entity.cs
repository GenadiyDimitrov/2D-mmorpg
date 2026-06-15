using Game.Shared;

namespace Game.Server.Simulation;

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

    // ----- Combat state -------------------------------------------------------

    /// <summary>Who this entity is trying to attack. Null = peaceful.</summary>
    public Guid? CombatTargetId { get; set; }

    /// <summary>True while actively chasing/attacking the combat target.</summary>
    public bool Engaged { get; set; }

    /// <summary>Ticks until the next basic attack is allowed.</summary>
    public int AttackCooldown { get; set; }

    public bool Dead { get; set; }

    // ----- Mob-only state ------------------------------------------------------

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
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level, Hp, MaxHp, Dead);
}
