# Every landing modifier, its payload, and the saving stat

> *"Show me all landing modifiers on what debuffs (name + stat decrease) and what is the saving stat
> ... Stuns/hold/fears/charm should be x0.7, All dots can be at x1, all mdef decreases and p Def
> decreases to X1, All p/m.atk and a/c/m.speed x0.85, buff cancel x0.5.. Somethig like that."*
> — owner, 2026-09-13

⚠ **THE TABLE BELOW IS GENERATED FROM THE CODE. Never edit it by hand:**

```
dotnet run --project tools/BalanceMatrix -- --landmods
```

⚠ **NOTHING HAS BEEN RETUNED.** The `want` column is your schema applied *mechanically*, so the
disagreements are visible. See **Open questions** at the bottom — seven skills your five buckets do
not reach, and three places the schema contradicts a ruling you already made.

## How a landing modifier is used

`DebuffLandMod` multiplies the **probability the debuff sticks**, on both roll paths:

- **Contested** (`DebuffLandChance`): 50% at parity, so ×0.70 → 35%, ×0.85 → 42.5%, ×1.50 → 75%.
- **Fizzle** (`MagicFailChance`, ~99% base): the mod scales `1 − fail`, the same quantity.

`save` is the stat that defends: **CON** physical, **SPT** magical, **none** = it takes the fizzle
roll instead of the contest (no `DebuffSchool` declared), or — for Burn — nothing saves at all.
⚠ For a **DoT the FAMILY picks the save**, not the skill's own `DebuffSchool`. The attacker's side is
always **ATK** since 0.142.0.

## The table

