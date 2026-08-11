# OPEN CHECKLIST — everything untested as of **0.59.0** (2026-08-11)

> **Rolling and unversioned.** Playtest-20 (your 2026-08-10 pass over ten builds) is closed and
> transcribed verbatim into [Playtest-Archive.md](Playtest-Archive.md#playtest-20) — this file has been
> rewritten against the four builds that came out of it. Every answer you gave is preserved there;
> everything you left open is carried forward below.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

---

## ⚠ BEFORE YOU START

**Install `L2Clone-0.59.0.apk` and unzip `Game.Server-0.59.0.zip`.** Protocol is **16** — it moved at
0.59.0 for the crafting push, and at 0.58.1 before that. Both sides together: the catalogs, skill
tables and world outlines are compiled into each, so a mismatched pair disagrees quietly instead of
refusing.

🔴 **Move `Game.Server/game.db` out (+ `-shm` + `-wal`).** 0.58.1 renamed the DEX column to AGI and
`EnsureCreated` cannot migrate a rename — an old character would load with a missing stat. Everything
older still applies too (0.52.0 columns, the 0.53.0 God removal, the mastery clamp, 0.55.0's three
title columns).

🔴 **FIVE BUILDS ARE UNPLAYED — 0.58.0, 0.58.1, 0.58.2, 0.58.3, 0.59.0.** The four 0.58.x builds came
out of your own playtest-20 finds, so most of this pass is *"did he fix what I reported"*. Two of them
changed combat maths that everything else sits on: **the weapon speed table** (§62) and **how magic
lands** (§64). **0.59.0 is CRAFTING** (§66) — new ground, and the only section here that is a feature
rather than a fix.

✅ **Pre-flight is clear.** `tools/SmokeTest` was re-run against the **0.59.0** server — **ALL CHECKS
PASSED**, including its blueprint-craft assertions. The server was boot-checked too (it starts, every
world validator runs, log clean). 0.59.0 touches the login sequence, which is exactly what the smoke
test exists to guard.

🟡 **You said you are re-authoring the weapon CSV.** Every row below marked **⏸ CSV** is a number that
your new CSV will move — skip those rows this pass, they will only be worth testing after your file
lands.

## Where to spend the pass, if you don't do all of it

In order of how expensive a defect would be to find later:

1. **§62 the weapon speed table** — every DPS number in the game moved. A 2H now swings slower than a
   1H, which is correct but makes the champion the worst melee until your CSV raises 2H P.Atk.
2. **§64 magic landing** — a brand-new formula replaced a stat that did nothing. If the parity number
   is not ~1% fail, everything above it is wrong.
3. **§66 crafting** — the only new feature here, and the one thing with no prior play at all.
4. **§63 the four bug fixes** — the dungeon wall and the jail were rebuilt, not patched.
5. **§62 evasion** — your biggest find. Confirm the +32 is gone at the exact character you measured.
6. Everything else.

---

## 0. ANSWERS I OWE YOU — read, don't test

- `56e` - **"What is this Resonance?"** It is the **human Warchanter racial passive** (`wc_human_pass`):
  +10% max MP, extra MP regen, and ×1.2 magic crit. You met it on the admin level-90 Warchanter seed —
  it is not a general mechanic, it is that one class/race combination's passive.

- `61b` - **"Which vendor?"** You are right: **no vendor sells above D grade**, so the "Mythic S-grade"
  string in a vendor list is unreachable today. Not a defect, nothing to test.

- 🔴 **Piercing Stab's level-32 MP cost is 58**, sitting between level 24's and level 30's smaller
  numbers. `rogue 20-35.csv` line 19 says 58, so the code is faithful to your file. **I did not touch
  it — it looks like a typo in the CSV and it is yours to rule on.**

- 🔴 **Your 2H S-row disagrees with the 1H's.** You authored the 2H S row as **532/192**, but the 1H
  line derives **211** M.Atk at S — which breaks your own "2H M.Atk = 1H M.Atk" rule at exactly that
  one rung. Your new CSV can settle it; flagging so it does not get lost.

- 🔴 **The champion test is still owed by you.** Reverting the 2H P.Atk raise puts champion/nuker back
  at **0.84×** — the nuker is ~19% ahead. `0a` below is that measurement.

---

## 62. 0.58.0 — the evasion root cause + your four number rulings

Full detail: none in `TestChecklist.Unity.md`, it is here. **Your finds #2, #6, #12 and `49a`.**

**The evasion root cause (your find #2 — the +32).**

- `62a` [] - 🔴 **The +32 is gone.** Build the exact character you measured: **Elf Phantom, level 60,
  35 AGI, light armour, no set, no weapon attribute, no buffs.** Evasion must read **108**
  (60 + 35 + 13), not 140. The cause was `Discipline.Phantom` carrying a flat `Evasion: 32` (and
  Trapper +12) from a placeholder table written before the evasion budget existed. ->

- `62b` [] - **At level 90 the same build reads ~147**, not 182 — your second data point. ->

- `62c` [] - **Evasion no longer jumps at the DISCIPLINE change (40).** That jump was the bug's
  signature. Take a discipline and watch the number: it must not move. ->

- `62d` [] - ⚠ **Phantom and Trapper did not just lose the budget — it moved to MaxHp** (Phantom +120,
  Trapper +45). Same survivability role, but through a channel that touches neither the accuracy
  contest nor any damage number. Say if you would rather it went somewhere else. ->

- `62e` [] - **The new rogue ultimate `Evasion Boost` exists at 28** — +20 Evasion, cancel-resist ×1.8,
  30s, 900s reuse, 20 MP. ⚠ Two channels in your CSV were **NOT built**: "skill evasion ×1.25" and
  "magic evasion ×1.1". The game has exactly one evasion channel; dodging a physical *skill*
  separately, and dodging *magic* at all, are new mechanics — I left them out rather than fake them.
  **Tell me if you want them as real mechanics and I will build them.** ->

**The weapon speed table (your find #12).**

- `62f` [] - 🔴 **A 2H is now slower than a 1H.** Attack speed base by type: **Dual 433 · 1H sword 379 ·
  1H blunt 379 · 2H sword 325 · 2H blunt 325 · bow 293 (very slow bow 227) · weaponless 300** — your
  numbers exactly. The bug was that the code folded 2H into 1H before reading the table. ->

- `62g` [] - **Cast speed is ×1 for every weapon type.** It was blunt 1.0 / everything else 0.8, which
  double-charged the wrong-weapon rule (Spellcaster Mastery owns that) and contradicted your profile.
  Hold a sword, a blunt, a staff: cast speed must differ only by your passives. ->

- `62h` [] - 🔴 **THE CONSEQUENCE, and it needs your ruling.** Rogue duals vs champion 2H, same-level
  mob: rogue/warrior went **0.92× → 1.05×** at level 20 and **1.22× → 1.37×** at 36. The rogue's DPS
  did not change — the champion's fell ~14%, purely from 325. **The rogue now out-damages the champion
  at every level 20-36.** In the inspiration game a 2H compensates with much higher P.Atk; ours does
  not. ⏸ **CSV** — your new weapon file is the fix, but confirm you agree that is where it belongs. ->

- `62i` [] - ⚠ **Mobs were pinned to 433, deliberately.** Almost every mob is weaponless, so your
  "weaponless 300" would have slowed **the entire bestiary by 31%** (BalanceMatrix: a level-20
  champion's survival time went 126s → 189s). Mobs kept 433. One constant to change if you want the
  bestiary on 300 — but check how the game feels first. ->

**Enchant rates (`49a`).**

- `62j` [] - **The new rates: +1..+3 safe (100%), +4..+9 66%, +10..+15 33%, +16 5%.** Your budget said
  ~51 safe scrolls for +0→+16; the formula gives 3 + 6/0.66 + 6/0.33 + 1/0.05 ≈ **50**. Enchant
  something a long way and see if the pace matches what you pictured. ->

**Raid bosses (your find #6).**

- `62k` [] - **Boss HP ×20 → ×100** and a new flat **Accuracy +20** by rank. A dodge build should no
  longer be able to stand in front of a boss untouched — that was the missing accuracy. ->

- `62l` [] - 🔴 **One number was NOT taken literally, and you should check it.** You said "PAtk from x5
  → x20", but the boss rank multiplier has always been **×2.5** — there is no ×5 in the boss path.
  Taking ×20 literally would be an 8× jump, not the 4× your own before/after describes, so I applied
  your **ratio**: **2.5 → 10**. Kill something and tell me if it hits as hard as you meant. ->

- `62m` [] - **The "HP boost passive ×2" needs no new code** — `MobMod.Hp` already multiplies and the
  Max HP Mod table reaches ×2 at rung 11. Put that mod on a boss and you get your 500-600k. ->

**Your CSV syncs.**

- `62n` [] - **Tank Defencive Wall is 30s**, not 60. ->

- `62o` [] - **Rogue light-armour mastery speed is flat +7**, not ×1.07. ⚠ `StatMods.MoveSpeed` existed
  but **nothing read it** — it is consumed now, flat before percent, rungs 3/4/5 at 28/32/36. ->

- `62p` [] - **Bow Expertise moved 28 → 36.** ->

---

## 63. 0.58.1 — the last four bugs, the QoL six, AGI, and the stat-swap rework

⚠ **This is the build that needs the db moved and a new APK (protocol 15).**

**The four bugs.**

- `63a` [] - **`#11` quest reward details stack.** 5 Dash Potions render as one row reading `x5`. The
  grant path was always right; this was display only. ->

- `63b` [] - 🔴 **`61h` the Hollow Crypt has walls now.** The root cause was worse than reported: a
  dungeon's world was its outline's **BOUNDING BOX**, and the crypt is a diagonal band — **only 55% of
  that box is actually floor**. The world now carries the real outline. Walk at every edge of the
  crypt, including the diagonals. ⚠ Its **entrance is annexed** (the arrival safe zone sits outside
  even the old box — that was the rubber-band you felt). ⚠ Known limit: a straight walk can cut a
  concave corner (0.76% of pairs, ≤129 units) — there is no pathfinding, and both halves draw the same
  line, so it should never rubber-band. ->

- `63c` [] - **`61d` the jail is a room.** Your 300×500 box, **one shared jail, not a cell per player**,
  and arrivals are spread instead of every inmate landing on the same coordinate — that coordinate
  was the "1px". Jail two characters and confirm they arrive apart and can pace. ->

- `63d` [] - **`53a` box picks are a BUDGET.** They were a set of ticks, so 10 picks meant 10
  *different* scrolls and "5 of one" was inexpressible. Now: open the Blessing Box, use the `-`/`+`
  steppers, take **5 of one + 3 of another + 2 of a third**, or 10 of the same. Both of your shapes
  should work. ->

**The QoL six.**

- `63e` [] - **`52b` the skill card reads the FLAGS, not the prose.** It printed the authored
  description, which is why Piercing Stab's was stale. It now prints blow / double / crit / flat /
  magic-crit in the resolver's own order, for every skill. ->

- `63f` [] - 🔑 **`53e`+`61j` the rubber-band on STOP — root cause found.** The decaying error is right
  for a walk that is **interrupted** and wrong for one that **ends**: at arrival the server stream lags
  one sample, so the leftover error points *forward* while the base position is still advancing on the
  same point — the sum walks past the destination and the ease-back is the decay. An arrival now
  **holds** at the destination. Walk somewhere and stop, many times. ->

- `63g` [] - **`54e` `/stat <name> <value>`** — your admin override, every stat. `/stat acc 999999`,
  `/stat eva 99999`, crit damage, crit rate, 12 stats in all, plus `m`/`a`/`c` routed into the
  existing `/spd` fields so it is one command. **`/stat` alone clears everything.** ⚠ `/spd` is not
  regressed — check it still works. ->

- `63h` [] - **`56c` the training dummies.** `dummy_magic` and `dummy_physical`, level 80, on the
  training row at x≈26500/27500. 1 damage per tick within 50 units, through the **real** resolvers.
  Stand there 10s = ~100 hits. 🔑 **A mob's WIT is a flat 5 at every level, so its magic crit is
  1.25%** — that is the number you were trying to observe, so expect roughly one crit per 80 hits. ->

- `63i` [] - **`59r` `/title` defaults to WHITE**, and colour is gated on a **Rune of Tincture**
  (Apothecary, 40k) — click the rune to open the colour list. ⚠ **The rune is NOT consumed**: your
  words made *possession* the right, and a one-shot item could not be clicked twice. Say if you meant
  it to burn. ->

- `63j` [] - **`58a` the tutorial teaches before the pigs.** A new step type waits until you actually
  *did* the thing, credited from the handlers that already run: part 1 now asks you to open a box,
  equip twice, and use a skill; part 5 has auto-farm at your "reach 18" slot. ⚠ Two of your asks did
  **not** become steps: it asks **one** box (a player who opened both creation boxes could not conjure
  another), and the rune explanation stayed **prose** (an "open Miren's rune box" step would gate the
  whole chain on her daily). Run a brand-new character and tell me if it now teaches enough. ->

**DEX → AGI (your find #3).**

- `63k` [] - **Every player-facing DEX now reads AGI** — stat sheet, skill text, tooltips, docs. ⚠ **The
  four stat-swap skill IDs still spell `dex` on purpose**: an id is a persisted key, and renaming one
  would delete a 15kk purchase. Nothing you can see should say DEX. ->

**The stat-swap rework (your find #4).**

- `63l` [] - **A rung is +1/−1, and you have NINE of them, character-wide.** A skill is one pair, and
  its level is how many rungs you put in that pair. **Raise cap +5 per stat** (so a pair caps at level
  5); no cap on selling beyond the nine. ->

- `63m` [] - 🔑 **The direction rule is DELETED.** `+5 AGI −5 CON` then `+4 CON −4 AGI` is legal and
  nets `+1 AGI −1 CON` — your worked example. Self-cancelling is allowed on purpose. ->

- `63n` [] - 🔑 **Price is per RUNG and global: `1/2/3/4/5/5/5/5/5` kk by rungs already owned = 35kk for
  all nine, however you spread them.** Buy them in a few different orders and confirm the total is
  always 35kk. ⚠ That could not live in the per-skill gold field, so the charge is computed at
  purchase and **the client runs the identical computation** — if the two ever disagree you will see a
  price change when you tap. ->

- `63o` [] - **The tenth rung is refused, and so is `+9` into one stat** (the +5 cap). ->

- `63p` [] - **The reset NPC reports what forgetting a skill FREES**, not what it cost — under
  position-based pricing there is no per-skill answer. Say if that reads wrong. ->

- `63q` [] - **The class lists are yours**: fighter ATK↔AGI, ATK↔CON, AGI↔CON + one-way **+SPT −ATK**;
  mage ATK↔WIT, ATK↔SPT, WIT↔SPT, AGI↔CON; **cleric = mage**; buffer keeps all. ->

---

## 64. 0.58.2 — magic gets its own landing formula, and mRes becomes damage reduction

Full detail: `docs/design/CombatResolution.md`. **This is your `57d`, and it was a real bug.**

- `64a` [] - 🔴 **The bug you found: the fail chance was bit-for-bit identical with a bow and a wand.**
  Spellcaster Mastery's "magic accuracy ×0.5" halved a stat (`MagicFailResist`) that is **0 on
  everyone**, because no skill in the game grants it. Half of zero is zero. It was also pointing the
  wrong way — the stat could only ever *subtract* from fail, so nothing could raise a fizzle. ->

- `64b` [] - **The new formula is yours**: `fail% = round(1.3^(defLvl − atkLvl) × defMod × weaponMod)`,
  clamped to 95%. **Parity = 1% fail / 99% success**, your anchor. Cast a lot at a same-level dummy. ->

- `64c` [] - 🔴 **Now a bow caster actually fails.** `weaponMod` is **25** for bow/dual/bare-handed (you
  offered 25 or 50; 25 is your own worked example). Expected success — **wand: 99 / 96 / 86 / 61 / 5%**
  at Δlevel 0 / +5 / +10 / +14 / +18. **Bow: 75 / 45 / 7 / 5%** at Δ 0 / +3 / +5 / +6. Hold a bow at
  parity and you should fail about **one cast in four**. ->

- `64d` [] - **The clamp is 95%, not 100** — your playtest-19 "nothing is unhittable" ruling applies in
  this channel too. Even at a hopeless level gap, 5% of casts land. ->

- `64e` [] - ⚠ **The bow penalty FADES when you punch down.** The formula is multiplicative, so at
  Δ−10 a bow caster is back to ~98% success. That is inherent to your model — flagged, not patched.
  Tell me if you want a floor under it. ->

- `64f` [] - 🔑 **mRes was never a fizzle chance — it is DAMAGE REDUCTION now.** Your words: *"the
  problem was we didn't have a mdmg reduction, that's why we converted them to a floor."*
  `healer/nuker 20-35.csv` says "magic def +20, mRes +5%", so it is now a divisor inside M.Def, the
  same shape as pierce defence. ⚠ You wrote "1.25 → ×0.75"; I pushed back and you took the divisor, so
  **1.25 → ×0.8** — that keeps the mob ladder (1.11/1.25/1.43/1.67/2) exact reciprocals of 0.9…0.5. ->

- `64g` [] - 🛑 **Deleted, do not expect to find them**: `MagicFailResist`, `MagicFailFloor`, and the
  whole fizzle-FLOOR concept. Magic no longer calls the physical avoid roll at all. Physical is
  unchanged — sanity-check that a melee miss still behaves exactly as before. ->

- `64h` [] - **The tank's Anti-Magic is the `defMod` ×2**, and you already ruled the tank is fine
  (25% magic damage reduction vs the mage line's 30%). Lv2/Lv3 stay at 43/76 as ×2.5/×3 until your 40+
  kits land. **Nothing to decide — this row is here so you don't re-report it.** ->

- `64i` [] - ⚠ **No gear grants mRes yet, and no mob has a magic-resist entry** — the ladder exists in
  `mobs_passives.csv` but nothing feeds it. Expected gap, not a bug. ->

---

## 65. 0.58.3 — your weapon numbers + the bestiary tab

**Your queue item (1).** No schema change; **protocol stays 15** (the new field is additive).

**The weapon half — ⏸ CSV, you are re-authoring this. Skim only.**

- `65a` [] - **The ×1.166 2H P.Atk raise is REVERTED** — you ratified nothing, so it went back. ->

- `65b` [] - **The F rung of all 8 weapon lines is re-authored** from your file, and a caster's M.Atk
  now sits **above** its P.Atk at F. ->

- `65c` [] - **`training_bow` is deleted.** Nothing should reference it. ->

**The bestiary — your two UI asks from playtest-20.**

- `65d` [] - 🔴 **The target window no longer blanks when you retarget.** Tap Info on a mob, then let
  auto-farm pick a new one: the sheet **stays on the mob you opened**. It used to key off the current
  target, so the drop table you were mid-read of vanished. ->

- `65e` [] - **The title says `[pinned]`** once the sheet stops describing what you are actually
  fighting — so a held-open window can never quietly show the wrong creature's numbers. ->

- `65f` [] - **A third tab: Skills** (mob-only, like Drops), showing the creature's **actives and
  passives together** — the passives **moved here** from the Stats tab. Effects stayed in Stats,
  because those are what is on it right now. ->

- `65g` [] - **A mob with no kit says "None — this creature only attacks"**, and that is correct, not
  missing data: only casters and bosses are given skills at all. ->

- `65h` [] - **The numbers are level-resolved** — the server formats these lines, so the same spell id
  reads as a different power on a level-20 and a level-70 caster. Check the same mob at two zone
  levels. ->

---

## 66. 0.59.0 — CRAFTING is reachable at last (+ the admin gear list was lying)

⚠ **New APK — protocol 16.** No schema change; `game.db` is fine for this build alone (the AGI reset
from 0.58.1 still applies if you have not done it yet).

🔑 **Nothing about crafting is new except the window.** Professions, refinement, recipes, blueprints and
mats-primary drops shipped 2026-07-06 — the phone simply had no way to reach any of it. So if a NUMBER
here feels wrong, that number is old and unplayed, not something I just invented.

**Getting started.**

- `66a` [] - **Menu → Craft** opens the window, and with no profession yet it shows the five choices,
  each saying what it refines and what it makes. It is on the MENU and not at an NPC on purpose: every
  material rarity drops in the field and refining what you just picked up should not need a trip to
  town. Say if you would rather it lived at a craft NPC. ->

- `66b` [] - 🔴 **Choosing is PERMANENT and the confirm says so.** Pick one; the window switches to that
  profession's pages, and the choice **survives a relog** (it saves immediately now rather than waiting
  on the 60s autosave). Try to pick again — it must refuse. ->

- `66c` [] - **Admin → Class still sets the profession directly** and the craft window follows it
  without reopening. That is your bypass for testing all five. ->

**The pages.**

- `66d` [] - **Refine** — 5 of the same type + 2 cross-profession, one rarity up, guaranteed. This is
  the trade engine: your own type is the only one you can refine, so the 2 cross mats have to come from
  a drop or another player. First rung unlocks at **level 20**, then 40 / 61 / 76. ->

- `66e` [] - **Gear** — every set piece your profession makes. ⚠ A Weapon Smith has **62** of these and
  an Armour Smith **63**, ordered by level, most of them dimmed. Tell me if that is too long a scroll on
  the phone and I will add a grade filter. ->

- `66f` [] - **Goods** — a Potion Master's 12 potions, a Scroll Scribe's 6 scrolls (the D and C enchant
  scrolls plus the attribute pair). A smith's Goods page and a scribe's Gear page are correctly empty
  and say so. ->

- `66g` [] - **Mats** — every material with what you hold, laid out type by type, your own type marked
  `(yours)`. Ones you have none of are still listed on purpose: the thing you are short of is exactly
  what you would hide by listing only the bag. ->

**A craft.**

- `66h` [] - 🔴 **The ingredient that is stopping you is RED.** Open any recipe you cannot afford: each
  input reads `have/need`, green when you have enough. This is the whole point of the row — say if the
  numbers are hard to read at that size. ->

- `66i` [] - **A guaranteed craft goes straight through; a risky one asks first** and names the odds,
  because a failure still eats the materials. Potions are 90%, scrolls 80%, everything else 100%. ->

- `66j` [] - **A locked recipe is dimmed, not hidden**, and says which of the three reasons it is:
  *Needs level N*, *Blueprint not learned*, or simply red ingredients. ->

- `66k` [] - **Blueprints.** An A/S-grade set piece needs one blueprint to UNLOCK and **one more every
  craft** — so the first costs two. The blueprint is listed as an ingredient in the row, not just as a
  gate. Admin → Items → Blueprints gives you all 36. ->

- `66l` [] - ⚠ **Two professions can craft NOTHING until level 20** — Potion Master and Scroll Scribe
  have no level-1 recipe at all, so a fresh character who picks one gets an empty window. The three
  smiths have 6-14 recipes at level 1. Authored, not a bug, but it reads as broken — tell me if you want
  a cheap level-1 recipe for those two. ->

**The admin bug this build found.**

- `66m` [] - 🔴 **The admin Equip tab has been giving you 70% gear.** It filtered on `Epic`, which was
  right until the rarity ladder was re-anchored and the authored tables became the **Mythic** rung with
  every lesser quality a derived copy of it. "Level 76 → Adamantine Blade" handed you the *Epic copy*:
  **P.Atk 196 where the real item is 281.** It never looked broken because the Epic list carries the
  same levels 1/20/40/52/61/76/80. **Every balance number you took off admin gear since the re-anchor
  was ~30% light** — worth knowing before you re-measure anything. Fixed; check a level-76 weapon's
  details now read the authored number. ->

- `66n` [] - **Admin → Items → Crafting materials**: all 25 at x200, plus **give-everything x500**.
  Sized for a real craft — one E-grade body is 100 commons and 50 uncommons, so a x10 button would be
  theatre. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

None of these happens by accident; each needs a session aimed at it.

- `55f` [] - 🔴 **The big one, and still unanswered after two passes: farm a mage 10+ unbroken minutes
  at level 40+.** Your playtest-19 finding was *"mages run out of MP in 2-3 minutes"*. The Restore
  Spirit ladder was built to fix it. The design is "farm 30-40 min, rest a bit" — not free. **If it is
  still 2-3 minutes, the ladder is not the fix and I need to know.** ->

- `0a` [] - **Nuker vs champion.** They were at parity and the nuker is now ~19% ahead (0.98× → 0.84×).
  You said this needs an auto-farm run to measure — *"when I leave the chars to play alone all
  measure"*. ⚠ If you do it, do `32z` in the same sitting: auto-farm skill chains surviving a relog
  have **never** been tested, and if the chains misbehave the measurement lies. ->

- `32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
  ranks, assist-leader — and all of it **survives a relog**. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ->

- `36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
  top. ->

- `25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. ->

- `13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

- ✅ ~~**CRAFTING**~~ — your queue item (3). **BUILT 0.59.0, test at §66.** The server half already
  existed; what was missing was the client, and the admin materials/recipes you asked for came with it.
- **`58d` item TAGS + `/give`** — your design (timed / bound / private as tags on a **real** item
  instance, never a cloned def, plus the full `/give` command). **Not built.** It blocks `58g` and
  `59c`, which you deferred until it exists. It is also your route to Mythic gear without crafting —
  say the word and it jumps the queue.
- **`58i` purge the inspiration game's name** from comments and docs (`l2` as the game → `IG`; the
  *level* meaning stays). **Not built**, mechanical, no risk — I will fold it into a quiet build.
- **Your find #9 — resurrect / party / flag rules.** Ultimate Resurrection should be tradable (at
  least the dropped and admin copies); you cannot res a party member while **you** are flagged, though
  you can res or heal a PK while unflagged and it flags you (inconsistent); you must be able to invite
  and trade with PvP-flagged players, with PK staying trade-blocked. **Not built — queued.**
- **3rd/4th class kits** — still blocked on your 40+ CSVs. Nothing invented in the meantime.
- **`G3` mobs-as-players** — needs the document and BalanceMatrix tables first, then 2-5 real mobs as
  an experiment, per your ruling.
- **Instances** — you are holding.
- **`C4`** auto-on for buff potions/scrolls — your ruling: comes later, with the AutoPot tabs.
- **`G2` / `0e` `lb_*` + `wc_*`** — closed by your ruling: leave them. Placeholders for 40+, commented
  out, harmless.
