# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated 2026-07-24 (0.28.66, after playtest-11 and
the exp rework). Full history: [CHANGELOG.md](CHANGELOG.md).

## Where things stand
**Playtest-11 passed the whole Unity checklist** (`§§1-15`), closing the A–F parity programme, the
playtest-10 batch, the world pass and the rune shots in one pass. The client is feature-complete
enough that the work has shifted from *"reach parity"* to *"make it a game"*.

**Since then, UNCOMMITTED and untested on device:** three tier-1 bug fixes and the **whole exp/party/
drop rework** (see below). `dotnet build` green, `BalanceMatrix` verified, SmokeTest not yet run.

---

## ✅ Just built (uncommitted)

**Exp rework** — new `Game.Shared/ExpCurve.cs` carries the real L2 curve to **level 100**, a fitted mob
curve (`0.026314·L^3.2427` above 30, seven hand anchors below), the SP ratio, the symmetric
`0.85^(gap-5)` level penalty (zero at 13), the party-size bonus, and the ±20% random roll. The kill
pipeline is now **shared pot, personal penalty**; drops take the killer's gap penalty. Full spec and
tables: [balance/ExpCurve.md](balance/ExpCurve.md), memory `exp-party-and-drop-design`.

**Three tier-1 bugs** — every character on the first account was born Admin (now only the first);
a second global broadcast leaked world entry/exit to non-friends (now off by default); the jail was not
a recognised domain so `/tp` to a jailed player warded the admin into a dungeon.

---

## 🔴 NEXT UP

**Tier 1 — ✅ ALL SEVEN FIXED** (0.28.67 + 0.28.68). Skills→Learn now explains why it can't; the soft
keyboard lifts the command bar; `[lead]` moves the badge and clears the button; dungeon mobs aggro,
retaliate and spread out. Root causes in memory `playtest-11-bugfix-progress`. **None device-tested.**

**Tier 2 — cheap UI/UX, mostly client-only**
Duplicate blue town-entry line · field banner needs **hit-test false** (it blocks ground taps) ·
player target frame `[info]`-only · debug-menu chat spam · spiritshot buff reads `719h59` not `29d` ·
bag `Equip` button first + column expands LEFT · stand-up delay (instant if seated >3s) · HoT floating
text for potions · target-window HP as digits + an MP bar for players · raw attack/cast speed beside
the multiplier (`1234/1500 (x3)`) · buff double-tap cancels / single tap opens details · party
buff/debuff **squares** + loot proposal **drop-down** · **world border** (orange dashed).

**Tier 3 — server work**
- ✅ **Damage ledger — BUILT (0.28.69).** Most damage earns the kill (drops, quest credit, karma);
  contested kills split the exp by damage share pooled by party; last hit kept as a raid-boss counter.
  The exp spec is now complete in code.
- 🎯 **Partial-stack trading** — design **ANSWERED: YES**. `TradeOffer` must carry per-item counts and
  split on completion. Closes the long-running trade-numpad thread.
- **Starter-gear redesign** — newbie boxes become a **level-10 quest**; levels 1-10 get the weakest gear
  in the game (training weapons 400g, training armor, no shots/jewels; broken jewels drop 1-5). The
  curve half is now DONE, so this is what's left of the early-game pass.
- **Shop rework** — items need details + buy-time info; prices far too cheap (equipment ≥200g, runes
  150k/1h and 280k/2h).
- **Client-side collision** — stop movement at the surface and reject out-of-world taps, so the server
  clamp (`ConfineToDomain`) goes back to being an anti-cheat backstop rather than the everyday
  mechanism. Memory `worlds-and-collision-design`.
- **Target a party member with no range restriction**, so kick/change-leader work from buttons.
- **Admins excluded from the rankings** — an admin at level 999 breaks every board.

**Tier 4 — new systems**
- **Chat**: colours, tabs, tags (`[!]` world, `[W]` whisper) — WPF already has the first two — plus
  **peek/fade when the log is hidden**.
- **Every non-admin command as an ACTION** — friend, party, sit/walk/run, attack/assist/next, living in
  the Skills window's **Actions tab** and **placeable on the skill bar**. The 0.28.55 player-target
  button grid comes out; `[info]` stays for mobs only.
