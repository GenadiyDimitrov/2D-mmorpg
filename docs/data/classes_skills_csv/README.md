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
  ✅ **RETIRED IN CODE 2026-08-28** (0.96.0, `BL-97`). It was an empty class that could still be
  chosen — a level-40 Knight could pick a discipline that taught nothing. Not any more.
- **`Magus` and `Tempest` merged into `nuker`.** These two carried the only substantial 40+ kit in
  the game outside the buffer's ladder, and **no seeded file had ever covered them** — `nuker 3rd`
  is 20 rows, and this is the first time you can see them in your own format. Because it is two kits
  folded into one, **two skills appear twice under different names**: `FlameBolt` as *Annihilate*
  (Magus) and *Chain Lightning* (Tempest), `GreaterWeakness` as *Mana Burn* and *Maelstrom*.
  Reconciling those two pairs is yours — they are the same underlying skill.
  ✅ **DONE IN CODE 2026-08-28** (0.96.0, `BL-97`, *"Tempests must go"*): the nuker is one discipline
  now, so those duplicate pairs are gone and the file needs no reconciling.

✅ **The `Discipline` enum HAS collapsed — both of them** (2026-08-28). `Tempest` and `Vanguard` keep
their enum VALUES — characters persisted them, so the numbers can never be reused — but nothing
mints, names, offers or teaches either any more, and an old save is migrated to the surviving sibling
on load. **Eight choosable paths per race, 24 third classes** — the roster this map drew.

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

### The `WEAPON` column (2026-08-29, `BL-105`)

Which weapon a skill or passive **demands**. Before this it was written only in the free-text DESCR,
where `--check` could not compare it — which is how the elf's Combo Mastery ran for weeks gated to a
weapon he never holds. Your grammar:

```
weaponType1[|weaponType2|weaponType3][/hands]
```

| cell | means |
| --- | --- |
| *(empty)* | no weapon requirement |
| `sword\|blunt\|bow` | any sword, or any blunt, or a bow |
| `sword\|blunt\|bow/1` | 1-handed sword, or 1-handed blunt, **or a bow** |
| `blunt` | any blunt — mace, maul, staff, wand |
| `blunt/2` | 2-handed blunt — staff or maul |
| `duals` | daggers only |

🔑 **`/1` and `/2` narrow the TYPES, not the weapon in your hands.** Bow and dual are inherently
two-handed and have no 1H variant, so the hands token passes straight over them — which is why
`sword|blunt|bow/1` still includes a bow.

- `duals/1` parses as `duals` and prints a **⚠ typo-warning** — legal, but the token does nothing.
- Anything but `/1` or `/2` — `/`, `/3`, `/a` — is an **🔴 error**, and the hands become invalid (the
  types still count, the narrowing is dropped).
- Order and case do not matter: `blunt|sword/1` == `SWORD | BLUNT/1`.

`--check` verifies every authored cell against the game. The column is generated once by
`dotnet run --project tools/SkillCsvSeed -- --weapon-column`; after that it is yours, and `--check` is
what keeps it honest.

### The format

The 2nd-class header **plus a trailing `RACE` column** — `human` / `elf` / `demon`, or blank for all
three. A skill for two races gets two rows. Everything else behaves as in the 2nd-class files,
`REPLACES` included. A skill with more than one rung is written as one row per rung, named `… L2`,
`… L3`.

**`MP` is ONE column, and it is the skill's WHOLE price** (2026-08-20). It used to be two — `INIT MP`
paid on cast, `FINIT MP` on landing — and every file was collapsed to their sum on that day, his call:
*"i can sum the IG values and we just split it in the code as 20/80"*. The sheets had never settled on
a ratio (20/80 on a heal, the full cost up front on a physical strike) because the ratio was never a
design decision. The engine now splits **every** skill 20/80 itself, so what you write here is what the
player is quoted and what the cast gate demands in full before the cast will start.

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

| what           | where                                                                                                                                                                                                                     |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **level**      | 76 (`FourthClassCatalog.ChangeLevel`)                                                                                                                                                                                     |
| **the key**    | **Rite of Ascension**, 100,000,000 gold, sold at **every Apothecary**, untradeable                                                                                                                                        |
| **the master** | **Archmaster Sevrin**, `master_class4`, west side of **Frostmere** — the last town on the level path, so the only one whose fields reach 76                                                                               |
| **quest**      | none yet, by your instruction. The long chain replaces the *purchase*, not the item: when it lands, the Rite comes off the shelf and becomes the chain's reward, and the class-change requirement itself does not change. |

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

