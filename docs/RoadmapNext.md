# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated **2026-08-05 (after playtest-18)**.
Full history: [CHANGELOG.md](CHANGELOG.md). The checklists: [testing/TestChecklist.Unity.md](testing/TestChecklist.Unity.md)
(the phone) and [testing/TestChecklist.md](testing/TestChecklist.md) (server-side; its client steps
predate the WPF harness being dropped in 0.42.8 — read them as "on the phone").

## Where things stand

**Playtest-17 (2026-08-03, server 0.45.0) cleared the backlog.** Six versions of unplayed work — §36
mob regen, §38 the account warehouse, §39 repeatable quests, §40 the quest window, §41 the mob cast bar,
§42 titles + chat tabs, §43 accuracy + attributes + the scroll windows — went through in **one** pass.
**84 items verified, plus 22 of the playtest-11 findings finally closed.** Not one of them was a broken
system; the four `[~]`s and the handful of `[!]`s are edge cases (see [testing/Playtest-17.md](testing/Playtest-17.md)
and §44 of the Unity checklist).

**So the machinery is done arguing and the game is now what's missing.** What he sent back is not a bug
list, it is a *game* list: inventory that cannot be navigated (no filters, no tabs, quest items sold by
accident), an enchant/scroll economy he has now fully specified, drop faucets he measured at level 23 and
wants cut by 10-20×, no way to start offline farming at all, and — behind it all — **crafting, which is
now the single blocker for every item above Epic**. Nothing above the 🔴 line is engine work.

**Playtest-15 (0.34.3) was the economy verdict; playtest-16 (0.42.0) was the polish verdict.** Fifteen
versions ran between them and the shape of the work changed again: nothing in playtest-16 was a crash,
a corruption or a system that didn't exist. Seventeen checklist items simply passed. Four passed *and
still failed the reader* — a window that showed the work but never the answer — and both real bugs were
gesture/refresh defects in the Unity client, not in the simulation.

So the job now is **the reader and the feel**, not the machinery. The one exception was found by playing
rather than by reading: mob HP regen was on the *player's* CON curve, which at level 90 gave a mob its
whole bar back every 5.6 seconds. That is fixed (0.42.3) and has no level term left in it.

**Shipped since the last update (0.28.91 → 0.42.3)** — see the CHANGELOG for each:

| | |
|---|---|
| **World** | the overworld is a generated PLAN (4-level camps, named gates, managing cities) · five cities, every town fully serviced · aggression authored per field · per-mob quest spawners |
| **Items & economy** | the six-quality ladder from F to S on one series · sets need four pieces of the same quality · rarity colour everywhere · **the price ladder** (sell derives from buy ÷ 10) · **the drop side** (grade-locked slot-family groups, `/droprate` global + per-group + per-item) · jewel slots |
| **Buffs** | the ladder: 14 families × ranks, 24 potions + 48 scrolls · the cleric buffs singles, the Warchanter owns the groups and Harmony (party-wide) · **a group is ONE buff that outranks and eats its singles** (0.42.0, reversing 0.36-0.41) · buff scrolls are actually consumed now |
| **Autopilot** | priority groups, cyclic order, heal threshold, assist-leader · retaliation · nothing walks you into melee unless you commanded it |
| **UI** | cooldown countdown on the bar · passives and masteries state their numbers (`SkillText`, shared by both clients) · character delete · drop tree with per-row % · consumable counts · set effects · one confirmation at a vendor |
| **Admin** | the debug menu works in release builds · the class change picks a **discipline**, not just the 2nd class · live tuning rows for the two mob-regen rates |

## 🔴 NOW — the next things

1. ~~**Publish 0.45.0 and play §36 + §40-43**~~ — **DONE, 2026-08-03.** See the paragraph above; the
   queue it produced is [testing/Playtest-17.md](testing/Playtest-17.md) / §44. Still-unreached items
   are listed at the bottom of that file (§37 partial-stack trading is top of them — the duo-icon rig
   makes it testable without a bot now).
