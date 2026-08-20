# Damage Model rework — unified `{Flat, Mod}` skills + lowered M.Atk

**Status:** DESIGN DRAFT (2026-07-16). Nothing built. This reverses the signed-off magic scaling, so it
ships only after the numbers below are calibrated in `tools/BalanceMatrix` and the owner approves.
Owner-driven; every number here is MEASURED with the real formulas, not hand-derived.

---

## 1. The two problems this solves

Both are measured (BalanceMatrix, level 80, A-grade gear unless noted).

**A. M.Atk becomes a cosmic number at high level.** `M.Atk = base × levelMod²`, and magic damage takes
`√M.Atk`, so the square is intentional — it cancels the √ so magic *damage* grows linearly in level like
physical. But it makes the *stat* explode: fine at the real cap (~3,900 at L80) but 244k–366k once a
character is over-levelled (the L821 debug case that started this). The big number is real but mostly
cosmetic — the √ compresses it back down in the damage formula.

**B. Physical skills barely scale with your attack.** Physical damage is `77·(pAtk + power)/def` — skill
`power` is a flat ADDITIVE bonus, and our physical skills carry Power 35–326. Next to a 2,000–20,000 pAtk
that flat is noise:

```
  pAtk | basic(mod 1)  add skill(+1000 flat)   what we WANT: MOD skill (flat 4700, mod 2.3)
  2000 |         115             173  (+50%)                     536  (+366%)
 10000 |         577             634  (+10%)                    1598  (+177%)
 20000 |        1154            1212  (+5%)                     2926  (+150%)
```

At 20k pAtk a flat +1000 skill is +5% over a basic — which is *exactly* why late-IG chronicles had to
hand archers 24k/32k-power skills. The fix is a **multiplier**, not a bigger flat.

---

## 2. Current formulas (as-is)

```
Physical:  dmg = 77 · (pAtk + power) / def                     power = flat, additive
Magic:     dmg = 91 · power · √mAtk / def                      power = multiplier
P.Atk =  (fist + weaponP) · PAtkStatMult(ATK) · levelMod       ∝ levelMod¹   (P.Atk-size)
M.Atk =  base · levelMod²                                       ∝ levelMod²   (cosmic at high lvl)
levelMod = (level + 89) / 100                                   = 1.0 at level 11 (the anchor)
PhysicalK = 77,  MagicK = 91   (StatCalculator)
```

The two `power` fields mean opposite things (additive vs multiplicative), which is why physical skills sit
at 35–326 and magic nukes at ~108 yet feel comparable.

---

## 3. The unified model — one skill shape for both channels

Every **damage** skill carries a `{Flat, Mod}` pair (replacing the single `Power`). Basic attacks are
just `Flat 0, Mod 1`.

```
Physical skill:  dmg = 77 · (Flat + Mod · pAtk) / def
Magic skill:     dmg = 91 · (Flat + Mod · mAtk) / def          (see §4 for the √ decision)
Basic attack:    Flat 0, Mod 1            → 77 · pAtk / def   /   91 · mAtk / def
Fighter skill:   e.g. Flat 4700, Mod 2.3  (a 230%-of-pAtk strike with a flat floor)
Mage skill:      e.g. Flat 0,    Mod …     (pure multiplier; mages *may* also take a Flat)
```

Same data structure, same formula shape, class-appropriate constants. `Mod` makes a skill scale WITH the
attack you invested in (gear/buffs pay off through skills); `Flat` gives low-attack characters a floor so
early skills still hit. One class system authors both — which is what we want for the shared discipline
tables.

### 3b. Physical skills at high level — `Mod`, not `Flat` (measured)

The reason physical skills *must* carry a `Mod`: a flat `power` cannot hold the damage curve at high level,
because the defender's P.Def grows with `level²/100` while `pAtk` grows only ~`level`. To hold 500 damage
vs a same-level maxed tank:

```
 Lvl   pAtk  tankPDef | FLAT power to hold 500 | MOD to hold 500 (Flat 1000)
  80   1068     1334  |         7,594          |        7.2
 200   1808     1711  |         9,302          |        5.6
 500   3658     4071  |        22,777          |        7.0
 800   5506     8452  |        49,377          |        9.8
```

