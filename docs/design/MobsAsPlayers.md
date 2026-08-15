# Mobs built like players — `G3` / `BL-47`

**Status: this is step 1 of three, and step 1 is a DOCUMENT, not code.** Your own order, 2026-08-06
(playtest-19 `0f`): *"I want it documented and balance matrix tables. So I can make comparisons. And
later we can do 2~5 mobs so I can test."*

| # | What | Where it stands |
|---|---|---|
| 1 | The design document | **this file**, 2026-08-15 |
| 1b | The BalanceMatrix tables to compare against | **built** — `dotnet run --project tools/BalanceMatrix`, section `G3` (six sub-tables) |
| 2 | 2-5 real mobs authored this way, to fight | **not built** — specced in §7 below, waiting on you |
| 3 | Anything wholesale | **explicitly not on the table.** You never asked for a migration |

Nothing in this file changes the game. Every number in it is measured by the tool, not derived by
hand — which matters here, because the last three times mob stats were reasoned about on paper the
paper was wrong.

---

## 1. What you actually said

Playtest-18 `G3`, 2026-08-05, after a loadout counter-proposal:

> *"i dont like the mobs having 200ATK and 200CON .. it looks over inflated even if it doesnt do as
> much as players does."*

You accepted *"fix the items with passives, same logic as a player holding a weapon"* and rejected the
inflated raw stats. Then you named the rule and the five families:

> **def from armor, attack from weapon, mdef from jewels — like a normal player.**

1. **Armor weight** — ~15 levels, 7 = neutral. Heavy = more def / less evasion / same MP. Light = less
   def / more evasion. **Levels 1-5 = robe**: even less def, *same* evasion, **more MP** — so caster
   creatures land there naturally.
2. **Weapon type** — a levelled passive with **a different NAME per level**, defining the type across
   seven axes: `atk / matk / atkspeed / cast / crit / critdmg / acc`. Blade neutral; blunt more
   accuracy less crit; two-handed ≈ 1.5 P.Atk / 0.72 attack speed; magic types less P.Atk, more M.Atk,
   more cast.
3. **Jewel type** — more / less M.Def.
4. **HP type** — more / less HP (tanks, warriors, elites more).
5. **Speed** — more / less (rogue-types faster).

And before any of it, the check you asked for first:

> *"before we do and start to build i would like to check if a player vs mob-player where the player
> is a normal character with balance matrix gear and a mob-player is just an entity that works exactly
> as a normal player can be done … Will we be able to manage something like everything-is-a-player
> logic (just different equipment and skill kits)?"*

That check is section 6 of this file. **The short answer is yes, it can be done, and no, it should not
be — because the thing it would buy you is already built under a different name.**

---

## 2. The first thing to know: those numbers are already inert

A mob's ATK and CON come from `StatCalculator.MobStats(level)`:

```
Con = 15 + level*2      Atk = 8 + level*2      Wit = 5      Agi = 30      Spt = 30
```

At level 80 that is **CON 175 / ATK 168** — your "200ATK and 200CON", near enough.

**Neither number reaches a single derived stat.** `Entity.RecomputeDerived` branches on `Kind` and
sends a mob somewhere else entirely (`Entity.cs:1659-1691`):

| Stat | Player | Mob |
|---|---|---|
| Max HP | `StatCalculator.MaxHp(CON, level, class curve)` | **`MobBaseStats.Hp(level)`** — CON not consulted |
| Max MP | `StatCalculator.MaxMp(SPT, …)` | `MobBaseStats.Mp(level)` |
| P.Atk | weapon × ATK stat × levelMod | **`MobBaseStats.PAtk(level)`** — ATK not consulted |
| M.Atk | weapon × stat × levelMod² | `MobBaseStats.MAtk(level)` |
| P.Def | `PhysicalDefenceBase(level)` + armor | `MobBaseStats.PDef(level)` = `4.2·level` |
| M.Def | `MagicDefenceBase(level)` + jewels | `MobBaseStats.MDef(level)` = `3.16·level` |

The only two mob stats that do anything at all are **AGI (flat 30)** — accuracy, evasion, crit rate,
attack speed — and **WIT (5)** — cast speed and magic crit.

🔑 **So the complaint is entirely a DISPLAY problem, and the display is real.** `GameUi.Target.cs:252`
prints `Power (ATK) 168 | CON 175` on the target's Stats tab. You are being shown two large numbers
that drive nothing, sitting next to P.Atk and HP numbers that came from somewhere else. That is the
whole of what you saw, and **it is fixable in one line without touching the simulation** (§8, decision A).

