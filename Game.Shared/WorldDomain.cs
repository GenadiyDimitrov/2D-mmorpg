using System;
using System.Collections.Generic;

namespace Game.Shared;

/// <summary>What KIND of world a point sits in. The owner's word for this is a "world"; the code has
/// called it a domain since the first walls landed, so the name stays.</summary>
public enum DomainKind
{
    /// <summary>The positive quadrant — the whole overworld map.</summary>
    Overworld = 0,
    /// <summary>One dungeon's bounding box, in the negative quadrant.</summary>
    Dungeon = 1,
    /// <summary>The jail cell — a circle, and a world of its own (not a dungeon).</summary>
    Jail = 2,
    /// <summary>The empty negative quadrant with no dungeon in it. Nothing should ever stand here;
    /// it exists so every point has an answer.</summary>
    Void = 3,
}

/// <summary>
/// A WORLD and its bounds — the ONE definition of "where may this position walk to".
///
/// <para>This used to live only in <c>GameLoopService.ConfineToDomain</c>, which made the server's
/// rubber-band the only thing in the game that knew where a wall was. The owner's architecture call
/// (2026-07-24) is a split: the CLIENT stops you at the surface and never emits an out-of-world
/// coordinate, and the SERVER keeps its clamp as the anti-cheat backstop. Two halves enforcing the
/// same rule is only safe if they cannot disagree — so the rule is here, in Game.Shared, and both
/// halves call it. Do NOT re-derive a bound at a call site.</para>
///
/// <para>Shape: a box; a CIRCLE when <see cref="Radius"/> &gt; 0 and there is no outline (the jail); or
/// a POLYGON when <see cref="Outline"/> is set (a dungeon), optionally ANNEXED to one circle — its
/// entrance safe zone. Value equality is the "is this the same world?" test — <c>At(a) != At(b)</c>
/// means a teleport is the only way across.</para>
/// </summary>
public readonly record struct WorldDomain(
    DomainKind Kind,
    string Id,
    string Name,
    float MinX, float MinY, float MaxX, float MaxY,
    float CentreX = 0f, float CentreY = 0f, float Radius = 0f,
    Vec2[]? Outline = null)
{
    /// <summary>A circle-shaped world — the jail. A dungeon also carries a radius (its entrance), but
    /// that circle is an ANNEX to its outline, not the whole world.</summary>
    public bool IsCircle => Radius > 0f && Outline is null;

    /// <summary>How far inside the wall a clamped point is placed. Landing exactly ON an edge fails the
    /// containment test it just satisfied once the float is rounded, which reads as a stuck player.</summary>
    private const float WallInset = 2f;

    /// <summary>The overworld: the positive quadrant, sealed at the zone rectangle.</summary>
    public static readonly WorldDomain Overworld = new(
        DomainKind.Overworld, "overworld", "the overworld",
        0f, 0f, GameConstants.ZoneWidth, GameConstants.ZoneHeight);

    /// <summary>The jail YARD — one shared room 300 × 500, and its OWN world, so a visiting admin is
    /// confined to it instead of being read as someone loose in the negative quadrant and dragged to the
    /// nearest dungeon. It was a circle; the owner asked for a rectangular room you can pace
    /// (playtest-20 `61d`), which is a plain box domain — no radius, no special case.</summary>
    public static readonly WorldDomain Jail = new(
        DomainKind.Jail, "jail", "the jail",
        GameConstants.JailX - GameConstants.JailWidth / 2f, GameConstants.JailY - GameConstants.JailHeight / 2f,
        GameConstants.JailX + GameConstants.JailWidth / 2f, GameConstants.JailY + GameConstants.JailHeight / 2f);

    /// <summary>The empty negative quadrant — the fallback when there are no dungeons at all. Bounded
    /// away from the overworld so a fall-through can never leak someone onto the map.</summary>
    public static readonly WorldDomain Void = new(
        DomainKind.Void, "void", "nowhere",
        GameConstants.WorldMinX, GameConstants.WorldMinY, 0f, 0f);

    /// <summary>A dungeon's world: its own OUTLINE, plus the entrance safe zone that serves it.
    ///
    /// <para>It used to be the outline's BOUNDING BOX, which is not the dungeon (owner, playtest-20
    /// `61h`: *"the Hollow Crypt has no walls"*). The crypt is a narrow diagonal band; its box is
    /// 5750 × 3350, so roughly three quarters of the "walled" area is empty ground outside the
    /// dungeon, and walking there is exactly what he did. Both halves clamp to the polygon now.</para>
    ///
    /// <para>The ENTRANCE has to come with it. The crypt's arrival safe zone sits at (-12000, -12000),
    /// 100 units outside even the old box — so a player who teleported in was standing outside their
    /// own world and got pulled off the doorstep. It is annexed rather than merged into the authored
    /// outline because the outline is also the DRAWN region, and regions must not overlap the town
    /// shape drawn for that same safe zone.</para>
    ///
    /// <para>KNOWN AND ACCEPTED: a move order is a straight line to the clamped destination, so a walk
    /// across a concave notch can cut the corner. Measured on the crypt: 0.76% of point pairs, worst
    /// excursion 129 units. It never rubber-bands — client and server draw the same line from the same
    /// geometry — and routing around it is pathfinding, which the game does not have.</para></summary>
    public static WorldDomain OfDungeon(Region d)
    {
        var (cx, cy, r) = EntranceOf(d);
        // Bounds cover the union, so the cheap box reject never discards a point standing in the annex.
        float minX = d.MinX, minY = d.MinY, maxX = d.MaxX, maxY = d.MaxY;
        if (r > 0f)
        {
            minX = MathF.Min(minX, cx - r); maxX = MathF.Max(maxX, cx + r);
            minY = MathF.Min(minY, cy - r); maxY = MathF.Max(maxY, cy + r);
        }
        return new(DomainKind.Dungeon, d.Id, d.Name, minX, minY, maxX, maxY, cx, cy, r, d.Outline);
    }

    /// <summary>The safe zone that serves a dungeon: the nearest one in the NEGATIVE quadrant whose
    /// circle actually touches the dungeon's box. "Touches" is the whole test — an entrance a player
    /// cannot walk out of into the dungeon would be a trap, and a distant safe zone is not an entrance.
    /// Resolved once per dungeon; the jail is excluded, it is a world of its own.</summary>
    private static readonly Dictionary<string, (float X, float Y, float R)> Entrances = BuildEntrances();

    private static Dictionary<string, (float, float, float)> BuildEntrances()
    {
        var map = new Dictionary<string, (float, float, float)>();
        foreach (var d in RegionMap.Dungeons)
        {
            float bestSq = float.MaxValue;
            foreach (var z in Towns.All)
            {
                if (z.X >= 0f && z.Y >= 0f) continue;
                float dx = MathF.Max(0f, MathF.Max(d.MinX - z.X, z.X - d.MaxX));
                float dy = MathF.Max(0f, MathF.Max(d.MinY - z.Y, z.Y - d.MaxY));
                float sq = dx * dx + dy * dy;
                if (sq > z.Radius * z.Radius || sq >= bestSq) continue;
                bestSq = sq;
                map[d.Id] = (z.X, z.Y, z.Radius);
            }
        }
        return map;
    }

    private static (float X, float Y, float R) EntranceOf(Region d) =>
        Entrances.TryGetValue(d.Id, out var e) ? e : (0f, 0f, 0f);

    /// <summary>The world a point is IN. Order matters: the jail is tested first because it sits in
    /// the negative quadrant and would otherwise be mistaken for dungeon ground.</summary>
    public static WorldDomain At(float x, float y)
    {
        if (Jail.Contains(x, y)) return Jail;
        if (x >= 0f && y >= 0f) return Overworld;

        // Negative quadrant: the dungeon you are in, or — if you have somehow ended up between them —
        // the nearest one, so the clamp still has somewhere legitimate to put you.
        var d = RegionMap.DungeonAt(x, y) ?? RegionMap.NearestDungeon(x, y);
        return d != null ? OfDungeon(d) : Void;
    }

    public bool Contains(float x, float y)
    {
        if (IsCircle) return InCircle(x, y);
        if (Outline is { Length: > 2 })
            return InPolygon(x, y) || (Radius > 0f && InCircle(x, y));
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    /// <summary>Pull a point back onto this world — the "stop at the surface" operation. A circle keeps
    /// the point's direction from the centre; a box clamps each axis; a polygon walks to the nearest
    /// point on its boundary (or on the annexed entrance circle, whichever is closer).</summary>
    public (float X, float Y) Clamp(float x, float y)
    {
        if (IsCircle) return ClampToCircle(x, y);
        if (Outline is { Length: > 2 })
        {
            if (Contains(x, y)) return (x, y);

            var (px, py) = NearestOnOutline(x, y);
            if (Radius > 0f)
            {
                var (cxp, cyp) = ClampToCircle(x, y);
                if (Sq(x - cxp, y - cyp) < Sq(x - px, y - py)) (px, py) = (cxp, cyp);
            }
            return (px, py);
        }
        return (Math.Clamp(x, MinX, MaxX), Math.Clamp(y, MinY, MaxY));
    }

    /// <summary>How far OUTSIDE this world the point lies; 0 when inside. Measured against the world's
    /// real shape, so a dungeon reports the distance to its outline rather than to a bounding box that
    /// is mostly not the dungeon.</summary>
    public float DistanceOutside(float x, float y)
    {
        if (Contains(x, y)) return 0f;
        var (cx, cy) = Clamp(x, y);
        return MathF.Sqrt(Sq(x - cx, y - cy));
    }

    private static float Sq(float dx, float dy) => dx * dx + dy * dy;

    private bool InCircle(float x, float y) => Sq(x - CentreX, y - CentreY) <= Radius * Radius;

    /// <summary>Circle clamp, pulled <see cref="WallInset"/> inside the rim rather than onto it.</summary>
    private (float X, float Y) ClampToCircle(float x, float y)
    {
        float dx = x - CentreX, dy = y - CentreY;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist <= Radius) return (x, y);
        float k = MathF.Max(0f, Radius - WallInset) / dist;
        return (CentreX + dx * k, CentreY + dy * k);
    }

    /// <summary>Ray-crossing containment, the same test <see cref="Region.Contains"/> uses — a dungeon's
    /// outline is concave, which is the reason its box was never a wall.</summary>
    private bool InPolygon(float x, float y)
    {
        if (x < MinX || x > MaxX || y < MinY || y > MaxY) return false;
        var poly = Outline!;
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].Y > y) != (poly[j].Y > y) &&
                x < (poly[j].X - poly[i].X) * (y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>The closest point on the outline's boundary, nudged inward along the edge normal so the
    /// result is INSIDE the polygon rather than exactly on it.</summary>
    private (float X, float Y) NearestOnOutline(float x, float y)
    {
        var poly = Outline!;
        float bestSq = float.MaxValue, bx = x, by = y;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            float ax = poly[j].X, ay = poly[j].Y;
            float ex = poly[i].X - ax, ey = poly[i].Y - ay;
            float len = ex * ex + ey * ey;
            float t = len <= 0f ? 0f : Math.Clamp(((x - ax) * ex + (y - ay) * ey) / len, 0f, 1f);
            float qx = ax + ex * t, qy = ay + ey * t;
            float sq = Sq(x - qx, y - qy);
            if (sq < bestSq) { bestSq = sq; bx = qx; by = qy; }
        }

        // Step from the boundary toward the point's own side of the wall — inward, since we only get
        // here when the point is outside. Falls back to the raw boundary point if the inset overshoots
        // a thin part of the outline, which is better than reporting a position outside the world.
        float dx = bx - x, dy = by - y;
        float d = MathF.Sqrt(dx * dx + dy * dy);
        if (d <= 0f) return (bx, by);
        float ix = bx + dx / d * WallInset, iy = by + dy / d * WallInset;
        return InPolygon(ix, iy) ? (ix, iy) : (bx, by);
    }
}
