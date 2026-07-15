# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs a change/tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## 📋 2026-07-15 PLAYTEST — RESULTS + NEW WORK QUEUE

Owner tested the 07-13 and 07-14 features. **VERIFIED WORKING** (details collapsed below):
subclasses · level cap + delevel + debug buffs · skill bar → DB · skill-bar readability + debug ·
stat-swap direction rule · skill-reset NPC · movable popups (great) · equipped-items pane ·
HealK=15 · OffChannelFactor stays 0.6.

**CHANGES NEEDED (found while testing) — not yet built:**
1. **Class uniqueness is on the wrong axis.** It bars a repeated ARCHETYPE; owner wants only a repeated
   **DISCIPLINE** barred. You SHOULD be able to own 4 mages (2 clerics = Lightbringer+Warchanter, 2
   nukers = Tempest + the other) — you just can't own two of the SAME discipline. → remove the archetype
   bar, keep discipline. **AND:** nothing caps subclass COUNT — you can add 20 mage classes. Needs a cap
   (or: base classes with no unique discipline left are pointless, so gate adding one).
2. **Mage-click revert.** ALL classes should click-to-attack (a mage out of MP needs to melee a mob to
   finish it). My "mage only targets" change was wrong — revert it.
3. **Skill cast must CANCEL the auto-attack walk, not pause it.** Double-clicking a mob starts a walk to
   melee; casting a skill only pauses the walk, so after the cast the character keeps walking to the
   target. Cast should STOP the move.
4. **Set info: only the BODY armor should show set requirements.** Right now boots (an accessory) show
   "3/4 heavy set" even after you've swapped your body to robe. A boots piece is not the set-defining
   piece. Show the set section only for the set-bearing body armor.
5. **Stat-swap groups: gate ALL groups by class, not just ATK.** Fighter may only do CON↔DEX, ATK↔CON,
   ATK↔DEX. Mage may only do CON↔DEX, ATK↔MEN, ATK↔WIT, WIT↔MEN.
6. **Stat-swap + training passives should require 3rd CLASS, not just level 40.** They currently appear
   at level 40; owner wants them only after the 3rd-class change.

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

### ⚠ Class uniqueness — WRONG AXIS, needs a change

- [~] **Bar the repeated DISCIPLINE, NOT the archetype.** Owner wants: you may own **4 mages** — two
      clerics (Lightbringer + Warchanter) and two nukers (Tempest + the other) — you just may not own two
      of the **same discipline** (no two Tempests). Currently the ARCHETYPE is barred (can't be a Nuker
      twice), which is too strict. → **remove the archetype bar; keep the discipline bar.**
