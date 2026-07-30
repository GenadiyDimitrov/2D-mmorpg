using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Shared;

/// <summary>
/// THE authored plan of the overworld's hunting grounds, and the generator that turns it into spawn
/// zones, field polygons and named teleport gates.
///
/// ── Why a plan instead of a list of circles ────────────────────────────────────────────────────
/// The world used to be hand-placed spawn circles with hand-listed mob rosters, and it broke in three
/// ways at once (owner, playtest-13 follow-up):
///
///   1. **A level-12 Werewolf stood in the level-1 camp.** A mob with a natural level brings its OWN
///      level — the zone's band is only a hint — so a hand-listed roster spanning 1-12 spawned pups at 1
///      and werewolves at 12 in the same circle. *"How exactly am I supposed to kill a pig next to a
///      werewolf?"* Rosters are now DERIVED from the band (<see cref="MobCatalog.InBand"/>), so a camp
///      cannot contain a creature outside its own levels.
///   2. **Bands were 5-6 levels wide.** They are 4 wide now (2 at the top), exactly as authored below.
///   3. **Camps were adjacent and fields hugged the town.** Camps are spaced so nothing wanders or
///      chases between them, and every field clears the town wall by <see cref="TownGap"/>.
///
/// ── The shape of a city ───────────────────────────────────────────────────────────────────────
/// A city owns 2-3 FIELDS. A field sits on a BEARING from the city at a fixed distance, and its camps
/// march along the arc at that distance — so the whole field is one walk out from town, and the levels
/// step sideways rather than deeper. (Marching outward instead would put the top camp ~6000 further out,
/// which on a 48000 map with cities 13-15k apart runs one city's fields into the next.)
///
/// Every camp gets a named TELEPORT GATE on its town-facing rim (<see cref="Region.Gates"/>), so a
/// gatekeeper offers "Bracken Hollow North — Lv 1-4" instead of dropping you at a random point in a
/// polygon. And every field records its MANAGING CITY, which is where you respawn when you die in it.
/// </summary>
public static class WorldPlan
{
    // ── Geometry ──────────────────────────────────────────────────────────────────────────────────
    /// <summary>Radius of a normal camp. Small enough that several fit around a city, big enough that
    /// its mobs have somewhere to wander (wander span is 0.6·radius, clamped to 0.9·radius).</summary>
    public const float CampRadius = 700f;

    /// <summary>Rim-to-rim gap between two neighbouring camps. Wandering is already clamped inside a
    /// mob's own zone, so this is about the two other ways a camp leaks into its neighbour: aggro
    /// (<see cref="GameConstants.MobAggroRange"/> = 400) and the visual read of "these are two places".
    /// A kilometre of empty ground between bands is what makes a 4-level camp mean anything.</summary>
    public const float CampGap = 1000f;

    /// <summary>How far a field's polygon must clear its city's safe-zone edge (owner: *"the fields not
    /// to be exactly next to the city"*). A field that touches the wall means stepping out of town is
    /// stepping into a camp.</summary>
    public const float TownGap = 1500f;

    /// <summary>How much the field polygon is inflated past its camps' rims.</summary>
    public const float FieldMargin = 700f;

    /// <summary>Centre-to-centre from an ELITE camp to the normal camp it belongs to (owner: *"elite
    /// spawners need to be closer to their non-elite spawner but far enough not to aggro — 1-1.5k"*).
    /// At 1500 with radii 350/700 the rims are 450 apart, just past aggro range: you can clear the
    /// normal camp to its edge without the elite noticing.</summary>
    public const float EliteOffset = 1500f;

    public const float EliteRadius = 350f;

    /// <summary>Centre-to-centre spacing that yields <see cref="CampGap"/> between two camp rims.</summary>
    public const float CampSpacing = CampRadius * 2f + CampGap;

    // ── The authored plan ─────────────────────────────────────────────────────────────────────────

