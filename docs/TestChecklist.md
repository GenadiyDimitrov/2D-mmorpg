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

### PvP heal rules (NOT tested this playtest — trade/PvP still to verify)
- [ ] **You can no longer heal the enemy you're fighting.** Targeting a hostile player and casting a
  heal/buff/cleanse now **self-casts** instead of healing him. (It used to accept ANY player as a
  support target — which is why healing mid-duel healed your opponent.)
- [ ] A support skill can only ever reach **yourself or a party member**. Verify: heal a party mate
  at range ✔; try to heal a non-party player → it heals you instead.
- [ ] **Supporting an outlaw makes you one.** Heal / restore-MP / cleanse a party member who is
  **flagged (purple)** or a **PK (red)** → **you go purple too**. A clean healer can no longer prop
  up a PK from behind with no risk.
- [ ] Self-healing never flags you. An already-red healer stays red (karma outranks the flag).

---

## To test now (BUFF ROWS / SET TOOLTIP / TRAINING OUTPOST — 2026-07-13)

### Buff bar — 4 rows by subtype
- [ ] The buff bar is now **four rows**, each hiding itself when empty, tinted to read apart:
  **buffs** (blue), **debuffs** (red), **item effects** (bronze), **consumables** (green).
- [ ] Drink a **healing or buff potion** → it appears in the **consumables** row (green), not mixed
  in with your buffer's buffs.
- [ ] A buffer's buffs stay in row 1; a mob's slow/bleed lands in the **debuff** row.
- [ ] The **item row is empty for now** — armor sets and weapon specials are still StatMods, not
  buffs. That row appears the moment they become skills (owner said row 3 can stay invisible).
- [ ] Double-click still drops a (beneficial) buff early.

### Item tooltip — set bonus
- [ ] Hovering a **set piece** now shows the set section: the set name, **Bonus:** (what it gives),
  **Items 3/4**, and a line per piece — **green ✔ = worn**, **grey ✖ = missing**.
- [ ] The Bonus line + count go **green only when the set is complete**, grey otherwise.
- [ ] Accessories are shared across a tier's bodies, so hovering a helm shows it against whichever
  body you're actually wearing.
### Set SHIELD bonus (2026-07-13 — I had this wrong first time)
- [ ] Each tier's **shield now belongs to that tier's HEAVY set** (the CSV puts shields in the same
  GroupId). It is **not** required to complete the set — wearing it adds an **extra** bonus on top.
- [ ] The tooltip now shows a **Shield Bonus:** line + the shield piece (green ✔ worn / grey ✖ not).
  It only goes green when the **4-piece set is complete AND its shield is equipped**.
- [ ] The shield extras, straight from the CSV (def-oriented heavy line only — the `_dmg` variants
  get none):
  - **Heavy 20** → +10% Shield Def
  - **Heavy 40** → +5% P.Def
  - **Heavy 52** → +25% Shield Def
  - **Heavy 61** → **reflect 5%** of melee basic-attack damage back at the attacker
  - **Heavy 76** → +5% P.Def, +5% M.Def, +25% Shield Def, reflect 5%
- [ ] Wear a full Heavy 61 set + its shield and let something melee you → you should see the
  attacker take **reflect** damage (bows are excluded; capped at 50%).
