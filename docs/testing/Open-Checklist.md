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

## My Finds — next pass (write here)

- [ ] ->

---

## §119 — 0.218.1: the Android text boxes — a caret, mid-text taps, Copy / Cut / All (your F1-F3, 2026-10-01)

⚠ **New APK.** No `game.db` delete. Your three finds (*"still dont see the cursor"*, *"still cannot copy text"*, *"still
cannot select part of text or go to its middle"*) had one root cause plus one wall: the caret was **never built** (it was
there and moving, just invisible), and Android's own copy menu **cannot exist** in this client (the app runs on
GameActivity, which has no native text box to hang that menu on). So copy is a small strip of ours, shown only while you
have text selected; paste stays the keyboard's.

- `119a` [ ] - **The caret:** tap any text box (chat, login, a gold amount, a window's search) and type. A blinking bar
  shows where you are typing. ->
- `119b` [ ] - **The middle of the text:** type a line in chat, then tap between two letters. The caret goes there, and
  typing or deleting changes the middle, not the end. A box that already holds text and gets focus from code (Reply, the
  whisper action) still starts at the end. ->
- `119c` [ ] - **Select + copy:** drag your finger across part of the text, or double-tap a word. It highlights, and a
  **Copy / Cut / All** strip appears just above the box. Copy, then paste with the keyboard somewhere else. Cut removes the
  part, All selects everything. Password boxes have no strip, on purpose. ->
- `119d` [ ] - **Chat box specifically:** it no longer has Android's own input strip above the keyboard (0.114.0's attempt
  at the native menu, which never worked here and cost chat its caret). You type straight into our box like every other
  one. Anything wrong with how it sits above the keyboard now? ->

---

## §118 — 0.218.0: Paved Streets — run faster on a city's streets (`BL-324`, 2026-10-01)

No new APK (the server sends the speed and the bar row). No `game.db` delete.

