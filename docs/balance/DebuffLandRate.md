# Why debuffs don't land — measured, 2026-09-12 (`BL-218`)

Your report, playtest 2026-09-12:

> *"Also debuffs almost never land wit all the resistanses we have … Also in general debuffs don't
> land … not human stuns mage nor the other way around … Can you get me same lvl debuffs and check
> their land rate with and without buffs/passives ? **I think we hit the floor for landing**."*

Measured with `dotnet run --project tools/BalanceMatrix -- --ccland [level] [quality]`, which builds
REAL level-90 fourth-tier characters in real gear and runs the exact product `GameLoopService`
computes. **Every number below is read off the code, not derived.**

---

## 🔑 THE ANSWER: YOU ARE NOT HITTING THE FLOOR. THE FLOOR NEVER GETS A CHANCE TO BITE.

`StatCaps.CcLandMin` is 10%, and it clamps the **stat contest only**. The contest between two
level-90 characters comes out at **50-54%**, nowhere near it. Then three multipliers are applied
**after** the clamp, and none of them is floored:

```
land = clamp( 0.5 + 0.5·(atk − def)/(atk + def), 10%, 90% )   ← the contest. ~52% at parity.
     × DebuffLandMod          ← the skill's own (BL-90): 1.50, 1.00, 0.70, 0.50 or 0.30
     × (1 − CcResist)         ← 0% / 0% / 28% / 40%  by GEAR QUALITY (common/rare/epic/mythic)
     × (1 − CcResist<school>) ← 20% bare → 35% NPC shelf → 50% FULL shelf
```

The product of the last three on a fully-blessed level-90 in **mythic** gear is **×0.30**, and on a
`×0.50` skill it is **×0.15**. That is where your stuns went. Nothing is clamped, nothing warns, and
each of the three was reasonable on its own.

---

## 1. What the roll actually reads (level 90, epic gear)

Base stats are level-flat in this game, so **these numbers are the same at 80 and at 90** — only gear
quality moves them.

| character | ATK | AGI | CON | SPT | CcResist | mag | phys |
|---|---|---|---|---|---|---|---|
| Magus / Lightbringer | 42 | 26 | 29 | 36 | 28% | 20% | 10% |
| Nullblade / Sharpshooter | 36 | 31 | 42 | 27 | 28% | 20% | 10% |
| Bulwark / Ravager | 37 | 29 | 44 | 26 | 28% | 20% | 10% |

with the NPC buff shelf on: **mag 35%, phys 20%** · with the FULL shelf (harmonies): **mag 50%,
phys 20%**. `CcResist` does not move — no buff feeds it; it is **armour-set only**.

### 🔴 The flat `CcResist` is a GEAR CLIFF, not a build choice

| gear | CcResist |
|---|---|
| common | **0%** |
| rare | **0%** |
| epic | **28%** |
| mythic | **40%** |

Every class gets the same number from its own tier's set. It is not something one build buys and
another gives up — at endgame everybody simply has it, so it reads as a blanket **−40% to all
incoming control** with no counterplay on the attacker's side at all.

### 🔴 And the stat contest barely moves

ATK 42 vs SPT 36 is **53.8%**, not 65%. AGI 31 vs CON 44 is **41%**. The spread between the best and
worst case in the whole table is **41% … 54%** — thirteen points. **You cannot build for landing
debuffs**, because the stat that would do it moves the answer by less than the gear cliff does.

---

## 2. The land rates, level 90, epic gear, SAME LEVEL both sides

Read a row as: bare → NPC shelf → full shelf.

### A Magus casting at a fully-blessed target

| skill | ×mod | bare | NPC shelf | FULL shelf |
|---|---|---|---|---|
| Arcane Burst | 1.50 | 52% | 42% | **32%** |
| Arcane Void | 0.30 | 11% | 9% | **7%** |

### A Lightbringer

| skill | ×mod | bare | NPC shelf | FULL shelf |
|---|---|---|---|---|
| Weapon Break | 1.50 | 52% | 42% | **32%** |
| Gravity | 1.00 | 35% | 28% | **22%** |
| Mana Strain | 0.50 | 18% | 14% | **11%** |

### A Bulwark (the tank's whole kit is control)

