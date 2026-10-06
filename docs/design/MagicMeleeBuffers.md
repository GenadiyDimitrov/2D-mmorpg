# Magic-melee buffers — the Warchanter hits with MAGIC

**Status: 🔵 DESIGN ONLY, `BL-335`.** Nothing is built; the numbers are MEASURED (§8). Your words: *"Just design for now."*
This page includes your answers of 2026-10-05 and your full answer sheet of 2026-10-06 (eight points). The questions
still open are at the bottom, each with my pick.

The problem it solves, in your words: *"we have warriors and tanks and I want to give them something unique"*. Today
every Warchanter is a physical fighter (demon maul + heavy, human mace + shield + heavy, elf bow + light): a
warrior, tank or archer with buffs. After this change all three wear a **robe** and deal **magic** damage. A
**toggle** is the strategy choice: off = mage stats; on = near-tank stats, paid for in the mana that pays for
their buffs.

Nothing here breaks *"a class grants no stats"*: every change is a skill or passive.

---

## 1. The core piece: a passive that turns the BASIC ATTACK into a magic hit

Your call (2026-10-05): *"make the 0mp spell a passive that swaps the basic attack action to a magic dmg one ...
then no need for cast speed and reuse because attack speed will measure them"*.

- **No cast means nothing to interrupt.** A basic swing is not a cast, so `TryInterruptCast` never touches it.
- **No tapping and nothing new for auto-hunt.** Auto-attack and the autopilot already drive the basic attack.
- **The weapon prices itself**: attack speed comes from the weapon, damage from its M.Atk.

**What the swing becomes.** `GameLoopService.ResolveBasicSwing` is the one place it changes.

| part of a basic swing   | today (physical)                 | with the passive (magic)                                    |
| ----------------------- | -------------------------------- | ----------------------------------------------------------- |
| formula                 | `77·pAtk / pDef` (power 0)       | `91·power·√mAtk / mDef` — **needs a POWER per rung**         |
| can it be avoided       | accuracy vs evasion → Miss       | **unchanged: accuracy vs evasion → Miss**, never a fizzle   |
| crit                    | DEX rate, ×10 cap, flat crit dmg | **magic crit**: WIT rate (cap 20%), ×3                       |
| block                   | shield block roll                | **none** — magic is never blocked                            |
| rune                    | War Rune                         | **Spell Rune**                                              |
| HP vamp (`MeleeVamp`)   | on a landed swing                | **unchanged** (§5)                                          |
| MP vamp (`ManaVamp`)    | on a landed swing                | **unchanged** (§5)                                          |
| Combo Mastery, Focus, reflect | on a landed swing          | unchanged                                                   |

🔑 **The magic formula multiplies by `power`, so a swing at power 0 does 0.** The passive carries one power per rung:
the number you balance with.

