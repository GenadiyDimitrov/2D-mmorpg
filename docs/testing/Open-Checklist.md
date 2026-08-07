# OPEN CHECKLIST — everything untested as of **0.55.0** (2026-08-07)

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

**Install the 0.55.0 APK and unzip the 0.55.0 server.** Protocol went **12 → 13** (a title-colour
field and two hub methods, all additions — the server still accepts older clients). Install **both**
sides anyway: an older APK draws every NPC as a bare "Marius" and every title in plain gold, and the
catalogs and skill tables are compiled into each side.

🔴 **DELETE `Game.Server/game.db` + `-shm` + `-wal`.** Not optional this time. Four separate reasons
stack up: 0.52.0 changed columns, 0.53.0 removed the God layer (an old character carrying `Race = 99`
or a God item references an id that no longer exists), the mastery restructure clamps `mastery_robe`
from 3 rungs to 2 on login, and 0.55.0 adds three title columns. ⚠ Delete the one in `Game.Server/`,
**not** the stale copy in `bin/Debug/` — that one is a decoy and deleting it will fool you into
thinking you reset.

🔴 **EIGHT BUILDS ARE UNPLAYED.** 0.49.0, 0.50.0, 0.51.0, 0.52.0, 0.53.0/0.53.1, 0.53.2, **0.54.0**
and **0.55.0** — by a distance the longest unplayed run this project has had. Two of them (0.51.0 and
the mastery restructure) had **no checklist section at all** until recently. That is the actual risk
here: not any single change, but that eight builds' worth of combat-maths reworks, a replaced starter
quest chain and a protocol bump have never met a player. **A play pass is worth more right now than
anything else I could build**, and it has been worth more for four builds running.

🔴 **PRE-FLIGHT I OWE YOU AND HAVE NOT DONE: `tools/SmokeTest` has not been run** since the mastery
restructure, which changed **`AutoLearnCoreSkills` — the login sequence**, which is the exact thing
the smoke test exists to catch. Its bugs are invisible to a human playtest (the client renders what
it was *sent*, so a bar can look perfect on screen and already be destroyed on the server). It needs
the server running, and I don't launch that unprompted. **Say "run the smoke test" and I will, before
you start.** -> ✅ **DONE 2026-08-07, ALL CHECKS PASSED** (~150 assertions, server log clean, no
exceptions). The login sequence is intact after the mastery restructure: a level-81 Bulwark learned
its kit and correctly skipped the 6 stat-swap passives; the main bar survived a subclass swap AND a
relog byte-for-byte; `item:`/`preset:` tokens kept; per-class levels did not leak. **This pre-flight
is closed — nothing blocks the play pass.**

---

## 🔴 0. DECISIONS I NEED FROM YOU (these block work — answer first)

`0a` [] - **The nuker now beats the champion by ~19%** where they were at parity, because magic crit
became a real channel (§56). Measured, not derived: CHAMPION/NUKER went **0.98× → 0.84×** at level 74
in best gear with NPC buffs. The levers are the **base 50** and the **flat ×3 crit damage**, both your
numbers. Is the nuker's lead earned, or do I trim it? -> This need to be tested. When I leave the chars to play alone all measure.

  ✅ **Understood — deferred, no code change.** You will judge it from an auto-farm run, not the
  matrix. ⚠ One flag: that makes **auto-farm the measuring instrument**, and `32z` (auto-farm skill
  chains + surviving a relog) has never been tested in any playtest. If the chains misbehave the
  measurement lies. Worth doing `32z` in the same sitting.

