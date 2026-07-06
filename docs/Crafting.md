# Crafting & Material Economy — design (not built yet)

Owner's gear-economy direction (2026-07-06). **Decision: crafting-primary hybrid.** Gear comes mainly
from crafting materials that DROP from mobs (not WoW-style gathering nodes); finished-item drops are a
RARE bonus (bosses/dungeons later). This is more content-efficient than dropping hundreds of finished
items, and the profession web forces player coordination — both goals the owner wants.

---

## 1. Materials (drop, don't gather)
- **Every mob drops COMMON materials.** Higher-level / stronger mobs drop MORE, and with an increasing
  chance for the better rarities.
- **Material rarity ladder:** Common → Uncommon → Rare → Epic (→ Legendary?).
- **Grade-upgrade chain** (spend surplus low mats): e.g. N Common → 1 Uncommon → 1 Rare → 1 Epic.
  - ⚠ **Keep the upgrade a SIDE path, not the spine** — pure 10× per step is exponential (1 Epic from
    Common = 1000s of mats). Mats should mostly drop AT their rarity from the matching zone; upgrading
    just smooths surplus. Ratios TBD but keep modest.
- **Difficulty scales with gear power:** the stronger/higher-level the gear, the harder its mats are to
  get/craft. Solo is always possible, just LONGER.

## 2. Professions (everyone can learn; specialize → must trade)
Jewel Maker · Weapon Smith · Armor Smith · Potion Master · Scroll Scribe.
- A character learns a limited number (1–2?) → a solo player covers the rest via **alts / longer grind**
  (fits the [[buffer-enchanter-design]] alt/one-man-party philosophy). Count-per-char = TBD.
- **Low-grade items** need few mats and NO cross-profession intermediates (soloable early).
- **Strongest-per-grade = the SET items** → these need cross-profession intermediates + EPIC mats.

## 3. The CIRCULAR cross-profession dependency (the coordination engine)
Each crafter makes their goods PLUS one intermediate the NEXT crafter needs — a closed loop:

```
 Jewel Maker  → jewels  + GEM ─────────────┐
 Weapon Smith → weapons + INGOT (needs GEM)│
 Armor Smith  → armor   + VIAL (needs INGOT)
 Potion Master→ potions + ACID/MAGIC FUEL (needs VIAL)
 (Fuel) ───────────────────→ back to Jewel Maker (needs FUEL)
```
- Gem → Weapon craft; Ingot → Armor craft; Vial → Potion craft; Acid/Fuel → Jewel craft. Closed 4-cycle
  (Jewel → Weapon → Armor → Potion → Jewel).
- So crafting a TOP-tier item of any type pulls in the whole chain → coordinate with others, or run all
  professions yourself across alts (slower).
- **Scroll Scribe** = 5th profession, place in the loop TBD (scrolls may need a bit of each intermediate,
  or sit outside the core cycle).

## 4. Item craft costs (tiered) — EXAMPLE, numbers TBD
- Lower grade: a handful of same-rarity mats, no intermediates.
- Strongest per grade (set item): e.g. **5 Epic + 50 Rare** mats **+** the required cross-craft
  intermediate(s). The **5 Epic** are themselves expensive to craft (many lower mats) → the real gate.
- Set lvl 20 vs set lvl 76: higher tier = more/rarer mats + more epics.

## 5. Reshapes the current drops
The tiered-gear drop table just built (`MobCatalog.StandardDrops`) will shift: mobs drop **materials** as
the main loot, finished tiered gear demoted to a RARE bonus; **bosses** drop finished top items (later).

---

## OPEN QUESTIONS (owner still deciding)
- Exact ratios: Common→Uncommon→Rare→Epic counts; how many Rare → a set-lvl-20 piece; how many Rare + Epic
  → a set-lvl-76 piece.
- **Jewels have no SETS yet** — add jewel sets? What makes a jewel need Epic mats (a cut Gem "core"?).
- **Weapons** — what makes them need Epic mats (a weapon "core"/rune?).
- Scroll Scribe's exact place in the dependency cycle.
- How professions are learned + how many per character; recipe acquisition (drop recipes vs. learn from a
  trainer); whether the grade-upgrade path exists at all or mats only drop at-tier.

## Pros/cons recap (why crafting-primary)
- **Pros:** content-efficient (drop a small reused material set, not hundreds of items); deterministic
  progress (less RNG rage); rich trading economy via professions; scales (new gear = new recipe).
- **Cons:** more systems to build (materials, recipes, craft action, professions, upgrades); needs
  mat-rate × recipe-cost tuning; can feel grindy; leans on the trade system (exists).
- **Hybrid:** keep finished-item drops as the rare boss/dungeon exception so bosses stay exciting without
  needing a giant finished-item catalog.

## Build order (when we start)
1. Material items (types × rarities) + a **recipe catalog** (inputs → output, profession, counts).
2. Wire materials into `StandardDrops` (by zone/level/rarity); demote finished gear to rare.
3. A **craft command** (consume inputs → produce output) — server logic, minimal UI.
4. **Professions** (learn/gate recipes) + the cross-profession intermediates (gem/ingot/vial/fuel).
5. Grade-upgrade recipes (tuned) if kept. Later: boss/dungeon finished-item drops; jewel sets; weapon cores.
