# Backlog archive — rulings that were replaced

The other half of [Backlog.md](Backlog.md). When you re-spec something, the **new** version is the
only one in the backlog; the version it replaced is cut and pasted down here, dated, under the same
id. Nothing is deleted — a superseded ruling is still the reason the code looks the way it does, and
this is where you look when you wonder why.

**This file is not a done-list.** Shipped work lives in [CHANGELOG.md](CHANGELOG.md); closed
playtests live in [testing/Playtest-Archive.md](testing/Playtest-Archive.md).

Two kinds of entry:

- **`BL-nn`** — a backlog entry that was rewritten before it was built. The old text sits here.
- **§ a superseded design** — a ruling that was already *built*, then reversed or replaced. It has
  no backlog id because it was never owed; it is here so the reversal is findable.

---

## Superseded backlog entries

*(none yet — this file was created 2026-08-12, at the same time as the backlog. The first entry
lands here the first time you re-spec something in `Backlog.md`.)*

---

## Superseded designs — built, then replaced

### § Shields: "don't add shield P.Def to the pool" and "cut it 5×" → **option 3**
**Replaced 2026-08-12** (`267313d`). You offered three ways out of the double-dip and chose the
third. Option 1 was to keep the shield's P.Def out of the overall stat and only apply it on a
successful block — you rejected it as *"invisible"*: *"with .3 it means leaving one hand open u can
equip another defence item (1h less p/mAtk so u get a pdef) — with .1 its invisible."* Option 2 was
the 5× cut alone, which left a mage at ~15.5% and a tank at ~24% — too close together. **Option 3 =
option 2 plus the tank's shield passive ×5**, and only that passive: *"sheild_mastery.Shield_PDef
will be the only part that will increase 5 times, the sheild chance, arrow defence and other
passives, sets and buffs that increase the shieldPdef/chance etc are kept as is."*

### § The tutorial: `69b`'s fix → `63j`'s re-spec
**Replaced 2026-08-12.** The 0.60.1 quest-step-supplies-its-props fix was shipped, then you played
it and wrote a better ruling: *"I have given better rulling in the `63j`."* No initial boxes at all,
boxes handed out by Cera and Pell exactly when they are opened, **plain** boxes decided by base
class rather than selection boxes, and the four-beat order travel → put an attack on the bar →
target-and-use → kill 5. Built the same day.

### § Timed / bound items: cloned `_bound` item defs → **per-instance tags**
**Replaced 2026-08-12** (`ed75bac`). The 0.54.0 newbie kit was built as cloned defs
(`ItemCatalog.BoundCopies`). You accepted the clone for that kit but refused it as the mechanism:
*"it is a REAL item with tags — never a new server-side def."* Five per-instance fields now carry it,
and the displayed tag is derived from them rather than stored.

### § Enchant: a PERCENTAGE of the item's stats → a **FLAT offset**
**Replaced 2026-08-11** (0.60.0). `BonusAt` is gone; the offset is the same for every class and is
chosen by **grade**, not by rarity. ⚠ Your objection to the class-flat half is live in the backlog
as `BL-12` — it has not been answered, so the flat model stands until it is.

### § Weapon crit-rate roll: a FLAT `CritRateFlat` → a **multiplier**
**Replaced 2026-08-07** (`0d`). The roll was being fed in as `value / 100`, so a maxed roll was +30
*percentage points* and it collapsed the 3:2:1 weapon identity the whole crit model exists to
create. Your ruling made it multiply, and raised the sword's ceiling 30% → 90% so a max roll lands
the two weapons together (sword `88 × 1.9 = 167`, dagger `132 × 1.3 = 172`). ⚠ A large dagger/bow
nerf at max roll, stated by you and intended — and still never played.

### § Evasion Mastery: raised evasion itself → **raises the FLOOR only**
**Replaced 2026-08-06** (`M9`, 0.50.0). *"Once I turned rogue my evasion jumps a lot, and it
shouldn't."* The passive was worth ~32 points of raw evasion on top of the floor, which meant the
floor was always the binding number anyway. Crit rate became your full IG model in the same pass,
and the rogue's ×1.20 crit passive moved onto Weapon Mastery at level 20.

