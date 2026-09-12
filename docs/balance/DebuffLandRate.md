# Why debuffs don't land — measured (`BL-218`)

> **2026-09-13 — THIS PAGE WAS REWRITTEN AND THE FIRST VERSION'S SHELF NUMBERS WERE WRONG.** Two
> things changed: your `20%` ruling on the SPT stack is now in the build, and `BL-223` fixed the rig,
> which had been dressing every "buffed" character in **four Marks and sixteen harmonies** where the
> game allows one Mark and eight. Everything below is re-measured.

Your report, playtest 2026-09-12:

> *"Also debuffs almost never land wit all the resistanses we have … not human stuns mage nor the
> other way around … Can you get me same lvl debuffs and check their land rate with and without
> buffs/passives ? **I think we hit the floor for landing**."*

`dotnet run --project tools/BalanceMatrix -- --ccland [level] [quality]` builds REAL level-90
fourth-tier characters in real gear and runs the exact product `GameLoopService` computes.

---

## 🔑 YOU ARE NOT HITTING THE FLOOR. THE FLOOR NEVER GETS A CHANCE TO BITE.

`StatCaps.CcLandMin` is 10%, and it clamps the **stat contest only** — which between two level-90
characters comes out at **50-54%**, nowhere near it. Three multipliers are applied **after** the
clamp and none of them is floored:

```
land = clamp( 0.5 + 0.5·(atk − def)/(atk + def), 10%, 90% )   ← ~52% at parity
     × DebuffLandMod          ← the skill's own (BL-90): 1.50, 1.00, 0.70, 0.50 or 0.30
     × (1 − CcResist)         ← 0% / 0% / 28% / 40%  by GEAR QUALITY. Armour sets only.
     × (1 − CcResist<school>) ← SUMMED from passive + buff + harmony + Mark, clamped at 80%
```

---

## 1. Where the school resistance comes from (level 90, both schools)

| source | SPT (magical) | CON (physical) |
|---|---|---|
| **Strong Mind / Strong Body** — shared 4th passive @76 | 20% | 10% |
| **Arcane and Feral Protection** — Warchanter class buff | ~~50%~~ → **20%** | 43→65% |
| **Harmony of the Soul** — Warchanter class harmony, top rung | ~~30%~~ → **20%** | — |
| **Holy Mark / Life Mark** | 15% | 10% |
| **summed, clamped at 80%** | ~~100%→80%~~ → **75%** | **75%** |

Plus the school-blind `CcResist` from armour: **0% common, 0% rare, 28% epic, 40% mythic.**

### 🔴 Before your ruling the SPT stack was ON THE 80% CLAMP

20 + 50 + 30 + 15 = **100%, clamped to 80**. That is the same trap as the reuse clamp you killed in
0.136.0: sitting on a ceiling, so retuning any ONE of the four would have moved **nothing**. Your fix
works only because it changed *two* of them at once — dropping the harmony alone would still have
summed to 85 and still clamped to 80.

---

## 2. The land rates now, level 90, epic gear, SAME LEVEL both sides

Rows: bare → NPC shelf → full shelf (harmonies + a Mark) → **a real party Warchanter**.

### Magical (SPT-defended)

| skill | ×mod | bare | shelf | full | **vs a Warchanter** |
|---|---|---|---|---|---|
| Arcane Burst / Weapon Break | 1.50 | 52% | 52% | 42% | **15-16%** |
| Gravity | 1.00 | 35% | 35% | 28% | **10-11%** |
| Mana Strain | 0.50 | 18% | 18% | 14% | **5%** |
| Arcane Void | 0.30 | 11% | 11% | 9% | **3%** |

### Physical (CON-defended)

