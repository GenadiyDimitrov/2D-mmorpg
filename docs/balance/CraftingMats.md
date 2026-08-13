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

- ~~the target cost curve (§5)~~ — **ANSWERED 2026-08-13, see §7.**
- whether Legendary/Mythic mats get an elite/boss faucet, or the quantities take the whole cut
- 🔑 the fail table and the mat cost are **one** knob (§5 ⚠). If S keeps 75%, the pile must be 4× smaller
  than the target implies; if the pile stays, the fail rate has to come down.

---

# §7 · HIS RULING, 2026-08-13 — the target curve, armor/jewel fractions, consumables

> ✅ **§7 is MEASURED** — `tools/BalanceMatrix` sections **`M8`-`M11`**, added for exactly this ruling.
> Re-run the tool rather than editing the numbers below.
>
> Three of the five open questions §7d first listed came back with answers that **contradicted my
> estimates**, so read §7e before acting on anything in this file: **elites can carry a faucet after all**,
> **potion uptime is 3-4× over his target already**, and **the enchant faucet is 3.6-18.6 h and hits ZERO
> at the S band**.

## §7a · What he ruled

**Weapon craft cost, per FINISHED weapon** (his words, 2026-08-13):

> *"2-3h of farming for E grade per weapon craft, 3-5h per D grade, 5-10 C, 12-1d B, 1-3d A, 7-14d S …
> 1d of farming to mean the full 12h (auto+offline) — so a 1-3d A grade to be a wall clock of 12-36h of
> non stop farming — that seems fair (atleast for now) — to craft a single weapon"*

🔑 **A "day" is 12 farm hours, not 24.** The auto+offline allowance is the unit. So the ladder in farm hours
is **E 2-3 · D 3-5 · C 5-10 · B 12-24 · A 12-36 · S 84-168**.

⚠ `12-1d` for B is ambiguous — under his own definition 1d *is* 12 h, so the range collapses. It does not
change the answer (12 h and 24 h both solve to 7-14 Epic), but it is the one number to re-confirm.

**Armor and jewels are FRACTIONS of the weapon**, authored so a full set costs exactly one weapon:

```
 armor (4 slots)   gloves WH/10   boots WH/10   helmet WH/3.33   body WH/2     sum = 1.000 WH
 jewels (5 slots)  ring   WH/10   earring WH/5  necklace WH/2.5              sum = 1.000 WH
                                   (2 rings + 2 earrings + 1 necklace)
```

✅ **Verified against the code**: `ArmorSlot = { Head, Body, Gloves, Boots }` and
`JewelType = { Ring×2, Earring×2, Necklace }` in `Game.Shared/Items.cs` — his slot counts are the real ones
and both sets sum to 1.000 on the nose. Full character = **3 WH** (weapon + armor set + jewel set).

🔴 **The SHIELD is unpriced.** It is its own `EquipSlot.Shield`, not one of the armor four, so it is outside
both sums. Needs a fraction of its own.

**Consumables** (his rules, and the hardest part):

1. *"1h of farming should buy u 1h of buffs"* — potion/scroll uptime is paid for at parity with farm time.
2. *"if for 1h of farming i can get 1 drop of enchant, it should cost me the same to make another one"* —
   craft cost = the drop's farm-time equivalent, so crafting **doubles** the consumable faucet.
3. *"if they skipp the craft to be able to craft runes at the same speed like the others"* — a crafter who
   forgoes gear crafting should reach runes at the same pace as one who forgoes consumables.

## §7b · What his curve solves to (`M8`)

`per-attempt budget = target/success × (1 − fail)`, then the 100 bulk : 1 accent shape priced off §2/§3:

```
 rung  per finished   per attempt |  1 bulk mat   1 accent |   solved bulk  accent    vs his own range
    E          2-3h      1.8-2.7h |        0.4m       1.9m |       245-368       4    BELOW his own range
    D          3-5h      2.6-4.3h |          2m      13.7m |        73-121       1    straddles his range
    C         5-10h          4-8h |        8.7m       1.0h |         26-51       1  🔴 under 100 — breaks
    B        12-24h     8.4-16.8h |        1.1h       7.7h |          7-14       0  🔴 under 100 — breaks
    A        12-36h         6-18h |        6.5h      45.7h |           1-3       0  🔴 under 100 — breaks
    S       84-168h        21-42h |        6.2h      43.6h |           3-6       0  🔴 under 100 — breaks
```

