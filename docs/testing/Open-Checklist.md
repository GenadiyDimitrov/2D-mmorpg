# OPEN CHECKLIST — five versions in one APK (**0.63.0 → 0.67.0**)

> **Rolling and unversioned.** The client on your phone is **0.62.0**. Everything below has been built
> and committed since, in five releases, and **none of it has ever run on a device** — including the
> playtest-22 fix batch (§79), which was written up for an APK that never went out. This is the
> largest untested surface the project has had; the "where to spend the pass" list below is not
> decoration.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids. Twenty-one of them were built in these
five versions and are therefore *deleted* from it — what is left there is what is still owed.

---

## ⚠ BEFORE YOU START — three things, and two of them will lock you out

🔴 **INSTALL THE NEW APK.** Your phone is on **0.62.0**; this is **0.67.0**. The protocol went
**17 → 20**, so the old client will be refused at login — that refusal reads "client out of date" and
is not a bug.

🔴 **DELETE `game.db`** (and `game.db-shm` / `game.db-wal`) next to `Game.Server.dll` **before the
first boot.** The schema changed twice — 0.63.0 (crafting professions and their exp) and 0.66.0
(`PicksRemaining` on an item instance). `EnsureCreated()` only builds a database that is *absent*; it
never adds a column, so an old file boots and then fails on the first query that needs one.
⚠ **Your characters go with it.** The debug seeds (`admin`/`admin`, the level-90 Warchanter) come back
on their own.

⏸ **Crafting is PARKED, at your instruction** — *"leave the salvage/mats etc craft until I'm able to
test it fully… that's a single playtest only for this."* §84 lists what is in the build so you are not
surprised by a new NPC, but **do not spend this pass on it**.

---

## Where to spend the pass, if you don't do all of it

1. **§80c — invisibility.** The biggest new surface, and the only one that can make another player's
   client wrong: a hidden character is **withheld from the world snapshot**, not merely flagged. Test
   it with two clients.
2. **§80e — threat and taunt.** The model you asked for now has numbers in it. A tank should hold a
   mob, and a buffer should be able to lose one.
3. **§81a — a physical skill can no longer be evaded.** It is a combat rule change that touches every
   melee fight in the game, and Evasion Boost's 25% is now the *only* dodge in it.
4. **§80a / §80b — nine new fields and two new dungeons.** This is where you will actually farm; the
   dungeon level mess had one cause and it is fixed at the root.
5. **§79 — the whole playtest-22 fix batch**, still untested. `79a` (the auto-buff tab was destroying
   its own setting) and `79c` (your block-reduction ruling) are the two that matter.

---

## 0. ANSWERS I OWE YOU — read, don't test

### 🔴 New, and three of these are rulings only you can make

- 🔴 **`BL-22` salvage: the S row cannot be moved by this feature at all.** Your budget was
  *"10~20% decrease in time should be ok"*, and that is exactly what the early rungs got —
  E −3% · D −10% · C −18% · B −0% · **A −0%** · **S 347h → 347h, −0%**. The cause is your own
  mapping, *"rarity for mats rarity"*: salvage can only pay the rarity of gear that **drops**, and
  gear rarity is capped by RANK — a normal mob stops at Epic and an **elite stops at Epic too**, so
  only a boss (0.09 kills/h) ever drops Legendary or Mythic. The A and S recipes bind on **Legendary
  Ingot**, which salvage therefore never produces. Proven, not argued: at a uniform quantity of **20**
  the early rungs collapse to −24/−39/−72% and **A and S still move 0.00%**. Quantity is not the
  binding constraint; the rarity mapping is. Option 1 — *accept it as a mid-game feature* — is what
  shipped, because the other two change things you did not ask to change. **`M13` in BalanceMatrix
  prints all three.**

- 🔴 **`BL-13` the boss curve — the ×100 IS landing, and that is not the problem.** A level-20 field
  boss spawns with exactly 36,000 HP; nothing is eaten. But against your six-minute / 3-DD target a
  single flat multiplier swings **11×** across the game: TTK **80s at 20**, 296s at 40, 684s at 60,
  **888s at 76**. Mob HP grows as `0.8·L²` while a geared party's DPS is nearly flat (448 → 525).
  Two decisions are yours: *should a level-20 field boss really take a level-20 party six minutes*,
  and do the late bosses come **down** to 360s or does the target itself **rise** with level.
  🔵 **The world boss has no rank to live in** — your *"an hour for ~10 parties"* is ~**167×** a field
  boss, which is a new rank with its own drops, phases and lockout, not a bigger number. Not invented.

- 🔴 **`BL-49` — a boss kill now swings 1000× across the levels.** The time-ratio EXP rule is in and
  measured (`exp/sec ×` reads exactly 1.20 elite / 1.50 boss at 20/40/60/76/85, which is the assertion
  that the payout and the time have not come apart). ⚠ But one **level-20** field boss is now **125%
  of a level** solo, while a level-85 one is **0.1%** — the same 150 trash kills either way. That
  spread is the levelling curve, not the boss rule, and it is the half of `BL-49` still open.
  🔑 It also fixed a **silent five-fold underpayment**: the old rule was HP-only and clamped at 20×
  while a boss carries 100× HP, so every field boss in the game paid a fifth of what it owed. Boss EXP
  20× → **150×**, elite 4× → **4.8×**.

- ⚠ **The buff-vs-heal threat ratio is off by ~8×, and the buff is not the wrong half.** You sized it
  against a ~1500-power quick heal at 70; the cleric's heal ladder stops at skill level **4** (learned
  at 35, power **301**), because everything above it is blocked on `BL-02`. So a level-70 group buff
  out-threatens a heal by ~8× today instead of your ~1.3×. **`BL-16`** is the half that has not caught
  up — and it is now load-bearing rather than cosmetic.

