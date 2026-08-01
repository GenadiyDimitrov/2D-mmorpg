using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>What a region IS. Towns are safe; fields are where things live.</summary>
public enum RegionKind { Field = 0, Town = 1 }

/// <summary>A point in server world coordinates.</summary>
public readonly record struct Vec2(float X, float Y);

/// <summary>
/// A NAMED teleport destination inside a region — one camp's doorstep.
///
/// Arrival used to be "pick one of the region's points at random", which meant a gatekeeper could only
/// offer "Frostmere Wastes" and then drop you anywhere in it, including in the middle of a level-90 camp
/// (owner: *"each teleport point not to be coordinates for a random teleport but to have a name +
/// description — fieldName1 West, fieldName1 East"*). A gate has an identity instead: a name a
/// gatekeeper can list, a description saying which levels and creatures are there, and ONE spot it
/// always lands you on — the camp's town-facing rim, so you arrive at the edge looking in.
/// </summary>
public sealed record TeleportPoint(string Id, string Name, string Description, Vec2 At);

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
    /// <summary>The NAMED teleport gates in this region — one per camp for a field, one for a town. Each
    /// is a listed destination in its city's gatekeeper menu.</summary>
    TeleportPoint[] Gates,
    /// <summary>The MANAGING CITY's safe-zone id: which city owns this field.
    ///
    /// A field belongs to a city (owner), and that is where you respawn when you die in it — not
    /// "whatever town is nearest", which on a map with cities 13k apart could send you to a city whose
    /// gatekeeper does not even list the field you just died in. Empty for towns and for the isolated
    /// areas (the dungeon, the boss vale), where nearest-city remains the rule.</summary>
    string CityId = "")
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

    /// <summary>SOME point inside the region — used where the caller has no destination in mind (the ward
    /// that pulls an escapee back into a dungeon). Named travel goes through a <see cref="TeleportPoint"/>
    /// instead; this is the fallback, and the centroid keeps it sensible when a region has no gates.</summary>
    public Vec2 Arrival(Random rng)
    {
        if (Gates.Length == 1) return Gates[0].At;
        if (Gates.Length > 1) return Gates[rng.Next(Gates.Length)].At;
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
    /// Hunting fields — 2-3 per city, each a convex polygon wrapping the CAMPS it holds, with a named
    /// gate per camp and a managing city.
    ///
    /// It used to be ONE field per city, hulled around every band that city owned, and the town sat inside
    /// it like an island. That is why a gatekeeper could only offer "Bracken Reach" and then drop you at a
    /// random point inside it. A field is a PLACE now — one arc of camps at one distance from town, a name,
    /// and 1-3 doorsteps you can be sent to by name.
    ///
    /// The outlines are still GENERATED (convex hull of the camp circles, inflated by
    /// <see cref="WorldPlan.FieldMargin"/>) so a field cannot disagree with the camps inside it, and the
    /// fill colour is still derived from the band, never authored. Fields no longer contain their town, so
    /// there is nothing to mask under: they sit <see cref="WorldPlan.TownGap"/> clear of the wall.
    /// </summary>
    public static readonly Region[] Fields =
        WorldPlan.Fields.Select(PlannedField).Concat(new Region[]
    {
        // ===== The AREAS THAT ARE NOT LEVEL BANDS — hand-authored, because they are not a city's
        //       hunting grounds and so have no place in WorldPlan =====
        // Everything above this comes from WorldPlan: one Region per planned FIELD, its polygon hulled
        // around its camps, its gates named per camp, and its managing city recorded. There used to be
        // ONE field per city wrapping every band it owned, which is why a gatekeeper could only offer
        // "Bracken Reach" and drop you anywhere inside it.

        // Training Grounds — wraps the Training Outpost + all four dummies (band 20-80)
        new("field_training", "Training Grounds", RegionKind.Field,
            new[] { new Vec2(21500, 3500), new Vec2(21900, 3100), new Vec2(26100, 3100), new Vec2(26500, 3500), new Vec2(26500, 4050), new Vec2(26200, 4450), new Vec2(24350, 5900), new Vec2(23650, 5900), new Vec2(21800, 4450), new Vec2(21500, 4050) },
            new[] { Gate("field_training#0", "Training Grounds", "Immortal dummies at Lv 20 / 40 / 60 / 80", 22500, 4000) }),

        // Sunken Vale — the valley-treant BOSS field (band 58-60). The boss sits alone in the centre;
        // its two trash spawners are >3500u away on the flanks, so you reach the boss without aggro.
        new("field_treant", "Sunken Vale", RegionKind.Field,
            new[] { new Vec2(18350, 44200), new Vec2(19400, 43100), new Vec2(28600, 43100), new Vec2(29650, 44200), new Vec2(29650, 45800), new Vec2(28600, 46900), new Vec2(19400, 46900), new Vec2(18350, 45800) },
            new[] { Gate("field_treant#0", "Sunken Vale West", "Lv 58-60 · trash flank, west of the boss", 20500, 45000),
                    Gate("field_treant#1", "Sunken Vale East", "Lv 58-60 · trash flank, east of the boss", 27500, 45000) }),

        // Hollow Crypt — the DUNGEON field (band 44-48), in the NEGATIVE quadrant (owner: dungeons live at
        // minus coords, off the overworld, reached by teleport). Elite rooms + the grave-lich boss; its
        // entrance safe zone (dungeon_hollow_crypt) sits just SW as a separate island.
        //
        // MANAGED BY GREYMARSH (44-48 sits inside Greymarsh's 40-60). It used to be managed by nobody,
        // which had two consequences that both read as bugs. Its gate appeared in NO gatekeeper's menu,
        // so the only way into the halls was to teleport to the entrance and walk. And dying in it fell
        // through to nearest-city — from a point at (-9600, -11000) that is a meaningless answer, since
        // every city is thousands of units away in the positive quadrant. Naming the city fixes both:
        // Greymarsh lists the halls, and the crypt returns its dead to the city that sent them.
        // (The boss vale is deliberately NOT given a city the same way: its band, 58-60, is the last two
        // levels of Greymarsh's range but it sits on Ironreach's doorstep, so band and geography
        // disagree and there is no obviously right answer to pick on the owner's behalf.)
        new("field_dungeon", "Hollow Crypt", RegionKind.Field,
            new[] { new Vec2(-11850, -11950), new Vec2(-11400, -12400), new Vec2(-10800, -12450), new Vec2(-7750, -11350), new Vec2(-6550, -10650), new Vec2(-6150, -10200), new Vec2(-6200, -9600), new Vec2(-6600, -9150), new Vec2(-7150, -9100), new Vec2(-8550, -9450), new Vec2(-11500, -10800), new Vec2(-11900, -11300) },
            new[] { Gate("field_dungeon#0", "Hollow Crypt Halls", "Lv 44-48 · elite rooms, all aggressive", -9600, -11000) },
            "town_greymarsh"),
    }).ToArray();

    private static TeleportPoint Gate(string id, string name, string description, float x, float y) =>
        new(id, name, description, new Vec2(x, y));

    /// <summary>Turn one PLANNED field into a Region: hull its camps (elite included, so the elite camp is
    /// inside its own field), carry its named gates across, and record the managing city.</summary>
    private static Region PlannedField(WorldPlan.Field field) =>
        new(field.Plan.Id, field.Plan.Name, RegionKind.Field,
            HullOf(field.Zones, WorldPlan.FieldMargin),
            field.Gates,
            field.Plan.CityId);

    /// <summary>
    /// TOWNS as regions (stage 2). Each is an OCTAGON INSCRIBED in its safe-zone circle (rad = r, so the
    /// drawn town sits just inside the safe radius). Safety is UNAFFECTED: `InAnySafeZone` unions the
    /// full circle with these polygons, so the circle — not the octagon — sets where you're safe. The
    /// octagon is only the drawn shape, kept snug against the circle so it no longer bleeds into the
    /// hunting fields around it (owner: regions must not overlap; towns were reading too large).
    /// </summary>
    /// Derived from <see cref="Game.Shared.Towns.All"/> rather than re-listed: the same seven ids,
    /// names, centres and radii were written out twice and had to keep agreeing.
    public static readonly Region[] Towns =
        Game.Shared.Towns.All.Select(z => Town(z.Id, z.Name, z.X, z.Y, z.Radius)).ToArray();

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
    /// <summary>The outline that WRAPS a set of camps: a convex hull of points sampled around each camp
    /// circle (radius + margin). A field simply IS "wherever its camps are, plus a margin", so the two can
    /// never disagree — which is what the old hand-authored dozen-vertex outlines could not promise. The
    /// shape hugs however the camps happen to be arranged: an arc of camps becomes a crescent.</summary>
    private static Vec2[] HullOf(SpawnZone[] zones, float margin)
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
        return ConvexHull(pts);
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
        return new Region(id, name, RegionKind.Town, outline,
                          new[] { new TeleportPoint(id, name, "City", new Vec2(cx, cy)) });
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

    // ── City ⇄ field ownership (owner, 2026-07-30) ────────────────────────────────────────────────
    // "Each field has its parent city (managing city), each city has its children (owned) fields." Two
    // things read this: the gatekeeper menu (a city lists the gates of the fields it owns, and only those)
    // and DEATH (you wake up in the city that manages where you fell, not in whatever town happens to be
    // nearest — which on a map with cities 13k apart could be a city whose gatekeeper cannot even send you
    // back). Nearest-city stays the FAILSAFE for the places no city manages: the boss vale, the dungeon,
    // and the empty ground between fields.

    /// <summary>The fields a city manages, ordered by their level band (lowest first) — the order a
    /// gatekeeper should list them in.</summary>
    public static Region[] FieldsOf(string cityId) =>
        Fields.Where(f => f.CityId == cityId)
              .OrderBy(f => LevelBand(f.Id)?.Min ?? int.MaxValue)
              .ToArray();

    /// <summary>The city that manages the field containing this point, or null when no field does (open
    /// ground, a town, the boss vale, a dungeon). Callers fall back to nearest-city.</summary>
    public static SafeZone? ManagingCity(float x, float y)
    {
        foreach (var f in Fields)
            if (f.CityId.Length > 0 && f.Contains(x, y))
                return Game.Shared.Towns.ById(f.CityId);
        return null;
    }

    private static readonly Dictionary<string, (TeleportPoint Gate, Region Field)> GateIndex =
        Regions.SelectMany(r => r.Gates.Select(g => (Gate: g, Field: r)))
               .ToDictionary(t => t.Gate.Id, t => t, StringComparer.Ordinal);

    /// <summary>A named teleport gate and the region it belongs to, by gate id.</summary>
    public static (TeleportPoint Gate, Region Field)? GateById(string id) =>
        GateIndex.TryGetValue(id, out var hit) ? hit : null;

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

        // A zone's AggressiveTypes must name mobs the zone actually SPAWNS. A typo there fails silently
        // and in the worst direction — the field simply turns peaceful, which looks like a design choice
        // rather than a mistake, and nobody notices until a playtest says "nothing attacks me here".
        var unknown = new List<string>();
        foreach (var z in WorldMap.SpawnZones)
        {
            if (z.AggressiveTypes is null) continue;
            foreach (var id in z.AggressiveTypes)
                if (Array.IndexOf(z.MobTypes, id) < 0)
                    unknown.Add($"({z.X:0},{z.Y:0}) Lv{z.MinLevel}-{z.MaxLevel}: '{id}' is not in this zone's roster");
        }
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                "AggressiveTypes naming a mob the zone doesn't spawn:\n  " + string.Join("\n  ", unknown));
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
