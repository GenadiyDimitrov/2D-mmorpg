# The mob curve against IG — the re-derivation asked for in `BL-78`

**Measured 2026-08-19, refitted and BUILT the same day (0.73.0).** This began as `BL-78` item 3:
*"can we have some research for 5-10 mobs of every lvl of the IG to compare its stats to ours of the
same lvl — i have the feeling that our mobs are weaker or atleast with a lot less hp."*

**The short answer: the instinct was right.** Against the IG data a player actually meets today, our
creatures were **half the defence and half the attack**, and — once IG's HP multipliers are counted —
a third to a fifth of the HP on the creatures that carry one. The one thing that was never wrong is
the shape of our *base* HP curve above 40.

**What shipped in 0.73.0:** P.Def, M.Def, P.Atk and M.Atk refitted to the current-chronicle curve, as
**one smooth function each**. HP untouched. §6 is the before/after.

---

## 0. ⚠ THE THING THAT MAKES EVERY OLDER MEASUREMENT SUSPECT — TWO CHRONICLES

The public IG databases do not agree with each other, and not by a little. **The same creature id, read
from two of them:**

| Deinonychus, id 22225, level 80 | HP | P.Atk | M.Atk | P.Def | M.Def |
|---|---|---|---|---|---|
| older-chronicle database | 3,290 | 1,600 | 2,154 | 341 | 250 |
| current-chronicle database | **13,763** | **4,514** | 2,257 | **1,053** | 750 |
| **ratio** | **4.2×** | **2.8×** | 1.0× | **3.1×** | **3.0×** |

🔑 **`MobBaseStats` was fitted to the OLDER one.** The old comment block in `MobBaseStats.cs` named its
six reference creatures — Keltir L1, Grizzly L17, Ghoul L32, Grandis L40, Invader Shaman L63, Tracker
Howl L81 — and those are all old-chronicle creatures. The 2026-07-14 fit was not sloppy; it was faithful
to a version of IG that has since been re-scaled roughly 3× on defence and attack. **That history is
kept in the new comment block, not deleted**, so whoever refits this next knows that "measured against
IG" is not one number.

**Everything below is measured against the CURRENT chronicle**, because that is the game he is comparing
against when he says ours feel weak.

---

## 1. How IG actually authors a creature — and it is our design, exactly

Every IG creature carries a list of graded passives. A level-83 Drakos Warrior reads, verbatim:

> `HP Increase (3x)` Lv11 · `Strong P. Atk.` Lv15 · `Average M. Atk.` Lv11 · `Very Strong P. Def.` Lv17 ·
> `Weak M. Def.` Lv7 · `Standard Type` Lv2 · `Bare Hands` Lv1 · `Dragons` Lv10 · `Fire Attacks` Lv1

🔑 **That is `MobMod` / `MobMasteries`, one for one.** A lean shared base curve, with identity bought by
passives on top. The architecture is right; what was wrong was the curve underneath it.

**And the tag layer is our tag layer, measured.** Taking, at each level, the median of the creatures
carrying each tier word and dividing by the `Average` creatures of the same level (pooled over levels
20-90):

| tier word | measured × Average | `MobMasteries.DefTable` rung |
|---|---|---|
| `Weak P. Def.` | **×0.82** | 0.83 (L10) |
| `Average P. Def.` | ×1.00 | 1.00 (L12, the neutral) |
| `Strong P. Def.` | **×1.21** | 1.21 (L14) |
| `Very Strong P. Def.` | **×1.61** | 1.61 (L17) |

`P. Atk.` reads the same ladder (×0.82 / ×1.00 / ×1.27). **The passive layer needed no change at all** —
only the curve under it did.

🔑 **This is also what makes the base curve READABLE rather than guessed.** "IG base" below is not a
median over a mixed roster: it is the median over the creatures **IG itself tags as `Average`**, which
is the ×1 rung by construction.

### The HP multiplier is the load-bearing one, and it is common

In the sample the `HP Increase` tag distributes ~77% `1x`, ~23% spread over ×2-×5 (one ×8 seen).

✅ **This is exactly his read, and the arithmetic lands:** base HP at 76 is **4,298**, so
- ×3 → **12,894** — his *"the 15k were a x3hp mobs"*
- ×5 → **21,490** — his *"normal field mobs have x6 hp and have 21k hp"*

So the 15k and 21k creatures are real, they are ordinary field creatures, and they get there **through
the multiplier, not through the base curve.** ⚠ **That authoring is still owed** — see §7.

---

