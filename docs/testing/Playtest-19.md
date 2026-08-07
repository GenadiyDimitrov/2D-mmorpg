# Playtest-19 — the 0.48.0 pass (owner, 2026-08-06)

**Source: his answered [Open-Checklist-0.48.0.md](Open-Checklist-0.48.0.md)**, transcribed here as the
AUTHORITATIVE queue, same convention as [Playtest-17.md](Playtest-17.md) / [Playtest-18.md](Playtest-18.md):
his wording verbatim in the quote block, my reading on the line above it. Ids are mine —
**M**y Finds (his own heading) — and the checklist ids stay as he answered them.

**The verdict in one line: five builds' worth of unplayed work went through in one pass and it held.**
0.46.0, 0.47.0 and 0.48.0 — the blocking defects, inventory hygiene, the quest section, the friction
tier, the text-box fix, x1 rates and the whole buff economy — are **played and green**, plus every
carried-forward item from §37/36/34/33/32 that was still blank. **Four defects only** (48g, 46d, 46m,
plus the 0.45.0 tick crash), and the rest of the file is *design*: rulings on the open decisions, a
combat-identity rework for the rogue, and a tutorial-quest spec.

---

## The six blocking decisions — five answered

**0a. `evade_mastery`, `precision`, `anti_magic` all STAY.** The G1 correction landed
([Skills-Not-In-CSVs.md](Skills-Not-In-CSVs.md) §3): they are auto-granted, not learned, which is why
they were missing from his CSVs.
> so `evade_mastery` I need - I give a change to it though in My Finds
> leave the precision and the anti magic

So the deletion is **only** the genuinely dead set: `reflexes`, `archer_armor_mastery`,
`archer_weapon_mastery`, `dispel_magic`, and the **Heavy Draw @24 grant** (see M7 — he wants it gone
above 40 too). `evade_mastery` stays but is **rewritten** by M9. ✅ **`class_balance_*` ruled 2026-08-07: "class_balance should be commented for now"** — the 8 defs and
their auto-grant come out of the live path but stay in the file. **Commented, NOT deleted.**

**0b. 🔴 The God layer goes — ALL of it.** Wider than I proposed.
> I want them deleted. Nothing that can't be acquired in game. If I need cosmic stats I can /enchant
> 9999999 and do /speed

`Race.God`, `ItemRarity.God`, `god_judgment`, `god_robes`, `hp_boost`, `greater_heal` and the God learn
table. **The rule underneath it is the interesting part: nothing exists in the game that cannot be
acquired in the game.** The debug rig is replaced by `/enchant <value>` (built in 0.49.0) plus `/speed`
— so those two commands are now load-bearing and must not regress. ⚠ Sweep the admin/debug menu for
anything that hands out God gear before deleting the ids, or the menu breaks.

**0c. Keep all SIX Dash rungs.** > keep them we will se when to drop

**0d. Sprint level 2 at 40 is right.** Nothing to change.

**0e. `lb_*` / `wc_*` — UNANSWERED.** Left blank. My recommendation stands (keep: one commented line
away from being learnable when the 40+ CSVs land). **Still owed back to him.**

**0f. G3 (mobs built like players) — document it, don't build it yet.**
> I want it documented and balance matrix tables. So I can make comparisons. And later we can do 2~5
> mobs so I can test

The order is: (1) a design doc, (2) `tools/BalanceMatrix` tables putting the mob curve and the
player-pipeline mob side by side per band, (3) **2-5 real mobs** built that way as a live experiment.
Not a wholesale migration off `MobBaseStats`. See [mob-as-player-design] in memory — the measurement
is already run and says type passives per band have to carry it.

---

## My Finds

**M1. ❓ Accuracy vs evasion — NOT A BUG, it is the ±20 lockout. Needs his ruling.**
> as admin i made my AS to 9999 - wih a bow I try to hit lvl 20/40/80 dummies
> - L20 vs L20-Dummy - I hit almost every time - the 5% evasion floor
> - L20 vs L40-Dummy - Didnt Hit once - where is the 5% evasion celing (the 5% hit floor)?
> - L20 vs L40/80-Dummy - With L1-`precision` passive the 10% hit floor - still miss - no hits
> - L40 vs L60/80-Dummy - With L2-`precision` passive the 20% hit floor - still miss - no hits

