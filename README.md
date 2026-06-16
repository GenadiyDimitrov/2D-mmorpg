# L2-like MMORPG — Phase 6 (skill learning, string skill ids, per-class files)

Server-authoritative multiplayer prototype. Phases 1-3 built movement,
interest management, combat, skills, buffs, the safe-zone town and banded
hunting grounds. **Phase 4 adds the RPG progression layer: the second-class
tree at level 20, items & equipment with grades, monster drops, inventory,
and a full trade-window system.**

## Run it (click and play)

Requirements: .NET 8 SDK, Windows, internet on first build (NuGet restore).

**Visual Studio:** open `Game.sln` → solution right-click → *Configure Startup
Projects…* → *Multiple startup projects* → **Game.Server** and
**Game.Client.Wpf** both *Start* → F5.

**CLI:** `dotnet run --project Game.Server`, then
`dotnet run --project Game.Client.Wpf` per client (unique names).

## Controls

| Action | Input |
|---|---|
| Move | Left-click ground |
| Target a mob & attack | Left-click a mob |
| Target a player (to trade) | Left-click a player |
| Skills | Keys **1-6** or skill bar |
| Inventory | **I** or the Inventory button |
| Equip / unequip | Click an item in the inventory |
| Second class (lvl 20) | Class button (top right) |
| Trade | Target a player → *Request Trade* in the target frame |
| Local / World / Whisper | plain / `!text` / `/w Name text` |

## New in Phase 6 (this build)

> **IMPORTANT — delete any old `game.db` before running.** Skill ids changed
> from ints to strings and characters now store learned skills + skill points,
> so the schema changed. Delete `game.db` (in `Game.Server/bin/Debug/net8.0/`)
> and a fresh one is created on launch.

### Skills are now learned with Skill Points
- Skills must be **learned** before use. You earn **Skill Points (SP)** alongside
  exp (≈ 1/4 of exp; tune `GameConstants.SkillPointRatio`).
- The **Skills window (K)** now has **two tabs**:
  - **Learned** — your usable skills, grouped by category (Physical / Magic /
    Buffs / Debuffs / Heals), each with a **To Bar** button.
  - **Skills to Learn** — unlearned skills **grouped by required level**, with a
    **Learn** button that's enabled only when your level + SP (and previous rank,
    for ranked lines) allow. Clicking Learn opens a **confirm popup** showing the
    description, details, and **SP cost in green/red**; confirm to learn it,
    after which it moves to the Learned tab and can be dragged to the bar.
- Hovering a skill shows its description + MP/cast/cooldown/duration.
- The **core class kit** (the mandatory upgrades like Greater Heal) is granted
  **free** on class change / level-up; the **extras** (HP Boost ranks, Wind Walk)
  are the ones you spend SP to learn. Learned skills + SP **persist**.

### String skill ids + per-class skill files
- Skill ids are now **stable strings** (`magic_bolt`, `greater_heal`,
  `hp_boost_1`). Same benefits as item keys: readable, reorder-safe,
  collision-guarded at startup.
- **One place to manage class skills:** `Game.Shared/RaceAndClasses/`. Each
  partial file registers a race+class line's skills with learn-levels, e.g.
  `Classes.Human.Mage.cs` declares the Human cleric/sorcerer learnable skills.
  Adding a skill to a class is a one-line `ClassSkills.Register(...)` edit.
- Example HP Boost line (3 ranks at 40/56/72 style levels) and Wind Walk are
  authored there to show the pattern; ranked skills must be learned in order.

### God race + God items (debug)
- A **God race (enum 99)** is creatable **only in DEBUG builds** but fully usable
  once made, with two God second classes (Demigod / Ascendant).
- Removed `legendary_windforce`; added two **God-tier** items (debug menu):
  **God's Judgment** (sword, attack + range 1000, all 8 attributes at 100%) and
  **God's Robes** (def/hp/mp/eva 1000, all armor attributes at 100%).

