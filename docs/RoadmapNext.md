# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated 2026-07-24 (0.28.61). Full history: [CHANGELOG.md](CHANGELOG.md).

## Just built (2026-07-23 → 24, 0.28.42 → 0.28.61)
Playtest-10 fixes · flat HoT potions (+ instant) · auto-potions Potions tab · potions on the bar ·
equipment presets A/B/C · sit/stand · **leaderboards** · **3h break reminder** · **equipment folded into
the bag** · **all target commands as buttons**.
**World pass:** whole map on filled **fields** (one wrapping field per town, town as an island) ·
**boss field** (Sunken Vale) + **dungeon field** (Hollow Crypt) · **no-rogue-spawner rule** (guarded at
startup) · **dungeons + jail in the negative quadrant** (teleport-reached) · **walls** (can't walk between
overworld / dungeon / jail; 500u ward teleports clip-outs back inside).

## Independent — buildable any time, no blockers
- **Shots as RUNES** — 🎯 DESIGN CONFIRMED (2026-07-24), NEXT UP. Soul/spiritshots become timed inventory
  runes (wall-clock expiry, delete-protected, boxes set the clock on open); removes the TrainingPassive.
  Full spec in memory `shots-rune-and-warehouse`. Then a **per-char warehouse** (space + rune-disable);
  account warehouse deferred.
- **MP potions** — a parallel set of flat mana-over-time tiers, same shape as the HP potions. Small.
- **Wearable titles** — show the leaderboard title over the head / by the name (extends leaderboards;
  the reward layer the owner hinted at).
- **Combat depth**: perfect/excellent block, magic-resist passives, position bonuses. Each independent.
- **More fields/dungeons** — the map is field-based now; adding a zone = author a field (spawners must
  live in one — the startup guard enforces it) and, for a dungeon, drop it in the negative quadrant.

## Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs (Lightbringer / Warchanter …).
  Everything class-progression waits here. Biggest single content unlock once the CSVs arrive.
- **Instances** — design done ([design/Instances.md]); owner HOLDING. Open decision: daily attempts
  GLOBAL vs PER-INSTANCE. The negative quadrant + walls are the groundwork (sealed rooms, teleport-only);
  instancing = per-party COPIES of a dungeon + the attempt/reset rules on top.
- **World expansion to 1kk+** (owner vision) — the size is one constant and the grid is sparse, so it
  scales freely; grow it as content + teleport hubs fill in, not as an empty void up front.
- **Castles + vault** — needs siege design; consumes the reserved `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — rare potions / premium consumables bought with boss/event points.
  Depends on bosses + instances producing the points first.
- **Crafting economy** — already BUILT; remaining polish (Epic recipes, mat sinks) is incremental.

## Deferred (explicit owner hold)
- **Bot-prevention CAPTCHA** — petrification after a random 200–500 back-to-back manual kills (over
  ~1h of nonstop farming + town refills), mob-immune while frozen, tap-to-answer challenge. See
  `reminder-bot-prevention-idea`. A CAPTCHA only stops low-effort scripts; **behavioural detection** is
  the real net against an AI that plays. Do the petrification + tap-CAPTCHA first; analytics later.
- **3rd-class CSVs, Instances** — owner said "Hold" (2026-07-22).

## My view of what's next
1. **Playtest 0.28.61** (tomorrow) — big untested stack; verify the world pass + walls on the phone first.
2. **Rune shots** — design locked; build it (item + expiry + boxes + reconciliation + remove passive +
   BalanceMatrix). Then the warehouse. See `shots-rune-and-warehouse`.
3. **MP potions** — cheap, completes the potion set. Independent, do anytime.
4. **Wearable titles** — small; gives the shipped leaderboards real teeth as a reward.
5. **3rd-class kits** — the moment the CSVs land, highest-value unlock.
6. **Instances** — after the daily-attempt decision; the walls/negative-quadrant work already set it up.
7. **Bot-prevention** — when the owner wants it; petrification + tap-CAPTCHA is a contained first slice.
