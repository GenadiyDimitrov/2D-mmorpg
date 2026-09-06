# Damage vs IG — the 2026-09-06 reference table, fitted

The owner supplied a full IG damage matrix with the STATS BEHIND IT (four attacker archetypes ×
four defender archetypes × four gear grades), after a session in which he said our damage was
*"laughable"*: *"a mage with a weapon t80m +16 does to someone with ~2k Def a 200-400 dmg"*,
*"harmonist elf that have 5100 p.atk does to an S grade robe user 400 with a crit"*,
*"The fight should be scary not potions to overheal the dmg"*.

This page fits OUR formulas to HIS table and says where the difference actually is. It is the
reference for the damage rework; `docs/Formulas.md` stays the short form of what the code does.

⚠ **Read the verdict before proposing a constant change.** The first proposal this session was to
raise `PhysicalK` 77→180 and `MagicK` 91→270. His table says that is wrong: **K is already right.**

## The verdict, in six lines

1. **`PhysicalK = 77` is CORRECT.** Fitting `77·(pAtk+power)/pDef` to his archer / tank rows, the K
   each row demands is FLAT ACROSS ALL FOUR DEFENDERS at every level — at 85: 86.7 / 87.0 / 87.0 /
   86.0; at 76: 75.5 / 75.9 / 75.6 / 74.7. **The ratio model and the defence divisor already
   reproduce his table.** The only drift is a gentle rise with level (58 → 70 → 76 → 87 across
   40/52/76/85) = a LEVEL MODIFIER we do not have.
2. 🔑 **PHYSICAL SKILL POWER IS SHORT — but on the ARCHER and WARRIOR kits only.** ⚠ *Revised
   2026-09-06. This line first read "~10x too small" as a global claim; the owner's answer showed it
   is not.* IG archer at 85: P.Atk 5800, **skill power 10200** — the power is TWICE the attack stat,
   where ours is authored `mod ≈ 1.2, flat 0` and contributes ~870 against P.Atk 4494. But the gap is
   **not uniform across our classes**: our elf harmonist's Sound Burst already hits a buffed mage for
   **495** where our archer's Precise Shot hits for **235**. The harmonist kits are close to right;
   the archer and warrior DAMAGE kits are the ones that were never authored. **His fix (2026-09-06):
   archer = elf harmonist skills + bow passives ×1.2; fighter = demon harmonist skills + 2H passives
   ×1.2.** 495 × 1.2 = 594 against IG's 870 @85 — inside ~1.5x, not 10x. 🔑 **His own Stab (11k@85 /
   15k@90) was already at IG scale** (IG fighter 12500@85): he had been authoring it right for a day
   before this page was written.
3. 🔴🔑 **MAGIC IS SHORT BECAUSE THE √ SITS ON THE PART THAT GROWS.** ⚠ *Revised 2026-09-06 — the
   first version of this line blamed the M.Def buff stack and a 3.3x M.Atk gap. The owner corrected
   both: his figures are **geared, NO buffs** (so our naked M.Def already matches IG), and magic
   PERCENTAGE buffs are **already outside the √** — `Entity.EffectiveMagicAttack` squares them on
   purpose (`magFactor * magFactor`, Owner 2026-07-16) so +32% really yields +32%.* What is still
   under the √ is `MagicAttack + magFlat`: the INT base, **the weapon's M.Atk and its enchant**, and
   flat buffs. That is the whole *"+16 does 200-400"* complaint (+16 staff = internal ×1.27 = damage
   **×1.13**), and it is the worse half because the base is the only part that GROWS. Naked, the
   magic attack term (`dmg·mDef/power`) grows **×8.8 for IG across 40→85 and ×2.6 for us across
   40→90**.
4. 🔑 **In his table magic damage does NOT track M.Def**, and the tank's wall is a FLAT PERCENT. At 85
   the mage hits for 950 / 950 / 1100 / 780 against M.Def 1600 / 1700 / 2000 / 1850 — highest against
   the HIGHEST M.Def — and only the tank is down, by 18% = his stated 20% spell-damage reduction.
   IG's whole four-class M.Def spread is 1.25x. (He has since ruled the tank cell **OK as it stands**.)