- **Block system** — `/block` `/unblock` `/blocklist`; permanent; silences every chat form.
- **Charisma system** — `/like` (+1, 20/day, never negative); killing costs `karma × 0.01`; every 20
  charisma = +1% exp/sp drop, capped 1000 = +50%; chatban −20/h, jail −100/h, kick −250/h, ban zeroes
  both; two values (0-1000 bonus pool, lifetime total for ranking).
- **Buy-back menu** — last 10 deleted/sold items; `[r]` restores a deleted or sold-for-0 item.

---

## Independent — buildable any time, no blockers
- **Per-char warehouse** — the other half of the rune-shots spec (space + rune-disable); the rune half
  shipped in 0.28.62-64. Account warehouse deferred. Memory `shots-rune-and-warehouse`.
- **Wearable titles** — leaderboard title over the head / by the name. Now overlaps with **charisma
  ranking**, which adds a second board; design the two together.
- **Combat depth** — perfect/excellent block, magic-resist passives, position bonuses. Each independent.
- **More fields/dungeons** — author a field (the startup guard enforces every spawner lives in one);
  for a dungeon, drop it in the negative quadrant.

## Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs. Everything class-progression waits
  here; biggest single content unlock once they arrive.
- **Instances** — design done; owner HOLDING. Open decision: daily attempts GLOBAL vs PER-INSTANCE.
  The negative quadrant + walls are the groundwork.
- **Command channel** — a leader party holding a group of parties, exp divided across the channel.
  Needed for epics/world bosses. Gated on **clans/alliances** first.
- **World expansion to 1kk+** — size is one constant and the grid is sparse, so it scales freely; grow
  it as content + teleport hubs fill in. The world border is the first visible step.
- **Castles + vault** — needs siege design; consumes the reserved `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — depends on bosses + instances producing the points first.
- **Crafting economy** — BUILT; remaining polish (Epic recipes, mat sinks) is incremental.

## Deferred (explicit owner hold)
- **MP potions** — DEFERRED 2026-07-24 (owner). Flat mana-over-time tiers mirroring the HP potions.
  Small and unblocked, but explicitly held: the MP economy it patches is decided by the 3rd-class
  discipline kits, and there is no point authoring numbers for a shortage that doesn't exist yet.
  The auto-potions window already reserves the MP row.
- **SP extraction — "SP bottles"** (2026-07-24). 1e9 SP → one bottle; skills cost **bottles + gold**.
  Also why `SkillPoints` can stay an `int`: bottling drains the counter so it never nears the 2.15e9
  ceiling. `AwardExp` saturates meanwhile. No dependencies.
- **Bot-prevention CAPTCHA** — petrification after 200-500 back-to-back kills, mob-immune, tap
  challenge. A CAPTCHA only stops low-effort scripts; **behavioural detection** is the real net, and
  **charisma + block** now give a social signal feeding the same problem — revisit together.
- **3rd-class CSVs, Instances** — owner said "Hold" (2026-07-22).

---

## My view of the order
1. 🔴 **PLAYTEST 0.28.69 on the phone.** Three commits of deep change (the whole exp/party/drop system,
   seven bug fixes, the damage ledger) are pushed and **none of it has been device-tested**. The
   SmokeTest has not run either, and `AwardExp` touches persisted Exp and SkillPoints. This is the
   biggest untested stack since the parity batches — verify before more lands on top of it.
2. **Starter-gear redesign** — the curve half is done; this completes the early-game pass. Verify with
   `BalanceMatrix` before/after.
3. **Tier-2 UI batch** — a dozen cheap client-only fixes; batch and build once.
4. **Partial-stack trading**, then the **shop rework** (pairs with starter gear).
5. **Chat** (colours/tabs/tags), then **block** — small, and makes the social layer safe.
6. **Charisma** — the biggest new system; design its board alongside **wearable titles**.
7. **Client-side collision**, **buy-back**, **warehouse** — self-contained, any time.
8. **3rd-class kits** the moment the CSVs land — still the highest-value unlock in the file.
