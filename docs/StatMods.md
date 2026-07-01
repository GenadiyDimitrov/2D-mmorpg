# StatMods — one stat-modifier system for every buff / debuff / passive / mastery

## Why
Today a single stat lives in 3–4 places: `PassiveEffect` fields, `MasteryEffect`
factors, per-effect buff `EffectMagnitude`s, and the `Effective*` getters that fold
buffs live. Adding one stat (e.g. `mpWhenRestored`, `BowRange`) means editing all of
them, plus a per-class helper (`TankHeavy`, `WarriorArmor`, …). It's easy to miss a
spot and hard to see, for a given final stat, *why* it is what it is.

Goal: **one `StatMods` value** carried by every modifier source, combined by **one
calculator**, so:
- adding a stat = one field, one place;
- every source (passive, buff, debuff, mastery, item attribute, set bonus) speaks the
  same language;
- the player's stat window can show the per-source **breakdown** (base → why → final).

## The model
Each stat carries a **flat** add and a **percent** fraction (both default 0 = no change,
so `default(StatMods)` is inert). Sources are folded (`StatTotals.Add`) and applied
(`StatTotals.Apply`) as:

```
final = (base + Σflat) × (1 + Σpct)
```

This **additive-percent** convention is what the current engine already does, so phases
2–5 migrate with **behavior parity** (same numbers out). It lives in ONE place
(`StatTotals.Apply` / `Add`), so switching to the owner's described **compound** model —
`base × ∏(1+pct) + Σflat` (percents multiply, flat applied after) — is a localized change
once we're ready. (Owner: the formula itself is "nothing to worry about for now"; the
visible payoff is the stat-window breakdown.)

Exceptions that are NOT flat+pct (added in later phases):
- **Floors** (`EvadeFloor`, `HitFloor`, `MagicFailFloor`): GUARANTEES — take the
  **max** across sources, never sum.
- A few "rate" stats are additive only (accuracy, flat crit points): `flat` with `pct 0`.

## Shape
`StatMods` (readonly record struct, all fields default to no-op) with a `{Flat, Mult}`
pair per stat. Grouped: HP/MP, P/M defence, P/M attack, accuracy/evasion, crit
(rate/damage/magic), speeds (atk/cast/move), regen (hp/mp), resists (crit-dmg/crit-rate/
bow/magic-fail/cancel), vamp (melee/spell), cooldown, interrupt (power/resist), the
PvE/PvP × skill/magic/basic damage matrix, shield (block-chance/shield-def), bow range,
restore-mp bonus, and the three floors.

`StatMods.Combine(IEnumerable<StatMods>)` → one aggregate (sum flats, multiply mults,
max floors). A `StatBreakdown` (source label → StatMods) is retained for the UI.

Conditional sources (armor mastery per worn weight, weapon mastery per weapon type)
stay as thin wrappers that RESOLVE to a `StatMods` for the current gear, then feed the
same combine.

## Migration phases (each builds + is behavior-checked)
1. **Foundation** — add `StatMods` + `Combine`/`Apply` (this commit). Nothing uses it
   yet; no behavior change.
2. **Masteries** — `MasteryEffect` → `StatMods` (fold the `TankHeavy`/`WarriorArmor`/…
   helpers into `StatMods` literals). Armor/weapon-mastery profiles resolve to StatMods.
3. **Passives** — `PassiveEffect` → `StatMods`; `RecomputeDerived`'s `ApplyPassive`
   becomes `Combine(all learned-passive StatMods)`.
4. **Buffs/debuffs** — buff `EffectMagnitude`s → each buff carries a `StatMods`; the
   `Effective*` getters collapse into reading the combined buff StatMods. (Keep the
   `SkillEffect` flags only for NON-stat behaviours: CC, DoT, shields, dispel, blink…)
5. **Items/attributes/sets** — the equipped-gear attribute loop emits StatMods too.
6. **Stat window** — surface the `StatBreakdown` (base → each source → final).

Behavior parity is the bar for phases 2–5: same numbers out, unless we deliberately
adopt the compound-percent model where it differs (e.g. Defensive Wall's flat+×2) — flag
those as we hit them.
