# OPEN CHECKLIST — after playtest 24

> **Rolling and unversioned.** Playtest 24 (2026-08-16) ran the 0.68.0 APK: `85b`, `85e`, `85f`, `85g`,
> `85h`, `85i`, `85m` and `86c` all came back `[x]` and are **gone from this file** — they live in
> [Playtest-Archive.md#playtest-24](Playtest-Archive.md#playtest-24) with your comments verbatim. What is
> below is the four rows you marked `[~]`, the eight you never reached, and **§87 — the two bugs and the
> four changes that pass produced, all six now BUILT in 0.69.0** and carrying test instructions.
>
> 🆕 **§88 is the mob demo** (`BL-47` step 2, 0.70.0) — the only section here that is a DESIGN call
> rather than a fix. ⚠ **Server-only: the 0.69.0 APK you already have works against it**, so it costs
> you nothing extra to walk over and fight them.
>
> ⚠ **Your marks came to me as an uploaded copy, not through the repo.** The working tree was untouched,
> so if you edit this file in place next pass, say so — otherwise I will look in the wrong place again.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
**Your own "My Finds" section is at the top** — keep using it, it worked twice now.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids.

---

## My Finds — next pass

- [ ] 

- [ ] 

- [ ] 

- [ ] 

- [ ] 

---

## 0. ANSWERS I OWE YOU — read, don't test

### ✅ Closed by playtest 24

- ✅ ~~**`85j` the boss EXP number**~~ — **you parked it yourself**: *"it still feels low for a 90 lvl boss.
  Leave it like that after we have the 40/76 kits the time will change and we will need to boost bosses and
  doing so will increase the exp propotionally."* So the **0.25 respawn exponent stays as it is** until
  `BL-02` lands, and the boost comes with the kits. Recorded so nobody re-tunes it in the meantime.
- ✅ ~~**`G3` §8-A, the mob attribute display**~~ — `86c` came back `[x]`. The two rows are gone and
  nothing about how a creature fights changed.
- ✅ ~~**`G3` §8-B, migrate or finish the passives**~~ — **you ruled: migrate** (`86b`). `BL-47` is rewritten
  around your answer and the superseded recommendation is in `BacklogArchive.md`. **Three questions of mine
  are on that entry and step 2 waits on them** — see §87f below.

### 🔴 Still yours to rule

- 🔴 **`BL-13` the boss curve — the ×100 IS landing, and that is not the problem.** A level-20 field
  boss spawns with exactly 36,000 HP. But against your six-minute / 3-DD target a single flat multiplier
  swings **11×** across the game: TTK **80s at 20**, 296s at 40, 684s at 60, **888s at 76**. Mob HP grows
  as `0.8·L²` while a geared party's DPS is nearly flat (448 → 525). Two decisions are yours: *should a
  level-20 field boss really take a level-20 party six minutes*, and do the late bosses come **down** to
  360s or does the target itself **rise** with level.
  🔵 **The world boss has no rank to live in** — your *"an hour for ~10 parties"* is ~**167×** a field
  boss, which is a new rank with its own drops, phases and lockout, not a bigger number.
  ⚠ Your `85j` park makes this **more** urgent, not less: you have deferred the EXP number to the kits,
  and the EXP number is derived from the kill time this entry is about.

- 🔴 **`BL-49` — the levelling curve, not the boss rule.** One **level-20** field boss is **125% of a
  level** solo while a level-85 one is **0.1%** — the same 150 trash kills either way. §85j moved the
  boss multiplier where you asked, and that spread survives it untouched, because it is the EXP curve.

- 🔴 **`BL-22` salvage: the S row cannot be moved by this feature at all.** Your budget was *"10~20%
  decrease in time"*; the early rungs got exactly that (E −3% · D −10% · C −18%) and **A and S got −0%**.
  The cause is your own *"rarity for mats rarity"* mapping: salvage pays the rarity of gear that
  **drops**, and a normal mob and an **elite both cap at Epic** — only a boss (0.09 kills/h) drops
  Legendary. The A and S recipes bind on **Legendary Ingot**. At a uniform quantity of 20 the early rungs
  collapse to −24/−39/−72% and **A and S still move 0.00%**. `M13` in BalanceMatrix prints all three.
  ⏸ Parked with the rest of crafting.

- ⚠ **The buff-vs-heal threat ratio is off by ~8×, and the buff is not the wrong half.** You sized it
  against a ~1500-power quick heal at 70; the cleric's heal ladder stops at skill level **4** (learned at
  35, power **301**) because everything above it is blocked on `BL-02`. **`BL-16`** is the half that has
  not caught up — and `BL-71` made it load-bearing rather than cosmetic.

