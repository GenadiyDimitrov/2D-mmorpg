# Backlog — everything asked for and not yet built

**One list. Features and changes only.** Bugs, verifications and "does this work" live in
[testing/Open-Checklist.md](testing/Open-Checklist.md) during a pass and in
[testing/Playtest-Archive.md](testing/Playtest-Archive.md) after it. This file is the other half:
the things you asked to be **built** or **changed** that are still owed.

Assembled 2026-08-12 from playtests 4-21, `Open-Checklist.md`, `Playtest-Archive.md`,
`Roadmap.md` / `RoadmapNext.md` and the design docs. Everything shipped up to `ed75bac`
(0.60.1 + the playtest-21 batch) has been checked out of it.

**Playtest 22 (2026-08-13) added `BL-65` … `BL-72`** — dungeon level bands, an item-id reference,
the `MpHeal` type, more 16-40 zones, invisibility ×3, mob social clans, the aggro/taunt model, and
unbuffed farm survivability. His bug finds from the same pass went to `testing/Open-Checklist.md`,
not here.

## The rules this file runs on

1. **Newest ruling wins, and it is the ONLY one shown.** When you re-spec something, its entry is
   rewritten in place. The old text is cut and pasted into
   [BacklogArchive.md](BacklogArchive.md) under the same id, dated. Never two live versions.
2. **An id is permanent.** `BL-07` means the same thing forever, even after a rewrite, so a note
   anywhere in the repo that cites it stays true. Ids are never reused and never renumbered.
3. **Built = deleted from here.** It goes to `CHANGELOG.md`, not into a done-pile in this file.
4. These ids do **not** collide with your checklist ids (`63l`, `C4`, `M9`, `G3`) — those are a
   playtest's numbering and die with the pass. Where an entry came from one, it says so.

**Status marks:** 🔴 ready to build · 🟡 gated on another entry here · 🔵 waiting on you (a
decision, a CSV, a measurement) · ⏸ you put it on hold.

---

## ★ The ones you have named most recently

Three of the original five are **built and deleted** (2026-08-12): `BL-01` the premium reward runes,
`BL-03` the Stat-Swap tab and `BL-04` the auto buff potion/scroll tab — the last two took `BL-39`
(the Mindwriter's misleading `(cost …)`) out with them. See `CHANGELOG.md`. Two are left.

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier).** Blocked on your skill CSVs —
  `docs/data/classes_skills_csv/` holds nothing above level 35. Still the single biggest content
  unlock in the project; nothing is invented in the meantime, by your own rule.

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

---

## Combat & stats

- `BL-06` 🔴 **Skill evasion as its own channel.** *"normaly no1 can evade a physical skill … now on
  then i miss a skill which is anoying — stab fails."* So: **a physical skill never misses by
  default**; evading one is an explicit, granted chance. The rogue ultimate is the only source —
  **25% → 40%**, and *"76lvl the physical phantom gets a 90% for 15s"*. *(playtest-21 `69e`, and
  `62e` before it.)*

- `BL-07` 🔴 **Physical skill reflect.** *"a warrior can get a 30% chance to reflect a physical
  [skill] … so we can have a 100% chance to reflect 15% p skill dmg, or 15% chance to reflect 100%
  p skill dmg."* Your default: warrior **@40 → 0.15 chance ×1 reflected**, **@76 → 0.30**.
  *(playtest-21, "My Finds", left as a bare `[]`.)*

- `BL-08` 🔴 **Debuff reflect.** *"tanks get 30% chance to reflect a debuff -> u cast on tank he
  reflects u get the debuff."* *(playtest-21, same block.)*

- `BL-09` 🔴 **A floor under the wrong-weapon magic penalty, bought back by Spellcaster Mastery.**
  *"hitting above the 0 difference is not failing … if we can make a floor … a strong 50% with wrong
  weapon celing((formula),0.5) that is always 50% on the norm … L1 - 0.5 .. L5 ..0.05(the min)."*
  Read as: a wrong-weapon caster is capped at 50% success at parity, and the five Mastery rungs walk
  the penalty 0.5 → 0.05. *(playtest-21 `64c`, never answered.)*

