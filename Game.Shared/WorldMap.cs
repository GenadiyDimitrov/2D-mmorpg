namespace Game.Shared;

/// <summary>
/// THE editable world layout. Everything about where things are lives here so
/// the server (spawning, collision) and the client (drawing zones/paths/border)
/// agree on one source of truth. To reshape the world, edit the lists below.
/// </summary>
public static class WorldMap
{
    /// <summary>The clamp bounds for mob SPAWNING and wandering — the FULL world, negative quadrant
    /// included. It used to be [0, Zone] (the positive overworld only), which was fine until dungeons
    /// and the jail moved into the negative quadrant: `ClampToBorder` then snapped every dungeon mob
    /// spawn (at e.g. -12000,-12000) to (0,0), so all of them piled onto the overworld corner far from
    /// the dungeon — the device playtest's "mobs spawn on the same spot and don't aggro" in the crypt.
    /// Now it spans [WorldMin, Zone], so a negative-quadrant spawn stays where it belongs.
    ///
    /// This is NOT the player boundary (that's ConfineToDomain) nor the drawn world border (that's the
    /// client's own [0, Zone] rectangle) — only the spawn/wander safety clamp.</summary>
    public static readonly WorldBorder Border = new(
        MinX: GameConstants.WorldMinX, MinY: GameConstants.WorldMinY,
        MaxX: GameConstants.ZoneWidth, MaxY: GameConstants.ZoneHeight);

