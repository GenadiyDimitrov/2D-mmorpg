# Move speed — your two formula orderings, measured (`BL-238`)

**2026-09-16.** You asked for the table before the ruling:

> *"every mark should decrease speed with 20% -> the move speed of chars with all the buffs is + 69
> and make all over 200.. And this values should be reserved for rogues ... I just don't know the
> formula we should use - can u make me tables with bot formulas below and : race,
> mage/fighter/rogue(with armor passive), base, without mark, formula1 with mark, formula 2 with
> mark, +60(sprint)"*

|        | ordering                                                                      |
| ------ | ----------------------------------------------------------------------------- |
| **F1** | `(base × buffs) × debuffs + flat` — the flat shelf survives the cut untouched |
| **F2** | `(base × buffs + flat) × debuffs` — the cut eats the flat shelf too           |

*F1* Is Declined - Owner don't like it!

✅ **RULED, 2026-09-16 — the line above is yours, and it settles ordering: F2.** It is also the
cheaper of the two to build, because **F2 is what the engine already does** (§0.3 below), so not one
slow in the game moves and the Mark cut becomes the only change. **Two questions from this page are
still open** — §0.1's *which reading of "decrease by 20%"* (the Marks **grant** +20% move speed
today, and the two readings are 40 points apart) and §0.2's *does the Harmony Mark take the cut*.
Nothing is built until those two land.

Everything below is measured, not derived: `dotnet run --project tools/BalanceMatrix -- --speed`
builds real level-90 `Entity` objects in real epic gear with the real NPC shelf and reads
`Entity.EffectiveSpeed` back to check the model against the engine before printing a single column.
Re-run it whenever a speed number moves.

---

## 0. Three things the measurement found before the tables start

### 🔴 A MARK GRANTS +20% MOVE SPEED TODAY
`markCore` in `Skills.Lightbringer4th.cs` — the half every Mark shares — carries
`BuffMoveSpeed +20%`, alongside the +20% attack speed and +20% cast speed. So Holy, Life and Blood
are all *speed buffs* right now, and a Mark is a large part of why everyone is over 200.

That means *"every mark should decrease speed with 20%"* has **two readings, 40 points apart**, and
I am not picking one for you:

- **Reading A** — the +20% stays and a 20% cut goes on top. Net: `×1.20 × 0.80 = ×0.96`. Wearing a
  Mark becomes almost speed-neutral.
- **Reading B** — the Mark's `+20%` **becomes** `−20%`. Wearing a Mark becomes a real cost.

Both are tabulated below. **This is the one question the table cannot answer for you.**

### ⚠ THE BUFFER'S HARMONY MARK CARRIES NO MOVE SPEED AT ALL
`markMags` in `Skills.Warchanter4th.cs` has thirteen lines and move speed is not one of them. So the
four Marks are already asymmetric on this axis: today the Warchanter's Mark is **20% of base slower**
than the healer's three. Under reading A it would become the *fast* Mark by the same margin; under
reading B all four would finally agree. Whichever you pick, say whether the Harmony Mark takes the
cut too.

### ⚠ F2 IS WHAT THE ENGINE ALREADY DOES
`Entity.EffectiveSpeed` is `ModifiedStat(base, BuffMoveSpeed) × (1 − SlowFraction)`, and
`ModifiedStat` is `base × (1 + pct) + flat`. So **every slow in the game is already ordering F2**.
Picking F1 is not a Mark change — it re-orders every percentage debuff there is, slows included.
That is one ruling, not two.

---

## 1. Where the +69 actually comes from

| source                                    |                           flat |      pct |
| ----------------------------------------- | -----------------------------: | -------: |
| SpeedTable base (Human fighter)           |                            115 |        — |
| gear + armour passives (folded into BASE) | +0 (heavy) … +17 (light rogue) |        — |
| Swift (NPC single)                        |                            +33 |        — |
| Frenzy                                    |                             +8 |        — |
| Harmony of Swift                          |                            +20 |        — |
| **Holy Mark**                             |                              0 | **+20%** |
| **= the shelf**                           |                        **+61** | **+20%** |

Your "+69" is the **+61 of buff flat** plus whatever gear and passives fold into the base — and the
Mark's percent on top of both. The split matters, because the two orderings treat the flat half and
the percent half differently, and **almost the entire move-speed shelf is flat**.

`raw` in the tables below is `SpeedTable.BaseRunSpeed(race, class)`; `base` is what the character
actually runs at before any buff — the armour set's move-speed line, item speed rolls, and (the
rogue's whole point) the light Armor Mastery's flat **+7**. All of that is inside `base`, so it is
multiplied by percents and, under F2, cut by the Mark.

