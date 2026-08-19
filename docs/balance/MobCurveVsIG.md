# The mob curve against IG — the re-derivation asked for in `BL-78`

**Measured 2026-08-19.** This is `BL-78` item 3: *"can we have some research for 5-10 mobs of every lvl
of the IG to compare its stats to ours of the same lvl — i have the feeling that our mobs are weaker or
atleast with a lot less hp."*

**The short answer: the instinct is right.** Against the IG data a player actually meets today, our
creatures are **half the defence, half the attack, and — once IG's HP multipliers are counted — a third
to a fifth of the HP.** The one thing that is not wrong is the shape of our *base* HP curve above 40.

---

## 0. ⚠ THE THING THAT MAKES EVERY OLDER MEASUREMENT SUSPECT — TWO CHRONICLES

The public IG databases do not agree with each other, and not by a little. **The same creature id, read
from two of them:**

| Deinonychus, id 22225, level 80 | HP | P.Atk | M.Atk | P.Def | M.Def |
|---|---|---|---|---|---|
| older-chronicle database | 3,290 | 1,600 | 2,154 | 341 | 250 |
| current-chronicle database | **13,763** | **4,514** | 2,257 | **1,053** | 750 |
| **ratio** | **4.2×** | **2.8×** | 1.0× | **3.1×** | **3.0×** |

🔑 **`MobBaseStats` was fitted to the OLDER one.** The comment block in `MobBaseStats.cs` names its six
reference creatures — Keltir L1, Grizzly L17, Ghoul L32, Grandis L40, Invader Shaman L63, Tracker Howl
L81 — and those are all old-chronicle creatures. The 2026-07-14 fit was not sloppy; it was faithful to a
version of IG that has since been re-scaled roughly 3× on defence and attack.

**Everything below is measured against the CURRENT chronicle**, because that is the game he is comparing
against when he says ours feel weak. Sample: **170 creatures** pulled individually across levels 1-85
(≈10 per 5-level band), each giving HP, P.Def, M.Def, P.Atk, M.Atk **and its NPC skill list**, plus a
2,886-creature old-chronicle sweep used only for the cross-check above.

---

## 1. How IG actually authors a creature — and it is our design

Every IG creature carries a list of graded passives. A level-83 Drakos Warrior reads, verbatim:

> `HP Increase (3x)` Lv11 · `Strong P. Atk.` Lv15 · `Average M. Atk.` Lv11 · `Very Strong P. Def.` Lv17 ·
> `Weak M. Def.` Lv7 · `Standard Type` Lv2 · `Bare Hands` Lv1 · `Dragons` Lv10 · `Fire Attacks` Lv1

🔑 **That is `MobMod` / `MobMasteries`, one for one** — and it confirms the design note already sitting in
`MobBaseStats.cs`: a lean shared base curve, with identity bought by passives on top. The architecture is
right. What is missing is that **we barely use the layer.**

### The HP multiplier is the load-bearing one, and it is common

In the 170-creature sample the `HP Increase` tag distributes:

| tag | count | share |
|---|---|---|
| `1x` | 122 | 77% |
| `2x` | 14 | 9% |
| `4x` | 15 | 9% |
| `3x` | 6 | 4% |
| `5x` | 2 | 1% |

**Roughly a quarter of all creatures carry a multiplier, and it goes to ×5** (he reports seeing ×6, which
this sample simply did not reach — nothing contradicts it).

✅ **This is exactly his read, and the arithmetic lands:** base HP at 76 is **4,298**, so
- ×3 → **12,894** — his *"the 15k were a x3hp mobs"*
- ×5 → **21,490** — his *"normal field mobs have x6 hp and have 21k hp"*

So the 15k and 21k creatures are real, they are ordinary field creatures, and they get there **through the
multiplier, not through the base curve.**

---

## 2. The curve, current chronicle

`IG base` = the modal value among `1x` creatures at that level — i.e. the shared curve before any
multiplier. `our` = `MobBaseStats` today.

