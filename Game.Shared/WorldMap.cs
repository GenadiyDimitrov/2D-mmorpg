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
    // The world is a 24000x24000 square. The starter town (Brackenford) sits at the
    // centre (12000,12000); six more towns ring it, and difficulty rises as you
    // tour the ring clockwise from the north (Stonewatch → Emberfall → Greymarsh →
    // Ironreach → Duskvale → Frostmere). Each band has 1-2 spawn zones beside its town.
    public static readonly SpawnZone[] SpawnZones =
    {
        // ===== Brackenford (centre) — levels 1-10 =====
        new(X: 9700,  Y: 12000, Radius: 1400, MinLevel: 1,  MaxLevel: 4,
            MobTypes: new[] { "grey_wolf", "brown_boar" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),
        new(X: 14300, Y: 12000, Radius: 1400, MinLevel: 4,  MaxLevel: 7,
            MobTypes: new[] { "green_slime", "brown_boar" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),
        new(X: 12000, Y: 9700,  Radius: 1300, MinLevel: 7,  MaxLevel: 10,
            MobTypes: new[] { "cave_spider", "grey_wolf" }, MaxCount: 10,
            RespawnSeconds: 10, RespawnVariance: 4),

        // ===== Stonewatch (north) — levels 10-22 =====
        new(X: 10200, Y: 4200,  Radius: 1500, MinLevel: 10, MaxLevel: 15,
            MobTypes: new[] { "dire_boar", "cave_spider" }, MaxCount: 12,
            RespawnSeconds: 12, RespawnVariance: 4),
        new(X: 13800, Y: 5200,  Radius: 1500, MinLevel: 16, MaxLevel: 22,
            MobTypes: new[] { "road_bandit", "dire_boar" }, MaxCount: 12,
            RespawnSeconds: 14, RespawnVariance: 5),

        // ===== Emberfall (north-east) — levels 22-34 =====
        new(X: 16500, Y: 6200,  Radius: 1500, MinLevel: 22, MaxLevel: 28,
            MobTypes: new[] { "road_bandit", "orc_raider" }, MaxCount: 12,
            RespawnSeconds: 15, RespawnVariance: 5),
        new(X: 19500, Y: 8200,  Radius: 1500, MinLevel: 28, MaxLevel: 34,
            MobTypes: new[] { "orc_raider", "cave_spider" }, MaxCount: 12,
            RespawnSeconds: 16, RespawnVariance: 5),

        // ===== Greymarsh (south-east) — levels 34-46 =====
        new(X: 16500, Y: 15200, Radius: 1500, MinLevel: 34, MaxLevel: 40,
            MobTypes: new[] { "orc_raider", "stone_golem" }, MaxCount: 11,
            RespawnSeconds: 18, RespawnVariance: 6),
        new(X: 19500, Y: 17500, Radius: 1500, MinLevel: 40, MaxLevel: 46,
            MobTypes: new[] { "stone_golem", "wraith" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6),

        // ===== Ironreach (south) — levels 46-58 =====
        new(X: 10200, Y: 20000, Radius: 1500, MinLevel: 46, MaxLevel: 52,
            MobTypes: new[] { "wraith", "stone_golem" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6),
        new(X: 13800, Y: 20000, Radius: 1500, MinLevel: 52, MaxLevel: 58,
            MobTypes: new[] { "wraith", "young_drake" }, MaxCount: 10,
            RespawnSeconds: 22, RespawnVariance: 7),

        // ===== Duskvale (south-west) — levels 58-70 =====
        new(X: 7200,  Y: 17800, Radius: 1500, MinLevel: 58, MaxLevel: 64,
            MobTypes: new[] { "young_drake", "wraith" }, MaxCount: 10,
            RespawnSeconds: 24, RespawnVariance: 7),
        new(X: 4500,  Y: 15200, Radius: 1500, MinLevel: 64, MaxLevel: 70,
            MobTypes: new[] { "young_drake", "stone_golem" }, MaxCount: 10,
            RespawnSeconds: 26, RespawnVariance: 8),

        // ===== Frostmere (north-west) — levels 70-80 =====
        new(X: 7200,  Y: 6200,  Radius: 1500, MinLevel: 70, MaxLevel: 76,
            MobTypes: new[] { "young_drake", "orc_raider" }, MaxCount: 10,
            RespawnSeconds: 28, RespawnVariance: 8),
        new(X: 4500,  Y: 8800,  Radius: 1500, MinLevel: 76, MaxLevel: 80,
            MobTypes: new[] { "young_drake", "wraith" }, MaxCount: 10,
            RespawnSeconds: 30, RespawnVariance: 9),

        // ===== Elite + Boss placeholders (more bosses/instances later) =====
        new(X: 5800,  Y: 5000,  Radius: 300,  MinLevel: 78, MaxLevel: 78,
            MobTypes: new[] { "young_drake" }, MaxCount: 1,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite),
        new(X: 12000, Y: 22500, Radius: 250,  MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "stone_golem" }, MaxCount: 1,
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
    // NOTE: all place names here are original/generic on purpose — NEVER use town,
    // region, or NPC names trademarked by other games (no Lineage/L2 names, etc.).
    public static readonly SafeZone[] SafeZones =
    {
        // Starter town at the map centre; six more ring it (clockwise from north).
        new("town_brackenford", "Brackenford",     12000, 12000, 1300),
        new("town_stonewatch",  "Stonewatch",      12000,  5000, 1000),
        new("town_emberfall",   "Emberfall",       18000,  7500, 1000),
        new("town_greymarsh",   "Greymarsh",       18000, 16500, 1000),
        new("castle_ironreach",  "Ironreach Keep", 12000, 19000, 1100),
        new("town_duskvale",    "Duskvale",         6000, 16500, 1000),
        new("town_frostmere",   "Frostmere",        6000,  7500, 1000),
    };

    /// <summary>The safe zone nearest to a point (always returns one). Used to
    /// respawn the dead at their closest town instead of the map centre.</summary>
    public static SafeZone NearestSafeZone(float x, float y)
    {
        SafeZone best = SafeZones[0];
        float bestSq = float.MaxValue;
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            float sq = dx * dx + dy * dy;
            if (sq < bestSq) { bestSq = sq; best = z; }
        }
        return best;
    }

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
        // --- Starter town: Brackenford (map centre, 12000,12000) ---
        new("priest_oren",   "High Priest Oren",   11700, 11800, NpcRole.QuestGiver),
        new("elder_marius",  "Elder Marius",       12300, 11800, NpcRole.QuestGiver),
        new("master_class",  "Class Master Vael",  12000, 11400, NpcRole.ClassChange),
        // Vendors (their wares are defined by ShopCatalog, keyed on these ids).
        new("merchant_potions", "Apothecary Miren", 11600, 11400, NpcRole.Vendor),
        new("merchant_gear",    "Armsmaster Dolan",  12400, 11400, NpcRole.Vendor),
        // --- Gatekeepers: one in every town (stands at its centre) so the whole
        //     travel network is reachable in both directions. ---
        new("gatekeeper_brackenford", "Gatekeeper Pell",   12000, 12300, NpcRole.Teleporter),
        new("gatekeeper_stonewatch",  "Gatekeeper Soren",  12000,  5000, NpcRole.Teleporter),
        new("gatekeeper_emberfall",   "Gatekeeper Ryn",    18000,  7500, NpcRole.Teleporter),
        new("gatekeeper_greymarsh",   "Gatekeeper Maela",  18000, 16500, NpcRole.Teleporter),
        new("gatekeeper_ironreach",   "Gatekeeper Vurst",  12000, 19000, NpcRole.Teleporter),
        new("gatekeeper_duskvale",    "Gatekeeper Talia",   6000, 16500, NpcRole.Teleporter),
        new("gatekeeper_frostmere",   "Gatekeeper Khaz",    6000,  7500, NpcRole.Teleporter),
    };

    public static readonly RoadPath[] Roads =
    {
        // Spokes from Brackenford (centre) out to each ring town.
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint(12000,  5000) }), // Stonewatch (N)
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint(18000,  7500) }), // Emberfall (NE)
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint(18000, 16500) }), // Greymarsh (SE)
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint(12000, 19000) }), // Ironreach (S)
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint( 6000, 16500) }), // Duskvale (SW)
        new(Width: 300, Points: new[] { new MapPoint(12000, 12000), new MapPoint( 6000,  7500) }), // Frostmere (NW)
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

public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2, Teleporter = 3 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role);

/// <summary>A safe zone (city/castle). Id is referenced by teleports later.</summary>
public record SafeZone(string Id, string Name, float X, float Y, float Radius);

