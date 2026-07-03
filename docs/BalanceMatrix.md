# Balance Matrix & Stat-System Audit (living doc)

> Keep this in sync with the `class-race-identity` memory. Regenerate the matrix
> whenever a combat formula or constant changes. Reference point for the matrix:
> **level 40, no buffs, same starter (F-grade) gear.**

> **⚠ GEAR CAVEAT (owner, 2026-07-03):** every table here assumes **newbie (lvl 1–19) gear** —
> so it's only accurate for a low-level player. As the player equips stronger sets the numbers
> change (esp. player→mob, where pAtk/pDef scale hard). Owner is providing **lvl 40 / 52 / 61 / 76
> armor SETS (mage + fighter) + weapons** for those bands → template at `docs/gear/gear_sets.csv`.
> **TODO:** once filled, regenerate §G/§H per gear tier (each band uses its own set + weapon).

> **Update 2026-06-26 (healer effect layer):** added a broad buff/effect primitive
> layer (channel-split P/M.Atk buffs, p/m crit rate, crit-dmg/rate resist, bow resist,
> magic-fail floor/resist, interrupt power/resist, melee/spell vamp, cooldown reduction,
> flat+% Max HP/MP & regen, MP restore, % heal). **The base damage/crit/HP/def formulas
> and stats are UNCHANGED**, so the **no-buff matrix in §C still holds.** What changed for
> a FUTURE *buffed* matrix: those effects now exist and the 2nd-class **Healer** has a full
> buff kit (see §F). Spell **range is now per-spell** (not class-tier) — affects kiting,
> not the per-hit numbers. Armor mastery is now data-driven (same numbers). A complete
> buffed matrix still waits on the other classes' buff kits.

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
| **HP** | ✅ **fixed** | tiers hit Melee/Rogue/Wizard/Healer; **Tank class-mod bumped 0.96→1.02 → L75 raw ≈ 3100** (the L2 tank track). |
| **P.Def** | ✅ structure | naked 68 + level²/100 + armor. ⚠ mastery is added FLAT, ref wants it as a `×Mastery` multiplier (minor). |
| **M.Def** | ✅ fixed | 20 + level²/100 + jewels, ×MEN (now the real curve), ×buffs. |
| **CON modifier** | ✅ **fixed** | now interpolates the real CON table (`ConCurve`, 20→0.79 … 36→1.12 … 50→1.83) — accurate at every reference point (was +7% high at CON 36). |
| **Attack training** | ✅ **leveled** | soulshot/spiritshot stand-in is now a LEVELED passive: `TrainingAttackPct` +10% @40 → +80% @75 → +100% @76+ (was a flat +100% @40). Applied to both atk channels. |
| **MEN modifier** | ✅ **fixed** | now interpolates the real MEN table (1.16→1.65). Fighters ×1.26–1.30, mages ×1.47–1.52 — everyone ≥1, small gap. |
| **MP** | ✅ **reworked** | `(MpClassLevelMod·(L²+3L)/2 + Level1BaseMp) × MEN modifier`; scales with **MEN** (Healer 0.68/Nuker 0.53/fighter 0.17 tiers; base mage 0.50). Mobs use a simple level curve. |
| **Soulshot / Spiritshot** | ➖ **cut** | DESIGN DECISION: no damage consumables. The leveled **Attack training** passive (above) is the permanent replacement — there is no shot system to build. |
| **(Level+89)/100 dmg mult** | ➖ removed | listed in ref data but the explicit dmg formulas omit it; we removed it from physical. Ambiguous — leaving out for now. |

