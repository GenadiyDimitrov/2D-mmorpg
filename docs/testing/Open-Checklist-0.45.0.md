# OPEN CHECKLIST — everything still untested as of 0.45.0 (2026-08-02)

Edit this on the phone: write your comment after the `->`. Put `x` in the `[]` if it passed with
nothing to say, `~` if it works but wants a change. Ids match `docs/testing/TestChecklist.Unity.md`

**Before you start:** install the 0.45.0 APK (protocol 11 — an older APK will be refused) and
**DELETE `Game.Server/game.db` + `-shm` + `-wal`** (old saves carry the old multi-attribute rolls).
Titles must be tested on a `test1..9` character — admins are excluded from every leaderboard.

---

Added [!] symbol in the file - that is priority or bug or not working properly
Added [?] symbol in the file - a question
[] without numer/id infront is mine for the current section

---

## others

[?] - How to start offline farm ? 
* in WPF there was a go offline button or when i leave to char select 
* but now i cannot leave in combat to char select nor there is [Offline] button 	

[!] - with the 1st character In the acc I put basic attack action on the bar and made it auto on ...(then delete that char) then entered with the newly created second char and when I put the action ot the bar it was already on.... When I removed it from the bar it still acted in a auto-farm. The actions should act as a skill for the character not for the account. Also removing something from the bar automatically disables the auto-on..when u put it back u need to reactivate it.

[!] - Opening compare of a pendant shows the details window of a ring.

[!] - Heavy Draw, Twin Blade should not exist. They are learned after lvl 20 and they are weaker than the actual csv skills - u can give me list with what is outside the csvs so i can tell you what skilsl to be removed

[!] - Quest items must not be shown in the selling vendor list or in the keeper 
* quest items are in their own bag, 
* unless is specifcally told that this quest item is tradable/sellable and it will go inside the normal inventory not the quest bag

[!] - Reloging make my auto farm timer to reset - server did not reset just the timer .. farmed for 15 mins went to town reloged then came back start to auto farm and timer from 7h44 to 7h59 ..	
	
[~] - Chat must reset on exit - I created a char start to play then deleted this char and made new one and when I entered the game my chat is full with the previous character. Each relog clears it and and have a limit for the last 1000 messages or something. 

[~] - newbie equipment should be untradable and unsellable. Even timed like a rune for 30 days.

[~] - items with time remaining like the rune and the newbie items should show time remaining inside details panel. Green for over a 7 days. White for over a 1d. Yellow for over 1h, Red in till dissapears.

[~] - Buff potions/scrolls should have auto-on on the hotbar .. same logic as buffs (they act as a buff they should be threaded as one)

[~] - Quests to show in NPCs window when onyl the actual NPC have something to deal with that quest - now every NPC I open it shows 3 quests ...

[~] - Quest details to show only after opening details .. not in the NPC window .. everywhere else to have only Name

[~] - Gatekeepers to have tabs - Zones/Cities - now everything is one big list

[~] - Bag -> Items - also to have Tabs or filter - Equip/Consumables/Mats - and to be ordered by name

[~] - Vendors that u sell to and the keeper to have the same filter - Equip/Consum/Mats

[~] - Having mytic F ring + Uncommon E ring -> equiping another E ring swaps it with the uncommon E - not the F ring .. swappin logic should not be only for rarity - make it for rarity + grade or just Mdef value

[~] - Opening item compare window + the window for details - should ackt as one - separate from Details window of item
* They must be as one (like the quipment panel in the bag) - You click compare and it extends to left and shows the equiped item details 
* The equiped item part dont have the bin/unequip buttons - its a acomparison part/column/side - only item details for equiped
* If I select item from inventory then click compare - it will show left side the equiped details (nothing more) and on the right the selected item details + [bin][euqip] buttons
* If I select item from equipped panel then click compare - it will show left side the equiped details (nothing more) and on the right the same item details + [bin][uneuqip] buttons
		
[~] - Scrolls of return drop rate should be cut 20 times more - at lvl 23 i have 550 .. when im returning ill need 5-10 to keep .. the other ill sell ... so 500* 20 = 10k .. not much but no need to fill up count that much - I use ~1 per ~250 dropped

[~] - Healing potions drop rate cut 10 times more - now i have 200C and 120U at lvl 23 .. should have ~20C and 0U and if i need them i need to buy them 
* the uncommon should start to drop after 40 and rare after 61 - both at that commo
		
[~] - Buff scrolls and potions
* Now my inventory is filled with buff scrolls and potions
* Remove all the buff scrolls from drops - even bosses
* Buff Potions selling price at 0 
	-> no potions gold farm 
	-> they must be buff aid not exploit 
	-> they can be traded/sold - if I dont need force potion and mage wont need might we can trade, or if I want to get rid of them just sell for 0 or destroy
