namespace Game.Shared;

/// <summary>
/// THE editable world layout. Everything about where things are lives here so
/// the server (spawning, collision) and the client (drawing zones/paths/border)
/// agree on one source of truth. To reshape the world, edit the lists below.
/// </summary>
public static class WorldMap
{
    /// <summary>The playable rectangle. Players cannot leave it; the client
    /// draws its outline so the edge is visible instead of an invisible wall.</summary>
    public static readonly WorldBorder Border = new(
        MinX: 0, MinY: 0,
        MaxX: GameConstants.ZoneWidth, MaxY: GameConstants.ZoneHeight);

    /// <summary>
    /// Mob spawn zones. Each is a circle at (X, Y) with a Radius that spawns
    /// MobCount mobs of the given types between MinLevel and MaxLevel.
    ///
    /// To add a zone — e.g. "at (1000,1000) radius 800, level 5-7 boars and
    /// spiders" — just add a SpawnZone line. Order doesn't matter; the server
    /// spawns each independently and the client tints each one.
    /// </summary>
    public static readonly SpawnZone[] SpawnZones =
    {
        // --- Starter ring near town (town center is the map middle) ---
        new(X: 6000,  Y: 4000,  Radius: 1400, MinLevel: 1,  MaxLevel: 3,
            MobTypes: new[] { "Wolf", "Boar" },            MobCount: 10),
        new(X: 9000,  Y: 4000,  Radius: 1400, MinLevel: 1,  MaxLevel: 3,
            MobTypes: new[] { "Slime", "Boar" },           MobCount: 10),

        // --- Mid-level fields ---
        new(X: 3000,  Y: 7000,  Radius: 1600, MinLevel: 4,  MaxLevel: 7,
            MobTypes: new[] { "Boar", "Spider" },          MobCount: 12),
        new(X: 12000, Y: 7000,  Radius: 1600, MinLevel: 4,  MaxLevel: 7,
            MobTypes: new[] { "Wolf", "Spider" },          MobCount: 12),

        // --- Higher-level, further out ---
        new(X: 2500,  Y: 11000, Radius: 1800, MinLevel: 8,  MaxLevel: 12,
            MobTypes: new[] { "Spider", "Bandit" },        MobCount: 12),
        new(X: 12500, Y: 11000, Radius: 1800, MinLevel: 13, MaxLevel: 18,
            MobTypes: new[] { "Bandit" },                  MobCount: 10),
    };

    /// <summary>
    /// "Roads": wide strips where mobs do NOT spawn, so there are safe-ish
    /// corridors leading toward the hunting grounds. The client draws them as
    /// thick, semi-transparent grey lines. Each path is a sequence of points
    /// with a half-width; the strip is the area within Width of any segment.
    /// </summary>
    public static readonly RoadPath[] Roads =
    {
        // From town center outward to each spawn cluster.
        new(Width: 320, Points: new[]
        {
            new MapPoint(7500, 5500),  // town
            new MapPoint(6000, 4000),
            new MapPoint(9000, 4000),
        }),
        new(Width: 320, Points: new[]
        {
            new MapPoint(7500, 5500),
            new MapPoint(3000, 7000),
            new MapPoint(2500, 11000),
        }),
        new(Width: 320, Points: new[]
        {
            new MapPoint(7500, 5500),
            new MapPoint(12000, 7000),
            new MapPoint(12500, 11000),
        }),
    };

    /// <summary>True if (x,y) lies on a road strip (used to keep mobs off roads).</summary>
    public static bool OnRoad(float x, float y)
    {
        foreach (var road in Roads)
            if (road.Contains(x, y))
                return true;
        return false;
    }

    /// <summary>Clamp a position to stay inside the world border.</summary>
    public static (float X, float Y) ClampToBorder(float x, float y) =>
        (Math.Clamp(x, Border.MinX, Border.MaxX),
         Math.Clamp(y, Border.MinY, Border.MaxY));
}

public record WorldBorder(float MinX, float MinY, float MaxX, float MaxY);

public record MapPoint(float X, float Y);

public record SpawnZone(
    float X, float Y, float Radius,
    int MinLevel, int MaxLevel,
    string[] MobTypes, int MobCount);

public record RoadPath(float Width, MapPoint[] Points)
{
    /// <summary>Is (px,py) within Width of any segment of this path?</summary>
    public bool Contains(float px, float py)
    {
        for (int i = 0; i < Points.Length - 1; i++)
        {
            if (DistanceToSegment(px, py, Points[i], Points[i + 1]) <= Width)
                return true;
        }
        return false;
    }

    private static float DistanceToSegment(float px, float py, MapPoint a, MapPoint b)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float apx = px - a.X, apy = py - a.Y;
        float lenSq = abx * abx + aby * aby;
        float t = lenSq <= 0 ? 0 : Math.Clamp((apx * abx + apy * aby) / lenSq, 0, 1);
        float cx = a.X + abx * t, cy = a.Y + aby * t;
        float dx = px - cx, dy = py - cy;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
