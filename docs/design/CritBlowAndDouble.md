# Crit damage, Blows and `[Double]` (spec)

Owner ruling, **2026-08-05**. This is a deliberate *simplification* of IG, not a copy of it — the
naming below was chosen because IG's own naming confuses players about where crit damage comes from.
Do not "restore" the IG model.

## Vocabulary

| Ours | IG's name for the same thing |
|---|---|
| **`[Double]`** | physical *skill critical* |
| **crit** (basic attack) | critical hit |
| **Blow** | dagger blow (Backstab / Deadly Blow) |

> *"There is no DoubleDmg in IG — our DoubleDmg **is** IG's physical skill crit."*

## 1. `[Double]` is a flat ×2 and nothing else

A `[Double]` never touches crit-damage values. You hit for 1000; when it doubles you see
`[Double] 2000`. That is the whole mechanic, and it is why it raises no questions about where crit
damage went — the confusion in IG comes from blows applying crit-damage values *and then* critting
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

This **replaces** the previous `max(AGI, ATK)` input and the previous 30% cap.

### `Can Crit` and `Can Double` are EXCLUSIVE, OPT-IN flags (owner ruling, playtest-19 M8)

> "If a skill is not described as `Can Crit` or `Can Double` it doesnt do it."

- `CanDouble` -> rolls the ATK curve, **never** a crit.
- `CanCrit` -> rolls the caster's crit rate (times the skill's own `CritRateMod`), **never** a double.
- `BlowOnCrit` -> the crit IS the landing gate; it may then double if it also has `CanDouble`.
- **Neither flag -> the skill lands flat.** It can still miss and can still be blocked.

This is a hard flag check, not a probability. It was prompted by a double-only Strike producing more
big hits than the blow it is meant to be worse than — and by a `[Double]` **reporting itself as a
crit**, which is exactly the confusion the `[Double]` naming exists to avoid. A double now returns
`CombatOutcome.Double` and the client draws it as `N x2` in its own colour.

`tools/BalanceMatrix` **§C3** audits every physical-damage skill's flags against its own description;
a row where they disagree is an authoring bug, not a formula bug.

### Why ATK and not AGI

AGI already buys the blow's landing roll. Paying the ×2 off AGI as well would pay the rogue **twice
for one stat**, and would give the warrior — whose stat *is* ATK — nothing from the mechanic at all.

- **AGI makes the blow land. ATK makes it double.**
- A rogue who wants doubles must give up ATK, or dump CON and play a ~2k-HP glass cannon that dies
  to any blow or shot in return. Everything is a choice.

## 2. Blows scale off CRIT DAMAGE, not attack power

In IG a blow's p.Atk contribution is almost irrelevant — 7-11k of skill power against under 1k of
p.Atk — and what actually grows a dagger's damage is **crit damage**. We reproduce that.

Sequence for a blow (`BlowOnCrit`):

1. **Land on a crit.** Roll the attacker's crit chance (AGI-driven), reduced by the target's
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

