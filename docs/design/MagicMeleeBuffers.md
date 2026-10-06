# Magic-melee buffers — the Warchanter hits with MAGIC

**Status: 🔵 DESIGN ONLY, `BL-335`.** Nothing is built. Your words: *"Just design for now."*
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
| **Sound Burst** (Elf) | physical bow, 900 range, **two hits**, 3s cast | **Magic Stab**: melee, ONE hit, fixed fail (below) |

🔴 **Acoustic Shock already has a 3s cooldown on a 5s stun.** That is the permanent lock §6 warned about, and it is
in the game TODAY. Nothing shortens a repeated stun: there is no immunity window and no diminishing returns, only the
defender's CON/SPT trim (×0.70-×1.00, `BL-156`). A demon who lands the contest can stun a player forever, refreshing
the stun before it expires. Against mobs it hardly matters; in PvP it is the strongest thing a demon owns. **My pick:
a 12s+ cooldown** (or a 2-3s stun). It is a one-cell change in your CSV.

**Magic Stab — fixed fail, raised by the elf's toggle** (your point 8.3: *"40~60% chance to not fail"* and point 3:
*"not always be 60% but 40% and with that toggle on to become 60%"*):
- A **new per-skill field, a FIXED success chance**: the fizzle roll uses only this number. There is no level term,
  no M.Accuracy and no defender modifier, so PvE and PvP behave the same.
- **40% success with the toggle off, 60% with it on**, via a new toggle field that adds to it (+0.20).
- A failed hit still lands ÷ 3, so the average is `0.4 + 0.6/3 = 0.60` of the power with the toggle off and
  `0.6 + 0.4/3 = 0.73` with it on: **the toggle is worth +22% on the stab**. The power is set so that the
  toggle-off average still matches a normal skill.
- ⚠ History: `FixedLandChance` (`BL-204`) lived one day and was deleted (`BL-207`). That was a debuff's land chance;
  this is the damage fizzle, a different roll. It is still a new field, so I'm telling you.

## 4. The toggle — one per race, with a price