* Buff potions should have 2 rarities - 
* Dash potions are drop only (and bossPoints) - no vendor (their sell price can stay) - they are not exactly a buff potion - they are clasified as one but act different (leave it be)
* Remove all the single buff scrolls that are not max lvl 
	-> each buff scroll is rare quality only 1 level that is the max for the buff - no need for 6 scrolls for 1 buff
* Add apothecary to sell buff scrolls selection boxes
	-> For 250k select up to 10. 
	-> having buffer or buffing from npc is cheaper
	-> for lvl 76+ to have the 19 buffs u will need to buy 2 boxes = 500k ... u probably can farm that much for 1h but having a buffer is still better
* Buff scrolls from those boxes are untradable/unsellable - boxes are tradable and sellable(price/25) but scrolls are not
		
[~] - For NPC-s add [speak] button (on place where the [attack] button of monsters is) - and clicking on that button it start to move me in range and opens dialog

[~] - Add /offline command + [Offline] button - for going into offline farm mode - to goes to char select and on that char that is offline farming to show timeleft

[~] - We need the craft - professions, window, etc .. now even in admin the only mytic are the set, everything else is epic rarity ... 

[~] - Add Feretite Wand in the Newbie selection box

[~] - When you equip a 2h weapon can the offhand slot get the same visual ? or becomes disabled with the same visual -> Feretite Battle Staff -> [Fba][Fba-disabled] ?

## 43. ACCURACY, ATTRIBUTES, SCROLL WINDOWS (0.45.0 — newest, unplayed)

43a [X] - A same-level mob is a fair fight at EVERY level. Try a ~level-20 character and a 70+ one against normal same-level mobs; neither should whiff constantly, the endgame one especially (that is the bug being fixed). -> 

43b [X] - Level gap still bites. A mob 10+ levels above you is still hard to hit, 20+ is still a full lockout. Only the same-level baseline changed. -> 

43c [X] - Rogues dodge, fighters land. A dagger rogue with Evasion Mastery should visibly eat more misses than a knight; a fighter with Precision should keep landing on something evasive. -> 

43d [X] - Watch mob crit/attack speed. Mob DEX fell from ~100 to 30 at level 90, and DEX drives crit + swing speed, so endgame mobs may now be too soft. Say if they feel like paper. -> 

43e [X] - A fresh drop is BARE. Farm until a weapon or jewel drops: no attribute line on it. Armor must never show one at any quality. -> 

43f [X] - "Can roll" is on the item page. Open any D-or-better weapon/jewel: under the stats it lists what that base COULD carry and the range, e.g. `Crit Rate 10~30%`. This is how you decide if a base deserves a scroll. -> 

43g [X] - The scroll windows exist at all. Bag -> tap an enchant or attribute scroll -> Use -> a list of legal targets -> confirm. Neither had ANY phone UI before this build. -> 

