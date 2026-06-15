# L2-like MMORPG — Phase 4

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
