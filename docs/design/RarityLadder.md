# Rarity ladder — one item, six rarities (owner design, 2026-07-29)

**Status:** DESIGN AGREED IN OUTLINE, not built. Two open questions at the bottom.
Supersedes the ad-hoc split between the "(Lesser)" vendor sets, the tiered TOP sets and the
`ScaledDropItems` Common/Uncommon/Rare copies.

## The problem this replaces

Today a single gear slot at one grade exists in **eight** forms across two unrelated generations:

| Line | Example (E-grade bow) | P.Atk |
|---|---|---|
| legacy grid (shop) | Worn Bow / Fine Worn Bow / Masterwork Worn Bow | 6 / 8 / 10 |
| Lesser drop copies | Common/Uncommon/Rare Electrum Longbow (Lesser) | 83 / 100 / 116 |
| Lesser (vendor top) | Electrum Longbow (Lesser) | 129 |
| Top drop copies | Common / Uncommon / Rare Electrum Longbow | 124 / 148 / 171 |
| Top (Epic, set bonus) | Electrum Longbow | 191 |

Rarity is baked into the item NAME, the two ladders interleave (Lesser 129 lands between Common
124 and Uncommon 148 — arithmetic, not a bug), and the vendor shows all of it in one flat list.

## The new model

**One base item per slot per grade.** Its rarity is a *property*, shown by COLOUR and by a
description row — never by the name.

```
Electrum Longbow          ← the name, always
  Name:    Electrum Longbow
  Grade:   E
  Rarity:  Uncommon        ← this row, plus the name's colour
  Type:    Bow (2H)
  Attack:  ...
```

### Six rarities, one scale

| Rarity | Stat % of base | Set bonus | Attributes |
|---|---|---|---|
| Common | 45 % | — | — |
| Uncommon | 55 % | — | — |
| Rare | 70 % | — | — |
| **Epic** | **70 %** | ✅ scaled to 70 % | ✅ scaled to 70 % |
| Legendary | 85 % | ✅ scaled to 85 % | ✅ scaled to 85 % |
| Mythic | 100 % | ✅ full | ✅ full |

**The split is at 70 %.** Rare and Epic carry identical raw stats; Epic is where set bonuses and
rolled attributes switch on. That single rule is what makes the ladder readable: below Epic you are
buying numbers, from Epic up you are buying *identity*.

⚠ **Anchoring:** today's A-grade top item = the new **Epic (70 %)**, and its set bonus scales to
70 %. Legendary and Mythic are **new items above today's ceiling** — Mythic is `today ÷ 0.7`,
i.e. **+43 % over the current best gear**. Run `tools/BalanceMatrix` before and after; this is a
real top-end inflation, deliberately taken.

### Drops

- **Normal mobs** — Common / Uncommon / Rare, plus Epic at a very low chance.
  Below level 75 they also drop **set recipes for their own grade**, at a rate *lower* than the
  full Epic item. Mobs 75+ drop no craft recipes (raw mats still drop).
- **Elite / stronger mobs** — Rare and Epic at meaningfully better rates; at 75+, recipes for
  their grade at a very low chance.
- **Bosses** — Epic ~70-80 % per kill · Legendary ~40-50 % · Mythic ~2-5 % ·
  armor recipes 50-60 % · weapon recipes 40-50 % · jewel recipes 70-80 %.

### Crafting — Legendary only

Crafting produces **Legendary** items and nothing else. Each craft: **70 % success**; of the
successes, **20 % come out Mythic** (so 56 % Legendary, 14 % Mythic, 30 % failed).

The owner's worked example, which is the intended pacing:

> 100 bosses → ~50 recipes, ~70 Epic items, ~40 Legendary, ~3 Mythic.
> Crafting those 50 recipes (assuming a spread of slots) → ~28 Legendary + ~7 Mythic.
> So a clan running boss parties and elite zones gears up fast; a solo player can grind elite
> zones (slow and dangerous) or find randoms.