43h [X] - The target list is FILTERED. Common attribute scroll offers only D/C/B weapons+jewels, Epic only A, Mythic only S. Uncommon/Rare/Legendary must not offer a bare item (they re-roll, they can't create) — empty list with a message if you own nothing eligible. -> 

43i [X] - Each scroll does its own thing. On a B dagger: Common = random attribute of its three, Uncommon = same attribute new number, Rare = same attribute in the TOP HALF. On an A weapon Epic=Common behaviour, Legendary=Rare behaviour. On an S weapon Mythic rolls a type at MAXIMUM every time. -> 

43j [X] - Ranges match the table. Spot-check: magic weapon cast speed tops at 15%, bow crit damage at 35%, ring HP regen at 5%. An S item always rolls the single top value. -> 

43k [X] - A refusal costs nothing. If the server rejects a use (wrong grade, nothing to re-roll), the scroll is still in your bag afterwards. -> 

43l [X] - The stat actually moves. Put an Attack Speed roll on a worn sword and watch the sheet/swing rate change, then unequip and it goes. Same for a bow's Accuracy % — it must MULTIPLY your finished accuracy, not add 30 flat. -> 

43m [X] - Enchant failure behaves as advertised. The confirm box states the odds and what failure costs: Common scroll failing DESTROYS the item, Uncommon resets to +0, Rare drops 1. -> 

43n [!] - Soulcrystal gear reads S. Level-80+ items (Soulcrystal / Starstone / Seraphite) were mislabelled A by the pricing enum. They must show S — and only a Mythic attribute scroll may touch them. -> the item details says A grade but the scroll it uses is mytic

[~] - enchant system should be reworked - there will be 3 type of scrolls (the current rarity logic)
 * "Scroll of enchant"(normal)  - current common  - brakes on fail
 * "Greater Scroll of enchant" - current rare - on fail -1
 * "Safe Scroll of enchant" - new effect - keep current enchant level
 * common is for E
 * uncommon is for D
 * rare is for C
 * epic is for B
 * legendary is for A
 * mytic is for S
 * drops are like the attri just grade lower (common from @20+,uncommon@40+,rare @52+,epic @61+, legend from elites at low chance and bosses higher @76+, mytic from bosses/instance bosses @80+ and dungeon monsters for @90)
 * safe enchant scrolls drop only from bosses at very very low chance, greater scrolls drop from elites at very low chance, normal scrolls drop like current uncommon. (For grades below A where are no elites ..there will be instances and the greater and safe will be got from there)
 
[~] - attri scrolls drop rates -> should start to drop after 40 for common 52 for uncommon and 61 for rare, epic from 76,legend from bosses 76+,mytic from bosses/instance bosses 80+ and dungeon monsters for 90

[~] - add all scrolls to the admin menu

[~] - add admin command /enchant <value> opens item selection to select any weapon or armor and enchant it to the Value. Unrestricted can do F grade +999999 (/enchant 999999 -> selects F weapon)

## 42. TITLES, CHAT TABS, LAST ACTIONS (0.44.0, unplayed) — use test1..9, NOT admin

42a [X] - You are told when you win a title. Log a plain character; within ~5 min a green line arrives: "You now top the Level board — the title «the Ascended» is yours to wear (Rank window)." -> 

42b [X] - The picker. Menu -> Rank -> Titles (7th tab): "No title" plus one row per board you top, each with Wear; the worn one reads Worn in green. Holding none shows the "reach #1 on any board" note. -> 

42c [X] - It appears over your head. Wear one: a small gold line above your name reading `the Ascended`, no quotes. "No title" removes it. -> 

42d [X] - Other players see it. With two characters online (second phone, or offline-farm one and log the other), the title shows over the OTHER character's head — it rides the entity snapshot. -> 

42e [X] - It survives a relog. Wear a title, exit to character select, come back: still worn and drawn. -> 

42f [X] - A title is HELD, not owned. Give a second character more gold (`/gold`) and wait for the refresh: the Wealth title moves. The loser gets "«the Wealthy» is no longer yours" and their plate line goes, with no re-picking. Win it back and it returns automatically. -> 

42g [X] - The cast bar still clears it. Cast something while wearing a title: the cast bar sits ABOVE the title line, not through it. -> 

42h [~] - The button is "Chat" now and the window is titled Chat. Five tabs: All / Local / World / PM / System. The old console is the System tab — nothing that used to be in the Log is missing. 
* Add another tab that is dmg and drop [Combat]
* remove the dmg + dropped items + exp (You -> Enemy <dmg>; You looted: <drop>; Exp: +e, Sp: +s, Gold: +g ) from the system and put them in this [combat] tab
* make the [combat] tab to open on separate window
* make your dmg green(not lime), and enemies dmg red

42i [X] - Colours and tags. Plain = white, Local tab. `!hello` = gold, `[W]`, World tab. `/w Name hi` = violet, `[PM]`, PM tab. Server lines = green, System. All shows every one of them, tagged, in arrival order. -> 

42j [X] - A tab shows only its own. In World, nothing local/system is listed; back in All everything is still there (filter, not wipe). Clear empties everything. -> 

42k [~] - Reply. Have someone whisper you, Chat -> Reply: the command box fills with `/w <name> `, keyboard opens, caret at the end. Type and send. -> same problem with the general texbox. It auto fill /w name, but when I start to type it overrides it dosnt edit it

42l [X] - The log still behaves. Spam combat for a minute with the window open: no lag spike, no rows drawing over each other, newest line always at the bottom. -> 

42o [X] - World chat has a level floor. On a fresh character under level 10, `!hello` is refused with "World chat opens at level 10. Local chat and whispers work now." while plain chat and `/w` still work. Past level 10 it goes out. Staff are exempt, so test on test1..9. -> 

42m [~] - Whisper is an action. Skills -> Actions -> Whisper: with a player targeted, Use fills the command box with `/w <name> `. With no target it says so. Put it on the bar and the slot must do the same. -> it's for every textbox..when it do "/w test" and when I select/start typing it clears the /w test and the message becomes normal -smae goes for the ip/game connection string and ant textbix ..click stsrt type is clear old value not edit 

42n [~] - The Actions list is complete. It must cover every non-admin command: attack, target-closest, sit/stand, walk/run, trade, party invite/leave/kick/leader, follow, assist, friend add/remove/list, like, block/unblock, whisper. -> 
Now add the missing commands like block or like

[~] - admins and moderators should have their own titles.

[~] - also titles need more "Sass" like the gold one is golden,the online is green, pvp is purplish not same as pvp flag, pk is dark red etc.. And not "the Devouted" but "Devouted" or something or atles t in "the" is capital,and titles are different font

## 41. MOB CAST BAR + TARGET RING (0.43.1, unplayed)

41a [X] - A casting mob shows a bar over its head. Find a caster/ranged mob or a boss: an amber bar with the SPELL'S NAME above the mob's nameplate, filling over the cast, gone when it lands. Your own cast bar stays the centred one at the bottom. -> 

41b [X] - It clears on an interrupt. Interrupt a casting mob (hard hit / interrupt skill): the bar disappears at once instead of running to the end. -> 

41c [X] - Killing a caster mid-cast leaves NO bar on the corpse. This was the bug found while building it. -> 

41d [X] - Several at once. Pull two casters: each gets its own bar on its own plate, and walking away and back leaves no ghost bar. -> 

41e [X] - Your target's name is flanked by two blue circles, round and solid. Tap another mob and they move; clear the target with the X and they vanish; walk the target off screen and they go, and return with it. -> 

41f [X] - They sit OUTSIDE the whole name at any length. Short name, long name, an aggressive mob (`*`), a quest NPC (`!`): the circles clear the text and the glyph rather than overlapping, level with the middle of the name. -> 

41g [X] - They survive a relog and a switching spree. Come back with nothing targeted: no stray circles anywhere. Tap ten mobs in a row: exactly ONE pair, always on the current target. -> 

## 40. THE QUEST WINDOW — three tabs (0.43.0, unplayed)

40a [X] - Three tabs and they are populated. Active / Available / Completed across the top, Active selected. Active looks as before (step line, progress, Where, Track, Abandon) plus a Details button. -> 

40b [X] - Available lists what you have NOT taken. Takeable first, each with giver and town ("From: Elder Marius — Brackenford"); then shut ones dimmed with the reason: `Requires level N`, `Outgrown — level N at most`, `Requires: <previous quest>`. -> 

40c [X] - Nothing from another race or class is listed. A Human Fighter must not see elf-only or mage-only quests anywhere — hidden, not locked. Nor another 2nd class's change chain once you have picked one. -> 

40d [X] - Completed lists NAMES, not raw ids. A daily done today reads "Done today — again after the server day rolls over"; a repeatable you finished sits in AVAILABLE saying "Repeatable — take it again", not in Completed. -> 

40e [X] - Details, from any row of any tab. Description, status, level band, giver + town, then every step in order: `[x]` done, `->` current (with its counter), `·` ahead, each with its own "where". Reward at the bottom. -> 

40f [X] - A contract's gather lines are structured. Details on a Huntmaster contract lists each token: what drops it, how often, how many you carry, and that each pays a % of that creature's own exp and gold. -> 

40g [X] - Accept moved to the detail page. A quest giver now offers a one-line row with a Details button (no wall of text in the dialog); the page carries Accept and Decline. Accept takes it and the dialog updates behind; Decline just closes. -> 

40h [X] - An open detail page stays live. Open Details on an active kill quest, kill one of its mobs without closing: the step counter on the page moves. -> 

40i [X] - Track still works. Pin from Active, tracker appears, capped at 5, and a handed-in quest unpins itself. -> 

[~] - newbie quest must be 10 to 35. Later newbie gear becomes unusable ... Same goes for the other chain quest "Blooded" that requires the "Proper kit"  12~35

## 39. REPEATABLE QUESTS — Huntmaster contracts (0.42.9, unplayed)

39a [X] - The contract is offered and says what it wants. Huntmaster Cera (Brackenford, 700 west of Gatekeeper Pell) at level 3-20 lists Bracken Contract reading `Collects: Fox Pelt (Fox), Werewolf Fang (Werewolf), Barbed Hook (Hook Spider)` — you can tell what to hunt BEFORE accepting. -> 

39b [X] - Tokens drop and STACK. Kill Foxes: each gives a Fox Pelt with a counting chat line, and the bag shows one row x12, not twelve rows. The quest window step reads `Gathered: Fox Pelt 12, Werewolf Fang 0, …` and climbs. -> 

39c [X] - Hand in whenever you like. Cera -> Complete takes every token (one "Handed over 12x Fox Pelt" line each) and pays exp AND gold. Handing in after ONE kill must also work. -> 

39d [X] - It does not close. The moment you hand in, the same contract is offered again and the "!" comes back over her head. Take it again and counts start at zero. This is the whole feature. -> 

39e [~] - The payout is a farm bonus, not a jackpot. Roughly +25-35% on what those kills already gave. A Fox Pelt ~102 exp / 14 gold, a Barbed Hook ~455 / 47. If a hand-in pays multiples of an hour's farming, say so. ->
* Putting part of quest items in the warehouse and then getting them back out (it should not be possible - a problem in different point) the qust item count in the details did not go up, but when i compleated it it take all
* Also make when compleating to show gotten exp/sp and gold - the reward ...

39f [X] - A finite contract repeats too. Huntmaster Radd (Stonewatch, 18-34): Thin the Herd, 20 Grizzly Bears, reward at the end, and STILL on his list afterwards. Ironreach has the same shape (Standing Orders, 25 Redhorn Footmen). -> 

39g [X] - Abandoning destroys the trophies. Gather a few tokens, Abandon: they leave the bag with "Your gathered trophies are discarded." -> 

39h [X] - The daily is unaffected. The Apothecary's rune quest is still once-a-day: hand it in and it must NOT come straight back. -> 

39i [X] - The creatures actually spawn. The server log at startup must not say "Quest kill targets with no dedicated spawner", and you should find Foxes/Werewolves/Hook Spiders without clearing a whole camp. -> 

39j [X] - A class change still works. Quest items stack now and the change consumes ONE rather than removing the row. Take a 2nd class at Class Master Vael: the two proofs are consumed and nothing else vanishes. -> 

## 38. THE ACCOUNT WAREHOUSE (0.42.7, unplayed) — two characters on ONE account

38a [X] - It crosses characters. Deposit a weapon into the Account bank on character A, log out, log in B on the same account, keeper -> Account -> Withdraw: it is there, and taking it out is FREE. -> 

38b [X] - 10k per new slot, merges are free. Empty bank, deposit a stack of a material: gold drops 10 000 with a system line. Deposit MORE of the same: merges, gold does not move. Something different: another 10 000. Under 10 000 gold a new-slot deposit is refused with the price in the message. -> 

38c [X] - Tradable only. A quest item / untradable piece must not even be LISTED in the Account deposit tab (it still lists in Private). Private stays free for everything. -> 

38d [X] - Town only. In a field both banks refuse with "You can only reach your warehouse in a town." -> 

38e [X] - Both characters see ONE bank. Leave A offline-farming, log in B, move something in the account bank from B, bring A back: A shows the CURRENT contents. No item may be duplicated by this — that is the real risk. -> 

38f [X] - A rune still expires in there. Park a war/spell rune in the account bank: no buff, but the clock runs and it vanishes when it runs out. -> 

## 37. PARTIAL-STACK TRADING (0.42.6, unplayed) — needs a second character (bot: `tools/SmokeTest -- bot`)

37a [] - Offering part of a stack. Tap a stack of 50 potions in the trade bag: the numpad opens ("Offer", max 50) and its subtitle counts what you KEEP. Offer 20: the offer list shows x20 and the bag row stays, reading x30. Tap again to add more, up to 50. -> 

37b [] - Non-stackables still toggle. Tap a weapon/armor piece: no numpad, straight onto the table, second tap takes it back. -> 

37c [] - The split is real. Complete a 20-of-50 trade: you keep 30, they gain 20 MERGED into the stack they already had, not a second row. -> 

37d [] - A shortfall kills the whole trade. Offer 20, then drink or sell some so you hold fewer than 20, both confirm: "Trade failed (items/gold changed or bags full)" and NOTHING moves. Neither side may end up with 14. ->

37e [] - A full bag is judged correctly. Nearly full bag: receiving a stack of something you ALREADY hold succeeds (merges, no new slot); something new is refused. Giving away part of a stack must NOT count as freeing a slot. -> 

## 36. MOB REGEN + the 0.42.3 batch (unplayed) — the one that needs real play

36a [X] - An improved buff's popup shows its EFFECT. Tap a group buff square (admin buff is the quickest source): the body must read real numbers — "+15% P.Atk & P.Def, 9% melee vampirism, +4 accuracy" — the way a Harmony buff reads, not `Parts: Might and Bulwark`. -> 

36b [X] - Nothing out-heals you any more. Chip at a mob a few levels above you with weak unbuffed damage: the bar must go DOWN and stay down. Before, a level-37 mob regenerated ~29 HP/s and a level-90 one ~1170 HP/s. -> 

36c [X] - The 20-second window. Take a mob to ~30%, run out of its view so it disengages, then watch it: it must walk home wounded and climb back over ~20 seconds, NOT be instantly full. Re-engage mid-climb and it is still hurt. -> 

36d [X] - The damage ledger survives a disengage. Take a mob to ~30%, run, and let it be killed while still healing (come back and finish it, or the bot): you must still share exp/drop credit. Then let one heal ALL the way to full and kill it fresh — that time the ledger must have RESET. -> 

36e [] - A boss re-pulled while wounded does NOT dump its phase script. Pull a boss past a phase threshold, break combat, re-engage before it heals: it must CONTINUE, not replay every announce/enrage/add-wave at once. A boss that disengaged enraged must still be enraged, losing it only at full health. -> 

36f [] - Safe-zone kiting is self-limiting. Aggro something, step into town so it resets, step out: you may re-engage it wounded, but it heals 5%/s while you wait, so hit-and-run must not let a weak character grind down something far above them. -> 

36g [X] - The two new tuning rows. Debug -> tuning: `Mob regen in combat (frac/s)` and `Mob regen idle (frac/s)`. Change, Apply, and the echo comes back clamped (combat 0-0.1, idle 0.001-1) and live without a restart. -> 

## 35. PLAYTEST-16 FOLLOW-UPS (0.42.1/0.42.2, unplayed)

35a [X] - A set says what it DOES. The piece list and filled slots were right, but never the reward — the bonus lines (actual stats, and anything gated on more pieces) must be on the set panel next to the pieces. -> 

35b [X] - One confirmation at a vendor, not two. The detail belongs on the ROW itself, better worded, for ALL vendor items not just stackables; for a consumable the NUMPAD is the confirmation, one tap away. Check buy AND sell, and that a non-stackable can still be read before buying. -> 

35c [X] - Every drop row carries its OWN %. The group title shows the group's chance; the indented item rows must print their individual chance — what YOU actually get per kill. -> 

35d [X] - Mastery/passive lines group by WEAPON, not by stat. One line per weapon group with its stats after it, weapons sharing a number folded together — not "P.Atk: sword +10, blunt +10 / cast: sword +10, dagger -100". -> 

35e [X] - A hold registers while your finger is still DOWN. Hold a buff square or skill slot: at 0.65s the menu appears under the finger, and letting go must not also fire the tap. Then scroll a long list slowly — that must NOT count as a hold (40px of travel cancels it). -> 

35f [X] - A box's contents appear in the bag immediately. Open a newbie box, a selection box and a shot/rune box with the BAG OPEN: the box goes, contents appear, no re-open. Same at a vendor with the sell list open: sell one and the row goes. -> 

## 34. THE GROUP BUFF IS ONE BUFF (0.42.0, unplayed)

34a [X] - The group eats its singles and cannot be undone. Take Swift and Agility from the NPC buffer, then have a Warchanter cast Swift and Sure: both squares vanish, ONE replaces them, numbers are the group's max rungs. A Rare Agility potion is then refused, STILL IN YOUR BAG, evasion unmoved. Same for the cleric's own Agility. -> 

34b [X] - The group's square behaves. One timer (the group's), the tap popup lists all parts and their numbers, press-and-hold removes the WHOLE thing, and afterwards the singles do NOT come back. -> 

