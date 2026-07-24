# Exp curve - proposed (L2-derived), levels 1-100

Generated 2026-07-24 from the owner's spec in memory `exp-party-and-drop-design`.
Implemented in `Game.Shared/ExpCurve.cs`; machine-readable copy:
[ExpCurve.csv](ExpCurve.csv) (TAB-separated).

## Formulas
```
exp_to_next(L) = levels 1-85  : the real Lineage 2 table (masterwork source) - a power law
                                8.492*L^3.2891 up to ~50, then SEVEN wall multipliers at
                                51/56/61/66/72/77/80 stacking to ~52x by level 85.
                 levels 86-100: 4Game per-level costs spliced on (see the note below).

mob_exp(L)     = L >= 30 : 0.026314 * L^3.2427    <- fitted to the owner`s L50/L75/L85 anchors
                 L <  30 : geometric interpolation through
                           (1,68) (2,148) (3,268) (5,631) (10,1149) (20,1566) (30,1622)
                 Chosen so kills-to-level land on the owner`s targets: 1-2-5 for the first
                 levels, ~20 by level 10, ~120 by level 20. THESE SEVEN NUMBERS ARE THE
                 EARLY-GAME TUNING KNOB; nothing above level 30 is affected by them.

sp_ratio(L)    = geometric interpolation over (1,1.00) (50,0.15) (70,0.10) (85,0.05),
                 held at 0.05 beyond 85.
mob_sp(L)      = mob_exp(L) * sp_ratio(L)
mobs_to_level  = ceil( exp_to_next(L) / mob_exp(L) )  -- solo, no party bonus, zero level gap
```

## Table

