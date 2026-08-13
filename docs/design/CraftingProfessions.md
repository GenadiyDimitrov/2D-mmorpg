# Crafting professions — masters, levels, and quitting (`BL-05` + `BL-40`)

**Status: DESIGN, from your playtest-21 `66a` reply. Not built yet.** This supersedes §3 ("Professions")
of [Crafting.md](Crafting.md) — everything else in that file (materials, refinement, the mat→item map,
the drop side) still stands and is built.

> *"better at NPC — and craft happens with their respected masters … u compleate the quest and u can
> take his proffesion."*
> *"The proffesion should not be final (i know i told you that is final, but now i changed my mind
> after getting a proffesion that i dont like.. sorry)."*

---

## 0. The one number that makes this fit: six rarities, six crafting levels

`ItemRarity` already has **exactly six** rungs — `Common(0) · Uncommon(1) · Rare(2) · Epic(3) ·
Legendary(4) · Mythic(5)` — and you asked for **six crafting levels, L1-L6**. They line up one to one,
so the whole design needs no new ladder and no mapping table:

> **At crafting level `N` you CRAFT goods of rarity `N-1`, and you REFINE up to rarity `N`.**

That is your rule verbatim: *"if im L1 i have common mats (that are dropped) and common potions (that
can be crafted) and in L2 tab i see only the uncommon mats (that can be sintesized from common items)
and none of the uncomon potions (dimed like now)"*. L1 crafts Common and refines into Uncommon; L2
crafts Uncommon and refines into Rare; L6 crafts Mythic and refines nothing, because there is nothing
above Mythic.

| Crafting level | crafts goods of | refines *into* |
|---|---|---|
| L1 | Common | Uncommon |
| L2 | Uncommon | Rare |
| L3 | Rare | Epic |
| L4 | Epic | Legendary |
| L5 | Legendary | Mythic |
| L6 | Mythic | — (top of the ladder) |

---

## 1. Crafting EXP — your marks, made exact

Your marks are the **cumulative** exp at which each level starts: **0 / 5 / 15 / 30 / 50 / 100**.
A craft's exp depends on the recipe's level *relative to yours*:

| recipe vs your level | exp per craft | crafts to cover 1 exp | your words |
|---|---|---|---|
| **−2 or lower** | **0** | ∞ | *"a -2 grades dont give of exp"* / *"L1 does nothing only craft result no exp"* |
| **−1** | ⅓ | 30 | *"lower 1 grade = 3 times more"* |
| **same** | 1 | 10 | *"x10 crafts per difference of same level"* |
| **+1** | 1¼ | 8 | *"higher 1 grade gives 20% more exp (so ~8 items)"* |
| **+2 or higher** | not craftable | — | *"L5 should not be available"* |

Your own worked example checks out exactly — **L3, 15 exp to reach L4**:

| crafting with | crafts needed | you said |
|---|---|---|
| L3 recipes (same) | 15 × 10 = **150** | *"i need 15x10 = 150 crafts to lvl L4"* |
| L2 recipes (−1) | 150 × 3 = **450** | *"if i use L2 crafts i need 450"* |
| L4 recipes (+1) | 150 × 0.8 = **120** | *"if i use L4 ill need 120"* |
| L1 recipes (−2) | never | *"L1 does nothing"* |

### Store it as an integer

⅓ and 1¼ of "0.1 exp" are not representable in an int and drift if stored as float. So **one
same-level craft is worth 12 internal points**, and your marks are multiplied by 120:

```
CraftExpPerCraft:  -1 → 4      same → 12      +1 → 15        (all exact integers)
CraftLevelMarks:   L1 0 · L2 600 · L3 1800 · L4 3600 · L5 6000 · L6 12000
```

Check: L1→L2 is 600 points ÷ 12 = **50 crafts** (your 5 exp × 10). L3→L4 is 1800 ÷ 12 = **150**, ÷ 4 =
**450** at −1, ÷ 15 = **120** at +1. The display divides by 120 and shows your numbers back to you.

⚠ **The UI shows a PERCENT within the level, not the raw points** — *"l2@100%"* is your own notation,
and it is the only form in which the freeze below makes sense to read.

---

## 2. The character-level gate, and the freeze

