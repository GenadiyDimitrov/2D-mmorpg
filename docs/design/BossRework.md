# Boss rework — per-boss identity, the ×2 ladders, and the one question that settles all of it

**Your message, 2026-09-05**, after soloing the level-90 boss on a tank in epic A gear and then
pulling its escort: *"boss and mobs do 300 dmg to a 2.5k Def tank"* … *"some Bosses have fighters
around them 2-5 which have more p atk than bosses … that who doesn't can have additional passive
skill 'solo boss' that additionally adds x2 p/m atk"* … *"all bosses use (our war/spell Runes ug-SS)
which in reality doubles their p/m.atk"* … *"most bosses have overpowered single/aoe phys/magic
skills that will do 1200/1500 to the same tank"* … *"if battle becomes longer than 20 min he gets a
buff that additionally doubles p/m atk and after 40 mins it's gives another x2 (only field/dungeon
bosses, world once the it times will be 2h and 3h)"* … *"bosses 85 with 3~6kk hp not like out 350k"*
… *"the bosses don't fallow the curve persay but have different edits per boss … are bosses separate
from the mobs file (in code .cs) ? they need their own to be edited/added - stat and skills as well"*

Tracked as `BL-166` (per-boss stat block), `BL-167` (the ×2 ladders), `BL-168` (the world-boss rank).

---

## 1. Your last question first: **half of it already exists.**

| | Where it lives today | Per-boss? |
|---|---|---|
| **Skills + phases + adds** | **`Game.Shared/BossCatalog.cs`** — a `BossProfile` keyed by mob-template id, holding a `BossSkillEntry[]` rotation (each with an HP window) and a `BossPhase[]` script (announce / enrage / add wave). | ✅ **Yes, already.** This is exactly the file you are describing, and it is where the dungeon full-silence went in `BL-155`. |
| **Stats** | **`MobCatalog.cs`** (the template + its optional `MobMod` passive block) **× `MobRankScale.cs`** (`Hp` / `Atk` / `Def` / `AccFlat`). | ❌ **No.** `MobRankScale` is **one set of numbers every boss in the game shares** — ×4 attack, ×2 defence, one HP curve. |

So a boss's **kit** is already unique per boss and its **stat block** is not. The `MobMod` seam
*could* express a per-boss lean (it has `PAtk`, `MAtk`, `PDef`, `MDef`, `Hp`, `AtkSpeed`, resists,
CC-resist overrides), but **three of the four boss templates in the game carry no `MobMod` at all** —
`valley_treant`, `dread_knight` and `disciple_of_the_dawn` are the bare curve × the shared rank. Only
`grave_lich` has one (`AntiMagic("Deathward")`).

**What `BL-166` does:** move the stat block into `BossProfile` beside the kit, so **one entry in one
file is the whole boss** — its HP multiple, its attack lean, its defence lean, its rotation, its
phases, its adds. `MobRankScale` stops being the boss's stats and becomes only the **default** a
profile overrides — which is your *"The curve is the base and every boss edit is making the boss
unique"*, said in code.

---

## 2. What a boss actually is today — **measured**, not remembered

`dotnet run --project tools/BalanceMatrix`, level 90, against your own prescribed party (tank,
healer, 2 champions, 1 nuker, best-for-tier gear, runes up):

| | measured |
|---|---|
| Boss HP at 90 | **343,474** (base 6,520 × the ×53 rank curve) |
| Boss P.Def at 90 | 1,544 |
| Basic attack → Knight | **728** = **5% of the tank's ~14.5k pool** |
| Basic attack → robe | 1,479 = 29% |
| Sustained dps → tank (shield in) | **442** |
| Healer's ceiling (top heal on cycle, not MP-limited) | **391** |
| **Tank's survival with NOBODY healing him** | **33 seconds** |
| Time-to-kill, 5-man at ceiling dps | 1,274s (21 min) |