| lvl | exp to next | mob exp | sp ratio | mob sp | mobs to level | cumulative mobs | source |
|----:|------------:|--------:|---------:|-------:|--------------:|----------------:|:-------|
| 1 | 68 | 68 | 1.0000 | 68 | 1 | 1 | masterwork |
| 2 | 295 | 148 | 0.9620 | 142 | 2 | 3 | masterwork |
| 3 | 805 | 268 | 0.9255 | 248 | 4 | 7 | masterwork |
| 4 | 1 716 | 411 | 0.8903 | 366 | 5 | 12 | masterwork |
| 5 | 3 154 | 631 | 0.8565 | 540 | 5 | 17 | masterwork |
| 6 | 5 249 | 711 | 0.8240 | 586 | 8 | 25 | masterwork |
| 7 | 8 136 | 802 | 0.7927 | 636 | 11 | 36 | masterwork |
| 8 | 11 955 | 904 | 0.7626 | 689 | 14 | 50 | masterwork |
| 9 | 16 851 | 1 019 | 0.7336 | 748 | 17 | 67 | masterwork |
| 10 | 22 973 | 1 149 | 0.7058 | 811 | 20 | 87 | masterwork |
| 11 | 30 475 | 1 185 | 0.6790 | 805 | 26 | 113 | masterwork |
| 12 | 39 516 | 1 222 | 0.6532 | 798 | 33 | 146 | masterwork |
| 13 | 50 261 | 1 261 | 0.6284 | 792 | 40 | 186 | masterwork |
| 14 | 62 876 | 1 300 | 0.6045 | 786 | 49 | 235 | masterwork |
| 15 | 77 537 | 1 341 | 0.5816 | 780 | 58 | 293 | masterwork |
| 16 | 94 421 | 1 384 | 0.5595 | 774 | 69 | 362 | masterwork |
| 17 | 113 712 | 1 427 | 0.5382 | 768 | 80 | 442 | masterwork |
| 18 | 135 596 | 1 472 | 0.5178 | 762 | 93 | 535 | masterwork |
| 19 | 160 266 | 1 518 | 0.4981 | 756 | 106 | 641 | masterwork |
| 20 | 187 922 | 1 566 | 0.4792 | 750 | 121 | 762 | masterwork |
| 21 | 218 762 | 1 572 | 0.4610 | 725 | 140 | 902 | masterwork |
| 22 | 252 997 | 1 577 | 0.4435 | 699 | 161 | 1 063 | masterwork |
| 23 | 290 836 | 1 583 | 0.4267 | 675 | 184 | 1 247 | masterwork |
| 24 | 332 497 | 1 588 | 0.4105 | 652 | 210 | 1 457 | masterwork |
| 25 | 378 201 | 1 594 | 0.3949 | 629 | 238 | 1 695 | masterwork |
| 26 | 428 173 | 1 599 | 0.3799 | 607 | 268 | 1 963 | masterwork |
| 27 | 482 647 | 1 605 | 0.3654 | 587 | 301 | 2 264 | masterwork |
| 28 | 541 857 | 1 611 | 0.3516 | 566 | 337 | 2 601 | masterwork |
| 29 | 606 042 | 1 616 | 0.3382 | 547 | 376 | 2 977 | masterwork |
| 30 | 675 450 | 1 622 | 0.3254 | 528 | 417 | 3 394 | masterwork |
| 31 | 750 330 | 1 804 | 0.3130 | 565 | 416 | 3 810 | masterwork |
| 32 | 830 937 | 2 000 | 0.3011 | 602 | 416 | 4 226 | masterwork |
| 33 | 917 531 | 2 209 | 0.2897 | 640 | 416 | 4 642 | masterwork |
| 34 | 1 010 378 | 2 434 | 0.2787 | 678 | 416 | 5 058 | masterwork |
| 35 | 1 109 744 | 2 674 | 0.2681 | 717 | 416 | 5 474 | masterwork |
| 36 | 1 215 906 | 2 930 | 0.2579 | 756 | 415 | 5 889 | masterwork |
| 37 | 1 329 143 | 3 202 | 0.2481 | 795 | 416 | 6 305 | masterwork |
| 38 | 1 449 736 | 3 491 | 0.2387 | 833 | 416 | 6 721 | masterwork |
| 39 | 1 577 978 | 3 798 | 0.2296 | 872 | 416 | 7 137 | masterwork |
| 40 | 1 714 158 | 4 123 | 0.2209 | 911 | 416 | 7 553 | masterwork |
| 41 | 1 858 578 | 4 466 | 0.2125 | 949 | 417 | 7 970 | masterwork |
| 42 | 2 011 538 | 4 829 | 0.2045 | 987 | 417 | 8 387 | masterwork |
| 43 | 2 173 347 | 5 212 | 0.1967 | 1 025 | 417 | 8 804 | masterwork |
| 44 | 2 344 318 | 5 616 | 0.1892 | 1 063 | 418 | 9 222 | masterwork |
| 45 | 2 524 767 | 6 040 | 0.1820 | 1 100 | 419 | 9 641 | masterwork |
| 46 | 2 715 019 | 6 487 | 0.1751 | 1 136 | 419 | 10 060 | masterwork |
| 47 | 2 915 398 | 6 955 | 0.1685 | 1 172 | 420 | 10 480 | masterwork |
| 48 | 3 126 237 | 7 446 | 0.1621 | 1 207 | 420 | 10 900 | masterwork |
| 49 | 3 347 873 | 7 961 | 0.1559 | 1 241 | 421 | 11 321 | masterwork |
| 50 | 5 370 971 | 8 500 | 0.1500 | 1 275 | 632 | 11 953 | masterwork |
| 51 | 5 737 357 | 9 064 | 0.1470 | 1 332 | 633 | 12 586 | masterwork |
| 52 | 6 121 498 | 9 653 | 0.1440 | 1 390 | 635 | 13 221 | masterwork |
| 53 | 6 523 923 | 10 268 | 0.1411 | 1 449 | 636 | 13 857 | masterwork |
| 54 | 6 945 178 | 10 910 | 0.1383 | 1 509 | 637 | 14 494 | masterwork |
| 55 | 9 847 742 | 11 579 | 0.1355 | 1 569 | 851 | 15 345 | masterwork |
| 56 | 10 461 823 | 12 275 | 0.1328 | 1 630 | 853 | 16 198 | masterwork |
| 57 | 11 103 227 | 13 001 | 0.1302 | 1 692 | 855 | 17 053 | masterwork |
| 58 | 11 772 715 | 13 755 | 0.1275 | 1 754 | 856 | 17 909 | masterwork |
| 59 | 12 471 057 | 14 539 | 0.1250 | 1 817 | 858 | 18 767 | masterwork |
| 60 | 19 798 547 | 15 353 | 0.1225 | 1 880 | 1 290 | 20 057 | masterwork |
| 61 | 20 936 137 | 16 199 | 0.1200 | 1 944 | 1 293 | 21 350 | masterwork |
| 62 | 22 120 557 | 17 076 | 0.1176 | 2 008 | 1 296 | 22 646 | masterwork |
| 63 | 23 353 014 | 17 985 | 0.1152 | 2 073 | 1 299 | 23 945 | masterwork |
| 64 | 24 634 736 | 18 927 | 0.1129 | 2 138 | 1 302 | 25 247 | masterwork |
| 65 | 34 622 619 | 19 903 | 0.1107 | 2 203 | 1 740 | 26 987 | masterwork |
| 66 | 36 467 935 | 20 913 | 0.1084 | 2 268 | 1 744 | 28 731 | masterwork |
| 67 | 38 383 956 | 21 958 | 0.1063 | 2 333 | 1 749 | 30 480 | masterwork |
| 68 | 40 372 393 | 23 039 | 0.1041 | 2 399 | 1 753 | 32 233 | masterwork |
| 69 | 42 434 976 | 24 156 | 0.1020 | 2 465 | 1 757 | 33 990 | masterwork |
| 70 | 44 573 456 | 25 310 | 0.1000 | 2 531 | 1 762 | 35 752 | masterwork |
| 71 | 58 487 000 | 26 501 | 0.0955 | 2 530 | 2 207 | 37 959 | masterwork |
| 72 | 73 627 796 | 27 731 | 0.0912 | 2 528 | 2 656 | 40 615 | masterwork |
| 73 | 90 058 594 | 28 999 | 0.0871 | 2 525 | 3 106 | 43 721 | masterwork |
| 74 | 107 843 995 | 30 307 | 0.0831 | 2 519 | 3 559 | 47 280 | masterwork |
| 75 | 127 050 464 | 31 655 | 0.0794 | 2 512 | 4 014 | 51 294 | masterwork |
| 76 | 220 000 006 | 33 045 | 0.0758 | 2 504 | 6 658 | 57 952 | masterwork |
| 77 | 360 000 000 | 34 475 | 0.0724 | 2 495 | 10 443 | 68 395 | masterwork |
| 78 | 588 000 000 | 35 949 | 0.0691 | 2 484 | 16 357 | 84 752 | masterwork |
| 79 | 2 100 724 166 | 37 465 | 0.0660 | 2 472 | 56 072 | 140 824 | masterwork |
| 80 | 2 100 000 000 | 39 024 | 0.0630 | 2 458 | 53 814 | 194 638 | masterwork |
| 81 | 2 520 000 000 | 40 629 | 0.0602 | 2 444 | 62 025 | 256 663 | masterwork |
| 82 | 3 024 000 000 | 42 278 | 0.0574 | 2 428 | 71 527 | 328 190 | masterwork |
| 83 | 3 628 800 000 | 43 973 | 0.0548 | 2 412 | 82 524 | 410 714 | masterwork |
| 84 | 4 354 560 000 | 45 714 | 0.0524 | 2 394 | 95 257 | 505 971 | masterwork |
| 85 | 5 977 044 329 | 47 502 | 0.0500 | 2 375 | 125 828 | 631 799 | masterwork |
| 86 | 8 207 524 971 | 49 338 | 0.0500 | 2 467 | 166 354 | 798 153 | 4game |
| 87 | 10 839 148 748 | 51 223 | 0.0500 | 2 561 | 211 608 | 1 009 761 | 4game |
| 88 | 14 341 344 002 | 53 157 | 0.0500 | 2 658 | 269 793 | 1 279 554 | 4game |
| 89 | 19 378 851 343 | 55 141 | 0.0500 | 2 757 | 351 442 | 1 630 996 | 4game |
| 90 | 24 936 584 968 | 57 175 | 0.0500 | 2 859 | 436 145 | 2 067 141 | 4game |
| 91 | 31 535 749 655 | 59 261 | 0.0500 | 2 963 | 532 151 | 2 599 292 | 4game |
| 92 | 36 909 329 234 | 61 399 | 0.0500 | 3 070 | 601 139 | 3 200 431 | 4game |
| 93 | 51 380 258 046 | 63 590 | 0.0500 | 3 180 | 807 993 | 4 008 424 | 4game |
| 94 | 58 255 218 000 | 65 834 | 0.0500 | 3 292 | 884 881 | 4 893 305 | 4game |
| 95 | 111 070 429 959 | 68 132 | 0.0500 | 3 407 | 1 630 225 | 6 523 530 | 4game |
| 96 | 214 951 605 018 | 70 485 | 0.0500 | 3 524 | 3 049 608 | 9 573 138 | 4game |
| 97 | 423 452 255 669 | 72 894 | 0.0500 | 3 645 | 5 809 152 | 15 382 290 | 4game |
| 98 | 940 673 308 350 | 75 359 | 0.0500 | 3 768 | 12 482 561 | 27 864 851 | 4game |
| 99 | 3 303 754 472 044 | 77 881 | 0.0500 | 3 894 | 42 420 546 | 70 285 397 | 4game |
| 100 | 5 286 007 155 270 | 80 461 | 0.0500 | 4 023 | 65 696 514 | 135 981 911 | 4game |

