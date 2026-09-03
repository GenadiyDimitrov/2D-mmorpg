# Backlog archive — rulings that were replaced

The other half of [Backlog.md](Backlog.md). When you re-spec something, the **new** version is the
only one in the backlog; the version it replaced is cut and pasted down here, dated, under the same
id. Nothing is deleted — a superseded ruling is still the reason the code looks the way it does, and
this is where you look when you wonder why.

**This file is not a done-list.** Shipped work lives in [CHANGELOG.md](CHANGELOG.md); closed
playtests live in [testing/Playtest-Archive.md](testing/Playtest-Archive.md).

Two kinds of entry:

- **`BL-nn`** — a backlog entry that was rewritten before it was built. The old text sits here.
- **§ a superseded design** — a ruling that was already *built*, then reversed or replaced. It has
  no backlog id because it was never owed; it is here so the reversal is findable.

---

## Superseded backlog entries

### `BL-78` — "mobs are too easy and the HP curve is ~3× short"
**Replaced 2026-08-19**, after item 3's research came back and you ruled on it. Two things in the text
below turned out to be wrong, and both are worth keeping because they are the reason the fix looks the
way it does. (a) It read the deficit as **HP**; measured, our base HP shape is 0.87 → 1.08 of IG's from
level 40 up and the gap was in **defence and attack** (~0.5×), which is what 0.73.0 refitted. (b) Your
15k and 21k creatures are real, but they come from IG's **`HP Increase` multiplier tag** — 23% of its
roster carries ×2-×5 — not from the shared curve, so the fix is authoring `MobMod.Hp`, not moving the
lever `BL-47` warned about spending. Full measurement: `balance/MobCurveVsIG.md`.

> - `BL-78` 🔴 **MOBS ARE TOO EASY AND THE HP CURVE IS ~3× SHORT — playtest 25, and it is the biggest
>   balance find in the file.** Your words: *"now mobs as general feel easy ... tank get hit fo 30 .. others
>   for 100-200 but the rogue almost one blow it ... mage one/two shot it .. and there is no thrill in
>   fighting"*, fought in **uncommon t40 at 40-45, uncommon t52 at 60, epic t76 at 80** — i.e. deliberately
>   under-geared, not a best-in-slot run. Four separate things, and they do not all move together:
>   1. 🔴 **HP.** *"the 80 mobs should have 15k not 5 .. the 60 lich is with 1500"*. Today
>      `MobBaseStats.Hp(level) = 40 + 0.8·level²` → **5,160 at 80**. Your 15k is **×2.9**, and it is a
>      curve change, not a constant: at 60 it reads 2,920 against a lich you measured at 1,500.
>      ⚠ **This is the one edit that moves every creature in the game at once** — and per `BL-47` it is
>      exactly the lever a per-creature pipeline would have cost you.
>   2. 🔴 **A CASTER MOB IS NOT A SQUISHY MOB** — *"caster mobs are not weaker than the other, they just use
>      spells (and have a bit less pdef, evasion not twice less)"*. The caster archetype currently pays
>      twice (low P.Def **and** low HP) for a role that should cost it a little P.Def and nothing else.
>   3. 🔵 **THE RESEARCH YOU ASKED FOR** — *"can we have some reaserch for 5-10 mobs of every lv of the IG to
>      compare its stats to our of the same lvl - i have the feeling that our mobs are weaker or atleast
>      with alot less hp"*. Owed as a table: IG creature HP/P.Def/P.Atk at matched levels against ours, so
>      the new curve is fitted rather than guessed. ⚠ **`MobBaseStats` was originally derived from IG
>      reference data**, so this is a re-derivation, and it should say what changed and why.
>   4. 🔵 **AND IT MAY BE THE PLAYER CURVE TOO** — *"a healer with 1500 hp getting hit for 300 is abit harsh
>      .. one time less defence cuz of robe the second hinder is the amount of hp"*. A robe class paying
>      for its role twice, on the player side, is the same complaint as (2) pointed the other way. Decide
>      these two together or a healer ends up in the same hole a caster mob just climbed out of.
>   🔑 **Measure it in `BalanceMatrix` before and after.** A 3× HP change moves every TTK, every farm time,
>   every EXP-per-hour figure and the `BL-13` boss table, all of which are printed by that tool.

