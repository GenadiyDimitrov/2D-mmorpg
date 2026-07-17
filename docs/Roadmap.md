# Roadmap — L2Clone (branch Gena)

Development TODO for game systems / in-game functions, bucketed by time horizon.
This is the "what to build" list (the "what to verify" list is `docs/TestChecklist.md`).
Claude keeps this updated as work moves between buckets.

Legend: `[ ]` open · `[~]` partially done · `[>]` blocked/waiting · `[x]` done (kept briefly for context).

---

## NOW (active / immediate)

### Playtest-5 queue (2026-07-17) — ✅ ALL BUILT (build 0/0, BalanceMatrix anchors held)

Owner re-tested the playtest-4 batch: nearly all VERIFIED. What came back, and what it actually was:

- [x] **🔴 Ghost corpses + alive-on-relog — TWO bugs.** (1) `NormalLeave` only removed a LIVING entity from
  the grid (`if (!entity.Dead)`), so logging out dead left the corpse in the grid forever — broadcast to
  everyone, unresurrectable (the res path looks the target up in `Entities`, which it had left), and a
  relog built a second entity beside it, stacking one corpse per attempt. Now always removes. The repro
  was the **char-select exit**; `EndOfflineSession` was already correct. (2) `DiedWhileAway` was set only
  for offline-farm/link-dead deaths → an ordinary death + logout healed you on relog. Now set on EVERY
  player death and cleared in `ResurrectTarget` as well as `HandleRespawn`.
- [x] **Angel's Protection was self-only for a reason nobody could see from the def**: it's a marker buff
  with `SkillEffect.None` (payload = the `KeepsBuffsOnDeath` flag field, the enum being full), and the
  cast path's ally branch tested Effect bits → it fell through to self-cast. Now `IsAllyTargetable(def)`
  = support Effect **OR** `Category == Buff`. Plus FixedCast 1s / FixedCooldown 10s.
- [x] **Grade penalty reworked to the GAP model + made visible** — see the design in the playtest-5 memory.
  Ladder F1/E20/D40/C52/B61/A76 (the `ItemLevel` tiers; the `ItemGrade` enum is pricing-only and has no
  C/D). gap 1-5 → x0.5/0.4/0.3/0.2/0.1. It is now a CHARACTER debuff applied LAST in `RecomputeDerived`,
  not a per-item scaler, so it can't be out-stacked. Two synthetic never-expiring debuff rows show it.
  `Entity.GradeLevelBonus` is the future "equip N levels early" perk hook. BalanceMatrix: anchors identical.
- [x] Cast bar shows the NAME only · res-scroll reuse 60s→10s · ultimate scrolls + Skill Stone in debug ·
  Equipped tab `[U]` unequip vs bag `[X]` destroy · DEBUG-only 10s character-delete window (undo a
  misclick AND reuse the name; the live 24h/7d/30d ladder is untouched).

### Playtest-4 queue (2026-07-17) — death / resurrection / Angel's pass — ✅ ALL BUILT (build 0/0, SmokeTest green)

Owner tested the death-penalty + resurrection + Angel's Protection batch. ⚠ **The server under test was
started before `3da3d79`**, so every Angel's observation is against the OLD build (20-min, free, no
Skill-Stone cost, no preservation BuffKey/Rank).

**Every item below is now built** — see docs/TestChecklist.md "PLAYTEST-4 FIX BATCH" for what to verify.
Three were not the shallow bugs they looked like:
- **Clicking a skill bar slot could never cast.** The panel's `PreviewMouseLeftButtonUp` TUNNELS
  (root→source), so it cleared `_dragFromIndex` before the slot's bubbling click handler read it — that
  handler's `if (_dragFromIndex < 0) return;` fired on every click. "Did a drag happen" is now its own
  flag (`_dragStarted`), armed at mouse-down.
