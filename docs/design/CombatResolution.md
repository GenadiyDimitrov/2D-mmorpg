# Combat Hit Resolution — Evade / Miss / Magic-Fail (spec)

> Single source for how an attack decides **land vs miss/fail**.
> 🔴 **The two channels no longer share a resolver (owner ruling 2026-08-10, playtest-20 `57d`).**
> **Physical** keeps the resolver below (accuracy vs evasion, floors, the 5/95 band).
> **Magic has its own formula** — see [§ Magic landing](#magic-landing-its-own-formula) at the
> bottom. Everything in the "resolver" sections applies to the PHYSICAL channel only.
> Keep in sync with the `combat-hit-resolution` memory and `BalanceMatrix.md`.

## The principle
Every "sure to X" guarantee is a **clamp on one number** — the avoid probability
(miss for physical, fail for magic). They don't fight over a single cap; they
*bracket* the result. The everyday stat math lives inside a 5–95% band that is applied **last**;
true `0`/`100%` are reached only by the Sure-Hit / Immunity flags.

## The resolver (per directed attack A → D → avoid probability)

🔴 **CHANGED 2026-08-06 (owner ruling, playtest-19 M1) — the level gap NO LONGER overrides the floors.**
Steps 2 and 3 swapped: the gap applies first, then the floor/ceiling window clamps it. **The band is
active at ALL times**, so there is no 100% lockout any more:

```
1. STAT ROLL    m = 0.05 + (defAvoidStat − atkHitStat) · slope        clamp [0.05, 0.95]
                  physical: evasion (AGI + gear + buffs, NO level)  vs  accuracy
                  (magic no longer enters here at all — see § Magic landing)

2. LEVEL GAP    Δ = atkLevel − defLevel;   G = LevelGap(|Δ|)
                  if Δ > 0:  m = min(m, 1 − G)     // attacker higher → caps defender avoid
                  if Δ < 0:  m = max(m, G)         // defender higher → forces attacker to avoid

3. FLOORS + BAND   m = clamp(m, max(0.05, defenderFloor), min(0.95, 1 − attackerHitFloor))
                  ALWAYS applied last, so 5%/95% and every class floor survive any gap.

4. FLAGS        defender Immune → m = 1.0     |     attacker SureHit → m = 0.0
```

**Precedence (top wins):** Immunity > SureHit > **class floors + the 5/95 band** > level gap > stat roll.

**Why the lockout went (his reasoning, and it is right):** the anti-powerlevel job is *already* done —
and done harder — by `ExpCurve.LevelGapMultiplier`, which pays **zero exp and zero drops at a 13-level
gap**, symmetric. A level-20 character in a level-90 field gains nothing and dies; making him also
*mathematically unable to connect* adds no protection and reads as a broken game. So: a level-20 rogue
still dodges his floor 10% of a level-90's swings, and a level-20 warrior with Precision still lands 10%
of his own. They still die. ⚠ A consequence to accept: nothing is ever unhittable — everyone keeps at
least a 5% hit and a 5% miss against anything.

## Level-gap curve `G(|Δ|)` — both channels

```
Δ ≤ 5        →  0                    (white band)
6 ≤ Δ ≤ 9    →  2.5%·(Δ−5)           +2.5%/lvl   →  2.5, 5, 7.5, 10
10 ≤ Δ ≤ 14  →  10% + 3%·(Δ−9)       +3%/lvl     →  13, 16, 19, 22, 25
15 ≤ Δ ≤ 19  →  25% + 10%·(Δ−14)     +10%/lvl    →  35, 45, 55, 65, 75
Δ ≥ 20       →  100%                 (⚠ then clamped by step 3 — see below)
```

| Δ | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20+ |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **G%** | 2.5 | 5 | 7.5 | 10 | 13 | 16 | 19 | 22 | 25 | 35 | 45 | 55 | 65 | 75 | **100** |

Symmetric: the higher-level character gets +G to **both** hit and evade vs the lower.

⚠ **The curve is unchanged; what changed is that step 3 runs after it.** `G = 100%` no longer means a
lockout — it means "pinned to the edge of the band", i.e. the loser keeps exactly his floor (5%, or more
if a class passive gives him one) and nothing more.

## Class floors (milestones 20 / 40 / 76 = 4th class)

These are **learned passive skills**, NOT hardcoded — the values live in `SkillDef`
`PassiveEffect`s and are auto-granted at the class-change milestone by
`SkillCatalog.FloorPassiveFor`. The resolver takes the **max** floor across passives.

| Floor | Class | Passive | @20 | @40 | @76 | meaning |
|---|---|---|---|---|---|---|
| **Evade** (min phys miss vs them) | Rogue | Evasion Mastery | 10% | 20% | 30% | sure to dodge |
| | Archer | Reflexes | 5% | 10% | 15% | half a rogue's |
| **Hit** (min phys hit; caps incoming miss at 1−floor) | Warrior | Precision | 10% | 20% | 30% | sure to land |

🔴 **There is no magic floor any more** (2026-08-10). The old row — Tank Anti-Magic 10/15/20%, Mage
"Spell Ward" 10% — is gone: the tank's Anti-Magic is a **×2 multiplier** on the magic-fail formula,
and the mage/healer/nuker "Anti-Magic" line is **magic-damage resistance**, which was always what the
CSVs said ("mRes +5%") and was only ever built as a floor because no damage-reduction stat existed.

~~Floor erosion by level gap~~ — **gone with the 2026-08-06 ruling.** A floor is now a floor at every
level gap: it can only ever be *widened* by the gap (when the gap favours you), never eroded.

## Sure-Hit skills (bypass evasion / anti-magic, lose only to defender Immunity)
- **Mighty Blow** (warrior heavy strike) — never misses; the answer to dodgy targets.
- **Disrupt** (instant interrupt) — never misses, always breaks a cast.
- **Weakness / Greater Weakness** (mage def curse) — never fizzles.
Damage nukes are deliberately NOT Sure-Hit, so the fail/anti-magic/level interplay stays.

## Intended matchups (same level, same gear — what the model produces)

| Matchup | Outcome | Why |
|---|---|---|
| Tank vs Mage | **Mage, narrowly** | ⚠ RE-READ 2026-08-10: fizzles no longer decide this. At parity the tank's ×2 is a **2%** fizzle, not 15% — his anti-magic is now m.Def + bulk, and the ×2 only pays off against a caster who out-levels him |
| Tank vs Rogue | **Rogue** | rogue hits ~95% + chips; tank misses 30–60% & hits soft |
| Tank vs Warrior | **even** | bulk vs damage; survival/skill decides |
| Warrior vs Rogue | **Warrior** | hit-floor + Sure-Hit skills + bulk beat evasion |
| Warrior vs Mage | **Mage** | 95% land, range, control, no warrior magic floor |
| Mage vs Rogue | **coinflip** | evasion useless vs magic; both squishy; crit race |
| Archer | flexible kiter | chips warriors, struggles vs tank HP, mirror vs mage/rogue |

Web: **Mage → Warrior → Rogue → Tank**, Tank≈Warrior, Tank↔Mage luck, Mage↔Rogue coinflip.
Magic ignoring evasion is the counter to evasion; Sure-Hit + hit-floor are the melee
counter to evasion; ~~anti-magic floor is the counter to mages~~ — **that counter is gone**
(2026-08-10). What answers a mage now is M.Def + magic resistance (damage), not fizzles.

## Magic landing — its own formula

🔴 **Owner ruling 2026-08-10 (playtest-20 `57d`).** Magic does **not** use the resolver above. In
percentage POINTS, `StatCalculator.MagicFailChance`:

```
fail% = round( 1.3^(defenderLvl − attackerLvl) × defenderMod × weaponMod )    clamp [0, 95]
success% = 100 − fail%
```

| term | value |
|---|---|
| level | `1.3^Δ`. Parity = ×1, so **same level = 1% fail / 99% success**. Casting DOWN rounds to 0 fail from Δ−2. |
| defender | **1** for everyone; **2** with the tank's Anti-Magic passive (2.5 / 3 at the higher rungs — ⚠ those two are extrapolated, the 40+ CSVs will overwrite them). |
| weapon | **1** with a trained caster weapon; **25** with a bow / dual / bare hands (`StatCaps.UntrainedWeaponMagicFailMod`). |

What it produces:

| Δ (def − atk) | 0 | +5 | +10 | +14 | +16 | +18 |
|---|---|---|---|---|---|---|
| wand, normal target | 99% | 96% | 86% | 61% | 33% | 5% |
| wand, tank (×2) | 98% | 93% | 72% | 21% | 5% | 5% |
| **bow** (×25) | **75%** | **7%** | 5% | 5% | 5% | 5% |

**There is deliberately no caster-side magic-accuracy stat.** The model is the level term, the
occasional tank ×2, and the weapon multiplier — nothing else. `MagicFailResist` was deleted: it was
our only spell-landing stat, it was **0 on every character in the game**, and the bow penalty was
built as "halve it", which is why `57d` ("I don't see my magic failing with a bow more than a wand")
was a real bug and not a perception problem.

The 95% clamp keeps the playtest-19 `M1` ruling alive in this channel too — nothing is unhittable,
and a gap that big already pays zero exp and zero drops.

### Magic resistance (a damage reduction, NOT a fizzle chance)
`Entity.MagicResist` sums the CSVs' "mRes +5%" and lands as `MagicDefCoef = 1 + MagicResist`, a
divisor **inside M.Def** — exactly the shape of `PierceDefCoef`/`BluntDefCoef`/`BowDefCoef`. So the
mob resistance ladder in `docs/data/mobs/mobs_passives.csv` reads the same way in both channels:
**1.25 → ×0.8 damage taken**, 2.0 → ×0.5, 0.5 → ×2.0. Those ladder values are exact reciprocals of
0.9…0.5 on purpose; a divisor is what keeps them symmetric.

## Not yet wired
- **Magic resistance has no gear source yet** — only the mage/healer/nuker Anti-Magic passive. Jewels
  are the natural home (they are already the only M.Def source).
- **Mobs have no magic resistance entry yet** — the ladder exists in `mobs_passives.csv` ("the same
  logic for all other resistances") but no `MobModifier` field feeds `Entity.MagicResist`.
- **Immune** flag has no ultimate buff yet (defaults false) — a future "Total Evasion /
  Magic Immunity" active should set `Entity.Immune` for its duration.
- **slope** = 1%/pt — first-pass tuning knob.
- Floors are now data (passives); only the base 5%, the level-gap curve, and the slope
  remain as code constants — per the "stats via skills, not hardcoded" rule.
