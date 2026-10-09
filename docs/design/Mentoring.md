# Mentoring (`BL-339`) — design, NOT BUILT

Status 2026-10-09: his spec below (verbatim), my read of it, and the questions that block a build.

## What already exists to build on
- **Friends list** (`/friend`) — the invite/accept/list shape he asked to copy.
- **Timed items** (`ExpiresAtUtc` on the instance) and **temporary selection boxes** (`Items.cs`: `Temporary Common
  Weapon/Armor Selection Box`, `Temporary {body} Set`) — the box-in-a-box shop flow is mostly there.
- **Grand Rune boxes** (`box_grand_rune_*`) — the rune shop items exist.
- **Buff families** — the mentee blessings must sit IN the originals' families (see option 1 below).

## What does NOT exist yet
- **A last-online time on a character** — needed by the list and the removal penalty. New column → `game.db` delete.
- **A per-day login history** (last 7 days) — needed by the mentor's activity figure. New column.
- **A bond record** (mentee → mentor, created, milestones paid) and **a penalty-until time** per character.
- **Two currencies** (Bond Certificate, Mentor Certificate) and the shop.

## My read

### The blessing — option 1 (separate self-only skills) is the right one
It is the only option that answers his own objection (mages getting Bloodlust, daggers getting Frenzy). It is
also smaller than it looks: the autopilot already auto-casts bar buffs, so the mentee places only the ones they
want. The skills are granted while the bond holds and cast-gated on "mentor online".
🔑 **Each must share its original's buff FAMILY.** Otherwise Mentor Precision stacks on a real Warchanter's
Precision. With the family shared, the existing rules handle it: a group evicts the single, a single never
overrides a group. New ids are justified (self-only, 1h); this is not the "copy only to rename" case faces replaced.

### The exploit is multiboxing, not alt accounts
"Not the same account" is right but weak. Five cheap low-level alts on other accounts idling in town = a permanent
L5 (+100% exp/SP) for the mentor. Rewards are self-limiting (raising a character to 76 is real work); the online
buff is not. **Proposal:** a mentee counts as online only if not on the mentor's IP/device AND not idle (combat or
exp gained in the last 10 min). The mentee's +50% has the mirror hole, but a mentor alt costs a 76 + 4th class
character, so it is fine as written.

### The T80 offer is poor value
100 mentor certificates = 10 mentees raised to 76, for 7 days of gear. Nobody picks it over potions.
Proposal: 30d, permanent, or far cheaper.

### Numbers that disagree
1. Mentor buff drop/gold: "5% per level" vs "L5 = 10%" — 2%/level (→10%) or 5%/level (→25%)?
2. 1d Grand Rune: priced at both 50 and 20 Bond Certificates.
3. 1d exp/SP rune: no price.
4. Activity "3 of 7 days ≈ 50%" is 43%. Proposal: show `3/7d`, not a %.

### Totals check (correct as written)
Mentor per mentee: 15+30+55 = 100 Bond, 1+3+6 = 10 Mentor. Mentee: 150+300+550 = 1000 Bond.
Gear tiers 100+250+500 = 850 ≤ 1000, and each payout covers its tier when it lands (150≥100, 300≥250, 550≥500).

## Open questions (his to answer)
1. **What does a penalty block?** Assumed: only inviting/accepting a new bond for that long; existing bonds untouched.
2. **Reward trigger:** reaching level 20/40/76, or completing the class change? The bond ends at 76, so the 76
   reward must land first.
3. **Minimum bond age for the mentor's milestone?** Else a 75 can join a mentor just to hand over 55 + 6.
   Proposal: the mentor is paid only if the mentee gained ≥5 levels while bonded.
4. **Can a mentee request?** "Pending" implies `/mentor invite <mentor>` from the mentee side too. Do open
   invites count toward the 10? Proposal: only accepted bonds count.
