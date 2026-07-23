using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>What a region IS. Towns are safe; fields are where things live.</summary>
public enum RegionKind { Field = 0, Town = 1 }

/// <summary>A point in server world coordinates.</summary>
public readonly record struct Vec2(float X, float Y);

/// <summary>
/// A named area of the world with a real OUTLINE.
///
/// The world used to be a bag of circles — spawn zones and towns were both (x, y, radius) — which is
/// why the map read as generated: scattered coins, towns in perfect rings, teleports landing on a
/// centre point. A region has a polygon instead, and that polygon is what the client fills, what
/// "you are in the Field of Massacre" tests against, and what makes the shape of the world authored
/// rather than emergent.
///
/// A region does NOT spawn anything. The circular <see cref="SpawnZone"/>s inside it still do, exactly
/// as before — which is what keeps this additive instead of a rewrite. Membership is GEOMETRIC: a
/// spawner belongs to the region whose polygon contains its centre. Nothing has to be cross-referenced
/// by hand, and a spawner added inside an outline joins that field automatically.
///
/// A region may legitimately contain NO spawners: a peaceful named area is just an outline.
/// </summary>
public sealed record Region(
    string Id,
    string Name,
    RegionKind Kind,
    /// <summary>Polygon outline, counter-clockwise, in world coordinates.</summary>
    Vec2[] Outline,
    /// <summary>Where a teleport lands. ONE point = always the same spot; several = pick at random,
    /// which spreads arrivals out instead of stacking everyone on a doorstep.</summary>
    Vec2[] ArrivalPoints)
{
    /// <summary>Axis-aligned bounds, computed once. Containment tests run per player per tick, and a
    /// box rejects almost every candidate in four comparisons — the polygon test only has to run for
    /// the handful that survive.</summary>
    public float MinX { get; } = Outline.Length == 0 ? 0f : Outline.Min(p => p.X);
    public float MaxX { get; } = Outline.Length == 0 ? 0f : Outline.Max(p => p.X);
    public float MinY { get; } = Outline.Length == 0 ? 0f : Outline.Min(p => p.Y);
    public float MaxY { get; } = Outline.Length == 0 ? 0f : Outline.Max(p => p.Y);

    /// <summary>True when the point is inside the outline. Ray-crossing test: count how many edges a
    /// ray to the right crosses; odd means inside. Handles concave outlines, which is the whole point
    /// of having them.</summary>
    public bool Contains(float x, float y)
    {
        if (x < MinX || x > MaxX || y < MinY || y > MaxY) return false;   // cheap reject

        bool inside = false;
        var poly = Outline;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            // The (yi > y) != (yj > y) test also settles the "ray passes exactly through a vertex"
            // case by counting each edge's lower endpoint only.
            if ((poly[i].Y > y) != (poly[j].Y > y) &&
                x < (poly[j].X - poly[i].X) * (y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>A teleport arrival point, or the outline's centroid when none was authored — landing
    /// somewhere sensible beats refusing to travel.</summary>
    public Vec2 Arrival(Random rng)
    {
        if (ArrivalPoints.Length == 1) return ArrivalPoints[0];
        if (ArrivalPoints.Length > 1) return ArrivalPoints[rng.Next(ArrivalPoints.Length)];
        return Outline.Length == 0
            ? new Vec2(0f, 0f)
            : new Vec2(Outline.Average(p => p.X), Outline.Average(p => p.Y));
    }
}

/// <summary>
/// The world's regions, and the queries over them.
///
/// Level bands are DERIVED from the spawners a region contains, never authored: two spawners at 5-15
/// and 13-17 make a 5-17 field. An authored copy of a derived number is a number that will drift the
/// first time someone edits a spawner and forgets the label.
/// </summary>
public static class RegionMap
{
    /// <summary>
    /// Hunting fields — ONE per town, each a convex polygon that WRAPS the town and covers its nearby
    /// spawners, so the whole map reads as filled fields (owner: "switch to fields"). The town sits
    /// INSIDE its field like an island: `At()` checks towns first, so gameplay containment is correct
    /// (in the town = the town, step out = the field), and the client masks the field colour under the
    /// town so it reads as a lake/island — no "donut" polygon needed.
    ///
    /// The outlines are GENERATED (convex hull of the town + its spawner circles, inflated), verified by
    /// `tools`-side geometry: every field contains its spawners + town centre and no two fields overlap.
    /// The FILL colour is derived from the spawners the field contains (green→red by level), never
    /// authored. Isolated spawns with no nearby town (the far bosses, the Hollow Crypt dungeon) keep
    /// their circles until they get a field of their own.
    /// </summary>
    public static readonly Region[] Fields =
    {
        // Bracken Reach — wraps Brackenford (band 1-10)
        new("field_brackenford", "Bracken Reach", RegionKind.Field,
            new[] { new Vec2(17250, 23250), new Vec2(18250, 22200), new Vec2(23300, 17350), new Vec2(24700, 17350), new Vec2(29750, 22200), new Vec2(30750, 23250), new Vec2(30750, 24750), new Vec2(29750, 25800), new Vec2(25750, 28200), new Vec2(22250, 28200), new Vec2(18250, 25800), new Vec2(17250, 24750) },
            new[] { new Vec2(19400, 24000), new Vec2(28600, 24000) }),

        // Stonewatch Wilds — wraps Stonewatch (band 10-22)
        new("field_stonewatch", "Stonewatch Wilds", RegionKind.Field,
            new[] { new Vec2(18200, 7450), new Vec2(19300, 6350), new Vec2(20800, 6250), new Vec2(28800, 8500), new Vec2(29850, 9650), new Vec2(29850, 11250), new Vec2(28750, 12400), new Vec2(27250, 12450), new Vec2(22950, 12600), new Vec2(19150, 10150), new Vec2(18150, 9050) },
            new[] { new Vec2(20400, 8400), new Vec2(27600, 10400) }),

        // Emberfall Barrens — wraps Emberfall (band 22-34)
        new("field_emberfall", "Emberfall Barrens", RegionKind.Field,
            new[] { new Vec2(30850, 11400), new Vec2(31950, 10250), new Vec2(33500, 10200), new Vec2(40200, 14600), new Vec2(41250, 15750), new Vec2(41200, 17350), new Vec2(40100, 18500), new Vec2(38550, 18550), new Vec2(34950, 17600), new Vec2(33400, 16050), new Vec2(30800, 12950) },
            new[] { new Vec2(33000, 12400), new Vec2(39000, 16400) }),

        // Greymarsh Fens — wraps Greymarsh (band 34-46)
        new("field_greymarsh", "Greymarsh Fens", RegionKind.Field,
            new[] { new Vec2(30850, 29400), new Vec2(31950, 28250), new Vec2(33500, 28200), new Vec2(37050, 30400), new Vec2(40250, 33300), new Vec2(41250, 34400), new Vec2(41200, 36000), new Vec2(40100, 37100), new Vec2(38550, 37200), new Vec2(34950, 35600), new Vec2(33400, 34050), new Vec2(30800, 30950) },
            new[] { new Vec2(33000, 30400), new Vec2(39000, 35000) }),

        // Ironreach Marches — wraps Ironreach Keep (band 46-58)
        new("field_ironreach", "Ironreach Marches", RegionKind.Field,
            new[] { new Vec2(18150, 39400), new Vec2(19150, 38250), new Vec2(22850, 35200), new Vec2(25150, 35200), new Vec2(28850, 38250), new Vec2(29850, 39400), new Vec2(29800, 40950), new Vec2(28700, 42100), new Vec2(19300, 42100), new Vec2(18200, 40950) },
            new[] { new Vec2(20400, 40000), new Vec2(27600, 40000) }),

        // Duskvale Hollows — wraps Duskvale (band 58-70)
        new("field_duskvale", "Duskvale Hollows", RegionKind.Field,
            new[] { new Vec2(6850, 29400), new Vec2(7950, 28250), new Vec2(9500, 28200), new Vec2(13050, 30400), new Vec2(14600, 31950), new Vec2(16600, 35100), new Vec2(16550, 36650), new Vec2(15400, 37750), new Vec2(13850, 37800), new Vec2(10950, 35600), new Vec2(9400, 34050), new Vec2(6800, 30950) },
            new[] { new Vec2(14400, 35600), new Vec2(9000, 30400) }),

        // Frostmere Wastes — wraps Frostmere (band 70-85). The emberwyrm elite is left OUT (it sits only
        // ~2150u from the Hollow Crypt boss room, so it can't share this field or take its own without
        // overlap) — it stays an isolated circle, like any lone elite.
        new("field_frostmere", "Frostmere Wastes", RegionKind.Field,
            new[] { new Vec2(6800, 17050), new Vec2(9400, 13950), new Vec2(10950, 12400), new Vec2(13850, 10200), new Vec2(15400, 10250), new Vec2(16550, 11350), new Vec2(16600, 12900), new Vec2(14600, 16050), new Vec2(13050, 17600), new Vec2(9500, 19800), new Vec2(7950, 19750), new Vec2(6850, 18600) },
            new[] { new Vec2(14400, 12400), new Vec2(9000, 17600) }),

        // Training Grounds — wraps the Training Outpost + all four dummies (band 20-80)
        new("field_training", "Training Grounds", RegionKind.Field,
            new[] { new Vec2(21500, 3500), new Vec2(21900, 3100), new Vec2(26100, 3100), new Vec2(26500, 3500), new Vec2(26500, 4050), new Vec2(26200, 4450), new Vec2(24350, 5900), new Vec2(23650, 5900), new Vec2(21800, 4450), new Vec2(21500, 4050) },
            new[] { new Vec2(22500, 4000), new Vec2(23500, 4000) }),

        // Sunken Vale — the valley-treant BOSS field (band 58-60). The boss sits alone in the centre;
        // its two trash spawners are >3500u away on the flanks, so you reach the boss without aggro.
        new("field_treant", "Sunken Vale", RegionKind.Field,
            new[] { new Vec2(18350, 44200), new Vec2(19400, 43100), new Vec2(28600, 43100), new Vec2(29650, 44200), new Vec2(29650, 45800), new Vec2(28600, 46900), new Vec2(19400, 46900), new Vec2(18350, 45800) },
            new[] { new Vec2(20500, 45000), new Vec2(27500, 45000) }),

        // Hollow Crypt — the DUNGEON field (band 44-48): the elite rooms + the grave-lich boss. Its
        // entrance safe zone (dungeon_hollow_crypt) sits just west as a separate island.
        new("field_dungeon", "Hollow Crypt", RegionKind.Field,
            new[] { new Vec2(6150, 6050), new Vec2(6600, 5600), new Vec2(7200, 5550), new Vec2(10250, 6650), new Vec2(11450, 7350), new Vec2(11850, 7800), new Vec2(11800, 8400), new Vec2(11400, 8850), new Vec2(10850, 8900), new Vec2(9450, 8550), new Vec2(6500, 7200), new Vec2(6100, 6700) },
            new[] { new Vec2(8400, 7000), new Vec2(9600, 7500) }),
    };

    /// <summary>
    /// TOWNS as regions (stage 2). Each is an OCTAGON INSCRIBED in its safe-zone circle (rad = r, so the
    /// drawn town sits just inside the safe radius). Safety is UNAFFECTED: `InAnySafeZone` unions the
    /// full circle with these polygons, so the circle — not the octagon — sets where you're safe. The
    /// octagon is only the drawn shape, kept snug against the circle so it no longer bleeds into the
    /// hunting fields around it (owner: regions must not overlap; towns were reading too large).
    /// </summary>
    public static readonly Region[] Towns =
    {
        Town("town_brackenford", "Brackenford",     24000, 24000, 3500),
        Town("town_stonewatch",  "Stonewatch",      24000, 10000, 2000),
        Town("town_emberfall",   "Emberfall",       36000, 15000, 2000),
        Town("town_greymarsh",   "Greymarsh",       36000, 33000, 2000),
        Town("castle_ironreach", "Ironreach Keep",  24000, 38000, 2200),
        Town("town_duskvale",    "Duskvale",        12000, 33000, 2000),
        Town("town_frostmere",   "Frostmere",       12000, 15000, 2000),
        Town("outpost_training", "Training Outpost", 24000, 5000, 400),
        Town("dungeon_hollow_crypt", "Hollow Crypt",  6000, 6000, 500),
    };

    /// <summary>An octagon centred on (cx,cy) INSCRIBED in the safe circle of radius r (its corners
    /// touch the circle, flat sides sit at 0.924·r), plus a single arrival point at the centre. The
    /// circle is unioned for safety, so the smaller drawn octagon never makes anywhere unsafe.</summary>
    private static Region Town(string id, string name, float cx, float cy, float r)
    {
        float rad = r * 1.0f;
        var outline = new Vec2[8];
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.PI / 8f + i * (MathF.PI / 4f);   // 22.5° + k·45°, counter-clockwise
            outline[i] = new Vec2(cx + rad * MathF.Cos(a), cy + rad * MathF.Sin(a));
        }
        return new Region(id, name, RegionKind.Town, outline, new[] { new Vec2(cx, cy) });
    }

    /// <summary>Every region — fields and towns — for the client to draw and "which region am I in".</summary>
    public static readonly Region[] Regions = Fields.Concat(Towns).ToArray();
    public static IEnumerable<Region> All => Regions;

    /// <summary>True when the point is inside any TOWN region — the polygon half of the safe-zone rule.</summary>
    public static bool InTown(float x, float y)
    {
        foreach (var t in Towns)
            if (t.Contains(x, y)) return true;
        return false;
    }

    private static readonly Dictionary<string, SpawnZone[]> SpawnersByRegion = BuildSpawnerIndex();

    private static Dictionary<string, SpawnZone[]> BuildSpawnerIndex()
    {
        var map = new Dictionary<string, SpawnZone[]>(StringComparer.Ordinal);
        foreach (var region in Fields)
            map[region.Id] = WorldMap.SpawnZones.Where(z => region.Contains(z.X, z.Y)).ToArray();
        return map;
    }

    /// <summary>The spawners whose CENTRE falls inside this region. Geometric membership means nothing
    /// has to be listed twice, and a spawner moved out of an outline stops belonging to it.</summary>
    public static SpawnZone[] SpawnersIn(string regionId) =>
        SpawnersByRegion.TryGetValue(regionId, out var zones) ? zones : Array.Empty<SpawnZone>();

    /// <summary>The region containing a point, or null. TOWNS are checked first, so standing in a town
    /// that abuts a field reads as "in the town" (the safe area wins). Fields don't overlap by design.</summary>
    public static Region? At(float x, float y)
    {
        foreach (var region in Towns)
            if (region.Contains(x, y)) return region;
        foreach (var region in Fields)
            if (region.Contains(x, y)) return region;
        return null;
    }

    public static Region? ById(string id) => Array.Find(Regions, r => r.Id == id);

    /// <summary>The level band a region covers, derived from its spawners. Null when it has none — a
    /// peaceful area has no level, and showing "0-0" would be worse than showing nothing.</summary>
    public static (int Min, int Max)? LevelBand(string regionId)
    {
        var zones = SpawnersIn(regionId);
        if (zones.Length == 0) return null;

        int min = int.MaxValue, max = int.MinValue;
        foreach (var zone in zones)
        {
            if (zone.MinLevel < min) min = zone.MinLevel;
            if (zone.MaxLevel > max) max = zone.MaxLevel;
        }
        return (min, max);
    }
}
