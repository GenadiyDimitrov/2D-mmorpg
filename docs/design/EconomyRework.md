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

### ✅ ANSWERED + BUILT (0.34.0) — "does F-Common ≈ the training gear?"

Measured off the built catalog, not derived. **No — and it missed in opposite directions:**

| | F-Common | training (old) | ratio |
|---|---|---|---|
| 1H sword P.Atk | 10 | 6 | 1.67× |
| daggers | 9 | 5 | 1.80× |
| bow | 22 | 11 | 2.00× |
| **light body P.Def** | **38** | **53** | **0.72×** |
| robe body P.Def | 22 | 27 | 0.81× |
| robe body +MP | 49 | 29 | 1.69× |

The WEAPONS were fine; the **armor was above the first thing you could loot**, so every early armor
drop was a downgrade. The owner's diagnosis of why (2026-07-30): the training armor was authored as the
**sum of an L2 upper + lower body**, taken from the **top** of the no-grade range, whereas this
ladder's F Common rung is 45 % of a **mid** no-grade set. The weapons were cut from L2's **top**
no-grade weapon, which is why they line up and the armor does not.

**Fix (his call, and the right one): move the STARTER down, not the F rung up** — light 53 → **35**,
robe 27 → **20**, +MP unchanged. Lifting F-Common instead would have broken the ladder's single rule
(every quality is a fixed fraction of the authored Mythic piece). Defence is a small share of survival
this early — he has levelled a melee fighter wearing none.

