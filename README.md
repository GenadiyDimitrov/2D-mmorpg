# L2-like MMORPG — Phase 2

Server-authoritative multiplayer prototype. Phase 1 gave us login, character
creation, click-to-move, chat, and interest management. **Phase 2 adds
combat**: targeting, auto-attacks with accuracy/evasion, crits, mob aggro
and leashing, death/respawn, exp and leveling.

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

Launch **multiple clients** for multiplayer — each needs a unique name.
Server listens on `http://localhost:5238`.

## Controls

| Action | Input |
|---|---|
| Move | Left-click the ground |
| Target & attack | Left-click a mob (or another player) |
| Stop attacking | Left-click the ground |
| Respawn after death | Button on the death overlay |
| Local chat (view range) | type in chat box, Enter |
| World chat | `/all your message` |

## How combat works (per the design doc)

- **Engagement is L2-style**: click a target and your character runs into
  melee range and auto-attacks every 1.5s. The *intent* always reaches the
  target — lag never eats your attack; the stats decide the outcome.
- **Miss chance** = 2% base, shifted 1% per point of evasion-vs-accuracy
  difference (`StatCalculator.MissChance`). Both derive from DEX + level,
  so higher-level characters almost never miss lower-level ones.
- **Damage** = `max(1, AttackPower*2 − Defence)`, ×2 on crit. Crit chance
  comes from DEX (25 DEX = 10%).
- **Mobs retaliate** when hit. *Spiders and Bandits are aggressive* — they
  attack players on sight within 1000 units. Mobs **leash**: chased 4000+
  units from home they reset, walk back, and heal to full.
- **Death**: mobs despawn and respawn at home after 10s, granting exp to
  the killer. Players leave a visible corpse and respawn at town via the
  overlay button. Level-ups recompute derived stats and heal to full.
- **Out-of-combat regen** ticks once per second.

Try it: a level-1 character beats Wolves comfortably, but a level-5 Bandit
hurts — exactly the "higher-level mobs beat lower-level characters" rule
from the design.

## What's where

```
Game.Shared        DTOs, enums, GameConstants, StatCalculator (ALL formulas)
Game.Server
  Hubs/GameHub     thin connection layer — only enqueues commands
  Simulation/
    World          live state + the command queue (single-writer model)
    Entity         player/mob state incl. combat + derived stats
    CellGrid       interest management (3000-unit cells, 3x3 lookup)
    GameLoopService  10 t/s tick: commands -> AI/combat/regen -> snapshots
Game.Client.Wpf
  NetworkChannel   the transport seam (reusable in Unity as-is)
  MainWindow       rendering, targeting, HP bars, floating damage, death UI
```

### Architecture rules in force

- **Server is authoritative.** Clients send intents (move target, attack
  target); the server validates, simulates, and tells everyone what is true.
- **Single-writer simulation.** Hub threads only enqueue commands; the loop
  drains them each tick. No locks anywhere.
- **Interest management.** You only receive entities within 3000 units.
- **Formulas live in Game.Shared** so a future client can predict damage
  and tooltips without ever being trusted.

## Roadmap

- **Phase 3** — skills, buffs/debuffs, MP costs, cast times (WIT), the
  class tree (2nd class at lvl 20)
- **Phase 4** — items, inventory, drops, equipment grades, the trade
  window, EF Core persistence
- **Phase 5** — accounts & auth, admin role (ban/kick/jail), multiple
  zones, environment objects, Unity client

## Unity later

The Unity client reuses `Game.Shared` (retarget to `netstandard2.1`) and
`NetworkChannel` unchanged; only rendering/input is rewritten. Create the
project in Unity Hub (2D URP), add `Microsoft.AspNetCore.SignalR.Client`
via NuGetForUnity, and port the snapshot/interpolation logic to a
MonoBehaviour.