34c [] - Buff scrolls are finally CONSUMED. Read a Scroll of Might: the buff lands and the stack drops by one. Before 0.42.0 all 48 buff scrolls read for free for ever. Check a potion's count too. -> 

34d [] - A scroll that would be refused is not read at all. Under a stronger buff, pressing it gives "would have no effect" BEFORE the 1s cast — no cast bar, no cooldown, nothing lost. Interrupt a scroll mid-read (walk into a mob): it must survive that too. -> 

34e [X] - Two groups that share nothing coexist. Might and Bulwark + Swift and Sure both up, two squares, all eight numbers live. A HIGHER rank of the same group replaces the lower rather than being refused. -> 

34f [X] - Admin buff = 9 rows. Five groups + three Harmony + Frenzy (the only family no group contains). Every other single is refused by the group covering it. Fifteen loose squares means the covering rule is not firing. -> 

34g [X] - Relog. A group comes back as ONE square with LESS time, full numbers intact, applied exactly once. -> 

34h [X] - The autopilot knows. With auto-buff on and a group up it must not re-cast every cycle (watch MP), and auto-potions must not drink a Might potion under Might and Bulwark. When the group expires it may cast again. -> 

[~] - New buff "Madness" or something that is max lvl Frenzy buff (no stat change) just a party buff like an Improved Frenzy
* give the healer all the single buffs + frenzy single 
* The party buffs + harmonies are buffer(warchanter descipline) only
* There should be 2-3 more harmonies and 1-2 improved buffs for lvls after 76