The flat balloons (7,594 → 49,377); the **Mod stays a small, roughly-flat single digit** (7–10). It stays
small because the tank's flat armor floor dilutes the `level²` term, so in practice `pDef` and `pAtk` grow
at similar rates — and a `Mod·pAtk` skill rides `pAtk` right alongside `pDef`. So high-level content adds
**bigger Mods (still single/low-double digit), never bigger flats** — no 24k/32k/366k skill powers ever.
In the real 1–90 range a `Mod ~2–3` already feels right; the ballooning only shows past the cap.

---

## 4. DECISION (2026-07-16): Path B (keep √, shrink the number) + honest linear buffs

**Chosen: Option B.** Reasoning from the measured drift work below: the √ is not just cosmetic — it's the
mechanism that **self-balances magic across the whole level range for free** (no per-level tuning, no
high-level skyrocket). Going linear (Option A) reintroduces exactly the "mages skyrocket at high level"
problem the √ prevents (measured drift: L20 → 0.41×, L800 → 2.13× off an L80 anchor). The √ costs the
server nothing (one instruction, mAtk is precomputed). So we KEEP the √ and only fix the two real
complaints — the cosmic number and the dishonest buff display:

**M.Atk model (final — BUILT 2026-07-16):** display-only shrink; combat math UNCHANGED so mobs + heals are
untouched (an early "store shrunk value + linear damage" cut broke mob casters and weakened heals — avoided).
- **Internal M.Atk stays `base·levelMod²`** and `MagicDamage`/`HealAmount` keep their `√` — so damage and
  heals are **byte-for-byte identical** to today (BalanceMatrix: mage 533/2249 vs tank/mob unchanged) and
  mob casters are unaffected.
- **Only the DISPLAY shrinks:** `EffectiveMagicAttackShown = scale·√(internal)` (scale=20), sent to the
  stats window + target frame. 2,954 → **1,087** @L85 (P.Atk size); the cosmic value is also sent as a
  **debug/IG-reference stat** (`MagicAttackInternal`, shown as "M.Atk (internal / IG-ref)").