### `BL-13` — "a flat ×100 cannot hit 360s at every level; do the late bosses come DOWN?"
**Replaced 2026-08-16/17 by his playtest-25 ruling**, which is *"bosses should take 10-15 even 30 mins
to kill"*. The measurement below is still correct and is quoted on the live entry; what it got wrong is
the **question it asked him**. It read the 11× spread as a defect and offered "bring the late bosses
down to 360s" as one of two options — but 360s was never his target, it was mine, extrapolated from an
old *"6 minutes"* remark he had not repeated. Against the band he actually wants, the late rows were
right all along and the early ones are 2-7.5× too fast. Kept as the reason `BalanceMatrix`'s boss
section exists and prints what it prints.

> A level-20 field boss spawns with exactly 36,000 HP = `MobBaseStats.Hp(20)` × the rank's ×100, and
> the scale survives every recompute. Nothing is being eaten. But measured against your 6-minute /
> 3-DD target (new `BalanceMatrix` section, ceilings — no downtime): 20 → **80s** (4.5× too fast),
> 40 → 296s (about right), 60 → **684s** (~2× too slow), 76 → **888s** (~2.5× too slow), 85 → 693s.
>
> A flat ×100 **cannot** hit 360s at every level: mob HP grows as `0.8·L²` while a geared party's DPS
> is nearly flat (448 → 525), so the boss rank swings **11× in difficulty between 20 and 76**. Nothing
> was changed — the curve is a ruling. **Two questions:** should a level-20 field boss take a level-20
> party six minutes, and do the late bosses come DOWN to 360s or does the target rise with level? The
> table prints what each level would need (×448 / ×122 / ×53 / ×41 / ×52).

### `BL-47` — "do not migrate mobs onto the player pipeline; finish the passive layer instead"
**Replaced 2026-08-16 by his playtest-24 answer** (`86b`), which is *migrate*. The recommendation
below stood for one day and is kept because it is the reason `MobsAsPlayers.md` reads the way it
does, and because two of its four arguments survive his ruling intact (the base curve is reference
data; `RecomputeDerived` branches on `Kind` in 21 places). The one it overturns is the third:

> ⚠ **The migration itself is measured and the recommendation is NOT to do it** — no gear combination
> closes the gaps (`G3.2`), the reconciliation would have to come from per-band passives anyway, and
> rebuilding on the player pipeline would discard the IG-measured base curve. The counter-case is
> real and stated in the doc: mob-player fights *are* playable, and creatures that hold visible gear
> is a design preference no table can settle. **§8 of the doc lists six questions; B is the one that
> gates everything.**

⚠ **Why it was overturned, recorded honestly, and it was not a close call.** The `G3.2` sweep behind
*"no gear combination closes the gaps"* had two blind spots he found and I had not: its enchant axis
stopped at **+16** (the player's practical ceiling — but a mob's enchant is an authored number, and his
example is **+60**), and it moved **every slot together**, so *"S grade Mace ... and B grade leather"* —
an over-enchanted weapon over under-grade armour, the one shape that can fix a mirror — was outside the
sweep by construction. **The claim was true of that sweep and overstated as a claim about gear.**

`G3.7` re-asks it his way and the answer flips: **12 of 16 archetype-levels land inside his ×2 passive on
all four stats at once**, the worst single miss falls from 185-221% to **94%**, and the biggest attack
passive still needed anywhere is **×1.60**. The optimiser also picked his loadout unprompted — lowest-tier
armour, weapon at level tier plus enchant. The four failures are all one failure, **the Nuker's HP**
(×2.01 → ×3.48), which his own *"and hp boost"* had already allowed for.

---

## Superseded designs — built, then replaced

### § Shields: "don't add shield P.Def to the pool" and "cut it 5×" → **option 3**
**Replaced 2026-08-12** (`267313d`). You offered three ways out of the double-dip and chose the
third. Option 1 was to keep the shield's P.Def out of the overall stat and only apply it on a
successful block — you rejected it as *"invisible"*: *"with .3 it means leaving one hand open u can
equip another defence item (1h less p/mAtk so u get a pdef) — with .1 its invisible."* Option 2 was
the 5× cut alone, which left a mage at ~15.5% and a tank at ~24% — too close together. **Option 3 =
option 2 plus the tank's shield passive ×5**, and only that passive: *"sheild_mastery.Shield_PDef
will be the only part that will increase 5 times, the sheild chance, arrow defence and other
passives, sets and buffs that increase the shieldPdef/chance etc are kept as is."*