### New rarities & attributes
- Rarities extended: **Epic (3), Legendary (4), God (99)** — higher rarities roll
  more attributes.
- Two new attributes: **Evasion %** and **Defence %**, available on **E-grade and
  up** gear, and they apply to your real stats.

### Quest groundwork (data types only)
- Added quest **data types** (`QuestDef`, `QuestStep`, `QuestReward`,
  `CharacterQuestState`) and a nullable **`RequiredQuestId`** hook on second
  classes — so class-change-by-quest drops in later without a refactor. The live
  quest system (NPCs, dialog UI, tracking) is a **future phase**; an
  `EntityKind.Npc` is reserved for it.

## New in Phase 5.4 (this build)

### Buff system rebuilt for a future buffer class
- **`SkillEffect` is now a `[Flags]` enum.** One skill can carry several effects
  at once: `Effect = BuffAtk | BuffMoveSpeed | BuffCastSpeed`. No more inventing
  a new enum member per combination — add a flag once and combine freely.
- **Per-effect magnitudes with flat OR percent.** A skill carries
  `EffectMagnitude[]`, each entry `(Effect, Value, Mode)` where Mode is
  `Flat` or `Percent`. So Wind Walk = `(BuffMoveSpeed, 33, Flat)`, a haste buff =
  `(BuffMoveSpeed, 0.30, Percent)`, and you can even put **both on one buff**
  (33 flat + 5%). Stats combine as **`(base + ΣFlat) × (1 + ΣPercent)`** per stat.
- **Working cast-speed, attack-speed, and evasion buffs** (not just from items
  now) — a buffer skill can buff them directly.

### Buff stacking rules (exactly two mechanisms)
- **Explicit `Replaces` (unconditional):** a buff lists buff keys it overrides,
  e.g. `improved_movement` with `Replaces = ["wind_walk", "agility"]`. Casting it
  removes those buffs **no matter their rank or magnitude** — the author declared
  the override.
- **Same `BuffKey` compares by `Rank`:** recasting the same buff applies only if
  the incoming `Rank ≥ existing Rank` (a full replace, refreshing duration).
  A **weaker** recast does nothing — no downgrade, no refresh. Equal rank = refresh.
- Unrelated buffs (different key, not in a `Replaces` list) simply **stack**.
- Current skills use this already: War Cry (`might` rank 1) and Greater War Cry
  (`might` rank 2) auto-supersede by rank; Weakness/Greater Weakness likewise
  (`curse_def` rank 1/2); Battle Fury is a two-effect buff (atk + move speed).

### How to author a buff (for the future buffer class)
```csharp
new(skillId, "Improved Movement", BaseClass.Mage,
    SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
    MpCost: 30, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 0,
    DurationTicks: 1200, BuffKey: "improved_movement", Rank: 1,
    Replaces: new[] { "wind_walk", "agility" },
    Magnitudes: new EffectMagnitude[]
    {
        new(SkillEffect.BuffMoveSpeed, 40, ModifierMode.Flat),
        new(SkillEffect.BuffEvasion,   10, ModifierMode.Flat),
    },
    Description: "Combines and improves Wind Walk and Agility."),
```

## New in Phase 5.3 (this build)

### In-game day/night clock
- Time of day now cycles. The **one speed knob** is `GameClock.TimeScale` in
  `Game.Shared/GameClock.cs` — in-game seconds per real second. Default **6**
  (a full game day = 4 real hours; day and night ~2h each). For testing, set it
  to **60** (full day in 24 real minutes) or **600** (~2.4 min) to watch night
  fall fast. An in-game **clock + Day/Night indicator** shows at the top of the
  screen.

### Population cap + respawn delay (no more instant respawns)
- Each spawn zone now keeps **up to `MaxCount` mobs alive and never exceeds it**.
  When a mob dies, the zone waits a delay rolled from **`RespawnSeconds ±
  RespawnVariance`** (real seconds), then respawns — only if under the cap.
