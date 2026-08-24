using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>
/// THE SHAPE OF A DUNGEON — one main corridor, side rooms off it, a boss chamber at the end.
///
/// <para>Owner, 2026-08-24: *"can we make the dungeons(valid for all) with one main cooridor few side
/// rooms for mobs — if 3 mob groups -> 2 rooms and the last one is protecting the boss as of now …
/// number of mobs groups -1 is the rooms on the sides .. and in the end of cooridor is the boss with
/// the last group upfont (far enought so u can go trough and atack the boss without newly spawned
/// elites to aggro u - same as now)."* That is the whole specification, and it is a RULE about counts
/// rather than a drawing: <b>N mob groups ⇒ N−1 side rooms, and group N stands in the corridor in
/// front of the boss.</b> So it is generated here, from the group list, instead of being three
/// hand-drawn polygons that have to be kept agreeing with three hand-placed rings of spawners.</para>
///
/// <para>Before this, a dungeon was four circles on a diagonal inside a hand-authored band — no
/// corridor, no rooms, nothing to walk down. The three dungeons were literally the same twelve-vertex
/// outline translated 10k and 22k SW, with their spawners re-typed by hand at the matching offsets.
/// Change the room count there and you re-draw a polygon by eye; change it here and the outline, the
/// spawners, the arrival gate and the wall all move together, because all four are read off the same
/// numbers.</para>
///
/// <para>⚠ THIS TYPE DEPENDS ON NOTHING. <see cref="Towns"/> reads its entrance safe zones from here,
/// <see cref="WorldMap"/> its spawners, <see cref="RegionMap"/> its field polygons — so a static field
/// initialiser here that touched any of them would rebuild the very cycle Towns was extracted to break
/// (see the comment on <see cref="Towns"/>). Everything below is literals and arithmetic.</para>
/// </summary>
public static class DungeonLayout
{
    // ===== The corridor, in DUNGEON-LOCAL units =====================================================
    // Local frame: `a` runs ALONG the corridor from the entrance safe zone's centre, `b` runs ACROSS it
    // (positive = left of the walking direction). Everything below is authored in that frame and rotated
    // into the world by Place(), so a dungeon can point in any direction without re-deriving a vertex.

    /// <summary>Half the corridor's width — the corridor is 600 units across. Wide enough for a party to
    /// walk abreast and for a 250-radius guard camp to sit in it, narrow enough that it reads as a
    /// corridor rather than a field with bumps.</summary>
    private const float CorridorHalfWidth = 300f;

    /// <summary>Where the polygon starts, measured from the entrance safe zone's CENTRE. Deliberately
    /// less than the entrance radius (500) so the corridor mouth OVERLAPS the entrance circle: the two
    /// shapes are unioned as one world (<see cref="WorldDomain.OfDungeon"/> annexes the circle), and a
    /// gap between them would be a wall across the only door.</summary>
    private const float CorridorStart = 300f;

    /// <summary>Where a gatekeeper's jump lands you: just inside the entrance circle, on the corridor's
    /// centre line, facing in. 🔑 This is the fix for *"Entering trough GK teleports me in the middle …
    /// not the start"* — the gate used to sit on the SECOND spawn ring, which was the middle of the
    /// dungeon by construction.</summary>
    private const float MouthAt = 450f;

    /// <summary>Distance from the entrance to the FIRST side room's centre, then the pitch between
    /// consecutive rooms. A room is 900 long, so a 1400 pitch leaves 500 units of plain corridor between
    /// two rooms — you can always see which door you are walking past.</summary>
    private const float FirstRoomAt = 1500f;
    private const float RoomPitch = 1400f;

    /// <summary>A side room: 900 along the corridor, 750 deep out from the corridor wall.</summary>
    private const float RoomHalfLength = 450f;
    private const float RoomDepth = 750f;

    /// <summary>Corridor run from the last room's far wall to the guard camp, and from the guard camp to
    /// the corridor's end (the boss chamber's mouth).</summary>
    private const float GuardRun = 900f;
    private const float CorridorTail = 700f;

    /// <summary>The boss chamber is a square room 1400 × 1400 at the corridor's end.</summary>
    private const float BossChamberHalf = 700f;

    // ===== Spawn radii ==============================================================================

    /// <summary>A side room's mob circle. 300 inside a 900 × 750 room keeps every spawn point off the
    /// walls, so nothing is ever pushed through one by the domain clamp.</summary>
    private const float RoomSpawnRadius = 300f;

