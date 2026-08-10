# Changelog

Development history, newest first.

Early work was tracked as **phases** — self-contained slices that each ended in a playable build.
Phases 1–3 built the foundation (movement, interest management, combat, skills, buffs, the
safe-zone town, banded hunting grounds); the written phase record runs to **Phase 24.1**
(2026-06-22). After that the phase numbering was dropped and commits became the record, so entries
from mid-2026 on are grouped **by date** instead. Later, `GameConstants.GameVersion` (starting
0.1.0, currently **0.57.1**) began gating the client/server protocol handshake — it tracks wire
compatibility, not this feature history.

For what's *planned* rather than done, see [Roadmap.md](Roadmap.md).

## 0.58.0 — 2026-08-10 — a class grants no stats, and the invented level-40+ kits are gone

*(The third slice of 0.58.0; the evasion root cause and the mob weapon table shipped in the two
commits before it and are not yet written up here.)*

**Identity is the kit, not the stats.** The owner's ruling: *"There is no identity. The identity is
just skills/passives kit … the magus and the tempest have same stats, just one has more dmg skills
while the other more debuffs … no more u change your class and get bonus."* Every class runs the same
stat formulas; what separates two disciplines of one archetype is what their skills *do*.

So the whole class-bonus layer is deleted — `ThirdClassCatalog.FlatFor` (the twelve per-discipline
leans: Bulwark +220 HP/+45 Def, Ravager +45 Atk, …), the Cleric's stray `+60 MP/+30 HP/+10 Def` (the
only one of eighteen 2nd classes that had one), the `Bonus` field on both class-def records, and the
two apply-blocks in `Entity.RecomputeDerived`. `ClassFlatBonus` the *record* survives as an
**armor-set** type; gear is not class.

The proposal to re-home the same numbers as discipline passives was rejected too — *"Remove them,
don't add them as passives. W8 on the 40+ csvs."* This table was where `Discipline.Phantom` hid an
`Evasion: 32` against a whole-game evasion budget of ~18 points, unnoticed until the 0.58.0 hunt: a
bonus nobody can see is a bonus nobody can tune.

**The 40+ purge.** *"Anything that's not inside the csv should not exist except the class balance."*
Every level-40+ skill grant was invented ahead of the CSVs, so `ClassSkillTables.Third.cs` loses the
placeholder rename kit for all ten fighter disciplines, the warrior demos (Cleaving Strike, Hamstring,
War Focus), the tank kit (Shield Bash, Provoke, Aegis, Last Stand, Indomitable), Terrifying Roar, the
Venomweaver DoT trio, and the rogue primitives (Shadowstep, Vanish, Repelling Shot, Snare Trap).
Kept by his exception list: everything nuker — Elemental Burst, Frost Bind, Entangling Roots, Glacial
Spike, Creeping Frost, Phase Shift, Mana Barrier, the Magus/Tempest kit — and the Warchanter's buff
ladder, which has a CSV behind it.

⚠ Only the **learn assignments** are gone; every `SkillDef` stays in the catalog. Anything already
learned keeps working (`LearnedSkills` persists ids, not table entries), and those defs are the raw
material for the level-40+ CSVs. Don't re-grant them, and don't invent replacements.

**The three floor passives move into the CSVs** — Evasion Mastery (rogue), Precision (warrior) and
Anti-Magic (tank) each get a level-20 row in `docs/data/classes_skills_csv/`, SP 0 because they are
auto-granted rather than bought. They still work at every tier via `FloorPassiveFor`; the CSV is now
the authority on their numbers. ⚠ The tank CSV separately contains a *different* skill called "Tank
Anti-Magic" (m.def +25/+45) — a stat, not the fizzle floor.

**Server + shared only** — no DTO, protocol unchanged, no schema change, `game.db` untouched. The
in-game effect is at 40+, where every discipline loses its lean (Bulwark's −220 HP/−45 Def is the
largest), and on the Human Cleric from level 20. Not visible in BalanceMatrix, which assigns a 3rd
class in only one section — **unmeasured and unplayed.**

## 0.57.1 — 2026-08-09 — a buffer's window swallowed the quest step behind it

Found mid-playtest, the first bug of the 0.57.0 play pass: the tutorial chain stalls at level 6, on
the beat that sends you to Spirit Helper Nyra.

`GameLoopService.SendDialog()` **returns early** for an `NpcRole.Buffer` so it can send the buff
window instead of the usual quest dialog — but `AdvanceTalkStep()`, which advances a `TalkTo` step
when you speak to its target, is called at the **bottom** of that same method. Talking to a buffer
therefore opened her window and never touched the quest. Buffer is the only role in `SendDialog` with
an early return (Warehouse and SkillReset ride through on the normal path as flags), which is why
every other stop in the chain — Pell, Cera, Miren, Dolan — worked and only this one didn't.

The branch now advances the step itself before returning. This is a class of bug the tutorial chain
invites by design: `M5` is built out of `TalkTo` steps pointed at *service* NPCs, so any future role
that short-circuits `SendDialog` reproduces it.

**Server-only** — no DTO, **protocol stays 14**, no schema change, and a character already stranded
on the step advances on the next talk (quest state lives in `ActiveQuestsJson` and survives). No new
APK: a 0.57.0 client is unchanged on the wire.

## 0.57.0 — 2026-08-08 — the last three of the queue: the S grade, the jail's wall, client collision

Playtest-11/17 **`B8` · `B9` · `B10`** — the three items left in the owner's build queue, which is now
empty. Server + Unity client both type-check; **no db reset, protocol stays 14** (nothing on the wire
moved).

**`B8` — S grade existed everywhere except in words.** `ItemCatalog.TierLetter` stopped at "A" for
level ≥ 76, so a level-80 Soulcrystal piece printed **`Grade: A`** in its details while the only scroll
that fits it says *"S grade only"* — the display path and every banded system named different grades.
`TierLetter` now returns "S" at 80. It deliberately does **not** grow S\*/S\*\* rungs for the 83/85
name themes: no banded system has those, and a letter no scroll answers to is the very bug being
fixed.

The same hole was in the grade PENALTY ladder — `GradePenalty.GradeLevels` ran `{1,20,40,52,61,76}`
while `EnchantRules.GradeOf` cited it as the source of *"E 20, D 40, C 52, B 61, A 76, **S 80**"*. So S
was the one tier with no grade gate: a level-76 character in a Soulcrystal set took **×1.00** while the
item's own details said *Requires level 80*. The ladder gains its seventh step, which makes that the
normal one-step **×0.5**. ⚠ **This is the one behaviour change in the release** — flagged for his
ruling as `61c`. `GapFactors` stays six long on purpose: the largest gap is now 6 and clamps to the
same ×0.1 floor.

**`B9` + `B10` — the client half of walls, which never existed.** The owner's 2026-07-24 architecture
call was a split: **the client stops you at the surface and never emits an out-of-world coordinate;
the server keeps its rubber-band as the anti-cheat backstop.** Only the backstop was ever built, so
the everyday experience of a wall was being yanked back through it.

Two halves may only enforce the same rule if they cannot disagree, so the geometry moved into
**`Game.Shared/WorldDomain.cs`** — one value type answering *which world is this point in* (Overworld
/ Dungeon / Jail / Void), *does it contain that point*, and *clamp it back onto me*. `ConfineToDomain`
is now four lines over it and `InJail`/`ClampToJail` are one-line delegations; the server behaves
exactly as before, by construction.

`GameBoot.Move()` — the single chokepoint every move order passes through — now answers two different
mistakes differently. A tap **past the edge of your own world** is clamped: you walk to the wall and
stop, the destination ring lands on the wall, and the server has nothing to correct. A tap that lands
**inside a different world** is not sent at all and says *"You can't walk to … — only a teleport goes
there"* — crossing is teleport-only, so quietly walking you to the nearest wall would be a lie. Note
the order matters for the jail: a tap outside the cell is *clamped* (you pace the cell), not refused,
because empty ground is not another world.

**And the jail is drawn.** The cell has been enforced since 0.28.67 — for the inmate and for the admin
who teleports in to talk to one — but nothing on screen said where it ended, which is why the clamp
read as "the game keeps yanking me" (`B9`). It gets the same orange dashed language as the world
border, as a ~18-dash circle that renders **only while you stand inside it**. Deliberately *not* on
the map-overlay toggle: the world rectangle is 24000 units of reference you look up once, this is a
260-unit wall you are pressed against. Dungeon boxes get no ring — a bounding box is not the polygon
the map already draws, and a rectangle there would contradict the coloured outline instead of
explaining it. The dash emitter is now one shared method (the 0.28.78 render-warning flood was a
material *per dash*; it still takes its material from the caller).

## 0.56.0 — 2026-08-07 — `D5`: the combat feed gets its own channel and its own window

*(Backfilled 2026-08-08 — 0.56.0 shipped without a changelog entry.)* Damage, loot and the per-kill
`Exp/SP/Gold` line left the System tab for **`ChatChannel.Combat`** and a **second window** (bottom-
right, beside the chat window). Server → client only: `HandleChat` demotes an inbound Combat *or*
System message to Local, so nobody can inject a fake loot line. The line's KIND ("LOOT"/"EXP") rides
in `ChatMessage.From` — on this channel From is a colour tag the client never prints — which kept the
wire change to one enum value. Protocol 13 → **14**, no schema change.

Client-side, `GameUi.World.cs` grew a private `LogView` type: two views (`_chatView`, `_combatView`)
over the **one** `ClientLog` buffer, rather than a copy-paste of the append/trim logic — that is the
code that used to freeze the phone. The Chat window's 6th button, **Combat**, is not a tab; it toggles
the window and stays lit while it is open. Two calls left for the owner (§60): combat is excluded from
`All`, and Chat's Clear still wipes everything.

## 0.55.0 — 2026-08-07 — the QoL five, titles you can write, and NPCs that wear their role

Playtest-19 **C1 · C3 · C14 · C16 · C17** — the rest of the QoL six he picked (C15 shipped inside
0.54.0). Server + Unity client both type-check; **no db reset**.

**C1 — the chat log is per CHARACTER.** A newly created character opened onto the *deleted*
character's conversation, for the same reason the auto-hunt marks did in playtest-17: the buffer is a
singleton that outlives whoever was talking. `ClientLog.ClearChat()` now runs from
`ResetWorldTransients`, so it clears on leaving the world, on offline-farm, and on logout. It drops
the **chat channels only** — the System tab is the crash trail the buffer exists for (a refused
connection, an exception in a SignalR callback), it is not per-character, and wiping it on every
relog would throw away the diagnostics for the relog itself. The buffer also goes **200 → 1000 lines**
(his cap): the console only ever *draws* `ConsoleDisplayRows` of them, so a deeper buffer costs
strings, not rows, and 200 lines was under a minute of combat.

**C3 — timed items say how long they have left**, in the details panel, colour-graded: green over 7
days, white over 1 day, yellow over 1 hour, red under it. Reads `InventoryItemDto.ExpiresAtUtc`,
which 0.54.0 already sent — it is a wall clock stamped at acquire time, so it is a plain `UtcNow`
difference and nothing the client counts down. Covers the 30-day loaner kit and every rune.

**C14 — a two-handed weapon greys the off-hand square** instead of leaving it empty. An empty "Shld"
square reads as *you could still put a shield on*, which is the one thing it is not. The square now
shows the two-hander's own abbreviation, dimmed toward grey, and goes non-interactive (there is no
second item to open). Gated on `ItemDef.OccupiesOffHand` — the same predicate the server equips by,
so the square cannot disagree with the rule that actually refused the shield.

