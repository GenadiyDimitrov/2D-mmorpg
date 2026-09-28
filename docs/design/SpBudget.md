# `BL-314` — the SP budget, measured (proposal, NOT built)

2026-09-28. Your why, from the 0.214.32-38 note: *"after lvl 20 or so u have SP to spare ... the sum of all the passives
is 3-4 times more that the current single one - until 75 .. after the sum should be x1 .. 76 is hard SP wise ... lets make
ppl to use their brains"*.

Before designing the split, I measured the gap with `dotnet run --project tools/BalanceMatrix -- --sp-budget`
(add a name for one path's level-by-level table, e.g. `--sp-budget Magus`). Every number below comes from that print.

## 1. How it is measured

- **SP earned in a level = `ExpToNext(L) / 20`.** SP is a fixed 1/20 of the same kill's EXP, so a level's worth of kills
  pays exactly that, however tough the mob and whether solo or in a party. This is at **×1** (the default rates), with no
  Favor, Blessing or runes, and without quest SP.
- **SP the kit costs** = every rung of the path (1st + 2nd + 3rd + ascended 4th), each priced with the class's own SP cost
  (so the CSV prices). From each pick-one group (the stat swaps, for example) only the dearest member counts.

| band | SP earned (×1) |
|---|---:|
| 1-19 | 42k |
| 20-39 | 729k |
| 40-59 | 5.6M |
| 60-75 | 40.2M |
| 76-85 | 1,244M |

## 2. What it shows

**At ×1, buying everything, there is no SP to spare between 20 and 75.** `x 20-75` below is SP earned divided by the kit's
cost over 20-75 (above 1 means you can afford all of it):

| path group | x 20-75 | passives' share of the kit | SP left at 75 after buying all |
|---|---:|---:|---:|
| daggers (Phantom, Stalker, Assassin) | 1.9-2.3 | 59% | +15 to +20M |
| bows (Sentinel, Soultracker, Sharpshooter) | 1.5-1.7 | 72% | +9 to +13M |
| warriors (Ravager, Warlord ×3 races) | 1.5-1.9 | 75% | +8 to +16M |
| tanks (Bulwark ×3) | 1.2-1.3 | 71% | +0.3 to +5M |
| Magus ×3 | 1.3 | 61% | +5M |
| **healers (Lightbringer, Warchanter ×3)** | **0.78-0.93** | 39-53% | **−10 to −19M (short)** |

Levelling from 74 to 75 alone pays 6.4M, so "+5M at 75" means about one level's worth of SP in hand. The Magus's bank
dips to **42k at level 74**.

**So where does "SP to spare" come from?** Almost certainly from three things this print does not count:
- **Bonuses.** The Wayfarer's Favor adds up to **+400%** (×5) while its gauge lasts, the Blessing adds +100%, and runes
  multiply on top. A player who keeps the gauge topped earns 2-5× this table.
- **Not buying everything.** An AoE you never use, a second debuff line.
- **Quest SP.**

**76+ really is hard, as you said.** At 76-85 the kit costs 2.2-4.7 billion against 1.24 billion earned, and
nothing here changes that.

**"You catch up after 76" is true for the 20-75 skills.** The whole 20-75 kit costs 20-60M, and levels 76-79 alone pay
~165M. ⚠ But that SP is also what the 4th-class kit needs, and that kit is already short on its own. Catching up the old
rungs is cheap; catching up on everything is not reached until ~89-90.

## 3. What your "3-4×" does, at ×1

The same ratio, with only the **20-75 passives** priced ×3 or ×4 (actives unchanged):

| path group | today | passives ×3 | passives ×4 | passive × that lands at 0.65 |
|---|---:|---:|---:|---:|
| daggers | 1.9-2.3 | 1.13-1.28 | 0.95-1.05 | **6.8-7.4** |
| bows | 1.5-1.7 | 1.03-1.14 | 0.89-0.98 | **6.8-7.4** |
| warriors | 1.5-1.9 | 0.82-0.99 | 0.68-0.80 | 4.2-5.2 |
| tanks | 1.2-1.3 | 0.74-0.80 | 0.63-0.67 | 3.8-4.2 |
| Magus | 1.3 | 0.79-0.80 | 0.65-0.67 | 4.0-4.1 |
| **healers** | **0.78-0.93** | **0.56-0.60** | **0.47-0.53** | **2.2-2.4** |

**A flat ×3-4 does not make everyone equally short.** It lands the tanks, Magus and warriors where you want them (about
two thirds of the kit at ×1). It leaves daggers and bows able to buy nearly everything, because their actives are cheap.
And it halves the healers, who are already short today.

## 4. My proposal

1. **Price the split to a TARGET, not a flat multiplier.** Aim every path at the same affordability over 20-75. My pick is
   **0.65 at ×1**: you can afford about two thirds of your kit from kills alone, and with the Favor kept up you can afford
   roughly all of it by 75. Each path's passives then cost what it takes to hit that (the last column: ~2.2× for healers,
   ~4× for tanks, Magus and warriors, ~7× for daggers and bows). Your 3-4× is the middle of that range, not a rule for all.
2. **76+ stays ×1**, as you said. Nothing past 75 is repriced.
3. **Healers first, before any split:** at ×1 they are short with nothing split at all. Either that is intended (a buffer
   buys many actives), or the healer CSVs' SP column is priced high. Your call; the numbers are in §2.
4. **After you rule**, the design goes back to the four open questions in the Backlog entry (which stats split, the CSV
   shape, weight gates, the `Replaces` chains), and `--sp-budget` is re-run on the new CSVs to check each path lands on the
   target.

## 5. Questions for you

1. **Flat ×3-4 for every class, or one target for every class** (the multiplier then differs, 2.2× to 7.4×)?
2. **The target at ×1**: 0.65 (my pick), or tighter or looser? Remember the Favor multiplies what you earn, so the target
   is "what a player with no bonus can afford".
3. **Healers**: are they meant to be short at ×1 today (0.78-0.93), or is their SP column priced too high?
4. **Should the print count the Favor?** If you know roughly what share of your farming runs with the gauge up, I can add
   an "average bonus" column so the table shows what you actually feel in play.