- ⚠ **Four numbers in this batch are mine, not yours**, each flagged in the source: the top rung of
  **Madness** (your penalty stride is regular, your gain steps only on even rungs, so the top rung had
  to take a step or differ by the penalty alone); **when a tank gets Backlash** (you gave the 30% and
  no level — it is granted at the 3rd class change, beside Deflection, which you *did* date); the
  Ultimate Scroll of Resurrection's **15,000 Value**; and the three small subclass-swap clauses
  (re-asking reports the time left, a different class mid-count is refused, a death cancels it).
  ⚠ Your own note on Rite of Preservation's 1h/1h is *"(not fixed)"*.

### Still open from last time

- 🔵 **Dark Dominion is the one thing I found and did NOT delete.** Six armour pieces (Plate, Leathers,
  Robe, Helm, Gauntlets, Sabatons) forming a real named SET with a real set bonus — and **nothing
  drops, sells or boxes them**, so the set has never been obtainable by anyone. Deleting a designed
  set is your call, not a cleanup. **Make it obtainable, or say the word and it goes.**

- ✅ ~~**`Robe 611` is `[NOT BUILT]`** — fourth pass running.~~ **BUILT** — see `82c`.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone again on purpose: §79c already moves the block channel, and moving both at once
  would make neither measurable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable*, the vendor refuses it outright ·
  `0`, `-` or omitted → **no opinion, use the catalog's price** (a stored `0` would mean "worth
  nothing", which is a different claim from "I did not say") · any positive number → that exact price,
  overriding the catalog (`k`/`m`/`b` and `1_000_000` both parse). Every argument after the item id
  follows the same rule: `-` is always *no opinion*.

- ✅ ~~`62j`/`74e` the enchant drop cut~~ **CLOSED by your data** (*"to 28 I got 2"*, against 80
  before). ✅ ~~`55f` mages and MP~~ **CLOSED by your own trace** — the ladder is the fix.
  ✅ ~~`77d`~~ — your `[x]` stands: `[-]` never walks a PAID rung down.

---

## 80. `BL-65` / `68` / `69` / `70` / `71` — THE PLAYTEST-22 BUILD BATCH (0.64.0)

- `80a` [] - 🔑 **NINE NEW FIELDS, EIGHTEEN NEW CAMPS — every 16-40 band now exists four times.**
  *"Add several new zones to duplicate the 16-20 … all the Stonewatch zones … to have 4 of each."*
  They go **east** (your *"the bot side fields can be extended … to the right"*, and the only direction
  with room: Brackenford is 14000 south, Frostmere 13000 west, north is the Training Outpost). A 3 × 3
  grid at x ≈ 31000 / 36000 / 41000, each lane keeping the original field's shape — low band nearest
  the city, high band furthest out:

  | | x ≈ 31000 | x ≈ 36000 | x ≈ 41000 |
  |---|---|---|---|
  | y ≈ 6500 | Sunward Moor 16-24 | Highstone Ridge 24-32 | Emberdust Barrens 32-40 |
  | y ≈ 12000 | Thornfen Moor 16-24 | Ravencrag Ridge 24-32 | Palewind Barrens 32-40 |
  | y ≈ 17500 | Mistlow Moor 16-24 | Bleakspur Ridge 24-32 | Cinderflat Barrens 32-40 |

  🔑 **The city was NOT moved.** You offered (*"the whole City can move to the right"*) and it turned
  out not to be needed — the generator places a field by bearing and distance, so more ground is just
  more distance, and not moving it avoids stranding every character saved standing inside it.
  ⚠ **Stonewatch's gatekeeper now lists 12 fields.** That is a long menu on a phone — `BL-41`'s grade
  filter is the same question in another window. **Take a ride to three of the new ones and check the
  mobs are your level and the camps are not on top of each other.** ->

- `80b` [] - 🔴 **THE DUNGEON LEVELS HAD ONE CAUSE, and it was not the sign on the gate.** *"Now a 32
  lvl mobs almost next to a 65 lvl which protect the 44 lvl boss … the mob lvls are all over the
  place."* Those were the literal numbers. One line in `SpawnMobFor`: **a mob with a NATURAL level
  brings its own**, so a spawner's Min/Max was only a label. The crypt's roster was `hollow_one` (58),
  `grave_robber_fighter` (32) and `dread_knight` (65) — three unrelated creatures wearing a "44-48"
  sign. Fixed at the **roster**:

  | Dungeon | Rooms | Boss | Entrance gated to |
  |---|---|---|---|
  | **Hollow Crypt** (same place) | 39-42 | Grave Lich **44** | Greymarsh |
  | **Sunless Warrens** (new) | 58-64 | Dread Knight **65** | Ironreach Keep |
  | **Ashen Sepulchre** (new) | 80-85 | Disciple of the Dawn **90** | Frostmere |

  Both new dungeons are the crypt's outline **translated** (10k and 22k south-west) on purpose — a
  known-good narrow diagonal the wall clamp, the entrance annex and the move order have all been
  measured against. Each entrance is gated to the city whose band contains it, so a level-1 is not
  offered the level-85 vaults. ⚠ The Sepulchre's rooms are **elites at 80-85**, which feeds the
  crafting mat faucet — that is a second high-level elite field. **Walk the crypt and confirm every
  room is 39-42.** ->