- The mob is removed on death and the **zone schedules** the replacement (the
  performant approach). A cosmetic corpse-fade can be layered on later.

### Elites & bosses
- A zone has a **`Rank`** (Normal / Elite / Boss). Elites are tougher (×4 HP,
  ×1.5 attack) with ~minutes respawn; bosses much tougher (×20 HP, ×2.5 attack)
  with hours-long respawn. Authoring example (already in `WorldMap.cs`):
  - **Elite**: `RespawnSeconds: 120, RespawnVariance: 30` → "2m 0s ±30s".
  - **Boss**: `RespawnSeconds: 21*3600, RespawnVariance: 3*3600` → "21h ±3h".
- **Boss/elite respawn timers are persisted** (real-world time) to the database,
  so a long timer **survives a server restart** — kill the boss, restart the
  server, and it's still on cooldown.
- On the map, elite zones are **amber** and boss zones **purple**, each labelled
  with rank, level, and the **[X ±Y] respawn** range.

### Day-only / night-only zones
- A zone's **`Active`** is `Always` (24h, default), `Day`, or `Night`. To swap
  mobs at dusk/dawn, overlap two zones at the same spot — one `Day`, one
  `Night` (there's a worked example in `WorldMap.cs` at 7500,9500). When the
  phase flips, inactive zones despawn and newly-active ones fill in.

### Where to edit
- **Speed of time:** `GameClock.TimeScale`.
- **Everything spawn-related:** `WorldMap.SpawnZones` — `MaxCount`,
  `RespawnSeconds`/`RespawnVariance`, `Rank`, `Active`, level band, mob types.

## New in Phase 5.2 (this build)

### The world is now visible and editable from one file
- **`Game.Shared/WorldMap.cs` is the single source of truth** for world layout —
  the server (spawning, collision) and client (drawing) both read it. To reshape
  the world you edit this one file.

### World border
- The playable rectangle is drawn as a **dashed outline**, so the edge is
  visible instead of an invisible wall. Defined by `WorldMap.Border`.

### Roads
- **Thick, semi-transparent grey strips** lead from town toward the hunting
  grounds; **mobs don't spawn on roads**, giving safe-ish corridors. Each road
  is a list of points with a half-width in `WorldMap.Roads` — add or reshape a
  road by editing its point list.

### Spawn zones (visible + self-documenting)
- Each spawn zone is drawn as a **light semi-transparent red disc** with a
  **label showing its level band and mob types**, so you can see at a glance
  where things spawn and what you'll meet. (Placeholder colour until real
  environment art.)
- **Fully editable** in `WorldMap.SpawnZones`. Your example —
  *"at (1000,1000) radius 800 spawn level 5-7 boars and spiders"* — is one line:
  ```csharp
  new(X: 1000, Y: 1000, Radius: 800, MinLevel: 5, MaxLevel: 7,
      MobTypes: new[] { "Boar", "Spider" }, MobCount: 10),
  ```
  The server spawns each zone independently (random point in the disc, avoiding
  the safe zone and roads), picks a random mob type and a level in the band, and
  the client tints + labels it automatically. Add as many zones as you like.

### How spawning works (for editing)
- On startup the server loops every `SpawnZone` and spawns `MobCount` mobs in
  it. Each mob remembers its home point and wanders/leashes around it; on death
  it respawns at home after the respawn timer. Change a zone's numbers and both
  the spawn behaviour and the on-screen overlay update together.

## New in Phase 5.1 (this build)

> **IMPORTANT — delete any old `game.db` before running.** Item IDs changed from
> integers to string keys, so the database schema changed. Delete the `game.db`
> file next to the server (or just let this fresh build create a new one). Old
> saves are not compatible.

### Item IDs are now stable string keys
- Every item has a permanent **string key** (e.g. `sword_e_rare`,
  `robe_f_common`, `potion_minor`) instead of a fragile integer. Keys are the
  item's identity — stored in saves, referenced by loot tables and the debug
  menu. **You never renumber**; new items just get new keys, and you can place
  them anywhere in the file. A **duplicate-key guard** at startup throws a clear
  error naming the collision instead of a cryptic crash.