### § The tutorial: `69b`'s fix → `63j`'s re-spec
**Replaced 2026-08-12.** The 0.60.1 quest-step-supplies-its-props fix was shipped, then you played
it and wrote a better ruling: *"I have given better rulling in the `63j`."* No initial boxes at all,
boxes handed out by Cera and Pell exactly when they are opened, **plain** boxes decided by base
class rather than selection boxes, and the four-beat order travel → put an attack on the bar →
target-and-use → kill 5. Built the same day.

### § Timed / bound items: cloned `_bound` item defs → **per-instance tags**
**Replaced 2026-08-12** (`ed75bac`). The 0.54.0 newbie kit was built as cloned defs
(`ItemCatalog.BoundCopies`). You accepted the clone for that kit but refused it as the mechanism:
*"it is a REAL item with tags — never a new server-side def."* Five per-instance fields now carry it,
and the displayed tag is derived from them rather than stored.

### § Enchant: a PERCENTAGE of the item's stats → a **FLAT offset**
**Replaced 2026-08-11** (0.60.0). `BonusAt` is gone; the offset is the same for every class and is
chosen by **grade**, not by rarity. ⚠ Your objection to the class-flat half is live in the backlog
as `BL-12` — it has not been answered, so the flat model stands until it is.

### § Weapon crit-rate roll: a FLAT `CritRateFlat` → a **multiplier**
**Replaced 2026-08-07** (`0d`). The roll was being fed in as `value / 100`, so a maxed roll was +30
*percentage points* and it collapsed the 3:2:1 weapon identity the whole crit model exists to
create. Your ruling made it multiply, and raised the sword's ceiling 30% → 90% so a max roll lands
the two weapons together (sword `88 × 1.9 = 167`, dagger `132 × 1.3 = 172`). ⚠ A large dagger/bow
nerf at max roll, stated by you and intended — and still never played.

### § Evasion Mastery: raised evasion itself → **raises the FLOOR only**
**Replaced 2026-08-06** (`M9`, 0.50.0). *"Once I turned rogue my evasion jumps a lot, and it
shouldn't."* The passive was worth ~32 points of raw evasion on top of the floor, which meant the
floor was always the binding number anyway. Crit rate became your full IG model in the same pass,
and the rogue's ×1.20 crit passive moved onto Weapon Mastery at level 20.

### § The ±20 level gap: a hard **lockout** → the floors stay live at every gap
**Replaced 2026-08-07** (`M1`, 0.53.0). Step ordering in `ResolveAvoidChance` swapped: level gap
first, the `[5%, 95%]` band and the floors **last**. `G = 1.0` now means "pinned to the edge of the
band", not "cannot be hit". Your reason: `ExpCurve.GapZero = 13` already pays zero exp and zero
drops seven levels earlier, so the lockout was doing no work.

### § Magic resist: **dropped, never to be a stat** → mRes is damage reduction
**Replaced 2026-08-09** (0.58.2). The old roadmap line said magic mitigation is only M.Def plus the
fizzle floor, and that *"mRes in owner CSVs = the fizzle floor"*. It isn't — mRes is a damage
reduction, and the fail chance is its own formula:
`fail% = round(1.3^(defLvl − atkLvl) × defMod × weaponMod)`, clamped at 95%, with parity anchored at
**1% fail**. ⚠ The "DROPPED" section at the bottom of `Roadmap.md` still carries the old wording and
is stale there.

### § Group buffs: a group *stacks alongside* its singles → **a group is ONE buff**
**Replaced 2026-08-01** (0.42.0, reversing 0.36-0.41). A group carries `GroupRank = 100 + level`,
every child's magnitudes and a `CoveredKeys` list, so it always outranks and evicts its singles and
a potion can never override it. Authoring rule that came with it: a group must be ≥ the best single
in **every** family it covers.

### § Spell range: scaled by the caster's class TIER → **per-spell**
**Replaced 2026-07.** `SkillMath.EffectiveRange` returns the skill's own `Range`, authored per
spell (heals short, healer attack ~750, nuker ~900, base nuke 600). The one exception kept is **bow
skills**, which still scale with the archer's bow tier (350/600/900) to match the basic-attack range
growth.

