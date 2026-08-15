# OPEN CHECKLIST — the 0.68.0 pass

> **Rolling and unversioned.** Playtest 23 (2026-08-15) closed the five-version backlog: §79, §75, §80a/b/e,
> §81a/d/e, §82a-d/f/g and §83a all came back `[x]` and are **gone from this file** — they live in
> [Playtest-Archive.md#playtest-23](Playtest-Archive.md#playtest-23) with your comments verbatim. What is
> below is what you did **not** reach, what you **ruled on** and I then built, and the new §85.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
**Your own "My Finds" section is at the top** — keep using it, it worked.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids.

---

## My Finds 0.68.0

- [ ] 

- [ ] 

- [ ] 

- [ ] 

- [ ] 

---

## 0. ANSWERS I OWE YOU — read, don't test

### ✅ Closed by playtest 23

- ✅ ~~**Dark Dominion**~~ — you ruled *"it falls in the category for deletion"*. **Deleted** (§85f):
  the six pieces, the set def and the debug grant. It is the last of the off-ladder gear, after `79e`'s 64.
- ✅ ~~**`55f` / `79i` the solo mage**~~ — **CLOSED by your own verdict**: *"now a mage can farm alone with
  vamp bolt when low on hp, restore when mp is low ... Now it works."* That also answers the half of
  `BL-72` that was about the mage; the **champion** half is still open (`0a` below).

### 🔴 Still yours to rule

- 🔴 **`BL-13` the boss curve — the ×100 IS landing, and that is not the problem.** A level-20 field
  boss spawns with exactly 36,000 HP. But against your six-minute / 3-DD target a single flat multiplier
  swings **11×** across the game: TTK **80s at 20**, 296s at 40, 684s at 60, **888s at 76**. Mob HP grows
  as `0.8·L²` while a geared party's DPS is nearly flat (448 → 525). Two decisions are yours: *should a
  level-20 field boss really take a level-20 party six minutes*, and do the late bosses come **down** to
  360s or does the target itself **rise** with level.
  🔵 **The world boss has no rank to live in** — your *"an hour for ~10 parties"* is ~**167×** a field
  boss, which is a new rank with its own drops, phases and lockout, not a bigger number.

- 🔴 **`BL-49` — the levelling curve, not the boss rule.** One **level-20** field boss is **125% of a
  level** solo while a level-85 one is **0.1%** — the same 150 trash kills either way. §85j moves the
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
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses. **`Backlash`'s
  level is no longer one of them** — see `85c`.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a third time on purpose: §79c moved the block channel and you passed it, so moving
  these now would make the next reading un-attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable*, the vendor refuses it outright ·
  `0`, `-` or omitted → **no opinion, use the catalog's price** · any positive number → that exact price
  (`k`/`m`/`b` and `1_000_000` both parse). Every argument after the item id follows the same rule:
  `-` is always *no opinion*.

---

## 85. THE PLAYTEST-23 FIX BATCH (0.68.0)

🔴 **NEW APK** (protocol 20 → 21; half of this is client work). **No DB reset** — nothing persisted
changed shape, so your characters survive this one.

- `85a` [] - 🔴 **SIGNAL FLARE COULD NOT CATCH ANYBODY, EVER** — *"Flare does nothing ...cannot find
  flagged player next to me."* `RevealHidden` walked the **party-support** enumeration: caster + party
  members, and it deliberately skips hidden players so a party heal cannot silently find someone nobody
  can see. Both halves are exactly wrong for a flare, whose whole subject is a hidden NON-party enemy —
  the two rules cancelled and left a no-op with a success message. It walks the grid itself now.
  ⚠ It is also **learned at 60 on the archer** now, not 28 on the rogue (`85b`), so testing it needs a
  level-60 archer or `/give`-level staff help. **Two clients: hide one, flare from the other.** ->

- `85b` [] - 🔴 **THE THREE HIDE SKILLS ARE WHERE YOU PUT THEM.** **`Prowl` → every melee rogue @40**
  (it was rogue 20, which handed the dagger's stance to every future archer as well), **`Signal Flare` →
  every archer @60**, **`Vanish` → every melee rogue @60, cooldown 2 min, duration 30s** — your numbers,
  all three learned normally for SP.
  🔑 The counter now sits **level with** the thing it counters instead of twelve levels below it, and the
  2-minute reuse is what gives the flare's 30s no-hide stamp any meaning at all: at the old 30s cooldown
  the stamp expired the same moment Vanish came back.
  ⚠ "Melee rogue" and "archer" are **three disciplines each** — the archer merge splits the rogue by race
  at 40 — so this is six registrations, not two. ⚠ **Vanish still costs 1 SP**; that number is yours (see
  `85n`). **Check all three appear in Learn at the right level and nowhere else.** ->

- `85c` [] - 🔴 **NO TAUNT HAD EVER FIRED FROM THE AUTO CHAIN** — *"Provoke is not auto used in any
  form."* It sorted into the never-cast bucket, because the debuff test asks for a contested effect or a
  debuff school and a taunt is neither. Taunts have their own rung now, **above Attack**: a tank's attack
  chain is never idle, so anything below it would fire on no tick at all.
  🔑 **This is also the answer to *"check the cyclic logic ...I feel there is a problem"*.** The cursor
  walk itself is correct — each priority group keeps its own place in your bar and wraps. What made it
  feel broken is that an armed row the chain cannot cast was skipped **in silence**. It now tells you
  which rows those are the moment you save. (What is left in that bucket is the handful that genuinely
  should not autopilot: a hide, a reveal, a trap, a resurrection.)
  🔑 **And your other question — the Basic Attack row's POSITION is irrelevant.** It is a toggle, not a
  chain entry: the code asks "is that row enabled anywhere in the bar". Slot 1 or slot 9 behaves
  identically. **Put a tank on auto with Provoke armed and watch it hold a mob.** ->

- `85d` [] - 🔴 **MOB SOCIAL CLANS ARE OFF, AND NOTHING WAS DELETED.** Your instruction, and `BL-73` is
  the note you asked for. 🔑 What you hit is **spawn density, not the 450 radius**: every camp generates
  on nearly one point, so a cry reaches all of it at once. Your target shape — *"it will call ONE, and
  while you fight, if others wander in the social range they will aggro"* — is what this same radius
  already does once a camp occupies real ground, so the retune when it comes back is the SPACING.
  One switch (`GameConstants.MobClansEnabled`); the twelve clans stay authored on the mobs and every line
  of the call code stays live. `Lure` is untouched. **Hit one wolf and confirm you fight one wolf.** ->

- `85e` [] - 🔴 **UNDYING WILL / RITE OF PRESERVATION ARE A DEATH PROMPT NOW.** *"I want phebyx blood - u
  die -> u stay dead until you click the resurrection prompt."* You die properly — aggro sheds, karma
  applies, the exp penalty applies, auto-hunt stops, buffs survive — and then you are offered a
  resurrection **where you fell, that never expires**. Accept and you rise at 30% with the exp back;
  decline and you are dead with the ordinary town respawn, your *"else back to town"*.
  🔑 One call changed. Everything you listed as *"the hole pipe"* was already running before that line.
  ⚠ **The heal-at-0 shape you liked for a warrior is not lost** — it is `Last Stand` (survive a fatal blow
  at 50%), already in the catalog, needing only a class and a level. That is 40+ authoring: **`BL-75`**.
  **Die on the level-90 admin wearing one and take your time answering.** ->

- `85f` [] - 🔴 **THE RES FLAG IS PAID WHEN THE CAST STARTS.** *"the flag should happen at the initializing
  the resurrect ..not after the dead agrees."* It was charged after a 10s channel AND a prompt the corpse
  might never answer — so the whole window in which the res could be contested was a window in which the
  resurrector was untouchable. Both the skill and the scroll charge it now, and an interrupt does not
  refund it: you were visibly holding a channel over an outlaw's body. ->

- `85g` [] - 🔴 **THE RESURRECTION SCROLL WORKS.** *"cannot use scroll of resurrection (cleric skill
  works) but scroll says 'need a fallen ally as its target'."* The server has had a targeted item-use path
  since the scroll shipped; **this client only ever called the untargeted one**, so the scroll validated a
  target that was never sent and refused itself every time. The cleric's skill worked because a cast has
  always carried its target. **Select a corpse and read one.** ->

- `85h` [] - 🔴 **THE DROP TAB SHOWS THE LEVEL PENALTY** — *"there should be the same penalty as exp/sp
  when mob and player have a difference and that penalty is not displayed."* The kill roll always applied
  it; the list never did. 🔑 The rune is what made it wrong rather than merely incomplete: both are
  per-player scalars on the same roll, so showing one and hiding the other **certifies the number as
  personal and then has it wrong by up to 100%**. A header now states the cut, and says outright when a
  creature drops nothing for you at all. **Inspect something your level, then something 15 levels under
  you.** ->

- `85i` [] - **DARK DOMINION IS GONE**, on your ruling. Six pieces, a real set bonus, and nothing in the
  game ever produced one. ⚠ The rule this leaves now has **no exception**: gear is LADDER or TRAINING.
  **Walk both shop shelves and open a few chests; nothing should 404.** ->

- `85j` [~] - 🔴 **BOSS EXP: THE PARTY SPLIT IS OUT, THE RESPAWN WAIT IS IN.** Two changes. The efficiency
  goes **1.5 → 2.0**, because the 1.5 was justified *by* the five-way split you struck out (*"the time it
  takes a 1 dd to kill the boss not 5"*) — priced for one DD, the top of your own "x1.2~2" is what is
  left. And a factor the formula never had: **what you spend waiting for it to come back**, measured
  against the world's own 22s trash cadence, so ordinary trash is ×1.00 and levelling does not move.
  A level-90 field boss goes **~6kk → ~24kk**, inside your *"at least 20kk"*; an elite gains ~29%.
  ⚠ **The 0.25 exponent is MY number and the only knob.** 1.0 would pay the wait in full, which assumes
  you stand at the corpse for thirty minutes; 0.25 is the share that lands on the figure you named. Say
  higher or lower and it is one constant. **Kill a field boss and read the number.** ->

- `85k` [] - **THE TARGET FRAME IS SHORTER AND SAYS MORE.** The title bar is the **name** now, the
  duplicate name row is gone (−28px), and the half-clipped type line is full width and reads
  **`Mob: 44, Aggressive, Social (wolf)`** / **`Player: Vagabond`**. ⚠ Social prints nothing while `85d`
  is off — the frame must not advertise a rule the simulation is not running. ⚠ "Vagabond" is not
  waiting on a lookup: there are **no player clans in the game yet**, so everyone genuinely is clanless.
  The mob INFO sheet leads with a Behaviour block (aggressive / social / rank) and has lost a mob's mana
  and its all-zero Utility rows. **Target a mob, a player and an NPC and read all three.** ->

- `85l` [] - **THE CHAT AND COMBAT WINDOWS MOVE, RESIZE AND LOCK** — corner grip, an L/U button in the
  title bar, and position + size + lock state remembered **on the device** (`PlayerPrefs`), your
  *"persistent for the apk not the server"*. 🔑 Device and not server is the right home: where a window
  sits is a property of the screen it is read on, and a layout pushed from the server would fight the one
  your phone just learned. **Move both, resize both, lock one, force-close the app and reopen it.** ->

- `85m` [] - **THE CHAT INPUT CLEARS YOUR CAMERA CUTOUT** — +20px. 🔑 It only reads as mid-screen **while
  typing**: the row lives at the bottom edge and the soft-keyboard lift is what puts it level with a
  landscape punch-hole, so the clearance is added to the LIFT and not to where it rests. Also here:
  ⚠ **the "take a break" banner is at 10 MINUTES** for this pass at your request, tagged in the source to
  go back to 3h. **Type something long and check you can see the first letters.** ->

- `85n` [] - 🔑 **YOUR EIGHT 40+ CSV FILES EXIST, SEEDED.** `melee rogue` · `archer` · `healer` · `buffer`,
  each `40-74` and `76-85`, in `docs/data/classes_skills_csv/`, in the 40+ format (the 20-35 header plus a
  trailing `RACE` column). They hold **exactly what the game already registers above 40** — nothing is
  invented, `BL-02` still stands. Generated once by `tools/SkillCsvSeed`, which **refuses to overwrite**,
  so they are yours the moment you open one. The README beside them maps each file to its disciplines.
  ⚠ **Four of the eight are nearly empty and that is the honest picture** — outside the Warchanter's buff
  ladder there is almost nothing above 40 in this game. ⚠ **`Vanish` shows an SP cost of 1** (the record
  default) precisely so you can see it and price it. **Nothing to test — read them.** ->

---

## 81. THE TWO REFLECTS — never reached in playtest 23

- `81b` [] - **`Deflection` — physical-skill reflect, warrior.** *"default warrior @40 → 0.15 chance ×1
  reflected; @76 → 0.3 chance ×1 reflected."* Your numbers verbatim, and your own pick between the two
  shapes you offered: the fraction stays **×1.0** at both rungs and only the **chance** moves. A landed
  physical skill rolls the victim's chance; on a hit the full damage goes back at the caster, **who can
  die to it**. Kept separate from the armour sets' `MeleeReflect` (5%, basic attacks only) — no blow is
  ever taxed by both, and two Deflection warriors terminate after one bounce. ->

- `81c` [] - **`Backlash` — debuff reflect, tank, 30%.** *"tanks get 30% chance to reflect a debuff →
  u cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff
  paths, because a bounce is not a resist: a tank who throws your stun back was never tested against
  it. The caster gets the effect with no resist roll of their own and no second bounce. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [~] - **Nuker vs champion, unbuffed.** Half of this closed itself in playtest 23: the **mage** can
  now farm solo (`79i`). The **champion** half is untouched — *"they both have hard time to farm without
  buffs"* — and is `BL-72`. Do it in the same sitting as a §85 auto-farm run. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row. ->

- `13a` [] - The **"take a break" banner**. ⚠ **Set to 10 MINUTES for this pass at your request**, and
  tagged in the source to go back to 3h — *"(tag it to return to default 3h after test)"*. See `85m`. ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**

- **3rd/4th class kits** — blocked on your **40+ CSVs** (`BL-02`), still the single biggest unlock.
  🔑 The authoring format is settled in **[docs/design/Disciplines.md](../design/Disciplines.md)**: you
  author **by DISCIPLINE with a trailing RACE column** — **10 CSVs, not 30** — and **six questions in it
  are waiting on you**. ⚠ **§85n seeded the files for you**, holding what already exists above 40, so
  you start by editing rather than by an empty sheet.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you; it wants its own ×100-rate playtest.
- **`BL-73` mob social clans** — off by one switch at your instruction, back on when the world map
  spreads the camps out.
- **`BL-74` the game launcher** — still not treating the app as a game; research owed.
- **`G3` mobs-as-players** — needs the document and the BalanceMatrix tables first (`BL-47`).
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
