# Changelog

Development history, newest first.

Early work was tracked as **phases** — self-contained slices that each ended in a playable build.
Phases 1–3 built the foundation (movement, interest management, combat, skills, buffs, the
safe-zone town, banded hunting grounds); the written phase record runs to **Phase 24.1**
(2026-06-22). After that the phase numbering was dropped and commits became the record, so entries
from mid-2026 on are grouped **by date** instead. Later, `GameConstants.GameVersion` (starting
0.1.0, currently **0.42.5**) began gating the client/server protocol handshake — it tracks wire
compatibility, not this feature history.

For what's *planned* rather than done, see [Roadmap.md](Roadmap.md).

## 2026-08-01 — A door to the crypt, and a ceiling on the bar (0.42.5)

**The Hollow Crypt was on every gatekeeper's menu.** A dungeon entrance is a safe zone, and every
safe zone was offered by every gatekeeper — so a level-1 standing in Brackenford was shown a level
44-48 dungeon in the same list as his first hunting field. Worse, the crypt's *field* was managed by
no city at all, which meant its named gate ("Hollow Crypt Halls") appeared in **nobody's** menu: the
only way inside was to teleport to the entrance and walk. So the dungeon was simultaneously offered
to everyone and reachable by no one.

A safe zone can now name a city that gates it (`SafeZone.GatedByCityId`), and the crypt names
**Greymarsh** — the city whose hunting band (40-60) contains the crypt's (44-48). Greymarsh's
gatekeeper offers the entrance and the halls; nobody else offers either. Giving the field a managing
city also fixed the second consequence: dying in the crypt used to fall through to nearest-city, and
from a point at (-9600, -11000) that is a meaningless answer, since every city is thousands of units
away in the positive quadrant. The crypt now returns its dead to the city that sent them.

The boss vale was deliberately left alone. Its band (58-60) is the last two levels of Greymarsh's
range, but it sits on Ironreach's doorstep — band and geography disagree, and there is no obviously
right answer to pick on the owner's behalf.

**The buff bar has a ceiling: 24.** Over it, the **oldest buff is dropped and the new one lands** —
never the reverse. A refusal arrives mid-fight and sends you hunting through the bar for something to
cancel, which is the exact moment you cannot afford to be reading icons. Three kinds of effect sit
outside the budget, each for its own reason: **debuffs**, because you did not choose them (counting
them would make every DoT a dispel, refusing them would make a full bar a debuff immunity);
**persistent gear effects**, because reconciliation puts them straight back, so evicting one buys a
slot for a fraction of a second and costs a flicker; and **toggles**, because only you switch those
off. Re-applying a buff makes it young again, so recasting a blessing does not leave it first out of
the door.

Also verified rather than built: **admins were already excluded from every leaderboard**
(`Role != AccountRole.Admin`, one query, one place). The roadmap had been carrying it as open work.

## 2026-08-01 — Two words a player can actually say (0.42.4)

The damage runes carried borrowed names. "Soulshot" and "Spiritshot" are another game's words, and
this project's rule is that formulas may be adapted but names may not be borrowed — the same rule
that renamed the towns and the currency. They are now the **War Rune** (+100% P.Atk, physical only)
and the **Spell Rune** (+41% effective M.Atk and +40 flat cast, magic only).

The owner picked the pair on the grounds that matter at a vendor: *"otherwise players will have a
mouthful to buy/sell/explain."* Two syllables each, and which one a fighter wants is legible from the
name alone.

Renamed with them: the eight sealed boxes (`War Rune Box (1h)` … `Spell Rune Box (30d)`), the
newbie choice box, the Apothecary's daily (`daily_runes`), and every id behind them —
`rune_war` / `rune_spell` / `box_war_rune_*` / `box_spell_rune_*`. ⚠ The item and quest **ids
changed**, so an existing `game.db` holds rows pointing at defs that no longer exist; delete it
(only the seeded admin account and any character that opened a box is affected).

Two skill names went the same way for the same reason: **Power Strike → Brutal Strike** and
**Power Shot → Heavy Draw**. Their skill ids are untouched — ids are append-only here and never
reach a player's eye. The bow skills that merely contain the ordinary English word "shot"
(Precise Shot, Repelling Shot, Snare Shot) were left alone.

## 2026-08-01 — Mobs stopped out-healing the player (0.42.3)

**Mob regen was on the PLAYER's CON curve, and mob CON is not player CON.** `HpRegenPerSecond` is
`(3 + 0.1·level) × 1.03^(CON − 40)` — an exponential, correct for a player, whose CON spans 36–47 and
so spreads only ×1.4 across every build in the game. A mob's CON is `15 + 2·level`: **195 at level 90**,
compounding ×1.06 *per level*, while `MobBaseStats.Hp` only grows as `40 + 0.8·level²`. Exponential
against polynomial has exactly one ending:

| mob level | CON | old regen | its whole HP bar | % of bar per second |
|---|---|---|---|---|
| 37 | 89 | ~29 HP/s | 1,135 | 2.6% |
| 75 | 165 | ~420 HP/s | 4,540 | 9.3% |
| 90 | 195 | ~1,170 HP/s | 6,520 | **18%** — its whole bar every 5.6s |
| 200 | 415 | ~1,500,000 HP/s | 32,040 | 4,700% |

The owner met the mid-level end of it: *"someone hitting a lvl-37 mob for 500 … if I'm not top geared
and start doing 100–200 the regen will overpower me"*. It was arithmetic, not gear.

**Dividing the curve was considered and rejected.** `÷10` holds to about level 110 and is absurd again
by 150 — it does not fix the cliff, it slides it forty levels along, which is the trap the owner named:
*"I don't want to get caught balancing everything for today's level range and tomorrow need rebalance
for introducing higher lvls"*. Anything with a level term in it has that problem. The fix has none.

**Mob regen is now a fraction of the mob's own pool, split by combat**, with no level term anywhere:

| | rate | what it means |
|---|---|---|
| engaged | **0.1%/s** | a maximum kill time: finish inside ~16 minutes |
| idle | **5%/s** | 20 seconds back to full, from any HP |

Both sentences stay true at level 1, level 200, on a 40-HP rat and on a five-million-HP boss, which is
why there is no boss special case and nothing here to revisit later. The in-combat figure is
deliberately tiny — its *only* job is to stop a hopelessly weak attacker chipping something down
forever (a mob wedged on geometry). It is **not** the anti-underlevelled mechanic: the level-gap table
already is that, with 75% avoid at 19 levels and a total lockout at 20+.

**`ResetMob` no longer heals to full**, and that is the substance of the change rather than a detail.
It ran from `Disengage` as well as from the leash, so a mob was *pristine the instant you left its
view* — the climb back to full never existed and nothing could be re-engaged while still hurt. It now
walks home wounded. The fast idle rate is its own abuse limit: hit-and-run into a safe zone gives the
mob back 5% of its bar for every second you are away.

Three things moved out of `ResetMob` into a new `MobRecoveryCheck`, which fires when the bar actually
reaches the top — they are properties of *"this pull is over and the creature is whole again"*, not of
*"it stopped chasing you"*:

- **the damage ledger** (owner: take it to 30%, run, and you are still on the ledger whether someone
  else finishes it or you come back — it resets at 100% *and* out of combat),
- **enrage** (a boss that disengages at 30% is still the enraged boss you left),
- **the boss phase cursor** — re-arming that at 30% HP would have made `AdvanceBossPhases` fire every
  remaining threshold in a single tick on the next pull: announces, enrages and add waves at once.
  Previously unreachable, because the full heal hid it.

**Players are untouched** — the tank keeps his CON bonus. Across the real 36–47 band the curve is a
×1.4 spread, which is exactly what it was designed to be; it only broke when fed a number three times
larger than any player will ever have. Both percentages are on the **live tuning panel**
(`Mob regen in combat` / `Mob regen idle`), so they can be swept during play instead of rebuilt.

**An improved buff reads like a Harmony buff.** Its popup said `Parts: Might and Bulwark` — a list of
one name. Since 0.42.0 a group is ONE buff carrying merged numbers, and the server has been sending
that buff's real description all along (`ApplyBuff`: `isGroup ? def.DescriptionAt(level)`); the client
was overwriting it with a part list built for the old fan-out shape. It keeps the description now, and
only appends the parts when several rows genuinely share a parent.

**Press-and-hold: 1.0s → 0.65s.** Reported as *"like 2s"* — a threshold with no feedback until it fires
always feels longer than it is. Android's own long-press is ~0.5s; this sits just above it so a slow tap
is still a tap.

## 2026-08-01 — Playtest-16: four windows that showed the work but not the answer (0.42.2)

Four items passed their checklist row and still failed the reader. Each was told what it was, never
what it was worth.

**A set now says what it grants** (§35a). The item window printed the set's `ClassFlatBonus` — and
*every tiered set leaves that empty*, carrying its real bonus in `Mods`. So the answer to "what does
this set do?" was a piece list and a blank, for nearly every set in the game. It reads `SkillText.Mods`
now (the formatter both clients already share), and the shield-conditional extra gets its own line,
because the shield completes nothing — it only adds.

**One confirmation at a vendor, not two** (§35b). 32d put a details dialog in FRONT of the numpad, so
buying a stack walked three windows to spend gold once. The details moved onto the **row** — buy *and*
sell, with a consumable's effect on it — and for a stackable **the pad is the confirmation**: it shows
the running total on every keystroke and its button says `Buy` / `Sell` rather than `OK`. A
non-stackable keeps the single confirm dialog; it has no pad to carry the question.

**Every drop row carries its own %** (§35c). The tree printed a member's share only when a per-item
override had been set, so an untouched group was a bare name list and looked like an even split, which
the weights never promised. Every member now prints `chance × its share` — what you actually get per
kill — and they sum back to the group's line.

**Masteries group by WEAPON, not by stat** (§35d). 32g pivoted them stat-major; the owner rejected it
on sight, and he's right: these are authored per weapon group, so stat-major reprints the same numbers
under every stat. Sources granting identical effects are folded into one row —
`Sword/Blunt:  P.Atk +10, M.Atk +10, Cast speed +10%` over `Dual/dagger/Bow:  Cast speed -100%`.
Sources granting nothing are dropped: a mastery that ignores bows says so by not mentioning them.

## 2026-08-01 — The admin class change picks the discipline, not just the 2nd class (0.42.1)

The admin panel's **Class** tab only ever offered the *2nd* class. The 3rd class had a hub method
(`DebugThirdClass`) and the Unity channel had the call — nothing anywhere invoked it, so the only
route to a discipline on the phone was its quest, with the item hand-ins and kill counts an admin is
trying to skip in the first place.

One list now covers **both tiers**. Each 2nd class is a row, with its two disciplines indented under
it; tapping a discipline grants the 2nd class along with it (`HandleDebugThirdClass` already forces
the parent 2nd class), so the whole change is **one tap**. The plain 2nd-class row stays for
below level 40, where the server refuses a 3rd class deliberately — the panel says so instead of
letting you find out by being refused. A discipline a *sibling* class already walks is shown greyed
as a note rather than offered, matching the uniqueness rule the server enforces.

A debug 2nd-class change also **saves immediately** now, as the 3rd-class path always did, instead of
waiting for the 60s autosave.

## 2026-07-31 — The Warchanter's kit, and the improved buffs go party-wide (0.41.1)

Still a placeholder until the 3rd-class CSV, but the shape is the owner's now.

**The improved groups are PARTY buffs** (`AlliesInRadius`, 800) — the answer to *"improved are party
right? If not make them."* **Harmony went with them**: that was already the recorded decision in the
design doc, and for a concrete reason — the autopilot hard-targets *self* for buffs, so a
single-target Harmony could never be handed out by a buffer left on auto-farm.

**Each improved group `Replaces` the singles it contains.** Learn *Might and Bulwark* and Might,
Bulwark, Vampirism and Aim leave the bar. Four skills become one; the bar collapses as the class
matures. (The replacement is on the *skill*, not the buff — the buffs still resolve by family rank.)

**The Warchanter's buff kit, 40 → 74:**

| Levels | What |
|---|---|
| **40-64** | every single ladder **topped out** — the cleric leaves off mid-ladder (Might L2 of 3, Focus L4 of 6) and never sees Ferocity, Insight, Body, Soul or Serenity at all |
| **60 / 62 / 64** | the three **Harmony** blessings (was 40/52/62) |
| **66 / 68 / 70 / 72 / 74** | the five **improved** groups, one per learnable level: Swift and Sure · Might and Bulwark · Force and Ward · Focus and Ferocity · Body and Soul |

Every family reaches its **max rung before** the improved buff that contains it — not enforced
anywhere, just the logic of the class: you learn the parts, then you learn to cast them in one breath.
Frenzy is deliberately not one of the five (its rung is already a whole eight-effect buff), so it ramps
with the singles at 62 and 64.

## 2026-07-31 — The cleric buffs one at a time; the group is the buffer's (0.41.0)

The other half of playtest-15 §2, and the answer to *"when I gave the CSV I made the buffs improved"*.

**Aim — the accuracy line, and the last missing potion.** Accuracy was a class-buff-only family; it is
now the exact mirror of Agility (evasion): **+1 / +2 / +4**, with its own potion and its own scroll at
Common / Uncommon / Rare. Hit and evade are the two halves of one contest, so a player who can buy one
can now buy the other. Vendor-stocked at Common, in the drop rungs and the recipe lists like the rest.

**The cleric learns the INDIVIDUAL buffs.** It used to learn five *groups*. Now:

| | learns | MP |
|---|---|---|
| **Base mage** (7) | Might, Bulwark | 30 |
| **Cleric** (20-35) | Might · Bulwark · Force · Ward · Aim · Vampirism · Resolve · Focus · Vigor · Swift · Alacrity · Agility · Haste · Frenzy | **30-50** |
| **Warchanter** (74) | Might and Bulwark · Force and Ward · Focus and Ferocity · Body and Soul · Swift and Sure · Frenzy at its top rung | **150-200** |

Every rung a cleric gets is the one the corresponding group level used to hand out, so a cleric who
buffs their whole list lands **exactly where they were** — it just costs more casts. That is the point:
the group is not a bigger number, it is four or five effects in one cast, and it is what the buffer
*class* buys. Six new levels of MP were added to each group to match (150 → 200 across levels 1-6).

