# Magic-melee buffers — the Warchanter hits with MAGIC

**Status: 🔵 DESIGN ONLY (2026-10-05), `BL-335`.** Nothing built. Your words: *"Just design for now."*
Your answers of 2026-10-05 are folded in; what is still open is at the bottom.

The problem it solves, in yours: *"we have warriors and tanks and I want to give them something unique"*. Today
every Warchanter is a physical fighter (demon maul + heavy, human mace + shield + heavy, elf bow + light), which
is a warrior/tank/archer with buffs. After this, all three wear a **robe** and deal **magic** damage: their skills
cannot be blocked or evaded (a spell can **fail** instead, landing damage ÷ 3), and they crit ×3 off WIT rather than
×10 off DEX. The races keep their current shape (demon heavy hitter, human shield, elf fast and evasive), just
turned to magic.

Nothing here breaks *"a class grants no stats"*: every change is a skill or passive.

---

## 1. The core piece: a passive that turns the BASIC ATTACK into a magic hit

Your call (2026-10-05): *"make the 0mp spell a passive that swaps the basic attack action to a magic dmg one ...
then no need for cast speed and reuse because attack speed will measure them"*.

Why it is the right shape:
- **No cast means nothing to interrupt.** A caster standing in melee is hit constantly, and any hit can cancel a
  cast (`TryInterruptCast`, chance ∝ damage ÷ max HP). A basic swing is not a cast.
- **No tapping and nothing new for auto-hunt.** The attack action, auto-attack and the autopilot already drive the
  basic attack; they keep doing so.
- **The weapon prices itself.** Attack speed comes from the weapon, damage from its M.Atk.

**What the swing becomes** (`GameLoopService.ResolveBasicSwing` is the one place; it already serves cleave and the
failed blow):

| part of a basic swing   | today (physical)                 | with the passive (magic)                                    |
| ----------------------- | -------------------------------- | ----------------------------------------------------------- |
| formula                 | `77·pAtk / pDef` (power 0)       | `91·power·√mAtk / mDef` — **needs a POWER**, see below       |
| can it be avoided       | accuracy vs evasion → Miss       | **unchanged: accuracy vs evasion → Miss** (your ruling, below) |
| crit                    | DEX rate, ×10 cap, flat crit dmg | **magic crit**: WIT rate (cap 20%), ×3                       |
| block                   | shield block roll                | **none** — magic is never blocked                            |
| rune (`FinalizeDamage`) | War Rune                         | **Spell Rune**                                              |
| HP vamp                 | `MeleeVamp`                      | **`MeleeVamp`, unchanged** (§4)                              |
| MP vamp (Mana Vampirism)| on a landed swing                | **unchanged** — your ruling: it works on the magic swing     |
| timing                  | attack speed                     | attack speed (unchanged)                                    |
| Combo Mastery proc, Focus, reflect, cancel power | on a landed swing | unchanged                                 |

- 🔑 **The magic formula multiplies by `power`, so a basic hit at power 0 does 0.** The passive must carry a power
  per rung: the same ladder shape every other passive has, and the one number per level you balance with. The three
  races differ here (demon slow + heavy, human fast + light, elf fastest + lowest).