### Full weapon & armor matrix
- Weapons are generated for **every type × grade × rarity**: sword, dual,
  bow, staff × {F, E} × {common, uncommon, rare} — keys like
  `bow_e_rare`. Armor likewise: heavy, light, robe × grade × rarity.
- **All classes can equip any weapon**; your skills determine whether a given
  weapon is actually good for you (matches the design doc). Bows/staves carry
  range; staves add MP; daggers are lower per-hit but suit the rogue's crit kit.
- Loot tables and starter gear now reference these keys; mages start with a
  staff + robe, fighters with a sword + leather.

### Legendary one-off
- **Windforce** (`legendary_windforce`): an E-grade bow with **5 fixed
  attributes** (Attack +30%, Attack Speed +25%, Move Speed +20%, HP +30%,
  MP +20%). Spawn it from the debug menu. Fixed attributes never reroll, unlike
  normal drops.

### Debug menu (DEBUG builds)
- Level +1; Windforce; a **Rare E of each weapon** (sword/dual/bow/staff);
  a **Rare E of each armor** (heavy/light/robe); and **x10** buttons for every
  scroll and potion (no more clicking one at a time). No shield yet — that
  arrives with block mechanics.

### War Cry split by class
- **Rogue & Archer**: War Cry becomes **Battle Fury** — +20% Attack **and**
  +15% Move Speed for 30s.
- **Warrior**: War Cry upgrades to **Greater War Cry** — +30% Attack.
- **Tank**: still swaps War Cry for **Fortify** (+50% Defence).

## New in Phase 5 (this build)

### Persistence (EF Core + SQLite)
- Characters and inventory now **survive server restarts**. The database is a
  single SQLite file (`game.db`) created automatically next to the server on
  first run — **no database server to install**.
- Characters **auto-save every 60s** and on logout; you log back in **where
  you left off** with your level, exp, stats, second class, and full inventory.
- Rolled item attributes persist via an EF Core **JSON column** (`OwnsMany …
  ToJson()`), so adding a new attribute type never needs a migration. Attributes
  roll once at drop time and are immutable thereafter (ready for a future
  "legendary reroll stone").
- **Swapping databases is one line** in `Program.cs`: replace `UseSqlite` with
  `UseNpgsql`/`UseSqlServer`; all the EF Core code is provider-agnostic.

### Accounts & character selection
- The flow is now **Register/Login → Character Select → Create/Enter**:
  - Account login screen with username + password (**PBKDF2-hashed**, never
    stored or sent in plaintext form).
  - Character selection lists all characters on the account; create new ones
    via the class-tree screen, then pick one to enter the world.
- **The first account registered becomes an admin** (convenient for testing).

### Admin role
- Admins use **slash-commands in chat**: `/help`, `/kick <name>`,
  `/ban <name>`, `/unban <name>`, `/jail <name>`, `/unjail <name>`, `/god`,
  `/where <name>`.
- **God mode** makes you immune to damage. **Jail** pins a player to the jail
  corner until released. **Ban** persists (works offline) and force-disconnects
  the player if they're online. Non-admin accounts can't invoke any of these —
  the server validates the admin flag, not the client.

> **First build note:** the server now references EF Core, so the first
> `dotnet build`/restore needs internet to pull the NuGet packages. After that
> it runs offline. The `game.db` file is created on first launch.

## New in Phase 4.8 (this build)

### Item attributes (rolled per drop)
- Weapons and armor now roll **random bonus attributes** when they drop, so two
  Steel Swords differ. **Count by rarity**: F common 0 / uncommon 1 / rare 2;
  E common 1 / uncommon 2 / rare 3 (and so on by grade).