**C16 — titles get a voice.** Three changes:

- **No more "the".** `the Devoted` → `Devoted`. A title is drawn on its own line above the name, not
  read as a sentence after it, so the article only made the short ones sit off-centre.
- **A colour per title,** his palette: gold board golden, time-played green, PvP purplish, PK dark
  red — plus sky for Level and rose for Charisma, which he did not name. The PvP purple is
  deliberately **deeper than the PvP-flag name colour** (`#CC80FF`): a flagged player's name is
  already purple, and matching it would turn a two-line plate into one purple blob.
- **A different face from the name** — italic + small caps with a little tracking. The client ships
  ONE TMP font asset with a static atlas, so a second typeface would have to be baked; TMP's
  synthesised styling on the font already there gets the effect he asked for.

**A title on the wire is TEXT plus a COLOUR** — `EntityDto.Title` + the new `EntityDto.TitleColor`
(RRGGBB, no `#`). Not an id: a title can be granted by a board, granted by a staff role, written by
its owner, or belong to an NPC, and the plate must not have to know which. That is what made the two
features below cost almost nothing to add. `ProtocolVersion` **12 → 13** (a field and two hub methods,
both pure additions); `MinAcceptedProtocol` stays at 8.

**C17 — admins and moderators hold their own titles**: *Game Master* and *Moderator*, held by
**account role** rather than by topping a board, so they cannot be lost to a rival. They arrive in the
same picker as every other title and are worn the same way — deliberately **opt-in**, like the board
titles, so staff can still walk around as a normal player. `/role` refreshes them live, so a
promotion offers the title and a demotion strips a worn one without waiting for a relog.

**Player-written titles.** `/title <text>` (**20 characters**) sets and wears a title of your own;
`/titlecolor <colour>` recolours it from a named palette. Gated on a per-character right, **granted at
level 76 from the same gate as Angel's Protection** (owner: *"grant title right to character on same
place as angels blessing, they both be later same quest"*) — the two are meant to be rewards of one
quest once that quest exists, and auto-granting them from a single place means that quest replaces one
condition rather than two that have quietly drifted apart. `/titleright <name> on|off` is the manual
override on top. The right is announced and pushed on the EDGE only, so it lands once: the moment you
reach 76, or on the first login of a character already past it. The
**ranking and staff words are RESERVED**: nobody can type `Warlord` and wear a rank they never earned,
which is the one rule that keeps an earned title worth earning. The palette is named colours rather
than free hex, so a written title also cannot be dressed in the PK board's dark red. Text is validated
server-side (letters, digits, space, `'`, `-` — rich-text markup would let a title recolour itself
past every rule here). What you wrote is kept while a board title is worn, so you can switch back
without retyping.

**NPCs wear their role as a title.** `Elder Marius` now plates as `Elder` over `Marius` (owner). Split
at the **last** space, because the roles are not all one word — *High Priest Oren*, *Spirit Helper
Nyra*, *Class Master Vael* — while the personal names all are. NPCs only: splitting a mob's name would
invent a creature called *Pup*. The catalog keeps the full authored name, so quest hints and dialog
headers still read "Elder Marius", and the target frame shows the whole person on its one row.

**`/target <name>`** selects by name instead of by finger — the case being a crowd around the
gatekeeper whose plate sits behind three other players'. Matches either half of an NPC's name or a
prefix of either (`/target Pell`, `/target Gatekeeper`), exact beating prefix and nearest winning
within each, so a Pell and a Pellon in the same square resolve the way you meant. Client-side, because
targeting is client-side — the server only ever hears a target id when you act on it.

⚠ **DELETE `Game.Server/game.db`** — three new columns (`CustomTitle`, `CustomTitleColor`,
`MayWriteTitle`) and `EnsureCreated()` does not migrate.

## 0.54.0 — 2026-08-07 — the tutorial chain, and the Newbie kit becomes a 30-day loaner

Playtest-19 **M5** and **M6**, the first two items of the build queue he set on 2026-08-07.

**The tutorial chain — five quests, levels 1→20 (`Game.Shared/Quests/Quests.Tutorial.cs`).** His
15-beat outline, whose point is *meeting every NPC in town* rather than the kills: Gatekeeper Pell
(free teleports to 40) → 5 pups → 3 → Huntmaster Cera (repeatable contracts) → 5 foxes → 6 → Spirit
Helper Nyra (she buffs 6-75) → Apothecary Miren (the free daily Rune) → 10 goblin scouts → 10 →
Armsmaster Dolan and the **Newbie kit** → 15 → Dolan again for the jewels + 1-day rune → 18 Elder
Marius → 19 High Priest Oren → 20 Class Master Vael.

It is five `QuestDef`s rather than one 15-step quest because a `QuestReward` pays on completion and
his outline pays three times along the way; they chain on `RequiresQuestId`, so the order still holds.

⚠ **It wraps the three class quests without gating them**, which is the rule he stated: *"U just can
lvl up to 20 go do the 3 quests and done.. / The chain is for the newebie equipment an the end
reward."* Part 5's steps are plain `TalkTo`s at Marius / Oren / Vael — the chain points at them and
at Cera's contracts and Miren's daily rune, and nothing in it is a prerequisite of anything they give.

**It REPLACES the old starter chain** (`starter_kit` / `starter_blooded`, `Quests.Starter.cs` deleted).
Those two were the same errand, and keeping both would have paid a new character two full kits. Parts
3 and 4 *are* those quests — same giver, same levels, same rewards, same verified pacing (12 000 exp
≈ 52% of level 10; 30 000 ≈ 39% of level 15), with the NPC beats built around them.

**Completion kit**, all bound: 1 Ultimate Scroll of Return ("of Escape"), 1 Ultimate Scroll of
Resurrection, 5 Dash Potion (Supreme), 5 Instant Healing Potion, plus 90 000 exp / 5 SP / 20 000 gold.

**M6 — the Newbie kit is a LOANER: bound, unsellable, 30-day timed, destroyable.** The "Newbie" ids
have been aliases onto the F tier's **Mythic** rung since 0.30-ish, and that is real ladder gear — it
drops, it is crafted, it is bought — so stamping *those* untradable and timed would have timed out
every Ferrite Mythic in the game. The quest boxes now hand out **bound copies** instead
(`ItemCatalog.BoundCopies`): cloned from the generated piece with `d with { … }`, so not one number is
authored and the gear CSV stays the only source of the kit's stats. Only the id (`_bound`), the name
(`Newbie …`), tradability, the prices and the 30-day clock differ. `SetId` is deliberately kept, so a
loaner body still completes its set — with loaner or with real accessories.

**Timed items are no longer a rune-only feature.** New `ItemDef.LifetimeSeconds` stamps
`ExpiresAtUtc` on the instance at ACQUIRE time (wall-clock, so it runs while offline), and the
expiry sweep — renamed `ReconcileRuneBuffs` → **`ReconcileTimedItems`** — now deletes *any* expired
item from bag, warehouse and account bank. Previously every one of those loops was gated on
`IsRune`, so a non-rune timed item would have sat there forever with a dead clock. A **worn** piece
expires too, and recomputes stats on the way out; otherwise the loaner armour would keep paying its
defence for as long as you never unequipped it.

Also folded in, since it is one line of the same box: **C15** — the **Ferrite Wand** is in the newbie
weapon selection box. A one-handed caster had no starter option at all.

⚠ **No db reset needed** (item defs are looked up live by DefId; the retired `starter_*` quest ids in
`CompletedQuests` are simply ignored). Characters that already hold un-bound `_t1` gear keep it — the
loaner only arrives via the quest.

## 0.53.2 — 2026-08-07 — Restore Spirit gets levels, the bow's accuracy roll goes flat 5

Two owner rulings in the band that has no CSV.

**The bow's accuracy roll is a MIRROR of the dual's evasion roll** — and carried the same defect,
inverted. `AccuracyPercent` (RampWide, cap 30) was a *multiplier* on a stat that already contains DEX
and level, so a maxed roll grew forever. It becomes `AttributeType.Accuracy`, **flat, cap 5**;
`RampEva` is renamed `RampFlat5` now that both flat lines share it. `AccuracyPercent` stays in the
enum, unrollable, so bows rolled before today still resolve — **no db reset needed**.

New `BalanceMatrix E1b` measures it and turns up a gap worth ruling on: accuracy and evasion are
**not symmetric**. `miss = clamp(5% + (eva − acc), floor, 95%)`, so evasion is additive against the
5% base from its first point, while accuracy can only claw back what sits above *both* the base and
the defender's evade floor. Against a rogue's 10% floor, +5 accuracy buys nothing until you already
out-evade him by 10+; against mobs a rogue is pinned at the 95% hit cap bare anyway — which is also
why deleting the old +30% cost nothing.

**Restore Spirit gets levels.** It had ONE level for life (20 MP for 65 HP @25) while the bolt ladder
grew 30 → 116, so it slowed the drain instead of sustaining a rotation. Ten levels now; level 10 @80
is **120 MP for 200 HP** — L2's Body to Mind (+120 MP / −360 HP) halved for our HP pools and rounded
to 200. Mage Armor Mastery gains rungs 5–8 @40/50/60/70 carrying `mpWhenRestored` 50/60/70/80. New
`SkillLevel.HpCost` / `HpCostAt` back the per-level HP price.

## 0.53.1 — 2026-08-07 — the rogue's evade ladder, `/spd`, and two clocks

See the three sections at the end of 0.53.0 below — they shipped in the same day's second pass:
the rogue's Evasion Mastery keyed to the class change rather than the level, the `/speed-*` family
collapsed into `/spd`, and game time + wall clock added to the status line.

## 0.53.0 — 2026-08-07 — the deletions, Heavy Draw off the rogue, and the ±20 lockout removed

Three items off the playtest-19 queue (`0a`, `0b`, `M7`, `M1`). Nothing here adds a system; two of
the three *remove* one, which is the point.

### `M1` — a 20-level gap is no longer a 100% miss lockout

`StatCalculator.ResolveAvoidChance` applied the class floors and *then* the level gap, so the gap
**overrode** them — and `LevelGap()` returns 1.0 at |Δ| ≥ 20. An admin with accuracy 9999 and a bow,
level 20 against a level-40 dummy, could not land a single hit, and no `precision` rung changed it.
That was the documented design; the owner overruled it, and the code backs him harder than he put it:
`ExpCurve.LevelGapMultiplier` already pays **zero exp AND zero drops from a 13-level gap**
(`GapZero = 13`), symmetric — seven levels *before* the lockout starts. The lockout protected nothing
and only produced "I swing forever and never connect".

**The change is the two steps swapped**: gap first, then the clamp into
`[max(0.05, defenderFloor), min(0.95, 1 − attackerHitFloor)]` **last**. `LevelGap()` itself is
untouched; `G = 1.0` stops meaning "lockout" and starts meaning "pinned to the edge of the band".
New precedence: `Immunity > SureHit > floors + the 5/95 band > level gap > stat roll`.

- level-20 rogue in a level-90 field: dodges **10%** (his Evasion Mastery floor), was 0%.
- level-20 warrior with Precision L1: lands **10%**, was never.
- no floor either side: **5%** each way, the universal band.

⚠ **The accepted consequence: nothing is unhittable.** A level-1 connects with a raid boss 5% of the
time — for no exp, no drop, and a swift death.

⚠ **`tools/BalanceMatrix` output is byte-identical before and after**, and that is correct, not a
missing measurement: the two orderings are arithmetically the same until `G` exceeds the floor window,
which needs |Δ| ≈ 19+. The tool builds same-level fights. This one has to be tested the way he found
it — a dummy 20+ levels away.