    /// <summary>
    /// Mob spawn zones. Each is a circle at (X, Y) with a Radius that spawns
    /// MobCount mobs of the given types between MinLevel and MaxLevel.
    ///
    /// To add a zone — e.g. "at (1000,1000) radius 800, level 5-7 boars and
    /// spiders" — just add a SpawnZone line. Order doesn't matter; the server
    /// spawns each independently and the client tints each one.
    /// </summary>
    // The world is a 48000x48000 square. The starter town (Brackenford) sits at the
    // centre (24000,24000); six more towns ring it, and difficulty rises as you
    // tour the ring clockwise from the north (Stonewatch → Emberfall → Greymarsh →
    // Ironreach → Duskvale → Frostmere). Each band has 1-2 spawn zones beside its town.
    // Coordinates were scaled ×2 from the old 24000 world so towns are far apart and
    // zones aren't clustered; zone radii kept so the gaps between them grew.
    public static readonly SpawnZone[] SpawnZones =
    {
        // ===================== THE FIVE CITIES (owner, 2026-07-29) =====================
        // The world was seven towns in a ring, each with two wide bands. It is FIVE cities now, each
        // owning a level range and holding 2-4 tighter FIELDS, because 6-level bands meant a character
        // spent half of every band either farming grey mobs or being outclassed:
        //
        //   Brackenford  1-16    2 fields     Stonewatch  16-40   4 fields
        //   Greymarsh    40-60   4 fields     Ironreach   60-75   3 fields
        //   Frostmere    76-90   3 fields + three ELITE spawners (80 / 84 / 90)
        //
        // Emberfall and Duskvale are GONE — their rosters redistributed into the bands above.
        //
        // Named mobs bring their OWN level (MobType.Level), so a field's roster is chosen to sit inside
        // its band; the band is what the UI reports. The one exception is the 85-90 field, which sets
        // ForceZoneLevel so the top-level roster respawns at 86-90 and there is somewhere to finish the
        // climb to the cap (owner: "make it so we can have a place to lvl up from 86 to 90" — new
        // creatures for that band come later).
        //
        // Fields sit >=2500 from their town centre (outside the safe zone) and are spaced so two bands
        // never bleed into one another.

        // AggressiveTypes is AUTHORED per field: as many or as few as the field should have. The
        // starter fields are deliberately PEACEFUL (empty list) — nothing should jump a level-3
        // character — and danger ramps from one type to two as the bands climb.

        // ===== Brackenford (centre, 24000/24000) — levels 1-16 =====
        new(X: 19400, Y: 24000, Radius: 1500, MinLevel: 1,  MaxLevel: 12,
            MobTypes: new[] { "ridgeback_pup", "fox", "goblin_scout", "ashen_wolf", "werewolf" }, MaxCount: 12,
            RespawnSeconds: 8, RespawnVariance: 3,
            AggressiveTypes: new string[0]),                       // the very first field: nothing hunts you
        new(X: 28600, Y: 24000, Radius: 1500, MinLevel: 8,  MaxLevel: 16,
            MobTypes: new[] { "goblin_scout", "ashen_wolf", "werewolf", "hook_spider", "orc_archer" }, MaxCount: 12,
            RespawnSeconds: 10, RespawnVariance: 4,
            AggressiveTypes: new[] { "werewolf" }),                // one thing to learn to watch for

        // ===== Stonewatch (north, 24000/10000) — levels 16-40 =====
        new(X: 20000, Y: 10000, Radius: 1400, MinLevel: 16, MaxLevel: 22,
            MobTypes: new[] { "orc_archer", "skeleton_grunt", "shield_skeleton", "grizzly_bear" }, MaxCount: 12,
            RespawnSeconds: 12, RespawnVariance: 4,
            AggressiveTypes: new[] { "grizzly_bear" }),
        new(X: 28000, Y: 10000, Radius: 1400, MinLevel: 22, MaxLevel: 28,
            MobTypes: new[] { "grizzly_bear", "cinder_imp", "watcher_eye", "lizardman_warrior" }, MaxCount: 12,
            RespawnSeconds: 14, RespawnVariance: 5,
            AggressiveTypes: new[] { "cinder_imp", "lizardman_warrior" }),
        new(X: 24000, Y: 6800,  Radius: 1300, MinLevel: 28, MaxLevel: 34,
            MobTypes: new[] { "lizardman_warrior", "marauder_recruit", "mantis_worker", "grave_robber_fighter", "medusa", "plunder_beetle" }, MaxCount: 12,
            RespawnSeconds: 15, RespawnVariance: 5,
            AggressiveTypes: new[] { "lizardman_warrior", "marauder_recruit" }),
        new(X: 24000, Y: 14000, Radius: 1400, MinLevel: 34, MaxLevel: 40,
            MobTypes: new[] { "medusa", "plunder_beetle", "wyrm", "marsh_mantis_soldier", "fen_lizardman_archer", "dune_orc_archer" }, MaxCount: 12,
            RespawnSeconds: 16, RespawnVariance: 5,
            AggressiveTypes: new[] { "wyrm", "dune_orc_archer" }),

        // ===== Greymarsh (south-east, 36000/33000) — levels 40-60 =====
        new(X: 32000, Y: 33000, Radius: 1400, MinLevel: 40, MaxLevel: 45,
            MobTypes: new[] { "dune_orc_archer", "rift_portling", "harpy", "ridge_orc_overlord", "grave_lich", "fomor_brute" }, MaxCount: 11,
            RespawnSeconds: 18, RespawnVariance: 6,
            AggressiveTypes: new[] { "harpy", "grave_lich" }),
        new(X: 40000, Y: 33000, Radius: 1400, MinLevel: 45, MaxLevel: 50,
            MobTypes: new[] { "fomor_brute", "marsh_marauder", "warped_drake", "amber_basilisk", "wildhorn_grunt", "mantis_follower", "ravener" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6,
            AggressiveTypes: new[] { "warped_drake", "ravener" }),
        new(X: 36000, Y: 29000, Radius: 1400, MinLevel: 50, MaxLevel: 55,
            MobTypes: new[] { "mantis_follower", "ravener", "marauder_warrior", "fallen_angel", "thornback", "gaze_hound", "ash_orc_soldier" }, MaxCount: 11,
            RespawnSeconds: 20, RespawnVariance: 6,
            AggressiveTypes: new[] { "gaze_hound", "fallen_angel" }),
        new(X: 36000, Y: 37000, Radius: 1400, MinLevel: 55, MaxLevel: 60,
            MobTypes: new[] { "ash_orc_soldier", "mirror_ghost", "mirror_wraith", "dune_orc_porter", "aether_wisp", "hollow_one", "sand_ratman", "valley_treant" }, MaxCount: 10,
            RespawnSeconds: 22, RespawnVariance: 7,
            AggressiveTypes: new[] { "mirror_wraith", "hollow_one" }),

        // ===== Ironreach (south, 24000/38000) — levels 60-75 =====
        new(X: 19500, Y: 38000, Radius: 1400, MinLevel: 60, MaxLevel: 65,
            MobTypes: new[] { "sand_ratman", "valley_treant", "cursed_blade", "bogwood", "fen_lizardman", "obsidian_knight", "crimson_drake", "wildhorn_scout", "dread_knight" }, MaxCount: 10,
            RespawnSeconds: 24, RespawnVariance: 7,
            AggressiveTypes: new[] { "crimson_drake", "dread_knight" }),
        new(X: 28500, Y: 38000, Radius: 1400, MinLevel: 65, MaxLevel: 70,
            MobTypes: new[] { "dread_knight", "spiteful_ghost", "wildhorn_elder", "highland_kookaburra", "highland_buffalo", "highland_buffalo_tamed", "dread_archer", "dire_beast" }, MaxCount: 10,
            RespawnSeconds: 26, RespawnVariance: 8,
            AggressiveTypes: new[] { "dread_knight", "dread_archer" }),
        new(X: 24000, Y: 33500, Radius: 1400, MinLevel: 70, MaxLevel: 75,
            MobTypes: new[] { "dire_beast", "revenant_minion", "redhorn_footman", "redhorn_elite", "sunland_orc_scout", "redhorn_recruit", "sunland_orc_warrior" }, MaxCount: 10,
            RespawnSeconds: 28, RespawnVariance: 8,
            AggressiveTypes: new[] { "dire_beast", "redhorn_elite" }),

        // ===== Frostmere (north-west, 12000/15000) — levels 76-90, the endgame city =====
        // Each field carries an ELITE spawner ~1200 away: close enough to be the same trip, far enough
        // that the elite does not aggro you while you clear the normal camp (owner: 1-1.5k).
        new(X: 8000,  Y: 15000, Radius: 1400, MinLevel: 76, MaxLevel: 80,
            MobTypes: new[] { "redhorn_soldier", "sunland_orc_commander", "sunland_orc_captain", "redhorn_general", "emberwyrm_drake", "scarlet_mantis", "wrathborn_demon" }, MaxCount: 10,
            RespawnSeconds: 30, RespawnVariance: 9,
            AggressiveTypes: new[] { "wrathborn_demon", "emberwyrm_drake", "redhorn_general" }),   // endgame: three
        new(X: 8000,  Y: 12200, Radius: 400,  MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "scarlet_mantis", "wrathborn_demon" }, MaxCount: 2,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite),

        new(X: 16000, Y: 15000, Radius: 1400, MinLevel: 81, MaxLevel: 84,
            MobTypes: new[] { "radiant_scout", "radiant_berserker", "radiant_mage", "splinter_mantis_drone", "needle_mantis_overseer", "splinter_mantis_walker" }, MaxCount: 10,
            RespawnSeconds: 30, RespawnVariance: 9,
            AggressiveTypes: new[] { "radiant_berserker", "radiant_mage", "needle_mantis_overseer" }),
        new(X: 16000, Y: 12200, Radius: 400,  MinLevel: 84, MaxLevel: 84,
            MobTypes: new[] { "needle_mantis_overseer", "splinter_mantis_walker" }, MaxCount: 2,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite),

        // 85-90: the ONLY field that forces its own levels onto the roster, so the level-85 creatures
        // respawn all the way to the cap and the last five levels have somewhere to happen.
        new(X: 12000, Y: 19500, Radius: 1500, MinLevel: 85, MaxLevel: 90,
            MobTypes: new[] { "disciple_of_the_dawn", "drake_leader", "radiant_berserker", "needle_mantis_overseer", "splinter_mantis_walker" }, MaxCount: 10,
            RespawnSeconds: 32, RespawnVariance: 10, ForceZoneLevel: true,
            AggressiveTypes: new[] { "drake_leader", "disciple_of_the_dawn", "radiant_berserker" }),
        new(X: 14700, Y: 21200, Radius: 400,  MinLevel: 90, MaxLevel: 90,
            MobTypes: new[] { "disciple_of_the_dawn", "drake_leader" }, MaxCount: 2,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite, ForceZoneLevel: true),

        // ===== Training Grounds: immortal, stationary, 0-damage dummies at fixed levels
        //       (20/40/60/80) for testing damage/skills. Clustered, one per level. =====
        new(X: 22500, Y: 4000, Radius: 200, MinLevel: 20, MaxLevel: 20,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 23500, Y: 4000, Radius: 200, MinLevel: 40, MaxLevel: 40,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 24500, Y: 4000, Radius: 200, MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 25500, Y: 4000, Radius: 200, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "training_dummy" }, MaxCount: 1, RespawnSeconds: 5),