- `BL-10` 🔵 **A floor under the fading bow-caster penalty.** The bow penalty currently vanishes
  entirely when you punch down. You were asked whether you want a floor under it and the reply is
  still empty. *(playtest-21 `64e`.)*

- `BL-11` 🔴 **Anti-magic / anti-physical mobs feed the mRes ladder.** *"We had a anti magic mobs
  (lower pdef more mdef) and anty physical (less m def more pdef) — this should feed your mres
  passive."* No mob feeds it today. *(playtest-21 `64i`.)*

- `BL-12` 🔵 **Enchant bonus should scale with what you put in.** Your objection to the flat-offset
  ruling, unanswered: *"not a warrior invest +3 and gets the same bonus as cleric +16."* Today the
  offset is identical for every class, by grade. Needs your call before anything moves.
  *(playtest-21 `68e`; the 0.60.0 model is in `docs/balance/BalanceMatrix.md` §E.)*

- `BL-13` 🔴 **Boss HP — check the ×2 passive is actually landing.** *"boss had 260? He should have
  520? Check."* Your targets: a **field boss ~6 minutes** for a 3-DD party (it dies in ~180s now),
  and *"a world [boss] should take an hour for ~10 parties (~50 DDs)"*. *(playtest-21 `62m`.)*

- `BL-14` 🔴 **Mob attack speed comes off the weapon, not a pinned 433.** *"didn't we gave monsters
  weapon types? Archer is slower but does more dmg, the fast attacking have more crit rate and more
  atck speed but less dmg."* Drive it off `InnateWeaponType`, which already exists.
  *(playtest-21 `62i`.)*

- `BL-15` 🔵 **`precision` / `anti_magic` floor rungs should follow the CLASS CHANGE, not level 76.**
  Implied by your rogue ruling and never carried back into either checklist — recorded in the
  changelog as "owed back to him" and then dropped. Confirm and it is a small authoring change.

- `BL-16` 🔴 **Heal powers need re-authoring.** They sit at ~151-301 against a scale that has moved
  to ~1000. Flagged as "a future tuning pass" and never scheduled.

- `BL-17` 🔴 **Re-author `BuffMagAtk`, and give magic-only buffs an explicit magic %.** Open TODO;
  until it lands those buffs over-perform.

- `BL-18` 🔵 **The nuker-vs-champion measurement (`0a`).** The nuker beats the champion by 19% in
  the matrix. You deferred the ruling to play: *"This need to be tested. When I leave the chars to
  play alone all measure."* ⚠ That makes auto-farm load-bearing for a balance decision — and
  auto-farm has never been through a long unattended run.

- `BL-19` ⏸ **Combat depth — held by you (2026-08-01).** Perfect/excellent block · position bonuses
  (hook reserved) · PvP and PvE damage multipliers (both hooks exist and are 1.0). *"the combat
  depth I don't want it build for now defer it."* Not dropped — do not build unasked.

