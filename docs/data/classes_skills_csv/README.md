# The class skill CSVs — what each file is, and which of them are yours

**These files are AUTHORITATIVE.** Nothing in the repo retunes them; the code reads them, never the
other way round. Where there is no CSV, nothing is invented (`BL-02`).

## The names are CLASS TIERS, not level bands (2026-08-17)

Your call: *"well for fighters 20-35 is not right .. they have skills at 36 .. so 2nd class is more
suited and understandable"*. A band in a filename is a claim about the content, and it was already
false. So every file is now `<name> <tier>`:

| tier  | what it is              | files                                             |
| ----- | ----------------------- | ------------------------------------------------- |
| `1st` | base class              | `fighter` · `mage`                                |
| `2nd` | the five 2nd classes    | `tank` · `warrior` · `rogue` · `nuker` · `cleric` |
| `3rd` | the discipline, from 40 | the eight below                                   |
| `4th` | the discipline, from 76 | the same eight                                    |

⚠ **Nothing but the names changed.** No row was added, removed or edited by the rename. The old
names, for reading old commits: `fighter/mage 01-15` → `1st`, `… 20-35` → `2nd`, `… 40-74` → `3rd`,
`… 76-85` → `4th`, and `melee rogue` → `dual`.

## The discipline map (yours, 2026-08-17)

*"class 2nd => desc1/desc2 3rd => desc1/desc2 4th"* —

| 2nd class | 3rd/4th disciplines       | your note                                                                                                                           |
| --------- | ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `cleric`  | **buffer** / **healer**   |                                                                                                                                     |
| `rogue`   | **archer** / **dual**     | *"same logic as warrior — one is range the other mele, diferent kits"*                                                              |
| `warrior` | **warrior** / **war_aoe** | *"one is aoe and more tanky .. the other is like a mele nuker"*                                                                     |
| `tank`    | **tank**                  | *"one tank is enough — wont have the vanguard the off tank, we have a warrior for that — the varity will come from race diference"* |
| `nuker`   | **nuker**                 | *"same logic as the tank, 1 discipline ... 3 identities"*                                                                           |

**Eight disciplines, sixteen 40+ files.** The three identities inside a one-discipline file are the
**RACE** column — that is the whole point of it, and it is why the plan is 16 files and not 48.

### Two things the map changed, and what happened to their content

- **`Vanguard` is gone** (the off-tank). Nothing was lost: the 2026-08-10 purge had already removed
  every Vanguard learn line, so `tank 3rd/4th` seeds from Bulwark alone.
- **`Magus` and `Tempest` merged into `nuker`.** These two carried the only substantial 40+ kit in
  the game outside the buffer's ladder, and **no seeded file had ever covered them** — `nuker 3rd`
  is 20 rows, and this is the first time you can see them in your own format. Because it is two kits
  folded into one, **two skills appear twice under different names**: `FlameBolt` as *Annihilate*
  (Magus) and *Chain Lightning* (Tempest), `GreaterWeakness` as *Mana Burn* and *Maelstrom*.
  Reconciling those two pairs is yours — they are the same underlying skill.

⚠ **The `Discipline` enum in code is NOT collapsed yet.** Its values persist on characters, so the
merge happens when the authored kits arrive, not because a file was renamed.

## What is actually in the sixteen 40+ files

| file               | rows | what                                                                                                                                                         |
| ------------------ | ---- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `nuker 3rd`        | 20   | Elemental Burst L1-L10 (40→75), Frost Bind, Entangling Roots, Glacial Spike, Creeping Frost, Phase Shift, Mana Barrier, + the four duplicate-name rows above |
| `buffer 3rd`       | 39   | the whole buff ladder — singles topped out, three Harmonies, five improved groups                                                                            |
| `dual 3rd`         | 2    | Prowl 40, Vanish 60                                                                                                                                          |
| `archer 3rd`       | 1    | Signal Flare 60                                                                                                                                              |
| `buffer 4th`       | 1    | Madness 76                                                                                                                                                   |
| `healer 4th`       | 1    | Rite of Preservation 83                                                                                                                                      |
| `tank 4th`         | 1    | Undying Will 83                                                                                                                                              |
| the other **nine** | 0    | `tank 3rd` · `warrior 3rd/4th` · `war_aoe 3rd/4th` · `dual 4th` · `archer 4th` · `healer 3rd` · `nuker 4th`                                                  |

**Nine empty files is the honest picture, not an oversight.** It is what the game registers above 40.

### The format

The 2nd-class header **plus a trailing `RACE` column** — `human` / `elf` / `ork`, or blank for all
three. A skill for two races gets two rows. Everything else behaves as in the 2nd-class files,
`REPLACES` included. A skill with more than one rung is written as one row per rung, named `… L2`,
`… L3`.

### ⚠ Two things about the seeded content

- **It is what the game ALREADY registers above 40, not a proposed kit.** Nothing was invented to
  fill these in.
- **The rows are your starting point, not a decision.** `Vanish` in particular ships with an **SP
  cost of 1** — the record default — because pricing a 40+ skill is 40+ balance and therefore yours.
  It is visible in the file for exactly that reason.

### Regenerating

`tools/SkillCsvSeed` writes the 40+ files from the compiled class tables. **It refuses to overwrite
an existing file**, so running it again is safe and does nothing — the moment you edit one it is
yours, and the tool will not touch it. (There is a force switch. Do not use it.)

⚠ The old band suffixes left **level 75 in neither file**, which silently dropped Elemental Burst's
10th rung. `3rd` now closes at 75; that rung is the last row of `nuker 3rd`.

## The seven authored files (yours)

