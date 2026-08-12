# Backlog — everything asked for and not yet built

**One list. Features and changes only.** Bugs, verifications and "does this work" live in
[testing/Open-Checklist.md](testing/Open-Checklist.md) during a pass and in
[testing/Playtest-Archive.md](testing/Playtest-Archive.md) after it. This file is the other half:
the things you asked to be **built** or **changed** that are still owed.

Assembled 2026-08-12 from playtests 4-21, `Open-Checklist.md`, `Playtest-Archive.md`,
`Roadmap.md` / `RoadmapNext.md` and the design docs. Everything shipped up to `ed75bac`
(0.60.1 + the playtest-21 batch) has been checked out of it.

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

## ★ The five you have named most recently

- `BL-01` 🔴 **Premium reward runes.** Exp · SP · Exp+SP · Gold (amount) · Drop (chance), at **+5%
  then 0.1 steps to +100%**. Plus **Rune of Sinister** (exp/sp gain stopped, gold and drop
  untouched — the grinder's rune) and **Rune of Sinners** (*"a timed rune given by the Gods to
  punish those who sinned"* — all four zeroed, cannot be sold, traded, discarded or banked).
  Rides the existing rune machinery: one item def + one buff skill each, so the buff bar shows it
  for free. ⚠ `SkillEffect` has **3 bits left** — spend ONE (`BuffRewardRate`) and carry the four
  magnitudes as `SkillDef` fields. ⚠ the drop multiplier must be passed **into**
  `MobCatalog.EffectiveRate`, never applied at a call site, or the inspect screen lies to a player
  wearing a Drop rune. *(2026-08-12; `/give` and item tags — its blocker — shipped in `ed75bac`.)*

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier).** Blocked on your skill CSVs —
  `docs/data/classes_skills_csv/` holds nothing above level 35. Still the single biggest content
  unlock in the project; nothing is invented in the meantime, by your own rule.

- `BL-03` 🔴 **A Stat-Swap tab.** *"its a bit chaotic .. need a new place -- may be a new tab where u
  see what stats u selected and before u confirm a selection to show what u are changing."* Your
  layout: `Next Price` · a committed-count row · `[+]`/`[-]` per pair · the increase/decrease pair
  per column · a running `Added: WIT +5 | ATK +3 | SPT −8` line. `[+]` greys at the +5 cap and after
  a paid rung, so you can only step back down and re-spend. Also kills `63m`
  (*"now with list of skill is visually hard to understand"*). *(playtest-21 `63l`/`63m`.)*

- `BL-04` 🔴 **The auto buff potion/scroll tab.** One row per buff family:
  `Bulwark [potion ☒][scroll ☐][max rarity: rare]`. When the buff drops off, priority is **rarity
  first, then scroll > potion** — uncommon scroll → uncommon potion → common scroll → common potion.
  **Absorbs `C4`** (auto-on for buff potions/scrolls), which you deferred *into* this tab.
  *(playtest-21, "My Finds".)*

- `BL-05` 🔴 **Crafting moves to NPC masters, and a profession is quittable.** *"better at NPC — and
  craft happens with their respected masters … u compleate the quest and u can take his
  proffesion."* Six crafting levels L1-L6 at exp marks **0/5/15/30/50/100**; 10 crafts of your own
  level per level, **×3** a grade below, **×0.8** a grade above, −2 grades pays nothing. Each rung
  unlocks the next rarity's mats and goods. Character-level gates **L1-2 → 20 · L3-4 → 40 · L5-6 →
  76**, and crafting exp **freezes at 100%** until you take the next class. Quitting at your master
  wipes your levels and you start over elsewhere: *"i know i told you that is final, but now i
  changed my mind."* ⚠ Ships with `BL-40` (output is absurd today). *(playtest-21 `66a`.)*

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

- `BL-31` 🔴 **A skill card must print the HP price, not only the MP gain.** *"it's not showing in
  the description what it takes to gain what .. −200hp +120mp .. is never written."*

- `BL-32` 🔴 **An HP-cost skill must be refused at low HP, the way MP is.** *"I cannot cast a skill
  when my mp is low .. so I cannot cast skill when hp is low."*

- `BL-33` 🔴 **Robe Armor Mastery is offered in two learn groups** — L1 appears in both the level-1
  and the level-7 group, and learning one makes the other vanish.

