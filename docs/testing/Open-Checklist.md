# OPEN CHECKLIST — everything untested as of **0.53.2** (2026-08-07)

> **This file is now UNVERSIONED and ROLLING.** It replaces the three per-version
> `Open-Checklist-0.45.0 / -0.47.0 / -0.48.0` files, which are gone — every item of theirs that was still open is carried
> forward below, and everything they answered is preserved verbatim in
> [Playtest-Archive.md](Playtest-Archive.md). When you finish a pass, I transcribe your answers into
> the archive and rewrite this file against the next build. One open checklist, always.

Edit this on the phone: write your comment after the `->`. Put `x` in the `[]` if it passed with
nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority, `?` for a
question. `[]` with no id in front is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

---

## ⚠ BEFORE YOU START

**Install the 0.53.2 APK and unzip the 0.53.2 server.** Protocol is **12**, unchanged since 0.46.0 —
but install both anyway, the catalogs and skill tables are compiled into each side.

🔴 **DELETE `Game.Server/game.db` + `-shm` + `-wal`.** Not optional this time. Three separate reasons
stack up: 0.52.0 changed columns, 0.53.0 removed the God layer (an old character carrying `Race = 99`
or a God item references an id that no longer exists), and the mastery restructure clamps
`mastery_robe` from 3 rungs to 2 on login. ⚠ Delete the one in `Game.Server/`, **not** the stale copy
in `bin/Debug/` — that one is a decoy and deleting it will fool you into thinking you reset.

🔴 **SIX BUILDS ARE UNPLAYED.** 0.49.0, 0.50.0, 0.51.0, 0.52.0, 0.53.0/0.53.1 and 0.53.2 — the
longest unplayed run this project has had. Two of them (0.51.0 and the mastery restructure) had **no
checklist section at all** until today. That is the actual risk here: not any single change, but that
six builds' worth of combat-maths reworks have never met a player. **A play pass is worth more right
now than anything else I could build.**

🔴 **PRE-FLIGHT I OWE YOU AND HAVE NOT DONE: `tools/SmokeTest` has not been run** since the mastery
restructure, which changed **`AutoLearnCoreSkills` — the login sequence**, which is the exact thing
the smoke test exists to catch. Its bugs are invisible to a human playtest (the client renders what
it was *sent*, so a bar can look perfect on screen and already be destroyed on the server). It needs
the server running, and I don't launch that unprompted. **Say "run the smoke test" and I will, before
you start.** -> 

---

## 🔴 0. DECISIONS I NEED FROM YOU (these block work — answer first)

`0a` [] - **The nuker now beats the champion by ~19%** where they were at parity, because magic crit
became a real channel (§56). Measured, not derived: CHAMPION/NUKER went **0.98× → 0.84×** at level 74
in best gear with NPC buffs. The levers are the **base 50** and the **flat ×3 crit damage**, both your
numbers. Is the nuker's lead earned, or do I trim it? -> 

`0b` [] - **Accuracy cannot beat an evasion FLOOR, and +5 accuracy therefore buys an archer nothing**
against a rogue (§55e). `miss = clamp(5% + (eva − acc), floor, 95%)` — evasion is additive from its
first point, accuracy only claws back what sits above *both* the 5% base and the defender's floor.
Against a rogue's 10% floor, +5 accuracy is worth **zero** until you already out-evade him by 10+.
The +5 is the right size; the **floor** is the problem. Do you want accuracy to eat into the floor,
or is an archer simply not supposed to beat a rogue's dodge? -> 

`0c` [] - **Physical crit damage base ×2.0 — still under research, still unchanged.** You think L2 is
×1.5 and neither of us can source it. The real question is *what the multiplier multiplies*: ours
scales the **whole ratio including skill power**, L2's scales the **attack term only** — so "should
be 1.5" may be the same ×2 on a smaller base. **A** = lower to 1.5. **B** = keep ×2 but apply it to
the attack term only (touches `CritFlatFactor` and every call site). ⚠ Rogue damage now rides almost
entirely on crit damage, so this is a big lever. -> 