| skill | ×mod | bare | shelf | full | **vs a Warchanter** |
|---|---|---|---|---|---|
| Grapple / Stay! / Phantom Jump | 1.00 | 30% | 30% | 30% | **8-10%** |
| Venom Stab / Venom Burst | 1.00 | 26% | 26% | 26% | **7-9%** |
| Binding Trap / Magic Arrow | 1.00 | 28% | 28% | 28% | **8-9%** |
| Shield Shock | 0.70 | 21% | 21% | 21% | **6-7%** |
| Numbing Shock (stun) | 0.50 | 15% | 15% | 15% | **4-5%** |

**In mythic gear subtract roughly another fifth from every "vs a Warchanter" cell.**

---

## 3. 🔴 YOUR TARGET IS NOT REACHED, AND HERE IS EXACTLY WHY

> *"I calculated we must do the harmony and buff also be 20% (not 30/50) that way the land rate will
> be 15-25% which is good"*

Your numbers are in the build. The band is not: **only your best skill reaches it** (15-16% on a
×1.50 mod) and the ordinary ×1.00 skills land **10-11% magical, 8-10% physical**. Two reasons, both
arithmetic rather than opinion:

**1. You counted three sources; there are four.** A **Mark** carries 15% SPT and 10% CON resistance
too. Your 20 + 20 + 20 = 60 is really **75**.

**2. They SUM in the engine; you multiplied.** Your `(1−.2)(1−.2)(1−.2)` = ×0.512 is the
multiplicative answer, and it is the generous one. Summing to 75% gives **×0.25** — less than half as
forgiving.

### Two ways to reach your band. My pick is the first.

**(a) Make school resistances COMPOUND instead of summing.** This is your own 0.136.0 ruling —
*"Make it mutiolicative if u haven't as any other buff is"* — applied to the same shape, and it takes
the 80% clamp out of reach for free, exactly as it did for reuse.

```
(1−.20)(1−.20)(1−.20)(1−.15) = ×0.435 , × (1−.28 set) = ×0.313
    →  ×1.00 skills land 16%,  ×1.50 skills 24%,  ×0.50 skills 8%
```

**That is your 15-25% band almost exactly.** ⚠ It reaches the CON side too, where the summed 75%
becomes ×0.315 — a real loosening for tanks, and the point at which your authored 43→65% CON column
would want a second look.

**(b) Cut further under the current summing.** To land ×1.00 skills at ~18% the four SPT sources have
to total ~50% rather than 75%. That means taking the Mark's 15% out or halving the passive as well —
more numbers moved, and still on the same brittle summing rule.

### 🔵 Still open from before, and unchanged by this pass

- **The flat `CcResist` gear cliff** — 0% / 0% / 28% / 40% by quality, armour-set only, identical for
  every class, nothing on the attacker's side answers it. It is a ×0.72 (epic) or ×0.60 (mythic)
  blanket sitting on top of everything above.
- **There is no attacker-side land channel in the engine at all**, which is why your SPT-passive idea
  is the missing half of the mechanic rather than one more buff. Still needs: **ladder or flat ×2**
  across 40/76/80, and **PvP-only or everywhere**.

---

## Reproducing this

```
dotnet run --project tools/BalanceMatrix -- --ccland            # level 90, epic
dotnet run --project tools/BalanceMatrix -- --ccland 90 mythic
CCDEBUG=1 dotnet run --project tools/BalanceMatrix -- --ccland  # itemise every resist source
```

⚠ **The rig has now been caught twice in two days on this one table** — `BL-218` (the
`CcResistMagical`/`CcResistPhysical` buff FIELDS were never copied, so the shelf moved resistance by
zero) and `BL-223` (`NewbieBuffSet` already contains the harmonies and Marks, so `fullShelf`
concatenated them a second time and every "buffed" row wore four Marks). Both are the same lesson in
different clothes: **this tool bolts BuffInstances on directly instead of going through `ApplyBuff`,
so every rule `ApplyBuff` enforces — field payloads, covering, one-buff-per-key — has to be taught to
it separately.** `CCDEBUG=1` exists because a summed stat sitting on its clamp hides how many addends
there were.
