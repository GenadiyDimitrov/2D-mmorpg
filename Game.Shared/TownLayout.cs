using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>`BL-319` — what a town's ground LOOKS like: main roads, a plaza, side paths and building
/// footprints, and the GATES in its wall. See <see cref="TownLayout"/>.</summary>
public enum TownShape
{
    /// <summary>A minor town: three gates, a stem and two arms (Stonewatch, Ironreach).</summary>
    Y = 0,
    /// <summary>A major city: four gates, two roads crossing at the plaza (Brackenford, Greymarsh, Frostmere).</summary>
    X = 1,
}

/// <summary>What a footprint is — the client colours by it; the server does not read it.</summary>
public enum LotKind { House = 0, Shop = 1, Church = 2, Craft = 3, Yard = 4, Guild = 5, Shrine = 6 }

/// <summary>A rectangular footprint in WORLD coordinates: centre, width (along its own x), height, and the
/// rotation in degrees. The church is two of them crossed. <see cref="Label"/> is the building's name.</summary>
public readonly record struct TownLot(Vec2 Centre, float W, float H, float AngleDeg, LotKind Kind, string Label);

/// <summary>A straight strip of ground in WORLD coordinates — a main road or a side path to a door.</summary>
public readonly record struct TownStrip(Vec2 A, Vec2 B, float Width, bool Path);

/// <summary>A gate: where the road crosses the wall, and the OUTWARD direction (radians, server axes — +Y is
/// south). Its two guards stand either side of it (<see cref="WorldPlan"/>).</summary>
public readonly record struct TownGate(Vec2 Centre, float Angle);

/// <summary>One town's plan, all in world coordinates.</summary>
public sealed record TownPlan(string TownId, TownShape Shape, Vec2 Centre, float PlazaRadius,
                              TownStrip[] Roads, TownLot[] Lots, TownGate[] Gates);

/// <summary>
/// `BL-319` (owner, 2026-09-28): *"how can we make it so a town looks like a town .. some towns are Y shaped
/// road (so 3 gates) or a X(+) shaped with 4 gates .. the shops and houses are on the side of roads .. small
/// paths that lead to the shops"*. The sketch he approved is <c>docs/design/TownPlans.html</c>; his five
/// answers are in the Backlog's `BL-319` entry:
/// <list type="number">
/// <item>walls are VISUAL — footprints on the ground, walked through. Collision waits for the 3D models
///   (`BL-281`) and a server-side check (`BL-323`);</item>
/// <item>majors are X, Stonewatch and Ironreach are Y, each Y turned to face its fields — a proof of concept,
///   other shapes later;</item>
/// <item>the majors grow to radius 3000 (*"a major city requires u to walk for a bit"*);</item>
/// <item>NPCs stand AT THE DOOR (*"until maybe bl-281 lands"*);</item>
/// <item>the hunt lodge is the ADVENTURERS GUILD and the Huntmaster is its GUILD RECEPTIONIST.</item>
/// </list>
///
/// <para>🔑 Both shapes are authored ONCE, in LOCAL coordinates — town centre at 0,0, north up, +Y south,
/// the same numbers as the sketch — and placed per town by <see cref="At"/> (rotate, then translate). The
/// NPCs use the same named door points (<see cref="XDoor"/>, <see cref="YDoor"/>), so a building and the
/// person at its door cannot drift apart. Their Y values are staggered ≥ 200 on purpose:
/// <see cref="WorldMap.ValidateNpcLabels"/> still holds, and still fails the boot if a nudge breaks it.</para>
///
/// <para>Depends on <see cref="Towns"/> only, for the same static-initialisation reason Towns exists.</para>
/// </summary>
public static class TownLayout
{
    /// <summary>A major city's main road, and a minor town's.</summary>
    public const float XRoadWidth = 270f, YRoadWidth = 210f;

    /// <summary>A side path from a main road to a door.</summary>
    public const float PathWidth = 70f;

