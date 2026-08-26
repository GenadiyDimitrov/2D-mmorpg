# Mobs built like players — `G3` / `BL-47`

> ## ✅ ANSWERED, 2026-08-26 — and the answer splits the work in two
>
> *"yes. Player mobs are hand crafted and field mobs stay on curve. Player mobs are player stats with
> equipped real items. Pk guards with overechsnted gear and fortress fighting npcs with undergear as
> we described."*
>
> That settles §8-B and the open question §6 was overturned into: **ordinary field creatures keep the
> `MobBaseStats` curve with `MobMod` passives** — the global lever you did not want to lose is kept —
> **and player-built creatures become a hand-placed CONTENT tool**, never the general pipeline. There
> is no migration and there will not be one.
>
> The roadmap for the built half is now `BL-79` (town/field PK guards, **over-enchanted**) and `BL-80`
> (fortress NPCs, **under-geared**), with their gear direction named in the same sentence. Everything
> measured below stands unchanged; what changes is only where it gets used.

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

`G3.2` sweeps grade × quality × enchant (4 × 6 × 5, enchant **+0…+16**) looking for a loadout that closes
P.Def, M.Def and attack together. **None exists.** The best "worst single miss" is 68% at level 20 and
**185-221% at level 80**.

🔴 **That result is real but it was OVERSTATED as a claim about gear, and he caught it (playtest 24).**
`G3.2` moves every slot together, so the one shape that fixes a mirror — an **over-enchanted weapon over
under-grade armour** — was outside its sweep by construction, and its enchant ceiling of +16 is the
*player's* practical limit, not a mob's, whose enchant is simply an authored number. **`G3.7` asks the
same question the way he posed it** and the answer flips: see §6.

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

A level-85 spawn of that template hits at **6% of its curve**.

🔴 **⚠ THIS TEST MEASURES A SCENARIO THE GAME DOES NOT RUN — corrected 2026-08-16.** It spans one template
from 20 to 85 because CLAUDE.md's rule says *"the ZONE assigns the level"*. The catalogue does not work
that way in practice, and the code says so: **`MobCatalog` holds 80 templates, each with its own natural
level, spaced ~2 levels apart**, and `GameLoopService.cs:13959` reads

```csharp
int level = mobType.Level > 0 && !zone.ForceZoneLevel ? mobType.Level
                                                      : _rng.Next(zone.MinLevel, zone.MaxLevel + 1);
```

