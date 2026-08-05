# OPEN CHECKLIST — everything still untested as of 0.48.0 (2026-08-05)

Edit this on the phone: write your comment after the `->`. Put `x` in the `[]` if it passed with
nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority, `?` for a question.
`[]` with no id in front is a free line for that section — add as many as you like.
Ids match `docs/testing/TestChecklist.Unity.md`.

**Before you start:** install the **0.48.0** APK. Protocol is **12**, unchanged since 0.46.0.
⚠ **If the last build you played was 0.45.0, DELETE `Game.Server/game.db` + `-shm` + `-wal`** — the
account farm-budget change in 0.46.0 added columns and `EnsureCreated()` will not add them to an old
file. Coming from 0.46.0 or 0.47.0, keep your save.

**Start with 48a.** It is the text-box bug that made 0.47.0 unplayable for you; if it is not fixed,
stop and tell me, because half of section 48 needs a typed value.

⚠ **Two things changed under you that are not bugs.** **Exp is x1 now**, not x10 — levelling is
genuinely ten times slower than every build you have played, and that is what you asked for
("I'll tune them if I need to", debug menu → rates). And **buff scrolls no longer drop at all**; they
come out of a 250k Apothecary box (48d-48h). Neither is a defect to report.

**This is a BIG unplayed batch** — five builds' worth. 0.46.0 and everything since has never been
played at all. Sections 46, 47 and 48 are the new work; everything under "CARRIED FORWARD" is what was
still blank in the 0.45.0 checklist.

---

## 🔴 0. DECISIONS I NEED FROM YOU (these block work, answer first)

0a [] - **G1, the skill deletion — the list I gave you was WRONG and I did not delete anything.**
`evade_mastery`, `precision` and `anti_magic` are **auto-granted to every rogue / warrior / tank** at
20/40/76, and `class_balance_*` to every character alive. They feed the sure-dodge / sure-hit /
magic-fizzle floors. They are missing from your CSVs only because they are auto-granted instead of
learned. **Genuinely dead and safe: `reflexes`, `archer_armor_mastery`, `archer_weapon_mastery`,
`dispel_magic`, and the Heavy Draw @24 grant.** Delete just those, or the live ones too? -> 

0b [] - **"God class + skills" — how wide?** `hp_boost` + `greater_heal` + the god learn table are
clearly in. But `Race.God` is your DEBUG RACE and `ItemRarity.God` / `god_judgment` / `god_robes` are
the debug gear your own admin menu hands out — deleting those takes your testing rig with them. I left
them alone. Right call? -> 

0c [] - **G5: I kept all SIX Dash rungs.** You named four (C15 U30 R45 M60) but Epic +50 and Legendary
+55 also exist and are in the drop tables. Cut the ladder to four, or keep six? -> 

0d [] - **G5: Sprint level 2 is learned at 40.** You gave the value (+60) but not the level, and the
authored rogue CSV stops at 36. Is 40 right? -> 

0e [] - **G2: keep or delete `lb_*` (8) and `wc_*` (12)?** They are the written-but-unreachable level-40
HEALER disciplines — Lightbringer (pure healer) and Warchanter (buffer). One commented line away from
being learnable when your 40+ CSVs land. My recommendation: **keep**. -> 

0f [] - **G3: mobs built like players** (no inflated STR/CON, real gear, player formulas). I measured it
— the player pipeline is the MIRROR of the mob curve and no gear combo closes the gap, so **type
passives per band** would have to carry it, which is your own spec. Do you want it built? -> 

---

## 48. THE 0.48.0 FIXES — the text box, x1 rates, and the buff economy (newest, unplayed)

**48a [] - 🔴 THE TEXT-BOX FIX — do this first, it is your "unplayable".** Tap a box that already has
something in it (the saved password on login, a rate row in debug, the server URL). The caret must land
**at the END**. Type `0` on a rate that says `1`: you must get **10**, never `01`. Backspace must delete
the last character, not do nothing. -> 

48b [] - **...and you can reach the middle.** Tap in the MIDDLE of the text in an already-focused box:
the caret must move there, so you can fix one character without clearing the lot. ⚠ If the keyboard
now fails to OPEN AT ALL on some box, say so immediately and name the box — that is the one risk in
this fix and it is one line to revert. -> 

48c [] - **Rates: x1 is the new default.** Debug → tuning: exp, sp and drop all read **1**. Kill
something and check the exp bar moves about a tenth of what you are used to. Drops themselves should
feel UNCHANGED (the x3 was folded into the groups, deliberately, so your measured economy survives) —
if gear feels three times rarer, that is a bug and it is mine. -> 