- `BL-34` 🔴 **"Madness" — a party buff at the top of the Frenzy family**, plus the 76+ buff
  expansion: the healer gets all the singles including a single Frenzy, and 76+ wants *"2-3 more
  Harmonies and 1-2 more improved buffs"*.

- `BL-35` 🔴 **Preservation / auto-res as a buff family.** The `AutoResurrect` flag is already in
  place for it and nothing uses it yet.

- `BL-36` 🔴 **Subclass swapping restricted to a safe zone, with a 5-minute delay.** The machinery
  swaps fine; the player-facing rules were never built.

- `BL-37` 🔴 **Delete the `TestHeal` power-1000 debug skill.** Marked removable, flagged for
  cleanup, never removed.

- `BL-38` 🔵 **Pets and summons** — immovable totems, class pets, the mage summoner. Designed, never
  scheduled, never re-raised by you.

---

## UI & client

- `BL-39` 🔴 **The Mindwriter must stop printing `(cost 25,000,000)` when forgetting is free.**
  *"i think it will cost me 25kk to remove them even though upper say its free -> make it `(losing
  25,000,000)` or something or remove the () as whole."* *(playtest-21 `63l`/`63p`; ships with
  `BL-03`.)*

- `BL-40` 🔴 **Crafting output is absurd, and it is a UI-adjacent economy bug.** A level-30 Potion
  Crafter had made **450 uncommon potions** (~100k saved, ~34k resale) out of ~15 uncommon wood; a
  Scroll Crafter **690 uncommon attribute scrolls**. The refine→craft chain multiplies far too
  generously. *(playtest-21 `66`; build with `BL-05`.)*

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

- `BL-52` 🔵 **World expansion toward 1kk+.** The 0.33.0 re-layout was the first step and nothing
  followed it. `BL-21` is queued behind this one.

---

## Quests

- `BL-53` 🔴 **Elder Marius shows a "!" and has no quest to give.** Reported in playtest-20, appears
  nowhere else in the repo, never fixed.

- `BL-54` 🔵 **Newbie items through quests** — hand the starter weapon/armor/jewel boxes out at
  levels 6/8/10. Your plan, never scheduled. ⚠ Re-check it against the tutorial as it now ships
  (`267313d` moved every box onto the step that needs it) before building.

- `BL-55` 🔵 **Two real starter armor SETS.** The current newbie light/robe sets are placeholders
  waiting on your numbers.

---

## Admin & debug tools

- `BL-56` 🔵 **The admin item picker as a selection box.** Either *rarity box → item* or *type box →
  rarity*: *"Pick wichever is easier to implemment."* Never actioned.

- `BL-57` 🔴 **A cheap level-1 recipe for the Potion Master and the Scroll Scribe.** Offered to you;
  your reply was *"and my luck i picked exactly those :)"* and nothing was built.

---

## Housekeeping

- `BL-58` 🔴 **`58i` — purge the inspiration game's name from the codebase.** Mechanical, no risk,
  written down twice as "I will fold it into a quiet build" and never folded in.

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

## Two bugs that fell off with no fix and no reply

These belong in the checklist, not here — but they were reported in playtest-20, answered nowhere,
and carried into no pass since. Parked here so they stop vanishing.

- `BL-63` 🔴 **Frost Bind strips a dummy's / an elite's HP multiplier.** *"makes training dummies go
  from 1kk hp to 5k and same for elites .. they lose their hp bonus. Dont know if its only for this
  debuff or no. But need investigation."*

- `BL-64` 🔴 **Your target is lost for the duration of a physical skill cast.** *"When casting skill
  (stab) my target is lost for the duration of the cast .. then back again (only physical 'stab',
  haven't tested with others yet)."*

---

## What was closed on 2026-08-12 and is deliberately NOT in this file

The playtest-21 batch and `58d` shipped in `267313d` → `ed75bac`: shields option 3 (P.Def ÷5,
Shield Mastery ×5) · the shield enchant `+9 → +3` · the wood/iron shield block profile · the whole
start quest re-spec · training club and knives deleted · the `x500` mats stall · auto-farm ignoring
`RequiredWeapon` · the training dummies + rank titles · `65d` · `67i` · `68h` · `63i` · `62j` ·
broken jewels → 9/5/3 · **item tags and the full `/give`**. They live in `CHANGELOG.md`.