## Findings — read before implementing

**1. The opening is now fast, as asked.** `mobs_to_level` runs **1, 2, 4, 5, 5** for levels 1-5, reaches
**20 at level 10**, **121 at level 20**, and joins the steady state at 30. Reaching level 20 costs 641
mobs in total — roughly two hours. Reaching 50 costs 11 321 (~31 h, ~13 days at 2.5 h/day).

**2. ⚠ The cost: mob exp goes nearly FLAT between 10 and 30.** A level-10 mob pays 1 149, a level-20 mob
1 566, a level-30 mob 1 622 — only ×1.4 across twenty levels, while `exp_to_next` grows ×29 over the same
span. That flatness is not a modelling artifact, it is the arithmetic of the request: killing 20 mobs at
level 10 and 417 at level 30 *forces* it. The visible consequence in play is that mobs in the 10-30 band
feel like they pay about the same regardless of level, and the difficulty ramp shows up purely as "the
bar moves slower". If that reads badly on the phone, the fix is to soften the early targets (e.g. 3-5-8
kills for the first levels instead of 1-2-5), which lets mob exp climb through the twenties again.

**3. Growth is heavily back-loaded.** mob exp rises ×24 across levels 1-30, then ×29 across 30-85. Low
mobs are therefore disproportionately valuable per kill. This is NOT exploitable: the symmetric
`0.85^(gap-5)` penalty pays zero at a 13-level gap, so nobody can farm the cheap early mobs from above.