The same roll, applied to a buff or debuff instead of damage, makes it last twice as long. (IG's
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
   `StatCaps.PhysicalDoubleRate` = 25%, and never reads AGI any more.
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

## 5. CRIT **RATE** — the IG model (owner spec, 2026-08-06, playtest-19 M9). ✅ BUILT 2026-08-06 (0.50.0).

His formula, on IG's 0-1000 scale (**1000 = 100%**, so the classic cap **500 = 50%**):

```
crit = (base_weapon_rate x buffs x passives + flat_bonuses) x debuffs x enemy_light_armor_mastery
```

**Weapon bases** — his own derivation `44 x weapon_crit / 4`, i.e. `11 x weapon_crit`:

| weapon | IG `weapon_crit` | base (1000 scale) | = % |
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

### THE SPEC — his final numbers, 2026-08-06

```
crit = ( 110 x weaponFactor x dexMod x buffs x passives + flat ) x debuffs x enemyLightArmorMastery
       clamp 0 .. 500 (50%).   Magic crit clamps at 200 (20%).
```

**1. `CHARACTER_BASE = 110` (11%), and the WEAPON multiplies it** — his *"character crit rate base is
110"*. `WeaponCritFactor` already carries the right multipliers, so this falls out with no new table:

| weapon | factor (existing) | 110 x factor | = % |
|---|---|---|---|
| dagger / bow (`Dual`, `Bow`) | 1.20 | **132** | 13.2 |
| sword / 2H sword (`Sword`) | 0.80 | **88** | 8.8 |
| blunt / 2H blunt (`Blunt`) | 0.40 | **44** | 4.4 |

(`WeaponType.Base()` folds `TwoHandedSword -> Sword`, `TwoHandedBlunt -> Blunt`, so a 2H gets its 1H
factor. Daggers ARE `Dual` in this codebase.)

**2. `dexMod` = 1% per point, centred on 30** — his table `20 = -10%, 25 = -5%, 30 = 0, 35 = +5%,
40 = +10%`, i.e. **`dexMod = 1 + (EffectiveAgi - 30) x 0.01`**. He rejected the old "5% + 1% per 10" as
too heavy: *"the dex alters most stats than any other mainStat - as, crt, acc, Eva."*
🔑 **30 is also `MobAgiReference`**, so a normal mob sits at exactly x1.00 — the neutral opponent.

**3. `flat` lands OUTSIDE every multiplier** — *"a flat 30 is flat 3%, not increased by buffs."*
⚠ Our buff code does the opposite today (`(crit + flat) x (1 + pct)`, `Entity.cs:1861`).

**4. The cap is 500 = 50% physical, 200 = 20% magic.** His call.

**His worked archer:** Elf rogue with a bow, AGI 40 -> `132 x 1.1 x 1.3 x 2 x 1.2 = 453` (**45.3%**) vs
`411` (41.1%) with no AGI bonus — *"a good 4.2%, still not max."*

### The AGI table he asked for (`StatCalculator.GetBaseStats`)

⚠ **AGI is per RACE + BASE CLASS only — there is no archetype split.** A Human Knight, Champion and
Assassin all sit at AGI 30; a rogue gets nothing for being a rogue. **The only AGI identity is being an
ELF.**

| race + base class | AGI | `dexMod` |
|---|---|---|
| **Elf fighter** | **35** | x1.05 |
| **Human fighter** | **30** | x1.00 *(= the mob reference)* |
| **Demon fighter** | **26** | x0.96 |
| Elf mage | 24 | x0.94 |
| Human mage | 21 | x0.91 |
| Demon mage | 20 | x0.90 |

So his *"an elf with 36 and +5 maxes it over 40"* is the **Elf fighter at 35**, +5 from gear -> 40 ->
x1.10. Reachable: the light `Nightleaf` sets carry `Agi +3` and `+1`. ⚠ And the other direction —
**heavy sets carry `Agi -2` / `-1`**, so a heavy warrior sits BELOW neutral (30 - 2 = 28 -> x0.98).

✅ **`dexMod` is LINEAR AND UNCAPPED** (his ruling, 2026-08-06):
> Leave it uncapped - a full dex archer with dex set with stat swap with whatever can reach cap ... And
> a warrior sacrificing dex for atk and con hinders his rate (low hinder but still a hinder)

**How far it actually reaches, measured against what exists today:**

| build | AGI | `dexMod` | bow ladder `132 x1.3 x1.2 x2 = 411.8` |
|---|---|---|---|
| Elf rogue, base | 35 | x1.05 | 432 = 43.2% |
| + light `Nightleaf` set (`Agi +3`) | 38 | x1.08 | 445 = 44.5% |
| + `swap_dex_atk` / `swap_dex_con` maxed (**+5**, 5 levels of +1) | **43** | **x1.13** | **465 = 46.5%** |
| *the cap* | *51.4* | *x1.214* | *500 = 50%* |

🔑 **So the cap is currently ~35 points out of reach even for a fully committed elf archer** — it needs
another AGI source, which is what the future **dye / tattoo layer** is for. That is the right shape: the
ceiling is aspirational rather than something a level-40 elf bumps into by accident, and it matches his
own reaction to 45.3% — *"still not max."*

**And the warrior's side of it, exactly as he described:** human fighter 30, heavy set `Agi -2`,
`swap_atk_dex` maxed `-5` -> **AGI 23 -> x0.93**. A max-ATK warrior pays 7% of his crit rate — sword
`88 -> 81.8`. Real, and small enough to be a trade rather than a trap.

### 🛑 GUARDRAIL — do not inflate the AGI crit term
> "dex main priority is not the crit rate as much as evasion and acc stats" (owner, 2026-08-06)

AGI has FOUR jobs and crit is deliberately the smallest. One point of AGI is worth, to a dagger user:

| AGI +1 | worth |
|---|---|
| **accuracy** | **+1.0 percentage point** of hit chance (`StatCaps.AvoidStatSlope` = 0.01) |
| **evasion** | **+1.0 percentage point** of avoid |
| attack speed | x1.0105, compounding (`AttackAgiModifier`) |
| **crit rate** | `dexMod +0.01` -> `132 x 0.01` = **+0.13 percentage points** |

**A AGI point is ~7.5x more valuable to accuracy than to crit on a dagger, ~11x on a sword** — that
ratio IS the design. If crit-from-AGI ever looks "too weak", it is not a bug; the mild multiplier is
what stops AGI becoming the one stat everyone stacks.

🔑 Consistency worth keeping: `AttackAgiModifier` is `1.0105^(AGI-30)` — the same ~1%/point centred on
the same 30 as `dexMod`. Attack speed and crit read the same way; accuracy and evasion are the flat,
dominant 1 point = 1%.

### What this changed in the code — ALL BUILT 2026-08-06 (0.50.0)
1. ✅ **Passives are MULTIPLIERS.** `Entity.CritRateMult` accumulates every `StatMods.CritRate` and
   `PassiveEffect.CritRate` as `x (1 + v)`; the authored numbers already read as percentages, so the
   conversion is mechanical (`CritRate: 0.20f` was `+20 points`, it is now `x1.20`).
2. ✅ **`CritRateResist` is a MULTIPLIER.** All four resolution sites now do
   `(chance - shieldCritDefense) x (1 - CritRateResist)`.
3. ✅ **Every clamp reads the StatCaps constant**, and there is now only ONE, at the end of the chain.
   The three stray `0.75`s are gone; magic clamps at `StatCaps.MagicCritRate` (0.20).
4. ✅ **AGI is a multiplier, not the base** — `StatCalculator.PhysicalCritBase(dex, weapon)` =
   `110 x weaponFactor x dexMod`, and `CritAgiMod` is the linear uncapped `1 + (dex - 30) x 0.01`.
5. ✅ **`flat` lives outside every multiplier** — `Entity.CritRateFlat`, folded in at the end as
   `base x mult + flat`.

✅ **Magic crit WAS converted, in 0.51.0** (owner ruling 2026-08-06) — this section used to say it was
deliberately left additive because "a mage's base is a 4% WIT figure where a x1.05 is worth nothing".
That base was the actual defect: `WIT x 0.001` put a human mage at **2.0%**, so the x2 Insight buff was
worth **+3 points** and the 200 cap needed **WIT 200**. Magic now runs the same chain:

```
magicCrit = ( 40 x witMod x passives x buffs  +  flat ) x debuffs      clamp 0 .. 200 (20%)
```

with **no weapon term** (rate is WIT + buffs only) and `witMod` **asymmetric** — `+0.10`/point above
the WIT-20 anchor, `+0.05`/point below, clamped at 0. The lower slope is load-bearing: a symmetric
0.10 zeroes the stat at WIT 10, and both the demon fighter (10) and every mob (5) live down there.

⚠ **The head of that chain was 50 until 2026-08-19** and is now **40**. Not a nerf for its own sake: at
50 the fully-kitted elf (WIT 30) computed *exactly* the 20% cap off Insight alone, so he was pinned to
the ceiling and the 4th-class crit-rate buff would have bought him nothing — nor would raising the cap.
At 40 he reads **8% bare / 16% with Insight / 32% at ×4**, i.e. the cap is a real ceiling with room
under *and* over it. That was the owner's whole point: *"one day if we want to increase it, no mage to
be short on crit"*.

Magic crit **damage** is `x2 base × ∏(1 + multipliers) × (1 − Σ debuffs)`, clamped `[1, 5]` — the
owner's formula, 2026-08-19. It was a flat **x3** taking no bonus at all from 2026-08-06; the flat form
existed because it used to read `CritDamageBonus`, the single crit-damage field shared with physical,
so Ferocity and the crit-damage attribute (both fighter buffs) paid a mage too. **That rule still
holds** — the chain reads `Entity.MagicCritDamageMult`, its own channel, fed only by
`SkillDef`/`PassiveEffect`/`StatMods.MagicCritDamage`. What changed is that the channel now has a knob,
for the 4th-class buffer/healer blessings (+30% each, **compounding**: ×2.6 alone, ×3.38 together).
Rate and damage are both their own channel. See `CHANGELOG.md` 2026-08-19 and the `=== MAGIC CRIT ===`
section of `tools/BalanceMatrix`.

### What it MEASURED (`tools/BalanceMatrix` §C2 / §C3) — read this before retuning anything

🔑 **§50h ("the rogue is at 0.65x warrior DPS, gated by a 9.2% crit") was a MEASURING ERROR.**
`BalanceMatrix` built its characters from the class tables only, and the archetype identity passive
(`SkillCatalog.FloorPassiveFor` -> Evasion Mastery / Reflexes / Precision / Anti-Magic) is **auto-granted
by `AutoLearnCoreSkills`, not in those tables**. So it measured a rogue with **no Evasion Mastery** —
and Evasion Mastery was where the old `+20 crit points` lived. With it in the model, the numbers before
this change were:

| lvl | rogue crit | rogue/warrior DPS |
|---|---|---|
| 20 | 29.2% | **0.99x** |
| 28 | 29.2% | 1.08x |
| 32 | 39.2% | **1.46x** |
| 36 | 39.2% | **1.63x** |

i.e. the rogue was at parity early and **running away exactly at the level-32 spike he predicted**
("the balance will shift at lvl 32 when each blow lands with the 64+% chance"). His instinct was right;
the tool was wrong. Both builders grant the floor passive now — never measure a character without it.

**After**, with the blow modifier paying for the change: 0.92x / 0.95x / 1.00x / 1.10x / 1.22x at
20/24/28/32/36. The spike is gone and the curve is smooth.

### 6. `SkillDef.CritRateMod` — the per-SKILL crit rate (the price of going multiplicative)

Multiplicative crit alone drops the rogue to a flat **0.75x** the warrior at every level: `x1.2` on a
13.2% base is +2.6 points where the old additive `+20%` was +20. The design note anticipated paying for
that "in the blow's own modifier (IG gave Mortal/Deadly Blow ~+20% of their own)", and that is what
`CritRateMod` is: a multiplier on the caster's crit rate **for that skill's roll only**.

**Stab and Piercing Stab carry `CritRateMod: 2.0`**, so a 15.8% rogue's blow lands at 31.6% — almost
exactly the 29.2% it landed at before. This is the right knob because it moves the dagger's blow and
*nothing else*: not his basic attacks, not his buffs, not another class. **It is also the one number to
retune if the rogue is off** — one float, one class, measured in §C1.

⚠ The skill modifier is deliberately **not** capped by `StatCaps.PhysicalCritRate`: 50% is the cap on a
*character's* crit rate, and a fully-buffed rogue (36%) will land blows at ~72%. That is the IG shape —
a buffed dagger lands most of its blows — and the `BlowFailFraction` floor is what the rest becomes.

### ⚠ The one authoring gap this exposed

**The flat term has no gear source worth the name.** His model leans on a flat "elegia heavy set +127"
to carry the classes that cannot multiply their way anywhere. In our catalog flat crit rate exists only
as a **random weapon attribute**, and only on **sword / dual / bow** — the weapons with the highest base
already. A 2H-blunt warrior therefore sits at **4.4%** with nothing that can raise it, and §C2 prints
`flat = 0` on every row to keep that visible. Adding a flat crit-rate line to the heavy sets is the
natural fix and is his call, not mine.
