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