### The rogue's evade ladder follows the CLASS CHANGE, not the level

A same-day refinement of M7, and the more interesting half of it. `FloorPassiveFor` used a flat
`level >= 76 ? 3 : level >= 40 ? 2 : level >= 20 ? 1 : 0` for all four identity floors. For the rogue
that is now:

- **Lv1 at 20** — every rogue, on the 2nd class change.
- **Lv2 at 40 only on taking a MELEE discipline** (Phantom / Venomweaver / Nullblade). A rogue sitting
  at level 40+ with no discipline chosen keeps Lv1; a RANGED discipline keeps Lv1 forever.
- **Lv3 to nobody.** Its milestone is the **4th class change, which does not exist yet** — 76 is only
  a level, and granting a rung there hands out a 3rd-class-sized bonus for no class change at all. The
  rung stays authored; when the 4th tier lands, gate it on *holding* that class, not on `level >= 76`.

The owner's framing is the useful part: the floor rungs are **class-change rewards**, and using a
level number as a proxy only worked while every milestone happened to have a class change on it.
⚠ `precision` and `anti_magic` still use the plain 20/40/76 curve and so still grant a Lv3 at 76 — the
same argument applies to them, but he ruled on the rogue's, and that call is owed back to him before
the warrior and tank quietly lose a floor.

### `M7` — Heavy Draw is off the rogue at every level

The @24 grant was the last survivor of the dead Archer table that the 2026-07-29 merge folded into the
rogue wholesale (Battle Fury @20 went in 0.42.x for the same reason). It is gone, and so are its three
level-40 discipline renames — "Piercing Shot" (Sharpshooter), "Snare Shot" (Trapper), "Rending Shot"
(Hunter) — because he asked for both halves: *"remove it - remove it from after 40lvl as well"*.
⚠ **The `power_shot` SkillDef STAYS**, now with no learn assignment anywhere and a comment saying why:
he ruled on the grants, not the skill, and the level-40 bow CSV is where it comes back.

The other half of M7 is the evade ladder. `SkillCatalog.FloorPassiveFor` now takes a `Discipline?`,
and a **ranged** rogue discipline is capped at Evasion Mastery **rung 1** — *"the archer should not
have evasion mastery after 40 .. the 10% are ok"* — while Phantom / Venomweaver / Nullblade keep the
full 20/40/76 ladder. New helper `Disciplines.IsRanged`. Since the merge made bow-vs-dagger a level-40
choice, the DISCIPLINE is the only thing that can tell the two apart. The grant is a plain assignment,
so picking a bow discipline at 40 downgrades an already-granted rung 2 back to 1 — intended.

### `0a` / `0b` — the deletions, correctly scoped this time

Deleted: `reflexes`, `archer_armor_mastery`, `archer_weapon_mastery`, `dispel_magic`, and the whole
**God layer** — `Race.God`, `ItemRarity.God`, `god_judgment`, `god_robes`, `hp_boost`, `greater_heal`,
`Classes.God.cs`, both God 2nd classes (98/99, ids retired), the God speed row, and the client's
God-gear debug rows and `Race.God` skips. His rule underneath it is the interesting part: **nothing
exists in the game that cannot be acquired in the game.**

⚠ **`/enchant <value>` and `/speed` are the replacement debug rig and are load-bearing now.**
⚠ The Treasure Chest's 1-in-a-million jackpot *was* the God sword; it is now the S-grade Mythic 1H
blade (`sword1h_t80`) — still a jackpot, but something the game contains.

Kept, on his rulings: `evade_mastery`, `precision`, `anti_magic` (the live class floors — the
2026-08-05 correction stands), and `class_balance_*`, which is **commented out, NOT deleted**
(*"class_balance should be commented for now"*). The eight ids, `ClassBalanceFor()` and
`BalancePassive()` all remain in the file; restoring the hook is uncommenting two blocks. Because the
defs left the catalog, `AutoLearnCoreSkills` strips the ids from characters that already carry one —
they were all-zero `PassiveEffect`s, so no number moves.

### Two smaller things

**`/spd` replaces the four `/speed-*` commands.** `/spd <m|a|c> <value>` forces one speed stat
(uncapped, runtime only, exactly as before) and a **bare `/spd` resets all three** — one verb, a
one-letter channel, and no fifth command name for the reset. The old `/speed-cast`, `/speed-attack`,
`/speed-atack`, `/speed-move` and `/speed-reset` are **gone**, not aliased. Purely server-side: the
client already forwards any unrecognised `/word rest` to `AdminCommand`.

**Two clocks in the title bar**, right after the framerate: in-game time and the phone's wall clock.
The server has always sent `LoginResult.ServerEpochUtc` and the client has always discarded it —
storing it lets the status line read game time off the **shared** `GameClock` rather than a second
copy of the formula, so the two sides cannot disagree about what time it is. (The `DateTimeKind` is
normalised on arrival; `Unspecified` over the wire would have put the clock out by the timezone
offset.) ⚠ They are in the title bar because the framerate is; when that bar goes, both move with it.

## 0.52.0 — 2026-08-07 — the four playtest-19 defects + the whole friction tier

*(Entry written retroactively on 2026-08-07 from `docs/RoadmapNext.md` and the session notes.)*

**The four defects.** `48g` the 250k Blessing Box is no longer consumed on a partial pick — the server
requires exactly `PickCount` and the client's Confirm reads *"Choose N more"* until the tally is full ·
`46d` `/ptinv` reaches an out-of-sight player: the proximity lookup turned out to be in the CLIENT, so
a new `PartyInviteByName` hub method resolves the name server-side · `46m` `FindEquippedCounterpart`
matches `JewelType`, not just `EquipSlot.Jewel`, and picks the weaker of a worn pair · `M3` the live
tick crash — the main `Simulate()` sweep iterates a reused snapshot and skips anything removed
mid-tick.

**The friction tier.** `M13` the [Talk] button, walk-to-then-talk, and movement locked while an NPC
window is open · `M12` a gatekeeper jump lands *beside* the destination GK (fee still charged
centre-to-centre) · `M11` one daily Apothecary quest offered by and returned to every town
(`QuestDef.AnyTownNpc`) · `M14` buyback 24 → 12 · `M4` a dead character cannot move but **can** be
party-invited and **can** trade (his reversal: death must not block what undoes death) · `M2` five
`SocialOptions` flags, their commands, and a real Options window · `46o` both warehouse caps → 200 ·
`M10` `Two-Hand Mastery` `DefencePct` −0.20 → −0.10 (⚠ unmeasured — BalanceMatrix builds no
2H-mastery warrior).

⚠ Schema change (`SocialOptions`): **delete `Game.Server/game.db`**.

## 0.51.0 — 2026-08-07 — MAGIC crit is its own channel: WIT is a multiplier, damage is a flat ×3

The magic twin of 0.50.0, and the same diagnosis: the rate was authored as `WIT × 0.001` — **2.0%**
for a human mage — so the ×2 Insight buff bought **+3 percentage points** and the 20% cap needed
**WIT 200** to reach. It was decorative. Measurements: `tools/BalanceMatrix` **=== MAGIC CRIT ===**
(the formula sweep next to the same numbers measured off real `Entity` objects).

**The formula**, deliberately the same shape as physical:

```
magicCrit = ( 50 × witMod × passives × buffs  +  flat ) × debuffs
            clamped ONCE at StatCaps.MagicCritRate = 200 (20%)
```

- **Base 50** (5%) — a character constant, `StatCalculator.MagicCharacterCritBase`. There is **no
  weapon term**: magic crit is WIT and buffs only (owner: "it's not weapon based").
- **witMod is a multiplier and ASYMMETRIC** — `+0.10`/point above WIT 20, `+0.05`/point below,
  clamped at 0. The anchor is the **human mage's 20** (×1.00), so a fully-kitted elf (23 base + 2 set
  + 5 swap = 30) reaches **×2.00** and lands on the cap exactly with the ×2 Insight buff.
  ⚠ The two slopes are not an oversight — see the guardrail on `StatCalculator.CritWitMod`. A
  symmetric 0.10 zeroes the stat at WIT 10, which is *inside* the real range (ork fighter 10, every
  mob 5), and would have made mob magic crit a dead mechanic.
- **Crit DAMAGE is a flat ×3 that takes no bonus.** It was `2.0 + CritDamageBonus` — the *one*
  crit-damage field, shared with physical — so Ferocity and the crit-damage item attribute, both
  authored for fighters, silently paid a mage too. Magic is a separate channel on both counts now.
- **Mid-chain clamps removed.** The rate was clamped to the cap at the gear step *and* the passive
  step, i.e. before the buff multiplied it. That is the mechanical reason a ×2 was worth +3 points.

**What moved.** Warchanter's **Resonance** was `+5 flat points` — on a 2% base that was 2.5× the
entire rate and the biggest magic-crit source in the game; it is now **×1.2**, the same multiplier
convention `PassiveEffect.CritRate` already used. `StatMods.MagicCritRate` (armour sets) was
**never read at all** and is now wired as a multiplier; the `MagicCritRate` item attribute is wired
as flat but **still nothing rolls it** — deliberate, per the 0.50.0 "no flat crit gear" ruling.

**Measured consequence — read this before tuning.** At level 74 in best gear with the NPC buff set,
the nuker's expected magic-crit factor goes **×1.06 → ×1.24**, and `CHAMPION/NUKER` moves
**0.98× → 0.84×**: the nuker now out-damages the champion by ~19% where they were at parity. That
is the honest price of the rework and it is a balance decision, not a bug — the levers are the base
50 and the flat ×3, both owner-set.

**Still open:** physical crit damage (base ×2.0) is **unchanged and under research** — the question
is not only 1.5-vs-2.0 but *what the multiplier multiplies*, since ours scales skill power too where
L2's scales the attack term. The 76+ "×1.3 rate, +5% cap" buff is deferred with the 4th-class CSVs;
it needs `StatCaps.MagicCritRate` to become per-entity raisable, like the move-speed cap.

## 0.50.0 — 2026-08-06 — Crit RATE is his L2 model; `Can Crit`/`Can Double` are exclusive

Playtest-19 **M8 + M9**, closing the crit-rate/crit-damage thread. Spec:
[design/CritBlowAndDouble.md §5](design/CritBlowAndDouble.md); measurements: `tools/BalanceMatrix`
**§C2** (the crit chain decomposed) and **§C3** (the M8 flag audit).

**The formula.** Crit rate is now his L2 model end to end:

```
crit = ( 110 × weaponFactor × dexMod × passives × buffs  +  flat ) × debuffs × enemyLightArmorMastery
       clamped once at StatCaps.PhysicalCritRate = 500 (50%)
```

- **The weapon multiplies a character base of 110** (11%) instead of DEX being the base. The existing
  `WeaponCritFactor` (1.2 / 0.8 / 0.4) already carried his 3:2:1, so dagger/bow land on **132**, sword
  **88**, blunt **44** with no new table.
- **DEX is a mild multiplier**, `1 + (DEX − 30) × 0.01`, linear and uncapped, centred on
  `MobDexReference` so a normal mob is exactly ×1.00. It is deliberately the smallest of DEX's four
  jobs — a DEX point is worth +1.0pp of accuracy but +0.13pp of a dagger's crit.
- **Passives and buffs MULTIPLY** (`Entity.CritRateMult`); they used to add points.
- **Flat sources land outside every multiplier** (`Entity.CritRateFlat`) — "a flat 30 is flat 3%, not
  increased by buffs". Buff code did the opposite, `(crit + flat) × (1 + pct)`.
