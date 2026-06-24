# Balance Matrix & Stat-System Audit (living doc)

> Keep this in sync with the `class-race-identity` memory. Regenerate the matrix
> whenever a combat formula or constant changes. Reference point for the matrix:
> **level 40, no buffs, same starter (F-grade) gear.**

---

## A. Reference formulas (owner's canonical L2-Legacy spec)
```
Max HP        = [((Base_HP_At_Level  * CON_Modifier) * Passives) * Buffs] + Flat_HP
Max MP        = [((Base_MP_At_Level  * MEN_Modifier) * Passives) * Buffs] + Flat_MP
M.Atk.Speed   = Base_Casting_Speed * WIT_Modifier * Mastery * ArmorSet * WeaponSA * Buffs
P.Atk.Speed   = Weapon_Base_Speed  * DEX_Modifier * Mastery * ArmorSet * WeaponSA * Buffs
P.Def         = [((Σ armor pDef + Level_Mod) + Flat_Passives) * Mastery] * Buffs
M.Def         = [((Σ jewel mDef + Level_Mod) + Flat_Passives) * MEN_Modifier] * Buffs
Magic Damage  = ((91 * Skill_Power * √M_Atk) / M_Def) * Shot * Element
Physical Dmg  = ((77 * P_Atk) / P_Def) * Shot * CritPower * Variance
Level_Mod (def)   = Level² / 100
Base dmg lvl-mult = (Level + 89) / 100   [listed, but NOT used in the dmg formulas above]
```

### Reference data tables
- **Base_HP @L75 (raw, pre-modifier):** Tank 3100 · Melee 2600 · Rogue/Archer 2100 · Wizard 1400 · Healer 1200
- **Base_MP @L75 (raw):** Healer 2000 · Wizard 1550 · Buffer 1100 · Fighter/Tank 500
- **Base_Casting_Speed:** Mage 166 · Fighter 150 · Orc Mystic 150
- **Weapon_Base_Speed:** Dagger/Fist 433 · 1H Sword 379 · 2H Sword 325 · Polearm 325 · Bow 293
- **CON_Modifier:** 20→0.79 · 30→1.00 · 36→1.12 · 40→1.35 · 43→1.48 · 45→1.57 · 47→1.67 · 50→1.83
- **MEN_Modifier:** 20→1.16 · 26→1.28 · 30→1.35 · 31→1.36 · 37→1.44 · 40→1.49 · 45→1.57 · 50→1.65
- **WIT_Modifier:** 20→1.00 · 23→1.18 · 25→1.30 · 30→1.63 · 35→2.06 · 40→2.65 · 45→3.39 · 50→4.32
- **DEX_Modifier:** 20→0.90 · 25→0.95 · 30→1.00 · 35→1.05 · 40→1.11 · 45→1.17 · 50→1.23
- **Shots:** Soulshot ×2.00 · Spiritshot ×1.414 · Blessed Spiritshot ×1.414 (+40% cast-bar cut)

---

## B. Implementation status (what we did / what's left)