`0b` [?] - **Accuracy cannot beat an evasion FLOOR, and +5 accuracy therefore buys an archer nothing**
against a rogue (§55e). `miss = clamp(5% + (eva − acc), floor, 95%)` — evasion is additive from its
first point, accuracy only claws back what sits above *both* the 5% base and the defender's floor.
Against a rogue's 10% floor, +5 accuracy is worth **zero** until you already out-evade him by 10+.
The +5 is the right size; the **floor** is the problem. Do you want accuracy to eat into the floor,
or is an archer simply not supposed to beat a rogue's dodge? -> 
  - Explain to me this one.
    > dagger have floor of 10%,\
    > all the passives,buffs and dex he have let say difference of 20 with a normal class/mob\
    > archer have a selfbuff that give him 10 acc then the weapon mastery +4 acc and 2 more normal buffs +4 => 10+4+4+4 = 22 + Acc\
    > clam(0.05 - 0.02 (20-22), 0.1,0.95) => so archer hits the floor of a dagger\
    > until the difference is +5 and up in faveour of evasion the floor is met\
    > is that what u ment by +5 acc dosnt give you nothing .. well if dagger invest in +10dex/-5atk/-5con and +5 eva on weapon\
    > then the difference become free +15 => clam(5% + (35-22=13),10floor) => miss = 18% -> but archer inves in +5 acc miss becomes 13% and also add a 5 dex here you go floor again\
    > Am i calculating right

  **YES — all four of your cases are exactly right**, I checked each against
  `StatCalculator.ResolveAvoidChance`. And **my framing was wrong**, not yours. The order is:
  `m = clamp(5% + (eva − acc)·1%, 5%, 95%)` → level gap → `clamp(m, max(5%, floor), …)`.
  Your cases: `(eva−acc) = −2` → 3% → floored to **10%** ✅ · lead 13 → **18%** ✅ · +5 acc → **13%** ✅
  · +5 DEX → 8% → floored to **10%** ✅.
  So accuracy pays **full value while his evasion lead is more than 5 points**, and nothing once the
  lead is 5 or less (the floor has taken over). The dead zone is the **last** 5 points, not the first
  10 — I had it backwards. Your +5 acc case is 18% → 13% miss = **+6.1% landed hits**, which is not
  nothing. ⚠ The bigger point: step 1 clamps at 5%, so accuracy **beyond** the defender's evasion is
  wasted against *everything*, not just rogues — accuracy is a catch-up stat, never a scaling one.
  🔴 **Still yours to rule:** may accuracy eat INTO the floor? I recommend **no** — the floor is
  authored as an anti-accuracy tool, so letting accuracy pierce it deletes the passive's purpose. -> Yes the floors and ceiling cannot be touched (they are there for a reason)


`0c` [X] - **Physical crit damage base ×2.0 — still under research, still unchanged.** You think L2 is
×1.5 and neither of us can source it. The real question is *what the multiplier multiplies*: ours
scales the **whole ratio including skill power**, L2's scales the **attack term only** — so "should
be 1.5" may be the same ×2 on a smaller base. **A** = lower to 1.5. **B** = keep ×2 but apply it to
the attack term only (touches `CritFlatFactor` and every call site). ⚠ Rogue damage now rides almost
entirely on crit damage, so this is a big lever. -> When you land a critical hit or use specialized dagger Blow skills, critical power multipliers inflate the numerator:
\(\text{Critical\ Damage}=\frac{77\times \text{P.\ Atk}\times 2\times \text{Crit\ Power\ Multipliers}}{\text{P.\ Def}}\) -> so you were right the base as i cen decifer the formula its 2 times then the mutlipliers(buffs passives)

  ✅ **CLOSED, nothing changes** — ×2.0 stays, neither A nor B. Your source settles it. (One residual
  I am *not* re-raising: your formula puts the ×2 on the attack term, ours scales the whole ratio
  including skill power. It only diverges on high-power skills; say the word if you ever want it.)

