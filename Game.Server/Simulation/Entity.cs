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

    public int Level { get; set; } = 1;
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }

    /// <summary>Current interest-management cell. Maintained by CellGrid.</summary>
    public (int Cx, int Cy) Cell { get; set; }

    /// <summary>Mob AI: ticks until the next wander decision.</summary>
    public int WanderTicks { get; set; }

    public EntityDto ToDto() =>
        new(Id, Name, Kind, Race, BaseClass, X, Y, Speed, Level, Hp, MaxHp);
}
