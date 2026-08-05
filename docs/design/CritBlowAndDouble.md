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