- 🔑 **The swing keeps the physical miss roll, not the fizzle** (your answer, 2026-10-05: *"We leave the fail chance
  as normal attack have acc vs evasion"*). So the swing can be EVADED (a miss deals 0), but it can never fizzle.
  The magic SKILLS keep the ordinary fizzle.

## 2. The three races

|                      | Demon                                    | Human                                       | Elf                                              |
| -------------------- | ---------------------------------------- | ------------------------------------------- | ------------------------------------------------ |
| weapon               | staff (2H magic blunt)                   | wand + shield                               | **fangs** (`Dual`)                               |
| armour               | robe                                     | robe                                        | **robe**                                         |
| magic swing (§1)     | slower, heavier                          | faster, lighter                             | fastest, lowest                                  |
| MP skill             | Sound Smash → melee magic damage         | Sound Smash → melee magic damage            | **Sound Burst → Magic Stab** (§5)                |
| second skill         | Acoustic Shock → magic damage + contested stun | **new**: shield-gated stun or hold, no damage (§6) | —                                    |
| toggle (Reinforcement, robe-gated) | P.Def + ❓ (HP / regen?)    | **higher P.Def + crit-DAMAGE resist**       | **evasion + crit-RATE resist + a smaller P.Def** |

**Reinforcement becomes each race's armour toggle**, gated to **robe** (your answer, 2026-10-05): it carries what
that race's armour passives gave, at a **lower MP/s** than today. Your target is *robe + Reinforcement == heavy*. The
armour gate already exists (`WEIGHT` column), so the gate is data.
- ⚠ **Three different number sets are three SKILLS, not three faces.** `BL-327`'s rule: a face changes the name and
  description, never the numbers. So it is `reinforcement` for one race plus two new ids (or three new ids with one
  shared display name if you want them all to read "Reinforcement").
- ⚠ The robe mastery's caster bonuses ride ON TOP of the toggle, so the MP/s is the only price; set it with
  BalanceMatrix, not by hand.
- ❓ **The demon's third stat.** His old identity bonus was accuracy (Hit Rate Mastery, removed below); you suggested
  *"maybe just hp+regen or something"*. Open.

**Taken away — Human and Demon:** Heavy Armor Mastery, Weapon Mastery, Critical Damage Resist (heavy-gated), Hit Rate
Mastery (demon), and **Sharpening** (*"would change depending on need - for now dropped for them"*). The human's
3rd-tier Shield Mastery is gated `heavy/shield`; it moves to `robe/shield`, which the 4th tier's
`buffer_shield_mastery` already is.

**Taken away — Elf:** Light Armor Mastery, Light Armor Evasion, Critical Resist (their effects move into his toggle),
Bow Mastery, Bow Proficiency, Bow Expertise, Harmonist Bow Proficiency.

**Kept — all three:** Robe Armor Mastery + Spellcaster Weapon Mastery.

**Elf weapon notes:**
- **Fang Proficiency replaces Harmonist Bow Proficiency.** Spellcaster Mastery charges an untrained weapon (bow,
  fangs, bare) ×0.5 cast, ×0.5 magic, ×25 fizzle (`Entity.cs:3976`); the bow passive cancels it with ×2 / ×2 /
  ×0.04. The fang one is the same numbers on `Dual`.
- **Combo Mastery and Mana Vampirism are gated `blunt|bow`** — the bow becomes `duals`.
- Your bow weapon-conversion idea (*"weapon.m.atk+90 ... weapon.p.atk−400, or % based"*) is not needed on fangs, but
  it is cheap if it ever is: every weapon already carries a P.Atk and an M.Atk factor applied **before** the stat
  formula (`Entity.cs:3395-3401`). A toggle multiplying those is exactly your % form. Noted, not proposed.

**No magic-weapon gate is needed.** Once heavy pays nothing and every hit is magic, a maul's extra P.Atk is unused and
its M.Atk (192 at S) loses plainly to a staff's (281); a mace's 192 loses to a wand's 256. The weapon's own numbers
do the enforcing, and your 2026-08-20 ruling (*"a mace still works, simply gives less"*) stands.

## 4. Vampirism — the swing drains, the skills do not, with NO new flag

Your worry (2026-10-05): *"mana vamp should work on magic basic attacks — problem is that the magic dmg spells
acoustic shock smash etc will vamp as well .. If we can have a cantVamp flag"*.

**They will not, and no flag is needed** — the engine already keeps the two apart:

| field         | where it is paid                                     | so…                                                 |
| ------------- | ---------------------------------------------------- | --------------------------------------------------- |
| `ManaVamp`    | **only** in `ResolveBasicSwing` (*"a skill never drains"*) | the magic swing drains MP; Sound Smash never does |
| `MeleeVamp`   | **only** in `ResolveBasicSwing`                      | the magic swing heals HP; skills never do          |
| `SpellVamp`   | every damage **skill** (`+ def.Lifesteal`)           | ⚠ don't give these classes this one                |

So the HP vamp you asked for (*"a self buff/passive spell vamp at the lvls of normal buff one"*) is granted as
**`MeleeVamp`** (basic-attack vamp), not `SpellVamp`. The swing reads it because it is still the basic attack, and
Sound Smash / Acoustic Shock / Magic Stab get nothing. (⚠ `MeleeVamp` skips a BOW — irrelevant now, none of the three
holds one.)

⚠ And never as a `Lifesteal` field on a skill: auto-hunt files any skill with `Lifesteal > 0` as a **heal**
(`GameLoopService.cs:6253`).

## 5. Magic Stab — the elf's Sound Burst, with an "always" fail

*"Sound burst is the new mele spell stab one"* and *"if we can have 'always' fail rate otherwise in pvp is just
stronger spell"*. Sound Burst stops being a 900-range bow skill and becomes the **melee magic stab**: very high
damage, failing ~60% of the time.

Today fail is a curve: `1.3^(targetLevel − rung's learn level)` points × modifiers, capped 95%. A "60%" skill on it
would be ~60% at one level gap and near the cap three levels up. So:
- **New per-skill field, a FIXED fail chance**: when set, the fizzle roll uses it and nothing else — no level term,
  no M.Accuracy, no defender modifier. Same in PvE and PvP.
- A failed hit still lands ÷ 3 (the fizzle rule), so 60% fail averages `0.4 + 0.6/3 = 0.6` of the power: it must hit
  ~1.7× harder than a normal skill just to break even. That is the gamble.
- ❓ **One hit, not two.** Sound Burst hits TWICE today, each hit rolling on its own. Two independent 60% rolls
  smooth the gamble out (both fail only 36% of the time), which is the opposite of what a stab is for. My pick: one
  hit at double the power.
- ⚠ History: `FixedLandChance` (`BL-204`) lived one day and was deleted (`BL-207`). That was a DEBUFF land chance; this
  is the damage fizzle, a different roll with a different reason — but it is a new field, so you know.

## 6. The human's shield skill

Your sketch (2026-10-05, not decided): *"~6s duration no dmg and mp cost 5~6s cd or something"*. Stun or hold, shield
required, no damage, contested like every CC, and it ships at `x1` in `debuff_landmods.csv`.

🔴 **A 5-6s cooldown on a 6s lock is a PERMANENT lock.** The game has **no CC immunity window and no diminishing
returns** — the only shortening is the defender's CON/SPT (×0.70-×1.00, `BL-156`). The human could re-apply before
the last one ends, forever, on any target that fails the contest; in PvP that is a player who never moves again.
Either the cooldown is well above the duration (e.g. 6s lock / 15s+ cooldown), or this skill is the reason to
build an immunity window. My pick: the longer cooldown — it is data, the window is a system.

## 7. What building it costs

- **Your CSV edits** (`buffer 3rd.csv`, `buffer 4th.csv`) — the CSVs are authoritative; every number here is yours.
- Engine: the magic-swing passive (§1), the fixed fail field (§5), the fang proficiency, three Reinforcement ids.
- BalanceMatrix before/after (robe + toggle vs heavy, the three swing powers).
- **A new APK** — the client builds its Learn tab from the compiled class tables.

## ✅ Answered (2026-10-05)

1. Sound Burst → the elf's melee **Magic Stab** (§5).
2. Elf armour → **robe**, toggle = evasion + crit-rate resist + small P.Def; human = higher P.Def + crit-damage
   resist; demon = P.Def + something (§2).
3. Mana Vampirism → **yes** on the magic swing; skills must not drain — already true, no flag (§4).
4. The swing → **accuracy vs evasion**, no fizzle (§1).
5. Human shield skill → ~6s, no damage, MP, short cooldown — **not final** (§6).

## ❓ Still open

1. **Demon toggle's third stat** — HP, regen, or something else (§2)?
2. **Magic Stab: one hit or two** — my pick one (§5).
3. **Human shield skill** — stun or hold, and its cooldown against the permanent-lock problem (§6).