    /// <summary>One camp: a level band, and the overrides a band occasionally needs.</summary>
    public sealed record Band(
        int Min, int Max,
        /// <summary>Explicit roster. Normally null — the roster is DERIVED from [Min,Max]. Set only where
        /// no template has a natural level in the band (the 86-90 camps), and then with ForceZoneLevel so
        /// the borrowed roster respawns at the band's levels.</summary>
        string[]? Mobs = null,
        /// <summary>Which types attack on sight. Null = the <see cref="AggressiveRamp"/> count, taken from
        /// the toughest aggressive-capable creatures in the roster. Authored per band when a field wants a
        /// specific pair.</summary>
        string[]? Aggressive = null,
        int? AggressiveCount = null,
        bool ForceZoneLevel = false);

    /// <summary>One field: whose city, where around it, and the camps it holds.</summary>
    public sealed record FieldPlan(
        string CityId, string Id, string Name,
        float Bearing, float Distance,
        Band[] Bands,
        /// <summary>Level of this field's ELITE camp; 0 for none. Placed <see cref="EliteOffset"/> further
        /// out from the field's last (highest) camp, reusing its roster.</summary>
        int EliteLevel = 0);

    private static Band B(int min, int max, string[]? mobs = null, string[]? aggressive = null,
                          int? aggressiveCount = null, bool force = false) =>
        new(min, max, mobs, aggressive, aggressiveCount, force);

    /// <summary>The 84-85 roster, borrowed by the 86-90 camps: there are no creatures authored above 85
    /// yet, and the last five levels still need somewhere to happen (owner). ForceZoneLevel makes them
    /// respawn at the camp's band rather than their own level, so the climb to the cap is real.</summary>
    private static readonly string[] SummitRoster =
    {
        "drake_leader", "disciple_of_the_dawn", "radiant_berserker",
        "needle_mantis_overseer", "splinter_mantis_walker",
    };

    /// <summary>
    /// THE world's hunting grounds. Levels 1-90, in 4-level bands (2-level at the top), grouped into
    /// fields, grouped under cities — the layout the owner specified in playtest-13:
    ///
    ///   Brackenford  1-16   2 fields    Stonewatch  16-40  3 fields
    ///   Greymarsh    40-60  3 fields    Ironreach   60-75  3 fields
    ///   Frostmere    76-90  3 fields, each with its own elite camp (80 / 84 / 90)
    ///
    /// Bearings are chosen so no city's fields reach another city's, the Training Grounds or the Sunken
    /// Vale — <see cref="ValidateLayout"/> fails the boot if that ever stops being true, so a bearing can
    /// be re-aimed without measuring anything by hand.
    /// </summary>
    public static readonly FieldPlan[] Plans =
    {
        // ── Brackenford (centre, 24000/24000, r3500) — levels 1-16 ────────────────────────────────
        // The two starter camps are PEACEFUL (AggressiveRamp gives 0 below 13): nothing should ever jump
        // a level-3 character. Danger starts in Bracken Downs.
        new("town_brackenford", "field_bracken_hollow", "Bracken Hollow", 180f, 5750f,
            new[] { B(1, 4), B(4, 8) }),
        new("town_brackenford", "field_bracken_downs", "Bracken Downs", 0f, 5750f,
            new[] { B(8, 12), B(12, 16) }),

        // ── Stonewatch (north, 24000/10000, r2000) — levels 16-40 ─────────────────────────────────
        // Bearings avoid 240-300° — that is the Training Outpost and its dummy row at y≈4000.
        new("town_stonewatch", "field_stone_moor", "Greyhollow Moor", 0f, 4300f,
            new[] { B(16, 20), B(20, 24) }),
        new("town_stonewatch", "field_stone_ridge", "Stonewatch Ridge", 90f, 4300f,
            new[] { B(24, 28), B(28, 32) }),
        new("town_stonewatch", "field_stone_barrens", "Ashen Barrens", 180f, 4300f,
            new[] { B(32, 36), B(36, 40) }),

        // ── Greymarsh (south-east, 36000/33000, r2000) — levels 40-60 ─────────────────────────────
        new("town_greymarsh", "field_marsh_shallows", "Marsh Shallows", 0f, 4300f,
            new[] { B(40, 44), B(44, 48) }),
        new("town_greymarsh", "field_marsh_mire", "Blackwater Mire", 90f, 4300f,
            new[] { B(48, 52), B(52, 56) }),
        new("town_greymarsh", "field_marsh_hollow", "Sunken Hollow", 270f, 4300f,
            new[] { B(56, 60) }),

        // ── Ironreach (south, 24000/38000, r2200) — levels 60-75 ──────────────────────────────────
        // 90° (further south) is the Sunken Vale boss field, so the third field goes north instead.
        new("castle_ironreach", "field_iron_march", "Ironreach March", 180f, 4600f,
            new[] { B(60, 64), B(64, 68) }),
        new("castle_ironreach", "field_iron_highlands", "Redhorn Highlands", 0f, 4600f,
            new[] { B(68, 72) }),
        new("castle_ironreach", "field_iron_crags", "Sunland Crags", 270f, 4600f,
            new[] { B(72, 75) }),

        // ── Frostmere (north-west, 12000/15000, r2000) — levels 76-90, the endgame city ───────────
        // Two-level bands, and one ELITE camp per field at the band cap (owner's 80 / 84 / 90).
        new("town_frostmere", "field_frost_wastes", "Frostmere Wastes", 180f, 5000f,
            new[] { B(76, 77), B(78, 79), B(80, 80) }, EliteLevel: 80),
        new("town_frostmere", "field_frost_expanse", "Radiant Expanse", 90f, 5000f,
            new[] { B(81, 82), B(83, 84) }, EliteLevel: 84),
        new("town_frostmere", "field_frost_summit", "Dawnbreak Summit", 270f, 5000f,
            new[] { B(85, 86, force: true),
                    B(87, 88, SummitRoster, force: true),
                    B(89, 90, SummitRoster, force: true) }, EliteLevel: 90),
    };

