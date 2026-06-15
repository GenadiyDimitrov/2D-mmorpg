# L2-like MMORPG — Phase 1

Server-authoritative multiplayer prototype: login, character creation
(race/class), click-to-move in a shared world, wandering mobs, local/world
chat, and grid-based interest management.

## Requirements

- .NET 8 SDK
- Windows (the test client is WPF; the server runs anywhere)
- Internet on the **first build only** (NuGet restores the SignalR client package)

## Run it (click and play)

**Visual Studio:**
1. Open `Game.sln`.
2. Right-click the solution → *Configure Startup Projects…* →
   *Multiple startup projects* → set **Game.Server** and **Game.Client.Wpf**
   to *Start* (server first in the list).
3. F5. Enter a name, pick race/class, *Connect & Play*.

**Command line:**
```
dotnet run --project Game.Server
dotnet run --project Game.Client.Wpf     (in a second terminal)
```

Launch **multiple clients** to see other players — each needs a unique
character name. Server listens on `http://localhost:5238`.

## Controls

| Action | Input |
|---|---|
| Move | Left-click anywhere in the world |
| Local chat (view range) | type in chat box, Enter |
| World chat | `/all your message` |

## What's where

```
Game.Shared        DTOs, enums, GameConstants, StatCalculator (formulas)
Game.Server
  Hubs/GameHub     thin connection layer — only enqueues commands
  Simulation/
    World          live state + the command queue (single-writer model)
    Entity         server-side player/mob state
    CellGrid       interest management (3000-unit cells, 3x3 lookup)
    GameLoopService  10 t/s tick: commands -> simulate -> snapshots
Game.Client.Wpf
  NetworkChannel   the transport seam (reusable in Unity as-is)
  MainWindow       rendering w/ interpolation, click-to-move, chat
```

### Architecture rules already in force

- **Server is authoritative.** The client sends *intents* (move target,
  chat); the server validates, simulates, and tells everyone what is true.
- **Single-writer simulation.** Hub threads never touch game state; they
  enqueue commands the loop drains each tick. No locks anywhere.
- **Interest management from day one.** You only receive entities within
  3000 units; cell grid makes the lookup O(neighbors), not O(world).
- **Transport behind a seam.** `NetworkChannel` is the only file that knows
  SignalR exists.

## Roadmap

- **Phase 2** — targeting, basic attacks (acc/eva via `StatCalculator.MissChance`),
  mob aggro, death/respawn, exp & levels
- **Phase 3** — skills, buffs/debuffs, the class tree (2nd class at lvl 20)
- **Phase 4** — items, inventory, drops, the trade window, EF Core persistence
- **Phase 5** — accounts & auth, admin role (ban/kick/jail), zones, Unity client

## Unity later

The Unity client reuses `Game.Shared` (retarget it to `netstandard2.1`) and
`NetworkChannel` unchanged; only the rendering/input layer is rewritten.
Create the project in Unity Hub (2D URP template), add the
`Microsoft.AspNetCore.SignalR.Client` package (via NuGetForUnity), and port
`MainWindow`'s snapshot/interpolation logic to a MonoBehaviour.