`StatCalculator.LevelGap` is piecewise and returns **1.0 at |Δ| ≥ 20 — a hard 100% lockout**, and
`ResolveAvoidChance` applies the level gap **after** the class floors precisely so that it overrides
them (documented in `docs/design/CombatResolution.md`: *"Precedence: Immunity > SureHit > level gap >
class floors > stat roll"*). Every case he lists is a gap of exactly 20 or more, so no accuracy number
and no `precision` rung can ever land a hit — only a `SureHit` skill can. That is the design working.

### 🔴 M1 — HIS RULING, 2026-08-06: the floors win, the lockout goes
> the 20 gap bears no drop not exp. No need for you to try at all at killing +20lvl mob. But having a
> floor/ceiling must be active at all times. Lvl 20 dagger in a 90 field must be missed 10% (cuz of
> floor). A lvl 20 warrior in a 90 field must hit 10% of the time cuz of floor … they will die anyways.
> and the exp/drop penalty gets them nowhere. So a floor and a ceiling means active all the time.

**He is right, and the code backs him harder than he put it:** `ExpCurve.LevelGapMultiplier` pays
**zero exp AND zero drops from a 13-level gap** (`GapZero = 13`), symmetric, deliberately stricter than
L2's. So the anti-powerlevel job is already done seven levels *before* the lockout even starts. The
lockout adds no protection and only produces the thing that read as broken to him — swinging forever
and never connecting.

**The change:** in `StatCalculator.ResolveAvoidChance`, swap steps 2 and 3 — apply the level gap
**first**, then clamp into `[max(0.05, defenderFloor), min(0.95, 1 − attackerHitFloor)]` **last**, so the
band and the class floors are active at every gap. `LevelGap()` itself is untouched; `G = 1.0` stops
meaning "lockout" and starts meaning "pinned to the edge of the band". Precedence becomes
`Immunity > SureHit > floors + the 5/95 band > level gap > stat roll`.
Delivered behaviour, exactly as he specified it:
- level-20 rogue in a level-90 field: dodges **10%** (his `evade_mastery` floor) instead of 0%.
- level-20 warrior with Precision L1: lands **10%** instead of never.
- no floor at all: still **5%** each way, the universal band.

⚠ **The consequence to accept: nothing is unhittable any more.** A level-1 character connects with a
raid boss 5% of the time. He has already priced that in — no exp, no drop, and he dies.

`docs/design/CombatResolution.md` is updated (the resolver block, the precedence line, and the
"floor erosion by level gap" paragraph, which is now deleted). Also worth doing with it: the client
should say *why* — "far above your level" — instead of a silent miss.

**M2. 🟡 Chat/social filtering commands + an Options window.**
> whispers towards yourself /block - whitouth a name blocks all.
> /block Name blockers the Name. - by block all I mean block all players messages in chat.
> /block-w block only whispers,/block-g global
> So a normal player or an admin will be able to limit their chat spam.
> /decline-t - declines trade,/decline-p - party
> those can be an options in the options window (that we don't have)

`/block` (all player chat) · `/block <name>` · `/block-w` (whispers only) · `/block-g` (global only) ·
`/decline-t` · `/decline-p`. This is **B11** on the not-built list, now specified. ⚠ The existing rule
stands: **an admin/moderator must not be blockable.** All six want to be toggles in an **Options
window**, which does not exist yet — that window is the real deliverable and `/decline-*` belongs in it
more than in chat.

**M3. 🔴 A server crash in the tick loop — still live in 0.48.0.**
> fail: Game.Server.Simulation.GameLoopService[0] Unhandled error in game tick
> System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
> at GameLoopService.Simulate() … GameLoopService.cs:line 5372

Line 5372 at `3dc092a` (0.45.0) is `foreach (var entity in _world.Entities.Values)` — the main entity
sweep in `Simulate()`. Something inside the loop body adds to or removes from `_world.Entities` on the
same tick (a death/despawn, a spawn, a teleport). **The same raw `foreach` is still there today**
(`GameLoopService.cs:5665`, and a second at `:895`). Fix: iterate a snapshot, or defer structural
changes to an after-loop drain. Rare, but it takes the whole tick down when it fires.

### Dead characters

**M4. 🔴 A dead character is not properly dead.**
> - Can move on the client side (gets rubberbanded back) - for others dont look like its moveing
> - Cannot be invited in party
> - Cannot be traded

Three separate calls, and only the first is unambiguously a bug:
- **Movement:** the client must refuse input while dead (the server already rubber-bands, which is the
  safety net, not the fix). Clear defect.
- **Party invite:** in L2 you *can* invite a dead player. My read: **should work** — being dead is
  exactly when you want to be pulled into a party for a res.
- **Trade:** should stay refused — but with a stated reason, not a silent nothing.

**M5. 🟡 The tutorial quest chain — "Welcome To The `<Game>` World".** A 15-step chain whose *point* is
meeting every NPC in town; each step names the NPC and says what they are for. Full text is in his
checklist under 0.49.0 and is reproduced verbatim in memory — the shape:

| step | who / what |
|---|---|
| 1 | Gatekeeper **Pell** — free teleports until 40 |
| 2 | kill 5 pigs → level 3 |
| 3 | Huntmaster **Cera** — repeatable hunt contracts (take one) |
| 4 | kill 5 foxes → level 6 |
| 5 | Spirit Helper **Nyra** — support magic 6-75 (take the buff) |
| 6 | Apothecary **Miren** — potions/scrolls **+ the free daily Rune** (take it) |
| 7 | kill X goblin riders → level 10 |
| 8 | Armsmaster **Dolan** — **the Newbie equipment** as the reward |
| 9-10 | reach 15 → back to Dolan for the 1-day rune + jewel box |
| 11-15 | reach 18 → Elder **Marius** (1st class quest) → 19 → High Priest **Oren** (2nd) → 20 → Class Master **Vael** |

Completion: the profession, plus **x1 Ultimate Scroll of Escape, x1 Ultimate Scroll of Resurrection,
x5 Mythic Dash Potion, x5 Instant Health Potion — all untradable/unsellable.**
> The 3 class quests can be taken withouth the chain / the chain is only to meet the NPCs / U just can
> lvl up to 20 go do the 3 quests and done.. / The chain is for the newebie equipment an the end reward

⚠ So the chain must **wrap** the three existing class quests without gating them, and the daily rune and
Huntmaster contracts are ordinary standalone quests that the chain merely points at. Filed as C13's
replacement (that entry said "newbie quest band 10-35" and this supersedes it).

**M6. 🔴 The newbie equipment is bound and TIMED.**
> I want the newbie equipment to be unsellable and untradable and timelimited for 30d (can be
> destroied) - from the dolans quest

This is **C2** and it now has a source: the Dolan step of M5. Destroyable, but not sellable, not
tradable, 30 days. Pairs with **C3** (timed items show remaining time, colour-graded).

**M7. 🔴 Heavy Draw is STILL granted to a rogue at 24 — and he wants it gone above 40 too.**
> I contnue to get `Heavy Draw` on a rogue 24lvl - remove it - remove it from after 40lvl as well -
> rogue leave onyl the evasion mastery to the mele discpilines after 40 .. the archer sohuld not have
> evasion mastery after 40 .. the 10% are ok

Expected — G1 deleted **nothing** (the list was wrong), so the @24 grant survived. New this round is the
40+ half: **after 40, the melee rogue disciplines keep only Evasion Mastery; the archer discipline gets
none** (the archer keeps its base 10% floor and no more). ⚠ Never delete the `power_shot` *definition* —
three level-40 discipline skills are renames of it.

**M8. 🔴 `Can Crit` / `Can Double` must be exclusive — a skill does only what it says.**
> If a skill is not described as `Can Crit` or `Can Double` it doesnt do it.
> - Now a Stirke skill should only Double yet it crits from 80->162 dmg.
> - Stab does 580 but very very low chance in the begining
> - Yet the strike critted more than the stab landed (Sword-8% crit while knives 12%)

The 0.49.0 crit/blow/`[Double]` work is unplayed but he has clearly already looked at it. The rule he
wants is a **hard flag check**, not a probability: a skill with `[Double]` and no `Can Crit` must never
roll a crit, and vice versa. His second point is a balance observation that falls out of it — a
double-only Strike firing at the weapon's crit rate produces more big hits than the blow it is supposed
to be worse than.

**M9. 🔴 The rogue identity rework — the biggest design item in the file.**
> - the evasion mastery passive for the rogue class should be only the evasion floor.
> - The +20% crit and +10 evasion should be removed
> - move the crit rate (the 20%) from 32+ rogue armorm mastery to lvl 20+
> - its good to have the higher crit rate early on,
> - if we leave the evasion mastery critical chance the balance will shift at lvl 32 when each blow
>   lands with the 64+% chance ...
> - the critical rate is not additive each passive/buff should multiply % on top of it base for
>   dagger/bow
> - and evasion is to op we established that +10 == 10% .. so he have 14 from armor, 4 from buff ...
>   thats free 18% .. we dont need to give him more.. that is sure 18% easion for characters of same
>   level and same Dex - everithing else will make him untuchable - the floor is only for fighting
>   fighters and archers (classes with high acc)

Four separate changes, and they interlock — this is the answer to §50h (the rogue at 0.65× warrior DPS
at 20-28), arrived at from the other direction:
1. **`evade_mastery` becomes floor-only.** Today it is `EvadeFloor 10/20/30% + CritRate +20% + Evasion
   +20` (`Skills.Common.cs:593`). Strip the crit and the evasion; keep only the floor. ⚠ His text says
   "+10 evasion" — the code says +20, worth confirming which he means to remove (I read: all of it).
2. **Move the +20% crit rate to level 20**, out of Armor Mastery @32. That is exactly the early-blow
   problem in §50h: the rogue's blow gate is a 9.2% crit until 32.
3. 🔴 **Crit rate stops being additive and becomes MULTIPLICATIVE** on the weapon's base — a real
   formula change in `StatCalculator`/`RecomputeDerived`, not authoring. It also makes his 32-point
   worry disappear on its own: ×1.2 on a 12% dagger base is 14.4%, not 32%.
4. **Evasion is capped by authoring, not by more floors** — 14 (armor) + 4 (buff) = 18 is already the
   budget. The *floor* stays but is framed as an anti-accuracy tool only.

**Measure before and after** (`tools/BalanceMatrix`) — 2 and 3 pull in opposite directions.

### M9 follow-up, 2026-08-06 — the L2 research he asked for
> well a 30% crit chance buff is multiplier..not addition ...why should a passive be a addition? - later
> lvls a rogue gets a 50% crit chance increase ... Can we do a research on l2 dagger classes critical how
> it's applied ...even if it's additive the +20% evasion_mastery bonus is unnececery

**What L2 actually did — it changed, and both answers are "L2":**
- Crit rate is a 0-1000 number where **1000 = 100%**, so **500 = 50%**.
- Weapon bases: **blunt/fist 4%, dual/polearm 8%, dagger/bow 12%** — our dagger 12 / sword 8 matches.
- **Early chronicles (C1-C2): crit buffs and passives were MULTIPLICATIVE.** In **C3 they were changed
  to ADDITIVE**, and at the same time a **hard cap of 500 (50%)** was introduced. The two changes went
  together: additive only works because the cap contains it.
- Dagger blows used the auto-attack crit formula **plus a per-skill modifier** (~20% for Mortal/Deadly
  Blow) — i.e. the blow's own chance is not the raw crit rate.

**So his instinct is pre-C3 L2, and it is self-consistent — but the honest engineering answer is that
the shape barely matters at the top and matters a lot in the middle:**

| rogue @ dagger 12% base | +20% passive | +20% then +50% later |
|---|---|---|
| **additive** (today, C3-style) | 32% | 82% → **capped at 50%** |
| **multiplicative** (his ask, C1-style) | 14.4% | 21.6% |

We already have the 50% cap (`StatCaps`), so additive means *the cap does all the containment* and the
32-point spike he objected to is real. Multiplicative removes the spike — but ⚠ **it also means his
other goal, "higher crit rate early on", is barely served**: moving the rung to 20 would give +2.4
points, not +20. And it will *lower* rogue DPS across the board, which pushes against §50h (rogue
already 0.65× warrior at 20-28). **Recommendation: build it multiplicative as he asked, but measure the
rogue's whole 20-40 curve in BalanceMatrix first and be ready to raise the dagger base or the blow
modifier to pay for it** — that is exactly how L2 pays for it (the per-skill blow modifier).

✅ **Unconditional either way, his words:** *"even if it's additive the +20% evasion_mastery bonus is
unnececery"* — strip the crit and the evasion off `evade_mastery`, floor only.

Sources: [Predator — L2 critical hit mechanics](https://predator.ge/en/news/lineage-2-critical-hit-mechanics-explained-auto-attacks-dagger-skills-and-chronicle-changes)
· [PMfun — critical rate for daggers](https://forum.pmfun.com/viewtopic.php?t=36564)

**M10. 🟡 Balance todo — two items, one of which I cannot find in the code.**
> ### Champtions
> is getting killed while offline farming when his bufs worn off while the dagger is getting missed
> like crazy - i have 65 acc and 95 evasion .. 30% difference is way high for this low lvl
> - we need to lower champions passives debuff -20%pdef to -10% - now have less than the dagger -
>   same as mage
> mages have big mana problem - for 2-3 mins their MP is depleated

- **The 30-point acc/eva spread** = 30% miss at the same level (1 point = 1%, by design since
  2026-08-02). Two characters of the same level should not be able to open a 30-point gap; that is what
  M9's evasion budget is about. This is the *evidence* for M9, not a separate ask.
- ✅ **FOUND IT, 2026-08-06 — it is `Two-Hand Mastery`, not an armor passive.** His answer:
  > the passive is two handed weapon mastery ... The +30%atk and -20% defence.. We need to lower it to
  > 10. I want a warrior in a heavy not to have lower defence than a mage...it's not logical..

  `Game.Shared/Skills/Skills.WeaponMasteries.cs:77-81` — `WarriorWeaponMastery`, five rungs, every one
  carrying **`DefencePct: -0.20f`** (plus `Evasion: -3`). Attack is `PhysAtkPct 0.30` on rung 1 and
  **0.50** on rungs 2-5, so the trade gets *better* with level while the penalty stays flat at −20%.
  **Change: `-0.20f` → `-0.10f` on all five.** ⚠ I looked in the armor masteries and the class bonuses
  first and reported it didn't exist — it was on the WEAPON mastery, gated to `WeaponType.TwoHanded`,
  which is why a 2H Champion in heavy armor ends up under a robed mage. Trivial edit, five numbers.
- **Mage MP: 2-3 minutes to empty.** Known and expected — MP potions are on explicit hold pending the
  3rd-class kits. Worth re-opening that hold: the offline farm makes a dry mage a *death*, not a pause.

**M11. 🟡 One daily Apothecary quest, shared by every town.**
> Can we give every apothecary the same daily quest - taken from one returned to other (or just start
> from every apothecary and finished to the same) ? - just when im lvl 40+ i have no way to go back to
> the 1st town just to take it (gk costs money)... i want to start it from every town once a day - same
> quest - same id - they dont overlap - taken from one cannot be taken 2nd time

**Same quest id offered by every Apothecary, turned in at any Apothecary, once per day per character.**
Mechanically: many-to-many NPC binding on one quest def, and the daily flag already exists.

**M12. 🟡 A gatekeeper jump should land you at the destination GK.**
> should spawn you next to the new city GK (next not on top => gk.x+150/gk.y+150) and not in the middle
> of town. => I teleport then need to move again to the next gk so i can go to a zone

Beside, not on top. Trivial change in the teleport destination table.

**M13. 🟡 The [Talk] button (this is C9, specified).**
> Clicking one time opens the target - with the `Talk` button
> Clicking secont time or the `Talk` button - the char start to move towards the npc and talk (open npc window)
> When im talking to npc my move should be forbidden - many times now i get next to the gk -> open
> window to teleport but before that i clicked somewhere on the ground and with open window i get "Too far"

Three parts: the button, **walk-to-then-talk** on the second tap, and — the one that actually bit him —
**movement is locked while an NPC window is open**, so a queued ground tap can't drag you out of range
mid-dialog.

**M14. 🟡 Cap the vendor buyback list** at 10-15 items.

---

## Defects found in the tested sections

**48g. 🔴 The Blessing Box eats your unspent picks.**
> form 17 you click 10 ok .. get 10 .. but from 17 my second box i clicked 7 .. to finish the scroll
> collection .. but then my box disappeared with my 3 unused ...

Confirmed in code: `GameLoopService.HandleSelectBoxItems` takes `cmd.ItemIds.Distinct().Take(PickCount)`
and refuses only an **empty** selection — any count from 1 to 10 consumes the box. The client's Confirm
is not gated on the tally either. **A 250k box can be spent on 7 scrolls.** Fix both ends: server
requires exactly `PickCount` (message otherwise), client disables Confirm until `10 / 10`. ⚠ Pick-1
boxes are unaffected (1 == PickCount), but the same rule covers them.

**46d. 🔴 B7 half-fixed: you can TARGET an out-of-sight party member, but not INVITE one.**
> /ptinv -> `no player x nearby` - cannot invite player out of sight. - when i invite him when im next
> to him then leave his sight it works...

B7 changed the target frame; the invite path still resolves the name through a proximity lookup. An
invite by NAME should reach any online player (or at least anyone in the same zone), not just someone in
your grid cell.

**46m. 🟠 C11/B2: compare on a PENDANT still opens a stud.**
> its one window but still opening compare of a selected pendant it opens stud details

The merged compare+details window is right; the **worn-item lookup** picks the wrong jewel slot for a
pendant. B2 is not actually closed.

**46o. 🟡 G6 warehouse: raise both caps now, lower them when the expandable system lands.**
> cant remember the exact numbers but the warehouse slots were expandable and base as 150-100 and
> account max was 10 ... can u make them account to max now and private as well and leave a note when
> making the expandable system to lower them

The account bank's cap is the small one and it is in the way. Raise both to the max, and leave the
comment where the expansion system will need to pull them back down.

**46u. 🟡 The economy verdict — the cut was smaller than the arithmetic said.**
> - now im rogue 33 and have 2.6kk gold while selling only gear, a worrior 31 - 1.4kk
> - so i think lowering drop by 3.3 and increase the selling by 2.5 its actully a 25% decrease
> - now its only 3 times harder to gear up 😀
> - sooner or later we will get to L2 drop rates/sell prices...

He is right about the composition (13× rarer × 2.5× dearer ≈ 5× on gold, but he is reading the
*gearing* cost, which only moved 3×). Not a defect and not urgent — the direction of travel he wants is
"eventually L2 rates". ⚠ The old note still applies: the real fix is the **coin curve**, not another
multiplier.

---

## What PASSED (no comment needed, marked `x` in his file)

- **All of §48 except 48g** — the text-box fix (48a/48b, his "unplayable", **closed**), x1 rates,
  no buff-scroll drops, the Blessing Box UI and its 11th-tick refusal, bound scrolls, the two potion
  rungs, and the quieter bag.
- **All of §47** — F1 target hand-over, QSell, save-login, the Sprint/Dash family and its whole ladder
  including Sprint L2 over Dash Mythic, the warehouse quest-token rescue, and the four UI review fixes
  (tracker overlap, five pins, preset C, compare no longer jumping).
- **All of §46 except 46d/46m/46o/46u** — per-character auto marks, the per-account farm balance, B6,
  the 0-count hotbar slot, `/offline`, undo-a-bin-delete, quest tokens undisposable, the shared
  filter/tabs, gatekeeper tabs, NPC quest scoping, jewel swap by delivered M.Def, and **the entire quest
  section Q1-Q5**.
- **Carried forward, now closed:** 37a/37b/37c (partial-stack trading), 36f, 34c/34d, 33l, 32b/32h/32o/
  32p/32s/32y.

**Still blank after this pass:** 37d, 37e (trade shortfall / full-bag judgement), 36e (boss phase script
on re-pull), 32z (the auto-farm chain matrix), 25b (no combat-log out of a DoT), 13a (the 3h banner),
17-1 (jail border), 17-23 (client collision).