- **Buffs (owner's square-it idea):** in `EffectiveMagicAttack`, a **magic-only** buff (`BuffMagAtk`) is
  applied **squared** — so its authored % is the HONEST effective % (`+32%` → +32% damage AND +32% on the
  shrunk display; the √ cancels the square). A **shared** attack buff (`BuffAtk`) stays **linear/√-dampened**
  (a `+20%` shared buff → ~+10% magic — the "20% pAtk / 10% mAtk" rule), UNCHANGED, no re-authoring.
- **Owner TODO:** re-author the **magic-only** `BuffMagAtk` values to their effective % (they now grant their
  full authored %, so e.g. an old +50% BuffMagAtk over-performs until halved). Shared BuffAtk buffs need no
  change. Unbuffed magic is already identical.
- Files: `StatCalculator.MagicAttackDisplayScale`, `Entity.EffectiveMagicAttack` (squared magic buff) +
  `EffectiveMagicAttackShown`, `Dtos.StatsUpdate.MagicAttackInternal`, the SendStats + TargetDetails sends,
  and the client "M.Atk (internal / IG-ref)" row. `MagicDamage`/`HealAmount` reverted to `√` (unchanged).

The `{Flat, Mod}` skill unification (§3) and the "physical skills should scale with pAtk" idea are STILL
open and being discussed separately for the PHYSICAL side — see the P.Atk discussion. Option A analysis
below is kept for the record (it's why we chose B).

---

## 4c. HEAL MODEL (owner 2026-07-17) — BUILT. No M.Atk; HealPower + HealReceived.

Heals **no longer use M.Atk** (a caster-weapon fighter no longer overheals). Two `{Flat, Mod}` sides:

```
endHeal   = (HealPowerFlat + skillPower) · HealPowerMod                    (healer OUTPUT)
finalHeal = (HealReceivedFlat + endHeal) · HealReceivedMod                 (target RECEPTION) + the % half
```

- `HealPower` (Flat/Mod) and `HealReceived` (Flat/Mod) are **new Entity stats**, default **0 / ×1**, so an
  untrained healer heals **exactly the skill power — nobody overheals unless a class / gear / passive / buff
  grants HealPower.** Set via `PassiveEffect.HealPowerFlat/Pct` + `HealReceivedFlat/Pct` (gear/passives).
- `HealOutputMult` / **Divine Focus** are **GONE** (2026-08-20, owner) — there is no weapon gate on healing any
  more; a healer with a sword heals for full, and the sword's own low M.Atk is the only trade. Anti-heal
  debuffs (`DebuffHealRecv`) lower `HealReceivedMod`. The **% -of-max-HP** heal half is unchanged (ignores all of this).
- Shown in the stats window: **Heal power (flat / mod)** and **Heal received (flat / mod)** rows.
- ⚠ **Heals are now WEAK by default** (= skill power, ~150-300) because no HealPower source exists yet. Next:
  the healer 20-min HealPower buff (needs a `BuffHealPower` effect) + a heal-power / skill-power retune.

---

## 4b. ⚠ AUTHORING A MAGIC-ATTACK BUFF OR PASSIVE (read before adding one)

The magic channel reads ONLY magic-only modifiers, applied SQUARED (which cancels the √), so **the value you
author is the effective % — author `BuffMagAtk` / `MagAtkPct` at exactly the magic % you want.** Rules:

1. Use **`BuffMagAtk`** (buffs) / **`MagAtkPct`** (passives) for magic. The shared **`BuffAtk` / `AttackPct`
   is PHYSICAL-ONLY now** — it does NOT touch M.Atk. A buff that should boost both channels carries BOTH an
   explicit physical value and an explicit magic value.
2. The magic % is a **design choice, not a formula conversion** — it may differ from the physical %. Example:
   a party "attack" buff might be `BuffPhysAtk 0.20` + `BuffMagAtk 0.10` (magic gets less, on purpose).
3. **No √ math to do.** `BuffMagAtk 0.30` → +30% magic damage AND +30% on the shown M.Atk AND the tooltip
   says +30%. (The 2026-07-16 re-author of the *existing* buffs — 0.75→0.32, 0.55→0.25, 0.20→0.10, 0.16→0.08,
   0.10→0.05 — was only to preserve their OLD √-dampened damage under the new honest formula. New buffs skip
   that; just author the % you want.)

---

## 4-OLD. (kept for the record) Option A vs B analysis

Lowering the M.Atk number can be done two ways. They differ on balance and cost.

### Option B — keep `√`, fold it into the stat (cheap, damage identical)
Store `M.Atk_shown = C · √(M.Atk_internal)` (C≈20) and drop the √ from the damage formula (it's baked into
the stat). Damage is **algebraically identical to today, everywhere** — a pure refactor. Measured:

```
 Lvl   mAtk_old  mAtk_new  fighterPAtk |  dmg_old  dmg_new
  80       2719      1042         1068 |     512      512
  90       3197      1130         1132 |     555      555
 821     366020     12099         5636 |    5945     5945     (C = 20)
```

`mAtk_new` lands right on top of a fighter's P.Atk, and damage never changes. **BUT:** magic skill powers
stay small (Mod ~108, not thousands), and `+%` M.Atk buffs get √-dampened — a `+75%` buff shows as ~`+32%`
on the new number (`√1.75 ≈ 1.32`). Cosmetically odd, damage-correct.

### Option A — drop the `√`, go fully linear (the clean end-state) — RECOMMENDED
`M.Atk = base · levelMod¹` (drop one power → P.Atk-magnitude), damage `= K · (Flat + Mod · mAtk)/def`, M.Def
re-scaled and `K` re-tuned. Magic becomes the **same math as physical**:

- Magic skill powers move into the **thousands, like fighters** (`108 → ~4k`), because Mod now multiplies a
  P.Atk-size number.
- `+%` buffs are **honest** (`+75%` = `+75%` damage; no √ dampening).
- Physical and magic are literally the same formula → one balance intuition, one skill authoring model.
- Cost: a **content pass over every damage skill** + a real re-balance. Damage is held at the calibration
  anchors (below) but scales differently off them (linear, not √) — that is the intended, accepted change.

**Recommendation: Option A.** It is the only one that delivers everything asked — P.Atk-size numbers,
honest buffs, magic skills in the thousands, and physical skills that scale — at the cost of a calibrated
re-author. Option B is the fallback if we want zero balance risk and only cosmetic-size numbers.

---

## 5. Stat scaling under Option A

| stat | now | proposed |
|------|-----|----------|
| P.Atk | `(fist+weaponP)·PAtkStatMult·levelMod¹` | unchanged |
| M.Atk | `base · levelMod²` | `base · levelMod¹` (drop the square → P.Atk-size) |
| P.Def | `PhysicalDefenceBase(level)` + armor | unchanged |
| M.Def | `base · MEN · levelMod¹` | rescaled down to match the smaller M.Atk (keep the ratio) |
| MagicK | 91 (× √) | re-tuned `K` (linear), set by calibration |

Level-scaling stays neutral: `mAtk ∝ levelMod¹` over `mDef ∝ levelMod¹` cancels, so damage tracks `Mod`
and gear, not raw level (same property the √ gave us today).

---

## 6. Skill migration (Power → {Flat, Mod})

`SkillDef.Power` / `PowerAt(level)` → `Flat` + `Mod` (both per-level for multi-level skills). Conversion is
per-archetype, calibrated so a representative build keeps its current damage at the anchor, then the Mod is
raised so skills actually matter:

| archetype | today | proposed shape | intent |
|-----------|-------|----------------|--------|
| Warrior / Tank melee skills | Power 35–326 (flat) | `Flat` small floor + `Mod` 1.5–2.5 | skills scale with pAtk |
| **Archer skills** | flat, negligible | `Flat` low + **`Mod` 2.0–3.0** | fixes the archer problem directly |
| Dagger blow | flat, crit-gated | `Flat` + high `Mod`, still crit-gated | burst scales with pAtk |
| Nuker/mage nukes | Power 37–116 (× √) | `Flat 0` + `Mod` (thousands under linear) | pure multiplier |
| Healer/utility | n/a (heal power separate) | unchanged | heals use HealK, not this |

Basic attacks become `{Flat 0, Mod 1}` and flow through the exact same code path — no special-case.

---

## 7. Calibration & validation (measure, don't derive)

Hold the owner's **signed-off anchors** while re-shaping:

1. A geared mage still **2–3 shots a normal same-level mob**.
2. A mage still does **~300–400 per nuke to a high-level tank** (buffed, in the real fight).
3. NEW: a fighter/archer **skill visibly beats a basic** (Mod does its job — see §1).

Process: extend `tools/BalanceMatrix` (the extreme-level + damage probes are already in) to print
before/after damage for mage nuke, fighter skill, archer skill vs mob and vs tank across levels. Tune `K`,
`M.Def` scale, and per-archetype `Mod`/`Flat` until the anchors hold and skills matter. **No ship until the
before/after table is on screen and approved.**

---

## 8. Implementation order (validated increments)

1. `SkillDef`: add `Flat` + `Mod` (+ per-level), keep `Power` temporarily as a fallback (`Flat=Power, Mod=1`
   default) so nothing breaks mid-migration.
2. `StatCalculator`: new `PhysicalDamage(pAtk, flat, mod, def)` and `MagicDamage(mAtk, flat, mod, def)`;
   route callers through them.
3. `Entity.RecomputeDerived`: M.Atk `levelMod²→levelMod¹`; rescale M.Def; retune `K`.
4. Re-author skills archetype-by-archetype, running BalanceMatrix after each.
5. Client: show M.Atk (now P.Atk-size) and, if useful, a skill's `Mod`/`Flat` in the tooltip.
6. Full BalanceMatrix pass + a live playtest of the anchors before commit.

---

## 9. Open decisions (need owner sign-off before build)

- **§4 fork:** Option A (linear, recommended) vs Option B (keep √, cheap). Everything below assumes A.
- **Mage `Flat`:** do mage skills get a Flat floor too, or stay pure `Mod` (Flat 0)?
- **M.Def magnitude:** shrink it to P.Def-size for readability, or only as far as the math needs?
- **The `Mod` bands** per archetype (§6) — first pass above; final values come from calibration.
- **Basic-attack `Mod`:** exactly 1.0, or a small per-class lean (some classes' basics matter more)?

## 10. Risks / rollback

- Reverses the 2026-07-14 signed-off magic scaling → must re-validate the anchors in play, not just the tool.
- Every damage skill is touched → stage it behind the `Power`-fallback (step 1) so a half-migrated build
  still runs.
- Rollback = revert the formula + stat changes; the `{Flat, Mod}` fields can stay dormant (`Mod=1,
  Flat=Power`) without affecting anything.

See [[l2-stat-system-rework]], [[damage-retune-and-stat-passives]], [[magic-defence-wit-rework]],
[[class-race-identity]]. Balance numbers live in `tools/BalanceMatrix`, not here.
