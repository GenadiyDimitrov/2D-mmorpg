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

1. **Open only, and sorted by id.** `BL-02` first, `BL-179` last, no categories to hunt through — the
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

★ **The ones you named most recently (2026-09-16, the playtest):** ten asks, `BL-238`…`BL-247`.
The twelve BUGS from the same pass are in [testing/Open-Checklist.md](testing/Open-Checklist.md) §100,
not here — and **nine of those thirteen are fixed** (0.146.1 / 0.147.0 / 0.148.0).

✅✅ **`BL-238` IS BUILT AND ARCHIVED (0.149.0).** You answered all three of its questions, two of them
by **editing files rather than writing a sentence**: **F2** (*"F1 Is Declined"*, on the page),
**reading B at −10%** (you retitled §3 and put *"Decrease movement speed with 10%"* on every Mark row
of `healer 4th.csv`), and **yes, the Harmony Mark takes it** (both `buffer 4th.csv` rows). It measures
onto your own hand table, row for row. 🔴 The `+20% move speed` the Marks used to GRANT went with it —
no CSV row ever authored it, and it was much of why everyone was over 200.

✅✅ **AND `BL-248` IS CLOSED — YOU DECLINED ALL THREE LEVERS (2026-09-16, archived).** *"the rogues
have enough sprint to outrun anyone, thats why is BL-249 .. a dash potion is a escape from a situation
.. not outruning the fastest classes in game .. so do not do any of the .1,.2,.3 -> we leave speed as
is (after the marks update)"*. **Move speed is settled** — nothing further is owed on it.

✅✅ **AND FOUR OF THE EIGHT ARE BUILT AND ARCHIVED (2026-09-17, 0.156.0)** — the cheap half, taken
first so the expensive ones land on their own: **`BL-242`** (the sell list shows the enchant, and the
attributes), **`BL-243`** (mana potions per rarity — built as the LADDER, not the dropdown, and the
window did not grow: the Potions tab is two columns now), **`BL-244`** (the fast button cycles
DEL:OFF → DEL:ON → BRAKE:ON) and **`BL-245`** (the crafter sees the keeper's shelf — and **spends**
from it, bag first, which was the open question in the entry).

✅ **AND `BL-239` IS BUILT AND ARCHIVED (2026-09-17, 0.157.0)** — the item lock, on the **def id**, so
the stack you lock stays locked after it is drunk empty and re-looted. It took `BL-244`'s open clause
with it: a locked row loses the bag's fast DEL/BRK button entirely, that being the one control in the
game with no confirmation behind it. ⚠ **A `game.db` delete** — the locks are a new column.

✅ **AND `BL-240` WITH IT (0.158.0)** — instant sale, scoped by the tab and the rarity and nothing
else. It moved `ItemCategory` out of the Unity client into `Game.Shared`, because the sweep runs on the
server and has to mean exactly what the tab you are looking at means.

✅ **AND `BL-241` WITH THEM (0.159.0)** — the per-type pickup rarity filter. Built as the loot-rule
change your bracket asked for: a filtered player leaves the party's LOOT ROSTER for that drop, so the
item goes to somebody who wants it rather than being destroyed. ⚠ **A `game.db` delete** — a new column.

✅ **AND `BL-246` CLOSES THE RUN (0.160.0)** — the character sheet is two tabs, to your layout row for
row: BASIC shows the LAST class, DETAILS the whole chain. ⚠ Only **four** of its rows really had no
source, not the seven the entry guessed at — the rest were already on the wire with nowhere to be
drawn. **All ten of the 2026-09-16 asks are now built.**

✅✅ **AND `BL-247` IS BUILT AND ARCHIVED (0.151.0)** — you took the recommendation (*"fill the gap with
the elits+boss, and fix the blueprints to take the rates multiplier"*). Four new elite camps (68 / 72 /
75 / 78), a new level-78 field boss in **Wyrmfall Basin**, and the recipe roll finally goes through the
rate knobs — it was a raw roll that no multiplier reached, which is why your ×100 never touched it.
🔴 **Two leftovers earned their own ids**: **`BL-253`** (your drop database — the measuring half ships
with it as `--drops`, the in-game window is owed) and **`BL-254`** (Rare Wood drops from nothing
anywhere, and filling the 66-79 hole does not change that — it is structural, and it is a question).

✅ **`BL-254` IS ANSWERED AND BUILT (2026-09-17, 0.167.0)** — option (c): *"make wood drop as lether ..
primary/secondary -> animals: leather/wood, plants: wood/leather"*. The two categories mirror each other
instead of sharing one line, Rare Wood has five sources, and all five material types are now somebody's
PRIMARY — which is what the entry's wider warning asked for. **`BL-259` is answered too**: the four
Warlord modifiers stay at `x1` (*"leave them as u made them -> playtest will show"*), and the rule
around them narrowed — an unpriced debuff ships at the DEFAULT rather than blocking on you.