> *"crafts need char level + crafting lvl => lvl20 char must lvl up to craft better items not i just
> to make 10 chars to sit in town and craft."*

| crafting levels | needs character | which is |
|---|---|---|
| L1, L2 | 20 | 2nd class |
| L3, L4 | 40 | 3rd class |
| L5, L6 | 76 | 4th class |

**The freeze is the load-bearing half**, and it is not the same thing as the gate:

> *"if i start to craft i can lvl up to L2@100% at lvl 20 - but my exp freezes until i reach the next
> class - i need to become 40lvl + 3rd class then the l2@100% becomes l3@0%."*

So crafting exp **accumulates up to the last mark its band allows and then stops dead** — it is not
banked and does not spill over. A level-20 character maxes at **L2 @ 100%**; the instant they reach 40
and take the 3rd class, that becomes **L3 @ 0%** and the bar starts moving again. Same at 76 for L5.

🔑 **Cap the exp, do not bank it.** Banking would let a level-20 character with nothing else to do grind
out L6 the day they hit 76, which is the exact thing the gate exists to prevent — *"not i just to make
10 chars to sit in town and craft"*. Freezing at 100% is also the version you described.

---

## 3. Getting a profession: the master's quest

> *"U go to the 'Master apothecary Roger' or watever → U accept a quest → he explains what a apothecery
> can craft and other means of aquirering the items(potions) like drop/bosses etc to lure you so u
> become one of his aprenteces.. → u compleate the quest and u can take his proffesion."*

Five masters, one per profession, each with a joining quest. The quest is the **explainer** — its step
text is where "here is what this profession makes, and here is how else you could get those items"
lives, because that is the moment a player is choosing.

The quest is gated at **character level 20**, the same as L1 itself: a profession you cannot use for
twenty levels is a trap, and it would let a player commit before seeing any of the five.

### Quitting — and your ruling on re-joining

> *"if some1 desides that he dont like the proffesion can go to his master and quits (losing all his
> levels) → then he can go to the other master and start the quests and at lvl 0."*

Talk to **your own** master → Quit → profession `None`, crafting exp **0**, crafting level **0**.

**Your ruling (2026-08-12):** *"Skip the quest if it's once done, but still lose levels if switching.
Like a mix from both."* So the two halves are decoupled:

- **The QUEST is remembered forever, per profession.** Once you have completed the Apothecary's joining
  quest, that master will take you back on the spot — you have already heard the speech. This needs no
  new storage: joining quests are ordinary quests and `CompletedQuests` already persists.
- **The LEVELS are lost every single time you switch**, quest or no quest. Coming back to a profession
  you once had puts you at **L1, 0%**, exactly like a stranger.

🔑 The distinction is exactly right and worth writing down: the quest is *knowledge* and you cannot
un-know it; the levels are *practice* and you lose those by walking away. It also means swapping back
and forth is cheap in time and ruinous in progress, which is the correct shape — the cost of a bad
choice should be the grind, not a repeated cutscene.

⚠ **Quitting is the one destructive action in the feature**, so it confirms with the loss spelled out
in numbers ("You are L4, 62%. Quitting sets this to L1, 0% and it cannot be undone."), the same way the
Mindwriter and the stat basket do.

---

## 4. Where crafting HAPPENS

Your ruling was *"better at NPC — and craft happens with their respected masters"*, against my original
argument that refining a mat you just picked up should not need a trip to town.

**Built as: the window opens either way, the ACTION needs the master.** Menu → Craft still opens it, in
**browse** mode — every recipe, every `have/need` count, your level and exp, all readable in the field,
with the craft button disabled and a line saying *"Visit <master> in <town> to craft."* Talking to your
master opens the same window with the buttons live.

🔑 This is an interpretation and worth an explicit yes/no from you. The reason it is not simply
"the window only exists at the NPC": the whole value of the `have/need` colouring is knowing **what to
farm**, and that is a decision you make in the field, not standing in front of the man who would have
sold it to you. Nothing can be *made* away from the master, which is what you actually asked for.

---

## 5. `BL-40` — the output is absurd, and the new ladder is the fix

> *"A lvl 30 Potion Crafter had crafted **450 uncommon potions** … (I crafted about 15 uncommon wood).
> A lvl 30 Scroll crafter had crafted **690 uncommon attri scrolls**."*

