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

## The 4th class exists now (2026-08-17)

It was the one hole in this tree — *"no name, no id, no registered skill"* — and it is closed.
Your call: *"lets add 4th classes as available option (now can be without quest but go in the
apothecary and buy a 100kk 4th_class_item and go to class master with it … then we add additional
long quest)"*. So:

| what | where |
| ---- | ----- |
| **level** | 76 (`FourthClassCatalog.ChangeLevel`) |
| **the key** | **Rite of Ascension**, 100,000,000 gold, sold at **every Apothecary**, untradeable |
| **the master** | **Archmaster Sevrin**, `master_class4`, west side of **Frostmere** — the last town on the level path, so the only one whose fields reach 76 |
| **quest** | none yet, by your instruction. The long chain replaces the *purchase*, not the item: when it lands, the Rite comes off the shelf and becomes the chain's reward, and the class-change requirement itself does not change. |

🔑 **A 4th class does not branch.** It is the *same discipline awakened* — one 3rd class has exactly
one ascension, so there is nothing to pick. That is why it carries no enum of its own and why the
debug panel offers a toggle rather than a list.

⚠ **It grants NO skills yet, on purpose.** `ClassKey` has no tier component, so registering a 4th
kit against the discipline would leak it to every level-40; and the standing 40+ rule is *"anything
that's not inside the csv should not exist"*. The kit lands with your `*.4th.csv` files (`BL-02`).
Until then the ascension buys you **the name** and **the L5/L6 crafting band** — and nothing else.

🔴 **`Crafting.RequireFourthClassForL5` was flipped to `true`.** Your gate is *"L5,6 needs 76 (4th
class)"*, and it sat at `false` only because no 4th class existed. It does now. **This is a real
change for anyone already at 76: the top two crafting rungs now cost the 100kk Rite.** One `const`
in `Crafting.cs` puts it back if you'd rather L5/L6 stayed on level alone.

## The names are PER RACE now (2026-08-17)

The old tree ended on a 🔴 asking whether a Human tank and an Ork tank should read as different
classes. Answer: **yes, at both tiers.** It is what *"the varity will come from race diference"*
already meant everywhere else — the kit could differ per race (the trailing `RACE` column), but the
LABEL could not, so the variety was invisible.

The whole table is **`Game.Shared/Classes.Names.cs`**, one row per (discipline, race). Two things
worth knowing before you edit it:

- **Names are free to change.** Nothing persists a class name — a character stores the numeric id
  (101-136 for 3rd, 201-236 for 4th). Retune any string in that file and no save breaks. The **ids**
  are the things that must never move.
- **A boot-time guard rejects duplicates.** The class-change NPC lists what you may become *by
  name*, so two classes sharing one would make two different changes indistinguishable. With 48
  hand-written strings that is the likeliest typo there is, so the server refuses to start.

⚠ **`Warlord` is retired.** It was the war_aoe discipline's name, and it is a class name in IG — the
same rule that took the old town names and the old currency term. Its three races are **Banneret /
Galeherald / Skullbreaker** now. (`Sorcerer`, the Human nuker's 2nd class, is the same kind of slip
and is *not* fixed here — renaming a 2nd class is your call and it is not what you asked for.)

## Human

### Fighter — 1st, 1-19 · `fighter 1st`

1. **Assassin** (rogue) 2nd · `rogue 2nd`
   1. **Nullblade** (dual) 3rd · `dual 3rd` → **Hexbane** 4th · `dual 4th`
   2. **Sharpshooter** (archer) 3rd · `archer 3rd` → **Deadeye** 4th · `archer 4th`
2. **Champion** (warrior) 2nd · `warrior 2nd`
   1. **Bladesworn** (warrior) 3rd · `warrior 3rd` → **Bladelord** 4th · `warrior 4th`
   2. **Banneret** (war_aoe) 3rd · `war_aoe 3rd` → **Warmarshal** 4th · `war_aoe 4th`
3. **Knight** (tank) 2nd · `tank 2nd`
   1. **Bulwark** (tank) 3rd · `tank 3rd` → **Ironcrown** 4th · `tank 4th`

