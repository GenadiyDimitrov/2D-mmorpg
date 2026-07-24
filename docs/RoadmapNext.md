# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated 2026-07-24 (0.28.66, after playtest-11).
Full history: [CHANGELOG.md](CHANGELOG.md).

## 🎉 Playtest-11 (2026-07-24, 0.28.66) — the whole Unity checklist PASSED
`§§1-15` verified end to end, closing the **A–F parity programme**, the **playtest-10 batch**, the
**world pass** and the **rune shots** in one pass. Only exceptions: **Skills→Learn is dead**, the
**soft keyboard covers the command bar**, and the 3h banner is untestable in a sitting. 0.28.65
(open boxes from the bag) and 0.28.66 (item-details layout) shipped after the test — unverified.

**The client is now feature-complete enough that the work has shifted from "reach parity" to
"make it a game".** Full findings: memory `playtest-11-queue`; retest list: `§17` of the checklist.

## 🔴 NEXT UP — the playtest-11 queue (nothing below is built)

**Tier 1 — bugs that break a feature outright**
- **Skills→Learn does nothing** (the whole progression loop is unreachable from the phone).
- **`isAdmin` is per-CHARACTER, must be per-ACCOUNT** — a non-admin char in an admin account has admin.
- **Soft keyboard covers the command bar** instead of lifting it.
- **`/tp` to a jailed player** lands in the dungeon, not the jail (negative-quadrant clamp).
- **Dungeon mobs don't aggro or retaliate** after a debug-menu displacement; they also **clamp together**.
- **`[lead]` doesn't move the party `*` flag** or clear its button.
- **"X entered the world" leaks to non-friends** — must be **mutual friends only** (rest debug-only).

**Tier 2 — cheap UI/UX fixes, mostly client-only**
Duplicate blue town-entry line · field banner needs **hit-test false** (it blocks ground taps) ·
player target frame should be `[info]`-only · debug-menu chat spam · spiritshot buff reads `719h59`
not `29d` · bag `Equip` button first + column expands LEFT · stand-up delay (instant if seated >3s) ·
HoT floating text for potions · target-window HP as digits + an MP bar for players · raw attack/cast
speed beside the multiplier (`1234/1500 (x3)`) · buff double-tap cancels / single tap opens details ·
party buff/debuff **squares** + loot proposal **drop-down** · a **world border** (orange dashed).

**Tier 3 — needs server work**
- 🎯 **Partial-stack trading** — design call **ANSWERED: YES**. `TradeOffer` must carry per-item counts
  and split the stack on completion. Was the last blocker on the trade numpad.
- **Target a party member with no range restriction** (so kick/change-leader work from buttons).
- **Admins excluded from the rankings** (an admin at level 999 breaks every board).
- **Shop rework** — items need details + buy-time info; **prices far too cheap** (equipment ≥200g;
  runes **150k/1h and 280k/2h**, a ~7% bulk discount vs two 1h runes).
- **Client-side collision** — stop movement at the surface and reject out-of-world taps, so the server
  clamp stops being the everyday mechanism (map edge `0`/`48000`, keeps). See the decision above.
- **Starter-gear redesign + the levelling curve** — newbie boxes become a level-10 quest; levels 1-10
  get the weakest gear in the game. Pair with the curve decision above.

**Tier 4 — new systems**
- **Chat**: colours, tabs, tags (`[!]` world, `[W]` whisper) — WPF already has the first two. Plus the
  **peek/fade when the log is hidden** request.
- **Every non-admin command as an ACTION** — friend, party, sit/walk/run, attack/assist/next. They live
  in the Skills window's **Actions tab** and must be **placeable on the skill bar**, like a skill — not
  target-frame buttons. The 0.28.55 player-target button grid comes out; `[info]` stays for mobs only.
- **Block system** — `/block` `/unblock` `/blocklist`; permanent; silences every chat form.
- **Charisma system** — `/like` (+1, 20/day, never negative); killing costs `karma × 0.01`; **every 20
  charisma = +1% exp/sp drop, capped 1000 = +50%**; chatban −20/h, jail −100/h, kick −250/h, ban zeroes
  both; **two values** (a 0-1000 bonus pool, and a lifetime total for ranking).
- **Buy-back menu** — last 10 deleted/sold items; `[r]` restores a deleted or sold-for-0 item.

## ✅ All four follow-ups answered (2026-07-24)
1. **Mob XP — neither the L2 formula nor a per-mob value.** `ExpToNext = 25L²` (quadratic) vs
   `MobExpReward = 40 + 35·L` (linear); the only per-mob variation is a toughness multiplier derived
   from the mob's HP. **See the finding below — the curve needs a decision.**
