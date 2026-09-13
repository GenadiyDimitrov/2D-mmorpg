# Contested debuff land rates — measured (`BL-218`, `BL-225`)

> **2026-09-13, third pass — the numbers below are the ones in the build now.** Your compounding
> ruling (`BL-225`), your 20% SPT ruling and your 35% CON ruling (`BL-226`) are all in, on top of the
> `BL-223` rig fix. Two earlier versions of this page had wrong numbers; this one is measured against
> the shipped build.

Measured with `dotnet run --project tools/BalanceMatrix -- --ccland [level] [quality]`, which builds
REAL level-90 fourth-tier characters in real gear and runs the exact product `GameLoopService`
computes. Every number here is read off the code.

---

## The formula, as it now is

Your ruling: *"I want the cc resist formula to be something if we have harmony 20%, buff 20%, Passive
20% -> baseLandRate x LandMod x (1-buff1/passive1) x (1-buff2/passive2) x (1-buffN/passiveN) … having
those 3 20% resists make the debuff land 2 times less (x 0.512) not ~4 (x 0.28) as it was."*

```
land = clamp( 0.5 + 0.5·(atk − def)/(atk + def), 10%, 90% )   ← the stat contest. ~52% at parity.
     × DebuffLandMod                    ← the skill's own (BL-90): 1.50 / 1.00 / 0.70 / 0.50 / 0.30
     × Π(1 − r)  over every BLANKET source        ← armour sets, shields
     × Π(1 − r)  over every SCHOOL source         ← passive, class buff, harmony, Mark
```

**Three 20% resistances are now ×0.512**, exactly as you wrote. The **80% clamps are gone** — a
product of factors below 1 can never reach 0, so the clamp had stopped being a safety net and become
a ceiling, which is the same thing you deleted from the reuse stack in 0.136.0. The only guard kept
is a sign guard against a source authored above 100%.

⚠ **Negative resistances still work and are why the retain is what gets stored**: the Magus's curses
author `CcResistMagical: -0.40`, which is simply a ×1.40 factor in the same product.

---

## 1. Where the resistance comes from (level 90)

| source | SPT (magical) | CON (physical) |
|---|---|---|
| **Strong Mind / Strong Body** — shared 4th passive @76 | 20% | 10% |
| **Arcane and Feral Protection** — Warchanter class buff | **20%** (was 50%) | 23→**35%** (was 43→65%) |
| **Harmony of the Soul** — Warchanter harmony, top rung | **20%** (was 30%) | — |
| **Holy Mark / Life Mark** | 15% | 10% |
| **Harmony Mark** | **none** | **none** |
| **Clarity / Fortitude** — the SINGLES the group covers, top rung | **20%** (was 50%) | **35%** (was 65%) |
| **compounded** (with Holy/Life Mark) | **56%** | **42%** |
| **compounded** (with Harmony Mark) | **49%** | **42%** |

Plus the school-blind `CcResist` from armour, its own factor: **0% common, 0% rare, 28% epic,
40% mythic.**

### 🔑 You were right about the Marks

*"the con/spt resists are on a single marks not on the harmony one ... Ppl will chose harmony mark"*
— confirmed in the code. **Harmony Mark carries no control resistance at all**, and all four Marks
share `MarkKey` with `FlatRank`, so taking it costs you the Holy/Life Mark's grant outright. Both
cases are measured below.

⚠ One correction to your arithmetic: the SPT Mark (Holy) is **15%**, not 10% — 10% is the CON one
(Life). So your ×0.460 is the CON case; the SPT case is ×0.435.

---
---

## 2. The land rates now — level 90, epic gear, same level both sides

### Magical (SPT-defended)

| skill | ×mod | bare | NPC shelf | full shelf | **WC + Holy Mark** | **WC + Harmony Mark** |
|---|---|---|---|---|---|---|
| Arcane Burst / Weapon Break | 1.50 | 52% | 52% | 44% | 25-28% | **30-33%** |
| Gravity | 1.00 | 35% | 35% | 30% | 17-19% | **20-22%** |
| Mana Strain | 0.50 | 18% | 18% | 15% | 8-10% | **10-11%** |
| Arcane Void | 0.30 | 11% | 11% | 9% | 5-6% | **6-7%** |

### Physical (CON-defended) — after the 35% ruling

| skill | ×mod | bare | NPC shelf | full shelf | **WC + either Mark** |
|---|---|---|---|---|---|
| Grapple / Stay! / Phantom Jump | 1.00 | 30% | 30% | 30% | **19-24%** |
| Shield Shock | 0.70 | 21% | 21% | 21% | **13-17%** |
| Numbing Shock (stun) | 0.50 | 15% | 15% | 15% | **10-12%** |

---