- ⚠ **Numbers that are mine, not yours** — each flagged in the source: the top rung of **Madness**; the
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses; and now the
  **0.25 respawn exponent**, which your `85j` park leaves standing as mine.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a third time on purpose: §79c moved the block channel and you passed it, so moving
  these now would make the next reading un-attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable* · `0`, `-` or omitted → use the
  catalog's price · any positive number → that exact price (`k`/`m`/`b` and `1_000_000` both parse).
  Every argument after the item id follows the same rule: `-` is always *no opinion*.

---

## 88. THE MOB DEMO YOU ASKED FOR — ✅ BUILT (0.70.0), and it is the one thing here that is a DESIGN call

`BL-47` step 2: *"and later we can do 2~5 mobs so I can test."* Five creatures built through the PLAYER
pipeline, each standing beside the ordinary creature of its own level. ⚠ **Server-only — the 0.69.0 APK
you already have works against this server**, so this rides along with playtest 25 at no extra install.

**Where:** any gatekeeper → **Proving Grounds** (a second gate on the Training Grounds, on the row
south of the dummies). Five columns; in each one the **player-built** creature is NORTH and its
**curve twin** — an ordinary mob of the same level, no passives, same weapon — is directly SOUTH.
Nothing attacks on sight and nothing drops loot: kill one, turn round, kill the other.

| col | player-built | Lv | its twin |
|---|---|---|---|
| 1 | Goblin Raider | 40 | Standard Marker (Lv 40) |
| 2 | Goblin Elder Raider | 45 | Standard Marker (Lv 45) |
| 3 | Cairn Lich (caster) | 60 | Standard Marker (Lv 60) |
| 4 | Fallen Seraph | 80 | Standard Marker (Lv 80) |
| 5 | Fallen Seraph, Runebearer | 80 | Standard Marker (Lv 80) |

Inspect any of them and the target window now says what it is built from — its weapon, its armour and
anything it holds.

- `88a` [] - 🔴 **COLUMNS 1 AND 2 — YOUR ±5 BAND.** These two are the SAME authored loadout five levels
  apart, and both carry **no passive at all**. Measured, defence and HP hold across the band (P.Def
  x1.04 → x0.95) but **P.Atk falls x0.87 → x0.64** — a quarter of its damage, because the mob attack
  curve is the steep one. It is left that way on purpose so you can feel it instead of reading it.
  **Test:** fight column 1's pair, then column 2's. **The question is whether the Elder Raider feels
  too soft for its level** — if it does, a band needs one attack number and *"prefixed 100+ mobs with
  +-5 lvl ranges"* costs one more column in the table. If it does not, the band is free. ->

- `88b` [] - 🔴 **COLUMNS 4 AND 5 — THE RUNE vs THE PASSIVE, and this is the one that decides the
  design.** They are identical creatures. #4 gets its damage from an **authored ×2.07 attack passive**;
  #5 has **no attack passive at all** and instead **holds a War Rune**. On the numbers they land in the
  same place (x1.00 vs x0.97). **Test:** fight both and tell me if they feel the same. If they do,
  **the whole attack side of this design becomes an item a creature carries** — no per-band table, no
  drift with level, and a creature that visibly holds the thing that makes it dangerous. ⚠ The rune is
  **held, never dropped** — you cannot loot it, which is your own *"not a dropped one..but just to
  hold stuff"*. ->

- `88c` [] - **COLUMN 3 — DOES A ×3.7 HP LICH READ AS A CASTER OR AS A SPONGE?** A caster creature is
  the one archetype gear cannot reach: its HP needs a passive far past your ×2, which your own *"and hp
  boost"* anticipated. On the numbers it lands exactly on the curve. **Test:** kill the Cairn Lich and
  then its twin, and say whether the fight lengths feel alike. This is a FEEL question — the arithmetic
  already agrees. ->