**Harmony has somewhere to go.** The three Harmony blessings are **learnable by the Warchanter at
40 / 52 / 62** — the layer with no potion, no scroll and no NPC that sells it, stacking on top of the
basic buffs. They became real player skills (200 MP, 1.5s cast, 600 range, 20 minutes) instead of
NPC-only defs. ⚠ The owner listed 40/52/62/**74** for Harmony, but only three Harmony blessings exist,
so 74 is the improved tier's slot; a fourth would have to be authored. All of this is explicitly a
placeholder — it will be re-cut with the 3rd-class CSV.

**The admin buff button grants everything**: the five improved groups, the three Harmony blessings and
all 19 singles — 27 in total. The groups are applied **first** on purpose, so the buff bar shows them
collapsed as groups rather than as fifteen loose squares (a group and its singles are the same rungs
for the same hour, and equal rank + equal time is refused, so whoever lands first owns the bar).

## 2026-07-31 — A potion buys one blessing, not all of them (0.40.0)

Playtest-15 big design **#2**, and step 6 of `docs/design/BuffLadders.md`. The speed group proved the
mechanism in 0.36.0; this is the other eleven families, their potions and scrolls, and the end of the
bug that started it: *"buff potions stack with the current buffs, making characters stronger than
intended."*

**Fifteen families, one number line each.** Every source of an effect — a potion, a scroll, a rung of
a class buff, the NPC buffer's hour — now applies the *same* single-buff skill, so they compete on the
family key by rank instead of adding up.

| | Family | Potion | Scroll |
|---|---|---|---|
| **Might** | % P.Atk | ✓ | ✓ |
| **Bulwark** | % P.Def | ✓ | ✓ |
| **Force** | % M.Atk | ✓ | ✓ |
| **Ward** | % M.Def | ✓ | ✓ |
| **Vampirism / Accuracy / Resolve** | melee vamp · accuracy · interrupt resist | — | — |
| **Body / Soul** | % Max HP · % Max MP | — | ✓ |
| **Vigor / Serenity** | % HP regen · % MP regen | — | ✓ |
| **Focus / Ferocity / Insight** | crit rate · crit damage · magic crit | — | ✓ |
| **Frenzy** | the whole trade-off buff | — | ✓ |

- **Potion + scroll families** run Common/Uncommon/Rare and the Rare rung equals the strongest class
  buff. Deliberate: consumables can cover the whole *basic* layer, and what keeps a buffer worth
  grouping with is Harmony, which has no consumable at all.
- **Scroll-only families** run **six** rungs and their scrolls sit on rungs 2 / 4 / 6, sold as Epic /
  Legendary / Mythic. Rarity is the price tier, rank is the power — an Epic Body scroll is rung 2 of 6.
  Three of the four families with no potion at all are the ones a class buff climbs furthest on.
- **Vampirism, Accuracy and Resolve have no consumable at any price.** They exist only inside a class
  buff, which is what a buffer still sells that a shopping trip cannot.

**The class buffs became groups.** Might (base mage + cleric), Force, Focus, Body and Frenzy now apply
children instead of one monolithic buff, the way Improved Speed already did. **Levels 1-4 cast exactly
the numbers they cast before** — nobody's buff changes today — and levels 5-6 climb to the NPC
buffer's maximum, waiting for the Warchanter tables. Their names follow the owner's rule (no "Improved
X"): **Might and Bulwark · Force and Ward · Focus and Ferocity · Body and Soul**, and Improved Speed
is renamed **Swift and Sure**.

⚠ **One real change of substance:** the old Might used `BuffAtk`, which raises *both* channels — a
mage's M.Atk was riding along on a physical blessing. The Might family is P.Atk only; M.Atk is the
Force family, with its own potion.

**The NPC buffer split too.** Its five bundled blessings were the last place a potion could stack on
top of something (a monolithic 1h Might on its own key). It now hands out **19 singles**, one per
family, each cancellable on its own — the "Full buff" button is unchanged and still does all of them
in one click. Per-buff price **halved** (3000 → 1500 per buff-level) so the full set costs about what
it did with 9 buttons rather than double.

**The admin buff button** now grants the **admin set**: those 19 *plus* the three Harmony blessings,
which no NPC offers and no consumable can reach (owner's ask). It is the only way to see a fully
buffed character, which is the state balance numbers should be read at.

**Where they come from.** The four potion families join the existing drop rungs (Common/Uncommon/Rare)
and the alchemist/scribe recipe lists; the Common potions are vendor-stocked. Scroll-only families
enter the drop table at **Epic from level 60** and **Legendary from 76**. Mythic buff scrolls have no
source yet, the same way Dash Mythic doesn't — both wait on playtest-15 §3, the drop-group rework.
Group weights were **not** raised: a rung with more items in it splits the same weight finer.

**Also:** `BuildCatalog` now fails at startup if a group buff names a child id that doesn't exist. A
typo there compiled fine and produced a buff that cast, cost MP and did nothing at all.

## 2026-07-31 — The autopilot casts in an order you chose (0.39.0)

Playtest-15 big design **#1**, plus §32u (free travel while levelling).

**Skill chains.** The auto-hunt used to walk one flat list top-to-bottom and cast the first thing that
was ready, which meant a short-reuse skill in slot 1 could starve everything below it and a heal
competed with an attack for the same turn. Now:

- **Three priority groups — heals → buffs/debuffs → attacks.** The first group with something to cast
  gets the tick; inside a group the order is still the bar order.
- **Cyclic vs first-available**, a toggle in the Auto Farm window. *First available* is the old shape
  (restart at the top: 1-2-1-3-1-4); *cyclic* carries on from the last skill used and only wraps once
  the rest of the group has had its turn (1-2-3-4-1). One cursor per group. Cyclic **wraps rather than
  waits** — the strict reading ("never go back to 1 until the last has fired") would park the
  character doing nothing while a 60s skill recharges.
- **A heal threshold of your own** (slider, 10–100, or off) instead of the hard-coded 70%. At **100**
  it heals on cooldown — the owner's one sanctioned piece of auto-support for a played healer. The
  heal also picks a target now: the most injured party member under the threshold within the skill's
  range, else yourself.
- **Buffs are renewed at under 60s left**, not only when missing, and a weaker rank counts as missing.
  The window is capped at half the buff's own duration so a 30s buff isn't recast every cycle, and a
  *strictly stronger* buff of the family is left alone (recasting under it is refused by `ApplyBuff`
  anyway — it would just burn MP every cycle).
- **Debuffs** fire when missing **or weaker** on the enemy. The old test was "any buff with this key",
  so a rank-1 debuff blocked its own rank-3 upgrade for the whole duration.
- **Assist party leader**: in a party you don't lead, the only target you may take is the leader's —
  and with no leader target you stand still. It overrides acquisition, retaliation and roaming
  together, because an "assist" that wanders off after whatever hit it is not assisting.

Config travels in `AutoHuntConfigDto` (three new optional fields) and persists inside the existing
`AutoHuntJson` blob — **no schema change, no `game.db` reset**. An old client that never sends them
gets exactly today's behaviour (threshold 70, first-available, no assist).

**Free travel under level 40 (§32u).** The gatekeeper fee is now `TeleportFee(level, …)`: nothing
below `GameConstants.FreeTeleportUnderLevel` = 40, the distance fee from 40 on. The price list and the
charge go through the same call, so you are billed what you were quoted, and both clients print
**"Free"** rather than "0 gold". It was never built before — the owner's "what happened to the free
teleport under 40?" had the answer "nothing, it doesn't exist yet".

## 2026-07-31 — A single item can be tuned on its own (0.38.1)

Playtest-15 big design **#3**, scoped down after the answer turned out to be "the math already
exists". His question — *"10 items all at the same %, roll 0.048 → pick one of the Commons"*, and
*"group at 100%, all items 100% — how to pick one at random?"* — describes **weighted single-pick**,
which is what `RollDrop` has done since the groups shipped: a group rolls once at the summed member
chance, then picks one member weighted by the individual chances. Equal percentages are equal
weights, so "all at 100%" is a uniform random pick. Nothing to build there.

What was genuinely missing is the ability to move **one item** without moving its whole rarity rung,
since a gear group is authored as one family × rarity rung. So: a third rate knob, per ITEM.

- **`RateConfig.DropItemRates`** — empty by default, so this ships inert. Composed on top of the
  group and global rates in two new helpers, `MobCatalog.ItemWeight` (authored chance × item rate)
  and `MobCatalog.EffectiveChance` (that × the group rate).
- **`/droprate item <id or name> <mult>`**, `x1` clears the override. It accepts the display **name**
  as well as the id, because the drop list on the phone shows names and nothing in the client ever
  shows an id — id-only would have meant guessing. A miss suggests up to five near matches.
- **The weighted pick now uses the tuned weight**, the same quantity the group's trigger is summed
  from. Using the raw authored chance there instead would have let the knob change how often a group
  fires without changing which member it lands on — the one thing it exists to do.
- **Inside a guaranteed group the knob moves SHARE, not volume.** Measured: Scroll of Resurrect at
  ×5 moves level-25 consumable value 95 → 107 while items-per-kill stays 2.89, because the Always
  group is already at 100% and the boost comes out of its siblings. That is the correct meaning of
  "tune this item, not its rarity".
- The target-inspect tree and `tools/BalanceMatrix` read the same two helpers, so a tuned item's
  displayed % stays the real one; a tuned group additionally prints each member's share.
- **Verified a no-op at default**: `BalanceMatrix` output is byte-identical before and after.
  The gear groups were deliberately left alone — they produce the gold curve the 2026-07-31 playtest
  confirmed by play, and re-authoring them was the branch not taken.

## 2026-07-31 — Jewels get their own slots (0.38.0)

Checklist **32t**, the last build item in §32. Jewels behaved like a LIST — five anonymous squares
filled in whatever order the bag happened to be in, and a third ring was refused with a message
instead of replacing one. Now they behave like gloves.

- **Five designated slots**: necklace · earring · earring · ring · ring. The paper-doll squares are
  named (`neck`, `ear1/2`, `ring1/2`), so an empty one says what belongs there.
- **Equipping into a full pair displaces, never refuses.** Which one goes: the **weakest**, and on a
  tie **slot 1** — the owner's rule, verbatim (`no slot < common < uncommon < … < mythic`). His
  worked example traces exactly: 1st common → slot 1; 2nd common → slot 2; a rare → slot 1 (tie); an
  uncommon → slot 2 (weaker); another uncommon → slot 2 again. A necklace (cap 1) degenerates to the
  same rule and simply swaps. You are told what came off.
- **Enchant breaks a rarity tie** (`ItemCatalog.JewelStrength`) — the one place the owner's rule was
  silent. Without it, replacing "the weakest of two commons" would drop the +3 as readily as the +0.
- **Which slot a jewel sits in is DERIVED, not stored** (`ItemCatalog.JewelSlotOrder`: strongest
  first, `DefId` as the stable tie-break). No new column, so **no `game.db` reset**, and the slot can
  never drift out of sync with the items. `DefId` rather than an instance id on purpose: the live
  `InstanceId` is regenerated on load and would reshuffle the pair on every relog.
- Server and both clients share the same two helpers, so the square you see and the jewel the server
  would replace can't disagree.

## 2026-07-31 — Playtest-15 batch 2: the windows stop withholding what they know (0.37.0)

The rest of §32 apart from the two that want their own pass (32t jewel slots, 32u free teleport).
Every item here is the same shape: the data existed and nothing was showing it.

- **The NPC buffer gives the BASIC tier only, one hour each** (owner: *"not the improved and
  harmonies … just the scroll buffs, 1h of single basic buff"*). Its Improved Speed GROUP is gone
  from `NewbieBuffSet`, replaced by four separate one-hour singles — **Swift / Alacrity / Agility /
  Haste** — and the three **Harmony** buffs are no longer offered at all. Both sets of `SkillDef`s
  stay in the catalogue (a buffer CLASS is meant to have the improved groups; nothing grants Harmony
  today). The buffer's edge over a potion is now only the DURATION, which is what the buff ladders
  were built to make true. Price is unchanged: still nine buffs.
- **32c the set bonus lists its pieces.** Which slots the set needs, the item that fills each, `[x]`
  for the ones you are wearing, `n / 4` at the top, and the piece you have on instead when it is the
  wrong one. The completion rule mirrors the server's `DetectActiveSet` (body carries the set id,
  the other slots the shared accessory line) rather than guessing at it.
- **32d a stackable opens its DETAILS first, then the numpad.** You were typing a quantity for
  something you had not been told anything about; the description only appeared at the confirm, one
  step too late. Buying and selling share one description builder now, so they cannot drift.
- **32e character select can delete a character.** There was no button at all — the server side
  (schedule, grace window, cancel) has been there the whole time and only the WPF harness could
  reach it. Delete is behind a confirm naming the character; a scheduled character stays listed,
  dimmed, counting down, with Restore in place of Delete. This is also what made 28e untestable.
- **32f the drop list is a TREE.** A group is a title line carrying the group's own name and chance
  (`Armor · Rare  (2.4%)`) with its items indented under it. As flat rows one 5% group read as five
  separate 5% drops — five times the truth.
- **32g mastery numbers are grouped by STAT, not by weapon.** `Cast speed:  Blunt +5%, Other −10%`
  instead of a cast-speed line under each weapon in turn. The mage's weapon proficiency read
  "+cast, −cast, +cast …" down the window and left the reader to hold four numbers in their head to
  see which weapon they should be holding.
- **32n consumables count on the hotbar** — bottom-left, `1…99` then `99+`, summed across split
  stacks.
- **32q auto-farm shows its remaining time.** `AutoHuntStatus` now carries the two runtime budgets
  (online idle, offline), the Auto button counts down in buff-timer format, and both the toggle and
  the start of an offline session say the budget in chat. The idle cap was being spent silently and
  the session simply stopped one day.
- **32r the farming-range circle needs auto-farm ON as well as the toggle** — with farming off it was
  drawing a rule nothing was enforcing. The toggle stays a remembered preference.
- **Buff ladders step 5: an improved buff is ONE square again.** Casting the cleric's Improved Speed
  put four squares on the bar — correct (the group applies four independent children, which is what
  lets a potion override one part of it) and unreadable. `BuffDto` now carries `SourceSkillId` +
  `SourceName`, set only for a group with MORE than one child, and the bar merges them: the parent's
  name, the SHORTEST remaining child as the timer, the parts with their own times in the popup, and a
  hold-to-cancel that drops the whole blessing rather than leaving three unnamed leftovers. A potion
  and a scroll are one-child groups by the same mechanism and are deliberately left alone — labelling
  their square with the bottle instead of the effect would be noise, not grouping.

⚠ **Protocol stays 8.** `AutoHuntStatus` and `BuffDto` each gained fields WITH DEFAULTS, which by the rule written
on `GameConstants.ProtocolVersion` is not a break (a missing DTO field degrades to its default; a hub
signature does not).
⚠ **Unity-side and therefore NOT compile-verified by `dotnet build`** — `GameUi.cs`,
`GameUi.World.cs`, `GameUi.Items.cs`, `GameUi.Vendor.cs`, `GameUi.AutoHunt.cs`, `GameBoot.cs` and
`NetworkChannel.cs` all changed. The APK build is the only thing that compiles them.

## 2026-07-31 — A potion argues with one part of a blessing, not the whole of it (0.36.0)

> ⚠ **Superseded by 0.42.0.** The child fan-out described here was the owner's later rejection: a
> group is ONE buff again, and conflicts resolve by FAMILY rather than by key. Kept as the record of
> why the ladder exists at all — the *families* and *ranks* below are still the live model.

An improved buff stopped applying a buff of its own and applied **children** instead — Swift,
Alacrity, Agility, Haste — each an ordinary buff on its own family key with its own rank, each
resolving alone against whatever you had already drunk, read or been blessed with. A rare Alacrity
potion took over the cast part of a low-level Speed and left the movement alone. No override tables:
one number line per effect.

**Equal rank keeps the LONGER remaining time**, which is the rule that makes the ladder honest — a
potion and a scroll of one tier are the same buff and differ only in duration, so a 20-minute potion
would otherwise have eaten an hour-long scroll. And a consumable refused on rank is no longer
swallowed and put on cooldown for nothing: it says a stronger blessing is up and stays in the bag.

Three things the fan-out broke on the way, all fixed here: the *"already up"* test keyed on the
parent (which now matched nothing, so the autopilot would re-cast every cycle and drink a whole stack
one bottle at a time); persistence saved a buff under the id of whatever GRANTED it, so a relog
re-applied every sibling at full duration — a free refresh for logging out; burst potions (Dash) are
never auto-drunk.

**Data:** the four speed families at Common/Uncommon/Rare, their potions (20 min) and scrolls (1 h),
the six-rarity Dash line on its own family (it must never evict an hour-long Swift), Improved Speed
re-authored to six levels, Wind Walk deleted. Spec: [design/BuffLadders.md](design/BuffLadders.md).

## 2026-07-31 — Nothing walks you into melee unless you asked it to (0.35.1)

The owner's correction to 0.35.0's half-measure. That build shipped *"auto-farm does not melee-walk
CASTERS"*; **the rule has no class in it** — nothing closes the distance unless commanded. Exactly
three things command it: the second tap on a target, the Attack button (bar action or target frame),
and, in auto-mode, the basic-attack action being on the bar and set to auto-on.

`AfterOffensiveSkill` tested `BaseClass != Mage`, which spared the nuker and still charged the bow
rogue in after a shot. It now asks whether the melee it is about to resume was **ordered** — which
needed somewhere to remember the order, since `HandleCast` deliberately wipes `Engaged`:
`Entity.AttackCommandTargetId`, set only by `HandleAttack`, cleared by a manual move, a follow, a
disengage and death, but **not** by a cast. So tap-tap-then-skill still chases, while a skill pressed
on its own never starts one, for any class. Mobs are exempt — their AI takes no orders from a hot bar.

Client-side the three ways to say "attack" were three code paths, and only the tap knew a party member
should be followed instead. They are one verb now (`GameBoot.AttackOrFollow`).

Also fixed a compile break 0.35.0 shipped: it added `GameBoot.Follow`/`FollowAsync` that already
existed — two CS0111s. **The Unity scripts are not in `Game.sln`, so `dotnet build` never saw them**
and the APK build would have failed.

## 2026-07-31 — The phone server just runs, and a tap no longer charges (0.35.0)

First batch off playtest-15 — 12 of the 22 §32 items.

**The phone server ships with Workstation GC.** Server GC reserved 256 GiB of regions up front, which
CoreCLR cannot do under proot, so it died before `Main` and had to be hand-edited out of
`runtimeconfig.json` after every deploy. Verified in the published output, not just the csproj.

**Tapping targets first and attacks on the second tap of the same target.** That one line fixes two
separate reports: charging in on the first tap is miserable on a caster, and the same line only ever
sent an `Attack` for a **mob** — so tapping a PLAYER selected them and sent nothing. That was the whole
of *"cannot kill party members even with pvp on"*; it was never a party rule, and the server had been
policing PvP correctly all along. (Follow-up the same day: `CanPvpHit` now refuses **same-party
outright** — opt-in irrelevant, a red party member irrelevant — because in a mass fight a mis-tap on
the ally beside you would quietly make you the enemy's best asset. The second tap follows them instead.)

**Auto-farm retaliates**: a mob already swinging at you outranks whatever is merely nearest, guarded so
it finishes a nearly-dead target and does not thrash between two attackers. It also stops walking
casters into melee, and finally tells the client what it is fighting, so the target window follows the
autopilot instead of sitting empty.

**The class change applies without a relog.** The client's `ActiveClass` — both the label and the Skills
window's Learn gate — is set only by the `Subclasses` push; the debug change, the subclass swap and the
reset all sent it, and the real quest-gated change was the one path that did not.

Also: the healing potion's share of the guaranteed drop group falls 50% → 30% with no stacking (the
group still fires every kill); Wind Walk leaves the nuker and Battle Fury the rogue (both `SkillDef`s
stay — clerics and five 3rd-class disciplines still grant them); the training tier authors its M.Atk
column like the rest of the ladder; buff potions and the Return/Resurrection scrolls sell at /25 like
gear, having previously been unsellable outright. SmokeTest: **ALL CHECKS PASSED**.

## 2026-07-30 — The bar counts your reuse down, and a passive states its numbers (0.34.3)

Playtest-14 batch 4 — the last two items in that queue, both client-side.

**Skill cooldowns.** The client knew nothing about reuse: the server tracked `SkillCooldowns` /
`PotionCooldowns` and told nobody, so the only way to learn a skill was still cooling was to tap it and
read the refusal. A new `Cooldowns` push is keyed by the **action-bar token** — a skill id, or
`item:defId` for a drink timer — so the client matches it against the bar it already holds and needs no
second mapping. Sent when a timer **starts** (cast completion, ESC-cancel, consumable use) and once on
entering the world; **not per tick** — the client counts down locally, so the overlay animates at frame
rate for one message per cast. No "total" on the wire either: the push happens the tick the timer
starts, so the first `Seconds` seen for a token IS the full reuse, replaced only when it comes back
higher. The square darkens and the dark part drains from the top with the seconds left in the middle
(it resizes rather than using a filled `Image`, because a filled `Image` needs a sprite and every box in
this UI is spriteless). A consumable has two reuse channels — the drink timer and the skill the item
grants — and the bar only holds the item token, so `ReuseOf` resolves `UseSkillId` as a fallback or a
Return scroll would look ready when it isn't.

**Passives state their numbers.** A passive showed its authored prose and nothing else — you could read
*"toughens your hide"* and still not know what an SP bought. The numbers were on the def the whole time;
nothing formatted them. New `Game.Shared/SkillText.cs` renders `PassiveEffect` (all ~60 fields),
`StatMods`, armor/weapon mastery profiles (broken down per weight and per weapon type) and buff
magnitudes as `Label +12%` lines, **level-aware**. Unity's skill detail and the Learn confirmation both
use it (Learn shows *"Now …"* above *"After …"* for an upgrade), and **the WPF harness delegates to the
same helper** instead of its own partial copy — 17 fields, level-1 only, masteries missing entirely — so
the two clients cannot disagree about what a passive is worth.

## 2026-07-30 — The rate is TWO knobs: a global one and one per drop group (0.34.2)

Corrects 0.34.1's reading of the rate table. **The authored numbers are the ×1 design, not what the
server hands out** — 5% authored is 5% at ×1 and 15% at ×3. So `DropChanceRate` goes back to 3, where it
always was, and every entry the owner did not specify goes back to its authored value with it.

But one global rate cannot be the whole story, and this is the owner's point: **the guaranteed groups are
authored as absolutes.** Mats 100%, always 100%, scrolls 70%. Multiplying those by a server rate cannot
make them more generous — it pins them at the clamp and throws away every weight inside the group. He
wants them to stay put *"at x10 or x200"*, and gear tunable independently: *"drop chance x200 and armor
group multiplier x0.01, in reality armor will be x2 drops"*.

So `RateConfig.DropGroupRates` — a multiplier per group (armor, accessory, weapon, jewel, mats, scrolls,
always, other) — composed with the global in exactly **one** place, `MobCatalog.EffectiveRate(groupId)`,
returning `(guaranteed ? 1 : DropChanceRate) × the group's own multiplier`. The kill roll, target-inspect
and BalanceMatrix all call it. That is deliberate: the one bug this system exists to prevent is the
number on screen drifting from the number you get, and three call sites each doing their own arithmetic
is how that happens.

Shipped defaults: global ×3, gear groups ×1/3, everything else ×1. The 1/3 is the system working rather
than a fudge — the design reads at ×1, the server runs at ×3, and his acceptance test is absolute
(~400k of trash gold by level 25). Measured: ×3 flat gives 1.08M, ×3 × 1/3 gives **402k**.

**`/droprate` makes it live.** No args lists the table; `/droprate <group> <x>` sets one; `/droprate gear
<x>` sets all four equipment groups; `/droprate global <x>` sets the server rate. A chat command and not
a tuning-panel row on purpose: the panel's payload is a wire DTO, so eight new fields there would bump
the protocol and demand a matching Unity build — for a knob whose entire value is being adjustable
mid-playtest, on the phone, without rebuilding anything.

## 2026-07-30 — The drop side lands, and the faucet closes on his number (0.34.1)

Playtest-14 batch 2 finished: §2 (rates) and §3 (grade lock + groups) were always one edit.

**Four gear groups, each grade-locked to the mob's single tier and randomised across its whole slot
family** — Armor (heavy/light/robe), Accessories (helm/gloves/boots/shield), Weapons (all 8 lines),
Jewels. C 5 / U 2 / R 0.2 / E 0.01 per group. *Where you farm no longer decides what you can loot* —
undead can drop a bow. Mats keep their family flavour, deliberately.

The group **engine** already existed and needed nothing: a group rolls once at the summed member chance
then picks one weighted, so a member's authored chance IS its marginal chance and the owner's table could
be written straight in. The one new idea is the **group id** — `10 + family*10 + rarity`, one group per
rarity **rung** — which is what lets the boss row (E 70 + L 40 + M 2 = 112%) pay several pieces while each
rung still randomises across the family.

Elite and boss **replace** the gear half at kill time, in `RollDrop`: rank is a property of the spawn, not
of the template, so it cannot live in a baked table. Mats are one stack per kill with the roll AS the
amount (50→1, 40→2, 9→4, 1→10). Scrolls 70% (half enchant, half buff potion). Always 100% (healing potion
/ return / resurrect). Broken jewels leave the drop tables — the F Commons are that line now — and stay
on the vendor's shelf.

**Measured, not derived.** `tools/BalanceMatrix` grows an ECONOMY section that resolves the real tables
with the real group math and the real vendor prices: **403k of trash gold by level 25** against his stated
~400k target, over ~168 kills at the live ×10 exp rate. Anchor: it prices the E Common gauntlet at sell
4,500, identical to the 0.33.3 measurement, so only the drop arithmetic is new. 0 unresolved ids across
7,886 entries.

Two things the code found that hand-reading did not: `MobCatalog.All = Build()` is declared first, so any
`static readonly` table below it is null when `Build()` runs (the rate tables had to become properties —
same trap as `ItemCatalog.DropTiers`); and *"below level 74 also drop a recipe at 0.1%"* **cannot be
built** — no recipe item exists under A grade, because recipes below 76 are learned by level rather than
found. Flagged, not faked.

**Target-inspect collapses each group to one line.** Not cosmetic: a mob carries ~97 entries now, and 97
near-identical 0.6% rows told the player nothing. One line per group is also the truthful reading — the 5%
really is one shared roll.

## 2026-07-30 — A quest mob respawns as ITSELF, and F gear finally drops (0.34.0)

**Per-mob spawners** (playtest-14 batch 3). A camp's mixed roster meant killing a werewolf was a 1-in-5
chance of getting a werewolf back, so farming a quest mob meant clearing the camp and waiting, and any
one creature's population drifted with the dice.

A zone now carries `DedicatedSpawn[]` on **top** of its mixed pool (owner: *"a self spawner that is on top
of the one they are in right now"*): a fixed count of ONE template whose deaths respawn that same
template. Which templates qualify is **derived from the quest catalogue** — `QuestCatalog.KillTargets`
collects every `KillMobs` step's target and merges it to the widest band any step accepts — so a new kill
quest gets its guaranteed population for free and a hand-list cannot rot. A camp qualifies only if it can
spawn the creature at a level the step will **count**. Elites/bosses are excluded (2-mob camps by design).
Result: 6 camps, 41 guaranteed mobs, biggest camp 11 → 20. `WorldPlan.UnservedKillTargets()` is logged at
startup: a misspelt `TargetId`, or a band that no longer overlaps its camp, is otherwise invisible until a
player takes the quest and cannot finish it.

The two population kinds are tracked **separately** in `ZoneRuntime`, and a spawn **records** which spawner
made it (`Entity.SpawnerMobId`) rather than inferring it from the template: a mixed roll can legitimately
produce a dedicated template, and crediting that death to the wrong bucket is exactly what would let the
guaranteed population drift back to the dice.

**F-grade gear drops.** `GearTier()` floored every level below 40 to the level-20 (E) tier, which is the
only reason gear drops were gated away from mobs under 18. F is part of the one ladder now, so the floor
becomes the F tier and the gate is gone: levels 1-19 no longer have mats-and-nothing as their whole loot
table. Rarity gates on **mob level** instead — Common from 1, Uncommon from 5, Rare from 10. 1087 drop
entries, 0 unresolved ids.

**Training armor re-cut, so early drops are never a downgrade.** Measured F-Common against the starter kit:
weapons were fine (1.7-2.0× up) but the **armor** was 0.72× — the first body you could loot was *worse than
what you start in*. The training armor was the sum of an upper + lower body from the TOP of the no-grade
range, while F Common is 45% of a MID no-grade set. Fixed on the **starter** side (light 53 → 35 P.Def,
robe 27 → 20, MP unchanged) so the ladder keeps its one rule: every quality is a fixed fraction of the
authored Mythic piece.

⚠ Also recorded: the old `"Worn"`/`"Steel"` item line under `LootTables` in `Items.cs` is **dead code**,
referenced from nowhere. The live path has always been `MobCatalog.StandardDrops` on the tiered ids.

## 2026-07-30 — The price ladder is one series, and gear stops paying for itself (0.33.3)

Playtest-14's headline was **level 25 with 3kk of gold from selling trash**. This is the PRICE half of the
fix; the drop half is 0.34.1.

**Sell price derives from buy**: tiered gear pays `buy / GearSellDivisor` (25). Not a knob picked from thin
air — the owner's acceptance test is *"~25 Robes buys one Leathers"*, and both are the Armor slot at the
same grade+rarity so they share a buy price, which makes the divisor exactly that ratio. Measured: 25.0.
The cut is confined to **gear**; mats/potions/scrolls keep `VendorSellFraction`, because they are not what
made a level-25 character rich and cutting them would nerf crafting income nobody asked to nerf.

**Rarity scales gold at HALF the power ratio** (22.5/27.5/35/35/42.5/100 % against power's
45/55/70/70/85/100), so rarity is worth less in gold than in stats. Mythic is the 100% base — a 2.35×
jump over Legendary, intended: Mythic is craft-only and meant to be traded between players for absurd sums.

**The table grows from 3 grades to 7.** It had been capping at the D column for every level ≥ 40, so a
B-grade item was priced as if it were D; that is why the top grades get dearer here, and it is what makes
*"gold farming stays meaningful only at the top grades"* true. The table is expressed in Mythic, but the
F/E/D cells are written as `Shop(x)` with `x` the owner's shop price — because the shop sells **Rare** only
at F-D, those numbers are the fixed points of the whole table; `Shop()` lifts them to the Mythic rung so
multiplying back down returns them exactly (all 35 verified identical). C..S hold the 2H weapon column's
slot fractions (set 45/25/15/15, 2H = 75% of a set, 1H = 90% of 2H, jewels 1/12, 1/6, 1/2) — the fractions
F/E/D already satisfy, so retuning a grade is one number, not eight.

Measured, E Common trash: **sells 18.4k → 4.5k**. Two consequences worth knowing: Common gear costs 1.84×
MORE at the vendor now, and B+ low-rarity gear sells for more than before.

⚠ **The 16× is BOTH levers** — sell (4.1×) × drop rate (4×) — not the sell side alone, which is easy to
double-count and read as 68×. So 3kk of trash gold at level 25 lands at ~184k. The owner names **~400k** as
the good number (a 7.5× cut), so the plan overshoots by ~2.2×; deliberately not tuned here, because the
grade lock and the mutually-exclusive groups move the figure again and the gap gets closed against a real
measurement (it was: 402k, 0.34.2).

## 2026-07-30 — Builds by git branch: built, then reverted (no version)

`tools/publish-build.ps1` published the APK + server zip to an **orphan `builds` branch** that was
rewritten rather than appended to — one parentless commit per publish holding the newest 3 versions,
force-pushed, so the blobs it dropped became unreachable and the branch stayed one generation of builds in
size. (At ~41 MB APK + ~15 MB zip per release and four releases in a day, committing `builds/` onto the
working branch would have added more permanent history in a week than the entire source repo has in months.)

**Reverted the same day, on the owner's call**: he remotes into the PC and takes the artifacts from
`builds/` directly, so nothing has to reach him through git at all. The branch was deleted from the remote.
Local `builds/` stays gitignored. ⚠ **Don't rebuild this** — dead weight in a repo gets rediscovered later
and half-believed.

## 2026-07-30 — Abandon actually abandons; char-select stops lying; the kill line (0.33.2)

Playtest-14's two "not working" items and one of its asks. Protocol is **unchanged (8)** — all three
ride existing messages.

- **The Abandon button did nothing but show its confirmation.** `GameBoot.QuestAction` bailed out on
  `DialogNpcId == Guid.Empty`, and Abandon is pressed from the QUEST LOG, where no dialog is open — so
  the call was never sent. The guard is right for accept / complete / change-class (the server re-checks
  you are standing in front of that specific NPC) and wrong for abandon, whose server handler never reads
  the npc id at all. Now exempted by name.

- **Char-select showed a stale level and class for one round trip.** `LeaveWorld` flipped the phase to
  `CharacterSelect` and refreshed the list *after*, so the screen came up holding the array captured at
  LOGIN. The server was never at fault — `GameHub.LeaveWorld` has awaited the character SAVE since
  playtest-13, so the row on disk was already correct. The fault was drawing before asking. It now fetches
  the list first and switches screens in one step; a failed fetch falls back to the old array and switches
  anyway rather than stranding the player in a world that is about to be cleared.

- **The kill line** — `Exp: +eee, SP: +sss, Gold: +ggg`, one per kill, per player. Exp/SP and gold are
  banked by two unrelated paths (`AwardKillExp`→`AwardExp`, `RollDrop`→`AwardGold`), each already looping
  over the in-range party members, so letting either announce its own share would print two lines per
  member on a party kill, interleaved with the loot lines. A kill opens a tally, both paths add into it,
  and one line per recipient is flushed after both — after the loot lines, so it reads as the kill's
  closing line. The tally is null outside a kill, so quest exp can't feed it. SP is reported as what was
  actually **banked**, not what was computed: those differ at the `int.MaxValue` saturation ceiling, and a
  line claiming SP you did not receive is worse than no line.

**Also confirmed, no code needed:** playtest-14 asked to make `/givegold` and the admin commands work on
the phone. 0.33.1 already did it — `DebugGoldCmd` is an `IAdminCommand` gated at runtime by role, and the
only `#if DEBUG` left in the server is account seeding and the destructive schema reset. The report was
against the installed 0.30.1 APK, which predates the fix. The APK rebuild is what proves it.

## 2026-07-30 — The debug menu is an ADMIN menu: it works in release builds now (0.33.1)

*"The server deploy on the phone made the #debug sections not working (you published it in release rather
than debug). Can we make the debug menu into an admin menu, each debug command into an admin command,
leaving the server gate to check isAdmin."*

Fifteen hub methods were wrapped in `#if DEBUG`, so the **release** server published to the phone accepted
every one of those calls and did nothing: the window opened, the buttons pressed, and pressing them was
silence. A compile flag was the wrong gate anyway — the question was never "is this a debug build" but
"is this character an admin", which the server already tracks at runtime.

- **`IAdminCommand`** is a new marker on all fifteen commands (+ the two tuning-panel ones). `ProcessCommands`
  checks it **once**, centrally, before dispatch — a per-handler check would be fifteen places to forget,
  and forgetting one in a shipped build hands a player free levels. A non-admin is **told**
  ("That is an admin-only command") and logged, not silently ignored: silence is what cost this an
  afternoon in the first place.
- The hub methods keep their `Debug*` **wire names** deliberately. Renaming ~50 string literals across two
  clients and two tools buys no behaviour and fails *silently* when one is missed — a fire-and-forget
  SignalR call to a method that no longer exists does exactly what the `#if DEBUG` bug did.
- **The menu entry is "Admin" and it COLLAPSES.** The overflow menu was laid out with fixed offsets, so
  hiding the entry left a 52px hole between Setup and Leave and an over-tall panel. It re-stacks the visible
  buttons and shrinks the panel to fit (owner: *"don't leave a gap between the buttons, collapse it"*). The
  window's title bar says **Admin** too.
- New `Boot.CanUseAdminTools` (`Role == Admin`) drives the toolbox, deliberately narrower than
  `Boot.IsAdmin` (Admin **or** Moderator), which still drives the moderation commands. A moderator was
  being shown a menu whose every button would answer "that is an admin-only command".
- **Character delete** got the same treatment: the 10-second undo window is now for ADMIN characters as well
  as debug builds. A `#if DEBUG` convenience is no convenience on the release server where the testing
  actually happens.
- Still `#if DEBUG`, correctly: the **admin/test account seeding** and the **destructive stale-schema DB
  reset**. Neither has an admin to authorise it — they run before anyone is logged in. Registration is
  unchanged (open in every build), and the existing bootstrap still applies: the **first character of the
  first account on a fresh server is born Admin**, which is how the phone gets a usable admin at all.

**Verified on a real release deployment**, not just by reading: `dotnet publish -c Release` to a clean
folder, empty database, `dotnet Game.Server.dll`, then the bot registered an account, was told "Admin
privileges active", and got `+10,000,000 Gold` and `reached level 6`. The bot now **self-registers** when
login fails, which is what makes it usable against a freshly published server (the seeding it used to rely
on is debug-only).

**SmokeTest restructured** around the new gate, and it got better for it: the protagonist is promoted to
Admin (it leans on the toolbox for levels, items, subclasses, professions), and a **second plain character**
is created as the moderation victim — an admin can be neither jailed nor demoted, both by design. Two new
assertions: a non-admin is refused *and told*, and an **admin character is kept off the leaderboards** —
which is the answer to the owner's playtest-13 puzzle, *"my ranking board was never updated … aaa, my chars
are admins"*.

## 2026-07-30 — The overworld is a PLAN: 4-level camps, named gates, managing cities (0.33.0, protocol 8)

*"How exactly am I supposed to kill a pig next to a werewolf? I wanted spawners to be close to level, not
to coordinates."* A mob with a natural level brings its **own** level — the zone's band is only a hint — so
the hand-listed 1-12 starter roster spawned Ridgeback Pups at 1 and Werewolves at 12 in the same circle.
The world is now generated from an authored **plan** (`Game.Shared/WorldPlan.cs`), and the roster is chosen
BY the band, which makes that impossible rather than merely discouraged.

**The layout**

- **4-level camps** (2 at the top), exactly the owner's `1-4, 4-8, 8-12 … 88-89, 90`. 27 normal camps + 3
  elite camps replace the old 13 wide zones.
- **Fields group camps under a city**: Brackenford 1-16 (2 fields), Stonewatch 16-40 (3), Greymarsh 40-60
  (3), Ironreach 60-75 (3), Frostmere 76-90 (3, each with an elite camp at 80 / 84 / 90).
- A field sits on a **bearing** from its city at a fixed distance, its camps marching along the arc — so
  the whole field is one walk out, and levels step sideways rather than deeper. (Marching outward would put
  the top camp ~6000 further out; with cities 13-15k apart that runs one city's fields into the next.)
- **Camps are 1000 apart rim-to-rim** (past the 400 aggro range), **fields clear the town wall by 1500**
  (*"the fields not to be exactly next to the city"*), and each **elite camp sits 1500 out from its
  field's top camp** — rims 450 apart, so you can clear the normal camp to its edge without waking it.
- Rosters, respawn cadence (8s → 32s by level) and the aggression ramp (0 types below 13, 1 to 40, 2 to 75,
  3 at the endgame) are all **derived**. Aggression is still authorable per camp — a count, or an explicit
  list of ids.
- The lone hand-placed level-78 emberwyrm elite is gone; every Frostmere field generates its own.

**Named teleport gates, and a managing city**

- Every camp has a **named gate** on its town-facing rim: `Bracken Downs North — Lv 8-12 · Goblin Scout,
  Ashen Wolf, Werewolf`. `Region.ArrivalPoints` (pick one at random) became `Region.Gates` — a
  `TeleportPoint` with an id, a name and a description. Arriving in the middle of a level-90 camp was a
  death, not a journey.
- A **gatekeeper lists its own city's field gates first**, grouped by field, then the roads to the other
  cities. `TeleportDest.ZoneId` → `DestId`, which now carries either a city id or a gate id (**protocol 8**),
  plus `Description` and `Group`. A gate belonging to another city is refused — otherwise a gate id would be
  a free warp anywhere on the map.
- Every field records its **managing city**, and **death sends you there**, with nearest-town kept as the
  failsafe for the places no city manages (open ground, the boss vale, a dungeon). Nearest-town alone was
  wrong in the case that matters: fields reach ~7k and cities are 13-15k apart, so dying on a field's far
  edge could wake you in a city whose gatekeeper cannot even send you back.

**Structure**

- `Towns.cs` is new and holds the safe zones, purely to break a static-init **cycle**: WorldMap's spawn
  zones are generated from WorldPlan, which needs the city centres. `WorldMap.SafeZones` forwards, so no
  call site changed. `RegionMap.Towns` now derives from the same list instead of re-listing all seven.
- **Two startup guards** (`WorldPlan.ValidateLayout` / `ValidateLevelCoverage`) fail the boot on: a roster
  member outside its band, an empty roster, camps too close, an elite inside aggro range or too far, a camp
  inside the town gap, overlapping fields, and any level 1-90 with nowhere to earn it. The first run caught
  four real clearance failures around Brackenford. A bearing is not a picture — none of this is visible in
  the source.
- 14 new SmokeTest assertions on the layout, plus an end-to-end gatekeeper test: talk, read the menu,
  travel to a named gate, land on it (79 units), get charged, and be refused a foreign city's gate.
- The startup region report now prints each field's managing city and its gates with descriptions.

## 2026-07-30 — NPCs stand on a diagonal; the quest !/? is twice the size (0.32.3)

*"Make the NPCs that are on the same line (y level) and next to each other a bit diagonal. Now their
names are overlapping and hiding their quest signs. Also double the size of the quest ! and ?, they are
just too small."* Two NPCs at the same Y draw their name plates at the same screen height, and one long
name then paints over the neighbour's plate — including the `!` you were scanning the town for. So the
marker was both hidden *and* too small to read when it wasn't.

- **Every town cluster is a diagonal staircase**, ~300-450 across and ~300 down per step, so no two
  neighbouring NPCs share a screen line. Brackenford's vendor row (Apothecary / Armsmaster / Outfitter /
  Keeper) and the generated ring-town clusters both re-laid out; the ring towns' gatekeepers moved 900
  above their town centre (they were standing *on* the centre point, on the same line as the generated
  armsmaster), and Greymarsh's Grandmaster moved to 1200 west, clear of the Outfitter.
- **`WorldMap.ValidateNpcLabels()` is a startup guard** — any two NPCs within 1500 on X and under 200
  apart on Y fail the boot, naming both and their coordinates. Layouts drift as NPCs are added and the
  failure is invisible in code, visible only on a phone screen. It caught five real collisions the moment
  it was written, all of them in the hand-placed gatekeeper block.
- **The quest glyph is drawn at 200% and bold.** `line-height=100%` pins the plate's line box to the
  *name's* height so the bigger glyph doesn't shove the name down, and nameplate labels no longer word-wrap
  — a 200-wide plate plus a 30px glyph would otherwise wrap a long-named NPC's `!` onto its own line,
  which is the overlap this whole entry is about.

## 2026-07-30 — The confirm dialog grows to fit its message (0.32.2)

*"The vendor details are good, just coming out of the confirm dialogue."* The dialog was a fixed
**520×200** panel with an **80px** text box — fine for "Sell 3 x Potion?", and it broke the moment the
vendor confirmation started carrying the item's full stat block: the text ran straight out through the
bottom of the panel, past the buttons.

- **`Ask()` now measures the message** with TMP's own `GetPreferredValues` and sizes the panel to it, so
  the dialog fits whatever it is given instead of every caller guessing a height. The cap is on the
  TEXT (56-460px), never the panel, so the button row always keeps its space and text can never overlap
  it. Clamped to stay on a phone screen.
- `overflowMode = Truncate` as a backstop: if a message ever exceeds the cap it is clipped inside its
  own rect rather than drawn over the buttons.
- The vendor's stat block is set at **15px** against the question's 19px — the question is the decision,
  the stats are evidence for it — and the redundant `Name:` row is dropped, since the name is already in
  the question line above.

⚠ Unity-side, so NOT compile-verified — the owner is holding APK builds until the fix batch is done.

## 2026-07-30 — Rarity colour in every item list (0.32.1)

The **bag** was the one list still painting every row the same grey — the list you look at most, and
the one where it matters most now that a piece exists at six qualities under ONE name. Two rows of
"Electrum Blade" were indistinguishable.

Coloured now: **bag rows** (`RefreshBag`), **trade offers**, **buy-back rows**, and **box selection
options**. Together with the vendor, warehouse, item details and worn squares from 0.29.1, every place
an item name is drawn carries its quality.

- An **equipped** bag row stays green. "This is what you are wearing" is the more urgent fact while
  scanning a bag, and the `*` prefix alone is easy to miss.
- Trade and box-selection get it deliberately: both are commitments made without inspecting the item,
  and box selection is irreversible.

🔴 **Fixed a conflict I introduced in 0.29.1.** TMP's `<color>` markup **overrides the label's own
colour** for that span, so colouring a vendor row's name cancelled the dimming that means "you can't
afford this" — the quality cue was quietly killing the affordability cue. The vendor and buy-back lists
now colour the name only when affordable, and leave it dim when not.

✅ Unity-compile-verified (headless APK build, zero `error CS`) — which also covers every client edit
since 0.30.1: quest markers, the tracker, the vendor detail view and the clipped-window fixes.

## 2026-07-30 — A set needs FOUR pieces of the same quality (0.32.0) — ⚠ DELETE `game.db`

The owner asked whether a Mythic set could be completed by an Epic helm, Legendary gloves and Epic
boots. **It could** — and it paid the full Mythic bonus.

Every Epic/Legendary/Mythic copy of a piece carried the *same* set id as the authored one, and all
accessories shared one line, so **mixing was strictly better than matching**: the quality of your
accessories didn't matter at all as long as they were Epic or above. (A Common/Uncommon/Rare piece has
no set id, so his "common helmet" would in fact have *broken* the set — the only part of the old
behaviour that was right.)

Now **each quality has its own set, and its own scaled bonus**:
- Item copies take a quality-suffixed set id (`set_light_t20_epic`, `…_legendary`); the authored piece
  is the Mythic rung and keeps the plain id.
- **The accessory line is quality-matched too** — otherwise a Mythic body would still have been
  completed by Epic accessories through the shared accessory id, which was half the original hole.
- Each authored set now generates Epic (70%) and Legendary (85%) variants via `StatMods.Scaled` and
  `ClassFlatBonus.Scaled`, including the shield-conditional extra. Measured: heavy T20's set HP goes
  **135 → 94.5** at Epic.
- Below Epic there is still no set at all.

Scaling every field uniformly is deliberate: choosing which fields shrink is a per-set design decision,
and this keeps ONE authored set as the single source of truth for its whole quality column.

**This also closes the "scaled set bonuses" gap** that had been open since 0.29.1 — and with it the S
grade's, whose `set_*_t80` ids now resolve like any other.

New SmokeTest assertions: Epic/Legendary/Mythic bodies do not share a set id, sub-Epic has none, the
Epic set's bonus (and shield bonus) is scaled below Mythic's, an Epic body's set demands Epic
accessories, and a Mythic helm does not satisfy it.
⚠ One of those assertions was wrong first time — it compared `Mods.MaxHp` on the LIGHT set, which
doesn't use that field, so it was comparing 0 against 0 and passing for the wrong reason. It asserts on
the heavy set, which actually carries HP.

## 2026-07-30 — The F sets exist; wire them to the F tier (0.31.3)

Correcting 0.31.2. I dropped the F-tier `SetId`s on the grounds that `ArmorSetCatalog` had no F set —
it did: the **Newbie sets** WERE the F sets (owner). Light = **+42 Max HP, +2% P.Def**; Robe =
**+15% cast speed**.

When the newbie kit became the F-grade top, those set ids needed to follow it onto the tiered pieces.
They didn't, so the bonuses were left **orphaned** — defined, but attached to items that no longer
exist. Renamed to the ids the tier generator actually emits (`set_light_t1`, `set_robe_t1`,
`set_acc_t1`) and the SetIds restored.

- **Heavy at F is new.** The newbie kit was fighter-light / mage-robe, so heavy had no set — but the F
  tier does have a heavy body, so a tank in F would complete nothing. It mirrors light (same numbers,
  the defensive line). One line to change if heavy should differ.
- Light's P.Def stays at the existing **2%**, not the 5% first mentioned — owner confirmed 2%.
- The old `set_newbie_*` ids are kept but unreferenced, so an old save resolves to a name.

**New SmokeTest assertions check the JOIN, not just that both halves exist.** A set is bound to its
pieces by an id string and nothing else, so a mismatch is a bonus that silently never applies — which
is exactly the bug 0.31.2 introduced. It now asserts each F body's `SetId` resolves to a definition
AND that the definition's accessory line matches the id the F accessories carry.

⚠ **S grade (level 80) still has this gap** — its bodies name `set_*_t80` with no definition, so that
bonus does nothing. That remains the open "scaled set bonuses" item.

## 2026-07-30 — F grade joins the ladder; "(Lesser)" is gone (0.31.2) — ⚠ DELETE `game.db`

Two owner calls, and they turn out to be the same change.

**The "(Lesser)" gear line is deleted.** *"We should have lesser items no longer — they've become the
common ones."* It was a parallel item set at the same levels as the real ladder, flagged `Epic` and
priced off the same table, so the shop was listing **Epic-priced Lesser gear beside the ladder's own
Common/Uncommon/Rare** — which is the wrong-price bug. One ladder per slot per grade now, and the low
QUALITIES are what "cheap gear" means.

**F grade is now part of that one ladder** (`ItemLevel 1`, themed "Ferrite" by `GradeTheme`). This is
what made deleting the Lesser line possible — it had been the only source of F gear, which is why the
deletion was deferred twice.

**The newbie kit IS the F-grade top.** *"Make the newbie gear the Ferrite Mythic — it's the top for
its grade."* The `Newbie*` ids are now aliases onto the F tier's **Mythic** rung, and the F tier's
Mythic numbers were authored FROM the old newbie stats — so **nothing got stronger or weaker in the
swap**; there is simply one item where there were two. `Newbie Sword` → **`Ferrite Blade`**, which is
the point: a real rung on the ladder, not a tutorial prop.

- F carries the full six qualities, so the shop still has something cheap (F-Common) and the level-10
  quest hands out the best F there is (F-Mythic).
- **No SetId at F.** Set bonuses start at E and `ArmorSetCatalog` has no F set; an item advertising a
  set that cannot exist is worse than one that plainly has none.
- ⚠ The same gap exists at **S** (level 80): its bodies carry a `SetId` with no matching
  `ArmorSetDef`, so the bonus silently does nothing. That is the still-open "scaled set bonuses" item.

New SmokeTest assertions: no `(Lesser)` gear exists, the newbie weapon is F-grade Mythic and themed
Ferrite, and F has its low rungs.

## 2026-07-30 — EF cartesian-product warning fixed (0.31.1)

The `Microsoft.EntityFrameworkCore.Query[20504]` warning on startup and on every login. Four queries
loaded `Items` → `Attributes` **and** `Subclasses` in one statement, which EF resolves as a single
JOIN — so the row count is (items × attributes) × subclasses and **every row drags a full copy of the
character**. A geared character with a stocked warehouse turns a ~50-row read into hundreds.

All four now use **`AsSplitQuery()`**: three round trips against a local SQLite file cost far less
than that multiplication. The login path (`LoadCharacterAsync`) is the one that matters — it runs for
every player entering the world. Verified: startup + a full SmokeTest login cycle now logs **zero**
EF warnings of any kind.

Fixed per-query rather than globally via `UseQuerySplittingBehavior`, deliberately: a global switch
would silently change every query in the app, including ones nobody has looked at, and the next
multi-collection query *should* raise the warning so someone decides about it.

**Also fixed a flaky SmokeTest assertion.** The charisma-board check ("a jail drained the player's
charisma") read the leaderboard once, but that board comes from the DATABASE and the drain reaches it
via a fire-and-forget background save — so the read raced the write and failed about one run in four
while the code was perfectly correct. It now polls for up to ~3s. A flaky assertion is as misleading
as a non-idempotent one: it trains you to re-run instead of to look.

## 2026-07-29 — S grade, and the ladder re-anchored to the top (0.31.0) — ⚠ DELETE `game.db`

The owner's reading of the ladder: **our A-grade is L2's LOW S-grade**, so A at full power is already
about right for level 85 — the +43% Mythic sitting above it was inflation, not content.

**The authored tier tables are now the MYTHIC piece** (100%), not the Epic (70%) anchor. Every lesser
quality is a fraction of the authored number instead of a multiple. Measured with `BalanceMatrix`:
**every existing stat is unchanged** — the only thing that disappeared is the phantom rung that used
to sit 43% above anything the game had been balanced for.

**New S grade** (`ItemLevel 80`, "Soulcrystal"), for levels 80+:
- **Derived from A × `SGradeOverA` (1.60)**, not hand-authored — one constant retunes the whole grade
  (owner: *"not so much authoring"*). A 1H blade: A **281** → S **450**.
- **Top half of the ladder only — Epic / Legendary / Mythic.** Below Epic a piece carries no set bonus
  and no attributes, which is not what endgame gear is for. More importantly **crafting produces
  LEGENDARY ONLY**, so an S grade without a Legendary rung could never be crafted at all and the
  blueprint economy would dead-end at A.
- A keeps its full six rungs; only S is top-half.

⚠ **This broke crafting and the SmokeTest caught it.** `FinishedItemRecipes` identified "the real
item" by `Rarity == Epic`. Re-anchoring made that match nothing, so **zero** craftable recipes were
generated — silently, with no error. The filter now keys on `Mythic`, and the same applies to
`RecipeBooks`. Worth remembering: several places use a rarity as a *proxy for "the authored piece"*.

**Where level 85 lands, in S gear** (was A gear):
| | M.Atk / P.Atk | kills a same-level mob in |
|---|---|---|
| Mage | 1511 → **2039** | 3.8 → **3.3 casts** |
| Fighter | 1100 → **1738** | 24.6 → **15.5 hits** |

S closes some of the fighter's gap, but **24.6 → 15.5 hits vs the mage's 3.3 casts is still a wide
gulf** — a pre-existing curve problem the ladder inherits rather than causes, and worth its own pass.

`tools/BalanceMatrix` also **had to be repaired to run at all**: it sat on `net8.0` after the server
moved to `net10.0`, and being outside `Game.sln` nothing caught it. It now takes a gear QUALITY and
knows about the S tier.

## 2026-07-29 — Which mobs are aggressive is AUTHORED per field (0.30.1)

0.29.2 made exactly one mob type aggressive per ordinary field — the roster's FIRST entry. The owner
pointed out the obvious limit: *"a zone where I want more than one, or 3 out of 5, now I cannot do."*
Positional means exactly one, always.

`SpawnZone.AggressiveTypes` replaces it:
- **`null`** (default) — the first roster entry, so a new zone is never accidentally wall-to-wall aggro.
- **a list** — exactly those types, however many.
- **an EMPTY list** — nothing here attacks on sight: a genuinely peaceful hunting field.

Every field now states its own answer, and the danger ramps deliberately instead of falling out of
list order: the **first Brackenford field is peaceful** (nothing should jump a level-3 character), the
second has one, the mid bands have two, and the Frostmere endgame fields have three.

**New startup guard**: an `AggressiveTypes` entry that names a mob the zone does not spawn now throws,
listing the zone and the bad id. That typo fails in the worst possible direction — the field silently
turns peaceful, which reads as a design choice rather than a mistake, and nobody notices until a
playtest says "nothing attacks me here". Verified by deliberately breaking one entry and watching the
server refuse to start.

## 2026-07-29 — The world is five cities (0.30.0) — ⚠ DELETE `game.db`

The world re-layout from playtest-13. Seven towns in a ring, each with two wide bands, becomes **five
cities** each owning a level range and holding 2-4 **tighter fields** — 6-level bands meant half of
every band was spent farming grey mobs or being outclassed.

| City | Band | Fields |
|---|---|---|
| Brackenford | 1-16 | 2 |
| Stonewatch | 16-40 | 4 |
| Greymarsh | 40-60 | 4 |
| Ironreach | 60-75 | 3 |
| Frostmere | **76-90** | 3 + three ELITE spawners (80 / 84 / 90) |

- **Emberfall and Duskvale are deleted** — towns, NPCs, roads, regions and safe zones. Their rosters
  were redistributed into the bands above; the level ladder is unbroken because the mob roster is a
  dense 1-85 run.
- **There is finally somewhere to reach the cap.** New `SpawnZone.ForceZoneLevel` makes the ZONE's
  band win over a named mob's own level, and the 85-90 field uses it so the top roster respawns at
  86-90 (owner: *"make it so we can have a place to lvl up from 86 to 90"*). Purpose-built creatures
  for that band come later — this is a deliberate reuse, not a fallback.
- **Each Frostmere field carries an elite spawner ~1200 away**: same trip, but far enough that the
  elite does not aggro while you clear the normal camp (owner asked for 1-1.5k).
- **Field outlines are GENERATED, not hand-drawn.** This is what made the re-layout possible at all.
  Each field used to be a dozen literal polygon vertices that had to keep agreeing with the circles
  inside it, enforced by a startup guard that refuses to boot on a "rogue spawner" — move a zone 500
  units and the server dies. `RegionMap.FieldOf` now builds the outline as a convex hull of the zone
  circles plus a margin, and `ZonesNear` picks a field's zones by POSITION, so re-ordering or
  re-banding the list cannot silently reshuffle the map. A field simply IS where its spawners are.

⚠ The generated hulls are larger than the hand-drawn ones, and the first attempt had **Stonewatch
swallowing the training dummies** (6 spawners, Lv 16-60, instead of 4 at 16-40) — caught by the
server's own field-membership report. The 28-34 field moved north and that field's margin tightened.
Verified: every band now reports exactly its own range, no rogue spawners.
✅ SmokeTest green on a fresh DB.

## 2026-07-29 — Every town is a real town (0.29.6)

**Every MAIN town now carries the same service set** (owner): a buffer, a warehouse keeper, the
**three** vendors and a gatekeeper. Before this the six ring towns had only a gatekeeper and a keeper —
no vendor, no buffer — so they were waypoints you teleported out of rather than places you could
resupply in. 24 NPCs → **49**.

- **Generated, not hand-listed** (`WorldMap.RingTownServices`). Six towns × five NPCs is thirty rows
  that all have to agree about their own layout, and the hand-listing had already drifted. Each town
  uses Brackenford's shape scaled to the smaller radius: vendors + keeper clustered EAST as one
  shopping stop, buffer bottom-centre, gatekeeper alone top-centre.
- **Shops are shared by REFERENCE.** A ring town's vendor id is the Brackenford id plus a town suffix
  (`merchant_gear_stonewatch` → `merchant_gear`), and `ShopCatalog` resolves it, so there is ONE stock
  list to edit and a town cannot quietly end up selling last month's catalogue. A town-specific
  override is just its own key.
- **The 3rd-class Grandmaster moved OUT of the starter town** to **Greymarsh** — the first town whose
  band (34-46) reaches the level-40 discipline change. You should not walk back to the newbie town to
  take a level-40 quest. The other 3rd-class quest NPCs belong beside him there, not accumulating in
  Brackenford.
- **Brackenford keeps what you use once**: the class masters and Mindwright Sela. Its vendor ids are
  unchanged, which matters — the starter chain and the Apothecary's daily reference them by id.

New SmokeTest assertion: **every vendor NPC in the world resolves to a stocked shop** (21 vendors, 0
empty). The suffix convention is exactly the kind of thing that silently stops matching and leaves you
with a vendor who greets you and sells nothing.

## 2026-07-29 — Vendor split + shop detail view + Brackenford town layout (0.29.5)

- **The gear trade is split in two** (owner, playtest-13): **Armsmaster Dolan** sells WEAPONS,
  **Outfitter Bryn** sells armor, shields and jewels. One vendor stocking the whole F/E/D ladder at
  three qualities is ~150 rows, and that flat wall is most of what made the shop unreadable.
- **Detail / Compact toggle** on the vendor window. Detail (the default) adds a second line per row
  naming the quality, grade, type and the stat that matters for the slot; Compact is the old one-line
  row for scrolling a long ladder fast. The preference is remembered across vendors.
- **The confirm dialog now carries the item's full stats and description** — the last moment before
  the gold leaves, and the only place with room to say what you are buying. That matters much more now
  that a piece exists at three qualities and the NAME no longer tells you which one you tapped.

**Brackenford is laid out by what you came for** (owner), instead of NPCs scattered around the town:
- **EAST — one shopping stop**: Apothecary, Armsmaster, Outfitter, and **Keeper Bram** moved in with
  them (banking and shopping are the same errand — you sell, you stash, you buy).
- **WEST — quests and class changes**: High Priest Oren, Elder Marius, Class Master Vael, Grandmaster
  Thorne.
- **TOP-CENTRE, alone**: Gatekeeper Pell — the one NPC you walk to from anywhere, so it belongs in
  neither cluster.
- **BOTTOM-CENTRE, alone**: Mindwright Sela. She used to stand 500 units from the Apothecary, where
  the two read as one clump and put a service next to a shop.

Each cluster is ~450 apart: close enough to be one stop, far enough that the name labels do not overlap.

## 2026-07-29 — Quest markers over NPCs + the on-screen tracker (0.29.4) — protocol 7

The two things the owner asked to see and test alongside abandon: *"i would like to see the
notifications and track"*.

- **Quest markers over NPC heads.** New `QuestMarks` push (gold **!** = a quest you can take, gold
  **?** = one you can hand in NOW, grey **?** = one you are on). Availability is PER PLAYER — level,
  race, class and history all decide it — so it is computed server-side and sent **from
  `SendQuestLog`**, which means it is emitted at every point the answer can change without a second
  set of call sites to keep in step. Ready-to-hand-in outranks in-progress outranks available.
- **On-screen quest TRACKER.** A `[Track]` button on every active quest row pins it to a small
  draggable panel that shows the objective and the kill counter while you fight, capped at **5**
  (owner asked for 3-5). Pinning past the cap drops the oldest rather than refusing — a button that
  silently does nothing reads as broken. Pins for quests that end are dropped automatically, and the
  panel hides itself when nothing is pinned.

🔴 **The SmokeTest earned its keep here — it found TWO real push bugs, both the same family** as the
playtest-13 tier-1 ones (server state changes, nothing tells the client):
- **A level-up never re-pushed the quest log.** `AdvanceLevelQuests` only pushes when it changed an
  active quest, so crossing a quest's MinLevel produced no marker until some unrelated quest event
  happened. `OnLevelUp` now pushes unconditionally — quests can now CLOSE on level too, so this
  matters in both directions.
- **A subclass swap never re-pushed it either.** Each class carries its own level, so swapping changes
  what is on offer; the markers kept describing the class you swapped away from. The test caught a
  level-81 main showing no markers at all, because the last push had been computed while a level-5
  subclass was active.

Both were invisible in play — exactly what the headless test exists for. New assertions cover the
markers at login, at level 1 (correctly none — nothing opens before 10) and after levelling.

⚠ **Protocol 7** — new `QuestMarks` push.

## 2026-07-29 — Quests: level ranges, abandon, and the Apothecary's daily (0.29.3)

- **Quests have a level RANGE, not just a floor.** `QuestDef.MaxLevel` (0 = no ceiling) closes a quest
  to new takers once you outgrow it, and `OfferedBy` stops listing it. Being mid-quest is never
  affected — only ACCEPTING is blocked, which is what stops a level-60 walking back to farm the
  starter chain. **Class quests deliberately carry no ceiling** (owner: "you need your job").
- **Abandon.** `QuestAction "abandon"` drops an active quest and its progress. The client puts a red
  Abandon button on every active row behind a confirmation that says what it costs — including that
  you may not be able to retake it if you are outside its level range.
- **DAILY quests.** `QuestDef.Daily` marks a quest that re-opens when the server day rolls over.
  Completing one records a dated stamp (`<id>@yyyy-MM-dd`) in the completed set instead of the bare id,
  so it never retires — no new DB column, one string per daily per day.
- 🆕 **"The Apothecary's Favour"** (`Quests.Daily.cs`) — talk to Apothecary Miren, get a **1-hour shot
  selection box**, once a day, **levels 6-75**. No kills: its whole job is to put shots in the hands of
  someone without 150 000 gold spare, so the early game is not shot-less while the mid game still buys
  them. The window closes at 75 because by then gold is not the constraint.
  The reward box is **untradable** and worth nothing at a vendor — unlike the 1h boxes Miren *sells* —
  since a free daily that could be farmed across characters and sold on would be a gold faucet.

⚠ Deferred from the quest batch: the on-screen quest TRACKER, the three-tab quest window
(active/unavailable/completed), the per-quest detail window with accept/decline, the new-quest
indicator over NPCs, and the full repeatable-quest system with per-mob exp/gold multipliers.

## 2026-07-29 — One aggressive mob type per field (0.29.2)

**71 of the 80 mob templates are flagged aggressive**, so every field above level 10 was wall-to-wall
aggro — a level-22 champion walking into a 22-28 zone was jumped by casters and melee at once and
simply died (owner, playtest-13: "22 lvl champion getting ganked by magic monsters and few melees
equals death").

Aggression is now decided at SPAWN time, per zone, instead of purely by the template:

- **Elites** still attack on sight; **bosses** still do not (unchanged — a boss is pulled deliberately).
- **Dungeons, instances and elite/boss grounds keep FULL aggression.** That is their character, and
  you go there on purpose. Dungeons identify themselves by construction: the overworld lives in
  `[0, Zone*]` and the negative quadrant is dungeon/jail space, so there is no extra flag to keep in
  sync (`SpawnZone.AllAggressive`).
- **An ordinary field has exactly ONE aggressive type** — the zone's FIRST roster entry
  (`SpawnZone.AggressiveType`). The field still bites; you can just fight one thing at a time.

A template that is passive stays passive everywhere — this rule only ever REMOVES aggression, never
adds it. To change which creature is the dangerous one in a zone, reorder its `MobTypes`.

## 2026-07-29 — The six-quality gear ladder + real shop prices (0.29.1) — ⚠ DELETE `game.db`

One item, six qualities (owner). Design: [design/RarityLadder.md](design/RarityLadder.md).

- **`ItemRarity` gains `Mythic`** (appended as 5 — these values are persisted on every saved item).
  The ladder is **Common 45 / Uncommon 55 / Rare 70 / Epic 70 / Legendary 85 / Mythic 100 %**.
- **THE SPLIT IS AT 70 %.** Rare and Epic carry identical raw stats; **Epic is where set bonuses and
  rolled attributes switch on** (`ItemCatalog.HasIdentity`). Below Epic you buy numbers, from Epic up
  you buy identity — which is what makes two same-statted qualities worth telling apart.
- **The authored gear tables are the EPIC anchor**, so today's best gear keeps exactly the stats it
  had, and Legendary/Mythic are new tiers ABOVE it (Mythic = 1/0.7 ≈ **+43 %**). ⚠ That is a real
  ceiling raise, taken deliberately — measure it with `tools/BalanceMatrix`, don't hand-derive it.
- **Attribute caps scale with quality too** (`AttributeSystem.Roll` × `RarityScale`). Without it,
  quality moved the stat block but left the rolls identical, so the top of the ladder was worth much
  less than its numbers implied.
- **Quality is OUT of the item name.** A piece is an "Electrum Longbow"; its quality is a property,
  shown by the name's **colour** and a `Rarity:` row. `Common Electrum Longbow` read as a different
  item rather than the same bow at a lower grade.
- **Six-colour rarity palette in the Unity client** — item details, vendor buy/sell rows, warehouse
  rows and the worn-equipment squares. The WPF harness only ever had three colours; Unity had none.
- **Structured item description**: Name / Grade / Rarity (+ % power) / Type, then the stats, with an
  Untradable line where it applies.
- **The "(Lesser)" line no longer spawns quality copies.** That is what made the two ladders
  interleave — a Lesser E bow (129) sat between the main line's Common (124) and Uncommon (148), so
  "lesser" read like a quality when it is a different ITEM. One ladder per piece now.
- **Real shop prices** (`ItemCatalog.TieredGearPrice`) from the owner's table, authored as the RARE
  price: F/E/D across gloves-boots / helm-shield / body / 1H / 2H / ring / earring / necklace, from
  3 000 up to 3 000 000. Quality scales it — **Common 35 %, Uncommon 70 %, Rare 100 %** — because the
  low qualities drop freely and at full price nobody would ever buy one.
- **The shop sells only F/E/D, and only to Rare.** The legacy generated grid ("Worn Sword" at P.Atk 6,
  the Fine/Masterwork prefixes), `AshWand` and `IronMace` are no longer stocked — they predate the
  gear ladder by a generation and were half of why the vendor list was unreadable. The catalogue still
  defines them so old saves resolve.

⚠ Still deferred from the design: folding the "(Lesser)" line away entirely (the main line has no F
tier yet, so it would leave levels 1-19 with nothing), scaled SET BONUSES, the vendor UI rework
(grid/list + confirm dialog), and splitting Armsmaster into two NPCs.
⚠ Epic+ price multipliers (1.5 / 2.5 / 4.0) are mine, not the owner's — they only affect what SELLING
one pays, since those tiers are never vendor stock.
✅ SmokeTest green on a fresh DB.

## 2026-07-29 — Archer merges into Rogue (0.29.0) — protocol 6, ⚠ DELETE `game.db`

Bow and dagger are ONE class until 40 (owner). You are a Rogue, you learn both the Stab and the Shot
ladders, and the split moves to the 3rd class. Design: [design/RogueArcherMerge.md](design/RogueArcherMerge.md).

- **Three 2nd classes removed:** Hunter (4, Ork), Warden (10, Elf), Marksman (16, Human). Their ids
  are left as permanent GAPS — class ids are persisted, so reusing one would silently turn an old save
  into a different class.
- **This fixes the hollow archer by deletion.** The old Archer table had exactly two skills
  (`BattleFury` @20, `PowerShot` @24) where every other archetype had a full 20-36 ladder — the
  playtest-13 finding. The Rogue table already taught BOTH `PiercingStab` and `PreciseShot` across
  20/24/28/32/36, so the merge needed no new authoring; the two orphans folded into it.
- **`Disciplines.Of` is now RACE-AWARE**, which is what lets one 2nd class open into different pairs:
  Human → **Nullblade** / Sharpshooter · Ork → Venomweaver / **Hunter** · Elf → Phantom / Trapper.
  Each race keeps one melee and one ranged branch. This matches the race flavours already written in
  `design/Disciplines.md` ("human evades magic, the elf evades phys, the ork should outlive the
  target"), so every branch maps onto a kit that was already designed — `Nullblade` is the human
  Phantom (anti-magic) under its own name, `Hunter` is the ork Sharpshooter.
- **Two new `Discipline` values, APPENDED** (Nullblade 12, Hunter 13) — never renumbered, they are
  persisted on characters. `Disciplines.Parent` sends all six rogue-line disciplines to `Rogue`.
- **The bow behaviours follow the Rogue line now**: the bow-skill range tier (`SkillMath.EffectiveRange`)
  and the basic-attack range bonus (`Entity.RecomputeDerived`) accept Rogue. `Range >= 300` still
  separates a bow skill from a dagger one, so a rogue's melee skills are untouched.
- `Archetype.Archer` stays in the enum (the HP track and those range rules still name it) but no 2nd
  class carries it.

⚠ **Protocol 6** — the client compiles `ClassCatalog` in, so an old client would still offer Marksman.
⚠ **Delete `game.db`**: any character holding class 4/10/16 no longer resolves.
✅ SmokeTest green on a fresh DB.

## 2026-07-29 — Playtest-13 tier 2 (0.28.96) — protocol 5

- **The server console is readable again.** EF Core logs every statement at Information, and this
  server saves constantly (event saves + a 60s autosave over every online player), so the useful lines
  were buried. Filters added in `Program.cs` for the EF command/query/infrastructure categories and
  ASP.NET's per-request lines; warnings and errors still come through. Verified: a full startup went
  from a wall of SQL to **64 lines, zero `Executed DbCommand`**.
- **EF warning 10103 gone.** The startup schema probes called `FirstOrDefault` with neither a filter
  nor an order. Any row will do — that's the point of the probe — so they now say so with
  `OrderBy(x => x.Id)`.
- **The debug "2nd class" buttons set the CLASS, not the crafting profession.** They were wired to
  `DebugSetProfession`, passing a class id (1-18) into the 5-value crafting enum, where it was clamped
  — so everything from id 5 up silently became ScrollScribe. New `DebugSecondClass` command/hub method
  applies the class directly, skipping the quest and level gates (race + base class are still checked).
  Crafting professions now have their own rows, and the class list only offers classes you could
  actually be.
- **The auto-farm "keep position" circle stays put.** `AutoHuntStatus` now carries `FarmCenterX/Y` —
  the server owns that anchor and it was never on the wire, so the client drew the ring around the
  CHARACTER. The one setting whose whole point is standing still looked like it was following you.
  Roaming mode still centres on the player, which is correct there.
- **Item-details and mob-info windows no longer render clipped.** Both put a `ContentSizeFitter` label
  in a `ScrollRect` and set its text without forcing a layout pass, so the body was laid out against
  its PREVIOUS size and the scroll offset was stale — the item window hid its first stat row under the
  title bar on the first open (fine on reopen), and the mob sheet showed with its top rows above the
  visible area. Both now rebuild and pin to the top when the text changes; the target window only does
  it when the text actually differs, since that refresh runs every frame.
  ⚠ **Unity-side, so NOT compile-verified** — `dotnet build` does not build the Unity project. The
  mob-window fix in particular wants an on-device look.

⚠ **Protocol 5** — new `DebugSecondClass` hub method, and `AutoHuntStatus` gained fields.

## 2026-07-29 — No combat-logging out of a DoT (0.28.95) — protocol 4

Owner's rule: a DoT keeps you IN COMBAT, so you can only return to character select once you have
escaped, killed them or died **and** nothing is ticking on you. This is the answer to the hole left by
0.28.94 — debuffs are deliberately not persisted (a DoT needs a live applier for attribution), so
without it you could shed a Venomweaver's stacks by quitting to the character screen.

- `IsInCombat` now also returns true while any `SkillEffect.AnyDot` buff is present. It is the shared
  predicate, so the same rule covers `/exit`, the equipment-preset swap, and the link-dead grace timer
  (which stays PAUSED while a DoT ticks — pulling the plug mid-bleed no longer runs the clock down).
- **Character select was not gated at all.** `/exit` checked combat; leaving to the character screen
  did not, which was the actual hole. It now refuses with the same rule.

🔴 **And a real bug in 0.28.92's save fix:** the client called `LeaveWorld` with SignalR's
**`SendAsync`**, which returns as soon as the message is written and never waits for the hub method.
So the hub awaiting the save delayed nothing and the character-select level could still be stale. Both
clients now use **`InvokeAsync`**, which is also what lets the refusal reach them. `LeaveWorld` returns
`string?` — null = left, otherwise the reason — and the clients stay in the world when refused rather
than showing the character list while the server still holds the entity.

⚠ **Protocol 4** — `LeaveWorld` changed shape and meaning.

## 2026-07-29 — Buffs survive logout (0.28.94) — ⚠ DELETE `game.db`

The last tier-1 item from [testing/Playtest-13.md](testing/Playtest-13.md). Buffs died on every logout
for the plain reason that nothing saved them. The owner's rule: a buff ends when it EXPIRES, is
dispelled/cancelled, or the subclass changes — not because you closed the game.

- New `CharacterRecord.BuffsJson` column + `PersistenceService.BuffSnapshot`. The snapshot is
  deliberately minimal — skill id, the LEVEL it was cast at, wall-clock expiry, stacks, shield pool,
  display name. Everything else (effect flags, magnitudes, DoT power) is rebuilt from the catalog, so a
  buff restored after a skill was retuned comes back with the CURRENT definition, not a stale copy.
- `BuffInstance.Level` records the level ApplyBuff was called with. `Rank` is stacking priority and was
  never the same number.
- Restored through the normal `ApplyBuff` path on entry to the world (`RestorePersistedBuffs`), then the
  remaining ticks / stacks / shield pool are written over the fresh values. **Time offline counts**: the
  expiry is wall-clock, so an hour away spends an hour of a one-hour buff and anything that ran out
  while you were gone never comes back.
- **Not saved, each for a reason:** debuffs (a DoT needs a live applier for damage attribution — so a
  relog still clears them; fixing that needs the attribution problem solved, not a bigger snapshot);
  internal DoT stack counters; the synthetic grade-penalty rows (recomputed); and RUNE buffs, which
  `ReconcileRuneBuffs` already re-derives from the held item, so saving them would double-apply.
- The four `Buffs.Clear()` sites — town respawn, subclass swap, character reset, death — are all
  deliberate and unchanged.

⚠ **Schema change.** `EnsureCreated()` only creates a DB that is absent; it will NOT add the new column.
**Delete `Game.Server/game.db` (+ `-shm`/`-wal`) before running.**
⚠ Wire is unchanged — still protocol 3.

## 2026-07-29 — Playtest-13 tier 1: seven bug fixes (0.28.93) — protocol 3

The first batch off [testing/Playtest-13.md](testing/Playtest-13.md). Seven of the eight tier-1 items;
each root cause is noted at the fix site.

- **Crafting materials stack properly.** `ItemDef.IsStackable` is now ONE shared predicate. The client's
  vendor had its own copy that omitted `EquipSlot.Material`, so a stack of 11 showed "x11" but sold one
  at a time with no quantity numpad, while the server happily stacked it. The warehouse also moved whole
  rows without merging, leaving several rows of the same material — deposit and withdraw now merge.
- **SP updates as it is earned.** `AwardExp` grants SP but only pushes `Progress`, never `Stats`, so the
  figure sat at its login value all session and only corrected on relog. `ProgressUpdate` now carries
  `SkillPoints` (the one push that fires per kill) and the client tracks it from both pushes. Sending
  the ~45-field `StatsUpdate` on every kill would have fixed it far more expensively.
- **Character select shows the real level and class.** Two separate faults. The level was stale because
  `LeaveWorld` returned before the background save landed and the client fetches the list immediately —
  the hub now awaits the save (5s cap, so a stuck write can't freeze the screen). The class was never
  *rendered*: the row printed `Race + BaseClass`, so every archer read "Human Fighter" and a Warchanter
  read "Human Mage". `CharacterSlot` gained `ThirdClass` and the row names discipline → second class →
  base class.
- **Newly learnable skills unlock without a relog.** `OnLevelUp` never re-sent the subclass, so the
  client's active-class level stayed at its login value and the Learn tab gated against it.
- **Buffs no longer cancel by double-tap — press and HOLD.** Double-tap was unusable on a phone: the
  details pop-up opened on the first tap and swallowed the second, so cancelling took a burst of taps
  that also cancelled the neighbours. Uses the same `PressAndHold` the skill bar already uses.
- **The previous character's buffs no longer linger.** `Buffs` and `BuyBack` are the only two
  per-character caches the server pushes CONDITIONALLY, so unlike inventory/stats/quests they were never
  replaced on a character switch. Both are cleared in `ResetWorldTransients`.
- **Quest-giver dialog refreshes on accept.** Accepting never passed the NPC through, so the panel kept
  showing the pre-accept text and you had to close and re-talk to learn the objective.
- **Combat no longer suppresses regen at all** (owner's call). `Regenerate` used to return early while
  `Engaged` or mid-cast. Auto-farm made that permanent — it re-asserts `Engaged` every tick a target
  exists — so a farming fighter regenerated nothing until they stopped. The rule was ours, not L2's:
  L2 modifies regen by STANCE, never by combat, and the stance stack already expresses "resting vs
  fighting". Regen is now governed by stance (sitting ×1.8, walking ×1.2, running ×1.0), the safe zone,
  SPT/CON and buffs only. Mages were never affected by the `Engaged` half — `ExecuteSkill` skips it for
  `BaseClass.Mage` — but they were paused mid-cast; that is gone too.

⚠ **Protocol 3** — `ProgressUpdate` and `CharacterSlot` both gained fields. The additions are optional,
but a NEW client against an OLD server would read SP as 0 after every kill, so the handshake must reject
that pairing. Server and APK deploy together.
⚠ **Not yet run:** SmokeTest (this touches the leave/save path and persistence) and a device test.

## 2026-07-25 — Buy-back window (Unity) (0.28.91)

Client UI for buy-back (server was done in 0.28.86). `GameUi.BuyBack.cs` — lists items you recently sold
(`Boot.BuyBack`, pushed when a vendor opens), tap a row to re-buy for the same gold; affordable rows lit,
others dimmed. Opened from the vendor dialog ("Buy back" row). `NetworkChannel`/`GameBoot` gain the
`BuyBack` push + `BuyBackItem(index)` call. Mirrors the (verified) warehouse window.
✅ Unity-compile-verified (headless build SUCCEEDED).

## 2026-07-25 — Warehouse NPC + block/like/unblock actions (0.28.90)

- **Warehouse is its own NPC** (owner): new `NpcRole.Warehouse` + a **Keeper in each of the 7 main towns**;
  its dialog opens the warehouse (`NpcDialog.Warehouse` flag). Moved the open trigger off the vendor dialog
  (the 0.28.89 stopgap) onto the keeper.
- **Block / Like / Unblock are ACTIONS** (owner: "/commands need an action button"): 3 new `ActionCatalog`
  entries (PlayerTarget) + client dispatch — the target supplies the name, like the friend actions.
- ⚠ The client pieces (dialog button, action dispatch) are Unity — NOT compile-verified until the next APK
  build. The server/shared side (NPC role + placement, NpcDialog flag, action catalog) is dotnet-verified.

## 2026-07-25 — Warehouse UI (Unity) (0.28.89)

The client warehouse window (`GameUi.Warehouse.cs`) — Deposit / Withdraw tabs, tap a row to move the whole
item; opened from a town NPC's dialog ("Bank"). `NetworkChannel` gains the `Warehouse` push + Open/Deposit/
Withdraw calls; `GameBoot` holds `Warehouse` and the three methods. Mirrors the vendor window's tab+list
shape (no prices/numpad — a move is reversible; the server owns the transfer + town-gate).
⚠ **NOT yet Unity-compile-verified** — `dotnet build` doesn't compile the Unity project (owner: leave the
APK). It mirrors the proven vendor patterns, so risk is low; the next Unity/APK build confirms it. Server
+ protocol were already done (0.28.83).

## 2026-07-25 — Charisma: moderation drains (0.28.88)

Completes charisma. The moderation actions now drain BOTH values (per started hour-band): **chatban −20/h,
jail −100/h, kick −250/h; ban → 0**. Because the admin handlers run on WORKER threads, each enqueues a
tick-thread `CharismaAdjustCmd(name, poolΔ, lifetimeΔ, zero?)` which applies to the live entity if online,
else via the DB (`AddCharismaAsync` / `ZeroCharismaAsync`). No schema change (reuses the charisma columns).
SmokeTest: a liked player is on the board, then a 60-min jail (−200) drops them off it.

## 2026-07-25 — Charisma / reputation — core (0.28.87)

Reputation with **two persisted values** (neither below 0): a **pool 0–1000** (drives the reward — every
20 = +1% exp/sp, cap +50%) and an uncapped **lifetime** (the ranking board).
- **`/like <name>`** (`Like` hub cmd): +1 to both, from a **20/day budget** (freely distributed, resets at
  UTC midnight, no receive cap). Works on an online target (live) or offline (DB write).
- **PK drain**: a kill drains both values by `karma × 0.01` — so a griefer can't top the board.
- **Exp/sp bonus**: each earner's own charisma multiplies their share (1.0–1.5), applied at the same
  personal stage as the mob-level gap (so it amplifies party-split exp per player).
- **Ranking**: a new **"charisma"** leaderboard on the lifetime value (#1 = "the Beloved").
- Persisted (Charisma / CharismaLifetime / daily-budget — SCHEMA CHANGE, game.db reset). SmokeTest: like
  raises charisma + spends budget + reaches the board; self-like blocked.
- ⏳ **Deferred**: the moderation drains (chatban/jail/kick −tiers, ban → 0). Those admin paths run on
  WORKER threads, so draining an online target's charisma there needs a tick-thread command — a bounded
  follow-up. Kills already drain lifetime, so the anti-griefer intent holds for PK now.

## 2026-07-25 — Buy-back (0.28.86, server-side)

Re-buy a recently-sold item at any vendor for the price you got. `Entity.BuyBack` is an in-memory list
(newest last, capped at `GameConstants.BuyBackSlots`=24, cleared on logout — no schema change) that records
each sale with enough to restore the item faithfully (enchant + rolled attributes). `HandleSell` records
the sale; `BuyBack(npc, index)` charges `unitPrice × qty` and restores the item. `BuyBackUpdate` is sent
when a vendor opens and after every sell/buy-back. Build-verified; NOT SmokeTest-covered (shop interaction
needs vendor-proximity the harness lacks; in-memory so no persistence risk). Client buy-back tab is the
next-APK follow-up alongside the warehouse/block windows.

## 2026-07-25 — Block / ignore list (0.28.85)

Per-character ignore list. `BlockCommand` block/unblock/list (mirrors the friend list). A blocked player's
**whisper, world and local chat** is filtered out for you; the blocked player is told nothing, but the
SENDER of a blocked whisper is told it wasn't accepted (a silently-vanishing whisper would read as a bug).
Block and friend may coexist — blocking only filters chat, and friend presence is a system message, not
chat. Persisted as `BlockedCsv` (SCHEMA CHANGE — delete game.db). SmokeTest: whisper filtered after block,
sender notified, list survives a relog.

## 2026-07-25 — Blueprint crafting + a latent crafting-crash fix (0.28.84)

- **Fixed a latent crash**: `RecipeCatalog` set `_byId = Build()` as an inline field initializer *before*
  the `Cross`/`Steps` tables it reads — static initializers run in textual order, so `Build()` NRE'd and
  the whole catalog threw `TypeInitializationException` **on first access (i.e. the first craft)**. It
  survived because crafting had never been exercised end-to-end. Fixed with an explicit static constructor
  (runs after all field initializers). Now covered by a SmokeTest craft.
- **Blueprint economy** (owner's design): an endgame (DropOnly / A-grade) recipe is unlocked by consuming
  **1 blueprint** (its recipe book — renamed "Blueprint: …"), and **each craft consumes 1 more**, so the
  first craft costs 2. `HandleCraft` requires + consumes `RecipeBookId(recipe.Id)` for DropOnly recipes;
  learn/craft messages spell out the cost. SmokeTest verifies unlock→craft→blocked-without-blueprint.

## 2026-07-25 — Private warehouse (0.28.83, server-side)

Built the per-character warehouse the shot-rune system already pointed at ("move a rune to the warehouse
to switch it off" — a rune's buff only applies while it's in the bag).

- **Model**: `Entity.Warehouse` — a second item list, separate from the bag so every bag iteration
  (equip, RecomputeDerived, drops, trade) is untouched. Base **50** slots (`GameConstants.WarehouseSize`).
- **Persistence** (SCHEMA CHANGE — delete game.db): items carry an `InWarehouse` flag; snapshot writes both
  lists, load routes each item to the bag or bank. Verified by SmokeTest (deposit → relog → item is in the
  BANK, not the bag).
- **Commands/DTO**: `OpenWarehouse` / `WarehouseDeposit` / `WarehouseWithdraw` + `WarehouseUpdate`. Deposit
  auto-unequips and `ReconcileRuneBuffs` drops a deposited rune's buff (withdraw re-applies it); a banked
  rune still expires. Sent on login so the client has it without a town trip. Access gated to **safe zones**.
- **NOT YET**: the client warehouse window (Unity + WPF) — server + protocol only; UI is the follow-up for
  the next APK. Account warehouse + slot-expansion tickets remain deferred.

## 2026-07-25 — Gear ladder: low-grade fills, named sets (0.28.82)

Filled the low-level gear holes and gave every tiered piece a proper name.

- **Low sets** (`ItemCatalog.LowTierFillers`): each grade now has a LOW set covering the bottom of its band,
  beside the existing TOP set — **Low F** (lvl 2-9), **Low E** (20-32), **Low D** (40-44). Equippable at the
  grade level (ItemLevel 1/20/40), ids `_t{L}lo`, armour interpolated between the Newbie floor and each
  grade top (fixes an inversion where low armour sat under the Newbie set). All 8 weapons + full armour.
- **Buyable + drop**: low sets sold at the Armsmaster (ids derived from the catalogue) with ItemLevel-scaled
  prices (Low F < E < D), and they auto-generate rarity drop copies like every tier.
- **Named gear**: `ItemCatalog.GradeTheme` gives each grade a MATERIAL prefix starting with the grade LETTER
  — Ferrite/Electrum/Darksteel/Cobalt/Bloodsteel/Adamantine/Soulcrystal/Starstone/Seraphite. Names are
  "{Material} {noun}" (Blade, Maul, Fangs, Longbow, Battlestaff; heavy Bulwark/Warplate, robe Vestments/
  Raiment, shield Aegis, Pendant/Band/Stud …). e.g. **Bloodsteel Warplate**, **Darksteel Vestments**. Low
  sets add "(Lesser)". S-grade themes are wired, waiting on the endgame CSV.

## 2026-07-25 — Magic stat model: weapon-based M.Atk (0.28.81)

Reworked M.Atk to L2's **multiplicative** shape (matching P.Atk, which already worked this way), because
the old **additive** base (`atkStat + level·2 + weaponM`) put the ~41-point power stat in as a flat FLOOR
— a level-1 mage read ~40 internal M.Atk where L2 has ~8, doing ~2.2× L2's magic damage and one-shotting
low-level mobs. Now the **weapon M.Atk is the base and the ATK stat multiplies it** (fist value when
unarmed), so a small wand yields small M.Atk and the staff's big base carries the endgame.

- **Two stat multipliers** (owner's "2 coefficients"): `PAtkStatMult` linear, `MAtkStatMult` super-linear
  `(atk/40)^1.75` ("INT is king" for magic). The exponent mainly rewards ATK *investment* (dyes/swaps) —
  geared endgame is driven by weapon M.Atk + robe `M.Atk ×1.17` + attributes, not the stat.
- Measured (BalanceMatrix): lvl-1 mage internal M.Atk 40→**8** (L2-exact); lvl-8 nuke 399→154. Endgame now
  lands on the original anchors (414 dmg vs a high-lvl tank [anchor 300-400], ~3.8 casts). Fighter physical
  untouched. Endgame magic will be set by the coming S-grade staff M.Atk, not the stat.
- **M.Atk display** = `min(internal, 20·√internal)` — honest small number low, shrink only the cosmic high end.
- **Mob M.Def** coefficient 3.0→3.16 (L2 lvl-83 mob = 262). **Mob SP** = flat **1/20** of exp (was a decaying
  1.0→0.05 curve; L2 is flat). `ExpCurve.md/.csv` regenerated.
- Roadmap added: `docs/design/GearLadderAndCrafting.md` (S/S\*/S\*\* grades, ladder gaps, blueprint crafting).