- The **attribute pool and roll ranges scale by grade**, defined in
  `Game.Shared/Attributes.cs`:
  - **F grade** pool: Max HP%, Move Speed% — rolls 1–10%.
  - **E grade** pool adds Max MP%, Cast Speed%, Attack Speed%, Attack% — HP/MP
    roll 10–30%, the rest 1–20%.
  - B/A/S inherit the bigger pool with stronger ranges (ready to tune).
- Attributes live on the **item instance**, show in the **inventory tooltip**
  and the **equip-comparison popup**, and feed real stats: HP/MP/Attack %,
  move speed, and **Cast Speed / Attack Speed** (which shorten cast time and
  basic-attack interval).

### Cast speed display (WIT-centered)
- Cast reduction is now centered on **WIT 25 = baseline (0%)**. Each point
  above 25 casts faster, each below slower (1.2%/point). The Stats window shows
  **Cast Speed** broken into the WIT contribution and item contribution, and
  the **cast bar** shows the effective bonus next to the skill name.

### Base-skill unlock levels
- Per your fix, base skills no longer wait for class change: **Power Strike @1,
  War Cry @5** (Fighter); **Magic Bolt @1, Weakness @3, Heal @5** (Mage).

### Fixes
- **Potion buttons**: the rarity letter (C/U/R, top-left) and the count
  (bottom-right) are now separated and readable.
- **Equip-comparison popup**: clicking an item now always shows **its own
  stats**, with the difference vs the equipped item as a secondary column.
  Clicking the equipped item (or an item with no counterpart) shows real values
  instead of zeros, and lists the item's rolled attributes.

## New in Phase 4.7 (this build)

### Where to edit skills (for you)
- **`Game.Shared/Skills.cs`** is now the single skill-design file, split into:
  - `SkillCatalog.All` — every skill's numbers + description.
  - `ClassProgression` — **which skills each class gets**, whether a skill
    **replaces** a base skill, and the **unlock level**.
- To give the Witch a DoT the Sorcerer doesn't get: add the `SkillDef`, then
  add a row to `ClassProgression.RaceOverrides` keyed `(Race.Ork, Archetype.Nuker)`
  with `new SkillGrant(id, unlockLevel: 25)`. Nothing else changes — the server
  validates and the client renders from these tables. The hooks for per-race,
  level-gated flavour skills (DoT vs burst vs control) are already in place.

### Base skills upgrade on class change
- Second classes now **transform** the base kit instead of just adding a skill:
  - **Tank**: keeps Power Strike; War Cry → **Fortify** (+50% def).
  - **Warrior**: keeps War Cry; Power Strike → **Mighty Blow**.
  - **Rogue**: keeps War Cry; Power Strike → **Twin Slash**.
  - **Archer**: keeps War Cry; Power Strike → **Power Shot** (ranged).
  - **Healer** (Cleric/Shaman/Priest): Heal → **Greater Heal**, Magic Bolt →
    **Holy Strike** (weaker nuke), keeps Weakness.
  - **Nuker** (Sorcerer/Witch/Inquisitor): Magic Bolt → **Flamebolt** (strong
    nuke), keeps Heal, Weakness → **Greater Weakness**.

### Class identity through numbers
- **Mages** basic-attack for ~15% of attack power — they live on skills + MP.
- **Fighters/Warriors** hit full (110%) and brawl with attack + skills.
- **Archers** hit full + **+15% crit** — kite with basic attacks and crits.
- **Rogues** hit 65% but get **+20% crit and +evasion** — skills + crits.
- **Tanks** hit 55% but bring standout defence (Fortify, heavy armor).
- Mage main skills now **~4s cast (WIT reduces) and ~1s cooldown** so they
  chain-cast, and hit meaningfully harder than a mage's basic attack.

### Stackable consumables
- Potions and enchant scrolls now **stack into one inventory slot** with a
  quantity (1 → 2 → … → "99+"). Drops merge into the stack, using one consumes
  one, trading moves the whole stack and **merges** into the receiver's stack.
  Gear stays one-per-slot (each piece keeps its own enchant level).

### Chat moved up
- The chat panel sits higher so it no longer overlaps the skill bar buttons.

