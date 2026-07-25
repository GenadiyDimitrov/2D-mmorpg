# Changelog

Development history, newest first.

Early work was tracked as **phases** — self-contained slices that each ended in a playable build.
Phases 1–3 built the foundation (movement, interest management, combat, skills, buffs, the
safe-zone town, banded hunting grounds); the written phase record runs to **Phase 24.1**
(2026-06-22). After that the phase numbering was dropped and commits became the record, so entries
from mid-2026 on are grouped **by date** instead. Later, `GameConstants.GameVersion` (starting
0.1.0, currently 0.27.0) began gating the client/server protocol handshake — it tracks wire
compatibility, not this feature history.

For what's *planned* rather than done, see [Roadmap.md](Roadmap.md).

## 2026-07-25 — Blueprint crafting + a latent crafting-crash fix (0.28.84)

- **Fixed a latent crash**: `RecipeCatalog` set `_byId = Build()` as an inline field initializer *before*
  the `Cross`/`Steps` tables it reads — static initializers run in textual order, so `Build()` NRE'd and
  the whole catalog threw `TypeInitializationException` **on first access (i.e. the first craft)**. It
  survived because crafting had never been exercised end-to-end. Fixed with an explicit static constructor
  (runs after all field initializers). Now covered by a SmokeTest craft.