## 2. The curve, current chronicle, as measured

`IG` = the median among the creatures tagged `Average <stat>` at that level. `old` = `MobBaseStats`
before 0.73.0. `new` = the fitted curve that shipped.

| lvl | IG P.Def | old | new | IG M.Def | old | new | IG P.Atk | old | new | IG M.Atk | old | new |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1  | 39  | 44  | 39  | 29  | 30  | 30  | 8     | 4     | 8     | 3     | 2    | 3    |
| 5  | 49  | 44  | 49  | 37  | 30  | 38  | 12    | 9     | 13    | 5     | 5    | 6    |
| 10 | 64  | 44  | 64  | 50  | 32  | 51  | 22    | 18    | 23    | 11    | 12   | 10   |
| 15 | 83  | 63  | 81  | 66  | 47  | 65  | 34    | 31    | 39    | 21    | 21   | 18   |
| 20 | 102 | 84  | 102 | 80  | 63  | 82  | 57    | 48    | 63    | 25    | 32   | 30   |
| 25 | 126 | 105 | 125 | 103 | 79  | 101 | 93    | 70    | 96    | 44    | 48   | 47   |
| 30 | 134 | 126 | 151 | 114 | 95  | 123 | 132   | 98    | 142   | 64    | 67   | 70   |
| 35 | 183 | 147 | 181 | 143 | 111 | 147 | 200   | 135   | 203   | 106   | 93   | 103  |
| 40 | 207 | 168 | 214 | 164 | 126 | 174 | 275   | 171   | 283   | 133   | 118  | 146  |
| 45 | 243 | 189 | 251 | 201 | 142 | 204 | 394   | 241   | 386   | 206   | 167  | 203  |
| 50 | 280 | 210 | 292 | 227 | 158 | 237 | 509   | 316   | 515   | 264   | 220  | 277  |
| 55 | 335 | 231 | 337 | 271 | 174 | 272 | 695   | 410   | 676   | 387   | 286  | 370  |
| 60 | 373 | 252 | 385 | 302 | 190 | 311 | 861   | 529   | 874   | 473   | 370  | 487  |
| 65 | 481 | 273 | 438 | 355 | 205 | 353 | 1,115 | 675   | 1,114 | 655   | 473  | 631  |
| 70 | 477 | 294 | 496 | 364 | 221 | 398 | 1,304 | 852   | 1,402 | 776   | 598  | 807  |
| 76 | 622 | 319 | 571 | 434 | 240 | 457 | 1,614 | 1,113 | 1,822 | 995   | 782  | 1,069 |

### What it says

- 🔴 **Defence was ~0.5-0.8× and got worse with level** — 0.51 P.Def and 0.55 M.Def at 76. This directly
  reverses what the old fit concluded; against the current chronicle our creatures were *paper*, not
  walls. **Fixed in 0.73.0.**
- 🔴 **Attack was ~0.5-0.7× across the entire midgame and endgame** — the flattest, most consistent
  deficit in the table. **Fixed in 0.73.0.**
- ✅ **M.Atk was the one attack column that was never badly out** (0.9-1.2× of IG below 40), which is why
  it rises only ×1.37 at the top while P.Atk rises ×1.65.
- ✅ **Our base HP shape is right from 40 up** — 0.87 → 1.08 of IG's base. The `40 + 0.8·lvl²` curve does
  not need replacing. 🔴 Below 20 it is ~0.5× IG's; ruled acceptable (levelling is fast there).
- 🔴 **And none of the above counts the multipliers.** Add the ~23% of creatures carrying ×2-×5 HP and
  the effective gap on those is another 2-5× on top. That layer is still unauthored — §7.

---

## 3. His two direct questions

### "Will this allow us to alter a mob with a custom value — two mobs both ×1 HP, one 200k and one 5k?"

**Yes, and IG does exactly that — but it is the exception, not the rule.**

The rule: at a given level, `1x` creatures overwhelmingly share ONE HP number. Every `1x` creature
sampled at 41 reads **1,593**; at 46, **1,943**. That is a shared base curve, and it is what gives IG the
global lever `BL-47` was worried about losing.

The exception: some creatures are authored clean off it while still tagged `1x` — 22688 (L82, `1x`,
**3,643**) against 22795 (L84, `1x`, **32,745**). Two levels apart, **9× the HP**. So the tag is a
**descriptor on the sheet, not the computation**.

🔑 **What this means for us:** keep `MobBaseStats` as the global lever, and treat `MobMod.Hp` as the
normal way a creature gets its bulk — which is already how it works. The engine needs **no change**.