## 33. THE POTION SPLIT — leftovers (0.40.0-0.41.1)

33b [X] - The class buffs cast the same numbers they always did. Levels 1-4 of Might and Bulwark / Force and Ward / Focus and Ferocity / Body and Soul / Frenzy were re-authored as groups but must not change a number. One deliberate exception: M.Atk moved out of Might into the FORCE family — check a mage's M.Atk before/after and that Force restores it. -> 

33e [X] - The admin buff button gives everything INCLUDING Harmony. (Superseded in shape by 34f — expect 9 rows — but the point stands: Harmony of Protection/Warrior/Wizard is reachable no other way, and this is the only way to see a fully buffed character.) -> 

33h [X] - Aim, the accuracy potion. Accuracy mirrors evasion: +1 / +2 / +4, a potion AND a scroll at Common/Uncommon/Rare, vendor-stocked at Common. It sits next to Agility in the Apothecary, and a cleric's Aim and an Aim potion do not stack. -> 

33l [] - The improved buffs are PARTY buffs (the eating half is verified, this half is not). Cast one in a party: it lands on every member within 800 range, not just the target. Same for Harmony. -> 

## 32. PLAYTEST-15 LEFTOVERS (built across 0.35.0-0.39.0, still untested)

32a [X] - The phone server starts with no hand-editing. Unzip a fresh Game.Server on the phone and `dotnet Game.Server.dll` must just run — no `GC heap initialization failed (0x8007000E)`, no editing runtimeconfig.json after every update. -> 