2. **The playtest-17 batch, in this order** — it splits cleanly into four passes and they are ordered by
   how much of his play time they give back:
   1. ✅ **The defects that block play — ALL DONE, 0.46.0, unplayed.** `B6` text boxes edit instead of
      wiping · `C12` `/offline` + a Menu button + the remaining time on the character-select row ·
      `B1` the auto-on marks are per CHARACTER and un-slotting clears them · `B5` the farm timer really
      *was* being handed back (a per-ACCOUNT daily balance now, not a session stopwatch) · `B7` an
      out-of-range party member can be targeted, with the frame drawn from the roster · `C18` **undo a
      bin-delete for free**, from Menu → Restore, never behind a vendor. **Needs an APK + a play pass.**
   2. ✅ **Inventory hygiene — ALL DONE 2026-08-05, unplayed.** `B4` every disposal path now refuses a
      quest token *before* the tap (the holes were the private bank, the trade table and the Bin button)
      · `C8` **one** classifier + name ordering shared by bag, vendor and keeper — All/Gear/Use/Mats,
      plus Quest in the bag · `C7` gatekeeper Zones/Cities tabs off the `Group` the server already sends
      · `C5` an NPC lists only the quests IT gave (the in-progress list was every quest you carried, at
      every NPC) · `C6` names only, `C11` compare + details as one window that grows a second column
      (`B2` dies with it) · `C10` jewels rank by **delivered M.Def**, enchant included, not rarity.
      **Needs an APK + a play pass**, together with the 🔴 batch above. Also picked up `G6` from
      playtest-18 (warehouse slots used/total, red when full).
   3. 🟡 **The faucet + the scroll economy** — `E1`/`E2` (÷20 return scrolls, ÷10 heal potions, rarity
      bands by level) are one-line rate changes and he measured them himself, so do them first; `E3` is
      the real work (**no buff-scroll drops at all**, potions sell at 0, two rarities, one max-rung
      scroll per buff, and an **Apothecary selection box: 250k for a pick of 10** — deliberately priced
      so a live buffer stays the better deal).
   4. 🔵 **The enchant rework** `D1` — three scroll TYPES (breaks / −1 / **safe**) with the RARITY
      choosing the grade E→S, drop bands one grade below the attribute scrolls, safe scrolls from bosses
      only. Plus `D2` `/enchant <value>` and every scroll in the admin menu.
   ✅ **`B3` is CLOSED** — the list went to him as [testing/Skills-Not-In-CSVs.md](testing/Skills-Not-In-CSVs.md)
   and he answered it on 2026-08-04 (playtest-18 `G1`, item 3 below). The deletion is unblocked.
3. 🆕 **The playtest-18 queue** (2026-08-04, [testing/Playtest-18.md](testing/Playtest-18.md), §45) —
   his second 0.45.0 pass, and mostly *answers* rather than bugs:
   - 🔴 **`G1` unblocks the skill deletion** — he named them: the four identity-floor passives,
     `archer_*_mastery`, `dispel_magic`, and the God class + skills. ⚠ `class_balance_*` he skipped and
     `lb_*`/`wc_*` he asked about instead (**`G2`, owed back to him**). Heavy Draw = remove the **Rogue
     @24 grant** of `power_shot`, never the definition — three level-40 discipline skills are renames.
   - 🔴 Three defects: **`Q1`** quest tracking is a client-only in-memory list and dies with the app
     (must be per CHARACTER, server-side with the quest log) · **`G7`** a hotbar consumable at 0 count is
     *disabled*, which also kills the gesture that would remove it — draw a permanent 100 % cooldown ·
     ~~**`V2`** the sell fraction~~ ✅ **DONE 2026-08-05** — his 0.8 was a misread (he sold a Robe, not
     the gloves). Real fix from his three-character farm: gear drop groups ×1/3 → **×0.025** and
     `GearSellDivisor` 25 → **10**. 4.06kk → 1.23kk over the same farm. See `EconomyRework.md` §4a.
   - `G5` the Dash potion and Sprint become ONE speed family (six rungs, potion E1/E2/E4/E5, Sprint
     E3/E6) — pure authoring on the existing family+Rank machinery · `Q2`-`Q5` tracker and Active-tab
     polish (same "full text only in Details" rule as `C6`) · `F1` leaving auto-farm must not drop the
     target · `V1` a `[QSell On/Off]` toggle · `G4` a save-login checkbox · `G6` warehouse slots
     used/total.
   - ❓ `G3` **mobs without inflated STR/CON, carrying real gear and running the PLAYER formulas** — a
     genuine design change (mobs move off `MobBaseStats` onto `RecomputeDerived` + equipment). Needs a
     design pass and his go. Filed with crafting: **trash disassembles into mats** instead of gold.
4. 🆕 **Crit damage, blows and `[Double]`** (2026-08-05 ruling, spec:
   [design/CritBlowAndDouble.md](design/CritBlowAndDouble.md)) — five items, one area, **not started**:
   the rogue armor mastery ignores the CSV's `with all` clause · the rogue weapon mastery's crit damage
   is swapped between levels 24 and 28 · every CSV's `crit dmg +N` was read as a *percentage* and must
   become **flat attack inside the crit** (rogue **and** warrior) · **blows must scale off crit damage**,
   which they currently ignore entirely, and `[Double]` becomes a pure **ATK** curve capped at 25%
   (dropping `max(DEX, ATK)` and the 30% cap) · `[Double]` on a buff/debuff doubles its duration.
   ⚠ This makes the `crit dmg +N` rungs the rogue's whole scaling curve — **BalanceMatrix before and
   after**.