- `BL-71` 🔵 **The aggro / taunt model.** *"So the question is what we have and how taunt works now
  and what it needs to be implemented"* (playtest-22). **Answered first, because it changes the
  size of the job:**
  - **A real per-attacker threat table already exists** — `Entity.Threat` (`Guid → float`), read by
    `RetargetByThreat`, which picks the maximum on every damage tick. It is not "last who hit me".
  - **Aggro IS damage today**, exactly as you want it: `AddThreat(target, attacker, damage)`, 1 point
    per 1 damage. A non-damaging offensive skill adds a flat **1**.
  - **Taunt exists and works**: `provoke` (Fighter/Tank, level 20) sets your threat to `top × 1.2 +
    100` and locks the mob onto you for ~3s. **It has no `Power`** — which is precisely the gap you
    described. There is also a rogue-side **detaunt** (Warding Step) that sheds 90%.
  - **Missing entirely**: taunt POWER as an authored number (so a lure and a 4th-class taunt differ),
    a taunt that scales per level, **threat decay**, **healer/buffer threat** (a healer is invisible
    to every mob in the game today), and any client-visible aggro list.
  - ⚠ One real defect found while answering: a **proximity pull adds no threat at all**, so the first
    point of damage from anyone instantly owns a mob that walked to you.
  **What you asked for on top:** `lure` ~500 power (range 200/400/600 by level), a tank taunt at
  1000-2000 at L1 rising to **20-30k** so a 7-8k physical skill cannot steal it, and healer threat
  ≈ `healPower / castSeconds × 10` (your worked example: 300 power / 2s = 1500; 500 power / 5s =
  1000). Ships with `BL-70`, which is what makes a lure worth having.

---

## Items & economy

- `BL-20` 🔴 **A partial Blessing Box pick returns a box for the rest.** *"I'll want to be able to
  pick 5 and I get my 5 scrolls + the box for the other 5."* You filed it as later.
  *(playtest-21 `63d`.)*

- `BL-21` 🟡 **Per-mob and per-zone drop identity.** *"I would like obe mob to drop let say a sword
  and a 2h sword, the other to drop only main armors, third boots and helmet … to go to a spot and
  know I can get there light armor and 2h-sword."* Then: *"later I'll want a 'ork settlment' where
  are 5 different ork types and I go there for lvl up, and several different settlements and zones
  with meanings."* You gated this yourself behind the world-map/positions pass (`BL-45`).

- `BL-22` 🔴 **Trash disassembles into crafting mats** instead of into gold — *"rarity for mats
  rarity, grade for mats ammount"*. Filed into `Crafting.md` and never built. Pairs with `BL-05`.

- `BL-23` 🔵 **The coin curve.** Gear value follows the tier ladder while coin stays linear, so the
  gap drifts to **51×** by level 76. The note in the archive is explicit that *"the real fix is the
  coin curve, not another multiplier"* — every rate tweak since has been a patch over this.

- `BL-24` 🔵 **The enchant-scroll types — you wanted to discuss them.** *"ENCHANTS — you said you
  want to DISCUSS them … bring it up when you are ready."* The three types (breaks / −1 / safe) ×
  six grades shipped in 0.53-0.60; the conversation you asked for never happened. The 30× drop cut
  (`62j`) is ratified and stays.

- `BL-25` 🔴 **The drop-group simplification.** *"In a way I want to simplify it"* — the inner roll
  should pick the drop **directly** rather than picking a rarity first, with per-item control (your
  example: a rarer Scroll of Resurrect inside its own group).

- `BL-26` 🔴 **The vendor half of the buy-back design** — a longer sold list. Flagged "still open,
  still not urgent" and never revisited.

- `BL-27` 🔴 **`Robe 611` has no item behind it.** You re-edited the row on 2026-08-11 without
  asking for the item, so it was left alone a second time. Say whether it should exist.

- `BL-28` ⏸ **MP potions** — held until the 40+ kits decide the MP economy.

- `BL-29` ⏸ **SP bottles** — 1e9 SP → one bottle; also what keeps `SkillPoints` an `int` honest.

- `BL-30` ⏸ **Recipe drops below A grade** — no recipe item exists under A (below 76 they are
  learned by level). Add the same way A+ was added, when there is a reason to.

---

## Classes & skills

- `BL-34` 🔴 **"Madness" — a party buff at the top of the Frenzy family**, plus the 76+ buff
  expansion: the healer gets all the singles including a single Frenzy, and 76+ wants *"2-3 more
  Harmonies and 1-2 more improved buffs"*.