## 2026-07-25 — Overnight bug + polish batch (0.28.80)

Autonomous session against the device-playtest findings (`playtest-12-results`). No schema changes, so
existing characters survive. Large social/economy features (charisma, block, buy-back, chat tabs,
partial-stack trade, client collision, wearable titles) are deliberately NOT here — they need a schema
change and/or the owner's input, flagged for a session together.

Fixed:
- **Equip presets on the bar no longer vanish** — `SyncSkillBar` was wiping `preset:` tokens (item:/
  action: were exempt, preset: was missed); it's now exempt too. SmokeTest guards it.
- **Dungeon mobs spawn in the dungeon and aggro** — `WorldMap.Border` was the positive overworld only,
  so `ClampToBorder` snapped every negative-quadrant (dungeon) spawn to (0,0); it now spans the full
  world. This was "mobs spawn on one spot and don't fight back" in the crypt.
- **Learn tab refreshes on level-up** — it keyed off `ActiveClass.Level`, which only the Subclasses push
  set (login/swap), so it went stale after a level-up; the Progress push now keeps it in step.
- **Basic attack is not auto by default** — the client seeded it into the auto set; now nothing is auto
  unless explicitly marked.
- **Admin characters excluded from all leaderboards** (an admin at level 999 would top them forever).
- **Shop pricing** — shot runes 150k/1h and 280k/2h; equipment floored at 200g (jewels exempt).
- **Low-level gear drops gated** — a level-8 mob no longer drops E-grade (level-20) gear; below mob
  level 18 the loot is training/broken gear + mats.