### § The ±20 level gap: a hard **lockout** → the floors stay live at every gap
**Replaced 2026-08-07** (`M1`, 0.53.0). Step ordering in `ResolveAvoidChance` swapped: level gap
first, the `[5%, 95%]` band and the floors **last**. `G = 1.0` now means "pinned to the edge of the
band", not "cannot be hit". Your reason: `ExpCurve.GapZero = 13` already pays zero exp and zero
drops seven levels earlier, so the lockout was doing no work.

### § Magic resist: **dropped, never to be a stat** → mRes is damage reduction
**Replaced 2026-08-09** (0.58.2). The old roadmap line said magic mitigation is only M.Def plus the
fizzle floor, and that *"mRes in owner CSVs = the fizzle floor"*. It isn't — mRes is a damage
reduction, and the fail chance is its own formula:
`fail% = round(1.3^(defLvl − atkLvl) × defMod × weaponMod)`, clamped at 95%, with parity anchored at
**1% fail**. ⚠ The "DROPPED" section at the bottom of `Roadmap.md` still carries the old wording and
is stale there.

### § Group buffs: a group *stacks alongside* its singles → **a group is ONE buff**
**Replaced 2026-08-01** (0.42.0, reversing 0.36-0.41). A group carries `GroupRank = 100 + level`,
every child's magnitudes and a `CoveredKeys` list, so it always outranks and evicts its singles and
a potion can never override it. Authoring rule that came with it: a group must be ≥ the best single
in **every** family it covers.

### § Spell range: scaled by the caster's class TIER → **per-spell**
**Replaced 2026-07.** `SkillMath.EffectiveRange` returns the skill's own `Range`, authored per
spell (heals short, healer attack ~750, nuker ~900, base nuke 600). The one exception kept is **bow
skills**, which still scale with the archer's bow tier (350/600/900) to match the basic-attack range
growth.

### § A class grants STATS → **identity is the kit**
**Replaced 2026-08-10.** The 2nd/3rd-class `ClassFlatBonus` fields were deleted. Two disciplines of
one archetype run identical stats and differ only in what their skills do. ⚠ The standing rule that
came with it: **do not re-home the same numbers as invented passives** — *"w8 on the 40+ csvs"*.
`ClassFlatBonus` survives as an armor-set type only. What still legitimately varies by class: the
per-archetype HP/MP growth curves and `BasicAttackMultiplier`.

### § Buff scrolls: 48 scrolls, dropped by mobs → **17, and the Blessing Box is the only source**
**Replaced 2026-08-05** (`E3`). One scroll per buff at the top rung, Rare, bound; the Rare potion
rung deleted (24 → 18 potions); **no buff-scroll drops from anything**; 250k at the Apothecary for a
pick of 10. The game's first real gold sink. Consumables per kill fell 33% → 18.5%.

### § A gathering contract pays an **authored** exp number → `RewardModifier` × the creature's own
**Replaced 2026-08-01** (0.42.9). `QuestGather.RewardModifier` **is** your `QuestItemRewardModifier`,
and it multiplies the mob's own `MobExpReward`/`MobGoldReward` at its natural level. That is what
keeps a repeatable contract level-appropriate with nothing to re-tune, forever.

### § Gear regen: a flat MP/s per item → a **percent roll**
**Replaced 2026-08-03** (0.45.0). A flat +9 ring was worth +22.7 after the multiplier stack and
dominated the level curve at every level. Rings now roll a percent, 1-5% by grade. The flat types
stay in the enum for pre-0.45 saves and nothing rolls them.

### § DEX → **AGI** (naming only)
**Replaced 2026-08-09** (0.58.1). Every player-facing surface reads AGI. ⚠ The four stat-swap skill
**ids** still spell `dex` on purpose: an id is a persisted key, and renaming one would delete a
15kk purchase.

### § `BL-26` The vendor half of the buy-back design — **a longer sold list**
**Closed 2026-08-14** — not built, superseded by your own later ruling. The entry read: *"The vendor
half of the buy-back design — a longer sold list. Flagged 'still open, still not urgent' and never
revisited."* It descended from the ORIGINAL design (`Roadmap.md:126`, *"a buy-back menu — last 10
deleted/sold; free restore for deleted or sold-for-0"*). Playtest-19's **`M14`** replaced it — *"Cap
the vendor buyback list at 10-15 items"* — and that shipped as `GameConstants.BuyBackSlots = 12`,
with the deleted half split off into its own 5-slot `Restorable` list (`C18`, your own two-list
fallback: a shared list would let a selling spree push the one thing you meant to undo off the end).
Lengthening the list now would walk back the cap you asked for.