- `118a` [ ] - **Run on a city's plaza, roads or side paths, out of combat:** a "Paved Streets" row appears on the buff
  bar and you run +50 faster (never past 250). Step off the paving, walk instead of run, or get into a fight and it goes.
  Does the edge of the road feel right, or too strict (a road's width is 270 in the big cities, 210 in Stonewatch/Ironreach)? ->
- `118b` [ ] - **Re-measure the Demon buffer** from your farm pass (Bdd needed ~2 h more than the Elves to 40) now that
  0.217.4 gave Human/Demon priests Holy Spike and Monster Knowledge and 0.217.5 cut the 40-75 SP. Is the Demon still
  behind, and by how much? ->

---

## §117 — 0.217.5: SP −30% for every skill learned at 40-75 (2026-10-01)

⚠ **New APK.** No `game.db` delete.

- `117a` [ ] - **Any 3rd class, levels 40-75:** every Learn-tab SP price is 70% of what it was (e.g. Tank Shield Mastery
  at 40: 19,200 → 13,400). Can you now afford your farming skill when it unlocks? 76+ and below 40 are unchanged. ->

---

## §116 — 0.217.4: Holy Spike + Monster Knowledge for Human/Demon priests; buffers fight with the weapon (2026-10-01)

⚠ **New APK.** No `game.db` delete.

- `116a` [ ] - **Human/Demon cleric learns Holy Spike at 20/25/30/35** (Demon sees "Spirit Spike"). It hits a monster;
  aimed at a player it is refused outright. The Elf cleric does not get it. ->
- `116b` [ ] - **Monster Knowledge** for the Human/Demon cleric at 35 (+5%), then the Human/Demon Warchanter at 40/48/52
  (+10/15/20%). The Lightbringer stops at +5%. ->
- `116c` [ ] - **At 40 the spike and the bolt leave:** Holy Ray (Lightbringer) retires both; Sound Smash / Acoustic Shock
  retire both on the Human/Demon Warchanter, Sound Burst retires the bolt on the Elf. The race spell (Vampiric Bolt /
  Frost Spikes) stays. ->
- `116d` [ ] - **Frost Spikes' description** reads "Slow the enemy for 15%." (new `@{slow}` placeholder). ->

---

## §115 — 0.217.3: your CSV pass in the code; race faces for the 3rd-class single buffs (2026-10-01)

⚠ **New APK.** No `game.db` delete.

- `115a` [ ] - **3rd-class single buffs wear the caster's race:** a Human healer/buffer casts "Blessing: Ferocity", an
  Elf "Forest Ferocity", a Demon "Demonic Contract: Ferocity" — the same for Fortitude, Endurance, Wellspring, Serenity,
  Insight, Fury, Guard, Bastion, Mana, Great Strength, Great Bulwark. Renamed nouns are mine, change any: Body →
  Endurance, Soul → Wellspring, Shield Blessing → Guard, Shield Hardening → Bastion. ->
- `115b` [ ] - **Buffer's equipment passives cost 0 SP at 40:** Heavy Caster Mastery (Human/Demon) and Harmonist Bow
  Proficiency (Elf); learning Heavy Caster Mastery removes Light Caster Mastery. ->
- `115c` [ ] - **Archer 4th Light armor Evasion** reads +13..+17 (2 under the dagger). ->

---

## §114 — 0.217.2: names live in the class CSVs again; the spirit helper reads its names from them (2026-09-30)

⚠ **New APK.** No `game.db` delete. Your call: the class CSV's `NAME` is the name again and a new last column
`DESCRIPTION` is the text; `skill_faces.csv` holds only race/class exceptions; `skill_faces_other.csv` the skills no
class learns. The game should read EXACTLY as before, except for the rows below.

- `114a` [ ] - **Spirit helper window:** every blessing reads "NPC Might", "NPC Aim", "NPC Harmony of the Might" … and
  the Marks read "NPC Blood Mark" / "NPC Holy Mark" / "NPC Life Mark". The buff bar shows the SAME name (no "NPC NPC").
  A healer's own Blood Mark still reads "Blood Mark". ->
- `114b` [ ] - **Your new racial faces:** Elf "Forest Strength" / Demon "Fire Strength" (Might), Elf "Forest Bulwark" /
  Demon "Fire Defence", Human "Blessing of Swiftness" / Elf "Wind Flow" / Demon "Hell" (Swift — ⚠ is "Hell" the name you
  meant?), and the Elf harmonist's Bow Expertise text. ->
- `114c` [ ] - **Typo fixes carried into the CSVs** (the game already showed the right spelling): Wirlwind → Whirlwind,
  Shattaring → Shattering Shout, Bow Stence → Bow Stance, Domonic → Demonic Blessing, Monster Knowlege → Knowledge,
  Healers Power → Healer's Power, rogue 2nd "Critical Damage" → Critical Damage Mastery, and capitals (Arrow Barrage,
  Ultimate Party Heal, Life/Blood/Vanguard Support, Over the Limit); buffs.csv harmonies → "NPC Harmony of …". Nothing
  to test — say if any was deliberate. ->

---

## §113 — 0.217.1: "Lv.N" is your own step, not the shared ladder's rung (2026-09-30)

⚠ **New APK.** No `game.db` delete. Your find: Agility learned straight at "Lv.2", and crit-damage levels jumping 3-5-8.
The engine still climbs the shared ladder (that is what lets one skill serve every class); the LABEL now counts your own
class path's steps, from your 1st class on.

- `113a` [ ] - **Cleric @30 learns Agility → "Agility Lv.1"** (+2 Evasion); Lightbringer/Warchanter @44 → Lv.2 (+3), @52
  → Lv.3 (+4). The buff bar shows the same Lv on whoever you buff. ->
- `113b` [ ] - **Rogue/archer Critical Damage Mastery** reads Lv.1, 2, 3 … with no gaps; a 3rd-class rung continues from
  your 2nd-class count. The learn confirm's "Now → After" shows YOUR current numbers. ->
- `113c` [ ] - **NPC buffer:** the bar reads **"NPC Might"**, "NPC Agility" … with no level. Potions/scrolls: no level. ->
- `113d` [ ] - **Your question — rename shared passives per class?** Archer and dagger both show "Critical Damage Mastery
  Lv.8" at 46, one +252, the other +282. My take: not a bug players will report (each sees only his own), but the face
  file makes a per-class name free — a CLASS row in `skill_faces.csv`, no code. Worth it where the numbers split hard
  (crit damage: +665 vs +1015 at 74), not for every ladder. Your names, if you want them. ->