The old tree ended on a 🔴 asking whether a Human tank and an Demon tank should read as different
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

⚠ **`Warlord` is retired as a NAME.** It was the war_aoe discipline's label, and it is a class name in
IG — the same rule that took the old town names and the old currency term. (`Discipline.Warlord`
survives as the internal enum value; nothing shows it.) Its three races are **Vanguard / Skirmisher /
Warborn** now.

✅ **And the 2nd classes were renamed too, 2026-08-28 (`BL-100`)** — the note that used to sit here
said `Sorcerer` was the same kind of slip and was NOT fixed here. It is fixed: every 2nd class is
race + role now (`Human Apprentice`), because nothing differs before 40 and a flavour name there was
promising an identity the game does not deliver.

## Classes and Races

### Races
  1. Humans
  2. Elves
  3. Demons

### Stats   
#### Fighters  
|       | Human |  Elf  | Demon |
| :---: | :---: | :---: | :---: |
|  ATK  |  40   |  36   |  41   |
|  CON  |  43   |  39   |  46   |
|  AGI  |  30   |  36   |  29   |
|  SPT  |  26   |  25   |  27   |
|  WIT  |  14   |  17   |  10   |
|  Sum  |  153  |  153  |  153  |
#### Mages
|       | Human |  Elf  | Demon |
| :---: | :---: | :---: | :---: |
|  ATK  |  41   |  37   |  42   |
|  CON  |  29   |  28   |  31   |
|  AGI  |  26   |  29   |  20   |
|  SPT  |  37   |  36   |  41   |
|  WIT  |  20   |  23   |  19   |
|  Sum  |  153  |  153  |  153  |

🔑 **Every column sums to 153** (owner, 2026-08-28). A race is a REDISTRIBUTION of the same points,
never a bigger pile — so a change to one cell must come out of another cell in the SAME column. This
drifted unnoticed for weeks before the rule was written down: the live columns had reached 153/153/150
and 148/141/162, leaving the elf mage 21 points behind the demon mage. The server now refuses to boot
if any column is off (`StatCalculator.BaseStatsNotSummingTo153`).

⚠ **This table IS the code.** It mirrors `StatCalculator.GetBaseStats` in `Game.Shared`, the same way
the skill CSVs mirror the catalogs — edit one, edit the other, in the same commit.

### Classes
| 1st     | 2nd        | 3rd              | 4th              | Race  | Weapon                      | Armor               | Path         |
| ------- | ---------- | ---------------- | ---------------- | ----- | --------------------------- | ------------------- | ------------ |
| Fighter |            |                  |                  |       | Sword/Blunt/Dual/Bow - 1/2h | Robe/Light/Heavy    | -            |
|         | Rogue      |                  |                  |       | Dual/Bow                    | Light               | -            |
|         |            | Assassin         | Nullblade        | Human | Dual                        | Light               | Mele Burst   |
|         |            | Phantom          | Shadowblade      | Elf   | Dual                        | Light               | Mele Burst   |
|         |            | Stalker          | Venomblade       | Demon | Dual                        | Light               | Mele Burst   |
|         |            | Sharpshooter     | Deadeye          | Human | Bow                         | Light               | Range Dmg    |
|         |            | Sentinel         | Trapper          | Elf   | Bow                         | Light               | Range Dmg    |
|         |            | Soultracker      | Soulhunter       | Demon | Bow                         | Light               | Range Dmg    |
|         | Warrior    |                  |                  |       | Sword/Blunt - 2h            | Heavy               | -            |
|         |            | Champion         | Sword Master     | Human | Sword - 2h                  | Heavy               | Mele Dmg     |
|         |            | Swiftblade       | Sword Saint      | Elf   | Sword - 2h                  | Heavy               | Mele Dmg     |
|         |            | Ravager          | Berserker        | Demon | Sword - 2h                  | Heavy               | Mele Dmg     |
|         |            | Vanguard         | War Master       | Human | Blunt - 2h                  | Heavy               | Mele AOE Dmg |
|         |            | Skirmisher       | War Storm        | Elf   | Blunt - 2h                  | Heavy               | Mele AOE Dmg |
|         |            | Warborn          | Warbringer       | Demon | Blunt - 2h                  | Heavy               | Mele AOE Dmg |
|         | Knight     |                  |                  |       | Sword/Blunt - 1h            | Heavy + Shield      | -            |
|         |            | Iron Guard       | Knight Commander | Human | Sword/Blunt - 1h            | Heavy + Shield      | Defence      |
|         |            | Templar          | Paladin          | Elf   | Sword/Blunt - 1h            | Heavy + Shield      | Defence      |
|         |            | Dread Knight     | Abyssal Knight   | Demon | Sword/Blunt - 1h            | Heavy + Shield      | Defence      |
| Mage    |            |                  |                  |       | Wand/Staff                  | Robe [+ Shield]     | -            |
|         | Priest     |                  |                  |       | Wand/Staff                  | Robe [+ Shield]     | -            |
|         |            | Holy Priest      | Holy Messenger   | Human | Wand/Staff                  | Robe [+ Shield]     | Heal         |
|         |            | Forest Whisperer | Forest Elder     | Elf   | Wand/Staff                  | Robe [+ Shield]     | Heal         |
|         |            | Dark Healer      | Occultist        | Demon | Wand/Staff                  | Robe [+ Shield]     | Heal         |
|         |            | Doctor           | War Doctor       | Human | Sword/Blunt/Wand - 1h       | Heavy/Robe + Shield | Buffer       |
|         |            | Harmonist        | War Harmonist    | Elf   | Bow/Wand/Staff              | Light/Robe          | Buffer       |
|         |            | Dreadcaller      | Warlock          | Demon | Sword/Blunt - 2h            | Heavy/Robe          | Buffer       |
|         | Apprentice |                  |                  |       | Wand/Staff                  | Robe [+ Shield]     | -            |
|         |            | Mana Adept       | Arcane Master    | Human | Wand/Staff                  | Robe [+ Shield]     | Nuke         |
|         |            | Water Adept      | Ice Master       | Elf   | Wand/Staff                  | Robe [+ Shield]     | Nuke         |
|         |            | Fire Adept       | Inferno Master   | Demon | Wand/Staff                  | Robe [+ Shield]     | Nuke         |



