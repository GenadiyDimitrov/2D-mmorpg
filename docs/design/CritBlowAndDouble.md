# Crit damage, Blows and `[Double]` (spec)

Owner ruling, **2026-08-05**. This is a deliberate *simplification* of L2, not a copy of it — the
naming below was chosen because L2's own naming confuses players about where crit damage comes from.
Do not "restore" the L2 model.

## Vocabulary

| Ours | L2's name for the same thing |
|---|---|
| **`[Double]`** | physical *skill critical* |
| **crit** (basic attack) | critical hit |
| **Blow** | dagger blow (Backstab / Deadly Blow) |

> *"There is no DoubleDmg in L2 — our DoubleDmg **is** L2's physical skill crit."*

## 1. `[Double]` is a flat ×2 and nothing else

A `[Double]` never touches crit-damage values. You hit for 1000; when it doubles you see
`[Double] 2000`. That is the whole mechanic, and it is why it raises no questions about where crit
damage went — the confusion in L2 comes from blows applying crit-damage values *and then* critting
for a further ×2.

### Chance — pure ATK, floor 2.5%, ceiling 25%

```
ATK_Diff = max(0, 0.75 × (ATK − 30))
Double%  = min(25, 2.5 + ATK_Diff)
```

| ATK | 30 | 40 | 50 | 60+ |
|---|---|---|---|---|
| Double% | 2.5 | 10 | 17.5 | 25 (capped) |

**`ATK` is the STAT** (the 30-60 band), never `EffectiveAtk` / p.Atk, which gear and buffs push into
the hundreds. A better weapon must not buy Double chance — only the build does (base stat, dyes,
stat buffs).

This **replaces** the previous `max(DEX, ATK)` input and the previous 30% cap.

### Why ATK and not DEX

DEX already buys the blow's landing roll. Paying the ×2 off DEX as well would pay the rogue **twice
for one stat**, and would give the warrior — whose stat *is* ATK — nothing from the mechanic at all.

- **DEX makes the blow land. ATK makes it double.**
- A rogue who wants doubles must give up ATK, or dump CON and play a ~2k-HP glass cannon that dies
  to any blow or shot in return. Everything is a choice.

## 2. Blows scale off CRIT DAMAGE, not attack power

In L2 a blow's p.Atk contribution is almost irrelevant — 7-11k of skill power against under 1k of
p.Atk — and what actually grows a dagger's damage is **crit damage**. We reproduce that.

Sequence for a blow (`BlowOnCrit`):

1. **Land on a crit.** Roll the attacker's crit chance (DEX-driven), reduced by the target's
   `ShieldCritDefense` + `CritRateResist`. A blow that fails to crit deals a flat
   `BlowFailFraction` floor (~10%) which can neither crit nor double. Blows bypass shields, so the
   floor is not blocked.
2. **Apply the crit-damage values** to the landed blow — the flat crit-damage add (below) and any
   crit-damage multiplier.
3. **Then it may `[Double]`** — the ATK roll above, ×2 on top.

⚠ Step 2 is the piece that does not exist yet: `ResolveBlow` currently returns the base damage
unmodified, so a dagger's `crit dmg +80` does **nothing** to Stab today. Steps 1 and 3 are already
correct in code.

## 3. Crit damage in the CSVs is FLAT, added as attack before the multiplier

Every class CSV writes crit damage as a `+` — rogue `+35/+64/+80/+140/+165`, warrior 2H
`+35/+48/+64/+84/+106`. These are **flat numbers**, not percentages. The catalog previously divided
each by 100 and fed it to `PhysicalCritMult` (`2.0 + bonus`), so `crit dmg +80` wrongly meant
"crit multiplier ×2.8".

The flat amount joins **attack**, inside the ratio, on a crit only:

```
crit damage = K · ((atk + flatCritDmg)·lvlMod + power) / def  ×  critMult
```

So it is divided by defence like everything else, the crit multiplier scales it, and off a crit it
does nothing.