2. **Starter gear — CONFIRMED WORK**, not a question (it landed under "not sure" by mistake). Build it.
3. **Rune prices — 150k / 1h and 280k / 2h**, a ~7% bulk discount against two 1h runes.
4. **Walls — a CLIENT/SERVER split, not a rewrite.** **Client = collision**: stop at the surface, never
   emit out-of-world coordinates, reject a tap outside your world before it becomes a move order (this
   half doesn't exist yet). **Server = prevention**: the existing rubber-band
   (`ConfineToDomain`, `GameLoopService.cs:712`) **stays** as the anti-cheat backstop — today's snap-back
   is the symptom of the missing client half, not a bug in the clamp. Crossing worlds stays
   **teleport-only**; the dashed border is the fallback marker where no wall art exists.
   Full design: memory `worlds-and-collision-design`.

## 🔴 Finding — the levelling curve is much shorter than it looks
Quadratic curve ÷ linear reward means **~2 200 kills for the whole 1→80** at `ExpRate 1`, and only
**~220** at the current ×10. At level 10 on ×10 you need **0.64 kills per level** — you level more than
once per kill, which is exactly what the playtest felt like. Endgame sits at ~56 kills/level, trivially
fast for an MMO (L2's curve is far steeper past 76).

| level | kills/level @×1 | cumulative |
|---|---|---|
| 10 | 6.4 | 33 |
| 30 | 20.6 | 310 |
| 50 | 34.9 | 873 |
| 79 | 55.6 | **2 196** |

**This is not an ×10 artifact.** Decide the curve alongside the starter-gear redesign — both target the
early game, and re-tuning one without the other will just move the problem. Measure with
`tools/BalanceMatrix`, never by hand (hand-derived balance has been wrong here before).

## Independent — buildable any time, no blockers
- **MP potions** — a parallel set of flat mana-over-time tiers, same shape as the HP potions. Small.
- **Wearable titles** — show the leaderboard title over the head / by the name (extends leaderboards).
  Now overlaps with **charisma ranking**, which adds a second board; design them together.
- **Combat depth**: perfect/excellent block, magic-resist passives, position bonuses. Each independent.
- **More fields/dungeons** — adding a zone = author a field (the startup guard enforces that every
  spawner lives in one) and, for a dungeon, drop it in the negative quadrant.
- **Per-char warehouse** — the other half of the rune-shots spec (space + rune-disable); the rune half
  shipped in 0.28.62–64. Account warehouse deferred. See memory `shots-rune-and-warehouse`.

## Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs (Lightbringer / Warchanter …).
  Everything class-progression waits here. Biggest single content unlock once the CSVs arrive.
- **Instances** — design done ([design/Instances.md]); owner HOLDING. Open decision: daily attempts
  GLOBAL vs PER-INSTANCE. The negative quadrant + walls are the groundwork.
- **World expansion to 1kk+** (owner vision) — the size is one constant and the grid is sparse, so it
  scales freely; grow it as content + teleport hubs fill in, not as an empty void up front.
  The **world border** above is the first visible step.
- **Castles + vault** — needs siege design; consumes the reserved `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — depends on bosses + instances producing the points first.
- **Crafting economy** — already BUILT; remaining polish (Epic recipes, mat sinks) is incremental.

## Deferred (explicit owner hold)
- **Bot-prevention CAPTCHA** — petrification after a random 200–500 back-to-back manual kills,
  mob-immune while frozen, tap-to-answer challenge. See `reminder-bot-prevention-idea`. A CAPTCHA only
  stops low-effort scripts; **behavioural detection** is the real net. Note **charisma + block** now give
  a social signal that feeds the same problem — worth revisiting together.
- **3rd-class CSVs, Instances** — owner said "Hold" (2026-07-22).

## My view of what's next
1. **Tier-1 bugs** — Learn, account-level `isAdmin`, the keyboard, the jail/dungeon teleport and the
   dungeon aggro. These are broken features, not polish; do them first and in one batch.
2. **Early-game pass** — the **starter-gear redesign** (confirmed) plus a **decision on the levelling
   curve**. Both target the same problem and neither should ship without the other. Verify with
   `tools/BalanceMatrix` before and after.
3. **Tier-2 UI batch** — a dozen small client-only fixes; cheap, and they're most of what the phone
   *feels* like. Batch and build once ([[feedback-build-cadence]]).
4. **Partial-stack trading** — now unblocked, and it closes the long-running trade-numpad thread.
5. **Shop rework** (details + prices) — pairs naturally with the starter-gear decision.
6. **Chat colours/tabs/tags**, then the **block** system — block is small and makes the social layer safe.
7. **Charisma** — the biggest new system in the list; do it after block, and design its ranking board
   alongside **wearable titles**.
8. **Buy-back menu** — self-contained, do whenever.
9. **3rd-class kits** — the moment the CSVs land, still the highest-value unlock.