**How the power is set** (your point 2: *"the swing need to be measured to match same lvl same weapon physical
against default mobs"*): BalanceMatrix measures it. For each rung's level, the magic swing's **damage per second**
against the default mob of that level must equal a physical fighter's swing with the same-grade weapon of the same
kind: staff vs 2H blunt, wand vs 1H blunt, fangs vs duals. I compare damage per second rather than per hit because
it includes each channel's own crit (×3 at WIT rate vs ×10 at DEX rate) and the miss roll. The output is the power
column, which I hand to you for the CSV.

⚠ The damage-out buckets (`Pve/PvpMagicDamagePct` vs `…BasicDamagePct`) must treat the swing as **BASIC**, not
magic. Otherwise the elf toggle's *magic skill* damage (§4) would also raise the elf's swing.

## 2. Weapons and what each race keeps or drops

Your point 1 and point 7:

|                    | Demon                     | Human                         | Elf                                        |
| ------------------ | ------------------------- | ----------------------------- | ------------------------------------------ |
| weapon             | **Battlestaff** (2H magic blunt) | **Wand + shield**      | **fangs / duals** (`Dual`)                 |
| armour             | robe                      | robe                          | robe                                       |
| damage skills      | Sound Smash + Acoustic Shock | Sound Smash                | Sound Burst → Magic Stab                   |

**All three KEEP:** Mage (robe) Armor Mastery, Spellcaster Weapon Mastery. **All three DROP:** Weapon Mastery,
Sharpening (§4 explains why only one toggle is left).

| race  | drops                                                                                         |
| ----- | --------------------------------------------------------------------------------------------- |
| Human | Heavy Armor Mastery, Cleric Heavy Armor Mastery, Tank Crit Resist, **Tank Shield Mastery** (its stats move into the toggle) |
| Demon | Heavy Armor Mastery, Cleric Heavy Armor Mastery, Tank Crit Resist, Fighter Accuracy          |
| Elf   | Bow Mastery, Bow Expertise, Light Armor Mastery, Rogue Evasion, Rogue Crit Resist            |

- ➕ **Elf also loses `rogue_bow_proficiency`** (*"range +400"*, Elf, @40, `buffer 3rd.csv`). It is not on your list,
  but it is a bow-only passive, so it does nothing once the elf holds fangs. I'll drop it unless you say otherwise.
- **`harmonist_bow_proficiency` → `harmonist_dual_proficiency`**, gated to duals: the same cancellation of the
  untrained-weapon caster penalty (×0.5 cast, ×0.5 magic, ×25 fizzle, `Entity.cs:3976`) that the bow one does today.
  It is a new id, not a rename: an id is append-only.
- **The human's 4th-tier `buffer_shield_mastery`** (robe/shield, +10% max HP/MP and regen) **stays**. It is already
  robe-gated and you didn't list it.
- **No magic-weapon gate is needed.** With every hit magic, a maul's M.Atk (192 at S) loses plainly to a staff's
  (281), and a mace's 192 loses to a wand's 256. A mace still works; it just does less.

## 3. Damage skills: all single-hit magic

Your point 8. Every number is **measured** before it reaches your CSV, against the same reference as §1.

| skill | today | becomes |
|---|---|---|
| **Sound Smash** (Human, Demon) | physical, 40 range, 1s cast, 3s cd | **magic spell**, 40 range. ⚠ Being a spell, its 1s cast CAN be interrupted |
| **Acoustic Shock** (Demon) | physical + 5s stun, 40 range, **3s cd** | magic + 5s stun. 🔴 see below |
| **Sound Burst** (Elf) | physical bow, 900 range, **two hits**, 3s cast | **Magic Stab**: melee, ONE hit, high fail (below) |

**Acoustic Shock's 3s cooldown on a 5s stun** is a permanent lock today, because a re-landed stun REFRESHES (a stun is
a buff, and at equal rank the incoming one replaces it). Your answer (2026-10-06): IG's rule fixes it at the root.
The same debuff type cannot be re-landed until the old one wears off, and a re-stun attempt has a chance to BREAK the
stun instead of resetting it. With that rule the cooldown stops mattering. It is a system of its own, filed as
**`BL-336`**. ⚠ **Until `BL-336` is built, the lock stays live in PvP.**

