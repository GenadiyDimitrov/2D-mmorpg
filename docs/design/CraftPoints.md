# Craft exp — `BL-315` (PROPOSAL, round 2, not built)

Owner, 2026-09-27, round 2 (replaces the MP/10 × batch-multiplier idea of round 1, which is in git history):
*"make a curve then not each lvlup to need same amount of points.. L0->l1 to need 200 but l1 to l2 to need like
500 .. Then 7k then 30 .. Etc and the Tier to give different multiplier ... a weapon not giving 400 at t80 but 4k
or even 40k .. Each base item to have a weight (points) and based on those points a craft total to be calculated
.. So cheaper crafts give less points an a expensive one will lvlup close to lvup atleast"*.

**Measured, not hand-made:** `dotnet run --project tools/BalanceMatrix -- --craft-weights`. The weights below were
GENERATED from two authored inputs, the level curve and what each tier's 2H should pay. Once you rule, they get
pasted into the code as fixed numbers (like the essence break tables: written once, never recomputed).

## The rule

**A craft pays Σ (qty consumed × the item's weight).** Refines too, no special case. A fail still pays (it
consumed its inputs). A lower-% gear recipe consumes less, so it pays less.

How the weights were set: each tier's **2H pays about one level-step at its gate** (T40 at L0, T52 at L2, T61 at
L4, T76 at L6, T80 at L8). Base mats and alloy are fixed small numbers; what the 2H still needs is split over that
tier's own items: parts 40% / Nightsilver 30% / essence 30%, and at T76/T80 parts 35 / Nightsilver 25 /
essence 25 / Volcanic Bar 15. Every other slot follows, because its recipe is the 2H's scaled down.

## §1 The curve (your first four; the rest mine)

| level | points | cumulative |
|---|---|---|
| L0→1 | 200 | 200 |
| L1→2 | 500 | 700 |
| L2→3 | 7,000 | 7,700 |
| L3→4 | 30,000 | 37,700 |
| L4→5 | 60,000 | 97,700 |
| L5→6 | 100,000 | 197,700 |
| L6→7 | 150,000 | 347,700 |
| L7→8 | 220,000 | 567,700 |
| L8→9 | 300,000 | 867,700 |
| L9→10 | 400,000 | 1,267,700 |

## §2 The weights (points per ONE item consumed)

| item | weight |
|---|---|
| Iron / Wood / Thread / Leather | 0.1 |
| Gem | 0.2 |
| Alloy | 6 (= what the alloy consumed) |
| Volcanic Ash / Stone | 14 · Volcanic Bar 540 |

| tier | part (any kind) | Nightsilver / Nightsilk (its rung) | essence (its grade) |
|---|---|---|---|
| T40 | 3.2 | 0.16 (normal) | 0.12 (D) |
| T52 | 110 | 8.6 (Refined) | 2.1 (C) |
| T61 | 990 | 99 (Rare) | 12 (B) |
| T76 | 2,300 | 650 (Refined Rare) | 20 (A) |
| T80 | 4,900 | 7,000 (Legendary) | 35 (S) |

## §3 What each craft pays

| craft | T40 | T52 | T61 | T76 | T80 |
|---|---|---|---|---|---|
| 2H | 300 | 5,880 | 49,470 | 132,660 | 276,500 |
| 1H | 240 | 4,704 | 39,576 | 106,128 | 221,200 |
| heavy body | 180 | 3,528 | 29,682 | 79,596 | 165,900 |
| helm / necklace | 120 | 2,352 | 19,788 | 53,064 | 110,600 |
| gloves | 60 | 1,176 | 9,894 | 26,532 | 55,300 |
| ring | 30 | 588 | 4,947 | 13,266 | 27,650 |

| refine | pays | apothecary | pays |
|---|---|---|---|
| Alloy | 6 | Common HP / MP x100 | 1 / 2 |
| Refined Nightsilver | 2 | Greater buff potion x6 | 2 |
| Rare Nightsilver | 86 | Uncommon HP / MP x50 | 4 / 8 |
| Refined Rare Nightsilver | 990 | War / Spell Rune 1h x3 | 100 |
| Legendary Nightsilver | 6,500 | Rare HP / MP x10 | 42 / 84 |
| Volcanic Bar | 560 | War / Spell Rune 2h x3 | 1,220 |
| | | Instant Healing / Supreme Dash x5 | 42 / 76 |

**Refines behind one 2H vs the 2H:** T40 0.2× · T52 0.1× · T61 0.3× · T76 0.9× · T80 1.1×. Round 1's problem
(the refines paying 158× the weapon) is gone, because a refine now pays for what it consumes and the rungs'
weights follow the 10:1 ratio closely enough.

## §4 Crafts per level-up

| level | craft | crafts |
|---|---|---|
| L0→1 | T40 2H / T40 ring / Alloy | 1 / 7 / 34 |
| L1→2 | T40 2H | 2 |
| L2→3 | T52 2H | 2 |
| L3→4 | T52 2H | 6 |
| L4→5 | T61 2H / T61 ring | 2 / 13 |
| L5→6 | T61 2H | 3 |
| L6→7 · L7→8 | T76 2H | 2 · 2 |
| L8→9 · L9→10 | T80 2H | 2 · 2 |

## ⚠ Two things this exposes

1. **The Apothecary cannot level.** Potions consume gems and one or two essence, so they pay 1-8. L2→3 takes
   **1,708** Uncommon HP batches, L4→5 **600** War Rune batches. That is your *"cheaper crafts give less"*, but
   followed all the way a potion-maker can only level by making gear. Options:
   **(a)** accept it: the generic level is the smith's, and the Apothecary rides on it.
   **(b)** potions pay from their SHELF value instead of their inputs (a War Rune 1h batch is sold at 450k, a
   T61 2H at 60M, so it would pay ~1/130 of the 2H, ~370, and L4→5 would take ~160 batches).
   **(c)** an authored weight per Apothecary line.
   **Mine: (b)**, because it keeps "expensive pays more" and needs no new table.
2. **A T80 2H pays ~276k, not your "4k or even 40k".** That follows from your own curve. Once L3→4 is 30k and
   the steps keep rising, a T80 weapon has to be worth a big step, or L8→10 takes dozens of T80 weapons. If you
   want T80 near 40k, the curve has to flatten after 30k (e.g. 30k → 35k → 40k … ~60k at L9→10), and then the
   T61/T76 weapons shrink with it. Either way, the thing to decide is **how many top-tier weapons one level-up
   should take**; today's print says 2.

## ❓ For you

1. **The curve past L3→4**: mine (60k … 400k, T80 2H ≈ 280k), or flat after 30k (T80 2H ≈ 40k)?
2. **How many of the tier's 2H per level-up?** (today: 1-2 early, 2 late; changing it scales every weight, one number)
3. **The Apothecary: (a), (b) or (c)?**

Already built in 0.214.30: the crafting window shows each recipe's **+N craft exp** and your **(done/needed)**
progress, reading the live numbers, so it follows whatever is built.