## New in Phase 4.6 (this build)

### Character creation — class tree
- The login screen is now a **button tree** instead of dropdowns:
  Race → Base Class → preview each Second Class. The right pane shows base
  stats (CON/ATK/WIT/DEX to compare), the class fantasy, the class-change
  stat bonus, and the full skill list with descriptions — so you know what
  you're getting into before creating. Name + Connect sit at the bottom.

### Skills window (K)
- Lists every skill you have with **description, MP cost, cast time,
  cooldown, and duration**. Each has a **To Bar** button.

### Configurable skill bar
- 8 slots. New skills **auto-fill the first free slot** when acquired (e.g.
  your signature skill on class change), but you can **assign** from the
  Skills window and **remove** by right-clicking a slot. Hotkeys 1-8.

### Buff bar + tooltips
- Active buffs/debuffs show as pills under the vital bars with **time left**;
  hover for a tooltip with the description and remaining seconds. Cast War Cry
  and you'll see the buff and its countdown. Debuffs are tinted red.

### Potions — fixed squares
- Three **always-visible colored squares** (green/blue/gold) bottom-right,
  with a **count badge** (caps at "99+"), **disabled when you have none**.
  Click or hotkeys Q/E. Counts also show as "99+" in the inventory.

### Inventory: remove + enchant
- Each item row has an **X** (destroy — sell/dismantle comes later) and, for
  gear, a **+** (enchant). The equip-compare popup is now **enchant-aware**
  (a +5 sword compares correctly against a +0).

### Enchanting
- Enchant gear **+1 to +16** with success bands from the design doc: **100%**
  to +3, **66%** +4-6, **40%** +7-9, **20%** +10-16. Each enchant level adds
  +20% of base bonus +1 flat. Three scrolls differ on failure:
  - **Common**: the item **breaks**.
  - **Uncommon**: enchant **resets to +0**.
  - **Rare**: enchant **drops by 1**.
  Scrolls **drop rarely from higher-level mobs** (rarer than any other loot;
  the better the scroll, the higher the level floor and the lower the odds).

### Debug menu (DEBUG builds only)
- A **Debug** button (only compiled in Debug configuration) opens a panel to
  grant scrolls, potions, F/E gear, and **Level +1** for testing. Both the
  client button and the server endpoints are `#if DEBUG`-gated, so a Release
  build has none of it.

## New in Phase 4.5 (this build)

### UI overhaul
- **Three colored vital bars** top-left: HP (red), MP (blue), EXP (gold),
  each with live numbers, replacing the old text line.
- **Stats window** — the *Stats* button (or **C**) opens a panel next to the
  inventory showing CON/ATK/WIT/DEX, max HP/MP, attack power, defence,
  accuracy/evasion, crit %, and attack range. It updates live on level-up,
  equip, and class change.
- **Equip comparison popup** — clicking an inventory item opens a popup that
  diffs the item against what's equipped in that slot (green = upgrade, red =
  downgrade) with **Equip/Close** buttons, instead of equipping instantly.
- **Chat tabs fixed** — the All/World/Local/Whisper tabs now sit at the bottom
  of the chat box (inside it), not floating above the panel.

### Per-mob loot tables
- Drops are now **per mob type**, not a global roll. Each mob has a loot table
  of (item, chance, mob-level band): Boars drop weapons, Wolves drop armor,
  Slimes drop robes/mage gear, Spiders drop light armor + bows, Bandits drop
  swords and the best F-grade gear. Low-level kills give F grade; level-11+
  kills give E grade — all defined in `LootTables` in `Game.Shared/Items.cs`,
  one dictionary keyed by mob name. Each table entry rolls independently, so a
  kill can drop zero, one, or several items.

