# Backlog archive — everything `Backlog.md` no longer owes

The other half of [Backlog.md](Backlog.md), which holds **open entries only**. Anything that stops
being owed — because it was built, because you declined it, or because a rewrite replaced its text —
is cut and pasted down here, dated, under the same id. Nothing is deleted: a closed or superseded
ruling is still the reason the code looks the way it does, and this is where you look when you
wonder why.

**This file is not a done-list of the CODE.** Shipped work lives in [CHANGELOG.md](CHANGELOG.md);
closed playtests live in [testing/Playtest-Archive.md](testing/Playtest-Archive.md). What is here is
what was **asked**, and how it was **ruled**.

Three kinds of entry:

- **`BL-nn`, closed** — built or declined. The bulk of them arrived in
  **[the 2026-09-03 sweep](#the-2026-09-03-sweep--everything-closed-moved-out-of-backlogmd)** at the
  bottom of this file, in id order.
- **`BL-nn`, superseded** — a backlog entry that was rewritten before it was built. The old text
  sits here.
- **§ a superseded design** — a ruling that was already *built*, then reversed or replaced. It has
  no backlog id because it was never owed; it is here so the reversal is findable.

---

## Superseded backlog entries

### `BL-263` (superseded 2026-09-18, 0.176.0) — the entry as filed, before three of its five gaps were settled

Filed 2026-09-18 and rewritten the same day, once you ruled on gaps 1, 2 and 3. Gap 1 was **declined**
(groups keep the flat `GroupRank`); gaps 2 and 3 were **built** in 0.176.0. The remaining open text is
in [Backlog.md](Backlog.md) under the same id. Verbatim as it stood:

> ## `BL-263` 🔵 BUFFS ARE WRAPPERS OVER `(family, level)` — your model
>
> **Your design, 2026-09-18, recorded in full in [design/BuffFamilies.md](design/BuffFamilies.md).**
> Nothing is built. This entry exists so the design is somewhere you will actually walk past.
>
> > everything is a wrapper for icon/duration/name/descr/animation/cost/cooldown/casttime/etc… but it
> > provides an effect of a family, and the same effect of one family doesn't stack.
>
> `human_body` / `demon_body` / `elf_body` / `npc_body` all provide `(hp_max, N)`. Different look,
> different cast, different duration — same number line, and **conflict is `(family, level)` only, never
> duration**. A group provides several families at `max+1` so no single can take a part back. A
> different family name (`har_hp_max` vs `hp_max`) is a different line and they stack.
>
> 🔑 **The engine already speaks this language** — `BuffKey` is your family, `Rank` your level,
> `CoveredKeys` your extra families, and `cast_hp_max` (the buffer's own castable Body) is already one
> id with six levels. Five things are missing. In the order they will bite:
>
> 1. 🔴 **A covered family carries no level.** `CoveredKeys` is `string[]`, so a group wins everywhere
>    by one number. Today's substitute is a rule a human must remember (*"a group must be ≥ the best
>    single in every family it covers"*) and **nothing checks it**. That class of silent downgrade has
>    already shipped twice.
> 2. 🔴 **Equal level keeps the longer duration** — you want duration ignored. We already paid a
>    constant (`HarmonyRank = NpcBuffRank + 1`) purely to work around this.
> 3. 🟡 **A wrapper's name and icon don't survive onto the buff** — `ApplyBuff` drops `displayName`
>    when it recurses into the child, so `demon_body` would land on the bar called "Body". Same root
>    cause as the cast-bar/buff-name mismatch you noticed.
> 4. 🟡 **A rung is addressed by id, not `(family, level)`** — and ⚠ **rung ids ARE in the database**
>    (`BuffsJson`), contrary to a comment in `Skills.BuffLadders.cs` that says they are not. Moving to
>    `(family, level)` is a save-format change, i.e. a `game.db` delete.
> 5. 🟢 **Passives have no family arbitration at all** — your ×4 cast-speed fear is real:
>    `ApplyPassive` multiplies every learned passive and only a `0.4…2.5` clamp hides it.
>
> **My recommendation is NOT to build the whole model.** Steps 1-3 are small, stand alone, and buy most
> of what you described — including racial Body variants, which need no new machinery at all once
> step 2 lands. Step 4 (the full `Provides: (Family, Level)[]`) only if 1-3 turn out not to be enough.
> For passives, **a boot CHECK before a mechanism**: flag two learnable-together passives feeding one
> channel with no `Replaces` between them. That catches the mis-authoring for a fraction of the cost.
>
> 🔵 **Waiting on you:** whether to take step 1 now (it is behaviour-neutral and turns an unchecked
> authoring rule into a boot assertion), or to hold the whole thing until the 40+ CSVs are done.

⚠ **The `HarmonyRank` line in point 2 was WRONG and is corrected in the code.** The +1 is not a
workaround for the duration rule — with duration gone it is the *only* thing keeping a covering group
above the singles it covers, so it stays.

### `BL-191` (superseded) — THE SKILL MASTERIES, as the roster stood 2026-09-10 to 2026-09-12

**Filed 2026-09-10 when `BL-190` shipped the engine, and mostly CLOSED the same day** when you
authored the passives that switch it on. What is left is a short list of choices, not a build.

### ✅ What is done (0.124.0)

| Skill id | Shown as | Who | Learn | Base |
|---|---|---|---|---|
| `overpower` | Overpower | Warrior → Ravager + Warlord | 20 / 40 / 76 | 3% / 7% / 10% double damage |
| `lasting_enchantment` | Lasting Enchantment | Lightbringer + Warchanter | 76 | 10% buff/debuff duration double |
| `reuse_reset_momentum` | **Arcane Momentum** / **Stab Momentum** | Magus + the three MELEE rogues | 76 | 5% reuse reset |
| `double_mastery` (Toggle) | **Overpower Mastery** / **Momentum Mastery** | Ravager + Warlord + the three melee rogues | **81** | **×2 on EVERY mastery base you have**; 50 HP/s, +25% MP on physical skills |

Measured at 90 in mythic gear (`BalanceMatrix` §C1): Ravager **9.4%** damage, **18.8%** under Blood
Rage; Warchanter 9.7% / Lightbringer 10.9% duration; Magus 5.5% reuse. Every CSV row is verified by
`--check`, which learned the three metrics.

### ❓ What is still yours

1. 🟡 **THE TWO LEARN LEVELS ON THE MELEE ROGUE ARE MINE — the only unauthored numbers in the
   layer.** You said *"add to duals 4th the same"* twice and gave no levels, so Stab Momentum sits at
   **76** and Momentum Mastery at **81**, mirroring the Magus's reuse rung and the warrior's toggle
   exactly. Two `ClassSkill` lines and two CSV rows if you meant otherwise.

2. 🟡 **THE WHOLE LAYER IS ON TRIAL BY YOUR OWN WORDS** — *"ill try with those changes and after
   playtest ill deside if i add or remove"*. What the rig reads at 90 in mythic gear, so the playtest
   has a baseline to argue with:

   | Class | dmg | duration | reuse |
   |---|---|---|---|
   | Ravager / Warlord | 9.4% | — | — |
   | Ravager **+ toggle** | **18.8%** | — | — |
   | Warchanter / Lightbringer | — | 9.7% / 10.9% | — |
   | Magus | — | — | 5.5% |
   | Nullblade (melee rogue) | — | — | 4.6% |
   | Nullblade **+ toggle** | — | — | **9.1%** |
   | Bulwark, Sharpshooter (bow rogue) | 0% | 0% | 0% |

3. ❓ **Does anything BUY a mastery rate besides the passive?** The engine has a buff channel
   (`SkillDef.MasteryMult`) and the `double_mastery` toggle is its only author. A party "Mastery
   Chant", a consumable, a rune — all one line each. Nothing is invented until you ask.
4. 🔵 **The ladders stop where you stopped them.** Overpower has three rungs because you named three;
   the other three skills have one each. `warrior 4th.csv` and `war_aoe 4th.csv` carry the 4th-tier
   header and their two rows with a banner saying the rest is yours — neither earns a `Check.Specs`
   line until you finish the file, and `dual 4th.csv` does not either.

### ⚠ One thing to know before you retune any of it

The base is **not** the rate. `rate = clamp(base × buffs × MasteryAtkMod(EffectiveAtk), 0, 25%)`, and
the band runs ×0.70 at ATK 30 to ×1.30 at ATK 50. So a 20% base is already at the cap for anyone with
ATK 45+, and **a 30% base is at the cap for everybody** — raising it past ~19% buys nothing without
raising `StatCaps.SkillMasteryRateMax` too. `BalanceMatrix` §C1 prints the whole surface.

---

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

### `BL-155` — "Disarm — the weapon stops paying, without leaving the hand"
**DECLINED and replaced 2026-09-03**, the day after it was written, by your own reading of it: *"If we
leave the weapon bonuses it's not a disarm. Let's don't do a disarm .. But I like your silence idea"*.
You were right, and the entry below is why: the one question it hung on — does a disarmed character
also fail the skills that REQUIRE a weapon — had only two answers, and both were bad. **Yes** made it a
silence wearing a stat debuff's name; **no** (my recommendation) made it a damage debuff nobody would
call a disarm. `BL-155` now holds the SILENCE family that replaced it. Original text:

> Your spec, 2026-09-03: *"another system for fighters a disarm - 'u are disarmed' no weapon bonuses
> apply for duration con saves (later visual can look like no weapon is equiped but without actually
> unequiping it because it will be nuecense to look for it in inventory)"*.
>
> Same contest as the pull (ATK vs CON), and the "don't actually unequip it" instinct is right — the
> item never moves, a flag on the character makes `Entity.RecomputeDerived` skip the weapon's
> contributions and the client draws an empty hand.
>
> 🔑 **One question decides whether this is a damage debuff or a stun with extra steps, and it is
> yours:** does a disarm also fail the skills that REQUIRE a weapon? Every fighter skill carries
> `RequiredWeapon` / `RequiredHands`, and if a disarmed character counts as empty-handed, most of a
> fighter's kit refuses to fire for the duration. That is enormously stronger than "no weapon bonuses" —
> it is a silence. **My recommendation: NO.** Keep the gates satisfied by the item that is still
> equipped, and let disarm do exactly what it says — remove the numbers. If you want the stronger
> version it should be a different, rarer skill with its own name.
>
> **And "no weapon bonuses" needs a boundary.** The weapon feeds six things; my read of your sentence is
> that the first four go and the last two stay:
>
> - ❌ its P.Atk / M.Atk contribution and the `MAtkBonus` split
> - ❌ its attack-speed base
> - ❌ its crit contribution
> - ❌ the matching Weapon Mastery passive (it is the weapon's bonus by another name)
> - ✅ **attack RANGE** — dropping a bow user from 400 to melee is a teleport-sized effect hidden inside
>   a stat debuff. Unless you want exactly that, in which case say so.
> - ✅ the skill gates, per above

---

## Closed on 2026-09-03 — built in 0.110.0

### `BL-156` — CON and SPT shorten a debuff as well as resisting it ✅ BUILT

**Shipped whole in 0.110.0, with nothing left owed by you.** Your spec: *"if we can make con and spt to
decrease duration of coresponding debuffs -> it saves with a % and if it lands on a high stat it stays
less (investing have benifits)"*. Your numbers: *"only 20~30% decrease no more. Like a 50 con/spt is
30% decrease and 30(the base what was) x1 so 30~50 == x1~0.7"*, and *"it cuts only 1~0.7 not 1.3~0.7 so
never increases duration .. Only decrease"*. And the mob half: *"If con/spt does anything for mobs it's
not just a decorative stat ok let's shorten it as well"*.

```
factor = clamp( 1 - 0.3 * (defenderStat - 30) / 20 ,  0.70 , 1.00 )
```

CON for a physical debuff, SPT for a magical one — the same stat that lost the landing contest, read
**raw** rather than through the land chance (which would have folded in `CcResist`, the school
blessings and `DebuffLandMod`, three channels that already paid on the roll). It lives in `ApplyBuff`,
so the contested branch, the fizzle branch, a reflected debuff, a whisp and a boss all obey one rule.

Your 30 and 50 landed almost exactly on the real spread: base CON 25-47, base SPT 25-41, armour ±3,
nothing buffs either stat — so a demon fighter sits at ×0.75 on stuns and ×1.00 on holds, a demon mage
the reverse at ×0.84, and every mage is untouched by the CON half. Mobs took it too: melee CON 45 →
×0.78, tank `MobMod` 50 → ×0.70, mage SPT 58 → ×0.70. **Player CC runs 12-30% short of its authored
duration against everything** — a farming change made with eyes open, whose lever if it bites is
`MobCcSpt`, not this curve.

The formula is in [Formulas.md](Formulas.md); the reasoning is in `StatCaps` beside the three constants.

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

---

## The 2026-09-03 sweep — everything CLOSED, moved out of `Backlog.md`

Your instruction: *"backlog contains only unfinished, undecided entries … all fixed/build to go to
the archive … now it's 2k4 rows of numbers … most build/changed/declined … and are very unordered …
when u say bl-153 I scroll or search and it's somewhere between bl-20 and bl-58 … order them and
leave only active"*.

So on 2026-09-03 `Backlog.md` was cut down to its **34 open entries, sorted by id**, and the
**91 closed blocks** below came out of it — **verbatim, in id order**: entries that were built
(✅ / 🟢), entries you declined (❌), and the `.old` texts that a rewrite had already superseded.
Nothing was reworded and nothing was dropped.

⚠ **This is still not a done-list of the CODE** — [CHANGELOG.md](CHANGELOG.md) is that, and it is the
one to read when you want to know what shipped. These entries are the record of what was **asked**
and how it was **ruled**, which the changelog does not carry.

Two loose ends worth knowing about, both from entries that came down here as closed:

- **`BL-125`** was fixed in 0.103.0 but says *"worth confirming on a 74+ buffer that Arcane and Feral
  Protection now really resists"* — a **verification**, so it belongs on
  [testing/Open-Checklist.md](testing/Open-Checklist.md), not in the backlog.
- **`BL-137`** is kept in full even though it owes nothing: it is the record of a wrong ruling I made
  and corrected the same day, and the lesson on it (*check the id before building an argument on a
  number*) is the reason it is worth finding again.

---

### Closed entries, in id order

- `BL-06` ✅ **BUILT 2026-08-14 (0.65.0)** — a physical skill is no longer subject to the
  accuracy-vs-evasion roll at all; the caster's accuracy, `Precision` and `EvadeFloor` were all
  removed from that branch and now govern basic attacks only. The **only** thing that dodges a skill
  is `Entity.SkillEvadeChance`, and **Evasion Boost is its only source: 25%**. That also settles the
  CSV's *"skill evasion x1.25"* — it was the 25%, not a multiplier. See `CHANGELOG.md`. Delete at the
  next sweep.
  - 🔵 **The 40% rung is NOT built and needs you.** `rogue 2nd.csv` gives Evasion Boost a single
    level, so there is no rung to hang it on and inventing one re-specs your CSV. Same for *"76lvl the
    physical phantom gets a 90% for 15s"* — a 4th-class skill. Both wait on `BL-02`.

- `BL-07` ✅ **BUILT 2026-08-14 (0.65.0)** — `Deflection`, the warrior passive, your numbers exactly:
  **@40 → 0.15 chance ×1.0 reflected, @76 → 0.30 ×1.0**. Auto-granted at the class change like the
  identity floors, on its own 40/76 ladder. Reflected damage is the full hit and can kill the caster;
  a bounce is applied directly so it never bounces twice. Kept separate from the armor sets' basic-
  attack `Reflect`. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-08` ✅ **BUILT 2026-08-14 (0.65.0)** — `Backlash`, the tank passive: **30% chance a debuff lands
  on its caster instead**, on both debuff paths (contested CC and the fizzle model). Rolled BEFORE the
  land contest, because a bounce is not a resist. See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ **One thing is mine, not yours: the LEVEL.** You gave the 30% and never said when a tank gets
    it. It is granted at the **3rd class change (40)**, beside Deflection. If you want it at the 2nd
    (20), say so — it is one line.

- `BL-11` ✅ **BUILT 2026-08-14 (0.65.0)** — the mob layer gains an **mRes channel**
  (`MobMod.MagicResist` + a *Magic Resistance* mastery track, the CSV's own "???? Resistance" row
  filled in), and the pair is actually authored: **Warded** (P.Def ×0.8 / M.Def ×1.5 / mRes +20%) on
  Grave Lich, Aether Wisp and Spiteful Ghost; **Ironhide** (P.Def ×1.5 / M.Def ×0.8 / mRes **−20%**,
  a real magic WEAKNESS) on Shield Skeleton, Fomor Brute and Dread Knight, plus a Magic Resistance
  rung on Obsidian Knight's Stoneplate. Before this, one mob in the game was anti-magic and none was
  anti-physical. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-14` ✅ **BUILT 2026-08-14 (0.65.0)** — two of your three clauses were already true (a mob's
  attack SPEED and CRIT RATE have come off `InnateWeaponType` since 2026-08-10); the third was not.
  `MobWeaponPowerFactor` (`433 / weaponBaseSpeed`) gives a mob the per-hit power a player gets free
  from the weapon ITEM, so a slow weapon buys damage instead of being a pure nerf. Measured at 40:
  Dual 171 P.Atk / 13.2% crit, Blunt 195 / 4.4%, 2H 227 / 8.8% — and **DPS is flat across all of
  them**, which is what makes it a trade. ⚠ **BOW is ×1.00 on purpose**: `MobRole.Archer` already pays
  that trade explicitly (P.Atk ×2, 450 range), and doubling it would put an archer at ~3× per arrow.
  See `CHANGELOG.md`. Delete at the next sweep.

- `BL-20` ✅ **BUILT 2026-08-14 (0.66.0)** — a partial pick now leaves the box in your bag carrying the
  picks you didn't spend (`InventoryItem.PicksRemaining`), and it is consumed only when the last one
  goes. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-22` ✅ **BUILT 2026-08-14 (0.66.0)** — a **Break down** button on any unworn tiered piece: rarity
  → the material's rarity, grade → the amount, and no gold, because *"u give up gold to get mats"*.
  See `CHANGELOG.md`. Delete at the next sweep.
  - 🔴 **YOUR BUDGET IS NOT REACHABLE AT S, AND NO TUNING CHANGES THAT — this needs your ruling.**
    Measured (new `BalanceMatrix` `M13`): D **−10%**, C **−18%** — inside your 10-20% — but B, A and
    **S all move 0%**, so a fully S-geared character stays at **347h**. Cause: *"rarity for mats
    rarity"* means salvage can only pay the rarity of the gear that DROPS, and gear rarity is capped by
    RANK, not band — a normal mob stops at Epic and **an elite stops at Epic too**; only a BOSS drops
    Legendary/Mythic gear, at 0.09 kills/h. The A and S recipes bind on **Legendary Ingot**. At a
    uniform quantity of 20 the early rungs collapse to −24/−39/−72% while A and S *still* move 0.00%.
    Your three options, none of them invented: **(1)** accept it as a mid-game feature (what ships);
    **(2)** let elites drop Legendary gear — opens a gear faucet that competes with crafting;
    **(3)** let a high grade bump the salvaged rarity a rung — contradicts "rarity for rarity".

- `BL-27` ✅ **BUILT 2026-08-14 (0.66.0)** — `set_robe_t61_sup` / `robe_t61_sup` ("Bloodsteel Raiment"),
  the tier's SUPPORT robe, straight off your CSV row. *"Stun/Fear Resist x1.7"* folds to `CcResist 0.4`
  — the same fold already shipped on the heavy and light `611` rows. See `CHANGELOG.md`. Delete at the
  next sweep.

- `BL-28` ✅ **BUILT 2026-08-27 (0.92.0, retuned 0.92.1) — MP POTIONS, PvE ONLY.** The hold came off the
  day `--mpdrain` measured the economy the 40+ kits had created (0.91.2). Three tiers mirroring the
  healing ladder's rates — **20 / 70 / 150 MP/s for 15s on a 30s reuse**, so **10 / 35 / 75 sustained**
  — at **double** the healing ladder's price: **120 / 500 / 3,000**. 🔴 **They do not drop anywhere**,
  which is what the double price buys: *"common/uncommon healing potions are dropped so u dont spend
  there … u need to buy mp pots"*. Two sources only — the Apothecary shelf for Common and Uncommon, the
  Potion Master's craft L5 for the Rare (*"its raiding support item that is economy player trade
  only"*). **PvE only, gated on the DRINK not the effect**: a potion already running ticks out its full
  15s when you flag, and only the next bottle is refused. 🔴 **Boss fights are deliberately allowed** —
  the gate is the PvP flag and a boss is PvE; that is a ruling, not an oversight.

- `BL-29` ✅ **BUILT 2026-08-26 (0.87.1) — SP BOTTLES.** An SP Broker takes 1e9 SP + 100kk gold and
  hands back a tradable bottle, with a confirm step before you drink one. 🔑 The point that outlived the
  feature: a skill can be priced in ITEMS (`LearnConsumableId`), which is what keeps `SkillPoints` an
  `int` honest rather than forcing a widening.

- `BL-34` ✅ **BUILT 2026-08-14 (0.66.0)** — **Madness**, a party-cast Frenzy handing out a new **rung 7**
  of the family, at **76 on the Warchanter** so an admin can party-buff with it. Your deliberate
  temporary home — *"when the kits land we will move it"*. See `CHANGELOG.md`. Delete at the next sweep.
  - ⏳ **The 76+ buff EXPANSION half of this entry is still owed and was NOT re-ruled**: *"2-3 more
    Harmonies and 1-2 more improved buffs"*, plus the healer getting all the singles including a single
    Frenzy. Blocked behind `BL-02` like every other 40+ authoring.

- `BL-35` ✅ **BUILT 2026-08-14 (0.66.0)** — two level-83 skills, both carrying keeps-buffs-on-death **and**
  the auto-resurrect nothing used until now: **Rite of Preservation** (Lightbringer, on an ally, 100%
  exp back) and **Undying Will** (Bulwark, self). Ranks 2 and 3 on the existing `buff_preservation`
  key, exactly as the 2026-07-17 comment reserved them. ⚠ Your *"(not fixed)"* stands on the 1h/1h
  numbers. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-36` ✅ **BUILT 2026-08-14 (0.66.0)** — instant in a town or peace zone, a 5-minute wait outside,
  out of combat either way, and **entering a city neither cancels nor shortcuts a running timer**. See
  `CHANGELOG.md`. Delete at the next sweep.

- `BL-42` ✅ **BUILT 2026-08-14 (0.66.0)** — `SkillText.Mechanics` states every FIELD-carried payload with
  its numbers, per level, on both the skill card and the Learn preview; the conditional lines now carry
  their condition ("Block chance (with a shield)"). 🔑 The cause was structural: the `SkillEffect` enum
  has been full for years, so every mechanic since has been a plain field, and the card read only flags
  and magnitudes. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-56` ✅ **BUILT 2026-08-15** — the Equip tab is one page with three selection boxes (type /
  quality / tier) instead of a drill-down. 🔑 The cause was worth knowing: it could only ever hand out
  **Mythic**, because the authored piece IS the Mythic one and the lesser qualities are generated copies
  at suffixed ids — so five sixths of the gear ladder was unreachable from the window used to set up a
  test. Chips rather than a dropdown, per your *"whichever is easier"*. See `CHANGELOG.md`. Delete at
  the next sweep.

- `BL-59` ✅ **BUILT 2026-08-14 (0.66.0)** — your TARGET-based re-spec, all three parts. Single-target
  support of a non-party player is allowed only while they are clean; a pvp/pk player can be supported
  only from inside their party, and doing it **flags you**; party invites are unrestricted; trade is
  blocked for **pk only**, not for a purple flag; res in party works for both. The Ultimate Scroll of
  Resurrection is tradable (the tutorial's copy is the separate `_bound` clone). ⚠ This **opens**
  something that was shut — support used to be party-only. Old self-based text in
  [BacklogArchive.md](BacklogArchive.md). See `CHANGELOG.md`. Delete at the next sweep.

- `BL-65` ✅ **BUILT 2026-08-13 (0.64.0)** — Hollow Crypt 39-42 / boss 44, **Sunless Warrens** 58-64 /
  boss 65, **Ashen Sepulchre** 80-85 / boss 90. Your layout exactly. 🔑 **The cause was real and not
  cosmetic:** a mob with a NATURAL level brings its own, so the spawner's band was only a label — the
  crypt was literally spawning 58 / 32 / 65 under a "44-48" sign. Fixed by the roster, not the sign.
  See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ The Sepulchre adds a **second 80-85 elite field**, which feeds the `EliteMatDrops` faucet — the
    top of the crafting ladder is now less scarce than `docs/balance/CraftingMats.md` measured. Ties
    into the farm-times decision you deferred under `BL-05`.

- `BL-66` ✅ **BUILT 2026-08-13** — the item-id reference and the staff-only id row. Kept here for one
  release only because it is the thing that unblocked his own §75/§76 testing; delete at the next
  sweep. *"Need a grouped list (in a file - like the commands one) with each equip/item ID, and in
  each items details in game only for admin to see: a row like the enchant info one with the ID."*
  → `docs/guides/ItemIds.md` (1,078 ids, **generated** by `tools/ItemIds`, never hand-written) and an
  `id <defId>` line under the enchant line on every item card, staff only.

- `BL-68` ✅ **BUILT 2026-08-13 (0.64.0)** — nine new Stonewatch fields on a 3×3 grid east of the
  city, so every 16-40 band now exists **four times**. See `CHANGELOG.md`. Delete at the next sweep.
  - The **city was not moved**. You offered to; it turned out not to be needed, since the generator
    places a field by bearing + distance. Not moving it avoids relocating a town every player knows.
  - ⚠ **Stonewatch's gatekeeper now lists 12 fields.** A long menu on a phone — the same question
    `BL-41` asks about the craft page, in a different window.

- `BL-69` ✅ **BUILT 2026-08-13 (0.64.0)** — all three kinds. Hide is now withheld from the world
  snapshot itself (so nobody renders or can click it), broken by anything but movement, revealed at
  skill EXECUTION not at the click, and countered by `Signal Flare` (rogue/bow 28: reveal in 300 +
  30s no-hide). Stealth is a buff-carried, action-proof, unaggroed-mobs-only effect delivered two
  ways — `Prowl` (rogue toggle at 20, 1 MP/s) and `Shrouding Hymn` (cleric party version at 30:
  1 min / 30s / 300 MP). `/invis` is absolute and manual-only. See `CHANGELOG.md`. Delete at the
  next sweep.
  - ✅ **Closed 2026-08-14: hidden is hidden from EVERYONE**, party and staff included. You are still
    in the party and still listed (status `Hidden`), you are simply not renderable, clickable or
    heal-targetable — skipped by party heals/buffs, both auto-target pickers and the manual ally cast.
    Death clears a hide, so a corpse stays resurrectable. Staff keep `/tp`, `/tpme`, `/jail`, `/where`
    (they resolve by NAME, never by sight). No protocol change.

- `BL-70` ✅ **BUILT 2026-08-13 (0.64.0)** — mob clans + the rogue's `Lure`. Twelve clans authored on
  the name-root families, a 450 radius, damage-only trigger, and a no-damage mob-only taunt whose
  ladder is reach (200/400/600). See `CHANGELOG.md`. Delete at the next sweep.
  🔴 **`Lure` MOVED 2026-08-19** — it was the 2nd-class rogue's at 20/28/36 and is now the melee/DUAL
  3rd's at **40, level 1 only** (*"No lure for lvl 29 and below .. It's a skill that need the prawl
  effect"*). Levels 2-3 are unreachable until you place their rungs in `dual 3rd.csv`.
  ⚠ **Untested against a real camp** — it needs a playtest in an orc/mantis field to say whether 450
  and "the answering mobs don't cry in turn" give the fight the size you pictured.

- `BL-71` ✅ **BUILT 2026-08-13/14 (0.64.0)** — the whole aggro/taunt model. Taunt POWER is an authored
  per-level field, Provoke is a 5-rung ladder (1500 → 5100 across 20/24/28/32/36), threat decays 1%/s,
  **heals generate `power / castSeconds × 10 × people`** and **buffs `grantLevel × 20 × people`** (your
  2026-08-14 rulings — for buffs it is the LEARNED level, so one taken at 50 is worth less than one
  taken at 70; and both scale with how many the cast reached),
  and the proximity-pull defect is fixed (a pull seeds 5% of the mob's own max HP). See `CHANGELOG.md`.
  Delete at the next sweep.
  - ⚠ **Your buff:heal ratio holds at the TOP of the game and not at 35 — and you closed that on
    2026-08-27.** You sized it against "a quick heal with ~1500 power at that lvl". The **2nd-tier**
    ladder stops at power 301 (learned at 35), so a group buff out-threatens a heal there by ~8×
    instead of ~1.3×; the **4th-tier** ladder you have since authored reaches Ultimate Heal 1400-2000
    plus Healer's Power +2000, which lands the ratio where you wanted it. That was `BL-16`, and your
    ruling was that the 40+ rungs carry it rather than the 20-35 numbers moving. The 20-35 mismatch is
    therefore **intended**, not owed: a level-35 cleric is not meant to out-heal a group buff.
  - A full party is **9**, not the 7 in your example, so a level-70 group buff tops out at **12,600** —
    which is the intent (*"Full buffing a full party should take the agro from mobs for awhile"*).
  - The remaining 20-30k taunt rungs are levels 6-10 of the same ×1.36 ladder and belong to the
    3rd/4th class kits — blocked on `BL-02`, like every other 40+ number.
  - Not built and not asked for: a client-visible aggro list.

- `BL-77` ✅ **BUILT 2026-08-16 (0.69.0)** — the PvP flag is the area filter, for every AOE and every
  no-damage skill at once, and it pairs with the reflect fix from the same pass: *the flag follows
  intent*. See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ **Three shape questions were open and I answered them as the shape every other system here
    already has** — party excluded from an area cast, support not routed through the rule, and only the
    ACTOR flagged (never the person revealed). Each is marked as mine in the source and on checklist
    row `87c`. **Re-rule any of them and it is a one-line change**; nothing depends on them.
  - ⏳ **The second warrior class is AOE and still does not exist** — that is `BL-02` authoring. It
    inherits this rule with no work: the filter lives in the shared area enumeration, not in a skill.

- `BL-79` ✅ **TOWN / FIELD GUARDS — BUILT 0.94.0, both halves, nothing outstanding.** Two tiers
  (**town = level 80, S grade Epic +0**; **field = level 90, S grade Epic +16, War Rune**), eight posts
  (five city gates, three quiet farming fields), karma-keyed aggro so only a PK is acquired, PvP-on as
  the gate to attack one, **no karma / flag / exp / drop** for the kill, aggro 400 melee / 600 archer,
  respawn **75±15s** town and **1.5±0.5s** field. Your *"the npcs still refuse trade"* shipped with it:
  `NpcRefusesService` on ten handlers, so killing the watch buys a PK the SAFE ZONE and nothing else.
  🔑 **Your fork — "treat them as mobs" vs "treat them like a player" — resolved to the player route**,
  because your calibration target is a player. `MobBuild.LearnsKit` teaches the PASSIVE half of the
  class kit; the town pair carries **no invented multiplier at all** and mirrors the reference player
  (P.Atk 1,158 vs 1,214; P.Def 1,101 vs 1,101). Only the FIELD pair has a passive, `GuardTower`, for
  your *"almost 1 shot a pk"*.
  ⚠ **Widening past eight posts is one line** (`GuardedFieldIds`, and the one-post-per-city loop),
  held until you have played them.

- `BL-82` ✅ **BUILT 2026-08-23 (0.80.0) — BOTH halves, not just the badge.** Playtest 25: *"Add a flag
  for admin to see that he is in god/invis ... but now i cannot see nothing."* Kept here for one release
  because it is the item you asked after by name; delete at the next sweep.
  - **The badge** — top strip, beside the version: your rank, plus `GOD`, `INVIS` and any forced speed.
    Red background for god, indigo for invisible. Staff only, and read from the SERVER's view of you, so
    a demotion that clears god mode clears the badge too.
  - **The opacity**, exactly as you ruled it: **0.7 stealthed** (Prowl / Conceal / Shrouding Hymn) and
    **0.4 invisible** (`/invis` and the rogue's Vanish both), on **your own marker only** — plus the
    **golden ring** around a god admin. 🔑 *For themselves only* is enforced by there being nothing to
    leak: the push describes one connection's own character and no one else's, and the observer half was
    already true server-side (`BL-69` — a hidden entity is an OMISSION from the snapshot, never a flag
    the client is trusted to honour).
  - 🔑 Why it had been silent: the god badge existed in the **WPF harness** and died with it in 0.42.8.
    The server never stopped pushing the state — the Unity client simply had no handler, so it went into
    the void, and `/invis` never pushed at all. `AdminStateDto` is now `SelfStateDto`, three fields
    richer, and it is pushed **on change from the tick loop** rather than from each command that could
    cause one: hide ends by expiry, damage, acting, a flare and death, and missing one of those would
    leave a visible character faded. Protocol **24 → 25**; see `CHANGELOG.md`.

*(`BL-86`, the shutdown countdown, was closed on 2026-08-27 — *"this is good enough … noticable
enoght"*. The toast is accepted; red text is optional and owed to nobody. See
[BacklogArchive.md](BacklogArchive.md).)*

- `BL-87` ✅ **BUILT 2026-08-23 — THE BUFF CAP IS 20, AND WHAT COUNTS IS A PER-BUFF FLAG.** Playtest 27:
  *"we need make max buffs limit. Now I have 24 buffs as healer ... So if we make it 20 then the buffer
  becomes a must"*, then his rules: *"A self buff that is 20min still counts as a buff while a self 30s
  buff is temporary and is not .... For example the bow expertise is a buff that counts toward the limit
  ... the flag is not self or not, the flag is per buff .. default is true (counts towards max) - toggle
  don't and heals etc"* · *"FIFO ..1st buff buffed gets removed"* · *"If a buff is not counted u can have
  20+14"*. Delete at the next sweep.
  - 🔑 **Half of it was already built and he had been living inside it.** A `MaxBuffSlots` cap with FIFO
    eviction has existed since the buff-ladder work — at **24**. That is why he counted exactly 24: he
    was *at the cap*, and buffs had been quietly falling off the back of his bar. `24 → 20`.
  - **New `SkillDef.CountsTowardBuffLimit`, default TRUE**, authored `false` on the temporary ones. NOT
    derived from `TargetMode` (Bow Expertise is `SelfOnly` and counts) and NOT derived from duration
    (that is a consequence, not the rule). ⚠ Read off the buff that **LANDS**: a one-child wrapper
    resolves to its child, so Dash is flagged on `buff_dash_*` and a Might potion is not flagged at all —
    it IS a single of the might family, out of a bottle, and pays its slot.
  - **Authored `false`** (every one ≤90s, which is what made the line easy to draw): the six Combo Rush
    rungs · War Cry / Greater War Cry · Battle Fury · Fortify · Shrouding Hymn · the three racial Renew
    verses · Harmony of Restoration (the party HoT) · Aegis · Battle Presence / Battle Defence · Conceal ·
    Defensive Wall · Evasion Boost · Indomitable · Last Stand · Mana Barrier · Meditation · the eight
    Dash/Sprint rungs · the three healing potions. Toggles, debuffs and the gear/rune row were already
    excluded by the engine and still are.
  - **The measured result: a fully-buffed character sits at 16 / 20**, four free. Self-serving off the
    NPC buffer cost **19 of 20** for a strictly weaker set — a group packs three or four families into
    one slot and a single never can. **The cap limits the alternative to the buffer, not the buffer.**
    ⚠ **That 19 is now 11** (0.81.0): playtest 28 cut the NPC set to his eleven, so the NPC bar and a
    real buffer's groups can finally coexist. The conclusion is unchanged and better served — what the
    cap squeezes is still the substitute, it just no longer squeezes it to the point of absurdity.
  - **Verify it with `dotnet run --project tools/BalanceMatrix -- --buffs`**, which prints a `SLOT` /
    `-` column using the same rule the engine uses, resolving wrappers the way `ApplyBuff` does. It was
    the tool that caught a bulk edit wrongly exempting the three **Swift** rungs.
  - ⚠ `GameLoopService.BuffPlan` was made `public` so the census reads the real resolver rather than a
    copy of it. ⚠ No CSV column changed — `--check` is green on all ten files.

- `BL-89` 🟢 **BUILT 2026-08-26 (0.86.0) — THE CHAT LOG HAS A READER.** Playtest 28, your question:
  *"don't we need a chat log … because now an admin/mod should ban based on som1 is trying to sell u for $
  on private chat"*. The WRITE half shipped in 0.81.0 and nothing ever opened it; `/chatlog` is the way a
  moderator on a phone reads it, which was always the point.
  - ✅ **All three shapes you asked for, and they combine:** `/chatlog <name>`, `/chatlog <name> -w`
    (whispers only), `/chatlog around <time>`, plus `-p <page>` on any of them. Into the System tab,
    25 lines a page, oldest-first, staff-only.
  - ✅ `around` takes **`15m` / `2h` / `1d`** first, because that is what a report actually sounds like —
    *"about ten minutes ago"*. `11:02` = today UTC; `yyyy-MM-dd HH:mm` names the day. Everything is UTC,
    since that is what the rows hold.
  - 🔑 **It flushes before it reads.** Lines wait for the 60-second autosave, so a straight query would
    have been blind to the last minute — and the case this exists for is a LIVE report. Typing `/chatlog`
    the moment you are told and seeing nothing would read as innocence.
  - ✅ **RETENTION RULED 2026-08-26: 90 DAYS.** *"90 days retention no point in keeping more .. if some1
    gets reported .. must take no more than a week to deem him banable or not"*. Live in 0.86.1 —
    `ChatLogRetentionDays` = 90 and the six-hourly sweep is deleting. 🔑 The window is sized to how long
    a CASE stays open (a week), not to how long the evidence stays interesting; **don't raise it "to be
    safe"** — that is the instinct the ruling rejected.
  - 🔴 **THE PRIVACY QUESTION, ANSWERED PROVISIONALLY — reverse it in one line if you disagree.**
    **Moderator and above read whispers** (they hold the jail and the kick, and the private channel is the
    case the feature is for). **A Chat Moderator reads PUBLIC channels only** — that rank goes to someone
    you do *not* fully trust (playtest 26), and a mute needs no private mail to justify it.

- `BL-90` 🟢 **BUILT 2026-08-24 (0.83.0) — A PER-SKILL DEBUFF SUCCESS MULTIPLIER, AND THE ROUTING BUG IT
  EXPOSED.** Your final shape: *"DebuffLandMod should be floating one value - default 1 … armor/weapon
  break + gravity + Arcane/Fros/Pyro blasts(nuker 3rd) should be 75% at parity (x1.5) and the other should
  be 25% at parity (x0.5)"*, with the values themselves authored into the CSVs as `(success chance xN)`.
  - ✅ `SkillDef.DebuffLandMod` + `SkillLevel.DebuffLandMod` (0 = inherit), read as `DebuffLandModAt(lvl)`
    — one plain float, default 1, per skill AND per rung. **No tier constants**: a first pass had four and
    you replaced them with the CSV column, which is the right home.
  - ✅ 🔑 **THE ROUTING FIX YOUR ARITHMETIC FORCED.** ×1.5 only reaches 75% off a **50%** base, and 50% is
    the CONTESTED curve — the fizzle path is ~99%. Armor Break, Weapon Break, Gravity and Mana Strain all
    declared `DebuffSchool.Magical`, all said *"Contested ATK vs SPT"* on their cards, and all took the
    **fizzle** roll anyway, because the branch tested the effect-FLAG mask and never read the school.
    `IsContestedDebuff` now reads both. Those four moved from ~99% to 75/50/75/25% at parity.
  - ✅ `SkillCsvSeed --check` **reads the `(success chance xN)` column** (119 authored rows). It needed a
    carve-out: the DESCR reader strips parentheticals as commentary, which was eating every one of them.
  - ✅ `BalanceMatrix` prints **=== DEBUFF SUCCESS ===** — your scale, every tagged skill, and the four
    that the routing fix moved.
  - **Applied from your CSVs:** Armor Break ×1.5 · Weapon Break ×1.5 · Gravity ×1 · Bind ×0.7 · Mana
    Strain ×0.5. **From your general rule, MAGICAL only:** Entangling Roots, Warding Step ×0.5.
  - 🔑 **PHYSICAL DEBUFFS STAY ×1** — your ruling: *"physical debuffs should be x1 for now .. Con saves ..
    we deside later"*. The physical school already contests **CON**, a stat a fighter really carries, so
    taxing it too would double-charge. Reverted on Shield Stun, Shield Bash, Stay!, Terrifying Roar.
    ⏳ Explicitly a HOLD, not an answer — re-raise when physical CC is actually measured.
  - ⚠ **YOUR MESSAGE AND YOUR CSV DISAGREE ON GRAVITY** — the message puts it in the ×1.5 group, all
    fourteen rows in `healer 3rd.csv` say `(success chance x1)`. The CSV won, per the standing rule. Say
    the word and it is one edit in both places.
  - ✅ **CLOSED 2026-08-27 — THE BURSTS AND THE WHOLE NUKER LIST SHIPPED IN 0.87.0.** You asked *"what
    about the 3 bursts ? they should be build as the chance for debuf per skill"* — they were, three days
    earlier, and **this entry was simply stale**. Verified in `Skills.Nuker3rd.cs`: Arcane / Frost / Pyro
    Burst all carry `DebuffLandMod: 1.5f`, and beside them Frost Spikes ×0.7, Frost Pierce ×0.5, Witches
    Curse ×0.7, Witches Scarecrow ×0.5, Arcane Void ×0.3 — every value your CSV authored. `nuker 3rd.csv`
    took its `Check.Specs` line the same day and `--check` is green on it.
    ⚠ 🔑 **THE LESSON IS ABOUT THIS FILE, NOT THE FEATURE.** Two entries (`BL-90` and `BL-91`) sat here
    claiming "not built" for three days after the code landed, because the kit that unblocked them was
    built under a different heading. **When a build closes a dependency, sweep the entries that named
    it** — the pass-end checklist rule already says to check the Backlog for stale marks, and this is
    exactly the failure it is meant to catch.
  - ⏸ **HELD BY YOU — Snare Trap and the Warchanter's stun-rider stay at ×1 until they are TESTED.**
    2026-08-27: *"the trap and stun leave them as still open until tested."* Both are hybrids that
    already exist on shipped classes; retro-taxing them is one line each and it waits for play, not for
    a decision. Not dropped.
  - ✅ **CLOSED 2026-08-27 — "buff removeal" needed nothing.** *"buff removal can be deleted .. arcane
    void is the one we need and later a demon tank probably will have a cancel too but thats still
    unauthored."* Arcane Void ×0.3 is built and is the cancel. An ork tank's cancel arrives with his
    authoring like any other unwritten row; it is not owed here.
  - ℹ️ Your *"(did the same for buffer)"* was a slip — *"did the same for healer ... my bad"*. Confirmed:
    `healer 3rd` and `nuker 3rd` carry the column, `buffer 3rd` has none and owes none.

- `BL-91` 🟢 **INTERRUPT IS IG'S OWN FORMULA (0.84.0) — BUILT AND FULLY RULED.**
  - ✅ **BUILT, IG's shape, with your two departures.** `FinalChance = (DmgTaken/MaxHP) × rand(1.00-1.20)
    × SPT-mod × (1 − resist buffs) × skill.InterruptMult`, + `InterruptPower` in percentage points.
    Your worked example reproduces exactly: 1000 on a 2000 pool, ×1, Resolve 54% → **23%**.
  - ✅ **Resolve is a PERCENT.** The ladder numbers did not move (18/25/36/40/42/48/54) and the CSV rows
    still read them; they are percentages of the incoming roll now. This answers the old QUESTION 2 —
    a flat buff on a growing pool always decays, a percent never does. ×0.46 at 20 and at 80 alike.
  - ✅ **The MEN curve, flattened to your numbers.** IG's 20 = ×1.00 / 50 = ×0.23 is ~4.8% per point and
    prices our level-39 human mage at ×0.395 — your *"a bit low"*. Ours is your alternative,
    **20 = ×1.00, 50 = ×0.67**, same geometric shape at a third the slope. Ours vs IG's on our bases:
    human ftr ×0.94/×0.78, elf mage ×0.85/×0.56, human mage ×0.78/×0.39, demon mage ×0.72/×0.29.
  - ✅ **No robe-set 50% resist** — *"and i dont want that"*. `StatCaps.InterruptResistMax` = 0.80 so any
    future source stacks into a clamp instead of multiplying past it.
  - ✅ **THUNDERSTORM IS FIXED BY THE MODEL, not by a patch.** 0.83.0 priced a cast against its own DPS, so
    a 300s reuse made the biggest nuke in the game the easiest cast to break. Reuse is not an input any
    more. A long cast is simply a cast that eats more hits.
  - ✅ Old QUESTION 1 is answered too: the *"58-94% per cast"* number came from DPS parity, which no longer
    exists. Measured now, a same-level fighter breaking a human mage's cast: **27% / 17% / 11% / 11%** per
    basic hit at 20/40/60/80, **12% / 8% / 5% / 5%** under Resolve.
  - ✅ **RULED: THE TWO NUKER INTERRUPT SKILLS ARE ×2** — *"add the nuker the two high interrupt skills a
    x2 chance. They are fast cast and x2 interrupt chance is good enough"* (2026-08-26). They are exactly
    the two rows in `nuker 3rd.csv` that say *"Higher chance to interrupt enemy casts"*: **Frost Spikes**
    and **Frost Pierce**, both `m.Atk +64` on a **2.5s cast / 1s reuse** at the top rung. All 28 rows now
    also carry **`(interrupt chance x2)`**, and `Descr.cs` reads that token, so it will be verified the
    day the kit is built. At 74, elf nuker vs a same-level human mage:

    ⚠ **RE-MEASURED 2026-08-27 — every number below moved, and not because of this skill.** `BL-78`.3
    rebuilt the PLAYER HP curve in 0.91.0, so the mage being cast at now has **3,239 HP** where this
    table's victim had ~1,280. A nuke is therefore a much smaller slice of him and every interrupt
    chance fell with it. The table is what `--goldflow`'s sibling `BalanceMatrix` prints today:

    | spell | dmg | % of HP | ×1 | **×2 (built)** | ×5 | ×10 | ×2 +Resolve |
    |---|---|---|---|---|---|---|---|
    | Frost Spikes | 160 | 4.9% | 4.3% | **8.5%** | 21.4% | 42.7% | 3.9% |
    | Frost Pierce | 160 | 4.9% | 4.3% | **8.5%** | 21.4% | 42.7% | 3.9% |
    | Elemental Blast | 270 | 8.3% | 7.2% | — | 36.1% | 72.1% | 3.3% |
    | Thunderstorm | 541 | 16.7% | 14.4% | — | 72.2% | 100% | 6.6% |

    Your ×10 guess came from expecting these to be small hits. Against the OLD pool they were not, which
    is why ×2 was the right call then. **Against the new pool they genuinely are small** — ×10 on a Frost
    skill is 42.7%, not the guaranteed cancel it used to be. At ×2 they read 8.5% per hit (3.9% through
    Resolve) and fire every ~2.5s, so a 4s cast now eats roughly 13% rather than a third.
    ❓ **Worth re-ruling when you next play a nuker**: ×2 was sized against a mage with a quarter of the
    HP he has today. Not changed unasked — the ruling stands until you move it.
  - ✅ **×2 IS IN THE CODE — YOUR WORDING FIX, 2026-08-27.** *"the x2 - nuker 3rd is build or atelast
    should be so fix the wording."* You were right and this bullet was wrong: the kit shipped in 0.87.0,
    **`InterruptMult: 2f` is on both Frost Spikes and Frost Pierce** in `Skills.Nuker3rd.cs`, and
    `nuker 3rd.csv` has had its `Check.Specs` line since the day the kit landed.
  - ✅ **AND THE ONE THING THAT REALLY WAS OWED IS NOW DONE.** `BalanceMatrix`'s interrupt table carried
    a hand-copied four-row literal (his CSV's top rungs, typed in while the kit was unbuilt). It now
    **reads the `SkillDef`s** — power, cast, reuse and the multiplier — so a retuned rung moves the table
    with it. 🔑 That literal is precisely why this entry was able to go stale while reading as current:
    a measurement that repeats an authored number instead of reading it will agree with itself forever.
  - **`BL-91` is CLOSED.** Nothing in it is outstanding; delete at the next sweep.
  - ⚠ `SkillDef.InterruptDefense` survives as a float FRACTION — the lever for *"this particular spell is
    hard to break"* without touching the caster's sheet. Unauthored.

    divide-by-zero. Unmeasured. Everything else in the model is your specification.

- `BL-92` ✅ **BUILT 2026-08-26 — BOTH HALVES. MP in 0.88.0/0.88.1, HP in 0.88.2.** You opened it,
  it was measured (`BalanceMatrix --mpregen`, then `--hpregen`), and you ruled every question the same
  day. A buffed level-74 mage was regenerating **288% of his own spell-spam cost**; he now sits at ~88%.
  The mastery `mpReg` ladder is FLAT and outside, SPT has its own curve, and **standing still is a
  stance**. Calm Spirit shipped with it. See 0.88.0-0.88.2 in `CHANGELOG.md`. Delete at the next sweep.
  - ✅ **THE HP HALF, ruled after you supplied IG's own numbers** (base 1.5-3.0 by race+class, ConMod
    CON 30→1.00 / 43→1.32, LvlMod `L/100 + 0.89`): *"I want to make the passives + not x as the mp ..
    and buffs to carry the multiplier .. and the flat is to added at the end"*. Every `hpReg` rung is
    now a flat +1.1…+2.7 HP/s and the flats sit outside. It ended the inversion that measurement found
    — a level-74 nuker on **27.5 HP/s against a tank's 16.4**, the class IG gives the lowest base regen
    holding the game's highest. Now warrior 18.0 > rogue 17.6 > tank 16.4 > nuker 12.9.
  - 🔵 **THE LEVEL TERM IS DEFERRED TO A PLAYTEST, NOT SETTLED.** *"Leave out lvl mod just leave the
    flat outside … So we will have x2 more than IG but not as much as we have now … Playtest will
    decide if it stays"*. We measure at **1.6-2.0× IG** at every level and the whole gap is ours
    (`1 + L/30`, ×3.71 across 1-85) against IG's (`L/100 + 0.89`, ×1.93) — which is character-for-
    character the `(level+89)/100` the damage formula already uses. `--hpregen` prints the swapped
    column ("if IG lvlMod") so the playtest has the number ready. **Do not take it without a ruling.**
  - 🔴 **THE FIGHTER FLATS ARE BACKWARDS AND YOU FLAGGED IT:** *"now fighters are not yet authored and
    have higher regen flat bonuses than mage"* — when the fighter 3rd/4th CSVs land, a fighter's `hpReg`
    flat must exceed a mage's. Today the nuker carries **+2.7**, the warrior **+1.6** (frozen at level
    32), the rogue **+1.2**, and the **tank none at all**; archer and dual have no `hpReg` row either.
  - ✅ **THE ORK BUFFER — CLOSED 2026-08-27, and it was already built.** *"the ork buffer have the hp
    boost skill so if its not build build it."* It does: `ClassSkillTables.Third.RegisterHpBoost`
    registers **HP Boost L1-L7 at 40/44/48/52/56/62/70 for the Warchanter of all three races**, Demon
    included, on your own 3rd-class SP ladder (36k → 390k, overridden per rung because the SkillDef's
    prices are the warrior's). So the demon buffer's extra HP comes from the KIT, which is the rule —
    identity is the skill kit, not a stat bonus — and no `hpReg` number was invented.
    ⚠ Note the distinction that made this look open: your original *"buffer ork should have more"* was
    about the **hpReg FLAT**, a regen number; HP Boost is a **max-HP** skill. It answers the intent, not
    the same field. If you did mean a bigger demon regen flat specifically, that is still unauthored.
  - ⚠ **The ladder stopped being progression**, knowingly: a nuker's six rungs from +1.1 to +2.7 used to
    buy +19 HP/s and now buy **+1.6 across 34 levels**. Same trade the `mpReg` ladder took. If those
    rungs should be felt, the FLAT numbers get re-authored bigger in the CSVs — not an engine change.
  - ✅ **EVERY PRIMARY STAT IS READ EFFECTIVE** (same day, second ruling): *"Need effective con to count
    on hp max/regen and whatever con have mod on … con armor set now will buy u nothing and atk-con
    won't hinder you"*. CON and ATK were the last two read BASE — by HP regen and by the character
    sheet / target panel, which sent `EffectiveWit/Agi/Spt` beside them. An armour set's `Con: -2,
    Str: +3` moved pool, regen and damage with **nothing on screen**. Fixed at all five sites; mob
    paths keep raw CON.
  - 🔴 **TWO ARMOUR ROWS FLAGGED, NOT CHANGED.** Your ruling was *"except armor masteries the 20%
    increase"*, so armour masteries kept their percents — but `rogue 2nd.csv` @36 carries `mpReg x1.8`
    and `tank 2nd.csv` @36 carries `mpReg x3.4`, which are weapon-mastery-sized numbers on armour rows.
    A tank at 36 therefore regenerates ×3.4. One-line change either way, waiting on you.
    ⚠ The HP pass makes this visible inside ONE row: `rogue 2nd.csv` @36 now reads `hpReg +1.2` (a flat,
    with every other hpReg) beside `mpReg x1.8` (still a percent, by the armour carve-out). Deliberate,
    and it stays odd-looking until you rule on the two mpReg numbers above.
  - 🔴 **`BalanceMatrix.BuildPlayer` NEVER SETS A 3rd CLASS**, found while building the report: every
    other mage table in the tool measures the level-35 kit at any level (no 40+ spells, no 40+ mastery
    ladder). `--mpregen` uses its own `BuildNuker`; the rest were deliberately left alone rather than
    silently moving every number the tool prints. Its own pass.
  - ⚠ Meditation's flat +25-40/s no longer gains from sitting — it was inside the stance multiplier on
    purpose ("sitting to meditate should pay") and the global flat rule outranks it. Knowingly traded.

- `BL-95` ✅ **BUFF PRESETS — BUILT 0.99.0, and the NPC set grew to SIXTEEN.** Your 2026-08-28 list
  added Serenity, Soul, Aim and Agility to the twelve, *"players to not be so overwhelmed by mobs
  (serenity, soul — longer mage sessions; agility+aim — fighter less misses, dagger less hits
  taken)"*, and named the two role presets by hand.
  - **Four buttons at the NPC now**: **Full** (16), **Mage** (10 — Bulwark, Force, Alacrity, Swift,
    Ward, Body, Soul, Serenity, Resolve, Frenzy), **Fighter** (10 — Bulwark, Might, Fury, Swift, Ward,
    Body, Vigor, Vamp, Frenzy, Aim), and **Custom** once you save one. Each row shows the COUNT as
    well as the price, because the buff bar caps at 20 and the full set is now 16.
  - **Save reads what you are WEARING**, your workflow exactly — buff fully, cancel what your class
    doesn't want, press Save. Both worked examples fell out of one filter with no special case: a
    buff records the def that *created* it, so a potion resolves to its family rung (never
    `npc_might`) and a group like Feral Bloodlust is ONE buff under the group's own id — so neither
    can leak into a preset.
  - **[Save] alone until you have one; then [Custom] [Save] [Delete]**, with Save asking before it
    overwrites and Delete asking before it deletes. Saving with no blessings on you is refused rather
    than stored, so a [Custom] button that casts nothing can never appear.
  - **PER SUBCLASS** — the question you flagged, answered the way the auto-marks bug says to answer
    it. `Subclass.BuffPreset`, new `SubclassRecord.BuffPresetJson`. ⚠ **game.db delete required.**
  - Also fixed on the way past: the NPC's accuracy single was displayed as **"Accuracy"** while the
    ladder, all three potions and all three scrolls call the family **"Aim"** — one blessing wearing
    the stat's name. It is `Aim` now; the id is unchanged.
  - 🔵 **The one number worth watching in play**: 16 against the cap of 20 leaves four free slots, not
    the eight the trim to twelve bought. The presets are the answer (ten leaves ten), but if the full
    set plus a party buffer feels tight, the fix is to take Mage/Fighter rather than to trim the set
    again.

- `BL-96` ✅ **THE `AOE` COLUMN — BUILT 0.94.2 on your go-ahead.** `LEARN, NAME, TYPE, RANGE, AOE,
  TARGET, …` across all 24 files, 1,425 rows, by a new `SkillCsvSeed --aoe-column`. `--check` now
  verifies the radius against `SkillDef.AreaRadiusAt`, so it is a **CHECKED number for the first
  time** instead of prose in the DESCR cell. Elemental Wave reads **`0,200,enemy/aoe`** — your worked
  example — and Arcane Wave **`900,400,enemy/aoe`**. It also settles the contradiction between your
  2026-08-27 ruling (the TARGET column does NOT encode where the circle sits) and your 2026-08-28
  description of Elemental Wave as `self/aoe`: with two columns, neither has to carry the other's
  meaning. `README.md` in the CSV folder documents the three columns.
  ✅ **AND YOU ANSWERED THE ONE OPEN QUESTION** (2026-08-28): the party heals read `600,600` because
  the range gate applied to the targeted ally, and you ruled the GATE should go — *"should be cast
  able without a target .. So 0/x"*. Range is 0 on all of them now (0.94.3), which also means they
  fire with an ENEMY selected. Nothing left open here.

- `BL-97` ✅ **BUILT 2026-08-28 (0.96.0) — THE TEMPEST AND THE VANGUARD ARE RETIRED. Eight choosable
  paths per race, 24 third classes.** You ruled: *"Tempests must go .. And elf nuker 3rd is starweaver, ork is
  cinderwitch and human stays magus"*, then *"Remove the vacant tank as well — the 3 tanks must have
  their name and the other is the same for the 3 races ... So is the one that must go"*.
  🔑 **Your test for which of a pair dies is the keeper**: both retired disciplines wore ONE name
  across all three races (Vanguard/Doomward, Tempest/Skybreaker), because neither was ever really
  three classes. The Vanguard also taught nothing — the 2026-08-10 purge took its learn lines — so a
  level-40 Knight could pick an empty class.
  ⚠ A retired discipline's NAME is free to reuse on a live one (a name is not an id) — which is what
  your `champion -> sword master` / `vanguard -> war master` rename does with it.
  🔑 **The three names were ALREADY exactly that** — `ClassNames` has read Magus / Starweaver /
  Cinderwitch since the per-race naming pass of 2026-08-17 — so the ruling's naming half cost nothing
  and the whole pass was the retirement.
  🔑 **And it deleted NO authored row.** My earlier note here warned it would; that was wrong.
  `nuker 3rd.csv` carries no discipline column, so the 208-row kit was registered to Magus **and**
  Tempest, identically — the retirement removed a duplicate registration, not content. `--check` is
  still clean.
  What actually changed: `Disciplines.Of` returns a NULLABLE second branch and the nuker's is null;
  third-class ids **112 / 124 / 136** and their ascensions **212 / 224 / 236** are now permanently
  vacant — as are the tank's **102 / 114 / 126** and **202 / 214 / 226** (the id is computed from the
  parent, so nothing else moved); the twelve class-proof quest items for those ids are gone with them
  (1080 → 1068 items); a character saved on a retired discipline is migrated to its surviving sibling
  on load rather than being left classless; and `ClassNames.DuplicateNames()` has no exemptions left.
  ⚠ One consequence worth knowing: **"two nukers" is no longer a legal subclass pair.** The one-per-
  discipline rule stands, so a second Sorcerer/Inquisitor/Witch would have to walk the Magus twice.
  Delete at the next sweep.

- `BL-100` ✅ **BUILT 2026-08-28 (0.97.0) — EVERY CLASS RENAMED TO YOUR TABLE.** *"now just sound
  over complicated ... but I want it simpler ... All races are the same until lvl 40 so we can call it
  elf-A human-A"*. The 2nd class is race+role now, and the six best words we had (`Assassin`,
  `Sentinel`, `Templar`, `Shadowblade`, `Stalker`, `Champion`) moved onto 3rd classes that earn them.
  ➡️ **[docs/design/ClassRenames.md](design/ClassRenames.md)** is the live roster and the record.
  🔑 Cost was zero: nothing persists a name, so no save broke and no `game.db` reset was needed.
  ⚠ Four built lines differ from your written list, each explained in that file — the biggest being
  **elf `Swiftblade → Sword Saint`**, because your `Sword Master` at the elf 3rd collides with the
  human 4th and `DuplicateNames()` has had no exemptions since `BL-97`: a hard startup failure, not
  just the smell you spotted.
  ✅ **AND THE LAST FLAG IS CLOSED TOO** (2026-08-28, 0.98.1): `Sword Dancer` — the one 3rd chosen to
  rhyme with its 4th rather than be its lesser form — is **`Skirmisher → War Storm`**, which puts the
  war_aoe 3rd tier in one voice: Vanguard / Skirmisher / Warborn. 🔑 Your *"anything aoe is War
  named"* rule holds across all six AoE/support 4th classes and is written into `ClassNames`.
  ✅ **ALL THREE OPEN ITEMS CLOSED THE SAME DAY, by you:**
  1. **`Sentinel` clashed with the MODERATOR's worn title.** You kept the class and rewrote the whole
     ladder: *"supreme being(owner) -> god(admin) -> demi god(mod) -> warden(chat mod) -> player"*.
     Two plates changed (Sentinel → **Demi God**, Silencer → **Warden**) and the four now read as one
     descending order instead of four unrelated words.
  2. **`Light Bringer` → `Holy Priest`**, which also ends the confusion with the demon healer, and
     gives the human line a ladder you can hear: Human Priest → Holy Priest → Holy Messenger.
  3. **The bow order** is `Soultracker → Soulhunter` on the demon, your own demon row's order.

- `BL-101` ✅ **BUILT 2026-08-28 (0.98.0) — THE THIRD RACE IS `DEMON`.** Your idea, and it earned
  itself twice over: **`Orc Archer` is already a level-12 MOB**, so the player race was sharing its
  name with common trash — and it killed the last naming exception, because the support line only had
  to hide behind `Shaman` while "Ork Priest" sounded like nothing. All fifteen 2nd classes are
  race+role now with no special case.
  🔑 **`Race.Demon` is still value 2** — a character persists the number, so every save is the same
  race under a new name. No `game.db` reset.
  Swept: the enum and all 17 code files (compiler-verified), 162 RACE-column cells across five CSVs,
  and the prose in the live docs. ⚠ **Owner quotes were left VERBATIM** — 35 lines carrying a `*"…"*`
  quote still say "ork", deliberately, and so do CHANGELOG / Roadmap / Playtest-Archive, which are
  historical records.
  ⚠ Of the three IG names in your demon column: **`Warlock` stays** (yours is a buffer, theirs a
  summoner — different role), **`Hell Knight` → `Dread Knight`** by your own swap, and `Dreadnought`
  was only ever an alternative. `Juggernaut` went back to being unused — *"sounds orkish"*.

- `BL-105` ✅ **BUILT 2026-08-29 (0.101.1) — THE `WEAPON` COLUMN, in your grammar.** You approved it the
  day it was proposed and wrote the spec yourself:
  `weaponType1[|weaponType2|weaponType3][/hands]`, with `duals/1` a typo-warning and anything but
  `/1`/`/2` an error that invalidates the hands. All of it is live: the column is in all 24 files
  (1,425 rows, 187 with a real requirement), `--check` verifies every cell, and the grammar is
  documented in `classes_skills_csv/README.md`.

  🔴 **It corrected a semantic shipped hours earlier.** Your `sword|blunt|bow/1` includes a BOW — so
  hands narrow the **TYPES**, not the equipped weapon, and 0.101.0 had it the other way round. Fixed in
  `WeaponTypes.Resolve`; it made the code simpler, since the playtest-28 fold now falls out of the
  expansion instead of being a special case.

  Your authoring rule is recorded with it: *"passives won't ever be a (bow/duals or one handed weapon)
  … but if authored they should work that way"* — nothing is special-cased or refused.

  ⤷ The original proposal, for the record:

- `BL-105`.old ❓ **A `WEAPON` COLUMN FOR THE SKILL CSVs — my proposal, your call.** A skill's weapon
  requirement is real, enforced code, and today it is written **only in the free-text DESCR** (*"with
  2h sword/blunt"*, *"Require: Bow/Blunt"*, *"Blunt:"*). That means `--check` cannot verify it, and it
  is exactly how the elf's Combo Mastery bug survived: his CSV row said Bow/Blunt, the code said Blunt
  alone, and nothing in the repo could notice the two disagreed.

  **The proposal:** one more structural column, `WEAPON`, holding `type/hands` — `blunt/2h`,
  `sword|blunt/1h`, `bow`, `dual`, blank for none. The same move you already approved twice, for `AOE`
  (`BL-96`) and for `TARGET` (`[scope]/[breadth]`), and the checker gains a real comparison instead of
  a description it has to parse. ⚠ It touches the header of every file, so it is not something to do
  quietly on the way past — say yes and it is one increment.

- `BL-107` ✅ **BUILT 2026-08-29 (0.102.0-0.102.1) — THE `WEIGHT` COLUMN, in the grammar you approved.** You
  said yes to all three questions: `heavy/shield` for AND, robe and naked OFF for the warrior (and the
  rogue), and DESCR keys kept as they are. Live: the column is in all 24 files (1,420 rows, 103 with a
  real requirement), `--check` verifies every cell, and both the active gate (`SkillDef.RequiredArmor`)
  and the passive one (`PassiveEffect.RequiredArmor`) are enforced — cast-time, auto-hunt and
  `RecomputeDerived` all read the one helper, `ArmorGate`.

  🔑 **A rung CAN carry more than one passive layer** (`SkillLevel.ExtraPassives`), each with its own
  gate — built for Shield Mastery, whose block rate needs a shield while its "+10% P.Def" needs a
  shield **and** heavy. ⚠ **Your 0.102.1 ruling collapsed that into one gate**, so the mechanism is
  live but has no author today; it is the tool if a rung ever needs two gates again.
  `DefencePctWithShield`, the bespoke field invented in 2026-08-21 because no general gate existed, is
  **deleted**.

  🔴 **Three things changed for a character, all of them yours:** a warrior or rogue in a ROBE or naked
  now gets nothing from their Armor Mastery (was: the "with all" half); Shield Mastery's bow resistance
  is now really shield-gated — its card always said *"Every part of it needs a shield"* and rungs 3-4
  were paying it to a tank holding a greatsword; and **Shield Mastery is `heavy/shield` on all four
  rungs** (0.102.1), so the Human Warchanter now CHOOSES — heavy+shield semi-tank, or robe+shield and
  the same shield story as the elf and demon buffers. *"Giving more pDef and shield rate+Def on a robe
  pushes one class in front a lot."*

  📋 **`DESCR-KEYS.md`** (generated, `--descr-keys`) is the key list you asked for: 46 keys, 141
  spellings, plus the scope labels. There is no `AllDef` — P.Def and M.Def are separate everywhere.

  ⤷ The original proposal, for the record:

- `BL-107`.old ❓ **A `WEIGHT` COLUMN — the armour twin of `BL-105`. Your idea, my counter-proposal on
  the spelling, and one thing you asked for that is already built.** Written up in full:
  **[`design/ArmorWeightGate.md`](design/ArmorWeightGate.md)**. Your words, 2026-08-29: *"Add a column a
  required weight … That way I can make the tank_shield_mastery L4 to work only on heavy|shield and not
  give the % defence on any armor except the heavy."*

  **The gap is real and narrower than it looks.** A weight gate exists today only inside
  `ArmorMasteryProfile` (its `Robe`/`Light`/`Heavy` slots), so ONLY an armour mastery can be
  weight-gated. Any other passive, and every active, cannot say "heavy only" at all. The shield half is
  already half-built — `PassiveEffect.RequiresShield` — and `DefencePctWithShield` exists as a bespoke
  field precisely BECAUSE there was no general gate; your note then was *"IG is shield+heavy but I'm not
  sure if we can"*. This is that.

  ⚠ **Where I disagree: `heavy|shield` should be `heavy/shield`.** A shield is not an armour weight, it
  is a different slot, so under the OR that `|` means everywhere else the cell reads *"heavy armour, or a
  shield with any armour"* — which pays a robe-wearer with a buckler the +10% P.Def you just said must
  never leave heavy. **You need AND, and `|` cannot say it.** It is yesterday's lesson exactly: a bare
  weapon type meant *any hands*, and "one-handed" was unsayable until hands became their own axis after
  the `/`. Same shape, same fix — `weight[|weight…][/shield]`, one grammar for both columns.

  ⚠ **`["light: x","heavy: y"]` — the split yes, the brackets no.** Your files already carry per-weight
  clauses in three spellings; a fixed key vocabulary (`robe:` `light:` `heavy:` `bare:` `shield:` `any:`,
  where `any:` = every state the column lists) normalises what you write instead of rewriting it, and
  quotes-inside-a-quoted-CSV-cell is the exact corruption vector that reverted two shipped commits once.

  ✅ **Your third idea is already in the game — do not add a Description column.** `SkillText.cs`
  generates the numbers from the data at the level you are looking at; the skill window already prints
  `Heavy: P.Def +40, Max MP +30` per rung. A `DESCRIPTION` column would restate that and go stale. The
  one thing genuinely missing is the *gate* line (`Requires: heavy armour + shield`) — a few lines once
  the column exists, not a column.

  **Three questions to answer and it is one increment** (§5 of the doc): (1) `/shield` instead of
  `|shield`? (2) does `light|heavy` really turn ROBE off for the warrior and rogue — it is what you
  wrote, and it is a small nerf reversing a deliberate 2026-07-01 fix, so say it once; (3) DESCR keys
  rather than a JSON array?

- `BL-108` ✅ **BUILT 2026-09-02 (0.103.0) — ALL FOUR FILES.** The Warchanter is the second finished
  4th class in the game. `--check` clean on `buffer 3rd` / `buffer 4th` / `healer 4th` / `shared 4th`
  (and `buffer 4th` now has its own `Check.Specs` line), zero ladder dips, `--learn-audit` clean.
  Full detail in the CHANGELOG. 🔑 **YOU NEED A NEW APK** — the client builds its Learn tab locally.

- `BL-109` ✅ **BUILT 2026-09-02 (0.104.0) — THE WHISPS, all nine skills and the six-summon PoC.**
  Every rule below is implemented as written; what follows is kept as the record of the design.

  **What the build added on top of your spec, and why:**
  - **`WhispCcAtk = 40` is the one invented number.** Your rule is that a whisp is uninfluenced by
    master gear and its own P/M.Atk is 1, so its debuffs need an attack of their own — and
    `whisps_skills.csv` has no attack column to read one from. 40 is a plain melee creature's
    (`StatCalculator.MobCcAtk`), contested at the MASTER'S level, so a whisp lands about as often as
    a level-matched monster's control before the skill's own modifier. **This is the figure to move
    if whisps land too often or too rarely.**
  - **A whisp never picks its own target** — it helps with the fight its master is already in.
    Pulling is a decision that belongs to the player, and a spirit that chose its own fights would
    be making it for him.
  - **The client draws them as coloured orbs**, one colour per summon, chasing the server's position.
    A deliberate placeholder: the position is honest and two whisps are told apart at a glance. The
    art is `BL-93`/`BL-103` work, not this.

  ⚠ **`whisp_gravity` and `whisp_clear` are BUILT BUT UNSUMMONABLE.** They are rows in your file, so
  they exist; your `tank 3rd.csv` PoC calls six whisps and neither is one of them. The day a class
  table names them they work. Nothing was invented to give them a home.

  ⚠ **ONLY THE `Whisps` BLOCK OF `tank 3rd.csv` WAS BUILT.** That file is still open — your `NOT
  DONE` banner is at line 228 — and the taunt, mass-taunt, intimidate, freeze, stay and charm
  ladders, the anti-magic and weapon masteries and Defensive Wall all wait for the one-pass tank
  delta. The whisp rows went in because the whisp SYSTEM is what you queued and those are the rows
  you wrote for it.

  🔴 **THREE THINGS IN YOUR FILE NEED ONE WORD EACH FROM YOU.** You laddered the whisp block from
  one rung to EIGHT while this was being built, and the build follows your ladders exactly — MP
  50→100, the taunt/charm aggro 6500→12000, the heal 250→740, armor break 10/5% → 30/15%, weapon
  break 5% → 15%, and the two level sets (40/46/52/58/62/66/70/74 and 43/49/55/60/64/68/72/74).
  `--check` is clean on every one of those numbers. These three it cannot settle:

  1. 🔴 **THE RACE COLUMN NOW GIVES THE HUMAN FOUR WHISPS AND THE DEMON NONE.** Your earlier rows
     read Human / Elf / **Demon**; the laddered block reads Human for taunt, bind, armor break AND
     weapon break. Taken literally a Demon Bulwark has no whisps at all, which is not a design — it
     is the tail of the Human block copied. **Built as Demon**, your original split, under the same
     typo rule the monotonic one runs on: strong evidence, corrected, and reported rather than
     silently accepted. One word puts it back if you meant it.
  2. 🟠 **Charming Whisp's last four rows say `uses whisp_provoke`** in the comment column where the
     first four say `whisp_charm`. Read as charm throughout — the skill is called Charming Whisp and
     its DESCR says *"Charming the enemy"*. Comment column only; nothing else read it.
  3. 🟡 **YOUR SP COLUMN IN THAT FILE HAS NO `k`.** Every other file writes `36k` / `880k`; this one
     writes `28` / `880`. Read as THOUSANDS — your level-74 rows say `880` where the healer template
     you pasted into the SAME file says `880k` at 74. ⚠ **Until you add the `k`s, `--check` reports
     28 yellow SP lines on this file** (`sp CSV 28 vs code 28000`). That is the report, deliberately
     left showing rather than taught away in the tool. It is wrong on every row of the file if it is
     wrong at all, not just these.

  ⚠ **ONE ENGINE DECISION THE BUILD HAD TO MAKE, because a startup guard demanded it be deliberate:**
  a whisp's Armor Break and Weapon Break ladder on the HEALER's buff keys, so the two compete rung
  for rung — which is your *"upgrade-or-fail against a Healer's Armor Break of lower/equal/higher
  level"*. The guard's two escapes both break your rule (a separate key lets them STACK; `FlatRank`
  pins the whisp at rank 1 forever), so the guard now carries a two-entry allowance with the reasoning
  written into it. The cost, stated: from rung 6 the whisp's number is slightly the stronger at the
  same rung, so an equal-rank tie goes to the healer's longer duration and the party gets .20 where
  the whisp offered .22 — in that one window only, and erring toward the healer's own spell.

  ---
  **Your original design, unchanged:** IG cubics — a non-targetable support entity that rides the
  master and fires its own skills.

  **The rules you set, which are the whole spec:**
  - **Not an `Entity`.** *"it can be part of the character game object no need a real entety"* — like the
    totem is a heal skill and not an entity. Leashed 100-200 from the master, follows on a short delay,
    parks at its slot when the master stops.
  - **Uninfluenced by master gear.** M.Atk, cast speed, landing rate scale on the **whisp's skill level
    + the master's level** only. Their own P/M.Atk is **1**, so every whisp debuff needs a **base ATK
    modifier** of its own.
  - **Slots are a PUSH-DOWN stack, not a set.** `[][][]` →A→ `[A][][]` →B→ `[B][A][]` →C→ `[C][B][A]`
    →D→ `[D][C][B]` →A→ `[A][D][C]` →D→ `[A][D][C]` (D refreshed). One slot by default; a
    passive raises it to 2 then 3.
  - **They behave as BUFFS**: 20 min default, resummon at 5s remaining (`BL-112`'s window), lost on
    death — *"if its easier we can make them to be saved by angelsProtection"*.
  - **Conditions, not IG's 8-13s clock**: *"i want our to be just like normal skills with some
    conditions and cooldown"*. The CSV carries them per skill — master in combat, range, HP band.
  - **Whisp debuffs do not stack with the player version**: Whisp Armor Break must upgrade-or-fail
    against a Healer's Armor Break of lower/equal/higher level.
  - **The three-way separation you drew, worth keeping written down:** totems stand still and cast AoE
    off the master's pvp-on/off · pets do only what the master orders and use no skills unprompted ·
    **whisps follow and act on their own**, off the master's pvp-on/off.

  **The PoC you asked for** (`tank 3rd.csv`, already authored): Human = taunt + bind, Elf = charm +
  heal, Demon = armor-break + weapon-break, all @40, plus `Whisp Mastery` @60 raising the limit to 2.
  ⚠ That row's `SKILL_ID` is `tank_shield_mastery` — a copy-paste, it needs its own id.
  Nine whisp skills in `whisps_skills.csv`: provoke, charm, bind, armor/weapon/gravity break, heal,
  quick-heal, clear. **`whisp_charm` depends on `BL-110`.**

- `BL-110` ✅ **BUILT 2026-09-02 (0.104.0) — FEAR AND CHARM, the two states where the SERVER drives
  your body.** *"both dont change target like taunt — just act uncontrolably"*.
  - **Fear** — cannot act; **runs** to a random point 100-200 away, picking a new one on arrival.
  - **Charm** — cannot act; **walks** toward the caster, re-aimed every tick.

  Neither re-points the victim's TARGET, which is the thread running through your whole `BL-123`
  ruling: charm and fear move the body and lock the hands, taunt alone moves the eyes.

  🔑 **Fear kept its bit and changed its meaning.** It used to be *"cannot cast or attack, can still
  move"* — a silence, not a fear, and the victim kept full control of his feet. **Charm is a FIELD**
  (`SkillDef.Charms`); the flag enum has been full since `1L << 62`, and every flag test on the way
  to a debuff had to be taught about it — the buff-row test, the contested-vs-fizzle test, the boss
  control immunity and the "is this hostile" test at the cast gate. Any one of them missed is a
  silent wrong answer, which is the standing cost of the enum being full.

  🔑 **`/buff` can reach a control skill now, and could not before.** It matched `Category.Buff`
  only, so there was no way on any character to put a stun, a fear or a charm on somebody and watch
  it. Fear and charm are server-driven MOVEMENT — the one class of effect you cannot check by reading
  a stat panel — so the tool that could not reach them was the one most needed. `/buff @target charm`.

  ✅ **CLOSED 2026-09-02 (0.105.0) — see `BL-123`. The old text follows.** ~~STILL OPEN: TAUNT IS MOB-ONLY.~~ `effect.HasFlag(Taunt) &&
  target.Kind == EntityKind.Mob` — the fourth `Kind`-shaped gate of that family — so a taunt does
  nothing in PvP, which your *"mobs/players"* wording asks for. Fear and charm work on both, since
  they were built after the lesson. Not fixed here because a taunt aimed at a PERSON needs a ruling
  you have not given: it locks his TARGET, and locking a player's target is a stronger thing to do
  to someone than moving his feet. **Say the word and it is a one-line change.**

- `BL-111` ✅ **BUILT 2026-09-02 (0.104.0) — FOUR BUFF BARS, AND THE `n/20` COUNT.** The split is the duration-shaped one you gave, and the counter comes from the SERVER off the same predicate that evicts, so the number and the rule cannot disagree. 🔴 **Asking for that number found a real bug: a DEBUFF occupied a buff slot** — a debuff def carries the default `BuffRow.Buff` (the Debuff row is a display override), so a poison landing on you at 20 buffs EVICTED ONE OF YOUR BLESSINGS and took the slot itself. Fixed, and guarded in SmokeTest §14. Original ask below.

- `BL-111`.old 🔵 **FOUR BUFF BARS, not one.** *"I cannot see if I have 20 or less buffs to not over buff me"*:
  1. **normal** — only what counts against the 20 cap;
  2. **toggles + consumables** — HP/MP potions, HoTs, the toggles;
  3. **items** — Runes and other item buffs;
  4. **others** — everything else.

  🔑 **The rule that decides the split is DURATION-SHAPED, not source-shaped**, and you gave the two
  worked examples: Bow Expertise is a **20-minute self-buff and belongs in NORMAL**, while the tank's
  ultimate and the warrior's Battle Defence/Presence are **30-120s and do not count against the limit**,
  so they go in **others**.

- `BL-112` ✅ **BUILT 2026-09-02 (0.102.5) — THE REBUFF WINDOW — 5 SECONDS BEFORE IT WEARS OFF, not after it drops.** *"Now they
  buff once they drop. They should buff when the time is 5s... All buffs should have 1~5s cast so when
  the buff have 5s remaining is caunt as able to be rebuffed."*
  - **The bug this fixes costs mana twice**: Conceal rebuffs at **15s remaining on a 30s buff**, so you
    pay for it twice over. Condition is `remaining <= 5s AND not on cooldown`.
  - **Second half, same entry:** *"if I have a buff L1 and I buff myself with it ...then after lvling up
    I learn L2 ..it should rebuf me ..because it's stronger"* — a higher rung of a buff you already
    hold is a rebuff trigger, not a wait.

- `BL-113` ✅ **BUILT 2026-09-02 (0.102.5) — A SKILL THAT LEAVES A BUFF CANNOT RE-EXECUTE WHILE THAT BUFF IS LIVE — a general rule,
  raised by Harmony of Restoration.** Your words: *"the logic is for all skills that leave a buff .. the
  skill cannot be reexecuted even after the cooldown is done while the same skill is already active
  (same as buffs, don't auto rebuff u till they worn off)"*.

  **The symptom:** HoR is fine below L9; from L9 it costs 400+ MP for +5/s and fires **on cooldown**
  rather than on threshold, draining you to zero. **Your model:** *"it's a healing buff ..not a skill ..
  It's like a hp pot — I cannot use another pot while the last is active"*. So: fire when HP ≤ the
  threshold **and** the previous instance is gone; on cooldown-end, re-check that you still hold the
  buff before re-firing.

  ✅ Shipped as `OwnBuffStillRunningOn`, applied to the auto chain's HEAL and MP-HEAL targets. ⚠ The
  other kinds are excluded because they already have a BETTER test, not because the rule stops at
  heals: a `Buff` must be able to re-fire inside `BL-112`'s new 5s renewal window, and a `Debuff`
  already runs this exact rule with a zero window. ⚠ **A MANUAL PRESS IS STILL ALLOWED** — your *"for
  all skills"* may mean the tap too, but refusing a hand-cast rebuff before a boss pull is a big
  behaviour change to infer from one sentence. Say the word and it becomes a hard gate.

  🔑 **Why the CD is deliberately low, which is the part not to "fix":** *"once we have a debuff that
  increases cooldown x2, HoR stays permanent instead of falling behind its duration — while a healing
  totem with cd 25 becomes 50 against a duration of 30"*. The low CD is armour against a future
  cooldown debuff. Do not raise it.

- `BL-114` ✅ **BUILT 2026-09-02 (0.102.11) — THE SELL DIVISOR IS PER-RARITY.** *"the sell prices are to
  much for the current drop rates ... common sels for 0.225 of the original price so selling 4.(4) items
  Is like I sold a real item"*. Your ladder, and it divides the **MYTHIC rung of the price table** —
  which is the units your own arithmetic is in, not the item's own buy price:

  | rarity | divisor | sell, as a fraction of the Mythic price | was | own-price divisor now |
  | --- | --- | --- | --- | --- |
  | Mythic | **/10** | 0.100 | 0.1000 — unchanged | /10 |
  | Legendary | **/25** | 0.040 | 0.0425 | /10.6 |
  | Epic | **/33** | 0.030 | 0.0350 | /11.6 |
  | Rare | **/50** | 0.020 | 0.0350 | /17.5 |
  | Uncommon | **/100** | 0.010 | 0.0275 | /27.5 |
  | Common | **/200** | 0.005 | 0.0225 | /45 |

  ⚠ **The entry as first written had the "was" column wrong** — it said a flat /25. The live divisor
  has been **/10** since playtest-18 (2026-08-05), and /10 off a Common's own price is the 0.0225 you
  quoted. Both readings are in the table above so neither can be misread again.

  ✅ Shipped in the one place (`ItemCatalog.SellPrice`), off a new `TieredGearBasePrice` — the row cell
  before `RarityPriceMul`. 🔑 **Only tiered GEAR moves**: use-consumables keep the flat /10, because a
  buff potion has no Mythic rung to be a fraction of and most carry `SellPriceOverride: 0` anyway.
  ⚠ It also **separates Epic from Rare**, which sold identically before (they share a buy multiplier of
  0.35 on purpose); /33 against /50 is your ladder and it is monotonic.

  📊 **Measured, not derived** (`tools/BalanceMatrix`, the playtest-18 farm at level 34): the Common
  gauntlet goes **buy 112,500 / sell 11,250 → sell 2,500**, so it needs **45 sales to buy its own
  replacement, up from 10**. The effective divisor on what actually drops at 34 is **/35.5**, was /10.
  🔴 **The consequence to look at: that farm's total falls from ~1.04M to ~549k** — your playtest-18
  target for it was ~1kk, so this puts it at **half the number you accepted a month ago**. That is the
  cut you asked for and it is bigger than the "4.4 → 20" line suggests, because the ladder bites the
  Common/Uncommon end which is nearly everything that drops. Say the word if you want the gear DROP
  rate raised back to meet it — that is the other knob and it is one number.

- `BL-115` ✅ **BUILT 2026-09-02 (0.102.7) — NPCs GET `canDie` AND `retaliate`, AND ATTACKING ANY NPC NEEDS PVP-ON.** *"field
  guards/watchmen are targetable and hittable even without a pvp-on ...and i can hit them auto in
  auto-farm ...they shouldn't act as mob"*. Your model, verbatim:
  - **every NPC** is attackable **only with pvp-on**;
  - **`canDie: false`** (the default) — normal HP, but it **cannot fall below 1**, a training dummy;
  - **`retaliate: false`** (the default) — *"don't strike back just sit and take it"*;
  - **guards are the only pair set true/true** — they can be killed and they do strike back.

  ⚠ **Auto-farm must never target an NPC**, which is the half that bit you.

  ✅ **What the guard half actually was, and it is worse than a missing rule:** `BL-79` DID write
  *"a player attacking them (pvp-on must be on)"* — into `CanPvpHit`, where it has sat since 0.94.0.
  Both callers asked it as `target.Kind == EntityKind.Player && !CanPvpHit(…)`, and **a guard is a
  MOB**, so no caller ever arrived with a target that clause could answer. The rule was stated in the
  code, commented at length, and unreachable. Both single-target paths delegate unconditionally now;
  an ordinary mob still answers `true`, so PvE is untouched. 🔑 **The auto-farm half needed a
  DIFFERENT answer, not the same one** — a guard is excluded from *acquisition* outright, because the
  toggle alone would still let a PK's autopilot walk into a guard tower. Defence is untouched.
  ✅ The NPC half is `NpcDef.CanDie` / `NpcDef.Retaliate`, both false everywhere, replacing the flat
  *"you can't attack that"* of 2026-07-21. ⚠ **The guards were NOT rebuilt as NPCs to carry the two
  booleans** — they are mobs with the mob AI, a class kit, real gear and a respawn timer, and they are
  already your true/true pair by construction. Say the word if you want them literally `NpcDef`s.

- `BL-116` ✅ **BUILT 2026-09-02 (0.102.8) — A LEASHED CREATURE SPRINTS HOME AND IS DEAF ON THE WAY.**
  *"I stand just outside the radius and shoot it with a bow .. The mob agros me back then stops and it
  moves towards/away from me and if I do more than it's 5% regen I can kill it"*. You rejected all
  three of my options and were right about every one: **full-heal on leash and damage-immunity both
  make the 5%/s regen ramp dead code, and an extending chase range has no natural stopping point.**
  Your fourth is what shipped — *"when a mob reaches the end of leash it start to sprint back to start
  .. like +100ms then when reached start it reset the ability to chace again"*.

  🔴 **AND THE NUMBER YOU WERE FIGHTING WAS NOT 5%, IT WAS 0.1% — a factor of 50.** `AddThreat` set
  `Engaged = true` unconditionally, on every hit, and the regen tick picks its rate off that same flag
  (0.1%/s engaged vs 5%/s idle). So the arrow that re-aggroed the mob also pinned it to the combat
  regen rate. **That is also the whole of *"it moves towards/away from me"***: the mob turned for home,
  the next arrow re-engaged it, it stepped back over the 1500 boundary, `ResetMob` fired again — it
  oscillated on the rim forever, permanently inside bow range, and never once got home.

  🔑 **THE SPRINT IS THE TRIM; THE DEAFNESS IS THE FIX.** `Entity.ReturningHome`, set by `ResetMob`,
  cleared within 60 units of home. While set the creature runs at `RunSpeed + 100`
  (`GameConstants.MobLeashSprintBonus`), does not wander, does not scan for aggro, and **takes no
  threat at all** — damage still lands and still kills it, but nothing re-targets or re-engages it.
  Without that early return in `AddThreat` the sprint would not survive its first tick.

  ✅ Your HP ruling is exactly what falls out, with no extra code: not being Engaged puts it on the
  **5%/s idle ramp for the whole return**, it goes on climbing at home, anyone may re-pull it wounded,
  and when the bar tops out with nobody having re-engaged, `MobRecoveryCheck` closes the pull (ledger,
  enrage and boss-phase cursor re-armed) — *"their hp keep the 5% untill when reached home and regen
  until some1 reengages ... when full and no1 reengaged then they reset aggro and etc"*.

  ⚠ Server-only; no new APK needed. ⚠ Kiting itself is **untouched and deliberately so** — your
  ruling: *"kiting is a way mage/archer with low defence to be able to farm actively ... monster speed
  is irelevant.. its only how many seconds u have before it cut the 900 range"*. See `BL-122`.

- `BL-117` ✅ **BUILT 2026-09-02 (0.104.0) — THE `[ORDER]` BUTTON, your five orders.** 🔑 ONE setting for every list in the game (bag, vendor sell, vendor buy, buyback, warehouse), so the order you pick in your bag is the order you see at the shop. Not persisted — a view preference, resetting to A-Z, which is what every list did before. Your ask: One button, cycling:
  normal/alphabetical → alphabetical descending → rarity then alphabetical (Mythic first) → rarity
  descending then alphabetical → **type then rarity then alphabetical** (all weapons by rarity within
  name, then armor, then jewels, then consumables). *"same for npcs sell/buy/buyback inventories"*.

- `BL-118` ✅ **BUILT 2026-09-02 (0.104.0) — `RateConfig.FreeClassChange`, on the tuning panel beside the exp rate (0/1).** 🔑 It applies to EVERYONE on the server, not to admins — the character who needs it is an ordinary player, which is the whole thing you were working around. ⚠ It waives the items and the quest, never the level, the race/class fit or the never-the-same-discipline rule. The NPC window reads the same flag, so the option is offered rather than greyed out. Your ask: *"at x100exp doing quest at 20 is
  kinda annoying ..and I enter with the admin char make the player an admin change class then make him
  player again"*. A toggle beside the exp rate in the admin menu; while it is on, the class master
  changes your class on **conversation alone** — no quest items, and no requirement to have done the
  two quests before it. Applies to non-admin characters, which is the whole point.

- `BL-119` ✅ **BUILT 2026-09-02 (0.102.3) — ARMOR MASTERY STACKED WITH THE DISCIPLINE MASTERIES, ×4 CAST SPEED.** *"I'm 40lvl
  harmonist with 35lvl armor_mastery and wc_harmonist_light_mastery — both remove the light penalty"*.
  Your own fix, which is the right one: the 40+ rungs become **`buffer_armor_mastery`**, which
  **replaces** `armor_mastery` rungs 20-35, and **`wc_chanter_heavy_mastery`** and
  **`wc_harmonist_light_mastery` also replace `armor_mastery`** so no two can be held at once.
  ✅ Shipped exactly as you wrote it. The 40-74 rungs are `buffer_armor_mastery` now, it replaces
  `armor_mastery` (and `mastery_robe` beneath it), and **both race masteries replace `armor_mastery`
  too** — which is the half that closes the window, because the bug only needed a buffer who had NOT
  yet bought the 40 rung and so still held the cleric's rung 4. 🔑 **A split needs a save migration
  where a delete does not**: `ParseLearnedSkills` carries the level across (rung 5 → 1, rung 18 → 14),
  or a saved Warchanter would have lost the whole ladder invisibly. Both buffer CSVs moved with it.

- `BL-120` ✅ **BUILT 2026-09-02 (0.102.7) — COMBO MASTERY WORKS WITH A BOW, and its chance is re-tuned.** *"buffers combo mastery
  should work with a bow ... Also 3% chance with `blunt/1` and 3.45% chance with `bow|blunt/2`"*. Your
  reason, which is why the two numbers differ: *"2h weapons are slower by ~12/18%, so increasing the
  chance balances the slower attack speed (bow is faster than 2h blunt as harmonist have bow
  expertise)"*. ⚠ The CSV row changes with the code — this is the third requirement in a fortnight that
  lived only in free-text `DESCR` and disagreed with its own row.

  ✅ **The bow half was already done** — it was fixed on 2026-08-29 as part of the weapon-gate run
  (the elf Warchanter could never once proc a passive he had paid 880k SP for). What landed today is
  the **second chance**: `SkillDef.ProcChanceTwoHanded`, unset on every other proc in the game, and
  read only after the weapon has already satisfied the gate. 🔑 **It is a general field and not a
  Combo-Mastery special case, because your reason generalises**: a proc rolled per LANDED HIT is worth
  less on a slower weapon, so any future proc a two-hander can carry owes the same correction.
  3.45/3.00 = **×1.15**, the middle of your own 12-18%. Bow and Dual are inherently two-handed, so a
  bow takes the higher branch by construction. Both `buffer 3rd.csv` rows moved with it.

- `BL-121` ❌ **DECLINED BY YOU, 2026-09-02 — NOTHING TO BUILD, AND NOTHING LEFT OPEN.** *"121 i dont
  want ... the game progresses faster no need to break the economy ... with vendor prices ..."*.
  My proposal was a `RateConfig.VendorPriceRate` knob; you ruled the knob itself out, not just its
  value. Kept as a record so it is not re-proposed: **vendor prices do not scale with rates**, and
  the reason is yours — a faster game is the point of the rates, and re-pricing the shop to claw it
  back is breaking the economy to defend a number nobody asked for. The analysis under it still
  stands and is still worth reading, but it argues for the same conclusion by a different road.

- `BL-122` ✅ **BUILT 2026-09-02 (0.102.9) — YOUR SIX BASE MOVE SPEEDS, AND NO DEX TERM.** Authored by
  you verbatim: **Elf 143/114, Human 115/109, Demon 112/113** (fighter/mage), replacing 130-165.

  🔑 **I had read your *"speed should be around 180 for slow and 210 for faster classes"* as the BASE
  table and proposed 180-210 here — wrong by ~65 points.** It describes where a party-buffed player
  LANDS. The buff stack is **+61** (Swift/Wind Grace +33, Harmony of Speed +20, Frenzy +8), so your
  table gives Human fighter 115+61 = **176** ≈ "180 for slow" and Elf fighter 143+61 = **204** ≈ "210
  for faster classes". A rogue's own +60 then clears 250 — your *"they usually max it out"*.

  ✅ **The no-DEX rule was already true** — *"IG is base class+race speed x dex mod but i dont want dex
  to affect speed"*. Nothing in the codebase has ever multiplied move speed by DEX, so there was
  nothing to remove. It is written into `SpeedTable` as a rule to KEEP, so nobody re-introduces the IG
  modifier while porting a formula from a reference table that carries it.

  ⚠ Two things in your table are deliberate and are NOT typos to interpolate away: the **Demon mage
  (113) is one point faster than the Demon fighter (112)**, and the **Elf fighter's 143 is a 28-point
  outlier** over every other row. Noted in the code so a later pass does not "fix" them.

  ⚠ **Still to come, when you author the rogue:** the sprint raises `MoveSpeedCap` itself to **300**
  while a dash potion leaves everyone else at 250. `Entity.MoveSpeedCap` is already per-entity, so
  that is one field on the sprint skill — no rework.

- `BL-123` ✅ **CLOSED 2026-09-02 (0.105.0). All three control states are built.** Charm and fear landed with `BL-110`; the taunt's last half landed with the tank, on your ruling: *"the aggression ladder is mob only. The actual target change is pvp (+ mob if mobs have targets though) and charm/fear work on both."* So the LOCK reaches players (their target is pinned to the taunter, refused in `HandleAttack`) and the AGGRO LADDER is paid only into a monster's threat table — a person has no threat table for it to mean anything against. 🔑 The fourth `Kind == Player`-shaped gate of that family, and the first that was HALF right: deleting the test outright would have paid threat into a table nobody reads. ⚠ `TauntLockTicks` only ever counted down in `MobAi`, so the lock would have been PERMANENT on a player — a counter only one kind of entity decrements is a trap the second the other kind can set it. Original entry follows.

- `BL-123`.old 🟠 **THE THREE CONTROL STATES — the ruling as written. ONE
  THING IS LEFT: TAUNT IS STILL MOB-ONLY, AND THAT NEEDS YOUR RULING.** See `BL-110` above for what
  landed. The rest of this entry is the original ruling and the state of the code before it.
  Your rulings, 2026-09-02, for mobs AND players alike:

  1. **Charm** — the target *"walk"* (**walking speed**, so `MoveState.Walking`) **toward the caster**
     for the duration. If the charm carries an aggro value it **adds to the caster's general aggro
     points**. 🔑 **Do NOT force a target change**, but the victim **cannot act**.
  2. **Fear** — the target *"run in place"*: **runs uncontrollably** inside a **100-200 radius**.
     🔑 **Do NOT force a target change**, but the victim **cannot act**.
  3. **Taunt** — the victim **may act and move freely; only its TARGET is locked to the caster.** If
     the taunt carries an aggro value it **adds to the caster's general aggro points**.

  🔑 **The common thread across all three: none of them changes who the victim is TARGETING except
  taunt, which changes only that.** Charm and fear move the body and lock the hands; they do not
  re-point the eyes.

  **Where the code stands today:**
  - **Charm does not exist** — no `SkillEffect` bit, no state. ⚠ `SkillEffect` has **zero bits left**
    (long-standing), so this needs the fields route, like `SkillLevel.ExtraPassives` before it.
  - **Fear exists but is the wrong shape.** `SkillEffect.Fear` (bit 41) today means only *"cannot cast
    or attack, can still move"* — the player keeps **full control** of his movement. Your version
    takes control away and drives him. `Entity.IsFeared` / `IsActionLocked` already give the
    can't-act half; the uncontrolled run is the missing half.
  - **Taunt is built for MOBS ONLY** (`GameLoopService`, `effect.HasFlag(SkillEffect.Taunt) &&
    target.Kind == EntityKind.Mob`) — so it does nothing in PvP, which your *"mobs/players"* wording
    asks for. ⚠ **This is a fourth `Kind == Player`-shaped gate**, the same family of bug as `BL-79`'s
    guards and the three found in 0.94.0.
  ✅ **ANSWERED + HALF BUILT 2026-09-02 (0.102.10): A PLAIN ADD. NO JUMP TO THE TOP.** *"taunt (and
  charm also adds aggro points) but they donnt mve you on top for free .. the idea is tank to spam
  taunt/charm for mob to keep it agrro on him .. if some1 is doing alot of dmg/heals the tank will
  ahve hard time to keep it up so the one must slow down so tank can take 1st place"*. The
  `max(mine, top) + power` jump is gone; the target LOCK is untouched and is the taunt's only
  guarantee. This makes the threat economy (damage 1:1, `ThreatHealFactor`, `ThreatBuffPerLevel`) load
  bearing instead of decorative whenever a tank had a taunt off cooldown.

  🟢 **MEASURED FIRST — your 4,500-6,000 ladder still works as a plain add, so don't re-tune it:**
  Provoke on a 6s reuse is **750-1,000 threat/s**, against a level-28-36 attacker's **~250-300 dps**
  (BalanceMatrix E4: 2.6-3.5s TTK on a 667-1,077 HP mob) and a cleric spamming Quick Heal at
  **~750/s** (301 power / 2s cast × `ThreatHealFactor` 10). So the tank leads on the taunt alone, is
  level with a flat-out healer, and IS pulled off by a healer plus a committed nuker — exactly the
  pressure you described.

  ⚠ **STILL OWED, waiting on your tank CSVs:** `--check` already reads **duration 1.5 and range 400**
  out of your in-flight `tank 2nd.csv` against the code's 3s / 600 — your *"ill lower it to 1.5s"*, and
  a range cut you have not mentioned. I have not chased it, because the same file also carries
  `tank 3rd.csv`'s 15 unregistered rungs of Taunt, Mass Taunt, Intimidate and Tank Anti-Magic. **Say
  when you are done and the whole tank delta goes in as one pass** — duration, range, the 40+ ladder,
  and the four Taunt descriptions (which still say "for 3s").

  🔑 **CHARM'S AGGRO IS UNCONDITIONAL, ITS CONTROL IS NOT.** *"charm can fail the actual debuff (the
  un-charm-movement) but still adds the points"*. So when charm is built, the threat add happens on
  CAST and only the walk-toward-caster rolls against the land rate — the reverse of every other debuff
  here, and the reason a tank can rely on charm for aggro at all. **That asymmetry is why the taunt's
  lock can be shortened to 1.5s**: the guaranteed half is the lock, so it should be the brief one.

- `BL-124` 🟢 **THE SLIPS `BL-108`'s BUILD TURNED UP — both rulings made, nothing owed.** Kept as a
  record of where your file disagreed with itself, so the same paste does not happen twice.

  **The two you ruled on, 2026-09-02** — *"spell mastery 76-90 to have its coresponding sp/gold cost
  and fix doctor blunt mastery 40-74 to `Blunt: ....` (same as buffer 4th)"*:
  - ✅ **Spell Mastery 76-90 now runs on the tier's ladder** (6.5kk / 11kk / 16kk / 80kk, then gold
    only, 5kk → 100kk). Its fifteen rows had been pasted out of `buffer 3rd.csv` still carrying that
    file's 36k … 880k SP and its `[]` in the gold cell, so a level-90 rung of your buffer's core
    caster passive cost **880k SP and no gold**.
  - ✅ **`doctor_blunt_mastery`'s eight 40-74 DESCR cells read "Blunt:"**, matching your own 76-90
    rows. The hands stay in the WEAPON column (`blunt/1`) where `--check` can see them. This is the
    `BL-105` rule doing its job — the prose and the enforced gate were saying opposite things, which
    is exactly the failure the column was built to end.

  **The four I corrected under the monotonic rule** (a value going backwards is a typo — interpolate
  or report, never accept). The CSV was edited to match the build, so file and game still agree:
  - **Harmony of Restoration** read 110 / **100** / 120 / **100** / 130 / **100** … — every ODD rung
    was an untouched copy of the level-74 row (100 HP/s, 10 MP/s). Buying rung 89 would have made the
    hymn *worse* than rung 88. Straightened to 110 → 180 in fives, which is what your even rungs
    describe. ⚠ Your **MP column** on the same ladder is kept verbatim, dip and all — it falls 488 →
    454 at level 80, which only makes the hymn cheaper, so nothing breaks.
  - **Harmony of the Wizard** had **two rows at level 78**. The second one's price cells are the 79
    band (80kk SP + 1kk gold), so it is read as 79.
  - **Sound Burst** had a second, identical **level-90 row** sitting at the bottom of the Sound Smash
    block. Removed.
  - **Doctor / Warlock Weapon Mastery** left the WEAPON column blank at the 4th tier while the 3rd
    tier gates them `blunt/1` and `blunt/2`. A ladder cannot change hands halfway up, so the gate is
    carried forward and the column filled in.

  **And two cells that were simply empty**, filled from the row's own siblings: `magic_proficiency`'s
  reuse/duration pair in `shared 4th.csv` (0/0 against Arcane Protection's and Physical Proficiency's
  30/10, on a row that is nothing but a proc), and the AoE radius of Harmony of the Soul / Madness /
  Mark (blank, where every other harmony says 800).

- `BL-125` 🔵 **A GROUP BUFF WAS DROPPING EVERY PAYLOAD THAT IS NOT A `SkillEffect` BIT. Fixed
  2026-09-02 (0.103.0) — logged because it is worth a playtest look, not because anything is owed.**
  `ApplyBuff` folded only its children's `Effect` and `Magnitudes` into a group. Half the buff payloads
  in the game are FIELDS instead (the flag enum has been full since `1L << 62`), so:
  - **Arcane and Feral Protection granted NOTHING AT ALL** — both its children are pure CC-resist
    fields, so the group landed with an icon and zero numbers;
  - **Soul Reinforcement lost its whole −20% / −10% MP-cost third.**

  Nothing on screen said so, which is why it survived: the buff appears on the bar either way. 🔑 The
  fix also gave a group's own rung the right to STATE a field, which is what lets Soul Reinforcement
  ladder its MP cost 21/11% → 30/20% across the 4th tier. ⚠ **Verified by reading the fold, not by
  playing** — worth confirming on a 74+ buffer that Arcane and Feral Protection now really resists.

- `BL-126` ✅ **BUILT 2026-09-03 (0.106.0) — `RateConfig.FreeBuffs`: ANY PLAYER MAY `/buff` HIMSELF, and
  the whole admin set now lasts an hour.** You gave both roads — *"a npc buffer that contains all buff
  for 1h ...harmonies marks etc ... Or easier with this settings on everyone can use /buff command
  (just self not others)"* — and named the second one easier. It is, and it lands the same thing: a
  non-admin character fully buffed without being promoted to admin and demoted again, exactly the
  workaround `BL-118` deleted for class change. A 0/1 on the Tune tab beside Free class change.
  - **Self only, enforced on the SERVER.** Under the flag a non-staff `/buff` never parses the target
    word — no name, no `@t` — so the half of the command that acts *on* someone else stays staff.
  - **`/buff` now travels from every client**, because whether a player may cast it is a server setting
    the client is never told (the tuning DTO is admin-only). With the flag off the server says
    *"Self-buffing is switched off on this server."* 🔑 The old non-staff path returned in SILENCE.
  - 🔑 **All admin buffs are 1 hour now** (*"make all the admin buffs 1h"*): a class buff's authored
    duration is 20 minutes or less, the NPC blessings beside them already ran an hour, so **half the
    bar expired while the other half stayed**. One override on the set the button and `/buff` hand out.
  - **Shrouding Hymn and Bow Expertise are out of the full buff**, as you asked — both were genuinely
    in it. Party stealth means a buffed test character cannot be attacked unless he starts the fight;
    Bow Expertise does nothing without a bow. Both still reachable as `/buff <name>`.
  - ⚠ **The NPC road is the half NOT built** — no buffer is spawned by the flag. It is now **`BL-128`**
    below, with the four things I would need from you, because it is the version that could ship to
    players and the command is not: `/buff` is a staff tool opened by a switch, an NPC is content.
  - ✅ **The two I dropped from the full buff were the two you wanted dropped — `BL-129`, answered.**

- `BL-127` ✅ **BUILT 2026-09-03 (0.106.0) — THE FUNCTIONS AND CLASS TABS, exactly your four points.**
  *"Function menu remove the lvl up buttons. Under full buff add the 4 marks and 2 great bulwark/might
  (now as mage I get might - I want to be able to swap it) ... The lvl up buttons go to the class tab...
  Also there the reset classes should be same principal as the subclass -> one button and selection."*
  - **The four level buttons moved to the Class tab**, and sit first — above everything they unblock (a
    discipline needs 40, a subclass its own floor, a 4th class 76).
  - **Six buff buttons under Full Buffs**: Holy / Life / Blood / Harmony Mark, Great Might, Great
    Bulwark. 🔑 These are precisely the buffs a full buff can only give you ONE of — the four Marks
    share a buff key, the two greats share theirs — so the set picked one and the others were
    unreachable. The button IS the swap. They send the skill ID, which `/buff` matches exactly, so a
    button can never trip the ambiguity rule a name lookup lives with ("Might" is three buffs).
  - **Reset is one button and a selection**, the same shape as "+ Add a class" beside it.

- `BL-128` ❌ **DROPPED BY YOU, 2026-09-03 — NOTHING OWED.** *"Skip the npc buffer (bl128)"*. The
  `/buff` road (`BL-126`, 0.106.0) covers the need, and it is the road you called the easier one when
  you gave both. If a buffer NPC is ever wanted as **content** rather than as a staff tool, the four
  questions in the old entry are the ones that have to be answered first — kept for that day only.

- `BL-128`.old ❓ **THE NPC BUFFER ITSELF — the half of `BL-126` I did NOT build, and it is the half that
  could ship to players.** Your first description, 2026-09-03: *"I want a setting in the menu same as
  the class without quest one to include a npc buffer that contains all buff for 1h ...harmonies marks
  etc ... It don't have full buff or partial just a save button. So if this setting is ON it spawns a
  buffer I don't care if requires a restart of server or not."* You then offered the `/buff` road as
  the easier one and **that is what shipped** (0.106.0) — it covers the test-server need, so nothing is
  blocked. What is NOT built is the NPC.

  **Why it is worth keeping as its own entry rather than closing with `BL-126`:** the two are not the
  same thing wearing different clothes. `/buff` is a staff tool opened to everybody by a switch — it
  cannot exist in a shipped game. **A buffer NPC is CONTENT**: it has a place in a town, a price (or
  not), a window, and the `BL-95` preset machinery already behind it. Your *"just a save button"* is a
  real design statement — the full/partial presets are what you would strip, leaving each player's own
  saved preset as the only door — and that is a shape the paid NPC buffer could eventually adopt too.

  **What I would need from you before building it**, and none of it is guessable:
  1. **Where does it stand?** One in every town, or one in a single test town you teleport to?
  2. **Free, or priced?** The `/buff` road made it free by construction; an NPC could keep the price
     and just carry the whole set (which is the version that survives into the real game).
  3. **The set**: the admin set at top rung (groups, Harmonies, and the Marks you asked buttons for),
     or the existing sixteen NPC blessings? *"all buff for 1h ...harmonies marks etc"* reads as the
     first, which is a strictly stronger buffer than any player can be.
  4. **Does the flag spawn it, or does it always exist and the flag only makes it free?** You said a
     restart is acceptable, which makes the spawn road cheap — but "always there, price waived" needs
     no restart at all and is one less state to reason about.

- `BL-129` ✅ **ANSWERED 2026-09-03 — *OUT*, AND THE 0.106.0 BUILD WAS ALREADY RIGHT. NO CODE CHANGED.**
  *"Nor the shrouding nor the expertise I want as a button or included in admin fullbuff (or /buff)."*
  The reading I built on was the right one: both sit in `SkillCatalog.AdminBuffSkip`
  (`Skills.Buffer.cs`), out of the admin set, and neither became a seventh button.
  🔑 **Verified while closing this that neither id is in `NewbieBuffSet` either** — that list is
  `Concat`ed onto the admin set *after* the skip filter runs, so a name sitting in both places would
  have walked straight back in past its own skip. Neither does.
  ⚠ **The one thing still true:** each remains castable ALONE by exact name (`/buff shrouding hymn`,
  `/buff bow expertise`) — the skip list governs what the SET hands out, not what the command may
  target. Say the word if you want the command to refuse them outright; it is one line.

- `BL-129`.old ❓ **ONE WORD OWED: DID YOU MEAN BOW EXPERTISE *OUT* OF THE FULL BUFF, OR AS A BUTTON?**
  Your line was *"And make all the admin buffs 1h and don't want a Shrouding hymn in the full buff. And
  bow expertise."* I read the last sentence as attaching to the **don't want**, and built it that way
  in 0.106.0: both Shrouding Hymn and Bow Expertise are out of the full buff, both still reachable as
  `/buff <name>`.

  **The reading is defensible but it is a reading.** What made me choose it: both were genuinely in the
  set, and both spoil the thing a full buff is for — Shrouding Hymn is party stealth, so a buffed test
  character cannot be attacked unless he starts the fight, and Bow Expertise is inert without a bow, so
  on every other build it is a square on the bar that means nothing. The other reading is that you
  wanted it as a **seventh button** beside the Marks and the greats.

  ⚠ Either way it costs one line to change, and **the thing that is NOT reversible by guessing is your
  intent** — which is why this is written down instead of decided quietly. Say "out" or "button".

- `BL-130` ✅ **BUILT 2026-09-03 (0.107.0) — WHISPS RE-SUMMON ON THEIR REUSE, NOT IN THE LAST FIVE SECONDS.** *"charming whisp (and
  i guess all whisps) resummon on cd not when whisps disapear"*. What is there today is `BL-112`'s
  renewal window applied to whisps: the cast gate refuses a whisp you already carry unless it has
  ≤5s left (`GameConstants.WhispResummonWindowTicks`), so in practice the only moment you may re-call
  one is the moment it is about to leave on its own. You are asking for the plain rule instead — **the
  skill's own 30s reuse is the only limiter**, and a re-call refreshes the whisp in place whenever the
  bar says it is ready.
  ⚠ **It costs 4 Skill Stones every time**, and that does not change. At a 30s reuse a player who
  spams it burns 480 stones an hour per slot instead of 12; that is a choice the price already
  punishes, not something the gate needs to prevent. Say so if you want a floor on it.
  🔑 The refusal lived at the **cast gate**, so removing it costs nothing that was paid — no MP was
  spent and no reuse was started by a refused call.

- `BL-131` ✅ **BUILT 2026-09-03 (0.107.0) — `/buff` GETS A DURATION, AND AN ADMIN BUFF MUST BE ABLE TO REPLACE THE ONE THE FULL
  BUFF GAVE YOU.** Two things in one ask:
  *"/buff command must have duration => /buff [target]<name>[duration][lvl] … /buff wc_harmony_mark 1h
  -> should buff me with buffer 4th mark lvl 2 for 1h … or if we cannot put duration in the command
  atleast make it 1h from /buff and admin buttons"*, and
  *"now im full buff and cannot put war bulwark because of 'something stronger'"*.
  1. **Duration token.** `/buff [target] <name> [duration] [lvl]`, where a duration is `90s` / `30m` /
     `1h`. **The default is 1 hour** for every route — the typed command, the six buttons, the full
     set — which is your fallback ask, and it makes the whole admin buff layer one number.
  2. 🔴 **THE REFUSAL IS A CONSEQUENCE OF `BL-126`, and it is the interesting half.** War Might and War
     Bulwark share a family at the SAME rank, and `ApplyBuff`'s rule for equal rank is *keep whichever
     runs longer*. Since 0.106.0 the full buff hands out its half at **1 hour**, so the button's
     20-minute War Bulwark could never win the comparison — the swap the six buttons exist for
     (`BL-127`) stopped working the day the hour landed. The four Marks are the same shape.
     🔑 **The general lesson: a duration override changes who wins a stacking contest.** Two rules that
     were independent stopped being independent the moment one side's clock was extended.
     **The fix is a FORCE flag on the admin path** — `/buff` and its buttons apply unconditionally,
     evicting whatever shares the family — rather than a duration tweak that would only move the
     problem. A staff command that silently does nothing is worse than one that overrides.

- `BL-132` ✅ **BUILT 2026-09-03 (0.107.0) — A PHYSICAL SKILL'S CAST TIME MUST READ ATTACK SPEED — AND ONLY *DAMAGE* SKILLS DID.**
  *"physical buffs/debuffs/spells should speed up by attack speed not cast … now i cast shield shock
  for ~2s .. when its default cast is 1s and my as is 580 (x1.74) and my cast is 182 (x0.55) …
  physical skills seem to work but the buffs dont"*.
  Your measurement is exactly right and the arithmetic confirms it: Shield Shock's 1s × the CAST
  multiplier 1.83 = **1.83s**, which is the ~2s you saw.
  🔑 **The test in the code was `Category == SkillCategory.Physical`, and `Category` is not the
  physical/magical axis** — it is a five-way role tag (Physical / Magic / Buff / Debuff / Heal). A
  physical STUN is authored `Category.Debuff` and a physical self-buff is `Category.Buff`, so both
  fell to the mage's stat. Only a physical *damage* skill ever took the right branch, which is
  precisely the split you described.
  🔑 **The axis already exists in your CSVs** — the `TYPE` column has said `Physical/Active`,
  `physical debuff`, `pfysical buff` and `Magic/*` all along, and `SkillDef.DebuffSchool` already
  carries the physical/magical word for contested debuffs. What was missing was (a) reading
  `DebuffSchool` in the speed decision and (b) any equivalent for BUFFS. Both land here; the eight
  physical buffs in the files (`sprint`, `evasion_boost`, `bow_expertise`, `wc_bow_expertise`,
  `defensive_wall`, `battle_regeneration`, `battle_presence`, `battle_defence`) get an explicit flag.
  🟢 **`--check` NOW COMPARES THE `TYPE` COLUMN — it never has.** That is exactly how `Charm` could
  disagree with your own file for a whole version without anything going yellow. Only the
  physical/magical WORD is compared (blanks, `Passive`, `Toggle` and `Whisp` are skipped, and every
  spelling of it is accepted — the column has never had a grammar). ⚠ It earned its keep on the first
  run: **Taunt and Mass Taunt** are authored `physical active` and were casting on the mage stat too.
  The check was then proved by planting the Charm break back and watching it report 19 rungs.

- `BL-133` ✅ **BUILT 2026-09-03 (0.107.0) — FIGHTER BASE CAST SPEED 150 → 300, AND THE ELF TANK'S CHARM BECOMES MAGICAL.**
  *"why fighters have so low cast speed ? shouldnt it all have about the 300~400 cast in the begining
  and only mages have the spellcaster_mastery … now my elf figter have 130 base and 182 buffed .. and
  i think he must have 260 (or whatever base x wit mod) and ~365 buffed"*.
  **Your two numbers are exact.** `ClassBaseCastSpeed` is 150 for every non-mage; the elf fighter's
  WIT is 17, so 150 × 0.864 = **130**, and ×1.4 from the buff = **182**. Raising the base to **300**
  gives **259** and **363** — your 260 and ~365, with nothing else touched. Full table:

  | class | WIT | base now | ×witMod = now | cast TIME now | base 300 → | buffed ×1.4 | cast TIME |
  |---|---|---|---|---|---|---|---|
  | Demon Fighter | 10 | 150 | **92** | ×3.62 | **184** | 258 | ×1.81 |
  | Human Fighter | 14 | 150 | **112** | ×2.98 | **224** | 313 | ×1.49 |
  | Elf Fighter | 17 | 150 | **130** | ×2.57 | **259** | 363 | ×1.29 |
  | Demon Mage | 19 | 300 | 286 | ×1.17 | 286 | 400 | ×1.17 |
  | Human Mage | 20 | 333 | 333 | ×1.00 | 333 | 466 | ×1.00 |
  | Elf Mage | 23 | 333 | **386** | ×0.86 | 386 | 540 | ×0.86 |

  🔑 **One correction to your model, and it makes your case stronger:** the elf mage's 386 is NOT
  Spellcaster Mastery — it is 333 × the WIT modifier of a 23-WIT elf. The masteries carry the *wrong*
  -armour and *wrong*-weapon PENALTIES (robe/light/heavy profiles, `CastSpeedPct −0.5`), which is your
  "193 without robe → 96 without wand". So base × witMod is already the model you described; the only
  wrong number in it was the fighter's 150.
  ⚠ **What this actually changes is small**, because `BL-132` moves every physical skill off cast
  speed in the same pass: what is left on a fighter's cast bar is his MAGICAL debuffs — which is the
  whole point of the ask. The one class that gains broadly is the **Warchanter**, a `BaseClass.Fighter`
  whose songs are magic; his casts roughly halve in time. Flag it if that is not wanted.
  - **THE ELF IS A MAGIC KNIGHT — your direction, recorded:** *"the idea is the elf is a magic knight
    and have magic debuffs while the other two rely on phisical - later the elf will have self cure and
    heals he then can invest in wit if he likes"*.
  - **DEBUFF SCHOOLS, per your ruling:** *"charm is a magic taunt not phisical -> charm is saved by
    SPT, Freeze as well, Stay and Shield Shock are the only physical debuffs atm and are saved by CON
    -> the tank 3rd is fixed (2nd charm is still physical active)"*. Freeze is already `Magical` in
    code. **Charm is `Physical` and must become `Magical`** — your `tank 3rd.csv` already says
    `Magical Debuff`, the code and `tank 2nd.csv` did not. Stay and Shield Shock stay `Physical`.
    ❓ **Intimidate (the Demon's fear) is `Physical` today and you did not name it.** "Stay and Shield
    Shock are the ONLY physical debuffs" reads as *fear is magical too*, but a Demon fear is a roar,
    not a spell — and the Demon has no WIT to cast it with. **Left PHYSICAL, flagged here**; one word
    changes it.

- `BL-134` ✅ **BUILT 2026-09-03 (0.107.0) — THE FIGHTER GETS `+WIT −SPT` ON THE MINDWRITER'S SHELF.** *"please add the +wit-spt in
  the skill swap for fighters as well"*. Today `StatSwapsFor` gives a fighter ATK↔AGI, ATK↔CON,
  AGI↔CON and the one-way `+SPT −ATK`; WIT is not on his shelf at all, which is what makes an elf tank
  unable to buy into his own magic debuffs. Added as `swap_wit_men` (+WIT −SPT).
  ⚠ **The reverse (`+SPT −WIT`) is NOT added** — you named one direction, and the fighter's other WIT
  door is already one-way by your own 2026-08-10 ruling. Reset at the Mindwriter is free, so nothing
  is trapped by leaving it one-way. Say the word and it is one line.

- `BL-135` ✅ **FIXED 2026-09-03 (0.107.0) — CANCELLING A CAST BY PRESSING ATTACK IS FREE — THE COOLDOWN NEVER STARTS.** *"starting
  to cast a skill … and click on attack that is on the skill bar it cancels the cast of the skill and
  dont enter it in cooldown .. while if i cast and cancel it trough same button 'X' and it start to
  cooldown"*. Confirmed in the code: `HandleAttack` calls `CancelCast(attacker)` and the parameter
  `startCooldown` defaults to **false**, so an attack order is a free cancel while the cast bar's X and
  ESC both pay the reuse. That is an exploit shape, not only an inconsistency — any long cast can be
  aborted at no cost by tapping Attack. **One rule: a cancel the PLAYER chose starts the cooldown; only
  an enemy interrupt or a forced stop does not.**

- `BL-136` ✅ **BUILT 2026-09-03 (0.107.0) — CHAT AND COMBAT DO NOT COUNT AS OPEN WINDOWS FOR THE BACK BUTTON.** *"also can chat and
  combat window not to count as opened windows for the back button"*. Both go through `ToggleWindow` →
  `OpenWindow`, which is what registers a panel on the back stack, so leaving the chat log open means
  every back press closes it instead of what you actually wanted closed. They are **persistent HUD**,
  not modal windows. Exempted by name; everything else on the stack is unchanged.

- `BL-137` 🔴🔴 **CORRECTED 2026-09-03, SAME DAY — I WAS WRONG, AND THE WHOLE ENTRY ABOVE IS BUILT ON A
  FALSE PREMISE. READ THIS INSTEAD.**
  You wrote *"a lvl 72 redhorn footman have 12561"* and **I read it as an IG creature.** It is not.
  **`redhorn_footman` is OURS** — `MobCatalog.cs`, "Redhorn Footman", level 72 — and so is the
  `cursed_blade` you measured next. You were telling me what OUR game does, and I answered as though
  you were quoting theirs.
  🔑 **THE ×3 IS THE ZONE LADDER, and it has been there since 0.94.0 — I built it, on your own ruling.**
  `WorldPlan.HpScaleFor(level)`: **×1 below 40, ×2 from 40, ×3 from 61** *(the ladder AS IT WAS THAT
  MORNING — you re-ruled it the same day, see `BL-148`)*, applied through
  `SpawnZone.HpScale` → `Entity.MobZoneHpScale`. It came from `BL-78` item 1, your words of
  2026-08-27: *"the 15k mobs are zone placed with x2/x3 hp .. some zones can have x1"*. So the
  arithmetic that "confirmed our curve against IG" was our own multiplier agreeing with itself.

  | mob | level | base `40+0.8·L²` | zone | shown |
  |---|---|---|---|---|
  | Cursed Blade | 61 | 3,016 | ×3 | **9,048** — your number exactly |
  | Redhorn Footman | 72 | 4,187 | ×3 | **12,561** — your number exactly |
  | an ELITE at 84 | 84 | 5,684 | ×3 **× rank ×4** | **68,208** — your ~68k |

  ⚠ **AND IT IS INVISIBLE, which is the fair half of your complaint:** *"in its info panel there is
  nowhere x3 and no passive in skills tab"*. Correct — the inspect plate lists `MobMod` passives
  (`MobMod.Describe`), and the zone multiplier is **not a MobMod**; it is a field property that no
  panel prints. A creature carrying triple HP with nothing on it saying so is a plate that lies by
  omission. **That is a bug and it is `BL-148` below**, together with your revision of the ladder.
  🔑 **The lesson, and it is mine:** *"a lvl 72 X has N"* is a measurement of SOMETHING, and which
  game it measures decides everything that follows. **Ask, or check the id, before building an
  argument on it** — `grep MobCatalog` would have cost one command and saved a wrong ruling.

- `BL-137`.old 🔴 **WRONG — SUPERSEDED THE SAME DAY BY THE CORRECTED `BL-137` FURTHER DOWN. Do not act on anything in this entry.** The premise is false: the creature is OURS, not IG's, and the x3 is our own zone ladder.

  The original text follows, kept only as the record of the mistake.

- `BL-137`.old (the wrong text) 🟢 ~~YOUR LEVEL-72 MOB SETTLES THE 15k QUESTION — AND IT VALIDATES OUR CURVE EXACTLY.~~
  *"also about the 15k on mobs .. a lvl 72 redhorn footman have 12561"*.
  Our base curve is `MobBaseStats.Hp(L) = 40 + 0.8·L²`, so **Hp(72) = 4,187**. And 4,187 **× 3 =
  12,561** — your number to the unit.
  🔑 **That is not a coincidence and it is not a curve error: it is an IG `HP Increase (x3)` tag.**
  `balance/MobCurveVsIG.md` measured this across 2,831 creatures — 77% are ×1 and 23% carry ×2-×5 —
  and it is why `BL-78`'s HP half was ruled *"stays as is"*: **our base equals their base, and the big
  numbers are bought by the multiplier layer** (`MobMod.Hp`), which exists, works, and is *unauthored*
  on the field roster. So the thing still owed is **authoring ×2-×5 onto the creatures that should
  carry it**, not moving the curve. A ×3 on our level-72 roster produces 12,561, the same as theirs.
  ⚠ This supersedes nothing and reverses nothing — it is the first outside data point that confirms
  the base, which is worth more than the ruling it agrees with.

  🔴 **YOU THEN WENT LOOKING FOR IT AND FOUND NOTHING — and that IS the answer, not a second bug:**
  *"about the mob healt .. i dont see in skills x3 hp on mobs passive ... its somewhere invisible"*.
  **The display works and the layer is real; the ROSTER is empty.** `MobMod.Hp != 1` already prints
  `Max HP ×3` in the target-inspect passive list (`MobMod.Describe`, beside "Wields:" and the resist
  lines), and **exactly four templates in the whole game carry an HP multiplier**: the guard tower
  (×2) and three deliberate demo/boss creatures (×3.73, ×1.46, ×1.46). **Not one ordinary field
  creature has one.** So nothing is hidden — there is nothing to see.
  🔑 **That makes the owed work concrete and small.** It is AUTHORING, one `MobMod.Hp` per template;
  the machinery to apply it, roll drops against it and show it on the plate is built and tested.
  Say the word and it becomes its own entry with the roster laid out — which creatures stay ×1 and
  which take ×2-×5 — for you to rule on. IG runs 77% at ×1, so most of the bestiary would not move.

- `BL-138` ✅ **BUILT 2026-09-03 (0.107.0) — THE LEARN TAB: THE ROW *IS* THE LEARN BUTTON, AND THE CONFIRM MOVES INSIDE.** *"clicking
  on the row in skills to learn tab not to open the details but the learn details now if i missclick it
  opens the details and is annoying .. u can remove the learn button and the actual row click is the
  learn click and inside the learn details to be a confirm button that is grayed out when unable to
  learn"*. Three changes to one window: the row's tap opens the **learn** view rather than the skill
  card, the separate [Learn] button on the row goes away, and the learn view carries the confirm —
  **greyed out, not hidden, when the skill cannot be learned**, so the reason is visible instead of the
  button being missing.

- `BL-139` ✅ **FIXED 2026-09-03 (0.107.0) — SHIELD REINFORCEMENT IS NOT A TOGGLE — IT WAS NEVER DECLARED AS ONE.** *"tanks Shield
  Reinforcement dont work .. dont activate - it not act as a toggle at all .. it casts something but
  doesnt do nothing (seems to act as a buff of instacast/0 duration) - for a split second i see my pdef
  rises"*. Your diagnosis is the bug, exactly: it is `Category: SkillCategory.Buff` with
  `MpPerSecond: 15` and **no `Toggle: true`**, so `ApplyBuff` reads its `DurationTicks` of 0 and the
  buff lands and expires on the same tick. The +300 P.Def you glimpsed was real, for one tick.
  🔑 **Your `tank 3rd.csv` row says `Toggle` in the TYPE column and always has** — this is the second
  disagreement in one day between that column and the code, after `Charm`. So the check that landed
  this morning for `BL-132` grows the other half: **`--check` now also compares the word `Toggle`**,
  and a stance that forgets the flag is a yellow line instead of a skill that quietly does nothing.

- `BL-140` ✅ **BUILT 2026-09-03 (0.107.0) — ENCHANT AND ATTRIBUTE FROM THE ITEM'S OWN DETAILS — the flow runs the wrong way round.**
  *"can we make enchant button on the actual equipment details .. i open details of a weapon and click
  Enchant it ask me which scroll if i have any and enchant .. now the reverse is a bit harsh -> find
  scroll click _. click use -> find weapon from 250 equipments -> click"* … *"also attribute scrolls
  the same way"*.
  You are right about which end is the long one: **you have few scrolls and many items.** Picking the
  scroll first means the second list is your whole bag; picking the item first means the second list is
  the two or three scrolls that can legally touch it.
  🔑 **BOTH DIRECTIONS STAY.** The scroll-first flow is the right one when you have just looted a
  scroll and want to know what it is for, and deleting it would break the Bin/Use shape every other
  consumable has. The new buttons are on the WEARABLE's detail panel, offered only when you actually
  hold a scroll that would be accepted — a button that opens an empty list is worse than no button.
  ⚠ The eligibility test is the same `ScrollCanTarget` the existing flow uses, read backwards, so the
  two directions can never disagree about what is legal — and the server stays the authority.

- `BL-141` ✅ **FIXED 2026-09-03 (0.107.0) — AN ATTRIBUTE LANDS AND THE ITEM'S DETAILS NEVER REDRAW.** *"attri scrlls dont update the
  weapon details after added -> i add attribute and the only way to see what have been added is to open
  the attribute weapon selection again"*.
  Two causes, both fixed: the detail window **closes itself** after a scroll (so there is nothing left
  to update), and nothing re-rendered it on a bag push anyway — the panel drew once from the DTO it was
  opened with and kept that copy forever.
  🔑 It now remembers WHICH INSTANCE it is showing and re-renders it from every inventory push, closing
  only if that item has actually left your bag. So an enchant, an attribute, an equip, a stack change —
  anything the server sends — is on screen the moment it lands. That is also what makes `BL-140` worth
  having: you enchant from the item's own page and watch the number move on it.
  ⚠ The detail panel keeps its OWN change-stamp and that stamp includes the ATTRIBUTES, which is the
  whole trick: a re-roll changes nothing else about an item — same instance, same enchant, same
  quantity — so a stamp built the way the BAG's is would have been identical and it still would not
  have redrawn. (The bag's own stamp is left alone deliberately: its rows do not print attributes, so
  it has nothing to redraw.)

- `BL-142` ✅ **BUILT 2026-09-03 (0.107.0) — THE `[ORDER]` SETTING PERSISTS ON THE PHONE.** *"can the order of bag and/or vendors be
  made persistent for the client (same as the chat windows)"*. `BL-117` shipped it deliberately
  unpersisted — *"a view preference, resetting to A-Z"* — because there was no settings message to
  carry it. There does not need to be one: it is a CLIENT preference, so it goes in `PlayerPrefs`
  beside the camera distance and the models toggle, and never touches the server or the character.
  ⚠ One thing fixed along the way: the five `[ORDER]` buttons (bag, vendor sell, vendor buy, buyback,
  warehouse) share one setting but each painted its own label at build time, so changing the order in
  the bag left the vendor's button reading the old word until that window was rebuilt. They now all
  relabel together.

- `BL-143` ✅ **BUILT 2026-09-03 (0.107.0) — BACKLASH MOVES TO THE 4TH CLASS (76) AND GETS ITS CSV ROW.** *"can you move backlash from
  the tank 3rd to tank 4th in the csv not that matters now but its not authored and not seeing it in the
  csv but in the game is a class mismatch"*.
  🔑 **It was never in the tank 3rd file — it was never in ANY file**, which is the whole defect: it is
  auto-granted from `SkillCatalog.ReflectPassiveFor`, and the level was **mine, not yours**. The code
  comment has said so since `BL-08`: *"⚠ THE LEVEL IS MINE, NOT HIS: he never said when a tank gets it.
  It is granted at the 3rd class change (40) to sit beside Deflection, which he DID date."* You have now
  dated it, so the invention is retired: **76**, and a row in `tank 4th.csv` at SP 0, which is what
  auto-granted looks like to `--check`.
  ⚠ **A tank already carrying it keeps it unless it is stripped**, because the grant is a plain
  assignment into `LearnedSkills` and nothing has ever taken one back. Any 40-75 tank on your `game.db`
  would have walked around with a skill the new rule says he cannot have, so the grant now also REMOVES
  the archetype's reflect passive below its gate — the same shape as the `class_balance` cleanup beside it.
  ⚠ The warrior's **Deflection** is untouched: you dated that one yourself (*"default warrior @40 -> 0.15
  chance x1 reflected; @76 -> 0.3"*), so it keeps Lv1 at 40 and Lv2 at 76.

- `BL-144` ✅ **BUILT 2026-09-03 (0.107.0) — SKILL STONES STACK TO 9,999; THE ELEMENTAL/HOLY/PHYSICAL STONES STAY AT 99.** *"skill
  stones to stack to 9999 while the element type stones to stay at 99 -> skill stones are used for fast
  reuse casts like heals etc .. and 99 are not near enough to have"*.
  🔑 **The line you drew is SPEND RATE, not what the item is**, and it is the right one: a Skill Stone is
  the reagent of ordinary, repeated casts — Ultimate Heal takes one or two per cast, a whisp four per
  call — so a raid evening burns hundreds and 99 is under an hour. The other three are set-piece
  reagents spent in ones, and they keep the 99.
  ⚠ It is the **first user of `ItemDef.MaxStackOverride`**, a field written for exactly this in 0.93.0
  and unused until today (*"only set this when one item has to disagree with its whole category"*). The
  number still lives in `StackLimits` as a named constant, so a retune is one edit — your own standing
  requirement for the stack system. Nothing else in the Consumable category moves.

- `BL-145` ✅ **FIXED 2026-09-03 (0.108.0) — AN HOUR-LONG SCROLL WAS HIDING IN THE RUNE ROW. IT WAS ALWAYS
  IN THE COUNT.** *"scroll/potion buffs and swift should count towards the buff limit.. now i have 2
  scrolls 16npc buffs + focus ferocity scrolls and the 2 scrolls are in the warrune bar"*.
  🔴 **HALF OF THIS ENTRY WAS WRONG WHEN I WROTE IT — my error, and it is the second in two days after
  `BL-137`.** Scroll and potion buffs, Swift among them, have ALWAYS counted against the twenty: the
  wrapper hands out the family's rung, the rung carries `CountsTowardBuffLimit: true`, and the wrapper's
  row is `BuffRow.Consumable`, which `CountsAgainstBuffCap` counts. `BL-147`'s generated page now proves
  it in a column — Swift, Focus and Ferocity all read **Slot: yes**. The claim above that the War Rune
  bar is `BuffRow.Item` was simply false: **every rune buff in the game is authored `Consumable`** (and
  exempted by its own flag), which is exactly why a scroll landed beside one. 🔑 **The lesson is the
  same one `BL-137` cost: a number or a field you did not READ is a guess.**
  🟢 **What WAS broken is the ROW, and the code contradicted its own doc comment.** That comment has said
  *"COUNTS AGAINST THE CAP IS THE FIRST TEST"* since `BL-111`; the code underneath tested `Item` and
  `Consumable` first, so the top bar was not the counted set. Now it is: everything spending a slot is in
  the top bar, and `Item`/`Consumable` hold only the free riders — runes, healing and mana potions, Dash,
  the toggles. A potion of healing still has its own bar (your playtest-27 ask); a blessing that came out
  of a scroll no longer hides in it.

- `BL-146` ✅ **BUILT 2026-09-03 (0.108.0) — THE `n/20` COUNT MOVES ONTO THE HIDE BUTTON, AND EVERY BAR
  GETS ITS OWN.** *"the x/20 text is invisible make the hid button show count (if possible over 15 yellow
  over 18 red) also i want each buff bar to have its own hide button"*. `BL-111` drew it as a bare 12pt
  label on the world layer with nothing behind it; the button beside it is a filled box big enough to
  read on a phone, so the number moved onto it. Your thresholds verbatim: **>15 yellow, >18 red** (red
  also covers the cap, where "20/20" and "19/20" are one glyph apart). Four buttons now, one per
  collapsible row, each with its own three-stage collapse. Debuffs still have none and are never hidden.
  ⚠ A hidden group keeps the one line its button sits on — four buttons stacked on the same y would be
  unclickable, and each has to stay in front of the bar it belongs to.

- `BL-147` ✅ **BUILT 2026-09-03 (0.108.0) — THE CONSUMABLE-BUFF INVENTORY, GENERATED →
  [`data/BuffConsumables.md`](data/BuffConsumables.md).** *"can u show me what buffs we have as scrolls
  and what on potions (which are bought which are crafted and which are same as npc buffer) and which we
  dont have that are single buffs"*. Written by
  `dotnet run --project tools/BalanceMatrix -- --buff-consumables`: **20 families with a consumable, 52
  without, 48 items.**
  🔑 **THE ANSWER TO YOUR LAST QUESTION — ladder families with no potion and no scroll: Clarity,
  Fortitude, Resolve, Shield Blessing, Shield Hardening, Vampirism.** Buffer-or-nothing.
  🔑 Generated because the interesting half is an **absence**: "which buffs have no potion" is wrong the
  day someone adds the missing bottle, and nobody re-reads a typed page. Every column is a query,
  including the slot column, which is the server's own `CountsAgainstBuffCap`.
  ⚠ Two traps the first draft fell into, both worth keeping: **a consumable buff has TWO shapes** (a
  Might Potion is a one-child wrapper; a healing potion IS the buff), and testing only the first listed
  `potion_heal` under "has no consumable" — the exact opposite of the truth. And **a toggle is not a
  ladder rung** despite having no duration, no MP and no cast, which filed Holy Soul as unreachable.

- `BL-148` ✅ **BUILT 2026-09-03 (0.108.0) — THE ZONE HP LADDER, RE-RULED, AND THE PLATE NOW SAYS SO.**
  Your ruling: *"Zone laddre x1<40, x1.5<76, x2<83, x3 84+, elits still have their x4 everywhere so
  x4<40, x6<76, x8<83, x12 84+ (futer tests will alter it probably..)"*

  | level | zone | elite (zone × rank ×4) | a field mob's TTK, was → is |
  |---|---|---|---|
  | < 40 | ×1 | ×4 | unchanged |
  | 40-75 | **×1.5** | ×6 | 61: 39s → **19s** · 72: 66s → **33s** |
  | 76-83 | ×2 | ×8 | 80: 46s → **31s** |
  | 84+ | ×3 | ×12 | 55s, unchanged |

  🔑 Your second list is the **composed** number, not a second knob: ×1.5 × 4 = ×6, ×2 × 4 = ×8,
  ×3 × 4 = ×12. `MobRankScale.Hp(Elite)` stays ×4 flat and was not touched, so **the 84+ elite keeps its
  68,208** — deliberate, and it is the number you opened the entry complaining about.
  ⚠ **LEVEL 83 IS MINE, NOT YOURS.** Your bands read `x2<83` and `x3 84+`, which leaves 83 unnamed; it is
  filed under ×2 so `x3 84+` is literally true. One line to move if you meant otherwise.
  ⚠ It multiplies HP and nothing else, so **lowering a rung raises farm rate**: the same EXP and drops
  now come out of levels 40-75 in half the time. Flagged, not absorbed.
  🟢 **And the plate prints them** — *"in its info panel there is nowhere x3 and no passive in skills
  tab"*. Correct, and plainly a bug: the two biggest terms in a creature's pool are entity FIELDS
  (`MobZoneHpScale`, `MobHpScale`), not `MobMod` passives, so `MobMod.Describe` could never see them.
  Two lines, **never pre-multiplied** — "×12" tells you nothing about which knob to turn. A boss is
  exempt from the zone ladder, so its zone line is not drawn.
  📐 Measured, not derived: **`dotnet run --project tools/BalanceMatrix -- --zonehp`**, new today.

- `BL-149` ✅ **BUILT — VAMPIRISM AND RESOLVE GET A SCROLL, AND THE BOX GOES 17 → 19.** *"vamp and
  resolve can be made as scrolls as well and add to boxes. Buffers/healers have resists, shield, great
  might/bulwark buffs"*.
  🔑 **They were the only two NPC blessings with no consumable anywhere in the game** — `BL-147`'s page
  is what surfaced it, and it mattered because `BL-150` stops the buffer at 75: without this, both
  would simply have vanished above 75 for anyone without a Warchanter.
  🔑 Your reason for being comfortable with it is the right one and it is now written into the code:
  the buffer class keeps **Clarity, Fortitude, Shield Blessing, Shield Hardening** and the Great
  Might/Bulwark layer — after this change those are the **only four families left with no consumable**,
  which the regenerated page proves in its own section 2.
  ⚠ **One scroll each, not a trio**, and at rungs **5 and 7, not 6** — a scroll takes its family's TOP
  rung, and those two ladders are not six deep. Craftable at Scribe L5 like the other scroll-only
  families; still **pick 10**, so the box got wider, not more generous.

- `BL-150` ✅ **BUILT — THE NPC BUFFER REWORKED: TWO TIERS, 19 BLESSINGS, NO [FULL BUFF], ENDS AT 75.**
  *"i would like npc to give fury/alacrity/force/mght/bulwark/swift/vamp/resolve from 6+,
  body,soul,vigor,serenity,agility,aim,ward,frenzy 40+"*, *"add and the focus,ferocity,insight to the
  npc 40+ as well"*, *"remove [full buff] from buffer ... only the two fighter and mage sets that i
  give you and they do not change"*.

  | tier | when | cost each | what |
  |---|---|---|---|
  | **free eight** | from **6** | **0** | Fury, Alacrity, Force, Might, Bulwark, Swift, Vampirism, Resolve |
  | **paid eleven** | from **40** | **15,000** | Body, Soul, Vigor, Serenity, Agility, Aim, Ward, Frenzy, Focus, Ferocity, Insight |
  | *above 75* | — | — | the buffer refuses; the Blessing Box, a Scribe or a real buffer takes over |

  🔑 **THE FREE/PAID LINE IS THE BUFF, NOT THE PLAYER.** That is the reversal, and it is the opposite
  of the old rule ("everyone free below 75, everyone pays above"). A level-74 character still pays
  nothing for Might and 15,000 for Aim; neither answer depends on who is asking.
  🔑 **Your two presets ARE the free eight, partitioned** — Fighter (might, bulwark, vamp, fury, swift)
  ∪ Mage (alacrity, force, bulwark, resolve) = exactly the eight, with Bulwark the buff both roles
  want. So *"you buff fighter+mage sets and buy all 40+ then save your own"* works: two free presses
  fill a levelling bar, and there is no longer one press that takes all nineteen.
  🔑 **The level gate is applied when a preset is EXPANDED**, which is what makes your saved-preset rule
  need no new state: *"if some1 buff me with body or soul and i save it and im <40lvl they will not
  activate .. they will activate after 40+"*. The id stays saved and starts landing on its own at 40.
  ⚠ **Price doubled 7,500 → 15,000** (`BuffCostPerLevel` 1,500 → 3,000), your arithmetic. But **a full
  set is now 165,000, not the 120,000 you calculated** — that sum was 8 paid, and you added Focus,
  Ferocity and Insight to the paid tier in the same message. Flagged, not absorbed.
  ⚠ **NINETEEN AGAINST A CAP OF TWENTY** — the exact state playtest 28 trimmed the set from 19 down to
  11 to escape. Deliberate this time: a real buffer's groups evict 18 of the 19 into 5 squares, so the
  squeeze is only felt buffing SOLO, where the only competition is your own class self-buffs. **If it
  bites, the cap moves, not the list.**
  ⚠ **THE RESTORE PRICE IS MINE, NOT YOURS.** You priced the buffs and said nothing about HP/MP
  restore; its old threshold was "free at or below 75", which the new 75 ceiling would have made free
  forever. Aligned to the paid tier instead: free below 40, priced 40-75. One constant to move.

- `BL-151` ✅ **BUILT — THE BLESSING BOX IS 300k.** *"Buff box price 250-> 300k twice as the cost per
  buff from npc but it gives you outside town buffs"*. 300,000 ÷ 10 picks = **30,000 a blessing-hour,
  exactly twice** the NPC's 15,000, and the price is now derived from those two numbers in a comment
  rather than picked — so a change to either is visibly a change to both. ⚠ The divisor is `PickCount`,
  which `BL-149` deliberately left at 10 while widening the box to 19 options.

- `BL-152` ✅ **BUILT — DASH POTIONS DROP ONLY TO UNCOMMON.** *"dash pots to drop to uncommon ... all
  else from crafters"*. Greater, Superior and Grand left the drop tables (Supreme was already
  craft-only); all six rungs remain craftable, so nothing became unobtainable.
  🔑 It finishes a rule two earlier passes started — playtest-17 `E3` removed the scrolls, playtest 28
  cut the stat potions to three speed families, and **both times Dash was written down as the
  deliberate exception**. "The top of a ladder is bought, not found" is now true without a footnote.
  ⚠ Unlike those two, this one **narrows** the faucet rather than concentrating it: the three removed
  ids were the whole of rungs 3-5, so there is nothing to redistribute their weight onto.

- `BL-153` ✅ **BUILT — EVERY RUNE IS MYTHIC.** *"make war/spell runes mythic grade (all others as well
  if they have no Levels but still SP rune 10 is different from SP rune 100)"*, then, the same day,
  answering the open question below: *"all runes if they can be same rarity at mythic and SP/EXP/etc
  runes just be same rarity at mythic"*.
  🔑 **First reading was a test, and the test was wrong.** The first pass took "SP rune 10 is different
  from SP rune 100" to mean *rarity* tells the rungs apart, so it made only the level-less runes Mythic
  (War, Spell, Sinister, Sinners) and left the 55 laddered reward runes on Epic. Your answer says the
  rung and the NAME carry that difference, not the colour of the line. So: **`EquipSlot.Rune` ⇒
  `ItemRarity.Mythic`, no exception** — all 59 of them. `RewardRune` no longer takes a rarity at all,
  and `ItemCatalog.ValidateRunes` now refuses to boot on a rune that is not Mythic, so the next rune
  authored by copying a neighbour cannot quietly break the rule.
  ⚠ **Display and sort only.** Rarity does feed crafting recipes, salvage and the shop ladder, but all
  three gate on `ItemLevel > 0` and a gear slot first, and a rune has ItemLevel 0; rune prices are
  pinned by `BuyPriceOverride: -1` / `SellPriceOverride: 0` / `Value: 0`, so `RarityPriceMul` never
  runs on one. Nothing in the economy moved.
  ❓ **One item deliberately left alone — say the word and it changes.** The **Rune of Tincture** (the
  title-colour item) is `EquipSlot.Consumable`, not a rune, and carries a real `Value: 40000` — making
  it Mythic would raise its vendor price, which is an economy change you did not ask for. It keeps
  Uncommon. Your "all runes" may well have meant it too; it is a one-line change either way.


---

### The narrative that used to run between the entries

Verbatim, in the order it stood in the file. Only the heading LEVELS were demoted so it nests
under this section; not a word of the text was changed.

#### The history that used to head the file — how it was assembled, and the passes that fed it

Assembled 2026-08-12 from playtests 4-21, `Open-Checklist.md`, `Playtest-Archive.md`,
`Roadmap.md` / `RoadmapNext.md` and the design docs. Everything shipped up to `ed75bac`
(0.60.1 + the playtest-21 batch) has been checked out of it.

**Playtest 22 (2026-08-13) added `BL-65` … `BL-72`** — dungeon level bands, an item-id reference,
the `MpHeal` type, more 16-40 zones, invisibility ×3, mob social clans, the aggro/taunt model, and
unbuffed farm survivability. His bug finds from the same pass went to `testing/Open-Checklist.md`,
not here.

**2026-08-14: he ruled on ALL EIGHT remaining 🔴 items in one message, and all eight are BUILT**
(0.66.0) — `BL-20` · `BL-22` · `BL-27` · `BL-34` · `BL-35` · `BL-36` · `BL-42` · `BL-59`. Two of them
left something behind that is his to answer, and both are flagged on their own entries below:
**`BL-22`'s farm budget cannot be reached at S** by any tuning, and **`BL-34`'s 76+ buff expansion**
was not re-ruled.

⚠ That message covered the eight he was SHOWN, not every 🔴 in the file. Five were still ready to
build and simply unqueued; he ruled on four of them on **2026-08-14**, and **three shipped as 0.67.0** —
`BL-43` (target cycling, retaliate-first) and `BL-46` (treat the app as a game; the second icon is
gone) are **built and deleted**, and `BL-49`'s boss-EXP half was built while its levelling-curve half
stayed open — until **2026-08-26, when he closed it with *"leave it"*** (see the bottom of this file).
`BL-45` (the presentation pass) is **his own "separate discussion later on"**, and its VISUAL half is
now `BL-93`, which he asked to talk about on the same day.

**⏸ CRAFTING IS PARKED, on his instruction (2026-08-14):** *"leave the salvage/mats etc craft until
I'm able to test it fully — need to increase the drop rate and exp by 100 so I can make chars
different professions to farm to see who can craft what — and it's a single playtest only for this."*
So `BL-05` (the two unruled crafting pieces), `BL-22`'s unreachable S budget and `BL-50` (the boss mat
pile vs the party loot rule) are **not to be worked on or re-raised** until he opens that playtest.
Nothing about them is blocked or broken — they are waiting on a test only he can run.

**Playtest 23 (2026-08-15) added `BL-73` and `BL-74`** — mob social clans back on once the world map
spreads the camps out, and the Game-Launcher research. Everything else he found that pass was either a
**bug** (they went to `testing/Open-Checklist.md` and are built as 0.68.0) or a **ruling on something
already built**, which by rule 1 rewrote the thing in place rather than opening an entry here.

**2026-08-15, after that pass, added `BL-76`** — boss skill gems in three rarities. New design, not a
playtest find; queued 🔴 with its numbers explicitly marked as yours to alter later.

**Playtest 24 (2026-08-16) added `BL-77`** — the PvP flag as the input to every AOE and no-damage skill's
target filter — and it was **BUILT the same day in 0.69.0**, together with both of its bug finds (reflect
flagging the defender; the System chat tab lagging), which live in `testing/Open-Checklist.md` §87. The
pass also **answered `BL-47`/`G3` §8-B**: *migrate*, and it named three levers the design doc never swept
(enchant, race as the main-stat carrier, a ×2 elite passive) — see that entry, whose step 2 is the only
thing playtest 24 produced that is not yet built.

**Playtest 25 (2026-08-16/17) added `BL-78` … `BL-83`** — the mob HP curve and the IG comparison he asked
for, two brand-new uses for player-built mobs (**town guards** and **fortress sieges**, both his own
design), god-mode debuff immunity plus the boss debuff rule beside it, the admin/stealth visibility flag,
and **taunt removed from the auto chain**. It also **answered `BL-13`** — a boss is **10-30 minutes**, so
the target rises rather than the late bosses coming down — and delivered the verdict on `BL-47` step 2:
the machinery works and the rune is indistinguishable from the passive, **but the design as it stands
loses the global curve lever**. Every built row in that pass came back green; the UI polish it asked for
lived in `testing/Open-Checklist.md` §89 as `BL-88`.

**✅ ALL SIX OF THOSE ARE NOW CLOSED.** `BL-78`'s two halves shipped in 0.73.0; `BL-82` in 0.80.0; and
**`BL-13`, `BL-81`, `BL-83` and `BL-88` were built together as 0.89.0** on 2026-08-26, the same day he
answered `BL-47` (*yes — field mobs stay on the curve, player mobs are a hand-placed content tool*).
What is left of that pass is `BL-79` and `BL-80`, which are the CONTENT his `BL-47` answer unblocks.

**🆕 2026-08-27: he ruled on FOURTEEN entries in one message, and NINE of them closed.** Deleted:
`BL-10` · `BL-12` · `BL-16` · `BL-17` · `BL-24` · `BL-54` · `BL-55` · `BL-86` · `BL-94`. Rewritten:
`BL-15` (learnable passives, gated on the warrior/rogue CSVs) and `BL-23` (an assertion replaced by a
measurement — `--goldflow`). Partly closed: `BL-90`, `BL-91` and `BL-92`'s demon-buffer bullet. All the
replaced text is in [BacklogArchive.md](BacklogArchive.md).

⚠ 🔑 **THREE OF THE FOURTEEN WERE STALE, NOT OPEN** — `BL-90`'s bursts, `BL-91`'s ×2 and `BL-92`'s demon
buffer were all already in the code, two of them since 0.87.0 three days earlier, and he was the one
who noticed (*"bl-24 - it build ? why blue ?"*, *"nuker 3rd is build or atelast should be so fix the
wording"*). **When a build closes a dependency, sweep every entry that named it in the same commit.**
A stale 🔴 costs more than a missing one: it invites work that is already done.

#### The "★ the ones you have named most recently" preamble

##### ★ The ones you have named most recently

Three of the original five are **built and deleted** (2026-08-12): `BL-01` the premium reward runes,
`BL-03` the Stat-Swap tab and `BL-04` the auto buff potion/scroll tab — the last two took `BL-39`
(the Mindwriter's misleading `(cost …)`) out with them. See `CHANGELOG.md`. Two are left, and one is
brand new.

**🆕 2026-08-26, in one message:** `BL-47` answered **yes** and closed · `BL-49` ruled *"leave it"* and
closed · **`BL-93` opened** for the in-game visuals discussion you asked for (*"models/terain etc."*)
· and `BL-13` + `BL-81` + `BL-83` + `BL-88` were built as **0.89.0**, so they are gone from this file.

#### Playtest 29 (2026-09-01) — the pass preamble

##### Playtest 29 — your pass of 2026-09-01. `BL-108` … `BL-121`

Twenty-two finds, written into `testing/Open-Checklist.md` §0. The **bugs** stay there, which is where
bugs live; the **changes and new systems** are the entries below. Your two `[?]` questions are
answered at the bottom of this section rather than as entries — neither asks for a build.

🔴 **THIS PASS IS NOT CLOSED, and I said it was.** 2026-09-03: *"I don't think playtest 29 is closed
... where is the npc admin buffer? the free class change is there but the idea about the buffer?"* —
and also *"the admin menu rework of functions and class tabs?"*. Both are correct: they are
`BL-126` and `BL-127` below. **Neither was ever written into any file**, so neither was built, and a
sweep that only reads the written record — which is what I did before answering — reports the pass
closed and is wrong. Everything with a `BL-nn` from this pass really is built; what is missing is
what never got one.

#### Playtest 29 — your two `[?]` questions, answered, and where its bugs went

##### Your two `[?]` questions — answered, nothing to build

**"Shouldn't mobs have normal hp? Why did the curve move?" — IT DID NOT MOVE.** `MobBaseStats.Hp` is
still `40 + 0.8·L²` and its last change was `d93f9ed`, **2026-07-14**. The 0.73.0 `BL-78` refit moved
P.Def / M.Def / P.Atk / M.Atk and left HP alone deliberately — your ruling. That curve reads **1,320 at
40 · 5,160 at 80 · 5,820 at 85**; nothing on the roster is 15k off it.

🔑 **What reads ~15k is a GUARD, and guards were never on the mob curve.** They are `PlayerBuilt`, so
their HP comes from `StatCalculator.MaxHp` — the **player** curve, the one that doubled in **0.91.0
(2026-08-27)** — and the Field pair is level **90** with a ×2 tower passive on top. Your very next find
is that guards are hittable, so that is almost certainly what you were swinging at. The only other big
numbers on the roster are `demo_lich` (×3.73, a deliberate demo) and Elite rank (×4). If it was an
ordinary creature, name it and it gets measured.

**"Should vendor prices scale with the rates?" — NO, and the distortion you spotted is not uniform.**
Multiplying income *and* prices by the same N is a no-op with extra digits. But the real asymmetry is
this: **rates multiply per-KILL income, while consumables are spent per-FIGHT.** At ×100 you get 100×
the gold per pig and still drink one potion per pig, so potions fall from a cost to free — `--goldflow`
already measured them at **0-3% of income at ×1**. Gear is the opposite: you also get 100× the *drops*,
so you buy **less** gear, and raising its price would make the vendor more relevant, not less. One
global multiplier is therefore the wrong instrument. And ×100 is a **test-server** setting — anything
tuned against it must be untuned for the ×1 game that ships. If you still want the test server to feel
honest, `BL-121` is the cheap honest version, and I would set it near **√rate** (×10 at ×100).

##### The bugs from this pass stay in the checklist

`testing/Open-Checklist.md` §0 holds the ones that are pure bugs and need no ruling: the ortho
zoom-out grey clip, the NPC buffer's dead `[Save]`, the harmonist not learning
serenity/vigor/vamp/force/insight, Bow Expertise surviving a weapon swap, toggles flickering under
auto-on, the level-1 mage casting with no weapon-proficiency penalty until something forces a
recompute, Phase Shift not updating your position on the client, and the stale buff bar after a long
reconnect.

#### Your pass of 2026-09-03 (in chat) — the preamble

##### Your pass of 2026-09-03 (in chat, not a playtest). `BL-130` … `BL-138`

Nine items, from one message plus three follow-ups. **Your order is the order below** — the things
that are wrong first, then the two design changes (cast speed, the fighter's WIT swap), then the UI
asks, and the mob-HP measurement last because it needs nothing built.

#### The buffer economy (`BL-149` … `BL-153`) — the preamble

##### The buffer economy — `BL-149` … `BL-153` (0.109.0, 2026-09-03)

Your rulings from the same chat pass, after reading `BL-147`'s generated page. The shape you were
building toward: *"those questions will build my idea to limit the buffer free to <60, 60~75 paid and
75 no buff only box"* — which you then refined into something better, where **the free/paid line is
the BUFF, not the player's level**.

#### The Quests section's closing note

##### Quests

*(`BL-54` and `BL-55` were closed on 2026-08-27 — both were already true. The tutorial hands the
newbie boxes out on its level-10 and level-15 steps, and the newbie light/robe sets ARE the two real
starter sets, not placeholders. See [BacklogArchive.md](BacklogArchive.md).)*

---

#### The three "closed by your own ruling" sections that ended the file

##### What was closed on 2026-08-12 and is deliberately NOT in this file

The playtest-21 batch and `58d` shipped in `267313d` → `ed75bac`: shields option 3 (P.Def ÷5,
Shield Mastery ×5) · the shield enchant `+9 → +3` · the wood/iron shield block profile · the whole
start quest re-spec · training club and knives deleted · the `x500` mats stall · auto-farm ignoring
`RequiredWeapon` · the training dummies + rank titles · `65d` · `67i` · `68h` · `63i` · `62j` ·
broken jewels → 9/5/3 · **item tags and the full `/give`**. They live in `CHANGELOG.md`.

**The housekeeping batch, later the same day** took out `BL-37` (the test heal, deleted — and the
retired-skill-id leak it exposed in the save loader) and `BL-58` (`58i`, the inspiration-game name
purge; the tag is `IG`).

##### Closed on 2026-08-26 by your own ruling

**`BL-47` — YES, AND IT SPLITS THE WORK IN TWO.** Your answer, verbatim: *"yes. Player mobs are hand
crafted and field mobs stay on curve. Player mobs are player stats with equipped real items. Pk guards
with overechsnted gear and fortress fighting npcs with undergear as we described."* So: ordinary field
creatures keep the `MobBaseStats` curve with `MobMod` passives — one function moves every creature,
which is the property you did not want to lose — and player-built creatures become a **hand-placed
content tool** with real player stats and real worn gear. Everything built in 0.70.0 serves that shape
unchanged, and **`BL-79` (PK guards, over-enchanted) and `BL-80` (fortress NPCs, under-geared) are now
the roadmap for it**, both with the gear direction you just named. Old text in
[BacklogArchive.md](BacklogArchive.md).

**`BL-49` — LEAVE IT.** *"well ofc it's lot slower to llv up 85+ than 20... Leave it."* The 125%-of-a-
level-at-20 against 0.1%-at-85 spread is the EXP curve doing what you want it to do. Closed; not
re-proposed. ⚠ One consequence to watch in play rather than in the file: `BL-13`'s boss HP curve made
the level-44 boss take ~3× longer and boss exp is derived from kill time, so it pays ~3× more — about
half a level per head in a nine-man. Nothing caps it; the sanity rail only bites below level ~37,
where nothing spawns. See the 0.89.0 entry of `CHANGELOG.md`.

---

##### Closed on 2026-08-14 by your own later ruling

**`BL-26` (the vendor half of the buy-back design — "a longer sold list") is DELETED, not built.** It
descended from the *old* design recorded at `Roadmap.md:126` (*"a buy-back menu — last 10
deleted/sold"*). Your **`M14`** ruling in playtest-19 replaced it — *"cap the vendor buyback list at
10-15 items"* — and that is what ships: `GameConstants.BuyBackSlots = 12`, alongside a **separate**
5-slot `Restorable` list for bin-deletes (`C18`), which is the better shape you yourself proposed.
Newest ruling wins, so lengthening the list now would walk back your own number. Old text in
[BacklogArchive.md](BacklogArchive.md).

---

**Six more were checked out against the CODE, not the list** — every one was already built in a pass
whose commit carried no changelog entry, which is why they were still sitting here: `BL-31` (`55b`,
the HP price on a skill card), `BL-32` (`55c`, refusing an HP skill at low HP), `BL-33` (`57b`, the
duplicated Robe Armor Mastery), `BL-53` (Elder Marius's empty "!"), `BL-63` (Frost Bind stripping a
mob's HP multiplier) and `BL-64` (the target dropped for a physical cast). The table in
`CHANGELOG.md` names the code that proves each one. ⚠ `BL-63` and `BL-64` were closed on a **reading
of the code**, never re-tested by him — they are on the checklist as verifications, not called done.


## 2026-09-04 — `BL-158`…`BL-162`: the NPC buffer levels up with you (BUILT, 0.111.0)

All five shipped the day they were written. His idea, verbatim: *"help single players that dont want
to spend time in party and or lvl up a buffer"*. Measure the result with
`dotnet run --project tools/BalanceMatrix -- --npcshelf`, which prints the shelf at every level that
changes it, read off the live catalog rather than off his CSV.

### `BL-158` 🔴 The NPC buffer LEVELS UP with you — the rung *and* the price

Your idea, 2026-09-04: *"my idea is ... NPC Buffer will 'LVL UP' with the character .. if a player asks
the npc for buffs he will receive only buffs available to the same lvl bugger/healer - the npc no
longer will provide @40 buff that is learned at 74 (except the 8 free)"*. Its purpose is stated too:
*"help single players that dont want to spend time in party and or lvl up a buffer"*.

**Why it matters.** Every `npc_*` blessing today is **one def welded to the TOP rung** —
`NpcSingle(NpcBody, "Body", Rung(FamMaxHp, 6), …)` — so a level-40 character wears the level-74 buff.
The NPC has nowhere to grow, and it flattens the levelling curve it is supposed to support.

**The data is already authored:** `docs/data/classes_skills_csv/buffs.csv`, columns `NPC LVL` and
`NPC Price`. The **free eight** are marked at their TOP rung, level 6, price 0, and do **not** ladder —
your stated exception, and it is exactly what the code does today, so those eight need no change at
all. The **paid eleven** ladder:

| buff | rung 1 | rung 2 | rung 3 |
|---|---|---|---|
| Focus | 40 · +20% crit rate · 5k | 44 · +25% · 10k | 52 · +30% · 15k |
| Agility | 40 · +2 Eva · 5k | 44 · +3 · 10k | 52 · +4 · 15k |
| Ward | 40 · +10% M.Def · 5k | 44 · +23% · 10k | 52 · +30% · 15k |
| Vigor | 40 · +10% HP reg · 5k | 48 · +15% · 10k | 56 · +20% · 15k |
| Serenity | 40 · +10% MP reg · 5k | 48 · +15% · 10k | 56 · +20% · 15k |
| Ferocity | 40 · +25% crit dmg · 5k | 48 · +30% · 10k | 56 · +35% · 15k |
| Aim | 40 · +2 Acc · 5k | 48 · +3 · 10k | 56 · +4 · 15k |
| Frenzy | 40 · −7%/+5% · 10k | 52 · −10%/+8% · 15k | — |
| Soul | 44 · +10% Max MP · 5k | 56 · +25% · 10k | 70 · +35% · 15k |
| Body | 44 · +10% Max HP · 5k | 56 · +25% · 10k | 70 · +35% · 15k |
| Insight | 62 · +50% M.crit · 10k | 70 · +100% · 15k | — |

🔑 **The NPC deliberately SKIPS rungs.** Body and Soul take rungs 1, 4 and 6 of six; Aim skips its
first. Those gaps are what a real buffer fills — the mechanism, not an oversight.

🔑 **The price is the RUNG's price, not the buff's.** The paid tier becomes *cheaper* below 52 than
today's flat 15,000 and only reaches 15,000 at full strength. `SingleBuffCost` already computes
`BuffCostPerLevel × level` and is merely fed a hard-coded nominal 5; feed it the real rung and pricing
follows with no second edit. The deferred TODO above `BufferMinLvl` in `GameLoopService.cs` predicted
this feature by name.

**How it gets built — you asked directly, so the answer is recorded here.** *"does npc buffer gives
cast_atk_phys or give npc_might ? -> if it gives own id .. can we make it just to give the real buff
like the /buff comand with rung and duration ?"*

- It gives `npc_might` — but that is a **one-child wrapper**. What actually LANDS on the player is
  already the real buff: `buff_atk_phys_3`, the identical rung def a cleric's Might hands out. The
  wrapper survives only in `SourceSkillId`, as a receipt.
- `cast_atk_phys` **is not an engine id at all.** It is the CSV's name for the cleric's cast. The real
  ladder is `buff_<family>_<rank>` (`SkillCatalog.Rung`), one def per rung, and **the rung index IS the
  rank**.
- The engine already supports the whole request: `ApplyBuff(target, def, LEVEL, …, durationOverride,
  sourceSkillId, …)` plus **`def.ChildBuffsAt(level)`**, which lets one wrapper declare a *different
  child per rung*. `/buff` is that same call with `def.MaxLevel`.

🔑 **So the wrappers STAY and gain a per-level child table — they are not deleted.** Deleting them
would re-point `NewbieBuffSet`, `FreeNpcBuffSet` and both role presets at rung ids, orphan every saved
custom preset (they store `npc_*` ids) and force a save migration, all for no gain. Keeping them means
**[Save] is untouched**, which is what you asked for, and the rung still lands as the real family def
so eviction by a Warchanter's group works unchanged.

The feature reduces to one table — id → [(your level, rung, price)] — read straight off the CSV.

✅ **ALL 30 RUNG VALUES VERIFIED TO EXIST, 2026-09-04 — there is nothing to author.** Every number in
the eleven ladders above is already a rung in `Skills.BuffLadders.cs`, at the index his level implies:
Focus 5/10/15/**20/25/30** · Agility (mirrors Aim) 1/**2/3/4** · Ward 10/20/**23**/**30** · Vigor
5/**10**/12/**15**/17/**20** · Serenity same · Ferocity 10/15/20/**25/30/35** · Aim 1/**2/3/4** ·
Insight 20/35/**50**/65/80/**100** · Soul **10**/15/20/**25**/30/**35** · Body same · Frenzy
**r1 = −7%/+5%/+5 move/−5 eva**, **r2 = −10%/+8%/+8/−8**. 🔑 **So his CSV is DESCRIBING the shipped
ladders, not proposing new ones** — the whole of `BL-158` is choosing *which* existing rung to hand out
and what to charge. No magnitudes, no renumbering, no new defs.

⚠ `buffs.csv` is in **no `Check.Specs`**, so `SkillCsvSeed --check` does not verify a line of this. It
earns its entry the day the ladder is code.

---

### `BL-159` 🔴 The NPC buffer loses its level-75 ceiling

Your ruling, 2026-09-04: *"My idea is to remove the max cap .. the scrolls are helping only so much and
they are just not to return to town but u get weaker - or if you are rich to have full single buffs at
max lvl form early on ... but still they have + and - .. so leave them be - NPC buffer no top cap .. and
if you want to farm semi buffed u return and rebuff or weaker with boxes"*.

This **reverses the half of `BL-150`** that read *"75 no buff only box"*. `BufferMaxLvl = 75` goes, and
with it the refusal message that sends you to the Blessing Box.

⚠ **The Blessing Box (`BL-151`) is NOT cut** — *"so leave them be"*. Its role changes from *the only
endgame buff layer* to *the weaker option you take rather than walk back to town*. That is a real role
and its 30k-a-blessing price does not need re-deriving.

⚠ **One constant to check while the ceiling comes out:** `RestoreCost` is free below 40 and charged
40-75. With no ceiling it becomes "free below 40, charged above", which is coherent — but **the 40 in
it was mine, not yours** (`BL-150` priced the buffs and said nothing about restore). One line to move
if you meant restore to stay free throughout.

---

### `BL-160` 🔴 Eight NPC single harmonies — the floor a real Warchanter evicts

Your ruling, 2026-09-04: *"the single harmonies should not exist atm .. we must create them and give
them to NPC buffer - they will be available as the actual harmony will be learned from the warchanters
so it will always be replaced if player buffs"*.

**Eight new single-rung AoE buffs, 50,000 gold each**, authored in `buffs.csv`. Each lifts ONE effect
out of a Warchanter harmony and unlocks at **the exact level the Warchanter gains that effect**. All
eight verified against `buffer 3rd.csv`:

| NPC single harmony | lvl | Warchanter rung that first grants it |
|---|---|---|
| Harmony of Ward · +30% M.Def | 44 | Harmony of Protection @**44** ✅ |
| Harmony of Force · +10% M.Atk | 48 | Harmony of the Wizard @**48** ✅ |
| Harmony of Swift · +20 move | 48 | Harmony of Speed @**48** ✅ |
| Harmony of Alacrity · +30% cast | 52 | Harmony of the Wizard @**52** ✅ |
| Harmony of Bulwark · +25% P.Def | 56 | Harmony of Protection @**56** ✅ |
| Harmony of the Might · +12% P.Atk | 56 | Harmony of the Warrior @**56** ✅ |
| Harmony of the Fury · +15% atk speed | 58 | Harmony of the Warrior @**58** ✅ |
| Harmony of Body · +30% Max HP | 66 | Harmony of Protection @**66** ✅ |

🔑 **Eight for eight. This is why they do not undercut the Warchanter** — your own argument: *"i wont
buff harmony of protection at 44 lvl because harmony of protection and harmony of ward give the same
effect at that lvl, but at 56 mine is already 1 space 2 buffs .. its strategy"*. The NPC single is the
floor; the class harmony is the same effect *plus* everything above it, in one bar slot. It is exactly
one slot behind, forever.

Mechanically this needs nothing new: a group harmony carries `GroupRank = 100 + level` and declares
`CoveredKeys`, so it **already** evicts a single of a family it covers.

✅ **The ids are clean — you fixed them in the CSV on 2026-09-04, mid-conversation.** Recorded because
the trap is real and will recur: the first draft reused `npc_harmony_warrior` for two different NPC
singles, and that id is **already the Warchanter's own multi-rung class harmony** (append-only,
`AdminBuffSet` names it) — building it would have overwritten a class skill. Also fixed: `wc_harmony_swift`
(did not exist; the Warchanter's is `wc_harmony_speed`) and `npc_harmony_Ward`'s stray capital in a
case-sensitive id. The eight now read `npc_harmony_ward` / `_bulwark` / `_body` / `_force` / `_alacrity`
/ `_swift` / `_might` / `_fury`, and **none collides** with the three that exist
(`npc_harmony_protection`, `_warrior`, `_wizard`). Build them as authored.

⚠ **One mechanical consequence, because eviction is permanent and not suspension.** The NPC harmony
runs 1 hour; the Warchanter's runs 5 minutes. If you buy the single and are *then* buffed by a
Warchanter, the group evicts your 1h buff and you are bare after five minutes — worse off than if you
had never been buffed. It only bites in that order, and your strategy argument says a player who has a
Warchanter would not buy the single anyway. Recorded, not treated as a defect.

❓ **Duration unconfirmed.** `buffs.csv` says *"NPC harmonies default duration 1 h"*. Assumed 1 hour.

---

### `BL-161` 🔵 The three Marks on the NPC shelf — level 78, 300k each

`buffs.csv` puts Holy Mark, Life Mark and Blood Mark on the NPC buffer at **78 / 300,000 gold**. They
are reachable only because `BL-159` removes the 75 ceiling, so this is gated on that.

The three already exist — they are **Lightbringer 4th-class skills** (`Skills.Lightbringer4th.cs`),
learned at **78 (rung 1)** and 83 (rung 2). Your CSV marks **rung 1 only**, at 78. Same pattern as the
harmonies in `BL-160`: the NPC gets the rung the class has just learned and never the one above it. No
new skills to author — this is a shelf entry and a price.

✅ **Both questions answered, 2026-09-04.**

1. **NO skill stones from the NPC** — *"no point for npc buffer to require from you skillstones to use
   marks it costs u 300k alreay"*. The 300,000 gold replaces the Lightbringer's 4-stone cost outright;
   it does not charge both. (A Lightbringer casting her own Mark still pays stones — that is her skill,
   not this shelf entry.)
2. **They do not stack, and that is the whole price model** — *"the marks dont stack .. if you deside to
   rebuff with other it will cost you new 300k .. and its learned at 78 so before that no mark at all"*.
   Their own text already says *"Do not Stack with Other 'Mark' Skills"*, so one Mark at a time,
   and switching is a fresh 300,000.

🔴 **That kills my pricing objection and the number behind it was wrong.** I costed a "full set" as all
THREE Marks — 900,000, which I called ~60% of a ~1.47M hour of buffs. You only ever wear ONE, so the
real endgame bill is 165k (eleven blessings) + 400k (eight harmonies) + 300k (one Mark) ≈ **865,000**,
and the Mark is about a third of it rather than two thirds. 🔑 **The lesson is the ordinary one: I
priced a set without reading the stacking rule printed in the same CSV cell.** The `--goldflow`
measurement I offered is no longer worth blocking on; say the word if you want it anyway.

---

### `BL-162` 🔴 Swift joins the Mage preset — the free eight split 3 / 3 / 2

Your correction, 2026-09-04: *"mage - swift, alacrity, resolve, bulwark, force - 5 out of 8 / fight-
swift, might, bulwark, vamp, fury - 5 out og 8 / the 8 buffs are 3-fighter(might,vamp,fury),
3-mage(force,resolv,alac), 2-shared(swift,bulwark)"*.

`FighterBuffSet` is already those five. **`MageBuffSet` has only four — Swift is missing.** One id to
add, and the split becomes exact: fighter-only 3, mage-only 3, shared 2, union = all eight, and
5 + 5 = 8 + 2 overlaps.

⚠ **A written contract moves with it.** The doc comment on `MageBuffSet` states the invariant *"Fighter
∪ Mage = FreeNpcBuffSet, with Bulwark **the one buff** both roles want"*. It becomes "Bulwark **and
Swift**". Update it in the same edit — a stated rule that quietly stops being true is how three of
these went wrong in two days.

**Nothing else about the presets changes.** You raised a second custom slot and then withdrew it: *"no
no .. the second preset was if the 1st wasnt per subclass... but as u stated that it is, no need for
second save"*. 🔑 **Custom presets are ALREADY per-subclass and always have been** —
`Entity.ActiveBuffPreset => ActiveSubclass.BuffPreset`, persisted per subclass via `BuffPresetJson` on
the subclass record, exactly like the skill bar. Change subclass, get that subclass's own preset; go
back to your main and it is untouched. No work owed, and no schema change.

---

## 2026-09-04 — `BL-154` and `BL-155`: the tank's 4th tier closes them both (BUILT, 0.112.0)

Both had been *engine built, rows placeholder* since 0.110.0, and both were waiting on the same thing:
`tank 4th.csv`. He finished it on 2026-09-04 (*"im done with tank 2/3/4 so its ready to build after the
npc buffer"*) and 0.112.0 built all 205 rows, which answers every `🔵` line in the two entries below —
Grapple's numbers, the two silences' numbers, and the Elf/Human+Demon split. What did NOT come with the
file is recorded as `BL-165`: the two AoE pull shapes he never asked for a second time, and the
drag-smoothing clamp still awaiting his eyes. The text of both entries follows verbatim.
### `BL-154` 🔵 Pull — BUILT (0.110.0-0.110.2); left: your CSV, the two AoE shapes, and one clamp awaiting your eyes

Your spec, 2026-09-03: *"tanks will have pull -> target or aoe around.. con saves and if succeed pulls
the target to the caster, hope its not instant but 300 range per second .. to look like a pull not
phase shift"*, then *"I like the whole pull to be a 1s~1.5s pull. And 1~2s stun. The pull idea is
shorten the distance + enemy interrupt rather than control"* and *"also one con contest for pull
+stun"*.

**✅ THE ENGINE SHIPPED IN 0.110.0**, with all seven of your rulings in it:

| | ruling | how it landed |
|---|---|---|
| 1 | Rooted, no actions | `IsActionLocked` grew a pull arm, beside charm and fear |
| 2 | The pull itself is not interruptible | It is a short PHYSICAL active; the drag deals no damage, so nothing rolls against it |
| 3 | Stops at melee range | `GameConstants.MeleeRange`, re-aimed each tick at the puller |
| 4 | Two AoE shapes, 2-5 bodies | 🔵 **the ENGINE is there, the SKILLS are not** — see below |
| 5 | Boss immune, players yes | `BossShrugsOff` learned `def.Pulls` |
| 6 | ~~Threat below the taunt~~ — **REVERSED by you, 2026-09-04** | 🔴 It is a **DAMAGE skill**, not a threat skill: `Power: 3000`, no `TauntPower`. See below |
| 7 | 1s stun on the SAME contest | Held on the victim and applied by `FinishPull` **on arrival**, so drag and stun run in sequence rather than overlapping |

🔑 **THE DRAG IS TIMED, NOT PACED — your 300/s and your 1-1.5s are two different rules and the second
one won.** A fixed speed makes the lockdown scale with the range you author (a 900 pull would take 3
seconds); a fixed duration does not. `PullSeconds` is the whole journey from any distance and the speed
is derived, floored at your 300/s so a short pull arrives early instead of crawling. **Range now buys
reach and never buys lockdown** — author 900 if you want the reach.

✅ **THE DRAG INTERRUPTS, AND YOU RULED THAT IT SHOULD** (2026-09-03, after it was flagged as a
correction to what this entry first claimed): *"I like the actual pull interrupt - it's the logical
way ... U don't see a mage being dragged and still casts."* So the chain interrupts twice over, and an
AoE pull — which carries no stun — still interrupts what it drags. It falls out of your *"like charmed
while dragging - no act"* for free: being dragged is an action lock, and `UpdateAction` has always
cancelled the cast of anything action-locked.

🔴 **GRAPPLE IS A DAMAGE SKILL, NOT A THREAT SKILL — you reversed row 6 on 2026-09-04:** *"does grapple
work in auto or is it a taunt skill .. if it's a taunt skill I want it to not be, and be a normal dmg
skill with 3k power (my standard dmg skill is 4k so later it will grow as well when authoring)"*.

It shipped in 0.110.0 as `TauntPower: 3000` with **no damage at all**, and that had a consequence you
found before I did: `BL-83` routes every threat skill to the **never-auto-cast** bucket, and
`TauntPower > 0` is the first test it applies — so a tank's new signature move could not appear in a
rotation at all. **Fixed in 0.110.1: the 3000 MOVED to `Power`, it did not double.** Grapple is now
`PhysicalDamage | Stun`, Power 3000, no `TauntPower`; it builds threat only through the damage it
deals, and it lands in the **Attack** rung of the auto chain. The drag, the stun tail and the one CON
contest are untouched.

🔴 **AND THE DRAG WAS DECLARING ITSELF A TELEPORT TEN TIMES A SECOND — fixed 0.110.2.** Your report:
*"it drags the monster but it's like lagging, not like a continuous clean drag ... it seems real time"*.
`EntityDto.Warp` is not a "position changed" flag — it is an instruction to the client to **`SnapTo`
and RETURN**, skipping interpolation. `TickPull` moves the body through `PlaceEntity`, which bumps that
counter on every call **by design** (it is the one seam blink, knockback, the gatekeeper and respawn
all pass through, which is what made the Phase Shift fix free). A pull calls it every tick, so the
client hard-snapped the mob ten times a second with nothing drawn between the snaps — a 10 Hz
staircase landing in exactly the right place. `PlaceEntity` now takes `announce` (default **true**, so
every other caller is unchanged) and `TickPull` passes `false`. 🔑 **The line is CONTINUITY, not "did
something else move it".** Server-side only; no APK needed for this one.

🟡 **AND ONE THING IS WAITING ON YOUR EYES — NOT BUILT, NOT TESTED.** Your instruction, 2026-09-04:
*"mark the one clamp / EntityView.Update as untested and I'll see it in game first then decide"*. The
client's interpolator sizes each segment by the measured gap between the last two updates, and the
server sends only what CHANGED — so a mob that stood still for ten seconds and is then grappled has a
**ten-second first segment**, and the drag's opening ~100ms draws almost frozen before the second
sample corrects it. It self-corrects after one sample: a hitch at the START of a drag, not a stutter
through it. A clamp on that span (~0.2s) fixes it, and every mob's first step out of an idle with it —
but `EntityView.Update` has been rewritten three times to kill the rubber-band, and this is not what
you reported. **The test is to grapple something that has been standing STILL**; if the body hangs for
a blink before it slides, that is this, and if you cannot see it, it does not need fixing.

**What is still owed, and it is yours:**

- 🔵 **`tank 4th.csv` has ONE placeholder row** (`Grapple`, 76, range 600, 1.2s drag, 1s stun, 15s
  reuse, 80 MP). Every number in it is mine except the ones you ruled. Fix them when you write the file.
- 🔵 **The two AoE shapes are not authored** — you named one pull, not three, and a skill nobody asked
  for is a skill nobody can retune. The engine serves both already: `TargetMode.EnemiesInRadius` with
  `AreaAtTarget` picking the centre (the target for the ranged one, the caster for the self-centred
  one) and **`MaxTargets` as your cap of five**, which the area sweep learned in the same pass. They
  need rows and nothing else.

### `BL-155` 🔵 Silence — BUILT (0.110.0); the boss skill is live, the tank rows are placeholders

🔴 **The DISARM is DECLINED, by you, 2026-09-03** — *"If we leave the weapon bonuses it's not a disarm.
Let's don't do a disarm .. But I like your silence idea"*. Old text in
[BacklogArchive.md](BacklogArchive.md); nothing of it is owed.

**✅ SHIPPED IN 0.110.0.** Physical silence (physical skills fail, **the basic attack still works**),
magical silence, and both at once = a full silence — two independent debuffs, so the "full" version
needs no third skill. It completes the disable map:

| disable | what it takes away | state |
|---|---|---|
| charm / fear | **everything** | built (`BL-110`) |
| hold / bind | **movement** | built |
| physical silence | **physical skills** (basic attack survives) | ✅ 0.110.0 |
| magical silence | **magical skills** | ✅ 0.110.0 |
| both / boss | **every skill** | ✅ 0.110.0 |

🔑 **The physical-vs-magical axis was already built and was not re-invented.** `SkillMath
.PacedByAttackSpeed` — the three-marker test from your `BL-133` cast-speed pass — was **renamed
`IsPhysical`**, the name of the question it actually answers, with the old name kept as a one-line
alias at the speed call sites. A skill can never be physical for cast speed and magical for silence.

✅ **The dungeon bosses have theirs** — *"a full silence aoe skill for 15s duration and 45s cd (mp cost
u deside)"*. **Word of Unmaking**: 150 ticks, 450 ticks, 500 radius, SPT-defended, **MP 0** like every
other boss skill (a rotation must never stall on mana), on `grave_lich` (44), `dread_knight` (65) and
`disciple_of_the_dawn` (90). 🔵 **Watch it in play: 15s on 45s is 33% uptime with no heals**, which is
brutal by design and the first number to move if a boss becomes unkillable.

**What is still owed, and it is yours:**

- 🔵 **Two placeholder rows on `tank 4th.csv`** — `Numbing Strike` (Human + Demon, CON-defended) and
  `Silencing Ward` (Elf, SPT-defended), one rung each at 76, 8s, 30s reuse, 70 MP. The race split
  continues the one `tank 3rd.csv` already draws; every other number is mine and yours to overwrite.
- ✅ **Bosses ARE immune to silence, and you ruled the whole boundary** (2026-09-03): *"bosses are
  mostly immune .. Only decreasing skills - like armor/weapon breaks tyoe and dot effects."* Checked
  against the code rather than assumed, and **it is already exactly that rule**: `BossShrugsOff` fires
  on `ControlCc` (= `Slow | Stun | Fear | Root`), charm, pull and the two silences, and explicitly
  exempts `AnyDot`. Armor Break and Weapon Break carry `DebuffPDef` / `DebuffAtk`, which is none of
  those — so the stat-strippers and the DoTs land on a boss today and always have. Nothing to build;
  the ruling is recorded so the next control payload knows which side of the line it goes on.
- 🔵 **The worm's own full silence** waits on `BL-157`.

---

## `BL-163` ✅ BUILT 2026-09-17 in **0.173.0** — The buffer's shelf as an EXTERNAL table — no wrappers, editable without a build

It is `docs/data/npc_buff_shelf.csv` now: fifty rows of `SHELF_ID,MIN_LEVEL,RUNG_SKILL_ID,RUNG_LEVEL,PRICE`,
read at server start, with the whole `BL-158` tier assertion deleted and every number byte-for-byte
what the C# table handed out (measured with `--npcshelf` and `--buffmenu`, before and after). The one
thing the entry did not foresee: the UNITY CLIENT compiles the same assembly and has no file, so the
thirty ids stay in C# as the shelf's UNIVERSE (`NpcShelfCatalogue`) while the file owns the tuning.
A fifth column, `RUNG_LEVEL`, exists for the Marks — a multi-rung class skill sold at rung 1.

The entry as it stood:


Your ruling on the shape, 2026-09-04, right after `BL-158` shipped: *"that's why I wanted the npc buffer
to be like the /buff command not like a wrapper or check player lvl and out him in a range table with
available buffs ... and that table can be a file with min lvl,skill_id_rung,price (editable from outside
- so a pvp server won't require new npc just change of id's) .. but whatever is working"*.

**What shipped in 0.111.0 is two thirds of this already.** The NPC does grant the REAL buff: `npc_ward`
is a one-child wrapper and what actually lands is `buff_def_mag_3`, the same rung def a cleric casts.
And the ladder IS a table — `SkillCatalog.NpcBuffTiers`, `id → (MinLevel, Price)[]`. What your version
changes is the two things that make it a *server-operator* feature rather than a developer one:

1. **Name the rung directly, drop the wrapper.** The table row carries `skill_id_rung`, so the shelf
   points at `buff_def_mag_3` and the NPC grants it exactly the way `/buff` does — `ApplyBuff(def, 1,
   durationOverride: NpcBuffTicks)`. No per-blessing `Levels` array to keep in step with the table, and
   no "tier index == SkillLevel index" invariant to guard (the whole startup check `BL-158` needed
   simply stops existing).
2. **Move it out of C#.** One file, read at startup: a PvP server retunes its buffer by editing ids and
   prices, with no rebuild and no new NPC. That is the actual ask and it is the part that has value
   beyond tidiness.

**The one thing that needs care, because it is a real regression if missed.** The table cannot be just
`(minLevel, rungId, price)` — it needs a fourth column, a stable **shelf id**, and the wrapper id is
what plays that role today. Two things key off it:
- **`[Save]` and the two role presets store what you PRESSED, not what landed** (`SourceSkillId`, and it
  is precisely the playtest-29 bug that killed [Save] for two versions). A preset holding rung ids would
  freeze the player at the rung they saved — save Ward at 44 and you would still be buying +23% at 70.
  A preset must name the BLESSING and re-resolve the rung at expansion, which is what makes his
  `BL-150` rule work: *"if some1 buff me with body or soul and i save it and im <40lvl they will not
  activate .. they will activate after 40+"*.
- **Saved presets already in the database hold `npc_*` ids.** Changing what a preset stores is a save
  migration, or a `game.db` delete — one is already owed, so this should ride it rather than add a second.

So the row is `(shelfId, minLevel, rungSkillId, price)`, and `shelfId` can stay `npc_ward` — the ids are
append-only anyway and every saved preset in existence already uses them.

**Also needed, and cheap:** startup validation that every `rungSkillId` resolves and every ladder is
monotonic (the same two guards `BL-158` added, moved to the loader — a typo in an operator-edited file
is far likelier than a typo in C#, so the file must refuse to load rather than silently sell nothing).
An admin reload command would be a nice-to-have; startup-read is enough to satisfy the ask.

⚠ **Nothing is broken today** — this is a refactor for editability, not a fix. Your own words:
*"but whatever is working"*. Queued behind the tank pass unless you say otherwise.


---

## `BL-164` — the Marks' rank (cut 2026-09-18, built in 0.177.0)

✅ **RULED AND BUILT THE SAME DAY.** You picked option 1, and stated the failure it had to close in
your own terms: *"i want mark to have ranks .. a Life mark L2 to be replaced only by other l2 marks ..
not some1 to be able to put lower rank -> admin of buffer gives me rank2 and stupid me goes to npc and
overrites it ... it shouldnt"*.

**What changed:** all four Marks (Holy, Life, Blood, Harmony) stopped being `FlatRank: true` and now
carry the rung in the rank — rung 1 lands at rank 1, rung 2 at rank 2. `SharesLadderKey: true` is the
declaration the `BL-85` startup guard demands for four childless multi-rung defs on one key, and it is
the honest one: these ARE four versions of the same buff that should compete rung for rung. Nothing
else moved — one key still means one Mark at a time, and a same-rung Mark from another race or from the
buffer still replaces freely, because equal rank replaces since `BL-263`.

The NPC's rung-1 Mark is now refused before the 300,000 gold is taken: the purchase path already asks
`BuffWouldLand` first, so the wall and the wallet agree.

Below is the entry as it stood, including the `BL-263` reopening that made option 1 close both
directions at once.

### `BL-164` 🔵 The three Marks share one Rank, so the weaker rung can out-hold the stronger

Found while building `BL-161`, and flagged rather than absorbed because the fix is a judgement call.

`Mark(...)` hardcodes `Rank: 1` for BOTH rungs (the Lightbringer learns rung 1 at 78, rung 2 at 83), and
all three Marks share one `BuffKey` so they never stack — which is correct and is your rule. The problem
is the tie: `ApplyBuff` resolves EQUAL rank by keeping the **longer remaining time**. So an NPC Mark,
sold at rung 1 for an hour, will refuse a Lightbringer's rung-2 Mark at 83 for up to 55 minutes — the
weaker buff holding out the stronger one.

⚠ **It is not caused by the NPC being a wrapper, and `BL-163` would not fix it.** Any delivery of rung 1
with an hour on it beats a 5-minute rung 2 at equal rank.

Three ways out, and it is your call which:
1. **Rank = rung** on the Mark ladder (rung 2 → rank 2), so the stronger one always wins. Cleanest, and
   it is how every other family here already behaves.
2. **The NPC's Mark runs 5 minutes**, like the class skill — but that contradicts your `buffs.csv`
   header (*"NPC marks default duration 1 h"*) and makes 300,000 gold a hard sell.
3. **Leave it** — the same "strategy" answer you gave for the harmony case, since a player with a
   Lightbringer in the party has no reason to buy the NPC's Mark.

Nothing is blocked on this; it only bites a level-83+ character who bought a Mark and then joined a
party with a 4th-class Lightbringer.

🔴 **REOPENED-AND-CHANGED BY `BL-263` (0.176.0, 2026-09-18).** Removing the duration tiebreak flips
this bug rather than fixing it. The complaint above — *the weaker rung out-holding the stronger* — is
**gone**: a Lightbringer's rung-2 Mark now replaces an NPC rung-1 Mark on the spot. But the reverse is
now possible and was not before: **buying the NPC's rung-1 Mark while wearing your own rung 2 will
overwrite it**, because at equal rank the last cast wins and `BuffWouldLand` no longer refuses it, so
the gold is taken too. **Option 1 (Rank = rung on the Mark ladder) is the fix, and it now closes both
directions at once.** ⚠ Option 3 ("leave it") no longer means what it meant when you read it.

---

## `BL-166` … `BL-169` — the boss rework and the rig behind it (cut 2026-09-05, built in 0.113.0)



### `BL-166` ❓ Bosses get their OWN stat block, beside their kit

Your question, 2026-09-05: *"the bosses don't fallow the curve persay but have different edits per
boss. Some have fighters and given decrease in stats the others are solo and given increase. The curve
is the base and every boss edit is making the boss unique — are bosses separate from the mobs file (in
code .cs - not folder file)? they need their own to be edited/added - stat and skills as well"*

**The answer is HALF, and the half you already have is the skills.** `Game.Shared/BossCatalog.cs`
holds a `BossProfile` per mob-template id — its skill rotation (each entry with an HP window) and its
phase script (announce / enrage / add wave). That file is exactly the thing you are asking for, and it
is where `BL-155`'s dungeon full-silence went.

**What is NOT per-boss is the stat block.** A boss's numbers are the ordinary creature curve
(`MobBaseStats`) × its template's optional `MobMod` × **`MobRankScale`, which is ONE set of numbers
every boss in the game shares** (×4 attack, ×2 defence, one HP curve). `MobMod` *could* carry a lean
— it has P.Atk / M.Atk / P.Def / M.Def / HP / attack speed / four resists / CC overrides — but **three
of the four boss templates carry no `MobMod` at all**: `valley_treant`, `dread_knight` and
`disciple_of_the_dawn` are the bare curve × the shared rank. Only `grave_lich` has one.

**So: give `BossProfile` a stat block**, so one entry in one file is the whole boss — HP multiple,
attack lean, defence lean, rotation, phases, adds. `MobRankScale` stops being *the* boss stats and
becomes the **default** a profile overrides. That is your *"the curve is the base and every boss edit
makes the boss unique"*, said in code, and it costs one record field plus a read in `BuildMob`.

📄 **Full write-up, with the measured numbers: [design/BossRework.md](design/BossRework.md).**
❓ **Open on you: §5 questions 3, 4 and 7** — is a boss's escort a hand-authored roster on the profile
or the generated trash already standing there; how much is the "decrease in stats" for an escorted
boss; and were the fighters that nearly killed you the `GuardTank`/`GuardArcher` pair (if so they are
already the template for what a boss's fighters should be).

---

### `BL-167` ❓ The boss attack ladders — `solo boss` ×2, the rune ×2, and a REAL enrage timer

Your rulings, 2026-09-05, after the level-90 boss failed to threaten your tank: a **`solo boss`
passive** worth ×2 P/M.Atk for a boss with no escort; **every boss holds a War/Spell Rune**, worth
another ×2; **overpowered single/AoE skills** doing 1200/1500 to a 2.5k-def tank; and an **enrage
ladder** — *"if battle becomes longer than 20 min he gets a buff that additionally doubles p/m atk and
after 40 mins it's gives another x2 (only field/dungeon bosses, world once the it times will be 2h and
3h)"*.

**Your diagnosis is right, and measured it is worse than you said.** At 90 the boss's basic attack on
a Knight is **728 — five percent of his ~14.5k pool** — and he survives **33 seconds with nobody
healing him at all**. You quoted 300; either number is scenery.

🔴 **AND THE ENRAGE TIMER IS AT NINETY SECONDS, WHICH I DOUBT YOU KNEW.**
`GameLoopService.BossEnrageTicks = 900` and the loop runs at **10 ticks/sec** — so it is 900 ticks =
**90 seconds** of engaged combat, not 900 seconds and nowhere near your 20 minutes. It fires **once**,
for **×1.5**, and never again. Today every boss in the game enrages a minute and a half in, by half.
Your ladder replaces it outright.

**The two ×2s are cheap.** `solo boss` is a profile field, not a mechanic. The rune already has a
precedent to copy: `demo_seraph_rune` and the four `BL-79` guards hold `ItemCatalog.WarRune` through
`MobBuild.Held` and it measures ×2.00 P.Atk. **I would give a boss the real held item** rather than
folding ×2 into the rank — a player who inspects a boss should see *why* it hits that hard.

⚠ **But the ladder has a ceiling and you should see the arithmetic before I build it.** ×2 solo × ×2
rune on today's ×4 rank = **×16**: the 90 boss's basic goes 728 → **2,912 (20% of the tank's pool)**
and its sustained damage on the tank goes 442 → **1,768**, against a healer ceiling of **391**. That is
a **4.5× deficit** — the tank dies in about ten seconds however well he is healed. For a **raid** with
four or five healers that is the fight you are describing and it is fine. **For the 5-man party of
`BL-13` it is not payable**, so on a field/dungeon boss these ladders get measured against the healer
the way `BL-13` measured the original ×4. Your *"not one shooting but a tank can feel it"* is still
the test. See `BL-168` — the split is what makes both numbers true at once.

📄 [design/BossRework.md](design/BossRework.md) §3. ❓ **Open on you: §5 question 5** — are the
1200/1500 against the same 2.5k-def tank, and is 1200 the single and 1500 the AoE? (An AoE hitting
harder than the single is the reverse of the usual, so I want it confirmed rather than assumed.)

---

### `BL-168` ❓ A WORLD-BOSS rank — 3-6kk HP for a raid, so the 5-man band survives

Your reading, 2026-09-05: *"looking in IG I'm seeing bosses 85 with 3~6kk hp not like out 350k"*, and
*"there are bosses with 3kk hp with fighters boss being a mage with less patk more m atk less Def, and
there are solo bosses with 6kk hp with high p atk and Def"*.

🔴 **THE HP NUMBER COLLIDES WITH A RULING OF YOURS, AND I AM NOT BUILDING IT QUIETLY.** `BL-13`
(playtest 25) is *"the bosses should take 10-15 even 30 mins to kill"*, and `MobRankScale.Hp` was
fitted **by measurement** to land every boss inside 600-1800s **for the 5-man party you prescribed**
(tank, healer, 2 champions, 1 nuker). At 90 it measures **343,474 HP → 1,274s, 21 minutes** — dead
centre of your own band. **×10 the HP and that party needs three and a half hours**, and that is the
*ceiling* estimate with no downtime, no deaths, no adds and no running back in. `BL-167`'s attack
ladder then makes it longer still, because more healing needed means fewer DDs.

**The two are not in conflict in IG, because a 3-6kk boss there is a RAID boss** — several parties,
not one. Our number is small because it was fitted to the party you named. And your own message
already draws the line: *"(only field/dungeon bosses, world once the it times will be 2h and 3h)"*.
`BL-13`'s own note says the same thing from the other side: *"a world boss has no rank of its own …
`BL-13` still says it wants a rank of its own"*.

**So: split the rank.** `MobRank` is `Normal / Elite / Boss`; add `WorldBoss`.

| | **Field / dungeon boss** (`Boss`) | **World boss** (`WorldBoss`, new) |
|---|---|---|
| Fought by | the 5-man party of `BL-13` | a **raid** — several parties |
| HP | today's curve (~343k at 90) — unchanged, it measures right | **3-6 kk**, authored per boss |
| Enrage ladder | 20 min ×2 → 40 min ×4 | 2h ×2 → 3h ×4 |
| Rune ×2 / `solo boss` ×2 | yes / yes | yes / yes |

That gives you **every number in your message with nothing overruled**: the 3-6kk lands on the world
boss where IG puts it, and the field/dungeon boss keeps the 10-30 minutes you ruled for it while still
gaining the attack ladders, the real enrage timer and the per-boss kit — which is what actually fixes
*"cannot kill my tank"*.

📄 [design/BossRework.md](design/BossRework.md) §4.
❓ **Open on you, and everything above turns on it: §5 questions 1 and 2** — **is a field/dungeon boss
still a 5-man fight and a world boss a raid?** If you want *every* boss at 3-6kk, then bosses are raid
content across the board, `BL-13`'s 10-30 min band is withdrawn, and I refit to the raid instead — say
so and it is one measurement, not an argument. And **how big is a raid** (two parties? nine?) — it
sets the HP fit and nothing else.

---

### `BL-169` 🔴 BalanceMatrix measures an UNBUFFED 2nd-class tank — every tank verdict it has printed is too weak

Found 2026-09-05, by checking your own reading against the tool. You gave your real character: *"my
paladin tank 90lvl with epic 76 gera have 1300pdef unbuffed and 2300+300 reinforcement to 3200 with
aegis sigil buffed from npc (mark/harmony) and the boss with 14.5k p atk does 400"*.

**Your 400 reproduces exactly** — `PhysicalDamage` is `77 × pAtk / def`, and 77 × 14,500 / 2,600 =
**429**. The damage model is fine. **The measuring rig is not.**

🔴 `BalanceMatrix.BuildBossParty` builds its tank as `BuildPlayer(Human, Fighter, level)`, and that
function:
1. **passes no discipline**, so at level 90 it is still a **2nd-class Knight** (`SecondClass = 13`) —
   no Bulwark, no Aegis, none of the 3rd/4th-tier kit that is most of a modern tank's defence;
2. **applies exactly one buff, the War Rune.** There is **no NPC buffer in the rig at all** — no mark,
   no harmony, no Shield Blessing, no reinforcement — even though the buffer has been in the game since
   `BL-149`…`BL-162` and every real player walks out of town with it up.

Back-computed from the tool's own output (77 × 14,500 / 728), its tank sits at **~1,533 P.Def** against
your real **2,600** — it understates a played tank by about **70%**, and has done since the buffer was
built. Gear is right (`GearTier` epic = your "epic 76"); it is the KIT and the BUFFS that are missing.

**Why this matters beyond bosses.** The `BL-13` *"IS A PARTY MANDATORY?"* table is the thing every boss
number in the game is tuned against — `MobRankScale.Atk`'s ×4 was chosen off it, in your own words
*"the largest multiplier that leaves the healer headroom"*. If the tank in that table is 70% under-
defended, then **`BL-13`'s attack band was fitted against a straw tank and is probably too gentle** —
which is exactly the complaint you opened with. It also means my first answer to you on `BL-167`
(*"×16 is a 4.5× deficit, not payable for a 5-man"*) was computed on that same straw tank and was
**too strong**; corrected in [design/BossRework.md](design/BossRework.md) §4.

**The fix** is small and entirely in the tool: give `BuildPlayer` the discipline the character would
have at its level, and apply the NPC buffer's standard shelf the way it applies the rune today. Then
re-run and re-read `BL-13`, `BL-167` and `BL-168` off honest numbers.

⚠ **Nothing in the GAME is broken by this** — `MobRankScale`, the shields and the buffs all behave as
authored. What is broken is the instrument, and it is the instrument every future balance ruling is
read from. That is why it is 🔴 and why it goes before `BL-167`'s numbers, not after.

🔑 **The rule this is the third instance of: check the RIG before the subject.** A measurement that
models a weaker character than the one being complained about will always agree that nothing is wrong.

✅ **`BL-169` IS BUILT (2026-09-05, tool only).** `BuildPlayer` gained `npcBuffed`, the existing
`ApplyNpcBuffs` gained `fullShelf` (the eight single harmonies of `BL-160` + one Mark of `BL-161` — it
had only ever offered `NewbieBuffSet`, the levelling shelf), and the boss party now takes
`Discipline.Bulwark` on the tank and the full shelf on all five. The "party mandatory" table gained
`bare` / `tankPDef` / `tankHP` / `blocked` / `avg-swing` columns so it can be checked against a real
screen instead of trusted. **At gear tier 76 it now reads 1,795 bare / 2,962 buffed / 18,755 HP against
his 1,300 / 2,600 / 17,000 — within ~14%, against ~70% understated before.** Nothing in the game moved.

🔴 **What it revealed, and it is large:** party dps at 90 went **270 → 1,752 (×6.5)** and boss
time-to-kill went **1,274s → 196s**. Every level from 60 up now prints **TOO FAST**, and the tank
verdict from 60 up is **"a tank cannot feel it"** — at 90 a boss swing costs 1% of his pool and he
survives **327 seconds unhealed**. `BL-13`'s band and its ×4 attack multiplier were BOTH fitted against
the straw tank, which is exactly the complaint that opened this. See
[design/BossRework.md](design/BossRework.md) §2c. ⚠ Two things now want his eye: the **cliff at 80**
(party dps 534 → 1,884 across the S-grade flip, so the HP curve must climb steeply there rather than
take a flat multiplier), and the rig's S-grade tank reading **5,679 P.Def / 29k HP** where he estimated
**3,300 / 19k**.

⚠ **`BL-169`, second pass (same day) — two more rig defects, and a table that can be proven wrong.**
(1) The first "within 14%" claim compared his **level-90** tank against the rig's **level-76 row** —
fourteen levels of HP curve apart, so the agreement was partly luck. (2) 🔴 **`quality: "epic"` in
`BuildPlayer` silently meant MYTHIC**: it mapped `null or "epic"` to the bare id on a stale comment
(*"the bare id IS the Epic"*), while `ItemCatalog.QualityId` is explicit that **Mythic is the authored
item and carries no suffix**. So every default caller has been measuring a full mythic loadout, and his
"epic 76" was irreproducible. ⚠ `_mythic` is not an id — asking for it prints "missing item" and
dresses a NAKED character. Both fixed; no other caller passes a quality, so nothing else moved.

✅ **New table `BL-169`: DOES THE RIG MATCH HIS SCREEN** — one level-90 tank, his two gear sets, his own
readings hardcoded as the expected values. 🔑 It is the **only falsifiable table in the tool**: every
other one prints whatever the formulas say and cannot disagree with anything, which is precisely how a
straw tank survived for as long as the NPC buffer has existed. Result: **bare P.Def within +9%** (the
gear + passive model is right), **buffed P.Def +32/33%** consistently (the buff layer — the rig buys all
twenty-nine shelf items, his stack multiplies ×2.0 against the rig's ×2.45), and **Max HP +45/62%**,
which does NOT track the P.Def gap and is therefore a separate cause (`npc_body`, the `NpcHBody`
harmony's +30% Max HP, or the Mark). ❓ **Open on him: which blessings does he actually run?**

🔑 **The tank side no longer needs the rig** — he has given ground truth at both gear tiers
(epic A 2,600 P.Def / 17k HP; mythic S 4,300 / 20k), so the boss gets tuned against HIS numbers. On
them, a level-90 boss costs him **2.1% of his pool per swing at epic A and 1.1% at mythic S** — the
endgame tank is twice as immune as the mid-tier one, which is the ratchet this whole rework exists to
break. See [design/BossRework.md](design/BossRework.md) §2c-2d.

---

## `BL-172` ✅ BUILT 2026-09-17 in **0.172.0** — `/unstuck <name>` — a 180-second rooted channel, cast IN TOWN, on another character of the same account

The entry as it stood:


Your spec, 2026-09-05: *"'/unstuck <name>' command that have 180s cast time and is available from the
same acc to other chars (Char1 -> /unstuck Char2) and after 180s Char2 is teleported to starting town
all his equipment is unequiped all his buffs/debuffs are cleared -> don't work on baned/kicked/jailed
char"* — and your ruling on the fork I raised, same day: *"Works only in town and roots unable to act
until cast ends or canceled. It's a unstuck command not a escape mechanism -> ur char1 stuck/bug/etc
.. u create char2 and use /unstuck char1"*.

**So the shape is settled, and it is the tighter one:**
- the **caster** must be standing in a town (safe zone) — refused anywhere else;
- the **caster is rooted** for the full 180s, unable to act, exactly like a channel. Anything that
  cancels a cast cancels this;
- the **target** is another character on the same account, and the ordinary case is a character that
  is **logged out**, because you make Char2 precisely in order to rescue Char1.

That last line is the whole of the engineering. 🔑 **The target is normally NOT a live `Entity`** —
there are three states and the command has to cover all of them:
1. **Fully logged out** — no entity. The unequip / clear / teleport has to be written to the
   **persisted record**, which today is only ever written out from a live entity on logout or autosave.
2. **Still in the world** — a logged-out character keeps playing as an offline farmer
   (`IsOfflineFarming`) or sits in the link-dead grace (`IsDisconnected`). Here there IS an entity.
3. **Logged in right now** — only reachable if the server ever allows two sessions on one account.

**The design:** force states 2 and 3 down to state 1 first — evict the entity exactly as a logout
does, so nothing is lost — then apply the effect to the record. One code path, and it cannot race the
tick loop.

**The gates are already on the data.** A jail sentence is `CharacterRecord.JailedUntilUtc` (per
character); a ban is `AccountRecord.BannedUntilUtc` (per **account**), so half your "not on a banned
char" rule enforces itself — a banned account cannot log Char1 in to type the command at all. Both are
still checked explicitly, because the account ban can be lifted while a character's jail runs on.

⚠ **One thing your ruling makes free that would not have been otherwise:** because the caster is
rooted in town for three minutes, this cannot be used as an escape, a fast travel, or a way to strip a
character mid-fight — which is exactly why no other abuse gate is needed on it.


## `BL-173` … `BL-178` — six of the nine asks (cut 2026-09-06, built in 0.114.0)

Six of the eight entries filed from his 2026-09-05 messages, built in one pass. `BL-172` (`/unstuck`)
and `BL-179` (the two TEST skills) stayed open — he held them back for a later pass.


### `BL-173` 🔴 `/return` — IT ALREADY EXISTS AS A SKILL, and it wants two numbers

Your spec: *"an active skill ot /return command that returns you to town... Works like the scroll of
return just have 60s fixed cast time (if you forgot to buy) reuse can stay 10s fixed"*, and on being
shown that the skill exists: *"Like a normal rerun just 60s/10s not 30s/5m like now (I noticed each
time I put skills to bar but somehow ignore as I haven't noticed :))"*.

🔑 **This is built.** `SkillCatalog.ReturnSkill` (`return_town`, "Return") is granted to **every**
character by `AutoLearnCoreSkills` and has been on your bar the whole time. It is `TeleportsToTown`,
fixed cast, fixed cooldown, no MP, no item.

| | today | ruled |
|---|---|---|
| cast | 30s (`CastTicks: 300`) | **60s** (`600`) |
| reuse | 5 min (`CooldownTicks: 3000`) | **10s** (`100`) |
| any damage cancels it (`FragileCast`) | yes | **yes — KEPT** |

✅ **`FragileCast` stays.** I recommended dropping it; you ruled *"like a normal return"*, which means
the behaviour is untouched and only the two numbers move. That is the better call and it is worth
writing down why: fragile + 10s reuse is a **free out-of-combat return**, retryable the moment you
disengage, and it takes nothing away from either scroll — the plain scroll still buys you 10s instead
of 60s, and the Ultimate still buys you the escape from a fight you are losing. Dropping fragile would
have made the 60s version a slow Ultimate scroll and devalued both.

A `/return` chat alias for the same skill is a few lines on top and costs nothing.

---

### `BL-174` 🔴 Return and Resurrection scrolls come off ordinary mobs — elites and the starter trio only

Your spec: *"remove scroll of return and resurrection from all mobs can leave them only on elits, and
the starting 3 mobs can keep droping return scrolls and no resurrection (pig/fox/goblin)"*.

Today both scrolls sit in the **ALWAYS** group in `MobCatalog.StandardDrops`, which every creature in
the game carries — 0.025 return / 0.0025 resurrection per kill below 75, plus the Ultimate pair at 75+.
That group was already cut twice for exactly this reason (playtest 15 and playtest 17 `E1`, where 550
return scrolls by level 23 was the finding); this is the third cut and the one that finishes it. The
potions stay where they are — you only named the scrolls.

**The shape it wants** is the one `MobCatalog.EnchantScrollDrops(level, rank)` already uses: a
rank-gated layer, so a scroll is authored **once** against the rank that earns it rather than being
subtracted from a table everything shares. ⚠ `StandardDrops(level, cat)` does not currently take a
rank, so it gains one — the only structural part of this.

The starter exception is three ids: **`ridgeback_pup` (1), `fox` (4), `goblin_scout` (8)** — return
scroll kept, resurrection gone. (Your "pig" is the Ridgeback Pup.)

**Free to build, pure data.** ⚠ It cuts a faucet with nothing replacing it: from level 9 up, a return
scroll comes off an elite or off a vendor. That is the stated intent, noted here so the next playtest
does not read it as a bug. 🔑 It also raises the value of `BL-173` — the free 60s Return becomes the
thing you fall back on when you have no scroll, which is precisely the *"if you forgot to buy"* in
your own spec.

---

### `BL-175` 🔴 The admin Teleport menu — a `[Bosses]` page, and three kinds of clutter out of `Spawn zones`

Your spec: *"admin tp menu to have [bosses] whit all the bosses inside and from 'zones' all the
training dummies, all the watchmen and the bosses to be remived"*.

The menu is `GameUi.Debug.BuildDebugTeleport` — three pages today (NPCs / Spawn zones / Cities), and
the zone page walks **every** `WorldMap.SpawnZones` entry. That is why it is unusable: the six training
dummies, both towers of every town guard post (generated per city in `WorldPlan`), and the boss zones
are all in the same list as the actual hunting grounds.

Everything needed to sort them is already on the data:
- **bosses** — the zone carries `Rank: MobRank.Boss`, and `BossCatalog` is the authoritative roster.
- **dummies** — the `training_dummy` / `dummy_magic` / `dummy_physical` mob ids.
- **watchmen** — the guard posts are generated by `WorldPlan` with the guard mob ids.

So: a fourth page `Bosses >` built from the boss-ranked zones, and the other three classes filtered out
of `Spawn zones`. **Free to build, client-only.** ⚠ **NEW APK** — the menu is built on the phone.

---

### `BL-176` 🔴 The admin Items tab — three flat walls become sub-pages

Three of your asks, merged into one entry because they are the same change to the same page and one
commit; say so and they split back into three ids:

- *"admin menu items to have group of attribute scrolls -> click attri scroll and opens all of them
  like a selection"*
- *"admin menu items ecnahnt scrolls to be one button and then selection per grade"*
- *"admin menu items to have buttons with potions/stones -> potions have all healing/mp potions,
  stones to have all stones skills and holy/etc."*

All three are the same complaint and it is a fair one: `BuildDebugItems` prints **eighteen** enchant
scroll rows under six grade headers, six attribute scroll rows, and scatters the potions across three
headers — one screen, no grouping, on a phone. The page already knows how to do sub-pages (`Crafting
materials >` and `Blueprints >` are exactly this, via `_debugItemsView`), so the pattern is in the file
and this is applying it four more times:

| button | opens |
|---|---|
| `Enchant scrolls >` | a grade picker (F…S), then that grade's three types |
| `Attribute scrolls >` | all six rarities |
| `Potions >` | every healing **and MP** potion rung |
| `Stones >` | Skill Stone, Elemental Stone and the rest of the reagent line |

⚠ **One thing to check while building, not to assume:** the enchant scroll rows are generated from
`ItemCatalog.EnchantScrollBands` × `EnchantScrollTypes` precisely so a scroll cannot be authored and
left unreachable in the menu. The new picker must stay generated the same way — a hand-written grade
list would lose that guarantee the first time you add a grade.

**Free to build, client-only.** ⚠ **NEW APK.**

---

### `BL-177` 🔴 The admin SP button gives 10kk, not 1kk

Your spec: *"admin menu function SP button to give 10kk not 1kk"*. It is one number in
`GameUi.Debug` — the Gold button beside it already gives 10,000,000, and its comment gives the reason
("a smaller button could not fund a single meaningful purchase to test with"), which now applies to SP
just as much: a 4th-class skill ladder costs far more than 1kk to walk. **Free, one number.**
⚠ **NEW APK** (it rides with `BL-176`).

---

### `BL-178` 🔴 THE ANDROID SELECTION MENU IS MISSING BECAUSE WE TURNED IT OFF — and it was to fix a bug of yours

Your note, 2026-09-05, sharpened on the second pass: *"I can paste from the keyboard copy clipboard
(SwiftKey keyboard) but the context menu after selection that shows 'copy/cut/select all' is not there
or the one that 'paste' if something is pending. and not shown on the keyboard as well. I do not want
new inner copy/paste system if we can make the normal work"*.

🔑🔑 **Found it, and it is one line we set deliberately.** `UiKit.InputField` sets
**`field.shouldHideMobileInput = true`** on every text field in the client. With that on, Android's
real `EditText` is off-screen and the soft keyboard only delivers keystrokes; TMP owns the buffer and
draws its own caret and its own selection highlight. **The copy / cut / select-all / paste context
menu belongs to that hidden native view**, so it can never appear — which is exactly your symptom:
selection works (that is TMP's), the clipboard menu does not, and paste only works when the *keyboard*
sends it as keystrokes (your SwiftKey clipboard), because that is the one path that still goes through.

⚠ **And the line is there because of you.** 0.47.0, your report: *"if there is a 1 I cannot make it 10
— it becomes 01"*, plus a saved password that could not be edited at all. On Android the soft keyboard
owned the text buffer, so tapping inside a field could not move the caret and there was no way to
reach the character you wanted to delete. `shouldHideMobileInput = true` is what fixed that. **So the
native context menu and the working caret are the same switch, pointing opposite ways.**

**The way to have both, and it is your "make the normal work":** flip the switch **per field**, not
globally — `false` for the **chat entry box alone**, `true` everywhere else. Chat is the one field
where you type fresh text rather than edit a pre-filled value, so the 0.47.0 caret bug has almost
nothing to bite on there, while the URL / password / gold-amount / tune fields — the ones that bug was
actually about — keep the behaviour that fixed them. It is an optional argument on
`UiKit.InputField` and one call site.

⚠ **What changes visually, and why it needs a look on the device before it ships:** with
`shouldHideMobileInput = false`, Android puts its **own** input strip above the keyboard and that is
where you type and where the context menu appears — our on-screen box is no longer the thing being
edited while the keyboard is up. That may also make the keyboard-lift offset on the chat row
(`GameUi.World`, the code that clears the punch-hole) pointless for that one field. Neither is a
blocker; both are things to see rather than reason about.

🔵 **Separately, and NOT covered by this:** copying a name *out of the chat log* is still impossible —
the log is a plain `TextMeshProUGUI` inside a `LogView` with no selection support at all. If you want
that too, say so: the usual trick is a read-only `TMP_InputField`, which is a different (small) change
to a different widget.

---

## `BL-180` … `BL-182` — the admin buff drawers, FullHeal, and staff flags that survive a relog (cut 2026-09-06, built in 0.115.0)

Three asks from one message, 2026-09-06, all built in the same pass.

---

### `BL-180` 🔴 `Functions > [Buffs]` — four drawers over every buff in the game

Your spec: *"can you make in functions under the buffsbuttons - add [buffs] -> sub menu to open with 4
more submenues -> single, group, harmonies, marks … Functions -> Buffs -> marks ->
[life][blood][holy][harmony] · Functions -> Buffs -> Harmonies -> [Warrior][Wizard] …. [Fury][Focus]…
· Functions -> Buffs -> Single -> [Might][Bulwark]….[Frenzy]…. · Functions -> Buffs -> Groups ->
[Arcane Insight]…[War frenzy]…."*, and on the six buttons that were already there: *"Functions ->
[Full buff][War might][War bulwark] - Can remove the 4 harmonies as they will be insoide their
colection and fullbuff gives harmony mark anyways"*.

**What it was:** six hand-written buff buttons on the Functions tab, and every other buff in the game
reachable only by typing `/buff <name>` on a phone keyboard.

**What it is now:** `Buffs >` opens the four drawers — **Singles (31) · Groups (9) · Harmonies (14) ·
Marks (4)**, fifty-eight buttons. Each is `/buff <id>`, which is that buff's **top rung for one hour**,
the same thing the Full Buffs button hands out and the state the balance numbers are read at. The four
Mark buttons left the Functions tab as you asked; `[Full Buffs]`, `[War Might]` and `[War Bulwark]`
stayed, because those two still share one buff key and the button is how you swap them.

🔑 **The four lists are DERIVED, not typed** (`SkillCatalog.AdminBuffMenu`). Same rule the gear, town,
zone and class lists on that window already run on, and for the same reason the hand-listed WPF menu
proved: a typed list goes stale and whole tiers silently vanish from it. Add a harmony to `buffer
3rd.csv` or a Mark to `buffer 4th.csv` and its button appears with no second edit.

- **the universe** = the two shelves the game has, unioned: what a max-level buffer CLASS can cast
  (`AdminBuffSet`, plus the three a full buff deliberately withholds — Shrouding Hymn, Bow Expertise,
  War Bulwark) and what the Spirit Helper SELLS (`NewbieBuffSet`, which carries the eight single
  harmonies and the three Marks since `BL-160`/`BL-161`). It READS both and writes to neither, which
  is the only relationship those two separate shelves are allowed to have.
- **which drawer** is asked of the data, and the order of the tests is the design: a **Mark** by its
  shared buff KEY (so the Harmony Mark files as a Mark, where you listed it, and not as a harmony); a
  **harmony** by its NAME, because that is the only thing the twelve share — the four class harmonies
  carry `Magnitudes` and the eight NPC ones are one-child wrappers, so no structural test sees both;
  a **group** by STRUCTURE (more than one child), the same test that puts groups first in a full buff.
- **one name, one button.** "Might" is the Warchanter's own ladder AND the NPC's hour-long single, so
  the union is deduplicated by display name, strongest first — exactly what typing the name already
  does, so button and command land the same buff. Nineteen NPC duplicates are shadowed that way.

📐 `dotnet run --project tools/BalanceMatrix -- --buffmenu` prints all four drawers and names anything
grantable the menu fails to reach. Finding a mis-filed harmony on a phone is how one survives three
versions.

⚠ One server change went with it: an **exact skill id now wins outright** in `/buff`'s name match,
before the fuzzy ladder of acronym / words-in-order / prefix / substring. Ids are unique so it can
never be ambiguous, and fifty-eight buttons was too many to leave riding on a fuzzy search.

---

### `BL-181` 🔴 `FullHeal` — both pools to full, instantly, in combat

Your spec: *"Functions -> FullHeal -> heals instantly mp/hp in combat or no"*.

A button on the Functions tab and `/heal [name]` behind it.

🔑 **It is a SET, not a heal.** Going through the healing path would drag in everything that makes a
heal interesting and useless here — the healing-received modifiers, the potion cooldown, the in-combat
refusal, the aggro a heal generates — and the point of the button is a known starting state *mid-fight*.
None of that is a game rule being sidestepped by accident; all of it is the request.

⚠ **It is not a resurrection.** Death has its own path (`BL-173`'s return flow), and a command that
silently did both would make "did that kill me?" unanswerable in a test. On a dead character it says so.

---

### `BL-182` 🔴 `/god` and `/invis` survive a relog

Your report: *"also can /invis and /god be persistant … after a DC(long stay in background) the char
that is incis+god is visible and mortal .. whatever i left my admin/owner with he stais again in the
next login/reconnect"*.

**Why it happened.** A short disconnect re-attaches to the LIVE entity and always kept both flags; a
long one evicts the character to the database, and neither flag was ever stored — so the state was
lost at exactly the moment you noticed it, which is why it looked intermittent.

Two new columns on `CharacterRecord`, written the instant either command is typed rather than at the
next autosave (a crash between the two is the case this entry is about).

🔑 **They are re-applied only if the character is still an ADMIN**, and the load does it *after* the
role. `IsAdmin`, not `IsStaff`: both commands sit on the admin side of the allow-list, so a Moderator
could never have set either. `/role` clears god mode on a *live* demotion, but a character demoted
while offline would otherwise log back in immortal and unseeable off a row nobody could reach. The
stored bits are not wiped by that refusal — a re-promotion restores exactly the state you left.

⚠ Nothing new pushes the badge or the 0.4 fade: the tick loop's own change-test (`PushSelfState`)
sends it on the first tick after you enter the world, which is the same mechanism that covers hide and
stealth. No second path to keep in step.

🔴 **A schema change — `game.db` must be deleted.** `EnsureCreated()` only creates a database that is
absent; it never adds a column.

---

## `BL-183` — a harmony is a GROUP over the eight single harmonies (cut 2026-09-06, built in 0.115.1)

Your ruling, 2026-09-06: *"Harmony of swift should not stack with harmony of speed. Harmony of warrior
replaces harmony of fury, harmony of might. Same goes hor harmony of (body,ward,bulwark) == harmony of
protection. Think of the as single harmonies and group harmonies -> group buffs replaces singles."*

**That was already the intent, and it had never once worked.** `BL-160` gave the Spirit Helper the
eight single harmonies and the four class harmonies were written to tear them off — the note in the
code even quoted you: *"his acts as a group one so replaces them"*. It was expressed as
`SkillDef.Replaces`, and `ApplyBuff` matches that list against buff **keys** while every author in the
catalog writes skill **ids** into it (its five other call sites — collapsing a superseded skill off the
learn list — need ids). The rows held `npc_harmony_swift`; the buff on the bar was keyed `npc_h_swift`.
Nothing matched, nothing was removed, and the two tiers stacked in silence for three versions.

**What it is now.** A new field, `CoveredKeys`, lets a childless buff declare the families it contains
— the thing a GROUP gets for free from its children and a harmony could never say, because a harmony
carries magnitudes rather than children. With it declared, the ordinary family contest does both
halves of your sentence: the class harmony **evicts** the single when it lands, and the Spirit Helper
**refuses to sell** it (and does not charge) while the class one stands.

🔑 **Rank had to move too.** Both tiers sat at rank 100, and at equal rank the engine keeps whichever
has longer left — the bought single runs an **hour**, a class harmony **five minutes**, so the rule
would have resolved backwards and the 50k single would have refused the Warchanter. Class harmonies
now sit one rank above the shelf they cover.

🔑 **It is per RUNG, not per skill.** A harmony's payload is cumulative and each single goes on sale at
exactly the level the harmony gains that effect, so Harmony of Protection claims Harmony of Ward from
rung 1 (@44), Bulwark only from rung 3 (@56) and Body only from rung 4 (@66). Covering all three from
rung 1 would let a level-44 Warchanter strip a level-56 player's 50,000-gold Harmony of Bulwark and
hand back nothing — a downgrade the player cannot refuse. Rung by rung the swap is exactly even: the
harmony's number at that rung and the single's number are the same number.

⚠ **`Replaces` was removed from the four harmonies rather than repaired.** Even fixed it is
unconditional and per-SKILL, so it would have broken the rung rule above. Its id→key resolution *was*
fixed in the engine as well, so any other author who wrote ids there now gets what they meant.

📐 **Measured, not asserted** — `dotnet run --project tools/BalanceMatrix -- --buffs` prints the
covering ladder rung by rung against the level each single sells at, and warns if any of the eight is
covered by nothing. Startup throws on a covered key that names no real buff.

No schema change; no new APK (the rule is entirely server-side).

---

## `BL-184` — a Clear All in Functions and at the Spirit Helper, free (cut 2026-09-06, built in 0.115.1)

Your ask: *"Add a clear all in the functions menu and in the npc buffer (free) to remove all active
effects (no debuffs)"*. Both places, and the NPC one is free.

**Functions** now has `CLEAR ALL BUFFS (debuffs stay)` directly under the buff buttons — the way back
from them. Everything else on that page puts something ON, and the only routes off were waiting an
hour or relogging, which **restores** buffs and therefore never worked. Both the buffed and the
unbuffed state are things a balance read needs; only one of them had a button.

**The Spirit Helper** gets `Clear all blessings   free`, under Restore. It is the missing half of your
own preset workflow (`BL-95`: *"the idea is to buff fully from npc then remove what u don't need as
that class and save it"*) — removing one square at a time was possible, starting over was not, and
with a twenty-slot bar the usual reason to start over is that you filled it with the wrong set.

⚠ It **asks before it fires**. Everything else on that window adds something and is undone by pressing
it again; this is the one row that destroys blessings you may have paid 50,000 gold each for, and it
sits directly under a row you came there to press. Free to run, expensive to run by accident.

⚠ It **stays free**, and that is a rule rather than a price point: the value of the button is that
pressing it is never a decision. Charge for it and "clear and rebuy" becomes strictly worse than
logging out, which is the behaviour it exists to remove.

`/clearbuffs [name]` is the command behind both, so it works from an old client and can be aimed at
someone else.

🔑 **What survives.** The filter is `BuffInstance.IsDebuff` — the same one every cure and cancel path
already uses, and deliberately *"carries no harmful flag"* rather than *"carries a buff flag"*: half
the payloads in this game are fields rather than flags, so the second reading would quietly skip them.
Also kept: **internal** effects (DoT stack counters — bookkeeping, never on the bar), anything flagged
**not cancellable** (Burn, the boss judgment), and **rune buffs**, which are the item in your bag being
worn rather than something you were given — the reconciliation loop re-applies one within the second,
so clearing it would only flicker the bar and inflate the count.

⚠ **Toggles DO go** — a stance is an active effect and you said all of them. Removing the buff is
exactly how a toggle is turned off already, so nothing is left behind claiming it is still on.

⚠ **NEW APK** (two buttons). No schema change.


---

## `BL-187` ✅ CLOSED 2026-09-10 (0.124.3) — ONE COMBINED RUNE, PREMIUM, ADMIN-ONLY

**His ruling, in one line:** *"build one rune that stays in admin menu and its 1d rune that combine
both. it will be prmium curency and close the quesion"*.

Built as **Grand Rune** (`rune_grand`) + **Grand Rune Box (1d)** (`box_grand_rune_24h`):

- `PhysDamageMult: 2.0` **and** `MagicDamageMult: 2.0`, plus the Spell Rune's flat +40 cast speed.
- **24 hours, one rung.** No 1h/2h/30d ladder — he asked for one item, not a fourth column.
- **Not buyable, not tradable, not dropped.** Its only route into a bag is the Admin panel's new
  "Runes (premium)" row; when a premium currency exists, this is the item it buys.

🔑 **Three of the entry's four open questions were answered by the DELIVERY MODEL rather than by a
number.** It asked whether the combined rune should be weaker per channel so as not to dominate the
singles, who it is for, and where it sits on the price ladder. Making it premium and admin-only
removes the ladder it would have had to compete on: it never appears beside the two vendor runes, so
a hybrid gets both channels at full strength and a pure class gets exactly what its own single
already gave it. Nobody is taxed and nobody is trapped. The fourth — the name — is **Grand Rune**,
which is a generic tier word in nobody's slot.

🔴🔑 **THE SUPERSEDING RULE IS NOT `CoveredKeys`, AND THE ENTRY'S RECOMMENDATION WOULD HAVE MISFIRED.**
`BL-187` proposed the `BL-183` group shape — declare `CoveredKeys` over `rune_war`/`rune_spell` and
outrank them. That is right for a CAST buff and wrong for a rune, because rune buffs are owned by
`ReconcileRuneBuffs`, which re-derives them from the held items about once a second. Under a covering
rank the loop would try to apply the War Rune on every pass, have `ApplyBuff` refuse it as outranked,
find the buff still missing, and mark stats dirty — a `SendStats` every second for as long as a
player held both. The rule instead lives where the WANTED SET is built: hold a Grand Rune and the two
singles are dropped from it once, and the removal step takes their buffs off. The items are untouched
— a superseded War Rune keeps ticking down in the bag and comes back by itself when the Grand Rune
expires.

<details><summary>The entry as filed, 2026-09-09</summary>
## `BL-187` 🔵 A THIRD RUNE THAT COMBINES BOTH CHANNELS

**Filed 2026-09-09, on your instruction** while the runes were rebuilt as damage multipliers
(`BL-185` step 1): *"add a note to make 3rd rune that combines both"*.

The engine side is already done and this is now a **catalogue entry, not a mechanic**: `SkillDef`
carries `PhysDamageMult` and `MagicDamageMult` independently, `Entity` compounds each channel
separately, and `GameLoopService.FinalizeDamage` picks the channel per hit. A combined rune is one
`SkillDef` setting **both** fields plus one `ItemDef`.

**What is still owed is yours to decide, because all of it is economy, not code:**

1. **The multiplier.** Same ×2 on both channels, or less on each (e.g. ×1.7/×1.7) so the combined
   rune is convenience rather than a strict upgrade? A flat ×2/×2 strictly dominates both singles for
   any hybrid, and equals them for a pure class.
2. **Who it is for.** Only a hybrid actually uses both channels. A pure nuker gains nothing over a
   Spell Rune, so priced equal to a single it is a trap, and priced above it is a tax on hybrids.
3. **Price and duration ladder.** The singles run 1h/2h vendor-bought (150 000 / 280 000 gold,
   tradable sealed) and 24h/30d premium (not buyable, not tradable). Same four rungs, or fewer?
4. **The name**, which must pass the `word + SAME RACE + SAME ROLE` test.

⚠ **Stacking is already decided by the existing rules and needs no new mechanic** — but check it
reads the way you want: the two singles have distinct `BuffKey`s (`rune_war` / `rune_spell`), so a
combined rune either takes a third key (and then all three stack, which is almost certainly wrong) or
declares `CoveredKeys` over both and outranks them by `GroupRank`, exactly as a group buff covers its
singles (`BL-183`). **The second is the right shape**; it just has to be authored.

</details>

---

## `BL-188` ✅ CLOSED 2026-09-09 (0.121.0) — THE BLOW LANDING RATE BECAME ITS OWN STAT

**Built the same day it was ruled.** The blow half shipped in 0.121.0 — see the CHANGELOG entry for
the model, the ladder and the measurements. The `[Double]` half was NOT built and moved to `BL-190`
on his instruction: *"Move the double to bl entry - I will want to be a passive to allow skills to
double .. Not all based on atk stat (only if passive is active)"*.

What shipped: `blowRate = clamp(0.30 × buffs × passives × BlowAgiMod(AGI), 20%, 80%) × (1 − BlowResist)`,
five new skills (Lethal Focus / Precision / Frenzy at 40/60/70, Vital Points at 52/64/74,
Assassination Instinct at 76, the Perfect/Brutal Strike choice at 80) and the tank's Vital Organ
Protection at 80. All fourteen authored numbers are read by `--check`. Measured with
`BalanceMatrix --blowrate`.

🔑 **The lesson worth keeping is that THE FIRST VERSION OF THIS ENTRY WAS WRONG IN BOTH HALVES**, and
it was wrong because it was written from the CSVs without reading the engine. It claimed the blow roll
hit a 50% cap (it clamped to **100%**) and that the 3rd-tier kits push ATK past `[Double]`'s 60-point
saturation (no player exceeds **41**). Its full original text is below.

<details><summary>The original entry, as filed and then rewritten on 2026-09-09</summary>

## `BL-188` 🔴 THE BLOW LANDING RATE BECOMES ITS OWN STAT — and `[Double]` is frozen at ~10%

**Filed 2026-09-09, on your instruction**, while the melee-rogue and archer 3rd kits were built:
*"also make a note to fix the skill double and blow crit chance"*. **Rewritten the same day** once
the code was actually read and measured, and once you ruled the blow model — the first draft's
diagnosis was wrong in both halves and is archived under this id.

### What the two paths read TODAY

| | rolled off | formula | clamp | where |
|---|---|---|---|---|
| `[Double]` (×2 damage) | the **raw `AtkStat`** | `min(25%, 2.5% + 0.75·(ATK − 30))` | 25% at ATK 60 | `StatCalculator.PhysicalDoubleChance` |
| a **BLOW** landing (`BlowOnCrit`) | the character's **crit rate** × the skill's `CritRateMod` (an unauthored 2.0) | the ordinary physical crit chain, doubled | **100%** — `Math.Clamp(…, 0f, 1f)` | `GameLoopService.ResolveBlow` |

Both flags are OPT-IN and mutually exclusive on the *gate* (playtest-19 M8: *"if a skill is not
described as Can Crit or Can Double it doesn't do it"*), and a blow does not set `CanCrit` — the
crit roll IS its landing gate. On a blow the double is a live SECOND roll after the crit lands, for
a further ×2; that part works and is not in scope here.

### The two real defects (measured, `--dmgmatrix … --his`, mythic gear)

**1. The blow roll has no 50% cap — it runs to 100%.** `ResolveBlow` clamps to `0f, 1f`, not to
`StatCaps.PhysicalCritRate`. `CritChance` is already clamped to 50% and then multiplied by 2.0 on
top:

| Nullblade | crit rate | blow lands |
|---|---|---|
| 74 unbuffed | 19.4% | **38.8%** |
| 74 buffed | 25.2% | **50.4%** |
| 78 buffed | 30.3% | **60.6%** |
| 85 / 90 buffed | 40.3% | **80.6%** |

At the 50% crit cap it is **100% — every stab lands**. That is playtest-19 M9's *"each blow lands
with the 64+% chance"* arriving again from the other direction. `CritRateMod: 2.0` is authored
nowhere: the stab rows say only *"can crit/double"*, and the `crit rate x1.3 / x1.4` on Dual Mastery
is the different knob (`CritRateMult`, folded into `CritChance`).

**2. `[Double]` is a per-race constant that nothing in the game can raise.** Two independent causes:

- **No player is near ATK 60.** Base fighter ATK is Elf 36 / Human 40 / Demon 41
  (`StatCalculator.GetBaseStats`) → 7.0% / 10.0% / 10.75%. The 25% cap is unreachable.
- **The call passes the raw stat, not the effective one** — `PhysicalDoubleChance(caster.AtkStat)`,
  while its crit twin uses `PhysicalCritBase(EffectiveAgi, …)`. `EffectiveAtk = AtkStat + BonusAtk`,
  and `BonusAtk` is where the level-40 `+5 ATK` stat swap, the armour sets and every `+ATK` passive
  land. **None of them buy any Double chance.** The doc comment defends the raw read (*"a better
  weapon must not buy Double chance, only the build does"*) — but `EffectiveAtk` is not p.Atk, it IS
  the build. This reads as a slip, not a design.

⚠ **Before touching that curve:** the same function also drives **double buff/debuff DURATION**
(`GameLoopService.cs:11742`, IG's level-76 Skill Mastery). Fixing the stat feed raises that too.

### ✅ YOUR RULING on the blow — 2026-09-09

**The blow landing rate becomes its OWN stat, divorced from crit rate.**

```
blowRate = 0.40 × (blow-rate buffs/passives) × dexMod        clamped to [20%, 80%]
dexMod   = 1 + 0.03 × (AGI − 30)        AGI clamped to [20, 40]  →  ×0.70 … ×1.30
```

The ladder you named, and it reproduces exactly:

| source | factor |
|---|---|
| base | 0.40 |
| *"a few of the skills I authored"* — retro-fitted rows | ×1.20 |
| one **buff** | ×1.20 |
| a **level-80 passive** | ×1.05 |
| **every rogue ends at** | **60.5%** |
| ELF, additional | ×1.10 |
| your worked example, AGI 40, all buffs | `0.4×1.2×1.2×1.05×1.3` = **78.6%** |

✅ **The anchor already exists in the code.** `StatCalculator.MobAgiReference = 30` is the same 30,
and `CritAgiMod` is `1 + 0.01·(AGI−30)`. Your `dexMod` is literally that mod **at 3× the slope**
with a ±30% clamp — one new constant, no new anchor, and the human fighter's base AGI is 30 on the
nose, so an unswapped human is exactly ×1.00.

⚠ **AGI gains a FIFTH job, and it is now its biggest.** `CritAgiMod` carries a standing guardrail —
*"deliberately the SMALLEST of AGI's four jobs … +0.13pp of a dagger's crit … Do not inflate it"*.
The blow mod does not violate that comment (it is a different function), but one AGI point now buys
+1.8pp of landing on a rotation that is one roll. For the dagger rogue AGI becomes decisively THE
stat. That is probably what you want for the AGI class; it should be a decision, not a discovery.

**Also ruled:** the **tank gets stab protection** — a *decreasing chance* of blows landing on him.
Note this is a different shape from `BowResist`, which cuts DAMAGE taken, not a roll. The general
case is `BL-189`.

### What is still owed before this can be built

1. 🔴 **The blow-rate sources do not exist in any CSV.** Nothing anywhere authors a blow rate today —
   every `crit rate x1.2 / x1.3 / x1.4` in `rogue 2nd`, `dual 3rd` and `archer 3rd` is crit rate
   proper. **The ×1.2 skill rows, the ×1.2 buff and the ELF ×1.1 are yours to author**, and the
   level-80 ×1.05 passive's home — `dual 4th.csv` — is still the two-line placeholder.
2. ❓ **The ELF ×1.1 double-counts with `dexMod`.** The elf fighter's base AGI is 36 → `dexMod`
   already ×1.18 against the human's ×1.00. Adding a racial ×1.1 puts the elf at ~78.5% where the
   human is 60.5%, and with a `+5 AGI` swap the elf is clamped by the 80% ceiling while the human
   sits at 69.6% and the demon (AGI 28) at ~57%. Is the ×1.1 meant to sit ON TOP of that, or is it
   the AGI lead expressed a second way?
3. ❓ **Clamp order against the tank's stab protection.** If `[20%, 80%]` is applied AFTER the
   defender's protection, a tank can never push a rogue below 20%. If BEFORE, he can. Which?
4. ❓ **Does a landed blow still use the crit-DAMAGE values?** Today it does — the flat crit-damage
   add plus `PhysicalCritMult`, which is what makes a stab scale off crit damage rather than p.Atk.
   The ruling divorces the GATE from crit rate; it says nothing about the damage model.
5. ❓ **`[Double]` is unruled.** The `AtkStat` → `EffectiveAtk` feed is a plain bug and can be fixed
   on its own (re-measure after — it moves buff-duration doubling too). Whether the 30-60 band and
   the 25% cap are right for a stat that lives at 36-41 is a separate question for you.

**Do not retune the CSV powers to compensate** — the powers are authored, the roll rate is not.

---

</details>

---

## `BL-190` ✅ CLOSED 2026-09-10 (0.123.0) — `[Double]` IS PASSIVE-GATED, AND IT BROUGHT TWO SIBLINGS

**Built the day the last three questions were answered.** Your ruling, in your own words:

> *"I want several passives .. One that resets cooldown of skills, one that doubles duration of bad
> and good buffs, and one that allow double dmg ... All will calculate the same just the base is
> based on the passive."*

and, on where the rate comes from and who has it: **the passive carries the base, ATK is only a band
around it**, and **nobody has one until you author it** — the entry's three ❓ answered in that order.

What shipped (0.123.0, see the CHANGELOG for the numbers):

```
MasteryAtkMod = 1 + 0.03 × (clamp(EffectiveAtk, 30, 50) − 40)      ×0.70 … ×1.30
rate          = 0                                                   when no passive grants it
              = clamp(passiveBase × MasteryAtkMod, 0, 25%)
```

Three channels, identical math, one cap: `Entity.DoubleDamageRate` (a `CanDouble` skill deals ×2),
`DoubleDurationRate` (a buff or debuff you cast lasts twice as long, one roll per cast) and
`CooldownResetRate` (the skill just cast comes straight off reuse; never on a `FixedCooldown` skill).
`PassiveEffect` grew the three matching fields and they SUM.

Answers to the four things the entry said were owed:

1. ✅ **The ATK curve does NOT survive as the rate** — the passive carries it, ATK is the ±30% band.
   The blow ladder's proven shape, as the entry predicted it would be.
2. ✅ **`CanDouble` stays as "eligible"** and no per-skill `RequiresDoublePassive` was added: the
   character-level rate is the gate, so *"not all"* is expressed by which passives a kit grants.
3. ✅ **Duration doubling is its OWN passive and its own number.** It was the same roll as damage,
   which is what made it the thing most likely to be missed; splitting it was done FIRST, before
   either rate was touched.
4. ✅ **The `AtkStat` → `EffectiveAtk` slip is fixed** — the band reads the effective stat, so the
   level-40 +5 swap, the armour sets and every `+ATK` passive finally count for something here.

🔴 **What this deliberately leaves behind is `BL-191`: nothing in the CSVs authors a mastery passive,
so every character in the game reads 0% / 0% / 0% and no skill doubles.** That is your ruling
("Nobody — dead until you author it") and not a regression to be quietly patched with a default.

<details><summary>The original entry, as filed 2026-09-09</summary>

## `BL-190` 🔴 SKILL `[Double]` — IT MUST BE GATED BY A PASSIVE, NOT BY THE ATK CURVE

**Split out of `BL-188` on 2026-09-09**, on your instruction, when the blow half of that entry was
built and the Double half deliberately was not: *"Move the double to bl entry - I will want to be a
passive to allow skills to double .. Not all based on atk stat (only if passive is active)"*.

### Your ruling, as far as it goes

A skill may `[Double]` **only while a PASSIVE that grants it is active**. `CanDouble` on the def stops
being sufficient on its own and becomes "this skill is *eligible* to double"; whether it actually can
is a question about the CHARACTER. The ATK curve is not the gate any more — you did not say whether it
survives as the *rate*, which is the first open question below.

### What is there today, measured (not derived)

```
Double% = clamp(2.5% + 0.75 × max(0, ATK − 30), 2.5%, 25%)      StatCalculator.PhysicalDoubleChance
```

Three facts about it, all confirmed in the code on 2026-09-09:

1. **It is a per-race CONSTANT that nothing in the game can raise.** Base fighter ATK is Elf 36 /
   Human 40 / Demon 41 (`StatCalculator.GetBaseStats`) → **7.0% / 10.0% / 10.75%**. The 25% cap needs
   ATK 60 and is unreachable by anyone.
2. 🔴 **The call passes the RAW stat, not the effective one** — `PhysicalDoubleChance(caster.AtkStat)`,
   while its crit twin uses `PhysicalCritBase(EffectiveAgi, …)`. `EffectiveAtk = AtkStat + BonusAtk`,
   and `BonusAtk` is where the level-40 `+5 ATK` swap, the armour sets and every `+ATK` passive land.
   **None of them buy any Double chance.** The doc comment defends the raw read ("a better weapon must
   not buy Double chance, only the build does") — but `EffectiveAtk` is not p.Atk, it IS the build.
   This reads as a slip rather than a design, and it is a one-word fix.
3. ⚠ **The same function drives DOUBLE BUFF/DEBUFF DURATION** (`GameLoopService.cs:11742`, IG's
   level-76 Skill Mastery — an area blessing doubles for everyone or for no one, rolled once per cast,
   players only). **Anything done to that curve moves buff durations too**, and that is the thing most
   likely to be missed.

`[Double]` is flagged on most physical actives today (`Skills.Fighter.cs`, `Skills.Dual3rd.cs`). On a
BLOW it is a live SECOND roll after the blow lands, for a further ×2 — that part works and `BL-188`
left it alone.

### What is owed before it can be built

1. ❓ **Does the ATK curve survive as the RATE, with the passive only as a gate — or does the passive
   carry its own rate the way the blow ladder does?** The blow rework did the latter and it worked
   cleanly (base × buffs × passives × stat mod, capped), so the shape exists and is proven.
2. ❓ **One passive for everything, or one per weapon/class?** *"Not all"* says some skills stop
   doubling; a single global passive cannot express that, but a per-skill `RequiresDoublePassive` flag
   plus one passive can.
3. ❓ **Does the buff/debuff DURATION double follow the same gate?** It is the same roll today. If it
   does not, it needs its own number and stops being free.
4. 🔵 **The `AtkStat` → `EffectiveAtk` slip can be fixed on its own, before any of this** — it is a bug
   in either design. Re-measure after: it moves duration-doubling too.

**Do not retune skill powers to compensate** — the powers are authored, the roll rate is not.

</details>

---

## `BL-191` — question 1 (Blood Rage's level) ANSWERED 2026-09-10, same day (0.124.1)

He answered it himself, in the only place that counts: he edited **81** into the `LEARN @ LVL` column
of `warrior 4th.csv` and `war_aoe 4th.csv`. Asked why, he gave the reason the next message:

> well some toggles will cost 50 hp .. doesnt matter if its 76/78/81 .. etc lvl .. its a toggle that
> will cost hp and give other benifits .. and i dont want at 76 lvl warrior to start doubling at 25%
> .. until 81 he is at base 10% .. at 81 then gets the toggle and become stronger

🔑 **The five levels are a PLATEAU, and that is the whole design.** Overpower's last rung lands at 76
and leaves the warrior on a 10% base (≈13% after a high-ATK band) for five levels; at 81 the toggle
doubles the base to 20%, the band carries it to 26%, and `StatCaps` trims it to **25%** — the ceiling
reached exactly once, at the end of the climb. Shipped at 76 it would have been reached on the day of
ascension with nothing left above it.

⚠ **The number 81 is not sacred — the ORDER is.** *"doesnt matter if its 76/78/81"*. If it ever
moves, it moves *later* than Overpower's top rung, never onto it. The price follows for free: 200kk
SP + 25kk gold is what `F4New(81)` returns, which is what he authored in the CSV to the digit.

The open text it replaces:

<details><summary>As filed 2026-09-10</summary>

1. 🟡 **BLOOD RAGE'S LEVEL IS AN ASSUMPTION — the only one in the build.** You gave the toggle its
   effects but no learn level, in a sub-bullet under the 20/40/76 ladder. It is at **76**, because
   Holy Soul (the only other 50 HP/s toggle in the game) is a 76 skill and because 50 HP/s at level
   20 kills a warrior in under a minute. **Say the word and it moves to 40** — it is one line in
   `ClassSkillTables.Fourth.cs` and two CSV rows.

</details>

🔑 **The lesson, for the next time I am tempted to pick a number for him:** the guess was defensible
(Holy Soul, the only other 50 HP/s toggle, is a 76 skill) and it was still wrong, because I picked
the level off the skill's *cost* while he picked it off the *ladder it sits on*. Flagging it as an
assumption in three places is what made it a one-line fix instead of a silent mis-tuning.

⚠ And the first correction I wrote was *also* wrong — I read the 200kk price as the point ("he gates
the toggle behind its own rung") when the price is merely what `F4New(81)` happens to return. **A
plausible rationale invented for someone else's number is still an invention**; the fix was to ask,
which took one line and got the real answer.

---

## `BL-191` — its original text, superseded 2026-09-10 the same day it was filed

He authored all four passives within the hour, so the "nothing grants them" entry never described a
state that survived a build. Its replacement in `Backlog.md` holds only what is still open. The
original follows.

<details><summary>As filed 2026-09-10</summary>

## `BL-191` 🔴 THE THREE SKILL MASTERIES EXIST AND NOTHING GRANTS THEM — the passives are yours to author

**Filed 2026-09-10, the moment `BL-190` shipped**, and it is the one thing that entry deliberately
left open. The engine is finished; the CSV rows are not written, and by your own ruling they are not
mine to invent.

### What is built and working

Three passive channels, one piece of math, one cap (`StatCaps.SkillMasteryRateMax` = 25%):

```
MasteryAtkMod = 1 + 0.03 × (clamp(EffectiveAtk, 30, 50) − 40)      ×0.70 … ×1.30
rate          = 0                                                   when no passive grants it
              = clamp(passiveBase × MasteryAtkMod, 0, 25%)
```

| Channel | What a hit of it does | When it rolls |
|---|---|---|
| `DoubleDamageRate` | a physical skill flagged `[Double]` deals **×2** | per hit; on a BLOW it is the second roll, after the blow lands |
| `DoubleDurationRate` | a buff or debuff **you cast** lasts **twice as long** | once per cast (an area blessing doubles for everyone or no one), player casts only |
| `CooldownResetRate` | the skill just cast comes **straight off reuse** | once per cast, after the reuse reduction; never on a `FixedCooldown` skill (Return, the ultimates) |

Authoring one is a single field on a `PassiveEffect` — `DoubleDamageRate: 0.10` is "10% before the
ATK band". They SUM across passives, so a rung ladder works the normal way (each rung replaces the
one below through the skill-level machinery) and two *different* masteries add.

### 🔴 What is NOT built, and why the game is quieter than it was

**No CSV authors any of the three, so every character reads 0% / 0% / 0%.** Concretely, as of
0.123.0 **nothing in the game doubles**: the ~14 `[Double]` skills in `Skills.Fighter.cs` and
`Skills.Dual3rd.cs` (Strike, Smash, Shot, Precise Shot, Cleaving Strike, the bleed/poison detonators,
Killing / Venom / Swift Stab's second roll…) all land flat, and buff durations no longer double for
anyone. That is your ruling — *"Nobody — dead until you author it"* — and it is written down here so
it is never mistaken for a bug and patched with a default.

The retired ATK curve paid Elf **7.0%** / Human **10.0%** / Demon **10.75%** flat, to everyone, for
free. If you want the shipping state to feel like the old one, a base of **0.10** at the first rung
lands within a point of it for a human.

### What is owed from you

1. ❓ **Which classes get which mastery, and at what rung.** *"Not all"* skills doubling is expressed
   by which KITS carry the damage mastery — a warrior with it and a mage without it is the whole
   mechanism, since no mage skill is `[Double]` anyway.
2. ❓ **The base rates per rung.** Nothing is invented: `BalanceMatrix` §C1 prints what any base pays
   at every ATK, so pick a number off that table rather than a feel.
3. ❓ **Whether the cooldown-reset one is a fighter tool, a caster tool, or both.** It is the only
   genuinely NEW mechanic of the three and it has no precedent in our kit to lean on. IG puts its
   reuse-reset on casters; nothing forces us to.
4. 🔵 **Whether a BUFF should be able to scale a mastery** (a "Mastery Chant" that multiplies the
   base for a party). The accumulator is already the right shape for it; no buff channel was added
   because nothing authored needs one, and adding an unused field is how the last three dead knobs
   got there.

⚠ It costs a **new APK** to show the numbers (the stats window has a `Buff x2 dur` / `Reuse reset`
line now), but not to make them work — the rates are server-side and the rolls are server-side.

</details>

## `BL-193` ✅ CLOSED 2026-09-10 (0.125.0) — A FAILED BLOW IS A NORMAL ATTACK

**Filed and built the same day.** Your ask: *"can we make if a blow fails to hit as normal atack
(with crit chance and everithing)?"*

### What you ruled

1. **What the fallback is** — *"a normal atack as if i never used skill but just basic attack"*. Not
   the skill's damage with a crit roll bolted on: a real basic attack, off `EffectiveBasicAttack`.
2. **The floor goes** — *"we remove the 10% wiff and floor or whatever .. if it missies or is blocked
   so be it ... its a normal baisc attack"*. So `BlowFailFraction` is deleted, and the fallback is
   fully evadable and fully blockable.
3. **Which skills** — *"mighty blow if its a stab skill and its rogue-line ok .. but if its warrior
   line it should be a normal physical skill"*.

### The answer to (3), which needed no work

**Every blow in the game is already dual/rogue-line.** Enumerated, not assumed: `BlowOnCrit` is set
on exactly four definitions — `Stab` (fighter 1st), `Piercing Stab` (rogue 2nd), and the `Killing` /
`Swift` / `Heavy` / `Venom Stab` family in `Skills.Dual3rd.cs` (with their 4th-tier rungs) — and all
of them carry `RequiredWeapon: WeaponType.Dual`. There is no warrior-line blow to reclassify.

🔑 **`Mighty Blow` is a NAME, not a mechanic.** `mighty_blow` is `SureHit: true` with no `BlowOnCrit`
at all — already the ordinary physical skill you said a warrior-line one should be. It is also an
**orphan**: no class table grants it and it appears in no CSV, like `Heavy Draw` (`power_shot`). It
survives as a definition only. Your read of `fighter 1st` was right — the file's three attack skills
are **Stab / Smash / Shot**, and the Stab is the blow.

### What it cost

- `GameLoopService`: `BlowLands` split out of `ResolveBlow` (the gate is now rolled by the CALLER,
  **before** the skill's miss roll, so neither branch is gated twice); `ResolveBasicAttack` split so
  its resolution half — `ResolveBasicSwing` — is shared with the fallback rather than copied.
- `SkillDef.BlowFailFraction` and `Skills.Dual3rd.ThirdTierBlowFloor` deleted, with the four skills'
  description strings rewritten.
- The Unity skill-detail line (`GameUi.SkillDetail.CritLine`) no longer quotes a percentage.
- 128 CSV cells across four files: *"otherwise N"* → *"otherwise normal attack"*.
- `BalanceMatrix` §C1: a `gate%` column and a `fail OLD / fail NEW` pair — **and a real bug fixed**,
  `SkillHitFactor` had been gating blows on the crit rate since before `BL-188`.

### Worth remembering

⚠ **The floor is not a knob to bring back.** If the rogue ends up too strong or too weak, the levers
are `BlowRate` (`BL-188`'s ladder) and the authored stab power — not a re-introduced fraction, which
would put two different failure payouts back in the same mechanic.

## `BL-194` ✅ CLOSED 2026-09-10 (0.126.0) — SEEING A TARGET'S DEBUFFS AND STACKS

**Filed and built the same day**, from the playtest: *"i cannot see stacks on enemy (need to see
debuffs+stacks)"*, and the reason — *"so i know when to burst"*. His instruction on scope: *"make
debuffs + stacks to show on enemy so i know when to burst ... do that untill i authior the table"*.

### The finding that made it bigger than a UI job

**There was no wire message for another entity's buffs at all.** Not a filter to relax — nothing
existed. `BuffUpdate` only ever carried the player's own bar; the party roster carried debuff NAMES for
members; an enemy carried nothing. And **selection itself was client-only** (`GameBoot.TargetId`),
so nothing server-side could answer "what is on the thing he is looking at".

That is also why his other find — *"burs dont do nothing .. or atleast dont show that it does"* — read
the way it did: Venom Burst was consuming a stack pool that was never visible.

### What was built

- `TargetBuffUpdate(TargetId, BuffDto[])`, the enemy-side twin of `BuffUpdate`.
- `Entity.UiTargetId` + a `SetTarget` hub method; the client sends it when the selection CHANGES.
  ⚠ `InspectTarget` is a different thing and stays a one-shot pull for the stats sheet.
- Pushed once a second off the same `secondTick` as his own bar, gated on a change signature
  (`LastTargetBuffSig`) — a selected mob usually carries nothing and that must cost nothing — plus one
  immediate push on selection so the row fills on the tap.
- 🔑 **The stack counter is FOLDED into the debuff it counts.** A stacking DoT is two buffs
  server-side: the damage effect on `BuffKey`, and an `Internal` counter on `StackKey`. The HUD reads
  `Venom x7`, never `Venom` beside `Venom (stacks)`.
- Client: one ellipsised line under the target frame's detail row, debuffs (red) then buffs (green),
  counts shown only above 1. The panel grew 160 → 186px to pay for it.

⚠ `ProtocolVersion` 35 → **36**, so it needs a new APK.

### Worth remembering

`GameBoot.TargetId` is a real property now rather than an auto-property — that is the whole trick that
makes every existing assignment site (tap, tab-target, auto-hunt, the clear-on-death rule) notify the
server without touching any of them.

## `BL-195` ⚠ SUPERSEDED THE SAME DAY BY `BL-198` — the shelf rule, as first built (duration)

🔴 **WHAT FOLLOWS SHIPPED FOR ONE AFTERNOON AND IS NOT HOW THE GAME WORKS.** He read it and replaced
the MECHANISM (not the intent) within hours: *"it should not work only on timer ... the limit should
have an id collection ... i gave the duration as filter not as solution"*. See **`BL-198`** below for
what is actually built. Kept because the intent below is still the intent, and because the reason it
was wrong is worth having in writing: a duration is not a property of a skill when something else in
the game is allowed to multiply it.

*"let's make all buffs that are not 20min and not harmonies or marks (the current ones) not enter the
limit. Now archer if not remove the 2 buffs (arcane insight and shield reinforcement) he have no way
to use his own .. Or make it party/target buffs to be in the limit and all self not. But I preffer the
20 min onse. Like bow expertise and bow blessing/egc to count towards limit but bow
Ferocity/swiftness don't."*

He named both designs and chose between them himself. The 20-minute one is the better of the two:
"self vs party" is about who *cast* it, and the cap is about what you *carry*. A 30-second stance is
not a shelf item whoever cast it.

### The rule

A buff occupies one of the 20 slots iff **`CountsTowardBuffLimit`** (authored, default true) **and**
(it runs **≥ 12000 ticks / 20 minutes**, **or** `SkillCatalog.IsHarmonyOrMark`), **and** it is not a
toggle, not a debuff, and its row is `Buff` or `Consumable`.

🔑 **The harmony/mark exception reuses the admin menu's own drawer tests, in the same order** — Mark
by buff KEY, harmony by NAME. Two copies of "what is a harmony" would drift the first time one grew a
thirteenth member.

🔴 **THE DURATION IS THE LONGEST OF THREE CLOCKS, and each one is load-bearing.** What it landed with
(the only one that sees a one-child WRAPPER's time — the child authors zero, so reading the def alone
would exempt the entire NPC shelf); the landing def's own authored time (the only one that survives a
RELOG, where the buff is re-applied with the seconds it had LEFT); and the source wrapper's, for a
relogged wrapper-delivered one, where neither of the other two can see twenty minutes.

### Worth remembering

`CountsTowardBuffLimit` did not go away and must not: it is how the three RUNES stay exempt despite
running an hour, because a ~1/s reconciliation loop owns them. See **`BL-198`**, which is open.

## `BL-196` ✅ CLOSED 2026-09-11 (0.128.0) — CAST SPEED AND CAST TIME ARE TWO DIFFERENT CHANNELS

*"the elf archer spirit mastery should increase the phisical cast speed as well. It should not
increase cast speed as stat for mages only. It should increase the end cast time.
(baseCastOrAttackSpeedValue x castOrAttackSpeedBuffs x castOrAttackSpeedDebuffs / 333 or whatever) x
castTimeDebffs x spirit_mastery and other cast time buffs"*

### The finding

**Spirit Mastery is the ARCHER's own party proc and it did nothing for the archer.** Its 20% rode a
`BuffCastSpeed` magnitude — the mage's stat. A physical skill is paced by ATTACK speed
(`SkillMath.PacedByAttackSpeed`), so the buff reached every mage in the party and not the Elf who
cast it. His CSV cell had always said `p.skill cast time`, and **no cast-speed channel could express
that at all**: cast speed is chosen *instead of* attack speed, per skill, by the physical/magical
axis. The distinction he drew is real and the engine did not have it.

### What was built

`SkillDef.CastTimePct` (+ the `SkillLevel` slot and `CastTimePctAt`) → `BuffInstance.CastTimePct` →
`Entity.CastTimeReduction` / `CastTimeMultiplier`, applied at both cast-length sites (the real cast in
`BeginCast`, and `AutoCycleTicks`'s estimate, which must agree with it or autohunt misprices MP/s).

```
castTicks = authored x (333 model: attack speed OR cast speed) x CastTimeMultiplier
CastTimeMultiplier = clamp(1 - SUM(buff.CastTimePct), 0.2, 3)
```

Everything inside his bracket is the existing 333 model; this is what is outside it. **It multiplies
the answer, so it does not care which stat produced it** — which is the whole reason it had to exist.

⚠ A `FixedCast` skill and a MOB skip it, exactly as they skip the speed model.
⚠ **Negative lengthens** — the `castTimeDebffs` half of his formula. Nothing authors one yet.

## `BL-199` ✅ CLOSED 2026-09-11 (0.128.0) — A VENOM STACK IS THE RECORD OF A BLADE GOING IN

*"Now venomweaver almost cannot stack venom... Stacks should be independent of dot... So each landed
venom blow adds stacks that do not do nothing just stacks, and try to do a venom debuff that do dmg
depending on those stacks and when venom burst is used it takes with it the stacks + the dot debuff
... (also I have the feeling that the burst don't do dmg per stack)"*

Four defects, and any one of them alone would have made the discipline unplayable.

### 1. A stack needed TWO rolls to come up

Stacks were banked inside the DoT's AGI-vs-CON contest, so a Venom Stab had to land its blow **and**
win the contest to bank anything. They are banked by the **strike** now: `ApplyDotStack` applies only
the DoT damage effect, and a new `AddDotStacks` is called after both arms of `ExecuteSkill` whenever
the damage arm connected. The venom debuff still lands on its own contest; losing it costs the damage,
never the pool.

### 2. The venom ticked for ONE stack no matter what

A stacking DoT is two statuses — the damage buff, pinned at `maxStacks: 1`, and a hidden counter that
holds the real number. `TickDots` read the pinned one. 🔴 **The fold existed in exactly one place,
`PushTargetBuffs`, so the bar showed "x7" while the damage was x1** — and the bar was the half telling
the truth about intent. `DotStacksOf` is now the one helper both read.

### 3. The burst left the venom running

So spending ten stacks changed nothing visible: the same debuff on the bar, and a number that looked
like an ordinary stab. It removes the counter **and** every DoT whose skill shares that `StackKey`.

### 4. The burst always DID multiply by stacks

He was right to suspect it and wrong about which half was broken — it was the pool being empty. It now
says what it spent: *"Venom Burst detonated 7 stack(s) — ×7 damage."*

### Worth remembering

🔑 **"Landed" on a `BlowOnCrit` skill has two readings, and the looser one was taken** — the strike
connecting, so a blow that fell through to a normal attack (`BL-193`) still banks if that swing hit.
The tighter reading puts the pool back behind a rate roll, which is half of what was broken. **That
choice is his to confirm: `BL-197`, open.**

## `BL-197` ✅ CLOSED 2026-09-11 (0.128.0) — ONLY A SUCCESSFUL STAB BANKS A VENOM STACK

Filed the same day it was answered. `BL-199` had rebuilt venom stacking and left one reading open:
*"each landed venom blow adds stacks"* has two meanings on a `BlowOnCrit` skill, and I took the looser
one (the strike connecting, so a blow that fell through to a normal attack still banked).

**His ruling:** *"only the succesfull stab .. not the failed/basick attack one .. if i chose to use
perfect_strike i land more ophen i stack faster but for less dmg ... if i use brutal_strike i land
less ofthen i stack slower but do more dmg."*

### Why his reading is the better design, and mine was not

🔑 **THE BLOW RATE IS THE KNOB HE BUILT THE CLASS AROUND.** `BL-188` made blow landing its own stat,
and the @80 pair — Perfect Strike (rate ×1.40) against Brutal Strike (+30% crit damage) — is a choice
between *more, smaller* and *fewer, bigger*. Under my reading the pool filled at the same speed either
way, so Perfect Strike bought nothing a Venomweaver cared about and the choice was only half real. Tie
the pool to the gate and the two buffs become two genuinely different rotations.

⚠ **This is not what made the class unplayable and it must not be confused with it.** That was the DoT's
SEPARATE AGI-vs-CON contest, which the pool no longer waits on at all (`BL-199`). One roll gates a
stack now, and it is the one the player has a buff for. **Two rolls for one outcome is the bug; one
roll the player can influence is the design.**

## `BL-198` ✅ CLOSED 2026-09-11 (0.128.0) — THE BUFF LIMIT IS A COLLECTION, NOT A TIMER

Filed as "the three runes are exempt, confirm or flip" and answered much more usefully than that:

*"it should not work only on timer ... the limit should have an id collection ... and if that skill is
inside that collection it goes to the buff bar and counts ... i gave the duration as filter not as
solution ... if one buff a 10 min buff and it doubles it probanbly break en enter the count .. but it
shouldns ... for now that collection must be the: single buffs, grouped buffs, harmonies, marks,
archers 20 min buffs, any other self 20 min buff we have (cant remember them all). U can ask me for
some that i didnt meantion."*

### 🔴 The counterexample is unanswerable, and it generalises

`BL-190`'s `DoubleDurationRate` doubles a landed duration **on a roll**. So under a duration test, a
10-minute buff that happened to roll a double would cross twenty minutes and start costing a square —
**the same buff on the same character, decided by a die.** The rule worth keeping out of this:

> **Never make a rule read a number something else in the game is allowed to multiply.**

The duration was his *description* of the set he had in mind, and I built the description instead of
the set. That is the whole mistake, and it is one to watch for: when someone characterises a group by
a property, check whether the property is stable before making it the definition.

### What is built

`SkillCatalog.BuffLimitIds`, **derived and not typed out** (a typed list goes stale and whole tiers
vanish from it), from two sources that between them are exactly his enumeration:

1. **the two shelves unioned** — the same universe the admin Buffs menu's four drawers come from, so
   singles, groups, harmonies and Marks are in **by identity, at any duration**;
2. **every other `BuffRow.Buff` skill whose AUTHORED `DurationTicks` ≥ 20 min** — the archer's Bow
   Expertise / Blessing / Spirit, and anything authored that long later, with no edit anywhere.

⚠ **Child ids are in the set too.** A single blessing lands through a one-child wrapper and the buff on
the bar carries the CHILD's id; a set of wrapper ids alone would match nothing at the only moment it is
asked.

⚠ **`BuffRow.Buff` in rule 2 closes the original question** — the three runes run an hour but draw in
the consumable row, so they are out without needing a special case. The mechanical reason still holds:
a ~1/s reconciliation loop owns them, so evicting one frees a square for a fraction of a second.
Potions and scrolls are `Consumable` too, but their CHILDREN are in via rule 1 — a potion of Might and
a cleric's Might are the same buff from different bottles, and always have been.

### His last sentence is answered with a tool, not a question

*"U can ask me for some that i didnt meantion."* — `dotnet run --project tools/BalanceMatrix --
--bufflimit` prints **both** halves: the 221 ids that cost a square and the 149 timed buffs that do
not. Reading a derived list beats remembering the buffs, and the FREE half is where he will spot
anything he wanted counted. The two sitting closest to the line today are **Shield Mastery (10 min)**
and **Bow Focus (5 min)**.

## `dual 4th.csv` ✅ FINISHED 2026-09-11 (0.128.0) — and the file earned its `Check.Specs` line

*"as general fix the duals 4th.csv in wording etc .. I added 3 new ulsitmate skills, 3 new passiives
for identity for each race and make vanish cooldown fixed. With that duals 4th is finihed (untill dmg
is rly tested)."*

Six new families built as authored — one defensive axis per race, carried at two strengths (a
permanent 5/7/10% passive at 80/85/90, and the same defence turned up to 25-30% for ten seconds at
83). Human vs magic, Elf vs physical skills, Demon vs people. Vanish became `FixedCooldown`.

### Worth remembering

🔴 **`atk -15%; def -15%` SURVIVED A WHOLE CHRONICLE ON 30 ROWS.** It was the per-skill venom rider,
dead since `DotTiers` made a DoT's side effect a property of the (kind, tier) table — and BOTH halves
were wrong by then, in opposite directions: the attack cut is −10% (P.Atk *and* M.Atk), and there is
no defence cut at all (*"for now no dot will decrease def"*). Nothing caught it because the file had
no `Check.Specs` line, which is precisely the argument for giving a DERIVED file one: the checker's
job is to stop two halves of the same fact drifting, and it does not care which half a human wrote.

⚠ **A `DisplayName` override is for when the flavour genuinely differs.** `double_mastery` got one
("Momentum Mastery") on the rogue, and he corrected it: the toggle does the same thing for the rogue
that it does for the warrior, so it keeps the def's own name, **Overpower Mastery**. `Stab Momentum`
keeps its override because there the base mechanic really is different from the Magus's Arcane
Momentum wearing the same id. **Rename the flavour, never the mechanic.**

---

## `BL-200` ✅ CLOSED 2026-09-11 — both ladder dips were typos, both fixed in code AND CSV

His ruling, within the hour of the entry being written: *"Warrior sword mastery should be
94->101->108 / Battle defense is x2->x2.5->x3 / Typo on both."*

- `warrior_sword_mastery` rung 8 (level 60): **91 → 101**, so the +7 step runs unbroken 52 → 150.
- `battle_defence`: **×2.3 → ×3.0** at 52, so the ladder is a clean +0.5 (×2 → ×2.5 → ×3).

Both the code array and his CSV cell moved in the same commit (0.130.0). `--check` reports no LADDER
DIP on either file any more.

🔑 **`--check`'s LADDER DIP is what found both**, and it found them on a pair of files that are not
finished — which is the `BL-197` argument for giving a half-authored file its `Check.Specs` line the
day it lands rather than the day it is done. Two typos, spotted the same afternoon they were written,
in rows nobody had played yet.

---

## `BL-201` ✅ CLOSED 2026-09-11 — the warrior loses Precision; ACCURACY is the replacement

Asked whether the row leaving `warrior 2nd.csv` meant the mechanic or just the row, he ruled:
*"Delete precition as we deleted the rogue floor. Probably will give more acc later on warrior. Now
he have +9 which kills 50% of the rogues evasion anyway (no need for another hit floor) - leave the
mechanic but not the skill on warrior."*

**What changed:** `SkillCatalog.FloorPassiveFor` no longer names `Archetype.Warrior`. That is the
whole change.

🔑 **NO UN-GRANT, AND THAT IS A RULING OF ITS OWN.** A strip in `AutoLearnCoreSkills` went in first,
on the reasoning that an auto-grant is a plain assignment and therefore permanent (which is why
`BL-143` needed exactly that for the tank's Backlash). He deleted it the same afternoon: *"no1 except
me plays this game for now .. so no lingering warriors when I clear a db .. So no point of migration
type to remove a skill from some1. They will never have it in the 1st place."*

**Pre-release, a `game.db` delete IS the migration.** Write un-grant code only for a skill that
shipped to somebody who is not him — and nothing has. ⚠ The Backlash strip predates this ruling and
is still in the file; it is not the pattern to copy.

**What deliberately did NOT change:** the `Precision` SkillDef and `PassiveEffect.HitFloor` both stay.
*"leave the mechanic but not the skill on warrior"* — the def because a save can still carry the id
and a learned id must resolve, the field because the resolver's floor is a general mechanic the next
thing to want one can author against.

🔑 **HIS ARGUMENT IS THAT ACCURACY ALREADY DID THE JOB, and it measures out.** Warrior's Strength now
carries up to **+9 accuracy** (`warrior_strenght` rung 4), and the resolver is one line —
`miss = 5% + (EVA − ACC) × 1%` — so nine points *is* nine points of a rogue's evasion lead. The floor
existed to stop an evasion-stacked rogue locking a warrior out entirely; `BalanceMatrix` §E1 puts a
buffed champion at **16% miss** against a light-armour rogue at 36, nowhere near the 90% ceiling the
floor capped. It was a second layer of protection that never bound.

⚠ **Still spelled three different ways across three files**, which is worth knowing next time one of
them is touched: the tank's Anti-Magic row is in `tank 2nd.csv` reporting ⚪ AUTO-GRANTED, the rogue
has no `Evasion Mastery` row at all, and the warrior now has neither row nor grant.

---

## `BL-200` 🔵 TWO LADDER DIPS in the warrior 3rd files — one number each, both yours

Both files landed 2026-09-11 and both are built (0.130.0). Two cells make a ladder go **down**, which
is the one shape `--check` refuses to guess at, so both are in the code **exactly as you wrote them**
and flagged at the line. Each is a single number once you rule.

### 1. `warrior_sword_mastery` (Two-Hand Mastery, Ravager) — rung 8 reads **+91 P.Atk**

`warrior 3rd.csv`, level 60. The column either side of it:

| level | 52 | 55 | 58 | **60** | 62 | 64 | 66 |
|---|---|---|---|---|---|---|---|
| P.Atk | 80 | 87 | 94 | **91** | 108 | 115 | 122 |

Every other step in the fifteen is an exact **+7**, which puts **101** where 91 sits — a digit swap
is the obvious reading, and 101 is the only value that makes the column one straight line. As
written, a Ravager who buys the level-60 rung **loses three points of attack he already had** and
pays 120k SP for the privilege.

⚠ The crit-damage column on the same fifteen rows is clean end to end (145 → 615), so this is one
cell, not a mis-transcribed ladder.

**Owed: 91 or 101.** `SkillCatalog.W3SwordAtk` in `Skills/Skills.Warrior3rd.cs`, one array entry.

### 2. `battle_defence` — rung 3 is **weaker** than rung 2

`warrior 3rd.csv`, the Ravager's only Battle Defence rows:

| level | 36 (2nd class) | 43 | 52 |
|---|---|---|---|
| P.Def | ×2.0 | ×2.5 | **×2.3** |
| SP | 20k | 42k | **74k** |

So the 74k rung is a **downgrade** on the 42k one. I can see three readings and nothing in the file
picks between them:

- **×3.0** — continues the +0.5 step from 2.0 → 2.5.
- **×3.5** — matches Battle Presence's own shape on the same two levels (it climbs ×1.35 → ×1.50 →
  ×1.65, i.e. the gap widens).
- **×3.2** — a digit swap, the same failure mode as the sword mastery above.

**Owed: one number.** `BattleDefenceRung(1.3f, …)` in `Skills/Skills.Fighter.cs`.

---

## `BL-201` ❓ `Precision` left `warrior 2nd.csv` — is the warrior's HIT FLOOR gone, or just the row?

Your 2026-09-11 pass deleted the whole `Precision` block from `warrior 2nd.csv`. **The mechanic is
still in the game** and I have not touched it: `SkillCatalog.FloorPassiveFor` auto-grants it at the
2nd class and it is the warrior's archetype identity passive, the twin of the rogue's Evasion Mastery
and the tank's Anti-Magic.

**What it does:** a **10% hit floor** — your physical attacks land at least one swing in ten no matter
how much evasion the target stacks. It grants no accuracy points and it is an anti-evasion tool only;
it exists so that a warrior is never locked out of hurting a rogue who has stacked evasion past his
accuracy. Rung 2 (20%) is written and reachable at 40; rung 3 (30%) is written and deliberately
unreachable until the 4th class exists.

**Why the row's absence proves nothing either way:** the skill is auto-granted, so it has no class-table
entry, so `--check` compares it against nothing and reported it only as ⚪ AUTO-GRANTED when the row
existed. Its disappearance is invisible to the tool, and a deleted row is not enough to delete a combat
mechanic on.

Three things it could have been:
1. **Tidying** — you dropped an SP-0 row that was never really a purchase. Nothing owed; I put the row
   back so the file says what the game does.
2. **A real removal** — the warrior loses the floor. Then the rogue's Evasion Mastery and the tank's
   Anti-Magic should probably be looked at in the same breath, since the three are one design.
3. **A move** — it belongs on the 3rd-tier files instead, with the rest of the 40+ identity.

🔵 **Nothing is built either way; say which and it is small.** ⚠ Same question, one file over: the
rogue's `rogue 2nd.csv` still has no `Precision`-shaped row either, and the tank's Anti-Magic row is
still in `tank 2nd.csv` reporting ⚪ AUTO-GRANTED — so the three identity passives are already spelled
three different ways across the three files.

---

## `BL-203` ✅ CLOSED 2026-09-11 (0.131.0) — THE STAB LADDERS HALVE, AND A HALVED STAB LEARNS TO DOUBLE

*"cut dual 3rd/4th stab skills to have twice less power … now ~11k dmg on a 90 mob with 19k hp. And
78k mob hit for 8k. A bit too much. Let atleast this dmg to be a double dmg. (kiling/heavy/swift/
venoms/etc..)"* · *"give duals 3rd/4th stabs a [double] flag and overpower passive but least than
warrior @40 3% @76-7% (1 rung less. If it's too low I'll give him the last rung at 80 — for now only
the 2 rungs)"* · *"make venomWeaver — venom stab to have the same power as killing strike (the new /2
dmg)"* · *"Increase heavy stab reuse to 7.5s., swift strike reuse to 5s. To balance the dmg~reuse for
races."*

### 🔴 HALVING THE POWER DOES NOT HALVE THE DAMAGE — the one number to carry forward

Power is a term INSIDE the ratio (`K·(atk·lvlMod + power)/def`), so the attacker's own P.Atk is
untouched by it. Measured at 90 in mythic gear (`BalanceMatrix --stab`, written for this), where the
ATK term is worth ~2,470 of the numerator:

| | power | blow | doubled |
|---|---|---|---|
| Killing Stab, before | 15,000 | ~6,970 | — |
| Killing Stab, after | 7,500 | **3,978** | **7,956** |

So the trade is better than "half unless you double": the floor fell to ×0.57 and the **ceiling rose
~14%**. ⚠ Never restate this as *"2 × the new IS the old"* — it is the mistake the block comment on
`StabPower` exists to stop, and it was in the first draft of that comment.

### What moved

- **Four ladders halved at both tiers** — Killing/Swift (`StabPower`), Heavy (×0.75 per hit, two
  hits), Venom Stab, Venom Burst (per consumed stack). Every ratio between the families is preserved
  by halving all of them, so ten stacks is still exactly twice a Killing Stab.
- **`CanDouble: true` on all four stab families.** A blow rolls its double INSIDE `ResolveBlow`,
  after the crit-damage values — so a doubled stab is the crit number ×2.
- **Venom Stab = Killing Stab's ladder.** The Demon's old ×0.50 would have compounded with the
  halving on the one discipline that has no Killing Stab. In numbers it barely moved: 675 → 625 at
  the first rung and nothing else.
- **Overpower at 40 (3%) and 76 (7%)** for all three melee rogues — the warrior's def, rungs 1 and 2,
  with `ClassSkill.SpCost` overriding the price to the tier the rogue meets it at (28,000 / 6.5kk).
  🔑 The top rung (10%) is left unreachable by design; if he adds it, it is **one line at 80 with
  `SkillLevel: 3`** and one CSV row, never a new ladder.
- **Reuse prices the races apart** now that they share a power ladder: Heavy 7.5s, Swift 5s, Venom 3s.

### Still open, and small

🟡 **The 76 rung charges NO GOLD.** `ClassSkill` can override SP and nothing else, and rung 2 of a
ladder authored for level 40 carries no `GoldCost`. Every other 76 row in `dual 4th.csv` charges 1kk
gold beside its SP. If that matters it wants a `GoldCost` override on `ClassSkill` — **not** a second
Overpower def.

🟡 **Venom Burst did NOT get `[Double]`.** His word was *"stabs"*, and the burst is a ×10 pool: a
double on a full detonation is a very large single number. Say if it should have it.

---

## `BL-204` ⚠ SUPERSEDED THE SAME DAY BY `BL-207` — venom burst on a flat 80% rider, as first built

🔴 **BUILT AND THEN REPLACED WITHIN THE HOUR**, on his counter-proposal. The diagnosis below is still
the right one and is why `BL-207` exists; the FIX is gone. `SkillDef.FixedLandChance` was removed with
it — do not re-add the field. What survives is the half he kept: `ExecuteSkill` no longer rolls a
rider it has already decided to skip, so the cosmetic `Fail` is gone at its source.

## `BL-204` (as built, superseded) — VENOM BURST LANDS ON A FLAT 80%, AND NOTHING SCALES IT

*"make venom burst to always have max 80% roof land rate independent on dex … now u fail fail, stack
3, fail fail, stack to 6 … then sooner or later u make 10 stack and venom burst fails and u cry :) …
The venom burst if fails takes stacks and don't do dmg, so it burns twice (low land rate and take
stacks) — let's punish only the true unlucky, 80% land rate, if it fails it fails."*

### 🔑 WHAT WAS ACTUALLY FAILING WAS THE RIDER, NOT THE DAMAGE

Venom Burst has **no blow gate** — it is not `BlowOnCrit`, and its damage lands on every cast that is
not evaded. What he was watching was its **venom contest**: the burst spends the pool in the damage
arm and only *then* rolls AGI-vs-CON for the rider, so a lost roll broadcasts `CombatOutcome.Fail` on
the same cast that had just consumed ten stacks. From the player's seat that is indistinguishable
from "the burst failed and took my stacks", and he is right that it is a double burn.

### The field, and why it is not a cap

`SkillDef.FixedLandChance` (0 = off) **replaces the stat curve outright** and skips the per-skill
`DebuffLandMod` and the target's CC/school resistances with it. A ceiling on the curve would still be
DEX-shaped underneath, which is the half he named; and a number authored as "always 80%" that a
blessing can still divide is not 80%. `ResistsDebuff` — total immunity — still applies, because
immunity is a different statement from a resistance.

⚠ **The stacks are still spent on a failure.** That is his ruling, not an oversight: he chose to fix
the rate and keep the cost. It matters most on the EMPTY-pool cast, where the rider is the whole point
of the skill (*"if no stacks present apply 1 venom stacks"*) — a Venomweaver now opens his rotation 4
times in 5 instead of on an AGI roll.

---

## `BL-205` ✅ CLOSED 2026-09-11 (0.131.0) — THE SKILL MASTERIES BELONG IN THE PASSIVES GROUP

*"all classes overpowerd/momentum is in buffs group not in passives in the skill window."*

The Known tab groups strictly by `SkillDef.Category` and heads each block with its name. Overpower was
`Physical` (so it sat among the damage skills) and Arcane/Stab Momentum + Lasting Enchantment were
`Buff` (so they sat among the stances). All three are `Passive` now.

⚠ **`double_mastery` deliberately stays a Buff.** Overpower Mastery / Momentum Mastery is a TOGGLE the
player switches on — it needs a bar slot, and `SkillCategory.Passive` is what makes the server refuse
to cast a skill at all.

🔑 The grouping was the *only* thing wrong: `def.Passive != null` has always kept these off the bar,
so nothing about their behaviour changes.

---

## `BL-206` ✅ CLOSED 2026-09-11 (0.131.0) — GROUND PAINT IS SCENERY, DECALS ARE GAMEPLAY

*"traps orange-gold circle for the owner is under the red zone poligon and I see only the half that is
outside if any."*

Exactly what the heights said. The map paints the ground in layers — spawn-zone discs **0.01**,
coloured FIELD polygons **0.02** (red at the high level bands), town islands **0.03**, region outlines
0.06, world border 0.08, jail 0.09 — while `GroundDecals` drew a trap at **0.01** (under the field
fill) and a totem at **0.02** (z-fighting it). Only the part of a trap outside a field polygon was
ever visible, which is what he saw.

The whole decal stack moved above every painted layer and kept its own order: trap **0.10**, totem
**0.11**, flash **0.13**.

🔑 **THE RULE, so the next decal does not repeat it:** ground paint is scenery and decals are
gameplay, so every decal goes above every painted layer. 0.10 is the floor for `GroundDecals`.
⚠ The totem was mis-layered too and nobody had reported it — worth remembering that a z-order bug is
only ever noticed on the layer someone is looking at.

---
## `BL-207` ✅ CLOSED 2026-09-11 (0.131.0) — THE BURST IS A STAB: it rolls the blow gate, it can double, and a failed one refunds

Supersedes `BL-204` the same day, on his counter-proposal — and his is the better design.

*"venom burst is a single stab skill that it's effective power depend on stacks count. It's not like
barrage -> 10 stabs x1.5k power; it's one stab x15k power (so if it lands with 10 stacks it's like a
killing stab with a double) … So venom burst also must land a double. Can we make venom burst to be
with normal land rate (30% like other stabs) and on fail not to take all stacks but to restore 3 — a
burst without stacks give 3, so a failed one takes all and gives you 3. U do burst for 10 stacks, if
it fails u pay only with 7 stacks and cd, so next 10 stacks are faster to stack, u don't start from
0."*

### 🔑 HIS RACE-PARITY MODEL, AND IT MEASURES OUT

He gave the design a unit — **multiples of that race's own plain stab per 10 seconds** — and asked
whether the three land in the same place. Measured at 90, mythic, unbuffed (`BalanceMatrix --stab`,
extended for this):

| race | rotation | × its own stab |
|---|---|---|
| Elf | 3 Killing + 2 Swift | **5.00** |
| Human | 3 Killing + 1.5 Heavy | **5.44** |
| Demon | 3 Venom Stab + 1 Burst (9 stacks) | **5.47** |

His arithmetic was right. ⚠ The board deliberately does NOT apply the blow rate — every race is gated
by the same roll, so it cancels — but it also does not capture that the Demon needs three
**successful** stabs before his burst is worth casting (`BL-197`), so his rotation is longer in real
time than the other two, whose every landed blow is damage on its own. That is the argument for his
column being the biggest of the three.

### 🔴 `damage × stacks` IS NOT `power × stacks`, and the difference is 55%

`ExecuteSkill` multiplies the **resolved damage** by the stacks spent. Power sits beside `atk·lvlMod`
inside the ratio, so multiplying afterwards multiplies the ATK term ten times over as well:

- as built (damage × 10): a full pool reads **10,850**, or **2.73×** a Killing Stab
- as he described it ("one stab x15k power"): it would read 6,970, or **1.75×**

**Kept as built.** His prose said 2×; the engine gives 2.7×; and it is the 2.7× that lands his own
parity table on 5.47 against the Human's 5.44. Flagged rather than silently reconciled — if he wants
the literal "one stab of 15k power" it is a one-line change (`pFlat * spent` before the ratio instead
of `damage * spent` after it) and the Demon's column drops to about 4.6.

### What moved

- **`BlowOnCrit: true` + `CanDouble: true`** on Venom Burst. It used to land ALWAYS and FLAT — no
  crit values at all — so this is a change in both directions: bigger when it lands, a basic swing
  when it does not, which is the trade the other two races already make.
- **The crit-flat factor is measured against `power × stacks`** (`critPower`). Feeding it the
  per-stack power while the damage was already ×10 would have valued the flat crit add against a tenth
  of the real numerator and inflated every detonation. It never mattered while the burst was not a
  blow, because it computed no crit factor at all.
- **A failed burst empties the pool and banks one cast's worth back** — 10 → 3 at the top rungs, per
  rung the same number a stackless burst lays (1/1/2/2/2/2/2/2/3×7). It runs through the SAME two
  calls the detonation does, so a failed burst and an empty-pool burst leave the target in identical
  states. `ClearStackPool` was extracted for exactly that reason: **one place, so a future change
  cannot fix the detonation and forget the failure.**
- **The cosmetic `Fail` is gone at its source.** A burst that spent a pool no longer rolls its rider
  contest at all — `spentStacks` already suppressed re-applying the DoT, so every branch of that roll
  was a no-op and the only thing it could still do was print `Fail` over a cast that had just dealt ten
  stacks of damage. **A roll whose every branch is a no-op is not a contest, it is a lie on the
  screen.** A burst that found no pool still rolls, which is his *"if no stacks present apply 1"*.

### ⚠ On the ~20%

He justified the halving with *"the double occur only ~20% of the time so average is lower than what
is now"* — and the conclusion is right, but the rate is **7%** (Overpower rung 2 at 76), doubling to
**14%** only with Momentum Mastery running from 81. At 7% the average landed stab is 4,257 against the
old 6,970; at 14%, 4,535. Either way it is well under, so nothing changed — but the number matters if
he tunes the ladder off it.

---

## `BL-192` ✅ CLOSED 2026-09-11 (0.132.0) — THE NUKER'S 4th-CLASS KIT IS BUILT

**All 236 rows.** Nineteen families continued past 74, six new skills, and the one engine gap this
entry named turned out to be real: `TryOnDamagedProcs` never passed the ATTACKER, so `ProcVictimRungs`
could not fire on a defensive proc and all three Spell Empowerments would have cost 150kk and done
nothing. `nuker 4th` earns its `Check.Specs` line and reads clean. See the 0.132.0 CHANGELOG entry for
what was corrected in the file, what was left alone, and the four items below that are still yours.

🔑 **TWO OF THIS ENTRY'S OWN RECOMMENDATIONS WERE STALE AND BOTH WERE WRONG** — the standing lesson
that a parked research note is a hypothesis, not a spec, and has to be re-verified against the code:
- It said *"the MP-cost cut has no StatMods field"*. `StatMods.MpCostPct` has existed since the
  healer's own 78+ robe rungs shipped, and is one number for BOTH channels by design. No
  `ExtraPassives` layer was needed.
- It recommended building the unqualified *"Decrease Mp Consumption"* as MAGIC-only. Its own evidence
  argued the other way (he writes `p.mp` when he means one channel), and the healer's identical robe
  clause already ships as both. Built as BOTH.

### ❓ STILL YOURS — four small things, none of them blocking

1. 🔴 **Force Empowerment's HP drain does not ladder.** Your 50 / 40 / 30 a second needs a per-LEVEL
   `HpPerSecond`; the field exists only on `SkillDef`, so all three rungs burn the first rung's **50**.
   That is the harshest of your three numbers, never a silent buff. `SkillLevel.HpPerSecond` plus one
   lookup in `TickToggleUpkeep` is the whole fix.
2. ❓ **Two numbers in the Spell Empowerments are MINE.** The retaliation rider lasts **10s** and the
   proc has a **10s** internal cooldown; your cells give the chance and the magnitude and nothing else.
   Both are the archer stances' own values, which are the only other victim-paying procs in the game.
3. ❓ **Pyro Burst's `(success chance x1.5)` is carried and is inert** — a burn's save is
   `DebuffSchool.None`, so nothing contests it. Your 3rd-tier row has no such clause and these three
   do. Say the word and the cells go; nothing changes either way.
4. ⚠ **The first 4th rung of all three race Bursts buys nothing** — power 150, the same rider, for
   100kk of gold, repeating the 3rd tier's last rung before climbing to 200 and 250. Built as
   authored (your Gravity has the same shape at the healer's tier boundary), but if it was meant to
   start at 200 it is three numbers.

---

### The entry as it stood, for the research in it


**`docs/data/classes_skills_csv/nuker 4th.csv` is DONE** (his words, 2026-09-10: *"so i think im done
with nuker 4th"*). 236 rows, 25 skills, the `NOT DONE` banner gone. **Nothing of it is built.** This
entry is the research so the build does not start from zero — it was done, then parked when he asked
for a commit so he could take an APK.

### What the file contains

**Continuing ladders** — the 3rd tier ran 14 rungs (40-74); the 4th runs **15, one per level, 76-90**.
Start rungs, counted off `RegisterNuker3rd`, not guessed:

| Skill | id | 4th rungs | Note |
|---|---|---|---|
| Anti magic | `anti_magic_mage` | **21-35** | ⚠ ALREADY IN THE DEF (`HealerFourthAntiMagicRungs`) and his rows match it digit for digit. Learn lines only. |
| Spellcaster Weapon Mastery | `healer_weapon_mastery` | **15-29** | ⚠ ALREADY IN THE DEF (`HealerFourthWeaponRungs`). Learn lines only. |
| Mage Armor Mastery | `nuker_armor_mastery` | **19-33** | The nuker's own. NEW rungs to author. |
| Elemental Blast | `elemental_blast` | 15-29 | |
| Quick Blast | `quick_blast` | 15-29 | |
| Elemental Wave | `elemental_wave` | 15-29 | |
| Arcane Wave | `arcane_wave` | 15-29 | Human |
| Frost Spikes / Frost Pierce | `frost_spikes` / `frost_pierce` | 15-29 | Elf |
| Witches Curse / Scarecrow | `witches_curse` / `witches_scarecrow` | 15-29 | Demon |
| Vampiric Bolt | `vampiric_bolt` | **20-34** | Human. 3rd ran rungs 6-19. |
| Arcane Void | `arcane_void` | **4-7** @76/80/85/90 | Human. Only MP moves; all four say 2~4. |
| Elemental Burst | `elemental_burst` | **4-6** @80/85/90 | power 200/225/250, 2 Elemental Stones |
| Thunderstorm | `thunderstorm` | **4-6** @80/85/90 | power 250/300/350, 3 stones |
| Arcane / Frost / Pyro Burst | `arcane_burst` etc. | **2-4** @80/85/90 | one per race |
| Arcane Momentum | `reuse_reset_momentum` | 1 @76 | ✅ ALREADY BUILT (`BL-191`) |

**His shared columns, read off the file** (state each once, as `Skills.Nuker3rd.cs` does):

- Bolt MP (Elemental Blast, Quick Blast, Frost Spikes, Frost Pierce): `69,71,73,77,79,91,95,97,99,103,105,107,111,113,115`
- Wave MP (Elemental Wave, Arcane Wave): `105,107,109,111,114,117,120,123,126,129,132,135,138,141,144`
- Heavy MP (Vampiric Bolt, Witches Curse, Witches Scarecrow): `138,142,146,154,158,182,190,194,198,206,210,214,222,226,230`
- Blast power (Elemental Blast, Vampiric Bolt): `110…138` by +2
- Quick power (Quick Blast, Witches Curse): `88,90,91,93,94,96,99,100,101,102,103,105,106,108,109`
- Wave power (Elemental Wave, Arcane Wave, Frost Spikes, Frost Pierce): `66,68,70,72,75,78,81,84,87,90,93,96,99,102,105`
- Frost Spikes slow: 40% ×4, 42% ×5, 45% ×6 · Witches Curse M.Def: 30% ×4, 32% ×5, 35% ×6 · Frost Pierce bleed rank **10 flat**
- ✅ **The SP/gold ladder is `HealerFourthSp` / `HealerFourthGold` exactly** — 6.5kk/11kk/16kk/80kk then SP 0 and gold 5kk→100kk. Reuse them; do not restate.

**Mage Armor Mastery's 15 new rungs** (his DESCR, in order 76→90): P.Def `89,91,92,93,95,96,97,99,100,101,103,104,105,107,108`; max MP `220,220,250,250,250,290,290,300,300,300,330,330,350,350,400`; mpWhenRestored `60% ×4, 65% ×5, 70% ×6`; **M.Def % `2,4,5,7,8,10,11,13,14,16,17,19,20,22,25`** and **MP-consumption reduction `0,0,5,5,5,8,8,8,8,8,10,10,10,10,10`** — the last two are NEW columns this ladder never had. M.Def% fits `StatMods.MDefPct` in the robe profile; the MP-cost cut has no StatMods field, so give the rung a second, robe-gated `PassiveEffect(RequiredArmor: Robe, MagicMpCostPct: …)` — the `SkillLevel.ExtraPassives` idiom, not a new StatMods field.
  ❓ **His "Decrease Mp Consumption" is unqualified.** Built as MAGIC-channel unless you say otherwise; a nuker casts magic, and the warrior's toggle only took the physical channel because his row said "p.mp".

### 🔴 SIX SKILLS ARE NEW, and four of them need engine work

1. **`nuker_shield_mastery`** @76 — a robe caster's shield passive: `RequiresShield`, M.Atk +5%, MP cost −10%, MP regen +10%, P.Def +100, **and the shield can never block** (`BlockChancePct: -1f`, which is the existing ×(1+pct) channel reaching ×0). No engine work.
2. **`nuker_mana_barrier`** @85 — 30 MP, 300s reuse, 30s, **5 SP bottles** (`LearnConsumableId: ItemCatalog.SpBottle`, the `archer 4th` idiom). 🔑 **A def called `mana_barrier` ALREADY EXISTS in `Skills.Mage.cs` with his exact numbers (70% / 0.5 MP / 30s / 300 reuse) and NO class table learns it** — an orphan, like Dispel Magic was. Change its id string to his `nuker_mana_barrier` rather than authoring a second one.
3. **`nuker_Force_empowerment`** @78/80/82 — a toggle: M.Atk +14/15/16%, magic MP consumption +20/15/10%, **50/40/30 HP a second**. Same shape as `double_mastery`; no engine work.
4. **`nuker_{human,elf,demon}_spell_empowerment`** @80/85/90 — a 600s self buff: magic MP cost up, M.Atk up, **and a 5% on-being-attacked proc whose payload lands on the ATTACKER**. 🔴 **THIS IS THE ENGINE GAP.** The proc machinery exists (`ProcOnDamaged`, `ProcVictimRungs`) and buff-carried procs already run, but `TryOnDamagedProcs(target, magicHit)` never passes the attacker, so `ProcVictimRungs` can't fire on a defensive proc. Two changes: pass the attacker through, and teach `PayOutProc`/the victim arm to deal DIRECT DAMAGE (the Human's *"inflicts damage on attackers with power 47/51/55"* — the Elf and Demon payloads are ordinary debuffs and already work).

### Before it can be called done

- A `Check.Specs` line for `nuker 4th` — **it earns one**, the file is finished.
- `dotnet run --project tools/SkillCsvSeed -- --check` green, and `--chains` re-read.
- Register to `Discipline.Magus` only — `Tempest` was retired (`BL-97`).
- ⚠ **NEW APK**: the class-skill table changes.

---


## `BL-208` ✅ THREE OF FOUR CLOSED 2026-09-11 (0.132.0), SAME DAY — the toggle drain laddered, the burst rung explained

Filed and answered within the hour. Your three rulings, and what each did:

1. ✅ **THE TOGGLE DRAIN NOW LADDERS, BOTH BARS.** *"Make togles to can change value of drain per lvl
   .. Some can drain more mp why some cant drain less hp?"* — and the asymmetry was exactly that:
   `SkillLevel.MpPerSecond` got its per-rung slot on 2026-08-27 and the HP half simply never did.
   Added `SkillLevel.HpPerSecond` + `SkillDef.HpPerSecondAt(level)`, and BOTH readers go through it now
   (the tick loop and the description card). Force Empowerment really drains **50 / 40 / 30** at
   78 / 80 / 82. Holy Soul and Overpower Mastery are untouched — a rung of 0 inherits the def.
2. ✅ **THE 10s / 10s ON THE SPELL EMPOWERMENT RIDER STANDS.** *"I haven't written the duration and cd of
   empowerment debuff part 10/10 is good call"*. Ratified, so they are no longer unauthored numbers.
3. ✅ **THE RACE-BURST @80 RUNG IS NOT A WASTED RUNG, AND THE REPORT WAS WRONG.** *"it don't give power
   but it gives higher debuff chance .. the magic become lvl 80 not 74 .. (it won't fail anyway but
   atleast debuff will land more often)"*. Correct, and it is the rule Witches Scarecrow already runs
   on: `DebuffLandChance` reads the RUNG's own LEARN LEVEL, so an identical spell bought at 80 wins a
   level contest a 74 one loses. A Burst cannot fizzle (`SureHit`), so the level term has nowhere else
   to show — buying the rung buys the rider's landing rate and nothing else. The code comment said
   "buys nothing" and now says this. ⚠ Damage stays as authored until you playtest it.

### The original entry's text, for the record

It listed four items; the fourth is still open and lives in `Backlog.md`. Items 1, 2 and 4 of that
list are the three above. Item 3 was Pyro Burst's inert `(success chance x1.5)` cell.

---

---


## `BL-209` ✅ CLOSED 2026-09-12 (0.133.0) — HARMONY OF THE WIZARD'S MAGIC CRIT RATE IS INSIGHT'S

*"We need to make harmony of wizard crit rate be same as insight (x2 not x1.3)"*

One number: the buff's crit-rate magnitude on rungs 4 and 5 (levels 78 and 79) went `0.30f` → `1.00f`,
which is exactly what `Insight`'s top rung carries. Both DESCR cells moved with it in
`buffer 4th.csv` **and** `buffs.csv`, which mirrors the same ladder and would otherwise have gone
stale in silence.

🔴🔑 **AND THE NUMBER HAD NEVER MATTERED, WHICH IS THE REAL FINDING.** Raising it changed nothing in
the rig, because the def's `Effect` mask never declared `BuffMagicCritRate` — see **`BL-214`**. The
30% had been discarded since 0.106.0. Fixing the mask is what actually gave you a crit rate; this
ruling is what then takes you to the ceiling.

📐 Measured (`--mcrit 90 epic`): a fully-blessed Magus of every race now reads **20%**, the
`StatCaps.MagicCritRate` cap, where a Human read 8.8% before. ⚠ The cap is therefore now the binding
constraint rather than the buff — carried forward as lever 3 of **`BL-215`**.


---


## `BL-210` ✅ CLOSED 2026-09-12 (0.133.0) — HARMONY MARK GIVES MAGIC CRIT DAMAGE, AND ×3.12 IS EXACT

*"also harmony mark don't give m crit dmg ..my crit dmg stays 2.6 but it should go to 3.12 (and we
should not limit it ..no other buffs to increase it x3.12 is max currently - hwi x1.3 mark x1.2 ==
3.12 not 2.6)"*

**You had already designed it and the code had never carried it.** The Mark's payload has thirteen
universal lines and every one of them is +20% — including `BuffCritRate`, `BuffMagicCritRate` and
`BuffCritDamage`. The fourth cell of that square, magic crit *damage*, was simply never authored, and
the rung text said *"critical rate and damage"* as though it were. Your own comment column on the
level-83 row proves the intent, written weeks ago: *"crit dmg we have 35% + 35% + 20% for phisical and
30+20% for magical"* — the `20%` in "30+20" is this Mark.

Built as `MagicCritDamage: 0.20f` on the def. 🔑 **It is a FIELD, not a magnitude, and that is why it
was missed**: `SkillEffect` has had no bits since `1L << 62`, so anyone auditing the skill by reading
its `Magnitudes` array sees twelve lines and no thirteenth. The healer's three Marks carry it the same
way.

📐 `StatCaps.MagicCritDamageBase 2.0 × 1.30 (Harmony of the Wizard) × 1.20 = ×3.12`, to the digit,
because `MagicCritDamageMult` compounds. The cap is ×5, so nothing is limiting it — your
*"we should not limit it"*. `--mcrit 90 epic` prints the chain.

**The checker learned to read it.** `magiccritdmg` is its own metric now, declared ABOVE `magiccritrate`
in `Descr.cs` — until today `magiccritrate`'s `"magic critical"` alias swallowed the cell
*"+30% magic critical dmg"* and compared it against the crit RATE. That went unseen for a chronicle
for the worst possible reason: both numbers on that row were 30, so the wrong reading agreed with the
right one.


---


## `BL-211` ✅ CLOSED 2026-09-12 (0.133.0) — CRIT-RATE RESIST CUTS THE BLOW RATE, REVERSING `BL-188`

*"The only think I want is the light armor mastery and every crit chance reduction passive/buff to
lower the blow rate as well (the blow is crit dmg so heaving less chance to be hit by crit means blows
as well. I forgot to mention)"*

```
rolledBlow = BlowRate × (1 − target.BlowResist) × (1 − target.CritRateResist)
```

The two defender terms **multiply**; they do not sum. Two independently-capped ladders that added
could pass 100% and invert the roll, and multiplying leaves the tank's own skill worth exactly its 30%.

🔑 **THIS KNOWINGLY REVERSES `BL-188`, WHICH LEFT IT OUT ON PURPOSE AND SAID WHY:** the rogue's own
Armor Mastery carries 25-35% crit-rate resist, so wiring it in makes light armour the best anti-rogue
armour in the game. That consequence has not gone away — it is now the intended one, and it is the
point of the ask: you had just measured duals two-or-three-shotting a mage and wanted the light kits
to have an answer.

📐 Measured at 90 (`--blowrate 90 epic`, which grew two defender columns for this): a light-armour
target reads 35% and multiplies an incoming blow by **0.65** — against **0.70** for the tank, who now
has company. A maxed Human dagger's 60% gate reads 39% into an archer or another dagger.

⚠ A shield's `ShieldCritDefense` still does NOT touch the roll. Your sentence named *passives and
buffs*; a shield is an item stat with its own layer, and folding it in would hand a third one to the
class you had just called *"almost immortal"*. Say the word if you meant it too.


---


## `BL-214` ✅ CLOSED 2026-09-12 (0.133.0) — FOUR BUFF PAYLOADS THAT HAD NEVER APPLIED, AND THE GUARD

**Not asked for — found while measuring `BL-209`.** Its crit-rate number went from 30% to 100% and the
rig printed the same rate before and after. A number that refuses to move when you change it is the
loudest signal there is.

🔴🔑 **THE MECHANISM.** A `BuffInstance` takes its `Effect` MASK from the SkillDef and its MAGNITUDES
from the RUNG, and every channel in `Entity.RecomputeDerived` is read behind `buff.Has(flag)`. So a
magnitude authored on a rung whose flag the def does not declare is discarded — no error, no log line,
nothing on any screen, and the skill card still advertises it, because `SkillText` reads the magnitude
directly. `SkillCsvSeed --check` cannot see it either: it compares your authored number against
`MagnitudesAt`, which is exactly the value the engine then throws away.

**The four, all authored, priced, documented, CSV-checked and inert:**

| skill | dead payload | dead since |
|---|---|---|
| **Harmony of the Wizard** | +20% MP regen **and** the whole magic crit-rate line | 0.106.0 |
| **Harmony of Protection** | 10% bow resistance (rung 6, @76) | 0.106.0 |
| **Lethal Precision** (Elf) | +10/15/20% crit damage — i.e. **the entire buff** | 0.121.0 |
| **Lethal Focus** (Human) | the crit-damage half of it | 0.121.0 |

🔑 The Lethal pair is the one that stings: `BL-188`'s whole design is *"the race split IS the
balance"* — Elf buys crit DAMAGE, Demon buys rate, Human splits — and only the Demon's half ever
worked, because his rides the `BlowRatePct` FIELD rather than a magnitude. `--blowrate` had been
printing the Elf's "+ race buff" column identical to his "passives only" column for nine versions and
nobody read it as a defect.

**Fixed, and then guarded so it cannot recur:** `SkillCatalog.BuildCatalog` throws at startup if any
def authors a magnitude its mask omits — the third member of the family beside the duplicate-id, the
child-id and the `CoveredKeys` guards, all of which exist for the same reason. `dotnet run --project
tools/BalanceMatrix -- --maskaudit` lists them; it reads **CLEAN**.

⚠ **Only that direction is an error.** A mask flag with no magnitude is legitimate and common: it is
how a ladder declares a channel its later rungs will fill, and how a FIELD payload keeps a buff
landable while carrying its real value elsewhere (Lethal Frenzy declares `BuffCritRate` with no
magnitude and pays through `BlowRatePct`).

---


## `BL-212` ✅ CLOSED 2026-09-12 (0.134.0) — THE CREATURE DAMAGE CURVE, AND IT IS A LEVEL MOD

Filed the same day the flat ×2 shipped, flagging that the ×2 was level-flat while the `BL-185` loss it
corrected was not. **You ruled within the hour:** *"Let's make it lvl mod as u said. But <76 to restore
what they lost (be as it was before bl185) and 76 to become harder."*

```
mult(L) = levelMod(L) x (1 + 0.40 x clamp((L-76)/14, 0, 1))
          L 20 -> x1.09    L 52 -> x1.41    L 76 -> x1.65    L 90 -> x2.51
BOSS: exempt
```

📐 **Measured against your own two anchors and it lands on them.** You gave, off your level-90 mage:
a normal creature ~130 and an elite ~300, wanting ~300 and ~750, with the elite's doubled P.Atk
carrying it to ~1500. `--mobdmg 90 epic --buffed` reads **107 → 268** for a normal and **161 → 806**
for an elite — ×2.51 and ×5.0 respectively, which is your 130 → 300 and your 300 → 1500 to the ratio.
⚠ The absolute numbers differ from yours (our rig's level-90 epic mage sheet is not your character,
and its elite reads 161 raw where you saw 300) — it is the RATIOS that are yours.

🔴 **THE BOSS IS EXEMPT** — *"Bosses to compensate with their passive so they won't change after the
base increase"*. Written as an exemption rather than as a division of `MobRankScale.Atk`'s boss rung,
because this multiplier is LEVEL-SHAPED and that rung is a single constant: it could only have
cancelled at one level. The result is exactly yours — nothing about a boss moves.

⚠ An ELITE is **not** exempt; it takes the curve AND its own ×3.0 attack rung. Guards and towers ride
it too, and their `MobMod.PAtk` multiplies on top as it always did.

🔑 **It reads the TARGET's level**, because what it undoes lives in the defender's own P.Def, and both
sides are tested: the attacker must not be a player and the target must be one. `BL-185`'s level term
is applied to player stats only, so paying it back on a mob-vs-mob hit would invent damage nothing
ever removed.

### The 0.133.0 text, superseded the same day

## `BL-212` 🔵 THE CREATURE ×2 IS BUILT — but it is LEVEL-FLAT and the loss it corrects is not

**BUILT exactly as you asked (0.133.0)**, both halves: every creature's finished damage is ×2
(`MobRankScale.MobDamageOut`, applied once in `GameLoopService.FinalizeDamage` to any attacker that is
not a player), and an ELITE's attack went ×1.5 → **×3.0**. On a basic attack the two compose to your
**×4**; on a mob skill carrying power it is less, because damage is a ratio and only the `pAtk` half
of `(pAtk + power)` is doubled by the elite's rung. Your *"not patk just dmg"* was the right call and
for a second reason as well: the creature attack curve is fitted to IG off 2,831 measured monsters and
is on the inspect panel, so moving it would make every future comparison lie.

**What "the last update" actually was**, since it decides whether ×2 is the right size: `BL-185`
(0.117.0) gave the PHYSICAL channel the defender level term M.Def had always had. A defender's P.Def
is now multiplied by `(level + 89)/100`. **So the loss you felt is level-shaped and the correction is
not:**

| your level | P.Def multiplier `BL-185` added | damage it removed | what ×2 restores |
|---|---|---|---|
| 20 | ×1.09 | −8% | **×2.18 of what it was** |
| 50 | ×1.39 | −28% | ×1.44 |
| 76 | ×1.65 | −39% | ×1.21 |
| 90 | ×1.79 | −44% | ×1.12 |

At 90 the ×2 is barely more than the correction — which is what you asked for and it lands well. At
20 it is more than double an over-correction. **Measured** (`dotnet run --project tools/BalanceMatrix`,
the E4 farm loop, unbuffed and solo — so a floor, not what you play): HP spent per kill doubles at
every level, and **kills-until-your-bar-is-empty** falls from 29 → 11 for a level-36 tank, 43 → 15 for
a melee rogue, and **8 → 3 for a level-36 nuker**. That is the same population `BL-72` already flags
as not surviving an unbuffed auto-farm.

🔵 **THE ONE CHOICE LEFT IS YOURS, and it is one line either way:**
- **Keep the flat ×2.** Simple, it is your number, and the low levels get harder. Nothing to do.
- **Make it the level term itself** — multiply creature damage by `PhysicalDefenceLevelMod(target)`
  instead of by 2. That restores *exactly* what `BL-185` took, at every level: ×1.09 at 20, ×1.79 at
  90. At the level you actually play it is 10% under your ×2 and you would not feel it; at 20 it is
  half of it.
- **Both** — the level term with a floor or a small flat bonus on top, if you want the hit "noticed"
  at 90 as well as restored.

⚠ **A BOSS TAKES THE ×2 TOO.** You measured bosses as *"OK for now ... They do ok dmg no1 survives"*
**before** this change, so their ladder (×4 × rune ×2 × solo ×2) now rides on top of a doubled base.
If a boss overshoots at the next playtest, the knob is `MobRankScale.Atk`'s boss rung, not this one.


---


## `BL-215` (superseded 2026-09-12) — the three-lever version, before you answered the IG question

## `BL-215` 🔵 THE MAGE'S DAMAGE — the crit pass bought +25%, and the three levers left are all yours

You asked one question directly: *"if we touch a bit the magic K(91) and increase it with like 30% does
will increase the overall dmg and will it break the low lvls? Or we need to increase the 76+ spells
power?"* Here is the measured answer, and then the choice.

### First: what today's work already bought, before any of these levers

Three things landed in 0.133.0 — your two crit rulings (`BL-209`, `BL-210`) and a bug neither of us
knew about (`BL-214`: Harmony of the Wizard's crit-rate line had **never worked**, because the def's
`Effect` mask did not declare the flag). Measured with `--mcrit 90 epic`, a fully-blessed Magus:

| | crit rate | crit damage | average damage multiplier |
|---|---|---|---|
| before | 8.8% | ×2.60 | ×1.141 |
| **after** | **20%** (the cap) | **×3.12** | **×1.424** |

**+25% average magic damage**, and your ×3.12 lands on the digit. **Re-playtest before pulling
anything else** — a good part of what you were reaching for with `MagicK` has already arrived.

### 🔴 Lever 1 — `MagicK` 91 → ~118. My recommendation: NO.

- **It is IG's constant, verbatim**, and so is `PhysicalK 77` (`docs/balance/DamageVsIG.md`). The last
  real measurement put our mage **1.19× ABOVE** IG's own observed damage at 76 unbuffed. Nothing in
  the constant is short.
- **It answers your "will it break the low lvls?" with: it changes them by exactly the same 30%.**
  `MagicK` is a flat scalar with no level term. The rig already reports a level-8 mage's first nuke
  killing a same-level mob in **one cast** (91 damage vs 91 HP) and a level-20 mage overkilling by
  1.7× — a 30% rise deepens both. Meanwhile at 90 an elite has ×4 HP and ×1.33 M.Def, so 30% takes it
  from ~24 casts to ~18. **The deficit you feel is level-shaped; this lever is not.**

### 🔵 Lever 2 — the 76+ spell power. The right shape, and it is YOUR file.

`nuker 4th.csv`'s blast ladder is **110 → 138** across 76 → 90 (+2 a level), and the 3rd tier ends at
108 at 74. So the whole 4th tier is +28% of power over fifteen levels while mob HP grows ~40% and an
elite multiplies it by four again. This is the number that decides endgame mage damage and it is
authored by you — I will not retune a CSV. **Give me a new column, or a multiplier to apply to it.**

### 🔵 Lever 3 — the magic crit-RATE cap, `StatCaps.MagicCritRate = 20%`. Newly load-bearing.

Your `BL-209` ruling took Harmony of the Wizard from ×1.3 to ×2 on the rate — and a fully-blessed
Magus of **every race** now sits exactly ON the 20% ceiling, so the second half of that buff is being
thrown away. That is not an argument against the ruling (it is what makes the Human and Demon reach
the cap at all, and they did not before), but it does mean **the cap is now the lever, not the buff**.
Its own doc-comment anticipated this: *"still max 20% but one day if we want to increase it no mage to
be short on crit"*. At ×3.12 crit damage, every 5 points of cap is about **+10%** average damage.

⚠ **And one thing that got HARDER today, which you should weigh with all three:** `BL-212` doubled
every creature's damage and tripled an elite's attack. The mage was already the sheet that spends the
most HP per kill.

---


## `BL-216` ✅ CLOSED 2026-09-12 (0.135.0) — THE SHOT CUTS 30% OFF THE FINAL CAST TIME

Filed the same day, measured but not built, because the premise was yours to confirm. **You confirmed
it with the arithmetic:**

> *"It increases the cast speed behind the scene with ~40% ..which is actually 30% decrease on the
> final cast time. So if rune is active the cast time of a spell is:
> `(baseCastTime/(charCastSpeed/333))x(runeActive ? 0.7 : 1)`. So a max cast speed of 1999 with spell
> that is 4000ms -> 4000/(1999/333) = 667 ms but with rune active it becomes 467ms."*

Built as `CastTimePct: 0.30f` on the Spell Rune — `CastTimeMultiplier = clamp(1 − Σ CastTimePct, 0.2,
3)`, so the finished cast is ×0.70 exactly as your formula says. 📐 Elemental Blast on a fully-stacked
level-90 Magus: **0.70s → 0.50s**, and the whole cycle 0.90s → 0.70s.

🔑 **The old `BuffCastSpeed 40` FLAT grant is kept beside it and is not the same thing.** Forty points
on a stat an endgame caster carries at 1400-1900 is about **+2%**, and at `StatCaps.CastSpeed` (1999)
it is worth nothing at all — while a cast-TIME cut survives the cap, because `CastTimeMultiplier`
multiplies after the 333 model. Removing the flat grant would have been a silent nerf to every
low-level caster to fix a complaint that only exists at 90, so both stand.

⚠ **The War Rune deliberately did NOT get the same.** You described the blessed SPIRITSHOT; whether
IG's soulshot shortens an attack is yours to say, and a blanket 30% off every fighter's skill
animation is not a small change to make on an inference.
⚠ Ticks are 100ms and `BeginCast` truncates, so your 467ms reads as 0.40s here at 1999. That is the
tick rate, not the number.

### The 0.134.0 text, as filed

## `BL-216` 🔵 THE SHOT DOES NOTHING FOR A CASTER'S CAST TIME — and the cap is why

*"why casting feels slow? I have 1500 cast ~4 times and 1s feels so long in real fight, ig with that
king of cast speed is almost instant ... (also ig bsps add 40% to the casting - a 40% reduction in the
final cast. May be that's is)"*

**I checked all of it. Our cast model is IG's, to the arithmetic**: `castTime = authored × 333 /
castSpeedStat`, so at 1500 a 4s spell takes 0.89s here and would take 0.89s there. Nothing is broken
and nothing is missing from that formula. Two things came out of the measurement instead.

### 1. 🔴 It was never the cast. It was the REUSE. (Fixed — your item 6.)

`--castcycle 90 epic`, an NPC-buffed Magus, and the reuse starts when the cast LANDS so the cycle is
the sum:

| | cast | reuse | cycle |
|---|---|---|---|
| before | 0.60s | 0.80s | 1.40s |
| **after your 0.5s** | 0.60s | **0.40s** | **1.00s** |

**57% of the cycle was reuse**, and the only thing shortening it was Spell Mastery's −20%. Your
instinct in item 6 was the right lever and it is built. Note the cast reads **0.60s, not 0.89s**: a
buffed Magus of every race is **on the cast-speed cap** (`StatCaps.CastSpeed` 1999). If you are seeing
1500 you are short of it, and the difference is 0.89s against 0.60s.

### 2. 🔵 THE SHOT — and here you may well be right. Your call.

The Spell Rune grants `BuffCastSpeed 40`, **FLAT**, on a stat that is already 1400-1999. **That is
worth about +2%.** And because you are at or near the cap, a cast-SPEED grant is worth nothing there
at all — whereas a cast-TIME cut still multiplies, since `CastTimeMultiplier` is applied *after* the
333 model. So if IG's blessed shot really is −40% on the final cast, we are delivering roughly a
fifteenth of it, and the channel to express it properly already exists (`SkillDef.CastTimePct`,
`BL-196` — the archer's Spirit Mastery is its only author today).

**One line**: `CastTimePct: 0.40f` on the Spell Rune, and Elemental Blast goes **0.60s → 0.30s**, the
cycle 1.00s → 0.70s — another **×1.4** on mage damage.

🔵 **I did NOT build it, for two reasons.** (1) It is a design change to the shot on a premise only you
can confirm — I could not verify the −40% against IG, and everything else in this pass was measured.
(2) The mage is already at **≈×2.3** from this pass alone (`BL-215`); another ×1.4 makes it ×3.2 and
the playtest stops being readable. Say the word and it is one line, better after you have felt the
rest.

⚠ **It would reach every caster, not just the nuker** — the healer and the buffer hold the same rune.

---


## `BL-217` ✅ CLOSED 2026-09-12 (0.136.0) — REUSE REDUCTIONS COMPOUND, AND THE CLAMP IS GONE

Filed the same evening the ladder was built, because the ladder landed on ×0.25 where your own
arithmetic said ×0.416. **You ruled option 2 within the hour:**

> *"Still additive as our was ..to 40% that's still 0.6 cooldown of a 1s spell ... Make it
> mutiolicative if u haven't as any other buff is ... Don't add clamp no need when it mutiolicative -
> I want to test with the 0.42 not 0.25 and if it additive to 80% the spell never can go bellow 0.2s."*

```
reuse  = authored × retain                     min 1 tick; skipped when FixedCooldown
retain = CooldownRetain × (physical ? CooldownRetainPhysical : CooldownRetainMagic)
         each source multiplies its channel's retain by its own (1 − r).  NO CLAMP.
```

🔑 **WHAT IS STORED IS WHAT SURVIVES.** The three `CooldownReduction*` properties are computed getters
now (`1 − retain`), so the stat panel, the `StatsUpdate` DTO and the target inspector all keep seeing
an ordinary reduction fraction — nothing on the wire changed and no other reader needed touching. And
a stray `+=` on one of them is now a **compile error** rather than a silent return to summing, which
is the point of making them read-only.

📐 Measured (`--castcycle 90 epic`), the caster stack: Spell Mastery 20% (blanket) × Harmony of the
Soul 20% × Harmony of the Wizard 35% (both magic) = **×0.416**, your number to three decimals.
Elemental Blast's 1s reuse reads 0.40s (ticks are 100ms), and the full cycle is 0.90s against the
1.70s you were playing.

🔑 **The three 0.8 clamps are gone and it is your reasoning that removed them** — *"if it additive to
80% the spell never can go bellow 0.2s"*. They were a hard floor of 0.2× the authored reuse, and the
caster stack had already reached 75% under summing, so your next tuning step would have moved a number
the engine had stopped reading. A product of `(1 − r)` terms approaches zero and never arrives, and
`ExecuteSkill` floors the finished reuse at one tick regardless.

⚠ **It reaches PHYSICAL reuse too**, which is what you asked for ("as any other buff is") and worth
knowing at the next playtest: Harmony of the Soul's −30% physical beside Bow Blessing's −20% now reads
×0.56 where it read ×0.50. Every stack of two or more reuse sources in the game got slightly *weaker*;
only stacks of three or more (the caster's) got stronger.

### The text as filed

## `BL-217` 🔵 THE MAGIC REUSE STACK IS BUILT — but our reductions SUM and your arithmetic MULTIPLIES

**BUILT (0.135.0).** Harmony of the Wizard gains three 3rd-tier rungs, exactly as you specified:

| rung | level | MP | SP | payload |
|---|---|---|---|---|
| 3 | 58 | 150 | 88k | +10% M.Atk, +30% cast speed, **−15% magic reuse** |
| 4 | 66 | 170 | 280k | … **−25%** |
| 5 | 74 | 190 | 880k | … **−35%** |

Every rung above (77/78/79, now numbered 6-8) carries the −35% forward, because a harmony rung is
cumulative. ⚠ The three MP figures are **mine** — they sit between rung 2's 126 and the 4th tier's 199
so the ladder stays monotonic without moving a cell you authored. The SP are **yours**, read off what
your other harmonies charge at those exact levels.

✅ **And your suspicion about Harmony of the Soul was wrong, which is why this is only a ladder.**
*"if the harmony buff don't reach the spell reuse and we fix it it should be ok"* — it reaches.
`SoulRung` authors `MagicCooldownPct` 0.10 → 0.20, `ApplyBuff` copies it, `RecomputeDerived` folds it
into `CooldownReductionMagic`, and `CooldownReductionFor` reads it for every magical skill. Nothing was
broken; the stack was one source short. I checked before building.

### 🔴 THE ONE THING THAT NEEDS YOU

**Our reuse reductions SUM. Yours multiply.**

```
ours:   authored × (1 − (20% + 20% + 35%))  =  × 0.25       ← clamped at 80%; we are at 75%
yours:  authored × 0.8 × 0.8 × 0.65         =  × 0.416
```

So the endgame magic reuse is **~40% shorter than you intended** — Elemental Blast's 1s reuse reads
**0.20s**, not the 0.42s your model gives. Measured cycle (`--castcycle 90 epic`), Elemental Blast:

| stack | cast | reuse | cycle |
|---|---|---|---|
| NPC shelf only (where you were) | 0.90s | 0.80s | **1.70s** |
| + Harmony of the Wizard L8 | 0.70s | 0.40s | 1.10s |
| + Harmony of the Soul L7 | 0.70s | 0.20s | 0.90s |
| + Spell Rune (`BL-216`) | **0.50s** | 0.20s | **0.70s** |

🔴 **And it matters more than the 40%, because of what you said next.** *"if still feels slow I'll up
the souls and mastery to 30%"* — under summing that is 30 + 30 + 35 = **95%, clamped to 80%**. You
would be tuning a number the engine has stopped listening to. Three ways out:

1. **Leave it summed and re-cut the numbers.** To land on your ×0.416 the three must total 58.4% —
   e.g. leave mastery and souls at 20 and make the harmony's top rung **18%** instead of 35%.
2. **Make reuse reductions COMPOUND** (`1 − r` multiplied instead of summed). One line, matches IG,
   matches how every other buff channel in this game already stacks (crit rate, magic crit damage,
   cast speed), and the 0.8 clamp stops being reachable by accident. ⚠ It reaches PHYSICAL reuse too,
   so every class's numbers move a little — Harmony of the Soul's −30% physical beside Bow Blessing's
   −20% would read ×0.56 instead of ×0.50.
3. **Leave it as built** and accept a faster endgame caster than IG's.

**I did not choose for you** — the summing rule is a documented engine decision that reaches every
class, and CLAUDE.md says to discuss a mechanic change of that size first. My pick is **2**.

### 🔵 Also still open

The **20% magic crit-RATE cap** (`StatCaps.MagicCritRate`), which every race now sits exactly on —
about +10% average damage per 5 points. It was lever 3 of `BL-215` and nothing has changed it.

---

## `BL-219` ✅ BUILT 0.137.0 (2026-09-12) — the target window: debuffs only, abbreviated, and LIVE

Playtest 2026-09-12: *"Remove the positive effect of the target window …leave only the abriviation of
debuffs.. Also I don't think the target window even debuffs are updated when they suppose to … I'm a
venom and hitting a mage ..when stab lands I suppose to see x3 but I dont ... I land several more
then in one go I see x9 ... Some times I se 3-6-9-10 ... Some times I see 6-9-10 ... At random ..
Like some kind of update interval ..."*

**It was exactly an update interval.** `PushTargetBuffs` ran on the once-a-second `secondTick`, the
same beat as your own buff bar. A stab banks its venom the instant it connects, so two stabs inside
one second arrived as a single jump of six, and the same two either side of the beat arrived as two
threes — which is precisely the 3-6-9-10 / 6-9-10 pattern, and why it looked random: the beat has no
relationship to when you press anything.

**Now it runs every tick (10/s).** 🔑 The change that makes that free is the ORDER: the signature is
built FIRST, straight off the buff list (name + stacks + whole seconds), and the expensive half — the
DTO list with its descriptions, icons and source lookups — only runs on the ticks where something
actually moved. A selected creature with an empty buff list is zero iterations and no message. The
countdown did not become ten times chattier (seconds are still rounded into the signature); only the
STACKS became immediate.

⚠ Extracted `StacksShown` so the signature and the DTO read the SAME number. When those two were
written out separately, a fold applied to one and not the other is exactly how the bar came to show
"x7" while the venom ticked for one (`BL-198`).

**And the window itself:** the beneficial half of the line is gone — it was there because a creature
carrying a blessing is worth seeing, but it is rare and on one ellipsised row it competed for width
with the only numbers you are reading. Names are abbreviated: a multi-word name becomes its INITIALS
("Venom Stab" → `VS`, "Vital Organ Protection" → `VOP`), a single-word one keeps its first four
letters ("Gravity" → `Grav`). The stack count is never shortened. The green/red colouring went with
the positive half — every row is a debuff now, so the colour said nothing.

⚠ **NEW APK** (client-side half). No protocol bump — nothing on the wire moved.

---

## `BL-220` ✅ BUILT 0.137.0 (2026-09-12) — DoT/HoT out of the combat chat

Playtest 2026-09-12: *"Remove dot/hot from combat chat (or make it option for the client) it's to
much flood and miss the dmg."*

Both: removed by default, with **Settings → `DoT/HoT in chat`** to bring them back. Default OFF
because you asked for them removed and offered the toggle as the alternative — a 30-second bleed
writes thirty lines into the one tab you are reading to see what your stab hit for.

🔑 **IT HIDES A LINE OF TEXT AND NOTHING ELSE.** The filter sits BELOW `CombatHappened`, so the
floating numbers over the target, the attack animations and the "who is hitting me" list all still
see every tick — watching a poison tick on the mob is still how you know it landed.

Covers the DoT tick, the HoT tick and the MANA half of a heal-over-time (Harmony of Restoration's
"+5 MP/s"). ⚠ **Mana VAMPIRISM is deliberately not covered**: it shares the `Mana` tag but is a
per-HIT effect broadcast as `Heal` rather than `ManaHeal`, so it is not a tick and you did not ask
for it. One word if you want it in.

⚠ The three tags moved to `GameConstants` (`DotTag` / `HotTag` / `ManaTickTag` / `IsTickTag`). They
were string literals on the server with a matching literal in the client's floater code — the
arrangement where renaming one half silently breaks the other and nothing errors.

⚠ **NEW APK.**

---

## `BL-221` ✅ BUILT 0.138.0 (2026-09-13) — Magical Armor 30% → 50%

*"Alao increase the magic armor to 50% ..it's a 10s buff ..let him take less dmg"*. Done, and the CSV
row with it.

| | mRes | divisor | a Magus's Arcane Burst |
|---|---|---|---|
| bare (the race passive alone) | 10% | 1.100 | 734 |
| + Magical Armor, was | 40% | 1.400 | 577 (×0.79) |
| + Magical Armor, **now** | **60%** | **1.600** | **505 (×0.69)** |

So it is **−31% on his own damage** now instead of −21%, and **−38%** against a dual carrying
neither. ⚠ The reminder from the measurement that opened this entry still holds: resistance is a
DIVISOR, so "+50%" is not "half damage" — it is ÷1.6.

---

## `BL-222` ✅ BUILT 0.138.0 (2026-09-13) — a trap could only ever see MOBS

*"Also both players are flagged both players are with pvp on ..and enemy cannot trigger trap ... Only
mobs... A trap should trigger when I'm put it and I'm with pvp on ... And any pvp /pk (not friendly)
walking over should trigger it if I'm with pvp off a trap triggers only by pk/mob (by any enemy that
wont flag me)"*.

🔴 **`FindTrapVictim` filtered on `e.Kind != EntityKind.Mob`, and the doc-comment above it read
"(and, once PvP exists, enemy players)"** — a TODO written before PvP shipped that nothing ever came
back to. The Trapper's entire discipline was PvE-only and nothing said so.

🔑 **THE RULE IS `CanPvpHit`, ASKED AS THE OWNER.** Your parenthetical — *"by any enemy that wont
flag me"* — is already exactly what that predicate means: a flagged or red player is always hittable,
an innocent one needs the PvP toggle. So the trap asks the ordinary attack question and inherits the
safe-zone check, the never-your-own-party rule and the guard/NPC doors for free. Nothing re-derived.

Two details that needed deciding:
- **The toggle is captured when you ARM it**, not read live (`TrapInstance.OwnerPvpEnabled`). Your
  *"when I'm put it"*. A trap already in the ground must not re-aim itself because you toggled PvP
  fifty metres away, where you cannot see it happen.
- **The safe-zone test reads the TRAP's position**, not the owner's, so walking back to town does not
  switch off a trap you left in the field. (`CanPvpHit` grew an overload for both.)

⚠ `FireTrap`'s sweep carried its own copy of the mobs-only filter, so without fixing it too an enemy
would have *sprung* a trap and walked away unharmed. One predicate, `TrapCatches`, both places — the
`BL-154`/`BL-123` lesson for the third time.

⚠ **ONE CONSEQUENCE, STATED NOT BURIED:** a trap armed with PvP **on** will trip on a clean (white)
stranger and flag you, exactly as swinging at him would. That is the bargain the toggle makes
everywhere else, but it is the one case where you are not standing there to choose. One line to
change if you would rather a PvP-on trap still ignored innocents.

🔵 Known edge, not fixed: `ApplyDamage` zeroes player-vs-player damage if the **attacker** is in a
safe zone, and for a trap the attacker is the owner. So a trap that fires while you are standing in
town does no damage (the CC still lands). Safe failure, rare, and the fix would mean threading a
position through the one seam every damage source in the game passes — not worth it for this.

---

## `BL-223` ✅ BUILT 0.138.0 (2026-09-13) — the rig was wearing four Marks and sixteen harmonies

Found while re-measuring `BL-218`. `SkillCatalog.NewbieBuffSet` has **contained**
`NpcSingleHarmonySet` and `NpcMarkSet` since `BL-160`/`BL-161` — its own doc-comment says so
("19 + 8 + 3 = THIRTY") — but `BalanceMatrix.ApplyNpcBuffs` still concatenated both again for
`fullShelf: true`. So the eight harmonies landed twice and the Marks four times; and even the PLAIN
shelf wore all three Marks, where all three share `MarkKey` with `FlatRank` and the engine allows
exactly **one**.

**Every "buffed" row this tool has printed since 0.113.0 was a character wearing buffs the game cannot
give him** — inflated M.Def, crit damage, cast speed and control resistance. `--mcrit` had a hand-written
workaround for the Mark half (it stripped `healer_mark` explicitly) which is the clue that was sitting
there the whole time.

🔑 **THE FIX IS THE ENGINE'S OWN INVARIANT: A BUFF KEY IS BUFF IDENTITY** — two buffs with the same key
never coexist on a bar. The shelf is deduped by key, which repairs this and any future overlap without
the builder needing to know which sets contain which. `fullShelf` now means what its name says: false =
the nineteen singles, true = all thirty.

⚠ **Other signed-off tables move**: anything passing `buffed` / `npcBuffed` — `--dmgmatrix`, `--stab`,
`--castcycle`, `--magicdef`, `--defbreak`. They are now LOWER and correct. If a number in an older
note disagrees with the tool, the tool is right.

⚠ Also added: `WarchanterParty()` (a real party buffer's blessings, which the NPC shelf does not sell
and which is what you actually play with), and it applies `CoveredKeys` so a class buff evicts the NPC
singles it contains rather than stacking with them (`BL-183`).

---

## `BL-224` ✅ BUILT 0.138.0 (2026-09-13) — Arrow Barrage: the `[Double]` off, power 2,500 → 2,000

*"OK a mage is killed by one arrow barrage .. Also remove double of arrow barrage if it can (I haven't
seen for about a x10 bae ages not a single arrow crit) but I don't hwat it to have.. Also decrease
it's dmg to 2k per arrow"*.

🔑 **THE DOUBLE WAS THE REAL PROBLEM AND IT IS ALSO WHY YOU NEVER SAW IT.** `BL-213` gave this skill
**ten independent doubling rolls** — one per arrow — where every other skill in the game gets one per
cast. At a ~10% mastery rate a ten-arrow volley doubles *some* arrow **65% of the time**, so the
volley's average ran ~10% hot while no single arrow ever looked doubled on screen. A per-shot roll on
a ten-shot channel is not the same mechanic as a per-cast roll, and the asymmetry is exactly the
report: a skill that deletes a mage while showing you nothing to blame.

Both the arrow def and the wrapper drop the flag — the wrapper carried it only so the skill card
could say so, and a card advertising a doubling that can no longer happen is worse than no card.

⚠ **`Power` is the AUTHORED power, not damage on screen.** Your *"~1k to an elit and ~870 to a mage"*
is what 2,500 produces through the ratio; 2,000 takes about a fifth off that, and losing the double
takes roughly another tenth off the volley's average. Combined: a barrage lands near **70%** of what
it did.

---

## Superseded text — `BL-218` and `BL-221` as they stood on 2026-09-12

Kept verbatim per rule 2 (a rewrite's old text comes here). `BL-218` was rewritten on 2026-09-13
after his 20% ruling and the `BL-223` rig fix; `BL-221` was closed as BUILT the same day.

## `BL-218` 🔵 WHY DEBUFFS DON'T LAND — measured; the SPT land-rate passive is yours to rule

Playtest 2026-09-12: *"Also debuffs almost never land wit all the resistanses we have … in general
debuffs don't land … not human stuns mage nor the other way around … Can you get me same lvl debuffs
and check their land rate with and without buffs/passives ? **I think we hit the floor for
landing**"* — and, conditionally, *"Can we add to a mage 40,76,80 a spt debuff land rate passive that
increases land rate of all spt debuffs 2 times (atleast in pvp) but 1st the ask below ?"*

📐 **THE MEASUREMENT IS DONE AND IT IS IN THE REPO: [balance/DebuffLandRate.md](balance/DebuffLandRate.md).**
New rig mode `dotnet run --project tools/BalanceMatrix -- --ccland [level] [quality]`, which builds
real 4th-tier characters and runs the exact product `GameLoopService` computes.

🔑 **YOU ARE NOT HITTING THE FLOOR.** `CcLandMin` is 10% and it clamps the STAT CONTEST only — which
between two level-90 characters comes out at **50-54%**, nowhere near it. What eats the number is the
three multipliers applied **after** the clamp, none of which is floored:

```
land = clamp(contest, 10%, 90%)      ~52% at parity
     × DebuffLandMod                 the skill's own: 1.50 / 1.00 / 0.70 / 0.50 / 0.30
     × (1 − CcResist)                0% / 0% / 28% / 40%  by GEAR QUALITY
     × (1 − CcResist<school>)        20% bare → 35% NPC shelf → 50% FULL shelf
```

So a fully-blessed level-90 target in mythic gear multiplies every incoming debuff by **×0.30**, and
a `×0.50` skill by **×0.15**. A tank's Numbing Shock lands **11%**. A Magus's Arcane Void lands
**6%**. The doc has the whole table, every class both ways.

**Three findings worth ruling on, smallest first:**

1. 🔴 **The flat `CcResist` is a GEAR CLIFF: 0% at common and rare, 28% at epic, 40% at mythic.** No
   buff feeds it — it is armour-set only, every class gets the same number from its own tier's set,
   and nothing on the attacker's side can answer it. This is the single biggest term in the product
   and it is why control stopped working around epic gear without anything being changed. **Halving
   it (0 / 0 / 14 / 20%) is my recommendation and costs nothing** — one armour-set number, no new
   mechanic, no `game.db` delete.
2. 🔵 **YOUR PASSIVE IS THE RIGHT SHAPE, and bigger than you may realise: there is no attacker-side
   land channel in the engine AT ALL today.** Every multiplier above is defender-side or authored per
   skill; the caster contributes one stat to a contest that moves by thirteen points end to end —
   which is why *"you cannot build for landing debuffs"* is literally true right now. One
   `PassiveEffect` field feeding one multiplier next to `DebuffLandMod` is the whole build. At ×2 it
   restores a fully-blessed target to Arcane Burst 64% / Gravity 44% / Mana Strain 22%. **Two things
   I need from you before building it:**
   - **×2 flat at all three rungs, or a ladder?** Three rungs at 40/76/80 reads as a ladder to me —
     ×1.3 / ×1.6 / ×2.0 — so a level-40 mage is not handed the endgame number.
   - **PvP only, or everywhere?** You wrote *"atleast in pvp"*. Both are one line. PvE control is
     already shortened by `MobCcSpt`, so doubling it there is a real farming change.
   ⚠ And if it goes in, it should be a channel every class can be given later, not a mage-only
   field — the tank's kit is control and sits at 11-27%.
3. 🔵 **The ×0.30 and ×0.50 skills are decoration now.** Your `BL-90` ruling priced those multipliers
   when the base at parity was 50% and nothing came after it. With three defensive layers behind
   them, ×0.30 means 6%. Raising the bottom tier to ×0.60 would be enough.

⚠ **The rig was lying until this pass**, and it is the fifth time the same builder has done it:
`ApplyNpcBuffs` never copied the `CcResistMagical`/`CcResistPhysical` **fields** (they are fields,
not `Effect`+`Magnitudes`, the flag enum being full), so the first run of this table read identical
buffed and unbuffed. Fixed in the same commit.

---

## `BL-221` ❓ MAGICAL ARMOR "DOES NOTHING" — the engine says it does; I need your two numbers

Playtest 2026-09-12: *"Also magic armor of null blade does nothing … he takes ~300 dmg less than
other duals because of his anti magic but with magic armor on the dmg is the same… Not 30% less"*.

**Measured end to end and it works** (`dotnet run --project tools/BalanceMatrix -- --mres 90`), a
level-90 Nullblade under a same-level Magus's Arcane Burst:

| stage | mRes | divisor | nuke damage |
|---|---|---|---|
| bare (the `dual_anti_magic` passive alone) | 10% | 1.100 | 734 |
| + Magical Armor | **40%** | **1.400** | **577**  (×0.79) |
| + NPC shelf | 10% | 1.100 | 272 |
| + NPC shelf + Magical Armor | **40%** | **1.400** | **213**  (×0.78) |

The def carries `BuffMagicResist 0.3 Percent` at both the skill and the rung level, `RecomputeDerived`
folds it, and `MagicDefCoef` divides the nuke by it — including a crit, which multiplies the already
resisted number.

🔑 **ONE THING TO KNOW: +30% MAGIC RESIST IS NOT −30% DAMAGE, AND IT NEVER WAS.** Resistance is a
DIVISOR (`damage ÷ (1 + mRes)`), the same shape as defence everywhere else in this game. Going from
+10% to +40% divides by 1.4 instead of 1.1, which is **−21%** on your own damage — but **−29.5%**
against a dual who has neither, which is probably the comparison you were making and is your "30%".
So the number you were looking for exists; it is just relative to the other dual, not to yourself.

❓ **WHAT I NEED:** −21% is not a subtle change, so if it really moved nothing, something in the live
path is not what the rig builds. Give me two numbers off one target — the same nuke's damage with the
buff visibly on the bar and immediately after it drops (it is **10 seconds**, 90s reuse) — and
whether the bar showed it at all. If they are equal I will chase the live path; if they are ~21%
apart the mechanic is fine and this entry closes.

---

## `BL-225` ✅ BUILT 0.139.0 (2026-09-13) — control resistances COMPOUND, and the clamps are gone

Your ruling: *"I want the cc resist formula to be something if we have harmony 20%, buff 20%, Passive
20% -> baseLandRate x LandMod x (1-buff1/passive1) x (1- buff2/passive2) x (1-buffN/passiveN) Or
something and having those 3 20% resists make the debuff land 2 times less (x 0.512) not ~4 (x 0.28)
as it was. And adding a set bonus u get to ~3 times less -> which is nicably less but not never"*.

```
land = clamp(contest, 10%, 90%) × DebuffLandMod
     × Π(1 − r) over every BLANKET source     (armour sets, shields)
     × Π(1 − r) over every SCHOOL source      (passive, class buff, harmony, Mark)
```

Three 20% resistances are **×0.512** exactly as you wrote, and with an epic set's 28% it is **×0.369**
— your *"~3 times less"*.

🔑 **THE FIELD STORES WHAT SURVIVES; THE RESISTANCE IS A GETTER.** Same shape `BL-217` used for reuse
in 0.136.0 and for the same three reasons: every existing reader kept working untouched, nothing on
the wire moved, and a stray `+=` on `CcResist` is now a **compile error** instead of a silent return
to summing. Accumulate through `AddCcResist` / `AddCcResistMagical` / `AddCcResistPhysical`, which are
the only writers.

⚠ **THE THREE 0.8 CLAMPS ARE DELETED.** They existed because summing could reach 100% and make a
character CC-immune; a product of factors below 1 never reaches 0. They had become ceilings the stack
was already sitting on — *"Don't add clamp no need when it mutiolicative"*, your 0.136.0 words, same
month, same mistake. Only a sign guard remains, against a source authored above 100%.

⚠ **NEGATIVE RESISTANCES STILL WORK** and are a second reason to store the retain: the Magus's curses
author `CcResistMagical: -0.40` and simply contribute a ×1.40 factor to the product.

⚠ `SchoolCcResist` was deleted and replaced by `SchoolCcRetain`, and the three roll sites now read the
retain directly instead of writing `1f - CcResist`. Algebraically identical — but a helper returning a
*resistance* sitting beside the product is precisely how the next edit reintroduces summing.

### ✅ You were right about the Marks

*"the con/spt resists are on a single marks not on the harmony one ... Ppl will chose harmony mark"* —
confirmed in the code. **Harmony Mark carries no control resistance at all**; only Holy Mark (SPT) and
Life Mark (CON) do, and all four share `MarkKey` with `FlatRank` so exactly one is ever on you. Both
cases are now measured rows in `--ccland`.

⚠ One correction: the SPT Mark is **15%**, not 10% — 10% is the CON one. So your ×0.460 is the CON
case; the SPT case is ×0.435.

### The result, level 90, fully buffed, same level

| ×1.00 skill lands | before today | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-22%** |
| physical (CON) | 8-10% | **10-13%** |

Your *"15-25% which is good"* is hit on the magical side. 🔵 **The CON side is not, and it is now the
outlier** — see `BL-218`.

---

## Superseded text — `BL-218` as it stood earlier on 2026-09-13

Kept verbatim per rule 2. Rewritten the same day once `BL-225` (compounding) was built and the
magical side reached his band, which moved the open question to the CON column.

## `BL-218` 🔵 WHY DEBUFFS DON'T LAND — your 20% ruling is IN; the band it was aimed at is not reached

**2026-09-13 — REWRITTEN.** Your ruling is built and the measurement is redone; the old text of this
entry is in the archive. 📐 The whole table: [balance/DebuffLandRate.md](balance/DebuffLandRate.md),
regenerated with `dotnet run --project tools/BalanceMatrix -- --ccland`.

### ✅ Built — your ruling, exactly as given

> *"I calculated we must do the harmony and buff also be 20% (not 30/50) that way the land rate will
> be 15-25% which is good"*

Harmony of the Soul's top rung **30% → 20%** SPT, Arcane and Feral Protection **50% → 20%** SPT, both
CSV rows moved with the code. ⚠ The **CON** half of Arcane/Feral (43→65%) is untouched: your message
is about SPT throughout, and that column is your authored CSV.

### 🔑 You were pinned on the 80% CLAMP, which is why only changing BOTH worked

The SPT sources SUM: passive 20 + buff 50 + harmony 30 + **Mark 15** = **115%, clamped to 80**. Same
trap as the reuse clamp you killed in 0.136.0 — dropping the harmony alone would still have summed
past 80 and moved **nothing**.

### 🔴 THE BAND IS STILL NOT REACHED, and it is arithmetic, not opinion

Against a target buffed by a real Warchanter, at level 90 in epic gear:

| | ×1.50 skills | ×1.00 skills | ×0.50 skills |
|---|---|---|---|
| magical (SPT) | **15-16%** | 10-11% | 5% |
| physical (CON) | — | 8-10% | 4-5% |

Only your best skill reaches 15%. Two reasons your arithmetic and the engine's disagree:

1. **You counted three sources; there are four.** A **Mark** carries 15% SPT (and 10% CON). Your
   20+20+20 = 60 is really **75**.
2. **They SUM; you multiplied.** `(1−.2)(1−.2)(1−.2)` = ×0.512 is the generous answer. Summing to
   75% is **×0.25**.

### ❓ Two ways to land your band — my pick is the first, and it is your own ruling

**(a) Make school resistances COMPOUND instead of summing** — *"Make it mutiolicative if u haven't as
any other buff is"* (your 0.136.0 words, same shape, and it takes the 80% clamp out of reach for
free):

```
(1−.20)(1−.20)(1−.20)(1−.15) = x0.435, x (1−.28 set) = x0.313
   →  x1.00 skills 16%,  x1.50 skills 24%,  x0.50 skills 8%
```

**That is 15-25% almost exactly.** ⚠ It reaches the CON side too: the summed 75% becomes ×0.315,
which is a real loosening for tanks and the moment your authored 43→65% CON column wants a second
look. That is why I have not just done it.

**(b) Cut further under the current summing** — the four SPT sources need to total ~50%, so the
Mark's 15% comes out or the passive halves as well. More numbers moved, same brittle rule.

### 🔵 Unchanged and still open

- **The flat `CcResist` gear cliff**: 0% common, 0% rare, **28% epic, 40% mythic** — armour-set only,
  identical for every class, and nothing on the attacker's side answers it. A ×0.72 / ×0.60 blanket
  on top of everything above.
- **There is no attacker-side land channel in the engine at all** — which is why your mage SPT
  passive is the missing half of the mechanic, not one more buff. Still needs two rulings from you:
  **ladder or flat ×2** across 40/76/80, and **PvP-only or everywhere**.


---

## Superseded text — `BL-218`, second version of 2026-09-13

Kept verbatim per rule 2. Replaced once the 35% CON ruling landed and both schools reached his band.

## `BL-218` 🔵 DEBUFF LAND RATES — magical is in your band; the CON column is now the outlier

**2026-09-13, rewritten again** — your 20% ruling and your compounding ruling (`BL-225`) are both
built, and what is left of this entry is different from what it was this morning. The old text is in
the archive. 📐 The whole table: [balance/DebuffLandRate.md](balance/DebuffLandRate.md).

### ✅ Where it landed

An ordinary `×1.00` magical debuff against a fully-buffed level-90 now lands **20-22%** with the
Harmony Mark (which you expect most people to wear) or **17-19%** with a Holy Mark. That is inside
your *"15-25% which is good"*. The `×1.50` skills sit at 30-33%, which reads right — they are the ones
you priced to be reliable.

### 🔴 THE ONE THING LEFT: the two schools are now TWICE as far apart

You ruled on SPT only, so **Feral Protection's CON column (43→65%) is untouched** and is now the
biggest resistance in the game. Compounded with Strong Body and a Mark it is **68%**, against the SPT
side's 49-56%:

| ×1.00 skill lands | |
|---|---|
| magical (SPT) | **20-22%** |
| physical (CON) | **10-13%** |

So the **tank's entire kit** — Grapple, Stay!, Shield Shock, Numbing Shock — and the Venomweaver's and
the Trapper's all sit at about half the mage's reliability, and the stun that ends a fight is at
**5-6%**.

🔵 **The lever is one number and it is yours: Feral Protection's CON column.** Its top rung at ~25%
instead of 65% would put both schools on the same footing; anything between moves it proportionally.
Not touched — your message was about SPT throughout and that column is your authored CSV.

### 🔵 Also still open

- **The flat `CcResist` gear cliff** — 0% common, 0% rare, **28% epic, 40% mythic**. Armour-set only,
  identical for every class, nothing on the attacker's side answers it. Now that the school stack
  compounds, **this is the largest single term left** in the whole product.
- **There is no attacker-side land channel in the engine at all** — your mage SPT passive would be the
  missing half of the mechanic. It still needs **ladder or flat ×2** across 40/76/80 and **PvP-only or
  everywhere** if you want it — but with the magical side now at 20% it may simply not be needed, and
  the CON column above is the better-targeted fix.


---

## `BL-226` ✅ BUILT 0.140.0 (2026-09-13) — the CON resistance comes down to 35%

*"OK make it 35% con resistance on the fortitude at max rung and I'll test it"* — after `BL-225` made
resistances compound, 65% here was the biggest single resistance in the game and left the tank's own
control kit landing at half the mage's rate.

🔑 **IT COULD NOT BE JUST THE MAX RUNG.** Fortitude's rung 4 was already **40%**, above your new
ceiling, so "change the top rung" would have produced 15 / 20 / 30 / 40 / … / 35 — a ladder that goes
DOWNWARDS, and every ladder here is monotonic. The twelve rungs are re-spread between the two numbers
that are yours: rung 1 stays **15%** (your authored first rung, the half of the 30-vs-15 gap you set
at level 40) and rung 12 is your new **35%**. Your old 20/30/40 at rungs 2-4 could not survive the new
ceiling.

⚠ **AND IT REACHED THREE MORE FAMILIES, because the checker caught what the first pass broke:**
- **Arcane and Feral Protection's CON column** (the GROUP over Fortitude) mirrors rungs 5-12 and moved
  with it — 43→65% became 23→35%. A group may never be weaker than a single it covers.
- 🔴 **CLARITY had to come down too, 50% → 20%**, and this was a real defect I introduced in 0.138.0
  and did not catch: your 20% SPT ruling moved the GROUP to 20% while the SINGLE it covers still gave
  50%. Clarity tops out at level 72 and the group takes over at 74, so **a character's SPT resistance
  would have DROPPED from 50% to 20% on levelling up**, and a party with no buffer would have been
  harder to debuff than one with a buffer. Re-spread to 11/14/17/20.
- The group's rung-1 blurb and the `cleric 2nd` Clarity row followed.

Seven CSV files moved with the code: `healer 3rd`, `healer 4th`, `buffer 3rd`, `buffer 4th`,
`cleric 2nd`. `SkillCsvSeed --check` is back to its two pre-existing Sundering Blow lines (`BL-202`).

### The result — a `×1.00` debuff against a fully-buffed level 90

| | before | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-23%** |
| physical (CON) | 8-10% | **19-24%** |

Both inside your band, and the two schools are within a couple of points of each other for the first
time.

---

## `BL-227` ✅ BUILT 0.141.0 (2026-09-13) — magic resistance also resists magic debuffs

*"OK I like the idea mresist to decrease the chance ..it look not so much op (it takes of tank/nage
~5% and 2% for nullblade) and we espect nullblade with magical armor to resist more."*

`mRes` now buys two things off one number: less magic **damage** (it is the divisor in
`MagicDefCoef`) and fewer magic **debuffs** landing. It is a plain `(1 − r)` factor on the magical
side, like every other source since `BL-225` — your *"endLandRate x 0.3(30% mresist)"* is linear, not
a second divisor. One place: `GameLoopService.SchoolCcRetain`, so all three roll sites inherit it.

⚠ **It includes PASSIVE mRes, deliberately.** I flagged that this makes the MAGE — whose `anti_magic`
ladder is the largest passive mRes in the game at 35% — the hardest of the three to land a magic
debuff on, which reads backwards. You looked at that row and took it. It is written into the code
comment so nobody quietly narrows it to buffs-only later.

⚠ A NEGATIVE mRes (a "Magic WEAK" creature, −20%) correctly becomes a ×1.20 factor and makes control
land MORE often — the same behaviour negative `CcResistMagical` already had.

### A `×1.00` magic debuff, fully buffed, level 90

| defender | mRes | without mRes | **with mRes** |
|---|---|---|---|
| Magus (mage) | 35% | 19.8% | **12.9%** |
| Bulwark (tank) | 21% | 22.8% | **17.9%** |
| Nullblade | 10% | 22.4% | **20.2%** |
| Nullblade **+ Magical Armor** (10s) | 60% | 22.4% | **9.0%** |

The Nullblade's ultimate is now a real ten-second control window as well as a damage one, which is
the thing you wanted from it.


## `BL-235` ✅ CLOSED 2026-09-14 — THE SINGLE-TARGET TWINS, and the double price that stopped existing

**Answered the same day it was raised, by moving the skills to another class:** *"No no ... Those
single buffs to be given to healers not buffers .... And mp should be decreased to the sum of buffs it
gives .. Healer 3rd (elf) learns "Arcane Insight" @70 and it costs 200(120+80)Mp"*, and then: *"That
way bl235 is not needed to decide."* Correct — the twins are the **Lightbringer's** now (0.145.0), one
rung each, priced at Σ(children). The 76-90 ladder stays the Warchanter's alone, so nothing is bought
twice and there was nothing left to rule on. The entry as it was raised is kept below.

### The original entry (0.144.0)

✅ **BUILT (0.144.0)** — nine single-target twins of the Warchanter's group buffs, three per race,
exactly as you specified: same name, same MP, same SP, same payload, `party/single` instead of
`party/aoe`, sharing the group's buff key so the two replace each other the way Great Might and War
Might already do. Elf gets the magic three (Arcane Serenity 70, Arcane Insight 72, Soul Reinforcement
74), Human the defensive three (Body Reinforcement 72, Shield Reinforcement 74, Arcane and Feral
Protection 74), Demon the attack three (Wind Grace 56, Feral Precision 58, Feral Bloodlust 74).

❓ **THE ONE THING I DECIDED FOR YOU: the 76-90 ladder is now bought twice.** Two of the nine groups
ladder into the 4th tier — **Soul Reinforcement** (Elf) and **Arcane and Feral Protection** (Human) —
and their twins have to ladder with them, level for level. They have no choice about that: the pair
share one buff key and compete at `GroupRank(level)`, so a twin left behind at rung 1 would stop being
able to replace the party version the moment the party version reached rung 2, and would go on handing
out a level-74 blessing at 90.

But laddering them means the twin's eight rungs charge **the group's own SP and gold a second time** —
`6.5kk` + `16kk` SP and `1kk … 100kk` gold, for a casting shape of something you already own. I did
**not** invent a discount, because a price is yours to set. Three ways to go, tell me which:

1. **Leave it** — a twin is a separate skill and separate skills cost. (What is built.)
2. **Free above rung 1** — you buy the ability once on the party version and the twin tracks it. One
   line in `ClassSkillTables.Fourth.cs` and eight zeroed cells in `buffer 4th.csv`.
3. **A fraction** — name it (10%? 25%?) and both sides move together.

⚠ The other seven twins are a single rung each and are not affected either way.

## `BL-236` ✅ CLOSED 2026-09-14 (0.145.1) — the healer's two 4th-tier bundles now cost the sum of their parts

**Answered by a CSV edit, nothing owed.** You fixed both MP columns in `healer 4th.csv` to Σ children,
and the code follows: Soul Reinforcement **330 → 400** (Ward 80 + Soul 120 + Mana Blessing 130 → 200),
Arcane and Feral Protection **215 → 285** (Clarity 85 + Fortitude 130 → 200).

The question as it was asked:

> ❓ **Neither MP column matches the rule you set in 0.145.0**, *"mp should be decreased to the sum of
> buffs it gives"*, when I apply it to your own 4th-tier single rows:
>
> | Bundle | 76 in your file | Σ children at 76 | Gap |
> |---|---|---|---|
> | Soul Reinforcement | **335** → 405 | Ward 80 + Soul 120 + Mana Blessing **130** = **330** → 400 | +5 every rung |
> | Arcane and Feral Protection | **290** → 360 | Clarity 85 + Fortitude **130** = **215** → 285 | **+75** every rung |

---

## `BL-237` — BUILT 2026-09-16 in 0.146.0. The pre-build entry, verbatim.

## `BL-237` 🔴 WARRIOR 3rd + 4th CSVs — reviewed, your fixes in, READY TO BUILD

**2026-09-14.** You landed `warrior 3rd.csv` (Ravager race kits, Charge, the three Presences),
`warrior 4th.csv` (76-90) and small `war_aoe 3rd.csv` edits (Final Stand acc, Antidote, Charge). I read
them end to end and listed 8 slips + 8 questions; **you fixed all 8 slips and answered the questions the
same day.** Nothing is built yet.

### Your rulings (built as written when this lands)
- **Charge**: 400 (3rd) / 600 (4th) is its RANGE; usable with a 2h sword **or** blunt.
- **Every Slash debuff lasts 15s.** Demon Slash is now cast 1 / reuse 3 like the other two.
- **All three Slashes land at ×0.7; Sword Shock at ×1.** → these go into `debuff_landmods.csv` **at
  build time**: that file is regenerated from the code, so a row for an unbuilt skill would be wiped.
  The `Chance x0.7` / `Success rate x1` comments come out of the class CSV in the same commit.
- **The 74-rung MP was a real re-price, not a typo**: at 350 MP your 4th Triple Slash was unusable on a
  900-MP warrior, so both 4th Slash skills were cut and the 3rd rungs now match (Double 88, Triple 98).
- **Focus Force** is IG's normal "power attack" as a physical skill that can double; we have no skill
  crits, so it carries +500 power and gathers Focus.
- **Saints Sword Dance** hits an area: `target/aoe`.
- **Two-Hand Mastery 4th**: 666 → **678** → 690 (+12 on both steps, so 80 onward is unchanged).

### Second round of rulings (same day) — applied to the CSVs
- Saints Sword Dance is `target/aoe` in **both** tiers.
- Demon Slash (3rd) is `Physical/Debuf`, like the other two.
- 🔑 **LAW: every melee physical attack skill has RANGE 40**, the melee basic-attack range — *"if I miss
  to type it it's a law"*. The 4th Slash rows were 0 → 40. Every class CSV was swept: no other melee
  strike breaks it (Signal Flare, Prowl, Vanish and Mass Taunt are self/area skills, not strikes).
- Focus Limit is `self/single` — it affects the caster only.
- 4th Focused Double Slash is cast **1.5 / reuse 3** (left over from copying the Triple).
- Still cosmetic, untouched: Sword Blast AOE `00`, Armor Mastery 4th 146 at 87, stale separator labels.

Not a question: the 4th file stops Final Stand, HP Boost, HP Regeneration, the Battle stances, Monster
Knowledge, Focus Mastery, Battle Frenzy and Antidote at their 74 rungs. The Final Stand acc edit changes a
built skill, so both 3rd files owe the code that change too.

## `BL-238` — BUILT 2026-09-16 in 0.149.0. The pre-build entry, verbatim.

He answered all three of its questions, two of them by EDITING FILES rather than writing a sentence:
**F2** (`*"F1 Is Declined"*`, on the page), **reading B at −10%** (he retitled §3 of
`docs/balance/MoveSpeedOrderings.md` and put *"Decrease movement speed with 10%"* on every Mark row
of `healer 4th.csv`), and **yes, the Harmony Mark takes it too** (both `buffer 4th.csv` rows).
🔴 The `+20% move speed` the Marks used to GRANT is gone with it — no CSV row ever authored it.
⚠ What the entry did NOT settle is the band: the cut does not reserve the top for rogues and no
ordering could. That half is `BL-248`.

## `BL-238` 🔵 EVERY MARK SHOULD COST 20% MOVE SPEED — the table is delivered, the ruling is yours

**2026-09-16.** *"every mark should decrease speed with 20% -> the move speed of chars with all the
buffs is + 69 and make all over 200.. And this values should be reserved for rogues ... I just don't
know the formula we should use - can u make me tables with bot formulas below and : race,
mage/fighter/rogue(with armor passive), base, without mark, formula1 with mark, formula 2 with mark,
+60(sprint)"*

  1. `(base × buffs) × debuffs + flat`
  2. `(base × buffs + flat) × debuffs`

🔑 **THE ASK IS THE TABLE, NOT THE RETUNE.** You said in as many words that you do not know which
ordering you want, so what is owed first is the measurement — both orderings, side by side, on the
rows you listed — and the ruling comes after you read it. It is a `tools/BalanceMatrix` job off real
`Entity` objects with real gear, never a hand-derived table: hand-derived balance numbers have been
wrong here before.

**Why it matters and what the problem actually is.** 250 is the buffed move CAP and the base run
speeds per race+class sit below it. Your complaint is that the full buff shelf adds **+69 flat** and
puts *everyone* over 200 — so the top of the speed band, which is meant to be the rogue's identity,
is bought by anyone who visits an NPC buffer. The 20% Mark cut is your lever; **the ordering decides
whether it is a real cut or almost nothing**, because a +69 FLAT applied AFTER a ×0.8 keeps most of
itself, and applied BEFORE it does not. That is exactly the difference between your two formulas.

⚠ It also touches every other percentage debuff in the game, not just Marks — slows SUM and are
clamped at 90%, and the same ordering question governs them. Whatever you pick becomes the rule.

✅🔑 **ONE OF THE THREE IS RULED — YOU PICKED F2.** You wrote it into the page itself, 2026-09-16:
*"**F1** Is Declined - Owner don't like it!"*. That is the cheaper ruling to build, because **F2 is
already what the engine does** (`ModifiedStat(base) × (1 − SlowFraction)`), so no slow in the game
moves and the Mark cut is the only change. **Still owed: which READING, and the Harmony Mark** — see
the two numbered questions below.

✅ **THE TABLE IS BUILT — [docs/balance/MoveSpeedOrderings.md](balance/MoveSpeedOrderings.md)**,
off `dotnet run --project tools/BalanceMatrix -- --speed` (real level-90 Entities, real epic gear,
the real shelf; the mode checks itself against `Entity.EffectiveSpeed` on every row). Nine rows,
both orderings, with and without the +60 sprint, exactly the columns you listed. **It needed THREE
answers back, not one; you have given the first:**

1. ✅ ~~**F1 or F2?**~~ — **F2**, ruled on the page 2026-09-16. No slow moves; the Mark cut is the
   whole change.
2. 🔴 **WHICH READING?** A Mark **grants +20% move speed today** (`markCore`,
   `Skills.Lightbringer4th.cs`) — Holy, Life and Blood are all speed buffs. So *"decrease speed with
   20%"* can mean **(A)** keep the +20% and cut 20% on top — net ×0.96, worth about **−5 points** —
   or **(B)** the +20% becomes −20%, worth **−35 to −44**. Forty points apart. Not picked for you.
3. 🔴 **Does the buffer's Harmony Mark take the cut?** It carries **no move speed at all** today, so
   the four Marks already disagree by 20% of base — under (A) it becomes the FAST Mark, under (B) they
   finally agree.

🔑 **AND THE FINDING WORTH READING BEFORE YOU RULE: the ordering does not reserve the band.** F1 and
F2 produce the **identical** rogue-minus-mage gap — both scale the same base difference by the same
factors, and the +61 flat shelf is common to all nine rows, so it cancels out of a difference. Every
version of the cut makes the rogue's lead *smaller*. What closed the band is the flat shelf itself
(the mage multiplies his own speed by 1.74, the elf rogue by 1.58 — a flat buff pays the slowest
character the most), and with sprint **every row in the game is at the 250 cap today**. §6 of the
page lists what would actually reserve the top of the band — a percent shelf instead of a flat one is
the shortest road — all unbuilt and unruled.

## `BL-237` §1-§4 — CONFIRMED 2026-09-16. The four readings that were mine, and his answer.

His answer, verbatim: *"BL-237 -> 1,2,3,4 as you desided -> ill do .5 later"*. All four stand as
built in 0.146.0; §5 (the Warlord's damage rows) stays open in the live file under the same id.

### 1. ❓ *"Decrease received HP 60%"* — I read it as HEALING RECEIVED
Battle Frenzy's only downside. I read it as "heals and potions restore 60/70/80% less while it runs",
because that is what pairs with *"can be used when HP is less or equal to 30%"* — you go berserk at a
third of your bar and nobody can top you back up. The other reading available was "you take 60% more
damage", which is a different and far harsher skill. **If you meant the second, it is one field.**

### 2. ❓ Saints Blessing's three numbers — I read two of them as CHANCES
*"Reflect 30% of normal basic attacks, 15% to reflect debuff and 10% to reflect Physical Damage
skill"*. I read the first as a FRACTION of the damage returned every time, and the other two as
CHANCES that the whole thing bounces — following your own Deflection ruling, where you were offered
*"a 100% chance to reflect 15%, or 15% chance to reflect 100%"* and picked the second. The `of` / `to`
in your own sentence is the tell, but it is thin, so it is written down here.

### 3. ❓ Battle Frenzy is EXEMPT from the buff-slot limit
Like the two Battle stances, and for their reason: a buff you may only press below 30% HP is an
emergency, not a slot you plan around. Battle Resilience is NOT exempt and stays that way. If you want
frenzy counted, it is one flag.

### 4. ⚠ TWO CELLS OF YOURS MOVED — both slips, both reversible
- **Sword Shock's DURR cell was 0** in both tiers while its DESCR said *"Stuns for 5s"*. A zero-tick
  stun is not a skill, so the cell is 5 now. (Your Human archer's Magic Arrow — the same idea with a
  bow — has always read 5.)
- **The `Chance x0.7` / `Success rate x1` comments came OUT of `warrior 3rd.csv`** and into
  `debuff_landmods.csv`, which is where landing modifiers live since `BL-232`. The values are yours,
  unchanged: three Slashes ×0.7, Sword Shock ×1.

## `BL-248` ⛔ DECLINED 2026-09-16 — MOVE SPEED IS SETTLED, ALL THREE LEVERS REFUSED

His ruling, verbatim and complete: *"we desided marks to debuff for 10% and no move speed buff in
them .. the rogues have enough sprint to outrun anyone, thats why is BL-249.. a dash potion is a
escape from a situation .. not outruning the fastest classes in game ... (an archer uses both frenzies
so he is fastest.. while duals wond because of the evasion drop but in long run can outrun any other
class) -> so do not do any of the .1,.2,.3 -> we leave speed as is (after the marks update)"*.

🔑 **HE REJECTED THE PREMISE, NOT JUST THE LEVERS.** The entry was written as "the band is not
reserved for rogues, here are three ways to reserve it". His answer is that **the band does not need
reserving** because the rogue's advantage is a SPRINT — a burst he can spend and everyone else cannot
— rather than a standing number, and the burst everyone else could buy (the dash potion) is what
actually broke it. So the fix was `BL-249`, an item reuse, and not a stat channel at all.
⚠ **He also accepts the ordering that falls out of it, explicitly**: the archer runs both frenzies and
is fastest; the dual refuses them for the evasion and loses the sprint race but wins the long one.
That is a designed spread, not a defect — **do not re-raise it as one.**

The pre-decline entry, verbatim:

## `BL-248` 🔵 THE SPEED BAND IS STILL NOT RESERVED FOR ROGUES — and the Mark cut could never have done it

**2026-09-16, the half of `BL-238` that outlived it.** Your complaint was two sentences and only one
of them is now answered. The Mark's price is built (0.149.0, −10% in the F2 position, both files).
This is the other one: *"this values should be reserved for rogues"*.

🔑 **THE CUT CANNOT DO IT, AND NEITHER ORDERING COULD.** Measured, in
[balance/MoveSpeedOrderings.md](balance/MoveSpeedOrderings.md) §5: F1 and F2 produce the **identical**
rogue-minus-mage gap, because both scale the same base difference by the same factor and the flat
shelf is common to every row, so it cancels out of a difference. Every version of the cut makes the
rogue's lead **smaller**. Marked and unsprinted the rogue leads the mage of his race by **16.6**
(Human), **37.8** (Elf) and **10.2** (Demon).

**What actually closed the band is that the shelf is FLAT.** The same +61 is worth proportionally
more to a slow character: the mage multiplies his own speed by 1.74, the elf rogue by 1.58.
🔑 **Your own follow-up table reaches the same place from the other side** — rogues skip Frenzy (it
costs evasion), so their shelf is **+53** against everyone else's **+61/+69**, and on your "full"
ordering the **Demon rogue comes out LAST of the eight**. A band that puts one rogue first and another
last is not a band.

**The levers, measured and unbuilt — pick one and it gets built:**

1. ⛔ **Make the shelf's move speed a PERCENT instead of a flat** — Swift `+33` → `×1.25`, so the band
   scales with base instead of collapsing toward it. It was the only lever that makes the rogue's base
   advantage survive buffing. ❓ **I read your 2026-09-16 note as DECLINING it:** *"buffs (swiftness
   +20/33, harmony of swiftness +20, harmony of speed +20, frenzy +5/8, harmony of madness +8) to be
   as is"* — those five ARE the shelf, and you listed them at their flat values. Verified against the
   code, all five match to the number (`+33`/`+20` Swift rungs, Harmony of Swift `+20`, Frenzy
   `+5`/`+8`, Harmony of Madness `+8`; the `+53`/`+61`/`+69` shelves this entry quotes are those
   sums). **Say if "as is" meant only "don't retune the magnitudes" and the flat→percent change is
   still open** — it is the only one of the three that fixes the cause rather than the symptom.
2. **Cut the flat shelf and give the difference to the rogue's own kit** — the light Armor Mastery
   already carries `speed +7` and is the natural home for more.
3. **Move the CAP per class.** `Entity.MoveSpeedCap` is already per-entity, so a rogue ceiling above
   250 (or everyone else's below it) costs nothing structurally. ⚠ Since 0.149.0 the **elf rogue is
   the only character in the game who reaches 250 on his own**, so this lever is live rather than
   theoretical.

⚠ **One measured slip in your own table, worth a look before you rule:** the **Demon fighter** row
repeats its `shelf` figures in the `full` columns (173/156/210 twice) where every other non-rogue row
gains +8. If that is a paste, his `full` is 181 and the ordering at the bottom of your list changes.


## `BL-249` ✅ BUILT 2026-09-16 in 0.150.0. The pre-build entry, verbatim.

## `BL-249` 🔴 DASH POTIONS GO TO A 90-SECOND REUSE

**2026-09-16.** *"Make dash potions reuse to 90s"*. One number, in one place:
`SkillCatalog.DashPotion` (`Skills.Common.cs`) builds all six rarities with `cooldownTicks: 600`
(60s) — it becomes **900**. The 15-second duration and the six `+15…+60` move-speed rungs are
untouched, so what changes is only how often the burst comes back: from **25% uptime to 16.7%**.

⚠ Two comments carry the old number and go with it: the *"15 seconds of sprint on a 1-minute reuse"*
header above the six `ItemDef`s in `Items.cs`, and anything in `docs/guides/ItemIds.md` that repeats
it. No CSV is involved — potions are not class skills.


## `BL-250` — RE-SPECCED 2026-09-16 (he answered every open question). The first draft, verbatim.

## `BL-250` 🔵 THE SIGILS BECOME SUBCLASS-GATED — your IG "subclass ability" model

**2026-09-16.** A rework of what 0.113-era built, and it changes the GATE, not the eighteen sigils —
every name in your six groups already exists in `Skills.Sigils.cs` under exactly that name.

**Your model, verbatim:** *"In IG when you lvl up a sub class to 75 u get to use its 'Ability' → each
subclass have its ability-identity … because a tank cannot take a tank subclass it cannot get its
ability … so i want sigils not to be separated as attak/support/defence .. u can have up to 3 sigils
active … each subclass @76(4th) activates a sigil slot (up to 3) + unlocks its designated sigils"*.

**What changes**

1. 🔑 **The three SLOTS stop being Attack / Defence / Support.** Today `SigilSlot` is a real
   exclusion axis — one per slot, enforced by `ExclusiveGroup`. It becomes **three identical slots**:
   any three of the eighteen, so long as you have unlocked them. The `SigilSlot` enum stays as a
   *label* for the UI or goes entirely; that is a free choice.
2. 🔑 **Slots are EARNED, not granted at 76.** Each subclass that reaches **76 (its own 4th class)**
   opens one slot, to a maximum of three. So a character with one 4th-class subclass has one sigil,
   and only a fully built-out character has three. Today all three open at once on the main.
3. 🔑 **A group is unlocked by OWNING a subclass of it**, and your main class grants nothing:

   | group | the classes that unlock it | its three sigils |
   |---|---|---|
   | mage | the 3 Apprentice | Frenzy · Mage Defence · Arcane Support |
   | healer | the 3 Priest, healer discipline | Holy Power · Holy Protection · Holy Support |
   | buffer | the 3 Priest, buffer discipline | Soul · Spirit · Immortality |
   | rogue | the 6 Rogue | Focus · Agility · Aim |
   | warrior | the 6 Warrior | Fury · Duel · Fortitude |
   | tank | the 3 Knight | Body · Aegis · Critical Protection |

✅ **The exclusion you want already falls out of the rule we have.** *"mage wont be able to take mage
sigils (cannot take another mage class) … rogues and warriors can take their own because they have a
separate discipline"* — `Player.CanAddDiscipline` already refuses a second class of the same
**discipline**, and the roster does the rest: the nuker's three names are one discipline (so no mage
may add a mage), while the rogue's six are **two** (dagger ↔ bow) and the warrior's six are two
(warrior ↔ war\_aoe), so those two archetypes can add their own. Healer/buffer/tank cannot. **No new
rule is needed for any of it** — and when summoners arrive, nuker ↔ summoner unlocks the mage group
for a mage exactly as you describe, with no code change here either.

**What this costs, so you can judge it before it is built**

- ⚠ **Sigils become END-game, hard.** Three sigils today = 76 + 60kk SP + 30kk gold. Three sigils
  after this = **three subclasses each levelled to 76**, on top of the existing gate that every class
  you own must be 75+ *with* its 3rd class before you may add another. That is a very long road, and
  it is the IG road. Say if you want the SP/gold price cut to compensate, or left as it is.
- ❓ **Does the MAIN class open a slot too?** Your sentence says *"each subclass"*, and IG's ability
  is a subclass ability. Taken literally a character with no subclass has **zero** sigils. Confirm.
- ❓ **What happens to a character who already owns three?** Pre-release, so the answer can simply be
  a `game.db` delete — but if you would rather they be re-granted under the new rule, say so.
- ⚠ The client's Sigils tab is built around the three named slots and would be rebuilt with them.

## `BL-251` ✅ BUILT 2026-09-16 in 0.150.0 — EVASION MASTERY IS REMOVED FROM EVERY ROGUE

Never had a pre-build entry: he specified it and it was built the same hour. His words, verbatim:

> *"Evasion mastery is removed out of any rogue/dual/archer . no1 learns it or auto gets it. same as
> warriors precision ... they have enought passive to acomudate for the evasion/acc difference with the
> same lvl player/mob ... duals with passive, +agi, +set Agi, +buffs gets about 25+ evasion differnese
> with the same lvl mob ... same goes for fighters (demon even more/ war_aoe will get +10 on a toggle
> so they will do without floor boost)"*

**Built exactly as `BL-201` built the warrior's half, four days earlier** — the two are one decision
made twice:

- `SkillCatalog.FloorPassiveFor` no longer names `Archetype.Rogue`. **The tank's `anti_magic` is now
  the only floor left in the game.**
- 🔑 **The SKILL and the MECHANIC both stay.** `evade_mastery` is still a `SkillDef` and
  `PassiveEffect.EvadeFloor` is still read by the resolver — only the grant is gone, so re-granting it
  is one line. Same treatment `precision` got, for the same reason.
- 🔑 **Nothing UN-grants it**, deliberately, under his standing pre-release rule: *"no point of
  migration type to remove a skill from some1. They will never have it in the 1st place."* A `game.db`
  delete is the migration.
- ⚠ `Disciplines.IsRanged` now has **no caller** — its one job was capping a bow rogue at rung 1. Kept
  (it is a roster fact, not a detail of that rule) and marked as uncalled.
- `docs/design/CombatResolution.md`'s floor table was **stale on three of its four rows** and was
  rewritten with this: it still listed Reflexes (deleted 2026-08-07) and Precision (gone in `BL-201`).
- `tools/BalanceMatrix`'s `GrantFloorPassive` is now a no-op for everyone but the tank, and says so —
  a rogue measured there has no evade floor, which is the game. Do not "fix" that by re-granting it.

🔴 **MEASURED AFTERWARDS — it is NOT a no-op, and the entry says so because the rig does.** A melee
rogue's natural evasion spread against a same-level mob measures **14 points at level 44 (19% dodge)**
and **11 at 52 (16%)** — both under the 20% the floor was pinning — so at those levels he loses 1-4
points of dodge. His case rests on gear `BalanceMatrix` does not dress in that section (the AGI set
and the full buff shelf, *"about 25+ evasion differnese with the same lvl mob"*), which is unmeasured.
The note is printed in the rig's §E1 so it cannot be lost.

✅ **It also closes a design gap the rig had been flagging.** §E1b used to read *"against a ROGUE, +5
accuracy buys NOTHING at any gap under 10 … accuracy is currently a stat that does nothing against the
one target class it is meant to counter"*, because the floor was a hard lower bound on miss. With both
floors gone every accuracy point is worth a full point from the first one — which is what makes the
warrior's `+9` from `BL-201` do anything at all. The two rulings complete each other.

## `BL-247` ✅ BUILT 2026-09-16 in 0.151.0 — the 66-79 hole is filled with four elite camps and a boss, and the blueprint roll takes the rate knobs. The pre-build entry, verbatim.

## `BL-247` 🔴 A-GRADE SCROLLS AND BLUEPRINTS NEED A SOURCE — the sweep is done, and the cause is ONE HOLE

**Asked 2026-09-16** — *"what drops A grade enchant scrolls ? Also where epic/rare wood and epic
leather are dropped -> also got none"* — then, the same day, a requirement: *"mobs 76-80 should drop A
scrolls, elits 80-85 should drop A grade blueprints ... 1st answer me if anything drops those things"*.
**Below is that answer. The build is yours to approve, because the honest fix is not the one you
asked for.**

### 🔴 THE FINDING: THERE IS NO ELITE OR BOSS ANYWHERE BETWEEN LEVEL 66 AND 79
Every scroll, top-material and blueprint faucet in the game is gated on `MobRank` being Elite or Boss
(`EnchantScrollDrops`, `EliteMatDrops`, `RollBossBonus`) — and **rank is a property of the SPAWN, not
the template**, so the only thing that creates one is a zone. Here is every Elite/Boss spawn that
exists today:

| source | rank | level |
|---|---|---|
| Hollow Crypt rooms | Elite | 39-42 |
| Hollow Crypt boss (`grave_lich`) | Boss | 44 |
| Sunless Warrens rooms | Elite | 58-64 |
| Valley field boss (`valley_treant`) | Boss | 60 |
| Sunless Warrens boss (`dread_knight`) | Boss | 65 |
| 🔴 **— nothing at all —** | | **66 → 79** |
| Frostmere elite camp | Elite | 80 |
| Ashen Sepulchre rooms | Elite | 80-85 |
| Radiant Expanse elite camp | Elite | 84 |
| Dawnbreak Summit elite camp | Elite | 90 |
| Ashen Sepulchre boss (`disciple_of_the_dawn`) | Boss | 90 |

🔑 **The A band is levels 76-79** (`Items.EnchantScrollBands`: A opens at 76, S at 80) — which lands
exactly in the hole. That single gap is the whole explanation for "I got none".

### 1. A-GRADE ENCHANT SCROLLS — one of the three is reachable, two are not

| item | what pays it | reachable today? |
|---|---|---|
| `scroll_enchant_a` (Normal) | an Elite in its own band (0.030 → **9%**); a Boss in its own band (0.100 → **30%**); a Boss ONE band above, i.e. 80+ (0.100 → **30%**) | ✅ **but from ONE mob in the game** — `disciple_of_the_dawn`, the L90 dungeon boss. Also craftable (Scribe rung 5, `craft_scroll_enchant_a`) |
| `scroll_greater_a` (Greater) | a Boss whose OWN band is A → **level 76-79** (0.030 → 9%) | 🔴 **NO. Unreachable.** No boss exists in 76-79, and Greater became boss-only earlier today (§100) |
| `scroll_safe_a` (Safe) | same — a Boss at 76-79 (0.0015 → 0.45%) | 🔴 **NO. Unreachable.** |

⚠ Neither Greater nor Safe is craftable at any rung, by design — *"they are the elite/boss reward"*.
So two of the three A scrolls currently have **no source of any kind**.

**Your ask was "mobs 76-80 should drop A scrolls".** ⚠ Taken literally that means ORDINARY mobs, and
`EnchantScrollDrops` pays a Normal-rank kill nothing at any grade — so it would be a new rule for the
A band alone. ❓ **I think what you actually want is an elite camp (and a boss) in the 76-79 hole**,
which pays all three A scrolls at the rates already authored, needs no new rule, and fixes the
materials below at the same time. **Say which** — camp, or normal mobs.

### 2. A-GRADE BLUEPRINTS — they already drop, at 1 in 1000
They exist (`recipe_craft_*`, `ItemGrade.A`, Epic rarity — one per Mythic level-76+ gear piece) and
`RollBossBonus` already pays them **exactly where you asked**: any **Elite at level ≥76**, which is the
80/84/90 camps and the whole Ashen Sepulchre. So the answer to *"does anything drop those"* is **yes**.

| rank | chance per kill |
|---|---|
| Elite ≥76 | **0.001 — one roll across all ten slot families** |
| Boss ≥76 | 0.50 armor · 0.40 weapon · 0.60 jewel |

🔑 **It is not a missing source, it is a rate.** 0.1% is a thousand elite kills for one book, and it is
a raw `_rng` roll — **no drop-group multiplier touches it**, so your ×100 test rate never applied to it
either. That is why you have none. ❓ **Give me the number you want and it is one line.** For scale,
0.02 would be ~1 book per 50 elite kills.

### 3. EPIC / RARE WOOD AND EPIC LEATHER — the original question

| material | source | reachable? |
|---|---|---|
| **Rare Wood** | 🔴 **nothing, anywhere.** Wood is only ever the *secondary* material of a category, and `StandardDrops` stops a secondary at Uncommon; `EliteMatDrops` has **no Rare rung at all** — it jumps Uncommon → Epic | ❌ craft only (PotionMaster refines 5 Uncommon Wood + Ingot + Thread) |
| **Epic Wood** | `EliteMatDrops` Elite/Boss at level **52-79** | ✅ Sunless Warrens (elites 58-64, boss 65) and `valley_treant`. 🔴 **Nothing at 80+ pays Epic** — the S band drops it for Legendary/Mythic |
| **Epic Leather** | same rung, same bands | ✅ same two places |

⚠ `StandardDrops` also has an Epic rung for a category's PRIMARY material at level ≥76 — and for
Leather that means an Animal or Plant mob at 76+. **There isn't one**: the highest Animal/Plant
template in the roster is `dire_beast` at 70. So that rung is dead code in practice.

### 🔑 MY RECOMMENDATION, IN ONE LINE
**Fill the 66-79 hole with an elite camp (or two) and a boss around 78.** It is one `WorldPlan` entry
each, it pays all three A scrolls, Epic Wood and Epic Leather at rates that are already authored and
already balanced, and it removes the reason three separate items are unobtainable — instead of three
separate special cases. The blueprint RATE is a genuinely separate question and wants a number from you.

## `BL-250` — RE-SPECCED AGAIN 2026-09-16 (the subclass TICKET economy, the NPC, the reset rule). The second draft, verbatim.

## `BL-250` 🔴 THE SIGILS BECOME SUBCLASS-GATED — fully answered, ready to build

**Re-specced 2026-09-16 with your answers; the first draft is in the archive.** It changes the GATE
and the PRICE, not the eighteen sigils — every name in your six groups already exists in
`Skills.Sigils.cs` under exactly that name.

**Your model, verbatim:** *"In IG when you lvl up a sub class to 75 u get to use its 'Ability' → each
subclass have its ability-identity … because a tank cannot take a tank subclass it cannot get its
ability … so i want sigils not to be separated as attak/support/defence .. u can have up to 3 sigils
active … each subclass activates a sigil slot (up to 3) + unlocks its designated sigils"*.

### 1. 🔑 THE THREE SLOTS STOP BEING Attack / Defence / Support
Today `SigilSlot` is a real exclusion axis — one per slot, enforced by `ExclusiveGroup`. It becomes
**three identical slots**: any three of the eighteen, so long as you have unlocked them. The
`SigilSlot` enum stays as a *label* for the UI or goes entirely; that is a free choice.

### 2. 🔑 THE GATE IS A SUBCLASS AT **75**, NOT 76
*"lets make them once sub becomes 75 u are able to get the tree + sigil slot -> 3rd class
(automatically gotten when taken subclass) … main class dont open slot; only subs will .. the 1st
three subs are required to open the 3 slot -> then every other just opens their tree (if not
opened)"*.

| what | rule |
|---|---|
| **level gate** | that SUBCLASS is **75** (was 76/4th in the first draft — this is your change) |
| **class gate** | it holds its **3rd class**, which it gets automatically when the subclass is created (see `BL-252`) |
| **slots** | subclass #1 → slot 1, #2 → slot 2, #3 → slot 3. **Three is the ceiling** |
| **subs 4, 5, 6** | open **only their tree**, never a fourth slot |
| **main class** | opens **nothing** — no slot, and no tree of its own group |

⚠ **This needs `GameConstants.MaxSubclasses` raised.** It is **4** today (main + 3 subs) and your
model needs **7** (main + 6). Nothing else about the constant's job changes.

### 3. 🔑 A GROUP IS UNLOCKED BY OWNING A SUBCLASS OF IT

| group | the classes that unlock it | its three sigils |
|---|---|---|
| mage | the 3 Apprentice | Frenzy · Mage Defence · Arcane Support |
| healer | the 3 Priest, healer discipline | Holy Power · Holy Protection · Holy Support |
| buffer | the 3 Priest, buffer discipline | Soul · Spirit · Immortality |
| rogue | the 6 Rogue | Focus · Agility · Aim |
| warrior | the 6 Warrior | Fury · Duel · Fortitude |
| tank | the 3 Knight | Body · Aegis · Critical Protection |

**Your own count of who ends up with what, verbatim:** *"as warrior/rogue u can have 6 subs and all
trees and change them as u like for the price of 100kk, tank,buffer,healer will get up to 5 trees
without their own, mage for now 5 trees"*. ✅ That falls straight out of the rule with nothing added:
`Player.CanAddDiscipline` already refuses a second class of the same **discipline**, and the roster
does the rest — the nuker's three names are ONE discipline (so no mage may add a mage), while the
rogue's six are **two** (dagger ↔ bow) and the warrior's six are **two** (warrior ↔ war_aoe), so
those two archetypes reach their own group and the other four do not. When summoners arrive, nuker ↔
summoner unlocks the mage group for a mage — **no code change here either**.

### 4. 🔑 THE PRICE MOVES ENTIRELY ONTO CLEARING
*"we can remove their sp/gold cost -> they are their own system. only clearing will cost 100kk (its
10kk now i think + losing the 60kk sp and 30kk gold)"*.

| | today | after |
|---|---|---|
| commit one sigil | 20kk SP + 10kk gold (`SigilSpCost` / `SigilGoldCost`) | **free** |
| commit all three | 60kk SP + 30kk gold | **free** |
| clear one | 10kk gold, no refund (`SigilResetGold`) | — |
| **clear** | — | **100kk gold** |

❓ **One reading is mine: `SigilResetGold` becomes 100kk and stays PER SIGIL.** Your sentence prices
"clearing" against the old total of *"10kk + losing the 60kk sp and 30kk gold"*, which is the cost of
one sigil, so per-sigil is the like-for-like comparison and it keeps the existing per-skill Forget
button at the Mindwright doing exactly what it does now. **If you meant 100kk to wipe all three at
once, it is one line.**

✅ **And re-taking a class is NOT a separate charge** — *"if u have tank,war,rogue and u decide to
remove them and want to add a mage it will cost you the removal price + adding mage class to 75"*.
That is the 100kk plus the ordinary cost of levelling a new subclass to 75; nothing new to build.

### 5. ⚠ What it costs you, for the record
Three sigils today = level 76 + 60kk SP + 30kk gold on ONE character. Three sigils after this =
**three subclasses each levelled to 75**, on top of the existing rule that every class you own must
be 75+ with its 3rd class before you may add another. The money is gone and the ROAD is the price —
which is what *"yes sigils become end game and hard"* asks for.

✅ **Characters who already own three: nothing is built.** *"development -> db is reset periodically -
no need for migrations"* — the standing pre-release rule.

⚠ The client's Sigils tab is built around the three named slots and gets rebuilt with them, so this
ships with an APK.


## `BL-255` ✅ BUILT 2026-09-16 in 0.151.1 — the duplicate-class rule compares the PATH, not the discipline. Your correction, and the entry as filed.

**Your correction, 2026-09-16:** *"How a nuker can hold 11? Buffer, healer, duals, Archer, warrior,
war aoe, Tank .. thats 7 .. Not 11"*. You were right and the code was not.

**What it was doing.** `Player.CanAddDiscipline` barred a repeated `Discipline` value. But the archer
merge made the rogue's split **per race** — dagger is three discipline values (Nullblade · Venomweaver
· Phantom) and bow is three (Sharpshooter · Hunter · Trapper) — and a subclass may be **any race**. So
one character could hold all three daggers as three separate classes, each of them legal, each of them
the same class with a different name on it.

**What it does now.** The comparison is `Disciplines.PathOf` — the parent archetype plus which BRANCH
of its pair the discipline is. Measured with `dotnet run --project tools/BalanceMatrix -- --paths`:

| path | branch | disciplines folded onto it |
|---|---|---|
| Tank | 0 | Bulwark |
| Warrior | 0 · 1 | Ravager · Warlord |
| Rogue | 0 · 1 | **Phantom · Venomweaver · Nullblade** · **Sharpshooter · Trapper · Hunter** |
| Healer | 0 · 1 | Lightbringer · Warchanter |
| Nuker | 0 | Magus |

**Twelve live disciplines fold into EIGHT paths**, so one character may own 8 classes — its main plus
**7 subclasses**, exactly your list.

⚠ **It cannot just ask `IsRanged`** — that was the obvious shortcut and it is wrong for the WARRIOR,
whose Ravager and Warlord are both melee and genuinely two paths. The branch INDEX separates them.

⚠ **The branch is DERIVED from `Disciplines.Of`**, the table that already authors each archetype's
pair per race, rather than written out a second time. A new class lands in the right path by being
authored in that pair and nowhere else.

⚠ **Nothing un-does an illegal pair a character already has** — the pre-release rule. In practice
nobody has one: `MaxSubclasses` is 4 and only the admin path can add them, so this was latent until
`BL-250` raises the slot count. It is fixed BEFORE that, not after.


## `BL-256` ✅ BUILT 2026-09-16 in 0.152.0 — the warrior PvP pass: ×2 damage, a real Charge, and two dead channels

**Your report, 2026-09-16, after duelling the three warrior races and taking a champion to 76:** *"Only
human does decent (low but better) dmg than other 2 warriors ... Elf sword dance never crits .. And
have the lowest dmg even when power is combined is equal to the demons .. so a demon and a elf do about
~1k to a human and human does with triple a 2400 (3x800)"*. Six asks. **Two of them were engine bugs,
not numbers**, and both were channels that had been dead since the day they were written.

### 1. ✅ ×2 POWER ON EVERY WARRIOR DAMAGE SKILL EXCEPT THE SLASHES
*"I want the 3 warriors - all dmg skills except slash of warrior to double in power"*.

Sword Shock · Demonic Smash · Sword Blast · Focused Blast · Focused Double Slash · Focused Tripple
Slash, every rung of the 3rd AND the 4th tier. The three Slashes are untouched, as you said. Demonic
Smash is still exactly 3× Sword Shock on all thirty rungs.

### 2. ✅ SAINTS SWORD DANCE ×2.5 — AND IT CAN DOUBLE AT LAST
*"Saints dance x2.5 and to be able to [double]"*. 150 → 750 becomes **375 → 1,875** (rounded to the
nearest 5 so the column stays readable); 780 → 1,200 becomes **1,950 → 3,000** (exact).

🔴 **Its inability to crit was not the dance, it was every area strike in the game.**
`DeliverSimpleHit` — the AREA damage path — was written for mob spells and traps. Its MAGIC arm grew a
fizzle roll and a crit roll on 2026-08-28, when a mage's AoE was first routed through it; **its
physical arm never did**. So every player physical AREA skill landed a flat hit — no crit, no
[Double], no block. The dance is ten `EnemiesInRadius` strokes, so all ten went through there. It now
takes the same three-way resolution as the single-target arm, gated on a PLAYER attacker exactly as
the magic arm is.

### 3. ✅ CHARGE IS A REVERSE PULL, AND IT WORKS NOW
*"charge does noting only use as vusual - no charge no displacement.. Nothing ..it should act as the
pull but reverse (caster goes to target)"*.

🔴 **Why it did nothing:** `BeginSkill`'s `offensive` mask asks about damage, debuffs, Cancel, Taunt,
`Charms`, `Pulls` and `Silence` — never `Blink`. Charge was the ONE skill whose only payload is a
targeted blink, so it self-cast and ran `BlinkAwayFromNearest(caster, max(1, 0))`: a one-unit hop. The
sixth time a payload in a field has had to be taught to that gate.

**What it is now:** `SkillDef.ChargesToTarget`, the drag machinery with its ends swapped —
`StartPull` and `StartCharge` are one method called two ways. The caster crosses the ground, re-aimed
every tick so it lands on a target that is running, un-announced steps so the client interpolates, and
action-locked while it runs. **0.4s**, not the pull's 1.2s: a tow can afford to lock you for over a
second, a leap cannot.

### 4. ✅ FOCUS IS PHYSICAL — AND SO WERE EIGHT OTHER BUFFS ROLLING A MAGE PASSIVE
*"focus must be physical (now it activates my magic proficiency)"*. The magic-cast proc trigger tested
`Category` alone, and `Category` is a ROLE tag (`BL-132`): a physical self-buff is `Category.Buff` and
declares itself with `PhysicalCast`. `SkillMath.IsPhysical` is the one three-marker test and that site
was not calling it — so Focus, the three Presences, the archer's stances and Dance of Fury had all
been rolling Magic Proficiency on every press.

### 5. ✅ FOCUS AND FOCUS FORCE TAKE A 0.5s REUSE
*"focus and focus force to have 0.5 cd... Now I spam it as crazy"*. Five ticks each. Not a reuse to
wait out — a floor against queueing a 0.5s cast on top of itself. Both CD cells moved with it.

### 6. ✅ FOCUS FORCE CARRIES 1,200 POWER
*"focus force to have 1200 power (I'll see how it goes)"*. ⚠ **Hand-priced, NOT part of the ×2 sweep** —
it is 1,200 rather than 1,000, and a later sweep must not double it again.

### ⚠ THE WARLORD WAS LEFT ALONE, AND THERE WAS NOTHING TO LEAVE
`war_aoe 3rd.csv` and `war_aoe 4th.csv` author **no damage skills at all** — Charge is the Warlord's
only active — so "the 3 warriors" and "the whole authored warrior damage kit" are the same set. The day
his damage rows land they are authored at the new scale.


## `BL-257` ✅ BUILT 2026-09-16 in 0.153.0 — PLATINUM, the account currency

**Your spec, 2026-09-16, verbatim:** *"Also make platinum -> copy of gold without the drop -> items Def
on their buy price also must have a platinum value (Default 0) · any item that have a platinum or/and
gold must be bought with the value · platinum is not an item. It cannot be traded (until global
marketplace) · platunum is account value. So any char in the acc shares it · add /giveplat admin
command same as givegold and make the slots tickets buy able with plat need 100/1000/5000"*.

### WHAT WAS BUILT
- **An ACCOUNT balance, not a character one.** `AccountFarmBudget` was already the one per-account
  runtime object (one load at login, one save), so it took the wallet and was renamed **`AccountState`**
  — a second per-account dictionary with a second lifetime rule would have drifted. There is no
  per-character copy, which is the whole of *"any char in the acc shares it"*.
- **Every change pushes to every online character of the account**, and is **flushed to the DB at
  once** rather than riding the 60s autosave. It is money.
- 🔴 **A guard the farm allowance does not need.** The state is created LAZILY for a character that
  never came through the login read. An empty one is the safe answer for a daily allowance; for money
  it is a WIPE — a lazily-created `Platinum = 0` written back deletes a real balance. So the state
  records whether it was really loaded, every platinum path refuses on one that was not, and the save
  passes `null` for the wallet rather than a zero.
- **`ItemDef.PlatinumPrice`, default 0.** Gold alone, platinum alone (`BuyPriceOverride: -1` beside
  it) or BOTH — and both are charged. `ItemCatalog.IsPurchasable` is the one place "is this for sale"
  is asked. The platinum price is authored verbatim: no rarity multiplier, no vendor tax, no
  equipment floor, because those exist to keep a DERIVED gold price sane.
- **It is not an item**, so there is nothing to drop, trade, warehouse, sell or loot — enforced by not
  existing rather than by a check.
- **`/giveplat`**, `/givegold`'s twin down to the k/m/b/t suffixes and the negative amount; it credits
  the ACCOUNT and says so.
- **Client**: the vendor title, shelf rows, affordability dimming, numpad maximum and confirm dialog
  all read both halves through three shared helpers; the bag line and the Stats window show it. All
  hide platinum at zero.

⚠ **`game.db` delete required** (new `Accounts.Platinum` column) and **an APK** (protocol 38).

### WHAT IT DID NOT BUILD
**The subclass slot tickets.** Your *"make the slots tickets buy able with plat need 100/1000/5000"* is
recorded in **`BL-250` §5** as slots 6 · 7 · 8 = **100 / 1,000 / 5,000 platinum**, and it unblocks that
entry's first blocker — but the ticket item, the slot ladder, the persisted slot count, the class
master's dialogue and the info panel are `BL-250`'s own build, and it still waits on two decisions of
yours (§6's swap price and §5's one-rung-too-long ladder).

---

## `BL-252` — BUILT 2026-09-17 in **0.154.0**. The entry as it stood, verbatim.

✅ Both of the readings marked "mine unless you say otherwise" were built as written: the rune is a
real held item, and it is granted **once per subclass CREATED**, never per swap.

## `BL-252` 🔴 A NEW SUBCLASS IS BORN AT 40, WITH A ONE-DAY RUNE

**2026-09-16**, given alongside `BL-250` and gating it — a subclass must be able to reach 75 for a
sigil slot to mean anything. Verbatim: *"i want when you change a sub class u get a sp/xp 100% 1d
rune. U get your lvl to lvl 40 (not lvl 1). skills are not learned (skills are like your lvl 1 char
creation) if a player want his sub class to have sp to learn his skills for up to 40lvl he must spend
on main class SP+Gold for SP bottle or must go farm a bit to lvl up skills. new sub class is born @40,
no learned skills (except auto learned like mage etc.), 0SP, 0% exp, rune for 1d sp/exp 100%"*.

| on creating a subclass | value |
|---|---|
| level | **40**, not 1 |
| exp into that level | **0%** |
| skill points | **0** |
| learned skills | **none** — except what `AutoLearnCoreSkills` grants anyway |
| 3rd class | **granted automatically** (it is a level-40 class change and the character is level 40) |
| a gift | a **1-day 100% SP/XP rune** |

🔑 **THE POINT IS THAT 40 LEVELS OF SP ARE *NOT* GIVEN WITH THE LEVELS.** A new sub stands at 40 with
an empty skill list and no SP to fill it, so you either buy SP bottles with your MAIN class's SP and
gold, or you farm the bar back up. That is the whole design — the level is a shortcut past the boring
part, the SP is not.

❓ **Two are mine unless you say otherwise:**
- **The rune is a real item in the inventory**, not an invisible timer — so it can be saved for a
  session rather than burning while you walk to a field. The rune layer already exists (War Rune /
  Spell Rune are held items), and an XP/SP rune is the same shape.
- **One rune per subclass CREATED**, not per swap. *"when you change a sub class"* can read either
  way, and per-swap would be farmable: swap out and back every day for a free rune forever.


---

## `BL-239` ✅ BUILT 2026-09-17 in **0.157.0** — the item lock, by DEF id. The entry as filed.

**2026-09-16.** *"we need a lock on items not to show in sell window nor their del/dismantle button to
be active. -> open details window of an item and top there is a button that locks that item. (its lock
for the current inventory -> cannot sell/dismantle/put in keeper/traded/etc ...) you lock item id ->
every item(stacks) of that item is locked -> you lock one stack of potions .. mobs drop more .. u get
new stack its also locked, u can use consumables when locked (lock prevent mistake sells/deletes/etc)"*

🔑 **IT LOCKS THE DEF ID, NOT THE INSTANCE**, and that is the whole design: a stack you lock stays
locked when it is consumed and re-dropped. So it persists as a **set of item ids on the character**,
not as a flag on an inventory row — which also means it survives a relog for free and needs no
migration of existing rows.

**What the lock blocks:** sell, dismantle, delete, warehouse, trade. **What it does NOT block:** USING
a consumable. A locked item should not appear in the sell window at all, and its delete/dismantle
buttons should be inert rather than hidden.

**Built as specced.** One gate (`LockRefuses`) behind all five refusals rather than five copies of the
rule; `LockedItemsCsv` on the character row (⚠ a `game.db` delete); the set rides to the client on
`InventoryUpdate` so it can never be a push behind the bag it describes. `BL-244`'s open clause — *"a
locked item ignores both"* — closed with it: the bag's fast DEL/BRK button is the one control with no
confirmation behind it, so a lock removes it rather than greying it.

## `BL-240` ✅ BUILT 2026-09-17 in **0.158.0** — instant sale by rarity, per tab. The entry as filed.

**2026-09-16.** *"We need a system for instant sell u click on button inside the vendor sell tab and it
shows rarity to instant sell -> it sells everitying of that rarity depending on the tab you are on.. If
I'm on the 'gear' tab and click 'instant sale' and chose 'rare' it sells all that are rare gear in my
inventory"*

The TAB scopes it (gear / mats / use) and the rarity picks the rung. ⚠ Reads directly against
`BL-239`: a locked item must be invisible to this, or the button is a foot-gun rather than a
convenience.

**Built to that scope exactly** — the tab and the rarity, not "and below". The rarity list is built
from what you actually hold and each row names the count and the gold; a locked, equipped or
vendor-refused item is never in it. ⚠ One thing found on the way: `ItemCategory` was private to the
Unity client, so the server had no way to mean the same "Gear" the tab does — it now lives in
`Game.Shared` alongside `ItemTag`, for the same reason `ItemTag` does.

## `BL-246` ✅ BUILT 2026-09-17 in **0.160.0** — the character sheet is two tabs, to your layout. The entry as filed.

**2026-09-16.** Your layout, row for row. 🔑 **BASIC shows the LAST class only** — *"Class: Shadowblade
(Directly Shadowblade, not ElfRogue,Descipiline etc ... just last class)"* — and DETAILS shows the full
chain. That distinction is the point of the split.

**1. BASIC**

| group | rows |
|---|---|
| Class | `Race: elf` · `Level: 85` — `Class: Shadowblade` |
| Primary | `ATK` `CON` `SPT` — `WIT` `AGI` |
| Basic | `HP` `MP` — `P.Atk` `M.Atk` — `P.Def` `M.Def` — `Atk Speed: 1/1500` `Cast Speed: 2/1999` — `Acc` `Eva` `Speed` |
| PVP | `PVP: 0` `PK: 0` — `Karma: 0` |

**2. DETAILS**

| group | rows |
|---|---|
| Class | `Elf Rogue -> Phantom -> Shadowblade` |
| Vitals | `HP/s` `MP/s` — `HP Receive: 0%` `MP Receive: 0%` — `Restore power: x1 + 0` |
| Offence | `Acc` — `Crit: 1%` `Crit dmg: x1.2 +1` — `M.Crit: 1%` `M.Crit dmg: x2` — `x2 Dmg: 0%` `Reuse rst: 0%` — `x2 Duration: 0%` `Stab Rate: 30%` — `Atk.Speed: x4` `Cast.Speed: x0.7` |
| Defence | `Eva` `Speed` `State: Run\|Sit\|Walk` — `M.Fail: 5%` `M.Resist: 20%` — `Crit: 15%` `Crit dmg: 35%` — `M.Crit: 0%` — `Block Rate: 0%` `Block Red: 0%` |

⚠ Several of these rows have **no source today** — `HP Receive` / `MP Receive`, `Restore power`, the
three mastery rates (`x2 Dmg`, `Reuse rst`, `x2 Duration`), `Stab Rate` and the crit-RESIST pair are
all live derived values that the stats payload does not currently carry. So this is a protocol change
as well as a layout, and it wants doing in one pass rather than a row at a time.

**Built to it row for row.** ⚠ The "several rows have no source" count was **four, not seven** —
checked against the code rather than the note: the three masteries, the crit-RESIST pair, `Restore
power` and `HP Receive` were already on the wire and simply had nowhere to be drawn. The four real
ones arrived with protocol 43: `MP Receive` (`RestoreMpMod`), `Stab Rate` (`BlowRate`), the magic
crit RESIST, and `M.Fail` — the last COMPUTED server-side at parity, because a fizzle chance needs
an attacker and "one of my own level" is its only reading on a sheet with no attacker in it.

## `BL-241` ✅ BUILT 2026-09-17 in **0.159.0** — the per-type pickup rarity filter, and it edits the party loot roster. The entry as filed.

**2026-09-16.** *"we need in bag rarity filter for any type gear/mats/use to be able to select min
rarity for pickup.. For 'gear' I make it rare and for 'use' I mkae it unc -> any uncommon/common gear
is ignored and not picked up and any 'use' that is common Is ignored as well; (if in party I'm ignored
in the roster if that rarity is filtered for me)"*

🔑 **THE PARTY CLAUSE IS THE INTERESTING HALF** and it is easy to miss: a filtered player is skipped
in the **loot roster** for that drop, not merely prevented from picking it up himself. So the filter
changes who the party's loot modes hand an item to — it is a loot-rule change, not a UI toggle.

**Built as that.** `Entity.PickupFilters` is character state (persisted, `PickupFiltersCsv`), pushed to
the client with the bag; `GameLoopService.PickupWanted` is the single gate every drop site asks, and
`LootRecipient` now takes the per-item ROSTER and returns a nullable — nobody wanted it, it is left on
the floor. Every loot-mode fallback checks the roster before falling back to the killer. The three
categories are the bag's own tabs (Gear / Use / Mats); Quest cannot be filtered. Protocol 42.

## `BL-242` ✅ BUILT 2026-09-17 in **0.156.0** — the sell list shows the enchant and the attributes. The entry as filed.

**2026-09-16.** *"sale list don't show enchant value and in the description of the sell item row should
show the attributed if any"*. Recorded here as well as in §100 because it is half a defect and half a
UI ask; the defect half is that a +6 and a +0 are indistinguishable in the window where you part with
them.

**Built as asked**, plus one thing found on the way: the sell list was asking `ItemCatalog` about the
DEF where the server's `HandleSell` asks the INSTANCE, so a per-instance price or a per-instance bind
was invisible to the window. Both sides read `ItemTag` now.

## `BL-243` ✅ BUILT 2026-09-17 in **0.156.0** — mana potions per rarity. The entry as filed.

**2026-09-16.** *"make the same as healing pots and for mana pots in the `auto potions` window -> mp
pots to be separated per rarity -> or we can make one potion for hp and one for mp and select from a
drop down which potion to use..."* — two shapes offered; the dropdown is the smaller one and the one
that stops the window growing again the next time a rarity is added.

**Built as the LADDER, not the dropdown**, and the window did not grow: the Potions tab became two
columns (heal left, mana right) in the same 760×520 panel. The ladder was the right half of the choice
because the three mana potions restore 120 / 500 / 3000 — a dropdown picks ONE, and picking one is
exactly the behaviour that was spending a Rare bottle to top up 40 MP.

## `BL-244` ✅ BUILT 2026-09-17 in **0.156.0** — the fast-delete button is a three-state cycle. The entry as filed.

**2026-09-16.** *"the button for fast delete in bag to be a cycle button after fast delete on to be fast
dismantle and the del button to become some dark purple for dismantle. DEL:OFF -> DEL:ON -> BRAKE:ON ->
DEL:OFF ..."* Three states, and the third is **dismantle**, coloured dark purple so the two destructive
modes cannot be confused for one another. ⚠ Reads against `BL-239` — a locked item ignores both.

**Built as asked.** The `BL-239` clause is still owed and will be honoured when the lock lands: the row
filter is one place. In Brake mode a row that cannot be salvaged shows no button rather than a dead one.

## `BL-245` ✅ BUILT 2026-09-17 in **0.156.0** — the crafter sees and SPENDS the keeper's shelf. The entry as filed.

**2026-09-16.** *"crafter should see mats in private wharehouse -> maybe the crafting window can have a
toggle button (on by default) [show keeper items]"*. On by default, so the common case needs no click.
⚠ The question the build has to answer is whether the craft CONSUMES from the warehouse or only
counts it toward the recipe — a window that says you can craft and then refuses is worse than one that
never offered.

**Answered: it CONSUMES.** Bag first, warehouse for the shortfall, on both sides of the wire; the
toggle rides the Craft call so the window and the server can never disagree about which containers were
in play.

## ✅ `BL-258` — THE MAGE HAS NO RACE BLOCK, AND THE GRADE ROWS LIVE IN ONE FILE

**Closed 2026-09-17, 0.163.0 — you answered BOTH halves by authoring `mage 1st.csv` the same day.**

1. **Yes, and with different skills** — exactly as the entry warned it would need to be. Not the
   fighter's six: three race BLESSINGS at level 7 (no fighter equivalent), the Elf's nine-rung
   `elf_self_heal`, the Demon's `demon_over_limit` burst, and the Human's `human_vampiric_bolt`
   ladder, which you moved out of the three nuker files to get there. The concern about an Elf cleric
   buying a worse copy of a spell he owns did not arise: `self_heal` was DELETED as a base-mage skill,
   so the Elf's ladder is the only one there is, and Human/Demon mages simply have none.
2. **Option (a)** — you pasted the seven `grade_penalty` rows into `mage 1st.csv`. The skip is still in
   `Check.cs` (a central row must not read as an unauthored extra on the other nine files) but it now
   names BOTH first-class files as owners rather than excusing the mage side.

The original entry follows verbatim.

## 🔵 `BL-258` — THE MAGE HAS NO RACE BLOCK, AND THE GRADE ROWS LIVE IN ONE FILE

Opened 2026-09-17, the day `fighter 1st.csv`'s race block was built (0.162.0). Two related questions,
both waiting on you, neither blocking anything that shipped.

**1. Does the MYSTIC get a race layer too?** Your instruction was *"fix all fighters"*, and it was
followed exactly: the Elf's cure and self-heal, the Demon's drain and bleed, the Human's parry and
rest stance reach every fighter of that race at every tier, from level 10 to 74. `mage 1st.csv`
authors no race rows, so no mage gets anything — and inventing some would be the one thing the
two-way CSV contract forbids, code the file does not author.

⚠ **It is not a symmetric job, and that is worth knowing before you decide.** Two of the six are
already a mage's day job: the healer has a targeted `antidote` on its own ladder and every mage has
`self_heal` from level 1. So a mystic race block would want DIFFERENT skills, not these six copied
across — otherwise an Elf cleric buys a worse version of a spell he already owns.

**2. The GRADE passive is authored once and learned by everyone.** `grade_penalty`'s seven rows
(1/20/40/52/61/76/80) sit in `fighter 1st.csv`, but grade is a CHARACTER property, so a mage learns
the identical rungs. Today the checker skips it on the mage side rather than pretend the rows are
missing. Two ways to settle it, your call:

- **(a) Paste the seven rows into `mage 1st.csv`** — the file then says what the class actually gets,
  and the skip comes out of `Check.cs`. One paste, one deleted branch. This is the tidier answer.
- **(b) Leave it as is** — one authored copy, verified once against `fighter 1st.csv` with a 1-90
  band, and a commented skip explaining why the mage specs do not see it.

Nothing is broken either way: the passive grants nothing, blocks nothing and is already correct in
the game for every class. This is about where the row LIVES, not about behaviour.

## `BL-237` ✅ BUILT 2026-09-17 in **0.164.0 / 0.165.0 / 0.166.0** — the WARLORD is finished. Both his files are mirrored in the code, `--check` is clean on both, and `war_sundering_blow` (the last derived fighter ladder in the game) is retired. The entry as it stood when it closed:


**Built 2026-09-16 (0.146.0); your four readings CONFIRMED 2026-09-16** — *"1,2,3,4 as you desided ->
ill do .5 later"*. So Battle Frenzy's downside is **healing received**, Saints Blessing's two `to`
numbers are **chances**, Battle Frenzy is **exempt from the buff-slot limit**, and both moved cells
(Sword Shock's 5s stun, the landing modifiers into `debuff_landmods.csv`) stand. Those four are
archived under this id. **Only §5 is still open, and it is yours:**

### 5. 🟡 YOUR ROWS LANDED 2026-09-16 — the 3rd file is BUILT, the 4th is half built
**`war_aoe 3rd.csv` is fully built (0.165.0)**: Shocking Shout, Whirlwind, Taunting Shout, the three
race Shouts, the three Supports and Battle Revival — ten ids that did not exist in the codebase at
all. `war_sundering_blow` is retired from the Warlord in the same commit, and with it the last derived
fighter ladder in the game.

**`war_aoe 4th.csv` is NOT finished.** Its four interlocking charges shipped in 0.164.0; still owed are
Master of Combat, Shocking Javelin, Final Stand rungs 4-5, HP Boost 76-90, the Supports' rung 4, and
the 76-90 continuations of everything above. ⚠ One reading of yours is needed there and is flagged in
0.165.0's CHANGELOG: **that file gives all three races the id `waraoe_life_support` at 80**, while its
own section headers read Life / Blood / Vanguard Support — the 3rd file's three separate ids. It is
being built as rung 4 of each race's own ladder.

What is still open beyond that: **`BL-259`** (three landing modifiers) and **`BL-261`** (Whirlwind's
dipping power column).


## `BL-261` ✅ FIXED BY YOU 2026-09-17, built the same hour in **0.166.1** — *"i fixed wirlwind rungs -> 300 +50/rung"*. The column is 300 / 350 / … / 1000 and runs straight into the 4th file's 1050 with no step at the tier boundary. `--check` is now completely clean on every walked file. The entry as it stood:


`war_aoe 3rd.csv`, built verbatim in 0.165.0. The column **goes down** between two rungs:

```
rung  1    2    3    4    5    6    7     8     9    10    11    12  |  13   14    15
lvl  40   43   46   49   52   55   58    60    62    64    66    68  |  70   72    74
     300  415  525  640  715  825  940  1125  1240  1350  1465  1540 | 900  950  1000
                                                                      ^^^ -41%
```

Your own rule (`BL-237`, Armor Mastery) is that a ladder which **rises** is yours and a ladder that
**dips** is a typo — so this is asked, not assumed. `SkillCsvSeed --check` now prints
🔵 **LADDER DIP** against it and will until the cells move.

### Three independent checks all say rungs 1-12 are the stale half, not 13-15
1. **Rungs 8-12 are the Elf Sword Dance's cells EXACTLY** (1125 / 1240 / 1350 / 1465 / 1540), and
   rungs 1-7 are that same ladder **minus 75**. Two skills in two different files do not agree to the
   unit by coincidence.
2. **`war_aoe 4th.csv` opens at 1050** and climbs +50 a rung to 1750. That continues 900 → 950 → 1000
   perfectly — and is far **below** 1540, so keeping rung 12 would mean levelling 68 → 76 **costs you
   a third of the skill's power**.
3. **+50 a rung backwards from 1000, fifteen rungs, lands on 300** — which is exactly what your rung 1
   already says. **Both ends of your ladder agree with each other; only the middle disagrees.**

❓ **If that reading is right, the fix is one column** — 300 climbing +50 a rung to 1000:
`300 350 400 450 500 550 600 650 700 750 800 850 900 950 1000`. Edit the twelve cells and say so; the
code is one array (`SkillCatalog.WaraoeWhirlwindPower`) and follows in a minute.

⚠ **Whichever way it goes, the SHAPE matters more than the cells.** Twenty strokes a cast means this
column is multiplied by twenty: at rung 12 as authored, one Whirlwind is 30,800 power against Shocking
Shout's 3,400 on the same rung. If 1540 is the intended number, the two are not in the same game.


---

## `BL-254` ✅ ANSWERED AND BUILT 2026-09-17 in **0.167.0** — you took option (c) and made the two categories MIRROR each other: *"make wood drop as lether .. primary/secondary -> animals: leather/wood, plants: wood/leather"*. Rare Wood has five sources now (Valley Treant 60, Bogwood 62), and all five material types are somebody's primary, which closes the entry's wider warning. The entry as it stood:

## `BL-254` ❓ RARE WOOD DROPS FROM NOTHING, ANYWHERE — is craft-only the intent?

**Found while answering `BL-247` (2026-09-16), and NOT fixed by it.** You asked *"where epic/rare wood
… are dropped -> also got none"*. Epic Wood now has thirty sources; **Rare Wood has none, and never
had any.**

The cause is structural, not a missing row:

- Every creature has a PRIMARY and a SECONDARY material type, by category (`StandardDrops`). **Wood is
  only ever a SECONDARY** — Animal/Plant pay Leather+Wood, MagicCreature/Angel pay Gem+Wood, and no
  category anywhere pays Wood as its primary.
- A secondary stops at **Uncommon**. The Rare rung (level 60+) and the Epic rung (76+) are authored
  for the PRIMARY only.
- `EliteMatDrops` — the elite/boss faucet — jumps **Uncommon → Epic**. It has no Rare rung at all.

So Rare Wood's only route is the PotionMaster refining 5 Uncommon Wood + an Ingot + a Thread.

❓ **Three ways out, and it is your call which:** (a) leave it — refining is the intended route and
wood is deliberately the "bought, not found" material; (b) give `EliteMatDrops` a Rare rung, which
pays every type and is one line; (c) give some category Wood as its PRIMARY (the obvious candidate is
Plant, which is Leather+Wood today and is the one category where leather makes no sense at all).

⚠ **Whichever you pick, it is the same question for every SECONDARY material at Rare** — Wood is just
the one you noticed, because it is the only type that is never anybody's primary.

---

## `BL-259` ✅ ANSWERED 2026-09-17, nothing owed — *"leave them as u made them -> playtest will show (if i forget to write modifiers put default ones .. in playtests ill modify them if needed)"*. All four keep the default `x1` they shipped with; the file and the code already agreed. 🔑 It also **narrowed `BL-232`**: an unpriced debuff now ships at the DEFAULT instead of blocking on him — the licence is the default, never an invented number. Recorded in `CLAUDE.md` and in the checker (0.167.0). The entry as it stood:

## `BL-259` ❓ FOUR LANDING MODIFIERS — four cells, and only you may set them

Built 2026-09-17 across 0.164.0-0.166.0 from your two `war_aoe` files. All four are **new debuffs**, so
by your own rule (`BL-232`) I do not price any of them:

> *"Then each new debuff to go there and to ask for modifier edit"*

`docs/data/debuff_landmods.csv` carries all four rows and their `SUCCESS` reads the code default of
**1** — that is the file being filled in, not a decision.

| SKILL | SKILL_ID | DESCR | SAVE | SHAPE | SUCCESS |
|---|---|---|---|---|---|
| Charge n Shock | `warrior_charge_stun` | charge; Stun | CON | `DEBUFF ONLY (1)` | **your cell** |
| Shocking Shout | `waraoe_shock_shout` | Stun | CON | `dmg+1 debuff` | **your cell** |
| Taunting Shout | `waraoe_taunting_shout` | vulnerable to blunt +10%; taunt | CON | `DEBUFF ONLY (1)` | **your cell** |
| Shocking Javelin | `waraoe_shock_javelin` | Stun | CON | `dmg+1 debuff` | **your cell** |

**What each does at once**, since that is the axis your modifier prices:

- **Charge n Shock** closes 800 of ground over two seconds and stuns for two — **no damage at all**, so
  it is a solo debuff by your `SHAPE` test, not a `dmg+1 debuff`. ⚠ For scale, not as a suggestion:
  *Phantom Jump* (blink + Stun) and *Grapple* (pull + Stun) both sit at **1** — but both arrive
  instantly or drag the victim, while this one telegraphs itself for two full seconds first.
- **Shocking Shout** is damage **and** a 5s stun **on a whole ring**, which is the heaviest thing in
  the three. Its neighbour *Acoustic Shock* (`dmg+1 debuff`, single target) is **1**.
- **Taunting Shout** is a taunt plus the blunt vulnerability, thirty seconds, on a 600-800 ring. It is
  the only one of the four whose payload is not control at all.
- **Shocking Javelin** is Shocking Shout thrown 900 away with a tighter ring (150 against 200). Same
  power ladder, same stun — so if the two do not share one modifier, the difference is paying for the
  reach.

✅ **The three race Shouts needed nothing** — you priced them yourself in the cell *"Single debuff
x1.5"*, and the code ships 1.5. That is the `BL-232` rule working: a shout that only curses lands more
readily than a Slash that curses **and** strikes (x0.7).

Edit the cell and `SkillCsvSeed --check` will print DRIFT until the code matches it.

---

## `BL-253` ✅ BUILT 2026-09-17 in **0.168.0** — the drop database, to your spec of the same day: *"a drop db should be build once and only once when server starts … it should remember it every restart until something tuches drops/mobs … on start if its missing its build with drops x1 … each ask of item it looks up and see mobs that drop and show the drop rate for the player (similar to [info->drops] on mobs) … same as server<>apk protocol -> a version that says (rebuild even when u have the mob database) otherwise it only build if missing"*. Both open questions in the entry were answered by that message: it is a PLAYER window, and it shows everything. The walk moved into `Game.Shared/DropIndex.cs` so the tool, the server and the client share one; the recipe roll and the boss mat pile became real tables (`MobCatalog.RecipeRolls` / `BossPile`), which closes the entry's ⚠ about the books drifting. ✅ **He ruled twice more the same day and both are built in 0.171.0**: the CACHE is gone entirely (*"build each restart … that way no drop version needed"* — 9-13 ms to build against 28 ms to read, so the file, the hash and the version stamp all went), and the WINDOW became what he described — a predicting text box, a Type/Rarity/Grade filter tree, and a sortable table. ⚠ One leftover: the boss pile takes no rate knob at all, which is **`BL-262`**. The entry as it stood:

## `BL-253` 🔵 A DROP DATABASE — "I SAY WHAT I AM LOOKING FOR AND IT SHOWS ME WHERE IT DROPS"

**2026-09-16, alongside `BL-247`:** *"we will need a drop database -> i say what im looking for and it
shows me all mob_name/[mob_lvl-elite|boss|normal]/location/drop_rate"*.

✅ **THE MEASURING HALF IS BUILT (0.151.0)** — `dotnet run --project tools/BalanceMatrix -- --drops
"greater scroll"` prints exactly those four columns for anything matching, by item name or id. It is
in `tools/BalanceMatrix/DropFinder.cs`, and it earned itself the day it was written: it caught a new
boss template spawning as ordinary camp filler, and every number in `BL-247`'s report is read off it.

🔑 **The one design fact worth carrying into the in-game version: it must walk SPAWNS, not templates.**
Rank is a property of the spawn, and half the top-end faucets in the game (every Greater/Safe enchant
scroll, every Epic+ material, every recipe book) exist only for an Elite or a Boss kill. A lookup
written against `MobType.Drops` would answer "nothing drops this" — correctly, and uselessly.

🔵 **What is still owed is the IN-GAME window**, which is what you actually asked for. Open questions:

1. ❓ **Where does it live** — a player window (a search box in the Items UI, reachable at any time),
   or an admin `/whatdrops <item>` that prints to chat? The first is a feature; the second is an hour.
   My reading: **player window**, because *"i say what im looking for"* is a play-time question, and a
   drop table nobody can read is why three items sat unobtainable for weeks.
2. ❓ **Does it show what you have not met yet?** A full index tells you a level-78 boss drops the A
   scroll before you have ever seen one. That is either the point of the feature or a spoiler; your
   call.
3. ⚠ **The recipe books are the one row the tool reconstructs rather than reads.** They are not
   `DropEntry`s — `RollBossBonus` rolls them by hand — so the two sides can drift. If the in-game
   version is built, that roll should move into a real drop table first, and then there is one source
   of truth instead of two.

---

## `BL-84` ✅ BUILT 2026-09-18 in **0.174.0** — every skill id reads as its skill

**136 ids renamed, zero collisions, 765 skills before and after.** The convention question this entry
closed with was answered by the DATA rather than by asking. A blanket "id = slugged name" flags 605 of
765 and collides on 71 name groups, because `buff_crit_rate_4` is not the bug he described — it is more
informative than its name, and SIX defs are called "Focus". The scope is the ids named after the
discipline or class that owned the SLOT, which is the shape of all three of his examples and which his
own 44+ kit already avoids (`urgent_heal`, `ultimate_heal`). 82 had a WRONG WORD (his complaint), 54
only needed the prefix off, and 131 KEPT their prefix because it IS the identity (three different
"Armor Mastery" passives). Race prefixes stay where races diverge. Generated and verified by a new
`--skillids` page in `tools/BalanceMatrix`: 82 + 54 before, 0 + 0 after. Constants moved in the same
pass, from reflection over the literals, so `LbElfDawn` is `HealerBlessing`. Five typos and every
surviving `_ork_` id fell out of it.

⚠ **The text below is the entry AS IT STOOD, and its old ids are left alone on purpose** — it quotes
him, and a record that renames the thing it is complaining about says nothing. (The rename sweep did
rewrite them here and in `Playtest-Archive.md`; both were restored.)

  ⏰ This is the reminder you asked for. The trigger you named has fired — `healer 3rd.csv` is built and
  shipped in 0.74.0 — so this is now next in the queue whenever you want it, not a filed idea.
  2026-08-17: *"After the healer is done I want to change all the game skills id's to match the skill
  names ... not `lb_elf_dawn` <> Healer's Blessing, it should be `healers_blessing` or something that
  matches it. Make a note to remind me after the healer is done (I want all the skills, not only the
  healers — all 1st, 2nd + healer 3rd)."*
  **Scope, his**: every skill in the **1st** and **2nd** class tables plus the **healer 3rd** — not the
  healer alone. The other seven disciplines follow when their CSVs land, so the convention has to be
  settled here and then simply obeyed.
  🔑 **Why it is worth doing**: the ids were named after the SLOT a skill sat in, not the skill. Three
  level-40 healer ids now openly contradict the thing they identify — `lb_elf_dawn` is *Healer Blessing*,
  `lb_human_mend` is *Quick Great Heal*, `lb_ork_font` is *Healing Totem* — because each was reused when
  his authored row landed on its slot. That is the right call for data (see `BL-02`) and the wrong one
  for reading code, and it gets worse with every CSV he writes.
  ✅ **NO MIGRATION NEEDED — he settled it the same day**: *"I'll reset the db anyways so it's not of a
  concern."* Ids are persisted (learned skills + the skill bar's `SkillBarCsv`), so a rename would
  normally orphan every character's bar — the failure `retired-skill-ids-leak` recorded once. A DB reset
  removes that entirely, which turns this from a migration into an ordinary rename. **Do it in one pass
  while the reset is happening**, not spread across versions, or the two halves meet in a live DB and the
  problem comes back. ⚠ Ids also appear in `docs/` and in the premium/consumable catalogs, and
  `SkillCsvSeed` matches CSV rows to code **by NAME**, so the checker can neither verify this pass nor
  catch a mistake in it — the compiler is the only safety net, which is fine for constants.
  🔵 Convention to settle with him before starting: strip the `lb_`/discipline prefixes entirely, or keep
  a short one for per-race variants that share a display name across races?

---

## `BL-265` ✅ FOUND AND BUILT 2026-09-18 in **0.179.0** — the AoE discipline had no AoE

**Your ask, 2026-09-18:** *"war_aoe taunting shout should work without a target and affect any target
in range"*.

**Both halves were true, and the second one was true of the whole discipline.** Taunting Shout is
fixed and so are the other six rows it turned up. Recorded here rather than left open because nothing
is owed: it is built, measured, and `--check` is clean on both `war_aoe` files.

### What was wrong — three separate omissions in one row

| | what the row said | what the engine did |
|---|---|---|
| **no `TargetMode`** | `enemy/aoe`, AOE 600 | landed on the **one selected body**. The circle still pulsed at 600 — `BroadcastAreaEffect` reads the radius whether or not anything sweeps it. |
| **`SkillEffect.None`** | *"provoke enemies in a large area"*, and an authored `TauntPower: 3000` | the taunt arm never ran. **The skill did not provoke anything at all**; the 3,000 was dead data. |
| **needs a target** | RANGE 0 — a ring around the caster | the cast-start gate refuses every offensive skill with nothing selected, so a Warlord walking into a pack had to click one of them first. |

### 🔑 The general rule, and why the radius is not it

**`TargetMode.EnemiesInRadius` is the ONLY thing that makes a ring resolve.** `AreaRadius` draws the
circle; the sweep is a separate branch of `ExecuteSkill` and a skill that does not declare the mode
never reaches it. That is the same *"the red circle pulses but nothing is hit"* shape the 2026-08-28
`AreaRadiusAt` fix named, one layer up — and it is silent both times, because the thing the player
reads (the circle) is drawn by the half that works.

### The measurement — seven skills, and all seven are yours

A pass over the whole catalog for *offensive, has a radius, does not sweep*:

| skill | file | radius | what it actually did |
|---|---|---|---|
| `shocking_shout` | `war_aoe 3rd` | 200 | stunned and struck **one** body |
| `waraoe_wirlwind_stroke` | `war_aoe 3rd` | 200 | twenty strokes, all at **one** body |
| `shattering_shout` / `breaking_shout` / `crippling_shout` | `war_aoe 3rd` | 200 | a *"solo debuff on a ring"* on **one** man — i.e. a Slash with the damage removed |
| `taunting_shout` | `war_aoe 3rd`/`4th` | 600/800 | above |
| `shocking_javelin` | `war_aoe 4th` | 150 | thrown 900, detonating on **one** body |
| `warrior_charge_stomp` | `war_aoe 4th` | 200 | the Stomp's arrival hit **one** body — so Charge n Stomp differed from plain Charge by its reuse and nothing else |

**Every other area skill in the game was already correct** — the mage's waves, the archer's barrage
and grenade, the Elf's Sword Dance, both tank mass-taunts, every trap, every boss slam. The defect is
confined to the two Warlord files, which landed on 2026-09-17; nothing older is touched. After the
fix the same pass returns **0**.

### What shipped

1. **All seven declare `TargetMode.EnemiesInRadius`.** Nothing else about them moved — same power,
   same MP, same radius, same rungs. `shocking_javelin` and `warrior_charge_stomp` keep
   `AreaAtTarget`, so those two still need a body to aim at; the other five are caster-centred.
2. **Taunting Shout carries `SkillEffect.Taunt`**, so its authored 3,000 threat is paid to every
   creature in the ring — the same helper the tank's Taunt and Tauting Wall use, so your *"the aggro
   ladder is mob-only, the lock reaches players"* ruling holds here unchanged.
3. **A caster-centred ring no longer needs a target** (`GameLoopService.SelfCentredArea`), joining the
   trap, the totem, the hide and the resurrection field on the self-delivered side of the cast gate.
   It is the general rule, not a special case for one skill: what a ring catches is decided by
   geometry when it lands, so the current selection has no part in it. Nothing is skipped by it —
   `EnemiesInRadius` carries the whole `BL-77` PvP area filter itself and flags per body it reaches.
4. **`SkillDef.TauntLockTicks`** — a new field, 0 = the old reading. One duration cell was meaning two
   different things: Taunting Shout's DURR of 30 is the **blunt vulnerability**, and letting the aim
   lock read it too would have pinned a whole ring for thirty seconds on a twenty-second reuse —
   strictly better than the TANK's own 10-minute Tauting Wall (3s). The provoke is 3s, his Wall's
   number; the rot stays 30s.

⚠ **No CSV moved**, and that is the point: every one of these rows already said `enemy/aoe`. The code
was behind the file, not the other way round.

⚠ **THIS IS A REAL POWER JUMP AND IT WANTS A PLAYTEST.** Whirlwind alone goes from 20 strokes on one
body to 20 strokes on everything within 200. That is what your rows author, and it is the first time
the discipline has actually been the AoE one — but no BalanceMatrix number in `docs/balance/` was
measured against it.

---

## `BL-266` ✅ FOUND AND BUILT 2026-09-18 in **0.179.0** — Relax did not seat you

**Your ask, 2026-09-18:** *"humans relax should prevent me from moving or acting"*.

**It was supposed to, everywhere but in the code that runs.** Three separate places already stated the
rule and none of them enforced it:

| where | what it said |
|---|---|
| `fighter 1st.csv`, all eight rows | *"Sit and relax: Gives 1.0 % HP/s regen **(cannot act, status is canceld on dmg taken)**"* |
| the skill's own description | *"Sit and let the body do its work. **You cannot act**, and any damage ends it."* |
| `GameLoopService`, the regen fold | *"…beside the sitting passives above and for the same reason: **Relax makes you sit**, so charging the sitting bonus on it again would inflate every rung"* |

The toggle applied its regen and left you standing. So the eight rungs — **all authored at 0 MP**,
because the price was meant to be the sitting — cost nothing whatever: **5% of max HP and 3% of max MP
per second, held while running, swinging and casting.** The regen was even being paid at the rate
tuned on the assumption that you were seated.

### 🔑 The machinery was already there. The skill simply never entered the state

`MoveState.Sitting` blocks the move tap, `HandleAttack` and `BeginSkill`; a hit stands you up; and
`EndsOnDamageTaken` — which Relax already carried — drops the buff the instant anything lands. Both
halves of your CSV clause were built. Nothing needed inventing; the stance needed wiring to the state.

### What shipped — `SkillDef.SeatsCaster` (new, Relax is its only user)

1. **On → you sit.** The same `MoveState`, the same `SatDownTick`, the same stand-up recovery, and the
   same *"you must be idle"* gate the sit button applies (engaged / casting / mid-stand are refused,
   out loud — a toggle that lights up and does nothing is the shape this fix exists to remove).
2. **Off → you stand.** The stance and the sit are one thing, so ending one ends the other; otherwise
   you are left rooted by a stance you can no longer see.
3. **The one exception to the seated cast gate**: a seating toggle **you are wearing** may be pressed
   to end it. Without it Relax would be the only toggle in the game you could not switch off the way
   you switched it on, and the only ways out would be the sit button and a monster. Turning one **on**
   while seated is still refused, like anything else.
4. **Standing by any other route ends the stance** (`EndSeatedStances`, called from the sit command) —
   or you could sit, toggle Relax, stand, and walk away still regenerating. A HIT needs no call there:
   the damage path already ends `EndsOnDamageTaken` buffs and stands the victim up in one breath.
5. **`SitDown`/`StandUp` are one shared pair**, so the sit command and the stance cannot drift into two
   different ideas of what sitting is.

⚠ **No CSV moved** — your row already said *"cannot act"*. As with `BL-265`, the code was behind the
file.

⚠ **Its `CAST 5` cell is not honoured and was not before this** — a `Toggle` flips instantly by design
(`BeginSkill` dispatches toggles above the cast machinery), so the 5-second sit-down animation your row
prices is not charged. Left alone deliberately: it is a separate decision, and `--check` passes because
the def does carry `CastTicks: 50`. Say the word if a seating toggle should pay its cast.

---

## `BL-275` ✅ BUILT 2026-09-23 in **0.187.0** — the warrior CSV edits of 2026-09-23

All four of his rows are in the code, and the DURR cell is settled the way the entry proposed: his
DESCR says *"10 times over 2s"*, and the code agreed once it moved, so **DURR is `2`** on all thirty
Whirlwind rows. `SkillCsvSeed --check` is green. Built:
- **Saints Sword Dance** — the wrapper and the stroke lose their radius: ten strokes at one body.
- **Whirlwind** — `ChannelShots` 20 → 10 and `DurationTicks` 40 → 20, the same power per stroke.
- **Master of Combat** — attack speed +20% → +10%, and −10 evasion (a flat minus on the buff, with
  `BuffEvasion` added to the mask so it actually applies).
- **Single Mark** (`single_mark`, new, warrior 2nd @20, 3,400 SP, toggle, 2H blunt, no MP) — ×2 crit
  rate (blunt 0.40 → a greatsword's 0.80), −20% on both skill-damage channels, and
  `Entity.SingleTargetOnly`: the skill sweep keeps only the main target (a self-centred ring keeps
  what you have selected, if it is inside), and the 2H-blunt basic cleave is off.

The entry as it stood:

## `BL-275` 🔴 THE WARRIOR CSV EDITS ALREADY IN THE TREE

Your uncommitted edits (2026-09-23), for the code to follow:
- `warrior 3rd/4th` **Saints Sword Dance**: `target/aoe` 150 → `target/single` 0.
- `war_aoe 3rd/4th` **Whirlwind**: *"10 times over 2s"* (was 20 over 4s), same power per hit.
  ⚠ **The DURR column still says `4`** on every row. I read your DESCR as the truth and DURR as a missed
  cell, so DURR becomes `2`. Say if not.
- `war_aoe 4th` **Master of Combat**: Atk.Speed +20% → **+10%**, and a new **−10 evasion**.
- `warrior 2nd` **Single Mark** (`single_mark`, @20, toggle, blunt/2): skill power −20%, P.Crit rate
  +100% (*"should match a greatsword's"*), and **every AoE off**, skills and passives: everything hits the
  main target only.
Built with `SkillCsvSeed --check` green, per the CSV rule.

---

## `BL-267` ✅ BUILT 2026-09-23 in **0.188.0** — the lock is per equipment item

Built as the entry's pick: **equipment locks per ITEM, stackables keep the def-id lock.** One flag,
two keys. The row carries `InventoryItem.Locked` (persisted, a new `ItemRecord.Locked` column);
the def-id set is unchanged. A pre-`BL-267` def lock on a gear def is still honoured, and unlocking
that item clears it. The wire did not change: the lock array now carries def ids and InstanceIds side
by side, and `SetItemLock`'s one string is whichever key the item uses. ⚠ Schema change, so a
`game.db` delete is needed.

The entry as it stood:

## `BL-267` ❓ THE LOCK IS PER ITEM, NOT PER DEF ID — a re-spec of `BL-239`

**Your words, 2026-09-23:** *"I want [lock] to be per equipment item not per item_ID .. now I have 2
maul weapons .. and one is +3 .. I lock it and I cannot sell the other maul"*.

`BL-239` (0.157.0) locked the **def id** on purpose, so that a locked potion stack stayed locked after
it was drunk empty and re-looted. That reason only holds for **stackables**. ❓ **My pick:** lock the
item INSTANCE for equipment (each maul is its own row, with its own enchant) and keep the def-id lock
for stackables. One flag, two keys. Or do you want the instance lock for stacks too? Then an emptied
stack forgets its lock.

---

## `BL-268` ✅ BUILT 2026-09-23 in **0.189.0** — landscape both ways

The entry's reading (*"`autorotateToLandscapeLeft/Right = true`, portrait both false"*) was
**already** the project setting, and had been for a long time. What stopped the flip was
`useOSAutorotation: 1`, which makes Unity declare `userLandscape`. That obeys the phone's rotation
lock, so with auto-rotate off the game never turns. It is now `0` (`sensorLandscape`), and `GameBoot.Awake`
sets the four autorotate flags + `AutoRotation` in code as well.

The entry as it stood:

## `BL-268` 🔴 LANDSCAPE BOTH WAYS

*"default game is landscape mode but I want to rotate on both sides (both landscapes only, no
portrait)"* — Unity `autorotateToLandscapeLeft/Right = true`, portrait both false. Needs an APK.

---

## `BL-271` ✅ BUILT 2026-09-23 in **0.190.0** — quest rewards in the combat channel

Built as specified: on completion the COMBAT feed gets one `EXP`-tagged line (exp, SP, gold, **as
banked**, after world rate × runes × the SP ceiling) and one `LOOT`-tagged line per reward item.
Server only.

The entry as it stood:

## `BL-271` 🔴 QUEST REWARDS IN THE COMBAT CHANNEL

*"receiving reward from quest should be shown in the combat channel .. exp/sp/reward .. if its written
a player can see and decide if that quest is worth repeating"* — one line per reward on quest
completion: EXP, SP, gold, each item × qty.

---

## `BL-276` ✅ BUILT 2026-09-23 in **0.191.0** — watchmen are NPCs, and fight a PK in town

Built. The guards stay MOBS in the simulation, because their "fighting script" *is* the mob AI (aggro,
chase, swing, leash), but the client is told `Npc`. It draws the NPC model, the yellow name, their
title (**Town Watch** / **Field Watch**), no aggressive `*`, and Talk (they answer with one line).
One helper, `TownShields`, lets a guard keep fighting a **PK** inside the safe zone: aggro, the caster
AI, the swing, and walking the town. A player's AoE no longer touches a guard with PvP off, which
fixes §102.7. A guard's own AoE reaches PKs only.

The entry as it stood:

## `BL-276` 🔴 WATCHMEN ARE NPCs, NOT MOBS

*"can town watchman and field watchman be NPCs? not mobs .. now they look like scary mobs red
aggressive lvl 90 .. they must be npc with titles and just fighting 'script' .. also they must be
allowed to fight inside the town - when a pk is inside a town and they lock on they should be able to
hit him"*. This also fixes §102.7 (your Whirlwind without PvP hit a field guard, and it killed you).

---

## `BL-278` ✅ BUILT 2026-09-23 in **0.194.0** — boss regen as a clock

Built as ruled. `StatCalculator.MobHpRegenPctCombat` (float, 0.001) is gone; `MobRegenDivisor` (int,
30000) replaces it on the admin Tune tab. Engaged HP/s = `maxHp ÷ D × EnrageRegenMult(stage)`, ×1 / ×2 / ×10,
hard-coded. Below D the tick is **skipped** (the regen loop's 1-HP floor would otherwise hand a point back).
0 on the panel = no engaged regen at all. Idle 5%/s and the engaged MP rate are untouched.

The entry as it stood:

## `BL-278` ❓ BOSS REGEN AS A CLOCK

§4 of the design doc. The pick: bosses `1/30000`/s, ×2 at the 1st enrage, ×5 at the 2nd; normal mobs
get no in-combat regen (5%/s idle stays). ❓ Is that one knob today or two?
✅ **2026-09-23:** one rule for every engaged mob: **HP/s = maxHp ÷ D, D = 30000** (one INT admin knob,
replacing `MobHpRegenPctCombat`). Bosses ×2 at the 1st enrage, ×10 at the 2nd (200/400/2000 HP/s on 6M).
No rank split; **maxHp < D → 0, not computed**. The multipliers are hard-coded. Idle 5%/s is unchanged.

---

## `BL-279` ✅ BUILT 2026-09-23 in **0.192.0** — custom skill delay for auto-hunt

Built to his spec. **The ❓ answered itself:** the delay persists **per skill id, per class**, in the
auto-hunt config beside the Auto mark. `AutoSkillDto.ExtraDelayTicks` already existed there, the
server already honoured it as "added", and no UI had ever set it. It gained `DelayExact` and
`DelayOn`. A slot is only a view of that, so moving the skill to another slot keeps its delay.

The entry as it stood:

## `BL-279` 🔴 CUSTOM SKILL DELAY FOR AUTO-HUNT

**Lost once already**; it is in no file in the repo. Your spec, 2026-09-23:
- Holding a bar skill → its context menu gains **"Delay: ON/OFF"** and **"Custom delay"** under "Auto on".
- "Custom delay" opens the numpad picker. The value is 1-9999 s, used by auto-hunt only, per skill.
- Where the picker has "max", it gets an **exact / added** switch:
  - **added** — the delay runs AFTER the skill's own reuse: 0.4 s reuse + 1 s = every 1.4 s.
  - **exact** — the skill fires every N s from use to use: the live reuse (it is dynamic) is subtracted
    at use time, and N can never go below the skill's real reuse.
- OK sets **Delay: ON** and puts a clock icon on the slot; the toggle switches between the default and
  the custom delay without losing the number.
❓ Where does it persist — with the server-owned bar (per character, per slot), or per skill id?

---

## `BL-269` ✅ BUILT 2026-09-23 in **0.193.0** — extra skill slots

Built as a **view setting**, which is what the entry's ⚠ asked for. The server's bar was already 60
slots (five pages of twelve), so nothing stored changed and `SyncSkillBar` needed nothing. Settings →
**Extra skill slots** cycles off / 6 / 12 / 18 / 24 (kept in `PlayerPrefs`). The extra squares are
rows of six stacked above the main bar, and they show the **pages after** the main bar's current page,
so they never repeat it and they page along with it.

The entry as it stood:

## `BL-269` 🔴 EXTRA SKILL SLOTS — 6 / 12 / 18 / 24

*"need option to add more 6/12/18/24 skill slots (half of or full the 2nd and 3rd skill bars) - like
additional skill bars"*. ⚠ **The bar belongs to the SERVER** (CLAUDE.md): the extra slots are extra
server-owned bar pages, persisted, and auto-placement (`SyncSkillBar`) has to know about them. The
option picks how many are SHOWN; it must not change what the server stores.

## `BL-05` ⤴ SUPERSEDED 2026-09-23 by `BL-273` — the old crafting's leftovers

Archived on your OK (2026-09-23). The 0.63.0 crafting it described (masters, six levels, the grade
ladder, the gear roll, the mat costs) is replaced wholesale by the 2026-09-23 rework: `BL-272` rarity
collapse, `BL-273` crafting, `BL-274` per-mob drops (`docs/design/Rework-2026-09-23.md`). Its two
consumable-ladder pieces (the stones and the chest/rune-box/exp-box economy) were copied into
`BL-273`, since the rework's consumable recipes (§2.2 #8) are where they now belong. The two "odd
numbers" it held (the C rung's 8 Rare mats, the 347 h S character) describe recipes that will no longer
exist; `BL-282` (`--craft-cost`) prices the new ones.

The entry as it stood:

- `BL-05` 🔵 **Crafting — the two pieces you did NOT rule.** The system itself SHIPPED in 0.63.0
  (masters, six levels, the freeze, the grade ladder, the gear roll, the mat costs, quitting). What is
  still owed is only what you left open:
  - **Where elemental + skill stones sit on the Potion Master's ladder** — *"somwhere and elemental
    stones + skill stones"*, no rung named. Not invented.
  - **The chest / rune-box / exp-box economy**, your own *"something like that"*: both consumable
    masters craft treasure chests of random scroll/potion loot as a sink against the **60kk gap to a
    Mythic S item**; Potion Master → tradable temporary War/Spell rune boxes (1h/2h), Scribe →
    tradable temporary EXP/SP boxes (5-30%, 1h/2h). A sketch, deliberately not built — spec it against
    the held War/Spell Rune and the `BL-01` premium runes, not as a new system.
  - ⏸ **Two numbers, left as they ship (your call, 2026-08-13):** *"the farm times will work on them
    leave them as is .. later will decide on them."* Both are measured and both are odd — the **C rung
    costs 8 Rare mats**, so a C recipe reads cheaper than an E one (the Rare faucet is 0.09/kill against
    Common's 1.76 while your C target is 5-10h), and a **fully S-geared character is 347 farm hours**.
    Shipped as-is on purpose; nothing is retuned until you say so. See `docs/balance/CraftingMats.md` §8.