**4. The curves track each other through the mid game.** Levels 30-45 sit flat at ~415-420 mobs per
level because the mob exponent (3.2427) almost exactly matches the player curve's (3.2891). Nothing
needs tuning in that band.

**5. The walls are the entire endgame.** `mobs_to_level` leaves the flat band exactly where the L2 walls
begin: 632 at 50, 1 290 at 60, 4 014 at 75, then **56 072 at 79** and **125 828 at 85**. Levels 79-85
alone are ~500 000 of the 631 799 total. Deliberate (x1 server, owner confirmed) — the one knob if the
endgame ever needs shortening, and it should be turned by softening walls, never by touching either base
curve.

**6. Totals (solo, x1, zero level gap).** 1->20 = 641 mobs. 1->50 = **11 321** (~31 h, ~13 days at
2.5 h/day). 1->85 (reaching 86) = **631 799 mobs** (~1 755 h, ~702 days at 2.5 h/day).

**7. EXP is `long`; SP deliberately SATURATES at `int.MaxValue`.** ✅ Both resolved.
The exp side is done — `ExpToNext`, `MobExpValue`, `MobSpValue` and `AwardExp` all take/return `long`,
and level 101's cumulative total (1.06e13) sits ~873 000x under `long.MaxValue`, so no level this curve
will ever cover can overflow.

