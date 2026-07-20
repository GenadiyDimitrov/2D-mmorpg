# Bare-hands / unequipped balance — investigation (owner request, 2026-07-15)

**Findings + proposal. NO code change yet — awaiting owner sign-off on the approach.**

## The problem (owner)
A naked level-1 fighter has **42 P.Atk** and one-shots level-4-8 mobs; you can level to 20 with no
gear at all. A naked mage has ~43 P.Atk too. Owner: *"I don't think our formulas are wrong, just how
we manage not being equipped."* — which is exactly right.

## Root cause (confirmed vs L2's own formula)
L2J's P.Atk is `P.Atk = basePAtk × STRbonus × levelMod × CHAbonus`
(source: L2J-Mobius `FuncPAtkMod`, same repo we used for the M.Atk/M.Def formulas).

- **`basePAtk` is the WEAPON's Physical Attack.** With no weapon it's a tiny FIST value (~4).
- **STR is only a MULTIPLIER**, not the base. At ~40 STR the bonus is ~0.9× (below the 49-STR baseline).
- So an unarmed L2 L1 fighter has P.Atk ≈ `4 × 0.9 × 0.9 ≈ 3` — he punches for almost nothing. The
  weapon is the *overwhelming* source of P.Atk (a low-grade sword is 20-40, D-grade 80+, and up).

**Ours is inverted.** `StatCalculator.AttackPower(atkStat, level) = atkStat + level·2`, and the weapon's
`AtkBonus` is ADDED on top (`Entity.RecomputeDerived`). So the character's ATK STAT is the base and the
weapon is a top-up:

| | our P.Atk | L2's shape |
|---|---|---|
| Human fighter L1, naked | **42** (40 ATK + 2) | ~3 (fist base × STR × level) |
| + newbie sword (~24 AtkBonus) | 66 (only +57%) | weapon DOMINATES (base jumps 5-10×) |

That 42 vs a level-4 mob (P.Def 44, HP 52): `77 × 42 / 44 ≈ 73` per hit → one-shot. Confirmed.

The same applies to a naked mage (ATK 40 → 43 unarmed P.Atk), which is why the owner noted a mage
could level by punching faster than by casting.

## The fix — DON'T rewrite the formula; penalise being unequipped

The owner is right that the damage formula is fine (we just tuned it, signed off in the magic re-scale).
The surgical fix is to make the UNARMED case weak, leaving every armed number untouched.

`Entity.BasicAttackPower` (which feeds AUTO-ATTACKS) is already a SEPARATE field from `AttackPower`
(which feeds skills). Today: `BasicAttackPower = Max(1, AttackPower)`. Proposal:

```
BasicAttackPower = WeaponType == WeaponType.None
    ? Max(1, (int)(AttackPower * UnarmedFactor))   // fists: a fraction of your power
    : AttackPower;
```

with **`UnarmedFactor ≈ 0.15`** (echoes L2's "fists are almost nothing"; also the value of the old
per-archetype mage basic-attack multiplier we removed). Worked numbers at L1 vs a level-4 mob (52 HP):

| | BasicAttackPower | dmg/hit | hits to kill |
|---|---|---|---|
| naked NOW | 42 | ~73 | 1 (one-shot) |
| naked, factor 0.15 | 6 | ~11 | ~5 |
| newbie sword (armed) | 66 | ~115 | 1 |

So: **naked → ~5 hits (a real chore), armed → snappy.** Exactly the "equip something" pressure L2 has.
Armed damage, skill damage and the damage formula are all untouched.

### Open questions for the owner
1. **`UnarmedFactor` value** — 0.15 (very weak, L2-like) vs 0.25 (weak but usable). Recommend 0.15.
2. **Physical SKILLS while unarmed** — this fix only touches AUTO-attacks (`BasicAttackPower`).
   A naked fighter's physical *skills* still use full `AttackPower`. Most physical skills need a weapon
   anyway, and low-level fighters lean on auto-attacks (the reported problem). Options: leave skills as
   is (simplest), or also require a weapon / apply the factor to physical-skill P.Atk. Recommend leaving
   skills for now; revisit if naked skill-spam becomes a thing.
3. **Unarmored DEFENCE** (owner said "no armor etc"). Naked P.Def today = `68 + level²/100` (68 at L1) —
   not the cause of the one-shotting, but if we want "no armor = fragile" too, add an unarmored P.Def
   penalty (e.g. ×0.5 when no body armor is worn). Separate, smaller change. Recommend a follow-up, not
   part of this fix.

### Not recommended: the "authentic" rewrite
Making the WEAPON the base P.Atk and ATK a pure multiplier (true L2) would touch every weapon value and
every physical damage number — right after we deliberately tuned them. High risk, no gameplay gain over
the unarmed-penalty approach. Skip it.