— **a natural level beats the zone band outright**. So a template spans ±0 levels today, and ±5 under his
playtest-24 rule (*"Prefixed 100+ mobs and give them +-5 lvl ranges ... Not a lvl 1 mob scaled with lvl to
85"* — which is the model this catalogue already implements, minus the variance and 20 templates).

**Therefore the level→grade function is NOT mandatory.** One authored loadout comfortably covers a ±5 band:
between levels 40 and 45 the same E-grade kit moves from x1.21 to about x1.10 on P.Def. The `G3.3` numbers
stand as a measurement of *stretching* a loadout; they are simply not an argument against per-template
loadouts. ⚠ **The one place the game really does stretch a roster is `zone.ForceZoneLevel`** — the 85-90
field reusing the top roster — and that is exactly the *"lvl 1 mob scaled to 85"* he objects to.

HP tracks fine either way, because HP comes from level, not from the frozen gear.

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

## 6. What this means — ⚠ THE RECOMMENDATION WAS OVERTURNED (2026-08-16)

🔴 **He ruled MIGRATE, and the measurement now agrees with him.** Playtest 24, `86b`: *"u said u cannot
manage to balance a player with current mobs curve ... human fighter with S grade Mace enchanted to +60
... and B grade leather only have the same pDef and twice less p atk ... if we make the elite passive x2
p atk and hp boost we can make him the same values."* `G3.7` was written to test exactly that loadout —
weapon and armour swept **independently**, weapon enchant to **+60** — and scores each result against his
own bar: *does what remains fit inside a ×2 passive?*

| Lvl | archetype | best armour | best weapon | P.Def | M.Def | atk | passive still needed (pd/md/atk/hp) | fits ×2 |
|---|---|---|---|---|---|---|---|---|
| 20 | Warrior | t1 Common | t20 Common | x1.71 | x1.29 | x1.19 | x0.58 / x0.78 / x0.84 / x0.77 | **YES** |
| 40 | Warrior | t1 Uncommon | t40 Rare | x1.04 | x0.99 | x1.02 | x0.97 / x1.01 / x0.98 / x1.06 | **YES** |
| 60 | Warrior | t1 Epic | t52 Common **+30** | x0.88 | x1.10 | x0.98 | x1.14 / x0.91 / x1.02 / x1.15 | **YES** |
| 80 | Warrior | t52 Common | t80 Epic **+16** | x0.95 | x1.65 | x0.64 | x1.05 / x0.60 / x1.55 / x1.21 | **YES** |
| 80 | Nuker | t52 Common | t80 Epic | x0.77 | x1.94 | x0.64 | x1.30 / x0.52 / x1.57 / **x3.48** | no |

**12 of 16 archetype-levels land inside a ×2 passive on all four stats at once**, the worst single miss
falls from 185-221% to **94%**, and the biggest attack passive still needed anywhere is **×1.60** — well
inside the ×2 he proposed. 🔑 **And the optimiser independently picked his loadout**: the best armour is
almost always the *lowest* tier available and the weapon is at or near level tier, several rungs of
enchant on top. That is *"S grade Mace + B grade leather"*, found by search rather than by assumption.

⚠ **The four failures are all the same failure — the Nuker's HP** (×2.01 → ×3.48 as level rises). Nothing
else misses: every P.Def, M.Def and attack figure in the table is inside ×2 at every level. A caster
creature needs an HP passive bigger than ×2, which §6 of the *old* text already said and which his own
*"and hp boost"* anticipated.

🔵 **The next lever, not yet swept:** `G3.7` still dresses all nine slots. At level 80 the binding
constraint is **M.Def over-delivering at ×1.65**, and the obvious fix is that a creature need not wear
jewels at all. Letting the sweep drop slots would tighten the 80 row further; it does not change the
verdict, so it was left for whoever needs it.

---

### The superseded recommendation, kept because it explains the code

**Do not migrate mobs onto the player pipeline. Finish the passive layer instead.**

⚠ Reasons 2 and 3 below are the ones `G3.7` overturned. **1 and 4 survive his ruling intact** and are
still true: the display complaint was inert, and the migration does discard the IG-measured curve unless
that curve is deliberately kept as the *target* the gear+passives reproduce — which is now the plan.

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

## 7. Step 2 — ✅ BUILT 2026-08-16, and the two comparisons are already answered

The five creatures you asked for (*"and later we can do 2~5 mobs so I can test"*) are in the game, in
the **Proving Grounds** — a gatekeeper destination on the row south of the training dummies. Each one
stands beside its **curve twin**: an ordinary `MobBaseStats` creature of the same level, no passives,
holding the same weapon, so the comparison is a walk and not a memory. Nothing is aggressive, nothing
drops loot, and **nothing else in the world changes** — the templates are fenced out of the generated
rosters (`MobCatalog.HandPlaced`).

| # | Creature | Lv | Race lean (±5) | Loadout | Passive it carries |
|---|---|---|---|---|---|
| 1 | **Goblin Raider** | 40 | +5 CON / +5 ATK / −5 AGI | t40 Rare sword2h over t1 Uncommon heavy | **none** |
| 2 | **Goblin Elder Raider** | 45 | same | **identical to #1** | **none** |
| 3 | **Cairn Lich** | 60 | −5 CON / +5 WIT | t52 Common staff **+30** over t1 Epic robe | HP ×3.73, P.Def ×1.02, M.Def ×0.78, M.Atk ×0.97 |
| 4 | **Fallen Seraph** | 80 | +5 AGI / −5 CON | t80 Epic sword2h **+16** over t52 Common heavy | HP ×1.46, P.Def ×1.05, M.Def ×0.61, **P.Atk ×2.07** |
| 5 | **Fallen Seraph, Runebearer** | 80 | same | **identical to #4**, plus a held **War Rune** | HP ×1.46, P.Def ×1.05, M.Def ×0.61, **no attack passive** |

### What it measures — `BalanceMatrix` `G3.8`, demo ÷ its twin

| creature | HP | P.Atk | P.Def | M.Def | M.Atk |
|---|---|---|---|---|---|
| Goblin Raider 40 | x1.10 | **x0.87** | x1.04 | x0.99 | x1.04 |
| Goblin Elder Raider 45 | x1.06 | **x0.64** | x0.95 | x0.96 | x0.79 |
| Cairn Lich 60 | x1.00 | — | x1.00 | x1.00 | x1.00 |
| Fallen Seraph 80 | x1.01 | **x1.00** | x1.00 | x1.01 | x0.72 |
| Seraph Runebearer 80 | x1.01 | **x0.97** | x1.00 | x1.01 | x0.72 |

🔴 **#1 vs #2 — one loadout covers a ±5 band on everything EXCEPT how hard it hits.** Defence and HP
barely move across five levels (P.Def x1.04 → x0.95, HP x1.10 → x1.06); **P.Atk falls x0.87 → x0.64**,
because the mob attack curve is the steep one. So your *"prefixed 100+ mobs and give them +-5 lvl
ranges"* works, and the one number that has to move within a band is attack — one enchant rung, or the
band's own attack passive. Both goblins are left deliberately BARE so the drift is visible; tuning it
flat would have answered your question with a guess.

🔴 **#4 vs #5 — YES, THE RUNE REPLACES THE PASSIVE.** Bare, that level-80 build reads **x0.48** of its
curve's P.Atk. The authored passive gets it to x1.00 and costs a per-creature, per-band number. The
**held War Rune gets it to x0.97 and costs an item**. That is your B3 answer measured rather than
argued, and it is the cheapest lever in this document: no table, no drift, and a creature that visibly
carries the thing that makes it dangerous.

⚠ **One number came out past your ×2, and the reason is worth keeping.** `G3.7` said the level-80
attack passive needed **×1.55**; the creature actually needs **×2.07**. `G3.7` measures against the bare
`MobBaseStats` curve, but the creature standing next to it *also* carries **BL-14's weapon power factor**
— a slow 2H weapon buys per-hit damage. Against what really spawns, the gap is bigger than the search
said. Nobody was wrong; the two things were measured against different targets, and `G3.8` is the one
that measures against the game.

⚠ **The races are placeholders.** You said *"make a demo then we do a system number"* — Goblin, Lich
and Angel are three names to hang a ±5 lean on, not a proposed bestiary.

### How it is built, in one paragraph

`MobType.Build` (a `MobBuild`) carries the class, the ±5 lean, the split loadout and any held item.
At spawn `Entity.ApplyMobBuild` gives the creature its identity and puts the gear ON it, and
`Entity.PlayerBuilt` swings the **six stat bases** — MaxHp, MaxMp, P.Atk, M.Atk, P.Def, M.Def — plus the
weapon P.Atk fold and the two magic level terms onto the player side of `RecomputeDerived`. **That is
all it moves.** It is still a Mob to aggro, drops, targeting, the client's plate, PvP and party; it
deliberately does not take armor SETS, the learned-passive loop, armor-weight masteries, the race+class
speed override (so it can still be kited) or the grade penalty. Rank and `MobMod` still land on top in
`ApplyMobScale` — which is the design: **gear gets you most of the way, the passive carries the
remainder.** Two things are switched off there for a player-built creature, both because they would pay
twice: BL-14's weapon power factor (it is holding the real weapon) and the ROLE's stat lean (a robe, a
staff and a mage class curve already make it a caster).

---

## 8. What is yours to decide

- ✅ **A. The display.** ~~Drop `Power (ATK)` and `CON` from a mob's target sheet?~~ **Done 2026-08-16**
  (`86c`, and you passed it `[x]`). SPT went with them; a mob's Attributes block is AGI + WIT only.
- ✅ **B. Migrate or finish the passive layer?** **You ruled MIGRATE** (playtest 24, `86b`), and `G3.7`
  vindicates the ruling — see §6. The doc's recommendation is superseded and kept there for the record.
- ✅ **B1. One stat block, no level curve — a flat ±5.** *"No race is like ork and elf — ork have higher
  con/atk less agi ..while elf have higher agi less atk/con. No lvl curve. Can go +-5 same as the swap
  passives etc."* ⚠ **This makes race FLAVOUR, not the reconciliation.** ±5 on a ~40-point stat is ±12.5%,
  against passive needs of ×1.5-2.0 — so a lich differs from a goblin through its **kit, gear and
  passives**, with race a light lean on top. The **passive band absorbs the `G3.6` drift**, as `G3.6`
  itself predicted.
- ✅ **B2. A demo first, the number after.** *"Make a demo then we do a system number."*
- ✅ **B3. Yes — a mob may hold an inventory.** *"If mob is a player it can have inventory (not a dropped
  one..but just to hold stuff) ... we can mix and match."* ⚠ **Held, never looted.** That admits the
  **War Rune**, which is ×2.00 of the ×4.04 attack gap at level 60 — the cheapest lever in this document.