`SkillPoints` stays an `int` **by decision** (owner, 2026-07-24), not as a stopgap. A full 1->85 earns
~1.5e9 SP at x1, so the 2.15e9 ceiling is genuinely reachable at higher SP rates — but the planned sink
makes that a non-issue: **SP EXTRACTION** will convert 1 000 000 000 SP into one **"SP bottle"** item,
and skills will cost bottles + gold instead of raw SP. SP is therefore *drained* rather than accumulated
forever, so the counter never needs widening. `AwardExp` saturates at `int.MaxValue`; the one thing that
must never happen is a silent wrap to negative. Roadmapped as **deferred** — see `docs/Roadmap.md`.

**8. SP shape is provisional.** The owner's four anchors do not sit on one curve, so this uses geometric
interpolation between them: it hits all four exactly and is monotonic, but the shape *between* anchors
is a choice, not a spec. Note `mob_sp` peaks near level 68 (~2 530) then declines — a level-85 mob gives
less SP than a level-68 one, because the ratio falls faster than mob exp rises. Confirm that is intended.

## Random exp factor (owner, 2026-07-24) — ±20% per kill

Every kill rolls a uniform multiplier in **[0.80, 1.20]** applied to that kill's exp. The point is that
identical mobs stop paying an identical number, without the pace moving.

The roll applies to the **FINAL exp the player actually receives** (owner) — not to the base mob value.
So if a kill would nominally pay 20 000, it pays **16 000-24 000**.

```
awarded_exp = round( base_exp(L) * gap_penalty * party_share * rate * uniform(0.80, 1.20) )
                                                                     ^ last, on the end result
```

**One roll per kill, applied to exp AND sp together** — so the sp/exp ratio stays meaningful rather than
drifting apart on independent rolls.

**In a party, the single roll is shared by everyone on that kill** — the POT varies, not each member
independently. Rolling per member would let two people standing on the same corpse see 16k and 24k from
one mob, which reads as a bug and invites accusations of a rigged split. Since multiplication commutes,
a shared roll is arithmetically the same as rolling the pot before the split, and it keeps every
member's share a fixed fraction of the same number.

**Why it is safe: variance shrinks as √N.** A uniform ±20% has a standard deviation of 11.55%, so over
N kills the spread of the total is `0.1155/√N`. Simulated over 2 000 trials per level:

| level | deterministic | avg | min-max | p05-p95 |
|---:|---:|---:|---:|---:|
| 10 | 20 | 20.5 | 19-22 | +/-0 |
| 20 | 121 | 120.5 | 116-125 | +/-2 |
| 30 | 417 | 417.0 | 409-426 | +/-4 |
| 40 | 416 | 416.3 | 409-424 | +/-4 |
| 49 | 421 | 421.0 | 413-430 | +/-4 |
| 60 | 1 290 | 1 290.1 | 1 276-1 303 | +/-7 |
| 70 | 1 762 | 1 761.8 | 1 747-1 778 | +/-8 |

