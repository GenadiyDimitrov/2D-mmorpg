# PLAYTEST ARCHIVE — every closed playtest, verbatim

**One file, newest first.** This replaces the eight separate `Playtest-*.md` files and the old
`Playtest-Archive.md#legacy-testchecklist`, which are gone. Nothing was summarised or edited away: each pass below is the
original file's content, unchanged, under its own marker. The reason for the merge is that these are
**closed** — every defect in them is fixed and every decision is ruled on — so they are read for
*rationale*, not worked from, and rationale is easier to search in one place.

**What you actually work from:**
- [Open-Checklist.md](Open-Checklist.md) — the single rolling list of what is still untested.
- [TestChecklist.Unity.md](TestChecklist.Unity.md) — the live per-section detail, kept current.

| pass | date | what it was |
|---|---|---|
| [Playtest-20](#playtest-20) | 2026-08-10 | the ten-build pass (0.49.0→0.57.0). 12 free-form finds; the dagger-evasion and weapon-speed roots |
| [Playtest-19](#playtest-19) | 2026-08-06 | the 0.48.0 pass. §46/47/48 green; four defects + M1-M14 |
| [Playtest-18](#playtest-18) | 2026-08-04 | the second 0.45.0 pass — quests Q1-Q5, the friction tier |
| [Playtest-17](#playtest-17) | 2026-08-03 | the 0.45.0 pass — crafting named the top content blocker |
| [Playtest-16](#playtest-16) | | ⚠ his numbering here is the **VERSION** (37c = 0.37.0) |
| [Playtest-15](#playtest-15) | | the economy verdict |
| [Playtest-14](#playtest-14) | | |
| [Playtest-13](#playtest-13) | | |
| [Playtest-0.28.76](#playtest-02876) | | the early Unity pass |
| [Legacy TestChecklist](#legacy-testchecklist) | 2026-07-13 → 07-21 | playtests 4-7, the pre-Unity-only era. Almost entirely ✅ verified; kept for the design calls inside it |

Answered checklists that fed these passes (`Open-Checklist-0.45.0/-0.47.0/-0.48.0.md`) were
transcribed into the playtest files at the time and are in git history.



---

<a id="playtest-20"></a>

# ══ Playtest-20 ══

# Playtest-20 — the ten-build pass, 0.49.0 → 0.57.0 (owner, 2026-08-10)

The longest unplayed run the project had: ten builds met a player at once. He answered most of
§49-§61 and added **12 free-form finds**, two of which (dagger evasion, the weapon speed table) were
root-cause bugs rather than tuning. Everything below is his file verbatim; his text is after each
`->`. Fixes shipped as **0.58.0 / 0.58.1 / 0.58.2 / 0.58.3**.

## He ruled the checklist row FORMAT (asked in-pass)

> I lean to .2 because its same just with a single dash infront - not so much change (we have
> [],[!], [?], [x] so the md viewer cannot destinguish them as checkbox unless its [ ] or [x] so no
> point in changi it to .1)

So the row shape is `- \`99zz\` [] - @Description -> @my_Comment`, with a blank line after each.

## His 12 free-form finds (verbatim)

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

## Section answers (his comments verbatim, after the `->`)

`0a` [ ] - The nuker beats the champion by ~19% where they were at parity (0.98× → 0.84×). Is the
nuker's lead earned, or do I trim it? -> This need to be tested. When I leave the chars to play alone all measure.

`0` (accuracy vs the evade FLOOR) -> then the difference become free +15 => clam(5% + (35-22=13),10floor) => miss = 18% -> but archer inves in +5 acc miss becomes 13% and also add a 5 dex here you go floor again

`0` (may accuracy pierce the floor?) -> Yes the floors and ceiling cannot be touched (they are there for a reason)

`0` (crit damage base) -> When you land a critical hit or use specialized dagger Blow skills, critical power multipliers inflate the numerator:
\(\text{Critical\ Damage}=\frac{77\times \text{P.\ Atk}\times 2\times \text{Crit\ Power\ Multipliers}}{\text{P.\ Def}}\) -> so you were right the base as i cen decifer the formula its 2 times then the mutlipliers(buffs passives)

`0` (flat vs % weapon crit rate) -> In l2 the weapons do a flat increase in crit (+64, +90, + 109 @SGrade) a % increase depend on the base crit value - so if we do a dagger with 30% and a sword with the same sword wont benifit the same, but if we do a flat 90 its as 9% increase after all the buffs/passives and is 9% across all. But yet the only one to do flat crit rate is a bit off ... everithing is % then crit to be flat .. why ? .. we need to alter the sword to 90% crit rate ... so unbuffed sword wielder to have 88x1.9 -> 167 and a dagger 132x1.3 = 171 -> then the max a tank investing in critical sword will not have 25% hp or 15% as but 43.5% crit rate (thats pure playstile choice)

`0` (the rogue "nerf") -> why u count it as a nerf .. the 430 with jsut a weapon attri was just way to OP .. the 400 we must get only after getting fully buffed

`0` (the 52-set boots P.Def typo) -> My bad. When i did the csv i added to the 40 sets a boots pdef as well -> 179 (fixed the csv)

`49a` [~] - Three scroll types behave differently: one breaks the item, one drops it −1, one is safe. -> the enchant scrolls work. Need to change the enchant rates.
   > +1~3 - safe (3 enchants, to enchant avg-3 scrolls)\
   > +4~9 - 66% (6 enchants, avg-9 scrolls)\
   > +10~15 - 33% (6 ench, avg-18 scrolls\
   > +16 - 5% (1 ench, avg-20 scrolls\
   > so a weapon +0~16 need ~51 scrolls, and that's if they are the safe one. (~823 when is greater)

`50c` [x] - `Can Crit` and `Can Double` must be EXCLUSIVE (your `M8`). -> a piercing blow can crit and double so it does.

`52b` [~] - `Can Crit` / `Can Double` render per skill in the skill window. -> not all skills. Piercing stab the description is outdated

`53a` [!] - The Blessing Box no longer eats itself on a partial pick. -> Now it forbids me to select 1 and aquire it.\
  Make it so to be able to (or)
  - use x amount of a single item (5 of item1, 3 of item2, 2 of item3)
  - take 1, then open it 9 more times and take the same item  (open and take item1 -> 9 times)

`53e` [!] - The friction tier as a whole — does the game feel less fiddly? -> well there is a bit of rubber banding when stopping. I click move and when it arrive at the destination it stops with inertia and comes back .. A small but it's there

`54e` [~] - The God layer is gone and the debug rig still works. -> I think we need to do the same commands and for other stats ...one statMod that is Admins only that overrides all stats - so I can do a acc 999999 or Eva 99999 or crit dmg or rate etc...

`56a` [x] - Magic crit rate is no longer decorative. -> 17% on human mage without the second double crit rate buff so it's OK.

`56c` [~] - A mob still crits occasionally (~1.25%), not never. -> make a magic training dummy 80 lvl with magic (50 range) that does 1 mdmg each 0.1s so for 10 s to hit me 100 times and see it i got atleast 1 crit dmg (can do the same for with a phys skill dummy)

`56e` [?] - Resonance reads as a percentage (×1.2), not a flat number. -> What is this Resonance?

`57b` [!] - Robe Armor Mastery is bought with SP at 7 and 14. -> the L1 is shown inside lvl-1 and lvl-7 learning groups. Learning one makes the other disappear and a the one at lvl-14 shows

`57d` [!] - A bow caster cannot BUFF his way out of the magic-accuracy penalty. -> I dont see my magic
  *(truncated in his file; clarified in chat 2026-08-10: "I don't see my magic FAILING with a bow more
  than with a wand", level 60 vs a level-60 dummy, several skills cast.)*

`55b` [!] - The skill card shows the HP price, not just the MP gain. -> it's not showing in the description what it takes to gain what ..-200hp +120mp ..is never written

`55c` [!] - Casting it at low HP refuses, or at least does not kill you. -> it should not allow you to use the skill wham hp is less than required health. It goes for every skill that take hp as well ... It should act as mp ..I cannot cast a skill whne my mp is low ..so I cannot cast skill when hp is low ..

`58a` [~] - A fresh character is offered `Welcome, Traveller` at level 1 and the five parts chain in order. ->
   - We fixed the Nyra part where she didn't accepted my talking
   - Works (after the fix) but we need to tell the fresh traveler before he fights the pigs ->
      - how to open his bag
      - how to open boxes
      - which armor/weapon to select...
      - how to equip/use skills/spells/attacks.. (if I'm new I will stand near the pigs naked and bear hands not knowing what to do) ...
      - After the Miren (aphotecatry) how to use the rune and what it does
      - Also there should be how to use auto potions and auto farm -> "reach lvl 18"  part looks OK for this (after the Dorian-jewels)

`58d` [~] - The kit is a 30-day LOANER. -> it's like that,but can we make it so every item have timer,is Tradable, is Sellable (-1 sell price). Meaning the item is not a clone (for Newibie equip is OK to be like that) but let say I want to give some1 a Soulcrystal item and that item to be timed,unsellable,untradable - not to make server side a new item, just take a real Soulcrystal item add sell price -1, add time x[s|m|h|d] (1m == 1min),flags it untradable => and the item reads as "Soulcrystal (temporary, bound)" =>
   - sellable + tradable == no tag/flag (normal)
   - unsellable + untradable == bound
   - sellable + untradable == private (or something smarter/better)
   - timed + normal/bound/private == temporary, (blank)/bound/private
   - it's real item just with tags. I later want to be able with command:
      - /give <name> sword1h_t10 -1 0 1d "Admin Sword" 5 -> and I get a "Admin Sword +5 (temporary bound)" a blade +5 enchanted for 1 day unsellable and untradable
      - (/give <name> <itemId> <sell price: -1 unsellable/0 - default/1[k/m/b...we have it]> <tradable: true/false or 1/0> <timed: 0 normal, 1d == 1 day..> <newItemName: "some new name limit to 20 symbols in quotes spaces to work" and "" empty quotes for default name> <enchant value>)
      - that way I can reach mytic no need for craft atm and can give anyone anything

`58g` [~] - A WORN loaner that expires is removed and your stats drop with it. -> if I have 58d then I'll test it

`58i` [~] - He never named the game. -> thats ok but on that note.. We need to rename everitying that says l2(as the game not the level),l2 clone project etc every comment to refer from l2 to the (inspiration game) or `IG` or other tag

`59c` [~] - A timed item says how long it has left, colour-graded. -> will test it with 58d

`59r` [~] - 20 characters max, letters/digits/space/`'`/`-` only. -> it works, but i want the title color to be default white for /title. And the /titlecolor to be a item like a rune that give you the right to use the /titlecolor + clicking on the title color rune item to open the colors as a list

`61b` [?] - Same item in the vendor list: "Mythic S-grade …". -> which vendor .. we have no vendor taht sells more than D (yet)

`61c` [X] - S gear now takes the normal one-step ×0.5 grade penalty below 80. -> ofc it needs penalty as max lvl penalty even more if youd like .. to balance the dmg of a lvl1 with F grade and S grade

`61d` [!] - `/jail` then `/tp`: you arrive in the jail with an orange dashed circle around the cell. ->
  - The jail cell is 1px x 1px ... make the jail like an dungeon .. with size 300x500 or something .. the jailed person to move inside ...
  - make a jail .. not a cell per player ..

`61h` [!] - Same at a dungeon wall — in the Hollow Crypt, tap outside the dungeon. -> it dont have walls and i can go out of the creep (get rubber in but still no collision)

`61j` [~] - Nothing normal changed. -> only the inertia stop that i explained in `53e`

## What PASSED (marked `x`, no comment)

§49 b/c/d · §50 a/b · §52 a/c/d · §53 b/c/d · §54 a-d, f-h · §56 b/d · §57 a/c/e/f · §55 a/d/e/g ·
§58 b/c/e/f/h · §59 a/b/d-q/s/t · §60 **all eight** (the combat window is clean) · §61 a/e/f/g/i.

## Never reached in this pass

`55f` (the 10-minute mage MP farm — the real question of §55), `0a` (nuker vs champion, deferred to an
auto-farm run), `37d`, `37e`, `36e`, `32z`, `25b`, `13a`.

---

<a id="playtest-19"></a>

# ══ Playtest-19 ══

# Playtest-19 — the 0.48.0 pass (owner, 2026-08-06)

**Source: his answered [Open-Checklist.md](Open-Checklist.md)**, transcribed here as the
AUTHORITATIVE queue, same convention as [Playtest-Archive.md#playtest-17](Playtest-Archive.md#playtest-17) / [Playtest-Archive.md#playtest-18](Playtest-Archive.md#playtest-18):
his wording verbatim in the quote block, my reading on the line above it. Ids are mine —
**M**y Finds (his own heading) — and the checklist ids stay as he answered them.

**The verdict in one line: five builds' worth of unplayed work went through in one pass and it held.**
0.46.0, 0.47.0 and 0.48.0 — the blocking defects, inventory hygiene, the quest section, the friction
tier, the text-box fix, x1 rates and the whole buff economy — are **played and green**, plus every
carried-forward item from §37/36/34/33/32 that was still blank. **Four defects only** (48g, 46d, 46m,
plus the 0.45.0 tick crash), and the rest of the file is *design*: rulings on the open decisions, a
combat-identity rework for the rogue, and a tutorial-quest spec.

---

## The six blocking decisions — five answered

**0a. `evade_mastery`, `precision`, `anti_magic` all STAY.** The G1 correction landed
([Playtest-Archive.md#skills-not-in-csvs](Playtest-Archive.md#skills-not-in-csvs) §3): they are auto-granted, not learned, which is why
they were missing from his CSVs.
> so `evade_mastery` I need - I give a change to it though in My Finds
> leave the precision and the anti magic

So the deletion is **only** the genuinely dead set: `reflexes`, `archer_armor_mastery`,
`archer_weapon_mastery`, `dispel_magic`, and the **Heavy Draw @24 grant** (see M7 — he wants it gone
above 40 too). `evade_mastery` stays but is **rewritten** by M9. ✅ **`class_balance_*` ruled 2026-08-07: "class_balance should be commented for now"** — the 8 defs and
their auto-grant come out of the live path but stay in the file. **Commented, NOT deleted.**

**0b. 🔴 The God layer goes — ALL of it.** Wider than I proposed.
> I want them deleted. Nothing that can't be acquired in game. If I need cosmic stats I can /enchant
> 9999999 and do /speed

`Race.God`, `ItemRarity.God`, `god_judgment`, `god_robes`, `hp_boost`, `greater_heal` and the God learn
table. **The rule underneath it is the interesting part: nothing exists in the game that cannot be
acquired in the game.** The debug rig is replaced by `/enchant <value>` (built in 0.49.0) plus `/speed`
— so those two commands are now load-bearing and must not regress. ⚠ Sweep the admin/debug menu for
anything that hands out God gear before deleting the ids, or the menu breaks.

**0c. Keep all SIX Dash rungs.** > keep them we will se when to drop

**0d. Sprint level 2 at 40 is right.** Nothing to change.

**0e. `lb_*` / `wc_*` — UNANSWERED.** Left blank. My recommendation stands (keep: one commented line
away from being learnable when the 40+ CSVs land). **Still owed back to him.**

**0f. G3 (mobs built like players) — document it, don't build it yet.**
> I want it documented and balance matrix tables. So I can make comparisons. And later we can do 2~5
> mobs so I can test

The order is: (1) a design doc, (2) `tools/BalanceMatrix` tables putting the mob curve and the
player-pipeline mob side by side per band, (3) **2-5 real mobs** built that way as a live experiment.
Not a wholesale migration off `MobBaseStats`. See [mob-as-player-design] in memory — the measurement
is already run and says type passives per band have to carry it.

---

## My Finds

**M1. ❓ Accuracy vs evasion — NOT A BUG, it is the ±20 lockout. Needs his ruling.**
> as admin i made my AS to 9999 - wih a bow I try to hit lvl 20/40/80 dummies
> - L20 vs L20-Dummy - I hit almost every time - the 5% evasion floor
> - L20 vs L40-Dummy - Didnt Hit once - where is the 5% evasion celing (the 5% hit floor)?
> - L20 vs L40/80-Dummy - With L1-`precision` passive the 10% hit floor - still miss - no hits
> - L40 vs L60/80-Dummy - With L2-`precision` passive the 20% hit floor - still miss - no hits

`StatCalculator.LevelGap` is piecewise and returns **1.0 at |Δ| ≥ 20 — a hard 100% lockout**, and
`ResolveAvoidChance` applies the level gap **after** the class floors precisely so that it overrides
them (documented in `docs/design/CombatResolution.md`: *"Precedence: Immunity > SureHit > level gap >
class floors > stat roll"*). Every case he lists is a gap of exactly 20 or more, so no accuracy number
and no `precision` rung can ever land a hit — only a `SureHit` skill can. That is the design working.

### 🔴 M1 — HIS RULING, 2026-08-06: the floors win, the lockout goes
> the 20 gap bears no drop not exp. No need for you to try at all at killing +20lvl mob. But having a
> floor/ceiling must be active at all times. Lvl 20 dagger in a 90 field must be missed 10% (cuz of
> floor). A lvl 20 warrior in a 90 field must hit 10% of the time cuz of floor … they will die anyways.
> and the exp/drop penalty gets them nowhere. So a floor and a ceiling means active all the time.

**He is right, and the code backs him harder than he put it:** `ExpCurve.LevelGapMultiplier` pays
**zero exp AND zero drops from a 13-level gap** (`GapZero = 13`), symmetric, deliberately stricter than
L2's. So the anti-powerlevel job is already done seven levels *before* the lockout even starts. The
lockout adds no protection and only produces the thing that read as broken to him — swinging forever
and never connecting.

**The change:** in `StatCalculator.ResolveAvoidChance`, swap steps 2 and 3 — apply the level gap
**first**, then clamp into `[max(0.05, defenderFloor), min(0.95, 1 − attackerHitFloor)]` **last**, so the
band and the class floors are active at every gap. `LevelGap()` itself is untouched; `G = 1.0` stops
meaning "lockout" and starts meaning "pinned to the edge of the band". Precedence becomes
`Immunity > SureHit > floors + the 5/95 band > level gap > stat roll`.
Delivered behaviour, exactly as he specified it:
- level-20 rogue in a level-90 field: dodges **10%** (his `evade_mastery` floor) instead of 0%.
- level-20 warrior with Precision L1: lands **10%** instead of never.
- no floor at all: still **5%** each way, the universal band.

⚠ **The consequence to accept: nothing is unhittable any more.** A level-1 character connects with a
raid boss 5% of the time. He has already priced that in — no exp, no drop, and he dies.

`docs/design/CombatResolution.md` is updated (the resolver block, the precedence line, and the
"floor erosion by level gap" paragraph, which is now deleted). Also worth doing with it: the client
should say *why* — "far above your level" — instead of a silent miss.

**M2. 🟡 Chat/social filtering commands + an Options window.**
> whispers towards yourself /block - whitouth a name blocks all.
> /block Name blockers the Name. - by block all I mean block all players messages in chat.
> /block-w block only whispers,/block-g global
> So a normal player or an admin will be able to limit their chat spam.
> /decline-t - declines trade,/decline-p - party
> those can be an options in the options window (that we don't have)

`/block` (all player chat) · `/block <name>` · `/block-w` (whispers only) · `/block-g` (global only) ·
`/decline-t` · `/decline-p`. This is **B11** on the not-built list, now specified. ⚠ The existing rule
stands: **an admin/moderator must not be blockable.** All six want to be toggles in an **Options
window**, which does not exist yet — that window is the real deliverable and `/decline-*` belongs in it
more than in chat.

**M3. 🔴 A server crash in the tick loop — still live in 0.48.0.**
> fail: Game.Server.Simulation.GameLoopService[0] Unhandled error in game tick
> System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
> at GameLoopService.Simulate() … GameLoopService.cs:line 5372

Line 5372 at `3dc092a` (0.45.0) is `foreach (var entity in _world.Entities.Values)` — the main entity
sweep in `Simulate()`. Something inside the loop body adds to or removes from `_world.Entities` on the
same tick (a death/despawn, a spawn, a teleport). **The same raw `foreach` is still there today**
(`GameLoopService.cs:5665`, and a second at `:895`). Fix: iterate a snapshot, or defer structural
changes to an after-loop drain. Rare, but it takes the whole tick down when it fires.

### Dead characters

**M4. 🔴 A dead character is not properly dead.**
> - Can move on the client side (gets rubberbanded back) - for others dont look like its moveing
> - Cannot be invited in party
> - Cannot be traded

Three separate calls, and only the first is unambiguously a bug:
- **Movement:** the client must refuse input while dead (the server already rubber-bands, which is the
  safety net, not the fix). Clear defect.
- **Party invite:** in L2 you *can* invite a dead player. My read: **should work** — being dead is
  exactly when you want to be pulled into a party for a res.
- **Trade:** should stay refused — but with a stated reason, not a silent nothing.

**M5. 🟡 The tutorial quest chain — "Welcome To The `<Game>` World".** A 15-step chain whose *point* is
meeting every NPC in town; each step names the NPC and says what they are for. Full text is in his
checklist under 0.49.0 and is reproduced verbatim in memory — the shape:

| step | who / what |
|---|---|
| 1 | Gatekeeper **Pell** — free teleports until 40 |
| 2 | kill 5 pigs → level 3 |
| 3 | Huntmaster **Cera** — repeatable hunt contracts (take one) |
| 4 | kill 5 foxes → level 6 |
| 5 | Spirit Helper **Nyra** — support magic 6-75 (take the buff) |
| 6 | Apothecary **Miren** — potions/scrolls **+ the free daily Rune** (take it) |
| 7 | kill X goblin riders → level 10 |
| 8 | Armsmaster **Dolan** — **the Newbie equipment** as the reward |
| 9-10 | reach 15 → back to Dolan for the 1-day rune + jewel box |
| 11-15 | reach 18 → Elder **Marius** (1st class quest) → 19 → High Priest **Oren** (2nd) → 20 → Class Master **Vael** |

Completion: the profession, plus **x1 Ultimate Scroll of Escape, x1 Ultimate Scroll of Resurrection,
x5 Mythic Dash Potion, x5 Instant Health Potion — all untradable/unsellable.**
> The 3 class quests can be taken withouth the chain / the chain is only to meet the NPCs / U just can
> lvl up to 20 go do the 3 quests and done.. / The chain is for the newebie equipment an the end reward

⚠ So the chain must **wrap** the three existing class quests without gating them, and the daily rune and
Huntmaster contracts are ordinary standalone quests that the chain merely points at. Filed as C13's
replacement (that entry said "newbie quest band 10-35" and this supersedes it).

**M6. 🔴 The newbie equipment is bound and TIMED.**
> I want the newbie equipment to be unsellable and untradable and timelimited for 30d (can be
> destroied) - from the dolans quest

This is **C2** and it now has a source: the Dolan step of M5. Destroyable, but not sellable, not
tradable, 30 days. Pairs with **C3** (timed items show remaining time, colour-graded).

**M7. 🔴 Heavy Draw is STILL granted to a rogue at 24 — and he wants it gone above 40 too.**
> I contnue to get `Heavy Draw` on a rogue 24lvl - remove it - remove it from after 40lvl as well -
> rogue leave onyl the evasion mastery to the mele discpilines after 40 .. the archer sohuld not have
> evasion mastery after 40 .. the 10% are ok

Expected — G1 deleted **nothing** (the list was wrong), so the @24 grant survived. New this round is the
40+ half: **after 40, the melee rogue disciplines keep only Evasion Mastery; the archer discipline gets
none** (the archer keeps its base 10% floor and no more). ⚠ Never delete the `power_shot` *definition* —
three level-40 discipline skills are renames of it.

**M8. 🔴 `Can Crit` / `Can Double` must be exclusive — a skill does only what it says.**
> If a skill is not described as `Can Crit` or `Can Double` it doesnt do it.
> - Now a Stirke skill should only Double yet it crits from 80->162 dmg.
> - Stab does 580 but very very low chance in the begining
> - Yet the strike critted more than the stab landed (Sword-8% crit while knives 12%)

The 0.49.0 crit/blow/`[Double]` work is unplayed but he has clearly already looked at it. The rule he
wants is a **hard flag check**, not a probability: a skill with `[Double]` and no `Can Crit` must never
roll a crit, and vice versa. His second point is a balance observation that falls out of it — a
double-only Strike firing at the weapon's crit rate produces more big hits than the blow it is supposed
to be worse than.

**M9. 🔴 The rogue identity rework — the biggest design item in the file.**
> - the evasion mastery passive for the rogue class should be only the evasion floor.
> - The +20% crit and +10 evasion should be removed
> - move the crit rate (the 20%) from 32+ rogue armorm mastery to lvl 20+
> - its good to have the higher crit rate early on,
> - if we leave the evasion mastery critical chance the balance will shift at lvl 32 when each blow
>   lands with the 64+% chance ...
> - the critical rate is not additive each passive/buff should multiply % on top of it base for
>   dagger/bow
> - and evasion is to op we established that +10 == 10% .. so he have 14 from armor, 4 from buff ...
>   thats free 18% .. we dont need to give him more.. that is sure 18% easion for characters of same
>   level and same Dex - everithing else will make him untuchable - the floor is only for fighting
>   fighters and archers (classes with high acc)

Four separate changes, and they interlock — this is the answer to §50h (the rogue at 0.65× warrior DPS
at 20-28), arrived at from the other direction:
1. **`evade_mastery` becomes floor-only.** Today it is `EvadeFloor 10/20/30% + CritRate +20% + Evasion
   +20` (`Skills.Common.cs:593`). Strip the crit and the evasion; keep only the floor. ⚠ His text says
   "+10 evasion" — the code says +20, worth confirming which he means to remove (I read: all of it).
2. **Move the +20% crit rate to level 20**, out of Armor Mastery @32. That is exactly the early-blow
   problem in §50h: the rogue's blow gate is a 9.2% crit until 32.
3. 🔴 **Crit rate stops being additive and becomes MULTIPLICATIVE** on the weapon's base — a real
   formula change in `StatCalculator`/`RecomputeDerived`, not authoring. It also makes his 32-point
   worry disappear on its own: ×1.2 on a 12% dagger base is 14.4%, not 32%.
4. **Evasion is capped by authoring, not by more floors** — 14 (armor) + 4 (buff) = 18 is already the
   budget. The *floor* stays but is framed as an anti-accuracy tool only.

**Measure before and after** (`tools/BalanceMatrix`) — 2 and 3 pull in opposite directions.

### M9 follow-up, 2026-08-06 — the L2 research he asked for
> well a 30% crit chance buff is multiplier..not addition ...why should a passive be a addition? - later
> lvls a rogue gets a 50% crit chance increase ... Can we do a research on l2 dagger classes critical how
> it's applied ...even if it's additive the +20% evasion_mastery bonus is unnececery

**What L2 actually did — it changed, and both answers are "L2":**
- Crit rate is a 0-1000 number where **1000 = 100%**, so **500 = 50%**.
- Weapon bases: **blunt/fist 4%, dual/polearm 8%, dagger/bow 12%** — our dagger 12 / sword 8 matches.
- **Early chronicles (C1-C2): crit buffs and passives were MULTIPLICATIVE.** In **C3 they were changed
  to ADDITIVE**, and at the same time a **hard cap of 500 (50%)** was introduced. The two changes went
  together: additive only works because the cap contains it.
- Dagger blows used the auto-attack crit formula **plus a per-skill modifier** (~20% for Mortal/Deadly
  Blow) — i.e. the blow's own chance is not the raw crit rate.

**So his instinct is pre-C3 L2, and it is self-consistent — but the honest engineering answer is that
the shape barely matters at the top and matters a lot in the middle:**

| rogue @ dagger 12% base | +20% passive | +20% then +50% later |
|---|---|---|
| **additive** (today, C3-style) | 32% | 82% → **capped at 50%** |
| **multiplicative** (his ask, C1-style) | 14.4% | 21.6% |

We already have the 50% cap (`StatCaps`), so additive means *the cap does all the containment* and the
32-point spike he objected to is real. Multiplicative removes the spike — but ⚠ **it also means his
other goal, "higher crit rate early on", is barely served**: moving the rung to 20 would give +2.4
points, not +20. And it will *lower* rogue DPS across the board, which pushes against §50h (rogue
already 0.65× warrior at 20-28). **Recommendation: build it multiplicative as he asked, but measure the
rogue's whole 20-40 curve in BalanceMatrix first and be ready to raise the dagger base or the blow
modifier to pay for it** — that is exactly how L2 pays for it (the per-skill blow modifier).

✅ **Unconditional either way, his words:** *"even if it's additive the +20% evasion_mastery bonus is
unnececery"* — strip the crit and the evasion off `evade_mastery`, floor only.

Sources: [Predator — L2 critical hit mechanics](https://predator.ge/en/news/lineage-2-critical-hit-mechanics-explained-auto-attacks-dagger-skills-and-chronicle-changes)
· [PMfun — critical rate for daggers](https://forum.pmfun.com/viewtopic.php?t=36564)

**M10. 🟡 Balance todo — two items, one of which I cannot find in the code.**
> ### Champtions
> is getting killed while offline farming when his bufs worn off while the dagger is getting missed
> like crazy - i have 65 acc and 95 evasion .. 30% difference is way high for this low lvl
> - we need to lower champions passives debuff -20%pdef to -10% - now have less than the dagger -
>   same as mage
> mages have big mana problem - for 2-3 mins their MP is depleated

- **The 30-point acc/eva spread** = 30% miss at the same level (1 point = 1%, by design since
  2026-08-02). Two characters of the same level should not be able to open a 30-point gap; that is what
  M9's evasion budget is about. This is the *evidence* for M9, not a separate ask.
- ✅ **FOUND IT, 2026-08-06 — it is `Two-Hand Mastery`, not an armor passive.** His answer:
  > the passive is two handed weapon mastery ... The +30%atk and -20% defence.. We need to lower it to
  > 10. I want a warrior in a heavy not to have lower defence than a mage...it's not logical..

  `Game.Shared/Skills/Skills.WeaponMasteries.cs:77-81` — `WarriorWeaponMastery`, five rungs, every one
  carrying **`DefencePct: -0.20f`** (plus `Evasion: -3`). Attack is `PhysAtkPct 0.30` on rung 1 and
  **0.50** on rungs 2-5, so the trade gets *better* with level while the penalty stays flat at −20%.
  **Change: `-0.20f` → `-0.10f` on all five.** ⚠ I looked in the armor masteries and the class bonuses
  first and reported it didn't exist — it was on the WEAPON mastery, gated to `WeaponType.TwoHanded`,
  which is why a 2H Champion in heavy armor ends up under a robed mage. Trivial edit, five numbers.
- **Mage MP: 2-3 minutes to empty.** Known and expected — MP potions are on explicit hold pending the
  3rd-class kits. Worth re-opening that hold: the offline farm makes a dry mage a *death*, not a pause.

**M11. 🟡 One daily Apothecary quest, shared by every town.**
> Can we give every apothecary the same daily quest - taken from one returned to other (or just start
> from every apothecary and finished to the same) ? - just when im lvl 40+ i have no way to go back to
> the 1st town just to take it (gk costs money)... i want to start it from every town once a day - same
> quest - same id - they dont overlap - taken from one cannot be taken 2nd time

**Same quest id offered by every Apothecary, turned in at any Apothecary, once per day per character.**
Mechanically: many-to-many NPC binding on one quest def, and the daily flag already exists.

**M12. 🟡 A gatekeeper jump should land you at the destination GK.**
> should spawn you next to the new city GK (next not on top => gk.x+150/gk.y+150) and not in the middle
> of town. => I teleport then need to move again to the next gk so i can go to a zone

Beside, not on top. Trivial change in the teleport destination table.

**M13. 🟡 The [Talk] button (this is C9, specified).**
> Clicking one time opens the target - with the `Talk` button
> Clicking secont time or the `Talk` button - the char start to move towards the npc and talk (open npc window)
> When im talking to npc my move should be forbidden - many times now i get next to the gk -> open
> window to teleport but before that i clicked somewhere on the ground and with open window i get "Too far"

Three parts: the button, **walk-to-then-talk** on the second tap, and — the one that actually bit him —
**movement is locked while an NPC window is open**, so a queued ground tap can't drag you out of range
mid-dialog.

**M14. 🟡 Cap the vendor buyback list** at 10-15 items.

---

## Defects found in the tested sections

**48g. 🔴 The Blessing Box eats your unspent picks.**
> form 17 you click 10 ok .. get 10 .. but from 17 my second box i clicked 7 .. to finish the scroll
> collection .. but then my box disappeared with my 3 unused ...

Confirmed in code: `GameLoopService.HandleSelectBoxItems` takes `cmd.ItemIds.Distinct().Take(PickCount)`
and refuses only an **empty** selection — any count from 1 to 10 consumes the box. The client's Confirm
is not gated on the tally either. **A 250k box can be spent on 7 scrolls.** Fix both ends: server
requires exactly `PickCount` (message otherwise), client disables Confirm until `10 / 10`. ⚠ Pick-1
boxes are unaffected (1 == PickCount), but the same rule covers them.

**46d. 🔴 B7 half-fixed: you can TARGET an out-of-sight party member, but not INVITE one.**
> /ptinv -> `no player x nearby` - cannot invite player out of sight. - when i invite him when im next
> to him then leave his sight it works...

B7 changed the target frame; the invite path still resolves the name through a proximity lookup. An
invite by NAME should reach any online player (or at least anyone in the same zone), not just someone in
your grid cell.

**46m. 🟠 C11/B2: compare on a PENDANT still opens a stud.**
> its one window but still opening compare of a selected pendant it opens stud details

The merged compare+details window is right; the **worn-item lookup** picks the wrong jewel slot for a
pendant. B2 is not actually closed.

**46o. 🟡 G6 warehouse: raise both caps now, lower them when the expandable system lands.**
> cant remember the exact numbers but the warehouse slots were expandable and base as 150-100 and
> account max was 10 ... can u make them account to max now and private as well and leave a note when
> making the expandable system to lower them

The account bank's cap is the small one and it is in the way. Raise both to the max, and leave the
comment where the expansion system will need to pull them back down.

**46u. 🟡 The economy verdict — the cut was smaller than the arithmetic said.**
> - now im rogue 33 and have 2.6kk gold while selling only gear, a worrior 31 - 1.4kk
> - so i think lowering drop by 3.3 and increase the selling by 2.5 its actully a 25% decrease
> - now its only 3 times harder to gear up 😀
> - sooner or later we will get to L2 drop rates/sell prices...

He is right about the composition (13× rarer × 2.5× dearer ≈ 5× on gold, but he is reading the
*gearing* cost, which only moved 3×). Not a defect and not urgent — the direction of travel he wants is
"eventually L2 rates". ⚠ The old note still applies: the real fix is the **coin curve**, not another
multiplier.

---

## What PASSED (no comment needed, marked `x` in his file)

- **All of §48 except 48g** — the text-box fix (48a/48b, his "unplayable", **closed**), x1 rates,
  no buff-scroll drops, the Blessing Box UI and its 11th-tick refusal, bound scrolls, the two potion
  rungs, and the quieter bag.
- **All of §47** — F1 target hand-over, QSell, save-login, the Sprint/Dash family and its whole ladder
  including Sprint L2 over Dash Mythic, the warehouse quest-token rescue, and the four UI review fixes
  (tracker overlap, five pins, preset C, compare no longer jumping).
- **All of §46 except 46d/46m/46o/46u** — per-character auto marks, the per-account farm balance, B6,
  the 0-count hotbar slot, `/offline`, undo-a-bin-delete, quest tokens undisposable, the shared
  filter/tabs, gatekeeper tabs, NPC quest scoping, jewel swap by delivered M.Def, and **the entire quest
  section Q1-Q5**.
- **Carried forward, now closed:** 37a/37b/37c (partial-stack trading), 36f, 34c/34d, 33l, 32b/32h/32o/
  32p/32s/32y.

**Still blank after this pass:** 37d, 37e (trade shortfall / full-bag judgement), 36e (boss phase script
on re-pull), 32z (the auto-farm chain matrix), 25b (no combat-log out of a DoT), 13a (the 3h banner),
17-1 (jail border), 17-23 (client collision).


---

<a id="playtest-18"></a>

# ══ Playtest-18 ══

# Playtest-18 — the second 0.45.0 pass (owner, 2026-08-04)

**Source: his own file, `mytest-26216`.** As with [Playtest-Archive.md#playtest-17](Playtest-Archive.md#playtest-17) this is the
AUTHORITATIVE queue: his wording is kept verbatim under each item, my reading is the line above it.
Ids are namespaced by his own headings — **G**eneral / **Q**uest / **F**arming / **V**endors.

**The verdict in one line:** not a bug hunt — this is the **answer to the B3 deliverable** (which skills
to delete), two design questions back at me, and a list of interface friction he hit while playing.
Only three items are defects: quest tracking doesn't survive a restart, the vendor sell fraction is not
the number he expects, and the hotbar disables a consumable slot you can then no longer remove.

> One line in his file — *"[!] Just noticed the clerics(@20) all the passives"* — he told me to
> **ignore**; it was left over from the test. Not an item.

---

## General

**G1. 🔴 The skills to delete — this is his answer to B3, and it unblocks the deletion.**
> - `evade_mastery`, `reflexes`, `precision`, `anti_magic` — the four "identity floor" passives.
> - `archer_armor_mastery`, `archer_weapon_mastery` — orphaned by the archer→rogue merge.
> - `dispel_magic`.
> - God class + skills

That is §3 of [Playtest-Archive.md#skills-not-in-csvs](Playtest-Archive.md#skills-not-in-csvs) — the "granted to NOBODY" list — minus
two entries he did **not** name: `class_balance_*` (8) and the commented-out `lb_*` / `wc_*`, which he
asks about instead (G2). **God class + skills** = `hp_boost`, `greater_heal` **and the god table
itself** (`ItemRarity.God` and the never-registered class stay or go with it — ask before widening).
⚠ The original B3 ask also covered **Heavy Draw**: the safe operation there is deleting the **Rogue @24
grant** of `power_shot`, never the definition — three level-40 discipline skills are renames of it.
Twin Slash is already gone below 40.

**G2. ❓ What are the commented-out Lightbringer (8, `lb_*`) and Warchanter per-race (12, `wc_*`)
skills?** ✅ **ANSWERED — written up for him at the end of §3 of
[Playtest-Archive.md#skills-not-in-csvs](Playtest-Archive.md#skills-not-in-csvs).** They are the **level-40 HEALER disciplines**:
Lightbringer = the pure healer (per-race heal/cleanse/root/anti-heal + a shared party buff and passive),
Warchanter = the buffer (per-race nuke / mega chant / party HoT / passive). The defs are registered and
alive; what is commented out is **one line of learn assignments** in `ClassSkillTables.Third.cs`, dropped
pending his level-40 CSVs. ⚠ The Warchanter's **buff** layer is separate and IS live
(`RegisterWarchanterBuffs()` — every group buff and Harmony in play today). **My recommendation: keep
both.** They are unreachable, so they cost nothing, and they are most of a 3rd-class healer kit already
written — one uncommented line when his CSVs land. His call.

**G3. ❓ Mobs built like players — no inflated STR/CON, real gear instead.**
> Can monsters have no inflated stats like the con and the str and have items (like weapons armors and
> jewels) - they will start to use normal formulas and be like weaker players - they will have items like
> claws, talons armor like feather, skin, skeletons can use real weapons like blades and aegises and
> armors like bulwark/leather

A real design change, not a tweak: it would move mobs off `MobBaseStats` onto the player pipeline
(`RecomputeDerived` + equipment), so a mob's difficulty would come from its kit. Attractive — it makes
mob stats readable and reuses one formula set — but it touches every mob, the level-assigns-stats rule
and probably the drop tables. **Needs a design pass and his go before anything moves.**

**G4. ✅ BUILT 2026-08-05. Save-login checkbox on the client.**
> Add a checkbox to the client to save login information or not - now always the password field is
> "admin"

> **Built:** `[x] Save login on this device` under the password field (a full-width button, not a
> 20px box — the kit has no toggle and a phone wants a tap target; `[x]`/`[ ]` are ASCII because the
> TMP atlas has no tick glyph). ON stores username **and** password after a login that actually
> SUCCEEDED — storing on submit would remember a typo forever. OFF stores neither, comes up blank,
> and **wipes what is already on disk** rather than merely stopping future writes. The server ADDRESS
> is remembered either way: it is not a credential. Default ON, and with nothing saved yet the
> inspector's admin/admin still fills in, so the debug rig is untouched — the moment he logs in as
> anyone else, that is what comes back, which is the whole of the complaint.
> ⚠ PlayerPrefs is not a secret store (a plain XML file in the app's private data). That is what
> every phone "remember me" gives, and it is why this is a choice rather than unconditional.

**G5. ✅ BUILT 2026-08-05. The Dash potion and the rogue's Sprint must not overlap — full spec, his:**
> Dash potion is the same as sprint skill just weaker (longer cd and weaker value)
> - Both are 15s, Potion CD 1min, Sprint cd 30s
> - Potion - C15, U30, R45, M60 -> Effect: E1, E2, E4, E5
> - Sprint L1-40, L2-60 -> Effect: E3, E6
> - Same effects or weaker are removed and replaced by the new effect -> SprintL1 replaces Dash C/U,
>   SprintL2 replaces Dash C/U/R/M and SprintL1 …

Read: one **speed family** with six rungs, ordered `E1 < E2 < E3 < E4 < E5 < E6`, where the potion owns
E1/E2/E4/E5 (Common 15 / Uncommon 30 / Rare 45 / Mythic 60) and Sprint owns E3 (L1, +40) and E6 (L2,
+60). Both last 15s; potion cooldown 60s, Sprint 30s. This is exactly the existing **family + Rank**
machinery (`ApplyBuff`) — a stronger rung replaces a weaker one and a weaker one is refused — so it is
an authoring change, not new code.

> **Built:** Sprint joined the `dash` family, and the family is now ranked by MAGNITUDE end to end:
>
> | rank | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
> |---|---|---|---|---|---|---|---|---|
> | | Dash C +15 | Dash U +30 | **Sprint L1 +40** | Dash R +45 | Dash E +50 | Dash L +55 | Dash M +60 | **Sprint L2 +60** |
>
> That is his two sentences exactly: Sprint L1 replaces Dash C/U (and is refused under anything
> stronger), Sprint L2 replaces everything including Sprint L1. **Sprint L2 sits above Dash M at the
> same +60 on purpose** — a class skill you levelled must not be overridable by a bottle, the same
> rule a group buff follows. Durations 15s both; reuse Sprint 30s / potion 60s, both already correct.
>
> ⚠ Sprint's two levels need DIFFERENT ranks and `Rank` lives on the `SkillDef`, not on `SkillLevel`
> — so Sprint is authored as a **one-child wrapper** whose level picks the child (`buff_sprint_1` /
> `buff_sprint_2`), the same machinery a potion uses. Its old private `"sprint"` BuffKey is gone;
> that key having a family to itself is precisely what let you hold Sprint and a Dash potion at once.
>
> **Two things I decided, both one line to reverse:**
> 1. **The potion keeps all SIX rungs.** He named four (C15 U30 R45 M60); Epic +50 and Legendary +55
>    also exist, are in the drop tables, and deleting them is a content change, not a stacking fix.
>    Ranks 5 and 6 are theirs. Say the word if he wants the ladder cut to four.
> 2. **Sprint level 2 is learned at 40.** He gave the level and the value but not where — and the
>    authored rogue CSV stops at 36. 40 is the next rung on that block's own 4-level cadence and
>    where the 3rd-class disciplines already sit. Without a grant, level 2 is unreachable.

**G6. ✅ BUILT 2026-08-05. The warehouse must show slots used / total.**
> Top-right of the window, per bank (private vs account have different caps), and it turns **red** once
> there is no room left — until now a full warehouse announced itself only as a refusal in the chat log.
> in the warehouse need to see spots taken/all - now i try to deposit and cant .. only after opening chat
> i saw that warehouse is full

**G7. ✅ BUILT 2026-08-05. A hotbar consumable slot at 0 count must not be DISABLED.**
> Disableing hotbar potion/consumable slot when I have 0 - means i cannot remove it from the bar.. make
> it like always in 100% cooldown - it looks the same just is not disabled.

The slot going dead also kills the drag/long-press that would remove it — the bar traps a slot you can
never clear. Draw it as a full cooldown sweep instead: same look, still interactive.

✅ Exactly that. `PressAndHold.Enabled` is wired to the button's `interactable`, so an out-of-stock item
slot lost the one gesture that opens Move/Remove/Auto. Such a slot now stays interactable and draws the
reuse sheet at full height with no countdown text (there is no timer, only an empty bag), and `FireSlot`
returns early so the tap is inert rather than earning a refusal from the server.

**G8. The rogue's Weapon Mastery has its crit damage swapped between 24 and 28** — *"i fixed it in the
rogue-csv"*. Already the second bullet of RoadmapNext 🔴 3.

**G9. `crit dmg` in the CSVs is FLAT, not a multiplier** — *"x0.8 .. its + .. so its flat increase. The
formula should have it -> added to base atack before the critical dmg % increase."* Already ruled
2026-08-05 and specified in [design/CritBlowAndDouble.md](../design/CritBlowAndDouble.md).

---

## Quest

**Q1. ✅ BUILT 2026-08-05. 🔴 Quest tracking is not persistent.**
> Quest tracking is not persistant. - I restarted the server and dont know if is because of logout or
> just not peristant per character

**Diagnosed — it is neither logout nor the server.** The tracker is a client-side
`List<string> _trackedQuests` in `GameUi.Quests.cs` and nothing ever writes it anywhere: not to the
server, not even to PlayerPrefs. So it dies with the app, and it is per-INSTALL rather than per
character. It must be stored per character; server-side alongside the quest log is the honest place.

> **Built:** the pin is a `Tracked` flag on `CharacterQuestState` — the per-character quest progress
> that is already persisted (`ActiveQuestsJson`), so it survives a relog and follows the character to
> another phone. **No schema change and no db reset:** an old save simply reads `Tracked = false`.
> The client no longer owns the list at all; it reads `QuestEntry.Tracked` and asks the server to
> toggle, which is the same rule the skill bar taught — the client never authors state it did not
> receive. The cap (5) moved to `GameConstants.MaxTrackedQuests` so both halves agree on it.

**Q2. ✅ BUILT 2026-08-05. Accepting a quest auto-tracks it.**
> Pinned by the SERVER on accept, so it is true however you took the quest. It **yields** at the cap
> rather than evicting: an automatic pin has no business pushing off one you chose. (An explicit
> Track past the cap still evicts — you asked for that one.)

**Q3. ✅ BUILT 2026-08-05. The tracker row shows only the objectives** (items / kills), not the full
description.
> It reads the structured steps now instead of the dialog's pre-formatted step sentence: a gathering
> contract lists `item  held`, a kill step puts its `3 / 10` on the same line as the objective, and
> the description, the location and the story are gone.

**Q4. ✅ BUILT 2026-08-05. Clicking a tracker row opens that quest's DETAIL page**, not the quest
window's list.
> The tracker was one block of text with nothing to click; it is now one tappable row per pinned
> quest, each opening its own Details. Dragging the panel still works — a drag cancels the tap.

**Q5. ✅ BUILT 2026-08-05. The Active tab's rows must be short, like the Available rows** — name plus
"Ready to hand in", level range, the give/return NPC, steps. Not the full text.
> Quest window in the Accepted tab the row must be only the Name and some short info -- like the
> Available rows --> "Ready To hand in", lvl range, return/taken npc, steps -- not full details

> **Built:** an Active row is now name + step `2 / 3` + level band, the status line, the progress
> NUMBER (and the gathered count for a contract), and **From: <npc> — <town>**, which every tab now
> carries. The step text, "Where:" and the mob name are gone to Details.

(Q3 and Q5 are the same rule as C6: full text lives in Details and nowhere else.)

---

## Farming

**F1. ✅ BUILT 2026-08-05. Turning auto-farm OFF must not drop what you are fighting.**
> When disablaling auto-farm not to cancel target and close target window and stp attacking - i must
> reselect mid fight to finish the kill

Switching to manual should leave the target, the target window and the attack running — only the
autopilot stops.

> **Diagnosed:** the server was still swinging the whole time. `AutoPilot` pushed `AutoTarget(null)`
> on the first tick after the toggle — "the window must not keep showing the autopilot's last pick" —
> and the client's `TargetId` follows that push, so the selection vanished while `CombatTargetId`,
> `Engaged` and `AttackCommandTargetId` were all still set. Re-selecting "to finish the kill" was
> re-selecting something already being hit.
>
> **Built:** switching to manual now **hands the target over** instead of cancelling it — the push
> re-sends the current target as long as it still names something alive and present, and only then
> null. Nothing else is touched, so the swing continues and only the autopilot stops. The window
> still clears the moment the target is genuinely gone, and the paths that really end a fight
> (`StopAutoHunt` on death or a spent budget) clear `CombatTargetId` themselves, so they push null
> exactly as before.

---

## Vendors

**V1. ✅ BUILT 2026-08-05. A quick-sell toggle, mirroring the bin: `[QSell On/Off]`.** With it on,
`[Sell]` sells the max amount in one tap instead of asking for a quantity — the same shape as the
inventory's `[Del On/Off]`.

> **Built:** `QSell: off / ON` beside the Sell tab, in the bin's armed red when on, and **hidden on
> the buy side** — a toggle that goes dead when you tap Buy reads as a bug (the same reasoning that
> put the category filter on both lists rather than blanking it). With it on a row sells the WHOLE
> stack on one tap: no numpad, no confirm. It deliberately covers the non-stacking case too — a
> single sword is one tap either way, and a "quick" sell that still popped a dialog for half the
> rows would not be one. The window title says so while it is armed. Unlike the bin it may skip the
> confirmation outright, because a sale is undoable from the buy-back list.

**V2. 🔴 Sell price = 0.25 of the buy price, for EVERYTHING.**
> All items must be sold for .25 of their price .. now they are sold for .8 (equipment)
> I know this will lower the price of A/S grade but the idea is not selling in the shop getting rich
> from trash.

✅ **RESOLVED AND BUILT 2026-08-05 — but not as written.** The 0.8 was his own misread: he sold a
**Feretite Robe** and read the number as the gloves' price. So there was no bug, and the ask became the
real one underneath it — *"selling items/trash making money ok .. but not farming"*.

He then produced the best economy datum this project has: **three characters, same ~14-15 h idle farm.**
Mage 34 selling nothing = **350k**. Tank 36 selling only equipment = **3.3kk**. Rogue 34 selling
everything = **4.6kk**. BalanceMatrix reproduces all three and shows **gear is the entire faucet** (10×
the coin drop; mats + potions are 2%).

**Shipped:** the cut went on the **drop rate**, not the price — cutting the price alone leaves you
wading through the same flood of junk, which is the actual complaint. Gear groups **×1/3 → ×0.025**
(13× rarer) and `GearSellDivisor` **25 → 10** (each drop worth 2.5× more). Measured: **4,055,588 →
1,227,289** over that farm, gear:coin from 10.3× down to 1.9×.

**V2b — the scroll pass, same day, also BUILT.** He corrected the consumable finding: Return/Resurrection
were already cut 20×/200× and *"are usefull u wont be seling all"*; and *"if you sell them not trade or
ecnaht the gear .. well goodluck not being part of the economy."* His ask: enchant + attribute scrolls
need *"lower the chances + move them in the lvls a bit"*. Measuring it flipped the diagnosis twice:

- **Enchant and attribute scrolls already sell for 0** (no `Value:` on the ItemDef) — they cannot feed
  the gold economy at all. The reason to cut them is the BAG, not the faucet.
- **The consumable gold was buff potions/scrolls**, 155/kill. His playtest-17 *"buff pots are 0 sell"*
  had **never been implemented** — and the ÷10 had just made them 2.5× richer.
- **Attribute scrolls at 27%/kill were an accident**: independent rolls take the global ×3 that the
  guaranteed groups are exempt from. Authored 0.09, delivered 0.27.

Shipped: `SellPriceOverride: 0` on every buff potion / buff scroll / Dash potion · enchant share of a
scroll rung 0.5 → **0.15**, floors 1/20/45 → **10/30/55** · attribute chances cut ~5× and spread over
their band (floors 40/52/61/76/80/84). Measured at level 33: enchant 30% → **9%**, attribute 27% →
**3.6%**, buff gold 155 → **2**/kill. **Total 1,038,115 = 1.04× target** (gear 65 / coin 34 / cons 1).

⏭ **Still open, not urgent:** gear value follows the tier ladder while coin is linear, so the ratio
still drifts to 51× by level 76 — the fix there is the **coin curve**, not another multiplier.
Full detail: `docs/design/EconomyRework.md` §4a + §4b.

**Later, not now — trash becomes crafting mats instead of gold:**
> Later we can make trash disasemble for crafting mats (rarity for mats rarity) (grade for mats ammount)
> - common -> common mats (F common 1-2, E common 5-10 ... S common - 500-1000)
> - uncommon -> uncommon mats (F common 2-3, E common 5-10 and uncommon 1-2 ... S common - 500-1000 and
>   uncommon (100-200))
> - etc... -> higher grade lowers the previous grades mats and increasing own

Rarity picks WHICH mats, grade picks HOW MANY, and each rarity step trades some of the previous tier's
mats for its own. **This belongs to crafting** — file it with [design/Crafting.md](../design/Crafting.md).

---

## ⏭ Owed back to him (blocking his decisions)

1. ~~**G2** — what `lb_*` (8) and `wc_*` (12) are~~ — ✅ answered in Playtest-Archive.md#skills-not-in-csvs; awaiting his
   keep/delete ruling (my recommendation: keep).
2. **G1** — a ruling on `class_balance_*` (8), which he skipped, and confirmation that "God class +
   skills" means the table as well as `hp_boost` / `greater_heal`.
3. **V2** — which item he saw selling at 0.8.
4. **G3** — my design read on item-carrying mobs before he commits to it.
5. **G5** — two calls I made to ship it: the Dash ladder KEPT all six rungs (he named four — Epic +50
   and Legendary +55 also exist and are in the drop tables), and **Sprint level 2 is learned at 40**
   (he gave the value, not the level; the rogue CSV stops at 36). Both are one line to change.

---

## Where the queue stands (2026-08-05)

**Built:** G6 G7 · Q1 Q2 Q3 Q4 Q5 · V2 V2b · **F1 · V1 · G4 · G5**.
**Not built:** **G1** (the skill deletion — unblocked by his answer, but still needs the two rulings
above), G2 (answered, awaiting his keep/delete), G3 (design, needs his go), G8 + G9 (already on
RoadmapNext 🔴 3 / `CritBlowAndDouble.md`).


---

<a id="playtest-17"></a>

# ══ Playtest-17 ══

# Playtest-17 — the 0.45.0 pass (owner, 2026-08-03)

**Source: `Open-Checklist.md`, filled in on the phone.** This file is the AUTHORITATIVE queue —
his own wording is kept verbatim under each item, my reading is the line above it. The checklist ticks
themselves went back into `TestChecklist.Unity.md` (search `P17`).

**The verdict in one line:** the biggest unplayed batch in the project's history — §36 and §38-§43,
six versions' worth — went through in a single pass and **almost all of it passed**. 84 checklist items
verified, 22 more from the ancient playtest-11 list closed. What came back is not broken machinery;
it is a **queue of bugs at the edges plus a long list of "now make it a game"** — inventory hygiene,
the scroll/enchant economy, crafting, and a text-box bug that affects every input in the client.

---

## 🔴 BUGS — these are defects, not opinions

**B1. ✅ BUILT 2026-08-05. Auto-farm actions are stored per ACCOUNT, not per CHARACTER.**
> with the 1st character In the acc I put basic attack action on the bar and made it auto on ...(then
> delete that char) then entered with the newly created second char and when I put the action ot the bar
> it was already on.... When I removed it from the bar it still acted in a auto-farm. The actions should
> act as a skill for the character not for the account. Also removing something from the bar
> automatically disables the auto-on..when u put it back u need to reactivate it.

Two faults in one: the auto-on flag survives a character DELETE (so it is keyed on the account), and
removing a slot from the bar does not clear its auto flag — the autopilot keeps firing an action that
is no longer on the bar. Rule: **auto-on belongs to the character, and un-slotting always clears it.**

✅ Neither fault was on the server, where the marks have always been a per-character column. `AutoSkills`
is a client-side `HashSet` on the singleton `GameBoot`, and **nothing ever cleared it** — not on leaving
the world, and not when the server pushed an EMPTY list, because that push was guarded by
`c.Skills.Length > 0` to protect a "basic attack on by default" that had already been removed. So the
marks simply walked from one character into the next one's session. Three changes: the guard is gone
(the server's list is the truth, including when it is empty), `ResetWorldTransients` clears the set with
the rest of the per-character state, and `AssignSlot` now clears the auto mark of whatever token it
displaced — unless the same token still sits in another slot, so *moving* a skill never disarms it.
`ToggleAutoSkill` also pushes unconditionally now; it used to push only while auto-hunt was running,
which left a mark made with auto-hunt off living nowhere but the client.

**B2. Compare on a pendant opens a RING's detail window.** Wrong slot resolved when picking what to
compare against — see also C10, the same swap logic is picking the wrong jewel.

**B3. Skills exist that are not in the CSVs.**
> Heavy Draw, Twin Blade should not exist. They are learned after lvl 20 and they are weaker than the
> actual csv skills - u can give me list with what is outside the csvs so i can tell you what skilsl to
> be removed

⏭ **Deliverable back to him: a list of every skill in the catalog that is NOT in his class CSVs**, so
he can mark the ones to delete. Do not delete anything before he answers.

**B4. ✅ BUILT 2026-08-05. Quest items appear in vendor sell lists and the warehouse.**
> Quest items must not be shown in the selling vendor list or in the keeper. quest items are in their
> own bag, unless is specifcally told that this quest item is tradable/sellable and it will go inside
> the normal inventory not the quest bag

Pairs with §39e: tokens parked in the warehouse stop counting toward the quest step but are still taken
on hand-in. **A quest item must be refusable by every disposal path** (sell, both banks, trade, bin).

> **Built:** sell and the account bank already refused one; the holes were the **private** bank (server
> `HandleWarehouseDeposit` now refuses, and neither bank lists the row), the **trade** table (the server
> dropped it silently — the client no longer offers it) and the item window's **Bin** button (the server
> refused, the button was still drawn). Every path now refuses it *before* the tap.

**B5. ✅ BUILT 2026-08-05 — and it was NOT a display bug. Relogging resets the auto-farm/offline TIMER.**
> Reloging make my auto farm timer to reset - server did not reset just the timer .. farmed for 15 mins
> went to town reloged then came back start to auto farm and timer from 7h44 to 7h59

~~The server kept the real budget (7h44 was right); the displayed remaining time jumped back up to 7h59.
A display/session-start bug, not a grant bug — but it reads as free time.~~

🔴 **That reading was wrong: the time really was being given back.** The caps were per-SESSION elapsed
counters on the CHARACTER, zeroed on every `EnterWorld` *and* again in `BeginOfflineFarm`, and never
persisted — so 7h59 was the honest display of a budget that had genuinely just refilled. His 14h
three-character farm (playtest-18) made it undeniable: per-entity counters meant three characters farmed
6h per 2h of wall clock. The allowance is now a **per-ACCOUNT daily balance** (8h online / 2h offline
free, 12h/4h premium) that is spent one tick per farming character per tick and refills at a fixed
server midnight. See the CHANGELOG and `docs/design/AutoHunt.md`. ⚠ schema change — db reset.

**B6. Every text box WIPES its pre-filled value on the first keystroke instead of editing it.** (§42k,
§42m)
> same problem with the general texbox. It auto fill /w name, but when I start to type it overrides it
> dosnt edit it … it's for every textbox..when it do "/w test" and when I select/start typing it clears
> the /w test and the message becomes normal - same goes for the ip/game connection string and any
> textbox ..click start type is clear old value not edit

**One fix, whole client**: focus must place the caret, not select-all-and-replace. This kills Reply,
the Whisper action and re-editing the server IP.

**B7. ✅ BUILT 2026-08-05. A party member out of range cannot be TARGETED at all.** (§17-24) — so assist
/ heal / buff / kick / change-leader are unreachable exactly when they matter.

✅ Tapping the roster row *did* set the target; the next world delta then threw it away. Interest
management stops sending an ally who walks out of view, and `GameBoot` cleared any target missing from
the snapshot — ~10 times a second — as a ghost guard. Party members are now exempt from that clear, and
the target frame falls back to the ROSTER row (name, level, class, both bars, `(out of sight)`), which
keeps arriving at any distance. The mob-only fast buttons stay hidden.

**B8. Soulcrystal-tier gear still prints A grade in item details** while the attribute scroll it accepts
is Mythic. (§43n) The display path and `AttributeSystem` read different grades.

**B9. The jail has no border**, so an admin teleported in is clamped straight back to the dungeon the
moment he moves. (§17-1)

**B10. Client-side collision still does not exist** — only the server rubber-band. (§17-23)

**B11. `/block` and `/like` have no chat commands and no Actions entries** (the buttons work). Also:
**you must not be able to block an admin or a moderator.**

---

## 🟠 CHANGES — agreed behaviour changes, small to medium

**C1. Chat must reset on exit.** A new character inherited the deleted character's chat log. Clear on
every relog, and cap the buffer (~1000 lines).

**C2. Newbie equipment untradable and unsellable**, and **timed like a rune** (30 days).

**C3. Timed items must show their remaining time in the details panel** — **green** over 7d, **white**
over 1d, **yellow** over 1h, **red** until it disappears.

**C4. Buff potions and scrolls get auto-on on the hotbar**, the same as buffs — "they act as a buff,
they should be threaded as one".

**C5. ✅ BUILT 2026-08-05. An NPC's window must list only the quests THAT NPC deals with.** Today every
NPC shows three.
> The OFFER list was already per-NPC. The `InProgress` list was not: it was every active quest you were
> carrying, rendered at every NPC in the world. It is now the ones **this** NPC gave you (`OfferNpcId`),
> plus the ones turnable here, which was always NPC-specific. The quest LOG is where you read your own.

**C6. ✅ BUILT 2026-08-05. Quest text only inside Details.** Everywhere else (NPC window, lists) shows
the NAME only.
> The NPC window's offer rows dropped their Location line and the in-progress rows dropped
> `CurrentStepText`. ⚠ Two deliberate reads of "name only": the in-progress row **keeps its `3 / 10`
> counter** (a number, not quest text — without it the row says nothing), and it **gained a Details
> button**, because a row that says less has to lead somewhere. Say if you want either changed.

**C7. ✅ BUILT 2026-08-05. Gatekeepers need tabs: Zones / Cities.** One flat list today.
> Two tabs inside the dialog, filtering on what the server already sends: a city has an empty `Group`,
> a hunting-field gate carries its field's name. Zones keeps the per-field headers. A gatekeeper with no
> local fields greys **Zones** out and opens on Cities rather than showing an empty tab.

**C8. ✅ BUILT 2026-08-05. Bag → Items needs tabs or a filter — Equip / Consumables / Mats — ordered by
name.** The same filter goes on **sell vendors and the warehouse keeper**.
> ONE classifier (`CategoryOf`) and one name ordering, shared by all three windows — three windows
> filtering three different ways would not have delivered the navigability that was the point.
> **All / Gear / Use / Mats** (+ **Quest** in the bag only, since a token can't be sold or banked at
> all). Gear = anything worn, runes included; Use = potions, scrolls **and boxes**; Mats = the rest.
> The bag traded its "Items | Quest" pair for the five, on a second row — five tabs don't fit beside
> the Equip and Del toggles. The vendor strip filters the **buy** list too: same window, one strip, and
> a filter that went dead on the Buy tab would read as a bug.

**C9. NPCs get a [Speak] button** where a monster's [Attack] button sits: it walks you into range and
opens the dialog.

**C10. ✅ BUILT 2026-08-05. Jewel swap picks the wrong piece.** Mythic F ring + Uncommon E ring,
equipping another E ring replaces the *Uncommon E*, not the F. **Swap must weigh grade AND rarity (or
simply the defence value), not rarity alone.**
> Took your fallback: `JewelStrength` is now the **M.Def the jewel actually delivers**, enchant
> included, with MP then HP breaking a tie. Rarity is only ever a fraction of a *grade's* ceiling, so it
> can never order two grades against each other — Mythic F gives 9 M.Def, Uncommon E gives 12, and the
> old key called the Mythic stronger. Also fixed the item window printing M.Def **un**-enchanted, which
> is the line you would have checked this against.

**C11. ✅ BUILT 2026-08-05. Compare and details are ONE window, not two.** (Takes **B2** with it — a
pendant opening a ring's window can't outlive it when there is only one window.)
> They must be as one (like the equipment panel in the bag) - You click compare and it extends to left
> and shows the equiped item details. The equiped item part dont have the bin/unequip buttons - its a
> comparison part/column/side - only item details for equiped. If I select item from inventory then
> click compare - it will show left side the equiped details (nothing more) and on the right the
> selected item details + [bin][equip] buttons. If I select item from equipped panel then click compare
> - left side the equiped details, right side the same item details + [bin][unequip] buttons.

Built exactly that: one panel that **grows a second column to the left** (the shape the bag's paper-doll
already uses), selected on the right with its buttons, worn on the left with none. Compare **toggles** —
it says "Hide" once open — and the window always reopens collapsed, so the shape is a decision you make
per item rather than one it remembers.

**C12. `/offline` command + an [Offline] button.** He cannot start offline farming at all any more —
the WPF client had a button, and leaving to character select is refused while in combat. The character
select must show the remaining time on a character that is offline-farming. (This is also §"others" Q1
— *"How to start offline farm?"* — the answer today is "you can't".)

**C13. Newbie quest band 10→35** (its gear is unusable later), and the follow-on chain *Blooded* /
*Proper kit* 12→35.

**C14. A 2h weapon should show something in the offhand slot** — the same icon greyed/disabled rather
than an empty square.

**C15. Add the Feretite Wand to the newbie selection box.**

**C16. Titles need more sass.** Gold board = golden, online = green, PvP = purplish (but not the PvP-flag
colour), PK = dark red. Not *"the Devouted"* — either *"Devouted"* or at least a capital *The*, and a
different font from the name.

**C17. Admins and moderators get their own titles.**

**C18. ✅ BUILT 2026-08-05. Buy-back must cover DELETED items, restored for 0 gold.** (Added 2026-08-03, after the
checklist went in — and it is not theoretical, it has already cost him an item.)
> buy back should work for deleted items as well .. u delete -> can buyback for 0 ... now i delete by
> mistake and cannot restore it (or buyback for shops last 10-20 items and restore last 5 items if they
> cant go in one place)

The buy-back design has said "last 10 deleted/sold" since 2026-07-24 (checklist item 32) and was never
built; **the DELETE half is the half he actually needs.** His own fallback if one list is awkward: the
**shop** keeps its last **10-20 sold**, and **deleted** keeps its own last **5**. ⚠ The deleted list must
NOT live behind a vendor window — you bin things in the field, which is exactly where the accident
happens. Restoring a binned item is free; a sold item still costs what it sold for (a sold-for-0 item is
free, same as a binned one).

✅ Built as **his fallback shape, two separate lists** — which is the better one: a shared list would let
a selling spree push the single thing you meant to undo off the end, and the two accidents have
different prices anyway. `Entity.Restorable` holds the last `GameConstants.RestoreSlots` = **5** binned
items (enchant and rolled attributes included, so a +6 sword comes back a +6 sword), `HandleRemoveItem`
records the exact quantity destroyed, and `RestoreItemCmd` carries **no npc id at all** — that is what
makes it work in the field. It opens from **Menu → Restore**, newest row first, and costs nothing.
Protocol **12** (new `RestoreUpdate` push); `MinAcceptedProtocol` stays 8, so an installed 0.45.x APK
still connects, it just has no Restore window. The vendor half of the design (a longer sold list) is
still open and still not urgent.

---

## 🟡 ECONOMY / FAUCET — drop rates he measured at level 23

**E1. Scroll of Return drop rate ÷20.** *"at lvl 23 i have 550 .. when im returning ill need 5-10 …
I use ~1 per ~250 dropped"*.

**E2. Healing potion drop rate ÷10.** *"now i have 200C and 120U at lvl 23 .. should have ~20C and 0U
and if i need them i need to buy them"*. **Uncommon must not drop before 40, Rare before 61.**

**E3. ✅ BUILT 2026-08-05. The buff-scroll and buff-potion economy** — his full spec:
- **Remove every buff SCROLL from drops. Even bosses.**
- **Buff potion sell price = 0.** *"no potions gold farm → they must be buff aid not exploit"* — still
  tradable and sellable so a fighter can pass a mage's potion on, just worth nothing.
- **Buff potions have 2 rarities** (down from the current ladder).
- **Dash potions are drop-only + boss points** — no vendor, sell price stays. They are classified as a
  buff potion but behave differently; leave them alone.
- **Remove every single-buff scroll that is not the MAX level of its buff.** One scroll per buff, Rare
  quality, top rung. *"no need for 6 scrolls for 1 buff"*.
- **The Apothecary sells buff-scroll SELECTION BOXES: 250k for a pick of up to 10.** At 76+ the 19 buffs
  cost two boxes = 500k — affordable in about an hour, so **a real buffer is still the better option**,
  which is the point. **Scrolls out of these boxes are untradable/unsellable; the BOX is tradable and
  sellable at price ÷ 25.**

> Built as specified. **17 scrolls survive** — one per buff, at its family's MAX rung, all Rare, all
> `Tradable: false` — so a boxed set is literally an NPC buffer's blessing for an hour. The eight
> scroll-only families keep their rung 6, which is also the first time the **Mythic rung has had any
> source at all**. Deleted: 43 item defs (the 18 Common/Uncommon buff scrolls, the 16 Epic/Legendary
> rungs, and the 9 **Rare potions**). Their ladder SKILLS stay — they are generated in bulk by
> `Ladder(...)`, and an unreferenced wrapper costs nothing.
>
> **Two rarities for potions = Common + Uncommon, and that is the load-bearing choice here.** Keeping
> the Rare potion instead would have left the top of every ladder falling out of the sky for free,
> and the 250k box with nothing to sell. Now a family reads *Lesser (found) → plain (found) →
> **scroll** (bought)*, and the thing you pay for is always the thing at the top.
>
> ⚠ **The rung split was a trap.** A drop rung divides half its weight among however many ids are in
> it, so deleting 17 scrolls would have handed their entire share to the surviving potions — silently
> DOUBLING the potion faucet as a side effect of a change meant to remove drops. The buff half is an
> explicit per-item chance now (the exact number each item delivered before), so every surviving
> potion drops at the rate it always did and the scrolls' share simply leaves the world. Measured:
> consumables per kill **33 % → 18.5 %** at level 33; total farm gold **unchanged at 1.04×** target,
> because buff potions already sold for 0. This is a **bag** fix and a **gold sink**, not a faucet cut.
>
> `tools/BalanceMatrix` grew an assertion for it — the Blessing Box's own contents are the list of 17,
> so the guard and the box can never drift apart. It reads **0 of 17 in drop tables**.
>
> The **Dash potion left the Apothecary's shelf** with the same change (drop + boss points only, his
> spec) and keeps its own per-item drop rate exactly. Rungs 3-5 of the scroll group are Dash-only now.
>
> Client: the selection popup grew a **pick-many mode** — rows toggle `[  ] / [x]`, the title tallies
> `3 / 10`, Confirm sends them in one go and the 11th tap is refused out loud rather than dropped.
> (The server already took `Take(PickCount)` over the distinct picks; only the chooser was pick-one.)
> ⚠ The tick is ASCII on purpose: the TMP atlas is baked, and a checkbox glyph would draw as a hollow
> box — the same trap that killed the `●` target marker in 0.43.1.

**E4. Attribute-scroll drop bands** — Common from 40, Uncommon 52, Rare 61, Epic 76, Legendary from
bosses 76+, Mythic from bosses/instance bosses 80+ and dungeon monsters at 90.

---

## 🔵 DESIGN — the enchant rework (his spec, verbatim intent)

**D1. Three enchant scroll TYPES, and rarity means GRADE, not behaviour:**

| Scroll | On failure |
|---|---|
| **Scroll of Enchant** (today's Common behaviour) | the item BREAKS |
| **Greater Scroll of Enchant** (today's Rare behaviour) | −1 enchant |
| **Safe Scroll of Enchant** (new) | keeps the current enchant level |

…and the RARITY of the scroll selects the grade it works on: **Common→E, Uncommon→D, Rare→C, Epic→B,
Legendary→A, Mythic→S** (one grade below the attribute-scroll bands).

**Drops:** normal scrolls drop like today's Uncommon (Common from 20+, Uncommon 40+, Rare 52+, Epic 61+,
Legendary from elites at a low chance and bosses higher 76+, Mythic from bosses/instance bosses 80+ and
dungeon monsters at 90). **Greater** drops from elites at a *very* low chance, **Safe** only from bosses
at a *very very* low chance. Below A grade there are no elites — those come from **instances** later.

**D2. Admin support for it:** every scroll in the admin menu, plus **`/enchant <value>`** — opens an item
picker and enchants the chosen weapon/armor to that value, unrestricted (`/enchant 999999` on an F
weapon must work).

**D3. Crafting is now the blocker for the item ladder.**
> We need the craft - professions, window, etc .. now even in admin the only mytic are the set,
> everything else is epic rarity

**D4. A new party buff at the top of the Frenzy family** ("Madness" or similar) — an Improved Frenzy with
no stat change of its own. Plus: **the healer gets all the single buffs + single Frenzy; party buffs and
Harmonies stay Warchanter-only; 2-3 more Harmonies and 1-2 more improved buffs for 76+.**

**D5. A [Combat] chat tab in its OWN window** — see §42h.

---

## Still untested after this pass (no APK problem, just not reached)

§37 partial-stack trading (needs a second character — the duo-icon rig can do it now) · §36e boss
phase re-pull · §36f safe-zone kiting · §34c/§34d scroll consumption · §33l party-wide improved buffs ·
§32b class change without relog · §32h potion faucet · §32o escape scrolls sellable · §32p buff-potion
sell price (**superseded by E3 — the answer is 0**) · §32s party members can never be hit · §32y
`/droprate item` · §32z auto-farm skill chains · §25b combat-logging out of a DoT · §13a the 3h banner ·
§9a chat peek/fade (not built).


---

<a id="playtest-16"></a>

# ══ Playtest-16 ══

# Playtest-16 — 2026-08-01, on the phone, server 0.42.0

The owner's own test sheet, **verbatim**, and his answers against it. He replies **"True"** when an
item passes.

## ⚠ His numbering is the VERSION, not the checklist section

He keeps his copy numbered **36x / 37x / 39x / 40x / 41x = 0.36.0 / 0.37.0 / 0.39.0 / 0.40.0 / 0.41.x**.
`TestChecklist.Unity.md` numbers the same items by **section** (§32 / §33 / §34). So **"37c" is not a
§37** — translate before searching. The mapping is in the results table below. (0.38.0/0.38.1 — jewel
slots and `/droprate item` — are not on his sheet at all.)

## Verdict

**17 items pass. 4 pass and still fail the reader. 2 new bugs, both client-side.** Nothing crashed,
nothing corrupted, no missing system. The one real simulation bug of the session wasn't on this sheet —
mob regen running on the *player's* CON curve — and was found by playing, not by reading.

All 4 follow-ups were built the same day (0.42.2, checklist §35a-d) and both bugs fixed in 0.42.1.

---

## The sheet, verbatim

```
36a.[ ] A potion overrides ONE part — a Rare Alacrity potion takes the cast-speed
        part of Improved Speed and leaves movement/atk-speed alone.
36b.[ ] Equal rank keeps the LONGER time — a 20-min potion must NOT eat your 1h scroll
        of the same tier.
36c.[ ] A refused consumable stays in the bag — no cooldown, and it says a stronger
        blessing is up.
36d.[ ] Auto-farm doesn't re-buff a live buff and doesn't drink a stack one bottle at a
        time; Dash is never auto-drunk.
36e.[ ] Relog is not a free refresh — buffs come back with LESS time, siblings included.
36f.[ ] Wind Walk is gone. Dash has six rarities on its own family and never evicts Swift.
37a.[ ] An improved buff is ONE square (was four). Timer = shortest part, tap lists the
        parts, press-and-hold removes the whole group. A potion square still reads "Swift".
37b.[ ] NPC buffer = basic only, 1h each, no group and no Harmony; price unchanged.
37c.[ ] Set bonus lists its pieces and which slots you have filled.
37d.[ ] A stackable at a vendor opens details FIRST, not the numpad.
37e.[ ] Character select has a Delete button (this also unblocks 28e).
37f.[ ] Drop list is a tree — group title + group %, items indented under it.
37g.[ ] Mastery/passive numbers group by STAT, not "+cast, −cast, +cast".
37h.[ ] Hotbar consumables count: 1, 2 … 99, then 99+.
37i.[ ] Auto-farm / offline farm show remaining time (24h00m01s format).
37j.[ ] The range ring shows only when the toggle AND auto-farm are both on.
39a.[ ] Cyclic OFF = first-available: 1-2-1-3-1-2. Cyclic ON = 1-2-3-1-2-3 (it may wrap
        early only if everything later is on cooldown).
39b.[ ] Priority: a needed heal beats a buff, a buff beats an attack.
39c.[ ] Heal threshold 50 = nothing above 50%. Set 100 on a healer = heals on cooldown,
        onto the most injured party member. Heal row OFF = never heals.
39d.[ ] A buff is renewed under 60s left, not only when expired, and never over a
        stronger version of the family. A rank-2 debuff replaces rank-1 on the mob.
39e.[ ] Assist party leader ON: you hit only his target, follow him onto a new one, and
        stand with an EMPTY target frame when he has none.
39f.[ ] All of it survives a relog (no db reset needed).
39g.[ ] Under 40 every gatekeeper row reads "Free" and costs 0 gold. At 40 the distance
        fee is back, minimum 50 — quoted price must equal the gold you lose.
40a.[ ] THE ONE THAT MATTERS: cleric's Might and Bulwark up (+8/+8) → a Lesser Might
        Potion is REFUSED and not consumed, P.Atk unmoved. A Greater one (+15%) replaces
        The P.Atk part while the +8% P.Def stays.
40b.[ ] Class buffs cast the same numbers as before — one exception: Might no longer
        Raises M.Atk. Check a mage's M.Atk falls, and that Force restores it.
40c.[ ] Apothecary stocks Might · Bulwark · Force · Ward (Lesser); scrolls drop. 20 min
        Potion / 1 h scroll; the scroll takes a second to read, the potion is instant.
40d.[ ] NPC buffer = 19 single blessings + Full buff, the list scrolls, each cancellable
        Alone. Full buff should cost about what nine buttons did — say so if it doubled.
40e.[ ] Scroll-only families (Body · Soul · Vigor · Serenity · Focus · Ferocity ·
        Insight · Frenzy) have NO potion at any price; cheapest scroll is Epic, drops
        60+/76+. Mythic buff scrolls have no source yet — not a bug.
40f.[ ] Nothing orphaned: no stale square that never expires, no dead skill on the bar.
41a.[ ] Aim: accuracy +1/+2/+4, potion AND scroll at Common/Uncommon/Rare, vendor-stocked
        at Common, sits next to Agility. Cleric's Aim and an Aim potion do not stack.
41b.[ ] A cleric learns 14 SEPARATE buffs at 30-50 MP (base mage gets Might + Bulwark at
        7). Buffing the whole list must land the same numbers as before — more casts,
        more MP, same result.
41c.[ ] Warchanter kit: singles top out 40-64, Harmony at 60/62/64 (200 MP, 20 min,
        nothing else grants them), the five improved groups at 66/68/70/72/74 (150-200 MP).
        Each family is at its MAX rung before its group appears.
41d.[ ] Improved buffs and Harmony are PARTY buffs — cast one in a party and every member
        within 800 gets it, not just the target.
41e.[ ] Learning an improved group DELETES its singles from the bar and the learn list
        (buff bar unaffected).
41f.[ ] Admin buff = 27: five groups + three Harmony + 19 singles. The bar shows the
        groups COLLAPSED (Might and Bulwark · Force and Ward · Focus and Ferocity ·
        Body and Soul · Swift and Sure), Harmony as its own three squares.
```

---

## Results

| His | Checklist | Item | Result |
|---|---|---|---|
| 36f | 32i | Wind Walk gone; Dash's own family, never evicts Swift | ✅ |
| 37b | 32w | NPC buffer basic-only, 1 h, no group/Harmony | ✅ |
| 37c | 32c | set bonus lists its pieces + filled slots | ✅ **but the EFFECT isn't shown** — *"what does that set do?"* |
| 37d | 32d | vendor stackable opens details first | ✅ **but it's a double confirmation** |
| 37e | 32e | character-select delete button | ✅ (also unblocks 28e) |
| 37f | 32f | drop list is a tree | ✅ **but the item rows want their own %** |
| 37g | 32g | passive numbers grouped | ✅ **but grouped by the WRONG axis** |
| 37h | 32n | hotbar consumable count 1…99, 99+ | ✅ |
| 37i | 32q | auto/offline farm remaining time | ✅ |
| 37j | 32r | range ring needs the toggle AND auto-farm | ✅ |
| 39g | 32u | free travel under 40, fee back at 40 | ✅ |
| 40c-40f | 33c/33d/33f/33g | potions+scrolls stocked · buffer 19+Full · scroll-only families · nothing orphaned | ✅ (incl. *"mythic scrolls have no source — not a bug"*) |
| 41b, 41c, 41e | 33i, 33j, 33l | cleric's 14 singles · Warchanter kit · groups delete their singles | ✅ |

**Not reported on:** 36a-36e · 37a · 39a-39f · 40a · 40b · 41a · 41d · 41f.

## ⚠ Four rows on this sheet now test a rule that no longer exists

**0.42.0 reversed the "a group is a bag of independent children" model** (his call: *"in l2 an improved
buff overrides its single parts… a single buff cannot override it"*). A group is ONE buff again, rank
`100 + level`, which evicts its singles and cannot be overridden by a potion or a scroll. So:

- **36a is dead.** A potion overriding one part of an improved buff is exactly what 0.42.0 removed.
- **40a is dead.** Its second half — a Greater potion replacing the P.Atk part while the +8% P.Def
  stays — is the old model. The live rule: the group refuses both.
- **41f is dead.** The admin buff is **9 rows** now (5 groups + 3 Harmony + Frenzy, the only family no
  group covers), not 27, and there are no separate squares to collapse.
- **37a is half-dead.** A group is still ONE square, but because it *is* one buff — not because four
  squares are merged. Its timer is the buff's own, not the shortest child's.

**36b-36e still hold** (equal rank keeps the longer time, a refused consumable stays in the bag, the
autopilot doesn't re-buff or drink a stack, relog is not a free refresh). The live sheet for all of
this is checklist **§34**.

## The 4 follow-ups — all built in 0.42.2, checklist §35a-d

- **32c → 35a** — a set says which pieces it needs, never **what it grants**. He literally can't tell
  what a set does.
- **32d → 35b** — details-then-numpad is **two confirmations**. Wanted: the detail **on the row**, for
  every vendor item; for a consumable the **numpad IS the confirmation**.
- **32f → 35c** — the tree shows group title + group %; the indented item rows need **their own %**.
- **32g → 35d** — grouping by STAT was the wrong axis. Verbatim: *"sword/blunt +10 pAtk +10 mAtk +10
  cast, dagger/bow -100 cast — more compact, logically written, not throw everything there."* Group by
  **weapon group**, list its stats after it.

## 2 new bugs — both fixed in 0.42.1

1. **Press-and-hold fired on RELEASE.** *"I start to hold and after a 1-2s to register it as hold, not
   to wait for a release."* `PressAndHold` decided the gesture in `OnPointerUp`. It fires in `Update` at
   the threshold now, release is a no-op, plus a 40 px travel cancel — a scrolling list carries the row
   under the finger, so `OnPointerExit` never fires and a slow scroll would otherwise arm a hold.
   (Threshold 1.0 s here, cut to **0.65 s** in 0.42.3 after he read 1.0 s as *"like 2s"*.)
2. **A box's contents never appeared in the bag.** The server pushed fine; `RefreshBag`'s change stamp
   hashed only length + equipped + quantity + enchant, and opening a box is a **swap** — one out, one
   in — so the stamp was identical and the rows were never rebuilt. Fixed by folding `InstanceId` in.
   **The vendor stamp had the same blind spot** and was fixed with it.


---

<a id="playtest-15"></a>

# ══ Playtest-15 ══

# Playtest-15 — 2026-07-31, on the phone, server 0.34.3

Two characters: **Mage 1→25** (~2h: 1-20 in ~1h, 20-25 mostly idle/auto-farm) and **Rogue 1→20** (~1h).
First time the server ran on the phone under the new portable publish.

This file is the owner's report **verbatim**. Triage/build order lives in memory `playtest-15-queue`
and `docs/RoadmapNext.md`.

---

## Checklist results (against `TestChecklist.Unity.md`)

**Untested:** 25 (all) · 30i · 30j · 30l

**PASS:** 26 (all) · 27 (all) · 28a-28d · 29 (all) · 30b, 30c, 30d, 30f, 30g, 30h, 30k · 31a, 31b, 31c, 31d, 31f

**PASS with a finding:**
- **26d** — the quantity/stackable items have no details window in the vendor; tapping goes directly to the numpad.
- **28e** — there is **no delete button in character select** at all, so the admin-delete window can't be reached.
- **30a** — "now seems fine and will be better when we fix the drop logic". **The faucet test passes.**
- **30e** — the drop list still looks clustered. Wanted: the **group is a TITLE** carrying the group name
  and the group %, with its rows **indented below it**.
- **31e** — passive numbers are right but unreadable when they alternate; e.g. the mage's weapon
  proficiency reads "+cast, then −cast, then + again…". **Group them.**

---

## Running the server on the phone for the first time

```
root@localhost:~/Game.Server# dotnet Game.Server.dll
GC: Reserving 274877906944 bytes (256 GiB) for the regions range failed, do you have a virtual memory limit set on this process?
GC heap initialization failed with error 0x8007000E
Failed to create CoreCLR, HRESULT: 0x8007000E
```

Fix he applies by hand every update:
```
root@localhost:~/Game.Server# nano Game.Server.runtimeconfig.json
```
…and sets `"System.GC.Server"` from `true` to `false`.

> **Can we make it so it ships with this false so I don't have to do it each time on my phone after
> a server update?**

---

## Mage 1-25 (1-20 ~1h, 20-25 more idle, ~1h)

1. For 2h I managed to get to lvl 25 have **~1kk gold** (haven't sold potions/scrolls — because of
   current high price I can make lots).
   - Found common wand + uncommon aegis + the mythic Ferrite … Found rare E robe but the others are
     weaker so I'm using the newbie set.
   - **Should decrease the HP potion drop rate** — because of infinite amount of potions I cannot die.
     But I take good dmg and there were times I had to use the vampiric just to keep me alive
     (which drains MP like crazy — that's ok).
   - good levelling pace
   - good amount of gold
   - **nuker has Wind Walk**, a self buff that stacks with other buffs and should not be there.

## Rogue 1-20 ~1h

- good levelling pace
- good amount of gold
- **Battle Fury must go** — it's not in the original CSV.

---

## Bugs

1. Finishing the quests for class change and changing it from the class master:
   - my class doesn't update
   - need to relog
   - after relog there is a delay for the skills window to refresh to access my unlearned skills list
2. The **set bonus is shown but not the equipment required/filled** for the set — that's missing now.

## Need change

1. Training (Wooden shield) should have **35 def**.
2. Ferrite Aegis (F shield) should have **90 pDef as Mythic**.
3. Despite all please update **all training weapons to have the 5 mAtk** as stats shown + **wand to have
   pAtk 6 and mAtk 7** and **remove the +6 maxMP**.
4. In auto-farm there should be a **retaliate**:
   - a mob hitting you is higher priority than nearest
   - I'm getting ganked by orc archers and still kill the nearest
5. Need **NextTarget** (targeting closest/retaliate 5 and cycling through them) — **DEFERRED**.
6. There is **auto move** in auto-farm:
   - when auto-farm is on, whatever class you are, you don't move towards the target when
     `BasicAttackAction` is not active in the hotbar
   - now my mage goes for melee and just sits over the mob waiting for the next cast — that goes for all
   - ⚠ CLARIFIED by the owner 2026-07-31, after the first pass shipped this as "don't melee-walk
     CASTERS": the rule has no class in it. *"I don't want anyone to move for melee range if not
     explicitly commanded."* Only three things command it — the second tap on a target, the Attack
     button (hot bar or target frame), and, in auto-mode, the basic-attack action being on the bar and
     set to auto-on. Anything else stands still with the target selected; an active skill still moves
     into CAST range. A skill therefore never starts a melee chase on its own, for any class.
7. Make **basic attack on tap/click be AFTER the target window**:
   - I click once, it only shows the target, not immediately going towards it
   - after the target is shown, if I click again (on the same target) it starts to move
   - if the target window is open and I click another target it only changes the target window, not
     going for a basic attack
   - it's very annoying on mages/archers — basic attack is on the second click on the same target, or
     the basic-attack button
8. **Consumables need a count on the hotbar** — 1, 2, 3 … 98, 99, 99+. Over 99 it shows `99+`.
9. **Scrolls of escape and return cannot be sold** even when they are tradable.
10. **Buff potions should lower their selling price** the same ÷25 as the others: 1500/25 = 60, now it's
    450. I didn't sell any potions in the shop because having 100 is too OP.
11. **Add timers for auto-farm and offline farming**:
    - same logic as the buff timers — `24h00m01s` == `1d`
    - when I enable auto-farm, show the time on the button
    - or just a single line in chat with the remaining on both, on each auto-farm on/off change
12. The **"show farming range" toggle should only show when it is enabled AND auto-farm is on**. Now I
    have a rogue circle in the farm zone while I'm selling in the shop with auto-farm off.
13. **Cannot kill party members even with PvP on.**
    - ⚠ CLARIFIED by the owner 2026-07-31, after this was first read as a bug: this is the behaviour he
      **wants kept and enforced**. *"I want to NOT be able to kill a party player — in a mass fight,
      accidentally tapping a party member means you are helping the enemy."* The tap-to-target /
      tap-again-to-attack rule applies to everything, **but on a party member the second tap starts to
      FOLLOW**, since you can't attack/PvP/PK them.
14. **Jewels should be like helmet/gloves — designated slots.**
    - now I equip jewels in a list, and I can try to equip a 3rd ring and it tells me that I can't
    - I want **two ring slots, two earring slots, one necklace slot**
    - when I equip a glove it replaces the one I'm wearing — I want jewels to do the same (pendant is a
      single slot, same logic)
    - equipping a ring/earring **switches the weaker one first**:
      - if both equipped are the same (2× common, 2× uncommon, 2× rare) → switch **slot 1**
      - if one equipped is weaker than the other → switch the **weaker** one
      - ordering: `no slot < common < uncommon < … < mythic`
      - worked example: no rings → 1st common goes to slot 1 (both slots same) → 2nd common goes to
        slot 2 (free/weaker) → a rare goes to slot 1 (both same weight) → an uncommon goes to slot 2
        (rare > common) → another uncommon replaces slot 2 again (rare > uncommon)

## Bigger changes

### 1. Auto-farm: cyclic and first-available skill order

- **cyclic**
  - when auto-farm is on, skills are executed 1-2-3 (skipping buffs/debuffs/heals)
  - even when skill 1 is ready, it does not go back to 1 until the last skill has been used
  - use 1 → use 2 if available (not on cooldown, not a heal/buff/debuff) → 3 → 4 → … → 1 → repeat
- **skill order (first available)**
  - skills are executed 1-2-1-3-1-4-1-2 (skipping buffs/debuffs/heals)
  - when skill 1 is available it is next in line
  - use 1 → 2 if available → 1 if available → 2 if available → 3 if available → 1 → 2 → 1 → 2 → 3 → …
- **heals**
  - there should be a **healing threshold %** below which the auto-healing skills become active
  - depending on cyclic/cooldown they are executed with the same logic, only when HP is below threshold
  - when HP drops below the %, it waits for the current cast to finish, then the healing chain starts;
    when HP is back over the %, normal skill execution resumes
- **buffs/debuffs**
  - if any buffs or debuffs are on the bar, the chain is always active (after the healing one),
    dependent on cyclic/cooldown
  - a **buff** (castable on self) fires if the same buff effect on the character is
    **not active / below 60s / a lesser effect**
  - a **debuff** (castable on the enemy) fires if the same debuff effect on the enemy is
    **not active / a lesser effect**
- **priority order: Heals → Buffs/Debuffs → Attack skills**
- there should be a checkbox/toggle for **AssistPartyLeader**
  - when on, you only assist — if the party leader has no target you wait; don't choose on your own
- **healers and buffers should be actively played** to keep the party alive and buffed
  - you cannot have an alt-bot buffer and an alt-bot healer on auto-farm always auto-buffing/healing you
  - your main damage dealer that auto-farms with 2 alt chars needs to "alt+tab" to buff/heal himself
  - the only auto-help: if the healer sets his threshold to 100% he always heals on cooldown → that
    activates the party heals on cooldown/custom, and he has party buffs

### 2. Buff potions and buff scrolls

- Buff potions now **stack with the current buffs**, making characters stronger than intended.
- I want buff potions/scrolls to be a **split buff**:
  - example, the cleric's speed buff:
    `L1 – 20 speed, 15% cast; L2 – 33 speed, 23% cast; L3 – 33 speed, 23% cast, 2 eva;`
    `L4 (not yet written for warchanter) – 33 speed, 30% cast, 2 eva, 15% AS;`
    `L5 – 33 speed, 30% cast, 4 eva, 23% AS; L6 – 33 speed, 30% cast, 4 eva, 33% AS`
  - the cleric's/warchanter's buff is an **improved buff that groups several buffs**
  - the potion/scroll buffs are **single buffs**:
    - **swift** — C 15 ms, U 20 ms, R 33 ms · Potion: 20 min duration, 1s cooldown, instant cast ·
      Scroll: 1h duration, 1s cooldown, 1s cast
    - **force** — C 15% cast, U 23% cast, R 30% cast · same potion/scroll terms
    - **agility** — C 1 eva, U 2 eva, R 4 eva · same potion/scroll terms
    - **haste** — C 15% AS, U 23% AS, R 33% AS · same potion/scroll terms
  - **potions AND scrolls** available for: Attack (pAtk), Defence (pDef), Magic-Attack (mAtk),
    Magic-Defence (mDef)
  - **scroll only**: Health (maxHP), Mana (maxMP), Health-Regeneration (hpRegen),
    Mana-Regeneration (mpRegen), Critical (pCritRate), Critical-Damage (pCritDmg),
    Magic-Critical (mCritRate), Frenzy (the full frenzy buff — −hp/mp +pAtk/mAtk etc.)
  - potions are **less duration, lower quality** — basic buffs can be covered by potions
  - scrolls are **longer duration, higher quality** — basic buffs start from **common** (if they have a
    potion analogue); scroll-only buffs start from **epic** (where they have no potion analogue)
    - e.g. the Health scroll (the body buff at L6 gives 35% max HP + other stuff — estimate, not exact,
      but 6 levels: 10, 15, 20, 25, 30, 35%): the scroll at **Mythic** gives 35%, **Legendary** 25,
      **Epic** 15
  - you have the max levels of buffs and I gave the list of what scrolls/potions can be — so estimate
    the buffs you don't have levels for
  - the pAtk/pDef is 8, 12, 15%; the mDef is 10, 20, 30% — something like that
- the current **Swift potion is renamed to Dash potion**:
  - **Dash potion** — C 15 ms, U 30 ms, R 45 ms, E 50 ms, L 55 ms, M 60 ms ·
    Potion: **15s duration, 1 min cooldown, instant cast** · **no scroll of that type**

### 3. The drop group idea needs to change

- I still want groups, but **inside the group the next roll should not be for a rarity, but directly
  for the drop**.
  - you roll for the group *armor*
    - then inside there is a standard drop list that you roll for
    - all the common armors are at 5% and uncommon at 2% — is there a way to have 10 items in a list all
      at the same % and, when you roll 0.048 (more than 2%, less than 5%), select one of the Commons,
      because all are within that range?
- In a way I want to **simplify** it.
- If I have a group at 100% and all items inside are at 100%, how do I select only one of them at random?
  - if the roll returns several items, roll again?
- That way I will be able to make a specific item drop less despite its rarity — like the Scroll of
  Resurrect — and will have better control over it per mob. (I start to understand why each entity is fixed.)
- I want the **potions/scrolls group to be more controlled**.
- Same for the **always** group — I can make the common health potion rate decrease, but if we make
  another health potion that is instant and it's common, I will not want it dropped the same as a
  normal HoT.

## Questions

- **What happened to the free teleport for levels < 40?**


---

<a id="playtest-14"></a>

# ══ Playtest-14 ══

# Playtest-14 — owner's report (2026-07-30, after 0.33.1)

Verbatim, as given. Reached level ~25 with **3kk gold purely from selling trash** — the headline
finding: the economy is a faucet with no drain, driven by drop rate × sell price.

---

## Items (the economy / drop batch)

1. **Training weapons still have no M.Atk** — and audit for any other item in the same state.

2. **Lower the SELL price of weapons/armor.** At level 25, 3kk gold from selling trash alone
   (Common at ~20 % drop rate × ~20k sell price). Lower it **at least 3×**.

3. **Lower the drop chances.** Now roughly 20 / 12 / 5. Target:
   - **Normal monsters** — C 5 %, U 2 %, R 0.2 %, E 0.01 % (below level 74 also drop a recipe at 0.1 %)
   - **Elite / dungeon / instance** — U 10 %, R 2 %, E 0.2 %, recipe 0.1 %
   - **Boss** — E 70 %, L 40 %, M 2 %, armor recipe 50 %, weapon recipe 40 %, jewel 60 %

4. **Drops must be GROUPED and grade-locked.**
   - **Grade lock:** a mob drops only ITS OWN grade. A level-40 mob drops D-grade recipe / armor /
     weapon — never E or C.
   - **Groups:** armor · accessories · weapons · jewels · crafting mats · recipes ·
     scrolls+buff-pots · always · gold.
   - Without groups you can get 20 light armors off one lucky kill.
   - Each group has a **trigger chance**; on a trigger it rolls a **rarity**; on a rarity hit it
     **randomises which item of that slot family** at the mob's grade+rarity.
   - Percentages below are examples for a NORMAL mob (the group chance and the inner rarity chance
     multiply out to the target in §3):

   | Group | Trigger | Inner rarity roll | Randomise among |
   |---|---|---|---|
   | Armor | 50 % | C 10 · U 4 · R 0.4 · E 0.02 | Light / Heavy / Robe |
   | Accessories | 50 % | C 10 · U 4 · R 0.4 · E 0.02 | Helmet / Boots / Gloves / Shield |
   | Weapons | 33 % | C 15 · U 6 · R 0.6 · E 0.03 | blade / fangs / longbow / wand / … |
   | Jewels | 100 % | C 5 · U 2 · R 0.2 · E 0.01 | Ring / Earring / Necklace |
   | Mats | 100 % | wood/iron/… — 50 % → 1, 40 % → 2, 9 % → 4, 1 % → 10 | **rarity = the AMOUNT** |
   | Scrolls | 100 % | C 40 · U 20 · R 10 | a buff potion (not healing) or a scroll of the grade |
   | Always | 100 % | <75: C 70 · U 30 · 75+: C 55 · U 40 · R 5 | health potion / escape / resurrect; 75+ adds rare pot + Ultimate escape/res |
   | Gold | always rolls (70 % or whatever it is now) | — | **its own group** — inside "Always" it would compete and you could never get both |

   > Mats note (owner): either roll the material and let rarity BE the amount, or roll the material
   > then roll the amount separately — whichever the current code makes easier.

## General

1. **Make `/givegold` and every other admin command work on the phone.**
2. **Visual for skill cooldowns.**
3. **Passives still not re-worded** — they show a brief description, not the actual stats.
4. **Add the exp / SP / gold chat row.**
5. **Increase the mob spawn limit** — more mobs, at least the ones quests need.
   - Give Werewolves / Ashen Wolves / Ork Archers / Grunts (etc.) **their own spawner** on top of
     the one they currently spawn at, and **remove them from that one**.
   - Then killing a Werewolf respawns a Werewolf in 30 s — not a Skeleton. Today you kill 50 mobs
     just to make the quest target reappear.
   - Once that holds, quest requirements can go up (15 archers, not 5) — **later**, with the quest
     rework, not now.

## Not working

1. **The Abandon button does nothing** except show its confirmation.
2. **Char-select is still briefly stale** — level and class update only after a delay.


---

<a id="playtest-13"></a>

# ══ Playtest-13 ══

# Playtest 13 — three sessions (VPN, phone-hosted server, quests)

**Date received:** 2026-07-29 · **Build under test:** ~0.28.90 (0.28.86–0.28.90 area)
**Status:** raw owner report, captured verbatim below. Triage lives in
[the queue section](#triage--queue) and in memory `playtest-13-queue`.

This is the AUTHORITATIVE list. Nothing here is scheduled until it appears in
[../RoadmapNext.md](../RoadmapNext.md).

---

## Owner's report (verbatim)

# setup and sessions
## first session
- over the vpn - like normal
- mage class -> elf mage -> lvl ~15 or so x1 exp
## second session
- I made the server at Dotnet 10
- I installed termux + proot-ubuntu in my android phone (thats why i needed the .net10)
- ran server there it isntalled Db all ran
- ran the client all worked
- archer class (human -> fighter -> marksman) 0 - 15 x1 exp -> 15-24 x5 exp
## third session
- champion class - 0-28 with +1 lvlup and x5 exp
- had to do quests
## conclusion
- i checked what i could
- it was fun playing
    * still plain (no sounds, a bit woody, no good visuals)
    * but a game that i enjoyed
    * still need alot of work though
    * and my play sessions were 2-3 for about 30 mins
# questions
- does your SmokeTest while listening, "botting" consumes large amount of tokens ?
    * because my weekly tokens were gone for about 3.5 days
    * or we just build a lot of stuff last session ?
- do you keep my wishes in different files as playtests ? I have the feeling some of my prompts-with ideas are lost
- i feel archers missing their 20-40 skills ? - do we only made 20-40 for cleric and no other ? - I saw that the chamipn have its skills 20-40
- i noticed that mobs drop com/uncom/rare same types as F/E (lesser) equip
    * saw that the lessers are better than the common but worse than uncommon/rare
    * what are those common uncommon ?
    * shouldnt the (lesser) in shops be the common onse ? - and remove/switch the lesser
# Bugs
- cannot cancel buffs with double click
    * almost impossible
    * make it hold not double click - the pop-up prevents me
    * need to spam taps to cancel - and then i cancel more than one that i needed
- sp don't update - when I buy skill relog (Even don't update in stats window) .
- when entering char select my lvl is not updated - and only lvl 7 don't unlock skills to learn (needed relog) the 14 unlocked them
- the char selection the text for class is not updateing -> admin stays Human Mage, the marskeman/champion stay as human fighter
- Entering with Admin with 30d shot + buffs then switching to other player in the acc the buffs are there...
    * Don't think they are active just visually there..
    * Maybe client don't clear buffs list because when I use potion the buffs disappears and only potion effect stays
- quest giver don't update when I accept the quest
    * i accept the quest and need to re-talk to say what to kill
- Crafting mats acts like a single item a stack of x11 is threted as single item - sold as one (no quantity selction) - stored as one row in keeper
    * scrolls and potions are sold with the numpad selection
- ranking system didnt update
- when opening items details for 1st time the Atk stat of the item is hidden below the tile bar - next when i reop it it goes one row down and shows ok
- The mob info stats window is cut - it's like the box with the text is aligned center and the upper halve goes negative top (same for the weapons just there are 3 rows and its visible)
# works
- buy back works
# need change or fix or not build
## buffs
- buffs should only disappear when their time is gone or are canceled(by debuff or double/hold or subclass change) now they dissappear with everything - relog, class change, some other change etc..
## vendors
- you can sell scrolls/pots with numpad quantity selection but not the crafting mats (they act as single item)
- split armsmaster to two vendors - wepons/armors
- vendor while buying from him there should be a description of the item -> clicking on the item opens confirmation dialog with the items description.
- vendors need better visual .. now big list no differnece + no description -> i hve no idea which is which
    * need wrap panel with item squares with tooltip the description and clicking on it opens the description/confirmation dialog
    * a button to switch between list-rows (each item row have two rows 1. name, 2. the description) - clicking again opens the confirmation
- keeper need some order inside no plain list
    * tabs or groups with different item types -> main [put/witraw] then in each [equip/consumables/crafting] - can be groups like the skills menu or tabs
    * i think the crafting mats are a bug - they are stored separate rows not stacking
## weapons
- all old gear + training one have no mAtk value - need to recheck all weapons
## quests
- some quests must have lvl range - from->to - outside these ranges u cannot accept it - class quests have no upper limit (u need your job)
- I want the apotichary to have a daily quest (no kills just accept - finish) that gives [shot 1h selection box - untradable] -> once per day, reset server time, from lvl 6 to lvl 75
- the soulshot boxes given from quest must be untradable but the one u buy should not be. - not sure how is now
- make only the name of the quest in npcs lists then clicking on it there opens a window that shows details/description and u have accept and decline buttons (now is one big text without explanation)
- in class master vail same just in details of the quest it will tell you the quests you need so u can accept/change the class.
- we need [abanden] quest button inside the quest window menu on active quests when you one the full quest details there should be abandon with confirmation that the whole progress will be lost and if outside the quest range cannot retake
- we need repeatable quests
    * quests that u take and tell you to do stuff
        + can be kill mobs indefinetily (gatharing quest items as u farm in a specific zone)
            = the quest reward can be normal (can give you gold, exp, item etc) + the amount of quest items u gathered gives gold and exp per each
            = for example you killed 1 mob just to return the quest for the main reward if any -> u take the main reward if any + 1*QuestItemRewardModifier*Exp +1*QuestItemRewardModifier*Gold
            = or you farm for an hour (u deside to lvl up and killed 20 sceletons and 55 bears) -> u take the main reward if any + 20*QuestItemRewardModifier(sceletons)*Exp +20*QuestItemRewardModifier(sceletons)*Gold + 55*QuestItemRewardModifier(bears)*Exp +55*QuestItemRewardModifier(bears)*Gold
            =  can be taken again - if not daily limited
        + or finite -> kill 10 of those 50 of those - gets reward at the end - normal quest just dont close on finish - can be taken again - if not daily limited
        + or talk with this/that  - gets reward at the end - normal quest just dont close on finish - can be taken again - if not daily limited
        + usually no daily limited repeated quests will have no main reward and be only farm quests - just for additional gold/exp
## class/skills
- all skills and passive should show the desctiption with numbers \
    * exsample (Increases mana regenaration and later levels increases evasion -lvl_1-> mana regeneration: +20% -lvl_3-> Mana regeneration: +20%, Evasion: +3 -lvl_X> Mana regeneration: +20%, Evasion: +3; Light Armor: Evasion +3 (ppl will notice is when wearing light armor is additional +3 on top of the 1st +3))
## items/equipment
- each item should have a description that shows when it's details window is open. -> examples:
    * [Shots 1d Selection Box - Untradable] A box containing Spirit(Magic) or Soul(Physical) shot that increases attack
    * [Soulshot Rune 1d Box - Untradable] A box containign Soulshot rune for 1 day - Timer starts after opening the box, timer works even offline
    * [Soulshot Rune 1d - Untradable] Rune that doubles PHYSICAL damage - Expires: xxxx or Timeleft: 24h
    * the Untradable can be otside the name just to be noticable
    * if the box is tradable the items it gives can be tradable or untradable - tradable box = untradable active rune
    * if the box is untradable the items can also be tradable or untradable - weapon selection box - untradable (maybe boss drop) = tradable weapon
- can we make the reariry not as the name ?
    * Common Electrium Longbow -> Electrium Longbow (Common) [Description:"Name: Electrium Longbow //n Grade: E //n Rearity: Common //n Type: Bow (2h) //n Attack: ....."]
    * Common Electrium Robe -> Electrium Robe (Common) [Description:"Name: Electrium Robe //n Grade: E //n Rearity: Common //n Type: Robe //n Defence: +def //n MP: +mp ....."]
- equipment in shop way to lo price
   * the only grades in shop must be F,E,D (lesser) - everything else is crafted/droped
   * armors  Gloves/Boots(F-6k,E-175k,D-600k), Helmet/Shield(F-10k,E-250k,D-1kk), Armor(F-18k,E-400k,D-1.8kk) -> main armorms give most defence while boots/gloves gives less
   * weapons twoHanded(F - 30k, E-750k, d-3kk), OneHanded(F - 27k, E-670k, d-2.7kk) -> 1h weapons are cheaper because they give less atack and u need to buy a shield ~1/3 of the shield price is saved
   * jewels  Rings(F-3k,E-70k,D-250k), Earrings(F-6k,E-140k,D-500k), Neck(F-12k,E-280k,D-1.5kk),
   * remoive "ash" and whatever low equipment we have that isnt the csv (sets/top weapons) or The last we build (lessers) - Darksteel,Cobalt etc..
## mobs
- mobs dont have cast bar - i thought we updated it
- all mobs are agressive in 10+zones
    * the only zones where mobs must be ALL aggressive are the dungeons/instances and the raid boss zones (except Bosses)
    * in other zones let say 20+ make one type agressive not all - now in 22-28lvl zone a 22 lvl champion getting ganked my magic monsers and few meles equals death
- also make zones closer to lvl 16-22, 22-28 are a bit harsh
    * the fields should look like -> 1-4,4-8,8-12,12-16,16-20 ... 68-72,72-75 ... 76-77, 78-79, 80-81 .... 88-89, 90 as level ranges for spawners
    * the spawners need to be in a fields something like that
        -> 1-16  are in the starting city with 1-12 and 8-16 in two field
        -> 16-40 can be different city with 3-5 field
        -> 40-60 3rd city with 3-5 fields
        -> 60-75 4th city again with 3-5 fields
        -> 76-85 5th city with 76-80 in one field + 1 elite 80 spawner, 81-84 second field + one 84lvl elites spawner, 85-90 in third field + elite 90lvl spawner
    * the different level ranges spawners in a fields need to have a distance one from another (elit spawners need to be closer to their non elit spawner but far enough not to agro -> 1-1.5k range?)
    * each city to be the same as the starting one (a bit smaller) to havethe vendors/keeper/gatekeeper - and each gatekeepre to teleport you to their own fields + the other cities
    * the dungeon gate can be in the city for its level
## general
- make the log for sql commands/selects not to show in console it's overflooding it - just important information
- when setting to keep position in auto farm the circle should stay in the place not continue going with character
- The debug class change buttons (2nd) changes craft class - ScrollScribe
    * I need the button that selects my current 2nd/3rd prof
    * or the class master not to require the 2 finished quests when admin
    * or the [compleate] buttons not to check the mobs/items
    * whichever is easier
## not build
- char select need [delete] button
- chat tabs still not build
- target visual - targeting mob not onlt target window - the actual mob needs indication as well
- auto farm targeting - auto farm dont show which mob its fighting
- quest giver need indication for new quest
- need info or an active quest what to kill or do - the quest windows (menu->quests) should show active/unavailable/compleated
    * active - as now - each row in this tab bust have [track] button that shows persistant on screen movable popup to track kills or who to speak to - limit to 3-5 tracked quests
    * unavailable - lvl to high, lvl to low, not compatable for class/race etc (not compatables can be hidden)
    * compleated - all copleated quests
    * each row in each tab must have [details] button to show information about the quest/description - who gave ti what u had to do each step etc ....
- add a asystem row after each kill -> "Exp: +eee, SP: +sss, Gold +ggg"
# need check
- the def of mage is 360 at lvl ~15 and mobs same lvl do 2-3dmg - need to check formulas - witout buffs is 257 and get 16 dmg on 110 life (okish) - hope when we make appropriate npc buffs for these levels this will clean itself
- warn: Microsoft.EntityFrameworkCore.Query[10103] The query uses the 'First'/'FirstOrDefault' operator without 'OrderBy' and filter operators. This may lead to unpredictable results
- at some point in auto farm i stopped regenerationg MP i thought it was after i died/respawned but then when i stoped the auto farm to escape the mob it start to regen again

---

## Triage — queue

Ordered by "breaks the session" → "annoys" → "wishlist". Nothing is started until it is
pulled into `RoadmapNext.md`.

### Tier 1 — bugs that corrupt or block (fix first)
1. **Buffs cleared on relog / class change / "some other change"** — buffs must only end on
   expiry, dispel, cancel or subclass swap. Server-side persistence + stop the client wiping
   its list. (Also explains the "Admin's buffs still visible on the next character" report —
   the client's buff list is not cleared on character switch.)
2. **SP not updated after buying a skill** (stale until relog, wrong in the stats window too).
3. **Char-select is stale** — level and class text never refresh (`admin` shows *Human Mage*,
   marksman/champion show *Human Fighter*); at level 7 the Learn list stayed locked until relog.
4. **Crafting mats do not stack** — treated as single items: no numpad on sale, one row per unit
   in the warehouse. Scrolls/potions are fine, so it is a per-item stackable flag.
5. **Ranking system did not update.**
6. **MP regen stops during auto-farm** (resumed after stopping the auto-farm; suspected around a
   death/respawn but not confirmed).
7. **Quest giver dialog does not refresh on accept** — must re-talk to see the objective.
8. **Buff cancel is double-click** — impossible on a phone (the pop-up eats the taps, and spam
   cancels neighbours). Change to press-and-hold.

### Tier 2 — UI defects
- Item details: the Atk row is hidden under the title bar on first open, correct on reopen.
- Mob info window is clipped — the text box is centred and its top half goes off-screen
  (same bug in the weapon window, visible there because it has three rows).
- SQL/EF logging floods the server console — quiet it down to important lines only.
- EF warning `10103` (`First`/`FirstOrDefault` without `OrderBy`).
- Auto-farm "keep position" circle follows the character instead of staying put.
- Debug 2nd-class button picks the crafting class (ScrollScribe) — needs to grant the *current*
  path's 2nd/3rd profession, or let admins bypass the class-master quest requirement.

### Tier 3 — systems to change
- **Vendors**: split Armsmaster into weapons + armor; item descriptions at buy time; a confirm
  dialog carrying the description; grid-of-squares view with a toggle to a two-line list view.
- **Warehouse**: group/tab by type (equip / consumables / crafting) under put/withdraw.
- **Shop prices + grades**: only F/E/D sold; the price table in the report replaces current
  prices; delete leftover low gear that is neither the CSV sets nor the "lesser" line ("ash",
  Darksteel, Cobalt …).
- **Rarity out of the name**: `Common Electrium Longbow` → `Electrium Longbow (Common)`, with a
  structured description block (Name / Grade / Rarity / Type / stats).
- **Item descriptions** for every item, with tradable/untradable shown outside the name, and the
  box-vs-contents tradability rules from the report.
- **Skill descriptions with numbers**, per learned level, including conditional lines
  (e.g. light-armor-only bonuses).
- **Weapons**: old gear + training weapons have no M.Atk — audit every weapon.
- **Quests**: level ranges (from→to; class quests have no upper bound), abandon button with a
  confirmation, per-quest detail window with accept/decline instead of one wall of text, class
  master listing the required quests, a daily apothecary quest granting a 1h shot box
  (untradable, level 6-75, server-time reset), quest-granted shot boxes untradable while bought
  ones are not, and a full **repeatable-quest** system (endless gathering with per-item exp/gold
  multipliers, finite repeatables, talk-to repeatables).
- **Quest window rework**: active / unavailable / completed tabs, [track] with an on-screen
  movable tracker (3-5 max), [details] on every row.
- **Mobs**: no cast bar; blanket aggression above zone level 10 must go — all-aggro only in
  dungeons/instances and boss zones, elsewhere one aggressive type per zone.
- **World re-layout**: narrower level bands (4-level fields low, tighter at the top), fields
  grouped per city (1-16 start city, 16-40, 40-60, 60-75, 76-90 with elite spawners), spacing
  between bands, every city gets vendors/keeper/gatekeeper, gatekeepers link their own fields
  plus the other cities, dungeon gate in the matching city.

### Tier 4 — not built yet
- Character delete button on char-select.
- Chat tabs.
- Target visual on the mob itself (not just the target window).
- Auto-farm should show which mob it is fighting.
- New-quest indicator over quest givers.
- Kill summary chat line: `Exp: +eee, SP: +sss, Gold +ggg`.

### Needs investigation
- Mage defence 360 at level ~15 vs same-level mobs doing 2-3 damage (257 unbuffed → 16 damage on
  110 HP, which reads fine) — check whether low-level buffs are simply overshooting.


---

<a id="playtest-02876"></a>

# ══ Playtest-0.28.76 ══

# Playtest 0.28.76 — phone checklist

Edit this on the phone as you go. For each line: change `[ ]` to `[x]` OK / `[!]` broken /
`[?]` unsure, and add a note after `>>`. Anything with a note comes back to Claude.

**Setup**
- Server + bot **Test2** (Lv 1, in town) are already up on my PC.
- Get the APK: `http://10.2.2.33:5238/apk`  → install over the old one.
- In the app, server URL: `http://10.2.2.33:5238/game`  (or `127.0.0.1` with `adb reverse`).
- Log in **admin / admin** (or test1..9 / test). Debug menu is admin-only.

---

## A. Bugs that were fixed — confirm they're gone
- [ ] **Skills → Learn** does something now: a locked/too-poor skill SAYS WHY (level/SP/gold), not a dead button.  >>
- [ ] Learning a skill you CAN afford opens the confirm and works.  >>
- [ ] **Soft keyboard LIFTS** the command bar instead of covering it.  >>
- [ ] **`[Lead]`** (party): passing lead moves the crown ★ and the button to the new leader.  >>
- [ ] `/jail test2` then `/tp test2` puts you in the **jail**, not a dungeon.  >>
- [ ] In the dungeon (gatekeeper → Hollow Crypt): mobs **aggro + fight back**, and are **spread out**, not clumped.  >>
- [ ] Non-friend log-in does NOT show "X entered the world" (only mutual friends do).  >>
- [ ] A non-admin character in the admin account can NOT use admin commands.  >>

## B. Tier-2 UI — 13 items
- [ ] Entering a town: only ONE banner, no leftover blue line under it.  >>
- [ ] The "You entered <field>" banner does NOT block tapping the ground under it.  >>
- [ ] Target a PLAYER: no Attack/Follow/Assist/Party/Trade buttons on the frame. Target a MOB: Attack + Info only.  >>
- [ ] Debug menu: taking 10 potions does NOT spam 10 chat lines (items/levels/buffs are silent; tp/karma/class still talk).  >>
- [ ] A 24h/30d shot rune buff reads like **29d**, not 719h59.  >>
- [ ] Bag: **Equip** button is FIRST; tapping it expands the paper-doll on the LEFT, list slides right.  >>
- [ ] Sit ≥3s then stand = INSTANT. Sit briefly / get hit then stand = short delay.  >>
- [ ] Drink a HoT potion while DAMAGED: a mint **"+N hot"** floats up each second.  >>
- [ ] Target frame shows HP as **numbers** (cur/max); a PLAYER target also shows an MP bar.  >>
- [ ] Stats window + mob Info show attack/cast speed as **1234 / 1500 (x3.7)**, not a bare x1.1.  >>
- [ ] Buff icon: SINGLE tap = details popup (tap outside closes). DOUBLE tap = cancels it. Debuffs won't cancel.  >>
- [ ] Party window: buffs/debuffs are little **squares** to the right of each member; window is shorter.  >>
- [ ] Party leader: **Loot** is a DROP-DOWN (tap to open, pick a mode), not a cycling button.  >>
- [ ] **World border**: an orange dashed line at the map edge; you stop at it (still rubber-bands for now).  >>

## C. New action buttons (Skills → Actions tab, drag to bar)
- [ ] The Actions tab lists: Add Friend, Remove Friend, Friend List, Leave Party, Kick, Pass Leadership (plus the old 8).  >>
- [ ] Add Friend / Kick / Pass Lead work off the TARGET (no typing). Friend List / Leave Party need no target.  >>
- [ ] Each can be dragged to the skill bar and used from there.  >>

## D. EXP / kill rewards (use the bot Test2 for party)
- [ ] Kill mobs solo: XP bar moves; kill count to level feels like a real MMO (slow), not one-shot-per-level.  >>
- [ ] Party with the bot (invite Test2), kill together: you BOTH get exp; the share feels right.  >>
- [ ] A far-off-level party member (bot is Lv1, you're higher) gets little/nothing; you still get yours.  >>
- [ ] Kill reward VARIES a bit per mob (±20% randomness), not a fixed number every time.  >>
- [ ] Drops still land; most-damage gets the loot.  >>

## E. Starter gear + the level-10 quest
- [ ] A BRAND-NEW character (make one) starts with **Training** gear (weak), a weapon-choice box + armor-choice box. No jewels, no shots.  >>
- [ ] Training weapons/armor buyable at the Armsmaster for 400g; broken jewels there too (40/30/60g).  >>
- [ ] Broken jewels DROP off low (Lv1-5) mobs.  >>
- [ ] At Lv10 the Armsmaster (Dolan) offers **"A Proper Kit"**; finishing it gives the Newbie armor + weapon boxes.  >>
- [ ] Then **"Blooded"** (kill werewolves + reach Lv15) gives the jewels box + a 1-day shot rune.  >>

## F. Weapons show M.Atk now
- [ ] A fighter weapon's card shows BOTH P.Atk and M.Atk (e.g. sword ~92 / 54).  >>
- [ ] A wand/staff still out-casts a sword: swap a caster from wand to mace → M.Atk drops sharply.  >>

---

## Free notes / anything else
>>
>>
>>


---

<a id="legacy-testchecklist"></a>

# ══ Legacy TestChecklist (pre-Unity-only, 2026-07) ══

> Superseded by TestChecklist.Unity.md. Kept verbatim for the design decisions recorded in it.

# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs a change/tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## 🧪 PLAYTEST-7 FIX BATCH — BUILT 2026-07-20. ⚠ **DELETE `Game.Server/game.db`** (per-character Role
## + ChatBannedUntilUtc). Build 0/0, SmokeTest ALL GREEN (incl. new ghost-entity / jail / mutual-friend
## regression checks). Restart server + client.

Server-side behaviour is SmokeTest-verified; everything visual needs your eyes.

**Design (added 2026-07-20, after the batch):**
- [ ] **Level is PRIVATE.** You see `Lv` on your OWN nameplate and on MOBS. Another player's nameplate,
      target frame and expand (▼) show **no level** — party members included (their level is in the
      **party window**, which is the one place it's shared). Enforced server-side: the number isn't
      even sent for other players, so it can't be read by a modified client.

- [ ] **Regen ticks every 3 seconds** (was 1s) in bigger chunks — L2's cadence. Healing SPEED is
      unchanged by the cadence itself; only the chunkiness. **CON now drives HP regen hard**: at lvl 40,
      CON 20 → 11 HP/tick, CON 40 → 21, CON 60 → 37 (CON 40 is deliberately identical to before).
      Tune live in **Debug Tuning**: *Regen tick (sec)*, *CON regen ×/point*, *MEN regen ×/point*.
- [ ] **SPT (Spirit) is a real stat now** — the stat window shows **CON / ATK / WIT / DEX / SPT**.
      It drives Max MP, MP regen and M.Def. Fighters' numbers are UNCHANGED (25/26/27); ork mage +3%,
      **elf mage −8%** vs before. ⚠ **schema change** — the DB rebuilds itself on first run (DEBUG).
- [ ] **SPT survives a subclass swap and a relogin** — it persists per-subclass like CON/ATK/WIT/DEX.
      Swap class, swap back, relog: SPT and Max MP must be right. (SmokeTest territory — run it.)
- [ ] **±Spirit swap passives now grant ±1 SPT per level** (not ±10% bundles). The stat window's SPT
      number must MOVE when one is learned, and Max MP / M.Def / MP regen with it. Worth ~+5-7% at
      level 5, not the old flat +10%.
- [ ] **Stat tooltips** — hovering the CON/ATK/WIT/DEX/SPT row (label OR number) explains what each
      stat buys. Also on the Max HP/MP row.
- [ ] **DoTs still tick every 1 second** — bleed/poison/venom, HoTs and the party window must be
      UNAFFECTED by the regen cadence (they used to share the same timer).

**Bugs:**
- [ ] **Enter focuses the chat input** (and un-hides the chat window if hidden), and **clicking anywhere
      outside the chat removes focus** so keys go back to the game.
- [ ] **No duplicate abbreviations anywhere** — skill bar + buff bar, across ALL skills AND consumables.
      hop/hotw/hotm → HOP/HOW/HOM; the two scrolls → URet/URes (were US/SO). Startup guard against collisions.
- [ ] **Ultimate Resurrection scroll casts from the SKILL BAR** (it wrongly demanded a friendly dead target;
      from the bag it worked). Potions + Return from the bar were already fine.
- [ ] **You can MOVE inside the jail cell** — chat/whisper/skills/items/escape all still blocked.
- [ ] **Sentence ends → teleport to the STARTING town** (not the nearest — the jail's location stays secret).
- [ ] **Admins are immune** to `/jail` `/kick` `/ban` (incl. on themselves).
- [ ] **Name lookup is case-insensitive** and no longer prints "character with name X cannot be found"
      while actually performing the action.
- [ ] **Kick/ban is INSTANT and removes the entity.** No ghost left standing (was targetable/killable/
      buffable 30min later, and blocked re-login with "character is already online"). The victim gets no
      confirm dialog: dropped to the login page first, THEN a "you have been kicked/banned" modal → OK.
- [ ] **God mode shows a persistent indicator** (currently only discoverable by re-issuing `/god`).
- [ ] **Bosses are not aggressive** (the Treant showed `*`); and the `*` has a **space** before it.
- [ ] **Mob card row order is stable** across expand/collapse (P.Atk/P.Def were swapping).

**New commands / role:**
- [ ] **Moderator role** (name TBD) — can ONLY `/jail`, `/kick`, `/chatban`. **Admin > Moderator > Player**:
      moderators can't moderate each other, only an Admin can act on them.
- [ ] **Roles are PER-CHARACTER, not per-account** — one account can hold an admin character alongside
      ordinary ones. (Ban stays per-account; jail/kick/chatban are per-character.)
- [ ] `/role <name> <player|moderator|admin>` — admin-only grant/revoke, works on offline characters.
- [ ] `/chatban <name> [min]` — blocks chat only (same block as jail's).
- [ ] `/spd <m|a|c> <v>` (admin, uncapped) · bare `/spd` resets all three. ⚠ Renamed 2026-08-07 from
      `/speed-cast|atack|move|reset`, which no longer exist.
- [ ] `/bag <name>` — admin views a player's inventory and can remove items.
- [ ] `/give <name>` — admin picks from own inventory, transfers with quantity + enchant; ignores tradability.
- [ ] `/givegold <name> <amount>` — negative subtracts; `k`/`m`/`b`/`t` suffixes and `1_002_003_004_005`
      underscores both parse.

**Design changes:**
- [ ] **Friends are MUTUAL.** `/fadd` invites (silently — the other side isn't notified). Until they add you
      back, `/flist` shows **[Pending]** and no online state. Once mutual: [Online]/[Offline] tags +
      "X is now Online/Offline".
- [ ] **Party-window debuffs use icons/abbreviations + tooltip**, never the full skill name.
- [ ] **Player expand (▼) shows class only — NOT level** (level is intel to withhold from enemies).
      Later: title / clan rank / clan name.
- [ ] **Mob expand → movable POPUP** with two tabs, **Details** (default) and **Drop**, styled like the
      player stats window. The target window keeps only its two summary rows; everything else lives there.

---

## ✅ ADMIN + SOCIAL (2026-07-17/18) — TESTED 2026-07-20 → see PLAYTEST-7 above
Jail/unjail, `/tp`, `/god`, target `[...]` menu, Follow all work. Kick/ban ghost-entity bug, admin
self-jail, jail movement, mutual friends → PLAYTEST-7.

## ✅ NETWORK: DELTA SNAPSHOTS (2026-07-17) — VERIFIED 2026-07-20
Entities spawn/move/despawn cleanly, own vitals + death fine, walk-away/walk-back clean.

## NETWORK: RESYNC (2026-07-21) — server side of a Unity client bug, but it affects BOTH clients
New hub method **`RequestResync`** → `ResyncCmd` → drops that connection's entry in `_lastSentByConn`,
so the next tick re-sends every visible entity as a full spawn. Purely additive — no protocol break,
no `GameVersion` bump. It exists because the delta feed previously had **no recovery path at all**: a
client that missed one spawn frame never heard about that entity again (a lean update it can't draw,
or literally nothing if the entity is standing still).
- [ ] WPF is unaffected — entities still spawn/move/despawn normally after this change.
- [ ] Nothing in the server log complains when a Unity client asks for a resync mid-session.
- [ ] Two clients on one account/zone: one resyncing must not disturb the other's stream.

## ✅ PLAYTEST-6 BATCH (2026-07-17) — VERIFIED 2026-07-20
Res prompt, square buff bar + short times + ≤60s blink, grade-penalty rows, skill emoji icons, party-window
debuffs, player/mob expand split, aggro `*`, instant Ultimate Return, bag `[E]` quick-equip, 5-row movable
skill bar with items on it — all confirmed. Follow-up polish on debuff labels, expand contents, the `*`
spacing and mob card order → PLAYTEST-7 above.

---

## ✅ PLAYTEST-5 FIX BATCH (2026-07-17) — VERIFIED 2026-07-17

**The ghost-corpse bug is FIXED** — die → exit to character select → log back in → you are DEAD, no corpse
left behind, relogs don't stack bodies. Also confirmed: Angel's Protection castable on a party member ·
Angel's 1s cast / 10s reuse both fixed · the grade penalty's two never-expiring debuff rows · the gap
penalty itself · the armor/jewels/shield debuff · **normal play unaffected** · cast bar name-only · res
scrolls 10s reuse · debug "Scrolls (x5)" group · Equipped-tab orange [U] · debug 10s character delete.

- [~] **A resurrected player who logs out logs back in ALIVE** — the *paid-for* case works, but declining
      (or ignoring) an offer and relogging leaves a **stale, dead prompt**. → queued at the top.
- [~] The debuff rows work, but the **`(x…)` in their names should go**. → queued at the top.

---

## ✅ PLAYTEST-4 FIX BATCH (2026-07-17) — VERIFIED 2026-07-17

Owner tested all 11. **Confirmed working:** party EXP ±9 gate · 3rd class gated below 40 · Resurrection
SP-learned · Resurrection 10s base cast · dead players show a real target window · shift-click targets a
dead player · target window movable + ✕ = full Escape · res+respawn ONE window · clicking a skill bar slot
casts it · vendor buy-quantity prompt · Karma CLEAR (all).

Only follow-ups (all BUILT in the playtest-5 batch at the top):
- [~] **Angel's Protection still could not be cast on anyone but me** (dead/alive/party). ROOT CAUSE: it's a
      marker buff with `SkillEffect.None`, and the cast path's ally branch tested Effect bits → self-cast.
      → fixed via `IsAllyTargetable`, + owner wants FixedCast 1s / FixedCooldown 10s.
- [~] **Resurrection scroll reuse 60s → 10s** (owner).

---

## ✅ DEATH XP PENALTY + RESURRECTION + ANGEL'S (2026-07-17) — VERIFIED 2026-07-17

Death XP penalty (40+ loses 5%, below 40 Novice's Grace), cleric/Lightbringer Resurrection levels
(L1@20/L2@40/L3@52/L4@61), the prompt's exp-restore %, **Angel's auto-learn @76**, **buffs survive death
while it's up**, and the **reagent vendor** (Apothecary stocks Skill Stone 400g + Elemental Stone 20k;
Elemental Burst consumes 1) — all confirmed.

Still open from this thread:
- [~] **Resurrection scrolls — cast VERIFIED, reuse was wrong.** Owner saw 10s cast + **1 min** reuse: the
      **10s cast is right and stays**; the 1 min was too long. → reuse dropped to **10s** (BUILT, top batch).
      Owner explicitly doesn't mind recasting immediately vs waiting another 10s, so 10s settles it.
- [ ] *(Groundwork, not castable yet)* Preservation buffs share one slot by priority (Angel's = weakest;
      future tank self-auto-res > healer target-auto-res > Angel's). An `AutoResurrect` flag is in place for
      the future auto-res buffs (nothing uses it yet).

---

## ✅ BATCH (2026-07-16) — VERIFIED 2026-07-17

Subclass "Add a class" fixes (main filtered; gate = level 75 + every owned class at 75 with its 3rd class,
admins exempt) · karma per-kill quadratic curve (≤+10 → 200, +50 and beyond → 15k cap) · party-window
click-to-target · equip unlock (any grade at any level) · discipline unique cross-race · count cap (4 /
admin unlimited) · swap + relog persistence — all confirmed.

Two items from this batch turned out to be broken and are REDONE in the playtest-5 batch at the top:
- [~] **Grade penalty** — the equip unlock worked, but the penalty was SILENT (no display, and it only
      touched weapon ATK / armor DEF), so the owner could not tell whether it applied. → redesigned to the
      gap ladder + full stat set + two visible debuff rows. **Re-test at the top, not here.**
- [~] **Offline-farm death sticks** — same root as the ghost-corpse bug: `DiedWhileAway` was only set for
      offline-farm / link-dead deaths, so an ordinary death + logout logged you back in at full HP, and the
      corpse was orphaned in the world. → **both fixed at the top.**

**Karma / PK / trade + debug menu reorg:** — ✅ **VERIFIED** (4 karma debug buttons; trading blocked while
PK or flagged; a PK can't buy from a vendor but can sell; trade-window contrast; Functions tab grouped Full
buffer · Gold & SP · Level · Karma; Class tab grouped Profession & skills · Classes (subclass) · Reset).
*(The level-33 3rd-class bug this section exposed is fixed + verified — see the playtest-4 header above.)*

**New findings (2026-07-16 playtest):**
- [x] ✅ **Archer "244k M.Atk" — RESOLVED 2026-07-16, NOT a bug.** The char was level **821** (debug over-
      level), not 82. Magic uses `levelMod²`, physical `levelMod¹`; at 821 that's 82.8× vs 9.1×, so the M.Atk
      *stat* balloons. MEASURED in BalanceMatrix (new extreme-level + damage probes): at 821 a MAGE has
      **366k** M.Atk (3× the archer's 112k) — so it's the shared level scaling, not archer-specific. And the
      actual DAMAGE stays balanced at every level (mage nuke 74 / fighter basic 49 / archer basic 104 vs a
      same-level tank) because magic damage takes `√mAtk / mDef` — the giant stat compresses. If anything,
      magic *falls off* at extreme levels (mDef outgrows √mAtk), it doesn't skyrocket. **No fix needed** at
      the real cap (90). If the cap rises to 100-200, re-run the BalanceMatrix damage probe to confirm.

---

## ✅ M.ATK DISPLAY SHRINK (2026-07-16, damage-model work) — VERIFIED 2026-07-17

M.Atk in the stats window is now P.Atk-size (with the cosmic value kept as the "M.Atk (internal / L2-ref)"
debug row), unbuffed magic damage + heals are unchanged, and magic-only M.Atk buffs are HONEST (an authored
+X% gives +X% damage AND +X% on the display) — all confirmed. See docs/design/DamageModel.md.

⚠ **Owner TODO still open:** re-author `BuffMagAtk` buff VALUES to their effective %s, and give an explicit
magic % to any buff/passive that should boost magic but used the shared BuffAtk/AttackPct (which is
physical-only now). Until then those magic-only buffs OVER-perform (they grant their FULL authored %).

---

## ✅ VERIFIED 2026-07-15 (afternoon batch — owner tested)

P.Atk L2 formula (bare-hands feeble, armed preserved), NPC buffer 3 paid options, gold tradable +
colour-tiered in the inventory, popups remember position across a client restart, stat-swap + training
passives require level 40 + 3rd class, subclass count limit (4 for a normal account), and PvP/PK/karma
shown in the character window — all confirmed. (The −int.max overflow the karma readout exposed is fixed
by the evening batch's karma cap.)

---

## ✅ 2026-07-15 PLAYTEST — RESULTS (verified) + NEW FEATURE QUEUE

Owner tested the 07-13 and 07-14 features — all **VERIFIED WORKING**: subclasses · level cap + delevel +
debug buffs · skill bar → DB · skill-bar readability + debug · stat-swap direction rule · skill-reset NPC ·
movable popups (great) · equipped-items pane · HealK=15 · OffChannelFactor stays 0.6.

**Changes found while testing:** mage-click reverted (all classes click-to-attack), skill cast cancels the
auto-attack walk, set info only on the BODY armor, stat-swap groups gated by class (fighter CON/DEX/ATK;
mage CON↔DEX + ATK/WIT/MEN), and stat-swap + training passives require level 40 + 3rd class — all BUILT +
VERIFIED. The class-uniqueness → discipline-only + count-cap rework was also built (evening) and is under
test in the "BATCH TO TEST" section at the top.

**NEW FEATURES / IDEAS (recorded to roadmap — see docs/Roadmap.md):**
- **Gold → an inventory ITEM** (L2 adena), tradable, and beyond int.max (long / stackable). Remove it
  from the vitals bar.
- **NPC buffer: 3 paid options** — full-buff (free ≤40; 3k·bufflevel each ≥40, ~150k for the full set),
  single-buff list, HP/MP restore (free ≤40; ≥40 costs `10k·(1−hp/maxhp) + 10k·(1−mp/maxmp)`).
- **Bare-hands is too strong** — a naked level-1 fighter (42 P.Atk) solos and one-shots level-4-8 mobs
  and can level to 20 with no gear. Investigate how unarmed/unarmored is handled. Mage has 43 P.Atk too.
- **Popup positions persisted** in the settings file (nested JSON per window), saved on close, defaulting
  when untouched.

**Untested older sections (07-09 and earlier) are left as `[ ]` below — not covered this playtest.**

---

## ✅ MAGIC RE-SCALE — SIGNED OFF BY THE OWNER IN THE 2026-07-14 PLAYTEST

**The damage numbers are CONFIRMED GOOD in-game. Do not re-tune them without a new reason.**
Owner, playing it: *"dmg seems fine — mage to tank 300-400 (1100 crits) for 11k HP is ok; tank to
mage 300 crits, ~120 dmg for 2k6 HP is fine"* and *"mage dmg is ok vs monsters, can solo, can dmg"*.

That also **closes the one number I had left open.** `tools/BalanceMatrix` predicted mage-vs-tank at
461/485 and I flagged it as possibly too hot — but that figure is both sides UNBUFFED. Buffed, in the
real fight, it lands squarely in the owner's 300-400. **The matrix was right and the target is met;
the gap was the buffs, not the balance.** Sets were confirmed working in the same session too.

Still worth eyeballing on a future pass (not blocking):
- [ ] **Leveling pace at 60-85 is ~3x faster** in wall-clock (same EXP per mob, mobs die 3x sooner).
      If that's too fast, drop `ExpRate` in Settings → Debug Tuning — no rebuild needed.
- [ ] **Boss/elite EXP.** A mob with an HP-multiplier passive now pays that multiple in EXP (a 3x-HP
      elite = 3x EXP). Was flat-by-level, so bosses paid trash EXP.
- [ ] **Fighter got faster too** (he rode the same broken mob curve). L85: ~25 basic hits to kill a
      same-level mob, was ~148. Check this doesn't now feel *too* fast.

---

## ✅ ALREADY VERIFIED FOR YOU — headless smoke test (2026-07-14)

`dotnet run --project tools/SmokeTest` (with the server up) drives a REAL client over SignalR and
asserts the whole subclass + skill-bar + persistence round-trip. **It passes.** So you can SKIP the
tedious half of the subclass section below — the following are machine-checked every run:

- a fresh character gets a populated skill bar
- adding a subclass gives it its OWN bar (it does not inherit the main class's)
- swapping back restores the main class's bar EXACTLY as arranged
- each class keeps its own level (11 vs 5 — they don't leak into each other)
- **all of it survives a full log-out / log-in**

It caught two real bugs before you ever saw them: a swap silently overwriting the new class's bar on
the server (while the client still *displayed* the right one), and — from the fix for that — a brand-new
character getting a completely EMPTY skill bar. Both would have eaten the first minutes of a playtest.

**What still needs YOUR hands is the WPF UI**, which I cannot click-test: drag & drop, the panel chrome,
the combat log, the equipped pane. Those are the risky ones now — do them first.

---

## ✅ SUBCLASSES (2026-07-14) — VERIFIED 2026-07-15

All subclass tests confirmed working: add a class, swap, per-class level/XP/skills/**skill bar**, shared
gear/gold, survives a relog, swap clears buffs, debug-reset drops subclasses. Machine-checked too by
`tools/SmokeTest`.

### ✅ Class uniqueness — RESOLVED 2026-07-16 (discipline-only + count cap shipped)

The archetype bar was replaced by a discipline-only bar (own 4 mages = 2 clerics + 2 nukers, just no two of
the same discipline), barred options greyed + server-refused, and subclass COUNT is now capped (4 normal /
admin unlimited) — all confirmed. Player-facing rules still not built: safe-zone-only swapping, 5-min swap delay.

---

## ✅ MOVABLE POPUPS + MAGE-CLICK (2026-07-14) — VERIFIED 2026-07-16

Every popup drags / closes / raises (owner: "very good to rearrange so they don't get in the way"), popup
positions persist to the settings file (saved on close, defaulting when untouched, like L2), the mage-click
change is reverted (all classes click-to-attack so a mage out of MP can melee a mob), and casting a skill
cancels the auto-attack walk outright (no longer keeps walking after the cast) — all confirmed.

---

## ✅ LEVEL CAP + DELEVEL + DEBUG BUFFS (2026-07-14) — VERIFIED 2026-07-15

Full-buff debug button, level cap 90 (admins exempt), delevel −1/−10, delevel keeps learned skills
(training passive re-synced to the new level) — all confirmed working.

---

## ✅ INVENTORY: EQUIPPED PANE + SET INFO (2026-07-14) — VERIFIED 2026-07-16

Equipped items have their own tab (Equipped / Bag / Quest), and the set requirement now shows only on
the BODY armor (the set-defining piece), not on boots/gloves/helm/accessories — all confirmed.

---

## ✅ SKILL BAR → DB (2026-07-14) — VERIFIED 2026-07-15

Bar persists per character, follows the character not the machine, "learn all" no longer reshuffles it,
cooldown no longer freezes a slot, cooldown countdown readable — all confirmed.

---

## ✅ SKILL BAR + DEBUG (2026-07-14) — VERIFIED 2026-07-15

Readable bar text, +10,000,000 gold button, debug class change keeps inventory, and drag & drop to
rearrange the slot buttons on the bar — all confirmed. (Dragging a skill FROM the skills window onto the
bar would be a separate feature if wanted.)

---

## ✅ STAT-SWAP DIRECTION RULE (2026-07-14) — VERIFIED 2026-07-15

Net-zero ring blocked, worked example holds, banned picks hidden + server-refused, learn-all grants no
swaps — all confirmed. (See the LEVEL-40 STAT-SWAP section below for two follow-up changes the owner wants.)

---

## ✅ TWO NUMBERS — DECIDED 2026-07-15

- **`OffChannelFactor` stays 0.6.** Owner: leave as is — a mage won't auto-attack and a fighter won't cast
  skills (once the bare-hands problem is fixed), so the off-channel trade doesn't need to bite harder.
- **`HealK` = 15 stays.** Owner: works ok, uses it to self-heal after a fight.
- ⚠ **TestHeal (power-1000 test skill on every char @76) can now be REMOVED** — it was only there to read
  these two numbers off the screen, and both are decided. Search `TEST ONLY` (3 spots in `Skills.Common.cs`
  + `GameLoopService.AutoLearnCoreSkills`). *(Not yet done — flag for cleanup.)*

---

## ✅ SKILL RESET NPC (Mindwright Sela — 2026-07-13) — VERIFIED 2026-07-15

Lists committed stat-swap skills + gold sunk, forgetting frees the group, gold not refunded, only
exclusive-group skills, out-of-range guard — all confirmed.

---

## ✅ LEVEL-40 STAT-SWAP PASSIVES (2026-07-13) — VERIFIED 2026-07-15

The only thing that moves your main stats now (born with CON/ATK/WIT/DEX; old free grants gone).
Gold-priced (1kk-5kk/level) + affordability-gated, each group a permanent commitment, the stats really
change (Max HP / eva-acc-crit-AS / cast-MP-crit / P&M.Atk), MEN gone as a stat, and the reset NPC
(Mindwright Sela) works. Both follow-up changes are in too: all groups gated by class (fighter CON/DEX/ATK;
mage CON↔DEX + ATK/WIT/MEN) and swaps + training passives require the 3rd class, not just level 40 — all confirmed.

---

## HEALS + PvP HEAL RULES (2026-07-13)

### ✅ Heal calibration + mechanics — VERIFIED 2026-07-15
HealK=15 works (owner uses it to self-heal after fights). Heals scale with M.Atk on the flat half,
staff-vs-sword changes heal output, fighter training no longer doubles M.Atk — all confirmed by play.
- [ ] ⚠ Still open (not blocking): **heal POWERS need re-authoring** — ours are 151-301, the target
  scale is ~1000. A future tuning pass.

### ✅ PvP heal rules — VERIFIED 2026-07-15
Can't heal the enemy you're fighting (self-casts), support reaches only self/party, supporting a
purple/red flags you, self-heal never flags — all confirmed.

---

## ✅ BUFF ROWS / SET TOOLTIP / SET SHIELD / TRAINING OUTPOST (2026-07-13) — VERIFIED 2026-07-15
4-row buff bar by subtype, set-bonus tooltip, set-shield bonus (incl. Heavy-61 reflect), Training
Outpost safe zone + Vess/Ilva — all confirmed.

## ✅ EVERYTHING IS A SKILL — potions/scrolls (2026-07-13) — VERIFIED 2026-07-15
Typing works in every text box, consumables cast skills (HoT/instant potions on the buff bar), Return
scrolls are item-granted not learned — all confirmed.

## ✅ DEBUG GEAR PICKER (2026-07-13) — VERIFIED 2026-07-15
Drill-down Armor/Weapons/Jewels by tier, full-set button, all 8 weapon families, read from ItemCatalog
— all confirmed.

---

## ✅ SKILL BAR (2026-07-13) — SUPERSEDED by "SKILL BAR → DB" above (verified 2026-07-15)

## ✅ DAMAGE RETUNE (2026-07-13) — SUPERSEDED by the MAGIC RE-SCALE at the top (signed off 2026-07-14)

The 07-13 MagicK 8→91 / archetype-multiplier removal / cast-speed rebase / weapon channel split were all
rolled into and re-tuned by the 2026-07-14 magic re-scale, which the owner signed off in play. Nothing to
re-test here separately. The `LevelStatBonus` removal and stats-no-longer-grow rules are verified via the
stat-swap testing above.

---

## To test now (disconnect / exit / combat + Return — 2026-07-09)

### ✅ Return skill + scrolls — VERIFIED 2026-07-15
Return skill (30s/5min, cancels on damage), Scroll of Return (Apothecary), Ultimate Scroll — all confirmed.

### Combat state + exit  *(NOT verified this playtest)*
- [ ] **Exit Game** (Settings) works out of combat (app closes). During combat (dealt/took damage in
  the last 30s) it's **blocked** with a message; 30s after the last hit you can exit.

### Disconnect fates (use 2+ clients)  *(NOT verified this playtest)*
- [x] **Go Offline (Auto-Farm)** button → you return to account select and your char keeps farming
  (offline), visible to others, until the 2h cap / death / relogin. VERIFIED.
- [ ] Drop while **auto-farming** (out of town) → offline farm (2h cap). The 2h cap is ONLY for
  offline farming — NOT for a network blip.
- [ ] Drop **mid-combat but not auto-farming** → your char **keeps defending** its current target
  (anti-combat-log) and the 180s grace timer is **paused** until combat ends (30s after the last
  hit); then the grace counts down. It is NOT put into the 2h offline farm.
- [ ] Drop while **out of combat, not auto-farming** → your char shows a **"⚠ Disconnected"** title
  above its head to nearby players, stays frozen and **in your party** (OFFLINE tag) for **180s**.
  Reconnect within 180s → resume seamlessly. After 180s → normal removal (leaves party).
- [ ] A disconnected (grace) char that a mob kills is removed immediately.
- [x] Offline-FARMING chars still look like normal players to non-party (no Disconnected title);
  only the grace state shows the title. VERIFIED.

---

## To test now (auto-hunt / idle farming — Phase 1, 2026-07-08)

**⚠️ Schema change:** added the `AutoHuntJson` column → **delete `Game.Server/bin/Debug/net8.0/game.db`
(+ `-shm`/`-wal`)** so it recreates before running.

### ✅ Auto-Hunt window + Behavior (Phase 1) — VERIFIED 2026-07-15
Auto-Hunt button/window, per-skill enable + reuse, HP/MP potion %, condition logic (attack on cd, buff
if missing, debuff if target lacks, self-heal <70%), auto-potions with auto off, Mana/s footer, normal
loot/XP — all confirmed.

### Offline farming (Phase 2, 2026-07-08) — partially verified 2026-07-16
- [x] With auto-hunt **ON** in a mob field, **close the client / disconnect** → a nearby character still
  **sees your char fight mobs** ("keeps hunting while away"). VERIFIED.
- [x] **Log back in** → re-attach to that same char with the loot/XP gained while away. VERIFIED.
- [x] Disconnecting **in a town**, while **dead**, or with auto **off** does a normal logout (no offline farming). VERIFIED.
- [~] ⚠ **EXPLOIT: an offline farmer that dies comes back ALIVE at full HP.** ✅ **BUILT 2026-07-16 (top batch).** Current: dies → stops → next
  login alive with auto off. Owner: he must **stay DEAD on re-entry** — otherwise "I'm about to die, can't
  escape → go offline-farm → re-enter full HP" is a free death-dodge. → on offline-farm death, persist the
  DEATH so re-login lands dead (at the res prompt / town), not healed.
- [ ] Caps: idle **8h** online / offline **2h**; hitting the idle cap stops auto and blocks re-enabling until relog.
- [x] Auto-hunt while offline still obeys the shared potion cooldown, buff-potion top-up, and skill conditions. VERIFIED.

### ✅ Debug Tuning panel (2026-07-10) — VERIFIED 2026-07-15
Live rates/karma/caps editing, cap=0→unlimited, persists across restart, window size persists,
admin-gated — all confirmed.

### ✅ PvP + flag/karma/PK (2026-07-10) — VERIFIED 2026-07-16
PvP/Counter toggles, PvP-on flags you purple + enemy retaliates + damage lands, attacking an innocent needs
PvP-on (purple/red free, hitting red doesn't flag), no PvP in towns, kill-innocent → red PK + karma,
kill flagged/red → PvP count (no karma), dying as PK −200 karma (clears at 0), farming as PK −20/kill,
counter-attack retaliation, karma persists across relog — all confirmed. (Karma AMOUNT formula is being
reworked — see the "Karma / PK / trade" batch at the top.)

### Stats-via-skills identity migration (2026-07-10)  *(NOT verified this playtest)*
- [ ] Rogue still has its crit/evasion identity (now from the **Evasion Mastery** passive: +20% crit,
  +20 eva); archer from **Reflexes** (+15% crit, +10 eva). Numbers should feel unchanged (parity).
- [ ] **Intentional change:** the **tank** no longer gets the old +level/2 magic defence (his Anti-Magic
  passive is his magic identity now) — confirm tank magic survivability still feels right.
- [ ] **Intentional change:** a base **rogue's basic attacks no longer interrupt casts** (that "cancel"
  becomes a 3rd-class discipline passive later) — confirm that's the intended feel.

### ✅ Roaming + target filters (2026-07-10) — VERIFIED 2026-07-15
Farm range, roam vs static spot, rank filter (mobs/elites/bosses), Basic-Attack row, survives relog —
all confirmed.

### ✅ Party + AFK interaction (2026-07-08) — VERIFIED 2026-07-16
Can't invite an auto-hunting/offline-farming player, AFK (yellow) / OFFLINE (grey) roster tags that clear on
reconnect, kick an AFK/offline member, leadership passes (★ moves) when the leader goes offline-farming,
unanswered invite auto-expires ~30s, an offline member that logs out leaves the party while a reconnecting
one stays — all confirmed.

---

## To test now (party window + mob cast-bar UI — 2026-07-07)

### Party window (WPF client) — partially verified 2026-07-16
- [~] ⚠ **Can't target party members through the party window.** ✅ **BUILT 2026-07-16 (top batch).** Clicking a roster row does NOT target that
  member — **a healer must be able to click an ally in the party panel to target + heal them.** Must-fix.
- [~] **Close (✕) on the party window reopens it immediately** (closes then re-opens). Minor; likely a
  WPF-harness-only quirk — the panel probably shouldn't offer a manual close while you're in a party (it
  hides on leave/disband anyway).
- [x] Invite via target frame → accept/decline prompt; Party panel lists members (name/Lv/class, HP/MP bars,
  ★ leader); leader ✕ kick works — corroborated by the verified invite/kick/AFK tests. VERIFIED.
- [x] Invite button on the target frame, **Leave** removes you / disbands below 2, roster HP/MP bars update
  live as members take damage/heal — VERIFIED.

### ✅ Party loot rules (2026-07-07) — VERIFIED 2026-07-16
New party defaults to Random, leader-only Loot dropdown, changing it starts an all-must-agree vote (Decline
cancels, ~30s timeout, snaps back on cancel), invite prompt shows inviter name + loot rule, Finders Keepers /
Random / Round Robin / Leader Only all distribute correctly, gold always split among in-range members (killer
keeps the remainder), only alive in-range members eligible — all confirmed.
- [ ] Boss/elite crafting-mat pile goes to a single recipient per the loot rule. *(not tested)*

### ✅ Mob / boss cast-bar — VERIFIED 2026-07-15
Orange cast-bar under the nameplate fills over cast time, clears on interrupt/kill/finish — confirmed.

### Boss unique skills + phases + adds (2026-07-07)  *(NOT verified this playtest)*
- Fight the **Valley Treant Lord** (Boss zone ~(24000, 45000), L60). Bring a party/high level — it
  has 20× HP. (Long real respawn; use debug teleport to reach the zone.)
- [ ] From full HP it casts **Devastating Slam** (telegraphed slam, dmg + stun) on its reuse timer.
- [ ] At **50% HP** it announces + **enrages** (rage buff, faster/harder hits) and **summons 2 adds**
  (bogwood, ~L52) that immediately attack whoever it's fighting.
- [ ] Below 50% it also starts casting **Thorn Nova** (wider magic burst + a slow) — a second,
  distinct boss skill it did NOT use above 50%. Its name shows in the cast-bar.
- [ ] At **25% HP** it announces the thorn storm (flavor line).
- [ ] Leash/reset (walk it home) **re-arms** the phases and clears its skill reuse; a fresh pull
  starts at Slam-only again. Adds do NOT respawn when killed.
- [ ] Other bosses with no profile still use the plain slam (unchanged).

---

## ✅ RANGED + CASTER MOBS (2026-07-03) — VERIFIED 2026-07-15
Archer mobs (bow from range, ×2 P.Atk, squishy), mage mobs (cast-only, MP-gated → helpless when out),
golem-type weapon resist (obsidian_knight) — all confirmed.

## ✅ MOB OVERHAUL (2026-07-02) — VERIFIED 2026-07-15
Mob base-stat curve, weapon-type P.Def resistance, the 80-mob roster + zones + drops — all confirmed.
*(Note: the mob-curve numbers were later reshaped by the 2026-07-14 magic re-scale — see the top section.)*

## ✅ PHYSICAL SKILLS SCALE BY ATTACK SPEED (2026-06-29) — VERIFIED 2026-07-15
Fighter physical-skill cast time follows attack speed, not cast speed — confirmed.
*(The "mage no auto-attack after a spell" item from this date is being REVISED — owner now wants all
classes to click-attack; see the playtest-3 queue at the top.)*

---

## Playtest 1 results (2026-06-28)

**Verified working:** damage & crits (incl. [Double]) at all levels; control lands (slow/
root/stun/fear); DoT + burst; defensive skills + Provoke/threat; movement (blink/knockback);
weapon masteries; mage damage feels OK for now.

**Fixed this round — ✅ VERIFIED 2026-07-15** (Restore Mana cost/targeting, Phase Shift no-target,
cast-bar class name, debug Level+10 / Learn-all buttons).

**Open items:**
- [ ] **FIGHTER BALANCE (big)** — Venomweaver burst ~1500; a Lv-49 tank solos hordes of Lv-64
  mobs. Skills work as intended; numbers need a tuning pass (damage-out / mastery / skill power).
- [ ] **Stacks not visible as a LEVEL on the mob** — expand the target window (▼) to see
  "Effects: Creeping Frost x3"; consider a stack readout on the always-visible target frame.
- [ ] **Friendly target dummy** to test heals/cure/buffs on an ally — needs ally-targeting
  (likely after PvP, so you can also damage/debuff friendly dummies).
- [ ] **Skill-detail TITLE shows base name** (not the class name) — owner will give the exact
  skill + race/2nd/3rd class next test. Suspect: client `_myThirdClass` not synced after a
  DEBUG 3rd-class change, so discipline-renamed skills fall back to the base name.
- Dummies don't regen — owner: don't care (they never die via the 1-HP floor).

---

## To test now (this session — 2026-06-27)

### ✅ Training Grounds + Blink/Knockback + Taunt/Threat (2026-06-27) — VERIFIED 2026-07-15
Immortal training dummies, Shadowstep/Repelling Shot/Phase Shift blink+knockback, threat-based aggro +
Provoke/detaunt — all confirmed.

### Combat primitives P2: poison & venom (Venomweaver per-race trio) — NUMBERS UNTUNED  *(NOT verified this playtest)*
- [ ] Venomweaver DoT is now per race: Human = bleed (−MS), **Elf = poison** (Toxic Sting/Burst), **Ork = venom** (Envenom/Venom Burst).
- [ ] Poison (Toxic Sting): magic DoT (ATK-vs-WIT) + slows the target's attack & cast speed ~15% (stat window of a player target; mobs just attack/cast slower). Toxic Burst spends stacks.
- [ ] Venom (Envenom): physical DoT (DEX-vs-CON) + lowers target attack ~15% and defence ~15% (a venomed mob hits softer and takes more). Venom Burst spends stacks.
- [ ] These secondary debuffs are cleansable and expire with the DoT; new DebuffAtk/DebuffAtkSpeed/DebuffCastSpeed channels don't affect buffs.

### Combat primitives P2: DoT (separated effect + stack counter) — NUMBERS UNTUNED
- [ ] Venomweaver "Rupture" @40 applies a bleed: a FLAT "DoT" tick each second + 15% slow; reapplying refreshes 30s and builds a stack (counter is hidden — not on the buff bar).
- [ ] Bleed tick damage does NOT grow with stacks (it's the damage effect); stacks only fuel the burst.
- [ ] "Detonate Wounds" @44 hits for ~damage × stacks (×10 at full), removes the COUNTER, and leaves the bleed DoT ticking.
- [ ] Detonate consumes only ITS line's stacks (ConsumeStackKey) — another applier's stacks are untouched.
- [ ] Bleed damage effect overrides by Rank (a stronger bleed replaces a weaker); counters stay independent.
- [ ] A DoT can finish the kill (credit + drops go to the applier).
- [~] Poison/venom + their −AS/cast, −atk/def secondaries not authored (need debuff channels outside AnyBuff). Cure/cancel skills not built yet.

### Mana shield + lethal save ("Mana Barrier" / "Last Stand") — NUMBERS UNTUNED
- [ ] Magus "Mana Barrier" @44: while up, taking damage drains MP (0.5 per damage) for 70% of the hit; HP loss is reduced; stops diverting when MP runs out.
- [ ] Bulwark "Last Stand" @44: a blow that would kill you within 10s instead leaves you at 50% HP, and the buff is consumed (one save).
- [ ] Both interact correctly with absorb shields (shield soaks first, then mana shield, then lethal save).

### Absorb shields ("Aegis") — NUMBERS UNTUNED
- [ ] Tank (Bulwark/Vanguard) "Aegis" @40: a self-shield absorbing 8% of max HP for 15s shows on the buff bar.
- [ ] While shielded, incoming damage drains the shield first; HP only drops once it's depleted; the shield buff vanishes when empty.
- [ ] Works vs all damage types (basic, skills, DoT ticks) — they all route through ApplyDamage.
- [~] Known cosmetic: floating combat text shows pre-absorb damage (HP loss is correct). To refine later.

### Cure / cancel (dispel) + cancel resist — NUMBERS UNTUNED
- [ ] Healer "Antidote" @25 removes poison/venom from an ally (or self); does NOT remove other debuffs (slow/bleed/stun).
- [ ] Nuker "Dispel Magic" @35 on an enemy strips up to 2 random beneficial buffs (test vs a buffed player, or self-cast a buff then have someone dispel).
- [ ] Internal DoT stack counters are NOT removed by cure/cancel; a non-Cancellable effect is immune.
- [ ] Existing Lightbringer full Cleanse still removes all debuffs (empty DispelMask = all).
- [ ] Tank "Indomitable" @48 (+80% cancel resist 30s): while up, most of the tank's buffs survive a Dispel Magic (each rolls an 80% save). Cure on debuffs is unaffected by resist.

### Stack / effect visibility
- [ ] A stacking buff on YOU shows "Name xN" on the buff bar (count updates as it stacks).
- [ ] Expand the target window on a bled/slowed mob → "Effects:" line lists its active effects with stacks (e.g. "Rupture (stacks) x5", "Slow") — so you can time Detonate Wounds.
- [ ] Effects line refreshes ~1/s while the panel is open.

### Combat primitives: generalized stacking (per-stack effect table) — NUMBERS UNTUNED
- [ ] Tempest "Creeping Frost" @44: each landing cast adds a stack — slow 10% → 20% → 30% on stacks 1-3, then the **4th stack FREEZES** the target (stun, no slow). Same skill, different effect per stack level.
- [ ] A resisted cast does NOT add a stack (stack only on success); re-landing refreshes the timer.
- [ ] Rogue bleed counter caps at its skill's MaxStacks (10) — editable per skill, not a global constant.
- [ ] A non-stacking buff/debuff (MaxStacks 1) behaves exactly as before.

### ✅ Expandable target window + Weapon/Mage masteries + Class-change blurbs (2026-06-27) — VERIFIED 2026-07-15
Target-frame ▼ inspect panel, fighter weapon masteries (+ 1H/2H gating), mage Spell Mastery + bow
penalty, 2nd/3rd-class dialog blurbs — all confirmed. *(Mastery percentages still `[~]` to tune later.)*

### Combat primitives P1: Root + physical Slow + skill-damage% — NUMBERS UNTUNED  *(NOT verified this playtest)*
- [ ] Nuker learns "Entangling Roots" @40 (magical root, ATK-vs-WIT): target can't move for 8s but can still act.
- [ ] Warrior learns "Hamstring" @40 (PHYSICAL slow, ATK-vs-CON, −60% MS) — confirms slow exists in both schools (vs the magical Frost Bind).
- [ ] Warrior learns "War Focus" @40 (20-min self-buff): +15% attack speed shows in the stats window; the +25% PvP skill/basic damage is latent (no PvP yet). Confirms the damage matrix wiring (PvE damage unchanged by it).
- [ ] Root lands via the contest (not fizzle); existing non-contest debuffs (Weakness/anti-heal) still behave as before.

### Combat primitives P1: conditional damage ("Glacial Spike") — NUMBERS UNTUNED
- [ ] Nuker learns "Glacial Spike" @44; on a normal target it does power-90 damage.
- [ ] After Frost Bind (slow) or Entangling Roots (root) on the same target, Glacial Spike hits ~50% harder.
- [ ] The bonus only applies while the target is slowed/rooted (wears off when the CC ends).

### Combat primitives P1: Stun + Fear ("Shield Bash" / "Terrifying Roar") — NUMBERS UNTUNED
- [ ] Vanguard learns "Shield Bash" @40 (stun 3s); warriors learn "Terrifying Roar" @40 (fear 5s).
- [ ] Stun: target can't move, cast or attack for the duration (a mob freezes; a casting target's cast breaks).
- [ ] Fear: target can't cast or attack but CAN still move.
- [ ] Both land via ATK-vs-CON contest (10–90%); bosses immune; cleansable; show on the target/expire normally.
- [ ] While YOU are stunned/feared, your skills are refused ("You are stunned." / "...too afraid to act.").

### Combat primitives P1: physical [Double] crit ("Cleaving Strike") — NUMBERS UNTUNED
- [ ] Warrior (Ravager/Warlord) learns "Cleaving Strike" @40; it sometimes hits for ~2× (shown as Crit).
- [ ] Double chance scales with the higher of DEX/ATK, capped 30%; ordinary skills never double.
- [ ] Existing physical skills (Power Strike, Mighty Blow, etc.) crit exactly as before (basic crit path unchanged).
- [ ] A shield can still block a non-doubled Cleaving Strike; a double ignores block (like a crit).

### Combat primitives P1: debuff contest + Slow ("Frost Bind") — NUMBERS UNTUNED
- [ ] Nuker (Magus/Tempest) learns "Frost Bind" @40; casting it on a mob visibly halves its move speed for 10s.
- [ ] Landing varies with the ATK-vs-WIT contest: high-WIT targets resist more (shows "Fail"/resisted), 10–90% bounds.
- [ ] Slowed target still moves (never fully stopped — that's Root); slow is cleansable / expires after 10s.
- [ ] Existing debuffs (Weakness, anti-heal, Root) behave exactly as before (not switched to the contest).

### Skill reagents / consumables + Nuker "Elemental Burst" (NEW) — NUMBERS UNTUNED
- [ ] Debug → Consumables → "Elemental Stone +10" grants 10 stones per click (stacks).
- [ ] Nuker 3rd class (Magus/Tempest) learns "Elemental Burst" @40, then 44/48/…/72/75 (10 levels, power 150→250).
- [ ] Casting without ≥10 stones is refused up front: "requires 10x Elemental Stone".
- [ ] Casting with stones works and consumes exactly 10 (inventory updates); damage scales with skill level.
- [ ] Stones are NOT consumed if the cast is interrupted / target lost.
- [ ] Other skills (empty `ConsumableId`) cast freely as before.

### ✅ Toggle skills + Healer "Combat Stance" (2026-06-27) — VERIFIED 2026-07-15
Combat Stance toggle (+50% P.Atk / −50% M.Atk), buff-bar ⟳ marker, no expiry, clears on death/relog —
all confirmed. *(±50% swap still `[~]` to tune later.)*

---

## ✅ Tuning targets (owner-stated) — VERIFIED 2026-07-15
Cleric-solo, low-level mob damage sane, mage TTK, healer numbers, armor masteries, mob passives, newbie
buffer set — owner reports these all feel right as they stand.

## ✅ Carryover from prior sessions — VERIFIED 2026-07-15
Buff/effect layer, buff-bar drop, economy/untradeable-reject/boxes, jewel caps, debug teleport,
enchant/reroll sync, per-race Holy Bolt name — all still good.


---

<a id="skills-not-in-csvs"></a>

# ══ Skills not in the CSVs (playtest-17 `B3`) ══

> ✅ **CLOSED by his ruling.** The deletions were made in 0.53.0; `lb_*`/`wc_*` stay as 40+
> placeholders. Kept verbatim because the name-override mapping in it is still true: one skill
> DEFINITION wears several class names, so deleting a definition hollows out three disciplines.

# Skills that exist in code but are NOT in your CSVs (playtest-17 B3)

**You asked for this list so you can say which to delete. Nothing has been deleted.**

Your CSVs live in `docs/data/classes_skills_csv/` — 7 files (`fighter 01-15`, `mage 01-15`,
`warrior/tank/rogue/nuker/healer 20-35`). They identify a skill by **NAME only**, so this is a
name diff. There are **no CSVs for level 40+**, so every 3rd-class skill is "outside the CSVs"
by definition and is listed separately at the bottom.

---

## ⚠ Read this before deleting anything

A skill NAME on a class table is often an **override** of a shared definition. Twelve 3rd-class
skills are really five underlying skills wearing different names:

- `power_shot` = **Heavy Draw** (rogue 24) **and** Piercing Shot / Snare Shot / Rending Shot (40)
- `twin_slash` = **Twin Slash** **and** Ambush / Venom Strike / Silencing Cut (40/43)

So **deleting the definition hollows out three disciplines**. The safe operation for the two you
named is to remove the **class-table grant at level 24**, keeping the definition for the 40+ kit.

## 1. The two you named

| you said | actually | where it comes from |
|---|---|---|
| **Heavy Draw** | `power_shot` — confirmed absent from every CSV | granted to **Rogue @24** |
| **Twin Blade** | there is no "Twin Blade" — you mean **Twin Slash** (`twin_slash`) | **already removed** from the level-24 archer table on 2026-07-01; at HEAD nothing below 40 grants it. You were playing an older build. |

**So: one grant to remove (Heavy Draw @24). Twin Slash is already gone below 40.**

## 2. Other learnable skills not in the CSVs

**Base mage** — `cast_def_phys` **Bulwark** (+8% P.Def) @7.

**Healer @20-35** — these are the individual buffs you moved off the Warchanter on 2026-07-31.
Your CSV still lists the OLD group names (Might/Force/Focus/Speed/Body), so they read as
"missing" but they are the intended replacement. **Probably keep all of these:**
Swift, Alacrity, Resolve, Bulwark, Vampirism, Agility, Aim, Haste, Vigor, Ward,
Combat Stance, Antidote, Resurrection, Restore Mana.

**Tank / warrior / nuker / base fighter** — nothing extra. Everything they grant is in a CSV.

## 3. In the catalog but granted to NOBODY (dead weight — safe to delete)

> ## ✅ RESOLVED AND BUILT, 2026-08-07 (0.53.0). This section is now HISTORY.
>
> What actually went: **`reflexes`, `archer_armor_mastery`, `archer_weapon_mastery`, `dispel_magic`,
> the Heavy Draw @24 grant (and its three 40+ discipline renames — you asked for both halves), and
> the WHOLE God layer** (`Race.God`, `ItemRarity.God`, `god_judgment`, `god_robes`, `hp_boost`,
> `greater_heal`, `Classes.God.cs`, the two God classes 98/99).
>
> What STAYED, on your rulings: **`evade_mastery`, `precision`, `anti_magic`** (live class floors) and
> **`class_balance_*`, which is COMMENTED OUT, not deleted** — *"class_balance should be commented for
> now"*. The `power_shot` **definition** also stays; only its grants are gone.
>
> ⚠ **Your debug rig is now `/enchant <value>` + `/speed` and nothing else** — the God gear rows are
> off the debug menu. Everything below is the reasoning that got here; keep it for the trap it
> documents (§"Why the list lied").

> ## 🔴 CORRECTION, 2026-08-05 — READ THIS BEFORE ACTING ON YOUR G1 ANSWER
>
> **Five lines of the list below were WRONG, and you answered "delete" on the strength of them.**
> Checked against the code before deleting anything (`GameLoopService.AutoLearnCoreSkills`, line ~1174):
>
> | I said | actually |
> |---|---|
> | `evade_mastery` "granted to nobody" | ❌ **auto-granted to EVERY rogue** at 20/40/76 (`FloorPassiveFor`) |
> | `precision` | ❌ **auto-granted to every warrior** at 20/40/76 |
> | `anti_magic` | ❌ **auto-granted to every tank** at 20/40/76 |
> | `class_balance_*` (8) | ❌ **auto-granted to EVERY character alive**, line 1179 |
> | `reflexes` | ✅ correct — dead, but only because `Archetype.Archer` no longer exists after the merge |
>
> These are not dead weight: they are the **class identity floors** — the rogue's sure-dodge, the
> warrior's sure-hit, the tank's magic-fizzle resist — and they feed live stats (`EvadeFloor`,
> `HitFloor`, `MagicFailFloor`, set in `Entity.RecomputeDerived`). They are documented as the design in
> [design/CombatResolution.md](../design/CombatResolution.md) §"Class floors". Deleting them silently
> removes a combat floor from every rogue, warrior and tank you have.
>
> They are absent from your CSVs because they are **auto-granted rather than learned** — they never
> needed a CSV row. That is a different thing from "nobody has them", and the diff that produced this
> list could not tell the two apart.
>
> **So G1 splits in two.** Genuinely dead and safe to delete: `reflexes`, `archer_armor_mastery`,
> `archer_weapon_mastery`, `dispel_magic`, and the Heavy Draw **grant** (never the definition).
> Live and load-bearing: `evade_mastery`, `precision`, `anti_magic`, `class_balance_*`. **Nothing has
> been deleted; your call on the second group.**

- `evade_mastery`, `reflexes`, `precision`, `anti_magic` — the four "identity floor" passives.
  ⚠ **three of these four are LIVE — see the correction above.**
- `class_balance_*` (8) — Class Balance passives. ⚠ **auto-granted to everyone — see above.**
- `archer_armor_mastery`, `archer_weapon_mastery` — orphaned by the archer→rogue merge. ✅ truly dead.
- `dispel_magic`. ✅ truly dead.
- Lightbringer (8, `lb_*`) and Warchanter per-race (12, `wc_*`) — **see the answer below.**
- `hp_boost`, `greater_heal` — god-only, and the god table is never registered.
  ⚠ But `Race.God` itself is **your debug race** and `ItemRarity.God` / `god_judgment` / `god_robes` are
  the debug gear your own admin menu hands out. "God class + skills" reads to me as the two SKILLS plus
  `Classes.God.cs`'s learn table — **not** the race enum or the debug item tier, which would take your
  testing rig with them. Confirm before I widen it.

### ❓ You asked what `lb_*` and `wc_*` are (playtest-18 G2)

**They are the level-40 HEALER disciplines — a written, finished 3rd-class kit that nobody can learn
yet.** The healer's two branches: **Lightbringer** = the pure healer, **Warchanter** = the buffer.
The definitions are alive and registered in the catalog; what is commented out is one line in
`ClassSkillTables.Third.cs` — `// RegisterLightbringer(); RegisterWarchanter();` — the *learn
assignments*, dropped **pending your level-40 CSVs**. So they are not dead like the four passives are
dead; they are **parked waiting on you.**

| | Human | Elf | Ork | shared |
|---|---|---|---|---|
| **Lightbringer** (8) | Mend @40 (fast strong single heal), Purify @44 (cleanse) | Dawn @40 (AoE heal + cleanse), Warden @44 (root + self de-taunt) | Font @40 (AoE heal), Sap @44 (anti-heal debuff) | Blessing of Light @48 (party +15 % HP/def), Devotion @52 (passive) |
| **Warchanter** (12) | Bolt @40, Chant @44, Renew @48, Passive @52 | same four | same four | — (the mega party chant is per-race, same magnitudes, different names) |

⚠ **The Warchanter's BUFF layer is a separate thing and it IS live** — `RegisterWarchanterBuffs()` runs,
which is where every group buff and Harmony you use today comes from. Deleting `wc_*` does **not** touch
those.

**My recommendation: keep both, delete neither.** They cost nothing (they are unreachable), and they are
most of a 3rd-class healer kit already written — re-authoring them later is more work than uncommenting
one line once your 40+ CSVs exist. If you want them gone anyway, say so and they go with the rest.

## 4. Level 40+ (no CSV exists yet)

- **12 stat-swap passives** (`swap_*`, gold-priced, @40).
- **24 discipline placeholders** @40/43 — the renamed shared skills listed in the warning above.
- The authored 3rd-class kit: `elemental_burst`, `frost_bind`, `entangling_roots`, `glacial_spike`,
  `creeping_frost`, `phase_shift`, `mana_barrier`, `cleaving_strike`, `hamstring`, `war_focus`,
  `terrifying_roar`, `shield_bash`, `provoke`, `aegis`, `last_stand`, `indomitable`, `rupture`,
  `detonate_wounds`, `toxic_sting`, `toxic_burst`, `envenom`, `venom_burst`, `shadowstep`,
  `vanish`, `repelling_shot`, `snare_trap`.
- The **Warchanter** buff table @40-74 + the five improved group buffs.

## 5. Name drift — these ARE in your CSVs, just spelled differently

Defencive Wall→Defensive Wall · Bow Expretise→Bow Expertise · Two Handed Mastery→Two-Hand Mastery ·
Anti magic→Anti-Magic · Taunt→Provoke · Rogue Armor/Weapon Mastery→Armor/Weapon Mastery ·
warrior "Strike"→Smash · rogue "Stab"/"Shot"→Piercing Stab/Precise Shot · healer "Holy Bolt"→
`holy_strike` (per-race name: Holy / Moonlight / Spirit Bolt).

## 6. In your CSV but NOT in the code (the reverse gap)

- healer **"Speed"** (all 4 levels) — `holy_speed` is Warchanter-only now.
- healer **"Body" @35** — she gets Vigor and Ward instead.