🔑 **Read the `1 bulk mat` column — it is the whole finding.** One Legendary mat is **6.2-6.5 farm hours by
itself** and one Mythic is **44-46 h**. Once a single unit of the bulk costs hours, no target curve can buy
a *pile* of them: a 36 h budget buys a handful. The top rungs are not mis-priced, they are **quantised too
coarsely to price**.

🔑 **His curve is ~2.5× CHEAPER than the §5 proposal at E/D/C and about the same at A/S.** That is the
opposite of what would help: cutting the target shrinks the pile, so the break in the 100:1 shape moved
**DOWN a rung, from B to C**. Only E and D still support a bulk pile at all, and both now solve *below* the
ranges he originally authored.

## §7c · His slot fractions, priced (`M9`)

Every cell is the midpoint of his range for that rung. A full character = weapon + armor set + jewel set =
**3 weapons**, which is the number to read the S row by.

```
 rung       weapon        body      helmet     gloves      boots       neck     ear x2    ring x2 |  FULL CHAR
    E         2.5h        1.3h         45m        15m        15m       1.0h        30m        15m |       7.5h
    D         4.0h        2.0h        1.2h        24m        24m       1.6h        48m        24m |      12.0h
    C         7.5h        3.8h        2.3h        45m        45m       3.0h       1.5h        45m |      22.5h
    B        18.0h        9.0h        5.4h       1.8h       1.8h       7.2h       3.6h       1.8h |      54.0h
    A        24.0h       12.0h        7.2h       2.4h       2.4h       9.6h       4.8h       2.4h |      72.0h
    S         126h       63.0h       37.8h      12.6h      12.6h      50.4h      25.2h      12.6h |       378h
```

✅ Both sets sum to **1.000 weapons** — his fractions are exact, nothing to adjust.
⚠ **A fully S-geared character is 378 farm hours = 31 of his 12-hour days.** That is the number to sanity-check,
not the per-item ones; it is the real endgame cost and it is the first time it has been stated.

## §7d · The recommendation (changed from §6)

**The measurement put §6's recommendation back.** I had briefly changed it to "abandon the 100:1 shape and
let the top be few-and-precious", on the assumption that an elite camp is a trickle. **`M11` refutes that
assumption** (§7e), so the recommendation is again **§6's: open a faucet at the top, on ELITES.**

**Primary — an elite mat faucet at 76+, carrying Epic / Legendary / Mythic.** It fixes B, A *and* S in one
change, it preserves his 100:1 shape everywhere, and it is exactly the shape D1 already used when the
normal-mob enchant faucet closed at B (`MobCatalog.EnchantScrollDrops`). It also finally gives an elite
camp a reason to exist for a level-80 farmer, which is the same argument that justified it for scrolls.

**Fallback, if he does not want a new drop table — few-and-precious.** An S weapon reads `6 Legendary Ore +
1 Mythic Ore`. Honest (it really is ~150 h), readable in a UI where `1 847 Iron Ore` is not, and needs
nothing re-measured. The *"feels like nothing"* objection is then a **presentation** problem — print the
farm-hour estimate on the recipe — not a balance one.

🔑 **Under the fallback ONLY, the top FAIL RATES must come down.** With a pile of 1 000 mats a fail is a
setback; with a pile of 6, S's 75 % eats **37 hours in one click** and the system reads as a slot machine.
Cut the top fail rates, or have a fail **return a fraction of the mats**. Same one-knob point as §5 ⚠. Under
the *primary* proposal the piles stay large and the fail table can stay as authored.

## §7e · What the measurement CHANGED (`M9`-`M11`) — read this before acting

Three of my five open questions came back contradicting the estimate I gave with them.

**1. 🔑 ELITES CAN CARRY A FAUCET — I was wrong that they are a trickle.**

```
   rank   camps   held    respawn   kills/h per camp   vs a normal farm
  Elite       6    3.8       125s              110.4            147.2%
   Boss       2    1.0    38 700s               0.09              0.1%
```

An elite camp is **respawn**-limited where a normal farm is **walk**-limited, and that turns out to be an
advantage: several held on a ~2 min timer beats the 70-81 kills/h of ordinary farming outright. ⚠ Both
figures are **ceilings** — no travel, no TTK, and an elite's own TTK is longer — so treat 110 as an upper
bound, not a farm rate. Even halved it clears the bar. 🔴 **Bosses cannot**: one spawn on a ~10.75 h timer
is 0.09 kills/h. A boss can gate a **one-off** (a single Mythic accent per item) and never a quantity.

