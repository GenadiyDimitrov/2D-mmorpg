# Combat Hit Resolution — Evade / Miss / Magic-Fail (spec)

> Single source for how an attack decides **land vs miss/fail**. Physical (accuracy
> vs evasion) and magic (anti-magic fizzle) share **one resolver**. This replaces the
> old near-cosmetic `MissChance` (2% base, 1%/pt) and the level-only `MagicFailChance`.
> Keep in sync with the `combat-hit-resolution` memory and `BalanceMatrix.md`.

## The principle
Every "sure to X" guarantee is a **clamp on one number** — the avoid probability
(miss for physical, fail for magic). They don't fight over a single cap; they
*bracket* the result. The everyday stat math lives inside a soft 5–95% band; true
`0`/`100%` are reached only by the level lockout and by Sure-Hit / Immunity flags.

## The resolver (per directed attack A → D → avoid probability)

```
1. STAT ROLL    m = 0.05 + (defAvoidStat − atkHitStat) · slope        clamp [0.05, 0.95]
                  physical: evasion (DEX + gear + buffs, NO level)  vs  accuracy
                  magic:    magicResist (≈0)                        vs  magicPen (≈0)
                            → same-level magic sits at the 5% base; floor + level do the rest

2. CLASS FLOORS m = clamp(m, defenderFloor, 1 − attackerHitFloor)     (interior window)

3. LEVEL GAP    Δ = atkLevel − defLevel;   G = LevelGap(|Δ|)
                  if Δ > 0:  m = min(m, 1 − G)     // attacker higher → caps defender avoid
                  if Δ < 0:  m = max(m, G)         // defender higher → forces attacker to avoid

4. FLAGS        defender Immune → m = 1.0     |     attacker SureHit → m = 0.0
```

**Precedence (top wins):** Immunity > SureHit > level gap > class floors > stat roll.
(So Sure-Hit still lands at the +20 lockout; a defender's Immunity ultimate beats even Sure-Hit.)

## Level-gap curve `G(|Δ|)` — both channels

```
Δ ≤ 5        →  0                    (white band)
6 ≤ Δ ≤ 9    →  2.5%·(Δ−5)           +2.5%/lvl   →  2.5, 5, 7.5, 10
10 ≤ Δ ≤ 14  →  10% + 3%·(Δ−9)       +3%/lvl     →  13, 16, 19, 22, 25
15 ≤ Δ ≤ 19  →  25% + 10%·(Δ−14)     +10%/lvl    →  35, 45, 55, 65, 75
Δ ≥ 20       →  100%                 (lockout — only SureHit lands)
```

| Δ | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20+ |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **G%** | 2.5 | 5 | 7.5 | 10 | 13 | 16 | 19 | 22 | 25 | 35 | 45 | 55 | 65 | 75 | **100** |

Symmetric: the higher-level character gets +G to **both** hit and evade vs the lower.

## Class floors (milestones 20 / 40 / 76 = 4th class)

| Floor | Class | @20 | @40 | @76 | meaning |
|---|---|---|---|---|---|
| **Evade** (min phys miss vs them) | Rogue | 10% | 20% | 30% | sure to dodge |
| **Hit** (min phys hit; caps incoming miss at 1−floor) | Warrior | 10% | 20% | 30% | sure to land |
| **Anti-magic** (min magic-fail vs them) | Tank | 10% | 15% | 20% | sure to resist |
| | anti-magic Rogue (3rd-class spec) | — | 10% | 15% | less than tank |
| | Mage (Nuker + Healer) | — | 10% | 10% | self-hardened |
| | Warrior / everyone else | 5% | 5% | 5% | universal base only |

Floor erosion by level gap: a Tank's 15% is subsumed once `G ≥ 15` (Δ ≈ 11);
a Rogue's 30% evade is clipped once `G > 70` (Δ ≈ 19) and gone at Δ ≥ 20.

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
- **anti-magic Rogue** floor waits on that 3rd-class spec (plug into the per-entity floor when built).
- **Immune** flag has no ultimate buff yet (defaults false).
- **slope** = 1%/pt — first-pass tuning knob.
