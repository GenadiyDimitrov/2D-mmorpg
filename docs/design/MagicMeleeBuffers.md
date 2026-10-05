# Magic-melee buffers — the Warchanter hits with MAGIC

**Status: 🔵 DESIGN ONLY (2026-10-05), `BL-335`.** Nothing built. Your words: *"Just design for now."*

The problem it solves, in yours: *"we have warriors and tanks and I want to give them something unique"*. Today
every Warchanter is a physical fighter (demon maul + heavy, human mace + shield + heavy, elf bow + light), which
is a warrior/tank/archer with buffs. After this, all three hit through the **magic channel**: no evasion and
no block can stop them, a spell can **fail** instead (lands damage ÷ 3), and they crit ×3 off WIT and M.Crit
rather than ×10 off DEX. No other fighter-shaped class does that. The races keep their current shape (demon heavy
hitter, human shield, elf fast and evasive), just turned to magic.

Nothing here breaks *"a class grants no stats"*: every change is a skill or passive.

---

## 1. The core piece: a passive that turns the BASIC ATTACK into a magic hit

Your call (2026-10-05), replacing my "0-MP spell": *"make the 0mp spell a passive that swaps the basic attack
action to a magic dmg one ... then no need for cast speed and reuse because attack speed will measure them"*.

It is the better design, and for more reasons than the one you gave:
- **No cast means nothing to interrupt.** A caster standing in melee is hit constantly, and any hit can cancel a
  cast (`TryInterruptCast`, chance ∝ damage ÷ max HP). A basic swing is not a cast.
- **No tapping and nothing new for auto-hunt.** The attack action, auto-attack and the autopilot already drive the
  basic attack; they keep doing so.
- **The staff/fang choice prices itself.** Attack speed comes from the weapon, damage from its M.Atk.

**What the swing becomes** (`GameLoopService.ResolveBasicSwing` is the one place, it already serves cleave and
the failed blow):

| part of a basic swing        | today (physical)                   | with the passive (magic)                                       |
| ---------------------------- | ---------------------------------- | -------------------------------------------------------------- |
| formula                      | `77·pAtk / pDef` (power 0)         | `91·power·√mAtk / mDef` — **needs a POWER**, see below          |
| can it be avoided            | evasion roll → Miss                | **fizzle roll** → lands ÷ 3 (the magic twin of a miss)          |
| crit                         | DEX rate, ×10 cap, flat crit dmg   | **magic crit**: WIT rate (cap 20%), ×3                          |
| block                        | shield block roll                  | **none** — magic is never blocked                               |
| rune (`FinalizeDamage`)      | War Rune                           | **Spell Rune**                                                 |
| vampirism                    | `MeleeVamp`                        | **`SpellVamp`** (see §4)                                        |
| timing                       | attack speed                       | attack speed (unchanged)                                       |
| Combo Mastery proc, Focus    | fire on a landed swing             | unchanged                                                      |
| Mana Vampirism, reflect, cancel power | on a landed swing         | unchanged (❓ Q3)                                              |

🔑 **The magic formula multiplies by `power`, so a basic hit at power 0 does 0.** The passive must carry a power per
rung: the same ladder shape every other passive has, and the one number per level you balance with. Demon and human
differ here (§2).

## 2. Human and Demon