```

=== EVERY LANDING MODIFIER, ITS PAYLOAD, AND THE SAVING STAT ===
    `now` = what ships today.  `want` = HIS SCHEMA (2026-09-13), applied mechanically.
      cancel x0.50 | control (stun/hold/fear/charm) x0.70 | dot x1.00
      defence cuts (P.Def/M.Def) x1.00 | offence & speed cuts x0.85
    A skill in two buckets is priced by the FIRST in that order - the half that takes the turn.

--- CANCEL  ->  x0.50   (2 skills)
  skill                    save    now   want  payload
  Arcane Void              SPT    0.30   0.50 < CANCEL 2 buff(s)
  Dazzling Arrow           CON    1.00   0.50 < CANCEL 3 buff(s); Stun

--- SILENCE  ->  NO RULE GIVEN   (3 skills)
  skill                    save    now   want  payload
  Numbing Shock            CON    0.50      - ? SILENCE physical
  Silencing Shock          SPT    0.70      - ? SILENCE magical
  Word of Unmaking         SPT    1.00      - ? SILENCE physical; SILENCE magical

--- CONTROL  ->  x0.70   (24 skills)
  skill                    save    now   want  payload
  Acoustic Shock           CON    1.00   0.70 < Stun +100%
  Bind                     SPT    0.80   0.70 < Root
  Binding Trap             CON    1.00   0.70 < Root
  Boss's Judgment          none   1.00   0.70 < Stun
  Charm                    SPT    0.70   0.70 . CHARM (takes control)
  Creeping Frost           SPT    1.00   0.70 < Stun; Slow
  Devastating Slam         CON    1.00   0.70 < Stun
  Entangling Roots         SPT    0.50   0.70 < Root
  Frost Burst              SPT    1.50   0.70 < BuffMagicDef -35%; Root
  Grapple                  CON    1.00   0.70 < PULL; Stun
  Intimidate               CON    1.00   0.70 < Fear
  Magic Arrow              CON    1.00   0.70 < Stun
  Phantom Jump             CON    1.00   0.70 < Stun
  Phantom Jump             SPT    1.00   0.70 < Slow +75%; CHARM (takes control)
  Phantom Jump             CON    1.00   0.70 < Slow +90%; Fear
  Shield Bash              CON    1.00   0.70 < Stun
  Shield Shock             CON    0.70   0.70 . Stun
  Snare Trap               CON    1.00   0.70 < Root
  Stay                     CON    1.00   0.70 < Root
  Terrifying Roar          CON    1.00   0.70 < Fear
  Warding Step             SPT    0.50   0.70 < Root
  Whisp Bind               SPT    1.00   0.70 < Root
  Whisp Charm              SPT    1.00   0.70 < CHARM (takes control)
  Witches Scarecrow        SPT    0.50   0.70 < Fear

--- DOT  ->  x1.00   (16 skills)
  skill                    save    now   want  payload
  Bleeding                 CON    1.00   1.00 . Bleed DoT + family rider; Slow +15%
  Bleeding                 CON    1.00   1.00 . Bleed DoT + family rider; Slow +15%
  Bleeding                 CON    1.00   1.00 . Bleed DoT + family rider; Slow +15%
  Bleeding Arrow           CON    1.00   1.00 . Bleed DoT + family rider
  Bleeding Trap            CON    1.00   1.00 . Bleed DoT + family rider
  Envenom                  CON    1.00   1.00 . Venom DoT + family rider
  Frost Pierce             CON    0.85   1.00 < Bleed DoT + family rider
  Poison Trap              SPT    1.00   1.00 . Poison DoT + family rider
  Poisoned                 SPT    1.00   1.00 . Poison DoT + family rider; DebuffAtkSpeed +15%; DebuffCastSpeed +15%
  Poisoned                 SPT    1.00   1.00 . Poison DoT + family rider; DebuffAtkSpeed +15%; DebuffCastSpeed +15%
  Poisoned                 SPT    1.00   1.00 . Poison DoT + family rider; DebuffAtkSpeed +15%; DebuffCastSpeed +15%
  Pyro Burst               none   1.50   1.00 < Burn DoT + family rider
  Rupture                  CON    1.00   1.00 . Bleed DoT + family rider
  Toxic Sting              SPT    1.00   1.00 . Poison DoT + family rider
  Venom Burst              CON    1.00   1.00 . Venom DoT + family rider
  Venom Stab               CON    1.00   1.00 . Venom DoT + family rider

--- DEFENCE  ->  x1.00   (10 skills)
  skill                    save    now   want  payload
  Armor Break              SPT    1.50   1.00 < BuffMagicDef -20%; DebuffDef +40%
  Chilled                  none   1.00   1.00 . BuffMagicDef -10%; Slow +30%
  Chilled                  none   1.00   1.00 . BuffMagicDef -10%; Slow +30%
  Chilled                  none   1.00   1.00 . BuffMagicDef -10%; Slow +30%
  Greater Weakness         none   1.00   1.00 . DebuffDef +45%
  Magic Arrow              CON    1.00   1.00 . DebuffAtk +30%; DebuffDef +30%
  Weakness                 none   1.00   1.00 . DebuffDef +30%
  Whisp Armor Break        SPT    1.00   1.00 . BuffMagicDef -20%; DebuffDef +40%
  Whisp Armor Break        SPT    1.00   1.00 . BuffMagicDef -15%; DebuffDef +30%
  Witches Curse            SPT    1.00   1.00 . BuffMagicDef -35%

--- OFFENCE/SPEED  ->  x0.85   (15 skills)
  skill                    save    now   want  payload
  Freeze                   SPT    1.00   0.85 < Slow +65%
  Frost Bind               SPT    1.00   0.85 < Slow +50%
  Frost Spikes             SPT    0.85   0.85 . Slow +45%
  Gravity                  SPT    1.00   0.85 < DebuffAtkSpeed +30%; DebuffCastSpeed +30%
  Hamstring                CON    1.00   0.85 < Slow +60%
  Magic Arrow              CON    1.00   0.85 < DebuffAtkSpeed +30%; Slow +30%
  Sapped                   none   1.00   0.85 < DebuffAtkSpeed +23%; DebuffCastSpeed +23%
  Sapped                   none   1.00   0.85 < DebuffAtkSpeed +23%; DebuffCastSpeed +23%
  Sapped                   none   1.00   0.85 < DebuffAtkSpeed +23%; DebuffCastSpeed +23%
  Thorn Nova               SPT    1.00   0.85 < Slow +40%
  Weapon Break             SPT    1.50   0.85 < DebuffAtk +25%
  Whisp Gravity            SPT    1.00   0.85 < DebuffAtkSpeed +7%; DebuffCastSpeed +7%
  Whisp Gravity            SPT    1.00   0.85 < DebuffAtkSpeed +23%; DebuffCastSpeed +23%
  Whisp Weapon Break       SPT    1.00   0.85 < DebuffAtk +25%
  Whisp Weapon Break       SPT    1.00   0.85 < DebuffAtk +15%

--- UNRULED  ->  NO RULE GIVEN   (4 skills)
  skill                    save    now   want  payload
  Arcane Burst             SPT    1.50      - ? SPT resist -40%
  Boss's Judgment          none   1.00      - ? (no payload on the def — check it)
  Mana Strain              SPT    0.50      - ? (no payload on the def — check it)
  Soul Sap                 none   1.00      - ? DebuffHealRecv +50%

  74 skills.  `<` = today differs from his schema.  `?` = his rule does not cover it.
  NOTHING WAS CHANGED BY THIS COMMAND. It is a report.
```

