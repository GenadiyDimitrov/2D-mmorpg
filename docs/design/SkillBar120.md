# 120 bar entries — proposal (`BL-321`)

> *"to have 120 entries .. the 5x12 for the main bar and 5x12 for the additional bar .. the main bar to be 0~60 (1~5)
> and additonal to be 61~120 (6~10) and the main bar still to move the additonal +-1 with it self .. now when i move my
> bar from 1 to 2 .. the 1 goes up (on the additonal place) and its ackward — the [to bar] in the skills tab never to be
> disabled … also remove the half bars ... the addtional bar can only be a full bar (12 slots not 6) and it follos the
> position of the main … the current additional bar button in the setup can only be 0/1/2"* (playtest of 0.214.16)

**Status: RULED and BUILT in 0.214.40** — his answers (§5) changed the model: see §5, it overrides §2-§3.

## 1. What it is today (0.214.16, `BL-299`)

- **60 entries**, one run. The main block shows `page × main + n`. The extra squares **continue the run** after it, so on
  page 1 the extras show entries 12-23 (page 2), and paging main to 2 moves page 2 **down** into main while page 3
  appears above. That is the "1 goes up" you saw: the extra block is always "the next pages", so it slides with you.
- Main shapes: 2x6, 1x12, 6x2, **6x1**. Extras: 1x6 … 4x6 / 1x12, 2x12 / 6x1 … 6x4, **half bars** included.
- `[To bar]` is disabled once the skill is anywhere on the bar, even on a page you cannot see.

## 2. The proposal

**Two fixed halves, not one run.**

| | entries | pages | shown by |
|---|---|---|---|
| **Main** | 0-59 | 1-5 | the main block, paged with `< n/5 >` |
| **Additional** | 60-119 | 6-10 | the additional bars, 0, 1 or 2 of them |

- **An additional bar is always a full 12**, in **the main's shape**: main 2x6 → each additional is 2x6 above it; 1x12 →
  1x12 above; 6x2 (vertical) → 6x2 to its left. Setup's button becomes **"Additional bars: 0 / 1 / 2"** and "Bar
  shape" keeps choosing the main (2x6 / 1x12 / 6x2).
- **The additional bars page with the main, ±1 as you said:** main on page *p* (1-5) → additional bar 1 shows page
  *p + 5* (6-10), bar 2 shows page *p + 6*, wrapping inside 6-10 (main 5 → bars 10 and 6). Paging main never moves an
  entry from one half to the other, so page 1 stays in main.
- **`[To bar]` is never disabled.** Pressed on a skill already on the bar, it **moves** it: the old slot is cleared when
  you tap the new one. A skill parked at 77 with no additional bar showing can be pulled into main.
- **Server:** `GameConstants.SkillBarSlots` 60 → 120. The stored bar is copied into a fresh array of that size
  (`SyncSkillBar`), so a saved 60-entry bar simply keeps its 60 entries in main, pages 1-5. No `game.db` delete needed.
  It changes the wire length of the bar, so the protocol version goes up (**a new APK and a new server together**).

## 3. What building it touches

- `GameConstants.SkillBarSlots` 120, `ProtocolVersion` +1.
- Client `GameUi.World`: `BarIndexOf` becomes two functions (main = `page×12 + n`; additional bar *b* = `60 +
  ((page + b) mod 5)×12 + n`), `ExtraOptions` → a 0/1/2 count, the layout places 1-2 copies of the main shape.
  `MaxVisibleSlots` 12 + 24 = 36.
- Client `GameUi.Skills`: `[To bar]` always live; `BeginAssign` remembers the old index and clears it on placement.
- SmokeTest: a bar with an entry at 77 survives a relog; a 60-entry bar grows to 120 in place.

## 4. Your questions

1. **The 6x1 main shape** holds 6, so it cannot show a 12-entry page. **Drop it** (my pick: the shapes become 2x6 / 1x12
   / 6x2), or keep it and let it show half a page?
2. **Two additional bars: *p+5* and *p+6*, wrapping inside 6-10** — or should bar 2 be fixed to one page you choose?
3. **Moving with `[To bar]`:** move (clear the old slot, my pick), or copy (the skill on two slots)?
4. The page label: **"1/5"** on the main only, or also a small "6"-"10" on each additional bar?

## 5. His answers (2026-09-28) — what was built in 0.214.40

1. **Shapes 1x10 / 2x5 / 5x2**, and the additional bars follow the main: *"main bar is 1x12 .. want 1 extra -> got
   2x12 -> 2 extra is 3x12"* — then corrected: *"the page to have 10 slots ... not 12 .. that why the table is 150 but
   the settings is left x12 ... my bad ... should have made the example with settings 2x5/1x10"*. So 1x10 → 2x10 → 3x10,
   2x5 → 4x5 → 6x5, 5x2 → 5x4 → 5x6.
2. **Each main page owns its own two additional bars** (not p+5 / p+6): *"they move with the main one ... each page
   1/5 will have 10/20/30 slots ... so max we will have 150 skill slots"*. His table:

   | main page | 0 extra bars | 1 extra bar | 2 extra bars |
   | :-------: | :----------: | :---------: | :----------: |
   | 1/5 | 1-10 | + 11-20 | + 21-30 |
   | 2/5 | 31-40 | + 41-50 | + 51-60 |
   | … | … | … | … |
   | 5/5 | 121-130 | + 131-140 | + 141-150 |

   (his 4/5 and 5/5 rows skipped and overlapped a block; read as the evident 30-per-page pattern). In code: page p
   (0-4) main = `p·30 + n`, additional bar b (1-2) = `p·30 + 10·b + n`. `GameConstants.SkillBarSlots` = **150**.
3. **`[To bar]` COPIES** and is never disabled: *"i can make 10slots with 1 skill ... one page to be for solo fighting
   .. the other to be for party .. some skills will be on both pages"*.
4. **"1/5" on the main only**; the additional bars look as before (separate, stuck to the main).

- Setup: "Bar shape: 2x5 / 1x10 / 5x2" and "Additional bars: 0 / 1 / 2" (new pref key `ui.extraBars`).
- Protocol 51 → 52 (new APK + server together). No `game.db` delete: an old 60-entry bar keeps its indices and
  lands in the new layout by index (its 11th/12th squares now sit at the start of page 1's additional bar 1).
