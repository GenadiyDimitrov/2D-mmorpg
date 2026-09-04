# Backlog — what is still owed

**One list. Features and changes only, and ONLY the ones that are still open.** Bugs, verifications
and "does this work" live in [testing/Open-Checklist.md](testing/Open-Checklist.md) during a pass and
in [testing/Playtest-Archive.md](testing/Playtest-Archive.md) after it.

**Reduced to open entries only on 2026-09-03**, on your instruction: *"backlog contains only
unfinished, undecided entries … all fixed/build to go to the archive … and are very unordered … when
u say bl-153 I scroll or search and it's somewhere between bl-20 and bl-58 … order them and leave
only active"*. Ninety-one closed entries — built, declined, and the old texts their rewrites replaced
— moved verbatim to [BacklogArchive.md](BacklogArchive.md), in id order. Nothing was deleted.

## The rules this file runs on

1. **Open only, and sorted by id.** `BL-02` first, `BL-106` last, no categories to hunt through — the
   **Area** column of the index is how you browse by subject. The moment an entry is built, declined
   or answered with nothing owed, it is **cut to [BacklogArchive.md](BacklogArchive.md)**, dated,
   under the same id. This file should never again grow a done-pile.
2. **Newest ruling wins, and it is the ONLY one shown.** When you re-spec something, its entry is
   rewritten in place and the old text goes to the archive. Never two live versions.
3. **An id is permanent.** `BL-07` means the same thing forever, even after a rewrite, so a note
   anywhere in the repo that cites it stays true. Ids are never reused and never renumbered.
4. These ids do **not** collide with your checklist ids (`63l`, `C4`, `M9`, `G3`) — those are a
   playtest's numbering and die with the pass. Where an entry came from one, it says so.

**Status marks:** 🔴 ready to build · 🟡 gated on another entry here · 🔵 waiting on you (a
decision, a CSV, a measurement) · ⏸ you put it on hold · ❓ a question of mine, unanswered.

⏸ **CRAFTING IS STILL PARKED, on your instruction (2026-08-14):** *"leave the salvage/mats etc craft
until I'm able to test it fully — need to increase the drop rate and exp by 100 so I can make chars
different professions to farm to see who can craft what — and it's a single playtest only for this."*
So **`BL-05`** and **`BL-50`** are not to be worked on or re-raised until you open that playtest.
Nothing about them is blocked or broken; they wait on a test only you can run.