        // ===== Elite + Boss placeholders (more bosses/instances later) =====
        // The emberwyrm ELITE roams the Frostmere Wastes (inside that field, an L70-85 zone) — it used to
        // sit at (11600,10000), which was too near the Hollow Crypt to take its own field, and left it a
        // "rogue" spawner outside every field. Moved in-zone so every spawner is a child of a field.
        new(X: 10500, Y: 17000, Radius: 300,  MinLevel: 78, MaxLevel: 78,
            MobTypes: new[] { "emberwyrm_drake" }, MaxCount: 1,
            RespawnSeconds: 180, RespawnVariance: 40, Rank: MobRank.Elite),
        new(X: 24000, Y: 45000, Radius: 250,  MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "valley_treant" }, MaxCount: 1,
            RespawnSeconds: 21 * 3600, RespawnVariance: 3 * 3600, Rank: MobRank.Boss),

        // Sunken Vale — trash for the treant BOSS field, kept on the flanks (>3500u from the boss) so you
        // reach the boss without an escort. Level 58-60 to sit just under the boss and match its band.
        new(X: 20500, Y: 45000, Radius: 1400, MinLevel: 58, MaxLevel: 60,
            MobTypes: new[] { "aether_wisp", "sand_ratman", "bogwood" }, MaxCount: 7,
            RespawnSeconds: 22, RespawnVariance: 7),
        new(X: 27500, Y: 45000, Radius: 1400, MinLevel: 58, MaxLevel: 60,
            MobTypes: new[] { "fen_lizardman", "cursed_blade", "wildhorn_scout" }, MaxCount: 7,
            RespawnSeconds: 22, RespawnVariance: 7),

        // ===== DUNGEON: Hollow Crypt — in the NEGATIVE quadrant (owner: dungeons live at minus coords,
        // reached by teleport, off the overworld). A dungeon is just a SpawnZone cluster + an ENTRANCE
        // safe zone (below); any gatekeeper teleports you to it. Harder ELITE rooms (level 44-48) that
        // respawn normally, ending in a boss. Normal drops (unlike an instance). Its field wraps these.
        new(X: -10800, Y: -11500, Radius: 350, MinLevel: 44, MaxLevel: 44,
            MobTypes: new[] { "hollow_one" }, MaxCount: 6,
            RespawnSeconds: 60, RespawnVariance: 15, Rank: MobRank.Elite),
        new(X: -9600,  Y: -11000, Radius: 350, MinLevel: 45, MaxLevel: 45,
            MobTypes: new[] { "grave_robber_fighter" }, MaxCount: 6,
            RespawnSeconds: 60, RespawnVariance: 15, Rank: MobRank.Elite),
        new(X: -8400,  Y: -10500, Radius: 350, MinLevel: 46, MaxLevel: 46,
            MobTypes: new[] { "dread_knight" }, MaxCount: 5,
            RespawnSeconds: 90, RespawnVariance: 20, Rank: MobRank.Elite),
        new(X: -7200,  Y: -10000, Radius: 300, MinLevel: 48, MaxLevel: 48,
            MobTypes: new[] { "grave_lich" }, MaxCount: 1,
            RespawnSeconds: 30 * 60, RespawnVariance: 5 * 60, Rank: MobRank.Boss),
    };

    /// <summary>
    /// "Roads": wide strips where mobs do NOT spawn, so there are safe-ish
    /// corridors leading toward the hunting grounds. The client draws them as
    /// thick, semi-transparent grey lines. Each path is a sequence of points
    /// with a half-width; the strip is the area within Width of any segment.
    /// </summary>
    /// <summary>Safe zones (cities/castles): no mobs spawn or enter, aggro
    /// clears inside, regen is boosted. Each has a stable id so teleports can
    /// target them later. The first is the starter town at map centre.</summary>
    // NOTE: all place names here are original/generic on purpose — NEVER use town,
    // region, or NPC names trademarked by other games (no Lineage/L2 names, etc.).
    public static readonly SafeZone[] SafeZones =
    {
        // Starter town at the map centre; six more ring it (clockwise from north).
        // Brackenford is the biggest (it holds all the quest/class/vendor NPCs, which
        // are spread out so their labels don't overlap); ring towns are roomy too.
        // FIVE cities (owner, 2026-07-29). Emberfall and Duskvale are gone — see SpawnZones.
        new("town_brackenford", "Brackenford",     24000, 24000, 3500),   // 1-16
        new("town_stonewatch",  "Stonewatch",      24000, 10000, 2000),   // 16-40
        new("town_greymarsh",   "Greymarsh",       36000, 33000, 2000),   // 40-60
        new("castle_ironreach",  "Ironreach Keep", 24000, 38000, 2200),   // 60-75
        new("town_frostmere",   "Frostmere",       12000, 15000, 2000),   // 76-90
        // Small outpost beside the Training Grounds, so you can buff up and teleport out without
        // leaving the dummies. Sits just SOUTH of the dummy row (they're at y=4000, radius 200),
        // clear of them — a safe zone keeps mobs out, and the dummies ARE mobs.
        new("outpost_training", "Training Outpost", 24000, 5000, 400),
        // The Hollow Crypt dungeon ENTRANCE, in the negative quadrant with the dungeon. Being a safe zone
        // makes it a teleport destination from every gatekeeper automatically (TeleportDestinationsFrom
        // default) — that teleport IS how you reach the dungeon now — and a safe arrive/regroup spot
        // before the elite rooms just NE of it.
        new("dungeon_hollow_crypt", "Hollow Crypt", -12000, -12000, 500),
    };

    /// <summary>The STARTER town (map centre). Used where "nearest" would leak information — a player
    /// released from jail is sent here rather than to whatever town happens to be closest, so the jail's
    /// location stays secret.</summary>
    public static SafeZone StartingTown => SafeZones[0];

    /// <summary>The safe zone nearest to a point (always returns one). Used to
    /// respawn the dead at their closest town instead of the map centre.</summary>
    public static SafeZone NearestSafeZone(float x, float y)
    {
        SafeZone best = SafeZones[0];
        float bestSq = float.MaxValue;
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            float sq = dx * dx + dy * dy;
            if (sq < bestSq) { bestSq = sq; best = z; }
        }
        return best;
    }

    /// <summary>True if the point is inside ANY safe zone. Stage-2 Regions migration (owner): this is now
    /// the UNION of the old safe-zone CIRCLES and the TOWN region POLYGONS. Union, not replacement, on
    /// purpose — the polygons are authored to CONTAIN their circles, but keeping the circle in the test
    /// means no location safe today can EVER become unsafe (the dangerous direction), while the polygon
    /// adds the corners the circle missed. This is the one function that gates PvP, jail release,
    /// respawn and vendor access, so it is deliberately the safe-side migration.</summary>
    public static bool InAnySafeZone(float x, float y)
    {
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            if (dx * dx + dy * dy <= z.Radius * z.Radius)
                return true;
        }
        return RegionMap.InTown(x, y);
    }

    /// <summary>The safe zone containing a point, or null.</summary>
    public static SafeZone? SafeZoneAt(float x, float y)
    {
        foreach (var z in SafeZones)
        {
            float dx = x - z.X, dy = y - z.Y;
            if (dx * dx + dy * dy <= z.Radius * z.Radius)
                return z;
        }
        return null;
    }

    /// <summary>Per-gatekeeper teleport menus: gatekeeper NPC id -> the ORDERED town
    /// ids it offers. A gatekeeper not listed here falls back to "all other towns".
    /// This is the seam for curating each gatekeeper's own collection (and, on a large
    /// map, its nearby zones) — edit a gatekeeper's list here to change its menu.</summary>
    public static readonly Dictionary<string, string[]> GatekeeperDestinations = new();

    /// <summary>The towns a gatekeeper offers travel to: its curated list if present
    /// in <see cref="GatekeeperDestinations"/>, otherwise every other town. Always
    /// excludes the gatekeeper's own town.</summary>
    public static IEnumerable<SafeZone> TeleportDestinationsFrom(string gatekeeperNpcId, SafeZone home)
    {
        if (GatekeeperDestinations.TryGetValue(gatekeeperNpcId, out var ids))
            return ids.Select(id => Array.Find(SafeZones, z => z.Id == id))
                      .Where(z => z is not null && z.Id != home.Id)
                      .Select(z => z!);
        return SafeZones.Where(z => z.Id != home.Id);
    }

    /// <summary>NPCs placed in the world (quest givers, class-change masters).
    /// Stationary, non-combat. Add NPCs here; quests/class-changes reference
    /// them by Id.</summary>
    public static readonly NpcDef[] Npcs = new NpcDef[]
    {
        // --- Starter town: Brackenford (map centre, 24000,24000, radius 3500). NPCs
        //     are spread ~2000 units apart (≈360px on screen) so labels don't overlap;
        //     the centre is left clear (where new characters spawn). ---
        // ===== BRACKENFORD TOWN LAYOUT (owner, 2026-07-29) ==========================================
        // Grouped by WHAT YOU CAME FOR instead of scattered: everyone who sells stands together on the
        // EAST side, everyone who gives a quest or changes your class on the WEST, the gatekeeper alone
        // at top-centre and the skill-resetter alone at bottom-centre. Each cluster is ~450 apart —
        // close enough to be one stop, far enough that the name labels do not overlap.
        // (The Apothecary and the Mindwright used to sit 500 apart on the same side, which read as one
        // clump and put a vendor next to a service that is nothing to do with shopping.)
        //
        // Smaller Y is NORTH on screen (WorldMapper flips Y), so 22200 is "above" the 24000 centre.

        // --- WEST: quests + class changes ---
        new("priest_oren",   "High Priest Oren",   22600, 24250, NpcRole.QuestGiver),
        new("elder_marius",  "Elder Marius",       22150, 24250, NpcRole.QuestGiver),
        new("master_class",  "Class Master Vael",  22800, 23800, NpcRole.ClassChange),
        // (The 3rd-class Grandmaster is NOT here — he stands in Greymarsh, below. See RingTownServices.)
        // --- EAST: the three vendors, one stop ---
        // (their wares are defined by ShopCatalog, keyed on these ids)
        new("merchant_potions", "Apothecary Miren", 25200, 23800, NpcRole.Vendor),
        // The gear trade is split in two (owner, playtest-13): one Armsmaster selling WEAPONS, one
        // Outfitter selling ARMOR, shields and jewels. A single vendor stocking the whole F/E/D ladder
        // at three qualities is ~150 rows, which is most of why the list read as "no idea which is which".
        new("merchant_gear",    "Armsmaster Dolan",  25650, 23800, NpcRole.Vendor),
        new("merchant_armor",   "Outfitter Bryn",    25400, 24250, NpcRole.Vendor),
        // Newbie buffer: blesses lvl 6-75 characters with a buffer's full buff set.
        new("buffer_newbie",    "Spirit Helper Nyra", 23400, 25800, NpcRole.Buffer),
        // Skill reset: un-learns the PERMANENT, mutually-exclusive picks (the level-40 stat swaps)
        // so a bad commitment can be re-chosen. Free to forget — the gold is NOT refunded.
        // BOTTOM-CENTRE, on its own: it is a service, not a shop, and it used to stand 500 from the
        // Apothecary where the two read as one clump (owner).
        new("resetter_main",    "Mindwright Sela",   24000, 25800, NpcRole.SkillReset),
        // --- Gatekeepers: one in every town (stands at its centre) so the whole
        //     travel network is reachable in both directions. ---
        // Brackenford's stands alone at TOP-CENTRE (owner) — it is the one NPC you walk to from
        // anywhere in town, so it should not be inside either cluster.
        new("gatekeeper_brackenford", "Gatekeeper Pell",   24000, 22200, NpcRole.Teleporter),
        new("gatekeeper_stonewatch",  "Gatekeeper Soren",  24000, 10000, NpcRole.Teleporter),
        new("gatekeeper_greymarsh",   "Gatekeeper Maela",  36000, 33000, NpcRole.Teleporter),
        new("gatekeeper_ironreach",   "Gatekeeper Vurst",  24000, 38000, NpcRole.Teleporter),
        new("gatekeeper_frostmere",   "Gatekeeper Khaz",   12000, 15000, NpcRole.Teleporter),

        // --- Training Outpost (24000, 5000, r=400), beside the dummies. The two NPCs are OFFSET
        //     from each other so their labels don't overlap: gatekeeper at the north edge, buffer
        //     at the south. Buff up, walk 800 north to the dummies, teleport out when done. ---
        new("gatekeeper_training", "Gatekeeper Vess",    24000, 4800, NpcRole.Teleporter),
        new("buffer_training",     "Spirit Helper Ilva", 24000, 5200, NpcRole.Buffer),

        // --- Warehouse Keepers: one per MAIN town (offset from the gatekeeper so labels don't overlap).
        //     Talking opens the private warehouse (deposit/withdraw). ---
        // Brackenford's keeper joins the VENDOR cluster (owner): banking and shopping are the same
        // errand — you sell, you stash, you buy — so they belong in one stop.
        new("warehouse_brackenford", "Keeper Bram",   25650, 24700, NpcRole.Warehouse),
    }.Concat(RingTownServices()).ToArray();

    /// <summary>Every MAIN town carries the same service set (owner, 2026-07-29): a buffer, a
    /// warehouse keeper, the THREE vendors and a gatekeeper. A town you cannot resupply in is a town
    /// you teleport out of, which made the ring towns waypoints rather than places.
    ///
    /// Only the STARTER town differs, and only by holding what you use once: the class masters and the
    /// skill-resetter (hand-placed above). The 3rd-class Grandmaster moved OUT to Greymarsh, the first
    /// town whose band spans level 40 — you should not be walking back to the newbie town to take a
    /// level-40 quest. Later 3rd-class quest NPCs belong beside him there.
    ///
    /// Generated rather than hand-listed: six towns × five NPCs is thirty rows that must all agree
    /// about their own layout, and the previous hand-listing had already drifted (keepers at the town
    /// edge, no vendors or buffer at all outside Brackenford).</summary>
    private static IEnumerable<NpcDef> RingTownServices()
    {
        // (id-suffix, display town, centre X, centre Y, keeper name, buffer name, apothecary,
        //  armsmaster, outfitter)
        var towns = new (string Key, float X, float Y, string Keeper, string Buffer,
                         string Potions, string Weapons, string Armor)[]
        {
            ("stonewatch", 24000, 10000, "Keeper Osric", "Spirit Helper Aven",
                "Apothecary Rilla", "Armsmaster Toren", "Outfitter Maeve"),
            ("greymarsh",  36000, 33000, "Keeper Wyn",   "Spirit Helper Cael",
                "Apothecary Thessa", "Armsmaster Rurik", "Outfitter Nerys"),
            ("ironreach",  24000, 38000, "Keeper Dagr",  "Spirit Helper Orla",
                "Apothecary Venn", "Armsmaster Hakon", "Outfitter Brida"),
            ("frostmere",  12000, 15000, "Keeper Hald",  "Spirit Helper Ylva",
                "Apothecary Nim", "Armsmaster Bors", "Outfitter Sigrid"),
        };

        foreach (var t in towns)
        {
            // Same shape as Brackenford, scaled to the ring towns' smaller radius (2000): the three
            // vendors + the keeper cluster EAST as one shopping stop, the buffer sits bottom-centre,
            // and the gatekeeper stands alone top-centre. ~300-450 apart so labels don't overlap.
            yield return new NpcDef($"merchant_potions_{t.Key}", t.Potions, t.X + 600, t.Y - 200, NpcRole.Vendor);
            yield return new NpcDef($"merchant_gear_{t.Key}",    t.Weapons, t.X + 950, t.Y - 200, NpcRole.Vendor);
            yield return new NpcDef($"merchant_armor_{t.Key}",   t.Armor,   t.X + 775, t.Y + 150, NpcRole.Vendor);
            yield return new NpcDef($"warehouse_{t.Key}",        t.Keeper,  t.X + 950, t.Y + 500, NpcRole.Warehouse);
            yield return new NpcDef($"buffer_{t.Key}",           t.Buffer,  t.X,       t.Y + 800, NpcRole.Buffer);
        }

        // The 3rd-class master lives in GREYMARSH (band 34-46) — the first town whose levels reach the
        // level-40 discipline change (owner). He stands on the WEST side, mirroring Brackenford's
        // "services east, class business west" split, and this is where the other 3rd-class quest NPCs
        // should join him rather than accumulating back in the starter town.
        yield return new NpcDef("master_class3", "Grandmaster Thorne", 35100, 33000, NpcRole.ClassChange);
    }

    public static readonly RoadPath[] Roads =
    {
        // Spokes from Brackenford (centre) out to each of the four other cities. The Emberfall and
        // Duskvale spokes went with their towns; the remaining four are the level path through the
        // world — north to Stonewatch, then round to Greymarsh, Ironreach and finally Frostmere.
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(24000, 10000) }), // Stonewatch (N)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(36000, 33000) }), // Greymarsh (SE)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(24000, 38000) }), // Ironreach (S)
        new(Width: 600, Points: new[] { new MapPoint(24000, 24000), new MapPoint(12000, 15000) }), // Frostmere (NW)
    };

    /// <summary>True if (x,y) lies on a road strip (used to keep mobs off roads).</summary>
    public static bool OnRoad(float x, float y)
    {
        foreach (var road in Roads)
            if (road.Contains(x, y))
                return true;
        return false;
    }

    /// <summary>The level band of the normal hunting grounds around a town: the
    /// min/max level across spawn zones whose nearest town is this one. Returns null
    /// if the town has no normal zones beside it (elite/boss spots are ignored).</summary>
    public static (int Min, int Max)? LevelRangeNear(SafeZone town)
    {
        int min = int.MaxValue, max = 0;
        foreach (var z in SpawnZones)
        {
            if (z.Rank != MobRank.Normal) continue;
            if (NearestSafeZone(z.X, z.Y).Id != town.Id) continue;
            min = Math.Min(min, z.MinLevel);
            max = Math.Max(max, z.MaxLevel);
        }
        return max == 0 ? null : (min, max);
    }

    /// <summary>Find a placed NPC by id (null if none).</summary>
    public static NpcDef? NpcById(string id) =>
        Array.Find(Npcs, n => n.Id == id);

    /// <summary>A "where to find this mob" hint: the nearest town to the spawn zones
    /// that contain <paramref name="mobTypeId"/> within [minLevel,maxLevel], plus that
    /// band's levels. Returns ("", 0, 0) if the mob isn't placed in any matching zone.</summary>
    public static (string Town, int Min, int Max) MobHuntingGround(string mobTypeId, int minLevel, int maxLevel)
    {
        int min = int.MaxValue, max = 0;
        SafeZone? best = null;
        float bestSq = float.MaxValue;
        foreach (var z in SpawnZones)
        {
            if (Array.IndexOf(z.MobTypes, mobTypeId) < 0) continue;
            // Honour the quest's level band when one is set (0 = unbounded).
            if (maxLevel > 0 && z.MinLevel > maxLevel) continue;
            if (minLevel > 0 && z.MaxLevel < minLevel) continue;
            min = Math.Min(min, z.MinLevel);
            max = Math.Max(max, z.MaxLevel);
            var town = NearestSafeZone(z.X, z.Y);
            float dx = z.X - town.X, dy = z.Y - town.Y;
            float sq = dx * dx + dy * dy;
            if (sq < bestSq) { bestSq = sq; best = town; }
        }
        return best is null ? ("", 0, 0) : (best.Name, min, max);
    }

    /// <summary>Clamp a position to stay inside the world border.</summary>
    public static (float X, float Y) ClampToBorder(float x, float y) =>
        (Math.Clamp(x, Border.MinX, Border.MaxX),
         Math.Clamp(y, Border.MinY, Border.MaxY));
}