32b [] - Class change applies without a relog. Finish the class-change quest and take it at the class master: the class updates immediately and the Skills window shows the new unlearned list at once. -> 

32h [] - HP potions drop less. Infinite potions make you unkillable; it is the potion FAUCET being closed, not the damage. -> 

32j [X] - Starter gear numbers. Training (Wooden) shield 35 def, Ferrite Aegis 90 pDef at Mythic, ALL training weapons show 5 mAtk, training wand pAtk 6 / mAtk 7 and NO +6 maxMP. -> 

32k [X] - Auto-farm retaliates. A mob that is hitting you outranks the nearest one as a target. -> 

32l [X] - NOTHING walks you into melee unless you commanded it. Four checks: (1) auto-farm with Attack not on the bar stands still, an active skill closes only to CAST range — for a FIGHTER too; (2) a skill alone never closes, as fighter and as bow rogue; (3) the melee combo survives, because tap-tap IS a command — a skill after it resumes auto-attack; (4) a walk order or follow cancels the standing order. -> 

32m [X] - Tap-to-target, tap-again-to-attack, and the Attack button means the same thing. First tap opens the target window, a second on the SAME target attacks (melee walks in, bow shoots from range), a party member follows. Do the identical checks with the Attack ACTION on the bar and the Attack BUTTON on the target frame — one code path, must not differ. -> 

