# OPEN CHECKLIST — everything untested after your **playtest 22** (2026-08-13)

> **Rolling and unversioned.** Playtest-22 (your 0.62.0 pass) is closed and transcribed **verbatim**
> into [Playtest-Archive.md](Playtest-Archive.md#playtest-22) — your marks, your comments and all
> twelve free-form finds, unedited. This file has been rewritten against it: the sections that passed
> outright are collapsed to one line each, because a ✅ that still takes half a page is a page you read
> again for nothing. What you flagged has been **fixed and written up in §79**, which is the section
> to test next.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids — **72** of them now; playtest 22 added
`BL-65`…`BL-72`, so every feature you asked for in that pass is written down and you do not have to
report it again.

---

## ⚠ BEFORE YOU START

🔴 **This checklist describes code that is COMMITTED BUT NOT YET BUILT INTO AN APK.** Nothing below
is testable until the next publish. Say the word and it goes out.

🔴 **`game.db` needs NOTHING** — no schema change in this batch.

⚠ **Protocol is unchanged (17).** The 0.62.0 client speaks to this server; the fixes are behavioural.

---

## Where to spend the pass, if you don't do all of it

1. **§79c the shield block reduction.** Your ruling changed a combat number that `70a` was measured
   against. It is the one thing here that can make a tank feel different.
2. **§79a the auto-buff tab** — it is the thing you found broken, and the root cause was destroying
   the setting on the server, not just failing to draw it.
3. **§79e the deleted gear.** Sixty-four item ids stopped existing. The risk is not that they are
   gone, it is that something still points at one.
4. **§75 and §76**, which you could not reach last time for want of item ids — §79d is the fix for
   exactly that, so this is now doable.

---

## 0. ANSWERS I OWE YOU — read, don't test

- ✅ ~~**`62j` / `74e` the enchant drop cut.**~~ **CLOSED by your data** — *"to 28 I got 2"*, against
  80 by level 28 before. The 30× cut is right; `EnchantShare` is not touched again. Your `[x]` also
  answers the `B at 0.075%` worry I flagged: leave it.

- ✅ ~~**`55f` mages and MP, open for three passes.**~~ **CLOSED by your own trace**, and it is worth
  keeping the shape of it: fine to 24 · at 24 the pool empties *slowly* and costs a few seconds' wait
  · Spell Mastery L2 + Elemental Bolt L1 farms clean · Bolt L2 costs more than it earns, and you
  ruled that correct — *"thats OK (its a nukers choce - stronger spell more mp)"* · Restore Spirit
  holds MP flat · Vampiric drains it, *"which is exactly as intended - its a death prevention
  skill."* **The ladder is the fix. This row is retired.**

- ✅ ~~**`77d`, the one place I had to interpret you.**~~ You marked `77d` `[x]` and said nothing, so
  the interpretation stands: **`[-]` never walks a PAID rung down**; un-committing is the
  Mindwriter's job. If that was assent-by-omission rather than assent, say so and it is a new entry.

- **`/give`'s `sellPrice` argument, your `[?]`.** Four cases, and only two of them are opinions:
  - **`-1`** → *unsellable*. The vendor refuses it outright.
  - **`0`, `-`, or omitted** → **no opinion, use the catalog's own price.** Deliberately the same
    thing: a stored `0` would mean "worth nothing", which is a different claim from "I did not say".
  - **`1xxxx`** (any positive number) → that exact gold price, overriding the catalog. `k`/`m`/`b`
    suffixes and `1_000_000` both parse.
  Every argument after the item id follows the same rule — `-` is always *no opinion*.

- 🔵 **Dark Dominion is the one thing I found and did NOT delete.** Six armour pieces (Plate,
  Leathers, Robe, Helm, Gauntlets, Sabatons) forming a real named SET with a real set bonus — and
  **nothing drops, sells or boxes them**, so the set has never been obtainable by anyone. It is
  off-ladder in exactly the way you asked me to hunt for, but deleting a designed set is your call,
  not a cleanup. **Make it obtainable, or say the word and it goes.**

- **`Robe 611` is still `[NOT BUILT]`** — fourth pass running. → `BL-27`.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone again on purpose: §79c already moves the block channel this build, and moving
  both at once would make neither measurable.

---

## 79. THE PLAYTEST-22 FIX BATCH — everything you flagged

- `79a` [] - 🔴 **THE AUTO-BUFF TAB: the root cause was DESTRUCTION, not a redraw.** *"it doesn't
  survive a relog ... it says that it's saved but relog says otherwise."* `SendAutoHuntConfig` — the
  echo the server sends the client at login — **never included the `Buffs` array**. The client keeps
  that echo as its entire idea of the config and sends it back with only the bit you edited changed,
  so the tab came back `null` and the server's handler cleared it. So the first press of the **Auto**
  button after a login wiped the tab on the server for real, and the empty rows you saw were honest.
  Fixed at both ends: the echo carries the tab, and a `null` array is now read as *"no opinion"*
  rather than *"clear it"*, so no client can ever empty it by omission again. 🔑 *This DTO has four
  sites and only the echo fails silently — the same shape as `67i`/`74b`.* **Set the tab, relog,
  reopen it; then set it, press Auto, relog, and reopen it — that second one is the case that was
  actually broken.** ->

- `79b` [] - **`78g`'s other half.** Reset always worked because Reset is client-side only; Save was
  the broken half. With `79a` fixed, *"one window, one Save"* is testable for the first time: set up
  **both** tabs, Save once, relog, and check neither half was dropped. ->

- `79c` [] - 🔴 **YOUR RULING ON BLOCK REDUCTION, and it moves a combat number.** *"what increase the
  reduction? The shield says 10 but I see 18% ..the shield says 20 I see 28% ... Shields dmg reduction
  is never increased by any means ...only chance."* You were reading the Shield Mastery **passive**:
  it added `ShieldDefPct × 0.04` to the block, which at a maxed 2.00 is exactly the **+8 points** you
  saw. There was a second, smaller one on the Mastery **buff** (+10 points more, when it is up).
  **Both are gone.** BlockReduction is now the shield's own printed number and nothing may raise it;
  the ladder scales block CHANCE and shield DEFENCE instead.
  ⚠ **This is a nerf on top of `70a`, which you passed while it was still +18%.** Measured: a maxed
  tank at ~33% block chance loses about **2.6 percentage points of total mitigation** — small, but it
  lands on the class you just signed off. **Play the tank again and say whether it still reads as a
  tank.** The card and the stat sheet must now show the same number. ->

- `79d` [] - 🔑 **THE ITEM-ID LIST, and why this one unblocks your own pass.** *"Need a grouped list
  (in a file - like the commands one) with each equip/item ID ... So I can use the `/give` command and
  I can test better 75 and 76."* → **[docs/guides/ItemIds.md](../guides/ItemIds.md)**: all **1,078**
  ids, grouped weapons / shields / armor / jewels / runes / potions / scrolls / boxes / materials /
  quest items, and the gear sorted by tier so "a level 40 heavy body" is one glance. **It is
  GENERATED** (`tools/ItemIds`), never hand-written — a hand-kept id list is wrong the first time
  anyone adds an item, and a wrong id sends you hunting for a typo in the command.
  Plus your second half: **an `id <defId>` row on the item card, staff only**, under the enchant line
  exactly where you put it. **Read an id off something in your bag and `/give` yourself another.** ->

- `79j` [] - **`/give` ends in `[amount]` now**, default 1, capped at 10,000 — *"if i want to get 1000
  mats not to have to write command 1000 times."* Because it is the last positional, everything before
  it is `-`:
  ```
  /give Gena mat_iron - - - - - - - 1000
  ```
  🔑 **It splits the way the BAG does, and that is the part to check.** A **stackable** (materials,
  potions, scrolls, quest items) is **one row carrying the quantity** — 1000 mats cost one slot, which
  is the case you asked for. **Gear cannot stack**, so an amount there is that many separate rows, it
  stops when the bag is full, and it tells you how many fit. Either way it is **one inventory push**,
  not one per unit — that was `66n`'s stall and it is not coming back. **Try 1000 of a material and
  then 5 of a sword.** ->

- `79e` [] - 🔴 **THE OFF-LADDER GEAR IS GONE — and it was 64 items, not one.** *"Brass amulet also
  need to be gone. Look for other items that are not from the grade items or training ... treasure
  chest just gave me masterwork iron sword."* What that chest handed you was `sword_e_rare`, one of a
  **sixty-item legacy grid** — 4 weapon types + 3 armour weights + 3 accessory slots, each × F/E ×
  Common/Uncommon/Rare — generated by a block that **predates the grade ladder by a whole generation**
  and was never re-cut with it. 🔑 *They survived four gear passes because exactly ONE line referenced
  them: that treasure chest. Nothing else showed them to anyone, so nothing else ever caught them.*
  Deleted: the 60, plus **Brass Amulet** (still on the Outfitter's shelf), **Silver Talisman**, **Iron
  Mace** and **Ash Wand**. The chest's 1% slot now rolls the real ladder's `sword1h_t20_rare`.
  ⚠ **THE RULE THIS LEAVES:** gear is **LADDER** (`ItemLevel > 0`, generated from `gear_sets.csv`) or
  **TRAINING** (its own CSV block, `72b`). There is no third category.
  **Open a few treasure chests, walk both shop shelves, and check nothing 404s.** (A bag row naming a
  deleted id is dropped on load — that path already existed and is intended.) ->

- `79f` [] - **The quest-token chat flood is gone.** *"Remove the `SYSTEM: Stonewatch Contract: Bear
  Pelt 93` .... its a drop item .. u can say in combat `You looted: Bear Pelt [Q]`."* Built as you
  wrote it, in the combat log beside every other drop. The running count went with it — the quest
  window is refreshed on the same kill and is where a contract's progress belongs. ->

- `79g` [] - **The gatekeeper closes behind you.** *"After the teleport from the GK the window of the
  old gk need to close automatically."* Taking a ride now shuts the dialog: you are not standing in
  front of him any more, and the rows left on screen are an NPC in another city. ->

- `79i` [] - 🔑 **MP regen on walk: you were RIGHT, and right about the reason too.** *"MP regen is
  unchanged when walking/runnin - or atleast vissually - it seems like its only visually."* The server
  has been paying the bonus the whole time (walking ×1.2, sitting ×1.8, safe zone ×5) — but
  `StandingRegen`, the function that fills the **stats window**, deliberately reported the RUNNING
  baseline and ignored the stance. So the one place you could check the bonus was the one place
  saying it did not exist. The sheet now shows what you are actually paid, and it is pushed the
  instant the stance changes. **Open Stats, toggle walk, and watch both regen numbers move by 20%.**
  ⚠ It also picks up the safe-zone ×5 now, so the number in town is genuinely five times the field
  one — that is real, not a bug. ->

- `79h` [] - ⚠ **`75c` — `/give <player>` alone: NOT a regression, it was never built on this
  client.** The server has always sent an `AdminGivePicker` message for it; **the Unity client has no
  handler for that message, and none for `/bag` either** — both are WPF-harness survivors that the
  checklist kept claiming worked. I did not build the window in this batch: `79d` gives you the ids,
  which is the route you actually asked for, and a picker window is a real piece of UI rather than a
  fix. **Queued as `BL-56`** (which is already "the admin item picker as a selection box"). Say if you
  want it before the rest of that entry. ->

---

## 70. THE SHIELD CUT — ✅ **PASSED**, one ruling taken

`70a` `70c` `70d` `70e` `70f` `70g` `70h` all `[x]`. **The number the build stood on holds**: −19%
P.Def still reads as a tank, and a shielded mage is mortal again. `70b` was not a pass but a
**ruling** — block reduction may never be raised — and it is built in **§79c**, which is where a
tank wants re-checking.

## 71. THE START QUEST — ✅ **PASSED, ALL EIGHT ROWS**

`71a`-`71h` `[x]`. Both dead-ends (the fighter with nothing to cast, the auto-farm beat no button
could credit) are gone, the boxes arrive from the quest and know your class, and the travel step
lands on the pups. The path every new character walks is clean.

## 72. THE TRAINING TIER — ✅ **PASSED**

`72a` `72b` `72c` `[x]`. Broken jewels at 9/5/3, the CSV's training block above each F row, the
Wooden Shield as proper training kit. ⚠ **§79e is the same class of bug caught one layer out** — the
tier block fixed what was *above* the ladder; the legacy grid was sitting *beside* it.

## 73. THE TRAINING DUMMIES — ✅ **PASSED**

`73a`-`73e` `[x]`. They work for the first time (the reach was 50 against a stop-distance of 80),
each shows its own name and title, magic evasion is measurable, and the rank titles landed.

## 74. THE FIVE SMALL ONES — ✅ **PASSED, ALL SIX**

`74a`-`74f` `[x]`, including `74e`'s enchant-drop verdict (see §0) and the x500 mats stall.

## 77. THE STAT-SWAP TAB — ✅ **PASSED, ALL NINE**

`77a`-`77i` `[x]`. The layout reads as a plan, the ladder charges 35,000,000 however you spread it,
the basket is all-or-nothing, and the rungs survive a relog. `77d`'s interpretation stands (§0).

## 78. THE AUTO BUFF TAB — rows a-e ✅ **PASSED**; save was broken

`78a`-`78e` `[x]` — seventeen families, rarity-then-scroll priority, never spending a bottle it
cannot improve on, working with auto-farm off, Dash correctly absent. `78f`/`78g` are fixed in
**§79a**/**§79b**.

---

## 75. ITEM TAGS + `/give` — still untested, now unblocked by §79d

- `75a` [] - **An instance carries five properties of its own**, each `null` meaning *no opinion, use
  the catalog*: **sell price** (−1 = unsellable) · **tradable** · **custom name** (20 chars) · **can
  store private** · **can store account**. ->

- `75b` [] - 🔑 **The tag on the card is DERIVED, never stored.** Sellable + tradable shows nothing;
  neither shows **"bound"**; sellable but untradable shows **"private"**; and a timer composes on top →
  **"(temporary, bound)"**. The three predicates live in `Game.Shared` and are called by the server
  *and* the item card. ->

- `75d` [] - **A tagged instance is ALWAYS a fresh bag row**, never merged into a stack — the tag
  belongs to that copy. Give yourself two differently-tagged copies of one item and confirm they stay
  apart. ->

- `75e` [] - **Enforcement reads the INSTANCE, not the def**, at the vendor, the trade offer, the
  private keeper and the account keeper. Try to sell, trade and store a bound one. ->

- `75f` [] - 🔴 **THE ROW THAT MATTERS: the tags survive a relog.** They are five real columns. *A
  bound item that comes back ordinary looks perfectly right until the moment it can be sold.* Tag
  something, log out, log in, check the card. ->

- `75g` [] - 🔑 **The two storage flags are deliberately separate, not one "storable".** An ordinary
  bound item is barred only from the **account** keeper; Sinners has to be barred from **both**. ->

---

## 76. PREMIUM REWARD RUNES — half tested

Passed: `76a` `76d` `76g` `76h` `76i` `[x]` — the 55 generated items, rungs never stacking, a rung's
popup reading its own level, the measured effect, and the startup refusal. Left:

- `76b` [] - **Rune of Sinister** — no exp, no SP; **gold and drops untouched**. *"So a grinder can
  grind and no lvl up."* ->

- `76c` [] - 🔴 **Rune of Sinners** — all four channels zeroed, *"bound to your soul for the time it
  has left."* New def-level **`SoulBound`**: refused by **both** keepers regardless of instance tags.
  Try to store one, trade one and delete one. ->

- `76e` [] - **The drop bonus is a PARAMETER of the one rate function**, not arithmetic at a call
  site. 🔑 **A player wearing a Drop rune is *shown* the chance they actually roll.** Open a mob's
  drop tab with and without one. ->

- `76f` [] - 🔴 **A stop is a hard override applied AFTER the max**, so no pile of bonus runes can
  dilute a punishment. **Kill something wearing Sinners and confirm all four are zero.** ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [~] - **Nuker vs champion.** Your playtest-22 note: *"they both have hard time to farm without
  buffs .. when i login in 1-2h after the npcs buffs are gone both are dead and with potion buffs."*
  🔑 **That is a finding, not the measurement** — and it means the run cannot be measured until
  something keeps them alive, which is what §78 is for. Both halves are now written up as `BL-72`
  (is unbuffed farm meant to be survivable?) beside `BL-18`. Do `32z` in the same sitting. ->

- `32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
  ranks, assist-leader — and all of it **survives a relog**. ⚠ `74f` changed what the chain does when
  a skill cannot fire (it skips instead of stalling), and `79a` changed what survives a relog. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row, so it needs a free slot. ->

- `36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
  top. ->

- `25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. ->

- `13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**
The eight from playtest 22:

| you said | id |
|---|---|
| separate the dungeons into level bands, and two more of them | `BL-65` |
| ~~a grouped item-id file + the id on the card for admins~~ | ✅ **built** — see `79d` |
| the `MpHeal` type, vampiric bolt as a Heal, an MP threshold | `BL-67` |
| more zones across the 16-40 band, by widening the map | `BL-68` |
| invisibility — rogue `hide`, `stealth` vs mobs, admin `/invis` | `BL-69` |
| mob clans / social circles, and the rogue's `lure` | `BL-70` |
| the aggro and taunt model — *"what we have and what it needs"* | `BL-71` (**answered in the entry**) |
| unbuffed farm is not survivable for either kit | `BL-72` |

🔑 **`BL-71` answers your taunt question in writing** — short version: a real per-attacker threat
table already exists, aggro already IS damage, and `provoke` already works; what is missing is taunt
POWER as a number, threat decay, and healer threat. Read the entry before you spec the rest.

Still true and unchanged:

- **3rd/4th class kits** — blocked on your 40+ CSVs (`BL-02`). Nothing is invented in the meantime.
- **Crafting professions** (`BL-05`) — the `Game.Shared` foundation is built and committed; the
  server half is not. ⚠ **Two questions of yours still block it**: gear is GRADED not rarity-ranked
  (so all 135 gear recipes land at L6), and the Scribe's L1 recipe (`BL-57`).
- **`G3` mobs-as-players** — needs the document and the BalanceMatrix tables first (`BL-47`).
- **Instances** — you are holding (`BL-48`). ⚠ **Dungeons are the cheap half**, and `BL-65` is now the
  concrete version of that.
- **`G2` / `0e` `lb_*` + `wc_*`** — closed by your ruling: leave them.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
