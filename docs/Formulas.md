# Formulas — the whole game's maths on one page

**Every number here is read off the code, not remembered.** Source in brackets after each block.
When you change a formula, change this file in the same commit — same rule as the skill CSVs.

Not here on purpose: the *reasoning* behind each choice (that is in the code comments and
`CHANGELOG.md`), and anything a catalog authors per-item (weapon speeds, drop rates, skill powers).

`L` = level · `atk/con/agi/wit/spt` = the five stats · caps live in `StatCaps.cs`.

---

## Damage

```
PhysicalDamage = 77 * (pAtk + power) / pDef                  min 1
MagicDamage    = 91 * power * sqrt(mAtk) / mDef              min 1
BasicAttack    = PhysicalDamage with power = 0
ManaDrain      = targetMaxMp * power / 1000                  power is PER MILLE
```

- **Defence is a DIVISOR, never a subtraction** — diminishing returns, and it can never zero a hit.
- `pDef` / `mDef` are multiplied by a weapon coefficient first (`WeaponDefenceCoef`: pierce/blunt/bow
  armour resistances), then floored at 1.
- ⚠ **Magic uses `sqrt(mAtk)` and physical uses `pAtk` flat.** That is why +M.Atk buffs feel weaker
  than they read: doubling M.Atk is ×1.41 damage, doubling P.Atk is ×2.
- Magic currently divides by **physical** defence in some paths — magic-resist is a `%` reduction
  (`BuffMagicResist`), not a separate defence stat.
- 🔑 **THE SHIELD IS NOT ARMOUR** (2026-09-09, `BL-185`): a shield contributes **no P.Def at all**.
  It pays only when it BLOCKS — `blocked = damage x (1 - BlockReduction)` — and `StatCaps.BlockChance`
  caps the roll at **80%**. `BlockReductionPct` (passive, buff and set) scales the shield's own
  reduction; nothing scales a shield defence any more, because there is none.
- 🔑 **THE SHOT — the War / Spell Runes multiply the FINISHED damage, ×2, per channel** (2026-09-09,
  `BL-185`). `FinalDamage = raw x (1+pve/pvp bonus) x conditional x skillMult x raidMult x takenMult
  x runeMult`, in `GameLoopService.FinalizeDamage`, and since `BL-212` (0.133.0) a `mobMult` factor rides beside
  `runeMult` in the same line. They are NOT stat buffs any more: `BuffPhysAtk
  1.00` fed an ADDITIVE formula and moved a 7635-power skill by only ×1.29 while reading "+100%", and
  the magic side reached ×1.414 only through a magnitude stored pre-`sqrt`. ×2 is IG's **blessed**
  shot exactly (its magic form is M.Atk ×4 under the `sqrt`, i.e. ×2 damage). ⚠ A rig that calls
  `StatCalculator` directly must apply `Entity.PhysDamageDealtMult`/`MagicDamageDealtMult` itself.

- 🔑 **A CREATURE'S FINISHED DAMAGE AGAINST A PLAYER IS MULTIPLIED BY A LEVEL CURVE** (`BL-212`,
  0.134.0). `MobRankScale.DamageOut(rank, targetLevel)`, applied once in `FinalizeDamage`:

  ```
  mult(L) = levelMod(L) x (1 + 0.40 x clamp((L-76)/14, 0, 1))
            L 20 -> x1.09    L 52 -> x1.41    L 76 -> x1.65    L 90 -> x2.51
  BOSS: exempt (x1.00)
  ```

  - **Below 76 it is exactly `levelMod`, and that is the point.** `BL-185` gave the physical channel
    the defender level term M.Def always had, so a player's P.Def is ×`(level+89)/100` and creature
    damage fell by precisely `1/levelMod` — 8% at 20, 44% at 90. This puts back what was taken and
    nothing more. (0.133.0 shipped a flat ×2, which over-corrected the bottom of the game by ~2.2×;
    owner, the same day: *"Let's make it lvl mod as u said."*)
  - **Above 76 it deliberately overshoots**, reaching ×2.51 at 90 — his *"76 to become harder"*,
    measured against his own numbers (a normal creature 130 → ~300, an elite 300 → 750, and ×2 again
    from the elite's own attack rung → 1500).
  - 🔑 **It reads the TARGET's level**, because the term it undoes lives in the defender's own P.Def.
  - ⚠ **Both sides are tested**: the attacker must not be a player (a guard, a tower and a boss are
    creatures too) and the target must be one (`BL-185`'s level term is `playerStats`-only, so paying
    it back on a mob-vs-mob hit would invent damage nothing removed). A player's reflected damage
    never enters this pipeline and is untouched.
  - 🔴 **A BOSS IS EXEMPT** — *"Bosses to compensate with their passive so they won't change after
    the base increase"*. An ELITE is not: it takes this AND its own ×3.0 attack rung.
  - ⚠ **It is DAMAGE, not P.Atk, and the distinction is real twice over.** The creature attack curve
    is fitted to IG off 2,831 measured monsters (`MobBaseStats.PAtk`) and is on the inspect panel; and
    because damage is a RATIO, doubling P.Atk is ×2 only while `power` is 0 — less on any skill.
  - 📐 `dotnet run --project tools/BalanceMatrix -- --mobdmg 90 epic --buffed`

`Game.Shared/StatCalculator.cs` — `PhysicalDamage`, `MagicDamage`, `ManaDrain`, `PhysicalK`, `MagicK`
`Game.Shared/MobRankScale.cs` — `MobDamageOut`

## Attack and defence inputs

```
AttackPower      = atk + L*2
PhysicalAttack   = weaponPAtk scaled by  atk/40
MagicAttack      = weaponMAtk scaled by  atk/40          (weapon MAtkBonus decides the split)
Accuracy         = agi + L
Evasion          = agi + L
PhysicalDefBase  = 80                                    (flat, empty-slot default)
MagicDefBase     = 41                                    (flat, empty-slot default)
P.Def            = (80 + gear + set + mastery%) * LevelMod
M.Def            = (41 + jewels + passives) * sptModifier * LevelMod
LevelMod         = (L + 89) / 100
```

🔑 **BOTH defences are `(base + gear) x LevelMod`, and NEITHER has a stat term on the physical side**
(2026-09-09, `BL-185`). P.Def had no multiplicative level term at all until then — it carried an
additive `68 + L*L/100` instead, while P.Atk has always carried `LevelMod`, so attack outran defence
as level rose. The two bases are IG's **empty-slot defaults** (chest 31 + legs 18 + head 12 + gloves 8
+ feet 7 + underwear 3 = 79-80; rear 9 + lear 9 + neck 13 + rfinger 5 + lfinger 5 = 41), which in IG
are REPLACED by whatever you equip rather than added to it — so on a geared character they are noise.
M.Def keeps its stat term (`sptModifier`, IG's MEN); **P.Def has none — IG's has no stat modifier.**

🔑 **ONE power stat (ATK) feeds both channels.** The *weapon* decides the split via `MAtkBonus` —
staff high, sword low. **WIT is not power**: it is cast speed + magic crit rate.

`StatCalculator.cs` — `AttackPower`, `PAtkStatMult`, `MAtkStatMult`, `PhysicalDefenceBase`, `LevelMod`

## Hit, evade, crit, block

```
AvoidChance = 0.05 + (defenderEvasion - attackerAccuracy) * 0.01      clamp [0.05, 0.95]
              then clamped again by the level gap, then by both sides' floors
PhysCritRate  = base(weapon) * (1 + (agi - 30)*0.01)                  cap 50%
PhysCritDmg   = 2.0 + bonus                                           cap x10
MagicCritRate = base * 1.63^((wit - 20)/10)                           cap 20%
MagicCritDmg  = 2.0 * mult * (1 - resist)                             cap x5
```

🔑 **THE MAGIC CRIT-DAMAGE MULTIPLIERS COMPOUND** (`Entity.MagicCritDamageMult`, folded from passives,
sets and buffs; debuffs SUM into `resist`). The whole shelf a level-90 nuker can wear, and it is
exactly the owner's own arithmetic of 2026-09-12: base ×2 × **1.30** Harmony of the Wizard L5
× **1.20** a Mark = **×3.12**, against a `StatCaps.MagicCritDamageCap` of ×5.
⚠ All four Marks share one buff key, so exactly one is ever on you — the 1.20 is not stackable.
📐 `dotnet run --project tools/BalanceMatrix -- --mcrit 90 epic` prints the chain stage by stage.

🔴 **A BUFF'S PAYLOAD IS ONLY APPLIED IF THE DEF'S `Effect` MASK DECLARES IT.** The mask comes from the
SkillDef, the magnitudes from the RUNG, and every channel in `RecomputeDerived` is gated on
`buff.Has(flag)` — so a magnitude authored on a rung whose flag the def omits is discarded in silence.
`SkillCatalog` throws at startup if the two disagree (`BL-214`, 0.133.0); `BalanceMatrix --maskaudit`
lists them. Four skills were in that state when the guard was written, the oldest dead since 0.106.0.

Both crit RATES are then reduced by whatever the DEFENDER carries — the two are separate stats:

```
rolledPhysCrit  = PhysCritRate  * (1 - target.CritRateResist)         resist clamp [0, 1]
rolledMagicCrit = MagicCritRate * (1 - target.MagicCritRateResist)    resist clamp [0, 1]
```

**Block** (shields only, physical only):
1. the shield lowers the attacker's crit **chance**;
2. if it still crits, the crit **ignores the shield**;
3. otherwise roll `BlockChance - skill.BlockAccuracy` → on a block, damage `* (1 - BlockReduction)`.

⚠ **DEX/AGI does NOT affect block** — flat shield values and passives only. **Magic is never blocked.**

**Blow landing rate** (dagger Stabs, `BlowOnCrit`) — **its own stat since `BL-188`, 0.121.0. It is
NOT the crit rate**, but since `BL-211` (0.133.0) the defender's **crit-rate resist cuts it anyway**:

```
BlowAgiMod = 1 + 0.03 * (clamp(agi, 20, 40) - 30)                     x0.70 … x1.30
BlowRate   = clamp(0.30 * Π(buffs, passives) * BlowAgiMod, 0.20, 0.80)
rolledBlow = BlowRate * (1 - target.BlowResist)                       BlowResist    clamp [0, 0.9]
                      * (1 - target.CritRateResist)                   CritRateResist clamp [0, 1]
```

⚠ **The cap is applied to the attacker's own rate BEFORE the defender's resists**, so the tank's Vital
Organ Protection (30%) takes a maxed 80% rogue to 56%.

🔑 **THE TWO DEFENDER TERMS MULTIPLY, THEY DO NOT SUM** — two independently-capped ladders that added
could pass 100% and invert the roll. `BlowResist` is the tank's dedicated answer (Vital Organ
Protection, 30%); `CritRateResist` is the channel every armour mastery, sigil and Mark already feeds,
so a LIGHT-armour class now brings its own. Measured at 90: a full light kit reads 35% and multiplies
an incoming blow by **0.65**. Owner, 2026-09-12: *"the light armor mastery and every crit chance
reduction passive/buff to lower the blow rate as well (the blow is crit dmg so heaving less chance to
be hit by crit means blows as well)"* — which knowingly reverses `BL-188`'s reason for leaving it out.
⚠ A shield's `ShieldCritDefense` still does NOT touch the roll: his sentence named passives and buffs.
📐 `dotnet run --project tools/BalanceMatrix -- --blowrate 90 epic`
A blow that lands is then computed with the CRIT-DAMAGE values and may roll a `[Double]` on top.