★ **The ones you named most recently:** ✅ **`BL-158`…`BL-162` — the NPC BUFFER pass is BUILT (0.111.0,
2026-09-04) and is in the archive.** The shelf levels up with you, the 75 ceiling is gone, eight single
harmonies and the three Marks are on sale, and Swift joined the Mage preset. See it with
`dotnet run --project tools/BalanceMatrix -- --npcshelf`. **⚠ NO new APK — the wire did not move
(protocol stays 33).** · 🔴 **NEXT: the TANK pass** — you finished `tank 2nd`/`3rd`/`4th` on 2026-09-04
(*"im done with tank 2/3/4 so its ready to build after the npc buffer"*), which unblocks the rest of
`BL-154` (pull) and `BL-155` (silence) and closes the last `NOT DONE` file in `BL-02` · **`BL-163`** (your
shape for the buffer shelf: an external `(shelfId, minLvl, rungId, price)` file so *"a pvp server won't
require new npc just change of id's"* — a refactor for editability, nothing is broken) · **`BL-164`**
(the Marks' rank tie, found while building `BL-161` — your call between three fixes) · `BL-156` (debuff
duration — **BUILT and CLOSED**, in the archive) · `BL-157` (the worm, a seed) ·
`BL-93` (the visuals conversation, yours to start) · `BL-102` (blocked on one file from you) ·
`BL-02` (the 40+ kits, blocked on your CSVs).

---

## Index — 37 open entries

| id | | what it is | area |
|---|---|---|---|
| `BL-02` | 🔵 | The 40+ class kits, 3rd and 4th tier — five files done, the rest wait on your CSVs | classes |
| `BL-05` | 🔵 | Crafting — the two pieces you did not rule ⏸ parked | items |
| `BL-09` | 🔵 | A floor under the wrong-weapon magic penalty, bought back by Spellcaster Mastery | combat |
| `BL-15` | 🟡 | `precision` / `anti_magic` as LEARNABLE passives — gated on the warrior/rogue CSVs | combat |
| `BL-18` | 🔵 | The nuker-vs-champion measurement — 19% apart, and whether that is wrong | combat |
| `BL-19` | ⏸ | Combat depth — perfect/excellent block, position bonuses | combat |
| `BL-21` | 🟡 | Per-mob and per-zone drop identity — queued behind `BL-48` | items |
| `BL-23` | 🔵 | The coin curve — measured, and it is not what the old entry claimed | items |
| `BL-25` | 🔵 | The drop-group simplification — half built, half unquotable | items |
| `BL-30` | ⏸ | Recipe drops below A grade | items |
| `BL-38` | 🔵 | Pets and summons — totems, class pets, the mage summoner | classes |
| `BL-41` | 🔵 | A grade filter on the craft Gear page | UI |
| `BL-44` | 🟡 | "Everything is a skill" — armor sets and weapon specials, the last two pieces | classes |
| `BL-45` | 🔵 | The presentation pass — sounds, effects, the feel of it | UI |
| `BL-48` | ⏸ | Instances — one decision open: daily attempts GLOBAL vs PER-INSTANCE | world |
| `BL-50` | ⏸ | A boss/elite mat pile must obey the party loot rule ⏸ parked with crafting | items |
| `BL-51` | 🔵 | Castles + vault — needs the siege design first | world |
| `BL-52` | 🔵 | World expansion toward 1kk+ | world |
| `BL-60` | 🔵 | Death penalty, resurrection skills, Angel's Protection | systems |
| `BL-61` | ⏸ | Network payload optimisation | systems |
| `BL-62` | ⏸ | Bot-prevention CAPTCHA | systems |
| `BL-72` | 🔵 | Unbuffed auto-farm is not survivable for either damage kit | world |
| `BL-73` | 🔵 | Mob social clans go back ON once the map spreads the camps out | world |
| `BL-74` | 🔵 | The phone still does not treat the app as a game | UI |
| `BL-75` | 🔵 | The heal-at-0 skill wants a warrior/demon home — waits on `BL-02` | classes |
| `BL-76` | 🔴 | Boss skill gems — a boss drops a gem that grants a skill, three rarities | items |
| `BL-78` | 🔵 | Mobs are too easy — three of four built, only THE BILL is left | world |
| `BL-80` | 🔵 | Fortress sieges — your own design, transcribed whole | world |
| `BL-84` | 🔴 | Rename every skill id to match its name — unblocked, needs a window | classes |
| `BL-93` | 🔵 | In-game visuals — models, terrain, the look of the world | UI |
| `BL-102` | 🔴 | The character models have no animation clips — one file from you | UI |
| `BL-103` | 🔵 | Visible weapons — the shape is settled, the meshes are not | UI |
| `BL-104` | 🔵 | The warrior's sword-vs-blunt split — ruled, nothing to attach it to yet | classes |
| `BL-106` | ❓ | Your cross-chain id rule — six ids disobey it; three answers wanted | classes |
| `BL-154` | 🔴 | Tank PULL — ENGINE BUILT (0.110.0-0.110.2); ✅ **his `tank 4th.csv` numbers LANDED 2026-09-04**; the two AoE shapes + the drag-smoothing clamp remain | combat |
| `BL-155` | 🔴 | SILENCE — BUILT (0.110.0); ✅ **the tank rows are no longer placeholders — Numbing Shock is authored 76→90** | combat |
| `BL-157` | 🔵 | The worm — a polymorph debuffer/nuker class, a seed only | classes |
| `BL-163` | 🔴 | The buffer shelf as an EXTERNAL table — no wrappers, editable without a build | classes |
| `BL-164` | 🔵 | The three Marks share one Rank, so the weaker rung can out-hold the stronger | classes |

---

## The entries

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier)** — ✅ **FIVE OF THE AUTHORED FILES ARE DONE — the BULWARK (tank) landed 2026-09-02 (0.105.0), the fourth finished 3rd class.** Race decides four of its tools, which is the first time race has decided anything about a class here: Human taunt/mass-taunt, Elf charm/freeze, Demon taunt/intimidate, and the two Shield Smashes split Human;Elf vs Demon. ⚠ THE PASS SPANNED ALL THREE TANK FILES — `tank 2nd.csv` was retuned in the same breath (taunt 3s→1.5s at 0 MP, Charm at 24, Shield Shock replacing Shield Stun, Stay! moved to the 3rd, Defensive Wall's ×2 terms deleted). ✅ **`tank 2nd`/`3rd`/`4th` are ALL FINISHED — he said so 2026-09-04 (*"im done with tank 2/3/4 so its ready to build after the npc buffer"*) and it is verified: the `NOT DONE` banner is gone, 205 authored rows, and Grapple + Numbing Shock are laddered 76→90 with real numbers. THE TANK PASS IS THE NEXT BUILD AFTER `BL-158`…`BL-162`.** Six slips in his files were caught and corrected on BOTH sides — see the CHANGELOG. Older note follows. The
  **Lightbringer (healer) shipped in 0.74.0**, the **whole Warchanter (buffer) in 0.76.0**, the
  **Lightbringer's 4th tier in 0.85.0** (with the shared kit and the eighteen Sigils), and the
  **NUKER's 3rd tier in 0.87.0** — 208 rows, 21 families, Magus and Tempest, all three races, 40 to 74.
  `SkillCsvSeed --check` is green on all twelve walked files. That is the proof the pipeline works end
  to end, four times over.

  ⚠ **The nuker one is the lesson worth keeping: `nuker 3rd.csv` had been FINISHED since before the
  healer's was, and nobody noticed for six days** — it was never added to `Check.Specs`, so the one tool
  that would have shouted about it never opened the file. **A finished file that no spec walks is
  invisible.** When you finish a file, say so, and its `Check.Specs` line goes in the same day.

  What is left, and it is now a short list:
  - 🔵 **`buffer 4th.csv` — you are authoring it.** Rows through the Mark block are done; line 125 is
    your `NOT DONE FOR NOW` banner and the bow/blunt/2H masteries, Twin Arrow, Sound Smash and Acoustic
    Shock sit below it. Not started, on your instruction (2026-08-26: *"dont do buffer 4th as im
    authoring it"*). When it lands, Harmony Mark's id is `harmony_mark` and it **must share `MarkKey`**
    with the healer's three or a healer's Mark and a buffer's would stack.
  - 🔵 **Five 3rd files are still two-line placeholders** — `tank` (one real row), `warrior`, `war_aoe`,
    `dual`, `archer` — and **seven 4th files** with them. Same rule: nothing invented in the meantime.
  - ✅ ~~**Calm Spirit**~~ — SHIPPED with `BL-92` in 0.88.0, the moment the MP-regen question it was
    held behind was answered. Nothing of the nuker's file is outstanding.

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

- `BL-09` 🔵 **A floor under the wrong-weapon magic penalty, bought back by Spellcaster Mastery.**
  ⚠ **Re-marked 🔵 on 2026-08-14 — it contradicts your own CSV.** This asks for five Mastery rungs
  walking the penalty 0.5 → 0.05; `docs/data/classes_skills_csv/mage 1st.csv` authors Spellcaster
  Mastery as a **single-level, auto-granted, never-replaced** passive carrying the whole rule
  (*"Bow/Dagger/None: cast x0.5, mAtk x0.5, mAcc x0.5"*), and the code matches it exactly
  (`Entity.cs:2264-2282`, `StatCaps.UntrainedWeaponMagicFailMod = 25`). Adding rungs re-specs the CSV.
  Your original words are kept below — say whether the CSV or this note wins.
  *"hitting above the 0 difference is not failing … if we can make a floor … a strong 50% with wrong
  weapon celing((formula),0.5) that is always 50% on the norm … L1 - 0.5 .. L5 ..0.05(the min)."*
  Read as: a wrong-weapon caster is capped at 50% success at parity, and the five Mastery rungs walk
  the penalty 0.5 → 0.05. *(playtest-21 `64c`, never answered.)*

- `BL-15` 🟡 **`precision` / `anti_magic` should be LEARNABLE PASSIVES, not auto-granted floors —
  and it waits for the warrior/rogue CSVs.** Your ruling, 2026-08-27: *"i would like them to be a
  learnable passive not a auto learn.. so remind me once i start authoring warrior/rogues."*
  - **What changes.** Today both are auto-granted floors: they appear at a level with no row, no SP
    price and no place in a ladder (`--check` reports them as ⚪ AUTO-GRANTED against `warrior 2nd.csv`
    and `tank 2nd.csv`, which is what an auto-grant looks like on purpose). As learnable passives they
    become ordinary CSV rows — learn level, SP, rungs — and the Learn tab shows them.
  - 🟡 **Gated, deliberately.** A learnable passive is a **CSV row**, and inventing one re-specs the
    file you have not written yet. `warrior 3rd`, `rogue 3rd` and their 4th-tier files are still
    two-line placeholders (`BL-02`), so this lands the day you author them and not before.
  - 🔔 **THIS IS THE REMINDER YOU ASKED FOR.** When you open a warrior or rogue file, `precision` and
    `anti_magic` want rows in it. ⚠ **A class-skill-TABLE change needs a new APK** — the client builds
    its Learn tab locally — so it rides a client batch, not a server-only push.
  - ⚠ The level question the old entry asked (class change vs 76) is answered by this: a learn level
    is whatever the row says, so there is nothing left to rule separately.

- `BL-18` 🔵 **The nuker-vs-champion measurement (`0a`).** The nuker beats the champion by 19% in
  the matrix. You deferred the ruling to play: *"This need to be tested. When I leave the chars to
  play alone all measure."* ⚠ That makes auto-farm load-bearing for a balance decision — and
  auto-farm has never been through a long unattended run.

- `BL-19` ⏸ **Combat depth — held by you (2026-08-01).** Perfect/excellent block · position bonuses
  (hook reserved) · PvP and PvE damage multipliers (both hooks exist and are 1.0). *"the combat
  depth I don't want it build for now defer it."* Not dropped — do not build unasked.

- `BL-21` 🟡 **Per-mob and per-zone drop identity.** *"I would like obe mob to drop let say a sword
  and a 2h sword, the other to drop only main armors, third boots and helmet … to go to a spot and
  know I can get there light armor and 2h-sword."* Then: *"later I'll want a 'ork settlment' where
  are 5 different demon types and I go there for lvl up, and several different settlements and zones
  with meanings."* You gated this yourself behind the world-map/positions pass (`BL-45`).

- `BL-23` 🔵 **The coin curve — MEASURED 2026-08-27, and it is not the problem the old entry claimed.**
  You replaced the assertion with a measurement request: *"i want potion/rune per hour consumation and
  golddrop/h .. to compare for fewe lvl rangees - for now at lvl 43 i have 5kk + gold so it dont seem
  like a problem."* Built as **`dotnet run --project tools/BalanceMatrix -- --goldflow`**, off the real
  drop tables, the real vendor prices and the real damage formulas. What it says:
  - ✅ **YOUR 43 READING IS EXACTLY RIGHT.** At 43 a farming character nets **740k-1,010k gold/hour**,
    so your 5kk is five to seven hours of play. The model and your save agree without tuning either.
  - ✅ **POTIONS ARE NOT A COST.** Priced at the cheapest tier that can actually SUSTAIN the deficit,
    potion burn is **0-3% of income at every band from 20 to 76**, and 10% in the single worst case
    (the level-85 nuker). There is no potion economy to fix; regen covers most kits outright.
  - 🔴 **THE RUNE IS THE REAL COST, AND ONLY WHEN YOU ARE POOR.** A 1h War Rune box is 150,000 flat.
    At 20-30 an hour of farm buys **2.4-2.9 hours** of rune (~35-40% of income); by 61 it buys 25, by
    85 it buys 37. So the rune is a **newbie tax that evaporates** — the opposite shape to a drift.
  - 🔴 **THE DRIFT IS REAL BUT IT IS 5.4×, NOT 51×.** Hours of farm per chest piece of your own tier:
    **1.68 at 20 → 0.50 at 40 → 0.46 at 61 → 0.31 at 76.** Monotone downward apart from the expected
    bump inside a tier (price is flat from 40 to 51 while gold keeps climbing, which is why 43 reads
    better than 40).
  - 🔴🔑 **THE SHARP FINDING IS A CLIFF AT 80, WHICH THE OLD ENTRY NEVER MENTIONED.** S grade is
    **top-half only** — Epic/Legendary/Mythic, no Common rung exists (`ItemCatalog.IsTopHalfOnly`) —
    so the cheapest level-80 body is **126,000,000** and an hour of farm buys **0.04** of it: about
    **26 hours per piece**, against 3 hours at 76. That is not the coin curve drifting, it is the
    gear ladder stepping, and it is where a fix belongs if you want one.
  - ❓ **What I need from you.** Three separate calls and they are independent: (a) is 26h/piece the
    intended endgame grind, or does S want a cheaper rung; (b) does the 150k rune want a cheaper
    low-level box, since it is only ever felt before 40; (c) is a 5.4× drift across 20-76 acceptable
    as pacing? **Nothing moves until you say** — every rate here is one you have already tuned once.

- `BL-25` 🔵 **The drop-group simplification — half built, half unquotable.** *"In a way I want to
  simplify it"* — the inner roll should pick the drop **directly** rather than picking a rarity first,
  with per-item control (your example: a rarer Scroll of Resurrect inside its own group).
  ⚠ **Re-marked 🔵 on 2026-08-14.** The **per-item half SHIPPED** — `RateConfig.DropItemRates` plus
  `/droprate item <id> <mult>`, which is your Scroll-of-Resurrect example working today. The other
  half has **no surviving verbatim quote anywhere in the repo**, and the current shape is deliberate:
  the comment at `MobCatalog.cs:262-265` records that one group per (family, rarity) is what lets a
  BOSS row summing past 100% (E 70 + L 40 + M 2) drop several pieces at once. Collapsing the groups
  would break boss multi-drops and move a measured economy. **Say it again in your own words and it
  goes back to 🔴.**

- `BL-30` ⏸ **Recipe drops below A grade** — no recipe item exists under A (below 76 they are
  learned by level). Add the same way A+ was added, when there is a reason to.

- `BL-38` 🔵 **Pets and summons** — immovable totems, class pets, the mage summoner. Designed, never
  scheduled, never re-raised by you.

- `BL-41` 🔵 **A grade filter on the craft Gear page.** 62-63 rows is a long scroll on the phone.
  The question was put to you and never answered.

- `BL-44` 🟡 **"Everything is a skill" — the last two pieces.** Armor sets and weapon specials are
  still `StatMods`, not skills, so **buff-bar row 3 (item effects) is permanently empty**; and the
  set tooltip's **shield row** has nothing to show until shields belong to sets. You called this
  optional at the time.

- `BL-45` 🔵 **The presentation pass.** Your words, still true: *"no sounds, a bit woody, no good
  visuals."* The loudest remaining gap. **You have reserved it for its own discussion** (2026-08-14:
  *"45 is a separate discussion later on"*) — do not start it piecemeal.
  ⤷ 🆕 **The VISUAL half of it now has its own id and its own conversation: `BL-93`.** `BL-45` keeps
  the rest — sound, feel, feedback, polish.

- `BL-48` ⏸ **Instances — you are holding.** Design is written (`design/Instances.md`). One
  load-bearing decision is still open: the daily attempt **GLOBAL vs PER-INSTANCE**. It changes the
  persisted model, so it is answered before anything is built. **Dungeons are the cheap half** —
  a dungeon is just a `SpawnZone` outside the town ring plus a teleport entrance, near-zero risk,
  and they can ship without instances.

- `BL-50` ⏸ **A boss/elite crafting-mat pile must obey the party loot rule.** Written as *(not
  tested)* and never tested. **PARKED with the rest of crafting** (see the top of this file) — it can
  only be verified inside the mat-farming playtest you have reserved.

- `BL-51` 🔵 **Castles + vault.** Needs the siege design first; consumes the reserved
  `VendorBuyTaxRate` hook.

- `BL-52` 🔵 **World expansion toward 1kk+.** The 0.33.0 re-layout was the first step and nothing
  followed it. `BL-21` is queued behind this one.

- `BL-60` 🔵 **Death penalty, resurrection skills, Angel's Protection.** The 2026-07-17 design —
  death XP penalty, res skills and scrolls, a buff-keep-on-death. Nothing exists in code. Overlaps
  `BL-59`; read them together.

- `BL-61` ⏸ **Network payload optimisation.** Split/delta snapshots and a local buff countdown, then
  optionally MessagePack. Deferred deliberately: no measured problem, the protocol still churns
  every session, and MessagePack's dynamic resolver does not work under Unity/IL2CPP without a
  codegen step. A late, one-line swap once the protocol settles.

- `BL-62` ⏸ **Bot-prevention CAPTCHA** ("petrification" after 200-500 manual kills). Revisit with
  behavioural detection. Your own worry stands: an AI, as opposed to an if/else bot, solves it.

- `BL-72` 🔵 **Unbuffed auto-farm is not survivable for either damage kit.** His `0a` note
  (playtest-22): *"they both have hard time to farm without buffs .. when i login in 1-2h after the
  npcs buffs are gone both are dead and with potion buffs."* Two separate questions inside it, and
  the second is the real one:
  1. Is an unbuffed nuker/champion *meant* to survive an unattended hour? The NPC buff ladder is
     currently load-bearing for auto-farm, which nothing was designed to be.
  2. **It also invalidates the `0a` measurement itself** (`BL-18`) — a run that ends in a death an
     hour in is not measuring the kits, and the auto-buff tab (§78) is what would keep one alive long
     enough to measure. Read the two together before spending a session on either.

- `BL-73` 🔵 **Mob social clans go back ON once the world map spreads the camps out** — your own note
  from playtest 23, *"Make a note to turn it on once the world map is in place."* The feature works and
  you saw it work; what makes it unplayable is **spawn DENSITY, not the 450 radius**: *"all mobs are
  spawning almost next to each other and hitting one wolf getting ganked by 10 other … For a mage lvl 9
  hitting a warefolf means dead."* Your target shape is *"it will call ONE, and while you fight, if
  others wander in the social range they will aggro"* — which is what the same 450 radius already does
  once a camp is not stacked on one point. **Nothing was deleted**: the twelve clans are still authored
  on the mobs and every line of the call code is intact, behind **one switch**
  (`GameConstants.MobClansEnabled`). Flip it when the camps are laid out; the retune that follows is
  the SPACING, not this feature.

- `BL-74` 🔵 **The phone still does not treat the app as a game** — playtest 23: *"as of 0.67.2 still
  game launcher don't treat it as a game. May be because of its development installation not store one.
  Dunno. Need to research how the phone and when it treats an app as a game."* Everything a manifest can
  claim is already claimed and shipped in 0.67.0 (`BL-46`): the duplicate LAUNCHER activity is deleted,
  `android:appCategory="game"` and Samsung's older `isGame="true"` are both declared, and exactly one
  launcher entry stands behind them. So the remaining variable is **outside the manifest** — One UI's
  Game Launcher is known to classify partly by Play Store category and install source, which a sideloaded
  debug APK has neither of. 🔵 **Owed as RESEARCH, not a build**, and it cannot be verified from here:
  it needs your device (does Game Booster's "add app manually" find it? does a release-signed APK behave
  differently from a debug one?). Nothing is broken in the game either way.

- `BL-75` 🔵 **The heal-at-0 skill wants a warrior/demon home.** Playtest 23, on the old Undying Will
  behaviour: *"That idea for undying skill is good for a warrior ork, when he must die just heal himself
  30%"* — and *"as I said good skill for a warrior"*. 🔑 **It is already built and needs no new mechanic**:
  `LastStand` (`SkillEffect.LethalSave`, revives to 50% of max HP off a fatal blow, buff consumed) has
  been in the catalog the whole time; its learn line went in the 40+ purge. What is missing is only a
  **class + a level + the percentage** — which is 40+ authoring, so it waits on `BL-02` with everything
  else. Your two words to settle when you get there: is it Ravager/Warlord or race-gated to the demon, and
  is the number your 30% or the skill's existing 50%?

- `BL-76` 🔴 **BOSS SKILL GEMS — a boss drops, for its own level, a gem that grants a skill.** Your
  design, 2026-08-15: *"A bosses to drop for their lvl a special skill gem .. 3 rarities ..
  Epic/Legend/Mythic ... Chance for boss like 50% for a epic ... 5 for l and 0.5 for myth ... A epic
  can get u a magic or a physical dmg skill for the current lvl that do 1:5 of a nukers/fighters skill
  as dmg .. A legend can get u a passive that increase pvp/pve atk/def + 1:2 skills dmg ... And myth
  can also increase a stat +1 (at random) with 1:1 dmg and higher % for pvp /pve dmg."* Your closing
  clause is part of the spec: ***"the % and values can be then altered"*** — the numbers below are
  placeholders you have pre-authorised to move, so do not treat a retune of them as re-speccing you.

  | Rarity | Drop chance / boss | What the gem carries |
  |---|---|---|
  | Epic | **50%** | one damage skill (magic OR physical) at the boss's level, **1/5** of the class skill's damage |
  | Legendary | **5%** | a passive: PvP/PvE **atk + def** — plus the skill at **1/2** damage |
  | Mythic | **0.5%** | the Legendary passive at a **higher** PvP/PvE %, **+1 to a random stat**, skill at **1/1** |

  🔑 **Why this one is worth building even before the numbers settle:** it is the first content that
  makes a boss kill matter *for its own sake* rather than as a lump of EXP, and it is the only reward
  in the game whose value is not on the gear ladder. It also gives the **PvP/PvE damage multiplier
  hooks a first real consumer** — they exist and are hardcoded 1.0 today, reserved under `BL-19`, which
  you are holding. A Legendary gem is what turns them on, so this entry is where that hold gets lifted.

  🔵 **Five shape questions, all small, all answerable at build time — none of them blocks queueing
  this.** Recorded now so the build does not invent them silently:
  1. **Is a gem consumed into a permanent learn, or is it worn?** "Get u a skill" reads as consumed.
     But a stat +1 and a PvP passive read as *equipment* — and a worn gem needs a slot, which the
     paperdoll does not have. Consumed-and-learned needs no new slot and no new UI.
  2. **What decides WHICH damage skill?** Rolled at the drop (so a gem is a lottery you can trade) or
     picked by the holder (so it is a reward you steer). Trade value differs completely.
  3. **"For their lvl" — does the gem carry the BOSS's level or the opener's?** A level-20 boss gem
     used at 60 is either dead weight or a free rung, and those are opposite economies.
  4. **Duplicates.** A second Epic gem of the same skill — refused, upgraded, or a second copy to sell?
  5. **`1:5` of WHOSE skill?** A nuker's and a fighter's top skill at the same level are not the same
     number, so the ratio needs one named reference skill per channel or it drifts by class.

  ✅ **The boss curve underneath it is no longer unruled** — this used to say *"a flat ×100 swings boss
  difficulty 11× between level 20 and 76"*, and `BL-13` fixed exactly that in 0.89.0: every boss in the
  game (44 / 60 / 65 / 90) now takes an 18-23 minute party fight. So a 50% gem drop no longer makes the
  lowest boss the cheapest gem in the game by fight length. ⚠ What is still uneven is the **EXP** it
  pays (`BL-49`, which you ruled *"leave it"*), so a lower boss remains the better hour in exp terms —
  worth knowing when you set the gem %, which are explicitly yours to move.

- `BL-78` 🔵 **MOBS ARE TOO EASY — three of the four items are BUILT; only THE BILL is left, and it
  is yours to rule.** Your playtest-25 words: *"now mobs as general feel easy ... tank get hit fo 30 ..
  others for 100-200 but the rogue almost one blow it .. mage one/two shot it .. and there is no
  thrill in fighting"*. The research you asked for is
  **[balance/MobCurveVsIG.md](balance/MobCurveVsIG.md)** — 2,831 IG creatures, levels 1-83.
  1. ✅ **THE HP MULTIPLIER — BUILT 0.94.0, and you moved the lever while ruling it.** This entry used
     to say "author `MobMod.Hp` across the roster". You ruled instead (2026-08-27): *"the 15k mobs are
     zone placed with x2/x3 hp .. some zones can have x1"* — so the **ZONE** carries it, the same
     creature reads ×1 in one field and ×3 in another, and not one template was edited. One derived
     ladder (`WorldPlan.HpScaleFor`), overridable per field with `Band.HpScale`. `MobBaseStats.Hp(80)`
     = 5,160, so ×3 = **15,480** — your *"the 80 mobs should have 15k not 5"* on the nose. ⚠ A boss
     ignores it (0.89.0's measured 12-25 min band derives from the same curve); an elite does not. It
     multiplies HP and nothing else. 🔴 **THE RUNGS ARE NOT THE ONES BUILT HERE ANY MORE — you re-ruled
     them 2026-09-03 as `BL-148` (×1 <40, ×1.5 40-75, ×2 76-83, ×3 84+).** Read that entry, or
     `WorldPlan.HpScaleFor` itself, never this line.
  2. ✅ **A CASTER MOB IS NOT A SQUISHY MOB — BUILT 0.94.0.** *"caster mobs are not weaker than the
     other, they just use spells (and have a bit less pdef, evasion not twice less)"*. ⚠ **This entry
     was wrong about the cause and said so for days**: it claimed a caster paid twice with "low P.Def
     AND low HP", and `MobRole.Mage` never touched HP at all. The double-dip was on DEFENCE — the
     role's ×0.7 compounding with a template's own `MobMod.PDef`, worst at `watcher_eye` (0.5 × 0.7 =
     **×0.35**). The role now reads like Archer, ×0.85 and +8 evasion — IG's `Light Armor Type` word
     for word.
  3. ✅ **THE PLAYER CURVE — BUILT 0.91.0.** Max HP is a growth rate that steps at every class change,
     keyed by discipline. A robe class at 52 survives **21s, not 9s**. See `BalanceMatrix --hpcurve`.
  4. 🔵 **THE BILL FROM 0.73.0, AND IT IS STILL YOUR CALL — now with a second charge on it.** Doubling
     creature defence took a full S-grade character from **347 to 603 farm hours** and dropped an elite
     camp from 115% of a normal farm to **76%**; an unattended farm at parity stopped sustaining itself
     (level 52: 26 kills before the HP bar empties, now 9). ⚠ **0.94.0's HP ladder adds to this bill,
     not beside it** — a ×2/×3 field is ×2/×3 the time-to-kill for the same reward, so `BL-22`'s budget
     and the auto-hunt consumable question both move again. Nothing has been retuned to compensate,
     deliberately: you asked for the mobs to be heavier, and quietly paying for it out of drop rates
     would hide whether the change worked. **Measure it with `--goldflow` and `--guards` before ruling.**

- `BL-80` 🔵 **FORTRESS SIEGES — your own design, transcribed whole, and you said it can wait.** *"this
  system can be defered and just have it as idea or can build some base ground for it."* Recorded here so
  it is not lost; the verbatim text is in
  [Playtest-Archive.md#playtest-25](testing/Playtest-Archive.md#playtest-25). The shape:
  - **A weekly window.** All fortresses attackable once a week; the quest is offered **30 minutes before**
    the start. *"once defeated they cannot be reengaged"* — no respawn, no re-taken quest.
  - **A garrison of social pMobs on a ±2 band** (a Lv 60 fortress runs 58-62): troopers/tanks and archers
    on **basic attack only** in **common t52** (aggro 400, archer range 600), mages in common t52 that
    cast, healers in common t52 that heal allies and deal no damage (passive, heal range 500, **normal
    heals not quick**). **Commanders** in **rare t52** use skills; the **king's guard** is **rare t61**
    (archer, mage, two healers, and a tank if the king is a warrior or a warrior if the king is a tank);
    the **king** is **mythic t61** with a War Rune and **twice HP/pDef/mDef/pAtk/mAtk**.
  - **Four gated stages through one entrance:** 10-15 outer troops → an outer **mob-gate** → 20-30 troops
    inside → the commander party → an inner gate → the leaders and the king. A **gate** is a *"targetable
    imovable door"* that becomes mortal only when its side is cleared, **immune to skills, DoT, debuffs and
    crits**, takes ~1000 normal attacks (his suggestion: 1 damage per hit, ~1000 HP).
  - **The commander party fights like players** — *"kill the healer 1st idea"*: near-infinite MP, quick
    heals, party heals, debuff removal; the tank taunts and uses an ultimate; the others stun and debuff.
  - **PvP is automatic inside the field**, other parties and clans can attack the same fortress, the king
    drops **boss loot + raid points**, and the completion quest pays *"every participant (not all that took
    the quest - but who fought inside)"* in gold, EXP and raid points.
  - 🔑 **It is a template.** *"if we make a template of a fortress - we can reause it just change the grade
    of equipment"* — so one authored fortress plus a grade parameter is the whole content pipeline.
  - 🟡 **Gated on real prerequisites, which is the honest reason to defer it**: `BL-47`'s pMobs (built),
    `BL-51` castles/sieges (nothing exists), **raid points** (no such currency), a **weekly world clock**
    (`GameClock` has no weekly window), and mob **healer/commander AI** that casts like a party. ⚠ It also
    presumes clans, which are **OFF** (`BL-73`).

- `BL-84` 🔴 **RENAME EVERY SKILL ID TO MATCH ITS NAME — UNBLOCKED 2026-08-20: THE HEALER IS DONE.**
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

- `BL-93` 🔵 **IN-GAME VISUALS — MODELS, TERRAIN, THE LOOK OF THE WORLD. You asked for the discussion,
  2026-08-26:** *"after all I want to speak about the in game visuals - models/terain etc."* Opened as
  a placeholder for that conversation and **deliberately not designed here** — the same treatment
  `BL-45` got, and for the same reason: it is the one area where starting piecemeal produces work that
  has to be thrown away when the direction is set.

  What is worth having ready when we do talk, so the conversation starts from facts rather than from
  scratch:
  - **What the client draws today.** Capsules and coloured plates on a flat ground plane, with the
    3D/LoS work (`client-3d-and-los-design`) as the only shape decision ever made. Every creature in
    the game is the same silhouette at a different scale and tint, so a level-80 field boss and a
    level-3 wolf read as the same object — which is a presentation problem, not a content one.
  - **The two ground layers that already exist and could carry a look for free** — the totem and AoE
    decals (0.79.x) and the zone/region system, which already knows where every camp, town ring, road
    and dungeon mouth is. Terrain that follows the zones costs nothing in new data.
  - **The constraint that decides everything: it is a PHONE.** Model budget, draw calls, atlas size and
    APK size are the real ceiling, and the TMP atlas is already static and full at 250 glyphs
    (`tmp-font-atlas-is-static`). An art direction that ignores the device is a rebuild.
  - **The IP rule applies to ART as hard as it does to names** — see `naming-no-trademarks`. Silhouettes
    and skins that read as another game's creatures are the same problem the town names were.

  🟢 **OPENED AND ANSWERED, 2026-08-26/27. Direction set, step 1 built.** What you ruled:
  - **Low-poly stylised, CC0 sources**, accepted once it was clear it is swappable later — and 🔑 the
    thing that locks you in is **the RIG, not the polycount**: Unity **Humanoid** avatars mean a better
    body drops onto the same skeleton with no code change. Generic would be the rebuild.
  - **Downloadable assets** (*"a 100mb apk then download 10gb data"*) — yes, Addressables + a remote
    catalog off `UseStaticFiles()` on the server you already run. 🔴 **Not needed yet** (43 MB APK with
    zero art; low-poly lands ~60-90 MB) and ⚠ **bandwidth is the ceiling — your server is a phone.**
    The seam is in for free: models load by key through one function.
  - **Camera: unchanged for now.** *"Let's make proof of concept with models then see camera where it
    stands."* I had argued for pulling in to a 3/4 view — **deferred behind the POC, don't re-propose.**

  **Step 1 is BUILT (protocol 29, see the CHANGELOG):** `Category`/`Role` on the wire, the family→prefab
  fallback chain, facing + attack/cast/death animation off messages that already existed, and a
  "3D models: off" quality preset. Everything still renders as spheres until art lands — deliberately.
  ⤷ ✅ **DONE 2026-08-28 (0.100.1) — the Editor session happened and `humanoid.prefab` exists.** Every
  player, NPC and humanoid mob now has a body; the FBX source packs are committed beside it
  (`Models/Characters/`, `Models/Monsters/`), your call: *"push all ill later remove/update them to
  prefabs - if PoC works"*. APK 43 → **49 MB**.
  ⤷ 🔵 **NEXT, and it needs no code:** 50 of the 83 mob templates are not humanoid and wore a human
  body. **20 of them are fixed as of 0.100.2** (animal + insect, below); Undead (9) and Dragon (5) are
  the next two and their FBXs are already in the repo. The copy-and-paste table is in
  `docs/guides/UnityClient.md`, *"The nine monster names"*. Demon/Angel/Plant have no fitting model
  yet. ⚠ Monster FBXs stay on rig **Generic**; only bipeds get Humanoid.
  ⤷ ✅ **THE DEFERRAL IS REVERSED BY YOU, 2026-08-28 — AND BUILT (0.100.2):** *"can u add 1~2 mobs? U
  said u can do it alone as I don't have access to the pc. (again as poc)"*. The 2026-08-28 ruling
  (*"skip automating the prefabs for now .. then ill do 1-2 animals by hand"* — archived) assumed you
  would hand-make the first ones; you cannot reach the Editor, so the tool exists:
  **`Assets/Editor/ModelSetup.cs`**, run headless with `-executeMethod`.
  **`mob_animal` (Rat) and `mob_insect` (Spider) are in** — 20 of the 79 roster templates, and the
  first creature in the game (Ridgeback Pup, Lv 1) is one of them. 🔑 **They ANIMATE** — the monster
  FBXs ship Idle/Walk/Run/Attack/Death, which is exactly what `EntityView` has been driving since
  protocol 29, so the animation path is now proven with no new message and no client code.
  🔑 **Adding a family is ONE LINE in `ModelSetup.Families`** — `mob_undead` (Skeleton, 9 templates)
  and `mob_dragon` (Dragon, 5) are the next two and their FBXs are already committed. Say the word.
  What is still NOT automatable, and is why you held it: which model suits a family, and its height —
  both are authored per row, because the packs disagree on scale (the Rat imports 2.9 units tall).

  ⤷ 🔴 **THE PLAYER MODEL CANNOT ANIMATE — see `BL-102`. It is a missing FILE, not missing code.**

  Still un-started, in the order I'd do them: **terrain generated from the zone circles** (biggest
  perceived change per hour, needs no art) → creature families → **8 skill-FX archetypes** (one enum +
  colour on `SkillDef`; the client reads `SkillCatalog` directly, so no protocol change) → **~25 sound
  clips + 2-3 ambient loops** → skybox/fog/day-night (🔑 `GameClock` is already server-synced).

- `BL-102` 🔴 **THE CHARACTER MODELS HAVE NO ANIMATION CLIPS — I need a file from you, and it is the
  only thing standing between you and a running character.** You asked, 2026-08-28: *"now if we can add
  runing animation"*. The wiring is done and proven — the two mob families in 0.100.2 walk, run, swing
  and die off messages that already existed. The player does not, for one reason:

  **All 21 FBXs in `Models/Characters/` contain zero animation.** Measured, not assumed: mesh,
  skeleton, bind pose, 65 bones — and `AnimationStack` count **0**. The monster pack ships five takes
  per creature; the character pack you committed ships none. `humanoid.prefab` is a body with nothing
  to play, so it slides in its bind pose. No controller can fix that: there is nothing to put in it.

  🔑 **What you did right and what it buys you:** you set the character rig to **Humanoid**. That is
  the setting that makes clips *retargetable*, so any humanoid animation set drops onto these bodies —
  and onto the elf and demon bodies you add next week — with no per-model work. This is exactly the
  swappability argument from the direction talk, arriving early.

  **Two ways to close it, both free, either is fine:**
  1. **The pack's own animation file.** These packs normally ship a separate animations FBX beside the
     characters; it was not in what you copied across. If you still have the download, that one file is
     the whole fix.
  2. **Mixamo** — upload one character FBX, pick clips, download "without skin". CC0-safe for this use
     and the standard route for a Humanoid rig.

  ⤷ ✅ **MY HALF IS BUILT (0.102.2) — WHAT IS LEFT IS THE FILE, AND NOTHING ELSE.** `ModelSetup` now
  has a character half: `BuildAll` builds the bodies as well as the creatures, the clip sources are
  imported as retargetable Humanoid motion, single-take files are renamed to their own file name (every
  Mixamo take is called `mixamo.com`), locomotion is looped and root-locked, `Casting` joined the
  generated controller, and `humanoid.prefab` is rebuilt with a wired `Animator`. **An empty folder is a
  skip, not a failure** — running it today changes nothing and overwrites nothing.

  **THE THREE STEPS THAT ARE YOURS:**
  1. Put animation files in **`Game.Client.Unity/Assets/Resources/Models/Characters/Animations/`**
     (the folder exists and is empty). Named `idle.fbx` · `walk.fbx` · `run.fbx` · `attack.fbx` ·
     `death.fbx` · `cast.fbx`. **Only `idle` is required** — `walk` falls back to `idle`, `run` to
     `walk`, so *two* files already give you a character that stands and runs. Mixamo: **FBX Binary**,
     **Without Skin**, tick *In Place*. Names are a substring match, so `Walking.fbx` works unrenamed.
  2. Run it with the Editor closed:
     `Unity.exe -batchmode -quit -nographics -projectPath …\Game.Client.Unity -executeMethod
     Game.ClientEditor.ModelSetup.BuildAll -logFile -`
  3. `pwsh tools/publish.ps1 -Apk` — `Resources` ships inside the build, so a new APK is not optional.

  📖 **Step by step, with the download settings and a symptom→cause table:**
  `docs/guides/UnityClient.md` → *"Adding move / idle / attack animations to the PLAYER"*.

  🔑 **You buy this once for every body you will ever have.** The clips retarget through the Humanoid
  avatar, so the same files animate all 21 characters and the elf and demon bodies you add later, with
  no per-model work and no second download.

- `BL-103` 🔵 **VISIBLE WEAPONS — the key shape is settled, the meshes are not. Your design, 2026-08-28:**
  *"if I make a sword1h.prefab and one sword1h_t20.prefab can that work? a t20 swords to be this one
  every rarity (we can change hue for example or glow) and if no tier prefab to fallback to default"*.
  **Yes — and `sword1h_t20` is not an invented name: it is literally an existing item id**
  (`TieredWeapons` emits `$"{w.Key}_t{L}"`).

  **The eight weapon keys** — `sword1h` · `sword2h` · `blunt1h` · `blunt2h` · `duals` · `bow` · `wand` ·
  `staff`. **The seven tiers** — 1 / 20 / 40 / 52 / 61 / 76 / 80.

  🔑 **KEY ON THE FAMILY + TIER, NOT ON THE ITEM ID.** The id space also holds `_rare` / `_epic` /
  `_legendary` copies (`ItemCatalog.QualityId`), `_lo` sets and `_dmg` variants — key on the id and
  every rarity demands its own prefab, which is exactly what your hue/glow plan avoids. The chain:

  ```
  Models/weapon_sword1h_t20     your tier prefab
  Models/weapon_sword1h         your default for that weapon
  (no file)                     draw no weapon; nothing breaks
  ```

  **Eight files give every weapon in the game a look**, and each tier prefab peels one rung off with no
  code change — the same shape that let `mob_animal` peel the animals off `humanoid`.
  🔑 The derivation is free and invents no taxonomy: **`WeaponType` + `IsMagicWeapon` maps exactly onto
  those eight keys** (Blunt+magic = wand, TwoHandedBlunt+magic = staff), and `ItemLevel` is the tier.

  **Rarity = a tint on whatever prefab answered** — your call, and correct. Two practical notes:
  - ⚠ **Prefer GLOW (emission) to hue.** Hue-shifting a textured mesh goes muddy; emission reads at
    phone size and does not fight the texture. Rarity is a 6-rung ladder and the drop copies populate
    all of it, so it is a natural intensity ramp.
  - 🔴 **It must go through a `MaterialPropertyBlock`.** `renderer.material.color` instantiates and
    mutates the shared asset — every sword in the world changes and batching dies. Same wall
    `SetOpacity` hit with models (the stealth-fade gap above).

  **Cost:**
  - ✅ **Free: the hand socket.** `GetBoneTransform(HumanBodyBones.RightHand)` on a Humanoid rig, no
    per-model setup — the second dividend of the rig choice.
  - ⚠ **Authoring, not code:** each mesh needs a consistent **grip origin and orientation** — the same
    class of decision as the family height in `ModelSetup`, and the same thing no script can decide.
    Duals need two sockets; a bow sits differently.
  - 🔴 **A protocol bump.** `EntityDto` carries NO equipment today. Four small values on the **spawn**
    DTO only (`EntityLean` untouched), the same shape `Category`/`Role` took in protocol 29.
  - ⚠ **Animation is per-weapon in a real MMO** and we have one clip set, so a sword swing will look
    right and a bow draw will not. Later problem, not a blocker.

  🔵 **TIMING — my recommendation, not a ruling:** there are **zero weapon meshes in the repo**, so like
  `BL-102` nothing draws until art exists. **Bundle this with the race bodies** — one protocol bump and
  one APK, instead of a reinstall now for a feature that draws nothing. The key shape above is all you
  need to start naming files against.

- `BL-104` 🔵 **THE WARRIOR'S SWORD-vs-BLUNT SPLIT — RULED BY YOU, NOTHING TO ATTACH IT TO YET.**
  Your ruling, 2026-08-29: *"the aoe warriors to be a 2h blunt while mele warriors to use 2h swords …
  we don't want an aoe warrior going with mace+shield and being even more hard to kill"*. This is the
  answer `classes_skills_csv/README.md` asked for when it flagged the split as *"new and nothing
  enforces it — say if it is meant as a rule"*. **It is a rule.**

  ✅ **The MECHANISM is built** (0.101.0, `WeaponHands` + `RequiredHands`) and the mace+shield half of
  your worry was already covered: Two-Hand Mastery has been two-handed-only since it was written, so a
  warrior who picks up a mace and shield already loses the entire passive.

  🔴 **What is NOT built is the split itself, because there is nothing to gate.** The 2nd-class warrior
  is ONE class and correctly takes either 2H type; the split lives at 3rd, and **there is no warrior
  3rd-class kit in the game** — `warrior 3rd.csv` and `war_aoe 3rd.csv` are both empty. So this entry
  is a **standing instruction for the day those files land**: every melee-warrior discipline passive is
  authored `AnySword + Hands.Two`, every AoE-warrior one `AnyBlunt + Hands.Two`. One line each, at the
  `WeaponMasteryProfile`. ⚠ Do not pre-invent the kit to have somewhere to put it (`BL-02`).

- `BL-106` ❓ **YOUR CROSS-CHAIN ID RULE — MEASURED. The masteries already obey it; six ids do not, and
  all six are the same class. Your call on each.** Your rule, 2026-08-29:

  > *"A chain of classes (fighter/mage) should replace their weaker skills with newer or continuing the
  > line .. but cross chain should have different id's … `mage_weap_mastery -> spellcaster_weap_mastery
  > -> buffer_weapon_mastery` and the other is `fight_weap_mastery -> war_weapon_mastery ->
  > swordmaster_weap_mastery`."*

  ✅ **It is now CHECKABLE**: `dotnet run --project tools/SkillCsvSeed -- --chain-audit`. It walks only
  the classes that really exist and reports ids learned by both chains, `Replaces` that cross chains,
  and defs whose declared `Class` disagrees with who is taught them.

  ✅ **THE GOOD NEWS FIRST — the weapon and armor masteries already read exactly as you describe.**
  Fighter chain: `fighter_weapon_mastery` → `tank_` / `warrior_` / `rogue_weapon_mastery`, with
  `fighter_armor_mastery` → `tank_` / `warrior_` / `rogue_armor_mastery`. Mage chain is separate
  throughout. **No mastery id is shared between the chains, and NO `Replaces` crosses a chain (zero).**

  🔵 **1. Two mage ids do not NAME their chain**, which is the half of your rule that is about reading
  the id rather than about collisions: **`weapon_mastery`** (the Mage's, `Skills.Mage.cs`) and
  **`armor_mastery`** (the Mage/Healer's). In your scheme they would be `mage_weap_mastery` and
  `mage_armor_mastery`. Nothing is broken today — the fighter's are prefixed, so they cannot collide —
  but a bare `weapon_mastery` is the exact ambiguity you are legislating against.

  🔵 **2. Six ids ARE learned by both chains, and every one of them is the WARCHANTER** — which makes
  sense, since the buffer is the mage that borrows from the fighter:

  | id | also learned by | what it is |
  | --- | --- | --- |
  | `tank_shield_mastery` | Tank, Bulwark | the Human buffer's Shield Mastery **is** the tank's skill |
  | `hp_boost` | Warrior, Ravager, Warlord | shared HP ladder |
  | `swap_atk_con` · `swap_atk_dex` · `swap_con_atk` · `swap_dex_atk` | most fighter classes | the ATK/CON/DEX stat swaps |

  ⚠ **26 more ids are shared and are NOT violations** — `shared 4th` (your own ALL-CLASSES block) plus
  the eighteen Sigils, which every ascended class learns on purpose. The audit separates them by a
  derived test, not a hand list, so a new sharer cannot hide among them.

  🔴 **THE COST, so you can price the decision: a rename here is NOT free like the class renames were.**
  A skill id is persisted in a character's learned set. Giving the buffer its own
  `buffer_shield_mastery` means minting a new id AND migrating every save that holds the old one —
  otherwise the skill silently disappears from those characters. That is why I have not done it.
  **Three options, pick per row:** (a) leave it — it genuinely IS the same skill, and sharing the id is
  honest; (b) rename with a load-time migration; (c) rename only the two bare mage ids (1), which is
  the cheapest and buys most of the readability.

  ⚠ **3. Thirty-seven defs declare the wrong `Class`** — e.g. every Sigil is `BaseClass.Fighter` but
  taught to both, `magic_proficiency` is Mage but taught to fighters. That field is not persisted, so
  fixing it is cheap; say the word and it goes in a sweep. It is listed by the same tool.


---

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

### `BL-157` 🔵 The worm — a debuffer/nuker class whose identity is polymorph

Your aside, 2026-09-03, while ruling the silences: *"We can later author a debhffer nuke class that
fight with making the enemy a worm ... So it can have a full silence as well."*

A seed, recorded so it is not lost — **nothing is being built and nothing is invented around it.** What
it says on its own: a caster whose kit is *transformation* rather than damage, carrying a single-skill
**full silence** (`BL-155`) as one of its tools. It would be the first polymorph in the game; the
nearest thing built is charm (`BL-110`), which already owns "the target is not yours to control any
more" and would be the engine to extend.

⚠ **It is a CLASS, so it is `BL-02` territory and blocked by the same thing** — the roster is EIGHT
choosable paths per race and 24 third classes, and both `Tempest` and `Vanguard` were retired to keep it
that size (`BL-97`). A new discipline either takes a free slot or replaces one, and that is your call,
not a design detail. Say where it sits before anything is drawn.


### `BL-163` 🔴 The buffer's shelf as an EXTERNAL table — no wrappers, editable without a build

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