**The reason the boss "cannot kill" you is not the per-hit number** — it is that a few percent per
swing is nothing a tank has to respect. He stands there for over half a minute with no healer at all.
That is the sentence to fix.

⚠ **The escort is a separate finding.** An ordinary level-90 creature measures **~98** on that tank,
so whatever nearly killed you was **not plain trash**. If those were the dungeon guards (`GuardTank` /
`GuardArcher` — player-built, +16 enchanted, holding a War Rune) that fully explains it, and it
**proves your own point for you: the guards hit ~3× harder than the boss they guard.**

### 🔴 2b. CORRECTION, same day — the numbers above are measured on the WRONG TANK

**You gave me your real character** (2026-09-05): *"my paladin tank 90lvl with epic 76 gera have
1300pdef unbuffed and 2300+300 reinforcement to 3200 with aegis sigil buffed from npc (mark/harmony)
and the boss with 14.5k p atk does 400 (blocked 75% of the time for 20% dmg and that's all whitout
the sigil)"*.

**Your 400 reproduces exactly.** `PhysicalDamage` is `77 × pAtk / def`, so 77 × 14,500 / 2,600 = **429**
— inside 7% of what you read off the screen, and the residue is just where in the buff stack you took
the reading. The damage model is not the problem and neither is your arithmetic.

🔴 **The problem is my measuring rig, and it has been wrong for every tank number in this document.**
`BalanceMatrix.BuildBossParty` builds its tank as `BuildPlayer(Human, Fighter, level)` — and that
function:
* takes **no discipline**, so at level 90 it is a **2nd-class Knight** with `SecondClass = 13`. No
  Bulwark, no Aegis, none of the 3rd/4th-tier kit you actually play.
* applies **exactly one buff — the War Rune**. **No NPC buffer at all**: no mark, no harmony, no
  Shield Blessing, no reinforcement.

Back-computed from its own output (77 × 14,500 / 728), the tank in that table sits at **~1,533 P.Def**
against your real **2,600** — the rig understates your tank's defence by about **70%**, and it has
done so since the NPC buffer was built. Tracked as **`BL-169`**.

**What that changes:**

| | rig's tank | your tank |
|---|---|---|
| P.Def | ~1,533 | **2,600** (3,200 with the sigil) |
| Boss basic | 728 | **400** |
| Block | shield + mastery only | **75% chance × 20% reduction = 15% average mitigation** |
| Boss basic, after block | ~700 | **340** |

So the boss hits you for **340 a swing after block**, not 728 — and your complaint stands *harder*
than the table said, because you are taking less than half what it modelled and still find the fight
trivial.

