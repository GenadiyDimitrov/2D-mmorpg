# Roadmap — compact view (what's left, what depends on what)

A one-screen digest of [Roadmap.md](Roadmap.md). Updated **2026-07-29 (0.28.91, after playtest-13)**.
Full history: [CHANGELOG.md](CHANGELOG.md). The raw playtest report: [testing/Playtest-13.md](testing/Playtest-13.md).

## Where things stand
**Playtest-13 is the first report where the verdict was "a game I enjoyed."** Three sessions —
elf mage to ~15, marksman to 24, champion to 28 — including one with **the server running on the
phone itself** (Termux + proot-ubuntu, .NET 10). Nothing crashed, the DB created itself, quests
were completed. The remaining complaint is presentation ("still plain — no sounds, a bit woody,
no good visuals") plus a long list of specific defects.

That changes the shape of the work again. Parity is done, "make it a game" is largely done; the
job now is **make it hold together** — stacking, state that survives a relog, screens you can read,
and quests that explain themselves.

**Shipped since the last update (0.28.67 → 0.28.91):** exp/party/drop rework · damage ledger ·
weapon-based multiplicative M.Atk · the low-grade gear fills + named tier sets · **warehouse**
(server + UI + its own Keeper NPC) · **blueprint crafting** · **block list** · **charisma/reputation**
+ moderation drains · **buy-back** (server + UI). Tier-1 and tier-2 of playtest-11 are all closed.

---

## 🔴 TIER 1 — playtest-13 bugs (do these first)

These break or corrupt state; several are one-line-class bugs with an outsized feel.

1. **Buffs are wiped by relog / class change / "some other change."** They must end only on
   expiry, dispel, cancel or subclass swap. Two halves: the server must persist active effects
   across a logout, and the client must stop dropping its list. The same defect's other face —
   **the previous character's buffs still showing after switching character on one account** —
   is the client never clearing the buff list on character switch.
2. **SP does not update after buying a skill** — stale everywhere including the stats window,
   correct only after a relog.
3. **Char-select is stale** — level not refreshed, and the class TEXT never changes (`admin`
   still reads *Human Mage*; marksman/champion still read *Human Fighter*). Related: at level 7
   the Learn list stayed locked until a relog (it worked at 14).
4. **Crafting materials do not stack** — sold one at a time with no numpad, one warehouse row per
   unit. Scrolls and potions behave, so this is a per-item stackable flag, not the UI.
5. **Rankings did not update.**
6. **MP regen stops during auto-farm** and resumes when auto-farm is stopped (suspected to start
   after a death/respawn, unconfirmed).
7. **Quest-giver dialog does not refresh on accept** — you must re-talk to the NPC to be told
   what to kill.
8. **Buff cancel must become press-and-hold.** Double-click is unusable on a phone: the pop-up
   eats the taps and spamming cancels the neighbouring buffs.

## 🟠 TIER 2 — UI + server hygiene

- Item details: the Atk row hides under the title bar on first open, correct on reopen.
- Mob-info window is clipped — the box reads as centre-aligned with its top half off-screen
  (the weapon window has the same bug, visible because it has three rows).
- **Quiet the EF/SQL console logging** — it floods the server console.
- EF warning `10103` — `First`/`FirstOrDefault` without `OrderBy`.
- Auto-farm "keep position": the circle follows the character instead of staying put.
- Debug 2nd-class button grants the CRAFTING class (ScrollScribe). Needs to grant the current
  path's real 2nd/3rd profession — or let admins bypass the class-master quest gate.

## 🟡 TIER 3 — systems to rework

**Items & vendors** (one connected batch — now driven by [design/RarityLadder.md](design/RarityLadder.md))
- 🆕 **The six-rarity ladder** — one base item per slot/grade at Common/Uncommon/Rare/Epic/
  Legendary/Mythic = 45/55/70/70/85/100 %, split at 70 % where set bonuses + attributes switch on.
  **Deletes the "(Lesser)" line entirely.** Raises the ceiling ~43 % (today's top becomes the Epic)
  — measure with `BalanceMatrix`, don't hand-derive. New drop tables + crafting (Legendary only,
  70 % success, 20 % of those Mythic).
- **Rarity out of the NAME** → the name is just `Electrum Bow`; rarity shows as the name's COLOUR
  plus a `Rarity:` row in a structured description (Name / Grade / Rarity / Type / stats).
- 🆕 **Rarity colours in the Unity client** — WPF has a 3-colour `RarityBrush`; Unity has none.
  Needs a 6-colour palette everywhere an item name is drawn (inventory, vendor, warehouse, trade,
  loot, details).
- **Every item gets a description**, tradability shown outside the name, and the box rules:
  a tradable box may yield an untradable item and vice versa.
- **Vendor rework**: split Armsmaster into weapons + armor; grid-of-squares with tooltips plus a
  toggle to a two-line list; tapping an item opens a confirm dialog carrying the description.
- **Shop price + grade pass**: sell only F/E/D, and only up to Rare. The playtest-13 table is the
  **Rare** price — Common 35 %, Uncommon 70 %, Rare 100 %. Delete the legacy gear (`AshWand`,
  `IronMace`, `WoodenShield`, `BrassAmulet`, the Worn/Steel/Tempered grid). **Darksteel/Cobalt/
  Electrum/Adamantine STAY** — they are the current ladder's grade themes.
- **Warehouse grouping** — put/withdraw, each with equip / consumables / crafting tabs or groups.
- **Weapon audit** — old gear and the training weapons carry no M.Atk.

**Quests** (the other connected batch)
- Level ranges (from → to), with class quests exempt from the upper bound.
- A **quest detail window** — the NPC lists names only; tapping opens description + accept/decline.
  The class master shows the required quests the same way.
- **Abandon** with a confirmation (and a warning when the level range makes it unretakeable).
- **Repeatable quests** — endless gathering (per-mob `QuestItemRewardModifier` paying exp+gold per
  quest item on turn-in, on top of any main reward), finite repeatables, and talk-to repeatables;
  daily-limited or not.
- **Daily apothecary quest** — accept-and-finish, gives a 1h shot selection box (untradable),
  levels 6-75, server-time reset. Quest-granted shot boxes untradable; bought ones tradable.
- **Quest window rework** — active / unavailable / completed tabs, `[track]` with a movable
  on-screen tracker (3-5 max), `[details]` on every row.

**World & mobs**
- **Aggression pass** — all-aggro belongs to dungeons, instances and boss zones only. Elsewhere
  make ONE mob type aggressive per zone; today a level-22 champion in a 22-28 zone is ganked by
  casters plus melee and dies.
- **Mob cast bar** — believed built, not showing.
- **World re-layout** — 4-level fields (1-4, 4-8, … 88-89, 90), grouped under five cities
  (1-16 starter with two fields · 16-40 · 40-60 · 60-75 · 76-90 with elite spawners at 80/84/90),
  spacing between bands so they don't bleed, elite spawners near but out of aggro range
  (1-1.5k), every city carrying vendors + keeper + gatekeeper, gatekeepers linking their own
  fields and the other cities, dungeon gates inside the matching city.

**Skills & classes**
- **Numeric skill/passive descriptions**, per learned level, including conditional lines
  (e.g. an extra bonus that only applies in light armor).
- 🆕 **Merge Archer into Rogue** — [design/RogueArcherMerge.md](design/RogueArcherMerge.md).
  Archer and dagger are one class until 40; delete Hunter (4) / Warden (10) / Marksman (16) and
  split at the 3rd class instead, **race-based**: Human → **Nullblade** / Sharpshooter ·
  Ork → Venomweaver (venom) / **Hunter** · Elf → Phantom / Trapper. Still two disciplines per
  class, so the id scheme survives; `Disciplines.Of` becomes race-aware and `Nullblade`/`Hunter`
  append to the `Discipline` enum. **This closes the missing archer 20-40 kit for free** — the
  Rogue table already teaches both Stab and Shot ladders — and every new discipline maps onto a kit
  already authored in `design/Disciplines.md`. Needs a `game.db` wipe.

## 🟢 TIER 4 — not built yet

- Character **delete** on char-select.
- **Chat tabs** (colours + tags shipped in WPF only; still the oldest open item).
- **Target visual on the mob itself**, not just the target window.
- Auto-farm should **show which mob it is fighting**.
- **New-quest indicator** over quest givers.
- **Kill summary line**: `Exp: +eee, SP: +sss, Gold +ggg`.

## 🔎 Needs investigation
- Mage P.Def 360 at level ~15 while same-level mobs hit for 2-3 (unbuffed 257 → 16 on 110 HP,
  which reads fine). Likely low-level buffs overshooting rather than a formula fault — measure
  with `tools/BalanceMatrix`, don't hand-derive.

---

## Findings from playtest-13's questions (answered 2026-07-29)

> **Both of these were answered by the owner the same day and became designs** —
> [RogueArcherMerge.md](design/RogueArcherMerge.md) and [RarityLadder.md](design/RarityLadder.md).
> The raw findings are kept below because they are the measurements the designs rest on.

**Archers really are missing their 20-40 kit — CONFIRMED.** In
`Game.Shared/RaceAndClasses/ClassSkillTables.Common.cs`, Tank, Warrior, Rogue, Nuker and Healer
each register a full 20/24/28/32/36 (or 20/25/30/35) ladder. `Archetype.Archer` registers exactly
two lines: `BattleFury @20` and `PowerShot @24`. So Marksman/Warden get the base-fighter 5/10/15
kit and then almost nothing. **Fix: author the archer ladder** — Rogue's shape is the right
template (armor/weapon mastery + `PreciseShot`/`PowerShot` levels + `BowExpertise`), plus the
archer's own identity skills.

**Common/Uncommon/Rare vs "(Lesser)" — both are real, and they collide.** The tiered ladder has a
TOP set per grade (`bow_t20` = *Electrum Longbow*, P.Atk 191, Epic, set bonus) and a LOW/vendor set
(`bow_t20lo` = *Electrum Longbow (Lesser)*, P.Atk 129, no attributes). Then `ScaledDropItems`
spawns Common/Uncommon/Rare copies at **65 / 78 / 90%** of *whatever it copies* — including the
Lesser pieces. So at E grade: Common 124 → **Lesser 129** → Uncommon 148 → Rare 171. That is
exactly the ordering the owner noticed, and it is arithmetic, not a bug. **What IS wrong:** the
shop still sells the *legacy* F-Common grid ("Worn Sword", P.Atk **6**) alongside the Lesser line,
so the vendor list mixes two unrelated generations of gear — which is also why the list is
unreadable. The shop pass should drop the legacy line and sell the Lesser sets only.
⚠ Open question for the owner: he asked to remove "ash and whatever low equipment … Darksteel,
Cobalt" — but **Darksteel/Cobalt ARE the new grade themes** (D and C) of the CSV ladder. `Ash Wand`
and `Iron Mace` are the legacy hand-added items. Confirm before deleting anything.

**SmokeTest / the bot cost zero tokens** — they are local .NET processes. The week went on the
0.28.81-0.28.91 build spree (eleven versions, several whole systems).

**Wishes are not lost** — every playtest is kept verbatim, now in `docs/testing/Playtest-NN.md` as
well as in memory.

---

## Independent — buildable any time, no blockers
- **Wearable titles** — leaderboard title over the head / by the name; design alongside the
  charisma board (charisma itself shipped in 0.28.87-88).
- **Combat depth** — perfect/excellent block, magic-resist passives, position bonuses.
- **Every non-admin command as an ACTION** — friend/party/sit/attack/assist in the Skills window's
  Actions tab. Block/like/unblock landed there in 0.28.90; the rest still have nowhere to live
  since the player target frame lost its buttons.
- **Partial-stack trading** — `TradeOffer` carries per-item counts and splits on completion.
  Now overlaps directly with the tier-1 stacking bug; do them together.
- **Client-side collision** — stop at the surface, reject out-of-world taps, so `ConfineToDomain`
  goes back to being an anti-cheat backstop.
- **Admins excluded from the rankings** — pairs with the tier-1 ranking bug.
- **Account warehouse** — the private one shipped; the account-wide half is still open.

## Dependent chains (blocked or gated)
- **3rd / 4th class kits** — 🔴 BLOCKED on the owner's skill CSVs; still the biggest single
  content unlock. The archer gap above is the same authoring job one tier lower.
- **Instances** — design done, owner HOLDING. Open decision: daily attempts GLOBAL vs PER-INSTANCE.
- **Command channel** — gated on clans/alliances.
- **World expansion to 1kk+** — the re-layout above is the first real step.
- **Castles + vault** — needs siege design; consumes the `VendorBuyTaxRate` hook.
- **Boss-point reward shop** — needs bosses/instances producing points.
- **Presentation pass** (owner's own words: "no sounds, a bit woody, no good visuals") — not yet
  a scheduled work item, but it is now the loudest remaining gap.

## Deferred (explicit owner hold)
- **MP potions** — held until the 3rd-class kits decide the MP economy.
- **SP bottles** — 1e9 SP → one bottle; also what keeps `SkillPoints` an `int` honest.
- **Bot-prevention CAPTCHA** — revisit with behavioural detection + charisma/block signals.
- **3rd-class CSVs, Instances** — "Hold" (2026-07-22).

---

## My view of the order
1. 🔴 **Tier 1, all eight.** Buff persistence and mat stacking are the two that change how the
   game feels; the rest are small. This is one batch, one build.
2. 🟠 **Tier 2 hygiene** — quiet the SQL log first (it costs nothing and makes every later
   server session readable), then the two clipped windows.
3. **The archer 20-40 kit** — a whole playable class is currently hollow, and unlike the 3rd-class
   work it is not blocked on anything.
4. **The item/vendor batch** — rarity-out-of-name, descriptions, shop grades + prices, vendor UI,
   warehouse grouping. They touch the same code and the same screens; splitting them costs more.
5. **The quest batch** — detail window, level ranges, abandon, then repeatables and the quest
   window's three tabs. The daily apothecary quest falls out of repeatables almost free.
6. **Aggression pass**, then the **world re-layout** (the bigger of the two by far).
7. **Tier 4 conveniences** — char delete, chat tabs, target visual, kill summary line.
8. **3rd-class kits** the moment the CSVs land.
