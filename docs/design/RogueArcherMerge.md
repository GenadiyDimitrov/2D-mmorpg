# Merging Archer into Rogue (owner design, 2026-07-29)

**Status:** DESIGN AGREED, not built. One open question (how many disciplines Rogue splits into).

## The change

Today the tree is `Fighter → {Tank, Warrior, Rogue, Archer} → 2 disciplines each at 40`.
The owner's correction: **archer and dagger are the same class until level 40.**

```
Fighter  →  Rogue (20-40, learns BOTH Stab and Shot)  →  40: ranged branch / melee branch
```

So the three Archer 2nd classes — **Hunter (4, Demon), Warden (10, Elf), Marksman (16, Human)** —
are removed, and their players become Rogues (Stalker / Shadowblade / Assassin). The split moves
to the 3rd class.

## Why this also fixes the missing archer kit

The playtest-13 finding was that `Archetype.Archer` registers only `BattleFury @20` and
`PowerShot @24`, while every other archetype has a full 20/24/28/32/36 ladder. Under this merge
that gap **disappears without authoring a new table**: the existing Rogue 2nd-class kit in
`ClassSkillTables.Common.cs` already teaches both weapons —

- `RogueArmorMastery` / `RogueWeaponMastery`, levels 1-5 @ 20/24/28/32/36
- `PiercingStab` levels 1-5 (the dagger line)
- `PreciseShot` levels 1-5 (the bow line)
- `Sprint` @20, `BowExpertise` @28

— which is precisely the combined kit the owner described. The two orphaned archer skills
(`BattleFury`, `PowerShot`) fold into the Rogue table or move up to the ranged discipline.

## What it costs in code

| Piece | File | Change |
|---|---|---|
| Remove 3 second classes | `Classes.cs` | delete ids 4, 10, 16 |
| Rogue splits into >2 disciplines | `Classes.Third.cs` `Disciplines.Of` | returns a **tuple of 2** today; needs an array if Rogue gets 4 |
| 3rd-class id scheme | `Classes.Third.cs` `ThirdClassCatalog.Build` | `baseId = 100 + (sc.Id-1)*2` **assumes exactly 2 disciplines per class** — must change if Rogue gets 4 |
| Discipline parent | `Classes.Third.cs` `Disciplines.Parent` | Sharpshooter/Trapper → `Archetype.Rogue` |
| Skill table | `ClassSkillTables.Common.cs` | drop the 2-line Archer registration; fold its skills into Rogue |
| Class-change quests | `Quests/Quests.ClassChangeChains.cs:33` | the Archer branch's quest targets go away |
| Archetype-keyed math | `StatCalculator.cs:63,139`, `Skills.Common.cs:143,168`, `Skills.cs:519`, `ClassSkills.cs:133`, `Entity.cs:1302` | each is a `switch` arm on `Archetype.Archer`; either keep the enum member unused, or move the ranged behaviour to a discipline check |

**`Archetype.Archer` should stay in the enum** even with no 2nd class using it — several formulas
(bow range tier, the 0.66 coefficient, the ranged skill-range rule) key on it, and a discipline
check can map Sharpshooter/Trapper back to it.

**Database:** existing characters holding class 4/10/16 become invalid. Standard practice here is
to delete `Game.Server/game.db` and let `EnsureCreated()` rebuild — no migration needed.

## DECIDED (owner, 2026-07-29): the split is RACE-BASED

Two disciplines per race — one melee, one ranged — but **which two differs by race**. Each race's
rogue line gets its own identity instead of all three sharing the same four options.

| Race | Melee discipline | Ranged discipline |
|---|---|---|
| **Human** | 🆕 **`Nullblade`** — anti-magic dagger | `Sharpshooter` — accuracy / single-target DPS |
| **Demon** | `Venomweaver` — **venom** DoT dagger | 🆕 **`Hunter`** — demon ranger |
| **Elf** | `Phantom` — physical-evasion dagger | `Trapper` — utility ranger |

**This is structurally the CHEAP shape.** Every 2nd class still yields exactly two disciplines,
so `Disciplines.Of` stays a 2-tuple and `ThirdClassCatalog.Build`'s `baseId = 100 + (sc.Id-1)*2`
scheme survives untouched. Changes needed:
- **`Disciplines.Of` becomes race-aware** — `Of(Race, Archetype)` instead of `Of(Archetype)`.
- **`Parent()`** maps Sharpshooter / Trapper / Hunter / Nullblade → `Archetype.Rogue`.
- **Two new `Discipline` enum members**, `Nullblade` and `Hunter`. Append them at the END
  (12, 13) — the values are persisted on characters, so never renumber the existing ones.

Per-race skill divergence already works (Venomweaver's DoT trio is authored three ways today), so
none of this needs new machinery.

## How this maps onto the ORIGINAL design

`docs/design/Disciplines.md` (owner-authored) built a **matrix**: discipline = the mechanic axis,
race = a flavour axis running through all four disciplines —

> *"human 'evades' magic, the elf evades phys. the ork should outlive the target"*

- **Human** — anti-magic (raises the enemy's *magic fail* chance), crit rate / crit damage
- **Elf** — anti-physical (evasion, AS/MS)
- **Demon** — brute, outlives the target (skill damage, max HP, def)

The new race-based assignment picks **one cell per race per row**, and every pick matches the
flavour that race already had. Nothing has to be invented:

| New discipline | Comes from | Status |
|---|---|---|
| `Nullblade` (Human melee) | **Phantom-Human** (stealth + anti-magic + crit) merged with **Venomweaver-Human** (bleed + anti-magic) | both authored on paper; the bleed half (`Rupture` / `DetonateWounds`) is **already coded** |
| `Phantom` (Elf melee) | **Phantom-Elf** (anti-phys stealth) unchanged | authored; `Shadowstep` / `Vanish` coded |
| `Venomweaver` (Demon melee) | **Venomweaver-Demon** (venom: AGI-vs-CON, −atk/def) unchanged | authored + coded (`Envenom` / `VenomBurst`) |
| `Sharpshooter` (Human ranged) | **Sharpshooter-Human** (crit focus) unchanged | authored, placeholder skills only |
| `Hunter` (Demon ranged) | **Sharpshooter-Demon** (damage focus, party atk / skill-dmg buff) | authored; renamed to its own discipline |
| `Trapper` (Elf ranged) | **Trapper-Elf** (root flavour) unchanged | authored; `RepellingShot` / `SnareTrap` coded |

**Orphaned** by the collapse (kept in `Disciplines.md` for reference, not built):
Phantom-Demon · Venomweaver-Elf (poison — `ToxicSting` / `ToxicBurst`, **already coded**) ·
Sharpshooter-Elf · Trapper-Human · Trapper-Demon.

⚠ The Elf **poison** line is coded and now unused. Owner decided (2026-07-29) that the Demon
**keeps venom as authored** — poison is parked, not reassigned.

### Naming notes
- `Nullblade` is a NEW name for an EXISTING kit — the human Phantom. The name *Phantom* moves to
  the Elf, who holds its original stealth/evasion identity.
- `Hunter` reuses the name freed by deleting the Demon Archer 2nd class (id 4).