- ✅ **B4. Balance against NORMAL mobs; elite and boss scale on top.** *"a elite and bosses will scale with
  passives out of them ... I just made the comparison with one elite mob."* 🔑 **For the record, since you
  could not remember it: Elite = HP ×4 / ATK ×1.5, Boss = HP ×100 / ATK ×10.** Neither the ×2 nor the
  ×10 you guessed, and your *"twice less p atk"* reading was against that ×1.5.
  ⚠ **THOSE NUMBERS MOVED IN 0.89.0 (`BL-13`)** and they no longer live in `GameLoopService` at all:
  they are `Game.Shared/MobRankScale.cs` now — boss HP is a **curve** (`43000 / L^1.49`, ×495 at level
  20 down to ×57 at 85), boss ATK is **×4**, and a rank finally carries **defence** (elite ×1.33, boss
  ×2.0). Everything below still holds: the comparison is against the NORMAL curve either way.
  ✅ **`G3.1`-`G3.7` already measure against `MobBaseStats`, the NORMAL curve** — rank multipliers are
  applied at spawn — so every number in this document already obeys the instruction.
  ⏳ Your *"scale with passives out of them"* implies those hardcoded rank multipliers should become
  passives. Same numbers either way; noted, not built.
- **C. How many rungs on Armor Weight, and what is the robe arm?** Your spec said ~15 with 7 neutral;
  what ships is 3 with 2 neutral, and the robe rung does not exist. **Still live** — the migration needs
  the passive layer *more*, not less, since `G3.7` leaves ×1.55-1.60 of attack for it to carry at 80.