- **Blueprint economy** (owner's design): an endgame (DropOnly / A-grade) recipe is unlocked by consuming
  **1 blueprint** (its recipe book — renamed "Blueprint: …"), and **each craft consumes 1 more**, so the
  first craft costs 2. `HandleCraft` requires + consumes `RecipeBookId(recipe.Id)` for DropOnly recipes;
  learn/craft messages spell out the cost. SmokeTest verifies unlock→craft→blocked-without-blueprint.

## 2026-07-25 — Private warehouse (0.28.83, server-side)

Built the per-character warehouse the shot-rune system already pointed at ("move a rune to the warehouse
to switch it off" — a rune's buff only applies while it's in the bag).

- **Model**: `Entity.Warehouse` — a second item list, separate from the bag so every bag iteration
  (equip, RecomputeDerived, drops, trade) is untouched. Base **50** slots (`GameConstants.WarehouseSize`).
- **Persistence** (SCHEMA CHANGE — delete game.db): items carry an `InWarehouse` flag; snapshot writes both
  lists, load routes each item to the bag or bank. Verified by SmokeTest (deposit → relog → item is in the
  BANK, not the bag).
- **Commands/DTO**: `OpenWarehouse` / `WarehouseDeposit` / `WarehouseWithdraw` + `WarehouseUpdate`. Deposit
  auto-unequips and `ReconcileRuneBuffs` drops a deposited rune's buff (withdraw re-applies it); a banked
  rune still expires. Sent on login so the client has it without a town trip. Access gated to **safe zones**.
- **NOT YET**: the client warehouse window (Unity + WPF) — server + protocol only; UI is the follow-up for
  the next APK. Account warehouse + slot-expansion tickets remain deferred.

## 2026-07-25 — Gear ladder: low-grade fills, named sets (0.28.82)

Filled the low-level gear holes and gave every tiered piece a proper name.

- **Low sets** (`ItemCatalog.LowTierFillers`): each grade now has a LOW set covering the bottom of its band,
  beside the existing TOP set — **Low F** (lvl 2-9), **Low E** (20-32), **Low D** (40-44). Equippable at the
  grade level (ItemLevel 1/20/40), ids `_t{L}lo`, armour interpolated between the Newbie floor and each
  grade top (fixes an inversion where low armour sat under the Newbie set). All 8 weapons + full armour.
- **Buyable + drop**: low sets sold at the Armsmaster (ids derived from the catalogue) with ItemLevel-scaled
  prices (Low F < E < D), and they auto-generate rarity drop copies like every tier.
- **Named gear**: `ItemCatalog.GradeTheme` gives each grade a MATERIAL prefix starting with the grade LETTER
  — Ferrite/Electrum/Darksteel/Cobalt/Bloodsteel/Adamantine/Soulcrystal/Starstone/Seraphite. Names are
  "{Material} {noun}" (Blade, Maul, Fangs, Longbow, Battlestaff; heavy Bulwark/Warplate, robe Vestments/
  Raiment, shield Aegis, Pendant/Band/Stud …). e.g. **Bloodsteel Warplate**, **Darksteel Vestments**. Low
  sets add "(Lesser)". S-grade themes are wired, waiting on the endgame CSV.

## 2026-07-25 — Magic stat model: weapon-based M.Atk (0.28.81)

Reworked M.Atk to L2's **multiplicative** shape (matching P.Atk, which already worked this way), because
the old **additive** base (`atkStat + level·2 + weaponM`) put the ~41-point power stat in as a flat FLOOR
— a level-1 mage read ~40 internal M.Atk where L2 has ~8, doing ~2.2× L2's magic damage and one-shotting
low-level mobs. Now the **weapon M.Atk is the base and the ATK stat multiplies it** (fist value when
unarmed), so a small wand yields small M.Atk and the staff's big base carries the endgame.

- **Two stat multipliers** (owner's "2 coefficients"): `PAtkStatMult` linear, `MAtkStatMult` super-linear
  `(atk/40)^1.75` ("INT is king" for magic). The exponent mainly rewards ATK *investment* (dyes/swaps) —
  geared endgame is driven by weapon M.Atk + robe `M.Atk ×1.17` + attributes, not the stat.
- Measured (BalanceMatrix): lvl-1 mage internal M.Atk 40→**8** (L2-exact); lvl-8 nuke 399→154. Endgame now
  lands on the original anchors (414 dmg vs a high-lvl tank [anchor 300-400], ~3.8 casts). Fighter physical
  untouched. Endgame magic will be set by the coming S-grade staff M.Atk, not the stat.
- **M.Atk display** = `min(internal, 20·√internal)` — honest small number low, shrink only the cosmic high end.
- **Mob M.Def** coefficient 3.0→3.16 (L2 lvl-83 mob = 262). **Mob SP** = flat **1/20** of exp (was a decaying
  1.0→0.05 curve; L2 is flat). `ExpCurve.md/.csv` regenerated.
- Roadmap added: `docs/design/GearLadderAndCrafting.md` (S/S\*/S\*\* grades, ladder gaps, blueprint crafting).

## 2026-07-25 — Overnight bug + polish batch (0.28.80)

Autonomous session against the device-playtest findings (`playtest-12-results`). No schema changes, so
existing characters survive. Large social/economy features (charisma, block, buy-back, chat tabs,
partial-stack trade, client collision, wearable titles) are deliberately NOT here — they need a schema
change and/or the owner's input, flagged for a session together.

Fixed:
- **Equip presets on the bar no longer vanish** — `SyncSkillBar` was wiping `preset:` tokens (item:/
  action: were exempt, preset: was missed); it's now exempt too. SmokeTest guards it.
- **Dungeon mobs spawn in the dungeon and aggro** — `WorldMap.Border` was the positive overworld only,
  so `ClampToBorder` snapped every negative-quadrant (dungeon) spawn to (0,0); it now spans the full
  world. This was "mobs spawn on one spot and don't fight back" in the crypt.
- **Learn tab refreshes on level-up** — it keyed off `ActiveClass.Level`, which only the Subclasses push
  set (login/swap), so it went stale after a level-up; the Progress push now keeps it in step.
- **Basic attack is not auto by default** — the client seeded it into the auto set; now nothing is auto
  unless explicitly marked.
- **Admin characters excluded from all leaderboards** (an admin at level 999 would top them forever).
- **Shop pricing** — shot runes 150k/1h and 280k/2h; equipment floored at 200g (jewels exempt).
- **Low-level gear drops gated** — a level-8 mob no longer drops E-grade (level-20) gear; below mob
  level 18 the loot is training/broken gear + mats.
- **Rare healing potion removed from the vendor** (drops/rewards only).
- **Party loot control** moved to a coloured button by the buffs toggle (random = blue), leader-only
  drop-down.
- **Bag equip paper-doll** moved below the header so the Head slot no longer overlaps the tabs.

Measured, not changed (flagged for an owner decision): the "low-level one-shot" is the mage nuke, which
one-shots trash at every level (the tuned level-20+ matrix does the same) — consistent nuker design, not
a low-level bug. BalanceMatrix now prints levels 1-10 with real starter gear.

---

## 2026-07-25 — Console freeze fix + speed-display fix (0.28.78)

Two device-playtest fixes.

- **Console freeze (regression from 0.28.77).** The append rewrite in 0.28.77 was correct, but the trim
  I added — `while (childCount > 120) Destroy(oldest)` — FROZE the phone: Unity's `Destroy` is deferred
  to end of frame, so `childCount` never drops inside the loop and `GetChild(0)` keeps returning the same
  already-marked object → an infinite loop the moment the log passed 120 lines. Now the excess is counted
  ONCE and that many rows are destroyed by index. (0.28.76 lag = real accumulation; 0.28.77 freeze = this
  trim bug; 0.28.78 resolves both.)
- **Attack/cast speed display was inverted.** The DTO field is a cast/attack-TIME multiplier (lower =
  faster: the server sends `SpeedBaseline / stat`), but the tier-2 display did `raw = mult × baseline`,
  which flips it — a fully-buffed caster read "158 (x0.47)" when the real stat was ~702 at ~2.1×. The raw
  stat is `baseline / mult` and the speed multiplier is `1/mult`. (Playtest: the cast-speed and M.Atk
  NUMBERS a player flagged were display artifacts; the lvl-1-one-shots-lvl-4-8 finding is real and
  deferred to a measured BalanceMatrix pass.)

---

## 2026-07-24 — Console lag fix + playtest APK (0.28.77)

Live device playtest (Gena) surfaced a real one the SmokeTest can't: with the chat/console window
OPEN, the phone lagged worse and worse until the log was cleared.

- **Console now appends instead of rebuilding.** `RefreshConsole` used to Destroy every child and
  rebuild all up-to-200 labels — each with a ContentSizeFitter — plus a `Canvas.ForceUpdateCanvases()`,
  on EVERY new log line while the window was open. During combat/debug spam that is many full
  teardown/rebuilds a second, and the cost grew with the accumulated line count — so clearing (→ ~0
  rows) made it cheap again, exactly what was seen. `ClientLog.Line` gained a monotonic `Seq` and a
  `ClearGeneration`; the console draws only undrawn lines and trims oldest rows past a 120 cap. Bounded
  work per frame regardless of session length.

Also fixed the deploy-order slip that made the first two rebuilds ship a STALE version label: the APK
version is stamped from `GameConstants.GameVersion` in the Unity plugin DLL, so `dotnet build` (which
copies the fresh DLL into Assets/Plugins) MUST run before the headless Unity build — see
`version-bump-deploy-order`. The served APK is now correctly 0.28.77.

---

## 2026-07-24 — Every name-only command is now a bar ACTION (0.28.76)

Completing the owner's "every command that doesn't need a value, only a name, as an action button". The
Actions tab already had eight (Attack, Target Closest, Sit/Stand, Run/Walk, Trade, Party Invite, Follow,
Assist) — so no command was ever homeless — and this adds the remaining six:

- **Add Friend / Remove Friend** (target a player) and **Friend List**.
- **Leave Party**, **Kick from Party** (target a member), **Pass Leadership** (target a member).

Each is placeable on the skill bar like any action. The TARGET supplies the name, so nothing is typed.
Commands that need a real VALUE stay typed — `/w <name> <message>` (a message) and trade quantities (a
number) — because a button cannot supply one. Admin commands are excluded (owner: "except admins").

Implementation note: the friend actions resolve the target to a NAME (the hub takes a name, because
friendship must work on someone offline or out of view); the party actions take an id, since a party
member is present by definition. The slash equivalents (`/fadd` `/frem` `/flist` `/ptleave` `/ptkick`
`/ptcl`) all still work.

---

## 2026-07-24 — Tier-2 UI batch, part 3 — the list is complete (0.28.75)

All thirteen cheap playtest-11 UI items are now done.

- **Buff taps: single = details, double = cancel.** Cancelling used to be a SINGLE tap, which put an
  irreversible action one stray touch away on a bar you brush past constantly — and there was no way at
  all to read what a buff did. A single tap now opens a tooltip-style popup (name, description, time
  left, whether it can be dismissed) that closes on a tap anywhere outside; a double tap within 0.35s
  cancels. Debuffs are not yours to dismiss, so double-tapping one just re-shows its details.
- **Party effects are SQUARES beside each member**, same shape as the personal buff bar and using the
  same abbreviations, green for buffs and red for debuffs. Rows are a fixed 46px now instead of growing
  to 64px when someone had effects, which is what kept making the window taller. Panel widened
  300 -> 380 so the squares clear the leader's Kick/Lead buttons.
  ⚠ **No `<60s` flashing**, unlike the personal bar: `PartyMemberDto` carries effect NAMES only, with no
  remaining time, so there is nothing to count down. It needs durations on the wire — a DTO change.
- **Loot mode is a DROP-DOWN**, not a cycle button. Cycling was not merely fiddly: every tap STARTS A
  VOTE the whole party has to answer, so tapping past a mode you did not want was not free. Picking a
  row proposes that mode directly; `NextLoot` is gone.
- **World border** — an orange dashed rectangle around the overworld, as a placeholder until there are
  mountains or an ocean. It deliberately does NOT hide behind the zone-colours toggle: walking into an
  invisible wall with nothing to explain it is the problem it solves, and that does not go away when the
  map overlay is off. Only the positive overworld is outlined — the negative quadrant is teleport-only,
  so its edges are never something you can walk up to.

---

## 2026-07-24 — Tier-2 UI batch, part 2 (0.28.74)

- **Bag: `Equip` leads the row and the paper-doll opens on the LEFT** (owner). Equip is the control that
  reshapes the window, so it goes first; the item list now slides right by exactly the width the window
  gains, keeping its position relative to the right edge instead of being shoved outward.
- **Potion heal-over-time has its own floater.** The pipeline was already correct end to end — potions
  carry `SkillEffect.HealOverTime`, the tick broadcasts `CombatOutcome.Heal`, the client draws `+N`.
  The problem was that it was INDISTINGUISHABLE: a potion tick, a cast heal and ambient regen all drew
  the same green `+N`. HoT ticks are tagged now and render as a distinct mint `+N hot`.
  ⚠ Note `TickHealOverTime` early-returns at full HP, so drinking while topped up heals nothing and
  shows nothing — correct, but it looks identical to broken and may be what was actually seen.

---

## 2026-07-24 — Weapons carry BOTH their CSV numbers; the caster rule moves into a passive (0.28.73)

The gear CSV has always authored weapons as a PAIR — `92/54` for a level-20 sword — but only one number
ever reached the game. A fighter weapon kept P and discarded M; a magic weapon kept M and discarded P.
The missing channel was reconstructed by multiplying the WHOLE finished channel by `OffChannelFactor`
(0.6), an invisible per-item multiplier. Two consequences: no weapon in the catalogue set `MAtkBonus`,
so **no weapon ever showed an M.Atk line on its card**, and "why is my M.Atk 60%?" had no in-game answer.

- **Both numbers are authored now** — `AtkBonus` = P, `MAtkBonus` = M, straight from the CSV, for all
  eight weapon families across all five tiers. `PAtkFactor`/`MAtkFactor` are retired to 1.0.
- **Weapons contribute their own M.Atk** like every other slot. Items that predate the migration have
  `MAtkBonus = 0` and fall back to the old shared-number behaviour, so nothing rebalances under them.
- **The caster rule moved into `Weapon Proficiency`**, where a player can read it. It is now TWO gates,
  because they answer different questions: **cast speed** keys on the trained TYPE (sword/blunt, which
  includes wands and staves), while **M.Atk** keys on the weapon actually being a MAGIC weapon — which
  the type cannot answer, since a wand and a mace are both `Blunt`. That is precisely the hole
  `MAtkFactor` was plugging: the old type check waved a mace-swinging caster through at full power.
- The multiplier lives once, on the class rule (`Entity.NonMagicWeaponMagicMult`), instead of on every
  weapon — so a fighter picking up a wand is no longer silently taxed for a caster's problem.

**Verified by measurement, not by reasoning** — this area has a history of hand-derived diagnoses
blaming the wrong system. `BalanceMatrix` output is byte-identical before and after across every case
it covers (mage, tank/fighter, mob curve, TTK, levelling pace).

Two deltas it does NOT cover, reasoned explicitly: a **caster holding a mace** now contributes the
weapon's real M.Atk (132 at A-grade) rather than its P.Atk (281) before the penalty, so that build gets
weaker — the intended direction. A **fighter's** M.Atk shifts slightly, since `(base + 281) x 0.6`
becomes `base + 132`; fighters have no spells, so it is inert unless a hybrid leans on it.

---

## 2026-07-24 — Tier-2 UI batch, part 1 (0.28.72)

Seven of the thirteen cheap playtest-11 UI items. Client scripts compile-checked with a headless Unity
build (`dotnet build Game.sln` does NOT cover the Unity project — see the checklist for the invocation).

- **The duplicate town line is gone.** There were TWO independent "You entered X" systems: the server's
  Region banner (big, with a background) and an older client-side zone label that coloured towns blue.
  The second one's own comment said it should be replaced "when Regions ship on both clients" — which
  they have. Removed.
- **The region banner no longer eats taps.** As a plain Image + text it was a raycast target, so every
  tap landing on it was swallowed ("prevents me clicking below my char"). It is a notice, never a
  control, so nothing about it is interactive now.
- **Durations roll over into DAYS** — a 30-day shot rune read `719h59`, which is true and useless. Now
  `29d`; past a week the hours are dropped entirely, so every tier stays at most four characters.
- **Debug-menu chat spam removed** for items, levels and buffs — taking ten potions wrote ten identical
  rows. Each already has visible feedback (inventory refresh, the level number, the buff bar). The rare
  ones keep their line: teleport coordinates, karma, class change.
- **A targeted PLAYER carries no fast buttons at all.** Attack/Follow/Assist/Party/Trade come off the
  frame; those belong in the Skills window's Actions tab, placeable on the bar. Mobs keep Attack (the
  core loop) and Info (stats + drops).
- **Target HP/MP as digits**: current/max instead of a percentage, plus an MP bar for player targets.
  ⚠ This REVERSES the older "another player's exact HP is information you should not have" rule, at the
  owner's request. Level stays private.
- **Attack/cast speed show the raw stat**: `1234 / 1500  (x3.70)` instead of a bare `x1.10`, in both the
  Stats window and mob Info. No wire change — the engine uses the L2 model where 333 = 1.0x, so the raw
  value is `mult x 333`, and the caps are the real `StatCaps` ones.
- **Standing up is INSTANT after a real rest** (seated >= 3s). The recovery exists to stop sit/stand
  spam and now only costs that. Being HIT while seated still pays the full delay — a combat interrupt is
  not a voluntary stand.

Remaining in the batch: bag Equip-first + expand-left, potion HoT floating text, buff double-tap
cancel / single-tap details, party buff-debuff squares + loot drop-down, world border, and
commands-as-actions.

---

## 2026-07-24 — The level-10 starter quest, and ReachLevel steps actually work (0.28.71)

Completes the starter-gear redesign. The Newbie kit is no longer given away — it is EARNED.

- **New starter chain**, offered by **Armsmaster Dolan** (the gear vendor handing out gear needs no
  explaining, and a new player is already walking to him to spend their first gold):
  - **"A Proper Kit"** (level 10) — slay 10 Ashen Wolves, return → the **armor + weapon** choice boxes.
  - **"Blooded"** (level 12, gated on the first) — slay 15 Werewolves, **reach level 15**, return →
    the **jewels box + 1-day shot rune**, the two things deliberately removed from character creation.
- Both rewards are SELECTION boxes, so the chain stays class-agnostic exactly like the creation kit.
- Pacing against the new curve: the rewards are 52% and 39% of a level — meaningful without
  trivialising — and the second quest spans **122 mobs** of levelling from 12 to 15, which is the
  "levelling to ~15 while doing it" the owner asked for.
- A vendor can host a quest and a shop at once: the dialog only special-cases Buffers, and the shop is
  attached alongside the quest list.

**`QuestStepType.ReachLevel` had never been implemented.** It has sat in the enum since quests were
written, but no quest used it, so nothing noticed that no code anywhere advanced such a step — a quest
reaching one would have stalled forever. `AdvanceLevelQuests()` now handles it, called from three
places: on **level-up**, on **quest accept** (you may take a quest already past its level) and after a
**kill or talk step advances** (finishing a step can make a ReachLevel step current, and a level-up is
the only other trigger — a player already past the level would otherwise sit there permanently).

---

## 2026-07-24 — Starter gear: the TRAINING tier for levels 1-10 (0.28.70)

The owner's playtest finding was that a new character one-shots everything: it started in the **Newbie**
set, which is strong enough to trivialise the first zones. That set is now the **level-10 quest reward**
(quest still to build), and a new character starts in a new, deliberately feeble **Training** tier.

- **Training weapons** (~a quarter of the Newbie numbers): Training Sword 6, Club 6, Knives 5, Bow 11,
  Wand 7 M.Atk. **400g each** at the Armsmaster, so a bad pick or a loss is recoverable — unlike the
  Newbie tier these are buyable.
  ⚠ The owner authored these as P.Atk/M.Atk pairs (6/5, 6/5, 5/5, 11/5, 5/7). Only the FIRST number is
  authored; the second follows from the weapon's CHANNEL FACTOR. Forcing a dagger's M.Atk to match its
  P.Atk would mean `MAtkFactor 1.0` — daggers casting as well as a staff — which reverses the
  weapon-identity rule the item model is built on. The standard 0.6 lands within a point or two.
- **Training armor**: Leather 53 P.Def, Robe 27 P.Def + 29 MP. No set bonus — the set line starts at
  the Newbie tier, i.e. at the quest.
- **NO jewels and NO shot runes at creation** (owner). Both were in the old kit.
- **Broken jewels** — a new level 1-5 drop line and the first accessory anyone owns: Broken Earring
  (11 M.Def, 40g), Ring (7, 30g), Necklace (15, 60g). They drop as one mutually-exclusive group (10%
  combined) from mobs at level ≤5 and are stocked at the Armsmaster. **Tradable**, unlike the bound
  starter kit — the first thing a new player owns that is worth selling.
- Both character-creation paths (`CreateCharacterAsync` and the live `GiveStarterKit`) now hand out the
  same two class-agnostic selection boxes, so there is no fighter/mage branch left to drift.

**Still to build:** the level-10 starter QUEST that awards the Newbie set, the jewels box and the 1-day
shot rune.

---

## 2026-07-24 — Damage ledger: most-damage earns the kill, contested kills split (0.28.69)

The last unbuilt piece of the exp spec. Until now there was **no per-attacker damage tracking at all**,
so "killer" meant whoever landed the final blow: a party could do 99% of the work, lose the last hit and
walk away with nothing.

- **New `Entity.DamageLog`** (mobs) — attacker id → damage actually dealt. Deliberately SEPARATE from
  `Threat`: threat is a targeting signal that taunt and detaunt move around on purpose, so it says who
  the mob wants to hit, not who earned it. Only PLAYER damage is banked. Cleared on spawn and on reset,
  so a mob that leashed home and healed owes nobody.
- **The top damager earns the kill** — drops, quest credit and the karma tick all key off them now.
- **Contested kills split the EXP by damage share**, pooled BY PARTY so a party counts as one contender:
  80% of the damage takes 80% of the exp; the other side takes 20% and no drops. Each contender's slice
  then runs the normal rules — pot × roll × party bonus, split equally, personal level-gap penalty.
- A contributor who **left the world** is skipped but their damage stays in the total: their share is
  forfeited, not redistributed, so having a friend log off can't inflate your cut.
- **`Entity.LastHitterId`** records the final blow. It is no longer what rewards pay on, but it is kept
  as a counter for raid/epic bosses (owner).
- One roll per kill still covers everyone on it, so two parties on one corpse see consistent numbers.

---

## 2026-07-24 — The last four playtest-11 tier-1 bugs (0.28.68)

All seven tier-1 bugs are now fixed. The two interesting ones were invisible from the symptom.

- **Skills → Learn now says why it can't.** The row was `canLearn ? action : null`, so an unaffordable or
  level-locked skill got a **dead button** — tapping did nothing, with no message, which is
  indistinguishable from a broken feature (and was reported as one). The server was always fine; every
  rejection path there sends a reason. The button is now always wired and explains level / SP / gold.
- **The soft keyboard lifts the command bar.** There was no keyboard handling anywhere in the client —
  the lift had never been written. Android's keyboard is an overlay that does not resize the game view,
  so a bar pinned to the bottom edge is simply swallowed. The field + Send + Log now offset by the
  keyboard height (converted from screen pixels to canvas units via the reference height).
- **`[lead]` moves the badge and the button.** The server was already correct; the client's party window
  only rebuilds when a **stamp** changes, and the stamp covered HP/MP/status/buff counts but **not
  `IsLeader`** — so passing leadership changed nothing it could see. `IsLeader` and the member IDs are
  in the stamp now (the IDs also catch swapping a member for another with identical HP). The `*` badge
  became a gold star.
- **Dungeon mobs aggro, retaliate and spread out again.** One root cause behind both symptoms: `MobAi`'s
  engaged branch returns early after a leash check, and **nothing ever cleared `Engaged` when the target
  left the world** (`DropAggroOn` was only wired to the stealth path). A mob whose target teleported away
  from the debug menu stayed engaged forever — never re-scanning for aggro, never wandering, just
  standing there, which read as both "they don't fight back" and "they're clamped together". Fixed with a
  live-target guard that retargets by threat or disengages, plus shedding aggro when a player leaves.
  Aggravating factor for the clumping: wander used a flat ±1000 offset and projected anything outside the
  zone **exactly onto the rim**, and the crypt's rooms are radius 300-350 — so every mob walked to the
  same small circle. The span now scales to the room and lands inside it.

---

## 2026-07-24 — EXP/party/drop rework + first playtest-11 fixes (0.28.67)

- **The whole progression curve moved to `Game.Shared/ExpCurve.cs`** — one place for the level curve,
  the mob reward, the SP ratio, the level-difference penalty, the party bonus and the random roll.
  - **Player curve = the real Lineage 2 table, levels 1-100.** Not a formula: its own shape is a power
    law (~8.492·L^3.2891) only to level 50, after which SEVEN multiplicative walls at 51/56/61/66/72/77/80
    stack to ~52x by 85. Levels 1-85 from the masterwork source, 86-100 spliced from 4Game (joining at 86
    reads x1.37; joining at 4Game's own 85 would have jumped x8.6 in one level). 4Game publishes levels
    88/89 transposed — 89 was CHEAPER than 88 — and they are swapped back into order here.
  - **Mob reward** `0.026314·L^3.2427`, fitted to 8.5k/30k/47.5k at levels 50/75/85; below level 30 it is
    interpolated through seven hand anchors so the opening costs 1-2-4-5-5 mobs rather than 295/805/858.
  - **Level-difference penalty** `0.85^(gap-5)`, zero at 13, **symmetric** — fighting up is penalised too,
    which is what stops a level-1 bow last-hitting a level-78 mob. Applies to EXP **and drops**.
  - **Party: shared pot, personal penalty.** `pot = mobValue × roll × partyBonus(n)`, split EQUALLY, then
    each member's own gap penalty applies to their share. The killer no longer gates the party's exp.
    Party bonus 2→x1.2 … 6→x2.0 … 9→x2.3.
  - **±20% random roll** on the final award, one roll per kill shared by the party, covering exp and SP.
  - EXP is `long` end to end. SP saturates at `int.MaxValue` by design — see the SP-bottle plan in
    [Roadmap.md](Roadmap.md).
  - `BalanceMatrix` now prints the full curve plus the gap and party tables, and reproduces
    [balance/ExpCurve.md](balance/ExpCurve.md) exactly: 1 mob for level 1, 20 at level 10, 121 at 20,
    125 828 at 85 — **631 799 to reach 86**, ~136 million to reach 100.
- **Fix: only the FIRST character of the owner's account is born Admin.** Every character on that account
  used to be, which quietly broke the per-character role model — a deliberately ordinary character still
  had every admin command. The role is per-CHARACTER by design; do not move it to the account.
- **Fix: world entry/exit no longer leaks to everyone.** The friend notice was already correctly
  mutual-only; a *separate* global broadcast was the leak. Now behind `AnnounceWorldEntryExit`, off.
- **Fix: `/tp` to a jailed player lands in the JAIL, not a dungeon.** The jail sits in the negative
  quadrant but is not a dungeon, so the dungeon ward grabbed any non-jailed visitor. It is now a
  first-class domain in both the movement wall and the ward.

---

## 2026-07-24 — Inventory boxes + item details (0.28.65 → 0.28.66), and PLAYTEST-11

- **Open boxes from the inventory** (0.28.65) — a plain box grants its contents straight to the bag; a
  **selection** box opens the choice popup and grants only the picked entry.
- **Item-details layout** (0.28.66) — the stat block is no longer crammed under the item name.
- 🎉 **Playtest-11 (0.28.66)** — the owner tested the **whole** `TestChecklist.Unity.md` end to end and
  **§§1-15 all passed**, closing the A–F parity programme, the playtest-10 batch, the world pass and the
  rune shots in a single pass. Exceptions: **Skills→Learn does nothing**, the **soft keyboard covers the
  command bar** instead of lifting it, and the 3h break banner can't be tested in a sitting. 0.28.65 and
  0.28.66 shipped after the test and remain unverified.
  The resulting work — 11 bugs, 16 changes, 5 additions (**block**, **charisma**, **buy-back**), and a
  **level 1-20 starter-gear redesign** — is queued in [Roadmap.md](Roadmap.md) and §17 of the checklist.
  Two design answers came out of it: **partial-stack trading is a YES**, and **admins must be excluded
  from the leaderboards**.

---

## 2026-07-24 — World pass (fields, walls, negative quadrant) + rune shots (0.28.56 → 0.28.64)

- **Whole map on FIELDS** (0.28.56–0.28.58) — field polygons are FILLED and coloured by level (replacing
  the spawn-zone circles); one convex field WRAPS each town with the town drawn on top as an island; a
  boss field (Sunken Vale) + a dungeon field (Hollow Crypt) + a Training Grounds field. All generated as
  convex hulls and verified (no overlaps; every spawner inside its field).
- **No rogue spawners** (0.28.59) — a startup guard throws if any spawner sits outside every field.
- **Dungeons + jail in the NEGATIVE quadrant** (0.28.60) — reached by teleport, off the positive
  overworld; position clamps + the (sparse) cell grid handle negative coordinates.
- **Walls** (0.28.61) — movement is confined to the domain you stand in: the overworld can't be walked
  out of into negative space, and a dungeon can't be walked out of; a ~500u ward teleports a clip-out
  back inside. Teleport is the only way across.
- **Soul/spiritshots as RUNES** (0.28.62–0.28.64) — the always-on training passive is gone; shots are
  held rune items with a wall-clock expiry (persisted, counts down offline, delete-protected). Delivered
  in boxes whose open stamps the clock (also stamped on any other acquire). 1h/2h at the Apothecary
  (tradable), 24h/30d premium/debug (bound); admin seeds both 30d. The newbie starter kit is now
  class-agnostic (armor choice box, one weapon of five incl. staff, a 1-day shot-rune choice box).

---

## 2026-07-23 — Playtest-10 fixes, potion rework, dungeon, regions, leaderboards (0.28.42 → 0.28.55)

Driven by on-phone playtests over VPN. Every entry verified by a headless Unity compile + `dotnet
build`; the server/client were rebuilt between rounds, never mid-test.

- **Playtest-10 round 1–2** (0.28.42–0.28.46) — click-through fixed (the press, not the release,
  decides whether a tap was over UI); the party window stops going stale on a member's leave/kick
  (client clears transients on entry, server pushes an empty party on entry); speed=1 rubber-band
  fixed (`ToLean` sends `EffectiveSpeed`, so walk/slow/stun predict correctly); **sit mechanics**
  (sitting freezes movement, standing has a recovery window); `/tpme`; **change-leader**; a 250-slot
  bag where **worn gear takes no slot** and unequipped gear lives in the Items bag; a hidden-by-default
  fast-delete toggle; party **buff/debuff view**; floating combat text for buffs/heals; the loot-vote
  bot for headless party tests.
- **Auto-farm range ring** (0.28.47) — a ground circle showing the search radius.
- **Flat heal-over-time potions** (0.28.48–0.28.49) — three tiers (Common/Uncommon/Rare) heal a FLAT
  amount over time as an ordinary buff, plus a separate **instant** panic potion that does not cancel
  them. An Auto-Potions **Potions tab** picks the tier per HP threshold, and potions can go on the
  quick-use bar as `item:<id>` tokens.
- **Equipment presets + paper-doll** (0.28.50) — save/restore worn gear as A/B/C loadouts (server
  refuses in combat), persisted in a new `EquipPresetsJson` column.
- **Hollow Crypt dungeon** (0.28.51) — elite rooms + a boss in the NW corner off the town ring, with
  an entrance safe zone; any gatekeeper offers it and the existing engine runs it.
- **Regions stage 2** (0.28.52) — towns became polygons; the safe zone is the UNION of the old circle
  and the town polygon; "you entered X" entry banners; region outlines on the ground.
- **Stand-up no longer rubber-bands** (0.28.53) — the recovery window gates actions, not movement, so
  standing never zeroes your speed under the client's prediction.
- **Leaderboards, break reminder, non-overlapping regions** (0.28.54) — a Menu → **Rank** window with
  five boards (Level / Wealth / PvP / PK / Time played), read from the DB off the loop; the #1 of each
  earns an honorary title. A **3h "take a break"** banner every 3h of continuous play (persisted
  online time). Field polygons pushed clear of the town safe-circles and town octagons inscribed in
  the circle, so regions no longer overlap (verified by a geometry script).
- **Equipment folds into the bag; every target command is a button** (0.28.55) — the standalone
  Equipment window is gone; the bag's **Equip** toggle expands the window to reveal a compact
  paper-doll column with the presets. Follow/Assist (the server always had them) and
  Trade/Party/Target-closest now work from the bar dispatcher, and the target frame shows a contextual
  Attack / Follow / Assist / Party / Trade / Info grid.

---

## 2026-07-23 — Unity↔WPF functional parity: batches A–F (0.28.35 → 0.28.41)

The program to bring the Unity mobile client to *functional* parity with the WPF harness (agreed
2026-07-21) reached all six batches. One batch = one commit; every batch verified by a headless
Unity compile (the `.sln` does not build the Unity assembly).

- **Debug window at full parity** (0.28.35) — six tabs (Equip / Items / Func / TP / Class / Tune),
  read from the catalogs rather than hand-listed.
- **Trade + party invite** (0.28.36) — both were already server-side; only the client half was
  missing. `Boot.PartyInvite` existed and nothing had ever called it, so party was untestable from
  the phone. Trade redraws only from the server's push (never optimistic — that's how players get
  robbed).
- **Auto-hunt setup, two windows** (0.28.37) — Auto-Potions (HP/MP % + on/off) and Auto-Farm (search
  range, keep-position, engage Normal/Elite/Boss), both opened from the Menu; the on/off stays on the
  top-right Auto button. Fixed a bug where the Auto toggle hardcoded potion/farm defaults and silently
  wiped configured settings on every enable — the client now caches the server's config and every path
  preserves the half it doesn't own.
- **Mob info at character depth + a lazy Drops tab** (0.28.38) — `TargetDetails` extended with the
  full stat layer (attributes, speeds, range, crit/vamp/regen/resist, rank). The window is two tabs;
  the drop table is fetched **once** when the Drops tab is first opened and not again until reopened.
- **Inventory rework** (0.28.39) — bag rows are `name (qty) [Details] [e|u]`; a details window carries
  full enchant-scaled stats, attributes, use-skill and set info, with per-kind actions, a bin-delete,
  and an equipment **Compare** (the worn counterpart opens alongside, marked with an orange E).
  Introduced a reusable **selection popup** (titled list of choices).
- **Vendors** (0.28.40) — the vendor asks Buy or Sell; buy lists wares, sell lists your sellable bag;
  a stackable item opens a **numpad** (digits / clear / backspace / keyboard box / Max) and every deal
  ends in a plain-text confirm. Selling was impossible from the phone before this.
- **Learn confirmation** (0.28.41) — the Skills → Learn tab no longer spends SP on one tap; a confirm
  window shows the change (power/MP before→after for an upgrade, or the level-1 numbers for a new
  skill) plus the cost.

Friends needed no work — `/fadd /frem /flist` already matched the WPF client (which has no friends
window). Deferred to the potion rework: the 3-tab auto-potions expansion. See
[Roadmap.md](Roadmap.md) for the 2026-07-23 design ideas (flat-HoT potions, auto-farm skill priority).

## 2026-07-20 — Level privacy, regen cadence, Spirit, and the Unity client

- **Unity mobile client, first playable pass** — a login / character-select / in-world HUD, fixing
  two bugs that had made it blind: it listened for the old full `Snapshot` event while the server
  now sends only deltas, and the project was set to the new Input System while the code used the
  legacy one. Also: an on-screen log console, a connection-liveness strip, and an `adb reverse`
  cabled-phone workflow. Fixed an IL2CPP build break (positional-record `GetHashCode` nested past
  clang's bracket limit) so the Android build compiles.
- **Level is private** — you see your own level and monsters' levels; other players' levels are not
  sent at all, only shared inside the party window. Enforced server-side.
- **L2 regen cadence** — health/mana regenerate in larger chunks every 3 seconds rather than a
  trickle every second, with CON weighted much harder. Damage-over-time and heal-over-time keep
  their own 1-second tick.
- **Spirit (SPT) is a full stat** — the fifth core stat (CON/ATK/WIT/DEX/SPT), driving max mana,
  mana regen and magic defence; stored per-subclass and persisted.
- Debug seed accounts, an automatic stale-DB rebuild, and LAN binding so a phone can reach the
  server.

## 2026-07-17 — Moderation, social, versioning, delta snapshots

- **Account roles + moderation** — persistent, timed jail / kick / ban, authorized server-side and
  shipped (not debug-gated), with a moderator role beneath admin.
- **Friends and social** — a mutual friends list with pending requests, follow/assist, admin
  teleport, and a target-window action menu (trade / invite / follow / assist).
- **Protocol versioning** — a login handshake rejects out-of-date clients, with a
  MAJOR.MINOR.BUILD version (0.27.0).
- **Delta snapshots** — each tick sends only spawns / changed fields / despawns instead of the whole
  visible world, cutting per-tick bandwidth sharply.
- **Skill-bar overhaul** — a 60-slot, multi-row, movable bar that also holds usable items; the
  server now owns bar auto-placement (a long-standing silent-corruption bug), and stops
  auto-placing learned skills at all. A placeable Actions catalog (attack / sit / follow / …).
- **Mob target window** — a compact card with a `[Details]` tab that fetches drops only on demand.

## 2026-07-13 — Damage model, death & resurrection, heals

- **Combat retune** — L2-style damage constants, with the weapon (not the class) deciding the
  physical/magic split, and hidden per-class stat grants removed.
- **`{Flat, Mod}` skill damage** — the foundation for physical skills scaling off attack power,
  landed backward-compatibly.
- **Heal rework** — heals no longer key off magic attack; they use dedicated HealPower /
  HealReceived stats, with no default overheal.
- **Death penalty & resurrection** — dying costs 5% of the level's experience (no de-level), with
  newbie protection below level 40; resurrection arrives as a four-level cleric skill plus ally-res
  scrolls with an accept/decline prompt, and Angel's Protection preserves buffs through death.
- **Everything is a skill** — potions and scrolls now cast skills, unifying consumables with the
  buff system; the buff bar groups by subtype.
- **Level-40 stat-swap passives** — the one thing that shifts a character's main stats.

## 2026-07-08 — Disconnect handling, PvP, auto-hunt, live tuning

- **Auto-hunt / idle farming** — the character farms while idle and, time-capped, while offline,
  reusing the same targeting/skill logic; with roaming bounds, a rank filter and a skill-priority
  order.
- **PvP system** — an L2-style flag / karma / player-kill system with enable and counter-attack
  toggles, self-defence gating, karma you grind off on mob kills, and a 15k cap.
- **Disconnect / exit state machine** — a combat state that gates logout, a disconnect grace window
  vs. offline-farming split, and universal Return skills + scrolls.
- **Debug tuning panel** — live, admin-only editing of rates / karma / caps in-game.
- Settings persist next to the executable; window size and position are remembered.

## 2026-07-01 — Gear, crafting, mobs, and the stat-modifier refactor

- **Tiered gear overhaul** — redesigned weapons, armour, shields, accessories and jewels as
  level-tiers with rolled attributes and named set bonuses; the old procedurally-generated gear was
  dropped in favour of these as (rare) mob drops.
- **Crafting economy** — materials (five types × five rarities) drop from mobs and feed a
  cross-profession crafting chain up to finished set pieces, with recipe books, professions, and a
  boss/elite drop bonus.
- **Mob overhaul** — an 80-mob roster with a CSV-driven base-stat curve, weapon-type resistances,
  and ranged / caster mob roles with their own spells.
- **StatMods refactor** — a single unified stat-modifier layer carries all item, set and mastery
  bonuses (compounding percentages), replacing the old mastery formula with explicit per-level data.
- A CC-resist stat, and the first Unity client slice (making `Game.Shared` consumable by Unity).

## 2026-06-28 — Combat primitives and the effect engine

The deep combat toolkit most later skills are built from: conditional and burst damage;
damage-over-time with a proper stack/effect model (poison, venom, bleed); contested crowd control
(stun, fear, root, slow); movement effects (blink, knockback); a defensive cluster (absorb shields,
mana shield, lethal save, cancel/dispel and cancel-resist); real threat/aggro with taunt (replacing
last-hit aggro); the physical-skill damage-out pipeline (PvE/PvP × skill/magic/basic); and immortal
training dummies for testing it all. Playtest-1 fixes landed alongside.

---

## Phase 24.1

### Lightbringer — the first fully-authored 3rd-class discipline

The pure-heal Healer discipline, built across all three races. One shared idea (keep
the party alive), three race expressions — proving the *discipline + race* model from
24.0 with real, distinct skills:

- **Human Lightbringer** — single-target powerhouse: **Mending Light** (strong, fast
  heal) + **Purify** (cleanse harmful effects from an ally).
- **Elf Lightbringer** — area coverage + control: **Dawn Bloom** (heals *and* cleanses
  all nearby allies) + **Warding Step** (roots an enemy for 8s and sheds the caster's
  aggro from nearby foes).
- **Ork Lightbringer** — area + suppression: **Spirit Font** (AoE heal — a stand-in
  until placed totems arrive) + **Soul Sap** (anti-heal: the target recovers only half
  the HP from any healing for 15s).

### New combat mechanics (engine)

Added cleanly to the `[Flags]` effect system, reusable by future disciplines:

- **AoE heal** — heals scale to all allies in radius (the healer included).
- **Cleanse** — strips curses, anti-heal and roots off an ally.
- **Anti-heal** — a debuff that reduces healing received (`HealReceivedMultiplier`).
- **Root** — holds a target in place (movement → 0) for the duration.
- **De-taunt (stub)** — nearby mobs drop the caster and won't re-aggro it for ~5s.
  A real threat system replaces this later.

> The Ork's **placed healing totem** (and pets/summons generally) is deferred to a
> dedicated subsystem; it ships here as a normal AoE heal. **Warchanter** (the buffer/HoT
> Healer discipline) is the next slice. No DB reset needed for this build.

## Phase 24.0

### 3rd-class framework (disciplines)

The plumbing for the whole 3rd tier — content (real per-discipline skills) lands in
later slices; this build proves the pipeline end-to-end with placeholder skills.

- **Each 2nd class splits into two disciplines at level 40.** A *discipline* is the
  shared identity; *discipline + race* is how it's expressed. 12 disciplines, 36 third
  classes (× 3 races): Tank→**Bulwark/Vanguard**, Warrior→**Ravager/Warlord**,
  Rogue→**Phantom/Venomweaver**, Archer→**Sharpshooter/Trapper**,
  Healer→**Lightbringer/Warchanter**, Nuker→**Magus/Tempest**.
- **Earned by a longer, harder quest chain** than the 2nd class: at level 40,
  **Grandmaster Thorne** sets the *Ordeal* then the *Ascension* — multi-target hunts
  through the high-level zones, capped by **Young Drake** kills (a stepping stone to
  real bosses later). Only offered for the 2nd class you hold, and only one discipline
  per character.
- Each discipline carries a **flat stat lean** (e.g. Bulwark = big +HP/+Def; Magus =
  +MP/+Atk) so the paths already feel different before their skills exist.
- Plumbed through the engine: `Entity.ThirdClass`, a `Discipline` dimension on the skill
  registry, the tiered class-change handler/dialog (gated to the right parent class), a
  level-40 reminder, and a new `ThirdClass` save column.

> **Schema change:** delete `Game.Server/bin/Debug/net8.0/game.db` (+ `-shm`/`-wal`) so
> the new `ThirdClass` column is created. The 4th tier (gold-gated + boss kills) comes later.

## Phase 23

### Class change is now earned through quests
- Every one of the **18 second classes** is unlocked by a **two-quest chain**, generated
  uniformly for all of them (no more level-only popup):
  - **Trial of the &lt;Class&gt;** (lvl 18, **Elder Marius**) — hunt a target, earn the
    **Trial Token**.
  - **Path of the &lt;Class&gt;** (lvl 20, requires the trial, **High Priest Oren**) — a
    second hunt, earn the **&lt;Class&gt;'s Proof**.
  - Bring both to **Class Master Vael** to change class (the proofs are consumed).
- **Offers are gated to what you can actually become**: a quest (and the class-change
  dialog itself) only appears for your **race + base class**, and only before you've
  already taken a second class. No seeing 18 irrelevant options.
- The lvl-20 reminder now **points you at the right NPCs** instead of saying "coming soon".
- Built as a **data-driven generator** (`Quests.ClassChangeChains.cs`) over
  `ClassCatalog.Playable`, so it produces all chains, the 36 quest items, and the 18
  class-change requirements from one loop — the same generator will drive future 3rd/4th
  tiers.

> Quest progress persists (existing columns), but **delete `game.db`** if you want the new
> quest items to seed cleanly on a fresh character.

## Phase 22.2

### Original town names (IP safety)
- Renamed every town away from Lineage-2 names to **original, generic** ones:
  **Brackenford** (starter), **Stonewatch**, **Emberfall**, **Greymarsh**, **Ironreach
  Keep**, **Duskvale**, **Frostmere**. Safe-zone ids and gatekeeper ids were renamed to
  match; nothing persisted referenced the old ids, so **no DB reset**.
- Also scrubbed the L2 currency term ("adena") from the docs — the currency is **Gold**.
- Policy going forward: never reuse names trademarked by other games (towns, NPCs, items,
  skills, currency). Stat *formulas* aren't copyrightable; *names* are.

## Phase 22.1

### Expanded world — zones up to level 80
- The world grew to **24000 × 24000**. The starter town **Brackenford** sits at the centre,
  with **six more towns ringing it** (Stonewatch, Emberfall, Greymarsh, Ironreach Keep,
  Duskvale, Frostmere) — difficulty rises as you tour the ring clockwise from the north.
- **~16 spawn zones cover levels 1-80** (1-2 per band), plus an elite and a boss
  placeholder. Four new higher-tier creatures (**Orc Raider, Stone Golem, Wraith, Young
  Drake**) fill the 20-80 range with their own drop tables; existing mobs cover the low end.
  There's no level cap, so you can climb toward 80 to be ready for class-change quest chains.
- **Every town has a gatekeeper**, so the whole travel network is reachable in both
  directions; teleport fees scale with the (now larger) distances.
- **You respawn at the nearest town** instead of the map centre — important now that the
  world is big.
- Bigger map = room for the next content: bosses, instances, and dungeons.

> No DB reset needed. Existing characters keep their saved position (clamped into bounds);
> walk or use a gatekeeper to reach the new towns.

## Phase 22

### Teleport-for-fee (gatekeepers)
- A **Gatekeeper** NPC stands in each safe zone (Giran / Dion / Aden). Talk to one to
  **pay gold and warp** to any other safe zone.
- The **fee is distance-based** (`GameConstants.TeleportFee` = distance × per-unit rate,
  with a floor), shown on each travel button; you can't afford → button disabled.
- The server validates range, gold, and that the destination isn't your current zone,
  then repositions you (like respawn: set position, clear path, update the grid). The
  client **snaps** the camera/sprite on a large jump instead of sliding across the map.

## Phase 21

### Vendors — NPC shops (buy & sell)
- Two town merchants: **Apothecary Miren** (potions, common buff potions, basic scrolls)
  and **Armsmaster Dolan** (plain F-grade weapons/armor/accessories, starter shield + jewel).
  Talk to one → **Browse Wares** → a shop window with **Buy** and **Sell** tabs.
- Every item now has a gold **Value** (`ItemDef.Value`; filled by an explicit per-item value
  or `ItemCatalog.DefaultValue` formula by grade/rarity/slot). **Buy price** = Value;
  **sell price** = Value × `VendorSellFraction` (30%). Quest items and god-tier one-offs
  have no value → can't be bought or sold.
- Vendor-sold gear is created **plain** (no rolled attributes) — rolled gear still comes
  from drops. The server validates gold, inventory space, range, and that the vendor
  actually stocks the item; it's all single-writer on the game loop.
- **Castle hook (not yet active):** `VendorBuyTaxRate` is wired into the buy price so a
  future castle-owned village can add a surcharge that flows to the castle vault. It's 0
  for now (no castle system yet).

## Phase 20

### Buff potions (a few buffs without a buffer)
- A new **buff-potion** type: drinking one applies a **timed buff** (weaker than a real
  class buff), so a solo / non-buffer character can still grab a couple. They **ignore the
  healing-potion cooldown** and don't heal.
- Three lines, each in **three rarity tiers** (rarity = strength + duration; a rarer one
  supersedes a weaker of the same line):
  - **Swiftness** — +15 / +20 / +30 Move Speed
  - **Focus** — +8% / +12% / +20% Cast Speed
  - **Haste** — +8% / +12% / +20% Attack Speed
  - Durations 60s / 90s / 180s (the rare lasts longest).
- Tooltips show the effect; the debug menu grants them.

### Scrolls & buff potions now drop
- **Attribute (reroll) scrolls** now drop from monsters — rare to find, like enchant
  scrolls — editable per mob in `MobCatalog`. Higher tiers are weighted to higher-level
  spawns (e.g. rare reroll scroll only at level 20+).
- **Uncommon/Rare buff potions** drop too; the **common** tiers are reserved for vendors
  (next phase). So drops give the strong buffs, vendors the basic ones.

### UI: tabbed debug menu + settings + class-change via quest
- The **debug menu** is now split into three tabs — **Equip** (weapons / armor / sets),
  **Consumables** (scrolls / potions), and **Functions** (Level +1, Class Change test) —
  instead of one long scroll.
- The old top-bar **Class** button is replaced by a **Settings** button (menu: *Character
  Selection* — leave the world, save, and pick another character without re-logging in;
  *Exit Game*).
- **Class change now belongs to a quest.** The direct class-change picker moved into the
  debug *Functions* tab (test bypass). Normal players hitting level 20+ without a second
  class get a one-time popup pointing them at the (not-yet-built) class-change quest — a
  temporary stub to be removed once those quests exist.

## Phase 19

> **Delete `game.db` before running** — characters gained a **Gold** column;
> `EnsureCreated` won't add it to an existing DB, so reset it (saves recreate fresh).

### Gold — a currency wallet
- Characters now have a **gold wallet** (persisted). Mobs **drop gold on every kill**,
  scaled by mob level × `RateConfig.GoldAmountRate` with a small ±20% variance
  (independent of the item drop table).
- The balance shows in the **status line** (e.g. *“Lv5 • 1,234 Gold”*) and syncs on
  login and whenever it changes.
- The currency name is **generic on purpose** (no IP) and centralised in
  `GameConstants.CurrencyName`, so it can be rebranded in one place.
- This is the foundation for the roadmap's **vendors** (buy/sell) and **teleport-for-a-fee**
  — the wallet gives the reroll scrolls and potions a real source/sink next.

## Phase 18

### Armor-weight masteries (wear what your class trains in)
- Your class is **trained in an armor weight**. Wearing the **body** piece of that weight
  grants a **bonus** (your class identity); wearing an **untrained** heavy/light body
  applies a **penalty**. **Robe never penalises**, and **Tanks/Warriors take no penalties**.
- The trained weight follows the class tree (base → second class):
  - **Mage → Robe** (cast speed, MP regen, max MP); **Nuker** keeps robe (stronger, +interrupt
    resist + magic def); **Healer → Light** (cast + atk speed, regen, a little acc/eva — so it
    can melee).
  - **Fighter → Light** (attack speed, HP regen, acc/eva); **Rogue → Light** (more atk/move
    speed, eva/acc); **Archer → Light** (atk speed + crit); **Tank → Heavy** (max HP, regen,
    big Def + magic resist, no penalties); **Warrior** trains **both heavy and light**.
- **Penalties** for the wrong weight: a mage in **heavy** is crushed (≈½ attack/cast/move
  speed and regen, −10 acc/eva); a fighter in heavy is milder (×0.8 speeds, −3). Wearing
  **light untrained** is a lighter ×0.8 / −3.
- The **Stats window** shows your current status (e.g. **“Robe Mastery”** or
  **“Heavy — untrained”**) so the effect is visible.
- Driven by class + archetype for now (encodes the class-change evolution); a **learnable,
  leveled mastery-skill layer** (spend SP, ranks) comes later, like the other passives. No
  DB reset needed.

## Phase 17

### Named armor sets with a set bonus
- Armor pieces can belong to a **named set** (`ItemDef.SetId`). Wearing **all four armor
  slots** (Head / Body / Gloves / Boots) of the same set grants that set's **bonus** on
  top of each piece's own stats and rolled attributes.
- A set can offer **several body weight variants** that share the same accessories — so a
  set has a *heavy* and a *robe* body, both completed by the same helm/gauntlets/boots.
- **Dark Dominion** ships as the first set: a **Plate** (heavy) or **Robe** body + shared
  Helm / Gauntlets / Sabatons. Full set → **+150 HP, +80 MP, +25 Def, +18 Atk** (feeds
  both physical and magic), **+6 Acc, +6 Eva** (tune in `ArmorSetCatalog`).
- The **Stats window** shows **“Set Bonus: <name> (complete)”** when active, and an item's
  **tooltip** lists the full-set bonus so you know what you're collecting toward.
- Sets are defined in `Game.Shared/ArmorSets.cs`; the debug menu grants both Dark Dominion
  variants. Set pieces are obtainable via debug for now (boss drops later). No DB reset
  needed this build.

## Phase 16.1

> **Delete `game.db` before running** — armor keys changed again
> (`<weight>_<slot>_<grade>_<rarity>`, Legs slot removed). A fresh DB hands new
> characters the new starter set.

### Fewer armor slots: one weighted body + weightless accessories
- Trimmed from five slots to **four** — **Head / Body / Gloves / Boots** (legs merged
  into one **full-body** piece) to cut the number of generated items (~90 → ~36).
- **Only the Body piece carries weight** (Heavy/Light/Robe) and the bulk of the defence.
  **Head / Gloves / Boots are weightless accessories**, generated once and **shared across
  all builds** (so a "set" can reuse the same accessories and differ only in its body
  armor by weight).
- **Per-slot attributes** (grade/rarity sets the *value*, not the count):
  - **Body** rolls **2** attributes from its weight pool — so it wants **Uncommon+**
    attribute scrolls (lock 1, reroll 1), not just Common.
  - **Head** rolls HP/MP regen · **Gloves** atk-speed/cast · **Boots** move-speed/eva —
    **1 each**.
- New characters start with **body + the three accessories**; the debug menu grants Rare
  body+accessory sets per weight.
- Still to come (the set phase): **named sets** with a **set bonus** (+CON/ATK, +max
  HP/MP) for wearing a matched body+accessories, and **armor-weight masteries**.

## Phase 15

### Attribute reroll scrolls (keep the item, fix the rolls)
- A new scroll family lets you **reroll an item's rolled attributes** instead of
  tossing a good grade/rarity piece because its stats are weak. Each reroll
  re-randomises the **unlocked** slots (both stat type and value, from the item's
  Phase-14 pool).
- **Lock-by-tier** — the scroll's rarity decides how many slots you can **keep**:
  - **Common** — lock 0 (reroll all)
  - **Uncommon** — lock 1
  - **Rare** — lock 2
  - **Legendary** — rerolls **all** slots and forces each to its **MAX** value (for a
    legendary item whose every stat should be maxed).
- **How to use it:** click the **⟳** button on any gear with attributes → the reroll
  popup lists each attribute with a **lock checkbox** → tick the ones to keep → pick a
  scroll. The server clamps your locks to the scroll's capacity, consumes one scroll,
  and re-applies stats live if the item is equipped.
- Enchant and attribute scrolls are now **distinct families** (an attribute scroll can't
  be used to enchant and vice-versa). Debug menu grants all four attribute scrolls.
- Drop chances aren't tuned yet — for now the scrolls come from the **debug menu**.

## Phase 14

### Rolled attributes now depend on the item, not just its grade
- **Which** attributes can roll is decided by the **weapon type / armor weight**, not a
  flat grade pool. **How big** a roll is still scales with **grade**; **how many** still
  comes from grade + rarity.
- **Pools** (`AttributeSystem.WeaponPool` / `ArmorPool`):
  - **Sword** as/atk/crit-rate · **Blunt** hp/atk/cast/crit-dmg · **Bow**
    crit-rate/crit-dmg/as/atk · **Dual** crit-rate/crit-dmg/move-speed/eva.
  - **Heavy** hp/as/hp-reg/acc · **Light** the versatile set (eva/acc, hp+mp regen,
    hp/mp, as/cast) · **Robe** cast/mp-reg/max-mp.
- **Five new attribute types**, all feeding real stats: **Accuracy**, **HP Regen**,
  **MP Regen** (flat), **Crit Rate** and **Crit Damage** (percent). Crit-rate from gear
  adds **on top of** the weapon crit factor; crit-damage raises your crit multiplier.
- Flat attributes (accuracy, regen) display **without** a `%`; percent ones keep it.
- Groundwork for **Phase 15 — attribute reroll scrolls** (lock-and-reroll toward each
  stat's max), so a good grade/rarity item is worth keeping and grinding, not tossing.

## Phase 13

> **Delete `game.db` before running** — a staff's item key changed
> (`staff_*` → `blunt_*`), so existing mage starter staves won't resolve; a fresh
> DB regenerates correct starter gear.

### Magic defence is its own channel
- **New magic-defence stat**, fully separate from physical defence: magic damage now
  divides by **`MagicDefence`**, not physical `pDef`. Base = **`level / 2`** (the
  physical formula minus the CON term — magic defence does **not** scale with any base
  stat).
- **Only JEWELS raise magic defence.** New **`EquipSlot.Jewel`** + an item `MDefBonus`;
  two starter jewels seeded (Brass Amulet, Silver Talisman). One jewel equips for now,
  built to expand to the L2 five-slot layout later. M.Def shows in the Stats window and
  the equip-comparison popup.
- **Tank "Anti Magic"** (archetype passive) adds extra magic defence on top of the base.

### WIT is purely a combat-utility stat
- WIT still drives **magic crit**, **cast speed**, and **interrupt resist** — and now
  also **offensive magic-interrupt power** (`wit·2`), so a **WIT mage out-interrupts an
  equal-level ATK mage** while the ATK mage hits harder. WIT adds **no** magic damage.

### Magic fail — floor and ceiling
- A spell can always fizzle (**≥1%**), scaling up by level gap to **90%** (was 80%).
- The **target** can raise the fail **floor** against itself: **Tank ~10%, mages ~5%**
  — so casters always have a real chance to fail against the prepared.

### Interrupts
- **Rogue basic attacks** now carry magic-interrupt power (`50 + level`) — daggers
  disrupt casters. Other archetypes' basics still don't interrupt.
- New **Disrupt** skill (Tank kit): **instant cast**, overwhelming interrupt power, so
  it **always breaks** an enemy cast.

### Weapon system — Blunt, one/two-handed, shields
- **`Staff` is gone — a staff is just a 2H Blunt** (`WeaponType.Blunt`). Blunt =
  **higher accuracy, lower crit** than bladed weapons.
- **One- vs two-handed** (`WeaponHands`) is now a real property. **A 2H weapon occupies
  the offhand**, so equipping a 2H weapon and a shield are **mutually exclusive** (one
  drops the other).
- **Per-weapon crit factor** (Sword ×0.80, Dual/Bow ×1.20, Blunt ×0.40) shapes crit by
  weapon; **Blunt also gets +accuracy** — the high-acc/low-crit identity.
- **1H magic blunts** let a mage trade a staff for **mace + shield**: hand-added
  **Iron Mace** (physical, shield-ok) and **Ash Wand** (1H magic blunt, mAtk > pAtk).
- **Daggers are consistently `Dual`** (no phantom `Dagger` type); fixed a mob drop that
  referenced a non-existent dagger key.

## Phase 12

> **Delete `game.db` before running** — characters now store shield-related
> equipment state correctly only on a fresh DB if you hit schema issues; safe to
> reset.

### Shields & block
- New **Shield** equip slot + item type with several values: **BlockChance**,
  **BlockReduction%**, **ShieldDefense**, **ShieldCritDefense**, **EvasionPenalty**.
  Two shields seeded (Wooden F, Iron E). Any class can equip a shield.
- **Block resolution** (physical only): the shield first lowers the attacker's
  **crit chance**; if it still crits, the **crit ignores the shield**; if it
  doesn't crit, **roll block** → on a block, damage is cut by the shield's flat
  **% reduction**. Shown as a "Block" hit on the client. **DEX does NOT affect
  block** — it's flat + passives.
- **Shield Mastery** (tank skill) scales the shield's block chance and defence —
  but only while a shield is equipped, so a buffed shield on a mage is still weak
  while a passive-stacked tank becomes a wall.
- Skills can carry **BlockAccuracy** to bypass blocks (most physical skills should).
- **Magic is not blocked** — it's mitigated by defence only, so mages aren't
  buried under fail + interrupt + block.

### Combat-feel fixes
- **Damaged mobs now aggro and chase** their attacker even when hit from range
  (the "cast from range, mob ignores you and regens" bug).
- **Magic weapons have no weapon range** and tiny basic-attack damage — a staff is
  useless as a melee poker, so you actually cast. Only **bows** have basic range.
- **Skill ranges scale by class tier**: magic **500 / 750 / 900** (lvl 1-20 /
  21-40 / 40+), bow skills **350 / 600 / 900**. Archer **basic-attack** range grows
  by tier too (400 → +200 → +500).
- **Faster casts**: Magic Bolt **2s**, Flame/Holy/Heal quicker; **instant debuffs**
  (Weakness 0.5s cast, 15s duration, 30s cooldown).
- **HP Boost ranks replace lower ranks** — learning rank 3 removes ranks 1 & 2 from
  your learned skills, and the active buff supersedes by rank.
- **Daggers are treated as Duals** (`WeaponType.Dual`) consistently.

### For Claude Code
- Added **`CLAUDE.md`** at the project root — full architecture, conventions, and
  design decisions so Claude Code starts with context. Install Claude Code with the
  native installer (`curl -fsSL https://claude.ai/install.sh | bash`, or the
  PowerShell one-liner on Windows), `cd` to the project, run `claude`. It can run
  `dotnet build` and fix real compile errors directly.

## Phase 11

### Casting commits you (root)
- Starting a cast **roots you** — you can't move until it finishes or you cancel.
  Range is checked at cast **start** only; once it begins, the spell **lands even
  if the target moves**. This removes the old move-cancel/recast loop.
- **ESC** cancels your own cast and starts its cooldown (you chose to bail).

### Interruption is a stat contest (not automatic)
- Being hit mid-cast **rolls** an interrupt, like accuracy vs evasion:
  **caster InterruptResist** (WIT-based stat + the skill's `InterruptDefense`)
  vs **attacker InterruptPower** (0 for normal hits + the attacking skill's
  `InterruptPower`).
- **Enemy interrupt = cast stops, NO cooldown** — you keep the MP loss and can
  retry immediately (so a 60s-cooldown ultimate isn't wasted by one unlucky hit).
- Per-skill tuning: `InterruptDefense: 99999` = effectively **uninterruptible**
  (ultimates); `InterruptPower: 99999` on an instant skill = a reliable
  **interrupt skill**. Both default 0 (use the character stat). Hooks reserved
  for gear/buff interrupt-resist later.

### Two-stage MP cost (toggle-skill groundwork)
- A skill can charge `InitialMpCost` at cast **start** and the remainder on
  **completion** (default: all on finish, so existing skills are unchanged). On
  cancel/interrupt you've paid the initial but not the finish — groundwork for
  toggle skills (initial cost + per-second upkeep) later.

### Cast & attack speed (L2-style 333 = 100%)
- New speed model: a stat where **333 = 1.0×**, higher = faster. **WIT drives
  cast speed**, **DEX drives attack speed**, with **per-class weights** (mage WIT
  ~5%/pt, fighter ~3%/pt) and **weapon base speeds** (dagger fast, bow slow,
  staff caster-normal). Approximated from the L2 tables — tune in
  `StatCalculator` (`CastSpeedStat`, `AttackSpeedStat`, weapon base speeds).
- Capped via `StatCaps` (cast 1999 ≈ 6×, attack 1500 ≈ 4.5×). WIT now makes a
  mage a **faster caster** (and magic-crit-prone), not a bigger nuker.

## Phase 10.1

### Level-banded drops
- `DropEntry` gained an optional **level band** (`MinLevel`/`MaxLevel`, 0/0 = any
  level). A drop only rolls when the mob's spawned level is in range — so **one
  creature can drop different loot at different levels** (e.g. `grey_wolf` drops
  common potions at any level but a better armour only at level 15+).
- This is a **superset** of the L2 approach: you can still author the pure-L2 way
  (distinct creature per level tier, no bands) AND the flexible way (one creature,
  level-varying loot), and mix them freely. The level check costs a couple of
  integer comparisons per drop entry — negligible next to the network send on a
  kill, so choose between styles on design clarity, not performance.

## Phase 10

### Placed safe zones (cities/castles)
- The single center safe zone is now a **list of placed zones with ids** in
  `WorldMap.SafeZones` (Town of Giran, Town of Dion, Aden Castle seeded). Each has
  a stable id so **teleports-for-a-fee** can target them later. `InSafeZone` now
  checks the whole list; all are drawn and labelled on the map.

### Server rate multipliers (`RateConfig`)
- One place to tune progression speed: **ExpRate, SpRate, DropChanceRate,
  DropAmountRate** (adena rate reserved for the currency phase). Defaults are set
  for fast testing (**x10 exp, x3 drop chance**) — set them to 1 for live.

### Mobs are now templates with per-mob drop tables
- Mobs are **distinct creatures by id** (`grey_wolf`, `brown_boar`, `dire_boar`,
  `green_slime`, `cave_spider`, `road_bandit`) in `MobCatalog`, each with its own
  **drop table**: `DropEntry(itemId, chance (float), minQty, maxQty)`. The same
  item can drop at different chances/amounts from different mobs.
- **Level lives on the ZONE, not the mob.** A mob template has no fixed level —
  the spawning zone assigns it (stats derive from that level), so the same
  creature appears at any level with the same drops. Want different loot? Make a
  new mob id. Want it tougher elsewhere? Spawn it in a higher-level zone.
- Zones now list **mob ids** instead of generic names. Drop chance/amount are
  scaled by the server rates on top of each entry's own values.

### Skill SP costs rescaled (L2 scarcity)
- Learnable skills now cost **hundreds–thousands of SP** (HP Boost 1000/3000/8000,
  Wind Walk 1500, Mass Wind Walk 5000) so the SP economy forces **prioritization**
  — you can't learn everything at once; you farm and choose. The SpRate multiplier
  makes testing fast without changing that balance.

### Where to tune
- **Cities:** `WorldMap.SafeZones`. **Rates:** `RateConfig`.
- **Mobs + drops:** `MobCatalog` (templates + drop tables). **Zones:** `WorldMap.SpawnZones` (mob ids + level band).
- **SP costs:** each skill's `SpCost` in `Skills.cs`.

## Phase 9

### Damage is now a ratio, not a subtraction
- Old model was `max(atk - def, 0)` — a wall once defence ≥ attack. **New model
  is L2-style ratio damage**: `K · (atk · lvlMod + power) / def`. Defence gives
  **diminishing returns** (never fully blocks), attack always does something, and
  damage **scales smoothly with level** via `lvlMod = (level+89)/100`.
- **Weapon variance**: each hit rolls a ± band by weapon type (bow/dagger spiky,
  blunt steady), so hits aren't identical.
- Tuning lives in `StatCalculator` (`PhysicalK`, `MagicK`, the formulas).

### Two damage channels (physical vs magic)
- **One power stat (ATK)** feeds **both** `pAtk` (physical) and `mAtk` (magic) —
  no separate INT. **Weapons decide the split** via a new **`MAtkBonus`**: a staff
  is mostly mAtk, a sword mostly pAtk, and **hybrid weapons are possible**
  (a weapon can give both).
- **Physical** can be **evaded** and crits up to **×10**. **Magic** can **fail**
  (reduced damage, not zero) and crits up to **×3** — the spiky mage feel. Magic
  currently mitigates against physical defence; magic-resist passives/jewels come
  later.

### Split, capped crits
- **Physical crit rate ← DEX** (cap **50%**); **magic crit rate ← WIT** (cap
  **20%**). So a high-WIT mage is a **fast, crit-prone caster, not a bigger
  nuker** — WIT buys crit frequency and cast speed, not raw power.
- Crit-damage caps: physical **×10**, magic **×3**. All caps in `StatCaps`.
- The Stats window now shows **P.Atk / M.Atk** and **Crit (Phys / Magic)**.

### Tuning notes
- Mob **defence growth was slowed** so attack outpaces it as you level (otherwise
  the ratio stays flat). Players stay tankier than mobs.
- Adjust feel via `StatCalculator.PhysicalK` / `MagicK`, weapon `mAtkFraction`
  (in `ItemCatalog`), and the crit caps in `StatCaps`.

## Phase 8

### Movement states (Run / Walk / Sit)
- Players have three movement states: **Running** (full speed), **Walking**
  (half speed, **+20% HP/MP regen**), and **Sitting** (can't move, **+80%
  regen** — sit to recover MP fast).
- **Z** toggles sit/stand, **X** toggles walk/run; the state shows under the
  clock. Walk↔run is instant; **getting hit while sitting** breaks the sit and
  triggers a short **stand-up delay** before you can move/cast again.
- Regen is a multiplier stack, so future passives/toggle skills can add to it
  (e.g. "+20% HP regen while sitting").

### Per-race+class speeds, with a cap
- Base **run speed** now depends on **race + class** (Elf fastest, Human slowest;
  within a race, fighters/rogues beat mages). Gear (`SpeedPercent`) and buffs
  raise it toward the **move cap of 250** (a normal player's buffed ceiling).
- The cap is **per-entity and raisable** (`MoveSpeedCap`), so a future rogue
  ultimate can briefly exceed 250 and outrun even a buffed mage.
- Central **`StatCaps`** holds all ceilings (move 250; attack-speed 1500 and
  cast-speed 1999 reserved for the casting round; crit 50%).

### Mob movement fixed
- New **`MobCatalog`**: each mob type has **walk** and **run** speeds (e.g. Wolf
  80/150, Bandit 60/108) and an aggressive flag. Mobs **walk while wandering,
  run when aggroed** — so players can kite, and a fighter outruns a bandit while
  a fast wolf still threatens a slow mage.
- **Wander is clamped to the mob's zone** — they no longer drift into
  neighbouring zones. Overlap same-level zones deliberately to mix mobs.

### Class change adds flat stats (identity)
- A class change can now grant **flat secondary bonuses** (e.g. a tank gets flat
  +Def/+HP), not just primary stats — primary stats stay reserved for the future
  dye/tattoo/set layer. Structure is wired; **Cleric** seeded as the example
  (+MP/+HP/+Def). Fill in other classes in `Classes.cs`.

### Where to tune
- **Speeds:** `SpeedTable` (players) and `MobCatalog` (mobs).
- **States/regen:** `MovementTuning`. **Caps:** `StatCaps`.
- **Class flat bonuses:** `ClassFlatBonus` on each `SecondClassDef` in `Classes.cs`.

## Phase 7

> **Delete `game.db` before running** — characters now store quests (new columns).

### NPCs you can talk to
- Stationary **NPCs** (gold dots, labelled `[Talk]`) are placed from
  `WorldMap.Npcs`. Click one (within range) to open a **dialog window** showing
  the quests they offer, quests ready to turn in, in-progress status, and (for
  class-change NPCs) class-change options.
- Three NPCs near town: **Elder Marius** and **High Priest Oren** (quest givers)
  and **Class Master Vael** (class change).

### Quests + the quest log
- Quests have ordered **steps** (talk / kill N mobs / collect / reach level),
  **rewards** (exp, SP, items), a **MinLevel**, and an optional
  **`RequiresQuestId`** so quests form **chains**. Kill steps advance as you kill
  matching mobs; talk steps advance when you visit the NPC.
- **Quest log** (press **J**) shows active quests and per-step progress. Quests
  persist across logout.

### Item-gated class change (the Cleric chain)
- The first worked chain, **Human Mage → Cleric**:
  1. **A Test of Devotion** (Elder Marius, lvl 18): talk → kill 5 Spiders →
     return → rewards the **Mark of Faith** (quest item).
  2. **The Cleric's Path** (High Priest Oren, lvl 20, needs chain 1): talk →
     kill 8 Wolves → return → rewards the **Cleric's Proof**.
  3. Bring both proofs to **Class Master Vael** → **become a Cleric** (items
     consumed). Different target class = different chain/items.
- The debug-menu class-change button still works (bypasses items, for testing).

### Quest items + a Quest inventory tab
- **Quest items** are non-droppable and non-tradeable, shown in a **separate
  "Quest Items" tab** in the inventory (toggle Gear / Quest Items).

### Where to author quests (the designated place)
- All quest content lives in **`Game.Shared/Quests/`**: `Quests.Root.cs`
  registers the chains, and per-chain files like `Quests.HumanMageCleric.cs`
  declare the quests, rewards, and the class-change requirement in one place.
  Class-change item requirements are in the `ClassChangeRequirements` table.
  Replicate the Cleric file for Sorcerer, Orc lines, etc.

## Phase 6.1

### Same skill, different name/icon per class
- A shared skill keeps **one id, one effect, one BuffKey** but can show a
  **different name (and, later, icon) per class** — set on the class's
  registration: `new ClassSkill(WindWalk, 20, DisplayName: "Holy Speed")`.
- So 10 classes can all use `wind_walk`; each sees its own label on the **skill
  bar, buff bar, and skills window**, while mechanically it's one buff that
  `improved_movement` replaces with a single `Replaces` entry. The buff bar shows
  the **casting class's** name (a cleric's buff reads "Holy Speed").
- Example: the Human Cleric's Wind Walk displays as **"Holy Speed"**.

### Party (area) buffs
- `SkillDef` gained a **`TargetMode`**: `SelfOrTarget` (default), `SelfOnly`, or
  `AlliesInRadius`. An area buff hits the **caster + nearby player characters**
  within `AreaRadius` (a stand-in for real party groups, which come later).
- Added **Mass Wind Walk** (id `mass_wind_walk`): same effect and **same BuffKey
  (`wind_walk`)** as the single-target version, but buffs nearby allies for more
  MP and a longer cooldown. Because it shares the BuffKey, `improved_movement`
  (or any `Replaces: ["wind_walk"]`) supersedes it too — one entry covers both
  the single and party versions. The Cleric's party version shows as
  **"Holy Procession"**.

### Design note (ids vs structure)
- **Skill ids stay flat and shared** (`wind_walk`, `holy_strike`) — that's the
  ability's identity, so stacking/replace logic stays simple and a buff shared by
  many classes needs only one `Replaces` entry.
- **The class tree's structure lives in `RaceAndClasses/`** — which class learns
  which skill, at what level, and under what display name. Per-class *uniqueness*
  (a genuinely different ability) gets its own flat id; per-class *flavour* (a
  rename of a shared skill) is just a `DisplayName` on the registration.

## Phase 6

> **IMPORTANT — delete any old `game.db` before running.** Skill ids changed
> from ints to strings and characters now store learned skills + skill points,
> so the schema changed. Delete `game.db` (in `Game.Server/bin/Debug/net8.0/`)
> and a fresh one is created on launch.

### Skills are now learned with Skill Points
- Skills must be **learned** before use. You earn **Skill Points (SP)** alongside
  exp (≈ 1/4 of exp; tune `GameConstants.SkillPointRatio`).
- The **Skills window (K)** now has **two tabs**:
  - **Learned** — your usable skills, grouped by category (Physical / Magic /
    Buffs / Debuffs / Heals), each with a **To Bar** button.
  - **Skills to Learn** — unlearned skills **grouped by required level**, with a
    **Learn** button that's enabled only when your level + SP (and previous rank,
    for ranked lines) allow. Clicking Learn opens a **confirm popup** showing the
    description, details, and **SP cost in green/red**; confirm to learn it,
    after which it moves to the Learned tab and can be dragged to the bar.
- Hovering a skill shows its description + MP/cast/cooldown/duration.
- The **core class kit** (the mandatory upgrades like Greater Heal) is granted
  **free** on class change / level-up; the **extras** (HP Boost ranks, Wind Walk)
  are the ones you spend SP to learn. Learned skills + SP **persist**.

### String skill ids + per-class skill files
- Skill ids are now **stable strings** (`magic_bolt`, `greater_heal`,
  `hp_boost_1`). Same benefits as item keys: readable, reorder-safe,
  collision-guarded at startup.
- **One place to manage class skills:** `Game.Shared/RaceAndClasses/`. Each
  partial file registers a race+class line's skills with learn-levels, e.g.
  `Classes.Human.Mage.cs` declares the Human cleric/sorcerer learnable skills.
  Adding a skill to a class is a one-line `ClassSkills.Register(...)` edit.
- Example HP Boost line (3 ranks at 40/56/72 style levels) and Wind Walk are
  authored there to show the pattern; ranked skills must be learned in order.

### God race + God items (debug)
- A **God race (enum 99)** is creatable **only in DEBUG builds** but fully usable
  once made, with two God second classes (Demigod / Ascendant).
- Removed `legendary_windforce`; added two **God-tier** items (debug menu):
  **God's Judgment** (sword, attack + range 1000, all 8 attributes at 100%) and
  **God's Robes** (def/hp/mp/eva 1000, all armor attributes at 100%).

### New rarities & attributes
- Rarities extended: **Epic (3), Legendary (4), God (99)** — higher rarities roll
  more attributes.
- Two new attributes: **Evasion %** and **Defence %**, available on **E-grade and
  up** gear, and they apply to your real stats.

### Quest groundwork (data types only)
- Added quest **data types** (`QuestDef`, `QuestStep`, `QuestReward`,
  `CharacterQuestState`) and a nullable **`RequiredQuestId`** hook on second
  classes — so class-change-by-quest drops in later without a refactor. The live
  quest system (NPCs, dialog UI, tracking) is a **future phase**; an
  `EntityKind.Npc` is reserved for it.

## Phase 5.4

### Buff system rebuilt for a future buffer class
- **`SkillEffect` is now a `[Flags]` enum.** One skill can carry several effects
  at once: `Effect = BuffAtk | BuffMoveSpeed | BuffCastSpeed`. No more inventing
  a new enum member per combination — add a flag once and combine freely.
- **Per-effect magnitudes with flat OR percent.** A skill carries
  `EffectMagnitude[]`, each entry `(Effect, Value, Mode)` where Mode is
  `Flat` or `Percent`. So Wind Walk = `(BuffMoveSpeed, 33, Flat)`, a haste buff =
  `(BuffMoveSpeed, 0.30, Percent)`, and you can even put **both on one buff**
  (33 flat + 5%). Stats combine as **`(base + ΣFlat) × (1 + ΣPercent)`** per stat.
- **Working cast-speed, attack-speed, and evasion buffs** (not just from items
  now) — a buffer skill can buff them directly.

### Buff stacking rules (exactly two mechanisms)
- **Explicit `Replaces` (unconditional):** a buff lists buff keys it overrides,
  e.g. `improved_movement` with `Replaces = ["wind_walk", "agility"]`. Casting it
  removes those buffs **no matter their rank or magnitude** — the author declared
  the override.
- **Same `BuffKey` compares by `Rank`:** recasting the same buff applies only if
  the incoming `Rank ≥ existing Rank` (a full replace, refreshing duration).
  A **weaker** recast does nothing — no downgrade, no refresh. Equal rank = refresh.
- Unrelated buffs (different key, not in a `Replaces` list) simply **stack**.
- Current skills use this already: War Cry (`might` rank 1) and Greater War Cry
  (`might` rank 2) auto-supersede by rank; Weakness/Greater Weakness likewise
  (`curse_def` rank 1/2); Battle Fury is a two-effect buff (atk + move speed).

### How to author a buff (for the future buffer class)
```csharp
new(skillId, "Improved Movement", BaseClass.Mage,
    SkillEffect.BuffMoveSpeed | SkillEffect.BuffEvasion,
    MpCost: 30, CastTicks: 20, CooldownTicks: 20, Range: 600, Power: 0,
    DurationTicks: 1200, BuffKey: "improved_movement", Rank: 1,
    Replaces: new[] { "wind_walk", "agility" },
    Magnitudes: new EffectMagnitude[]
    {
        new(SkillEffect.BuffMoveSpeed, 40, ModifierMode.Flat),
        new(SkillEffect.BuffEvasion,   10, ModifierMode.Flat),
    },
    Description: "Combines and improves Wind Walk and Agility."),
```

## Phase 5.3

### In-game day/night clock
- Time of day now cycles. The **one speed knob** is `GameClock.TimeScale` in
  `Game.Shared/GameClock.cs` — in-game seconds per real second. Default **6**
  (a full game day = 4 real hours; day and night ~2h each). For testing, set it
  to **60** (full day in 24 real minutes) or **600** (~2.4 min) to watch night
  fall fast. An in-game **clock + Day/Night indicator** shows at the top of the
  screen.

### Population cap + respawn delay (no more instant respawns)
- Each spawn zone now keeps **up to `MaxCount` mobs alive and never exceeds it**.
  When a mob dies, the zone waits a delay rolled from **`RespawnSeconds ±
  RespawnVariance`** (real seconds), then respawns — only if under the cap.
- The mob is removed on death and the **zone schedules** the replacement (the
  performant approach). A cosmetic corpse-fade can be layered on later.

### Elites & bosses
- A zone has a **`Rank`** (Normal / Elite / Boss). Elites are tougher (×4 HP,
  ×1.5 attack) with ~minutes respawn; bosses much tougher (×20 HP, ×2.5 attack)
  with hours-long respawn. Authoring example (already in `WorldMap.cs`):
  - **Elite**: `RespawnSeconds: 120, RespawnVariance: 30` → "2m 0s ±30s".
  - **Boss**: `RespawnSeconds: 21*3600, RespawnVariance: 3*3600` → "21h ±3h".
- **Boss/elite respawn timers are persisted** (real-world time) to the database,
  so a long timer **survives a server restart** — kill the boss, restart the
  server, and it's still on cooldown.
- On the map, elite zones are **amber** and boss zones **purple**, each labelled
  with rank, level, and the **[X ±Y] respawn** range.

### Day-only / night-only zones
- A zone's **`Active`** is `Always` (24h, default), `Day`, or `Night`. To swap
  mobs at dusk/dawn, overlap two zones at the same spot — one `Day`, one
  `Night` (there's a worked example in `WorldMap.cs` at 7500,9500). When the
  phase flips, inactive zones despawn and newly-active ones fill in.

### Where to edit
- **Speed of time:** `GameClock.TimeScale`.
- **Everything spawn-related:** `WorldMap.SpawnZones` — `MaxCount`,
  `RespawnSeconds`/`RespawnVariance`, `Rank`, `Active`, level band, mob types.

## Phase 5.2

### The world is now visible and editable from one file
- **`Game.Shared/WorldMap.cs` is the single source of truth** for world layout —
  the server (spawning, collision) and client (drawing) both read it. To reshape
  the world you edit this one file.

### World border
- The playable rectangle is drawn as a **dashed outline**, so the edge is
  visible instead of an invisible wall. Defined by `WorldMap.Border`.

### Roads
- **Thick, semi-transparent grey strips** lead from town toward the hunting
  grounds; **mobs don't spawn on roads**, giving safe-ish corridors. Each road
  is a list of points with a half-width in `WorldMap.Roads` — add or reshape a
  road by editing its point list.

### Spawn zones (visible + self-documenting)
- Each spawn zone is drawn as a **light semi-transparent red disc** with a
  **label showing its level band and mob types**, so you can see at a glance
  where things spawn and what you'll meet. (Placeholder colour until real
  environment art.)
- **Fully editable** in `WorldMap.SpawnZones`. Your example —
  *"at (1000,1000) radius 800 spawn level 5-7 boars and spiders"* — is one line:
  ```csharp
  new(X: 1000, Y: 1000, Radius: 800, MinLevel: 5, MaxLevel: 7,
      MobTypes: new[] { "Boar", "Spider" }, MobCount: 10),
  ```
  The server spawns each zone independently (random point in the disc, avoiding
  the safe zone and roads), picks a random mob type and a level in the band, and
  the client tints + labels it automatically. Add as many zones as you like.

### How spawning works (for editing)
- On startup the server loops every `SpawnZone` and spawns `MobCount` mobs in
  it. Each mob remembers its home point and wanders/leashes around it; on death
  it respawns at home after the respawn timer. Change a zone's numbers and both
  the spawn behaviour and the on-screen overlay update together.

## Phase 5.1

> **IMPORTANT — delete any old `game.db` before running.** Item IDs changed from
> integers to string keys, so the database schema changed. Delete the `game.db`
> file next to the server (or just let this fresh build create a new one). Old
> saves are not compatible.

### Item IDs are now stable string keys
- Every item has a permanent **string key** (e.g. `sword_e_rare`,
  `robe_f_common`, `potion_minor`) instead of a fragile integer. Keys are the
  item's identity — stored in saves, referenced by loot tables and the debug
  menu. **You never renumber**; new items just get new keys, and you can place
  them anywhere in the file. A **duplicate-key guard** at startup throws a clear
  error naming the collision instead of a cryptic crash.

### Full weapon & armor matrix
- Weapons are generated for **every type × grade × rarity**: sword, dual,
  bow, staff × {F, E} × {common, uncommon, rare} — keys like
  `bow_e_rare`. Armor likewise: heavy, light, robe × grade × rarity.
- **All classes can equip any weapon**; your skills determine whether a given
  weapon is actually good for you (matches the design doc). Bows/staves carry
  range; staves add MP; daggers are lower per-hit but suit the rogue's crit kit.
- Loot tables and starter gear now reference these keys; mages start with a
  staff + robe, fighters with a sword + leather.

### Legendary one-off
- **Windforce** (`legendary_windforce`): an E-grade bow with **5 fixed
  attributes** (Attack +30%, Attack Speed +25%, Move Speed +20%, HP +30%,
  MP +20%). Spawn it from the debug menu. Fixed attributes never reroll, unlike
  normal drops.

### Debug menu (DEBUG builds)
- Level +1; Windforce; a **Rare E of each weapon** (sword/dual/bow/staff);
  a **Rare E of each armor** (heavy/light/robe); and **x10** buttons for every
  scroll and potion (no more clicking one at a time). No shield yet — that
  arrives with block mechanics.

### War Cry split by class
- **Rogue & Archer**: War Cry becomes **Battle Fury** — +20% Attack **and**
  +15% Move Speed for 30s.
- **Warrior**: War Cry upgrades to **Greater War Cry** — +30% Attack.
- **Tank**: still swaps War Cry for **Fortify** (+50% Defence).

## Phase 5

### Persistence (EF Core + SQLite)
- Characters and inventory now **survive server restarts**. The database is a
  single SQLite file (`game.db`) created automatically next to the server on
  first run — **no database server to install**.
- Characters **auto-save every 60s** and on logout; you log back in **where
  you left off** with your level, exp, stats, second class, and full inventory.
- Rolled item attributes persist via an EF Core **JSON column** (`OwnsMany …
  ToJson()`), so adding a new attribute type never needs a migration. Attributes
  roll once at drop time and are immutable thereafter (ready for a future
  "legendary reroll stone").
- **Swapping databases is one line** in `Program.cs`: replace `UseSqlite` with
  `UseNpgsql`/`UseSqlServer`; all the EF Core code is provider-agnostic.

### Accounts & character selection
- The flow is now **Register/Login → Character Select → Create/Enter**:
  - Account login screen with username + password (**PBKDF2-hashed**, never
    stored or sent in plaintext form).
  - Character selection lists all characters on the account; create new ones
    via the class-tree screen, then pick one to enter the world.
- **The first account registered becomes an admin** (convenient for testing).

### Admin role
- Admins use **slash-commands in chat**: `/help`, `/kick <name>`,
  `/ban <name>`, `/unban <name>`, `/jail <name>`, `/unjail <name>`, `/god`,
  `/where <name>`.
- **God mode** makes you immune to damage. **Jail** pins a player to the jail
  corner until released. **Ban** persists (works offline) and force-disconnects
  the player if they're online. Non-admin accounts can't invoke any of these —
  the server validates the admin flag, not the client.

> **First build note:** the server now references EF Core, so the first
> `dotnet build`/restore needs internet to pull the NuGet packages. After that
> it runs offline. The `game.db` file is created on first launch.

## Phase 4.8

### Item attributes (rolled per drop)
- Weapons and armor now roll **random bonus attributes** when they drop, so two
  Steel Swords differ. **Count by rarity**: F common 0 / uncommon 1 / rare 2;
  E common 1 / uncommon 2 / rare 3 (and so on by grade).
- The **attribute pool and roll ranges scale by grade**, defined in
  `Game.Shared/Attributes.cs`:
  - **F grade** pool: Max HP%, Move Speed% — rolls 1–10%.
  - **E grade** pool adds Max MP%, Cast Speed%, Attack Speed%, Attack% — HP/MP
    roll 10–30%, the rest 1–20%.
  - B/A/S inherit the bigger pool with stronger ranges (ready to tune).
- Attributes live on the **item instance**, show in the **inventory tooltip**
  and the **equip-comparison popup**, and feed real stats: HP/MP/Attack %,
  move speed, and **Cast Speed / Attack Speed** (which shorten cast time and
  basic-attack interval).

### Cast speed display (WIT-centered)
- Cast reduction is now centered on **WIT 25 = baseline (0%)**. Each point
  above 25 casts faster, each below slower (1.2%/point). The Stats window shows
  **Cast Speed** broken into the WIT contribution and item contribution, and
  the **cast bar** shows the effective bonus next to the skill name.

### Base-skill unlock levels
- Per your fix, base skills no longer wait for class change: **Power Strike @1,
  War Cry @5** (Fighter); **Magic Bolt @1, Weakness @3, Heal @5** (Mage).

### Fixes
- **Potion buttons**: the rarity letter (C/U/R, top-left) and the count
  (bottom-right) are now separated and readable.
- **Equip-comparison popup**: clicking an item now always shows **its own
  stats**, with the difference vs the equipped item as a secondary column.
  Clicking the equipped item (or an item with no counterpart) shows real values
  instead of zeros, and lists the item's rolled attributes.

## Phase 4.7

### Where to edit skills (for you)
- **`Game.Shared/Skills.cs`** is now the single skill-design file, split into:
  - `SkillCatalog.All` — every skill's numbers + description.
  - `ClassProgression` — **which skills each class gets**, whether a skill
    **replaces** a base skill, and the **unlock level**.
- To give the Witch a DoT the Sorcerer doesn't get: add the `SkillDef`, then
  add a row to `ClassProgression.RaceOverrides` keyed `(Race.Ork, Archetype.Nuker)`
  with `new SkillGrant(id, unlockLevel: 25)`. Nothing else changes — the server
  validates and the client renders from these tables. The hooks for per-race,
  level-gated flavour skills (DoT vs burst vs control) are already in place.

### Base skills upgrade on class change
- Second classes now **transform** the base kit instead of just adding a skill:
  - **Tank**: keeps Power Strike; War Cry → **Fortify** (+50% def).
  - **Warrior**: keeps War Cry; Power Strike → **Mighty Blow**.
  - **Rogue**: keeps War Cry; Power Strike → **Twin Slash**.
  - **Archer**: keeps War Cry; Power Strike → **Power Shot** (ranged).
  - **Healer** (Cleric/Shaman/Priest): Heal → **Greater Heal**, Magic Bolt →
    **Holy Strike** (weaker nuke), keeps Weakness.
  - **Nuker** (Sorcerer/Witch/Inquisitor): Magic Bolt → **Flamebolt** (strong
    nuke), keeps Heal, Weakness → **Greater Weakness**.

### Class identity through numbers
- **Mages** basic-attack for ~15% of attack power — they live on skills + MP.
- **Fighters/Warriors** hit full (110%) and brawl with attack + skills.
- **Archers** hit full + **+15% crit** — kite with basic attacks and crits.
- **Rogues** hit 65% but get **+20% crit and +evasion** — skills + crits.
- **Tanks** hit 55% but bring standout defence (Fortify, heavy armor).
- Mage main skills now **~4s cast (WIT reduces) and ~1s cooldown** so they
  chain-cast, and hit meaningfully harder than a mage's basic attack.

### Stackable consumables
- Potions and enchant scrolls now **stack into one inventory slot** with a
  quantity (1 → 2 → … → "99+"). Drops merge into the stack, using one consumes
  one, trading moves the whole stack and **merges** into the receiver's stack.
  Gear stays one-per-slot (each piece keeps its own enchant level).

### Chat moved up
- The chat panel sits higher so it no longer overlaps the skill bar buttons.

## Phase 4.6

### Character creation — class tree
- The login screen is now a **button tree** instead of dropdowns:
  Race → Base Class → preview each Second Class. The right pane shows base
  stats (CON/ATK/WIT/DEX to compare), the class fantasy, the class-change
  stat bonus, and the full skill list with descriptions — so you know what
  you're getting into before creating. Name + Connect sit at the bottom.

### Skills window (K)
- Lists every skill you have with **description, MP cost, cast time,
  cooldown, and duration**. Each has a **To Bar** button.

### Configurable skill bar
- 8 slots. New skills **auto-fill the first free slot** when acquired (e.g.
  your signature skill on class change), but you can **assign** from the
  Skills window and **remove** by right-clicking a slot. Hotkeys 1-8.

### Buff bar + tooltips
- Active buffs/debuffs show as pills under the vital bars with **time left**;
  hover for a tooltip with the description and remaining seconds. Cast War Cry
  and you'll see the buff and its countdown. Debuffs are tinted red.

### Potions — fixed squares
- Three **always-visible colored squares** (green/blue/gold) bottom-right,
  with a **count badge** (caps at "99+"), **disabled when you have none**.
  Click or hotkeys Q/E. Counts also show as "99+" in the inventory.

### Inventory: remove + enchant
- Each item row has an **X** (destroy — sell/dismantle comes later) and, for
  gear, a **+** (enchant). The equip-compare popup is now **enchant-aware**
  (a +5 sword compares correctly against a +0).

### Enchanting
- Enchant gear **+1 to +16** with success bands from the design doc: **100%**
  to +3, **66%** +4-6, **40%** +7-9, **20%** +10-16. Each enchant level adds
  +20% of base bonus +1 flat. Three scrolls differ on failure:
  - **Common**: the item **breaks**.
  - **Uncommon**: enchant **resets to +0**.
  - **Rare**: enchant **drops by 1**.
  Scrolls **drop rarely from higher-level mobs** (rarer than any other loot;
  the better the scroll, the higher the level floor and the lower the odds).

### Debug menu (DEBUG builds only)
- A **Debug** button (only compiled in Debug configuration) opens a panel to
  grant scrolls, potions, F/E gear, and **Level +1** for testing. Both the
  client button and the server endpoints are `#if DEBUG`-gated, so a Release
  build has none of it.

## Phase 4.5

### UI overhaul
- **Three colored vital bars** top-left: HP (red), MP (blue), EXP (gold),
  each with live numbers, replacing the old text line.
- **Stats window** — the *Stats* button (or **C**) opens a panel next to the
  inventory showing CON/ATK/WIT/DEX, max HP/MP, attack power, defence,
  accuracy/evasion, crit %, and attack range. It updates live on level-up,
  equip, and class change.
- **Equip comparison popup** — clicking an inventory item opens a popup that
  diffs the item against what's equipped in that slot (green = upgrade, red =
  downgrade) with **Equip/Close** buttons, instead of equipping instantly.
- **Chat tabs fixed** — the All/World/Local/Whisper tabs now sit at the bottom
  of the chat box (inside it), not floating above the panel.

### Per-mob loot tables
- Drops are now **per mob type**, not a global roll. Each mob has a loot table
  of (item, chance, mob-level band): Boars drop weapons, Wolves drop armor,
  Slimes drop robes/mage gear, Spiders drop light armor + bows, Bandits drop
  swords and the best F-grade gear. Low-level kills give F grade; level-11+
  kills give E grade — all defined in `LootTables` in `Game.Shared/Items.cs`,
  one dictionary keyed by mob name. Each table entry rolls independently, so a
  kill can drop zero, one, or several items.

### Potions (grade/rarity based)
- Three healing potions on a **shared 30s cooldown**, used from the **potion
  action bar** (hotkeys **Q**/**E**) or by **clicking them in the inventory**:
  - *Minor* (common): heals 1% max HP/sec for 15s
  - *Healing* (uncommon): 2% max HP/sec for 15s
  - *Greater* (rare): instant 50% max HP heal
- Potions are a **separate effect channel from natural regen** — they tick
  during combat too. **Rarity override**: a higher-rarity potion cancels a
  lower one's effect; same rarity restarts it (safe-guarded, though the
  cooldown normally prevents it). You start with two Minor and one Greater to
  test. Any mob can also drop potions on top of its gear table.

## Phase 4

### Second-class tree (level 20)
- At level 20 the **Class** button opens your six race/base-appropriate
  options — the 18 design-doc classes (Beast, Templar, Knight, Cleric,
  Sorcerer, …) mapped onto 6 archetypes: **Tank, Warrior, Rogue, Archer,
  Healer, Nuker**.
- Choosing one is permanent, grants a permanent core-stat bonus, full-heals
  you, and unlocks a **signature skill** that joins your skill bar:
  Fortify (Tank), Mighty Blow (Warrior), Twin Slash (Rogue), Power Shot
  (Archer), Greater Heal (Healer), Flame Burst (Nuker).
- Archetype range rules from the doc are in: **Archer** second classes get
  +500 basic-attack range with a bow (capped 1100); **Healer/Nuker** get
  +500 spell range (capped 900).

### Items & equipment
- Grades **F/E/B/A/S** gate by level (0/20/40/60/80); rarities Common,
  Uncommon, Rare. Weapons add attack (bows/staves also set ranged range);
  armor comes in Heavy/Light/Robe with def/HP/eva/MP profiles.
- Equip/unequip from the inventory; one item per slot (weapon, armor).
  Equipping recomputes all derived stats server-side and re-validates the
  level requirement. You start with a Rusty Sword and Leather Vest.

### Drops
- Killing a mob has a 30% drop chance (70/25/5 common/uncommon/rare);
  level-13+ mobs can drop E-grade gear. Loot lands in your bag (30 slots)
  and pops a system message.

### Trade window
- Target a player within range → *Request Trade*. They get an accept/decline
  prompt. The window matches the design doc: **their offer on top, your
  offer in the middle, your bag on the bottom**, Ready/Cancel in the footer.
- Click bag items to add (max 10), click your offered items to pull them
  back. **Any change resets both Ready flags** — no bait-and-switch.
- The trade commits only when both press Ready; the server re-validates both
  inventories inside a single step (items still owned, bags have room) before
  swapping. Equipped items can't be traded; disconnect/death cancels safely.