    /// <summary>Each town's shape and its turn, in degrees clockwise on the map. A Y's gates must face its
    /// fields: Stonewatch's stem points south at Brackenford with its arms toward the northern moors;
    /// Ironreach is the same Y turned 180°, stem north at Brackenford and its arms to the southern fields.</summary>
    private static (TownShape Shape, float Turn) ShapeOf(string townId) => townId switch
    {
        "town_stonewatch" => (TownShape.Y, 0f),
        "castle_ironreach" => (TownShape.Y, 180f),
        _ => (TownShape.X, 0f),
    };

    /// <summary>The five cities that get a plan — every <see cref="Towns.All"/> entry that is not the Training
    /// Outpost or a dungeon door.</summary>
    public static bool HasPlan(SafeZone z) => !z.DungeonEntrance && z.Id != "outpost_training";

    /// <summary>A local point (centre 0,0, north up, +Y south) in <paramref name="townId"/>, in world units.</summary>
    public static Vec2 At(string townId, Vec2 local)
    {
        var town = Towns.ById(townId) ?? throw new ArgumentException($"No town '{townId}'.");
        float t = ShapeOf(townId).Turn * MathF.PI / 180f;
        float c = MathF.Cos(t), s = MathF.Sin(t);
        return new Vec2(town.X + local.X * c - local.Y * s, town.Y + local.X * s + local.Y * c);
    }

    /// <summary>The local doors of an X city — where each NPC stands. The ≥ 200 Y stagger is deliberate
    /// (see the class summary): anything within 1500 on X must not share a screen line.</summary>
    public static class XDoor
    {
        public static Vec2 Gatekeeper => new(0, -100);          // the centre stone
        public static Vec2 Shrine => new(-330, 120);            // the plaza's edge, beside the pavilion
        // The CHURCH (north-west): a column at its east face, and one at its north door.
        public static Vec2 Church1 => new(-560, -1500);
        public static Vec2 Church2 => new(-560, -1100);
        public static Vec2 Church3 => new(-560, -700);
        public static Vec2 ChurchNorth => new(-1100, -1750);
        // The MARKET (north-east): Arms & Armour, then the Apothecary with the Assayer.
        public static Vec2 Arms1 => new(330, -1300);
        public static Vec2 Arms2 => new(330, -900);
        public static Vec2 Apothecary => new(330, -500);
        public static Vec2 Assayer => new(330, -300);
        // The KEEPER (south-east), and Frostmere's Ledgerkeep at its side door.
        public static Vec2 Keeper => new(650, 330);
        public static Vec2 KeeperSide => new(330, 560);
        // The CRAFTHALL (south-west): the Master at its SOUTH door with the Anvil yard behind him — exactly
        // GameConstants.TalkRange (250) apart, so both are in reach from between them (`BL-303`: *"anvil and master
        // are always togheter"*) — and three places at its west face.
        public static Vec2 Crafter => new(-940, 930);
        public static Vec2 Anvil => new(-940, 1180);
        public static Vec2 CraftWest1 => new(-1400, 450);
        public static Vec2 CraftWest2 => new(-1400, 700);
        public static Vec2 CraftWest3 => new(-1400, 1400);
        // The ADVENTURERS GUILD, up the north road.
        public static Vec2 Guild => new(370, -2200);
    }

    /// <summary>The local doors of a Y town (stem south, arms north-west and north-east).</summary>
    public static class YDoor
    {
        public static Vec2 Guild => new(0, -550);
        public static Vec2 Gatekeeper => new(0, -150);
        public static Vec2 Shrine => new(-370, 60);
        public static Vec2 Keeper => new(370, 280);
        public static Vec2 Apothecary => new(-170, 500);
        public static Vec2 Arms1 => new(170, 720);
        public static Vec2 Arms2 => new(170, 940);
    }

    /// <summary>Every city's plan. A METHOD-backed lazy, not a field initialiser: WorldMap and WorldPlan both
    /// read it while THEY are being built.</summary>
    public static IReadOnlyList<TownPlan> Plans => _plans ??= Towns.All.Where(HasPlan).Select(Build).ToArray();
    private static TownPlan[]? _plans;

    public static TownPlan? PlanOf(string townId) => Plans.FirstOrDefault(p => p.TownId == townId);

