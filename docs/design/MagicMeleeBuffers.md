# Magic-melee buffers — the Warchanter hits with MAGIC

**Status: 🟢 BUILT in 0.230.0 (2026-10-06), `BL-335`.** Your go: *"start to build .. change the csvs with what we
talked .. then interpolate the numbers in between"*. **§9 lists what the build did and every number that is mine,
not yours**: they are your CSV cells now, so tune them there after the playtest. §1-§8 are the design as agreed and
the measurement that priced it.

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

## 4. The two toggles — defence and offence, each with a price

Your answers of 2026-10-06. Two toggles per race: Reinforcement (defence) and Sharpening (the race's other half). Each
one costs +15% skill MP and half of today's Sharpening MP/s.

| | Reinforcement | Sharpening |
|---|---|---|
| **Human** (Sharpening gated to the **shield**) | P.Def to match heavy + its mastery, P.Crit **damage** resist, **bow resist** (from Tank Shield Mastery) | Tank Shield Mastery's shield numbers: **shield damage reduction, shield (block) rate, % P.Def** |
| **Demon** (Sharpening gated to the 2H blunt) | P.Def to match heavy + its mastery, P.Crit **rate** resist, P.Crit **damage** resist | more **M.Atk**, **accuracy** |
| **Elf** (Sharpening gated to duals) | **Light Armor Mastery's P.Def** + P.Crit **rate** resist | **evasion** + **spell accuracy** (M.Accuracy +20 → Magic Stab 60→40% fail) + **spell damage** |

- **The human's numbers come from the masteries the human loses:** Heavy Armor Mastery feeds Reinforcement, and Tank Shield
  Mastery splits across both toggles (bow resist → Reinforcement; shield reduction, rate and its % P.Def →
  Sharpening). So the human's §8 C gap (+54%) is shared: the shield mastery's % P.Def moves to Sharpening, and the
  rest is Reinforcement's. The probe splits the two when it's built.
- **The elf's P.Def is only the light mastery** (your answer 4): the measured gap to today's elf is ~+17% at 85-90.
  The crit-rate resist (Rogue Crit Resist's) goes into Reinforcement; the evasion (+11 at 85-90 to reach today's
  elf) goes into Sharpening, beside the spell accuracy and spell damage.
- **Price:** MP-cost buffs **ADD** in this engine (`MagicMpCostReduction +=`), so both on = exactly **+30%**, your
  number. `MagicMpCostPct` already exists, so the price needs no code.
- 🔑 **Different numbers per race = different ids** (`BL-327`: a face never changes a number), so three
  Reinforcement ids and three Sharpening ids. Combo Mastery works the same way: one chance field, one id per race.
- **P.Def as a PERCENT, not a flat** (measured, §8 C). One authored flat point = exactly one final point, so a flat
  can't follow the NPC shelf, but heavy armour does: the human's gap to today goes from 645 to 1032 when buffed. As a
  % of the robe P.Def the gap is the same buffed or not. Today's Reinforcement is a flat (+300…+700), so this is a
  change of mode.

**The swing passive: ONE id, a different rung schedule per race** (your answer 2), like
`fighter_critical_dmg_mastery`: one power ladder, and each race reaches its rungs at its own levels, so each race's
power follows its own row in §8 A. One id; the per-race table is only learn levels.

**Skill power: the UNBUFFED column of §8 B, set lower** (your answer 3: *"almost always the buffer is buffed"*). The
buffed run puts them 10-50% higher, so unbuffed is already on the low side of where they'll sit in play.

**Names: Reinforcement and Sharpening stay** (your answer, 2026-10-06): both are still toggles, and you'll rename them
in the CSVs later if you want to.

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
- 2026-10-06 (second follow-up): the two toggles per race as in §4; the swing = one id with per-race rung schedules;
  skill power = the unbuffed column, set lower; the elf's Reinforcement = only the light mastery. And, beside this
  page, **Holy Bolt + Holy Ray at half damage in PvP**, built as 0.229.3.

- 2026-10-06 (third): the race bolts get the PvP cut too (built as 0.229.4); the elf's crit-rate resist → Reinforcement,
  evasion + spell accuracy → Sharpening; the names stay.

## 9. BUILT — 0.230.0 (2026-10-06)

Both CSVs were rewritten to the design above, and the code was rewritten to the CSVs. `--check` is clean, every SP
ladder rises, and `BalanceMatrix --magicmelee` now reads the BUILT class back.

**The ids.** Different numbers per race need different ids (`BL-327`), so the Human keeps the old ones:

| | Human | Demon | Elf |
|---|---|---|---|
| swing | `magic_swing` (Resonant Strikes): ONE id, each race learns its own rungs | ← | ← |
| damage | `sound_smash` | `sound_smash_demon` + `acoustic_shock` | `magic_stab` (new; `sound_burst` is gone) |
| Reinforcement | `reinforcement` | `reinforcement_demon` | `reinforcement_elf` |
| Sharpening | `sharpening` (shield) | `sharpening_demon` (2H blunt) | `sharpening_elf` (duals) |
| Combo Mastery | `combo_mastery` 3% (1H blunt) | `combo_mastery_demon` 3.5% (2H blunt) | `combo_mastery_elf` 2.6% (duals) |
| other | | | `harmonist_dual_proficiency` (new; `harmonist_bow_proficiency` is gone) |

**Dropped rows** (and so dropped from the class): Heavy Armor Mastery, Heavy Caster Mastery, Critical Damage Resist,
Weapon Mastery (all races), Hit Rate Mastery, Shield Mastery, Light Armor Mastery, Light armor Evasion, Critical
Resist, Bow Mastery, Bow Proficiency, Bow Expertise. **Changed:** Mana Vampirism is gated to blunt or duals and every
race reaches 9%. Monster Knowledge is all three races. Spellcaster Weapon Mastery's weapon cell gained `duals` (the
elf keeps it, §2).
**0.230.4 (his):** the 20-39 Cleric (`cleric 2nd.csv`, shared with the Lightbringer) lost Light Armor Mastery, Light
Caster Mastery (`clerics_light_armor_mastery`, now learned by nobody) and Light armor Evasion: *"Nothing for a light or
heavy armors"*. The robe is the only armour a Cleric masters at 20-39.
**0.230.5 (his fast test):** Human Smash and Elf Stab power ÷3, Demon Smash ×1.5 (Acoustic Shock with it, to stay
0.9×): *"A 76 elf with a magic stab critical one shots a 75 lvl"*, a failed stab or smash still hit like a nuke, and
*"Demon with all the buffs does 3 times lower than human"*. The Human Smash also retires Vampiric Bolt and the Elf Stab
Frost Spikes, the mage-1st ranged nukes a buffer kept to 80. 🔴 The unbuffed `--magicmelee` rig disagrees with what he
saw: before this pass it put all three races at ×0.96-1.04 of the healer, after it Human/Elf sit at ×0.5 and Demon at
×1.4. The 3× and 9× gaps he reported are not in the rig, so something the rig leaves out (his gear, the full buff shelf)
moves the Human and Elf far more than the Demon. Not chased yet.