🔑 **A blow that MISSES ITS MARK IS AN ORDINARY BASIC ATTACK** (`BL-193`, 0.125.0) — full basic
damage off `EffectiveBasicAttack`, its own accuracy roll, its own crit and its own block. There is no
`BlowFailFraction` any more: the old flat floor (1% at the 3rd tier, 10% at the 2nd) is gone.

⚠ **The gate is rolled BEFORE the skill's own miss roll**, so each branch carries exactly ONE miss
gate — `SkillEvadeChance` for a landed blow, basic accuracy for one that fell through.

**A STACK BURST IS A BLOW** (`ConsumeStackKey`; today only Venom Burst — `BL-207`, 0.131.0):

```
landed  = resolvedDamage * stacksSpent, then the crit-damage values, then [Double]
          ^ the DAMAGE is multiplied, NOT the power
failed  = an ordinary basic swing; the pool is emptied and ONE cast's worth banked back
```

🔑 **`damage * stacks` is not `power * stacks`.** Power sits beside `atk·lvlMod` inside the ratio, so
multiplying afterwards multiplies the ATK term too: at 90 a full pool reads **2.7×** a Killing Stab
where "one stab of 15k power" would read 1.75×. The crit-flat factor is measured against
`power * stacks` so it is not inflated by the same asymmetry.

`Entity.BlowRate` / `BlowResist` · `StatCalculator.BlowAgiMod` / `BlowRate` ·
`GameLoopService.BlowLands` / `ResolveBlow` / `ResolveBasicSwing`

**Bow resistance** — applied FIRST, before crit and block, and only when the attacker's weapon is a
bow (basic attacks and physical skills alike):

```
baseDamage = max(1, baseDamage * (1 - target.BowResist))    BowResist clamp [0, 0.9]
```

Sums across passives, buffs (`SkillEffect.BuffBowResist`) and mob mastery profiles, then clamps.
Its only source in the player kit is **Shield Mastery** (16% at rung 2 → 40% at rungs 6-7), so it is
worn by tanks and by the Human Warchanter in heavy armour, and by nobody else.

`Entity.BowResist` · `GameLoopService.ResolvePhysicalCritAndBlock` / `ResolvePhysicalDouble`

`StatCalculator.cs` — `ResolveAvoidChance`, `PhysicalCritBase`, `MagicCritBase`, `PhysicalCritMult`,
`MagicCritMult` · block in `GameLoopService.ApplyDamage`

## Basic-attack cleave (the warrior's blunt masteries, 0.130.0)

```
extraBodies = CleaveTargets - 1          <- CleaveTargets COUNTS the real target
victims     = up to extraBodies hostiles within CleaveRadius OF THE TARGET
```

Each extra body takes a **whole basic swing**, not a share of one: its own miss roll, crit, block and
every on-hit rider, plus its own Retaliate and Kill so a cleaved kill still credits the drop. There is
no damage falloff and no per-victim scaling — his line is *"Allow basic attack to hit around in 150
range (max N targets)"* and nothing more.

Radius is measured from the **target**, not the attacker: he is hitting around the body he swung at.
Both sources are weapon masteries, so the gate is the equipped weapon and nothing else — a
**two-handed blunt**, at 150 range, for 2→4 targets (`warrior_weapon_mastery`, from 20) or 5→10
(`warrior_blunt_mastery`, the Warlord, from 40). Holding both, the **LARGER** wins; they never sum.

`PassiveEffect.CleaveTargets`/`CleaveRadius` · `Entity.CleaveTargets` · `GameLoopService.ResolveCleave`

## Last stand — the two passives that read your own HP bar

One HP band, read live, strongest-first and **exclusive** (at 20% HP you are in the bottom band and
nothing else). Never applied as a buff: HP moves every tick and nothing recomputes derived stats when
it does, so a buff would need a watcher on the damage path, the heal path, the regen tick *and* the
potion path, and whichever was forgotten is where the bonus silently sticks or vanishes.

```
band         = HP% < 25 -> 3 | < 50 -> 2 | < 75 -> 1 | else 0
FinalDefense = P.Def x (1 + f[skillLevel][band])   f = 5/10/15 · 7/14/21 · 10/20/30  %
               M.Def x (1 + m[skillLevel][band])   m = 0/2.5/5 · 0/3.5/7 · 0/5/10    %
FinalStand   = P.Atk x (1 + a[skillLevel][band])   a = 5/10/20 · 7/15/25 · 10/20/30  %
```

Final Defense is the tank's (`tank_final_defense`, 40/52/60); Final Stand is the warrior's
(`warrior_final_stand`, 40/52/60, both disciplines) and rides both physical attack getters, so basic
swings gain it too. Each lands OUTSIDE the buff stack, as a multiplier on the finished stat.