Verified: **every** F-Common piece now beats its starter counterpart (armor, all five weapon lines,
and the robe's MP). The starter kit stays as it is; you begin in something worse and gear UP.

### ✅ The F tier was ALREADY authored — and one thing here was wrong

`Items.cs` has carried F at Common/Uncommon/Rare since 0.31.2/0.31.3 (the `Ferrite` line, generated
from the one ladder). So §1's "author an F tier" was mostly already done.

⚠ **Correction to an earlier reading of this file:** the old generated `"Worn"` / `"Steel"` item line
(`sword_f_common`, `light_body_f_common` — 4-6 P.Def) is **DEAD CODE**. It lives under
`LootTables` in `Items.cs`, which is referenced from nowhere in the solution. Mobs never dropped it.
The live drop path is `MobCatalog.StandardDrops`, which has always used the tiered `_t<level>_<rarity>`
copies. Do not "fix" the drop tables to point away from it — there is nothing to repoint.

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

## 4. Sell prices — **sell = buy ÷ 25** ✅ BUILT

Owner confirmed (2026-07-30): sell **derives from buy**, it is not a cut off the old sell price.
*"that's why always I give the buy prices — this is the Server's price, the economy may differ in
10ts of times."* The divisor is not arbitrary: his own acceptance test is "≈25 Robes buys one
Leathers", and Robe/Leathers are the same slot at the same grade+rarity, so they share a buy price —
the divisor **is** that ratio. Measured: exactly 25.0.

Confined to TIERED GEAR (`GameConstants.GearSellDivisor`). Mats, potions and scrolls keep the generic
`VendorSellFraction` (30 %) — they are not what made a level-25 character rich, and cutting them would
nerf crafting income nobody asked to nerf.

The intent, in the owner's own terms:
- Selling ~25 Robe armors should buy one Light armor.
- Trading with other newbies should be the better route.
- Gear farming becomes about **gearing up, not gold farming**.
- Gold farming stays meaningful only at the **top grades**, where the items are genuinely expensive.

## 5. Buy prices ✅ BUILT

**⚠ The F/E/D numbers are the RARE price, not the Mythic one.** Owner, correcting a first reading of
his answer: *"The f, E, D prices that are in the shop are for rare… The shop sells rare only F-D."*
So the shop keeps showing exactly the numbers it shows today, and the **Mythic rung is derived above
them** at ÷0.35 (the Rare multiplier). The 2H C→S column, by contrast, he gives as **Mythic**.

|  | F | E | D | C | B | A | S |
|---|---|---|---|---|---|---|---|
| | *(RARE — the shop price)* | | | *(MYTHIC)* | | | |
| gloves / boots | **6k** | **175k** | **600k** | 5.4kk | 12kk | 24kk | 120kk |
| helmet / shield | **10k** | **250k** | **1kk** | 9kk | 20kk | 40kk | 200kk |
| body armor | **18k** | **400k** | **1.8kk** | 16.2kk | 36kk | 72kk | 360kk |
| 1H weapon | **27k** | **670k** | **2.7kk** | 24.3kk | 54kk | 108kk | 540kk |
| **2H weapon** | **30k** | **750k** | **3kk** | **27kk** | **60kk** | **120kk** | **600kk** |
| ring | **3k** | **70k** | **250k** | 2.25kk | 5kk | 10kk | 50kk |
| earring | **6k** | **140k** | **500k** | 4.5kk | 10kk | 20kk | 100kk |
| necklace | **12k** | **280k** | **1.5kk** | 13.5kk | 30kk | 60kk | 300kk |

**Bold = the owner's authored numbers.** The resulting **2H Mythic ladder** is
F 85.7k · E 2.14kk · D **8.57kk** · C **27kk** · B **60kk** · A **120kk** · S **600kk**
(D ≈ 9kk and C 15kk→27kk are his own corrections; B was raised 30kk → 60kk, *"30 was to cheap"*).

Every non-bold C..S cell is derived by holding the 2H column's slot fractions. That is not a guess —
they are the fractions the authored F/E/D numbers already satisfy, all verified against the live catalog:

- **2H weapon = 75 % of a full 4-piece set** (body+helm+gloves+boots): measured 75.0 % at all seven grades.
- the set splits **45 / 25 / 15 / 15** across those pieces.
- **1H = 90 % of 2H** ("1h weapons are cheaper because they give less attack and you need to buy a
  shield" — about a third of the shield's price is the saving).
- jewels: ring **1/12**, earring **1/6**, necklace **1/2** of the 2H price.

So retuning a grade is **one number** — its 2H cell — not eight. And because the F/E/D cells are
written in code as `Shop(x)` with x the authored shop price, the Rare rung round-trips exactly:
**all 35 shop prices verified identical to the authored numbers.**

Other authored rules that check out exactly:
- **1H = 2H − shield ÷ 3** (30k−3.3k≈27k, 750k−83k≈670k, 3kk−333k≈2.7kk). "1h weapons are cheaper
  because they give less attack and you need to buy a shield."
- **Main armors carry most of the defence, gloves/boots least** — the price split follows the stat split.
- **S is deliberately extreme** because S Mythic is craft-only: *"if you want, go ahead and sell it —
  players will probably trade it for billions."*

### Measured effect (real catalog, before vs after)

| | buy | sell |
|---|---|---|
| F/E/D **Common** | **1.84× DEARER** | **4.1× less** |
| F/E/D Rare | unchanged (it is the shop price) | **7.5× less** |
| F/E/D Mythic | 1.4× cheaper | **10.5× less** |
| C | 2–3× dearer | 1.3–3.3× less |
| **B / A / S** | much dearer | **MORE than today** (B Mythic body 2.16kk → 1.44kk is less, but B Common 189k → 324k is more) |

The E-grade Common gauntlet — the level-25 playtest's trash — sold for **18.4k** and now sells for
**4.5k**. With the 4× drop cut, trash gold at that level falls **~16×**.

**That 16× is the TOTAL — both levers, not the sell side alone**: 4.1× (sell) × 4× (drop rate). It is
easy to double-count the drop cut on top of it and get 68×.

### 🎯 The owner's target: ~400k of trash gold by level 25

Given the 3kk the playtest reported, that is a **7.5×** total cut. The plan as built gives **16.3×**
(→ ~184k), so it currently **overshoots by ~2.2×**.

**Do not tune for it yet.** Grade lock and the mutually-exclusive groups (§3) will move the number
again, so build the drop side first and re-measure the real figure. If it still needs softening, the
knob is the **Common price multiplier** (22.5 %) — raising it lifts sell price, though it also makes
Common gear dearer to buy, which is already the flagged complaint below. The **÷25 divisor is pinned**
by the "25 Robes buys one Leathers" test and is not the knob.

⚠ **Common gear now costs 1.84× MORE at the vendor** (E Common body 140k → 257k). That follows from
the rarity scale — Common is 22.5/35 = 64 % of the Rare shop price where it used to be 35 % — but it
runs against the earlier note that low qualities "are priced as the convenience they are". Flag for
the owner.

⚠ **B and above sell for MORE than before at the low rarities.** Not a bug: the old code capped the
table at the D column for every level ≥ 40, so a B-grade item was priced as if it were D. The new
ladder is what makes the owner's *"gold farming stays meaningful only at the top grades"* true.

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

## ✅ All three answered (2026-07-30) — §4 and §5 are BUILT

1. **Mythic is the 100 % base, Legendary 42.5 %** — yes, confirmed; the 2.35× jump at the top is intended.
2. **E and F were never missing.** They were given back in playtest-13 and have been live in
   `Items.cs` since — the doc was wrong to list this as open. What was genuinely new is B 30kk → 60kk,
   and later C 15kk → 27kk with D's Mythic rung landing at ~9kk.
3. **Sell derives from BUY**, at ÷25 — see §4.

**And the correction that followed:** the F/E/D table is the **RARE** price (the shop sells Rare only
at F-D), so Mythic is derived *above* it, not equal to it. This is the single most load-bearing fact
in §5 — reading it the other way moves every price by 2.86× and halves the faucet fix.

## Still open (batch 2 proper — the drop side)

- **The rates in §2** (5 % / 2 % / 0.2 % / 0.01 %, elite and boss columns) — deliberately NOT applied in
  0.34.0. Today's chances (4 % / 1.5 % / 0.4 % body, 2.5 % / 1 % weapon, 3 % helm) are untouched, so the
  rate decision is still open and un-pre-empted. §2 and §3 are the SAME edit.
- **Grade LOCK (§3)** — a mob still drops only its own tier via `GearTier(level)`, but that is a *floor*
  mapping, not the authored lock, and the slot family is still `cat`-derived rather than randomised
  across the family.
- **Delete the training gear entirely?** Not done, and now arguably unnecessary: with the armor re-cut,
  the training kit reads as a deliberate worst rung rather than a parallel line. The owner's §1 wording
  ("delete the training gear, replace with an untradable Common") would give the same *shape*; the
  re-cut achieves it without a second set of ids. **Confirm which he wants before deleting anything.**
- **F Epic/Legendary copies still exist** (`ScaledDropItems` only excludes the top-half rule at S).
  §1 says F should be Common/Uncommon/Rare only. Harmless today — drops are C/U/R and F recipes do not
  exist — so it was left alone rather than risking the crafting path an hour before a build.

## ✅ Built in 0.34.0

- **F-grade gear now drops.** `GearTier()` returns the F tier (level 1) below level 20 instead of
  flooring to 20, and the level-18 gear gate is GONE — it only existed *because* of that floor (a
  level-8 mob dropping E-grade gear). Rarity is gated by mob level instead, per §1: **Common from 1,
  Uncommon from 5, Rare from 10.** Verified: 1087 drop entries across the roster, 0 unresolved ids.
- **Training armor re-cut** (see above), so nothing lootable at 1-19 is a downgrade.
- **Per-mob spawners** (playtest-14 batch 3) — see `DedicatedSpawn` in `WorldMap.cs`.

## Where it lives in code

- `ItemCatalog.TieredGearPrice` — the 7-grade × 8-slot table (Mythic-anchored).
- `ItemCatalog.RarityPriceMul` — 22.5 / 27.5 / 35 / 35 / 42.5 / 100 %.
- `ItemCatalog.SellPrice` — tiered gear takes `buy ÷ GearSellDivisor`; everything else keeps
  `VendorSellFraction`.