**2. 🔴 The per-TYPE penalty is 2-3.6×, and it does NOT vanish at the top rarities as I predicted.**

`StandardDrops` splits the guaranteed Common group three ways (mats.A, mats.B, Gem) but authors the higher
rarities as independent rolls on **A and B only** — so which types exist at all depends on the mob's
**category**, and the answer differs per band:

```
 band            Common  Uncommon      Rare      Epic
 E (20-39)         2.3x      2.4x         -         -
 C (52-60)         2.2x      3.6x      2.0x         -
 A (76-79)         2.5x      1.6x      1.0x      1.0x     <- single-type band: Ingot only
 S (80-85)         2.0x      3.5x      2.5x      2.5x
```

🔑 **If a recipe names a type (`300 Ingot`) rather than a material, multiply that rung's §7b hours by this**
— measured against the *best* type in the band, so it is a floor on the penalty, not a worst case. The A
band is the odd one out at 1.0×: only Ingot drops Rare/Epic there at all, which also means an A-band farmer
crafting anything *but* Ingot recipes has no local source.

**3. 🔴 Both consumable premises are wrong, in opposite directions.**

```
 gr   levels  kills/h |  scrolls/h  enchants/h  1 enchant every  potions/h  buff-min/h
  F     1-19       81 |      0.081       0.081            12.3h     15.313       192.7
  E    20-39       81 |      0.162       0.162             6.2h     17.445       231.2
  D    40-51       78 |      3.094       0.274             3.6h     17.086       223.8
  C    52-60       76 |      4.307       0.191             5.2h     16.876       217.8
  B    61-75       70 |      4.339       0.123             8.1h     15.077       200.9
  A    76-79       72 |       6.06       0.054            18.6h     15.498       204.5
  S    80-85       75 |      7.198           0            never     16.252       214.4
```

- 🔑 **"1 h of farming should buy 1 h of buffs"**: parity would be 60 buff-min/h. The game already runs
  **193-231 — 3-4× OVER his target.** Potion uptime is not scarce, and pricing a crafted potion at his rule
  would make it *cheaper* than it already is. **This rule cuts the potion faucet; it does not raise it.**
- 🔑 **"1 h of farming = 1 enchant drop"**: it is **3.6-18.6 h**, and it gets *worse* as you climb — the
  opposite shape to the one the premise assumes. My earlier "~8×" estimate was the right order but the wrong
  shape. A crafted-scroll price built on "one an hour" would be ~10× too cheap.
- 🔴 **The S band reads ZERO enchants/h, by design not by bug**: the normal-mob enchant faucet closes at 80
  (D1), leaving elites and bosses as the only source. So at the exact level the crafting ladder needs its
  top rung, **the drop it would be priced against does not exist** — which makes crafting the *intended* A/S
  scroll supply rather than a convenience. That is an argument for pricing it generously.
- ⚠ **Craft-at-parity still means nobody crafts** (unchanged, and unmeasurable): break-even, and strictly
  worse once the fail chance counts. The real value of crafting a consumable is **determinism** — you pick
  *which* scroll. Price at parity and let determinism be the reason.

## §7f · Still open

1. **Primary or fallback?** (§7d) — elite mat faucet at 76+, or few-and-precious with reduced fail rates.
2. **Do recipes name a material TYPE or just a material?** — a 2-3.6× on the whole ladder (§7e 2).
3. **The shield's fraction** (§7a 🔴).
4. **Re-confirm B** — `12-1d` collapses to a point under his own definition of a day (§7a ⚠).
5. **Is 378 h — 31 of his 12-hour days — the right cost for a fully S-geared character?** (§7c ⚠). It falls
   out of his own numbers, but it had never been stated as one figure before.

**The one measurement still missing:** an elite's own **TTK and travel time**, which would turn `M11`'s 110
kills/h ceiling into a real rate. Everything else §7 needs is now printed.

---

*§1-§6 from `tools/BalanceMatrix` §M (`M1`-`M7`); §7 is his ruling, measured by `M8`-`M11`.
Related: `docs/design/CraftingProfessions.md` §5c/§5d, `docs/design/Crafting.md`, the `BL-05` entry in
`docs/Backlog.md`.*