- `BL-35` 🔴 **Preservation / auto-res as a buff family.** The `AutoResurrect` flag is already in
  place for it and nothing uses it yet.

- `BL-36` 🔴 **Subclass swapping restricted to a safe zone, with a 5-minute delay.** The machinery
  swaps fine; the player-facing rules were never built.

- `BL-69` 🔵 **Invisibility, in three separate kinds.** His full spec (playtest-22). They share a
  word and nothing else, so they are three builds:
  1. **Full `hide`** (melee rogue) — a buff nobody renders, targets or checks as nearby; mobs lose
     aggro, aggressive mobs do not start a chase. **Anything but movement breaks it**: hitting, a
     skill, a potion. 🔑 His timing rule for a gap-closer: *"i want to click the skill and im not in
     range to start to move towards the target but still invisible once the skill is executed then i
     appear"* — the reveal is at execution, not at the click. An **archer AoE non-damage debuff**
     strips `hide` in a 200-400 radius and applies a **30s no-hide** debuff; any AoE damage also
     reveals.
  2. **`stealth` vs aggressive mobs only** — a rogue TOGGLE (1 MP/s) plus a buffer's party version
     (1 min, 30s cd, 300 MP). Already-aggroed mobs keep chasing and hitting; un-aggroed ones never
     start. Does **not** drop player targets, does not hide you from players, does not break until
     you stop it. The point: *"toggle-on makes the rogues farm in peacefull zones."*
  3. **Admin `/invis`** — absolute. No reveal, no AoE, no skill use breaks it; it toggles off only by
     typing the command again. He is still hittable unless in god mode.
  ⚠ Some machinery exists — the mob aggro scan already skips stealthed players, and `DropAggroOn`
  already wipes one entity's aggro from every mob. Neither of the three buffs does.

- `BL-38` 🔵 **Pets and summons** — immovable totems, class pets, the mage summoner. Designed, never
  scheduled, never re-raised by you.

---

## UI & client

- `BL-41` 🔵 **A grade filter on the craft Gear page.** 62-63 rows is a long scroll on the phone.
  The question was put to you and never answered.

- `BL-42` 🔴 **Passives and skills should describe themselves with real NUMBERS, per level** —
  including the conditional lines (*"light-armour-only"* bonuses and the like). `63e` fixed the
  flags, not the numeric prose. *"all skills and passive should show the desctiption with numbers."*

- `BL-43` 🔴 **`NextTarget` / target cycling.** Deferred once and never raised again.

- `BL-44` 🟡 **"Everything is a skill" — the last two pieces.** Armor sets and weapon specials are
  still `StatMods`, not skills, so **buff-bar row 3 (item effects) is permanently empty**; and the
  set tooltip's **shield row** has nothing to show until shields belong to sets. You called this
  optional at the time.

- `BL-45` 🔴 **The presentation pass.** Your words, still true: *"no sounds, a bit woody, no good
  visuals."* The loudest remaining gap and not a scheduled item.

- `BL-46` 🔴 **The second launcher icon must be gone before a store release.** Kept on purpose as
  the duo-testing rig. Half the cause is ours (`AndroidManifest.xml` declares both activities with
  a LAUNCHER filter); the other half is likely a Samsung profile clone, which no manifest edit
  removes. Verify on the device at store time.

---

## World & mobs

- `BL-47` 🔵 **`G3` — mobs built like players.** Mobs stop carrying inflated STR/CON and move onto
  `RecomputeDerived` + real equipment, with passive "type" layers (armor weight, weapon type, jewel,
  hp, speed). Your three ordered steps: *"I want it documented and balance matrix tables … and later
  we can do 2~5 mobs so I can test."* **None of the three is done** — step 1 is a doc and a matrix,
  and it needs no code.

