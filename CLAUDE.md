# CLAUDE.md — L2Clone (Lineage 2-inspired MMORPG, C# .NET 8)

This file orients Claude Code on this project. Read it before making changes.

## What this is
A server-authoritative, L2-inspired MMORPG built solo for learning. The WPF
client is a **test harness**, not the final client (a 2D cross-platform client
reusing `Game.Shared` + `NetworkChannel` is the eventual goal). Names are
deliberately generic to avoid IP issues; stat *formulas* are not copyrightable
and are adapted from L2 references.

## Build & run
- Open `Game.sln` in Visual Studio. Configure **Multiple Startup Projects**:
  set **Game.Server** and **Game.Client.Wpf** both to *Start*, then F5.
- CLI: `dotnet run --project Game.Server` and `dotnet run --project Game.Client.Wpf`.
- Server listens at `http://localhost:5238`, SignalR hub at `/game`.
- **The sandbox that authored most of this could NOT compile** — so there may be
  occasional compile slips. When you (Claude Code) build, fix real `dotnet build`
  errors directly; that's the main advantage of working here vs. chat.

## Solution layout (3 projects)
- **Game.Shared** (`net8.0` lib) — DTOs, enums, formulas, catalogs. No server/
  client deps. Key files: `Dtos.cs`, `Enums.cs`, `GameConstants.cs`,
  `StatCalculator.cs` (damage/speed/crit/interrupt math), `Skills/` (skill defs
  split per class/discipline — `Skills.cs` = `SkillDef`/`PassiveEffect`/`SkillMath`
  + the one `BuildCatalog` assembly; `Skills.Fighter.cs`, `Skills.Lightbringer.cs`,
  etc. each contribute their `SkillDef`s; `SkillCatalog` is a partial class),
  `Items.cs` (`ItemCatalog`), `Classes.cs` (`ClassCatalog`),
  `MobCatalog.cs` (mob templates + drop tables), `WorldMap.cs` (zones, NPCs,
  safe zones), `StatCaps.cs`, `RateConfig.cs`, `GameClock.cs`, `Quests.cs`,
  `RaceAndClasses/*` (per-class skill tables), `Quests/*` (quest chains).
- **Game.Server** (ASP.NET Core console, NOT IIS) — authoritative simulation.
  `Hubs/GameHub.cs` (thin: only enqueues commands), `Simulation/` (`World.cs`
  = command records + entity dict + grid; `Entity.cs` = entity + `RecomputeDerived`;
  `GameLoopService.cs` = the single-writer tick loop, ~2k lines, where almost all
  game logic lives; `CellGrid.cs`, `ZoneRuntime.cs`), `Persistence/` (EF Core
  SQLite: `GameDbContext.cs`, `Records.cs`, `PersistenceService.cs`).
- **Game.Client.Wpf** (`net8.0-windows`) — `NetworkChannel.cs` (transport seam,
  reusable by a future Unity client), `MainWindow.xaml(.cs)`, `MainWindow.Phase4.cs`
  (partial of the same class — they share private fields).

## Concurrency model (important)
- Hub methods ONLY enqueue command records onto `World.Commands` (a
  `ConcurrentQueue`). They never touch entity state.
- `GameLoopService` is the **single writer**: each tick it drains the queue and
  mutates the world. **No locks anywhere.** 10 ticks/sec via `PeriodicTimer`.
- To add a feature: add a command record in `World.cs`, a hub method that
  enqueues it, a `case` in the command switch, and a handler in `GameLoopService`.

## Persistence
- EF Core + SQLite (`game.db`). Swap to Postgres = `UseSqlite`→`UseNpgsql` (1 line).
- Uses `EnsureCreated()`, which **only creates the DB if the file is absent** — it
  does NOT add new columns to an existing DB. **Workflow on schema change: delete
  `Game.Server/game.db` (and `-shm`/`-wal`) and let it recreate.** The connection string is
  `Data Source=game.db`, resolved against the CONTENT ROOT (`Game.Server/`) — *not* `bin/Debug/net8.0/`,
  where a stale copy may also sit and mislead you into thinking you reset the schema when you didn't.
  Migrations are deferred until there's real data to preserve.
- Only **characters/accounts** persist (load on login, save on logout + 60s
  autosave + some events). Mobs, zones, rates, casting state are runtime-only.
- Quests persist via `CompletedQuestsCsv` + `ActiveQuestsJson` columns.

## Core conventions (follow these)
- **Skill ids are STRINGS** everywhere (`"magic_bolt"`), append-only, collision-
  guarded at startup. Item DefIds are strings too. Mob template ids are strings.
