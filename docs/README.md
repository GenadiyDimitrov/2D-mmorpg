# Documentation

Index of everything under `docs/`.

## Start here
- [**CHANGELOG.md**](CHANGELOG.md) — what has been built, newest first.
- [**Roadmap.md**](Roadmap.md) — what's planned (Now / Next / Later / Eventually / Blocked / Dropped).

## `guides/` — how to build & run
- [UnityClient.md](guides/UnityClient.md) — build and run the mobile client on a phone.
- [Publishing.md](guides/Publishing.md) — `tools/publish.ps1`: the versioned server zip + APK in `builds/`.

## `testing/` — what to verify
- [TestChecklist.md](testing/TestChecklist.md) — the running WPF / server playtest checklist.
- [TestChecklist.Unity.md](testing/TestChecklist.Unity.md) — the Unity client checklist.

## `design/` — how systems are meant to work
- [CombatResolution.md](design/CombatResolution.md) — the unified hit/evade/fail resolver.
- [DamageModel.md](design/DamageModel.md) — the `{Flat, Mod}` skill-damage model.
- [StatMods.md](design/StatMods.md) — the unified stat-modifier layer.
- [Crafting.md](design/Crafting.md) — the crafting-primary economy.
- [Instances.md](design/Instances.md) — instances & dungeons (design, not built).
- [AutoHunt.md](design/AutoHunt.md) — idle / offline auto-hunt.
- [Disciplines.md](design/Disciplines.md) · [Descipline.md](design/Descipline.md) — 3rd-class discipline design.
- [BareHands.md](design/BareHands.md) · [Unarmored.md](design/Unarmored.md) — unarmed / unarmored penalty investigations.

## `balance/` — measured balance
- [BalanceMatrix.md](balance/BalanceMatrix.md) — the balance audit. Its reasoning holds; some numbers
  are stale (measure with `tools/BalanceMatrix`, don't derive by hand).

## `data/` — source tables
- [classes_skills_csv/](data/classes_skills_csv) — per-class skill kits.
- [gear/](data/gear) — tiered gear sets.
- [mobs/](data/mobs) — mob base stats and passives.

---

New file? Put it where it belongs: a how-to in `guides/`, a what-to-check in `testing/`, a
system spec in `design/`, balance work in `balance/`, source tables in `data/`. Keep this index and
project-level docs (`CHANGELOG.md`, `Roadmap.md`) at the `docs/` root.
