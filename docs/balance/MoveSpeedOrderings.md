# Move speed — your two formula orderings, measured (`BL-238`)

**2026-09-16.** You asked for the table before the ruling:

> *"every mark should decrease speed with 20% -> the move speed of chars with all the buffs is + 69
> and make all over 200.. And this values should be reserved for rogues ... I just don't know the
> formula we should use - can u make me tables with bot formulas below and : race,
> mage/fighter/rogue(with armor passive), base, without mark, formula1 with mark, formula 2 with
> mark, +60(sprint)"*

| | ordering |
|---|---|
| **F1** | `(base × buffs) × debuffs + flat` — the flat shelf survives the cut untouched |
| **F2** | `(base × buffs + flat) × debuffs` — the cut eats the flat shelf too |

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

| source | flat | pct |
|---|---:|---:|
| SpeedTable base (Human fighter) | 115 | — |
| gear + armour passives (folded into BASE) | +0 (heavy) … +17 (light rogue) | — |
| Swift (NPC single) | +33 | — |
| Frenzy | +8 | — |
| Harmony of Swift | +20 | — |
| **Holy Mark** | 0 | **+20%** |
| **= the shelf** | **+61** | **+20%** |

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

| race | role | raw | base | shelf | NO MARK | TODAY | F1 | F2 | +sprint TODAY | +sprint F1 | +sprint F2 |
|---|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| Human | mage | 109 | 114 | ×1.00 +61 | 175 | 198 | 170 | 158 | 250\* | 230 | 206 |
| Human | fighter | 115 | 115 | ×1.00 +61 | 176 | 199 | 171 | 159 | 250\* | 231 | 207 |
| Human | rogue | 115 | 132 | ×1.00 +61 | 193 | 220 | 188 | 176 | 250\* | 248 | 224 |
| Elf | mage | 114 | 119 | ×1.00 +61 | 180 | 204 | 175 | 163 | 250\* | 235 | 211 |
| Elf | fighter | 143 | 143 | ×1.00 +61 | 204 | 233 | 198 | 186 | 250\* | 250\* | 234 |
| Elf | rogue | 143 | 161 | ×1.00 +61 | 222 | 250\* | 216 | 203 | 250\* | 250\* | 250\* |
| Demon | mage | 113 | 118 | ×1.00 +61 | 179 | 203 | 174 | 162 | 250\* | 234 | 210 |
| Demon | fighter | 112 | 112 | ×1.00 +61 | 173 | 195 | 169 | 156 | 250\* | 229 | 204 |
| Demon | rogue | 112 | 129 | ×1.00 +61 | 190 | 216 | 185 | 173 | 250\* | 245 | 221 |

## 3. READING B — the Mark's +20% becomes −20%

| race | role | raw | base | shelf | NO MARK | TODAY | F1 | F2 | +sprint TODAY | +sprint F1 | +sprint F2 |
|---|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|
| Human | mage | 109 | 114 | ×1.00 +61 | 175 | 198 | 152 | 140 | 250\* | 212 | 188 |
| Human | fighter | 115 | 115 | ×1.00 +61 | 176 | 199 | 153 | 141 | 250\* | 213 | 189 |
| Human | rogue | 115 | 132 | ×1.00 +61 | 193 | 220 | 167 | 155 | 250\* | 227 | 203 |
| Elf | mage | 114 | 119 | ×1.00 +61 | 180 | 204 | 156 | 144 | 250\* | 216 | 192 |
| Elf | fighter | 143 | 143 | ×1.00 +61 | 204 | 233 | 175 | 163 | 250\* | 235 | 211 |
| Elf | rogue | 143 | 161 | ×1.00 +61 | 222 | 250\* | 190 | 178 | 250\* | 250 | 226 |
| Demon | mage | 113 | 118 | ×1.00 +61 | 179 | 203 | 155 | 143 | 250\* | 215 | 191 |
| Demon | fighter | 112 | 112 | ×1.00 +61 | 173 | 195 | 151 | 138 | 250\* | 211 | 186 |
| Demon | rogue | 112 | 129 | ×1.00 +61 | 190 | 216 | 164 | 152 | 250\* | 224 | 200 |

## 4. What a Mark costs you, against no Mark at all

| race | role | today | A: F1 | A: F2 | B: F1 | B: F2 |
|---|---|---:|---:|---:|---:|---:|
| Human | mage | +22.8 | −4.6 | −16.8 | −22.8 | −35.0 |
| Human | fighter | +23.0 | −4.6 | −16.8 | −23.0 | −35.2 |
| Human | rogue | +26.5 | −5.3 | −17.5 | −26.5 | −38.7 |
| Elf | mage | +23.8 | −4.8 | −17.0 | −23.8 | −36.0 |
| Elf | fighter | +28.6 | −5.7 | −17.9 | −28.6 | −40.8 |
| Elf | rogue | +32.2 | −6.4 | −18.6 | −32.2 | −44.4 |
| Demon | mage | +23.6 | −4.7 | −16.9 | −23.6 | −35.8 |
| Demon | fighter | +22.4 | −4.5 | −16.7 | −22.4 | −34.6 |
| Demon | rogue | +25.9 | −5.2 | −17.4 | −25.9 | −38.1 |

**Reading A + F1 is the weakest lever on the board: about −5 points.** Reading B + F2 is the
strongest: −35 to −44.

---

## 5. 🔑 The part you should read before ruling: the ordering does NOT reserve the band

How far the rogue sits above the mage **of his own race**, buffed, no sprint:

| race | no mark | today | A: F1 | A: F2 | B: F1 | B: F2 |
|---|---:|---:|---:|---:|---:|---:|
| Human | 18.4 | 22.1 | 17.7 | 17.7 | 14.7 | 14.7 |
| Elf | 42.0 | 50.4 | 40.3 | 40.3 | 33.6 | 33.6 |
| Demon | 11.4 | 13.6 | 10.9 | 10.9 | 9.1 | 9.1 |

**F1 and F2 give the identical gap.** They have to: both multiply the same base difference by the
same factors, and the flat shelf is the same +61 on every row, so it cancels out of a difference.
A cut that lands on everyone lowers everyone — it cannot hand the top of the band to anyone. Every
version of the Mark cut makes the rogue's lead **smaller**, not bigger.

What actually closed the band is the flat shelf, because a flat +61 is worth more to a slow
character than to a fast one:

| race | role | base | buffed | gain | × base |
|---|---|---:|---:|---:|---:|
| Human | mage | 114 | 198 | +84 | 1.74 |
| Human | fighter | 115 | 199 | +84 | 1.73 |
| Human | rogue | 132 | 220 | +87 | 1.66 |
| Elf | mage | 119 | 204 | +85 | 1.71 |
| Elf | fighter | 143 | 233 | +90 | 1.63 |
| Elf | rogue | 161 | 254 | +93 | 1.58 |
| Demon | mage | 118 | 203 | +85 | 1.72 |
| Demon | fighter | 112 | 195 | +83 | 1.74 |
| Demon | rogue | 129 | 216 | +87 | 1.67 |

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