★ **And from your notes file, same day:** **`BL-249`** (dash potions to a 90-second reuse) — **BUILT
in 0.150.0 and archived**. **`BL-251`**, Evasion Mastery removed from every rogue discipline, is
**BUILT in 0.150.0** too (archived — the twin of the warrior's `BL-201`), and **`BL-252`** — a new
subclass born at 40 with a 1-day rune instead of at level 1 — is **BUILT in 0.154.0 and archived**.

🟡 **`BL-250` — THE SERVER HALF IS BUILT (0.155.0). Everything you ruled is in.** Platinum exists (`BL-257`, 0.153.0), the bought slots cost 500kk · 5kkk · 100 plat · 1,000 plat, **a swap below 75 is FREE**, and **the 5,000-platinum rung is cut until the summoner ships** — seven rungs for seven reachable subclasses. Your second pass
on it added the slot ladder (three earned, five bought, as consumable **tickets**), the rule that a
subclass below 75 can be swapped out, the class-master NPC that hands them out, and the panel that
says what a subclass will give you before you commit. **Every decision it was waiting on is answered.**
The sigil half of it is unchanged and still settled. ✅ Its prerequisite `BL-252` (a sub is born at 40)
is BUILT, in 0.154.0. 🔵 **What is LEFT is the CLIENT (the dialogue + the info panel, an APK) and the
SIGIL half (§1-§4).** ❓ And one NEW question came out of the build — §9.6, the completeness gate
against a BOUGHT slot. ⚠ 0.155.0 needs a `game.db` delete (two new columns).

✅ **The third thing in that file needed no build.** You asked whether the Mark's cut *"sits in the
buff part or debuff part of the formula — if it's in the debuff part a 10% decrease is good … if it's
in the buff part it should be −15%"*. **It is in the debuff part**, so **10% stands and no CSV wording
changes**: `Entity.EffectiveSpeed` reads `ModifiedStat(base, BuffMoveSpeed) * (1 − slow) * (1 −
BuffSpeedPenaltyFraction)`, i.e. `(base × buffs + flat) × 0.90` — the cut is a trailing factor applied
*after* the flat shelf, exactly your `(114 + 61) × 0.9 = 157.5`, and the built game measures 158 on
that row. Recorded at the foot of [balance/MoveSpeedOrderings.md](balance/MoveSpeedOrderings.md).

★ **The ones before those (2026-09-13, second message):** the CC-resist formula.
✅ **BUILT (0.139.0)** as **`BL-225`** — control resistances **COMPOUND** now, every source its own
`(1−r)` factor, so your harmony 20% + buff 20% + passive 20% is **×0.512** and with an epic set
**×0.369**, your *"~3 times less"*. The three 0.8 clamps are deleted for the same reason you deleted
the reuse one in 0.136.0. ✅ **And you were right about the Marks** — Harmony Mark carries no control
resistance at all, so choosing it costs you the Holy/Life Mark's grant outright; both cases are
measured rows now. (One correction: the SPT Mark is 15%, not 10% — 10% is the CON one.)

✅ **AND THE LAST TWO RULINGS ARE IN TOO** — **`BL-226`** (0.140.0) took Fortitude's CON resistance to
your **35%** at the top, which needed the whole ladder re-spread because rung 4 was already above the
new ceiling, and which turned up a levelling CLIFF shipped in 0.138.0 (Clarity 50% vs the group's 20%,
so SPT resistance would have DROPPED at 74). **`BL-227`** (0.141.0) makes **magic resistance also
resist magic debuffs**, passives included, as you ruled — the Nullblade's Magical Armor is now a real
ten-second control window (22.4% → 9.0%). ⏸ The boss-jewel idea is filed as **`BL-228`**, future
content, not scheduled.

🟢 **`BL-218` needs nothing from you now** — every ruling in it is built and both schools land inside
your 15-25% band. It stays listed only so the next playtest has somewhere to disagree.

★ **Earlier the same day:** four asks, **ALL BUILT (0.138.0)**, in the archive — **`BL-224`** (Arrow
Barrage: the `[Double]` off and power 2,500 → 2,000; the double was TEN rolls, one per arrow, which is
why the volley ran hot while you never saw an arrow crit), **`BL-222`** (traps could only ever see
MOBS — a TODO from before PvP shipped; they now ask the ordinary attack question as you, with the
toggle captured when you ARMED it), **`BL-221`** (Magical Armor 30% → 50%) and your **20% SPT
ruling**. ⚠ Also **`BL-223`**: the balance rig had been dressing every "buffed" character in **four
Marks and sixteen harmonies** since 0.113.0, so every `--buffed` table it printed was too high.
⚠ **NEW APK.**

★ **The ones before those (2026-09-12, the second message of the playtest):** six asks.
✅ **THREE BUILT (0.137.0)**, in the archive — **`BL-219`** (the target window: positive effects gone,
debuffs abbreviated, and the stack counter now updates the instant a stab banks rather than on a
1-second beat — that beat was your *"3-6-9-10 … at random"*), **`BL-220`** (DoT/HoT out of the combat
chat, with a Settings toggle) and the first rig fix inside `BL-218`. The sixth (duals vs mage,
*"mage does good amount of dmg now"*) was an observation with nothing owed.

★ **The ones before those (2026-09-06):** three asks in one message, filed as
**`BL-180`…`BL-182`** and **all three BUILT (0.115.0)** — they are in the archive. `BL-180` the admin
`Functions > [Buffs]` drill-down (four derived drawers — 31 singles, 9 groups, 14 harmonies, 4 marks —
and the four Mark buttons off the Functions tab, as you asked); `BL-181` `FullHeal` / `/heal`, both
pools to full instantly and in combat; `BL-182` `/god` and `/invis` surviving a relog. ⚠ **NEW APK**,
and 🔴 **a `game.db` delete** — `BL-182` adds two columns.

★ **And one more the same day, `BL-183`, BUILT (0.115.1)** — also in the archive. Your ruling that a
class harmony is the GROUP over the Spirit Helper's eight single harmonies and replaces them. It was
already written that way and had never worked once: the rule was expressed with a field the engine
matches by buff KEY while it held skill IDs, so the two tiers had been stacking in silence since
`BL-160` shipped. Both directions hold now, rung by rung, and the covering ladder is printed by
`--buffs` so it can never go quietly dead again. No schema change, no new APK.

★ **And `BL-184`, BUILT (0.115.1)** — the Clear All you asked for, in both places: `Functions > CLEAR
ALL BUFFS` and a free `Clear all blessings` row at the Spirit Helper (it asks first — it is the one row
there that destroys blessings you may have paid 50k each for). Debuffs, DoT stack counters and rune
buffs survive; toggles do not. `/clearbuffs [name]` is the command behind both. ⚠ **NEW APK.**

★ **The ones before those (2026-09-05):** nine asks across two messages, filed as
**`BL-172`…`BL-179`**. ✅ **SIX ARE BUILT (0.114.0, 2026-09-06)** and are in the archive — `BL-173`
(`/return` at 60s/10s, plus the chat alias), `BL-174` (the return/resurrection faucet off ordinary
mobs), `BL-175` (the `[Bosses]` teleport page and three kinds of clutter out of `Spawn zones`),
`BL-176` (enchant / attribute / potions / stones as sub-pages — which turned up the Holy and Physical
stones being unreachable from the admin menu at all), `BL-177` (SP 10kk) and `BL-178` (the chat box
alone keeps Android's native input, so its copy/paste menu is back). ⚠ **NEW APK.**
**Two are left, and you asked for both to wait:** **`BL-172`** 🔴 `/unstuck` — ready, a **rooted 180s
channel cast in town** (not a background timer, not an escape), and **`BL-179`** 🔵, which needs one
choice of yours: `test_phys`/`test_magic` are granted to **every character in the game** from a block
labelled `TEST ONLY — DELETE ME`.
· ✅ **THE TANK PASS IS BUILT (0.112.0, 2026-09-04).** All 205 rows
of `tank 4th.csv`, which closes `BL-154` (pull), `BL-155` (silence) — both in the archive — and the last
`NOT DONE` file in `BL-02`. Fourteen ladders continue past 74; six things are new (Magic Wall, Tauting
Wall, the Perfect Whisp, a race-split three-rung Backlash, Whisp Mastery's third slot, Silencing Shock).
`SkillCsvSeed --check` is green on all fifteen walked files. **⚠ NEW APK — the client builds its Learn
tab locally**, and 🔴 **a `game.db` delete** (Backlash stopped being auto-granted). 🔵 **What it leaves
you is `BL-165`** — the two AoE pull shapes (yours to do later) and one clamp.
· ✅ `BL-158`…`BL-162` — the NPC BUFFER pass is BUILT (0.111.0) and is in the archive: the shelf levels up
with you, the 75 ceiling is gone, eight single harmonies and the three Marks are on sale, and Swift
joined the Mage preset. See it with `dotnet run --project tools/BalanceMatrix -- --npcshelf`
· **`BL-163`** (your
shape for the buffer shelf: an external `(shelfId, minLvl, rungId, price)` file so *"a pvp server won't
require new npc just change of id's"* — a refactor for editability, nothing is broken) · **`BL-164`**
(the Marks' rank tie, found while building `BL-161` — your call between three fixes) · `BL-156` (debuff
duration — **BUILT and CLOSED**, in the archive) · `BL-157` (the worm, a seed) ·
`BL-93` (the visuals conversation, yours to start) · `BL-102` (blocked on one file from you) ·
`BL-02` (the 40+ kits, blocked on your CSVs).

---

## Index — 49 open entries

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
| `BL-157` | 🔵 | The worm — a polymorph debuffer/nuker class, a seed only | classes |
| `BL-163` | 🔴 | The buffer shelf as an EXTERNAL table — no wrappers, editable without a build | classes |
| `BL-164` | 🔵 | The three Marks share one Rank, so the weaker rung can out-hold the stronger | classes |
| `BL-165` | 🔵 | What the tank's 4th tier LEFT OPEN — the two AoE pulls (yours), and one clamp | combat |
| `BL-170` | 🔵 | THE CLIFF AT 80 — party dps triples across the S-grade flip; three ways out, your pick | combat |
| `BL-171` | 🔵 | THE WORLD BOSS — stats built; the encounter, mass-PvP rules and loot are owed | combat |
| `BL-172` | 🔴 | `/unstuck <name>` — 180s rooted channel, cast in town, on another char of the same account | systems |
| `BL-179` | 🔵 | The two TEST skills are granted to EVERY character — three ways to gate them, your pick | systems |
| `BL-185` | 🔵 | THE DAMAGE REWORK — ✅ the SHOT (runes x2) and the DEFENCE SHAPE built 0.117.0; the armour spread + the x1.17 residual are open | combat |
| `BL-186` | ⏸ | THE MAX LEVEL CAP — can it be removed? POSTPONED on your call 2026-09-10; not the next thing | systems |
| `BL-189` | 🔵 | Weapon-type protection — `BowResist` generalised to every weapon type | combat |
| `BL-202` | 🔵 | THE WARRIOR'S DAMAGE + CONTROL SKILLS — the half of both 3rd kits still owed | classes |
| `BL-208` | ❓ | ONE cosmetic cell left from the Magus's 4th kit — three of four closed the same day | classes |
| `BL-213` | 🟡 | The mastery roster, rewritten to your table — BUILT; two display names and four learn levels are mine | classes |
| `BL-215` | 🔵 | THE MAGE'S DAMAGE — two levers pulled (≈×2.3 since 0.132.0); the crit-rate CAP is the third | combat |
| `BL-218` | 🟢 | DEBUFF LAND RATES — all rulings built; nothing owed unless a playtest says so | combat |
| `BL-228` | ⏸ | FUTURE — boss jewels that trade one school of control against another | items |
| `BL-229` | 🔵 | THE 74→76 DEBUFF CLIFF — an un-ascended caster keeps casting the @74 rung while the world levels past it | combat |
| `BL-230` | 🔵 | CONTROL RESISTANCE IS NOT ON THE NPC SHELF — part 1 only; part 2 built in 0.142.0 | buffs |
| `BL-231` | 🟢 | THE LANDING-MODIFIER SCHEMA — superseded by `BL-232`; the five-bucket version was rejected | combat |
| `BL-232` | 🔵 | `debuff_landmods.csv` IS LIVE — 74 rows built and checked; the SUCCESS column is yours to author | combat |
| `BL-233` | ❓ | THE DEMON BUFFER'S P.DEF — measured three ways and heavy is AHEAD; I need your two sheets | classes |
| `BL-234` | ❓ | URGENT LESSER HEAL — built to your four numbers; the per-rank falloff is mine to confirm | classes |
| `BL-250` | 🟡 | THE SUBCLASS SYSTEM — **the server half is BUILT (0.155.0)**; what is left is the CLIENT dialogue + panel (APK) and the SIGIL half (§1-§4). ❓ one new question in §9.6 | classes |
| `BL-253` | 🔵 | A DROP DATABASE — name an item, see every source; the BalanceMatrix half is built, the in-game window is owed | items |
| `BL-260` | ❓ | **SUMMONERS — the conversation we have never had**, and four shipped decisions already lean on it | classes |

---

## The entries

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier)** — ✅ **FIVE OF THE AUTHORED FILES ARE DONE — the BULWARK (tank) landed 2026-09-02 (0.105.0), the fourth finished 3rd class.** Race decides four of its tools, which is the first time race has decided anything about a class here: Human taunt/mass-taunt, Elf charm/freeze, Demon taunt/intimidate, and the two Shield Smashes split Human;Elf vs Demon. ⚠ THE PASS SPANNED ALL THREE TANK FILES — `tank 2nd.csv` was retuned in the same breath (taunt 3s→1.5s at 0 MP, Charm at 24, Shield Shock replacing Shield Stun, Stay! moved to the 3rd, Defensive Wall's ×2 terms deleted). ✅ **`tank 2nd`/`3rd`/`4th` are ALL FINISHED — he said so 2026-09-04 (*"im done with tank 2/3/4 so its ready to build after the npc buffer"*) and it is verified: the `NOT DONE` banner is gone, 205 authored rows, and Grapple + Numbing Shock are laddered 76→90 with real numbers. ✅✅ **AND IT IS BUILT — 0.112.0, 2026-09-04. `tank 4th.csv` is the SIXTH authored file done and the LAST `NOT DONE` file in this entry; `tank 4th` earned its `Check.Specs` line the same day and all fifteen walked files are green.** What it left open is `BL-165`, not this entry. Eleven slips across his tank files have now been caught and corrected on BOTH sides — six in the 3rd-tier pass, five in the 4th — see the CHANGELOG. Older note follows. The
  **Lightbringer (healer) shipped in 0.74.0**, the **whole Warchanter (buffer) in 0.76.0**, the
  **Lightbringer's 4th tier in 0.85.0** (with the shared kit and the eighteen Sigils), and the
  **NUKER's 3rd tier in 0.87.0** — 208 rows, 21 families, Magus and Tempest, all three races, 40 to 74.
  `SkillCsvSeed --check` is green on all twelve walked files. That is the proof the pipeline works end
  to end, four times over.

  ⚠ **The nuker one is the lesson worth keeping: `nuker 3rd.csv` had been FINISHED since before the
  healer's was, and nobody noticed for six days** — it was never added to `Check.Specs`, so the one tool
  that would have shouted about it never opened the file. **A finished file that no spec walks is
  invisible.** When you finish a file, say so, and its `Check.Specs` line goes in the same day.

  What is left, and it is now a short list — **the six authored files are ALL built:** healer 3rd + 4th,
  buffer 3rd + 4th, nuker 3rd, and tank 2nd/3rd/4th. What is missing is what you have not written:
  - ✅ ~~`warrior 3rd` / `warrior 4th`~~ — **DONE, built 2026-09-16 in 0.146.0 (`BL-237`).** The
    RAVAGER's whole kit, both tiers, race by race, and `warrior 4th` earned its `Check.Specs` line the
    same day. It is the SEVENTH and EIGHTH authored file built.
  - ✅ ~~`war_aoe 3rd` / `war_aoe 4th`~~ — **DONE, built 2026-09-17 in 0.164.0-0.166.0 (`BL-237`).**
    The WARLORD's whole kit, both tiers, race by race; `war_aoe 4th` earned its `Check.Specs` line the
    same day. The NINTH and TENTH authored file built, and with them `war_sundering_blow` — the last
    derived fighter ladder in the game — is retired. **Every authored warrior file is now mirrored.**
  - 🔵 **The rogue's two are still two-line placeholders** — `dual 3rd`/`4th` and `archer` were built
    from his files in 0.119/0.120; what is left on this bullet is `nuker 4th`.
    Same rule: nothing invented in the meantime, and each earns its `Check.Specs` line the day you
    finish it.
  - ✅ ~~`buffer 4th.csv`~~ — DONE, built by `BL-108` in 0.103.0. Harmony Mark shares `MarkKey` with the
    healer's three, so a healer's Mark and a buffer's can never stack.
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
  - 🔔 **THIS IS THE REMINDER YOU ASKED FOR, AND IT HAS NOW FIRED ONCE AND GONE UNANSWERED.** You
    wrote `warrior 3rd.csv` and `warrior 4th.csv` on 2026-09-14 and neither carries a `precision` or an
    `anti_magic` row — so 0.146.0 built both files without them and both stayed auto-granted floors.
    They are still yours to author, and the next file to open is `war_aoe`. When you open a warrior or
    rogue file, `precision` and `anti_magic` want rows in it. ⚠ **A class-skill-TABLE change needs a new APK** — the client builds
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

### `BL-165` 🔵 What the tank's 4th tier left open — the two AoE pulls, and one clamp

Everything in `tank 4th.csv` is built (0.112.0-0.112.1, 2026-09-04). What is left is **two things**, both
of which you have said are yours — kept together so they are one read rather than two.
**Nothing here is blocked and nothing is broken**; the tank is complete and playable as it stands.

**1. ✅ CLOSED — Shield Smash - Power's ladder.** Its crit-damage column restarted at 15% against 35%
at level 74; the code refused it and shipped flat at **35% / 15%**, matching its Rate twin. You ruled
on it the same day (*"fix all the csv to match what you told me needed fixing"*), so the eight cells
now agree and there is nothing left to decide. ✅ **So are the other five** — the id paste, the AOE
cell, the WEIGHT paste, the Cyrillic `к` and the `mpReg` column, whose origin you named yourself
(`healer 4th.csv` carries `mpReg +3.4`; the mage's number stayed in the tank's file) — and which you
then corrected further: the tank's MP regen is **additive**, not multiplicative, on all three tiers.

**2. ✅ CLOSED — the three whisp ladders end at 90.** They read 91 for one commit; you named it a typo
(*"the intelisence of vsCode … make it go by 2 from 89lvl and I missed it"*) and both sides now say 90.

**2b. ✅ CLOSED — the unauthored ×1.1 MP regen is gone.** The four rungs below level 36 granted an MP
regen no `tank 2nd.csv` row mentions; you ruled it out the same day (*"remove the x1.1 mp regen from
tank 20~32, at 36 he jumps to +3.1 directly"*). The tank's MP-regen column is now entirely yours.
**3. 🔵 THE TWO AoE PULL SHAPES ARE STILL NOT AUTHORED.** `BL-154` rule 4 gave the pull two more shapes
— a ranged one taking the target plus 2-4 around IT, and a self-centred one taking 2-5 around the
CASTER — and the **engine serves both today**: `TargetMode.EnemiesInRadius` with `AreaAtTarget` picking
the centre and `MaxTargets` as your cap of five. Your finished file authors ONE pull, so one pull is
what exists. They need rows and nothing else.

**4. 🟡 THE DRAG-SMOOTHING CLAMP IS STILL UNTESTED AND UNBUILT** — your instruction, 2026-09-04: *"mark
the one clamp / EntityView.Update as untested and I'll see it in game first then decide"*. The client
sizes each interpolation segment by the measured gap between the last two updates, so a mob that stood
still for ten seconds and is then grappled has a ten-second first segment and its opening ~100ms draw
almost frozen. **The test is to grapple something that has been standing STILL**; if the body hangs for
a blink before it slides, that is this, and if you cannot see it, it does not need fixing.

✅ **AND THE WEAPON MASTERY IS IN** — you spotted its absence the same day (*"I have forgotten the
weapon mastery"*) and its fifteen rungs shipped in 0.112.1: P.Atk +90→+200, ×1.085 kept, and attack
speed +1% / +3% / +5% in your three bands. ⚠ Its FLAT ladder is the straight line between your two
endpoints, rounded (7.857 a rung) — one array if you want a shape instead.

⚠ **And six things were corrected in `tank 4th.csv` itself** — an id paste on eight rows, an AOE cell,
a WEIGHT paste on thirty rows, a Cyrillic `к` in an SP cell, a pasted `mpReg` column, and Shield Smash
- Power's restarting crit-damage ladder. Each is listed with its reasoning in the 0.112.0 and 0.112.1
[CHANGELOG](CHANGELOG.md) entries, and each is one edit to reverse. They are corrections, not retunes:
none of them changes a number you chose.

---

### `BL-170` 🔵 THE CLIFF AT 80 — party dps triples across the S-grade flip, and every endgame number rides on it

Found while building `BL-167`, and it is the one thing that pass left open. Measured with `BL-169`'s
honest party (buffed Bulwark tank, buffed Lightbringer, three buffed DDs):

```
 Lvl  gear   party dps
  44   t40         370
  60   t52         558
  65   t61         525
  76   t76         534      <- flat from 44 to 76
  85   t80       1 884      <- x3.5 in one step
  90   t80       1 752
```

**Party damage is flat at ~370-560 for thirty-two levels and then triples in one gear tier.** That is
not a boss problem and it cannot be fixed on the boss side: your ×10 HP lands the endgame exactly where
you want it (3.43 kk at 90, 30-33 minutes escorted) and simultaneously puts a **level-44 dungeon boss at
109 minutes** for a full party, 219 solo. No smooth boss-HP curve can reconcile those, because the thing
that is not smooth is the PARTY.

⚠ It also crosses your own *"not one shooting"* line at the bottom: with the new ×2 boss attack, a
single basic hit takes **91-95% of a robe's pool at levels 20-44**, against 22-23% at 85-90.

**Three ways out, and it is yours to pick** (fuller version in
[design/BossRework.md](design/BossRework.md) §6):

1. **Taper the ×10 by level** — ~×3 below 76 rising to ×10 at 85+. One knob, keeps the endgame, fixes
   the bottom. ⚠ It bakes the cliff into the boss curve, so a future gear change inherits it.
2. **Fix the cliff itself.** The S-grade jump is the real outlier and it distorts every endgame number
   in the game — exp pacing, drop value, elite camps — not just bosses. Widest benefit, and it moves
   numbers you have already signed off.
3. **Leave it for now.** The bosses below 80 are the Hollow Crypt lich (44), the world boss (60) and
   the Dread Knight (65). If nobody is fighting those this week it can wait for the bot party you
   want — *"one day we will make a party of bots to help me fight a Boss to see really what happens"* —
   and the whole band gets re-measured at once, honestly, instead of tapered by hand twice.

🔑 **Do not re-derive this one.** `BL-13`'s original band and `BL-169`'s correction were both cases of a
number being reasoned to instead of measured, and both were wrong. Whatever is picked here gets read off
`tools/BalanceMatrix` before and after.

---

### `BL-171` 🔵 THE WORLD BOSS — the encounter, the mass-PvP rules and the loot (the stats are built)

Your definition, 2026-09-05, correcting my reading of the Valley Treant: *"The treat is field boss (same
as dungeon one) world boss is a clan/party of clans mass pvp massacre where the boss is the target ..and
that boss will have about x2~3 aditional stats and x10 additional hp .. So now if boss have 28k p atk/6kk
hp a world one will have 50~60k p atk and 120kk~180kk hp(6kk x2~3 x10) so several parties can fight it
while fighting others for the best loot in the game"*.

✅ **The STAT rung is built (0.113.1)** — `BossProfile.World`, ×2.5 on every stat and ×25 on HP over a
solo boss. At level 90 that is **171.7 kk HP**, inside your 120-180kk, with **3,860 P.Def**.
🔑 It is a FLAG on the profile, not a fourth `MobRank`: fourteen places test `Rank == MobRank.Boss` for
behaviour a world boss wants unchanged (control immunity, the zone-HP exemption, `AutoAttackBoss`, the
raid-level lock, boss judgment, the exp rank, the plate title), so a new rank meant fourteen edits with
a silent bug waiting at any one of them.

🔴 **NOTHING IS AUTHORED WITH IT, and three things have to be decided before anything can be:**

**1. How many parties is "several"? Measured, 172 kk is a very large pool for our damage model.**
One 5-man does **694 dps** against its 3,860 P.Def. So:

| raid | time to kill |
|---|---|
| 1 party (5) | 69 hours |
| 9 parties (45) | **7.6 hours** |
| 34 parties (172) | 2 hours |

Even read generously — much of a mass-PvP fight is spent fighting *people*, not the boss — 7.6 hours
for nine full parties is a long evening. Either the pool comes down, or a world boss is explicitly an
all-server event that runs for most of a day. **Your call, and it is one number.**

**2. Your P.Atk example does not match your HP example, and I used the HP one.** *"28k p atk → 50~60k"*
is ×2 off an **escorted** boss; *"6kk × 2~3 × 10"* is off a **solo** one. I took the solo base for both,
which gives **~128k P.Atk** rather than your 50-60k. On your mythic-S tank that is **9.3% of your pool
per swing** — a real raid number; at your 60k it would be 4.4%, which is *softer than a solo boss
already hits*. I think 128k is the right game and 60k is the arithmetic slip, but say which — it is the
`WorldStatMult` constant and nothing else.

**3. The encounter itself does not exist.** A world boss needs a place (an open field, not a dungeon —
the PvP is the point), the mass-PvP rules around it (does the zone force-flag? does karma apply? how do
several clans contest a kill?), and the loot — *"the best loot in the game"* — which touches `BL-76`'s
boss skill gems and the party loot rules. That is a design pass, not a number.

⚠ **The Valley Treant was mis-filed as the world boss in 0.113.0 and is corrected here.** Its 21-hour
respawn made it *look* like one and I read the respawn as the classification; it is a FIELD boss and is
back on the 20/30-minute enrage ladder. There is now no world boss in the game at all, which is honest —
the tier exists and nothing wears it yet.

---

### `BL-172` 🔴 `/unstuck <name>` — a 180-second rooted channel, cast IN TOWN, on another character of the same account

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

---

### `BL-179` 🔵 The two TEST skills are granted to EVERY character in the game

Your question, 2026-09-05: *"also test skills can they be only owners or added from the admin menu.. Or
they be moved to item that uses them and given from admin menu only?"*

**They are worse than you thought.** `test_phys` and `test_magic` (`SkillCatalog.TestPhysSkill` /
`TestMagicSkill`) are handed to **every character, at any level, unconditionally**, by
`AutoLearnCoreSkills` — inside a block whose own banner reads `==== TEST ONLY — DELETE ME ====`. They
are not owner-gated in any way. They read their damage from the Debug panel's `TestSkillPower` /
`TestSkillMod`, which are **server-global** fields, so on a live server an ordinary player would hold
two castable skills whose power is whatever the owner last typed into a tuning box.

**Three ways you named, and what each really costs:**

| | what it is | verdict |
|---|---|---|
| **owner-only** | `Entity.Role` already exists (`AccountRole`), so the grant becomes one `if` and a `Remove` for everyone else | ~2 lines, server-only, **no APK** |
| **from the admin menu** | a button in the Debug tab that adds/removes the two ids on demand | the above, plus one command + one button |
| **an item that casts it** | the Scroll of Return pattern — the item's skill is invoked on use and never learned | most work, and a test skill you want to spam at a dummy is the worst fit for a consumable |

🔴 **My pick: the middle one — role-gate the grant to nothing by default, and put a `Test skills
ON/OFF` toggle in the admin Function tab.** It clears the two rows off every character's list
*including yours*, costs one tap when you actually want them, needs no new item id and no stack to
keep topped up, and leaves the skills fully functional — target, cooldown and the live Flat/Mod tuning
all keep working, which the item route would complicate for no gain.

⚠ **Do not simply delete them.** Both are still wired into the damage path
(`GameLoopService` reads `def.Id == TestPhysSkill` / `TestMagicSkill` when computing the hit) and they
are how the `{Flat, Mod}` curve gets read live — the thing that has settled several balance arguments
here. This entry is about who can reach them, not about removing them.

---

## `BL-185` 🔵 THE DAMAGE REWORK — measured against your own IG matrix

### 🔴 REWRITTEN 2026-09-09 — REFITTED TO REAL DATA. Everything below this block is superseded.

You rejected the generated matrices — *"I'm not sure the table is perfectly edited. It's gemini way
of research"* — and told me to build the reference myself. Two real sources replaced them: **your own
in-game measurements** (`balance/ig-reference-authored-realgame.csv`) and **IG's own skill/item
database**. The full refit is [balance/DamageVsIG.md](balance/DamageVsIG.md), rewritten wholesale.

⚠ **Do not fit anything to `IG-reference.csv` again.** Both generated tables were wrong in opposite
directions, and each produced a confident, wrong proposal from me.

**What the real data says:**

| | verdict |
|---|---|
| formula + `PhysicalK 77` + `MagicK 91` | ✅ correct, confirmed twice over. Not to be touched. |
| weapon catalogue | ✅ **is IG's, verbatim** — bow 323/84, 400/99, 581/132; staff 226/167, 274/193; 2H 282/114. Our level-80 row is ~10% hot; that is the only drift. |
| jewel M.Def | ✅ **is IG's, verbatim** — 95 / 71 / 48. |
| nuke power ladder | ✅ **is IG's, verbatim** — 52/58/65/72/78/82/85/89/92/96… **Magic spell power was never short.** |
| our mage, unbuffed | ✅ **436 vs his 365 at level 76 — we are 1.19x ABOVE.** |
| ~~take the √ off M.Atk~~ | 🔴 **WITHDRAWN.** It rested on a units error of mine: the old page compared IG's *internal* M.Atk to our *shown* one (`min(internal, 20·√internal)`) and invented a 3x gap. |
| ~~cut the M.Def buff legs~~ | 🔴 **WITHDRAWN (2nd time).** Your buff stack moves magic damage ×1.79; ours ×1.71. They already match. |
| ~~rescale the archer/warrior kits~~ | 🔴 **WITHDRAWN.** Your authored `archer 3rd.csv` (Twin Arrows 1000→5000, two arrows) matches IG's real ladder (1110→4870). 0.116.0 was right. |

**🔴 THE ONE REAL GAP — THE PER-CAST SHOT, MEASURED ×2.35.** Taking your five in-game rows, running
your buffed M.Atk / stated M.Def / stated power through our formula and comparing to what you
actually observed: **2.40 / 2.23 / 2.37 / 2.24 / 2.52** at levels 20/40/52/61/76. Flat across 56
levels — one multiplicative term we do not have.

🔑 **Your algebra was right and mine was wrong.** `√(a·b) = √a·√b`, and our `BuffMagAtk` magnitudes
are stored *already square-rooted*, so the Spell Rune's `0.414` really is a ×2 M.Atk shot exactly as
you said. The finding is not that we converted it wrongly — it is that **the real shot is worth
×2.35, so our rune delivers 40% of what it replaced.**

**✅ ANSWERED — the archer's double hit.** Your `archer 3rd.csv` authors Twin Arrows as *"two arrows
each dealing +X power"*, and IG's real archer skill is a two-arrow skill too. **Keep `HitCount: 2`.**

**🔵 WHAT IS STILL OWED, and all three are your call:**

1. **The shot.** ×2.35 per cast is the measured value. Either raise the Spell Rune to deliver it
   (`BuffMagAtk 0.414 → 1.35`) or reinstate a consumable. ⚠ *"no shot"* was your own ruling, so this
   is a decision, not a bug to fix quietly. The physical side is not covered by your file yet.
2. **M.Def ~2x yours** — your mage reads 911 at 76; ours reads 2032 at 90. Wants a matched-level
   measurement before any number moves.
3. **Armour P.Def spread** — ours 3.75x heavy:robe, **IG's 1.47x**. It lives in the Armor Mastery
   ladders, not the base sheets. The only 2026-09-06 finding that survived.

⏸ **Nothing to do on:** the formula, both K constants, the weapon/jewel catalogues, the nuke ladder,
the buff shelf, or the archer kit. All confirmed correct against real data.

---

**2026-09-06.** You said the damage was *"laughable"* — *"a mage with a weapon t80m +16 does to
someone with ~2k Def a 200-400 dmg"*, *"harmonist elf that have 5100 p.atk does to an S grade robe
user 400 with a crit"*, *"We need to fix the dmg ...as general"*, *"The fight should be scary not
potions to overheal the dmg"* — and then supplied a full IG damage matrix **with the stats behind
it** (4 attackers × 4 defenders × 4 gear grades, P.Atk / M.Atk / skill power / P.Def / M.Def /
normal / crit / DPS).

**The full fit is [balance/DamageVsIG.md](balance/DamageVsIG.md); his table is preserved verbatim
there and as [balance/IG-reference.csv](balance/IG-reference.csv). Read it before touching a
constant.** The short version:

🔴 **`PhysicalK = 77` and `MagicK = 91` ARE CORRECT — do not raise them.** An earlier proposal this
same session was 180 / 270; his table kills it. Fitting `77·(pAtk+power)/pDef` to his archer and tank
rows, the K each row demands is **flat across all four defenders at every level** (at 85: 86.7 / 87.0
/ 87.0 / 86.0). Our ratio model and our defence divisor already reproduce his spread. The only drift
is a mild rise with level, 58 → 70 → 76 → 87 across 40/52/76/85 — a **level modifier we don't have**.

**Six changes, in the order they should land** — revised 2026-09-06 after your answers:

| # | change | why, measured |
|---|---|---|
| 1 | **Build the ARCHER and WARRIOR damage kits from the HARMONIST kits, +20%** — archer = elf harmonist skills + bow passives ×1.2; fighter = demon harmonist skills + 2H passives ×1.2 (your recipe) | 🔑 **My original "physical skill power is 10x short" was wrong as a global claim.** Our elf harmonist Sound Burst already hits a buffed mage for **495**; our archer Precise Shot for **235** — the harmonist kits are close to right and the archer/warrior kits are the ones that do not exist. 495 × 1.2 = 594 against IG's 870 @85: inside ~1.5x, not 10x. IG's ladder for reference: archer 1200/2400/6200/10200, fighter 1800/3200/7500/12500, tank 800/1400/3100/5200 at 40/52/76/85. Carries your **+20% on the passives** too. |
| 2 | **Raise base light/robe P.Def toward IG's** | Naked-to-naked (IG @85 vs ours @90): tank 3200 vs **2682 ✅**, but fighter 2400 vs **944** and mage 1600 vs **715** — 2.2-2.5x LOW. **IG's robe→plate spread is 2.0x; ours is 3.75x.** That is the structural cause of the archer's 1:9.2 across tank/fighter/mage, and it lives in the BASE SHEETS, not the shelf. Alone it would REDUCE damage to squishies — it only lands correctly beside #1, whose numerator term is far larger (IG gets 870 on a mage from `5800+10200`; we get 236 from `4494+870`). |
| 3 | **Take the √ off the BASE M.Atk and refit `MagicK`** | 🔴🔑 **CORRECTION — magic PERCENTAGE buffs are ALREADY outside the √.** `Entity.EffectiveMagicAttack` squares them deliberately (`magFactor * magFactor`, *Owner 2026-07-16*) so +32% yields +32%. What is still under the √ is `MagicAttack + magFlat`: the INT base, **the weapon's M.Atk and its enchant**, and flat buffs. **That is the +16 complaint exactly** (+16 staff = internal ×1.27 = damage **×1.13**), and it is the worse half because the base is the only part that GROWS. Naked, the magic attack term (`dmg·mDef/power`) grows **×8.8 for IG across 40→85 and ×2.6 for us across 40→90**. Fitting his 16 mage rows: linear M.Atk needs K to drift only **×1.40** across the grades, where √ needs **×5.1** — and physical drifts ×1.49, so **linear magic and physical want ONE shared level modifier**. Confirming: his table puts mage M.Atk 6500 beside archer P.Atk 5800; under a √ an M.Atk of 6500 contributes 80. |
| 4 | **The shelf's COMPOUNDING (the HP legs) — NOT its M.Def legs** | 🔑 **Your correction:** the IG figures are GEARED, NO BUFFS — and naked our M.Def matches IG on all three classes (1633/1700, 2032/2000, 2261/1850). **"Cut Ward / Harmony of Ward" is WITHDRAWN.** What stands, measured inside our own game and citing nothing external: buffing BOTH sides drops every damage cell 25-40%, because HP (×2.05) and defence (×2.45) MULTIPLY while attack has one multiplier (×1.44) — ~3x survivability against ~1.5x offence. That is compounding, and the HP legs (Body +35%, Harmony of Body +30%) are the cheapest half to remove. |
| 5 | **Crit: multipliers UNCHANGED** — ×1.35 Ferocity, ×1.35 harmony, ×1.2 Mark | Your ruling: *"The crit dmg should be as is now"*. The two missing pieces are the **archer's 700 flat crit damage** and the **+20% passives** (which ride with #1). ⚠ Today's top rung is `RogueWM` critDmg **165**, so 700 is ×4.2 — but know its size: flat crit damage joins pAtk INSIDE the ratio (`StatCalculator.CritFlatFactor`), so at P.Atk 4494 the 700 is **+15.6% on a basic crit**, and once #1 gives the archer's skills a real flat power it falls to about **+5%**. A real lever, not a fix. |
| 6 | *last* — **the level modifier**, if still needed | Your call: *"This depends on the outcome of all others"*. The physical K his rows demand drifts 58 → 70 → 76 → 87 across 40/52/76/85 (×1.49); linear magic drifts ×1.40. One shared modifier would cover both — but only measure it after 1-5. |

🔴 **Your boss worry is right, and it is ASYMMETRIC** — *"I just hope if we fix the formulas the dmg
of bosses that we fixed not to skyrocket"*. Raising PLAYER skill power raises player→boss damage but
**not** boss→player, because bosses mostly basic-attack: bosses get easier without getting more
dangerous. **Every step of this re-runs the `BL-13` boss-pace section in the same pass as
`--dmgmatrix`, and both get reported.** If TTK falls out of your 10-30 minute band, the compensation
goes on the BOSS (its HP, or its own skill powers) — never on the formula.

⚠ **`StatCalculator.MagicDamage` is shared with HEALS and with mob casters.** #3 refits all three or none.
⚠ **The squaring in `EffectiveMagicAttack` exists ONLY to cancel the √.** Take the √ out without
taking the squaring out and every magic buff pays 1.725² = **×2.98**. Same edit, both.

**Not part of this entry, but it will move when these land:** the S-grade craft budget. A measured
global, so re-measure `--dmgmatrix` and the M-sections together before assuming it holds.

### ✅ STEP 1 IS BUILT (2026-09-06) — the rest is still open

Your recipe, verbatim: *"For archer take the elf harmonist skills and bow passives ... Increase them
with ~20%"* / *"For fighter kit take demon harmonist skills and 2h wepon passives increase them by
~20%"*. Built at **×1.25**, the midpoint of your 20~30%, in `Skills.FighterKits3rd.cs` — every number
is a source ladder times that factor, nothing is invented, and each one names its source.

| | warrior (Ravager / Warlord) | archer (Sharpshooter / Hunter / Trapper) |
|---|---|---|
| weapon line | **Two-Handed Sword Mastery** — Warlock Weapon Mastery ×1.25 (P.Atk 38→125, +3 acc), 2H SWORD | **Archer Bow Mastery** — Harmonist Bow Mastery ×1.25 (P.Atk 125→750), +400 range unscaled |
| damage skill | **Sundering Blow** — Sound Smash's 13 rungs ×1.25 (power 1250→5000), 2H sword | **Split Volley** — Sound Burst's 13 rungs ×1.25, 900 range, **2 hits** |
| armour | rungs 6-20 of the warrior's own Armor Mastery = the tank's heavy ladder **minus the crit-damage reduction**, exactly as you said | rungs 6-20 of the rogue's own Armor Mastery = **half the tank's P.Def ladder** (32→86 flat, ×1.055→×1.075), your pick |
| extras | — | **Bow Expertise** (+12%, the harmonist's rung, cloned to Fighter) · **Killing Focus** (+20% crit damage, +700 flat) |

**Measured, level 90, mythic, both sides buffed** — archer P.Atk **4494 → 6647**, warrior **4256 → 4615**:

| | before | after | your target |
|---|---|---|---|
| archer skill crit on a mage, **per arrow** | 618 | **1513** | 1500 |
| archer skill crit on a mage, **per use (2 arrows)** | 618 | **3025** | 1500 ⚠ |
| archer skill crit on a fighter, per use | 468 | 1778 | 1000 |
| warrior skill crit on a mage | 524 | **1088** | 700-1500 ✅ |

🔴 **THE ARCHER LANDS AT DOUBLE YOUR NUMBER, and it is your recipe doing it, not a slip.** Sound Burst
carries `HitCount: 2` — two independent resolutions, each rolling its own crit — so Split Volley
inherits it. Per arrow it is 1513 against your 1500; per press it is 3025. Your ×1.2 over the
harmonist is applied exactly (harmonist per use 2525 → archer 3025). **Your call:** keep it (the
archer is then genuinely the top physical DD, which is what an archer is), or halve `KitFactor` for
the volley alone. One constant either way.

⚠ **NEW APK REQUIRED.** This changes `ClassSkills`, and the client builds its Learn tab locally from
the compiled tables. No schema change, so no `game.db` delete.

**Not owed:** the HP-boost item. `RegisterHpBoost` already gives the warrior rungs 4-10 — up to
**+1000 max HP** at 43/49/55/62/66/70/74 — where the buffer stops at +700. Your ask was already met.
**No CSV rows owed either:** `warrior 3rd.csv` and the archer's file do not exist yet, and when they
land they overwrite every number above. `SkillCsvSeed --check` is clean.

### 🔴 TWO RIG BUGS THIS TURNED UP, both fixed, both of which move signed-off tables

1. **The boss party's three DDs had no 3rd class.** `BL-169` gave the TANK and the HEALER their
   disciplines and stopped there, so `BL-13` has been measuring every boss against two 2nd-class
   Champions and a 2nd-class Sorcerer in endgame gear. It surfaced because the warrior kit landed and
   the boss table **did not move by one second** — the rig could not see the kit. Fixed; the numbers
   moved: 60 84m→**69m**, 65 93m→**70m**, 44 109m→104m, but 76 99m→**119m** and 85 30m→32m.
2. **`TopPhysSkillPower` ignored the weapon gate** — it picked the highest-power skill LEARNED, not
   the highest the character could actually cast. Harmless while almost nothing physical was
   weapon-gated at the top of a ladder; not harmless now that Sundering Blow needs a 2H sword and
   Split Volley a bow. Fixed.

🔴 **AND A NEW OPEN FINDING: party DPS DIPS AT 76.** With a real 3rd-class party it reads 679 at 60,
698 at 65, then **444 at 76** before recovering to 1746 at 85. A ladder going backwards is a defect by
your own monotonic rule. It is NOT caused by the new kits — it appears the moment the DDs are given
any 3rd class — and it is the same neighbourhood as `BL-170`'s cliff at 80. Not chased yet.

⚠ **The rest of `BL-185` (steps 2-6) is untouched.** Constants were patched, measured and reverted;
tree is clean of them. `dotnet run --project tools/BalanceMatrix -- --dmgmatrix 90 mythic --his
--buffed` is the board this is judged on, and 🔑 **the CSVs move with the code** — #1 and #5 are skill
data, so every rung touched owes its row in `docs/data/classes_skills_csv/` in the same commit.

---

## `BL-186` ⏸ THE MAX LEVEL CAP — can it be removed? **POSTPONED (your call, 2026-09-10)**

**Filed 2026-09-09, on your instruction:** *"make a note to ask you about max lvl cap and if we can
remove it (but 1st to finish the dmg/def discussion)"*.

⏸ **POSTPONED INDEFINITELY, 2026-09-10** — *"bl-186 is a big discussion and rely on current systems
to work so to be changed... so we prsopone it for now"*. Not declined and not closed: the question
stands, it is simply not the next thing. It stays in this file so it is not lost, and nothing in it
should be investigated or half-built until you reopen it.

When it opens, the things that will need answering are roughly: what actually enforces the cap today
and in how many places; what the EXP curve does past it; whether the mob roster, the zone ladder and
the gear tiers have anything to give above the cap; and what an uncapped level does to `levelMod =
(level+89)/100`, which multiplies every attack and defence number in the game.

---

## `BL-189` 🔵 WEAPON-TYPE PROTECTION — `BowResist` generalised to every weapon type

**Your ask, 2026-09-09**, alongside the blow ruling: *"make a note later I want to do a wepon type
protection ... Like the bow resist but for other"*.

Today exactly one weapon type has a defence against it. `Entity.BowResist` (`Entity.cs:1167`) cuts
the damage a BOW attack deals, is clamped `[0, 0.9]`, and is fed from three places — armour-set
`StatMods`, passives, and the `SkillEffect.BuffBowResist` buff flag. Nothing equivalent exists for
sword, blunt, dual, spear or fist.

The generalisation is mechanical — one `float[]` indexed by `WeaponType.Base()` instead of one
field, with the same three feeds and the same clamp — but it is not free, so it wants your shape
before anyone writes it:

- **Damage or chance?** `BowResist` cuts DAMAGE. The tank's stab protection you ruled in `BL-188`
  cuts a ROLL. If weapon-type protection is one system it has to pick one, or carry both.
- **Who carries it?** Armour sets are the natural home (a heavy set resisting blunt is the genre
  convention) — but every set that gains one becomes a rock-paper-scissors statement about PvP.
- **Does the wire change?** `BowResist` rides the stats payload the client shows; six of them is a
  DTO change, and a positional DTO change is a `ProtocolVersion` bump (`BL-185` learned that).

Nothing is built. Filed so the ask is in the repo rather than only in a chat line.

---

## `BL-202` 🔵 THE WARRIOR'S DAMAGE AND CONTROL SKILLS — the half of both 3rd kits that is still owed

Your own words, 2026-09-11: *"I made some passives and buffs for warrior/aoe 3rd - they are missing
only teir dmg and control (active dmg) skills."*

Everything else in both files is **built** (0.130.0) — armour, Warrior's Strength, both weapon
masteries, Final Stand, HP Boost, HP Regeneration, Overpower, Battle Regeneration/Presence/Defence/
Resilience, Monster Knowledge. What neither file has is a single **damage** or **control** row.

**What stands in until they land:** `war_sundering_blow`, the last survivor of the `BL-185` derived
kit (Sound Smash's thirteen rungs at ×1.25 power, gated to a two-handed sword). It is registered on
**both** disciplines, which is a stand-in's shape and not yours — everything else in those two files
splits sword from blunt, and the damage will almost certainly split too.

⚠ **`--check` prints `🟠 NOT IN THE CSV  Sundering Blow` against both files, deliberately.** Both files
earned their `Check.Specs` line the day they landed rather than the day they are finished (the
`BL-197` lesson: a code side that is half yours and half derived is exactly where the two drift apart
silently). That line is the pressure working, and it goes out the moment your rows arrive.

🔑 **Two things the Warlord's half will want to answer**, because his identity is already built and
his damage is not:
- His basic attack already cleaves up to **ten** bodies. Does his damage skill AoE on top of that, or
  is the cleave the AoE and his skills single-target?
- He is twenty points of flat P.Atk under the Ravager at every rung, by your own columns. If his
  skills match the Ravager's power, the cleave is pure profit; if they are under, the gap compounds.


---


## `BL-208` ❓ ONE COSMETIC CELL LEFT FROM THE MAGUS'S 4th KIT

Filed 2026-09-11 with four items; **three you closed the same day** and they are in
[BacklogArchive.md](BacklogArchive.md) — the toggle drain now ladders per rung (both bars), the Spell
Empowerment rider's 10s / 10s is ratified, and the race-Burst @80 rung is a **debuff-landing** step,
not a wasted one (the spell becomes level 80, and `DebuffLandChance` reads the rung's own learn level).

**What is left is one inert cell.** Pyro Burst's three 4th-tier rows carry `(success chance x1.5)`;
your 3rd-tier row does not, and it looks copied from the Arcane Burst and Frost Burst rows beside them.

**It cannot bite either way.** A burn's save is `DebuffSchool.None` — your own *"for burn nothing
protects .. always land"* — so the landing branch skips the contest this number would modify. It is
carried on the def so your cell and the code read alike, rather than deleted from your file over a
number that does nothing. Say the word and the three cells go.

---


## `BL-213` 🟡 THE MASTERY ROSTER IS REBUILT TO YOUR TABLE — six numbers in it are still mine

**BUILT (0.133.0).** Your table, verbatim, and what each line became:

| you said | who that is | built |
|---|---|---|
| mages → duration/reuse, no toggle | Magus | had reuse @76; **gains Lasting Enchantment @76** |
| archers/warriors/duals → double_dmg/reuse + toggle | Ravager + Warlord | had Overpower 3/7/10% @20/40/76 and the toggle @81; **gain reuse @76** |
| ” | the three MELEE rogues | already had all three (`BL-203` gave them Overpower @40/76) — **unchanged** |
| ” | the three ARCHERS | **all four are new**: Overpower @40/76, reuse @76, toggle @81 |
| “1 rung lower than warriors … (40,76)” | archers + duals | rung 1 (3%) at 40, rung 2 (7%) at 76 — the ladder `BL-203` already gave the duals |
| buffer/healer → duration no toggle | Lightbringer + Warchanter | **unchanged** |
| tank → nothing | Bulwark | **unchanged**, and the only line of the old roster that survived |

This **reverses `BL-191`'s "ARCHER — never"**, which was ruled 2026-09-10 with a reason (*"archer have
enough skills that are always hit wit big power"*) and which I asked you to confirm before writing it
down. Two days of play reversed it. The old entry is in [BacklogArchive.md](BacklogArchive.md).

🟡 **WHAT IS MINE AND NOT YOURS — six numbers, every one a single line to change:**

1. **Two DISPLAY NAMES.** `reuse_reset_momentum` is ONE skill with ONE number and a per-class name —
   your own rename rationale, *"as other classes can aqure it too"*. The Magus calls it **Arcane
   Momentum** and the dagger rogue **Stab Momentum**, both yours. The warrior's and the archer's
   copies needed labels, so I took them from each file's own vocabulary: **"Battle Momentum"** (beside
   Battle Regeneration / Resilience / the Battle stances) and **"Bow Momentum"** (beside Bow Blessing
   / Spirit / Focus / Stance). The toggle keeps **Overpower Mastery** on all four classes, per your
   2026-09-11 correction.
2. **Four LEARN LEVELS.** The warrior's new reuse rung, the Magus's new duration rung and the
   archer's reuse rung all sit at **76**; the archer's toggle at **81**. They mirror every other
   mastery rung in the game, which is the only defensible read of *"the same … as duals"*.

✅ **AND THE `[Double]` GAP IS CLOSED — his list, 2026-09-12:** *"Every archer dmg skill without
explotion and magic arrow - so the twin/heavy/barrage(each arrow on its own)"*. `CanDouble` is now on
**Twin Arrows**, **Heavy Arrow** and **Arrow Barrage**; Explosive Arrow, the three Magic Arrows and the
three race ultimates at 85 are deliberately not on it. 🔑 *"each arrow on its own"* was already the
shape and needed nothing: both volleys are CHANNELS, the wrapper resolves nothing, and every arrow is
its own `ExecuteSkill` with its own miss, crit and now double roll — so Arrow Barrage rolls ten times.
The flag sits on the arrow (the mechanic) and on the wrapper (the label the card reads).

⚠ **NEW APK** — the class-skill TABLES changed and the client builds its Learn tab locally.


---

## `BL-215` 🔵 THE MAGE'S DAMAGE — two of the three levers are pulled; the third is still yours

**Rewritten 2026-09-12** after you answered it. The original text is in
[BacklogArchive.md](BacklogArchive.md).

### ✅ Lever 1 — `MagicK`. CLOSED, and your own question closed it.

*"I still wonder does mdef in IG have lvl mod? (and same question for pDef)"* — **it does, and we
measured it off your own five in-game rows** (`docs/balance/DamageVsIG.md`). Divide your stated total
M.Def by your gear M.Def and then by `levelMod`, and the remainder is constant to ±2% across 56
levels: `IG M.Def = SUM(jewel M.Def) × MENbonus × (level+89)/100`. Your P.Def divides out the same way.
So the level term is shared with IG, `MagicK 91` stays IG's verbatim constant, and your branch is the
second one: fix `BL-212` as a level mod, raise the 76+ spell power.

### ✅ Lever 2 — the 76+ spell power. BUILT (0.134.0).

*"increase the mages spell power ... atleast 30% ... (so about 20~40 points up 110-> 130, 138->180/190)
after 76"*. Built as **×1.30 on all three 4th-tier rotation ladders**:

| ladder | skills | was | now |
|---|---|---|---|
| blast | Elemental Blast · Vampiric Bolt | 110 → 138 | **143 → 179** |
| fast/rider | Quick Blast · Witches Curse | 88 → 109 | **114 → 142** |
| area/rider | Elemental Wave · Arcane Wave · Frost Spikes · Frost Pierce | 66 → 105 | **86 → 137** |

⚠ **×1.30 rather than your two point figures, because they disagree with each other**: +20 on 110 is
+18%, under your own *"atleast 30%"* floor, while +40 on 138 is +29%. The percentage is the ruling and
the points were prefixed "about"; 143 sits just over your "130" and 179 inside your "180/190".
⚠ **All three ladders, not just the blast.** You named the blast's numbers because they are the ones
you read, but raising only it would silently retune Quick Blast and the waves DOWN by 30% against it.
Their ratios to each other are exactly as you authored them.
🔵 **The ULTIMATES are NOT in it** — Elemental Burst, Thunderstorm, Arcane Void and the three race
Bursts keep their power. They are five-minute showpieces rather than *"the dmg"*. One line each if
you want them, and this is the only part of your item 3 I did not do.

### 🔵 Lever 3 — the magic crit-RATE cap. STILL OPEN, and now the only ceiling left.

`StatCaps.MagicCritRate = 20%`, and since `BL-209` a fully-blessed Magus of **every race** sits exactly
on it — so the second half of the Harmony of the Wizard buff you just doubled is being thrown away.
Its own doc-comment anticipated this: *"still max 20% but one day if we want to increase it no mage to
be short on crit"*. At ×3.12 crit damage, **every 5 points of cap is about +10% average damage**.

### 📐 What the mage has gained since you last played (0.132.0 → 0.134.0)

| | |
|---|---|
| crit pass (`BL-209`/`BL-210` + the `BL-214` bug) | **×1.25** |
| 76+ spell power | **×1.30** |
| Elemental Blast's reuse 1.0s → 0.5s (cycle 1.40s → 1.00s) | **×1.40** |
| **compounded** | **≈ ×2.3** |

⚠ **Re-playtest before pulling lever 3.** That is a lot in one pass, and if it lands you will not be
able to tell which of the three did it.


---

---

## `BL-218` 🟢 DEBUFF LAND RATES — every ruling is built; open only if a playtest disagrees

**2026-09-13, third rewrite.** Your 20% SPT ruling, the compounding ruling (`BL-225`) and the 35% CON
ruling are all built. The old text is in the archive. 📐 Tables:
[balance/DebuffLandRate.md](balance/DebuffLandRate.md) ·
`dotnet run --project tools/BalanceMatrix -- --ccprofile`

### ✅ Where it landed — a `×1.00` debuff against a fully-buffed level 90

| | before this pass | now |
|---|---|---|
| magical (SPT) | 10-11% | **20-23%** |
| physical (CON) | 8-10% | **19-24%** |

Both inside your *"15-25% which is good"*, and the two schools are within a couple of points of each
other for the first time. A `×0.50` skill (Numbing Shock) halves those; a `×1.50` one (Arcane Burst,
Weapon Break) lands about 30%.

### ❌ DECLINED — the mage's ×2 SPT land-rate passive

Your *"if you haven't added the passive on mage for x2 spt resistance ..good don't and remove it as
ruling"*. Never built; struck. The land rates got there from the resistance side instead, so there is
no attacker-side channel in the engine and nothing plans to add one.

### ✅ ANSWERED AND BUILT (`BL-227`, 0.141.0) — magic resistance also resists magic debuffs

Your question: *"I wonder just logically shouldnt mresist add to magic debuffs resistance? that way a
tank and a nullblade(for 10s) will have aditional anti magic - like endLandRate x 0.3(30% mresist)"*.
Table A of `--ccprofile`, where the last column is now the BUILD:

| defender | SPT | mRes | without mRes | **with mRes (built)** |
|---|---|---|---|---|
| Magus (mage) | 36 | 35% | 19.8% | **12.9%** |
| Bulwark (tank) | 26 | 21% | 22.8% | **17.9%** |
| Nullblade | 27 | 10% | 22.4% | **20.2%** |
| Nullblade + Magical Armor (10s) | 27 | 60% | 22.4% | **9.0%** |

Your ruling: *"I like the idea mresist to decrease the chance ..it look not so much op ... and we
espect nullblade with magical armor to resist more."* Built, passives included — you looked at the
mage row and took it. The Nullblade ultimate is now a real ten-second control window.

⚠ Noted for later, since it is now the build: the MAGE carries the most magic resistance of the three
(35%, from the nuker's own anti-NUKE `anti_magic` ladder), so he is the hardest of all to land a magic
debuff on. You judged that acceptable; if a playtest disagrees, the narrow version is "mResist from
buffs and ultimates only, not passives", which is one line.

### 🔵 Also still open, unchanged

- **The flat `CcResist` gear cliff** — 0% common, 0% rare, **28% epic, 40% mythic**. Armour-set only,
  identical for every class, nothing on the attacker's side answers it. Now the largest single term
  left in the product.
- ⚠ **Battle Resilience is very strong under compounding.** 80% CON *and* SPT for 60 seconds on a 150s
  reuse takes a stun from 19% to **3.8%** — near-immunity for 40% of the time. It was already the
  dominant term when resistances summed (it alone hit the old 0.8 clamp); compounding has made it
  cleaner but no smaller. Not touched — flagging it because it is now the biggest number in the file.


---

## `BL-228` ⏸ FUTURE CONTENT — boss jewels that trade one school of control against another

Your idea, 2026-09-13, parked by you in the same breath as raising it:

> *"Later we can have like ig boss jewels that give chance to one school and resist other , and
> depending on what jewels u equip u can resist stuns and your fears land or resist fears and land
> holds ..etc (mark it as future content)"*

**Not scheduled and not designed** — filed so it is not lost, because it is the first idea in this
project that would make control a BUILD rather than a stat.

🔑 **THE ENGINE IS ALREADY SHAPED FOR IT, and that is worth recording while it is fresh.** Since
`BL-225` every control resistance is its own `(1 − r)` factor in a product, and since `BL-227` a
second, differently-sourced number (`MagicResist`) joins that same product. A jewel that reads
*"+X% chance for your HOLDS to land, −Y% resistance to FEAR"* is two more factors — one on the
attacker's side, one on the defender's — and needs no new mechanic, only:

1. **A per-EFFECT axis.** Everything today is per-SCHOOL (SPT vs CON). Trading "stuns" against
   "fears" needs the factors keyed on the `SkillEffect` bit (Stun / Fear / Root / Slow), not on
   `DebuffSchool`. That is the real work in this entry.
2. **An attacker-side land channel**, which the engine still does not have at all — the same gap the
   declined mage SPT passive would have filled. A jewel granting *"your fears land more"* is the
   natural first thing to own it.
3. **Jewels as a slot that carries authored skill-shaped payloads**, which armour sets already do
   (`ClassFlatBonus` survives as an armour-set type) — so the precedent exists.

⚠ **Don't build 1 or 2 speculatively.** Both are cheap only once the jewels tell them what shape to
be; inventing a per-effect resist matrix with nothing authoring it is how `BL-192`'s parked
recommendations went wrong.

---

## `BL-229` 🔵 THE 74 → 76 DEBUFF CLIFF — a rung that stops climbing while the world does not

Found 2026-09-13 while answering your *"it don't feel the curses land so often"*. **Nothing here is a
defect**; every number below is the design working exactly as authored. It is a shape you have not
ruled on, so it is yours to decide.

`DebuffLandChance` reads the rung's **LEARN level**, not the caster's — your own ruling 2026-08-19:
*"it should be difference enemy lvl and skill learned lvl .. not casters"*. That is what makes a
ladder worth climbing. The consequence at the top of the 3rd tier is a wall.

**Witches Curse (`x0.70`), against a same-level melee creature (SPT 38):**

| your level | best rung you hold | it lands |
|---|---|---|
| 40 – 74 | tracks you (`@40` … `@74`) | **36.7%** every step |
| 80, still `@74` | `@74` | **24.3%** |
| 85, still `@74` | `@74` | **15.7%** |
| 90, still `@74` | `@74` | **9.5%** |
| 90, ascended (`@90`) | `@90` | **36.7%** |

The 4th tier fixes it completely — its rungs run `@76, @77, @78 … @90`, one per level, so an ascended
caster never drifts. **The cliff is entirely the gap between reaching 76 and paying the 100kk Rite at
Archmaster Sevrin.** A caster who levels to 85 first has watched his whole debuff kit fall to a third
of its rate with nothing on screen explaining why.

**Three ways out, if you want one:**

1. **Leave it.** The Rite is the fix, and a kit that decays until you pay for it is a real incentive.
2. **Floor the drift at the tier boundary** — a rung never counts as more than N levels behind the
   caster (N = 2 would hold 36.7% ≈ 30%).
3. **Say it on the skill card** — *"cast at level 74 · loses power against higher creatures"* — and
   change no number at all. Cheapest, and it turns an invisible decay into a reason to ascend.

⚠ It is not a nuker problem. Every contested debuff in the game is on this curve: holds, stuns, fears,
armour breaks, DoT openers. The nuker is just where you happened to look.

---

## `BL-230` 🔵 CONTROL RESISTANCE IS NOT ON THE NPC SHELF — and two nuker races land the same

> *"A 90 lvl ice master (frost spike/Pierce) or a infermo master (whiches curse) land somehow the same
> at 90lvl demon archer with ot without buffs for spt resist"* — 2026-09-13

**Both halves of that are true, and they are two different things.** Measured with
`dotnet run --project tools/BalanceMatrix -- --ccland`, which now carries an **Elf Magus (Ice
Master)** and a **Demon Magus (Inferno Master)** as attackers and a **Demon Hunter** as a defender —
his exact case was not in the table before.

### 1. 🔴 The NPC buffer sells no control resistance — but your ADMIN FULL BUFF does

⚠ **CORRECTION, after you said *"I test with fullbuff from admin menu"*.** `AdminBuffSet` is every skill
every race of **Warchanter** can learn, and the Warchanter learns **Clarity and Fortitude**
(`RegisterWarchanterBuffs`). So your full buff DOES raise SPT resistance — 20% → 49-56% — and the
magic debuffs DO drop with it, 24% → 13-16%. **This section is about the NPC SHELF only, and it is
not what you were seeing.** What you saw is §2: the two casters equal to EACH OTHER, in both states.

| vs Hunter (Demon bow), lvl 90 | bare | **+ NPC shelf** | + full shelf | + Warchanter + Holy Mark |
|---|---|---|---|---|
| Frost Spikes (Ice, SPT, ×0.70) | 23% | **23%** | 20% | 13% |
| Witches Curse (Inferno, SPT, ×0.70) | 24% | **24%** | 21% | 13% |
| Frost Pierce (Ice, **CON**, ×0.50) | 13% | **13%** | **13%** | 9% |

`magRes` reads **20% bare and 20% buffed**; `phyRes` reads **10% and 10%**. The whole 30-blessing
shelf — Might, Ward, Body, Soul, Insight, eight harmonies, three Marks — contains **neither Clarity
(SPT) nor Fortitude (CON)**. `NpcBuffTiers` is *"his CSV, verbatim"*, so this is authored, not a code
slip; it has simply never been tested from the receiving end.

**So today, control resistance is not something a solo player can buy.** It comes only from your gear
set, your own passives, a real Lightbringer casting Clarity/Fortitude, or a Mark — and the Mark that
carries it is the one nobody takes (`BL-225`: Harmony Mark grants none, and all four share `MarkKey`).

**Your call — three shapes:**
1. **Put Clarity and Fortitude on the shelf**, laddered and priced like Ward/Body. Buffing then
   visibly changes how often you get held. Simplest, and it makes the shelf honest.
2. **Leave it — control resistance is what a BUFFER CLASS is for.** Defensible: it gives the
   Lightbringer something the NPC can never sell, which is the argument for every class buff.
3. **Only the low rungs on the shelf**, the top two class-only — the compromise the Ward ladder
   already uses.

### 2. ✅ RULED AND BUILT (0.142.0) — they landed the same because it WAS the same roll

`save = Magical` (SPT) and `xmod = 0.70` on both — his own CSV rows, *"(success chance x0.7)"* each.
And **a class grants no stats** (2026-08-10), so the only thing separating the two casters is base
ATK: Elf Magus **38**, Demon Magus **43**. Five points of ATK is worth about **one percentage point**
in the contest. 23% vs 24% is not a coincidence — it is the design holding.

⚠ **The lever, if you want them to differ, is the `xmod` in the CSV, not a stat.** Two same-school
debuffs at the same multiplier will always land within a point of each other no matter who casts them.

⚠ **Frost Pierce is a genuinely different axis** and worth knowing: it opens a **Bleed**, and a DoT's
save comes from the FAMILY, not the skill's `DebuffSchool` (your 2026-09-10 ruling) — so it is
**AGI vs CON**, and no amount of SPT resistance will ever touch it. That is why its row does not move
even on the full shelf.

---

**`BL-230` §2 closed 2026-09-13 — built as 0.142.0.** Your three rulings, verbatim:

1. *"why bleed is agi vs con? Physical buffs should be atk vs con magical atk vs spt"* → **the attacker
   is always ATK.** The AGI branch is gone from both roll sites, now one `GameLoopService.CcContest`.
2. *"remove the 3 skills the success decrease .. they don't Harm as a buff removal or bind or silence
   … not sure kill if they land"* → Frost Spikes **0.70 → 0.85**, Frost Pierce **0.50 → 0.85**,
   Witches Curse **0.70 → 1.00**. Scarecrow keeps 0.50 and Arcane Void 0.30: a fear and a cancel take
   the target's turn, which is the line you drew.
3. Your 0.85-not-1.00 reason is recorded in the code and measurable: `--slowstack`.

What is still 🔵 and owed is **§1 only** — whether Clarity/Fortitude belong on the NPC shelf.

---

## `BL-231` 🔵 THE LANDING-MODIFIER SCHEMA — priced by what landing TAKES AWAY

> *"Show me all landing modifiers on what debuffs(name + stat decrease) and what is the saving stat
> ... Stuns/hold/fears/charm should be x0.7, All dots can be at x1, all mdef decreases and p Def
> decreases to X1, All p/m.atk and a/c/m.speed x0.85, buff cancel x0.5.. Somethig like that."*
> — 2026-09-13

📐 **THE FULL TABLE IS [balance/DebuffLandMods.md](balance/DebuffLandMods.md)** — 74 skills, every one
with its payload and its saving stat, regenerated from the code with
`dotnet run --project tools/BalanceMatrix -- --landmods`. **Nothing has been retuned.** The `want`
column is your schema applied mechanically so the disagreements show.

Your five buckets classify **63 of 74**. What needs you:

### A. Seven skills the rule does not reach
Three **silences** sitting at three different numbers today (×0.50 / ×0.70 / ×1.00) with no rule —
you grouped silence with *"a buff removal or bind"* earlier the same day, so is it **control (0.70)**
or **cancel (0.50)**? Then **Soul Sap** (anti-heal −50% HP received), **Mana Strain** (MP costs ×2),
**Arcane Burst** (cuts the target's SPT *resist* 40% — a force multiplier, not a debuff) and
**Boss's Judgment** (a scripted boss mechanic; probably outside the schema entirely).

### B. Four places it contradicts a ruling you already made
1. **Frost Pierce** — you set ×0.85 an hour earlier *because* its bleed's hidden 20% slow stacks with
   Frost Spikes. "All dots at ×1" undoes that. ⚠ And **every** bleed carries that slow (`DotTiers`),
   so the DoT bucket is really "×1 including a movement debuff the row does not show".
2. **Armor Break / Weapon Break are ×1.50 on your own `BL-90` ruling** — *"should be 75% at parity
   (x1.5)"*. The schema drops them to ×1.00 and ×0.85. Opposite direction from everything else here.
3. **Thirteen stuns/roots/fears outside the nuker are ×1.00 today** and the schema takes them all to
   ×0.70 — a real, broad nerf to every tank and rogue, not a nuker tweak. Intended?
4. **Dazzling Arrow cancels 3 buffs AND stuns, at ×1.00** while Arcane Void (cancels 2) sits at ×0.30.
   This one is the best argument *for* the schema in the whole file.

### C. Two facts to know before ruling
- **Pyro Burst's ×1.50 does nothing** — Burn saves against nothing, so `alwaysLands` short-circuits
  before the modifier is read. Decoration; should be ×1.00 or deleted either way.
- **`save = none` means the FIZZLE roll (~99%), not the contest (50%).** Chilled, Sapped, Weakness,
  Greater Weakness, Soul Sap and Boss's Judgment are all in that state, so a ×1.00 there is close to
  *"always lands"* — a very different thing from ×1.00 on a contested skill. If defence cuts are
  going to ×1.00 across the board, look at those rows twice.

**Say go and I apply the whole schema in one pass** (code + both nuker CSVs + `Formulas.md`), with
whatever you decide for A and B.

---

## `BL-232` 🔵 `debuff_landmods.csv` — built, checked, and waiting on your pass

> *"I group them but it's not OK as u said ... So dmg + debuff should have lower chance than a solo
> debuff ... Ao take all the debuffs each single skill make them in a table and put the modifiers
> there -> name of skill, I'd of skill, class that learns it, description of the skill (what it does
> and what stat it debuffs - % of max rung), saving stat, success modifier. Then each new debuff to go
> there and to ask for modifier edit ... The current classes csv descriptions to remove the modifiers
> and those modifiers to be red from that new file"* — 2026-09-13

✅ **BUILT (0.142.1).** `docs/data/debuff_landmods.csv`, 74 rows, your six columns plus `SHAPE` and
`IN_CODE` as decision aids. `(success chance xN)` is stripped from all four class CSVs (231 rows).
`SkillCsvSeed --check` walks it. `CLAUDE.md` carries the contract.

🔑 **Your new rule replaces the bucket scheme in `BL-231`, and it is a better rule**: the modifier
prices **how much one cast does at once**, not what kind of effect it is. Buckets keyed on the effect
put Armor Break (two debuffs, no damage, ×1.5) and Witches Curse (one debuff + damage) in the same
place while they deserve opposite numbers. Applied so far: **Witches Curse ×1.00 → ×0.85**, your
worked example.

### 🔵 What is owed: your pass over the `SUCCESS` column

The file ships as a faithful mirror of today's code, so **nothing moved except Witches Curse**. Edit
`SUCCESS`, tell me, and I move the code to match. The `SHAPE` column sorts the work for you:

| SHAPE | rows | your rule says |
|---|---|---|
| `DEBUFF ONLY (1)` | 32 | ×1.00 — *"a solo slow or a solo dot should be at x1"* |
| `DEBUFF ONLY (2)` | 15 | ? — two cuts, no damage. **Armor Break is here, at ×1.5** |
| `dmg+1 debuff` | 16 | ×0.85 — *"witches curse that does dmg should be x0.85"* |
| `dmg+2 debuffs` | 8 | ? — *"with dmg or other debuff should go lower"*. Lower than 0.85 — 0.70? |
| `dmg+3 debuffs` | 3 | ? — lower still? |

⚠ **Four questions the file cannot answer for you:**
1. **`DEBUFF ONLY (2)` vs `(1)`** — is Armor Break ×1.5 *because* it is debuff-only, or is ×1.5 its
   own special case? Gravity is also debuff-only-with-two-cuts and sits at ×1.00 today.
2. **`dmg+2` and `dmg+3`** — how far below 0.85? Eleven rows wait on one number each.
3. **The three silences** are ×0.50 / ×0.70 / ×1.00 and none of them does damage. Still unruled.
4. **`SAVE = none (fizzle roll)`** — those skills take the ~99% fizzle roll, not the 50% contest, so
   ×1.00 there means "always lands" rather than "half the time". Different number, same column.

### ⚠ 39 of the 74 rows are NOT LEARNABLE — don't spend modifiers on them
`Shield Bash`, `Envenom`, `Rupture`, `Terrifying Roar`, `Snare Trap`, `Entangling Roots`, `Soul Sap`,
`Warding Step`, `Weakness`, `Greater Weakness`, `Frost Bind`, `Creeping Frost`, `Hamstring`,
`Toxic Sting` were orphaned by the **2026-08-10 40+ purge** and the nuker rebuild — the learn
assignments went, the defs stayed on purpose. The others are boss / whisp / proc-granted. The `CLASS`
column says which. **They are also the obvious raw material for the 40+ files still to come**, which
is exactly why they were kept.

## `BL-233` ❓ THE DEMON BUFFER'S P.DEF — I cannot reproduce it, and here is what I measured

> *"check demon buffer (epic 76 heavy + maul) had less pDef than elf buffer (epic 76 light + bow)
> both @90lvl admin buffed"* — 2026-09-13

🔴 **The rig says the opposite, in both states.** A new mode builds exactly those two characters —
level 90, epic quality, tier-76 gear, the 4th-class kit, the demon in `heavy_t76_epic` + 2H maul and
the elf in `light_t76_epic` + bow — and reads `EffectiveDefence`, which is the identical field the
character sheet prints:

```
dotnet run --project tools/BalanceMatrix -- --bufferdef 90 epic 76 [--buffed]

  race     CON |  items   P.Def   M.Def |      HP   eva   (unbuffed)
  human     29 |    366    1118    1476 |    7255   105
  demon     31 |    366     991    1509 |    7814   109
  elf       25 |    308     887    1443 |    6184   129     demon − elf = +104

  race     CON |  items   P.Def   M.Def |      HP   eva   (ADMIN-BUFFED)
  human     29 |    366    1788    2656 |   11515    96
  demon     31 |    366    1585    2716 |   12438   100
  elf       25 |    308    1419    2597 |    9748   120     demon − elf = +166
```

**Why the gap can only widen when you buff:** every P.Def buff on the admin bar is a PERCENT
(Harmony of Protection, Harmony Mark, Bulwark — the mode prints them per race), and a percent
multiplies whatever it lands on. So buffing *amplifies* an armour lead, it cannot reverse one.

**And there is nothing weight-shaped that could reverse it.** P.Def in this game has **no stat term
at all** — not CON, not AGI (`Entity.RecomputeDerived`, and `docs/Formulas.md`) — so its only inputs
are items, armour masteries, set bonuses and buffs, and all four were checked:

| input | heavy (Demon) | light (Elf) |
|---|---|---|
| epic 76 body | **232** | 174 |
| helm + gloves + boots + jewels | 134 | 134 |
| `buffer_armor_mastery` rung 29 | +193 (identical for all three weights, by your own one-line rows) | +193 |
| race mastery (`Heavy Armor Mastery` / `Harmonist Light Mastery`) | no P.Def — speed clauses, crit-damage resist, MP regen | no P.Def — speed clauses, evasion, crit-rate resist |
| tier-76 set bonus | Ironforge A: HP/STR/CON/CC — **no P.Def** | Nightleaf A: P.Atk/atk speed/MP — **no P.Def** |

### ❓ What I need from you (any one of these settles it)

1. **The two numbers off the two sheets**, and the two characters' levels.
2. **What the demon is actually wearing in the BODY slot** — if a piece is missing or it is a
   different body, the 232 is not being paid at all.
3. Whether either character had **buffs already up** when you pressed the admin full buff (the set is
   18 squares against a cap of 20, so a couple of pre-existing ones can push it over and the FIFO
   drops the OLDEST — which is the GROUPS, i.e. exactly the P.Def layers).

Until one of those lands there is nothing to fix: every path I can measure has heavy ahead by
~100 (bare) to ~170 (buffed).

## `BL-234` ❓ URGENT LESSER HEAL — you gave four numbers, the fifth is mine

✅ **BUILT (0.143.0)**, at 83 for all three buffer races, exactly as you specified: MP **250**, cast
**3s**, reuse **5s**, **3** Skill Stones, range **0/1000**, **5** targets, **20%** on the first.

❓ **The per-rank falloff is the one thing you did not give.** I carried the healer's own **−2%**
over, so the five slots pay **20 / 18 / 16 / 14 / 12%** of each target's own maximum HP (worst-hurt
first, the caster placed by his own injury like anyone else). Tell me if you want a different decay —
it is one number in `Skills.Warchanter4th.cs` and one in the CSV row.

📐 On a 21k tank the whole cast is ~16,700 HP spread over five people, against the healer's ~46,000
over eleven: `dotnet run --project tools/BalanceMatrix -- --healpower 90 epic`.


---

---



## `BL-250` 🟢 THE SUBCLASS SYSTEM — SLOTS, TICKETS, AN NPC, AND THE SIGILS THEY UNLOCK

**Re-specced 2026-09-16, the SECOND time that day; both earlier drafts are in the archive.** The sigil
half (§1-§4) is unchanged and settled. What is new is everything around it: subclass slots are now a
thing you EARN and then BUY, a subclass is taken from an NPC, and a subclass below 75 can be swapped
out. ✅ **FULLY UNBLOCKED 2026-09-17.** The currency landed on 2026-09-16 (`BL-257`) and the prices
with it; on the 17th you ruled the **swap free below 75** and **cut the 5,000-platinum rung** until the
summoner ships. Nothing is waiting on you; see §9.

**Your model, verbatim, in two messages:** *"In IG when you lvl up a sub class to 75 u get to use its
'Ability' → each subclass have its ability-identity … you can have up to 3 sigils active … each
subclass activates a sigil slot (up to 3) + unlocks its designated sigils"*, then *"we can make subs
slot limit to 3 and u need to pay 500kk, 5kkk, X amount of premium, Y amount of premium, Z amount of
premium to unlock all slots … those values give you a subclassTicket and u can unlock them
using(consumable) ticket"*.

### 1. 🔑 THE THREE SIGIL SLOTS STOP BEING Attack / Defence / Support
Today `SigilSlot` is a real exclusion axis — one per slot, enforced by `ExclusiveGroup`. It becomes
**three identical slots**: any three of the eighteen, so long as you have unlocked them. The
`SigilSlot` enum stays as a *label* for the UI or goes entirely; that is a free choice.

### 2. 🔑 A SIGIL SLOT IS OPENED BY A SUBCLASS AT **75**, NOT 76
*"lets make them once sub becomes 75 u are able to get the tree + sigil slot -> 3rd class
(automatically gotten when taken subclass) … main class dont open slot; only subs will .. the 1st
three subs are required to open the 3 slot -> then every other just opens their tree (if not
opened)"*.

| what | rule |
|---|---|
| **level gate** | that SUBCLASS is **75** |
| **class gate** | it holds its **3rd class**, granted automatically when the subclass is created (`BL-252`) |
| **slots** | subclass #1 → sigil slot 1, #2 → slot 2, #3 → slot 3. **Three is the ceiling** |
| **subs 4 and up** | open **only their tree**, never a fourth sigil slot |
| **main class** | opens **nothing** — no slot, and no tree of its own group |

### 3. 🔑 A SIGIL GROUP IS UNLOCKED BY OWNING A SUBCLASS OF IT

| group | the classes that unlock it | its three sigils |
|---|---|---|
| mage | the 3 Apprentice | Frenzy · Mage Defence · Arcane Support |
| healer | the 3 Priest, healer discipline | Holy Power · Holy Protection · Holy Support |
| buffer | the 3 Priest, buffer discipline | Soul · Spirit · Immortality |
| rogue | the 6 Rogue | Focus · Agility · Aim |
| warrior | the 6 Warrior | Fury · Duel · Fortitude |
| tank | the 3 Knight | Body · Aegis · Critical Protection |

**Your own count, verbatim:** *"as warrior/rogue u can have 6 subs and all trees and change them as u
like for the price of 100kk, tank,buffer,healer will get up to 5 trees without their own, mage for now
5 trees"*. ✅ That falls straight out of the rule with nothing added: `Player.CanAddDiscipline` already
refuses a second class on the same **path** (`BL-255`), so the nuker is ONE path — no mage may add a
mage — while the warrior's six names are **two** paths and the rogue's six are **two** (dagger and
bow, each spelled three ways by race). Those two archetypes reach their own group and the other four
do not. When summoners arrive, nuker ↔ summoner unlocks the mage group for a mage, with no code change
here either.

### 4. 🔑 THE SIGIL PRICE MOVES ENTIRELY ONTO CLEARING
*"we can remove their sp/gold cost -> they are their own system. only clearing will cost 100kk (its
10kk now i think + losing the 60kk sp and 30kk gold)"*.

| | today | after |
|---|---|---|
| commit one sigil | 20kk SP + 10kk gold (`SigilSpCost` / `SigilGoldCost`) | **free** |
| commit all three | 60kk SP + 30kk gold | **free** |
| clear one | 10kk gold, no refund (`SigilResetGold`) | **100kk gold** |

❓ **One reading is mine: `SigilResetGold` becomes 100kk and stays PER SIGIL**, because your sentence
prices "clearing" against the old cost of ONE sigil. If you meant 100kk to wipe all three at once, it
is one line.

### 5. 🔴 NEW — THE SUBCLASS SLOT LADDER: THREE EARNED, FIVE BOUGHT
A character does not simply "have" subclass slots any more. **Three arrive with your progress, and
every one after that is bought.**

| slot | how it opens | your words |
|---|---|---|
| **1** | your MAIN reaches **76** and takes its 4th class | *"When you get main to 76(4th) u get your 1st ticket"* |
| **2** | subclass #1 reaches **75** | *"then once sub gets to 75 u get ur secondTicket (+ sigils and etc)"* |
| **3** | subclass #2 reaches **75** | *"same for second"* |
| — | subclass #3 reaching 75 pays **no ticket** | *"then u lvl up ur 3rd sub class and no ticket only sigils"* |
| **4** | bought for **500kk gold** | |
| **5** | bought for **5kkk gold** (5 billion — checked: `Gold` is a `long` on the entity, the record and every DTO, so it fits) | |
| **6 · 7** | bought for **100 / 1,000 PLATINUM** ✅ | *"make the slots tickets buy able with plat need 100/1000/5000"* (2026-09-16) |
| ~~**8**~~ | ~~5,000 platinum~~ — **CUT 2026-09-17, until the summoner exists** | *"remove last platinum rung for now and when summoner is build we will add it back"* |

✅ **THE LADDER IS SEVEN RUNGS AND THE ROSTER IS SEVEN SUBCLASSES — they match exactly.** Three earned
+ four bought (500kk · 5kkk · 100 plat · 1,000 plat) = **7 slots**, and `--paths` measures **8 paths**,
one of which your main occupies. Nobody ever sees the *"no more available subclasses"* wall on a
ticket; it now only guards the case where a character's own class mix runs out early.

🔑 **THE 5,000-PLATINUM RUNG COMES BACK WITH THE SUMMONER**, which adds a ninth path and makes eight
subclasses reachable. 🔑 **That is a one-line addition by design** — the ladder is an authored list of
rung prices and the *"is a discipline still available"* gate is computed, so adding a path and a rung
needs no other change. Do not hard-code "seven" anywhere.

🔑 **THE TICKET IS AN ITEM, NOT A COUNTER.** *"those values give you a subclassTicket and u can unlock
them using(consumable) ticket"* — earning or buying one puts a **Subclass Ticket** in your bag, and
CONSUMING it is what opens the slot. That is worth having for a reason beyond flavour: it separates
the reward from the decision, so the ticket the main's 4th class paid you can sit in the bag until you
know which class you want.

🔑 **AND THE LADDER STOPS WHEN THE ROSTER DOES.** *"when no more available subclasses … next ticket is
locked and cannot be bought .. with the text that no more available subclasses -> when we add more it
will be available again"*. So the purchase is gated on a COMPUTED question — "is there a discipline
left that this character could legally add" — never on an authored number, and the day a new class
lands the ticket unlocks itself with nothing edited.

🔑 **HOW MANY SUBCLASSES EXIST TO BUY SLOTS FOR: SEVEN.** I first wrote eleven here and **you were
right to push back** — *"Buffer, healer, duals, Archer, warrior, war aoe, Tank .. thats 7 .. Not 11"*.
The rule that counts is now the PATH rather than the raw discipline (**`BL-255`**, built in 0.151.1),
and measured with `--paths` the twelve live disciplines fold into **eight**: Tank 1 · Warrior 2 ·
Rogue 2 · Healer 2 (healer + buffer) · Nuker 1. A main takes one, so **seven subclasses is the
ceiling for anybody**.

✅ **The ladder was one rung longer than that roster, and you cut it** (2026-09-17): the 5,000-platinum
rung is gone until the summoner adds a ninth path, so seven rungs meet seven reachable subclasses.

### 6. 🔴 NEW — A SUBCLASS AT 74 OR BELOW CAN BE SWAPPED OUT
*"while your subclass is less or equal to 74 .. u are allowed to remove it (reset it to other - mage
subclass can take other mage subclass when resetting -> it takes its place so no duplicates will be at
the end)"*.

- The gate is the SUBCLASS's own level, **≤ 74**. At 75 it is yours for good — which is exactly the
  level that pays its sigil slot and its tree, so the point of no return is the point of reward.
- **The replacement takes its SLOT.** That is the load-bearing half: the no-duplicate-discipline check
  has to ignore the slot being replaced, or swapping a Magus for a Magus-race sibling would refuse
  itself. Your *"it takes its place so no duplicates will be at the end"* is that rule stated.
- ⚠ It also means a mis-picked subclass is not a dead character, which is what makes the 500kk and
  5kkk slots safe to sell.

✅ **A SWAP IS FREE — your ruling, 2026-09-17:** *"The swap below 75 of sub should be free.. You lose
your progress anyways."* Nothing is charged below 75; the levels you throw away (a sub is born at 40,
so up to 34 of them, plus every SP you fed it) are the whole price. At 75 the swap is refused, not
priced. Your earlier *"the removal price"* line therefore refers to the SIGIL clearing in §4 and to
re-levelling the new class, not to a subclass removal fee.

### 7. 🔴 NEW — YOU TAKE A SUBCLASS FROM AN NPC
*"admins can take subclass as its of now ... and normal players also need a NPC to give them (u can
reuse the @40 class master to open new dialogue when u go back to him with main @76+4th)"*.

- The admin path (`HandleDebugAddSubclass`) is **untouched** — it stays the unlimited, ungated one.
- The **level-40 class master** grows a second dialogue, offered when you return to him with a main at
  **76+ holding its 4th class**. Same NPC, same town, a different conversation.
- It consumes a **Subclass Ticket** (§5) and applies the ordinary rules the debug path already
  enforces: every class you own at 75+ with its 3rd class, no repeated discipline, gear unequipped.

### 8. 🔴 NEW — "WHAT DOES THIS SUBCLASS GIVE ME" MUST BE READABLE BEFORE YOU COMMIT
*"we will need an detailed information when taking subclass what that subclass will give you when
reaching 75lvl etc"*. The dialogue in §7 shows, per offered class: the **sigil group it unlocks** and
its three sigils, whether it would open a **sigil SLOT** (its number) or only the tree, that it starts
at **40 with 0 SP** (`BL-252`), and the 1-day rune it comes with. Everything on that panel is derived
from the catalogues — nothing about it is authored twice.

### 9. 🔵 WHAT IS BLOCKING THE BUILD

1. ✅ **THE PREMIUM CURRENCY EXISTS — `BL-257`, built 2026-09-16 in 0.153.0.** PLATINUM: an account
   balance, not an item, with a `PlatinumPrice` on every `ItemDef`, a vendor that charges gold and/or
   platinum, and `/giveplat`. This blocker is gone.
2. ✅ **X / Y ARE 100 / 1,000 PLATINUM**, your 2026-09-16 message; **Z (5,000) is CUT until the
   summoner ships**, your 2026-09-17 ruling. Recorded in §5.
3. ✅ **§6's swap price is FREE below 75** — your ruling, 2026-09-17.
4. ✅ **§5's cap is settled** — the ladder is trimmed to seven rungs, matching the seven reachable
   subclasses.
5. ❓ **A READING, and it does not block: §4's clearing price.** `SigilResetGold` becomes 100kk
   **per sigil** unless you say it wipes all three for one payment.
6. 🔴 ❓ **NEW, AND IT IS A REAL INTERACTION YOU MAY NOT HAVE SEEN. The COMPLETENESS GATE now bites on
   a BOUGHT slot in a way it never did on an earned one.** The rule — *every class you own must be at
   75 with its 3rd class before you may add another* — predates this entry and you have never repealed
   it, so **0.155.0 kept it**. But look at what it does against the ladder: an EARNED slot is paid BY a
   subclass reaching 75, so the gate is satisfied by construction and you never notice it. A **500kk or
   5kkk slot can be bought at any moment** and then sits unusable until every other class you own is at
   75. **Should a bought slot bypass the gate?** My reading if you say nothing: **keep it as built** —
   it is your existing rule, it is what stops half-levelled subclasses stacking up, and a bought slot is
   never lost, only waiting. But it is your call and it costs one line either way.
7. ✅ ~~`GameConstants.MaxSubclasses` is 4~~ — done in 0.155.0, and it is **derived from the ladder**
   rather than typed (`1 + SubclassSlots.MaxSlots`), so restoring the 8th rung moves it too. The gate
   that actually binds is the per-character `SubclassSlotsUnlocked`, persisted, starting at 0.

### 9b. ✅ WHAT IS BUILT (0.155.0) AND WHAT IS LEFT

**BUILT, server-side and SmokeTest-covered:** the slot ladder and its prices (`Game.Shared/SubclassSlots.cs`);
the **Subclass Ticket** item; the two persisted counts; the three earned triggers (main 76+4th, and the
first two subclasses reaching 75) fired from level-up, the 4th-class change and login; the ticket
purchase gated on the COMPUTED *"no more available subclasses"*; **the free swap-out at ≤74** with the
duplicate check run as if the outgoing class were already gone; the player-facing `TakeSubclass`
alongside the still-ungated admin path, both through one `CreateSubclass`; and §7+§8's dialogue payload
(`SubclassOfferInfo`) with the sigil group, the sigils, the slot it would open and the swappable rows —
all of it derived.

🔵 **LEFT: (a) the CLIENT — drawing that dialogue and the info panel, plus using a ticket from the bag.
APK, protocol 39. (b) the SIGIL half, §1-§4.** `SkillCatalog.SigilGroupOf` already exists and is used
for display; what is not built is the GATING — three identical slots opened by a subclass at 75, groups
unlocked by owning a subclass of them, commit free, clearing 100kk.

### 10. ⚠ For the record
Three sigils today = level 76 + 60kk SP + 30kk gold on ONE character. Three sigils after this = **three
subclasses each levelled to 75**, each of them born at 40 with no SP. The money is gone and the ROAD is
the price — which is what *"yes sigils become end game and hard"* asks for.

✅ **Characters who already own three sigils: nothing is built.** *"development -> db is reset
periodically - no need for migrations"*, the standing pre-release rule.

⚠ The client's Sigils tab is built around the three named slots and gets rebuilt with them; the class
master's new dialogue and §8's panel are client work too. **This ships with an APK.**

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


## `BL-260` ❓ SUMMONERS — the conversation we have never had

**Opened 2026-09-17 on your instruction** (*"make a note to discuss summoners"*). Nothing here is a
proposal; it is the list of decisions that are ALREADY SHIPPED and already assume a summoner exists.
The point of the entry is that the discussion is owed **before** any of them is built on further.

`BL-38` (*"Pets and summons — immovable totems, class pets, the mage summoner"*) is the old,
never-scheduled design note. This is the **live** one, because four things now depend on it:

| what already assumes it | where | what it assumes |
|---|---|---|
| the **8th subclass slot** | `BL-250` §5 | cut until *"summoner is build"* — 5,000 platinum comes back with it |
| the **mage sigil group** | `BL-250` §3 | a mage can only reach his OWN group once nuker ↔ summoner is a second **path** |
| the **path count** | `BL-255` | 8 paths today; the summoner is the **ninth**, and that is what makes eight subclasses reachable |
| the **roster shape** | `CLAUDE.md` | *"EIGHT choosable paths per race and 24 third classes"* — a ninth path moves both numbers |

### What has to be decided, and none of it is decided
1. **Is it a base class, a DISCIPLINE of the mage, or a 3rd-class branch?** `BL-255` makes this the
   load-bearing question: `CanAddDiscipline` refuses a second class on the same **path**, so whether
   the summoner is a new path or a new name on the nuker's path decides whether a mage can ever
   subclass into it — which is the whole reason the mage group is locked today.
2. **Three race names, like every other discipline?** The roster is `Discipline` values; a ninth path
   is three more of them plus three 4th-class names.
3. **What is a summon, mechanically?** `BL-38` names three different things — an immovable totem (which
   already exists, `PlacesTotem`), a class pet, and a summoner's creature. The engine has no
   *controlled second body* at all: no ownership, no commands, no threat inheritance, no XP split.
   That is the real cost, and it is not small.
4. **Does it break "a class grants NO stats — identity is the KIT"?** A pet IS stats, delivered
   sideways. Either the pet's numbers derive from the owner's, or the rule gets its first exception.

⚠ **Until this is answered, do not let a fifth thing depend on it.** Each of the four above was a
one-line "…when the summoner ships"; that is cheap once and a trap four more times.

---