    /// <summary>The guard camp's circle, in the corridor — smaller than <see cref="CorridorHalfWidth"/>
    /// so the whole camp fits between the walls.</summary>
    private const float GuardSpawnRadius = 250f;

    /// <summary>The boss's own circle inside its chamber.</summary>
    private const float BossSpawnRadius = 300f;

    /// <summary>Clear ground between the guard camp's near edge and the boss's, in units.
    ///
    /// <para>🔑 THIS IS THE NUMBER HIS RULE IS ABOUT: *"far enought so u can go trough and atack the boss
    /// without newly spawned elites to aggro u"*. It works out at
    /// <c>CorridorTail + BossChamberHalf − GuardSpawnRadius − BossSpawnRadius</c> = 850, against a
    /// <see cref="GameConstants.MobAggroRange"/> of 400 — so an elite that respawns while you are on the
    /// boss is still more than twice its own aggro radius away from you. <see cref="Validate"/> asserts
    /// it at startup rather than trusting the arithmetic to survive the next edit.</para></summary>
    public static float GuardToBossClearance =>
        CorridorTail + BossChamberHalf - GuardSpawnRadius - BossSpawnRadius;

    /// <summary>An upper bound on how far OUTSIDE the outline a straight walk between two points that
    /// are both inside the dungeon can stray.
    ///
    /// <para>🔑 A CORRIDOR WITH ROOMS IS DEEPLY CONCAVE, AND THIS GAME HAS NO PATHFINDING. A move order
    /// is a straight line to a destination that is clamped ONCE, at the far end
    /// (<c>GameLoopService.ConfineToDomain</c>); nothing walks the line. So a tap from one side room into
    /// the one across the corridor clips the wall corner between them — client and server draw the same
    /// line from the same geometry, so it never rubber-bands, but it does cut. Measured on the crypt over
    /// 400,000 random point pairs: 40% cut a corner at all, worst excursion 683 units. The old diagonal
    /// band cut on 0.76% of pairs by at most 129, so this is the price of the shape he asked for, and it
    /// is paid in the notches between rooms rather than anywhere on the route down the corridor (the
    /// entrance → room → room → guard → boss walk peaks at 102).</para>
    ///
    /// <para>⚠ IT HAS ONE HARD CONSUMER: the dungeon WARD (<c>GameLoopService.EnforceDungeonWalls</c>),
    /// which teleports anyone found too far outside a dungeon back to its entrance. Its 500-unit
    /// tolerance is smaller than a legitimate corner cut, so without this the ward would have yanked a
    /// player to the door on roughly 0.8% of cross-room walks — an anti-cheat net firing on ordinary
    /// movement, which is the worst kind of bug to diagnose. The bound is deliberately generous
    /// (room depth plus corridor half-width, 1050 against a measured 683): the ward is a safety net for
    /// broken geodata, not the wall itself, and 1050 units off a dungeon is still nowhere.</para></summary>
    public const float MaxCornerCut = RoomDepth + CorridorHalfWidth;

    // ===== The plans ================================================================================

    /// <summary>One mob group. N of these produce N−1 side rooms plus the guard camp in front of the
    /// boss — the group list IS the floor plan.</summary>
    public sealed record Group(string[] MobTypes, int MinLevel, int MaxLevel, int MaxCount,
                               double RespawnSeconds = 60, double RespawnVariance = 15);

    /// <summary>The boss at the end of the corridor.</summary>
    public sealed record Boss(string[] MobTypes, int MinLevel, int MaxLevel,
                              double RespawnSeconds = 30 * 60, double RespawnVariance = 5 * 60,
                              bool ForceZoneLevel = false);

    /// <summary>One dungeon: where its door is, which way the corridor runs from it, and who lives in it.
    /// <paramref name="DirX"/>/<paramref name="DirY"/> need not be normalised.</summary>
    public sealed record Plan(
        string Id, string Name,
        string EntranceId, string EntranceName,
        float EntranceX, float EntranceY, float EntranceRadius,
        string CityId,
        float DirX, float DirY,
        string GateName,
        Group[] Groups, Boss Boss)
    {
        /// <summary>The line a gatekeeper's menu shows under this dungeon's name. DERIVED, not authored:
        /// the band is the groups' own levels and the room count is the group count minus one, so a
        /// roster edit cannot leave the menu advertising a dungeon that no longer exists. It used to be
        /// a literal string beside the rosters, and it was already one revision stale.</summary>
        public string GateDescription
        {
            get
            {
                int lo = Groups.Min(g => g.MinLevel), hi = Groups.Max(g => g.MaxLevel);
                int rooms = Math.Max(0, Groups.Length - 1);
                string band = lo == hi ? $"Lv {lo}" : $"Lv {lo}-{hi}";
                string plan = rooms == 1 ? "one side room off the corridor"
                                         : $"{rooms} side rooms off the corridor";
                return $"{band} · {plan}, all aggressive · Lv {Boss.MaxLevel} boss at the end";
            }
        }
    }