    /// <summary>`BL-324` — is (x,y) on a city's PAVING: the plaza, a main road or a side path? The roads end at
    /// the wall, so this is also "inside the walls". Owner, 2026-10-01: *"the 'Only Streets' effect idea is
    /// good"* — the street shapes were already here, so no geodata is needed for it.</summary>
    public static bool OnStreet(float x, float y)
    {
        var at = new Vec2(x, y);
        foreach (var plan in Plans)
        {
            // Cheap reject: nothing of a plan lies further from its centre than its longest road.
            float dx = x - plan.Centre.X, dy = y - plan.Centre.Y;
            if (dx * dx + dy * dy > 4000f * 4000f) continue;
            if (dx * dx + dy * dy <= plan.PlazaRadius * plan.PlazaRadius) return true;
            foreach (var s in plan.Roads)
                if (DistToSegment(at, s.A, s.B) <= s.Width / 2f) return true;
        }
        return false;
    }

    private static float DistToSegment(Vec2 p, Vec2 a, Vec2 b)
    {
        float vx = b.X - a.X, vy = b.Y - a.Y;
        float len2 = vx * vx + vy * vy;
        float t = len2 <= 0f ? 0f : Math.Clamp(((p.X - a.X) * vx + (p.Y - a.Y) * vy) / len2, 0f, 1f);
        float cx = a.X + t * vx - p.X, cy = a.Y + t * vy - p.Y;
        return MathF.Sqrt(cx * cx + cy * cy);
    }

    /// <summary>Where a road leaves town toward (tx,ty): the gate whose outward direction is closest to that
    /// bearing. The world's inter-city roads run centre → this gate → the other town's gate → its centre.</summary>
    public static Vec2 GateToward(string townId, float tx, float ty)
    {
        var plan = PlanOf(townId) ?? throw new ArgumentException($"No town plan '{townId}'.");
        float want = MathF.Atan2(ty - plan.Centre.Y, tx - plan.Centre.X);
        return plan.Gates.OrderBy(g => AngleGap(g.Angle, want)).First().Centre;
    }

    private static float AngleGap(float a, float b)
    {
        float d = MathF.Abs(a - b) % (2f * MathF.PI);
        return d > MathF.PI ? 2f * MathF.PI - d : d;
    }

    // ── Building ──────────────────────────────────────────────────────────────────────────────────