32o [] - Escape/return scrolls can be sold. They are tradable but the vendor refuses them. -> 

32p [] - Buff potions sell at ÷25 like everything else: 1500/25 = 60, not 450. -> 

32s [] - Your own party can NEVER be hit, and the tap follows them instead. Tap a party member twice: "You follow X.", never a swing — with PvP ON and with a RED party member too, and with an offensive SKILL. Then confirm a NON-party player CAN be attacked on the second tap with PvP on, out of town (that half was genuinely broken). -> 

32t [X] - Jewels have designated slots. The paper-doll shows Neck / Ear / Ear / Ring / Ring and an empty square names its slot. Equipping swaps like gloves — a third ring must NEVER be refused. Swap replaces the WEAKER, ties replace slot 1, empty counts as weaker than Common. Then relog: the pair comes back in the same two squares. -> 

32v [X] - Auto-farm shows its target. While the autopilot runs, the target window shows the creature it is on, updates as it switches, clears when it has none. Pairs with 32k — you can't see it retaliate if you can't see what it chose. -> 

32x [X] - An improved buff is ONE square on the bar. Cast the cleric's Improved Speed: exactly one square (0.36.0 put up four). Press-and-hold removes the whole thing, and a potion square is unchanged — a Swift potion reads "Swift", not "Swift Potion (Greater)". -> 

32y [] - One item can be tuned on its own. As admin: `/droprate item Scroll of Resurrect 5` must accept the NAME and confirm; bare `/droprate` then lists it under "Per-item overrides". In a mob's drop tree the Always group still reads 100% but its members print their SHARES and the scroll's has grown at the others' expense — the group must NOT fire more often. `... 1` clears it. A wrong name suggests near matches. -> 

32z [] - Auto-farm skill chains. In the Auto Farm window: cyclic OFF reads 1-2-1-3-1-2, cyclic ON reads 1-2-3-1-2-3; a needed heal beats a buff beats an attack; heal threshold 50 heals only under 50%, 100 on a healer fires on cooldown and in a party lands on the most injured in range, heal row OFF never auto-casts; an auto-buff recasts under 60s remaining but not under a stronger same-family buff; a rank-1 debuff is replaced by the higher rank; assist-party-leader means you attack only what he attacks and stand still when he has none; all of it survives a relog. -> 

---

## OLDER OPEN ITEMS — from playtest-11 (2026-07-24). Some may already be fixed since; if so just write "gone".

17-1 [~] - `/jail test1` then `/tp test1` teleports to the DUNGEON, not the jail (both live in the negative quadrant — position clamping). -> jail have no border. Teleported admin still gets teleported to dungeon if he moves inside the jail

17-2 [X] - Mobs don't attack inside the dungeon when you are teleported in from the debug menu — no aggro, no retaliation. -> 

17-3 [X] - Mobs are clamped together in the crypt, bunched on one spot. -> 

17-4 [X] - The soft keyboard COVERS the command bar instead of lifting it. -> 

17-5 [X] - "Test1 entered the world" leaks to non-friends — shown while a request is only [pending]. Entry/exit notices must be mutual friends only. -> 