`Entity.LastStandBand` / `FinalDefenceBonus` / `FinalStandBonus`

## The three skill masteries (`BL-190`)

Three passives, one piece of math. Each grants the BASE RATE of its own roll; ATK is only a band
around it, and **nothing else grants any of them** — no passive, no roll.

```
MasteryAtkMod = 1 + 0.03 * (clamp(atk, 30, 50) - 40)                  x0.70 … x1.30
rate          = 0                                                     when base <= 0
              = clamp(base * MasteryAtkMod, 0, 0.25)                  cap shared by all three
```

| Mastery | What it does | Rolled |
|---|---|---|
| `DoubleDamageRate` | a physical skill flagged `[Double]` deals ×2 | per hit, after the blow roll on a blow |
| `DoubleDurationRate` | a buff or debuff you cast lasts twice as long | once per cast, player casts only |
| `CooldownResetRate` | the skill just cast comes straight off reuse | once per cast, never on a `FixedCooldown` skill |

⚠ `atk` is **EffectiveAtk** — the +5 swap at 40, the armour sets and every `+ATK` passive count.

**Who grants one** (`BL-191`, authored 2026-09-10 — nobody else has any of the three):

| Passive | Who | Learn | Base |
|---|---|---|---|
| **Overpower** | Warrior, then Ravager + Warlord | 20 / 40 / 76 | 3% / 7% / 10% damage |
| **Lasting Enchantment** | Lightbringer + Warchanter | 76 | 10% duration |
| `reuse_reset_momentum` — "Arcane Momentum" (Magus) / "Stab Momentum" (Nullblade, Phantom, Venomweaver) | Magus + the three MELEE rogues | 76 | 5% reuse |
| `double_mastery` (toggle) — "Overpower Mastery" (warrior) / "Momentum Mastery" (melee rogue) | Ravager + Warlord + the three melee rogues | 81 | **×2 on every mastery base its holder has**; 50 HP/s, +25% MP on physical skills |

⚠ **The toggle scales all three channels, not just damage** — that is what lets one def serve a
warrior (whose base is damage) and a melee rogue (whose base is reuse). `SkillDef.MasteryMult` is
applied to all three accumulators in `Entity.RecomputeDerived`, *before* the ATK band and the cap.

Measured at level 90, mythic gear (`BalanceMatrix` §C1): Ravager 9.4% damage (18.8% with the toggle),
Warchanter 9.7% / Lightbringer 10.9% duration, Magus 5.5% reuse, Nullblade 4.6% reuse (9.1% with the
toggle). The **tank and the BOW rogue read 0/0/0 and always will** — both ruled out by name on
2026-09-10.

🟡 The whole layer is provisional by his own words: *"ill try with those changes and after playtest
ill deside if i add or remove"*.

Retired with this: `StatCalculator.PhysicalDoubleChance`, `min(25, 2.5 + 0.75·(ATK−30))` off the RAW
stat — a per-race constant (Elf 7.0% / Human 10.0% / Demon 10.75%) that nothing could raise, and
which drove buff/debuff duration doubling off the *damage* number.

`Entity.DoubleDamageRate` / `DoubleDurationRate` / `CooldownResetRate` ·
`StatCalculator.MasteryAtkMod` / `SkillMasteryRate` · `StatCaps.SkillMasteryRateMax` ·
`PassiveEffect.DoubleDamageRate` / `DoubleDurationRate` / `CooldownResetRate`

## Landing a spell (fizzle)

```
failPoints = round( 1.0 * 1.3^(targetLevel - RUNG'sLearnLevel) * defenderMod * weaponMod )
           + defenderMagicEvasion            flat percentage POINTS
           - casterMagicAccuracy             flat percentage POINTS
failChance = clamp(failPoints / 100, 0, 0.95)
```

- **Parity (rung level == target level) is 1%.** +6 levels → 5%, +16 → 67%, +18 → the 95% cap.
- 🔑 **The level read is the RUNG's learn level, not the caster's** — a level-80 mage casting a rung
  authored at 40 fizzles like a 40. That is what makes a skill ladder matter.
- **A fizzle is not a miss**: it still lands `damage / 3`.
- `SureHit` skips this entirely (the three level-74 nuker bursts).
- ⚠ M.Acc / M.Evasion are FLAT POINTS, not levels — 4 points is worth ~6 levels near parity and
  ~0.2 levels at the top of the curve, because the curve is exponential and the points are not.

`StatCalculator.MagicFailChance` · `StatCaps.MagicLevelBase/MagicFailParityPoints/MagicFailMax`

## Landing a debuff (contested CC)

```
def    = defenderStat * CcLevelBase^(defenderLevel - attackerLevel)
chance = 0.5 + 0.5 * (attackerAtk - def) / (attackerAtk + def)
         clamp [0.10, 0.90], then * skill.DebuffLandMod, then re-cap at 0.90
```

- `defenderStat` is **CON** for a physical debuff, **SPT** for a magical one (`DebuffSchool`).
- `CcLevelBase` is derived, not authored: it is whatever makes the floor land exactly 18 levels out.
- **`DebuffLandMod` is the per-skill success multiplier** (`BL-90`): ×1.5 = 75% at parity, ×1 = 50%,
  ×0.7 = 35%, ×0.5 = 25%, ×0.3 = 15%. A ×0.5 skill may go under the floor; nothing may pass 0.90.
- ⚠ Attacker level here is also the **RUNG's** learn level.

- **BURN lands unconditionally** — a DoT whose family saves against nothing (`DotTiers.Save` = None)
  skips the contest entirely.
- 🔑 **A BURST THAT DETONATED DOES NOT ROLL THIS CONTEST AT ALL** (`BL-207`, 0.131.0). Spending a stack
  pool sets `spentStacks`, which already suppresses re-applying the DoT — so rolling first only ever
  produced a cosmetic `Fail` over a cast that had just dealt ten stacks of damage. A burst that found
  **no** pool still rolls, because that is where the rider is the point of the skill.
