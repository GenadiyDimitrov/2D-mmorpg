# Anti-type mobs — the measured list (`BL-280`, 2026-09-24, ❓ yours to rule)

> *"anti mage and anti fighter and anti archer mobs need to be in self zones"* … **for now:** pull the anti-type
> mobs out so 1-40 levels the same for every archetype.

## ✅ Your ruling, 2026-09-24 (supersedes the questions and picks below)

> My idea of anti mobs is to have zones:
> 1. Zone with
>   - one type mobs that have +60% bow resistance and -20% resist to blunt/sword/fangs
>   - other tyoe mobs with +50% mRes and  -20% to blunt/sword/fangs
> 2. another zone
>   -  mobs with +60% res to blunt/sword/fangs and -20% mres
>   - other mobs with +60% res to blunt/sowrd/fangs and -20% to bow
> 3. Third zone with mobs with a 1/2hp passive and more clustered so an aoe to gather easier and aoe kill
>
> Those all zones can repeat @55~60,@75~80 - they are more prof of concept.
>
> Current resistance mobs to be removed from current zones.
>
> Agressive mobs to be removed from normal zones .. Only zones 80+ and any dungeon are all aggressive and maybe near a
> field boss

**How it reads against the engine** (a note for building it, not open questions):
- "Fangs" = daggers, which are duals (`WeaponType.Dual`). Sword and dual share **one** coefficient today
  (`MobMod.PierceResist`), and blunt has its own (`BluntResist`). So "blunt/sword/fangs" = both, set to the same value.
- A weapon resist is a **P.Def coefficient** (`WeaponDefenceCoef`); mRes is a **damage divisor** (`MagicDefCoef`).
  So "+60% bow" = `BowDefResist 1.6` and "−20% blunt/sword" = `0.8`; "+50% mRes" = `MagicResist +0.50`.
  Check them with `--antitype` after authoring: zone 1's bow kind should read about bow ×1.6 and melee ×0.8.
- These are **new mob ids** in new zones. The eight measured below lose their skew or leave the roster. ⚠ Either
  way, `shield_skeleton`'s class-change hunt must keep an in-band target.
- The aggression rule touches `WorldPlan`'s `AggressiveRamp`/`PickAggressive` for every generated camp below 80.

## ❓ Build plan, 2026-09-24: waiting on your OK (nothing is built yet)

**1. Twelve new creatures: three zones × two kinds × two tiers.** The names are mine and original, so rename freely.
Each kind has one **natural level (58 or 78)**, which decides its drops. Its camps use `ForceZoneLevel`, so it
spawns at every level in its field (55-60 or 75-80). One id covers the whole 5-level spread, the way the 85-90
Summit roster already works.

| zone | kind | 55-60 id (lvl 58) | 75-80 id (lvl 78) | passive (`MobMod`) |
| ---- | ---- | ----------------- | ----------------- | ------------------ |
| 1 · melee zone | anti-bow | `shellback_crawler` Shellback Crawler | `ironshell_crawler` Ironshell Crawler | `BowDefResist 1.6`, `PierceResist 0.8`, `BluntResist 0.8` |
| 1 · melee zone | anti-mage | `hexward_golem` Hexward Golem | `frostward_golem` Frostward Golem | `MagicResist +0.50`, `PierceResist 0.8`, `BluntResist 0.8` |
| 2 · ranged zone | magic-weak | `gloomhusk_treant` Gloomhusk Treant | `rimebark_treant` Rimebark Treant | `PierceResist 1.6`, `BluntResist 1.6`, `MagicResist −0.20` |
| 2 · ranged zone | bow-weak | `marsh_harpy` Marsh Harpy | `storm_harpy` Storm Harpy | `PierceResist 1.6`, `BluntResist 1.6`, `BowDefResist 0.8` |
| 3 · AoE zone | swarm | `mire_swarmling` Mire Swarmling | `frost_swarmling` Frost Swarmling | `Hp 0.5` |
| 3 · AoE zone | swarm | `mudskitter` Mudskitter | `rimeskitter` Rimeskitter | `Hp 0.5` |

"Sword/dual" is one coefficient (`PierceResist`), so fangs come with it for free. Blunt is set to the same value.
A kind's other channels stay neutral: the anti-bow crawler takes normal magic, for example.

**2. They must not leak into other camps.** A normal template joins every generated camp whose band holds its level
(`MobCatalog.InBand`). `HandPlaced` would keep them out, but it also skips them when drop profiles are dealt, so they
would drop no gear. I add a new template flag, **`OwnField`**: `InBand` skips it, and it still gets a drop profile.
⚠ Adding six creatures each to the 52-60 and 76-79 profile bands **re-deals those two bands**. Some existing
creatures there will change which gear kinds they carry. The 76-79 band has only five creatures today, so that one
gains the most.