## Open questions — the seven your rule does not reach

| skill | today | payload | the question |
|---|---|---|---|
| **Numbing Shock** | ×0.50 | silence PHYSICAL | Three silences, three different numbers, no rule. |
| **Silencing Shock** | ×0.70 | silence MAGICAL | You grouped silence with *"a buff removal or bind"* on 2026-09-13 — |
| **Word of Unmaking** | ×1.00 | silence BOTH | so is silence **control (0.70)** or **cancel (0.50)**? And should the |
| | | | double one be steeper than the singles? |
| **Soul Sap** | ×1.00 | anti-heal, −50% HP received | Not a stat cut and not control. It decides fights in a party. |
| **Mana Strain** | ×0.50 | the target's MP costs ×2 | A resource debuff. Nearest bucket is "offence" (×0.85)? |
| **Arcane Burst** | ×1.50 | the target's **SPT resist −40%** | It makes your *next* debuff land more. A force multiplier, not a debuff. |
| **Boss's Judgment** | ×1.00 | the boss's stun on an interloper | Scripted boss mechanic — probably should be outside the schema entirely. |

## Four places the schema disagrees with a ruling you already made

0. 🔴 **It undoes Frost Pierce, which you set an hour earlier in the same session.** Frost Pierce is a
   **bleed**, so "all dots at ×1" makes it ×1.00 — but you put it at **×0.85** for a reason you gave
   yourself: *"it adds 20% slow and the 45% slow from spike it makes the archer with 68 speed"*. Both
   rules are yours and they point opposite ways on this one skill. **My read: the slow argument wins**,
   because it is about this skill specifically and the DoT rule is about the family in general — but
   it is the same question for every DoT that carries a movement rider, and **every bleed in the game
   carries one** (`DotTiers`: −20% move at all ranks). So "all dots at ×1" is really "all dots at ×1
   *including a 20% slow you cannot see on the row*". Worth one more look.

1. 🔴 **Armor Break and Weapon Break are ×1.50 because you said so.** `BL-90`, 2026-08-19:
   *"armor/weapon break + gravity … should be 75% at parity (x1.5)"*. Your new schema puts Armor
   Break at **×1.00** (a defence cut) and Weapon Break at **×0.85** (an attack cut) — both *down* from
   75% to 50% and 42.5%. That is the opposite direction from the rest of this pass. **Confirm before
   I touch them**, because ×1.5 is the number that made the contested curve necessary in the first
   place.
2. 🔴 **Every stun, root and fear outside the nuker is ×1.00 today** — Shield Bash, Devastating Slam,
   Grapple, Intimidate, Terrifying Roar, Snare Trap, Binding Trap, Stay, Acoustic Shock, Whisp Bind,
   Whisp Charm, Creeping Frost, Phantom Jump. Your schema takes **all thirteen to ×0.70**. That is a
   broad, real nerf to every tank and rogue in the game, not a nuker tweak. Intended?
3. 🔴 **Dazzling Arrow cancels 3 buffs AND stuns, at ×1.00.** It is the single most valuable thing a
   debuff can do and it lands at full rate today, while Arcane Void — which only cancels 2 — sits at
   ×0.30. Your schema makes it ×0.50. It is the strongest argument *for* the schema in this file.

## Two things worth knowing before you rule

- ⚠ **Pyro Burst's ×1.50 does nothing.** Burn is saved against by nothing, so `alwaysLands`
  short-circuits the contest *before* the modifier is read. The number is decoration; it should be
  ×1.00 whatever you decide, or deleted.
- ⚠ **`save = none` on a debuff means it takes the FIZZLE roll (~99%), not the contest.** Chilled,
  Sapped, Weakness, Greater Weakness, Soul Sap and Boss's Judgment are all in that state. A ×1.00
  there is close to *"always lands"*, which is a very different thing from ×1.00 on a contested one
  (50%). If defence cuts are going to ×1.00 across the board, these are the rows to look at twice.
