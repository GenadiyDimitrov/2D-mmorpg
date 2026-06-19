# L2-like MMORPG — Phase 14 (item attribute pools)

Server-authoritative multiplayer prototype. Phases 1-3 built movement,
interest management, combat, skills, buffs, the safe-zone town and banded
hunting grounds. **Phase 4 adds the RPG progression layer: the second-class
tree at level 20, items & equipment with grades, monster drops, inventory,
and a full trade-window system.**

## Run it (click and play)

Requirements: .NET 8 SDK, Windows, internet on first build (NuGet restore).

**Visual Studio:** open `Game.sln` → solution right-click → *Configure Startup
Projects…* → *Multiple startup projects* → **Game.Server** and
**Game.Client.Wpf** both *Start* → F5.

**CLI:** `dotnet run --project Game.Server`, then
`dotnet run --project Game.Client.Wpf` per client (unique names).

## Controls

| Action | Input |
|---|---|
| Move | Left-click ground |
| Target a mob & attack | Left-click a mob |
| Target a player (to trade) | Left-click a player |
| Skills | Keys **1-6** or skill bar |
| Inventory | **I** or the Inventory button |
| Equip / unequip | Click an item in the inventory |
| Second class (lvl 20) | Class button (top right) |
| Trade | Target a player → *Request Trade* in the target frame |
| Local / World / Whisper | plain / `!text` / `/w Name text` |

## New in Phase 14 (this build)

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

## New in Phase 13 (this build)

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

## New in Phase 12 (this build)

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

## New in Phase 11 (this build)

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

## New in Phase 10.1 (this build)

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

## New in Phase 10 (this build)

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

## New in Phase 9 (this build)

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

## New in Phase 8 (this build)

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

## New in Phase 7 (this build)

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

## New in Phase 6.1 (this build)

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

## New in Phase 6 (this build)

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

## New in Phase 5.4 (this build)

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

## New in Phase 5.3 (this build)

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

## New in Phase 5.2 (this build)

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

## New in Phase 5.1 (this build)

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

## New in Phase 5 (this build)

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

## New in Phase 4.8 (this build)

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

## New in Phase 4.7 (this build)

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

## New in Phase 4.6 (this build)

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

## New in Phase 4.5 (this build)

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

## New in Phase 4

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

## What's where

```
Game.Shared
  GameConstants, StatCalculator     core + combat + progression formulas
  Enums, Dtos, CastInfo             wire contracts
  Skills (SkillCatalog)             base + archetype signature skills, ranges
  Items (ItemCatalog)               item defs, grade gates, drop rolls
  Classes (ClassCatalog)            18 classes -> 6 archetypes, stat bonuses
Game.Server
  Hubs/GameHub                      thin: every call -> a queued command
  Simulation/
    World, TradeSession             state + command queue (single writer)
    Entity                          stats/buffs/inventory/equip/2nd class
    CellGrid                        interest management
    GameLoopService                 10 t/s: cmds -> AI/skills/combat -> snaps
Game.Client.Wpf
  NetworkChannel                    transport seam (Unity-reusable)
  MainWindow / .Phase4              world view + inventory/class/trade UI
```

The concurrency model is unchanged and still the whole point: hub threads
only enqueue commands, the loop is the single writer, **zero locks** — even
trade (a two-party, item-moving transaction) is just commands resolved on
the loop thread, so it can't race.

## Roadmap

- **Phase 5** — EF Core persistence (accounts, character save/load), real
  login/auth, admin role (ban/kick/jail, god mode), boss monsters with the
  ±10 level paralysis rule, shields/orbs proc items, environment terrain
- **Phase 6** — the Unity 2D client (reuses Game.Shared + NetworkChannel),
  then multiple seamless zones toward the 75k×75k world