    /// <summary>How many types attack on sight at a given band cap. Ramped, not flat: 71 of ~80 templates
    /// are flagged aggressive, and honouring that made every field above 10 wall-to-wall aggro — a
    /// level-22 melee walking into a 22-28 camp was jumped by casters and melee at once and simply died
    /// (owner, playtest-13). Starter camps have none; the endgame has three.</summary>
    public static int AggressiveRamp(int bandMax) =>
        bandMax <= 12 ? 0 : bandMax <= 40 ? 1 : bandMax <= 75 ? 2 : 3;

    /// <summary>Respawn cadence for a band, derived from its level: 8s in the starter camps up to ~32s at
    /// the cap. Authored per zone before, which meant 27 numbers that all had to trend the same way.</summary>
    private static double RespawnFor(int bandMin) => 8.0 + 24.0 * Math.Min(1.0, bandMin / 90.0);

    // ── Generation ────────────────────────────────────────────────────────────────────────────────

    /// <summary>A generated camp: its spawn zone, plus the gate that teleports you to its edge.</summary>
    public sealed record Camp(SpawnZone Zone, TeleportPoint? Gate);

    /// <summary>A generated field: the plan it came from and the camps inside it.</summary>
    public sealed record Field(FieldPlan Plan, Camp[] Camps)
    {
        public SpawnZone[] Zones => Camps.Select(c => c.Zone).ToArray();
        public TeleportPoint[] Gates => Camps.Where(c => c.Gate is not null).Select(c => c.Gate!).ToArray();
    }

    private static readonly Field[] _fields = Generate();

    public static IReadOnlyList<Field> Fields => _fields;

    /// <summary>The CITIES — the safe zones that own hunting fields (and so carry the full service set:
    /// buffer, keeper, three vendors, gatekeeper). The training outpost and the dungeon entrance are safe
    /// zones but not cities, which is exactly the distinction "who owns fields" draws.</summary>
    public static SafeZone[] Cities =>
        Towns.All.Where(z => Plans.Any(p => p.CityId == z.Id)).ToArray();

    /// <summary>The fields this city manages, in plan order (ascending level).</summary>
    public static Field[] FieldsOf(string cityId) =>
        _fields.Where(f => f.Plan.CityId == cityId).ToArray();

    /// <summary>Every generated spawn zone (normal camps + elite camps), in plan order.</summary>
    public static SpawnZone[] SpawnZones => _fields.SelectMany(f => f.Zones).ToArray();

