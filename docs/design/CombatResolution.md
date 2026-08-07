# Combat Hit Resolution — Evade / Miss / Magic-Fail (spec)

> Single source for how an attack decides **land vs miss/fail**. Physical (accuracy
> vs evasion) and magic (anti-magic fizzle) share **one resolver**. This replaces the
> old near-cosmetic `MissChance` (2% base, 1%/pt) and the level-only `MagicFailChance`.
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
                  physical: evasion (DEX + gear + buffs, NO level)  vs  accuracy
                  magic:    magicResist (≈0)                        vs  magicPen (≈0)
                            → same-level magic sits at the 5% base; floor + level do the rest

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
| **Anti-magic** (min magic-fail vs them) | Tank | Anti-Magic | 10% | 15% | 20% | sure to resist |
| | Mage (Nuker + Healer) | Spell Ward | — | 10% | 10% | self-hardened |
| | anti-magic Rogue (3rd-class spec) | *(TODO)* | — | 10% | 15% | less than tank |
| | Warrior / everyone else | — | 5% | 5% | 5% | universal base only |

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
| Tank vs Mage | **luck race** | mage lands 85% (15% anti-magic); tank HP/mDef vs burst — fizzles decide |
| Tank vs Rogue | **Rogue** | rogue hits ~95% + chips; tank misses 30–60% & hits soft |
| Tank vs Warrior | **even** | bulk vs damage; survival/skill decides |
| Warrior vs Rogue | **Warrior** | hit-floor + Sure-Hit skills + bulk beat evasion |
| Warrior vs Mage | **Mage** | 95% land, range, control, no warrior magic floor |
| Mage vs Rogue | **coinflip** | evasion useless vs magic; both squishy; crit race |
| Archer | flexible kiter | chips warriors, struggles vs tank HP, mirror vs mage/rogue |

Web: **Mage → Warrior → Rogue → Tank**, Tank≈Warrior, Tank↔Mage luck, Mage↔Rogue coinflip.
Magic ignoring evasion is the counter to evasion; Sure-Hit + hit-floor are the melee
counter to evasion; anti-magic floor is the counter to mages.

## Not yet wired
- **anti-magic Rogue** floor waits on that 3rd-class spec — add an `anti_magic` rank-2
  passive (—/10/15) and grant it in `FloorPassiveFor` once the spec exists.
- **Immune** flag has no ultimate buff yet (defaults false) — a future "Total Evasion /
  Magic Immunity" active should set `Entity.Immune` for its duration.
- **slope** = 1%/pt — first-pass tuning knob.
- Floors are now data (passives); only the base 5%, the level-gap curve, and the slope
  remain as code constants — per the "stats via skills, not hardcoded" rule.
