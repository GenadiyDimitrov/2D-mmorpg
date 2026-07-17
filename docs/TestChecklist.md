# Test Checklist — L2Clone (branch Gena)

Running list of things to verify in-game. Claude keeps this updated as features land;
the owner tests manually and ticks items off. **`[ ]` = not tested, `[x]` = verified,
`[~]` = tested, needs a change/tuning.** Newest features first. When asked to test, Claude shows
this file.

---

## 🧪 DEATH XP PENALTY + RESURRECTION (built 2026-07-17) — no schema change (game.db from the 2026-07-16 batch is fine)

Built + `dotnet build` 0/0. Death XP penalty was committed 2026-07-16; resurrection is the new part below.

**Death XP penalty (2026-07-16):**
- [ ] **Die at level 40+ → lose 5% of the level's exp** (a system line says how much). You **sit at the
      start of the level — never delevel**. PvP death loses exp too.
- [ ] **Below level 40 → no loss** (newbie protection). Character window shows a **"Novice's Grace"** passive
      (display-only, no stats) up to level 40, then it disappears.

**Resurrection (2026-07-17):**
- [ ] **Cleric resurrection skill.** All clerics (Healer 2nd class) auto-learn **Resurrection** — L1 @20,
      L2 @40; **Healers (Lightbringer)** continue to L3 @52, L4 @61. Restores **25 / 50 / 75 / 100%** of the
      target's lost exp and revives at **30% HP/MP**. 10s (fixed) cast.
- [ ] **Target a fallen ally, then cast** — select the dead party member in the **party window** (click their
      roster row) and cast Resurrection. (World-corpse Shift-click is a later add; use the party window.)
- [ ] **Confirmation prompt.** The dead player gets a **"X offers to resurrect you — restores N% exp"** popup
      with **Resurrect / Decline** — so you don't stand up on top of the mob. Decline → stay dead. Accept →
      revive at 30% HP/MP + exp restored. Prompt auto-expires after ~30s; a town-respawn cancels it.
- [ ] **Resurrection scrolls** (used on a dead ally, NOT self). **Scroll of Resurrection** (Apothecary, 1500g)
      = 10s cast, 0% exp restored. **Ultimate Scroll of Resurrection** (not vendor-stocked; debug-give
      `scroll_resurrect_ultimate`) = ~0.5s cast, 100% restored. Select the dead ally, use the scroll → same
      confirm prompt. Both have a 1-min reuse.

---

## 🧪 BATCH TO TEST (built 2026-07-16) — ⚠ restart client+server + **DELETE game.db** (new `DiedWhileAway` column)

Built + `dotnet build` 0/0 + **SmokeTest green** (main→3rd class→cross-race subclass add, gate, relog). Not
yet play-tested. (Note: under `dotnet run` the db is `Game.Server/game.db`; via VS/F5 it's
`Game.Server/bin/Debug/net8.0/game.db` — delete whichever your run uses.)