    private static Field[] Generate()
    {
        var built = new List<Field>(Plans.Length);
        foreach (var plan in Plans)
        {
            // Towns, not WorldMap — WorldMap's spawn zones are generated FROM here, so reading it back
            // would be a static-initialisation cycle. See the comment on Towns.
            var city = Towns.ById(plan.CityId)
                       ?? throw new InvalidOperationException(
                           $"Field '{plan.Id}' names an unknown city '{plan.CityId}'.");

            // Camps march along the ARC at Distance, centred on Bearing. The angular step is whatever
            // yields CampSpacing between neighbours at this radius, so the gap is a distance the reader
            // can reason about rather than a hand-tuned angle.
            int n = plan.Bands.Length;
            double step = 2.0 * Math.Asin(Math.Min(1.0, CampSpacing / (2.0 * plan.Distance)));
            double centre = plan.Bearing * Math.PI / 180.0;

            var camps = new List<Camp>(n + 1);
            for (int i = 0; i < n; i++)
            {
                var band = plan.Bands[i];
                double angle = centre + (i - (n - 1) / 2.0) * step;
                float x = city.X + plan.Distance * (float)Math.Cos(angle);
                float y = city.Y + plan.Distance * (float)Math.Sin(angle);
                camps.Add(new Camp(BuildZone(band, x, y, CampRadius, MobRank.Normal, 11),
                                   BuildGate(plan, band, x, y, angle, i, n)));
            }

            // The elite camp sits EliteOffset further out along the last (highest) camp's own bearing —
            // same trip, its own ground, and past aggro range of the normal camp.
            if (plan.EliteLevel > 0)
            {
                var last = plan.Bands[n - 1];
                double angle = centre + ((n - 1) - (n - 1) / 2.0) * step;
                float ex = city.X + (plan.Distance + EliteOffset) * (float)Math.Cos(angle);
                float ey = city.Y + (plan.Distance + EliteOffset) * (float)Math.Sin(angle);
                var eliteBand = new Band(plan.EliteLevel, plan.EliteLevel,
                                         last.Mobs ?? MobCatalog.InBand(last.Min, last.Max).Select(m => m.Id).ToArray(),
                                         ForceZoneLevel: true);
                camps.Add(new Camp(BuildZone(eliteBand, ex, ey, EliteRadius, MobRank.Elite, 2), Gate: null));
            }

            built.Add(new Field(plan, camps.ToArray()));
        }
        return built.ToArray();
    }

    private static SpawnZone BuildZone(Band band, float x, float y, float radius, MobRank rank, int maxCount)
    {
        var roster = band.Mobs ?? MobCatalog.InBand(band.Min, band.Max).Select(m => m.Id).ToArray();
        double respawn = rank == MobRank.Elite ? 180.0 : RespawnFor(band.Min);
        double variance = rank == MobRank.Elite ? 40.0 : Math.Round(respawn / 3.0);

        // Elites are aggressive by RANK, so an authored list there would be dead weight.
        string[]? aggressive = rank == MobRank.Elite
            ? null
            : band.Aggressive ?? PickAggressive(roster, band.AggressiveCount ?? AggressiveRamp(band.Max));

        return new SpawnZone(x, y, radius, band.Min, band.Max, roster, maxCount,
                             respawn, variance, rank, ActiveTime.Always,
                             band.ForceZoneLevel, aggressive);
    }

    /// <summary>The N types that attack on sight: the TOUGHEST aggressive-capable creatures in the roster.
    /// Deterministic (the roster is level-ordered), and it puts the danger where a player would guess it
    /// is — the biggest thing in the camp is the thing that comes for you.</summary>
    private static string[] PickAggressive(string[] roster, int count)
    {
        if (count <= 0) return Array.Empty<string>();
        return roster.Where(id => MobCatalog.Get(id).Aggressive)
                     .OrderByDescending(id => MobCatalog.Get(id).Level)
                     .ThenBy(id => id, StringComparer.Ordinal)
                     .Take(count)
                     .ToArray();
    }