**Magic Stab: the normal curve plus a big fail, and the toggle cuts it** (your 2026-10-06 correction: *"not 60%
fixed .. high chance to fail ... the toggle just to give less fail chance"*):
- **The skill carries its own fail POINTS** (new per-skill field), ADDED to the ordinary fizzle curve. Measured (§8 D):
  **+59 points** reads 60% at parity and still drifts with level (63% at +5, 67% at +8). A ×60 MULTIPLIER on the curve
  would read 60% at parity and hit the 95% cap two levels up, which is why it is points and not a multiplier.
- **The toggle gives M.Accuracy +20**, a field that already exists (the Marks grant it). The elf has no other damage
  spell, so nothing else benefits, as you said. 60% → 40% fail.
- An average cast lands **0.60** of the power with the toggle off and **0.73** with it on, so the toggle is worth +22% on
  the stab.
- That makes **one new field** (the skill's fail points), not two.

## 4. The two toggles — defence and offence, weapon-gated, each with a price

Your point 3, plus your 2026-10-06 idea for Sharpening: **keep it as the second toggle, gated to each race's weapon**
(Human 1H blunt, Demon 2H blunt, Elf duals). 🔑 **I take your version over my single toggle.** It gives four postures
(off / defence / offence / both), and the gate is what enforces each race's weapon. Reinforcement keeps its name.

| | Human | Demon | Elf |
|---|---|---|---|
| **Reinforcement** (robe; human robe+shield) | P.Def to TODAY's heavy level, Tank Shield Mastery's shield numbers, P.Crit **damage** resist 15% | P.Def to today's heavy level, P.Crit damage + rate resist ~8% | P.Def to today's light level, evasion, P.Crit **rate** resist 15% |
| **Sharpening** (1H blunt / 2H blunt / duals) | ❓ your numbers (see below) | accuracy + a little M.Atk | magic SKILL damage + M.Accuracy +20 (Magic Stab 60→40% fail) |

- **Price (your answer):** each toggle **+15% skill MP cost** and **half the MP/s** of today's Sharpening. ⚠ Small
  correction: MP-cost buffs **ADD** in this engine (`MagicMpCostReduction +=`), so both on = exactly **+30%**, not 32%.
  It is your 30% either way. `MagicMpCostPct` already exists, so the price needs no code.
- 🔑 **Different numbers per race = different ids** (`BL-327`: a face never changes a number). So that is three
  Reinforcement ids and three Sharpening ids. Your Combo Mastery answer works the same way: **one chance field, one id
  per race**, and each race's face prints its own number. No third proc field.
- ❓ **The human's Sharpening has no offence listed.** The human's table above only had a defence half. Does the human
  get a Sharpening at all, and if so, what's in it?
- **P.Def as a PERCENT, not a flat** (measured, §8 C). One authored flat point = exactly one final point, so a flat
  can't follow the NPC shelf. Heavy armour does follow it: the gap to today doubles when buffed (645 → 1032 for a
  human at 90). As a % of the robe P.Def, the gap is **the same buffed or not**: Human **+54%** at 85-90, Demon
  **+33%**, Elf **+17%**. Today's Reinforcement is a flat (+300…+700), so this is a change of mode.

**Names** — parked until last, as you said: *Iron / War / Wind Cadence*, or one shared *Battle Cadence*.

## 5. Vampirism — no new flag, and all three races reach 9%

Your point 4.

- **Mana Vampirism** is paid only in `ResolveBasicSwing` (*"a skill never drains"*), so the magic swing drains MP
  and Sound Smash / Acoustic Shock / Magic Stab never do. **All three races reach rung 3 (9%)**: the elf's stop at
  rung 2 existed only because of the bow (`ClassSkillTables.Third.cs:489`). The gate `blunt|bow` becomes
  `blunt|dual`.
- **HP vamp needs no `SpellVamp`.** `MeleeVamp` is paid in the same place (`GameLoopService.cs:16179`). It is not
  gated by damage type; the only thing it skips is a **bow**, and none of the three holds one any more. Their own
  Vampiric buff (`cast_vamp`, +7/8/9% melee vampirism) already grants `MeleeVamp`, so it keeps working on the magic
  swing, and the skills still don't drain. ⚠ Don't give them `SpellVamp`: that one IS paid on every damage skill.

## 6. The small changes

- **Combo Mastery**: one id per race, one chance each — Human 3% (1H blunt), Demon 3.5% (2H blunt), Elf 2.6% (duals).
- **Monster Knowledge**: the elf learns it too, on rungs 2-4 at 40/48/52.
- **`rogue_bow_proficiency`** goes with the bow (your answer).
- **The human's shield skill — SKIPPED** (your answer): *"i dont want a human buffer to swap the real tank"*. A party
  short of something calls the buffer that covers it, but a party of buffers must never match a real party, and never
  at a boss. A **shield strike with higher taunt**, to SHARE aggro with an under-geared tank, is an idea for later,
  not part of this design.

## 7. What building it costs

- **Your CSV edits** (`buffer 3rd.csv`, `buffer 4th.csv`). The measured numbers in §8 are your starting points.
- Engine: the magic-swing passive (per-rung power), the per-skill fail points, Reinforcement P.Def as a %, six
  toggle ids, three Combo Mastery ids, the dual proficiency.
- `debuff_landmods.csv`: Acoustic Shock's row re-checked (its shape stays `dmg+1 debuff`).
- **A new APK**: the client builds its Learn tab from the compiled class tables.

## 8. MEASURED (2026-10-06) — `dotnet run --project tools/BalanceMatrix -- --magicmelee [levels] [--buffed]`

The rig is today's Warchanter with your drop list removed, the new weapons on, the 4th-tier kit at 76+, and the
dual proficiency simulated. The target is a default mob of the same level. All values are expected values: crit,
miss and fizzle are folded in.

**A. The swing power per rung** — the magic swing's DPS = today's physical swing with the same weapon kind:

| level | 40 | 52 | 61 | 76 | 85 | 90 |
|---|---|---|---|---|---|---|
| Human (wand ↔ mace) | 12 | 13 | 13 | 15 | 23 | 24 |
| Demon (staff ↔ maul) | 13 | 15 | 16 | 18 | 26 | 26 |
| Elf (fangs ↔ fangs) | 11 | 15 | 15 | 19 | 28 | 29 |

Buffed (full NPC shelf) the same columns read 11-12 / 11-13 / 13-15 / 14-16 / 21-29 / 21-30, so **the swing power
barely depends on buffs**: one number per rung holds both. The three races are within ~20% of each other, so either
one shared ladder (the elf ~10% light at the top) or one per race.

**B. The skills, priced to a HEALER's rotation** (your *"on par as healers not as nukers/warriors"*). Rotation = best
repeatable damage skill + the class's own swing in the reuse gap; the swing from A fills the buffer's gap. Skills
keep today's authored cast/reuse and become spells (paced by CAST speed). Shock is held at 0.9× Smash; the stab at
the 0.60 average.

| level | healer DPS (target) | Human Smash power | Demon Smash power (Shock 0.9×) | Elf Stab power |
|---|---|---|---|---|
| 40 | 216 | 23 | 15 | 108 |
| 52 | 269 | 36 | 23 | 172 |
| 61 | 325 | 52 | 30 | 240 |
| 76 | 492 | 99 | 54 | 450 |
| 85 | 650 | 102 | 61 | 449 |
| 90 | 656 | 112 | 67 | 501 |

Buffed, the powers come out ~10-50% higher (Human 25 → 170, Demon 18 → 92, Elf 121 → 640), because the healer's
buffed nuke gains more than the swing does. Authoring between the two columns is a judgement for you; I'd start
from the unbuffed one and let the playtest move it.

**Cast speed, which you asked about:** in the same buff state the buffer casts at the healer's speed (×0.68 vs ×0.68
at 90; the elf ×0.59). The party gets the buffer's speed buffs too, so this gives the buffer no edge of its own.
And **the reuse dominates the cycle**: Sound Smash is 0.6s cast + 2.4s reuse, so even the full buffed speed only
takes the cycle from 3.0s to 2.7s. **Cast speed cannot turn these skills into a nuker's**; the power can.

