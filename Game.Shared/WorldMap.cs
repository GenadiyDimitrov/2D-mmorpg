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
    /// Mob spawn zones — the circles that actually maintain living mobs.
    ///
    /// The OVERWORLD's zones are GENERATED from <see cref="WorldPlan"/>: 4-level bands (2 at the top),
    /// grouped into fields, grouped under cities, with each camp's roster chosen BY LEVEL from
    /// <see cref="MobCatalog"/>. They used to be hand-placed circles with hand-listed rosters, and that is
    /// exactly how a level-12 Werewolf came to share the starter camp with a level-1 Ridgeback Pup — a
    /// natural-level mob ignores the zone's band, so a 1-12 roster spawned both (owner: "how exactly am I
    /// supposed to kill a pig next to a werewolf"). Deriving the roster from the band makes that
    /// impossible rather than merely discouraged. To reshape the overworld, edit WorldPlan.Plans.
    ///
    /// What stays HAND-AUTHORED below is everything that is not a level band: the training dummies (fixed
    /// levels, immortal, no drops), the world boss and its trash flanks, and the Hollow Crypt dungeon rooms.
    /// </summary>
    public static readonly SpawnZone[] SpawnZones = WorldPlan.SpawnZones.Concat(new SpawnZone[]
    {
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

        // The two dummies that hit BACK, level 80 (owner, `56c`). Same row, past the level-80 target,
        // so the training ground reads left-to-right as "things you hit" then "things that hit you".
        // Stand within GameConstants.DummyStrikeRange and each lands one hit per tick.
        new(X: 26500, Y: 4000, Radius: 200, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "dummy_magic" }, MaxCount: 1, RespawnSeconds: 5),
        new(X: 27500, Y: 4000, Radius: 200, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "dummy_physical" }, MaxCount: 1, RespawnSeconds: 5),

        // ===== THE PROVING GROUNDS (BL-47 step 2) — the five creatures built like PLAYERS, each with
        //       the ordinary creature of its own level standing beside it. His step 2: *"and later we
        //       can do 2~5 mobs so I can test."*
        //
        // Laid out as FIVE COLUMNS on the row south of the dummies, so the comparison is a walk and not
        // a memory: in every column the PLAYER-BUILT creature is the north one (y=2600) and its CURVE
        // TWIN — an ordinary MobBaseStats mob of the same level, no passives — is directly south of it
        // (y=2000). Kill one, turn round, kill the other.
        //
        //   col 1  x=22200  Lv 40 · Goblin Raider          — the baseline: gear alone, no stat passive
        //   col 2  x=23400  Lv 45 · Goblin Elder Raider    — THE SAME BUILD, +5 levels (his ±5 band)
        //   col 3  x=24600  Lv 60 · Cairn Lich             — the caster, and its x3.3 HP passive
        //   col 4  x=25800  Lv 80 · Fallen Seraph          — the top band, with a x1.55 attack passive
        //   col 5  x=27000  Lv 80 · Seraph, Runebearer     — the same, but a HELD WAR RUNE and NO passive
        //
        // 1 vs 2 answers "does one loadout cover a ±5 band"; 4 vs 5 answers "can a held rune replace an
        // authored attack passive". Nothing here is aggressive and nothing drops loot — you pick the
        // fight and the only thing that changes hands is exp.
        //
        // ⚠ These sit inside the TRAINING GROUNDS field polygon, which was extended south to hold them
        // (Regions.cs). Move a column and that polygon has to follow it, exactly as the striking dummies
        // taught: a spawner outside every field fails ValidateSpawnersInFields and the server will not boot.
        new(X: 22200, Y: 2600, Radius: 150, MinLevel: 40, MaxLevel: 40,
            MobTypes: new[] { "demo_goblin_raider" }, MaxCount: 1, RespawnSeconds: 15),
        new(X: 22200, Y: 2000, Radius: 150, MinLevel: 40, MaxLevel: 40,
            MobTypes: new[] { "demo_curve_40" }, MaxCount: 1, RespawnSeconds: 15),

        new(X: 23400, Y: 2600, Radius: 150, MinLevel: 45, MaxLevel: 45,
            MobTypes: new[] { "demo_goblin_raider_elder" }, MaxCount: 1, RespawnSeconds: 15),
        new(X: 23400, Y: 2000, Radius: 150, MinLevel: 45, MaxLevel: 45,
            MobTypes: new[] { "demo_curve_45" }, MaxCount: 1, RespawnSeconds: 15),

        new(X: 24600, Y: 2600, Radius: 150, MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "demo_lich" }, MaxCount: 1, RespawnSeconds: 15),
        new(X: 24600, Y: 2000, Radius: 150, MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "demo_curve_60" }, MaxCount: 1, RespawnSeconds: 15),

        new(X: 25800, Y: 2600, Radius: 150, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "demo_seraph" }, MaxCount: 1, RespawnSeconds: 15),
        new(X: 25800, Y: 2000, Radius: 150, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "demo_curve_80" }, MaxCount: 1, RespawnSeconds: 15),

        new(X: 27000, Y: 2600, Radius: 150, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "demo_seraph_rune" }, MaxCount: 1, RespawnSeconds: 15),
        new(X: 27000, Y: 2000, Radius: 150, MinLevel: 80, MaxLevel: 80,
            MobTypes: new[] { "demo_curve_80" }, MaxCount: 1, RespawnSeconds: 15),

        // ===== Boss placeholders (more bosses/instances later) =====
        // The lone emberwyrm ELITE that used to roam here is GONE: every Frostmere field generates its
        // own elite camps (78 / 80 / 84 / 90), placed 1500 out from the camp whose band holds them — so a
        // hand-placed elite at a hand-picked level was both redundant and the one spawner most likely to
        // land on top of a generated camp.
        new(X: 24000, Y: 45000, Radius: 250,  MinLevel: 60, MaxLevel: 60,
            MobTypes: new[] { "valley_treant" }, MaxCount: 1,
            RespawnSeconds: 21 * 3600, RespawnVariance: 3 * 3600, Rank: MobRank.Boss),

        // ── WYRMFALL BASIN — the A-BAND FIELD BOSS (`BL-247`, his *"fill the gap with the elits+boss"*) ──
        // The Emberwyrm Matriarch at 78, north-west of Frostmere, laid out exactly like the Sunken Vale:
        // the boss alone in the centre, two trash flanks 3500u out so you reach her without an escort.
        //
        // 🔑 WHY A BOSS AND NOT ANOTHER ELITE CAMP. `MobCatalog.EnchantScrollDrops` pays an elite the
        // band's ORDINARY scroll and nothing else; the Greater and the Safe scroll are boss-only (§100,
        // 2026-09-16), and a boss pays them for its OWN band. The A band is 76-79, so `scroll_greater_a`
        // and `scroll_safe_a` had no source of any kind in the game until this spawner existed — not a
        // rare one, none. 78 puts her in the middle of the band rather than on either edge of it.
        //
        // ⚠ Same 21h ± 3h timer as the treant. A boss gates a one-off, never a supply (0.09 kills/h);
        // the A-band ELITE camp in Frostmere Wastes is what a farmer actually clears.
        new(X: 7000, Y: 27000, Radius: 250, MinLevel: 78, MaxLevel: 78,
            MobTypes: new[] { "emberwyrm_matriarch" }, MaxCount: 1,
            RespawnSeconds: 21 * 3600, RespawnVariance: 3 * 3600, Rank: MobRank.Boss),

        // The flanks. Roster is what the A band actually holds — the drakes nest here and the Redhorn
        // and Sunland warbands are up here raiding the nests, which is also where they live in the
        // Frostmere Wastes camps two fields south.
        new(X: 3500, Y: 27000, Radius: 1400, MinLevel: 76, MaxLevel: 79,
            MobTypes: new[] { "emberwyrm_drake", "redhorn_general", "sunland_orc_captain" }, MaxCount: 7,
            RespawnSeconds: 26, RespawnVariance: 8),
        new(X: 10500, Y: 27000, Radius: 1400, MinLevel: 76, MaxLevel: 79,
            MobTypes: new[] { "emberwyrm_drake", "redhorn_soldier", "sunland_orc_commander" }, MaxCount: 7,
            RespawnSeconds: 26, RespawnVariance: 8),

        // Sunken Vale — trash for the treant BOSS field, kept on the flanks (>3500u from the boss) so you
        // reach the boss without an escort. Level 58-60 to sit just under the boss and match its band.
        new(X: 20500, Y: 45000, Radius: 1400, MinLevel: 58, MaxLevel: 60,
            MobTypes: new[] { "aether_wisp", "sand_ratman", "bogwood" }, MaxCount: 7,
            RespawnSeconds: 22, RespawnVariance: 7),
        new(X: 27500, Y: 45000, Radius: 1400, MinLevel: 58, MaxLevel: 60,
            MobTypes: new[] { "fen_lizardman", "cursed_blade", "wildhorn_scout" }, MaxCount: 7,
            RespawnSeconds: 22, RespawnVariance: 7),

        // ===== DUNGEONS: see DungeonLayout, which generates every one of them ==============
        // They used to be twelve literal circles here — four per dungeon on a diagonal — with three
        // hand-drawn polygons in Regions.cs that had to keep agreeing with them. Both sides are now
        // generated from ONE group list per dungeon, because his 2026-08-24 layout rule is a rule about
        // COUNTS: N mob groups means N-1 side rooms off a main corridor, with group N standing in front
        // of the boss at the end of it. Add a group there and the room, the wall and the outline all
        // appear together.
        //
        // ⚠ THE BAND IS STILL THE POINT (BL-65). His report: *"Now a 32 lvl mobs almost next to a 65
        // lvl which protect the 44 lvl boss ... The mob lvls are all over the place."* A mob with a
        // NATURAL level brings its own and the spawner's Min/Max is then only a label, so each roster is
        // stocked with creatures whose natural level sits in the band it advertises. The rosters moved
        // across to DungeonLayout unchanged.
    }).Concat(DungeonLayout.SpawnZones)
      // BL-79's guard posts — the town gates and the three guarded fields. Authored in WorldPlan
      // beside the fields they belong to, because a post's position is DERIVED from a city's radius
      // and a field's last camp; writing the coordinates out here would be two files agreeing by hand.
      .Concat(WorldPlan.GuardZones).ToArray();

    /// <summary>Safe zones (cities/castles). AUTHORED IN <see cref="Towns"/> — this forwards, so every
    /// existing call site is unchanged. They moved out because <see cref="SpawnZones"/> is generated from
    /// <see cref="WorldPlan"/>, which needs the city centres: leaving the towns here made the two types
    /// initialise each other and read a half-built array. See the comment on Towns.</summary>
    public static SafeZone[] SafeZones => Towns.All;

    /// <summary>The STARTER town (map centre). Used where "nearest" would leak information — a player
    /// released from jail is sent here rather than to whatever town happens to be closest, so the jail's
    /// location stays secret.</summary>
    public static SafeZone StartingTown => Towns.Starting;

    /// <summary>The safe zone nearest to a point (always returns one). Used where the question really is
    /// "which safe circle is closest" — labelling an NPC's or a quest giver's location, for instance.
    ///
    /// ⚠ NOT the right question for SENDING somebody home: this counts a dungeon entrance as a
    /// destination, and inside a dungeon the entrance is always the nearest one. Use
    /// <see cref="NearestTown"/> for that.</summary>
    public static SafeZone NearestSafeZone(float x, float y) => Nearest(x, y, dungeonEntrances: true);

    /// <summary>The nearest place that counts as a TOWN — the same search as
    /// <see cref="NearestSafeZone"/> with the dungeon doors taken out of it.
    ///
    /// <para>🔑 This is the Scroll of Return's answer (owner, 2026-08-24: *"using scroll of return ->
    /// returns me to the starting chamber of the crypt .. not a main town … the return scrolls should
    /// teleprt you back in town not in the start of the dungeon - its valid even for a instance (u
    /// reenter)"*). A dungeon entrance is a safe zone in every other respect, so the escape button was
    /// finding it first and putting you back on the doorstep of the place you were escaping. Escaping
    /// TO a dungeon door is not escaping; and since the door is a teleport destination, being sent to a
    /// real town costs you nothing but the trip back in.</para></summary>
    public static SafeZone NearestTown(float x, float y) => Nearest(x, y, dungeonEntrances: false);

    private static SafeZone Nearest(float x, float y, bool dungeonEntrances)
    {
        SafeZone best = SafeZones[0];
        float bestSq = float.MaxValue;
        foreach (var z in SafeZones)
        {
            if (!dungeonEntrances && z.DungeonEntrance) continue;
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

    /// <summary>Is <paramref name="npcId"/> the SAME SERVICE as <paramref name="baseId"/> — that is,
    /// the starter town's NPC or any ring town's copy of it? Every town's service NPC is named
    /// `{baseId}_{townKey}` (see <see cref="RingTownServices"/>), so this is the one place that rule is
    /// read rather than re-derived.
    ///
    /// Used by quests marked <see cref="QuestDef.AnyTownNpc"/>: a level-40 should not have to pay a
    /// gatekeeper to walk back to town 1 for a daily errand every town's Apothecary could hand out
    /// (owner, playtest-19 M11).</summary>
    public static bool IsSameService(string baseId, string npcId) =>
        !string.IsNullOrEmpty(baseId) && !string.IsNullOrEmpty(npcId)
        && (npcId == baseId || npcId.StartsWith(baseId + "_", StringComparison.Ordinal));

    /// <summary>The TELEPORTER standing in a safe zone, or null if it has none.
    ///
    /// A jump used to land you on the destination town's centre point, which is nowhere near its
    /// gatekeeper — so travelling on meant landing, then walking across town to the next gatekeeper
    /// (owner, playtest-19 M12). Arriving beside the gatekeeper makes the chain one tap.</summary>
    public static NpcDef? GatekeeperIn(SafeZone zone)
    {
        foreach (var n in Npcs)
        {
            if (n.Role != NpcRole.Teleporter) continue;
            float dx = n.X - zone.X, dy = n.Y - zone.Y;
            if (dx * dx + dy * dy <= zone.Radius * zone.Radius)
                return n;
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
    /// excludes the gatekeeper's own town, and always excludes a zone GATED to a
    /// different city (see <see cref="SafeZone.GatedByCityId"/>) — that is how the
    /// Hollow Crypt stopped appearing on the level-1 town's menu.</summary>
    public static IEnumerable<SafeZone> TeleportDestinationsFrom(string gatekeeperNpcId, SafeZone home)
    {
        if (GatekeeperDestinations.TryGetValue(gatekeeperNpcId, out var ids))
            return ids.Select(id => Array.Find(SafeZones, z => z.Id == id))
                      .Where(z => z is not null && z.Id != home.Id && OfferedFrom(z!, home))
                      .Select(z => z!);
        return SafeZones.Where(z => z.Id != home.Id && OfferedFrom(z, home));
    }

    /// <summary>May the gatekeeper standing in <paramref name="home"/> send you to
    /// <paramref name="zone"/>? True unless the zone is gated to some OTHER city. A curated
    /// <see cref="GatekeeperDestinations"/> list is filtered by this too: a hand-written menu
    /// naming a gated zone is a mistake, not an override.</summary>
    private static bool OfferedFrom(SafeZone zone, SafeZone home) =>
        zone.GatedByCityId.Length == 0 || zone.GatedByCityId == home.Id;

    /// <summary>NPCs placed in the world (quest givers, class-change masters).
    /// Stationary, non-combat. Add NPCs here; quests/class-changes reference
    /// them by Id.
    ///
    /// <para>🔑 `BL-319` (2026-09-28): every town NPC stands AT THE DOOR of its building — see
    /// <see cref="TownLayout"/>, which owns the buildings and the named door points (<see cref="TownLayout.XDoor"/>,
    /// <see cref="TownLayout.YDoor"/>). His grouping, from the `BL-319` note: *"weaon and armor vendors are in one
    /// shop, apoth(+ essence one) is his own, keeper is his onw building, anvil+craft also"*, a church in the big
    /// cities for the class master and the Mindwright, and the old Huntmaster is the Adventurers Guild's
    /// GUILD RECEPTIONIST (*"this one will give/reward quests"*). The hand-placed clusters and their staircase
    /// arithmetic are gone; the ≥ 200 Y stagger now lives in the door points, and <see cref="ValidateNpcLabels"/>
    /// still guards it at boot.</para></summary>
    public static readonly NpcDef[] Npcs = TownNpcs().Concat(new NpcDef[]
    {
        // --- Training Outpost (24000, 5000, r=400), beside the dummies. Not a town plan — the two NPCs are
        //     OFFSET so their labels don't overlap: gatekeeper at the north edge, buffer at the south. ---
        new("gatekeeper_training", "Gatekeeper Vess",    24000, 4800, NpcRole.Teleporter),
        new("buffer_training",     "Spirit Helper Ilva", 24000, 5200, NpcRole.Buffer),
    }).ToArray();

    /// <summary>The Master Crafter's NPC id (the STARTER town's copy — every town's copy answers to it
    /// through <see cref="IsSameService"/>).</summary>
    public const string CraftMasterId = "craft_master";

    /// <summary>The Anvil's NPC id (`BL-303`), the starter town's copy; every town's is <c>anvil_&lt;town&gt;</c>.</summary>
    public const string AnvilId = "anvil";

    /// <summary>Is this NPC id a Master Crafter (any town's copy)?</summary>
    public static bool IsCraftMaster(string npcId) => IsSameService(CraftMasterId, npcId);

    /// <summary>Every town carries the same service set (owner, 2026-07-29): a gatekeeper, a buffer, a
    /// warehouse keeper, the three vendors and (since `BL-319`) the Guild Receptionist. A town you cannot
    /// resupply in is a town you teleport out of.
    ///
    /// ⚠ Since `BL-303` the three MAJOR cities (<see cref="IsMajorCity"/>) also hold the Master Crafter, his Anvil and a
    /// Mindwright; Stonewatch and Ironreach do not. Each class master stands in the major city whose band
    /// reaches his change: Vael in Brackenford, the Grandmaster in Greymarsh (40), the Archmaster in Frostmere (76).
    ///
    /// ⚠ Brackenford's ids mostly carry no town suffix (<c>merchant_potions</c>, <c>resetter_main</c>, …): they
    /// predate the ring towns and quests name them. Every other town's id is <c>&lt;service&gt;_&lt;town&gt;</c>.</summary>
    private static IEnumerable<NpcDef> TownNpcs()
    {
        // (town id, id suffix, gatekeeper, keeper, buffer, apothecary, armsmaster, outfitter, receptionist)
        var towns = new (string Id, string Key, string Gate, string Keeper, string Buffer,
                         string Potions, string Weapons, string Armor, string Guild)[]
        {
            ("town_brackenford", "brackenford", "Gatekeeper Pell",  "Keeper Bram",  "Spirit Helper Nyra",
                "Apothecary Miren", "Armsmaster Dolan", "Outfitter Bryn",   "Guild Receptionist Cera"),
            ("town_stonewatch",  "stonewatch",  "Gatekeeper Soren", "Keeper Osric", "Spirit Helper Aven",
                "Apothecary Rilla", "Armsmaster Toren", "Outfitter Maeve",  "Guild Receptionist Radd"),
            ("town_greymarsh",   "greymarsh",   "Gatekeeper Maela", "Keeper Wyn",   "Spirit Helper Cael",
                "Apothecary Thessa", "Armsmaster Rurik", "Outfitter Nerys", "Guild Receptionist Sela"),
            ("castle_ironreach", "ironreach",   "Gatekeeper Vurst", "Keeper Dagr",  "Spirit Helper Orla",
                "Apothecary Venn", "Armsmaster Hakon", "Outfitter Brida",   "Guild Receptionist Torv"),
            ("town_frostmere",   "frostmere",   "Gatekeeper Khaz",  "Keeper Hald",  "Spirit Helper Ylva",
                "Apothecary Nim", "Armsmaster Bors", "Outfitter Sigrid",    "Guild Receptionist Ingra"),
        };

        foreach (var t in towns)
        {
            bool starter = t.Key == "brackenford";
            string Id(string service, string brackenford) => starter ? brackenford : $"{service}_{t.Key}";
            NpcDef At(string id, string name, Vec2 door, NpcRole role)
            {
                var p = TownLayout.At(t.Id, door);
                return new NpcDef(id, name, p.X, p.Y, role);
            }

            if (TownLayout.PlanOf(t.Id)?.Shape == TownShape.Y)
            {
                // ---- A Y TOWN: guild up the north path, shrine and keeper in the wedges, shops on the stem ----
                yield return At($"gatekeeper_{t.Key}", t.Gate, TownLayout.YDoor.Gatekeeper, NpcRole.Teleporter);
                yield return At($"hunter_{t.Key}", t.Guild, TownLayout.YDoor.Guild, NpcRole.QuestGiver);
                yield return At($"buffer_{t.Key}", t.Buffer, TownLayout.YDoor.Shrine, NpcRole.Buffer);
                yield return At($"warehouse_{t.Key}", t.Keeper, TownLayout.YDoor.Keeper, NpcRole.Warehouse);
                yield return At($"merchant_potions_{t.Key}", t.Potions, TownLayout.YDoor.Apothecary, NpcRole.Vendor);
                // The gear trade is split in two (owner, playtest-13): weapons, then armour/shields/jewels — one shop.
                yield return At($"merchant_gear_{t.Key}", t.Weapons, TownLayout.YDoor.Arms1, NpcRole.Vendor);
                yield return At($"merchant_armor_{t.Key}", t.Armor, TownLayout.YDoor.Arms2, NpcRole.Vendor);
                continue;
            }

            // ---- AN X CITY ----
            yield return At($"gatekeeper_{t.Key}", t.Gate, TownLayout.XDoor.Gatekeeper, NpcRole.Teleporter);
            yield return At($"hunter_{t.Key}", t.Guild, TownLayout.XDoor.Guild, NpcRole.QuestGiver);
            yield return At(Id("buffer", "buffer_newbie"), t.Buffer, TownLayout.XDoor.Shrine, NpcRole.Buffer);
            yield return At($"warehouse_{t.Key}", t.Keeper, TownLayout.XDoor.Keeper, NpcRole.Warehouse);
            yield return At(Id("merchant_potions", "merchant_potions"), t.Potions, TownLayout.XDoor.Apothecary, NpcRole.Vendor);
            yield return At(Id("merchant_gear", "merchant_gear"), t.Weapons, TownLayout.XDoor.Arms1, NpcRole.Vendor);
            yield return At(Id("merchant_armor", "merchant_armor"), t.Armor, TownLayout.XDoor.Arms2, NpcRole.Vendor);
            // The Master Crafter at the crafthall, his ANVIL in the yard behind (*"anvil and master are always
            // togheter"*, `BL-303`; *"later model will be just an anvil"*, so no title).
            yield return At(Id(CraftMasterId, CraftMasterId), CraftMasterName(t.Key), TownLayout.XDoor.Crafter, NpcRole.CraftMaster);
            yield return At(Id(AnvilId, AnvilId), "Anvil", TownLayout.XDoor.Anvil, NpcRole.Anvil);

            switch (t.Key)
            {
                case "brackenford":
                    // THE CHURCH: the 2nd-class master, the two tutorial quest givers, and the Mindwright at the
                    // north door. Skill reset un-learns the PERMANENT picks (the level-40 stat swaps); the gold
                    // is not refunded.
                    yield return At("master_class", "Class Master Vael", TownLayout.XDoor.Church1, NpcRole.ClassChange);
                    yield return At("priest_oren", "High Priest Oren", TownLayout.XDoor.Church2, NpcRole.QuestGiver);
                    yield return At("elder_marius", "Elder Marius", TownLayout.XDoor.Church3, NpcRole.QuestGiver);
                    yield return At(ResetterId, "Mindwright Sela", TownLayout.XDoor.ChurchNorth, NpcRole.SkillReset);
                    break;

                case "greymarsh":
                    // The 3rd-class master: Greymarsh is the first town whose band spans the level-40 change.
                    yield return At("master_class3", "Grandmaster Thorne", TownLayout.XDoor.Church1, NpcRole.ClassChange);
                    yield return At($"{ResetterId}_{t.Key}", MindwrightName(t.Key), TownLayout.XDoor.Church2, NpcRole.SkillReset);
                    // `BL-272` part 2 — the T52 ESSENCE SHOP, in the one town whose band (40-60) is T52's, in the
                    // Apothecary's building (*"apoth(+ essence one) is his own"*).
                    yield return At(ShopCatalog.EssenceMerchant, "Assayer Corvane", TownLayout.XDoor.Assayer, NpcRole.Vendor);
                    break;

                case "frostmere":
                    // The 4th-class master: the only town whose neighbours reach the level-76 ascension.
                    yield return At("master_class4", "Archmaster Sevrin", TownLayout.XDoor.Church1, NpcRole.ClassChange);
                    yield return At($"{ResetterId}_{t.Key}", MindwrightName(t.Key), TownLayout.XDoor.Church2, NpcRole.SkillReset);
                    // The SP BROKER at the Keeper's side door — SP bottles are a 76+ concept.
                    yield return At("sp_broker", "Ledgerkeep Mora", TownLayout.XDoor.KeeperSide, NpcRole.SpExchange);
                    // `BL-274` part 3 (0.208.0) — the three RECIPE GIVERS, at the crafthall. Each gives a T76 and a
                    // T80 daily and sends you to the other two.
                    yield return At(QuestCatalog.RecipeWeaponGiver, "Weaponwright Harrow", TownLayout.XDoor.CraftWest1, NpcRole.QuestGiver);
                    yield return At(QuestCatalog.RecipeArmourGiver, "Armourer Edda", TownLayout.XDoor.CraftWest2, NpcRole.QuestGiver);
                    yield return At(QuestCatalog.RecipeJewelGiver, "Jeweller Ossian", TownLayout.XDoor.CraftWest3, NpcRole.QuestGiver);
                    break;
            }
        }
    }

    /// <summary>`BL-303`: is this town a MAJOR city — Brackenford (the start), Greymarsh (the 3rd-class master, 40-65)
    /// or Frostmere (the 4th-class master, 76+)? They hold the Master Crafter, his Anvil and a Mindwright, and since
    /// `BL-319` they are the X-shaped cities (<see cref="TownLayout"/>); Stonewatch and Ironreach are Y towns with
    /// shops, buffer, gatekeeper, keeper and the Guild Receptionist. A METHOD: it is read while <see cref="Npcs"/> is
    /// being built.</summary>
    public static bool IsMajorCity(string townKey) => townKey is "brackenford" or "greymarsh" or "frostmere";

    /// <summary>The Mindwright's (skill reset) NPC id — Brackenford's; a major city's is <c>resetter_main_&lt;town&gt;</c>.</summary>
    public const string ResetterId = "resetter_main";

    private static string MindwrightName(string townKey) => "Mindwright " + (townKey == "greymarsh" ? "Ivo" : "Rhosa");

    /// <summary>A ring town's Master Crafter's display name. One ORDER with a chapter in every town, so the
    /// TITLE is constant and only the given name changes (the old Master Smiths' names, kept).</summary>
    private static string CraftMasterName(string townKey) => "Master Crafter " + townKey switch
    {
        "brackenford" => "Gorran",
        "stonewatch" => "Bern",
        "greymarsh" => "Kell",
        "ironreach" => "Odric",
        _ => "Fenn",
    };

    /// <summary>Startup guard for the ⚠ rule above: no two NPCs standing near each other may share a
    /// screen line. Two NPCs at the same Y draw their name plates at the same height, and one long name
    /// then paints over the neighbour's plate — hiding the quest "!"/"?" you were scanning the town for
    /// (owner, playtest-13). Layouts drift as NPCs are added, and the failure is invisible in code and
    /// obvious only on a phone screen, so it is checked at boot instead.
    ///
    /// "Near" = within <paramref name="near"/> on X; "same line" = within <paramref name="minDy"/> on Y.
    /// Throws with both names and coordinates so the fix is a one-line nudge.</summary>
    public static void ValidateNpcLabels(float near = 1500f, float minDy = 200f)
    {
        var bad = new List<string>();
        for (int i = 0; i < Npcs.Length; i++)
            for (int j = i + 1; j < Npcs.Length; j++)
            {
                var a = Npcs[i]; var b = Npcs[j];
                if (Math.Abs(a.X - b.X) <= near && Math.Abs(a.Y - b.Y) < minDy)
                    bad.Add($"{a.Name} ({a.X},{a.Y}) and {b.Name} ({b.X},{b.Y})");
            }
        if (bad.Count > 0)
            throw new InvalidOperationException(
                "NPC labels would overlap — nudge one of each pair diagonally (see the ⚠ note in " +
                "WorldMap.Npcs):\n  " + string.Join("\n  ", bad));
    }

    /// <summary>The roads between the cities: Brackenford out to each of the other four, the level path through
    /// the world (north to Stonewatch, round to Greymarsh, Ironreach and finally Frostmere).
    ///
    /// `BL-319`: each runs centre → its own GATE → the other town's gate → centre, so a road leaves town where the
    /// guards stand rather than through the wall. Kept 600 wide — it is the strip mobs are kept off.</summary>
    public static readonly RoadPath[] Roads =
        new[] { "town_stonewatch", "town_greymarsh", "castle_ironreach", "town_frostmere" }
            .Select(to => GateRoad("town_brackenford", to)).ToArray();

    private static RoadPath GateRoad(string fromId, string toId)
    {
        var a = Towns.ById(fromId)!; var b = Towns.ById(toId)!;
        var ga = TownLayout.GateToward(a.Id, b.X, b.Y); var gb = TownLayout.GateToward(b.Id, a.X, a.Y);
        return new RoadPath(Width: 600, Points: new[]
        {
            new MapPoint(a.X, a.Y), new MapPoint(ga.X, ga.Y), new MapPoint(gb.X, gb.Y), new MapPoint(b.X, b.Y),
        });
    }

    /// <summary>True if (x,y) lies on a road strip (used to keep mobs off roads).</summary>
    public static bool OnRoad(float x, float y)
    {
        foreach (var road in Roads)
            if (road.Contains(x, y))
                return true;
        return false;
    }

    /// <summary>The level band of the hunting grounds a city MANAGES — what a gatekeeper shows beside
    /// another city's name so "where am I going" is answered before you pay.
    ///
    /// Derived from the city's OWNED fields (<see cref="WorldPlan.FieldsOf"/>), not from "whichever normal
    /// spawn zones happen to be nearest this town". Nearest-town was a proxy that happened to agree with
    /// ownership; with fields reaching ~7k and cities 13-15k apart, one bearing re-aimed toward a
    /// neighbour is all it takes for the proxy to attribute a field to the wrong city.</summary>
    public static (int Min, int Max)? LevelRangeNear(SafeZone town)
    {
        int min = int.MaxValue, max = 0;
        foreach (var field in WorldPlan.FieldsOf(town.Id))
            foreach (var z in field.Zones)
            {
                if (z.Rank != MobRank.Normal) continue;
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
/// A spawner for ONE named template, layered on top of a zone's mixed roster. It keeps exactly
/// <paramref name="Count"/> of <paramref name="MobId"/> alive, and a death here respawns THAT
/// creature — not a fresh roll of the roster.
///
/// This is the fix for the owner's playtest-14 note: *"killing a werewolf guarantees the spawn of a
/// werewolf again, not a 4-mob rotation"*. In a camp with a five-type roster, a mixed spawner turns
/// every kill into a 1-in-5 chance of the thing you actually need, so farming a quest mob meant
/// clearing the whole camp and waiting — and the population of any one creature drifted with the
/// dice. A quest target gets its own guaranteed slice instead. Which templates qualify is DERIVED
/// from the quest catalogue (<see cref="QuestCatalog.KillTargets"/>), so a new kill quest is served
/// automatically.
/// </summary>
public record DedicatedSpawn(string MobId, int Count);

/// <summary>
/// A spawn zone: a disc that maintains up to MaxCount living mobs. When a mob
/// dies the zone waits RespawnSeconds (± Variance) then respawns it — but never
/// exceeds MaxCount, and only while the zone is active for the current time of
/// day. Respawn timing is authored in SECONDS (real seconds); the in-game
/// description shows "[center ±variance]".
///
/// On top of that mixed pool a zone may carry <see cref="DedicatedSpawn"/>s: per-template spawners
/// whose deaths respawn the SAME creature.
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
    string[]? AggressiveTypes = null,
    // Per-template spawners layered ON TOP of the mixed roster above. See DedicatedSpawn.
    DedicatedSpawn[]? Dedicated = null,
    // ===================================================================================
    //  HP MULTIPLIER — the ZONE places it, not the template (BL-78 item 1, owner 2026-08-27:
    //  "the 15k mobs are zone placed with x2/x3 hp .. some zones can have x1").
    //
    //  ⚠ THIS OVERRODE THE FILED PLAN. BL-78 item 1 was written as per-template MobMod.Hp
    //  authoring across the roster — 4 of our 80 templates carry one against IG's 23%. He moved
    //  the lever to the ZONE instead, which means the same creature reads x1 in one field and x3
    //  in another, and not one template is edited to get there.
    //
    //  His 15k: MobBaseStats.Hp(80) = 40 + 0.8*6400 = 5,160, so x2 = 10,320 and x3 = 15,480 —
    //  "the 80 mobs should have 15k not 5", exactly.
    //
    //  ⚠ IT IS NOT THE SAME RULE AS ForceZoneLevel ABOVE. A template's own LEVEL beats the zone's
    //  band; this multiplier is the zone's unconditionally, because it is the knob for making a
    //  FIELD feel heavy rather than for describing a creature.
    //
    //  ⚠ BOSS-RANK SPAWNS ARE EXEMPT — see the composition site in Entity.ApplyMobScale. 0.89.0
    //  measured every boss into his 12-25 minute band off a curve; letting a field's x3 through
    //  would trip a boss straight out of it, silently, from an edit that never mentions bosses.
    // ===================================================================================
    float HpScale = 1f,
    // `BL-280` (owner, 2026-09-24): *"make only 80+ mobs aggressive. So a 78~80 camp having 80 mobs make
    // the 80 mobs aggressive."* A SPAWN below this level never attacks on sight, whatever its type; 0 = no
    // level gate. It is per spawned creature, not per camp, because a ForceZoneLevel camp rolls one
    // template at several levels. Every generated normal camp sets it to 80.
    int AggressiveFromLevel = 0)
{
    /// <summary>Stable id from coordinates+rank, used to persist boss timers.</summary>
    public string Id => $"{(int)X}_{(int)Y}_{Rank}";

    /// <summary>The per-template spawners, never null.</summary>
    public DedicatedSpawn[] DedicatedSpawns => Dedicated ?? Array.Empty<DedicatedSpawn>();

    /// <summary>How many of this template the zone keeps alive in its OWN spawner (0 = it has none and
    /// is part of the mixed roster pool instead).</summary>
    public int DedicatedCount(string mobId)
    {
        foreach (var d in DedicatedSpawns)
            if (string.Equals(d.MobId, mobId, StringComparison.OrdinalIgnoreCase))
                return d.Count;
        return 0;
    }

    /// <summary>Total living mobs this zone maintains: the mixed pool PLUS every dedicated spawner.
    /// Dedicated counts are additive (owner: *"a self spawner that is on top of the one they are in
    /// right now"*) — a guaranteed quest population must not be paid for out of the camp's variety.</summary>
    public int TotalCount => MaxCount + DedicatedSpawns.Sum(d => d.Count);

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

// ⚠ APPEND ONLY — the client and the persisted spawn rows both read these by NUMBER.
public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2, Teleporter = 3, Buffer = 4, SkillReset = 5, Warehouse = 6, CraftMaster = 7, SpExchange = 8, Anvil = 9 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
/// <param name="CanDie">`BL-115`, his words: *"canDie if false hp can't go below 1"*. FALSE on every
/// NPC in the world today — they take the hit, the number floats, and the pool stops at 1: a training
/// dummy that happens to sell potions. It is a property rather than a blanket rule because he named
/// the pair that would be true/true (the watch), and because "immortal" and "will not fight back" are
/// two different statements about an NPC that a single flag would fuse.</param>
/// <param name="Retaliate">*"retaliate if false don't strike back just sit and take it"*. Also FALSE
/// everywhere today. ⚠ THE WATCH IS NOT AN NpcDef — the guards `BL-79` built are MOBS
/// (`MobType.Guard`), with the mob AI, the class kit, the real gear and a respawn timer, and they are
/// already the true/true pair by construction: they die and they hit back. Rebuilding them as NPCs to
/// carry these two booleans would throw all of that away to arrive back where they started. What the
/// guards were actually missing is the PvP gate, which was written but unreachable — see CanPvpHit.</param>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role,
                     bool CanDie = false, bool Retaliate = false);

/// <summary>A safe zone (city/castle). Id is referenced by teleports later.</summary>
/// <param name="GatedByCityId">Empty for a city — every gatekeeper offers it, which is what makes the
/// world one connected map. Set to a CITY id for a place that should be reached through ONE door: a
/// dungeon entrance belongs to the city whose hunting band matches the dungeon's, so finding it is
/// part of levelling into that band rather than a line on every menu from level 1. Enforced in
/// <see cref="WorldMap.TeleportDestinationsFrom"/>; the gated zone's own gatekeeper (if it has one)
/// still offers everything, so a dungeon is never a one-way trip.</param>
/// <param name="RegenBoost">Does standing here pay the town regen multiplier? TRUE for the five
/// CITIES only. FALSE for the training outpost and the three dungeon ENTRANCES (owner, playtest 27:
/// *"only in the big cities ..not in a starting point of elit dungeon ...I can sit with the healer
/// with 220mp/s regen and heal like crazy"*). A safe zone still does everything else it always did
/// there — no mobs, no aggro, no PvP — it just is not a rest stop, so an elite dungeon cannot be
/// farmed from a chair one step outside its door.</param>
/// <param name="DungeonEntrance">Is this a DUNGEON DOOR rather than a settlement? TRUE for the three
/// entrances generated by <see cref="DungeonLayout.EntranceZones"/>, false for everything else.
///
/// It exists for one rule (owner, 2026-08-24): *"the return scrolls should teleprt you back in town not
/// in the start of the dungeon - its valid even for a instance (u reenter)."* A Scroll of Return asks
/// for the nearest safe zone, and inside a dungeon the nearest safe zone is the dungeon's own door — so
/// the escape button was walking you back to the room you were trying to escape from. It is a separate
/// flag from <see cref="RegenBoost"/> on purpose: they happen to be false for the same three zones
/// today, but one is about resting and the other is about where "home" is, and the training outpost is
/// the case that separates them — no regen boost, but a perfectly good place to be sent home to.
/// See <see cref="WorldMap.NearestTown"/>.</param>
public record SafeZone(string Id, string Name, float X, float Y, float Radius, string GatedByCityId = "",
    bool RegenBoost = true, bool DungeonEntrance = false);