### "There is something called Vitality, 0 to 3720 — what does it do?"

**It is the EXP-bonus economy and has nothing to do with HP or difficulty.** Vitality is a player
resource granting bonus EXP/SP (up to +300%), *consumed* by killing ordinary creatures. The per-creature
number is how much a kill drains:

> **Vitality consumed = Exp ÷ Level² × 100 ÷ 9**

Checked against his own reading — creature 22688, exp 19,224 at level 82:
`19,224 ÷ 6,724 × 11.11 =` **31.8**, and he read **32**. It is a derived number, not an authored knob.

⚠ One reading detail worth knowing: **his database prints HP with the multiplier already applied; the one
used here prints it before.** Creature 22757 reads 111,547 here with `HP Increase (2x)`, and he read
**223k** — exactly 2×. Neither is wrong; they are showing different halves of the same sum.

---

## 4. The fit that shipped

His constraint, verbatim: *"everithing above lvl 20 should walk normal curve because there are
bosses/instances that will derive from it (with passives)"*. A boss is base × a passive, so **any kink in
the base is inherited and multiplied by every derived creature.** So the requirement was one function,
checked for smoothness *before* accuracy.

All four are the same family:

```
P.Def(L) = 0.00113  · (L + 44)^2.743
M.Def(L) = 0.0027   · (L + 38)^2.542
P.Atk(L) = 1.12e-6  · (L + 31)^4.539
M.Atk(L) = 1.14e-7  · (L + 32)^4.904
```

`a·(L + shift)^k` with positive `a`, `shift`, `k` is strictly increasing and infinitely differentiable at
every level — **no floor, no band, no piecewise table.** Both of the old discontinuities are gone with it:
the `Math.Max(44, …)` P.Def floor (a corner at level ~10) and the 57-node interpolated P.Atk/M.Atk table
(a slope change at every node).

**Verified off the compiled code**, levels 1-95, all four columns: **zero decreasing steps**; the only
second-difference wobble is ±1 from integer truncation (largest third difference above level 20 = 3
units), not a feature of the curve.

**Accuracy** against the measured series: P.Def and M.Def within 4% at every sampled level from 1 to 70
(worst: +13% at 30, −9% at 65); P.Atk within 8% from 25 to 70; M.Atk within 11%. The fit runs ~10% hot
above 70 — the price of one curve that also has to fit 45-70, and the direction he asked for anyway.

✅ **The sub-20 stretch came free, so it was taken.** Fitting from level 1 costs nothing above 20 (the
shift term does the work), and it fixes a real bug on the way past: a level-10 mage **no longer one-shots
a same-level creature** (`LOW LEVEL 1-10` in `BalanceMatrix`: nuke 149 → 92 against 120 HP).

---

## 5. Method, so this is repeatable

- **Source: `l2elo.com`, his call** — server-rendered, current chronicle, and the only database that
  publishes the **NPC skill list**, which is what makes the base-vs-tag split readable at all.
- **Next.js JSON API, no scraping.** `buildId` from `"buildId":"…"` in any page HTML (it changes when
  they redeploy). Roster: `/_next/data/<buildId>/en/database/npcs/type/l2monster.json?lvl=41-50` — ⚠ only
  the ten canonical decade bands are accepted, anything else is silently ignored, and each band is capped
  at **400 rows** (which is why 79-80 and 84-85 are missing: the 71-80 and 81-90 bands fill up before
  reaching them). Detail: `/_next/data/<buildId>/en/database/npcs/<id>.json` → `pageProps.npcData` is a
  JSON **string** holding `information.{level,hp,mp,physical_attack,magical_attack,physical_defence,
  magical_defence,evasion,accuracy,attack_speed,exp,…}` and `skills[]`.
- **Sample: 2,831 creatures — every `l2monster` the roster would return, levels 1-83**, each read
  individually with its full stat block and skill list.
- **Per-level value** = median among the creatures tagged `Average <stat>`. ⚠ **Levels ending in 4 and 9
  are excluded from the fit**: IG's raid bosses sit on those levels and their minions crowd the roster
  there (level 49 reads 498 P.Def against 280 at 50). Levels with fewer than 3 tagged creatures are
  dropped, then three rounds of 25% outlier rejection.
- **Fitting**: grid search on `shift`, weighted linear least squares on `log y` vs `log(L + shift)` —
  log space because the error that matters across a curve spanning 8 → 2,600 is *relative*, weighted by
  √(sample count) so a level with 86 creatures behind it outvotes one with three.
