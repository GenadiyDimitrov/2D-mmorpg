# Crafting & Material Economy — design (LOCKED 2026-07-06, not built yet)

Gear comes mainly from **crafting materials that DROP from mobs** (not gathering nodes); finished-item
drops are a **rare bonus** (bosses/elite zones). More content-efficient than dropping hundreds of finished
items, and the profession web forces player coordination. Solo is always possible, just longer.

---

## 1. Materials — RARITY-based (not grade-based)
- **Rarity ladder:** Common → Uncommon → Rare → Epic → Legendary. A "Rare Ingot" is reused across every
  C/B recipe — that's the content saving.
- **5 types ↔ 5 professions** (each profession REFINES one type):
  Gem→Jeweler · Ingot→Weapon Smith · Leather→Armor Smith · Thread→Scroll Scribe · Wood→Potion Master.
- **Drops scale with mob level** (amount + rarity gate):
  - ~L5 → 1 Common; ~L15 → 3–5 Common. **L30+ starts Uncommon**, **L60+ Rare**, **L76+ Epic** (very low).
  - **Bosses** drop more + one rarity higher, PLUS a chance at the finished set. (E.g. L30 boss: ×10 Common/
    Uncommon + 1 Rare at very low chance + the E set.)
  - **Elite / party zones** (stronger, not higher-level mobs) → better rates + amounts (group incentive).
- **THE load-bearing rule:** every rarity has a DIRECT drop source at its zone level AND a craft-upgrade path,
  so you FARM the right zone for the right rarity; upgrading only smooths surplus. (Never forced to upgrade
  Common→Epic — that would be exponential.) Refined mats **both drop (rare) and craft (profession)** → a
  non-Jeweler can still get Uncommon Gems by grinding drops; professions are an EFFICIENCY/TRADE path, NOT a gate.

## 2. Material upgrade (refinement)
- **1 higher rarity = 5 of the SAME type (one lower) + 2 CROSS mats from 2 DIFFERENT professions.**
  - e.g. 1 Uncommon Gem = 5 Common Gems + 1 Ingot + 1 Wood (cross types). → the Jeweler needs mats a
    Weapon Smith / Potion Master made → trade.
- Only the OWNING profession can refine its type (Jeweler refines Gems, etc.).

## 3. Professions
- **1 profession per character**, **auto-learned by CHARACTER LEVEL**, **no re-spec** (for now).
  - FUTURE: profession XP (level a prof by crafting) → then allow re-prof (loses prof level, restarts at 1).
- **Recipe access by tier:**
  - Common + Uncommon items → recipes **auto-known by char level** (craft from Common/Unc mats).
  - **Rare items → DROP ONLY** (not craftable).
  - **Epic SETS → boss recipe + craft** (the strongest per grade; D set craftable @30+).
  - **A-grade (76) recipes → boss-drop / trade.**

## 4. Craft success chance (fail consumes the mats — the risk)
- **Common item: 80%** · **Uncommon: 50%** · **Epic SET: 100%** (guaranteed, but the mats are the gate).
- (Rare = drop only, so no craft roll.) Failed craft consumes mats (assumption — retune to partial-refund if desired).

## 5. Mat → item mapping (primary mat = the crafter's own type; rest force trade)
| Item | Profession | Materials |
|---|---|---|
| **Armor** | Armor Smith | **Leather** + Ingot + Thread + Gem (Heavy=ingot-heavy, Light=leather-heavy, Robe=thread-heavy) |
| **Weapon** | Weapon Smith | **Ingot** + Gem + Wood (melee=ingot, bow/staff=+wood, caster=+gem) |
| **Jewel** | Jeweler | **Gem** + Ingot + Leather |
| **Potion** | Potion Master | **Wood** + Thread + Gem |
| **Scroll** | Scroll Scribe | **Thread** + Wood + Gem |

## 6. Finished items — 4 rarities per grade
- Each grade has **Common / Uncommon / Rare** finished items that **DROP from mobs** (usable, best-for-grade
  but NOT as strong as the set) + an **Epic SET** (craft/boss = the strongest per grade).
- **Drop items = scaled-down set stats** (Claude generates them, no CSV): **Common ≈ 65%, Uncommon ≈ 78%,
  Rare ≈ 90%** of the set base; standalone (no set bonus). Easy to retune.
- The current tiered gear (weapons/armor/sets already built) = the **Epic/set tier** (100%).

## 7. Example cost (E set, retune via drop rate + global RateConfig)
- E armor body ≈ **50 Uncommon + 100 Common** mats (e.g. 25 Ingot, 10 Leather, 10 Thread, 5 Gem uncommon).
- Higher grades: D = lots of Uncommon + some Rare; C/B = lots of Rare + some Epic; A = Epic + Legendary.
- Sanity: ~L30 mob ~4 Common/kill → a body ≈ 100–150 kills, a full set ≈ a few hours for the *best* 20–40 set.

## 8. Drops become mats-primary (reshapes what we just built)
`MobCatalog.StandardDrops` (currently drops finished tiered gear) → drop **materials** as the main loot +
Common/Unc/Rare finished DROP items + (bosses) the Epic set. The tiered sets stay as the craft/boss output.
Use L2-style **drop GROUPS** (a group rolls to fire, then each item rolls its own chance).

---

## Build order (when we roll)
1. **Materials** (5 types × 5 rarities = 25 items, tradable/stackable) + **Profession** enum. *(no-test data — FIRST)*
2. **Recipe catalog** (inputs → output, profession, char-level, success chance) — data.
3. **Craft action** (consume inputs, roll success, produce output) — server logic + minimal UI.
4. **Mat-refinement recipes** (5 + 2 cross) + **finished-item recipes** (Common/Unc auto; Epic set via boss recipe).
5. **Drop rework:** mats-primary + scaled Common/Unc/Rare drop items + boss set/recipe drops (drop groups).
6. Professions (learn by level) + later: profession XP / re-prof; A-grade + Rare-recipe drops; elite-zone rates.

## Still open (small)
- Failed-craft policy (consume all vs partial refund — assumed consume).
- Exact mat counts per recipe/grade (numbers), and drop rates (tune with RateConfig).
- Wood/Thread thematic role in potions/scrolls (herb/parchment) — cosmetic.
