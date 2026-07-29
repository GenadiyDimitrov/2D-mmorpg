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
