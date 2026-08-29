# `DESCR` — every word the checker understands

**GENERATED — do not edit by hand.** `dotnet run --project tools/SkillCsvSeed -- --descr-keys`
regenerates it from the alias table in `tools/SkillCsvSeed/Descr.cs`, which is the same table
`--check` reads your rows with. If a word is not on this page, a number next to it comes back
`UNREAD` — not wrong, just unverified.

Keep writing them the way you write them now. Every spelling in the right-hand column is
already understood, case does not matter, and the longest match wins — so `magic crit` reads as
magic crit rate and never as plain crit rate.

## How a value is read

- `+40` / `-2` is a **flat** addend; `+7%` and `x1.07` are both the **percent** form of the same
  thing (`x1.07` → +7%, `x0.5` → −50%). Write whichever reads better.
- A number binds to the **nearest** stat word, before or after it: `p.def +40` and `+40 p.def`
  are the same. Keep the word next to its number and nothing can cross-match.
- `;` starts a new clause. A clause may open with a **scope label** — see below.
- Anything in `(brackets)` is treated as commentary and ignored, with ONE exception:
  `(success chance x1.5)` and `(interrupt chance x2)` are read as data.

## Scope labels — which gear state a clause is about

| Label | Means |
|---|---|
| `robe:` `light:` `heavy:` | that body-armour weight only |
| `bare:` `naked:` `none:` | no body armour |
| `with light` `with heavy` … | the same, in sentence form |
| `with sword` `with blunt` `with bow` `with duals` | that weapon only |
| `with all` / `with any` | everything the `WEIGHT` / `WEAPON` column allows |

The gate itself belongs in the **`WEIGHT`** and **`WEAPON`** columns, not in the prose — those are
what the game enforces and what `--check` compares. A label here only says which half of a
multi-part row a number belongs to.

## The stat words

| Key | Write any of |
|---|---|
| `power` | `power`, `transfers`, `heal for`, `heals for`, `restores`, `damages the mp`, `friendly targets` |
| `blockrate` | `shield defence rate`, `shield defense rate`, `shield rate`, `block rate`, `block chance` |
| `shielddef` | `shield.p.def`, `shiled defence`, `shield defence`, `shield def`, `shield pdef`, `shield p.def` |
| `mdef` | `magic defence`, `magic defense`, `magic def`, `m.def`, `mdef` |
| `matk` | `magic attack`, `m.atk`, `matk`, `mattack` |
| `patk` | `physical attack`, `p.atk`, `patk`, `pattack`, `p.attack` |
| `pdef` | `physical defence`, `physical defense`, `p.def`, `pdef`, `p. def` |
| `maxhp` | `maxhp`, `max hp` |
| `maxmp` | `maxmp`, `max mp` |
| `mpcost` | `mp consumption`, `mp cost`, `mana cost`, `mana consumption` |
| `mpregrun` | `running`, `while running`, `run` |
| `mpregwalk` | `walking`, `while walking`, `walk` |
| `mpregstand` | `standing still`, `standing`, `while standing` |
| `mpreg` | `mp regeneration`, `mp regen`, `mpreg`, `mp reg`, `mp` |
| `hpreg` | `hp regeneration`, `hp regen`, `hpreg`, `hp reg` |
| `cast` | `cast speed`, `casting speed`, `cast` |
| `as` | `attack speed`, `atack speed`, `atk speed`, `as` |
| `ms` | `move speed`, `movement speed`, `ms`, `speed`, `move` |
| `reuse` | `reuse delay`, `reuse`, `cooldown` |
| `mres` | `mres`, `magic resist`, `magic resistance`, `chance for spells to fizzle`, `spells to fizzle` |
| `critdmg` | `critical damage`, `crit damage`, `crit dmg`, `critdmg` |
| `critrate` | `critical rate`, `crit rate`, `critrate`, `critical` |
| `magiccritrate` | `magic critical`, `magic crit` |
| `skilleva` | `skill evasion` |
| `magiceva` | `magic evasion` |
| `eva` | `evasion`, `eva` |
| `acc` | `accuracy`, `acc` |
| `interruptmult` | `interrupt chance` |
| `interrupt` | `interrupt resistance`, `interrupt` |
| `manavamp` | `mana vampirism`, `mana vamp` |
| `vamp` | `vampirism`, `vamp` |
| `restoremp` | `mpwhenrestored`, `mp when restored` |
| `bowrange` | `range` |
| `bowresist` | `bow resistance`, `bow resist`, `arrow defence` |
| `ccresist` | `cc resist`, `ccresist` |
| `critrateres` | `crit rate resist`, `critical rate resist` |
| `critdmgres` | `crit dmg reduction`, `crit dmg resist`, `crit damage reduction`, `critical damage reduction`, `critical damage resist` |
| `successchance` | `success chance` |
| `procchance` | `chance` |
| `ccresist` | `resist to spt`, `resist to con` |
| `cancelresist` | `cancel resist`, `buff cancel resist` |
| `aggro` | `aggro`, `threat` |
| `reagent` | `consumes`, `skill stones`, `skill stone`, `elemental stones`, `elemental stone` |
| `resexp` | `of lost exp`, `lost exp` |
| `lifesteal` | `of the damage dealt`, `of damage dealt`, `heals you` |
| `offencespeed` | `offence and speed`, `offense and speed` |

46 keys, 141 spellings.

## Words that are read but are not stats

Numbers next to these are consumed deliberately so they do not report as `UNREAD`:
durations in `s`/`min`, ranges, `rank N`, `lvl N`, stack counts, and the `otherwise N`
restatement of a non-crit damage. See the ignore rules in `Descr.cs`.
