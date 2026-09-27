# Craft exp from MP — `BL-315` (PROPOSAL, not built)

Owner, 2026-09-27: *"the refinement need to give points for crafting ... It's hard to lvl up crafting on alloy
alone ... base the points that a craft gives by the mp it requires ... an rcp that gives 1 item is multiplied by
x5, a 100item output is x1, 2~5 is x4, 6~10 x3, 11~50 x2 and >50 x1 ... generics are x1 (refinements alloy,
volcanic etc) ... before u build give me a table per lvl what points are needed and each craft how much will
give by that formula and I'll tell u increase lvl up points by 20,50 or 100."*

**Measured, not hand-made:** `dotnet run --project tools/BalanceMatrix -- --craft-points` prints every table
below off the live `RecipeCatalog`. Re-run it after any MP change.

## The formula

`exp per attempt = MP / 10 × mult`, rounded, never below 1. A fail still pays (today's rule, kept).

| output of one attempt | mult |
|---|---|
| 1 item (every gear piece) | ×5 |
| 2-5 | ×4 |
| 6-10 | ×3 |
| 11-50 | ×2 |
| more than 50 | ×1 |
| a REFINE (Nightsilver/Nightsilk steps, Alloy, Volcanic Bar) | ×1 |

## §1 What each craft pays (NEW) vs today

| craft | gate | MP | out | mult | **NEW** | today |
|---|---|---|---|---|---|---|
| Alloy, Refined Nightsilver/-silk | L0 | 50 | 1 | ×1 | **5** | 0 |
| Rare Nightsilver/-silk | L3 | 100 | 1 | ×1 | **10** | 0 |
| Refined Rare Nightsilver/-silk | L5 | 150 | 1 | ×1 | **15** | 0 |
| Volcanic Bar, Legendary Nightsilver/-silk | L7/L8 | 200 | 1 | ×1 | **20** | 0 |
| T40 ring / gloves / shield / 1H / 2H | L0 | 25/50/100/150/200 | 1 | ×5 | **12 / 25 / 50 / 75 / 100** | 1 |
| T52 same five | L2 | 40/75/150/225/300 | 1 | ×5 | **20 / 38 / 75 / 112 / 150** | 2 |
| T61 same five | L4 | 50/100/200/300/400 | 1 | ×5 | **25 / 50 / 100 / 150 / 200** | 3 |
| T76 same five | L6 | 90/175/350/525/700 | 1 | ×5 | **45 / 88 / 175 / 262 / 350** | 5 |
| T80 same five | L8 | 100/200/400/600/800 | 1 | ×5 | **50 / 100 / 200 / 300 / 400** | 8 |
| Common HP / MP (x100) | L0 | 50 | 100 | ×1 | **5** | 1 |
| Greater Swift / Alacrity / Fury (x6) | L1 | 50 | 6 | ×3 | **15** | 1 |
| Uncommon HP / MP (x50) | L2 | 100 | 50 | ×2 | **20** | 1 |
| War / Spell Rune 1h (x3) | L4 | 200 | 3 | ×4 | **80** | 1 |
| Rare HP / MP (x10) | L6 | 200 | 10 | ×3 | **60** | 1 |
| War / Spell Rune 2h (x3) | L8 | 200 | 3 | ×4 | **80** | 1 |
| Instant Healing, Supreme Dash (x5) | L10 | 200 | 5 | ×4 | **80** | 1 |

## §2 Exp per level — today × 20 / 50 / 100

Today's ladder is `PointsForLevel(N) = 10·N·(N+1)` (each level costs 20 more than the last). Cells: the step to
the next level, with the cumulative total in brackets.

| level | today | ×20 | ×50 | ×100 |
|---|---|---|---|---|
| L0→1 | 20 [20] | 400 [400] | 1,000 [1,000] | 2,000 [2,000] |
| L1→2 | 40 [60] | 800 [1,200] | 2,000 [3,000] | 4,000 [6,000] |
| L2→3 | 60 [120] | 1,200 [2,400] | 3,000 [6,000] | 6,000 [12,000] |
| L3→4 | 80 [200] | 1,600 [4,000] | 4,000 [10,000] | 8,000 [20,000] |
| L4→5 | 100 [300] | 2,000 [6,000] | 5,000 [15,000] | 10,000 [30,000] |
| L5→6 | 120 [420] | 2,400 [8,400] | 6,000 [21,000] | 12,000 [42,000] |
| L6→7 | 140 [560] | 2,800 [11,200] | 7,000 [28,000] | 14,000 [56,000] |
| L7→8 | 160 [720] | 3,200 [14,400] | 8,000 [36,000] | 16,000 [72,000] |
| L8→9 | 180 [900] | 3,600 [18,000] | 9,000 [45,000] | 18,000 [90,000] |
| L9→10 | 200 [1,100] | 4,000 [22,000] | 10,000 [55,000] | 20,000 [110,000] |

## §3 Crafts per level-up (the craft you would naturally be making at that stage)

| craft | exp | from | ×20 | ×50 | ×100 |
|---|---|---|---|---|---|
| Alloy | 5 | L0→1 | 80 | 200 | 400 |
| T40 2H | 100 | L0→1 | 4 | 10 | 20 |
| T40 ring | 12 | L0→1 | 34 | 84 | 167 |
| Common HP x100 | 5 | L0→1 | 80 | 200 | 400 |
| T52 2H | 150 | L2→3 | 8 | 20 | 40 |
| Uncommon HP x50 | 20 | L2→3 | 60 | 150 | 300 |
| T61 2H | 200 | L4→5 | 10 | 25 | 50 |
| War Rune 1h x3 | 80 | L4→5 | 25 | 63 | 125 |
| T76 2H | 350 | L6→7 | 8 | 20 | 40 |
| T80 2H | 400 | L8→9 | 9 | 23 | 45 |

## §4 ⚠ The refines behind ONE weapon outweigh the weapon itself

Refines paid 0 until now because one weapon needs thousands of them. At ×1 they pay, and the steps stack
(10 of a rung make 1 of the next), so here is what the refines alone behind one **2H** pay, beside the
weapon's own exp. The 2H's Nightsilver: 300 normal (dropped, no refine) / 200 Refined / 150 Rare /
50 Refined Rare / 10 Legendary; plus its Alloy (10…50) and, at T76/T80, its Volcanic Bars (40 / 70).

| tier | refines behind one 2H | the 2H's own exp | refines ÷ weapon |
|---|---|---|---|
| T40 | 50 (10 alloy) | 100 | 0.5× |
| T52 | 1,100 (200 Refined + 20 alloy) | 150 | 7× |
| T61 | 9,150 (150 Rare + 1,500 Refined + 30 alloy) | 200 | 46× |
| T76 | 31,750 (50 RR + 500 Rare + 5,000 Refined + 40 alloy + 40 bars) | 350 | 91× |
| T80 | 63,350 (10 Leg + 100 RR + 1,000 Rare + 10,000 Refined + 50 alloy + 70 bars) | 400 | 158× |

So from T52 up, **the refining is the levelling, and the craft itself is a rounding error**. The curve also
turns upside down: at ×100 the early levels are slow (L0→2 is 6,000 exp, or 60 T40 2H's worth, since T40
has almost no refines), and the late ones are fast (a single T80 2H's refine chain, 63k, is more than the
whole L7→L10 stretch, 56k).

The price stays fair either way: the refine MP was paid. But if the item is meant to be what levels you,
the refines need their own weight. **My pick:** keep your ×1 for **Alloy and the Volcanic Bar** (single
steps, which is where your *"hard on alloy alone"* came from), and give the **Nightsilver/Nightsilk steps
×0.1**, one tenth, the same as the 10:1 ratio, so a refined rung pays what its share of the next one would.
That puts the T61 chain at ~1,050 and the T80 chain at ~7,800 (still more than the item, but not 158×). The
half-points (a 50-MP step at ×0.1 = 0.5) would be carried as a fraction, not rounded up to 1 per craft.

## ❓ For you

1. **Factor: ×20, ×50 or ×100?** (mine: **×50**. At ×50, L0→1 is 10 T40 2H's or 200 alloy, and L10 is
   55,000.)
2. **The refine weight (§4): ×1 as you wrote, or Nightsilver/Nightsilk at ×0.1?**

Already built in 0.214.30, independent of both answers: the crafting window shows each recipe's
**+N craft exp** and your progress **(done/needed exp)** beside your level. It reads the live numbers,
so it will follow whatever you pick.
