# OPEN CHECKLIST — rolling, reset 2026-09-29

> **A clean reset**, from your note of 2026-09-29: *"this file i havent used for a while. its stale with old entries, most
> of the rows inside are a backlog entries"*. The whole old file (the 0.93.0 header, §81-§107, the old finds, KNOWN OPEN)
> is kept **verbatim** at the top of [Playtest-Archive.md](Playtest-Archive.md#open-checklist-2026-09-29). Everything
> still owed to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a `BL-nn`.
>
> **This file now holds only three things:** your finds, the rows for builds you have not played yet, and §0, the
> questions only you can answer.
>
> **Rows:** write your comment after the `->`. Put `x` in the `[]` if it passed with nothing to say, `~` if it works but
> wants a change, `!` if it is a bug, `?` for a question. A `-` row with no id is a free line.

---

## My Finds — next pass (empty, write here)

---

## §109 — 0.214.42: the Blessing bar you can read (2026-09-29)

⚠ **New APK** (a client-only change; the server is the same as 0.214.41).

- `109a` [ ] - **A running Blessing is deep amber now**, not bright gold, and its timer has a firmer outline. The
  filling gauge (not running) is a slightly darker gold than before, so the two states still differ at a glance. Can
  you read `Blessing 12:34` at a glance, on both the full and the nearly empty bar? ->
- - ->

## §108 — THE TOWNS, BUILT (0.214.41, `BL-319`) — your five answers, 2026-09-28

Built from your answers to the sketch (https://claude.ai/artifact/6GVfFFjotDCfKUWZ3pFr5N): visual walls, majors X and
minor towns Y, the majors at r 3000, NPCs at the door, and the **Adventurers Guild** with its **Guild Receptionist**.
Your two side ideas are filed: server-side collision is **`BL-323`** (waits on the models) and the town sprint is **`BL-324`**.
⚠ **New APK.** The old one still connects but draws no streets.

- `108a` [ ] - **Brackenford is an X.** Four roads from the plaza to four gates, with two guards at each. The church is
  north-west (Vael, Oren, Marius, and the Mindwright at its north door). Arms & Armour and the Apothecary are north-east,
  the Keeper south-east, and the crafthall south-west (Master Crafter at the south door, Anvil right behind him). Does it
  read as a town? ->
- `108b` [ ] - **Greymarsh and Frostmere are the same X, now r 3000**, with their fields 1000 further out. Greymarsh
  has the Grandmaster at the church and the Assayer beside the Apothecary; Frostmere has the Archmaster, Ledgerkeep Mora at
  the Keeper's side door, and the three recipe givers at the crafthall. Is the walk too long? (That is `BL-324`'s job.) ->
- `108c` [ ] - **Stonewatch and Ironreach are Ys.** Stonewatch's stem points south at Brackenford; Ironreach is the
  same Y turned round (stem north). Guild up the short road, shrine and keeper in the side wedges, shops on the stem. ->
- `108d` [ ] - **The Guild Receptionist** (was the Huntmaster) gives the same contracts under the new name; the
  tutorial's first step now says "Pell on the plaza". ->
- `108e` [ ] - **The roads between towns leave through a gate** and meet the other town's gate. Tap to move inside a
  building's footprint: the move marker must still show on top. ->
- - ->

---
- ✅ **Your answer, 2026-09-29:** *"I like the towns now. Now they look like a town and not scatared NPCs in a circle"*.
  And `BL-321`, the skill bar: *"also looks good"*.

---

## 0. STILL YOURS TO RULE — read, don't test

- 🔴 **`BL-314` — the passive split is designed** (`docs/design/PassiveSplit.md`), from your four answers. Your multipliers
  land every path at 0.54-0.67 (healers 0.45-0.51), and everyone is short from level 20. **Six questions in §9:** my
  k picks per archetype, the ladder file, equal price shares, `hp_regeneration`, the gates, the names.
- 🔴 **`BL-324` — the town sprint, my proposal** (in the Backlog entry): the **paved streets** idea with a VISIBLE buff.
  One question there.
- 🔴 **`BL-305`'s placeholder numbers** are still mine: the L10 recipe inputs, the 1/100 drop band, the boss 0.2 and the
  generic-gear MP. Played in 0.214.20-22 without a comment, so they stand until you say otherwise.

- 🔴 **`BL-94` — the fizzle floor.** *"shouldn't hit at all on the floor"*, carried out of playtest 28
  unbuilt and now a backlog entry rather than a checklist row. It is one line and it changes how it
  feels to fight above your level, so it wants your word: **flat 0 on a fizzle, or 0 only once the fail
  chance is at its ceiling** (the second is what today's `damage / 3` approximates).

- 🔴 **The CC ladder is yours to author** — `90v`'s red half. The attacker's level in the debuff contest
  is the rung's learn level, so every CC skill expires out of usefulness; a rung ladder in the CSVs is
  the fix, and nine CC skills are today learnable by nobody at all.

- 🔴 **`BL-22` salvage: the S row cannot be moved by this feature at all.** Your budget was *"10~20%
  decrease in time"*; the early rungs got exactly that (E −3% · D −10% · C −18%) and **A and S got −0%**.
  The cause is your own *"rarity for mats rarity"* mapping: salvage pays the rarity of gear that
  **drops**, and a normal mob and an **elite both cap at Epic** — only a boss (0.09 kills/h) drops
  Legendary. The A and S recipes bind on **Legendary Ingot**. `M13` in BalanceMatrix prints all three.
  ⏸ Parked with the rest of crafting, along with the **603h craft time you accepted**.

- ⚠ **The buff-vs-heal threat ratio, `BL-16`.** You sized the buff against a ~1500-power quick heal at
  70; the cleric's ladder stopped at skill level **4** (learned at 35, power **301**). The Lightbringer's
  40-74 rungs now exist, so the heal side finally has numbers above 35 — and §93A has just made every
  heal a **smaller fraction of a bigger bar**. `93e` and `90a` are where you would feel it, and together
  they are what decides whether `BL-16` is urgent.

- ⚠ **Numbers that are mine, not yours** — each flagged in the source: the top rung of **Madness**; the
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses; the **0.25 respawn
  exponent**, which your `85j` park leaves standing as mine; and now the **Rare Mana Potion's craft
  rung** (Potion Master L5, inferred from the Rare healing potion sitting there).

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a sixth time on purpose: the block channel moved once and Shield Mastery moved
  once, and moving these in the same pass would make neither reading attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable* · `0`, `-` or omitted → use the
  catalog's price · any positive number → that exact price (`k`/`m`/`b` and `1_000_000` both parse).
  Every argument after the item id follows the same rule: `-` is always *no opinion*.

---
