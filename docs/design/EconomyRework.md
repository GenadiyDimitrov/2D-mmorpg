# Economy rework — drops, grades, prices (playtest-14, owner-specified 2026-07-30)

The spec for **batch 2**. Source: playtest-14 (`docs/testing/Playtest-14.md`) plus the owner's answers
to the two questions it raised. Nothing here is built yet.

**The problem being solved:** level 25 with **3kk gold from selling trash alone** — Common gear dropping
at ~20 % and selling for ~20k. Two levers multiply, so both get cut.

---

## 1. The F tier (answers "levels 1-19 have no grade to lock to")

Owner: F-grade gear is worn for **less than an hour** at the start of a character. It does not need a
full six-rarity ladder.

- **Author an F tier at Common / Uncommon / Rare only.** No Epic / Legendary / Mythic at F — the
  level-15 quest already hands out a Mythic, so there is nothing for the top half to add.
- **Delete the training gear.** Replace it with an **untradable Common** armor + weapon as the starter
  kit.
- **The broken jewels become the F Common jewels** and drop normally.
- Rarity is introduced BY MOB LEVEL, not all at once:
  - level 1+ → **Common**
  - level 5+ → **Uncommon**
  - level 10+ → **Rare**

> ❓ **First job of batch 2, unanswered:** does an F-grade **Common** land close to today's training
> gear? Owner asked this directly. It decides whether the starter kit is a straight swap or needs its
> numbers moved. **Measure it with `tools/BalanceMatrix` — do not hand-derive** (this project has been
> burned by hand-derived balance before).

## 2. Drop rates (playtest-14 §3, unchanged)

| | Common | Uncommon | Rare | Epic | Recipe |
|---|---|---|---|---|---|
| Normal | 5 % | 2 % | 0.2 % | 0.01 % | 0.1 % (below level 74) |
| Elite / dungeon / instance | — | 10 % | 2 % | 0.2 % | 0.1 % |
| Boss | — | — | — | E 70 %, L 40 %, M 2 % | armor 50 %, weapon 40 %, jewel 60 % |

Roughly a **4× cut** on Common (20 % → 5 %).

**Real drops are limited to Common / Uncommon / Rare.** Epic and above are craft/boss only.

## 3. Grade lock + drop groups (playtest-14 §4, unchanged)

A mob drops **only its own grade** — a level-40 mob drops D recipe/armor/weapon, never E or C. Group
trigger chances and the inner rarity rolls are in the playtest doc's table.

⚠ The group ENGINE already exists: `DropEntry.GroupId > 0` rolls once at the summed member chances then
picks one weighted (`MobCatalog.cs`, resolved in `GameLoopService.cs` ~6640-6700). That is
mathematically identical to the owner's "trigger 50 % then double the inner roll". What is missing is
**more groups, the grade lock, and randomising across the slot family** — not a new mechanism.

## 4. Sell prices — cut **25×**

Owner revised up from "at least 3×". Combined with the 4× drop cut, gold from trash falls **~100×**.

The intent, in the owner's own terms:
- Selling ~25 Robe armors should buy one Light armor.
- Trading with other newbies should be the better route.
- Gear farming becomes about **gearing up, not gold farming**.
- Gold farming stays meaningful only at the **top grades**, where the items are genuinely expensive.

## 5. Buy prices

Owner's list — **weapon** buy prices by grade:

| Grade | D | C | B | A | S |
|---|---|---|---|---|---|
| Weapon | 3kk | 15kk | 30kk | 120kk | 600kk |

- **A 2h weapon = 75 % of a full armor set** → full set = weapon ÷ 0.75. At D: set ≈ 4kk.
- "All prices are scaled main armors" — the **main armor piece** at D ≈ **1kk**, consistent with a
  ~4-piece set.
- **S is deliberately extreme** because S Mythic is craft-only: *"if you want, go ahead and sell it —
  players will probably trade it for billions."*

### Rarity price scaling — power ratio, HALVED

The rarity ladder's power ratios are 45 / 55 / 70 / 70 / 85 / 100 %. The owner wants **gold to scale at
half that**, so rarity moves price less than it moves power:

> *"the price for Mythic D grade is 1kk, Common is 45 % so 450k now — I want it to 225k"*
> *"the scale of power cut by half for scale for gold — 70 % rare/epic power = 35 % gold"*

| Rarity | Power | Price |
|---|---|---|
| Common | 45 % | **22.5 %** |
| Uncommon | 55 % | **27.5 %** |
| Rare | 70 % | **35 %** |
| Epic | 70 % | **35 %** |
| Legendary | 85 % | **42.5 %** |
| Mythic | 100 % | **100 %** (the base the others are a fraction of) |

---

## ❓ Open — confirm before building §5

1. **Is Mythic really 100 % while Legendary is 42.5 %?** That is a 2.35× jump at the top. The owner's
   worked example (D Mythic = 1kk, Common = 225k) says Mythic IS the 100 % base, so the halving applies
   only to the rarities beneath it. Plausible on purpose — Mythic is craft-only and meant to be traded
   for absurd sums — but it should be said out loud before it is built in.
2. **E and F are not in the price list.** The given grades do not form a clean series (D→C ×5, C→B ×2,
   B→A ×4, A→S ×5), so downward extrapolation is a guess. Need E and F named, or explicit permission
   to pick them.
3. **Is the 25× cut off the CURRENT sell price**, or should sell become a fixed fraction of the new buy
   price? The "25 robes buys one light armor" line is a ratio between sell and buy in the same tier, so
   deriving sell FROM buy may be the more stable rule.