5. **Which rung of each `wc_*` buff?** Only the harmonies have rungs named. Top rung, or by mentee level?
6. **Certificates bound (untradeable)?** Proposal: yes.
7. **Mentor +100% vs runes:** additive or multiplicative?
8. **Mentor buff rungs, restated:** on any mentee login the level jumps to the online-mentee count (cap 5); on
   expiry it re-applies at the current count (0 = it ends); a logout never lowers it early. Right?

## His spec (2026-10-09, verbatim)

My idea is when a player reaches 76lvl+4th class he can become a mentor (automatically become one without mentees and benifits)
1. mentor:
   - to become a mentor u must use "/mentor invite <name>" -> sends invitation to a player that is below lvl 76 - same principal as firends list
   - a mentor can hold up to 10 mentees
   - a mentor gets access to commands
     - accept mentee - /mentor accept <name> -> if aplayer want you to be his mentor u can accept
     - remove mentee - /mentor remove <name> -> u can remove your mentor status from a mentee (not online for long time or dont lvl up etc)
       - if a mentor removes a mentee the mentor gets a penalty depending on mentees last login
         - Less than 24h (23:59:59h | list shows - 0m~23h) -> 24h penalty
         - Less than 3d (2 days + 23:59:59h| list shows - 1d, 2d) -> 12h penalty
         - Less than 5d (4 days + 23:59:59h| list shows - 3d, 4d) -> 6h penalty
         - Less than 7d (6 days + 23:59:59h| list shows - 5d, 6d) -> 3h penalty
         - 7d+ (7th day and 0 second | list shows - 7d,8d,..30d,..etc) -> no penalty
     - mentee list - /mentor list -> same principle as firend list just with lvl information and last online -> row: <Name> <Online|Offline|Invited|Pending> <level> <lastonline>
       - level -> just a number can be `Gena Online (25) 0m`;
       - lastOnline -> if status is Online the last online is 0m -> when offline each min gets the same logic as buffs duration -> 1m,5m,1h,5h,1d,30d etc ..no seconds needed, when over 1h no minutes need, when its ofer a day no need for hours and minutes `Gena Offline (25) 29d`
       - Invited -> (U gave invite and wait for his accept); Pending -> (he gave invite and w8 for you to accept)
         - any pending|invited is removed from list when the mentee gets a mentor(if it you he become your online|offline, if its other than you he is removed from the list, when he reaches 76 he is removed from the list)
         - `Gena Pending`, `Gena Invited` no last login time nor lvl
   - a mentor gets rewards  when a mentee reaches class changes -> 20,40,76 -> (15,30,55) 100 in total (a bond certificate) per mentee and 1,3,6 mentor certificates (10/mentee)
   - there should be a mentor/mentee shop that a mentor can buy stuff with (later discussion, now only stacking certificates) like some things from premium shop like grand runes or daily .. or other stuff
   - when atleast one mentee is online (char is in the game) the mentor gets buff levels 1~5 (depending on online mentees) each online mentee the mentor gets one lvl of that buff -> buff that increase 20% exp/sp gain and drop/gold by 5% (L5 == 100% increase in exp/sp and 10% in drop_chance and gold_amount) (not counted thoward the limit) - 10min each stage -> when mentee neters the mentor gets a buff L1, nex mentee enters -> L2, 1st mentee leaves -> the L2 effects continue to last untill its 10 mins , after the 10 mins he gets a L1 for 10 mins and when the second mentee leaves after the 10 mins his buff worns off and no new applied until any manteee neters (it goes a rung up on mentee enter, but the highest rung stays for its 10 min duration)