- **`CritRateResist` became a multiplier.** As a subtraction it annihilated low-crit builds: an 11.4%
  blunt warrior against a rogue's 0.15 light-armor resist critted **0.0%**; he now keeps 9.7%.
- The three stray **0.75** clamps along the chain are gone — there is one clamp, and it reads
  `StatCaps`. Magic crit likewise now clamps at its real `StatCaps.MagicCritRate` (0.20), not 0.5.
  ⚠ Magic crit stays **additive**: his ruling named "dagger/bow", and a mage's base is a 4% WIT figure
  where a ×1.05 would be nothing. Flagged, not silently changed.

**M9 authoring.** `evade_mastery` is the **evade floor and nothing else** — the `+20% crit` and `+20
evasion` are gone (the evasion budget was already closed at ~18 by armor + buff). Its crit moved to the
rogue **Weapon Mastery at level 20**, as ×1.20 on all five rungs: he wanted the high crit rate early,
not the spike at 32 that the old `+10/+10` at 32/36 produced.

**M8 — `Can Crit` / `Can Double` are exclusive, opt-in flags.** New `SkillDef.CanCrit`. A physical skill
now rolls a crit only if it says so; a `[Double]` skill never also crits; a skill with neither lands flat
(it can still miss and still be blocked). A `[Double]` also stopped **reporting itself as a crit** —
new `CombatOutcome.Double`, shown as `N x2` in its own colour, which is the whole point of the `[Double]`
naming. §C3 audits every physical skill's flags against its own description; all 20 agree.

**New: `SkillDef.CritRateMod`** — a per-skill crit-rate multiplier, L2's rule that a blow's landing
chance was never the raw crit rate. Stab and Piercing Stab carry **×2.0**. This is the knob the rework
is *paid* for with: it lifts the dagger's blow without touching basic-attack crit, buffs, or any other
class.

**🔑 §50h was a measuring error, not a balance finding.** `BalanceMatrix` never granted the archetype
identity floor passive (`FloorPassiveFor`, auto-granted in game by `AutoLearnCoreSkills`), so it
measured a rogue with **no Evasion Mastery** — hence "9.2% crit, 0.65× warrior DPS". With the passive
in the model the *old* rogue was at **29.2% crit and 0.99× / 1.08× / 1.46× / 1.63×** the warrior at
20/28/32/36: already at parity, then running away exactly at the level-32 spike he predicted. Both
builders now grant it.

Measured rogue-vs-warrior DPS, old → new: **0.99 → 0.92** (20), 1.08 → 1.00 (28), **1.46 → 1.10** (32),
**1.63 → 1.22** (36). The 32+ spike is gone and the curve is smooth.

⚠ **One authoring gap this exposes.** The flat term is the part of his model that carries the classes
that cannot multiply — his "elegia heavy set +127". We have **no such source**: flat crit rate exists
only as a random weapon attribute, and only on sword/dual/bow. So a 2H-blunt warrior sits at 4.4% with
nothing to raise it. §C2 prints `flat = 0` on every row to keep this visible.

No protocol bump (`CombatOutcome.Double` is an appended enum value), no db reset. Needs an APK.

## 0.49.0 — 2026-08-05 — Crit damage is FLAT, blows scale off it, `[Double]` is an ATK curve

His 2026-08-05 ruling, built in one pass. Design: [design/CritBlowAndDouble.md](design/CritBlowAndDouble.md);
measurements: `tools/BalanceMatrix` §C1.

**The misreading.** Every class CSV writes crit damage as a `+` — rogue `+35/+64/+80/+140/+165`,
warrior 2H `+35/+48/+64/+84/+106`. The catalog divided each by 100 and fed it to `PhysicalCritMult`
(`2.0 + bonus`), so "crit dmg +80" quietly meant "crit multiplier ×2.8". It is a **flat number**, and
it joins **attack** inside the ratio on a crit only:

```
crit damage = K · ((atk + flatCritDmg)·… + power) / def  ×  critMult
```

so it is divided by defence like everything else and off a crit it does nothing. New field all the way
down: `PassiveEffect.CritDamageFlat` → `Entity.CritDamageFlat` → `StatCalculator.CritFlatFactor`, which
expresses the term as a factor on the finished hit — exact, because everything after the ratio
(variance, weapon coef, the damage-out pipeline) is a linear multiplier.

**Blows.** A landed blow returned `baseDamage` **unmodified**, so a dagger's whole crit-damage ladder
did nothing to Stab. `ResolveBlow` now applies the crit-damage values to a landed blow — flat add, then
the multiplier — before the `[Double]` roll on top. Expected damage per blow at the five rungs roughly
doubles: **90 → 148** at level 20, **178 → 354** at 36. That is the rogue's entire scaling ladder
switched on; measure before touching those five numbers.

**`[Double]`.** Ours *is* L2's physical skill crit — a flat ×2 that never touches crit damage, which is
the whole reason for the name. Its chance is now a pure **ATK** curve, `min(25, 2.5 + 0.75·(ATK−30))`,
capped by `StatCaps.PhysicalDoubleRate`; it no longer reads DEX at all. **DEX makes a blow land, ATK
makes it double** — paying the ×2 off DEX too would pay the rogue twice for one stat and give the
warrior nothing from the mechanic. And `[Double]` on a **buff or debuff doubles its duration** (L2's
level-76 Skill Mastery): one roll per cast, player casts only, shown as `Name [Double]`.

**Two authoring bugs fixed with it.** The rogue weapon mastery's @24 and @28 rungs were **swapped**
(80 before 64). The rogue ARMOR mastery gated *everything* on light armour, while the CSV puts MP
regen, HP regen and P.Def under **`with all`** — so a rogue in robe or heavy got nothing at all from it.
The warrior's identical CSV grammar was already implemented correctly, which is what proved it a slip.

**Client:** the stats window gained `Crit dmg flat +N` / `[Double] N%` (the second derived client-side
from the ATK stat). `StatsUpdate.CritDamageFlat` is a trailing optional field — **no protocol bump, no
db reset**. **BalanceMatrix** gained §C1 (the curve old-vs-new, both mastery ladders, rogue-vs-warrior
DPS), a `BuildRogue` that actually wears daggers and light armour, and `TopSkill` now honours the
weapon gate the server enforces — without it a sword-and-shield tank was being measured on Stab.

## 0.49.0 — 2026-08-05 — The enchant rework (`D1`/`D2`): two axes instead of three scrolls

The last unbuilt pass of the playtest-17 batch. Until now an "enchant scroll" was three items whose
only difference was what a *failure* cost, and **any** of them worked on **any** item — so the Common
scroll you found at level 10 was a legitimate tool against endgame gear, and the ladder said nothing.

The owner's `D1` splits that into **two independent axes**, and both now mean something:

| | |
|---|---|
| **TYPE** — what a failure costs | **Scroll of Enchant** destroys the item · **Greater** drops it by 1 · **Safe** (new) keeps the enchant |
| **GRADE** — what it may be spent on, signalled by RARITY | Common→**E** · Uncommon→**D** · Rare→**C** · Epic→**B** · Legendary→**A** · Mythic→**S** |

Three types × six grades = **18 scrolls**, generated from one table (`ItemCatalog.EnchantScrollBands`
× `EnchantScrollTypes`) that the catalog, the drop layer and the admin menu all read, so a rung cannot
be authored in one and missing from another. One grade below the attribute-scroll bands, as specified.
**There is no F scroll** — F is the training tier you leave by 20, which is exactly why `D2` asks for an
unrestricted admin path.

⚠ **The three original ids are kept and re-pointed** (`scroll_common`/`_uncommon`/`_rare` → the E/D/C
Normals). Those three shipped at Common/Uncommon/Rare rarity and the new rule maps straight onto that,
so every saved bag, box table and crafting recipe naming them stays valid — **no db reset**. What
changes is their failure behaviour: the D and C ones used to reset to +0 / drop 1, and all Normals
break now.

**The band is enforced in Shared** (`EnchantRules.Accepts`), which is the same code the client filters
its target list with — so the picker can never offer something the server then refuses, and the
refusal names *both* sides rather than leaving you to guess which half you got wrong.

**Drops are BANDED, not floored.** A grade-locked scroll that keeps dropping forever is bag clutter by
construction — a level-80 farm raining E scrolls nobody can spend — so each rung now has a ceiling as
well as a floor, generous enough (it lives until the band *two* above opens) that you keep finding
scrolls for gear you are still wearing. Measured per kill: **0 % below 20, 6 % at 20-39, 10.5 % at
40-51, 7.5 % at 52-60, 5.3 % at 61-75, 2.3 % at 76-79, 0 % at 80+** — against a flat 10.5 % from
level 55 before. The normal-mob faucet **closes at 80** on purpose: from there the scrolls are an
elite/boss reward, which is his ladder *and* the first real reason for a level-80 farmer to clear an
elite camp.

**A/S scrolls and every Greater and Safe are elite/boss only** (`MobCatalog.EnchantScrollDrops`, layered
at kill time by RANK the way `GearDrops` already was, and added to the target-inspect list in the same
breath so the number shown stays the number you get). It is keyed off the *band the mob's level sits
in*, which is why his whole spec falls out with no special cases: an elite at 78 is in the A band, a
boss at 82 is in S. An elite pays its band's Normal at 9 % and Greater at 1.8 %; a boss pays its band
**and the one below** at 30 % each, Greater 9 %, and **Safe at 0.45 %** — the rarest line in the game.
The "dungeon monsters at 90" rung has nowhere to live until instances exist; it is flagged, not faked.

**`D2` — `/enchant <value>`** opens your own bag as a picker and sets the chosen piece outright,
bypassing the band, the scroll, the success roll and the +16 ceiling, because its job is to reach
states the ladder cannot (`/enchant 999999` on an F weapon works; the only bound is an anti-overflow
clamp far above it). Handled client-side like `/offline` and `/ptinv` — it needs a picker, and the
picker is a window over the bag the client already holds — with the staff role re-checked server-side
on `AdminEnchantCmd`. **All 18 scrolls are in the debug menu**, grouped by grade.

`tools/BalanceMatrix` grew a **§D1** that prints the 18 (asserting each one's type/grade/rarity axes
actually landed on the def) and the elite/boss layer at every band, plus an integrity check for the
rank entries — which live in no template and so never reached the existing drop-table check.

⚠ **Needs a new APK** (the item catalog ships inside it). Protocol unchanged at 12, no db reset.

## 0.48.0 — 2026-08-05 — A text box you cannot type into, x1 rates, and `E3`

**🔴 The blocker first: every text box in the client was un-editable if it already held a value.**
Owner, on 0.47.0: *"when I try to edit saved login to change the pass I cannot go below the saved part
… if there is a 1 I cannot make it 10, it becomes 01, 101, 11 — never 10 … cannot edit rates."*

Two faults, and the first one is ours from 0.46.0. `B6` turned **off** select-all-on-focus so that a
pre-filled box could be edited instead of wiped — correct, and half a fix: with select-all off TMP
restores the caret to the field's *stored* position, which for text set from code is **0**. So
focusing a filled box parked the caret at the FRONT and every keystroke prepended. The second fault
made it unrecoverable: on Android the soft keyboard owned the text buffer, so the TMP caret was not
TMP's to move and **tapping inside the field could not reposition it** — there was no way to reach
the character you wanted to delete.

