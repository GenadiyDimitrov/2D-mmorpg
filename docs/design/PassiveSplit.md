# `BL-314` — the passive split, designed (proposal, NOT built)

2026-09-29. Built on your answers to `SpBudget.md` §5:

1. *"not flat for everyone -> make it as u said .. target for evey class"*
2. *"make the target x0.60 and x0.45~0.55 for healer/buffers — rogues can even be about x7.5~8, warriors/tanks x4.5~5.5,
   mages/healers x4~4.5"*
3. *"harder for healers they have most skills of all so they need to deside -> support/party or dmg/solo"*
4. *"dont count the favor -> ... the favor just speedup the things not change it"*

Every number here is printed by two BalanceMatrix commands:
`--sp-budget` (the last table, "HIS PER-ARCHETYPE k") and `--passive-inventory [skill-id]` (what every passive carries,
and what a split costs in rungs). Nothing below is hand-computed.

**No CSV or skill has been touched.** This doc is for you to rule on; §8 has the questions.

---

## 1. Your multipliers, measured

`x` = SP earned from kills over 20-75 ÷ what the whole kit costs over 20-75, with the 20-75 passives priced ×k. The
Favor is not counted (your answer 4).

| path                                       | k low → x       | k mid → x        | k high → x      | **my pick** | x at my pick |
| ------------------------------------------ | --------------- | ---------------- | --------------- | ----------: | -----------: |
| daggers (Phantom, Stalker, Assassin)       | 7.5 → 0.60-0.64 | 7.75 → 0.59-0.62 | 8 → 0.57-0.61   |    **7.75** |    0.59-0.62 |
| bows (Sentinel, Soultracker, Sharpshooter) | 7.5 → 0.61-0.65 | 7.75 → 0.59-0.63 | 8 → 0.58-0.62   |    **7.75** |    0.59-0.63 |
| warriors (Ravager ×3, Warlord ×3)          | 4.5 → 0.62-0.73 | 5 → 0.57-0.67    | 5.5 → 0.53-0.62 |       **5** |    0.57-0.67 |
| tanks (Bulwark ×3)                         | 4.5 → 0.58-0.62 | 5 → 0.54-0.57    | 5.5 → 0.51-0.54 |     **4.5** |    0.58-0.62 |
| Magus ×3                                   | 4 → 0.65-0.67   | 4.25 → 0.63-0.64 | 4.5 → 0.60-0.61 |     **4.5** |    0.60-0.61 |
| Lightbringer ×3                            | 4 → 0.51-0.53   | 4.25 → 0.50-0.51 | 4.5 → 0.49-0.50 |    **4.25** |    0.50-0.51 |
| Warchanter ×3                              | 4 → 0.47-0.49   | 4.25 → 0.45-0.47 | 4.5 → 0.44-0.45 |       **4** |    0.47-0.49 |

- Your numbers land where you wanted them. I nudged tanks to 4.5 and the Magus to 4.5 (both still inside your ranges),
  because at the midpoint the tank was under 0.60 and the Magus over it.
- **Bows take the rogue's number.** You said "rogues"; the bows measured the same as the daggers (their actives are just
  as cheap), so a lower number would leave them buying nearly everything again.
- **The Warchanter lands lowest (0.47-0.49) even at your lowest 4.** That is your answer 3: he has the biggest kit.
- 🔑 **Everyone is short from level 20.** With these prices, buying everything the moment it opens runs out of SP at
  **level 20 itself**, the first 2nd-class level (short by 17k-57k). So choosing starts at the class change, not in the 50s.
  That is what you asked for. The 20-24 levels will simply feel different from today.
- A path's k is **one number per archetype, set in one place** (`SpBudget.HisK` today; a table in `SkillCatalog` when
  built). It is not typed into each CSV row: the CSV keeps the ×1 price and the engine multiplies it (see §5).

## 2. What the passives carry today

`--passive-inventory` lists every passive a playable path can learn. The split only concerns the **bundles**:

| bundle                                             |   rungs | levels | stats inside                                                                       |
| -------------------------------------------------- | ------: | ------ | ---------------------------------------------------------------------------------- |
| `rogue_armor_mastery` / `archer_armor_mastery`     | 35 / 30 | 20-90  | P.Def, evasion, move speed, HP regen, MP regen, crit-rate resist (all LIGHT-gated) |
| `tank_armor_mastery`                               |      35 | 20-90  | P.Def, P.Def %, evasion −, MP regen, crit-damage resist (HEAVY)                    |
| `warrior_armor_mastery`                            |      35 | 20-90  | P.Def, evasion (light), max HP (heavy), HP regen                                   |
| `mage_armor_mastery`                               |      33 | 20-90  | P.Def, max MP, restore-MP %, M.Def %, MP cost % (ROBE)                             |
| `healer_armor_mastery` / `buffer_armor_mastery`    |      29 | 40-90  | P.Def, max MP, M.Def %, MP cost %                                                  |
| `spell_mastery` / `spellcaster_weapon_mastery`     | 33 / 29 | 20-90  | P.Atk, M.Atk, cast speed, cooldown, HP regen, MP regen (weapon-gated)              |
| `dual_weapon_mastery` / `bow_mastery`              |      30 | 40-90  | P.Atk, P.Atk %, crit damage, crit rate, attack speed, accuracy (+ bow range)       |
| `rogue_weapon_mastery`                             |       5 | 20-36  | the same six, 2nd class                                                            |
| `tank_weapon_mastery`                              |      35 | 20-90  | P.Atk, P.Atk %, attack speed                                                       |
| `warrior_blunt_mastery` / `warrior_weapon_mastery` |  30 / 5 | 20-90  | P.Atk, crit damage, cleave targets, cleave radius                                  |
| `tank_anti_magic` / `anti_magic_mage`              |      35 | 7-90   | M.Def, magic resist (+ fail mod)                                                   |
| `hp_regeneration`                                  |       7 | 43-74  | HP regen, sitting HP/MP regen                                                      |
| `tank_shield_mastery`                              |       7 | 20-52  | block chance, block reduction, bow resist, P.Def % (heavy + shield)                |

The same stat is re-authored inside many of them: **P.Atk in 14 passives, P.Def in 10, MP regen in 15 (flat or %),
evasion in 8, attack speed in 8, accuracy in 8, cast speed in 7, HP regen in 6.** That is your "20 armor masteries".

**Not split** (already one stat, or a pair on purpose): the stat swaps (Power/Vigour …, a pair by design), the level-76
sigils, the racial blessings (level 7, free), the level-83 proficiencies, and the single-purpose passives (Overpower,
Vital Points, HP Boost, Calm Spirit, Whisp Mastery …). They are still repriced ×k if they sit in 20-75 (§5).

## 3. The split — which stats

🔑 **One rule: one piece = one stat.** A flat value and a % of the SAME stat stay together (P.Def +N and P.Def ×1.07 is
one "P.Def" piece; P.Atk +N and P.Atk % is one "P.Atk" piece). Everything else becomes its own passive.

Taking `rogue_armor_mastery` as the example, one skill with 35 rungs becomes six:

| piece (proposed id) | gate   | rungs today → rungs after | what it is                 |
| ------------------- | ------ | ------------------------- | -------------------------- |
| `light_defence`     | light  | 35 → 35                   | P.Def (changes every rung) |
| `light_evasion`     | light  | 35 → 10                   | evasion                    |
| `light_step`        | light  | 35 → 6                    | move speed                 |
| `light_crit_guard`  | light  | 35 → 3                    | crit-rate resist           |
| `hp_regen_mastery`  | see §6 | 35 → 8                    | HP regen                   |
| `mp_regen_mastery`  | see §6 | 35 → 8                    | MP regen                   |

🔑 **A piece needs a rung only where ITS number changes.** Today crit-rate resist is "learned" 35 times, but it only
changes 3 times (0.15 at 20, 0.25 at 40, 0.35 at 60); the other 32 rungs re-state it because P.Def moved. After the split,
each rung of each piece is a real step up, which is exactly the decision you want. **So the split roughly doubles the rows
(35 → 70 for the rogue), it does not multiply them by six.** The print's last table has the count for every bundle:
tank armor 35 → 58, warrior armor 35 → 88, mage armor 33 → 83, spell mastery 33 → 84, dual mastery 30 → 73.

### Shared ladders, not per-class copies

Your note: *"the base mastery is the same across classes ... if we take the regen and make it separate passive we can mix
and match"*. So a piece like `hp_regen_mastery` is **one skill id with one ladder of numbers**, and each class's table
grants the rungs it gets, at its own levels. That is exactly how `bow_expertise` works now (the archer gets rung 2 at 52,
the Elf buffer gets rung 2 at 56).