5. ~~**Regen from gear vs regen from level**~~ — **answered and built in 0.45.0.** Gear regen is a
   PERCENT roll now (rings, 1-5% by grade) rather than a flat MP/s that dominated the level curve at
   every level. The flat types stay in the enum for pre-0.45 saves and nothing rolls them.
6. **CRAFTING is now the top content blocker** — his words: *"we need the craft — professions, window,
   etc .. now even in admin the only mythic are the set, everything else is epic rarity."* Every design
   is written ([design/Crafting.md](design/Crafting.md), [design/GearLadderAndCrafting.md](design/GearLadderAndCrafting.md));
   nothing above Epic can be reached in play without it. It outranks the deferred combat-depth work and
   is second only to the 3rd/4th class kits, which are still blocked on his CSVs.
   ~~**Watch endgame mob crit after the DEX change.**~~ — checked in playtest-17 (§43d) and mobs do
   **not** feel like paper; the flat-30 DEX stays. If a specific creature should be nasty it gets a
   MobMod passive, not the old `10 + level` curve back.

## 🟡 OPEN — carried forward, nothing blocking them

**Quests**
- ~~Repeatable quests~~ — **built 0.42.9.** `QuestDef.Repeatable` covers all three shapes the owner
  named; gathering contracts carry `QuestGather` lines whose `RewardModifier` **is** his
  `QuestItemRewardModifier`, paying a fraction of the creature's own kill exp+gold per token. A
  Huntmaster in every city, ~+25-35% on the hour you farm one. See the CHANGELOG.
- ~~The 3-tab quest window and the per-quest detail window~~ — **built 0.43.0.** Active / Available /
  Completed, Details on every row, Accept+Decline moved onto the detail page and out of the NPC's wall
  of text. The middle tab is *Available* rather than "unavailable" — it holds both what you can take and
  what is shut, with the reason. The promised protocol bump was spent here (**9**): the gather counts
  are structured fields now (`QuestEntry`/`QuestStepDto`/`QuestGatherDto`). `MinAcceptedProtocol` stays
  8, so an installed 0.42.x APK still connects. Checklist §40 — **unplayed, needs an APK.**

**World**
- ~~Mob cast bar~~ — **built 0.43.1**, and the "believed built" was half right: the SERVER had been
  broadcasting `MobCastInfo` to everyone nearby since bosses shipped, and no client ever subscribed.
  The nameplate now draws an amber bar + the spell's name over the mob's head. Also fixed: `Kill`
  cleared the cast by hand instead of through `CancelCast`, so a caster killed mid-spell left its bar
  hanging over the corpse.

**Items**
- ~~The attribute system~~ — **rebuilt 0.45.0** on the owner's spec. One attribute per item, **nothing
  drops with one** (a scroll is the only source, so a scroll is never wasted on trash), armor carries
  none at all (its identity is its set), and item QUALITY no longer gates or scales it — the table is
  absolute per GRADE (D/C/B/A/S = `ItemLevel` 40/52/61/76/80). Six scrolls, each locked to one band:
  Common/Uncommon/Rare for D-C-B, **Epic**/Legendary for A, **Mythic** for S (always MAX). No lock,
  no guaranteed top roll outside S. Checklist §43.
- ~~Enchant + attribute scroll UI on the phone~~ — **built 0.45.0**, and the reason they "didn't work"
  was that the client had no window at all: the hub methods had existed since the enchant system
  shipped and nothing ever called them. Both now run one flow — tap the scroll → **Use** → a list of
  legal targets → confirm — filtered by `AttributeSystem`, the same code the server validates with.

**UI / conveniences**
- 🆕 **The playtest-17 UI queue** (2026-08-03) — the whole C-list in
  [testing/Playtest-17.md](testing/Playtest-17.md): bag/vendor/keeper filters, gatekeeper tabs, NPC quest
  scoping, the merged compare+details window, a [Speak] button on NPCs, timed-item countdowns with
  colour, auto-on for buff potions/scrolls, a **[Combat] chat tab in its own window**, title colours and
  fonts, admin/moderator titles. Individually small, collectively the difference between a systems demo
  and a game — and the text-box focus bug (`B6`) is a prerequisite for two of them.
- ~~Chat tabs~~ — **built 0.44.0**, and with them the colours and tags: the phone's Log window is now a
  Chat window with **All / Local / World / PM / System**, world gold `[W]`, whispers violet `[PM]`,
  system green, local white, plus a **Reply** that fills in `/w <name> `. The tabs are a FILTER over
  the one log buffer, so "All" costs nothing and the append-only draw (the 0.28.77 lag fix) survives.
  This was the oldest open item in the file.