### The root cause, measured

It is **not** a loop and **not** a batch button. `HandleCraft` crafts exactly once per tap, there is no
quantity spinner and no cooldown. The generosity is in a **ratio**, and it is one specific mismatch:

| | |
|---|---|
| **Refining** costs | **7 mats in → 1 out** (5 same-type + 2 cross), guaranteed |
| **`craft_potion_healing`** costs | 5 **Common** mats → **5 "Uncommon Healing Potion"** |
| **`craft_scroll_uncommon`** costs | 5 **Common** mats → **5 D enchant scrolls** |

🔑 **An UNCOMMON good is being made out of COMMON materials, five at a time.** One common mat becomes
one uncommon potion — while the refiner next to him pays *seven* commons for a single uncommon *mat*.
Crafting is therefore **7× cheaper than refining** at every rung, which is why the potion crafter had
450 of them: at ~1.76 common mats per mob kill, 450 potions is about 250 kills.

### The fix falls out of §0 and needs no new rule

> **A recipe's INPUT rarity must equal its OUTPUT's rung.** L1 makes Common goods out of Common mats;
> L2 makes Uncommon goods out of Uncommon mats.

That is already what §0 says the crafting levels mean, so `BL-40` is not a separate patch — it is what
happens when the ladder is enforced. The healing potion keeps `OutputQty 5`; what changes is that it now
eats **5 Uncommon mats = 35 common-equivalents** instead of 5 Commons. **A flat 7× cut at every rung**,
which lands his 450 potions at ~64 and the 690 attribute scrolls at ~99, without touching drop rates,
prices, or the `OutputQty` numbers he has already played with.

⚠ It also means the two consumable professions genuinely have nothing to make at L1 until `BL-57` is
authored — see below. Under the old model that was cosmetic; here it is load-bearing.

### The second, smaller leak

`GameHub.Craft` is the only crafting hub method with **no `Sessions.ContainsKey` check** and no rate
limit — every other debug-adjacent method has one. Not the cause of `BL-40`, but it is the reason a
craft can be tapped as fast as the phone can send, and the `66n` x500 stall was the same shape of
problem. Worth closing while the file is open.

---

## 5b. 🔵 THE ONE THING THAT DOES NOT FIT: gear is graded, not rarity-ranked

Found while building, and it needs your ruling before the smiths work at all.

§0's identity — *crafting level = rarity* — is exactly right for **materials and consumables**, and it
is how you described them (*"in L2 tab i see only the uncommon mats … and none of the uncomon potions"*).
It does **not** work for gear, because **every craftable gear recipe outputs a Mythic item**. The
authored tables *are* the Mythic rung (0.59.x); Common→Legendary copies of a sword are drop-only. So
deriving the rung from rarity files all **135 gear recipes at L6**, and measured just now:

```
WeaponSmith   L1-reachable:   2  (of 67)      PotionMaster  L1-reachable:  14  (of 18)
ArmorSmith    L1-reachable:   2  (of 68)      ScrollScribe  L1-reachable:   4  (of 11)
Jeweler       L1-reachable:   2  (of 29)
```

The three smiths would be able to craft **nothing but their two refines** until L6. That is worse than
the bug `BL-57` reported, and it is not what you asked for.

🔑 **Your own spec already says the answer, and I missed it on the first read.** For the exp rule you
wrote *"a lvl takes lets say 10 items of the same **GRADE** to craft — lower 1 **grade** = 3 times more,
higher 1 **grade** gives 20% more exp"* — grade, four times, never "rarity". You used *rarity* only for
the mats-and-goods visibility rule. So the rung is **"the tier of the thing you made"**, which is
*rarity* for a material or a consumable and *grade* for a piece of gear.

Gear has **seven** grades (`GradeLevels`: F=1 · E=20 · D=40 · C=52 · B=61 · A=76 · S=80) against six
crafting levels, so two of them must share a rung. Fitting them to the character-level bands you
already fixed (L1-2→20 · L3-4→40 · L5-6→76) leaves exactly one sensible arrangement:

| | L1 | L2 | L3 | L4 | L5 | L6 |
|---|---|---|---|---|---|---|
| **gear grade** | F (1) | E (20) | D (40) | **C (52) + B (61)** | A (76) | S (80) |
| char floor | 20 | 20 | 40 | 40 | 76 | 76 |
| mats / consumables | Common | Uncommon | Rare | Epic | Legendary | Mythic |

**C and B share L4** because they are the only pair whose item levels (52 and 61) both sit inside the
same character band (40-75). Every other arrangement puts a grade behind a character gate above its own
level — B gear is usable at 61, so parking it at L5 would mean a level-61 player could wear it and not
make it.

⚠ **I have NOT built this mapping** — it is one table and five minutes, but it decides what every smith
can make at every rung, and you have been clear that authored ladders are yours. Say yes and it goes in
as written.

## 6. What else is in the neighbourhood

- **`BL-57`** — Potion Master and Scroll Scribe have **no L1 recipe at all** (*"and my luck i picked
  exactly those :)"*). Under this design that is worse than before: L1 is now where you earn your way to
  L2, so those two professions cannot start.
  - ✅ **Potion Master: done.** `potion_minor` ("Common Healing Potion") is the entry rung of a line he
    already makes the other two rungs of — no conflict with anything.
  - 🔵 **Scroll Scribe: YOUR CALL, and it blocks him.** Both candidates collide with a ruling you have
    already made, so I did not pick one:
    - `attrscroll_common` — you reserved it as **drop-only on purpose**: *"the attribute economy has a
      faucet the scribe can't flood."*
    - `scroll_common`, the **E-band normal enchant scroll** — fits his existing line exactly (he makes
      the D and C normal scrolls), but it is the very item `62j` cut **30×** for flooding you with 80
      scrolls by level 28. A craft at ~1 common mat per scroll would undo that from the other end.
    - My recommendation: **`scroll_common` at `OutputQty` 1, not 5.** Five Common mats for one E scroll
      is ~3 kills each — far tighter than the pre-`62j` faucet, and it gives the scribe a real L1. But it
      is a number on top of a ruling you just made, so it is yours.
- **`BL-41`** — the grade filter on the Gear page (62-63 rows). Still unanswered by you. The crafting
  level now hides most of that list on its own (an L1 smith sees only Common), so this may have solved
  itself; worth looking at before building a filter.
- **`BL-22`** — trash disassembles into mats. Separate, additive, and not needed for any of the above.
  Kept out of this build deliberately so the exp curve can be measured against a known mat supply.

---

## Your three rulings, 2026-08-12 — all settled

1. ✅ **Browse in the field, craft at the master** (§4). The window opens from the menu anywhere, with
   the Craft/Refine buttons dead and a line naming your master's town.
2. ✅ **The joining quest is gated at character level 20** (§3) — the same gate as L1 crafting itself.
3. ✅ **A quest once completed is never re-done; the levels are lost every time** (§3). Your words:
   *"Skip the quest if it's once done, but still lose levels if switching. Like a mix from both."*

## What this touches (from the code survey, 2026-08-12)

Everything below exists today and is what the build has to move:

| | today | after |
|---|---|---|
| `Profession` | `Game.Shared/Crafting.cs:5`, set by a **self-pick hub method**, permanent | granted by a master's quest, quittable |
| level/exp | **does not exist** | `CraftLevel` + `CraftExp` on the entity, 2 new DB columns |
| `Recipe` | `Recipes.cs:13` — `LearnLevel` is a CHARACTER level | gains a crafting-level rung; character level stays as a second gate |
| recipe count | ≈173 (WeaponSmith 60 · ArmorSmith 62 · Jeweler 25 · PotionMaster **16** · ScrollScribe **10**) | +2 minimum (`BL-57`) |
| masters | **no crafting NPC exists at all**; `NpcRole` has no crafting role | 5 masters, 1 new `NpcRole` |
| the window | `GameUi.Crafting.cs`, opened from the **menu** (`GameUi.World.cs:706`) | same window, browse vs craft mode |
| choosing | `GameHub.ChooseProfession` — ⚠ the one hub method with **no session check** | replaced by the quest; check added |

⚠ **This is a DB schema change** (`CraftLevel`, `CraftExp` on the character record), so it needs the
usual `game.db` delete-and-recreate, and a protocol bump for the `CraftingUpdate` DTO.
