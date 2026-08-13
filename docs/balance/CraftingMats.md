# Crafting materials — the faucet, measured per GRADE GROUP

**Owner, 2026-08-13:** *"make me a balance matrix file with all the dropped mats for a lvl .. a kills/h +
the drops of mats and rarity .. in each grade group .. so we can decide the mats consumption per item ...
now looking at it 1000-2000 legend mats for a single 75% chance to fail mytiic S is a bit harsh ... ofc
depending on the mats drop"*

This is that file. It **blocks the `BL-05` recipe costs** — the mat quantities in
`docs/design/CraftingProfessions.md` §5c were all given as *"depending on drop rates/amount"*, i.e. as
ranges to be resolved by measurement, and this is the measurement.

> ⚠ **Do not read the numbers below as fixed.** They are printed by `tools/BalanceMatrix` (section `§M`)
> from the live drop tables and the live refine recipes. Change `MobCatalog.StandardDrops`,
> `RateConfig.DropGroupRates` or `Recipes.cs` and **re-run the tool** rather than editing this file:
>
> ```
> dotnet run --project tools/BalanceMatrix
> ```
>
> Everything here goes through `MobCatalog.EffectiveChance`, so it is the same number the kill roll uses
> and the same number target-inspect shows the player. Nothing is hand-multiplied.

---

## The three-sentence answer

1. **His instinct was right, and it was conservative.** The ladder does not break at S — it breaks at **B**.
2. **Legendary and Mythic materials do not drop. At all. From anything.** The only source is refining, at
   7-in-1-out, on top of an Epic mat that itself only drops from level 76 at 0.015/kill. That single fact
   is what makes the top three rungs cost years instead of hours.
3. **The bottom half of his ladder is already correct.** Solve E, D and C against a sane target and the
   answer lands *inside the ranges he already wrote*. Only B, A and S need a ruling.

---

## §1 · How fast anyone actually farms

`kills/h = 3600 / (TTK + 40s)`. The 40 seconds is walking, respawn and retarget, and it is **calibrated,
not assumed**: it is what makes the model reproduce the playtest-18 mage — 350k of pure coin over a 14.5 h
idle farm at level 34, i.e. ~84 kills/h. Time-to-kill is 4-11 seconds across the whole game, so **combat is
not the farm's clock; walking is.** That is why kills/h barely moves from band to band (81 → 70).

## §2 · What a kill yields, per grade band

Row = the band's **top** level, i.e. its best case. The mat rarity gates sit at **30 / 60 / 76**
(uncommon / rare / epic), so the *bottom* of E, B and A yield less than the row shows.

```
 gr   levels                  mob    TTK  kills/h |     Common  Uncommon      Rare      Epic Legendary    Mythic
  F     1-19       Skeleton Grunt   4.4s       81 |       1.76         0         0         0         0         0
  E    20-39 Fen Lizardman Archer   4.4s       81 |       1.76      0.39         0         0         0         0
  D    40-51     Marauder Warrior   5.9s       78 |       1.76      0.39         0         0         0         0
  C    52-60          Sand Ratman   7.2s       76 |       1.76      0.39      0.09         0         0         0
  B    61-75  Sunland Orc Warrior  11.2s       70 |       1.76      0.39      0.09         0         0         0
  A    76-79      Emberwyrm Drake  10.3s       72 |       1.76      0.39      0.09     0.015         0         0
  S    80-85 Disciple of the Dawn   7.9s       75 |       1.76      0.39      0.09     0.015         0         0
```

Per HOUR:

```
 gr   levels  kills/h |     Common  Uncommon      Rare      Epic Legendary    Mythic
  F     1-19       81 |      142.6         0         0         0         0         0
  E    20-39       81 |     142.48     31.57         0         0         0         0
  D    40-51       78 |     137.88     30.55         0         0         0         0
  C    52-60       76 |     134.16     29.73      6.86         0         0         0
  B    61-75       70 |     123.68     27.41      6.32         0         0         0
  A    76-79       72 |     125.85     27.89      6.44      1.07         0         0
  S    80-85       75 |     131.97     29.24      6.75      1.12         0         0
```

🔴 **The Legendary and Mythic columns are zero in every band.** No creature in the game drops either.

⚠ These are all five material *types* together. The mats group splits three ways by mob **category** (two
flavored types + Gem), so any one type is about a third of the Common column — and a refine's two CROSS
inputs come from a different creature family or from trade. That is by design (it is what forces trade),
but it means a solo crafter's real rate is lower than the totals above.

## §3 · What one material costs, in kills

The cheaper of *farming it* and *refining it out of the rung below* — read off the real recipes (5 of
itself + 2 cross today), never a hardcoded 7.

```
 gr   levels |     Common  Uncommon      Rare      Epic Legendary    Mythic   (kills per 1 mat)
  F     1-19 |          1         4        28       195     1 364     9 549
  E    20-39 |          1         3        18       126       879     6 156
  D    40-51 |          1         3        18       126       879     6 156
  C    52-60 |          1         3        11        78       544     3 811
  B    61-75 |          1         3        11        78       544     3 811
  A    76-79 |          1         3        11        67       467     3 267
  S    80-85 |          1         3        11        67       467     3 267
```

**Refining is never the cheap path for anything that drops.** The drop ladder thins by ~4-6× a rung while a
refine costs 7-in-1-out, so farming always wins where a faucet exists. Above Epic none does, so Legendary is
a forced ×7 and Mythic a forced ×49 on the rarest thing in the table: **467 kills for one Legendary mat,
3,267 for one Mythic**, at the best farm in the game.

## §4 · His authored ranges, priced in farm hours