- `88d` [] - **NOTHING ELSE IN THE WORLD CHANGED, and that is worth one look.** These nine templates
  are fenced out of the generated rosters, so no ordinary field should have gained a Goblin Raider or a
  "Standard Marker". **Test:** hunt anywhere at 40-45 or 76-85 for a few minutes and confirm you meet
  only the creatures you always met. If one of these shows up in a real field, say so — that is the
  fence leaking, and it is one line. ->

- 🔵 **WHAT I OWE YOU AFTER THIS: "then we do a system number."** The demo says the machinery works; it
  does not say how many creatures to build with it, and that number was always yours. Two decisions come
  out of the fights above — whether a band needs its own attack number (`88a`), and whether a creature's
  damage comes from a rune or a passive (`88b`). Everything after that is authoring.

---

## 87. PLAYTEST-24 FINDS — ✅ ALL BUILT IN 0.69.0, test them

Your two `[!]` finds and the four changes your `[~]` rows asked for. **Everything below is in the
0.69.0 APK + server.** No DB reset, protocol unchanged.

- `87a` [] - 🔴 **REFLECT NO LONGER FLAGS THE DEFENDER — your anti-PK exploit, fixed.** *"Reflect should
  not flag me — that's a big anti pk exploit...som1 comes to me and wants to kill me but I don't want to
  ..so he hits me see I become pvp flag and he just kills me."* You were exactly right about the
  mechanism: reflect damage runs through the same damage function as a real blow with the roles
  SWAPPED, so the code that flags "the attacker" was flagging you.
  **Test:** two characters outside town, wear an armour set with reflect, turn YOUR PvP **off**, let the
  other one (PvP on) hit you until a `Reflect` line appears in the combat feed. **Your name must stay
  white** and he must stay purple. Kill him with reflect if you can — that should still count as a
  normal PvP kill, not a PK, and give you no karma. ⚠ **All three reflect paths were covered**, so if
  you ever reach `81b` Deflection or `81c` Backlash, check the same thing there. ->

- `87b` [] - 🔴 **THE SYSTEM/ALL CHAT TABS NO LONGER LAG.** Your two clues were the whole diagnosis:
  only System and All, and a restart cures it. Those are the only tabs the 1000-line buffer actually
  fills, and every time the window was reopened or the tab was switched the client rebuilt **the entire
  buffer** — up to 1000 text rows in one frame — then threw ~880 of them away immediately. It now draws
  only as many rows as the window can hold.
  **Test:** play until the log is well filled (an hour of anything), then switch tabs back and forth
  onto System and All repeatedly, and close/reopen the chat window. It should be instant, and **it must
  not get worse the longer the session runs** — that "worse over time, fine after a restart" is the
  exact symptom to watch for. ->

- `87c` [] - 🔴 **THE PVP FLAG IS NOW THE AOE FILTER** — your rule from `85a`, built as `BL-77`.
  **Test with PvP OFF:** an area skill and the flare reach **creatures only** — a player standing in
  the radius is untouched and unrevealed, and the flare tells you so instead of saying "nobody was
  hiding". **Test with PvP ON:** the same cast reaches players, reveals a hidden one, **and flags you**
  — including when it deals no damage at all, which was the flare's whole complaint. A taunt or a
  cancel aimed at a player flags you now too; it never did.
  ⚠ **Three things I decided, because they were open and none of them blocked the build. Say if any is
  wrong:** (1) **your own party is never touched**, PvP on or off, same as every other system here;
  (2) **support is not routed through this** — a heal or a buff on a stranger keeps its own rule, since
  it is castable on a player but is not an attack; (3) **only YOU flag** — the person your flare
  revealed did nothing, and flagging him is your own exploit wearing a different coat. ->

- `87d` [] - **THE TARGET FRAME'S FIRST ROW IS NO LONGER COVERED** (`85k`). ⚠ **You read the cause as
  the title row and it was the opposite end** — the `Mob: 44, Aggressive` line was under the **Attack
  button**, not under the title bar. It is the other half of the shrink you asked for in playtest 23:
  the rows below the title moved up with the deleted name row, and the buttons, which hang off the
  panel's floor, moved up with it — into them. The frame now has ONE button row (five of the seven
  buttons have been hidden since playtest 23 anyway) and 12px more height.
  **Test:** target a mob — the level/aggro line must be fully readable with the Attack and Info buttons
  clear of it. Then an NPC (Talk) and a player (no buttons).
  ✅ **You called it the generic bug, so all 23 windows with a title bar were checked. You were right
  twice**: the **trade** window's partner-name line was biting 6px out of both column headers — fixed,
  and its row positions are now measured off the title bar instead of a hand-picked number. The other
  21 are clean. Worth a glance at the trade window while you are in one. ->

