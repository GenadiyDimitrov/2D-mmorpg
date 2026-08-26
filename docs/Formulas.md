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

`Game.Shared/StatCalculator.cs` — `PhysicalDamage`, `MagicDamage`, `ManaDrain`, `PhysicalK`, `MagicK`

## Attack and defence inputs

```
AttackPower      = atk + L*2
PhysicalAttack   = weaponPAtk scaled by  atk/40
MagicAttack      = weaponMAtk scaled by  atk/40          (weapon MAtkBonus decides the split)
Accuracy         = agi + L
Evasion          = agi + L
PhysicalDefBase  = 68 + L*L/100                          (+ gear)
MagicDefBase     = 20 + L*L/100                          (+ gear, + spt)
LevelMod         = (L + 89) / 100
```

🔑 **ONE power stat (ATK) feeds both channels.** The *weapon* decides the split via `MAtkBonus` —
staff high, sword low. **WIT is not power**: it is cast speed + magic crit rate.

`StatCalculator.cs` — `AttackPower`, `PAtkStatMult`, `MAtkStatMult`, `PhysicalDefenceBase`, `LevelMod`

## Hit, evade, crit, block

```
AvoidChance = 0.05 + (defenderEvasion - attackerAccuracy) * 0.01      clamp [0.05, 0.95]
              then clamped again by the level gap, then by both sides' floors
PhysCritRate  = base(weapon) * (1 + (agi - 30)*0.01)                  cap 50%
PhysCritDmg   = 2.0 + bonus                                           cap x10
DoubleChance  = 0.025 + 0.0075 * (atk - 30)                           cap 25%
MagicCritRate = base * 1.63^((wit - 20)/10)                           cap 20%
MagicCritDmg  = 2.0 * mult * (1 - resist)                             cap x5
```

**Block** (shields only, physical only):
1. the shield lowers the attacker's crit **chance**;
2. if it still crits, the crit **ignores the shield**;
3. otherwise roll `BlockChance - skill.BlockAccuracy` → on a block, damage `* (1 - BlockReduction)`.

⚠ **DEX/AGI does NOT affect block** — flat shield values and passives only. **Magic is never blocked.**

`StatCalculator.cs` — `ResolveAvoidChance`, `PhysicalCritBase`, `MagicCritBase`, `PhysicalCritMult`,
`MagicCritMult` · block in `GameLoopService.ApplyDamage`

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

`StatCalculator.DebuffLandChance` · `StatCaps.CcLandMin/CcLandMax/CcLevelFloorGap`

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
MaxHp        = (classMod * (L*L + 3L)/2 + level1Base) * conModifier(con)
MaxMp        = (classMod * (L*L + 3L)/2 + level1Base) * sptModifier(spt)

HpRegen/s    = ((3 + L*0.1) * 1.03^(con - 40) + flats) * stance * safeZone * hpRegenMult * (1+buff%)
MpRegen/s    =  (2 + L*0.08) * sptRegenModifier(spt)
                  * stance * calmSpirit * mpRegenMult * (1+buff%)   + flats

sptRegenModifier(spt) = clamp(1 + (spt - 40)*0.02, 0.70, 1.30)
stance                = running 0.70 | walking 0.85 | STANDING STILL 1.00 | sitting 1.50
```

- `conModifier` is a **steep** curve (~3%/point); `sptModifier` is **gentle** (1.16 @20 → 1.65 @50).
- 🔑 **MP regen has its OWN stat curve.** `sptModifier` still drives Max MP and M.Def, but regen left
  it on 2026-08-26 (`BL-92`) for the wider linear `sptRegenModifier` — so Spirit buys visible sustain
  (every fighter sits at the 0.70 floor; the ork mage reaches 1.10).
- 🔑 **THE MP FLATS ARE OUTSIDE, THE HP FLATS ARE INSIDE.** Not a typo: the global "flats after
  percentages" rule was applied to MP, and the **HP half is deliberately held** pending its own pass.
  The mage weapon-mastery ladder (`mpReg +1.1 … +3.4`) is one of those MP flats; it was a ×1.1…×3.4
  MULTIPLIER until that day, which is what let a buffed mage regenerate ~290% of his own spam cost.
  The armour masteries' `mpReg x1.2` stays a percent.
- 🔑 **A mage gets that ×1.2 exactly ONCE**, from the armour he wears: **robe** from the born
  Spellcaster Mastery, **light** (cleric) / **heavy** (buffer) from their own Armor Mastery. It used to
  be granted twice on robe (×1.44). Never re-add `mpReg` to a `Robe:` slot.
- **Standing still** is derived (no move target), not a `MoveState` — that enum is persisted.
- **×2** inside a city safe zone with `RegenBoost`. No combat/casting suppression, by ruling.
- **Calm Spirit** (nuker, 6 rungs) multiplies the *stance*: ×0.30→×0.70 running, ×1.03→×1.20 walking,
  ×1.00→×1.02 standing. At its top rung walking and standing regen are exactly equal.

`StatCalculator.MaxHp/MaxMp/HpRegenPerSecond/MpRegenPerSecond/SptRegenModifier` ·
`MovementTuning.RegenMultiplier` · `GameLoopService.Regenerate` · measured by `BalanceMatrix --mpregen`

## Speed

```
timeMultiplier = 333 / speedStat                 lower = faster
AttackSpeedStat = weaponBase * 1.0105^(agi - 30)          cap 1500
CastSpeedStat   = classBase  * 1.63^((wit - 20)/10)       cap 1999
   classBase: fighter 150 · human/elf mage 333 · ork mage 300
MoveSpeed: per race+class table, buffed cap 250 (per-entity, raisable)
```

`StatCalculator.SpeedBaseline`, `AttackAgiModifier`, `CastWitModifier`, `ClassBaseCastSpeed` ·
`Entity.EffectiveCastSpeedMultiplier` / `EffectiveAttackSpeedMultiplier` · `StatCaps.MoveSpeed/AttackSpeed/CastSpeed`

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

`Game.Shared/MobBaseStats.cs` · `docs/balance/MobCurveVsIG.md` · CSV dump:
`dotnet run --project tools/BalanceMatrix -- --dump-mob-csv`

## Drop rates

```
effective = RateConfig.DropChanceRate * DropGroupRates[group] * perItemOverride
```

Guaranteed groups (mats / always / scrolls) **ignore the global** — they are authored as absolutes.
⚠ Composed in **one** place, `MobCatalog.EffectiveRate`; never redo this arithmetic at a call site,
or the kill roll and the number the player is shown will disagree.

---

## Where to look when this page is not enough

| | |
|---|---|
| every ceiling and tuning constant | `Game.Shared/StatCaps.cs` |
| the combat maths itself | `Game.Shared/StatCalculator.cs` |
| mob curves | `Game.Shared/MobBaseStats.cs` |
| HP/MP growth per archetype | `Game.Shared/Classes.cs` |
| exp curve | `Game.Shared/ExpCurve.cs` · `docs/balance/ExpCurve.md` |
| skill range tiers, the MP split | `SkillMath` in `Game.Shared/Skills/Skills.cs` |
| **measured** numbers, never derived | `dotnet run --project tools/BalanceMatrix` |

🔑 **Measure, don't derive.** Hand-computed balance numbers have been wrong here before — the whole
2026-07-14 magic re-scale started from a hand-derived diagnosis that blamed the wrong system. Extend
BalanceMatrix rather than doing arithmetic in your head off this page.