Fixed as the other half of B6: **focus now lands the caret at the END** of whatever the box holds
(`UiKit.CaretToEnd`, waiting a frame because `ActivateInputField` and the keyboard both finish after
the select event and would overwrite it), and **`shouldHideMobileInput = true`** hands the text back
to TMP so the keyboard only delivers keystrokes and a tap inside the field places the caret. One
builder, so it reaches the login boxes, the debug tuning rows, chat, character creation, trade gold
and the vendor numpad together. ⚠ If a device ever refuses to open a keyboard for a hidden input,
that flag is the line to flip back; the symptom would be "no keyboard", never "the caret is stuck".

**Rates default to x1** (owner: *"make default x1 exp/drop/sp, I'll tune them if I need to"*). `ExpRate`
10 → **1**, `DropChanceRate` 3 → **1**, `SpRate` already 1. ⚠ The drop knob could not simply move:
gear groups and the independent rolls were taking that x3 and were tuned *through* it. So the x3 was
folded into them — gear `0.025 → 0.075`, `other → 3` — and `BalanceMatrix` confirms every delivered
number is unchanged (reference farm still **1,038,115**, attribute scrolls still 3.6 %/kill). The
knobs read 1 and the game plays exactly as it was measured; only the units moved. **Exp really is 10×
slower now** — that one is a real change, and the one he asked for.

### `E3`: the buff economy, and the game's first gold sink

**No protocol change (still 12), no schema change, no db reset.** But the item catalog lives in `Game.Shared`,
which ships *inside* the APK — so this needs a **new client build**, not just a server one.

**The shape changed, not just the numbers: a potion is what you FIND, a scroll is what you BUY.** They
used to mirror each other rung for rung — same buff, 20 minutes vs an hour — which meant the top of
every ladder fell out of the sky for free and the paid layer had nothing left to sell.

- **17 scrolls, down from 48.** One per buff, at its family's MAX rung, all Rare, all **bound**. A
  boxed set is literally an NPC buffer's blessing for an hour. The eight scroll-only families keep
  their rung 6 — which is the first time the **Mythic rung has had any source at all**.
- **18 potions, down from 27.** Rungs Common and Uncommon; the Rare potion is gone. A family now
  reads *Lesser (found) → plain (found) → **scroll** (bought)*.
- **The Blessing Box** — 250k at the Apothecary, **pick 10 of the 17**, and the only source of a buff
  scroll in the game: no drop, no boss, no craft, no shelf. Two boxes cover all 17 ≈ an hour's
  farming, deliberately: a live buffer has to stay the better deal. The **box** is tradable and sells
  at Value ÷ 25; what comes out of it is not.
- **Dash left the Apothecary** (drop + boss points only, his spec) and keeps its old drop rate exactly.
- Deleted: 43 item defs, 18 scribe recipes. Their ladder SKILLS stay — generated in bulk, unreferenced,
  free. A DefId that leaves the catalog is dropped on load, so an old save just loses them.

**⚠ The trap, worth remembering for any weighted group.** A drop rung splits half its weight among
however many ids are in it, so deleting 17 of a rung's 19 would not have removed those drops — it
would have handed their share to the surviving potions and **doubled the potion faucet** as a side
effect of a change meant to cut drops. The buff half is an explicit per-item chance now, set to what
each item delivered before. Measured: consumables per kill **33 % → 18.5 %** at level 33, and total
farm gold **unchanged at 1.04× target** — buff potions already sold for 0, so this is a **bag** fix
and a **sink**, never a gold change. `tools/BalanceMatrix` asserts *0 of 17 in drop tables*, using the
box's own contents as the list so the guard cannot drift.

**Client — the selection popup learned to pick many.** Rows toggle `[  ] / [x]`, the title tallies
`3 / 10`, Confirm sends them in one go, and the 11th tap is refused out loud rather than silently
dropped (the server takes the first `PickCount` it recognises, so a swallowed tap would spend a 250k
box on a set the player did not choose). The server side already handled it; only the chooser was
pick-one. ⚠ The tick is ASCII: the TMP atlas is baked, and a checkbox glyph draws as a hollow box.

## 0.47.0 — 2026-08-05 — The playtest-18 friction tier, and four defects a review caught first

**Protocol stays 12; no schema change, no db reset.** This is the label for everything that landed on
top of 0.46.0 unpublished: the playtest-17 inventory-hygiene tier, the whole playtest-18 quest section
(`Q1`-`Q5`), and the four items below.

**`F1` — turning auto-farm off no longer drops what you are fighting.** The server was swinging the
whole time: `AutoPilot` pushed `AutoTarget(null)` on the first manual tick and the client's selection
follows that push, while `CombatTargetId` / `Engaged` / `AttackCommandTargetId` all stayed set — so
*"i must reselect mid fight to finish the kill"* was re-selecting something already being hit.
Switching to manual now **hands the target over**: it re-pushes the current target for as long as that
still names something alive and present, and only then null. `StopAutoHunt` (death, spent budget)
clears `CombatTargetId` itself, so the paths that really end a fight push null exactly as before.

**`V1` — quick-sell.** `QSell: off/ON` beside the Sell tab, in the bin's armed red, and hidden on the
buy side — a toggle that goes dead when you tap Buy reads as a bug. On, a row sells the WHOLE stack in
one tap: no numpad, no confirm, non-stacking rows included. It may skip the confirmation where the bin
cannot, because a sale is undoable from the buy-back list.

**`G4` — save-login checkbox.** `[x] Save login on this device` under the password field. It stores
username *and* password only after a login that actually SUCCEEDED (storing on submit would remember a
typo forever), and OFF stores neither, comes up blank, and **wipes what is already on disk** rather
than merely stopping future writes. The server ADDRESS is remembered either way — it is not a
credential. Default on with the inspector's admin/admin as the fallback, so the debug rig is untouched.
⚠ PlayerPrefs is not a secret store; that is what every phone "remember me" gives, and it is why this
is a choice rather than unconditional.

**`G5` — Dash and Sprint become ONE speed family.** Ranked by magnitude end to end:

| rank | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---|---|---|---|---|---|---|---|
| | Dash C +15 | Dash U +30 | **Sprint L1 +40** | Dash R +45 | Dash E +50 | Dash L +55 | Dash M +60 | **Sprint L2 +60** |

which is his spec exactly: Sprint L1 replaces Dash C/U, Sprint L2 replaces everything including Sprint
L1. Sprint L2 sits above Dash M at the same +60 **on purpose** — a class skill you levelled must not be
overridable by a bottle, the same rule a group buff follows. ⚠ `Rank` lives on `SkillDef` and not on
`SkillLevel`, so a two-level skill cannot vary its rank: Sprint is authored as a **one-child wrapper**
whose level picks the child, the machinery a potion already uses. Its old private `"sprint"` BuffKey is
gone — that key having a family to itself is what let Sprint and a Dash potion both run at once.

**Four UI defects found by review before they were ever played:**
- A quest token already IN a warehouse **could never be taken out**. `B4` stops one going in, but the
  private bank accepted them until `B4` shipped, and `InCategory(All, …)` hides Quest while the keeper
  has no Quest tab — so the token was unreachable and its quest step stalled, with every tab reading
  "Your warehouse is empty". The All tab rescues it on the withdraw side now.
- The quest tracker sized its rows by counting `\n`. Word-wrap is on, so a real objective is one
  newline and two drawn lines: wrapped rows came out short and spilled onto the pin below. It measures
  with `GetPreferredValues` now, and the panel — a fixed 180px, which `Q2`'s auto-pin made worse —
  grows with its content.
- The bag's paper-doll column hung off the bottom of the window: `C8`'s second tab row pushed it down
  36px and **preset C drew outside the window, over the world** (a `PanelBox` does not clip). The bag
  is 60px taller while the column is open, and the list takes the extra height.
- Opening compare moved the column you were reading a quarter-screen right — the panel's pivot is its
  centre, so growing it pushes both edges outward. Shifting the panel left by half a column cancels it.

**Not built, and why:** `G1`'s skill deletion. Checking the list against the code first showed five of
its lines were wrong — `evade_mastery`, `precision` and `anti_magic` are **auto-granted to every rogue /
warrior / tank** at 20/40/76 by `AutoLearnCoreSkills`, and `class_balance_*` to every character alive.
They feed `EvadeFloor` / `HitFloor` / `MagicFailFloor` and are the class identity floors documented in
`design/CombatResolution.md`; they are absent from the CSVs only because they are auto-granted rather
than learned. See the red block in `testing/Playtest-Archive.md#skills-not-in-csvs` §3. Nothing was deleted.

## 0.46.0 — 2026-08-05 — Four playtest defects: auto marks, distant party targets, empty slots, undo

⚠ **Protocol 12** (`MinAcceptedProtocol` stays 8, so an installed 0.45.x APK still connects — it simply
has no Restore window). ⚠ **Schema change from the account-budget entry below — delete `game.db`.**