    /// <summary>The three dungeons (BL-65: *"put them in the lvl ranges and make 2 more dungeons"* — a
    /// 40, a 60 and an 85). They share the corridor direction and differ only in where the door is and
    /// who lives inside; the shape now comes from the group COUNT, so giving one of them a fourth group
    /// gives it a third side room and nothing else has to be touched.
    ///
    /// ⚠ THE BAND IS THE POINT (BL-65). A mob with a natural level brings its own, so a room's
    /// Min/Max is only a label unless the roster's own levels agree with it — every roster below is
    /// stocked with creatures whose natural level sits in the band. The one exception is the Sepulchre's
    /// boss, where nothing is authored above 85 and <c>ForceZoneLevel</c> makes the zone win.</summary>
    public static readonly Plan[] All =
    {
        // --- Hollow Crypt (~40, boss 44). Keeps its door, its lich and its NE-running corridor. ---
        new("field_dungeon", "Hollow Crypt",
            "dungeon_hollow_crypt", "Hollow Crypt", -12000f, -12000f, 500f,
            "town_greymarsh", 0.9231f, 0.3846f, "Hollow Crypt Halls",
            new[]
            {
                new Group(new[] { "fen_lizardman_archer", "dune_orc_archer" }, 39, 40, 6),
                new Group(new[] { "harpy" },                                  42, 42, 6),
                new Group(new[] { "ridge_orc_overlord" },                     42, 42, 5, 90, 20),
            },
            new Boss(new[] { "grave_lich" }, 44, 44)),

        // --- Sunless Warrens (~60, boss 65). ---
        new("field_dungeon_warrens", "Sunless Warrens",
            "dungeon_sunless_warrens", "Sunless Warrens", -22000f, -22000f, 500f,
            "castle_ironreach", 0.9231f, 0.3846f, "Sunless Warrens Depths",
            new[]
            {
                new Group(new[] { "hollow_one", "sand_ratman" },        58, 60, 6),
                new Group(new[] { "cursed_blade", "fen_lizardman" },    61, 62, 6),
                new Group(new[] { "obsidian_knight", "crimson_drake" }, 63, 64, 5, 90, 20),
            },
            new Boss(new[] { "dread_knight" }, 65, 65)),

        // --- Ashen Sepulchre (~85, boss 90). The boss is the ONE spawner in any dungeon that forces its
        //     level: nothing is authored above 85, and 90 is the number he asked for. ---
        new("field_dungeon_sepulchre", "Ashen Sepulchre",
            "dungeon_ashen_sepulchre", "Ashen Sepulchre", -34000f, -34000f, 500f,
            "town_frostmere", 0.9231f, 0.3846f, "Ashen Sepulchre Vaults",
            new[]
            {
                new Group(new[] { "wrathborn_demon", "scarlet_mantis", "radiant_berserker" }, 80, 82, 6),
                new Group(new[] { "splinter_mantis_walker", "needle_mantis_overseer" },       83, 84, 6),
                new Group(new[] { "drake_leader", "disciple_of_the_dawn" },                   85, 85, 5, 90, 20),
            },
            new Boss(new[] { "disciple_of_the_dawn" }, 90, 90, ForceZoneLevel: true)),
    };

    // ===== The generated shape ======================================================================

    /// <summary>A dungeon's geometry, in WORLD coordinates: the outline that walls it, the spot a
    /// gatekeeper lands you on, one centre per side room, the guard camp in front of the boss, and the
    /// boss itself.</summary>
    public sealed record Shape(Vec2[] Outline, Vec2 Mouth, Vec2[] Rooms, Vec2 Guard, Vec2 Boss);

    private static readonly Dictionary<string, Shape> Shapes =
        All.ToDictionary(p => p.Id, Build, StringComparer.Ordinal);

    /// <summary>The generated geometry of one dungeon.</summary>
    public static Shape ShapeOf(Plan plan) => Shapes[plan.Id];