    /// <summary>The named gate for a camp: a point on its TOWN-FACING rim, so you arrive at the edge of
    /// the camp rather than in the middle of it (arriving inside a level-90 camp is a death, not a
    /// travel), plus the name and description a gatekeeper lists it under.
    ///
    /// The name is "&lt;Field&gt; &lt;compass&gt;" (owner: *"fieldName1 West, fieldName1 East"*). The compass word
    /// comes from the camp's position ALONG THE ARC — the axis the bands march down — so a three-camp
    /// field reads North / Centre / South rather than three words for what is really one direction.</summary>
    private static TeleportPoint BuildGate(FieldPlan plan, Band band, float x, float y,
                                           double angle, int index, int count)
    {
        // Step back toward town by the camp's radius: on the rim, facing the way you came.
        float gx = x - CampRadius * (float)Math.Cos(angle);
        float gy = y - CampRadius * (float)Math.Sin(angle);

        string where;
        if (count == 1)
        {
            where = "Gate";
        }
        else
        {
            double offset = index - (count - 1) / 2.0;   // -1, 0, +1 … along the arc
            if (Math.Abs(offset) < 0.4)
            {
                where = "Centre";
            }
            else
            {
                // Tangential direction at this bearing, signed by which way along the arc we sit.
                double t = plan.Bearing * Math.PI / 180.0;
                float tx = (float)(-Math.Sin(t)) * Math.Sign(offset);
                float ty = (float)Math.Cos(t) * Math.Sign(offset);
                where = Compass(tx, ty);
            }
        }

        var roster = band.Mobs ?? MobCatalog.InBand(band.Min, band.Max).Select(m => m.Id).ToArray();
        string names = string.Join(", ", roster.Take(3).Select(id => MobCatalog.Get(id).Name));
        if (roster.Length > 3) names += ", …";

        string label = band.Min == band.Max ? $"Lv {band.Min}" : $"Lv {band.Min}-{band.Max}";
        return new TeleportPoint($"{plan.Id}#{index}", $"{plan.Name} {where}",
                                 $"{label} · {names}", new Vec2(gx, gy));
    }

    /// <summary>Eight-point compass word for a direction. Smaller Y is NORTH (the client flips Y when it
    /// draws the world), which is the one thing to keep straight here.</summary>
    private static string Compass(float dx, float dy)
    {
        // Angle measured from NORTH (-Y), clockwise, so the 8 sectors fall out of a divide.
        double deg = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        if (deg < 0) deg += 360.0;
        int sector = (int)Math.Round(deg / 45.0) % 8;
        return sector switch
        {
            0 => "North", 1 => "North-East", 2 => "East", 3 => "South-East",
            4 => "South", 5 => "South-West", 6 => "West", _ => "North-West",
        };
    }

    // ── Startup guards ────────────────────────────────────────────────────────────────────────────