`0d` [~] - **The gear CSV's attribute ceiling.** I found while updating `docs/data/gear/gear_sets.csv`
that the shipped roll system is the **inverse** of what that file described: **the MAX is flat across
all five grades and the MINIMUM is what ramps**. A D-grade sword and an S-grade sword both cap at
+30% crit rate; the grade only removes bad luck. Your file said the ceiling climbs per grade. Which
did you mean? -> In l2 the weapons do a flat increase in crit (+64, +90, + 109 @SGrade) a % increase depend on the base crit value - so if we do a dagger with 30% and a sword with the same sword wont benifit the same, but if we do a flat 90 its as 9% increase after all the buffs/passives and is 9% across all. But yet the only one to do flat crit rate is a bit off ... everithing is % then crit to be flat .. why ? .. we need to alter the sword to 90% crit rate ... so unbuffed sword wielder to have 88x1.9 -> 167 and a dagger 132x1.3 = 171 -> then the max a tank investing in critical sword will not have 25% hp or 15% as but 43.5% crit rate (thats pure playstile choice)

  ✅ **BUILT** — sword ceiling 30 → **90** (`RampSwordCrit = 3/15/30/60/90`). Your two numbers land
  exactly: sword `88 × 1.9 = 167`, dagger `132 × 1.3 = 172`.
  🔴 **But building it found a real defect, bigger than the question you asked.** The roll was not a
  multiplier at all — `Entity.RecomputeDerived` put it in `CritRateFlat` as `value / 100`, so a maxed
  roll was **+30 PERCENTAGE POINTS**: sword 8.8% → **38.8%**, dagger 13.2% → **43.2%**, both nearly
  at the 50% cap off one roll. That is **+300 on your 0-1000 scale** vs L2's +109 at S grade, and 10×
  your own rule for that channel (*"a flat 30 is flat 3%"* — the divisor should have been 1000).
  Worse, being FLAT it **collapsed the 3:2:1 weapon identity** the crit model exists to create, since
  the same +30 is worth far more to the weapon with the smaller base. It now multiplies — which is
  what the tooltip has always said.
  ⚠ **This is a large dagger/bow NERF at max roll (43.2% → 17.2%).** It is the number you wrote
  yourself, so I built it, but nobody has played it. Watch rogue damage in the pass. -> why u count it as a nerf .. the 430 with jsut a weapon attri was just way to OP .. the 400 we must get only after getting fully buffed
  Archer - 132x1.2(passive) = 158x1.3(single buff) = 206 => here if we add the x1.3 attri we get 267 which is 6% and without the attri do a x2 harmony we get 411 ... but if we want we can then add the attri end get to the cap 535
  Fighter - 88x1.3(buff) = 114x1.9(attri) = 217 ~same as dagger without atri then the x2 harmony here u go on a single sword 434 near the cap

`0e` [!] - **`light` body armor at 52 (202 P.Def) is WEAKER than at 40 (218).** Authored that way in
your CSV and shipped that way, so the C body is a downgrade for anyone who already has the D one.
Typo, or deliberate (the 52 line trades defence for its DEX/P.Atk set bonus)? -> My bad. When i did the csv i added to the 40 sets a boots pdef as well -> 179 (fixed the csv)

  ✅ **BUILT** — synced `Items.cs` to your CSV edit: the light body array and all three
  `light_t40_*` variants 218 → **179**. Light P.Def is monotonic again: 86 / 125 / **179** / 202 /
  220 / 249, so the C body is an upgrade over the D one. -> 

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

## 58. 0.54.0 — the tutorial chain (M5) + the newbie kit as a 30-day loaner (M6)

No section in `TestChecklist.Unity.md` — the detail is here. **No db reset needed.** ⚠ Test this on a
**brand-new character**: the chain starts at level 1 and it **replaced** the old starter quests.

`58a` [] - **A fresh character is offered `Welcome, Traveller` by Huntmaster Cera at level 1**, and
the five parts chain in order: Welcome (1) → Blessings and Bottles (3) → Properly Armed (6) → Blooded
(10) → A Trade to Learn (15). Each one only offers after the one before it is handed in. -> 
`58b` [] - **The old starter quests are GONE** — no `starter_kit` / `starter_blooded` anywhere, and
you are never paid two newbie kits. -> 
`58c` [] - **The chain does not GATE the three class quests** (Marius / Oren / Vael). Part 5 points at
them; you can still level to 20 and do them having ignored the chain entirely. -> 
`58d` [] - 🔴 **The kit is a 30-day LOANER**: every piece reads **"Newbie …"**, is **untradable**,
cannot be sold, and carries a clock. ⚠ The real ladder gear it was cloned from (`sword1h_t1` etc.)
must be **untouched** — a Ferrite Mythic you drop or craft has no clock and sells normally. -> 
`58e` [] - **The completion consumables are bound too** (Ultimate Scroll of Return / Resurrection,
Dash and Instant Healing potions) but carry **no clock**. -> 
`58f` [] - **The loaner is a SET**: wearing the bound body + the accessories still completes the
armour set and pays its bonus. -> 
`58g` [] - ⚠ **A WORN loaner that expires is removed and your stats drop with it.** Easiest check:
`/spd`-style debug is no help here, so trust `58h` instead unless you want to wait 30 days. -> 
`58h` [] - **The pacing still holds**: part 4 ends ≈ level 10 and part 5 ≈ level 15 without grinding
between them. If it strands you, say where. -> 
`58i` [] - 🔴 **He never named the game.** The parts are called "Welcome, Traveller" etc. because
"Welcome To The `<Game>` World" needed a world name. **Give me one and I will use his literal
title.** -> 