- [ ] **Subclass "Add a class" fixes.** (a) The **main class is now filtered** — a Human Bulwark is no
      longer offered Bulwark (picker folds in the active class's discipline; the server also re-sends the
      class list after a debug profession change so it can't go stale). (b) **Gate changed: level 76 → 75,
      and EVERY class you own must be level 75+ AND hold its 3rd class** before you can add another (admins
      exempt). Below that → refused with a message. Picker header states the rule.
- [ ] **Karma per-kill curve (quadratic).** gap = killer−victim: ≤+10 → 200; then skyrockets
      (+20≈1.1k, +30≈3.9k, +40≈8.5k); **+50 and beyond → 15k cap**. A lvl-82 on a lvl-1 now hits the cap
      (was ~3k). *(Debug "karmaLevel" tuning field is now inert — the curve is fixed; flag if you want it removed.)*
- [ ] **Grade penalty + equip unlock.** You can now **equip any grade at any level** (the level gate is
      gone). Above-grade gear has its **weapon ATK / armor DEF scaled down** until you reach the grade's
      level: F=1, E=20(×0.5), B=40(×0.4), A=52(×0.3), S=61(×0.2). Equip A-grade at lvl1 → check the stat is
      reduced; level past 52 → full power. Tooltip now says "full power at Lv X", not "requires".
- [ ] **Party window: click a member to target them.** Clicking a roster row targets that ally (targets
      only, never attacks) — a healer can now select + heal a party member from the window.
- [ ] **Offline-farm death sticks (anti-exploit).** Die while offline-farming (or during the link-dead
      grace) → you log back in **DEAD** (res prompt), not full HP. Stays dead across relogs until you
      actually respawn. Normal (alive) logout still logs in healed.

**Subclass rework (Debug → Class tab):** — owner tested 2026-07-16
- [~] **Add a class = pick a discipline.** ✅ **ALL BUILT 2026-07-16 (test via the top batch).** BUGS + rule changes found:
      - **Main class isn't filtered.** test1 (Human Bulwark) is still offered Human Bulwark in the picker.
        ROOT CAUSE: the picker's `owned` set is built from `_subclasses` only, which goes STALE after a
        debug profession change (that path re-sends Stats+Learned but not the class list, so the main still
        looks like a base Fighter with ThirdClass=0). `_myThirdClass` updates but the picker ignores it.
        → **FIX: include the active class's discipline (`_myThirdClass`) in the filter**, and/or re-send the
        class list from `HandleDebugThirdClass`. (Server `CanAddDiscipline` already counts the main.)
      - **Adding Vanguard also removed Bulwark** from the list — same stale-`_subclasses` cause: the add
        finally pushes a fresh class list so the main's Bulwark belatedly drops out. Fixed by the above.
      - **Requirement change: level 76 → 75, AND require a 3rd class.** No 4th tier exists, so gate on
        **level 75 + has a 3rd class** (don't force subs toward a 4th class). `SubclassLevel 76 → 75`.
      - **Adding a new class requires ALL owned classes to be level 75+** (they already carry a 3rd prof).
      - The new class still starts at **level 1**, own race, 3rd class pre-approved.
- [x] **Discipline is unique, cross-race.** VERIFIED.
- [~] **All gear unequips on add** — ✅ **grade penalty BUILT 2026-07-16 (top batch).** Works, BUT it only fires on ADD. On a **class CHANGE/swap** to a
      level-1 class you keep your A-grade gear equipped (owner: that's fine) — so we now **need the GRADE
      PENALTY** (low level in high-grade gear → gear combat stats scaled down). Previously deferred; owner
      wants it now. See the deferred spec in docs/Roadmap.md.
- [x] **Count cap** (4 normal / admin unlimited). VERIFIED.
- [x] **Swap + relog persistence.** VERIFIED.

**Karma / PK / trade:** — owner tested 2026-07-16
- [~] **Karma per-kill cap — ✅ QUADRATIC CURVE BUILT 2026-07-16 (top batch).** A level-82 killing a level-1 (+81
      gap) got only **~3k** karma; a huge gap should hit the cap, not undershoot. Owner's intent — karma per
      kill scales with the **level gap (killer − victim)** and CAPS at a **+50** gap:
      - ≤ +10 gap: ~**200** (baseline).
      - then it **skyrockets**: +11 → **400**, +12 → **600**, … accelerating up to **+50 → 15,000** (the cap).
      - beyond +50 stays at the 15k cap.
      So the 15k cap is reached at a +50 gap (killing far-below-level in a low-level zone), and the current
      formula that *undershoots* on big gaps must be replaced. Exact curve = design decision (anchors above).
- [x] **4 karma debug buttons** (+1000 / −1000 / +20 / −20; at 0 the red name + streak clear). VERIFIED.
- [x] **Trading blocked while PK or flagged** (either party); innocent-to-innocent works. VERIFIED.
- [x] **A PK can't BUY from a vendor**; purple can; selling works for everyone. VERIFIED.
- [x] **Trade window contrast** — partner offer + your-inventory panels readable. VERIFIED.

**Debug menu reorg:**
- [ ] **Functions tab** is grouped top-to-bottom: **Full buffer · Gold & SP · Level · Karma**.
- [ ] **Class tab** is grouped: **Profession & skills** (class change + give all skills) · **Classes
      (subclass)** (swap + add) · **Reset Character**.

**New findings (2026-07-16 playtest):**
- [x] ✅ **Archer "244k M.Atk" — RESOLVED 2026-07-16, NOT a bug.** The char was level **821** (debug over-
      level), not 82. Magic uses `levelMod²`, physical `levelMod¹`; at 821 that's 82.8× vs 9.1×, so the M.Atk
      *stat* balloons. MEASURED in BalanceMatrix (new extreme-level + damage probes): at 821 a MAGE has
      **366k** M.Atk (3× the archer's 112k) — so it's the shared level scaling, not archer-specific. And the
      actual DAMAGE stays balanced at every level (mage nuke 74 / fighter basic 49 / archer basic 104 vs a
      same-level tank) because magic damage takes `√mAtk / mDef` — the giant stat compresses. If anything,
      magic *falls off* at extreme levels (mDef outgrows √mAtk), it doesn't skyrocket. **No fix needed** at
      the real cap (90). If the cap rises to 100-200, re-run the BalanceMatrix damage probe to confirm.

---

## 🧪 M.ATK DISPLAY SHRINK (built 2026-07-16, damage-model work — NOT committed) — see docs/DamageModel.md

Combat is UNCHANGED (internal M.Atk + √ formulas untouched, so damage + heals are byte-identical and mob
casters unaffected). This is display-only + honest magic buffs.
- [ ] **M.Atk in the stats window is now P.Atk-size** (~1,087 @L85 instead of 2,954). A new row **"M.Atk
      (internal / L2-ref)"** shows the old cosmic value for reference. Target-frame M.Atk also shrunk.
- [ ] **Unbuffed magic damage + heals are identical** to before (verified in BalanceMatrix: mage 533 vs
      tank / 2249 vs mob unchanged).
- [ ] **Magic-only M.Atk buffs are now HONEST** — a `BuffMagAtk` authored at +X% gives +X% damage AND +X%
      on the displayed M.Atk (squared internally to cancel the √). ⚠ **They now grant their FULL authored %**,
      so existing magic-only buffs OVER-perform until re-authored to their effective (halved-ish) value —
      **owner's re-authoring TODO.** Shared attack buffs (BuffAtk) are unchanged (√-dampened, ~half on magic).

---

## ✅ VERIFIED 2026-07-15 (afternoon batch — owner tested)

P.Atk L2 formula (bare-hands feeble, armed preserved), NPC buffer 3 paid options, gold tradable +
colour-tiered in the inventory, popups remember position across a client restart, stat-swap + training
passives require level 40 + 3rd class, subclass count limit (4 for a normal account), and PvP/PK/karma
shown in the character window — all confirmed. (The −int.max overflow the karma readout exposed is fixed
by the evening batch's karma cap.)

---

## ✅ 2026-07-15 PLAYTEST — RESULTS (verified) + NEW FEATURE QUEUE

Owner tested the 07-13 and 07-14 features — all **VERIFIED WORKING**: subclasses · level cap + delevel +
debug buffs · skill bar → DB · skill-bar readability + debug · stat-swap direction rule · skill-reset NPC ·
movable popups (great) · equipped-items pane · HealK=15 · OffChannelFactor stays 0.6.

**Changes found while testing:** mage-click reverted (all classes click-to-attack), skill cast cancels the
auto-attack walk, set info only on the BODY armor, stat-swap groups gated by class (fighter CON/DEX/ATK;
mage CON↔DEX + ATK/WIT/MEN), and stat-swap + training passives require level 40 + 3rd class — all BUILT +
VERIFIED. The class-uniqueness → discipline-only + count-cap rework was also built (evening) and is under
test in the "BATCH TO TEST" section at the top.

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

### ✅ Class uniqueness — RESOLVED 2026-07-16 (discipline-only + count cap shipped)

The archetype bar was replaced by a discipline-only bar (own 4 mages = 2 clerics + 2 nukers, just no two of
the same discipline), barred options greyed + server-refused, and subclass COUNT is now capped (4 normal /
admin unlimited) — all confirmed. Player-facing rules still not built: safe-zone-only swapping, 5-min swap delay.

---

## ✅ MOVABLE POPUPS + MAGE-CLICK (2026-07-14) — VERIFIED 2026-07-16

Every popup drags / closes / raises (owner: "very good to rearrange so they don't get in the way"), popup
positions persist to the settings file (saved on close, defaulting when untouched, like L2), the mage-click
change is reverted (all classes click-to-attack so a mage out of MP can melee a mob), and casting a skill
cancels the auto-attack walk outright (no longer keeps walking after the cast) — all confirmed.

---

## ✅ LEVEL CAP + DELEVEL + DEBUG BUFFS (2026-07-14) — VERIFIED 2026-07-15

Full-buff debug button, level cap 90 (admins exempt), delevel −1/−10, delevel keeps learned skills
(training passive re-synced to the new level) — all confirmed working.

---

## ✅ INVENTORY: EQUIPPED PANE + SET INFO (2026-07-14) — VERIFIED 2026-07-16

Equipped items have their own tab (Equipped / Bag / Quest), and the set requirement now shows only on
the BODY armor (the set-defining piece), not on boots/gloves/helm/accessories — all confirmed.

---

## ✅ SKILL BAR → DB (2026-07-14) — VERIFIED 2026-07-15

Bar persists per character, follows the character not the machine, "learn all" no longer reshuffles it,
cooldown no longer freezes a slot, cooldown countdown readable — all confirmed.

---

## ✅ SKILL BAR + DEBUG (2026-07-14) — VERIFIED 2026-07-15

Readable bar text, +10,000,000 gold button, debug class change keeps inventory, and drag & drop to
rearrange the slot buttons on the bar — all confirmed. (Dragging a skill FROM the skills window onto the
bar would be a separate feature if wanted.)

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

## ✅ LEVEL-40 STAT-SWAP PASSIVES (2026-07-13) — VERIFIED 2026-07-15

The only thing that moves your main stats now (born with CON/ATK/WIT/DEX; old free grants gone).
Gold-priced (1kk-5kk/level) + affordability-gated, each group a permanent commitment, the stats really
change (Max HP / eva-acc-crit-AS / cast-MP-crit / P&M.Atk), MEN gone as a stat, and the reset NPC
(Mindwright Sela) works. Both follow-up changes are in too: all groups gated by class (fighter CON/DEX/ATK;
mage CON↔DEX + ATK/WIT/MEN) and swaps + training passives require the 3rd class, not just level 40 — all confirmed.

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
- [x] **Go Offline (Auto-Farm)** button → you return to account select and your char keeps farming
  (offline), visible to others, until the 2h cap / death / relogin. VERIFIED.
- [ ] Drop while **auto-farming** (out of town) → offline farm (2h cap). The 2h cap is ONLY for
  offline farming — NOT for a network blip.
- [ ] Drop **mid-combat but not auto-farming** → your char **keeps defending** its current target
  (anti-combat-log) and the 180s grace timer is **paused** until combat ends (30s after the last
  hit); then the grace counts down. It is NOT put into the 2h offline farm.
- [ ] Drop while **out of combat, not auto-farming** → your char shows a **"⚠ Disconnected"** title
  above its head to nearby players, stays frozen and **in your party** (OFFLINE tag) for **180s**.
  Reconnect within 180s → resume seamlessly. After 180s → normal removal (leaves party).
- [ ] A disconnected (grace) char that a mob kills is removed immediately.
- [x] Offline-FARMING chars still look like normal players to non-party (no Disconnected title);
  only the grace state shows the title. VERIFIED.

---

## To test now (auto-hunt / idle farming — Phase 1, 2026-07-08)

**⚠️ Schema change:** added the `AutoHuntJson` column → **delete `Game.Server/bin/Debug/net8.0/game.db`
(+ `-shm`/`-wal`)** so it recreates before running.

### ✅ Auto-Hunt window + Behavior (Phase 1) — VERIFIED 2026-07-15
Auto-Hunt button/window, per-skill enable + reuse, HP/MP potion %, condition logic (attack on cd, buff
if missing, debuff if target lacks, self-heal <70%), auto-potions with auto off, Mana/s footer, normal
loot/XP — all confirmed.

### Offline farming (Phase 2, 2026-07-08) — partially verified 2026-07-16
- [x] With auto-hunt **ON** in a mob field, **close the client / disconnect** → a nearby character still
  **sees your char fight mobs** ("keeps hunting while away"). VERIFIED.
- [x] **Log back in** → re-attach to that same char with the loot/XP gained while away. VERIFIED.
- [x] Disconnecting **in a town**, while **dead**, or with auto **off** does a normal logout (no offline farming). VERIFIED.
- [~] ⚠ **EXPLOIT: an offline farmer that dies comes back ALIVE at full HP.** ✅ **BUILT 2026-07-16 (top batch).** Current: dies → stops → next
  login alive with auto off. Owner: he must **stay DEAD on re-entry** — otherwise "I'm about to die, can't
  escape → go offline-farm → re-enter full HP" is a free death-dodge. → on offline-farm death, persist the
  DEATH so re-login lands dead (at the res prompt / town), not healed.
- [ ] Caps: idle **8h** online / offline **2h**; hitting the idle cap stops auto and blocks re-enabling until relog.
- [x] Auto-hunt while offline still obeys the shared potion cooldown, buff-potion top-up, and skill conditions. VERIFIED.

### ✅ Debug Tuning panel (2026-07-10) — VERIFIED 2026-07-15
Live rates/karma/caps editing, cap=0→unlimited, persists across restart, window size persists,
admin-gated — all confirmed.

### ✅ PvP + flag/karma/PK (2026-07-10) — VERIFIED 2026-07-16
PvP/Counter toggles, PvP-on flags you purple + enemy retaliates + damage lands, attacking an innocent needs
PvP-on (purple/red free, hitting red doesn't flag), no PvP in towns, kill-innocent → red PK + karma,
kill flagged/red → PvP count (no karma), dying as PK −200 karma (clears at 0), farming as PK −20/kill,
counter-attack retaliation, karma persists across relog — all confirmed. (Karma AMOUNT formula is being
reworked — see the "Karma / PK / trade" batch at the top.)

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

### ✅ Party + AFK interaction (2026-07-08) — VERIFIED 2026-07-16
Can't invite an auto-hunting/offline-farming player, AFK (yellow) / OFFLINE (grey) roster tags that clear on
reconnect, kick an AFK/offline member, leadership passes (★ moves) when the leader goes offline-farming,
unanswered invite auto-expires ~30s, an offline member that logs out leaves the party while a reconnecting
one stays — all confirmed.

---

## To test now (party window + mob cast-bar UI — 2026-07-07)

### Party window (WPF client) — partially verified 2026-07-16
- [~] ⚠ **Can't target party members through the party window.** ✅ **BUILT 2026-07-16 (top batch).** Clicking a roster row does NOT target that
  member — **a healer must be able to click an ally in the party panel to target + heal them.** Must-fix.
- [~] **Close (✕) on the party window reopens it immediately** (closes then re-opens). Minor; likely a
  WPF-harness-only quirk — the panel probably shouldn't offer a manual close while you're in a party (it
  hides on leave/disband anyway).
- [x] Invite via target frame → accept/decline prompt; Party panel lists members (name/Lv/class, HP/MP bars,
  ★ leader); leader ✕ kick works — corroborated by the verified invite/kick/AFK tests. VERIFIED.
- [x] Invite button on the target frame, **Leave** removes you / disbands below 2, roster HP/MP bars update
  live as members take damage/heal — VERIFIED.

### ✅ Party loot rules (2026-07-07) — VERIFIED 2026-07-16
New party defaults to Random, leader-only Loot dropdown, changing it starts an all-must-agree vote (Decline
cancels, ~30s timeout, snaps back on cancel), invite prompt shows inviter name + loot rule, Finders Keepers /
Random / Round Robin / Leader Only all distribute correctly, gold always split among in-range members (killer
keeps the remainder), only alive in-range members eligible — all confirmed.
- [ ] Boss/elite crafting-mat pile goes to a single recipient per the loot rule. *(not tested)*

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
