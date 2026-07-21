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
    /// Hunting fields. Outlines are deliberately irregular — the point of the exercise is that the map
    /// stops looking like scattered circles.
    ///
    /// These cover the EXISTING spawn zones around the starter town; the remaining bands are data work
    /// rather than engineering, and a field with no outline authored yet simply has no region (its
    /// spawners keep working exactly as they do today).
    /// </summary>
    public static readonly Region[] Fields =
    {
        // West of Brackenford — the level 1-8 slopes. Wraps the two starter spawners.
        new("field_hollow", "Bracken Hollow", RegionKind.Field,
            new[]
            {
                new Vec2(16800, 21200), new Vec2(20600, 20400), new Vec2(21400, 23000),
                new Vec2(21000, 26400), new Vec2(18200, 27200), new Vec2(16400, 24600),
            },
            new[] { new Vec2(18600, 22400), new Vec2(19400, 25600) }),

        // East of Brackenford — the level 4-10 wolds.
        new("field_wolds", "Ashen Wolds", RegionKind.Field,
            new[]
            {
                new Vec2(27000, 20600), new Vec2(31400, 21400), new Vec2(32200, 24800),
                new Vec2(30600, 27400), new Vec2(27400, 26800), new Vec2(26800, 23600),
            },
            new[] { new Vec2(29000, 22600), new Vec2(30200, 25400) }),

        // North approach out of Brackenford — wraps the Lv 8-10 spawner at (24000, 19400).
        // NOTE: the first draft of this outline stopped at y=19200 and therefore contained NOTHING,
        // 200 units short. The startup report caught it on the first run; by eye the polygon looked
        // fine. Author outlines against the spawner coordinates, not against intuition.
        new("field_downs", "Winterward Downs", RegionKind.Field,
            new[]
            {
                new Vec2(21400, 15600), new Vec2(26600, 15200), new Vec2(28000, 18600),
                new Vec2(25800, 21000), new Vec2(22000, 20800), new Vec2(20600, 18200),
            },
            new[] { new Vec2(23800, 17600) }),

        // The road north to Stonewatch — the Lv 16-22 band at (27600, 10400).
        new("field_marches", "Sundered Marches", RegionKind.Field,
            new[]
            {
                new Vec2(25200, 8200), new Vec2(29800, 7600), new Vec2(31400, 10800),
                new Vec2(29600, 13400), new Vec2(26000, 12800), new Vec2(24600, 10400),
            },
            new[] { new Vec2(27400, 9600), new Vec2(28600, 12000) }),

        // A peaceful one, to prove a region needs no spawners at all: the lake south of the centre.
        new("field_mirrorlake", "Mirror Lake", RegionKind.Field,
            new[]
            {
                new Vec2(22600, 28200), new Vec2(25600, 28000), new Vec2(26400, 30600),
                new Vec2(24200, 31800), new Vec2(22000, 30400),
            },
            new[] { new Vec2(24200, 29800) }),
    };

    /// <summary>Every region. Towns join this list in stage 2; until then the array is fields only, and
    /// safe-zone rules still run off <see cref="WorldMap.SafeZones"/> untouched.</summary>
    public static IEnumerable<Region> All => Fields;

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

    /// <summary>The region containing a point, or null. Fields do not overlap by design; if two ever
    /// do, the first authored wins, which is at least deterministic.</summary>
    public static Region? At(float x, float y)
    {
        foreach (var region in Fields)
            if (region.Contains(x, y)) return region;
        return null;
    }

    public static Region? ById(string id) => Array.Find(Fields, r => r.Id == id);

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