---

## 59. 0.55.0 — the QoL five (C1 · C3 · C14 · C16 · C17) + written titles + NPC roles

No section in `TestChecklist.Unity.md` — the detail is here. 🔴 **DELETE `game.db`** — three new
columns (`CustomTitle`, `CustomTitleColor`, `MayWriteTitle`).

`59a` [] - **C1 — chat resets per character.** Talk in Local/World, leave to character select, enter
on a *different* character: the chat tabs are **empty**. Delete a character and make a new one — the
new one must not inherit its chat. ⚠ The **System tab is deliberately KEPT** (it is the crash trail);
if you want that wiped too, say so. -> 
`59b` [] - **C1 — the buffer holds ~1000 lines**, not 200. Spam a fight and scroll back further than
you could before. Watch for lag — the window still only *draws* 120 rows, so it should feel the same.
-> 
`59c` [] - **C3 — a timed item says how long it has left**, in item details, colour-graded: **green**
over 7d, **white** over 1d, **yellow** over 1h, **red** under. Check it on a **newbie kit piece**
(≈30d, green) and on a **1-day rune** (white/yellow). -> 
`59d` [] - **C14 — a two-handed weapon greys the off-hand square.** Equip a 2H sword/staff/bow: the
Shld square shows the *weapon's* abbreviation, dimmed, and **does not open anything** when tapped.
Equip a 1H + shield and the square goes back to normal. -> 
`59e` [] - **C16 — no more "the".** Titles read `Wealthy`, `Devoted`, `Warlord`, `Feared`,
`Ascended`, `Beloved`. -> 
`59f` [] - **C16 — each title has its own colour**, over the head *and* on the Rank board *and* in the
picker: gold=golden, time-played=green, PvP=purple, PK=dark red, level=sky, charisma=rose. ⚠ The PvP
title purple is **deeper than a flagged player's purple name** on purpose — tell me if they still read
as the same colour on the phone. -> 
`59g` [] - **C16 — the title's face differs from the name**: italic small caps with a little tracking.
The client has ONE font asset, so this is TMP's synthesised styling rather than a second typeface —
**if it still reads as "just the name again", say so and I will bake a real font.** -> 
`59h` [] - **C17 — staff titles.** On an admin account the Rank window's Titles tab offers **«Game
Master» — staff**; a moderator gets **«Moderator» — staff**. They are **opt-in** like every other
title (nothing is worn until you pick it) — tell me if you would rather staff wore theirs
automatically. -> 
`59i` [] - **C17 — `/role` takes effect live.** Promote a logged-in character to moderator: the title
appears in their picker without a relog. Demote them while they wear it: it comes straight off. -> 

### The titles you asked for on 2026-08-07 (after the queue was built)

`59k` [] - **NPCs wear their role.** `Elder Marius` plates as **`Elder`** over **`Marius`**. Check the
multi-word ones too — **High Priest Oren**, **Spirit Helper Nyra**, **Class Master Vael**,
**Grandmaster Thorne** — they split at the LAST space, so only the personal name should be on the name
line. ⚠ **A MOB must NOT split**: "Ridgeback Pup" stays one name. -> 
`59l` [] - **The full name survives everywhere it should**: quest text, the dialog header and the
target frame still read the whole "Elder Marius". -> 
`59m` [] - 🔴 **`/target Pell` works in a crowd** — the thing you actually asked for. Also try
`/target Gatekeeper` (the role half) and `/target Pel` (a prefix). It only finds what is IN SIGHT. -> 
`59n` [] - **`/title` is refused below 76**: a low character gets "you have not been granted the right
to name yourself". -> 
`59o` [] - 🔑 **The right arrives at level 76, with Angel's Protection** — your ask. Level a character
to 76 (or log in one already past it): **one** system line offers `/title`, and the Rank window grows
the hint. It must NOT repeat on every login. ⚠ Both grants now come from one place, so **the future
quest replaces one condition, not two**. -> 
`59p` [] - **Then it works**: `/title Bonecrusher` sets AND wears it in one step, `/titlecolor violet`
recolours it, `/title` with no text clears it. `/titleright <name> on|off` is the manual override
(⚠ **online characters only**). -> 
`59q` [] - 🔴 **The reserved words hold**: `/title Warlord`, `/title wealthy`, `/title Game Master`
are all refused. This is the rule that keeps a board title worth earning — if any of them gets
through, that is a bug, not a nitpick. -> 
`59r` [] - **20 characters max**, and letters/digits/space/`'`/`-` only. Try `/title <color=#FF0000>x`
— it must be refused, or a title could recolour itself past the palette. -> 
`59s` [] - **It survives a relog**, and the picker offers it back as **«your title» — your own** after
you switch to a board title and want it again. -> 
`59t` [] - **Revoking works**: `/titleright <name> off` takes a worn written title straight off the
head. ⚠ **On a 76+ character it comes BACK on the next login** — the level gate re-grants it. Say if
you want a revoke to stick; it costs one more column. -> 
`59u` [] - ⚠ **Protocol is 13.** Install the 0.55.0 APK *and* server. (An older APK draws NPCs as a
bare "Marius" and every title in plain gold — expected, not a bug.) -> 