🔑 **The shared ladder keeps every number you authored.** Its rungs are the union of every value any class has for that
stat, sorted. The rogue's HP regen (2.5, 3, 3.5, 4, 4.5, 5, 5.5, 6) and the mage's (1.1, 1.6, 1.7, 2.1, 2.6, 2.7) become
one ladder of at least 14 rungs (1.1 … 6, more once the warrior's and the 76+ values join). Each class learns only the rungs that match what it has today, so **the split is a
repricing, not a rebalance.** Nobody's stats change on the day it ships. The engine already skips rungs a class's table
does not offer (`ClassSkills.NextLearnableLevel`), so nothing new is needed for that.

The pieces that would be shared (one id, many classes):

| shared piece                                                                  | fed from                                                                                         |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| `hp_regen_mastery`                                                            | rogue, archer, warrior armor; spell and spellcaster weapon masteries; `hp_regeneration` (see Q4) |
| `mp_regen_mastery`                                                            | rogue, archer, tank armor; spell and spellcaster weapon masteries                                |
| `mana_capacity` (max MP)                                                      | mage, healer, buffer armor; `armor_mastery`                                                      |
| `spell_haste` (cast speed %)                                                  | spell and spellcaster weapon masteries                                                           |
| `spell_recovery` (cooldown %)                                                 | the same two                                                                                     |
| `light_evasion`, `light_step`, `light_crit_guard`                             | rogue and archer armor                                                                           |
| `robe_mdef` (M.Def %), `robe_mp_cost` (MP cost %)                             | mage, healer, buffer armor                                                                       |
| `weapon_accuracy`, `weapon_haste` (attack speed %), `weapon_crit` (crit rate) | dual, bow, rogue, tank weapon masteries, warriors' strength                                      |

The pieces that stay **per class**, because they are the class's identity (the numbers differ a lot between classes):
- the **P.Def piece of each armor weight** (the tank's heavy P.Def is not the warrior's), and
- the **P.Atk / M.Atk piece of each weapon mastery**, with crit damage and cleave (the warrior's blunt power, the dagger's
  power, the bow's power).

These keep their current ids, minus the stats that moved out. That also keeps the `--check` history and the CSV rows'
names stable.

## 4. The CSV shape

**My pick: a shared ladder file plus the class files, the same two-file pattern as `debuff_landmods.csv`.**

- **`docs/data/passive_ladders.csv`** (new): one row per rung of every SHARED piece, holding the numbers.
  `SKILL_ID, RUNG, VALUE, GATE`. The numbers live in one place, so two classes can never drift apart on the same rung.
- **The class CSVs** keep one row per piece per rung the class gets: `SKILL_ID`, rung, `LEVEL`, `SP`, and the same `DESCR`
  as today, which `--check` reads back against the ladder file. So a class file still shows everything the class learns,
  in order, with its price. The per-class pieces (P.Def, P.Atk) keep their numbers in the class file as today.
- `SkillCsvSeed --check` grows a third walker for the ladder file (DRIFT / NOT IN THE FILE), like the land-mod one.

The other option was one row per rung of every piece in each class file, with its numbers. It is simpler to read in one
file, but the regen ladder would then be typed in eight files, and the first time one of them is edited they disagree.

## 5. The prices

- 🔑 **The CSV `SP` column stays the ×1 price** (the price for everyone, and what 76+ charges). **The ×k lives in the
  engine** as one number per archetype, applied to passive rungs whose learn level is 20-75. So retuning a class's
  scarcity later is one number, not every row, and `--sp-budget` stays the check.
- **Splitting the price between pieces:** the old bundle's rung at level L cost P. The pieces that step up at L share P
  **equally**. A piece that does not change at L costs nothing at L. By construction, the pieces together cost exactly
  what the bundle did at ×1, and ×k below 76. This needs no new authoring. You can then move SP between pieces in the CSV
  if one is worth more than another (Q3).
- **Every other 20-75 passive** (HP Boost, Calm Spirit, Vital Points, Overpower's first rungs …) is also ×k. That is how
  the budget was measured, and it is your "sum of all the passives". **Actives stay ×1.**
- **76+ stays ×1**, unchanged.

## 6. Weight gates

Today every stat inside an armor bundle is gated by that armor weight: a rogue's HP regen pays only in light armor.

**My pick: a piece keeps the gate that belongs to the STAT, not the bundle it came from.**
- **Armor stats stay gated:** P.Def (per weight), evasion, move speed, crit-rate resist (light), M.Def %, MP cost % (robe),
  P.Def % and crit-damage resist (heavy). They are what the armor does.
- **Body stats lose the gate:** HP regen, MP regen, max MP, cast speed, cooldown. They are what the character has trained.
  A shared piece can only have ONE gate (the gate sits on the skill's rung, and the rogue's regen is light-gated while the
  mage's is weapon-gated), so sharing them forces a choice anyway, and "none" is the only gate that fits every class.
- **Weapon stats stay gated** to their weapon: P.Atk, crit, attack speed, accuracy, cleave.

What changes in play: a rogue in plate would keep his regen pieces (not his P.Def, evasion or speed), and a mage holding
a dagger would keep his regen. Both are odd choices nobody makes, and nothing in the game strips armor or weapons (your
`BL-107` argument).

## 7. `Replaces` chains

Today the 2nd→3rd switch is done by `Replaces`: `dual_weapon_mastery` replaces `rogue_weapon_mastery`, the 3rd-class armor
replaces the 1st-class `fighter_armor_mastery`, and so on. After the split:
- **Shared pieces need no `Replaces` at all.** The 3rd class simply continues the same ladder at a higher rung.
- **Per-class pieces keep today's chains unchanged** (the P.Def and P.Atk pieces keep the ids that carry them).
- The 1st-class `fighter_armor_mastery` / `fighter_weapon_mastery` (levels 5-15) are **not split**. They are below 20, so
  there is no scarcity to add, and each weight's P.Def piece replaces them exactly as today.
- Any **other** `Replaces` that names a bundle (a buff or a racial passive that switches a mastery off) is re-pointed at
  the piece that now carries that stat. The build step lists every such chain with `--passive-inventory` before it edits.

## 8. What building it costs

- Every class CSV from 2nd to 4th tier that holds a bundle is rewritten (by a seed tool, then checked with `--check`), plus
  the new ladder file. **I would build it one archetype per version**, rogue first (the biggest bundle), measuring
  `--sp-budget` after each one.
- A `game.db` delete (the learned skill ids change). Pre-release, so no migration code.
- **A new APK**: the client builds its Learn tab from the compiled class tables.
- SmokeTest and `--check` after each archetype.

## 9. Questions for you

1. **k per archetype, my picks in §1:** daggers 7.75, bows 7.75, warriors 5, tanks 4.5, Magus 4.5, Lightbringer 4.25,
   Warchanter 4. OK? And should bows really take the rogue's number?
2. **Shared ladder file + class files (§4)**, or every number in every class file?
3. **Price split between pieces:** equal shares of the old rung price (my pick, no new authoring; you retune later), or
   do you want to weight them now (for example, P.Def 50% and the rest shared)?
4. **`hp_regeneration`** (the 7-rung HP regen + sitting regen passive, 43-74) already IS a split-out piece. Fold it into
   `hp_regen_mastery`, or keep it separate as the "sitting" passive?
5. **Gates (§6):** body stats ungated, armor and weapon stats gated. OK?
6. **Names:** the ids above are placeholders. Do you want to name the pieces, or shall I pick generic ones?

## 10. Your answers (2026-09-29) and the split as authored

1. **Targets, not k:** daggers **x≈0.65**, bows **0.55**, warriors **0.65**, tanks **0.70**, Magus **0.60**, Lightbringer
   **0.55**, Warchanter **0.50** — *"warriors/rogues need more sp to survive as melee, tanks to be effective defenders,
   healers with about half so they make decisions, buffers prioritize buffs, archers farm fast enough, mages need only
   their main spell"*. k is solved from these **after** the split (the rung counts change the kit cost).
2. **Every number in the class files** (no shared ladder file). You author; I fix ids and SP afterwards.
3. **Equal share** of the old rung price. ⚠ The SP in the piece rows is still a **placeholder**: 2nd and 4th tier copy
   the parent rung, 3rd tier halves it, and the parents still carry their full old price. The equal-share pass is next.
4. *(unanswered)* — see "decided by me" below.
5. **Gates as §6.** 6. **Generic names.**

**The pieces** (you authored fighter 1st, rogue 2nd, warrior 2-4, tank 2-4 and began dual 3rd; I finished the rest in
the same shape — a piece gets a row only where its own number changes):

| piece id                                             | stat                                                       | gate       | files                                              |
| ---------------------------------------------------- | ---------------------------------------------------------- | ---------- | -------------------------------------------------- |
| `hp_regeneration` / `mp_regeneration`                | HP / MP regen                                              | none       | every fighter and mage file with regen in a bundle |
| `fighter_critical_dmg_mastery`                       | crit damage                                                | none       | rogue, dual, archer, warrior, war_aoe              |
| `fighter_accuracy`                                   | accuracy                                                   | none       | rogue 2nd, warrior 2-3, war_aoe 3rd                |
| `fighter_str_mastery`                                | P.Atk ×1.085                                               | none       | fighter 1st (replaced by `warriors_strength`)      |
| `rogue_critical_rate_mastery` / `rogue_fury_mastery` | crit rate / attack speed %                                 | bow\|duals | rogue, dual                                        |
| `rogue_swift_mastery`                                | move speed                                                 | light      | rogue, dual, archer                                |
| `rogue_bow_proficiency`                              | bow range                                                  | bow        | rogue 2nd, archer 3rd                              |
| `dual_weapon_prof`                                   | the 3% MP/crit proc                                        | duals      | dual 3rd                                           |
| `tank_defence_mastery`                               | P.Def %, crit-dmg resist, evasion − (one piece, your call) | heavy      | tank 2-4                                           |
| `mp_capacity`                                        | max MP                                                     | none       | cleric/nuker 2nd, nuker/healer/buffer 3-4          |
| `mana_recovery`                                      | MP when restored %                                         | none       | nuker 2-4                                          |
| `cooldown_mastery` / `cast_speed_mastery`            | reuse % / cast speed                                       | none       | cleric/nuker 2nd, nuker/healer/buffer 3rd          |
| `mdef_mastery` / `mp_cost_mastery`                   | M.Def % / MP cost %                                        | none       | nuker/healer/buffer 4th                            |

The parents keep only their identity number: armor = P.Def (+ evasion and crit-rate resist for light, + HP for the
warrior's heavy), weapon = P.Atk / M.Atk (+ cleave for the blunt).

**Slips in your rows, corrected to the old numbers** (the split is a repricing, not a rebalance):
- rogue 2nd: crit dmg at 28 was 84 → **80**; Fury (AS ×1.05) moved 20 → **36** and Swift (+7) 20 → **28**, where the old
  bundle first gave them; crit dmg 32 SP 20000 → 11000.
- warrior 2nd: Accuracy 24 → **20** (Warriors Strength gave +3 at 20).
- warrior 3rd: two leftover rogue rows (crit dmg +140 at 40, +165 at 43) removed — they collided with +145 / +172.
- warrior 3rd / war_aoe 3rd: the acc +3/+6/+9 left inside Warriors Strength moved to `fighter_accuracy` (as you did in
  2nd); Warriors Strength keeps only its P.Atk %.
- tank 3rd: Tank Defence was missing the **58** rung (×1.15 arrived at 58, the 35% resist at 60).
- dual 3rd: the parent still held crit dmg / crit rate / AS / acc (now only P.Atk); the proc's `8 / 5` CD/duration had
  been copied onto the pieces; the regen pieces were light-gated (now none, §6).

**Decided by me — overrule any of them:**
- **Q4:** your `hp_regeneration` id is now the generic regen piece, so the old warrior-only 43-74 passive (flat regen +
  sitting regen) is renamed **`warrior_regeneration`** ("Warrior Regeneration"), unchanged otherwise.
- **M.Def % and MP cost % are ungated**, against §6: the buffer's armor gives them on all three weights and the mage's
  on robe only, and one shared piece can carry one gate.
- **Left bundled:** `tank_anti_magic` / `anti_magic_mage` (M.Def + M.Res), `tank_shield_mastery`, the tank's 4th-tier
  weapon attack speed, and the buffers' race masteries (Warlock/Doctor accuracy, Harmonist range) — you left the tank
  ones as they were, and the rest are one-rung or constant stats with nothing to skip.

## 11. Second pass (2026-09-29): one ladder per armor weight and per weapon family

Your follow-up: *"combine it to the mage_armor_mastery starting from lvl 1 and everyone learns their rungs ... no
replaces needed ... we can do it for other weights and weapons"*. With your answers (bow separate; buffers robe + their
race's weight; fighters no robe), every class-specific armor and weapon mastery is gone. **The §10 table still holds
for the regen / crit / speed pieces; the parents it names no longer exist.** Every row sits in the class CSVs.

| ladder                                                 | gate                | who learns it (their own levels)                                                                                | replaced                                                                                                                                                                                                                                                                          |
| ------------------------------------------------------ | ------------------- | --------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `heavy_armor_mastery`                                  | heavy               | fighter 1st, tank, warrior, war_aoe, buffer **Human/Demon**                                                     | `fighter_armor_mastery`, `tank_armor_mastery`, `warrior_armor_mastery` (its "all weights" P.Def + the heavy extra), `buffer_armor_mastery`                                                                                                                                        |
| `light_armor_mastery`                                  | light               | fighter 1st, rogue, dual, archer, warrior, war_aoe, cleric, buffer **Elf**                                      | `rogue_armor_mastery`, `archer_armor_mastery`, the warrior's and cleric's light P.Def                                                                                                                                                                                             |
| `mage_armor_mastery`                                   | robe                | mage 1st, nuker, cleric, healer, buffer (all races)                                                             | `mastery_robe`, `healer_armor_mastery`, the cleric's robe P.Def                                                                                                                                                                                                                   |
| `rogue_evasion` / `rogue_crit_resist`                  | light               | the same light wearers (warrior evasion too; cleric +2 at 35)                                                   | the rogue's / archer's / warrior's evasion and crit-rate resist                                                                                                                                                                                                                   |
| `heavy_vitality`                                       | heavy               | warrior, war_aoe                                                                                                | the warrior's "Heavy: HP +50 … +300"                                                                                                                                                                                                                                              |
| `weapon_mastery` (P.Atk)                               | sword\|blunt\|duals | fighter 1st, mage 1st, every melee class, the mages' old spell-mastery P.Atk, Warlock + Doctor (summed with it) | `fighter_weapon_mastery`, `tank_weapon_mastery`, `warrior_weapon_mastery`, `warrior_sword_mastery`, `warrior_blunt_mastery`, `dual_weapon_mastery`, the rogue's dagger half, mage 1st `weapon_mastery`, `spell_mastery`'s P.Atk, `warlock_weapon_mastery`, `doctor_blunt_mastery` |
| `bow_mastery` (P.Atk)                                  | bow                 | rogue 2nd, archer, buffer **Elf**                                                                               | the rogue's bow half, `harmonist_bow_mastery`                                                                                                                                                                                                                                     |
| `spellcaster_weapon_mastery` (M.Atk)                   | sword\|blunt\|bow   | mage 1st, nuker, cleric, healer, buffer                                                                         | `spell_mastery`, mage 1st `weapon_mastery`'s M.Atk                                                                                                                                                                                                                                |
| `blunt_cleave`                                         | 2h blunt            | warrior 2nd (2-4), war_aoe 3rd (5-10)                                                                           | the cleave inside the two warrior blunt masteries                                                                                                                                                                                                                                 |
| `anti_magic` / `magic_resistance` / `magic_protection` | none                | tank (to +160 / +20% / one rung at 80), every mage (to +149 / +35%, no protection)                              | `tank_anti_magic`, `anti_magic_mage`                                                                                                                                                                                                                                              |
| `fury_mastery`                                         | none                | rogue, dual, archer, **tank** (4th-tier +1/3/5%)                                                                | `rogue_fury_mastery` (renamed: it is no longer the rogue's alone)                                                                                                                                                                                                                 |

**Your two mistakes, fixed:** robe P.Def at 48 is **47** in every file (the nuker and buffer had 50, as at 52), and
spellcaster M.Atk at 48 is **36** (nuker, healer, buffer had 45).

**What moves in play** (repricing apart):
- 🔴 **The Ravager loses 22 P.Atk and 106 crit damage from 40 on.** His 2nd-tier `warrior_weapon_mastery` had no Replaces
  and STACKED under `warrior_sword_mastery` (the Warlord's did not stack). One ladder ends that, and the values are yours
  (*"aoe ends at 130 while warrior ends at 150"*). Say if the stack was intended; it would be +22 on every Ravager rung.
- The mages keep their 2nd-tier P.Atk (+10 at 35) past 40 (`spellcaster_weapon_mastery` used to replace it). Harmless.
- A mage's 5% fizzle at 14 (`anti_magic_mage` rung 2) is gone, per *"no magic_prot"* for mages. It only lived 14-19.
- The dagger's first 3rd-tier P.Atk rung (+20 at 40) is dropped: the 2nd tier already ends at +21. It was a dip in the
  old files too, so the dagger keeps +21 until 43 (+23).
- The healer's first robe rung carries `[armor_mastery]`, so it still drops the cleric's light-armor casting fix
  (`armor_mastery`, which now holds only that fix) exactly as `healer_armor_mastery` did.
- The Elf buffer's spell P.Atk no longer pays on a bow (melee gate); its bow P.Atk is `bow_mastery` (+100 … +1000).

**For the build step:** `SkillCsvSeed --check` treats a skill's rows as one ladder regardless of RACE, so the buffer's
Human/Demon and Elf `weapon_mastery` rows show as 29 false "LADDER DIPS". The checker must learn races before it can
vouch for these files.

**Other merges I can see (not done, your call):**
- `dual_anti_magic` (dagger 4th, Human: M.Res +5/7/10%) **is** `magic_resistance`, but at a lower scale than the mages'
  (they are at 35%). As a rung of the shared ladder it would need its own values — or keep it as it is.
- `harmonist_light_mastery` (Elf buffer) carries evasion +6 and crit-rate resist 15%, which are `rogue_evasion` /
  `rogue_crit_resist` rungs; `wc_chanter_heavy_mastery` carries crit-damage resist 15%, which `tank_defence_mastery`
  also has (bundled with P.Def % and evasion −). Their cast/attack-speed "restore" is the real identity and would stay.
- `warriors_strength` (P.Atk % with a 2H) and `fighter_str_mastery` (×1.085, any weapon) are the same stat; one
  `strength_mastery` ladder (×1.085 → ×1.2 → +25% → +30%) would drop another Replaces.
- `tank_shield_mastery` is already shared (tank + Human buffer). `hp_boost`, `overpower`, `reuse_reset_momentum`,
  `lasting_enchantment` are already one id across classes.

## 12. Third pass (2026-09-29): your answers, the equal-share SP pass, and k solved

**Your answers to §11:**
1. **Ravager:** *"shouldnt have stacked -> +22/106 -> +52/145 not 74/251 -> now the split fixed it"*. The one ladder
   stays; the Ravager is at +52 P.Atk / +145 crit damage at 40, as your values say.
2. **Daggers:** keep **+21 at 36 / +23 at 43**, no rung at 40 (*"no point for +1 atk"*).
3. **Mages:** the +10 P.Atk past 40 stays (the summoners will need it later); the 5% fizzle at 14 was a mistake, so it
   stays gone. You retuned the Elf buffer's `bow_mastery` to follow the archers' rungs (40-75 two rungs behind, 76+
   three) so it nearly makes up the melee P.Atk it lost in the split.
4. **Other splits, all DONE in the CSVs:**
   - cleric `armor_mastery` @20 (the light casting fix) → **`clerics_light_armor_mastery`**; the healer's robe rung
     now replaces that id.
   - `tank_defence_mastery` → **`tank_defence_mastery`** (P.Def ×, evasion −) + **`tank_crit_resist`** (crit damage
     reduction), heavy-gated, each with a row only where its own number moves (tank 2nd 20; 3rd 40/58 and 40/60; 4th
     76/79/80/82/85 and 79/82/85).
   - dagger 4th `dual_anti_magic` → **`magic_resistance`** (Human, +5/+7/+10% at 80/85/90, unchanged).
   - `harmonist_light_mastery` → **`rogue_evasion`** (+6) + **`rogue_crit_resist`** (+15%), Elf, light. Its casting
     restore is gone: the Elf buffer already has the cleric's light fix from 20, and the two together were the ×4
     you spotted.
   - `wc_chanter_heavy_mastery` → your **`cleric_heavy_armor_mastery`** @40 (Human/Demon, heavy, "Heavy Caster
     Mastery", your row verbatim) + **`tank_crit_resist`** (15%, heavy).
   - `warriors_strength` and `fighter_str_mastery` → one **`strength_mastery`**: rung 1 (fighter 1st @5, ×1.085) has
     NO weapon gate; the warrior's rungs (×1.2 @20, +25% @40, +30% @52; Warlord +25% @40) are gated to 2H sword/blunt.
     ⚠ **For the build:** one id with per-rung gates is new to the engine. The rule it needs: a passive pays its
     **highest learned rung whose gate holds**, so a warrior holding a bow still gets rung 1's ×1.085 and never the
     +30%. The `[fighter_str_mastery]` Replaces is gone.

**The equal-share SP pass (done):** every piece row is repriced from the OLD bundle rung it came from
(`git show 1f90cae:<file>`): a bundle rung's price (SP, and gold on the 4th tier) is split equally among the pieces that
have a row at that level. Two consequences of your "a piece gets a row only where its number moves": a level where
only one piece moves keeps the full old price on that piece, and the total per level is conserved. Two rules for the
edge cases:
- **A piece fed by two bundles that both stacked gets the sum** (the buffer's spell P.Atk + Warlock/Doctor mastery →
  `weapon_mastery`).
- **A row shared by several races gets the mean of their prices.** The buffer's Human/Demon `weapon_mastery` @40 is
  36k: 45k for the Human (the Doctor's mastery had no second piece), 27k for the Demon (the Warlock's split with
  accuracy).
- Your `cleric_heavy_armor_mastery` row said 36k; as one of two pieces of the old 36k heavy mastery it is **18k**.
- The tank's `magic_protection` @80 was auto-granted (SP 0) in the old files; it now takes a third of the 80 Anti-Magic
  rung (1.7kk gold). Say if it should go back to free.

Every path's 20-75 passive total is within 0.1% of the old one (rounding and the race mean), so the prices are a
redistribution, not a change.

**k, solved** with the new `BalanceMatrix --sp-budget-csv [dir]`, which prices every path from its CSV files (the
split is not in the compiled tables yet) and solves each archetype's multiplier on its 20-75 passives from your x
targets (k = the mean of its paths' k):

| group        | x target | **k**    | x per path at that k |
| ------------ | -------: | -------: | -------------------- |
| daggers      |     0.65 | **7.15** | 0.62-0.67            |
| bows         |     0.55 | **9.09** | 0.53-0.56            |
| warriors     |     0.65 | **4.63** | 0.61-0.72            |
| tanks        |     0.70 | **3.58** | 0.67-0.72            |
| Magus        |     0.60 | **4.60** | 0.60                 |
| Lightbringer |     0.55 | **3.52** | 0.54-0.56            |
| Warchanter   |     0.50 | **3.73** | 0.49-0.51            |

- Against your first guesses (rogue/archer 7.5-8, warrior/tank 4.5-5.5, mage/healer 4-4.5): the bows need **more**
  (their passives are only 22-26% of the kit, so it takes ×9 to reach 0.55), and the tanks **less** (0.70 is a looser
  target).
- The warriors spread widest (0.61-0.72): the Elf Ravager's actives are cheap. One k per archetype cannot flatten that;
  it is the kit, not the passives.
- Run on the pre-split files (`--sp-budget-csv <old folder>`), the tool reproduces the code-based `--sp-budget` (Magus
  1.34, healers 0.78, tanks 1.32), and the same k to two decimals.
- Everyone is short by 10-50k on arriving at 20 (the 2nd-tier rungs open at once): one or two mobs.

**For the build step:** `SkillCsvSeed --check` now reads the RACE column, so a shared id holding two races' ladders is
two ladders (the 29 false dips on the buffer's `weapon_mastery` are gone, and no real dip is left). Build order: the
engine learns the ids above, per-rung gates for `strength_mastery`, and ×k per archetype on 20-75 passive rungs (in the
engine, not the CSV); then `--check`, SmokeTest, a `game.db` delete and a new APK.

## 13. Built (0.215.0, 2026-09-29)

**The ladders are generated from the CSVs.** `dotnet run --project tools/SkillCsvSeed -- --gen-passives` reads every class
file and writes `Game.Shared/Skills/Skills.PassiveLadders.g.cs` (34 ladders, 756 rungs) and
`Game.Shared/RaceAndClasses/ClassSkillTables.Passives.g.cs` (1,388 learn rows). A ladder is the union of every value any
class authors for that id, and each class learns its own rungs of it at its own price and gold. The generator refuses on
an unknown stat word, two values for one stat, or a class whose rungs would go down. **After editing a passive row of these
ids, regenerate**; `--check` then compares as before. The hand-written half (builders, the Dual Proficiency proc) is
`Skills.PassiveLadders.cs`.

**Each piece keeps its bundle's engine channel.** Armour pieces are `ArmorMasteryProfile`s (percentages compose as they did
inside the bundle), weapon pieces `WeaponMasteryProfile`s, and the rest plain passives gated by armour weight.

**×k is in the engine** (`Game.Shared/SpScarcity.cs`): every PASSIVE rung a class table offers at 20-75 costs ×k, rounded to
three significant figures, written onto the row once when the tables load. The 2nd-tier rogue and cleric pay the mean of
their two groups (8.12 and 3.625). The CSVs show the scaled price (`--apply-k`, a one-time migration, did that on this
date); the generator reads a 20-75 price back through `SpScarcity.Unscale`. `BalanceMatrix --sp-budget`'s "x 20-75" is
now each path at its k: daggers 0.62-0.66, bows 0.53-0.56, warriors 0.61-0.72, tanks 0.67-0.72, Magus 0.60,
Lightbringer 0.54-0.56, Warchanter 0.49-0.51.

**Two engine additions:**
- `SkillDef.PayHighestGatedRung` (only `strength_mastery`): the passive pays the highest rung at or below the one you own
  whose weapon gate your weapon meets, so a warrior with a bow keeps ×1.085.
- `ClassSkill.Replaces` (and `GoldCost`): a class ROW can retire a skill. The Lightbringer's first robe rung retires
  `clerics_light_armor_mastery`; the Warchanter, climbing the same `mage_armor_mastery`, keeps it. Checked at learn, in
  the learn list (server and client) and in the debug learn-all.

**Deleted:** the 29 old bundles (every armour/weapon mastery, `spell_mastery`, the warrior's strength and two-hand masteries,
`tank_anti_magic`, `anti_magic_mage`, `dual_anti_magic`, the buffer armour and race weapon masteries), their class rows,
the central `MasterySkills` injector, ~100 helpers that only they used, and two migration shims for their old ids.

**Things that moved without being asked:**
- The old `anti_magic` was an AUTO-GRANTED tank fizzle multiplier (×2 / ×2.5 / ×3 from 20 / 40 / 76), in no CSV. The id is
  now his M.Def ladder, so the grant moved to `tank_spell_ward` ("Spell Ward"), unchanged. It makes the paid ×2
  `magic_protection` @80 worth nothing (the engine keeps the higher multiplier): `BL-325`.
- A shared ladder's rung numbers are its union index, so the Skills window shows a rung number above the count of
  rungs that class has bought (they skip the other classes' rungs).
- `--check` learned two things: race-split rows at one level pair by race, and `(x1000)` SP cells keep their decimals
  (`50.1` = 50,100).

## 14. Superseded pricing (0.216.0, `BL-326`)

The ×k above is gone. His later rule: each level's SP is one pot, split by weight (passive pieces 0.33, utility 1,
strikes 1.5; his weights in `docs/data/sp_weights.csv`, applied by `SkillCsvSeed --reweigh-sp`). The pots are the
prices this section left behind, so the x targets of §12 still hold. See `docs/CHANGELOG.md` 0.216.0.