`0d` [] - **The gear CSV's attribute ceiling.** I found while updating `docs/data/gear/gear_sets.csv`
that the shipped roll system is the **inverse** of what that file described: **the MAX is flat across
all five grades and the MINIMUM is what ramps**. A D-grade sword and an S-grade sword both cap at
+30% crit rate; the grade only removes bad luck. Your file said the ceiling climbs per grade. Which
did you mean? -> 

`0e` [] - **`light` body armor at 52 (202 P.Def) is WEAKER than at 40 (218).** Authored that way in
your CSV and shipped that way, so the C body is a downgrade for anyone who already has the D one.
Typo, or deliberate (the 52 line trades defence for its DEX/P.Atk set bonus)? -> 

---

## My Finds

- [] - 
  > 

- [] - 
  > 

- [] - 

---

## 49. 0.49.0 — the enchant rework (`D1`/`D2`)

Full detail: §49. Three scroll TYPES × six grade bands = 18 scrolls.

`49a` [] - Three scroll types behave differently: one **breaks** the item, one drops it **−1**, one is
**safe**. -> 
`49b` [] - Rarity picks the grade band E→S; a scroll refuses an item outside its band. -> 
`49c` [] - `/enchant <value>` with the item picker still works, unrestricted (an F weapon to +999999).
⚠ **This is now load-bearing** — with the God layer deleted it is one of only two ways to get cosmic
stats for testing. -> 
`49d` [] - Enchant scrolls drop from the right sources per type, and the drop rate reads sane. -> 

---

## 50. 0.49.0 — crit damage, BLOWS and `[Double]`

Full detail: §50. ⚠ Its 0.65× figure is **debunked** — see §52, which supersedes it.

`50a` [] - A **blow** lands from behind/stealth and reads as a blow, not a crit. -> 
`50b` [] - `[Double]` shows in the combat text when a skill doubles. -> 
`50c` [] - 🔴 **`Can Crit` and `Can Double` must be EXCLUSIVE** (your `M8`). A `[Double]` Strike was
**critting** — 80 → 162 on a skill described as double-only. Confirm one skill can no longer do both. -> 

---

## 52. 0.50.0 — crit RATE on your L2 model

Full detail: §52. This closed the crit thread; §50h was a **measuring error** on my side, not a defect.

`52a` [] - Crit rate follows the L2 model (base × DEX mod × passives × buffs, clamped once at the end). -> 
`52b` [] - `Can Crit` / `Can Double` render per skill in the skill window. -> 
`52c` [] - Per-skill crit modifiers apply — a skill authored to crit more actually does. -> 
`52d` [] - ⚠ The one you flagged: **a sword at 8% crit was out-critting knives at 12%.** Confirm that
is gone. -> 

---

## 53. 0.52.0 — the playtest-19 DEFECTS + the FRICTION tier

Full detail: §53. ⚠ **delete `game.db`**.

`53a` [] - 🔴 **`48g` the Blessing Box no longer eats itself on a partial pick.** Tick **7 of 10** and
confirm: either it refuses until you pick exactly 10, or it gives 7 and keeps the box. Last time the
box vanished and the 3 unused picks were lost. -> 
`53b` [] - 🔴 **`46d` `/ptinv` can invite an out-of-sight player.** "no player x nearby" was the bug;
the earlier fix corrected the target frame, not the invite lookup. -> 
`53c` [] - 🟠 **`46m` compare on a PENDANT opens a PENDANT**, not a stud. -> 
`53d` [] - 🟡 `46o` both warehouse caps raised to max, with the note to lower them when expansion
lands. -> 
`53e` [] - The friction tier as a whole — does the game feel less fiddly, or did I just move the
friction somewhere else? -> 

---

## 54. 0.53.0 + 0.53.1 — the DELETIONS, `M7`, `M1`, `/spd`, two clocks

Full detail: §54. ⚠ **delete `game.db`**.