---

## 2. READING A — the Mark keeps its +20% and takes a 20% cut on top

Level 90, epic gear, full NPC shelf. `250*` = the row exceeds the cap and the game gives you 250.
Today +Sprint all geto to max 250

| race  | role    |  raw | base | shelf     | NO MARK | TODAY |   F2 | +sprint F2 |
| ----- | ------- | ---: | ---: | --------- | ------: | ----: | ---: | ---------: |
| Human | mage    |  109 |  114 | ×1.00 +61 |     175 |   198 |  158 |        206 |
| Human | fighter |  115 |  115 | ×1.00 +61 |     176 |   199 |  159 |        207 |
| Human | rogue   |  115 |  132 | ×1.00 +61 |     193 |   220 |  176 |        224 |
| Elf   | mage    |  114 |  119 | ×1.00 +61 |     180 |   204 |  163 |        211 |
| Elf   | fighter |  143 |  143 | ×1.00 +61 |     204 |   233 |  186 |        234 |
| Elf   | rogue   |  143 |  161 | ×1.00 +61 |     222 | 250\* |  203 |      250\* |
| Demon | mage    |  113 |  118 | ×1.00 +61 |     179 |   203 |  162 |        210 |
| Demon | fighter |  112 |  112 | ×1.00 +61 |     173 |   195 |  156 |        204 |
| Demon | rogue   |  112 |  129 | ×1.00 +61 |     190 |   216 |  173 |        221 |

## 3. READING B — the Mark's +20% becomes −10%

Today +Sprint all geto to max 250

| race  | role    |  raw | base | shelf     | NO MARK | TODAY | F2(10) | +sprint F2(10) |
| ----- | ------- | ---: | ---: | --------- | ------: | ----: | -----: | -------------: |
| Human | mage    |  109 |  114 | ×1.00 +61 |     175 |   198 |    157 |            211 |
| Human | fighter |  115 |  115 | ×1.00 +61 |     176 |   199 |    158 |            212 |
| Human | rogue   |  115 |  132 | ×1.00 +61 |     193 |   220 |    174 |            228 |
| Elf   | mage    |  114 |  119 | ×1.00 +61 |     180 |   204 |    162 |            216 |
| Elf   | fighter |  143 |  143 | ×1.00 +61 |     204 |   233 |    184 |            238 |
| Elf   | rogue   |  143 |  161 | ×1.00 +61 |     222 | 250\* |    200 |          250\* |
| Demon | mage    |  113 |  118 | ×1.00 +61 |     179 |   203 |    161 |            215 |
| Demon | fighter |  112 |  112 | ×1.00 +61 |     173 |   195 |    156 |            210 |
| Demon | rogue   |  112 |  129 | ×1.00 +61 |     190 |   216 |    171 |            225 |

## 4. What a Mark costs you, against no Mark at all

| race  | role    | today | A: F2 | B: F2 |
| ----- | ------- | ----: | ----: | ----: |
| Human | mage    | +22.8 | −16.8 | −35.0 |
| Human | fighter | +23.0 | −16.8 | −35.2 |
| Human | rogue   | +26.5 | −17.5 | −38.7 |
| Elf   | mage    | +23.8 | −17.0 | −36.0 |
| Elf   | fighter | +28.6 | −17.9 | −40.8 |
| Elf   | rogue   | +32.2 | −18.6 | −44.4 |
| Demon | mage    | +23.6 | −16.9 | −35.8 |
| Demon | fighter | +22.4 | −16.7 | −34.6 |
| Demon | rogue   | +25.9 | −17.4 | −38.1 |

**Reading A + F1 is the weakest lever on the board: about −5 points.** Reading B + F2 is the
strongest: −35 to −44.

---

## 5. 🔑 The part you should read before ruling: the ordering does NOT reserve the band

How far the rogue sits above the mage **of his own race**, buffed, no sprint:

| race  | no mark | today | A: F1 | A: F2 | B: F1 | B: F2 |
| ----- | ------: | ----: | ----: | ----: | ----: | ----: |
| Human |    18.4 |  22.1 |  17.7 |  17.7 |  14.7 |  14.7 |
| Elf   |    42.0 |  50.4 |  40.3 |  40.3 |  33.6 |  33.6 |
| Demon |    11.4 |  13.6 |  10.9 |  10.9 |   9.1 |   9.1 |