**`B1` — the auto-on flag "stored per account".** It was never on the server, where the marks have
always been a per-character column. `AutoSkills` is a `HashSet` on the singleton `GameBoot` and
**nothing ever cleared it**: not on leaving the world, and not when the server pushed an EMPTY list,
because that push was guarded by `c.Skills.Length > 0` to protect a "basic attack on by default" that
had already been removed. So one character's marks walked into the next one's session, and a freshly
created character arrived already auto-farming an action it had never been given. The guard is gone (the
server's list is the truth, including when it is empty), `ResetWorldTransients` clears the set with the
rest of the per-character state, and `AssignSlot` clears the auto mark of whatever token it displaced —
unless the same token still sits in another slot, so *moving* a skill never disarms it. That is the
second half of his report: *"removing something from the bar automatically disables the auto-on."*
`ToggleAutoSkill` also pushes unconditionally now; it used to push only while auto-hunt was running,
which left a mark made with auto-hunt off living nowhere but the client.

**`B7` — a party member out of range could not be targeted.** Tapping the roster row *did* set the
target; the next world delta threw it away. Interest management stops sending an ally who walks out of
view, and `GameBoot` cleared any target missing from the snapshot — ten times a second — as a ghost
guard. Party members are exempt from that clear now, and the target frame falls back to the ROSTER row
(name, level, class, both bars, `(out of sight)`), which keeps arriving at any distance. So assist,
heal, buff, kick and change-leader are reachable at exactly the range they matter.

**`G7` — a hotbar consumable at 0 count was disabled**, and `PressAndHold.Enabled` is wired to the
button's `interactable` — so the slot also lost the only gesture that could remove it, and the bar
trapped a square you could never clear. It stays interactable now and draws the reuse sheet at FULL
height with no countdown text (there is no timer, only an empty bag), which is exactly what he asked
for: *"make it like always in 100% cooldown - it looks the same just is not disabled."* The tap is inert
rather than earning a refusal from the server.

**`C18` — undo a bin-delete, for free.** Designed since 2026-07-24, never built, and it had already cost
him an item he binned by mistake. Built as **his own fallback shape, two separate lists**, which is the
better one: a shared list would let a selling spree push the single thing you meant to undo off the end,
and the two accidents have different prices anyway. `Entity.Restorable` keeps the last
`GameConstants.RestoreSlots` = **5** binned items with their enchant and rolled attributes, so a +6
sword comes back a +6 sword; `HandleRemoveItem` records the exact quantity destroyed (the bin numpad can
take part of a stack). `RestoreItemCmd` carries **no npc id at all** — that is the whole point, and it is
why the window opens from **Menu → Restore** and not from a vendor: you bin things in the field, which
is where the accident happens, so an undo you can only reach in town is no undo. Newest row first. The
vendor half of the old design (a longer sold list) is still open and still not urgent.

## 2026-08-05 — The farm allowance belongs to the ACCOUNT, and it is a balance, not a stopwatch

⚠ **Schema change — delete `Game.Server/game.db` (+ `-shm`/`-wal`).**

The owner's 14-hour, three-character offline farm turned up something the caps were supposed to prevent
and never did. *"I get the Timer back"* — he was exactly right, and the code agreed with him twice over.

The idle/offline caps were **per-session elapsed counters on the CHARACTER**, and they were zeroed in
two separate places: on every `EnterWorld` (*"Fresh session: refill the runtime budgets"*), and again
inside `BeginOfflineFarm`. Neither counter was ever persisted. So the 2h offline cap worked exactly once
per login: farm 2h → the session ends → log back in → a fresh 2h, forever. And because the counters were
per-ENTITY, three characters on one account farmed **six hours per two hours of wall clock**. The 8h
online cap had the same hole; its `AutoHuntLocked` flag, the thing that was supposed to mean "no more
until tomorrow", was cleared on the next login.

So the allowance stopped being an elapsed time compared to a cap and became a **balance that is spent**,
on the **account**:

| | Free | Premium |
|---|---|---|
| Online auto-hunt | **8h / day / account** | 12h |
| Offline farming | **2h / day / account** | 4h |

The drain rule is one line and every property anyone wanted falls out of it: **each tick, every one of
the account's characters that is farming spends one tick of the balance.** One character gets the full
2h; ten characters get twelve minutes each; no branch anywhere counts characters. That is deliberate —
ten characters × 12 min yields exactly the same gold as one × 2h, so the thing being capped is
**gold/hour/account**, which is the only quantity worth capping.

**Refill is a fixed server midnight**, applied lazily: the row stores a DATE, and the first read of a new
day tops both balances back up. It therefore accrues correctly across a server restart with no scheduler
and no catch-up pass. A rolling *"24h since your last refill"* was proposed and **rejected by the owner,
correctly** — it anchors the reset to whenever you last spent, so it drifts: play at 08:00 and your next
window is 08:00; miss it, start at 22:00, and the window walks round the clock until it costs you a whole
day. A daily allowance has to be spendable anywhere in the day. The known consequence — start at 22:00,
drain 2h, reset at 00:00, drain 2h more — is **accepted on purpose**: it still averages to the cap per
day, and he named the trick himself and called it player agency.

Continuous regen (a tank that refills at some rate per hour) was considered and dropped, for two reasons
that are worth keeping: regen *while spending* makes the real drain `1 − rate`, so a "2h" tank silently
runs 2h40m at 0.25; and regen only while *not* offline-farming is still free money, because the refill
window is exactly when you are online auto-farming — the other budget. `2h off → 8h auto → 2h off` is an
ordinary day, and a "2h cap" would really pay 5-6h per 24h. The regen model belongs to **rested XP**
instead, where the reward is EXP rather than a gold faucet.

The lock is gone with the counters. `AutoHuntLocked` only ever existed to mean "the cap is reached" and
it was cleared at login, which is how the cap was escaped; the balance itself is the gate now, and it
refuses the toggle, the config, `/offline` **and** the disconnect-to-offline-farm branch. That last one
matters: without it, dropping the connection with an empty allowance would park you as an offline farmer
and eject you on the next tick, printing *"X keeps hunting while away"* and *"X stopped hunting"* one
after the other.

- New: `Simulation/AccountFarmBudget.cs`, `World.AccountBudgets` (keyed by AccountId, same lifetime rule
  as `AccountWarehouses`), five columns on `AccountRecord`, and the load/save pair in
  `PersistenceService`. Saved on the 60s autosave behind a dirty flag, flushed immediately on
  `NormalLeave` / `EndOfflineSession` so a crash can't hand back time already spent.
- Premium is the per-account cap override (`-1` = server default, `0` = unlimited, `>0` = explicit), set
  with the new admin command **`/farmcap <player> <autoHours> <offlineHours>`**.
- The Debug panel's cap rows and `/testcaps` now call `RefillAllBudgets()`. With a balance model,
  lowering the cap to 30s does nothing on its own — the 8h already in the tank is what the loop spends,
  so a tester would have waited eight hours for a "30 second cap".
- Gold sellers, honestly: account budgets fully kill *"one account, ten characters"*. They do **not**
  kill *"ten accounts, one character"* — account creation is free, so the farmer's cost goes from
  invisible to merely linear. Worth having anyway for the designed ceiling, but the V2 economy cut below
  does more than any detector will.

## 2026-08-05 — Playtest-18 V2: the gold faucet, cut on the DROP RATE instead of the price

The owner's reported "equipment sells at 0.8" turned out to be his own misread — he sold a Feretite Robe
and read the number as the gloves'. No bug. But the ask underneath was real: *"selling items/trash making
money ok .. but not farming."*

He then produced the best economy measurement this project has had: **three characters through the same
~14-15 h idle farm**, differing only in what they sold. A mage that sold nothing finished level 34 with
**350k**; a tank that sold only equipment finished 36 with **3.3kk**; a rogue that sold everything
finished 34 with **4.6kk**.

`tools/BalanceMatrix` gained a section that reproduces exactly that experiment. It calibrates on the
COIN — the one component with no player choice in it — so 350k resolves to **1,211 kills at ~84/h**, and
then prices those same kills through the real drop tables. It lands within ~15% of both other characters,
which is what makes the rest of the tuning trustworthy rather than derived.

The verdict it returned: **gear is the entire faucet.** Sold gear was **10× the mob's own gold drop**;
mats and potions together were **2%**. The owner's tank banking 3.3 of the 4.6 while selling only
equipment is the same finding arriving from the other direction. Cutting `VendorSellFraction` — the
obvious-looking knob — would have achieved nothing at all.

His first proposal was to cut the sell price ×0.1, which measures correctly (0.80× of his ~1kk target)
but only moves one number. The drop VOLUME would have been untouched, so the offline farm still buries
the player in junk to click through, and the buy:sell spread would have gone to 250:1 — a Robe you find
sells for 450 while the shop sells it for 112,500. He agreed, and chose the sharper version of the
alternative: put the cut on the rate and push the price the *other* way, so what you do find is worth
finding.

| | before | after |
|---|---|---|
| gear group multiplier (`RateConfig.DropGroupRates`) | ×1/3 | **×0.025** (13× rarer) |
| `GameConstants.GearSellDivisor` | 25 | **10** (worth 2.5× more) |
| gear sales over that farm | 3,619,984 | **678,747** |
| consumables + mats | 85,688 | **198,542** |
| coin | 350,000 | 350,000 |
| **total** | **4,055,588** | **1,227,289** |
| gear : coin ratio | 10.3× | **1.9×** |

"Ten Robes buys one Leathers" replaces the old ÷25 acceptance test.

Two things the measurement surfaced that are **not** fixed. First, the consumable line went *up* 2.3×,
because `GearSellDivisor` governs use-consumables as well as gear — the belief that buff potions are
0-sell and scrolls don't drop does not hold (`GroupScrolls`, 70% guaranteed, carries enchant *and* buff
scrolls; `GroupAlways` carries Scroll of Return and Resurrection). Consumables are now 16% of income and
are what puts the result at 1.23× rather than 1.0×; left alone deliberately, being inside the noise of
the original measurement. Second, gear sale value follows the tier ladder while the mob's coin drop is
linear in level, so *any* flat multiplier fixes one band and drifts — 1.9× at level 33, 16.7× at 52,
51.7× at 76. Much flatter than the 275× it was, but the same shape. The real fix there is the coin curve,
not another multiplier, and it can wait until the endgame is actually played.

The sweep in BalanceMatrix now normalises against the LIVE values of both knobs rather than hardcoded
1/3 and 25, so it keeps telling the truth after any future retune. Full detail:
`docs/design/EconomyRework.md` §4a.

**The scroll pass, same day.** The owner rejected the consumable finding on its merits — Return and
Resurrection were already cut 20× and 200× in playtest-17, and *"are usefull u wont be seling all"* — and
redirected it at the enchant and attribute scrolls. Measuring that flipped the diagnosis twice. Enchant
and attribute scrolls carry no `Value:` at all, so they **already sell for 0** and cannot feed the gold
economy however many drop; what they flood is the bag. The gold in the consumable line was **buff potions
and buff scrolls** at 155/kill — his playtest-17 decision that *"buff pots are 0 sell (ppl still can sell
them to others if they want)"* had never actually been implemented, and the new ÷10 divisor had just made
them 2.5× richer. And attribute scrolls landing on 27% of kills was a mechanical accident: they are
INDEPENDENT drop rolls, so they take the global ×3 that the guaranteed groups are exempt from — authored
0.09, delivered 0.27.

Three changes: `SellPriceOverride: 0` on every buff potion, buff scroll and Dash potion (Value stays — it
is still the buy price, and player trade is untouched); the enchant scroll's share of a scroll rung cut
from 0.5 to **0.15** with its level floors moved from 1/20/45 to **10/30/55**; and the attribute scrolls
cut ~5× and spread across the band they serve instead of all arriving at 40 (floors now 40/52/61/76/80/84).

| level | enchant | attribute | buff gold/kill |
|---|---|---|---|
| 33 | 30 % → **9 %** | — | 155 → **2** |
| 40 | 30 % → **9 %** | 27 % → **3.6 %** | 155 → **2** |
| 85 | 35 % → **10.5 %** | 39.9 % → **9.6 %** | 747 → **1** |

BalanceMatrix gained a SCROLLS section that reports per-kill frequency by family, because for these items
frequency — not gold — is the thing being tuned. The reference farm now totals **1,038,115 (1.04× target)**,
split gear 65 % / coin 34 % / consumables 1 %.

## 2026-08-03 — Playtest-17: the whole backlog played in one pass (no code)

Six versions of unplayed work went through in a single sitting on the 0.45.0 APK: §36 mob regen, §38 the
account warehouse, §39 repeatable quests, §40 the quest window, §41 the mob cast bar and target circles,
§42 titles and chat tabs, §43 accuracy + attributes + the scroll windows. **84 checklist items verified,
and 22 of the playtest-11 findings from 2026-07-24 finally closed.** Nothing came back as a broken
system — the failures are edge cases (a per-account auto-farm flag, a text box that wipes its own
pre-filled value, a pendant opening a ring's window, an out-of-range party member that cannot be
targeted, Soulcrystal gear printing A grade while taking a Mythic scroll).

What replaced them is a *game* list rather than a bug list: inventory filters and tabs, quest items that
must be refused by every disposal path, drop faucets he measured at level 23 (return scrolls ÷20, heal
potions ÷10), a fully-specified buff-scroll economy (no scroll drops at all, an Apothecary selection box
at 250k for ten), a three-type enchant rework (breaks / −1 / **safe**, with the scroll's rarity choosing
the GRADE), and **crafting, which is now the single blocker for anything above Epic rarity.**

Report: [testing/Playtest-Archive.md#playtest-17](testing/Playtest-Archive.md#playtest-17) (his own wording) · index: §44 of
[testing/TestChecklist.Unity.md](testing/TestChecklist.Unity.md) · queue:
[RoadmapNext.md](RoadmapNext.md).

## 2026-08-02 — Accuracy that scales, attributes you choose, and scrolls that finally work (0.45.0)

Three connected things: the miss roll was quietly broken at every level above 20, attributes were
rebuilt around scrolls instead of drops, and both scroll types got the phone UI they never had.

### Accuracy and evasion — `DEX + level`, and a mob's DEX is flat

The old rule was `Accuracy = Evasion = DEX`, with level handled only by the cross-level gap curve.
That reads as reasonable and is a disaster in practice, because **a player's DEX never grows** (it is
rolled at creation and only gear/stat-swap passives move it) while a mob's was `10 + level`. The two
crossed at level 20 and then diverged one point — one percent — per level, in *both directions at
once*: the mob out-evaded the player AND out-accuracied him into the 5% floor. A naked level-90
fighter missed **75%** of his swings at a same-level mob, and the mob never missed him at all.