48d [] - **Buff scrolls do not drop any more. At all.** Farm anything, at any level, boss included: not
one `Scroll of ...` buff scroll. (Enchant, attribute, Return and Resurrection scrolls still drop —
those are different items and should be unchanged.) -> 

48e [] - **The Blessing Box: 250,000 at the Apothecary**, under the **Use** filter. Debug → items also
gives you two for free. Opening it offers **17 scrolls and lets you tick TEN**: rows read `[  ]` and
`[x]`, the title counts `3 / 10`, tapping a ticked row unticks it. ⚠ If you see a hollow box instead of
`[x]` that is the font atlas and it is a bug. -> 

48f [] - **The 11th tick is refused with a message**, not silently ignored. This is the one that
matters — a swallowed tap would spend a 250k box on a set you did not choose. -> 

48g [] - **Confirm gives you exactly the ten you ticked** and eats ONE box. Cancel eats nothing:
reopen and the box is still there. -> 

48h [] - **A scroll out of the box is BOUND** — it cannot be sold or traded (the box itself can, for
10,000). Read one: it should land the same value an NPC buffer gives you, for an hour. -> 

48i [] - **Buff potions have two rungs only** — *(Lesser)* and plain. There is no "(Greater)" buff
potion anywhere now: not in a drop, not at the Apothecary, not in the debug menu. **Dash** still has all
six rungs and still drops, but it is no longer on the Apothecary's shelf. -> 

48j [] - **The bag should be visibly quieter.** Measured: consumables fell from ~33% of kills to ~18.5%
at level 33. An offline farm should come back with roughly half the consumable clutter — and the same
gold, because buff potions already sold for 0. -> 

[] -> 

[] -> 

---

## 47. THE FRICTION TIER + REVIEW FIXES (0.47.0 — unplayed)

47a [] - **F1: turning auto-farm OFF keeps your target.** Auto-farm onto a mob, then switch to manual
mid-fight: the target must STAY selected, the target window must stay open, and your character must keep
swinging. Only the autopilot stops. You should never have to re-select to finish that kill. -> 

47b [] - **...and it still clears when it should.** Kill the mob you were handed: the target window
clears on its own, no ghost target on the corpse. Die while auto-farming, or run the farm budget out:
auto stops AND the target clears, as before. -> 

47c [] - **V1: QSell.** Vendor -> Sell: a `QSell: off` button beside the Sell tab. Tap it, it turns red
and reads `QSell: ON`, and the window title says "one tap sells the WHOLE stack". Now one tap on a row
sells the entire stack — no numpad, no confirm. Off again and the numpad is back. -> 

47d [] - **QSell is hidden on the Buy tab.** Tap Buy: the button disappears rather than sitting there
dead. -> 

47e [] - **G4: save-login.** Login screen has `[x] Save login on this device` under the password box.
Log in as one of your own accounts, close the app, reopen: YOUR username and password are filled in, not
`admin`. That was the whole complaint. -> 

47f [] - **Unticking it really forgets.** Tap the box to `[ ]`: the two fields clear immediately. Close
the app, reopen: still blank. The SERVER ADDRESS must still be remembered either way — you should never
retype the IP. -> 

47g [] - **G5: Sprint and Dash are ONE family now.** As a rogue with Sprint: drink a Dash potion
(Common/Uncommon), then cast Sprint — the Dash square is REPLACED, you do not get two speed buffs.
Then with Sprint L1 up, drink a Rare Dash (+45): that one wins, because it is stronger. -> 

47h [] - **A weaker rung is REFUSED, not wasted.** With Sprint up, drink a Common Dash potion: it must
be refused with a message and **still be in your bag afterwards**. -> 

47i [] - **The ladder order is right.** Rough order, weakest to strongest:
`Dash C +15 · Dash U +30 · Sprint L1 +40 · Dash R +45 · Dash E +50 · Dash L +55 · Dash M +60 ·
Sprint L2 +60`. Spot-check a couple of pairs against your move speed on the stat sheet. -> 

47j [] - **Sprint L2 beats even Dash Mythic.** At level 40+ with Sprint level 2: cast it, then drink a
Mythic Dash (+60) — the potion must be refused. A class skill you levelled must not be overridable by a
bottle. -> 

47k [] - **Timings.** Both last 15s. Sprint's reuse is 30s, the potion's 60s. -> 

