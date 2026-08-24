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
        // The lone emberwyrm ELITE that used to roam here is GONE: every Frostmere field now generates its
        // own elite camp at its band cap (80 / 84 / 90), placed 1500 out from the field's top camp — so a
        // hand-placed elite at a hand-picked level was both redundant and the one spawner most likely to
        // land on top of a generated camp.
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
    }).Concat(DungeonLayout.SpawnZones).ToArray();

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

        // ⚠ NO TWO NPCs IN A CLUSTER SHARE A Y (owner, 2026-07-30). Side-by-side NPCs at the same Y put
        // their name labels on the same screen line, and a long name then covers the neighbour's plate —
        // including its quest "!"/"?", which is the one thing you are scanning the town for. Every
        // cluster is a DIAGONAL staircase instead: each NPC steps ~450 across and ~300 down from the
        // last, so labels never share a line and are still one short walk apart.

        // --- WEST: quests + class changes ---
        new("master_class",  "Class Master Vael",  22900, 23650, NpcRole.ClassChange),
        new("priest_oren",   "High Priest Oren",   22450, 23950, NpcRole.QuestGiver),
        new("elder_marius",  "Elder Marius",       22000, 24250, NpcRole.QuestGiver),
        // (The 3rd-class Grandmaster is NOT here — he stands in Greymarsh, below. See RingTownServices.)
        // --- EAST: the three vendors, one stop ---
        // (their wares are defined by ShopCatalog, keyed on these ids)
        new("merchant_potions", "Apothecary Miren", 25100, 23750, NpcRole.Vendor),
        // The gear trade is split in two (owner, playtest-13): one Armsmaster selling WEAPONS, one
        // Outfitter selling ARMOR, shields and jewels. A single vendor stocking the whole F/E/D ladder
        // at three qualities is ~150 rows, which is most of why the list read as "no idea which is which".
        new("merchant_gear",    "Armsmaster Dolan",  25550, 24050, NpcRole.Vendor),
        new("merchant_armor",   "Outfitter Bryn",    25000, 24350, NpcRole.Vendor),
        // Newbie buffer: blesses lvl 6-75 characters with a buffer's full buff set.
        new("buffer_newbie",    "Spirit Helper Nyra", 23400, 25550, NpcRole.Buffer),
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
        // The HUNTMASTER stands beside the gatekeeper, in every city (the ring towns' are generated
        // below). He hands out the repeatable hunting contracts for the fields THIS city manages, and
        // taking one is the errand immediately before "teleport me to the field" — so he belongs on
        // that walk rather than in the quest cluster on the far side of town.
        new("hunter_brackenford",     "Huntmaster Cera",   23300, 22500, NpcRole.QuestGiver),
        // The ring towns' gatekeepers stand TOP-CENTRE too, 900 above the town centre. They used to sit
        // on the centre point itself, which put them on the same screen line as the generated armsmaster
        // (and, in Greymarsh, the Grandmaster) — the exact label overlap the ⚠ note is about.
        new("gatekeeper_stonewatch",  "Gatekeeper Soren",  24000,  9100, NpcRole.Teleporter),
        new("gatekeeper_greymarsh",   "Gatekeeper Maela",  36000, 32100, NpcRole.Teleporter),
        new("gatekeeper_ironreach",   "Gatekeeper Vurst",  24000, 37100, NpcRole.Teleporter),
        new("gatekeeper_frostmere",   "Gatekeeper Khaz",   12000, 14100, NpcRole.Teleporter),

        // --- Training Outpost (24000, 5000, r=400), beside the dummies. The two NPCs are OFFSET
        //     from each other so their labels don't overlap: gatekeeper at the north edge, buffer
        //     at the south. Buff up, walk 800 north to the dummies, teleport out when done. ---
        new("gatekeeper_training", "Gatekeeper Vess",    24000, 4800, NpcRole.Teleporter),
        new("buffer_training",     "Spirit Helper Ilva", 24000, 5200, NpcRole.Buffer),

        // --- Warehouse Keepers: one per MAIN town (offset from the gatekeeper so labels don't overlap).
        //     Talking opens the private warehouse (deposit/withdraw). ---
        // Brackenford's keeper joins the VENDOR cluster (owner): banking and shopping are the same
        // errand — you sell, you stash, you buy — so they belong in one stop.
        new("warehouse_brackenford", "Keeper Bram",   25450, 24650, NpcRole.Warehouse),

        // --- THE CRAFTING QUARTER (`BL-05`, 2026-08-13). Five masters, one per profession: each hands
        //     out his own joining quest, grants his profession on completion, takes it back if you quit,
        //     and is the ONLY place his recipes can actually be made (owner: *"better at NPC — and craft
        //     happens with their respected masters … u compleate the quest and u can take his
        //     proffesion"*). The window still OPENS anywhere, in browse mode.
        //
        //     🔑 In EVERY town, not just here. His playtest-19 M11 complaint about the hunting contracts
        //     — *"i have no way to go back to the 1st town just to take it"* — is the same shape, and a
        //     level-80 flying to the starter town to make one S sword would be a worse version of it.
        //     The ring towns' five are generated in RingTownServices; these are Brackenford's, hand-placed
        //     because its own west side is already full of class masters.
        //
        //     SOUTH-WEST of town, on the diagonal staircase rule (⚠ above): no two share a Y, and none is
        //     within 200 Y of the Spirit Helper or the Mindwright they stand near.
        new("craft_weaponsmith",  "Master Smith Gorran",     22200, 25850, NpcRole.CraftMaster),
        new("craft_armorsmith",   "Master Armorer Halvard",  22350, 26150, NpcRole.CraftMaster),
        new("craft_jeweler",      "Master Jeweler Ysolde",   22500, 26450, NpcRole.CraftMaster),
        new("craft_potionmaster", "Master Apothecary Roderic", 22650, 25250, NpcRole.CraftMaster),
        new("craft_scribe",       "Master Scribe Alden",     22800, 25000, NpcRole.CraftMaster),
    }.Concat(RingTownServices()).ToArray();

    /// <summary>The NPC id of a profession's master (the STARTER town's copy — every town's copy answers
    /// to it through <see cref="IsSameService"/>). Empty for <see cref="Profession.None"/>.</summary>
    public static string CraftMasterId(Profession p) => p switch
    {
        Profession.WeaponSmith   => "craft_weaponsmith",
        Profession.ArmorSmith    => "craft_armorsmith",
        Profession.Jeweler       => "craft_jeweler",
        Profession.PotionMaster  => "craft_potionmaster",
        Profession.ScrollScribe  => "craft_scribe",
        _ => "",
    };

    /// <summary>The profession a craft-master NPC teaches, or <see cref="Profession.None"/> if the id is
    /// not a master's. Accepts every town's copy (<c>craft_jeweler_frostmere</c> as well as
    /// <c>craft_jeweler</c>), which is what makes joining and crafting town-agnostic.</summary>
    public static Profession CraftMasterProfession(string npcId)
    {
        foreach (Profession p in new[]
        {
            Profession.WeaponSmith, Profession.ArmorSmith, Profession.Jeweler,
            Profession.PotionMaster, Profession.ScrollScribe,
        })
            if (IsSameService(CraftMasterId(p), npcId)) return p;
        return Profession.None;
    }

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
                         string Potions, string Weapons, string Armor, string Hunter)[]
        {
            ("stonewatch", 24000, 10000, "Keeper Osric", "Spirit Helper Aven",
                "Apothecary Rilla", "Armsmaster Toren", "Outfitter Maeve", "Huntmaster Radd"),
            ("greymarsh",  36000, 33000, "Keeper Wyn",   "Spirit Helper Cael",
                "Apothecary Thessa", "Armsmaster Rurik", "Outfitter Nerys", "Huntmaster Sela"),
            ("ironreach",  24000, 38000, "Keeper Dagr",  "Spirit Helper Orla",
                "Apothecary Venn", "Armsmaster Hakon", "Outfitter Brida", "Huntmaster Torv"),
            ("frostmere",  12000, 15000, "Keeper Hald",  "Spirit Helper Ylva",
                "Apothecary Nim", "Armsmaster Bors", "Outfitter Sigrid", "Huntmaster Ingra"),
        };

        foreach (var t in towns)
        {
            // Same shape as Brackenford, scaled to the ring towns' smaller radius (2000): the three
            // vendors + the keeper cluster EAST as one shopping stop, the buffer sits bottom-centre,
            // and the gatekeeper stands alone at the centre. A DIAGONAL staircase (~300 across, ~300
            // down per step) so no two of them share a Y — see the ⚠ note on the Brackenford block.
            yield return new NpcDef($"merchant_potions_{t.Key}", t.Potions, t.X + 600, t.Y - 350, NpcRole.Vendor);
            yield return new NpcDef($"merchant_gear_{t.Key}",    t.Weapons, t.X + 900, t.Y -  50, NpcRole.Vendor);
            yield return new NpcDef($"merchant_armor_{t.Key}",   t.Armor,   t.X + 600, t.Y + 250, NpcRole.Vendor);
            yield return new NpcDef($"warehouse_{t.Key}",        t.Keeper,  t.X + 900, t.Y + 550, NpcRole.Warehouse);
            yield return new NpcDef($"buffer_{t.Key}",           t.Buffer,  t.X,       t.Y + 900, NpcRole.Buffer);
            // Beside the gatekeeper (top-centre, Y-900), west of it and on its own Y — see the
            // Brackenford Huntmaster for why he stands on the way OUT of town.
            yield return new NpcDef($"hunter_{t.Key}",           t.Hunter,  t.X - 700, t.Y - 650, NpcRole.QuestGiver);

            // The CRAFTING QUARTER (`BL-05`), west, clear of the east shopping cluster by more than the
            // label guard's 1500 so only the buffer, the hunter and the gatekeeper constrain the Y
            // staircase. The westernmost master takes the SMALLEST Y offset so every one of the five
            // stays inside the ring towns' 2000 radius (worst case here is 1803).
            var masters = new (string Key, Profession Prof, float Dx, float Dy)[]
            {
                ("weaponsmith",  Profession.WeaponSmith,  -1400,  -150),
                ("armorsmith",   Profession.ArmorSmith,   -1300,   150),
                // +650 rather than the staircase's +450: GREYMARSH alone carries a sixth NPC on this side
                // (Grandmaster Thorne, at +400), and the boot-time label guard caught the 50-unit clash.
                ("jeweler",      Profession.Jeweler,      -1200,   650),
                ("potionmaster", Profession.PotionMaster, -1100,  1200),
                ("scribe",       Profession.ScrollScribe, -1000,  1500),
            };
            foreach (var (key, prof, dx, dy) in masters)
                yield return new NpcDef(
                    $"craft_{key}_{t.Key}", CraftMasterName(prof, t.Key), t.X + dx, t.Y + dy,
                    NpcRole.CraftMaster);
        }

        // The 3rd-class master lives in GREYMARSH (band 34-46) — the first town whose levels reach the
        // level-40 discipline change (owner). He stands on the WEST side, mirroring Brackenford's
        // "services east, class business west" split, and this is where the other 3rd-class quest NPCs
        // should join him rather than accumulating back in the starter town.
        yield return new NpcDef("master_class3", "Grandmaster Thorne", 34800, 33400, NpcRole.ClassChange);

        // The 4th-class master lives in FROSTMERE — the last town on the level path out of
        // Brackenford (north to Stonewatch, round to Greymarsh, Ironreach, and finally here), so it
        // is the only town whose neighbours reach the level-76 ascension. Same west-side offset as
        // Thorne (-1200, +400), which is a Y the ring-town layout already leaves free: the crafting
        // quarter's jeweler was bumped to +650 for exactly this gap, and +400 clears the armorsmith
        // (+150) and the jeweler by 250 each — over ValidateNpcLabels' 200 minimum.
        yield return new NpcDef("master_class4", "Archmaster Sevrin", 10800, 15400, NpcRole.ClassChange);
    }

    /// <summary>A ring town's craft master's display name. The five are one ORDER with a chapter in every
    /// town, so the TITLE is constant and only the given name changes — which is also what tells a player
    /// that the Master Smith in Frostmere is the same service as the one in Brackenford.</summary>
    private static string CraftMasterName(Profession prof, string townKey)
    {
        string title = prof switch
        {
            Profession.WeaponSmith  => "Master Smith",
            Profession.ArmorSmith   => "Master Armorer",
            Profession.Jeweler      => "Master Jeweler",
            Profession.PotionMaster => "Master Apothecary",
            _                       => "Master Scribe",
        };
        // One given name per (profession, town). Invented, generic, no trademarks — see the naming rule
        // in CLAUDE.md.
        string given = (prof, townKey) switch
        {
            (Profession.WeaponSmith,  "stonewatch") => "Bern",  (Profession.WeaponSmith,  "greymarsh") => "Kell",
            (Profession.WeaponSmith,  "ironreach")  => "Odric", (Profession.WeaponSmith,  _)           => "Fenn",
            (Profession.ArmorSmith,   "stonewatch") => "Ruve",  (Profession.ArmorSmith,   "greymarsh") => "Marek",
            (Profession.ArmorSmith,   "ironreach")  => "Sten",  (Profession.ArmorSmith,   _)           => "Ilka",
            (Profession.Jeweler,      "stonewatch") => "Perrin",(Profession.Jeweler,      "greymarsh") => "Alys",
            (Profession.Jeweler,      "ironreach")  => "Corvin",(Profession.Jeweler,      _)           => "Runa",
            (Profession.PotionMaster, "stonewatch") => "Hesper",(Profession.PotionMaster, "greymarsh") => "Odal",
            (Profession.PotionMaster, "ironreach")  => "Wren",  (Profession.PotionMaster, _)           => "Sable",
            (_,                       "stonewatch") => "Quill", (_,                       "greymarsh") => "Tamsin",
            (_,                       "ironreach")  => "Ovid",  (_, _)                                 => "Yorick",
        };
        return $"{title} {given}";
    }

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
    DedicatedSpawn[]? Dedicated = null)
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

public enum NpcRole { QuestGiver = 0, ClassChange = 1, Vendor = 2, Teleporter = 3, Buffer = 4, SkillReset = 5, Warehouse = 6, CraftMaster = 7 }

/// <summary>A placed NPC. Id is referenced by quests + class-change requirements.</summary>
public record NpcDef(string Id, string Name, float X, float Y, NpcRole Role);

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