`54a` [] - 🔴 **`M1` — nothing is unhittable any more. Do your own test first**: admin, accuracy 9999,
a bow, **level 20 vs a level 40/80 dummy**. You must now land **~5%** where it was *zero, forever*.
With **Precision** L1 the floor is **10%**, L2 (40+) **20%**. The other way: a level-20 rogue in a
level-90 field must **dodge ~10%**. ⚠ Sanity-check the ordinary case did NOT move — same-level is
still ~5%/95%, a 10-15 level gap still hurts. Exp and drops still pay **zero from a 13-level gap**;
killing far above you stays pointless, just no longer impossible. -> 
`54b` [] - **`M7` Heavy Draw is gone from the rogue at every level** — not at 24, and not as
Piercing/Snare/Rending Shot on a 40 ranged discipline. -> 
`54c` [] - **Evasion Mastery follows the CLASS CHANGE, not the level.** Lv1 at 20 for every rogue;
Lv2 at 40 **only on taking a MELEE discipline**; a ranged discipline stays Lv1 forever; **Lv3 goes to
nobody** (its milestone is the 4th class change, which doesn't exist). ⚠ Check a level-40+ rogue with
**no discipline chosen yet stays at Lv1** — that is the actual change. -> 
`54d` [] - **The deletions broke nothing by their absence**: no Reflexes, Bow Mastery, archer Armor
Mastery, Dispel Magic, HP Boost, Greater Heal or "Class Balance" rows — but `evade_mastery`,
`precision` and `anti_magic` all **stay** and still grant at 20/40/76. -> 
`54e` [] - **The God layer is gone and the debug rig still works.** Creation offers Human/Elf/Ork
only; no God's Judgment / God's Robes in Boxes. ⚠ `/enchant` and `/spd` are now the **only** route to
cosmic stats — test them like they matter, because they do. -> 
`54f` [] - The Treasure Chest still opens and pays its staples (its jackpot is now the S-grade Mythic
1H blade). -> 
`54g` [] - **`/spd` replaces the four `/speed-*` commands.** `/spd m 250`, `/spd a 1200`, `/spd c
1500`; **bare `/spd` resets all three**; a bad form prints usage. ⚠ The old `/speed-*` must fall
through to unknown-command. -> 
`54h` [] - **Two clocks in the title bar** — `game 14:32 · 09:47:12`. Watch for a minute: game time
must advance ~6 minutes, and **survive a relog** without jumping. -> 

---

## 56. 0.51.0 — magic crit becomes its OWN channel

Full detail: §56. **Never indexed in a checklist before today.**

`56a` [] - Magic crit rate is no longer decorative — a human mage was stuck at **2.0%** and the 20%
cap needed WIT 200. Cast with and without **Insight**: the buff must now roughly **double** observed
crit frequency (it used to be clamped away mid-chain and bought +3 points). -> 
`56b` [] - An **elf mage in the full kit** hits 10%, and ×2 Insight puts him at **20% — the cap
exactly**. -> 
`56c` [] - A **mob** still crits occasionally (~1.25%), not never. -> 
`56d` [] - **Ferocity and the crit-damage weapon attribute no longer pay mages.** Both are authored
for fighters and used to leak through a shared field. Put a crit-damage attribute on a staff: the
magic crit multiplier must **not** move — it is a flat ×3. -> 
`56e` [] - **Resonance** reads as a percentage (×1.2), not a flat number. -> 

---

## 57. The MAGE MASTERY RESTRUCTURE — masteries now STACK

Full detail: §57. ⚠ **delete `game.db`**. 🔴 **SmokeTest not run — see the pre-flight above.**

`57a` [] - **Armor masteries stack.** Which one applied used to be decided by **dictionary order**.
A nuker's robe MP-regen ×1.2 now multiplies Spellcaster Mastery's ×1.2 — visibly better than either
alone. -> 
`57b` [] - **Robe Armor Mastery is bought with SP at 7 and 14** (2 rungs, no longer auto-granted,
no longer 3 rungs), and **"Weapon Proficiency" appears on nobody**. ⚠ The migration clamp is the risk:
a mage with **no robe P.Def at all** is the failure mode. Deleting the db avoids it — do that. -> 
`57c` [] - **The wrong-weapon penalty is a penalty, not an execution.** It was ×0.05 — annihilation.
Now ×0.5. Hold in turn: wand/staff (×1), sword/blunt (cast ×1, M.Atk ×0.6), bow/dagger/bare (×0.5
across the board). The order must degrade as listed. -> 
`57d` [] - ⚠ **A bow caster cannot BUFF his way out of the magic-accuracy penalty** — it is applied
after buffs, on purpose. Buff up holding a bow and confirm it survives. -> 
`57e` [] - **A cleric in light armor composes back to cast ×1.00 / attack ×1.00**, vs ×1.05 in a robe
— your "−5% from a robe". A **nuker** in light stays punished. -> 
`57f` [] - **The dual's evasion roll is FLAT +5**, never a percentage. Mob-miss against the rogue
should read **21-23%** at max roll — it was **33-42%**. You said 16% bare is fine, so `evade_mastery`
was deliberately left alone. -> 

---

## 55. 0.53.2 — Restore Spirit gets LEVELS, the bow's accuracy roll goes FLAT 5

Full detail: §55. No db reset needed for this section alone.

`55a` [] - **Restore Spirit is a ten-rung ladder** (25/40/45/50/55/60/65/70/75/80), ending at
**120 MP for 200 HP**. It had ONE rung for life while the bolt ladder grew 30 → 116. Rung 1 (20 MP /
65 HP @25) is the **CSV** and must not have moved. -> 
`55b` [] - ⚠ **The skill card shows the HP price**, not just the MP gain. A skill that silently eats
200 HP reads as a bug the first time it kills you. -> 
`55c` [] - Casting it at **low HP** refuses, or at least does not kill you. -> 
`55d` [] - **Mage Armor Mastery rungs 5-8 @40/50/60/70** carry mpWhenRestored **50/60/70/80**. ⚠ P.Def
and max MP are **frozen** at the rung-4 values deliberately — if those climb, that's a defect. -> 
`55e` [] - **The sum at 80, in a robe: 200 MP for 200 HP** — your "+200 MP for −200 HP" endpoint. Out
of a robe the same cast delivers only 120. -> 
`55f` [] - 🔴 **The actual question: farm a mage 10+ unbroken minutes at 40+.** Your playtest-19
finding was *"mages run out of MP in 2-3 minutes"*. Does the rotation sustain now? It is not meant to
be free — the design is "farm 30-40 min, rest a bit". **If it is still 2-3 minutes, the ladder is not
the fix and I need to know.** -> 
`55g` [] - **The bow's accuracy roll reads Accuracy +1..+5 FLAT**, never a percentage, at every grade.
⚠ **An old bow keeps its `AccuracyPercent` roll and must still work** — the enum entry was kept and
made unrollable, so no db reset was needed. -> 

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

These have survived several checklists untouched because none of them happens by accident. If you
want them closed, they need a session aimed at them.

`37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. -> 
`37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items are
refused. -> 
`36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
top. -> 
`32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
ranks, assist-leader — and all of it **survives a relog**. -> 
`25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. -> 
`13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). -> 

