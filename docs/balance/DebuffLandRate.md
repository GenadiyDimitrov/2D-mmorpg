# Contested debuff land rates — measured (`BL-218`, `BL-225`)

> **2026-09-13, rewritten again — the numbers below are the ones in the build now.** Your
> compounding ruling (`BL-225`) is in, on top of your `20%` SPT ruling and the `BL-223` rig fix.

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
| **Arcane and Feral Protection** — Warchanter class buff | **20%** (was 50%) | 43→**65%** |
| **Harmony of the Soul** — Warchanter harmony, top rung | **20%** (was 30%) | — |
| **Holy Mark / Life Mark** | 15% | 10% |
| **Harmony Mark** | **none** | **none** |
| **compounded** (with Holy/Life Mark) | **56%** | **68%** |
| **compounded** (with Harmony Mark) | **49%** | **68%** |

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

## 2. The land rates now — level 90, epic gear, same level both sides

### Magical (SPT-defended)

| skill | ×mod | bare | NPC shelf | full shelf | **WC + Holy Mark** | **WC + Harmony Mark** |
|---|---|---|---|---|---|---|
| Arcane Burst / Weapon Break | 1.50 | 52% | 52% | 44% | 25-28% | **30-33%** |
| Gravity | 1.00 | 35% | 35% | 30% | 17-19% | **20-22%** |
| Mana Strain | 0.50 | 18% | 18% | 15% | 8-10% | **10-11%** |
| Arcane Void | 0.30 | 11% | 11% | 9% | 5-6% | **6-7%** |

### Physical (CON-defended)

| skill | ×mod | bare | NPC shelf | full shelf | **WC + either Mark** |
|---|---|---|---|---|---|
| Grapple / Stay! / Phantom Jump | 1.00 | 30% | 30% | 30% | **10-13%** |
| Venom Stab / Venom Burst | 1.00 | 26% | 26% | 26% | **9-11%** |
| Binding Trap / Magic Arrow | 1.00 | 28% | 28% | 28% | **10-11%** |
| Shield Shock | 0.70 | 21% | 21% | 21% | **7-9%** |
| Numbing Shock (stun) | 0.50 | 15% | 15% | 15% | **5-6%** |

---

## 3. ✅ Your band is hit on the magical side

An ordinary `×1.00` magical debuff against a fully-buffed target now lands **20-22%** with the
Harmony Mark most people will wear, **17-19%** with a Holy Mark. That is inside your *"15-25% which
is good"*. The `×1.50` skills sit a little above it at 30-33%, which reads correct — they are the
ones you priced to be reliable.

### 🔴 But the two schools are now TWICE as far apart, and CON is the outlier

You ruled on SPT only, so the CON stack still carries **Feral Protection's 43→65%**. Compounded with
Strong Body and a Mark that is **68%**, against the SPT side's 49-56%. Result:

| | ×1.00 skill lands |
|---|---|
| magical (SPT) | **20-22%** |
| physical (CON) | **10-13%** |

So the tank's kit — Grapple, Stay!, Shield Shock, Numbing Shock — and the Venomweaver's and the
Trapper's are all at roughly half the reliability of the mage's, and the stun that ends a fight is at
**5-6%**. 🔵 **The lever is one number: Feral Protection's CON column, which is yours.** Bringing its
top rung from 65% to ~25% would put both schools on the same footing; anything in between moves it
proportionally. I have not touched it — it is your authored CSV and your message was about SPT
throughout.

### 🔵 Also still open, unchanged

- **The flat `CcResist` gear cliff** — 0% / 0% / 28% / 40% by quality, armour-set only, identical for
  every class, and nothing on the attacker's side answers it. It is the ×0.72 (epic) / ×0.60 (mythic)
  factor sitting on top of everything above. Now that the school stack compounds, this is the largest
  single term left.
- **There is no attacker-side land channel in the engine at all** — your mage SPT passive is the
  missing half of the mechanic. Still needs: **ladder or flat ×2** across 40/76/80, and **PvP-only or
  everywhere**. With the magical side now landing 20%, it may simply not be needed.

---

## Reproducing this

```
dotnet run --project tools/BalanceMatrix -- --ccland            # level 90, epic
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