5. 🔑 **The sqrt is the wrong SHAPE, by his own numbers.** Fitting his 16 mage rows both ways, the K
   each level demands drifts **×5.1 under √M.Atk (41 → 210)** and only **×1.40 under linear M.Atk
   (1.43 → 2.00)**. The physical side drifts ×1.49 over the same range — so **linear magic and
   physical want ONE shared level modifier, and the √ wants a 5x correction with no counterpart.**
   Confirming it from the table itself: he lists mage M.Atk **6500** beside archer P.Atk **5800**,
   same order of magnitude. Under a √ model an M.Atk of 6500 contributes 80, which nobody designs.
6. 🔴🔑 **OUR ROBE/LIGHT P.Def IS 2.2-2.5x TOO LOW AT BASE — the physical spread problem.** Now that
   we know his sheets are naked: tank 3200 vs our 2682 ✅, but fighter 2400 vs our **944** and mage
   1600 vs our **715**. **IG's robe→plate spread is 2.0x; ours is 3.75x.** Our buffs are quietly
   covering for a light/robe base that is far too thin, and that is what produces the archer's 1:9.2
   across tank / fighter / mage.
## His stat sheet vs ours (IG @85 vs BalanceMatrix @90 mythic)

| stat | IG | ours UNBUFFED | ours BUFFED |
|---|---|---|---|
| archer P.Atk | 5800 | 3132 | 4494 |
| fighter P.Atk | 4900 | 2966 | 4256 |
| tank P.Atk | 3100 | — | — |
| **mage M.Atk (shown)** | **6500** | 1157 🔴 | 1976 🔴 |
| archer P.Def | 1850 | — | — |
| fighter P.Def | 2400 | 944 | 2313 ✅ |
🔑 The pattern, once his "geared, NO buffs" is applied: **M.Def is right at base on all three classes
and the shelf then roughly triples it; P.Def is right at base only for the TANK** — our fighter and
mage carry 2.2-2.5x too little, and the buffs are covering for it. So the physical spread problem is
in the BASE SHEETS and the magic one is in the SHAPE of the formula, not in the M.Def buffs.
## His skill/spell power ladder — the number to author against

| | 40 | 52 | 76 | 85 |
|---|---|---|---|---|
| archer skill power | 1200 | 2400 | 6200 | 10200 |
| fighter skill power | 1800 | 3200 | 7500 | 12500 |
| tank skill power | 800 | 1400 | 3100 | 5200 |
| **mage SPELL power** | **108** | **112** | **122** | **130** |

🔑🔑 **Physical power is in the THOUSANDS; magic power is ~110-130 flat.** The asymmetry is because
physical ADDS power to pAtk while magic MULTIPLIES power by the attack term. His authored nuke
(150@85 / 200@90) matches IG's 130@85 — **the magic side's authored power is already correct.**

## The two physical regimes in his table (deliberate, worth copying)

The FIGHTER rows need K 2.0-2.7x the archer's at every level, and the fighter's crit is exactly
**×2.00** everywhere while the archer's climbs **3.6 → 4.0 → 4.5 → 5.0** (40/52/76/85). End result at
85: archer crit 3750, fighter crit 4400 — comparable — but the archer's NON-crit is 3x lower.
**The archer lives on crits; the fighter lives on consistent hits.** Ours today: archer ×2.6,
mage ×2.0, fighter ×2.1. Mage crit is ×4.00 at EVERY level in his table (ours: ×2.0).

## What this asks for — see `BL-185` for the live list

The six steps, his rulings folded in (2026-09-06). **`BL-185` is authoritative; this is the summary.**

1. **Archer and warrior damage kits = the HARMONIST kits + 20%** — archer from the elf harmonist's
   skills + bow passives, fighter from the demon harmonist's + 2H passives. His recipe, and it is
   better than the "physical power is 10x short" claim this page first made: our harmonist already
   hits a buffed mage for **495** where our archer hits for **235**, so the harmonist kits are close
   to right and the archer/warrior kits are the ones that do not exist.