**3. Six new fields, two camps each.** The camps are `B(55,57)` + `B(58,60)` and `B(75,77)` + `B(78,80)`, each
with an explicit roster (the zone's two kinds) and `force: true`:
- **55-60 → Greymarsh** (its 40-60 city): Shellback Flats (1), Harpy Fen (2), Swarming Mire (3).
- **75-80 → Frostmere** (its 76-90 city): Ironshell Drifts (1), Stormcrest Ridge (2), Rimeskitter Hollow (3).
- Bearings and distances are fitted by booting against `ValidateLayout`, the same way the `BL-68` grid was. Greymarsh
  has 180° free and Frostmere has 0°, and the rest go on a second ring. They are new ground, so no existing camp
  moves. No elite camps are added.
- **Zone 3 is denser**: 20 creatures per camp instead of 11, in a 500-radius camp instead of 700. That is about 3.5×
  the density.
- HP: the field ladder still applies (×1.5 below 76, ×2 at 76+), so zone 3 lands at ×0.75 / ×1.0 of a plain mob.

**4. The eight old anti-type mobs: neutralise them, don't delete them** (*my pick*). Their skewing passive is removed
(`AntiMagic`/`AntiPhysical`/`Stoneplate`/`Magic Monster`), and each keeps its id, name, role, weapon and drops. This:
- takes the resists out of every current zone, which is your instruction;
- keeps `shield_skeleton` as the Tank/Healer/Nuker `mobB` with no quest edit, and it is no longer a 1.5× hunt for
  the Tank;
- keeps `dread_knight`'s gathering contract (`Quests.Repeatable.cs`) and the Sunken Vale roster that uses
  `aether_wisp` unchanged;
- ⚠ also makes the **dungeon** copies plain: `grave_lich` (boss, 44), `dread_knight` (boss, 65) and `obsidian_knight`
  (a room mob, 63) share the template. A boss's identity comes from its `BossProfile` (phases, adds, enrage), not
  from these passives, so I'd accept that. Say if you want the dungeon copies kept as they are (that costs a
  boss-only twin template for each).

**5. Aggression: only camps whose band starts at 80.** `AggressiveRamp` becomes `band.Min >= 80 ? 3 : 0`.
- **Loses aggression:** every generated normal camp from 13 to 79, which is all of Stonewatch, Greymarsh and
  Ironreach, plus Frostmere Wastes 76-77 and 78-79. The six new fields are peaceful too.
- **Keeps it:** Wastes 80, Radiant Expanse, Dawnbreak Summit, **every elite camp** (aggressive by rank, which a
  lure needs), **every dungeon** (its rooms are elite rank) and the two **field-boss** flank rosters (Wastes boss,
  Sunken Vale). That covers your "maybe near a field boss".
- I read "80+" as *everything in the camp is 80+*, so a camp that runs 78-80 stays peaceful.

**6. One question of mine: zone 3's EXP.** A kill pays by level alone, not by HP (`ExpCurve.MobExpReward`). So a
half-HP swarm pays full EXP for half the work, even to a single-target class, and zone 3 becomes the best EXP/hr in
its band for **everyone**, not only for AoE. *My pick:* halve its EXP and gold with a new `MobMod.Reward 0.5`. A
single-target class then earns about what it earns elsewhere, and the density bonus goes to AoE only. Say "full"
if you meant the zone as a general fast farm.

**Verification once built:** `--antitype` should show the 8 old mobs neutral, the crawler at bow ≈1.6 / melee ≈0.8,
the golem at mage ≈1.5 / melee ≈0.8, and the harpy and treant at melee ≈1.6 with bow or mage ≈0.8.
Then `--dump-mob-csv`, the CHANGELOG, `Formulas.md` (the aggression line), and SmokeTest.

**Rule on:** (a) names/ids OK? (b) neutralise the 8, and the dungeon copies too? (c) zone 3's EXP: half or full?
(d) aggression = band starts at 80?

Nobody had ever listed which mobs are "anti-type". This is that list, **measured, not read off the passives**:

```
dotnet run --project tools/BalanceMatrix -- --antitype [maxLevel]
```

For every rostered template (bosses, guards, dummies and hand-placed creatures skipped) it builds three attackers at
the mob's own level in their own tier's gear: a **Human nuker**, a **2H-sword warrior** and a **bow rogue**. Each
attacker's **kill time on the mob** is divided by the same attacker's kill time on a **plain mob of that level**,
with no template and no passives. 1.00 = neutral, 1.50 = takes half again as long. The per-hit coefficients the server
applies (weapon-type resist, magic resist, bow resist) are included.

