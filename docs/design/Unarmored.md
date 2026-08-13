# Unarmored (no-armor) defence — investigation (owner request, 2026-07-15)

**Findings + recommendation. NO code change — companion to `docs/design/BareHands.md`.**

## The question
After the P.Atk fix made a naked character *feeble on offence*, is a naked character also too
*tanky on defence*? (Owner: "do an unarmored investigation as we did for bare-hands.")

## How IG does it
IG's P.Def is the **sum of small per-slot base values**, one per armor slot, and equipping a slot
**replaces that slot's base with the armor piece's P.Def**. Example (IG, Maestro): chest 31, legs 18,
head 12, feet 7, gloves 8, underwear 3, cloak 1 → **~80 naked**, level-independent. A full armor set
raises it many-fold (A-grade body alone is 100s). So naked ≈ 80 and stays there; armor provides the
overwhelming bulk.

## How WE do it
`StatCalculator.PhysicalDefenceBase(level) = 68 + level²/100`, a **single flat base independent of
armor**, with each armor piece's `DefBonus` **added on top**. Same shape for M.Def (`20 + level²/100`).

| | naked P.Def | with a full tier set |
|---|---|---|
| L1 | 68 | ~68 + newbie armor |
| L40 | 84 | ~84 + 240 body + accessories ≈ 500+ |
| L85 | 140 | ~140 + 332 body + accessories ≈ 900+ |

So our naked P.Def is **close to IG's at low level** (68 vs ~80) but, unlike IG, it **grows with level**
(→ 140 by L85) — a naked L85 is a bit tankier than IG intends. Armor still dominates in both.

## Is it a problem? — NO, not right now
- The reported exploit ("level to 20 naked") is **already dead** from the P.Atk fix: a naked character
  can't kill anything (2 P.Atk, ~13-180 hits per mob). Defence doesn't matter if you can't fight.
- Naked P.Def is only ~1/4–1/6 of armored at every level, so armor is already clearly worth wearing.
- Nothing one-shots *because of* being unarmored; there's no live balance bug here.

## Recommendation: LOW PRIORITY, don't change it now
The clean way to make "naked = fragile" the IG way is per-slot bases (equipping a slot swaps its base).
We deliberately use **one flat base + armor on top**, so the only lever within our model is to lower the
flat base and let armor carry more — but armor *adds* to that base, so lowering it also lowers **armored**
defence and would re-tune the whole defence curve (the same entanglement as the P.Atk change, but here
with **no gameplay problem to justify it**).

If we ever want naked-fragility for PvP/authenticity, do it then, and mirror the P.Atk approach: reduce
the flat base, make armor the dominant source, and re-verify defence in `tools/BalanceMatrix`. For now:
**leave it.** One small, cheap tweak is available if desired — drop the `level²/100` growth so naked
P.Def/M.Def stay ~flat like IG (68/20) instead of climbing to 140 — but even that isn't needed.