- Accuracy and evasion are now **`DEX + level`** on both sides (`StatCalculator.Accuracy/Evasion`).
- ⚠ **That alone fixes nothing** — the level terms cancel and the mob's own DEX growth still runs
  away. Measured, not assumed: the first build of this change produced the identical 75%. So a
  normal mob's DEX is now the **flat `StatCalculator.MobDexReference = 30`**, the human-fighter base.
- The result, from `tools/BalanceMatrix`: a naked human fighter vs a same-level normal mob sits at
  **5% both ways at every level from 1 to 90**. All spread now comes from gear and passives, which is
  where it was always supposed to come from — fighters buy **accuracy** (weapon masteries, the
  `Precision` hit floor), rogues buy **evasion** (`Evasion Mastery`, the light-armour masteries).
  Both passives already existed; they simply never mattered against a curve that outran them.
- ⚠ **Side effect, deliberate but worth watching:** DEX also drives crit rate and attack speed, so a
  level-90 mob went from DEX 100 to 30 and now crits less and swings slightly slower. If endgame
  mobs feel soft, the fix is a MobMod passive on the ones that should be nasty — not the DEX curve,
  which is the thing that just got fixed.
- `tools/BalanceMatrix` now prints a **HIT / MISS** table (naked and geared, both directions). This
  change was diagnosed and verified there rather than by hand, per the standing rule.

### Attributes — one per item, and only ever from a scroll

- **Nothing drops with an attribute any more.** Every dropped weapon and jewel is bare. The reason is
  economic (owner): *"you won't waste scrolls on trash when you know the next drop can be better."*
  Only the god-tier debug one-offs, which author theirs in the catalog, still arrive with any.
- **One attribute per item**, maximum. The multi-roll and the whole attribute-LOCK mechanic are gone.
- **Armor carries none at all** — armor identity is its SET bonus. `ArmorPool`/`ArmorSlotPool` deleted.
- **Item QUALITY no longer touches attributes.** It used to both gate them (Epic+ only) and scale the
  ceiling (70/85/100%). The new table is absolute per GRADE, so a Common sword can carry the same
  maximum roll as a Mythic one. Quality still buys raw stats and set identity.
- The table is keyed on the **real ladder** — `ItemLevel` 40/52/61/76/80 = D/C/B/A/S — not the
  `ItemGrade` enum, which has no C or D. Attributes start at **D grade, level 40**.
- Per-family pools, authored by the owner: magic weapons roll cast speed / M.Atk / max MP; swords
  attack speed / crit rate / max HP; blunt attack speed / crit damage / max HP; duals evasion / crit
  rate / crit damage; bows accuracy / crit rate / crit damage. Jewels are 1–5%: rings HP/MP **regen**,
  earrings max HP/MP, necklaces P.Atk/M.Atk.
- **Accuracy and the regens became PERCENT rolls.** A flat accuracy roll made no sense once the base
  stat grows with level — it would decay to nothing. The three old flat types are kept in the enum so
  pre-0.45 saves still render, but nothing rolls them.

### The scrolls, and the phone UI they never had

Six attribute scrolls, each locked to one grade band and doing exactly one thing:

| Band | Roll a type | Re-roll the value | Re-roll in the top half |
|---|---|---|---|
| **D / C / B** | Common | Uncommon | Rare |
| **A** | Epic *(new)* | — | Legendary |
| **S** | Mythic *(new)* — always at MAX | — | — |

- A "re-roll the value" scroll **cannot create** an attribute — it refuses a bare item and tells you
  which scroll to use first. A refusal never consumes the scroll.
- No lock, and no guaranteed-top-value scroll outside S.
- **Both scroll types are usable on the phone for the first time.** The commands had existed on the
  server since the enchant system was built and *nothing ever sent them* — the client had no window,
  so scrolls were dead weight in the bag. Inventory → tap a scroll → **Use** → a filtered list of
  legal targets → confirm. The list is built from `AttributeSystem`, the same code the server
  validates with, so it can never offer a target the server will refuse. Enchant's confirm box states
  the odds *and* what a failure costs (a Common scroll destroys the item).
- The item page now shows a **"Can roll"** block: what this base could carry and its range, before
  you spend anything on it.
- ⚠ **Soulcrystal/Starstone/Seraphite items (level 80+) were labelled A grade.** `TierGrade` had no S
  rung, so every S item came out as A — which the new grade-banded scrolls would have read wrong.
  Fixed.

**Protocol 11** (`RerollAttributes` dropped its `lockedIndices` argument). `MinAcceptedProtocol` stays
8: no shipped client has ever called that method, because none had the UI. 🔴 **Delete `game.db`** —
existing items still carry old multi-attribute rolls. Checklist §43.

## 2026-08-01 — Titles you can wear, chat that is sorted, and the last action (0.44.0)

Three of the four remaining 🟡 OPEN items. The fourth, combat depth, is **deferred at the owner's
request** and is not in this release.

- **Wearable titles.** The leaderboards have handed out honorary titles since 0.28.54 — "the Wealthy",
  "the Warlord" — and they existed only as a word beside a name inside the Rank window. A title is now
  something you **wear**: pick one in Rank → **Titles**, and it draws as a small gold line above your
  name in the world, for everyone near you.
  - **A title is HELD, not earned-and-kept.** You hold it while you are rank 1 of its board, and the
    server re-reads the boards every five minutes. The alternative — a persisted "titles I have ever
    won" set — says the opposite of what the board says the moment someone out-earns you, and the
    board is the entire point of the title. It also means no new writes to offline rows and no set to
    keep in sync: the only thing persisted is your **choice** (the category id), which survives losing
    and regaining the board, so a title you win back comes straight back on with nothing to re-pick.
  - The boards are read by ONE method (`GetTitleHoldersAsync` calls `GetLeaderboardAsync(cat, 1)` per
    category) rather than a second query of its own, so the rules that decide a board — admins
    excluded, zero rows excluded, the tie-breaks — cannot drift away from the board the player is
    looking at. It runs on a worker and hands the answer to the single writer as a command.
  - ⚠ **Admin characters are excluded from every board**, so an admin can never hold a title. That is
    the existing (deliberate) rule, not a new one — test on a plain character.
  - Schema: `CharacterRecord.TitleCategory`. **Delete `game.db`.**
- **Chat tabs, colours and tags on the phone** — the oldest open item in the roadmap, and the last
  thing the deleted WPF harness still did better. Chat and diagnostics shared one undifferentiated
  list, so a whisper was one grey line among a hundred warnings. The window is now **Chat**, with
  **All / Local / World / PM / System**, world in gold tagged `[W]`, whispers in violet tagged `[PM]`,
  system in green, local white. The old console is the System tab — nothing that was visible has been
  hidden.
  - The tabs are a **filter over one buffer**, not four buffers: a line is written once and each tab
    decides whether to draw it. That keeps "All" free, keeps the interleaving between channels honest,
    and keeps the monotonic `Seq` that lets the console append rows instead of rebuilding them (the
    0.28.77 lag fix). Only a tab *switch* redraws — once per tap, never per message.
  - **Reply** fills the command box with `/w <last whisperer> ` and opens the keyboard on it.
- **The action list is complete.** Every non-admin command has lived in the Skills → Actions tab since
  2026-07-24 except one: a whisper needs a MESSAGE, and no button can supply one. **Whisper** is now an
  action that does the half a button can — it puts `/w <target's name> ` in the command box and hands
  you the caret. The name is the part that is miserable to type on a phone.
- **A level floor on world chat** (`GameConstants.WorldChatMinLevel`, default **10**). World is the one
  channel that reaches every player at once, so it is the one worth making throwaway accounts for;
  local and whisper stay open so a new player can still ask for help where they are standing. Staff are
  exempt. A mutable static rather than a `const` on purpose — it is a policy dial, not a fact about the
  game; set it to 1 to open world chat to everyone.
- Fixed while in there: the Rank window's tab row had six boards at 104px in a 560px window since
  charisma was added, so the last tab hung off the edge. The window is 700 wide and the tabs are sized
  to fit the row (seven of them now).

Protocol **10** (`EntityDto.Title`, a `SetTitle` hub method, a `Titles` push). `MinAcceptedProtocol`
stays **8**: both are additions an older client neither reads nor calls, so an installed 0.42/0.43 APK
still plays — it simply sees no titles and no tabs.

## 2026-08-01 — Two things you could not see: the mob's cast, and your own target (0.43.1)

Both were on the roadmap's open list, both are pure *reader* work, and one of them turned out to be a
listener that was never written rather than a feature that was never built.

- **The mob cast bar.** The server has broadcast `MobCastInfo` to everyone near a casting mob since
  bosses shipped (2026-07-07) — caster, skill name, seconds — and the Unity client subscribed to
  neither the message nor anything like it. That is why the roadmap said *"believed built, never seen
  on screen"*: it **was** built, on the half nobody could see. The nameplate now carries an amber bar
  and the spell's name above the mob's head, filling on the client's own clock from the duration the
  server sent (there is one push, at the start — not one per tick). This is the whole point of a
  telegraph: a boss's slam is now something you get a second and a half of warning about, and can walk
  out of, interrupt, or decide to eat.
- **A killed caster no longer leaves a bar over its corpse.** `Kill` cleared `CastingSkillId` by hand
  instead of going through `CancelCast`, and cancelling is what PUSHES the clear — so an interrupted
  cast cleaned up and a *lethally* interrupted one did not. It now routes through `CancelCast`, which
  also fixes the same hole for a player killed mid-cast (their own bar).