## 4. `[Double]` on buffs and debuffs = double duration

The same roll, applied to a buff or debuff instead of damage, makes it last twice as long. (L2's
level-76 Skill Mastery: fighters STR, mages INT.) Additive to the above; build it last.

## Consequence to watch

The rogue's damage curve becomes almost entirely the five `crit dmg +N` rungs, with Double as a rare
bonus on top. Mistune those five numbers and the class is mistuned with nothing else compensating.
**Measure with `tools/BalanceMatrix` before and after** — do not ship this hand-derived.

## Work items — ALL BUILT 2026-08-05

1. ✅ Rogue **armor** mastery — honours `with all` vs `with light`. `SkillCatalog.RogueArmor`
   (`Skills.Masteries.cs`) puts MP regen + flat P.Def (and the L5 HP regen) on **every** weight and
   keeps evasion / crit-rate resist / speed light-only, exactly as the warrior's already did.
2. ✅ Rogue **weapon** mastery — the @24 and @28 rungs were swapped; now +35/+64/+80/+140/+165.
3. ✅ Crit damage `+N` is flat attack inside the crit — `PassiveEffect.CritDamageFlat` →
   `Entity.CritDamageFlat` → `StatCalculator.CritFlatFactor`, on rogue, warrior and archer masteries.
   It rides as a FACTOR on the finished hit, which is exact: everything after the ratio is linear.
4. ✅ Blows apply the crit-damage values (`ResolveBlow`); `[Double]` is the ATK curve capped at
   `StatCaps.PhysicalDoubleRate` = 25%, and never reads DEX any more.
5. ✅ `[Double]` doubles buff/debuff duration — one roll per cast in the skill-cast path, PLAYER
   casts only (potions, scrolls and the NPC buffer come through other paths and never roll), shown
   as `Name [Double]` on the floating text.

Measured, not derived: `tools/BalanceMatrix` §C1 prints the Double curve old-vs-new, both mastery
ladders as OLD (multiplier) vs NEW (flat) expected damage, and rogue-vs-warrior sustained DPS.
The headline numbers at the five rungs (20/24/28/32/36):

- the **blow** roughly doubles in expected damage (90→148 … 178→354): the crit-damage ladder used
  to do literally nothing to it;
- a **basic attack** is unchanged to within ~1% — the flat add and the old bogus multiplier happen
  to be worth about the same on a small P.Atk, which is why this never looked broken;
- the rogue lands at **0.65× the warrior's DPS at 20-28**, then **0.94× at 32 and 1.04× at 36**
  when the mastery's crit-rate rung arrives. The early gap is the open tuning question: a blow
  lands full damage only on a crit, and that is 9.2% until level 32.

## Client

The stats window gained a `Crit dmg flat +N` / `[Double] N%` row (the Double figure is derived
client-side from the ATK stat, so it costs no protocol). `StatsUpdate.CritDamageFlat` is a trailing
optional field — no protocol bump, no db reset.

---

## 5. CRIT **RATE** — the L2 model (owner spec, 2026-08-06, playtest-19 M9). NOT BUILT.

His formula, on L2's 0-1000 scale (**1000 = 100%**, so the classic cap **500 = 50%**):

```
crit = (base_weapon_rate x buffs x passives + flat_bonuses) x debuffs x enemy_light_armor_mastery
```

**Weapon bases** — his own derivation `44 x weapon_crit / 4`, i.e. `11 x weapon_crit`:

| weapon | L2 `weapon_crit` | base (1000 scale) | = % |
|---|---|---|---|
| blunt / fist | 4 | 44 | 4.4 |
| sword / dual | 8 | 88 | 8.8 |
| dagger / bow | 12 | 132 | 13.2 |

**His worked ladder** (every number below reproduces from the formula):

