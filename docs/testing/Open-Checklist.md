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
>
> **Housekeeping (your rule, 2026-10-02):** *"dont leave stale open-checklist entries. Checked should go and the '~' ones
> should stay with '~' removed and description updated so they can be rechecked"*. So after each pass: an `x` row leaves
> (a section with nothing left leaves whole); a `~` row stays, unmarked, reworded to what the next build does; your finds
> move into the new version's section. What left is kept verbatim in
> [Playtest-Archive.md](Playtest-Archive.md#checklist-0220-pass), comments and all.

---

## My Finds — next pass (write here)

- [ ] ->

---

## §135 — 0.224.0: whole-second cooldowns; sheets that open whole; the rune texts agree (2026-10-02)

⚠ **New APK + server restart.** No `game.db` delete. Your finds.

- `135a` [ ] - **Skill bar reuse counter** shows whole seconds only (`3`, `2`, `1`), no tenths. ->
- `135b` [ ] - **Character window → Details** on the FIRST open after a fresh start: every stat visible, the scroll runs
  to the bottom. Same for a **skill card** and a **Learn** card (tap a Learn row): the description is all there first
  time. Leave the Character window open while you regen: it does not jump back to the top. ->
- `135c` [ ] - **Rune texts** (your 132a): the Spell Rune and the Grand Rune (item, each box at the Apothecary, the buff
  icon) both say *"shortens spell cast time"* and nothing else about casting. ->

## §134 — 0.223.2: the Master's Trial — five gather steps, and a fail never walks you back (2026-10-02)

Server-only. ⚠ A character already mid-trial: abandon and retake it (the steps are renumbered again).

- `134a` [ ] - **Take the trial** (your 130a): after the first talk you get the five separate gather steps again, each
  with its own counter (wood 0/20, iron 0/20, gems 0/20, recipes 0/2, head 0/1). Gather in any order. ->
- `134b` [ ] - **Fail the hammer with nothing spare**: you STAY on the craft step — no going back, no talk-back. The
  message says to gather another set; the trial's mats keep dropping; craft again at the anvil when you have them. ->

## §133 — 0.223.1: presets survive a restart; level-ups are your own news; `/help` and `/buff` (2026-10-02)

Server-only. No `game.db` delete.

- `133a` [ ] - **Equipment presets** (your `[!]`): ⚠ re-save each preset ONCE on this build (the old saves hold ids
  that no longer exist). Then restart the game / relog: applying the preset equips everything, no "N items missing". ->
- `133b` [ ] - **Level up**: you see "You reached level N!"; nobody else gets a line about it. ->
- `133c` [ ] - **`/help` as a Player** (your 128a): `/buff` is listed only while the server hands out free buffs
  (`RateConfig.FreeBuffs`, off by default) — so normally it is gone. ->
- `127d` [ ] - **Notifications** (your `?`): my answer is `BL-332` in the Backlog — what each kind would take, and four
  questions. ->

---

## 0. STILL YOURS TO RULE — read, don't test

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