⚠ **AGI 30 is load-bearing and must survive anything done here.** It is your own 2026-08-02 call:
`MobAgiReference` **is** the human-fighter base, so a same-level normal mob is a *neutral opponent*
(the 5% miss floor both ways) and every point of spread is earned by gear or a passive. The old
`10 + level` is what caused the accuracy collapse. It must never come back.

---

## 3. The world moved under this entry — four of your five families already exist

`G3` was written on 2026-08-05. Since then `MobMasteries` shipped (the `mobs_passives.csv` layer) and
0.65.0 added mob weapon types and the mRes channel. Measured against your five families today:

| # | Your family | What exists now | Gap |
|---|---|---|---|
| 1 | **Armor weight** | `MobMasteries.ArmorWeightTable` — P.Def × + a **flat evasion add**, exactly your "def for evasion" trade | ⚠ **3 rungs, not ~15**, neutral at L2 not L7, and **no robe arm at all** — nothing grants the "less def, same evasion, more MP" caster rung |
| 2 | **Weapon type** | `WeaponWeightTable` — 17 rungs of P.Atk ↔ attack speed. Plus 0.65.0: a mob holds a real `InnateWeaponType`, and `MobWeaponPowerFactor` (`433 / weaponBaseSpeed`) gives it the per-hit power a player gets free from the weapon item | **~4 of your 7 axes.** `atk`, `atkspeed`, `crit` and (via the weapon) `acc` are live; **`matk`, `cast` and `critdmg` are not bundled into the type** |
| 3 | **Jewel type (M.Def)** | `DefTable` via the `mDef` pick, 22 rungs | Built. Not *called* "jewel" — it is a raw M.Def track |
| 4 | **HP type** | `PoolTable` via `maxHp`, 21 rungs, and the same table drives Max MP and both regens | Built |
| 5 | **Speed** | ⚠ **Not a passive.** Move speed is a template field and a column in `mob_base_stats.csv` (`Run_Speed`) | The one family with no mastery track |

Read that table again before deciding anything. **Your five-family design is roughly 70% shipped**, and
it shipped as `MobMod` / `MobMasteries` — a per-mob *pick a level per track* system, tuned from a CSV,
which is precisely the shape you asked for ("a levelled passive, a different NAME per level").

🔑 **And the base curve is no longer a formula you have to fight.** `MobBaseStats` interpolates an
IG-measured table for MP/P.Atk/M.Atk, with HP/P.Def/M.Def as three lean formulas, and
`docs/data/mobs/mob_base_stats.csv` holds the authored per-creature rows. A mob's numbers are **your
data** now. Rebuilding mobs on the player pipeline would throw that reference table away and replace it
with gear that has to be authored to reproduce it.

---

## 4. What the player pipeline actually produces

`tools/BalanceMatrix` section `G3.1` builds a **`Kind=Player` mob** — a real entity through the real
`RecomputeDerived`, dressed in one-grade-down Common +0 tier gear, no rune — and divides it by today's
mob curve. `x1.00` would mean gear alone reproduces the mob.

| Lvl | archetype | HP | P.Def | M.Def | P.Atk |
|---|---|---|---|---|---|
| 20 | Warrior | x1.30 | **x1.71** | x1.29 | **x0.35** |
| 40 | Warrior | x0.94 | x1.21 | x1.34 | **x0.40** |
| 60 | Warrior | x0.86 | x1.08 | **x1.51** | **x0.25** |
| 80 | Warrior | x0.83 | x1.09 | **x1.94** | **x0.20** |

**The player pipeline is the MIRROR of the mob curve.** Armor over-delivers defence; the weapon
under-delivers attack, by 2-5×, at every level and in every grade. Today's mob is a glass cannon — high
attack, low defence — and a player-shaped entity of the same level is the exact opposite.

`G3.2` sweeps grade × quality × enchant (4 × 6 × 5) looking for a loadout that closes P.Def, M.Def and
attack together. **None exists.** The best "worst single miss" is 68% at level 20 and **185-221% at
level 80** — the higher the level, the further gear alone lands from the target. Gear cannot flip the
sign, because the sign is structural: a player's damage comes from a weapon item and a mob's comes from
an authored curve.

⚠ **Half the attack gap is just the War Rune.** A level-60 mob-player warrior has **131 P.Atk bare, 262
with a War Rune**, against the mob's authored **529**. The rune — a thing players hold and mobs do not —
closes ×2.00 of the ×4.04 needed. The rest is the weapon-type passive's job.

