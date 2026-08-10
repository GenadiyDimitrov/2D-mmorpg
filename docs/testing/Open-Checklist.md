# OPEN CHECKLIST — everything untested as of **0.57.0** (2026-08-08)

> **This file is now UNVERSIONED and ROLLING.** It replaces the three per-version
> `Open-Checklist-0.45.0 / -0.47.0 / -0.48.0` files, which are gone — every item of theirs that was still open is carried
> forward below, and everything they answered is preserved verbatim in
> [Playtest-Archive.md](Playtest-Archive.md). When you finish a pass, I transcribe your answers into
> the archive and rewrite this file against the next build. One open checklist, always.

Edit this on the phone: write your comment after the `->`. Put `x` in the `[ ]` if it passed with
nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority, `?` for a
question. `[ ]` with no id in front is a free line for that section — add as many as you like.
Ids match [TestChecklist.Unity.md](TestChecklist.Unity.md), which carries the full detail for every
section number referenced here.

---

## ⚠ BEFORE YOU START

**Install `L2Clone-0.57.0.apk` and unzip `Game.Server-0.57.0.zip`** (both in `builds/`). Protocol is
**14** — it moved at 0.56.0, for the combat channel. Install **both** sides: the catalogs, skill
tables and world bounds are compiled into each, so a mismatched pair disagrees quietly rather than
refusing.

🔴 **DELETE `Game.Server/game.db` + `-shm` + `-wal`.** Not optional. Four reasons stack up from builds
you have not played yet: 0.52.0 changed columns, 0.53.0 removed the God layer (an old character
carrying `Race = 99` or a God item references an id that no longer exists), the mastery restructure
clamps `mastery_robe` from 3 rungs to 2 on login, and 0.55.0 adds three title columns. (0.56.0 and
0.57.0 add nothing — but the earlier four still apply.) ⚠ Delete the one in `Game.Server/`, **not**
the stale copy in `bin/Debug/` — that one is a decoy and deleting it will fool you into thinking you
reset.

🔴 **TEN BUILDS ARE UNPLAYED.** 0.49.0, 0.50.0, 0.51.0, 0.52.0, 0.53.0/0.53.1, 0.53.2, 0.54.0,
0.55.0, **0.56.0** and **0.57.0** — by a distance the longest unplayed run this project has had. The
risk is not any single change; it is that ten builds' worth of combat-maths reworks, a replaced
starter quest chain, a protocol bump and now a change to how *movement itself* is enforced have never
met a player. **And the build queue you set on 2026-08-07 is now empty** — there is nothing left
queued for me to write, so this pass is the only thing that moves the project.

✅ **Pre-flight is clear.** `tools/SmokeTest` ran 2026-08-07, all ~150 assertions passed, server log
clean. Nothing since (0.56.0 chat, 0.57.0 movement/grades) touches persistence, the skill bar,
subclasses or the login sequence, so it still stands. Say the word if you want it re-run anyway.

