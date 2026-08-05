# Playtest-17 — the 0.45.0 pass (owner, 2026-08-03)

**Source: `Open-Checklist-0.45.0.md`, filled in on the phone.** This file is the AUTHORITATIVE queue —
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
