# OPEN CHECKLIST — the 0.81.1 pass

> **Rolling and unversioned.** §90 is everything built between 0.71.0 and 0.77.0 that you have not
> played yet. §91 is your six free-form finds from 2026-08-22.
>
> 🔑 **PLAYTEST 28 (2026-08-23, your twelve new My Finds) IS ANSWERED IN FULL — eleven built, one
> answered.** Playtest 27 before it is answered in full too (all five finds and every `->` comment in
> §90 and §91). What is answered rather than built is marked 🟠 and says why.
>
> **Built for playtest 28:** the mana-restore exploit now reads the **KIT, not the skill book** ·
> **chat is filed per character** and survives a relog and an app kill · a **chat log table** for
> moderation · **runes off the buff cap** (the exemption was written down and never reached one) ·
> the buff details say **which potion** they came from · the NPC buffer cut **19 → your 11** and its
> window moved to **6-90, free to 75** · buff-potion drops down to **Swift / Alacrity / Fury / Dash**,
> the other six off the loot tables — shop keeps **Common only**, the **CRAFTER** supplies the Uncommon
> rung (your correction; and yes, potions are tradable, always were) · tapping your own panel **targets
> you**, [Char] moved into the bag · a **blunt skill accepts a maul** — and every other weapon gate with
> it · flat buffs apply **after** percentages · the sound skills **retire Holy Bolt** · an **aqua ring**
> on a live toggle.
>
> **Built 2026-08-23 (playtest 27):** city regen ×5→×2 and city-only · stat swaps off the Learn tab ·
> the **max buff cap at 20** with a per-buff flag (`BL-87`) · mana vamp 3/7/10% → **1/1.5/2%** ·
> **cast chaining** · `_ . -` legal in names · **`/buff` takes a target**, `@s`/`@self`, `~` relative
> `/tp`, `/where` for everyone · the buff **level in the effects popup** · the god badge and stealth
> opacity (0.80.0).
>
> 🔴 **THE APK IS STILL NOT BUILT.** `ProtocolVersion` is **25** and a big share of the above is
> client-side — the chat filing, the self-target, the [Char] button, the toggle ring, and the Learn tab
> losing Holy Bolt. **Say the word and I will build both halves.**
>
> 🔴 **STILL NOT BUILT, ON PURPOSE:** `BL-85` (a harmony's rungs share one rank) — `91g`'s first half.
> And §89's three UI changes (`BL-88`), which ride the next client batch. The chat log has **no reader
> command** yet → **`BL-89`**, which also carries **two questions only you can answer**: how long to keep
> the log, and whether staff below admin may read whispers.
>
> 🔴 **BEFORE YOU PLAY: delete `Game.Server/game.db`** (and `-shm`/`-wal`) — owed since the 0.71.0
> schema change, again since `AccountRole` renumbered (§91d), and again now for the **chat log table**.
> ⚠ It is in `Game.Server/`, not `bin/Debug/`. The stale-schema check should recreate it for you on
> first boot; deleting it by hand is the certain version. **Install BOTH halves.**
>
> ✅ **Your marks go in the repo, not an upload** — that has worked six passes running.

Rows are the format you picked (option 2): write your comment after the `->`. Put `x` in the `[]` if
it passed with nothing to say, `~` if it works but wants a change, `!` if it is a bug or priority,
`?` for a question. A `-` row with no id is a free line for that section — add as many as you like.
**Your own "My Finds" section is at the top** — keep using it, it is where most of the real content
arrives, and playtest 27's five finds all came in that way.

🔑 **This file is for TESTING. What is still owed to be BUILT lives in
[docs/Backlog.md](../Backlog.md)** as permanent `BL-nn` ids.

---

## My Finds — playtest 27 + 28

*(Reset 2026-08-22. Your previous six went to §91 — five built, one routed to `BL-86`.)*

**Playtest 27 — all five BUILT (2026-08-23).** The regen and buff-cap changes are server-side; the rest
need the new APK. **Playtest 28 — the twelve below them, eleven built and one answered.**

- [!] 🟢 **BUILT.** Hp/mp regen in cities should be decreased to x2 and only in the big cities ..not in a starting point of elit dungeon ...I can sit with the healer with 220mp/s regen and heal like crazy.
  -> Multiplier **5 → 2**, and it is a **CITY** bonus now, not a safe-zone one: new `SafeZone.RegenBoost`,
  **false on the Training Outpost and all three dungeon entrances** (Hollow Crypt, Sunless Warrens, Ashen
  Sepulchre). They keep everything else a safe zone does — no mobs, no aggro — they just are not rest
  stops. 🔑 **The stack was the real number**: town ×5 × sitting ×1.8 = **×9**, and Meditation's flat
  +MP/s sits INSIDE that multiplier on purpose, so it was being paid ×9 too — that alone was most of your
  220. It is **×3.6** now. ⚠ **The Training Outpost is my call, not your words** — you said "big cities",
  and it is a 400-radius hut beside the dummies. Say if you want it back. ->

- [~] 🟢 **BUILT (CLIENT).** The stat swap,passive should not be in the "to learn" tab. They have their own. Only show in the passives already learned.
  -> Filtered out of the Learn tab entirely. They are bought on the **Stats** tab — a basket you stage
  for free, a running "Added:" line, one total — and read back on **Known**, greyed, once owned. The
  per-rung gold pricing that had to be duplicated in the Learn tab went with the rows, so exactly ONE
  place in the client prices a swap now. ->

- [~] 🟢 **BUILT — `BL-87`.** we need make max buffs limit. Now I have 24 buffs as healer ... So if we make it 20 then the buffer becomes a must. Tell me how much buffs we have how many harmonies and we should make the buffs to have a flag - do they count in the limit or no. Some buffs like rogues dash,dash pots ..some other limited time buffs won't be caunt to the limit ..toggles also are never vaunted.  For now the only buffs that are with a caunt are group/harmones/singles every else self/temporary/ultimate is not to the limit
  -> 🔑 **HALF OF THIS ALREADY EXISTED AND YOU WERE SITTING IN IT.** A cap with FIFO eviction has been in
  the engine since the buff-ladder work — **at 24**. That is why you counted exactly 24: you were **at
  the cap**, and buffs had been quietly falling off the back of your bar. It is **20** now.
  - **The flag is per buff, default TRUE** (`SkillDef.CountsTowardBuffLimit`) — not derived from self,
    not derived from duration. **Bow Expertise counts**, as you said. Authored `false` on: the six Combo
    Rush rungs, War Cry / Greater War Cry, Battle Fury, Fortify, Shrouding Hymn, the three racial Renew
    verses, **Harmony of Restoration** (the party HoT), Aegis, Battle Presence / Battle Defence, Conceal,
    Defensive Wall, Evasion Boost, Indomitable, Last Stand, Mana Barrier, Meditation, the eight
    Dash/Sprint rungs and the three healing potions. Every one is ≤90s — that fell out, it is not the
    rule. **Toggles, debuffs and the gear/rune row** were already free and stay free.
  - **FIFO exactly as you ruled** — oldest applied goes first, 2h left or not, and the new buff always
    lands. Never a refusal. **The cap counts only counted buffs**, so your `20 + 14` is right.
  - 🔑 **Measured: a fully-buffed character sits at 16 / 20 — four free.** Buffing yourself off the NPC
    buffer instead costs **19 of 20** for a strictly weaker set. The cap squeezes the ALTERNATIVE to the
    buffer, not the buffer. **`dotnet run --project tools/BalanceMatrix -- --buffs`** prints it. ->

- [~] 🟢 **BUILT.** Should lower the buffers mana vamp - to op - same levels just 1,1.5,2% or 10% on 10/15/20% chance
  -> **1% / 1.5% / 2%**, code and `buffer 3rd.csv` both. 🔑 **Your two options are the same expected
  value** — 10% on a 10/15/20% chance averages 1/1.5/2% — so it was a FEEL question, not a numbers one,
  and the flat one won: a sustain line is the wrong place for variance. You want to know whether you can
  keep buffing, not roll for it. The proc version is one `ProcChance` field away if you want the spike. ->

- [!] 🟢 **BUILT.** Cancel casting should be done only from clicking the same skill on the bar (it's X) or the cast bar .. Now I click one skill and clicking the second cancels the first and start the seconds cast ...I have no way of chaining skills -> I want to be able to click one skill clock second and when 1st is done the second to begin (only 2) while 1st is cast ..clicking on any other skill chains the last clicked ... Now if I do it fast I can skip buffs ...
  -> **Exactly your rule.** While a cast is in flight (or a queued skill is walking into range):
  **the same skill cancels it** (and pays the reuse, same as the cast bar's X); **any other skill becomes
  the chained one**, replacing whatever was chained before. **ONE chain slot**, per your "(only 2)".
  - 🔑 **The chained cast is re-gated when it FIRES, not when you click it.** MP, cooldown, range and
    target are all re-checked at that moment, so a chain that has become impossible just fails the way it
    would have if you had pressed it yourself — nothing is reserved and nothing is pre-paid.
  - **A cancel ends the whole plan**, chain included — including an enemy interrupt. Leaving it armed
    would hand you a surprise cast minutes later, which is the thing this was built to remove.
  - **Toggles are never chained** — instant, no cast time, so they just fire.
  - ⚠ The cast bar's X now also drops a QUEUED skill that has not started casting yet. There was no way
    to call one of those off before. ->

- [!] 🟢 **BUILT.** A healer/buffer that haven't learned mana restore can be restored - it's a exploit .. Not to check only current learned ..should check the actual kit (future/oresent/etc) 20lvl cleric should not be able to be restored even when he should learn it at lvl 30
  -> **Exactly your rule: the KIT answers now, not the skill book** (`IsManaRestorer`, one helper, both
  call sites — the manual cast and the autopilot's target pick). 🔑 **It was worse than you saw.** The old
  test was `HasSkill`, which does not make a level-20 cleric an exception — it makes the rule a **level
  window**: a cleric was a legal restore target from **1 to 29** and stopped being one at 30. So two
  clerics could print mana off each other for thirty levels and then have the door shut in their faces.
  The loop this exists to stop is a property of the CLASS — two characters who can each turn HP into MP
  — so the class table is what has to answer it, at every level. ->

- [!] 🟢 **BUILT (CLIENT).** chat again is saved between logins. Don't reset
  -> 🔑 **This and playtest-17's `C1` are the SAME rule, not opposite ones**, which is why it is worth
  saying how it landed. `C1` was *"chat must reset on exit"* — because a freshly created character opened
  onto a **deleted** character's conversation. You want it kept between logins. Both readings say the
  chat belongs to the **CHARACTER**: the first complaint was it leaking ACROSS characters, this one is it
  being thrown away WITHIN one. So it is **filed, not wiped** — leaving the world stores the chat under
  whoever was talking, entering it restores that character's own and nobody else's.
  - **It goes to disk**, not just memory, and it also flushes when Android backgrounds the app. "Between
    logins" on a phone mostly means *the OS killed us*, and an in-memory stash would have quietly failed
    the one case you are most likely to hit. Last **300** chat lines per character.
  - The **System tab is still never saved** — it is the crash trail, it is not per-character, and it is
    the one thing you want fresh for the relog you are doing right now. ->

- [?] 🟢 **BUILT (the log half).** don't we need a chat log -I Mean in db as who said what and when abs to who - columns: time/sender/receiver(charid or world or normal or guild etc)/message - because now an admin/mod should ban based on som1 is trying to sell u for $ on private chat - how the bug games work it out ?with tickets with a screenshot or they have their chat log?
  -> **They have the log.** A screenshot is evidence the *reporter* supplies and the accused can dispute;
  a server-side log is what the moderator actually reads, and it is the only thing that answers "what
  else has this account been saying" instead of judging one cropped image. **Tickets are how a case
  opens — the log is how it is decided.** So: `ChatLogRecord`, your four columns plus the channel —
  `AtUtc` / `SenderCharacterId` + `SenderName` / `Channel` + `ReceiverName` / `Text`.
  - **The id AS WELL AS the name**, because a name can be freed by a delete and re-taken by someone else,
    and a six-month-old log row that only says "Aldric" is then evidence against the wrong person.
  - **Channel and receiver are two columns, not one overloaded field** — that is what makes *"every
    whisper this account sent"* a query rather than a string search.
  - **It logs what was DELIVERED.** A line refused for a chat ban, a jail, the world-chat level floor or
    an empty body never reaches the table — it was not said to anyone. A whisper to someone who blocked
    you is refused too. A `/block` on Local/World only filters who *hears* it, so that IS logged.
  - **Written off the tick**: the loop buffers a minute of chat and the autosave flushes it in one batch.
  - 🔴 **What is NOT built is the READER.** There is no `/chatlog` command yet — today it is a table you
    open with a SQLite browser. Say the word and it becomes an admin command (by name, by time window, by
    channel). It is also a **schema change**, which the stale-schema check will handle by recreating
    `game.db` on first boot. ->

- [!] 🟢 **BUILT.** Runes are caunted towards the buff limit ... If I have auto buff pots on -> I buff some buffs fade for over buffing then pots start to buff me and in the end I have semi buff form npc buffer and semi potions
	- I want buff potions in their details to say Potion of "name" I'm getting buffed "Mig" and don't know if it's my or the potions.
  -> **Both halves, and the first one was a real bug with an embarrassing cause.** The cap's own comment
  claimed runes were exempt — and it exempted `BuffRow.Item`, while **every rune in the game is authored
  `Consumable`**. So the exemption was written down and never reached a single rune. War Rune, Spell Rune
  and the whole reward-rune ladder now carry `CountsTowardBuffLimit: false`. Evicting one was pointless
  as well as unfair: the rune reconciliation puts it straight back on the next tick, so the cap spent a
  slot, dropped a real blessing to free it, and ended the second with the rune still on your bar.
  - **The details now say where the buff came from** — a `From: Might Potion (Lesser).` line at the top
    of the popup. It is in the DESCRIPTION rather than the name on purpose: the square's abbreviation is
    built from the name, and "Might Potion (Lesser)" does not abbreviate to `Mig`. You asked for it in
    the details, and the bar keeps reading as the buff it actually is.
  - 🔑 **Why the label could not already tell you:** a potion's wrapper owns the duration and the bar
    row, but the buff that LANDS is the family rung — literally the same buff a buffer casts, same key,
    same rank. That identity is the whole design (it is what makes the two compete instead of stack), so
    the only place the difference can live is the text. ->

- [~] 🟢 **BUILT.** to much buffs from npc and potions (only buff potions - not healing nor scrolls) 
	- npc buffer should have only: Body,vigor,resolve,alacrity,might,bulwarc,vamp,ward,force,fury,frenzy -> (p/m.Def,p/m.atk,p/m.speed,hp max/regen, cast interrupt/vamp,frenzy)
	- potions drop are limited to alacrity/fury/swift + dash-ocassionally -> the other buff potions are only from the apothecary masters
	- those 11 buffs are enough for the start of the game from the npc buffer
  -> **Nineteen → your eleven, and the drop faucet down to three families + Dash.**
  - **The buffer keeps:** Might, Bulwark, Force, Ward, Vampirism, Resolve, Body, Vigor, Alacrity, Fury,
    Frenzy. **Gone:** Aim, Focus, Ferocity, Insight, Soul, Serenity, Swift, Agility. 🔑 Your parenthesis
    is the shape and the eight that left are all one thing — **the optimiser's row**: the whole
    accuracy/crit block, the MP pair (the HP pair stayed, because dying is what a new character does),
    move speed (Dash covers it) and evasion.
  - ⚠ **"Fury" is the attack-speed family and "Alacrity" is cast speed** — its NPC single is still filed
    under the old id `npc_haste`, so if you ever grep for it, that is why. Not a typo either way.
  - 🔑 **This is the other half of `BL-87`.** Nineteen NPC singles against a cap of twenty left you ONE
    free slot, so taking the full NPC set and grouping with a real buffer were mutually exclusive.
    Eleven leaves **nine free**, which is what makes a buffer worth having instead of a cheaper
    substitute for one. It also kills the churn you described: 11 NPC + the 3 potion families that are
    left = 14, comfortably under 20, so nothing gets evicted and no potion tops up a hole.
  - **Drops:** rung 1 and rung 2 now carry **Swift / Alacrity / Fury / Dash** and nothing else. The rung
    WEIGHTS are untouched, exactly as when the scrolls came out — this does not narrow the faucet, it
    concentrates it: ten ids became four, so a buff potion drops just as often and is 2.5× more likely to
    be one of the three you can only get that way.
  - 🔑 **CORRECTED, same day, on your reply.** *"By apothecary master I meant the CRAFTER not the shop …
    the shop can supply common only and the crafter can supply the rest."* I had read "apothecary
    masters" as the shop NPC and stocked the Uncommon rungs on her shelf; that is reverted. **The shop
    sells the Common rung of all nine families and nothing above it.**
  - **Nothing had to be built for the crafter — he already makes all of it.** The Potion Master's ladder
    (your own: *"l2 - common buff pots … l4 - uncommon buff pots"*) has carried all nine Common potions
    at craft **L2** and all nine Uncommon at craft **L4** since the crafting build. So the six that left
    the loot tables land exactly where you wanted them: **a player Potion Master at L4 is now the ONLY
    source of an Uncommon Agility / Might / Bulwark / Force / Ward / Aim potion in the game.** That is
    the real content of this change — it hands a whole rung of a consumable to the player economy
    instead of to a vendor.
  - **And yes, potions are tradable — all of them, and always have been.** It is your own ruling from
    playtest-18 `V2b`: *"buff pots are 0 sell (ppl still can sell them to others if they want)"* —
    `SellPriceOverride: 0` so a **vendor** will not buy one back, `Tradable` left at its default so a
    **player** can. Nothing needed changing. ⚠ The one consumable that is NOT tradable is the buff
    **SCROLL** (`Tradable: false`, playtest-17 `E3`) — box-only and bound, because the Blessing Box was
    the tradable thing. Say if you want that reconsidered now that the crafter has a real shelf. ->

- [~] 🟢 **BUILT.** make npc buffer free until 75
	-  not limited 75 as max, but to the max of the game(currently 90) 
		- available (6~75 -> 6~90)
		- free 6~39 -> 6~75; paid 40~75 -> 75~90
	-  after 75 we make the payment required.
  -> **6-90 available, free to 75, paid 76-90.** The max reads `GameConstants.MaxPlayerLevel`, so it
  follows the cap if the game ever grows past 90 rather than needing a second edit.
  - 🔑 **This reverses what the price was FOR, and I think you are right to.** The old shape charged from
    40 — exactly when a real buffer class becomes available — so the NPC competed with a *player* on
    price. The new one makes the NPC the free floor for the whole levelling game and only charges in the
    endgame band, where gold is plentiful and the buffer you actually want is a person. What squeezes the
    NPC below 75 now is the **buff cap and the set's ceiling** (basic rungs, no groups, no harmonies) —
    not a bill.
  - The dialog line reads the constant now instead of saying "level 40" in hard-coded text. ->

- [~] 🟢 **BUILT (CLIENT).** clicking on myself (name upper left) should targets me - the details/char info button should be in the inventory next to the [equip] button, as a healer in a party it's hard to target urself fast from the window,and now outside party u cannot target ursel at all
  -> **Both, and you were literally right that it was impossible.** A world tap on your own body is
  refused on purpose (`!view.IsSelf`, so your own collider can never steal a tap meant for the ground
  under your feet) and the party window only exists when you are in a party — so a **solo healer had no
  way at all to select himself**, which on a bar full of ally-targeted skills is the difference between a
  heal landing and a cast being thrown away.
  - Tapping your vitals panel now **targets you**. The character sheet moved to a **[Char] button in the
    bag, between [Equip] and [Del]** — your call, and the better home anyway: the sheet and the
    paper-doll are the same question asked twice.
  - The target frame already handled "the target is me" (it hides the Attack/Invite row), so nothing else
    needed teaching. ->

- [!] 🟢 **BUILT.** cannot use acoustic shock and sound smash with maul (2h blunt) only work with 1h .. Should work with the 4 weapons (maul,mace,wand,staff - all blunts) same goes for all other.
  -> 🔑 **`Blunt` and `TwoHandedBlunt` are two different BITS**, and the gate was a raw mask test — so a
  skill authored "blunt" silently meant "one-handed blunt", and your own maul locked you out of your own
  damage skills. One rule now (`WeaponTypes.Satisfies`) at **all four** places that asked the question:
  the cast gate, the auto-farm's skill pick, the on-hit proc check and the weapon-mastery passive.
  - ⚠ **The fold is conditional, and that is the whole subtlety.** Folding a two-handed weapon down to
    its base type unconditionally would also let a maul pass a genuinely **two-hands-only** requirement —
    Whirlwind, Crushing Blow, the 2H mastery — because `TwoHandedBlunt.Base()` is `Blunt`. So: if a
    requirement NAMES a two-handed bit it is asking about hands and is matched exactly; if it names only
    base types, hands are not its business and the weapon is folded. Both authored shapes keep working
    with no skill row edited.
  - The proc check was already folding — **unconditionally**, which is the bug in the other direction. It
    is on the same helper now. ->

- [!] 🟢 **BUILT.** sharpening and reinforcement toggles should apply after everitying as a flat bonus not before buffs. Armor x buffs + reinforcement 
  -> **`base × (1 + Σ%) + Σflat`.** It was `(base + Σflat) × (1 + Σ%)`, which put every flat bonus INSIDE
  the percentage stack — so Reinforcement's +600 P.Def was worth 600 to an unbuffed character and ~900 to
  a fully-buffed one. **The toggle you flip to survive a bad pull was worth least exactly when you were
  unbuffed and needed it most.** Now a flat bonus means the number it says, always.
  - ⚠ **It applies to EVERY flat magnitude, not just the two stances** — Resolve's flat interrupt
    resistance, Aim's flat accuracy, the Spell Rune's flat +40 cast, a flat debuff. Deliberate: two
    orders of composition living side by side is how a formula stops being predictable, and your rule
    reads as a rule about *flats*, not about two skill ids. Say if you meant it narrower.
  - **Measured: `BalanceMatrix` is byte-for-byte identical before and after** — every buff set it models
    is pure percentage, so nothing it reports moved at all. The real change is arithmetic and small: at a
    ~30% P.Def buff stack, Reinforcement L13 goes from +780 effective to +600, i.e. about **−6% total
    P.Def while the stance is up**. Sharpening L13 loses ~120 of ~1500 P.Atk on a ~40% stack. ->

- 🟢 **BUILT.** I think holy bolt should be replaced from sound smash/burst - are the attack skills of buffers - healers replace wit with stronger one, same should be valit for the buffers.
  -> **You were describing something the healer already does and the buffer never did.** Holy Ray carries
  `Replaces: [holy_strike]`, so a Lightbringer's Learn tab and bar drop the obsolete bolt the moment the
  real spell arrives. The Warchanter inherited Holy Bolt from the cleric tier and kept it forever, beside
  a kit that was supposed to have superseded it. **All three sound skills carry the clause now** — Sound
  Smash, Sound Burst and Acoustic Shock — not just the first one a race learns, because an ork can buy
  Smash and Shock at 40 in either order and the retirement must not depend on the shopping order.
  - ⚠ **The trade is real and it is yours to accept.** Holy Bolt is a SPELL with **no weapon
    requirement**; all three of these are weapon-gated (blunt / bow / blunt). A Warchanter caught with the
    wrong weapon in his hands now has **no attack skill at all** rather than a weak one. That fits the
    rest of your 3rd-class design — each race's Warchanter is built around one weapon — but it is a door
    closing as well as opening. Say if you want Holy Bolt left as the bare-handed fallback.
  - 🔴 **Needs the new APK**: the client builds its Learn tab locally from the compiled class tables. ->

- [!] 🟠 **ANSWERED — no, and here is why it does not.** shouldn't lvl 14 vamp bolt fail all the time fighting 37/39 mobs ? I hit them for ~300 and vamp ~120.
  -> 🔑 **The fizzle roll reads the CASTER's level, never the skill's** — `fail% = 1.3^(mobLvl −
  casterLvl) × …`. You are above those mobs, so you are casting DOWN, and casting down is **0%**. The
  rung is not in the roll and never has been (that was measured and ruled on 2026-08-22; the table is in
  `docs/balance/BalanceMatrix.md`). Your 120 is exactly 40% of 300, so the drain is working too.
  - 🔑 **The interesting half is the 300, not the fizzle.** Damage is `K·(mAtk·lvlMod + power)/def`. Your
    rung-1 power is **21**. At your level `mAtk·lvlMod` is in the hundreds — so the rung contributes
    something like 3-5% of that hit and your *gear* is doing the rest. That is why a level-14 skill still
    works at 40, and it is the same reason buying rungs feels like it does nothing.
  - ⚠ **And a Warchanter never gets another rung.** Rungs 2-5 of Vampiric Bolt are on the **nuker**
    ladder; the cleric line takes rung 1 at 14 and stops. So this is not "a low rung I haven't upgraded",
    it is the only rung your class will ever have — which is a decent argument for your OWN next row,
    the one that retires Holy Bolt for the sound skills. Vampiric Bolt is the same kind of leftover.
  - **What I did NOT do:** put the skill's rung into the fizzle roll. It would break every class that
    legitimately carries an old rung, and the honest fix for "a rung-1 nuke is still fine at 40" is the
    power ladder, not a landing penalty. Say if you want it looked at as a balance pass. ->

- [~] 🟢 **BUILT (CLIENT).** toggle skill on the skill bar should be marked with aqua border or different color (not so bright just different form the rest) when "on" (I see the buff but more visual is OK)
  -> **A muted aqua ring, drawn OUTSIDE the green auto ring** so a stance that is both on and auto-marked
  shows both instead of one hiding the other (aqua peeks 5px, green 2px). The colour is deliberately
  desaturated, per your "not so bright": a saturated cyan on a dark bar reads as an ALERT, and a stance
  being on is the opposite of an alert — it is the state you meant to be in.
  - No protocol change and no server work: **a live toggle is already a buff on the bar** under the
    skill's own key with no timer, so the client always had the answer — nothing was reading it from the
    SLOT's side. Which is also the honest version of your "I see the buff but more visual is OK": the
    information was there, it was just in the wrong place for a thumb. ->

---

## My Finds — next pass (empty, write here)

*The twelve above are answered — this is the blank page for whatever the 0.81.1 build turns up.*

⚠ **What is worth aiming at first**, because it is where this pass changed the most and none of it has
been played: the **buff bar** (11 NPC buffs + potions, nothing should silently fade any more), a
**Warchanter with a maul** (Sound Smash / Acoustic Shock must fire), **Reinforcement and Sharpening**
under a full buff set, and **relogging with chat on screen**.

- [ ] 

- [ ] 

- [ ] 

---

## 91. YOUR SIX FINDS OF 2026-08-22 — five built, 0.78.0

🟢 = built and ready to confirm. Every row is **server-side unless it says otherwise**; the three that
need the client are marked, and **the 0.78.0 APK does not exist yet** — say the word.

- `91a` 🟢 [~] - **A NAME CANNOT BE NOTHING ANY MORE.** *"I can register two chars named - " " & " ""*.
  🔑 `Trim()` was never going to catch it: **U+200B ZERO WIDTH SPACE is not whitespace to .NET**, so one
  and two of them are two different non-empty names that both draw as nothing. One rule now, in
  `Game.Shared`, run by the server AND (once rebuilt) the create screen: **English letters and digits
  only, 3-16, must start with a letter.** Your three questions answered as one narrow rule —
  **Cyrillic is out**, spaces are out, symbols are out. The reason is not tidiness: `/whisper`,
  `/ptinv`, `/jail` and the friend list are all name-addressed, so a name must be typeable on *every
  other player's* keyboard, and `/role <name> <role>` splits on the last space. ⚠ **The 3-character
  minimum is mine, not yours** — say if you want 1 back. ⚠ Case-insensitive matching was **already
  there** and was not the bug. -> 'symbols like _ . - I see no reason why cannot be included. Players should be able to separate `Name_.-Family `
  ⤷ 🟢 **DONE — the three symbols are legal now** (`_` `.` `-`, anywhere after the first character,
  consecutive ones included, so `Name_.-Family` is exactly what your example says it is). You are right
  that they cost nothing: none of the three is a token separator, none needs a keyboard layout nobody
  has, and none can be confused with *nothing at all*, which is what the rule was actually built for.
  **Everything else stands** — no spaces, no Cyrillic, must start with a letter. ⚠ **CLIENT + SERVER**:
  the create screen runs the same shared rule, so it needs the APK to stop refusing them.

- `91b` 🟢 [x] - **`@target` WORKS ON EVERY COMMAND THAT TAKES A NAME.** *"a player named "IlIlllIIllI"
  for a human is impossible to read"*. All four spellings — `@target`, `%target`, `@t`, bare `~` — and
  it is substituted **once, on the client, before anything is parsed**, so it covers commands that do
  not exist yet: `/jail @target`, `/w ~ hello`, `/ptinv @target`, `/give @target sword1h_t10`. Whole
  tokens only, so an ordinary message is untouched; with nothing targeted the command is **not sent at
  all**. ⚠ **CLIENT-SIDE — needs the APK.** ->

- `91c` 🟢 [x] - **`/server shutdown|reboot|on [minutes] [adminOnly]`**, built to your spec.
  `-`/blank/`0` = instant, **unparseable = 30 min**; each command replaces the one before it, which is
  why `on` is the cancel. `adminOnly` writes a flag beside the exe and the server comes back **staff
  only** until `/server on` — checked per CHARACTER, so an admin's ordinary character is refused too.
  **Your announcement ladder verbatim**: `/server shutdown 117` → `1:57h, 1:00h, 50, 40, 30, 20, 10,
  9…1 min, 59…1 sec`. 🔑 When it fires it **saves every character first**, on the tick thread, before
  the process goes down. ⚠ **It is NOT your "red big" permanent overlay yet** — every line rides the
  existing toast + System chat, which is what let it work with no protocol change. That half is
  **`BL-86`** and is your call. ->

- `91d` 🟢 [~] - **FIVE STAFF RANKS — plain names, fantasy titles.** Your ruling when asked: the enum,
  `/role` and every system message say **Owner / Admin / Moderator / Chat Moderator / Player**, and the
  worn TITLE keeps your words — **Supreme Being · God · Sentinel · Silencer**. 🔑 **The Chat Moderator
  has (un)chatban and nothing else** — no kick, no jail, no `/where` — for exactly the reasons you
  gave. 🔑 **The Owner is `owner.txt` beside the exe**, one name, read once at startup, not in the DB
  and not reachable from any command; a fresh database seeds it with `Admin`. ⚠ **A real bug fell out:**
  `/role` allowed granting your OWN rank, so any admin could mint another admin — the comment on that
  line said the opposite. Now strictly below your own, which is what makes Admin the Owner's gift.
  ⚠ **The enum's NUMBERS moved**, so this is the second reason to delete `game.db`, and old clients
  misread every rank. -> can u make it for time being if now owner.txt is missing at start to create it with name Owner - each time I remove the GameServer folder it will remove it as well - make a comment to delete the file creation when game going public. Otherwise the rank/roles work
  ⤷ 🟢 **DONE.** `ServerControl.EnsureOwnerFileForDev()` runs at startup and writes the file when it is
  missing, containing the name **`Owner`** plus two comment lines telling you to put YOUR character there
  and restart. 🔑 **Why the old seed did not cover you**: it only fires on a fresh DATABASE, and your loop
  is the other one — you delete the deployed FOLDER, which takes `owner.txt` with it while the DB you keep
  survives, so the second install had no Owner and no way to appoint one (the rank is deliberately
  unreachable from any command). ⚠ **The method is named `...ForDev`, shouts in a comment block, and both
  it and its one call site say DELETE BEFORE PUBLIC** — a file that writes itself is a file an attacker can
  predict, and on a real server a missing owner.txt must mean NO owner.

- `91e` 🟢 [x] - **THE ADMIN FULL BUFF FOLLOWS THE BUFFER NOW, AND AT MAX RUNG.** Both halves of your
  find were real. It was a **hand-written list** that had gone stale exactly as you predicted — it is
  now **derived from the Warchanter's own class tables**, so a new rung/harmony/group in
  `buffer 3rd.csv` appears with no second edit (**34 entries → 62**). And it applied **everything at
  level 1**, which is why your harmonies were L1; each buff now lands at its own top rung — Harmony of
  the Warrior at 6, Harmony of Protection at 5. ⚠ The paid NPC buffer is untouched. ->

- `91f` 🟢 [~] - **`/buff [name] [level]`.** `/buff` alone = the whole set at max. The matcher is
  forgiving in tiers — exact · acronym · every word in order · prefix · substring — because **your own
  example does not match literally**: you type "harmony of wizard" for a buff called "Harmony of **the**
  Wizard", so joining words are skipped on both sides, and acronyms are generated three ways (`hotw`,
  `how`, `hw`). An out-of-range rung is **not clamped**; it tells you the range, per your own sentence.
  ⚠ `hw` really is ambiguous — Warrior and Wizard — and it says so rather than guessing. -> the name was a player's name ...`/buff @target [buff-name-optional] [level-optional]` lates make each player name I have to write I can do -> Gena == @t|@target or If I want myself -> MyName == @s|@self
	  - if my name is admin and player name is Ivan and I have him as my target
		- /buff @t == /buff @target == /buff Ivan
		- /buff @s == /buff @self == /buff admin
		- optional buff name /buff @s aim 1 == buf myself with Aim buff lvl 1 => acc +1
	- Now target don't work cannot buff no1 else except me ...and I have a button...
	- other symbols than @t/target or @s/self should not work
	- ~ symbol should be used for relativity as for example /tp 100 123 -> teleports me on exactly 100x and 123y ...while /tp ~100 ~-50 -> teleports me current coordinates +100x and -50y
	- /where should work for anyone - they can see their own map coordinates -> to tell friends where to find them -> while /where player-name should work only for admins+
  ⤷ 🟢 **ALL FIVE DONE.** I had read "the name" as the buff's name; it was the player's. `/buff` applied
  to the caster and nothing else, which is the bug you hit.
  - **`/buff [who] [buff] [level]`.** The client turns `@t`/`@target` and `@s`/`@self` into a NAME before
    sending, so the server only has to decide whether the first word is a person: **if it names someone
    online it is the target, otherwise the whole argument is the buff and the target is you.** So
    `/buff @t`, `/buff Ivan`, `/buff @s aim 1` and the old `/buff aim 1` all read the way you wrote them.
    The target gets a "so-and-so blessed you" line. ⚠ A player named after a buff would shadow it —
    resolved toward the PLAYER on purpose, since a buff can always be reached with `/buff @s <name>`.
  - **Only `@t`/`@target` and `@s`/`@self` are tokens now.** `%target` is gone and so is bare `~`.
  - **`~` is the RELATIVE prefix**, and `/tp` gained coordinates: `/tp 100 123` is exact, `/tp ~100 ~-50`
    is "+100x, −50y from here", a bare `~` means "unchanged" so `/tp ~ 5000` walks straight north on your
    own x. Mixing is allowed. Clamped to the world bounds. `/tp <name>` is untouched.
    🔑 **This is why `~` had to stop meaning "my target"** — one character cannot mean both on one line.
  - **`/where` is now two commands.** Bare `/where` works for **anyone** and reports your own coordinates
    (and the town you are standing in, if any). `/where <name>` stays **staff**, unchanged. ⚠ The client
    used to refuse every `/` command from a non-admin before it left the phone, so this needed a client
    change too — **needs the APK**.

- `91g` [~] - 🔴 **FOUND ON THE WAY PAST, NOT BUILT — `BL-85`, and it is your Combo Rush rule again.**
  `/buff harmony of protection 3` on a fully-buffed character **downgrades it from rung 5**, and the
  command is not at fault: every rung of a Harmony shares **one** `Rank`, so rung 1 and rung 5 are
  equal, and equal rank keeps whichever has longer left. In a party that means **a level-44
  Warchanter's Harmony Lv1 replaces a level-74's Lv5** once the Lv5 is under five minutes. The single
  ladders are fine (each rung is a separate child def with its own rank); it is the four harmonies,
  Great Might, Great Bulwark and Mana Blessing that fall through. **The fix is one line and it is
  deliberately not in this batch** — `BuffPlan` is the resolver every buff in the game goes through,
  and moving it in the same version as two 3rd-class kits would make this pass unreadable. Ruling
  wanted: build it next, or fold it into the pass? -> I can't see a buffs rank once I have it as effect - I see it in "known" as `Aim Lv.1` but once is in the effects bar and click on it to open details. The title just says Aim no lvl.. 
  ⤷ 🟢 **DONE — that is a different bug from `BL-85`, and a simpler one.** The server knew the level all
  along (`BuffInstance.Level`, kept so a buff can be rebuilt on login) and simply never sent it, so the
  one screen you go to in order to ask "which rung am I carrying" could not answer. `BuffDto` gained a
  `Level` and the popup title now reads **`Aim   Lv.1`**, spelled the way the Known tab spells it.
  ⚠ Only when the buff actually HAS a ladder — "Frenzy Lv.1" on a one-level buff is noise, so the server
  sends 0 there and the title stays clean. ⚠ **Protocol 23 → 24, needs the APK.**
  🔴 **`BL-85` itself is still NOT built** — that is the rank collision, still wanting its own increment.

---

## 90. THE 0.77.0 PASS — six versions that have never been played

**This is the section for this pass.** Two whole 3rd-class kits are the headline; everything under D
and E is engine work that has only ever been checked by build, smoke test and BalanceMatrix.

### A. The two 3rd-class disciplines — `BL-02`, two of ten

- `90a` [] - **THE LIGHTBRINGER, 40-74** (0.74.0, off your finished `healer 3rd.csv`). The first
  authored 3rd-class kit in the project. **Race splits it twice**, once on the heal and once on the
  debuff: **Human** Quick Great Heal + Gravity · **Elf** Healer Blessing + Bind · **Ork** the Healing
  Totem + Armor Break. Everything in the file is built to **74**; nothing above it exists. 🔑 What is
  worth your attention is not whether the skills fire but whether the kit **plays as a healer** —
  heal-per-cast against a fight's damage, MP against a fight's length. ->

- `90b` [x] - **THE WARCHANTER'S BUFF LAYER, 40-74** — nine group buffs split by lane and four Harmony
  ladders. 🔑 **A harmony does NOT evict your singles**: it carries its own key, so it stacks and the
  two multiply. That is the design, not a bug — but it is the thing most likely to look wrong from
  inside the buff list. ->

- `90c` [x] - **THE WARCHANTER'S NON-BUFF HALF** — 16 families, and the race split IS the class, in your
  own words: **Human** heavy + blunt + shield, Sound Smash · **Elf** light + bow, Sound Burst (two hits,
  900 range) · **Ork** heavy + blunt, Sound Smash **plus your new Acoustic Shock** (Sound Smash's twin
  ladder with a contested 5s stun, ork-only). ⚠ **Two defects in your CSV were found and fixed** on the
  way in, and you should know because both moved a built number: Spell Mastery's second `mAtk +15` is a
  **P.Atk** (your own `cleric 2nd.csv` writes it that way, and rung 5's P.Atk moved 18 → 15), and Armor
  Mastery's **Light** row was stripped of its speed clauses from rung 5 up — stacked with Harmonist Light
  Mastery they drove an elf straight into the cast-speed clamp. **Do not put them back.** ->

- `90d` [x] - **Combo Mastery — the FIRST ON-HIT PROC in the game.** A new engine primitive: a chance on
  landing a hit to cast a skill at yourself or the party, on its own cooldown. Nothing else in the game
  works this way, so it is the one most likely to misbehave under fast attack speed. ->

- `90e` [x] - **Harmonist Bow Proficiency — an elf Warchanter with a BOW is a full caster.** It is the
  first skill in the game that **undoes** the untrained-caster-weapon rule: your `cast x2, mAtk x2,
  mAcc x0.04` are the exact inverses of Spellcaster Mastery's bow penalty, so the three multipliers
  cancel to 1. ⚠ It only works with the passive learned — a bow before it is still a ×0.5 / ×0.5 / ×25
  fizzle punishment. ->

- `90f` [] - **Combo Rush — the one ladder in the game that goes BACKWARDS, deliberately.** Rungs give
  AS 5/7.5/10/10/15/20 and cast 2.5/5/7.5/**5**/10/15; rung 3 → 4 loses 2.5% cast on purpose, your call:
  *"even if some other buffer procs lvl 3 buff u still get your effect over"*. One family, one key, index
  as rank — so your own rung 4-6 always outranks a party-mate's rung 1-3. ->

- `90g` [x] - **Shield Mastery, one skill on two classes** — tank at 20/28/36/**52**, Human Warchanter at
  40/60/70 (no rung 4). 🔑 Your CSV percentages are **IG units**; the build multiplies the shield-P.Def
  column by 5 and leaves everything else literal. The bow resist is back at rungs 3/4 (16% / 24%), and
  the +10% P.Def is **shield-gated** — no shield, no bonus. Both your rulings. ->

### B. The two kits' cost to everything else

- `90h` [~] - **Quick Heal is back on the Learn list, at SP you have already paid.** Fallout from the
  Harmony fix (§D): Harmony used to replace Quick Heal, so buying Harmony stripped it. Nothing replaces
  it now, so it returns at the cleric's 20/25/30/35 rungs. ⚠ **You will be asked for the SP a second
  time.** Say if that is unacceptable and it becomes a refund. -> harmony of restoration replace e party heal .. Heal is replaced buy great heal .. We need something that replaces quick heal - what will be good replacement for it or which has the Mos logical to replace it ..harmony of protection ?or any of the 3 passives (shield/2h/bow)?
  ⤷ 🟠 **MY ANSWER: NOTHING SHOULD, AND NOTHING NEEDS TO — no code changed.** Both candidates you name
  would re-create the exact bug this row is: Harmony of Protection is a defensive BUFF and the three
  masteries are weapon passives, and wiring `Replaces` between unrelated skills is what stripped Quick
  Heal in the first place (a passive called "Harmony" was replacing a heal).
  - **`Replaces` is for "this is strictly the better version of that", and the ladder is already whole**:
    Human Lightbringer → **Quick Great Heal** replaces it (Great Heal's power on a 2s cast). Elf
    Lightbringer → **Healer Blessing** replaces it (heal + cure in one). Ork Lightbringer → keeps it,
    correctly: his answer is a **Healing Totem**, which is a different tool, not a better Quick Heal.
  - **The Warchanter keeps it too**, and should: his own heal is a Renew verse — a party heal in a 600
    radius **centred on himself**. It cannot reach a hurt ally across the field, so it does not supersede
    a 600-range targeted heal. Take Quick Heal away and the buffer loses a real ability.
  - **The double SP charge answers itself**: it only existed for a character that had already paid before
    the strip, and your `game.db` delete is owed twice over anyway. On a fresh DB nobody pays twice.
  - ⚠ Say the word if you would rather have it gone from the buffer for kit-size reasons — that is a
    different argument (too many buttons), and then the honest tool is removing the learn rows, not a
    `Replaces` that lies about why.

- `90i` [x] - **Every group buff now PRINTS ITS NUMBERS on the card.** *"War frenzy have no description"*
  was really "no numbers": a group buff has no magnitudes of its own — they live one hop down in the
  rungs it names — so the card printed prose and then said nothing. War Frenzy now reads
  `Max HP −10% | Max MP −10% | P.Atk +8% | M.Atk +8% | Cast speed +8% | Atk speed +8% | Move speed +8 |
  Evasion −8`. It affects War Might/Bulwark, Frenzy and **all ten Warchanter groups**. ⚠ It immediately
  caught a stale line of its own: Frenzy Lv1's prose claimed −30% Max HP/MP over a rung that gives −7%.
  The prose was wrong, the rung was right, and the prose was corrected to it. ->

### C. What you can SEE — the client half

- `90j` [x] - **TOTEM FOOTPRINTS AND AN AoE FLASH** — *"I want to see where it stands and the AOE so I can
  stand inside"*. 🔑 A totem was **never on the wire at all** (it is not an entity), so this is a new
  channel, not a rendering fix. Green HP, blue mana, both rings if a totem fills both pools; the ring is
  drawn at the server's real radius. The flash is 0.55s at the true footprint, coloured by what the
  skill does. ⚠ **Every AoE BUFF flashes too** — that is *"same goes for all AOE skills"* taken
  literally, and a full group-buff rotation is a lot of yellow in a row. If it reads as noise, say so:
  muting it is one line. ⚠ **A MOB's AoE does not flash** — deliberate for now, because telegraphing a
  boss's ground slam is a real balance decision and not a side effect of this. Say if you want it. ->

- `90k` [x] - **THE ORK MAGE'S ATK IS 31 → 47** — your find, and the root cause is worth one line: **IG has
  two power stats and we have one.** Our mage ATK column was IG's **INT**, copied straight across, which
  took the half of the spread the ork mystic LOSES and threw away the half he WINS (his STR is 25, the
  highest of any mystic). 47 = 41 × 25/22, derived from IG's own ratio rather than invented. 🔑 Thm09u54check
  that it is right is your own sentence — at 47 the **human mage is the middle value of all five stats**,
  ork owns CON/SPT/ATK, elf owns WIT/AGI. ⚠ **Base stats are read at character creation: this needs a NEW
  character.** ⚠ It lands on every ork MAGE, so the ork nuker gains ~+12% M.Atk too. ->

### D. Playtest 26's three finds — read, then confirm they are gone

All three fixed, all three inside the 0.77.0 APK.

- `90l` [~] - **Harmony of Restoration can be cast again.** The gate read the `RestoreMp` **flag** — every
  MP source in the game — and a Warchanter is himself a mana-restorer, so his own party HoT refused
  itself. It is now the skill id, per your ruling *"only Restore is forbidden"*. **Freed: Harmony of
  Restoration, Mana Totem, Restore Spirit.** Restore Mana still refuses a restorer, which is its own
  rule. Auto-hunt carried a copy of the same test and was narrowed with it, so auto and manual refuse
  exactly the same casts. -> they work just the harmony of restoration the mana part is no different of the healing -bshowbsame green 10 as +100 whilenthebmanantotrem is a blue 20
  ⤷ 🟢 **DONE — one word.** The MP tick was broadcast as `CombatOutcome.Heal` with the skill named
  "Mana", so it drew the green heal number; the distinct **`ManaHeal`** outcome already existed (it is
  what makes the Mana Totem blue) and this one path was written before it and never moved. Harmony of
  Restoration's mana half now reads **`+20 MP` in blue**, like the totem. ⚠ Server-side, live on restart.

- `90m` [x] - **War Frenzy now removes Frenzy — including on a character that already owns both.**
  `Replaces` named `cast_frenzy`, an id **no class learns**; everyone is granted `holy_frenzy`, whose
  display name is plainly "Frenzy". 🔑 The more important half: `Replaces` was enforced **at learn time
  only**, so a correction reached nobody who had already spent the SP. Superseded ids now **die on
  load**. Your existing Warchanter is fixed by logging in — no new character needed for this one. ->

- `90n` [x] - **Harmony of Restoration replaces PARTY HEAL**, your own correction. Great Heal takes Heal,
  Harmony takes Party Heal, Quick Heal survives as the fast single-target (see `90h`). All fourteen
  `buffer 3rd.csv` rows moved with the code. ->

### E. Engine and economy — never played, only measured

Six versions of server work. Nothing here has a client tell; it shows up as numbers feeling different.

- `90o` [] - **`BL-78`: THE CREATURES STOP BEING PAPER (0.73.0) — the question this pass exists to
  answer.** Your *"mobs feel easy"* got its two halves built: **P.Def, M.Def, P.Atk and M.Atk are now
  four smooth `a·(L+shift)^k` curves**, refitted off **2,831 IG creatures** in the chronicle you actually
  play, and the old table's two discontinuities went with them. ⚠ **HP DID NOT MOVE** — that is your own
  park (*authored later, with instances*), so an 80 mob is still ~5k and not the 15k you named. So the
  question is narrow: **with defence and attack fixed but HP unchanged, do they still feel like paper?**
  ->

- `90p` [] - **A skill has ONE MP price, and the engine splits it 20/80.** One `MP` column, the number
  you are quoted, and the gate demands **all** of it before the cast starts — you can never begin a cast
  you cannot finish. An interrupt costs you the 20% and nothing more. ->

- `90q` [] - **Mana Ray drains a SHARE OF THE TARGET'S MP POOL**, not a magic-damage number — ~14.5% a
  cast, seven casts to empty anyone. 🔴 You brought IG's own drain formula, it measured within ±8%, and
  you ruled *"leave it as is"* — this row is only asking whether it FEELS right in a fight. ->

- `90r` [] - **Magic crit: rate cap 50% → 40%, damage ×2, and the rate ladder gets headroom** (8/16/32%).
  The cut buys room under the cap for the 40+ rungs that now exist. ->

- `90s` [x] - **`mpWhenRestored` is a PERCENT**, and casts and totem pulses go down one pipe. 🔴 Anchored
  at 80 it is worth **−25% to −57% below level 60** — that is your own CSV row, flagged rather than
  silently rescaled. ->

- `90t` [] - **Rates: ×N now means ×N of everything, and never clamps.** Above 100% a drop pays **COPIES**
  (250% = two plus a 50% roll for a third), so the guaranteed-group exemption came off — a 100% mats
  group at ×30 fires 30 weighted picks in your authored proportions. 🔴 **Bug fixed in passing: quest gold
  and quest SP were paid RAW** — on a ×30 server every quest paid ×1. ⚠ **`DropAmount` is not a rate**;
  setting it too squares the multiplier, so it left the Debug panel for `/droprate amount <x>`. ->

- `90u` [] - **Buff prices are per-rung, verbatim from your sheet** (`RungCost[]`). ⚠ The irregular
  spacing is correct — it is what you authored, not a smoothing error. ->

- `90v` [] - **The debuff contest gets a LEVEL, and mobs get stats in human ranges.** 🔑 The level term
  scales the **defender's** stat. 🔴 **Known and unfixed**: the attacker's level is read as the RUNG's
  learn level, so all five CC skills expire out of usefulness — the fix is a CSV ladder and it is owed
  from you. ->

- `90w` [] - **Renames — display names only, every id untouched.** `Haste` → **Fury** (the consumables
  followed: Fury Potion, Scroll of Fury) · `Provoke` → **Taunt**, which is your name and your four-rung
  ladder at 24/28/32/36 · **Resolve caps at +54** (the 60 rung is commented out, not deleted — *"no1 is
  leatrning it atm"*) · Alacrity gained **rung 3 at 48**, the cast-speed buff missing from the healer. ->

- `90x` [x] - **One caster weapon penalty left, and the healer took his own masteries.** Sword and blunt
  are ×1/×1, **Divine Focus is deleted**, and the fork is blunt-only / robe-only. 🔑 The gate is the
  weapon TYPE. 🔑 The fizzle chain is a **product** — a `0` means "not in the chain", never "zero
  chance". ->

- `90y` [x] - **The magic fizzle curve, written down** (`docs/balance/BalanceMatrix.md`, marked CURRENT,
  plus `BalanceMatrix --fizzle`). 🔑 It reads the **caster's** level, never the skill's: casting DOWN is
  **0%**, up is 5% at +6, 18% at +11, 67% at +16. 🔑 M.Def and mRes are **not** in the roll — and a
  fizzle still lands `dmg/3`. **Read, don't test.** ->

---

## 0. ANSWERS I OWE YOU — read, don't test

### ✅ Closed since the last update

- ✅ ~~**`BL-78`'s defence and attack halves**~~ — **built in 0.73.0**, see `90o`. The HP half is parked
  by you until instances exist. The old curve was not sloppy, it was faithful to an **older chronicle of
  IG**: the public databases disagree by ~3× because they are different versions of the game. That
  reasoning is written into `MobBaseStats.cs` rather than deleted, because "measured against IG" is not
  one number.
- ✅ ~~**Whether the ork mage's 31 ATK was really IG's**~~ — it is, verbatim, and that was the bug rather
  than the evidence against it (`90k`).
- ✅ ~~**IG's mana-drain formula**~~ — measured against ours (within ±8%), and you ruled *"leave it as
  is"*. Closed; I will not re-propose it.
- ✅ ~~**The buffer file's completeness**~~ — *"Ok i finished the buffer"*, the `NOT DONE` banner came
  off, and the whole file is built. `--check` is **green on all ten files, for the first time ever.**

### 🔴 Still yours to rule

- 🔴 **`BL-47` — the ONE question left on mobs-as-players, and it is a yes/no.** You marked the demo
  *"It works"* and then named its real cost: *"with current mobs we can say 'this one will have x2 hp'
  and whole the mobs on the field are altered.. while with the pMobs we will alter one and it will be
  good in the lvl range (+-5) not across the board."* **That is correct and structural** — one function
  moves every creature; a per-creature loadout has to be re-authored one at a time. But you also named
  where they *should* go: **town guards** and **fortress sieges**, both hand-placed and few. So:
  **do ordinary field creatures stay on the `MobBaseStats` curve with ×2 passives, and player-built mobs
  become a hand-placed CONTENT tool instead of the general pipeline?** Everything already built serves
  that shape unchanged. Say yes and `BL-79`/`BL-80` are the roadmap.
  ⚠ **One thing from the demo you never commented on**: `88a`, whether the level-45 Elder Raider felt too
  soft beside the level-40 Raider. It only matters if a pMob carries a ±5 band at all.

- 🔴 **`BL-49` — the levelling curve, not the boss rule.** One **level-20** field boss is **125% of a
  level** solo while a level-85 one is **0.1%** — the same 150 trash kills either way. §85j moved the
  boss multiplier where you asked, and that spread survives it untouched, because it is the EXP curve.
  ⚠ **`BL-13` sits on top of this**: a boss that takes 3-10× longer carries 3-10× the EXP with it.

- 🔴 **The CC ladder is yours to author** — `90v`'s red half. The attacker's level in the debuff contest
  is the rung's learn level, so every CC skill expires out of usefulness; a rung ladder in the CSVs is
  the fix, and nine CC skills are today learnable by nobody at all.

- 🔴 **`BL-22` salvage: the S row cannot be moved by this feature at all.** Your budget was *"10~20%
  decrease in time"*; the early rungs got exactly that (E −3% · D −10% · C −18%) and **A and S got −0%**.
  The cause is your own *"rarity for mats rarity"* mapping: salvage pays the rarity of gear that
  **drops**, and a normal mob and an **elite both cap at Epic** — only a boss (0.09 kills/h) drops
  Legendary. The A and S recipes bind on **Legendary Ingot**. `M13` in BalanceMatrix prints all three.
  ⏸ Parked with the rest of crafting, along with the **603h craft time you accepted**.

- ⚠ **The buff-vs-heal threat ratio is off by ~8×, and the buff is not the wrong half.** You sized it
  against a ~1500-power quick heal at 70; the cleric's heal ladder stopped at skill level **4** (learned
  at 35, power **301**). **`BL-16`** is the half that had not caught up. ⚠ **This is now partly
  answerable**: the Lightbringer's 40-74 rungs exist, so the heal side finally has numbers above 35 —
  `90a` is where you would feel it.

- ⚠ **Numbers that are mine, not yours** — each flagged in the source: the top rung of **Madness**; the
  Ultimate Scroll of Resurrection's **15,000 Value**; the three subclass-swap clauses; and the
  **0.25 respawn exponent**, which your `85j` park leaves standing as mine.

- **The heavy sets' shield clauses are still unchanged PERCENTAGES** (`shield.p.def x1.10 / x1.25 /
  x1.30`). Left alone a fifth time on purpose: §79c moved the block channel and you passed it, and
  `90g` has just moved Shield Mastery — moving these in the same pass would make neither reading
  attributable.

- **`/give`'s `sellPrice` argument, your `[?]`.** `-1` → *unsellable* · `0`, `-` or omitted → use the
  catalog's price · any positive number → that exact price (`k`/`m`/`b` and `1_000_000` both parse).
  Every argument after the item id follows the same rule: `-` is always *no opinion*.

---

## 89. PLAYTEST 25'S ROUTING — three UI changes are STILL owed

The eight finds became `BL-13`, `BL-47`, `BL-78` … `BL-83`; that table lives in
[docs/Backlog.md](../Backlog.md), and only `BL-78`'s two halves have been built since (`90o`).

**The three UI changes are now `BL-88`** (2026-08-23 — three passes without an id was the problem). Still
not built; small, client-side, and they ride the next batch:
- **The target window's title row** — *"only the name of the target. No lvl no target.title, now the
  [title + name + lvl] overflows"*; the mob title moves down into the `Mob:` row.
- **The chat window's buttons** — *"decreasing the width of the chat leaves the [combat] button floating
  in the air - make the buttons smaller or like the icons on the top"*.
- **The gear picker, second pass** (`87f`) — *"Make the buttons even smaller. Like the tab buttons in
  height. Also there is no splitter bellow the [S 80] button."*

---

## 85. NEVER REACHED — still owed from the 0.68.0 batch

✅ `85b` `85c` `85d` `85e` `85f` `85g` `85h` `85i` `85m` all closed — see the
[playtest-24](Playtest-Archive.md#playtest-24) and [playtest-25](Playtest-Archive.md#playtest-25)
archives. `85c` came back as a **reversal** and is now `BL-83`.

- `85n` [~] - 🔑 **TWO OF THE TEN 40+ FILES ARE NOW AUTHORED AND BUILT** — `healer 3rd.csv` (0.74.0) and
  `buffer 3rd.csv` (0.76.0), which is `90a`-`90g` above. That is this row moving for the first time in
  four passes, and it is the biggest unlock in the project doing exactly what it was always going to do.
  **Eight disciplines are still empty**: `tank` · `warrior` · `war_aoe` · `dual` · `archer` · `nuker`,
  each `3rd` and `4th`, plus the two `4th` files for the ones you have finished. They are seeded in
  `docs/data/classes_skills_csv/` holding **exactly what the game already registers above 40** — nothing
  is invented — so you start by editing, not from an empty sheet. Your own playtest-25 note still applies
  to the other eight: *"mage is the only one with @40+ skills"*. **Nothing to test — author them.** ->

---

## 81. THE TWO REFLECTS — never reached in playtest 23, 24, 25 OR 26

⚠ **`87a` is confirmed green**, so the reflect-flag bug is fixed on all three paths. These two are the
other two paths and have still never been played. Check the flag behaviour in the same sitting.

- `81b` [] - **`Deflection` — physical-skill reflect, warrior.** *"default warrior @40 → 0.15 chance ×1
  reflected; @76 → 0.3 chance ×1 reflected."* Your numbers verbatim: the fraction stays **×1.0** at both
  rungs and only the **chance** moves. A landed physical skill rolls the victim's chance; on a hit the full
  damage goes back at the caster, **who can die to it**. Kept separate from the armour sets' `MeleeReflect`
  (5%, basic attacks only) — no blow is ever taxed by both, and two Deflection warriors terminate after
  one bounce. ->

- `81c` [] - **`Backlash` — debuff reflect, tank, 30%.** *"tanks get 30% chance to reflect a debuff →
  u cast on tank he reflects u get the debuff."* Rolled **before** the land contest on both debuff
  paths, because a bounce is not a resist: a tank who throws your stun back was never tested against
  it. The caster gets the effect with no resist roll of their own and no second bounce. ->

---

## CARRIED FORWARD — never reached in any playtest, needs a deliberate setup

- `0a` [~] - **Nuker vs champion, unbuffed.** Half of this closed itself in playtest 23: the **mage** can
  now farm solo (`79i`). The **champion** half is untouched — *"they both have hard time to farm without
  buffs"* — and is `BL-72`. ⚠ **The blocker on this row has cleared**: it was waiting on `BL-78` moving
  every TTK number, and `BL-78`'s defence and attack halves have now landed (`90o`). A reading taken this
  pass is a reading that stands. Do it in the same sitting as an auto-farm run. ->

- `37d` [] - A trade **shortfall aborts the whole trade** with nothing moved. ->

- `37e` [] - **Full-bag judging** on a trade: merges into an existing stack succeed, brand-new items
  are refused. ⚠ Interacts with `75d` — a tagged item is always a new row. ->

- `13a` [] - The **"take a break" banner**. ⚠ **Still at 10 MINUTES** — set there at your request for the
  0.68.0 pass, tagged in the source to go back to 3h, and reached by no playtest since, so it stays until
  you have actually read one (`GameConstants.BreakReminderSeconds`). ->

---

## KNOWN OPEN — not defects, don't spend the pass on them

**Everything you asked to be BUILT lives in [docs/Backlog.md](../Backlog.md) with a permanent id.**

- **`BL-02` the 40+ kits** — **two of ten done** (`85n`). The authoring format is settled in
  **[docs/design/Disciplines.md](../design/Disciplines.md)**: you author **by DISCIPLINE with a trailing
  RACE column**, **10 CSVs not 30**. Still the single biggest unlock, and your `85j` EXP park depends on
  it landing.
- **`BL-84` rename every skill id to match its name** — unblocked the day the healer landed, and your
  standing order puts it **after** the healer. Where an authored row hit an existing skill's exact slot
  the id was **reused rather than retired**, which is right for the data and wrong for reading code.
- **`BL-13` a boss is 10-30 minutes** — your ruling; the entry carries three jobs and only the first is
  arithmetic: lift the low end · **give a boss defence and attack** (today `Boss` is HP ×100 / ATK ×10
  with **no defence term at all**, which is exactly why it reads as a sponge) · re-base the target on a
  real party — tank + healer + DDs — instead of the three DDs the current table measures.
- **`BL-79` / `BL-80` / `BL-81` / `BL-82` / `BL-83`** — from playtest 25, all queued 🔴 except the
  fortress, which you said can wait.
- **`BL-05` / `BL-22` / `BL-50` crafting** — ⏸ parked by you; it wants its own ×100-rate playtest.
- **`BL-73` mob social clans** — off by one switch at your instruction, back on when the world map
  spreads the camps out. ⚠ `BL-80`'s garrison presumes clans, so it is one of that entry's prerequisites.
- **`BL-74` the game launcher** — still not treating the app as a game; research owed.
- **`BL-76` boss skill gems** — recorded, not built; five shape questions on the entry. ⚠ Read it beside
  `BL-13` — a 30-minute boss is a different drop proposition.
- **Instances** — you are holding (`BL-48`); the dungeons were the cheap half and are built. ⚠ This is
  also what `BL-78`'s HP half is parked behind.
- **Two playtest-20 bugs closed on a reading of the code, never re-tested**: Frost Bind stripping a
  dummy's/elite's HP multiplier (`BL-63`) and the target lost during a physical cast (`BL-64`).