- `Effective*` stat getters on `Entity` return **float**; cast to `(int)` when
  feeding int-typed `StatCalculator` methods.
- Editing existing files in the chat sandbox used `str_replace` / `cat > EOF`
  because `create_file` fails on existing paths — irrelevant for Claude Code,
  which can edit normally.
- **Delivery in chat sessions** was a ready-to-run zip to `/mnt/user-data/outputs/`.
  In Claude Code you just edit in place.

## Key design decisions (don't silently reverse these)
- **Damage is a RATIO, not subtraction**: `K·(atk·lvlMod + power)/def`. lvlMod =
  `(level+89)/100`. Defence is a divisor (diminishing returns, never zero-blocks).
  `PhysicalK`/`MagicK` in `StatCalculator`.
- **One power stat (ATK)** feeds BOTH `pAtk` and `mAtk`; the **weapon** decides the
  split via `MAtkBonus` (staff high, sword low — hybrids possible). WIT is NOT
  power: it drives **cast speed + magic crit rate**, not magic damage.
- **Two damage channels**: physical can be evaded, crits up to ×10 (DEX, cap 50%);
  magic can "fail" (reduced damage) + crits up to ×3 (WIT, cap 20%). Magic divides
  by physical defence for now (magic-resist passives/jewels later).
- **Speed**: 250 is the buffed move CAP (per-entity, raisable). Base run speeds per
  race+class in `SpeedTable` sit below it. Cast/attack speed use the L2 "333 = 1.0×"
  model (`StatCalculator.CastSpeedStat`/`AttackSpeedStat`, per-class coefficients,
  weapon base speeds). All ceilings in `StatCaps`.
- **Movement states**: Run / Walk (+20% regen) / Sit (+80% regen, can't move,
  stand-up delay when hit). Regen is a multiplier stack so passives can add later.
- **Casting roots you** (commit on start; range checked at start only; lands even
  if target moves). ESC cancels + starts cooldown. **Interrupt is a stat contest**:
  caster `InterruptResist` (WIT + skill `InterruptDefense`) vs attacker
  `InterruptPower` (skill param). Enemy interrupt = no cooldown, retry; you keep the
  initial-MP loss. Two-stage MP (`InitialMpCost`/`FinishMp`) for future toggles.
- **Block (shields)**: shield carries BlockChance, BlockReduction%, ShieldDefense,
  ShieldCritDefense, ShieldEvasionPenalty. Resolution: shield lowers attacker crit
  CHANCE → if it still crits, crit ignores shield; else roll block → flat % damage
  reduction. DEX does NOT affect block (flat + passives only). **Shield Mastery**
  buff scales shield values (only with a shield equipped). Skills can carry
  `BlockAccuracy` to bypass blocks. Magic is NOT blocked (mitigated by defence only)
  to avoid stacking fail+interrupt+block and making mages useless.
- **Mobs are templates** (`MobCatalog`: id, name, speeds, behavior, drop table) with
  NO fixed level — the **ZONE assigns level** (stats derive from it). Same creature
  at any level = same drops; different loot = new mob id; tougher elsewhere = higher
  zone. **Drop tables** are per-mob `DropEntry(itemId, chance:float, minQty, maxQty,
  MinLevel, MaxLevel)`; the level band lets one mob drop different loot at different
  levels.
- **Rates are GLOBAL** (`RateConfig`: ExpRate, SpRate, DropChanceRate,
  DropAmountRate; gold rate reserved), not per-item.
- **Buffs**: flat skill id = ability identity; `BuffKey` = buff identity for
  stacking. Stacking rules in `ApplyBuff`: (1) same `BuffKey` compares `Rank`
  (incoming ≥ existing → replace; weaker → ignore); (2) explicit `Replaces[]`
  removes listed buffs. Per-class flavor = `DisplayName` override on `ClassSkill`.
