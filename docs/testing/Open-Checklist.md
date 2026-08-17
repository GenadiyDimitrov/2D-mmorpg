# OPEN CHECKLIST — after playtest 25

> **Rolling and unversioned.** Playtest 25 (2026-08-16/17) ran the 0.69.0 APK against the 0.70.0 server.
> **Every built row came back `[x]`** — all five 0.69.0 fixes (§87) and all four mob-demo rows (§88) — so
> those two sections are **gone from this file** and live in
> [Playtest-Archive.md#playtest-25](Playtest-Archive.md#playtest-25) with your comments verbatim.
>
> 🔑 **The pass was your eight free-form finds, not the rows.** Where each one went is §89 below.
> Nothing from playtest 25 is built yet, so **there is nothing new to test in this file today** — what is
> below is the four rows that were never reached in the last three passes, and the answers I owe you.
>
> ✅ **Your marks were in the repo this time, not an upload.** That is the right way round — keep doing
> it and I will always find them.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
**Your own "My Finds" section is at the top** — keep using it, it has worked three passes running and
it is now where most of the real content arrives.

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

### ✅ Closed by playtest 25

- ✅ ~~**`BL-13` the boss curve**~~ — **you ruled, and it inverts the question I asked.** *"bosses should
  take 10-15 even 30 mins to kill ... A 3 min boss is not a boss its a stronger elite mob."* So the
  target **rises**; the late bosses were right all along (60 → 684s, 76 → 888s, 85 → 693s are all inside
  600-1800s) and **only levels 20 and 40 are wrong**, at 2-7.5× too fast. My "should the late ones come
  down to 360s" was extrapolated from an old *"6 minutes"* remark, and it was the wrong question.
  The entry now carries three jobs, and only the first is arithmetic: lift the low end, **give a boss
  defence and attack** (today `Boss` is HP ×100 / ATK ×10 with **no defence term at all**, which is
  exactly why it reads as a sponge), and **re-base the target on a real party** — tank + healer + DDs —
  since the current table measures three DDs and no healer. ⚠ Your `85j` EXP park partly resolves itself:
  boss EXP is derived from kill time.
- ✅ ~~**`88b` the rune vs the passive**~~ — *"they relatevley feel the same"*. **The attack side of a
  player-built creature is an item it carries.** No per-band attack table, no drift with level.
- ✅ ~~**`G3` §8, the mobs-as-players verdict**~~ — you fought them and the answer is a **split**, not a
  yes or a no. See `BL-47`; the one open question is restated in §89 below.

### 🔴 Still yours to rule

- 🔴 **`BL-47` — the ONE question left on mobs-as-players, and it is a yes/no.** You marked the demo
  *"It works"* and then named its real cost: *"with current mobs we can say 'this one will have x2 hp'
  and whole the mobs on the field are altered.. while with the pMobs we will alter one and it will be
  good in the lvl range (+-5) not across the board."* **That is correct and structural** — one function
  moves every creature; a per-creature loadout has to be re-authored one at a time. But you also named
  where they *should* go: **town guards** and **fortress sieges**, both hand-placed and few. So:
  **do ordinary field creatures stay on the `MobBaseStats` curve with ×2 passives, and player-built mobs
  become a hand-placed CONTENT tool instead of the general pipeline?** Everything already built serves
  that shape unchanged. Say yes and `BL-79`/`BL-80` are the roadmap.
  ⚠ **One thing from the demo you never commented on**: `88a`, whether the level-45 Elder Raider felt too
  soft beside the level-40 Raider (its P.Atk falls x0.87 → x0.64 across the band). It only matters if a
  pMob carries a ±5 band at all, so it can wait for the answer above.

- 🔴 **`BL-49` — the levelling curve, not the boss rule.** One **level-20** field boss is **125% of a
  level** solo while a level-85 one is **0.1%** — the same 150 trash kills either way. §85j moved the
  boss multiplier where you asked, and that spread survives it untouched, because it is the EXP curve.
  ⚠ **`BL-13` now sits on top of this**: a boss that takes 3-10× longer carries 3-10× the EXP with it.

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
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses; and the
  **0.25 respawn exponent**, which your `85j` park leaves standing as mine.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a fourth time on purpose: §79c moved the block channel and you passed it, so moving
  these now would make the next reading un-attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable* · `0`, `-` or omitted → use the
  catalog's price · any positive number → that exact price (`k`/`m`/`b` and `1_000_000` both parse).
  Every argument after the item id follows the same rule: `-` is always *no opinion*.

---

## 89. WHERE PLAYTEST 25 WENT — nothing to test yet, this is the routing

Your eight finds, and what each one became. **None of it is built**, so no row here has a `[]` — they
come back as testable rows once they ship.

| your find | where it went | state |
|---|---|---|
| bosses should take 10-30 min, more def + atk, party mandatory | **`BL-13`** — rewritten around your ruling | 🔴 ready |
| mobs feel easy · 80 mob wants **15k HP not 5k** · a caster mob is not squishy · the **IG comparison research** | **`BL-78`** (new) | 🔴 ready |
| town/field **guards** — Lv 80 Mythic t80, aggro on PK only, PK radar, two per town exit | **`BL-79`** (new) | 🔴 ready |
| **fortress sieges** — the whole design, verbatim | **`BL-80`** (new) | 🔵 you said it can be deferred |
| god mode resists every debuff · a **boss** is immune to control only (DoT and stat-down still land) | **`BL-81`** (new) | 🔴 ready |
| an admin can't see he is in god/invis · the stealth opacity rule | **`BL-82`** (new) | 🔴 ready |
| **remove taunt from the auto chain** (`85c`) | **`BL-83`** (new) | 🔴 ready |
| the mob demo verdict | **`BL-47`**, rewritten — one yes/no question left, in §0 above | 🔵 yours |

**Three UI changes have no `BL` id because they are small and go in the next batch**, listed here so
they are not lost:
- **The target window's title row** — *"only the name of the target. No lvl no target.title, now the
  [title + name + lvl] overflows"*; the mob title moves down into the `Mob:` row. ⚠ This is one layer
  further into the frame `87d` just fixed.
- **The chat window's buttons** — *"decreasing the width of the chat leaves the [combat] button floating
  in the air - make the buttons smaller or like the icons on the top"*. Same family as `87e`.
- **The gear picker, second pass** (`87f`) — *"Make the buttons even smaller. Like the tab buttons in
  height. Also there is no splitter bellow the [S 80] button."*

🔑 **What I would build first, if you do not say otherwise:** `BL-78`, because *"mobs feel easy"* is the
one that changes how the game plays, it moves every TTK and farm number the other entries are measured
against, and `BL-13` is partly downstream of it. The three UI changes ride along in the same APK.

---

## 85. NEVER REACHED — still owed from the 0.68.0 batch

✅ `85b` `85c` `85d` `85e` `85f` `85g` `85h` `85i` `85m` all closed — see the
[playtest-24](Playtest-Archive.md#playtest-24) and [playtest-25](Playtest-Archive.md#playtest-25)
archives. `85c` came back as a **reversal** and is now `BL-83`.

- `85n` [] - 🔑 **YOUR SIXTEEN 40+ CSV FILES EXIST, SEEDED.** Re-cut on 2026-08-17 to the discipline map
  and the tier names you gave: `tank` · `warrior` · `war_aoe` · `dual` · `archer` · `buffer` · `healer` ·
  `nuker`, each `3rd` and `4th`, in `docs/data/classes_skills_csv/`, in the 40+ format (the 2nd-class
  header plus a trailing `RACE` column). They hold **exactly what the game already registers above 40** —
  nothing is invented, `BL-02` still stands. Generated by `tools/SkillCsvSeed`, which **refuses to
  overwrite**. ⚠ **Nine of the sixteen are empty and that is the honest picture** — the content is the
  buffer's ladder and, visible in a file for the first time, the **nuker's 20 rows** (Elemental Burst
  1-10, Frost Bind, Glacial Spike, Mana Barrier, Phase Shift…), which no seeded file had ever covered.
  ⚠ **`Vanish` shows an SP cost
  of 1** (the record default) precisely so you can see it and price it. **Nothing to test — read them.**
  🔴 **Third pass in a row untouched**, and it is still the single biggest unlock in the project. Your own
  playtest-25 note names it again without meaning to: *"mage is the only one with @40+ skills"* — that is
  what a missing kit looks like from inside a fight. ->

---

## 81. THE TWO REFLECTS — never reached in playtest 23, 24 OR 25

⚠ **`87a` is now confirmed green**, so the reflect-flag bug is fixed on all three paths. These two are the
other two paths and have still never been played. Check the flag behaviour in the same sitting.

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
  buffs"* — and is `BL-72`. Do it in the same sitting as an auto-farm run. ⚠ **`BL-78` will invalidate any
  reading taken before it lands** — a 3× mob HP change moves every number this row is about. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row. ->

- `13a` [] - The **"take a break" banner**. ⚠ **Still at 10 MINUTES** — it was set there at your request
  for the 0.68.0 pass and tagged in the source to go back to 3h, and neither playtest 24 nor 25 reached
  it, so it stays until you have actually read one (`GameConstants.BreakReminderSeconds`). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**

- **3rd/4th class kits** — blocked on your **40+ CSVs** (`BL-02`), still the single biggest unlock.
  🔑 The authoring format is settled in **[docs/design/Disciplines.md](../design/Disciplines.md)**: you
  author **by DISCIPLINE with a trailing RACE column** — **10 CSVs, not 30** — and **six questions in it
  are waiting on you**. ⚠ **`85n` seeded the files**, holding what already exists above 40, so you start
  by editing rather than by an empty sheet. Your `85j` park depends on this landing.
- **`BL-78` / `BL-79` / `BL-80` / `BL-81` / `BL-82` / `BL-83`** — new this pass, all queued 🔴 except the
  fortress; see §89 for what each one is.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you; it wants its own ×100-rate playtest.
- **`BL-73` mob social clans** — off by one switch at your instruction, back on when the world map
  spreads the camps out. ⚠ `BL-80`'s garrison presumes clans, so it is one of that entry's prerequisites.
- **`BL-74` the game launcher** — still not treating the app as a game; research owed.
- **`BL-76` boss skill gems** — recorded, not built; five shape questions on the entry. ⚠ Read it beside
  `BL-13` now — a 30-minute boss is a different drop proposition.
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