47l [] - 🔴 **A quest token stuck in the warehouse can be taken out.** This was a real trap: if you ever
banked a quest item in the PRIVATE warehouse before this build, it became invisible and unrecoverable
while its quest stayed stalled. Open the keeper -> Withdraw -> All: any banked token must be listed and
withdrawable. (Depositing one is still refused — that is B4 and correct.) -> 

47m [] - **The quest tracker rows no longer overlap.** Pin several quests with LONG objectives ("Hunt in
the Bracken fields, then return to Huntmaster Cera"): each pin's text must sit inside its own box, not
run over the pin below. The tracker panel must GROW to fit them instead of drawing past its bottom
edge. -> 

47n [] - **Five pins fit.** Accept five quests (Q2 auto-pins them): all five readable, none drawn on top
of the world outside the panel. -> 

47o [] - **The bag's preset C is reachable.** Bag -> Equip: the paper-doll column opens and the window
gets TALLER. Presets A, B and C must all be inside the window with their Save / Equip / To bar buttons
tappable. Preset C used to draw outside the window over the world. -> 

47p [] - **Compare no longer jumps.** Item details -> Compare: the column you were reading must stay
where your thumb is, and the worn item appears to its LEFT. It used to slide a quarter-screen right,
taking the Equip/Bin buttons with it. -> 

---

## 46. THE 0.46.0 BATCH + INVENTORY HYGIENE + THE QUEST SECTION (unplayed)

### Bugs that blocked play

46a [] - **B1: auto-on marks are per CHARACTER.** Put basic attack on the bar of character A and mark it
auto-on. Delete A, make B, put the action on B's bar: it must arrive OFF. Removing a skill from the bar
must clear its auto mark; MOVING it between slots must NOT. -> 

46b [] - **B5: the farm budget is a per-ACCOUNT daily BALANCE, not a session timer.** Farm 15 minutes,
go to town, relog, come back: the time left must be LOWER, never handed back. It refills at server
midnight. Check it is shared across characters on the same account. -> 

46c [] - **B6: text boxes EDIT instead of wiping.** Tap any pre-filled box — the server address, the
command bar after Reply/Whisper fills `/w name ` — and start typing: your text must be ADDED, not
replace what was there. This was the one that broke Reply and Whisper. -> 

46d [] - **B7: an out-of-range party member can be targeted.** Party up, let them walk out of sight, tap
their roster row: the target frame must appear (name, level, class, both bars, `(out of sight)`) and
stay — not be wiped by the next world update. Assist / heal / buff / kick / change-leader must all work
from there. -> 

46e [] - **G7: a hotbar consumable at 0 count is not disabled.** Run a potion stack to zero: the slot
draws as a full cooldown sweep, still looks the same, and press-and-hold still opens Move/Remove/Auto so
you can clear it. Tapping it does nothing (no refusal spam). -> 

46f [] - **C12: `/offline` and the [Offline] button.** You could not start offline farming AT ALL
before. Menu -> Offline (and the `/offline` command) must put you into offline farm and drop you to
character select, with the time left shown on that character's row. -> 

46g [] - **C18: undo a bin-delete, free, in the field.** Bin an item by mistake, then Menu -> Restore:
the last 5 binned items, newest first, restored for 0 gold, ANYWHERE — not at a vendor. A +6 sword must
come back +6 with its rolled attribute. Bin part of a stack and only that many come back. -> 

### Inventory hygiene

46h [] - **B4: a quest token cannot be disposed of anywhere.** Try to sell one at a vendor, deposit it in
BOTH warehouses, put it on a trade table, and bin it. Every path must refuse it *before* the tap — the
row should not even be offered. -> 

46i [] - **C8: one filter, three windows.** Bag, vendor and the keeper all get the same tabs —
All / Gear / Use / Mats (+ Quest in the bag only) — and every list is ordered BY NAME. -> 

46j [] - **C7: gatekeeper tabs.** Zones / Cities instead of one long list. -> 

46k [] - **C5: an NPC lists only ITS OWN quests.** Every NPC used to show every quest you carried. Open
three different NPCs with quests in progress: each shows only what it gave you. -> 

46l [] - **C6: names only outside Details.** No quest wall-of-text in the NPC window or the list rows —
description, location and story live in Details and nowhere else. -> 

46m [] - **C11 + B2: compare and details are ONE window.** Select a bag item -> Compare: the window
grows a second column, worn piece on the left (no buttons), selected on the right with [bin]/[equip].
From the equipment panel it is the same shape with [unequip]. And a PENDANT must open a pendant, not a
ring — that was B2. -> 

46n [] - **C10: jewel swap weighs delivered M.Def, not rarity.** Wear a Mythic F ring and an Uncommon E
ring, equip another E ring: it must replace the WEAKER by actual M.Def (enchant included), not the
F-grade one. -> 

46o [] - **G6: the warehouse says how full it is.** Top-right per bank, `Slots 12 / 30`, and it turns
RED when full. You should never again find out from a chat line you weren't looking at. -> 

### The quest section

46p [] - 🔴 **Q1: quest pins SURVIVE a relog.** Pin two quests, exit to character select, come back:
still pinned. Restart the server: still pinned. They are per CHARACTER now — a second character must
have its own pins, not yours. -> 

46q [] - **Q2: accepting a quest pins it automatically.** However you take it. At the cap (5) an
automatic pin must YIELD — it must not push off one you chose yourself. An explicit Track past the cap
still evicts the oldest. -> 

46r [] - **Q3: the tracker shows OBJECTIVES only.** Items and kills — `Fox Pelt 12`, `3 / 10` — never
the description, the location or the story. -> 

46s [] - **Q4: a tracker row opens that quest's Details.** One tappable row per pin. Dragging the
tracker panel must NOT count as a tap. -> 

46t [] - **Q5: Active rows are as short as Available rows.** Name + step `2 / 3` + level band, the
status line, the progress number, and `From: <npc> — <town>`. No step text, no "Where:", no mob name —
those are in Details. -> 

### Economy (V2 / V2b — measured, not guessed)

46u [] - **The gear faucet is 3.3× smaller.** Gear drop groups went ×1/3 → ×0.025 (13× rarer) and the
sell divisor 25 → 10 (each piece worth 2.5× more). Over your ~14h idle farm the measurement says
**4.06kk → 1.23kk**. Farm a while and tell me if the coin feels right or if you are now poor. -> 

46v [] - **You are no longer wading through junk.** The point was the BAG as much as the gold — far
fewer gear drops, each worth more. Does the bag stay manageable? -> 

46w [] - **Buff potions and buff scrolls sell for 0.** Your playtest-17 ask had never actually been
implemented. Check at a vendor. Enchant and attribute scrolls already sold for 0. -> 

46x [] - **Enchant / attribute scrolls drop far less and start later.** At level 33 the measurement went
enchant 30% → 9% per kill, attribute 27% → 3.6%. Attribute scrolls now start at 40/52/61/76/80/84 by
rarity. Say if they still flood the bag. -> 

---

## CARRIED FORWARD — still blank from the 0.45.0 checklist

### 37. PARTIAL-STACK TRADING (0.42.6) — needs a second character (the 2nd app icon is the duo rig)

37a [] - Offering part of a stack: tap a stack of 50 potions in the trade bag, numpad opens ("Offer",
max 50), subtitle counts what you KEEP. Offer 20 → offer list shows x20, bag row stays reading x30. -> 

37b [] - Non-stackables still toggle: tap a weapon, straight onto the table, second tap takes it back. -> 

37c [] - The split is real: complete a 20-of-50 trade, you keep 30, they gain 20 MERGED into a stack
they already had, not a second row. -> 

37d [] - A shortfall kills the whole trade: offer 20, then sell some so you hold fewer, both confirm →
"Trade failed" and NOTHING moves. Neither side may end up with 14. -> 

37e [] - A full bag is judged correctly: receiving a stack you ALREADY hold succeeds (merges); something
new is refused. Giving away part of a stack must NOT count as freeing a slot. -> 

### 36. MOB REGEN leftovers

36e [] - A boss re-pulled while wounded does NOT dump its phase script. Pull past a threshold, break
combat, re-engage before it heals: it must CONTINUE, not replay every announce/enrage/add-wave at once.
A boss that disengaged enraged must still be enraged, losing it only at full health. -> 

36f [] - Safe-zone kiting is self-limiting. Aggro something, step into town so it resets, step out: you
may re-engage it wounded, but it heals 5%/s while you wait, so hit-and-run must not let a weak character
grind down something far above them. -> 

### 34. THE GROUP BUFF

34c [] - Buff scrolls are CONSUMED. Read a Scroll of Might: the buff lands and the stack drops by one.
Check a potion's count too. -> 

34d [] - A scroll that would be refused is not read at all. Under a stronger buff, pressing it says
"would have no effect" BEFORE the cast — no cast bar, no cooldown, nothing lost. Interrupt a scroll
mid-read: it must survive that too. -> 

### 33. THE POTION SPLIT

33l [] - The improved buffs are PARTY buffs. Cast one in a party: it lands on every member within 800
range, not just the target. Same for Harmony. -> 

### 32. PLAYTEST-15 LEFTOVERS

32b [] - Class change applies without a relog: the class updates immediately and the Skills window shows
the new unlearned list at once. -> 

32h [] - HP potions drop less. It is the potion FAUCET being closed, not the damage. -> 

32o [] - Escape/return scrolls can be sold. They are tradable but the vendor refuses them. -> 

32p [] - Buff potions sell at ÷25 like everything else — ⚠ **superseded by 46w: they sell at 0 now.**
Just confirm 0. -> 

32s [] - Your own party can NEVER be hit, and the tap follows them instead. Tap a party member twice:
"You follow X.", never a swing — with PvP ON, with a RED party member, and with an offensive SKILL.
Then confirm a NON-party player CAN be attacked on the second tap with PvP on, out of town. -> 

32y [] - One item can be tuned on its own: `/droprate item Scroll of Resurrect 5` accepts the NAME and
confirms; bare `/droprate` lists it under "Per-item overrides". In the drop tree the Always group still
reads 100% but its members print their SHARES and the scroll's has grown at the others' expense. `... 1`
clears it. -> 

32z [] - Auto-farm skill chains: cyclic OFF reads 1-2-1-3-1-2, cyclic ON reads 1-2-3-1-2-3; a needed heal
beats a buff beats an attack; heal threshold 50 heals only under 50%, 100 on a healer fires on cooldown
and in a party lands on the most injured in range, heal row OFF never auto-casts; an auto-buff recasts
under 60s remaining but not under a stronger same-family buff; a rank-1 debuff is replaced by the higher
rank; assist-party-leader means you attack only what he attacks and stand still when he has none; all of
it survives a relog. -> 

### Older

25b [] - No combat-logging out of a DoT: while a bleed/poison/venom is on you, character select must
REFUSE ("You can't leave while in combat") and you stay in the world. Same for `/exit`. Pulling the plug
mid-DoT must not run the link-dead grace down. -> 

13a [] - The ~3h "take a break" banner — needs 3 hours of continuous play. -> 

17-1 [] - `/jail test1` then `/tp test1`: the jail has no border, so an admin who moves inside it gets
teleported to the dungeon. -> 

17-23 [] - Real impassable WALLS — client collision (you stop at the surface). Today only the server
rubber-bands you. -> 

---

## STILL NOT BUILT — nothing to test, listed so nothing is silently dropped

- 🔴 **CRAFTING** — professions, window, recipes. Still the top content blocker: nothing above Epic can
  be reached in play without it. Design is fully written.
- **The enchant rework (D1)** — three scroll TYPES (breaks / −1 / **safe**) with RARITY choosing the
  grade E→S, safe scrolls from bosses only. Plus `/enchant <value>` and every scroll in the admin menu.
- ~~**E3, the rest of the buff economy**~~ — **BUILT in 0.48.0, test it as 48d-48j.** Two rarities, one
  max-rung scroll per buff, and the Apothecary 250k box of 10 are all in.
- **Crit damage / blows / `[Double]`** — the ruling is written (`design/CritBlowAndDouble.md`): crit dmg
  is FLAT inside the crit, blows scale off crit damage, `[Double]` becomes an ATK curve capped at 25%.
  Not started; needs BalanceMatrix before and after.
- **Rogue Weapon Mastery crit damage swapped 24↔28** (G8) — you fixed it in the CSV, not yet in code.
- **3rd / 4th class kits** — blocked on your CSVs.
- **Instances / dungeons** — design done, you are holding.
- **C1** chat clears on relog · **C2** newbie gear untradable + 30-day timer · **C3** timed items show
  remaining time, colour-graded · **C4** buff potions/scrolls take auto-on · **C9** a [Speak] button on
  NPCs · **C13** newbie quest band 10-35 · **C14** a 2h weapon greys the offhand · **C15** Feretite Wand
  into the newbie box · **C16/C17** title colours, fonts and admin titles · **D4** a top-of-family party
  Frenzy + more Harmonies for 76+ · **D5** the [Combat] chat tab in its own window.
- **B9** jail border · **B10** client collision · **B11** `/block` + `/like` chat commands, and an
  admin/moderator must not be blockable.
- Entities are coloured billboards, no models, no animation (waits on the art pass). Portrait layout is
  not supported.

---

## FREE SPACE — anything else you hit

[] -> 

[] -> 

[] -> 

[] -> 

[] -> 