- **D. Does the weapon TYPE carry `matk` / `cast` / `critdmg`?** Today it carries P.Atk, attack speed and
  crit. The other three are authored separately if at all.
- **E. Speed as a passive, or left as a template field?** It is the only one of your five with no track.
- ✅ **F. The 2-5 experimental mobs are BUILT** (2026-08-16) — see §7 for what they are, where they are
  and what they already measured. What is left is yours: **fight them**. The numbers say the system
  works; only you can say whether a ×3.7 HP lich reads as a fair caster or as a sponge, and whether a
  goblin at the bottom of its ±5 band hits softly enough to notice.

---

## 9. How to re-run any of this

```
dotnet run --project tools/BalanceMatrix        # scroll to "G3 MOB-AS-PLAYER FEASIBILITY"
```

`G3.1` per-stat ratios · `G3.2` the gear sweep · `G3.3` the frozen loadout across bands · `G3.4` TTK both
directions · `G3.5` the side effects of flipping `Kind` · `G3.6` the passive multipliers and their drift ·
🆕 **`G3.8` THE DEMO — the five authored creatures spawned the way the server spawns them, each divided
by its curve twin.** It builds them through `Entity.ApplyMobBuild`, the same method `BuildMob` calls, so
the measured creature and the spawned one can never drift apart. **Re-run it after touching a demo
template**: those `MobMod` numbers are fitted, not derived. ·
🆕 **`G3.7` HIS loadout — weapon and armour swept SEPARATELY, enchant to +60, scored against his ×2
passive bar.** ⚠ `G3.2` was deliberately **left untouched** when `G3.7` was added, so its old reading
stays attributable; `G3.7` is the same question asked without `G3.2`'s two blind spots.

⚠ The verdict block at the end of `G3` prints **computed** figures, not prose. It used to restate them
as hardcoded text, and by 2026-08-15 three of those claims had drifted from the table printed directly
above them (the TTK range, the level-80 dps, and a swing-clock side effect that had since been fixed).
If you extend the section, keep the summary reading from the same variables the table does.

**Related:** `docs/design/StatMods.md` · `docs/balance/BalanceMatrix.md` (older hand audit — good
reasoning, stale numbers) · `docs/data/mobs/mobs_passives.csv` · `docs/data/mobs/mob_base_stats.csv`