| **Buff/effect layer** | ✅ **built (2026-06-26)** | `SkillEffect` widened to `long`; flat+% Max HP/MP & regen, channel-split P/M.Atk buffs, p/m crit rate, crit-dmg/rate resist, bow resist, magic-fail floor/resist, interrupt power/resist, melee/spell vamp, cooldown reduction, % heal, MP restore. Folded in `RecomputeDerived` + combat hooks. Inert unless a buff/passive carries them. |
| **Armor mastery** | ✅ **data-driven (2026-06-26)** | Per-archetype `ArmorMasteryProfile` skills (Skills.Masteries.cs / Skills.Healer.cs) replace the hardcoded table; numbers translated 1:1, level-scaling via `*PerLevel` coefficients. |
| **Weapon mastery** | ⚠️ **data-driven, NUMBERS UNTUNED (2026-06-27)** | Weapon-CONDITIONAL masteries for the FIGHTER archetypes only (`WeaponMasteryProfile`, Skills.WeaponMasteries.cs), learned @20, applied in `RecomputeDerived` only while the matching weapon is held: Warrior Two-Hand (TWO-HANDED sword +15% pAtk/+3% crit, blunt +12% pAtk/+10 acc), Rogue Dual (+10% pAtk/+5% crit/+15% crit dmg), Archer Bow (+12% pAtk/+20% crit dmg/+5 acc), Tank (ONE-HANDED sword/blunt +6% pAtk/+5–10 acc). Masteries now gate on weapon HANDS (1H vs 2H) too. **Mages (Nuker/Healer) have NO weapon-type mastery** — armor mastery + the flat caster passive carry identity. Nukers now use the SAME `spell_mastery` as healers (replaces base `weapon_mastery`). Both mage passives also carry a **bow penalty: holding a bow halves cast speed** (CastSpeedPct −1 ⇒ ×2 cast time). **Fighter damage tables below do not yet include these — re-derive after tuning.** |
| **Spell range** | ✅ **per-spell (2026-06-26)** | `EffectiveRange` returns `def.Range`; no class-tier scaling (bow skills excepted). Heals 600 < attack spells; Holy Bolt 750, Flamebolt 900. |

> **Hit / evade / magic-fail** moved to a unified resolver + level-gap curve + class
> floors (now learnable passives). That layer has its own spec — see
> **`docs/CombatResolution.md`** (this table no longer tracks the old MissChance / MagicFailChance).

### Fixes
1. ✅ **MEN modifier (M.Def) DONE** — real MEN table (interpolated). Raised fighter magic def and shrank the mage/fighter gap.
2. ✅ **MP rework DONE** — Base_MP tier curve × MEN modifier; no longer uses WIT.
3. ✅ **CON modifier DONE** — table-interpolated (`ConCurve`); CON 36 now 1.12 (was 1.20).
4. ✅ **Tank HP tier DONE** — class-mod 0.96→1.02; L75 tank raw ≈ 3100.
5. ✅ **Soulshots CUT** — replaced by the leveled Attack-training passive (no consumable damage).
6. ⬜ *Later:* mastery as a `×` multiplier on def (not flat); buffed matrix; archetype-level matrix.

---

## C. Matchup matrix — L40, no buffs, starter gear (post MEN+MP fix)

### Per-class baseline (L40)
| Class | HP | MP | pDef | mDef | Main hit (crit-folded) |
|---|---|---|---|---|---|
| Human Fighter | 1205 | 203 | 134 | 45 | phys ~82–100 |
| Elf Fighter | 969 | 206 | 134 | 46 | phys ~80–98 |
| Ork Fighter | 1378 | 209 | 134 | 47 | phys ~82–100 |
| Human Mage | 410 | 692 | 109 | 53 | magic ~77–90 |
| Elf Mage | 378 | 700 | 109 | 54 | magic ~76–88 |
| Ork Mage | 507 | 715 | 109 | 55 | magic ~74–85 |

(mDef now via the real MEN curve — fighters jumped ~32 → ~46, so magic hits them softer.)