17-6 [X] - `[info]` shows only for monsters/bosses, never for a player target — and the old player-target button grid comes OUT (commands live in the Actions tab instead). -> 

17-7 [X] - Debug-menu chat spam: 10 potions print 10 lines. Drop the system messages for debug items/buffs/levels, keep the rare ones (tp coords, karma cleared, class change). -> 

17-8 [X] - `[lead]` doesn't update the party `*` flag or remove the [lead] button; and `*` should become a star or crown. -> 

17-9 [X] - Duplicate town-entry text — a blue line under the big banner; remove the old one. -> 

17-10 [X] - `isAdmin` is per-CHARACTER, not per-ACCOUNT — a non-admin character in an admin account can run admin commands. -> 

17-11 [X] - Skills window -> Learn does NOTHING. Action / To-bar / Use all work; Learn alone is dead. -> 

17-12 [X] - Stand-up timing: a delay after tapping to stand, but INSTANT if you have been sitting longer than 3s. -> 

17-13 [X] - Bag: Equip button first, and the equip column expands LEFT. -> 

17-14 [X] - Spell Rune buff reads `719h59` instead of `29d` — duration needs day rollover. -> 

17-15 [X] - Admins excluded from the ranking system (an admin at level 999 breaks every board). [likely done in 0.44.0 — confirm] -> 

17-16 [X] - Shop items need details + buy-time info — a war rune shows no "works ONLY on PHYSICAL" text anywhere. -> 

17-17 [X] - Shop prices far too cheap: equipment from 200g minimum, runes 150k/1h and 280k/2h. -> 

17-18 [X] - Show raw attack/cast speed numbers, not just the multiplier: `1234/1500 (x3)`, not a bare `x1.1`. -> 

17-19 [X] - No HoT floating text for potions. -> 

17-20 [X] - Target window numbers: mobs show current/max HP as digits, players the same plus an MP bar. -> 

17-21 [X] - Party window: buffs/debuffs as squares to the right of each member (no duration text, still flashing under 60s) to cut the height; loot proposal as a drop-down. -> 

17-22 [X] - World border: an orange dashed line like the jail's, as the fallback where there is no physical collision marker. -> 

17-23 [~] - Real impassable WALLS — client collision (you stop at the surface, no out-of-world coords sent, a tap outside your world is rejected before it becomes a move order). The server rubber-band stays as the anti-cheat backstop. -> well only server ruberbands me not the impassible wall

17-24 [!] - Target a party member with NO range restriction so move-to/assist/heal/buff resolve out of view, and kick / change-leader work from the action buttons. Minimal frame. ->  can't target out of range 

17-25 [X] - Buff tap behaviour: press-and-hold cancels, a single tap opens a details popup that closes on an outside tap; holding a DEBUFF shows details instead (debuffs can't be dismissed). -> 

## OLDER CLIENT ITEMS still unticked

16a [X] - Open a box from the inventory: a plain box grants its contents straight to the bag, a SELECTION box opens the choice popup and grants only the picked entry. -> 

16b [X] - Item details layout: the stat block is no longer crammed under the item name; long names and full stat sheets both lay out cleanly. -> 

13a [] - The ~3h "take a break" banner — needs 3 hours of continuous play. -> 

9a [!] - Chat peek/fade when the log is hidden: last 3-5 lines flash at the chat's spot for 3-5s then fade, filtered by the active tab, with a pin toggle. NOT BUILT — leave unless you want it moved up. -> 

25a [X] - Buffs survive a relog: note the timers, exit to character select, re-enter — they come back with LESS time, not full and not gone. Time keeps running while away, an expired buff must NOT reappear, runes appear exactly ONCE, and another character shows ITS buffs. -> 

25b [] - No combat-logging out of a DoT: while a bleed/poison/venom is on you, character select must REFUSE ("You can't leave while in combat") and you stay in the world. Same for `/exit`. Pulling the plug mid-DoT must not run the link-dead grace down. -> 

26 [X] - "You entered <field>" needs hit-test FALSE — it currently blocks tapping the ground beneath it. -> 

## NOT BUILT — nothing to test, listed so nothing is silently dropped

- Block system (`/block`, `/unblock`, `/blocklist`) -> block with action button works, need / chat commands, u cannot block someone that is an admin or moderator
- Charisma system (`/like`, karma cost, exp/sp bonus pool, moderation penalties) -> like action works, need /chat commands ,exp/karma etc didn't test
- Buy-back menu (last 10 sold/deleted items)
- Starter-gear redesign (newbie boxes become a level-10 quest; weakest gear at 1-10) -> this is true now after shield fix 
- Levelling curve decision (`ExpToNext = 25L²` vs `MobExpReward = 40 + 35·L`) — needs your call, then measure with Balance Matrix -> can't test. Need full working game to start playing and test only ti's 
- 3-tab auto-potions, per-skill auto-farm priority UI, clock, `/ptinv`-style target commands