| lvl | IG base HP | our HP | x | IG P.Def | our | x | IG M.Def | our | x | IG P.Atk | our | x |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1  | 62    | 40    | **0.65** | 39  | 44  | 1.13 | 29  | 30  | 1.03 | 8     | 4     | 0.50 |
| 6  | 147   | 68    | **0.46** | 51  | 44  | 0.86 | 40  | 30  | 0.75 | 14    | 10    | 0.71 |
| 11 | 292   | 136   | **0.47** | 68  | 46  | 0.68 | 53  | 34  | 0.64 | 25    | 20    | 0.80 |
| 16 | 417   | 244   | **0.59** | 72  | 67  | 0.93 | 62  | 50  | 0.81 | 50    | 34    | 0.68 |
| 21 | 364   | 392   | 1.08 | 90  | 88  | 0.98 | 76  | 66  | 0.87 | 52    | 52    | 1.00 |
| 26 | 769   | 580   | 0.75 | 149 | 109 | 0.73 | 120 | 82  | 0.68 | 100   | 75    | 0.75 |
| 31 | 1,003 | 808   | 0.81 | 166 | 130 | 0.78 | 119 | 97  | 0.82 | 141   | 105   | 0.74 |
| 36 | 1,278 | 1,076 | 0.84 | 181 | 151 | 0.83 | 150 | 113 | 0.75 | 216   | 143   | 0.66 |
| 41 | 1,593 | 1,384 | 0.87 | 177 | 172 | 0.97 | 154 | 129 | 0.84 | 374   | 187   | **0.50** |
| 46 | 1,943 | 1,732 | 0.89 | 263 | 193 | 0.73 | 209 | 145 | 0.69 | 454   | 255   | **0.56** |
| 51 | 2,323 | 2,120 | 0.91 | 319 | 214 | **0.67** | 234 | 161 | 0.69 | 607   | 333   | **0.55** |
| 56 | 2,724 | 2,548 | 0.94 | 325 | 235 | 0.72 | 281 | 176 | **0.63** | 815   | 431   | **0.53** |
| 61 | 3,136 | 3,016 | 0.96 | 317 | 256 | 0.81 | 281 | 192 | **0.68** | 1,150 | 556   | **0.48** |
| 66 | 3,546 | 3,524 | 0.99 | 451 | 277 | **0.61** | 365 | 208 | **0.57** | 1,170 | 708   | 0.61 |
| 71 | 3,939 | 4,072 | 1.03 | 487 | 298 | **0.61** | 387 | 224 | **0.58** | 1,568 | 892   | **0.57** |
| 76 | 4,298 | 4,660 | 1.08 | 622 | 319 | **0.51** | 434 | 240 | **0.55** | 1,614 | 1,113 | 0.69 |

### What it says

- ✅ **Our base HP shape is right from 40 up** — 0.87 → 1.08 of IG's base, drifting mildly hot at the top.
  The `40 + 0.8·lvl²` curve does not need replacing.
- 🔴 **Our base HP is HALF of IG's below 20** (0.46-0.65 at levels 6-16). The early game is the worst-fitted
  part of the curve and nobody has ever looked at it.
- 🔴 **Defence is ~0.5-0.8× and gets worse with level** — 0.51 P.Def and 0.55 M.Def at 76. This directly
  reverses what the old fit concluded; against the current chronicle our creatures are *paper*, not walls.
- 🔴 **Attack is ~0.5-0.7× across the entire midgame and endgame.** This is the flattest, most consistent
  deficit in the table.
- 🔴 **And none of the above counts the multipliers.** Add the ~23% of creatures carrying ×2-×5 HP and the
  effective gap on those is another 2-5× on top.

🔑 **Put together: an IG creature at 60-76 has roughly twice our defence, twice our attack, and — if it is
one of the multiplied ones — three to five times our HP.** That is *"no thrill in fighting"* with a
measurement behind it.

---

## 3. His two direct questions

### "Will this allow us to alter a mob with a custom value — two mobs both ×1 HP, one 200k and one 5k?"

**Yes, and IG does exactly that — but it is the exception, not the rule.**

The rule: at a given level, `1x` creatures overwhelmingly share ONE HP number. Every `1x` creature sampled
at 41 reads **1,593**; at 46, **1,943**; at 81, **2,917**. That is a shared base curve, and it is what gives
IG the global lever `BL-47` was worried about losing.

The exception: some creatures are authored clean off it while still tagged `1x`.

| id | name | lvl | tag | HP |
|---|---|---|---|---|
| 22688 | Evil Spirit of the Mine | 82 | `1x` | **3,643** |
| 22795 | Divinity Manager | 84 | `1x` | **32,745** |
| 22225 | Deinonychus | 80 | `1x` | **13,763** |

Two levels apart, same tag, **9× the HP**. So the tag is a **descriptor on the sheet, not the computation** —
the creature's HP is authored and the tag reports roughly where it landed.

🔑 **What this means for us:** keep `MobBaseStats` as the global lever, and treat `MobMod.Hp` as the normal
way a creature gets its bulk — which is already how it works. The engine needs **no change** to do what he
is describing. What is missing is *authoring*: today the multiplier is used on a handful of creatures, and
in IG it is on a quarter of them.

### "There is something called Vitality, 0 to 3720 — what does it do?"

**It is the EXP-bonus economy and has nothing to do with HP or difficulty.** Vitality is a player resource
that grants bonus EXP/SP (up to +300%), and it is *consumed* by killing ordinary creatures. The per-creature
number is how much a kill drains:

> **Vitality consumed = Exp ÷ Level² × 100 ÷ 9**

Checked against his own reading — creature 22688, exp 19,224 at level 82:
`19,224 ÷ 6,724 × 11.11 =` **31.8**, and he read **32**. It is a derived number, not an authored knob.

