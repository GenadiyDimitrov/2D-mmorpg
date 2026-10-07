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

## §144 — 0.232.0: the party window has no window (2026-10-07)

⚠ **APK + server restart.** Needs a party of two or more.

- `144a` [ ] - No frame or background: only the member plates and their effects. A tap in a gap between members (or
  beside a plate) reaches the world: it walks you, or selects whatever is behind it. Each member added makes it taller. ->
- `144b` [ ] - Tap a plate → that member is selected (their plate lights). Hold your own → *Leave party*. Hold someone
  else's as leader → *Make leader* / *Kick*; as a non-leader → nothing. A tap outside the menu closes it. ->
- `144c` [ ] - Effects: six per row under the plate, with picture + time. Tap one → THEIR buff card (name, Lv, text,
  time left, "On <name>."), not yours. ->
- `144d` [ ] - "Party N options" → Party options: Buffs / Debuffs / All / None changes what is under every plate; Small
  halves the whole roster while the header stays full size; the leader can propose a loot mode, others only see it. Both
  settings survive a restart of the app. ->
- `144e` [ ] - Drag the header, a plate or an effect: the whole window moves. A drag never selects anyone or opens the
  menu. ->

---

## §143 — 0.230.0: the Warchanter hits with MAGIC (`BL-335`, 2026-10-06)

⚠ **Server restart + APK + `game.db` delete** (or a new Warchanter). Numbers marked "mine" in §9 of
`docs/design/MagicMeleeBuffers.md` are first passes, so tune them in the CSV.

- `143a` [ ] - All three races in a robe: a basic swing shows MAGIC damage (it crits big and rarely, like a spell),
  can still MISS, is never blocked, and restores MP with Mana Vampirism. ->
- `143b` [ ] - Human (wand + shield) Sound Smash / Demon (battlestaff) Sound Smash + Acoustic Shock / Elf (duals) Magic
  Stab: each is a spell now. It can fizzle, cast speed shortens it, and a hit can interrupt it. Holy Bolt is gone from
  the bar. ->
- `143c` [ ] - Magic Stab fizzles often (~60% on an even-level mob), and ~40% with the Elf's Sharpening lit. ->
- `143d` [ ] - Reinforcement raises P.Def by a % and Sharpening adds its race's half. Each makes skills cost 15% more
  (both = 30%) and drains MP every second. Sharpening goes dark when you take off the shield (Human), the 2H blunt
  (Demon) or the duals (Elf). ->
- `143e` [ ] - Feel: does a Warchanter's damage feel like a HEALER's (the target), not a nuker's or a warrior's? ->
- `143f` [ ] - The Learn tab: no heavy/light/weapon/bow masteries, no Shield Mastery and no Bow Expertise. The Elf gets
  Monster Knowledge and Mana Vampirism to 9%; Combo Mastery procs with your race's weapon only. ->
- `143g` [ ] - (0.230.1) The Human's Sound Smash refuses a two-handed blunt; the Demon's refuses a one-handed one.
  Harmony of Restoration, cast every 30s rather than spammed, now leaves you with MORE MP from 64 up (at 74: 280
  spent, 300 back). At 76+ it is learned every other level. ->

---

## §142 — 0.229.1: Common jewels give no MP (2026-10-04)

⚠ **Server restart + APK.** No `game.db` delete.

- `142a` [ ] - A Common T61 necklace/earring/ring shows M.Def but no MP line, and equipping it leaves Max MP unchanged;
  the plain (Mythic) piece still gives its MP. ->

---

## §141 — 0.229.0: fighters carry ~1200 MP at 75 (2026-10-03)

⚠ **Server restart.** No APK, no `game.db` delete (Max MP is recomputed at login).

- `141a` [ ] - A level-75 fighter (any of the four) shows roughly 1400-1600 Max MP naked, about 2.3× what it had; a
  mage's Max MP is unchanged. ->

---

## §140 — 0.228.0: runes stack, up to 12 hours (2026-10-03)

⚠ **Server restart.** No APK, no `game.db` delete.

- `140a` [ ] - Open a War Rune box (2h), then another: ONE War Rune in the bag, about 4h left, chat says "extended". ->
- `140b` [ ] - Keep opening 2h boxes: at ~10h the next one opens (→ ~12h), the one after stays SEALED with "runes stack
  to 12h". A 1h box at 11h+ is refused the same way. ->
