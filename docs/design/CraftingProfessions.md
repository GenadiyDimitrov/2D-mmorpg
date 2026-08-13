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

## 5b. ✅ RULED 2026-08-13: gear is GRADE-based, and F is not craftable at all

Found while building; **answered by you on 2026-08-13** — his ruling is §5c below. The diagnosis is
kept because it is why the question existed.

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
crafting levels. My proposal was to make **C and B share L4**; that is superseded — see §5c.

---

## 5c. ✅ THE GEAR LADDER, as he ruled it (2026-08-13)

> *"Gear is graded -> F gear can be uncraftable .. rly no point to craft F grade ... its mostly to get
> you to 20 (as u get free mytic @10/15) ... so 7 grades - 1 = 6 .. L1 is E, L2-D, L3-C, L4-B, L5-A,
> L6-S"*
>
> *"just the idea is grade based not as much as rarity based"*

**F is not craftable.** That is what makes the ladder exact — seven grades minus F is six, against six
rungs, with nothing shared and nothing invented:

| | L1 | L2 | L3 | L4 | L5 | L6 |
|---|---|---|---|---|---|---|
| **gear grade** | E (20) | D (40) | C (52) | B (61) | A (76) | S (80) |
| char floor | 20 | 20 | 40 | 40 | 76 | 76 |
| mats / consumables *(unchanged, §0)* | Common | Uncommon | Rare | Epic | Legendary | Mythic |

🔑 **His arrangement is strictly better than mine, for a reason worth keeping**: my C+B share put B at
L5, behind the character-76 gate, so a level-61 player could *wear* B gear and not *make* it. Dropping F
removes the pair-up entirely and every grade now sits at or below its own character band. The one
remaining offset is benign: an L2 smith at character 20 can craft **D** gear he cannot wear until 40 —
that is fine, gear is tradable, and making what you cannot yet use is a normal crafter role.

### Only Legendary and Mythic are craftable — everything below is drop-only

> *"the only craftable gears should be legend, mytic (others are drop based anyways)"*

This settles the §5b diagnosis from the other end. The doc noted that *every craftable gear recipe
outputs a Mythic item* and treated that as the awkward part; his ruling makes it the design. A gear
craft now has **two possible successes and a failure**, rolled per attempt:

### Gear craft is NOT 100% — the success table

> *"the gear is not crafted at 100% ... E - (50% for mytic, 40% for legend, 10% fail); D - 45m, 40l,
> 15fail; C - 40m, 40l, 20fail; B - 30m, 40l, 30fail; A - 20, 30, 50fail; S - 5m, 20l, 75 fail"*

| rung | grade | → Mythic | → Legendary | fail (mats lost) |
|---|---|---|---|---|
| L1 | E | 50% | 40% | 10% |
| L2 | D | 45% | 40% | 15% |
| L3 | C | 40% | 40% | 20% |
| L4 | B | 30% | 40% | 30% |
| L5 | A | 20% | 30% | 50% |
| L6 | S | **5%** | 20% | **75%** |

A fail **consumes the materials and produces nothing**. This is the first real sink in the crafting
economy and it is what makes the mat costs below survivable as a design.

### Material costs, by grade

> *"the E needs common mats(500-1000)+~10 uncommon ... the D needs uncommon(100-200-500)+2~5 rare, C
> need rare(100-200)+1~2epic, B-epic(100-200)+1~2legend, A-legend(100-200)+1~2mytic, S-legend(1000~2000)
> +(10~20)mytic ... depending on drop rates/amount"*

| rung | grade | bulk mat | + top-up mat |
|---|---|---|---|
| L1 | E | Common ×500-1000 | Uncommon ×~10 |
| L2 | D | Uncommon ×100-200-500 | Rare ×2-5 |
| L3 | C | Rare ×100-200 | Epic ×1-2 |
| L4 | B | Epic ×100-200 | Legendary ×1-2 |
| L5 | A | Legendary ×100-200 | Mythic ×1-2 |
| L6 | S | Legendary ×1000-2000 | Mythic ×10-20 |

The shape is consistent: **a pile of your own rung's mat, plus a few of the rung above.** His ranges are
explicitly "depending on drop rates/amount", so the exact number inside each range is a **measurement,
not a choice** — pick it against real drop rates with `tools/BalanceMatrix` / `EffectiveRate`, never by
hand.

⚠ **Two things to raise before authoring the numbers, both about L6.** S combines the largest cost on
the ladder with a **75% fail**, so the expected spend per *successful* S item is ~4 attempts — on the
order of **4,000-8,000 Legendary and 40-80 Mythic mats**. And it can still land on Legendary (20%), so
a *Mythic* S item is one roll in twenty: ~20 attempts, ~20,000-40,000 Legendary mats. That may be
exactly the intent — it is the counterpart to the 60kk shop price he mentions below — but it is a big
enough number that it should be measured and shown to him before it ships, not discovered in a playtest.

## 5d. ✅ THE TWO CONSUMABLE PROFESSIONS, as he ruled them (2026-08-13)

Gear-grade parity is the organising idea for these two as well — *"just the idea is grade based not as
much as rarity based"* — so each rung names **the gear grade it serves**, not only a rarity of output.