| build | chain | result |
|---|---|---|
| dagger, 1 buff | 132 x1.3 | 171 |
| + rogue passive x1.2 | | 205 — *where bows stop* |
| melee rogue 50+, x1.5 passive instead | 132 x1.3 x1.5 | 257 |
| + Harmony x2 | | **514 -> capped 500 = 50%** |
| bow + Harmony | 205 x2 | **410 = 41%** |
| sword warrior + buff + Harmony | 88 x1.3 x2 | 228 |
| + "elegia heavy set" **flat +127** | | **355 = 35.5%** |
| blunt warrior + buff + Harmony | 44 x1.3 x2 | 114 |
| + flat +127 | | **241 = 24.1%** |

🔑 **Why the flat term exists, and it is the whole point of the model:** multipliers reward whoever
already has a big base, so a blunt warrior can never multiply his way to a usable crit rate. The heavy
set's **flat** +127 is what carries the classes that do not live on crits — and it is worth ~3x more to
the blunt than to the dagger. Keep a flat gear source of crit rate; do not "simplify" it into a
percentage.

### What our code already matches
- **The weapon ratios are ALREADY his.** `StatCalculator.WeaponCritFactor` is dual/bow **1.20**, sword
  **0.80**, blunt **0.40** — the same 3 : 2 : 1 as 132 : 88 : 44. Nothing to change.
- **Buffs already have a multiplicative path.** `Entity.RecomputeDerived` does
  `CritChance = (CritChance + flat) x (1 + percent)` for `BuffCritRate` — flat-then-percent, exactly the
  shape of his formula.
- A **flat** gear source exists: `AttributeType.CritRate`.
- A defender-side term exists: `CritRateResist`, already carried by the rogue's LIGHT armor mastery
  (0.15) = his `enemy_light_armor_mastery`.

### What has to change
1. 🔴 **Passives become MULTIPLIERS.** Today `CritChance + pe.CritRate` (additive points) at
   `Entity.cs:1702` and `:1736`. His model wants `x1.2` / `x1.5`, not `+20 points`.
2. 🔴 **`CritRateResist` becomes a MULTIPLIER, not a subtraction.** Today the resolver does
   `chance - target.CritRateResist` (`GameLoopService.cs:9481, 9523, 9554, 9572`). Subtraction annihilates
   low-crit builds — a 11.4% blunt warrior against a 0.15-resist rogue crits **0%**; as a multiplier he
   keeps 9.7%. This is the same reasoning as the flat term above.
3. ⚖ **The cap: ours is 0.75, his model assumes 0.50.** `StatCaps.PhysicalCritRate` is 0.50 but it only
   clamps the DEX step; passives, buffs and the final clamp all use **0.75** (`Entity.cs:1702, 1736,
   1890`). His ladder is authored against a 500 cap — a maxed dagger lands at 514 and is *supposed* to
   be capped. **Needs his call.** Recommendation: go to 0.50 with him, or the top of his ladder loses
   its ceiling and the dagger keeps climbing.
4. ⚖ **Where DEX goes.** Today the base *is* DEX: `PhysicalCritChance(dex) = 0.05 + dex x 0.0009`, then
   `x WeaponCritFactor`. His formula starts from a flat weapon base and never mentions DEX. L2 keeps a
   DEX modifier as a **multiplier on the weapon base**, which is also the smallest change here:
   `base_weapon x dexMod x buffs x passives + flat`. **Needs his call**, but that is my recommendation —
   dropping DEX from crit entirely would strip the rogue's main stat of its identity.

⚠ **Measure the 20-40 rogue curve in `tools/BalanceMatrix` before AND after** — see §50h. Multiplicative
alone *lowers* rogue DPS at low level (x1.2 on a 13.2% base is +2.6 points, where the old additive +20%
was +20). His ladder only reaches 50% once Harmony and the 50+ x1.5 passive exist, so the early game
must be re-checked, and the price may have to be paid in the blow's own modifier (L2 gave Mortal/Deadly
Blow ~+20% of their own).