---

## KNOWN OPEN — not defects, don't spend the pass on them

Tracked, ruled on, or deliberately queued. Listed so you don't re-report them.

- **`B9` the jail has no border** — an admin teleported inside is clamped back to the dungeon. Queued.
- **`B10` client-side collision does not exist** — only the server rubber-band. Queued.
- **`B8` soulcrystal-tier gear prints A grade** in details while accepting a Mythic attribute scroll.
  Queued.
- **`D5` the [Combat] chat tab in its own window.** Queued.
- **`M5`/`M6` the tutorial chain + bound 30-day newbie gear.** Next up, and it is a quest project.
- **`C1` `C3` `C14` `C15` `C16` `C17`** — the QoL six you picked. Queued behind M5/M6.
- **`C4`** auto-on for buff potions/scrolls — **your ruling: comes later, with the AutoPot tabs.**
- **`G2` / `0e` `lb_*` + `wc_*`** — **CLOSED by your ruling: leave them.** Placeholders for 40+,
  commented out, harmless. I will stop asking.
- **`D4` `G5` `F1` `V1` `G4`** — **done and tested at 0.49.0.** Older docs still list some as open;
  they are stale on this point.
- **Crafting (`D3`)** — designed, unbuilt, and still the top content blocker. **3rd/4th class kits**
  — blocked on your 40+ CSVs. **`G3` mobs-as-players** — needs the document + BalanceMatrix tables
  first, then 2-5 real mobs as an experiment, per your ruling. **Instances** — you are holding.
- **The champion's −10% P.Def** (was −20%) — owed by **you**, on a re-test.