    /// <summary>Where along the corridor each side room sits, and where the guard camp and boss do.
    /// Split out so <see cref="Build"/> reads as a trace of the outline rather than as arithmetic.</summary>
    private static (float[] Rooms, float Guard, float End, float Boss) Stations(int groupCount)
    {
        int rooms = Math.Max(0, groupCount - 1);
        var at = new float[rooms];
        for (int i = 0; i < rooms; i++) at[i] = FirstRoomAt + i * RoomPitch;

        // A one-group dungeon has no side rooms at all; the corridor then runs from its mouth straight
        // to the guard camp. Nothing authored is shaped that way, but the geometry must not depend on it.
        float lastWall = rooms > 0 ? at[rooms - 1] + RoomHalfLength : CorridorStart;
        float guard = lastWall + GuardRun;
        float end = guard + CorridorTail;
        return (at, guard, end, end + BossChamberHalf);
    }

    private static Shape Build(Plan plan)
    {
        // Unit vectors: `u` along the corridor, `v` its LEFT normal. (u, v) is a right-handed rotation of
        // the world axes, so a counter-clockwise trace in local coordinates stays counter-clockwise in
        // the world — which is what Region's outlines are authored as.
        float len = MathF.Sqrt(plan.DirX * plan.DirX + plan.DirY * plan.DirY);
        float ux = plan.DirX / len, uy = plan.DirY / len;
        float vx = -uy, vy = ux;
        Vec2 Place(float a, float b) =>
            new(plan.EntranceX + ux * a + vx * b, plan.EntranceY + uy * a + vy * b);

        var (rooms, guard, end, boss) = Stations(plan.Groups.Length);

        // Rooms alternate sides down the corridor — the first on the LEFT, the second on the RIGHT, and
        // so on. Alternating rather than lining them up on one wall is what keeps a straight run from
        // the door to the boss impossible to take without walking past every door.
        static float Side(int i) => (i & 1) == 0 ? 1f : -1f;

        // ---- Trace the outline counter-clockwise: out along the RIGHT wall (bumping out around every
        //      room on that side), around the boss chamber, and back along the LEFT wall.
        var poly = new List<Vec2>();
        poly.Add(Place(CorridorStart, -CorridorHalfWidth));
        for (int i = 0; i < rooms.Length; i++)
        {
            if (Side(i) > 0f) continue;                       // left-hand room, picked up on the way back
            float a = rooms[i];
            poly.Add(Place(a - RoomHalfLength, -CorridorHalfWidth));
            poly.Add(Place(a - RoomHalfLength, -CorridorHalfWidth - RoomDepth));
            poly.Add(Place(a + RoomHalfLength, -CorridorHalfWidth - RoomDepth));
            poly.Add(Place(a + RoomHalfLength, -CorridorHalfWidth));
        }
        poly.Add(Place(end, -CorridorHalfWidth));
        poly.Add(Place(end, -BossChamberHalf));
        poly.Add(Place(end + 2f * BossChamberHalf, -BossChamberHalf));
        poly.Add(Place(end + 2f * BossChamberHalf, BossChamberHalf));
        poly.Add(Place(end, BossChamberHalf));
        poly.Add(Place(end, CorridorHalfWidth));
        for (int i = rooms.Length - 1; i >= 0; i--)
        {
            if (Side(i) < 0f) continue;                       // right-hand room, already traced above
            float a = rooms[i];
            poly.Add(Place(a + RoomHalfLength, CorridorHalfWidth));
            poly.Add(Place(a + RoomHalfLength, CorridorHalfWidth + RoomDepth));
            poly.Add(Place(a - RoomHalfLength, CorridorHalfWidth + RoomDepth));
            poly.Add(Place(a - RoomHalfLength, CorridorHalfWidth));
        }
        poly.Add(Place(CorridorStart, CorridorHalfWidth));

        // A room's mob circle sits in the MIDDLE of the room, out past the corridor wall.
        float roomOut = CorridorHalfWidth + RoomDepth / 2f;
        var roomCentres = new Vec2[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
            roomCentres[i] = Place(rooms[i], Side(i) * roomOut);

        return new Shape(poly.ToArray(), Place(MouthAt, 0f), roomCentres,
                         Place(guard, 0f), Place(boss, 0f));
    }

    // ===== What the rest of the world reads =========================================================

    /// <summary>The three entrance SAFE ZONES, for <see cref="Towns"/>.
    ///
    /// <para>Each is GATED to the city whose band contains the dungeon's: a safe zone is otherwise a
    /// destination on EVERY gatekeeper's list, and a level-1 in the starter town was being offered the
    /// level-85 vaults in the same menu as his first hunting field. The gate is not one-way — the
    /// entrance has no gatekeeper, and you leave a dungeon the way every dungeon is left, with a Scroll
    /// of Return.</para>
    ///
    /// <para>⚠ <c>RegenBoost: false</c> and <c>DungeonEntrance: true</c> are both deliberate and both
    /// mean something different. No regen boost is his playtest-27 ruling (*"only in the big cities …not
    /// in a starting point of elit dungeon"*); the entrance flag is what keeps a Scroll of Return from
    /// counting this as the town it sends you home to.</para></summary>
    public static IEnumerable<SafeZone> EntranceZones =>
        All.Select(p => new SafeZone(p.EntranceId, p.EntranceName, p.EntranceX, p.EntranceY,
                                     p.EntranceRadius, GatedByCityId: p.CityId,
                                     RegenBoost: false, DungeonEntrance: true));

    /// <summary>Every dungeon's spawners: one per side room, one guard camp in the corridor, one boss.
    /// Elite rank throughout except the boss, and normal respawn timers — a dungeon is not an instance
    /// (owner: dungeons respawn and drop normally; instances are a separate system, not built yet).</summary>
    public static IEnumerable<SpawnZone> SpawnZones
    {
        get
        {
            foreach (var plan in All)
            {
                var shape = ShapeOf(plan);
                for (int i = 0; i < plan.Groups.Length; i++)
                {
                    var g = plan.Groups[i];
                    // The LAST group is the one standing in the corridor in front of the boss; every
                    // group before it gets a side room. That is his rule stated once, here.
                    bool isGuard = i == plan.Groups.Length - 1;
                    var at = isGuard ? shape.Guard : shape.Rooms[i];
                    yield return new SpawnZone(
                        at.X, at.Y, isGuard ? GuardSpawnRadius : RoomSpawnRadius,
                        g.MinLevel, g.MaxLevel, g.MobTypes, g.MaxCount,
                        g.RespawnSeconds, g.RespawnVariance, MobRank.Elite);
                }

                var b = plan.Boss;
                yield return new SpawnZone(
                    shape.Boss.X, shape.Boss.Y, BossSpawnRadius,
                    b.MinLevel, b.MaxLevel, b.MobTypes, 1,
                    b.RespawnSeconds, b.RespawnVariance, MobRank.Boss,
                    ForceZoneLevel: b.ForceZoneLevel);
            }
        }
    }

    /// <summary>Every dungeon as a REGION: its generated outline, its one arrival gate at the corridor
    /// mouth, and the city that manages it (which is also where its dead are returned).</summary>
    public static IEnumerable<Region> Fields =>
        All.Select(p =>
        {
            var shape = ShapeOf(p);
            return new Region(p.Id, p.Name, RegionKind.Field, shape.Outline,
                new[] { new TeleportPoint(p.Id + "#0", p.GateName, p.GateDescription, shape.Mouth) },
                p.CityId);
        });

    /// <summary>Startup guard — the two things this file promises that arithmetic could quietly break.
    ///
    /// <para>(1) THE BOSS RUN-UP. His rule is that you can reach the boss and fight it without a
    /// respawning elite pulling you off it. That is <see cref="GuardToBossClearance"/> against
    /// <see cref="GameConstants.MobAggroRange"/>, and it is the first thing a tweak to
    /// <see cref="CorridorTail"/> or a spawn radius would break — silently, because the symptom is a
    /// boss fight that occasionally goes wrong rather than an error.</para>
    ///
    /// <para>(2) THE DOOR. The corridor mouth must overlap the entrance safe zone, or the dungeon's
    /// world and its entrance annex do not touch and the only way in is a wall.</para>
    ///
    /// <para>Called from Program.cs beside the other world guards. Throwing at boot is the point: both
    /// of these are invisible from a map screenshot.</para></summary>
    public static void Validate()
    {
        if (GuardToBossClearance <= GameConstants.MobAggroRange)
            throw new InvalidOperationException(
                $"Dungeon boss run-up is only {GuardToBossClearance:0} units of clear ground, against a "
                + $"{GameConstants.MobAggroRange:0} aggro range — a respawning guard would pull you off the "
                + "boss. Lengthen CorridorTail or shrink the guard/boss spawn radii.");

        foreach (var p in All)
            if (CorridorStart >= p.EntranceRadius)
                throw new InvalidOperationException(
                    $"{p.Name}: the corridor starts {CorridorStart:0} units out but its entrance safe zone "
                    + $"is only {p.EntranceRadius:0} across — the door and the dungeon do not touch.");
    }
}
