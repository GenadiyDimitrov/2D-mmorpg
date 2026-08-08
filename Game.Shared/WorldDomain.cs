using System;

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
/// <para>Shape: a box, or a CIRCLE when <see cref="Radius"/> &gt; 0 (the jail). Value equality is the
/// "is this the same world?" test — <c>At(a) != At(b)</c> means a teleport is the only way across.</para>
/// </summary>
public readonly record struct WorldDomain(
    DomainKind Kind,
    string Id,
    string Name,
    float MinX, float MinY, float MaxX, float MaxY,
    float CentreX = 0f, float CentreY = 0f, float Radius = 0f)
{
    public bool IsCircle => Radius > 0f;

    /// <summary>The overworld: the positive quadrant, sealed at the zone rectangle.</summary>
    public static readonly WorldDomain Overworld = new(
        DomainKind.Overworld, "overworld", "the overworld",
        0f, 0f, GameConstants.ZoneWidth, GameConstants.ZoneHeight);

    /// <summary>The jail cell. A circle, so a sentence feels like a cell you can pace rather than a
    /// box; and its OWN world, so a visiting admin is confined to it instead of being read as someone
    /// loose in the negative quadrant and dragged to the nearest dungeon.</summary>
    public static readonly WorldDomain Jail = new(
        DomainKind.Jail, "jail", "the jail",
        GameConstants.JailX - GameConstants.JailRadius, GameConstants.JailY - GameConstants.JailRadius,
        GameConstants.JailX + GameConstants.JailRadius, GameConstants.JailY + GameConstants.JailRadius,
        GameConstants.JailX, GameConstants.JailY, GameConstants.JailRadius);

    /// <summary>The empty negative quadrant — the fallback when there are no dungeons at all. Bounded
    /// away from the overworld so a fall-through can never leak someone onto the map.</summary>
    public static readonly WorldDomain Void = new(
        DomainKind.Void, "void", "nowhere",
        GameConstants.WorldMinX, GameConstants.WorldMinY, 0f, 0f);

    public static WorldDomain OfDungeon(Region d) =>
        new(DomainKind.Dungeon, d.Id, d.Name, d.MinX, d.MinY, d.MaxX, d.MaxY);

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
        if (IsCircle)
        {
            float dx = x - CentreX, dy = y - CentreY;
            return dx * dx + dy * dy <= Radius * Radius;
        }
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    /// <summary>Pull a point back onto this world — the "stop at the surface" operation. A circle keeps
    /// the point's direction from the centre; a box clamps each axis.</summary>
    public (float X, float Y) Clamp(float x, float y)
    {
        if (IsCircle)
        {
            float dx = x - CentreX, dy = y - CentreY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist <= Radius) return (x, y);
            float k = Radius / dist;
            return (CentreX + dx * k, CentreY + dy * k);
        }
        return (Math.Clamp(x, MinX, MaxX), Math.Clamp(y, MinY, MaxY));
    }
}