- `80c` [] - 🔴 **INVISIBILITY, IN YOUR THREE SEPARATE KINDS** — they share a word and nothing else,
  so they are three pieces of state, not one flag with modes.
  **1 · Hide** (`Vanish`, the Phantom's) is *actually* hidden now. It used to mean "invisible to mob AI
  targeting" and nothing more — every other player still saw and could click you. A hidden character is
  now **withheld from the world snapshot itself**: the client never receives them, so it cannot draw,
  click or list them, and everyone who could see you is told you left the instant you hide.
  **Anything but movement ends it** — a hit, any skill, a potion, damage taken. That is what makes your
  *"any AoE damage also reveals"* true with no special case in any AoE.
  🔑 **The reveal is at EXECUTION, not at the click** — your rule, and the reason a gap-closer works:
  *"i want to click the skill and im not in range … but still invisible once the skill is executed then
  i appear."*
  🔑 **Hidden means hidden from EVERYONE, party and staff included** — your ruling, which overruled the
  narrow version I shipped first. Your answer disposes of the objection: **you cannot die hidden**,
  because taking or dealing damage reveals you before it lands. **You are not removed from the party**
  — the roster still lists you and shows **`Hidden`**; what goes is being renderable, clickable and
  heal-targetable, so party heals/buffs skip you and a ranged ally cast falls through to a self-cast.
  **Staff lose sight, not control**: `/tp`, `/tpme`, `/jail`, `/where` resolve by NAME.
  **The counter is `Signal Flare`** (rogue/bow, 28): non-damaging, reveals every hidden character
  within 300 **and bars them from hiding for 30s** — the second half is what makes it an answer rather
  than an inconvenience.
  **2 · Stealth** hides you from **unaggroed monsters only** — players still see you, anything already
  chasing keeps chasing, and it does **not** break when you act. `Prowl` (rogue toggle at 20, **1
  MP/s**, no cast — *"toggle-on makes the rogues farm in peacefull zones"*) and `Shrouding Hymn` (the
  buffer's party version at 30: 1 min, 30s reuse, **300 MP**, your numbers — the price is the point).
  **3 · `/invis`** (admin) is absolute: nothing in the simulation ends it, and it hides you from other
  staff. Still *hittable* by area damage — `/god` is the separate switch, your own distinction.
  **Test with two clients: hide in front of the other one and confirm you VANISH from his screen, not
  just from the mobs.** ->

- `80d` [] - 🔑 **MOBS HAVE A SOCIAL CIRCLE, and the rogue has a way around it.** Twelve clans (`orc`,
  `mantis`, `redhorn`, `wildhorn`, `radiant`, `drake`, `skeleton`, `dread`, `mirror`, `lizardman`,
  `marauder`, `wolf`). Damage one and every clanmate within **450** joins, seeded with the same threat
  the pull is worth, so the person who started it owns the whole camp. The radius is deliberately
  **wider than a mob's own 400 aggro range** — a camp that answers only as far as it can already see
  you is four independent mobs, not a camp.
  🔑 **The trigger is DAMAGE and nothing else** — your ruling: *"social circle only works if a mob is
  hit, not when taunted/debuffed/aggroed/etc."* Two limits stop a zone-wide riot: a clanmate already
  fighting somebody is left alone, and the mobs that answer a cry do not cry in turn.
  **The rogue gets `Lure`** (20/28/36) — which is what the damage-only rule exists to permit. Power
  **500** (far below Provoke: a lure is how you *start* a fight), and its ladder is pure **reach:
  200 / 400 / 600**, your numbers, so a level-36 rogue out-ranges a mob's aggro and pulls without
  stepping into the camp's notice. **Mob-only, and it refuses a person out loud.**
  **Pull one orc and count how many come. Then lure one out and check the camp stayed home.** ->

- `80e` [] - 🔴 **THE THREAT MODEL HAS NUMBERS NOW.** Most of it already existed — a real per-attacker
  table, damage as aggro 1:1, a working Provoke. What was missing was everything that made it
  *authorable*.
  **Taunt POWER is a number on the skill.** A taunt does two separate things and they are no longer
  the same thing: it puts you on top and **locks** the mob there for 3s, and *then* adds its power as
  the **cushion** deciding whether you still hold it when the lock ends. Because threat is damage, it
  reads literally — a 5,100 taunt means someone must out-damage you by 5,100 to take the mob. The old
  rule was `top × 1.2 + 100`, identical at every level and for every taunt ever written; 20% of the
  top is a rounding error once a DD lands 7-8k a skill, which is the complaint this came from.
  **Provoke is a ladder** on the tank's 20/24/28/32/36 cadence: **1500 · 2000 · 2800 · 3800 · 5100**,
  anchored on your two endpoints (*"1000-2000 at L1"* → *"20-30k"*) — a ×1.36 step that lands inside
  20-30k at skill level 10, which belongs to the 40+ kits.
  **A healer is no longer invisible to every mob in the game**: `power / castSec × 10 × peopleHealed`,
  given to every engaged mob fighting somebody the cast helped — so a heal in another zone costs
  nothing. Computed from the **authored** power and cast time, never from the HP that landed.
  **A buff is `LEARNED-level × 20 × peopleAffected`** — 🔑 the level it is **learned** at, not the
  caster's: *"if I learn a buff at 50 and another at 70 the 50 one should have less aggro value."*
  `HolyForce` is learned at 70 → 1400 a head → **12,600** across a full party of 9 (a full party is 9,
  not the 7 in your example). ⚠ **A buff cast before the pull is worth ZERO** — support threat only
  reaches mobs already fighting somebody the cast helped. A buffer draws aggro for re-buffing
  **mid-fight**, which is when he should.
  **Threat decays 1%/s** on an engaged mob — proportional, so it can never re-order the table on the
  tick it runs; what it shrinks is the *gaps*, which is what makes a taunt something you renew.
  **And a real defect found while answering: a proximity pull added NO threat at all** — a mob that
  walked to you arrived with an empty table, so the first point of damage from anyone owned the kill.
  A pull is now seeded at **5% of the mob's own max HP**.
  **Take a tank and a DD to one mob: taunt, let the DD burn, and see how long you hold it.** ->

---

## 81. `BL-06` / `07` / `08` / `11` / `14` — THE COMBAT-CHANNEL BATCH (0.65.0)

- `81a` [] - 🔴 **A PHYSICAL SKILL IS NO LONGER EVADED AT ALL.** *"normaly no1 can evade a physical
  skill … now on then i miss a skill which is anoying — stab fails … then stab should land but misses
  … no1 evades only rogues gets a floor while in an ultimate 25%."* The accuracy-vs-evasion roll is
  **gone from the physical-skill branch entirely** — and with it the caster's accuracy, the warrior's
  `Precision` hit floor and the rogue's `EvadeFloor`, none of which have any say over a skill now. All
  three still govern **basic attacks**, untouched.
  What replaces it is one defender-side grant, and **the rogue's Evasion Boost is the only thing in
  the game that sets it: 25%, for its 30s.** That also resolves the CSV's long-unbuilt *"skill evasion
  x1.25"* — it was never a multiplier, it was the 25%.
  🔵 **The 40% rung is deliberately NOT built**: `rogue 20-35.csv` authors Evasion Boost as a single
  level and adding a rung would re-spec your data. Same for *"76lvl the physical phantom gets 90% for
  15s"* — a 4th-class skill. Both are `BL-02`.
  **Take the melee out and confirm a skill never says "miss" again.** ->

- `81b` [] - **`Deflection` — physical-skill reflect, warrior.** *"default warrior @40 → 0.15 chance ×1
  reflected; @76 → 0.3 chance ×1 reflected."* Your numbers verbatim, and your own pick between the two
  shapes you offered: the fraction stays **×1.0** at both rungs and only the **chance** moves. A landed
  physical skill rolls the victim's chance; on a hit the full damage goes back at the caster, **who can
  die to it**. Kept separate from the armour sets' `MeleeReflect` (5%, basic attacks only) — no blow is
  ever taxed by both, and two Deflection warriors terminate after one bounce. ->

- `81c` [] - **`Backlash` — debuff reflect, tank, 30%.** *"tanks get 30% chance to reflect a debuff →
  u cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff
  paths, because a bounce is not a resist: a tank who throws your stun back was never tested against
  it. The caster gets the effect with no resist roll of their own and no second bounce.
  ⚠ **The level is mine** — you gave the 30% and no level, so it lands at the **3rd class change (40)**
  beside Deflection. One line to move it to the 2nd. ->

- `81d` [] - 🔑 **ANTI-MAGIC AND ANTI-PHYSICAL MOBS — the pair was a COMMENT and one mob.** *"We had a
  anti magic mobs (lower pdef more mdef) and anty physical (less m def more pdef) — this should feed
  your mres passive."* Two things were missing, not one. **The channel**: a mob could only raise
  M.Def — a flat divisor a levelling mage out-scales — while `mRes`, the *percentage* channel every
  player anti-magic passive already reads, had **no mob-side route at all**. It has one now, plus a
  Magic Resistance track in the mastery layer (the same twelve rungs as the three weapon resists — it
  is literally the CSV's *"???? Resistance"* row, filled in). A **negative** value is a magic
  WEAKNESS, which is what makes the anti-physical half mean something.

  | | P.Def | M.Def | mRes | who |
  |---|---|---|---|---|
  | **Warded** (anti-magic) | ×0.8 | ×1.5 | +20% | Grave Lich 44 · Aether Wisp 58 · Spiteful Ghost 66 |
  | **Ironhide** (anti-physical) | ×1.5 | ×0.8 | **−20%** | Shield Skeleton 20 · Fomor Brute 45 · Dread Knight 65 |

  Watcher Eye (26) keeps its own steeper 2.0/0.5 and gains the mRes half; Obsidian Knight (63) takes
  Magic Resistance **L5**, so the golem that already resists arrows and blades is the one a mage
  answers. Spread 20 → 66 on purpose: Shield Skeleton is early, where "bring the mage" is teachable.
  **Hit a Shield Skeleton with a mage and with a fighter and confirm they read differently.** ->

- `81e` [] - **A MOB'S WEAPON DECIDES ITS HIT SIZE NOW, not just its speed.** *"Archer is slower but
  does more dmg, the fast attacking have more crit rate and more atck speed but less dmg."* Two of your
  three clauses were already true (speed and crit rate have come off the mob's weapon since 2026-08-10).
  The third was not, and its absence was a real defect: a **player** gets the trade free from the
  weapon ITEM (a 2H sword carries more P.Atk than duals), but a mob has no item — its P.Atk is one
  level curve — so handing out weapons changed only the RATE. **A club mob was 12% worse than a claw
  mob at nothing.** The missing half is `433 / weaponBaseSpeed`, referenced on the DUAL's 433 because
  that is the speed every mob was pinned to *before* the weapon change — so it is **DPS-neutral against
  the pin**: nothing is nerfed, the slowed mobs get their lost damage back as hit size.
  Measured at 40 vs a same-level champion: dps **16.5 / 16.4 / 15.8 / 16.0 / 16.3** across dual, sword,
  club, 2H and unarmed — flat, which is what makes it a trade — while P.Atk runs 171 → **227** and crit
  runs **13.2%** → 4.4%. ⚠ **BOW is ×1.00 on purpose**: an archer mob already pays that trade in its
  ROLE (P.Atk ×2, 450 range, −15% P.Def), and charging it twice would be ~3× per arrow. ->

---

## 82. YOUR EIGHT RULINGS, BUILT (0.66.0) — 🔴 this is the DB reset

- `82a` [] - **A partial Blessing Box pick keeps the box for the rest.** *"I'll want to be able to pick
  5 and I get my 5 scrolls + the box for the other 5"* — **"is OK"**. Taking fewer than ten used to be
  refused outright, and that refusal was itself a fix (playtest-19 `48g`: 7 of 10 from a 250k box, the
  rest silently forfeited). The picks now live on the **item instance**, so the box just stays in the
  bag with a smaller number on it and is consumed when its last pick is spent — no `box_scrolls_5`
  family, no second item handed back, no free slot needed at the moment of the split, and the
  InstanceId never changes. The counter reads `0 / 5`.
  🔑 The remainder is decremented by what was actually **granted**, not by what was asked for, so picks
  lost to a full inventory stay in the box. **Open a box, take 5, close it, re-open it.** ->

- `82b` [] - ⏸ **`BL-22` Break down — IN THE BUILD, BUT PARKED WITH THE REST OF CRAFTING.** Any unworn
  piece of tiered gear has a **Break down** button: the item's **rarity** is the material's rarity, its
  **grade** decides the amount, and its own maker's material decides the type (a blade → Ingots, plate
  → Leather, a robe → Thread, a ring → Gems). It is an **alternative to selling, never a bonus on top**
  — nothing here pays gold, and the gates are deliberately the *selling* gates, so a bound item cannot
  be laundered into tradable materials. 🔑 **Bin ≠ Break down, and always were**: the bin restores,
  salvage never does. **The finding it produced needs your ruling — see §0.** ->

- `82c` [] - **`Robe 611` finally has an item** — fourth pass, now built. *"build as u wish."* Taken
  literally off the CSV: WIT +2, INT −2, SPT +2, Speed +7, as **"Bloodsteel Raiment"**. ⚠ The one
  clause needing a reading, *"Stun/Fear Resist x1.7"*, is **not** a guess — it is the same fold you
  already accepted on the other two `611` rows (heavy and light), both of which ship as `CcResist 0.4`.
  Identity: the base 61 robe is the caster line (Cast ×1.15, SPT −1), and INT −2 with SPT +2 inverts
  that trade — so this is the tier's **support** robe, the 40 Warden / 52 Sage line continued at B. ->

- `82d` [] - **"Madness", the party Frenzy, at 76 on the buffer.** 🔑 *"put it at 76 on the buffer"*,
  explicitly so an admin can party-buff with it now — *"and when the kits land we will move it."* A
  deliberate temporary home at the top of the Warchanter's 40-74 ladder, which is the only 76 slot the
  game has; the debug admin is a level-90 Warchanter, so it is castable the moment the server boots.
  A thin party wrapper handing out a **new rung 7** of the Frenzy family, so it outranks and evicts any
  weaker Frenzy the party is wearing. ⚠ One number is invented and flagged in the source (see §0):
  **−6% Max HP/MP, +9% offence and speed, +9 move, −8 evasion.** ->

- `82e` [] - **The two level-83 preservation skills.** Both carry Angel's whole effect (buffs survive
  death) **plus the auto-resurrect that nothing in the game used until now**: **Rite of Preservation**
  (Lightbringer, cast on an ally — *they* rise where they fell, 100% exp returned, 1h/1h) and **Undying
  Will** (Bulwark, the self version).
  🔑 The RANKS are not invented — the Angel's Protection comment has said since 2026-07-17 that the
  healer's target auto-res is **Rank 2** and the tank's self auto-res **Rank 3**, both above Angel's
  Rank 1 on the shared key. The exp return rides on the **buff**, because by the time you die the
  caster may be across the map; the death penalty still applies first, so a 100% skill nets to zero —
  your rule is *"you die, you have the penalty."*
  ⚠ **An explicit, named exception to `BL-02`**: you authorised these two skills and only these two.
  ⚠ Your own note on the 1h/1h is *"(not fixed)"*. ->

- `82f` [] - **The subclass swap rules.** Out of a town: a **5-minute wait**. In a town or peace zone:
  **instant, no cd**. Both require being out of combat. The machinery always swapped fine — the gate is
  what was missing. 🔑 The clause that shapes the whole method: *"When changed out if town and 5min
  start to count and enter in town the countdown stays … w8 the 5mins then change (city don't trigger
  the cd) both waits it."* So the pending-swap check sits **above** the safe-zone fast path — reversed,
  walking into the nearest town would skip the wait. **The town rule decides whether a timer starts,
  never whether one finishes.** ⚠ Three small clauses are mine (§0). **Start a swap in a field, walk
  into town, and confirm you still wait.** ->

- `82g` [] - 🔑 **SKILLS AND PASSIVES DESCRIBE THEMSELVES WITH REAL NUMBERS.** *"all skills and passive
  should show the desctiption with numbers."* The gap was structural, not cosmetic: the `SkillEffect`
  flag enum has been **full** for years (`1L << 62` was the last bit), so every mechanic since has been
  added as a plain **field** — Resurrect, KeepsBuffsOnDeath, Lifesteal, GrantsHide, PlacesTrap, Rewards,
  TauntPower, BlockAccuracy. The card reads flags and magnitudes, so **none of it ever appeared**.
  Angel's Protection is the clearest case: its entire payload is one bool, so its card could say
  nothing at all about what it did.
  It is wired into the detail card **and** the Learn preview, where several of them are exactly what an
  upgrade buys (Resurrection walks 25 → 50 → 75 → 100%, a taunt 1500 → 5100). The **conditional** lines
  you asked about carry their condition: "Block chance (with a shield)", "Bow range (with a bow)".
  **Open five skills you know and check the card now says what they do.** ->

- `82h` [] - 🔴 **RESURRECT / PARTY / PvP-FLAG, RE-SPECCED — you replaced the old rule entirely.** It
  was self-based (*"you cannot res a party member while YOU are flagged"*); it is now **target-based**:

  | situation | rule |
  |---|---|
  | single-target support of a **non-party** player | allowed **if they are not pvp/pk** |
  | target **is** pvp/pk | allowed **only** from inside their party |
  | supporting a still-flagged player | 🔑 **flags you** |
  | party invite to a pvp/pk player | allowed |
  | trade | allowed with **pvp**, **never** with pk |
  | res in the same party | allowed for **both** |

  ⚠ **This OPENS something that used to be shut**: support was party-only and anything else fell
  through to a self-cast. Helping a passing stranger is legal now, and the flag is what prices it —
  which is the whole point of moving the test from the caster to the target. Trade used to refuse
  **both** purple and red, which made a 60-second flag a trading ban you earned by defending yourself;
  **karma is the sentence, so karma is what blocks a trade.** The **Ultimate Scroll of Resurrection is
  tradable** now (*"atleast the one that drop and from the admin menu"*) — the tutorial is unaffected
  because its completion kit hands out the separate `_bound` clone. ->

---

## 83. THE POLISH PASS (0.67.0)

- `83a` [] - **`NextTarget`: retaliation outranks distance.** *"Need NextTarget (targeting
  closest/retaliate 5 and cycling through them)."* Half of it was there — the nearest living mob within
  2500, stepping outward on each press. What was missing is the half your note leads with: **anything
  that has hit you in the last 10s now sorts ahead of everything that has not**, and only then does
  distance decide. The ring is capped at **5**, your number.
  🔑 This is the *manual* twin of the autopilot fix from the same playtest (*"a mob hitting you is
  higher priority than nearest … I'm getting ganked by orc archers and still kill the nearest"*) — the
  tap-to-cycle selector had the identical hole. **Client-only, no protocol change**: the combat feed
  already carries every blow with its attacker's id, so the client keeps its own short retaliation
  memory. Deliberately NOT shared with the server's `RetaliationTarget` — that one picks what the
  autopilot will *fight*, this one picks what you are *looking at*. ->

- `83b` [] - **The second launcher icon is gone, and the app declares itself a game.** *"Since my phone
  updated it didn't appear. Now I'm using Secure Folder so I can have 2 clients side by side. Need only
  to be able to make it as a game … to enter the game launcher on its own and to be able to use the
  game boost features."* The dead `UnityPlayerActivity` block is **deleted** — it was kept as the
  duo-testing rig and that reading was half wrong: our entry point is GameActivity, so that activity
  merged in **disabled** while keeping its LAUNCHER filter, drawing a second icon and nothing else. The
  two independent clients were always **Secure Folder**, so deleting it costs no test capability.
  🔑 **That deletion is what makes the game-mode hint work** — a game launcher classifies an *app*, not
  an icon, and a package declaring two LAUNCHER entries is ambiguous to it. `appCategory="game"` was
  already present and inert; it is now joined by the older `isGame="true"` for the One UI builds that
  still read it, with exactly one launcher activity behind them. **After installing: one icon, and
  check whether Game Booster now sees it.** ->

- `83c` [] - **A boss pays for the time it costs.** *"Bosses should give exp based on how long it takes
  to kill a normal mob vs boss (x1.2~2) … not a real formula, just a curve to have."* EXP and SP are
  now `base × killTimeRatio × rankEfficiency`, at **1.2 elite / 1.5 boss** — your range, and the one
  number the design reads off: *an hour spent on bosses is worth 1.5 hours spent on trash.*
  🔑 **A time RATIO needs no simulation and no per-boss authoring**: time-to-kill is `EHP / yourDPS`
  compared between two mobs at the same level against the same player, so **your DPS cancels
  completely** and nothing about the killer enters the number. It reads `HP × P.Def` off the spawned
  entity, so rank multipliers, MobMod HP passives and buffs a mob is standing in are all already
  counted.
  🔑 **1.5 and not 2.0 for a boss, because a boss is fought by a PARTY** — five people kill it five
  times faster and split five ways, so the efficiency each sees is exactly this constant; 2.0 would
  make boss-camping strictly dominant. **See §0 for the two things this raises that need you.** ->

---

## 84. ⏸ CRAFTING (0.63.0) — IN THE BUILD, PARKED ON YOUR INSTRUCTION

Listed so nothing surprises you, **not** to be tested this pass — *"that's a single playtest only for
this."* Crafting is a **profession** with six levels and five masters, each with a joining quest; the
gear ladder is **GRADE**-based (F is uncraftable, a craft can FAIL); an **elite mat faucet** was added
because Legendary/Mythic materials previously dropped from nothing at all. ⚠ `BL-05`, `BL-22` and
`BL-50` are all parked together — the mat economy needs a **×100-rate** run that only you can do.

---

## 79. THE PLAYTEST-22 FIX BATCH — ⚠ STILL UNTESTED, this is its first APK

- `79a` [] - 🔴 **THE AUTO-BUFF TAB: the root cause was DESTRUCTION, not a redraw.** *"it doesn't
  survive a relog ... it says that it's saved but relog says otherwise."* `SendAutoHuntConfig` — the
  echo the server sends the client at login — **never included the `Buffs` array**. The client keeps
  that echo as its entire idea of the config and sends it back with only the bit you edited changed,
  so the tab came back `null` and the server's handler cleared it. So the first press of the **Auto**
  button after a login wiped the tab on the server for real, and the empty rows you saw were honest.
  Fixed at both ends: the echo carries the tab, and a `null` array is now read as *"no opinion"*
  rather than *"clear it"*. 🔑 *This DTO has four sites and only the echo fails silently — the same
  shape as `67i`/`74b`.* **Set the tab, relog, reopen it; then set it, press Auto, relog, and reopen
  it — that second one is the case that was actually broken.** ->

- `79b` [] - **`78g`'s other half.** Reset always worked because Reset is client-side only; Save was
  the broken half. With `79a` fixed, *"one window, one Save"* is testable for the first time: set up
  **both** tabs, Save once, relog, and check neither half was dropped. ->

- `79c` [] - 🔴 **YOUR RULING ON BLOCK REDUCTION, and it moves a combat number.** *"what increase the
  reduction? The shield says 10 but I see 18% ..the shield says 20 I see 28% ... Shields dmg reduction
  is never increased by any means ...only chance."* You were reading the Shield Mastery **passive**:
  it added `ShieldDefPct × 0.04` to the block, which at a maxed 2.00 is exactly the **+8 points** you
  saw. There was a second, smaller one on the Mastery **buff** (+10 points more, when it is up).
  **Both are gone.** BlockReduction is now the shield's own printed number and nothing may raise it;
  the ladder scales block CHANCE and shield DEFENCE instead.
  ⚠ **This is a nerf on top of `70a`, which you passed while it was still +18%.** Measured: a maxed
  tank at ~33% block chance loses about **2.6 percentage points of total mitigation** — small, but it
  lands on the class you just signed off. **Play the tank again and say whether it still reads as a
  tank.** The card and the stat sheet must now show the same number. ->

- `79d` [] - 🔑 **THE ITEM-ID LIST, and why this one unblocks your own pass.** *"Need a grouped list
  (in a file - like the commands one) with each equip/item ID ... So I can use the `/give` command and
  I can test better 75 and 76."* → **[docs/guides/ItemIds.md](../guides/ItemIds.md)**: all **1,078**
  ids, grouped weapons / shields / armor / jewels / runes / potions / scrolls / boxes / materials /
  quest items, and the gear sorted by tier so "a level 40 heavy body" is one glance. **It is
  GENERATED** (`tools/ItemIds`), never hand-written — a hand-kept id list is wrong the first time
  anyone adds an item, and a wrong id sends you hunting for a typo in the command.
  Plus your second half: **an `id <defId>` row on the item card, staff only**, under the enchant line
  exactly where you put it. **Read an id off something in your bag and `/give` yourself another.** ->

- `79j` [] - **`/give` ends in `[amount]` now**, default 1, capped at 10,000 — *"if i want to get 1000
  mats not to have to write command 1000 times."* Because it is the last positional, everything before
  it is `-`:
  ```
  /give Gena mat_iron - - - - - - - 1000
  ```
  🔑 **It splits the way the BAG does, and that is the part to check.** A **stackable** (materials,
  potions, scrolls, quest items) is **one row carrying the quantity** — 1000 mats cost one slot, which
  is the case you asked for. **Gear cannot stack**, so an amount there is that many separate rows, it
  stops when the bag is full, and it tells you how many fit. Either way it is **one inventory push**,
  not one per unit — that was `66n`'s stall and it is not coming back. **Try 1000 of a material and
  then 5 of a sword.** ->

- `79e` [] - 🔴 **THE OFF-LADDER GEAR IS GONE — and it was 64 items, not one.** *"Brass amulet also
  need to be gone. Look for other items that are not from the grade items or training ... treasure
  chest just gave me masterwork iron sword."* What that chest handed you was `sword_e_rare`, one of a
  **sixty-item legacy grid** — 4 weapon types + 3 armour weights + 3 accessory slots, each × F/E ×
  Common/Uncommon/Rare — generated by a block that **predates the grade ladder by a whole generation**
  and was never re-cut with it. 🔑 *They survived four gear passes because exactly ONE line referenced
  them: that treasure chest.* Deleted: the 60, plus **Brass Amulet** (still on the Outfitter's shelf),
  **Silver Talisman**, **Iron Mace** and **Ash Wand**. The chest's 1% slot now rolls the real ladder's
  `sword1h_t20_rare`.
  ⚠ **THE RULE THIS LEAVES:** gear is **LADDER** (`ItemLevel > 0`, generated from `gear_sets.csv`) or
  **TRAINING** (its own CSV block, `72b`). There is no third category.
  **Open a few treasure chests, walk both shop shelves, and check nothing 404s.** ->

- `79f` [] - **The quest-token chat flood is gone.** *"Remove the `SYSTEM: Stonewatch Contract: Bear
  Pelt 93` .... its a drop item .. u can say in combat `You looted: Bear Pelt [Q]`."* Built as you
  wrote it, in the combat log beside every other drop. The running count went with it — the quest
  window is refreshed on the same kill and is where a contract's progress belongs. ->

- `79g` [] - **The gatekeeper closes behind you.** *"After the teleport from the GK the window of the
  old gk need to close automatically."* Taking a ride now shuts the dialog: you are not standing in
  front of him any more, and the rows left on screen are an NPC in another city. ->

- `79i` [] - 🔑 **MP regen on walk: you were RIGHT, and right about the reason too.** *"MP regen is
  unchanged when walking/runnin - or atleast vissually - it seems like its only visually."* The server
  has been paying the bonus the whole time (walking ×1.2, sitting ×1.8, safe zone ×5) — but
  `StandingRegen`, the function that fills the **stats window**, deliberately reported the RUNNING
  baseline and ignored the stance. So the one place you could check the bonus was the one place saying
  it did not exist. The sheet now shows what you are actually paid, and it is pushed the instant the
  stance changes. **Open Stats, toggle walk, and watch both regen numbers move by 20%.** ⚠ It also
  picks up the safe-zone ×5 now, so the number in town is genuinely five times the field one. ->

- `79h` [] - ⚠ **`75c` — `/give <player>` alone: NOT a regression, it was never built on this client.**
  The server has always sent an `AdminGivePicker` message for it; **the Unity client has no handler for
  that message, and none for `/bag` either** — both are WPF-harness survivors that the checklist kept
  claiming worked. I did not build the window: `79d` gives you the ids, which is the route you actually
  asked for, and a picker is a real piece of UI rather than a fix. **Queued as `BL-56`.** ->

---

## 75. ITEM TAGS + `/give` — still untested, unblocked by §79d

- `75a` [] - **An instance carries five properties of its own**, each `null` meaning *no opinion, use
  the catalog*: **sell price** (−1 = unsellable) · **tradable** · **custom name** (20 chars) · **can
  store private** · **can store account**. ->

- `75b` [] - 🔑 **The tag on the card is DERIVED, never stored.** Sellable + tradable shows nothing;
  neither shows **"bound"**; sellable but untradable shows **"private"**; and a timer composes on top →
  **"(temporary, bound)"**. The three predicates live in `Game.Shared` and are called by the server
  *and* the item card. ->

- `75d` [] - **A tagged instance is ALWAYS a fresh bag row**, never merged into a stack — the tag
  belongs to that copy. Give yourself two differently-tagged copies of one item and confirm they stay
  apart. ->

- `75e` [] - **Enforcement reads the INSTANCE, not the def**, at the vendor, the trade offer, the
  private keeper and the account keeper. Try to sell, trade and store a bound one. ->

- `75f` [] - 🔴 **THE ROW THAT MATTERS: the tags survive a relog.** They are five real columns. *A
  bound item that comes back ordinary looks perfectly right until the moment it can be sold.* Tag
  something, log out, log in, check the card. ->

- `75g` [] - 🔑 **The two storage flags are deliberately separate, not one "storable".** An ordinary
  bound item is barred only from the **account** keeper; Sinners has to be barred from **both**. ->

---

## 76. PREMIUM REWARD RUNES — half tested

Passed: `76a` `76d` `76g` `76h` `76i` `[x]` — the 55 generated items, rungs never stacking, a rung's
popup reading its own level, the measured effect, and the startup refusal. Left:

- `76b` [] - **Rune of Sinister** — no exp, no SP; **gold and drops untouched**. *"So a grinder can
  grind and no lvl up."* ->

- `76c` [] - 🔴 **Rune of Sinners** — all four channels zeroed, *"bound to your soul for the time it
  has left."* New def-level **`SoulBound`**: refused by **both** keepers regardless of instance tags.
  Try to store one, trade one and delete one. ->

- `76e` [] - **The drop bonus is a PARAMETER of the one rate function**, not arithmetic at a call site.
  🔑 **A player wearing a Drop rune is *shown* the chance they actually roll.** Open a mob's drop tab
  with and without one. ->

- `76f` [] - 🔴 **A stop is a hard override applied AFTER the max**, so no pile of bonus runes can
  dilute a punishment. **Kill something wearing Sinners and confirm all four are zero.** ->

---

## ✅ PASSED IN PLAYTEST 22 — collapsed, nothing owed

**§70 the shield cut** — `70a` `70c`-`70h` `[x]`; −19% P.Def still reads as a tank and a shielded mage
is mortal again. `70b` was a **ruling**, not a pass, and is built in §79c.
**§71 the start quest** — all eight rows `[x]`; both dead-ends gone, the boxes arrive from the quest.
**§72 the training tier** — `72a`-`72c` `[x]`. ⚠ §79e is the same class of bug one layer out.
**§73 the training dummies** — `73a`-`73e` `[x]`; they work for the first time (the reach was 50
against a stop-distance of 80).
**§74 the five small ones** — `74a`-`74f` `[x]`, including the enchant-drop verdict.
**§77 the stat-swap tab** — all nine `[x]`; the ladder charges 35,000,000 however you spread it.
**§78 the auto-buff tab** — `78a`-`78e` `[x]`; `78f`/`78g` are fixed in §79a/§79b.

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [~] - **Nuker vs champion.** Your playtest-22 note: *"they both have hard time to farm without
  buffs .. when i login in 1-2h after the npcs buffs are gone both are dead and with potion buffs."*
  🔑 **That is a finding, not the measurement** — the run cannot be measured until something keeps them
  alive, which is what §78 is for. Both halves are written up as `BL-72` beside `BL-18`. Do `32z` in
  the same sitting. ->

- `32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
  ranks, assist-leader — and all of it **survives a relog**. ⚠ `74f` changed what the chain does when a
  skill cannot fire (it skips instead of stalling), and `79a` changed what survives a relog. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row. ->

- `36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
  top. ->

- `25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. ->

- `13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**
The eight from playtest 22 are all **built and in this APK**: `BL-65` (§80b) · `BL-67` (MpHeal, the
hardcoded 60) · `BL-68` (§80a) · `BL-69` (§80c) · `BL-70` (§80d) · `BL-71` (§80e) · the item-id file
(§79d). Only **`BL-72`** — unbuffed farm is not survivable for either kit — is still a question, and it
is a question for you, not a build.

Still true and unchanged:

- **3rd/4th class kits** — blocked on your **40+ CSVs** (`BL-02`), still the single biggest unlock.
  🔑 The authoring format is settled and written up in **[docs/design/Disciplines.md](../design/Disciplines.md)**:
  you author **by DISCIPLINE with a trailing RACE column** — **10 CSVs, not 30** — and there are **six
  questions in it waiting on you**. Nothing 40+ is invented in the meantime; `BL-35` (§82e) is your own
  named exception.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you, see §84.
- **`G3` mobs-as-players** — needs the document and the BalanceMatrix tables first (`BL-47`).
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built (§80b).
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