### The one that decides the shape

`G3.6` asks: what multiplier would each type passive have to supply, and does one number work at every
level?

| archetype | stat | L20 | L40 | L60 | L80 | spread |
|---|---|---|---|---|---|---|
| Warrior | HP | x0.77 | x1.06 | x1.16 | x1.21 | 1.58× |
| Warrior | P.Def | x0.58 | x0.83 | x0.92 | x0.92 | 1.58× |
| Warrior | M.Def | x0.78 | x0.75 | x0.66 | x0.52 | 1.51× |
| Warrior | P.Atk | x2.82 | x2.51 | x4.04 | x5.02 | **2.00×** |
| Nuker | HP | x2.01 | x2.97 | x3.32 | x3.48 | 1.73× |

**Every stat, every archetype, drifts 1.5-2.0× across the level range.** A passive cannot be one flat
number; it needs a per-band table. 🔑 That is not a blocker — **it is literally your own spec**
("15 levels", "a different NAME per level"). The measurement found the shape the design already assumed.

### Two more results worth having

**`G3.3` — a frozen loadout rots.** The same E-grade Common warrior kit, spawned up the bands:

| spawn | 20 | 30 | 40 | 52 | 61 | 76 | 85 |
|---|---|---|---|---|---|---|---|
| P.Def x | 2.27 | 1.57 | 1.21 | 0.98 | 0.88 | 0.76 | 0.73 |
| P.Atk x | 1.19 | 0.64 | 0.40 | 0.21 | 0.14 | 0.08 | **0.06** |

A level-85 spawn of that template hits at **6% of its curve**. So if "the ZONE assigns the level"
survives — and it is one of this project's load-bearing rules — then a **level→grade function is
mandatory**, not a nice-to-have. HP tracks fine, because HP comes from level, not from the frozen gear.

**`G3.4` — the fights are playable but not equivalent.** Against a geared Champion with a War Rune,
mob-player TTKs land **1.9-35.7s** against today's mob's **2.4-20.5s** — the tank end is nearly twice
as long as anything the game currently spawns, and the nuker end is under two seconds. Their damage OUT
is the weaker half: at level 80 a mob-player deals **13-37 dps** where today's mob deals **71**. Same
attack gap as `G3.1`, showing up in a fight.

---

## 5. The costs, measured rather than asserted

- **`RecomputeDerived` branches on `Kind` in 21 places** (`Entity.cs`). Most are player-only features —
  armor sets, subclass, the learned-skill passive loop, the speed override. Not blockers individually,
  but "everything is a player" means auditing all 21, not flipping one flag.
- **Player HP is exponential in CON.** This is the twin of the 0.42.3 mob-regen bug. A mob on
  `StatCalculator.MaxHp` needs its CON in player range *and* an `HpClassLevelModifier` that mobs do not
  have — which implies a **mob archetype table**: new machinery, not reuse.
- **A mob kit must be an AUTHORED list.** `ClassSkills.Cumulative` drags in masteries: giving a level-60
  warrior mob-player its 7 class skills moves P.Atk **131 → 216 (×1.65)** while P.Def does not move at
  all. A kit taken from the class table would silently re-tune the creature.
- **Every fitted number re-rolls** — TTK, zone bands, exp pace, all of BalanceMatrix.
- ✅ **Two old worries are now closed.** The swing clock no longer moves when `Kind` flips
  (`G3.5a` reads **×1.00**; it was ×1.15 when this was first measured). And the grade penalty is inert
  for lower-grade mob gear, as designed.
- ⚠ **The AGI benchmark survives for fighter archetypes by construction** — `MobAgiReference` *is* the
  human-fighter base. **The Nuker archetype falls off it** (AGI 21), which is a real, if small, change
  to the neutral-opponent rule for caster creatures.

---

## 6. What this means — the recommendation

**Do not migrate mobs onto the player pipeline. Finish the passive layer instead.**

The reasoning, in order:

1. The thing you objected to — inflated ATK/CON — **is already inert**, and its only remaining effect
   is two numbers printed on the target window. That is a display fix, not an architecture project.
2. The reconciliation the migration would need **cannot come from gear** (`G3.2`: no combination works).
   It has to come from per-band type passives — which is the `MobMasteries` system that already ships.
3. So the migration's end state and the current system's end state are **the same authored table**,
   reached either by adding rungs to a CSV that already exists, or by rebuilding the entity pipeline,
   auditing 21 branches, inventing a mob archetype table and re-rolling every balance number first.
