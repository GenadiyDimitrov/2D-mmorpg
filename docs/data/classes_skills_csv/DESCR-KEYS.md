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
| `blockreduction` | `shield dmg reduction`, `shield damage reduction`, `shield reduction`, `shield.p.def`, `shiled defence`, `shield defence`, `shield def`, `shield pdef`, `shield p.def` |
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
| `as` | `p.atk.speed`, `atk.speed`, `attack.speed`, `attack speed`, `atack speed`, `atk speed`, `as` |
| `movatkspeed` | `move/attack.speed`, `move/attack speed` |
| `atkcastspeed` | `atk/cast.speed`, `atk/cast speed`, `attack/cast speed` |
| `slow` | `slowed by`, `slowed with`, `slow` |
| `ms` | `move speed`, `movement speed`, `ms`, `speed`, `move` |
| `reuse` | `reuse delay`, `reuse`, `cooldown` |
| `mres` | `mres`, `m.res`, `magic resist`, `magic resistance`, `chance for spells to fizzle`, `spells to fizzle` |
| `critdmg` | `critical damage`, `crit damage`, `crit dmg`, `critdmg` |
| `blowrate` | `blow landing rate`, `blow rate`, `blowrate` |
| `blowres` | `blow resist`, `blow resistance` |
| `doublerate` | `double damage rate`, `double rate`, `doublerate` |
| `durationrate` | `double duration rate`, `duration double rate`, `durationrate` |
| `reusereset` | `reuse reset rate`, `reuse reset`, `cooldown reset` |
| `critdmg` | `p.crit.dmg`, `p.critical.dmg` |
| `critrate` | `p.crit.rate`, `p.critical.rate` |
| `pvpdmg` | `pvp dmg`, `pvp damage`, `pvp spell power` |
| `skillreflect` | `to reflect physical damage skill`, `reflect physical damage skill`, `physical skill reflect` |
| `debuffreflect` | `to reflect debuff`, `reflect debuff`, `debuff reflect` |
| `reflect` | `reflect` |
| `channelshots` | `times over` |
| `critrate` | `critical rate`, `crit rate`, `critrate`, `critical` |
| `magiccritdmg` | `magic critical damage`, `magic critical dmg`, `magic crit damage`, `magic crit dmg`, `m.crit.dmg`, `m crit dmg` |
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
| `critrateres` | `m.crit.rate.received`, `p.crit.rate.received`, `crit.rate.received`, `critical.rate.received`, `rate.received`, `crit rate resist`, `critical rate resist` |
| `critdmgres` | `p.critical.dmg.received`, `p.crit.dmg.received`, `crit.dmg.received`, `critical.dmg.received`, `dmg.received`, `crit dmg reduction`, `crit dmg resist`, `crit damage reduction`, `critical damage reduction`, `critical damage resist`, `crit damage resist` |
| `successchance` | `success chance` |
| `procchance` | `chance` |
| `ccresist` | `resist to spt`, `resist to con` |
| `ccresist` | `resistance to debuffs`, `resist to debuffs`, `debuff resistance` |
| `cancelresist` | `removal attacks`, `removal`, `cancel resist`, `buff cancel resist` |
| `healrecv` | `received hp`, `healing received`, `heal received` |
| `hpgate` | `less or equal to`, `when hp is below`, `hp is below` |
| `pvedmg` | `pve dmg`, `pve damage`, `pve spell power` |
| `aggro` | `aggro`, `threat` |
| `reagent` | `consumes`, `skill stones`, `skill stone`, `elemental stones`, `elemental stone` |
| `resexp` | `of lost exp`, `lost exp` |
| `lifesteal` | `of the damage dealt`, `of damage dealt`, `heals you` |
| `offencespeed` | `offence and speed`, `offense and speed` |
| `chargecap` | `focus' up to`, `focus up to` |
| `chargespend` | `focus spend` |
| `chargepower` | `focus bonus` |
| `chargeonhit` | `focus on hit` |
| `chargeoncrit` | `focus on crit` |
| `hpprice` | `hp price` |
| `mpprice` | `mp price` |

73 keys, 224 spellings.

## Words that are read but are not stats

Numbers next to these are consumed deliberately so they do not report as `UNREAD`:
durations in `s`/`min`, ranges, `rank N`, `lvl N`, stack counts, and the `otherwise N`
restatement of a non-crit damage. See the ignore rules in `Descr.cs`.
