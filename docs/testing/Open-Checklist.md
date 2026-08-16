# OPEN CHECKLIST — after playtest 24

> **Rolling and unversioned.** Playtest 24 (2026-08-16) ran the 0.68.0 APK: `85b`, `85e`, `85f`, `85g`,
> `85h`, `85i`, `85m` and `86c` all came back `[x]` and are **gone from this file** — they live in
> [Playtest-Archive.md#playtest-24](Playtest-Archive.md#playtest-24) with your comments verbatim. What is
> below is the four rows you marked `[~]`, the eight you never reached, and the **new §87** — the two bugs
> and the four changes that pass produced. **Nothing in §87 is built yet**; it is here so it is not lost.
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

## 87. PLAYTEST-24 FINDS — ⚠ NOT BUILT YET, nothing here is testable

These are your two `[!]` finds and the four changes your `[~]` rows asked for. Rows stay `[]` and the
bodies get rewritten with test instructions the moment each one is built.

- `87a` [] - 🔴 **REFLECT FLAGS THE DEFENDER — your anti-PK exploit.** *"Reflect should not flag me -
  That's a big anti pk exploit...som1 comes to me and wants to kill me but I don't want to ..so he hits me
  see I become pvp flag and he just kills me."* Not yet diagnosed in code. ⚠ **Which reflect matters**:
  `81b` Deflection and `81c` Backlash have never been reached in two passes, so what you hit is almost
  certainly the **armour sets' `MeleeReflect`** (5%, basic attacks only) — but the fix has to cover all
  three paths or the next one you meet does the same thing. ->

- `87b` [] - 🔴 **THE SYSTEM CHAT TAB LAGS THE GAME.** *"System chat lagging the game ... Other tabs don't
  just system(respectedly and 'all')"* · *"actually my 1st admin acc have a problem with the game chat ....
  After fame restart it wirks."* 🔑 Two facts that narrow it a lot: only **System and All** (the two tabs
  that receive every message rather than a filtered slice), and **a restart clears it** — so it is state
  that accumulates in the client, not the transport. ->

- `87c` [] - 🔴 **THE PVP FLAG AS THE AOE TARGET FILTER** — your rule from `85a`, now **`BL-77`** because it
  is a system, not a flare fix: pvp-off = an area skill reaches creatures only · pvp-on = it reaches
  players **and flags you on the reach, not on the damage** · a no-damage skill castable on a player is
  monster-only with PvP off and flags with it on. 🔑 **Same principle as `87a`** — the flag follows
  **intent**: what you deliberately do flags you, what your gear does back to an attacker does not.
  ⚠ Three shape questions on the `BL-77` entry (party members inside the area; whether a *heal* aimed at a
  stranger counts; whether the person revealed is flagged too). ->

- `87d` [] - **THE TARGET FRAME CLIPS ITS FIRST ROW** (`85k`). *"the text 'mob:..' is hidden. Same problem
  we had with every window and the text inside ..the texblock don't take into account the title row. And
  the 1st text is half hidden."* ⚠ You call it the **generic** window bug rather than one frame's mistake,
  so the fix is in the shared panel layout, and every window gets re-walked after it. ->

- `87e` [] - **THE CHAT / COMBAT WINDOWS — four separate asks** (`85l`). They move and resize; what is
  wrong: **(a)** the combat window *"cannot go left below certain distance"*; **(b)** the resize is
  **inverted** — *"I drag down it goes from bottom to top increasing is height but the bottom is the frozen
  position. The drag button should move not thebtop/left"*; **(c)** *"remove the row with 'clear' and
  'replay' it's now an empty space and the text never gets to the bottom"* — move them **up beside L/U**,
  clear as a **bin** icon, replay as a **speech bubble**; **(d)** the grip *"is shown only on unlocked so it
  can cut into the text"* while unlocked, and **L/U wants a padlock icon**. ⚠ (c) and (d) are icons —
  check the TMP atlas before picking glyphs, it is static and 250 glyphs. ->

- `87f` [] - **THE GEAR PICKER, AND THE `G3` ANSWER.** Two unrelated halves of `86a`/`86b`.
  **Picker** (`86a`): *"good just make the selection buttons smaller in height and add a header on the
  filtered gear list. Now it's the same row as the grade (needs a splitter)."*
  **`G3`** (`86b`): you ruled **migrate**, and you were right about the measurement. My sweep had two
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