`fighter 1st` · `mage 1st` · `tank 2nd` · `warrior 2nd` · `rogue 2nd` · `nuker 2nd` · `cleric 2nd`.
`rogue 2nd` covers **both** the dagger and the bow to level 40 (the archer merge), which is why there
is no separate archer file below 40. `cleric 2nd` was `healer 20-35` until 2026-08-17 — **cleric** is
the 2nd class, **healer** is one of the two disciplines above it, so both names are correct.


# Classes Tree

Every node below is what the game **registers today**, not a proposal. Tiers are
`1st` 1-19 · `2nd` 20-39 · `3rd` 40-75 · `4th` 76+, and the file each node reads is in `code`.

⚠ **No 4th class exists in any form** — no name, no id, no registered skill, and
`ThirdClassCatalog.ChangeLevel = 40` is the last class change the code knows about. The `4th` rows are
listed so the tree is complete and so the sixteen `4th` CSVs have somewhere to point.
🔑 One thing is already waiting on it: **`Crafting.RequireFourthClassForL5`** — your gate is *"L5,6
needs 76 (4th class)"*, and until a 4th class exists level 76 opens L5/L6 on its own. That flag is the
one line to flip the day it lands.

## Human

### Fighter — 1st, 1-19 · `fighter 1st`

1. **Assassin** (rogue) 2nd · `rogue 2nd`
   1. **Nullblade** (dual) 3rd · `dual 3rd` → *unnamed* 4th · `dual 4th`
   2. **Sharpshooter** (archer) 3rd · `archer 3rd` → *unnamed* 4th · `archer 4th`
2. **Champion** (warrior) 2nd · `warrior 2nd`
   1. **Ravager** (warrior) 3rd · `warrior 3rd` → *unnamed* 4th · `warrior 4th`
   2. **Warlord** (war_aoe) 3rd · `war_aoe 3rd` → *unnamed* 4th · `war_aoe 4th`
3. **Knight** (tank) 2nd · `tank 2nd`
   1. **Bulwark** (tank) 3rd · `tank 3rd` → *unnamed* 4th · `tank 4th`

### Mage — 1st, 1-19 · `mage 1st`

1. **Sorcerer** (nuker) 2nd · `nuker 2nd`
   1. **Magus** (nuker) 3rd · `nuker 3rd` → *unnamed* 4th · `nuker 4th`
2. **Cleric** (cleric) 2nd · `cleric 2nd`
   1. **Lightbringer** (healer) 3rd · `healer 3rd` → *unnamed* 4th · `healer 4th`
   2. **Warchanter** (buffer) 3rd · `buffer 3rd` → *unnamed* 4th · `buffer 4th`

## Elf

### Fighter — 1st

1. **Shadowblade** (rogue) 2nd
   1. **Phantom** (dual) 3rd → *unnamed* 4th
   2. **Trapper** (archer) 3rd → *unnamed* 4th
2. **Sentinel** (warrior) 2nd
   1. **Ravager** (warrior) 3rd → *unnamed* 4th
   2. **Warlord** (war_aoe) 3rd → *unnamed* 4th
3. **Templar** (tank) 2nd
   1. **Bulwark** (tank) 3rd → *unnamed* 4th

### Mage — 1st

1. **Inquisitor** (nuker) 2nd
   1. **Magus** (nuker) 3rd → *unnamed* 4th
2. **Priest** (cleric) 2nd
   1. **Lightbringer** (healer) 3rd → *unnamed* 4th
   2. **Warchanter** (buffer) 3rd → *unnamed* 4th

## Ork

### Fighter — 1st

1. **Stalker** (rogue) 2nd
   1. **Venomweaver** (dual) 3rd → *unnamed* 4th
   2. **Hunter** (archer) 3rd → *unnamed* 4th
2. **Warrior** (warrior) 2nd
   1. **Ravager** (warrior) 3rd → *unnamed* 4th
   2. **Warlord** (war_aoe) 3rd → *unnamed* 4th
3. **Beast** (tank) 2nd
   1. **Bulwark** (tank) 3rd → *unnamed* 4th

### Mage — 1st

1. **Witch** (nuker) 2nd
   1. **Magus** (nuker) 3rd → *unnamed* 4th
2. **Shaman** (cleric) 2nd
   1. **Lightbringer** (healer) 3rd → *unnamed* 4th
   2. **Warchanter** (buffer) 3rd → *unnamed* 4th

## 🔴 The thing to decide: only the ROGUE line has a name per race

Read the three trees side by side and the gap is obvious. The rogue's two branches are named
**per race** — Nullblade/Phantom/Venomweaver and Sharpshooter/Trapper/Hunter, six names — because the
archer merge split that line by race in 2026-07-29. **Every other 3rd class carries the same name for
all three races**: an Ork Beast, an Elf Templar and a Human Knight all become a **Bulwark**.

That sits awkwardly next to your own ruling for the new map — *"the varity will come from race
diference"*. The KIT can already differ per race (the `RACE` column in the 3rd/4th CSVs is exactly
that), but the NAME cannot: `ThirdClassDef.Name` is just `Discipline.ToString()`, one string per
discipline. If a Human tank and an Ork tank should read as different classes, that is **12 names to
invent** (6 shared disciplines × 3 races, minus the 6 that exist) — and it is a naming decision, so it
is yours. Nothing in the code blocks it; `ThirdClassCatalog.Build` would take a per-race name table.

## Two orphans from the old map

`Discipline` still has **`Vanguard`** (the off-tank) and **`Tempest`** (the AoE nuker), which your
2026-08-17 map drops and merges. They are left in the enum on purpose — the values persist on
characters — and they hold no learn rows, so nothing reaches them. The `nuker 3rd.csv` rows carrying
*Chain Lightning* and *Maelstrom* are Tempest's, folded in.