⚠ Two things the measurement showed that are worth knowing:
- **Today's Warchanter already sits on the healer line at 85-90** (Human 650 vs 650 at 85). At 40-61 it is 1.2-1.7×
  over, and the elf 1.1-2× over everywhere. So this change is a cut at low levels and for the elf.
- **On this metric the healer is 97% of the nuker** (656 vs 679 at 90; Vampiric Bolt vs Elemental Blast). The nuker's
  real edge (AoE, the long-reuse nukes this metric skips) isn't counted here, so "on par with the healer" is a
  generous target. If the buffer ends up feeling like a nuker, aim lower than this table.

**C. Reinforcement's P.Def gap** — robe vs TODAY's heavy (light) Warchanter, unbuffed:

| level | 40 | 52 | 61 | 76 | 85 | 90 |
|---|---|---|---|---|---|---|
| Human | 196 (+42%) | 165 (+27%) | 173 (+24%) | 442 (+51%) | 625 (+54%) | 645 (+54%) |
| Demon | 168 (+36%) | 165 (+27%) | 173 (+24%) | 274 (+31%) | 384 (+33%) | 396 (+33%) |
| Elf | 89 (+19%) | 69 (+11%) | 63 (+9%) | 137 (+16%) | 193 (+17%) | 199 (+17%) |

The jump at 76 is the 4th-tier heavy masteries. Against the bare heavy/light ITEMS (no mastery) the elf's gap is ~0:
**a robe with the robe mastery already matches unmastered light armour**. So "light armour of the same grade" has to
mean today's elf, mastery included, or the elf's toggle gets almost no P.Def. Evasion: the elf in robe is 125 at 90
vs 136 today, so the toggle needs **+11 evasion** to close that.

**D. Magic Stab's fail** (added points, toggle = M.Accuracy +20):

| Δ level | −3 | 0 | +2 | +5 | +8 |
|---|---|---|---|---|---|
| fail, toggle off (+59) | 59% | 60% | 61% | 63% | 67% |
| fail, toggle on | 39% | 40% | 41% | 43% | 47% |
| ×60 multiplier instead | 27% | 60% | 95% | 95% | 95% |

## ✅ Answered

- 2026-10-05: Sound Burst → Magic Stab; robe for all; Mana Vampirism on the magic swing; the swing keeps the
  accuracy-vs-evasion miss.
- 2026-10-06 (sheet): weapons, swing measured vs physical, per-race toggle stats, vamp at 9% for all three, Combo
  Mastery per weapon, Monster Knowledge for the elf, keep/drop lists, all damage skills single-hit.
- 2026-10-06 (follow-up): Combo Mastery = one id per race; Magic Stab = curve + high fail, toggle cuts it; the stun
  answer is IG's no-refresh rule (`BL-336`); price = 30% MP cost + Sharpening's MP/s, split across TWO weapon-gated
  toggles; names last; the human shield skill skipped; `rogue_bow_proficiency` dropped; skill power AND cast time
  measured against the healer.

## ❓ Still open

1. **The human's Sharpening** — does the human get one, and what's in it (§4)?
2. **Swing ladder** — one shared ladder or one per race (§8 A)? My pick: one per race. It's the same passive with a
   different power per race, so it's three ids, the same as the toggles.
3. **Skill power: the unbuffed or buffed column** (§8 B). My pick: unbuffed, then the playtest.
4. **The elf's P.Def reference**: today's elf (with mastery, +17%), as measured (§8 C). My pick: yes.
5. **Names** — last.
