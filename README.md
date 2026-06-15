# L2-like MMORPG — Phase 5 (persistence, accounts, admin)

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