2. mentee:
   - a mentee can have only one mentor
   - a mentees menotr cannot be of the same acc
   - a mentomenteesr gets access to commands
     - accept mentors invitation - /mentor accept <name> -> if a mmentor want you to be his mentee u can accept
     - remove mentors - /mentor remove -> u cannot accept new mentor if u already have one -> removes the active mentor and gets u penalty depending on mentors online status
         - (same five-step table as the mentor's)
     - mentee list - /mentor list -> shows list with pending mentors or active and online status -> row if no active bond: <name> <Online|Offline> <activity_status> (name of mentor and last 7days % based online status -> active for 3 days out of 7 activity status ~50%) so you know which mentor is active more than the other and current satus; row if you have active mentor: <name> <online|offline> <lastlogin>
   - when a mentee gets a mentor he gets buff skills:
     - buffs are self only and equal to: madness, wc_feral_precision, wc_feral_bloodlust, wc_arcane_insight, wc_arcane_serenity, wc_body_reinforcement, wc_wind_grace, npc_harmony_protection L4, harmony_of_speed L1, npc_harmony_warrior L5, npc_harmony_wizard L5 -> duration can be 1h
     - with face `Mentor Blessing: Name` and descritpion "Mentor is watching over you: Increases ...." similar to the human buffers faces
     - 3 options
       1. he gets each and every if those buffs as self only and inside his skill list so to put them on auto-use on skillbar and choses what to use -> each skill is duration 1h and to recast need an online mentor
       2. he gets a single `Mentor Blessing` skill in the skill list and using it gives the buffs above .. -> can be auto-used -> buffs stay 1h and only this skill is gated and works only when mentor is online
       3. mentee gets all buffs above for 1h automatically and when one buff worns off it checks the mentor status -> if mentor is online it gets it -> but then mage will get feral bloodlust and precition and fightwers will get arcane isight and daggers will get frenzy that decreases evasion and not all daggers would like that
      - 2 and 3 have the same problem with the unwanted buffs -> but 1 seems the hardest to execute
   - a mentee gets rewards  when reaches class change -> 20,40,76 -> (150,300,550) 1000 in total (a bond certificate)
   - an onlime mentor gets the mentee an automatic buff for 10min that increases exp/sp +50% - when mentor is offline the buff worns off
3. Shop:
   - mentee can buy runes (ex/sp or grand), temporary equipment (armors with working set bonus and weapons already attributed to max), blessing boxes etc
   - the prices should decide
   - for example a 100 bond certificates should be enough for T20 temp30d set + T20 temp30d weapon, 250 for T40 temp30d set + T40 attributed temp30d weapon, and 500 for T76 temp7d set + T76 attributed temp7d weapon
   - for example 1d grand rune can cost 50 bond certs -> so one mentee can decide to skip equipment and have 20d worth of grand runes, or for mentor one mentee const 2d worth of rune
   - 1d grand rune can cost 20 bond cers -> each mentee can give you 5d worth of runes
   - bless box can cost ~10 bond
   - 1d 20% exp/sp rune costs
   - a mentor can give 100 mentor certs to buy 7day temporary T80 set + T80 weapon
   - a mentor can exchange 1 mentor cert for:
     -  500 uncommon Hp pots
     -  250 uncommon mp pots
     -  100 rare hp pots
     -  50 rare mp pots
     -  10 instant hp pots
     -  5 mythic dash pots
     -  2 ultimate retrun scrolls
     -  1 ultimate resurect scroll
   -  the temporary boxes are -> u buy u get a box 7d -> opening the box gets u the equipment inside (own time duration)
      - `T20 armor box 30d` duraton of the actual box 7d, open to select one T20 set box (heavy|lishgt|robe) -> `T20 Light Set box 30d` (7d duration of the actual box) -> gets Helmet,Boots,Gloves,Body with set bonus T20 for 30days (shield included in any set)
      - `T40 weapon box 30d` duration of the actual box 7d, open to select one T40 weapon box (maul|mace|bow..etc) -> `T40 Wand box 30d` (7d duration of actual box) -> open to select one *Darksteel Wand - Alacrity*, *Darksteel Wand - Soul*, *Darksteel Wand - Force* T40 for 30 days -> pre attributed to the max value -> in bag *Darksteel Wand - Force (B/T)*