2. **Raise base light/robe P.Def toward IG's** (finding 6) — only alongside step 1, never alone.
3. **Take the √ off the base M.Atk and refit `MagicK`** (findings 3 and 5).
   ⚠ **`StatCalculator.MagicDamage` is shared with HEALS and mob casters** — all three or none.
   ⚠ **The squaring in `EffectiveMagicAttack` exists ONLY to cancel the √.** Remove one without the
   other and every magic buff pays 1.725² = **×2.98**.
4. **The shelf's COMPOUNDING, not its M.Def legs** — "cut Ward" is withdrawn. What stands is measured
   here and cites nothing external: buffing both sides drops every damage cell 25-40%, because HP
   (×2.05) and defence (×2.45) multiply while attack has one multiplier (×1.44).
5. **Crit multipliers UNCHANGED** (his ruling: ×1.35 Ferocity, ×1.35 harmony, ×1.2 Mark). Missing:
   the archer's **700 flat crit damage** and **+20% passives**. ⚠ Flat crit damage joins pAtk inside
   the ratio, so 700 at P.Atk 4494 is **+15.6%** on a basic crit and ~**+5%** once step 1 gives the
   archer's skills a real flat power.
6. **The level modifier, last**, if 1-5 leave one owed.

🔴 **Every step re-runs the `BL-13` boss-pace section in the same pass as `--dmgmatrix`.** Raising
PLAYER skill power raises player→boss damage but not boss→player (bosses mostly basic-attack), so
bosses get easier without getting more dangerous. Compensation goes on the boss, never the formula.

## Reproducing this

- our board: `dotnet run --project tools/BalanceMatrix -- --dmgmatrix 90 mythic --his --buffed`
- the fit: his rows are in `IG-reference.csv` beside this file; predicted = `77·(pAtk+power)/pDef`
  for physical and `91·power·√mAtk/mDef` for magic, and `K_needed = K·actual/predicted`.

## His table, verbatim

Columns: `Level, Equipment, Attacker, Attacker PAtk, Attacker MAtk, Skill/Spell Power,
Atk/Cast Speed, Defender, Defender PDef, Defender MDef, Min/Normal Dmg, Max/Crit Dmg, DPS`