### Potions (grade/rarity based)
- Three healing potions on a **shared 30s cooldown**, used from the **potion
  action bar** (hotkeys **Q**/**E**) or by **clicking them in the inventory**:
  - *Minor* (common): heals 1% max HP/sec for 15s
  - *Healing* (uncommon): 2% max HP/sec for 15s
  - *Greater* (rare): instant 50% max HP heal
- Potions are a **separate effect channel from natural regen** — they tick
  during combat too. **Rarity override**: a higher-rarity potion cancels a
  lower one's effect; same rarity restarts it (safe-guarded, though the
  cooldown normally prevents it). You start with two Minor and one Greater to
  test. Any mob can also drop potions on top of its gear table.

## New in Phase 4

### Second-class tree (level 20)
- At level 20 the **Class** button opens your six race/base-appropriate
  options — the 18 design-doc classes (Beast, Templar, Knight, Cleric,
  Sorcerer, …) mapped onto 6 archetypes: **Tank, Warrior, Rogue, Archer,
  Healer, Nuker**.
- Choosing one is permanent, grants a permanent core-stat bonus, full-heals
  you, and unlocks a **signature skill** that joins your skill bar:
  Fortify (Tank), Mighty Blow (Warrior), Twin Slash (Rogue), Power Shot
  (Archer), Greater Heal (Healer), Flame Burst (Nuker).
- Archetype range rules from the doc are in: **Archer** second classes get
  +500 basic-attack range with a bow (capped 1100); **Healer/Nuker** get
  +500 spell range (capped 900).

### Items & equipment
- Grades **F/E/B/A/S** gate by level (0/20/40/60/80); rarities Common,
  Uncommon, Rare. Weapons add attack (bows/staves also set ranged range);
  armor comes in Heavy/Light/Robe with def/HP/eva/MP profiles.
- Equip/unequip from the inventory; one item per slot (weapon, armor).
  Equipping recomputes all derived stats server-side and re-validates the
  level requirement. You start with a Rusty Sword and Leather Vest.

### Drops
- Killing a mob has a 30% drop chance (70/25/5 common/uncommon/rare);
  level-13+ mobs can drop E-grade gear. Loot lands in your bag (30 slots)
  and pops a system message.

### Trade window
- Target a player within range → *Request Trade*. They get an accept/decline
  prompt. The window matches the design doc: **their offer on top, your
  offer in the middle, your bag on the bottom**, Ready/Cancel in the footer.
- Click bag items to add (max 10), click your offered items to pull them
  back. **Any change resets both Ready flags** — no bait-and-switch.
- The trade commits only when both press Ready; the server re-validates both
  inventories inside a single step (items still owned, bags have room) before
  swapping. Equipped items can't be traded; disconnect/death cancels safely.

## What's where

```
Game.Shared
  GameConstants, StatCalculator     core + combat + progression formulas
  Enums, Dtos, CastInfo             wire contracts
  Skills (SkillCatalog)             base + archetype signature skills, ranges
  Items (ItemCatalog)               item defs, grade gates, drop rolls
  Classes (ClassCatalog)            18 classes -> 6 archetypes, stat bonuses
Game.Server
  Hubs/GameHub                      thin: every call -> a queued command
  Simulation/
    World, TradeSession             state + command queue (single writer)
    Entity                          stats/buffs/inventory/equip/2nd class
    CellGrid                        interest management
    GameLoopService                 10 t/s: cmds -> AI/skills/combat -> snaps
Game.Client.Wpf
  NetworkChannel                    transport seam (Unity-reusable)
  MainWindow / .Phase4              world view + inventory/class/trade UI
```

The concurrency model is unchanged and still the whole point: hub threads
only enqueue commands, the loop is the single writer, **zero locks** — even
trade (a two-party, item-moving transaction) is just commands resolved on
the loop thread, so it can't race.

## Roadmap

- **Phase 5** — EF Core persistence (accounts, character save/load), real
  login/auth, admin role (ban/kick/jail, god mode), boss monsters with the
  ±10 level paralysis rule, shields/orbs proc items, environment terrain
- **Phase 6** — the Unity 2D client (reuses Game.Shared + NetworkChannel),
  then multiple seamless zones toward the 75k×75k world