- **Rare healing potion removed from the vendor** (drops/rewards only).
- **Party loot control** moved to a coloured button by the buffs toggle (random = blue), leader-only
  drop-down.
- **Bag equip paper-doll** moved below the header so the Head slot no longer overlaps the tabs.

Measured, not changed (flagged for an owner decision): the "low-level one-shot" is the mage nuke, which
one-shots trash at every level (the tuned level-20+ matrix does the same) — consistent nuker design, not
a low-level bug. BalanceMatrix now prints levels 1-10 with real starter gear.

---

## 2026-07-25 — Console freeze fix + speed-display fix (0.28.78)

Two device-playtest fixes.

- **Console freeze (regression from 0.28.77).** The append rewrite in 0.28.77 was correct, but the trim
  I added — `while (childCount > 120) Destroy(oldest)` — FROZE the phone: Unity's `Destroy` is deferred
  to end of frame, so `childCount` never drops inside the loop and `GetChild(0)` keeps returning the same
  already-marked object → an infinite loop the moment the log passed 120 lines. Now the excess is counted
  ONCE and that many rows are destroyed by index. (0.28.76 lag = real accumulation; 0.28.77 freeze = this
  trim bug; 0.28.78 resolves both.)
- **Attack/cast speed display was inverted.** The DTO field is a cast/attack-TIME multiplier (lower =
  faster: the server sends `SpeedBaseline / stat`), but the tier-2 display did `raw = mult × baseline`,
  which flips it — a fully-buffed caster read "158 (x0.47)" when the real stat was ~702 at ~2.1×. The raw
  stat is `baseline / mult` and the speed multiplier is `1/mult`. (Playtest: the cast-speed and M.Atk
  NUMBERS a player flagged were display artifacts; the lvl-1-one-shots-lvl-4-8 finding is real and
  deferred to a measured BalanceMatrix pass.)