public record WorldBorder(float MinX, float MinY, float MaxX, float MaxY);

public record MapPoint(float X, float Y);

/// <summary>Mob rank — drives default respawn timing and lets the UI label
/// elites/bosses. Normal uses the zone's respawn range; Elite/Boss usually set
/// long ranges explicitly.</summary>
public enum MobRank { Normal = 0, Elite = 1, Boss = 2 }

/// <summary>When a zone is active. Always = 24h; Day/Night gate by the game
/// clock so you can run day-only and night-only zones (overlap two zones at the
/// same spot with different mobs to swap them at dusk/dawn).</summary>
public enum ActiveTime { Always = 0, Day = 1, Night = 2 }

/// <summary>
/// A spawn zone: a disc that maintains up to MaxCount living mobs. When a mob
/// dies the zone waits RespawnSeconds (± Variance) then respawns it — but never
/// exceeds MaxCount, and only while the zone is active for the current time of
/// day. Respawn timing is authored in SECONDS (real seconds); the in-game
/// description shows "[center ±variance]".
/// </summary>
public record SpawnZone(
    float X, float Y, float Radius,
    int MinLevel, int MaxLevel,
    string[] MobTypes, int MaxCount,
    double RespawnSeconds = 10, double RespawnVariance = 0,
    MobRank Rank = MobRank.Normal,
    ActiveTime Active = ActiveTime.Always,
    // Normally a NAMED mob brings its own level and the band here is descriptive. Set this and the
    // ZONE wins: every spawn rolls MinLevel..MaxLevel regardless of the template. Used by the top
    // field so the level-85 roster can fill 86-90 until creatures are authored for that band — a
    // deliberate reuse, not a fallback (owner, 2026-07-29).
    bool ForceZoneLevel = false,
    // WHICH mob types attack on sight here. null = just the first entry; a list = exactly those;
    // an empty list = none. See IsAggressiveType.
    string[]? AggressiveTypes = null)
{
    /// <summary>Stable id from coordinates+rank, used to persist boss timers.</summary>
    public string Id => $"{(int)X}_{(int)Y}_{Rank}";

    /// <summary>Does EVERY aggressive template in this zone actually attack on sight?
    ///
    /// Only dungeons/instances and elite/boss grounds (owner, playtest-13). Out in the ordinary
    /// fields only the AUTHORED types are aggressive — see <see cref="IsAggressiveType"/> — because 71 of
    /// the 80 templates are flagged aggressive, and a level-22 champion walking into a 22-28 field was
    /// being jumped by casters and melee at once and simply dying. Danger should be somewhere you
    /// CHOOSE to go.
    ///
    /// Dungeons are the negative quadrant by construction (the overworld lives in [0, Zone*]), so
    /// that is what identifies one — no extra flag to keep in sync.</summary>
    public bool AllAggressive => Rank != MobRank.Normal || X < 0 || Y < 0;

    /// <summary>Which mob types in this zone attack on sight. AUTHORED per zone, not positional
    /// (owner, 2026-07-29) — a field might want two of five to be dangerous, or none at all, and
    /// "whichever is listed first" cannot express either.
    ///
    ///   • <c>null</c> (the default) — the FIRST entry in <see cref="MobTypes"/> is aggressive. This
    ///     is just a sane default so a new zone is never accidentally wall-to-wall aggro.
    ///   • a list — exactly those types, however many.
    ///   • an EMPTY list — nothing here attacks on sight; a genuinely peaceful hunting field.
    ///
    /// A template that is passive stays passive either way: this can only ever REMOVE aggression,
    /// never grant it (see GameLoopService.ResolveAggression).</summary>
    public bool IsAggressiveType(string mobId) =>
        AggressiveTypes is null
            ? MobTypes.Length > 0 && mobId == MobTypes[0]
            : Array.IndexOf(AggressiveTypes, mobId) >= 0;

    public bool IsActiveAt(DayPhase phase) => Active switch
    {
        ActiveTime.Day => phase == DayPhase.Day,
        ActiveTime.Night => phase == DayPhase.Night,
        _ => true
    };

    /// <summary>Human-readable respawn label, e.g. "2m 0s ±30s" or "21h ±3h".</summary>
    public string RespawnLabel => $"{Fmt(RespawnSeconds)} ±{Fmt(RespawnVariance)}";

    private static string Fmt(double seconds)
    {
        if (seconds >= 3600) return $"{seconds / 3600:0.#}h";
        if (seconds >= 60) return $"{(int)(seconds / 60)}m {(int)(seconds % 60)}s";
        return $"{(int)seconds}s";
    }
}

public record RoadPath(float Width, MapPoint[] Points)
{
    /// <summary>Is (px,py) within Width of any segment of this path?</summary>
    public bool Contains(float px, float py)
    {
        for (int i = 0; i < Points.Length - 1; i++)
        {
            if (DistanceToSegment(px, py, Points[i], Points[i + 1]) <= Width)
                return true;
        }
        return false;
    }

    private static float DistanceToSegment(float px, float py, MapPoint a, MapPoint b)
    {
        float abx = b.X - a.X, aby = b.Y - a.Y;
        float apx = px - a.X, apy = py - a.Y;
        float lenSq = abx * abx + aby * aby;
        float t = lenSq <= 0 ? 0 : Math.Clamp((apx * abx + apy * aby) / lenSq, 0, 1);
        float cx = a.X + abx * t, cy = a.Y + aby * t;
        float dx = px - cx, dy = py - cy;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

}

public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2, Teleporter = 3, Buffer = 4, SkillReset = 5, Warehouse = 6 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role);

/// <summary>A safe zone (city/castle). Id is referenced by teleports later.</summary>
public record SafeZone(string Id, string Name, float X, float Y, float Radius);