---

## §112 — 0.217.0: skill FACES — one skill, a look per race or class (`BL-327`, 2026-09-30)

⚠ **New APK** and a **`game.db` delete** (the three racial Might ids are gone — they are one skill now).

Your design: `docs/data/skill_faces.csv` is how a skill LOOKS, the class CSVs are what it DOES. Columns
`SKILL_ID,NAME,RACE,CLASS,DESCRIPTION,COMMENT`. Every skill has a blank row (everyone); add a `human`/`elf`/`demon` row,
or a CLASS row with a real class name ("Ice Master"), and that race/class sees its own name and text. Most specific wins:
your class (4th → 3rd → 2nd name) → your race → the blank row. Mobs and NPCs always read the blank row.

**Numbers in DESCRIPTION:** `@` = the skill's power; `@{m.def}`, `@{max hp}`, `@{duration}` = a named number (any word
from `DESCR-KEYS.md`); `[ … ]` = shown only at levels that have it. Write the TOP level's text — a clause whose number a
lower level does not have drops out by itself (Harmony of Protection at 44 reads "+30% M.Def."). After editing run
`dotnet run --project tools/SkillCsvSeed -- --gen-faces`; `--check` reports a bad word, race, class or a stale file.

The seed: 767 skills, actives first. 144 cells are EMPTY on purpose (the game's own per-level text shows until you write
one — their COMMENT shows the top level's wording); a COMMENT with "typed numbers kept" means a number I could not tie to
the skill's data, so it is fixed text.

- `112a` [ ] - **Elf / Demon healer:** the bar, the skill window and the Learn list say **Moonlight / Spirit Bolt** (they
  said Holy Bolt), the cast bar says the same, and the bar square lights up while you cast it (it did not before). ->
- `112b` [ ] - **Mage at 7 learns ONE Might**, named for your race (Forest Might / Demonic Strength / Blessing of Might);
  a cleric at 20 continues the same skill at rung 2 under the same name. ->
- `112c` [ ] - **An Elf buffs a Human:** the Human's buff bar reads **Forest Might** with the Elf's text — and still does
  after the Human relogs. ->
- `112d` [ ] - **Harmony of Protection** tooltip at 44 / 56 / 76 shows only what that level gives. ->
- `112e` [ ] - Rogue 2nd's "Critical Damage" is now **Critical Damage Mastery** (the CSV NAME is a label now, the face is
  the name). Add a class row if you want the short name back. ->

---

## §111 — 0.216.0: SP is one pot per level, split by weight (`BL-326`, 2026-09-29)

⚠ **New APK** (the Learn tab reads prices compiled into the client). No `game.db` delete needed.

Your rule: each level's SP (the sum of every skill that opens there) is split by weight — passive pieces 0.33, utility
and buffs 1, strikes/debuffs/heals/traps 1.5. The weights are **yours** in `docs/data/sp_weights.csv` (one row per
skill; your archer example is marked `owner`, the rest `default`). Edit a WEIGHT, then run
`dotnet run --project tools/SkillCsvSeed -- --reweigh-sp` — it rewrites the SP cells and regenerates. Every weight at 1 =
"the sum divided by the count". The passive ×k is gone; each level costs what it cost before (affordability unchanged,
except Elf Ravager 0.72→0.78 and Magus 0.60→0.57-0.63, whose three races' kits differ).

- `111a` [ ] - **Archer at 60**: Light Armor / Bow / Crit Damage Mastery 94.7k each, Critical Resist, Signal Flare and
  Bow Stance 287k, Twin Arrows / Explosive Arrow / your trap / your Magic Arrow 431k. Is that the shape you wanted? ->
- `111b` [ ] - **Skim `sp_weights.csv`** (318 skills): anything at the wrong weight? A passive you rate higher (like
  Critical Resist at 1), or an active that is only utility? ->
- `111c` [x] - **In game, the Learn tab** shows the new prices at 20-75 (passives far cheaper, actives dearer). -> passed (your [x] on the version in the 2026-10-01 note)

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