- [ ] Equipping a shield from the WRONG tier gives no extra (its SetId won't match).

### Training Outpost (safe zone by the dummies)
- [ ] A small **safe zone "Training Outpost"** (24000, 5000, radius 400) sits just SOUTH of the
  training dummies (they're at y=4000). It's deliberately clear of them — a safe zone keeps mobs
  out, and the dummies ARE mobs.
- [ ] Inside it: **Gatekeeper Vess** at the north edge and **Spirit Helper Ilva** (buffer) at the
  south, offset so their labels don't overlap.
- [ ] Buff up at Ilva → walk ~800 north to the dummies → test → teleport out with Vess.
- [ ] The outpost is now also a **teleport destination from every other gatekeeper**.

---

## To test now (EVERYTHING IS A SKILL — potions/scrolls — 2026-07-13)

### Typing in text boxes (the real cause of "can't write in the auto-potion boxes")
- [ ] You can now **type into every text box**: auto-hunt HP%/MP%, farm range, per-skill reuse,
  debug tuning. The key handler only excused the CHAT box, so typing anywhere else fired the game
  hotkeys and ate the keystroke — "5" cast skill 5, "i" opened the inventory, and the digit never
  landed. That's why it "used skills" instead of writing.

### Consumables now cast skills
- [ ] **Healing potions are skills.** Minor/Healing are heal-over-time BUFFS (1%/s and 2%/s for 15s)
  — they now appear on the **buff bar** instead of a bespoke potion channel. Greater is an instant
  50% heal.
- [ ] Drinking a **Greater potion over a Minor HoT** replaces it (BuffKey + Rank does the
  "stronger cancels weaker" that the old hand-rolled potion-rarity state did).
- [ ] The **shared 30s drink cooldown** across healing potions still works; **buff potions still
  ignore it**.
- [ ] **Auto-potion (auto-hunt) still drinks the best HP potion** below your threshold, and the
  "keep buff potions active" top-up still works.
- [ ] Potion **tooltips** now read from the skill's description.

### Return scrolls are no longer learned skills
- [ ] **Scroll of Return / Ultimate Scroll of Return no longer appear in your skill list.** They were
  auto-granted as learned skills; now the ITEM grants the skill. Double-click the scroll → it still
  channels and teleports, and is still consumed (refunded if interrupted).
- [ ] The plain **Return** skill (30s cast, 5min cd) IS still a learned skill everyone has — unchanged.

---

## To test now (DEBUG GEAR PICKER — 2026-07-13)

- [ ] Debug → **Equip** tab is now a drill-down: **Armor & Shields / Weapons / Jewels** → pick a
  **level (20 / 40 / 52 / 61 / 76)** → click any individual piece to receive it.
- [ ] The **level 20-40 sets now exist in the menu** — they were never exposed before (the old tab
  only had a few hardcoded E-grade "rare" items and the named sets, none of which were the tiered
  gear). That's why they looked missing.
- [ ] Armor levels also offer **★ Full Heavy / Light / Robe Set** (body + helm + gloves + boots) in
  one click, since a set bonus needs all four. Individual pieces are listed underneath.
- [ ] Weapons list all 8 families per level (sword 1H/2H, blunt 1H/2H, duals, bow, wand, staff) —
  use these to feel the new **weapon channel factors** (a staff should melee poorly, a sword should
  cast poorly).
- [ ] The lists are read from `ItemCatalog` (not hardcoded), so **new gear added to the CSV appears
  here automatically**.
- [ ] The old **Rare Weapons (E) / Rare Armor Sets / Named Sets (Dark Dominion)** blocks are gone,
  as requested. Boxes + Legendary (God's Judgment/Robes) are kept.

---

## ✅ SKILL BAR (2026-07-13) — SUPERSEDED by "SKILL BAR → DB" above (verified 2026-07-15)

## ✅ DAMAGE RETUNE (2026-07-13) — SUPERSEDED by the MAGIC RE-SCALE at the top (signed off 2026-07-14)

The 07-13 MagicK 8→91 / archetype-multiplier removal / cast-speed rebase / weapon channel split were all
rolled into and re-tuned by the 2026-07-14 magic re-scale, which the owner signed off in play. Nothing to
re-test here separately. The `LevelStatBonus` removal and stats-no-longer-grow rules are verified via the
stat-swap testing above.

---

## To test now (disconnect / exit / combat + Return — 2026-07-09)

### Return skill + scrolls
- [ ] Everyone has a **Return** skill (30s cast, 5min cd): channel it → teleport to the nearest town.
  Taking **any** damage cancels it. Cast speed / cooldown buffs do NOT change its 30s/5min.
- [ ] Buy a **Scroll of Return** from the Apothecary (500g); double-click it → 10s cast → teleport.
  It's consumed on success, refunded if interrupted. Sells for 0.
- [ ] (Debug-give) **Ultimate Scroll of Return** → ~instant cast return. Not sold/dropped.

### Combat state + exit
- [ ] **Exit Game** (Settings) works out of combat (app closes). During combat (dealt/took damage in
  the last 30s) it's **blocked** with a message; 30s after the last hit you can exit.

### Disconnect fates (use 2+ clients)
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

### Auto-Hunt window (WPF client)
- [ ] Enter world → an **"Auto-Hunt"** button appears top-right; it opens the config window.
- [ ] The window lists your **active skills** (passives hidden) with an enable checkbox, a type tag
  (attack/buff/debuff/heal) and a **reuse (s)** box prefilled with each skill's own cooldown.
- [ ] HP%/MP% potion boxes + "Keep buff potions active" checkbox. **Apply** saves; settings **survive
  relog** (persisted).

### Behavior
- [ ] Toggle **Enabled** (checkbox) → you auto-walk to the nearest mob, basic-attack, and cast your
  enabled auto-skills; killing one retargets the next. Turn it off → you stop acquiring new targets.
- [ ] **Attack** skills fire on cooldown; a **buff** skill only casts when its buff is missing on you;
  a **debuff** only when the target doesn't already have it; a self-**heal** only below 70% HP.
- [ ] Raising a skill's **reuse (s)** above its default makes it fire slower (never faster than default).
- [ ] **Auto-potions** work even with auto-hunt OFF: drop below the HP% and the best HP potion is drunk
  (shared potion cooldown respected). (No MP potions exist yet — MP% is a no-op.)
- [ ] "Keep buff potions active" re-drinks any buff potion in your bag whose buff has expired.
- [ ] The window footer shows **Mana: X /s** (sum of enabled auto-skills, after any MP-cost/CD buffs)
  and updates as buffs go up/down.
- [ ] Loot/XP/gold behave exactly like manual play (incl. party loot rules) — no idle penalty.

### Offline farming (Phase 2, 2026-07-08)
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

### Debug Tuning panel (2026-07-10) — live tuning while you play
- [ ] Settings → **Debug Tuning (admin)** opens a panel of live values (rates, karma, caps).
- [ ] Change **Exp/Drop rates** + Apply → next kills/drops use the new rate immediately.
- [ ] Change **karma** values (base / ×consec / ×level / −death / −mob) + Apply → next PK/death/mob-kill
  uses them. Change **idle/offline/grace** caps (seconds) + Apply → observe a cap/grace fire quickly.
- [ ] **Idle/offline cap = 0 → unlimited** (leave someone farming/levelling to gauge speed).
- [ ] Debug values **persist across server restarts** (`debug-config.json` in the server folder).
- [ ] **Window size persists** — resize the client, close, reopen → same size (`%LocalAppData%\L2Clone`).
- [ ] Config files are gitignored + not in the build/Debug output.
- [ ] Non-admins: the panel does nothing (server refuses).

### PvP + flag/karma/PK (2026-07-10) — ⚠ delete game.db first (new karma columns)
- [ ] Top-right **PvP** and **Counter** toggle buttons (tint when on).
- [ ] With **PvP: On**, attack another player **outside a town** → you turn **purple** (name), the
  target can hit back; skills/basics land and show damage. (Damage-check the migrated crit/eva + skills.)
- [ ] Attacking an **innocent (white)** needs PvP On; a **purple/red** player is attackable without it
  (retaliation / hunting a PK) and attacking a **red** player does NOT flag you.
- [ ] **Kill an innocent** → you go **red (PK)**, gain **karma** (200 base; more per consecutive kill and
  the more levels above the victim); status line shows KARMA.
- [ ] **Kill a flagged/red** player → **PvP count** up, no karma.
- [ ] **Dying** as a PK lowers your karma by 200; at 0 the **red name clears**.
- [ ] **Farming as a PK** sheds karma: each mob kill is −20 (take a camper's spot, grind it clean).
  (All karma values are tunable consts: base/consec/level growth, per-death, per-mob.)
- [ ] **No PvP in towns** (safe zones block it; 0 damage if someone runs to town mid-fight).
- [ ] **Counter-attack**: an auto-hunting/offline char with **Counter: On** retaliates against a player
  attacker (finishes a near-dead mob first, else switches).
- [ ] Karma persists across relog (a PK is still red after logging back in).

### Stats-via-skills identity migration (2026-07-10) — verify no regressions
- [ ] Rogue still has its crit/evasion identity (now from the **Evasion Mastery** passive: +20% crit,
  +20 eva); archer from **Reflexes** (+15% crit, +10 eva). Numbers should feel unchanged (parity).
- [ ] **Intentional change:** the **tank** no longer gets the old +level/2 magic defence (his Anti-Magic
  passive is his magic identity now) — confirm tank magic survivability still feels right.
- [ ] **Intentional change:** a base **rogue's basic attacks no longer interrupt casts** (that "cancel"
  becomes a 3rd-class discipline passive later) — confirm that's the intended feel.

### Roaming + target filters (2026-07-10)
- [ ] Auto-Hunt window now has a **Farm range** box, a **Static spot** checkbox, **Mobs/Elites/Bosses**
  checkboxes, and a **Basic Attack** row atop the skill list.
- [ ] **Roaming** (Static off): with auto on and no mob nearby, the character **wanders** within the
  farm range and engages mobs it finds; the search circle follows you.
- [ ] **Static spot** (on): it only engages mobs within the circle centered where you turned auto on;
  when none are left it **walks back to the center**. It may chase a fleeing mob slightly outside.
- [ ] **Rank filter**: with only **Mobs** checked it ignores elites/bosses; tick Elites/Bosses to include them.
- [ ] **Basic Attack** row: fighters tick it → they melee when no skill is ready. A **mage unticks it**
  → it only casts skills and never melees (walks into skill range instead).
- [ ] Settings survive relog (persisted).

### Party + AFK interaction (2026-07-08)
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

### Party window (WPF client)
- [ ] Target another player → the target frame shows an **"Invite to Party"** button. Click it →
  they get a centered **accept/decline prompt**; you see a "Party invite sent" chat line.
- [ ] On accept, both of you show a **Party panel** (top-left, under the vitals/buff bar) listing
  every member with name/Lv/class + **live HP and MP bars**. Leader has a ★.
- [ ] Leader sees a small **✕ kick** button on other rows (not on self); a non-leader sees none.
  Kicking removes that member (their panel hides); the kicked player gets a chat notice.
- [ ] **Leave** button removes you; when a party drops below 2 it **disbands** (everyone's panel
  hides). The invite button is hidden for players already in your party, and for non-leaders.
- [ ] Roster HP/MP bars update as members take damage / heal (server refresh).

### Party loot rules (2026-07-07)
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

### Mob / boss cast-bar
- [ ] When a mob/boss begins a visible cast (e.g. the boss **"Devastating Slam"**), an orange
  **cast-bar appears under its nameplate** and fills over the cast time, then disappears.
- [ ] Interrupting / killing the caster (or the cast finishing) clears the bar cleanly.

### Boss unique skills + phases + adds (2026-07-07)
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

## To test now (ranged + caster mobs — 2026-07-03)

### Archer mobs (orc_archer L16, dune_orc_archer L40, fen_lizardman_archer L39, dread_archer L69)
- [ ] They shoot from ~450 range (don't run into melee), hit noticeably harder (×2 P.Atk), and
  are squishier (light armor: lower P.Def, a bit more evasion). Bow attacks apply bow variance.

### Mage mobs (watcher_eye L26, aether_wisp L58, rift_portling L40, radiant_mage L82)
- [ ] NO basic attacks — they only cast. Long nuke from ~600 (4s cast), short jab up close (~150,
  1.5s). Damage scales with mob level (nuke pow 18→129, jab 7→33).
- [ ] Higher M.Atk, lower P.Atk/P.Def than a same-level melee mob.
- [ ] They burn MP per cast; when MP runs out they stand HELPLESS (no attacks) — a free kill if you
  outlast their mana. (Mob cast-bar now renders under the nameplate — see the 2026-07-07 section.)
- [ ] rift_portling = a beefy caster (champion HP) that nukes; watcher_eye also has high M.Def.

### Golem-type resist (obsidian_knight L63, Duskvale)
- [ ] Sword/dual hits land for less (Pierce ×1.43 P.Def), arrows much less (Bow ×2), blunt MORE
  (×0.5). Inspect shows the resist lines.

---

## To test now (mob overhaul — 2026-07-02)

### Mob base-stat curve (docs/mobs/mob_base_stats.csv) — BIG BALANCE SHIFT
- [ ] Mobs now use the CSV level curve → ~2-3× their old HP/def/atk. Fights should feel
  meaningfully longer/harder. Inspect a mob (▼ on the target frame) and sanity-check its
  HP/P.Def/M.Def/P.Atk vs the CSV row for its level (should match at authored levels).
- [ ] **Cleric can still SOLO a same-level mob** (target: ~L30) — slower but possible.
- [ ] Low-level mobs don't ~one-shot players (physical mob damage sane at 2-3× atk).

### Weapon-type resistance (P.Def route)
- [ ] `obsidian_knight` (Lv 63, Duskvale): sword & bow hits land for noticeably LESS, a
  blunt weapon for MORE (vs its normal P.Def). Inspect shows "Sword/Dual Resist / Bow Resist
  / Blunt Weak" lines.
- [ ] `watcher_eye` (Lv 26) is hard for mages (high M.Def) / easy for fighters; `rift_portling`
  (Lv 40 champion) has ~3.5× the normal L40 HP.

### New 80-mob roster + zones + drops
- [ ] Every field zone spawns the new named mobs at their natural level (levels roughly match
  each zone's band). L80-85 mobs appear in Frostmere (9000,17600).
- [ ] Drops still flow (potions/gear/scrolls by level; gear TYPE by family — undead/caster→robe,
  animal→light, insect→daggers, demon/dragon→heavy, humanoid→sword).
- [ ] Class-change hunt quests (orc_archer/skeleton_grunt/shield_skeleton, Lv 16-21) and the
  3rd-class chain (medusa/marsh_mantis_soldier/fen_lizardman_archer, Lv 34-39) count kills.
- [ ] Boss = Valley Treant (Lv 60, south), Elite = Emberwyrm Drake (Lv ~78, NW) spawn & fight.

---

## To test now (this session — 2026-06-29)

### Mage no auto-attack after a spell
- [ ] After casting an OFFENSIVE spell on a mob, a mage (Nuker/Healer) no longer runs at
  the target to melee — it stays put. Fighters still flow skill → auto-attack as before.

### Physical skills scale by ATTACK speed (not cast speed) — NUMBERS UNTUNED
- [ ] A fighter's physical skill cast time now follows ATTACK speed (DEX + weapon), not the
  WIT-driven cast speed — so a fighter no longer casts melee skills sluggishly.
- [ ] Faster attack speed (buffs / fast weapon) shortens physical-skill cast; a slow heavy
  2H weapon lengthens it slightly. Magic / buff / heal skills still use cast speed.
- [ ] NEXT CSV: owner gives fighters a real `CastTicks` per physical skill so this can be
  felt against the actual attack speed (heavy strikes ~1s, lighter ~0.1–0.2s).

---

## Playtest 1 results (2026-06-28)

**Verified working:** damage & crits (incl. [Double]) at all levels; control lands (slow/
root/stun/fear); DoT + burst; defensive skills + Provoke/threat; movement (blink/knockback);
weapon masteries; mage damage feels OK for now.

**Fixed this round (RE-TEST next launch):**
- [ ] **Restore Mana** now costs ~1.2× what it restores (72 MP → 60) and CANNOT target self
  or another mana-restorer (healer→non-healer only).
- [ ] **Phase Shift** no longer needs a target — blinks ~400 away from the nearest enemy.
- [ ] **Cast bar** shows the class skill name (e.g. "Moonlight Bolt"), not the base form.
- [ ] **Debug** menu: "Level +10" and "Learn all skills (to my level)" buttons.

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

### Training Grounds (test dummies)
- [ ] A cluster of immortal **Training Dummy (Lv 20/40/60/80)** spawns at ~(22500–25500, 4000) — reach via debug Teleport → Zones.
- [ ] Dummies never move, never attack, and never die — but they DO take (and display) damage; HP drops then regens (~1M HP, ~10k/s regen, floored at 1).
- [ ] Use them to verify [Double] crits, DoT ticks/stacks (Effects line in the target window), slow/stun/etc. land, and damage scaling.

### Movement: blink + knockback — NUMBERS UNTUNED
- [ ] Phantom "Shadowstep" @40: teleports you behind the target, then hits ([Double]).
- [ ] Trapper "Repelling Shot" @40: damages and shoves the target ~200 away.
- [ ] Tempest "Phase Shift" @48: blinks you ~400 away from the target (escape).
- [ ] Blink/knockback respect world bounds; the moved entity stops its current path (doesn't slide).

### Taunt + real threat/aggro — NUMBERS UNTUNED
- [ ] A mob now targets the highest-THREAT attacker (threat = damage dealt), not just the last hitter — e.g. a high-damage player pulls aggro off a low-damage one.
- [ ] Tank "Provoke" @40 forces the mob onto the tank (its target switches to you) and holds ~3s even if others out-damage you.
- [ ] Detaunt (e.g. rogue Shadowstep/BattleFury detaunt) sheds ~90% of your threat → the mob retargets to the next-highest, or leashes home if no one else.
- [ ] Mob still leashes/resets correctly (threat clears on reset).

### Combat primitives P2: poison & venom (Venomweaver per-race trio) — NUMBERS UNTUNED
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

### Expandable target window (commit ccb5805)
- [ ] Targeting a mob shows a `▼` expand button on the target frame; plain NPCs (vendor/gatekeeper) show no button.
- [ ] Clicking `▼` opens the panel and shows HP/MP, P/M.Atk, P/M.Def, Acc/Eva/Crit.
- [ ] A mob's passive lines appear (e.g. Green Slime → "Magic Monster", "M.Def +100%", "P.Def −50%"; Stone Golem → "Armored Brute").
- [ ] Bow/Crit resist lines show only when non-zero, and are NOT duplicated.
- [ ] Panel refreshes ~once/sec during a fight (HP/MP track damage).
- [ ] `▲` collapses it; switching targets re-queries; clearing target (Esc/✕) hides it.

### Weapon masteries — fighters (commit a574309) — NUMBERS UNTUNED
- [ ] Learnable @20 (500 SP) in the skills window for Tank/Warrior/Rogue/Archer.
- [ ] Bonus applies ONLY while the matching weapon is held; no penalty for a "wrong" weapon.
- [ ] Warrior "Two-Hand Mastery": sword +15% pAtk/+3% crit; blunt +12% pAtk/+10 acc.
- [ ] Rogue "Dual Mastery": dual +10% pAtk/+5% crit/+15% crit dmg.
- [ ] Archer "Bow Mastery": bow +12% pAtk/+20% crit dmg/+5 acc.
- [ ] Tank "Weapon Expertise": sword/blunt +6% pAtk/+5–10 acc.
- [ ] Stat window reflects the change when you swap weapons.
- [ ] **1H/2H gating**: Warrior bonus applies ONLY with a 2H sword/blunt (not the 1H sword); Tank ONLY with a 1H sword/blunt (not the 2H greatsword). Dual/bow unaffected (always 2H).
- [~] Tune the percentages once the feel is clear.

### Mage masteries (commit 361127f)
- [ ] Nukers learn **Spell Mastery** (same as healers, @20/25/30/35); it replaces base Weapon Mastery (no double-apply).
- [ ] Caster **bow penalty**: a mage holding a bow casts at ~half speed (cast bar ~2× longer). Inert with staff/other weapons.
- [ ] No "Staff Mastery"/"Mace Mastery" anywhere (removed).

### Class-change dialog blurbs (commits f754f11, f03b23e)
- [ ] 2nd-class NPC shows the archetype blurb under each class option.
- [ ] 3rd-class (grandmaster) NPC shows the discipline blurb per option.

### Combat primitives P1: Root + physical Slow + skill-damage% — NUMBERS UNTUNED
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

### Toggle skills + Healer "Combat Stance" (NEW — toggle mechanic) — NUMBERS UNTUNED
- [ ] Cleric learns "Combat Stance" @20; clicking it activates (costs 20 MP), clicking again deactivates (free).
- [ ] Active stance: P.Atk +50%, M.Atk −50% in the stats window; melee hits harder, heals/Holy Bolt weaker.
- [ ] The stance shows on the buff bar with `⟳` (no countdown); double-clicking it also turns it off.
- [ ] Stance does NOT expire over time; it clears on death/relog (runtime-only).
- [ ] No MP drain while held (activation cost only — by design for now).
- [~] Tune the ±50% swap once melee-cleric farming is tested.

---

## Tuning targets (owner-stated)

- [ ] **Cleric can solo a same-level (~30) mob** — slower than a fighter, but possible (not impossible, not two-shot).
- [ ] Low-level physical mobs do NOT ~one-shot players (magic-vs-physical mob parity).
- [ ] Mage TTK ~60s @75 is acceptable pre-CC — do NOT over-buff mage damage.
- [ ] Healer numbers: heals, Force (interrupt resist), Frenzy.
- [ ] Armor-mastery numbers per archetype (bonuses + untrained penalties).
- [ ] Mob passive modifiers (Magic Monster / Armored Brute) feel right vs mage/fighter.
- [ ] NPC newbie buffer set (Might/Force/Focus/Speed/Body/Frenzy) applies and shows stats.

---

## Carryover from prior sessions (verify still good)

- [ ] Buff/effect layer: Might applies def/atk; Speed applies cast speed; Force applies M.Atk @rank2.
- [ ] Buff bar: double-click/✕ drops a buff and stats update.
- [ ] Economy: merchants reject untradeable newbie items; boxes (random + selection) open and grant loot.
- [ ] Jewels: 2 rings / 2 earrings / 1 necklace caps enforced; jewel attributes roll.
- [ ] Debug teleport tab: NPCs / Zones / Cities; zones drop you ~400 outside the spawn ring.
- [ ] Enchant/reroll popup matches the inventory (no ±1 desync).
- [ ] Per-race Holy Bolt name (Human Holy / Elf Moonlight / Ork Spirit Bolt).
