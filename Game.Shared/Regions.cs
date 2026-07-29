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
    public static readonly Region[] Fields = new[]
    {
        // ===== THE FIVE CITIES' HUNTING FIELDS — GENERATED from their spawn zones =====
        // These used to be a dozen hand-written vertices each, which had to keep agreeing with the
        // circles inside them or the startup guard refused to boot the server. They are now derived:
        // a field IS "wherever its spawners are, plus a margin" (see FieldOf), so moving or re-banding
        // a zone can no longer strand it outside its own field. That is what made the 7-town → 5-city
        // re-layout a data edit instead of a geometry exercise.
        //
        // One field per CITY, wrapping all of that city's bands. Zones are picked by position rather
        // than index so re-ordering the list above cannot silently reshuffle the map.
        FieldOf("field_brackenford", "Bracken Reach", 900f, ZonesNear(24000, 24000, 7000)),
        FieldOf("field_stonewatch",  "Stonewatch Wilds", 600f, ZonesNear(24000, 10000, 6000)),
        FieldOf("field_greymarsh",   "Greymarsh Fens", 900f, ZonesNear(36000, 33000, 6000)),
        FieldOf("field_ironreach",   "Ironreach Marches", 900f, ZonesNear(24000, 38000, 6000)),
        FieldOf("field_frostmere",   "Frostmere Wastes", 900f, ZonesNear(12000, 15000, 9000)),

        // Training Grounds — wraps the Training Outpost + all four dummies (band 20-80)
        new("field_training", "Training Grounds", RegionKind.Field,
            new[] { new Vec2(21500, 3500), new Vec2(21900, 3100), new Vec2(26100, 3100), new Vec2(26500, 3500), new Vec2(26500, 4050), new Vec2(26200, 4450), new Vec2(24350, 5900), new Vec2(23650, 5900), new Vec2(21800, 4450), new Vec2(21500, 4050) },
            new[] { new Vec2(22500, 4000), new Vec2(23500, 4000) }),

        // Sunken Vale — the valley-treant BOSS field (band 58-60). The boss sits alone in the centre;
        // its two trash spawners are >3500u away on the flanks, so you reach the boss without aggro.
        new("field_treant", "Sunken Vale", RegionKind.Field,
            new[] { new Vec2(18350, 44200), new Vec2(19400, 43100), new Vec2(28600, 43100), new Vec2(29650, 44200), new Vec2(29650, 45800), new Vec2(28600, 46900), new Vec2(19400, 46900), new Vec2(18350, 45800) },
            new[] { new Vec2(20500, 45000), new Vec2(27500, 45000) }),

        // Hollow Crypt — the DUNGEON field (band 44-48), in the NEGATIVE quadrant (owner: dungeons live at
        // minus coords, off the overworld, reached by teleport). Elite rooms + the grave-lich boss; its
        // entrance safe zone (dungeon_hollow_crypt) sits just SW as a separate island.
        new("field_dungeon", "Hollow Crypt", RegionKind.Field,
            new[] { new Vec2(-11850, -11950), new Vec2(-11400, -12400), new Vec2(-10800, -12450), new Vec2(-7750, -11350), new Vec2(-6550, -10650), new Vec2(-6150, -10200), new Vec2(-6200, -9600), new Vec2(-6600, -9150), new Vec2(-7150, -9100), new Vec2(-8550, -9450), new Vec2(-11500, -10800), new Vec2(-11900, -11300) },
            new[] { new Vec2(-9600, -11000), new Vec2(-8400, -10500) }),
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
        Town("town_greymarsh",   "Greymarsh",       36000, 33000, 2000),
        Town("castle_ironreach", "Ironreach Keep",  24000, 38000, 2200),
        Town("town_frostmere",   "Frostmere",       12000, 15000, 2000),
        Town("outpost_training", "Training Outpost", 24000, 5000, 400),
        Town("dungeon_hollow_crypt", "Hollow Crypt",  -12000, -12000, 500),
    };

    /// <summary>Build a FIELD's outline from the spawn zones it should contain, instead of hand-drawing
    /// a polygon around them.
    ///
    /// Hand-authored outlines were the single most fragile thing in the world file: every field was a
    /// dozen literal vertices that had to keep agreeing with the circles inside it, and a startup guard
    /// (<see cref="ValidateNoRogueSpawners"/>) throws if any spawner escapes. Move a zone by 500 units
    /// and the server refuses to boot. Deriving the outline means a field simply IS "wherever its
    /// spawners are, plus a margin" — the two can no longer disagree.
    ///
    /// The shape is a convex hull of points sampled around each zone circle (radius + margin), which
    /// gives an organic outline that hugs however the zones happen to be arranged: a line of zones
    /// becomes a corridor, a clump becomes a blob. Arrival points are the zone centres, so a teleport
    /// spreads people across the field rather than stacking them on one doorstep.</summary>
    /// <summary>Every OVERWORLD spawn zone whose centre lies within <paramref name="radius"/> of a
    /// city. Picking a field's zones by POSITION rather than by index means re-ordering or re-banding
    /// the zone list cannot silently reshuffle which field owns what. Boss, dungeon and training
    /// spawners are excluded by their own fields being authored separately — they sit far outside any
    /// city radius.</summary>
    private static SpawnZone[] ZonesNear(float cx, float cy, float radius) =>
        WorldMap.SpawnZones
            .Where(z => (z.X - cx) * (z.X - cx) + (z.Y - cy) * (z.Y - cy) <= radius * radius)
            .ToArray();

    private static Region FieldOf(string id, string name, float margin, params SpawnZone[] zones)
    {
        const int samples = 12;
        var pts = new List<Vec2>(zones.Length * samples);
        foreach (var z in zones)
        {
            float r = z.Radius + margin;
            for (int i = 0; i < samples; i++)
            {
                float a = i * (MathF.PI * 2f / samples);
                pts.Add(new Vec2(z.X + r * MathF.Cos(a), z.Y + r * MathF.Sin(a)));
            }
        }
        return new Region(id, name, RegionKind.Field, ConvexHull(pts),
                          zones.Select(z => new Vec2(z.X, z.Y)).ToArray());
    }

    /// <summary>Andrew's monotone chain — sort by x then y, sweep the lower and upper hulls. Returns the
    /// hull counter-clockwise, which is the winding <see cref="Region.Outline"/> documents.</summary>
    private static Vec2[] ConvexHull(List<Vec2> pts)
    {
        if (pts.Count <= 3) return pts.ToArray();
        pts.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));

        static float Cross(Vec2 o, Vec2 a, Vec2 b) =>
            (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

        var hull = new List<Vec2>(pts.Count * 2);
        foreach (var p in pts)   // lower
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        int lower = hull.Count + 1;
        for (int i = pts.Count - 2; i >= 0; i--)   // upper
        {
            var p = pts[i];
            while (hull.Count >= lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0)
                hull.RemoveAt(hull.Count - 1);
            hull.Add(p);
        }
        hull.RemoveAt(hull.Count - 1);   // last point repeats the first
        return hull.ToArray();
    }

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

    /// <summary>The DUNGEON fields — those authored entirely in the NEGATIVE quadrant (dungeons live off
    /// the overworld, reached by teleport). Used to WALL players inside a dungeon so they can't wander
    /// out into the empty negative void or back to the overworld on foot.</summary>
    public static readonly Region[] Dungeons =
        Fields.Where(f => f.MaxX < 0 && f.MaxY < 0).ToArray();

    /// <summary>The dungeon whose BOUNDING BOX contains the point, or null when the point is in the
    /// overworld (both coords ≥ 0). Bbox, not polygon, so a player standing in a dungeon's corner still
    /// counts as "in the dungeon" and is walled to it rather than snapped to the overworld.</summary>
    public static Region? DungeonAt(float x, float y)
    {
        if (x >= 0 && y >= 0) return null;
        foreach (var d in Dungeons)
            if (x >= d.MinX && x <= d.MaxX && y >= d.MinY && y <= d.MaxY) return d;
        return null;
    }

    /// <summary>The dungeon whose bounding box is nearest the point (0 = inside it). Null if there are no
    /// dungeons. Used to pull an escapee back into the closest dungeon.</summary>
    public static Region? NearestDungeon(float x, float y)
    {
        Region? best = null; float bestSq = float.MaxValue;
        foreach (var d in Dungeons)
        {
            float dx = MathF.Max(0f, MathF.Max(d.MinX - x, x - d.MaxX));
            float dy = MathF.Max(0f, MathF.Max(d.MinY - y, y - d.MaxY));
            float sq = dx * dx + dy * dy;
            if (sq < bestSq) { bestSq = sq; best = d; }
        }
        return best;
    }

    /// <summary>How far (world units) the point lies OUTSIDE the dungeon's bounding box; 0 when inside.</summary>
    public static float DistanceOutsideBox(Region box, float x, float y)
    {
        float dx = MathF.Max(0f, MathF.Max(box.MinX - x, x - box.MaxX));
        float dy = MathF.Max(0f, MathF.Max(box.MinY - y, y - box.MaxY));
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Enforce the rule "no rogue spawners — every spawner is a child of a field" at startup
    /// (same spirit as the skill-id / abbreviation guards). A spawner outside every field would show as a
    /// lone circle with no zone identity, no derived band, and no field to belong to; catch it here rather
    /// than on the map. Throws listing the offenders.</summary>
    public static void ValidateSpawnersInFields()
    {
        var rogue = new List<string>();
        foreach (var z in WorldMap.SpawnZones)
        {
            bool inField = false;
            foreach (var f in Fields)
                if (f.Contains(z.X, z.Y)) { inField = true; break; }
            if (!inField)
                rogue.Add($"({z.X:0},{z.Y:0}) Lv{z.MinLevel}-{z.MaxLevel} [{string.Join('/', z.MobTypes)}]");
        }
        if (rogue.Count > 0)
            throw new InvalidOperationException(
                "Rogue spawners — every spawner must be a child of a field (add/extend a field to cover it):\n  "
                + string.Join("\n  ", rogue));
    }

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