⚠ **The 2nd-class column is the SHORT form.** In game a 2nd class wears its race — `Human Rogue`,
`Elf Warrior`, `Demon Apprentice` — because that is the whole point of the `BL-100` rename. The Race
column on the 3rd/4th rows is what tells you which of the three you are reading.

🔑 **The WEAPON, ARMOR and PATH columns are information the CODE does not hold yet**, and they are the
most useful thing in this table. Two notes on where they stand:

- **The three buffer rows are already true** and match the built kit exactly — Human takes Heavy
  (`Chanter Heavy Mastery`), Elf takes Light + Bow (`Harmonist Bow/Light`), Demon takes 2-handed
  blunt (`Bloodchanter Two-Hand Mastery`). Nothing to do.
- ✅ **The warrior SWORD-vs-BLUNT split IS A RULE — you ruled it 2026-08-29.** *"The aoe warriors to
  be a 2h blunt while mele warriors to use 2h swords."* It is **not enforced yet and that is correct**:
  there is still no warrior 3rd-class kit, so there is no passive to gate. It is carried as `BL-104`
  and applied the day `warrior 3rd.csv` / `war_aoe 3rd.csv` land.

✅ **THE CODE HOLDS THE WEAPON COLUMN NOW — both halves of it** (0.101.0). It used to hold only the
type, because a requirement could say *"two-handed"* but never *"one-handed"*: a bare `Sword|Blunt`
means **any hands of it** (playtest 28), so a maul passed a mask meant to read 1H. Hands are their own
axis now — `WeaponHands` (`Any`/`One`/`Two`) beside the type mask — and your three 2026-08-29 rulings
are live: **Knight** = 1H sword/blunt (was *"any weapon"*), **Demon buffer** = 2H blunt, **Human
buffer** = 1H blunt via his own Shield Mastery, no gate needed. The shared Spell Mastery stays
blunt-or-bow at **any** hands, per *"they share one so we gate only the type"*. Wrong weapon costs you
the bonus and nothing else — there is no penalty, by your ruling.

---
## What each race's three names are trying to say

Not a rule the code enforces — a tone, so a new row has somewhere obvious to sit:

| race      | the register                                | the tank line, worked out                    |
| --------- | ------------------------------------------- | -------------------------------------------- |
| **Human** | martial, ordered, heraldic — rank and oath  | Human Knight → Iron Guard → Knight Commander |
| **Elf**   | light, wind, growth — precision and grace   | Elf Knight → Templar → Paladin               |
| **Demon** | dread, blood, the abyss — ruin and appetite | Demon Knight → Dread Knight → Abyssal Knight |