### Mage — 1st, 1-19 · `mage 1st`

1. **Sorcerer** (nuker) 2nd · `nuker 2nd`
   1. **Magus** (nuker) 3rd · `nuker 3rd` → **Runelord** 4th · `nuker 4th`
2. **Cleric** (cleric) 2nd · `cleric 2nd`
   1. **Lightbringer** (healer) 3rd · `healer 3rd` → **Lifewarden** 4th · `healer 4th`
   2. **Warchanter** (buffer) 3rd · `buffer 3rd` → **Oathkeeper** 4th · `buffer 4th`

## Elf

### Fighter — 1st

1. **Shadowblade** (rogue) 2nd
   1. **Phantom** (dual) 3rd → **Nightveil** 4th
   2. **Trapper** (archer) 3rd → **Bramblewarden** 4th
2. **Sentinel** (warrior) 2nd
   1. **Thornblade** (warrior) 3rd → **Windreaver** 4th
   2. **Galeherald** (war_aoe) 3rd → **Stormcrown** 4th
3. **Templar** (tank) 2nd
   1. **Aegis** (tank) 3rd → **Dawnshield** 4th

### Mage — 1st

1. **Inquisitor** (nuker) 2nd
   1. **Starweaver** (nuker) 3rd → **Celestine** 4th
2. **Priest** (cleric) 2nd
   1. **Dawnsworn** (healer) 3rd → **Everdawn** 4th
   2. **Harmonist** (buffer) 3rd → **Gracebinder** 4th

## Ork

### Fighter — 1st

1. **Stalker** (rogue) 2nd
   1. **Venomweaver** (dual) 3rd → **Plaguefang** 4th
   2. **Hunter** (archer) 3rd → **Bloodhunter** 4th
2. **Warrior** (warrior) 2nd
   1. **Ravager** (warrior) 3rd → **Bloodrager** 4th
   2. **Skullbreaker** (war_aoe) 3rd → **Bonecrusher** 4th
3. **Beast** (tank) 2nd
   1. **Ironhide** (tank) 3rd → **Stonemaw** 4th

### Mage — 1st

1. **Witch** (nuker) 2nd
   1. **Cinderwitch** (nuker) 3rd → **Pyrelord** 4th
2. **Shaman** (cleric) 2nd
   1. **Bonemender** (healer) 3rd → **Spiritbinder** 4th
   2. **Bloodchanter** (buffer) 3rd → **Totemlord** 4th

## What each race's three names are trying to say

Not a rule the code enforces — a tone, so a new row has somewhere obvious to sit:

| race | the register | worked example |
| ---- | ------------ | -------------- |
| **Human** | martial, ordered, heraldic — rank and oath | Knight → Bulwark → Ironcrown |
| **Elf** | light, wind, growth — precision and grace | Templar → Aegis → Dawnshield |
| **Ork** | bone, blood, endurance — outlast and break | Beast → Ironhide → Stonemaw |

Two disciplines changed which race owns their old name: **Ravager** moved to the **Ork** (it always
read as the ork's word) and the Human took **Bladesworn**; **Magus** stayed **Human** and the other
two races got names of their own. Nothing else was re-pointed.

## Two orphans from the old map

`Discipline` still has **`Vanguard`** (the off-tank) and **`Tempest`** (the AoE nuker), which your
2026-08-17 map drops and merges. They are left in the enum on purpose — the values persist on
characters — and they hold no learn rows, so no SKILL reaches them.

⚠ But they are still **selectable**: `Disciplines.Of` returns two disciplines for every archetype, so
a level-40 Knight is still offered `Vanguard` and a level-40 Sorcerer still offered `Tempest` at the
Grandmaster. Collapsing that is a code change with persisted ids in it, not a rename, which is why
the file rename did not do it. Until then the two carry **one name for all three races** (Vanguard →
Doomward, Tempest → Skybreaker) rather than six invented names for two classes on their way out.

The `nuker 3rd.csv` rows carrying *Chain Lightning* and *Maelstrom* are Tempest's, folded in.