- ⚠ **Do NOT mix in `pmfun`/`dropspoil`** — those are the old chronicle. See §0.
- Excluded throughout: creatures above 83, raid/grand-boss types, and the 400-cap gaps above.

---

## 6. What it did to the game — `BalanceMatrix`, before and after

| | before | after |
|---|---|---|
| Champion TTK on a same-level creature, L60 / L80 | 20.5s / 17.9s | **31.4s / 33.2s** |
| Creature DPS onto that champion, L60 / L80 | 47 / 71 | **77 / 116** |
| Tank survives (standing still), L20 / L52 | 133s / 109s | **104s / 65s** |
| Nuker survives, L52 | 16s | **9s** |
| Kills before the HP bar empties, L52 champion / nuker | 26 / 6 | **9 / 2** |
| Field boss TTK, 3 DD, L60 / L76 / L85 | 684s / 888s / 693s | **1,046s / 1,586s / 1,353s** |
| Kills per hour, S band (80-85) | 75 | **65** |
| Full S-grade character, farm hours (`M12c`) | 347h | **603h** |

- ✅ **`BL-13` lands without touching a boss.** His playtest-25 ruling was *10-30 minutes*; field bosses
  now read **17.4 min at 60, 26.4 min at 76, 22.5 min at 85** — because a boss's defence is the base
  curve (rank multiplies HP and P.Atk only), so it inherited the rise. It was 11-15 min before.
- 🔴 **The farm economy is the bill.** TTK roughly doubles at the top, so an elite camp fell from 115% of
  a normal farm to **76%**, and a full S-grade character went from 347 to **603 farm hours**. `BL-22`'s
  budget was already unreachable at S; it is now unreachable by ~1.7×. **That solve has to be re-run
  against these numbers, not the old ones.**
- 🔴 **An unattended farm now dies.** "Kills before the HP bar empties" at 52 falls from 26 to 9 (nuker
  6 → 2). Auto-hunt at level and grade parity is no longer self-sustaining without consumables.
- ✅ EXP and SP **per kill** are unchanged: `MobKillTimeRatio` reads HP and P.Def as *ratios to the base
  curve*, so a normal creature still scores exactly 1.0 and the boss/elite premiums are untouched. What
  moved is EXP per *hour*, through TTK.

---

## 7. What `BL-78` still owes after this

1. 🔴 **Author the HP multiplier across the roster** — the biggest remaining item and the one his
   *"the 80 mobs should have 15k not 5"* actually names. `MobMod.Hp` exists and works; IG puts a
   multiplier on ~23% of creatures, we put it on a handful. Target IG's own mix: ~75% at ×1, the rest
   spread over ×2-×5, with the fat ones being the creatures that should read as dangerous.
2. 🔴 **Stop charging caster creatures twice** (`BL-78` item 2). IG's caster tag is `Light Armor Type` —
   *"Weak P. Def. and strong Evasion"* — it costs defence and buys evasion, and **does not touch HP**.
   Ours pays `PDef 0.5` *and* a small HP pool. His *"caster mobs are not weaker than the other, they just
   use spells (and have a bit less pdef, evasion not twice less)"* is IG's own rule, word for word.
3. ✅ **The player side of the same question** (`BL-78` item 4) — *"a healer with 1500 hp getting hit for
   300 is abit harsh"*. Creature attack rose ~1.65× here and made it louder. **Answered 2026-08-27 in
   0.91.0**, on the player side rather than by walking this refit back: player Max HP was refitted to
   IG's own per-class tables and CON curve, roughly ×1.9 for fighters and ×2.6-3.6 for the mage line.
   A robe at 52 went from 9s of survival to 21s. So this document's numbers still stand — what changed
   is the pool they are measured against. See `docs/Formulas.md` and `BalanceMatrix --hpcurve`.
4. ⚠ **Mob social clans are OFF** (`GameConstants.MobClansEnabled`, `BL-73`). A large share of IG's field
   danger is the pack answering. Any "thrill" judgement made with clans off is measuring a different
   game, and this half is already built.
5. 🔴 **Re-solve `BL-22`'s farm budget** against §6.

### ⚠ One observation that does not reproduce
*"the 60 lich is with 1500"* — the only level-60 lich in the game is the Proving Grounds **Cairn Lich**,
and `BalanceMatrix` `G3.8` reads it at **2,909 HP**, exactly on curve (×1.00). Worth pinning down before
it is treated as evidence, because as a curve datapoint it is wrong by half.