**The numbers that are MINE, not yours.** Change any of them in the CSV and the code follows:

- **The swing ladder** is measured (§8 A, re-run at every learn level), held so it only rises:
  Human 12/13/14 @40/52/58, then 15/20/23/24 @76/80/83/90. Demon 13/15/16/17 @40/52/60/64, then 18/24/25/26
  @76/80/82/84. Elf 10/12 @40/52, then 15/21/23 @76/80/83.
- 🔴 **The ELF's powers are LOWER than §8 said, on purpose.** The §8 rig never let Spellcaster Weapon Mastery count
  on duals, but the design keeps it for the elf (§2). The built elf therefore has ~30% more M.Atk and read ×1.25-1.40
  of the healer line. I re-solved its swing and Magic Stab against the same targets: Stab is 93 → 248 at the 3rd
  tier and 337 → 401 at the 4th, instead of 108 → 352 and 450 → 501.
- **Skill power** is the unbuffed column of §8 B at every rung. The 4th tier is a straight +1 a level (Human 98 →
  112, Demon Smash 53 → 67, Shock 47 → 61), because the measurement dips at 80 and 83 where the gear tier changes.
  Acoustic Shock is ~0.9× the Demon's Smash.
- **Toggle P.Def is a %, on a rising line.** The measured gap FALLS from 40 to 74 (a robe's mastery grows faster than
  the heavy gap), and a ladder may not fall, so it rises gently across the average. Human/Demon get +22% → +28%
  (40-74). At 76-90 the Human gets +36% → +40% and the Demon +29% → +33%. The Elf gets +9% → +12%, then +15% →
  +18.5%. The Human's Sharpening adds Shield Mastery's +10% P.Def from 70.
- **Crit resists:** Human −15% crit damage (Critical Damage Resist's). Demon **8%** crit rate and **8%** crit damage
  (**yours**, 0.230.1; I had 15%). Elf −15% crit rate (Critical Resist's). Human bow resistance 16% from 60, as Shield
  Mastery had it.
- **Hands (yours, 0.230.1):** the Human's Sound Smash is `blunt/1`, the Demon's Sound Smash and Acoustic Shock are
  `blunt/2`.
- **Demon Sharpening:** M.Atk +10% → +20% (**mine**) and accuracy +3/+4/+5 (Hit Rate Mastery's levels).
- **Elf Sharpening:** evasion +6 → +12 (light-armour evasion, §8 C), +20 M.Accuracy (yours), and spell damage +5%
  → +12% (**mine**).
- **Upkeep:** each toggle costs 2 → 8 MP/s (half the old Sharpening, rounded up). Both lit = +30% skill MP.
- **4th-tier SP and gold** for every rewritten row were copied from the row it replaced. The swing's 4th-tier rows
  price like Spellcaster Weapon Mastery at the same level.

**Measured after the build** (`--magicmelee`, unbuffed): every race's rotation is ×0.96-1.04 of the healer's at
40-90. Reinforcement takes a robe from 1193 to 1670 (Human), 1587 (Demon) and 1414 (Elf) P.Def at 90; today's heavy
Human read 1838 with shield mastery, which Sharpening's +10% brings back to 1790. Magic Stab fails 60% at parity
with Sharpening off and 40% with it on.

**Side effects to know:**
- **Acoustic Shock's stun is a MAGIC debuff now**, so it is resisted with SPT and not CON (`debuff_landmods.csv`
  moved). The shape (`dmg+1 debuff`) and `SUCCESS` x1 are unchanged.
- The damage skills are SPELLS now: cast speed paces them, they can fizzle, and they can be interrupted.
- A Warchanter saved before 0.230.0 still holds the masteries this removed. Delete `game.db` or make a new character.

## ❓ Still open

- **`BL-336`** (stuns cannot be refreshed, and a re-stun can break one) is **deferred by you**: *"i have always
  played with restetting stuns no difference for now"*.
- Everything in §9 marked **mine** is waiting on your playtest.
