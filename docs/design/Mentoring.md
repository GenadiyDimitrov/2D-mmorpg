# Mentoring (`BL-339`) — design, NOT BUILT

Status 2026-10-09: his spec (verbatim, bottom), then three passes of his answers (newest at the top wins). BUILDABLE; only the shop prices are open (exp rune = `rune_expsp` L6, 50%).

## His answers, THIRD pass (2026-10-09) — the four left open, now closed. WINS over everything below.

### 1. Mentor pay: Bond Certificates at each level, Graduation Certificates once at 76
- **Bond Certificates (15/30/55) are paid to the mentor DIRECTLY at level 20/40/76**, as before, to whoever holds the
  bond at that moment.
- **The mentor's own currency is now the Graduation Certificate, paid ONCE, at 76: 10 minus every milestone this
  mentor missed.** Bonded before 20 → **10**. Bonded at 20-39 → **9** (missed the 1). Bonded at 40-75 → **6** (missed
  1 + 3). So a mentor who picks up a 75 still gets 6, as he wanted, and raising from 1 is worth more.
- Build note: the bond record keeps "was bonded to THIS mentor on reaching 20 / 40"; graduation reads those two flags.
- "Mentor Certificate" as a name is RETIRED. The two currencies are **Bond Certificate** and **Graduation Certificate**,
  both bound, ×9999.

### 2. The 1d exp/SP rune = 50 Bond Certificates — kept
A **gold vs exp decision**: the mentee can take the T20 set and one 1d rune, and the exp/SP rune is the one rune that
**cannot be bought anywhere else**, where a 1d War/Spell Rune costs gold (his figure: 280k ×12). It is a `rune_exp`
rung, so it evicts or loses to the other exp runes by rank, the same way the ladder already works.
🔑 **RUNG SETTLED: +50% exp AND SP = `rune_expsp` level 6** (his: 20% is too low and 100% too high against
~3.4M gold for 12× 2h War/Spell Runes). The rung already exists; no new item is needed.