⚠ **So it is not the mechanism he was hoping for**, and it should not be modelled. If we ever build a
vitality/rest-bonus system it belongs with EXP rates, not with the mob curve. The answer to "can two
creatures at the same level have wildly different HP" is the section above, and it is yes for a different
reason.

⚠ One reading detail worth knowing: **his database prints HP with the multiplier already applied; the one
used here prints it before.** Creature 22757 reads 111,547 here with `HP Increase (2x)`, and he read
**223k** — exactly 2×. Neither is wrong; they are showing different halves of the same sum. Worth
remembering before two numbers get compared that were never the same quantity.

---

## 4. What `BL-78` should actually do

The entry says *"the 80 mobs should have 15k not 5"*. **That target is correct and it is reachable without
touching the base curve** — which is the good news, because the base curve is the one edit that moves every
creature at once and the one `BL-47` warned about spending.

In order of size:

1. 🔴 **Author the HP multiplier across the roster.** Base 4,298 at 76 × 3 = 12,894; × 5 = 21,490. His 15k
   and 21k both fall out. `MobMod.Hp` already exists and already works. Target something like IG's own mix —
   ~75% at ×1, ~25% spread over ×2-×5, with the fat ones being the ones that should read as dangerous.
2. 🔴 **Raise creature ATTACK ~1.5-2×** across 40-80. This is the most consistent deficit in the table and
   it is the one that produces *"tank get hit fo 30"*. Author it as a graded attack preset, the way IG does.
3. 🔴 **Raise creature DEFENCE ~1.3-2×**, weighted to the top (P.Def 0.51 and M.Def 0.55 at 76). ⚠ This one
   directly contradicts the 2026-07-14 note in `MobBaseStats.cs` claiming defence was already right — that
   note was true against the older chronicle and is false against this one. It should be rewritten, not
   deleted, so the reason is preserved.
4. 🟡 **Fix the early game.** Our base HP is **~0.5× IG's at levels 6-16**. Nobody has complained, because
   the early game is short — but it is the worst-fitted stretch of the curve.
5. 🔴 **Stop charging caster creatures twice** (`BL-78` item 2). IG's caster tag is `Light Armor Type` —
   *"Weak P. Def. and strong Evasion"* — it costs defence and buys evasion, and **does not touch HP**. Ours
   pays `PDef 0.5` *and* a small HP pool. His *"caster mobs are not weaker than the other, they just use
   spells (and have a bit less pdef, evasion not twice less)"* is IG's own rule, word for word.
6. ⚠ **Mob social clans are OFF** (`GameConstants.MobClansEnabled`, `BL-73`). A large share of IG's field
   danger is the pack answering. Any "thrill" judgement made with clans off is measuring a different game,
   and this half is already built.

### 🔵 The one decision that is his
**Which chronicle do we target?** Everything above measures against the current one. Adopting it means a
roughly 2× defence and attack rescale of every creature in the game plus a real multiplier layer — and then
**every player-facing number moves**: TTK, farm times, EXP/hour, the `BL-13` boss table, the `BL-22` farm
budget. That is not a reason not to do it; it is a reason to do it in one measured pass with
`BalanceMatrix` before and after, rather than in pieces.

### ⚠ One observation that does not reproduce
*"the 60 lich is with 1500"* — the only level-60 lich in the game is the Proving Grounds **Cairn Lich**, and
`BalanceMatrix` `G3.8` reads it at **2,909 HP**, exactly on curve (×1.00). Worth pinning down before it is
treated as evidence, because as a curve datapoint it is wrong by half.

---

## 5. Method, so this is repeatable

- **Current chronicle**, 170 creatures: individual lookups on a database that publishes the full stat block
  *and* the NPC skill list, sampled ≈10 per 5-level band across 1-85. Fields: level, HP, MP, P.Def, M.Def,
  P.Atk, M.Atk, STR/DEX/INT/WIT, elemental defences, and every NPC skill with its id and level. The
  `HP Increase (Nx)` tag is read from that skill list, which is what makes the base-vs-multiplier split
  possible at all.
- **Older chronicle**, 2,886 creatures: bulk level-band listings (name, level, aggro, exp, sp, HP, P.Atk,
  M.Atk, runSpd, atkRange), used only for the cross-chronicle check in §0 and for the exp figures behind
  the vitality formula.
- "IG base" = the **modal** value among `1x` creatures at a level, which is well defined because they agree
  exactly (n=3-10 per level); "IG P.Def/M.Def/P.Atk" = the **median** among the same creatures, which do
  *not* agree exactly because those tiers vary per creature.
- Excluded: creatures above 85 (instance and epic content — the median at 85 is 111,546 HP and meaningless),
  zero-exp rows, and the fortress-siege garrison families.
- Ours: `MobBaseStats.Hp/PDef/MDef/PAtk` read directly, plus the `=== MOB CURVE ===` block of
  `tools/BalanceMatrix`.