- [x] **No repeated DISCIPLINE** — verified.
- [x] **Barred options greyed out** in the picker + server refuses them — verified.
- [ ] ⚠ **NEW HOLE: nothing caps subclass COUNT.** You can add 20 mage base classes, of which only two
      lead to a working (unique-discipline) endgame class. Needs a cap on how many classes a character can
      own (owner's design had 3-4), or gate "add class" so you can't stack pointless duplicates.

Still not built (player-facing rules): the 3-4 cap, safe-zone-only swapping, 5-min swap delay.

---

## MOVABLE POPUPS + MAGE-CLICK (2026-07-14)

- [x] **Every popup can be dragged / closed / raised** — VERIFIED, owner: "very good to rearrange so they
      don't get in the way."
- [ ] **NEW: persist popup positions** to the settings file (nested JSON per window: Window / inventory /
      skills…), saved on CLOSE (not on every move), defaulting when the file is untouched — start where
      you last left them, like L2. (Roadmap.)
- [~] **REVERT the mage-click change.** Owner wants ALL classes to click-to-attack — a mage out of MP
      needs to melee a mob to finish it. My "mage only targets" change was wrong.
- [ ] ⚠ **REAL BUG: skill cast must CANCEL the walk-to-target, not pause it.** Double-clicking a mob
      starts a melee walk; casting a skill only PAUSES the movement, so after the cast finishes the
      character keeps walking to the target. The cast should STOP the auto-attack move outright.

---

## ✅ LEVEL CAP + DELEVEL + DEBUG BUFFS (2026-07-14) — VERIFIED 2026-07-15

Full-buff debug button, level cap 90 (admins exempt), delevel −1/−10, delevel keeps learned skills
(training passive re-synced to the new level) — all confirmed working.

---

## INVENTORY: EQUIPPED PANE + SET INFO (2026-07-14)

- [x] **Equipped items have their own tab** (Equipped / Bag / Quest) — VERIFIED.
- [~] **Set info shows on the wrong pieces.** The set section now appears, but on ACCESSORY pieces too —
      e.g. after swapping your body from heavy to robe, the **boots still show "3/4 heavy set"** until you
      put the heavy body back. A boots piece is not the set-defining item. Owner wants the set requirement
      shown **only on the BODY armor** (the piece that actually carries the set), not on boots/gloves/helm.

---

## ✅ SKILL BAR → DB (2026-07-14) — VERIFIED 2026-07-15

Bar persists per character, follows the character not the machine, "learn all" no longer reshuffles it,
cooldown no longer freezes a slot, cooldown countdown readable — all confirmed.

---

## ✅ SKILL BAR + DEBUG (2026-07-14) — VERIFIED 2026-07-15

Readable bar text, +10,000,000 gold button, debug class change keeps inventory — all confirmed.

- [x] **DRAG & DROP on the bar** — owner: *"I think I tested it wrong the whole time — I was trying to
      drag from the skills MENU (like the first time), not rearrange the buttons ON the bar."* The rework
      was about rearranging the slot buttons on the bar, which now works. ✔ (If dragging a skill FROM the
      skills window onto the bar is also wanted, that's a separate feature — say so.)

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

## LEVEL-40 STAT-SWAP PASSIVES (2026-07-13) — mostly verified, 2 changes wanted

The ONLY thing that moves your main stats now. Born with CON/ATK/WIT/DEX; old free grants gone.

- [x] Gold-priced (1kk-5kk/level), affordability-gated, deducted — VERIFIED.
- [x] A group is a PERMANENT commitment; the alternative disappears — VERIFIED.
- [x] The stats REALLY change (Max HP / eva-acc-crit-AS / cast-MP-crit / P&M.Atk) — VERIFIED.
- [x] MEN is no longer a stat (±2% MaxMP/M.Def/MP-regen per point) — VERIFIED.
- [x] The reset NPC is BUILT (Mindwright Sela) — this old "NOT built" note is stale.
- [~] **CHANGE: gate ALL groups by class, not just ATK.** Right now only the ATK group is class-locked.
      Owner wants:
      - **Fighter** may only do: CON↔DEX, ATK↔CON, ATK↔DEX.
      - **Mage** may only do: CON↔DEX, ATK↔MEN, ATK↔WIT, WIT↔MEN.
      (So e.g. a fighter should NOT see WIT/MEN swaps, and a mage should NOT see the DEX-cost ones.)
- [~] **CHANGE: require the 3rd CLASS, not just level 40.** The swaps AND the training passives
      (Spirit/Body Training) currently appear at level 40. Owner wants them to appear only after the
      **3rd-class change** — i.e. 3rd class → then training + swap skills show.

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
- [ ] **Go Offline (Auto-Farm)** button → you return to account select and your char keeps farming
  (offline), visible to others, until the 2h cap / death / relogin.
- [ ] Drop while **auto-farming** (out of town) → offline farm (2h cap). The 2h cap is ONLY for
  offline farming — NOT for a network blip.
- [ ] Drop **mid-combat but not auto-farming** → your char **keeps defending** its current target
  (anti-combat-log) and the 180s grace timer is **paused** until combat ends (30s after the last
  hit); then the grace counts down. It is NOT put into the 2h offline farm.
- [ ] Drop while **out of combat, not auto-farming** → your char shows a **"⚠ Disconnected"** title
  above its head to nearby players, stays frozen and **in your party** (OFFLINE tag) for **180s**.
  Reconnect within 180s → resume seamlessly. After 180s → normal removal (leaves party).
- [ ] A disconnected (grace) char that a mob kills is removed immediately.
- [ ] Offline-FARMING chars still look like normal players to non-party (no Disconnected title);
  only the grace state shows the title.

---

## To test now (auto-hunt / idle farming — Phase 1, 2026-07-08)

**⚠️ Schema change:** added the `AutoHuntJson` column → **delete `Game.Server/bin/Debug/net8.0/game.db`
(+ `-shm`/`-wal`)** so it recreates before running.

### ✅ Auto-Hunt window + Behavior (Phase 1) — VERIFIED 2026-07-15
Auto-Hunt button/window, per-skill enable + reuse, HP/MP potion %, condition logic (attack on cd, buff
if missing, debuff if target lacks, self-heal <70%), auto-potions with auto off, Mana/s footer, normal
loot/XP — all confirmed.

### Offline farming (Phase 2, 2026-07-08)  *(NOT verified this playtest)*
- [ ] With auto-hunt **ON** and standing in a mob field (not a town), **close the client / disconnect**.
  A second logged-in character nearby should still **see your character** and watch it fight mobs
  (it keeps hunting; a "keeps hunting while away" line appears).
- [ ] **Log back in** → you re-attach to that same character with the loot/XP it gained while away
  (not a stale copy).
- [ ] Disconnecting **in a town** (safe zone), while **dead**, or with auto **off** does a normal
  logout (no offline farming).
- [ ] An offline farmer that **dies** (mobs out-damage its potions) stops and logs out ("stopped
  hunting"); on next login it's alive with auto **off** (must re-enable).
- [ ] Caps: idle **8h** online / offline **2h** (constants — verify via a temporary lower value if
  needed). Hitting the idle cap stops auto and **blocks re-enabling until you re-log**.
- [ ] Auto-hunt while offline still obeys the shared potion cooldown, buff-potion top-up, and skill
  conditions (same brain as online).

### ✅ Debug Tuning panel (2026-07-10) — VERIFIED 2026-07-15
Live rates/karma/caps editing, cap=0→unlimited, persists across restart, window size persists,
admin-gated — all confirmed.

### PvP + flag/karma/PK (2026-07-10) — PARTIALLY verified 2026-07-15
- [x] Top-right **PvP** / **Counter** toggle buttons — verified.
- [x] **PvP: On** → attack a player outside town turns you purple, they can hit back, damage lands — verified.
- [x] Attacking an **innocent** needs PvP On; purple/red attackable without it; hitting red doesn't flag — verified.
- [x] **No PvP in towns** — verified.
- [ ] **Kill an innocent** → red (PK) + karma; **kill a flagged/red** → PvP count, no karma. *(not tested)*
- [ ] **Dying** as a PK −200 karma (red clears at 0); **farming** as a PK −20/kill. *(not tested)*
- [ ] **Counter-attack** retaliation; **karma persists** across relog. *(not tested)*

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

### Party + AFK interaction (2026-07-08)  *(NOT verified this playtest)*
- [ ] You **can't invite** a player who is auto-hunting (idle) or offline-farming — you get "X is
  auto-hunting and can't be invited right now."
- [ ] A party member who turns auto-hunt **on** shows a yellow **• AFK** tag on their roster row;
  a member who goes **offline-farming** shows a grey **• OFFLINE** tag (hover for a tooltip). The
  tag clears when they turn auto off / reconnect.
- [ ] The party can **kick** an AFK/offline member normally.
- [ ] If the **party leader** goes offline-farming, leadership passes to an online member (★ moves).
- [ ] A party **invite** left unanswered **auto-expires after ~30s**: the invitee's prompt disappears
  and the inviter is told "X didn't respond," so they can re-invite (no permanent "considering
  another invite" block).
- [ ] An offline member that logs out (cap/death) **leaves the party**; one that reconnects **stays**
  in it (tag clears).

---

## To test now (party window + mob cast-bar UI — 2026-07-07)

### Party window (WPF client)  *(NOT verified this playtest)*
- [ ] Target another player → the target frame shows an **"Invite to Party"** button. Click it →
  they get a centered **accept/decline prompt**; you see a "Party invite sent" chat line.
- [ ] On accept, both of you show a **Party panel** (top-left, under the vitals/buff bar) listing
  every member with name/Lv/class + **live HP and MP bars**. Leader has a ★.
- [ ] Leader sees a small **✕ kick** button on other rows (not on self); a non-leader sees none.
  Kicking removes that member (their panel hides); the kicked player gets a chat notice.
- [ ] **Leave** button removes you; when a party drops below 2 it **disbands** (everyone's panel
  hides). The invite button is hidden for players already in your party, and for non-leaders.
- [ ] Roster HP/MP bars update as members take damage / heal (server refresh).

### Party loot rules (2026-07-07)  *(NOT verified this playtest)*
- [ ] A new party **defaults to Random** loot (settings-panel-configurable later).
- [ ] The party panel shows a **Loot** dropdown. Only the **leader** can change it (disabled for
  members).
- [ ] Leader changing the dropdown **starts a vote**: every other member gets an **Agree/Decline**
  prompt; the leader sees "waiting for the party to agree." It applies **only if ALL agree**
  ("Loot rule set to … (agreed by all)"); any **Decline cancels** it, and it **times out** (~30s).
  On cancel the leader's dropdown snaps back to the current rule.
- [ ] A party invite **prompt shows the inviter's name AND the loot rule** you'd join under.
- [ ] **Finders Keepers**: item drops go to whoever landed the kill (as before).
- [ ] **Random**: each item drop goes to a random in-range member; others see "X looted Y."
- [ ] **Round Robin**: consecutive drops rotate through in-range members in join order.
- [ ] **Leader Only**: all item drops go to the leader (if in range; else the killer).
- [ ] **Gold is ALWAYS split** evenly among in-range members regardless of the loot rule (killer
  keeps the odd remainder); solo = the killer gets it all.
- [ ] Boss/elite crafting-mat pile goes to a single recipient per the loot rule.
- [ ] Only members **in share range** (ViewRange) and alive are eligible; out-of-range members are
  skipped (loot falls back toward the killer where applicable).

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