A mob is **ANTI-X** when X's factor is ≥ **1.25×** the fastest of the other two. It is **ANTI-PHYSICAL** when the
warrior and the bow are both ≥ 1.25× the mage.

## The result: 8 of 79 templates are anti-type, and 71 are neutral

| lvl | mob                | mage | warrior | bow  | verdict                  | passive                               |
| --- | ------------------ | ---- | ------- | ---- | ------------------------ | ------------------------------------- |
| 20  | `shield_skeleton`  | 0.63 | 1.50    | 1.50 | **ANTI-PHYSICAL**        | P.Def ×1.5, M.Def ×0.8, mRes −0.20    |
| 26  | `watcher_eye`      | 2.50 | 0.68    | 0.68 | **ANTI-MAGE**            | P.Def ×0.8, M.Def ×2.0, mRes +0.25    |
| 44  | `grave_lich`       | 1.80 | 0.80    | 0.80 | ANTI-MAGE                | P.Def ×0.8, M.Def ×1.5, mRes +0.20    |
| 45  | `fomor_brute`      | 0.64 | 1.51    | 1.50 | ANTI-PHYSICAL            | P.Def ×1.5, M.Def ×0.8, mRes −0.20    |
| 58  | `aether_wisp`      | 1.80 | 0.68    | 0.68 | ANTI-MAGE                | P.Def ×0.8, M.Def ×1.5, mRes +0.20    |
| 63  | `obsidian_knight`  | 0.80 | 1.43    | 2.00 | ANTI-PHYSICAL, +ARCHER   | "Stoneplate": sword + bow resists (bow worst), mRes −0.20 |
| 65  | `dread_knight`     | 0.64 | 1.49    | 1.51 | ANTI-PHYSICAL            | P.Def ×1.5, M.Def ×0.8, mRes −0.20    |
| 66  | `spiteful_ghost`   | 1.80 | 0.80    | 0.80 | ANTI-MAGE                | P.Def ×0.8, M.Def ×1.5, mRes +0.20    |

Near-misses, below the 1.25 line: the **archer- and mage-role** mobs (`orc_archer`, the two lizardman/orc archers at
39-40, `dread_archer`, `radiant_mage`) run 0.85-0.95 for the two physical attackers, because a ranged role trades
some defence. They are slightly *easier* for fighters and are not anti anything.

**What the numbers say:**
- **Only two anti-type mobs live in 1-40**: `shield_skeleton` (20) and `watcher_eye` (26). Your "for now" is two mobs.
- **There is no pure anti-archer mob.** `obsidian_knight` is anti-physical with the bow worst hit (2.00). An
  anti-archer family would have to be authored.
- ⚠ **`shield_skeleton` is a class-change quest target.** It is the second hunt (`mobB`) for the **Tank, Healer and
  Nuker** paths (`Quests.ClassChangeChains.cs`, `HuntTargets`). So the **Tank's** class change runs through an
  anti-physical mob at 1.5× kill time, while the Warrior, Rogue and Archer paths never meet it. Removing it from the
  world without touching the quest would leave three class changes with no target to kill.
- Every anti-type template joins **every generated camp whose level band contains it** (`MobCatalog.InBand`), mixed
  with neutral mobs. That mixing is the thing `BL-280` objects to.

## ❓ Questions

1. **Is the measure right?** Kill time against a plain mob of the same level, with 1.25× as the anti line. *My pick:
   yes.* Nothing sits between 1.1 and 1.4, so the line is not fragile.
2. **1-40, the two mobs.** Three ways:
   - **(a) Neutralise them for now** (*my pick*). Drop their skewing passives so they are plain mobs. No map work,
     the class-change quest keeps its target, and 1-40 levels the same for everyone at once. The anti versions come
     back as **new ids** in their own zones with `BL-281`, which is where you put the real design anyway.
   - (b) Take them out of the generated camps and hand-place each in **its own small camp**. They stay anti-type
     now, but that is authored spawn coordinates for two camps (the map work `BL-281` is meant to do), plus the
     Tank/Healer/Nuker quests pointed at the new camp.
   - (c) Take them out entirely and move the three quests' `mobB` to a neutral mob that spawns at 17-21 in the
     Stonewatch grounds. `skeleton_grunt` (18) is already `mobA`, so this needs a new level ~20 mob.
3. **41-90, the other six.** *My pick: leave them mixed until `BL-281`.* Your "for now" named only 1-40, and above
   40 the zones are where the map rework starts anyway.
4. **Do you want an anti-archer family at all?** Today none exists. *My pick: decide with `BL-281`'s zone plan, not now.*
5. **The other half of `BL-280`, mob clusters** (*"space mobs so a nuker is not pulling clusters"*), is not measured
   here. It needs a camp-density pass over `WorldPlan`, and I will draft it separately once 2 is ruled.