```
40,C-Grade,Archer,1100,150,1200,550,Archer,420,320,320,1150,1100
40,C-Grade,Fighter,950,120,1800,650,Archer,420,320,750,1500,950
40,C-Grade,Mage,220,1050,108,850,Archer,420,320,450,1800,1250
40,C-Grade,Tank,650,140,800,500,Archer,420,320,180,580,620
40,C-Grade,Archer,1100,150,1200,550,Fighter,520,340,260,930,890
40,C-Grade,Fighter,950,120,1800,650,Fighter,520,340,600,1200,760
40,C-Grade,Mage,220,1050,108,850,Fighter,520,340,450,1800,1250
40,C-Grade,Tank,650,140,800,500,Fighter,520,340,145,460,490
40,C-Grade,Archer,1100,150,1200,550,Mage,360,400,370,1330,1270
40,C-Grade,Fighter,950,120,1800,650,Mage,360,400,880,1760,1120
40,C-Grade,Mage,220,1050,108,850,Mage,360,400,520,2080,1440
40,C-Grade,Tank,650,140,800,500,Mage,360,400,210,670,720
40,C-Grade,Archer,1100,150,1200,550,Tank,650,380,210,750,720
40,C-Grade,Fighter,950,120,1800,650,Tank,650,380,480,960,610
40,C-Grade,Mage,220,1050,108,850,Tank,650,380,380,1520,1050
40,C-Grade,Tank,650,140,800,500,Tank,650,380,115,370,390
52,B-Grade,Archer,1850,220,2400,680,Archer,720,550,410,1650,1680
52,B-Grade,Fighter,1600,180,3200,780,Archer,720,550,1100,2200,1520
52,B-Grade,Mage,310,1950,112,1150,Archer,720,550,580,2320,1850
52,B-Grade,Tank,1050,200,1400,600,Archer,720,550,240,840,920
52,B-Grade,Archer,1850,220,2400,680,Fighter,850,580,350,1400,1420
52,B-Grade,Fighter,1600,180,3200,780,Fighter,850,580,930,1860,1280
52,B-Grade,Mage,310,1950,112,1150,Fighter,850,580,580,2320,1850
52,B-Grade,Tank,1050,200,1400,600,Fighter,850,580,205,715,780
52,B-Grade,Archer,1850,220,2400,680,Mage,610,650,480,1920,1950
52,B-Grade,Fighter,1600,180,3200,780,Mage,610,650,1300,2600,1790
52,B-Grade,Mage,310,1950,112,1150,Mage,610,650,660,2640,2110
52,B-Grade,Tank,1050,200,1400,600,Mage,610,650,280,980,1070
52,B-Grade,Archer,1850,220,2400,680,Tank,1100,620,270,1080,1100
52,B-Grade,Fighter,1600,180,3200,780,Tank,1100,620,720,1440,990
52,B-Grade,Mage,310,1950,112,1150,Tank,1100,620,490,1960,1560
52,B-Grade,Tank,1050,200,1400,600,Tank,1100,620,155,545,590
76,S-Grade,Archer,3400,380,6200,920,Archer,1250,980,580,2600,2850
76,S-Grade,Fighter,2900,310,7500,1100,Archer,1250,980,1600,3200,2250
76,S-Grade,Mage,520,3800,122,1650,Archer,1250,980,720,2880,2730
76,S-Grade,Tank,1850,340,3100,820,Archer,1250,980,310,1150,1380
76,S-Grade,Archer,3400,380,6200,920,Fighter,1550,1020,470,2110,2310
76,S-Grade,Fighter,2900,310,7500,1100,Fighter,1550,1020,1290,2580,1810
76,S-Grade,Mage,520,3800,122,1650,Fighter,1550,1020,720,2880,2730
76,S-Grade,Tank,1850,340,3100,820,Fighter,1550,1020,250,925,1110
76,S-Grade,Archer,3400,380,6200,920,Mage,1100,1200,660,2970,3250
76,S-Grade,Fighter,2900,310,7500,1100,Mage,1100,1200,1820,3640,2550
76,S-Grade,Mage,520,3800,122,1650,Mage,1100,1200,830,3320,3150
76,S-Grade,Tank,1850,340,3100,820,Mage,1100,1200,350,1300,1560
76,S-Grade,Archer,3400,380,6200,920,Tank,2050,1100,350,1570,1720
76,S-Grade,Fighter,2900,310,7500,1100,Tank,2050,1100,970,1940,1360
76,S-Grade,Mage,520,3800,122,1650,Tank,2050,1100,610,2440,2310
76,S-Grade,Tank,1850,340,3100,820,Tank,2050,1100,190,700,840
85,Vesper,Archer,5800,620,10200,1350,Archer,1850,1600,750,3750,4600
85,Vesper,Fighter,4900,510,12500,1500,Archer,1850,1600,2200,4400,3100
85,Vesper,Mage,850,6500,130,1999,Archer,1850,1600,950,3800,4180
85,Vesper,Tank,3100,580,5200,1150,Archer,1850,1600,420,1680,2100
85,Vesper,Archer,5800,620,10200,1350,Fighter,2400,1700,580,2900,3550
85,Vesper,Fighter,4900,510,12500,1500,Fighter,2400,1700,1700,3400,2390
85,Vesper,Mage,850,6500,130,1999,Fighter,2400,1700,950,3800,4180
85,Vesper,Tank,3100,580,5200,1150,Fighter,2400,1700,320,1280,1600
85,Vesper,Archer,5800,620,10200,1350,Mage,1600,2000,870,4350,5340
85,Vesper,Fighter,4900,510,12500,1500,Mage,1600,2000,2550,5100,3600
85,Vesper,Mage,850,6500,130,1999,Mage,1600,2000,1100,4400,4840
85,Vesper,Tank,3100,580,5200,1150,Mage,1600,2000,480,1920,2400
85,Vesper,Archer,5800,620,10200,1350,Tank,3200,1850,430,2150,2640
85,Vesper,Fighter,4900,510,12500,1500,Tank,3200,1850,1270,2540,1790
85,Vesper,Mage,850,6500,130,1999,Tank,3200,1850,780,3120,3430
85,Vesper,Tank,3100,580,5200,1150,Tank,3200,1850,240,960,1200
```
