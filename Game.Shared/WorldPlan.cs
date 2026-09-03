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
        bool ForceZoneLevel = false,
        /// <summary>This camp's HP multiplier, overriding the level-derived ladder in
        /// <see cref="HpScaleFor"/>. Null = take the ladder, which is what every camp does today.
        /// Authored only where a specific field should read heavier or lighter than its level says.</summary>
        float? HpScale = null);

    /// <summary>One field: whose city, where around it, and the camps it holds.</summary>
    public sealed record FieldPlan(
        string CityId, string Id, string Name,
        float Bearing, float Distance,
        Band[] Bands,
        /// <summary>Level of this field's ELITE camp; 0 for none. Placed <see cref="EliteOffset"/> further
        /// out from the field's last (highest) camp, reusing its roster.</summary>
        int EliteLevel = 0);

    private static Band B(int min, int max, string[]? mobs = null, string[]? aggressive = null,
                          int? aggressiveCount = null, bool force = false, float? hpScale = null) =>
        new(min, max, mobs, aggressive, aggressiveCount, force, hpScale);

    // ===================================================================================
    //  THE FIELD HP LADDER — BL-78 item 1 (owner, 2026-08-27): "the 15k mobs are zone placed with
    //  x2/x3 hp .. some zones can have x1".
    //
    //  ONE rule, derived from the camp's own level, rather than a number typed on each of the
    //  generated camps — the same way this generator already derives respawn time and the
    //  aggressive count. A field that wants to disagree sets Band.HpScale and says why.
    //
    //  🔑 RE-RULED 2026-09-03 (`BL-148`), and this is the LIVE ladder — four rungs, not three:
    //  *"Zone laddre x1<40, x1.5<76, x2<83, x3 84+, elits still have their x4 everywhere so x4<40,
    //  x6<76, x8<83, x12 84+ (futer tests will alter it probably..)"*
    //
    //      < 40   x1        elite x4
    //     40-75   x1.5      elite x6
    //     76-83   x2        elite x8
    //     84+     x3        elite x12
    //
    //  His second list is the COMPOSED number, not a second knob: x1.5 x 4 = x6, x2 x 4 = x8,
    //  x3 x 4 = x12. `MobRankScale.Hp(Elite)` stays x4 flat and is not touched by this — the elite
    //  column exists here only so the two knobs can be read together, which is the whole reason
    //  the plate now prints them as two lines (`BL-148` half two).
    //
    //  ⚠ LEVEL 83 IS MINE, NOT HIS. His bands read "x2<83" and "x3 84+", which leaves 83 unnamed;
    //  it is filed under x2 so that "x3 84+" is literally true. One line to move if he meant 83.
    //
    //  WHAT CHANGED AND WHY (the old three-rung ladder was x1 / x2 from 40 / x3 from 61):
    //   - x1 below 40 is UNCHANGED. Levelling is fast down here and our base HP is deliberately
    //     ~0.5x IG's below 20 (he ruled that acceptable). Tripling a newbie field would be felt as a
    //     wall, not thrill, and it is the one stretch of the game nobody has complained about.
    //   - THE x3 MOVED FROM 61 TO 84. It was set on his playtest-25 headline (*"the 80 mobs should
    //     have 15k not 5"*) and it delivered that — MobBaseStats.Hp(80) = 5,160, x3 = 15,480 — but a
    //     whole game of levels 61-83 came with it, and measured (tools/BalanceMatrix --zonehp) that
    //     is a 39-66 second kill for a solo buffed farmer at every level from 61 to 83. His own find:
    //     *"a lvl 72 redhorn footman have 12561"*. It is now 4,187 x 1.5 = 6,281.
    //   - x1.5 IS A NEW RUNG and the reason the ladder is four deep: the middle of the game needed
    //     something between "dies instantly" and "triple", and a half step is expressible because
    //     this has always been a float all the way down (SpawnZone.HpScale -> MobZoneHpScale).
    //
    //  ⚠ It multiplies HP and NOTHING else. Damage, defence, EXP and drops are untouched, so a camp
    //  takes longer to clear but does not hit harder — which is the honest reading of his complaint
    //  (things die too fast), and it keeps this lever off the 0.73.0 attack refit. The corollary is
    //  that LOWERING it raises farm rate: the same EXP and the same drops now come out in half the
    //  time from 61 to 83.
    //  ⚠ A BOSS IGNORES IT — see Entity.ApplyMobScale. An ELITE does not.
    //  ⚠ Re-measure with `dotnet run --project tools/BalanceMatrix -- --zonehp` after ANY change here
    //  or to MobBaseStats: the numbers above are read off that tool, never derived by hand.
    // ===================================================================================
    /// <summary>The HP multiplier a camp gets from its level alone. See the block above.</summary>
    public static float HpScaleFor(int level) =>
        level >= 84 ? 3f : level >= 76 ? 2f : level >= 40 ? 1.5f : 1f;

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

        // ── Stonewatch, THE OTHER THREE COLUMNS (BL-68) ───────────────────────────────────────────
        // *"Add several new zones to duplicate the 16-20, 20-24, 24-28, 28-32, 32-36, 36-40 (all the
        // Stonewatch zones) ... `north` and `south` zones to have 4 of each."* So every band that
        // existed once now exists FOUR times. The point is somewhere else to farm at your level, not
        // a longer ladder — the bands are identical, only the ground is new.
        //
        // 🔑 THEY GO EAST, which is his own instruction (*"the bot side fields can be extended ...
        // increased ~4 times in width (to the right)"*) and also the only direction with room:
        // Brackenford sits 14000 due SOUTH, Frostmere 13000 to the WEST, and NORTH is the Training
        // Outpost and its dummy row. East is 22000 of empty map.
        //
        // He offered to MOVE the city to make space. It is not moved, and did not need to be: the
        // generator places a field by bearing + distance, so three more RINGS at 8600 / 12900 / 17200
        // buy the same ground without relocating a town every player already knows — and without
        // stranding every saved character standing inside it. Ring 1 keeps its bearings; the outer
        // rings lean progressively east as the arc they can use narrows around Brackenford.
        //
        // ⚠ The geometry is not hand-derived. ValidateLayout fails the BOOT on any camp that touches
        // a town wall, another camp or another field, and prints the shortfall in units — so these
        // bearings were converged on by booting, not by trusting arithmetic in a comment.
        // The nine sit on a 3×3 GRID east of the city — three north-south lanes at x ≈ 31000 / 36000
        // / 41000, each lane keeping ring 1's own shape (the low band nearest the city, the high band
        // furthest out). The bearing/distance pairs below are that grid converted to the polar form
        // the generator takes; the grid is what to read, the numbers are just its address.
        //
        // North lane (y ≈ 6500) — above the city, clear of the Training Outpost's dummy row.
        new("town_stonewatch", "field_stone_moor_2", "Sunward Moor", 333.43f, 7826f,
            new[] { B(16, 20), B(20, 24) }),
        new("town_stonewatch", "field_stone_ridge_2", "Highstone Ridge", 343.74f, 12500f,
            new[] { B(24, 28), B(28, 32) }),
        new("town_stonewatch", "field_stone_barrens_2", "Emberdust Barrens", 348.37f, 17357f,
            new[] { B(32, 36), B(36, 40) }),

        // Middle lane (y ≈ 12000) — due east, the straight extension he described.
        new("town_stonewatch", "field_stone_moor_3", "Thornfen Moor", 15.95f, 7280f,
            new[] { B(16, 20), B(20, 24) }),
        new("town_stonewatch", "field_stone_ridge_3", "Ravencrag Ridge", 9.46f, 12166f,
            new[] { B(24, 28), B(28, 32) }),
        new("town_stonewatch", "field_stone_barrens_3", "Palewind Barrens", 6.71f, 17117f,
            new[] { B(32, 36), B(36, 40) }),

        // South lane (y ≈ 17500) — east of Brackenford, which is why it leans out rather than down:
        // Brackenford's own wall and its east field are what the first attempt at this collided with.
        new("town_stonewatch", "field_stone_moor_4", "Mistlow Moor", 46.97f, 10259f,
            new[] { B(16, 20), B(20, 24) }),
        new("town_stonewatch", "field_stone_ridge_4", "Bleakspur Ridge", 32.01f, 14150f,
            new[] { B(24, 28), B(28, 32) }),
        new("town_stonewatch", "field_stone_barrens_4", "Cinderflat Barrens", 23.80f, 18581f,
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

    // ===================================================================================
    //  BL-79 — THE GUARD POSTS. Where the watch actually stands.
    //
    //  Owner, 2026-08-27: "try make town guards and one archer(overenchanded) in several zones (like
    //  piesfull farming zones)", and from playtest 25: "each towns exit will have two guards - a tank
    //  and an archer".
    //
    //  A TOWN POST sits just outside the city's safe radius, on the bearing of that city's first
    //  hunting field — which is what "the town's exit" means here, since a city has no authored door
    //  and its fields ARE its roads. Just outside matters twice over: a safe zone keeps mobs out
    //  entirely, and MobAi skips any candidate standing in one, so a guard placed inside would be
    //  both illegal and blind.
    //
    //  A FIELD POST sits at the far camp of a quiet farming field — his "peaceful farming zones".
    //  Three of them, spread across the level range rather than clustered, so the feature can be
    //  tested at more than one level without a taxi ride between them.
    //
    //  ⚠ ONE POST PER CITY, NOT ONE PER BEARING. Five cities carry ~25 field plans between them, and
    //  a guard pair on every one of them would be fifty level-80 creatures standing around the world
    //  to serve a feature nobody has played yet. Widening this is one line in the loop below once he
    //  has seen it work.
    // ===================================================================================

    /// <summary>How far beyond a city's safe radius its guard post stands.</summary>
    private const float GuardPostOffset = 300f;

    /// <summary>The radius a guard post patrols. Small on purpose — a post is a gate, not a camp,
    /// and the pair should read as standing TOGETHER at the road.</summary>
    private const float GuardPostRadius = 220f;

    /// <summary>Respawn for a TOWN guard: 60-90s, his own numbers (2026-08-27). Authored as a centre
    /// with a variance because that is the shape SpawnZone takes — 75 ± 15 IS 60-90.</summary>
    private const double TownGuardRespawnSeconds = 75.0;
    private const double TownGuardRespawnVariance = 15.0;

    /// <summary>Respawn for a FIELD guard: *"1-2s (if ever killed)"*. Effectively immediate — the
    /// parenthesis is the design statement, not a caveat. A guard tower is not something you clear;
    /// killing one buys you a second and a half, which is to say nothing at all.</summary>
    private const double FieldGuardRespawnSeconds = 1.5;
    private const double FieldGuardRespawnVariance = 0.5;

    /// <summary>The fields that get a field post. Chosen for spread across the level range, and named
    /// by id so a re-aimed bearing or a renamed field cannot silently move a guard somewhere else.</summary>
    private static readonly string[] GuardedFieldIds =
    {
        "field_stone_barrens",      // Stonewatch, the mid-20s/30s farm
        "field_marsh_hollow",       // Greymarsh, the 40-60 band
        "field_frost_expanse",      // Frostmere, the 76-90 band
    };

    /// <summary>Every guard post in the world (BL-79). Concatenated into WorldMap.SpawnZones.
    ///
    /// ⚠ A TOWN POST IS DELIBERATELY OUTSIDE EVERY FIELD POLYGON, which is exactly what
    /// Regions.ValidateSpawnersInFields exists to reject — see the guard-post exemption there. The
    /// rule's reason ("a spawner outside every field has no zone identity and no derived band") is
    /// the one thing a guard post does not suffer from: it is hand-authored identity from end to end.
    /// </summary>
    public static SpawnZone[] GuardZones => _guardZones;

    private static readonly SpawnZone[] _guardZones = BuildGuardZones();

    private static SpawnZone[] BuildGuardZones()
    {
        var zones = new List<SpawnZone>();
        string[] townPair = { "guard_town_tank", "guard_town_archer" };
        string[] fieldPair = { "guard_field_tank", "guard_field_archer" };

        // ---- One post at each city's exit ----
        foreach (var city in Cities)
        {
            var firstPlan = Array.Find(Plans, p => p.CityId == city.Id);
            if (firstPlan is null) continue;

            double angle = firstPlan.Bearing * Math.PI / 180.0;
            float dist = city.Radius + GuardPostOffset;
            zones.Add(GuardZone(city.X + dist * (float)Math.Cos(angle),
                                city.Y + dist * (float)Math.Sin(angle),
                                townPair, 80, TownGuardRespawnSeconds, TownGuardRespawnVariance));
        }

        // ---- One post at the far camp of each guarded field ----
        foreach (var id in GuardedFieldIds)
        {
            var field = _fields.FirstOrDefault(f => f.Plan.Id == id);
            if (field is null || field.Camps.Length == 0) continue;

            // The LAST normal camp — the deepest part of the field, where somebody farming quietly
            // would actually be, rather than the entrance everyone walks through.
            var camp = field.Camps[^1].Zone;
            zones.Add(GuardZone(camp.X, camp.Y, fieldPair, 90,
                                FieldGuardRespawnSeconds, FieldGuardRespawnVariance));
        }

        return zones.ToArray();
    }

    private static SpawnZone GuardZone(float x, float y, string[] pair, int level,
                                      double respawn, double variance) =>
        new(x, y, GuardPostRadius, level, level, pair, pair.Length,
            respawn, variance, MobRank.Normal, ActiveTime.Always,
            // The guard templates carry their own natural level, so ForceZoneLevel stays false and
            // the band above is descriptive — the same convention every named creature runs on.
            ForceZoneLevel: false,
            // Both of them "aggressive", which for a guard means only "acquires targets at all";
            // MobType.Guard is what narrows that to PKs. Naming them explicitly beats the default
            // (first entry only), which would leave the archer permanently asleep.
            AggressiveTypes: pair,
            Dedicated: pair.Select(m => new DedicatedSpawn(m, 1)).ToArray(),
            // A guard's pool is its own — it is a player-built creature and the field HP ladder is
            // for the mob curve. x1 keeps "match a level-80 player in S+0" true.
            HpScale: 1f);

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

        // The camp's HP multiplier: its own if authored, otherwise the level ladder. Keyed on Max —
        // a camp is named by the top of its band, and that is the level a player judges it at.
        float hpScale = band.HpScale ?? HpScaleFor(band.Max);

        return new SpawnZone(x, y, radius, band.Min, band.Max, roster, maxCount,
                             respawn, variance, rank, ActiveTime.Always,
                             band.ForceZoneLevel, aggressive,
                             DedicatedFor(roster, band, rank), hpScale);
    }

    /// <summary>How many of a quest target a camp keeps in its OWN spawner, on top of the mixed pool.</summary>
    private const int QuestSpawnPerType = 4;

    /// <summary>Floor for the above once a camp has several quest targets to serve.</summary>
    private const int MinQuestSpawn = 2;

    /// <summary>The per-template spawners for a camp: one for every roster entry some quest asks the
    /// player to KILL, sized so the guaranteed population never more than doubles the camp.
    ///
    /// The band check matters as much as the roster check. A quest step accepts kills only inside its
    /// own level window, so a camp that CAN spawn the creature but never at a level the step counts is
    /// no use to that quest and gets no dedicated spawner — which is also what keeps a level-12
    /// werewolf's guaranteed slice in the camp where the quest can actually use it.
    ///
    /// Elites and bosses are excluded: their camps hold 2 mobs by design, and a guaranteed slice of
    /// four would turn an elite ground into a normal one.</summary>
    private static DedicatedSpawn[]? DedicatedFor(string[] roster, Band band, MobRank rank)
    {
        if (rank != MobRank.Normal) return null;

        var targets = new List<string>();
        foreach (string id in roster)
        {
            var target = QuestCatalog.KillTargets
                .FirstOrDefault(t => string.Equals(t.MobId, id, StringComparison.OrdinalIgnoreCase));
            if (target is null) continue;

            // The levels this camp can actually produce for this template: its own natural level,
            // unless the zone overrides levels wholesale (ForceZoneLevel), in which case the band wins.
            var mob = MobCatalog.Get(id);
            int spawnMin = mob.Level > 0 && !band.ForceZoneLevel ? mob.Level : band.Min;
            int spawnMax = mob.Level > 0 && !band.ForceZoneLevel ? mob.Level : band.Max;

            // Overlap against the step's window, where 0 means "unbounded".
            if (target.MinLevel > 0 && spawnMax < target.MinLevel) continue;
            if (target.MaxLevel > 0 && spawnMin > target.MaxLevel) continue;

            targets.Add(id);
        }

        if (targets.Count == 0) return null;

        // Cap the total addition at the camp's own size, so a roster full of quest targets cannot
        // triple the population.
        int per = Math.Min(QuestSpawnPerType, Math.Max(MinQuestSpawn, 11 / targets.Count));
        return targets.Select(id => new DedicatedSpawn(id, per)).ToArray();
    }

    /// <summary>Quest kill targets that NO generated camp serves with a dedicated spawner — either the
    /// creature is in no camp's roster at all, or only at levels the quest step will not count.
    ///
    /// Returned rather than thrown: a target may legitimately live in a hand-authored dungeon zone
    /// (<see cref="WorldMap.SpawnZones"/> appends those) or be a boss, and failing the boot over one
    /// would be worse than saying so. The server logs it at startup — which is the only way a typo in
    /// a quest's TargetId, or a quest whose band no longer matches any camp, ever becomes visible.</summary>
    public static string[] UnservedKillTargets()
    {
        var served = SpawnZones
            .SelectMany(z => z.DedicatedSpawns.Select(d => d.MobId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return QuestCatalog.KillTargets
            .Where(t => !served.Contains(t.MobId))
            .Select(t => t.MobId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
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