- **A circle on each side of your target's name.** Until now the only place a target existed was the
  target *window*: on the battlefield, the mob you were about to hit looked exactly like the four
  beside it. Fine with a mouse cursor sitting on it, useless on a phone where the finger has already
  lifted. Two blue dots now flank the selected entity's name, positioned from the name's *rendered*
  width so they sit outside it however long it is and whatever else the plate is carrying (the quest
  `!`, the aggressive `*`).
  - They are **real UI elements, created when something is targeted and destroyed when nothing is**
    (owner's call, twice: first over a ground ring, then over a text glyph). There is exactly one
    target, so there is exactly one pair and nothing to pool — losing the target, walking it off
    screen, or leaving the world all destroy them.
  - The circle itself is a **sprite generated at runtime** — a 64px texture with a one-pixel feathered
    edge, built once and tinted by the Image. Not an imported .png, because this UI is authored
    entirely in code: an image asset is a file only the Editor can add and nobody can review in a diff.
    Two earlier attempts are worth recording as *not* the answer: a procedural ground ring (a mesh, a
    per-frame follow and a collider to keep out of the way of taps) and a bullet character (still a
    font glyph, and the TMP atlas here is static — `●` and every emoji draw as a hollow box, the trap
    that once put "[]" on every close button).

No protocol change (**9**, `MinAcceptedProtocol` still 8) and no `game.db` reset: nothing new goes over
the wire. It does need a **new APK** — all of it is client rendering. Checklist §41.

## 2026-08-01 — The quest window: three tabs and a page per quest (0.43.0)

**The oldest reader complaint in the file, closed.** Owner, playtest-13: *"need info on an active quest
what to kill or do — the quest window should show active/unavailable/compleated … each row in each tab
must have a [details] button to show information about the quest/description — who gave it, what u had
to do each step etc"*. Until now the log was one flat list of what you happened to be carrying: no way
to see what was coming, no way to see what you had done (completed quests were printed as raw *ids*),
and no way to read a quest at all.

- **Three tabs.** *Active* is the old list, with Track and Abandon where they were. *Completed* is
  every quest you have finished, by name. The middle tab is **Available**, not "unavailable": it lists
  every quest you have not taken — the ones you can take now first, then the shut ones each carrying
  the reason (`Requires level 20`, `Outgrown — level 15 at most`, `Requires: <the quest before it>`).
  A tab that could only ever tell you what you *cannot* do would leave *"what can I do now"* answered
  nowhere but the marks over NPC heads.
- **Hidden vs. locked** is the owner's own rule (*"not compatables can be hidden"*): another race's or
  another class's quest is not a goal you can work towards, so it never appears; a level floor or an
  unfinished prerequisite is a plan, so it does. The gating deliberately mirrors
  `QuestCatalog.OfferedBy` — if the window said "available" about something the NPC would not hand
  over, it would be lying.
- **A page per quest** — every step with a tick, an arrow on the one you are on, its own counter and
  its own "where", plus the giver and his town, the level band, the gathering lines and the reward.
  Reachable from every row of every tab, and from the NPC.
- **Accept and Decline moved onto that page.** An offer in a conversation is now a one-line row with a
  Details button, so the dialog is no longer a wall of description text; the decision is taken on the
  page that shows what the quest actually asks for. (Roadmap: *"per-quest detail window with
  accept/decline instead of one wall of text"*.)

**The protocol bump 0.42.9 promised.** That release folded the gathering counts into the step *text*
to avoid spending one; this is where it was spent (**protocol 9**), and the counts are structured
fields now — `QuestStepDto`, `QuestGatherDto`, `QuestEntry` — so the client formats them instead of
parsing a sentence. `MinAcceptedProtocol` stays at **8**: the only wire change is a field *added* to
`QuestLog`, which an older client does not read, so an installed 0.42.x APK still plays against this
server. No `game.db` reset.

## 2026-08-01 — Repeatable quests: the Huntmaster's contracts (0.42.9)

**The last unbuilt system of any size.** The owner asked for repeatable quests in playtest-13 and named
three shapes; all three are now one flag, `QuestDef.Repeatable`, plus a gathering mechanism for the
first.

- **Endless gathering** — *"can be kill mobs indefinitely (gathering quest items as u farm in a
  specific zone)"*. A contract carries `QuestGather` lines: while it is active, each named creature
  drops its own token on a credited kill. Its single step is a TalkTo back at the giver, so you hand in
  whenever you like — after one kill or after an hour — take the payment, and take the contract again.
- **Finite** — *"kill 10 of those, 50 of those… just dont close on finish"*. An ordinary kill quest
  with `Repeatable: true`. No new machinery at all.
- **Talk-to** — the same with nothing to kill. The Apothecary's daily was already one.

**The payout has no authored numbers.** Each line's `RewardModifier` **is** the owner's
`QuestItemRewardModifier`, and it multiplies the creature's *own* kill value: a token pays that
fraction of `MobExpReward`/`MobGoldReward` at the creature's natural level, again. So his worked
example — `20 × mod(skeletons) × Exp + 55 × mod(bears) × Exp` — falls out directly, the per-mob
modifier has an obvious meaning ("how much of a bonus is farming *this* worth"), and a contract stays
level-appropriate at every band with nothing to re-tune when the exp curve moves. The finite contracts'
completion bonus is written the same way, as "five kills' worth", rather than as a number that dates.
A live kill's toughness multiplier and level-gap penalty are **not** applied to a token — both make it
pay slightly less than its share, which is the safe direction, and it means a level-90 farming foxes
cashes them at a fox's rate.

**Repeatable ≠ un-completed.** A repeatable still records its bare id in `CompletedQuests`, so
"have you ever done this" and `RequiresQuestId` chains keep working; what makes it repeatable is that
the offer filter ignores that record. That filter is now one method, `QuestClosed`, asked by both the
NPC's list and the "!" markers so they cannot disagree — and it checks `Daily` **first**, which is
exactly the owner's *"can be taken again — if not daily limited"*.

**Content: a Huntmaster in every city**, standing beside the gatekeeper (taking a contract is the
errand immediately before "teleport me to the field"). Each offers one endless contract covering the
three bands his city manages, with the modifier rising by creature — 0.25 / 0.30 / 0.35, so working a
contract is worth roughly **+25-35% exp and gold** on the hour you farm it. Stonewatch and Ironreach
also carry a finite contract each, to prove that shape. Fifteen gathering tokens, one per creature,
so each can pay its own modifier.

Two supporting changes this needed:

- **Quest items stack.** A contract hands out a token per kill; one row each would fill the bag in
  twenty minutes. The class-change proofs are quest items too and are unaffected — a chain grants
  exactly one of each — but the class change now consumes *one*, by quantity, rather than removing the
  row.
- **Abandoning a gathering contract destroys its tokens.** It has to: quest items cannot be discarded
  (the rule that protects the class proofs), so tokens left behind would be undeletable dead weight.
  Safe to do bluntly because a gather token belongs to exactly one quest — `QuestCatalog.Register`
  refuses a duplicate at startup, along with two lines of one quest sharing a token.

The gather creatures feed `QuestCatalog.KillTargets` like kill steps do, so each gets its dedicated
spawner for free and a misspelt id shows up in the startup warning rather than as a contract nobody
can fill. Verified: all fifteen resolve, every Huntmaster exists, `UnservedKillTargets()` is empty.

Progress is shown by folding the token counts into the step text (`Gathered: Bear Pelt 12, …`), and the
offer shows what a contract collects and from what (`Collects: Bear Pelt (Grizzly Bear), …`) so the
accept decision is informed. That keeps `ProtocolVersion` at **8** — an installed 0.42.x APK plays this
with no rebuild. The 3-tab quest window will want it structured; it can have it then, with one protocol
bump instead of two. `game.db` does **not** need deleting.

## 2026-08-01 — The WPF harness is gone (0.42.8)

**Two clients, one of them unplayed since the first APK.** `Game.Client.Wpf` — 8,699 lines of desktop
test harness — has been deleted, and `Game.sln` is now the server and the shared library only.

It was never the real client; it was scaffolding from before there was a phone build, and it did its
job. But it was still *in the solution*, which meant every DTO change, every new protocol field and
every new window had to either be mirrored into a WPF panel nobody opened or left to break the build.
That tax was being paid on every batch, for a client with no user.

Nothing depended on it. `tools/SmokeTest` and `tools/BalanceMatrix` reference only `Game.Shared` and
`Game.Server`; `tools/publish.ps1` never touched it; and `NetworkChannel` — originally written in the
WPF project as the reusable transport seam — was forked into the Unity client long ago
(`Assets/Scripts/Net/NetworkChannel.cs`), so the copy that mattered is the one that stayed.

The Unity client had already reached parity window-for-window: skills, items, equipment, vendor,
buyback, trade, warehouse, party, quests, rank, regions, target, auto-hunt, settings and debug all
have a `GameUi.*` partial. The one thing WPF still had that the phone does not is **chat tabs with
colours and tags**, which remains an open roadmap item — the reference implementation is
`Game.Client.Wpf/MainWindow.xaml.cs` at commit `f33ed0e`, and git keeps it.

No gameplay, protocol or persistence change: `ProtocolVersion` stays at **8**, so an installed 0.42.x
APK still connects. `game.db` does **not** need deleting.

## 2026-08-01 — The account warehouse (0.42.7) — ⚠ DELETE `game.db`

**A door between your own characters.** The private warehouse shipped long ago; the account-wide half
had been open on the roadmap since. It is now built, and it is deliberately *not* just a bigger
private bank — it answers a different question, so it has different rules:

- **Tradable items only.** An item that cannot be traded is bound to the character that earned it —
  quest items, bound gear. If the account bank could move them it would be a laundering route around
  the tradable flag rather than a convenience, so a bound item is not even listed in its deposit tab.
- **10 000 gold per SLOT**, charged when a deposit has to *open* a slot. Merging into a stack already
  in there is free: the fee buys the slot, not the deposit, so the second thousand of a material
  costs nothing and a mule account stops being free storage. The private warehouse stays free.
- **Withdrawing is free.** Charging to get your own things back is a trap, not a cost.

Fifty slots, town-only (the same safe-zone gate as the private one), and a warehoused rune still
expires there — the bank is space, not a time-pause.

**The shared list is the interesting part.** Two characters of one account *can* be in the world at
the same time, because offline farming leaves a character standing there after its player logs in on
another. So the bank is one live list in `World.AccountWarehouses`, keyed by account id, and the copy
read from the database during login is adopted **only if that account has no live list yet** — a list
already in memory is newer than anything on disk. Every deposit or withdrawal pushes the new contents
to *every* character of that account who is in the world, so the second one is never looking at
contents from before the first one moved something.

It also gets its own table (`AccountItemRecord`) rather than a flag on the character's items.
Hanging shared goods off whichever character happened to deposit them would mean deleting that
character took the account's bank with it.

The phone's warehouse window grew a second row of buttons — **Private / Account** — instead of a
second window, because the question at the keeper is "where do I put this", which is one task with a
choice in it, not two errands. ⚠ **New table: delete `Game.Server/game.db` (+ `-shm`/`-wal`).**

## 2026-08-01 — Twenty of your fifty potions (0.42.6)

**A trade offer carries counts now.** Until this version an item went on the trade table whole: you
could hand over a stack of fifty healing potions or none of them, and the only way to give a friend
twenty was to sell forty to a vendor first and buy them back afterwards. `TradeOffer` now sends
`TradeOfferEntry(InstanceId, Quantity)` instead of a bare instance id, and the stack **splits at
completion** — the sender keeps the remainder, the receiver gets a fresh instance that merges into
whatever stack they already had.

Three details are the whole of the correctness here:

- **The count is clamped on the server**, not trusted from the client: to `1..stack` for a stackable
  and to exactly `1` for anything else. A client asking to trade 500 of its 50 potions is not an
  exploit, it is a rounding error.
- **The offer is re-resolved at completion, and a shortfall fails the WHOLE trade** rather than
  delivering what is left. If you promised twenty and drank six while the window was open, the other
  side agreed to twenty; handing them fourteen is a different bargain they never accepted.
- **The bag-space check learned what a partial stack does.** Giving away part of a stack frees no
  slot (the remainder stays), and an incoming stackable costs no slot if a stack of it survives on
  the receiving side. The old check counted rows and would have refused legal trades and allowed
  illegal ones once splits existed.

Both clients ask how many: the phone reuses the vendor **numpad** ("Offer", showing what you keep),
and the WPF harness gets a small modal quantity box, since it had no number entry of its own. A
partly-offered stack **stays in your bag list showing the remainder** — the alternative, a row that
disappears the moment you offer one of it, reads as though the other forty-nine went with it.

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

The last tier-1 item from [testing/Playtest-Archive.md#playtest-13](testing/Playtest-Archive.md#playtest-13). Buffs died on every logout
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

The first batch off [testing/Playtest-Archive.md#playtest-13](testing/Playtest-Archive.md#playtest-13). Seven of the eight tier-1 items;
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