- **The res prompt was unreachable, not merely ugly.** `ResurrectPrompt` and `DeathOverlay` were both
  centred and the overlay is declared later, so it drew on top and buried the Resurrect button. Merged
  into one window (the owner's design) rather than nudged apart.
- **Resurrection's cast was 4s FIXED**, not the documented 10s, and `FixedCast: true` made cast speed
  irrelevant. Now 10s base, cast-speed-scaled → ~1.67s at the 1999 cap (the owner's 1-2s target).

**Verified working:** death XP penalty level gate (<40 no loss, 40+ loses) · cleric/healer Resurrection
skill levels · the res prompt shows the correct exp-restore % · karma per-kill quadratic curve · party-window
click-to-target · debug menu reorg.

**Corrections (well-specified):**
- [ ] **Party EXP is level-gated to ±9.** The killer distributes exp only to party members within **9
  levels** of himself; a member **±10 or more** away gets **0**. (Today the split is level-weighted with no
  cutoff — `AwardExp`/the party split path.)
- [ ] **BUG: 3rd class granted at level 33** from the debug menu — the level-40 gate is broken on that path.
  (Suspect `HandleDebugThirdClass` not applying `CanTakeThirdClass`'s level rule.)
- [ ] **Resurrection must NOT be auto-learned.** It's a normal skill bought with **SP** — remove it from
  `AutoLearnCoreSkills`. Cast stays **10s**, dropping to **1-2s at max level** (per-level cast time; instant
  would be OP).
- [ ] **Angel's Protection is a TARGET buff, not self-cast.**
- [ ] **Debug karma group: add a `[Clear all]` button.**

**Client / UI (the batch's real friction):**
- [ ] **Dead target is a GHOST target** — targeting a dead player shows no target window, so you res someone
  without knowing who. The target frame must render dead targets.
- [ ] **Target window must be MOVABLE** — it overlaps the skills button in windowed mode. (The `PanelChrome`
  drag layer exists; the target frame never opted in.) ⚠ Its ✕ behaving like **ESC** (cancel the cast + clear
  the target) is **INTENDED — leave it alone.**
- [ ] **GROUP the res + respawn windows into ONE window** (owner's design — they currently overlap so you
  physically cannot accept a res until after you respawn):
  - the res offer appears **above the respawn button in the same window**;
  - **Accept** → resurrect on the spot · **Respawn** → resurrect in town ·
  - **Decline** → keeps the respawn button, does **NOT** close the window or respawn you — so you can
    decline a 0% scroll res and wait for a 100% one.
  - Make them movable as well (or instead, if grouping lands first).
- [ ] **Skill bar is not clickable** — skills fire only from the keyboard. Clicking a bar slot must cast it.
- [ ] **Shift-click targets a DEAD player** in the world; plain click keeps targeting live ones (dead
  requires shift). This retires the "use the party window" workaround shipped with resurrection.
- [ ] **Vendor consumables: buy-quantity prompt.** Clicking Buy opens a confirm window with
  **1 / 10 / 100 / 1000 / Cancel**; it closes **only on Cancel** (so you can buy repeatedly). Later: a proper
  numeric textbox / on-screen numpad for keyboard *and* touch input.

### Playtest-3 queue (2026-07-15) — from the owner's test pass, NOT yet built

Almost everything from the 07-13/07-14 queues VERIFIED (see docs/TestChecklist.md). These are the
changes and new features that came out of the play session:

**Corrections (small, well-specified):**
- [x] **Class uniqueness → DISCIPLINE only + full SUBCLASS REWORK — DONE 2026-07-15.** Adding a class is no
  longer "pick Fighter/Mage". You pick a specific **3rd-class discipline** from the whole catalog (all 3
  races), pre-approved so it **skips the 2nd/3rd-class quests**. Rules: **level 76+** only
  (`ThirdClassCatalog.SubclassLevel`); a **discipline is unique across the character, cross-race included**
  (owning any Tempest bars every Tempest — `Entity.CanAddDiscipline` checks ALL owned classes, the active
  one too; the old `CanTakeThirdClass` excluded the active slot and was wrong for the add path); the new
  class starts at **level 1 with its OWN race**, and every **equipped item is unequipped** (no level-1
  class in level-76 gear). A subclass now carries its own **Race** (`Entity.Race` became a proxy into
  `ActiveSubclass.Race` so ~10k lines of logic keep working); persisted on `SubclassRecord`/snapshot;
  `SubclassDto` gained Race so the client names a class "{Race} {Discipline}". Verified by SmokeTest
  (Human main + Ork Bulwark subclass, cross-race, pre-approved 3rd class, level isolation, relog).
  ([[subclass-system-design]])
- [x] **Cap subclass COUNT — DONE 2026-07-15** (part of the rework above). Normal accounts cap at
  `GameConstants.MaxSubclasses` (4); **admins are unlimited** (the no-duplicate-discipline filter still
  applies to them). ⚠ delete game.db (new `Race` column on the Subclasses table).
- [ ] **Revert the mage-click change — ALL classes click-to-attack.** A mage out of MP must be able to
  melee a mob to finish it. Undo the `iAmCaster` guard in `WorldCanvas` click handling.
- [ ] **Skill cast must CANCEL the auto-attack walk, not pause it.** Double-click a mob → walk to melee;
  casting a skill only pauses movement, so the char keeps walking after the cast. Clear `TargetX/Y` on
  cast start.
- [ ] **Set info only on the set-bearing BODY armor.** Boots/gloves/helm (accessories) currently show
  "3/4 heavy set" even after the body is swapped to robe. `BuildSetSection` should return null for
  non-body pieces (or show the set the equipped BODY defines, not the hovered accessory's).
- [ ] **Stat-swap groups: gate ALL by class, not just ATK.** Fighter: CON↔DEX, ATK↔CON, ATK↔DEX only.
  Mage: CON↔DEX, ATK↔MEN, ATK↔WIT, WIT↔MEN only. (`StatSwapsFor` in `Skills.StatSwap.cs`.)
- [ ] **Stat-swap + training passives require the 3rd CLASS, not level 40.** They appear at 40 now; owner
  wants them only after the 3rd-class change. Gate the learn-list + the training auto-grant on
  `ThirdClass > 0`.
- [ ] **Cleanup: remove the TEST-ONLY TestHeal skill** (power-1000 heal on every char @76). Both numbers
  it existed to read (OffChannelFactor 0.6, HealK 15) are now decided. 3 spots, search `TEST ONLY`.

**Karma / PK — corrections DONE 2026-07-15:**
- [x] **Karma per-kill cap 15k (was effectively 1kk / could overflow).** `KarmaMaxPerKill = 15_000`, and
  the level/consecutive exponents are clamped to 15 before `Math.Pow` so a huge level gap can't produce a
  double that overflows the `(int)` cast to `int.MinValue` (the "−2.1 billion karma" bug — a level-380
  admin killing a level-41 char). Load also clamps karma to `[0, 1_000_000]` to heal any corrupted rows.
  ~500-1000 mob kills (−20 each) clears a full cap, as intended.
- [x] **Trading blocked while PK or flagged.** `HandleTradeRequest` and `HandleTradeRespond` refuse if
  EITHER party is not `Innocent` (re-checked at accept). Selling to vendors is unaffected.
- [x] **A PK (red) can't BUY from vendors** (`HandleBuy` refuses `PvpFlag.Pk`); a PvP-flagged (purple)
  player still can. Selling unaffected.
- [x] **4 debug karma buttons** (Functions tab): +1000 / −1000 (cross the PK line fast) and +20 / −20
  (one mob-kill's worth). `DebugKarma(delta)` → `HandleDebugKarma`, clamps `[0, 1_000_000]` and clears the
  PK streak/red name at 0.

**Debug menu reorg — DONE 2026-07-15:**
- [x] New **"Class" tab** holds all class management, grouped: *profession & skills* (class change + give
  all skills) · *classes (subclass)* (swap + "Add a class" discipline picker) · *reset character*. The
  **Functions** tab is regrouped top-to-bottom: **full buffer → gold+SP → level → karma**. The "Add a
  class" picker lists every discipline you don't already own, across all races, gated at level 76.

**Deferred / needs design:**
- [x] **Grade penalty (L2-style low-level-in-high-grade gear) — BUILT 2026-07-16.** `GradePenalty`
  (Game.Shared/Items.cs): min level F=1/E=20/B=40/A=52/S=61; below it the item's **weapon ATK / armor DEF**
  is multiplied by ×0.5(E)/0.4(B)/0.3(A)/0.2(S). Applied in `Entity.RecomputeDerived` before masteries/sets.
  The **equip level gate was removed** (owner: you may equip any grade at any level and just eat the
  penalty), and `ItemCatalog.RequiredLevel` now delegates to `GradePenalty.MinLevel`. Numbers tunable.
- [ ] **Marketplace + premium currency.** Player marketplace (list/buy) and a second, premium currency;
  both tradable and inter-convertible with gold. (Noted from the gold work below.)
- [ ] **Damage-model rework (unified `{Flat, Mod}` skills + lowered M.Atk)** — see `docs/DamageModel.md`.
  Makes physical skills scale with pAtk (Mod), unifies physical/magic skill authoring, and drops M.Atk from
  cosmic `levelMod²` to P.Atk-size. Design drafted + measured; **awaiting owner's pick of Option A (linear,
  recommended) vs Option B (keep √)**. Reverses the signed-off magic scaling → calibrate in BalanceMatrix
  and re-validate the anchors before build.

**New features (bigger — some need a design decision, see docs + questions below):**
- [~] **Gold — long + TRADABLE + coloured display — DONE 2026-07-15 (owner: not an item).** Gold was
  already `long` end-to-end (no int.max cap). Added: (1) **tradable in the trade panel** — each side types
  what they PAY, net gold changes hands on completion (server clamps to what you own, re-checks at commit,
  resets ready flags on change); (2) **inventory gold line, colour-tiered** — white <1kk, yellow <100kk,
  green <1kkk, purple ≥1kkk. NOT made an inventory item (owner call). Still later: marketplace + premium
  currency (both tradable / inter-convertible).
- [x] **NPC buffer — 3 paid options — DONE 2026-07-15.** The buffer NPC now opens a dialog (was buff-on-talk)
  with: **Full buff set**, **Restore HP/MP to full**, and a **single-buff list**. Free ≤40; priced above:
  each buff = `3k · buffLevel`; the buffs are single-level defs today but are the MAX-STRENGTH set, so
  priced at a nominal **level 5** → **15k/buff, 135k for the full set** (owner's example: 10×5×3=150k).
  **Calibrated against mob gold**: `MobGoldReward = 25 + lvl·8 ≈ 345/mob` at L40, dropped on EVERY kill →
  ~120-170k gold/hour, so a full buff ≈ ~1h of farming (the intent). Restore = `10k·(1−hp/max) +
  10k·(1−mp/max)`, per-pool cap 10k. Window 6-75 unchanged. Server-authoritative (clamps gold, re-checks
  range). Tunable consts: `BuffCostPerLevel`, `BufferBuffNominalLevel`, `RestoreCostCap`. When multi-level
  buffs land, the nominal 5 becomes the real per-buff level and cost tracks it. ([[buffer-enchanter-design]])
- [x] **BARE-HANDS — FIXED 2026-07-15** via the L2 multiplicative P.Atk formula (owner chose this over the
  penalty). See commit `ac8108f` / `docs/BareHands.md`. Naked is now feeble by the FORMULA (weapon is the
  base), armed high-level preserved, magic untouched (proven by A/B). Companion investigation for defence:
  `docs/Unarmored.md` — conclusion: leave it, no live problem now that naked can't deal damage.
- [x] **Persist popup positions — DONE 2026-07-15.** `client-settings.json` is now nested
  (`{Window:{Position,Size}, Panels:{<name>:{X,Y}}}`); each popup's drag offset is saved on window CLOSE
  (not per move) and restored next run; untouched panels stay at their default (0,0). Window geometry moved
  under `Window`.

### Playtest-2 queue (2026-07-14) — agreed, NOT yet built

- [x] **Server would not start** — `SkillCatalog`'s static field initializer `All = BuildCatalog()`
  (Skills.cs) ran BEFORE `StatSwapGold`, a static array in the other partial file
  (Skills.StatSwap.cs), was assigned → NRE → the whole type failed to initialize. Static field
  initializers across partial files run in compiler FILE order. Fixed with an **explicit static
  ctor** (its body is guaranteed to run after every field initializer). Also de-risks
  `FighterArmorLevels` / `MageRobeLevels` / `NewbieBuffSet`, which survived on file-ordering luck.

- [x] **MAGIC RE-SCALE (the big one) — DONE 2026-07-14.** The culprit was **the mob curves**, not the
  jewels. Researched the retail L2 mob table (Keltir L1, Grizzly L17, Ghoul L32, Grandis L40,
  Invader Shaman L63, Tracker Howl L81, Drake Warrior L85) plus the L2J stat formulas, then built
  `tools/BalanceMatrix` (a console app that constructs REAL geared Entities and prints the matrix)
  so every number below is **measured, not derived**.

  **What was actually wrong:**
  1. **Mob P.Def/M.Def were QUADRATIC; L2's are LINEAR** (~4.2*lvl and ~3*lvl, floored at L1). The
     two curves cross at ~43 — so our low mobs were paper (M.Def 5 at L1 where L2 has 30: a L21
     mage nuked a L24 mob for 2k) and our high mobs were walls (448 at L80 vs L2's ~253).
     **One bug, both ends** — exactly the symptom reported.
  2. **Mob HP was 2.8-4.5x too high above L45** (15,420 at L80; L2's Tracker Howl is ~5,500). Now
     `40 + 0.8*lvl^2`. Fat mobs are NOT a fatter curve — L2 makes a specific mob tanky with an
     "HP Increase (2x/3x)" PASSIVE, which is exactly our `MobMod` layer. Keep the curve lean.
  3. **Player M.Atk was flat in level.** L2: `M.Atk = base x INTbonus^2 x levelMod^2` and
     `M.Def = base x MENbonus x levelMod` (verified against L2J `FuncMAtkMod`/`FuncMDefMod`). The
     **square is the whole trick**: magic damage takes `sqrt(M.Atk)`, so `sqrt(levelMod^2) =
     levelMod` and magic ends up LINEAR in level, like physical. We had neither term. (MEN was
     already wired; only the level terms were missing.)
  4. **The nuke ladder stopped at level 35 / power 44**, so a L85 mage fought with a L35 spell.
     Extended Elemental / Quick / Vampiric Bolt to **13 levels, learned every 5 from 20 to 80**, on
     L2's linear power curve anchored **108 @ 74** (top = 116 @ 80).
  5. **Mob EXP ignored toughness** — a boss with 10x the HP paid the same EXP as the trash beside it.
     Now `MobExpValue` = level curve x the mob's actual HP multiple (L2 pays by toughness: a Drake
     carries ~8.5x a normal mob's HP and pays ~7.5x the EXP).

  **Mob P.Atk / M.Atk / MP measured to already track L2 within ~30% — left alone. Jewels untouched,
  per owner.**

  Measured before -> after (`dotnet run --project tools/BalanceMatrix`):

  | | mage casts to kill a same-level mob | mage dmg to a tank | fighter hits to kill |
  |---|---|---|---|
  | lvl 20 | 0.5 -> 0.5 | 117 -> 167 | 1.3 -> 2.0 |
  | lvl 40 | 3.5 -> 1.3 | 157 -> 225 | 11.1 -> 8.7 |
  | lvl 61 | 19.0 -> 2.0 | 164 -> 336 | 41.5 -> 15.0 |
  | lvl 85 | **79.4 -> 2.6** | 184 -> **485** | **147.7 -> 24.9** |

  The mage went from exploding (0.5 -> 79 casts) to nearly FLAT (0.5 -> 2.6). Physical rode the
  same broken mob curve and got the same benefit for free.

  ### ✅ SIGNED OFF IN THE 2026-07-14 PLAYTEST — do not re-tune without a new reason
  Owner, in-game: *"dmg seems fine — mage to tank 300-400 (1100 crits) for 11k HP is ok; tank to mage
  300 crits, ~120 dmg for 2k6 HP is fine"*, *"mage dmg is ok vs monsters, can solo, can dmg"*.
  This **closes the one number that was left open**: the matrix predicted mage-vs-tank at 461/485 and
  I flagged it as maybe too hot — but that reading is both sides UNBUFFED. Buffed, in the real fight,
  it lands squarely in the 300-400 target. The matrix was right; the gap was the buffs, not the
  balance.

  **Still noted, not blocking:**
  - A level-20 mage still one-shots (0.5 casts). That IS retail L2; the magnitude is at least
    proportionate now (787 dmg vs a 360-HP mob), and the curve ABOVE it is fixed.
  - **Leveling at 60-85 is now ~3x faster in wall-clock** (same EXP/mob, mobs die 3x sooner). The
    EXP curve is deliberately untouched; `ExpRate` is live-editable in the debug tuning panel.
  - Our absolute magnitudes stay smaller than retail (mage M.Atk reads ~2.9k, not L2's ~16k). The
    RATIOS give the asked-for fight; making the numbers *look* like L2's is a cosmetic rescale.

- [x] **Stat-swap "direction" rule — DONE 2026-07-14.** Every stat you touch now commits to ONE
  direction: taking `+X -Y` bans every other skill that RAISES X (the old `ExclusiveGroup`, kept —
  the skill-reset NPC keys off it), every skill that LOWERS X, and every skill that RAISES Y. A
  second skill that also LOWERS Y is still allowed and stacks. That makes the circular net-zero ring
  (+A-B, +B-C, +C-A = 45kk for +0) unreachable — the 2nd skill in any ring always tries to raise a
  stat the 1st one already sold.
  - `Skills.StatSwap.cs` restructured around a `SwapTable` of `(id, name, Up, Down)`. The exclusive
    group, the `PassiveEffect`, the description AND the rule are all derived from it, so a new swap
    cannot fall out of sync with the rule that polices it.
    `SkillCatalog.StatSwapConflict(id, learned)` IS the rule; enforced on the server learn path and
    mirrored in the WPF learn list (a banned pick is never offered). Verified in
    `tools/BalanceMatrix`: the owner's worked example reproduces exactly (holding `+ATK-MEN` and
    `+WIT-MEN` leaves only `+CON-DEX` / `+DEX-CON` open), and the ring is blocked at its 2nd skill.
  - **BUG FIXED: debug "learn all skills"** no longer grants every stat swap.
    ⚠ **Deviation from the original ask** (which was "take the first, skip what it bans"): it now
    grants **NO** swaps at all. Any subset is an arbitrary BUILD decision, and that greedy pick lands
    on four swaps that ALL sacrifice **ATK** — our single power stat — for **-20 ATK**, which would
    quietly wreck the damage numbers that button exists to test. It now says so in chat rather than
    doing it silently. Buy swaps deliberately in the skills window (they cost gold, and the
    skill-reset NPC un-picks them).

- [x] **Equipped items out of the inventory list — DONE 2026-07-14.** Inventory tabs are now
  **Equipped / Bag / Quest**, and an item lives in exactly ONE of them: the **Bag hides what you are
  wearing**, which is what unclogs it. The Equipped pane is ordered by body slot and each row is
  labelled with its slot, so it reads as a character sheet you can swap gear from.
- [x] **Skill bar + auto-hunt are CHARACTER data → in the DB — DONE 2026-07-14.**
  **Auto-hunt was ALREADY there** (`AutoHuntJson` on the character row) — only the skill bar had to
  move. New `SkillBarJson` column ⚠ (delete `game.db`), `SkillBarDto` both ways, `SetSkillBarCmd`,
  `Entity.SkillBars`. `client-settings.json` is now **window geometry ONLY**, and says so.
  - Stored as a **MAP** (bar-key → slots), not one array, because a bar is per-CLASS: when subclasses
    land, each class gets its own key with no schema change. Today one key, `Entity.MainSkillBarKey`.
    ([[subclass-system-design]])
  - **BUG FIXED: "Learn all skills" reshuffled the bar.** Root cause: the client parks newly-learned
    skills in free slots on every Learned push, and the saved layout was loaded from a *file* behind a
    latch — so when Learned won that race the client re-filled an EMPTY bar from scratch (in id order)
    and then **saved that over the player's real layout**. Now the server pushes the bar BEFORE the
    learned skills (SignalR preserves per-connection order) and auto-placement is a no-op until it has
    arrived. It also only saves when the bar actually moved — each save is now a round-trip + DB write.
  - **BUG FIXED: a cooling-down skill could not be moved or removed.** It set `Button.IsEnabled=false`,
    and a disabled WPF button receives no mouse input at all. Cooldown is a CAST restriction, not a
    "you may not rearrange your UI" one — the slot is now merely dimmed (`UseSkill` already refuses to
    fire an unready skill).
  - Cooldown countdown recoloured (DarkGoldenrod on the light bar; Gold in the dark Skills window) and
    **mirrored into the Skills window**, so you can see what's ready without reading along the bar.
- [x] **Debug: 100k gold button -> 10kk — DONE 2026-07-14.** (100k could not fund a single stat swap,
  which costs 1kk-5kk per level.) **Debug race/class change now KEEPS the inventory** (owner reversed
  his earlier request): everything is UNEQUIPPED instead, and the starter kit only tops up pieces you
  do not already own, so repeated re-rolls do not silt the bag up with duplicate newbie boxes.

- [x] **BUG: skill-bar drag & drop — ROOT-CAUSED AND FIXED 2026-07-14.** A WPF `Button` CAPTURES the
  mouse on press, and that single fact produced BOTH symptoms:
  1. `DragDrop.DoDragDrop` is unreliable when called from a control that holds capture -> "a drag is
     very hard to even start".
  2. Once that capture IS lost, `MouseMove` stops being routed to the button you pressed and goes to
     whatever button is now UNDER THE CURSOR — whose handler fired with ITS OWN slot index and so
     dragged **its own** skill -> "it moves a DIFFERENT skill than the one grabbed", and why the next
     attempt grabbed yet another one.
  Last session's fix (carry the skill id in the drag payload) could never have worked: the WRONG id
  was being picked up in the first place. Now the drag origin is recorded ONCE at mouse-DOWN
  (`_dragFromIndex`) and never re-read from whichever button raises the move event, and capture is
  released before `DoDragDrop`. The payload still carries the skill id — that guards the OTHER hazard
  (a re-render mid-drag invalidating the index before the drop lands).
  ⚠ **Not click-tested** — WPF cannot be driven from the agent here. Needs the owner's hands.

- [x] **BUG: set info missing from the item window — FIXED 2026-07-14.** It was reported built on
  07-13 because it *was* built — just never reachable. TWO faults:
  1. `BuildSetSection` was only ever attached to the hover **TOOLTIP**, never to the item WINDOW —
     which is the window you open when deciding what to wear. It is now in both.
  2. That tooltip set **white** text on WPF's default **light** tooltip chrome, so even the hover
     version was white-on-white and invisible. The tooltip is now explicitly dark-backed.
  The set DATA was fine — `tools/BalanceMatrix` now asserts it (55 items carry a `SetId`, 27 sets,
  **0 orphaned**), so a future set that forgets its catalog entry gets caught instead of silently
  rendering nothing.
- [x] **BUG: "Learn all skills" granted every stat-swap passive — FIXED 2026-07-14** (see the
  direction-rule entry above; it now grants NO swaps and says so).
- [x] **BUG: "Learn all skills" reshuffles the skill bar — FIXED 2026-07-14** as part of moving the
  bar into the DB (see that entry above for the root cause).

- [x] **SUB-CLASSES — BUILT 2026-07-14** ⚠ (new `Subclasses` table → delete `game.db`).
  One character owns several classes and plays one at a time. See [[subclass-system-design]].

  **THE SPLIT** (`Subclass.cs` documents it — get this wrong and it has to be redone):
  | | |
  |---|---|
  | **CLASS-level** (`Subclass`) | level, XP, skill points, base class, 2nd/3rd class, CON/ATK/WIT/DEX, learned skills, **skill bar** |
  | **CHARACTER-level** (`Entity`) | race, inventory, gold, karma, quests, profession, auto-hunt, position |
  | **CLIENT-level** (`client-settings.json`) | window position/size, and nothing else |

  The four core stats are CLASS-level because they derive from (Race, BaseClass) — swap a fighter for
  a mage and CON/ATK/WIT/DEX must swap with him. ⚠ **UPDATE 2026-07-15: Race moved to CLASS-level too** —
  the subclass rework allows **cross-race subclasses** (a Human main with an Ork Bulwark subclass), so each
  `Subclass` carries its own `Race`; `Entity.Race` is now a proxy into `ActiveSubclass.Race`.

  **Why the refactor was small:** `Entity.Level` / `.BaseClass` / `.LearnedSkills` / `.SkillPoints` /
  `.Con…Dex` / `.SecondClass` / `.ThirdClass` / `.Exp` are now **PROXIES into the active subclass**. So
  every existing line of game logic that says `player.Level` still works untouched, and a class swap is
  just moving an index. Exactly **three** call sites broke; the rest of the server never noticed.

  **Persistence:** a `SubclassRecord` table (one row per owned class) is the source of truth. The
  matching columns on the character row are kept as a **mirror of the ACTIVE class**, rewritten on every
  save, purely so character-SELECT can list a character without loading its classes. Never read them for
  gameplay. A character with no subclass rows (fresh, or created before this) reconstructs slot 0 from
  that mirror, so nothing needs migrating.

  **Debug flow (the owner's test loop) — UPDATED 2026-07-15:** Debug → **Class** tab → "Classes
  (subclass)" lists what you own, with a **Switch** button each, plus **"Add a class (discipline)"** which
  opens a picker of every discipline you don't already own, across all races (gated at level 76). Swap on
  the spot to compare two builds instead of relogging onto another character. Each class keeps its own
  level, XP, skills and **skill bar** — swap away, swap back, find it exactly as you left it.
  A swap clears buffs, the cast in progress and the combat target and re-pushes stats/skills/bar/progress.
  **Debug character-RESET drops the subclasses on purpose** — it re-rolls the whole character (race + base
  class) at slot 0.

  **CLASS UNIQUENESS — REVISED 2026-07-15 to DISCIPLINE-ONLY, cross-race.** A character may not walk the
  same **DISCIPLINE** twice, across ALL the classes it owns (active included) — and this spans races,
  because the same discipline (Tempest) exists as a separate 3rd class for each race. There is **no
  archetype bar** (owner: you SHOULD be able to own two mages, e.g. a Lightbringer and a Tempest). The
  add-a-subclass path uses `Entity.CanAddDiscipline` (checks every owned class); the older
  `CanTakeThirdClass` (used by the level-40 quest change) deliberately excludes the ACTIVE slot, which was
  wrong for the add path and is why `CanAddDiscipline` exists. Count is capped at `MaxSubclasses` (4) for
  normal accounts; **admins unlimited** (the discipline filter still holds). Verified by SmokeTest.

  **Deliberately still NOT built** (player-facing rules on the COMMAND, not the mechanism):
  safe-zone-only swapping, the 5-minute swap delay, and a
  player-facing UI. `HandleSwitchSubclass` does the state work; those rules will gate the entry point.

### Earlier

- [x] **Mob base-stat curve** — owner sent a L1-85 mob CSV (`docs/mobs/mob_base_stats.csv`).
  Wired as `MobBaseStats` (per-level HP/MP/P.Def/M.Def/P.Atk/M.Atk curve, interpolated);
  `Entity.RecomputeDerived` mob branch now reads it (`MobMaxHp/MobMaxMp/MobDefence`/the
  `MobMagicDefence` override are retired for mobs). Structure = `curve(level) × conMod × passives`
  (conMod hook = ×1 for now). Outliers (Rift Portling) = curve × HP/P.Def MobMod passive.
- [x] **Mob roster + zone/drops rollout** — the old placeholder mobs replaced by the 80-mob
  renamed roster (Level + `MobCategory`); the ~15 field zones + boss/elite + 3rd-class & class-
  change quest targets rewired by band; drop tables ported by level via `MobCatalog.StandardDrops`.
- [x] **Weapon-type resistance (P.Def route)** — `StatCalculator.WeaponDefenceCoef` folds a
  per-weapon (Pierce/Blunt/Bow) coefficient INTO pDef at the hit (so an ignore-def skill bypasses
  it); `Entity.{Pierce,Blunt,Bow}DefCoef`; demo `obsidian_knight` (resist sword/arrow, weak to blunt).
- [x] **Ranged + caster mobs (mob roles)** — `MobRole` {Melee, Archer, Mage}. Archer = bow basic
  from ~450 range, ×2 P.Atk, light armor (orc/dune/fen/dread archers). Mage = NO basic attack,
  casts two leveled mob spells (`mob_nuke` 600/4s/1s pow 18-129, `mob_bolt` 150/1.5s/0.5s pow 7-33;
  13 levels by mob level), higher M.Atk / lower P.Atk+P.Def, MP-gated → out of MP it stands
  helpless (watcher_eye/aether_wisp/rift_portling/radiant_mage). Reuses the player cast pipeline
  (LearnedSkills + QueuedSkillId); mobs cast at authored time (WIT multiplier bypassed).
- [x] **Golem-type resist** — obsidian_knight: Pierce ×1.43 P.Def, Bow ×2, Blunt ×0.5 (weak).
- [~] **RE-CHECK balance after the mob curve** — mobs are now ~2-3× prior HP/def/atk. **Matrices
  regenerated** (`docs/BalanceMatrix.md` §H Mob↔Player @40/@75; §I per-gear-tier 40/52/61/76 vs the mob
  curve from `gear_sets.csv`). NO one-shots; gear DEFENSE keeps pace, but **player OFFENSE falls behind at
  high tiers** (solo grind balloons — fighter 13→131 hits, mage 19→210 casts L40→76). **Owner decision:**
  raise high-tier weapon atk / ease mob HP+def at 61-85 / lean on crit+attributes+party; add a jewel tier.
  Also open: cleric-solo-L30, <40 feel, archer ×2 lethality to squishies.
- [~] **Gear/item overhaul** (`docs/gear/gear_sets.csv`). **DONE:** foundation (StatMods carries item/set
  stats; MAtk%/MagicCrit attr types + ToStatMods bridge) + **40 tiered WEAPONS** (8 types × 20/40/52/61/76,
  ids `<key>_t<level>`, D/C/B/A display) with level-driven attributes (count 40→1/52→1/61→2/76→3, per-level
  maxes, caster pool via `IsMagicWeapon`, bow slow/very-slow via `AttackSpeedBase`) + **attribute-cancel
  debug** (`DebugCancelAttr(index)`; -1 = all). + **50 base ARMOR/shield/accessory/JEWEL pieces**
  (`TieredArmor()`, base stats on existing rails, no attributes). **NEXT:** set BONUSES via StatMods incl.
  main-stats (main-stat pre-pass in RecomputeDerived) + dmg/support variants + cohesive names; debug-give
  single body + accessory box; remove old armor drops + add new as rare; regen matrix. See [[gear-item-overhaul]].
- [>] **Base class kits** — owner to provide several passives/buffs/skills per class; wire them
  as real per-class content (beyond the placeholder discipline kits), then tune.
- [>] **Fighter balance pass** — awaiting owner targets (Venomweaver burst cap, tank durability
  vs +N-level mobs, etc.). Mechanics are fine; numbers need it.
- [>] **Skill-detail TITLE shows base name** — owner to give exact skill + race/2nd/3rd class
  next test; suspect client `_myThirdClass` not synced after a DEBUG 3rd-class change.

- [x] **Training Grounds** — immortal/stationary/0-damage **Training Dummy** mobs at Lv
  20/40/60/80 (MobType.Dummy + Entity.TrainingDummy; spawn zones ~(22500–25500, 4000)) for
  damage/skill testing. Reach via debug Teleport → Zones.

- [~] **Tune placeholder numbers** after the next playtest: fighter weapon masteries, armor
  masteries, healer powers, mob modifiers, caster bow penalty. (See `docs/TestChecklist.md`.)
- ~~Low-level **physical mob damage** so mobs don't ~one-shot players~~ — **NOT A REAL ISSUE
  (owner, 2026-07-14): "mobs don't one hit players".** This entry was stale; the mob-curve rework
  already moved these numbers. Don't act on it.
- ~~**Cleric-solos-a-30-mob** balance pass~~ — folded into the above; owner says the balance pass as
  written "isn't right". Re-open only from a fresh in-game observation, not from this list.

- [x] **Level cap 90 + delevel buttons — DONE 2026-07-14.** `GameConstants.MaxPlayerLevel = 90`, applied
  in the XP path (EXP parks at 0 at the cap instead of piling up invisibly, which would otherwise dump
  several instant levels the day the cap is raised). **Admins are EXEMPT** (`LevelCapFor`) so the top of
  the curve stays testable without lifting the cap for everyone. `DebugLevelCmd` now carries a DELTA, so
  +1 / +10 / **−1 / −10** all share one path and one round-trip (+10 used to fire ten separate commands,
  each with its own level-up broadcast and character save).
  **Delevel keeps every learned skill** (owner) — the "Skills to Learn" tab already gates by level, so it
  simply stops offering what you can't reach; nothing needed changing there. ⚠ The ONE thing re-synced is
  the auto-granted combat-training passive, whose level is a pure function of character level: it is not a
  skill you chose, and leaving a level-9 (+100% attack) passive on a character just dropped to 40 would
  silently inflate the damage numbers you delevelled in order to measure.

- [x] **Debug "Full Buffs (1h)" button — DONE 2026-07-14.** The whole NPC buff set on yourself, at ANY
  level, without the walk. Deliberately has NO level gate: the NPC's 6-75 window is a GAME RULE, and
  debug exists to skip the walk, not to re-enforce the rule. It is the only way to get buffed above 75
  today — which matters, because the balance numbers the owner signs off on are BUFFED numbers.
  (`GrantFullBuffSet` is now shared by the NPC and the button; the level gate is the only difference.)

- **NOT touched: the NPC buffer's level gate.** Still `lvl 6-75` (`ApplyNewbieBuffs`), as designed
  ([[buffer-enchanter-design]] — the full-buff NPC is the SOLO stopgap to 75). Owner: leave it at 6-75.

- [ ] **NPC buffer must give LEVEL-APPROPRIATE buffs** (owner, deferred 2026-07-14). Today it hands the
  **max-level** set to everybody, so a level-10 character walks away with buffs no real buffer could cast
  for another 60 levels. Rule: **an NPC may never grant a buff stronger than a real player buffer of that
  character's level could cast.** So the granted buff LEVEL must scale with the character's level.
  **Blocked on the buff skills becoming multi-level first** (they are single-level today) — the same
  `SkillLevel` ladder the nukes now use ([[multi-level-skills]]). Do it with the Enchanter/buffer class
  work ([[buffer-enchanter-design]]).

- [ ] **Training passives as a purchasable BUFF or ITEM, not an auto-granted passive** (owner idea,
  2026-07-14 — thinking out loud, not decided). Physical/Spirit Training is currently auto-granted and
  its level is a pure function of character level (it is our soulshot/spiritshot stand-in: +10%…+100%
  attack). The idea: make it something you BUY — a day-long buff, or an item effect — rather than
  something you simply have. That would give the gold sink real teeth and make "am I shotted?" a
  decision instead of a constant. **Consequences to think through before building:** every damage number
  in `tools/BalanceMatrix` currently assumes the passive is ON (it is granted automatically), so the
  baseline would shift; and an unbuffed/unshotted character becomes a genuinely different power level,
  which is exactly what soulshots do in L2. See [[stats-via-skills-not-hardcoded]].

- [ ] **Shot-buff items + passive RUNES** (owner, 2026-07-17 — NEXT, after the death/res playtest). Reframe
  soul/spiritshots as inventory items that grant a TIMED buff (not per-hit): soulshot ≈ +100% pAtk, spiritshot
  ≈ +41% effective mAtk. Needs a **`SurvivesDeath` buff flag** (persists through death, independent of Angel's
  Protection). Alongside them, **RUNES that act like a passive**: while held in the INVENTORY they add to your
  passives / the items buff-bar row — timed, do NOT disappear on death, and are NOT consumed (an inventory-held
  standing buff, distinct from the used-up shot consumable). Vendor pricing (100-200k, 1-2h) ties into the
  deferred premium-currency work. See [[death-res-noblesse-shots-design]] §4.

### Playtest-3 leftovers (2026-07-14) — the only two things from that session not built

- [x] **BUG: the mage runs into melee to auto-attack — FIXED 2026-07-14.** Traced rather than guessed:
  `AfterOffensiveSkill` already excludes mages (post-CAST), and `Retaliate` only engages mobs — so the
  culprit was the **click**. The client's "clicking a mob attacks" sends an Attack command, the server
  engages you, and `UpdateAutoAttack` then CHASES into basic-attack range — i.e. a caster sprinting into
  melee to poke with a staff (magic weapons have no weapon range and near-zero basic damage), dragged out
  of casting position. **A mage now only TARGETS on click; fighters keep click-to-attack.** Fixed at the
  click rather than in `UpdateAutoAttack`, because auto-hunt deliberately DOES walk a caster in (for SPELL
  range) and still melees if you tick its Basic Attack row — gating the chase server-side would have
  broken that.

- [x] **Movable popups — DONE 2026-07-14.** Every popup gets a drag strip, a ✕, and **click-to-raise**
  (the other half of "the Debug window is covering my inventory" — you can now bring one to the front).
  Built as ONE reusable chrome applied at runtime (`MainWindow.PanelChrome.cs`) rather than authored into
  thirteen Borders in XAML: one copy of the drag code, a new panel opts in with a single line, and each
  panel keeps its authored home position (it is nudged from there by a `RenderTransform`, so no layout
  churn). `EquipPopup` gets its real close action, since its ✕ must also clear the item it is acting on.

## NEXT (clear, mostly self-contained — can do without owner input)

- [x] **"Reset skills" NPC (stat-swap re-pick)** — BUILT. `NpcRole.SkillReset` + `ForgetSkillCmd`;
  **Mindwright Sela** in Brackenford un-learns any skill with an `ExclusiveGroup` (today: the
  level-40 stat swaps), freeing its group to commit again. Removing is FREE; the gold spent is NOT
  refunded (and the dialog says how much you're writing off).

- [ ] **Per-type CC resist (the gear CSV's `x1.7` lines)** — owner: wanted, but LATER (2026-07-13).
  The armor sets author resists as a **multiplier per CC TYPE**, e.g. `Sleep/Hold/Poison/Bleed
  Resist x1.7` on the def-oriented 61/76 lines and `Stun/Fear Resist x1.7` on the `_dmg` variants.
  We currently collapse all of that into a **single flat `StatMods.CcResist: 0.4f`** — so the two
  variants are indistinguishable and the authored ×1.7 is not what's applied. To do properly:
  split `CcResist` into per-type channels (Sleep / Hold / Poison / Bleed / Stun / Fear), express
  them as the CSV's multiplier on the resisting side of `StatCalculator.DebuffLandChance`, and
  re-author the set rows from `docs/gear/gear_sets.csv`. Until then the `_dmg` vs `_def` set
  identity ("tanky vs aggressive") does not actually differ in CC behaviour.

- [~] **Combat primitives layer** (prerequisite for disciplines, bosses, PvP). Build to
  `docs/Disciplines.md` rules. **Started:** the ATK-vs-CON/WIT **debuff hit-contest**
  (`StatCalculator.DebuffLandChance`, 10–90%, 50% at equal, bosses immune) + **Slow**
  (move-speed %, first contested CC; demo skill "Frost Bind" for nukers) + physical
  **`[Double]` crit** (`SkillDef.CanDouble`, ×2 from higher of DEX/ATK cap 30%; demo skill
  "Cleaving Strike" for warriors; existing skills unchanged) + **Stun & Fear** (contested,
  action-locking; demo skills "Shield Bash"/"Terrifying Roar") + **Root-via-contest** +
  physical **Slow** (demo "Hamstring") + a **damage-OUT pipeline** (`FinalizeDamage`):
  a 2×3 matrix of PvE/PvP × skill/magic/basic damage bonuses + per-skill `Pvp/PveDamageMult`
  (all neutral until PvP exists). Demo "War Focus" (+15% AS, +25% PvP skill/basic).
  + **conditional damage** (+% vs slowed/rooted/stunned/feared; `SkillDef.ConditionalOn`/
  `ConditionalDamagePct`; demo "Glacial Spike"). Existing non-contest debuffs left on the
  fizzle model (owner: new-only). **P1 light items DONE.**
- [~] **P2 heavy systems — STARTED.** **DoT-with-stacks DONE (L2 separated model)**: a DoT
  applies (1) a **damage effect** (flat per-tick, overrides by Rank, cure/cancel by flag+level)
  and (2) a separate **stack counter** (`SkillDef.StackKey`, hidden/`Internal`) that the burst
  consumes (`ConsumeStackKey`) for ×stacks — leaving the DoT. Counters are per-skill and
  shareable, independent of override/cure. Demo: Rupture → Detonate Wounds (Venomweaver).
  **Generalized stacking**: editable max + a per-stack effect TABLE (`SkillDef.StackLevels`) —
  each stack is an effect level (its own Effect + Magnitudes), so a stack can change the effect
  qualitatively (Tempest "Creeping Frost" = slow 10/20/30% on 1-3, FREEZE on 4). Stacks only on
  a successful land. A bare counter = stacking with no table (rogue burst fuel).
  **Poison/venom secondaries DONE**: new `DebuffAtk` / `DebuffAtkSpeed` / `DebuffCastSpeed`
  stat-debuff channels (outside AnyBuff, folded in the Effective getters); Venomweaver is now
  per-race — Human bleed (−MS), Elf poison (Toxic Sting/Burst, −AS/cast), Ork venom
  (Envenom/Venom Burst, −atk/def). **Stack/effect visibility DONE**: buff bar "Name xN"; inspect window "Effects:" line w/ stacks.
  **Cure/cancel DONE**: one `Dispel` helper + `SkillEffect.Cancel`; `SkillDef.DispelMask`
  (effect filter, e.g. cure-poison = Poison|Venom), `DispelCount` (random N), `DispelMaxLevel`
  (Rank ≤), `Cancellable` flag (internal counters immune). Demo: healer "Antidote" (cure
  poison/venom), nuker "Dispel Magic" (strip 2 random buffs). **Cancel resist**: each cancelled
  buff rolls a save vs the victim's `CancelResist` (`SkillEffect.BuffCancelResist` /
  `PassiveEffect.CancelResistPct`); tank ult "Indomitable" = +80%. **Absorb shields DONE**:
  `SkillEffect.Shield` + `BuffInstance.ShieldPool` (flat Power + % max HP); `ApplyDamage` soaks
  the pool before HP for all damage types, removes the buff when empty. Demo: tank "Aegis"
  (8% max HP, 15s). **Mana shield + lethal save DONE**: `SkillEffect.ManaShield` (divert % of
  damage to MP at a per-dmg rate) + `LethalSave` (survive one fatal blow → revive %), both in
  `ApplyDamage` after shields. Demos: Magus "Mana Barrier", Bulwark "Last Stand".
  **Taunt + real threat DONE**: mobs keep a threat table (`Entity.Threat`, threat = damage)
  and target the top-threat foe; `SkillEffect.Taunt` spikes threat + locks the mob briefly
  (`TauntLockTicks`); detaunt sheds 90% threat and retargets. Demo: tank "Provoke".
  **Blink + knockback DONE**: `SkillEffect.Blink` (caster → behind target, or away by
  `BlinkRange`) + `SkillEffect.Knockback` (shove target by `KnockbackRange`); `PlaceEntity`
  clamps + regrids. Demos: Phantom "Shadowstep", Trapper "Repelling Shot", Tempest "Phase
  Shift". **STEALTH + TRAPS DONE (2026-07-07):** `SkillDef.GrantsStealth` → `Entity.StealthTicks`
  (invisible to mob AI, sheds current aggro via `DropAggroOn`, broken by any offensive action; demo
  Phantom "Vanish"); `SkillDef.PlacesTrap`/`TrapRadius`/`TrapLifeTicks` → server-only `World.Traps`
  scanned each tick (`TickTraps`/`FireTrap` delivers the skill's damage + contested CC to the first
  intruder; demo Trapper "Snare Trap" = damage + Root). Both ride flag FIELDS, not new SkillEffect bits
  (enum full). **P2 combat primitives COMPLETE** (poison/venom secondaries were already done). (Shield
  floating-text shows pre-absorb damage — cosmetic.)
- [x] **Stats-via-skills: archetype identity leans → data** (2026-07-10, [[stats-via-skills-not-hardcoded]]).
  Base + 2nd-class armor masteries were already data (Phase 2). This pass removed the last hardcoded
  per-archetype IDENTITY switches in `StatCalculator`: rogue/archer **crit + evasion leans** moved into
  the floor passives (Evasion Mastery +20%/+20, Reflexes +15%/+10 — parity). **Removed** (owner call):
  the tank's `level/2` magic-def bonus (his Anti-Magic passive is his magic identity) and the base rogue's
  `50+level` basic-attack interrupt (→ a future 3rd-class discipline passive on the anti-magic rogue).
  Left as an allowed COEFFICIENT: the per-archetype basic-attack multiplier (structural, order-sensitive)
  + the base HP/MP/def level-growth curves. ⚠ Two intentional balance changes to verify in the playtest.
- [x] **1H vs 2H weapon-mastery gating** — done: `Entity.WeaponHands` tracks equipped hands;
  `WeaponMasteryProfile.RequiredHands` gates the bonus (Warrior = 2H only, Tank = 1H only).
- [x] **Toggle-skill mechanic** + **Healer "Combat Stance"** — done: a toggle skill applies
  its self-buff indefinitely (click again / double-click buff to end); the stance trades
  +50% P.Atk for −50% M.Atk. (Numbers untuned. Future: per-tick MP drain for toggles.)
- [x] **Skill reagents/consumables** — done: `SkillDef.ConsumableId`/`ConsumableAmount`; a
  skill with a reagent checks it up front and consumes it on cast completion (refunded on
  interrupt). Empty = casts freely. No skill uses it yet — assign to "ultimate" skills.
- [x] **Admin MODERATION — roles + jail/kick/ban — BUILT 2026-07-17 (0d8fdb0, SmokeTest-verified).** Distinct from the
  DEBUG cheats: these SHIP in release, so they are authorized SERVER-SIDE by the caller's role/IsAdmin, not
  by `#if DEBUG`. Accounts carry a ROLE (Player/Gm/Admin; IsAdmin derives). **JAIL** (per character, timed):
  no chat/whisper, no escape skills (TeleportsToTown), relogin respawns in jail, teleported to a fixed jail
  spot; admin has an un-jail list. **KICK** (per character, timed): booted to login; the account can log in
  but that char can't enter for the set time. **BAN** (per account, timed): no login until it expires.
  Persistence: `Account.Role`/`BannedUntilUtc`, `Character.JailedUntilUtc`/`KickedUntilUtc` (⚠ schema →
  delete game.db). See [[admin-moderation-design]].
- [ ] **Premium class-reset item** — lets a player undo the irreversible class-chain commitment.
- [~] **Client settings panel** — the Settings window gained a **Debug Tuning (admin)** page
  (2026-07-10): live-edit `RateConfig` (exp/sp/drop-chance/drop-amount/gold) + karma
  (base/×consec/×level/−death/−mob) + auto-hunt caps (idle/offline/grace in sec), server-authoritative
  + admin-gated (`DebugConfigDto`, `Request/SetDebugConfig`). Runtime only — bake final values back into
  the code defaults. Those karma/cap consts are now runtime fields. STILL TODO: real player-preference
  settings (e.g. the per-player default party loot mode).

## LATER (bigger systems)

- [>] **3rd-class discipline kits** — 12 disciplines × per-race skill lists. Framework +
  flat stat leans exist; needs the combat primitives layer first, then per-race kits.
  Lightbringer (healer) + Warchanter (buffer, gets a "Prophecy" party buff) are first up.
  ([[discipline-skills-plan]], [[class-tier-design]], [[mage-path-wip]])
- [x] **Crafting & material economy** — BUILT (`docs/Crafting.md` / [[crafting-economy-design]]): mats drop
  from mobs (5 types ↔ 5 professions), refine 5-same+2-cross, finished-item recipes, all 5 professions craft,
  profession persist+choose, boss/elite mat piles. Scaled Common/Unc/Rare DROP gear (Epic set = craft/boss).
  **Polish DONE:** `KnownRecipes` (persisted) unlocks DropOnly A-grade recipes via a dropped recipe BOOK
  (EquipSlot.Box → open to learn; A-grade bosses drop them); L2 mutually-exclusive drop GROUPS (`DropEntry.GroupId`
  — one weighted pick per group; body/weapon copies grouped in StandardDrops). Numbers retune-later.
- [x] **Party / grouping system** — COMPLETE. Server + transport: `Party` (leader + members) in World;
  invite/accept-decline/leave/kick commands+hub+handlers; leader reassigns + auto-disband under 2;
  XP SPLIT among in-range members (level-weighted + size bonus) + kill-quest credit to all in range;
  AoE ally heals/buffs (`PlayersInRadius`) target PARTY members only (solo = self). WPF party WINDOW +
  invite button + invite prompt done last session. **LOOT RULES DONE (2026-07-07):** `LootMode`
  {FindersKeepers, Random, RoundRobin, LeaderOnly} on `Party`; `LootRecipient` routes each item drop
  (RoundRobin cursor / random / leader-if-in-range / killer); boss-mat pile → one recipient; **GOLD
  ALWAYS splits** evenly among in-range members (`AwardGold`, killer keeps remainder) regardless of
  mode; `PartyUpdate` carries `LootMode`; client party panel loot dropdown (leader-editable). New parties
  **default to Random**. A leader's change is a **unanimous VOTE** (`PartyLootVoteCmd`/`PartyLootVoteDto`:
  every other member Agree/Declines; applies only if all agree; decline/timeout/membership-change cancels)
  with an Agree/Decline prompt. The **invite prompt shows the loot rule** the invitee would join under.
  See [[party-loot-modes]].
- [~] **Active mob skills** — caster (Mage-role) mobs cast two generic leveled spells (nuke + jab,
  MP-gated); BOSSES now have data-driven unique kits + phases + adds (see Boss mechanics, `BossCatalog`);
  client cast-bar for mobs done. Still to do: mob buffs/heals/CC for NON-boss mobs (shaman heals, etc.).
- [~] **Leveled MobMastery layer (mobs_passives.csv)** — BUILT (`Game.Shared/MobMasteries.cs`): the
  per-level tables (Weapon/Armor Weight, M.Atk/P.Atk/Max HP/MP/Regen HP/MP/M.Def/P.Def Mods, Pierce/
  Blunt/Bow Resistance) + `MobMasteries.Build(...)` that resolves per-mastery LEVEL picks into a
  `MobMod` (extended with MaxMp/AtkSpeed/HpRegen/MpRegen mults + flat Eva; applied at spawn). Demo:
  obsidian_knight authored via `Build(pierce:10, bow:12, blunt:2)`. STILL TODO: Stun/Fear/status
  resists (with the CC layer), and moving mob picks off `MobMod` onto a mob StatMods fold if desired.
- [x] **Boss mechanics** — DONE. **±10-level rule** (`StatCalculator.RaidLevelGapMult` in `FinalizeDamage`);
  **enrage** timer (`BossTick`: one-time +50% atk / faster-swing rage after ~90s, undone on leash-reset);
  **telegraphed AoE** "Devastating Slam" (`boss_slam`, `TargetMode.EnemiesInRadius`); **visible mob cast-bar**
  (`MobCastInfo` DTO + client rendering). **PER-MOB UNIQUE SKILLS + PHASES + ADDS DONE (2026-07-07):**
  data-driven `BossCatalog` (`BossProfile` keyed by mob-template id = a `BossSkillEntry[]` kit with HP-gated
  entries + a `BossPhase[]` HP-threshold script). `BossTick` now runs the enrage timer, the phase script
  (`AdvanceBossPhases` → announce / `EnrageBoss` / `SummonAdds`) and a skill rotation (`SelectBossSkill` picks
  the first ready HP-gated skill with a foe in radius; reuse via per-skill `CooldownTicks`/`SkillCooldowns`).
  `SummonAdds` spawns Normal-rank, no-zone (no respawn) minions engaged on the boss's target via a refactored
  `BuildMob` (extracted from `SpawnOneInZone`; also used by zone spawns). New phase skill **"Thorn Nova"**
  (`boss_thorn_nova`, magic AoE + slow). Demo boss: Valley Treant Lord (slam → 50% enrage+2 bogwood
  adds+Thorn Nova → 25% shout). `ResetMob` re-arms phases + clears reuse. See [[boss-mechanics]].
  **Deferred:** boss buffs/heals, multi-stage HP-bar phases, unique skills for the other bosses.
- [ ] **Boss helper mobs** (owner idea, 2026-07-08, deferred) — dedicated support monsters that assist a
  boss: dealing extra damage, HEALING the boss, or buffing it. A separate system from the current
  `SummonAdds` (which spawns plain minions) — needs mob heal/buff casting (the non-boss "mob buffs/heals/CC"
  line under Active mob skills). Pull it in when that mob-support layer is built.
- [~] **Auto-hunt / idle farming** (owner request, 2026-07-08). **PHASE 1 (online idle) BUILT:**
  server-driven `AutoPilot` (per-tick, before UpdateAction): auto-target nearest hostile
  (`AcquireAutoTarget`) → engage (reuses `UpdateAutoAttack` chase/basic) → `TryAutoSkill` queues the
  first eligible auto-skill (known/enabled/off base-cd+extra-delay/MP-ok; condition inferred —
  `ClassifyAuto`: attack→on cd, buff→if key missing on self, debuff→if target lacks it, heal→self<70%).
  Auto-potions at HP/MP % (always active; `BestHealPotion`; MP pots reserved — no items yet) + keep buff
  potions up (empty list = all in bag). Per-char config persisted (`AutoHuntJson`), edited in a WPF
  **Auto-Hunt window** (enable, HP/MP %, keep-buff-potions, per-skill enable + reuse-seconds). MP/s HUD
  (`AutoHuntStatus`, after cost/CD buffs) pushed each regen tick. Loot/XP = manual (owner).
  **PHASE 2 (true offline) BUILT:** offline = the same AutoPilot with the connection dropped (SendTo
  no-ops → all UI skipped). Disconnect with auto on keeps the char in the world (`IsOfflineFarming`,
  visible/attackable, mobs aggro it); reconnect re-attaches. Runtime caps: **idle 8h / offline 2h**
  (constants; purchasable 12h/4h a hook); cap or **death** stops it (offline = deferred logout via
  `_endOfflineQueue`); idle cap locks re-enable until re-log. Design: `docs/AutoHunt.md`,
  [[auto-hunt-design]]. Debug `/testcaps` shrinks caps+grace to seconds (2026-07-10). **ROAMING BUILT
  (2026-07-10):** farm-range radius (200–2000) + roaming vs static-spot (soft chase, return-to-centre) +
  target-rank filter (mobs/elites/bosses) + basic-attack-as-an-auto-skill (`AutoHuntIds.BasicAttack`;
  mages skills-only, fighters melee). **Thread FINISHED (2026-07-10):** roam now BOUNDED (wanders within
  the farm circle around home, skips safe zones/roads) + auto-skill **priority reorder** (▲/▼ in the
  window). Deferred (need other systems): purchasable cap extensions, PvP no-counter, full common-skills
  bar (sit/walk/dance), auto-heal party, offline-gains summary.
- [x] **Disconnect / exit / combat + Return** (owner spec, 2026-07-09; [[disconnect-exit-system]]).
  Combat state (30s decay off the last damage). Disconnect FATE (`HandleLeave`): offline-farm ONLY when
  genuinely offline-farming (auto on, not locked — the 2h cap's domain); everyone else alive → a 180s
  **link-dead grace** ("⚠ Disconnected" head title, stays in party, no offline-cap drain, reconnect
  resumes) that is combat-aware (a mid-combat drop keeps defending, timer PAUSED until combat ends —
  anti-combat-log), else normal removal. **Combat-gated Exit** (`LogoutCmd`) +
  a **Go Offline** button. New `SkillDef` flags FixedCast / FixedCooldown / FragileCast / TeleportsToTown;
  universal auto-granted **Return** skill (30s/5min, fragile) + **Scroll of Return** (Apothecary 500g,
  10s) + **Ultimate** scroll (near-instant, not sold). `ItemDef.UseCastSkillId` (double-click a
  consumable → cast). Deferred: purchasable cap extensions, PvP no-counter, ultimate-scroll vendor.
- [ ] **Buffer = "Enchanter" + full-buff NPC to 75** — owner direction ([[buffer-enchanter-design]]):
  ONE buffer class holds ALL buffs (race-flavored); add **dances/songs** (extra atk/cast mults) to the NPC
  buffer later; a **full-buff NPC buffer up to lvl 75** is the SOLO stopgap. High-tier solo being hard is
  INTENDED — buffs/party close the gap, don't nerf the mob curve.
- [ ] **Position bonuses** — backstab / flanking damage (hook reserved).
- [~] **PvP + flag/karma/PK** — BUILT 2026-07-10 ([[pvp-system]]): PvP-enable + counter-attack toggles;
  **L2 flag system** — attacking flags you purple, killing an innocent → PK (red name + persisted karma,
  `200·1.1^consec·1.2^lvlDiff`), killing a flagged/red → PvP count; each death decays karma (−200) and
  clears the red at 0; red/purple freely attackable, innocent needs the opt-in; safe-zone gated. Client
  colours names (red/purple/white) + shows karma. Damage rides the existing `FinalizeDamage` pvp path.
  each death −200 karma and **each mob kill −20** (a PK grinds karma off — take a camper's spot + farm);
  all karma values are tunable consts. ⚠ Persisted karma columns → delete game.db. **Deferred (owner):**
  PK death item-drop, PK town-respawn ban + **town guards / prevention for future PvP/PK-FREE ZONES**,
  PvP/PvE damage MULTIPLIERS still 1.0, duel/consent mode.
- [ ] **Perfect / excellent block** — shield block tiers above the current flat block.
- [ ] **Class-vs-class balance matrix** (buffed) + damage-K tuning once all kits exist.
  ([[class-race-identity]])

## EVENTUALLY (long-term / large)

- [~] **The real client** — 2.5D Unity (no Z axis; server stays 2D), reusing `Game.Shared`
  + `NetworkChannel`. The WPF app is only a test harness. ([[client-3d-and-los-design]])
  **STARTED (2026-07-03):** `Game.Shared` now multi-targets `net8.0;netstandard2.1` (+ IsExternalInit
  polyfill) so Unity can consume it. A vertical slice lives in `Game.Client.Unity/` (scripts + README):
  ported `NetworkChannel`, `GameBoot` (auto-login→enter world), `EntityManager`/`EntityView`
  (billboard quads + interpolation), `CameraRig` (steep pitch now → lower to ~50 for 2.5D), touch
  move/attack, main-thread dispatcher. Owner builds the Unity project + Android per the README. NEXT:
  UI (target frame/skill bar/cast bars), then swap billboards for animated 3D models (visual-only).
- [ ] **Line of sight** — server-side LoS using STATIC occluder data (not entities), for
  the new client. ([[client-3d-and-los-design]])
- [~] **Network payload optimization (owner asked 2026-07-17).** MessagePack + delta snapshots.
  1. [x] **Delta / dirty-flag snapshots — BUILT (server SmokeTest-verified; client needs the playtest).**
     `WorldSnapshot` (full every tick) → `SnapshotDelta(Spawns full, Updates lean, Despawns)`; server diffs
     per-connection last-sent state, client merges lean onto the cached DTO and runs the same apply path.
     Legacy full-snapshot path kept but unused. Static fields no longer re-sent 10×/s.
  2. **MessagePack SignalR protocol** (`.AddMessagePackProtocol()` both sides, ~1 line each) — binary,
     drops repeated JSON field names, compact numbers → ~40-60% smaller per full snapshot, compounds with
     deltas. ⚠ **Two gotchas:** (a) our DTOs are positional records with `init` setters — SignalR's
     contractless MessagePack resolver usually handles them but may need `[SerializationConstructor]`;
     (b) **Unity/IL2CPP** — MessagePack's dynamic resolver does NOT work under AOT; needs the `mpc`
     codegen run at build time. JSON just works there, so this is the moment to solve the Unity AOT story.
  **Why deferred:** no measured bandwidth/CPU problem (localhost dev), the protocol is still churning
  (DTO fields added most sessions), and JSON stays readable on the wire (SmokeTest + inspection). It's a
  one-line swap you can make LATE once the protocol stabilizes and you've measured. Sits behind the
  `NetworkChannel` seam so the WPF harness + future Unity client share it. ([[client-3d-and-los-design]])
- [ ] **INSTANCES & DUNGEONS** — full owner spec captured 2026-07-14 in **`docs/Instances.md`**.
  Read that before touching this. Two features, very different sizes — don't conflate them.

  **DUNGEONS = small, mostly DATA.** A dungeon IS a `SpawnZone`: harder mobs, normal respawn, can hold a
  boss, "just not on the main map". We already have zones/elites/bosses/drops/teleports, and the 48000²
  world only uses the middle for the 7-town ring — so it's a zone authored outside the ring plus a
  teleport entrance. ("Map layers / under-over ground" is a CLIENT concern; the server is 2D and doesn't
  care.) **Do this one first — value now, near-zero risk.**

  **INSTANCES = a real system.** Owner's rules: one attempt per player per DAY (reset 00:00 server time;
  debug every 10 min), consumed on START whether you finish or not; per-instance open window + day-of-week
  mask (default 00:00:00-23:59:59); one NPC per CATEGORY; party leader only, party ≥4, every member in the
  level band and unspent; level bands 20-29 … 76-85; rooms of banded ELITE mobs that never respawn + a
  mid-band boss; **trash pays NOTHING — only the boss, with a custom table and far more exp than a field
  boss**; 1h limit; death → respawn at the entrance NPC and you may re-enter; leaving the party loses the
  attempt; one active instance per player; **no subclass swapping while inside**.

  **The architectural decision** (everything hangs off it): `World` has ONE flat entity dict and ONE grid
  — there is no "your party's private copy of a room". But visibility is radius-based (`ViewRange` 3000)
  over a spatial grid, so **two parties 20,000 units apart already cannot see each other**. So an instance
  = an **off-map COORDINATE SLAB per running instance**, which reuses the whole existing engine free
  (spawning, grid, combat, threat, loot, death). A real per-instance dict+grid is cleaner on paper and
  touches World / grid / snapshots / broadcast / teleport / party all at once — **not worth it**.

  ⚠ **`GameClock` IS NOT SERVER TIME** — it is *in-game* time at ×6. Daily resets MUST use real wall-clock
  time, or the attempt resets every 4 real hours. Store the last-attempt DATE per character and *compare*;
  then reset needs no scheduled job and survives restarts for free.

  ⚠ **Open questions for the owner are listed at the bottom of `docs/Instances.md`** — the load-bearing one
  is whether the daily attempt is GLOBAL (one instance of any kind per day) or PER-INSTANCE. The
  level-29→30 rule implies GLOBAL. It changes the persisted data model, so confirm before building.
- [ ] **Castles + vault** — consumes the reserved `VendorBuyTaxRate` hook; siege loop.
- [ ] **4th class tier** — the top of the 4-tier tree. ([[class-tier-design]])

## BLOCKED / WAITING ON OWNER

- [>] **Lightbringer + Warchanter CSVs** — 3rd-class kits @40 need owner's skill numbers.
- [>] **Two real starter armor SETS** — owner to provide; current newbie light/robe sets are
  placeholders. ([[item-properties-boxes]])
- [>] **Newbie items via quests** — give the starter weapon/armor/jewel boxes through lvl
  6/8/10 quests (owner's plan).

## DROPPED (decided NOT to build — don't re-add)

- ~~Magic-resist layer~~ — magic mitigation is ONLY mDef (divisor) + the magic-fail/fizzle
  floor. "mRes" in owner CSVs = the fizzle floor. No flat magic-damage-reduction stat.
- ~~Soulshots / spiritshots~~ — the leveled **Attack-training** passive is the permanent
  replacement; there is no shot consumable system.