Per craft **attempt** (before the fail table):

```
 rung                          recipe (his ranges)               hours @ own band               hours @ 85
E (20+)             500-1000 Common + 10-10 Uncommon                  3.8 h - 7.3 h            4.1 h - 7.9 h
D (40+)                  100-500 Uncommon + 2-5 Rare                   3.7 h - 18 h             3.7 h - 18 h
C (52+)                      100-200 Rare + 1-2 Epic                    16 h - 31 h              16 h - 31 h
B (61+)                 100-200 Epic + 1-2 Legendary                  118 h - 237 h             95 h - 190 h
A (76+)               100-200 Legendary + 1-2 Mythic                698 h - 1 397 h          666 h - 1 332 h
S (80+)           1000-2000 Legendary + 10-20 Mythic     6 659 h - 13 319 h (1.5 y) 6 659 h - 13 319 h (1.5 y)
```

And after the fail table — **a fail eats the mats, so the sticker price is not the price**:

```
 rung   fail  attempts  ->Mythic |             per success (own band)          per MYTHIC (own band)
    E    10%      1.1x      2.0x |                      4.3 h - 8.2 h                   7.7 h - 15 h
    D    15%      1.2x      2.2x |                       4.4 h - 21 h                   8.3 h - 39 h
    C    20%      1.2x      2.5x |                        19 h - 39 h                    39 h - 78 h
    B    30%      1.4x      3.3x |                      169 h - 338 h                  395 h - 790 h
    A    50%      2.0x      5.0x |                  1 397 h - 2 793 h              3 492 h - 6 983 h
    S    75%      4.0x     20.0x | 26 638 h (3.0 y) - 53 276 h (6.1 y) 133 189 h (15.2 y) - 266 379 h (30.4 y)
```

`->Mythic` is `1/P(mythic)`: a craft that *succeeds* still lands on Legendary most of the time, and at S it
does so 19 times out of 20. **A Mythic S item is 15 to 30 years of continuous farming.** His *"a bit harsh"*
was an understatement by about four orders of magnitude.

## §5 · The counter-proposal

His six ranges all share **exactly one shape: 100 bulk to 1 accent**, in every rung, at both ends of every
range. So the ladder has one free number per rung and pricing it is a solve, not a redesign. Target below =
5 h for an E item, doubling per rung to 160 h for an S — **that curve is my proposal and the one number here
worth arguing with.** Everything else is the drop table doing arithmetic.

```
 rung  target/success              his range    solved  accent                 verdict
    E              5h        500-1000 Common       613       6        INSIDE his range
    D             10h       100-500 Uncommon       243       2        INSIDE his range
    C             20h           100-200 Rare       103       1        INSIDE his range
    B             40h           100-200 Epic        24       1  8x smaller — shape breaks
    A             80h      100-200 Legendary         6       1  35x smaller — shape breaks
    S            160h    1000-2000 Legendary         6       1  333x smaller — shape breaks
```

🔑 **E, D and C land inside his own authored ranges.** The bottom half needs nothing from me except picking
the number inside the range he already wrote — **613 / 243 / 103**, with 6 / 2 / 1 accent mats.

🔴 **B, A and S all solve below 100 bulk**, at which point the 100:1 shape stops being expressible — there is
no longer a pile for the accent to accent. Those three rungs are not mis-numbered; **they are un-authorable
at the current faucet.**

⚠ **S solves to the same pile as A despite twice the target.** Its 75% fail rate eats the entire doubling on
its own. The fail table and the mat cost are one knob, not two — moving either moves the same number.

## §6 · The two levers, and which one I would pull

**Lever 1 — open a faucet at the top.** For the authored quantities to survive, at level 85:

```
 rung  target/attempt  kills available   Legendary/kill    Mythic/kill
    A             20h            1 500           0.1778         0.0053
    A             50h            3 749           0.0711         0.0021
    A            100h            7 498           0.0356         0.0011
    S             20h            1 500           1.7782         0.0533
    S             50h            3 749           0.7113         0.0213
    S            100h            7 498           0.3556         0.0107
```

For scale: the current top of the ladder is the Epic mat at **0.015/kill**, and Common — the most abundant
thing in the game — runs at **1.76/kill**. The S row asks a *Legendary* mat to drop **more often than once
per kill**. That is the arithmetic saying the faucet alone cannot carry his quantities.

**Lever 2 — cut the quantities**, which is §5.

**My recommendation: both, weighted toward the faucet.** Cutting quantities alone gets to "6 Legendary mats
for an S item", which is numerically fine and *feels* like nothing — the whole point of an S craft is that it
is an undertaking. Giving Legendary and Mythic mats a real source is also the change that fits what already
exists: **the top enchant scrolls solved this exact problem in D1** by making elites and bosses the source
once the normal-mob faucet closed (`MobCatalog.EnchantScrollDrops`). Legendary and Mythic mats want the same
treatment — a boss/elite drop at 76+ — and then the quantities can stay large enough to feel like a project.

**Still owed from him** (nothing below should be authored first):

- the target cost curve (§5) — is 5 h → 160 h per finished item the feel he wants?
- whether Legendary/Mythic mats get an elite/boss faucet, or the quantities take the whole cut
- 🔑 the fail table and the mat cost are **one** knob (§5 ⚠). If S keeps 75%, the pile must be 4× smaller
  than the target implies; if the pile stays, the fail rate has to come down.

---

*Generated by `tools/BalanceMatrix` §M (`M1`-`M7`). Related: `docs/design/CraftingProfessions.md` §5c/§5d,
`docs/design/Crafting.md`, the `BL-05` entry in `docs/Backlog.md`.*