---

## 2026-07-24 — Console lag fix + playtest APK (0.28.77)

Live device playtest (Gena) surfaced a real one the SmokeTest can't: with the chat/console window
OPEN, the phone lagged worse and worse until the log was cleared.

- **Console now appends instead of rebuilding.** `RefreshConsole` used to Destroy every child and
  rebuild all up-to-200 labels — each with a ContentSizeFitter — plus a `Canvas.ForceUpdateCanvases()`,
  on EVERY new log line while the window was open. During combat/debug spam that is many full
  teardown/rebuilds a second, and the cost grew with the accumulated line count — so clearing (→ ~0
  rows) made it cheap again, exactly what was seen. `ClientLog.Line` gained a monotonic `Seq` and a
  `ClearGeneration`; the console draws only undrawn lines and trims oldest rows past a 120 cap. Bounded
  work per frame regardless of session length.

Also fixed the deploy-order slip that made the first two rebuilds ship a STALE version label: the APK
version is stamped from `GameConstants.GameVersion` in the Unity plugin DLL, so `dotnet build` (which
copies the fresh DLL into Assets/Plugins) MUST run before the headless Unity build — see
`version-bump-deploy-order`. The served APK is now correctly 0.28.77.

---

## 2026-07-24 — Every name-only command is now a bar ACTION (0.28.76)

Completing the owner's "every command that doesn't need a value, only a name, as an action button". The
Actions tab already had eight (Attack, Target Closest, Sit/Stand, Run/Walk, Trade, Party Invite, Follow,
Assist) — so no command was ever homeless — and this adds the remaining six:

- **Add Friend / Remove Friend** (target a player) and **Friend List**.
- **Leave Party**, **Kick from Party** (target a member), **Pass Leadership** (target a member).

Each is placeable on the skill bar like any action. The TARGET supplies the name, so nothing is typed.
Commands that need a real VALUE stay typed — `/w <name> <message>` (a message) and trade quantities (a
number) — because a button cannot supply one. Admin commands are excluded (owner: "except admins").

Implementation note: the friend actions resolve the target to a NAME (the hub takes a name, because
friendship must work on someone offline or out of view); the party actions take an id, since a party
member is present by definition. The slash equivalents (`/fadd` `/frem` `/flist` `/ptleave` `/ptkick`
`/ptcl`) all still work.

---

## 2026-07-24 — Tier-2 UI batch, part 3 — the list is complete (0.28.75)

All thirteen cheap playtest-11 UI items are now done.

- **Buff taps: single = details, double = cancel.** Cancelling used to be a SINGLE tap, which put an
  irreversible action one stray touch away on a bar you brush past constantly — and there was no way at
  all to read what a buff did. A single tap now opens a tooltip-style popup (name, description, time
  left, whether it can be dismissed) that closes on a tap anywhere outside; a double tap within 0.35s
  cancels. Debuffs are not yours to dismiss, so double-tapping one just re-shows its details.
- **Party effects are SQUARES beside each member**, same shape as the personal buff bar and using the
  same abbreviations, green for buffs and red for debuffs. Rows are a fixed 46px now instead of growing
  to 64px when someone had effects, which is what kept making the window taller. Panel widened
  300 -> 380 so the squares clear the leader's Kick/Lead buttons.
  ⚠ **No `<60s` flashing**, unlike the personal bar: `PartyMemberDto` carries effect NAMES only, with no
  remaining time, so there is nothing to count down. It needs durations on the wire — a DTO change.
- **Loot mode is a DROP-DOWN**, not a cycle button. Cycling was not merely fiddly: every tap STARTS A
  VOTE the whole party has to answer, so tapping past a mode you did not want was not free. Picking a
  row proposes that mode directly; `NextLoot` is gone.
- **World border** — an orange dashed rectangle around the overworld, as a placeholder until there are
  mountains or an ocean. It deliberately does NOT hide behind the zone-colours toggle: walking into an
  invisible wall with nothing to explain it is the problem it solves, and that does not go away when the
  map overlay is off. Only the positive overworld is outlined — the negative quadrant is teleport-only,
  so its edges are never something you can walk up to.

---

## 2026-07-24 — Tier-2 UI batch, part 2 (0.28.74)

- **Bag: `Equip` leads the row and the paper-doll opens on the LEFT** (owner). Equip is the control that
  reshapes the window, so it goes first; the item list now slides right by exactly the width the window
  gains, keeping its position relative to the right edge instead of being shoved outward.
- **Potion heal-over-time has its own floater.** The pipeline was already correct end to end — potions
  carry `SkillEffect.HealOverTime`, the tick broadcasts `CombatOutcome.Heal`, the client draws `+N`.
  The problem was that it was INDISTINGUISHABLE: a potion tick, a cast heal and ambient regen all drew
  the same green `+N`. HoT ticks are tagged now and render as a distinct mint `+N hot`.
  ⚠ Note `TickHealOverTime` early-returns at full HP, so drinking while topped up heals nothing and
  shows nothing — correct, but it looks identical to broken and may be what was actually seen.

---

## 2026-07-24 — Weapons carry BOTH their CSV numbers; the caster rule moves into a passive (0.28.73)

The gear CSV has always authored weapons as a PAIR — `92/54` for a level-20 sword — but only one number
ever reached the game. A fighter weapon kept P and discarded M; a magic weapon kept M and discarded P.
The missing channel was reconstructed by multiplying the WHOLE finished channel by `OffChannelFactor`
(0.6), an invisible per-item multiplier. Two consequences: no weapon in the catalogue set `MAtkBonus`,
so **no weapon ever showed an M.Atk line on its card**, and "why is my M.Atk 60%?" had no in-game answer.

- **Both numbers are authored now** — `AtkBonus` = P, `MAtkBonus` = M, straight from the CSV, for all
  eight weapon families across all five tiers. `PAtkFactor`/`MAtkFactor` are retired to 1.0.
- **Weapons contribute their own M.Atk** like every other slot. Items that predate the migration have
  `MAtkBonus = 0` and fall back to the old shared-number behaviour, so nothing rebalances under them.
- **The caster rule moved into `Weapon Proficiency`**, where a player can read it. It is now TWO gates,
  because they answer different questions: **cast speed** keys on the trained TYPE (sword/blunt, which
  includes wands and staves), while **M.Atk** keys on the weapon actually being a MAGIC weapon — which
  the type cannot answer, since a wand and a mace are both `Blunt`. That is precisely the hole
  `MAtkFactor` was plugging: the old type check waved a mace-swinging caster through at full power.
- The multiplier lives once, on the class rule (`Entity.NonMagicWeaponMagicMult`), instead of on every
  weapon — so a fighter picking up a wand is no longer silently taxed for a caster's problem.

**Verified by measurement, not by reasoning** — this area has a history of hand-derived diagnoses
blaming the wrong system. `BalanceMatrix` output is byte-identical before and after across every case
it covers (mage, tank/fighter, mob curve, TTK, levelling pace).

Two deltas it does NOT cover, reasoned explicitly: a **caster holding a mace** now contributes the
weapon's real M.Atk (132 at A-grade) rather than its P.Atk (281) before the penalty, so that build gets
weaker — the intended direction. A **fighter's** M.Atk shifts slightly, since `(base + 281) x 0.6`
becomes `base + 132`; fighters have no spells, so it is inert unless a hybrid leans on it.

---

## 2026-07-24 — Tier-2 UI batch, part 1 (0.28.72)

Seven of the thirteen cheap playtest-11 UI items. Client scripts compile-checked with a headless Unity
build (`dotnet build Game.sln` does NOT cover the Unity project — see the checklist for the invocation).

- **The duplicate town line is gone.** There were TWO independent "You entered X" systems: the server's
  Region banner (big, with a background) and an older client-side zone label that coloured towns blue.
  The second one's own comment said it should be replaced "when Regions ship on both clients" — which
  they have. Removed.
- **The region banner no longer eats taps.** As a plain Image + text it was a raycast target, so every
  tap landing on it was swallowed ("prevents me clicking below my char"). It is a notice, never a
  control, so nothing about it is interactive now.
- **Durations roll over into DAYS** — a 30-day shot rune read `719h59`, which is true and useless. Now
  `29d`; past a week the hours are dropped entirely, so every tier stays at most four characters.
- **Debug-menu chat spam removed** for items, levels and buffs — taking ten potions wrote ten identical
  rows. Each already has visible feedback (inventory refresh, the level number, the buff bar). The rare
  ones keep their line: teleport coordinates, karma, class change.
- **A targeted PLAYER carries no fast buttons at all.** Attack/Follow/Assist/Party/Trade come off the
  frame; those belong in the Skills window's Actions tab, placeable on the bar. Mobs keep Attack (the
  core loop) and Info (stats + drops).
- **Target HP/MP as digits**: current/max instead of a percentage, plus an MP bar for player targets.
  ⚠ This REVERSES the older "another player's exact HP is information you should not have" rule, at the
  owner's request. Level stays private.
- **Attack/cast speed show the raw stat**: `1234 / 1500  (x3.70)` instead of a bare `x1.10`, in both the
  Stats window and mob Info. No wire change — the engine uses the L2 model where 333 = 1.0x, so the raw
  value is `mult x 333`, and the caps are the real `StatCaps` ones.
- **Standing up is INSTANT after a real rest** (seated >= 3s). The recovery exists to stop sit/stand
  spam and now only costs that. Being HIT while seated still pays the full delay — a combat interrupt is
  not a voluntary stand.

Remaining in the batch: bag Equip-first + expand-left, potion HoT floating text, buff double-tap
cancel / single-tap details, party buff-debuff squares + loot drop-down, world border, and
commands-as-actions.

---

## 2026-07-24 — The level-10 starter quest, and ReachLevel steps actually work (0.28.71)

Completes the starter-gear redesign. The Newbie kit is no longer given away — it is EARNED.

- **New starter chain**, offered by **Armsmaster Dolan** (the gear vendor handing out gear needs no
  explaining, and a new player is already walking to him to spend their first gold):
  - **"A Proper Kit"** (level 10) — slay 10 Ashen Wolves, return → the **armor + weapon** choice boxes.
  - **"Blooded"** (level 12, gated on the first) — slay 15 Werewolves, **reach level 15**, return →
    the **jewels box + 1-day shot rune**, the two things deliberately removed from character creation.
- Both rewards are SELECTION boxes, so the chain stays class-agnostic exactly like the creation kit.
- Pacing against the new curve: the rewards are 52% and 39% of a level — meaningful without
  trivialising — and the second quest spans **122 mobs** of levelling from 12 to 15, which is the
  "levelling to ~15 while doing it" the owner asked for.
- A vendor can host a quest and a shop at once: the dialog only special-cases Buffers, and the shop is
  attached alongside the quest list.

**`QuestStepType.ReachLevel` had never been implemented.** It has sat in the enum since quests were
written, but no quest used it, so nothing noticed that no code anywhere advanced such a step — a quest
reaching one would have stalled forever. `AdvanceLevelQuests()` now handles it, called from three
places: on **level-up**, on **quest accept** (you may take a quest already past its level) and after a
**kill or talk step advances** (finishing a step can make a ReachLevel step current, and a level-up is
the only other trigger — a player already past the level would otherwise sit there permanently).

---

## 2026-07-24 — Starter gear: the TRAINING tier for levels 1-10 (0.28.70)

The owner's playtest finding was that a new character one-shots everything: it started in the **Newbie**
set, which is strong enough to trivialise the first zones. That set is now the **level-10 quest reward**
(quest still to build), and a new character starts in a new, deliberately feeble **Training** tier.

- **Training weapons** (~a quarter of the Newbie numbers): Training Sword 6, Club 6, Knives 5, Bow 11,
  Wand 7 M.Atk. **400g each** at the Armsmaster, so a bad pick or a loss is recoverable — unlike the
  Newbie tier these are buyable.
  ⚠ The owner authored these as P.Atk/M.Atk pairs (6/5, 6/5, 5/5, 11/5, 5/7). Only the FIRST number is
  authored; the second follows from the weapon's CHANNEL FACTOR. Forcing a dagger's M.Atk to match its
  P.Atk would mean `MAtkFactor 1.0` — daggers casting as well as a staff — which reverses the
  weapon-identity rule the item model is built on. The standard 0.6 lands within a point or two.
- **Training armor**: Leather 53 P.Def, Robe 27 P.Def + 29 MP. No set bonus — the set line starts at
  the Newbie tier, i.e. at the quest.
- **NO jewels and NO shot runes at creation** (owner). Both were in the old kit.
- **Broken jewels** — a new level 1-5 drop line and the first accessory anyone owns: Broken Earring
  (11 M.Def, 40g), Ring (7, 30g), Necklace (15, 60g). They drop as one mutually-exclusive group (10%
  combined) from mobs at level ≤5 and are stocked at the Armsmaster. **Tradable**, unlike the bound
  starter kit — the first thing a new player owns that is worth selling.
- Both character-creation paths (`CreateCharacterAsync` and the live `GiveStarterKit`) now hand out the
  same two class-agnostic selection boxes, so there is no fighter/mage branch left to drift.

**Still to build:** the level-10 starter QUEST that awards the Newbie set, the jewels box and the 1-day
shot rune.

---

## 2026-07-24 — Damage ledger: most-damage earns the kill, contested kills split (0.28.69)

The last unbuilt piece of the exp spec. Until now there was **no per-attacker damage tracking at all**,
so "killer" meant whoever landed the final blow: a party could do 99% of the work, lose the last hit and
walk away with nothing.

- **New `Entity.DamageLog`** (mobs) — attacker id → damage actually dealt. Deliberately SEPARATE from
  `Threat`: threat is a targeting signal that taunt and detaunt move around on purpose, so it says who
  the mob wants to hit, not who earned it. Only PLAYER damage is banked. Cleared on spawn and on reset,
  so a mob that leashed home and healed owes nobody.
- **The top damager earns the kill** — drops, quest credit and the karma tick all key off them now.
- **Contested kills split the EXP by damage share**, pooled BY PARTY so a party counts as one contender:
  80% of the damage takes 80% of the exp; the other side takes 20% and no drops. Each contender's slice
  then runs the normal rules — pot × roll × party bonus, split equally, personal level-gap penalty.
- A contributor who **left the world** is skipped but their damage stays in the total: their share is
  forfeited, not redistributed, so having a friend log off can't inflate your cut.
- **`Entity.LastHitterId`** records the final blow. It is no longer what rewards pay on, but it is kept
  as a counter for raid/epic bosses (owner).
- One roll per kill still covers everyone on it, so two parties on one corpse see consistent numbers.

---

## 2026-07-24 — The last four playtest-11 tier-1 bugs (0.28.68)

All seven tier-1 bugs are now fixed. The two interesting ones were invisible from the symptom.

- **Skills → Learn now says why it can't.** The row was `canLearn ? action : null`, so an unaffordable or
  level-locked skill got a **dead button** — tapping did nothing, with no message, which is
  indistinguishable from a broken feature (and was reported as one). The server was always fine; every
  rejection path there sends a reason. The button is now always wired and explains level / SP / gold.
- **The soft keyboard lifts the command bar.** There was no keyboard handling anywhere in the client —
  the lift had never been written. Android's keyboard is an overlay that does not resize the game view,
  so a bar pinned to the bottom edge is simply swallowed. The field + Send + Log now offset by the
  keyboard height (converted from screen pixels to canvas units via the reference height).
- **`[lead]` moves the badge and the button.** The server was already correct; the client's party window
  only rebuilds when a **stamp** changes, and the stamp covered HP/MP/status/buff counts but **not
  `IsLeader`** — so passing leadership changed nothing it could see. `IsLeader` and the member IDs are
  in the stamp now (the IDs also catch swapping a member for another with identical HP). The `*` badge
  became a gold star.
