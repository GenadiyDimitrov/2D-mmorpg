# OPEN CHECKLIST — everything untested as of **0.61.0** (2026-08-12)

> **Rolling and unversioned.** Playtest-21 (your 2026-08-11/12 pass over eight builds) is closed and
> transcribed verbatim into [Playtest-Archive.md](Playtest-Archive.md#playtest-21) — this file has been
> rewritten against the one build that came out of it (§70-§76). Every answer you gave is preserved
> there; everything you left open is either below or has an id in [Backlog.md](../Backlog.md).

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids — 64 of them, swept out of playtests 4-21
so that a feature ask stops dying under a bug pass. When a row below says *"→ `BL-14`"*, that means
the thing is known, written down and queued, and you do not have to report it again.

---

## ⚠ BEFORE YOU START

**Install `L2Clone-0.61.0.apk` and unzip `Game.Server-0.61.0.zip`.** Protocol is still **16** — it
last moved at 0.59.0 and nothing since has changed the wire shape; the new fields are all additive.
Install both halves together anyway: the catalogs, skill tables and world outlines are compiled into
each side, so a mismatched pair disagrees *quietly* instead of refusing. This build is mostly catalog
numbers (the shield cut) plus **55 new items**, which an old client would simply not know about.

🔴 **MOVE `Game.Server/game.db` OUT (+ `-shm` + `-wal`) — this build needs it.** `58d` added five
per-instance columns to the item table (sell price, tradable, custom name, and the two warehouse
flags) and `EnsureCreated` does not add columns to an existing DB. An old file loads with the columns
missing, which is exactly the failure `58d` was built to prevent: a bound item comes back ordinary and
looks perfectly right until the moment you can sell it.

⚠ **ONE BUILD IS UNPLAYED, but it is a wide one.** 0.61.0 is the whole playtest-21 fix batch —
**every shield in the game changed**, the **start quest was re-specced end to end**, the training tier
was re-authored, and enchant scrolls dropped 30× less often. On top of that sit two features you asked
for by name: **item tags + `/give`** (§75) and the **premium reward runes** (§76).

✅ **Pre-flight is clear.** `tools/SmokeTest` was re-run against the **0.61.0** server — **ALL CHECKS
PASSED**, including its new §8 item-tag round-trip and §9 reward-rune assertions. The server was
boot-checked too (it starts, every world validator runs — including the new `ValidateRunes` — log
clean).

## Where to spend the pass, if you don't do all of it

In order of how expensive a defect would be to find later:

1. **§70 the shield cut.** Every shield in the game lost **80% of its flat defence** at once and
   Shield Mastery gained 5× to pay for it. `70a` (a tank still tanks) and `70e` (a mage no longer
   tanks) are the two numbers this whole build stands on. Get them wrong and every defence number
   after this build is wrong with them.
2. **§71 the start quest.** It is the one path *every* new character walks, and it has now dead-ended
   on you twice. Roll a brand-new fighter AND a brand-new mage — the class split is new code.
3. **§76 the reward runes** — 55 new items and a payload that multiplies exp, SP, gold and drops.
   `76d` (the rungs never stack) and `76f` (a stop really stops) are the ones that matter; a rune that
   silently pays double is not visible from inside the game.
4. **§75 item tags + `/give`.** This is your route to handing out anything, so the tag has to survive
   a relog. `75f` is that row.
5. **§72 the training tier** — small, but it is the rung that has drifted above the ladder four times.
6. **§73 the dummies**, which have never once worked. They are now the only way to measure a hit rate
   without a real fight.
7. **§74** the five small ones, and everything else.

---

## 0. ANSWERS I OWE YOU — read, don't test

- ✅ ~~**`62h` the champion vs the rogue.**~~ **CLOSED by your ruling** (*"the champion have enough
  Patk boosts skills/passives while dagger rely purley on blows"*). Nothing was retuned: the 2H stays
  at 325 attack speed and its P.Atk stays as your CSV authored it. 🔑 **I will not raise 2H P.Atk "to
  fix the champion" in any future pass** — you have now ruled on it twice. The measurement still owed
  is `0a`, and it measures the *kits*, not the weapons.

- ✅ ~~**`67m` the wood shield's 30% reduction.**~~ **CLOSED — it was a real miss.** The Wooden and Iron
  Shields were hand-authored and the 0.59.1 block re-cut only touched the generated rungs, so both were
  left carrying the old profile. Both are on the tier profile now (§70).

- ✅ ~~**`63i` the Rune of Tincture on the Apothecary shelf.**~~ **CLOSED — removed**, per *"remove the
  rune from apothecary (add it to admin) and it will be only event/premium bought."* Admin → Items
  still has it.

- 🔴 **`62j`'s new number is the ONE thing in this build I could be wrong about, and it is yours to
  rule on.** Your data pinned the bug exactly: the E scroll was `0.40 × EnchantShare(0.15)` = **6% of
  every kill**, and it was the only rung live below level 40 — which is how you had 80 scrolls by 28.
  `EnchantShare` is cut **30×** to 0.005, so E is now **0.20%** · D 0.15% · C 0.10% · B **0.075%**.
  The rung *shape* is untouched, and boxes, elites and bosses are untouched. ⚠ **B at 0.075% may be
  too thin** — that is roughly one scroll per 1,300 kills at a level where you kill slower. See `74e`.

- **`Robe 611` is still `[NOT BUILT]`** — the only authored body in `gear_sets.csv` with no item behind
  it. Third pass in a row it has been left alone. → `BL-27`. Say the word if you want it real.

- **The heavy sets' shield clauses are unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 / x1.30`),
  so after the 5× cut they buy a fifth of the absolute defence they used to. That is arithmetic, not a
  decision — I left them alone because changing them *and* the base in one build would make `70a`
  unmeasurable. Rule on them once you have played a tank.

- **`67t` the training weapons** — you said *"we can leave only a sword and a wand the others are
  useless."* Done: `training_club` and `training_knives` are deleted, `training_bow` was already gone.
  Sword and wand are what the boxes now hand out, by base class.

---

## My Finds

- [] -

- [] -

- [] -

- [] -

---

## 70. THE SHIELD CUT — your option 3, complete

Your find, your three options, your pick: *"Im leaning to .3 - I like how it looks and sound."* The
double-dip was real — a shield's `ShieldDefense` was folded into physical defence **permanently**, so
it already paid on every hit, and a block then took another 34-47% off on top.

- `70a` [] - 🔴 **THE NUMBER THE BUILD STANDS ON: a tank must still tank.** Shield defence is cut **5×**
  (`90 143 203 230 256 299 413` → **`18 29 41 46 51 60 83`**, F→S — your 61 rung lands on your own
  worked 51) and **Shield Mastery's passive is raised 5×** to pay for it (`ShieldDefPct` 0.30/0.30/
  0.40/0.40 → **1.50/1.50/2.00/2.00**). Measured on BalanceMatrix, a 1H+shield tank's P.Def goes L20
  625 → **498**, L52 1031 → **801** — about **−19% throughout**, with survival at L20 186s → **133s**.
  Play a tank and say whether −19% reads as "still a tank" or as "the tank died". ->

- `70b` [] - 🔑 **Only the Mastery scales, per your ruling** — *"sheild\_mastery.Shield\_PDef will be the
  only part that will increase 5 times the sheild chance, arrow defence and other passives, sets and
  buffs that increase the shieldPdef/chance etc are kept as is."* So the Mastery **buff** (+50%),
  `BlockChancePct`, `BowResist` and the sets' `shield.p.def x1.25` all keep their old numbers and
  therefore buy a fifth of the absolute defence they used to — **on purpose**. Written into all three
  places it lives so it cannot drift back. ->

- `70c` [] - **Block BEHAVIOUR is bit-identical to 0.59.1.** Only the flat defence moved. The
  block-reduction coupling was rescaled to match (`BlockReduction += ShieldDefPct * 0.2f` → **`* 0.04f`**,
  so 0.04 × 2.00 is the same +0.08 that 0.2 × 0.40 used to give). If blocking *feels* different, that is
  a defect, not the design. ->

- `70d` [] - 🔴 **`67m` the Wooden and Iron Shields were MISSED by the 0.59.1 re-cut** — hand-authored,
  so the generated pass never touched them, which is why the wood shield still carried 30% reduction.
  Both are on the tier profile at their own grade now: Wooden `.10/.10/.03/3`, Iron `.15/.10/.05/5`.
  Their defence went 35 → **7** and 90 → **18**. ⚠ **`shield_iron` is DELETED** per *"Iron sheld can go"*
  — nothing should reference it. ->

- `70e` [] - 🔴 **The point of the whole exercise: a shielded mage is mortal again.** *"Mage should not
  be immortal even with a shield it helps a bit but not 47% dmg reduction with 33% chance."* Play a
  shielded mage and a shielded tank back to back — the gap should now read as a **class difference**
  rather than "everyone who holds a shield is fine". ->

- `70f` [] - **`68c` the shield enchant is +3 per level, not +9** — *"the shield enchant should become
  an armor +3 not +9 (i was considering that it worked only in block state)."* Same rule as armour now.
  Enchant one and watch it climb by exactly 3. ->

- `70g` [] - **The `Shield def:` row is GONE from the stat sheet** — *"this way we can remove the
  `Shield def:` row in stats"*, because the shield's defence is simply part of P.Def now. The DTO still
  carries the field; nothing on screen should show it. ->

- `70h` [] - ⚠ **`67p`'s evasion penalty is untouched** (`3 5 5 7 7 10 10`). Your note about a 1H rogue
  trading ~6% reduction for −10 evasion still stands, and it now trades for **less defence than it
  did**. Left deliberately — it is one number and it wants your eye after `70a`, not before. ->

---

## 71. THE START QUEST — `63j`, re-specced end to end

Your longest single comment of the pass, built as written. ⚠ **Roll a brand-new character to test
this, and roll one of EACH base class** — the box contents are class-conditional now, which is new
code on a path every player walks.

- `71a` [] - 🔴 **Creation grants NO boxes.** *"Make no inital boxes."* A new character starts with
  potions only. That was the source of your three weapons and three armours. ->

- `71b` [] - 🔴 **The boxes arrive from the QUEST, and only when the step needs them.** Part 1's first
  step carries the two training boxes, so they land when Huntmaster Cera gives you the quest, and the
  box beat carries them again. Granting is idempotent by construction — *"you hold none of this id"* is
  the only condition — so relogging, re-entering the step or talking to Cera twice can never hand you a
  second one. ->

- `71c` [] - **The training boxes are PLAIN, not selections, and they know your class.** *"armor box ->
  mage gets robe, fighter gets light; weapon box -> mage gets wand, fighter gets sword."* Built as a new
  `BoxEntry.ForClass` filter applied in the random path, the selection *offer* **and** the selection
  *confirm* — the confirm is the authoritative one, so a client that offered the wrong thing still
  cannot grant it. Open both on a fighter and on a mage. ->

- `71d` [] - 🔴 **Part 1's steps are your order, one beat at a time**: talk to Pell → **open a box** →
  **equip ×2** → **use Pell's list to travel** → **put something on the bar** → **target and use it** →
  **kill 5 pups** → reach level 3 → back to Cera. Two of those are brand-new step types: `Teleport`
  (credited in the teleport handler) and `AssignBar` (credited when the client reports a **player** edit
  of the bar, so the skill-bar rule holds). ->

- `71e` [] - 🔴 **THE FIGHTER DEAD-END IS FIXED: a BASIC ATTACK now credits the "use it" beat.** *"Fighter
  dont have a skill (I had to use TestSkill) to continue with quest."* It was credited only from a
  completed **cast**, and a level-1 fighter has nothing to cast. Roll a fighter, hit a pup with a plain
  attack, and the step must tick. ->

- `71f` [] - 🔴 **THE AUTO-FARM DEAD-END IS FIXED, and the root cause explains `63a` too.** *"Cannot
  complete the Auto-on part of the quest ... nothing works to allow me to continue."* The client's Auto
  button stopped calling `ToggleAutoHunt` when it was changed to push the whole config — and only the
  toggle handler credited the step, so **the step could not be credited by any button on the screen**.
  The config handler credits it now. That is also why `63a` showed you the reward and never finished. ->

- `71g` [] - **The travel step really lands on the pups.** Brackenford's first gate is `Lv 1-4 ·
  Ridgeback Pup, Fox` (asserted in SmokeTest), so *"Use Pell to go to the pigs"* puts a target in front
  of you rather than leaving you in town. ->

- `71h` [] - **`training_club` and `training_knives` are DELETED** — defs, constants and shop rows.
  *"Other training Club and training knives can be deleted."* Nothing should reference them. ->

---

## 72. THE TRAINING TIER IS WRITTEN DOWN

🔑 **This rung has drifted above the F ladder four times** (training armor 2026-07-24, the Wooden
Shield 2026-07-31, its block profile in 0.59.1, and now the jewels) — because it is the ONE tier that
is hand-authored instead of generated from a column. So the fix is not just the numbers.

- `72a` [] - **Broken jewels are your 9 / 5 / 3** (necklace / earring / ring; they were 15 / 11 / 7).
  Your find: they beat **F Common in all three slots and F Uncommon in two**, which made the starter
  reward better than the first thing you buy. ->

- `72b` [] - 🔑 **`gear_sets.csv` now has a TRAINING TIER block**, with every hand-written starter piece
  on its own row **directly above its F row** — so the next time you edit an F rung, the row above it is
  the thing that used to silently outrank it. This is the actual fix for the class of bug; the numbers
  are just this instance of it. ->

- `72c` [] - **The Wooden Shield is proper training kit** — untradable, sell price 0, 400 gold, and on
  the tier block. It is the shield a new character is *given*, not one they should keep. ->

---

## 73. THE TRAINING DUMMIES ACTUALLY WORK NOW — `63h`

*"Both dummies act as the old - they dont do nothing different."* They were inert for **three**
reasons, not one, and the first is the one worth remembering.

- `73a` [] - 🔴 **Nobody was ever inside the strike radius.** `DummyStrikeRange` was authored at your
  literal **50**, but a melee attacker is walked to `MeleeRange` = **80** and stops there, and a caster
  stands at 600. Now **150**. 🔑 *A reach authored from a design note must be checked against the
  stop-distance the movement code actually produces.* Stand in front of one and you should take ~10
  hits per second. ->

- `73b` [] - **All three dummies were the same plate.** The spawner hard-coded `"Training Dummy (Lv N)"`
  for every one of them, so `dummy_magic` and `dummy_physical` were indistinguishable — that is exactly
  what you were looking at. Each now shows its own name. ->

- `73c` [] - **Your titles: `Normal` / `Physical` / `Magic`**, on a new `MobType.Title` field. No client
  change was needed — the nameplate already draws a title for any entity. ->

- `73d` [] - ✅ **`69d` MAGIC EVASION IS FINALLY TESTABLE.** The magic dummy feeds the +4 fail-chance
  channel you ruled on, and Fails were observed on the wire. **Stand in front of `dummy_magic` with
  Evasion Boost up and without it** — 99% success should become ~95%. This is the row `69d` was waiting
  for. ->

- `73e` [] - 🆕 **RANK TITLES, your ask mid-session.** Elites wear **`Elite`** in red and **lost the
  `Elite ` prefix from their name**; the valley treant is **`Field Boss`** in aqua; the grave lich is
  **`Dungeon Boss`** in orange. Bosses keep their `Lord` suffix. 🔑 Field vs dungeon is read from the
  **coordinates**, reusing the existing "dungeons are the negative quadrant" rule, so there is no second
  flag to drift out of sync. ->

---

## 74. THE FIVE SMALL ONES

- `74a` [] - 🔴 **`65d` the target window — root cause was SERVER-side, not the client.** *"i select a
  target and select my next target manually before the first dies -> then the 1st dies and closes my
  second."* In manual play the autopilot pushed a live-target message **every tick**, and a null push is
  a *revocation of a selection the server never made* — so killing A wiped your manual B. Manual play may
  now only ever **hand a target over**, never take one away. The client drops its own target on the
  **alive→dead transition** (your *"'DIES' not 'DEAD'"*), so tapping a corpse still sticks. ->

- `74b` [] - 🔴 **`67i` was bigger than the line you saw.** *"it shows the flat +200 crit dmg in the stats
  but the Leather armor descritpion dont have it."* Five channels had **no formatter line at all** —
  `CritRateFlat`, `CritDamageFlat`, `MagicResist`, `PvpDamageTakenPct` and `AccuracyPct` — so **every S
  set in the game described itself with numbers missing**, not just that one. 🔑 *Appending a field to
  `StatMods` requires a text line in the same commit; nothing fails loudly when the formatter is missing
  one.* Read all three S set descriptions. ->

- `74c` [] - **`68h` F-grade gear says `Unenchantable`** instead of a "Per enchant +N" line — *"F grade
  should say unenchantable or atleast remove the + bonus - u cannot get it."* Gear slots only. ->

- `74d` [] - **`66n` the x500 mats freeze, root cause.** The admin give enqueued **one command per unit**
  — 25 materials × 500 = **12,500 commands**, each granting one item and re-serialising the whole
  inventory. Quantity rides on the command now: a stackable is a single add, only real gear loops, one
  inventory push at the end, and the whole thing is clamped to 10,000. **Spam the x500 button.** ->

- `74e` [] - 🔴 **`62j` the enchant drop cut — the number to rule on.** 30× down: E **0.20%** · D 0.15% ·
  C 0.10% · B **0.075%**. Farm a while below 40 and tell me if scrolls now feel like *"for over farm not
  a casual one"* or like they stopped existing. ⚠ **B is the rung I would expect to be wrong.** ->

- `74f` [] - **Auto-farm respects a skill's weapon requirement.** *"in auto farm the char uses stab and
  strike with blunt or knives ..while in manual use it's declined."* The chain checked cooldown, MP and
  HP but never the weapon gate or the HP-below condition. Both are checked now, as a **skip** — the
  cursor moves to a skill that *can* fire rather than stalling. ->

---

## 75. ITEM TAGS + `/give` — `58d`, your design

*"It is a REAL item with tags — never a new server-side def."* Built so you can hand out a Rune of
Sinners, but it is the general mechanism for handing out anything.

- `75a` [] - **An instance carries five properties of its own**, each `null` meaning *no opinion, use the
  catalog*: **sell price** (−1 = unsellable) · **tradable** · **custom name** (20 chars) · **can store
  private** · **can store account**. ->

- `75b` [] - 🔑 **The tag on the card is DERIVED, never stored.** Sellable + tradable shows nothing;
  neither shows **"bound"**; sellable but untradable shows **"private"**; and a timer composes on top →
  **"(temporary, bound)"**. The three predicates live in `Game.Shared` and are called by the server
  *and* the item card — a display that quietly disagrees with the rule it describes is exactly how `67i`
  happened. ->

- `75c` [] - **The command**: `/give <player> <itemId> [sellPrice] [tradable] [timed] ["name"] [enchant]
  [canStorePrivate] [canStoreAccount]`. Everything after the item id is optional and positional, `-`
  means *no opinion*, and **`1m` is one MINUTE** (your rule). `/give <player>` alone still opens your own
  bag as a picker. ->

- `75d` [] - **A tagged instance is ALWAYS a fresh bag row**, never merged into a stack — the tag belongs
  to that copy. Give yourself two differently-tagged copies of one item and confirm they stay apart. ->

- `75e` [] - **Enforcement reads the INSTANCE, not the def**, at the vendor, the trade offer, the private
  keeper and the account keeper. Try to sell, trade and store a bound one. ->

- `75f` [] - 🔴 **THE ROW THAT MATTERS: the tags survive a relog.** They are five real columns (which is
  why this build needs the db moved). *A bound item that comes back ordinary looks perfectly right until
  the moment it can be sold.* Tag something, log out, log in, check the card. ->

- `75g` [] - 🔑 **The two storage flags are deliberately separate, not one "storable".** The two banks
  answer different questions: an ordinary bound item is barred only from the **account** keeper, while
  Sinners has to be barred from **both**. ->

---

## 76. PREMIUM REWARD RUNES — `BL-01`, your spec

*"We need premium rune that stops the exp gain (so a grinder can grind and no lvl up)"*, a ladder
*"from 0,1~2 over 0.1 -> 1.1,1.2 ... x2"* plus your +5% rung, and the two named ones. It rides entirely
on the rune machinery that already existed — a rune has granted a buff from inside your bag since the
War and Spell runes; the only new thing is the **payload**.

- `76a` [] - **Five channels, each ONE skill whose LEVELS are the rungs**: Rune of **Experience** ·
  **Skillpoints** · **Exp-SP** · **Gold** · **Drop**. Eleven rungs each — your +5%, then tenths to
  +100% — so **55 items**, generated from one table, with the percentage in the id (`rune_exp_20`). An
  id that states its own number cannot come to mean a different one later. ->

- `76b` [] - **Rune of Sinister** — no exp, no SP; **gold and drops untouched**. *"So a grinder can grind
  and no lvl up."* ->

- `76c` [] - 🔴 **Rune of Sinners** — all four channels zeroed, *"bound to your soul for the time it has
  left."* ⚠ **Untradable alone did NOT bar it from the private keeper**, which takes anything that is
  not a quest item — so it could have been parked there until it expired, which is the one thing it must
  not allow. New def-level **`SoulBound`**: refused by **both** keepers regardless of instance tags, so
  the punishment does not depend on whoever handed it out remembering the right `/give` arguments. Try
  to store one, trade one and delete one. ->

- `76d` [] - 🔴 **Rungs never stack — the best one wins.** They share a family key, so the strongest rung
  you hold is applied and a weaker running one is evicted; when a +100% expires, a +20% in your bag takes
  over by itself on the next pass. Rates are folded by **MAX, never summed**, so an Exp rune next to an
  Exp-SP rune is +50%, not +70%. **Hold two at once and watch the buff bar.** ->

- `76e` [] - **The drop bonus is a PARAMETER of the one rate function**, not arithmetic at a call site —
  so the kill roll and the target-inspect drop list ask the same function with the same player. 🔑 **A
  player wearing a Drop rune is *shown* the chance they actually roll.** Open a mob's drop tab with and
  without one. ->

- `76f` [] - 🔴 **A stop is a hard override applied AFTER the max**, so no pile of bonus runes can dilute
  a punishment. ⚠ The 1-SP floor had to be skipped when the channel is zeroed — without that, Sinister
  still paid 1 SP a kill. **Kill something wearing Sinners and confirm all four are zero.** ->

- `76g` [] - **A leveled buff's popup reads ITS OWN level's text now.** Every rung used to describe
  itself as +5%, because the popup read the skill's generic blurb. The bar square is named after the
  **item**, so it reads "Rune of Experience (50%)". ->

- `76h` [] - **Measured, BalanceMatrix §R (new).** +100% Exp **halves** the climb to 60 (18,737 →
  **9,368** kills) and drops lifetime trash gold to **0.50×** with it. +100% Drop = **×1.90** total sold
  value; +100% Gold only **×1.10**, because coin is a small share of what a kill is worth. Worth knowing
  before you price them. ->

- `76i` [] - **Startup refuses a broken rune.** A rune naming a missing buff skill, or a rung its ladder
  does not have, used to sit in the bag looking perfect and pay nothing; the server now fails to start.
  Nothing for you to do — this row exists so you know the failure mode is covered. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

None of these happens by accident; each needs a session aimed at it.

- `55f` [] - 🔴 **The big one, and still unanswered after three passes: farm a mage 10+ unbroken minutes
  at level 40+.** Your playtest-19 finding was *"mages run out of MP in 2-3 minutes"*. The Restore
  Spirit ladder was built to fix it. The design is "farm 30-40 min, rest a bit" — not free. **If it is
  still 2-3 minutes, the ladder is not the fix and I need to know.** ->

- `0a` [] - **Nuker vs champion** — and after `62h` this is now the *only* thing that can settle it, since
  you ruled the weapon-speed comparison was the wrong one. The nuker is ~19% ahead (0.84×). You said this
  needs an auto-farm run: *"when I leave the chars to play alone all measure."* ⚠ If you do it, do `32z`
  in the same sitting — auto-farm skill chains surviving a relog have **never** been tested, and if the
  chains misbehave the measurement lies. → `BL-18`. ->

- `32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
  ranks, assist-leader — and all of it **survives a relog**. ⚠ Newly relevant: `74f` changed what the
  chain does when a skill cannot fire (it skips instead of stalling). ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items are
  refused. ⚠ Now interacts with `75d` — a tagged item is always a new row, so it needs a free slot. ->

- `36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
  top. ->

- `25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. ->

- `13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT now lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**
This section used to carry those and they kept dying under bug passes; it now carries only the pointer.
The ones from playtest-21 you will most likely reach for:

| you said | id |
|---|---|
| the Stat-Swap tab, and the Mindwriter's misleading `(cost 25kk)` | `BL-03`, `BL-39` |
| the auto buff potion/scroll tab | `BL-04` |
| crafting at NPC masters, quittable professions, and the absurd output | `BL-05`, `BL-40` |
| skill evasion — *"normaly no1 can evade a physical skill"* | `BL-06` |
| physical-skill reflect and debuff reflect | `BL-07`, `BL-08` |
| a floor under the wrong-weapon magic penalty | `BL-09` |
| anti-magic / anti-physical mobs feeding the mRes ladder | `BL-11` |
| boss HP — *"260? He should have 520?"* | `BL-13` |
| mob attack speed from `InnateWeaponType` | `BL-14` |
| a partial Blessing Box pick returning a box for the rest | `BL-20` |
| spreading drops out — one mob's sword, another's armour, the ork settlement | `BL-21`, `BL-25` |
| the admin item picker as a selection box | `BL-56` |
| a cheap level-1 recipe for the Potion Master and Scroll Scribe | `BL-57` |

Still true and unchanged:

- **3rd/4th class kits** — blocked on your 40+ CSVs (`BL-02`). Nothing is invented in the meantime.
- **`G3` mobs-as-players** — needs the document and the BalanceMatrix tables first, then 2-5 real mobs
  as an experiment, per your ruling (`BL-47`).
- **Instances** — you are holding (`BL-48`). ⚠ **Dungeons are the cheap half** and can ship without them.
- **`G2` / `0e` `lb_*` + `wc_*`** — closed by your ruling: leave them. Placeholders for 40+, commented
  out, harmless.
- **Two playtest-20 bugs that were never answered or carried**: Frost Bind stripping a dummy's/elite's
  HP multiplier (`BL-63`), and your target being lost for the duration of a physical skill cast
  (`BL-64`). ⚠ `BL-63` is worth a minute during §73 — you will be standing at a dummy anyway.