## Feasibility — checked against the code, 2026-07-29

**Scaling the stats: already exists.** `ItemCatalog.ScaledDropItems` walks every base-tier piece
and emits copies with `AtkBonus`/`DefBonus`/`MpBonus`/… multiplied by a scale factor. Changing
`DropTiers` from three entries to six is a data edit.

**Scaling attributes: easy.** `AttributeSystem.TieredWeaponMax(type, isMagic, attr, level)` returns
the cap per (family, attribute, tier). Multiplying its result by the rarity's factor gives exactly
the owner's example (a 30 % cast-speed cap → 21 % at 70 %, 25.5 % at 85 %). `TieredWeaponAttributeCount`
can stay level-driven, or take the same factor if the count should thin out too.

**Scaling set bonuses: possible, more work.** `ArmorSetDef` carries a `ClassFlatBonus`, a full
`StatMods`, and two loose percents (`DefencePct`, `CastSpeedPct`). Scaling needs a
`StatMods * float` helper plus one for `ClassFlatBonus` — mechanical, but it touches every field.
The other half: drop copies currently clear `SetId` (`SetId = ""`), so Epic/Legendary/Mythic copies
must KEEP a set id and get their own generated `ArmorSetDef` per rarity
(`set_heavy_t76_epic` / `_legendary` / `_mythic`). Both halves are contained.

**Verdict:** all three scale. The owner's fallback ("if we cannot scale them, only the top rarity
gets bonuses") is not needed.

## DECIDED (owner, 2026-07-29)

### The "(Lesser)" line is DELETED
Common at 45 % occupies the space Lesser held, so the whole parallel line goes. One name per slot
per grade: **`Electrum Bow`** — what used to be "Lesser" and what used to be "Top" collapse into
the same item, distinguished only by rarity.

### Naming: rarity lives ONLY in the colour and the description
The word never appears in the item name — no "Common", no "Legendary", no "(Lesser)". The name is
the name; the **colour** carries the rarity, and the description has a `Rarity:` row.

### Vendor prices scale by rarity
The playtest-13 price table is the **Rare** price (owner: *"my price is for rare-lesser"*):

| Rarity | Vendor price |
|---|---|
| Common | **35 %** of the listed price |
| Uncommon | **70 %** |
| Rare | **100 %** (the listed price) |

Epic and above are not sold. Owner's reasoning: *"the com/uncom are easily obtainable by drop —
with this high of a price there's no point in buying them."* A vendor tier that nobody buys is
dead content, so the low rarities are priced as the convenience they are.

### 🔴 Rarity COLOURS must be visible in the inventory (Unity)
Owner: *"also in inventory the colors should be visible as it was in the WPF."*

**This is a real gap, not a regression.** The WPF client has `MainWindow.Phase4.cs:481`
`RarityBrush()` — Uncommon → LightSkyBlue, Rare → Gold, everything else → Gainsboro — applied to
inventory rows, shop rows and the equip list. **The Unity client has no rarity colouring at all**;
`GameUi.Items.cs:267` merely appends `"  Grade / Rarity"` as plain text. The port needs a
**six-colour** palette (the WPF one only ever defined three), used everywhere an item name is
drawn: inventory, vendor, warehouse, trade, loot, item details.

## Still open

1. **Legacy gear removal list.** `AshWand`, `IronMace`, `WoodenShield`, `BrassAmulet` and the whole
   `WeaponKey`/`ArmorKey` F/E × Common/Uncommon/Rare grid ("Worn" / "Steel" / "Tempered" /
   "Fine " / "Masterwork ") are the pre-ladder generation and should go.
   **Darksteel / Cobalt / Electrum / Adamantine … are NOT legacy** — they are the `GradeTheme()`
   names of the current ladder (D / C / E / A) and stay. Confirmed by the owner.
2. **The six colours themselves** — the WPF palette defines three; Epic / Legendary / Mythic need
   colours chosen.