    /// <summary>Fail the boot on any layout mistake the plan can make. All of these were real bugs or
    /// near-misses, and every one of them is invisible in the source (a bearing is not a picture) and
    /// obvious only after walking there in-game.
    ///
    ///   • an EMPTY roster — a band whose levels no creature occupies would spawn nothing at all;
    ///   • a roster member OUTSIDE the band (unless ForceZoneLevel) — the pig-and-werewolf bug itself;
    ///   • camps closer than <see cref="CampGap"/> rim-to-rim, so one band's mobs can chase into another's;
    ///   • an ELITE nearer than aggro range to its camp's rim, or further than the owner's 1.5k;
    ///   • a camp closer than <see cref="TownGap"/> to ANY town wall;
    ///   • two camps of DIFFERENT fields overlapping, which would make the field polygons overlap too.</summary>
    public static void ValidateLayout()
    {
        var bad = new List<string>();

        foreach (var field in _fields)
        {
            foreach (var camp in field.Camps)
            {
                var z = camp.Zone;
                string tag = $"{field.Plan.Name} Lv{z.MinLevel}-{z.MaxLevel}";

                if (z.MobTypes.Length == 0)
                    bad.Add($"{tag}: no creature has a natural level in this band — author Mobs + ForceZoneLevel");

                if (!z.ForceZoneLevel)
                    foreach (var id in z.MobTypes)
                    {
                        int lvl = MobCatalog.Get(id).Level;
                        if (lvl < z.MinLevel || lvl > z.MaxLevel)
                            bad.Add($"{tag}: '{id}' is level {lvl}, outside the band");
                    }

                // Clear of every town wall, not just its own — a field can drift into a neighbour's.
                foreach (var town in Towns.All)
                {
                    float d = Dist(z.X, z.Y, town.X, town.Y) - z.Radius - town.Radius;
                    if (d < TownGap)
                        bad.Add($"{tag}: only {d:0} from the {town.Name} wall (need {TownGap:0})");
                }
            }

            // Within a field: neighbouring normal camps apart, elite close but not aggro-close.
            var normals = field.Camps.Where(c => c.Zone.Rank == MobRank.Normal).Select(c => c.Zone).ToArray();
            for (int i = 0; i < normals.Length; i++)
                for (int j = i + 1; j < normals.Length; j++)
                {
                    float gap = Dist(normals[i].X, normals[i].Y, normals[j].X, normals[j].Y)
                              - normals[i].Radius - normals[j].Radius;
                    if (gap < CampGap - 1f)
                        bad.Add($"{field.Plan.Name}: Lv{normals[i].MinLevel}-{normals[i].MaxLevel} and "
                              + $"Lv{normals[j].MinLevel}-{normals[j].MaxLevel} are only {gap:0} apart (need {CampGap:0})");
                }

            foreach (var elite in field.Camps.Where(c => c.Zone.Rank == MobRank.Elite).Select(c => c.Zone))
            {
                float nearest = normals.Length == 0 ? 0f
                    : normals.Min(nz => Dist(elite.X, elite.Y, nz.X, nz.Y) - elite.Radius - nz.Radius);
                if (nearest < GameConstants.MobAggroRange)
                    bad.Add($"{field.Plan.Name}: the elite camp is {nearest:0} from a normal camp's rim — "
                          + $"inside aggro range ({GameConstants.MobAggroRange:0})");
                if (nearest > EliteOffset)
                    bad.Add($"{field.Plan.Name}: the elite camp is {nearest:0} away — the owner's rule is "
                          + "close enough to be the same trip (~1-1.5k)");
            }
        }

        // Across fields: camps of two different fields must not touch, or their polygons overlap.
        for (int a = 0; a < _fields.Length; a++)
            for (int b = a + 1; b < _fields.Length; b++)
                foreach (var za in _fields[a].Camps.Select(c => c.Zone))
                    foreach (var zb in _fields[b].Camps.Select(c => c.Zone))
                    {
                        float gap = Dist(za.X, za.Y, zb.X, zb.Y) - za.Radius - zb.Radius
                                  - FieldMargin * 2f;
                        if (gap < 0f)
                            bad.Add($"{_fields[a].Plan.Name} and {_fields[b].Plan.Name} overlap "
                                  + $"(Lv{za.MinLevel}-{za.MaxLevel} vs Lv{zb.MinLevel}-{zb.MaxLevel}, {gap:0} short)");
                    }

        if (bad.Count > 0)
            throw new InvalidOperationException(
                "World layout is invalid — re-aim a bearing or distance in WorldPlan.Plans:\n  "
                + string.Join("\n  ", bad.Distinct()));
    }

    private static float Dist(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2, dy = y1 - y2;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Every level 1..<see cref="GameConstants.MaxPlayerLevel"/> must be inside some band, or there is a
    /// stretch of the climb with nowhere to do it. Separate from ValidateLayout because it is about the
    /// PLAN's coverage rather than its geometry.</summary>
    public static void ValidateLevelCoverage()
    {
        var gaps = new List<int>();
        for (int lvl = 1; lvl <= GameConstants.MaxPlayerLevel; lvl++)
            if (!Plans.Any(p => p.Bands.Any(b => lvl >= b.Min && lvl <= b.Max)))
                gaps.Add(lvl);
        if (gaps.Count > 0)
            throw new InvalidOperationException(
                "No hunting ground covers level(s): " + string.Join(", ", gaps));
    }
}