So a +/-20% swing on every kill moves a whole level by about +/-1%. At level 85 (125 828 kills) the
effect is +/-0.03% — statistically invisible.

**✅ No floor, no exemption — the plain 0.80-1.20 roll applies at EVERY level, level 1 included.**
The low-level asymmetry (level 1 averaging 1.5 kills instead of 1, because a sub-1.0 roll costs a whole
extra mob and a high roll saves nothing) was raised and **explicitly declined** by the owner: *"the 1st
level you will get -20 but next level you'll get more exp… if I need one or two mobs it's not a
problem."* Reaching level 10 costs 87 mobs, which the roll spreads to roughly 70-104; the worst case is
an unlucky player sitting at level 10 while a lucky friend hits 11, and the gap thins to nothing as N
grows. **Do not reintroduce a floor.**

## Party exp — the pot is shared, the penalty is personal

```
pot             = base_exp(mob) * randomRoll(0.80-1.20) * partyBonus(n)   // NO penalty here
share           = pot / memberCount                                        // equal for everyone
awarded(member) = share * gapPenalty(|member.Level - mob.Level|)           // personal

gapPenalty(gap) = gap <= 5 ? 1.0 : gap >= 13 ? 0.0 : pow(0.85, gap - 5)
partyBonus(n)   = n <= 1 ? 1.0 : n <= 6 ? 1.0 + 0.2*(n-1) : 2.0 + 0.1*(n-6)
```

Worked example (owner's): a level-75 top-damages a level-75 mob worth ~33k → +10% roll = 36k → x2.0 for
six members = 72k → /6 = **12k each**. Members at 70-80 bank the full 12k; a **level-65** member (gap 10)
banks `12k * 0.85^5 = 5 324`; anyone 13+ levels off banks nothing.

**The killer does NOT gate the party's exp.** The pot is the full mob value regardless of who landed the
kill — a level-60 who top-damages a level-75 mob earns 0 *for himself* while his level-75 party mates
still bank full shares. Anti-powerlevelling is enforced entirely by the **personal** penalty, in both
directions: a low-level cannot be dragged through a high zone, and a high-level gains nothing babysitting
a low one. The killer still decides **the drop**, and is still whoever dealt the most damage.

## Levels 86-100 — spliced from 4Game (added 2026-07-24)

The table now runs to **level 100**, so the cap can be raised later without revisiting the curve.
`GameConstants.MaxPlayerLevel` is 90 and sits comfortably inside it; nothing is extrapolated.

Levels 1-85 come from the masterwork source; **86-100 splice on 4Game's per-level costs**. Joining here
rather than at 4Game's own level 85 is the owner's call and the right one: 4Game's level-85 total is only
~5.0bn against masterwork's 19.8bn, so joining there would jump **x8.6 in a single level**, where joining
at 86 reads a smooth **x1.37**.

**⚠ 4Game publishes levels 88 and 89 TRANSPOSED.** As printed, level 89 costs 10 839 148 748 against
level 88's 14 341 344 002 — so level 89 is **CHEAPER than 88** (x0.76), which would mean the bar moves
*faster* at the higher level. Swapped back into order here, giving 87->90 = x1.32, x1.32, x1.35 — smooth,
and unmistakably what was meant. Both `ExpCurve.cs` and the CSV carry the corrected order; revert the two
values if you ever want the published numbers verbatim.

**Scale check.** Level 101's cumulative total is 1.06e13 — roughly **873 000x** under `long.MaxValue`, so
there is no overflow risk on the exp side at any level this curve will ever cover. (`SkillPoints` is a
different story — see finding 7.)

**What the tail actually costs.** Levels 86-100 add ~135.3 million mobs on top of the 631 799 needed to
reach 86 — a total of **135 981 911** for 1->100, or ~377 700 hours at 10 s/kill. That is L2's real
post-85 curve and it is not meant to be reachable by ordinary play; it is here so the ceiling exists.
