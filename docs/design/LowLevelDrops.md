# Drops below level 40 — proposal (`BL-307`)

> *"Lower lvl mobs also need full drops. F/E grade also need Common equipments and drops for mythic/common. Now <40
> players rely solely on gold mob drop .. and no lucky drops or any mat to exchange for money (with other players when
> economy is present)"* (playtest of 0.206.0)

**Status: BUILT in 0.214.38**, exactly as proposed (owner, 2026-09-28: *"build the BL-307 as u proposed - looks good on
paper"*), which also answers §4: the rates stand, mats from 20, the starter creatures are dealt a specialty like the rest,
and nothing more on scrolls. The rates live in `MobCatalog` (`CommonScaleF/E`, `RareGearChance`, `BaseMatMinLevel`,
`BaseMatLowPerKill`); `--low-drops` now reads them from there. It was measured with
`dotnet run --project tools/BalanceMatrix -- --low-drops [F-scale E-scale F-lucky E-lucky mats]`. That mode reads the
real catalogs and the M1 kill clock, and it changes nothing in the game. To try other numbers, run it with your own
five values (for example `--low-drops 1 0.35 1000 3000 0.1`).

## 1. What a player below 40 earns today

Solo, same-level creatures, x1 rates, the M1 clock (~85 kills an hour, the one Favor and `--craft-cost` use):

| levels | creatures | hours spent there | kills | coin / h | drops / h (sold) | **total / h** |
|---|---|---|---|---|---|---|
| 1-9 | 3 | 0.7 | 62 | 5,800 | 640 | **6,400** |
| 10-19 | 5 | 6.8 | 570 | 11,900 | 80 | **11,900** |
| 20-29 | 5 | 26.9 | 2,330 | 19,200 | 80 | **19,200** |
| 30-39 | 8 | 50.3 | 4,150 | 24,900 | 90 | **24,900** |
| *40-42, for scale* | 3 | | | 29,300 | 453,000 | **482,000** |

- **You read it right: below 40 it is coin and nothing else.** The drops column is healing potions and the odd
  enchant scroll. It is ~85 hours of play.
- **At 40 the income jumps ~20×**, and almost all of that is drops (Commons, recipes, parts). Coin barely moves.
- An E-grade Mythic piece costs **856k** on average at the shop. That is **35-45 hours of coin** at 20-39, so today the
  shop is out of reach and nothing drops.
- Base mats (Iron, Thread, Wood, Leather, Gem) already drop from **35**, at 0.2 a kill. They sell to the shop for
  **2 gold**, so their value is player trade: they are what the T40 crafters need.

## 2. The proposal: the 40+ table shape, at F and E

The same machinery as 40+ (`BL-274`/`BL-287`), extended down with two new bands. Nothing new is invented, only reused:

1. **Common F and E gear.** A Common copy of every `_t1` (F) and `_t20` (E) base piece: same stats as the Mythic, no set,
   no attribute, no enchant, exactly like T40-T61 Commons today. It sells for the Common's usual 5% of its Mythic
   (~3,000 F, ~21,000 E).
2. **Each creature gets a specialty** (weapons / body / small armour / jewellery), dealt across two new bands, 1-19
   and 20-39, by the same rule as 40+. Both bands are big enough: 8 creatures in 1-19 and 13 in 20-39 (the deal needs
   a weapon carrier for each of the 8 lines, plus one each for body, small armour and jewellery).
3. **The lucky drop:** the full **Mythic** piece of the creature's kinds, one roll a kill, like 40+'s 1/10,000.
4. **Base mats from 20** instead of 35, at 0.1 a kill until 34 (35+ keeps its 0.2). A trade good for the T40 crafters.
5. **Elites x2** on everything, as at 40+.
6. **No recipes and no parts below 40.** Crafting starts at T40, so there is nothing to make with them.

**My rates:**

| | F (1-19) | E (20-39) | 40+ today, for comparison |
|---|---|---|---|
| Common per slot | **the T40 table x1** (ring 2% … weapon 1%) | **the T40 table x0.35** (ring 0.7% … weapon 0.35%) | T40 ring 2% … weapon 1% |
| Commons per kill (average creature) | 0.029 (1 in 34) | 0.010 (1 in 98) | 0.029 |
| lucky Mythic per kill | **1 in 1,000** | **1 in 3,000** | 1 in 10,000 |
| base mats per kill | none | **0.1** (20-34) | 0.2 → 1.5 |

**What that gives a player levelling through:**

| levels | Commons found | lucky Mythics found | mats found | **total / h** | vs today |
|---|---|---|---|---|---|
| 1-9 | 1.8 | 0.06 | 0 | **19,400** | ×3.0 |
| 10-19 | 16.6 | 0.6 | 0 | **24,100** | ×2.0 |
| 20-29 | 23.8 | 0.8 | 262 | **50,600** | ×2.6 |
| 30-39 | 42.5 | 1.4 | 234 | **54,800** | ×2.2 |

The mats add almost nothing to the hourly figure (2 gold each at the shop). Their worth is what a crafter pays you.

### Why these numbers

- **E is x0.35, not x1.** At the full T40 table a player finds **~190 E Commons** over the 77 hours of 20-39. That is
  every slot ten times over and a bag problem. At x0.35 it is ~66: most of the 18 kinds turn up once or twice, and a
  player's own kinds (their weapon, their armour weight) turn up several times.
- **F stays at x1** because the band is short (7.5 hours) and the newbie kit is a 30-day loaner. F Commons are what
  replaces it, and they cover the pieces the kit does not have: heavy armour, the blunts and the shield.
- **The lucky piece keeps the same weight in an hour as at 40.** 1/3,000 at E pays ~12k/h in expected value, the same as
  40+'s 1/10,000 on a T40 Mythic. That makes 1-2 finds over 20-39, each worth ~20 hours of coin. That is a lucky drop.
  F is richer (1/1,000) because the band is short. Otherwise nobody would ever see one.
- **The cliff at 40 stays, but smaller.** From ~20× down to ~9×. I did not try to close it: 40 is where crafting,
  recipes and parts start, and that should still feel like a step up.

## 3. What building it touches (for when you rule)

- `ItemCatalog.CommonMinLevel` 40 → 1, so the Common copies exist at F and E. ⚠ This adds 36 items to the catalog and
  so **needs a new APK**. `ItemIds.md` is regenerated.
- `MobCatalog.ProfileBands` gains `(1, 19)` and `(20, 39)`. `CraftTier` stays 0 below 40, so the gear half needs its
  own "gear tier" (1 / 20) beside the craft tier, so that recipes, parts and Nightsilver stay off.
- `CommonGearSlotChance` gains the rows `1 => T40 row × 1` and `20 => T40 row × 0.35`. `RareGearPerKill` becomes a
  per-tier value. `BaseMatMinLevel` 35 → 20, with 0.1 up to 34.
- ⚠ **The three starter creatures** (Ridgeback Pup, Fox, Goblin Scout) are dealt into the 1-19 band like everything
  else. If you want the first ten minutes to stay coin-only, they can be left out.
- `mob_drops.csv` regenerated, Formulas.md's drop section, SmokeTest: an F/E Common drops and cannot be enchanted.

## 4. Your questions

1. **The rates:** F Commons x1, E x0.35, lucky 1/1,000 and 1/3,000, mats 0.1 from 20. Yes, or which ones move?
2. **Mats from 20, or keep 35?** They sell for 2 gold, so below 35 they are only worth anything once players trade.
3. **The starter creatures:** dealt a specialty like the rest, or kept to coin (and their return scroll)?
4. **E-grade buff potions / enchant scrolls:** nothing changes there. E scrolls already drop from 20 (1 in ~500).
   Say if you meant more of those by "full drops".
