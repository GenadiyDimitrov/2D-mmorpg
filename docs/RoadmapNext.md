# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated **2026-08-01 (0.42.3, after playtest-16)**.
Full history: [CHANGELOG.md](CHANGELOG.md). The checklists: [testing/TestChecklist.Unity.md](testing/TestChecklist.Unity.md)
(the phone) and [testing/TestChecklist.md](testing/TestChecklist.md) (server-side; its client steps
predate the WPF harness being dropped in 0.42.8 — read them as "on the phone").

## Where things stand

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
| **Items & economy** | the six-quality ladder from F to S on one series · sets need four pieces of the same quality · rarity colour everywhere · **the price ladder** (sell derives from buy ÷ 25) · **the drop side** (grade-locked slot-family groups, `/droprate` global + per-group + per-item) · jewel slots |
| **Buffs** | the ladder: 14 families × ranks, 24 potions + 48 scrolls · the cleric buffs singles, the Warchanter owns the groups and Harmony (party-wide) · **a group is ONE buff that outranks and eats its singles** (0.42.0, reversing 0.36-0.41) · buff scrolls are actually consumed now |
| **Autopilot** | priority groups, cyclic order, heal threshold, assist-leader · retaliation · nothing walks you into melee unless you commanded it |
| **UI** | cooldown countdown on the bar · passives and masteries state their numbers (`SkillText`, shared by both clients) · character delete · drop tree with per-row % · consumable counts · set effects · one confirmation at a vendor |
| **Admin** | the debug menu works in release builds · the class change picks a **discipline**, not just the 2nd class · live tuning rows for the two mob-regen rates |

## 🔴 NOW — the next three things

1. **Publish 0.42.3 and play checklist §36.** `pwsh tools/publish.ps1` → `builds/Game.Server-0.42.3.zip`
   + `builds/L2Clone-0.42.3.apk`. ⚠ `dotnet build` **before** the Unity build or the APK ships a stale
   version stamp. **No `game.db` reset needed** (no schema change since 0.42.0 — that one did need it).
   §36 is: the buff popup · nothing out-heals you · the 20s idle window · the ledger surviving a
   disengage · **the boss phase script must not replay** · safe-zone kiting · the two tuning rows.
   ⚠ (Was: "the Unity scripts are never compile-verified until the APK build." No longer true —
   `cd Game.Client.Unity && dotnet build Assembly-CSharp.csproj` type-checks them in ~18s. Do that
   before every APK.)
2. **Regen from gear vs regen from level — the owner's call, not built.** `AttributeType.MpRegen` rolls
   **flat** MP/s scaled by GRADE `(1+g, 3+g·2)`, while the level term is nearly flat (2.8 → 9.2 across
   levels 1-90). So gear dominates regen at *every* level; a level-35 subclass wearing the main's
   level-90 gear (28 MP/s on a ~4.9/s base) is only what made it visible. Options: make the roll a % of
   base, or scale the base harder with level.

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

**UI / conveniences**
- **Chat tabs** on the phone (colours + tags only ever shipped in the WPF harness, deleted in 0.42.8 —
  the reference implementation is `MainWindow.xaml.cs` at commit `f33ed0e`; oldest open item in the file).
- ~~Target visual on the mob itself~~ — **built 0.43.1.** A blue dot on each side of the target's name
  (`• Foxhound •`), at the quest marker's size and weight. The owner's call over the ground ring that
  was built first. ⚠ A **bullet**, U+2022 — the TMP atlas is static, so `●` and every emoji draw as a
  hollow box.
- **Wearable titles** — the leaderboard title over the head / by the name.
- **Every non-admin command as an ACTION** in the Skills window's Actions tab. Block/like/unblock landed
  there; friend/party/sit/attack/assist still have nowhere to live.

**Combat depth** — perfect/excellent block, position bonuses (hook reserved), PvP/PvE damage
multipliers (still 1.0). ⚠ Magic-resist as a stat and per-hit damage consumables are **dropped, not
pending** (offence comes from the held War/Spell Rune) — see the
bottom of [Roadmap.md](Roadmap.md).

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
