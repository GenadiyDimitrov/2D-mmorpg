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

## 2. Drop rates ✅ BUILT (0.34.1)

| | Common | Uncommon | Rare | Epic | Recipe |
|---|---|---|---|---|---|
| Normal | 5 % | 2 % | 0.2 % | 0.01 % | 0.1 % (below level 74) |
| Elite / dungeon / instance | — | 10 % | 2 % | 0.2 % | 0.1 % |
| Boss | — | — | — | E 70 %, L 40 %, M 2 % | armor 50 %, weapon 40 %, jewel 60 % |

Roughly a **4× cut** on Common (20 % → 5 %).

⚠ **These are per GROUP, not in total.** With four gear groups a normal kill yields *some* Common piece
~20 % of the time — but spread over **18 item lines** instead of the 3 that used to drop, so any ONE
piece is far rarer than before. That is the shape the owner asked for ("trading with other newbies
should be the better route").

### ⚙ Two rate knobs, not one (owner, 2026-07-30 — corrects a first reading)

**The authored table is the ×1 design.** 5 % authored means 5 % at ×1 and **15 % at ×3**.
`RateConfig.DropChanceRate` stays the server's rate knob and is expected to move, *"x10 or x200"*.

But a global rate cannot be the only knob, because the guaranteed groups are authored as **absolutes**
(mats 100 %, always 100 %, scrolls 70 %) and must stay put at any rate — multiplying a 100 % group by
×200 makes it no more generous, it just pins it at the clamp and discards every weight inside it.
So, in the owner's own words:

> *"we can introduce each group multiplier (global), so we can have drop chance ×200 and armor group
> multiplier ×0.01 — in reality armor will be ×2 drops."*

**`RateConfig.DropGroupRates`** is that: a per-group multiplier composed on top of the global rate.
`MobCatalog.EffectiveRate(groupId)` is the ONE place the two combine —

```
rate = (guaranteed group ? 1 : DropChanceRate) × DropGroupRates[group]
```

— and the kill roll, the target-inspect list and `BalanceMatrix` all call it, so the number on screen
cannot drift from the number you get. Groups: `armor` · `accessory` · `weapon` · `jewel` · `mats` ·
`scrolls` · `always` · `other` (the independent entries).

**Shipped defaults: global ×3 (unchanged), gear groups ×1/3, everything else ×1.** The 1/3 is this
system doing its job rather than a fudge: the design reads at ×1, the server runs at ×3, and the owner's
acceptance test is absolute (~400k by level 25) — ×3 flat measures 1.08M, ×3 × 1/3 measures 402k.
**If `DropChanceRate` ever goes back to 1, put the gear groups back to 1 with it.**

Live-tunable in game, admin only — **`/droprate`** lists everything, `/droprate <group> <x>` sets one,
`/droprate gear <x>` sets all four equipment groups, `/droprate global <x>` sets the server rate. It is a
CHAT command and not a tuning-panel row on purpose: the panel's payload is a wire DTO, so eight new
fields there would bump the protocol and need a matching Unity build — for a knob whose entire value is
being adjustable mid-playtest, on the phone, without rebuilding anything.

**Epic drops at 0.01 % from normal mobs** (the owner's own column), but only from **E grade up**: §1 puts
F at Common/Uncommon/Rare only. Legendary and Mythic stay boss-only.

**Recipes are NOT dropped below A grade.** There is no item to drop — every recipe under level 76 is
learned by LEVEL (`RecipeCatalog.DropOnly` is only set at 76+), so the owner's "below level 74 also drop
a recipe at 0.1 %" needs recipe books authored for the lower grades first. Flagged, not faked.

## 3. Grade lock + drop groups ✅ BUILT (0.34.1)

A mob drops **only its own grade** — a level-40 mob drops D recipe/armor/weapon, never E or C. Group
trigger chances and the inner rarity rolls are in the playtest doc's table.

⚠ The group ENGINE already existed: `DropEntry.GroupId > 0` rolls once at the summed member chances then
picks one weighted (`MobCatalog.cs`, resolved in `GameLoopService.cs` ~6650). That is mathematically
identical to the owner's "trigger 50 % then double the inner roll", so a member's authored chance IS its
marginal drop chance and the §2 table could be written straight in — no new mechanism was needed.

**How it was built (`MobCatalog.GearDrops`):**
- **Four gear groups** — Armor (heavy/light/robe) · Accessories (helm/gloves/boots/shield) ·
  Weapons (all 8 lines) · Jewels (necklace/ring/earring). A hit randomises across the whole family, so
  **where you farm no longer decides which armor weight or weapon line you can ever loot.** The old
  category flavour (Undead → robe+wand) is gone for GEAR; mats keep theirs.
- **The grade lock is `GearTier(level)`** — a mob offers exactly ONE tier (1/20/40/52/61/76), so there
  is nothing to lock out. S (80) is deliberately absent: S is top-half-only and stays craft/boss.
- A group id is `10 + family*10 + (int)rarity` — **one group per rarity RUNG.** That is what lets the
  boss row (E 70 + L 40 + M 2 = 112 %) pay out several pieces while each rung still randomises across the
  family. Cost for a normal mob: a 0.1 % chance of both a Common and an Uncommon armor off one kill.
- **Elite and boss REPLACE the gear half at kill time**, in `RollDrop` — rank is a property of the SPAWN
  (the zone assigns it), not of the template, so it cannot live in the baked table. `RollBossBonus` keeps
  the mat pile and now rolls the owner's recipe numbers (boss armor 50 / weapon 40 / jewel 60, elite 0.1),
  but no longer decides gear itself.
- **Broken jewels are out of the drop tables** — §1 makes the F Common jewels that line, and the Jewel
  group drops them from level 1. The items stay in the catalog and on the starter vendor's shelf.
- **Mats: one stack per kill and the roll IS the amount** (50 % → 1, 40 % → 2, 9 % → 4, 1 % → 10),
  authored as one group member per (type, amount) so the existing weighted pick resolves both at once.
- **Scrolls (70 %)**: half an enchant scroll of the grade, half a buff potion; rungs unlock at level
  20 / 45. **Always (100 %)**: a healing potion / return scroll / resurrection scroll, C 70 · U 30, and
  C 55 · U 40 · R 5 from level 75 where the Ultimate scrolls join. Measured, these are ~7 % of trash
  income — the buff potions do not re-open the faucet through a second pipe.
- The **target-inspect drop list** now collapses each group to one line. Not cosmetic: a mob carries ~97
  entries now, and 97 near-identical 0.6 % rows told the player nothing. One line per group is also more
  truthful — the 5 % really is one roll shared across the family.

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

### 🎯 The owner's target: ~400k of trash gold by level 25 — ✅ **HIT, 403k** (measured 0.34.1)

The ~184k / "overshoots by 2.2×" figure that used to stand here was the PRICE side alone; the drop side
moved it back up, exactly as this section predicted it would. Measured on the built catalog by
`tools/BalanceMatrix` (§ ECONOMY), which resolves the real drop tables with the real group math and the
real vendor prices:

| level reached | kills (live ×10 exp) | trash gold sold |
|---|---|---|
| 11 | 8 | 2.5k |
| 21 | 75 | 81k |
| **26** | **168** | **403k** ← target was ~400k |
| 41 | 753 | 2.9M |
| 62 | 2,132 | 51M |
| 86 | 63,175 | 10.5B |

Per kill at level 25: **3,477 gold** — gear 2,989 · consumables 261 · coin 225 · mats 2.

The model assumes he vendors everything and kills his own level, which is what "3kk purely from selling
trash" described. Validation anchor: the tool prices the E Common gauntlet at **sell 4,500**, identical
to the figure measured for the price half in 0.33.3 — so the price path is the same one, and only the
drop side is new arithmetic.

⚠ **The top of the curve is steep on purpose** — 51M by 61, 10.5B by 85. That is the owner's own
*"gold farming stays meaningful only at the top grades"*, and it follows from A-grade Common body armor
selling for 648k. Worth confirming he means it at that magnitude; it is one number (the grade's 2H cell)
to retune if not.

If it ever needs softening again the knob is the **Common price multiplier** (22.5 %) — raising it lifts
sell price, though it also makes Common gear dearer to buy, which is the flagged complaint below. The
**÷25 divisor is pinned** by the "25 Robes buys one Leathers" test and is not the knob.

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

## Still open

- **Recipe drops below A grade** (§2's "below level 74 also drop a recipe at 0.1 %"). No item exists to
  drop — recipes under 76 are learned by level, not found. Needs recipe books authored for F–B first.
  **Owner's call (2026-07-30): add it later, the same way A+ was added.** Not a blocker.
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
- `MobCatalog.GearDrops(level, rank)` — the §2/§3 rate table × the four slot families. `StandardDrops`
  bakes the Normal row into each template; `GameLoopService.RollDrop` swaps in the Elite/Boss row.
- `MobCatalog.NormalGearRates` / `EliteGearRates` / `BossGearRates` — the three §2 columns, one place.
  ⚠ They are **properties, not fields**: `All = Build()` is declared first and would read a null field.
- `RateConfig.DropChanceRate` (×3) + `RateConfig.DropGroupRates` (per group) — combined ONLY by
  `MobCatalog.EffectiveRate(groupId)`. See §2. Admin command: `/droprate`.
- `tools/BalanceMatrix` § ECONOMY — the measurement. **Re-run it after touching any of the above**; the
  faucet arithmetic multiplies and has been hand-derived wrong twice.