- `87e` [] - **THE CHAT / COMBAT WINDOWS — all four asks** (`85l`).
  **(a)** The combat window reaches the left edge now. The clamp assumed every window was centred and
  neither of these is, so a right-pinned window could be dragged far off the right and stopped dead
  just past the left. **(b)** Resize follows the grip: dragging down grows the window downward and the
  **top-left corner stays put**. **(c)** The Clear/Reply row is gone, the text runs to the bottom of
  the window, and both are icons in the title bar beside the padlock — **bin** = clear, **speech
  bubble** = reply. **(d)** The grip no longer vanishes when you lock; it stays and dims, so the corner
  it occupies is the same corner at all times.
  ⚠ **The three icons are DRAWN, not font characters** — the bundled font has no bin, bubble or padlock
  and would have given you the hollow box you have reported twice. Tell me if any of them reads as
  something else at that size; they are shapes I can adjust. ->

- `87f` [] - **THE GEAR PICKER** (`86a`) — *"make the selection buttons smaller in height and add a
  header on the filtered gear list. Now it's the same row as the grade (needs a splitter)."* Both done:
  chips are 28px instead of 34, and the list now opens with a header naming what it is filtered to.
  ✅ The `G3`/`86b` half of this row is closed — it is design, not something to test; it is the block
  below, kept here because it is the record of what you ruled. ->

- **`G3` / `86b` — NOTHING TO TEST, this is where the answer is written down.** You ruled **migrate**,
  and you were right about the measurement. My sweep had two
  blind spots you found: it stopped at **+16** enchant, and it moved every slot **together**, so your
  *"S grade Mace +60 and B grade leather"* was never constructed. **`G3.7` re-runs it your way:
  12 of 16 archetype-levels land inside your ×2 passive on all four stats at once**, worst miss
  **185-221% → 94%**, biggest attack passive still needed **×1.60** — and the search picked your loadout
  on its own. ⚠ **The four failures are one failure, the Nuker's HP** (×2.01 → ×3.48), which your *"and hp
  boost"* already covered. `BL-47`, `MobsAsPlayers.md` §6/§8 and `BacklogArchive.md` are all rewritten.
  ✅ **You answered all three the same day** (race = a flat ±5, no curve · demo first, roster number after ·
  a mob may hold a non-droppable inventory, so **yes to the rune**), plus *balance against normal mobs* —
  which every `G3` number already did. 🔑 **And your roster ruling is ~90% built already**: `MobCatalog`
  holds **80 templates each with its own natural level**, ~2 levels apart, and a natural level already
  beats the zone band — only the **±5 variance** and ~20 templates are missing. That **retires `G3.3`**:
  no level→grade function is needed after all. **Step 2 is unblocked**; §7 of the doc is rebuilt around
  races, the split loadout and a held rune. Still open: §8 **C/D/E**. ->

---

## 85. NEVER REACHED IN PLAYTEST 24 — still owed from the 0.68.0 batch