### 3. Anti-abuse: a weighted, 10-rung buff, ONE mentee per account, AFK does not count. NO IP/device rule.
He declined the IP/device rule (*"some1 with IT knowledge can easily workaround that ... I need to test and use this to
lvl up/buff"*). Instead:
- **One mentee per account per mentor.** A mentor cannot bond two characters of the same account, so ten alts on one
  alt account are worth one mentee.
- **AFK does not count.** An online mentee adds to the buff only if actively playing (combat or exp gained in the last
  10 min, my proposal; he: *"an online player that is afk refuses you lvls"*).
- **The buff gets 10 rungs, and a mentee's weight grows with their level.** His targets: 10 mentees at lvl 20 → L2,
  10 at lvl 40 → L8, 5 at lvl 60 → L10 (= +100%). The curve that hits all three exactly (mine):

  **weight = mentee level² / 1800; buff rung = floor(sum of online, active mentees' weights), cap 10.**

  | mentee level | weight | to reach L10 alone |
  |---|---|---|
  | 10 | 0.06 | — (10 of them = L0) |
  | 20 | 0.22 | — (10 = **L2**) |
  | 40 | 0.89 | — (10 = **L8**) |
  | 60 | 2.00 | **5** |
  | 75 | 3.13 | 4 (3 = L9) |

  So a few high-level mentees max it for a short time (they graduate soon), or many active low-level ones build it up
  slowly. Both are what he described.
- **Per rung: +10% exp/SP, +1% drop/gold** → L10 = +100% exp/SP, +10% drop/gold, which keeps his earlier top values.
  (The per-rung split is mine; he set the totals.)
- Still the online aura from pass 2: no timer shown, **2-min re-check, 3-min grace** (his "ok" to my pick).

### 4. Graduation Certificate shop: REAL items, not only timed ones
Graduation Certificates buy **permanent** things at high cost (platinum or other appealing items, real T76/T80 gear;
his scale: *"100 mentees = helmet"*, i.e. ~1000 certs for one T76/T80 piece). So an active mentor grows stronger
through items as well as the buff. Passive mentee-farming is possible but slower than boss farming in a party.
**Prices are the later shop discussion**, not set here. The 1-cert consumable list from the spec stays.

### Still open
- The shop price list, which needs its own pass (he called it "later discussion").
- Anything else is BUILDABLE.

## His answers (2026-10-09, second pass) — the third pass above wins where they differ

### Numbers, settled
1. **Mentor buff: +20% exp/SP and +2% drop/gold per level** → L5 = +100% exp/SP, +10% drop/gold. ("5%" was a slip.)
2. **Two currencies, renamed:** **Bond Certificate** = the level-up payout (mentee 150/300/550, mentor 15/30/55);
   **Mentor Certificate** = the mentor's own (1/3/6). He floated paying the mentor **10 "Graduation Certificates" once
   at graduation** instead of 1/3/6 (still open, see below).
3. **1d Grand Rune = 50 Bond Certificates.** The 1d exp/SP rune: price it ~50 too so it is a real choice (faster vs
   cheaper levelling), **or drop it**, since the mentee already has the +50% while the mentor is online. (Still open.)
4. **Mentor activity is shown as a %**, e.g. `3/7d (43%)`.

### Questions, answered
1. **The penalty blocks NEW bonds both ways**: no new mentor and no new mentee while it runs; existing bonds stay.
   It is checked at **accept** time, so an invite sent before the penalty fails on accept:
   `Mentor is under a bond penalty for Xh. Bond cannot be made.`
2. **Rewards fire on reaching LEVEL 20 / 40 / 76**, not on the class change (a class-quest gate could be farmed for
   the bonus). The mentee does the class quest when they choose.
3. **No minimum bond age.** His reasoning: a mentor with time to raise characters to 76 bonds them at level 1 anyway
   for the full 100/10, not 55/6. A 75 at 90% who never found a mentor can ask in world chat, and any mentor with a free
   slot gets 55/6 for free. Both sides win.
4. **Either side invites; the other answers.** `/mentor invite <name>` → `/mentor accept|decline <name>`. Accept or
   decline clears the pending/invited row; accept turns it into a bond row. Messages (his wording; he wrote
   "apprentices/students/mentees" as name candidates, one word to pick at build):
   - mentee invites a full mentor: `Mentor <name> cannot accept any more mentees.`
   - mentor invites someone already bonded: `Mentee <name> already has a mentor.`
   - full mentor invites: `You cannot accept any more mentees. Use '/mentor remove <name>' or wait for graduation.`
5. **Group buffs given without a rung = the rung learned at level ≤ 74** (no 4th-class rungs; some ladders go on past
   76). The harmonies stay at the rungs he named (Protection L4, Speed L1, Warrior L5, Wizard L5). They are capped on
   purpose so a real buffer is still worth having.
6. **Both certificates are BOUND**: no trade, no account warehouse, no sell, no break. Use in the shop or keep in the
   private warehouse. Stack ×9999.
7. **Exp/SP bonuses ADD**, the same as runes: rune +100% + mentor buff +100% = +200% = ×3.
8. **The mentor buff becomes an ONLINE AURA, not a 10-min timer.** No duration is shown (so players read it as
   online-linked). It re-checks every **1-3 min** and sets its level to the current online-mentee count (cap 5).
   When a mentee logs out there is a **1-3 min grace period** so a crash costs nothing. This replaces the "highest rung
   holds 10 min" rule, which the enter/leave pump exploited (ten alts relogging = L5 for 10 min). Engine need: a
   buff with no timer that the server renews, drawn with no countdown and no expiry blink.

### Still open after this pass — CLOSED by the third pass above
- **Graduation Certificates (10 at 76) or 1/3/6 per milestone?** My pick: **keep 1/3/6.** With a lump sum at 76,
  the late joiner his Q3 welcomes gets all 10 certs, the same as a mentor who raised the character from level 1. That
  removes the reason to bond early. Under 1/3/6 the late joiner gets 6, which is still "free" and still a win-win.
- **The 1d exp/SP rune: 50 Bond, or cut?** My pick: **cut it for now.** The mentee already gets +50% whenever the mentor
  is online. A rune that only matters while the mentor is away is a weak buy next to a Grand Rune at the same price.
  Add it later if the shop feels thin.
- **The multibox hole is not closed by the aura.** The aura stops the relog pump but not five alt accounts idling in
  town: that is a permanent L5 (+100%). My proposal stands: a mentee counts as online only if they are **not on the
  mentor's IP/device** and **not idle** (combat or exp gained in the last 10 min). Needs his yes or no.
- **Recheck interval:** "1-3 min". My pick: **2 min re-check, 3 min grace.**

### T80 offer, as he asked ("elaborate?")
100 Mentor Certificates = **ten mentees raised to 76**, which is months of a mentor's time. The same 100 certs on the
1-cert list buy 100 Ultimate Resurrect Scrolls, or 50,000 uncommon HP potions, or 500 mythic dash potions. Against
that, **7 days** of T80 gear is a bad trade: a mentor at 76+ with a 4th class already wears end-game gear of their own,
and the set is gone in a week. Nobody sensible picks it, so the line does nothing.
Options, my pick first:
- **30-day T80 set + weapon for ~30 Mentor Certs** (three graduations). Still a real goal, and it lasts long enough
  to matter. ← my pick
- 7 days for ~10 certs (one graduation): a cheap "try the next tier" item.
- Permanent at 100: a real prestige goal, but it competes with crafting/drops, which `BL-282` priced carefully.

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


## Open questions — ANSWERED 2026-10-09, see "His answers" above
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