| | Demon | Human |
|---|---|---|
| weapon | staff (2H magic blunt) | wand + shield |
| armour | robe | robe |
| basic-attack passive | **slower, heavier** (staff speed, higher power) | **faster, lighter** (wand speed, lower power) |
| MP skill | Sound Smash → melee **magic** damage | Sound Smash → melee **magic** damage |
| second skill | Acoustic Shock → magic damage + contested stun (the demon's debuff) | **new**: shield-gated stun or hold, **no damage** |

**Taken away:** Heavy Armor Mastery, Weapon Mastery, Critical Damage Resist (heavy-gated), Hit Rate Mastery (demon),
and **Sharpening** (*"would change depending on need - for now dropped for them"*). The human's 3rd-tier Shield
Mastery is gated `heavy/shield`; it moves to `robe/shield`, which the 4th tier's `buffer_shield_mastery` already is.

**Kept:** Robe Armor Mastery + Spellcaster Weapon Mastery.

**Reinforcement becomes the "heavy armour toggle"**: gated to **robe**, carrying what the heavy-armour passives gave
(P.Def, crit-damage resist), at a **lower MP/s** than today. Your target: *robe + Reinforcement == heavy*. The armour
gate already exists (`WEIGHT` column), so this is data. ⚠ The robe mastery's caster bonuses ride ON TOP of that, so
the MP/s is the only price; set it with that in mind (BalanceMatrix, not by hand).

**No magic-weapon gate is needed.** Once heavy pays nothing and every hit is magic, a maul's extra P.Atk is unused
and its M.Atk (192 at S) loses plainly to a staff's (281); a mace's 192 loses to a wand's 256. The weapon's own
numbers do the enforcing, and your 2026-08-20 ruling (*"a mace still works, simply gives less"*) stands.

## 3. Elf — FANGS, not the bow

Your pick (2026-10-05): fangs. *"If elf is left as bow it's near a nuker, while swapping to fangs it rly can be
unique."* Agreed: a fast-casting ranged wand/bow caster is a Magus with buffs.

| | Elf |
|---|---|
| weapon | fangs (`Dual`) |
| armour | light |
| basic-attack passive | the **fastest, lowest power** of the three (fang attack speed) |
| big skill | **Magic Stab**: very high damage, **fails ~60% of the time, always** (§5) |
| MP skill | Sound Burst → a **magic** attack (❓ Q1: its range) |
| toggle | **Elf Reinforcement**, gated to **light**: evasion, a smaller P.Def bonus, crit-rate resist (what the light-armour passives give today) |

- **Fang Proficiency replaces Harmonist Bow Proficiency.** Spellcaster Mastery charges an untrained weapon (bow,
  fangs, bare) ×0.5 cast, ×0.5 magic, ×25 fizzle (`Entity.cs:3976`); the bow passive cancels it for a bow with
  ×2 / ×2 / ×0.04. The fang one is the same numbers on `Dual`.
- **Taken away:** Bow Mastery, Bow Proficiency, Bow Expertise, Harmonist Bow Proficiency; Light Armor Mastery,
  Light Armor Evasion and Critical Resist move into the toggle (same pattern as §2; ❓ Q2).
- **Combo Mastery and Mana Vampirism are gated `blunt|bow`** — the bow becomes `duals`.
- **The weapon conversion you proposed for the bow** (*"weapon.m.atk+90 ... weapon.p.atk−400, or % based"*) is not
  needed on fangs, but it is cheap if it ever is: every weapon already carries a P.Atk and an M.Atk factor applied
  **before** the stat formula (`Entity.cs:3395-3401`, `ItemDef.PAtkFactor/MAtkFactor`). A toggle multiplying those
  is exactly your % form. Noted, not proposed.

## 4. Spell vamp

*"a self buff/passive spell vamp at the lvls of normal buff one"*. `SpellVamp` already exists on the caster and adds
to every spell's lifesteal; the magic swing (§1) reads it too.

⚠ **It must NOT be a `Lifesteal` field on an attack skill.** Auto-hunt files any skill with `Lifesteal > 0` as a
**heal** (`GameLoopService.cs:6253`, your 2026-08-13 ruling), so it would fire only under the HP threshold. A
self-buff or passive granting `SpellVamp` avoids that entirely.

## 5. "Always" fail — Magic Stab

*"if we can have 'always' fail rate otherwise in pvp is just stronger spell"*. Today fail is a curve:
`1.3^(targetLevel − rung's learn level)` points × modifiers, capped 95%. A "60%" skill authored on it would be ~60%
at one level gap and near the cap three levels up, and M.Accuracy / weapon modifiers would move it. So:

- **New per-skill field, a FIXED fail chance**: when set, the fizzle roll uses it and nothing else — no level term,
  no M.Accuracy, no defender modifier. A failed hit still lands ÷ 3 (the fizzle rule), so 60% fail averages
  `0.4 + 0.6/3 = 0.6` of the power: the skill must hit ~1.7× harder than a normal one just to break even. That is
  the gamble, and it is the same in PvE and PvP.
- ⚠ History: `FixedLandChance` (`BL-204`) lived one day in 2026-09 and was deleted (`BL-207`). That was a DEBUFF
  land chance and the problem it solved was fixed at the source. This is a different roll (damage fizzle) with a
  different reason, so it does not repeat that mistake — but it is a new field, so you know.

## 6. What building it costs

- **Your CSV edits** (`buffer 3rd.csv`, `buffer 4th.csv`) — the CSVs are authoritative; every number here is yours.
- Engine: the magic-swing passive (§1), the fixed fail field (§5), `SpellVamp` on the swing, the fang proficiency.
  New debuff rows default to `x1` in `debuff_landmods.csv` for you to tune in a playtest.
- BalanceMatrix before/after (the robe + toggle vs heavy question, the three swing powers).
- **A new APK** — the client builds its Learn tab from the compiled class tables.

## ❓ Open questions

1. **Elf's Sound Burst as a magic attack** — keep its 900 range and two hits, or make it melee like the others?
2. **Elf armour** — light (as written above) or robe like the other two? Light keeps the evasion identity, but robe
   is where Robe Armor Mastery and the caster bonuses live.
3. **Mana Vampirism** — its row says *"physical basic attack only"*, written when every basic attack was physical.
   Should the magic swing still drain MP? (My pick: yes — it is how the buffer refills between Sound Smashes.)
4. **Does the magic swing fizzle?** It is the magic channel's twin of the evasion miss, so by default yes (1% at
   parity, more against higher levels). Or treat it as never failing?
5. **The human's shield stun/hold** — stun or root, its duration, and its MP. It ships at `x1` in
   `debuff_landmods.csv`.