⚠ **The Demon register changed with the race** (2026-08-28, `BL-101`). It used to be *bone, blood,
endurance* — an ork's register — and that is exactly why the mage lines never worked: an ork shaman
was a shrug. Dread and the abyss give `Dreadcaller`, `Occultist` and `Warlock` somewhere to stand.

🔑 **Three sets run ACROSS the races, and a new name should join one of them:**

| the set                          | Human                   | Elf                       | Demon                |
| -------------------------------- | ----------------------- | ------------------------- | -------------------- |
| nuker — the element growing up   | Mana → Arcane           | Water → Ice               | Fire → Inferno       |
| war_aoe 3rd — a martial POSITION | Vanguard                | Skirmisher                | Warborn              |
| AoE/support 4th — a **War** word | War Master · War Doctor | War Storm · War Harmonist | Warbringer · Warlock |

## The orphans from the old map — both gone

✅ **`Tempest` is RETIRED (2026-08-28, 0.96.0, `BL-97`).** Your ruling: *"Tempests must go .. And elf
nuker 3rd is starweaver, ork is cinderwitch and human stays magus"*. The nuker archetype now opens
into **one** discipline and the three identities are the three RACES — exactly the shape of your
2026-08-17 map (*"1 discipline ... 3 identities"*). The names needed no work: they had read Magus /
Starweaver / Cinderwitch since the per-race naming pass.

🔑 **It cost no authored content.** `nuker 3rd.csv` has no discipline column, so its 208 rows were
registered to Magus and Tempest identically; retiring one deleted a duplicate registration. The enum
VALUE stays (characters persisted it, so 11 can never be reused), nothing offers it, and a character
saved on a Tempest becomes that race's Magus the next time it loads.

✅ **`Vanguard` (the off-tank) is RETIRED TOO**, hours after the Tempest and on the same ruling:
*"Remove the vacant tank as well — the 3 tanks must have their name and the other is the same for the
3 races ... So is the one that must go."* It had held no learn rows since the 2026-08-10 purge, so it
was an **empty class a level-40 Knight could still pick**. Gone.

🔑 **THE TEST IN THAT SENTENCE IS THE PART TO REMEMBER.** A discipline whose NAME is the same for all
three races was never three classes — and both retired ones were exactly that (Vanguard → Doomward,
Tempest → Skybreaker). The naming table had been recording the answer since 2026-08-17.

⚠ **A retired discipline's name is free to reuse.** A name is not an id and nothing persists one —
which is what lets `Vanguard` come back as the human warrior's AoE class in the 2026-08-28 rename.

## Columns — and why RANGE and AOE are two of them (`BL-96`, 2026-08-28)

`LEARN @ LVL, NAME, TYPE, RANGE, AOE, TARGET, CAST s, CD s, DURRATION s, DESCR, MP, SP COST, …`

- **RANGE** — how far you may THROW it. The distance to the target the cast gate checks at cast start.
- **AOE** — how WIDE it goes off. 0 = single target.
- **TARGET** — `[self|target|party|enemy]/[single|aoe]`: who it affects and how many.

Owner, 2026-08-28: *"the column range is spell range (what distance form target) while description
should say the AOE range for the actual effect ... Or we should add another column aoe range after
every range column"*, and the worked example: *"Elemental wave - 200 AOE around caster 0 cast range …
if we have the two range columns then they will be 0,200"*.

🔑 **Why it was worth a schema change.** RANGE used to mean two different things depending on the
skill — "how far can I throw this" for a nuke, "how wide does this go off" for a party heal — so it
could not be compared against any single code field, and the radius was never a CHECKED number, only
prose in DESCR. It is checked now, against `SkillDef.AreaRadiusAt`.

It also settles a contradiction. On 2026-08-27 the TARGET column was ruled to deliberately NOT say
where the circle sits; on 2026-08-28 Elemental Wave was described as `self/aoe`, which says exactly
that. With two columns neither has to carry the other's meaning: Elemental Wave is `0,200,enemy/aoe`
(erupts around you) and Arcane Wave is `900,400,enemy/aoe` (thrown, detonates on the target).

⚠ **A party heal reads `600,600`, not `0,600`.** The 0-range reading was proposed but the range gate
really does apply to the ally you target — `SkillMath.EffectiveRange` is checked at cast start for any
non-self target — so 600 is what the game does. Changing that is a behaviour decision, not a column one.

⚠ Regenerated by `dotnet run --project tools/SkillCsvSeed -- --aoe-column`, which is a **one-off
migration**: it refuses to run on a file that already has the column. `--check` keeps it honest after.