- **A WHISP contests on a FLAT attack of its own** (`GameConstants.WhispCcAtk` = 40, a plain melee
  creature's) at the **MASTER'S level** — never the master's ATK, never his gear. `BL-109`.
- **A TAUNT has no roll at all**, and its two halves go different distances (`BL-123`): the target
  LOCK lands on mobs AND players; the aggro ladder is paid only into a monster's threat table.

`StatCalculator.DebuffLandChance` · `StatCaps.CcLandMin/CcLandMax/CcLevelFloorGap`

## How LONG a landed debuff runs (`BL-156`, 0.110.0)

```
factor   = clamp( 1 - 0.3 * (defenderStat - 30) / 20 ,  0.70 , 1.00 )
duration = round( authoredTicks * factor )        min 1 tick
```

- Same `defenderStat` as the landing contest above — **CON** physical, **SPT** magical — and the
  **raw stat**, never the land chance (that would fold in `CcResist`, the school blessings and
  `DebuffLandMod`, all of which already paid on the roll).
- Below 30 → ×1.00. A low stat never *lengthens* a debuff.
- **Players:** demon fighter CON 47 → ×0.75 · human 43 → ×0.81 · elf 39 → ×0.87 · every mage ×1.00.
  SPT: demon mage 41 → ×0.84 · human 37 → ×0.90 · elf 36 → ×0.91 · every fighter ×1.00.
  Nothing in the game buffs CON or SPT; armor sets move CON by ±3.
- **Mobs too.** Melee CON 45 → ×0.78 · Archer 43 → ×0.81 · Mage CON 40 → ×0.85 · tank `MobMod` 50 →
  ×0.70 · **mage SPT 58 → ×0.70** (past the floor). Player CC runs 12-30% short of its authored time.
- Applied in **`ApplyBuff`**, so every road reaches it: the contest, the fizzle path, a reflected
  debuff, a whisp, a boss. Composes with **[Double]** by multiplication (2 × 0.7).

`StatCalculator.DebuffDurationFactor` · `StatCaps.DebuffDurationStatBase/StatFull/Floor`

## Pull (`BL-154`, 0.110.0)

```
travel   = distance(caster, target) - 80          (MeleeRange; <=0 -> stun at once)
ticks    = round(PullSeconds / 0.1)
perTick  = max(travel / ticks, 300 * 0.1)         (PullMinSpeed floor, so short pulls arrive early)
```

Timed, not paced: the drag takes `PullSeconds` from **any** distance, so range buys reach and never
lockdown. Direction is re-aimed each tick at the puller. One contest (ATK vs CON) covers the drag and
the stun; the stun is applied **on arrival**, so drag and stun run in sequence. Being dragged is an
action lock, so it cancels a cast the same way charm and fear do.

`GameLoopService.StartPull/TickPull/FinishPull` · `SkillDef.Pulls/PullSeconds` · `GameConstants.PullMinSpeed`

## Silence (`BL-155`, 0.110.0)

Two independent debuffs. A skill refuses to fire when `SkillMath.IsPhysical(def)` matches the half you
are wearing — physical silence stops physical skills, magical silence stops magical ones, both at once
is a full silence. **A basic attack is never silenced** (it is not a skill). Bosses are immune to
both; the boss's own `boss_full_silence` sets both fields.

`Entity.IsSilencedPhysical/IsSilencedMagical` · `SkillDef.SilencePhysical/SilenceMagical`

## Reflecting a debuff back at its caster (`BL-08`; per-school since `tank 4th.csv`, 0.112.0)

```
chance = DebuffSchool.Physical -> DebuffReflectPhys
         DebuffSchool.Magical  -> DebuffReflectMagic
         DebuffSchool.None     -> max(the two)
```

- Rolled **before** the land/fizzle contest. On a hit the effect is applied to the CASTER and the
  intended target takes nothing; the bounced copy gets no resist roll and **cannot bounce again**.
- Each channel is the **MAX** across passives (a guarantee, never a sum), clamped to 0.95. A blanket
  `PassiveEffect.DebuffReflectChance` means *either school* and feeds both accumulators.
- Self-casts never bounce. A reflected DoT carries no `SourceId`, so the reflector is never PvP-flagged
  by its ticks — do not "fix" that by stamping him as the source.
- Today's only source is the tank's **Backlash**, 77/80/83: the matching school 10/20/30%, the
  opposite one 5/10/15%. Physical Backlash (Human + Demon) reads CON as the matching school, Magical
  Backlash (Elf) reads SPT.

`GameLoopService.TryReflectDebuff` · `PassiveEffect.DebuffReflectPhysChance/DebuffReflectMagicChance`


## Damage over time (bleed / poison / venom / burn)

**Both halves come from a (TYPE, TIER) table** — `Game.Shared/Skills/DotTiers.cs`, the mirror of
`docs/data/dot_table.csv`. Neither the damage nor the side effect belongs to the skill that delivered
it (owner, 2026-09-10: *"remove the dot side effect from the skills"*), so every bleed in the game
slows by the same amount and every tier-11 venom ticks for the same number.

```
tick/s = DotTiers.DamagePerSecond(kind, tier) * stacks        FLAT — no defence, no ATK, no level term
```

| type | tier 1 → 11 (dmg/s **per stack**) | max stacks | side effect | saves on |
|---|---|---|---|---|
| **Venom** | 5,5,7,7,9,9,10,10,15,15,20 | **10** | −10% P.Atk **and** M.Atk | CON |
| **Poison** | 20,30,40,50,60,70,90,110,130,150,200 | 1 | −15% attack **and** cast speed | SPT |
| **Bleed** | 20,20,40,40,60,60,80,80,100,100,150 | 1 | −20% move speed (every rank) | CON |
| **Burn** | *(10,11,12 only)* 100,125,150 | 1 | −70/72/75% **HP and MP received** | — nothing |

- **Flat, undivided, once a second.** *"its true all effects do flat dmg."* The physical/magical label
  decides **only which stat saves** — nothing else follows from it.
- **Only the landing is a stat contest** (`StatCalculator.DebuffLandChance`, attacker AGI for
  bleed/venom else ATK, defender CON or SPT). Once it lands, the tier is the whole number.
- **Only venom stacks.** `DotTiers.MaxStacks` caps the family, so a skill cannot be authored into a
  stacking bleed. The side effect does **not** scale with stacks — `BuffInstance.Percent` sums
  magnitudes and never multiplies by `Stacks`, so venom's −10% is −10% at one stack or ten.
- 🔑 **THE STACKS ARE BANKED BY THE STRIKE, NOT BY THE CONTEST** (`BL-199`, 0.128.0). A stacking
  skill's pool is a hidden counter on its `StackKey`; a resolution of the damage arm that **LANDS THE
  SKILL** adds `StacksPerCast` to it. The DoT is a separate thing that lands on its own AGI-vs-CON
  contest; losing it costs the damage, never the pool. Owner, 2026-09-11: *"Stacks should be
  independent of dot ... each landed venom blow adds stacks that do not do nothing just stacks"*.
  🔴 Until then BOTH rolls had to come up for one stack — the blow gate *and* the contest — which is
  the whole of *"venomweaver almost cannot stack venom"*.
- 🔑 **ON A BLOW SKILL THE BLOW GATE IS THE STACK GATE** (`BL-197`, same day): a failed blow resolves
  as an ordinary basic attack (`BL-193`) and banks **nothing**. That is deliberate and it is the
  design — *"if i chose to use perfect_strike i land more ophen i stack faster but for less dmg ... if
  i use brutal_strike i land less ofthen i stack slower but do more dmg"*. The @80 choice between
  those two buffs only means something if the rate it moves is also the stacking rate. **One roll
  gates a stack, and the player has a buff for it.**
- 🔑 **`stacks` in the tick formula is the COUNTER's**, read through `GameLoopService.DotStacksOf`. The
  damage buff itself is pinned at one (`ApplyDotStack`), so `b.Stacks` is the wrong number and venom
  ticked for a single stack at any count until 0.128.0 — while the bar showed "x7", because the fold
  existed there and nowhere else.
- **A BURST takes the DoT with the pool.** `ConsumeStackKey` multiplies the burst's damage by the
  stacks, removes the counter **and** removes every DoT whose skill shares that `StackKey` (owner:
  *"when venom burst is used it takes with it the stacks + the dot debuff"*), then tells the caster
  what it spent. A burst that found no pool falls through and lays the first stack instead.
- **Burn is saved against by nothing and always lands** (*"for burn nothing protects .. always land"*).
  The contest is skipped outright rather than multiplied up, so `CcResist` and the per-school blessing
  cannot claw it back.
- **Cures reach tier 11; tier 12 is beyond every cleanse.** Antidote ladders to 10; **Holy Blessing**
  (@78) is the only thing that reaches 11. `DotTiers.Curable` refuses 12 to every cure in the game, so
  it cannot be lifted by mis-authoring a cleanse.
- **No DoT lowers defence** (*"for now no dot will decrease def"*).
- **Burn has no `SkillEffect` bit and never can** — `1L << 62` is the last free flag. It carries
  `SkillEffect.Poison` for membership (`AnyDot`, the bar, what a cure may strip) and declares
  `DotKind.Burn` for its numbers.

🔴 **Do not restore `DotPowerAt`'s fallback to `Power`.** Until 0.125.1 a DoT with no authored
`DotPower` ticked for the skill's *direct-hit* power — and only one skill in the catalogue authored the
field, so Bleeding Arrow (power 15,000 over 30s) dealt **450,000**. A skill's Power is its direct hit;
a DoT rider is a second number.

`DotTiers` · `GameLoopService.TickDots` / `ApplyBuff` / `ApplyDotStack` / `AddDotStacks` / `DotStacksOf`

## Crit rate and crit damage, taken OFF the attacker (`tank 3rd.csv`, 0.105.0)

```
critChance = clamp((base*mult + flat) * (1 - Σ CritRatePenalty),  0, cap)     Shield Smash - Rate
critExtra  = (flatFactor*mult - 1) * (1 - target.CritDmgResist)
                                   * (1 - attacker.CritDamagePenalty)         Shield Smash - Power
```

- Both penalties sit on the CREATURE as a debuff, not on the tank as a resistance — so the whole
  party stops being critted, which is the only reason the skills are worth a slot.
- Each is clamped to 0.9: a smash can never make something literally incapable of critting.
- The defender's own `CritDmgResist` and the attacker's penalty MULTIPLY; both bites land.

`Entity.CritRatePenalty/CritDamagePenalty/MagicCritRatePenalty` · `ResolvePhysicalCritAndBlock`

## Interrupting a cast (IG's own formula)

```
chance = damageTaken / casterMaxHp * random(1.00..1.20)
       * spiritMod(spt)
       * (1 - resolveResist)                clamp resist at 0.80
       * skill.InterruptMult
       + flatBonus/100
```

🔑 **The yardstick is the CASTER'S HP POOL, not the spell.** Nothing about the spell — its damage,
cast time or reuse — is an input. Cast time still matters, but as an *emission*: a longer cast eats
more hits. `spiritMod` is flattened from IG's: 20 SPT = ×1.00, 50 SPT = ×0.67.

`StatCalculator.InterruptChance`, `SpiritInterruptMod`

## Pools and regen

```
MaxHp        = hpBase(race, baseClass, discipline, L) * conHpModifier(effectiveCon)
MaxMp        = (classMod * (L*L + 3L)/2 + level1Base) * sptModifier(effectiveSpt)

hpBase(L)    = level1Base(race, baseClass)
                 + SUM over tiers of  g(tier) * ( Q(hi) - Q(lo) )      Q(L) = (L*L + 3L)/2
               tier edges = the class-change levels:  1-19 | 20-39 | 40-75 | 76-85
               i.e. each level grants  g * (L+1)  HP, and g STEPS UP at every class change.

g per track    1-19  20-39  40-75  76-85       level1Base   fighter / mage
  tank         0.95   1.45   1.51   1.51         human          44 / 41
  warrior      0.86   1.32   1.37   1.37         elf            40 / 37
  rogue|archer 0.79   1.21   1.26   1.26         demon            50 / 49
  buffer       0.90   1.02   1.23   1.23
  healer|nuker 0.74   0.84   1.01   1.01

conHpModifier   CON 20 -> 1.00 , 30 -> 1.25 , 40 -> 1.80 , 50 -> 2.58 , 60 -> 3.72

HpRegen/s    =  (3 + L*0.1) * 1.03^(effectiveCon - 40)
                  * stance * safeZone * hpRegenMult * (1+buff%)     + flats
MpRegen/s    =  (2 + L*0.08) * sptRegenModifier(effectiveSpt)
                  * stance * calmSpirit * mpRegenMult * (1+buff%)   + flats

sptRegenModifier(spt) = clamp(1 + (spt - 40)*0.02, 0.70, 1.30)
stance                = running 0.70 | walking 0.85 | STANDING STILL 1.00 | sitting 1.50
flats                 = the hpReg/mpReg mastery rungs + gear flats + flat regen buffs
                        + the SITTING-ONLY flats, while sitting (see below)
```

**Sitting-only flats** (2026-09-11, the warrior's HP Regeneration passive — *"Increase Hp regen +1.4;
When sitting Hp regen +1, Mp regen +2.0"*). `PassiveEffect.HpRegenSitting` / `MpRegenSitting`, flat
per second, paid only while `MoveState == Sitting` and added with the other flats — i.e. **OUTSIDE**
the stance multiplier. That placement is the whole of it: sitting already pays ×1.5 on the formula
half, so folding an authored +2.0 inside would silently make it +3.0.

**IG's own HP regen, for reference** (owner supplied it 2026-08-26; ours is compared against it by
`BalanceMatrix --hpregen`):

```
IG:  HpRegen = ( base * ConMod * LvlMod + flat ) * buffs      <- IG's flat is INSIDE, ours is last

     base (no buffs/passives), per RACE+CLASS:
         fighter Human/Demon  2.5-3.0      elven fighter  2.0-2.5
         mage    Human/Elf  1.5-2.0      demon mystic     2.0-2.2
     ConMod   CON 30 -> 1.00 , CON 43 -> 1.32        == 1.32^((con-30)/13) , i.e. 1.0216/point
     LvlMod   Level/100 + 0.89                       == our damage lvlMod, (level+89)/100
```

🔑 **Our HP regen factors EXACTLY into IG's shape**, which is what makes the two comparable:

```
(3 + L*0.1) * 1.03^(con-40)   ==   3.00 * (1 + L/30) * 1.03^(con-40)
                                   base    LvlMod      ConMod
```

Three numbers differ and nothing else: our base is **3.00 for every race and class**, our ConMod
centres on **CON 40** not 30, our step is **1.03** not 1.0216. At level 1 every fighter lands inside
IG's band and mages 6-13% above it.

- 🔑 **THE HP TRACK IS KEYED BY DISCIPLINE, AND IT IS A PURE FUNCTION** (0.91.0, owner 2026-08-27).
  Keyed by discipline where the character has one, by archetype before 40 — which is the only way
  Warchanter (buffer) and Lightbringer (healer) can differ, since they share `Archetype.Healer`.
  Nothing is accumulated: taking a discipline at 40 recomputes the **whole** curve on the new track,
  so a Warchanter visibly gains **+20%** HP the moment he class-changes. That is deliberate — it is
  how IG's per-class table jump is reproduced without a discontinuity in L.
- 🔑 **The step in `g` IS the class-growth bonus.** Fitted to IG's own per-class tables: a knight's
  rate jumps **+53%** at 2nd class, a mystic's only **+13%**. Error vs those tables is 0% at 1/40/80,
  −3% at 10, **+7% at 20** (worst), +5% at 50-60. The owner's three anchors — tank@40 CON43 = 2380,
  buffer@40 CON31 = 1180, knight@80 CON43 = 9840 — read **2414 / 1184 / 9969**. Ordering he set and
  the curve holds: nuker = healer < buffer < rogue < warrior < tank.
- ⚠ **`conHpModifier` is normalised at CON 20, not 30**, and it is far steeper than the table it
  replaced (×2.58 at CON 50 against the old ×1.83). Both halves matter together: the base table above
  is quoted *against this curve*, so changing one without the other rescales every pool. Verified by
  `BalanceMatrix --hpcurve`, which prints the tracks, the anchors and the class-change step.
- ⚠ **Flats stay OUTSIDE** for Max HP, per the global rule — so an authored `+HP` passive is worth
  its face value and nothing more. IG puts them inside; the owner's ruling (2026-08-27) is that we
  keep our order and **he doubles the authored number** when writing the CSV rung.
- `sptModifier` is **gentle** (1.16 @20 → 1.65 @50) — MP was not touched by this pass.
- 🔑 **MP regen has its OWN stat curve.** `sptModifier` still drives Max MP and M.Def, but regen left
  it on 2026-08-26 (`BL-92`) for the wider linear `sptRegenModifier` — so Spirit buys visible sustain
  (every fighter sits at the 0.70 floor; the demon mage reaches 1.10).
- 🔑 **BOTH BARS PUT THEIR FLATS OUTSIDE**, the global "flats after percentages" rule (playtest 28,
  `Entity.ModifiedStat`). MP moved 2026-08-26, HP the same day once measured (`BL-92`).
- 🔑 **EVERY `hpReg`/`mpReg` MASTERY IS A FLAT PER-SECOND GRANT**, read off the CSV rung **whole**:
  `hpReg +2.7` = +2.7 HP/s, never ×2.7. Both were multipliers until `BL-92` and both were wrong the
  same way (MP ×4.84 by 74; HP ×2.7, which put a level-74 nuker on 27.5 HP/s against a tank's 16.4).
  **Never re-enter one as a percent.** ⚠ **THE TANK'S HEAVY ARMOR MASTERY JOINED THEM 2026-09-04** on
  his ruling (*"the mp regen of tank is also additive, not multiplicative"*): its `x3.1 … x5.1` was the
  same leftover `BL-92` fixed for the mage and never fixed here, so a level-74 tank multiplied his
  whole chain by 5.1 instead of adding 5.1/s. One ladder across three tiers now — **nothing below level 36, then
  3.1 / 3.5 / 3.9 / 4.3 / 4.7 / 5.1 MP per second** (the four rungs below 36 carried an invented ×1.1
  that he removed the same day). What is left of the carve-out is the generic `mpReg x1.1/x1.2` the
  warrior/rogue/mage armour masteries grant on every weight, which is still a percent.
- ⚠ **HP regen sits at ~1.6-2.0× IG — known and accepted**, owner 2026-08-26: *"we will have x2 more
  than IG … Playtest will decide if it stays"*. The whole gap is the **level term** (ours ×3.71 across
  1-85, IG's ×1.93). Swapping it was measured and **not** taken — don't close it without a new ruling.
- 🔴 **Open:** fighter 3rd/4th kits unauthored — when they land, a fighter's `hpReg` flat must exceed a
  mage's (today nuker **+2.7** > warrior **+1.6** > rogue **+1.2** > tank **0**; archer/dual have no
  row). The **demon buffer** should carry more; how much is undecided.
- 🔑 **EVERY primary stat is read EFFECTIVE** (base + armour-set + stat-swap deltas), owner
  2026-08-26: *"Need effective con to count on hp max/regen and whatever con have mod on"*. CON and
  ATK were the last two read BASE — by HP regen and by the character sheet / target panel — so a set's
  `Con: -2, Str: +3` moved your pool, your regen and your damage with **nothing on screen**. Max HP,
  HP regen, the debuff save and both panels now all read `Effective*`. Mob paths keep raw CON.
- 🔑 **A mage gets that ×1.2 exactly ONCE**, from the armour he wears: **robe** from the born
  Spellcaster Mastery, **light** (cleric) / **heavy** (buffer) from their own Armor Mastery. It used to
  be granted twice on robe (×1.44). Never re-add `mpReg` to a `Robe:` slot.
- **Standing still** is derived (no move target), not a `MoveState` — that enum is persisted.
- **×2** inside a city safe zone with `RegenBoost`. No combat/casting suppression, by ruling.
- **Calm Spirit** (nuker, 6 rungs) multiplies the *stance*: ×0.30→×0.70 running, ×1.03→×1.20 walking,
  ×1.00→×1.02 standing. At its top rung walking and standing regen are exactly equal.

`StatCalculator.MaxHp/MaxMp/HpRegenPerSecond/MpRegenPerSecond/SptRegenModifier` ·
`MovementTuning.RegenMultiplier` · `GameLoopService.Regenerate` · measured by `BalanceMatrix --mpregen`
and `BalanceMatrix --hpregen` (the latter also prints IG's own numbers beside ours)

## Speed

```
timeMultiplier = 333 / speedStat                 lower = faster
AttackSpeedStat = weaponBase * 1.0105^(agi - 30)          cap 1500
CastSpeedStat   = classBase  * 1.63^((wit - 20)/10)       cap 1999
   classBase: NON-MAGE 300 · human/elf mage 333 · demon mage 300      (`BL-133`, was 150)
MoveSpeed: base per race+class, NO dex term, buffed cap 250 (per-entity, raisable)
   fighter: elf 143 · human 115 · demon 112      walk = run * 0.5
   mage:    elf 114 · demon 113 · human 109      mob: walk wandering, run engaged, +100 leashed home
   ROOT/STUN = 0 · FEAR = run speed, driven · CHARM = walk speed, driven   (`BL-110`)
   WHISP: max(masterSpeed * 1.6, 200), x3 outside the 100-200 leash band   (`BL-109`)
```

- 🔑 **WHICH STAT PACES A CAST IS THE SKILL'S PHYSICAL/MAGICAL AXIS, NOT its `Category`** (`BL-132`).
  `SkillMath.IsPhysical` = `Category.Physical` **or** `DebuffSchool.Physical` **or** the
  `PhysicalCast` flag (the physical BUFFS). Physical → ATTACK speed, everything else → cast speed.
  ⚠ Renamed from `PacedByAttackSpeed` in 0.110.0 when `BL-155`'s SILENCE became its second caller —
  the same test now answers "which speed paces this?" and "which silence stops this?". The old name
  survives as a one-line alias at the speed call sites, where that is what the question means.
  It used to ask `Category == Physical` alone, which is a ROLE tag — so a physical stun (`Debuff`)
  and a physical self-buff (`Buff`) were both paced by the mage's stat. The authority is his CSVs'
  `TYPE` column, and `SkillCsvSeed --check` now compares that word.
- A MOB and a `FixedCast` skill use the authored time flat (multiplier 1).

- **Fear and charm swap the BASE and keep everything else** — buffs, slows and the cap all still
  apply, so a Swift-buffed victim panics faster. Read before the mob/player branches, so a charmed
  mob walks even while Engaged and a charmed player walks even while running.

`StatCalculator.SpeedBaseline`, `AttackAgiModifier`, `CastWitModifier`, `ClassBaseCastSpeed` ·
`Entity.EffectiveCastSpeedMultiplier` / `EffectiveAttackSpeedMultiplier` / `EffectiveSpeed` ·
`StatCaps.MoveSpeed/AttackSpeed/CastSpeed`, `SpeedTable.BaseRunSpeed` · `GameConstants.MobLeashSprintBonus`

## Skill MP cost

```
quoted = the ONE authored MP number
charged = quoted * MpCostFactor(buffs and debuffs)        clamp x0.2 .. x3
   gate: you must hold ALL of `charged` before the cast starts
   split: 20% on cast start, 80% on landing
```

⚠ **Every MP question goes through `GameLoopService.EffectiveMpCost`** — the gate, both charges and
autohunt's budget. Reading the authored number anywhere else is the bug.

`SkillMath.InitialMpFraction` · `GameLoopService.EffectiveMpCost`

## Skill reuse

```
reuse  = authored * retain                               min 1 tick; skipped when FixedCooldown
retain = CooldownRetain                                  every skill (Spell Mastery, buffs)
       * (IsPhysical(def) ? CooldownRetainPhysical : CooldownRetainMagic)

each source multiplies its channel's retain by its own (1 - r).  NO CLAMP.
```

🔑 **THE CHANNEL IS `SkillMath.IsPhysical`, THE SAME TEST THE SPEED MODEL AND SILENCE USE** (0.128.0).
It asked `Category == Physical` until then — a ROLE tag — so the rogue's Sprint, the archer's three
traps and every physical stance were filed under MAGIC reuse (owner: *"rogues sprint is physical not
magical"*), and Bow Blessing's *"−20% physical reuse"* did not in fact reach *"every skill an archer
owns"*. Same mistake `BL-132` fixed for cast pacing; this was the last call site still asking Category.

⚠ "Magic" is spells, buffs, debuffs AND heals — everything the physical test rejects. The per-channel
halves exist because one buff can carry two different numbers (Harmony of the Soul: −20% magic,
−30% physical).

🔴🔑 **THEY COMPOUND, AND THERE IS NO CLAMP** (`BL-217`, 0.136.0). What is stored is what SURVIVES
— `Entity.CooldownRetain` / `CooldownRetainPhysical` / `CooldownRetainMagic`, each multiplied by a
source's own `(1 − r)`. The caster stack at 90 is Spell Mastery 20% (blanket) × Harmony of the Soul 20%
× Harmony of the Wizard 35% (both magic) = **×0.416**, the owner's own number. Summed it read ×0.25.

- ⚠ **WRITE TO `CooldownRetain*`, NEVER TO `CooldownReduction*`** — the latter three are computed
  getters (`1 − retain`) so the stat panel and the DTO keep an ordinary fraction, and a stray `+=` is
  a compile error instead of a silent return to summing.
- ⚠ **The 0.8 clamps are GONE and that is deliberate.** They were a hard floor of 0.2× the authored
  reuse, and the caster stack had already reached 75% — the next number raised would have moved
  nothing. A product of `(1 − r)` approaches zero and never arrives; `ExecuteSkill` floors at 1 tick.
- ⚠ A negative `r` (a "slow reuse" debuff) multiplies retain ABOVE 1 and lengthens the reuse, which
  is correct. Nothing authors one today.

## Cast length (`BL-196`, 0.128.0)

```
castTicks = authoredCast
          * (IsPhysical(def) ? 333/attackSpeedStat : 333/castSpeedStat)   ← the 333 model
          * CastTimeMultiplier                                            ← the cast-TIME channel
   min 2 ticks · a MOB and a FixedCast skill skip BOTH factors (multiplier 1)

CastTimeMultiplier = clamp(1 - SUM(buff.CastTimePct), 0.2, 3)
```

🔑 **CAST SPEED AND CAST TIME ARE TWO DIFFERENT CHANNELS.** Cast *speed* is a STAT, and it only paces
skills the physical/magical axis sends to it — so a cast-speed grant is worth nothing to a fighter,
whose skills are paced by attack speed. Cast *time* multiplies the answer, whichever stat produced it.
His formula, verbatim: *"(baseCastOrAttackSpeedValue x castOrAttackSpeedBuffs x castOrAttackSpeedDebuffs
/ 333 or whatever) x castTimeDebffs x spirit_mastery and other cast time buffs"* — everything inside
his bracket is the 333 model; `CastTimePct` is what is outside it.

⚠ Authored by exactly one skill today: the archer's **Spirit Mastery** (−20%), which was a
`BuffCastSpeed` magnitude and therefore did nothing for the archer who cast it. A NEGATIVE
`CastTimePct` lengthens a cast — his *"castTimeDebffs"* — and nothing authors one yet.

🔑 **THE SHOT CUTS CAST TIME BY 30%** (`BL-216`, 0.135.0). The Spell Rune carries
`CastTimePct: 0.30f` — owner: *"It increases the cast speed behind the scene with ~40% ..which is
actually 30% decrease on the final cast time ... (baseCastTime/(charCastSpeed/333))x(runeActive ?
0.7 : 1)"*. Its older `BuffCastSpeed 40` FLAT grant stays beside it and is kept honest about what it
is: forty points on a stat an endgame caster carries at 1400-1900, i.e. **about +2%**.
⚠ The WAR rune has no equivalent — he described the blessed SPIRITSHOT only.

🔑 **THE ROTATION IS CAST + REUSE, AND THE REUSE WAS THE BIGGER HALF.** The reuse starts when the
cast LANDS (`ExecuteSkill`), never when it begins, so a nuke's cycle is the sum. Elemental Blast
(4s authored cast, 1s authored reuse) on a level-90 Magus in epic gear, `--castcycle 90 epic`:

| stack | cast speed | cast | reuse (retain) | cycle |
|---|---|---|---|---|
| NPC shelf only | 1425 | 0.90s | 0.80s (×0.800) | 1.70s |
| + Harmony of the Wizard L8 (−35% reuse) | 1853 | 0.70s | 0.50s (×0.520) | 1.20s |
| + Harmony of the Soul L7 (−20% reuse) | 1853 | 0.70s | 0.40s (**×0.416**) | 1.10s |
| + Spell Rune (−30% cast time) | 1853 | **0.50s** | 0.40s | **0.90s** |

⚠ **A cast-SPEED grant dies at the cap and a cast-TIME cut does not**, because `CastTimeMultiplier`
is applied *after* the 333 model. `StatCaps.CastSpeed` is 1999 and a fully-stacked caster is close to
it. That is the practical difference between the two channels.
📐 `dotnet run --project tools/BalanceMatrix -- --castcycle <level> <quality>`

`SkillDef.CastTimePct` · `Entity.CastTimeMultiplier` · `GameLoopService.BeginCast` / `AutoCycleTicks`

## Which buffs cost a slot (`BL-198`, 0.128.0)

```
occupies a slot  iff  landingDef.Id ∈ SkillCatalog.BuffLimitIds
                 and  CountsTowardBuffLimit         authored VETO; default true, nearly inert now
                 and  not toggle, not a debuff, not Internal, row is Buff or Consumable
cap = 20 · over it the OLDEST counted buff is dropped, FIFO — never a refusal
```

🔑 **IT IS A COLLECTION, NOT A DURATION** (owner, 2026-09-11: *"it should not work only on timer ...
the limit should have an id collection ... i gave the duration as filter not as solution"*).

🔴 **The counterexample that killed the duration test:** *"if one buff a 10 min buff and it doubles it
probanbly break en enter the count .. but it shouldns"*. `BL-190`'s `DoubleDurationRate` doubles a
landed duration **on a roll**, so a 10-minute buff that rolled a double would start costing a square —
the same buff on the same character, decided by a die. **A property of the skill must never read a
number something else in the game is allowed to multiply.**

**The collection is DERIVED, never typed out** (a typed list goes stale and whole tiers vanish from
it), from two sources that between them are exactly his enumeration — *"single buffs, grouped buffs,
harmonies, marks, archers 20 min buffs, any other self 20 min buff"*:

1. **The two shelves unioned** — the buffer CLASS kit + the Spirit Helper's shelf. That is the same
   universe the admin Buffs menu's four drawers come from, so singles, groups, harmonies and Marks are
   in by identity, at any duration.
2. **Every other buff whose AUTHORED `DurationTicks` ≥ 20 min** and whose row is `BuffRow.Buff`. This
   is what sweeps in the archer's Bow Expertise / Blessing / Spirit with no edit anywhere.

⚠ **`BuffRow.Buff` in rule 2 is what keeps the RUNES out** — they run an hour, but a ~1/s
reconciliation loop owns them, so evicting one frees a square for a fraction of a second and then puts
it straight back. Potions and scrolls are also `Consumable`, but their CHILDREN are already in via rule
1 — a potion of Might and a cleric's Might are the same buff from different bottles.

⚠ **Child ids are in the set too.** A single blessing lands through a one-child wrapper, and the buff
on the bar carries the CHILD's id — a set of wrapper ids alone would match nothing at the only moment
it is asked.

📐 `dotnet run --project tools/BalanceMatrix -- --bufflimit` prints both halves: what costs a square
and what does not.

`SkillCatalog.BuffLimitIds` / `DrawerOf` · `GameConstants.BuffLimitMinDurationTicks` (rule 2 only) ·
`GameLoopService.OccupiesBuffSlot` / `CountsAgainstBuffCap` / `EvictOldestBuffIfFull`

`Entity.CooldownReductionFor` · `GameLoopService.ExecuteSkill` / `AutoCycleTicks`

## Mobs

```
Hp    = 40 + 0.8 * L^2
PDef  = 0.00113  * (L + 44)^2.743
MDef  = 0.0027   * (L + 38)^2.542
PAtk  = 1.12e-6  * (L + 31)^4.539
MAtk  = 1.14e-7  * (L + 32)^4.904
Gold  = 25 + L*8
Regen = 0.1%/s of its OWN pool engaged, 5%/s idle          no level term
```

- ⚠ The four combat curves are **one smooth `a*(L+shift)^k` each**, refitted to the current chronicle
  of IG off 2,831 creatures. **Keep them smooth** — bosses derive from the base with passives, so a
  kink is inherited and multiplied.
- ⚠ `StatCalculator.MobMaxHp` is a **different, mostly-unused** linear path. Check which one a mob
  actually uses before quoting a number.
- Mob regen is a fraction of its own pool because the player CON curve is exponential and a mob's
  CON is `15 + 2L` — that curve on a mob is absurd.

### The ZONE's HP multiplier (`BL-78` item 1, 0.94.0 — **re-ruled `BL-148`, 0.108.0**)

```
finalMaxHp = base * rankHpScale * zoneHpScale     a BOSS-rank spawn ignores zoneHpScale

level     zone      elite (zone x rank x4)
 < 40     x1        x4
40-75     x1.5      x6
76-83     x2        x8
  84+     x3        x12
```

- Authored on `SpawnZone.HpScale`, derived by `WorldPlan.HpScaleFor(level)` off the camp's Max level,
  overridable per field with `Band.HpScale`. The two knobs compose in ONE place, `Entity.ApplyMobScale`.
- The **elite column is the product, not a second knob**: `MobRankScale.Hp(Elite)` is x4 flat at every
  level and was not touched (owner: *"elits still have their x4 everywhere"*).
- It multiplies **HP and nothing else** — damage, defence, EXP and drops are untouched. The corollary:
  lowering a rung raises farm rate, since the same EXP now comes out in less time.
- ⚠ A boss is exempt: 0.89.0's measured 12-25 min band derives from the same curve, so a field's x3
  would be inherited and multiplied. An elite is **not** exempt.
- ⚠ Level **83** is filed under x2 — his bands read `x2<83` and `x3 84+`, which leaves 83 unnamed.
- Measure, never derive: `dotnet run --project tools/BalanceMatrix -- --zonehp`.

### Mob ROLE stat lean (`Entity.ApplyMobScale`, skipped for a player-built creature)

```
Archer   PAtk x2     BasicAtk x2    PDef x0.85   Eva +8    range 450
Mage     MAtk x1.5   PAtk x0.5      PDef x0.85   Eva +8    no basic attack
```

- ⚠ The Mage line was `PDef x0.7` with no evasion until 0.94.0. It is IG's `Light Armor Type` — weak
  P.Def, strong evasion, **HP never touched by role**. It compounds with the template's own
  `MobMod.PDef`, which is how `watcher_eye` came to stand in x0.35.

`Game.Shared/MobBaseStats.cs` · `docs/balance/MobCurveVsIG.md` · CSV dump:
`dotnet run --project tools/BalanceMatrix -- --dump-mob-csv`

## Rank (elite / boss)

Applied on top of the base curves above, recorded on the entity and re-applied after **every**
recompute (never multiplied in place).

```
            HP                       PAtk/MAtk   PDef/MDef   Accuracy
Elite       x4                       x3.0        x1.33       +0
Boss        43000 / L^1.49  (min 20) x4          x2.0        +20
Contest     StatCaps.CcRankMult:     Elite x1.33, Boss x2.0   (CON/SPT/ATK, the debuff roll)
```

- 🔑 **THE ELITE'S ATTACK IS x3.0 SINCE `BL-212`** — his x2 on the x1.5 that was there,
  *"so elit with the double in dmg and double in patk should do ~x4 dmg as of now"*. The other half of
  that is `MobRankScale.DamageOut` (see **Damage**), and the two compose to **x5.0 at level 90** on a
  BASIC attack — measured 161 → 806 against a level-90 epic mage, which is his "300 → 1500" exactly.
  On a mob skill carrying power it is less, because damage is a ratio.
- 🔴 **THE BOSS IS EXEMPT FROM `DamageOut`** and therefore unchanged by all of it, on his ruling.
- ⚠ The BOSS HP multiplier is a **curve, not a number**: the base pool is quadratic in L while a
  party's DPS is flat across the game, so a flat multiple made time-to-kill grow with `L²`. This one
  lands every level in the 600-1800s band (12-25 min for a 5-man).
- Time-to-kill ∝ `HP x defence`, and **boss EXP is derived from that product** — so giving a rank
  defence doubled its exp with no exp number edited. That ratio is clamped at 400, which bites only
  below level ~37; the lowest boss in the game is at 44 (score 306), so no boss meets the rail.
- A boss is immune to `SkillEffect.ControlCc` and to knockback; attrition (DoT, stat-down, regen
  suppression) lands normally. God mode resists **everything**, as a resist, never as a refusal.

`Game.Shared/MobRankScale.cs` · `GameLoopService.BuildMob` · `Entity.ApplyMobScale` ·
measured by `dotnet run --project tools/BalanceMatrix` (the two `BL-13` tables)

### The raid level gap (both halves)

`gap = |playerLevel - bossLevel|`, symmetric in both rules.

```
DAMAGE to a boss      gap<=5  x1.0 | 6-10  1.0-0.06*(gap-5) | 11+  max(0.10, 0.70-0.10*(gap-10))
INTERFERING           gap<=9  free | gap>9  one rung up THE BOSS'S JUDGMENT (`BL-98`)
```

**The boss's judgment — six rungs, odd = petrified, even = remembered.**

```
rung  lasts    runs out into   offend while holding it
L1     3 min   → L2            (cannot act)
L2     1 h     → clean         → L3
L3    30 min   → L4            (cannot act)
L4     1 h     → clean         → L5
L5     2 h     → L6            (cannot act)
L6    24 h     → clean         → L5      ← cycles L5<->L6 until 24h pass un-offended
```

- Fires on **any hostile act aimed at the boss** (a landed hit, or a taunt/cancel/debuff that lands
  nothing) and on **any act of support aimed at a PARTY MEMBER fighting it** — heal, MP restore, buff,
  cleanse, resurrect. One AoE reaching two participants costs **one** rung.
- "Fighting it" = the player is in that boss's threat table, claim valid for **30s**.

**`BL-99` — a raid participant is unhelpable by outsiders.** A second rule, gated on **party
membership** rather than level; the two divide the world between them.

```
caster is...        area / party support        aimed single-target support
in his party        lands (BL-98 band applies)  lands (BL-98 band applies)
NOT in his party    silently skipped, no cost   REFUSED at cast start + one rung, ANY level
```

- The splash is skipped and never punished (you chose the ground, not the man on it); the aim is
  punished, at cast start — **the flag is on the reach, not on the heal** (`BL-77`'s shape).
- Only three skills can splash onto a non-party player at all: Urgent Great Heal and the two totems
  (`FriendlyInRadius` / `PlacesTotem`). Everything else is `AlliesInRadius` = party only.
- ⚠ When an alliance / raid group exists, `RaidLocked` reads "raid group", not "party".
- Petrified = `SkillEffect.Stun` + `FreezesHp` — cannot move/cast/attack, HP cannot change in either
  direction, and the character leaves every threat table. An even rung has **no effect at all**.
- ⚠ **Unremovable, and enforced structurally**: the RUNG is the state (`Entity.BossJudgmentRung`,
  read directly by `IsStunned`/`HpFrozen`); the buff is only its icon, re-asserted once a second. The
  clock runs while offline and the expiry walk is exact.

`StatCalculator.RaidLevelGapMult` / `.BossJudges` · `Game.Shared/Skills/Skills.BossJudgment.cs`
(`BossJudgment` = the ladder as data) · `GameLoopService.AddThreat` · `.TryBossJudgment` ·
`.OnSupport` · `.TickBossJudgment` · `.WalkBossJudgmentClock`

## Drop rates

```
effective = RateConfig.DropChanceRate * DropGroupRates[group] * perItemOverride
```

Guaranteed groups (mats / always / scrolls) **ignore the global** — they are authored as absolutes.
⚠ Composed in **one** place, `MobCatalog.EffectiveRate`; never redo this arithmetic at a call site,
or the kill roll and the number the player is shown will disagree.

⚠ A template's `Drops` is **not the whole table**. RANK is a property of the spawn, not the template,
so three layers are added at kill time (`GameLoopService.RollDrop`, mirrored by target-inspect):
`GearDrops` *replaces* the gear groups, and `EnchantScrollDrops`, `UtilityScrollDrops` (return /
resurrection, `BL-174`) and `EliteMatDrops` *add* for elites and bosses.

In a GROUP the member's authored chance **is** its marginal per-kill chance: the group fires once at
the members' SUM and then picks one weighted, so adding or removing members changes how often the
group fires, never what the other members pay.

---

## Where to look when this page is not enough

| | |
|---|---|
| every ceiling and tuning constant | `Game.Shared/StatCaps.cs` |
| the combat maths itself | `Game.Shared/StatCalculator.cs` |
| mob curves | `Game.Shared/MobBaseStats.cs` |
| elite / boss rank multipliers | `Game.Shared/MobRankScale.cs` |
| HP/MP growth per archetype | `Game.Shared/Classes.cs` |
| exp curve | `Game.Shared/ExpCurve.cs` · `docs/balance/ExpCurve.md` |
| skill range tiers, the MP split | `SkillMath` in `Game.Shared/Skills/Skills.cs` |
| **measured** numbers, never derived | `dotnet run --project tools/BalanceMatrix` |

🔑 **Measure, don't derive.** Hand-computed balance numbers have been wrong here before — the whole
2026-07-14 magic re-scale started from a hand-derived diagnosis that blamed the wrong system. Extend
BalanceMatrix rather than doing arithmetic in your head off this page.