- **Dungeon mobs aggro, retaliate and spread out again.** One root cause behind both symptoms: `MobAi`'s
  engaged branch returns early after a leash check, and **nothing ever cleared `Engaged` when the target
  left the world** (`DropAggroOn` was only wired to the stealth path). A mob whose target teleported away
  from the debug menu stayed engaged forever — never re-scanning for aggro, never wandering, just
  standing there, which read as both "they don't fight back" and "they're clamped together". Fixed with a
  live-target guard that retargets by threat or disengages, plus shedding aggro when a player leaves.
  Aggravating factor for the clumping: wander used a flat ±1000 offset and projected anything outside the
  zone **exactly onto the rim**, and the crypt's rooms are radius 300-350 — so every mob walked to the
  same small circle. The span now scales to the room and lands inside it.

---

## 2026-07-24 — EXP/party/drop rework + first playtest-11 fixes (0.28.67)

- **The whole progression curve moved to `Game.Shared/ExpCurve.cs`** — one place for the level curve,
  the mob reward, the SP ratio, the level-difference penalty, the party bonus and the random roll.
  - **Player curve = the real Lineage 2 table, levels 1-100.** Not a formula: its own shape is a power
    law (~8.492·L^3.2891) only to level 50, after which SEVEN multiplicative walls at 51/56/61/66/72/77/80
    stack to ~52x by 85. Levels 1-85 from the masterwork source, 86-100 spliced from 4Game (joining at 86
    reads x1.37; joining at 4Game's own 85 would have jumped x8.6 in one level). 4Game publishes levels
    88/89 transposed — 89 was CHEAPER than 88 — and they are swapped back into order here.
  - **Mob reward** `0.026314·L^3.2427`, fitted to 8.5k/30k/47.5k at levels 50/75/85; below level 30 it is
    interpolated through seven hand anchors so the opening costs 1-2-4-5-5 mobs rather than 295/805/858.
  - **Level-difference penalty** `0.85^(gap-5)`, zero at 13, **symmetric** — fighting up is penalised too,
    which is what stops a level-1 bow last-hitting a level-78 mob. Applies to EXP **and drops**.
  - **Party: shared pot, personal penalty.** `pot = mobValue × roll × partyBonus(n)`, split EQUALLY, then
    each member's own gap penalty applies to their share. The killer no longer gates the party's exp.
    Party bonus 2→x1.2 … 6→x2.0 … 9→x2.3.
  - **±20% random roll** on the final award, one roll per kill shared by the party, covering exp and SP.
  - EXP is `long` end to end. SP saturates at `int.MaxValue` by design — see the SP-bottle plan in
    [Roadmap.md](Roadmap.md).
  - `BalanceMatrix` now prints the full curve plus the gap and party tables, and reproduces
    [balance/ExpCurve.md](balance/ExpCurve.md) exactly: 1 mob for level 1, 20 at level 10, 121 at 20,
    125 828 at 85 — **631 799 to reach 86**, ~136 million to reach 100.
- **Fix: only the FIRST character of the owner's account is born Admin.** Every character on that account
  used to be, which quietly broke the per-character role model — a deliberately ordinary character still
  had every admin command. The role is per-CHARACTER by design; do not move it to the account.
- **Fix: world entry/exit no longer leaks to everyone.** The friend notice was already correctly
  mutual-only; a *separate* global broadcast was the leak. Now behind `AnnounceWorldEntryExit`, off.
- **Fix: `/tp` to a jailed player lands in the JAIL, not a dungeon.** The jail sits in the negative
  quadrant but is not a dungeon, so the dungeon ward grabbed any non-jailed visitor. It is now a
  first-class domain in both the movement wall and the ward.

---

## 2026-07-24 — Inventory boxes + item details (0.28.65 → 0.28.66), and PLAYTEST-11

- **Open boxes from the inventory** (0.28.65) — a plain box grants its contents straight to the bag; a
  **selection** box opens the choice popup and grants only the picked entry.
- **Item-details layout** (0.28.66) — the stat block is no longer crammed under the item name.
- 🎉 **Playtest-11 (0.28.66)** — the owner tested the **whole** `TestChecklist.Unity.md` end to end and
  **§§1-15 all passed**, closing the A–F parity programme, the playtest-10 batch, the world pass and the
  rune shots in a single pass. Exceptions: **Skills→Learn does nothing**, the **soft keyboard covers the
  command bar** instead of lifting it, and the 3h break banner can't be tested in a sitting. 0.28.65 and
  0.28.66 shipped after the test and remain unverified.
  The resulting work — 11 bugs, 16 changes, 5 additions (**block**, **charisma**, **buy-back**), and a
  **level 1-20 starter-gear redesign** — is queued in [Roadmap.md](Roadmap.md) and §17 of the checklist.
  Two design answers came out of it: **partial-stack trading is a YES**, and **admins must be excluded
  from the leaderboards**.

---

## 2026-07-24 — World pass (fields, walls, negative quadrant) + rune shots (0.28.56 → 0.28.64)

- **Whole map on FIELDS** (0.28.56–0.28.58) — field polygons are FILLED and coloured by level (replacing
  the spawn-zone circles); one convex field WRAPS each town with the town drawn on top as an island; a
  boss field (Sunken Vale) + a dungeon field (Hollow Crypt) + a Training Grounds field. All generated as
  convex hulls and verified (no overlaps; every spawner inside its field).
- **No rogue spawners** (0.28.59) — a startup guard throws if any spawner sits outside every field.
- **Dungeons + jail in the NEGATIVE quadrant** (0.28.60) — reached by teleport, off the positive
  overworld; position clamps + the (sparse) cell grid handle negative coordinates.
- **Walls** (0.28.61) — movement is confined to the domain you stand in: the overworld can't be walked
  out of into negative space, and a dungeon can't be walked out of; a ~500u ward teleports a clip-out
  back inside. Teleport is the only way across.
- **War/Spell Runes as RUNES** (0.28.62–0.28.64) — the always-on training passive is gone; shots are
  held rune items with a wall-clock expiry (persisted, counts down offline, delete-protected). Delivered
  in boxes whose open stamps the clock (also stamped on any other acquire). 1h/2h at the Apothecary
  (tradable), 24h/30d premium/debug (bound); admin seeds both 30d. The newbie starter kit is now
  class-agnostic (armor choice box, one weapon of five incl. staff, a 1-day shot-rune choice box).

---

## 2026-07-23 — Playtest-10 fixes, potion rework, dungeon, regions, leaderboards (0.28.42 → 0.28.55)

Driven by on-phone playtests over VPN. Every entry verified by a headless Unity compile + `dotnet
build`; the server/client were rebuilt between rounds, never mid-test.

- **Playtest-10 round 1–2** (0.28.42–0.28.46) — click-through fixed (the press, not the release,
  decides whether a tap was over UI); the party window stops going stale on a member's leave/kick
  (client clears transients on entry, server pushes an empty party on entry); speed=1 rubber-band
  fixed (`ToLean` sends `EffectiveSpeed`, so walk/slow/stun predict correctly); **sit mechanics**
  (sitting freezes movement, standing has a recovery window); `/tpme`; **change-leader**; a 250-slot
  bag where **worn gear takes no slot** and unequipped gear lives in the Items bag; a hidden-by-default
  fast-delete toggle; party **buff/debuff view**; floating combat text for buffs/heals; the loot-vote
  bot for headless party tests.
- **Auto-farm range ring** (0.28.47) — a ground circle showing the search radius.
- **Flat heal-over-time potions** (0.28.48–0.28.49) — three tiers (Common/Uncommon/Rare) heal a FLAT
  amount over time as an ordinary buff, plus a separate **instant** panic potion that does not cancel
  them. An Auto-Potions **Potions tab** picks the tier per HP threshold, and potions can go on the
  quick-use bar as `item:<id>` tokens.
- **Equipment presets + paper-doll** (0.28.50) — save/restore worn gear as A/B/C loadouts (server
  refuses in combat), persisted in a new `EquipPresetsJson` column.
- **Hollow Crypt dungeon** (0.28.51) — elite rooms + a boss in the NW corner off the town ring, with
  an entrance safe zone; any gatekeeper offers it and the existing engine runs it.
- **Regions stage 2** (0.28.52) — towns became polygons; the safe zone is the UNION of the old circle
  and the town polygon; "you entered X" entry banners; region outlines on the ground.
- **Stand-up no longer rubber-bands** (0.28.53) — the recovery window gates actions, not movement, so
  standing never zeroes your speed under the client's prediction.
- **Leaderboards, break reminder, non-overlapping regions** (0.28.54) — a Menu → **Rank** window with
  five boards (Level / Wealth / PvP / PK / Time played), read from the DB off the loop; the #1 of each
  earns an honorary title. A **3h "take a break"** banner every 3h of continuous play (persisted
  online time). Field polygons pushed clear of the town safe-circles and town octagons inscribed in
  the circle, so regions no longer overlap (verified by a geometry script).
- **Equipment folds into the bag; every target command is a button** (0.28.55) — the standalone
  Equipment window is gone; the bag's **Equip** toggle expands the window to reveal a compact
  paper-doll column with the presets. Follow/Assist (the server always had them) and
  Trade/Party/Target-closest now work from the bar dispatcher, and the target frame shows a contextual
  Attack / Follow / Assist / Party / Trade / Info grid.

---

## 2026-07-23 — Unity↔WPF functional parity: batches A–F (0.28.35 → 0.28.41)

The program to bring the Unity mobile client to *functional* parity with the WPF harness (agreed
2026-07-21) reached all six batches. One batch = one commit; every batch verified by a headless
Unity compile (the `.sln` does not build the Unity assembly).

- **Debug window at full parity** (0.28.35) — six tabs (Equip / Items / Func / TP / Class / Tune),
  read from the catalogs rather than hand-listed.
- **Trade + party invite** (0.28.36) — both were already server-side; only the client half was
  missing. `Boot.PartyInvite` existed and nothing had ever called it, so party was untestable from
  the phone. Trade redraws only from the server's push (never optimistic — that's how players get
  robbed).
- **Auto-hunt setup, two windows** (0.28.37) — Auto-Potions (HP/MP % + on/off) and Auto-Farm (search
  range, keep-position, engage Normal/Elite/Boss), both opened from the Menu; the on/off stays on the
  top-right Auto button. Fixed a bug where the Auto toggle hardcoded potion/farm defaults and silently
  wiped configured settings on every enable — the client now caches the server's config and every path
  preserves the half it doesn't own.
- **Mob info at character depth + a lazy Drops tab** (0.28.38) — `TargetDetails` extended with the
  full stat layer (attributes, speeds, range, crit/vamp/regen/resist, rank). The window is two tabs;
  the drop table is fetched **once** when the Drops tab is first opened and not again until reopened.
- **Inventory rework** (0.28.39) — bag rows are `name (qty) [Details] [e|u]`; a details window carries
  full enchant-scaled stats, attributes, use-skill and set info, with per-kind actions, a bin-delete,
  and an equipment **Compare** (the worn counterpart opens alongside, marked with an orange E).
  Introduced a reusable **selection popup** (titled list of choices).
- **Vendors** (0.28.40) — the vendor asks Buy or Sell; buy lists wares, sell lists your sellable bag;
  a stackable item opens a **numpad** (digits / clear / backspace / keyboard box / Max) and every deal
  ends in a plain-text confirm. Selling was impossible from the phone before this.
- **Learn confirmation** (0.28.41) — the Skills → Learn tab no longer spends SP on one tap; a confirm
  window shows the change (power/MP before→after for an upgrade, or the level-1 numbers for a new
  skill) plus the cost.

Friends needed no work — `/fadd /frem /flist` already matched the WPF client (which has no friends
window). Deferred to the potion rework: the 3-tab auto-potions expansion. See
[Roadmap.md](Roadmap.md) for the 2026-07-23 design ideas (flat-HoT potions, auto-farm skill priority).

## 2026-07-20 — Level privacy, regen cadence, Spirit, and the Unity client