✅ `85b` `85e` `85f` `85g` `85h` `85i` `85m` all passed — [see the archive](Playtest-Archive.md#playtest-24).

- `85c` [] - 🔴 **NO TAUNT HAD EVER FIRED FROM THE AUTO CHAIN** — *"Provoke is not auto used in any
  form."* It sorted into the never-cast bucket, because the debuff test asks for a contested effect or a
  debuff school and a taunt is neither. Taunts have their own rung now, **above Attack**: a tank's attack
  chain is never idle, so anything below it would fire on no tick at all.
  🔑 **This is also the answer to *"check the cyclic logic ...I feel there is a problem"*.** The cursor
  walk itself is correct — each priority group keeps its own place in your bar and wraps. What made it
  feel broken is that an armed row the chain cannot cast was skipped **in silence**. It now tells you
  which rows those are the moment you save.
  🔑 **And your other question — the Basic Attack row's POSITION is irrelevant.** It is a toggle, not a
  chain entry. **Put a tank on auto with Provoke armed and watch it hold a mob.** ->

- `85d` [] - 🔴 **MOB SOCIAL CLANS ARE OFF, AND NOTHING WAS DELETED.** Your instruction, and `BL-73` is
  the note you asked for. 🔑 What you hit is **spawn density, not the 450 radius**: every camp generates
  on nearly one point, so a cry reaches all of it at once. One switch (`GameConstants.MobClansEnabled`);
  the twelve clans stay authored and every line of the call code stays live. `Lure` is untouched.
  **Hit one wolf and confirm you fight one wolf.** ->

- `85n` [] - 🔑 **YOUR EIGHT 40+ CSV FILES EXIST, SEEDED.** `melee rogue` · `archer` · `healer` · `buffer`,
  each `40-74` and `76-85`, in `docs/data/classes_skills_csv/`, in the 40+ format (the 20-35 header plus a
  trailing `RACE` column). They hold **exactly what the game already registers above 40** — nothing is
  invented, `BL-02` still stands. Generated by `tools/SkillCsvSeed`, which **refuses to overwrite**.
  ⚠ **Four of the eight are nearly empty and that is the honest picture.** ⚠ **`Vanish` shows an SP cost
  of 1** (the record default) precisely so you can see it and price it. **Nothing to test — read them.**
  🔴 Second pass in a row untouched, and it is still the single biggest unlock in the project. ->

---

## 81. THE TWO REFLECTS — never reached in playtest 23 OR 24

⚠ **These two now matter more than they did**: `87a` says a reflect flags the defender, and these are the
other two reflect paths. Test them in the same sitting as that fix.

- `81b` [] - **`Deflection` — physical-skill reflect, warrior.** *"default warrior @40 → 0.15 chance ×1
  reflected; @76 → 0.3 chance ×1 reflected."* Your numbers verbatim: the fraction stays **×1.0** at both
  rungs and only the **chance** moves. A landed physical skill rolls the victim's chance; on a hit the full
  damage goes back at the caster, **who can die to it**. Kept separate from the armour sets' `MeleeReflect`
  (5%, basic attacks only) — no blow is ever taxed by both, and two Deflection warriors terminate after
  one bounce. ->

- `81c` [] - **`Backlash` — debuff reflect, tank, 30%.** *"tanks get 30% chance to reflect a debuff →
  u cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff
  paths, because a bounce is not a resist: a tank who throws your stun back was never tested against
  it. The caster gets the effect with no resist roll of their own and no second bounce. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [~] - **Nuker vs champion, unbuffed.** Half of this closed itself in playtest 23: the **mage** can
  now farm solo (`79i`). The **champion** half is untouched — *"they both have hard time to farm without
  buffs"* — and is `BL-72`. Do it in the same sitting as an auto-farm run. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row. ->

- `13a` [] - The **"take a break" banner**. ⚠ **Still at 10 MINUTES** — it was set there at your request
  for the 0.68.0 pass and tagged in the source to go back to 3h, and playtest 24 did not reach it, so it
  stays until you have actually read one (`GameConstants.BreakReminderSeconds`). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**

- **3rd/4th class kits** — blocked on your **40+ CSVs** (`BL-02`), still the single biggest unlock.
  🔑 The authoring format is settled in **[docs/design/Disciplines.md](../design/Disciplines.md)**: you
  author **by DISCIPLINE with a trailing RACE column** — **10 CSVs, not 30** — and **six questions in it
  are waiting on you**. ⚠ **`85n` seeded the files**, holding what already exists above 40, so you start
  by editing rather than by an empty sheet. Your `85j` park now depends on this landing.
- **`BL-77` the PvP/AOE flag rule** — new this pass, queued 🔴; the testable half is `87c`.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you; it wants its own ×100-rate playtest.
- **`BL-73` mob social clans** — off by one switch at your instruction, back on when the world map
  spreads the camps out.
- **`BL-74` the game launcher** — still not treating the app as a game; research owed.
- **`BL-76` boss skill gems** — recorded, not built; five shape questions on the entry.
- **`G3` mobs-as-players** — ✅ documented, and **you ruled: migrate**. It now waits on the three
  questions in `87f`, not on work.
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
