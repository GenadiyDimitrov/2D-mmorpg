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
    // The world is a 48000x48000 square. The starter town (Brackenford) sits at the
    // centre (24000,24000); six more towns ring it, and difficulty rises as you
    // tour the ring clockwise from the north (Stonewatch → Emberfall → Greymarsh →
    // Ironreach → Duskvale → Frostmere). Each band has 1-2 spawn zones beside its town.
    // Coordinates were scaled ×2 from the old 24000 world so towns are far apart and
    // zones aren't clustered; zone radii kept so the gaps between them grew.
    public static readonly SpawnZone[] SpawnZones =
    {
        // Named mobs bring their OWN level (MobType.Level); the band on each zone is
        // descriptive — the roster below places each creature in its natural band.
        // ===== Brackenford (centre) — levels 1-10 =====
        new(X: 19400, Y: 24000, Radius: 1400, MinLevel: 1,  MaxLevel: 4,
            MobTypes: new[] { "ridgeback_pup", "fox" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),
        new(X: 28600, Y: 24000, Radius: 1400, MinLevel: 4,  MaxLevel: 8,
            MobTypes: new[] { "fox", "goblin_scout" }, MaxCount: 10,
            RespawnSeconds: 8, RespawnVariance: 3),
        new(X: 24000, Y: 19400, Radius: 1300, MinLevel: 8,  MaxLevel: 10,
            MobTypes: new[] { "goblin_scout", "ashen_wolf" }, MaxCount: 10,
            RespawnSeconds: 10, RespawnVariance: 4),

        // ===== Stonewatch (north) — levels 10-22 =====
        new(X: 20400, Y: 8400,  Radius: 1500, MinLevel: 10, MaxLevel: 15,
            MobTypes: new[] { "ashen_wolf", "werewolf", "hook_spider" }, MaxCount: 12,
            RespawnSeconds: 12, RespawnVariance: 4),
        new(X: 27600, Y: 10400, Radius: 1500, MinLevel: 16, MaxLevel: 22,
            MobTypes: new[] { "orc_archer", "skeleton_grunt", "shield_skeleton", "grizzly_bear" }, MaxCount: 12,
            RespawnSeconds: 14, RespawnVariance: 5),

        // ===== Emberfall (north-east) — levels 22-34 =====
        new(X: 33000, Y: 12400, Radius: 1500, MinLevel: 22, MaxLevel: 28,
            MobTypes: new[] { "grizzly_bear", "cinder_imp", "watcher_eye", "lizardman_warrior" }, MaxCount: 12,
            RespawnSeconds: 15, RespawnVariance: 5),
        new(X: 39000, Y: 16400, Radius: 1500, MinLevel: 28, MaxLevel: 34,
            MobTypes: new[] { "lizardman_warrior", "marauder_recruit", "mantis_worker", "grave_robber_fighter", "medusa", "plunder_beetle" }, MaxCount: 12,
            RespawnSeconds: 16, RespawnVariance: 5),

        // ===== Greymarsh (south-east) — levels 34-46 =====
        new(X: 33000, Y: 30400, Radius: 1500, MinLevel: 34, MaxLevel: 40,
            MobTypes: new[] { "medusa", "wyrm", "marsh_mantis_soldier", "fen_lizardman_archer", "dune_orc_archer" }, MaxCount: 11,
            RespawnSeconds: 18, RespawnVariance: 6),
        new(X: 39000, Y: 35000, Radius: 1500, MinLevel: 40, MaxLevel: 46,
            MobTypes: new[] { "rift_portling", "ridge_orc_overlord", "harpy", "grave_lich", "fomor_brute", "marsh_marauder" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6),

        // ===== Ironreach (south) — levels 46-58 =====
        new(X: 20400, Y: 40000, Radius: 1500, MinLevel: 46, MaxLevel: 52,
            MobTypes: new[] { "marsh_marauder", "warped_drake", "wildhorn_grunt", "amber_basilisk", "ravener", "mantis_follower", "marauder_warrior", "fallen_angel" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6),
        new(X: 27600, Y: 40000, Radius: 1500, MinLevel: 52, MaxLevel: 58,
            MobTypes: new[] { "fallen_angel", "thornback", "gaze_hound", "ash_orc_soldier", "mirror_wraith", "mirror_ghost", "dune_orc_porter", "aether_wisp", "hollow_one" }, MaxCount: 10,
            RespawnSeconds: 22, RespawnVariance: 7),

        // ===== Duskvale (south-west) — levels 58-70 =====
        new(X: 14400, Y: 35600, Radius: 1500, MinLevel: 58, MaxLevel: 64,
            MobTypes: new[] { "aether_wisp", "valley_treant", "sand_ratman", "cursed_blade", "bogwood", "fen_lizardman", "obsidian_knight", "crimson_drake", "wildhorn_scout" }, MaxCount: 10,
            RespawnSeconds: 24, RespawnVariance: 7),
        new(X: 9000,  Y: 30400, Radius: 1500, MinLevel: 64, MaxLevel: 70,
            MobTypes: new[] { "crimson_drake", "dread_knight", "wildhorn_elder", "spiteful_ghost", "highland_kookaburra", "highland_buffalo", "highland_buffalo_tamed", "dread_archer", "dire_beast" }, MaxCount: 10,
            RespawnSeconds: 26, RespawnVariance: 8),

        // ===== Frostmere (north-west) — levels 70-85 =====
        new(X: 14400, Y: 12400, Radius: 1500, MinLevel: 70, MaxLevel: 76,
            MobTypes: new[] { "dire_beast", "revenant_minion", "redhorn_footman", "sunland_orc_scout", "redhorn_elite", "redhorn_recruit", "sunland_orc_warrior", "redhorn_soldier", "sunland_orc_commander" }, MaxCount: 10,
            RespawnSeconds: 28, RespawnVariance: 8),
        new(X: 9000,  Y: 17600, Radius: 1500, MinLevel: 76, MaxLevel: 85,
            MobTypes: new[] { "redhorn_soldier", "sunland_orc_captain", "redhorn_general", "emberwyrm_drake", "wrathborn_demon", "scarlet_mantis", "radiant_scout", "radiant_berserker", "radiant_mage", "splinter_mantis_drone", "needle_mantis_overseer", "splinter_mantis_walker", "drake_leader", "disciple_of_the_dawn" }, MaxCount: 10,
            RespawnSeconds: 30, RespawnVariance: 9),

        // ===== Training Grounds: immortal, stationary, 0-damage dummies at fixed levels
        //       (20/40/60/80) for testing damage/skills. Clustered, one per level. =====
        new(X: 22500, Y: 4000, Radius: 200, MinLevel: 20, MaxLevel: 20,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 23500, Y: 4000, Radius: 200, MinLevel: 40, MaxLevel: 40,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 24500, Y: 4000, Radius: 200, MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 25500, Y: 4000, Radius: 200, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),

        // ===== Elite + Boss placeholders (more bosses/instances later) =====
        new(X: 11600, Y: 10000, Radius: 300,  MinLevel: 78, MaxLevel: 78,
            MobTypes: new[] { "emberwyrm_drake" }, MaxCount: 1,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite),
        new(X: 24000, Y: 45000, Radius: 250,  MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "valley_treant" }, MaxCount: 1,
            RespawnSeconds: 21 * 3600, RespawnVariance: 3 * 3600, Rank: MobRank.Boss),

        // ===== DUNGEON: Hollow Crypt (NW corner, OFF the town ring) =====
        // A dungeon is just a SpawnZone cluster away from the ring with an ENTRANCE safe zone (below),
        // so any gatekeeper offers it and the existing engine runs it for free. Harder ELITE rooms
        // (level 44-48) that respawn normally, ending in a boss. Normal drops (unlike an instance).
        new(X: 7200,  Y: 6500,  Radius: 350, MinLevel: 44, MaxLevel: 44,
            MobTypes: new[] { "hollow_one" }, MaxCount: 6,
            RespawnSeconds: 60, RespawnVariance: 15, Rank: MobRank.Elite),
        new(X: 8400,  Y: 7000,  Radius: 350, MinLevel: 45, MaxLevel: 45,
            MobTypes: new[] { "grave_robber_fighter" }, MaxCount: 6,
            RespawnSeconds: 60, RespawnVariance: 15, Rank: MobRank.Elite),
        new(X: 9600,  Y: 7500,  Radius: 350, MinLevel: 46, MaxLevel: 46,
            MobTypes: new[] { "dread_knight" }, MaxCount: 5,
            RespawnSeconds: 90, RespawnVariance: 20, Rank: MobRank.Elite),
        new(X: 10800, Y: 8000,  Radius: 300, MinLevel: 48, MaxLevel: 48,
            MobTypes: new[] { "grave_lich" }, MaxCount: 1,
            RespawnSeconds: 30 * 60, RespawnVariance: 5 * 60, Rank: MobRank.Boss),
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
        // Brackenford is the biggest (it holds all the quest/class/vendor NPCs, which
        // are spread out so their labels don't overlap); ring towns are roomy too.
        new("town_brackenford", "Brackenford",     24000, 24000, 3500),
        new("town_stonewatch",  "Stonewatch",      24000, 10000, 2000),
        new("town_emberfall",   "Emberfall",       36000, 15000, 2000),
        new("town_greymarsh",   "Greymarsh",       36000, 33000, 2000),
        new("castle_ironreach",  "Ironreach Keep", 24000, 38000, 2200),
        new("town_duskvale",    "Duskvale",        12000, 33000, 2000),
        new("town_frostmere",   "Frostmere",       12000, 15000, 2000),
        // Small outpost beside the Training Grounds, so you can buff up and teleport out without
        // leaving the dummies. Sits just SOUTH of the dummy row (they're at y=4000, radius 200),
        // clear of them — a safe zone keeps mobs out, and the dummies ARE mobs.
        new("outpost_training", "Training Outpost", 24000, 5000, 400),
        // The Hollow Crypt dungeon ENTRANCE (NW corner). Being a safe zone makes it a teleport
        // destination from every gatekeeper automatically (TeleportDestinationsFrom default), and gives
        // a safe spot to arrive/regroup before the elite rooms just east of it.
        new("dungeon_hollow_crypt", "Hollow Crypt", 6000, 6000, 500),
    };

    /// <summary>The STARTER town (map centre). Used where "nearest" would leak information — a player
    /// released from jail is sent here rather than to whatever town happens to be closest, so the jail's
    /// location stays secret.</summary>
    public static SafeZone StartingTown => SafeZones[0];

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

    /// <summary>Per-gatekeeper teleport menus: gatekeeper NPC id -> the ORDERED town
    /// ids it offers. A gatekeeper not listed here falls back to "all other towns".
    /// This is the seam for curating each gatekeeper's own collection (and, on a large
    /// map, its nearby zones) — edit a gatekeeper's list here to change its menu.</summary>
    public static readonly Dictionary<string, string[]> GatekeeperDestinations = new();

    /// <summary>The towns a gatekeeper offers travel to: its curated list if present
    /// in <see cref="GatekeeperDestinations"/>, otherwise every other town. Always
    /// excludes the gatekeeper's own town.</summary>
    public static IEnumerable<SafeZone> TeleportDestinationsFrom(string gatekeeperNpcId, SafeZone home)
    {
        if (GatekeeperDestinations.TryGetValue(gatekeeperNpcId, out var ids))
            return ids.Select(id => Array.Find(SafeZones, z => z.Id == id))
                      .Where(z => z is not null && z.Id != home.Id)
                      .Select(z => z!);
        return SafeZones.Where(z => z.Id != home.Id);
    }

    /// <summary>NPCs placed in the world (quest givers, class-change masters).
    /// Stationary, non-combat. Add NPCs here; quests/class-changes reference
    /// them by Id.</summary>
    public static readonly NpcDef[] Npcs =
    {
        // --- Starter town: Brackenford (map centre, 24000,24000, radius 3500). NPCs
        //     are spread ~2000 units apart (≈360px on screen) so labels don't overlap;
        //     the centre is left clear (where new characters spawn). ---
        new("priest_oren",   "High Priest Oren",   22000, 22500, NpcRole.QuestGiver),
        new("elder_marius",  "Elder Marius",       26000, 22500, NpcRole.QuestGiver),
        new("master_class",  "Class Master Vael",  21500, 24500, NpcRole.ClassChange),
        // 3rd-class master: gives the harder lvl-40 discipline chains AND performs
        // the change (an NpcRole.ClassChange NPC can also be a quest giver).
        new("master_class3", "Grandmaster Thorne", 26500, 24500, NpcRole.ClassChange),
        // Vendors (their wares are defined by ShopCatalog, keyed on these ids).
        new("merchant_potions", "Apothecary Miren", 22000, 26000, NpcRole.Vendor),
        new("merchant_gear",    "Armsmaster Dolan",  26000, 26000, NpcRole.Vendor),
        // Newbie buffer: blesses lvl 6-75 characters with a buffer's full buff set.
        new("buffer_newbie",    "Spirit Helper Nyra", 24000, 26500, NpcRole.Buffer),
        // Skill reset: un-learns the PERMANENT, mutually-exclusive picks (the level-40 stat swaps)
        // so a bad commitment can be re-chosen. Free to forget — the gold is NOT refunded.
        new("resetter_main",    "Mindwright Sela",   21500, 26000, NpcRole.SkillReset),
        // --- Gatekeepers: one in every town (stands at its centre) so the whole
        //     travel network is reachable in both directions. ---
        new("gatekeeper_brackenford", "Gatekeeper Pell",   24000, 21500, NpcRole.Teleporter),
        new("gatekeeper_stonewatch",  "Gatekeeper Soren",  24000, 10000, NpcRole.Teleporter),
        new("gatekeeper_emberfall",   "Gatekeeper Ryn",    36000, 15000, NpcRole.Teleporter),
        new("gatekeeper_greymarsh",   "Gatekeeper Maela",  36000, 33000, NpcRole.Teleporter),
        new("gatekeeper_ironreach",   "Gatekeeper Vurst",  24000, 38000, NpcRole.Teleporter),
        new("gatekeeper_duskvale",    "Gatekeeper Talia",  12000, 33000, NpcRole.Teleporter),
        new("gatekeeper_frostmere",   "Gatekeeper Khaz",   12000, 15000, NpcRole.Teleporter),

        // --- Training Outpost (24000, 5000, r=400), beside the dummies. The two NPCs are OFFSET
        //     from each other so their labels don't overlap: gatekeeper at the north edge, buffer
        //     at the south. Buff up, walk 800 north to the dummies, teleport out when done. ---
        new("gatekeeper_training", "Gatekeeper Vess",    24000, 4800, NpcRole.Teleporter),
        new("buffer_training",     "Spirit Helper Ilva", 24000, 5200, NpcRole.Buffer),
    };

    public static readonly RoadPath[] Roads =
    {
        // Spokes from Brackenford (centre) out to each ring town (coords ×2; wider too).
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(24000, 10000) }), // Stonewatch (N)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(36000, 15000) }), // Emberfall (NE)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(36000, 33000) }), // Greymarsh (SE)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(24000, 38000) }), // Ironreach (S)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(12000, 33000) }), // Duskvale (SW)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(12000, 15000) }), // Frostmere (NW)
    };

    /// <summary>True if (x,y) lies on a road strip (used to keep mobs off roads).</summary>
    public static bool OnRoad(float x, float y)
    {
        foreach (var road in Roads)
            if (road.Contains(x, y))
                return true;
        return false;
    }

    /// <summary>The level band of the normal hunting grounds around a town: the
    /// min/max level across spawn zones whose nearest town is this one. Returns null
    /// if the town has no normal zones beside it (elite/boss spots are ignored).</summary>
    public static (int Min, int Max)? LevelRangeNear(SafeZone town)
    {
        int min = int.MaxValue, max = 0;
        foreach (var z in SpawnZones)
        {
            if (z.Rank != MobRank.Normal) continue;
            if (NearestSafeZone(z.X, z.Y).Id != town.Id) continue;
            min = Math.Min(min, z.MinLevel);
            max = Math.Max(max, z.MaxLevel);
        }
        return max == 0 ? null : (min, max);
    }

    /// <summary>Find a placed NPC by id (null if none).</summary>
    public static NpcDef? NpcById(string id) =>
        Array.Find(Npcs, n => n.Id == id);

    /// <summary>A "where to find this mob" hint: the nearest town to the spawn zones
    /// that contain <paramref name="mobTypeId"/> within [minLevel,maxLevel], plus that
    /// band's levels. Returns ("", 0, 0) if the mob isn't placed in any matching zone.</summary>
    public static (string Town, int Min, int Max) MobHuntingGround(string mobTypeId, int minLevel, int maxLevel)
    {
        int min = int.MaxValue, max = 0;
        SafeZone? best = null;
        float bestSq = float.MaxValue;
        foreach (var z in SpawnZones)
        {
            if (Array.IndexOf(z.MobTypes, mobTypeId) < 0) continue;
            // Honour the quest's level band when one is set (0 = unbounded).
            if (maxLevel > 0 && z.MinLevel > maxLevel) continue;
            if (minLevel > 0 && z.MaxLevel < minLevel) continue;
            min = Math.Min(min, z.MinLevel);
            max = Math.Max(max, z.MaxLevel);
            var town = NearestSafeZone(z.X, z.Y);
            float dx = z.X - town.X, dy = z.Y - town.Y;
            float sq = dx * dx + dy * dy;
            if (sq < bestSq) { bestSq = sq; best = town; }
        }
        return best is null ? ("", 0, 0) : (best.Name, min, max);
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

public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2, Teleporter = 3, Buffer = 4, SkillReset = 5 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role);

/// <summary>A safe zone (city/castle). Id is referenced by teleports later.</summary>
public record SafeZone(string Id, string Name, float X, float Y, float Radius);

