# L2-like MMORPG — Phase 3

Server-authoritative multiplayer prototype. Phases 1-2 gave us login, movement,
interest management, combat, exp and death. **Phase 3 adds skills, MP, cast
times, buffs/debuffs — plus the world structure feedback round: a safe-zone
town, level-banded hunting grounds, tighter aggro, and a full chat overhaul.**

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
| Move | Left-click the ground |
| Target & attack | Left-click a mob/player |
| Skills | Keys **1-5** or skill bar buttons |
| Local chat | plain text + Enter |
| World chat | `!your message` |
| Whisper | `/w CharName message` (or pick a name in the Whisper tab) |
| Respawn | Button on the death overlay |

## New in Phase 3

### World structure
- **Safe zone (town)** — the green circle in the center. No mobs spawn or
  enter it, aggroed mobs drop aggro the moment you step inside, and natural
  regen is **5x** while there (until /sit exists).
- **Level-banded hunting grounds** — mobs spawn in rings around town:
  1300-3500 → lvl 1-3, 3500-6000 → 4-7, 6000-8500 → 8-12, 8500+ → 13-18.
  A mob's level comes from its home position, and leashing (1500) keeps it
  there — no lvl-15 Bandit wandering into the starter ring.
- **Mob name colors by level difference**: gray (very weak, -6 and below),
  green (weak), white (normal), yellow (strong, +2..+5), red (very strong).
- **Aggro reduced to 400** so aggressive mobs (Spiders, Bandits) are a
  danger you walk into, not a death sentence.

### Skills (keys 1-5)
- **Fighter**: Power Strike (physical nuke, +10 accuracy but can still miss),
  War Cry (+20% attack for 30s).
- **Mage**: Magic Bolt (600 range), Heal (self), Weakness (-30% target
  defence for 15s).
- **Cast times** scale with WIT (`SkillCatalog.AdjustedCastTicks`) — a cast
  bar shows the wind-up; moving cancels it.
- **Spells don't miss — they fail**: 3% base + 2% per level the target is
  above the caster, exactly per the design doc. Physical skills roll the
  normal accuracy check with a +10 bonus.
- Out-of-range skill use runs you into range first (L2-style), and after an
  offensive skill your auto-attack continues.
- MP costs, per-skill cooldowns, and all formulas are server-enforced;
  the client's cooldown display is only cosmetic.

### Chat overhaul
- **System panel on top** (1st row), tabbed chat below (2nd row):
  **All / World / Local / Whisper**.
- World chat is now `!message`. Whispers are `/w Name message`; the Whisper
  tab keeps a dropdown of everyone you've whispered with — click a name to
  pre-fill `/w Name `.
- Every tab keeps the last 150 lines.

## What's where

```
Game.Shared        DTOs, enums, GameConstants, StatCalculator, SkillCatalog
Game.Server
  Hubs/GameHub     thin connection layer — only enqueues commands
  Simulation/
    World          live state + the command queue (single-writer model)
    Entity         stats, buffs, skill/cast state
    CellGrid       interest management (3000-unit cells, 3x3 lookup)
    GameLoopService  10 t/s: commands -> AI/skills/combat/regen -> snapshots
Game.Client.Wpf
  NetworkChannel   the transport seam (reusable in Unity as-is)
  MainWindow       rendering, skill bar, cast bar, tabbed chat, safe zone
```

The action priority per entity per tick is a tiny state machine:
**casting > queued skill (chasing into range) > auto-attack**. Hub threads
still only enqueue commands; the loop is the single writer; zero locks.

## Roadmap

- **Phase 4** — the class tree (2nd classes at lvl 20 with converted skills
  — it gets its own phase because it expands the SkillCatalog heavily),
  items, inventory, drops, equipment grades, the trade window
- **Phase 5** — EF Core persistence, accounts & auth, admin role
  (ban/kick/jail), multiple zones, environment objects, Unity client