---

## 60. 0.56.0 — D5, the Combat feed in its own window

No section in `TestChecklist.Unity.md` — the detail is here. **No schema change; `game.db` is fine.**
⚠ **Protocol is 14** — server and APK together. (An older APK has no case for the new channel and
prints loot/exp as plain Local chat: noisy, never lost.)

`60a` [] - **The System tab is quiet now.** Kill something with the Chat window open on *System*: no
damage lines, no `You looted:`, no `Exp: +…`. Only real system lines (refusals, learn notices) land
there. -> 
`60b` [] - **The 6th button opens a WINDOW, not a tab.** Chat → **Combat**: a second window appears
(bottom-right) and the Chat window stays open and usable beside it. The button stays lit while it is
open, and goes dark when you close either one. -> 
`60c` [] - **Colours.** Your own damage is **green**, the mob's damage to you is **red**, loot is
gold, the `Exp/SP/Gold` line is blue. ⚠ The green is deliberately *deeper than lime* (your words) —
say if it now reads too close to the System tab's green. -> 
`60d` [] - **All stays readable.** Fight for a minute with the Chat window on **All**: combat is
**not** in it. That is on purpose — All would otherwise be the exact wall of damage the window was
built to get away from. **Tell me if you would rather All showed everything.** -> 
`60e` [] - **Two Clears, two scopes.** Combat's **Clear** empties only the combat window; the Chat
window's **Clear** still empties everything (including combat). Say if you want Chat's Clear to
spare the combat feed too. -> 
`60f` [] - **Party loot still names the taker.** In a party, `X looted Y.` lands in the *combat*
window of the members who did not get it. -> 
`60g` [] - **No lag spike.** Spam a fight with **both** windows open — the rewrite that made the
console append-only now serves two views, so this is the one that would show a regression: rows
drawing over each other, a freeze, or the phone heating. -> 
`60h` [] - **It resets per character.** Leave to character select and enter on another: the combat
window is empty (it follows the C1 chat reset, not the System tab). -> 

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
- ~~**`D5` the [Combat] chat tab in its own window.**~~ ✅ **BUILT 0.56.0** — test at §60.
- ~~**`M5`/`M6` the tutorial chain + bound 30-day newbie gear.**~~ ✅ **BUILT 0.54.0** — test at §58.
- ~~**`C1` `C3` `C14` `C15` `C16` `C17`** — the QoL six you picked.~~ ✅ **ALL BUILT** — `C15` rode
  along in 0.54.0, the other five are 0.55.0. Test at §59. The queue is now down to **`B8`, `B9`,
  `B10`** (listed above) — and, ahead of any of them, **a play pass**.
- **`C4`** auto-on for buff potions/scrolls — **your ruling: comes later, with the AutoPot tabs.**
- **`G2` / `0e` `lb_*` + `wc_*`** — **CLOSED by your ruling: leave them.** Placeholders for 40+,
  commented out, harmless. I will stop asking.
- **`D4` `G5` `F1` `V1` `G4`** — **done and tested at 0.49.0.** Older docs still list some as open;
  they are stale on this point.
- **Crafting (`D3`)** — designed, unbuilt, and still the top content blocker. **3rd/4th class kits**
  — blocked on your 40+ CSVs. **`G3` mobs-as-players** — needs the document + BalanceMatrix tables
  first, then 2-5 real mobs as an experiment, per your ruling. **Instances** — you are holding.
- **The champion's −10% P.Def** (was −20%) — owed by **you**, on a re-test.