    private static TownPlan Build(SafeZone z)
    {
        var (shape, _) = ShapeOf(z.Id);
        // The wall is the octagon of RegionMap.Town, whose flat sides sit at r·cos 22.5° — a road ends there.
        float wall = z.Radius * MathF.Cos(MathF.PI / 8f);
        // Gate directions in LOCAL degrees (0 = east, 90 = south): the X's four sides, the Y's stem and arms.
        float[] gates = shape == TownShape.X ? new[] { 0f, 90f, 180f, 270f } : new[] { 90f, 225f, 315f };
        float roadW = shape == TownShape.X ? XRoadWidth : YRoadWidth;

        Vec2 P(float x, float y) => At(z.Id, new Vec2(x, y));
        var roads = new List<TownStrip>();
        var gateList = new List<TownGate>();
        float turn = ShapeOf(z.Id).Turn;
        foreach (float g in gates)
        {
            float a = g * MathF.PI / 180f;
            var end = P(wall * MathF.Cos(a), wall * MathF.Sin(a));
            roads.Add(new TownStrip(P(0, 0), end, roadW, Path: false));
            gateList.Add(new TownGate(end, (g + turn) * MathF.PI / 180f));
        }

        var lots = new List<TownLot>();
        TownLot Lot(float x0, float y0, float x1, float y1, LotKind kind, string label = "", float angle = 0f) =>
            new(P((x0 + x1) / 2f, (y0 + y1) / 2f), x1 - x0, y1 - y0, angle + turn, kind, label);
        TownLot Rot(float cx, float cy, float w, float h, float angle) =>
            new(P(cx, cy), w, h, angle + turn, LotKind.House, "");
        void Path(float x0, float y0, float x1, float y1) =>
            roads.Add(new TownStrip(P(x0, y0), P(x1, y1), PathWidth, Path: true));

        float plaza;
        if (shape == TownShape.X)
        {
            plaza = 480f;
            // Church (a cross), market, keeper, crafthall + anvil yard, shrine pavilion, the guild.
            lots.Add(Lot(-1260, -1650, -960, -550, LotKind.Church, "Church"));
            lots.Add(Lot(-1610, -1300, -610, -900, LotKind.Church));
            lots.Add(Lot(380, -1500, 1100, -850, LotKind.Shop, "Arms & Armour"));
            lots.Add(Lot(380, -700, 920, -260, LotKind.Shop, "Apothecary"));
            lots.Add(Lot(380, 380, 920, 820, LotKind.Shop, "Keeper"));
            lots.Add(Lot(-1350, 380, -530, 880, LotKind.Craft, "Crafthall"));
            lots.Add(Lot(-1150, 1000, -730, 1350, LotKind.Yard));
            lots.Add(Rot(-360, 300, 220, 220, 45f) with { Kind = LotKind.Shrine, Label = "Shrine" });
            lots.Add(Lot(420, -2400, 900, -2000, LotKind.Guild, "Adventurers Guild"));
            // Houses, off the roads, in every quarter.
            lots.Add(Lot(1300, -1500, 1680, -1200, LotKind.House));
            lots.Add(Lot(1400, -900, 1740, -610, LotKind.House));
            lots.Add(Lot(1150, 450, 1510, 750, LotKind.House));
            lots.Add(Lot(1250, 950, 1610, 1250, LotKind.House));
            lots.Add(Lot(450, 1300, 830, 1600, LotKind.House));
            lots.Add(Lot(-2000, 1350, -1640, 1650, LotKind.House));
            lots.Add(Lot(-800, 1500, -440, 1800, LotKind.House));
            lots.Add(Lot(-2150, -500, -1810, -220, LotKind.House));
            lots.Add(Lot(-600, -2300, -280, -2020, LotKind.House));
            lots.Add(Lot(450, 2000, 790, 2280, LotKind.House));
            lots.Add(Lot(-800, 2050, -460, 2330, LotKind.House));
            // Side paths from the main roads to the doors.
            Path(-135, -1100, -610, -1100);
            Path(135, -1100, 380, -1100);
            Path(135, -450, 380, -450);
            Path(650, 135, 650, 380);
            Path(-135, 930, -940, 930);
            Path(135, -2200, 420, -2200);
        }
        else
        {
            plaza = 330f;
            // The guild up the north path, the shrine and the keeper in the side wedges, shops on the stem.
            lots.Add(Lot(-230, -1000, 230, -600, LotKind.Guild, "Adventurers Guild"));
            lots.Add(Lot(-760, -80, -420, 220, LotKind.Shrine, "Shrine"));
            lots.Add(Lot(420, 130, 760, 430, LotKind.Shop, "Keeper"));
            lots.Add(Lot(-560, 380, -220, 680, LotKind.Shop, "Apothecary"));
            lots.Add(Lot(220, 600, 660, 1020, LotKind.Shop, "Arms & Armour"));
            // Houses down the stem and in the wedges, turned to face their arm.
            lots.Add(Lot(240, 1150, 500, 1370, LotKind.House));
            lots.Add(Lot(240, 1450, 480, 1650, LotKind.House));
            lots.Add(Lot(-500, 800, -220, 1020, LotKind.House));
            lots.Add(Lot(-480, 1150, -220, 1350, LotKind.House));
            lots.Add(Lot(-500, 1450, -240, 1650, LotKind.House));
            lots.Add(Rot(-1000, -460, 240, 200, 45f));
            lots.Add(Rot(1000, -460, 240, 200, -45f));
            lots.Add(Rot(-620, -1000, 230, 190, 45f));
            lots.Add(Rot(620, -1000, 230, 190, -45f));
            Path(0, -330, 0, -600);
            Path(-105, 60, -420, 60);
            Path(105, 280, 420, 280);
            Path(-105, 530, -220, 530);
            Path(105, 820, 220, 820);
        }

        return new TownPlan(z.Id, shape, new Vec2(z.X, z.Y), plaza, roads.ToArray(), lots.ToArray(), gateList.ToArray());
    }
}