## 3. ✅ BOTH SCHOOLS ARE IN YOUR BAND

| ×1.00 skill, fully buffed | before this pass | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-23%** |
| physical (CON) | 8-10% | **19-24%** |

Inside your *"15-25% which is good"*, and within a couple of points of each other for the first time.
The `×1.50` skills sit a little above at ~30%, which reads correct — they are the ones you priced to
be reliable.

---

## 4. The two profile tables (`--ccprofile`)

One row per DEFENDER, one column per layer of the product. `+set` is gear and the character's own
passives; `+buffed` is the full party stack with the Harmony Mark.

### A. A `×1.00` MAGIC debuff, cast by a same-level Magus

| defender | SPT | mRes | base | +set | +buffed | *× mRes (proposal)* |
|---|---|---|---|---|---|---|
| Magus (mage) | 36 | 35% | 53.8% | 31.0% | **19.8%** | *12.9%* |
| Bulwark (tank) | 26 | 21% | 61.8% | 35.6% | **22.8%** | *17.9%* |
| Nullblade | 27 | 10% | 60.9% | 35.1% | **22.4%** | *20.2%* |
| Nullblade **+ Magical Armor** (10s) | 27 | 60% | 60.9% | — | **22.4%** | *9.0%* |

### B. A STUN (`×1.00`), cast by a same-level Bulwark

| defender | CON | BRes | base | +set | +buffed | *was (65%)* | *× BRes* |
|---|---|---|---|---|---|---|---|
| Magus (mage) | 29 | 80% | 56.1% | 36.3% | **23.6%** | *12.7%* | *4.7%* |
| Venomweaver (rogue) | 46 | 80% | 44.6% | 28.9% | **18.8%** | *10.1%* | *3.8%* |
| Ravager (warrior) | 44 | 80% | 45.7% | 29.6% | **19.2%** | *10.4%* | *3.8%* |

`BRes` = **Battle Resilience** at its top rung — 80% CON *and* SPT, 60s on a 150s reuse. A button,
not a standing buff, so it is its own column. Numbing Shock's own `×0.50` halves the `+buffed` cell.

---

## 5. ❓ Should magic RESISTANCE also resist magic debuffs?

> *"I wonder just logically shouldnt mresist add to magic debuffs resistance? that way a tank and a
> nullblade(for 10s) will have aditional anti magic. - like endLandRate x 0.3(30% mresist)"*

Measured above as the `× mRes` column, **not built**. It does what you want for the two classes you
named: the Nullblade's ultimate becomes a real ten-second control-immunity window (22.4% → **9.0%**)
and the tank picks up a few points.

🔴 **But the MAGE has the most magic resistance of the three (35%)**, so he would end up the hardest
of all to land a magic debuff on — backwards from every other line in this design, where the mage is
the one who gave up CON/SPT to buy offence. His 35% comes from the nuker's own `anti_magic` passive
ladder, which exists to survive *nukes*, not curses.

**If you want this, it reads better as mResist from BUFFS and ULTIMATES only, not from passives** —
which is exactly the Nullblade-and-tank case you described, and leaves the mage's anti-nuke passive
out of it. One line either way.

## 🔵 Also still open

- **The flat `CcResist` gear cliff** — 0% common, 0% rare, **28% epic, 40% mythic**. Armour-set only,
  identical for every class, nothing on the attacker's side answers it. Now the largest single term
  left in the product.
- ⚠ **Battle Resilience is the biggest number in the file** — 80% for 60s of every 150s takes a stun
  to 3.8%, near-immunity 40% of the time. It was already dominant when resistances summed (it alone
  reached the old 0.8 clamp); compounding made it cleaner, not smaller. Not touched.

---

## Reproducing this

```
dotnet run --project tools/BalanceMatrix -- --ccland            # the per-skill matrix
dotnet run --project tools/BalanceMatrix -- --ccprofile         # the two tables above
dotnet run --project tools/BalanceMatrix -- --ccland 90 mythic
CCDEBUG=1 dotnet run --project tools/BalanceMatrix -- --ccland  # itemise every resist source
```

⚠ **The rig was caught twice in two days on this one table** — `BL-218` (the `CcResistMagical` /
`CcResistPhysical` buff FIELDS were never copied, so the shelf moved resistance by zero) and `BL-223`
(`NewbieBuffSet` already contains the harmonies and Marks, so `fullShelf` concatenated them again and
every "buffed" row wore four Marks). Same lesson twice: **this tool bolts BuffInstances on directly
instead of going through `ApplyBuff`, so every rule `ApplyBuff` enforces — field payloads, covering,
one-buff-per-key — has to be taught to it separately.** `CCDEBUG=1` exists because a summed stat
sitting on its clamp hides how many addends there were.