### Scroll Scribe — level-gated, and L1 is deliberately not gear-related

> *"scroll scribe is lvl gated .. l1-20lvl can craft common resurection scrols, scrols of return;
> (nothing gear related) L2- scrol enchant common(D), attri uncommon(D) (he gets gear related for the
> grade); L3- atri rare, scrolls rare .. anytign for C grade + basic scrolls for buffs; l4 - anything for
> B grade; L5 - any scrolls (anything) for A grade + other buff scrolls; L6 - S grade stuff + ultimate
> escape + ultimate resurect"*

| rung | serves grade | can craft |
|---|---|---|
| L1 (char 20) | **none** | Common resurrection scrolls · scrolls of return |
| L2 | D | Common enchant scroll · Uncommon attribute scroll |
| L3 | C | Rare attribute · Rare scrolls · anything for C · **basic buff scrolls** |
| L4 | B | anything for B grade |
| L5 | A | any scrolls for A grade · **other buff scrolls** |
| L6 | S | S-grade stuff · **Ultimate Escape** · **Ultimate Resurrect** |

🔑 **The Scribe's ladder is offset one rung from the smiths': his gear service starts at D (L2), not E
(L1).** That is what buys the non-gear L1, and it means five grades (D→S) fill five rungs (L2→L6)
exactly. No rung is empty and `BL-57` needs no invented recipe.

⚠ **One authoring detail to check against the item defs, not assume**: he writes *"scrol enchant
common(D)"*, but §6's earlier note describes `scroll_common` as the **E-band** normal enchant scroll. So
either the id's band and his grade label disagree, or "common" here names the rarity and not the band.
Read `Items.cs` before wiring the rung — do not guess which.

### Potion Master — HP and buff pots alternate, "dash" climbs every rung

> *"potion master the same idea -> L1 - common hp pots + dash, l2 - common buff pots + unc-dash, l3 -
> uncommon hp pots + rare-dash, l4 - uncommon buff pots + epic-dash, l5 - rare hp pots + legend-dash,
> l6 - rare/mythic wahtever is the strongest buff pots + mytic dash --- and somwhere and elemental stones
> + skill stones"*

| rung | potion line | "dash" rarity |
|---|---|---|
| L1 | Common **HP** pots | Common |
| L2 | Common **buff** pots | Uncommon |
| L3 | Uncommon **HP** pots | Rare |
| L4 | Uncommon **buff** pots | Epic |
| L5 | Rare **HP** pots | Legendary |
| L6 | Rare/Mythic — the strongest **buff** pots | Mythic |

The pattern is a **two-stride alternation**: HP and buff lines take turns, each advancing a rarity every
second rung, while *dash* advances every single rung and is the one line that reaches Mythic. This keeps
`BL-57`'s existing ✅ answer intact — `potion_minor` (Common Healing Potion) is exactly an L1 Common HP
pot.

**Also owed a home:** *"somewhere ... elemental stones + skill stones"* — he did not pin a rung. Not
invented here; it needs one line from him, or a proposal measured against where those stones are used.

### Both masters: chests, and tradable temporary boxes

> *"both scroll and pots masters can craft a treasure chests with some random loot of scrolls/ports etc
> .. to fill up the gap of 60kk for mytic S grade in the shop - may be a potion master can craft runes
> (tradable temporary - war/spell rune boxes - 1h,2h) while scroll can craft (tradable temporary exp/sp
> boxes - 5-30%, 1h,2h).. something like that"*

- **Treasure chests** with random scroll/potion loot, craftable by both. Stated purpose: **a gold sink /
  value faucet to close the 60kk gap to a Mythic S item in the shop.**
- **Potion Master → War / Spell rune boxes** — tradable, *temporary*, 1h and 2h durations.
- **Scroll Scribe → EXP / SP boxes** — tradable, temporary, **5-30%**, 1h and 2h.

⚠ His own *"something like that"* marks this as a **sketch, not a spec** — the shape is ruled, the
numbers are not. It also leans on the held **War Rune / Spell Rune** (the accepted replacement for
per-hit damage consumables) and on the `BL-01` premium reward runes, so it should be specced against
those two rather than as a new system. **Do not author it in this build.**

### What is still open on crafting after these rulings

- 🔵 The **exact number inside each mat range** (§5c) — a measurement against drop rates.
- 🔵 **Mats for the Scribe and the Potion Master**: *"the mats consumed i have no idea how much and
  what"* — explicitly delegated. Propose, measure, show him.
- 🔵 Where **elemental + skill stones** sit on the Potion Master's ladder.
- 🔵 The chest / rune-box / exp-box economy above (sketch only).

## 6. What else is in the neighbourhood

- ✅ **`BL-57` — ANSWERED 2026-08-13, and the answer dissolves the conflict.** Both of my candidates
  collided with a ruling of his (`attrscroll_common` is deliberately drop-only; `scroll_common` is the
  item `62j` cut 30× for flooding). **He picked neither**: the Scribe's L1 is simply *not gear-related*
  at all. See §5d — L1 is Common resurrection scrolls and scrolls of return. Both prior rulings survive
  untouched, which is why this is the better answer and not a compromise.
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
