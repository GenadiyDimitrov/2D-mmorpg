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
        // MaxCount = population cap; RespawnSeconds/Variance = delay after a kill.
        new(X: 6000,  Y: 4000,  Radius: 1400, MinLevel: 1,  MaxLevel: 3,
            MobTypes: new[] { "grey_wolf", "brown_boar" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),
        new(X: 9000,  Y: 4000,  Radius: 1400, MinLevel: 1,  MaxLevel: 3,
            MobTypes: new[] { "green_slime", "brown_boar" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),

        // --- Mid-level fields ---
        new(X: 3000,  Y: 7000,  Radius: 1600, MinLevel: 4,  MaxLevel: 7,
            MobTypes: new[] { "brown_boar", "cave_spider" }, MaxCount: 12,
            RespawnSeconds: 12, RespawnVariance: 4),
        new(X: 12000, Y: 7000,  Radius: 1600, MinLevel: 4,  MaxLevel: 7,
            MobTypes: new[] { "grey_wolf", "cave_spider" }, MaxCount: 12,
            RespawnSeconds: 12, RespawnVariance: 4),

        // --- Higher-level, further out ---
        new(X: 2500,  Y: 11000, Radius: 1800, MinLevel: 8,  MaxLevel: 12,
            MobTypes: new[] { "cave_spider", "road_bandit" }, MaxCount: 12,
            RespawnSeconds: 15, RespawnVariance: 5),
        new(X: 12500, Y: 11000, Radius: 1800, MinLevel: 13, MaxLevel: 18,
            MobTypes: new[] { "road_bandit" }, MaxCount: 10,
            RespawnSeconds: 18, RespawnVariance: 6),

        // --- Day/night example: same spot, different mobs by time of day. ---
        new(X: 7500,  Y: 9500,  Radius: 1500, MinLevel: 5,  MaxLevel: 9,
            MobTypes: new[] { "brown_boar", "grey_wolf" }, MaxCount: 10,
            RespawnSeconds: 12, RespawnVariance: 4, Active: ActiveTime.Day),
        new(X: 7500,  Y: 9500,  Radius: 1500, MinLevel: 7,  MaxLevel: 11,
            MobTypes: new[] { "cave_spider", "road_bandit" }, MaxCount: 10,
            RespawnSeconds: 12, RespawnVariance: 4, Active: ActiveTime.Night),

        // --- Elite: single tough mob, ~2min ±30s respawn. ---
        new(X: 4000,  Y: 9000,  Radius: 300,  MinLevel: 12, MaxLevel: 12,
            MobTypes: new[] { "road_bandit" }, MaxCount: 1,
            RespawnSeconds: 120, RespawnVariance: 30, Rank: MobRank.Elite),

        // --- Boss: ~21h ±3h respawn, persisted across restarts. ---
        new(X: 11000, Y: 13000, Radius: 250,  MinLevel: 20, MaxLevel: 20,
            MobTypes: new[] { "road_bandit" }, MaxCount: 1,
            RespawnSeconds: 21 * 3600, RespawnVariance: 3 * 3600, Rank: MobRank.Boss),
    };

    /// <summary>
    /// "Roads": wide strips where mobs do NOT spawn, so there are safe-ish
    /// corridors leading toward the hunting grounds. The client draws them as
    /// thick, semi-transparent grey lines. Each path is a sequence of points
    /// with a half-width; the strip is the area within Width of any segment.
    /// </summary>
    /// <summary>Safe zones (cities/castles): no mobs spawn or enter, aggro
    /// clears inside, regen is boosted. Each has a stable id so teleports can
    /// target them later. The first is the starter town at map centre.</summary>
    public static readonly SafeZone[] SafeZones =
    {
        new("town_giran",   "Town of Giran",   7500, 7500, 1200),
        new("town_dion",    "Town of Dion",    3000, 3000, 900),
        new("castle_aden",  "Aden Castle",     12000, 4000, 1000),
    };

    /// <summary>True if the point is inside ANY safe zone.</summary>
    public static bool InAnySafeZone(float x, float y)
    {
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            if (dx * dx + dy * dy <= z.Radius * z.Radius)
                return true;
        }
        return false;
    }

    /// <summary>The safe zone containing a point, or null.</summary>
    public static SafeZone? SafeZoneAt(float x, float y)
    {
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            if (dx * dx + dy * dy <= z.Radius * z.Radius)
                return z;
        }
        return null;
    }

    /// <summary>NPCs placed in the world (quest givers, class-change masters).
    /// Stationary, non-combat. Add NPCs here; quests/class-changes reference
    /// them by Id.</summary>
    public static readonly NpcDef[] Npcs =
    {
        // Near town center (map middle). Tune coords as the town grows.
        new("priest_oren",   "High Priest Oren",   7200, 7300, NpcRole.QuestGiver),
        new("elder_marius",  "Elder Marius",       7800, 7300, NpcRole.QuestGiver),
        new("master_class",  "Class Master Vael",  7500, 6900, NpcRole.ClassChange),
        // Vendors (their wares are defined by ShopCatalog, keyed on these ids).
        new("merchant_potions", "Apothecary Miren", 7100, 6900, NpcRole.Vendor),
        new("merchant_gear",    "Armsmaster Dolan",  7900, 6900, NpcRole.Vendor),
    };

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

/// <summary>Mob rank — drives default respawn timing and lets the UI label
/// elites/bosses. Normal uses the zone's respawn range; Elite/Boss usually set
/// long ranges explicitly.</summary>
public enum MobRank { Normal = 0, Elite = 1, Boss = 2 }

/// <summary>When a zone is active. Always = 24h; Day/Night gate by the game
/// clock so you can run day-only and night-only zones (overlap two zones at the
/// same spot with different mobs to swap them at dusk/dawn).</summary>
public enum ActiveTime { Always = 0, Day = 1, Night = 2 }

/// <summary>
/// A spawn zone: a disc that maintains up to MaxCount living mobs. When a mob
/// dies the zone waits RespawnSeconds (± Variance) then respawns it — but never
/// exceeds MaxCount, and only while the zone is active for the current time of
/// day. Respawn timing is authored in SECONDS (real seconds); the in-game
/// description shows "[center ±variance]".
/// </summary>
public record SpawnZone(
    float X, float Y, float Radius,
    int MinLevel, int MaxLevel,
    string[] MobTypes, int MaxCount,
    double RespawnSeconds = 10, double RespawnVariance = 0,
    MobRank Rank = MobRank.Normal,
    ActiveTime Active = ActiveTime.Always)
{
    /// <summary>Stable id from coordinates+rank, used to persist boss timers.</summary>
    public string Id => $"{(int)X}_{(int)Y}_{Rank}";

    public bool IsActiveAt(DayPhase phase) => Active switch
    {
        ActiveTime.Day => phase == DayPhase.Day,
        ActiveTime.Night => phase == DayPhase.Night,
        _ => true
    };

    /// <summary>Human-readable respawn label, e.g. "2m 0s ±30s" or "21h ±3h".</summary>
    public string RespawnLabel => $"{Fmt(RespawnSeconds)} ±{Fmt(RespawnVariance)}";

    private static string Fmt(double seconds)
    {
        if (seconds >= 3600) return $"{seconds / 3600:0.#}h";
        if (seconds >= 60) return $"{(int)(seconds / 60)}m {(int)(seconds % 60)}s";
        return $"{(int)seconds}s";
    }
}

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

public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role);

/// <summary>A safe zone (city/castle). Id is referenced by teleports later.</summary>
public record SafeZone(string Id, string Name, float X, float Y, float Radius);