- `140c` [ ] - A 1d (admin) box on top of a 12h rune still opens and adds the full day. ->
- `140d` [ ] - War and Spell runes stack separately; the Grand Rune still hides both while it runs. ->

---

## §139 — 0.227.2: a toggle on auto stays on (2026-10-03)

⚠ **Server restart.** No APK, no `game.db` delete.

- `139a` [ ] - **Your find:** Sharpening (and Reinforcement) armed on the auto bar, auto-hunt on — it lights ONCE
  ("Sharpening activated.") and stays up; no on/off flicker. ->
- `139b` [ ] - Run your MP dry with it armed: it ends ("not enough MP to hold it") and the autopilot leaves it off for
  ~10 s, then re-lights it once you have 10 s of upkeep. ->

---

## §138 — 0.226.1: a replacement is transitive (2026-10-02)

⚠ **New APK + server restart.** No `game.db` delete.

- `138a` [ ] - **Your find:** a Demon buffer who owns Holy Bolt/Holy Spike learns Sound Smash at 40 — Holy Bolt, Holy
  Spike AND Magic Bolt are all gone, and Magic Bolt does not come back on relog or level-up, nor in the Learn tab. ->
- `138b` [ ] - **A fighter** with Smash (or a rogue with Precise Shot) learns a 3rd-tier slash/shout/stab: the 1st-tier
  Strike / Shot / Stab does not reappear either. ->

---

## §137 — 0.226.0: icons for the NPC buffs, potions, scrolls, runes and the paving (`BL-331`, 2026-10-02)

⚠ **New APK + server restart.** No `game.db` delete. Every picture is a row in `docs/data/skill_icons.csv`; the review
page is `docs/design/SkillIcons.html`.

- `137a` [ ] - **Spirit helper:** take a few NPC singles and a group. Each single wears its class single's picture
  (NPC Might = Might); the four groups have their own. ->
- `137b` [ ] - **HP / MP potions** on the skill bar and their buff on the buff bar: red for HP, blue for MP, the bottle
  shape is the tier, the Instant Healing Potion a heart bottle. The count still shows on the bar. ->
- `137c` [ ] - **Swift / Alacrity / Fury potions and Dash:** a flask each, coloured by the potion's rarity. ->
- `137d` [ ] - **Runes** (War / Spell / Grand and any reward rune you can `/give`): a runic glyph each on the buff bar. ->
- `137e` [ ] - **Paved Streets** in town: a stone-path picture instead of letters. ->
- `137f` [ ] - **Not in your list, done the same way:** the buff scrolls (the single's picture in the scroll's rarity
  colour) and the two Over-Grade debuffs (broken shield / broken axe). Keep or change? ->

## §136 — 0.225.2: skill SP is a formula, and every ladder rises (`BL-334`, 2026-10-02)

⚠ **New APK + server restart.** No `game.db` delete. Your knobs: `docs/data/sp_bands.csv`, `sp_adj.csv` (±% per file),
`sp_weights.csv`, then `SkillCsvSeed -- --reprice-sp`.

- `136a` [ ] - **Learn tab, any class:** every skill's next rung costs MORE than the one you bought before it, at every
  level up to 75, across the 2nd → 3rd class change too. ->
- `136b` [ ] - **The pace, by feel:** a tier at 40 bought by ~43-45, 52 by ~55-56, and 61-75 tight enough that you
  postpone a few (52-60 ×0.85, 61-75 ×0.8). 1-19 has room to spare (×1.75). Too tight or too loose → change a band's X in `sp_bands.csv`. ->
- `136c` [ ] - **Tank, 40-52:** Shield Shock's 3rd-class rungs climb only +1% (122k → 130k): the tank 2nd curve is
  steep. OK as is, or raise the tank 2nd `ADJ` (a cheaper 2nd class) in `sp_adj.csv` and `--reprice-sp`? ->
- `136d` [ ] - **Races in one class:** each race's OWN skills cost that race's share (×0.5-×2, e.g. Demon Magus own skills
  ~×0.7, Elf Magus ~×1.3), so a Human and an Elf of the same class feel equally tight. Compare two races at 60+ if you can. ->

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
