# Anti-type mobs — the measured list (`BL-280`, 2026-09-24, ❓ yours to rule)

> *"anti mage and anti fighter and anti archer mobs need to be in self zones"* … **for now:** pull the anti-type
> mobs out so 1-40 levels the same for every archetype.

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
