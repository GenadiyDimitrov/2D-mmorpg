# OPEN CHECKLIST — everything untested as of **0.60.1** (2026-08-11)

> **Rolling and unversioned.** Playtest-20 (your 2026-08-10 pass over ten builds) is closed and
> transcribed verbatim into [Playtest-Archive.md](Playtest-Archive.md#playtest-20) — this file has been
> rewritten against the six builds that came out of it (§62-§67). Every answer you gave is preserved
> there; everything you left open is carried forward below.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

---

## ⚠ BEFORE YOU START

**Install `L2Clone-0.60.1.apk` and unzip `Game.Server-0.60.1.zip`.** Protocol is **16** — it moved at
0.59.0 for the crafting push, and at 0.58.1 before that; **nothing since has moved it** (no DTO
changed). Both sides together anyway: the catalogs, skill tables and world outlines are compiled into
each, so a mismatched pair disagrees quietly instead of refusing — 0.59.1 is *entirely* a catalog
change, so an older client would show you the old S numbers and no set bonus while looking healthy.

🔴 **Move `Game.Server/game.db` out (+ `-shm` + `-wal`).** 0.58.1 renamed the DEX column to AGI and
`EnsureCreated` cannot migrate a rename — an old character would load with a missing stat. Everything
older still applies too (0.52.0 columns, the 0.53.0 God removal, the mastery clamp, 0.55.0's three
title columns).

🔴 **EIGHT BUILDS ARE UNPLAYED — 0.58.0 … 0.60.1.** The four 0.58.x builds came out of your own
playtest-20 finds, so much of this pass is *"did he fix what I reported"*. Two of them changed combat
maths that everything else sits on: **the weapon speed table** (§62) and **how magic lands** (§64).
**0.59.0 is CRAFTING** (§66) — new ground, the only feature rather than a fix. **0.59.1 is your own
S-grade CSV** (§67). **0.60.0 is the enchant rework** (§68) — every enchanted number in the game moved.
**0.60.1 (§69) is the tutorial dead-end you hit today**, plus the magic evasion you ruled on.

✅ **Pre-flight is clear.** `tools/SmokeTest` was re-run against the **0.60.1** server — **ALL CHECKS
PASSED**, including its blueprint-craft assertions. The server was boot-checked too (it starts, every
world validator runs, log clean). 0.59.0 touches the login sequence, which is exactly what the smoke
test exists to guard.

🟢 **The weapon CSV landed — the ⏸ CSV rows are LIVE again.** You said you were re-authoring it, and you
did, twice on 2026-08-11: the weapon pass (shipped in 0.58.3) and the S-grade pass (0.59.1). Rows still
marked **⏸ CSV** below were written before that; they are now testable, and §67 says what each of them
became.

## Where to spend the pass, if you don't do all of it

In order of how expensive a defect would be to find later:

1. **§68 the enchant rework** — every enchanted item in the game changed at once, and it is the newest
   thing here. `68a` (what a +16 weapon is now worth) and `68d` (a +16 armour set) are the two numbers
   the whole build stands on.
2. **§62 the weapon speed table** — every DPS number in the game moved. A 2H now swings slower than a
   1H, which is correct but makes the champion the worst melee until your CSV raises 2H P.Atk.
3. **§64 magic landing** — a brand-new formula replaced a stat that did nothing. If the parity number
   is not ~1% fail, everything above it is wrong.
4. **§66 crafting** — the only new feature here, and the one thing with no prior play at all.
5. **§67 the S grade** — your own numbers, but 21 pieces that completed no set now complete three, and
   **`67f` the light-S +200 flat crit damage** is the single biggest number you have ever authored in
   that channel. If one thing in §67 is going to be wrong, it is that.
6. **§63 the four bug fixes** — the dungeon wall and the jail were rebuilt, not patched.
7. **§62 evasion** — your biggest find. Confirm the +32 is gone at the exact character you measured.
8. Everything else.

---

## 0. ANSWERS I OWE YOU — read, don't test

- `56e` - **"What is this Resonance?"** It is the **human Warchanter racial passive** (`wc_human_pass`):
  +10% max MP, extra MP regen, and ×1.2 magic crit. You met it on the admin level-90 Warchanter seed —
  it is not a general mechanic, it is that one class/race combination's passive.

- `61b` - **"Which vendor?"** You are right: **no vendor sells above D grade**, so the "Mythic S-grade"
  string in a vendor list is unreachable today. Not a defect, nothing to test.

- ✅ ~~**Piercing Stab's level-32 MP cost is 58.**~~ **CLOSED — you ruled it a typo** (*"should be 28..
  a typeo"*) and fixed the CSV yourself. **Built in 0.60.1**: level 4 costs **28**, so the line now runs
  18 / 21 / 24 / 28 / 30 with no spike. Nothing to test beyond glancing at the skill card.

- ✅ ~~**Your 2H S-row disagrees with the 1H's.**~~ **CLOSED by your own 0.59.1 CSV.** You authored the
  whole level-80 column, and every fighter weapon now shares **S M.Atk 192** — the 1H's derived 211 is
  gone with the derivation. The two lines agree again; nothing owed.

- 🔴 **The champion test is still owed by you.** Reverting the 2H P.Atk raise puts champion/nuker back
  at **0.84×** — the nuker is ~19% ahead. `0a` below is that measurement. ⚠ 0.59.1 did **not** settle
  this: at S the 2H kept 532 while the 1H was cut to 437, but that is the *top* rung only — the 20-36
  band `62h` complains about is untouched.

- 🔑 **"i write percents as x1.23" — noted, and applied.** Your `xN.NN` is how you write **a percent**,
  not necessarily a multiply, so "Mele Vamp x1.02" was built as **2% flat vamp**, per your
  *"my mistake that vamp is additive not multiplicative"*. I will read every future `xN.NN` against
  what the stat can actually do rather than multiplying blindly.

- **`Robe 611` is still `[NOT BUILT]`.** It is the only authored body in `gear_sets.csv` with no item
  behind it (`WIT +2; INT −2; SPT +2`), and you edited its row again on 2026-08-11 without asking for
  the item — so I left it alone a second time. Say the word if you want it real.

---

## My Finds

- [~] - Broken jewels (they are like "train" gear) now have more than uncommon F gear 
   - Broken - (Neck-15,Ear-11,Ring-7)
   - F common/uncommon/rare - (Neck-11/13/17,Ear-7/8/11,Ring-5/6/8)
  Broken should become -> Neck - 9, Ear - 5, Ring - 3

- [~] - I have updated a commented section in the gear csv where the shields defences are .. 
  - To much dmg reduction on top of the additional pdef when sucsessifull blocked 
  - Mage should not be immortal even with a shield it helps a bit but not 47% dmg reduction with 33% chance thats average 15% permanent reduction
  > - BlockChance   .10 .15 .15 .20 .20 .25 .25   (F 20 40 52 61 76 S)
  > - BlockReduce   .10 .10 .15 .15 .20 .20 .25
  > - CritDefense   .03 .05 .05 .07 .07 .10 .10
  > - EvasionPen      3   5   5   7   7  10  10
  > That way the average dmg red from 15% -> to 6%

- [!] - in auto farm the char uses stab and strike with blunt or knives ..while in manual use it's declined because of required weapon
  
- [~] - I feel that we need to spread out the drops and each next grade to drop rearer that the last ...
  - Now my bag is not overflowing with stuff which is good the feel that "I farmed there for hours and still no sword" is a bit annoying. The drop rates now for equip is okish now but the every mob drops everitying
  - But I would like obe mob to drop let say a sword and a 2h sword,the other to drop only main armors,third boots and helmet etc... meaning..to go to a spot and know I can get there light armor and 2h-sword..not everitying and hope to drop my weight.
  - that can be done after we finish and start to work on the world map and positions... Now every mob is on top of the next ...
  - later I'll want a "ork settlment" where are 5 different ork types and I go there for lvl up, and several different settlements and zones with meanings.
  
- [~] - We must do the auto buff potions/scrolls tab
  - they are buff-named so it can be a list with each row 
    - (buff name) [potion-checkbox][scroll-checkbox][button to select maximum rarity]
    - example "Bulwark [x][ ][rare]"
    - that way I know it will use bulwark when it worns off and it will use the potion and wont use the scroll, and if I have rare it will use it before starting to use the uncommon
    - the priority when no effect is buffed is ->  first the rarity,if rarity is the same - scroll > potion. Meaning If I have checked scroll and potion and given them uncommon rarity as maxandnmy buff worns off, it will use uncommon scroll > uncommon potion > common scroll > common potion 
  
- [!] - I just noticed that the sheild Pdef is added to the whole Pdef (armor + helmet + gloves + boots + shield)
  - The shield defence was intended as 
    - normal: (dmg vs effective_enemypdef(armor+helmet+gloves+boots))
    - dmg when sucsessifull blocked -> (dmg vs (effective_enemyPdef + shieldPdef)) x dmg reduction
    - thats why the tank/cleric felt immortal .. 
      - a 61 tank with Bloodsteel set 683 p def + shield 1041 (tank with passives make shield def from 256 to 358)-> thats 50% increace in defence + 34% block shance and 28% dmg reduction (which is ~10% effective dmg reduction) and unintetionally u get 60% dmg reduction
      - same for mage - increases his defence by 50% -> ~532 to 788 (with 4% dmg reduction by block/dmgreduction)-> unintentionally gets 54% dmg reduction 
    - two ways out of this situation
      > 1. Keep Current shield Pdef and dont add it to the overal Pdef stat -> when sucssesfull blocked then to take it into consideration
      > 2. Lower the shield Pdef (the inspiration uses .1 - and is considered default 20% to block - so the defence is about 5 times more than if it was permenent) so we cut the current pdef of a shield 5 times 
      >   - 256 Bloodsteel Aegis -> 51 Pdef -> tanks passives x1.4 = 71 Pdef added to overall defence stat
      >   - this way we can remove the `Shield def:` row in stats
      >   - this becomes ~10% increase in defence (bloodsteel stats the other grades can varry) for every wielder
      > 3. same as .2 just to increase the shield Defence increase skills/passives 
      >   - 40% tanks to become 200% - because a inspiration 50% + x40% passive = 70% dmg reduction on a suscesfull block for a tank and 50% for others
      >   - tanks block about 80% with the calculated 70+% thats average 56%  dmg reduction 
      >   - and if we dont change it the mage will reduce avg ~15.5%(9.5% pdef increase and 6% S grade shield block_X_reduction) while the tank ~24% ... (10% increase in pdef + 14% S grade block_X_reduction)
      >   - but if we make a 200% (current 40% x 5) passive 51 -> 153 -> 18% pdef increase + 14% averageBlock = 32% - which is good now for 61lvl without the 3rd class kists ... later this for a tank will become 50-60% and we will come to the end game reduction -> while keeping other classes mortal with a shield (just a help for a cleric -15%received dmg -> not immortality)
    - the shield enchant should become an armor +3 not +9 (i was considering that it worked only in block state)
    - Im leaning to .3 - I like how it looks and sound - if its harder to make lets discuss it
    - i know .1 is just a 2 formulas change but with .3 it means leaving one hand open u can equip another defence item (1h less p/mAtk so u get a pdef) -> with .1 its invisible
  
- [] - New formulas
    - We also should build a physical skill reflect -> a warrior can get a 30% chance to reflect a physical buff (40+ 15%, 76 - 30%) - u can add as well a % dmg reflected
      - so we can have a 100% change to reflect 15% p skill dmg
      - or 15% chance to reflect 100% p skill dmg
      - default warrior @40 -> 0.15 chance x1 reflected; @76 -> 0.3 chance x1 reflected 
    - Also we will need a chance to reflect debuffs - tanks get 30% chance to reflect a debuff -> u cast on tank he reflects u get the debuff
  
- [] -
  
- [] -

---

## 62. 0.58.0 — the evasion root cause + your four number rulings

Full detail: none in `TestChecklist.Unity.md`, it is here. **Your finds #2, #6, #12 and `49a`.**

**The evasion root cause (your find #2 — the +32).**

- `62a` [x] - 🔴 **The +32 is gone.** Build the exact character you measured: **Elf Phantom, level 60,
  35 AGI, light armour, no set, no weapon attribute, no buffs.** Evasion must read **108**
  (60 + 35 + 13), not 140. The cause was `Discipline.Phantom` carrying a flat `Evasion: 32` (and
  Trapper +12) from a placeholder table written before the evasion budget existed. ->

- `62b` [x] - **At level 90 the same build reads ~147**, not 182 — your second data point. ->

- `62c` [x] - **Evasion no longer jumps at the DISCIPLINE change (40).** That jump was the bug's
  signature. Take a discipline and watch the number: it must not move. ->

- `62d` [x] - ⚠ **Phantom and Trapper did not just lose the budget — it moved to MaxHp** (Phantom +120,
  Trapper +45). Same survivability role, but through a channel that touches neither the accuracy
  contest nor any damage number. Say if you would rather it went somewhere else. ->

- `62e` [~] - **The new rogue ultimate `Evasion Boost` exists at 28** — +20 Evasion, cancel-resist ×1.8,
  30s, 900s reuse, 20 MP. ⚠ Two channels in your CSV were **NOT built**: "skill evasion ×1.25" and
  "magic evasion ×1.1". The game has exactly one evasion channel; dodging a physical *skill*
  separately, and dodging *magic* at all, are new mechanics — I left them out rather than fake them.
  **Tell me if you want them as real mechanics and I will build them.** -> the magic evasion should be magic fail chance like 3-4
  **✅ BUILT in 0.60.1 — see `69d`.** Not an evasion roll: **+4 points of fail on spells cast at you**
  (the top of your 3-4). "Skill evasion ×1.25" is still open — say the word.

**The weapon speed table (your find #12).**

- `62f` [x] - 🔴 **A 2H is now slower than a 1H.** Attack speed base by type: **Dual 433 · 1H sword 379 ·
  1H blunt 379 · 2H sword 325 · 2H blunt 325 · bow 293 (very slow bow 227) · weaponless 300** — your
  numbers exactly. The bug was that the code folded 2H into 1H before reading the table. ->

- `62g` [x] - **Cast speed is ×1 for every weapon type.** It was blunt 1.0 / everything else 0.8, which
  double-charged the wrong-weapon rule (Spellcaster Mastery owns that) and contradicted your profile.
  Hold a sword, a blunt, a staff: cast speed must differ only by your passives. ->

- `62h` [x] - 🔴 **THE CONSEQUENCE, and it needs your ruling.** Rogue duals vs champion 2H, same-level
  mob: rogue/warrior went **0.92× → 1.05×** at level 20 and **1.22× → 1.37×** at 36. The rogue's DPS
  did not change — the champion's fell ~14%, purely from 325. **The rogue now out-damages the champion
  at every level 20-36.** In the inspiration game a 2H compensates with much higher P.Atk; ours does
  not. ⚠ **Your CSV has since landed and did NOT fix this** — 0.59.1 raised the 2H only at S (532 vs the
  1H's 437). The 20-36 band is exactly as described. Still yours to rule on. -> the champion have enough Patk boosts skills/passives while dagger rely purley on blows
  **✅ RULED, NOTHING CHANGED.** Your answer is that the comparison was the wrong one: the champion's
  P.Atk comes from his KIT (masteries + buffs), the dagger's damage comes from blows landing, so a raw
  weapon-speed DPS ratio does not describe either. **No retune** — the 325 stands, the 2H P.Atk stays as
  authored, and I will not raise either "to fix the champion". ⚠ The one thing still owed here is
  `0a`/`62l`'s **auto-farm measurement**, which measures the kits rather than the weapons.

- `62i` [?] - ⚠ **Mobs were pinned to 433, deliberately.** Almost every mob is weaponless, so your
  "weaponless 300" would have slowed **the entire bestiary by 31%** (BalanceMatrix: a level-20
  champion's survival time went 126s → 189s). Mobs kept 433. One constant to change if you want the
  bestiary on 300 — but check how the game feels first. -> didn't we gave monsters weapon types ? Archer is slower but does more dmg,the fast attacking have more crit rate and more atck speed but less dmg ..etc? In game it looks that way.

**Enchant rates (`49a`).**

- `62j` [~] - **The new rates: +1..+3 safe (100%), +4..+9 66%, +10..+15 33%, +16 5%.** Your budget said
  ~51 safe scrolls for +0→+16; the formula gives 3 + 6/0.66 + 6/0.33 + 1/0.05 ≈ **50**. Enchant
  something a long way and see if the pace matches what you pictured. -> that's good just the drops are very high...I got 80 scroll by lvl 28 .. I need like 2-3 ..enchant scrolls must be for over farm not a casual one...

**Raid bosses (your find #6).**

- `62k` [x] - **Boss HP ×20 → ×100** and a new flat **Accuracy +20** by rank. A dodge build should no
  longer be able to stand in front of a boss untouched — that was the missing accuracy. -> now is perfect. An elf phantom gets hit for ~800 on a 2500 pool and without a healer (even with one its not certain) won't survive on his own. Using the ultimate still gets him hit (missed a lot more) but still hit. Ork bulwark gets hit for ~300 on a 6k pool. He blocks for ~170 and ultimate makes him get hit for ~60 ..so a tank in a road party is a must ..on his own he won't survive as he hitting the boss for 300 while the dagger gets 1-2k. So dagger + tank + healer probably will kill the boss while anyone solo can't ...as it should be. Now the treat is a real boss.

- `62l` [x] - 🔴 **One number was NOT taken literally, and you should check it.** You said "PAtk from x5
  → x20", but the boss rank multiplier has always been **×2.5** — there is no ×5 in the boss path.
  Taking ×20 literally would be an 8× jump, not the 4× your own before/after describes, so I applied
  your **ratio**: **2.5 → 10**. Kill something and tell me if it hits as hard as you meant. -> '62k' have a Boss dmg

- `62m` [~] - **The "HP boost passive ×2" needs no new code** — `MobMod.Hp` already multiplies and the
  Max HP Mod table reaches ×2 at rung 11. Put that mod on a boss and you get your 500-600k. -> boss had 260? He should have 520? Check. If something was wrong ? Still at a 260 is not a bad number. Just a party with 2 or 3 dmg dealers will kill it fast (3x ~500dmg/s =1500/s => ~180s to kill a boss) need more hp ... 520 would have made it 6mins which is okish for a field boss...a world should take an hour for ~10 parties (~50 DDs)

**Your CSV syncs.**

- `62n` [x] - **Tank Defencive Wall is 30s**, not 60. ->

- `62o` [x] - **Rogue light-armour mastery speed is flat +7**, not ×1.07. ⚠ `StatMods.MoveSpeed` existed
  but **nothing read it** — it is consumed now, flat before percent, rungs 3/4/5 at 28/32/36. ->

- `62p` [x] - **Bow Expertise moved 28 → 36.** ->

---

## 63. 0.58.1 — the last four bugs, the QoL six, AGI, and the stat-swap rework

⚠ **This is the build that needs the db moved and a new APK (protocol 15).**

**The four bugs.**

- `63a` [~] - **`#11` quest reward details stack.** 5 Dash Potions render as one row reading `x5`. The
  grant path was always right; this was display only. -> it shows it as reward x5 but didn't finish the quest (the autofarm part bug)

- `63b` [x] - 🔴 **`61h` the Hollow Crypt has walls now.** The root cause was worse than reported: a
  dungeon's world was its outline's **BOUNDING BOX**, and the crypt is a diagonal band — **only 55% of
  that box is actually floor**. The world now carries the real outline. Walk at every edge of the
  crypt, including the diagonals. ⚠ Its **entrance is annexed** (the arrival safe zone sits outside
  even the old box — that was the rubber-band you felt). ⚠ Known limit: a straight walk can cut a
  concave corner (0.76% of pairs, ≤129 units) — there is no pathfinding, and both halves draw the same
  line, so it should never rubber-band. ->

- `63c` [x] - **`61d` the jail is a room.** Your 300×500 box, **one shared jail, not a cell per player**,
  and arrivals are spread instead of every inmate landing on the same coordinate — that coordinate
  was the "1px". Jail two characters and confirm they arrive apart and can pace. -> 

- `63d` [~] - **`53a` box picks are a BUDGET.** They were a set of ticks, so 10 picks meant 10
  *different* scrolls and "5 of one" was inexpressible. Now: open the Blessing Box, use the `-`/`+`
  steppers, take **5 of one + 3 of another + 2 of a third**, or 10 of the same. Both of your shapes
  should work. -> fine. Leave it like that. But defer for later - I'll want to be able to pick 5 and I get my 5 scrolls + the box for the other 5. Now is OK

**The QoL six.**

- `63e` [x] - **`52b` the skill card reads the FLAGS, not the prose.** It printed the authored
  description, which is why Piercing Stab's was stale. It now prints blow / double / crit / flat /
  magic-crit in the resolver's own order, for every skill. ->

- `63f` [x] - 🔑 **`53e`+`61j` the rubber-band on STOP — root cause found.** The decaying error is right
  for a walk that is **interrupted** and wrong for one that **ends**: at arrival the server stream lags
  one sample, so the leftover error points *forward* while the base position is still advancing on the
  same point — the sum walks past the destination and the ease-back is the decay. An arrival now
  **holds** at the destination. Walk somewhere and stop, many times. ->

- `63g` [x] - **`54e` `/stat <name> <value>`** — your admin override, every stat. `/stat acc 999999`,
  `/stat eva 99999`, crit damage, crit rate, 12 stats in all, plus `m`/`a`/`c` routed into the
  existing `/spd` fields so it is one command. **`/stat` alone clears everything.** ⚠ `/spd` is not
  regressed — check it still works. ->

- `63h` [!] - **`56c` the training dummies.** `dummy_magic` and `dummy_physical`, level 80, on the
  training row at x≈26500/27500. 1 damage per tick within 50 units, through the **real** resolvers.
  Stand there 10s = ~100 hits. 🔑 **A mob's WIT is a flat 5 at every level, so its magic crit is
  1.25%** — that is the number you were trying to observe, so expect roughly one crit per 80 hits. ->
    - Both dummies act as the old - they dont do nothing different - nor they have faster attack nor spell skills
    - Add titles on them like - `Normal|Physical|Magic`

- `63i` [~] - **`59r` `/title` defaults to WHITE**, and colour is gated on a **Rune of Tincture**
  (Apothecary, 40k) — click the rune to open the colour list. ⚠ **The rune is NOT consumed**: your
  words made *possession* the right, and a one-shot item could not be clicked twice. Say if you meant
  it to burn. -> works but remove the rune from apothecary (add it to admin) and it will be only event/premium bought 

- `63j` [!] - **`58a` the tutorial teaches before the pigs.** A new step type waits until you actually
  *did* the thing, credited from the handlers that already run: part 1 now asks you to open a box,
  equip twice, and use a skill; part 5 has auto-farm at your "reach 18" slot. ⚠ Two of your asks did
  **not** become steps: it asks **one** box (a player who opened both creation boxes could not conjure
  another), and the rune explanation stayed **prose** (an "open Miren's rune box" step would gate the
  whole chain on her daily). Run a brand-new character and tell me if it now teaches enough. ->
  - Cannot complete the Auto-on part of the quest. I opened both auto pots and farm clicked on save,clicked auto-on on skills clicked auto-farm clicked offline...nothing works to allow me to continue. 
  - Fighter dont have a skill (I had to use TestSkill) to continue with quest
  - the before "use skill to kill" should be: 
    - "Use Pell to go to the pigs" (i need to see a target not standing in town)
    - then "open skills -> put atkAction or bolt to bar" (need to have something on bar)
    - then "target a pig - use skill/atkAction" (need to know how to target and use atack or skill)
    - then "kill 5 pigs on your own" ...
  - I managed to get 2 Training light armors, 1 robe and 3 weapons ...
    > I start with boxes (need to remove them - no need, qhest supplies them)\
    > I open my initial boxes (+1 weap/armor)\
    > I get to the Cera - she gives me boxes\
    > I open my Ceras boxes (+1 weap/armor)\
    > I go to Pell - she gives me boxes\
    > I finish that part and I end up with 3 armors and 3 weapons ... 
  - Make no inital boxes. After Ceras talk and after I talk to Pell then to get my boxes\
    > Then so I get the boxes exactly before I need to open them\
    > Its a training items so i cant sell them for exploit but still annoying to discard "bugs" 
  - Also I think the Training gear should be with normal boxes not selection
    -  Open 
       -  armor box -> mage gets robe, fighter gets light
       -  weapon box -> mage gets wand, fighter gets sword
       -  (or if its hard to do base class difference give both)
    -  Any fighter cen get trough with a sword
    -  Other training Club and training knives can be deleted

**DEX → AGI (your find #3).**

- `63k` [x] - **Every player-facing DEX now reads AGI** — stat sheet, skill text, tooltips, docs. ⚠ **The
  four stat-swap skill IDs still spell `dex` on purpose**: an id is a persisted key, and renaming one
  would delete a 15kk purchase. Nothing you can see should say DEX. ->

**The stat-swap rework (your find #4).**

- `63l` [~] - **A rung is +1/−1, and you have NINE of them, character-wide.** A skill is one pair, and
  its level is how many rungs you put in that pair. **Raise cap +5 per stat** (so a pair caps at level
  5); no cap on selling beyond the nine. -> its a bit chaotic .. need a new place -- may be a new tab where u see what stats u selected and before u confirm a selection to show what u are changing ... otherwise is working and is good 
  - and mindwriter should not  say `(cost 25000000)` because i think it will cost me 25kk to remove them even though upper say its free -> make it `(losing 25,000,000)` or something or remove the () as whole
  > Swap Tab\
  > ```
  > Next Price: 5,000,000
  > --|--|--|--|--|--|--|--|-- (table)
  > Commited|(0)|(0)|(5)|(0)|(0)|(3)|(0)|(0)
  > buttons for plus|[+]|[+]|[X]|[X]|[+]|[+]|[+]|[+]
  > Increase +1|(CON)|(DEX)|(WIT)|(WIT)|(ATK)|(ATK)|(SPT)|(SPT)
  > Decrease -1|(DEX)|(CON)|(SPT)|(ATK)|(WIT)|(SPT)|(WIT)|(ATK)
  > buttons for minus|[-]|[-]|[-]|[-]|[-]|[-]|[-]|[]
  > --- (end table)
  > Added: WIT: +5 | ATK: +3 | SPT: -8
  > ```
  > The [+] buttons for WIT are disabled because +5 is reached\
  > once the next one is clicked and price is paid the all [+] buttons are disabled\
  > that way u can decrease by 1 the stat and pay the next price again\
  > if i have wit+5 and atk +4 for -9 spt .. i can decrease the wit to +4 and spt -8 and add Atk+1 for atk+5 for the 5kk price\
  > or I can remove all (click 9 times the [-]) and start from 1kk again (35kk all)

- `63m` [x] - 🔑 **The direction rule is DELETED.** `+5 AGI −5 CON` then `+4 CON −4 AGI` is legal and
  nets `+1 AGI −1 CON` — your worked example. Self-cancelling is allowed on purpose. -> now with list of skill is visually hard to understand

- `63n` [x] - 🔑 **Price is per RUNG and global: `1/2/3/4/5/5/5/5/5` kk by rungs already owned = 35kk for
  all nine, however you spread them.** Buy them in a few different orders and confirm the total is
  always 35kk. ⚠ That could not live in the per-skill gold field, so the charge is computed at
  purchase and **the client runs the identical computation** — if the two ever disagree you will see a
  price change when you tap. ->

- `63o` [x] - **The tenth rung is refused, and so is `+9` into one stat** (the +5 cap). ->

- `63p` [~] - **The reset NPC reports what forgetting a skill FREES**, not what it cost — under
  position-based pricing there is no per-skill answer. Say if that reads wrong. -> the - (cost 25kk) from 63l

- `63q` [x] - **The class lists are yours**: fighter ATK↔AGI, ATK↔CON, AGI↔CON + one-way **+SPT −ATK**;
  mage ATK↔WIT, ATK↔SPT, WIT↔SPT, AGI↔CON; **cleric = mage**; buffer keeps all. ->

---

## 64. 0.58.2 — magic gets its own landing formula, and mRes becomes damage reduction

Full detail: `docs/design/CombatResolution.md`. **This is your `57d`, and it was a real bug.**

- `64a` [x] - 🔴 **The bug you found: the fail chance was bit-for-bit identical with a bow and a wand.**
  Spellcaster Mastery's "magic accuracy ×0.5" halved a stat (`MagicFailResist`) that is **0 on
  everyone**, because no skill in the game grants it. Half of zero is zero. It was also pointing the
  wrong way — the stat could only ever *subtract* from fail, so nothing could raise a fizzle. ->

- `64b` [x] - **The new formula is yours**: `fail% = round(1.3^(defLvl − atkLvl) × defMod × weaponMod)`,
  clamped to 95%. **Parity = 1% fail / 99% success**, your anchor. Cast a lot at a same-level dummy. ->

- `64c` [~] - 🔴 **Now a bow caster actually fails.** `weaponMod` is **25** for bow/dual/bare-handed (you
  offered 25 or 50; 25 is your own worked example). Expected success — **wand: 99 / 96 / 86 / 61 / 5%**
  at Δlevel 0 / +5 / +10 / +14 / +18. **Bow: 75 / 45 / 7 / 5%** at Δ 0 / +3 / +5 / +6. Hold a bow at
  parity and you should fail about **one cast in four**. -> works.. But hitting above the 0 difference is not failing. And start to feel it at 3-4lvk bellow. So if we can make a floor ... Now 95% chance magic to succeed and 5 to fail.. If we make it a strong 50% with wrong weapon celing((formula),0.5) that is always 50% on the norm  .. And we can add that to the wrong penalty ... L1 - 0.5 ..L5 ..0.05(the min) tinsusceed a spell

- `64d` [x] - **The clamp is 95%, not 100** — your playtest-19 "nothing is unhittable" ruling applies in
  this channel too. Even at a hopeless level gap, 5% of casts land. ->

- `64e` [x] - ⚠ **The bow penalty FADES when you punch down.** The formula is multiplicative, so at
  Δ−10 a bow caster is back to ~98% success. That is inherent to your model — flagged, not patched.
  Tell me if you want a floor under it. ->

- `64f` [x] - 🔑 **mRes was never a fizzle chance — it is DAMAGE REDUCTION now.** Your words: *"the
  problem was we didn't have a mdmg reduction, that's why we converted them to a floor."*
  `healer/nuker 20-35.csv` says "magic def +20, mRes +5%", so it is now a divisor inside M.Def, the
  same shape as pierce defence. ⚠ You wrote "1.25 → ×0.75"; I pushed back and you took the divisor, so
  **1.25 → ×0.8** — that keeps the mob ladder (1.11/1.25/1.43/1.67/2) exact reciprocals of 0.9…0.5. ->

- `64g` [x] - 🛑 **Deleted, do not expect to find them**: `MagicFailResist`, `MagicFailFloor`, and the
  whole fizzle-FLOOR concept. Magic no longer calls the physical avoid roll at all. Physical is
  unchanged — sanity-check that a melee miss still behaves exactly as before. ->

- `64h` [x] - **The tank's Anti-Magic is the `defMod` ×2**, and you already ruled the tank is fine
  (25% magic damage reduction vs the mage line's 30%). Lv2/Lv3 stay at 43/76 as ×2.5/×3 until your 40+
  kits land. **Nothing to decide — this row is here so you don't re-report it.** ->

- `64i` [~] - ⚠ **No gear grants mRes yet, and no mob has a magic-resist entry** — the ladder exists in
  `mobs_passives.csv` but nothing feeds it. Expected gap, not a bug. -> We had a anti magic mobs (lower pdef more mdef) and anty  physical (less m def more pdef) - this should feed your mres passive

---

## 65. 0.58.3 — your weapon numbers + the bestiary tab

**Your queue item (1).** No schema change; **protocol stays 15** (the new field is additive).

**The weapon half — this was your first CSV pass of 2026-08-11; the second one is §67.**

- `65a` [x] - **The ×1.166 2H P.Atk raise is REVERTED** — you ratified nothing, so it went back. ->

- `65b` [x] - **The F rung of all 8 weapon lines is re-authored** from your file, and a caster's M.Atk
  now sits **above** its P.Atk at F. ⚠ 0.59.1 moved the **wand** again — F is **19/22** (was 22/23) and
  its level-52 rung is **155/132** (was 140/122). ->

- `65c` [x] - **`training_bow` is deleted.** Nothing should reference it. ->

**The bestiary — your two UI asks from playtest-20.**

- `65d` [~] - 🔴 **The target window no longer blanks when you retarget.** Tap Info on a mob, then let
  auto-farm pick a new one: the sheet **stays on the mob you opened**. It used to key off the current
  target, so the drop table you were mid-read of vanished. -> the problem is when i select a target and select my next target manually before the first dies -> then the 1st dies and closes my second so i need to retarget
    - make it so if(target == current target && currnet target dies){ closes }; if (current != targetDies) no close... PS: "DIES" not "DEAD" - I still can retarget death targets (mobs dont leave a corpse but still later if we have a necromancer spells we will need corpses - or something ... not importnat now)

- `65e` [x] - **The title says `[pinned]`** once the sheet stops describing what you are actually
  fighting — so a held-open window can never quietly show the wrong creature's numbers. ->

- `65f` [x] - **A third tab: Skills** (mob-only, like Drops), showing the creature's **actives and
  passives together** — the passives **moved here** from the Stats tab. Effects stayed in Stats,
  because those are what is on it right now. ->

- `65g` [x] - **A mob with no kit says "None — this creature only attacks"**, and that is correct, not
  missing data: only casters and bosses are given skills at all. ->

- `65h` [x] - **The numbers are level-resolved** — the server formats these lines, so the same spell id
  reads as a different power on a level-20 and a level-70 caster. Check the same mob at two zone
  levels. ->

---

## 66. 0.59.0 — CRAFTING is reachable at last (+ the admin gear list was lying)

⚠ **New APK — protocol 16.** No schema change; `game.db` is fine for this build alone (the AGI reset
from 0.58.1 still applies if you have not done it yet).

🔑 **Nothing about crafting is new except the window.** Professions, refinement, recipes, blueprints and
mats-primary drops shipped 2026-07-06 — the phone simply had no way to reach any of it. So if a NUMBER
here feels wrong, that number is old and unplayed, not something I just invented.

**Getting started.**

- [!] - With the current method
  -  A lvl 30 - Potion Crafter - had crafted 450 uncommon potions - which saves him ~100k gold and he can make out ~34k if sells them in the shop (I crafted about 15- uncommon wood)
  -  A lvl 30 - Scroll crafter - had crafted 690 uncommon attri scrolls - dont know the store price as i cannot buy them ... and he can make out ~12k 
  
- `66a` [~] - **Menu → Craft** opens the window, and with no profession yet it shows the five choices,
  each saying what it refines and what it makes. It is on the MENU and not at an NPC on purpose: every
  material rarity drops in the field and refining what you just picked up should not need a trip to
  town. Say if you would rather it lived at a craft NPC. -> better at NPC - and craft happens with their respected masters
    - U go to the "Master apothecary Roger" or watever 
      - U accept a quest 
      - he explains what a apothecery can craft and other means of aquirering the items(potions) like drop/bosses etc to lure you so u become one of his aprenteces..
      - u compleate the quest and u can take his proffesion
    - The proffesion should not be final (i know i told you that is final, but now i changed my mind after getting a proffesion that i dont like.. sorry)
      - Crafting increases lvl(crafting not char) 
      - at crafting exp moods (0,5,15,30,50,100 - a difference between crafting levels that need for next craft) u aquire new rarity/better stuff to craft (all call them L1~l6)
        - After quest u become l1 -> to lvl up to l2 u need 5-0 = 5diference -> x10 crafts per difference of same level or 5x10x0.8 of next(l2) -> to lvl up to l3 u need 15-5 = 10 -> 10x10 = 100(l2) or 100x0.8(l3) -> etc...
      - a lvl takes lets say 10 items of the same grade to craft - lower 1 grade = 3 times more, higher 1 grade gives 20% more exp (so ~8 items), a -2 grades dont give of exp
        -  if im L3 - 15~30lvl - so 15lvls needed to L4
        -  if i use L3 crafts i need 15(levels needed)x10(crafts per level) = 150 crafts to lvl L4 -> if i use L2 crafts i need 450(15x10x3) -> if i use L4 ill need 120(15x10x0.8) -> L1 does nothing only craft result no exp, L5 should not be available
        -  if im L1 i have common mats (that are dropped) and common potions (that can be crafted) and in L2 tab i see only the uncommon mats (that can be sintesized from common items) and none of the uncomon potions (dimed like now) that i can craft -> once i lvl up to L2 i can see the uncommon potions + l3 rare mats
      - if some1 desides that he dont like the proffesion can go to his master and quits (losing all his levels)
        - then he can go to the other master and start the quests and at lvl 0
    - crafts need char level + crafting lvl => lvl20 char must lvl up to craft better items not i just to make 10 chars to sit in town and craft ..
      - L1,2 crafts need lvl20 (2nd class) char
      - L3,4 needs 40(3rd class)
      - L5,6 needs 76(4th class)
      - if i start to craft i can lvl up to L2@100% at lvl 20 - but my exp freezes until i reach the next class - i need to become 40lvl + 3rd class then the l2@100% becomes l3@0% -> and same for l4@100% untill 76+4th = l5@0%

- `66b` [x] - 🔴 **Choosing is PERMANENT and the confirm says so.** Pick one; the window switches to that
  profession's pages, and the choice **survives a relog** (it saves immediately now rather than waiting
  on the 60s autosave). Try to pick again — it must refuse. ->

- `66c` [x] - **Admin → Class still sets the profession directly** and the craft window follows it
  without reopening. That is your bypass for testing all five. ->

**The pages.**

- `66d` [x] - **Refine** — 5 of the same type + 2 cross-profession, one rarity up, guaranteed. This is
  the trade engine: your own type is the only one you can refine, so the 2 cross mats have to come from
  a drop or another player. First rung unlocks at **level 20**, then 40 / 61 / 76. ->

- `66e` [x] - **Gear** — every set piece your profession makes. ⚠ A Weapon Smith has **62** of these and
  an Armour Smith **63**, ordered by level, most of them dimmed. Tell me if that is too long a scroll on
  the phone and I will add a grade filter. ->

- `66f` [x] - **Goods** — a Potion Master's 12 potions, a Scroll Scribe's 6 scrolls (the D and C enchant
  scrolls plus the attribute pair). A smith's Goods page and a scribe's Gear page are correctly empty
  and say so. ->

- `66g` [x] - **Mats** — every material with what you hold, laid out type by type, your own type marked
  `(yours)`. Ones you have none of are still listed on purpose: the thing you are short of is exactly
  what you would hide by listing only the bag. ->

**A craft.**

- `66h` [x] - 🔴 **The ingredient that is stopping you is RED.** Open any recipe you cannot afford: each
  input reads `have/need`, green when you have enough. This is the whole point of the row — say if the
  numbers are hard to read at that size. ->

- `66i` [x] - **A guaranteed craft goes straight through; a risky one asks first** and names the odds,
  because a failure still eats the materials. Potions are 90%, scrolls 80%, everything else 100%. ->

- `66j` [x] - **A locked recipe is dimmed, not hidden**, and says which of the three reasons it is:
  *Needs level N*, *Blueprint not learned*, or simply red ingredients. ->

- `66k` [x] - **Blueprints.** An A/S-grade set piece needs one blueprint to UNLOCK and **one more every
  craft** — so the first costs two. The blueprint is listed as an ingredient in the row, not just as a
  gate. Admin → Items → Blueprints gives you all 36. ->

- `66l` [x] - ⚠ **Two professions can craft NOTHING until level 20** — Potion Master and Scroll Scribe
  have no level-1 recipe at all, so a fresh character who picks one gets an empty window. The three
  smiths have 6-14 recipes at level 1. Authored, not a bug, but it reads as broken — tell me if you want
  a cheap level-1 recipe for those two. -> and my luck i picked exactly those :)

**The admin bug this build found.**

- `66m` [~] - 🔴 **The admin Equip tab has been giving you 70% gear.** It filtered on `Epic`, which was
  right until the rarity ladder was re-anchored and the authored tables became the **Mythic** rung with
  every lesser quality a derived copy of it. "Level 76 → Adamantine Blade" handed you the *Epic copy*:
  **P.Atk 196 where the real item is 281.** It never looked broken because the Epic list carries the
  same levels 1/20/40/52/61/76/80. **Every balance number you took off admin gear since the re-anchor
  was ~30% light** — worth knowing before you re-measure anything. Fixed; check a level-76 weapon's
  details now read the authored number. -> 
    - it works now but can u make the singgle item to become "single item selection box" for rarity
      > I open Admin > weapons > L40(D) -> two fix choices:\
      > 1. I see a Common, Uncommon ... Mytic boxes -> I pick the rarity -> I open it and select 1 - Bow/Sword/..Etc
      > 2. I see Blade,Fangs ... Wand boxes -> I pick the type -> I open it and select 1 - Common/Uncommon/...Mytic\
    
      Pick wichever is easier to implemment - I can live with eather

- `66n` [!] - **Admin → Items → Crafting materials**: all 25 at x200, plus **give-everything x500**.
  Sized for a real craft — one E-grade body is 100 commons and 50 uncommons, so a x10 button would be
  theatre. -> I made the mistake to spam the x500 ... didnt know it was cycle - each 5 mats 500 times 1 by 1 ... -> when i open the Mats tab i see each sinlge item increasing 1 by 1 500 times and going to the next ... now the game is Stalled  (had to restart) it froze somewhere in between the 6500-7000 one mat in the middle was at ~6800 after relog it become 9k to all... - Make a delay or prevent me from spamming until i receive the all 25x500 mats

---

## 67. 0.59.1 — the S grade is AUTHORED, and it finally has set bonuses

⚠ **New APK, but protocol stays 16** — no DTO changed, and **`game.db` survives this build**. Install
both halves anyway: 0.59.1 is *only* catalog numbers, so a mismatched pair shows you the old S grade
and no set bonus while looking perfectly healthy.

🔑 **This is your second `gear_sets.csv` pass of 2026-08-11, marked "Eddited by him" per row.** Almost
everything here is your own number coming back to you — what is worth your time is whether the *shape*
feels right in play, not whether the digits match your file.

**The structural change: nothing is derived any more.**

- `67a` [x] - 🔴 **`SGradeOverA` (a flat ×1.60 over the A row) is DELETED.** Every weapon, body, shield,
  accessory and jewel now carries its own authored level-80 row. Your numbers are a **cut** against the
  old derivation almost everywhere — so if S feels weaker than you remember, that is you, not a bug. ⚠
  A table with no 80 row now simply has **no S item**, which is a visible hole rather than a silently
  invented number. ->

- `67b` [x] - ⚠ **Armour was cut harder than weapons, and it is a real TTK change.** Bodies and
  accessories land ~×1.33 over A; weapons ~×1.55; jewels ~×1.45. **Offence outruns defence at S by
  ~17%.** Fight something at 80+ and say whether endgame now kills too fast. ->

- `67c` [x] - **The weapon S column, as authored** (old derived → yours): 1H **450/211 → 437/192** ·
  duals **434/211 → 382/192** · bow **930/211 → 794/192** · wand **360/280 → 360/256** · staff
  **438/309 → 426/281** · 2H stays at your **532/192**. The bow lost the most (−15%) because derivation
  was compounding its already-outsized A P.Atk. ->

- `67d` [x] - **Two rungs below S moved too.** The **duals A P.Atk 271 → 246** (duals were the one
  fighter line whose A did not sit on the shared shape), and the **level-40 bow is one item again** —
  the 316 row is deleted and the 323 row ships at `as:293`, per *"Remove (this was the very slow bow)"*
  / *"Build this => as:293"*. ->

- `67e` [x] - **The F jewel rung was RAISED** (necklace/ring/earring 18/9/13 → **25/12/16**), and it
  fixed a real drift: the M.Def band ratio went 1.80× → **1.51×**. Jewels are the only source of M.Def,
  so low levels were genuinely under-supplied. A level-1 caster should stop feeling like paper. ->

**The S set bonuses — brand new, three of them.**

- `67f` [x] - 🔴 **THE ONE TO WATCH: light S carries flat crit damage +200.** For scale, 2H Weapon
  Mastery at level 5 is **+106** — so a rogue in S Nightleaf roughly **triples** his flat crit-damage
  term. You already ruled it stands (*"the 35 kit is 100 .. a 40+ kit have a 600 .. so its not of a
  concern while no 40+ kits yet"*), so this row is not asking you to re-decide — it is asking you to
  **hit something with it and see**. ⚠ When your 40+ kits land, re-read it against them; do not let me
  quietly retune it. ->

- `67g` [x] - **21 orphaned pieces → 0.** Before this, an S body plus S accessories completed **nothing**
  — the top grade was the only one with no set identity. Wear a full S set of each weight and confirm
  the set name appears in the stat sheet. ->

- `67h` [x] - **Heavy S "Ironforge"** — STR +3, CON +2, AGI −2, MaxHP +550, **crit rate +10 points**,
  magic resist 2%, melee vamp 2%, CC resist, **PvP damage taken ×0.95**. It is the first set in the game
  to carry crit rate at all. With its shield: P.Def/M.Def +6%, ShieldDef +30%, reflect 5%, and a
  **second** ×0.95 PvP — your CSV repeats it in the shield clause, so shield-up is **×0.9025**. Say if
  you meant it to compound. ->

- `67i` [~] - **Light S "Nightleaf"** — P.Atk +5%, attack speed +5%, MaxMP +300, AGI/STR +2, CON −2, and
  four channels light never had: **move speed ×1.03**, crit rate +3 points, the +200 crit damage above,
  evasion +3, PvP taken ×0.95. ⚠ Move speed is applied **flat then percent**, matching the passive path.
  Check the speed number is not over cap. -> it shows the flat +200 crit dmg in the stats but the Leather armor descritpion dont have it 

- `67j` [x] - **Robe S "Arcanum"** — the A set with **M.Atk ×1.17 restored** and INT +2. So the caster's
  magic-damage step from A to S lives in the **set**, not the staff. ->

- `67k` [x] - 🔴 **A (76) robe was RE-AUTHORED DOWNWARD and this one you WILL feel below 80:** M.Atk
  ×1.17 → **×1.10**, plus **SPT −1**. Measured at level 76 that is **−5.6% M.Atk and −2.8% nuke damage**,
  and less MP/regen on top. Consistent with your F-rung caster re-author, but it is a nerf to the
  76-79 window — confirm you meant it. ->

- `67l` [x] - **Measured at level 85, S gear** (BalanceMatrix, before → after): **fighter** MaxHP
  5832 → **6737 (+16%)**, P.Atk 1738 → 1690, P.Def 1792 → 1777, skill 421 → 410 — tankier, slightly
  softer. **Mage** M.Atk 2039 → **2165 (+6%)** *even though the staff was cut*, because the new robe set
  more than pays for it; M.Def 1336 → 1235, MaxMP 4665 → 4243, nukes-per-bar 27.8 → 25.3. ->

**Shields stop double-dipping — your complaint, built.**

- `67m` [!] - 🔴 **You were right about the double-dip.** *"To much dmg reduction on top of the additional
  pdef when sucsessifull blocked. Mage should not be immortal even with a shield."* A shield's
  `ShieldDefense` is folded into physical defence **permanently** — it already pays on every hit — and a
  block then removed another 34-47% on top. Only the block half was cut; the flat defence is untouched.
  **Average mitigation from blocking alone: 5.1% → 1.0% at F, 15.0% → 6.3% at S.** -> the wood shield still caries 30% dmg reduction should be 10%

- `67n` [x] - **The new profile, F→S:** block chance `.15 .22 .24 .26 .28 .30 .32` → **`.10 .15 .15 .20
  .20 .25 .25`**; reduction `.34…​.47` → **`.10 .10 .15 .15 .20 .20 .25`**; crit defence `.08…​.16` →
  **`.03…​.10`**. ⚠ The crit-defence column is the **other half of the same nerf**, not a separate one:
  block resolution runs crit FIRST (the shield lowers crit *chance*; a crit that still lands ignores the
  block). ->

- `67o` [x] - 🔑 **Shield MASTERY is untouched, on purpose.** A mastery tank at S still reaches ~14%
  average mitigation — about the old shield-only number — so the nerf lands exactly on the **shield-only
  wearer, i.e. the mage**, which is what you asked for. Play a shielded mage and a shielded tank back to
  back and confirm the gap now feels like a class difference. ->

- `67p` [x] - ⚠ **The shield evasion penalty got HARSHER at the top:** `5 7 7 8 8 9 9` → **`3 5 5 7 7 10
  10`**. A low-grade shield now costs a light-armour class less; an S shield costs it slightly more. -> 
    - well if a rogue (choses 1h weapon for some reason) and equips a shield he gets a penalty - 10% chance 10% reduction -3 evas .. converts low dmg reduction for less evasion penalty ... if he gets a S shield he converts ~6% dmg reduction to a -10 evasion - if he have the 30% floor that wont be of a concern but still easier to hit and why would he have a single handed weapon as a rogue (not a bow/douals )
    - if a cleric/mage/tank uses a shield the evasion was never an issue 

**PvP damage RECEIVED — the one genuinely new mechanic.**

- `67q` [] - **The receiving half of the PvP matrix exists now.** All three existing PvP modifiers were
  attacker-side; nothing could express *"I take less in PvP"* until your S sets needed it. It is
  **PvP-only** — no PvE number moved anywhere in this build. ->

- `67r` [] - 🔴 **The weapon's +5% PvP is ENCHANT-GATED, per your ruling:** *"if a weapon is enchanted to
  +4 or more and its A or S to add the 5% pvp bonus .. as a price that u risked to break a weapon."*
  Built as grade **A(76) or S(80) AND enchant ≥ +4** → +5% to all three PvP channels (basic, skill,
  magic). Below +4 the weapon adds **nothing**. It is separate from the weapon's rolled attribute.
  Duel someone with a +3 and a +4 and confirm the step. ->

- `67s` [] - **The armour half (−5% PvP damage taken) is SET-ONLY** — it lives in the S set bonuses and
  nowhere else, by your design. The weapon half pays on every hit; the armour half needs the whole set.
  ->

**Housekeeping from the same CSV.**

- `67t` [~] - **Your four training-weapon rows are documentation, not a change** (sword 6/5, club 6/5,
  knives 5/5, wand 5/7) — they describe what already ships. No training bow or staff, consistent with
  `training_bow`'s deletion in 0.58.3. -> in the quest part i mentioned it but we can leave only a sword and a wand the others are useless (u get ur weapon by lvl 10)

---

## 68. 0.60.0 — ENCHANTING STOPS BEING A PERCENTAGE

⚠ **Protocol stays 16, `game.db` survives** — the bonus is recomputed from the stored enchant level, so
your saved items simply get the new numbers. New APK anyway: the maths lives in `Game.Shared`.

🔑 **Every enchanted item in the game changed at once.** The old rule ran EVERY bonus on EVERY slot
through `base + 0.20·base·level + level` — **×4.2 at +16** — which against the 0.59.1 ladder made a +16
S blade hit for 1851 and a +16 S set quarter incoming damage. Enchanting was worth two and a half
grades in both directions, so PvP was a count of scrolls. It is now your flat per-level table.

- `68a` [x] - 🔴 **A weapon's enchant is FLAT per level**: 1H sword/blunt/wand/**duals** +6 P.Atk, 2H
  greatsword/maul/**staff** +8, **bow by grade** (E10 · D12 · C14 · B16 · A18 · **S20**), and **+6 M.Atk
  for any weapon**. Enchant one and watch the item details climb by exactly that each scroll. ->

- `68b` [x] - **The bow row is the one deliberate outlier** — 2.5× a greatsword at S — *"as archer they
  rely on basic attack and acc so a more P.Atk jump is better, while the others should rely more on
  crit/skills"*. Measured, it CLOSES the archer's gap to the dagger (324 vs 333 dps at S) instead of
  opening one. Play an enchanted bow and say whether that reads right in the hand. ->

- `68c` [!] - **A shield's defence is TRIPLE an armour piece's: +9 per enchant, not +3.** Same logic
  inverted — a shield's reduction only pays on a block, so its enchant pays in flat defence instead.
  ⚠ It rides on `ShieldDefense`, a different accumulator from armour's P.Def, so the two never
  double-count. -> NO! the shield change

- `68d` [x] - 🔴 **Armour: +3 P.Def per enchant, plus Max HP BY GRADE** (E0 · D0 · C15 · B20 · A25 ·
  **S30**). A full +16 S set is **+1920 HP**. Jewels mirror it: +3 M.Def and MP by grade (C1 · B2 · A3 ·
  S5). Enchant a set and check the HP number moves by the right amount per piece. ->

- `68e` [x] - 🔑 **THE OFFSET IS THE SAME FOR EVERY CLASS — your ruling, don't re-report it.** That +1920
  HP is +37% for a tank and +130% for a healer. I offered weight-scaling and body-only; you refused
  both: *"an enchant is just an offset of the norm, same for all"*. This row exists so it does not look
  like a bug when you meet a very tanky enchanted healer. -> a tanky enchanted healer will only be when he is fighting non enchanted wahtever ... Some1 invest in defence gets a defence .. not a warrior invest +3 and gets the same bonus as cleric +16

- `68f` [x] - **By GRADE, not rarity.** A Common S body and a Mythic S body gain the same 30 HP per
  enchant — enchanting cheap gear of a high grade is deliberately worth it. ->

- `68g` [x] - ⚠ **Everything you did not name STOPPED SCALING**: Evasion, the robe's inherent +MP, a
  weapon's +MP, armour M.Def. If you expected one of those to grow with +16 and it does not, that is
  this decision, not a defect — but tell me and I will author a row for it. -> thats armor +hp/pdef, jewesl +mp/mdef, weapon +atk thats enough

- `68h` [~] - **The item card now prints the enchanted TOTAL even on stats the piece does not natively
  carry** — every tiered armour has HP 0, so a +16 S body owed +480 HP that the old display hid
  completely. There is also a grey **"Per enchant +N …"** line saying what the next scroll buys. -> F grade should say unenchantable or atleast remove the + bonus - u cannot get it (unless admin)

- `68i` [x] - **Measured end to end** (`BalanceMatrix` §E, full tier loadout at +0 vs +16, real
  resolvers). At S: **tank +21% dps · warrior +21% · dagger +16% · mage +15% · archer +34%**, and
  defence moves further than offence for everyone (the mage's time-to-kill goes 12.9s → 38.1s). The
  question for you: does a fully enchanted character feel about a *third* stronger, or more? ->

- `68j` [x] - ⚠ **The enchant RATES did not change** (`62j`'s +1..3 safe / 66% / 33% / 5% at +16). What
  changed is only what a level is worth. ->

---

## 69. 0.60.1 — the tutorial dead-end you hit today

**Your first find of this pass, fixed the same hour.**

- `69a` [x] - 🔴 **THE BUG: the tutorial could not be finished if you opened your boxes early.** *"u
  added the open a box in the travellers quest ... but i opened it before i got the quest now i cant
  continue."* A DoAction step is a gate, and a gate whose prop you already consumed is a wall. ->

- `69b` [~] - 🔑 **Your fix, built as the general rule**: *"update the quest to give you the boxes after u
  speak with cera"*. A step can now declare the items it needs, and while that step is current anything
  you do not hold is handed over. Part 1 carries the two **training** boxes both on its first step (so
  they arrive when Cera gives you the quest) and on the box beat itself. -> I have given better rulling in the `63j`

- `69c` [x] - 🔴 **YOUR STRANDED CHARACTER REPAIRS ITSELF ON LOGIN** — no admin grant needed. The supply
  runs from the one call every quest change already passes through, and login is one of them. **Log in
  with the character that is stuck, look in your bag, open the box, and the chain should continue.**
  ⚠ If it does not, that is the first thing to tell me. ->

- `69d` [!] - **Magic evasion, your `62e` ruling** (*"should be magic fail chance like 3-4"*): Evasion
  Boost now adds **+4 percentage points** to the fail chance of spells cast at you, for its 30s. At
  parity a caster goes from 99% success to 95%; against one punching up it stacks on a fail chance that
  is already climbing. Flat and additive on purpose — multiplying would be worth nothing at parity and
  enormous at a level gap. **Stand in front of `dummy_magic` with the buff up and without it.** -> `63h`

- `69e` [~] - ⚠ **"Skill evasion ×1.25" is STILL not built** — the other unbuilt channel from the same
  CSV row. Dodging a physical *skill* separately from a basic attack is a new resolution mechanic and
  you have not ruled on it. Say the word. -> we should build it .. 
    - normaly no1 can evade a physical skill (magic fails) 
    - currently - now on then i miss a skill which is anoying - stab fails... then stap should land but misses ... 
    - no1 evades only rogues gets a floor while in an ulitmate 25%,40% .. -> 76lvl the physical phantom gets a 90% for 15s .. 

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

None of these happens by accident; each needs a session aimed at it.

- `55f` [] - 🔴 **The big one, and still unanswered after two passes: farm a mage 10+ unbroken minutes
  at level 40+.** Your playtest-19 finding was *"mages run out of MP in 2-3 minutes"*. The Restore
  Spirit ladder was built to fix it. The design is "farm 30-40 min, rest a bit" — not free. **If it is
  still 2-3 minutes, the ladder is not the fix and I need to know.** ->

- `0a` [] - **Nuker vs champion.** They were at parity and the nuker is now ~19% ahead (0.98× → 0.84×).
  You said this needs an auto-farm run to measure — *"when I leave the chars to play alone all
  measure"*. ⚠ If you do it, do `32z` in the same sitting: auto-farm skill chains surviving a relog
  have **never** been tested, and if the chains misbehave the measurement lies. ->

- `32z` [] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
  ranks, assist-leader — and all of it **survives a relog**. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ->

- `36e` [] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
  top. ->

- `25b` [] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. ->

- `13a` [] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

- ✅ ~~**CRAFTING**~~ — your queue item (3). **BUILT 0.59.0, test at §66.** The server half already
  existed; what was missing was the client, and the admin materials/recipes you asked for came with it.
- ✅ ~~**THE S GRADE WAS DERIVED, NOT AUTHORED**~~ — your queue item (4). **BUILT 0.59.1, test at §67.**
  The whole level-80 column is your own hand numbers now, and S has set bonuses for the first time.
- **ENCHANTS — you said you want to DISCUSS them**, not that you want them built. Nothing was invented
  in the meantime; the only enchant change since is `62j`'s rate table and `67r`'s +4 PvP gate. Bring it
  up when you are ready and we design it before any code.
- **`58d` item TAGS + `/give`** — your design (timed / bound / private as tags on a **real** item
  instance, never a cloned def, plus the full `/give` command). **Not built.** It blocks `58g` and
  `59c`, which you deferred until it exists. It is also your route to Mythic gear without crafting —
  say the word and it jumps the queue.
- **`58i` purge the inspiration game's name** from comments and docs (`l2` as the game → `IG`; the
  *level* meaning stays). **Not built**, mechanical, no risk — I will fold it into a quiet build.
- **Your find #9 — resurrect / party / flag rules.** Ultimate Resurrection should be tradable (at
  least the dropped and admin copies); you cannot res a party member while **you** are flagged, though
  you can res or heal a PK while unflagged and it flags you (inconsistent); you must be able to invite
  and trade with PvP-flagged players, with PK staying trade-blocked. **Not built — queued.**
- **3rd/4th class kits** — still blocked on your 40+ CSVs. Nothing invented in the meantime.
- **`G3` mobs-as-players** — needs the document and BalanceMatrix tables first, then 2-5 real mobs as
  an experiment, per your ruling.
- **Instances** — you are holding.
- **`C4`** auto-on for buff potions/scrolls — your ruling: comes later, with the AutoPot tabs.
- **`G2` / `0e` `lb_*` + `wc_*`** — closed by your ruling: leave them. Placeholders for 40+, commented
  out, harmless.