⚠ **And note the shape of your block: it is BROAD AND SHALLOW.** `StatCaps.BlockChance` is **1.00** —
a 100% ceiling — and you are already at 75% of it, while `BlockReduction` is at **0.20 of an 0.80
ceiling** (the A-grade shield's own authored number; nothing but the shield sets it any more). 75% × 20%
is the 15% average you yourself computed the last time you ruled on shields (*"not 47% dmg reduction
with 33% chance, that's average 15%"*, 2026-08-11). That is working as authored — but it means block
is nearly maxed on the axis that does the least, and **any future rise in `BlockReduction` scales a
tank's mitigation hard.** Worth knowing before anyone touches it.

---

## 2c. THE RIG IS FIXED — and every number above was wrong in your favour

`BL-169` is built (tool only, nothing in the game moved). The boss party is now a **buffed Bulwark
tank + buffed Lightbringer + three buffed DDs**, wearing the full NPC shelf — every blessing at its
level's tier, the eight single harmonies (`BL-160`) and one Mark (`BL-161`).

**And the tool now carries a table that can be PROVEN WRONG**, which nothing else in it can: you read
these numbers off your own Paladin, so a row that disagrees means the rig is wrong, not the game.

⚠ My first attempt at this comparison was sloppy and I am flagging it rather than quietly fixing it:
I put your **level-90** epic-76 tank beside the rig's **level-76** row and called it a 14% match. Those
differ by fourteen levels of HP curve as well as by gear, so the agreement was partly luck. One level,
two gear sets, is the only comparison that isolates what you actually changed.

🔴 **And it turned up a second rig defect on the way.** `BuildPlayer` mapped `quality: "epic"` to the
*bare* item id on a stale comment (*"the bare id IS the Epic"*). `ItemCatalog.QualityId` is explicit
that **Mythic is the authored item and carries no suffix** — so asking for epic silently got **mythic**,
and every default caller in the tool has been measuring a full mythic loadout. (`_mythic` is not an id
at all: asking for it prints "missing item" and dresses a naked character, which is what the first run
of this table did.) Fixed; no other caller passes a quality, so nothing else moved.

### The falsifiable table, level 90, your two sets

```
                   set  barePDef    PDef     his   delta   MaxHp     his   delta
      epic A (tier 76)      1414    3465    2600    +33%   27527   17000    +62%
    MYTHIC S (tier 80)      2318    5679    4300    +32%   28997   20000    +45%
      epic S (tier 80)      1738    4258       -       -   27664       -       -
```

| | verdict |
|---|---|
| **Bare P.Def: 1,414 vs your 1,300 — +9%** | ✅ the gear + passive model is **right** |
| **Buffed P.Def: +32% and +33%** | ⚠ consistent across both sets, so it is the **buff layer**, not the gear: the rig buys every blessing, all eight harmonies and a Mark. Your stack multiplies ×2.0 (1,300→2,600); the rig's ×2.45. |
| **Max HP: +62% and +45%** | 🔴 **this one does not track the P.Def gap, so it is a separate thing.** Your gear adds +18% HP going A→S (17k→20k); the rig's adds +5% (27.5k→29k). Something in the rig's buff/passive layer is carrying HP that yours is not — `npc_body`, the `NpcHBody` harmony (+30% Max HP) and the Mark are the candidates. |

**❓ The one thing that would close it: which blessings do you actually have up?** The rig takes the
whole shelf. If you run eight of them rather than twenty-nine, that is the entire remaining difference
and there is nothing to fix.

⚠ **But note what this does NOT change.** The tank side no longer needs the rig at all — you have given
ground truth at both gear tiers, and I will tune the boss against **your** numbers. The rig still
matters for party DPS and time-to-kill, where there is no screen to read them off.

### 🔴 And now the boss tables, corrected

```
 Lvl   boss HP   party dps      TTK           band          (was, on the straw party)
  44   242 982        370      656s    ok (11 min)          225 dps / 1079s
  60   281 453        558      505s       TOO FAST          205 dps / 1376s
  76   315 821        534      591s       TOO FAST          208 dps / 1515s
  85   333 854      1 884      177s       TOO FAST          292 dps / 1144s
  90   343 474      1 752      196s       TOO FAST          270 dps / 1274s
```

**Party DPS at 90 went 270 → 1,752 — six and a half times.** Time-to-kill went **21 minutes → 3.3
minutes.** Every level from 60 up is now outside your own 600-1800s band, at the fast end.

```
 Lvl  bare  tankPDef  tankHP  basic→tank  %tank  blocked  avg/swing  dps→tank  unhealed   verdict
  60  1361      2246    7151         119     2%      101        113        75      110s   a tank cannot feel it
  76  1795      2962   18755         189     1%      151        173       115      182s   a tank cannot feel it
  90  2318      5679   28997         172     1%      129        153       102      327s   a tank cannot feel it
```

**From level 60 up the verdict is literally "a tank cannot feel it."** At 90 the boss's swing costs you
**1% of your pool** and you stand there **327 seconds — five and a half minutes — with no healer at
all.** Your *"tank just sits and some1 chips away at it"* is not an impression; it is what the numbers
say, and `BL-13` never saw it because it was measuring a character nobody plays.

### 🔑 Which answers `BL-168` without needing the raid question

At 90, 343,474 HP buys 196 seconds. To land in **your own 600-1800s band with the 5-man party**:

| target | HP needed at 90 |
|---|---|
| 600s (10 min) | **1.05 kk** |
| 1800s (30 min) | **3.15 kk** |

**Your "3~6kk" is not raid content after all — 3kk is a 28-minute fight for a FIVE-MAN**, right at the
top of the band you set. 6kk is 57 minutes, which is where a raid (or your solo-boss ×4 damage forcing
a faster kill) belongs. So your own two shapes fall out of the measurement exactly as you drew them:
**~3kk = the escorted field boss, ~6kk = the solo/world boss.** My "you need a raid or the band dies"
was an artifact of the broken rig, and I withdraw it.

⚠ **One thing for your eye before I refit the curve: there is a CLIFF AT 80.** Party dps goes
534 → 1,884 between 76 and 85 as the gear tier flips to S. So the boss HP curve has to **climb steeply
there** — a flat multiplier across the whole range cannot land both ends in the band.

### 2d. And what your ladders do, computed on YOUR numbers rather than the rig's

Boss P.Atk 14,500 at level 90; your block is 75% × 20% at A grade, 75% × 25% at S.

| | epic A — 2,600 P.Def / 17k HP | MYTHIC S — 4,300 P.Def / 20k HP |
|---|---|---|
| today (×4 rank) | 365 avg/swing = **2.1% of pool** | 211 avg/swing = **1.1% of pool** |
| escorted boss ×2 | 730 = 4.3% | 422 = 2.1% |
| solo boss ×4 | 1,460 = 8.6% | 844 = 4.2% |
| solo + 20-min enrage (×8) | 2,920 = 17% | 1,688 = 8.4% |
| solo + 30-min enrage (×16) | 5,840 = **34%** | 3,376 = **17%** |

🔑 **Two things fall out, and both are yours confirmed.** First, your *"what happens with S mythic"* has
an answer: **the endgame tank is TWICE as immune as the A-grade one** (1.1% a swing against 2.1%) —
gear outruns the boss, which is the ratchet that has to be broken. Second, **your enrage ladder is
exactly the right shape**: it does nothing early and at 30 minutes it is taking a sixth of an
endgame tank's bar per swing. That is a soft enrage that enforces your own 10-30 minute band instead
of a number in a comment enforcing it.

---

## 3. Your five asks, costed

### (a) The `solo boss` passive — ×2 P/M.Atk for a boss with no escort
**Cheap and correct.** It is a field on the profile, not a new mechanic. It also names something the
game already has and never labelled: `valley_treant` calls two adds at 50%, the three dungeon bosses
stand alone, and nothing in the code knows the difference.

### (b) Every boss carries a War/Spell Rune — ×2 P/M.Atk
**Cheap, and there is already a precedent to copy**: `demo_seraph_rune` and the four guard templates
hold `ItemCatalog.WarRune` through `MobBuild.Held`, and it measures ×2.00 P.Atk. Two ways to do it —
give the boss the real held rune (self-documenting, shows on the inspect plate) or fold ×2 into the
rank. **I'd give it the real item**: a player who inspects a boss should see *why* it hits that hard,
and it costs nothing.

### (c) Overpowered single/AoE skills — 1200/1500 on the same tank
**This is the part that needs the least new machinery and buys the most.** `BossCatalog` already
takes a rotation with HP windows; what is missing is that the three dungeon bosses each have exactly
**one** real attack (`BossSlamSkill`) and the world boss has two. A boss's damage should arrive
through its *kit*, not through its auto-attack — that is what makes a fight a fight rather than a
tank-and-spank.

### (d) The enrage ladder — 20 min ×2, 40 min ×4 (field/dungeon); 2h / 3h (world)
🔴 **The timer is currently at NINETY SECONDS, and I doubt you knew that.**
`GameLoopService.BossEnrageTicks = 900`, and the loop is 10 ticks/sec — so **900 ticks = 90 seconds
of engaged combat**, not 900 seconds and nowhere near 20 minutes. It fires **once**, for **×1.5**, and
never again. So today: every boss in the game enrages a minute and a half in, by half, forever.
Your ladder replaces it with 20 min ×2 → 40 min ×4 (field/dungeon), 2h / 3h (world).

### (e) HP — *"bosses 85 with 3~6kk hp not like out 350k"*
🔴 **This is the one that collides with a ruling of yours, and I am not going to build it quietly.**
Your `BL-13` ruling (playtest 25) is *"the bosses should take 10-15 even 30 mins to kill"*, and the
whole `MobRankScale.Hp` curve was fitted **by measurement** to land every boss in the game inside
600–1800s **for a 5-man party**. At 90 it measures 1,274s — 21 min, dead centre.

**×10 the HP and that 5-man party needs three and a half hours** — and that is the *ceiling* estimate
with no downtime, no deaths, no adds and no running back in. Then the ×4 attack ladder makes it
worse, not better: more healing needed means fewer DDs, which means the fight is longer still.

The two are not in conflict in IG, because **a 3–6kk boss there is a RAID boss** — several parties,
not one. Our number is small because it was fitted to the party you prescribed. So:

---

## 4. The proposal — split the rank, and the contradiction dissolves

`MobRank` is `Normal / Elite / Boss` today, and `BL-13`'s own note already says *"a world boss has no
rank of its own … `BL-13` still says it wants a rank of its own"*. Your own message draws the same
line — *"(only field/dungeon bosses, world once the it times will be 2h and 3h)"*. So make it real:

| | **Field / dungeon boss** (`MobRank.Boss`) | **World boss** (`MobRank.WorldBoss`, new) |
|---|---|---|
| Fought by | the 5-man party of `BL-13` | a **raid** — several parties |
| HP | today's curve (~343k at 90) — **unchanged, it measures right** | **3–6 kk**, your number, authored per boss |
| Enrage ladder | 20 min ×2 → 40 min ×4 | 2h ×2 → 3h ×4 |
| Rune ×2 | yes | yes |
| `solo boss` ×2 | yes, if it has no escort | yes, if it has no escort |
| Respawn | as now | as now (21h ± 3h) |

That gives you **every number in your message** with nothing overruled: the 3–6kk lands on the world
boss where IG puts it, and the field/dungeon boss keeps the 10-30 minutes you ruled for it — while
still gaining the ×2/×2 attack ladder, the timed enrage and the real kit, which is what actually fixes
*"cannot kill my tank"*.

### ⚠ The ceiling warning I gave you first was overstated — here is the corrected version

**What I said (2026-09-05, first pass):** ×2 solo × ×2 rune on today's ×4 = ×16, so the basic goes
728 → 2,912 and the sustained damage 442 → 1,768 against a 391 healer ceiling — *"a 4.5× deficit, not
payable for a 5-man"*.

**That was computed on the rig's unbuffed 2nd-class Knight** (§2b), not on your Paladin. On your real
tank the same ladder reads:

| | today | + `solo boss` ×2 + rune ×2 |
|---|---|---|
| Boss P.Atk | 14,500 | 58,000 |
| Basic on your 2,600 P.Def | 400 | 1,600 |
| After your 15% average block | **340** | **1,360** |

So the ladder is **×4 on what you actually take**, not the 4.5× *deficit against the healer* I
claimed. Whether one healer covers 1,360 a swing depends on the boss's swing rate and on your real HP
pool, which I do not have — but it is close enough to the line that **it may well be right for a
5-man, and my warning was too strong.** I am not going to replace one hand-derived number with
another: `BL-169` fixes the rig first, then this gets re-measured properly, and your *"not one
shooting but a tank can feel it"* is still the test.

---

## 5. What I need from you before building

1. 🔑 **Is a field/dungeon boss still a 5-man fight, and a world boss a raid?** Everything above turns
   on this. If you want *every* boss at 3–6kk HP, then bosses are raid content across the board and
   `BL-13`'s 10-30 min band is withdrawn — say so and I'll refit to the raid instead.
2. **How big is a raid?** Two parties (10)? Nine (45)? It sets the HP fit and nothing else.
3. **The escort — "fighters which have more p atk than bosses".** Are those a **hand-authored roster
   on the profile** (this boss has these five named creatures at these levels), or the generated trash
   already standing around it? The former is what your sentence sounds like and is what I'd build.
4. **"Given decrease in stats" for a boss with fighters — how much?** Your two examples read as
   ~3kk HP / mage lean / lower P.Def for an escorted boss vs ~6kk / high P.Atk + P.Def for a solo one.
   Confirm those are the two shapes and I'll author them as two presets a profile picks from.
5. **The 1200/1500 skill numbers — are they against the same 2.5k-def tank, and is 1200 the single
   and 1500 the AoE?** If so the AoE hitting harder than the single is deliberate and I'll build it
   that way; it's the reverse of the usual.
6. ✅ **ANSWERED — the 400 is real and the model reproduces it.** Your numbers exposed `BL-169`
   instead; see §2b. What I still need is **your HP pool at 90**, because "a tank can feel it" is
   measured as a share of that and I only have the rig's (wrong) figure.
7. **Was the escort that nearly killed you the guard pair (`GuardTank` / `GuardArcher`)?** If yes,
   they are already the template for what a boss's fighters should be.
8. **Confirm my reading of *"blocked 75% of the time for 20% dmg"*** — I read it as 20% *removed*
   (so a blocked hit still lands 80%), which is what `BlockReduction` means in the code and is exactly
   the A-grade shield's authored 0.20. If you meant a blocked hit deals only 20%, the mitigation is
   60% rather than 15% and §2b's bottom line moves a long way.

---

## 6. BUILT — 0.113.0, 2026-09-05

Your ruling: *"bosses as we desided get x2 atk and x10hp, if boss is solo gets another x2 on both
(atk/hp) … Now I just want to feel it and going solo vs boss to be nearly impossible"*. Built as
`BL-166` (the stat block) + `BL-167` (the ladders + the real enrage timer). `BL-168` is closed by it.

| | escorted | solo |
|---|---|---|
| attack | ×2 | ×4 |
| HP | ×10 | ×20 |
| enrage | ×2 at 20 min, ×4 at 30 (world boss: 2h / 3h) | same |

**Your HP numbers land exactly.** At level 90 a boss is now **3.43 kk escorted / 6.87 kk solo** —
against your *"bosses 85 with 3~6kk hp"*. Nothing was fitted to get there; your two ×2s produced it.

```
 Lvl      rank    boss HP   party dps      TTK           band
  44      Boss    2429821         370    6562s TOO SLOW (109m)
  44 Boss/solo    4859643         370   13125s TOO SLOW (219m)
  60      Boss    2814530         558    5047s TOO SLOW (84m)
  76      Boss    3158218         534    5911s TOO SLOW (99m)
  85      Boss    3338544        1884    1772s    ok (30 min)
  90      Boss    3434746        1752    1960s TOO SLOW (33m)
  90 Boss/solo    6869493        1752    3920s TOO SLOW (65m)
```

✅ **The endgame is right** — 30 to 33 minutes escorted at 85-90, 65 solo, which is your band plus the
"nearly impossible alone" you asked for.

🔴 **Below 80 it is not, and by a lot.** A level-44 dungeon boss now takes a full party **109 minutes**
escorted and **219 solo**. And the attack half crosses your own red line down there: at 20-44 a single
basic attack takes **91-95% of a robe's pool**, which is *"one shooting"* in the plainest sense.

**The cause is not the boss curve — it is the CLIFF AT 80.** Party dps is flat at ~370-560 from 44 all
the way to 76, then **triples to 1,884 at 85** when the gear tier flips to S. So a flat ×10 lands the
top correctly and overshoots the bottom by three to seven times; no smooth boss-HP curve can fix that,
because the thing that is not smooth is the party.

⚠ **I left your numbers exactly as ruled and am reporting this rather than quietly tapering them** —
the endgame, where you are playing, is right, and the low end is a decision about the gear ladder
rather than about bosses. Three ways out, and it is your call:

1. **Taper the ×10 by level** — ~×3 below 76 rising to ×10 at 85+. One knob, keeps the endgame, fixes
   the bottom, but bakes the cliff into the boss curve where a future gear change would inherit it.
2. **Fix the cliff itself** — the S-grade jump is the real outlier and it distorts every endgame number
   in the game, not just bosses. Biggest fix, widest benefit, and it moves numbers you have signed off.
3. **Leave it** — the low-level bosses are the three dungeon bosses at 44 and 65 and the world boss at
   60. If nobody is fighting them right now, this can wait until the bot party exists and the whole
   band gets re-measured at once.

---

## 7. CORRECTION — the treant is a FIELD boss, and a world boss is a tier above (0.113.1)

Your correction, hours after 0.113.0: *"The treat is field boss (same as dungeon one) world boss is a
clan/party of clans mass pvp massacre where the boss is the target ..and that boss will have about
x2~3 aditional stats and x10 additional hp .. So now if boss have 28k p atk/6kk hp a world one will
have 50~60k p atk and 120kk~180kk hp(6kk x2~3 x10) so several parties can fight it while fighting
others for the best loot in the game"*.

🔴 **I mis-filed the Valley Treant.** 0.113.0 put it on the 2h/3h ladder because it respawns every 21
hours — **I read the respawn as the classification.** A world boss is a kind of *encounter*, not a rare
spawn. It is back on the field ladder, and there is now no world boss in the game at all.

**The stat rung is built**: `BossProfile.World` = ×2.5 every stat, ×25 HP, both over the SOLO boss
(which is what your own "6kk × 2~3 × 10" starts from). At 90:

```
  90       Boss    x527      3434746    1544        1752    1960s TOO SLOW (33m)
  90  Boss/solo   x1054      6869493    1544        1752    3920s TOO SLOW (65m)
  90 Boss/WORLD  x26340    171737328    3860         694  247403s      clan raid
```

**171.7 kk, inside your 120-180kk.** ✅

### Two things it leaves for you

**1. 172 kk is a very large pool for our damage model.** One 5-man does 694 dps against its 3,860 P.Def:

| raid | time to kill |
|---|---|
| 1 party (5) | 69 h |
| 9 parties (45) | **7.6 h** |
| 34 parties (172) | 2 h |

Even allowing that most of a mass-PvP fight is spent on *people* rather than the boss, 7.6 hours for
nine full parties is a long evening. Either the pool comes down or a world boss is deliberately an
all-server, all-day event. One constant either way.

**2. Your two examples disagree, and I followed the HP one.** *"28k p atk → 50~60k"* is ×2 off an
**escorted** boss; *"6kk × 2~3 × 10"* is off a **solo** one. I used the solo base for both, so P.Atk
lands at **~128k** rather than 50-60k. On your mythic-S tank:

| | per swing after block | share of your 20k pool |
|---|---|---|
| ×2.5 (built) | 1,857 | **9.3%** |
| your 50-60k | 873 | 4.4% |

4.4% is *softer than a solo boss already hits you* (4.2%), so I think 128k is the right game and the
50-60k is the arithmetic slip — but it is your number and one constant, so say which.

**3. And the encounter does not exist.** It needs a place (an open field — the PvP is the point), the
mass-PvP rules (does the zone force-flag? does karma apply? how do several clans contest a kill?) and
the loot. That is `BL-171`, and it is a design pass rather than a number.