4. And the migration would **discard the IG-measured base curve** (`MobBaseStats` + `mob_base_stats.csv`)
   — the one piece of mob data in this project that is reference material rather than invention.

What that leaves worth building is small, cheap and entirely inside the shipped system:

- **Family 1 needs its robe arm and more rungs** — 3 rungs is not 15, and there is no way today to
  author "less def, same evasion, more MP" for a caster creature.
- **Family 2 needs the missing axes bundled** — `matk`, `cast` and `critdmg` are not part of the weapon
  type the way `atk`, `atkspeed` and `crit` now are.
- **Family 5 needs a track at all** — speed is a template field, not a passive.
- **A level→grade function** if per-template loadouts are ever wanted (`G3.3`).

**But `G3.4` is the honest counter-argument and it is yours to weigh:** the mob-player fights *are*
playable, and if what you want is not stat parity but creatures that feel like characters — that hold
a real weapon and wear real armor you can see and loot — none of the above delivers that, and the
migration does. That is a design preference, not a balance result, and this document cannot decide it
for you.

---

## 7. Step 2, specced: the 2-5 mobs you asked to fight

Whatever you decide in §8, the experiment you asked for is cheap and does not commit the project to
anything. The shape that answers the most with the least:

| # | Creature | Built how | What it tests |
|---|---|---|---|
| 1 | A **level-40 warrior** humanoid | mob-player, Warrior archetype, one grade down, Common +0, authored 3-skill kit | The baseline: does a player-pipeline mob at the *easiest* band feel like a mob? |
| 2 | The **same template at level 76** | identical loadout, no level→grade rule | Proves `G3.3` in play — it should hit like paper |
| 3 | The same template at 76 with a **level→grade** loadout | grade tracked to level | Whether the level→grade function alone fixes #2 |
| 4 | A **level-60 tank** | mob-player, Tank archetype, shield | The 35.7s TTK end of `G3.4` — is it a boss-feeling fight or a boring one? |
| 5 | A **level-60 caster** | mob-player, Nuker archetype | The archetype that falls off the AGI benchmark and needs a ×3.3 HP passive |

Ship them behind a spawner in one field, next to their `Kind=Mob` equivalents so the comparison is a
walk and not a memory. **Nothing else in the world changes**, which is what keeps this an experiment.

⚠ Build them only after §8 decision B — if you want the display fix and nothing else, this experiment
is answering a question you have closed.

---

## 8. What is yours to decide

- **A. The display.** `Power (ATK)` and `CON` on a mob's target sheet are numbers that drive nothing.
  **Drop the two rows for mobs?** (One line, no simulation change, and it fully answers your original
  complaint.) Or keep them and change what they mean?
- **B. Migrate or finish the passive layer?** §6 recommends finishing the passives. The counter-case is
  in the last paragraph of §6 and it is a real one. **Nothing proceeds until you pick.**
- **C. If passives: how many rungs on Armor Weight, and what is the robe arm?** Your spec said ~15 with
  7 neutral; what ships is 3 with 2 neutral, and the robe rung does not exist.
- **D. Does the weapon TYPE carry `matk` / `cast` / `critdmg`?** Today it carries P.Atk, attack speed and
  crit. The other three are authored separately if at all.
- **E. Speed as a passive, or left as a template field?** It is the only one of your five with no track.
- **F. Do you still want the 2-5 experimental mobs (§7)?** They cost little and answer B by feel rather
  than by table — which, given `G3.4`, may be the only way B *can* be answered.

---

## 9. How to re-run any of this

```
dotnet run --project tools/BalanceMatrix        # scroll to "G3 MOB-AS-PLAYER FEASIBILITY"
```

`G3.1` per-stat ratios · `G3.2` the gear sweep · `G3.3` the frozen loadout across bands · `G3.4` TTK both
directions · `G3.5` the side effects of flipping `Kind` · `G3.6` the passive multipliers and their drift.

⚠ The verdict block at the end of `G3` prints **computed** figures, not prose. It used to restate them
as hardcoded text, and by 2026-08-15 three of those claims had drifted from the table printed directly
above them (the TTK range, the level-80 dps, and a swing-clock side effect that had since been fixed).
If you extend the section, keep the summary reading from the same variables the table does.

**Related:** `docs/design/StatMods.md` · `docs/balance/BalanceMatrix.md` (older hand audit — good
reasoning, stale numbers) · `docs/data/mobs/mobs_passives.csv` · `docs/data/mobs/mob_base_stats.csv`