### § A class grants STATS → **identity is the kit**
**Replaced 2026-08-10.** The 2nd/3rd-class `ClassFlatBonus` fields were deleted. Two disciplines of
one archetype run identical stats and differ only in what their skills do. ⚠ The standing rule that
came with it: **do not re-home the same numbers as invented passives** — *"w8 on the 40+ csvs"*.
`ClassFlatBonus` survives as an armor-set type only. What still legitimately varies by class: the
per-archetype HP/MP growth curves and `BasicAttackMultiplier`.

### § Buff scrolls: 48 scrolls, dropped by mobs → **17, and the Blessing Box is the only source**
**Replaced 2026-08-05** (`E3`). One scroll per buff at the top rung, Rare, bound; the Rare potion
rung deleted (24 → 18 potions); **no buff-scroll drops from anything**; 250k at the Apothecary for a
pick of 10. The game's first real gold sink. Consumables per kill fell 33% → 18.5%.

### § A gathering contract pays an **authored** exp number → `RewardModifier` × the creature's own
**Replaced 2026-08-01** (0.42.9). `QuestGather.RewardModifier` **is** your `QuestItemRewardModifier`,
and it multiplies the mob's own `MobExpReward`/`MobGoldReward` at its natural level. That is what
keeps a repeatable contract level-appropriate with nothing to re-tune, forever.

### § Gear regen: a flat MP/s per item → a **percent roll**
**Replaced 2026-08-03** (0.45.0). A flat +9 ring was worth +22.7 after the multiplier stack and
dominated the level curve at every level. Rings now roll a percent, 1-5% by grade. The flat types
stay in the enum for pre-0.45 saves and nothing rolls them.

### § DEX → **AGI** (naming only)
**Replaced 2026-08-09** (0.58.1). Every player-facing surface reads AGI. ⚠ The four stat-swap skill
**ids** still spell `dex` on purpose: an id is a persisted key, and renaming one would delete a
15kk purchase.

### § `BL-26` The vendor half of the buy-back design — **a longer sold list**
**Closed 2026-08-14** — not built, superseded by your own later ruling. The entry read: *"The vendor
half of the buy-back design — a longer sold list. Flagged 'still open, still not urgent' and never
revisited."* It descended from the ORIGINAL design (`Roadmap.md:126`, *"a buy-back menu — last 10
deleted/sold; free restore for deleted or sold-for-0"*). Playtest-19's **`M14`** replaced it — *"Cap
the vendor buyback list at 10-15 items"* — and that shipped as `GameConstants.BuyBackSlots = 12`,
with the deleted half split off into its own 5-slot `Restorable` list (`C18`, your own two-list
fallback: a shared list would let a selling spree push the one thing you meant to undo off the end).
Lengthening the list now would walk back the cap you asked for.

### § `BL-59` Resurrect / party / PvP-flag rules — the **SELF-based** version
**Replaced 2026-08-14** (0.66.0). He re-specced the whole rule TARGET-based; the new text is in
`CHANGELOG.md` and the entry is built and deleted from `Backlog.md`. The superseded text read:

> `BL-59` 🔴 **Resurrect / party / PvP-flag rules (your find #9).** Three parts, none built:
> Ultimate Resurrection scrolls should be tradable (*"atleast the one that drop and from the admin
> menu"*); you cannot res a party member while **you** are flagged, but may res or heal a PK while
> unflagged; inviting and trading with PvP-flagged players must work, with PK still trade-blocked.

The load-bearing difference is the middle clause. The old rule asked about the CASTER's flag ("while
**you** are flagged"); the new one asks about the TARGET's, and adds that supporting a still-flagged
player flags *you*. Those are different systems, not a rewording — the old one restricted a clean
player's ability to act, the new one prices helping an outlaw. Trade also moved: it used to be "PK
still trade-blocked" alongside a flag block, and is now PK-only, so a purple flag no longer bars a
trade at all.

---

## Closed on 2026-08-27 — the fourteen-ruling message

He answered fourteen entries in one message and closed nine of them outright. The texts that were
**deleted** from `Backlog.md` are kept here; the ones that were **rewritten** keep their old version
below, under the same id, as the rules require.

### `BL-94` — "a fizzled spell should do NOTHING, not a third"
**Closed 2026-08-27, and it was HIS OWN WORDING that was wrong, not the code.** *"u can remove it ..
failing a spell is 1/3 dmg - IG is like that not 0 my wording was wrong."* The `damage / 3` payload
stays exactly as it is. The half of the original question that WAS a bug — the fizzle chance reading
the caster's level instead of the rung's — shipped in 0.81.2 and is unaffected. The deleted text:

> - `BL-94` 🔵 **THE FIZZLE FLOOR — a fizzled spell should do NOTHING, not a third.** Your ruling of
>   2026-08-24: *"shouldn't hit at all on the floor"*. … ❓ flat 0 on a fizzle, or 0 only once the fail
>   chance is at its ceiling (so a small fizzle still chips)?

🔑 Worth keeping: this is the second time a verbatim quote of his turned out to be a *phrasing* slip
rather than a ruling (the first was *"did the same for buffer"* on `BL-90`). A quote is evidence of
what he said, not proof of what he meant — when a quote asks for something that contradicts the
reference game, ask before building it. Not building it was correct here.

### `BL-10` — a floor under the fading bow-caster penalty
**Closed 2026-08-27 — deliberately no floor.** *"casting down with a bow is a choice .. u cast slower
if not buffer and if buffer u negate the penalty ... uf iyu want to be a nuker with a bow and kill -10
or lower enemys ok .. do it .. your choice."* The penalty fading as you punch down is the intended
shape: it is a build choice with a real cost, and a buffer can buy it back. The deleted text:

> - `BL-10` 🔵 **A floor under the fading bow-caster penalty.** The bow penalty currently vanishes
>   entirely when you punch down. You were asked whether you want a floor under it and the reply is
>   still empty. *(playtest-21 `64e`.)*

⚠ `BL-09` — the floor under the **wrong-weapon** magic penalty — is a DIFFERENT entry and is still
open. It was not covered by this ruling and must not be closed alongside it.

### `BL-12` — "enchant bonus should scale with what you put in"
**Closed 2026-08-27: the current model already IS his answer, and he had ruled it once before.** *"I
think i ruled they stay the same .. anyone that put time and effort in their enchanted gear should get
a bonus .. if a healer +16 spend months enchanting failing ets should be alot stronger than a warrior
with +3 gear."* Verified against `Game.Shared/Enchant.cs`: the bonus is **flat per enchant LEVEL**, so
+16 is worth sixteen rungs and +3 is worth three — the healer he describes is 5.3× the warrior on the
same slot. The comment block at `Enchant.cs:55-81` already carries his 2026-08-11 quote making the
same point. The deleted text:

> - `BL-12` 🔵 **Enchant bonus should scale with what you put in.** Your objection to the flat-offset
>   ruling, unanswered: *"not a warrior invest +3 and gets the same bonus as cleric +16."* Today the
>   offset is identical for every class, by grade. Needs your call before anything moves.

🔑 The word that caused the entry: "the same offset for every CLASS" was read as "the same total for
every ENCHANT LEVEL". It never was — per-class identical, per-level cumulative.

### `BL-16` — heal powers need re-authoring (the 20-35 ladder)
**Closed 2026-08-27 by the 40+ rungs, which is the second of the two exits the entry offered.** *"we
have the authored heal powers .. so max lvl heal can heal 1400-2000 and a healing power +2k for 15s ..
so a 4k heal is a good on a 10-15k hp tank ... then we have % based heal."* All three pieces are in
his own files and were checked: `healer 4th.csv` authors **Ultimate Heal 1400 → 2000** across 82-90,
**Healers Power +1000 → +2000 for 15s** across 80-90, and `healer 3rd.csv` authors **Urgent Heal at
15% of the pool** — so the % channel exists too. The 20-35 numbers stay untouched. Old text:

> - `BL-16` 🔵 **Heal powers need re-authoring — and they are YOUR numbers, so you have to move them.**
>   They sit at ~151-301 against a scale that has moved to ~1000. … **Landing your ratio needs Quick
>   Heal ≈ 970 power** … Two ways out and both are yours: send new 20-35 numbers, or let the 40+ rungs
>   (`BL-02`) carry it, since a ~1500-power quick heal is a 40+ rung by your own sizing.

The **new** half of his message — a group buff carrying a DIMINISHING % heal — is not this entry and
opened as **`BL-95`**.

### `BL-17` — re-author `BuffMagAtk`, and give magic-only buffs an explicit magic %
**Closed 2026-08-27: *"authored . working system"*.** The discrepancy the entry was blocked on
(`Force@25 = x1.55` in an old CSV against a shipped `+25%`) was settled by his own later authoring, in
explicit percent, and code and CSV agree today: `cleric 2nd.csv` **+25% @25**, `healer 3rd.csv` /
`buffer 3rd.csv` **+28% @44 and +32% @52**, against `Skills.BuffLadders.cs:287`
`Ladder(FamMagAtk, "Force", …, 0.15f, 0.25f, 0.28f, 0.32f)`. Old text:

> - `BL-17` 🔵 **Re-author `BuffMagAtk`, and give magic-only buffs an explicit magic %.** ⚠ **Re-marked
>   🔵 on 2026-08-14** … your CSV's Force@25 is `x1.55` while the shipped `FamMagAtk` rung is **+25%**.
>   Per your `xN.NN`-is-a-percent convention those may not even be the same claim. Not reconciled by
>   guessing — say which is right.

### `BL-23` — the coin curve (the ASSERTION version)
**Rewritten 2026-08-27.** He replaced the claim with a measurement request: *"explain - i want
potion/rune per hour consumation and golddrop/h .. to compare for fewe lvl rangees - for now at lvl 43
i have 5kk + gold so it dont seem like a problem."* The new entry is in `Backlog.md`; this is what it
replaced:

> - `BL-23` 🔵 **The coin curve.** Gear value follows the tier ladder while coin stays linear, so the
>   gap drifts to **51×** by level 76. The note in the archive is explicit that *"the real fix is the
>   coin curve, not another multiplier"* — every rate tweak since has been a patch over this.

⚠ **The 51× was never measured.** `--goldflow` (built the same day) measures the drift at **5.4×**
across 20→76, plus a genuine **cliff at 80** that the old text never mentioned. An assertion that sat
in the file for a fortnight was wrong by an order of magnitude in one direction and silent about the
sharper problem in the other — which is the argument for measuring before re-specing, again.

### `BL-24` — the enchant-scroll types
**Closed 2026-08-27.** *"it build ? why blue ?"* — it IS built (three types × six grades, 0.53-0.60,
plus the ratified 30× drop cut). The 🔵 was never about code: the entry existed only to hold open a
**conversation he asked for and never had**. Nothing is owed, so it is closed; if he wants the
discussion it can be reopened as a fresh id. The deleted text:

> - `BL-24` 🔵 **The enchant-scroll types — you wanted to discuss them.** *"ENCHANTS — you said you
>   want to DISCUSS them … bring it up when you are ready."* The three types (breaks / −1 / safe) ×
>   six grades shipped in 0.53-0.60; the conversation you asked for never happened. The 30× drop cut
>   (`62j`) is ratified and stays.

🔑 The lesson is about the FILE, not the feature: a backlog entry that holds a place for a conversation
looks identical to one holding an unbuilt feature. A 🔵 that is only waiting on a chat should say so in
its first line, or it reads as work owed forever.

### `BL-54` — newbie items through quests
**Closed 2026-08-27: already true.** *"the newbie set is givven from the starter quest."* Verified in
`Quests.Tutorial.cs`: the armour-choice and weapon boxes are handed out on the level-10 step and the
jewels + rune-choice boxes on the level-15 step. His original plan said 6/8/10; the tutorial rebuild
(`267313d`) moved each box onto the step that needs it, which is what the entry's own ⚠ asked to be
re-checked before building. It was checked; there is nothing left to build. Deleted text:

> - `BL-54` 🔵 **Newbie items through quests** — hand the starter weapon/armor/jewel boxes out at
>   levels 6/8/10. Your plan, never scheduled. ⚠ Re-check it against the tutorial as it now ships
>   (`267313d` moved every box onto the step that needs it) before building.

### `BL-55` — two real starter armor SETS
**Closed 2026-08-27: the placeholders were never placeholders.** *"the two real starter sets are the
newbie light/robe not a place holders."* The newbie light and robe sets in `ItemCatalog` ARE the
shipped starter sets — Ferrite Mythic, unsellable, untradable, 30-day timed, per his own 2026-07 rules.
No numbers are owed. Deleted text:

> - `BL-55` 🔵 **Two real starter armor SETS.** The current newbie light/robe sets are placeholders
>   waiting on your numbers.

### `BL-86` — the shutdown countdown is text, not a big red banner
**Closed 2026-08-27: the toast is accepted.** *"this is good enough - can be red text but its ok if
dont - noticable enoght."* The announcement ladder shipped in 0.78.0 and stays on the existing `Notice`
toast + System chat — which means it keeps working on clients built before the feature, the reason it
was done that way. Red text is explicitly optional; if a client batch is going out anyway it can be
coloured then, but nothing is owed and it is not a reason to cut an APK. Deleted text:

> - `BL-86` 🔵 **THE SHUTDOWN COUNTDOWN IS TEXT, NOT A BIG RED BANNER — your call whether that is
>   enough.** … Making it a red, large, and (under 60s) persistent overlay is a client-side element and
>   a new push. **Say if the toast reads well enough**; if not, this is small and rides the next client
>   batch with §89's three UI changes.

### `BL-15` — `precision` / `anti_magic` floor rungs (the AUTO-GRANT version)
**Rewritten 2026-08-27.** He answered the "which level" question and changed the delivery mechanism
with it: *"i would like them to be a learnable passive not a auto learn.. so remind me once i start
authoring warrior/rogues."* The old text asked only about the LEVEL:

> - `BL-15` 🔵 **`precision` / `anti_magic` floor rungs should follow the CLASS CHANGE, not level 76.**
>   Implied by your rogue ruling and never carried back into either checklist — recorded in the
>   changelog as "owed back to him" and then dropped. Confirm and it is a small authoring change.

The load-bearing difference: an auto-granted floor is an **engine** decision made at a level, and a
learnable passive is a **CSV row** with a learn level, an SP price and a place in a ladder. The second
cannot be built ahead of his authoring, so the entry moved from "a small authoring change" to a hold
against the warrior/rogue files.

---

## `BL-145` — the original entry, replaced 2026-09-03 (0.108.0)

Half of it was **wrong**, and it is archived rather than deleted precisely for that: the claim below
that consumable buffs "ride free of the cap" and that the War Rune bar is `BuffRow.Item` was never
true. Scroll and potion buffs have always counted, and every rune buff in the game is authored
`BuffRow.Consumable`. The real defect was the client's grouping ORDER. Second bad reading in two days
after `BL-137` — both times the fix was one command away.

> `BL-145` 🔵 **SCROLL AND POTION BUFFS MUST COUNT TOWARD THE 20 — AND SWIFT WITH THEM.** *"scroll/potion
> buffs and swift should count towards the buff limit.. now i have 2 scrolls 16npc buffs + focus
> ferocity scrolls and the 2 scrolls are in the warrune bar"*. Two separate things in one sentence and
> both are real: consumable buffs are riding free of the cap (`CountsTowardBuffLimit`), and the ones you
> are carrying are landing in the **wrong ROW** — the War Rune bar, which is the `BuffRow.Item` shelf
> for persistent gear effects, not for something you drank. A scroll belongs in the Consumable row and
> in the count.

## `BL-148` — the open questions, answered 2026-09-03 (0.108.0)

He answered both in one line (*"Zone laddre x1<40, x1.5<76, x2<83, x3 84+, elits still have their x4
everywhere"*), so the two options this entry was holding open are closed: 41-83 became **×1.5 to 75 and
×2 from 76**, a rung finer than either candidate offered, and the **elite ×4 stays**.

> `BL-148` 🟠 **THE ZONE HP LADDER IS WRONG AND INVISIBLE — YOUR REVISION, AND MY BUG.** *"the only mobs
> that should have x3 hp are zones 84+ and elits x2 mmay be .. now elits have 68k hp"*.
> Two halves:
> 1. **THE LADDER.** Today `WorldPlan.HpScaleFor` is **×1 <40, ×2 ≥40, ×3 ≥61** (0.94.0, from your
>    playtest-25 ruling *"the 15k mobs are zone placed with x2/x3 hp"*). You are now moving the ×3 up to
>    **84+**. What that leaves open, and what I will bring you numbers on rather than guess: **what
>    41-83 becomes** — all ×2, or ×1 until some level and ×2 after — and whether the **elite ×4**
>    (`MobRankScale`) drops to ×2. ⚠ The two multiply: an elite at 84 is base × zone × rank, which is
>    what produces the 68,208 you saw. Halving the rank alone still leaves 34k; ×2 rank with a ×3 zone
>    is 34k, ×2 rank with a ×2 zone is 22.7k.
> 2. **THE PLATE MUST SAY SO.** Whatever the numbers become, a creature with tripled HP has to show it
>    where you looked for it — beside the `MobMod` passives on the inspect panel. **Not a MobMod**: it
>    is a field property, so it needs its own line rather than being faked as a fake passive.