| Piece | Status | Notes |
|---|---|---|
| **Attack speed** | ✅ matches | Weapon bases (433/379/325/293) + DEX modifier `1.0105^(DEX−30)` — tracks the table closely. |
| **Cast speed** | ✅ matches | Class base 166/150 + WIT modifier `1.63^((WIT−20)/10)` — tracks the table (20→1.0, 40→2.65, 50→4.32). |
| **WIT modifier** | ✅ matches | exponential ≈ reference at all points. |
| **DEX modifier** | ✅ matches | exponential ≈ reference at all points. |
| **Physical damage** | ✅ structure | `77·(pAtk+power)/pDef`, no lvl term. Matches `77·pAtk/pDef`. |
| **Magic damage** | ✅ structure, ⚠ constant | `K·power·√mAtk/mDef`. **K=8, not 91** — deliberate: our mAtk (~120, √≈11) is ~10× smaller than L2's, so 91 would over-damage. Scale choice, not a bug. |
| **HP** | ✅ mostly | tiers hit Melee/Rogue/Wizard/Healer (~2554/2057/1404/1199 vs 2600/2100/1400/1200). **Tank ~2934 vs target 3100 — bump.** |
| **P.Def** | ✅ structure | naked 68 + level²/100 + armor. ⚠ mastery is added FLAT, ref wants it as a `×Mastery` multiplier (minor). |
| **M.Def** | ⚠ **BUG** | structure right (20 + level²/100 + jewels, ×MEN, ×buffs) BUT the **MEN modifier curve is wrong** (see below). |
| **CON modifier** | ⚠ off mid-range | `1.0305^(CON−30)` matches ref at 40–50, but **CON 36 → 1.20 vs ref 1.12** (+7%), and 20 → 0.74 vs 0.79. Affects elf-fighter & mage HP. |
| **MEN modifier** | ❌ **wrong** | we reused the CON curve → **MEN 30 gives 1.00, but ref says 1.35**; fighters get ×0.86 (penalised) when ref says everyone is **1.16–1.65** (fighters ×1.28, mages ×1.49). Must replace with the real MEN curve. |
| **MP** | ❌ **not reworked** | still old `30 + WIT·4 + level·8`. Ref wants **Base_MP tier curve × MEN modifier** — and MP scales with **MEN, not WIT**. No tier base, wrong stat. |
| **Soulshot / Spiritshot** | ❌ not built | no shot system yet (roadmap). Hooks noted in damage callers. |
| **(Level+89)/100 dmg mult** | ➖ removed | listed in ref data but the explicit dmg formulas omit it; we removed it from physical. Ambiguous — leaving out for now. |

### Key fixes identified
1. **MEN modifier (M.Def):** replace the CON-curve reuse with the real MEN table (1.16→1.65). This *raises fighter magic def* (×0.86 → ×1.28) and *shrinks the mage/fighter gap* (mage 1.49 / fighter 1.28 = ×1.16, not ×1.57). Changes the matrix.
2. **MP rework:** Base_MP tier curve (Healer 2000 … Fighter 500 @L75) × MEN modifier; stop using WIT for MP.
3. **CON modifier accuracy:** switch CON (and the others) to **table interpolation** to match the reference exactly instead of a single exponent (fixes CON 36).
4. **Tank HP tier:** bump class-level-mod so L75 tank ≈ 3100.
5. *Later:* mastery as a `×` multiplier on def (not flat); soulshots; buffed matrix; archetype-level matrix.

> Recommended: do all four modifiers as **interpolated reference tables** (single source of truth, exact match, kills the "tangle"). MEN-for-MP and MEN-for-mDef then share one correct curve.

---

## C. Matchup matrix — L40, no buffs, starter gear (FIRST-PASS)
> ⚠ Will shift once the MEN modifier + MP fixes land (fighters become more magic-resistant).

### Per-class baseline (L40)
| Class | HP | pDef | mDef | Main hit (crit-folded) |
|---|---|---|---|---|
| Human Fighter | 1205 | 134 | 31 | phys ~82–100 |
| Elf Fighter | 969 | 134 | 32 | phys ~80–98 |
| Ork Fighter | 1378 | 134 | 33 | phys ~82–100 |
| Human Mage | 410 | 109 | 47 | magic ~84–129 |
| Elf Mage | 378 | 109 | 49 | magic ~82–126 |
| Ork Mage | 507 | 109 | 51 | magic ~78–120 |

### Damage matrix (crit-folded avg per hit/cast)
| Attacker ↓ \ Defender → | Fighter | Mage |
|---|---|---|
| **Fighter** (basic physical) | ~82 | ~100 |
| **Mage** (Magic Bolt, power 45) | ~129 | ~84 |

### Time-to-kill (attack ~1.4/s, cast ~1.5s)
| Matchup | TTK |
|---|---|
| Fighter → Mage | ~3 s |
| Mage → Fighter | ~14 s |

**Read:** stand-still fight → fighter wins; mage's edge = range 500 + CC/kite/burst (skill layer). Ork tankiest, elf frailest, human mid. Scale is sane (no L1 one-shots, no mush).

---

## D. Constants / knobs
`PhysicalK=77` · `MagicK=8` (first-pass) · HP tiers Tank 0.96/Warrior 0.83/Rogue·Archer 0.66/Nuker 0.45/Healer 0.38 (base Fighter 0.80/Mage 0.42) · naked pDef 68 / mDef 20 · caps: move 250, cast 1999, attack 1500, phys crit 50%/×10, magic crit 20%/×3.
