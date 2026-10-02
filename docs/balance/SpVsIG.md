# Skill SP prices vs IG (`BL-334`, 2026-10-02)

The research behind `--reprice-sp`. Source: l2elo.com (Interlude), 8 class lines and 56 creatures, read through the
site's JSON (`/_next/data/<buildId>/en/database/classes/<uri>.json`, `.../npcs/<id>.json`).

## 1. How IG prices a skill

- **Every skill costs the same total at a learn tier.** Gladiator at 40: a mastery is 3 rungs × 11k = 33k, and
  Vicious Stance is 1 rung × 33k = 33k. Sorcerer at 40: 3 × 11k, 2 × 17k or 1 × 34k. So IG has no real weights; the
  number of rungs a tier carries is the only thing that differs. (You read this right before I did.)
- **The tier price follows the EXP curve.** Base rung 11k at 40 → 50k at 58 → 97k at 60 → 770k at 74 (×70). Our
  `ExpToNext` grows ×63 over the same levels, so a rung is about **5-7% of the SP one level pays**.
- **Ladders never fall**: 6 small dips in 470 ladders, all at the 24 → 28 step.

## 2. SP per kill

IG creatures pay **≈ 1/12 of their EXP as SP at 20-60, 1/10 in the 70s, 1/8 at 80+**. We pay 1/20. The EXP table is the
same one, so IG earns about twice the SP, and its kits cost about twice ours to match.

## 3. Affordability (x = SP earned in a band / the kit's cost in it)

| band  | IG (at its own SP rate) | ours before 0.225.0 | ours from 0.225.1 (his targets) |
| ----- | ----------------------: | ------------------: | ------------------------------: |
| 1-19  |                    ~2.4 |                  ~3 |                            1.75 |
| 20-39 |                    ~1.5 |            0.6-0.96 |                            1.25 |
| 40-51 |                         |                     |                             0.9 |
| 52-60 |       0.83-1.06 (40-75) |     0.68-1.0 (40-75)|          0.85 (0.75 in 0.225.0) |
| 61-75 |                         |                     |  0.8 (0.6 in 0.225.0, too hard) |

The paths IG measured: Gladiator, Warlord, Paladin, Treasure Hunter, Hawkeye, Sorcerer, Bishop, Prophet.

## 4. What we built

IG's shape (price = tier price × weight, tier price off the EXP curve), with your weights kept and your band targets in
place of IG's. The formula is in `docs/Formulas.md` (*Skill SP prices*). Before 0.225.0 the CSVs had **365 falling
steps across 321 ladders**; after, none (`--check` fails any that come back).