- `BL-48` ⏸ **Instances — you are holding.** Design is written (`design/Instances.md`). One
  load-bearing decision is still open: the daily attempt **GLOBAL vs PER-INSTANCE**. It changes the
  persisted model, so it is answered before anything is built. **Dungeons are the cheap half** —
  a dungeon is just a `SpawnZone` outside the town ring plus a teleport entrance, near-zero risk,
  and they can ship without instances.

- `BL-49` 🔴 **Levelling pace and boss EXP, by eye.** Three items never revisited: the 60-85 band
  runs ~3× faster than the rest, the elite/boss EXP multiplier wants a look, and the fighter
  kill-speed sanity check was never run.

- `BL-50` 🔴 **A boss/elite crafting-mat pile must obey the party loot rule.** Written as *(not
  tested)* and never tested.

- `BL-51` 🔵 **Castles + vault.** Needs the siege design first; consumes the reserved
  `VendorBuyTaxRate` hook.

- `BL-65` 🔴 **Dungeons get level bands, and there are three of them.** *"separate the dungeons ...
  Now a 32 lvl mobs almost next to a 65 lvl which protect the 44 lvl boss ... The mob lvls are all
  over the place ... put them in the lvl ranges and make 2 more dungeons"* (playtest-22). His layout:
  **Hollow Creep stays ~40 with its 44 boss**, plus a new **~60 (65 boss)** and a new **~85 (90
  boss)**. The band, not the boss, is the fix — a dungeon whose mobs span 32-65 has no level at which
  it is playable.

- `BL-68` 🔵 **More zones in the 16-40 band, by widening the map west-to-east.** *"Add several new
  zones to duplicate the 16-20, 20-24, 24-28, 28-32, 32-36, 36-40 (all the `Stonewatch` zones)"*
  (playtest-22). His method, which is a map change rather than a content one: *"The whole City can
  move to the right so the bot side fields can be extended ... For example `Greyhollow Moor` -
  increased ~4 times in width (to the right) and `north` and `south` zones to have 4 of each."* So
  each existing band becomes four parallel fields at the same levels — the point is somewhere else to
  farm at your level, not a longer ladder. Interacts with `BL-52`.

