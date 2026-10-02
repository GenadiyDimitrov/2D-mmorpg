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

## §129 — 0.222.0: `/who <name>` (2026-10-02)

Your ask: *"`/who <name>` command that opens stat window of the character - an *admin* command"*. ⚠ **New APK + server
restart.** No `game.db` delete.

- `129a` [ ] - **`/who <name>` on an online player** (a second phone, or a second character): the Character window opens
  headed with their name, Basic and Details tabs both theirs (class, stats, PvP/karma, Favor, gold). Close it and press
  Char: your own sheet again. ->
- `129b` [ ] - **`/who` on someone offline** says they are not online; a moderator typing `/who` is refused. ->

---

## §128 — 0.221.0: `/help` per rank; actions show their typed command (2026-10-02)

Your find: the `/help` layout by rank, no action commands in it, the missing admin ones, and `(/command)` on actions.
⚠ **New APK + server restart.** No `game.db` delete.

- `128a` [ ] - **`/help` as each rank** (switch with `/role`): a player sees the `@s`/`@t` line and **--- Commands ---**;
  a Chat Moderator adds **--- Chat Moderator ---**; a Moderator adds **--- Moderator ---** above it; an Admin **--- Admin
  only ---**; you, the Owner, **--- Owner only ---** on top. Each line is the command, then what it does. ->
- `128b` [ ] - **Nothing is missing and nothing is wrong** in the Admin list (`/stat`, `/tpme`, `/enchant`, `/whatdrops`,
  `/farmcap`, `/testcaps` were the missing ones). Name any line that reads wrong. ->
- `128c` [ ] - **A moderator can still use exactly what he could** — `/jail`, `/kick`, `/where <name>`, `/chatlog -w` —
  and is still refused `/tp` or `/god`. (The allow-list is now read from the same list `/help` prints.) ->
- `128d` [ ] - **Skills window → Actions:** Invite to Party reads `(/ptinv <name>)` in small grey after its name; the same
  for the friend, party, whisper, like and block actions. The chat `/help` no longer lists them. ->

---

## §127 — 0.220.3: four of your finds (2026-10-02)

⚠ **New APK + server restart.** No `game.db` delete.

- `127a` [ ] - **Human / Demon buffer at 40:** learning Heavy Armor Mastery removes Rogue Evasion as well as Light Armor
  Mastery (your CSV edit: *"no point of human and demon to keep the `rogue_evasion` when their `light_armor_mastery` is
  being replaced"*). The Elf buffer keeps both. ->
- `127b` [ ] - **Chat keeps 300 rows** (was 120). Fight a while, then scroll the System tab back to what dropped earlier. ->
- `127c` [ ] - **The quest arrow is on for every character you enter** (your 121d: *"set location to true … Newbies need an
  arrow"*). The cause: turning the arrow off ("Location: current") on one character kept it off on the next one in the
  same session. A new character now points at Cera from the first second, no tap needed. ->
- `127d` [ ] - **Notifications** (your `?`): my answer is `BL-332` in the Backlog — what each kind would take, and four
  questions. ->

---

## §125 — 0.220.1: an expiring buff pulses instead of flashing yellow (2026-10-01)

- `125a` [ ] - **Under 60s left, the buff square now fades 1 → 0.3 → 1** once a second (0.220.3; it went to 0.5, which
  you found *"almost visible"*). Debuffs and a greyed (inactive) buff don't pulse. ->

---

## §121 — 0.218.2: copy that pastes, the guide to Cera (2026-10-01)

- `121a` [ ] - **Copy / Cut in the Secure Folder copy of the game.** It works in the main game (your answer). Samsung's
  Secure Folder keeps its OWN clipboard, walled off from the phone's, so a word copied inside it pastes only inside it.
  Test it there: copy in chat, paste back into chat in the same Secure Folder game. If THAT fails it is ours; if only
  pasting out to the main side fails, that is the wall, and the Secure Folder's settings decide it, not the game. ->
- `121d` [ ] - **A new character is born holding "Adventure Begins"** with the arrow already on Cera (see `127c`). Talk to
  her: it is gone, no reward, and "Welcome, Traveller" is in her window. ->

---

## §117 — 0.217.5: SP −30% for every skill learned at 40-75 (2026-10-01)

⚠ **New APK.** No `game.db` delete.

- `117a` [ ] - **Any 3rd class, levels 40-75:** every Learn-tab SP price is 70% of what it was (e.g. Tank Shield Mastery
  at 40: 19,200 → 13,400). Can you now afford your farming skill when it unlocks? 76+ and below 40 are unchanged. -> Im lvling at the moment in few minutes ill be 40 and start checing after 40 farms -> if no db reset will be needed ill finish this test as well

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