Your point 3: *"i just want to make sure their having that toggle as strategy option .. not always on"*. Off = mage
stats, on = near-tank stats. It is gated to **robe** (the human's also to the shield).

| | Human (defence + block) | Demon (attack + accuracy) | Elf (skill damage + evasion) |
|---|---|---|---|
| P.Def | = the **heavy** set of the same grade | = the **heavy** set of the same grade | = the **light** set of the same grade |
| shield | Tank Shield Mastery's numbers (dmg reduction +25%, shield rate +85%, +10% P.Def at its top rung) | — | — |
| crit | P.Crit **damage** resist 15% | P.Crit damage **and** rate resist ~8% | P.Crit **rate** resist 15% |
| offence | — | accuracy + a little M.Atk (or swing power) | evasion + magic SKILL damage + Magic Stab 40 → 60% |

- 🔑 **Three different number sets = three SKILL IDS, not three faces** (`BL-327`: a face changes the name and text,
  never the numbers). `reinforcement` stays as one race's id; the other two are new.
- **The P.Def number per rung is measured**, not guessed: BalanceMatrix gives the gap between the heavy (or light)
  set and the robe set at each grade, and that gap is the rung.

**The price.** You offered a lower max MP, a higher MP cost, or slower casts. 🔑 **My pick: higher skill MP cost while
it is on (×1.3), plus a small MP/s.** Two reasons:
- It hits **exactly the buffer's economy**. Toggle on = you tank and you buff less, or you pay for it. That is the
  "strategy" you asked for.
- It already exists: `MagicMpCostPct` (a negative value raises the cost), which flows through `EffectiveMpCost`, so
  the cast gate and both charges see it. I'd write no new code for it.

Why not the other two:
- **Slower casts** mostly hurt the buff round before a pull, which is not when the buffer fights.
- **Lower max MP** punishes the whole pool every time the buffer turns it on, which is a bigger penalty than you described.

**Sharpening.** You offered keeping it as a SECOND "strategy" toggle with weapon gates. **My pick: drop it and use
one toggle.** Two toggles mean two drains to price, and both on at once would undo the trade-off. One toggle per race
carries both halves (defence AND that race's offence, as in the table above).

**Rename** (*"because it gives attacking bonuses as well"*). Each race has its own id, so each can have its own name.
Suggestions, all generic: **Iron Cadence** (Human), **War Cadence** (Demon), **Wind Cadence** (Elf), or one shared
**Battle Cadence**. Pick any of these or name it yourself.

## 5. Vampirism — no new flag, and all three races reach 9%

Your point 4.

- **Mana Vampirism** is paid only in `ResolveBasicSwing` (*"a skill never drains"*), so the magic swing drains MP
  and Sound Smash / Acoustic Shock / Magic Stab never do. **All three races reach rung 3 (9%)**: the elf's stop at
  rung 2 existed only because of the bow (`ClassSkillTables.Third.cs:489`). The gate `blunt|bow` becomes
  `blunt|dual`.
- **HP vamp needs no `SpellVamp`.** `MeleeVamp` is paid in the same place (`GameLoopService.cs:16179`). It is not
  gated by damage type; the only thing it skips is a **bow**, and none of the three holds one any more. Their own
  Vampiric buff (`cast_vamp`, +7/8/9% melee vampirism) already grants `MeleeVamp`, so it simply keeps working on the
  magic swing, and the skills still don't drain. ⚠ Don't give them `SpellVamp`: that one IS paid on every damage
  skill.

## 6. The small changes

- **Combo Mastery** (your point 5): **3% with blunt/1, 3.5% with blunt/2, 2.6% with duals**, gate `blunt|dual`.
  The engine has two chances today (`ProcChance` 3%, `ProcChanceTwoHanded` 3.45%), so the dual chance is a third
  field. Each race holds exactly one weapon kind (wand / staff / fangs), so a **per-race face** shows only that
  race's % — your "face will show only the needed %" with nothing new in the face system.
- **Monster Knowledge** (your point 6): **the elf learns it too**, on the same rungs 2-4 at 40/48/52.

## 7. What building it costs

- **Your CSV edits** (`buffer 3rd.csv`, `buffer 4th.csv`). Every number is yours; I bring you the measured ones (the
  swing powers, skill powers, toggle P.Def per rung).
- Engine: the magic-swing passive, the fixed success field + its toggle bonus, the dual chance on Combo Mastery, the
  dual proficiency, two new toggle ids.
- BalanceMatrix before/after (swing DPS vs physical, robe + toggle vs heavy/light, the three skills).
- `debuff_landmods.csv` row for Acoustic Shock re-checked (its shape stays `dmg+1 debuff`).
- **A new APK**: the client builds its Learn tab from the compiled class tables.

## ✅ Answered

- 2026-10-05: Sound Burst → Magic Stab; robe for all; Mana Vampirism on the magic swing; the swing keeps the
  accuracy-vs-evasion miss.
- 2026-10-06 (eight points): weapons (§2), swing measured vs physical (§1), the per-race toggle and its stats (§4),
  vamp on the swing for all three races at 9% (§5), Combo Mastery per weapon (§6), Monster Knowledge for the elf (§6),
  the keep/drop lists (§2), all damage skills single-hit (§3). The **demon's third stat** (accuracy + crit resist +
  a little M.Atk) and **Magic Stab one hit** are settled by it.

## ❓ Still open

1. **Acoustic Shock: 3s cooldown on a 5s stun = a permanent lock, live today** (§3). My pick: 12s+ cooldown.
2. **The toggle's price** (§4). My pick: ×1.3 skill MP cost while on + a small MP/s.
3. **One toggle or two** (§4). My pick: one; Sharpening goes.
4. **The toggle's name(s)** (§4).
5. **The human's extra shield skill** from 2026-10-05 (§6 of the old page: ~6s lock, no damage). Your answer sheet
   doesn't mention it, and the human toggle now carries the shield. **Is it dropped?** If it stays, it has the same
   cooldown problem as #1.
6. **`rogue_bow_proficiency` for the elf**: dropped with the bow (§2)? My pick: yes.