**F1 and F2 give the identical gap.** They have to: both multiply the same base difference by the
same factors, and the flat shelf is the same +61 on every row, so it cancels out of a difference.
A cut that lands on everyone lowers everyone — it cannot hand the top of the band to anyone. Every
version of the Mark cut makes the rogue's lead **smaller**, not bigger.

What actually closed the band is the flat shelf, because a flat +61 is worth more to a slow
character than to a fast one:

| race  | role    | base | buffed | gain | × base |
| ----- | ------- | ---: | -----: | ---: | -----: |
| Human | mage    |  114 |    198 |  +84 |   1.74 |
| Human | fighter |  115 |    199 |  +84 |   1.73 |
| Human | rogue   |  132 |    220 |  +87 |   1.66 |
| Elf   | mage    |  119 |    204 |  +85 |   1.71 |
| Elf   | fighter |  143 |    233 |  +90 |   1.63 |
| Elf   | rogue   |  161 |    254 |  +93 |   1.58 |
| Demon | mage    |  118 |    203 |  +85 |   1.72 |
| Demon | fighter |  112 |    195 |  +83 |   1.74 |
| Demon | rogue   |  129 |    216 |  +87 |   1.67 |

The mage multiplies his own speed by **1.74**; the elf rogue by **1.58**. The buff shelf is
*regressive* on move speed — it pays the slowest character the most.

⚠ And note the sprint columns in §2/§3: **with sprint, every row in the game is at the 250 cap
today.** The cap is doing the compressing at the top end, not the base table.

---

## 6. So what would actually reserve the band (not built, not ruled — your call)

None of these is in the code; they are what the measurement points at, listed so you can rule on
numbers instead of on a description.

1. **Make the shelf's move speed a PERCENT instead of a flat.** Swift `+33` → `×1.25` and the band
   scales with base instead of collapsing toward it. This is the single change that makes the rogue's
   base advantage survive buffing, and it is independent of the F1/F2 question.
2. **Cut the flat shelf and give the difference to the rogue's own kit** — the light Armor Mastery
   already carries `speed +7` and is the natural home for more of it.
3. **Lower the cap for non-rogues, or raise `MoveSpeedCap` for rogues.** It is already a per-entity
   field (`Entity.MoveSpeedCap`), so this costs nothing structurally.
4. **The Mark cut on top of any of the above**, for the reason you gave it: a Mark should not be free.

---

## 7. Regenerating this page

```
dotnet run --project tools/BalanceMatrix -- --speed [level] [quality]
```

Defaults to level 90 and epic gear. The mode checks its own arithmetic against
`Entity.EffectiveSpeed` on every row and prints `!!` if the model and the engine disagree, so a
future change to how speed composes cannot leave this page quietly wrong.


Owner test Table -> Rogues dont use Frenzy (decreases evasion)
Shelf/Full for rogues is 53 for all other is 61/69

| race  | role    | base  | shelf | F2(10)/shelf | +spr F2(10)/sh | full  | F2(10)/full | +dsh F2(10)/sh |
| ----- | ------- | :---: | :---: | :----------: | :------------: | :---: | :---------: | :------------: |
| Human | mage    |  114  |  175  |     157      |      211       |  183  |     165     |      219       |
| Human | fighter |  115  |  176  |     158      |      212       |  184  |     166     |      220       |
| Human | rogue   |  132  |  185  |     166      |      220       |  185  |     166     |      220       |
| Elf   | mage    |  119  |  180  |     162      |      216       |  188  |     169     |      223       |
| Elf   | fighter |  143  |  204  |     184      |      238       |  212  |     191     |      245       |
| Elf   | rogue   |  161  |  214  |     193      |      247       |  214  |     193     |      247       |
| Demon | mage    |  118  |  179  |     161      |      215       |  187  |     168     |      222       |
| Demon | fighter |  112  |  173  |     156      |      210       |  173  |     156     |      210       |
| Demon | rogue   |  129  |  182  |     164      |      218       |  182  |     164     |      218       |

Shelf: (1)Elf Rogue > Elf Fighter > (3)Human Rogue > (4)Demon Rogue
Full: (1)Elf Rogue > Elf Fighter > Elf Mage > Demon Mage > (5)Human Rogue > Human Fighter > Human Mage > (8)Demon Rogue

If rogues dont use Frenzy/Harmony of Maddness they become lot slower they dont lose 16 evasion and ~21%HP but they miss on 16 speed (~ +7/14 with mark)
Their sprint alows them to keep high speed more often and have jumping skill. -> after all other use their dash pots for the next 75 secs they are slower