- ~~Target visual on the mob itself~~ — **built 0.43.1.** Two blue circles flanking the target's name,
  as real UI elements created on target and destroyed when it clears, drawn from a runtime-generated
  circle sprite and placed from the name's rendered width. The owner rejected both earlier shapes: a
  ground ring, then a text bullet ("no one wants font circles").
- ~~Wearable titles~~ — **built 0.44.0.** Rank → **Titles** picks one, and it draws as a gold line above
  your name in the world. A title is **held while you are rank 1**, not earned and kept: the server
  re-reads the boards every 5 minutes, only your CHOICE is persisted (`TitleCategory`), and a title you
  win back returns on its own. Admin characters are excluded from the boards, so they hold none.
  ⚠ Schema change — **delete `game.db`**. Checklist §42.
- ~~Every non-admin command as an ACTION~~ — **closed 0.44.0.** The list was already complete except
  for the one command that needs typed text: **Whisper** is now an action that fills the command box
  with `/w <target> ` and hands you the caret. (The roadmap's "friend/party/sit/attack/assist have
  nowhere to live" was stale — they have been in the Actions tab since 2026-07-24.)

**Release hygiene**
- ✅ **Two app icons on the phone — KEPT ON PURPOSE as the duo-testing rig; remove before the store.**
  The second entry runs a fully independent client: 2026-08-02 the owner logged in as admin on one and
  test1 on the other, both connected, saw each other in the world and **formed a working party**. This is
  now how party/duo is tested — no second device, no bot needed. 🔴 **It must be gone for a store
  release.** Half the cause is ours: `Assets/Plugins/Android/AndroidManifest.xml` declares BOTH
  `UnityPlayerActivity` and `UnityPlayerGameActivity` with a LAUNCHER intent-filter (Unity's template
  ships both and expects the unused one deleted); our entry is GameActivity, so `UnityPlayerActivity`
  merges in `enabled="false"` yet still shows an icon. ⚠ But that block alone can't produce two *running*
  clients — same package, one process, `launchMode="singleTask"` — so a Samsung profile-level clone
  (Dual Messenger / Secure Folder) is likely also in play, and **that half is a phone setting no manifest
  edit removes.** Verify on the device (`adb shell pm list users`) at store time.

**Combat depth** — ⏸ **DEFERRED at the owner's request (2026-08-01)**, not dropped: perfect/excellent
block, position bonuses (hook reserved), PvP/PvE damage multipliers (still 1.0). ⚠ Magic-resist as a
stat and per-hit damage consumables are **dropped, not pending** (offence comes from the held War/Spell
Rune) — see the bottom of [Roadmap.md](Roadmap.md).

**Presentation** — the owner's own words, still true: *"no sounds, a bit woody, no good visuals."* Not a
scheduled work item, and now the loudest remaining gap.

## 🔗 Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs. Still the biggest single content
  unlock; the discipline designs are written ([design/Disciplines.md](design/Disciplines.md)).
- **Instances** — design done ([design/Instances.md](design/Instances.md)), owner HOLDING. Open decision:
  daily attempts GLOBAL vs PER-INSTANCE.
- **Command channel** — gated on clans/alliances.
- **Castles + vault** — needs siege design; consumes the `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — needs bosses/instances producing points.
- **World expansion to 1kk+** — the 0.33.0 re-layout was the first real step.

## ⏸ Deferred (explicit owner hold)
- **MP potions** — held until the 3rd-class kits decide the MP economy.
- **SP bottles** — 1e9 SP → one bottle; also what keeps `SkillPoints` an `int` honest.
- **Bot-prevention CAPTCHA** — revisit with behavioural detection + charisma/block signals.
- **Recipe drops below A grade** — no recipe item exists under A (recipes below 76 are learned by level).
  Add the same way A+ was added, when there's a reason to.

## 📐 Designs written, not built
[RarityLadder.md](design/RarityLadder.md) (superseded in practice by the shipped six-quality ladder) ·
[GearLadderAndCrafting.md](design/GearLadderAndCrafting.md) · [Crafting.md](design/Crafting.md) ·
[Instances.md](design/Instances.md) · [DamageModel.md](design/DamageModel.md) (awaiting the owner's
Option A/B pick) · [StatMods.md](design/StatMods.md) (phases 1-2 done, 3-6 pending).

---

## Two rules this file keeps forgetting to state

- **Measure, don't derive.** `tools/BalanceMatrix` builds real `Entity` objects with real gear and runs
  the real formulas — including the economy tables. Every hand-derived balance number in this project's
  history has been wrong at least once.
- **The SmokeTest proves the server, not the game.** `tools/SmokeTest` covers login, persistence, the
  skill bar, subclasses and buffs. It does **not** cover mobs, combat or anything on screen — so a green
  run says nothing about a combat or UI batch.