### Damage matrix (crit-folded avg per hit/cast) — vs **same-level** target
> ⚠ **STALE @40 numbers:** this matrix was computed when the training passive was a flat ×2 at level 40. Training is now **leveled** (`TrainingAttackPct`: +10% @40 → +80% @75 → +100% @76+), so the **@40** attacker numbers below are far too high (they assumed ×2; it's now ×1.1). The **@75** numbers (~×1.8) are roughly right. Regenerate when convenient.
> Human representative. Gear held at **starter** to isolate level + skill-power scaling — real L75 gear would push attacker numbers higher. Mage uses the **3rd-class nuke** (Hurricane-equiv): power 49 @40, 108 @75.

| Attacker ↓ \ Defender → | Fighter | Mage |
|---|---|---|
| **Fighter @40** (basic physical) | ~162 | ~199 |
| **Fighter @75** (basic physical) | ~192 | ~224 |
| **Mage @40** (3rd nuke, power 49) | ~140 | ~119 |
| **Mage @75** (3rd nuke, power 108) | ~182 | ~156 |

> Note the **ratio model**: against a *same-level* target, per-hit numbers barely grow 40→75, because attack and defense scale together (the nuke's power 49→108 ≈ ×2.2 is nearly cancelled by the target's mDef growth 45→96). Level-appropriate fights stay similar in feel; real power gaps come from **gear + skill tier**, not raw level.

### Time-to-kill (attack ~1.4/s, cast ~1.5s)
| Matchup | TTK |
|---|---|
| Fighter → Mage | ~3 s |
| Mage → Fighter | ~20 s |

**Read:** the correct MEN curve makes fighters notably **magic-resistant** (mDef ~46), so a mage's plain nuke now does ~90 to a fighter (was ~129) and the stand-still mage→fighter fight stretched to ~20s. This *widens* the melee dominance, which is intended to be offset by the mage's **CC / kite / burst / mDef-debuff** kit (skill layer) — a plain-nuke mage SHOULD lose a stand-up fight. Ork tankiest, elf frailest, human mid. Mages now have ~3.4× the MP of fighters.

---

## D. Constants / knobs
`PhysicalK=77` · `MagicK=8` (first-pass) · HP tiers Tank 0.96/Warrior 0.83/Rogue·Archer 0.66/Nuker 0.45/Healer 0.38 (base Fighter 0.80/Mage 0.42) · naked pDef 68 / mDef 20 · caps: move 250, cast 1999, attack 1500, phys crit 50%/×10, magic crit 20%/×3.

---

## E. Skill-power spec — skill levels (ranks)
> **Skill levels are now IMPLEMENTED** (`SkillDef.Levels[]`; one skill, per-level power).
> Live powers: base **Magic Bolt** 12/15/21 (L1/7/14) → **Holy Bolt** (healer) 21/25/30/36
> (L20/25/30/35) / **Flame Bolt** (nuker) 95. Healer **Heal** 67/107/151/195/245/301;
> Quick Heal 151/195/245/301; Party Heal 121/156/196/241. The chain below is the design
> shape to keep extending (3rd/4th-class spans); use it when regenerating the matrix.

**Mage single-target nuke chain (L2 reference, pmfun — "okish" per owner):**
| Tier | L2 name | Char levels | Power span (L2 scale) |
|---|---|---|---|
| Base mage | **Wind Strike** | 1–~20 | ~11 → ~45 |
| 2nd class | **Twister** | 20–35 | ~20 → ~37 |
| 3rd class (nuker) | **Hurricane** | 40–74 | **49 → 108** (~+1.7/level) |
| 3rd class (buffer/healer) | Hurricane-equiv | 40–74 | **~43 → ~96** (10–15 below nuker; scales slower) |

**Notes for use:**
- These are L2-scale powers (their formula uses K=91). **Our `MagicK` stays 8** — keep these as the *relative shape* (Wind < Twister < Hurricane; healer ≈ 12% below nuker, slower ramp), feeding our `8·power·√mAtk/mDef`.
- Sanity at our scale (L40 trained mage, mAtk 246, vs fighter mDef 46): power 49 → ~134, power 108 → ~295 per hit (before crit/variance/mDef-debuff).
- Source: pmfun Spellhowler (Hurricane 49→78 @L40–56, extrapolated to ~108 @L74). base.l2j.ru preferred but unreachable (self-signed cert).

---

## F. 2nd-class Healer buff kit (for the future BUFFED matrix)
> The first full buff kit. When a buffed matrix is generated, fold these (cast on the
> relevant ally) on top of the §C baseline. Buffs castable on allies / self.

| Buff (lvl) | Effect (combat-relevant) |
|---|---|
| **Might** 2/3/4 | +12% Attack (both channels) · +8/12/12% Defence · lvl4 also 8% melee vampirism |
| **Force** 1/2 | +18/25 interrupt resist ("magic-cancel resist") · lvl2 also **+55% M.Atk** |
| **Focus** | +20% physical crit rate |
| **Speed** 1–4 | +15→23% cast speed · +20/33 move · +2 evasion |
| **Body** | +10% HP regen |
| **Frenzy** | **−30% Max HP & MP** · +5% P.Atk · +10% M.Atk · +5% cast & atk speed · +5 move |
| **Restore Mana** | +60 MP (instant, on an ally) |

**Passives (always on):** Spell Mastery (+M/P.Atk, +5% cast, **−10% reuse**, MP/HP-regen mult),
Anti-Magic (+M.Def + magic-fail floor 5→10%), Armor Mastery (per worn weight: robe = +MP/regen/def).

> Note for the buffed mage matrix: Force's **+55% M.Atk** → only **√1.55 ≈ +24%** magic damage
> (√mAtk model), and Frenzy's +10% M.Atk → ~+5% damage. Crit/vamp/interrupt buffs shift the
> *feel* (burst, sustain, cast-protection) more than the per-hit average.

---

## G. Newbie-set matrices — Fighter vs Mage @40 & @75 (2026-06-27)
> **Assumptions:** Human, generic BASE class (no 2nd-class spec), Newbie sets equipped +
> armor mastery learned, **all 5 newbie jewels equipped** (2 earrings + 2 rings = 44 mDef;
> necklace = 18 pDef). Buffed = the 5 NPC newbie buffs (Might/Force/Focus/Speed/Body).
> Fighter = basic melee (1H Newbie Sword 24 pAtk); Mage = **Flamebolt** nuke (power
> **95 @40**, **108 @75**). Damage is **crit-folded**, variance averaged to 1.0. ESTIMATE.
> *(Updated 2026-06-27 for the 1→5 jewel slots — mDef roughly doubled vs the old 1-jewel
> baseline, which roughly HALVED magic damage.)*

**Stats @40:**

| | HP | MP | pAtk | mAtk | pDef | mDef | Crit |
|---|---|---|---|---|---|---|---|
| Fighter no-buff | 1246 | 203 | 158 | – | 263 | 101 | 6.4% ×2.0 |
| Fighter buffed | 1682 | 274 | 182 | – | 302 | 131 | 8.3% ×2.35 |
| Mage no-buff | 421 | 881 | – | 160 | 222 | 118 | 2.4% ×2.0 |
| Mage buffed | 568 | 1189 | – | 280 | 255 | 153 | 4.8% ×2.35 |

**Stats @75:**

| | HP | MP | pAtk | mAtk | pDef | mDef | Crit |
|---|---|---|---|---|---|---|---|
| Fighter no-buff | 3691 | 645 | 385 | – | 321 | 151 | 6.7% ×2.0 |
| Fighter buffed | 4983 | 871 | 443 | – | 369 | 196 | 8.7% ×2.35 |
| Mage no-buff | 1233 | 2554 | – | 390 | 279 | 177 | 2.7% ×2.0 |
| Mage buffed | 1664 | 3448 | – | 682 | 321 | 230 | 5.4% ×2.35 |

**Damage / hit (crit-folded), vs same-level UNBUFFED target:**

| L40 attacker | vs Fighter | vs Mage | | L75 attacker | vs Fighter | vs Mage |
|---|---|---|---|---|---|---|
| Fighter no-buff | 49 | 58 | | Fighter no-buff | 98 | 113 |
| Fighter buffed | 59 | 70 | | Fighter buffed | 119 | 137 |
| Mage no-buff | 97 | 83 | | Mage no-buff | 116 | 99 |
| Mage buffed | 134 | 115 | | Mage buffed | 160 | 137 |

**Hits-to-kill (HP ÷ dmg/hit). Buffed = BOTH buffed (attacker dmg recomputed vs the
buffed defender's higher HP + defence):**

| Matchup | @40 no-buff | @40 buffed | @75 no-buff | @75 buffed |
|---|---|---|---|---|
| Fighter → Fighter | 25 | 32 | 38 | 48 |
| Fighter → Mage   | 7  | 9  | 11 | 14 |
| Mage → Fighter   | 13 | 16 | 32 | 40 |
| Mage → Mage      | 5  | 6  | 12 | 16 |

**Action speed** (seconds per action, from the L2 speed model — base attack 15 ticks,
Flamebolt cast 40 ticks + 1 s reuse): Fighter sword ≈ **0.95 s/hit** (no-buff) → **0.71 s**
(Speed +33%). Mage Flamebolt cycle ≈ **3.6 s** @40 / **3.3 s** @75 (no-buff) → **3.0 / 2.75 s**
buffed (robe mastery + Spirit Training + Speed buff). Fighter ≈ 1 hit/s; mage ≈ hits ×3.3.

**TIME-TO-KILL (seconds = hits × action-time). no-buff / buffed:**

| Matchup | @40 | @75 |
|---|---|---|
| Fighter → Fighter | 24 / 23 | 36 / 34 |
| Fighter → Mage   | **7 / 6**  | **10 / 10** |
| Mage → Fighter   | **47 / 48** | **106 / 110** |
| Mage → Mage      | 18 / 18 | 40 / 44 |

**Reading it:**
- **5 jewels = a magic wall.** Doubling mDef ~halved magic damage, so the mage's per-hit
  numbers fell to fighter-ish levels and the magic matchups stretched a lot (Mage→Fighter
  ~47 s @40, ~106 s @75). Physical fights are unchanged (jewels add mDef, not pDef — except
  the necklace's +18 pDef, which only nudged Fighter→Mage).
- **Per-hit roughly even now; per-second the FIGHTER dominates hard.** ~1 s swings vs ~3.3 s
  casts means Fighter→Mage ~7 s vs Mage→Fighter ~47 s @40.
- **Buffs make fights LONGER** (defensive ≈ offensive): HTK/TTK rises ~25 % when both buff.
- **Caveat (current state):** STATIONARY stand-up model with **no mage CC/control yet** — so
  the long mage TTK is acceptable FOR NOW. Once the mage gets its control kit, the fighter is
  meant to "feel those seconds as agony" (kited/locked). Balance raw numbers AFTER control
  lands; don't over-buff mage damage to compensate. *Mob/item balance is a separate next-week
  pass — target: a cleric can solo a same-level mob, slower but not impossible.*

> **KEEP IN SYNC:** the three tables above (damage / hits-to-kill / seconds) are one set —
> regenerate ALL of them whenever a combat formula, constant, stat, weapon/armor/set, or buff
> changes. They share the same assumptions block.

---

## H. Mob ↔ Player — after the mob base-stat CURVE + ranged/caster mobs (2026-07-03)

> **Why:** mob stats now come from the authored per-level curve (`MobBaseStats`, from
> `docs/mobs/mob_base_stats.csv`) instead of the old crude formula — **~2–3× the old HP/def/atk** —
> and mobs gained ARCHER (bow, ×2 P.Atk, light armor) and MAGE (no basic; nuke + jab, ×1.5 M.Atk,
> ×0.5 P.Atk, ×0.7 P.Def) roles. Player stats = §G no-buff (Human, newbie set + 5 jewels). Mob swing
> ≈ **2.0 s** (`MobAttackIntervalTicks` 20). Mage-mob nuke ≈ 4 s cast + 1 s reuse. Same-level. ESTIMATE,
> crit not folded (adds ~5–10%). Variance averaged to 1.0.

**Same-level mob base stats (curve):** L40 → HP 1980 · pDef 143 · mDef 112 · P.Atk 171 · M.Atk 118.
L75 → HP 12420 · pDef 490 · mDef 390 · P.Atk 1065 · M.Atk 748.
(Archer P.Atk = ×2; Mage M.Atk = ×1.5, mob-nuke power = 62 @L40 / 107 @L75, jab 17 / 28.)

### H1. MOB → PLAYER — damage per hit / cast (and hits-to-kill · seconds)
| L40 mob ↓ \ target → | vs Fighter (HP 1246) | vs Mage (HP 421) |
|---|---|---|
| **Melee** (P.Atk 171) | 50 · 25 hits · ~50 s | 59 · 7 hits · ~14 s |
| **Archer** (P.Atk 342, ranged) | 100 · 12 hits · ~25 s | 119 · **4 hits · ~8 s** |
| **Mage** (nuke 62) | 65 · 19 casts · long | 56 · 8 casts · ~32 s |

| L75 mob ↓ \ target → | vs Fighter (HP 3691) | vs Mage (HP 1233) |
|---|---|---|
| **Melee** (P.Atk 1065) | 256 · 14 hits · ~28 s | 294 · 4 hits · ~8 s |
| **Archer** (P.Atk 2130, ranged) | 511 · 7 hits · ~14 s | 588 · **2 hits · ~4 s** |
| **Mage** (nuke 107) | 190 · 19 casts · long | 162 · 8 casts · long |

### H2. PLAYER → MOB @40 (level-appropriate newbie gear) — the ~2–3× longer fights
| Attacker | dmg/hit | vs L40 mob (HP 1980) |
|---|---|---|
| **Fighter** basic (pAtk 158, pDef 143) | 85 | 23 hits · ~23 s |
| **Mage** nuke (Flamebolt 95, mDef 112) | 86 | 23 casts · ~83 s |
> Old L40 mob ≈ 830 HP → the fighter's ~23 s was ~10 s before: the curve ~**2.3×'d** fight length, as intended.
>
> **@75 player→mob omitted on purpose:** §G's L75 player still wears *newbie* gear, so its pAtk (385)
> is far below what the new mob pDef (490) assumes — you'd get ~60/hit (≈200 hits), a gear artifact,
> not a mob problem. A real L75 player has A-grade gear (pAtk many× higher). Regenerate this row only
> against level-appropriate gear.

### H3. Reading it (findings)
- **No one-shots anywhere.** The lowest hits-to-kill is **2** (L75 archer mob → mage, from range); a
  same-level melee mob needs 4 (mage) to 25 (fighter). So the 2–3× jump did **not** create a "mob
  deletes you" case at 40/75. (Low levels <40 and the cleric-solo loop still want a real playtest —
  not modeled here; but with no one-shot at 40/75 and HP/def scaling together, low levels should be OK.)
- **Fights got ~2–3× longer for players** (H2) — exactly the point of the curve. A nuker/cleric now
  grinds a same-level mob HP pool 2–3× bigger, so DPS/heal sustain matters more. **Cleric-solo-a-L30
  target now needs a playtest** — it's "slower" as intended; watch it's not "impossible."
- **Archer mobs are the sharp edge.** ×2 P.Atk from ~450 range = a same-level MAGE dies in **4 (@40) /
  2 (@75)** arrows before closing. Fighters are fine (7–12). Intended flavor (a robe caster shouldn't
  eat arrows), but **the ×2 is my session pick from your 1.5–2.5 range** — if it feels too lethal to
  squishy targets, dial the archer multiplier to ~1.7–1.8 (`MobRole.Archer` in `SpawnOneInZone`). Your call.
- **Mage mobs are slow but steady + FINITE.** ~4 s nukes and MP-gated — outlast their mana and they're
  helpless. Fair. Their per-hit is between melee and archer.

> Regenerate H when the mob curve, a combat constant, the role multipliers, or the mob spells change.

---

## I. Per-gear-tier vs the mob curve (from `docs/gear/gear_sets.csv`, 2026-07-03)

> **ESTIMATE.** Fighter = Heavy body + Gloves/Boots/Helm + **1H Sword** (no shield); Mage = Robe set
> + **Staff**. **BASE variant** set only. Derived stats built from the level formulas + the CSV gear,
> **calibrated to §G's L40 values**. Non-weapon P.Atk/M.Atk carry §G's masteries/attack-training
> (interpolated 40→76). **NOT folded:** weapon ATTRIBUTES (crit-rate to +20%, crit-dmg, +HP, atk-spd —
> these lift offense a lot, esp. crit), armor set % bonuses, and **JEWELS** (so mDef is low — jewels are
> a separate progression to add). Precise numbers need the gear wired in-game; this is the shape/keep-pace check.

### I1. Player derived stats per tier
| Fighter | HP | P.Def | M.Def | P.Atk(sword) | | Mage | HP | P.Def | M.Def | M.Atk(staff) |
|---|---|---|---|---|---|---|---|---|---|---|
| L40 | 1516 | 460 | 45 | 290 | | L40 | 421 | 330 | 53 | 247 |
| L52 | 2020 | 519 | 59 | 406 | | L52 | 645 | 384 | 69 | 360 |
| L61 | 2619 | 569 | 72 | 502 | | L61 | 850 | 423 | 84 | 441 |
| L76 | 4317 | 651 | 98 | 649 | | L76 | 1263 | 485 | 115 | 565 |

### I2. Same-level MELEE mob → player (dmg/hit · hits-to-kill)
| Tier | → Fighter | → Mage |
|---|---|---|
| L40 | 29 · 53 | 40 · 11 |
| L52 | 52 · 39 | 70 · 9 |
| L61 | 75 · 35 | 101 · 8 |
| L76 | 132 · 33 | 177 · 7 |

### I3. Player → same-level mob (dmg · hits/casts-to-kill; mob HP 1980/4070/6520/12980)
| Tier | Fighter (sword) | Mage (staff nuke) |
|---|---|---|
| L40 | 156 · 13 | 106 · 19 |
| L52 | 129 · 32 | 87 · 47 |
| L61 | 118 · 55 | 77 · 85 |
| L76 | 99 · 131 | 62 · 210 |

### I4. Reading it (findings)
- **DEFENSE keeps pace.** Gear pDef/HP scale with mob P.Atk: a fighter stays ~33–53 hits-to-die at every
  tier (survivable), a mage ~7–11 (squishy but STABLE across tiers — it doesn't get worse). So the gear
  curve tracks the mob attack curve well.
- **OFFENSE falls behind at high tiers.** Mob HP + P.Def/M.Def grow faster than weapon atk, so a SOLO grind
  balloons: fighter 13 → **131 hits**, mage 19 → **210 casts** from L40 → L76. That's the real signal.
  - Softeners not modeled that would help: **weapon attributes** (esp. crit-rate/crit-dmg → big DPS), armor
    set % bonuses, and the L2 assumption of **party play**. But even so the top-tier gap is steep.
  - **Levers (owner's call):** raise high-tier weapon P/M.Atk, ease the mob HP/def curve at ~61–85, and/or
    lean the design on crit + attributes + party. Also: **add a jewel tier** (mage mDef is unmodeled/low here).

### I5. Offense WITH weapon attributes + set bonuses (I3 was raw weapon only — NO buffs, NO attrs, NO set)
> I1–I4 = **no buffs, no weapon attributes, no set % bonuses** (raw weapon vs mob). But max-rolled weapon
> attributes (count by level: 40→1/52→1/61→2/76→3) + the dmg-oriented set % bonuses lift DPS a lot — crit
> rate/dmg, atk/cast speed, M.Atk. Estimated DPS multipliers (best attrs picked, dmg set variant):

| Tier | Fighter DPS × | → hits (was I3) | Mage DPS × | → casts (was I3) |
|---|---|---|---|---|
| L40 | ×1.05 | 12 (13) | ×1.32 | 14 (19) |
| L52 | ×1.05 | 30 (32) | ×1.32 | 36 (47) |
| L61 | ×1.44 | 38 (55) | ×1.39 | 61 (85) |
| L76 | ×1.51 | **87 (131)** | ×1.84 | **114 (210)** |

- Sword attrs = crit-rate + atk-speed (crit folded ×~2); staff attrs = cast-speed + M.Atk(√) + magic-crit
  (×3), plus the L76 robe set's **M.Atk ×1.17** — that's why the mage gains more.
- **+ BUFFS** (the 5 NPC buffs: Might +12% atk, Focus +20% crit, Force +55% M.Atk≈+24% dmg, Speed atk/cast):
  another ~×1.3, so **buffed** high-tier ≈ fighter **~67 hits**, mage **~88 casts** at L76.

**Revised reading:** with attributes + set + buffs the high-tier solo grind roughly **halves** vs raw I3.

**OWNER INTENT (2026-07-03) — this is BY DESIGN, not a bug to "fix":**
- **Undergeared for a zone SHOULD be much harder** — that's the intended gate.
- **L76+ solo is MEANT to be harder** (not impossible) — L2's clan/party design. Once the **40+ class
  passives**, more **buffs**, and (later) **dances/songs** land, and with a **party**, it balances out.
  Do NOT over-buff weapons or nerf the high-tier mob curve to make solo easy — leave the gap.
- The stopgap for solo players = a **full-buff NPC buffer up to lvl 75** (see roadmap), until they make an
  alt buffer / one-man-party or find a real one. So model the "buffed" columns as the expected solo baseline.
- Only concrete follow-up from §I: **add the jewel tier** (mage M.Def) — already in `gear_sets.csv`.

> Regenerate I when the gear CSV, the mob curve, or a combat constant changes. Exact version = read the
> derived stats in-game once the gear is equipped (this estimate carries §G's mastery/training baseline).