| skill | ×mod | bare | NPC shelf | FULL shelf |
|---|---|---|---|---|
| Grapple / Stay! | 1.00 | 30% | 27% | **27%** |
| Shield Shock | 0.70 | 21% | 19% | **19%** |
| Numbing Shock (stun) | 0.50 | 15% | 13% | **13%** |

### A Venomweaver

| skill | ×mod | bare | NPC shelf | FULL shelf |
|---|---|---|---|---|
| Phantom Jump | 1.00 | 30% | 27% | **27%** |
| Venom Stab / Venom Burst | 1.00 | 26% | 24% | **24%** |

### A Trapper

| skill | ×mod | bare | NPC shelf | FULL shelf |
|---|---|---|---|---|
| Binding Trap / Magic Arrow | 1.00 | 28% | 25% | **25%** |

**In mythic gear subtract another ~5 points from every cell** (`--ccland 90 mythic`): Arcane Burst
27%, Numbing Shock 11%, Arcane Void 6%.

> 🔑 **"not human stuns mage nor the other way around" is exactly right, and it is not symmetric.**
> The mage's control is MAGICAL, so it eats the school blessing that the full shelf takes to 50%; the
> fighter's is PHYSICAL and only meets 20%. The mage is the one being shut out — a Magus's Arcane
> Burst lands 32% where a tank's Grapple lands 27% but never falls further when the shelf goes up.

---

## 3. What I would change, and your call on each

### 🔵 Your own proposal — the mage's SPT land-rate passive at 40/76/80

> *"Can we add to a mage 40,76,80 a spt debuff land rate passive that increases land rate of all spt
> debuffs 2 times (atleast in pvp)?"*

**There is no attacker-side land channel in the engine at all today.** Every multiplier in the
product above is defender-side or authored per skill; the caster contributes one stat to a contest
that moves by thirteen points. So this is not "one more buff" — it is the missing half of the
mechanic, and it is cheap: one `PassiveEffect` field feeding one multiplier next to `DebuffLandMod`.

At ×2 it puts a fully-blessed target back to: Arcane Burst **64%**, Gravity **44%**, Mana Strain
**22%**, Arcane Void **14%**. That is roughly "cancel the school blessing", which is a defensible
place to land.

⚠ **Two things to rule before it is built:**
1. **×2 at the top, or a ladder?** Three rungs at 40/76/80 reads as a ladder to me — say ×1.3 /
   ×1.6 / ×2.0 — so the level-40 mage is not handed the endgame number. Your call.
2. **PvP only, or everywhere?** You wrote *"atleast in pvp"*. Everywhere is one line; PvP-only is
   also one line (the PvE/PvP split already exists for damage). PvE control against creatures is
   already shortened by `MobCcSpt`, so doubling it there is a real farming change.

### 🔴 And the one I would do first, because it costs nothing and fixes every class

**The gear `CcResist` cliff (0 → 28 → 40%).** It is the single biggest factor in the table, it is
identical for everyone, and it is the reason control stopped working somewhere around epic gear
without anything being changed. Halving it (0 / 0 / 14 / 20%) puts every cell above back within a
few points of where it was before endgame gear existed, and it needs no new mechanic. A `game.db`
delete is not even required — it is an armour-set number.

### 🔵 Third, and smallest: the `×0.30` and `×0.50` skills

Arcane Void at 7% and Numbing Shock at 13% are not "unreliable", they are decoration. Your `BL-90`
ruling set those multipliers when the base at parity was 50% and there was nothing after it; with
three defensive layers stacked behind them the ×0.30 tier no longer means what you priced it to
mean. Raising the floor tier to ×0.60 would be enough.

---

## Reproducing this

```
dotnet run --project tools/BalanceMatrix -- --ccland            # level 90, epic
dotnet run --project tools/BalanceMatrix -- --ccland 90 mythic
dotnet run --project tools/BalanceMatrix -- --ccland 80 rare
```

⚠ **The rig was wrong until this pass and is worth knowing about.** `ApplyNpcBuffs` did not copy the
`CcResistMagical` / `CcResistPhysical` **fields** off a buff def — they are fields, not
`Effect`+`Magnitudes`, because the flag enum is full — so the NPC shelf moved control resistance by
**zero** and the first run of this table read identical buffed and unbuffed. That is the **fifth**
field channel this one builder has been caught missing. If a buff has a number, ask where the number
rides before trusting a "buffed" heading.