- `BL-70` 🔵 **Mob clans — a social circle that answers a cry for help.** Written as the COUNTER to
  `BL-69`: *"when we include the rogues hide, a rogue can have an easy farm in an elite zone and kill
  a single mob without getting disturbed"* (playtest-22). Mobs join a named group (`Ork`) with a
  **400-500 radius**; hitting one turns the rest of its group on you.
  🔑 **The trigger is DAMAGE and nothing else** — *"social circle only works if a mob is hit, not when
  taunted/debuffed/aggroed/etc.. only if the mob start to take dmg he 'cries' for help."* That rule is
  what makes a **`lure`** (a rogue's mob-only taunt, no player targets) the intended way to pull one
  out: his IG picture is a rogue walking between aggressive mobs, luring the one the party wants and
  running it back to safety. Needs `BL-71` for the taunt power a lure carries.

- `BL-52` 🔵 **World expansion toward 1kk+.** The 0.33.0 re-layout was the first step and nothing
  followed it. `BL-21` is queued behind this one.

---

## Quests

- `BL-54` 🔵 **Newbie items through quests** — hand the starter weapon/armor/jewel boxes out at
  levels 6/8/10. Your plan, never scheduled. ⚠ Re-check it against the tutorial as it now ships
  (`267313d` moved every box onto the step that needs it) before building.

- `BL-55` 🔵 **Two real starter armor SETS.** The current newbie light/robe sets are placeholders
  waiting on your numbers.

---

## Admin & debug tools

- `BL-56` 🔵 **The admin item picker as a selection box.** Either *rarity box → item* or *type box →
  rarity*: *"Pick wichever is easier to implemment."* Never actioned.

- `BL-66` ✅ **BUILT 2026-08-13** — the item-id reference and the staff-only id row. Kept here for one
  release only because it is the thing that unblocked his own §75/§76 testing; delete at the next
  sweep. *"Need a grouped list (in a file - like the commands one) with each equip/item ID, and in
  each items details in game only for admin to see: a row like the enchant info one with the ID."*
  → `docs/guides/ItemIds.md` (1,078 ids, **generated** by `tools/ItemIds`, never hand-written) and an
  `id <defId>` line under the enchant line on every item card, staff only.

- `BL-72` 🔵 **Unbuffed auto-farm is not survivable for either damage kit.** His `0a` note
  (playtest-22): *"they both have hard time to farm without buffs .. when i login in 1-2h after the
  npcs buffs are gone both are dead and with potion buffs."* Two separate questions inside it, and
  the second is the real one:
  1. Is an unbuffed nuker/champion *meant* to survive an unattended hour? The NPC buff ladder is
     currently load-bearing for auto-farm, which nothing was designed to be.
  2. **It also invalidates the `0a` measurement itself** (`BL-18`) — a run that ends in a death an
     hour in is not measuring the kits, and the auto-buff tab (§78) is what would keep one alive long
     enough to measure. Read the two together before spending a session on either.

---

## Housekeeping

- `BL-59` 🔴 **Resurrect / party / PvP-flag rules (your find #9).** Three parts, none built:
  Ultimate Resurrection scrolls should be tradable (*"atleast the one that drop and from the admin
  menu"*); you cannot res a party member while **you** are flagged, but may res or heal a PK while
  unflagged; inviting and trading with PvP-flagged players must work, with PK still trade-blocked.

- `BL-60` 🔵 **Death penalty, resurrection skills, Angel's Protection.** The 2026-07-17 design —
  death XP penalty, res skills and scrolls, a buff-keep-on-death. Nothing exists in code. Overlaps
  `BL-59`; read them together.

- `BL-61` ⏸ **Network payload optimisation.** Split/delta snapshots and a local buff countdown, then
  optionally MessagePack. Deferred deliberately: no measured problem, the protocol still churns
  every session, and MessagePack's dynamic resolver does not work under Unity/IL2CPP without a
  codegen step. A late, one-line swap once the protocol settles.

- `BL-62` ⏸ **Bot-prevention CAPTCHA** ("petrification" after 200-500 manual kills). Revisit with
  behavioural detection. Your own worry stands: an AI, as opposed to an if/else bot, solves it.

---

## What was closed on 2026-08-12 and is deliberately NOT in this file

The playtest-21 batch and `58d` shipped in `267313d` → `ed75bac`: shields option 3 (P.Def ÷5,
Shield Mastery ×5) · the shield enchant `+9 → +3` · the wood/iron shield block profile · the whole
start quest re-spec · training club and knives deleted · the `x500` mats stall · auto-farm ignoring
`RequiredWeapon` · the training dummies + rank titles · `65d` · `67i` · `68h` · `63i` · `62j` ·
broken jewels → 9/5/3 · **item tags and the full `/give`**. They live in `CHANGELOG.md`.

**The housekeeping batch, later the same day** took out `BL-37` (the test heal, deleted — and the
retired-skill-id leak it exposed in the save loader) and `BL-58` (`58i`, the inspiration-game name
purge; the tag is `IG`).

**Six more were checked out against the CODE, not the list** — every one was already built in a pass
whose commit carried no changelog entry, which is why they were still sitting here: `BL-31` (`55b`,
the HP price on a skill card), `BL-32` (`55c`, refusing an HP skill at low HP), `BL-33` (`57b`, the
duplicated Robe Armor Mastery), `BL-53` (Elder Marius's empty "!"), `BL-63` (Frost Bind stripping a
mob's HP multiplier) and `BL-64` (the target dropped for a physical cast). The table in
`CHANGELOG.md` names the code that proves each one. ⚠ `BL-63` and `BL-64` were closed on a **reading
of the code**, never re-tested by him — they are on the checklist as verifications, not called done.
