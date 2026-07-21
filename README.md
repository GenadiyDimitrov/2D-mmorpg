# 2D MMORPG

A server-authoritative multiplayer RPG built in C# / .NET 8 — a solo project written to learn how
the classic tab-target MMO actually works underneath: interest management, a single-writer
simulation loop, stat and damage maths, progression, and an authoritative server that never trusts
the client.

The world, classes, items and skills are original. The *systems* are modelled on the tab-target MMO
tradition (levels, grades, buffs, drop tables, class trees), because that's the genre being studied.

> **Status: work in progress, and a learning project.** It runs and it's playable, but it is not a
> product and makes no promises about stability, balance or backward compatibility. Save files are
> routinely thrown away between builds.

---

## What it does

- **Authoritative server simulation** at 10 ticks/second — movement, combat, AI, casting and loot
  are all resolved server-side. The client renders what it is told and asks for things politely.
- **Characters and progression** — races and a branching class tree, levels, experience, skill
  points, learned skills, and subclasses (one character, several classes, each with its own level).
- **Combat** — physical and magic damage channels with distinct resolution (evasion vs. spell
  failure), critical hits, shields and blocking, cast times that commit you, and interrupts resolved
  as a stat contest rather than a coin flip.
- **Items** — equipment with grades and rolled attributes, enchanting, named armour sets with set
  bonuses, weapon types that change how a class plays, and per-monster level-banded drop tables.
- **A world** — multiple towns and levelled hunting zones, safe zones, NPCs, shops, teleporters,
  quests, and monsters that range from trash to bosses with telegraphed mechanics.
- **Playing together** — parties with shared experience and configurable loot rules, trading, chat
  channels, friends, PvP with a flagging/karma system, and moderation tools.
- **Quality of life** — an idle/offline auto-hunt mode, a configurable skill bar, and a live tuning
  panel for rates and caps.

## How it works

Three projects, and one rule that shapes everything:

```
Game.Shared   →  DTOs, enums, formulas, catalogs. No server or client dependencies.
                 Both clients compile against it, so the wire contract cannot drift.

Game.Server   →  ASP.NET Core console app. SignalR hub + the simulation.
                 The hub is deliberately thin: it only enqueues commands.

Game.Client.* →  Renderers. WPF (desktop test harness) and Unity (mobile).
                 Both talk through the same NetworkChannel seam.
```

**The concurrency model is the point.** Hub methods never touch game state — they push command
records onto a queue. The game loop is the **single writer**: once per tick it drains that queue and
mutates the world. There are **no locks anywhere**. Even a two-party item trade is just commands
resolved on the loop thread, so it cannot race.

Networking is SignalR over WebSockets. Each client receives a per-tick **delta** of only what it can
currently see — spawns, changed fields, despawns — scoped by a spatial grid, so cost scales with
what's nearby rather than with the size of the world.

Persistence is EF Core + SQLite, and covers accounts and characters. The live world — monsters,
zones, casting state — is deliberately runtime-only and rebuilt on start.

## Tech

.NET 8 · ASP.NET Core · SignalR · EF Core + SQLite · WPF · Unity 6 (Android)

## Running it

Requires the **.NET 8 SDK** (and Windows for the WPF client).

```bash
dotnet run --project Game.Server        # http://localhost:5238, hub at /game
dotnet run --project Game.Client.Wpf    # one per client
```

In Visual Studio: open `Game.sln`, set **Game.Server** and **Game.Client.Wpf** as multiple startup
projects, then F5.

For the mobile client see [docs/guides/UnityClient.md](docs/guides/UnityClient.md).

### Controls (WPF)

| Action | Input |
|---|---|
| Move | Left-click ground |
| Target & attack | Left-click a monster |
| Target a player | Left-click a player |
| Skills | Keys **1–6**, or the skill bar |
| Inventory | **I** |
| Skills window | **K** |
| Chat: local / world / whisper | plain text / `!text` / `/w Name text` |

## Repository layout

```
Game.Shared/        formulas, catalogs, DTOs — the shared contract
Game.Server/        Hubs/ (thin) · Simulation/ (the tick loop) · Persistence/
Game.Client.Wpf/    desktop client — the test harness
Game.Client.Unity/  mobile client (Unity 6, Android)
tools/              SmokeTest (headless protocol test) · BalanceMatrix (balance harness)
docs/               design specs, guides, checklists, changelog
```

## Documentation

Start at **[docs/README.md](docs/README.md)**, which indexes everything. The most useful entry points:

- [Changelog](docs/CHANGELOG.md) — what has been built, newest first
- [Roadmap](docs/Roadmap.md) — what's planned
- [Design specs](docs/design) — how the combat, damage, stat and crafting systems are meant to work
- [Unity client guide](docs/guides/UnityClient.md) — building and running on a phone

## Testing

Two tools exist because some classes of bug are invisible to playing the game:

- **`tools/SmokeTest`** — a headless client that speaks the real protocol: logs in, creates a
  character, edits the skill bar, adds and swaps a subclass, reconnects, and asserts everything
  survived. Persistence bugs can leave the screen looking perfectly correct while the saved state is
  already destroyed; this catches those.
- **`tools/BalanceMatrix`** — builds real entities with real gear and runs the actual combat
  formulas, printing damage, time-to-kill and levelling pace. Balance is measured here, not derived
  by hand.

---

*Names, world and lore are original. Any resemblance to existing games is in the mechanics, which is
the part worth learning from.*