## Where to spend the pass, if you don't do all of it
Ten sections is a lot. If you get tired, these are the ones where a defect would be **expensive to
find later**, in order: **§61** (movement — the client can now edit your destination; a wrong bound
shows up as taps that quietly go somewhere else), **§53/§55/§57** (the combat-maths reworks — every
number below them assumes these are right), **§58** (the tutorial chain replaced the old starter
quests, so a new character's first hour is untested), then everything else.

---

## ✅ 0. DECISIONS — ALL ANSWERED, nothing here blocks the pass (skim or skip)

Kept for the record only. `0a` is deferred to an auto-farm run (your call), `0b` `0c` `0d` `0e` are
closed. **The one live thing in this section is a warning, not a question:** `0a` makes auto-farm the
measuring instrument, and `32z` — auto-farm skill chains surviving a relog — has never been tested.
If the chains misbehave, the measurement lies. Worth doing `32z` in the same sitting.

`0a` [ ] - **The nuker now beats the champion by ~19%** where they were at parity, because magic crit
became a real channel (§56). Measured, not derived: CHAMPION/NUKER went **0.98× → 0.84×** at level 74
in best gear with NPC buffs. The levers are the **base 50** and the **flat ×3 crit damage**, both your
numbers. Is the nuker's lead earned, or do I trim it? -> This need to be tested. When I leave the chars to play alone all measure.

  ✅ **Understood — deferred, no code change.** You will judge it from an auto-farm run, not the
  matrix. ⚠ One flag: that makes **auto-farm the measuring instrument**, and `32z` (auto-farm skill
  chains + surviving a relog) has never been tested in any playtest. If the chains misbehave the
  measurement lies. Worth doing `32z` in the same sitting.

`0b` [?] - **Accuracy cannot beat an evasion FLOOR, and +5 accuracy therefore buys an archer nothing**
against a rogue (§55e). `miss = clamp(5% + (eva − acc), floor, 95%)` — evasion is additive from its
first point, accuracy only claws back what sits above *both* the 5% base and the defender's floor.
Against a rogue's 10% floor, +5 accuracy is worth **zero** until you already out-evade him by 10+.
The +5 is the right size; the **floor** is the problem. Do you want accuracy to eat into the floor,
or is an archer simply not supposed to beat a rogue's dodge? -> 
  - Explain to me this one.
    > dagger have floor of 10%,\
    > all the passives,buffs and dex he have let say difference of 20 with a normal class/mob\
    > archer have a selfbuff that give him 10 acc then the weapon mastery +4 acc and 2 more normal buffs +4 => 10+4+4+4 = 22 + Acc\
    > clam(0.05 - 0.02 (20-22), 0.1,0.95) => so archer hits the floor of a dagger\
    > until the difference is +5 and up in faveour of evasion the floor is met\
    > is that what u ment by +5 acc dosnt give you nothing .. well if dagger invest in +10dex/-5atk/-5con and +5 eva on weapon\
    > then the difference become free +15 => clam(5% + (35-22=13),10floor) => miss = 18% -> but archer inves in +5 acc miss becomes 13% and also add a 5 dex here you go floor again\
    > Am i calculating right

  **YES — all four of your cases are exactly right**, I checked each against
  `StatCalculator.ResolveAvoidChance`. And **my framing was wrong**, not yours. The order is:
  `m = clamp(5% + (eva − acc)·1%, 5%, 95%)` → level gap → `clamp(m, max(5%, floor), …)`.
  Your cases: `(eva−acc) = −2` → 3% → floored to **10%** ✅ · lead 13 → **18%** ✅ · +5 acc → **13%** ✅
  · +5 DEX → 8% → floored to **10%** ✅.
  So accuracy pays **full value while his evasion lead is more than 5 points**, and nothing once the
  lead is 5 or less (the floor has taken over). The dead zone is the **last** 5 points, not the first
  10 — I had it backwards. Your +5 acc case is 18% → 13% miss = **+6.1% landed hits**, which is not
  nothing. ⚠ The bigger point: step 1 clamps at 5%, so accuracy **beyond** the defender's evasion is
  wasted against *everything*, not just rogues — accuracy is a catch-up stat, never a scaling one.
  🔴 **Still yours to rule:** may accuracy eat INTO the floor? I recommend **no** — the floor is
  authored as an anti-accuracy tool, so letting accuracy pierce it deletes the passive's purpose. -> Yes the floors and ceiling cannot be touched (they are there for a reason)

  ✅ **CLOSED, no code change — the floors and ceilings stay untouchable.** Recorded as a standing
  rule, not a one-off answer: accuracy is a catch-up stat that pays full value only while the
  defender's evasion lead is more than 5 points, and never pierces the floor. **§0 is now fully
  answered — nothing in it blocks the pass.**


`0c` [X] - **Physical crit damage base ×2.0 — still under research, still unchanged.** You think L2 is
×1.5 and neither of us can source it. The real question is *what the multiplier multiplies*: ours
scales the **whole ratio including skill power**, L2's scales the **attack term only** — so "should
be 1.5" may be the same ×2 on a smaller base. **A** = lower to 1.5. **B** = keep ×2 but apply it to
the attack term only (touches `CritFlatFactor` and every call site). ⚠ Rogue damage now rides almost
entirely on crit damage, so this is a big lever. -> When you land a critical hit or use specialized dagger Blow skills, critical power multipliers inflate the numerator:
\(\text{Critical\ Damage}=\frac{77\times \text{P.\ Atk}\times 2\times \text{Crit\ Power\ Multipliers}}{\text{P.\ Def}}\) -> so you were right the base as i cen decifer the formula its 2 times then the mutlipliers(buffs passives)

  ✅ **CLOSED, nothing changes** — ×2.0 stays, neither A nor B. Your source settles it. (One residual
  I am *not* re-raising: your formula puts the ×2 on the attack term, ours scales the whole ratio
  including skill power. It only diverges on high-power skills; say the word if you ever want it.)

`0d` [~] - **The gear CSV's attribute ceiling.** I found while updating `docs/data/gear/gear_sets.csv`
that the shipped roll system is the **inverse** of what that file described: **the MAX is flat across
all five grades and the MINIMUM is what ramps**. A D-grade sword and an S-grade sword both cap at
+30% crit rate; the grade only removes bad luck. Your file said the ceiling climbs per grade. Which
did you mean? -> In l2 the weapons do a flat increase in crit (+64, +90, + 109 @SGrade) a % increase depend on the base crit value - so if we do a dagger with 30% and a sword with the same sword wont benifit the same, but if we do a flat 90 its as 9% increase after all the buffs/passives and is 9% across all. But yet the only one to do flat crit rate is a bit off ... everithing is % then crit to be flat .. why ? .. we need to alter the sword to 90% crit rate ... so unbuffed sword wielder to have 88x1.9 -> 167 and a dagger 132x1.3 = 171 -> then the max a tank investing in critical sword will not have 25% hp or 15% as but 43.5% crit rate (thats pure playstile choice)

  ✅ **BUILT** — sword ceiling 30 → **90** (`RampSwordCrit = 3/15/30/60/90`). Your two numbers land
  exactly: sword `88 × 1.9 = 167`, dagger `132 × 1.3 = 172`.
  🔴 **But building it found a real defect, bigger than the question you asked.** The roll was not a
  multiplier at all — `Entity.RecomputeDerived` put it in `CritRateFlat` as `value / 100`, so a maxed
  roll was **+30 PERCENTAGE POINTS**: sword 8.8% → **38.8%**, dagger 13.2% → **43.2%**, both nearly
  at the 50% cap off one roll. That is **+300 on your 0-1000 scale** vs L2's +109 at S grade, and 10×
  your own rule for that channel (*"a flat 30 is flat 3%"* — the divisor should have been 1000).
  Worse, being FLAT it **collapsed the 3:2:1 weapon identity** the crit model exists to create, since
  the same +30 is worth far more to the weapon with the smaller base. It now multiplies — which is
  what the tooltip has always said.
  ⚠ **This is a large dagger/bow NERF at max roll (43.2% → 17.2%).** It is the number you wrote
  yourself, so I built it, but nobody has played it. Watch rogue damage in the pass. -> why u count it as a nerf .. the 430 with jsut a weapon attri was just way to OP .. the 400 we must get only after getting fully buffed
  Archer - 132x1.2(passive) = 158x1.3(single buff) = 206 => here if we add the x1.3 attri we get 267 which is 6% and without the attri do a x2 harmony we get 411 ... but if we want we can then add the attri end get to the cap 535
  Fighter - 88x1.3(buff) = 114x1.9(attri) = 217 ~same as dagger without atri then the x2 harmony here u go on a single sword 434 near the cap

  ✅ **CLOSED — you are right and "nerf" was the wrong word.** I checked all four of your chains and
  they land: archer `132×1.2×1.3 = 206`, `×2 harmony = 412`, `+attri = 535`; fighter
  `88×1.3×1.9 = 217`, `×2 harmony = 434`. Both reach the **500 (50%) cap only when FULLY buffed**,
  which is the design — the old build handed you 430 off a weapon roll alone. No change; I was
  measuring the max roll in isolation instead of against the buff stack it is supposed to need.
  Still worth **watching rogue damage** in the pass, but as an observation, not a suspected regression. -> 

`0e` [!] - **`light` body armor at 52 (202 P.Def) is WEAKER than at 40 (218).** Authored that way in
your CSV and shipped that way, so the C body is a downgrade for anyone who already has the D one.
Typo, or deliberate (the 52 line trades defence for its DEX/P.Atk set bonus)? -> My bad. When i did the csv i added to the 40 sets a boots pdef as well -> 179 (fixed the csv)

  ✅ **BUILT** — synced `Items.cs` to your CSV edit: the light body array and all three
  `light_t40_*` variants 218 → **179**. Light P.Def is monotonic again: 86 / 125 / **179** / 202 /
  220 / 249, so the C body is an upgrade over the D one. -> 

---

## Open-Checklist.md rows format change

When writing the OpenChecklist file can the checks be one of the two:

- 1.
  > "- [ ] - `99zz` - @Description -> @my_Comment" \
  > and after the -> for my comment to have a empty row\

  >[!Tip]
  > - [x] - `56b` - An **elf mage in the full kit** hits 10%, and ×2 Insight puts him at **20% — the cap
exactly**. -> 
  > - [ ] - `56c` - A **mob** still crits occasionally (~1.25%), not never. -> 

  >[!Note]
  > The .md-Viewer [ ] is a empty checkbox and [x] is checked and '-' is new unordered list and is easier to read

- 2.

  > "- `99zz` [] - @Description -> @my_Comment" \

  >[!Tip]
  > - `56b` [] - An **elf mage in the full kit** hits 10%, and ×2 Insight puts him at **20% — the cap
exactly**. -> 
  > - `56c` []- A **mob** still crits occasionally (~1.25%), not never. ->

  >[!Note]
  > The .md-Viewer '-' is new unordered list and is esier to read\
  > This '2.' is the same as your writing just with a dash infront each checkId

I lean to .2 because its same just with a single dash infront - not so much change (we have [],[!], [?], [x] so the md viewer cannot destinguish them as checkbox unless its [ ] or [x] so no point in changi it to .1)

## My Finds

- [!] - Tanks Ultimate is 30s not 60 => fixed the csv

- [!] - Why the dagger evasion is so high ? 
  - acc 98- elf 35dex lvl 60 => 35+60 = 95 + 3(passive) 98 ok ... 
  - evasion is 140 ?!? -> @60 + 35DEX = 95 evasion + 13 eva passive = 108 -> I have 140 (only light armor - no set, no weapon attri, no buffs.. only 35 DEX and Phantom class) where the other 32 come from ? 
    - the evasion_mastery should only increase the floor .. 
    - once i turned rogue my evasion jumps alot (and it shuldnt)
    - now this 32% difference is 32% on a 20 floor, if its was 98 vs 108 its the 10 differecen and its a 20% floor hit all the time 
    - while the archer will stay at 10% difference on a 10% floor 
    - later all rogues will have an ultimate that increases the evasion with 20-30 so it will jump from 10-20 difference to ~40-50% to evade ... but for 30 sec ..
  - evasion 182 @90 -> easy (slow, but easy) Elite farm (only common pots) -> 90+40DEX+13EVa+4Buff = 147 at lvl 90 not 182 -> +35 evasion from unknown source -> vs 120 ACC - 62% evasion .. else it will be 30% floor for mele and 27%  for archer on 10% floor (mytic light armor - no set, no weapon attry)  
  - CSV fix
    - I updated the CSV - rogue - at lvl 26 there is an L1 ultimate skill (L2-@55 -> eva +30, pys skil eva x1.4, mskill eva x1.2; mp cost 50 - everithing else the same (cd,duration,80% cancel resist))
    - Also speed is +7 flat not x1.07
    - The Bow expertice was with the 36 lvl skills but it was lvl 28 so i fixed it as well

- [~] - Can we rename `DEX` to `AGI` - everywhere
    
- [~] - We need to change the Stat swap passives with something else
  -  Now +5 dex +5  atk - 10 con 
  -  and I need +2 Dex -2 Atk , +3 Dex - 3con 
  -  We need to make it
     -   +1 physical stat to -1 physical stat (atk,con,dex)
     -   +1 magycal stat to -1 magycal stat (atk,wit,men)
     -   to a max +5 for a single stat
     -   and maximum +9 -9 for all stats combined
         -   can be +5+4-9,+1/5/3-1/5/3, 
  - for example: +5dex - 5con, +4 atk -4con
    - we will remove the "stupidity check" where you can cancel yourself
    - +5 dex -5 con , +4 con - 4dex => +1dex -1 con
  -  It still can be passives but buyable from mindwriter
     - fighters can increase ATK-DEX, ATK-CON, DEX-CON, DEX-ATK, CON-ATK, CON-DEX, SPT-ATK
     - mages can increase ATK-WIT, ATK-SPT, WIT-ATK, WIT-SPT, SPT-ATK, SPT-WIT, CON-DEX, DEX-CON
     - clerics are the same as mage - we have a passive to balance the increase in matak with mele weapon 
  - Prices:
    - 1~9 items now we have 1,2,3,4,5 = 15kk for 1 - can have 3 (mage) so 45kk for all 15 stats (3kk per stat)
    - now we will have 9 stat -> +1 ~ +9 => gold [1,2,3,4,5,5,5,5,5]kk/lvl => 35kk for all, first 15kk are the same - the last 4 stats for 5kk ea

- [~] - Need to rework the Evasion vs Acc
  - Elf dagger @60 - 147 eva (143 unbuffed, 140 without set) vs Treant 90 acc ... 
  - With occasional rare potions and NPC buffer soloed the Boss
  - Dmg is ok (the boss is weak) 1400-2000+ stabs for 58k hp ...

- [~] - Raid bosses need a 
  - Boss passive
    -  HP from x20 -> x100 (from 50-60k to 250-300k)
    -  Acc +20
    -  PAtk from x5 -> x20
    -  MAtk seems ok
  - Hp boost passive x2 hp (250-300k to 500-600k)

- [!] - `Frost bind` magus skill makes training dummies go from 1kk hp to 5k and same for elites .. they lose their hp bonus
  Dont know if its only for this debuff or no. But need investigation

- [!] - When casting skill (stab) my target is lost for the duration of the cast ..then Back again (only physical "stab" haven't tested with others yet)

- [!] - Few resurrect/oarty things ...
   - ultimate resuractions are untradable... They should be tradable atleast the one that drop and from the admin menu
   - cannot resurrect a party member when flagged ... (if I'm not pvp flag I can resurrect party member even if he is pk) and I become pvp flag - same for healing)
   - need to be able to invite pk/pvpflag players to party and trade with pvp (pk cannot trade) ...

- [!] - Elder Marius after completing the 1st quest (2nd class) gets an "!" symbol and no quest to give.

- [!] - Quest reward in details is listed as single items .. X5 Dash potions are 5 rows ..not a single with "x5".

- [!] - a 2h wepon have the same atack/speed as 1h. And blunt and sword have different cast/atack speed - they shouldnt.
  > - All wepon should have the same cast speed x1 (no weapon changes cast speed for a weapon type, only passives)... 
  > - Atack speed differs:
  >   - Knives are fastest (433 - Very fast)
  >   - 1h sword and 1h blunt (379 base attack speed - Fast)
  >   - 2h sword and blunts are default (325 - Normal)
  >   - bows are slowest (293 - slow | 227 - Very slow)
  >   - Weaponless (300)


---

## 49. 0.49.0 — the enchant rework (`D1`/`D2`)

Full detail: §49. Three scroll TYPES × six grade bands = 18 scrolls.

`49a` [~] - Three scroll types behave differently: one **breaks** the item, one drops it **−1**, one is
**safe**. -> the enchant scrolls work. Need to change the enchant rates.
   > +1~3 - safe (3 enchants, to enchant avg-3 scrolls)\
   > +4~9 - 66% (6 enchants, avg-9 scrolls)\
   > +10~15 - 33% (6 ench, avg-18 scrolls\
   > +16 - 5% (1 ench, avg-20 scrolls\
   > so a weapon +0~16 need ~51 scrolls, and that's if they are the safe one. (~823 when is greater)

`49b` [x] - Rarity picks the grade band E→S; a scroll refuses an item outside its band. -> 

`49c` [x] - `/enchant <value>` with the item picker still works, unrestricted (an F weapon to +999999).
⚠ **This is now load-bearing** — with the God layer deleted it is one of only two ways to get cosmic
stats for testing. -> 

`49d` [x] - Enchant scrolls drop from the right sources per type, and the drop rate reads sane. -> 

---

## 50. 0.49.0 — crit damage, BLOWS and `[Double]`

Full detail: §50. ⚠ Its 0.65× figure is **debunked** — see §52, which supersedes it.

`50a` [?] - A **blow** lands from behind/stealth and reads as a blow, not a crit. -> 

`50b` [x] - `[Double]` shows in the combat text when a skill doubles. -> 

`50c` [x] - 🔴 **`Can Crit` and `Can Double` must be EXCLUSIVE** (your `M8`). A `[Double]` Strike was
**critting** — 80 → 162 on a skill described as double-only. Confirm one skill can no longer do both. -> a piercing blow can crit and double so it does.

---

## 52. 0.50.0 — crit RATE on your L2 model

Full detail: §52. This closed the crit thread; §50h was a **measuring error** on my side, not a defect.

`52a` [x] - Crit rate follows the L2 model (base × DEX mod × passives × buffs, clamped once at the end). -> 

`52b` [~] - `Can Crit` / `Can Double` render per skill in the skill window. -> not all skills. Piercing stab the description is outdated

`52c` [x] - Per-skill crit modifiers apply — a skill authored to crit more actually does. -> 

`52d` [x] - ⚠ The one you flagged: **a sword at 8% crit was out-critting knives at 12%.** Confirm that
is gone. -> 

---

## 53. 0.52.0 — the playtest-19 DEFECTS + the FRICTION tier

Full detail: §53. ⚠ **delete `game.db`**.

`53a` [!] - 🔴 **`48g` the Blessing Box no longer eats itself on a partial pick.** Tick **7 of 10** and
confirm: either it refuses until you pick exactly 10, or it gives 7 and keeps the box. Last time the
box vanished and the 3 unused picks were lost. -> 
  Now it forbids me to select 1 and aquire it.\
  Make it so to be able to (or)
  - use x amount of a single item (5 of item1, 3 of item2, 2 of item3)
  - take 1, then open it 9 more times and take the same item  (open and take item1 -> 9 times)

`53b` [x] - 🔴 **`46d` `/ptinv` can invite an out-of-sight player.** "no player x nearby" was the bug;
the earlier fix corrected the target frame, not the invite lookup. -> 

`53c` [x] - 🟠 **`46m` compare on a PENDANT opens a PENDANT**, not a stud. -> 

`53d` [x] - 🟡 `46o` both warehouse caps raised to max, with the note to lower them when expansion
lands. -> 

`53e` [!] - The friction tier as a whole — does the game feel less fiddly, or did I just move the
friction somewhere else? -> well there is a bit of rubber banding when stopping. I click move and when it arrive at the destination it stops with inertia and comes back .. A small but it's there

---

## 54. 0.53.0 + 0.53.1 — the DELETIONS, `M7`, `M1`, `/spd`, two clocks

Full detail: §54. ⚠ **delete `game.db`**.

`54a` [x] - 🔴 **`M1` — nothing is unhittable any more. Do your own test first**: admin, accuracy 9999,
a bow, **level 20 vs a level 40/80 dummy**. You must now land **~5%** where it was *zero, forever*.
With **Precision** L1 the floor is **10%**, L2 (40+) **20%**. The other way: a level-20 rogue in a
level-90 field must **dodge ~10%**. ⚠ Sanity-check the ordinary case did NOT move — same-level is
still ~5%/95%, a 10-15 level gap still hurts. Exp and drops still pay **zero from a 13-level gap**;
killing far above you stays pointless, just no longer impossible. -> 

`54b` [x] - **`M7` Heavy Draw is gone from the rogue at every level** — not at 24, and not as
Piercing/Snare/Rending Shot on a 40 ranged discipline. -> 

`54c` [x] - **Evasion Mastery follows the CLASS CHANGE, not the level.** Lv1 at 20 for every rogue;
Lv2 at 40 **only on taking a MELEE discipline**; a ranged discipline stays Lv1 forever; **Lv3 goes to
nobody** (its milestone is the 4th class change, which doesn't exist). ⚠ Check a level-40+ rogue with
**no discipline chosen yet stays at Lv1** — that is the actual change. -> 

`54d` [x] - **The deletions broke nothing by their absence**: no Reflexes, Bow Mastery, archer Armor
Mastery, Dispel Magic, HP Boost, Greater Heal or "Class Balance" rows — but `evade_mastery`,
`precision` and `anti_magic` all **stay** and still grant at 20/40/76. -> 

`54e` [~] - **The God layer is gone and the debug rig still works.** Creation offers Human/Elf/Ork
only; no God's Judgment / God's Robes in Boxes. ⚠ `/enchant` and `/spd` are now the **only** route to
cosmic stats — test them like they matter, because they do. -> I think we need to do the same commands and for other stats ...one statMod that is Admins only that overrides all stats - so I can do a acc 999999 or Eva 99999 or crit dmg or rate etc...

`54f` [x] - The Treasure Chest still opens and pays its staples (its jackpot is now the S-grade Mythic
1H blade). -> 

`54g` [x] - **`/spd` replaces the four `/speed-*` commands.** `/spd m 250`, `/spd a 1200`, `/spd c
1500`; **bare `/spd` resets all three**; a bad form prints usage. ⚠ The old `/speed-*` must fall
through to unknown-command. -> 

`54h` [x] - **Two clocks in the title bar** — `game 14:32 · 09:47:12`. Watch for a minute: game time
must advance ~6 minutes, and **survive a relog** without jumping. -> 

---

## 56. 0.51.0 — magic crit becomes its OWN channel

Full detail: §56. **Never indexed in a checklist before today.**

`56a` [x] - Magic crit rate is no longer decorative — a human mage was stuck at **2.0%** and the 20%
cap needed WIT 200. Cast with and without **Insight**: the buff must now roughly **double** observed
crit frequency (it used to be clamped away mid-chain and bought +3 points). -> 17% on human mage without the second double crit rate buff so it's OK.

`56b` [x] - An **elf mage in the full kit** hits 10%, and ×2 Insight puts him at **20% — the cap
exactly**. -> 

`56c` [~] - A **mob** still crits occasionally (~1.25%), not never. -> make a magic training dummy 80 lvl with magic (50 range) that does 1 mdmg each 0.1s so for 10 s to hit me 100 times and see it i got atleast 1 crit dmg (can do the same for with a phys skill dummy)

`56d` [x] - **Ferocity and the crit-damage weapon attribute no longer pay mages.** Both are authored
for fighters and used to leak through a shared field. Put a crit-damage attribute on a staff: the
magic crit multiplier must **not** move — it is a flat ×3. -> 

`56e` [?] - **Resonance** reads as a percentage (×1.2), not a flat number. -> What is this Resonance?

---

## 57. The MAGE MASTERY RESTRUCTURE — masteries now STACK

Full detail: §57. ⚠ **delete `game.db`**. 🔴 **SmokeTest not run — see the pre-flight above.**

`57a` [x] - **Armor masteries stack.** Which one applied used to be decided by **dictionary order**.
A nuker's robe MP-regen ×1.2 now multiplies Spellcaster Mastery's ×1.2 — visibly better than either
alone. -> 

`57b` [!] - **Robe Armor Mastery is bought with SP at 7 and 14** (2 rungs, no longer auto-granted,
no longer 3 rungs), and **"Weapon Proficiency" appears on nobody**. ⚠ The migration clamp is the risk:
a mage with **no robe P.Def at all** is the failure mode. Deleting the db avoids it — do that. -> the L1 is shown inside lvl-1 and lvl-7 learning groups. Learning one makes the other disappear and a the one at lvl-14 shows

`57c` [x] - **The wrong-weapon penalty is a penalty, not an execution.** It was ×0.05 — annihilation.
Now ×0.5. Hold in turn: wand/staff (×1), sword/blunt (cast ×1, M.Atk ×0.6), bow/dagger/bare (×0.5
across the board). The order must degrade as listed. -> 

`57d` [!] - ⚠ **A bow caster cannot BUFF his way out of the magic-accuracy penalty** — it is applied
after buffs, on purpose. Buff up holding a bow and confirm it survives. -> I dont see my magic 

`57e` [x] - **A cleric in light armor composes back to cast ×1.00 / attack ×1.00**, vs ×1.05 in a robe
— your "−5% from a robe". A **nuker** in light stays punished. -> 

`57f` [x] - **The dual's evasion roll is FLAT +5**, never a percentage. Mob-miss against the rogue
should read **21-23%** at max roll — it was **33-42%**. You said 16% bare is fine, so `evade_mastery`
was deliberately left alone. -> 

---

## 55. 0.53.2 — Restore Spirit gets LEVELS, the bow's accuracy roll goes FLAT 5

Full detail: §55. No db reset needed for this section alone.

`55a` [x] - **Restore Spirit is a ten-rung ladder** (25/40/45/50/55/60/65/70/75/80), ending at
**120 MP for 200 HP**. It had ONE rung for life while the bolt ladder grew 30 → 116. Rung 1 (20 MP /
65 HP @25) is the **CSV** and must not have moved. -> 

`55b` [!] - ⚠ **The skill card shows the HP price**, not just the MP gain. A skill that silently eats
200 HP reads as a bug the first time it kills you. -> it's not showing in the description what it takes to gain what ..-200hp +120mp ..is never written

`55c` [!] - Casting it at **low HP** refuses, or at least does not kill you. -> it should not allow you to use the skill wham hp is less than required health. It goes for every skill that take hp as well ... It should act as mp ..I cannot cast a skill whne my mp is low ..so I cannot cast skill when hp is low .. 

`55d` [x] - **Mage Armor Mastery rungs 5-8 @40/50/60/70** carry mpWhenRestored **50/60/70/80**. ⚠ P.Def
and max MP are **frozen** at the rung-4 values deliberately — if those climb, that's a defect. -> 

`55e` [x] - **The sum at 80, in a robe: 200 MP for 200 HP** — your "+200 MP for −200 HP" endpoint. Out
of a robe the same cast delivers only 120. -> 

`55f` [ ] - 🔴 **The actual question: farm a mage 10+ unbroken minutes at 40+.** Your playtest-19
finding was *"mages run out of MP in 2-3 minutes"*. Does the rotation sustain now? It is not meant to
be free — the design is "farm 30-40 min, rest a bit". **If it is still 2-3 minutes, the ladder is not
the fix and I need to know.** -> 

`55g` [x] - **The bow's accuracy roll reads Accuracy +1..+5 FLAT**, never a percentage, at every grade.
⚠ **An old bow keeps its `AccuracyPercent` roll and must still work** — the enum entry was kept and
made unrollable, so no db reset was needed. -> 

---

## 58. 0.54.0 — the tutorial chain (M5) + the newbie kit as a 30-day loaner (M6)

No section in `TestChecklist.Unity.md` — the detail is here. **No db reset needed.** ⚠ Test this on a
**brand-new character**: the chain starts at level 1 and it **replaced** the old starter quests.

`58a` [~] - **A fresh character is offered `Welcome, Traveller` by Huntmaster Cera at level 1**, and
the five parts chain in order: Welcome (1) → Blessings and Bottles (3) → Properly Armed (6) → Blooded
(10) → A Trade to Learn (15). Each one only offers after the one before it is handed in. -> 
   - We fixed the Nyra part where she didn't accepted my talking
   - Works (after the fix) but we need to tell the fresh traveler before he fights the pigs -> 
      - how to open his bag 
      - how to open boxes 
      - which armor/weapon to select... 
      - how to equip/use skills/spells/attacks.. (if I'm new I will stand near the pigs naked and bear hands not knowing what to do) ... 
      - After the Miren (aphotecatry) how to use the rune and what it does
      - Also there should be how to use auto potions and auto farm -> "reach lvl 18"  part looks OK for this (after the Dorian-jewels)

`58b` [x] - **The old starter quests are GONE** — no `starter_kit` / `starter_blooded` anywhere, and
you are never paid two newbie kits. -> 

`58c` [x] - **The chain does not GATE the three class quests** (Marius / Oren / Vael). Part 5 points at
them; you can still level to 20 and do them having ignored the chain entirely. -> 

`58d` [~] - 🔴 **The kit is a 30-day LOANER**: every piece reads **"Newbie …"**, is **untradable**,
cannot be sold, and carries a clock. ⚠ The real ladder gear it was cloned from (`sword1h_t1` etc.)
must be **untouched** — a Ferrite Mythic you drop or craft has no clock and sells normally. -> it's like that,but can we make it so every item have timer,is Tradable, is Sellable (-1 sell price). Meaning the item is not a clone (for Newibie equip is OK to be like that) but let say I want to give some1 a Soulcrystal item and that item to be timed,unsellable,untradable - not to make server side a new item, just take a real Soulcrystal item add sell price -1, add time x[s|m|h|d] (1m == 1min),flags it untradable => and the item reads as "Soulcrystal (temporary, bound)" =>
   - sellable + tradable == no tag/flag (normal)
   - unsellable + untradable == bound
   - sellable + untradable == private (or something smarter/better)
   - timed + normal/bound/private == temporary, (blank)/bound/private 
   - it's real item just with tags. I later want to be able with command: 
      - /give <name> sword1h_t10 -1 0 1d "Admin Sword" 5 -> and I get a "Admin Sword +5 (temporary bound)" a blade +5 enchanted for 1 day unsellable and untradable 
      - (/give <name> <itemId> <sell price: -1 unsellable/0 - default/1[k/m/b...we have it]> <tradable: true/false or 1/0> <timed: 0 normal, 1d == 1 day..> <newItemName: "some new name limit to 20 symbols in quotes spaces to work" and "" empty quotes for default name> <enchant value>)
      - that way I can reach mytic no need for craft atm and can give anyone anything

`58e` [x] - **The completion consumables are bound too** (Ultimate Scroll of Return / Resurrection,
Dash and Instant Healing potions) but carry **no clock**. -> 

`58f` [x] - **The loaner is a SET**: wearing the bound body + the accessories still completes the
armour set and pays its bonus. -> 

`58g` [~] - ⚠ **A WORN loaner that expires is removed and your stats drop with it.** Easiest check:
`/spd`-style debug is no help here, so trust `58h` instead unless you want to wait 30 days. -> if I have 58d then I'll test it

`58h` [x] - **The pacing still holds**: part 4 ends ≈ level 10 and part 5 ≈ level 15 without grinding
between them. If it strands you, say where. -> 

`58i` [~] - 🔴 **He never named the game.** The parts are called "Welcome, Traveller" etc. because
"Welcome To The `<Game>` World" needed a world name. **Give me one and I will use his literal
title.** -> thats ok but on that note.. We need to rename everitying that says l2(as the game not the level),l2 clone project etc every comment to refer from l2 to the (inspiration game) or `IG` or other tag

---

## 59. 0.55.0 — the QoL five (C1 · C3 · C14 · C16 · C17) + written titles + NPC roles

No section in `TestChecklist.Unity.md` — the detail is here. 🔴 **DELETE `game.db`** — three new
columns (`CustomTitle`, `CustomTitleColor`, `MayWriteTitle`).

`59a` [x] - **C1 — chat resets per character.** Talk in Local/World, leave to character select, enter
on a *different* character: the chat tabs are **empty**. Delete a character and make a new one — the
new one must not inherit its chat. ⚠ The **System tab is deliberately KEPT** (it is the crash trail);
if you want that wiped too, say so. -> 

`59b` [x] - **C1 — the buffer holds ~1000 lines**, not 200. Spam a fight and scroll back further than
you could before. Watch for lag — the window still only *draws* 120 rows, so it should feel the same.
-> 

`59c` [~] - **C3 — a timed item says how long it has left**, in item details, colour-graded: **green**
over 7d, **white** over 1d, **yellow** over 1h, **red** under. Check it on a **newbie kit piece**
(≈30d, green) and on a **1-day rune** (white/yellow). -> will test it with 58d

`59d` [x] - **C14 — a two-handed weapon greys the off-hand square.** Equip a 2H sword/staff/bow: the
Shld square shows the *weapon's* abbreviation, dimmed, and **does not open anything** when tapped.
Equip a 1H + shield and the square goes back to normal. -> 

`59e` [x] - **C16 — no more "the".** Titles read `Wealthy`, `Devoted`, `Warlord`, `Feared`,
`Ascended`, `Beloved`. -> 

`59f` [x] - **C16 — each title has its own colour**, over the head *and* on the Rank board *and* in the
picker: gold=golden, time-played=green, PvP=purple, PK=dark red, level=sky, charisma=rose. ⚠ The PvP
title purple is **deeper than a flagged player's purple name** on purpose — tell me if they still read
as the same colour on the phone. -> 

`59g` [x] - **C16 — the title's face differs from the name**: italic small caps with a little tracking.
The client has ONE font asset, so this is TMP's synthesised styling rather than a second typeface —
**if it still reads as "just the name again", say so and I will bake a real font.** -> 

`59h` [x] - **C17 — staff titles.** On an admin account the Rank window's Titles tab offers **«Game
Master» — staff**; a moderator gets **«Moderator» — staff**. They are **opt-in** like every other
title (nothing is worn until you pick it) — tell me if you would rather staff wore theirs
automatically. -> 

`59i` [x] - **C17 — `/role` takes effect live.** Promote a logged-in character to moderator: the title
appears in their picker without a relog. Demote them while they wear it: it comes straight off. -> 

### The titles you asked for on 2026-08-07 (after the queue was built)

`59k` [x] - **NPCs wear their role.** `Elder Marius` plates as **`Elder`** over **`Marius`**. Check the
multi-word ones too — **High Priest Oren**, **Spirit Helper Nyra**, **Class Master Vael**,
**Grandmaster Thorne** — they split at the LAST space, so only the personal name should be on the name
line. ⚠ **A MOB must NOT split**: "Ridgeback Pup" stays one name. -> 
`59l` [x] - **The full name survives everywhere it should**: quest text, the dialog header and the
target frame still read the whole "Elder Marius". -> 

`59m` [x] - 🔴 **`/target Pell` works in a crowd** — the thing you actually asked for. Also try
`/target Gatekeeper` (the role half) and `/target Pel` (a prefix). It only finds what is IN SIGHT. -> 

`59n` [x] - **`/title` is refused below 76**: a low character gets "you have not been granted the right
to name yourself". -> 

`59o` [x] - 🔑 **The right arrives at level 76, with Angel's Protection** — your ask. Level a character
to 76 (or log in one already past it): **one** system line offers `/title`, and the Rank window grows
the hint. It must NOT repeat on every login. ⚠ Both grants now come from one place, so **the future
quest replaces one condition, not two**. -> 

`59p` [x] - **Then it works**: `/title Bonecrusher` sets AND wears it in one step, `/titlecolor violet`
recolours it, `/title` with no text clears it. `/titleright <name> on|off` is the manual override
(⚠ **online characters only**). -> 

`59q` [x] - 🔴 **The reserved words hold**: `/title Warlord`, `/title wealthy`, `/title Game Master`
are all refused. This is the rule that keeps a board title worth earning — if any of them gets
through, that is a bug, not a nitpick. -> 

`59r` [~] - **20 characters max**, and letters/digits/space/`'`/`-` only. Try `/title <color=#FF0000>x`
— it must be refused, or a title could recolour itself past the palette. -> 
  it works, but i want the title color to be default white for /title. And the /titlecolor to be a item like a rune that give you the right to use the /titlecolor + clicking on the title color rune item to open the colors as a list

`59s` [x] - **It survives a relog**, and the picker offers it back as **«your title» — your own** after
you switch to a board title and want it again. -> 

`59t` [x] - **Revoking works**: `/titleright <name> off` takes a worn written title straight off the
head. ⚠ **On a 76+ character it comes BACK on the next login** — the level gate re-grants it. Say if
you want a revoke to stick; it costs one more column. -> 

`59u` [x] - ~~⚠ Protocol is 13, install the 0.55.0 APK *and* server.~~ **Superseded — the pass ships as
0.57.0 at protocol 14; see BEFORE YOU START at the top. Nothing to check here.** -> 

---

## 60. 0.56.0 — D5, the Combat feed in its own window

No section in `TestChecklist.Unity.md` — the detail is here. **No schema change; `game.db` is fine.**
⚠ **Protocol is 14** — server and APK together. (An older APK has no case for the new channel and
prints loot/exp as plain Local chat: noisy, never lost.)

`60a` [x] - **The System tab is quiet now.** Kill something with the Chat window open on *System*: no
damage lines, no `You looted:`, no `Exp: +…`. Only real system lines (refusals, learn notices) land
there. -> 

`60b` [x] - **The 6th button opens a WINDOW, not a tab.** Chat → **Combat**: a second window appears
(bottom-right) and the Chat window stays open and usable beside it. The button stays lit while it is
open, and goes dark when you close either one. -> 

`60c` [x] - **Colours.** Your own damage is **green**, the mob's damage to you is **red**, loot is
gold, the `Exp/SP/Gold` line is blue. ⚠ The green is deliberately *deeper than lime* (your words) —
say if it now reads too close to the System tab's green. -> 

`60d` [x] - **All stays readable.** Fight for a minute with the Chat window on **All**: combat is
**not** in it. That is on purpose — All would otherwise be the exact wall of damage the window was
built to get away from. **Tell me if you would rather All showed everything.** -> 

`60e` [x] - **Two Clears, two scopes.** Combat's **Clear** empties only the combat window; the Chat
window's **Clear** still empties everything (including combat). Say if you want Chat's Clear to
spare the combat feed too. -> 

`60f` [x] - **Party loot still names the taker.** In a party, `X looted Y.` lands in the *combat*
window of the members who did not get it. -> 

`60g` [x] - **No lag spike.** Spam a fight with **both** windows open — the rewrite that made the
console append-only now serves two views, so this is the one that would show a regression: rows
drawing over each other, a freeze, or the phone heating. -> 

`60h` [x] - **It resets per character.** Leave to character select and enter on another: the combat
window is empty (it follows the C1 chat reset, not the System tab). -> 

---

## 61. 0.57.0 — the last three of the queue: `B8` the S grade, `B9` the jail wall, `B10` client collision

**No schema change; `game.db` is fine. Protocol stays 14** — nothing on the wire moved. Server and
APK should still go together, because the client now reads the world's bounds out of `Game.Shared`.

**`B8` — the S grade exists in words now.**
`61a` [x] - Debug → Equip → **Level 80**: the menu says **(S-Grade)**, not (A-Grade). Open any piece's
details: **`Grade: S`**. Before, a Soulcrystal item called itself A while the only scroll that fits it
says "S grade only" — the two finally agree. -> 

`61b` [?] - Same item in the **vendor** list: "Mythic **S**-grade …". -> which vendor .. we have no vendor taht sells more than D (yet)

`61c` [X] - ⚠ **The one behaviour change, tell me if you hate it.** S gear now sits on the grade
ladder like every other tier, so a character **below 80** wearing level-80 gear takes the normal
one-step **×0.5** grade penalty (it was ×1.00 — S was the only tier with no gate, while its own
details already said *Requires level 80*). At 80+ nothing changes. -> ofc it needs penalty as max lvl penalty even more if youd like .. to balance the dmg of a lvl1 with F grade and S grade

**`B9` — the jail has a wall you can see.**

`61d` [!] - `/jail <name>` then `/tp <name>`: you arrive **in the jail**, and an **orange dashed
circle** is drawn around the cell. Walk at it — you stop on the line instead of being snapped back. -> 
  - The jail cell is 1px x 1px ... make the jail like an dungeon .. with size 300x500 or something .. the jailed person to move inside ...
  - make a jail .. not a cell per player ..

`61e` [x] - The ring is **not** on the map-overlay toggle; it appears because you are standing in the
cell and disappears when you leave. -> 

`61f` [x] - Same for the inmate: a jailed character sees the same ring and can pace the cell. -> 

**`B10` — the client has collision now.** (The server clamp is untouched — it is the anti-cheat
backstop, and this is the half that was missing.)

`61g` [x] - **Walk into the world edge.** Tap past the border in the overworld: you walk to the edge
and **stop there**. No rubber-band, no snap-back — the destination ring lands on the edge, which is
the tell that the client refused to ask for the impossible. -> 

`61h` [!] - **Same at a dungeon wall.** In the Hollow Crypt, tap outside the dungeon: you stop at the
boundary. -> it dont have walls and i can go out of the creep (get rubber in but still no collision)

`61i` [x] - **Crossing worlds on foot is refused out loud.** If you can get a tap to land in a
different world than the one you are in, nothing moves and the log says *"You can't walk to … — only
a teleport goes there."* (Hard to reach on purpose; if you never see it, that is fine — say so.) -> 

`61j` [~] - **Nothing normal changed.** An ordinary hunting session: no stutter, no move that gets
eaten, no tap that lands short. This is the risk of the change — the client is now allowed to edit
your destination, so a wrong bound would show up as taps that quietly go somewhere else. -> only the inertia stop that i explained in `53e`

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

These have survived several checklists untouched because none of them happens by accident. If you
want them closed, they need a session aimed at them.

`37d` [ ] - A trade **shortfall aborts the whole trade** with nothing moved. -> 

`37e` [ ] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items are
refused. -> 

`36e` [ ] - A **re-pulled wounded boss continues its phase script** instead of replaying it from the
top. -> 

`32z` [ ] - **Auto-farm skill chains**: cyclic order, heal/buff/attack priority, thresholds, debuff
ranks, assist-leader — and all of it **survives a relog**. -> 

`25b` [ ] - **No combat-logging out of a DoT** — char select and `/exit` both refuse while it ticks. -> 

`13a` [ ] - The **~3h "take a break" banner** (needs 3 hours of continuous play to see). -> 

---

## KNOWN OPEN — not defects, don't spend the pass on them

Tracked, ruled on, or deliberately queued. Listed so you don't re-report them.

- ~~**`B8` `B9` `B10`**~~ ✅ **ALL BUILT 0.57.0** — test at §61. **The build queue you set on
  2026-08-07 is now EMPTY.** Nothing is queued; the next move is yours — and the highest-value one is
  still an APK and a play pass over nine unplayed builds, not more code.
- ~~**`D5` the [Combat] chat tab in its own window.**~~ ✅ **BUILT 0.56.0** — test at §60.
- ~~**`M5`/`M6` the tutorial chain + bound 30-day newbie gear.**~~ ✅ **BUILT 0.54.0** — test at §58.
- ~~**`C1` `C3` `C14` `C15` `C16` `C17`** — the QoL six you picked.~~ ✅ **ALL BUILT** — `C15` rode
  along in 0.54.0, the other five are 0.55.0. Test at §59.
- **`C4`** auto-on for buff potions/scrolls — **your ruling: comes later, with the AutoPot tabs.**
- **`G2` / `0e` `lb_*` + `wc_*`** — **CLOSED by your ruling: leave them.** Placeholders for 40+,
  commented out, harmless. I will stop asking.
- **`D4` `G5` `F1` `V1` `G4`** — **done and tested at 0.49.0.** Older docs still list some as open;
  they are stale on this point.
- **Crafting (`D3`)** — designed, unbuilt, and still the top content blocker. **3rd/4th class kits**
  — blocked on your 40+ CSVs. **`G3` mobs-as-players** — needs the document + BalanceMatrix tables
  first, then 2-5 real mobs as an experiment, per your ruling. **Instances** — you are holding.
- **The champion's −10% P.Def** (was −20%) — owed by **you**, on a re-test.