- **Spell range is PER-SPELL** (the skill's own `Range`), NOT class-tier-based:
  `SkillMath.EffectiveRange` returns `def.Range` for spells (heals shorter than attack
  spells; healer attack ~750, nuker ~900, base nuke 600 — authored per skill). The ONE
  exception kept is **bow skills**, which still scale with the archer's bow tier
  (350/600/900, `SkillMath.RangeTier`), matching the bow basic-attack range growth.
  Magic weapons have NO weapon range (melee basic, tiny basic damage — power is in
  spells). Only bows have basic range (400 base).
- **Daggers are treated as DUALs** (`WeaponType.Dual`) — fast, lower per-hit.
- **Class change** grants flat secondary bonuses (`ClassFlatBonus` on
  `SecondClassDef`) for class identity (e.g. tank flat +Def/+HP) — primary stats are
  reserved for the future dye/tattoo/set layer. Item-gated via
  `ClassChangeRequirements` (consumes quest items at a class-change NPC).

## Validation without a compiler (chat-sandbox technique; Claude Code can just build)
The chat sandbox validated by stripping string-literals + `//` comments, then
brace/paren counting, plus XAML parse and StatsUpdate field-count alignment. Known
false-positive sources when brace-counting raw text: `http://` in `Program.cs`,
`[0,24)` in `GameClock`. Claude Code should prefer `dotnet build`.

## Naming (IP safety — important)
Names are deliberately generic to avoid IP issues. **Never use names trademarked by
other games** — no Lineage/L2 town, NPC, item, skill, or currency names (e.g. the old
"Giran/Aden/Gludio" towns and "adena" currency were slips, since renamed). Invent original
generic fantasy names (current towns: Brackenford, Stonewatch, Emberfall, Greymarsh,
Ironreach, Duskvale, Frostmere). Stat *formulas* are not copyrightable; *names* are.

## Roadmap (not yet built)
Built since this list was first written: gold wallet, NPC shop vendors, teleport-for-fee,
buff potions, the level 1-80 world. Still to do: 3rd/4th class tower (cleric→bishop quest
chains); party/grouping (replace "allies in radius" stand-in); boss mechanics (±10-level
rule, boss skills, enrage); instances/dungeons; castles + vault (consumes the
`VendorBuyTaxRate` hook); perfect/excellent block; magic-resist passives; soulshots;
position bonuses; PvP/PvE multipliers (hooks default 1.0); the real 2D client.

## Verify server behaviour with the SMOKE TEST, not by playing
`tools/SmokeTest` is a headless SignalR client speaking the real protocol (no window). With the
server running:
```
dotnet run --project tools/SmokeTest
```
It logs in, creates a fresh character, arranges a skill bar, adds a subclass, swaps, swaps back,
**relogs**, and asserts the bars/levels/XP survived. **Run it after touching persistence, the skill
bar, subclasses or the login sequence.**

It exists because this area's bugs are invisible to a human playtest: the client renders the state it
was *sent*, so a bar can be correct on screen and already destroyed on the server, only surfacing as
corruption on the next login. It has already caught two such bugs. It creates a NEW character each run
— a test that is not idempotent lies to you.

## Balance work: measure, don't derive
`tools/BalanceMatrix` (a console app, deliberately NOT in `Game.sln`, so it never affects the
owner's build) constructs REAL `Entity` objects with REAL best-for-tier gear and runs the actual
combat formulas:
```
dotnet run --project tools/BalanceMatrix
```
**Use it before and after any combat/stat change** — it prints the mob curve, mage/fighter stats,
damage, time-to-kill and levelling pace. Hand-derived balance numbers have been wrong here before
(the whole 2026-07-14 magic re-scale started from a hand-derived diagnosis that blamed the wrong
system). Extend the tool rather than hand-computing a new table. `docs/BalanceMatrix.md` is the
older hand-written audit — its formulas and reasoning are good, its NUMBERS are stale.

## The skill bar belongs to the SERVER
The server owns the bar and does the auto-placement of newly-learned skills
(`GameLoopService.SyncSkillBar`), and `SendLearned` always pushes the bar *with* the skills.
**The client must never write a bar it did not author** — it may only `SaveSkillBar()` when the
PLAYER edits it (drag / assign / remove). This is not fussiness: while auto-placement lived in the
client, every server push of `Learned` that arrived while the client held a *different* bar (a fresh
login, a subclass swap) made it re-park skills against the wrong bar and save the result — destroying
the real layout on the server while the client went on to receive the correct bar and *look* perfectly
fine. It bit twice before it was understood.

## Working style (owner's rules — these matter)
- **Never launch the server/client unprompted.** The owner tests manually and will say when to run.
  Build (`dotnet build`) freely; don't `dotnet run` the game to "check" something.
- **"Commit" means commit AND push.** Only "just commit" / "only push" means one of them.
- Discuss design before large mechanic changes; deliver focused, validated increments.
- Cyrillic text from the owner is Bulgarian.

## Style
Keep changes consistent with the above. Prefer C# .NET idioms. For web/UI work the
owner prefers ASP.NET + HTML/CSS, JavaScript only as a last resort.