- **Unity mobile client, first playable pass** — a login / character-select / in-world HUD, fixing
  two bugs that had made it blind: it listened for the old full `Snapshot` event while the server
  now sends only deltas, and the project was set to the new Input System while the code used the
  legacy one. Also: an on-screen log console, a connection-liveness strip, and an `adb reverse`
  cabled-phone workflow. Fixed an IL2CPP build break (positional-record `GetHashCode` nested past
  clang's bracket limit) so the Android build compiles.
- **Level is private** — you see your own level and monsters' levels; other players' levels are not
  sent at all, only shared inside the party window. Enforced server-side.
- **L2 regen cadence** — health/mana regenerate in larger chunks every 3 seconds rather than a
  trickle every second, with CON weighted much harder. Damage-over-time and heal-over-time keep
  their own 1-second tick.
- **Spirit (SPT) is a full stat** — the fifth core stat (CON/ATK/WIT/DEX/SPT), driving max mana,
  mana regen and magic defence; stored per-subclass and persisted.
- Debug seed accounts, an automatic stale-DB rebuild, and LAN binding so a phone can reach the
  server.

## 2026-07-17 — Moderation, social, versioning, delta snapshots

- **Account roles + moderation** — persistent, timed jail / kick / ban, authorized server-side and
  shipped (not debug-gated), with a moderator role beneath admin.
- **Friends and social** — a mutual friends list with pending requests, follow/assist, admin
  teleport, and a target-window action menu (trade / invite / follow / assist).
- **Protocol versioning** — a login handshake rejects out-of-date clients, with a
  MAJOR.MINOR.BUILD version (0.27.0).
- **Delta snapshots** — each tick sends only spawns / changed fields / despawns instead of the whole
  visible world, cutting per-tick bandwidth sharply.
- **Skill-bar overhaul** — a 60-slot, multi-row, movable bar that also holds usable items; the
  server now owns bar auto-placement (a long-standing silent-corruption bug), and stops
  auto-placing learned skills at all. A placeable Actions catalog (attack / sit / follow / …).
- **Mob target window** — a compact card with a `[Details]` tab that fetches drops only on demand.

## 2026-07-13 — Damage model, death & resurrection, heals

- **Combat retune** — L2-style damage constants, with the weapon (not the class) deciding the
  physical/magic split, and hidden per-class stat grants removed.
- **`{Flat, Mod}` skill damage** — the foundation for physical skills scaling off attack power,
  landed backward-compatibly.
- **Heal rework** — heals no longer key off magic attack; they use dedicated HealPower /
  HealReceived stats, with no default overheal.
- **Death penalty & resurrection** — dying costs 5% of the level's experience (no de-level), with
  newbie protection below level 40; resurrection arrives as a four-level cleric skill plus ally-res
  scrolls with an accept/decline prompt, and Angel's Protection preserves buffs through death.
- **Everything is a skill** — potions and scrolls now cast skills, unifying consumables with the
  buff system; the buff bar groups by subtype.
- **Level-40 stat-swap passives** — the one thing that shifts a character's main stats.

## 2026-07-08 — Disconnect handling, PvP, auto-hunt, live tuning

- **Auto-hunt / idle farming** — the character farms while idle and, time-capped, while offline,
  reusing the same targeting/skill logic; with roaming bounds, a rank filter and a skill-priority
  order.
- **PvP system** — an L2-style flag / karma / player-kill system with enable and counter-attack
  toggles, self-defence gating, karma you grind off on mob kills, and a 15k cap.
- **Disconnect / exit state machine** — a combat state that gates logout, a disconnect grace window
  vs. offline-farming split, and universal Return skills + scrolls.
- **Debug tuning panel** — live, admin-only editing of rates / karma / caps in-game.
- Settings persist next to the executable; window size and position are remembered.

## 2026-07-01 — Gear, crafting, mobs, and the stat-modifier refactor

- **Tiered gear overhaul** — redesigned weapons, armour, shields, accessories and jewels as
  level-tiers with rolled attributes and named set bonuses; the old procedurally-generated gear was
  dropped in favour of these as (rare) mob drops.
- **Crafting economy** — materials (five types × five rarities) drop from mobs and feed a
  cross-profession crafting chain up to finished set pieces, with recipe books, professions, and a
  boss/elite drop bonus.
- **Mob overhaul** — an 80-mob roster with a CSV-driven base-stat curve, weapon-type resistances,
  and ranged / caster mob roles with their own spells.
- **StatMods refactor** — a single unified stat-modifier layer carries all item, set and mastery
  bonuses (compounding percentages), replacing the old mastery formula with explicit per-level data.
- A CC-resist stat, and the first Unity client slice (making `Game.Shared` consumable by Unity).

## 2026-06-28 — Combat primitives and the effect engine

The deep combat toolkit most later skills are built from: conditional and burst damage;
damage-over-time with a proper stack/effect model (poison, venom, bleed); contested crowd control
(stun, fear, root, slow); movement effects (blink, knockback); a defensive cluster (absorb shields,
mana shield, lethal save, cancel/dispel and cancel-resist); real threat/aggro with taunt (replacing
last-hit aggro); the physical-skill damage-out pipeline (PvE/PvP × skill/magic/basic); and immortal
training dummies for testing it all. Playtest-1 fixes landed alongside.

---

## Phase 24.1

### Lightbringer — the first fully-authored 3rd-class discipline

The pure-heal Healer discipline, built across all three races. One shared idea (keep
the party alive), three race expressions — proving the *discipline + race* model from
24.0 with real, distinct skills:

- **Human Lightbringer** — single-target powerhouse: **Mending Light** (strong, fast
  heal) + **Purify** (cleanse harmful effects from an ally).
- **Elf Lightbringer** — area coverage + control: **Dawn Bloom** (heals *and* cleanses
  all nearby allies) + **Warding Step** (roots an enemy for 8s and sheds the caster's
  aggro from nearby foes).
- **Ork Lightbringer** — area + suppression: **Spirit Font** (AoE heal — a stand-in
  until placed totems arrive) + **Soul Sap** (anti-heal: the target recovers only half
  the HP from any healing for 15s).

### New combat mechanics (engine)

Added cleanly to the `[Flags]` effect system, reusable by future disciplines:

- **AoE heal** — heals scale to all allies in radius (the healer included).
- **Cleanse** — strips curses, anti-heal and roots off an ally.
- **Anti-heal** — a debuff that reduces healing received (`HealReceivedMultiplier`).
- **Root** — holds a target in place (movement → 0) for the duration.
- **De-taunt (stub)** — nearby mobs drop the caster and won't re-aggro it for ~5s.
  A real threat system replaces this later.

> The Ork's **placed healing totem** (and pets/summons generally) is deferred to a
> dedicated subsystem; it ships here as a normal AoE heal. **Warchanter** (the buffer/HoT
> Healer discipline) is the next slice. No DB reset needed for this build.

## Phase 24.0

### 3rd-class framework (disciplines)

The plumbing for the whole 3rd tier — content (real per-discipline skills) lands in
later slices; this build proves the pipeline end-to-end with placeholder skills.

- **Each 2nd class splits into two disciplines at level 40.** A *discipline* is the
  shared identity; *discipline + race* is how it's expressed. 12 disciplines, 36 third
  classes (× 3 races): Tank→**Bulwark/Vanguard**, Warrior→**Ravager/Warlord**,
  Rogue→**Phantom/Venomweaver**, Archer→**Sharpshooter/Trapper**,
  Healer→**Lightbringer/Warchanter**, Nuker→**Magus/Tempest**.
- **Earned by a longer, harder quest chain** than the 2nd class: at level 40,
  **Grandmaster Thorne** sets the *Ordeal* then the *Ascension* — multi-target hunts
  through the high-level zones, capped by **Young Drake** kills (a stepping stone to
  real bosses later). Only offered for the 2nd class you hold, and only one discipline
  per character.
- Each discipline carries a **flat stat lean** (e.g. Bulwark = big +HP/+Def; Magus =
  +MP/+Atk) so the paths already feel different before their skills exist.
- Plumbed through the engine: `Entity.ThirdClass`, a `Discipline` dimension on the skill
  registry, the tiered class-change handler/dialog (gated to the right parent class), a
  level-40 reminder, and a new `ThirdClass` save column.

> **Schema change:** delete `Game.Server/bin/Debug/net8.0/game.db` (+ `-shm`/`-wal`) so
> the new `ThirdClass` column is created. The 4th tier (gold-gated + boss kills) comes later.

## Phase 23

### Class change is now earned through quests
- Every one of the **18 second classes** is unlocked by a **two-quest chain**, generated
  uniformly for all of them (no more level-only popup):
  - **Trial of the &lt;Class&gt;** (lvl 18, **Elder Marius**) — hunt a target, earn the
    **Trial Token**.
  - **Path of the &lt;Class&gt;** (lvl 20, requires the trial, **High Priest Oren**) — a
    second hunt, earn the **&lt;Class&gt;'s Proof**.
  - Bring both to **Class Master Vael** to change class (the proofs are consumed).
- **Offers are gated to what you can actually become**: a quest (and the class-change
  dialog itself) only appears for your **race + base class**, and only before you've
  already taken a second class. No seeing 18 irrelevant options.
- The lvl-20 reminder now **points you at the right NPCs** instead of saying "coming soon".
- Built as a **data-driven generator** (`Quests.ClassChangeChains.cs`) over
  `ClassCatalog.Playable`, so it produces all chains, the 36 quest items, and the 18
  class-change requirements from one loop — the same generator will drive future 3rd/4th
  tiers.

> Quest progress persists (existing columns), but **delete `game.db`** if you want the new
> quest items to seed cleanly on a fresh character.

## Phase 22.2

### Original town names (IP safety)
- Renamed every town away from Lineage-2 names to **original, generic** ones:
  **Brackenford** (starter), **Stonewatch**, **Emberfall**, **Greymarsh**, **Ironreach
  Keep**, **Duskvale**, **Frostmere**. Safe-zone ids and gatekeeper ids were renamed to
  match; nothing persisted referenced the old ids, so **no DB reset**.
- Also scrubbed the L2 currency term ("adena") from the docs — the currency is **Gold**.
- Policy going forward: never reuse names trademarked by other games (towns, NPCs, items,
  skills, currency). Stat *formulas* aren't copyrightable; *names* are.

## Phase 22.1

### Expanded world — zones up to level 80
- The world grew to **24000 × 24000**. The starter town **Brackenford** sits at the centre,
  with **six more towns ringing it** (Stonewatch, Emberfall, Greymarsh, Ironreach Keep,
  Duskvale, Frostmere) — difficulty rises as you tour the ring clockwise from the north.
- **~16 spawn zones cover levels 1-80** (1-2 per band), plus an elite and a boss
  placeholder. Four new higher-tier creatures (**Orc Raider, Stone Golem, Wraith, Young
  Drake**) fill the 20-80 range with their own drop tables; existing mobs cover the low end.
  There's no level cap, so you can climb toward 80 to be ready for class-change quest chains.
- **Every town has a gatekeeper**, so the whole travel network is reachable in both
  directions; teleport fees scale with the (now larger) distances.
- **You respawn at the nearest town** instead of the map centre — important now that the
  world is big.
- Bigger map = room for the next content: bosses, instances, and dungeons.

> No DB reset needed. Existing characters keep their saved position (clamped into bounds);
> walk or use a gatekeeper to reach the new towns.

## Phase 22

### Teleport-for-fee (gatekeepers)
- A **Gatekeeper** NPC stands in each safe zone (Giran / Dion / Aden). Talk to one to
  **pay gold and warp** to any other safe zone.
- The **fee is distance-based** (`GameConstants.TeleportFee` = distance × per-unit rate,
  with a floor), shown on each travel button; you can't afford → button disabled.
- The server validates range, gold, and that the destination isn't your current zone,
  then repositions you (like respawn: set position, clear path, update the grid). The
  client **snaps** the camera/sprite on a large jump instead of sliding across the map.

## Phase 21

### Vendors — NPC shops (buy & sell)
- Two town merchants: **Apothecary Miren** (potions, common buff potions, basic scrolls)
  and **Armsmaster Dolan** (plain F-grade weapons/armor/accessories, starter shield + jewel).
  Talk to one → **Browse Wares** → a shop window with **Buy** and **Sell** tabs.
- Every item now has a gold **Value** (`ItemDef.Value`; filled by an explicit per-item value
  or `ItemCatalog.DefaultValue` formula by grade/rarity/slot). **Buy price** = Value;
  **sell price** = Value × `VendorSellFraction` (30%). Quest items and god-tier one-offs
  have no value → can't be bought or sold.
- Vendor-sold gear is created **plain** (no rolled attributes) — rolled gear still comes
  from drops. The server validates gold, inventory space, range, and that the vendor
  actually stocks the item; it's all single-writer on the game loop.
- **Castle hook (not yet active):** `VendorBuyTaxRate` is wired into the buy price so a
  future castle-owned village can add a surcharge that flows to the castle vault. It's 0
  for now (no castle system yet).

## Phase 20

### Buff potions (a few buffs without a buffer)
- A new **buff-potion** type: drinking one applies a **timed buff** (weaker than a real
  class buff), so a solo / non-buffer character can still grab a couple. They **ignore the
  healing-potion cooldown** and don't heal.
- Three lines, each in **three rarity tiers** (rarity = strength + duration; a rarer one
  supersedes a weaker of the same line):
  - **Swiftness** — +15 / +20 / +30 Move Speed
  - **Focus** — +8% / +12% / +20% Cast Speed
  - **Haste** — +8% / +12% / +20% Attack Speed
  - Durations 60s / 90s / 180s (the rare lasts longest).
- Tooltips show the effect; the debug menu grants them.

### Scrolls & buff potions now drop
- **Attribute (reroll) scrolls** now drop from monsters — rare to find, like enchant
  scrolls — editable per mob in `MobCatalog`. Higher tiers are weighted to higher-level
  spawns (e.g. rare reroll scroll only at level 20+).
- **Uncommon/Rare buff potions** drop too; the **common** tiers are reserved for vendors
  (next phase). So drops give the strong buffs, vendors the basic ones.

### UI: tabbed debug menu + settings + class-change via quest
- The **debug menu** is now split into three tabs — **Equip** (weapons / armor / sets),
  **Consumables** (scrolls / potions), and **Functions** (Level +1, Class Change test) —
  instead of one long scroll.
- The old top-bar **Class** button is replaced by a **Settings** button (menu: *Character
  Selection* — leave the world, save, and pick another character without re-logging in;
  *Exit Game*).
- **Class change now belongs to a quest.** The direct class-change picker moved into the
  debug *Functions* tab (test bypass). Normal players hitting level 20+ without a second
  class get a one-time popup pointing them at the (not-yet-built) class-change quest — a
  temporary stub to be removed once those quests exist.

## Phase 19

> **Delete `game.db` before running** — characters gained a **Gold** column;
> `EnsureCreated` won't add it to an existing DB, so reset it (saves recreate fresh).

### Gold — a currency wallet
- Characters now have a **gold wallet** (persisted). Mobs **drop gold on every kill**,
  scaled by mob level × `RateConfig.GoldAmountRate` with a small ±20% variance
  (independent of the item drop table).
- The balance shows in the **status line** (e.g. *“Lv5 • 1,234 Gold”*) and syncs on
  login and whenever it changes.
- The currency name is **generic on purpose** (no IP) and centralised in
  `GameConstants.CurrencyName`, so it can be rebranded in one place.
- This is the foundation for the roadmap's **vendors** (buy/sell) and **teleport-for-a-fee**
  — the wallet gives the reroll scrolls and potions a real source/sink next.

## Phase 18

### Armor-weight masteries (wear what your class trains in)
- Your class is **trained in an armor weight**. Wearing the **body** piece of that weight
  grants a **bonus** (your class identity); wearing an **untrained** heavy/light body
  applies a **penalty**. **Robe never penalises**, and **Tanks/Warriors take no penalties**.
- The trained weight follows the class tree (base → second class):
  - **Mage → Robe** (cast speed, MP regen, max MP); **Nuker** keeps robe (stronger, +interrupt
    resist + magic def); **Healer → Light** (cast + atk speed, regen, a little acc/eva — so it
    can melee).
  - **Fighter → Light** (attack speed, HP regen, acc/eva); **Rogue → Light** (more atk/move
    speed, eva/acc); **Archer → Light** (atk speed + crit); **Tank → Heavy** (max HP, regen,
    big Def + magic resist, no penalties); **Warrior** trains **both heavy and light**.
- **Penalties** for the wrong weight: a mage in **heavy** is crushed (≈½ attack/cast/move
  speed and regen, −10 acc/eva); a fighter in heavy is milder (×0.8 speeds, −3). Wearing
  **light untrained** is a lighter ×0.8 / −3.
- The **Stats window** shows your current status (e.g. **“Robe Mastery”** or
  **“Heavy — untrained”**) so the effect is visible.
- Driven by class + archetype for now (encodes the class-change evolution); a **learnable,
  leveled mastery-skill layer** (spend SP, ranks) comes later, like the other passives. No
  DB reset needed.

## Phase 17

### Named armor sets with a set bonus
- Armor pieces can belong to a **named set** (`ItemDef.SetId`). Wearing **all four armor
  slots** (Head / Body / Gloves / Boots) of the same set grants that set's **bonus** on
  top of each piece's own stats and rolled attributes.
- A set can offer **several body weight variants** that share the same accessories — so a
  set has a *heavy* and a *robe* body, both completed by the same helm/gauntlets/boots.
- **Dark Dominion** ships as the first set: a **Plate** (heavy) or **Robe** body + shared
  Helm / Gauntlets / Sabatons. Full set → **+150 HP, +80 MP, +25 Def, +18 Atk** (feeds
  both physical and magic), **+6 Acc, +6 Eva** (tune in `ArmorSetCatalog`).
- The **Stats window** shows **“Set Bonus: <name> (complete)”** when active, and an item's
  **tooltip** lists the full-set bonus so you know what you're collecting toward.
- Sets are defined in `Game.Shared/ArmorSets.cs`; the debug menu grants both Dark Dominion
  variants. Set pieces are obtainable via debug for now (boss drops later). No DB reset
  needed this build.

## Phase 16.1

> **Delete `game.db` before running** — armor keys changed again
> (`<weight>_<slot>_<grade>_<rarity>`, Legs slot removed). A fresh DB hands new
> characters the new starter set.

### Fewer armor slots: one weighted body + weightless accessories
- Trimmed from five slots to **four** — **Head / Body / Gloves / Boots** (legs merged
  into one **full-body** piece) to cut the number of generated items (~90 → ~36).
- **Only the Body piece carries weight** (Heavy/Light/Robe) and the bulk of the defence.
  **Head / Gloves / Boots are weightless accessories**, generated once and **shared across
  all builds** (so a "set" can reuse the same accessories and differ only in its body
  armor by weight).
- **Per-slot attributes** (grade/rarity sets the *value*, not the count):
  - **Body** rolls **2** attributes from its weight pool — so it wants **Uncommon+**
    attribute scrolls (lock 1, reroll 1), not just Common.
  - **Head** rolls HP/MP regen · **Gloves** atk-speed/cast · **Boots** move-speed/eva —
    **1 each**.
- New characters start with **body + the three accessories**; the debug menu grants Rare
  body+accessory sets per weight.
- Still to come (the set phase): **named sets** with a **set bonus** (+CON/ATK, +max
  HP/MP) for wearing a matched body+accessories, and **armor-weight masteries**.

## Phase 15

### Attribute reroll scrolls (keep the item, fix the rolls)
- A new scroll family lets you **reroll an item's rolled attributes** instead of
  tossing a good grade/rarity piece because its stats are weak. Each reroll
  re-randomises the **unlocked** slots (both stat type and value, from the item's
  Phase-14 pool).
- **Lock-by-tier** — the scroll's rarity decides how many slots you can **keep**:
  - **Common** — lock 0 (reroll all)
  - **Uncommon** — lock 1
  - **Rare** — lock 2
  - **Legendary** — rerolls **all** slots and forces each to its **MAX** value (for a
    legendary item whose every stat should be maxed).
- **How to use it:** click the **⟳** button on any gear with attributes → the reroll
  popup lists each attribute with a **lock checkbox** → tick the ones to keep → pick a
  scroll. The server clamps your locks to the scroll's capacity, consumes one scroll,
  and re-applies stats live if the item is equipped.
- Enchant and attribute scrolls are now **distinct families** (an attribute scroll can't
  be used to enchant and vice-versa). Debug menu grants all four attribute scrolls.
- Drop chances aren't tuned yet — for now the scrolls come from the **debug menu**.

## Phase 14

### Rolled attributes now depend on the item, not just its grade
- **Which** attributes can roll is decided by the **weapon type / armor weight**, not a
  flat grade pool. **How big** a roll is still scales with **grade**; **how many** still
  comes from grade + rarity.
- **Pools** (`AttributeSystem.WeaponPool` / `ArmorPool`):
  - **Sword** as/atk/crit-rate · **Blunt** hp/atk/cast/crit-dmg · **Bow**
    crit-rate/crit-dmg/as/atk · **Dual** crit-rate/crit-dmg/move-speed/eva.
  - **Heavy** hp/as/hp-reg/acc · **Light** the versatile set (eva/acc, hp+mp regen,
    hp/mp, as/cast) · **Robe** cast/mp-reg/max-mp.
- **Five new attribute types**, all feeding real stats: **Accuracy**, **HP Regen**,
  **MP Regen** (flat), **Crit Rate** and **Crit Damage** (percent). Crit-rate from gear
  adds **on top of** the weapon crit factor; crit-damage raises your crit multiplier.
- Flat attributes (accuracy, regen) display **without** a `%`; percent ones keep it.
- Groundwork for **Phase 15 — attribute reroll scrolls** (lock-and-reroll toward each
  stat's max), so a good grade/rarity item is worth keeping and grinding, not tossing.

## Phase 13

> **Delete `game.db` before running** — a staff's item key changed
> (`staff_*` → `blunt_*`), so existing mage starter staves won't resolve; a fresh
> DB regenerates correct starter gear.

### Magic defence is its own channel
- **New magic-defence stat**, fully separate from physical defence: magic damage now
  divides by **`MagicDefence`**, not physical `pDef`. Base = **`level / 2`** (the
  physical formula minus the CON term — magic defence does **not** scale with any base
  stat).
- **Only JEWELS raise magic defence.** New **`EquipSlot.Jewel`** + an item `MDefBonus`;
  two starter jewels seeded (Brass Amulet, Silver Talisman). One jewel equips for now,
  built to expand to the L2 five-slot layout later. M.Def shows in the Stats window and
  the equip-comparison popup.
- **Tank "Anti Magic"** (archetype passive) adds extra magic defence on top of the base.

### WIT is purely a combat-utility stat
- WIT still drives **magic crit**, **cast speed**, and **interrupt resist** — and now
  also **offensive magic-interrupt power** (`wit·2`), so a **WIT mage out-interrupts an
  equal-level ATK mage** while the ATK mage hits harder. WIT adds **no** magic damage.

### Magic fail — floor and ceiling
- A spell can always fizzle (**≥1%**), scaling up by level gap to **90%** (was 80%).
- The **target** can raise the fail **floor** against itself: **Tank ~10%, mages ~5%**
  — so casters always have a real chance to fail against the prepared.

### Interrupts
- **Rogue basic attacks** now carry magic-interrupt power (`50 + level`) — daggers
  disrupt casters. Other archetypes' basics still don't interrupt.
- New **Disrupt** skill (Tank kit): **instant cast**, overwhelming interrupt power, so
  it **always breaks** an enemy cast.

### Weapon system — Blunt, one/two-handed, shields
- **`Staff` is gone — a staff is just a 2H Blunt** (`WeaponType.Blunt`). Blunt =
  **higher accuracy, lower crit** than bladed weapons.
- **One- vs two-handed** (`WeaponHands`) is now a real property. **A 2H weapon occupies
  the offhand**, so equipping a 2H weapon and a shield are **mutually exclusive** (one
  drops the other).
- **Per-weapon crit factor** (Sword ×0.80, Dual/Bow ×1.20, Blunt ×0.40) shapes crit by
  weapon; **Blunt also gets +accuracy** — the high-acc/low-crit identity.
- **1H magic blunts** let a mage trade a staff for **mace + shield**: hand-added
  **Iron Mace** (physical, shield-ok) and **Ash Wand** (1H magic blunt, mAtk > pAtk).
- **Daggers are consistently `Dual`** (no phantom `Dagger` type); fixed a mob drop that
  referenced a non-existent dagger key.

## Phase 12

> **Delete `game.db` before running** — characters now store shield-related
> equipment state correctly only on a fresh DB if you hit schema issues; safe to
> reset.

### Shields & block
- New **Shield** equip slot + item type with several values: **BlockChance**,
  **BlockReduction%**, **ShieldDefense**, **ShieldCritDefense**, **EvasionPenalty**.
  Two shields seeded (Wooden F, Iron E). Any class can equip a shield.
- **Block resolution** (physical only): the shield first lowers the attacker's
  **crit chance**; if it still crits, the **crit ignores the shield**; if it
  doesn't crit, **roll block** → on a block, damage is cut by the shield's flat
  **% reduction**. Shown as a "Block" hit on the client. **DEX does NOT affect
  block** — it's flat + passives.
- **Shield Mastery** (tank skill) scales the shield's block chance and defence —
  but only while a shield is equipped, so a buffed shield on a mage is still weak
  while a passive-stacked tank becomes a wall.
- Skills can carry **BlockAccuracy** to bypass blocks (most physical skills should).
- **Magic is not blocked** — it's mitigated by defence only, so mages aren't
  buried under fail + interrupt + block.

### Combat-feel fixes
- **Damaged mobs now aggro and chase** their attacker even when hit from range
  (the "cast from range, mob ignores you and regens" bug).
- **Magic weapons have no weapon range** and tiny basic-attack damage — a staff is
  useless as a melee poker, so you actually cast. Only **bows** have basic range.
- **Skill ranges scale by class tier**: magic **500 / 750 / 900** (lvl 1-20 /
  21-40 / 40+), bow skills **350 / 600 / 900**. Archer **basic-attack** range grows
  by tier too (400 → +200 → +500).
- **Faster casts**: Magic Bolt **2s**, Flame/Holy/Heal quicker; **instant debuffs**
  (Weakness 0.5s cast, 15s duration, 30s cooldown).
- **HP Boost ranks replace lower ranks** — learning rank 3 removes ranks 1 & 2 from
  your learned skills, and the active buff supersedes by rank.
- **Daggers are treated as Duals** (`WeaponType.Dual`) consistently.

### For Claude Code
- Added **`CLAUDE.md`** at the project root — full architecture, conventions, and
  design decisions so Claude Code starts with context. Install Claude Code with the
  native installer (`curl -fsSL https://claude.ai/install.sh | bash`, or the
  PowerShell one-liner on Windows), `cd` to the project, run `claude`. It can run
  `dotnet build` and fix real compile errors directly.

## Phase 11

### Casting commits you (root)
- Starting a cast **roots you** — you can't move until it finishes or you cancel.
  Range is checked at cast **start** only; once it begins, the spell **lands even
  if the target moves**. This removes the old move-cancel/recast loop.
- **ESC** cancels your own cast and starts its cooldown (you chose to bail).

### Interruption is a stat contest (not automatic)
- Being hit mid-cast **rolls** an interrupt, like accuracy vs evasion:
  **caster InterruptResist** (WIT-based stat + the skill's `InterruptDefense`)
  vs **attacker InterruptPower** (0 for normal hits + the attacking skill's
  `InterruptPower`).
- **Enemy interrupt = cast stops, NO cooldown** — you keep the MP loss and can
  retry immediately (so a 60s-cooldown ultimate isn't wasted by one unlucky hit).
- Per-skill tuning: `InterruptDefense: 99999` = effectively **uninterruptible**
  (ultimates); `InterruptPower: 99999` on an instant skill = a reliable
  **interrupt skill**. Both default 0 (use the character stat). Hooks reserved
  for gear/buff interrupt-resist later.

### Two-stage MP cost (toggle-skill groundwork)
- A skill can charge `InitialMpCost` at cast **start** and the remainder on
  **completion** (default: all on finish, so existing skills are unchanged). On
  cancel/interrupt you've paid the initial but not the finish — groundwork for
  toggle skills (initial cost + per-second upkeep) later.

### Cast & attack speed (L2-style 333 = 100%)
- New speed model: a stat where **333 = 1.0×**, higher = faster. **WIT drives
  cast speed**, **DEX drives attack speed**, with **per-class weights** (mage WIT
  ~5%/pt, fighter ~3%/pt) and **weapon base speeds** (dagger fast, bow slow,
  staff caster-normal). Approximated from the L2 tables — tune in
  `StatCalculator` (`CastSpeedStat`, `AttackSpeedStat`, weapon base speeds).
- Capped via `StatCaps` (cast 1999 ≈ 6×, attack 1500 ≈ 4.5×). WIT now makes a
  mage a **faster caster** (and magic-crit-prone), not a bigger nuker.

## Phase 10.1

### Level-banded drops
- `DropEntry` gained an optional **level band** (`MinLevel`/`MaxLevel`, 0/0 = any
  level). A drop only rolls when the mob's spawned level is in range — so **one
  creature can drop different loot at different levels** (e.g. `grey_wolf` drops
  common potions at any level but a better armour only at level 15+).
- This is a **superset** of the L2 approach: you can still author the pure-L2 way
  (distinct creature per level tier, no bands) AND the flexible way (one creature,
  level-varying loot), and mix them freely. The level check costs a couple of
  integer comparisons per drop entry — negligible next to the network send on a
  kill, so choose between styles on design clarity, not performance.

## Phase 10

### Placed safe zones (cities/castles)
- The single center safe zone is now a **list of placed zones with ids** in
  `WorldMap.SafeZones` (Town of Giran, Town of Dion, Aden Castle seeded). Each has
  a stable id so **teleports-for-a-fee** can target them later. `InSafeZone` now
  checks the whole list; all are drawn and labelled on the map.

### Server rate multipliers (`RateConfig`)
- One place to tune progression speed: **ExpRate, SpRate, DropChanceRate,
  DropAmountRate** (adena rate reserved for the currency phase). Defaults are set
  for fast testing (**x10 exp, x3 drop chance**) — set them to 1 for live.

### Mobs are now templates with per-mob drop tables
- Mobs are **distinct creatures by id** (`grey_wolf`, `brown_boar`, `dire_boar`,
  `green_slime`, `cave_spider`, `road_bandit`) in `MobCatalog`, each with its own
  **drop table**: `DropEntry(itemId, chance (float), minQty, maxQty)`. The same
  item can drop at different chances/amounts from different mobs.
- **Level lives on the ZONE, not the mob.** A mob template has no fixed level —
  the spawning zone assigns it (stats derive from that level), so the same
  creature appears at any level with the same drops. Want different loot? Make a
  new mob id. Want it tougher elsewhere? Spawn it in a higher-level zone.
- Zones now list **mob ids** instead of generic names. Drop chance/amount are
  scaled by the server rates on top of each entry's own values.

### Skill SP costs rescaled (L2 scarcity)
- Learnable skills now cost **hundreds–thousands of SP** (HP Boost 1000/3000/8000,
  Wind Walk 1500, Mass Wind Walk 5000) so the SP economy forces **prioritization**
  — you can't learn everything at once; you farm and choose. The SpRate multiplier
  makes testing fast without changing that balance.

### Where to tune
- **Cities:** `WorldMap.SafeZones`. **Rates:** `RateConfig`.
- **Mobs + drops:** `MobCatalog` (templates + drop tables). **Zones:** `WorldMap.SpawnZones` (mob ids + level band).
- **SP costs:** each skill's `SpCost` in `Skills.cs`.

## Phase 9

### Damage is now a ratio, not a subtraction
- Old model was `max(atk - def, 0)` — a wall once defence ≥ attack. **New model
  is L2-style ratio damage**: `K · (atk · lvlMod + power) / def`. Defence gives
  **diminishing returns** (never fully blocks), attack always does something, and
  damage **scales smoothly with level** via `lvlMod = (level+89)/100`.
- **Weapon variance**: each hit rolls a ± band by weapon type (bow/dagger spiky,
  blunt steady), so hits aren't identical.
- Tuning lives in `StatCalculator` (`PhysicalK`, `MagicK`, the formulas).

### Two damage channels (physical vs magic)
- **One power stat (ATK)** feeds **both** `pAtk` (physical) and `mAtk` (magic) —
  no separate INT. **Weapons decide the split** via a new **`MAtkBonus`**: a staff
  is mostly mAtk, a sword mostly pAtk, and **hybrid weapons are possible**
  (a weapon can give both).
- **Physical** can be **evaded** and crits up to **×10**. **Magic** can **fail**
  (reduced damage, not zero) and crits up to **×3** — the spiky mage feel. Magic
  currently mitigates against physical defence; magic-resist passives/jewels come
  later.

### Split, capped crits
- **Physical crit rate ← DEX** (cap **50%**); **magic crit rate ← WIT** (cap
  **20%**). So a high-WIT mage is a **fast, crit-prone caster, not a bigger
  nuker** — WIT buys crit frequency and cast speed, not raw power.
- Crit-damage caps: physical **×10**, magic **×3**. All caps in `StatCaps`.
- The Stats window now shows **P.Atk / M.Atk** and **Crit (Phys / Magic)**.

### Tuning notes
- Mob **defence growth was slowed** so attack outpaces it as you level (otherwise
  the ratio stays flat). Players stay tankier than mobs.
- Adjust feel via `StatCalculator.PhysicalK` / `MagicK`, weapon `mAtkFraction`
  (in `ItemCatalog`), and the crit caps in `StatCaps`.

## Phase 8

### Movement states (Run / Walk / Sit)
- Players have three movement states: **Running** (full speed), **Walking**
  (half speed, **+20% HP/MP regen**), and **Sitting** (can't move, **+80%
  regen** — sit to recover MP fast).
- **Z** toggles sit/stand, **X** toggles walk/run; the state shows under the
  clock. Walk↔run is instant; **getting hit while sitting** breaks the sit and
  triggers a short **stand-up delay** before you can move/cast again.
- Regen is a multiplier stack, so future passives/toggle skills can add to it
  (e.g. "+20% HP regen while sitting").

### Per-race+class speeds, with a cap
- Base **run speed** now depends on **race + class** (Elf fastest, Human slowest;
  within a race, fighters/rogues beat mages). Gear (`SpeedPercent`) and buffs
  raise it toward the **move cap of 250** (a normal player's buffed ceiling).
- The cap is **per-entity and raisable** (`MoveSpeedCap`), so a future rogue
  ultimate can briefly exceed 250 and outrun even a buffed mage.
- Central **`StatCaps`** holds all ceilings (move 250; attack-speed 1500 and
  cast-speed 1999 reserved for the casting round; crit 50%).

### Mob movement fixed
- New **`MobCatalog`**: each mob type has **walk** and **run** speeds (e.g. Wolf
  80/150, Bandit 60/108) and an aggressive flag. Mobs **walk while wandering,
  run when aggroed** — so players can kite, and a fighter outruns a bandit while
  a fast wolf still threatens a slow mage.
- **Wander is clamped to the mob's zone** — they no longer drift into
  neighbouring zones. Overlap same-level zones deliberately to mix mobs.

### Class change adds flat stats (identity)
- A class change can now grant **flat secondary bonuses** (e.g. a tank gets flat
  +Def/+HP), not just primary stats — primary stats stay reserved for the future
  dye/tattoo/set layer. Structure is wired; **Cleric** seeded as the example
  (+MP/+HP/+Def). Fill in other classes in `Classes.cs`.

### Where to tune
- **Speeds:** `SpeedTable` (players) and `MobCatalog` (mobs).
- **States/regen:** `MovementTuning`. **Caps:** `StatCaps`.
- **Class flat bonuses:** `ClassFlatBonus` on each `SecondClassDef` in `Classes.cs`.

## Phase 7

> **Delete `game.db` before running** — characters now store quests (new columns).

### NPCs you can talk to
- Stationary **NPCs** (gold dots, labelled `[Talk]`) are placed from
  `WorldMap.Npcs`. Click one (within range) to open a **dialog window** showing
  the quests they offer, quests ready to turn in, in-progress status, and (for
  class-change NPCs) class-change options.
- Three NPCs near town: **Elder Marius** and **High Priest Oren** (quest givers)
  and **Class Master Vael** (class change).

### Quests + the quest log
- Quests have ordered **steps** (talk / kill N mobs / collect / reach level),
  **rewards** (exp, SP, items), a **MinLevel**, and an optional
  **`RequiresQuestId`** so quests form **chains**. Kill steps advance as you kill
  matching mobs; talk steps advance when you visit the NPC.
- **Quest log** (press **J**) shows active quests and per-step progress. Quests
  persist across logout.

### Item-gated class change (the Cleric chain)
- The first worked chain, **Human Mage → Cleric**:
  1. **A Test of Devotion** (Elder Marius, lvl 18): talk → kill 5 Spiders →
     return → rewards the **Mark of Faith** (quest item).
  2. **The Cleric's Path** (High Priest Oren, lvl 20, needs chain 1): talk →
     kill 8 Wolves → return → rewards the **Cleric's Proof**.
  3. Bring both proofs to **Class Master Vael** → **become a Cleric** (items
     consumed). Different target class = different chain/items.
- The debug-menu class-change button still works (bypasses items, for testing).

### Quest items + a Quest inventory tab
- **Quest items** are non-droppable and non-tradeable, shown in a **separate
  "Quest Items" tab** in the inventory (toggle Gear / Quest Items).

### Where to author quests (the designated place)
- All quest content lives in **`Game.Shared/Quests/`**: `Quests.Root.cs`
  registers the chains, and per-chain files like `Quests.HumanMageCleric.cs`
  declare the quests, rewards, and the class-change requirement in one place.
  Class-change item requirements are in the `ClassChangeRequirements` table.
  Replicate the Cleric file for Sorcerer, Orc lines, etc.

## Phase 6.1

### Same skill, different name/icon per class
- A shared skill keeps **one id, one effect, one BuffKey** but can show a
  **different name (and, later, icon) per class** — set on the class's
  registration: `new ClassSkill(WindWalk, 20, DisplayName: "Holy Speed")`.
- So 10 classes can all use `wind_walk`; each sees its own label on the **skill
  bar, buff bar, and skills window**, while mechanically it's one buff that
  `improved_movement` replaces with a single `Replaces` entry. The buff bar shows
  the **casting class's** name (a cleric's buff reads "Holy Speed").
- Example: the Human Cleric's Wind Walk displays as **"Holy Speed"**.

### Party (area) buffs
- `SkillDef` gained a **`TargetMode`**: `SelfOrTarget` (default), `SelfOnly`, or
  `AlliesInRadius`. An area buff hits the **caster + nearby player characters**
  within `AreaRadius` (a stand-in for real party groups, which come later).
- Added **Mass Wind Walk** (id `mass_wind_walk`): same effect and **same BuffKey
  (`wind_walk`)** as the single-target version, but buffs nearby allies for more
  MP and a longer cooldown. Because it shares the BuffKey, `improved_movement`
  (or any `Replaces: ["wind_walk"]`) supersedes it too — one entry covers both
  the single and party versions. The Cleric's party version shows as
  **"Holy Procession"**.

### Design note (ids vs structure)
- **Skill ids stay flat and shared** (`wind_walk`, `holy_strike`) — that's the
  ability's identity, so stacking/replace logic stays simple and a buff shared by
  many classes needs only one `Replaces` entry.
- **The class tree's structure lives in `RaceAndClasses/`** — which class learns
  which skill, at what level, and under what display name. Per-class *uniqueness*
  (a genuinely different ability) gets its own flat id; per-class *flavour* (a
  rename of a shared skill) is just a `DisplayName` on the registration.

## Phase 6

> **IMPORTANT — delete any old `game.db` before running.** Skill ids changed
> from ints to strings and characters now store learned skills + skill points,
> so the schema changed. Delete `game.db` (in `Game.Server/bin/Debug/net8.0/`)
> and a fresh one is created on launch.

### Skills are now learned with Skill Points
- Skills must be **learned** before use. You earn **Skill Points (SP)** alongside
  exp (≈ 1/4 of exp; tune `GameConstants.SkillPointRatio`).
- The **Skills window (K)** now has **two tabs**:
  - **Learned** — your usable skills, grouped by category (Physical / Magic /
    Buffs / Debuffs / Heals), each with a **To Bar** button.
  - **Skills to Learn** — unlearned skills **grouped by required level**, with a
    **Learn** button that's enabled only when your level + SP (and previous rank,
    for ranked lines) allow. Clicking Learn opens a **confirm popup** showing the
    description, details, and **SP cost in green/red**; confirm to learn it,
    after which it moves to the Learned tab and can be dragged to the bar.
- Hovering a skill shows its description + MP/cast/cooldown/duration.
- The **core class kit** (the mandatory upgrades like Greater Heal) is granted
  **free** on class change / level-up; the **extras** (HP Boost ranks, Wind Walk)
  are the ones you spend SP to learn. Learned skills + SP **persist**.

### String skill ids + per-class skill files
- Skill ids are now **stable strings** (`magic_bolt`, `greater_heal`,
  `hp_boost_1`). Same benefits as item keys: readable, reorder-safe,
  collision-guarded at startup.
- **One place to manage class skills:** `Game.Shared/RaceAndClasses/`. Each
  partial file registers a race+class line's skills with learn-levels, e.g.
  `Classes.Human.Mage.cs` declares the Human cleric/sorcerer learnable skills.
  Adding a skill to a class is a one-line `ClassSkills.Register(...)` edit.
- Example HP Boost line (3 ranks at 40/56/72 style levels) and Wind Walk are
  authored there to show the pattern; ranked skills must be learned in order.

### God race + God items (debug)
- A **God race (enum 99)** is creatable **only in DEBUG builds** but fully usable
  once made, with two God second classes (Demigod / Ascendant).
- Removed `legendary_windforce`; added two **God-tier** items (debug menu):
  **God's Judgment** (sword, attack + range 1000, all 8 attributes at 100%) and
  **God's Robes** (def/hp/mp/eva 1000, all armor attributes at 100%).

### New rarities & attributes
- Rarities extended: **Epic (3), Legendary (4), God (99)** — higher rarities roll
  more attributes.
- Two new attributes: **Evasion %** and **Defence %**, available on **E-grade and
  up** gear, and they apply to your real stats.

### Quest groundwork (data types only)
- Added quest **data types** (`QuestDef`, `QuestStep`, `QuestReward`,
  `CharacterQuestState`) and a nullable **`RequiredQuestId`** hook on second
  classes — so class-change-by-quest drops in later without a refactor. The live
  quest system (NPCs, dialog UI, tracking) is a **future phase**; an
  `EntityKind.Npc` is reserved for it.

## Phase 5.4

### Buff system rebuilt for a future buffer class
- **`SkillEffect` is now a `[Flags]` enum.** One skill can carry several effects
  at once: `Effect = BuffAtk | BuffMoveSpeed | BuffCastSpeed`. No more inventing
  a new enum member per combination — add a flag once and combine freely.
- **Per-effect magnitudes with flat OR percent.** A skill carries
  `EffectMagnitude[]`, each entry `(Effect, Value, Mode)` where Mode is
  `Flat` or `Percent`. So Wind Walk = `(BuffMoveSpeed, 33, Flat)`, a haste buff =
  `(BuffMoveSpeed, 0.30, Percent)`, and you can even put **both on one buff**
  (33 flat + 5%). Stats combine as **`(base + ΣFlat) × (1 + ΣPercent)`** per stat.
- **Working cast-speed, attack-speed, and evasion buffs** (not just from items
  now) — a buffer skill can buff them directly.

### Buff stacking rules (exactly two mechanisms)
- **Explicit `Replaces` (unconditional):** a buff lists buff keys it overrides,
  e.g. `improved_movement` with `Replaces = ["wind_walk", "agility"]`. Casting it
  removes those buffs **no matter their rank or magnitude** — the author declared
  the override.
- **Same `BuffKey` compares by `Rank`:** recasting the same buff applies only if
  the incoming `Rank ≥ existing Rank` (a full replace, refreshing duration).
  A **weaker** recast does nothing — no downgrade, no refresh. Equal rank = refresh.
- Unrelated buffs (different key, not in a `Replaces` list) simply **stack**.
- Current skills use this already: War Cry (`might` rank 1) and Greater War Cry
  (`might` rank 2) auto-supersede by rank; Weakness/Greater Weakness likewise
  (`curse_def` rank 1/2); Battle Fury is a two-effect buff (atk + move speed).

### How to author a buff (for the future buffer class)
```csharp
new(skillId, "Improved Movement", BaseClass.Mage,
    SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
    MpCost: 30, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 0,
    DurationTicks: 1200, BuffKey: "improved_movement", Rank: 1,
    Replaces: new[] { "wind_walk", "agility" },
    Magnitudes: new EffectMagnitude[]
    {
        new(SkillEffect.BuffMoveSpeed, 40, ModifierMode.Flat),
        new(SkillEffect.BuffEvasion,   10, ModifierMode.Flat),
    },
    Description: "Combines and improves Wind Walk and Agility."),
```

## Phase 5.3

### In-game day/night clock
- Time of day now cycles. The **one speed knob** is `GameClock.TimeScale` in
  `Game.Shared/GameClock.cs` — in-game seconds per real second. Default **6**
  (a full game day = 4 real hours; day and night ~2h each). For testing, set it
  to **60** (full day in 24 real minutes) or **600** (~2.4 min) to watch night
  fall fast. An in-game **clock + Day/Night indicator** shows at the top of the
  screen.

### Population cap + respawn delay (no more instant respawns)
- Each spawn zone now keeps **up to `MaxCount` mobs alive and never exceeds it**.
  When a mob dies, the zone waits a delay rolled from **`RespawnSeconds ±
  RespawnVariance`** (real seconds), then respawns — only if under the cap.
- The mob is removed on death and the **zone schedules** the replacement (the
  performant approach). A cosmetic corpse-fade can be layered on later.

### Elites & bosses
- A zone has a **`Rank`** (Normal / Elite / Boss). Elites are tougher (×4 HP,
  ×1.5 attack) with ~minutes respawn; bosses much tougher (×20 HP, ×2.5 attack)
  with hours-long respawn. Authoring example (already in `WorldMap.cs`):
  - **Elite**: `RespawnSeconds: 120, RespawnVariance: 30` → "2m 0s ±30s".
  - **Boss**: `RespawnSeconds: 21*3600, RespawnVariance: 3*3600` → "21h ±3h".
- **Boss/elite respawn timers are persisted** (real-world time) to the database,
  so a long timer **survives a server restart** — kill the boss, restart the
  server, and it's still on cooldown.
- On the map, elite zones are **amber** and boss zones **purple**, each labelled
  with rank, level, and the **[X ±Y] respawn** range.

### Day-only / night-only zones
- A zone's **`Active`** is `Always` (24h, default), `Day`, or `Night`. To swap
  mobs at dusk/dawn, overlap two zones at the same spot — one `Day`, one
  `Night` (there's a worked example in `WorldMap.cs` at 7500,9500). When the
  phase flips, inactive zones despawn and newly-active ones fill in.

### Where to edit
- **Speed of time:** `GameClock.TimeScale`.
- **Everything spawn-related:** `WorldMap.SpawnZones` — `MaxCount`,
  `RespawnSeconds`/`RespawnVariance`, `Rank`, `Active`, level band, mob types.

## Phase 5.2

### The world is now visible and editable from one file
- **`Game.Shared/WorldMap.cs` is the single source of truth** for world layout —
  the server (spawning, collision) and client (drawing) both read it. To reshape
  the world you edit this one file.

### World border
- The playable rectangle is drawn as a **dashed outline**, so the edge is
  visible instead of an invisible wall. Defined by `WorldMap.Border`.

### Roads
- **Thick, semi-transparent grey strips** lead from town toward the hunting
  grounds; **mobs don't spawn on roads**, giving safe-ish corridors. Each road
  is a list of points with a half-width in `WorldMap.Roads` — add or reshape a
  road by editing its point list.

### Spawn zones (visible + self-documenting)
- Each spawn zone is drawn as a **light semi-transparent red disc** with a
  **label showing its level band and mob types**, so you can see at a glance
  where things spawn and what you'll meet. (Placeholder colour until real
  environment art.)
- **Fully editable** in `WorldMap.SpawnZones`. Your example —
  *"at (1000,1000) radius 800 spawn level 5-7 boars and spiders"* — is one line:
  ```csharp
  new(X: 1000, Y: 1000, Radius: 800, MinLevel: 5, MaxLevel: 7,
      MobTypes: new[] { "Boar", "Spider" }, MobCount: 10),
  ```
  The server spawns each zone independently (random point in the disc, avoiding
  the safe zone and roads), picks a random mob type and a level in the band, and
  the client tints + labels it automatically. Add as many zones as you like.

### How spawning works (for editing)
- On startup the server loops every `SpawnZone` and spawns `MobCount` mobs in
  it. Each mob remembers its home point and wanders/leashes around it; on death
  it respawns at home after the respawn timer. Change a zone's numbers and both
  the spawn behaviour and the on-screen overlay update together.

## Phase 5.1

> **IMPORTANT — delete any old `game.db` before running.** Item IDs changed from
> integers to string keys, so the database schema changed. Delete the `game.db`
> file next to the server (or just let this fresh build create a new one). Old
> saves are not compatible.

### Item IDs are now stable string keys
- Every item has a permanent **string key** (e.g. `sword_e_rare`,
  `robe_f_common`, `potion_minor`) instead of a fragile integer. Keys are the
  item's identity — stored in saves, referenced by loot tables and the debug
  menu. **You never renumber**; new items just get new keys, and you can place
  them anywhere in the file. A **duplicate-key guard** at startup throws a clear
  error naming the collision instead of a cryptic crash.

### Full weapon & armor matrix
- Weapons are generated for **every type × grade × rarity**: sword, dual,
  bow, staff × {F, E} × {common, uncommon, rare} — keys like
  `bow_e_rare`. Armor likewise: heavy, light, robe × grade × rarity.
- **All classes can equip any weapon**; your skills determine whether a given
  weapon is actually good for you (matches the design doc). Bows/staves carry
  range; staves add MP; daggers are lower per-hit but suit the rogue's crit kit.
- Loot tables and starter gear now reference these keys; mages start with a
  staff + robe, fighters with a sword + leather.

### Legendary one-off
- **Windforce** (`legendary_windforce`): an E-grade bow with **5 fixed
  attributes** (Attack +30%, Attack Speed +25%, Move Speed +20%, HP +30%,
  MP +20%). Spawn it from the debug menu. Fixed attributes never reroll, unlike
  normal drops.

### Debug menu (DEBUG builds)
- Level +1; Windforce; a **Rare E of each weapon** (sword/dual/bow/staff);
  a **Rare E of each armor** (heavy/light/robe); and **x10** buttons for every
  scroll and potion (no more clicking one at a time). No shield yet — that
  arrives with block mechanics.

### War Cry split by class
- **Rogue & Archer**: War Cry becomes **Battle Fury** — +20% Attack **and**
  +15% Move Speed for 30s.
- **Warrior**: War Cry upgrades to **Greater War Cry** — +30% Attack.
- **Tank**: still swaps War Cry for **Fortify** (+50% Defence).

## Phase 5

### Persistence (EF Core + SQLite)
- Characters and inventory now **survive server restarts**. The database is a
  single SQLite file (`game.db`) created automatically next to the server on
  first run — **no database server to install**.
- Characters **auto-save every 60s** and on logout; you log back in **where
  you left off** with your level, exp, stats, second class, and full inventory.
- Rolled item attributes persist via an EF Core **JSON column** (`OwnsMany …
  ToJson()`), so adding a new attribute type never needs a migration. Attributes
  roll once at drop time and are immutable thereafter (ready for a future
  "legendary reroll stone").
- **Swapping databases is one line** in `Program.cs`: replace `UseSqlite` with
  `UseNpgsql`/`UseSqlServer`; all the EF Core code is provider-agnostic.

### Accounts & character selection
- The flow is now **Register/Login → Character Select → Create/Enter**:
  - Account login screen with username + password (**PBKDF2-hashed**, never
    stored or sent in plaintext form).
  - Character selection lists all characters on the account; create new ones
    via the class-tree screen, then pick one to enter the world.
- **The first account registered becomes an admin** (convenient for testing).

### Admin role
- Admins use **slash-commands in chat**: `/help`, `/kick <name>`,
  `/ban <name>`, `/unban <name>`, `/jail <name>`, `/unjail <name>`, `/god`,
  `/where <name>`.
- **God mode** makes you immune to damage. **Jail** pins a player to the jail
  corner until released. **Ban** persists (works offline) and force-disconnects
  the player if they're online. Non-admin accounts can't invoke any of these —
  the server validates the admin flag, not the client.

> **First build note:** the server now references EF Core, so the first
> `dotnet build`/restore needs internet to pull the NuGet packages. After that
> it runs offline. The `game.db` file is created on first launch.

## Phase 4.8

### Item attributes (rolled per drop)
- Weapons and armor now roll **random bonus attributes** when they drop, so two
  Steel Swords differ. **Count by rarity**: F common 0 / uncommon 1 / rare 2;
  E common 1 / uncommon 2 / rare 3 (and so on by grade).
- The **attribute pool and roll ranges scale by grade**, defined in
  `Game.Shared/Attributes.cs`:
  - **F grade** pool: Max HP%, Move Speed% — rolls 1–10%.
  - **E grade** pool adds Max MP%, Cast Speed%, Attack Speed%, Attack% — HP/MP
    roll 10–30%, the rest 1–20%.
  - B/A/S inherit the bigger pool with stronger ranges (ready to tune).
- Attributes live on the **item instance**, show in the **inventory tooltip**
  and the **equip-comparison popup**, and feed real stats: HP/MP/Attack %,
  move speed, and **Cast Speed / Attack Speed** (which shorten cast time and
  basic-attack interval).

### Cast speed display (WIT-centered)
- Cast reduction is now centered on **WIT 25 = baseline (0%)**. Each point
  above 25 casts faster, each below slower (1.2%/point). The Stats window shows
  **Cast Speed** broken into the WIT contribution and item contribution, and
  the **cast bar** shows the effective bonus next to the skill name.

### Base-skill unlock levels
- Per your fix, base skills no longer wait for class change: **Power Strike @1,
  War Cry @5** (Fighter); **Magic Bolt @1, Weakness @3, Heal @5** (Mage).

### Fixes
- **Potion buttons**: the rarity letter (C/U/R, top-left) and the count
  (bottom-right) are now separated and readable.
- **Equip-comparison popup**: clicking an item now always shows **its own
  stats**, with the difference vs the equipped item as a secondary column.
  Clicking the equipped item (or an item with no counterpart) shows real values
  instead of zeros, and lists the item's rolled attributes.

## Phase 4.7

### Where to edit skills (for you)
- **`Game.Shared/Skills.cs`** is now the single skill-design file, split into:
  - `SkillCatalog.All` — every skill's numbers + description.
  - `ClassProgression` — **which skills each class gets**, whether a skill
    **replaces** a base skill, and the **unlock level**.
- To give the Witch a DoT the Sorcerer doesn't get: add the `SkillDef`, then
  add a row to `ClassProgression.RaceOverrides` keyed `(Race.Ork, Archetype.Nuker)`
  with `new SkillGrant(id, unlockLevel: 25)`. Nothing else changes — the server
  validates and the client renders from these tables. The hooks for per-race,
  level-gated flavour skills (DoT vs burst vs control) are already in place.

### Base skills upgrade on class change
- Second classes now **transform** the base kit instead of just adding a skill:
  - **Tank**: keeps Power Strike; War Cry → **Fortify** (+50% def).
  - **Warrior**: keeps War Cry; Power Strike → **Mighty Blow**.
  - **Rogue**: keeps War Cry; Power Strike → **Twin Slash**.
  - **Archer**: keeps War Cry; Power Strike → **Power Shot** (ranged).
  - **Healer** (Cleric/Shaman/Priest): Heal → **Greater Heal**, Magic Bolt →
    **Holy Strike** (weaker nuke), keeps Weakness.
  - **Nuker** (Sorcerer/Witch/Inquisitor): Magic Bolt → **Flamebolt** (strong
    nuke), keeps Heal, Weakness → **Greater Weakness**.

### Class identity through numbers
- **Mages** basic-attack for ~15% of attack power — they live on skills + MP.
- **Fighters/Warriors** hit full (110%) and brawl with attack + skills.
- **Archers** hit full + **+15% crit** — kite with basic attacks and crits.
- **Rogues** hit 65% but get **+20% crit and +evasion** — skills + crits.
- **Tanks** hit 55% but bring standout defence (Fortify, heavy armor).
- Mage main skills now **~4s cast (WIT reduces) and ~1s cooldown** so they
  chain-cast, and hit meaningfully harder than a mage's basic attack.

### Stackable consumables
- Potions and enchant scrolls now **stack into one inventory slot** with a
  quantity (1 → 2 → … → "99+"). Drops merge into the stack, using one consumes
  one, trading moves the whole stack and **merges** into the receiver's stack.
  Gear stays one-per-slot (each piece keeps its own enchant level).

### Chat moved up
- The chat panel sits higher so it no longer overlaps the skill bar buttons.

## Phase 4.6

### Character creation — class tree
- The login screen is now a **button tree** instead of dropdowns:
  Race → Base Class → preview each Second Class. The right pane shows base
  stats (CON/ATK/WIT/DEX to compare), the class fantasy, the class-change
  stat bonus, and the full skill list with descriptions — so you know what
  you're getting into before creating. Name + Connect sit at the bottom.

### Skills window (K)
- Lists every skill you have with **description, MP cost, cast time,
  cooldown, and duration**. Each has a **To Bar** button.

### Configurable skill bar
- 8 slots. New skills **auto-fill the first free slot** when acquired (e.g.
  your signature skill on class change), but you can **assign** from the
  Skills window and **remove** by right-clicking a slot. Hotkeys 1-8.

### Buff bar + tooltips
- Active buffs/debuffs show as pills under the vital bars with **time left**;
  hover for a tooltip with the description and remaining seconds. Cast War Cry
  and you'll see the buff and its countdown. Debuffs are tinted red.

### Potions — fixed squares
- Three **always-visible colored squares** (green/blue/gold) bottom-right,
  with a **count badge** (caps at "99+"), **disabled when you have none**.
  Click or hotkeys Q/E. Counts also show as "99+" in the inventory.

### Inventory: remove + enchant
- Each item row has an **X** (destroy — sell/dismantle comes later) and, for
  gear, a **+** (enchant). The equip-compare popup is now **enchant-aware**
  (a +5 sword compares correctly against a +0).

### Enchanting
- Enchant gear **+1 to +16** with success bands from the design doc: **100%**
  to +3, **66%** +4-6, **40%** +7-9, **20%** +10-16. Each enchant level adds
  +20% of base bonus +1 flat. Three scrolls differ on failure:
  - **Common**: the item **breaks**.
  - **Uncommon**: enchant **resets to +0**.
  - **Rare**: enchant **drops by 1**.
  Scrolls **drop rarely from higher-level mobs** (rarer than any other loot;
  the better the scroll, the higher the level floor and the lower the odds).

### Debug menu (DEBUG builds only)
- A **Debug** button (only compiled in Debug configuration) opens a panel to
  grant scrolls, potions, F/E gear, and **Level +1** for testing. Both the
  client button and the server endpoints are `#if DEBUG`-gated, so a Release
  build has none of it.

## Phase 4.5

### UI overhaul
- **Three colored vital bars** top-left: HP (red), MP (blue), EXP (gold),
  each with live numbers, replacing the old text line.
- **Stats window** — the *Stats* button (or **C**) opens a panel next to the
  inventory showing CON/ATK/WIT/DEX, max HP/MP, attack power, defence,
  accuracy/evasion, crit %, and attack range. It updates live on level-up,
  equip, and class change.
- **Equip comparison popup** — clicking an inventory item opens a popup that
  diffs the item against what's equipped in that slot (green = upgrade, red =
  downgrade) with **Equip/Close** buttons, instead of equipping instantly.
- **Chat tabs fixed** — the All/World/Local/Whisper tabs now sit at the bottom
  of the chat box (inside it), not floating above the panel.

### Per-mob loot tables
- Drops are now **per mob type**, not a global roll. Each mob has a loot table
  of (item, chance, mob-level band): Boars drop weapons, Wolves drop armor,
  Slimes drop robes/mage gear, Spiders drop light armor + bows, Bandits drop
  swords and the best F-grade gear. Low-level kills give F grade; level-11+
  kills give E grade — all defined in `LootTables` in `Game.Shared/Items.cs`,
  one dictionary keyed by mob name. Each table entry rolls independently, so a
  kill can drop zero, one, or several items.

### Potions (grade/rarity based)
- Three healing potions on a **shared 30s cooldown**, used from the **potion
  action bar** (hotkeys **Q**/**E**) or by **clicking them in the inventory**:
  - *Minor* (common): heals 1% max HP/sec for 15s
  - *Healing* (uncommon): 2% max HP/sec for 15s
  - *Greater* (rare): instant 50% max HP heal
- Potions are a **separate effect channel from natural regen** — they tick
  during combat too. **Rarity override**: a higher-rarity potion cancels a
  lower one's effect; same rarity restarts it (safe-guarded, though the
  cooldown normally prevents it). You start with two Minor and one Greater to
  test. Any mob can also drop potions on top of its gear table.

## Phase 4

### Second-class tree (level 20)
- At level 20 the **Class** button opens your six race/base-appropriate
  options — the 18 design-doc classes (Beast, Templar, Knight, Cleric,
  Sorcerer, …) mapped onto 6 archetypes: **Tank, Warrior, Rogue, Archer,
  Healer, Nuker**.
- Choosing one is permanent, grants a permanent core-stat bonus, full-heals
  you, and unlocks a **signature skill** that joins your skill bar:
  Fortify (Tank), Mighty Blow (Warrior), Twin Slash (Rogue), Power Shot
  (Archer), Greater Heal (Healer), Flame Burst (Nuker).
- Archetype range rules from the doc are in: **Archer** second classes get
  +500 basic-attack range with a bow (capped 1100); **Healer/Nuker** get
  +500 spell range (capped 900).

### Items & equipment
- Grades **F/E/B/A/S** gate by level (0/20/40/60/80); rarities Common,
  Uncommon, Rare. Weapons add attack (bows/staves also set ranged range);
  armor comes in Heavy/Light/Robe with def/HP/eva/MP profiles.
- Equip/unequip from the inventory; one item per slot (weapon, armor).
  Equipping recomputes all derived stats server-side and re-validates the
  level requirement. You start with a Rusty Sword and Leather Vest.

### Drops
- Killing a mob has a 30% drop chance (70/25/5 common/uncommon/rare);
  level-13+ mobs can drop E-grade gear. Loot lands in your bag (30 slots)
  and pops a system message.

### Trade window
- Target a player within range → *Request Trade*. They get an accept/decline
  prompt. The window matches the design doc: **their offer on top, your
  offer in the middle, your bag on the bottom**, Ready/Cancel in the footer.
- Click bag items to add (max 10), click your offered items to pull them
  back. **Any change resets both Ready flags** — no bait-and-switch.
- The trade commits only when both press Ready; the server re-validates both
  inventories inside a single step (items still owned, bags have room) before
  swapping. Equipped items can't be traded; disconnect/death cancels safely.

